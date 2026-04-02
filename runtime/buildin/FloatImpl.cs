using Jint.Native.Number.Dtoa;
using juicescript.ABC;
using juicescript.runtime.buildin.Pooling;
using System;
using System.Buffers;
using System.Diagnostics;
using static juicescript.runtime.Player;

namespace juicescript.runtime.buildin
{
    internal class FloatImpl
    {
        private const string Digits = "0123456789abcdefghijklmnopqrstuvwxyz";

        [NativeFunction(".float$public::valueOf")]
        public static void Float_valueOf(Context context,
            ASMethod method,
            int scope_ptr,
            NaNBoxing thisPtr,
            int stackStPos, ref ReceiveError error, int returnSlotIndex)
        {
#if DEBUG
            if (thisPtr.ValueType != NaNBoxing.BoxType.Float)
                throw new InvalidOperationException();
#endif

            context.StackSlots[returnSlotIndex] = thisPtr;
        }

        [NativeFunction(".float$public::toString")]
        public static void Float_toString(Context context,
            ASMethod method,
            int scope_ptr,
            NaNBoxing thisPtr,
            int stackStPos, ref ReceiveError error, int returnSlotIndex)
        {
#if DEBUG
            if (thisPtr.ValueType != NaNBoxing.BoxType.Float)
                throw new InvalidOperationException();
#endif

            var scope = (RtPayloadMethodScope)context.GC.Heap[scope_ptr].facility;
            var radix = scope.ReadSlot(0, context.player);

            Debug.Assert(radix.ValueType == NaNBoxing.BoxType.Int);
            var radixValue = radix.IntValue;

            if (radixValue < 2 || radixValue > 36)
            {
                context.player.RaiseRangeError(ref error, "radix must be between 2 and 36");
                return;
            }

            float x = thisPtr.FloatValue;

            string str = FloatToString(x, radixValue);

            int str_ptr = context.GC.AllocString(str);
            if (str_ptr == 0)
            {
                context.player.RaiseOutOfMemory(ref error);
                return;
            }

            context.StackSlots[returnSlotIndex].SetHeapPtr(str_ptr);
        }

        private static string FloatToString(float n, int radix)
        {
            if (float.IsNaN(n))
            {
                return "NaN";
            }

            if (n == 0)
            {
                return "0";
            }

            if (float.IsInfinity(n))
            {
                return float.IsNegativeInfinity(n) ? "-Infinity" : "Infinity";
            }

            bool negative = false;
            if (n < 0)
            {
                negative = true;
                n = -n;
            }

            if (radix == 10)
            {
                return negative ? "-" + FloatToStringDecimal(n) : FloatToStringDecimal(n);
            }

            int intPart = (int)MathF.Truncate(n);
            float fracPart = n - intPart;

            string result = IntToString(intPart, radix);
            if (fracPart > 0)
            {
                result += "." + FloatFractionToString(fracPart, radix);
            }

            return negative ? "-" + result : result;
        }

        private static string FloatToStringDecimal(float n)
        {
            if (n == 0) return "0";

            int intPart = (int)MathF.Truncate(n);
            float fracPart = n - intPart;

            string result = intPart.ToString();
            if (fracPart > 0)
            {
                var sb = new ValueStringBuilder(stackalloc char[32]);
                for (int i = 0; i < 7 && fracPart > 0; i++)
                {
                    fracPart *= 10;
                    int digit = (int)MathF.Truncate(fracPart);
                    sb.Append(Digits[digit]);
                    fracPart -= digit;
                }
                result = intPart.ToString() + "." + sb.ToString().TrimEnd('0');
            }

            return result;
        }

        private static string IntToString(int n, int radix)
        {
            if (n == 0)
            {
                return "0";
            }

            var sb = new ValueStringBuilder(stackalloc char[32]);
            while (n > 0)
            {
                int digit = n % radix;
                sb.Append(Digits[digit]);
                n /= radix;
            }

            sb.Reverse();
            return sb.ToString();
        }

        private static string FloatFractionToString(float n, int radix)
        {
            var sb = new ValueStringBuilder(stackalloc char[32]);
            for (int i = 0; i < 10 && n > 0; i++)
            {
                n *= radix;
                int digit = (int)MathF.Truncate(n);
                sb.Append(Digits[digit]);
                n -= digit;
            }
            return sb.ToString();
        }

        private static string CreateExponentialRepresentation(
            ref DtoaBuilder buffer,
            int exponent,
            bool negative,
            int significantDigits)
        {
            bool negativeExponent = false;
            if (exponent < 0)
            {
                negativeExponent = true;
                exponent = -exponent;
            }

            var sb = new ValueStringBuilder(stackalloc char[128]);
            if (negative)
            {
                sb.Append('-');
            }
            sb.Append(buffer[0]);
            if (significantDigits != 1)
            {
                sb.Append('.');
                sb.Append(buffer.Slice(1, buffer.Length - 1));
                int length = buffer.Length;
                sb.Append('0', significantDigits - length);
            }

            sb.Append('e');
            sb.Append(negativeExponent ? '-' : '+');
            sb.Append(exponent);

            return sb.ToString();
        }

        [NativeFunction(".float$public::toExponential")]
        public static void Float_toExponential(Context context,
            ASMethod method,
            int scope_ptr,
            NaNBoxing thisPtr,
            int stackStPos, ref ReceiveError error, int returnSlotIndex)
        {
#if DEBUG
            if (thisPtr.ValueType != NaNBoxing.BoxType.Float)
                throw new InvalidOperationException();
#endif

            var scope = (RtPayloadMethodScope)context.GC.Heap[scope_ptr].facility;
            var fractionDigits = scope.ReadSlot(0, context.player);

            Debug.Assert(fractionDigits.ValueType == NaNBoxing.BoxType.Int);
            var f = fractionDigits.IntValue;

            if (f < 0 || f > 20)
            {
                context.player.RaiseRangeError(ref error, "digits must be between 0 and 20");
                return;
            }

            double x = thisPtr.FloatValue;

            var dtoaBuilder = new DtoaBuilder(stackalloc char[101]);
            DtoaNumberFormatter.DoubleToAscii(
                ref dtoaBuilder,
                x,
                DtoaMode.Precision,
                requested_digits: f + 1,
                out _,
                out var decimalPoint);

            Debug.Assert(dtoaBuilder.Length > 0);
            Debug.Assert(dtoaBuilder.Length <= f + 1);

            int exponent = decimalPoint - 1;
            var result = CreateExponentialRepresentation(ref dtoaBuilder, exponent, x < 0, f + 1);

            int str_ptr = context.GC.AllocString(result);
            if (str_ptr == 0)
            {
                context.player.RaiseOutOfMemory(ref error);
                return;
            }

            context.StackSlots[returnSlotIndex].SetHeapPtr(str_ptr);
        }

        [NativeFunction(".float$public::toFixed")]
        public static void Float_toFixed(Context context,
            ASMethod method,
            int scope_ptr,
            NaNBoxing thisPtr,
            int stackStPos, ref ReceiveError error, int returnSlotIndex)
        {
#if DEBUG
            if (thisPtr.ValueType != NaNBoxing.BoxType.Float)
                throw new InvalidOperationException();
#endif

            var scope = (RtPayloadMethodScope)context.GC.Heap[scope_ptr].facility;
            var digits = scope.ReadSlot(0, context.player);

            Debug.Assert(digits.ValueType == NaNBoxing.BoxType.Int);

            int f = digits.IntValue;

            if (f < 0 || f > 20)
            {
                context.player.RaiseRangeError(ref error, "digits must be between 0 and 20");
                return;
            }

            float n = thisPtr.FloatValue;

            string str = FloatToFixedString(n, f);

            int str_ptr = context.GC.AllocString(str);
            if (str_ptr == 0)
            {
                context.player.RaiseOutOfMemory(ref error);
                return;
            }

            context.StackSlots[returnSlotIndex].SetHeapPtr(str_ptr);
        }

        private static string FloatToFixedString(float n, int fractionDigits)
        {
            if (float.IsNaN(n))
            {
                return "NaN";
            }

            if (float.IsInfinity(n))
            {
                return float.IsNegativeInfinity(n) ? "-Infinity" : "Infinity";
            }

            bool negative = false;
            if (n < 0)
            {
                negative = true;
                n = -n;
            }

            if (fractionDigits == 0)
            {
                int rounded = (int)MathF.Round(n);
                return negative ? "-" + rounded : rounded.ToString();
            }

            int intPart = (int)MathF.Truncate(n);
            float fracPart = n - intPart;

            var sb = new ValueStringBuilder(stackalloc char[32]);
            if (negative)
            {
                sb.Append('-');
            }

            sb.Append(intPart);
            sb.Append('.');

            for (int i = 0; i < fractionDigits; i++)
            {
                fracPart *= 10;
                int digit = (int)MathF.Truncate(fracPart);
                sb.Append(Digits[digit]);
                fracPart -= digit;
            }

            return sb.ToString();
        }

        [NativeFunction(".float$public::toPrecision")]
        public static void Float_toPrecision(Context context,
            ASMethod method,
            int scope_ptr,
            NaNBoxing thisPtr,
            int stackStPos, ref ReceiveError error, int returnSlotIndex)
        {
#if DEBUG
            if (thisPtr.ValueType != NaNBoxing.BoxType.Float)
                throw new InvalidOperationException();
#endif

            var scope = (RtPayloadMethodScope)context.GC.Heap[scope_ptr].facility;
            var precision = scope.ReadSlot(0, context.player);
            Debug.Assert(precision.ValueType == NaNBoxing.BoxType.Int);

            int p = precision.IntValue;

            if (p < 1 || p > 21)
            {
                context.player.RaiseRangeError(ref error, "precision must be between 1 and 21");
                return;
            }

            double x = thisPtr.FloatValue;

            var dtoaBuilder = new DtoaBuilder(stackalloc char[101]);
            DtoaNumberFormatter.DoubleToAscii(
                ref dtoaBuilder,
                x,
                DtoaMode.Precision,
                p,
                out var negative,
                out var decimalPoint);

            int exponent = decimalPoint - 1;
            if (exponent < -6 || exponent >= p)
            {
                string str = CreateExponentialRepresentation(ref dtoaBuilder, exponent, negative, p);

                int str_ptr = context.GC.AllocString(str);
                if (str_ptr == 0)
                {
                    context.player.RaiseOutOfMemory(ref error);
                    return;
                }

                context.StackSlots[returnSlotIndex].SetHeapPtr(str_ptr);
            }
            else
            {
                var sb = new ValueStringBuilder(stackalloc char[128]);

                if (decimalPoint <= 0)
                {
                    sb.Append("0.");
                    sb.Append('0', -decimalPoint);
                    sb.Append(dtoaBuilder._chars.Slice(0, dtoaBuilder.Length));
                    sb.Append('0', p - dtoaBuilder.Length);
                }
                else
                {
                    int m = System.Math.Min(dtoaBuilder.Length, decimalPoint);
                    sb.Append(dtoaBuilder._chars.Slice(0, m));
                    sb.Append('0', System.Math.Max(0, decimalPoint - dtoaBuilder.Length));
                    if (decimalPoint < p)
                    {
                        sb.Append('.');
                        var extra = 1;
                        if (dtoaBuilder.Length > decimalPoint)
                        {
                            int len = dtoaBuilder.Length - decimalPoint;
                            int n = System.Math.Min(len, p - (sb.Length - extra));
                            sb.Append(dtoaBuilder._chars.Slice(decimalPoint, n));
                        }

                        sb.Append('0', System.Math.Max(0, extra + (p - sb.Length)));
                    }
                }

                string str = sb.ToString();

                int str_ptr = context.GC.AllocString(str);
                if (str_ptr == 0)
                {
                    context.player.RaiseOutOfMemory(ref error);
                    return;
                }

                context.StackSlots[returnSlotIndex].SetHeapPtr(str_ptr);
            }
        }
    }
}
