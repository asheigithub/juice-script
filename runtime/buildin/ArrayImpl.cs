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

#if DEBUG
			if (rest_array.StoreMode != RtPayloadArray.ArrayStoreMode.cache_on_stack)
				throw new InvalidOperationException();
#endif

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

		class ArrayToString : IPrint
		{
			internal StringBuilder sb;
			public void Write(string message)
			{
				sb.Append(message);
			}

			public void Write(ReadOnlySpan<char> chars)
			{
				sb.Append(chars);
			}

			public void WriteLine(string message)
			{
				sb.AppendLine(message);
			}
		}

		[NativeFunction(".Array$@::toString")]
		public static void Array_Proto_toString(
			Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex
			)
		{
			
			context.player.ConvertValueType(ref error, thisPtr, TypeKind.Array, context.ARRAY, ref context.StackSlots[returnSlotIndex], scope_ptr);
			if (error.raised)
			{
				context.StackSlots[returnSlotIndex].SetUndefined();
				return;
			}

			var v = context.StackSlots[returnSlotIndex];
			if (v.ValueType != NaNBoxing.BoxType.HeapPtr)
			{
				context.player.RaiseTypeError(ref error, thisPtr, TypeKind.Array);
				context.StackSlots[returnSlotIndex].SetUndefined();
				return;
			}

			RtHeapInstance arr = context.GC.Heap[v.HeapPtr];
			Debug.Assert(arr.TypeKind == RtHeapTypeKind.ARRAY);

			RtPayloadArray arr_payload = (RtPayloadArray)arr.facility;

			StringBuilder sb = new StringBuilder();
			ArrayToString arrayToString = new ArrayToString();
			arrayToString.sb = sb;

			arr_payload.Trace(context, stackStPos, ref error, scope_ptr, arrayToString, arr,",");

			string str = sb.ToString();
			if (string.IsNullOrEmpty(str))
			{
				context.StackSlots[returnSlotIndex].SetHeapPtr(context.player.EMPTY_STR);
			}
			else
			{
				NaNBoxing o;
				if (context.player.TryCreateStringValue(str, out o, ref error))
				{
					context.StackSlots[returnSlotIndex] = o;
				}
			}

		}

		[NativeFunction(".Array$:AS3::concat")]
		public static void Array_concat(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{ 
			Array_Proto_concat(context,method,scope_ptr,thisPtr,stackStPos,ref error,returnSlotIndex);
		}


		[NativeFunction(".Array$@::concat")]
		public static void Array_Proto_concat(
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
			targetArray.methodscopeslot_ref_state = 0;
			targetArray.HEAPINSTANCE_PTR = 0;
			targetArray.SetLength(0,context.player,ref error);
			
			

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
						(NaNBoxing key,NaNBoxing v) => 
						{
							

							if (key.ValueType == NaNBoxing.BoxType.LocalString)
							{
								Span<char> temp = stackalloc char[16];

								int l = key.GetLocalStringChars(temp);
								uint index;
								if (uint.TryParse(temp.Slice(0, l), out index))
								{
									if (index < src_len)
									{
										own_property.TryAdd(index, v);
									}
								}
							}
							else
							{
								var k = ((RtPayloadString)context.GC.Heap[key.HeapPtr].facility).Str.AsSpan();
								uint index;
								if (uint.TryParse(k, out index))
								{
									if (index < src_len)
									{
										own_property.TryAdd(index, v);
									}
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


		//.Array$:AS3::push
		[NativeFunction(".Array$:AS3::push")]
		public static void Array_push(
			Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex
			)
		{ 
			Array_Proto_push(context,method,scope_ptr,thisPtr,stackStPos,ref error,returnSlotIndex);
		}

		[NativeFunction(".Array$@::push")]
		public static void Array_Proto_push(
			Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex
			)
		{
			// 1. Validate thisPtr is an Array
			if (thisPtr.ValueType != NaNBoxing.BoxType.HeapPtr || 
				context.GC.Heap[thisPtr.HeapPtr].TypeKind != RtHeapTypeKind.ARRAY)
			{
				context.player.RaiseTypeError(ref error, thisPtr, TypeKind.Array);
				context.StackSlots[returnSlotIndex].SetUInt(0);
				return;
			}

			// 2. Get array instance and rest parameters
			var arrayInstance = context.GC.Heap[thisPtr.HeapPtr];
			var scope = (RtPayloadMethodScope)context.GC.Heap[scope_ptr].facility;
			var rest = scope.ReadSlot(0, context.player);
			var restArray = (RtPayloadArray)context.GC.Heap[rest.HeapPtr].facility;
			var restSpan = restArray.stack_store.Span;

			// 3. Get current length
			var array = (RtPayloadArray)arrayInstance.facility;
			uint currentLength = array.GetLength(context.player);

			// 4. Handle empty push case
			if (restSpan.Length == 0)
			{
				context.StackSlots[returnSlotIndex].SetUInt(currentLength);
				return;
			}

			// 5. Check for overflow
			if (currentLength > uint.MaxValue - (uint)restSpan.Length)
			{
				context.player.RaiseRangeError(ref error, ((long)currentLength + restSpan.Length).ToString() , uint.MaxValue);
				context.StackSlots[returnSlotIndex].SetUInt(currentLength);
				return;
			}

			// 6. Push each element
			for (int i = 0; i < restSpan.Length; i++)
			{
				uint targetIndex = currentLength + (uint)i;
				context.player.SetArraySlot(restSpan[i], targetIndex, arrayInstance, ref error);
				if (error.raised)
				{
					context.StackSlots[returnSlotIndex].SetUInt(currentLength);
					return;
				}
			}

			// 7. Return new length
			uint newLength = currentLength + (uint)restSpan.Length;
			context.StackSlots[returnSlotIndex].SetUInt(newLength);
		}



		[NativeFunction(".Array$:AS3::pop")]
		public static void Array_pop(
			Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex
			)
		{
			Array_Proto_pop(context, method, scope_ptr, thisPtr, stackStPos, ref error, returnSlotIndex);
		}

		//.Array$@::pop
		[NativeFunction(".Array$@::pop")]
		public static void Array_Proto_pop(
			Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex
			)
		{
			// 1. Validate thisPtr is an Array
			if (thisPtr.ValueType != NaNBoxing.BoxType.HeapPtr ||
				context.GC.Heap[thisPtr.HeapPtr].TypeKind != RtHeapTypeKind.ARRAY)
			{
				context.player.RaiseTypeError(ref error, thisPtr, TypeKind.Array);
				context.StackSlots[returnSlotIndex].SetUndefined();
				return;
			}

			var scope = (RtPayloadMethodScope)context.GC.Heap[scope_ptr].facility;
			
			RtPayloadArray array;
			RtPayloadArray.FindAndUpdateHeapInstancePtr(thisPtr.HeapPtr,context.player,out array);

			if (array.array_len > 0)
			{
				bool isoutindex;
				NaNBoxing e = array.ReadSlot( array.array_len-1 , context.player, out isoutindex);
				context.StackSlots[returnSlotIndex] = e;

				array.SetLength(array.array_len - 1, context.player, ref error);
			}
			else
			{
				context.StackSlots[returnSlotIndex].SetUndefined();
			}

		}

		//.Array$:AS3::join
		[NativeFunction(".Array$:AS3::join")]
		public static void Array_join(
			Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex
			)
		{
			Array_Proto_join(context, method, scope_ptr, thisPtr, stackStPos, ref error, returnSlotIndex);
		}


		[NativeFunction(".Array$@::join")]
		public static void Array_Proto_join(
			Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex
			)
		{
			// 1. Validate thisPtr is an Array
			if (thisPtr.ValueType != NaNBoxing.BoxType.HeapPtr ||
				context.GC.Heap[thisPtr.HeapPtr].TypeKind != RtHeapTypeKind.ARRAY)
			{
				
				context.StackSlots[returnSlotIndex].SetHeapPtr( context.player.EMPTY_STR );
				return;
			}

			
			var scope = (RtPayloadMethodScope)context.GC.Heap[scope_ptr].facility;

			var sep = scope.ReadSlot(0, context.player);

			ReadOnlySpan<char> sepstr;

			if (scope.__sendargcount == 0)
			{
				sepstr = ",";
			}
			else if (sep.ValueType == NaNBoxing.BoxType.Undefined)
			{
				sepstr = ",";
			}
			else if (context.player.IsPrimitive(sep))
			{
				sepstr = Extensions.GetPrimitiveValueToString(context.player, sep);
			}
			else
			{
				context.player.ConvertValueType(ref error, sep, TypeKind.String, context.STRING, ref context.StackSlots[returnSlotIndex], scope_ptr);
				if (error.raised)
				{
					return;
				}

				sepstr = Extensions.GetPrimitiveValueToString(context.player, context.StackSlots[returnSlotIndex]);
			}


			StringBuilder sb = new StringBuilder();
			ArrayToString arrayToString = new ArrayToString();
			arrayToString.sb = sb;

			RtPayloadArray array;
			int thisP = RtPayloadArray.FindAndUpdateHeapInstancePtr(thisPtr.HeapPtr, context.player, out array);


			array.Trace(context, stackStPos, ref error, scope_ptr, arrayToString, context.GC.Heap[thisP],sepstr );

			string str = sb.ToString();
			if (string.IsNullOrEmpty(str))
			{
				context.StackSlots[returnSlotIndex].SetHeapPtr(context.player.EMPTY_STR);
			}
			else
			{
				int p = context.GC.AllocString(str);
				if (p == 0)
				{
					context.player.RaiseOutOfMemory(ref error);
					return;
				}
				else
				{
					context.StackSlots[returnSlotIndex].SetHeapPtr(p);
				}
			}


		}



		[NativeFunction(".Array$:AS3::shift")]
		public static void Array_shift(
			Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex
			)
		{
			Array_Proto_shift(context, method, scope_ptr, thisPtr, stackStPos, ref error, returnSlotIndex);
		}


		[NativeFunction(".Array$@::shift")]
		public static void Array_Proto_shift(
			Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex
			)
		{
			

		}




	}

}
