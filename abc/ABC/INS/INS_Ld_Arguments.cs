using juicescript.ABC.Locaters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
    public sealed class INS_Ld_Arguments : Instruction
    {
        public INS_Ld_Arguments(Token token) : base(token)
        {
        }

        public override INS_Code INS_Code => INS_Code.ld_arguments;

       

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
            return $"INS_Ld_Arguments   [{dst}] <- arguments";
        }

    }
}
