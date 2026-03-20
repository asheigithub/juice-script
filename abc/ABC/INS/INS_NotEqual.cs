using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using juicescript.ABC.Locaters;

namespace juicescript.ABC.INS
{
    public sealed class INS_NotEqual : Instruction
    {
        public override INS_Code INS_Code => INS_Code.not_equal;

       
        public StackLocater v1;

        public StackLocater v2;

        public INS_NotEqual(Token token) : base(token)
        {
        }

        public override int Size
        {
            get
            {
                return 4 + 4 + 4;
            }
        }

        protected override void WriteByte(BinaryWriter bw)
        {
          
            v1.Write(bw);
            v2.Write(bw);
        }

		protected override void ReadFromBinary(BinaryReader br)
		{
			
            v1.ReadFromBinary(br);
            v2.ReadFromBinary(br);
		}


		public override string ToString()
        {
            return $"NotEqual(!=)   [{dst}]<- [{v1}] != [{v2}]";
        }

        public override List<StackLocater> GetDef()
        {
            return new List<StackLocater> { dst };
        }

        public override List<StackLocater> GetUse()
        {
            return new List<StackLocater> { v1, v2 };
        }

        public override bool MaybeRaiseError()
        {
            return true;
        }

        public override void RemappingSlots(Dictionary<int, int> mapping)
        {
            if (mapping.TryGetValue(dst.index, out int newIndex))
                dst.index = newIndex;
            if (mapping.TryGetValue(v1.index, out int newIndex1))
                v1.index = newIndex1;
            if (mapping.TryGetValue(v2.index, out int newIndex2))
                v2.index = newIndex2;
        }
		
	}
}
