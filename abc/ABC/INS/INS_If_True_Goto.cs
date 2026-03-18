using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
	public sealed class INS_If_True_Goto : Instruction
	{
		public INS_If_True_Goto(Token token) : base(token)
		{
		}

		public override INS_Code INS_Code => INS_Code.if_true_goto;

		public override int Size =>  4 + 4 + 4;

		public int flag_id
		{
			get { return dst.index; }
			set { dst.index = value; }
		}

		public int offset;
		public StackLocater condition;


		protected override void ReadFromBinary(BinaryReader br)
		{
			
			offset = br.ReadInt32();
			condition.ReadFromBinary(br);
		}

		protected override void WriteByte(BinaryWriter bw)
		{
			
			bw.Write(offset);
			condition.Write(bw);
		}

		public override string ToString()
		{
			return $"If_True_Goto	( if( {condition} == true ) goto  FLAG_{flag_id})";
		}

        public override List<StackLocater> GetDef()
        {
            return new List<StackLocater>();
        }

        public override List<StackLocater> GetUse()
        {
            return new List<StackLocater> { condition };
        }

        public override bool MaybeRaiseError()
        {
            return false;
        }

	}
}
