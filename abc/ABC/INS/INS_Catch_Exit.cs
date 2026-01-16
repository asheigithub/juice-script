using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
	public sealed class INS_Catch_Exit : Instruction
	{
		public INS_Catch_Exit(Token token) : base(token)
		{
		}

		public override INS_Code INS_Code => INS_Code.catch_exit;

		public override int Size => 4;

		protected override void WriteByte(BinaryWriter bw)
		{
			
		}

		protected override void ReadFromBinary(BinaryReader br)
		{
			
		}

		public override string ToString()
		{
			return "CATCH_EXIT";
		}

	}
}
