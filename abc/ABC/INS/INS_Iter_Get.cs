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

		public ScopeHeapLocater holdObj;
		
		public StackLocater iterator;

		//public StackLocater iter_context;



		public int flag_end_id;
		public int flag_offset;



		protected override void ReadFromBinary(BinaryReader br)
		{
			holdObj.ReadFromBinary(br);
			iterator.ReadFromBinary(br);
			//iter_context.ReadFromBinary(br);
			flag_end_id = br.ReadInt32();
			flag_offset = br.ReadInt32();
		}

		protected override void WriteByte(BinaryWriter bw)
		{
			holdObj.Write(bw);
			iterator.Write(bw);
			//iter_context.Write(bw);
			bw.Write(flag_end_id);
			bw.Write(flag_offset);
		}

		public override string ToString()
		{
			return $"ITER_Get {iterator}<-{holdObj}.[iterator]  if failed GOTO Flag_{flag_end_id} ";
		}

	}
}
