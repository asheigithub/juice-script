using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using juicescript.ABC.Locaters;

namespace juicescript.ABC.INS
{
    public sealed class INS_Ld_False : Instruction
    {
        public override INS_Code INS_Code =>  INS_Code.ld_false;

       
        public INS_Ld_False(Token token) : base(token)
        {
        }

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
            return $"Ld_False   [{dst}] <- false";
        }

    }
}
