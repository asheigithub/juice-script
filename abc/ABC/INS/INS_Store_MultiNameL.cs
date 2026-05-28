using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using juicescript.ABC.Locaters;

namespace juicescript.ABC.INS
{
    /// <summary>
    /// 将数据保存到MultiNameL  这里 dst承载要保存的数据的地址
    /// </summary>
    public sealed class INS_Store_MultiNameL : Instruction
    {

		public INS_Store_MultiNameL(Token token) : base(token)
		{
		}

		public override INS_Code INS_Code => INS_Code.store_MultiNameL;

		public override int Size
		{
			get
			{
				return 4 + 4 + 4 + 4 + 4;
			}
		}


		public StackLocater instance;
		public StackLocater name;
		public StackLocater tmp_holder;


		public int super_type_index;

		protected override void WriteByte(BinaryWriter bw)
		{

			instance.Write(bw);
			name.Write(bw);
			tmp_holder.Write(bw);
			bw.Write(super_type_index);
		}

		protected override void ReadFromBinary(BinaryReader br)
		{

			instance.ReadFromBinary(br);
			name.ReadFromBinary(br);
			tmp_holder.ReadFromBinary(br);
			super_type_index = br.ReadInt32();
		}

		public override string ToString()
		{
			return $"Store_MultiNameL [(({super_type_index}){instance}).{name}] <- {dst}";
		}

		public override List<StackLocater> GetDef()
		{
			return new List<StackLocater> {};
		}

		public override List<StackLocater> GetUse()
		{
			return new List<StackLocater> { instance, name ,dst,tmp_holder };
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
