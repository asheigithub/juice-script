using juicescript.ABC.Locaters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
    public sealed class INS_Move : Instruction
    {
        public INS_Move(Token token) : base(token)
        {
        }

        public override INS_Code INS_Code => INS_Code.move;

		
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
            return $"Move   [{dst}]<-[{source}]";
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
            return false;
        }

    }
}
