using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
	public sealed class INS_O_IncrDecr_StoreVar : Instruction
	{
		public INS_O_IncrDecr_StoreVar(Token token) : base(token)
		{
		}

		public override INS_Code INS_Code => INS_Code.O_IncrDecr_StoreVar;

		public override int Size
		{
			get
			{
				return 4 + 4 + 4 + 4 + 4 + 4;
			}
		}


		public StackLocater source;
		public StackLocater result;
		public int addvalue;

		public ScopeHeapLocater heap;
		public StackLocater convertedloc;


		public override IEnumerable<StackLocater> GetDef()
		{
			if (dst.index != result.index)
			{
				yield return result;
				yield return dst;

			}
			else
			{
				
				yield return dst;
			}
			yield return convertedloc;

		}

		public override IEnumerable<StackLocater> GetUse()
		{
			yield return source;
		}

		public override bool MaybeRaiseError()
		{
			return true;
		}

		public override void RemappingSlots(Dictionary<int, int> mapping)
		{
			if (mapping.TryGetValue(dst.index, out int newIndex))
				dst.index = newIndex;
			if (mapping.TryGetValue(source.index, out int newIndex1))
				source.index = newIndex1;
			if (mapping.TryGetValue(result.index, out int newIndex2))
				result.index = newIndex2;
			if (mapping.TryGetValue(convertedloc.index, out int newIndex3))
				convertedloc.index = newIndex3;


		}

		protected override void ReadFromBinary(BinaryReader br)
		{
			source.ReadFromBinary(br);
			result.ReadFromBinary(br);
			addvalue = br.ReadInt32();
			heap.ReadFromBinary(br);
			convertedloc.ReadFromBinary(br);
		}

		protected override void WriteByte(BinaryWriter bw)
		{
			source.Write(bw);
			result.Write(bw);
			bw.Write(addvalue);
			heap.Write(bw);
			convertedloc.Write(bw);

		}

		public override string ToString()
		{
			return $"O_Incr_StoreVar [offset:{heap.MemberIndex}] <-( ctype(([{(dst.index != result.index ? $"{source}->{result}" : $"{dst}")}], [{dst}] = [{source}] + ({addvalue})) ->{convertedloc} ) , [{convertedloc}]) ";
		}

	}
}
