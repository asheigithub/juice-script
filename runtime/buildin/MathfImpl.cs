using juicescript.ABC;
using System;
using System.Runtime.CompilerServices;
using static juicescript.runtime.Player;

namespace juicescript.runtime.buildin
{
    internal class MathfImpl
    {
        [NativeFunction("$.Mathf$public::sin")]
        public static void Mathf_sin(Context context,
            ASMethod method,
            int scope_ptr,
            NaNBoxing thisPtr,
            int stackStPos, ref ReceiveError error, int returnSlotIndex)
        {
            var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
            var f = scope.ReadSlot(0, context.player);
            float val = Extensions.GetFloatValue(f);
            context.StackSlots[returnSlotIndex].SetFloat(MathF.Sin(val));
        }

        [NativeFunction("$.Mathf$public::cos")]
        public static void Mathf_cos(Context context,
            ASMethod method,
            int scope_ptr,
            NaNBoxing thisPtr,
            int stackStPos, ref ReceiveError error, int returnSlotIndex)
        {
            var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
            var f = scope.ReadSlot(0, context.player);
            float val = Extensions.GetFloatValue(f);
            context.StackSlots[returnSlotIndex].SetFloat(MathF.Cos(val));
        }

        [NativeFunction("$.Mathf$public::tan")]
        public static void Mathf_tan(Context context,
            ASMethod method,
            int scope_ptr,
            NaNBoxing thisPtr,
            int stackStPos, ref ReceiveError error, int returnSlotIndex)
        {
            var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
            var f = scope.ReadSlot(0, context.player);
            float val = Extensions.GetFloatValue(f);
            context.StackSlots[returnSlotIndex].SetFloat(MathF.Tan(val));
        }

        [NativeFunction("$.Mathf$public::asin")]
        public static void Mathf_asin(Context context,
            ASMethod method,
            int scope_ptr,
            NaNBoxing thisPtr,
            int stackStPos, ref ReceiveError error, int returnSlotIndex)
        {
            var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
            var f = scope.ReadSlot(0, context.player);
            float val = Extensions.GetFloatValue(f);
            context.StackSlots[returnSlotIndex].SetFloat(MathF.Asin(val));
        }

        [NativeFunction("$.Mathf$public::acos")]
        public static void Mathf_acos(Context context,
            ASMethod method,
            int scope_ptr,
            NaNBoxing thisPtr,
            int stackStPos, ref ReceiveError error, int returnSlotIndex)
        {
            var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
            var f = scope.ReadSlot(0, context.player);
            float val = Extensions.GetFloatValue(f);
            context.StackSlots[returnSlotIndex].SetFloat(MathF.Acos(val));
        }

        [NativeFunction("$.Mathf$public::atan")]
        public static void Mathf_atan(Context context,
            ASMethod method,
            int scope_ptr,
            NaNBoxing thisPtr,
            int stackStPos, ref ReceiveError error, int returnSlotIndex)
        {
            var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
            var f = scope.ReadSlot(0, context.player);
            float val = Extensions.GetFloatValue(f);
            context.StackSlots[returnSlotIndex].SetFloat(MathF.Atan(val));
        }

        [NativeFunction("$.Mathf$public::atan2")]
        public static void Mathf_atan2(Context context,
            ASMethod method,
            int scope_ptr,
            NaNBoxing thisPtr,
            int stackStPos, ref ReceiveError error, int returnSlotIndex)
        {
            var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
            var y = Extensions.GetFloatValue(scope.ReadSlot(0, context.player));
            var x = Extensions.GetFloatValue(scope.ReadSlot(1, context.player));
            context.StackSlots[returnSlotIndex].SetFloat(MathF.Atan2(y, x));
        }

        [NativeFunction("$.Mathf$public::ceil")]
        public static void Mathf_ceil(Context context,
            ASMethod method,
            int scope_ptr,
            NaNBoxing thisPtr,
            int stackStPos, ref ReceiveError error, int returnSlotIndex)
        {
            var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
            var f = Extensions.GetFloatValue(scope.ReadSlot(0, context.player));
            context.StackSlots[returnSlotIndex].SetFloat(MathF.Ceiling(f));
        }

        [NativeFunction("$.Mathf$public::ceilToInt")]
        public static void Mathf_ceilToInt(Context context,
            ASMethod method,
            int scope_ptr,
            NaNBoxing thisPtr,
            int stackStPos, ref ReceiveError error, int returnSlotIndex)
        {
            var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
            var f = Extensions.GetFloatValue(scope.ReadSlot(0, context.player));
            context.StackSlots[returnSlotIndex].SetInt((int)MathF.Ceiling(f));
        }

        [NativeFunction("$.Mathf$public::floor")]
        public static void Mathf_floor(Context context,
            ASMethod method,
            int scope_ptr,
            NaNBoxing thisPtr,
            int stackStPos, ref ReceiveError error, int returnSlotIndex)
        {
            var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
            var f = Extensions.GetFloatValue(scope.ReadSlot(0, context.player));
            context.StackSlots[returnSlotIndex].SetFloat(MathF.Floor(f));
        }

        [NativeFunction("$.Mathf$public::floorToInt")]
        public static void Mathf_floorToInt(Context context,
            ASMethod method,
            int scope_ptr,
            NaNBoxing thisPtr,
            int stackStPos, ref ReceiveError error, int returnSlotIndex)
        {
            var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
            var f = Extensions.GetFloatValue(scope.ReadSlot(0, context.player));
            context.StackSlots[returnSlotIndex].SetInt((int)MathF.Floor(f));
        }

        [NativeFunction("$.Mathf$public::round")]
        public static void Mathf_round(Context context,
            ASMethod method,
            int scope_ptr,
            NaNBoxing thisPtr,
            int stackStPos, ref ReceiveError error, int returnSlotIndex)
        {
            var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
            var f = Extensions.GetFloatValue(scope.ReadSlot(0, context.player));
            context.StackSlots[returnSlotIndex].SetFloat(MathF.Round(f));
        }

        [NativeFunction("$.Mathf$public::roundToInt")]
        public static void Mathf_roundToInt(Context context,
            ASMethod method,
            int scope_ptr,
            NaNBoxing thisPtr,
            int stackStPos, ref ReceiveError error, int returnSlotIndex)
        {
            var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
            var f = Extensions.GetFloatValue(scope.ReadSlot(0, context.player));
            context.StackSlots[returnSlotIndex].SetInt((int)MathF.Round(f));
        }

        [NativeFunction("$.Mathf$public::abs")]
        public static void Mathf_abs(Context context,
            ASMethod method,
            int scope_ptr,
            NaNBoxing thisPtr,
            int stackStPos, ref ReceiveError error, int returnSlotIndex)
        {
            var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
            var f = Extensions.GetFloatValue(scope.ReadSlot(0, context.player));
            context.StackSlots[returnSlotIndex].SetFloat(MathF.Abs(f));
        }

        [NativeFunction("$.Mathf$public::sign")]
        public static void Mathf_sign(Context context,
            ASMethod method,
            int scope_ptr,
            NaNBoxing thisPtr,
            int stackStPos, ref ReceiveError error, int returnSlotIndex)
        {
            var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
            var f = Extensions.GetFloatValue(scope.ReadSlot(0, context.player));
            context.StackSlots[returnSlotIndex].SetFloat(MathF.Sign(f));
        }

        [NativeFunction("$.Mathf$public::max")]
        public static void Mathf_max(Context context,
            ASMethod method,
            int scope_ptr,
            NaNBoxing thisPtr,
            int stackStPos, ref ReceiveError error, int returnSlotIndex)
        {
            var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
            var a = Extensions.GetFloatValue(scope.ReadSlot(0, context.player));
            var b = Extensions.GetFloatValue(scope.ReadSlot(1, context.player));
            context.StackSlots[returnSlotIndex].SetFloat(MathF.Max(a, b));
        }

        [NativeFunction("$.Mathf$public::min")]
        public static void Mathf_min(Context context,
            ASMethod method,
            int scope_ptr,
            NaNBoxing thisPtr,
            int stackStPos, ref ReceiveError error, int returnSlotIndex)
        {
            var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
            var a = Extensions.GetFloatValue(scope.ReadSlot(0, context.player));
            var b = Extensions.GetFloatValue(scope.ReadSlot(1, context.player));
            context.StackSlots[returnSlotIndex].SetFloat(MathF.Min(a, b));
        }

        [NativeFunction("$.Mathf$public::clamp")]
        public static void Mathf_clamp(Context context,
            ASMethod method,
            int scope_ptr,
            NaNBoxing thisPtr,
            int stackStPos, ref ReceiveError error, int returnSlotIndex)
        {
            var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
            var value = Extensions.GetFloatValue(scope.ReadSlot(0, context.player));
            var min = Extensions.GetFloatValue(scope.ReadSlot(1, context.player));
            var max = Extensions.GetFloatValue(scope.ReadSlot(2, context.player));
            context.StackSlots[returnSlotIndex].SetFloat(MathF.Max(min, MathF.Min(max, value)));
        }

        [NativeFunction("$.Mathf$public::clamp01")]
        public static void Mathf_clamp01(Context context,
            ASMethod method,
            int scope_ptr,
            NaNBoxing thisPtr,
            int stackStPos, ref ReceiveError error, int returnSlotIndex)
        {
            var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
            var value = Extensions.GetFloatValue(scope.ReadSlot(0, context.player));
            context.StackSlots[returnSlotIndex].SetFloat(MathF.Max(0, MathF.Min(1, value)));
        }

        [NativeFunction("$.Mathf$public::repeat")]
        public static void Mathf_repeat(Context context,
            ASMethod method,
            int scope_ptr,
            NaNBoxing thisPtr,
            int stackStPos, ref ReceiveError error, int returnSlotIndex)
        {
            var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
            var t = Extensions.GetFloatValue(scope.ReadSlot(0, context.player));
            var length = Extensions.GetFloatValue(scope.ReadSlot(1, context.player));
            if (length == 0) 
            {
                context.StackSlots[returnSlotIndex].SetFloat(0);
                return;
            }
            float result = t % length;
            if (result < 0) result += length;
            context.StackSlots[returnSlotIndex].SetFloat(result);
        }

        [NativeFunction("$.Mathf$public::pingPong")]
        public static void Mathf_pingPong(Context context,
            ASMethod method,
            int scope_ptr,
            NaNBoxing thisPtr,
            int stackStPos, ref ReceiveError error, int returnSlotIndex)
        {
            var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
            var t = Extensions.GetFloatValue(scope.ReadSlot(0, context.player));
            var length = Extensions.GetFloatValue(scope.ReadSlot(1, context.player));
            if (length == 0)
            {
                context.StackSlots[returnSlotIndex].SetFloat(0);
                return;
            }
            float result = t % (length * 2);
            if (result < 0) result += length * 2;
            if (result > length) result = length * 2 - result;
            context.StackSlots[returnSlotIndex].SetFloat(result);
        }

        [NativeFunction("$.Mathf$public::lerp")]
        public static void Mathf_lerp(Context context,
            ASMethod method,
            int scope_ptr,
            NaNBoxing thisPtr,
            int stackStPos, ref ReceiveError error, int returnSlotIndex)
        {
            var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
            var a = Extensions.GetFloatValue(scope.ReadSlot(0, context.player));
            var b = Extensions.GetFloatValue(scope.ReadSlot(1, context.player));
            var t = Extensions.GetFloatValue(scope.ReadSlot(2, context.player));
            if (t < 0) t = 0;
            if (t > 1) t = 1;
            context.StackSlots[returnSlotIndex].SetFloat(a + (b - a) * t);
        }

        [NativeFunction("$.Mathf$public::lerpUnclamped")]
        public static void Mathf_lerpUnclamped(Context context,
            ASMethod method,
            int scope_ptr,
            NaNBoxing thisPtr,
            int stackStPos, ref ReceiveError error, int returnSlotIndex)
        {
            var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
            var a = Extensions.GetFloatValue(scope.ReadSlot(0, context.player));
            var b = Extensions.GetFloatValue(scope.ReadSlot(1, context.player));
            var t = Extensions.GetFloatValue(scope.ReadSlot(2, context.player));
            context.StackSlots[returnSlotIndex].SetFloat(a + (b - a) * t);
        }

        [NativeFunction("$.Mathf$public::lerpAngle")]
        public static void Mathf_lerpAngle(Context context,
            ASMethod method,
            int scope_ptr,
            NaNBoxing thisPtr,
            int stackStPos, ref ReceiveError error, int returnSlotIndex)
        {
            var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
            var a = Extensions.GetFloatValue(scope.ReadSlot(0, context.player));
            var b = Extensions.GetFloatValue(scope.ReadSlot(1, context.player));
            var t = Extensions.GetFloatValue(scope.ReadSlot(2, context.player));
            var delta = DeltaAngle(a, b);
            if (t < 0) t = 0;
            if (t > 1) t = 1;
            context.StackSlots[returnSlotIndex].SetFloat(a + delta * t);
        }

        [NativeFunction("$.Mathf$public::inverseLerp")]
        public static void Mathf_inverseLerp(Context context,
            ASMethod method,
            int scope_ptr,
            NaNBoxing thisPtr,
            int stackStPos, ref ReceiveError error, int returnSlotIndex)
        {
            var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
            var a = Extensions.GetFloatValue(scope.ReadSlot(0, context.player));
            var b = Extensions.GetFloatValue(scope.ReadSlot(1, context.player));
            var value = Extensions.GetFloatValue(scope.ReadSlot(2, context.player));
            if (MathF.Abs(a - b) < float.Epsilon)
            {
                context.StackSlots[returnSlotIndex].SetFloat(0);
                return;
            }
            context.StackSlots[returnSlotIndex].SetFloat((value - a) / (b - a));
        }

        [NativeFunction("$.Mathf$public::smoothStep")]
        public static void Mathf_smoothStep(Context context,
            ASMethod method,
            int scope_ptr,
            NaNBoxing thisPtr,
            int stackStPos, ref ReceiveError error, int returnSlotIndex)
        {
            var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
            var from = Extensions.GetFloatValue(scope.ReadSlot(0, context.player));
            var to = Extensions.GetFloatValue(scope.ReadSlot(1, context.player));
            var t = Extensions.GetFloatValue(scope.ReadSlot(2, context.player));
            if (t <= 0) 
            {
                context.StackSlots[returnSlotIndex].SetFloat(from);
                return;
            }
            if (t >= 1)
            {
                context.StackSlots[returnSlotIndex].SetFloat(to);
                return;
            }
            float tClamped = t < 0 ? 0 : (t > 1 ? 1 : t);
            float tSmooth = tClamped * tClamped * (3 - 2 * tClamped);
            context.StackSlots[returnSlotIndex].SetFloat(from + (to - from) * tSmooth);
        }

        [NativeFunction("$.Mathf$public::moveTowards")]
        public static void Mathf_moveTowards(Context context,
            ASMethod method,
            int scope_ptr,
            NaNBoxing thisPtr,
            int stackStPos, ref ReceiveError error, int returnSlotIndex)
        {
            var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
            var current = Extensions.GetFloatValue(scope.ReadSlot(0, context.player));
            var target = Extensions.GetFloatValue(scope.ReadSlot(1, context.player));
            var maxDelta = Extensions.GetFloatValue(scope.ReadSlot(2, context.player));
            if (MathF.Abs(target - current) <= maxDelta)
            {
                context.StackSlots[returnSlotIndex].SetFloat(target);
                return;
            }
            context.StackSlots[returnSlotIndex].SetFloat(current + MathF.Sign(target - current) * maxDelta);
        }

        [NativeFunction("$.Mathf$public::moveTowardsAngle")]
        public static void Mathf_moveTowardsAngle(Context context,
            ASMethod method,
            int scope_ptr,
            NaNBoxing thisPtr,
            int stackStPos, ref ReceiveError error, int returnSlotIndex)
        {
            var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
            var current = Extensions.GetFloatValue(scope.ReadSlot(0, context.player));
            var target = Extensions.GetFloatValue(scope.ReadSlot(1, context.player));
            var maxDelta = Extensions.GetFloatValue(scope.ReadSlot(2, context.player));
            var delta = DeltaAngle(current, target);
            if (MathF.Abs(delta) <= maxDelta)
            {
                context.StackSlots[returnSlotIndex].SetFloat(target);
                return;
            }
            context.StackSlots[returnSlotIndex].SetFloat(current + MathF.Sign(delta) * maxDelta);
        }

        [NativeFunction("$.Mathf$public::deltaAngle")]
        public static void Mathf_deltaAngle(Context context,
            ASMethod method,
            int scope_ptr,
            NaNBoxing thisPtr,
            int stackStPos, ref ReceiveError error, int returnSlotIndex)
        {
            var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
            var current = Extensions.GetFloatValue(scope.ReadSlot(0, context.player));
            var target = Extensions.GetFloatValue(scope.ReadSlot(1, context.player));
            context.StackSlots[returnSlotIndex].SetFloat(DeltaAngle(current, target));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float DeltaAngle(float current, float target)
        {
            float delta = (target - current) % 360;
            if (delta > 180) delta -= 360;
            if (delta < -180) delta += 360;
            return delta;
        }

        [NativeFunction("$.Mathf$public::pow")]
        public static void Mathf_pow(Context context,
            ASMethod method,
            int scope_ptr,
            NaNBoxing thisPtr,
            int stackStPos, ref ReceiveError error, int returnSlotIndex)
        {
            var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
            var f = Extensions.GetFloatValue(scope.ReadSlot(0, context.player));
            var p = Extensions.GetFloatValue(scope.ReadSlot(1, context.player));
            context.StackSlots[returnSlotIndex].SetFloat(MathF.Pow(f, p));
        }

        [NativeFunction("$.Mathf$public::exp")]
        public static void Mathf_exp(Context context,
            ASMethod method,
            int scope_ptr,
            NaNBoxing thisPtr,
            int stackStPos, ref ReceiveError error, int returnSlotIndex)
        {
            var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
            var power = Extensions.GetFloatValue(scope.ReadSlot(0, context.player));
            context.StackSlots[returnSlotIndex].SetFloat(MathF.Exp(power));
        }

        [NativeFunction("$.Mathf$public::sqrt")]
        public static void Mathf_sqrt(Context context,
            ASMethod method,
            int scope_ptr,
            NaNBoxing thisPtr,
            int stackStPos, ref ReceiveError error, int returnSlotIndex)
        {
            var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
            var f = Extensions.GetFloatValue(scope.ReadSlot(0, context.player));
            context.StackSlots[returnSlotIndex].SetFloat(MathF.Sqrt(f));
        }

        [NativeFunction("$.Mathf$public::log")]
        public static void Mathf_log(Context context,
            ASMethod method,
            int scope_ptr,
            NaNBoxing thisPtr,
            int stackStPos, ref ReceiveError error, int returnSlotIndex)
        {
            var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
            var f = Extensions.GetFloatValue(scope.ReadSlot(0, context.player));
            context.StackSlots[returnSlotIndex].SetFloat(MathF.Log(f));
        }

        [NativeFunction("$.Mathf$public::logBase")]
        public static void Mathf_logBase(Context context,
            ASMethod method,
            int scope_ptr,
            NaNBoxing thisPtr,
            int stackStPos, ref ReceiveError error, int returnSlotIndex)
        {
            var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
            var f = Extensions.GetFloatValue(scope.ReadSlot(0, context.player));
            var p = Extensions.GetFloatValue(scope.ReadSlot(1, context.player));
            context.StackSlots[returnSlotIndex].SetFloat(MathF.Log(f) / MathF.Log(p));
        }

        [NativeFunction("$.Mathf$public::log10")]
        public static void Mathf_log10(Context context,
            ASMethod method,
            int scope_ptr,
            NaNBoxing thisPtr,
            int stackStPos, ref ReceiveError error, int returnSlotIndex)
        {
            var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
            var f = Extensions.GetFloatValue(scope.ReadSlot(0, context.player));
            context.StackSlots[returnSlotIndex].SetFloat(MathF.Log10(f));
        }

        [NativeFunction("$.Mathf$public::approximately")]
        public static void Mathf_approximately(Context context,
            ASMethod method,
            int scope_ptr,
            NaNBoxing thisPtr,
            int stackStPos, ref ReceiveError error, int returnSlotIndex)
        {
            var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
            var a = Extensions.GetFloatValue(scope.ReadSlot(0, context.player));
            var b = Extensions.GetFloatValue(scope.ReadSlot(1, context.player));
            context.StackSlots[returnSlotIndex].SetBoolean(MathF.Abs(a - b) <= float.Epsilon);
        }

        [NativeFunction("$.Mathf$public::isPowerOfTwo")]
        public static void Mathf_isPowerOfTwo(Context context,
            ASMethod method,
            int scope_ptr,
            NaNBoxing thisPtr,
            int stackStPos, ref ReceiveError error, int returnSlotIndex)
        {
            var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
            var value = scope.ReadSlot(0, context.player).IntValue;
            context.StackSlots[returnSlotIndex].SetBoolean((value & (value - 1)) == 0 && value > 0);
        }

        [NativeFunction("$.Mathf$public::nextPowerOfTwo")]
        public static void Mathf_nextPowerOfTwo(Context context,
            ASMethod method,
            int scope_ptr,
            NaNBoxing thisPtr,
            int stackStPos, ref ReceiveError error, int returnSlotIndex)
        {
            var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
            var value = scope.ReadSlot(0, context.player).IntValue;
            if (value <= 0)
            {
                context.StackSlots[returnSlotIndex].SetInt(1);
                return;
            }
            value--;
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;
            value++;
            context.StackSlots[returnSlotIndex].SetInt(value);
        }
    }
}
