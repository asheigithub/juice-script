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


	}
}
