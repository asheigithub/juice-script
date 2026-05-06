using juicescript.ABC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static juicescript.runtime.Player;

namespace juicescript.runtime.buildin
{
	internal class MathImpl
	{
		//.Math$public::floor
		[NativeFunction("$.Math$public::floor")]
		public static void Math_floor(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			NaNBoxing num = ((RtMethodScope)context.GC.Heap[scope_ptr].facility).ReadSlot(0, context.player);
#if DEBUG
			if (num.ValueType != NaNBoxing.BoxType.Number)
			{
				throw new InvalidOperationException();
			}
#endif

			context.StackSlots[returnSlotIndex].SetNumber( Math.Floor(num.Number) );

		}

		//.Math$public::ceil 
		[NativeFunction("$.Math$public::ceil")]
		public static void Math_ceil(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			NaNBoxing num = ((RtMethodScope)context.GC.Heap[scope_ptr].facility).ReadSlot(0, context.player);
#if DEBUG
			if (num.ValueType != NaNBoxing.BoxType.Number)
			{
				throw new InvalidOperationException();
			}
#endif

			context.StackSlots[returnSlotIndex].SetNumber(Math.Ceiling(num.Number));

		}

		//.Math$public::abs
		[NativeFunction("$.Math$public::abs")]
		public static void Math_abs(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			NaNBoxing val = ((RtMethodScope)context.GC.Heap[scope_ptr].facility).ReadSlot(0, context.player);
			context.StackSlots[returnSlotIndex].SetNumber(Math.Abs(val.Number));
		}

		//.Math$public::acos
		[NativeFunction("$.Math$public::acos")]
		public static void Math_acos(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			NaNBoxing val = ((RtMethodScope)context.GC.Heap[scope_ptr].facility).ReadSlot(0, context.player);
			context.StackSlots[returnSlotIndex].SetNumber(Math.Acos(val.Number));
		}

		//.Math$public::asin
		[NativeFunction("$.Math$public::asin")]
		public static void Math_asin(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			NaNBoxing val = ((RtMethodScope)context.GC.Heap[scope_ptr].facility).ReadSlot(0, context.player);
			context.StackSlots[returnSlotIndex].SetNumber(Math.Asin(val.Number));
		}

		//.Math$public::atan
		[NativeFunction("$.Math$public::atan")]
		public static void Math_atan(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			NaNBoxing val = ((RtMethodScope)context.GC.Heap[scope_ptr].facility).ReadSlot(0, context.player);
			context.StackSlots[returnSlotIndex].SetNumber(Math.Atan(val.Number));
		}

		//.Math$public::atan2
		[NativeFunction("$.Math$public::atan2")]
		public static void Math_atan2(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			NaNBoxing y = ((RtMethodScope)context.GC.Heap[scope_ptr].facility).ReadSlot(0, context.player);
			NaNBoxing x = ((RtMethodScope)context.GC.Heap[scope_ptr].facility).ReadSlot(1, context.player);
			context.StackSlots[returnSlotIndex].SetNumber(Math.Atan2(y.Number, x.Number));
		}

		//.Math$public::cos
		[NativeFunction("$.Math$public::cos")]
		public static void Math_cos(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			NaNBoxing val = ((RtMethodScope)context.GC.Heap[scope_ptr].facility).ReadSlot(0, context.player);
			context.StackSlots[returnSlotIndex].SetNumber(Math.Cos(val.Number));
		}

		//.Math$public::exp
		[NativeFunction("$.Math$public::exp")]
		public static void Math_exp(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			NaNBoxing val = ((RtMethodScope)context.GC.Heap[scope_ptr].facility).ReadSlot(0, context.player);
			context.StackSlots[returnSlotIndex].SetNumber(Math.Exp(val.Number));
		}

		//.Math$public::log
		[NativeFunction("$.Math$public::log")]
		public static void Math_log(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			NaNBoxing val = ((RtMethodScope)context.GC.Heap[scope_ptr].facility).ReadSlot(0, context.player);
			context.StackSlots[returnSlotIndex].SetNumber(Math.Log(val.Number));
		}

		//.Math$public::max
		[NativeFunction("$.Math$public::max")]
		public static void Math_max(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr].facility;
			
			double result = double.NegativeInfinity;
			bool hasNaN = false;
			
			for (ushort slotIdx = 0; slotIdx < 3; slotIdx++)
			{
				var slot = scope.ReadSlot(slotIdx, context.player);
				
				if (slotIdx < 2)
				{
					context.player.ConvertValueType(ref error, slot, TypeKind.Number,
						context.NUMBER, ref context.StackSlots[returnSlotIndex], scope_ptr);
				}
				else
				{
					if (slot.ValueType == NaNBoxing.BoxType.Null || slot.ValueType == NaNBoxing.BoxType.Undefined)
					{
						break;
					}
					var restArray = (RtArray)context.GC.Heap[slot.HeapPtr].facility;
					var restSpan = restArray.stack_store.Span;
					for (int i = 0; i < restSpan.Length; i++)
					{
						context.player.ConvertValueType(ref error, restSpan[i], TypeKind.Number,
							context.NUMBER, ref context.StackSlots[returnSlotIndex], scope_ptr);
						if (error.raised)
						{
							return;
						}
						slot = context.StackSlots[returnSlotIndex];
						if (double.IsNaN(slot.Number))
						{
							hasNaN = true;
							break;
						}
						if (slot.Number > result || (slot.Number == result && 1.0 / slot.Number > 1.0 / result))
						{
							result = slot.Number;
						}
					}
					break;
				}
				
				if (error.raised)
				{
					return;
			}
			
			double val = context.StackSlots[returnSlotIndex].Number;
			if (double.IsNaN(val))
			{
				hasNaN = true;
				break;
			}
			if (val > result || (val == result && 1.0 / val > 1.0 / result))
			{
				result = val;
			}
		}
		
		if (hasNaN)
			{
				context.StackSlots[returnSlotIndex].SetNumber(double.NaN);
			}
			else
			{
				context.StackSlots[returnSlotIndex].SetNumber(result);
			}
		}

		//.Math$public::min
		[NativeFunction("$.Math$public::min")]
		public static void Math_min(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr].facility;
			
			double result = double.PositiveInfinity;
			bool hasNaN = false;
			
			for (ushort slotIdx = 0; slotIdx < 3; slotIdx++)
			{
				var slot = scope.ReadSlot(slotIdx, context.player);
				
				if (slotIdx < 2)
				{
					context.player.ConvertValueType(ref error, slot, TypeKind.Number,
						context.NUMBER, ref context.StackSlots[returnSlotIndex], scope_ptr);
				}
				else
				{
					if (slot.ValueType == NaNBoxing.BoxType.Null || slot.ValueType == NaNBoxing.BoxType.Undefined)
					{
						break;
					}
					var restArray = (RtArray)context.GC.Heap[slot.HeapPtr].facility;
					var restSpan = restArray.stack_store.Span;
					for (int i = 0; i < restSpan.Length; i++)
					{
						context.player.ConvertValueType(ref error, restSpan[i], TypeKind.Number,
							context.NUMBER, ref context.StackSlots[returnSlotIndex], scope_ptr);
						if (error.raised)
						{
							return;
						}
						slot = context.StackSlots[returnSlotIndex];
						if (double.IsNaN(slot.Number))
						{
							hasNaN = true;
							break;
						}
						if (slot.Number < result || (slot.Number == result && 1.0 / slot.Number < 1.0 / result))
						{
							result = slot.Number;
						}
					}
					break;
				}
				
				if (error.raised)
				{
					return;
			}
			
			double val = context.StackSlots[returnSlotIndex].Number;
			if (double.IsNaN(val))
			{
				hasNaN = true;
				break;
			}
			if (val < result || (val == result && 1.0 / val < 1.0 / result))
			{
				result = val;
			}
		}
		
		if (hasNaN)
			{
				context.StackSlots[returnSlotIndex].SetNumber(double.NaN);
			}
			else
			{
				context.StackSlots[returnSlotIndex].SetNumber(result);
			}
		}

		//.Math$public::pow
		[NativeFunction("$.Math$public::pow")]
		public static void Math_pow(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			NaNBoxing bas = ((RtMethodScope)context.GC.Heap[scope_ptr].facility).ReadSlot(0, context.player);
			NaNBoxing pow = ((RtMethodScope)context.GC.Heap[scope_ptr].facility).ReadSlot(1, context.player);
			context.StackSlots[returnSlotIndex].SetNumber(Math.Pow(bas.Number, pow.Number));
		}

		//.Math$public::random
		[NativeFunction("$.Math$public::random")]
		public static void Math_random(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			context.StackSlots[returnSlotIndex].SetNumber(Random.Shared.NextDouble());
		}

		//.Math$public::round
		[NativeFunction("$.Math$public::round")]
		public static void Math_round(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			NaNBoxing val = ((RtMethodScope)context.GC.Heap[scope_ptr].facility).ReadSlot(0, context.player);
			context.StackSlots[returnSlotIndex].SetNumber(Math.Round(val.Number));
		}

		//.Math$public::sin
		[NativeFunction("$.Math$public::sin")]
		public static void Math_sin(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			NaNBoxing val = ((RtMethodScope)context.GC.Heap[scope_ptr].facility).ReadSlot(0, context.player);
			context.StackSlots[returnSlotIndex].SetNumber(Math.Sin(val.Number));
		}

		//.Math$public::sqrt
		[NativeFunction("$.Math$public::sqrt")]
		public static void Math_sqrt(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			NaNBoxing val = ((RtMethodScope)context.GC.Heap[scope_ptr].facility).ReadSlot(0, context.player);
			context.StackSlots[returnSlotIndex].SetNumber(Math.Sqrt(val.Number));
		}

		//.Math$public::tan
		[NativeFunction("$.Math$public::tan")]
		public static void Math_tan(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			NaNBoxing val = ((RtMethodScope)context.GC.Heap[scope_ptr].facility).ReadSlot(0, context.player);
			context.StackSlots[returnSlotIndex].SetNumber(Math.Tan(val.Number));
		}

	}
}
