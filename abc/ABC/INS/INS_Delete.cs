using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
    public sealed class INS_Delete : Instruction
    {
        public override INS_Code INS_Code => INS_Code.delete;

       

        public StackLocater todelete;

        public INS_Delete(Token token) : base(token)
        {
        }

        public override int Size
        {
            get
            {
                return 4 + 4 ;
            }
        }

        protected override void WriteByte(BinaryWriter bw)
        {
           
            todelete.Write(bw);
        }

		protected override void ReadFromBinary(BinaryReader br)
		{
			
            todelete.ReadFromBinary(br);
		}

        public override string ToString()
        {
            return $"Delete(/)   [{dst}]<- delete [{todelete}]";
        }

        public override List<StackLocater> GetDef()
        {
            return new List<StackLocater> { dst };
        }

        public override List<StackLocater> GetUse()
        {
            return new List<StackLocater> { todelete };
        }

        public override bool MaybeRaiseError()
        {
            return true;
        }

    }
}
