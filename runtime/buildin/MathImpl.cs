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
		[NativeFunction(".Math$public::floor")]
		public static void Math_floor(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			NaNBoxing num = ((RtPayloadMethodScope)context.GC.Heap[scope_ptr].facility).ReadSlot(0, context.player);
#if DEBUG
			if (num.ValueType != NaNBoxing.BoxType.Number)
			{
				throw new InvalidOperationException();
			}
#endif

			context.StackSlots[returnSlotIndex].SetNumber( Math.Floor(num.Number) );

		}

		//.Math$public::ceil 
		[NativeFunction(".Math$public::ceil")]
		public static void Math_ceil(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			NaNBoxing num = ((RtPayloadMethodScope)context.GC.Heap[scope_ptr].facility).ReadSlot(0, context.player);
#if DEBUG
			if (num.ValueType != NaNBoxing.BoxType.Number)
			{
				throw new InvalidOperationException();
			}
#endif

			context.StackSlots[returnSlotIndex].SetNumber(Math.Ceiling(num.Number));

		}

	}
}
