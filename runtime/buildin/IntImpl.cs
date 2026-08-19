using Jint.Native.Number.Dtoa;
using juicescript.ABC;
using juicescript.runtime.buildin.Pooling;
using System;
using System.Buffers;
using System.Diagnostics;
using static juicescript.runtime.Player;

namespace juicescript.runtime.buildin
{
	internal class IntImpl
	{
		private const string Digits = "0123456789abcdefghijklmnopqrstuvwxyz";

		[NativeFunction(".int$public::valueOf")]
		public static void Int_valueOf(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
#if DEBUG
			if (thisPtr.ValueType != NaNBoxing.BoxType.Int)
				throw new InvalidOperationException();
#endif

			context.StackSlots[returnSlotIndex] = thisPtr;
		}

		[NativeFunction(".int$public::toString")]
		public static void Int_toString(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
#if DEBUG
			if (thisPtr.ValueType != NaNBoxing.BoxType.Int)
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

			int x = thisPtr.IntValue;

			Span<char> buffer = stackalloc char[64];

			var str = IntToString(x, radixValue,buffer);

			if (context.player.TryCreateStringValue(str, out NaNBoxing r, ref error))
			{
				context.StackSlots[returnSlotIndex] = r;
			}
			//int str_ptr = context.GC.AllocString(str);
			//if (str_ptr == 0)
			//{
			//	context.player.RaiseOutOfMemory(ref error);
			//	return;
			//}

			//context.StackSlots[returnSlotIndex].SetHeapPtr(str_ptr, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
		}

		internal static ReadOnlySpan<char> IntToString(int n, int radix,Span<char> buffer)
		{
			Debug.Assert(buffer.Length >= 64);

			if (n == 0)
			{
				buffer[0]='0';
				return buffer.Slice(0, 1);
			}

			bool negative = false;
			uint u;

			if (n < 0)
			{
				negative = true;
				if (n == int.MinValue)
				{
					u = 0x80000000u;
				}
				else
				{
					u = (uint)(-n);
				}
			}
			else
			{
				u = (uint)n;
			}

			var sb = new ValueStringBuilder(buffer);

			while (u > 0)
			{
				uint digit = u % (uint)radix;
				sb.Append(Digits[(int)digit]);
				u /= (uint)radix;
			}

			if (negative)
			{
				sb.Append('-');
			}

			sb.Reverse();
			return sb.ToCharSpan(); //sb.ToString();
		}

		internal static ReadOnlySpan<char> CreateExponentialRepresentation(
			ref DtoaBuilder buffer,
			int exponent,
			bool negative,
			int significantDigits,
			Span<char> vbuffer
			)
		{
			Debug.Assert(vbuffer.Length >= 128);

			bool negativeExponent = false;
			if (exponent < 0)
			{
				negativeExponent = true;
				exponent = -exponent;
			}

			var sb = new ValueStringBuilder(vbuffer);
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

			return sb.ToCharSpan();
		}

		[NativeFunction(".int$public::toExponential")]
		public static void Int_toExponential(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
#if DEBUG
			if (thisPtr.ValueType != NaNBoxing.BoxType.Int)
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

			double x = thisPtr.IntValue;

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
			var result = CreateExponentialRepresentation(ref dtoaBuilder, exponent, x < 0, f + 1, stackalloc char[128]);

			if (context.player.TryCreateStringValue(result, out NaNBoxing r, ref error))
			{
				context.StackSlots[returnSlotIndex] = r;
			}
		}

		[NativeFunction(".int$public::toFixed")]
		public static void Int_toFixed(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
#if DEBUG
			if (thisPtr.ValueType != NaNBoxing.BoxType.Int)
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

			int n = thisPtr.IntValue;
			Span<char> buffer = stackalloc char[64];

			var str = IntToString(n, 10,buffer);

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

		
		[NativeFunction(".int$public::toPrecision")]
		public static void Int_toPrecision(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
#if DEBUG
			if (thisPtr.ValueType != NaNBoxing.BoxType.Int)
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

			double x = thisPtr.IntValue;

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
				var str = CreateExponentialRepresentation(ref dtoaBuilder, exponent, negative, p,stackalloc char[128]);

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
