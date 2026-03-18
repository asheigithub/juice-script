using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
	public sealed class INS_Is : Instruction
	{
		public INS_Is(Token token) : base(token)
		{
		}

		public override INS_Code INS_Code => INS_Code.get_is;

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
			return $"Is   [{dst}]<- [{v1}] is [{v2}]";
		}

        public override List<StackLocater> GetDef()
        {
            return new List<StackLocater> { dst };
        }

        public override List<StackLocater> GetUse()
        {
            return new List<StackLocater> { v1, v2 };
        }

        public override bool MaybeRaiseError()
        {
            // Player.cs中get_is会调用RaiseTypeError，可能抛出异常
            return true;
        }
	}
}
