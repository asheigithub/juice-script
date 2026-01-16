using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
	public sealed class INS_Multiply : Instruction
	{
		public INS_Multiply(Token token) : base(token)
		{
		}

		public override INS_Code INS_Code => INS_Code.multiply;

		public StackLocater v1;

		public StackLocater v2;

		public override int Size => 4 + 4 +4;

		protected override void ReadFromBinary(BinaryReader br)
		{
			v1.ReadFromBinary(br);
			v2.ReadFromBinary(br);
		}

		protected override void WriteByte(BinaryWriter bw)
		{
			v1.Write(bw);
			v2.Write(bw);
		}

		public override string ToString()
		{
			return $"Multiply(*)   [{dst}]<- [{v1}],[{v2}]";
		}
	}
}
