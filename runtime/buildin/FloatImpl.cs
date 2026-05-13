using Jint.Native.Number.Dtoa;
using juicescript.ABC;
using juicescript.runtime.buildin.Pooling;
using System;
using System.Buffers;
using System.Diagnostics;
using System.Globalization;
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

            var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
            var radix = scope.ReadSlot(0, context.player);

            Debug.Assert(radix.ValueType == NaNBoxing.BoxType.Int);
            var radixValue = radix.IntValue;

            if (radixValue < 2 || radixValue > 36)
            {
                context.player.RaiseRangeError(ref error, "radix must be between 2 and 36");
                return;
            }

            float x = thisPtr.FloatValue;

            Span<char> buffer = stackalloc char[128];

            var str = FloatToString(x, radixValue,buffer);

			if (context.player.TryCreateStringValue(str, out NaNBoxing r, ref error))
			{
				context.StackSlots[returnSlotIndex] = r;
			}

			//int str_ptr = context.GC.AllocString(str);
			//if (str_ptr == 0)
			//{
			//    context.player.RaiseOutOfMemory(ref error);
			//    return;
			//}

			//context.StackSlots[returnSlotIndex].SetHeapPtr(str_ptr, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
		}

        internal static ReadOnlySpan<char> FloatToString(float n, int radix,Span<char> buffer)
        {
            Debug.Assert(buffer.Length >= 128);

            if (float.IsNaN(n))
            {
                "NaN".CopyTo(buffer);
                return buffer.Slice(0, 3);
                //return "NaN";
            }
            
            if (n == 0)
            {
                buffer[0] = '0';
                return buffer.Slice(0, 1);
                //return "0";
            }

            if (float.IsInfinity(n))
            {
                if (float.IsNegativeInfinity(n))
                {
                    "-Infinity".CopyTo(buffer);
                    return buffer.Slice(0, 9);
				}
                else
                {
					"Infinity".CopyTo(buffer);
					return buffer.Slice(0, 8);
				}
                //return float.IsNegativeInfinity(n) ? "-Infinity" : "Infinity";
            }

            bool negative = false;
            if (n < 0)
            {
                negative = true;
                n = -n;
            }

            if (radix == 10)
            {
                if (n.TryFormat(buffer.Slice(1), out int wchars, default, System.Globalization.CultureInfo.InvariantCulture))
                {
					for (int i = 1; i < wchars; i++)
					{
						if (buffer[i] == 'E') buffer[i] = 'e';
					}

					if (negative)
                    {
                        buffer[0]='-';
						return buffer.Slice(0,1+ wchars);
					}
                    else
                    {
                        return buffer.Slice(1, wchars);
                    }
                }

                if (negative)
                {
                    buffer[0] = '-';
                    var r = FloatToStringDecimal(n, buffer.Slice(1));
                    return buffer.Slice(0, r.Length + 1);
                }
                else
                {
                    return FloatToStringDecimal(n, buffer);
                }

                //return negative ? "-" + FloatToStringDecimal(n) : FloatToStringDecimal(n);
            }

            float intPart =MathF.Truncate(n);
            float fracPart = n - intPart;

            //string result = IntToString(intPart, radix);
            //if (fracPart > 0)
            //{
            //    result += "." + FloatFractionToString(fracPart, radix);
            //}

            //return negative ? "-" + result : result;

            if (intPart > int.MaxValue)
            {
				int k = 0;
				while (intPart > int.MaxValue)
				{
					intPart = intPart / radix;
					k++;
				}

                if (negative)
                {

                    buffer[0] = '-';

                    var b = IntToString((int)intPart, radix, buffer.Slice(1));
                    if (k < 63)
                    {
                        buffer.Slice(1+b.Length, k).Fill('0');
                        return buffer.Slice(0,1+ b.Length + k);
                    }
                    else
                    {
                        return "-" + b.ToString() + "".PadRight(k, '0'); // 只能溢出了
                        ;
                    }
                }
                else
                {
					var b = IntToString((int)intPart, radix, buffer);
					if (k < 64)
					{
						buffer.Slice(b.Length, k).Fill('0');
						return buffer.Slice(0, b.Length + k);
					}
					else
					{
						return b.ToString() + "".PadRight(k, '0'); // 只能溢出了
						;
					}
				}
				
            }
            else if (negative)
            {
                buffer[0] = '-';

                var result = IntToString((int)intPart, radix, buffer.Slice(1));

                if (fracPart > 0)
                {
                    buffer[1 + result.Length] = '.';

                    var p2 = FloatFractionToString(fracPart, radix, buffer.Slice(1 + result.Length + 1));

                    return buffer.Slice(0, 1 + result.Length + 1 + p2.Length);

                }
                else
                {
                    return buffer.Slice(0, result.Length + 1);
                }

            }
            else
            {
				var result = IntToString((int)intPart, radix, buffer);

                if (fracPart > 0)
                {
                    buffer[result.Length] = '.';

                    var p2 = FloatFractionToString(fracPart, radix, buffer.Slice(result.Length + 1));

                    return buffer.Slice(0, 1 + result.Length + 1 + p2.Length);

                }
                else
                {
                    return result;
                }
			}



        }

        private static ReadOnlySpan<char> FloatToStringDecimal(float n , Span<char> buffer)
        {
            Debug.Assert(buffer.Length >= 96);

            if (n == 0) { buffer[0] = '0'; return buffer.Slice(0,1); }//return "0";

            int intPart = (int)MathF.Truncate(n);
            float fracPart = n - intPart;

            var result = IntToString(intPart, 10, buffer);  //intPart.ToString();

            if (fracPart > 0)
            {
                var sb = new ValueStringBuilder( buffer.Slice(result.Length + 1) );//stackalloc char[32]);
                for (int i = 0; i < 7 && fracPart > 0; i++)
                {
                    fracPart *= 10;
                    int digit = (int)MathF.Truncate(fracPart);
                    sb.Append(Digits[digit]);
                    fracPart -= digit;
                }

                var f = sb.ToCharSpan().TrimEnd('0');

                buffer[result.Length] = '.';

                result = buffer.Slice(0, result.Length + 1 + f.Length);

                //result = intPart.ToString() + "." + sb.ToString().TrimEnd('0');
            }

            return result;
        }

        private static ReadOnlySpan<char> IntToString(int n, int radix , Span<char> buffer  )
        {
            Debug.Assert(buffer.Length >= 32);

            if (n == 0)
            {
                buffer[0] = '0';
                return buffer.Slice(0, 1);
                //return "0";
            }
            
            var sb = new ValueStringBuilder(buffer);
            while (n > 0)
            {
                int digit = n % radix;
                sb.Append(Digits[digit]);
                n /= radix;
            }

            sb.Reverse();
            //return sb.ToString();
            return sb.ToCharSpan();
        }

        private static ReadOnlySpan<char> FloatFractionToString(float n, int radix,Span<char> buffer)
        {
            Debug.Assert(buffer.Length >= 32);

            var sb = new ValueStringBuilder(buffer);
            for (int i = 0; i < 10 && n > 0; i++)
            {
                n *= radix;
                int digit = (int)MathF.Truncate(n);
                sb.Append(Digits[digit]);
                n -= digit;
            }
            return sb.ToCharSpan(); //ToString();
        }

        private static ReadOnlySpan<char> CreateExponentialRepresentation(
            ref DtoaBuilder buffer,
            int exponent,
            bool negative,
            int significantDigits ,
            Span<char> vsbuffer
            )
        {
            Debug.Assert(vsbuffer.Length >= 128);

            bool negativeExponent = false;
            if (exponent < 0)
            {
                negativeExponent = true;
                exponent = -exponent;
            }

            var sb = new ValueStringBuilder(vsbuffer);
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

            return sb.ToCharSpan(); //sb.ToString();
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

            var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
            var fractionDigits = scope.ReadSlot(0, context.player);

            Debug.Assert(fractionDigits.ValueType == NaNBoxing.BoxType.Int);
            var f = fractionDigits.IntValue;

            if (f < 0 || f > 20)
            {
                context.player.RaiseRangeError(ref error, "digits must be between 0 and 20");
                return;
            }


			double x = thisPtr.FloatValue;
			Span<char> temp = stackalloc char[64];
			if (thisPtr.FloatValue.TryFormat(temp, out int wchars, default, System.Globalization.CultureInfo.InvariantCulture))
			{
				double.TryParse(temp.Slice(0, wchars), out x);
			}


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

            Span<char> buffer = stackalloc char[128];

            var str = CreateExponentialRepresentation(ref dtoaBuilder, exponent, x < 0, f + 1 , buffer);

			if (context.player.TryCreateStringValue(str, out NaNBoxing result, ref error))
			{
				context.StackSlots[returnSlotIndex] = result;
			}

			//int str_ptr = context.GC.AllocString(result);
			//if (str_ptr == 0)
			//{
			//    context.player.RaiseOutOfMemory(ref error);
			//    return;
			//}

			//context.StackSlots[returnSlotIndex].SetHeapPtr(str_ptr, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
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

            var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
            var digits = scope.ReadSlot(0, context.player);

            Debug.Assert(digits.ValueType == NaNBoxing.BoxType.Int);


            double x = thisPtr.FloatValue;
			Span<char> temp = stackalloc char[64];
			if (thisPtr.FloatValue.TryFormat(temp, out int wchars, default, System.Globalization.CultureInfo.InvariantCulture))
			{
				double.TryParse(temp.Slice(0, wchars), out x);
			}


			if (digits.IntValue < 0 || digits.IntValue > 20)
			{
				context.player.RaiseRangeError(ref error, "digits must be between 0 and 20");
			}

			int f = digits.IntValue;

			if (double.IsNaN(x))
			{
				context.StackSlots[returnSlotIndex].SetHeapPtr(context.player.NAN_STR, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				return;
			}

			const double Ten21 = 1e21;

			//string str;

			Span<char> buffer1 = stackalloc char[128];

			ReadOnlySpan<char> str = buffer1;


			if (x >= Ten21 || x <= -Ten21)
			{
				str =  Numeric.ToNumberString(x, buffer1);
				goto lbl_str;
			}

			bool negative = false;
			if (x < 0)
			{
				negative = true;
				x = -x;
			}

			if (f == 0)
			{
				// Fast path: no fractional digits
				var rounded = System.Math.Round(x, MidpointRounding.AwayFromZero);
				var result = negative ? "-" + ((long)rounded).ToString(CultureInfo.InvariantCulture) : ((long)rounded).ToString(CultureInfo.InvariantCulture);
				str = result;
				goto lbl_str;
			}

			// Use .NET formatting for f <= 99 (fast path)
			if (f <= 99)
			{
                Span<char> format = stackalloc char[128];
                format[0] = 'f';
                ReadOnlySpan<char> ff = format;
                if (f.TryFormat(format.Slice(1), out int fw))
                {
                    ff = format.Slice(0, 1 + fw);
                }
                else
                {
                    ff = "f" + f;
                }

                // handle non-decimal with greater precision
                if (System.Math.Abs(x - (long)x) < Numeric.DoubleIsIntegerTolerance)
                {
                    
                    if (((long)x).TryFormat(buffer1.Slice(1), out int lc, ff, CultureInfo.InvariantCulture))
                    {
                        if (negative)
                        {
                            buffer1[0] = '-';
                            str = buffer1.Slice(0, 1 + lc);
                        }
                        else
                        {
                            str = buffer1.Slice(1, lc);
                        }
                        goto lbl_str;
                    }

					var result = ((long)x).ToString("f" + f, CultureInfo.InvariantCulture);
					str = negative ? "-" + result : result;
				}

				if (x.TryFormat(buffer1.Slice(1), out int wc, ff, CultureInfo.InvariantCulture))
                {
                    if (negative)
                    {
                        buffer1[0] = '-';
                        str = buffer1.Slice(0, 1 + wc);
                    }
                    else
                    {
						str = buffer1.Slice(1, wc);
					}
                    goto lbl_str;
                }

				var formatted = x.ToString("f" + f, CultureInfo.InvariantCulture);
				str = negative ? "-" + formatted : formatted;
				goto lbl_str;
			}

            //不可能走到这里
            // Use Dtoa infrastructure for f == 100 (avoids .NET format specifier limitation)
           // str = Numeric.ToFixedDtoa(x, f, negative);


		lbl_str:

			if (context.player.TryCreateStringValue(str, out NaNBoxing r, ref error))
			{
				context.StackSlots[returnSlotIndex] = r;
			}







		}

        //private static string FloatToFixedString(float n, int fractionDigits)
        //{
        //    if (float.IsNaN(n))
        //    {
        //        return "NaN";
        //    }

        //    if (float.IsInfinity(n))
        //    {
        //        return float.IsNegativeInfinity(n) ? "-Infinity" : "Infinity";
        //    }

        //    bool negative = false;
        //    if (n < 0)
        //    {
        //        negative = true;
        //        n = -n;
        //    }

        //    if (fractionDigits == 0)
        //    {
        //        int rounded = (int)MathF.Round(n);
        //        return negative ? "-" + rounded : rounded.ToString();
        //    }

        //    int intPart = (int)MathF.Truncate(n);
        //    float fracPart = n - intPart;

        //    var sb = new ValueStringBuilder(stackalloc char[32]);
        //    if (negative)
        //    {
        //        sb.Append('-');
        //    }

        //    sb.Append(intPart);
        //    sb.Append('.');

        //    for (int i = 0; i < fractionDigits; i++)
        //    {
        //        fracPart *= 10;
        //        int digit = (int)MathF.Truncate(fracPart);
        //        sb.Append(Digits[digit]);
        //        fracPart -= digit;
        //    }

        //    return sb.ToString();
        //}

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

            var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
            var precision = scope.ReadSlot(0, context.player);
            Debug.Assert(precision.ValueType == NaNBoxing.BoxType.Int);

            int p = precision.IntValue;

            if (p < 1 || p > 21)
            {
                context.player.RaiseRangeError(ref error, "precision must be between 1 and 21");
                return;
            }

			double x = thisPtr.FloatValue;
			Span<char> temp = stackalloc char[64];
            if (thisPtr.FloatValue.TryFormat(temp, out int wchars, default, System.Globalization.CultureInfo.InvariantCulture))
            {
                double.TryParse(temp.Slice(0, wchars), out x);
            }

            
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
                Span<char> buffer = stackalloc char[128];

                var str = CreateExponentialRepresentation(ref dtoaBuilder, exponent, negative, p,buffer);
				if (context.player.TryCreateStringValue(str, out NaNBoxing result, ref error))
				{
					context.StackSlots[returnSlotIndex] = result;
				}

				//int str_ptr = context.GC.AllocString(str);
				//if (str_ptr == 0)
				//{
				//    context.player.RaiseOutOfMemory(ref error);
				//    return;
				//}

				//context.StackSlots[returnSlotIndex].SetHeapPtr(str_ptr, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
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
				if (context.player.TryCreateStringValue(str, out NaNBoxing result, ref error))
				{
					context.StackSlots[returnSlotIndex] = result;
				}

				//string str = sb.ToString();

				//int str_ptr = context.GC.AllocString(str);
				//if (str_ptr == 0)
				//{
				//    context.player.RaiseOutOfMemory(ref error);
				//    return;
				//}

				//context.StackSlots[returnSlotIndex].SetHeapPtr(str_ptr, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
			}
        }
    }
}
