using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using juicescript.ABC.Locaters;

namespace juicescript.ABC.INS
{
    public sealed class INS_END : Instruction
    {
        public INS_END() : base(null)
        {
        }

        public override INS_Code INS_Code => INS_Code.END;

        public override int Size
        {
            get
            {
                return 4;
            }
        }

        protected override void WriteByte(BinaryWriter bw)
        {
           
        }

		protected override void ReadFromBinary(BinaryReader br)
		{
			
		}

        public override string ToString()
        {
            return "END";
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

    }
}
