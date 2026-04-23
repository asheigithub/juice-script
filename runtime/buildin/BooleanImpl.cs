using juicescript.ABC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static juicescript.runtime.Player;

namespace juicescript.runtime.buildin
{
	internal class BooleanImpl
	{
		//.Boolean$public::valueOf
		[NativeFunction(".Boolean$public::valueOf")]
		public static void Boolean_valueOf(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			context.StackSlots[returnSlotIndex].SetBoolean(thisPtr.Boolean);
		}
	}
}
