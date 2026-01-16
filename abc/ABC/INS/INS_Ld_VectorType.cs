using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
    public sealed class INS_Ld_VectorType : Instruction
    {
        public INS_Ld_VectorType(Token token) : base(token) { }
        public override INS_Code INS_Code => INS_Code.ld_VectorType;

        public override int Size
        {
            get 
            {
                return 4 + 4;
            }
        }

       
        public int vectortype_index;

        protected override void WriteByte(BinaryWriter bw)
        {
            
            bw.Write(vectortype_index);
        }

		protected override void ReadFromBinary(BinaryReader br)
		{
			
            vectortype_index = br.ReadInt32();
		}

        public override string ToString()
        {
            return $"Ld_VectorType   [{dst}] <- [vectortype_index: {vectortype_index}]";
        }


    }
}
