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
	/// resume_point: 恢复执行的PC位置标志ID
	/// data_offset: 生成器状态数据偏移量（用于优化内存）
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
	}
}