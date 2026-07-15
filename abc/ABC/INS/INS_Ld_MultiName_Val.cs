using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
	public sealed class INS_Ld_MultiName_Val : Instruction
	{
		public INS_Ld_MultiName_Val(Token token) : base(token)
		{
		}

		public override INS_Code INS_Code => INS_Code.ld_MultiName_Val;

		public override int Size
		{
			get
			{
				return 4 + 4 + 4 + 4;
			}
		}


		public StackLocater instance;
		public int name_index;
		public StackLocater refholder;

		protected override void WriteByte(BinaryWriter bw)
		{

			instance.Write(bw);
			bw.Write(name_index);
			refholder.Write(bw);
		}

		protected override void ReadFromBinary(BinaryReader br)
		{

			instance.ReadFromBinary(br);
			name_index = br.ReadInt32();
			refholder.ReadFromBinary(br);
		}

		public override string ToString()
		{
			return $"Ld_MultiName_Val [{dst}] <- [{instance}.(name_index:{name_index})]";
		}

		public override List<StackLocater> GetDef()
		{
			return new List<StackLocater> { dst };
		}

		public override List<StackLocater> GetUse()
		{
			return new List<StackLocater> { instance,refholder };
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

			if (mapping.TryGetValue(refholder.index, out int newIndex2))
				refholder.index = newIndex2;


		}


	}

}
