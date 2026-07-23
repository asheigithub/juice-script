using juicescript.ABC.Locaters;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
//using static juicescript.ABC.INS.INS_Op_stack_Var_ldConst;

namespace juicescript.ABC.INS
{
	public sealed class INS_BitWise : Instruction
	{
		public enum BItWiseMode
		{
		//	&	bitwise AND 将 expression1 和 expression2 转换为 32 位无符号整数，并对整数参数的每一位执行 Boolean AND 运算。
 		//<<	bitwise left shift  将 expression1 和 shiftCount 转换为 32 位整数，并将 expression1 中的所有位向左移动由 shiftCount 转换所得到的整数指定的位数。
 		//~bitwise NOT 将 expression 转换为一个 32 位带符号整数，然后按位对 1 求补。
 		//|	bitwise OR  将 expression1 和 expression2 转换为 32 位无符号整数，并在 expression1 或 expression2 的对应位为 1 的每个位的位置放置一个 1。
 		//>>	bitwise right shift 将 expression 和 shiftCount 转换为 32 位整数，并将 expression 中的所有位向右移动由 shiftCount 转换所得到的整数指定的位数。
 		//>>>	bitwise unsigned right shift    此运算符与 bitwise right shift(>>) 运算符基本相同，只是此运算符不保留原始表达式的符号，因为左侧的位始终用 0 填充。
 		//^	bitwise XOR

			bitwise_and, // &
			left_shift,  // <<
			bitwise_not, // ~
			bitwise_or,  // | 
			right_shift, // >>
			unsigned_right_shift, //>>>
			xor //^
		}


		public INS_BitWise(Token token) : base(token)
		{
		}

		public override INS_Code INS_Code => INS_Code.bitwise;

		public StackLocater v1;

		public StackLocater v2;

		public BItWiseMode wiseMode;

		public override int Size => 4 + 4 + 4; // wiseMode嵌入v1

		protected override void ReadFromBinary(BinaryReader br)
		{
			uint v = br.ReadUInt32();
			wiseMode = (BItWiseMode)(v & 0xff);
			v1.index = (int)(v >> 8);
			v2.ReadFromBinary(br);

		}

		protected override void WriteByte(BinaryWriter bw)
		{
			if (v1.index > 0xffffff)
			{
				throw new InvalidOperationException();
			}

			uint v = (uint)v1.index << 8 | (byte)wiseMode;
			bw.Write(v);
			v2.Write(bw);
		}

		private string GetWiseString()
		{
			switch (wiseMode)
			{
				case BItWiseMode.bitwise_and:
					return "&";
				case BItWiseMode.left_shift:
					return "<<";
				case BItWiseMode.bitwise_not:
					return "~";
				case BItWiseMode.bitwise_or:
					return "|";
				case BItWiseMode.right_shift:
					return ">>";
				case BItWiseMode.unsigned_right_shift:
					return ">>>";
				case BItWiseMode.xor:
					return "^";
				default:
					return "ERR";
			}
		}


		public override string ToString()
		{
			if (wiseMode == BItWiseMode.bitwise_not)
			{
				return $"BIT(~)   [{dst}]<- ~[{v1}]";
			}
			else
			{
				return $"BIT({GetWiseString()})   [{dst}]<- [{v1}] {GetWiseString()} [{v2}]";
			}
		}

        public override IEnumerable<StackLocater> GetDef()
        {
			//return new List<StackLocater> { dst };
			yield return dst;
        }

        public override IEnumerable<StackLocater> GetUse()
        {
			if (wiseMode == BItWiseMode.bitwise_not)
			{
				yield return v1;

				//return new List<StackLocater> { v1 };
			}
			else
			{
				yield return v1;
				yield return v2;

				//return new List<StackLocater> { v1, v2 };
			}
        }

        public override bool MaybeRaiseError()
        {
            return true;
        }

        public override void RemappingSlots(Dictionary<int, int> mapping)
        {
            if (mapping.TryGetValue(dst.index, out int newIndex))
                dst.index = newIndex;
            if (mapping.TryGetValue(v1.index, out int newIndex1))
                v1.index = newIndex1;
            if (mapping.TryGetValue(v2.index, out int newIndex2))
                v2.index = newIndex2;
        }

	}
}
