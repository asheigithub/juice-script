using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using juicescript.ABC.Locaters;

namespace juicescript.ABC.INS
{
    public sealed class INS_Positive : Instruction
    {
        public override INS_Code INS_Code => INS_Code.positive;


        public StackLocater src;

        public INS_Positive(Token token) : base(token)
        {
        }

        public override int Size
        {
            get
            {
                // opcode (1) + dst (4) + src (4)
                return 4 + 4;
            }
        }

        protected override void WriteByte(BinaryWriter bw)
        {
            src.Write(bw);
        }

		protected override void ReadFromBinary(BinaryReader br)
		{
            src.ReadFromBinary(br);
		}

        public override string ToString()
        {
            return $"Positive(+)  [{dst}] <- [{src}]";
        }

        
    }
}
