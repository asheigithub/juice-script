using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
    public sealed class INS_Ld_MultiNameL_Val : Instruction
    {
        public INS_Ld_MultiNameL_Val(Token token) : base(token)
        {
        }

        public override INS_Code INS_Code => INS_Code.ld_MultiNameL_Val;

        public override int Size
        {
            get
            {
                return 4 + 4 + 4 + 4;
            }
        }

        
        public StackLocater instance;
        public StackLocater name;
        public StackLocater refholder;

        protected override void WriteByte(BinaryWriter bw)
        {
            
            instance.Write(bw);
            name.Write(bw);
            refholder.Write(bw); 
        }

		protected override void ReadFromBinary(BinaryReader br)
		{
			
            instance.ReadFromBinary(br);
            name.ReadFromBinary(br);
			refholder.ReadFromBinary(br);
		}

        public override string ToString()
        {
            return $"Ld_MultiNameL_Val [{dst}] <- [({instance}).{name}]";
        }

        public override IEnumerable<StackLocater> GetDef()
        {
            //return new List<StackLocater> { dst};
            yield return dst;
        }

        public override IEnumerable<StackLocater> GetUse()
        {
            //return new List<StackLocater> { instance, name, refholder };
            yield return instance;
            yield return name;
            yield return refholder; 
        }

        public override bool MaybeRaiseError()
        {
            return true;
        }

        public override void RemappingSlots(Dictionary<int, int> mapping)
        {
            if (mapping.TryGetValue(dst.index, out int newIndex))
                dst.index = newIndex;
            if (mapping.TryGetValue(instance.index, out int newIndex1))
                instance.index = newIndex1;
            if (mapping.TryGetValue(name.index, out int newIndex2))
                name.index = newIndex2;
			if (mapping.TryGetValue(refholder.index, out int newIndex3))
				refholder.index = newIndex3;

		}



    }
}
