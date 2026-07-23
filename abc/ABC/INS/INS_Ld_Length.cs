using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
	public sealed class INS_Ld_Length : Instruction
	{
		public INS_Ld_Length(Token token) : base(token)
		{
		}

		public override INS_Code INS_Code => INS_Code.ld_length;

		public override int Size => 4 + 4;

		public StackLocater instacnce;

		public override IEnumerable<StackLocater> GetDef()
		{
			//return new List<StackLocater>() { dst};
			yield return dst;
		}

		public override IEnumerable<StackLocater> GetUse()
		{
			//return new List<StackLocater>() { instacnce };
			yield return instacnce;
		}

		public override bool MaybeRaiseError()
		{
			return true; //访问null
		}

		public override void RemappingSlots(Dictionary<int, int> mapping)
		{
			if (mapping.TryGetValue(dst.index, out int newIndex))
				dst.index = newIndex;

			if (mapping.TryGetValue(instacnce.index, out newIndex))
				instacnce.index = newIndex;

		}

		protected override void ReadFromBinary(BinaryReader br)
		{
			instacnce.ReadFromBinary(br);
		}

		protected override void WriteByte(BinaryWriter bw)
		{
			instacnce.Write(bw);
		}


		public override string ToString()
		{
			return $"Ld_Length   [{dst}] <- {instacnce}.length";
		}

	}
}
