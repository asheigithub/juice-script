using Jint.Native.Number.Dtoa;
using juicescript.ABC;
using juicescript.runtime.buildin.Pooling;
using System;
using System.Buffers;
using System.Diagnostics;
using static juicescript.runtime.Player;

namespace juicescript.runtime.buildin
{
	internal class UintImpl
	{
		private const string Digits = "0123456789abcdefghijklmnopqrstuvwxyz";

		[NativeFunction(".uint$public::valueOf")]
		public static void Uint_valueOf(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
#if DEBUG
			if (thisPtr.ValueType != NaNBoxing.BoxType.Uint)
				throw new InvalidOperationException();
#endif

			context.StackSlots[returnSlotIndex] = thisPtr;
		}

		[NativeFunction(".uint$public::toString")]
		public static void Uint_toString(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
#if DEBUG
			if (thisPtr.ValueType != NaNBoxing.BoxType.Uint)
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

			uint x = thisPtr.UIntValue;

			string str = UintToString(x, radixValue);

			int str_ptr = context.GC.AllocString(str);
			if (str_ptr == 0)
			{
				context.player.RaiseOutOfMemory(ref error);
				return;
			}

			context.StackSlots[returnSlotIndex].SetHeapPtr(str_ptr, (byte)RtHeapTypeKind.STRING);
		}

		private static string UintToString(uint n, int radix)
		{
			if (n == 0)
			{
				return "0";
			}

			var sb = new ValueStringBuilder(stackalloc char[32]);

			while (n > 0)
			{
				uint digit = n % (uint)radix;
				sb.Append(Digits[(int)digit]);
				n /= (uint)radix;
			}

			sb.Reverse();
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

		[NativeFunction(".uint$public::toExponential")]
		public static void Uint_toExponential(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
#if DEBUG
			if (thisPtr.ValueType != NaNBoxing.BoxType.Uint)
				throw new InvalidOperationException();
#endif

			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
			var fractionDigits = scope.ReadSlot(0, context.player);

			Debug.Assert(fractionDigits.ValueType == NaNBoxing.BoxType.Int);

			int f = fractionDigits.IntValue;

			if (f < 0 || f > 20)
			{
				context.player.RaiseRangeError(ref error, "digits must be between 0 and 20");
				return;
			}

			double x = thisPtr.UIntValue;

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
			var result = CreateExponentialRepresentation(ref dtoaBuilder, exponent, false, f + 1);

			int str_ptr = context.GC.AllocString(result);
			if (str_ptr == 0)
			{
				context.player.RaiseOutOfMemory(ref error);
				return;
			}

			context.StackSlots[returnSlotIndex].SetHeapPtr(str_ptr, (byte)RtHeapTypeKind.STRING);
		}

		[NativeFunction(".uint$public::toFixed")]
		public static void Uint_toFixed(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
#if DEBUG
			if (thisPtr.ValueType != NaNBoxing.BoxType.Uint)
				throw new InvalidOperationException();
#endif

			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
			var digits = scope.ReadSlot(0, context.player);

			Debug.Assert(digits.ValueType == NaNBoxing.BoxType.Int);

			int f = digits.IntValue;

			if (f < 0 || f > 20)
			{
				context.player.RaiseRangeError(ref error, "digits must be between 0 and 20");
				return;
			}

			uint x = thisPtr.UIntValue;

			string str = UintToFixed(x, f);

			int str_ptr = context.GC.AllocString(str);
			if (str_ptr == 0)
			{
				context.player.RaiseOutOfMemory(ref error);
				return;
			}

			context.StackSlots[returnSlotIndex].SetHeapPtr(str_ptr, (byte)RtHeapTypeKind.STRING);
		}

		private static string UintToFixed(uint n, int fractionDigits)
		{
			if (fractionDigits == 0)
			{
				return UintToString(n, 10);
			}

			var sb = new ValueStringBuilder(stackalloc char[32]);

			if (n == 0)
			{
				sb.Append("0.");
				sb.Append('0', fractionDigits);
				return sb.ToString();
			}

			while (n > 0)
			{
				uint digit = n % 10;
				sb.Append(Digits[(int)digit]);
				n /= 10;
			}

			sb.Reverse();
			sb.Append('.');
			sb.Append('0', fractionDigits);

			return sb.ToString();
		}

		[NativeFunction(".uint$public::toPrecision")]
		public static void Uint_toPrecision(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
#if DEBUG
			if (thisPtr.ValueType != NaNBoxing.BoxType.Uint)
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

			double x = thisPtr.UIntValue;

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

				context.StackSlots[returnSlotIndex].SetHeapPtr(str_ptr, (byte)RtHeapTypeKind.STRING);
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

				context.StackSlots[returnSlotIndex].SetHeapPtr(str_ptr, (byte)RtHeapTypeKind.STRING);
			}
		}
	}
}
