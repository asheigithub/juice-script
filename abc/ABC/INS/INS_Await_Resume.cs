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
	public sealed class INS_Await_Resume : Instruction
	{
		public INS_Await_Resume(Token token) : base(token)
		{
		}

		public override INS_Code INS_Code => INS_Code.await_resume;

		public override int Size => 4;

		
		protected override void ReadFromBinary(BinaryReader br)
		{
			
		}

		protected override void WriteByte(BinaryWriter bw)
		{
			
		}

		public override string ToString()
		{
			return $"await remuse[{dst}]";
		}

        public override List<StackLocater> GetDef()
        {
            return new List<StackLocater> { dst };
        }

        public override List<StackLocater> GetUse()
        {
            return new List<StackLocater>();
        }

        public override bool MaybeRaiseError()
        {
            // Player.cs中await_resume检测到rejected状态时会设置error并跳转
            return true;
        }
	}
}