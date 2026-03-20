using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
	public sealed class INS_Typeof : Instruction
	{
		public override INS_Code INS_Code => INS_Code.get_typeof;


		

		public StackLocater src;

		public INS_Typeof(Token token) : base(token)
		{
		}

		public override int Size
		{
			get
			{
				// opcode (1) + dst (4) + src (4)
				return  4 + 4;
			}
		}

		protected override void WriteByte(BinaryWriter bw)
		{
			
			src.Write(bw);
		}

		protected override void ReadFromBinary(BinaryReader br)
		{
			
			src.ReadFromBinary(br);
		}

		public override string ToString()
		{
			return $"typeof  [{dst}] <- [{src}]";
		}

        public override List<StackLocater> GetDef()
        {
            return new List<StackLocater> { dst };
        }

        public override List<StackLocater> GetUse()
        {
            return new List<StackLocater> { src };
        }

        public override bool MaybeRaiseError()
        {
            return false;
        }

        public override void RemappingSlots(Dictionary<int, int> mapping)
        {
            if (mapping.TryGetValue(dst.index, out int newIndex))
                dst.index = newIndex;
            if (mapping.TryGetValue(src.index, out int newIndex1))
                src.index = newIndex1;
        }


	}
}
