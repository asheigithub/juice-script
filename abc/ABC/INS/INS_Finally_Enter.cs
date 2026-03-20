using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
	public sealed class INS_Finally_Enter : Instruction
	{
		public INS_Finally_Enter(Token token) : base(token)
		{
		}

		public override INS_Code INS_Code => INS_Code.finally_enter;


		public override int Size => 4;

		protected override void WriteByte(BinaryWriter bw)
		{
			
		}

		protected override void ReadFromBinary(BinaryReader br)
		{
			
		}

		public override string ToString()
		{
			return "FINALLY_ENTER";
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
