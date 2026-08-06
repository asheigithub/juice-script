using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
	public sealed class INS_O_Ld_InstanceFiled : Instruction
	{
		public INS_O_Ld_InstanceFiled(Token token) : base(token)
		{
		}

		public override INS_Code INS_Code => INS_Code.O_Ld_InstanceFiled;

		public override int Size
		{
			get
			{
				return 4 + 4 + 4 + 4 + 4;
			}
		}


		public StackLocater instance;
		public int scopemember_index;

		public ushort offset;
		public ushort slotsize;

		public uint mask;


		protected override void ReadFromBinary(BinaryReader br)
		{
			instance.ReadFromBinary(br);
			scopemember_index = br.ReadInt32();

			offset = br.ReadUInt16();
			slotsize = br.ReadUInt16();

			mask = br.ReadUInt32();

		}

		protected override void WriteByte(BinaryWriter bw)
		{
			instance.Write(bw);
			bw.Write(scopemember_index);
			bw.Write(offset);
			bw.Write(slotsize);
			bw.Write(mask);
		}


		public override string ToString()
		{
			return $"O_Ld_InstanceField   [{dst}] <- [{instance} . scopemember: {scopemember_index},offset:{offset},size:{slotsize}]";
		}


		public override IEnumerable<StackLocater> GetDef()
		{
			yield return dst;
		}

		public override IEnumerable<StackLocater> GetUse()
		{
			yield return instance;
		}

		public override bool MaybeRaiseError()
		{
			return true; // access null
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
