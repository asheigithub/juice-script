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
			uint store = br.ReadUInt32();

			compMode = (CompMode)(store & 0xff);
			jump_mode = (store >> 8 & 0xff) > 0;
			v1 = (byte)(store >> 16 & 0xff);
			v2 = (byte)(store >> 24 & 0xff);
		}

		protected override void WriteByte(BinaryWriter bw)
		{
			bw.Write(offset);
			uint store = (uint)compMode | ((uint)(jump_mode ? 1 : 0) << 8) | ((uint)v1 << 16) | ((uint)v2 << 24);
			bw.Write(store);
		}

		public override string ToString()
		{
			return $"If_LogicOp_Goto	( if( [stack:{v1}] {GetCompModeString()} [stack:{v2}] == { jump_mode } ) goto  FLAG_{flag_id})";
		}

        public override List<StackLocater> GetDef()
        {
            return new List<StackLocater>();
        }

        public override List<StackLocater> GetUse()
        {
            return new List<StackLocater> 
            { 
                new StackLocater { index = v1 }, 
                new StackLocater { index = v2 } 
            };
        }

        public override bool MaybeRaiseError()
        {
            return false;
        }

	}
}
