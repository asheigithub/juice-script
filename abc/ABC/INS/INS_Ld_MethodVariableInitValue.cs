using juicescript.ABC.Locaters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
	public sealed class INS_Ld_MethodVariableInitValue : Instruction
	{
		public override INS_Code INS_Code => INS_Code.ld_MethodVariableInitValue;

		public override int Size
		{
			get
			{
				return 4 + 4 + 8;
			}
		}


		public ScopeHeapLocater heap;

		public NaNBoxing cacheraw;

		public INS_Ld_MethodVariableInitValue(Token token) : base(token)
		{
		}

		protected override void WriteByte(BinaryWriter bw)
		{
			heap.Write(bw);
			bw.Write(cacheraw.Raw);

		}

		protected override void ReadFromBinary(BinaryReader br)
		{
			heap.ReadFromBinary(br);

			ulong raw = br.ReadUInt64();
			cacheraw = new NaNBoxing(raw);

		}

		public override string ToString()
		{
			return $"Ld_VariableInit   [{heap}]";
		}

        public override List<StackLocater> GetDef()
        {
            return new List<StackLocater> {  };
        }

        public override List<StackLocater> GetUse()
        {
            return new List<StackLocater>();
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
