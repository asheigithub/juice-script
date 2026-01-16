using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
	public sealed class INS_Catch_Enter : Instruction
	{
		public INS_Catch_Enter(Token token) : base(token)
		{
		}

		public override INS_Code INS_Code => INS_Code.catch_enter;

		

		public ScopeHeapLocater catch_exception;

		public override int Size
		{
			get
			{
				return 4
					+ 4;
			}
		}

		protected override void WriteByte(BinaryWriter bw)
		{
			//dst无用

			catch_exception.Write(bw);
		}

		protected override void ReadFromBinary(BinaryReader br)
		{
			
			catch_exception.ReadFromBinary(br);
		}

		public override string ToString()
		{
			return $"CATCH_ENTER ex:{catch_exception}";
		}

		
	}
}
