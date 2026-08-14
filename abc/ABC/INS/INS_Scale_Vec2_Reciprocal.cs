using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
	public sealed class INS_Scale_Vec2_Reciprocal : Instruction
	{
		public override INS_Code INS_Code => INS_Code.scale_Vec2_reciprocal;

		public StackLocater vec;

		public StackLocater factor;

		public INS_Scale_Vec2_Reciprocal(Token token) : base(token)
		{
		}

		public override int Size
		{
			get
			{
				return 4 + 4 + 4;
			}
		}

		protected override void WriteByte(BinaryWriter bw)
		{

			vec.Write(bw);
			factor.Write(bw);
		}

		protected override void ReadFromBinary(BinaryReader br)
		{

			vec.ReadFromBinary(br);
			factor.ReadFromBinary(br);
		}


		public override string ToString()
		{
			return $"Scale_Vec2   [{dst}]<- (Vector2)[{vec}].Scale(1.0/{factor})";
		}

		public override IEnumerable<StackLocater> GetDef()
		{
			//return new List<StackLocater> { dst };
			yield return dst;
		}

		public override IEnumerable<StackLocater> GetUse()
		{
			//return new List<StackLocater> { v1, v2 };
			yield return vec;
			yield return factor;
		}

		public override bool MaybeRaiseError()
		{
			return true; //当参数为null时抛出
		}

		public override void RemappingSlots(Dictionary<int, int> mapping)
		{
			if (mapping.TryGetValue(dst.index, out int newIndex))
				dst.index = newIndex;
			if (mapping.TryGetValue(vec.index, out int newIndex1))
				vec.index = newIndex1;
			if (mapping.TryGetValue(factor.index, out int newIndex2))
				factor.index = newIndex2;
		}
	}
}
