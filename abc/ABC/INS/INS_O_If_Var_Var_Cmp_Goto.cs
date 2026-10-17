using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Text;
using static juicescript.ABC.INS.INS_If_LogicOp_Goto;


namespace juicescript.ABC.INS
{
	public sealed class INS_O_If_Var_Var_Cmp_Goto : Instruction
	{
		public INS_O_If_Var_Var_Cmp_Goto(Token token) : base(token)
		{

		}

		public override INS_Code INS_Code => INS_Code.O_If_Var_Var_Cmp_Goto;

		public override int Size => 4 + 4 
			+ 4  //打包compMode和jumpMode
			+ 2+2 
			+ 4 
			+ 4 
			;

		public int flag_id
		{
			get { return dst.index; }
			set { dst.index = value; }
		}

		public int offset;

		public INS_If_LogicOp_Goto.CompMode compMode;
		public bool jump_mode;

		public ushort var1;
		public ushort var2 ;

		public int var1_stackindex;
		public int var2_stackindex;

		protected override void ReadFromBinary(BinaryReader br)
		{
			offset = br.ReadInt32();
			compMode = (CompMode)br.ReadInt16();
			jump_mode = br.ReadInt16() == 1;

			var1 = br.ReadUInt16();
			var2 = br.ReadUInt16();
			var1_stackindex = br.ReadInt32();
			var2_stackindex = br.ReadInt32();

		}

		protected override void WriteByte(BinaryWriter bw)
		{
			bw.Write(offset);
			bw.Write((short)compMode);
			bw.Write( jump_mode? (short)1:(short)0 );
			bw.Write(var1);
			bw.Write(var2);
			bw.Write(var1_stackindex);
			bw.Write(var2_stackindex);

		}

		public override void RemappingSlots(Dictionary<int, int> mapping)
		{
			if (mapping.TryGetValue(var1_stackindex, out int newIndex))
				var1_stackindex = newIndex;
			if (mapping.TryGetValue(var2_stackindex, out int newIndex1))
				var2_stackindex = newIndex1;
		}


		private string GetCompModeString()
		{
			switch (compMode)
			{
				case CompMode.strict_equal:
					return "===";
				case CompMode.strict_neq:
					return "!==";
				case CompMode.equal:
					return "==";
				case CompMode.notequal:
					return "!=";
				case CompMode.less:
					return "<";
				case CompMode.greater:
					return ">";
				case CompMode.less_equal:
					return "<=";
				case CompMode.greater_equal:
					return ">=";
				default:
					return "!!Known";
			}
		}

		public override bool MaybeRaiseError()
		{
			return compMode != CompMode.strict_equal && compMode != CompMode.strict_neq;
		}


		public override IEnumerable<StackLocater> GetUse()
		{
			yield break;
		}

		public override IEnumerable<StackLocater> GetDef()
		{
			yield return new StackLocater() { index = var1_stackindex };
			yield return new StackLocater() { index = var2_stackindex };
		}

		public override string ToString()
		{
			return $"O_If_Var_Var_Cmp_Goto	( if( [offset:{var1}] {GetCompModeString()} [offset:{var2}] == {jump_mode}, Ld_Var(offset:{var1})->[stack:{var1_stackindex}] ,Ld_Var(offset:{var2})->[stack:{var2_stackindex}]  ) goto  FLAG_{flag_id})";
		}
	}
}
