using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
	/// <summary>
	/// FLAG 指令：使用 dst.index 承载 flag_id，但语义上不表示目标变量
	/// </summary>
	public sealed class INS_Flag : Instruction
	{
		public INS_Flag(Token token) : base(token)
		{
		}

		public int flag_id
		{
			get { return dst.index; }
			set { dst.index = value; }
		}

		public override INS_Code INS_Code =>  INS_Code.flag;

		public override int Size => 4;

		protected override void ReadFromBinary(BinaryReader br)
		{
			// nothing to read; dst.index already restored from head
		}

		protected override void WriteByte(BinaryWriter bw)
		{
			// nothing to write; dst.index already restored from head
		}

		public override string ToString()
		{
			if (flag_id == 0xffffff)
			{
				return $"virtual jump_to_end";
			}
			else
			{
				return $"FLAG_{flag_id}";
			}
		}

	}
}
