using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
    public sealed class INS_Ld_this : Instruction
    {
        public INS_Ld_this(Token token) : base(token)
        {
        }

        public override INS_Code INS_Code => INS_Code.ld_This;

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
            return $"Ld_This   [{dst}] <- this";
        }

    }
}
