using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
	/// <summary>
	/// 用dst承载要插入的元素。
	/// </summary>
	public sealed class INS_Array_Vector_InitElement : Instruction
	{
		public INS_Array_Vector_InitElement(Token token) : base(token)
		{
		}

		public override INS_Code INS_Code =>  INS_Code.array_vector_initelement;

		public override int Size => 4 + 4 + 4;

		public StackLocater instance;

		public int index;

		public override List<StackLocater> GetDef()
		{
			return new List<StackLocater>();
		}

		public override List<StackLocater> GetUse()
		{
			return new List<StackLocater>() { dst , instance };
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

		protected override void ReadFromBinary(BinaryReader br)
		{
			instance.ReadFromBinary(br);
			index = br.ReadInt32();
		}

		protected override void WriteByte(BinaryWriter bw)
		{
			instance.Write(bw);
			bw.Write(index);
		}

		public override string ToString()
		{
			return $"SetElement (instance:{instance})[{index}]={dst}";
		}

	}
}
