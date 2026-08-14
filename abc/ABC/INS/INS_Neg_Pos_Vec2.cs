using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
	public sealed class INS_Neg_Pos_Vec2 : Instruction
	{
		public override INS_Code INS_Code => INS_Code.neg_pos_Vec2;


		public StackLocater src;

		public bool is_positive;

		public INS_Neg_Pos_Vec2(Token token) : base(token)
		{
		}

		public override int Size
		{
			get
			{
				return 4 + 4;
			}
		}

		protected override void WriteByte(BinaryWriter bw)
		{
			if (src.index > 0xffffff)
				throw new InvalidOperationException();

			int store = (src.index << 1) | (is_positive ? 0 : 1);

			bw.Write(store);

			//src.Write(bw);
		}

		protected override void ReadFromBinary(BinaryReader br)
		{
			int store = br.ReadInt32();

			is_positive = (store & 1) == 0;

			src.index = store >> 1;

			//src.ReadFromBinary(br);
		}

		public override string ToString()
		{
			if (is_positive)
			{
				return $"Pos  [{dst}] <- (+(Vector2)[{src}])";
			}
			else
			{
				return $"Neg  [{dst}] <- (-(Vector2)[{src}])";
			}
		}

		public override IEnumerable<StackLocater> GetDef()
		{
			//return new List<StackLocater> { dst };
			yield return dst;
		}

		public override IEnumerable<StackLocater> GetUse()
		{
			//return new List<StackLocater> { src };
			yield return src;
		}

		public override bool MaybeRaiseError()
		{
			// v1 == null
			return true;
		}

		public override void RemappingSlots(Dictionary<int, int> mapping)
		{
			if (mapping.TryGetValue(dst.index, out int newIndex))
				dst.index = newIndex;
			if (mapping.TryGetValue(src.index, out int newIndex1))
				src.index = newIndex1;
		}
	}
}
