using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
    public sealed class INS_Store_HeapValueRef : Instruction
    {
        public override INS_Code INS_Code =>  INS_Code.storeHeapValueRef;

        public override int Size
        {
            get
            { 
                return 4 + 4;
            }
        }

        

        public StackLocater source;

        public INS_Store_HeapValueRef(Token token) : base(token)
        {
        }

        protected override void WriteByte(BinaryWriter bw)
        {
            
            source.Write(bw);
        }

		protected override void ReadFromBinary(BinaryReader br)
		{
			
            source.ReadFromBinary(br);
		}

        public override string ToString()
        {
            return $"Store_HeapValueRef [{dst}] <- [{source}]";
        }

    }
}
