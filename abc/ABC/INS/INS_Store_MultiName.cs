using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace juicescript.ABC.INS
{
	/// <summary>
	/// 将数据保存到MultiName  这里 dst承载要保存的数据的地址
	/// </summary>
	public sealed class INS_Store_MultiName : Instruction
	{
		public INS_Store_MultiName(Token token) : base(token)
		{
		}

		public override INS_Code INS_Code =>  INS_Code.store_MultiName;

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

		protected override void ReadFromBinary(BinaryReader br)
		{
			instance.ReadFromBinary(br);
			name_index = br.ReadInt32();
			refholder.ReadFromBinary(br);
		}

		protected override void WriteByte(BinaryWriter bw)
		{
			instance.Write(bw);
			bw.Write(name_index);
			refholder.Write(bw);
		}


		public override string ToString()
		{
			return $"Store_MultiName [({instance}).[const name_idx:{name_index}]] <- {dst}";
		}

		public override List<StackLocater> GetDef()
		{
			return new List<StackLocater>();
		}

		public override List<StackLocater> GetUse()
		{
			return new List<StackLocater> { instance, refholder, dst };
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
