using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using juicescript.ABC.Locaters;

namespace juicescript.ABC.INS
{
	/// <summary>
	/// dst无用
	/// </summary>
	public sealed class INS_Return_Void : Instruction
	{
		public INS_Return_Void(Token token) : base(token)
		{
		}

		public override INS_Code INS_Code => INS_Code.return_void;

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
			return "Return";
		}

        public override IEnumerable<StackLocater> GetDef()
        {
			//return new List<StackLocater>();
			yield break;
        }

        public override IEnumerable<StackLocater> GetUse()
        {
			//return new List<StackLocater>();
			yield break;
        }

        public override bool MaybeRaiseError()
        {
            return false;
        }

        public override void RemappingSlots(Dictionary<int, int> mapping)
        {
        }

	}
}
