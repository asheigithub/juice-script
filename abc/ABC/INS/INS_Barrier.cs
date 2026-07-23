using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
	public sealed class INS_Barrier : Instruction
	{
		public INS_Barrier(Token token) : base(token)
		{
		}

		public StackLocater[] uselist;

		public override INS_Code INS_Code =>  INS_Code.expression_barrier;

		public override int Size => 4 + 4 + 4 * uselist.Length;


		protected override void ReadFromBinary(BinaryReader br)
		{
			uselist = new StackLocater[br.ReadInt32()];
			for (int i = 0; i < uselist.Length; i++)
			{
				uselist[i].ReadFromBinary(br);
			}
		}

		protected override void WriteByte(BinaryWriter bw)
		{
			bw.Write(uselist.Length);
			for (int i = 0; i <	uselist.Length; i++)
			{
				uselist[i].Write(bw);
			}
		}



		public override IEnumerable<StackLocater> GetDef()
		{
			//return new List<StackLocater>();
			yield break;
		}

		public override IEnumerable<StackLocater> GetUse()
		{
			//return new List<StackLocater>(uselist);

			return uselist;

		}

		public override bool MaybeRaiseError()
		{
			return false;
		}

		public override void RemappingSlots(Dictionary<int, int> mapping)
		{
			for (int i = 0; i < uselist.Length; i++)
			{
				if (mapping.TryGetValue(uselist[i].index, out int newIdx))
					uselist[i].index = newIdx;
			}
		}

		public override string ToString()
		{
			return $"INS_Barrier [{string.Join(",", uselist)}]";
		}
	}
}
