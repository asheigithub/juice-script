using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
	public sealed class INS_If_LogicOp_Goto : Instruction
	{
		public enum CompMode : byte
		{
			strict_equal,		
		}


		public INS_If_LogicOp_Goto(Token token) : base(token)
		{

		}

		public override INS_Code INS_Code => INS_Code.if_logicOp_goto;

		public override int Size =>  4 + 4 + 4 ;

		public int flag_id
		{
			get { return dst.index; }
			set { dst.index = value; }
		}

		public int offset;

		public CompMode compMode;
		public bool jump_mode;

		public byte v1;
		public byte v2;

		private string GetCompModeString()
		{
			switch (compMode)
			{
				case CompMode.strict_equal:
					return "===";
				default:
					return "!!Known";
			}
		}


		protected override void ReadFromBinary(BinaryReader br)
		{
			offset = br.ReadInt32();

			compMode = (CompMode)br.ReadByte();
			jump_mode = br.ReadBoolean();
			v1 = br.ReadByte();
			v2 = br.ReadByte();
		}

		protected override void WriteByte(BinaryWriter bw)
		{
			bw.Write(offset);
			bw.Write((byte)compMode);
			bw.Write(jump_mode);
			bw.Write(v1);
			bw.Write(v2);
			
		}

		public override string ToString()
		{
			return $"If_LogicOp_Goto	( if( [stack:{v1}] {GetCompModeString()} [stack:{v2}] == { jump_mode } ) goto  FLAG_{flag_id})";
		}

	}
}
