using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Text;

namespace juicescript.ABC.INS
{
	public sealed class INS_O_Ld_Arr_Var : Instruction
	{
		public INS_O_Ld_Arr_Var(Token token) : base(token)
		{
		}

		public override INS_Code INS_Code => INS_Code.O_Ld_Array_To_Var;

		public override int Size
		{
			get
			{
				return 4 + 4 + 4 + 4 + 4 ;
			}
		}


		public StackLocater instance;
		public StackLocater name;
		public StackLocater refholder;

		public ScopeHeapLocater heap;

		protected override void WriteByte(BinaryWriter bw)
		{

			instance.Write(bw);
			name.Write(bw);
			refholder.Write(bw);
			heap.Write(bw);
		}

		protected override void ReadFromBinary(BinaryReader br)
		{

			instance.ReadFromBinary(br);
			name.ReadFromBinary(br);
			refholder.ReadFromBinary(br);
			heap.ReadFromBinary(br);
		}

		public override string ToString()
		{
			return $"Ld_Arr_To_Var [offset:{heap.MemberIndex}] <- ({instance})[{name}],convertto:[{dst}]";
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

		}




	}
}
