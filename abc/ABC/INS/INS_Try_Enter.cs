using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
	/// <summary>
	/// dst承载hold_error.
	/// </summary>
	public sealed class INS_Try_Enter : Instruction
	{
		public override INS_Code INS_Code =>  INS_Code.try_enter;

		public int finally_pc; // finally块的位置
		public int finally_exit_pc; //finally块结束的位置
		//public StackLocater hold_error;//finally中暂存错误的位置

		public int[] catch_pc; //cache块的位置表

		public INS_Try_Enter(Token token) : base(token)
		{

		}

		public override int Size
		{
			get
			{
				return 
					 4 
					+ 4 + 4
					+ 4  + 4 * catch_pc.Length;
			}
		}

		protected override void WriteByte(BinaryWriter bw)
		{
			bw.Write(finally_pc);
			bw.Write(finally_exit_pc);

			//hold_error.Write(bw);
			bw.Write(catch_pc.Length);
			for (int i = 0; i < catch_pc.Length; i++)
			{
				bw.Write(catch_pc[i]);
			}
		}

		protected override void ReadFromBinary(BinaryReader br)
		{
			finally_pc = br.ReadInt32();
			finally_exit_pc = br.ReadInt32();
			//hold_error.ReadFromBinary(br);
			catch_pc = new int[br.ReadInt32()];
			for (int i = 0; i < catch_pc.Length; i++)
			{
				catch_pc[i]= br.ReadInt32();
			}
		}


		public override string ToString()
		{
			return $"TRY_ENTER hold{dst}";
		}

        public override IEnumerable<StackLocater> GetDef()
        {
			//用于保存try结构内抛出的异常。当进入finally内，需要暂存在这里。
			//return new List<StackLocater> { dst };
			yield return dst;
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
            if (mapping.TryGetValue(dst.index, out int newIndex))
                dst.index = newIndex;
        }

	}
}
