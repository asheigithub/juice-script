using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
	/// <summary>
	/// 组合指令，从ScopeH和Const中取值，然后做计算后存入dst
	/// </summary>
	public sealed class INS_Op_stack_Var_ldConst : Instruction
	{
		public enum OpMode : byte
		{
			strict_eq,	
			sub,
		}

		private string GetModeStr()
		{
			switch (mode)
			{
				case OpMode.strict_eq:
					return "===";
				case OpMode.sub:
					return "-";
				default:
					return "!!!Known";
			}
		}


		public INS_Op_stack_Var_ldConst(Token token) : base(token)
		{
		}

		public byte const_index;
		public byte heap_member;
		public OpMode mode;
		public byte tempLoc;


		public override INS_Code INS_Code => INS_Code.op_stack_Variable_ldconst;

		public override int Size => 4 +4;
		

		protected override void ReadFromBinary(BinaryReader br)
		{
			const_index = br.ReadByte();
			heap_member = br.ReadByte();
			mode = (OpMode)br.ReadByte();
			tempLoc = br.ReadByte();
		
		}

		protected override void WriteByte(BinaryWriter bw)
		{
			bw.Write(const_index);
			bw.Write(heap_member);
			bw.Write((byte)mode);
			bw.Write(tempLoc);
		}

		public override string ToString()
		{
			return $"Op_stack_Var_ldConst [stack:{dst}] <- [var:{heap_member}] {GetModeStr()} [const:{ const_index }]";
		}

	}
}
