using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Text;

namespace juicescript.ABC.INS
{
	
	public sealed class INS_O_Var_Self_Add : Instruction
	{
		public INS_O_Var_Self_Add(Token token) : base(token)
		{
		}

		public override INS_Code INS_Code => INS_Code.O_Var_Self_Add;
		public override int Size
		{
			get
			{
				return 4 + 4 + 4;
			}
		}

		public ScopeHeapLocater heap;
		public StackLocater addvalue;

		

		public override IEnumerable<StackLocater> GetDef()
		{
			yield return dst;
		}

		public override IEnumerable<StackLocater> GetUse()
		{
			yield return addvalue;
		}

		public override bool MaybeRaiseError()
		{
			return true;
		}

		public override void RemappingSlots(Dictionary<int, int> mapping)
		{
			if (mapping.TryGetValue(dst.index, out int newIndex))
				dst.index = newIndex;
			if (mapping.TryGetValue(addvalue.index, out int newIndex1))
				addvalue.index = newIndex1;
			
		}

		protected override void ReadFromBinary(BinaryReader br)
		{
			heap.ReadFromBinary(br);
			addvalue.ReadFromBinary(br);
			
		}

		protected override void WriteByte(BinaryWriter bw)
		{
			heap.Write(bw);
			addvalue.Write(bw);
			
		}

		public override string ToString()
		{
			return $"O_Var_Self_Add [offset:{heap.MemberIndex}] = [offset:{heap.MemberIndex} + {addvalue} ] , [convertloc:{dst}]) ";
		}

	}
}
