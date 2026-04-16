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

		//.String$@::concat
		[NativeFunction(".String$@::concat")]
		public static void String_Proto_concat(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			//if (thisPtr.ValueType != NaNBoxing.BoxType.LocalString && (thisPtr.ValueType != NaNBoxing.BoxType.HeapPtr
			//	||
			//	context.GC.Heap[thisPtr.HeapPtr].TypeKind != RtHeapTypeKind.STRING
			//	))
			//{
			//	context.player.RaiseTypeError(ref error, thisPtr, TypeKind.String);
			//	return;
			//}

			context.player.ConvertValueType(ref error, thisPtr, TypeKind.String, context.STRING, ref context.StackSlots[returnSlotIndex], scope_ptr, thisPtr);
			if (error.raised)
			{
				return;
			}

			thisPtr = context.StackSlots[returnSlotIndex];

			var scope = (RtPayloadMethodScope)context.GC.Heap[scope_ptr].facility;
			context.StackSlots[returnSlotIndex].SetUndefined();

			var rest = scope.ReadSlot(0, context.player);
			var rest_array = (RtPayloadArray)context.GC.Heap[rest.HeapPtr].facility;

#if DEBUG
			if (rest_array.StoreMode != RtPayloadArray.ArrayStoreMode.cache_on_stack)
				throw new InvalidOperationException();
#endif

			var arguments = rest_array.stack_store.Span;

			if (arguments.Length == 0)
			{
				context.StackSlots[returnSlotIndex] = thisPtr;
			}
			else
			{
				StringBuilder sb = new StringBuilder();

				if (thisPtr.ValueType == NaNBoxing.BoxType.LocalString)
				{
					Span<char> temp = stackalloc char[16];
					int len = thisPtr.GetLocalStringChars(temp);

					sb.Append(temp.Slice(0, len));
				}
				else
				{
					sb.Append(((RtPayloadString)context.GC.Heap[thisPtr.HeapPtr].facility).Str);
				}


				Span<char> argchars = stackalloc char[16];
				for (int i = 0; i < arguments.Length; i++)
				{
					var arg = arguments[i];
					context.player.ConvertValueType(ref error, arg, TypeKind.String, context.STRING, ref context.StackSlots[returnSlotIndex], scope_ptr,thisPtr);
					if (error.raised)
					{
						return;
					}

					if (context.StackSlots[returnSlotIndex].ValueType == NaNBoxing.BoxType.LocalString)
					{
						
						int len = context.StackSlots[returnSlotIndex].GetLocalStringChars(argchars);
						sb.Append(argchars.Slice(0, len));
					}
					else
					{
						sb.Append(((RtPayloadString)context.GC.Heap[context.StackSlots[returnSlotIndex].HeapPtr].facility).Str);
					}
				}

				NaNBoxing v;
				context.player.TryCreateStringValue(sb.ToString(), out v, ref error);
				context.StackSlots[returnSlotIndex] = v;
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
				context.StackSlots[returnSlotIndex].SetInt(((RtPayloadString)context.GC.Heap[thisPtr.HeapPtr].facility).Str.Length );
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
			var scope = (RtPayloadMethodScope)context.GC.Heap[scope_ptr].facility;
			double index = scope.ReadSlot(0, context.player).Number;

			if (double.IsNaN(index) || double.IsInfinity(index) || (int)index<0)
			{
				context.StackSlots[returnSlotIndex].SetHeapPtr(context.player.EMPTY_STR);
			}

			int i = (int)index;

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
					context.StackSlots[returnSlotIndex].SetLocalString(bytes.Slice(0,utf8len));

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
					int utf8len = System.Text.Encoding.UTF8.GetBytes( str.AsSpan().Slice(i,1) , bytes);
					context.StackSlots[returnSlotIndex].SetLocalString(bytes.Slice(0, utf8len));

				}
			}
			//context.StackSlots[returnSlotIndex].SetInt(  )

		}

	}
}
