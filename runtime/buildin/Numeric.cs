using Jint.Native.Number.Dtoa;
using juicescript.ABC;
using juicescript.runtime.buildin.Pooling;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
		[NativeFunction(".sbyte$:AS3::valueOf")]
		public static void SByte_ValueOf(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
#if DEBUG
			if (thisPtr.ValueType != NaNBoxing.BoxType.Sbyte)
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
