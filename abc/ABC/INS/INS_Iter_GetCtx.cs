using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
	public sealed class INS_Iter_GetCtx : Instruction
	{
		public INS_Iter_GetCtx(Token token) : base(token)
		{
		}

		public override INS_Code INS_Code =>  INS_Code.iter_initctx;

		public override int Size => 4;

		/// <summary>
		/// 迭代器上下文存储位置（方法变量）
		/// 复用 dst.index 的存储空间：
		/// - 高16位：ScopeIndex
		/// - 低16位：MemberIndex
		/// </summary>
		public ScopeHeapLocater iterContextVar
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
			// iterContextVar 已经从 dst.index 中读取，不需要额外读取
		}

		protected override void WriteByte(BinaryWriter bw)
		{
			// iterContextVar 已经编码到 dst.index 中，不需要额外写入
		}

		public override string ToString()
		{
			return $"ITER_GetContext -> {iterContextVar}";
		}

	}
}
