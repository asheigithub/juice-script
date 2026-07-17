using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
	public sealed class INS_O_Ld_Function_BindGLobal : Instruction
	{
		public INS_O_Ld_Function_BindGLobal(Token token) : base(token)
		{
		}

		public override INS_Code INS_Code => INS_Code.O_ld_function_bindGlobal;

		public override int Size
		{
			get
			{
				return 4 + 4 + 4;
			}
		}


		public ScopeHeapLocater heapLocater;
		public int const_index;


		protected override void WriteByte(BinaryWriter bw)
		{

			heapLocater.Write(bw);
			bw.Write(const_index);
		}

		protected override void ReadFromBinary(BinaryReader br)
		{

			heapLocater.ReadFromBinary(br);
			const_index = br.ReadInt32();
		}


		public override string ToString()
		{
			return $"O_Ld_Function_bindGlobal   [{dst}] <- [function: {const_index} at {heapLocater}]";
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
			return true;
		}

		public override void RemappingSlots(Dictionary<int, int> mapping)
		{
			if (mapping.TryGetValue(dst.index, out int newIndex))
				dst.index = newIndex;
		}

	}
}
