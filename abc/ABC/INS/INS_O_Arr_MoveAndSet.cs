using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Text;

namespace juicescript.ABC.INS
{
	public sealed class INS_O_Arr_MoveAndSet : Instruction
	{
		public INS_O_Arr_MoveAndSet(Token token) : base(token)
		{
		}
		public override INS_Code INS_Code => INS_Code.O_Array_MoveAndSet;

		public override int Size
		{
			get
			{
				return 4 + 4 + 4 + 4 + 4 +4;
			}
		}


		public StackLocater instance;
		public StackLocater name;
		public StackLocater refholder;

		public StackLocater index2;
		public StackLocater value2;

		protected override void WriteByte(BinaryWriter bw)
		{

			instance.Write(bw);
			name.Write(bw);
			refholder.Write(bw);

			index2.Write(bw);
			value2.Write(bw);

		}

		protected override void ReadFromBinary(BinaryReader br)
		{

			instance.ReadFromBinary(br);
			name.ReadFromBinary(br);
			refholder.ReadFromBinary(br);

			index2.ReadFromBinary(br);
			value2.ReadFromBinary(br);
		}

		public override IEnumerable<StackLocater> GetDef()
		{
			//return new List<StackLocater> { dst};
			yield return dst;
		}

		public override IEnumerable<StackLocater> GetUse()
		{
			//return new List<StackLocater> { instance, name, refholder };
			yield return instance;
			yield return name;
			yield return refholder;

			yield return index2;
			yield return value2;

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
			if (mapping.TryGetValue(name.index, out int newIndex2))
				name.index = newIndex2;
			if (mapping.TryGetValue(refholder.index, out int newIndex3))
				refholder.index = newIndex3;

			if (mapping.TryGetValue(index2.index, out int newIndex4))
				index2.index = newIndex4;

			if (mapping.TryGetValue(value2.index, out int newIndex5))
				value2.index = newIndex5;
		}

		public override string ToString()
		{
			return $"O_Arr_MoveAndSet [{dst}] <- ({instance})[{name}]; ({instance})[{index2}]<-[{dst}];({instance})[{name}]<-[{value2}];   ";
		}

	}
}
