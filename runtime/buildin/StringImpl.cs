using juicescript.ABC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
				context.GC.Heap[thisPtr.HeapPtr].TypeKind != RtHeapTypeKind.STRING
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

			var scope = (RtPayloadMethodScope)context.GC.Heap[scope_ptr].facility;

			var rest = scope.ReadSlot(0, context.player);
			var rest_array = (RtPayloadArray)context.GC.Heap[rest.HeapPtr].facility;

#if DEBUG
			if (rest_array.StoreMode != RtPayloadArray.ArrayStoreMode.cache_on_stack)
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
			//	sb.Append(((RtPayloadString)context.GC.Heap[thisPtr.HeapPtr].facility).Str);
			//}

			sb.Append(Extensions.GetPrimitiveValueToString(context.player, context.StackSlots[returnSlotIndex]));


			Span<char> argchars = stackalloc char[16];
			for (int i = 0; i < arguments.Length; i++)
			{
				var arg = arguments[i];

				if (context.player.IsPrimitive(arg))
				{
					sb.Append(Extensions.GetPrimitiveValueToString(context.player, arg));

				}
				else
				{
					context.player.ConvertValueType(ref error, arg, TypeKind.String, context.STRING, ref context.StackSlots[returnSlotIndex], scope_ptr, thisPtr);
					if (error.raised)
					{
						return;
					}

					sb.Append(Extensions.GetPrimitiveValueToString(context.player, context.StackSlots[returnSlotIndex]));

				}
			}

			NaNBoxing v;
			context.player.TryCreateStringValue(sb.ToString(), out v, ref error);
			context.StackSlots[returnSlotIndex] = v;

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
				context.StackSlots[returnSlotIndex].SetInt(((RtPayloadString)context.GC.Heap[thisPtr.HeapPtr].facility).Str.Length);
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



			var scope = (RtPayloadMethodScope)context.GC.Heap[scope_ptr].facility;


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
					context.StackSlots[returnSlotIndex].SetHeapPtr(context.player.EMPTY_STR);
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
				var str = ((RtPayloadString)context.GC.Heap[thisPtr.HeapPtr].facility).Str;
				int len = str.Length;

				if (i < 0 || i > len - 1)
				{
					context.StackSlots[returnSlotIndex].SetHeapPtr(context.player.EMPTY_STR);
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



			var scope = (RtPayloadMethodScope)context.GC.Heap[scope_ptr].facility;


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
				var str = ((RtPayloadString)context.GC.Heap[thisPtr.HeapPtr].facility).Str;
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

			var scope = (RtPayloadMethodScope)context.GC.Heap[scope_ptr].facility;

			var rest = scope.ReadSlot(0, context.player);
			var rest_array = (RtPayloadArray)context.GC.Heap[rest.HeapPtr].facility;

#if DEBUG
			if (rest_array.StoreMode != RtPayloadArray.ArrayStoreMode.cache_on_stack)
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
			context.player.TryCreateStringValue(sb.ToString(), out result, ref error);
			context.StackSlots[returnSlotIndex] = result;
		}



		[NativeFunction(".String$:AS3::indexOf")]
		public static void String_indexOf(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{ 
			String_Proto_indexOf(context,method,scope_ptr,thisPtr,stackStPos,ref error,returnSlotIndex);
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



			var scope = (RtPayloadMethodScope)context.GC.Heap[scope_ptr].facility;


			NaNBoxing val = scope.ReadSlot(0,context.player);
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
						context.StackSlots[returnSlotIndex].SetInt(-1);
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
					string val_str = ((RtPayloadString)context.GC.Heap[val.HeapPtr].facility).Str;
					ReadOnlySpan<char> val_char = val_str.AsSpan();

					if (i >= str_span.Length)
					{
						context.StackSlots[returnSlotIndex].SetInt(-1);
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
				var str = ((RtPayloadString)context.GC.Heap[thisPtr.HeapPtr].facility).Str;

				if (val.ValueType == NaNBoxing.BoxType.LocalString)
				{
					Span<char> temp = stackalloc char[16];
					int len = val.GetLocalStringChars(temp);
					ReadOnlySpan<char> val_char = temp.Slice(0,len);

					var str_span = str.AsSpan();
					if (i >= str_span.Length || i< 0)
					{
						context.StackSlots[returnSlotIndex].SetInt(-1);
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
							context.StackSlots[returnSlotIndex].SetInt( i + find );
						}
					}

				}
				else
				{
					string val_str = ((RtPayloadString)context.GC.Heap[val.HeapPtr].facility).Str;

					if (i >= str.Length || i<0)
					{
						context.StackSlots[returnSlotIndex].SetInt(-1);
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
			



		}

	}

}
