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
	/// yield return 指令
	/// dst: 要yield的返回值的栈位置
	/// </summary>
	public sealed class INS_Yield_Return : Instruction
	{
		public INS_Yield_Return(Token token) : base(token)
		{
		}

		public override INS_Code INS_Code => INS_Code.yield_return;

		public override int Size => 4;

		
		protected override void ReadFromBinary(BinaryReader br)
		{
			
		}

		protected override void WriteByte(BinaryWriter bw)
		{
			
		}

		public override string ToString()
		{
			return $"Yield_Return [{dst}]";
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
            // Player.cs中yield_return调用LoadValue和StoreReturnSlot可能抛出异常
            return true;
        }

        public override void RemappingSlots(Dictionary<int, int> mapping)
        {
            if (mapping.TryGetValue(dst.index, out int newIndex))
                dst.index = newIndex;
        }
	}
}