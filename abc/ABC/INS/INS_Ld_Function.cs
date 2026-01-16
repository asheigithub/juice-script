using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
    public sealed class INS_Ld_Function : Instruction
    {
        public INS_Ld_Function(Token token) : base(token)
        {
        }

        public override INS_Code INS_Code => INS_Code.ld_function ;

        public override int Size
        {
            get
            {
                return 4 + 4 + 4;
            }
        }

        
        public ScopeHeapLocater heapLocater;
        public int const_index;


        protected override void WriteByte(BinaryWriter bw)
        {
           
            heapLocater.Write(bw);
            bw.Write(const_index);
        }

		protected override void ReadFromBinary(BinaryReader br)
		{
			
            heapLocater.ReadFromBinary(br);
            const_index = br.ReadInt32();
		}


        public override string ToString()
        {
            return $"Ld_Function   [{dst}] <- [function: {const_index} at {heapLocater}]";
        }

    }
}
