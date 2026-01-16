using juicescript.ABC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static juicescript.runtime.Player;

namespace juicescript.runtime.buildin
{
	internal class ArrayImpl
	{
		
		[NativeFunction(".Array$public::Array")]
		public static void Array(Context context,
			ASMethod method,
			int scope_ptr, 
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtPayloadMethodScope)context.GC.Heap[scope_ptr].facility;

			var arrayinstance = context.GC.Heap[thisPtr.HeapPtr];			
			var rest = scope.ReadSlot(0, context.player);

			var rest_array = (RtPayloadArray)context.GC.Heap[rest.HeapPtr].facility;

			if (rest_array.StoreMode != RtPayloadArray.ArrayStoreMode.cache_on_stack)
				throw new InvalidOperationException();

			var array = (RtPayloadArray)arrayinstance.facility;

			var rest_span = rest_array.stack_store.Span;

			if (rest_span.Length > 0)
			{
				if (rest_span[0].Raw == thisPtr.Raw) // 这个参数是特意构造的，正常写代码是写不出的
				{
					/*
					 * 构造方法在 ExpressionIL.cs中 将new_instance的目标槽位当作参数传入，正常不可能写出这样的代码。
					 * StackLocater dst = makeOrGetLocater((TypeKind)compileEnv.CompileContext.player_for_compiler.Context.OBJECT.Type_identifier);
					 * List<StackLocater> arguments = new List<StackLocater>();
					 * arguments.Add(dst);
					 */

					rest_span = rest_span.Slice(1);
					goto lbl_rest_case;
				}
			}


			if (rest_span.Length == 1)
			{
				var a = rest_span[0];

				switch (a.ValueType)
				{
					case NaNBoxing.BoxType.Number:
					case NaNBoxing.BoxType.Int:
					case NaNBoxing.BoxType.Uint:
					case NaNBoxing.BoxType.Sbyte:
					case NaNBoxing.BoxType.Byte:
					case NaNBoxing.BoxType.Short:
					case NaNBoxing.BoxType.UShort:
					case NaNBoxing.BoxType.Float:

						var len = Extensions.GetDoubleValue(a);

						if (
							  len >= 0 &&
							   len == Math.Floor(len) &&
							   len <= uint.MaxValue

							)
						{
							uint count = (uint)Math.Floor( len);

							array.array_len = count;
							if (count <= RtPayloadArray.MAX_CACHE_ELEMENT)
							{
								for (int i = 0; i < count; i++)
								{
									array.cache_store[i].SetUndefined();
								}

								for (uint i = count; i < RtPayloadArray.MAX_CACHE_ELEMENT; i++)
								{
									array.cache_store[i].setFault();
								}

								return;

							}
							else
							{
								array.ChangeStoreToHeap(context.player, ref error);
								return;
							}

						}
						else
						{
							context.player.RaiseError(ref error, $"index is not a positive integer ({len})");
							return;
						}
					case NaNBoxing.BoxType.Undefined:
					case NaNBoxing.BoxType.Null:
					case NaNBoxing.BoxType.Boolean:
					case NaNBoxing.BoxType.HeapPtr:
						goto lbl_rest_case;
					case NaNBoxing.BoxType.Fault:
					default:
						throw new InvalidOperationException();
				}
			}


		lbl_rest_case:

			if (array.StoreMode == RtPayloadArray.ArrayStoreMode.cache)
			{
				array.array_len = (uint)rest_span.Length;
				for (int i = 0; i < rest_span.Length; i++)
				{
					var oldv = rest_span[i];
					if (oldv.ValueType == NaNBoxing.BoxType.HeapPtr)
					{
						var obj = context.GC.Heap[oldv.HeapPtr];
						if (obj.TypeKind == RtHeapTypeKind.INSTANCE && ((ASInstance)obj.Type).Flags.HasFlag(ClassFlags.Struct))
						{
							int cache_struct_ptr = array.cache_structs[i];

							var v_struct = context.GC.Heap[cache_struct_ptr];

							v_struct.Type = obj.Type;
							((RtPayloadInstance)v_struct.facility).HEAPINSTANCE_PTR = 0;
							((RtPayloadInstance)v_struct.facility).methodscopeslot_ref_state = 0;
							((RtPayloadInstance)v_struct.facility).CopyFrom(obj, context.player, obj.Type._link_codescope.TypeLayout.Size);

							array.cache_store[i].SetHeapPtr(cache_struct_ptr);
						}
						else
						{
							var v = context.player.GetSaveValue(oldv, ref error);
							if (error.raised)
							{
								return;
							}
							array.cache_store[i] = oldv;
						}

					}
					else
					{
						array.cache_store[i] = oldv;
					}
				}

				for (int i = rest_span.Length; i < RtPayloadArray.MAX_CACHE_ELEMENT; i++)
				{
					array.cache_store[i].setFault(); //另未赋值对象为fault.
				}
			}
			else
			{ 
				array.array_len = (uint)rest_span.Length;
				array.InitHeapData(rest_span, context.player, ref error);
			}
			
		}

		[NativeFunction(".Array$public::get#length")]
		public static void Array_get_length(
			Context context,
			ASMethod method,
			int scope_ptr, 
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex
			)
		{
			var arrayinstance = context.GC.Heap[thisPtr.HeapPtr];

			context.StackSlots[returnSlotIndex].SetUInt( ((RtPayloadArray)arrayinstance.facility).GetLength(context.player) );

		}

		[NativeFunction(".Array$public::set#length")]
		public static void Array_set_length(
			Context context,
			ASMethod method,
			int scope_ptr, 
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex
			)
		{
			var scope = (RtPayloadMethodScope)context.GC.Heap[scope_ptr].facility;
			var arrayinstance = context.GC.Heap[thisPtr.HeapPtr];
			var len = scope.ReadSlot(0, context.player);

			((RtPayloadArray)arrayinstance.facility).SetLength(len.UIntValue,context.player,ref error);
		}




		[NativeFunction(".Array$@::concat")]
		public static void Array_concat(
			Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex
			)
		{
			var scope = (RtPayloadMethodScope)context.GC.Heap[scope_ptr].facility;

			//var arrayinstance = context.GC.Heap[thisPtr.HeapPtr];
			var rest = scope.ReadSlot(0, context.player);

			var rest_array = (RtPayloadArray)context.GC.Heap[rest.HeapPtr].facility;

			Debug.Assert(rest_array.StoreMode == RtPayloadArray.ArrayStoreMode.cache_on_stack);
	
			//var array = (RtPayloadArray)arrayinstance.facility;
			var rest_span = rest_array.stack_store.Span;

			int ptrIndex = returnSlotIndex;
			int instancePtr = context.CacheArrayPtr + ptrIndex;
			var instance = context.GC.Heap[instancePtr];
			instance.Type = context.ARRAY.Instance;

			RtPayloadArray targetArray = (RtPayloadArray)instance.facility;
			targetArray.SetLength(0,context.player,ref error);
			targetArray.methodscopeslot_ref_state = 0;


			context.StackSlots[returnSlotIndex].SetHeapPtr(instancePtr);//先保存

			uint index = 0;
			for (int i = 0; i < rest_span.Length+1; i++)
			{
				if (index == uint.MaxValue)
				{
					context.player.RaiseRangeError(ref error, index.ToString(), uint.MaxValue);
					context.StackSlots[returnSlotIndex].SetUndefined();
					return;
				}

				NaNBoxing element = default;
				if (i == 0)
				{
					element = thisPtr;
				}
				else
				{ 
					element =  rest_span[i-1];
				}

				if (element.ValueType == NaNBoxing.BoxType.HeapPtr && context.GC.Heap[element.HeapPtr].TypeKind == RtHeapTypeKind.ARRAY) // 拆元素
				{
					
					RtPayloadArray src;
					int src_p = RtPayloadArray.FindAndUpdateHeapInstancePtr(element.HeapPtr, context.player, out src);
					RtHeapInstance src_instance = context.GC.Heap[src_p];


					uint src_len = src.GetLength(context.player);

					if ((ulong)index + src_len >= uint.MaxValue)
					{
						context.player.RaiseRangeError(ref error, index.ToString(), uint.MaxValue);
						context.StackSlots[returnSlotIndex].SetUndefined();
						return;
					}

					targetArray.SetLength(index + src_len, context.player, ref error);
					if (error.raised)
					{
						context.StackSlots[returnSlotIndex].SetUndefined();
						return;
					}

					Dictionary<uint, NaNBoxing> own_property = new Dictionary<uint, NaNBoxing>();
					//先遍历proto,把洞补了
					context.player.VisitArrayProto(src_instance, 
						(string key,NaNBoxing v) => 
						{
							uint index;
							if (uint.TryParse(key, out index))
							{
								if (index < src_len)
								{
									own_property.TryAdd(index, v);
								}
							}
						
						} );

					foreach (var item in own_property)
					{
						context.player.SetArraySlot(item.Value, item.Key + index, instance, ref error);
						if (error.raised)
						{
							return;
						}
					}


					uint st = 0;
					uint key; uint next;
					NaNBoxing v;
					while (src.TryReadIterItem((int)st, out key, out next, out v, context))
					{
						context.player.SetArraySlot(v, key + index, instance , ref error);
						if (error.raised)
						{
							return;
						}

						st = next;
					}


					index = index + src_len;
				}
				else
				{
					context.player.SetArraySlot(element, index, instance, ref error);
					if (error.raised)
					{
						return;
					}

					index++;
				}

				
			}




			int finalptr = RtPayloadArray.FindAndUpdateHeapInstancePtr(instancePtr, context.player, out targetArray);
			context.StackSlots[returnSlotIndex].SetHeapPtr(finalptr);

		}

	}
}
