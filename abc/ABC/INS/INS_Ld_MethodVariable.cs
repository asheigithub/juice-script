using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
    public sealed class INS_Ld_MethodVariable : Instruction
    {
        public override INS_Code INS_Code => INS_Code.ld_methodVariable;

        public override int Size
        {
            get
            {
                return 4 + 4;
            }
        }

       
        public ScopeHeapLocater heap;

        public INS_Ld_MethodVariable(Token token) : base(token)
        {
        }

        protected override void WriteByte(BinaryWriter bw)
        {
           
            heap.Write(bw);
        }

		protected override void ReadFromBinary(BinaryReader br)
		{
		   
            heap.ReadFromBinary(br);
		}

        public override string ToString()
        {
            return $"Ld_MethodVar   [{dst}] <- [{heap}]";
        }

        public override List<StackLocater> GetDef()
        {
            return new List<StackLocater> { dst };
        }

        public override List<StackLocater> GetUse()
        {
            return new List<StackLocater>();
        }

        public override bool MaybeRaiseError()
        {
            return false;
        }

    }

}
