using Jint.Native.Number.Dtoa;
using juicescript.ABC;
using juicescript.runtime.buildin.Pooling;
using System;
using System.Buffers;
using System.Diagnostics;
using static juicescript.runtime.Player;

namespace juicescript.runtime.buildin
{
    internal class ShortImpl
    {
        private const string Digits = "0123456789abcdefghijklmnopqrstuvwxyz";

        [NativeFunction(".short$:AS3::valueOf")]
        public static void Short_valueOf(Context context,
            ASMethod method,
            int scope_ptr,
            NaNBoxing thisPtr,
            int stackStPos, ref ReceiveError error, int returnSlotIndex)
        {
#if DEBUG
            if (thisPtr.ValueType != NaNBoxing.BoxType.Short)
                throw new InvalidOperationException();
#endif

            context.StackSlots[returnSlotIndex] = thisPtr;
        }

        [NativeFunction(".short$public::valueOf")]
        public static void Short_valueOf_Public(Context context,
            ASMethod method,
            int scope_ptr,
            NaNBoxing thisPtr,
            int stackStPos, ref ReceiveError error, int returnSlotIndex)
        {
#if DEBUG
            if (thisPtr.ValueType != NaNBoxing.BoxType.Short)
                throw new InvalidOperationException();
#endif

            context.StackSlots[returnSlotIndex] = thisPtr;
        }

        [NativeFunction(".short$public::toString")]
        public static void Short_toString(Context context,
            ASMethod method,
            int scope_ptr,
            NaNBoxing thisPtr,
            int stackStPos, ref ReceiveError error, int returnSlotIndex)
        {
#if DEBUG
            if (thisPtr.ValueType != NaNBoxing.BoxType.Short)
                throw new InvalidOperationException();
#endif

            var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
            var radix = scope.ReadSlot(0);

            Debug.Assert(radix.ValueType == NaNBoxing.BoxType.Int);
            var radixValue = radix.IntValue;

            if (radixValue < 2 || radixValue > 36)
            {
                context.player.RaiseRangeError(ref error, "radix must be between 2 and 36");
                return;
            }

            short x = thisPtr.ShortValue;

            var str = IntImpl.IntToString(x, radixValue,stackalloc char[64]);

			if (context.player.TryCreateStringValue(str, out NaNBoxing r, ref error))
			{
				context.StackSlots[returnSlotIndex] = r;
			}
		}

        //private static string ShortToString(short n, int radix)
        //{
        //    if (n == 0)
        //    {
        //        return "0";
        //    }

        //    bool negative = false;
        //    ushort u;

        //    if (n < 0)
        //    {
        //        negative = true;
        //        if (n == short.MinValue)
        //        {
        //            u = 0x8000;
        //        }
        //        else
        //        {
        //            u = (ushort)(-n);
        //        }
        //    }
        //    else
        //    {
        //        u = (ushort)n;
        //    }

        //    var sb = new ValueStringBuilder(stackalloc char[32]);

        //    while (u > 0)
        //    {
        //        ushort digit = (ushort)(u % (ushort)radix);
        //        sb.Append(Digits[digit]);
        //        u /= (ushort)radix;
        //    }

        //    if (negative)
        //    {
        //        sb.Append('-');
        //    }

        //    sb.Reverse();
        //    return sb.ToString();
        //}

        //private static string CreateExponentialRepresentation(
        //    ref DtoaBuilder buffer,
        //    int exponent,
        //    bool negative,
        //    int significantDigits)
        //{
        //    bool negativeExponent = false;
        //    if (exponent < 0)
        //    {
        //        negativeExponent = true;
        //        exponent = -exponent;
        //    }

        //    var sb = new ValueStringBuilder(stackalloc char[128]);
        //    if (negative)
        //    {
        //        sb.Append('-');
        //    }
        //    sb.Append(buffer[0]);
        //    if (significantDigits != 1)
        //    {
        //        sb.Append('.');
        //        sb.Append(buffer.Slice(1, buffer.Length - 1));
        //        int length = buffer.Length;
        //        sb.Append('0', significantDigits - length);
        //    }

        //    sb.Append('e');
        //    sb.Append(negativeExponent ? '-' : '+');
        //    sb.Append(exponent);

        //    return sb.ToString();
        //}

        [NativeFunction(".short$public::toExponential")]
        public static void Short_toExponential(Context context,
            ASMethod method,
            int scope_ptr,
            NaNBoxing thisPtr,
            int stackStPos, ref ReceiveError error, int returnSlotIndex)
        {
#if DEBUG
            if (thisPtr.ValueType != NaNBoxing.BoxType.Short)
                throw new InvalidOperationException();
#endif

            var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
            var fractionDigits = scope.ReadSlot(0);

            Debug.Assert(fractionDigits.ValueType == NaNBoxing.BoxType.Int);
            var f = fractionDigits.IntValue;

            if (f < 0 || f > 20)
            {
                context.player.RaiseRangeError(ref error, "digits must be between 0 and 20");
                return;
            }

            double x = thisPtr.ShortValue;

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
            var result = IntImpl.CreateExponentialRepresentation(ref dtoaBuilder, exponent, x < 0, f + 1,stackalloc char[128]);

			if (context.player.TryCreateStringValue(result, out NaNBoxing r, ref error))
			{
				context.StackSlots[returnSlotIndex] = r;
			}
		}

        [NativeFunction(".short$public::toFixed")]
        public static void Short_toFixed(Context context,
            ASMethod method,
            int scope_ptr,
            NaNBoxing thisPtr,
            int stackStPos, ref ReceiveError error, int returnSlotIndex)
        {
#if DEBUG
            if (thisPtr.ValueType != NaNBoxing.BoxType.Short)
                throw new InvalidOperationException();
#endif

            var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
            var digits = scope.ReadSlot(0);

            Debug.Assert(digits.ValueType == NaNBoxing.BoxType.Int);

            int f = digits.IntValue;

            if (f < 0 || f > 20)
            {
                context.player.RaiseRangeError(ref error, "digits must be between 0 and 20");
                return;
            }

            short n = thisPtr.ShortValue;

			Span<char> buffer = stackalloc char[64];
			var str = IntImpl.IntToString(n, 10, buffer);

			if (f > 0)
			{
				buffer[str.Length] = '.';
				buffer.Slice(str.Length + 1, f).Fill('0');

				str = buffer.Slice(0, str.Length + 1 + f);

				//str = str + "." + new string('0', f);
			}

			if (context.player.TryCreateStringValue(str, out NaNBoxing r, ref error))
			{
				context.StackSlots[returnSlotIndex] = r;
			}
		}

        [NativeFunction(".short$public::toPrecision")]
        public static void Short_toPrecision(Context context,
            ASMethod method,
            int scope_ptr,
            NaNBoxing thisPtr,
            int stackStPos, ref ReceiveError error, int returnSlotIndex)
        {
#if DEBUG
            if (thisPtr.ValueType != NaNBoxing.BoxType.Short)
                throw new InvalidOperationException();
#endif

            var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
            var precision = scope.ReadSlot(0);
            Debug.Assert(precision.ValueType == NaNBoxing.BoxType.Int);

            int p = precision.IntValue;

            if (p < 1 || p > 21)
            {
                context.player.RaiseRangeError(ref error, "precision must be between 1 and 21");
                return;
            }

            double x = thisPtr.ShortValue;

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
                var str = IntImpl.CreateExponentialRepresentation(ref dtoaBuilder, exponent, negative, p, stackalloc char[128]);

				if (context.player.TryCreateStringValue(str, out NaNBoxing r, ref error))
				{
					context.StackSlots[returnSlotIndex] = r;
				}
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

                var str = sb.ToCharSpan();

				if (context.player.TryCreateStringValue(str, out NaNBoxing r, ref error))
				{
					context.StackSlots[returnSlotIndex] = r;
				}
			}
        }
    }
}