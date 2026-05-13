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
		public static void Vector2(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			var vector2 = context.GC.Heap[thisPtr.HeapPtr];

			NaNBoxing x = scope.ReadSlot(0, context.player);
			NaNBoxing y = scope.ReadSlot(1, context.player);

#if DEBUG
			if (x.ValueType != NaNBoxing.BoxType.Float || y.ValueType != NaNBoxing.BoxType.Float)
			{
				throw new InvalidOperationException();
			}

#endif
			
			
			((RtInstance)vector2).SetSlot(x, 0, ((ASInstance)vector2.Type)._link_codescope  , context.player);
			((RtInstance)vector2).SetSlot(y, 1, ((ASInstance)vector2.Type)._link_codescope  , context.player);

		}

		//geom.Vector2$public::toString
		[NativeFunction("geom.Vector2$public::toString")]
		public static void Vector2_toString(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			var vector2 = context.GC.Heap[thisPtr.HeapPtr];
			var payload = (RtInstance)vector2;


			NaNBoxing x = payload.ReadSlot(0, vector2.Type._link_codescope, context.player);
			NaNBoxing y = payload.ReadSlot(1, vector2.Type._link_codescope, context.player);
			
			

			int str = context.GC.AllocString($"({x.FloatValue.ToString("F2")},{y.FloatValue.ToString("F2")})");
			if (str == 0)
			{
				context.player.RaiseOutOfMemory(ref error);
				return;
			}

			context.StackSlots[returnSlotIndex].SetHeapPtr(str, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
		}

		[NativeFunction("$geom.Vector2$private::Vec2addVec2")]
		public static void Vector2_Vec2addVec2(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			var vec2 = (ASClass)((RtScriptClass)context.GC.Heap[thisPtr.HeapPtr]).Meta;

			NaNBoxing v1 = scope.ReadSlot(0, context.player);
			NaNBoxing v2 = scope.ReadSlot(1, context.player);

			Debug.Assert(v1.ValueType == NaNBoxing.BoxType.HeapPtr);
			Debug.Assert(v2.ValueType == NaNBoxing.BoxType.HeapPtr);

			var vector2_a = context.GC.Heap[v1.HeapPtr];
			var payload_a = (RtInstance)vector2_a;

			NaNBoxing x1 = payload_a.ReadSlot(0, vec2.Instance._link_codescope, context.player);
			NaNBoxing y1 = payload_a.ReadSlot(1, vec2.Instance._link_codescope, context.player);

			var vector2_b = context.GC.Heap[v2.HeapPtr];
			var payload_b = (RtInstance)vector2_b;

			NaNBoxing x2 = payload_b.ReadSlot(0, vec2.Instance._link_codescope, context.player);
			NaNBoxing y2 = payload_b.ReadSlot(1, vec2.Instance._link_codescope, context.player);

			int resultptr = context.player.InitCacheInstance(vec2, returnSlotIndex, false);
			var vector2_result = context.GC.Heap[resultptr];
			var payload_result = (RtInstance)vector2_result;

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
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			var vec2 = (ASClass)((RtScriptClass)context.GC.Heap[thisPtr.HeapPtr]).Meta;

			NaNBoxing v1 = scope.ReadSlot(0, context.player);
			NaNBoxing v2 = scope.ReadSlot(1, context.player);

			Debug.Assert(v1.ValueType == NaNBoxing.BoxType.HeapPtr);
			Debug.Assert(v2.ValueType == NaNBoxing.BoxType.HeapPtr);

			var vector2_a = context.GC.Heap[v1.HeapPtr];
			var payload_a = (RtInstance)vector2_a;

			NaNBoxing x1 = payload_a.ReadSlot(0, vec2.Instance._link_codescope, context.player);
			NaNBoxing y1 = payload_a.ReadSlot(1, vec2.Instance._link_codescope, context.player);

			var vector2_b = context.GC.Heap[v2.HeapPtr];
			var payload_b = (RtInstance)vector2_b;

			NaNBoxing x2 = payload_b.ReadSlot(0, vec2.Instance._link_codescope, context.player);
			NaNBoxing y2 = payload_b.ReadSlot(1, vec2.Instance._link_codescope, context.player);

			int resultptr = context.player.InitCacheInstance(vec2, returnSlotIndex, false);
			var vector2_result = context.GC.Heap[resultptr];
			var payload_result = (RtInstance)vector2_result;

			NaNBoxing x = default; x.SetFloat(x1.FloatValue - x2.FloatValue);
			NaNBoxing y = default; y.SetFloat(y1.FloatValue - y2.FloatValue);

			payload_result.SetSlot(x, 0, vec2.Instance._link_codescope, context.player);
			payload_result.SetSlot(y, 1, vec2.Instance._link_codescope, context.player);

		}

		[NativeFunction("geom.Vector2$public::dot")]
		public static void Vector2_Dot(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			var vector2_this = context.GC.Heap[thisPtr.HeapPtr];
			var payload_this = (RtInstance)vector2_this;

			NaNBoxing x1 = payload_this.ReadSlot(0, vector2_this.Type._link_codescope, context.player);
			NaNBoxing y1 = payload_this.ReadSlot(1, vector2_this.Type._link_codescope, context.player);

			NaNBoxing v_arg = context.StackSlots[stackStPos];
			Debug.Assert(v_arg.ValueType == NaNBoxing.BoxType.HeapPtr);

			var vector2_arg = context.GC.Heap[v_arg.HeapPtr];
			var payload_arg = (RtInstance)vector2_arg;

			NaNBoxing x2 = payload_arg.ReadSlot(0, vector2_arg.Type._link_codescope, context.player);
			NaNBoxing y2 = payload_arg.ReadSlot(1, vector2_arg.Type._link_codescope, context.player);

			float result = x1.FloatValue * x2.FloatValue + y1.FloatValue * y2.FloatValue;

			NaNBoxing nan_result = default;
			nan_result.SetFloat(result);
			context.StackSlots[returnSlotIndex] = nan_result;
		}

		[NativeFunction("geom.Vector2$public::cross")]
		public static void Vector2_Cross(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			var vector2_this = context.GC.Heap[thisPtr.HeapPtr];
			var payload_this = (RtInstance)vector2_this;

			NaNBoxing x1 = payload_this.ReadSlot(0, vector2_this.Type._link_codescope, context.player);
			NaNBoxing y1 = payload_this.ReadSlot(1, vector2_this.Type._link_codescope, context.player);

			NaNBoxing v_arg = context.StackSlots[stackStPos];
			Debug.Assert(v_arg.ValueType == NaNBoxing.BoxType.HeapPtr);

			var vector2_arg = context.GC.Heap[v_arg.HeapPtr];
			var payload_arg = (RtInstance)vector2_arg;

			NaNBoxing x2 = payload_arg.ReadSlot(0, vector2_arg.Type._link_codescope, context.player);
			NaNBoxing y2 = payload_arg.ReadSlot(1, vector2_arg.Type._link_codescope, context.player);

			float result = x1.FloatValue * y2.FloatValue - y1.FloatValue * x2.FloatValue;

			NaNBoxing nan_result = default;
			nan_result.SetFloat(result);
			context.StackSlots[returnSlotIndex] = nan_result;
		}

		[NativeFunction("$geom.Vector2$private::Vec2mulFloat")]
		public static void Vector2_Vec2mulFloat(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			var vec2 = (ASClass)((RtScriptClass)context.GC.Heap[thisPtr.HeapPtr]).Meta;

			NaNBoxing v1 = scope.ReadSlot(0, context.player);
			NaNBoxing s = scope.ReadSlot(1, context.player);

			Debug.Assert(v1.ValueType == NaNBoxing.BoxType.HeapPtr);
			Debug.Assert(s.ValueType == NaNBoxing.BoxType.Float);

			var vector2_a = context.GC.Heap[v1.HeapPtr];
			var payload_a = (RtInstance)vector2_a;

			NaNBoxing x1 = payload_a.ReadSlot(0, vec2.Instance._link_codescope, context.player);
			NaNBoxing y1 = payload_a.ReadSlot(1, vec2.Instance._link_codescope, context.player);

			float scalar = s.FloatValue;

			int resultptr = context.player.InitCacheInstance(vec2, returnSlotIndex, false);
			var vector2_result = context.GC.Heap[resultptr];
			var payload_result = (RtInstance)vector2_result;

			NaNBoxing x = default; x.SetFloat(x1.FloatValue * scalar);
			NaNBoxing y = default; y.SetFloat(y1.FloatValue * scalar);

			payload_result.SetSlot(x, 0, vec2.Instance._link_codescope, context.player);
			payload_result.SetSlot(y, 1, vec2.Instance._link_codescope, context.player);
		}

		[NativeFunction("$geom.Vector2$private::Vec2mulNumber")]
		public static void Vector2_Vec2mulNumber(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			var vec2 = (ASClass)((RtScriptClass)context.GC.Heap[thisPtr.HeapPtr]).Meta;

			NaNBoxing v1 = scope.ReadSlot(0, context.player);
			NaNBoxing s = scope.ReadSlot(1, context.player);

			Debug.Assert(v1.ValueType == NaNBoxing.BoxType.HeapPtr);

			var vector2_a = context.GC.Heap[v1.HeapPtr];
			var payload_a = (RtInstance)vector2_a;

			NaNBoxing x1 = payload_a.ReadSlot(0, vec2.Instance._link_codescope, context.player);
			NaNBoxing y1 = payload_a.ReadSlot(1, vec2.Instance._link_codescope, context.player);

			float scalar = (float)s.Number;

			int resultptr = context.player.InitCacheInstance(vec2, returnSlotIndex, false);
			var vector2_result = context.GC.Heap[resultptr];
			var payload_result = (RtInstance)vector2_result;

			NaNBoxing x = default; x.SetFloat(x1.FloatValue * scalar);
			NaNBoxing y = default; y.SetFloat(y1.FloatValue * scalar);

			payload_result.SetSlot(x, 0, vec2.Instance._link_codescope, context.player);
			payload_result.SetSlot(y, 1, vec2.Instance._link_codescope, context.player);
		}

		[NativeFunction("$geom.Vector2$private::Vec2divFloat")]
		public static void Vector2_Vec2divFloat(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			var vec2 = (ASClass)((RtScriptClass)context.GC.Heap[thisPtr.HeapPtr]).Meta;

			NaNBoxing v1 = scope.ReadSlot(0, context.player);
			NaNBoxing s = scope.ReadSlot(1, context.player);

			Debug.Assert(v1.ValueType == NaNBoxing.BoxType.HeapPtr);
			Debug.Assert(s.ValueType == NaNBoxing.BoxType.Float);

			var vector2_a = context.GC.Heap[v1.HeapPtr];
			var payload_a = (RtInstance)vector2_a;

			NaNBoxing x1 = payload_a.ReadSlot(0, vec2.Instance._link_codescope, context.player);
			NaNBoxing y1 = payload_a.ReadSlot(1, vec2.Instance._link_codescope, context.player);

			float scalar = s.FloatValue;

			int resultptr = context.player.InitCacheInstance(vec2, returnSlotIndex, false);
			var vector2_result = context.GC.Heap[resultptr];
			var payload_result = (RtInstance)vector2_result;

			NaNBoxing x = default; x.SetFloat(x1.FloatValue / scalar);
			NaNBoxing y = default; y.SetFloat(y1.FloatValue / scalar);

			payload_result.SetSlot(x, 0, vec2.Instance._link_codescope, context.player);
			payload_result.SetSlot(y, 1, vec2.Instance._link_codescope, context.player);
		}

		[NativeFunction("$geom.Vector2$private::Vec2divNumber")]
		public static void Vector2_Vec2divNumber(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			var vec2 = (ASClass)((RtScriptClass)context.GC.Heap[thisPtr.HeapPtr]).Meta;

			NaNBoxing v1 = scope.ReadSlot(0, context.player);
			NaNBoxing s = scope.ReadSlot(1, context.player);

			Debug.Assert(v1.ValueType == NaNBoxing.BoxType.HeapPtr);

			var vector2_a = context.GC.Heap[v1.HeapPtr];
			var payload_a = (RtInstance)vector2_a;

			NaNBoxing x1 = payload_a.ReadSlot(0, vec2.Instance._link_codescope, context.player);
			NaNBoxing y1 = payload_a.ReadSlot(1, vec2.Instance._link_codescope, context.player);

			float scalar = (float)s.Number;

			int resultptr = context.player.InitCacheInstance(vec2, returnSlotIndex, false);
			var vector2_result = context.GC.Heap[resultptr];
			var payload_result = (RtInstance)vector2_result;

			NaNBoxing x = default; x.SetFloat(x1.FloatValue / scalar);
			NaNBoxing y = default; y.SetFloat(y1.FloatValue / scalar);

			payload_result.SetSlot(x, 0, vec2.Instance._link_codescope, context.player);
			payload_result.SetSlot(y, 1, vec2.Instance._link_codescope, context.player);
		}

		[NativeFunction("$geom.Vector2$private::FloatmulVec2")]
		public static void Vector2_FloatmulVec2(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			var vec2 = (ASClass)((RtScriptClass)context.GC.Heap[thisPtr.HeapPtr]).Meta;

			NaNBoxing s = scope.ReadSlot(0, context.player);
			NaNBoxing v1 = scope.ReadSlot(1, context.player);

			Debug.Assert(s.ValueType == NaNBoxing.BoxType.Float);
			Debug.Assert(v1.ValueType == NaNBoxing.BoxType.HeapPtr);

			float scalar = s.FloatValue;

			var vector2_a = context.GC.Heap[v1.HeapPtr];
			var payload_a = (RtInstance)vector2_a;

			NaNBoxing x1 = payload_a.ReadSlot(0, vec2.Instance._link_codescope, context.player);
			NaNBoxing y1 = payload_a.ReadSlot(1, vec2.Instance._link_codescope, context.player);

			int resultptr = context.player.InitCacheInstance(vec2, returnSlotIndex, false);
			var vector2_result = context.GC.Heap[resultptr];
			var payload_result = (RtInstance)vector2_result;

			NaNBoxing x = default; x.SetFloat(scalar * x1.FloatValue);
			NaNBoxing y = default; y.SetFloat(scalar * y1.FloatValue);

			payload_result.SetSlot(x, 0, vec2.Instance._link_codescope, context.player);
			payload_result.SetSlot(y, 1, vec2.Instance._link_codescope, context.player);
		}

		[NativeFunction("$geom.Vector2$private::NumbermulVec2")]
		public static void Vector2_NumbermulVec2(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			var vec2 = (ASClass)((RtScriptClass)context.GC.Heap[thisPtr.HeapPtr]).Meta;

			NaNBoxing s = scope.ReadSlot(0, context.player);
			NaNBoxing v1 = scope.ReadSlot(1, context.player);

			Debug.Assert(v1.ValueType == NaNBoxing.BoxType.HeapPtr);

			float scalar = (float)s.Number;

			var vector2_a = context.GC.Heap[v1.HeapPtr];
			var payload_a = (RtInstance)vector2_a;

			NaNBoxing x1 = payload_a.ReadSlot(0, vec2.Instance._link_codescope, context.player);
			NaNBoxing y1 = payload_a.ReadSlot(1, vec2.Instance._link_codescope, context.player);

			int resultptr = context.player.InitCacheInstance(vec2, returnSlotIndex, false);
			var vector2_result = context.GC.Heap[resultptr];
			var payload_result = (RtInstance)vector2_result;

			NaNBoxing x = default; x.SetFloat(scalar * x1.FloatValue);
			NaNBoxing y = default; y.SetFloat(scalar * y1.FloatValue);

			payload_result.SetSlot(x, 0, vec2.Instance._link_codescope, context.player);
			payload_result.SetSlot(y, 1, vec2.Instance._link_codescope, context.player);
		}


		[NativeFunction("$geom.Vector2$private::Vec2Neg")]
		public static void Vector2_Vec2Neg(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			var vec2 = (ASClass)((RtScriptClass)context.GC.Heap[thisPtr.HeapPtr]).Meta;

			NaNBoxing v = scope.ReadSlot(0, context.player);
			
			Debug.Assert(v.ValueType == NaNBoxing.BoxType.HeapPtr);

			
			var vector2_a = context.GC.Heap[v.HeapPtr];
			var payload_a = (RtInstance)vector2_a;

			NaNBoxing x1 = payload_a.ReadSlot(0, vec2.Instance._link_codescope, context.player);
			NaNBoxing y1 = payload_a.ReadSlot(1, vec2.Instance._link_codescope, context.player);

			int resultptr = context.player.InitCacheInstance(vec2, returnSlotIndex, false);
			var vector2_result = context.GC.Heap[resultptr];
			var payload_result = (RtInstance)vector2_result;

			x1.SetFloat(-x1.FloatValue);
			y1.SetFloat(-y1.FloatValue);

			payload_result.SetSlot(x1, 0, vec2.Instance._link_codescope, context.player);
			payload_result.SetSlot(y1, 1, vec2.Instance._link_codescope, context.player);
		}


		[NativeFunction("$geom.Vector2$private::Vec2Positive")]
		public static void Vector2_Vec2Positive(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			var vec2 = (ASClass)((RtScriptClass)context.GC.Heap[thisPtr.HeapPtr]).Meta;

			NaNBoxing v = scope.ReadSlot(0, context.player);
			
			Debug.Assert(v.ValueType == NaNBoxing.BoxType.HeapPtr);

			
			var vector2_a = context.GC.Heap[v.HeapPtr];
			var payload_a = (RtInstance)vector2_a;

			NaNBoxing x1 = payload_a.ReadSlot(0, vec2.Instance._link_codescope, context.player);
			NaNBoxing y1 = payload_a.ReadSlot(1, vec2.Instance._link_codescope, context.player);

			int resultptr = context.player.InitCacheInstance(vec2, returnSlotIndex, false);
			var vector2_result = context.GC.Heap[resultptr];
			var payload_result = (RtInstance)vector2_result;

			x1.SetFloat(+x1.FloatValue);
			y1.SetFloat(+y1.FloatValue);

			payload_result.SetSlot(x1, 0, vec2.Instance._link_codescope, context.player);
			payload_result.SetSlot(y1, 1, vec2.Instance._link_codescope, context.player);
		}





	}
}
