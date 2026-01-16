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
			Debug.Assert(thisPtr.ValueType == NaNBoxing.BoxType.HeapPtr);
			Debug.Assert(context.GC.Heap[thisPtr.HeapPtr].TypeKind == RtHeapTypeKind.STRING);

			context.StackSlots[returnSlotIndex] = thisPtr;

		}
	}
}
