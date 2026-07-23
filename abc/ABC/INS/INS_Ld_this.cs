using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
    public sealed class INS_Ld_this : Instruction
    {
        public INS_Ld_this(Token token) : base(token)
        {
        }

        public override INS_Code INS_Code => INS_Code.ld_This;

        public override int Size
        {
            get
            {
                return 4;
            }
        }

       
        protected override void WriteByte(BinaryWriter bw)
        {
            
        }

		protected override void ReadFromBinary(BinaryReader br)
		{
			
		}

        public override string ToString()
        {
            return $"Ld_This   [{dst}] <- this";
        }

        public override IEnumerable<StackLocater> GetDef()
        {
            //return new List<StackLocater> { dst };
            yield return dst;
        }

        public override IEnumerable<StackLocater> GetUse()
        {
            //return new List<StackLocater>();
            yield break;
        }

        public override bool MaybeRaiseError()
        {
            return false;
        }

        public override void RemappingSlots(Dictionary<int, int> mapping)
        {
            if (mapping.TryGetValue(dst.index, out int newIndex))
                dst.index = newIndex;
        }

    }
}
