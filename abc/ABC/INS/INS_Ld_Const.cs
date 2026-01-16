using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using juicescript.ABC.Locaters;

namespace juicescript.ABC.INS
{
    public sealed class INS_Ld_Const : Instruction
    {
        public override INS_Code INS_Code =>  INS_Code.ld_const;

        public override int Size
        {
            get 
            { 
               
                return 4 + 4;
            }
        }

       

        public int const_index;

        public INS_Ld_Const(Token token) : base(token)
        {
        }

        protected override void WriteByte(BinaryWriter bw)
        {
          
            bw.Write(const_index);

        }

		protected override void ReadFromBinary(BinaryReader br)
		{
			
            const_index = br.ReadInt32();
		}

        public override string ToString()
        {
            return $"Ld_Const   [{dst}] <- [const id: {const_index}]";
        }

    }
}
