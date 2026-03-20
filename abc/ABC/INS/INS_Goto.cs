using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using juicescript.ABC.Locaters;

namespace juicescript.ABC.INS
{
	public sealed class INS_Goto : Instruction
	{
		public INS_Goto(Token token) : base(token)
		{

		}

		public override INS_Code INS_Code =>  INS_Code.goto_flag;

		public override int Size =>  4 + 4;

		public int flag_id
		{
			get { return dst.index; }
			set { dst.index = value; }
		}

		private int offset;
		
		public int jumpTrys
		{
			get
			{
				return offset >> 24;
			}

			set
			{ 
				offset = (value << 24) | jumpOffset;
			}

		}

		public int jumpOffset
		{
			get
			{
				return offset & 0xffffff;
			}

			set
			{ 
				offset = (jumpTrys <<24) | value;
			}

		}



		protected override void ReadFromBinary(BinaryReader br)
		{
			//flag_id = br.ReadInt32();
			offset = br.ReadInt32();
		}

		protected override void WriteByte(BinaryWriter bw)
		{
			//bw.Write(flag_id);
			bw.Write(offset);
		}

        public override string ToString()
        {
            return $"Goto Flag_{ flag_id }";
        }

        public override List<StackLocater> GetDef()
        {
            return new List<StackLocater>();
        }

        public override List<StackLocater> GetUse()
        {
            return new List<StackLocater>();
        }

        public override bool MaybeRaiseError()
        {
            return false;
        }

        public override void RemappingSlots(Dictionary<int, int> mapping)
        {
        }

	}
}
