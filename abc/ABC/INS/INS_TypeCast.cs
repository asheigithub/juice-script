using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
	public sealed class INS_TypeCast : Instruction
	{
		public INS_TypeCast(Token token) : base(token)
		{
		}

		public override INS_Code INS_Code => INS_Code.type_cast;

		public override int Size => 4 + 4 + 4;

		public StackLocater value;
		public int class_id;

		protected override void ReadFromBinary(BinaryReader br)
		{
			value.ReadFromBinary(br);
			class_id = br.ReadInt32();
		}

		protected override void WriteByte(BinaryWriter bw)
		{
			value.Write(bw);
			bw.Write(class_id);
		}

		public override string ToString()
		{
			return $"Cast {dst} <- class_id:({class_id}){value} ";
		}

	}
}
