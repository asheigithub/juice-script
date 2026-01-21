using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
	/// <summary>
	/// dst字段承载 instance
	/// </summary>
	public sealed class INS_Iter_Get : Instruction
	{
		public INS_Iter_Get(Token token) : base(token)
		{
		}

		public override INS_Code INS_Code => INS_Code.iter_get;

		public override int Size => 4 + 4 + 4 +4 + 4;

		public ScopeHeapLocater iterSrcObj_HoldInHeap;
		
		public StackLocater iterSrcobj;

		

		public int flag_end_id;
		public int flag_offset;

		/// <summary>
		/// 迭代器存储位置（方法变量）
		/// 复用 dst.index 的存储空间：
		/// - 高16位：ScopeIndex
		/// - 低16位：MemberIndex
		/// </summary>
		public ScopeHeapLocater iterVar
		{
			get
			{
				return new ScopeHeapLocater
				{
					ScopeIndex = (ushort)(dst.index >> 16),
					MemberIndex = (ushort)(dst.index & 0xFFFF)
				};
			}
			set
			{
				dst.index = (value.ScopeIndex << 16) | value.MemberIndex;
			}
		}

		protected override void ReadFromBinary(BinaryReader br)
		{
			iterSrcObj_HoldInHeap.ReadFromBinary(br);
			iterSrcobj.ReadFromBinary(br);
			//iter_context.ReadFromBinary(br);
			flag_end_id = br.ReadInt32();
			flag_offset = br.ReadInt32();
		}

		protected override void WriteByte(BinaryWriter bw)
		{
			iterSrcObj_HoldInHeap.Write(bw);
			iterSrcobj.Write(bw);
			//iter_context.Write(bw);
			bw.Write(flag_end_id);
			bw.Write(flag_offset);
		}

		public override string ToString()
		{
			return $"ITER_Get {iterVar}<-{iterSrcObj_HoldInHeap}.[iterator]  if failed GOTO Flag_{flag_end_id} ";
		}

	}
}
