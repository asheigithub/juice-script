using juicescript.ABC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
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
			((RtPayloadInstance)vector2.facility).SetSlot(y, 1, ((ASInstance)vector2.Type)._link_codescope  , context.player);

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

		[NativeFunction("$geom.Vector2$private::Vec2addVec2")]
		public static void Vector2_Vec2addVec2(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtPayloadMethodScope)context.GC.Heap[scope_ptr].facility;

			var vec2 = (ASClass)((RtPayloadScriptClass)context.GC.Heap[thisPtr.HeapPtr].facility).Meta;

			NaNBoxing v1 = scope.ReadSlot(0, context.player);
			NaNBoxing v2 = scope.ReadSlot(1, context.player);

			Debug.Assert(v1.ValueType == NaNBoxing.BoxType.HeapPtr);
			Debug.Assert(v2.ValueType == NaNBoxing.BoxType.HeapPtr);

			var vector2_a = context.GC.Heap[v1.HeapPtr];
			var payload_a = (RtPayloadInstance)vector2_a.facility;

			NaNBoxing x1 = payload_a.ReadSlot(0, vec2.Instance._link_codescope, context.player);
			NaNBoxing y1 = payload_a.ReadSlot(1, vec2.Instance._link_codescope, context.player);

			var vector2_b = context.GC.Heap[v2.HeapPtr];
			var payload_b = (RtPayloadInstance)vector2_b.facility;

			NaNBoxing x2 = payload_b.ReadSlot(0, vec2.Instance._link_codescope, context.player);
			NaNBoxing y2 = payload_b.ReadSlot(1, vec2.Instance._link_codescope, context.player);

			int resultptr = context.player.InitCacheInstance(vec2, returnSlotIndex, false);
			var vector2_result = context.GC.Heap[resultptr];
			var payload_result = (RtPayloadInstance)vector2_result.facility;

			NaNBoxing x = default;x.SetFloat( x1.FloatValue + x2.FloatValue );
			NaNBoxing y = default;y.SetFloat( y1.FloatValue + y2.FloatValue );

			payload_result.SetSlot(x, 0, vec2.Instance._link_codescope, context.player);
			payload_result.SetSlot(y, 1, vec2.Instance._link_codescope, context.player);

		}

		[NativeFunction("$geom.Vector2$private::Vec2subVec2")]
		public static void Vector2_Vec2subVec2(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtPayloadMethodScope)context.GC.Heap[scope_ptr].facility;

			var vec2 = (ASClass)((RtPayloadScriptClass)context.GC.Heap[thisPtr.HeapPtr].facility).Meta;

			NaNBoxing v1 = scope.ReadSlot(0, context.player);
			NaNBoxing v2 = scope.ReadSlot(1, context.player);

			Debug.Assert(v1.ValueType == NaNBoxing.BoxType.HeapPtr);
			Debug.Assert(v2.ValueType == NaNBoxing.BoxType.HeapPtr);

			var vector2_a = context.GC.Heap[v1.HeapPtr];
			var payload_a = (RtPayloadInstance)vector2_a.facility;

			NaNBoxing x1 = payload_a.ReadSlot(0, vec2.Instance._link_codescope, context.player);
			NaNBoxing y1 = payload_a.ReadSlot(1, vec2.Instance._link_codescope, context.player);

			var vector2_b = context.GC.Heap[v2.HeapPtr];
			var payload_b = (RtPayloadInstance)vector2_b.facility;

			NaNBoxing x2 = payload_b.ReadSlot(0, vec2.Instance._link_codescope, context.player);
			NaNBoxing y2 = payload_b.ReadSlot(1, vec2.Instance._link_codescope, context.player);

			int resultptr = context.player.InitCacheInstance(vec2, returnSlotIndex, false);
			var vector2_result = context.GC.Heap[resultptr];
			var payload_result = (RtPayloadInstance)vector2_result.facility;

			NaNBoxing x = default; x.SetFloat(x1.FloatValue - x2.FloatValue);
			NaNBoxing y = default; y.SetFloat(y1.FloatValue - y2.FloatValue);

			payload_result.SetSlot(x, 0, vec2.Instance._link_codescope, context.player);
			payload_result.SetSlot(y, 1, vec2.Instance._link_codescope, context.player);

		}
	}
}
