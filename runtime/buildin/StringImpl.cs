using juicescript.ABC;
using juicescript.ABC.Locaters;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static juicescript.runtime.Player;

namespace juicescript.runtime.buildin
{
	internal class StringImpl
	{
		//.String$:AS3::toString
		[NativeFunction(".String$:AS3::toString")]
		public static void String_toString(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			String_Proto_toString(context, method, scope_ptr, thisPtr, stackStPos, ref error, returnSlotIndex);
		}

		[NativeFunction(".String$@::toString")]
		public static void String_Proto_toString(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			if (thisPtr.ValueType != NaNBoxing.BoxType.LocalString && (thisPtr.ValueType != NaNBoxing.BoxType.HeapPtr
				||
				context.GC.Heap[thisPtr.HeapPtr].Kind != RtHeapTypeKind.STRING
				))
			{
				context.player.RaiseTypeError(ref error, thisPtr, TypeKind.String);
				return;
			}

			context.StackSlots[returnSlotIndex] = thisPtr;
		}

		//.String$:AS3::concat
		[NativeFunction(".String$:AS3::concat")]
		public static void String_concat(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			String_Proto_concat(context, method, scope_ptr, thisPtr, stackStPos, ref error, returnSlotIndex);
		}

		//.String$@::concat
		[NativeFunction(".String$@::concat")]
		public static void String_Proto_concat(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{

			if (thisPtr.ValueType == NaNBoxing.BoxType.Null || thisPtr.ValueType == NaNBoxing.BoxType.Undefined)
			{
				context.player.RaiseTypeError(ref error, thisPtr, TypeKind.String);
				return;
			}

			context.player.ConvertValueType(ref error, thisPtr, TypeKind.String, context.STRING, ref context.StackSlots[returnSlotIndex], scope_ptr);
			if (error.raised)
			{
				return;
			}

			thisPtr = context.StackSlots[returnSlotIndex];

			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			var rest = scope.ReadSlot(0, context.player);
			var rest_array = (RtArray)context.GC.Heap[rest.HeapPtr];

#if DEBUG
			if (rest_array.StoreMode != RtArray.ArrayStoreMode.cache_on_stack)
				throw new InvalidOperationException();
#endif

			var arguments = rest_array.stack_store.Span;

			StringBuilder sb = new StringBuilder();

			//if (thisPtr.ValueType == NaNBoxing.BoxType.LocalString)
			//{
			//	Span<char> temp = stackalloc char[16];
			//	int len = thisPtr.GetLocalStringChars(temp);

			//	sb.Append(temp.Slice(0, len));
			//}
			//else
			//{
			//	sb.Append(((RtPayloadString)context.GC.Heap[thisPtr.HeapPtr]).Str);
			//}
			Span<char> buffer = stackalloc char[16];
			sb.Append(Extensions.GetPrimitiveValueToString(context.player, context.StackSlots[returnSlotIndex], buffer));


			Span<char> argchars = stackalloc char[16];
			for (int i = 0; i < arguments.Length; i++)
			{
				var arg = arguments[i];

				if (context.player.IsPrimitive(arg))
				{
					sb.Append(Extensions.GetPrimitiveValueToString(context.player, arg,buffer));

				}
				else
				{
					context.player.ConvertValueType(ref error, arg, TypeKind.String, context.STRING, ref context.StackSlots[returnSlotIndex], scope_ptr, thisPtr);
					if (error.raised)
					{
						return;
					}

					sb.Append(Extensions.GetPrimitiveValueToString(context.player, context.StackSlots[returnSlotIndex],buffer));

				}
			}

			NaNBoxing v;
			if (context.player.TryCreateStringValue(sb.ToString(), out v, ref error))
			{
				context.StackSlots[returnSlotIndex] = v;

				context.GC.CheckGC(ref error);
			}

		}




		//.String$public::get#length
		[NativeFunction(".String$public::get#length")]
		public static void String_length(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{

			if (thisPtr.ValueType == NaNBoxing.BoxType.LocalString)
			{
				Span<char> temp = stackalloc char[16];
				int len = thisPtr.GetLocalStringChars(temp);
				context.StackSlots[returnSlotIndex].SetInt(len);
			}
			else
			{
				context.StackSlots[returnSlotIndex].SetInt(((RtString)context.GC.Heap[thisPtr.HeapPtr]).Str.Length);
			}
			//context.StackSlots[returnSlotIndex].SetInt(  )

		}

		//.String$:AS3::charAt
		[NativeFunction(".String$:AS3::charAt")]
		public static void String_charAt(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			String_Proto_chatAt(context, method, scope_ptr, thisPtr, stackStPos, ref error, returnSlotIndex);
		}


		//.String$@::charAt
		[NativeFunction(".String$@::charAt")]
		public static void String_Proto_chatAt(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			if (thisPtr.ValueType == NaNBoxing.BoxType.Null || thisPtr.ValueType == NaNBoxing.BoxType.Undefined)
			{
				context.player.RaiseTypeError(ref error, thisPtr, TypeKind.String);
				return;
			}

			context.player.ConvertValueType(ref error, thisPtr, TypeKind.String, context.STRING, ref context.StackSlots[returnSlotIndex], scope_ptr);
			if (error.raised)
			{
				return;
			}

			thisPtr = context.StackSlots[returnSlotIndex];



			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];


			NaNBoxing index_box = default;
			context.player.ConvertValueType(ref error, scope.ReadSlot(0, context.player), TypeKind.Int, context.INT, ref index_box);
			Debug.Assert(!error.raised);


			int i = index_box.IntValue;

			if (thisPtr.ValueType == NaNBoxing.BoxType.LocalString)
			{
				Span<char> temp = stackalloc char[16];
				int len = thisPtr.GetLocalStringChars(temp);
				//context.StackSlots[returnSlotIndex].SetLocalString()

				if (i < 0 || i > len - 1)
				{
					context.StackSlots[returnSlotIndex].SetHeapPtr(context.player.EMPTY_STR, (byte)RtHeapTypeKind.STRING);
				}
				else
				{
					Span<byte> bytes = stackalloc byte[16];
					int utf8len = System.Text.Encoding.UTF8.GetBytes(temp.Slice(i, 1), bytes);
					context.StackSlots[returnSlotIndex].SetLocalString(bytes.Slice(0, utf8len));

				}

			}
			else
			{
				var str = ((RtString)context.GC.Heap[thisPtr.HeapPtr]).Str;
				int len = str.Length;

				if (i < 0 || i > len - 1)
				{
					context.StackSlots[returnSlotIndex].SetHeapPtr(context.player.EMPTY_STR, (byte)RtHeapTypeKind.STRING);
				}
				else
				{
					Span<byte> bytes = stackalloc byte[16];
					int utf8len = System.Text.Encoding.UTF8.GetBytes(str.AsSpan().Slice(i, 1), bytes);
					context.StackSlots[returnSlotIndex].SetLocalString(bytes.Slice(0, utf8len));

				}
			}
			//context.StackSlots[returnSlotIndex].SetInt(  )
		}


		//.String$:AS3::charCodeAt
		[NativeFunction(".String$:AS3::charCodeAt")]
		public static void String_charCodeAt(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			String_Proto_charCodeAt(context, method, scope_ptr, thisPtr, stackStPos, ref error, returnSlotIndex);
		}

		//.String$@::charCodeAt
		[NativeFunction(".String$@::charCodeAt")]
		public static void String_Proto_charCodeAt(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			if (thisPtr.ValueType == NaNBoxing.BoxType.Null || thisPtr.ValueType == NaNBoxing.BoxType.Undefined)
			{
				context.player.RaiseTypeError(ref error, thisPtr, TypeKind.String);
				return;
			}

			context.player.ConvertValueType(ref error, thisPtr, TypeKind.String, context.STRING, ref context.StackSlots[returnSlotIndex], scope_ptr);
			if (error.raised)
			{
				return;
			}

			thisPtr = context.StackSlots[returnSlotIndex];



			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];


			NaNBoxing index_box = default;
			context.player.ConvertValueType(ref error, scope.ReadSlot(0, context.player), TypeKind.Int, context.INT, ref index_box);
			Debug.Assert(!error.raised);


			int i = index_box.IntValue;

			if (thisPtr.ValueType == NaNBoxing.BoxType.LocalString)
			{
				Span<char> temp = stackalloc char[16];
				int len = thisPtr.GetLocalStringChars(temp);
				//context.StackSlots[returnSlotIndex].SetLocalString()

				if (i < 0 || i > len - 1)
				{
					context.StackSlots[returnSlotIndex].SetNumber(double.NaN);
				}
				else
				{

					context.StackSlots[returnSlotIndex].SetNumber(temp[i]);

				}

			}
			else
			{
				var str = ((RtString)context.GC.Heap[thisPtr.HeapPtr]).Str;
				int len = str.Length;

				if (i < 0 || i > len - 1)
				{
					context.StackSlots[returnSlotIndex].SetNumber(double.NaN);
				}
				else
				{

					context.StackSlots[returnSlotIndex].SetNumber(str[i]);

				}
			}
		}



		[NativeFunction("$.String$:AS3::fromCharCode")]
		public static void String_fromCharCode(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{

			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			var rest = scope.ReadSlot(0, context.player);
			var rest_array = (RtArray)context.GC.Heap[rest.HeapPtr];

#if DEBUG
			if (rest_array.StoreMode != RtArray.ArrayStoreMode.cache_on_stack)
				throw new InvalidOperationException();
#endif

			var arguments = rest_array.stack_store.Span;

			StringBuilder sb = new StringBuilder();

			for (int i = 0; i < arguments.Length; i++)
			{
				var arg = arguments[i];
				NaNBoxing charCode = default;
				context.player.ConvertValueType(ref error, arg, TypeKind.Int, context.INT, ref charCode);
				if (error.raised)
				{
					return;
				}

				sb.Append((char)charCode.IntValue);
			}

			NaNBoxing result;
			if (context.player.TryCreateStringValue(sb.ToString(), out result, ref error))
			{
				context.StackSlots[returnSlotIndex] = result;

				context.GC.CheckGC(ref error);
			}
		}



		[NativeFunction(".String$:AS3::indexOf")]
		public static void String_indexOf(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			String_Proto_indexOf(context, method, scope_ptr, thisPtr, stackStPos, ref error, returnSlotIndex);
		}


		//.String$@::indexOf
		[NativeFunction(".String$@::indexOf")]
		public static void String_Proto_indexOf(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			if (thisPtr.ValueType == NaNBoxing.BoxType.Null || thisPtr.ValueType == NaNBoxing.BoxType.Undefined)
			{
				context.player.RaiseTypeError(ref error, thisPtr, TypeKind.String);
				return;
			}

			context.player.ConvertValueType(ref error, thisPtr, TypeKind.String, context.STRING, ref context.StackSlots[returnSlotIndex], scope_ptr);
			if (error.raised)
			{
				return;
			}

			thisPtr = context.StackSlots[returnSlotIndex];



			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];


			NaNBoxing val = scope.ReadSlot(0, context.player);
			if (val.ValueType == NaNBoxing.BoxType.Null || val.ValueType == NaNBoxing.BoxType.Undefined)
			{
				context.StackSlots[returnSlotIndex].SetInt(-1);
				return;
			}

			NaNBoxing index_box = default;
			context.player.ConvertValueType(ref error, scope.ReadSlot(1, context.player), TypeKind.Int, context.INT, ref index_box);
			Debug.Assert(!error.raised);


			int i = index_box.IntValue;
			if (thisPtr.ValueType == NaNBoxing.BoxType.LocalString)
			{
				Span<char> str_buffer = stackalloc char[16];
				int len = thisPtr.GetLocalStringChars(str_buffer);
				ReadOnlySpan<char> str_span = str_buffer.Slice(0, len);

				if (val.ValueType == NaNBoxing.BoxType.LocalString)
				{
					Span<char> temp = stackalloc char[16];
					len = val.GetLocalStringChars(temp);
					ReadOnlySpan<char> val_char = temp.Slice(0, len);

					if (i >= str_span.Length)
					{
						if (len == 0)
						{
							context.StackSlots[returnSlotIndex].SetInt(0);
						}
						else
						{
							context.StackSlots[returnSlotIndex].SetInt(-1);
						}
					}
					else
					{
						int find = str_span.Slice(i).IndexOf(val_char, StringComparison.Ordinal);
						if (find < 0)
						{
							context.StackSlots[returnSlotIndex].SetInt(-1);
						}
						else
						{
							context.StackSlots[returnSlotIndex].SetInt(i + find);
						}
					}

				}
				else
				{
					string val_str = ((RtString)context.GC.Heap[val.HeapPtr]).Str;
					ReadOnlySpan<char> val_char = val_str.AsSpan();

					if (i >= str_span.Length)
					{
						if (val_str.Length == 0)
						{
							context.StackSlots[returnSlotIndex].SetInt(0);
						}
						else
						{
							context.StackSlots[returnSlotIndex].SetInt(-1);
						}
					}
					else
					{
						int find = str_span.Slice(i).IndexOf(val_char, StringComparison.Ordinal);
						if (find < 0)
						{
							context.StackSlots[returnSlotIndex].SetInt(-1);
						}
						else
						{
							context.StackSlots[returnSlotIndex].SetInt(i + find);
						}
					}



				}



			}
			else
			{
				var str = ((RtString)context.GC.Heap[thisPtr.HeapPtr]).Str;

				if (val.ValueType == NaNBoxing.BoxType.LocalString)
				{
					Span<char> temp = stackalloc char[16];
					int len = val.GetLocalStringChars(temp);
					ReadOnlySpan<char> val_char = temp.Slice(0, len);

					var str_span = str.AsSpan();
					if (i >= str_span.Length || i < 0)
					{
						if (len == 0)
						{
							context.StackSlots[returnSlotIndex].SetInt(0);
						}
						else
						{
							context.StackSlots[returnSlotIndex].SetInt(-1);
						}
					}
					else
					{
						int find = str_span.Slice(i).IndexOf(val_char, StringComparison.Ordinal);
						if (find < 0)
						{
							context.StackSlots[returnSlotIndex].SetInt(-1);
						}
						else
						{
							context.StackSlots[returnSlotIndex].SetInt(i + find);
						}
					}

				}
				else
				{
					string val_str = ((RtString)context.GC.Heap[val.HeapPtr]).Str;

					if (i >= str.Length || i < 0)
					{
						if (i == 0 && val_str.Length == 0)
						{
							context.StackSlots[returnSlotIndex].SetInt(0);
						}
						else
						{
							context.StackSlots[returnSlotIndex].SetInt(-1);
						}
					}
					else
					{
						context.StackSlots[returnSlotIndex].SetInt(str.IndexOf(val_str, i));
					}
				}

			}
		}

		//.String$:AS3::lastIndexOf
		[NativeFunction(".String$:AS3::lastIndexOf")]
		public static void String_lastIndexOf(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			String_Proto_lastIndexOf(context, method, scope_ptr, thisPtr, stackStPos, ref error, returnSlotIndex);
		}



		//.String$@::lastIndexOf
		[NativeFunction(".String$@::lastIndexOf")]
		public static void String_Proto_lastIndexOf(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			if (thisPtr.ValueType == NaNBoxing.BoxType.Null || thisPtr.ValueType == NaNBoxing.BoxType.Undefined)
			{
				context.player.RaiseTypeError(ref error, thisPtr, TypeKind.String);
				return;
			}

			context.player.ConvertValueType(ref error, thisPtr, TypeKind.String, context.STRING, ref context.StackSlots[returnSlotIndex], scope_ptr);
			if (error.raised)
			{
				return;
			}

			thisPtr = context.StackSlots[returnSlotIndex];



			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];


			NaNBoxing val = scope.ReadSlot(0, context.player);
			if (val.ValueType == NaNBoxing.BoxType.Null || val.ValueType == NaNBoxing.BoxType.Undefined)
			{
				context.StackSlots[returnSlotIndex].SetInt(-1);
				return;
			}

			NaNBoxing index_arg = scope.ReadSlot(1, context.player);
			if (double.IsNaN(index_arg.Number))
			{
				index_arg.SetNumber(0x7FFFFFFF);
			}

			NaNBoxing index_box = default;
			context.player.ConvertValueType(ref error, index_arg, TypeKind.Int, context.INT, ref index_box);
			Debug.Assert(!error.raised);


			int startIndex = index_box.IntValue;

			if (thisPtr.ValueType == NaNBoxing.BoxType.LocalString)
			{
				Span<char> str_buffer = stackalloc char[16];
				int strLen = thisPtr.GetLocalStringChars(str_buffer);
				ReadOnlySpan<char> str_span = str_buffer.Slice(0, strLen);

				if (val.ValueType == NaNBoxing.BoxType.LocalString)
				{
					Span<char> temp = stackalloc char[16];
					int valLen = val.GetLocalStringChars(temp);
					ReadOnlySpan<char> val_span = temp.Slice(0, valLen);

					if (valLen == 0)
					{
						int result = startIndex > strLen - 1 ? strLen : startIndex;
						context.StackSlots[returnSlotIndex].SetInt(result);
						return;
					}

					int startIdx = startIndex;
					if (startIdx > strLen - 1)
					{
						startIdx = strLen - 1;
					}
					if (startIdx < 0)
					{
						context.StackSlots[returnSlotIndex].SetInt(-1);
						return;
					}

					for (int i = startIdx; i >= 0; i--)
					{
						bool found = true;
						if (i + valLen > strLen)
						{
							found = false;
						}
						else
						{
							for (int j = 0; j < valLen; j++)
							{
								if (str_span[i + j] != val_span[j])
								{
									found = false;
									break;
								}
							}
						}
						if (found)
						{
							context.StackSlots[returnSlotIndex].SetInt(i);
							return;
						}
					}
					context.StackSlots[returnSlotIndex].SetInt(-1);
				}
				else
				{
					string val_str = ((RtString)context.GC.Heap[val.HeapPtr]).Str;
					ReadOnlySpan<char> val_span = val_str.AsSpan();
					int valLen = val_str.Length;

					if (valLen == 0)
					{
						int result = startIndex > strLen - 1 ? strLen : startIndex;
						context.StackSlots[returnSlotIndex].SetInt(result);
						return;
					}

					int startIdx = startIndex;
					if (startIdx > strLen - 1)
					{
						startIdx = strLen - 1;
					}
					if (startIdx < 0)
					{
						context.StackSlots[returnSlotIndex].SetInt(-1);
						return;
					}

					for (int i = startIdx; i >= 0; i--)
					{
						if (i + valLen <= strLen && str_span.Slice(i, valLen).SequenceEqual(val_span))
						{
							context.StackSlots[returnSlotIndex].SetInt(i);
							return;
						}
					}
					context.StackSlots[returnSlotIndex].SetInt(-1);
				}
			}
			else
			{
				var str = ((RtString)context.GC.Heap[thisPtr.HeapPtr]).Str;
				int strLen = str.Length;
				ReadOnlySpan<char> str_span = str.AsSpan();

				if (val.ValueType == NaNBoxing.BoxType.LocalString)
				{
					Span<char> temp = stackalloc char[16];
					int valLen = val.GetLocalStringChars(temp);
					ReadOnlySpan<char> val_span = temp.Slice(0, valLen);

					if (valLen == 0)
					{
						int result = startIndex > strLen - 1 ? strLen : startIndex;
						context.StackSlots[returnSlotIndex].SetInt(result);
						return;
					}

					int startIdx = startIndex;
					if (startIdx > strLen - 1)
					{
						startIdx = strLen - 1;
					}
					if (startIdx < 0)
					{
						context.StackSlots[returnSlotIndex].SetInt(-1);
						return;
					}

					for (int i = startIdx; i >= 0; i--)
					{
						if (i + valLen <= strLen && str_span.Slice(i, valLen).SequenceEqual(val_span))
						{
							context.StackSlots[returnSlotIndex].SetInt(i);
							return;
						}
					}
					context.StackSlots[returnSlotIndex].SetInt(-1);
				}
				else
				{
					string val_str = ((RtString)context.GC.Heap[val.HeapPtr]).Str;
					int valLen = val_str.Length;

					if (valLen == 0)
					{
						int result = startIndex > strLen - 1 ? strLen : startIndex;
						context.StackSlots[returnSlotIndex].SetInt(result);
						return;
					}

					int startIdx = startIndex;
					if (startIdx > strLen - 1)
					{
						startIdx = strLen - 1;
					}
					if (startIdx < 0)
					{
						context.StackSlots[returnSlotIndex].SetInt(-1);
						return;
					}

					for (int i = startIdx; i >= 0; i--)
					{
						if (i + valLen <= strLen && str_span.Slice(i, valLen).SequenceEqual(val_str))
						{
							context.StackSlots[returnSlotIndex].SetInt(i);
							return;
						}
					}
					context.StackSlots[returnSlotIndex].SetInt(-1);
				}
			}
		}
		//.String$:AS3::slice
		[NativeFunction(".String$:AS3::slice")]
		public static void String_slice(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			String_Proto_slice(context, method, scope_ptr, thisPtr, stackStPos, ref error, returnSlotIndex);
		}

		//.String$@::slice
		[NativeFunction(".String$@::slice")]
		public static void String_Proto_slice(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			/*
			 在JS里，如果 slice(0)  , 这时 endIndex是0。但是如果传 slice(0,undefined),那么endIndex是 0x7fffffff,这个过于奇怪.
			AS3中，endIndex如果传入了undefined,undefined转换为Number是NaN,于是就是0，按Flash的来。
			 */


			if (thisPtr.ValueType == NaNBoxing.BoxType.Null || thisPtr.ValueType == NaNBoxing.BoxType.Undefined)
			{
				context.player.RaiseTypeError(ref error, thisPtr, TypeKind.String);
				return;
			}

			context.player.ConvertValueType(ref error, thisPtr, TypeKind.String, context.STRING, ref context.StackSlots[returnSlotIndex], scope_ptr);
			if (error.raised)
			{
				return;
			}

			thisPtr = context.StackSlots[returnSlotIndex];

			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			NaNBoxing startArg = scope.ReadSlot(0, context.player);
			NaNBoxing endArg = scope.ReadSlot(1, context.player);

			if (double.IsPositiveInfinity(startArg.Number))
			{
				startArg.SetNumber(0x7fffffff);
			}

			if (double.IsPositiveInfinity(endArg.Number))
			{
				endArg.SetNumber(0x7fffffff);
			}


			NaNBoxing startIndex = default;
			NaNBoxing endIndex = default;

			context.player.ConvertValueType(ref error, startArg, TypeKind.Int, context.INT, ref startIndex);
			if (error.raised)
			{
				return;
			}
			context.player.ConvertValueType(ref error, endArg, TypeKind.Int, context.INT, ref endIndex);
			if (error.raised)
			{
				return;
			}

			int strLen;
			if (thisPtr.ValueType == NaNBoxing.BoxType.LocalString)
			{
				Span<char> str_buffer = stackalloc char[16];
				strLen = thisPtr.GetLocalStringChars(str_buffer);

				int startIdx = startIndex.IntValue;
				int endIdx = endIndex.IntValue;

				if (startIdx < 0)
				{
					startIdx = strLen + startIdx;
					if (startIdx < 0) startIdx = 0;
				}
				else if (startIdx > strLen)
				{
					startIdx = strLen;
				}

				if (endIdx < 0)
				{
					endIdx = strLen + endIdx;
					if (endIdx < 0) endIdx = 0;
				}
				else if (endIdx > strLen)
				{
					endIdx = strLen;
				}

				if (startIdx >= endIdx)
				{
					context.StackSlots[returnSlotIndex].SetHeapPtr(context.player.EMPTY_STR, (byte)RtHeapTypeKind.STRING);
				}
				else
				{
					int sliceLen = endIdx - startIdx;
					if (sliceLen > 16) sliceLen = 16;
					Span<char> temp = stackalloc char[16];
					thisPtr.GetLocalStringChars(temp);
					var result = temp.Slice(startIdx, sliceLen);


					NaNBoxing v = default;
					Span<byte> dst = stackalloc byte[64];
					int len = System.Text.Encoding.UTF8.GetBytes(result, dst);
					if (len <= 5)
					{
						v.SetLocalString(dst.Slice(0, len));
						context.StackSlots[returnSlotIndex] = v;
					}
					else
					{
						var resultStr = new string(result);
						if (context.player.TryCreateStringValue(resultStr, out v, ref error))
						{
							context.StackSlots[returnSlotIndex] = v;

							context.GC.CheckGC(ref error);
						}
					}




				}
			}
			else
			{
				var str = ((RtString)context.GC.Heap[thisPtr.HeapPtr]).Str;
				strLen = str.Length;

				int startIdx = startIndex.IntValue;
				int endIdx = endIndex.IntValue;

				if (startIdx < 0)
				{
					startIdx = strLen + startIdx;
					if (startIdx < 0) startIdx = 0;
				}
				else if (startIdx > strLen)
				{
					startIdx = strLen;
				}

				if (endIdx < 0)
				{
					endIdx = strLen + endIdx;
					if (endIdx < 0) endIdx = 0;
				}
				else if (endIdx > strLen)
				{
					endIdx = strLen;
				}

				if (startIdx >= endIdx)
				{
					context.StackSlots[returnSlotIndex].SetHeapPtr(context.player.EMPTY_STR, (byte)RtHeapTypeKind.STRING);
				}
				else
				{
					string result = str.Substring(startIdx, endIdx - startIdx);
					NaNBoxing v;
					if (context.player.TryCreateStringValue(result, out v, ref error))
					{
						context.StackSlots[returnSlotIndex] = v;
						context.GC.CheckGC(ref error);
					}
				}
			}
		}



		// .String$:AS3::split
		[NativeFunction(".String$:AS3::split")]
		public static void String_split(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			String_Proto_split(context, method, scope_ptr, thisPtr, stackStPos, ref error, returnSlotIndex);
		}

		[NativeFunction(".String$@::split")]
		public static void String_Proto_split(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			/*根据test262,传入undefined会导致split没有任何匹配,传入null却会当作"null"匹配
			 */


			if (thisPtr.ValueType == NaNBoxing.BoxType.Null || thisPtr.ValueType == NaNBoxing.BoxType.Undefined)
			{
				context.player.RaiseTypeError(ref error, thisPtr, TypeKind.String);
				return;
			}

			context.player.ConvertValueType(ref error, thisPtr, TypeKind.String, context.STRING, ref context.StackSlots[returnSlotIndex], scope_ptr);
			if (error.raised)
			{
				return;
			}

			thisPtr = context.StackSlots[returnSlotIndex];
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
			NaNBoxing delimiter = scope.ReadSlot(0, context.player);
			NaNBoxing limit = scope.ReadSlot(1, context.player);


			if (limit.ValueType == NaNBoxing.BoxType.Undefined)
			{
				limit.SetInt(0x7fffffff);
			}

			context.player.ConvertValueType(ref error, limit, TypeKind.Uint, context.UINT, ref limit, scope_ptr);
			if (error.raised)
			{
				return;
			}

			
			{
				
				Span<char> _buffer = stackalloc char[16];
				ReadOnlySpan<char> delimiter_char =_buffer;
				bool delimiterIsUndefined = false;

				if (delimiter.ValueType == NaNBoxing.BoxType.Null)
				{
					delimiter_char = "null";
				}
				else if (delimiter.ValueType == NaNBoxing.BoxType.Undefined)
				{
					delimiter_char = ""; delimiterIsUndefined = true;
				}
				else
				{
					context.player.ConvertValueType(ref error, delimiter, TypeKind.String, context.STRING, ref context.StackSlots[returnSlotIndex], scope_ptr);
					if (error.raised)
					{
						return;
					}
					NaNBoxing v = context.StackSlots[returnSlotIndex];
					if (v.ValueType == NaNBoxing.BoxType.LocalString)
					{
						var len = v.GetLocalStringChars(_buffer);
						delimiter_char = _buffer.Slice(0, len);
					}
					else
					{
						delimiter_char = ((RtString)context.GC.Heap[v.HeapPtr]).Str.AsSpan();
					}
				}


				var instancePtr = context.CacheArrayPtr + returnSlotIndex;
				var instance = context.GC.Heap[instancePtr];
				instance.Type = context.ARRAY.Instance;

				((RtArray)instance).array_len = 0;
				((RtArray)instance).methodscopeslot_ref_state = 0;
				((RtArray)instance).HEAPINSTANCE_PTR = 0;


				context.StackSlots[returnSlotIndex].SetHeapPtr(instancePtr, (byte)RtHeapTypeKind.ARRAY);


				var arr_payload = (RtArray)context.GC.Heap[instancePtr];
				Debug.Assert(arr_payload.StoreMode == RtArray.ArrayStoreMode.cache);


				
				Span<char> thisbuffer = stackalloc char[16];
				ReadOnlySpan<char> thisStr = thisbuffer;
				if (thisPtr.ValueType == NaNBoxing.BoxType.LocalString)
				{
					var len = thisPtr.GetLocalStringChars(thisbuffer);
					thisStr = thisbuffer.Slice(0, len);
				}
				else
				{
					thisStr = ((RtString)context.GC.Heap[thisPtr.HeapPtr]).Str.AsSpan();
				}

				if (context.StackPosition + 1 >= Context.STACK_LENGTH)
				{
					context.player.RaiseStackOverflow(ref error);
					return;
				}

				while (true)
				{
					int index = thisStr.IndexOf(delimiter_char, StringComparison.Ordinal);

					ReadOnlySpan<char> toemplace = thisStr;

					bool isbreak = false;


					if (!delimiterIsUndefined && delimiter_char.Length == 0)
					{
						if (thisStr.Length == 0 || limit.UIntValue <= arr_payload.array_len)
						{
							return;
						}

						toemplace = thisStr.Slice(0, 1);
						thisStr = thisStr.Slice(1);
					}
					else if (index >= 0 && delimiter_char.Length > 0 && limit.UIntValue > arr_payload.array_len)
					{
						toemplace = thisStr.Slice(0, index);
						thisStr = thisStr.Slice(index + delimiter_char.Length);
					}
					else if (limit.UIntValue > arr_payload.array_len)
					{
						toemplace = thisStr;
						isbreak = true;
					}
					else
					{
						return;
					}

					if (arr_payload.array_len + 1 >= RtArray.MAX_CACHE_ELEMENT)
					{
						if (arr_payload.StoreMode != RtArray.ArrayStoreMode.normal)
						{
							instancePtr = arr_payload.ChangeStoreToHeap(context.player, ref error);
							if (error.raised)
							{
								return;
							}
							instance = context.GC.Heap[instancePtr];
							arr_payload = (RtArray)instance;
							context.StackSlots[returnSlotIndex].SetHeapPtr(instancePtr, (byte)RtHeapTypeKind.ARRAY);


							context.GC.CheckGC(ref error);

						}
					}

					context.StackPosition++;

					NaNBoxing result;
					if (context.player.TryCreateStringValue(new string(toemplace), out result, ref error))
					{
						context.StackSlots[context.StackPosition - 1] = result;
						context.player.SetArraySlot(result, arr_payload.array_len, instance, ref error);
						context.StackPosition--;

						if (error.raised)
						{
							return;
						}

						context.GC.CheckGC(ref error);
					}
					else
					{
						context.StackPosition--;
						return;
					}

					if (isbreak)
					{
						break;
					}

				}

			}

		}




		//.String$:AS3::substring
		[NativeFunction(".String$:AS3::substring")]
		public static void String_substring(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			String_Proto_substring(context, method, scope_ptr, thisPtr, stackStPos, ref error, returnSlotIndex);
		}

		//.String$@::substring
		[NativeFunction(".String$@::substring")]
		public static void String_Proto_substring(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			if (thisPtr.ValueType == NaNBoxing.BoxType.Null || thisPtr.ValueType == NaNBoxing.BoxType.Undefined)
			{
				context.player.RaiseTypeError(ref error, thisPtr, TypeKind.String);
				return;
			}

			context.player.ConvertValueType(ref error, thisPtr, TypeKind.String, context.STRING, ref context.StackSlots[returnSlotIndex], scope_ptr);
			if (error.raised)
			{
				return;
			}

			thisPtr = context.StackSlots[returnSlotIndex];

			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			NaNBoxing startArg = scope.ReadSlot(0, context.player);
			NaNBoxing endArg = scope.ReadSlot(1, context.player);

			if (double.IsPositiveInfinity(startArg.Number))
			{
				startArg.SetNumber(0x7fffffff);
			}

			if (double.IsPositiveInfinity(endArg.Number))
			{
				endArg.SetNumber(0x7fffffff);
			}

			NaNBoxing startIndex = default;
			NaNBoxing endIndex = default;

			context.player.ConvertValueType(ref error, startArg, TypeKind.Int, context.INT, ref startIndex);
			if (error.raised)
			{
				return;
			}
			context.player.ConvertValueType(ref error, endArg, TypeKind.Int, context.INT, ref endIndex);
			if (error.raised)
			{
				return;
			}

			int strLen;
			if (thisPtr.ValueType == NaNBoxing.BoxType.LocalString)
			{
				Span<char> str_buffer = stackalloc char[16];
				strLen = thisPtr.GetLocalStringChars(str_buffer);

				int startIdx = startIndex.IntValue;
				int endIdx = endIndex.IntValue;

				if (startIdx < 0) startIdx = 0;
				if (endIdx < 0) endIdx = 0;
				if (startIdx > strLen) startIdx = strLen;
				if (endIdx > strLen) endIdx = strLen;

				if (startIdx > endIdx)
				{
					int temp = startIdx;
					startIdx = endIdx;
					endIdx = temp;
				}

				if (startIdx >= endIdx)
				{
					context.StackSlots[returnSlotIndex].SetHeapPtr(context.player.EMPTY_STR, (byte)RtHeapTypeKind.STRING);
				}
				else
				{
					int sliceLen = endIdx - startIdx;
					if (sliceLen > 16) sliceLen = 16;
					Span<char> temp = stackalloc char[16];
					thisPtr.GetLocalStringChars(temp);
					var result = temp.Slice(startIdx, sliceLen);

					NaNBoxing v = default;
					Span<byte> dst = stackalloc byte[64];
					int len = System.Text.Encoding.UTF8.GetBytes(result, dst);
					if (len <= 5)
					{
						v.SetLocalString(dst.Slice(0, len));
						context.StackSlots[returnSlotIndex] = v;
					}
					else
					{
						var resultStr = new string(result);
						if (context.player.TryCreateStringValue(resultStr, out v, ref error))
						{
							context.StackSlots[returnSlotIndex] = v;

							context.GC.CheckGC(ref error);
						}
					}
				}
			}
			else
			{
				var str = ((RtString)context.GC.Heap[thisPtr.HeapPtr]).Str;
				strLen = str.Length;

				int startIdx = startIndex.IntValue;
				int endIdx = endIndex.IntValue;

				if (startIdx < 0) startIdx = 0;
				if (endIdx < 0) endIdx = 0;
				if (startIdx > strLen) startIdx = strLen;
				if (endIdx > strLen) endIdx = strLen;

				if (startIdx > endIdx)
				{
					int temp = startIdx;
					startIdx = endIdx;
					endIdx = temp;
				}

				if (startIdx >= endIdx)
				{
					context.StackSlots[returnSlotIndex].SetHeapPtr(context.player.EMPTY_STR, (byte)RtHeapTypeKind.STRING);
				}
				else
				{
					string result = str.Substring(startIdx, endIdx - startIdx);
					NaNBoxing v;
					if (context.player.TryCreateStringValue(result, out v, ref error))
					{
						context.StackSlots[returnSlotIndex] = v;

						context.GC.CheckGC(ref error);
					}
				}
			}
		}



		[NativeFunction(".String$:AS3::substr")]
		public static void String_substr(Context context,
					ASMethod method,
					int scope_ptr,
					NaNBoxing thisPtr,
					int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{


			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			NaNBoxing startArg = scope.ReadSlot(0, context.player);
			NaNBoxing lenArg = scope.ReadSlot(1, context.player);

			if (double.IsPositiveInfinity(startArg.Number))
			{
				startArg.SetNumber(0x7fffffff);
			}

			if (double.IsPositiveInfinity(lenArg.Number))
			{
				lenArg.SetNumber(0x7fffffff);
			}

			NaNBoxing startIndex = default;
			NaNBoxing length = default;

			context.player.ConvertValueType(ref error, startArg, TypeKind.Int, context.INT, ref startIndex);
			if (error.raised)
			{
				return;
			}
			context.player.ConvertValueType(ref error, lenArg, TypeKind.Int, context.INT, ref length);
			if (error.raised)
			{
				return;
			}

			int strLen;
			if (thisPtr.ValueType == NaNBoxing.BoxType.LocalString)
			{
				Span<char> str_buffer = stackalloc char[16];
				strLen = thisPtr.GetLocalStringChars(str_buffer);

				int startIdx = startIndex.IntValue;
				int len = length.IntValue;

				if (startIdx < 0)
				{
					startIdx = strLen + startIdx;
				}

				if (startIdx < 0) startIdx = 0;
				if (startIdx >= strLen)
				{
					context.StackSlots[returnSlotIndex].SetHeapPtr(context.player.EMPTY_STR, (byte)RtHeapTypeKind.STRING);
					return;
				}

				if (len < 0) len = 0;

				int maxLen = strLen - startIdx;
				if (len > maxLen) len = maxLen;

				if (len <= 0)
				{
					context.StackSlots[returnSlotIndex].SetHeapPtr(context.player.EMPTY_STR, (byte)RtHeapTypeKind.STRING);
				}
				else
				{
					if (len > 16) len = 16;
					Span<char> temp = stackalloc char[16];
					thisPtr.GetLocalStringChars(temp);
					var result = temp.Slice(startIdx, len);

					NaNBoxing v = default;
					Span<byte> dst = stackalloc byte[64];
					int byteLen = System.Text.Encoding.UTF8.GetBytes(result, dst);
					if (byteLen <= 5)
					{
						v.SetLocalString(dst.Slice(0, byteLen));
						context.StackSlots[returnSlotIndex] = v;
					}
					else
					{
						var resultStr = new string(result);
						if (context.player.TryCreateStringValue(resultStr, out v, ref error))
						{
							context.StackSlots[returnSlotIndex] = v;

							context.GC.CheckGC(ref error);
						}
					}
				}
			}
			else
			{
				var str = ((RtString)context.GC.Heap[thisPtr.HeapPtr]).Str;
				strLen = str.Length;

				int startIdx = startIndex.IntValue;
				int len = length.IntValue;

				if (startIdx < 0)
				{
					startIdx = strLen + startIdx;
				}

				if (startIdx < 0) startIdx = 0;
				if (startIdx >= strLen)
				{
					context.StackSlots[returnSlotIndex].SetHeapPtr(context.player.EMPTY_STR, (byte)RtHeapTypeKind.STRING);
					return;
				}

				if (len < 0) len = 0;

				int maxLen = strLen - startIdx;
				if (len > maxLen) len = maxLen;

				if (len <= 0)
				{
					context.StackSlots[returnSlotIndex].SetHeapPtr(context.player.EMPTY_STR, (byte)RtHeapTypeKind.STRING);
				}
				else
				{
					string result = str.Substring(startIdx, len);
					NaNBoxing v;
					if (context.player.TryCreateStringValue(result, out v, ref error))
					{
						context.StackSlots[returnSlotIndex] = v;

						context.GC.CheckGC(ref error);
					}
				}
			}
		}


		//.String$:AS3::toLowerCase
		[NativeFunction(".String$:AS3::toLowerCase")]
		public static void String_toLowerCase(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{ 
			String_Proto_toLowerCase(context,method,scope_ptr,thisPtr,stackStPos,ref error,returnSlotIndex);
		}

		//.String$@::toLowerCase
		[NativeFunction(".String$@::toLowerCase")]
		public static void String_Proto_toLowerCase(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			if (thisPtr.ValueType == NaNBoxing.BoxType.Null || thisPtr.ValueType == NaNBoxing.BoxType.Undefined)
			{
				context.player.RaiseTypeError(ref error, thisPtr, TypeKind.String);
				return;
			}

			context.player.ConvertValueType(ref error, thisPtr, TypeKind.String, context.STRING, ref context.StackSlots[returnSlotIndex], scope_ptr);
			if (error.raised)
			{
				return;
			}

			thisPtr = context.StackSlots[returnSlotIndex];

			if (thisPtr.ValueType == NaNBoxing.BoxType.LocalString)
			{
				
				Span<char> thisbuffer = stackalloc char[16];

				var len = thisPtr.GetLocalStringChars(thisbuffer);
				ReadOnlySpan<char> thisStr = thisbuffer.Slice(0, len);

				Span<char> dst = stackalloc char[16];
				int lowcaselen = thisStr.ToLowerInvariant(dst);

				ReadOnlySpan<char> lowcase = dst.Slice(0,lowcaselen);

				Span<byte> bytes = stackalloc byte[16];
				int utf8len = System.Text.Encoding.UTF8.GetBytes(lowcase, bytes);

				if (utf8len <= 5)
				{
					context.StackSlots[returnSlotIndex].SetLocalString(bytes.Slice(0, utf8len));
				}
				else
				{
					string lowcaseStr = lowcase.ToString();
					NaNBoxing v;
					if (context.player.TryCreateStringValue(lowcaseStr, out v, ref error))
					{
						context.StackSlots[returnSlotIndex] = v;

						context.GC.CheckGC(ref error);
					}
				}

			}
			else
			{
				string lowcase = ((RtString)context.GC.Heap[thisPtr.HeapPtr]).Str.ToLowerInvariant();
				NaNBoxing v;
				if (context.player.TryCreateStringValue(lowcase, out v, ref error))
				{
					context.StackSlots[returnSlotIndex] = v;

					context.GC.CheckGC(ref error);
				}
			}

			
		}

		//.String$:AS3::toUpperCase
		[NativeFunction(".String$:AS3::toUpperCase")]
		public static void String_toUpperCase(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			String_Proto_toUpperCase(context, method, scope_ptr, thisPtr, stackStPos, ref error, returnSlotIndex);
		}

		//.String$@::toUpperCase
		[NativeFunction(".String$@::toUpperCase")]
		public static void String_Proto_toUpperCase(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			if (thisPtr.ValueType == NaNBoxing.BoxType.Null || thisPtr.ValueType == NaNBoxing.BoxType.Undefined)
			{
				context.player.RaiseTypeError(ref error, thisPtr, TypeKind.String);
				return;
			}

			context.player.ConvertValueType(ref error, thisPtr, TypeKind.String, context.STRING, ref context.StackSlots[returnSlotIndex], scope_ptr);
			if (error.raised)
			{
				return;
			}

			thisPtr = context.StackSlots[returnSlotIndex];

			if (thisPtr.ValueType == NaNBoxing.BoxType.LocalString)
			{

				Span<char> thisbuffer = stackalloc char[16];

				var len = thisPtr.GetLocalStringChars(thisbuffer);
				ReadOnlySpan<char> thisStr = thisbuffer.Slice(0, len);

				Span<char> dst = stackalloc char[16];
				int lowcaselen = thisStr.ToUpperInvariant(dst);

				ReadOnlySpan<char> lowcase = dst.Slice(0, lowcaselen);

				Span<byte> bytes = stackalloc byte[16];
				int utf8len = System.Text.Encoding.UTF8.GetBytes(lowcase, bytes);

				if (utf8len <= 5)
				{
					context.StackSlots[returnSlotIndex].SetLocalString(bytes.Slice(0, utf8len));
				}
				else
				{
					string lowcaseStr = lowcase.ToString();
					NaNBoxing v;
					if (context.player.TryCreateStringValue(lowcaseStr, out v, ref error))
					{
						context.StackSlots[returnSlotIndex] = v;

						context.GC.CheckGC(ref error);
					}
				}

			}
			else
			{
				string lowcase = ((RtString)context.GC.Heap[thisPtr.HeapPtr]).Str.ToUpperInvariant();
				NaNBoxing v;
				if (context.player.TryCreateStringValue(lowcase, out v, ref error))
				{
					context.StackSlots[returnSlotIndex] = v;

					context.GC.CheckGC(ref error);
				}
			}


		}


		
		[NativeFunction(".String$:AS3::toLocaleLowerCase")]
		public static void String_toLocaleLowerCase(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			String_Proto_toLowerCase(context, method, scope_ptr, thisPtr, stackStPos, ref error, returnSlotIndex);
		}

		[NativeFunction(".String$@::toLocaleLowerCase")]
		public static void String_Proto_toLocaleLowerCase(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			String_Proto_toLowerCase(context, method, scope_ptr, thisPtr, stackStPos, ref error, returnSlotIndex);
		}


		
		[NativeFunction(".String$:AS3::toLocaleUpperCase")]
		public static void String_toLocaleUpperCase(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			String_Proto_toUpperCase(context, method, scope_ptr, thisPtr, stackStPos, ref error, returnSlotIndex);
		}

		[NativeFunction(".String$@::toLocaleUpperCase")]
		public static void String_Proto_toLocaleUpperCase(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			String_Proto_toUpperCase(context, method, scope_ptr, thisPtr, stackStPos, ref error, returnSlotIndex);
		}


		//.String$:AS3::valueOf
		[NativeFunction(".String$:AS3::valueOf")]
		public static void String_valueOf(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			String_Proto_valueOf(context,method,scope_ptr,thisPtr,stackStPos,ref error,returnSlotIndex);
		}

		[NativeFunction(".String$@::valueOf")]
		public static void String_Proto_valueOf(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			//if (thisPtr.ValueType == NaNBoxing.BoxType.Null || thisPtr.ValueType == NaNBoxing.BoxType.Undefined)
			//{
			//	context.player.RaiseTypeError(ref error, thisPtr, TypeKind.String);
			//	return;
			//}

			if (thisPtr.ValueType != NaNBoxing.BoxType.HeapPtr && thisPtr.ValueType != NaNBoxing.BoxType.LocalString)
			{
				context.player.RaiseTypeError(ref error, thisPtr, TypeKind.String);
				return;
			}

			if (thisPtr.ValueType == NaNBoxing.BoxType.HeapPtr)
			{
				RtHeapBase test = context.GC.Heap[thisPtr.HeapPtr];
				if (test.Kind != RtHeapTypeKind.STRING)
				{
					context.player.RaiseTypeError(ref error, thisPtr, TypeKind.String);
					return;
				}
			}

			context.StackSlots[returnSlotIndex] = thisPtr;

		}




		//.String$:AS3::replace
		[NativeFunction(".String$:AS3::replace")]
		public static void String_replace(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			String_Proto_replace(context, method, scope_ptr, thisPtr, stackStPos, ref error, returnSlotIndex);
		}


		//.String$@::replace
		[NativeFunction(".String$@::replace")]
		public static void String_Proto_replace(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			if (thisPtr.ValueType == NaNBoxing.BoxType.Null || thisPtr.ValueType == NaNBoxing.BoxType.Undefined)
			{
				context.player.RaiseTypeError(ref error, thisPtr, TypeKind.String);
				return;
			}

			context.player.ConvertValueType(ref error, thisPtr, TypeKind.String, context.STRING, ref context.StackSlots[returnSlotIndex], scope_ptr);
			if (error.raised)
			{
				return;
			}

			thisPtr = context.StackSlots[returnSlotIndex];

			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			NaNBoxing pattern = scope.ReadSlot(0, context.player);
			NaNBoxing repl = scope.ReadSlot(1, context.player);


			Span<char> buffer1 = stackalloc char[16];
			Span<char> buffer2 = stackalloc char[16];

			var thisStr = Extensions.GetPrimitiveValueToString(context.player, thisPtr,buffer1);
			var patternStr = Extensions.GetPrimitiveValueToString(context.player, pattern,buffer2);

			int find = thisStr.IndexOf(patternStr);
			if (find >=0 )
			{

				if (repl.ValueType == NaNBoxing.BoxType.HeapPtr)
				{
					RtHeapBase replinstance = context.GC.Heap[repl.HeapPtr];
					if (replinstance.Kind == RtHeapTypeKind.CLOSURE)
					{
						if (context.StackPosition + 4 > Context.STACK_LENGTH)
						{
							context.player.RaiseStackOverflow(ref error);
							return;
						}


						int basePos = context.StackPosition;

						context.StackSlots[basePos].SetUndefined();

						var slots = context.StackSlots.AsSpan(context.StackPosition + 1, 3);
						slots.Clear();
						slots[0] = pattern;
						slots[1].SetInt(find);
						slots[2] = thisPtr;

						context.StackPosition += 4;

						var closure = (RtClosure)replinstance;

						unsafe
						{
							StackLocater* args = stackalloc StackLocater[3];
							args->index = 0;
							(args + 1)->index = 1;
							(args + 2)->index = 2;

							context.player.RunMethod(((ASMethodBody)replinstance.Type).Method, closure.This, closure.ScopePtr,
								closure.ScopeType, 3, (byte*)args, slots, ref error,
								basePos
								);

							if (error.raised)
							{
								context.StackPosition = basePos;
								return;
							}

							repl = context.StackSlots[basePos];


							if (context.player.IsPrimitive(repl))
							{

							}
							else
							{
								context.player.ConvertValueType(ref error, repl, TypeKind.String, context.STRING, ref slots[1]);
								if (error.raised)
								{
									context.StackPosition = basePos;
									return;
								}

								repl = slots[1];
							}

							Span<char> replbuffer = stackalloc char[16];

							var replStr = Extensions.GetPrimitiveValueToString(context.player, repl,replbuffer);



							//string final =  thisStr.Substring(0, find) + replStr + thisStr.Substring(find + patternStr.Length);
							string final = $"{thisStr.Slice(0,find)}{replStr}{thisStr.Slice(find+patternStr.Length)}";


							NaNBoxing v;
							if (context.player.TryCreateStringValue(final, out v, ref error))
							{
								context.StackSlots[returnSlotIndex] = v;
							}

							context.StackPosition = basePos;

							context.GC.CheckGC(ref error);

							return;

						}
					}
					else
					{
						goto lbl_nofunctioncase;
					}

				}

			lbl_nofunctioncase:
				{
					if (context.StackPosition + 1 > Context.STACK_LENGTH)
					{
						context.player.RaiseStackOverflow(ref error);
						return;
					}


					int basePos = context.StackPosition;

					context.StackSlots[basePos].SetUndefined();
					context.StackPosition += 1;

					if (context.player.IsPrimitive(repl))
					{
					}
					else
					{
						context.player.ConvertValueType(ref error, repl, TypeKind.String, context.STRING, ref context.StackSlots[basePos], scope_ptr);
						if (error.raised)
						{
							context.StackPosition = basePos;
							return;
						}
						repl = context.StackSlots[basePos];
					}

					Span<char> replbuffer = stackalloc char[16];

					var replStr = Extensions.GetPrimitiveValueToString(context.player, repl,replbuffer);
					//string final = thisStr.Substring(0, find) + replStr + thisStr.Substring(find + patternStr.Length);
					string final = $"{thisStr.Slice(0, find)}{replStr}{thisStr.Slice(find + patternStr.Length)}";

					NaNBoxing v;
					if (context.player.TryCreateStringValue(final, out v, ref error))
					{
						context.StackSlots[returnSlotIndex] = v;
					}

					context.StackPosition = basePos;
					context.GC.CheckGC(ref error);

				}





			}



		}

		//.String$@::search
		[NativeFunction(".String$:AS3::search")]
		public static void String_search(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			String_Proto_search(context, method, scope_ptr, thisPtr, stackStPos, ref error, returnSlotIndex);
		}


		[NativeFunction(".String$@::search")]
		public static void String_Proto_search(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			if (thisPtr.ValueType == NaNBoxing.BoxType.Null || thisPtr.ValueType == NaNBoxing.BoxType.Undefined)
			{
				context.player.RaiseTypeError(ref error, thisPtr, TypeKind.String);
				return;
			}

			context.player.ConvertValueType(ref error, thisPtr, TypeKind.String, context.STRING, ref context.StackSlots[returnSlotIndex], scope_ptr);
			if (error.raised)
			{
				return;
			}

			thisPtr = context.StackSlots[returnSlotIndex];



			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];


			
			

			NaNBoxing val = scope.ReadSlot(0, context.player);

			if (scope.__sendargcount == 0) //如果完全没有传参，则默认是"";
			{
				ReadOnlySpan<byte> temp = stackalloc byte[0];
				val.SetLocalString(temp);
			}

			else if (val.ValueType == NaNBoxing.BoxType.Undefined)
			{
				val.SetHeapPtr(context.player.TYPEOF_undefined_STR, (byte)RtHeapTypeKind.STRING);
			}


			if (context.StackPosition + 1 > Context.STACK_LENGTH)
			{
				context.player.RaiseStackOverflow(ref error);
				return;
			}


			int basePos = context.StackPosition;

			context.StackSlots[basePos].SetUndefined();
			context.StackPosition += 1;



			context.player.ConvertValueType(ref error, val, TypeKind.String, context.STRING, ref context.StackSlots[basePos], scope_ptr);
			if (error.raised)
			{
				context.StackPosition = basePos;
				return;
			}
			val = context.StackSlots[basePos];
			



			if (thisPtr.ValueType == NaNBoxing.BoxType.LocalString)
			{
				Span<char> str_buffer = stackalloc char[16];
				int len = thisPtr.GetLocalStringChars(str_buffer);
				ReadOnlySpan<char> str_span = str_buffer.Slice(0, len);

				if (val.ValueType == NaNBoxing.BoxType.LocalString)
				{
					Span<char> temp = stackalloc char[16];
					len = val.GetLocalStringChars(temp);
					ReadOnlySpan<char> val_char = temp.Slice(0, len);

					
					
					int find = str_span.IndexOf(val_char, StringComparison.Ordinal);
					if (find < 0)
					{
						context.StackSlots[returnSlotIndex].SetInt(-1);
					}
					else
					{
						context.StackSlots[returnSlotIndex].SetInt( find);
					}
					

					context.StackPosition = basePos;

				}
				else
				{
					Span<char> buffer=stackalloc char[16];
					var val_char = Extensions.GetPrimitiveValueToString(context.player, val,buffer); 
					//ReadOnlySpan<char> val_char = val_str.AsSpan();

					
					
					int find = str_span.IndexOf(val_char, StringComparison.Ordinal);
					if (find < 0)
					{
						context.StackSlots[returnSlotIndex].SetInt(-1);
					}
					else
					{
						context.StackSlots[returnSlotIndex].SetInt(find);
					}
					

					context.StackPosition = basePos;

				}



			}
			else
			{
				var str = ((RtString)context.GC.Heap[thisPtr.HeapPtr]).Str;

				if (val.ValueType == NaNBoxing.BoxType.LocalString)
				{
					Span<char> temp = stackalloc char[16];
					int len = val.GetLocalStringChars(temp);
					ReadOnlySpan<char> val_char = temp.Slice(0, len);

					var str_span = str.AsSpan();
					
					
					int find = str_span.IndexOf(val_char, StringComparison.Ordinal);
					if (find < 0)
					{
						context.StackSlots[returnSlotIndex].SetInt(-1);
					}
					else
					{
						context.StackSlots[returnSlotIndex].SetInt(find);
					}

					context.StackPosition = basePos;
				}
				else
				{
					Span<char> buffer = stackalloc char[16];
					var val_str = Extensions.GetPrimitiveValueToString( context.player, val,buffer );

					
					
					context.StackSlots[returnSlotIndex].SetInt(str.AsSpan().IndexOf(val_str));
					
				}

			}



		}


	}

}
