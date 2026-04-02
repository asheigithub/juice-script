using Jint.Native.Number.Dtoa;
using juicescript.ABC;
using juicescript.runtime.buildin.Pooling;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static juicescript.runtime.Player;

namespace juicescript.runtime.buildin
{
	internal class Numeric
	{
		

		[NativeFunction(".Number$public::valueOf")]
		public static void Number_valueOf(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
#if DEBUG
			if (thisPtr.ValueType != NaNBoxing.BoxType.Number)
				throw new InvalidOperationException();
#endif

			context.StackSlots[returnSlotIndex] = thisPtr;
		}

		[NativeFunction(".Number$public::toString")]
		public static void Number_toString(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
#if DEBUG
			if (thisPtr.ValueType != NaNBoxing.BoxType.Number)
				throw new InvalidOperationException();
#endif

			var scope = (RtPayloadMethodScope)context.GC.Heap[scope_ptr].facility;
			var radix = scope.ReadSlot(0, context.player);

			Debug.Assert(radix.ValueType == NaNBoxing.BoxType.Int);

			if (radix.IntValue < 2 || radix.IntValue > 36)
			{
				context.player.RaiseRangeError(ref error, "radix must be between 2 and 36");
			}

			var x = thisPtr.Number;

			if (double.IsNaN(x))
			{
				context.StackSlots[returnSlotIndex].SetHeapPtr( context.player.NAN_STR );
				return;
			}

			if (x == 0)
			{
				context.StackSlots[returnSlotIndex].SetHeapPtr(context.player.ZERO_STR);
				return;
			}

			if (double.IsPositiveInfinity(x) || x >= double.MaxValue)
			{
				context.StackSlots[returnSlotIndex].SetHeapPtr(context.player.POSITIVEINF_STR);
				return;
			}

			if (double.IsNegativeInfinity(x) || x <= double.MinValue)
			{
				context.StackSlots[returnSlotIndex].SetHeapPtr(context.player.NEGATIVEINF_STR);
				return;
			}

			string str = ToNumberString(x, radix.IntValue);

			int str_ptr = context.GC.AllocString(str);
			if (str_ptr == 0)
			{
				context.player.RaiseOutOfMemory(ref error);
				return;
			}

			context.StackSlots[returnSlotIndex].SetHeapPtr(str_ptr);
		}


		[NativeFunction(".Number$public::toPrecision")]
		public static void Number_toPrecision(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
#if DEBUG
			if (thisPtr.ValueType != NaNBoxing.BoxType.Number)
				throw new InvalidOperationException();
#endif

			var scope = (RtPayloadMethodScope)context.GC.Heap[scope_ptr].facility;
			var precision = scope.ReadSlot(0, context.player);

			Debug.Assert(precision.ValueType == NaNBoxing.BoxType.Int);

			var x = thisPtr.Number;

			if (double.IsNaN(x))
			{
				context.StackSlots[returnSlotIndex].SetHeapPtr(context.player.NAN_STR);
				return;
			}

			if (double.IsInfinity(x))
			{
				context.StackSlots[returnSlotIndex].SetHeapPtr(double.IsNegativeInfinity(x) ? context.player.NEGATIVEINF_STR : context.player.POSITIVEINF_STR);
				return;
			}

			if (precision.IntValue < 1 || precision.IntValue > 21)
			{
				context.player.RaiseRangeError(ref error, "precision must be between 1 and 21");
			}

			int p = precision.IntValue;

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

				// Use fixed notation.
				if (negative)
				{
					sb.Append('-');
				}

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
						var extra = negative ? 2 : 1;
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

		internal const double DoubleIsIntegerTolerance = double.Epsilon * 100;

		[NativeFunction(".Number$public::toFixed")]
		public static void Number_toFixed(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
#if DEBUG
			if (thisPtr.ValueType != NaNBoxing.BoxType.Number)
				throw new InvalidOperationException();
#endif

			var scope = (RtPayloadMethodScope)context.GC.Heap[scope_ptr].facility;
			var digits = scope.ReadSlot(0, context.player);

			Debug.Assert(digits.ValueType == NaNBoxing.BoxType.Int);

			var x = thisPtr.Number;

			
			if (digits.IntValue < 0 || digits.IntValue > 20)
			{
				context.player.RaiseRangeError(ref error, "digits must be between 0 and 20");
			}

			int f = digits.IntValue;

			if (double.IsNaN(x))
			{
				context.StackSlots[returnSlotIndex].SetHeapPtr(context.player.NAN_STR);
				return;
			}

			const double Ten21 = 1e21;

			string str;

			if (x >= Ten21 || x <= -Ten21)
			{
				str = ToNumberString(x);
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
				// handle non-decimal with greater precision
				if (System.Math.Abs(x - (long)x) < DoubleIsIntegerTolerance)
				{
					var result = ((long)x).ToString("f" + f, CultureInfo.InvariantCulture);
					str = negative ? "-" + result : result;
					goto lbl_str;
				}

				var formatted = x.ToString("f" + f, CultureInfo.InvariantCulture);
				str = negative ? "-" + formatted : formatted;
				goto lbl_str;
			}

			// Use Dtoa infrastructure for f == 100 (avoids .NET format specifier limitation)
			str = ToFixedDtoa(x, f, negative);


		lbl_str:

			int str_ptr = context.GC.AllocString(str);
			if (str_ptr == 0)
			{
				context.player.RaiseOutOfMemory(ref error);
				return;
			}

			context.StackSlots[returnSlotIndex].SetHeapPtr(str_ptr);

			;

		}

		[NativeFunction(".Number$public::toExponential")]
		public static void Number_toExponential(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
#if DEBUG
			if (thisPtr.ValueType != NaNBoxing.BoxType.Number)
				throw new InvalidOperationException();
#endif

			var scope = (RtPayloadMethodScope)context.GC.Heap[scope_ptr].facility;
			var fractionDigits = scope.ReadSlot(0, context.player);

			Debug.Assert(fractionDigits.ValueType == NaNBoxing.BoxType.Int);

			var x = thisPtr.Number;


			

			int f = fractionDigits.IntValue;

			if (double.IsNaN(x))
			{
				context.StackSlots[returnSlotIndex].SetHeapPtr(context.player.NAN_STR);
				return;
			}

			if (double.IsInfinity(x))
			{
				context.StackSlots[returnSlotIndex].SetHeapPtr(double.IsNegativeInfinity(x) ? context.player.NEGATIVEINF_STR : context.player.POSITIVEINF_STR);
				return;
			}

			if (fractionDigits.IntValue < 0 || fractionDigits.IntValue > 20)
			{
				context.player.RaiseRangeError(ref error, "digits must be between 0 and 20");
			}


			bool negative = false;
			if (x < 0)
			{
				x = -x;
				negative = true;
			}

			int decimalPoint;
			var dtoaBuilder = new DtoaBuilder(stackalloc char[f == -1 ? FastDtoa.KFastDtoaMaximalLength + 8 : 101]);

			//if (f == -1)
			//{
			//	DtoaNumberFormatter.DoubleToAscii(
			//		ref dtoaBuilder,
			//		x,
			//		DtoaMode.Shortest,
			//		requested_digits: 0,
			//		out _,
			//		out decimalPoint);
			//	f = dtoaBuilder.Length - 1;
			//}
			//else
			{
				DtoaNumberFormatter.DoubleToAscii(
					ref dtoaBuilder,
					x,
					DtoaMode.Precision,
					requested_digits: f + 1,
					out _,
					out decimalPoint);
			}

			Debug.Assert(dtoaBuilder.Length > 0);
			Debug.Assert(dtoaBuilder.Length <= f + 1);

			int exponent = decimalPoint - 1;
			var result = CreateExponentialRepresentation(ref dtoaBuilder, exponent, negative, f + 1);

			int str_ptr = context.GC.AllocString(result);
			if (str_ptr == 0)
			{
				context.player.RaiseOutOfMemory(ref error);
				return;
			}

			context.StackSlots[returnSlotIndex].SetHeapPtr(str_ptr);

		}




		private static string ToFixedDtoa(double x, int fractionDigits, bool negative)
		{
			if (x == 0)
			{
				var sb = new ValueStringBuilder(stackalloc char[128]);
				if (negative)
				{
					sb.Append('-');
				}
				sb.Append("0.");
				sb.Append('0', fractionDigits);
				return sb.ToString();
			}

			var dtoaBuilder = new DtoaBuilder(stackalloc char[fractionDigits + 50]);
			DtoaNumberFormatter.DoubleToAscii(
				ref dtoaBuilder,
				x,
				DtoaMode.Fixed,
				fractionDigits,
				out _,
				out var decimalPoint);

			var result2 = new ValueStringBuilder(stackalloc char[fractionDigits + 50]);
			if (negative)
			{
				result2.Append('-');
			}

			if (decimalPoint <= 0)
			{
				// 0.000...digits
				result2.Append("0.");
				result2.Append('0', -decimalPoint);
				result2.Append(dtoaBuilder._chars.Slice(0, dtoaBuilder.Length));
				int remaining = fractionDigits - (-decimalPoint + dtoaBuilder.Length);
				if (remaining > 0)
				{
					result2.Append('0', remaining);
				}
			}
			else if (decimalPoint >= dtoaBuilder.Length)
			{
				// Integer part only, pad with zeros
				result2.Append(dtoaBuilder._chars.Slice(0, dtoaBuilder.Length));
				result2.Append('0', decimalPoint - dtoaBuilder.Length);
				if (fractionDigits > 0)
				{
					result2.Append('.');
					result2.Append('0', fractionDigits);
				}
			}
			else
			{
				// digits split across integer and fractional part
				result2.Append(dtoaBuilder._chars.Slice(0, decimalPoint));
				result2.Append('.');
				int fracDigitsFromDtoa = dtoaBuilder.Length - decimalPoint;
				result2.Append(dtoaBuilder._chars.Slice(decimalPoint, fracDigitsFromDtoa));
				int remaining = fractionDigits - fracDigitsFromDtoa;
				if (remaining > 0)
				{
					result2.Append('0', remaining);
				}
			}

			return result2.ToString();
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

		private static string ToNumberString(double x, int radix)
		{
			if (x < 0)
			{
				return "-" + ToNumberString(-x, radix);
			}

			if (radix == 10)
			{
				return ToNumberString(x);
			}

			var integer = (long)x;
			var fraction = x - integer;

			string result = ToBase(integer, radix);
			if (fraction != 0)
			{
				result += "." + ToFractionBase(fraction, radix);
			}

			return result;
		}

		internal static string ToBase(long n, int radix)
		{
			const string Digits = "0123456789abcdefghijklmnopqrstuvwxyz";
			if (n == 0)
			{
				return "0";
			}

			var sb = new ValueStringBuilder(stackalloc char[64]);
			while (n > 0)
			{
				var digit = (int)(n % radix);
				n /= radix;
				sb.Append(Digits[digit]);
			}
			sb.Reverse();
			return sb.ToString();
		}

		internal static string ToFractionBase(double n, int radix)
		{
			// based on the repeated multiplication method
			// http://www.mathpath.org/concepts/Num/frac.htm

			const string Digits = "0123456789abcdefghijklmnopqrstuvwxyz";
			if (n == 0)
			{
				return "0";
			}

			var result = new ValueStringBuilder(stackalloc char[64]);
			while (n > 0 && result.Length < 50) // arbitrary limit
			{
				var c = n * radix;
				var d = (int)c;
				n = c - d;

				result.Append(Digits[d]);
			}

			return result.ToString();
		}

		internal static string ToNumberString(double m)
		{
			 const int SmallDtoaLength = FastDtoa.KFastDtoaMaximalLength + 8;
			

			if (double.IsNaN(m))
			{
				return "NaN";
			}

			if (m == 0)
			{
				return "0";
			}

			if (double.IsInfinity(m))
			{
				return double.IsNegativeInfinity(m) ? "-Infinity" : "Infinity";
			}

			var builder = new DtoaBuilder(stackalloc char[SmallDtoaLength]);

			DtoaNumberFormatter.DoubleToAscii(
				ref builder,
				m,
				DtoaMode.Shortest,
				0,
				out var negative,
				out var decimal_point);


			var stringBuilder = new Pooling.ValueStringBuilder(stackalloc char[64]);
			if (negative)
			{
				stringBuilder.Append('-');
			}

			if (builder.Length <= decimal_point && decimal_point <= 21)
			{
				// ECMA-262 section 9.8.1 step 6.
				stringBuilder.Append(builder._chars.Slice(0, builder.Length));
				stringBuilder.Append('0', decimal_point - builder.Length);
			}
			else if (0 < decimal_point && decimal_point <= 21)
			{
				// ECMA-262 section 9.8.1 step 7.
				stringBuilder.Append(builder._chars.Slice(0, decimal_point));
				stringBuilder.Append('.');
				stringBuilder.Append(builder._chars.Slice(decimal_point, builder.Length - decimal_point));
			}
			else if (decimal_point <= 0 && decimal_point > -6)
			{
				// ECMA-262 section 9.8.1 step 8.
				stringBuilder.Append("0.");
				stringBuilder.Append('0', -decimal_point);
				stringBuilder.Append(builder._chars.Slice(0, builder.Length));
			}
			else
			{
				// ECMA-262 section 9.8.1 step 9 and 10 combined.
				stringBuilder.Append(builder._chars[0]);
				if (builder.Length != 1)
				{
					stringBuilder.Append('.');
					stringBuilder.Append(builder._chars.Slice(1, builder.Length - 1));
				}

				stringBuilder.Append('e');
				stringBuilder.Append((decimal_point >= 0) ? '+' : '-');
				int exponent = decimal_point - 1;
				if (exponent < 0)
				{
					exponent = -exponent;
				}

				stringBuilder.Append(exponent);
			}

			return stringBuilder.ToString();
		}

	}
}
