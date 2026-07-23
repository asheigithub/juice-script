using juicescript.ABC.Locaters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
	public sealed class INS_Ld_MemberInitValue : Instruction
	{
		public override INS_Code INS_Code => INS_Code.ld_memberInitValue;

		public override int Size
		{
			get
			{
				return 4 + 4;
			}
		}


		public ScopeHeapLocater heap;

		public INS_Ld_MemberInitValue(Token token) : base(token)
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
			return $"Ld_MemberInit   [{heap}]";
		}

        public override IEnumerable<StackLocater> GetDef()
        {
			//return new List<StackLocater> {  };
			yield break;
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
