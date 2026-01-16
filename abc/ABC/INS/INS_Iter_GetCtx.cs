using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
	public sealed class INS_Iter_GetCtx : Instruction
	{
		public INS_Iter_GetCtx(Token token) : base(token)
		{
		}

		public override INS_Code INS_Code =>  INS_Code.iter_initctx;

		public override int Size => 4;

		
		protected override void ReadFromBinary(BinaryReader br)
		{
			
		}

		protected override void WriteByte(BinaryWriter bw)
		{
			
		}

		public override string ToString()
		{
			return "ITER_GetContext";
		}

	}
}
