using juicescript.ABC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static juicescript.runtime.Player;

namespace juicescript.runtime.buildin
{
	internal class GeomImpl
	{
		[NativeFunction("geom.Vector2$public::Vector2")]
		public static void Array(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtPayloadMethodScope)context.GC.Heap[scope_ptr].facility;

			var vector2 = context.GC.Heap[thisPtr.HeapPtr];

			NaNBoxing x = scope.ReadSlot(0, context.player);
			NaNBoxing y = scope.ReadSlot(1, context.player);

#if DEBUG
			if (x.ValueType != NaNBoxing.BoxType.Float || y.ValueType != NaNBoxing.BoxType.Float)
			{
				throw new InvalidOperationException();
			}

#endif
			((RtPayloadInstance)vector2.facility).SetSlot(x, 0, ((ASInstance)vector2.Type)._link_codescope  , context.player);

		}

		//geom.Vector2$public::toString
		[NativeFunction("geom.Vector2$public::toString")]
		public static void Vector2_toString(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtPayloadMethodScope)context.GC.Heap[scope_ptr].facility;

			var vector2 = context.GC.Heap[thisPtr.HeapPtr];
			var payload = (RtPayloadInstance)vector2.facility;


			NaNBoxing x = payload.ReadSlot(0, vector2.Type._link_codescope, context.player);
			NaNBoxing y = payload.ReadSlot(1, vector2.Type._link_codescope, context.player);
			
			

			int str = context.GC.AllocString($"({x.FloatValue.ToString("F2")},{y.FloatValue.ToString("F2")})");
			if (str == 0)
			{
				context.player.RaiseOutOfMemory(ref error);
				return;
			}

			context.StackSlots[returnSlotIndex].SetHeapPtr(str);
		}


	}
}
