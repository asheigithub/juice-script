using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
	/// <summary>
	/// await return 指令
	/// dst: 要await的返回值的栈位置
	/// </summary>
	public sealed class INS_Await_Return : Instruction
	{
		public INS_Await_Return(Token token) : base(token)
		{
		}

		public override INS_Code INS_Code => INS_Code.await_return;

		public override int Size => 4;

		
		protected override void ReadFromBinary(BinaryReader br)
		{
			
		}

		protected override void WriteByte(BinaryWriter bw)
		{
			
		}

		public override string ToString()
		{
			return $"await [{dst}]";
		}

        public override IEnumerable<StackLocater> GetDef()
        {
            //return new List<StackLocater>();
			yield break;
        }

        public override IEnumerable<StackLocater> GetUse()
        {
			//return new List<StackLocater> { dst };
			yield return dst;
        }

        public override bool MaybeRaiseError()
        {
            return true;
        }

        public override void RemappingSlots(Dictionary<int, int> mapping)
        {
            if (mapping.TryGetValue(dst.index, out int newIndex))
                dst.index = newIndex;
        }
	}
}