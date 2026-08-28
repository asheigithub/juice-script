using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
	public sealed class INS_O_Store_InstanceField : Instruction
	{
		public override INS_Code INS_Code => INS_Code.O_Store_InstanceField;

		public override int Size
		{
			get
			{
				return 4 + 4 + 4 + 4;
			}
		}

		public StackLocater instance;

		public ushort typekind;
		public ushort scopemember_index;
		public ushort offset;
		public ushort slotsize;


		public INS_O_Store_InstanceField(Token token) : base(token)
		{
		}

		protected override void WriteByte(BinaryWriter bw)
		{

			instance.Write(bw);
			bw.Write(typekind);
			bw.Write(scopemember_index);
			bw.Write(offset);
			bw.Write(slotsize);
		}

		protected override void ReadFromBinary(BinaryReader br)
		{

			instance.ReadFromBinary(br);
			typekind = br.ReadUInt16();
			scopemember_index = br.ReadUInt16();
			offset = br.ReadUInt16();
			slotsize = br.ReadUInt16();
		}

		public override string ToString()
		{
			return $"O_Store_InstanceField   [{instance} . field: {scopemember_index}]<- [{dst}]";
		}

		public override IEnumerable<StackLocater> GetDef()
		{
			//return new List<StackLocater> {  };
			yield break;
		}

		public override IEnumerable<StackLocater> GetUse()
		{
			//return new List<StackLocater> {dst, instance };
			yield return dst;
			yield return instance;
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
		}


	}
}
