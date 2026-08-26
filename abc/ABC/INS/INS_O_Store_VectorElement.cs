using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
	public sealed class INS_O_Store_VectorElement : Instruction
	{
		public INS_O_Store_VectorElement(Token token) : base(token)
		{
		}

		public override INS_Code INS_Code => INS_Code.O_Store_Vector_Element;

		public override int Size
		{
			get
			{
				return 4 + 4 + 4 + 4;
			}
		}


		public StackLocater instance;
		public StackLocater name;
		public StackLocater tmp_holder;

		protected override void WriteByte(BinaryWriter bw)
		{

			instance.Write(bw);
			name.Write(bw);
			tmp_holder.Write(bw);

		}

		protected override void ReadFromBinary(BinaryReader br)
		{

			instance.ReadFromBinary(br);
			name.ReadFromBinary(br);
			tmp_holder.ReadFromBinary(br);

		}

		public override string ToString()
		{
			return $"O_Store_VectorElement [({instance})[{name}] <- {dst}";
		}

		public override IEnumerable<StackLocater> GetDef()
		{
			//return new List<StackLocater> {};
			yield break;
		}

		public override IEnumerable<StackLocater> GetUse()
		{
			//return new List<StackLocater> { instance, name ,dst,tmp_holder };
			yield return instance;
			yield return name;
			yield return dst;
			yield return tmp_holder;
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
			if (mapping.TryGetValue(tmp_holder.index, out int newIndex3))
				tmp_holder.index = newIndex3;
		}


	}
}
