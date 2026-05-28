using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
	public sealed class INS_Comparison : Instruction
	{
		public INS_Comparison(Token token) : base(token)
		{
		}

		public StackLocater v1;

		public StackLocater v2;

		/// <summary>
		/// 0,1,2,3  小于,大于,小于等于,大于等于
		/// </summary>
		public byte opMode;

		public override INS_Code INS_Code =>  INS_Code.logic_comparison;

		public override int Size => 4 + 4 + 4;

		protected override void ReadFromBinary(BinaryReader br)
		{
			uint v = br.ReadUInt32();
			opMode =(byte)( v & 0xff);
			v1.index = (int)(v >> 8);
			v2.ReadFromBinary(br);

		}

		protected override void WriteByte(BinaryWriter bw)
		{
			if (v1.index > 0xffffff)
			{
				throw new InvalidOperationException();
			}

			uint v = (uint)v1.index << 8 | opMode;
			bw.Write(v);
			v2.Write(bw);
		}

		public override string ToString()
		{
			return $"logic_cmp [{dst}] <- [{v1}] {( opMode==0 ? "<" : opMode == 1 ? ">" : opMode == 2 ? "<=" : ">=" )} [{v2}] ";

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
