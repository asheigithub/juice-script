using juicescript.ABC;
using juicescript.ABC.Locaters;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static juicescript.NaNBoxing;
using static juicescript.runtime.buildin.VectorImpl;
using static juicescript.runtime.Player;
using static System.Formats.Asn1.AsnWriter;
using static System.Net.Mime.MediaTypeNames;

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
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			var arrayinstance = context.GC.Heap[thisPtr.HeapPtr];
			var rest = scope.ReadSlot(0, context.player);

			var rest_array = (RtArray)context.GC.Heap[rest.HeapPtr];

#if DEBUG
			if (rest_array.StoreMode != RtArray.ArrayStoreMode.cache_on_stack)
				throw new InvalidOperationException();
#endif

			var array = (RtArray)arrayinstance;

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
							uint count = (uint)Math.Floor(len);

							array.array_len = count;
							if (count <= RtArray.MAX_CACHE_ELEMENT)
							{
								for (int i = 0; i < count; i++)
								{
									array.cache_store[i].SetUndefined();
								}

								for (uint i = count; i < RtArray.MAX_CACHE_ELEMENT; i++)
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

			if (array.StoreMode == RtArray.ArrayStoreMode.cache)
			{
				array.array_len = (uint)rest_span.Length;
				for (int i = 0; i < rest_span.Length; i++)
				{
					var oldv = rest_span[i];
					if (oldv.ValueType == NaNBoxing.BoxType.HeapPtr)
					{
						var obj = context.GC.Heap[oldv.HeapPtr];
						if (obj.Kind == RtHeapTypeKind.INSTANCE && ((ASInstance)obj.Type).Flags.HasFlag(ClassFlags.Struct))
						{
							int cache_struct_ptr = array.cache_structs[i];

							var v_struct = context.GC.Heap[cache_struct_ptr]; 

							v_struct.Type = obj.Type;
							((RtInstance)v_struct).HEAPINSTANCE_PTR = 0;
							((RtInstance)v_struct).methodscopeslot_ref_state = 0;
							((RtInstance)v_struct).CopyFrom(obj, context.player, obj.Type._link_codescope.TypeLayout.Size);

							array.cache_store[i].SetHeapPtr(cache_struct_ptr, (byte)RtHeapTypeKind.INSTANCE , (byte)HeapKindFlag.FLAG_STRUCT);
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

				for (int i = rest_span.Length; i < RtArray.MAX_CACHE_ELEMENT; i++)
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

			context.StackSlots[returnSlotIndex].SetUInt(((RtArray)arrayinstance).GetLength(context.player));

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
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
			var arrayinstance = context.GC.Heap[thisPtr.HeapPtr];
			var len = scope.ReadSlot(0, context.player);

			((RtArray)arrayinstance).SetLength(len.UIntValue, context.player, ref error);
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

		//[NativeFunction(".Array$:AS3::toString")]
		//public static void Array_toString(Context context,
		//	ASMethod method,
		//	int scope_ptr,
		//	NaNBoxing thisPtr,
		//	int stackStPos, ref ReceiveError error, int returnSlotIndex)
		//{
		//	Array_Proto_toString(context, method, scope_ptr, thisPtr, stackStPos, ref error, returnSlotIndex);
		//}



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

			RtHeapBase arr = context.GC.Heap[v.HeapPtr];
			Debug.Assert(arr.Kind == RtHeapTypeKind.ARRAY);

			RtArray arr_payload = (RtArray)arr;

			StringBuilder sb = new StringBuilder();
			ArrayToString arrayToString = new ArrayToString();
			arrayToString.sb = sb;

			arr_payload.Trace(context, stackStPos, ref error, scope_ptr, arrayToString, arr, ",");

			string str = sb.ToString();
			if (string.IsNullOrEmpty(str))
			{
				context.StackSlots[returnSlotIndex].SetHeapPtr(context.player.EMPTY_STR, (byte)RtHeapTypeKind.STRING , (byte)HeapKindFlag.NONE);
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
			Array_Proto_concat(context, method, scope_ptr, thisPtr, stackStPos, ref error, returnSlotIndex);
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
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			//var arrayinstance = context.GC.Heap[thisPtr.HeapPtr];
			var rest = scope.ReadSlot(0, context.player);

			var rest_array = (RtArray)context.GC.Heap[rest.HeapPtr];

			Debug.Assert(rest_array.StoreMode == RtArray.ArrayStoreMode.cache_on_stack);

			//var array = (RtPayloadArray)arrayinstance;
			var rest_span = rest_array.stack_store.Span;

			int ptrIndex = returnSlotIndex;
			int instancePtr = context.CacheArrayPtr + ptrIndex;
			var instance = context.GC.Heap[instancePtr];
			instance.Type = context.ARRAY.Instance;

			RtArray targetArray = (RtArray)instance;
			targetArray.methodscopeslot_ref_state = 0;
			targetArray.HEAPINSTANCE_PTR = 0;
			targetArray.SetLength(0, context.player, ref error);



			context.StackSlots[returnSlotIndex].SetHeapPtr(instancePtr, (byte)RtHeapTypeKind.ARRAY, (byte)HeapKindFlag.NONE);//先保存

			uint index = 0;
			for (int i = 0; i < rest_span.Length + 1; i++)
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
					element = rest_span[i - 1];
				}

				if (element.ValueType == NaNBoxing.BoxType.HeapPtr && element.HeapKind == (byte)RtHeapTypeKind.ARRAY) // 拆元素
				{

					RtArray src;
					int src_p = RtArray.FindAndUpdateHeapInstancePtr(element.HeapPtr, context.player, out src);
					RtHeapBase src_instance = context.GC.Heap[src_p];


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
						(NaNBoxing key, NaNBoxing v) =>
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
								var k = ((RtString)context.GC.Heap[key.HeapPtr]).Str.AsSpan();
								uint index;
								if (uint.TryParse(k, out index))
								{
									if (index < src_len)
									{
										own_property.TryAdd(index, v);
									}
								}

							}


						});

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
						context.player.SetArraySlot(v, key + index, instance, ref error);
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




			int finalptr = RtArray.FindAndUpdateHeapInstancePtr(instancePtr, context.player, out targetArray);
			context.StackSlots[returnSlotIndex].SetHeapPtr(finalptr, (byte)RtHeapTypeKind.ARRAY, (byte)HeapKindFlag.NONE);

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
			Array_Proto_push(context, method, scope_ptr, thisPtr, stackStPos, ref error, returnSlotIndex);
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
				context.GC.Heap[thisPtr.HeapPtr].Kind != RtHeapTypeKind.ARRAY)
			{
				context.player.RaiseTypeError(ref error, thisPtr, TypeKind.Array);
				context.StackSlots[returnSlotIndex].SetUInt(0);
				return;
			}

			// 2. Get array instance and rest parameters
			var arrayInstance = context.GC.Heap[thisPtr.HeapPtr];
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
			var rest = scope.ReadSlot(0, context.player);
			var restArray = (RtArray)context.GC.Heap[rest.HeapPtr];
			var restSpan = restArray.stack_store.Span;

			// 3. Get current length
			var array = (RtArray)arrayInstance;
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
				context.player.RaiseRangeError(ref error, ((long)currentLength + restSpan.Length).ToString(), uint.MaxValue);
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
				context.GC.Heap[thisPtr.HeapPtr].Kind != RtHeapTypeKind.ARRAY)
			{
				context.player.RaiseTypeError(ref error, thisPtr, TypeKind.Array);
				context.StackSlots[returnSlotIndex].SetUndefined();
				return;
			}

			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			RtArray array;
			RtArray.FindAndUpdateHeapInstancePtr(thisPtr.HeapPtr, context.player, out array);

			if (array.array_len > 0)
			{
				bool isoutindex;
				NaNBoxing e = array.ReadSlot(array.array_len - 1, context.player, out isoutindex);
				context.StackSlots[returnSlotIndex] = e;

				if (e.ValueType == BoxType.HeapPtr && e.HeapKind == (byte)RtHeapTypeKind.INSTANCE)
				{
					var check = context.GC.Heap[e.HeapPtr];
					if (((ASInstance)check.Type).Flags.HasFlag(ClassFlags.Struct))
					{
						int clonedptr = returnSlotIndex + context.CacheInstancePtr;
						var cacheObj = context.GC.Heap[clonedptr];
						cacheObj.Type = check.Type;

						((RtInstance)cacheObj).methodscopeslot_ref_state = 0;
						((RtInstance)cacheObj).HEAPINSTANCE_PTR = 0;
						((RtInstance)cacheObj).CopyFrom(check, context.player, check.Type._link_codescope.TypeLayout.Size);

						context.StackSlots[returnSlotIndex].SetHeapPtr(clonedptr, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.FLAG_STRUCT);
					}
				}




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
				context.GC.Heap[thisPtr.HeapPtr].Kind != RtHeapTypeKind.ARRAY)
			{

				context.StackSlots[returnSlotIndex].SetHeapPtr(context.player.EMPTY_STR, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				return;
			}


			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			var sep = scope.ReadSlot(0, context.player);


			Span<char> buffer = stackalloc char[16];
			ReadOnlySpan<char> sepstr = buffer;

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
				sepstr = Extensions.GetPrimitiveValueToString(context.player, sep, buffer);
			}
			else
			{
				context.player.ConvertValueType(ref error, sep, TypeKind.String, context.STRING, ref context.StackSlots[returnSlotIndex], scope_ptr);
				if (error.raised)
				{
					return;
				}

				sepstr = Extensions.GetPrimitiveValueToString(context.player, context.StackSlots[returnSlotIndex], buffer);
			}


			StringBuilder sb = new StringBuilder();
			ArrayToString arrayToString = new ArrayToString();
			arrayToString.sb = sb;

			RtArray array;
			int thisP = RtArray.FindAndUpdateHeapInstancePtr(thisPtr.HeapPtr, context.player, out array);


			array.Trace(context, stackStPos, ref error, scope_ptr, arrayToString, context.GC.Heap[thisP], sepstr);

			string str = sb.ToString();
			if (string.IsNullOrEmpty(str))
			{
				context.StackSlots[returnSlotIndex].SetHeapPtr(context.player.EMPTY_STR, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
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
					context.StackSlots[returnSlotIndex].SetHeapPtr(p, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
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
			// 1. Validate thisPtr is an Array
			if (thisPtr.ValueType != NaNBoxing.BoxType.HeapPtr ||
				context.GC.Heap[thisPtr.HeapPtr].Kind != RtHeapTypeKind.ARRAY)
			{
				context.player.RaiseTypeError(ref error, thisPtr, TypeKind.Array);
				context.StackSlots[returnSlotIndex].SetUndefined();
				return;
			}

			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			RtArray array;
			int instancePtr = RtArray.FindAndUpdateHeapInstancePtr(thisPtr.HeapPtr, context.player, out array);

			if (array.array_len > 0)
			{
				RtHeapBase instance = context.GC.Heap[instancePtr];

				bool isoutindex;
				NaNBoxing e = array.ReadSlot(0, context.player, out isoutindex);
				context.StackSlots[returnSlotIndex] = e;

				if (e.ValueType == BoxType.HeapPtr && e.HeapKind == (byte)RtHeapTypeKind.INSTANCE)
				{
					var check = context.GC.Heap[e.HeapPtr];
					if (((ASInstance)check.Type).Flags.HasFlag(ClassFlags.Struct))
					{
						int clonedptr = returnSlotIndex + context.CacheInstancePtr;
						var cacheObj = context.GC.Heap[clonedptr];
						cacheObj.Type = check.Type;

						((RtInstance)cacheObj).methodscopeslot_ref_state = 0;
						((RtInstance)cacheObj).HEAPINSTANCE_PTR = 0;
						((RtInstance)cacheObj).CopyFrom(check, context.player, check.Type._link_codescope.TypeLayout.Size);

						context.StackSlots[returnSlotIndex].SetHeapPtr(clonedptr, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.FLAG_STRUCT);
					}
				}

				array.DoShift(context.player, ref error);
				if (error.raised)
				{
					return;
				}

				//for (uint i = 1; i < array.array_len; i++)
				//{
				//	NaNBoxing v =  array.ReadSlot(i, context.player, out isoutindex);
				//	context.player.SetArraySlot(v, i - 1, instance, ref error);

				//	Debug.Assert(!error.raised);
				//}


				array.SetLength(array.array_len - 1, context.player, ref error);
			}
			else
			{
				context.StackSlots[returnSlotIndex].SetUndefined();
			}

		}

		//.Array$:AS3::unshift
		[NativeFunction(".Array$:AS3::unshift")]
		public static void Array_unshift(
			Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex
			)
		{
			Array_Proto_unshift(context, method, scope_ptr, thisPtr, stackStPos, ref error, returnSlotIndex);
		}

		[NativeFunction(".Array$@::unshift")]
		public static void Array_Proto_unshift(
			Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex
			)
		{
			// 1. Validate thisPtr is an Array
			if (thisPtr.ValueType != NaNBoxing.BoxType.HeapPtr ||
				context.GC.Heap[thisPtr.HeapPtr].Kind != RtHeapTypeKind.ARRAY)
			{
				context.player.RaiseTypeError(ref error, thisPtr, TypeKind.Array);
				context.StackSlots[returnSlotIndex].SetUndefined();
				return;
			}

			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
			var rest = scope.ReadSlot(0, context.player);
			var restArray = (RtArray)context.GC.Heap[rest.HeapPtr];
			var restSpan = restArray.stack_store.Span;

			RtArray array;
			int instancePtr = RtArray.FindAndUpdateHeapInstancePtr(thisPtr.HeapPtr, context.player, out array);

			// 3. Get current length
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
				context.player.RaiseRangeError(ref error, ((long)currentLength + restSpan.Length).ToString(), uint.MaxValue);
				context.StackSlots[returnSlotIndex].SetUInt(currentLength);
				return;
			}

			// 7,扩容并且移动元素

			array.DoUnshift(context.player, ref error, restSpan);
			if (error.raised)
			{
				return;
			}

			instancePtr = RtArray.FindAndUpdateHeapInstancePtr(thisPtr.HeapPtr, context.player, out array);
			var instance = context.GC.Heap[instancePtr];
			//复制元素
			for (int i = 0; i < restSpan.Length; i++)
			{
				context.player.SetArraySlot(restSpan[i], (uint)i, instance, ref error);
				if (error.raised)
				{
					return;
				}
			}

			context.StackSlots[returnSlotIndex].SetUInt(currentLength + (uint)restSpan.Length);

		}

		//.Array$:AS3::reverse
		[NativeFunction(".Array$:AS3::reverse")]
		public static void Array_reverse(
			Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex
			)
		{

			RtArray array;
			int instancePtr = RtArray.FindAndUpdateHeapInstancePtr(thisPtr.HeapPtr, context.player, out array);
			var instance = context.GC.Heap[instancePtr];

			array.DoReverse(context, ref error, returnSlotIndex);

			context.StackSlots[returnSlotIndex].SetHeapPtr(instancePtr, (byte)RtHeapTypeKind.ARRAY, (byte)HeapKindFlag.NONE);

		}



		[NativeFunction(".Array$:AS3::some")]
		public static void Array_some(
			Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex
			)
		{
			Array_Proto_some(context, method, scope_ptr, thisPtr, stackStPos, ref error, returnSlotIndex);
		}

		[NativeFunction(".Array$@::some")]
		public static void Array_Proto_some(
			Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex
			)
		{
			if (thisPtr.ValueType != NaNBoxing.BoxType.HeapPtr ||
				context.GC.Heap[thisPtr.HeapPtr].Kind != RtHeapTypeKind.ARRAY)
			{
				//context.player.RaiseTypeError(ref error, thisPtr, TypeKind.Array);
				context.StackSlots[returnSlotIndex].SetBoolean(false);
				return;
			}

			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];


			RtArray array;
			int arrPtr = RtArray.FindAndUpdateHeapInstancePtr(scope.ThisPtr.HeapPtr, context.player, out array);

			uint len = array.array_len;


			context.StackSlots[returnSlotIndex].SetHeapPtr(arrPtr, (byte)RtHeapTypeKind.ARRAY, (byte)HeapKindFlag.NONE); //保持到槽，防止GC

			if (context.StackPosition + 5 >= Context.STACK_LENGTH)
			{
				context.player.RaiseStackOverflow(ref error);
				return;
			}

			var cb = scope.ReadSlot(0, context.player);
			if (cb.ValueType != BoxType.HeapPtr)
			{
				context.player.RaiseTypeError(ref error, cb, TypeKind.Function);
				return;
			}

			var _this = scope.ReadSlot(1, context.player);
			//if (scope.__sendargcount < 2)
			//{
			//	_this.SetUndefined();
			//}


			var cbmethod = ((ASMethodBody)context.GC.Heap[cb.HeapPtr].Type).Method;
			var cbclosure = (RtClosure)context.GC.Heap[cb.HeapPtr];

			if (cbmethod.__ismethod && !cbmethod.__is_call_or_apply)
			{
				_this = cbclosure.This;
			}
			else if (cbmethod.__is_hasOwnProperty)
			{

			}
			else if ((_this.ValueType == NaNBoxing.BoxType.Undefined
				||
				_this.ValueType == NaNBoxing.BoxType.Null
				) //&& scope.__sendargcount==2
				)
			{

				var sss = context.GC.Heap[cb.HeapPtr].Type._link_codescope.Parent; //Context.GC.Heap[scope_ptr].Type._link_codescope.Parent;
				while (sss.Kind != CodeScopeKind.Script)
				{
					sss = sss.Parent;
				}

				var globalptr = ((ASScript)sss.Container).__global_index__;
				_this.SetHeapPtr(globalptr, (byte)RtHeapTypeKind.GLOBAL, (byte)HeapKindFlag.NONE);

			}



			int basePos = context.StackPosition;
			var argSlots = context.StackSlots.AsSpan(basePos, 5);
			argSlots.Clear();

			context.StackPosition += 5;

			unsafe
			{

				StackLocater* args = stackalloc StackLocater[3];
				args[0].index = 2;
				args[1].index = 3;
				args[2].index = 4;

				bool issome = false;
				uint olen = len;
				for (uint i = 0; i < len && i < olen; i++)
				{
					bool isoutofindex;
					NaNBoxing v = array.ReadSlot(i, context.player, out isoutofindex);

					argSlots[2] = v;
					argSlots[3].SetInt((int)i);
					argSlots[4].SetHeapPtr(arrPtr, (byte)RtHeapTypeKind.ARRAY, (byte)HeapKindFlag.NONE);

					NaNBoxing r = context.player.RunMethod(cbmethod, _this, cbclosure.ScopePtr, cbclosure.ScopeType, 3, (byte*)args, argSlots, ref error, basePos + 1);
					if (error.raised)
					{
						context.StackPosition -= 5;
						return;
					}


					arrPtr = RtArray.FindAndUpdateHeapInstancePtr(scope.ThisPtr.HeapPtr, context.player, out array);
					len = array.array_len;


					context.player.ConvertValueType(ref error, r, TypeKind.Boolean, context.BOOLEAN, ref r);
					Debug.Assert(!error.raised); //转BOOLEAN不会失败

					if (r.Boolean)
					{
						issome = true;
						break;
					}

				}

				context.StackSlots[returnSlotIndex].SetBoolean(issome);

			}


			context.StackPosition -= 5;


		}


		//.Array$@::every


		[NativeFunction(".Array$:AS3::every")]
		public static void Array_every(
			Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex
			)
		{
			Array_Proto_every(context, method, scope_ptr, thisPtr, stackStPos, ref error, returnSlotIndex);
		}

		[NativeFunction(".Array$@::every")]
		public static void Array_Proto_every(
			Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex
			)
		{
			if (thisPtr.ValueType != NaNBoxing.BoxType.HeapPtr ||
				context.GC.Heap[thisPtr.HeapPtr].Kind != RtHeapTypeKind.ARRAY)
			{
				//context.player.RaiseTypeError(ref error, thisPtr, TypeKind.Array);
				context.StackSlots[returnSlotIndex].SetBoolean(false);
				return;
			}

			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];


			RtArray array;
			int arrPtr = RtArray.FindAndUpdateHeapInstancePtr(scope.ThisPtr.HeapPtr, context.player, out array);

			uint len = array.array_len;


			context.StackSlots[returnSlotIndex].SetHeapPtr(arrPtr, (byte)RtHeapTypeKind.ARRAY, (byte)HeapKindFlag.NONE); //保持到槽，防止GC

			if (context.StackPosition + 5 >= Context.STACK_LENGTH)
			{
				context.player.RaiseStackOverflow(ref error);
				return;
			}

			var cb = scope.ReadSlot(0, context.player);
			if (cb.ValueType != BoxType.HeapPtr)
			{
				context.player.RaiseTypeError(ref error, cb, TypeKind.Function);
				return;
			}

			var _this = scope.ReadSlot(1, context.player);
			//if (scope.__sendargcount < 2)
			//{
			//	_this.SetUndefined();
			//}


			var cbmethod = ((ASMethodBody)context.GC.Heap[cb.HeapPtr].Type).Method;
			var cbclosure = (RtClosure)context.GC.Heap[cb.HeapPtr];

			if (cbmethod.__ismethod && !cbmethod.__is_call_or_apply)
			{
				_this = cbclosure.This;
			}
			else if (cbmethod.__is_hasOwnProperty)
			{

			}
			else if ((_this.ValueType == NaNBoxing.BoxType.Undefined
				||
				_this.ValueType == NaNBoxing.BoxType.Null
				) //&& scope.__sendargcount==2
				)
			{

				var sss = context.GC.Heap[cb.HeapPtr].Type._link_codescope.Parent; //Context.GC.Heap[scope_ptr].Type._link_codescope.Parent;
				while (sss.Kind != CodeScopeKind.Script)
				{
					sss = sss.Parent;
				}

				var globalptr = ((ASScript)sss.Container).__global_index__;
				_this.SetHeapPtr(globalptr, (byte)RtHeapTypeKind.GLOBAL, (byte)HeapKindFlag.NONE);

			}



			int basePos = context.StackPosition;
			var argSlots = context.StackSlots.AsSpan(basePos, 5);
			argSlots.Clear();

			context.StackPosition += 5;

			unsafe
			{

				StackLocater* args = stackalloc StackLocater[3];
				args[0].index = 2;
				args[1].index = 3;
				args[2].index = 4;


				uint olen = len;
				for (uint i = 0; i < len && i < olen; i++)
				{
					bool isoutofindex;
					NaNBoxing v = array.ReadSlot(i, context.player, out isoutofindex);

					argSlots[2] = v;
					argSlots[3].SetInt((int)i);
					argSlots[4].SetHeapPtr(arrPtr, (byte)RtHeapTypeKind.ARRAY, (byte)HeapKindFlag.NONE);

					NaNBoxing r = context.player.RunMethod(cbmethod, _this, cbclosure.ScopePtr, cbclosure.ScopeType, 3, (byte*)args, argSlots, ref error, basePos + 1);
					if (error.raised)
					{
						context.StackPosition -= 5;
						return;
					}


					arrPtr = RtArray.FindAndUpdateHeapInstancePtr(scope.ThisPtr.HeapPtr, context.player, out array);
					len = array.array_len;


					context.player.ConvertValueType(ref error, r, TypeKind.Boolean, context.BOOLEAN, ref r);
					Debug.Assert(!error.raised); //转BOOLEAN不会失败

					if (!r.Boolean)
					{
						context.StackSlots[returnSlotIndex].SetBoolean(false);
						return;
					}

				}

				context.StackSlots[returnSlotIndex].SetBoolean(true);

			}


			context.StackPosition -= 5;


		}


		[NativeFunction(".Array$:AS3::forEach")]
		public static void Array_forEach(
			Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex
			)
		{
			Array_Proto_forEach(context, method, scope_ptr, thisPtr, stackStPos, ref error, returnSlotIndex);
		}

		[NativeFunction(".Array$@::forEach")]
		public static void Array_Proto_forEach(
			Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex
			)
		{
			if (thisPtr.ValueType != NaNBoxing.BoxType.HeapPtr ||
				context.GC.Heap[thisPtr.HeapPtr].Kind != RtHeapTypeKind.ARRAY)
			{
				//context.player.RaiseTypeError(ref error, thisPtr, TypeKind.Array);
				context.StackSlots[returnSlotIndex].SetBoolean(false);
				return;
			}

			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];


			RtArray array;
			int arrPtr = RtArray.FindAndUpdateHeapInstancePtr(scope.ThisPtr.HeapPtr, context.player, out array);

			uint len = array.array_len;


			context.StackSlots[returnSlotIndex].SetHeapPtr(arrPtr, (byte)RtHeapTypeKind.ARRAY, (byte)HeapKindFlag.NONE); //保持到槽，防止GC

			if (context.StackPosition + 5 >= Context.STACK_LENGTH)
			{
				context.player.RaiseStackOverflow(ref error);
				return;
			}

			var cb = scope.ReadSlot(0, context.player);
			if (cb.ValueType != BoxType.HeapPtr)
			{
				context.player.RaiseTypeError(ref error, cb, TypeKind.Function);
				return;
			}

			var _this = scope.ReadSlot(1, context.player);

			var cbmethod = ((ASMethodBody)context.GC.Heap[cb.HeapPtr].Type).Method;
			var cbclosure = (RtClosure)context.GC.Heap[cb.HeapPtr];

			if (cbmethod.__ismethod && !cbmethod.__is_call_or_apply)
			{
				_this = cbclosure.This;
			}
			else if (cbmethod.__is_hasOwnProperty)
			{

			}
			else if ((_this.ValueType == NaNBoxing.BoxType.Undefined
				||
				_this.ValueType == NaNBoxing.BoxType.Null
				) //&& scope.__sendargcount==2
				)
			{

				var sss = context.GC.Heap[cb.HeapPtr].Type._link_codescope.Parent; //Context.GC.Heap[scope_ptr].Type._link_codescope.Parent;
				while (sss.Kind != CodeScopeKind.Script)
				{
					sss = sss.Parent;
				}

				var globalptr = ((ASScript)sss.Container).__global_index__;
				_this.SetHeapPtr(globalptr, (byte)RtHeapTypeKind.GLOBAL, (byte)HeapKindFlag.NONE);

			}



			int basePos = context.StackPosition;
			var argSlots = context.StackSlots.AsSpan(basePos, 5);
			argSlots.Clear();

			context.StackPosition += 5;

			unsafe
			{

				StackLocater* args = stackalloc StackLocater[3];
				args[0].index = 2;
				args[1].index = 3;
				args[2].index = 4;


				uint olen = len;
				for (uint i = 0; i < len && i < olen; i++)
				{
					bool isoutofindex;
					NaNBoxing v = array.ReadSlot(i, context.player, out isoutofindex);

					argSlots[2] = v;
					argSlots[3].SetInt((int)i);
					argSlots[4].SetHeapPtr(arrPtr, (byte)RtHeapTypeKind.ARRAY, (byte)HeapKindFlag.NONE);

					NaNBoxing r = context.player.RunMethod(cbmethod, _this, cbclosure.ScopePtr, cbclosure.ScopeType, 3, (byte*)args, argSlots, ref error, basePos + 1);
					if (error.raised)
					{
						context.StackPosition -= 5;
						return;
					}


					arrPtr = RtArray.FindAndUpdateHeapInstancePtr(scope.ThisPtr.HeapPtr, context.player, out array);
					len = array.array_len;


				}

				context.StackSlots[returnSlotIndex].SetUndefined();

			}


			context.StackPosition -= 5;


		}

		[NativeFunction(".Array$:AS3::filter")]
		public static void Array_filter(
			Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex
			)
		{
			Array_Proto_filter(context, method, scope_ptr, thisPtr, stackStPos, ref error, returnSlotIndex);
		}

		[NativeFunction(".Array$@::filter")]
		public static void Array_Proto_filter(
			Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex
			)
		{
			if (thisPtr.ValueType != NaNBoxing.BoxType.HeapPtr ||
				context.GC.Heap[thisPtr.HeapPtr].Kind != RtHeapTypeKind.ARRAY)
			{
				context.player.RaiseTypeError(ref error, thisPtr, TypeKind.Array);
				//context.StackSlots[returnSlotIndex].SetUndefined();
				return;
			}

			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];


			RtArray array;
			int arrPtr = RtArray.FindAndUpdateHeapInstancePtr(scope.ThisPtr.HeapPtr, context.player, out array);

			uint len = array.array_len;


			context.StackSlots[returnSlotIndex].SetHeapPtr(arrPtr, (byte)RtHeapTypeKind.ARRAY, (byte)HeapKindFlag.NONE); //保持到槽，防止GC

			if (context.StackPosition + 5 >= Context.STACK_LENGTH)
			{
				context.player.RaiseStackOverflow(ref error);
				return;
			}

			var cb = scope.ReadSlot(0, context.player);
			if (cb.ValueType != BoxType.HeapPtr)
			{
				context.player.RaiseTypeError(ref error, cb, TypeKind.Function);
				return;
			}

			var _this = scope.ReadSlot(1, context.player);
			//if (scope.__sendargcount < 2)
			//{
			//	_this.SetUndefined();
			//}


			var cbmethod = ((ASMethodBody)context.GC.Heap[cb.HeapPtr].Type).Method;
			var cbclosure = (RtClosure)context.GC.Heap[cb.HeapPtr];

			if (cbmethod.__ismethod && !cbmethod.__is_call_or_apply)
			{
				_this = cbclosure.This;
			}
			else if (cbmethod.__is_hasOwnProperty)
			{

			}
			else if ((_this.ValueType == NaNBoxing.BoxType.Undefined
				||
				_this.ValueType == NaNBoxing.BoxType.Null
				) //&& scope.__sendargcount==2
				)
			{

				var sss = context.GC.Heap[cb.HeapPtr].Type._link_codescope.Parent; //Context.GC.Heap[scope_ptr].Type._link_codescope.Parent;
				while (sss.Kind != CodeScopeKind.Script)
				{
					sss = sss.Parent;
				}

				var globalptr = ((ASScript)sss.Container).__global_index__;
				_this.SetHeapPtr(globalptr, (byte)RtHeapTypeKind.GLOBAL, (byte)HeapKindFlag.NONE);

			}

			int ptrIndex = returnSlotIndex;
			int result_instancePtr = context.CacheArrayPtr + ptrIndex;
			var result_instance = context.GC.Heap[result_instancePtr];
			result_instance.Type = context.ARRAY.Instance;

			var result = (RtArray)result_instance;

			result.array_len = 0;
			result.methodscopeslot_ref_state = 0;
			result.HEAPINSTANCE_PTR = 0;

			context.StackSlots[returnSlotIndex].SetHeapPtr(result_instancePtr, (byte)RtHeapTypeKind.ARRAY, (byte)HeapKindFlag.NONE);


			int basePos = context.StackPosition;
			var argSlots = context.StackSlots.AsSpan(basePos, 5);
			argSlots.Clear();

			context.StackPosition += 5;

			unsafe
			{

				StackLocater* args = stackalloc StackLocater[3];
				args[0].index = 2;
				args[1].index = 3;
				args[2].index = 4;


				uint olen = len;
				for (uint i = 0; i < len && i < olen; i++)
				{
					bool isoutofindex;
					NaNBoxing v = array.ReadSlot(i, context.player, out isoutofindex);

					argSlots[2] = v;
					argSlots[3].SetInt((int)i);
					argSlots[4].SetHeapPtr(arrPtr, (byte)RtHeapTypeKind.ARRAY, (byte)HeapKindFlag.NONE);

					NaNBoxing r = context.player.RunMethod(cbmethod, _this, cbclosure.ScopePtr, cbclosure.ScopeType, 3, (byte*)args, argSlots, ref error, basePos + 1);
					if (error.raised)
					{
						context.StackPosition = basePos;
						return;
					}


					arrPtr = RtArray.FindAndUpdateHeapInstancePtr(scope.ThisPtr.HeapPtr, context.player, out array);
					len = array.array_len;


					context.player.ConvertValueType(ref error, r, TypeKind.Boolean, context.BOOLEAN, ref r);
					Debug.Assert(!error.raised); //转BOOLEAN不会失败

					if (r.Boolean)
					{
						context.player.SetArraySlot(v, result.array_len, context.GC.Heap[result_instancePtr], ref error);
						//result.SetSlot(v, result.array_len, context.player, ref error);
						if (error.raised)
						{
							context.StackPosition = basePos;
							return;
						}

						result_instancePtr = RtArray.FindAndUpdateHeapInstancePtr(result_instancePtr, context.player, out result);

					}

				}
			}



			context.StackPosition = basePos;

			context.StackSlots[returnSlotIndex].SetHeapPtr(result_instancePtr, (byte)RtHeapTypeKind.ARRAY, (byte)HeapKindFlag.NONE);


		}



		[NativeFunction(".Array$:AS3::map")]
		public static void Array_map(
			Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex
			)
		{
			Array_Proto_map(context, method, scope_ptr, thisPtr, stackStPos, ref error, returnSlotIndex);
		}

		[NativeFunction(".Array$@::map")]
		public static void Array_Proto_map(
			Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex
			)
		{
			if (thisPtr.ValueType != NaNBoxing.BoxType.HeapPtr ||
				context.GC.Heap[thisPtr.HeapPtr].Kind != RtHeapTypeKind.ARRAY)
			{
				context.player.RaiseTypeError(ref error, thisPtr, TypeKind.Array);
				//context.StackSlots[returnSlotIndex].SetUndefined();
				return;
			}

			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];


			RtArray array;
			int arrPtr = RtArray.FindAndUpdateHeapInstancePtr(scope.ThisPtr.HeapPtr, context.player, out array);

			uint len = array.array_len;


			context.StackSlots[returnSlotIndex].SetHeapPtr(arrPtr, (byte)RtHeapTypeKind.ARRAY, (byte)HeapKindFlag.NONE); //保持到槽，防止GC

			if (context.StackPosition + 5 >= Context.STACK_LENGTH)
			{
				context.player.RaiseStackOverflow(ref error);
				return;
			}

			var cb = scope.ReadSlot(0, context.player);
			if (cb.ValueType != BoxType.HeapPtr)
			{
				context.player.RaiseTypeError(ref error, cb, TypeKind.Function);
				return;
			}

			var _this = scope.ReadSlot(1, context.player);
			//if (scope.__sendargcount < 2)
			//{
			//	_this.SetUndefined();
			//}


			var cbmethod = ((ASMethodBody)context.GC.Heap[cb.HeapPtr].Type).Method;
			var cbclosure = (RtClosure)context.GC.Heap[cb.HeapPtr];

			if (cbmethod.__ismethod && !cbmethod.__is_call_or_apply)
			{
				_this = cbclosure.This;
			}
			else if (cbmethod.__is_hasOwnProperty)
			{

			}
			else if ((_this.ValueType == NaNBoxing.BoxType.Undefined
				||
				_this.ValueType == NaNBoxing.BoxType.Null
				) //&& scope.__sendargcount==2
				)
			{

				var sss = context.GC.Heap[cb.HeapPtr].Type._link_codescope.Parent; //Context.GC.Heap[scope_ptr].Type._link_codescope.Parent;
				while (sss.Kind != CodeScopeKind.Script)
				{
					sss = sss.Parent;
				}

				var globalptr = ((ASScript)sss.Container).__global_index__;
				_this.SetHeapPtr(globalptr, (byte)RtHeapTypeKind.GLOBAL, (byte)HeapKindFlag.NONE);

			}

			int ptrIndex = returnSlotIndex;
			int result_instancePtr = context.CacheArrayPtr + ptrIndex;
			var result_instance = context.GC.Heap[result_instancePtr];
			result_instance.Type = context.ARRAY.Instance;

			var result = (RtArray)result_instance;

			result.array_len = 0;
			result.methodscopeslot_ref_state = 0;
			result.HEAPINSTANCE_PTR = 0;

			context.StackSlots[returnSlotIndex].SetHeapPtr(result_instancePtr, (byte)RtHeapTypeKind.ARRAY, (byte)HeapKindFlag.NONE);


			int basePos = context.StackPosition;
			var argSlots = context.StackSlots.AsSpan(basePos, 5);
			argSlots.Clear();

			context.StackPosition += 5;

			unsafe
			{

				StackLocater* args = stackalloc StackLocater[3];
				args[0].index = 2;
				args[1].index = 3;
				args[2].index = 4;


				uint olen = len;
				for (uint i = 0; i < len && i < olen; i++)
				{
					bool isoutofindex;
					NaNBoxing v = array.ReadSlot(i, context.player, out isoutofindex);

					argSlots[2] = v;
					argSlots[3].SetInt((int)i);
					argSlots[4].SetHeapPtr(arrPtr, (byte)RtHeapTypeKind.ARRAY, (byte)HeapKindFlag.NONE);

					NaNBoxing r = context.player.RunMethod(cbmethod, _this, cbclosure.ScopePtr, cbclosure.ScopeType, 3, (byte*)args, argSlots, ref error, basePos + 1);
					if (error.raised)
					{
						context.StackPosition = basePos;
						return;
					}


					arrPtr = RtArray.FindAndUpdateHeapInstancePtr(scope.ThisPtr.HeapPtr, context.player, out array);
					len = array.array_len;

					context.player.SetArraySlot(r, result.array_len, context.GC.Heap[result_instancePtr], ref error);
					//result.SetSlot(v, result.array_len, context.player, ref error);
					if (error.raised)
					{
						context.StackPosition = basePos;
						return;
					}

					result_instancePtr = RtArray.FindAndUpdateHeapInstancePtr(result_instancePtr, context.player, out result);

				}
			}

			context.StackPosition = basePos;

			context.StackSlots[returnSlotIndex].SetHeapPtr(result_instancePtr, (byte)RtHeapTypeKind.ARRAY, (byte)HeapKindFlag.NONE);

		}

		[NativeFunction(".Array$:AS3::indexOf")]
		public static void Array_indexOf(
			Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex
			)
		{
			Array_Proto_indexOf(context, method, scope_ptr, thisPtr, stackStPos, ref error, returnSlotIndex);
		}

		[NativeFunction(".Array$@::indexOf")]
		public static void Array_Proto_indexOf(
			Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex
			)
		{
			if (thisPtr.ValueType != NaNBoxing.BoxType.HeapPtr ||
				context.GC.Heap[thisPtr.HeapPtr].Kind != RtHeapTypeKind.ARRAY)
			{
				context.player.RaiseTypeError(ref error, thisPtr, TypeKind.Array);
				return;
			}

			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			RtArray array;
			int arrPtr = RtArray.FindAndUpdateHeapInstancePtr(scope.ThisPtr.HeapPtr, context.player, out array);

			uint len = array.array_len;

			NaNBoxing searchElement = scope.ReadSlot(0, context.player);
			var fromIndexVal = scope.ReadSlot(1, context.player);
			Debug.Assert(fromIndexVal.ValueType == BoxType.Uint);

			uint fromIndex = fromIndexVal.UIntValue;


			if (fromIndex >= len)
			{
				context.StackSlots[returnSlotIndex].SetInt(-1);
				return;
			}

			for (uint i = fromIndex; i < len; i++)
			{
				bool isoutofindex;
				NaNBoxing element = array.ReadSlot(i, context.player, out isoutofindex);
				if (context.player.IsStrictlyEqual(element, searchElement))
				{
					context.StackSlots[returnSlotIndex].SetInt((int)i);
					return;
				}
			}

			context.StackSlots[returnSlotIndex].SetInt(-1);
		}


		[NativeFunction(".Array$:AS3::lastIndexOf")]
		public static void Array_lastIndexOf(
			Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex
			)
		{
			Array_Proto_lastIndexOf(context, method, scope_ptr, thisPtr, stackStPos, ref error, returnSlotIndex);
		}

		[NativeFunction(".Array$@::lastIndexOf")]
		public static void Array_Proto_lastIndexOf(
			Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex
			)
		{
			if (thisPtr.ValueType != NaNBoxing.BoxType.HeapPtr ||
				context.GC.Heap[thisPtr.HeapPtr].Kind != RtHeapTypeKind.ARRAY)
			{
				context.player.RaiseTypeError(ref error, thisPtr, TypeKind.Array);
				return;
			}

			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			RtArray array;
			int arrPtr = RtArray.FindAndUpdateHeapInstancePtr(scope.ThisPtr.HeapPtr, context.player, out array);

			uint len = array.array_len;

			if (len == 0)
			{
				context.StackSlots[returnSlotIndex].SetInt(-1);
				return;
			}

			NaNBoxing searchElement = scope.ReadSlot(0, context.player);
			var fromIndexVal = scope.ReadSlot(1, context.player);

			Debug.Assert(fromIndexVal.ValueType == BoxType.Int);

			int fromIndex = fromIndexVal.IntValue;


			int startIndex;
			if (fromIndex < 0)
			{
				startIndex = (int)len + fromIndex;
				if (startIndex < 0)
				{
					startIndex = -1;
				}
			}
			else if (fromIndex >= (int)len)
			{
				startIndex = (int)len - 1;
			}
			else
			{
				startIndex = fromIndex;
			}

			for (int i = startIndex; i >= 0; i--)
			{
				bool isoutofindex;
				NaNBoxing element = array.ReadSlot((uint)i, context.player, out isoutofindex);
				if (context.player.IsStrictlyEqual(element, searchElement))
				{
					context.StackSlots[returnSlotIndex].SetInt(i);
					return;
				}
			}

			context.StackSlots[returnSlotIndex].SetInt(-1);
		}

		[NativeFunction(".Array$:AS3::slice")]
		public static void Array_slice(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex
			)
		{
			Array_Proto_slice(context, method, scope_ptr, thisPtr, stackStPos, ref error, returnSlotIndex);
		}
		[NativeFunction(".Array$@::slice")]
		public static void Array_Proto_slice(
			Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex
			)
		{
			// 1. Validate thisPtr is an Array
			if (thisPtr.ValueType != NaNBoxing.BoxType.HeapPtr ||
				context.GC.Heap[thisPtr.HeapPtr].Kind != RtHeapTypeKind.ARRAY)
			{
				context.player.RaiseTypeError(ref error, thisPtr, TypeKind.Array);
				return;
			}
			RtArray array;
			int arrPtr = RtArray.FindAndUpdateHeapInstancePtr(thisPtr.HeapPtr, context.player, out array);
			uint len = array.array_len;
			// 2. Read parameters (defaults: A=0, B=16777215 are passed by RunMethod)
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
			var startVal = scope.ReadSlot(0, context.player);
			var endVal = scope.ReadSlot(1, context.player);
			// 3. Normalize start (handle negative: count from end)
			int start = startVal.IntValue;
			if (start < 0) start = (int)len + start;
			if (start < 0) start = 0;
			if (start > (int)len) start = (int)len;
			// 4. Normalize end (16777215 means "until end", negative means count from end)
			int end = endVal.IntValue;
			if (end == 16777215)
			{
				end = (int)len;
			}
			else if (end < 0)
			{
				end = (int)len + end;
				if (end < 0) end = 0;
			}
			else if (end > (int)len)
			{
				end = (int)len;
			}
			// 5. Create new array for result
			int ptrIndex = returnSlotIndex;
			int result_instancePtr = context.CacheArrayPtr + ptrIndex;
			var result_instance = context.GC.Heap[result_instancePtr];
			result_instance.Type = context.ARRAY.Instance;
			var result = (RtArray)result_instance;
			result.array_len = 0;
			result.methodscopeslot_ref_state = 0;
			result.HEAPINSTANCE_PTR = 0;
			context.StackSlots[returnSlotIndex].SetHeapPtr(result_instancePtr, (byte)RtHeapTypeKind.ARRAY, (byte)HeapKindFlag.NONE);
			// 6. Copy elements from start to end
			for (int i = start; i < end && i < len; i++)
			{
				bool ishole;
				NaNBoxing v = array.ReadSlot((uint)i, context.player, out ishole);
				if (!ishole)
				{
					context.player.SetArraySlot(v, result.array_len, context.GC.Heap[result_instancePtr], ref error);
					if (error.raised) return;
				}
				else
				{
					result.SetLength(result.array_len + 1, context.player, ref error);
					if (error.raised) return;
				}
				result_instancePtr = RtArray.FindAndUpdateHeapInstancePtr(result_instancePtr, context.player, out result);
			}
			context.StackSlots[returnSlotIndex].SetHeapPtr(result_instancePtr, (byte)RtHeapTypeKind.ARRAY, (byte)HeapKindFlag.NONE);
		}


		[NativeFunction(".Array$:AS3::splice")]
		public static void Array_splice(Context context,
			ASMethod method, int scope_ptr,
			NaNBoxing thisPtr, int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			Array_Proto_splice(context, method, scope_ptr, thisPtr, stackStPos, ref error, returnSlotIndex);
		}

		[NativeFunction(".Array$@::splice")]
		public static void Array_Proto_splice(
			Context context, ASMethod method, int scope_ptr,
			NaNBoxing thisPtr, int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			// 1. 校验 this 是 Array
			if (thisPtr.ValueType != NaNBoxing.BoxType.HeapPtr ||
				context.GC.Heap[thisPtr.HeapPtr].Kind != RtHeapTypeKind.ARRAY)
			{
				context.player.RaiseTypeError(ref error, thisPtr, TypeKind.Array);
				return;
			}
			// 2. 获取原数组和长度
			RtArray array;
			int arrPtr = RtArray.FindAndUpdateHeapInstancePtr(thisPtr.HeapPtr, context.player, out array);
			uint len = array.array_len;
			// 3. 读取参数（RunMethod 已传入默认值）
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
			var startVal = scope.ReadSlot(0, context.player);   // int: startIndex
			var deleteCountVal = scope.ReadSlot(1, context.player); // uint: deleteCount
			var values = scope.ReadSlot(2, context.player);  // Array: ...values

			// 4. 标准化 startIndex（负数从末尾计数）
			int start = startVal.IntValue;
			if (start < 0) start = (int)len + start;
			if (start < 0) start = 0;
			if (start > (int)len) start = (int)len;




			// 5. 计算实际删除数量
			uint deleteCount;

			uint requested = deleteCountVal.UIntValue;
			uint available = (start >= len) ? 0 : len - (uint)start;
			deleteCount = Math.Min(requested, available);



			// 检查是否会越界
			var values_array = (RtArray)context.GC.Heap[values.HeapPtr];
			var values_span = values_array.stack_store.Span;
			// 注意：values 的存储方式是 cache_on_stack

			Debug.Assert((long)len - deleteCount + values_span.Length >= 0);

			if ((long)len - deleteCount + values_span.Length > uint.MaxValue)
			{
				context.player.RaiseRangeError(ref error, ((long)len - deleteCount + values_span.Length).ToString(), uint.MaxValue);
				context.StackSlots[returnSlotIndex].SetUndefined();
				return;
			}




			// 6. 创建结果数组（存放被删除的元素）
			int result_instancePtr = context.CacheArrayPtr + returnSlotIndex;
			var result_instance = context.GC.Heap[result_instancePtr];
			result_instance.Type = context.ARRAY.Instance;
			var result = (RtArray)result_instance;
			result.array_len = 0;
			result.HEAPINSTANCE_PTR = 0;
			result.methodscopeslot_ref_state = 0;
			context.StackSlots[returnSlotIndex].SetHeapPtr(result_instancePtr, (byte)RtHeapTypeKind.ARRAY, (byte)HeapKindFlag.NONE); //先存上，防止GC!很重要



			// 7. 复制被删除的元素到结果数组
			for (uint i = (uint)start; i < (uint)start + deleteCount && i < len; i++)
			{
				bool ishole;
				NaNBoxing v = array.ReadSlot(i, context.player, out ishole);
				if (!ishole)
				{
					context.player.SetArraySlot(v, result.array_len, context.GC.Heap[result_instancePtr], ref error);
					if (error.raised) return;
				}
				else
				{
					result.SetLength(result.array_len + 1, context.player, ref error);
					if (error.raised) return;
				}

				result_instancePtr = RtArray.FindAndUpdateHeapInstancePtr(result_instancePtr, context.player, out result);
			}


			// 9. 调用 DoSplice 执行实际修改
			array.DoSplice(context, ref error, start, deleteCount, (long)values_span.Length - deleteCount);
			if (error.raised) return;



			// 10,统一复制插入的数组

			arrPtr = RtArray.FindAndUpdateHeapInstancePtr(thisPtr.HeapPtr, context.player, out array);
			var instance = context.GC.Heap[arrPtr];

			for (int i = 0; i < values_span.Length; i++)
			{
				context.player.SetArraySlot(values_span[i], (uint)(start + i), instance, ref error);
				if (error.raised)
				{
					return;
				}
			}
			// 11. 返回结果数组
			context.StackSlots[returnSlotIndex].SetHeapPtr(result_instancePtr, (byte)RtHeapTypeKind.ARRAY, (byte)HeapKindFlag.NONE);
		}


		//.Array$:AS3::insertAt
		[NativeFunction(".Array$:AS3::insertAt")]
		public static void Array_insertAt(Context context,
			ASMethod method, int scope_ptr,
			NaNBoxing thisPtr, int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			// 2. 获取原数组和长度
			RtArray array;
			int arrPtr = RtArray.FindAndUpdateHeapInstancePtr(thisPtr.HeapPtr, context.player, out array);
			uint len = array.array_len;
			// 3. 读取参数（RunMethod 已传入默认值）
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
			var startVal = scope.ReadSlot(0, context.player);   // int: startIndex

			var element = scope.ReadSlot(1, context.player);

			// 4. 标准化 startIndex（负数从末尾计数）
			int start = startVal.IntValue;
			if (start < 0) start = (int)len + start;
			if (start < 0) start = 0;
			if (start > (int)len) start = (int)len;


			// 5. 调用 DoSplice 调整存储
			array.DoSplice(context, ref error, start, 0, 1);
			if (error.raised) return;

			// 6. 插入
			context.player.SetArraySlot(element, (uint)(start), context.GC.Heap[arrPtr], ref error);
			if (error.raised)
			{
				return;
			}

			context.StackSlots[returnSlotIndex].SetUndefined();

		}



		//removeAt
		[NativeFunction(".Array$:AS3::removeAt")]
		public static void Array_removeAt(Context context,
			ASMethod method, int scope_ptr,
			NaNBoxing thisPtr, int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			RtArray array;
			int arrPtr = RtArray.FindAndUpdateHeapInstancePtr(thisPtr.HeapPtr, context.player, out array);
			uint len = array.array_len;
			// 3. 读取参数（RunMethod 已传入默认值）
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
			var indexVal = scope.ReadSlot(0, context.player);   // int: startIndex

			// 4. 标准化 startIndex（负数从末尾计数）
			int index = indexVal.IntValue;
			if (index < 0) index = (int)len + index;
			if (index < 0)
			{
				context.StackSlots[returnSlotIndex].SetUndefined();
				return;
			}


			if (index >= len)
			{
				context.StackSlots[returnSlotIndex].SetUndefined();
				return;
			}


			RtHeapBase instance = context.GC.Heap[arrPtr];

			bool isoutindex;
			NaNBoxing e = array.ReadSlot((uint)index, context.player, out isoutindex);
			context.StackSlots[returnSlotIndex] = e;

			if (e.ValueType == BoxType.HeapPtr && e.HeapKind == (byte)RtHeapTypeKind.INSTANCE)
			{
				var check = context.GC.Heap[e.HeapPtr];
				if (((ASInstance)check.Type).Flags.HasFlag(ClassFlags.Struct))
				{
					int clonedptr = returnSlotIndex + context.CacheInstancePtr;
					var cacheObj = context.GC.Heap[clonedptr];
					cacheObj.Type = check.Type;

					((RtInstance)cacheObj).methodscopeslot_ref_state = 0;
					((RtInstance)cacheObj).HEAPINSTANCE_PTR = 0;
					((RtInstance)cacheObj).CopyFrom(check, context.player, check.Type._link_codescope.TypeLayout.Size);

					context.StackSlots[returnSlotIndex].SetHeapPtr(clonedptr, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.FLAG_STRUCT);
				}
			}

			array.DoSplice(context, ref error, index, 1, -1);
			if (error.raised)
			{
				context.StackSlots[returnSlotIndex].SetUndefined();
				return;
			}

		}


		//sort
		[NativeFunction(".Array$:AS3::sort")]
		public static void Array_sort(Context context,
			ASMethod method, int scope_ptr,
			NaNBoxing thisPtr, int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			Array_Proto_sort(context, method, scope_ptr, thisPtr, stackStPos, ref error, returnSlotIndex);
		}

		[NativeFunction(".Array$@::sort")]
		public static void Array_Proto_sort(Context context,
			ASMethod method, int scope_ptr,
			NaNBoxing thisPtr, int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			// 1. 校验 this 是 Array
			if (thisPtr.ValueType != NaNBoxing.BoxType.HeapPtr ||
				context.GC.Heap[thisPtr.HeapPtr].Kind != RtHeapTypeKind.ARRAY)
			{
				context.player.RaiseTypeError(ref error, thisPtr, TypeKind.Array);
				return;
			}

			// 2. 获取原数组和长度
			RtArray array;
			int arrPtr = RtArray.FindAndUpdateHeapInstancePtr(thisPtr.HeapPtr, context.player, out array);
			uint len = array.array_len;

			// 3. 读取参数（RunMethod 已传入默认值）
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
			var rest = scope.ReadSlot(0, context.player);
			var restArray = (RtArray)context.GC.Heap[rest.HeapPtr];
			var restSpan = restArray.stack_store.Span;


			if (restSpan.Length == 0)
			{
				/*
				 排序区分大小写（Z 优先于 a）。
				按升序排序（a 优先于 b）。
				修改该数组以反映排序顺序；在排序后的数组中不按任何特定顺序连续放置具有相同排序字段的多个元素。
				元素无论属于何种数据类型，都作为字符串进行排序，所以 100 在 99 之前，这是因为 "1" 的字符串值小于 "9" 的字符串值。
				 */
				NaNBoxing behavior = default; behavior.SetInt(0);
				SortHelper.QuickSort(scope, scope_ptr, context, ref error, behavior);

				if (error.raised)
				{
					return;
				}

				context.StackSlots[returnSlotIndex].SetHeapPtr(arrPtr, (byte)RtHeapTypeKind.ARRAY, (byte)HeapKindFlag.NONE);

			}
			else
			{
				NaNBoxing sortBehavior = restSpan[0];

				if (sortBehavior.ValueType == BoxType.LocalString || sortBehavior.ValueType == BoxType.Null || sortBehavior.ValueType == BoxType.Undefined)
				{
					context.player.RaiseTypeError(ref error, sortBehavior, TypeKind.Function);
					return;
				}

				int basePos = context.StackPosition;
				context.StackPosition += 1;

				context.StackSlots[basePos] = sortBehavior;

				if (sortBehavior.ValueType == BoxType.HeapPtr)
				{
					context.player.ConvertValueType(ref error, sortBehavior, TypeKind.Function, context.FUNCTION, ref context.StackSlots[basePos]);
					if (error.raised)
					{
						context.StackPosition = basePos;
						return;
					}
				}

				SortHelper.QuickSort(scope, scope_ptr, context, ref error, context.StackSlots[basePos]);

				context.StackPosition = basePos;

				if (error.raised)
				{
					return;
				}

				arrPtr = RtArray.FindAndUpdateHeapInstancePtr(scope.ThisPtr.HeapPtr, context.player, out array);
				context.StackSlots[returnSlotIndex].SetHeapPtr(arrPtr, (byte)RtHeapTypeKind.ARRAY, (byte)HeapKindFlag.NONE);


			}



		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private static int comparer(NaNBoxing a, NaNBoxing b, NaNBoxing sortBehavior, Context context, int scope_ptr, ref ReceiveError error)
		{

			//确保空槽不会传入。
			if (a.ValueType == BoxType.Fault && b.ValueType == BoxType.Fault)
			{
				return 0;
			}
			else

			if (a.ValueType == BoxType.Fault && b.ValueType != BoxType.Fault)
			{
				return 1;
			}
			else if (a.ValueType != BoxType.Fault && b.ValueType == BoxType.Fault)
			{
				return -1;
			}


			if (sortBehavior.ValueType == BoxType.HeapPtr)
			{
				if (a.ValueType == BoxType.Undefined && b.ValueType != BoxType.Undefined)
				{
					return 1;
				}
				else if (a.ValueType != BoxType.Undefined && b.ValueType == BoxType.Undefined)
				{
					return -1;
				}

				RtHeapBase func = context.GC.Heap[sortBehavior.HeapPtr];
				RtClosure closure = (RtClosure)func;

				ASMethod method = ((ASMethodBody)func.Type).Method;

				if (context.StackPosition + 2 >= Context.STACK_LENGTH)
				{
					context.player.RaiseStackOverflow(ref error);
					return 0;
				}


				unsafe
				{
					StackLocater* args = stackalloc StackLocater[2];
					args->index = 0;
					(args + 1)->index = 1;

					var slots = context.StackSlots.AsSpan(context.StackPosition, 2);
					slots.Clear();

					slots[0] = a;
					slots[1] = b;

					int basePos = context.StackPosition;

					context.StackPosition += 2;

					context.player.RunMethod(method, closure.This, closure.ScopePtr, closure.ScopeType, 2, (byte*)args, slots, ref error, basePos);

					if (error.raised)
					{
						context.StackPosition = basePos;
						return 0;
					}

					context.player.ConvertValueType(ref error, context.StackSlots[basePos], TypeKind.Number, context.NUMBER, ref slots[0], scope_ptr);
					context.StackPosition = basePos;

					double v = slots[0].Number;
					if (v > 0)
						return 1;
					else if (v == 0 || double.IsNaN(v))
						return 0;
					else
						return -1;

				}

			}
			else
			{



				context.player.ConvertValueType(ref error, sortBehavior, TypeKind.Int, context.INT, ref sortBehavior); //这里不可能出错
				int option = sortBehavior.IntValue;

				if ((option & 16) == 16)
				{
					if (a.ValueType == BoxType.Undefined)
					{
						context.player.RaiseTypeError(ref error, a, TypeKind.Number);
						return 0;
					}

					//转数字
					context.StackPosition++;
					context.StackSlots[context.StackPosition - 1].SetUndefined();
					context.player.ConvertValueType(ref error, a, TypeKind.Number, context.NUMBER, ref context.StackSlots[context.StackPosition - 1], scope_ptr);
					if (error.raised)
					{
						context.StackPosition--;
						return 0;
					}

					double v1 = context.StackSlots[context.StackPosition - 1].Number;

					if (b.ValueType == BoxType.Undefined)
					{
						context.player.RaiseTypeError(ref error, b, TypeKind.Number);
						return 0;
					}

					context.player.ConvertValueType(ref error, b, TypeKind.Number, context.NUMBER, ref context.StackSlots[context.StackPosition - 1], scope_ptr);
					if (error.raised)
					{
						context.StackPosition--;
						return 0;
					}

					double v2 = context.StackSlots[context.StackPosition - 1].Number;
					if (error.raised)
					{
						context.StackPosition--;
						return 0;
					}

					context.StackPosition--;

					if (double.IsNaN(v1) && double.IsNaN(v2))
					{
						return 0;
					}
					else if (double.IsNaN(v1))
					{
						return 1;
					}
					else if (double.IsNaN(v2))
					{
						return -1;
					}
					else if ((option & 2) == 2)
					{
						if (v1 == v2)
							return 0;
						else if (v1 < v2)
							return 1;
						else
							return -1;
					}
					else
					{
						if (v1 == v2)
							return 0;
						else if (v1 > v2)
							return 1;
						else
							return -1;
					}
				}
				else
				{
					if (a.ValueType == BoxType.Undefined && b.ValueType != BoxType.Undefined)
					{
						return 1;
					}
					else if (a.ValueType != BoxType.Undefined && b.ValueType == BoxType.Undefined)
					{
						return -1;
					}

					//字符串比较


					context.StackSlots[context.StackPosition].SetUndefined();
					context.StackSlots[context.StackPosition + 1].SetUndefined();

					context.StackPosition += 2;
					context.GC.CheckGC(ref error);

					context.player.ConvertValueType(ref error, a, TypeKind.String, context.STRING, ref context.StackSlots[context.StackPosition - 2], scope_ptr);
					if (error.raised)
					{
						context.StackPosition -= 2;
						return 0;
					}

					context.player.ConvertValueType(ref error, b, TypeKind.String, context.STRING, ref context.StackSlots[context.StackPosition - 1], scope_ptr);
					if (error.raised)
					{
						context.StackPosition -= 2;
						return 0;
					}

					//unsafe
					{


						Span<char> temp1 = stackalloc char[16];
						ReadOnlySpan<char> chars1 = temp1;
						NaNBoxing box1 = context.StackSlots[context.StackPosition - 2];
						if (box1.ValueType == BoxType.HeapPtr)
						{
							string v1 = ((RtString)context.GC.Heap[box1.HeapPtr]).Str;
							chars1 = v1.AsSpan();
						}
						else
						{
							//Debug.Assert(box1.ValueType == BoxType.LocalString);
							//int len = box1.GetLocalStringChars(temp1);
							//chars1 = temp1.Slice(0, len);

							chars1 = Extensions.GetPrimitiveValueToString(context.player, box1, temp1);

						}



						Span<char> temp2 = stackalloc char[16];
						ReadOnlySpan<char> chars2 = temp2;
						ref NaNBoxing box2 = ref context.StackSlots[context.StackPosition - 1];
						if (box2.ValueType == BoxType.HeapPtr)
						{
							string v = ((RtString)context.GC.Heap[box2.HeapPtr]).Str;
							chars2 = v.AsSpan();
						}
						else
						{
							//Debug.Assert(box2.ValueType == BoxType.LocalString);
							//int len = box2.GetLocalStringChars(temp2);
							//chars2 = temp2.Slice(0, len);


							chars2 = Extensions.GetPrimitiveValueToString(context.player, box2, temp2);

						}


						context.StackPosition -= 2;

						int comp = chars1.CompareTo(chars2, (option & 1) == 1 ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal); //.Compare(v1, v2, (option & 1) == 1);
						if ((option & 2) == 2)
							return -comp;
						else
							return comp;

					}
				}


				//throw new NotImplementedException();
			}


		}


		static class SortHelper
		{
			class SortException : Exception
			{
				public ReceiveError raisedErr;
			}


			public static void QuickSort(RtMethodScope scope, int scope_ptr, Context context, ref ReceiveError error, NaNBoxing sortBehavior)
			{

				RtArray vpayload;
				int vecPtr = RtArray.FindAndUpdateHeapInstancePtr(scope.ThisPtr.HeapPtr, context.player, out vpayload);

				if (vpayload.array_len == 0)
					return;

				if (context.StackPosition + 2 >= Context.STACK_LENGTH)
				{
					context.player.RaiseStackOverflow(ref error);
					return;
				}

				context.StackSlots[context.StackPosition].SetUndefined();
				context.StackSlots[context.StackPosition + 1].SetUndefined();

				if (vpayload.array_len <= 1024 * 8 * 64 && vpayload.StoreMode == RtArray.ArrayStoreMode.normal)
				{
					//Sort 把数组中的所有元素拷到原生数组里排序 // 只考虑normal,其他两种反正数量不多，而且还不用管sturct缓存，normal里的对象肯定在堆里，省的麻烦。
					int oLen = (int)vpayload.array_len;

					if (sortBehavior.ValueType == BoxType.HeapPtr) //自定义排序
					{
						NaNBoxing[] values = ArrayPool<NaNBoxing>.Shared.Rent(oLen);
						
						context.GC.PushTemporyHolder(values,oLen);

						for (int i = 0; i < oLen; i++)
						{
							NaNBoxing v = vpayload.ReadSlot((uint)i, context.player, out bool ishole);
							if (ishole)
							{
								values[i].setFault();
							}
							else
							{
								values[i] = v;
							}
						}

						bool needstop = false;
						ReceiveError stopErr = default;

						values.AsSpan().Slice(0, oLen).Sort(
						(a, b) => {

							if (needstop)
								return 0;

							ReceiveError e = default;
							int c = comparer(a, b, sortBehavior, context, scope_ptr, ref e);
							if (e.raised)
							{
								stopErr = e;
								needstop = true;
							}

							

							return c;
						}
						);

						if (needstop)
						{
							error = stopErr;
							goto lbl_clean;
						}

						vecPtr = RtArray.FindAndUpdateHeapInstancePtr(scope.ThisPtr.HeapPtr, context.player, out vpayload);
						if (vpayload.array_len != oLen)
						{
							context.player.RaiseError(ref error, "array length changed!");
							goto lbl_clean;
						}

						//将值复制回去。
						vpayload.CopyFromArray(values.AsSpan().Slice(0,oLen),context.player,ref error);

					lbl_clean:
						var th = context.GC.PopTemporyHolder();
						Debug.Assert(th == values);
						ArrayPool<NaNBoxing>.Shared.Return(values);

					}
					else
					{
						context.player.ConvertValueType(ref error, sortBehavior, TypeKind.Int, context.INT, ref sortBehavior); //这里不可能出错

						int option = sortBehavior.IntValue;

						NaNBoxing[] values = ArrayPool<NaNBoxing>.Shared.Rent(oLen);
						
						context.GC.PushTemporyHolder(values,oLen);

						int[] ind = ArrayPool<int>.Shared.Rent(oLen);
						for (int i = 0; i < oLen; i++)
						{
							ind[i] = i;
						}
						int[] inv = ArrayPool<int>.Shared.Rent(oLen);


						for (int i = 0; i < oLen; i++)
						{

							NaNBoxing v = vpayload.ReadSlot((uint)i, context.player, out bool ishole);
							if (ishole)
							{
								values[i].setFault();
							}
							else
							{
								if ((option & 16) == 16)
								{
									if (v.ValueType == BoxType.Undefined)
									{
										context.player.RaiseTypeError(ref error, v, TypeKind.Number);
										goto lbl_clean;
									}

									//转数字								
									context.StackSlots[context.StackPosition].SetUndefined();
									context.player.ConvertValueType(ref error, v, TypeKind.Number, context.NUMBER, ref context.StackSlots[context.StackPosition], scope_ptr);
									if (error.raised)
									{
										goto lbl_clean;
									}

									values[i] = context.StackSlots[context.StackPosition];

								}
								else
								{
									//转字符串
									context.StackSlots[context.StackPosition].SetUndefined();

									if (v.ValueType != BoxType.Undefined)
									{
										context.GC.CheckGC(ref error);
										context.player.ConvertValueType(ref error, v, TypeKind.String, context.STRING, ref context.StackSlots[context.StackPosition], scope_ptr);
										if (error.raised)
										{
											goto lbl_clean;
										}

										values[i] = context.StackSlots[context.StackPosition];
									}
									else
									{
										values[i].SetUndefined();
									}
								}


								vecPtr = RtArray.FindAndUpdateHeapInstancePtr(scope.ThisPtr.HeapPtr, context.player, out vpayload);
								if (vpayload.array_len != oLen)
								{
									context.player.RaiseError(ref error, "array length changed!");
									goto lbl_clean;
								}
							}
						}

						ind.AsSpan().Slice(0, oLen).Sort(
							(l, r) =>
							{
								var a = values[l];
								var b = values[r];

								//确保空槽不会传入。
								if (a.ValueType == BoxType.Fault && b.ValueType == BoxType.Fault)
								{
									return 0;
								}
								else

								if (a.ValueType == BoxType.Fault && b.ValueType != BoxType.Fault)
								{
									return 1;
								}
								else if (a.ValueType != BoxType.Fault && b.ValueType == BoxType.Fault)
								{
									return -1;
								}

								if ((option & 16) == 16)
								{
									double v1 = a.Number;
									double v2 = b.Number;

									if (double.IsNaN(v1) && double.IsNaN(v2))
									{
										return 0;
									}
									else if (double.IsNaN(v1))
									{
										return 1;
									}
									else if (double.IsNaN(v2))
									{
										return -1;
									}
									else if ((option & 2) == 2)
									{
										if (v1 == v2)
											return 0;
										else if (v1 < v2)
											return 1;
										else
											return -1;
									}
									else
									{
										if (v1 == v2)
											return 0;
										else if (v1 > v2)
											return 1;
										else
											return -1;
									}

								}
								else
								{
									if (a.ValueType == BoxType.Undefined && b.ValueType != BoxType.Undefined)
									{
										return 1;
									}
									else if (a.ValueType != BoxType.Undefined && b.ValueType == BoxType.Undefined)
									{
										return -1;
									}

									Span<char> temp1 = stackalloc char[16];
									ReadOnlySpan<char> chars1 = temp1;
									NaNBoxing box1 = a;
									if (box1.ValueType == BoxType.HeapPtr)
									{
										string v1 = ((RtString)context.GC.Heap[box1.HeapPtr]).Str;
										chars1 = v1.AsSpan();
									}
									else
									{

										chars1 = Extensions.GetPrimitiveValueToString(context.player, box1, temp1);

									}



									Span<char> temp2 = stackalloc char[16];
									ReadOnlySpan<char> chars2 = temp2;
									NaNBoxing box2 = b;
									if (box2.ValueType == BoxType.HeapPtr)
									{
										string v = ((RtString)context.GC.Heap[box2.HeapPtr]).Str;
										chars2 = v.AsSpan();
									}
									else
									{

										chars2 = Extensions.GetPrimitiveValueToString(context.player, box2, temp2);

									}

									int comp = chars1.CompareTo(chars2, (option & 1) == 1 ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal); //.Compare(v1, v2, (option & 1) == 1);
									if ((option & 2) == 2)
										return -comp;
									else
										return comp;
								}


							}
							);

						//最终排序
						for (int i = 0; i < oLen; i++)
							inv[ind[i]] = i;

						for (int i = 0; i < oLen; i++)
						{
							while (inv[i] != i)
							{
								int target = inv[i];

								// swap arr[i] <-> arr[target]
								//(arr[i], arr[target]) = (arr[target], arr[i]);
								vpayload.Swap((uint)i, (uint)target, context, ref error, context.StackPosition);
								if (error.raised)
								{
									goto lbl_clean;
								}
								// swap idx[i] <-> idx[target]
								(inv[i], inv[target]) = (inv[target], inv[i]);
							}
						}

					lbl_clean:
						{
							var th = context.GC.PopTemporyHolder();
							Debug.Assert(th == values);
							ArrayPool<NaNBoxing>.Shared.Return(values);
							ArrayPool<int>.Shared.Return(ind);
							ArrayPool<int>.Shared.Return(inv);
						}
					}

				}
				else
				{

					context.StackPosition++;

					QuickSort(scope, ref vpayload, vecPtr, scope_ptr, 0, vpayload.array_len - 1, context, ref error, sortBehavior, context.StackPosition - 1);

					context.StackPosition--;
				}
			}

			
			
			private static void QuickSort(RtMethodScope scope, ref RtArray vpayload, int vecptr, int scope_ptr, long left, long right, Context context, ref ReceiveError error, NaNBoxing sortBehavior, int tempslot)
			{
				if (left >= right) return;

				if (right - left < 16)
				{
					var olen = vpayload.array_len;
					long i, j;
					for (i = left + 1; i <= right; i++)
					{
						//key = arr[i];  // 当前待排序元素
						NaNBoxing key = vpayload.ReadSlot((uint)i, context.player, out bool ishole_p);
						context.StackSlots[tempslot] = key;
						if (key.ValueType == BoxType.HeapPtr && key.HeapKind == (byte)RtHeapTypeKind.INSTANCE)
						{
							RtInstance src = (RtInstance)context.GC.Heap[key.HeapPtr];
							if (((ASInstance)src.Type).Flags.HasFlag(ClassFlags.Struct))
							{
								int clonedptr = tempslot + context.CacheInstancePtr;
								var dst = context.GC.Heap[clonedptr];

								vpayload.CopyStruct(dst, src, context.player);
								context.StackSlots[tempslot].SetHeapPtr(clonedptr, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.FLAG_STRUCT);
								key = context.StackSlots[tempslot];
							}
						}


						j = i - 1;     // 已排序部分的最后一个元素索引
									   // 在已排序部分中寻找插入位置
									   //while (j >= 0 && arr[j] > key)
									   //{
									   //	arr[j + 1] = arr[j];  // 元素后移
									   //	j--;
									   //}

						//arr[j + 1] = key;  // 插入到正确位置

						while (j >= 0)
						{
							NaNBoxing vj = vpayload.ReadSlot((uint)j, context.player, out bool ishole_j);
							long comp = comparer(vj, key, sortBehavior, context, scope_ptr, ref error);
							if (error.raised)
							{
								return;
							}
							RtArray.FindAndUpdateHeapInstancePtr(vecptr, context.player, out vpayload);
							if (vpayload.array_len != olen)
							{
								context.player.RaiseError(ref error, "array length changed!");
								return;
							}

							if (comp > 0)
							{
								context.player.SetArraySlot(vj, (uint)j + 1, vpayload, ref error);
								if (error.raised)
								{
									return;
								}
								j--;
							}
							else
							{
								break;
							}
						}

						context.player.SetArraySlot(key, (uint)j + 1, vpayload, ref error);
					}
				}
				else
				{
					long pivotIndex = Partition(ref vpayload, scope, scope_ptr, vecptr, left, right, context, ref error, sortBehavior, tempslot);
					if (error.raised)
					{
						return;
					}

					vecptr = RtArray.FindAndUpdateHeapInstancePtr(vecptr, context.player, out vpayload);

					QuickSort(scope, ref vpayload, vecptr, scope_ptr, left, pivotIndex - 1, context, ref error, sortBehavior, tempslot);
					if (error.raised)
					{
						return;
					}

					QuickSort(scope, ref vpayload, vecptr, scope_ptr, pivotIndex + 1, right, context, ref error, sortBehavior, tempslot);
					if (error.raised)
					{
						return;
					}
				}
			}

			private static long Partition(ref RtArray vpayload, RtMethodScope scope, int scope_ptr, int vecptr, long left, long right,
				Context context, ref ReceiveError error, NaNBoxing sortBehavior, int tempslot)
			{


				RtArray.FindAndUpdateHeapInstancePtr(scope.ThisPtr.HeapPtr, context.player, out vpayload);
				SelectPivot(scope, scope_ptr, ref vpayload, context, left, right, ref error, sortBehavior, tempslot);
				if (error.raised)
				{
					return 0;
				}

				long i = left;
				long j = right;
				//long keyi = left;

				NaNBoxing pivot = vpayload.ReadSlot((uint)left, context.player, out bool ishole_p);
				context.StackSlots[tempslot] = pivot;
				if (pivot.ValueType == BoxType.HeapPtr && pivot.HeapKind == (byte)RtHeapTypeKind.INSTANCE)
				{
					RtInstance src = (RtInstance)context.GC.Heap[pivot.HeapPtr];
					if (((ASInstance)src.Type).Flags.HasFlag(ClassFlags.Struct))
					{
						int clonedptr = tempslot + context.CacheInstancePtr;
						var dst = context.GC.Heap[clonedptr];

						vpayload.CopyStruct(dst, src, context.player);
						context.StackSlots[tempslot].SetHeapPtr(clonedptr, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.FLAG_STRUCT);
						pivot = context.StackSlots[tempslot];
					}

				}

				uint olen = vpayload.array_len;
				while (i < j)
				{
					while (i < j)
					{
						NaNBoxing vj = vpayload.ReadSlot((uint)j, context.player, out bool ishole_j);

						long comp = comparer(vj, pivot, sortBehavior, context, scope_ptr, ref error);
						if (error.raised)
						{
							return 0;
						}
						RtArray.FindAndUpdateHeapInstancePtr(scope.ThisPtr.HeapPtr, context.player, out vpayload);
						if (vpayload.array_len != olen)
						{
							context.player.RaiseError(ref error, "array length changed!");

							return 0;
						}


						if (comp >= 0)
						{
							j--;
						}
						else
						{
							break;
						}
					}

					if (i < j)
					{
						NaNBoxing vj = vpayload.ReadSlot((uint)j, context.player, out bool ishole_j);
						context.player.SetArraySlot(vj, (uint)i, vpayload, ref error);
						if (error.raised)
						{
							return 0;
						}
					}

					while (i < j)
					{
						NaNBoxing vi = vpayload.ReadSlot((uint)i, context.player, out bool ishole_i);
						long comp = comparer(vi, pivot, sortBehavior, context, scope_ptr, ref error);
						if (error.raised)
						{
							return 0;
						}
						RtArray.FindAndUpdateHeapInstancePtr(scope.ThisPtr.HeapPtr, context.player, out vpayload);
						if (vpayload.array_len != olen)
						{
							context.player.RaiseError(ref error, "array length changed!");

							return 0;
						}
						if (comp <= 0)
						{
							i++;
						}
						else
						{
							break;
						}
					}

					if (i < j)
					{
						NaNBoxing vi = vpayload.ReadSlot((uint)i, context.player, out bool ishole_i);
						context.player.SetArraySlot(vi, (uint)j, vpayload, ref error);
						if (error.raised)
						{
							return 0;
						}
					}

					//vpayload.Swap((uint)i, (uint)j, context, ref error, tempslot);
					if (error.raised)
					{
						return 0;
					}
				}

				context.player.SetArraySlot(pivot, (uint)i, vpayload, ref error);
				return i;






				//RtArray vpayload;
				//int vecPtr = RtArray.FindAndUpdateHeapInstancePtr(scope.ThisPtr.HeapPtr, context.player, out vpayload);

				////T pivot = arr[right];
				//bool ishole;
				//NaNBoxing pivot = vpayload.ReadSlot((uint)right, context.player, out ishole);

				//long i = left - 1;
				//for (long j = left; j < right; j++)
				//{
				//	bool ishole2;
				//	NaNBoxing test = vpayload.ReadSlot((uint)j, context.player, out ishole2);

				//	//if (pivot.Raw != test.Raw)

				//	if (pivot.Raw == test.Raw && pivot.ValueType != BoxType.HeapPtr)
				//	{
				//	}
				//	else
				//	{
				//		uint olen = vpayload.array_len;

				//		long comp = ArrayImpl.comparer(test, pivot, sortBehavior, context, scope_ptr, ref error);
				//		if (error.raised)
				//		{
				//			return 0;
				//		}

				//		vecPtr = RtArray.FindAndUpdateHeapInstancePtr(scope.ThisPtr.HeapPtr, context.player, out vpayload);

				//		if (vpayload.array_len != olen)
				//		{
				//			context.player.RaiseError(ref error, "array length changed!");

				//			return 0;
				//		}

				//		//if (comp == 0)
				//		//{
				//		//	comp = right - j;
				//		//}

				//		if (comp < 0)
				//		{
				//			i++;
				//			vpayload.Swap((uint)i, (uint)j, context, ref error, tempslot);
				//			if (error.raised)
				//			{

				//				return 0;
				//			}
				//		}

				//	}
				//}

				//vpayload.Swap((uint)(i + 1), (uint)right, context, ref error, tempslot);

				//if (error.raised)
				//{
				//	return 0;
				//}

				//return i + 1;
			}


			private static void SelectPivot(RtMethodScope scope, int scope_ptr, ref RtArray vpayload, Context context, long left, long right, ref ReceiveError error
				, NaNBoxing sortBehavior, int tempslot
				)
			{
				long mid = left + (right - left) / 2;


				NaNBoxing l = vpayload.ReadSlot((uint)left, context.player, out bool isholeL);
				NaNBoxing m = vpayload.ReadSlot((uint)mid, context.player, out bool isholeM);
				NaNBoxing r = vpayload.ReadSlot((uint)right, context.player, out bool ishleR);

				var olen = vpayload.array_len;

				{
					long comp = ArrayImpl.comparer(l, m, sortBehavior, context, scope_ptr, ref error);
					if (error.raised)
					{
						return;
					}
					RtArray.FindAndUpdateHeapInstancePtr(scope.ThisPtr.HeapPtr, context.player, out vpayload);
					if (vpayload.array_len != olen)
					{
						context.player.RaiseError(ref error, "array length changed!");
						return;
					}
					if (comp > 0)
					{
						vpayload.Swap((uint)left, (uint)mid, context, ref error, tempslot);
						if (error.raised)
						{
							return;
						}
						NaNBoxing temp = l;
						l = m;
						m = temp;
					}
				}

				{
					long comp = ArrayImpl.comparer(l, r, sortBehavior, context, scope_ptr, ref error);
					if (error.raised)
					{
						return;
					}
					RtArray.FindAndUpdateHeapInstancePtr(scope.ThisPtr.HeapPtr, context.player, out vpayload);
					if (vpayload.array_len != olen)
					{
						context.player.RaiseError(ref error, "array length changed!");
						return;
					}
					if (comp > 0)
					{
						vpayload.Swap((uint)left, (uint)right, context, ref error, tempslot);
						if (error.raised)
						{
							return;
						}
						NaNBoxing temp = l;
						l = r;
						r = temp;
					}
				}

				{
					long comp = ArrayImpl.comparer(m, r, sortBehavior, context, scope_ptr, ref error);
					if (error.raised)
					{
						return;
					}
					RtArray.FindAndUpdateHeapInstancePtr(scope.ThisPtr.HeapPtr, context.player, out vpayload);
					if (comp > 0)
					{
						vpayload.Swap((uint)mid, (uint)right, context, ref error, tempslot);
						if (error.raised)
						{
							return;
						}
						NaNBoxing temp = m;
						l = r;
						r = temp;
					}
				}

				vpayload.Swap((uint)mid, (uint)left, context, ref error, tempslot);


				//// 排序 left, mid, right 三个位置的值
				//if (array[left] > array[mid]) Swap(array, left, mid);
				//if (array[left] > array[right]) Swap(array, left, right);
				//if (array[mid] > array[right]) Swap(array, mid, right);

				//// 此时 mid 位置是中位数，交换到 left 位置作为基准
				//Swap(array, left, mid);
			}


		}

		//.Array$:AS3::sortOn
		[NativeFunction(".Array$:AS3::sortOn")]
		public static void Array_sortOn(Context context,
			ASMethod method, int scope_ptr,
			NaNBoxing thisPtr, int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{

			RtArray array;
			int arrPtr = RtArray.FindAndUpdateHeapInstancePtr(thisPtr.HeapPtr, context.player, out array);
			uint len = array.array_len;


			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			var fieldName = scope.ReadSlot(0, context.player);
			var options = scope.ReadSlot(1, context.player);

			bool fieldnameisarray = false;
			RtArray fields = null;
			if (fieldName.ValueType == BoxType.LocalString)
			{

			}
			else if (fieldName.ValueType == BoxType.HeapPtr)
			{
				//RtHeapBase fn = context.GC.Heap[fieldName.HeapPtr];
				if (fieldName.HeapKind == (byte)RtHeapTypeKind.STRING)
				{

				}
				else if (fieldName.HeapKind == (byte)RtHeapTypeKind.ARRAY)
				{
					fieldnameisarray = true;
					RtArray.FindAndUpdateHeapInstancePtr(fieldName.HeapPtr, context.player, out fields);
				}
				else
				{
					context.player.RaiseTypeError(ref error, fieldName, TypeKind.String);
					return;
				}
			}
			else
			{
				context.player.RaiseTypeError(ref error, fieldName, TypeKind.String);
				return;
			}


			if (!fieldnameisarray && options.ValueType == BoxType.HeapPtr
				)
			{
				options.SetUndefined();
			}

			bool optionisarray = false;
			RtArray option_arr = null;
			if (fieldnameisarray)
			{
				for (uint i = 0; i < fields.array_len; i++)
				{
					bool ishole;
					NaNBoxing f = fields.ReadSlot(i, context.player, out ishole);
					if (f.ValueType == BoxType.LocalString)
					{

					}
					else if (f.ValueType == BoxType.HeapPtr && f.HeapKind == (byte)RtHeapTypeKind.STRING)
					{

					}
					else
					{
						context.player.RaiseTypeError(ref error, f, TypeKind.String);
						return;
					}
				}

				if (options.ValueType == BoxType.HeapPtr)
				{
					//RtHeapBase op = context.GC.Heap[options.HeapPtr];
					if (options.HeapKind == (byte)RtHeapTypeKind.ARRAY)
					{
						RtArray.FindAndUpdateHeapInstancePtr(options.HeapPtr, context.player, out option_arr);
						if (option_arr.array_len == fields.array_len)
						{
							optionisarray = true;
						}
						else
						{
							options.SetUndefined();
						}
					}
				}
			}


			SortOnHelper.QuickSort(scope, scope_ptr, context, ref error, fieldnameisarray, optionisarray, fieldName, options);

			if (error.raised)
			{
				return;
			}

			arrPtr = RtArray.FindAndUpdateHeapInstancePtr(scope.ThisPtr.HeapPtr, context.player, out array);
			context.StackSlots[returnSlotIndex].SetHeapPtr(arrPtr, (byte)RtHeapTypeKind.ARRAY, (byte)HeapKindFlag.NONE);


		}




		private static bool TryFind(NaNBoxing obj, Context context, ReadOnlySpan<char> mode, int namestr, ref ReceiveError error, out NaNBoxing value)
		{
			value = default; value.SetUndefined();

			if (obj.ValueType != BoxType.HeapPtr)
			{
				return false;
			}



			var instance = context.GC.Heap[obj.HeapPtr];
			if (context.StackPosition + 1 >= Context.STACK_LENGTH)
			{
				context.player.RaiseStackOverflow(ref error);
				return false;
			}
			var stackslots = context.StackSlots.AsSpan(context.StackPosition, 2); stackslots.Clear();
			var stPos = context.StackPosition;
			context.StackPosition += 2;

			var ns_set = context.player.nsSetIncludingPublicAndAS3;
			ASContainer as_type = instance.Kind == RtHeapTypeKind.STRING ? context.STRING.Instance : instance.Type;


			int code = context.player.MultiNameLSearch(ns_set, instance.Kind,
				as_type, mode, namestr, new StackLocater() { index = 0 }, stackslots, stPos, obj, context.player.check_MultiNameLSearch_issameorinherit(obj, null), ref error, true);
			switch (code)
			{
				case 0:
					break;
				case 1:
					//有异常产生
					context.StackPosition -= 2;
					return false;
				case 2:
					context.StackPosition -= 2;
					context.GC.CheckGC(ref error);
					context.player.RaiseTypeError_Ambiguous(ref error, mode);
					return false;
				default:
					throw new InvalidOperationException();
			}
			value = context.player.LoadValue(stackslots[0], -1, ref error, stackslots, stPos);
			if (error.raised)
			{
				context.StackPosition -= 2;
				return false;
			}

			context.StackPosition -= 2;


			return true;

		}



		private static long do_sorton(NaNBoxing test, NaNBoxing pivot, NaNBoxing field, NaNBoxing option_box, Context context, int scope_ptr, ref ReceiveError error)
		{
			Span<char> buffer = stackalloc char[16];
			var name = Extensions.GetPrimitiveValueToString(context.player, field, buffer);

			NaNBoxing a;
			if (!TryFind(test, context, name, field.ValueType == BoxType.HeapPtr ? field.HeapPtr : 0, ref error, out a))
			{
				a.SetUndefined();
			}
			if (error.raised) return 0;



			NaNBoxing b;


			if (!TryFind(pivot, context, name, field.ValueType == BoxType.HeapPtr ? field.HeapPtr : 0, ref error, out b))
			{
				b.SetUndefined();
			}
			if (error.raised) return 0;



			context.player.ConvertValueType(ref error, option_box, TypeKind.Int, context.INT, ref option_box);
			if (error.raised) return 0;

			int option = option_box.IntValue;

			if ((option & 16) == 16)
			{

				if (a.ValueType == BoxType.Undefined)
				{
					context.player.RaiseTypeError(ref error, a, TypeKind.Number);
					return 0;
				}

				//转数字
				context.StackPosition++;
				context.StackSlots[context.StackPosition - 1].SetUndefined();
				context.player.ConvertValueType(ref error, a, TypeKind.Number, context.NUMBER, ref context.StackSlots[context.StackPosition - 1], scope_ptr);
				if (error.raised)
				{
					context.StackPosition--;
					return 0;
				}

				double v1 = context.StackSlots[context.StackPosition - 1].Number;

				if (b.ValueType == BoxType.Undefined)
				{
					context.player.RaiseTypeError(ref error, b, TypeKind.Number);
					return 0;
				}

				context.player.ConvertValueType(ref error, b, TypeKind.Number, context.NUMBER, ref context.StackSlots[context.StackPosition - 1], scope_ptr);
				if (error.raised)
				{
					context.StackPosition--;
					return 0;
				}

				double v2 = context.StackSlots[context.StackPosition - 1].Number;
				if (error.raised)
				{
					context.StackPosition--;
					return 0;
				}

				context.StackPosition--;

				if (double.IsNaN(v1) && double.IsNaN(v2))
				{
					return 0;
				}
				else if (double.IsNaN(v1))
				{
					return 1;
				}
				else if (double.IsNaN(v2))
				{
					return -1;
				}
				else if ((option & 2) == 2)
				{
					if (v1 == v2)
						return 0;
					else if (v1 < v2)
						return 1;
					else
						return -1;
				}
				else
				{
					if (v1 == v2)
						return 0;
					else if (v1 > v2)
						return 1;
					else
						return -1;
				}
			}
			else
			{
				if (a.ValueType == BoxType.Undefined && b.ValueType != BoxType.Undefined)
				{
					return 1;
				}
				else if (a.ValueType != BoxType.Undefined && b.ValueType == BoxType.Undefined)
				{
					return -1;
				}

				//字符串比较


				context.StackSlots[context.StackPosition].SetUndefined();
				context.StackSlots[context.StackPosition + 1].SetUndefined();

				context.StackPosition += 2;
				context.GC.CheckGC(ref error);

				context.player.ConvertValueType(ref error, a, TypeKind.String, context.STRING, ref context.StackSlots[context.StackPosition - 2], scope_ptr);
				if (error.raised)
				{
					context.StackPosition -= 2;
					return 0;
				}

				context.player.ConvertValueType(ref error, b, TypeKind.String, context.STRING, ref context.StackSlots[context.StackPosition - 1], scope_ptr);
				if (error.raised)
				{
					context.StackPosition -= 2;
					return 0;
				}

				//unsafe
				{


					Span<char> temp1 = stackalloc char[16];
					ReadOnlySpan<char> chars1 = temp1;
					NaNBoxing box1 = context.StackSlots[context.StackPosition - 2];
					if (box1.ValueType == BoxType.HeapPtr)
					{
						string v1 = ((RtString)context.GC.Heap[box1.HeapPtr]).Str;
						chars1 = v1.AsSpan();
					}
					else
					{
						//Debug.Assert(box1.ValueType == BoxType.LocalString);
						//int len = box1.GetLocalStringChars(temp1);
						//chars1 = temp1.Slice(0, len);

						chars1 = Extensions.GetPrimitiveValueToString(context.player, box1, temp1);

					}



					Span<char> temp2 = stackalloc char[16];
					ReadOnlySpan<char> chars2 = temp2;
					ref NaNBoxing box2 = ref context.StackSlots[context.StackPosition - 1];
					if (box2.ValueType == BoxType.HeapPtr)
					{
						string v = ((RtString)context.GC.Heap[box2.HeapPtr]).Str;
						chars2 = v.AsSpan();
					}
					else
					{
						//Debug.Assert(box2.ValueType == BoxType.LocalString);
						//int len = box2.GetLocalStringChars(temp2);
						//chars2 = temp2.Slice(0, len);


						chars2 = Extensions.GetPrimitiveValueToString(context.player, box2, temp2);

					}


					context.StackPosition -= 2;

					int comp = chars1.CompareTo(chars2, (option & 1) == 1 ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal); //.Compare(v1, v2, (option & 1) == 1);
					if ((option & 2) == 2)
						return -comp;
					else
						return comp;

				}
			}
		}

		private static long sorton_comparer(NaNBoxing test, NaNBoxing pivot, bool fieldisarray, bool optionisarray, NaNBoxing field, NaNBoxing option_box, Context context, int scope_ptr, ref ReceiveError error)
		{

			//确保空槽不会传入。
			if (test.ValueType == BoxType.Fault && pivot.ValueType == BoxType.Fault)
			{
				return 0;
			}
			else
			if (test.ValueType == BoxType.Fault && pivot.ValueType != BoxType.Fault)
			{
				return 1;
			}
			else if (test.ValueType != BoxType.Fault && test.ValueType == BoxType.Fault)
			{
				return -1;
			}



			if (fieldisarray)
			{
				RtArray fieldarr;
				RtArray.FindAndUpdateHeapInstancePtr(field.HeapPtr, context.player, out fieldarr);


				RtArray optionarr = null;
				if (optionisarray)
				{
					RtArray.FindAndUpdateHeapInstancePtr(option_box.HeapPtr, context.player, out optionarr);
				}

				for (uint i = 0; i < fieldarr.array_len; i++)
				{
					bool ishole;
					NaNBoxing f = fieldarr.ReadSlot(i, context.player, out ishole);

					NaNBoxing o = option_box;

					if (optionisarray)
					{
						o = optionarr.ReadSlot(i, context.player, out ishole);
					}

					long c = do_sorton(test, pivot, f, o, context, scope_ptr, ref error);
					if (error.raised)
					{
						return 0;
					}

					if (c != 0)
					{
						return c;
					}

				}

				return 0;
			}
			else
			{
				return do_sorton(test, pivot, field, option_box, context, scope_ptr, ref error);

			}
		}

		static class SortOnHelper
		{
			struct sortItem
			{
				public NaNBoxing value;
				public int index;
			}

			public static void QuickSort(RtMethodScope scope, int scope_ptr, Context context, ref ReceiveError error, bool fieldisarray, bool optionisarray, NaNBoxing field, NaNBoxing option)
			{

				RtArray vpayload;
				int vecPtr = RtArray.FindAndUpdateHeapInstancePtr(scope.ThisPtr.HeapPtr, context.player, out vpayload);

				if (vpayload.array_len == 0)
					return;


				if (context.StackPosition + 2 >= Context.STACK_LENGTH)
				{
					context.player.RaiseStackOverflow(ref error);
					return;
				}



				if (vpayload.array_len <= 1024 * 8 * 64)
				{
					//SortOn ,按字段排序。所以我的处理就是先把字段值读出来，根据排序需求先转成字符串或者数字，然后本地排序
					int oLen = (int)vpayload.array_len;

					int fieldCount = 1;

					NaNBoxing[] sortfields = null;
					int[] sortOptions = null;
					int[] ind = ArrayPool<int>.Shared.Rent(oLen);
					for (int i = 0; i < oLen; i++)
					{
						ind[i] = i;
					}

					RtArray fieldarr = null;
					RtArray optionarr = null;
					if (fieldisarray)
					{

						RtArray.FindAndUpdateHeapInstancePtr(field.HeapPtr, context.player, out fieldarr);

						fieldCount = (int)fieldarr.array_len;
						if (fieldCount == 0)
						{
							return;
						}


						//throw new NotImplementedException();
					}

					sortfields = ArrayPool<NaNBoxing>.Shared.Rent(oLen * fieldCount);
					sortOptions = ArrayPool<int>.Shared.Rent(fieldCount);
					
					context.GC.PushTemporyHolder(sortfields, oLen * fieldCount);

					//读取字段值
					int basePos = context.StackPosition;
					context.StackPosition++;

					context.StackSlots[context.StackPosition].SetUndefined();

					Span<char> namebuffer = stackalloc char[16];

					for (int j = 0; j < oLen; j++)
					{
						//读对象
						NaNBoxing key = vpayload.ReadSlot((uint)j, context.player, out bool ishole);

						if (!ishole)
						{
							//obj保存到槽里防止GC
							context.StackSlots[basePos] = key;
							if (key.ValueType == BoxType.HeapPtr && key.HeapKind == (byte)RtHeapTypeKind.INSTANCE)
							{
								RtInstance src = (RtInstance)context.GC.Heap[key.HeapPtr];
								if (((ASInstance)src.Type).Flags.HasFlag(ClassFlags.Struct))
								{
									int clonedptr = basePos + context.CacheInstancePtr;
									var dst = context.GC.Heap[clonedptr];

									vpayload.CopyStruct(dst, src, context.player);
									context.StackSlots[basePos].SetHeapPtr(clonedptr, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.FLAG_STRUCT);
									key = context.StackSlots[basePos];
								}
							}
						}

						for (int i = 0; i < fieldCount; i++)
						{
							NaNBoxing seachName;
							int s_option;
							if (fieldisarray)
							{
								RtArray.FindAndUpdateHeapInstancePtr(field.HeapPtr, context.player, out fieldarr);
								seachName = fieldarr.ReadSlot((uint)i, context.player, out bool ifhole);
								if (ifhole)
								{
									ishole = true;
								}

								if (optionisarray)
								{

									RtArray.FindAndUpdateHeapInstancePtr(option.HeapPtr, context.player, out optionarr);

									NaNBoxing o = optionarr.ReadSlot((uint)i, context.player, out bool isohole);
									if (isohole)
									{
										s_option = 0;
									}
									else
									{
										s_option = o.IntValue;
									}
								}
								else
								{
									s_option = option.IntValue;
								}

							}
							else
							{
								seachName = field;
								s_option = option.IntValue;
							}

							int fieldid = j * fieldCount + i;

							if (ishole)
							{
								sortfields[fieldid].setFault();
							}

							var name = Extensions.GetPrimitiveValueToString(context.player, seachName, namebuffer);

							NaNBoxing a;
							if (!TryFind(key, context, name, seachName.ValueType == BoxType.HeapPtr ? seachName.HeapPtr : 0, ref error, out a))
							{
								a.SetUndefined();
							}
							if (error.raised)
							{

								var sf = context.GC.PopTemporyHolder();
								Debug.Assert(sf == sortfields);

								ArrayPool<NaNBoxing>.Shared.Return(sortfields);
								ArrayPool<int>.Shared.Return(ind);
								ArrayPool<int>.Shared.Return(sortOptions);



								context.StackPosition = basePos;
								return;
							}



							sortOptions[i] = s_option;

							if ((s_option & 16) == 16)
							{
								//转数字
								if (a.ValueType == BoxType.Undefined)
								{
									var sf = context.GC.PopTemporyHolder();
									Debug.Assert(sf == sortfields);

									ArrayPool<NaNBoxing>.Shared.Return(sortfields);
									ArrayPool<int>.Shared.Return(ind);
									ArrayPool<int>.Shared.Return(sortOptions);


									context.player.RaiseTypeError(ref error, a, TypeKind.Number);
									context.StackPosition = basePos;
									return;
								}

								//转数字

								context.StackSlots[context.StackPosition].SetUndefined();
								context.player.ConvertValueType(ref error, a, TypeKind.Number, context.NUMBER, ref context.StackSlots[context.StackPosition], scope_ptr);
								if (error.raised)
								{
									var sf = context.GC.PopTemporyHolder();
									Debug.Assert(sf == sortfields);

									ArrayPool<NaNBoxing>.Shared.Return(sortfields);
									ArrayPool<int>.Shared.Return(ind);
									ArrayPool<int>.Shared.Return(sortOptions);


									context.StackPosition = basePos;
									return;
								}
								sortfields[fieldid] = context.StackSlots[context.StackPosition];


							}
							else
							{
								//转字符串
								if (a.ValueType == BoxType.Undefined)
								{
									sortfields[fieldid].SetUndefined();
								}
								else
								{
									//字符串比较
									context.StackSlots[context.StackPosition].SetUndefined();


									context.GC.CheckGC(ref error);

									context.player.ConvertValueType(ref error, a, TypeKind.String, context.STRING, ref context.StackSlots[context.StackPosition], scope_ptr);
									if (error.raised)
									{
										var sf = context.GC.PopTemporyHolder();
										Debug.Assert(sf == sortfields);

										ArrayPool<NaNBoxing>.Shared.Return(sortfields);
										ArrayPool<int>.Shared.Return(ind);
										ArrayPool<int>.Shared.Return(sortOptions);


										context.StackPosition = basePos;
										return;
									}
									sortfields[fieldid] = context.StackSlots[context.StackPosition];


								}

							}

							vecPtr = RtArray.FindAndUpdateHeapInstancePtr(scope.ThisPtr.HeapPtr, context.player, out vpayload);
							if (vpayload.array_len != oLen)
							{
								var sf = context.GC.PopTemporyHolder();
								Debug.Assert(sf == sortfields);

								ArrayPool<NaNBoxing>.Shared.Return(sortfields);
								ArrayPool<int>.Shared.Return(ind);
								ArrayPool<int>.Shared.Return(sortOptions);


								context.StackPosition = basePos;
								context.player.RaiseError(ref error, "array length changed!");
								return;
							}
						}

					}


					ind.AsSpan().Slice(0, oLen).Sort((l, r) =>
					{

						Span<char> temp1 = stackalloc char[16];
						Span<char> temp2 = stackalloc char[16];

						for (int i = 0; i < fieldCount; i++)
						{
							NaNBoxing a = sortfields[l * fieldCount + i];
							NaNBoxing b = sortfields[r * fieldCount + i];

							//空槽，前面处理结果是空槽第一个字段就是Fault
							if (a.ValueType == BoxType.Fault && b.ValueType == BoxType.Fault)
							{
								return 0;
							}
							else
							if (a.ValueType == BoxType.Fault && b.ValueType != BoxType.Fault)
							{
								return 1;
							}
							else if (a.ValueType != BoxType.Fault && b.ValueType == BoxType.Fault)
							{
								return -1;
							}


							int option = sortOptions[i];

							if ((option & 16) == 16)
							{
								double v1 = a.Number;
								double v2 = b.Number;

								if (double.IsNaN(v1) && double.IsNaN(v2))
								{
									return 0;
								}
								else if (double.IsNaN(v1))
								{
									return 1;
								}
								else if (double.IsNaN(v2))
								{
									return -1;
								}
								else if ((option & 2) == 2)
								{
									if (v1 == v2)
									{
										if (i == fieldCount - 1)
										{
											return 0;
										}
										else
										{
											continue;
										}
									}
									else if (v1 < v2)
										return 1;
									else
										return -1;
								}
								else
								{
									if (v1 == v2)
									{
										if (i == fieldCount - 1)
										{
											return 0;
										}
										else
										{
											continue;
										}
									}
									else if (v1 > v2)
										return 1;
									else
										return -1;
								}

							}
							else
							{
								if (a.ValueType == BoxType.Undefined && b.ValueType != BoxType.Undefined)
								{
									return 1;
								}
								else if (a.ValueType != BoxType.Undefined && b.ValueType == BoxType.Undefined)
								{
									return -1;
								}
								{

									ReadOnlySpan<char> chars1 = temp1;
									NaNBoxing box1 = a;
									if (box1.ValueType == BoxType.HeapPtr)
									{
										string v1 = ((RtString)context.GC.Heap[box1.HeapPtr]).Str;
										chars1 = v1.AsSpan();
									}
									else
									{
										//Debug.Assert(box1.ValueType == BoxType.LocalString);
										//int len = box1.GetLocalStringChars(temp1);
										//chars1 = temp1.Slice(0, len);

										chars1 = Extensions.GetPrimitiveValueToString(context.player, box1, temp1);

									}

									ReadOnlySpan<char> chars2 = temp2;
									NaNBoxing box2 = b;
									if (box2.ValueType == BoxType.HeapPtr)
									{
										string v = ((RtString)context.GC.Heap[box2.HeapPtr]).Str;
										chars2 = v.AsSpan();
									}
									else
									{
										//Debug.Assert(box2.ValueType == BoxType.LocalString);
										//int len = box2.GetLocalStringChars(temp2);
										//chars2 = temp2.Slice(0, len);


										chars2 = Extensions.GetPrimitiveValueToString(context.player, box2, temp2);

									}


									int comp = chars1.CompareTo(chars2, (option & 1) == 1 ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal); //.Compare(v1, v2, (option & 1) == 1);

									if (comp == 0)
									{
										if (i == fieldCount - 1)
										{
											return 0;
										}
										else
										{
											continue;
										}
									}

									if ((option & 2) == 2)
										return -comp;
									else
										return comp;

								}


							}

						}

						return 0;

					});


					//最终排序
					int[] inv = ArrayPool<int>.Shared.Rent(oLen);
					for (int i = 0; i < oLen; i++)
						inv[ind[i]] = i;

					for (int i = 0; i < oLen; i++)
					{
						while (inv[i] != i)
						{
							int target = inv[i];

							// swap arr[i] <-> arr[target]
							//(arr[i], arr[target]) = (arr[target], arr[i]);
							vpayload.Swap((uint)i, (uint)target, context, ref error, basePos);
							if (error.raised)
							{
								context.StackPosition = basePos;
								return;
							}

							// swap idx[i] <-> idx[target]
							(inv[i], inv[target]) = (inv[target], inv[i]);
						}
					}





					context.StackPosition = basePos;

					var s = context.GC.PopTemporyHolder();
					Debug.Assert(s == sortfields);

					ArrayPool<NaNBoxing>.Shared.Return(sortfields);
					ArrayPool<int>.Shared.Return(ind);
					ArrayPool<int>.Shared.Return(inv);
					ArrayPool<int>.Shared.Return(sortOptions);


				}

				else
				{

					context.StackPosition++;
					context.StackSlots[context.StackPosition - 1].SetUndefined();


					QuickSort(ref vpayload, scope, vecPtr, scope_ptr, 0, vpayload.array_len - 1, context, ref error, fieldisarray, optionisarray, field, option, context.StackPosition - 1);

					context.StackPosition--;
				}
			}

			private static void QuickSort(ref RtArray vpayload, RtMethodScope scope, int vecptr, int scope_ptr, long left, long right, Context context, ref ReceiveError error, bool fieldisarray, bool optionisarray, NaNBoxing field, NaNBoxing option, int tempslot)
			{
				if (left >= right) return;

				if (right - left < 16)
				{
					var olen = vpayload.array_len;
					long i, j;
					for (i = left + 1; i <= right; i++)
					{

						//key = arr[i];  // 当前待排序元素
						NaNBoxing key = vpayload.ReadSlot((uint)i, context.player, out bool ishole_p);
						context.StackSlots[tempslot] = key;
						if (key.ValueType == BoxType.HeapPtr && key.HeapKind == (byte)RtHeapTypeKind.INSTANCE)
						{
							RtInstance src = (RtInstance)context.GC.Heap[key.HeapPtr];
							if (((ASInstance)src.Type).Flags.HasFlag(ClassFlags.Struct))
							{
								int clonedptr = tempslot + context.CacheInstancePtr;
								var dst = context.GC.Heap[clonedptr];

								vpayload.CopyStruct(dst, src, context.player);
								context.StackSlots[tempslot].SetHeapPtr(clonedptr, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.FLAG_STRUCT);
								key = context.StackSlots[tempslot];
							}
						}


						j = i - 1;     // 已排序部分的最后一个元素索引
									   // 在已排序部分中寻找插入位置
									   //while (j >= 0 && arr[j] > key)
									   //{
									   //	arr[j + 1] = arr[j];  // 元素后移
									   //	j--;
									   //}
									   //arr[j + 1] = key;  // 插入到正确位置

						while (j >= left)
						{
							NaNBoxing vj = vpayload.ReadSlot((uint)j, context.player, out bool ishole_j);
							long comp = sorton_comparer(vj, key, fieldisarray, optionisarray, field, option, context, scope_ptr, ref error);
							if (error.raised)
							{
								return;
							}
							RtArray.FindAndUpdateHeapInstancePtr(vecptr, context.player, out vpayload);
							if (vpayload.array_len != olen)
							{
								context.player.RaiseError(ref error, "array length changed!");
								return;
							}

							if (comp > 0)
							{
								context.player.SetArraySlot(vj, (uint)j + 1, vpayload, ref error);

								if (error.raised)
								{
									return;
								}
								j--;
							}
							else
							{
								break;
							}
						}

						context.player.SetArraySlot(key, (uint)j + 1, vpayload, ref error);
					}
				}
				else
				{
					long pivotIndex = Partition(ref vpayload, scope, vecptr, left, right, context, ref error, fieldisarray, optionisarray, field, option, tempslot);
					if (error.raised)
					{
						return;
					}

					QuickSort(ref vpayload, scope, vecptr, scope_ptr, left, pivotIndex - 1, context, ref error, fieldisarray, optionisarray, field, option, tempslot);
					if (error.raised)
					{
						return;
					}

					QuickSort(ref vpayload, scope, vecptr, scope_ptr, pivotIndex + 1, right, context, ref error, fieldisarray, optionisarray, field, option, tempslot);
					if (error.raised)
					{
						return;
					}
				}
			}

			private static long Partition(ref RtArray vpayload, RtMethodScope scope, int scope_ptr, long left, long right,
				Context context, ref ReceiveError error, bool fieldisarray, bool optionisarray, NaNBoxing field, NaNBoxing option, int tempslot)
			{

				RtArray.FindAndUpdateHeapInstancePtr(scope.ThisPtr.HeapPtr, context.player, out vpayload);
				SelectPivot(scope, scope_ptr, ref vpayload, context, left, right, ref error, fieldisarray, optionisarray, field, option, tempslot);
				if (error.raised)
				{
					return 0;
				}

				NaNBoxing pivot = vpayload.ReadSlot((uint)left, context.player, out bool ishole_p);
				long i = left;
				long j = right;
				long keyi = left;

				context.StackSlots[tempslot] = pivot;
				if (pivot.ValueType == BoxType.HeapPtr && pivot.HeapKind == (byte)RtHeapTypeKind.INSTANCE)
				{
					RtInstance src = (RtInstance)context.GC.Heap[pivot.HeapPtr];
					if (((ASInstance)src.Type).Flags.HasFlag(ClassFlags.Struct))
					{
						int clonedptr = tempslot + context.CacheInstancePtr;
						var dst = context.GC.Heap[clonedptr];

						vpayload.CopyStruct(dst, src, context.player);
						context.StackSlots[tempslot].SetHeapPtr(clonedptr, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.FLAG_STRUCT);
						pivot = context.StackSlots[tempslot];
					}

				}


				uint olen = vpayload.array_len;
				while (i < j)
				{
					while (i < j)
					{
						NaNBoxing vj = vpayload.ReadSlot((uint)j, context.player, out bool ishole_j);

						long comp = sorton_comparer(vj, pivot, fieldisarray, optionisarray, field, option, context, scope_ptr, ref error);
						if (error.raised)
						{
							return 0;
						}
						RtArray.FindAndUpdateHeapInstancePtr(scope.ThisPtr.HeapPtr, context.player, out vpayload);
						if (vpayload.array_len != olen)
						{
							context.player.RaiseError(ref error, "array length changed!");

							return 0;
						}


						if (comp >= 0)
						{
							j--;
						}
						else
						{
							break;
						}
					}

					if (i < j)
					{
						NaNBoxing vj = vpayload.ReadSlot((uint)j, context.player, out bool ishole_j);
						context.player.SetArraySlot(vj, (uint)i, vpayload, ref error);
						if (error.raised)
						{
							return 0;
						}
					}

					while (i < j)
					{
						NaNBoxing vi = vpayload.ReadSlot((uint)i, context.player, out bool ishole_i);
						long comp = sorton_comparer(vi, pivot, fieldisarray, optionisarray, field, option, context, scope_ptr, ref error);
						if (error.raised)
						{
							return 0;
						}
						RtArray.FindAndUpdateHeapInstancePtr(scope.ThisPtr.HeapPtr, context.player, out vpayload);
						if (vpayload.array_len != olen)
						{
							context.player.RaiseError(ref error, "array length changed!");

							return 0;
						}
						if (comp <= 0)
						{
							i++;
						}
						else
						{
							break;
						}
					}

					if (i < j)
					{
						NaNBoxing vi = vpayload.ReadSlot((uint)i, context.player, out bool ishole_i);
						context.player.SetArraySlot(vi, (uint)j, vpayload, ref error);
						if (error.raised)
						{
							return 0;
						}
					}

					//vpayload.Swap((uint)i, (uint)j, context, ref error, tempslot);
					if (error.raised)
					{
						return 0;
					}
				}

				//vpayload.Swap((uint)i, (uint)keyi, context, ref error, tempslot);

				context.player.SetArraySlot(pivot, (uint)i, vpayload, ref error);
				return i;

				///// 优化点 2：三数取中法处理“已近有序”
				////    // 取左端、右端和中间位置的三个数，将中位数交换到 left 位置作为基准
				////    SelectPivot(array, left, right);

				////int pivot = array[left];
				////int i = left;
				////int j = right;

				////while (i < j)
				////{
				////	// 从右向左找第一个小于 pivot 的数
				////	while (i < j && array[j] >= pivot) j--;
				////	if (i < j) array[i] = array[j];

				////	// 从左向右找第一个大于 pivot 的数
				////	while (i < j && array[i] <= pivot) i++;
				////	if (i < j) array[j] = array[i];
				////}

				////// 基准值归位
				////array[i] = pivot;
				////return i;










				////T pivot = arr[right];
				//bool ishole;
				//NaNBoxing pivot = vpayload.ReadSlot((uint)right, context.player, out ishole);

				//long i = left - 1;
				//for (long j = left; j < right; j++)
				//{
				//	bool ishole2;
				//	NaNBoxing test = vpayload.ReadSlot((uint)j, context.player, out ishole2);

				//	//if (pivot.Raw != test.Raw)

				//	if (pivot.Raw == test.Raw && pivot.ValueType != BoxType.HeapPtr)
				//	{
				//	}
				//	else
				//	{
				//		uint olen = vpayload.array_len;

				//		long comp = ArrayImpl.sorton_comparer(test, pivot, fieldisarray,optionisarray,field,option  , context, scope_ptr, ref error);
				//		if (error.raised)
				//		{
				//			return 0;
				//		}

				//		RtArray.FindAndUpdateHeapInstancePtr(scope.ThisPtr.HeapPtr, context.player, out vpayload); //访问属性也可能导致数组本身变化

				//		if (vpayload.array_len != olen)
				//		{
				//			context.player.RaiseError(ref error, "array length changed!");

				//			return 0;
				//		}

				//		//if (comp == 0)
				//		//{
				//		//	comp = right - j;
				//		//}

				//		if (comp < 0)
				//		{
				//			i++;
				//			vpayload.Swap((uint)i, (uint)j, context, ref error, tempslot);
				//			if (error.raised)
				//			{

				//				return 0;
				//			}
				//		}

				//	}
				//}

				//vpayload.Swap((uint)(i + 1), (uint)right, context, ref error, tempslot);

				//if (error.raised)
				//{
				//	return 0;
				//}

				//return i + 1;
			}







			private static void SelectPivot(RtMethodScope scope, int scope_ptr, ref RtArray vpayload, Context context, long left, long right, ref ReceiveError error
				, bool fieldisarray, bool optionisarray, NaNBoxing field, NaNBoxing option, int tempslot
				)
			{
				long mid = left + (right - left) / 2;


				NaNBoxing l = vpayload.ReadSlot((uint)left, context.player, out bool isholeL);
				NaNBoxing m = vpayload.ReadSlot((uint)mid, context.player, out bool isholeM);
				NaNBoxing r = vpayload.ReadSlot((uint)right, context.player, out bool ishleR);

				var olen = vpayload.array_len;

				{
					long comp = ArrayImpl.sorton_comparer(l, m, fieldisarray, optionisarray, field, option, context, scope_ptr, ref error);
					if (error.raised)
					{
						return;
					}
					RtArray.FindAndUpdateHeapInstancePtr(scope.ThisPtr.HeapPtr, context.player, out vpayload);
					if (vpayload.array_len != olen)
					{
						context.player.RaiseError(ref error, "array length changed!");
						return;
					}

					if (comp > 0)
					{
						vpayload.Swap((uint)left, (uint)mid, context, ref error, tempslot);
						if (error.raised)
						{
							return;
						}
						NaNBoxing temp = l;
						l = m;
						m = temp;
					}
				}

				{
					long comp = ArrayImpl.sorton_comparer(l, r, fieldisarray, optionisarray, field, option, context, scope_ptr, ref error);
					if (error.raised)
					{
						return;
					}
					RtArray.FindAndUpdateHeapInstancePtr(scope.ThisPtr.HeapPtr, context.player, out vpayload);
					if (vpayload.array_len != olen)
					{
						context.player.RaiseError(ref error, "array length changed!");
						return;
					}
					if (comp > 0)
					{
						vpayload.Swap((uint)left, (uint)right, context, ref error, tempslot);
						if (error.raised)
						{
							return;
						}
						NaNBoxing temp = l;
						l = r;
						r = temp;
					}
				}

				{
					long comp = ArrayImpl.sorton_comparer(m, r, fieldisarray, optionisarray, field, option, context, scope_ptr, ref error);
					if (error.raised)
					{
						return;
					}
					RtArray.FindAndUpdateHeapInstancePtr(scope.ThisPtr.HeapPtr, context.player, out vpayload);
					if (comp > 0)
					{
						vpayload.Swap((uint)mid, (uint)right, context, ref error, tempslot);
						if (error.raised)
						{
							return;
						}
						NaNBoxing temp = m;
						l = r;
						r = temp;
					}
				}

				vpayload.Swap((uint)mid, (uint)left, context, ref error, tempslot);


				//// 排序 left, mid, right 三个位置的值
				//if (array[left] > array[mid]) Swap(array, left, mid);
				//if (array[left] > array[right]) Swap(array, left, right);
				//if (array[mid] > array[right]) Swap(array, mid, right);

				//// 此时 mid 位置是中位数，交换到 left 位置作为基准
				//Swap(array, left, mid);
			}







		}


	}

}
