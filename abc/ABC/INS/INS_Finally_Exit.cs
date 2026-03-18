using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
	public sealed class INS_Finally_Exit : Instruction
	{
		public INS_Finally_Exit(Token token) : base(token)
		{
		}

		/// <summary>
		/// 这是enter_try对应的holderror,到了这里可以释放了
		/// </summary>
		public StackLocater HoldError
		{
			get 
			{
				return dst;
			}
			set
			{ 
				dst = value;
			}
		}

		public override INS_Code INS_Code => INS_Code.finally_exit;

		public override int Size => 4;

		protected override void WriteByte(BinaryWriter bw)
		{
			
		}

		protected override void ReadFromBinary(BinaryReader br)
		{
			
		}

		public override string ToString()
		{
			return "FINALLY_EXIT";
		}

        public override List<StackLocater> GetDef()
        {
            return new List<StackLocater>();
        }

        public override List<StackLocater> GetUse()
        {
            return new List<StackLocater> { HoldError };
        }

        public override bool MaybeRaiseError()
        {
            return false;
        }

	}
}
