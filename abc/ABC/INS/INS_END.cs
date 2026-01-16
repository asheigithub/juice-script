using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    }
}
