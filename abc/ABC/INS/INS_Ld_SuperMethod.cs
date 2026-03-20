using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
    public sealed class INS_Ld_SuperMethod : Instruction
    {
        public INS_Ld_SuperMethod(Token token) : base(token)
        {
        }

        public override INS_Code INS_Code => INS_Code.ld_supermethod;

        public override int Size
        {
            get
            {
                return 4 + 4 + 4;
            }
        }


        
        public StackLocater instance;
        public int const_index;

        protected override void WriteByte(BinaryWriter bw)
        {
           
            instance.Write(bw);
            bw.Write(const_index);
        }

		protected override void ReadFromBinary(BinaryReader br)
		{
			
            instance.ReadFromBinary(br);
            const_index = br.ReadInt32();
		}

        public override string ToString()
        {
            return $"Ld_SuperMethod   [{dst}] <- [instance:{instance}].super.vtable[{const_index}]";
        }

        public override List<StackLocater> GetDef()
        {
            return new List<StackLocater> { dst };
        }

        public override List<StackLocater> GetUse()
        {
            return new List<StackLocater> { instance };
        }

        public override bool MaybeRaiseError()
        {
            return false;
        }

        public override void RemappingSlots(Dictionary<int, int> mapping)
        {
            if (mapping.TryGetValue(dst.index, out int newIndex))
                dst.index = newIndex;
            if (mapping.TryGetValue(instance.index, out int newIndex1))
                instance.index = newIndex1;
        }

    }
}
