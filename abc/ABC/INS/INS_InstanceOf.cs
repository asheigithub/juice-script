using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
	public sealed class INS_InstanceOf : Instruction
	{
		public override INS_Code INS_Code => INS_Code.get_instanceof;

		public StackLocater src;

		public StackLocater type;
		public INS_InstanceOf(Token token) : base(token)
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
			
			src.Write(bw);
			type.Write(bw);
		}

		protected override void ReadFromBinary(BinaryReader br)
		{
			
			src.ReadFromBinary(br);
			type.ReadFromBinary(br);
		}

		public override string ToString()
		{
			return $"instanceof  [{dst}] <- [{src} instanceof {type}]";
		}

        public override IEnumerable<StackLocater> GetDef()
        {
			//return new List<StackLocater> { dst };
			yield return dst;
        }

        public override IEnumerable<StackLocater> GetUse()
        {
			//return new List<StackLocater> { src, type };
			yield return src;
			yield return type;
        }

        public override bool MaybeRaiseError()
        {
            return true;
        }


        public override void RemappingSlots(Dictionary<int, int> mapping)
        {
            if (mapping.TryGetValue(dst.index, out int newIndex))
                dst.index = newIndex;
            if (mapping.TryGetValue(src.index, out int newIndex1))
                src.index = newIndex1;
            if (mapping.TryGetValue(type.index, out int newIndex2))
                type.index = newIndex2;
        }


	}
}
