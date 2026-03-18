using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
	/// <summary>
	/// dst 承载要抛的异常。
	/// </summary>
	public sealed class INS_Throw : Instruction
	{
		public INS_Throw(Token token) : base(token)
		{
		}


		public override INS_Code INS_Code => INS_Code.throw_error;

		public override int Size => 4;

		protected override void ReadFromBinary(BinaryReader br)
		{
			
		}

		protected override void WriteByte(BinaryWriter bw)
		{
			
		}

		public override string ToString()
		{
			return $"THROW {dst}";
		}

        public override List<StackLocater> GetDef()
        {
            return new List<StackLocater>();
        }

        public override List<StackLocater> GetUse()
        {
            return new List<StackLocater> { dst };
        }

        public override bool MaybeRaiseError()
        {
            return true;
        }

	}
}
