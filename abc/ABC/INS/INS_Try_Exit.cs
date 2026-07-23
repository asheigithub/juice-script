using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using juicescript.ABC.Locaters;

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
