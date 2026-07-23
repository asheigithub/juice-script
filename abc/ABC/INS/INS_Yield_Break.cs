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
	/// yield break 指令
	/// 用于提前终止生成器执行
	/// dst无用（遵循指令格式要求）
	/// </summary>
	public sealed class INS_Yield_Break : Instruction
	{
		public INS_Yield_Break(Token token) : base(token)
		{
		}

		public override INS_Code INS_Code => INS_Code.yield_break;

		public override int Size
		{
			get
			{
				return 4; // 简单指令，只有基础4字节
			}
		}

		protected override void WriteByte(BinaryWriter bw)
		{
			// 无额外数据需要写入
		}

		protected override void ReadFromBinary(BinaryReader br)
		{
			// 无额外数据需要读取
		}

		public override string ToString()
		{
			return "Yield_Break";
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