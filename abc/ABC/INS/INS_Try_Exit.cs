using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
	public sealed class INS_Try_Exit : Instruction
	{
		public override INS_Code INS_Code =>  INS_Code.try_exit;

		public INS_Try_Exit(Token token) : base(token)
		{

		}

		public override int Size
		{
			get
			{
				return 4;
			}
		}

		protected override void WriteByte(BinaryWriter bw)
		{
			
		}

		protected override void ReadFromBinary(BinaryReader br)
		{
			
		}

		public override string ToString()
		{
			return "TRY_EXIT";
		}

	}
}
