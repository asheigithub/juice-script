using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
	public sealed class INS_In : Instruction
	{
		public INS_In(Token token) : base(token)
		{
		}

		public override INS_Code INS_Code => INS_Code.get_in;

		public override int Size => 4 + 4 + 4;


		public StackLocater v1;

		public StackLocater v2;

		protected override void WriteByte(BinaryWriter bw)
		{
			v1.Write(bw);
			v2.Write(bw);
		}

		protected override void ReadFromBinary(BinaryReader br)
		{
			v1.ReadFromBinary(br);
			v2.ReadFromBinary(br);
		}


		public override string ToString()
		{
			return $"In   [{dst}]<- [{v1}] in [{v2}]";
		}
	}
}
