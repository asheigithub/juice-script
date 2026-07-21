using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
	/// <summary>
	/// 保存进instance的成员中
	/// 用dst承载要保存的值
	/// </summary>
	public sealed class INS_Store_InstanceMember : Instruction
	{
		public override INS_Code INS_Code => INS_Code.store_instanceMember;

		public override int Size
		{
			get
			{
				return 4 + 4 + 4;
			}
		}

		public StackLocater instance;

		public uint scopemember_index;

		public INS_Store_InstanceMember(Token token) : base(token)
		{
		}

		protected override void WriteByte(BinaryWriter bw)
		{

			instance.Write(bw);
			bw.Write(scopemember_index);
		}

		protected override void ReadFromBinary(BinaryReader br)
		{

			instance.ReadFromBinary(br);
			scopemember_index = br.ReadUInt32();
		}

		public override string ToString()
		{
			return $"Store_InstanceMember   [{instance} . scopemember: {scopemember_index}]<- [{dst}]";
		}

		public override List<StackLocater> GetDef()
		{
			return new List<StackLocater> {  };
		}

		public override List<StackLocater> GetUse()
		{
			return new List<StackLocater> {dst, instance };
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
