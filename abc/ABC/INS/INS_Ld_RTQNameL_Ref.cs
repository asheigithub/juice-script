using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
    public sealed class INS_Ld_RTQNameL_Ref : Instruction
    {
        public override INS_Code INS_Code => INS_Code.ld_RTQNameL_Ref;

        public override int Size
        {
            get
            {
                return 4 +4 + 4 + 4;
            }
        }

        
        public StackLocater instance;
        public StackLocater ns;
        public StackLocater name;

        public INS_Ld_RTQNameL_Ref(Token token) : base(token)
        {
        }

        protected override void WriteByte(BinaryWriter bw)
        {
           
            instance.Write(bw);
            ns.Write(bw);
            name.Write(bw);
        }

		protected override void ReadFromBinary(BinaryReader br)
		{
			
            instance.ReadFromBinary(br);
            ns.ReadFromBinary(br);
            name.ReadFromBinary(br);
		}

        public override string ToString()
        {
            return $"Ld_RTQNameL_Ref [{dst}] <- [{instance}.{ns}::{name}]";
        }

        public override List<StackLocater> GetDef()
        {
            return new List<StackLocater> { dst };
        }

        public override List<StackLocater> GetUse()
        {
            return new List<StackLocater> { instance, ns, name };
        }

        public override bool MaybeRaiseError()
        {
            return true;
        }

    }
}
