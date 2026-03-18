using juicescript.ABC.Locaters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
    public sealed class INS_Ld_ValueRef : Instruction
    {
        public INS_Ld_ValueRef(Token token) : base(token)
        {
        }

        public override INS_Code INS_Code => INS_Code.ld_ValueRef;

		
		public StackLocater source;
        
        public override int Size
        {
            get
            {
                return  4 + 4;
            }
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
            return $"Ld_ValueRef   [{dst}]<-[{source}]";
        }

        public override List<StackLocater> GetDef()
        {
            return new List<StackLocater> { dst };
        }

        public override List<StackLocater> GetUse()
        {
            return new List<StackLocater> { source };
        }

        public override bool MaybeRaiseError()
        {
            return true;
        }

    }
}
