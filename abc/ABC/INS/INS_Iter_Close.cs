using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
	/// <summary>
	/// dst字段承载 instance.
	/// </summary>
	public sealed class INS_Iter_Close : Instruction
	{
		public INS_Iter_Close(Token token) : base(token)
		{
		}

		public override INS_Code INS_Code => INS_Code.iter_close;

		public override int Size =>  4 + 4 + 4 + 4;


		public ScopeHeapLocater holdObj;
		public StackLocater iterator;
		/// <summary>
		/// 迭代器上下文存储位置（方法变量）
		/// </summary>
		public ScopeHeapLocater iterContextVar;

		protected override void ReadFromBinary(BinaryReader br)
		{
			holdObj.ReadFromBinary(br);
			iterator.ReadFromBinary(br);
			iterContextVar.ReadFromBinary(br);
		}

		protected override void WriteByte(BinaryWriter bw)
		{
			holdObj.Write(bw);
			iterator.Write(bw);
			iterContextVar.Write(bw);
		}

		public override string ToString()
		{
			return $"ITER_Close {holdObj}.{iterator} ctx:{iterContextVar}";
		}

	}
}
