using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
	public sealed class INS_Incr_Decr : Instruction
	{
		public INS_Incr_Decr(Token token) : base(token)
		{
		}

		public override INS_Code INS_Code => INS_Code.increment_decrement;

		public override int Size => 4 + 4 + 4 + 4;

		public StackLocater source;
		public StackLocater result;
		public int addvalue;

		protected override void ReadFromBinary(BinaryReader br)
		{
			source.ReadFromBinary(br);
			result.ReadFromBinary(br);
			addvalue = br.ReadInt32();
		}

		protected override void WriteByte(BinaryWriter bw)
		{
			source.Write(bw);
			result.Write(bw);
			bw.Write(addvalue);
		}

		public override string ToString()
		{
			return $"Incr return [{ ( dst.index !=  result.index ? $"{source}->{result}" : $"{dst}" ) }], [{dst}] = [{source}] + ({addvalue})";
		}

        public override IEnumerable<StackLocater> GetDef()
        {
			if (dst.index != result.index)
			{
				yield return result;
				yield return dst;

				//return new List<StackLocater>() { result,dst };
			}
			else
			{
				//return new List<StackLocater>() { dst };
				yield return dst;
			}

			//return new List<StackLocater>() { };

		}

        public override IEnumerable<StackLocater> GetUse()
        {
			if (dst.index != result.index)
			{
				return new List<StackLocater>() { source };
			}
			else
			{
				return new List<StackLocater>() { source };
			}

			//return new List<StackLocater> { source ,result , dst};
		}

        public override bool MaybeRaiseError()
        {
            // Player.cs中increment_decrement调用ToPrimitive和Exec_Add可能抛出异常
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
        }


	}
}
