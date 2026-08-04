using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
	public sealed class INS_O_NewInstance_StoreVar : Instruction
	{
		public override INS_Code INS_Code => INS_Code.O_NewInstance_MethodVar;

		protected override void WriteByte(BinaryWriter bw)
		{
			heap.Write(bw);
			typeLocator.Write(bw);
			bw.Write(args.Length);
			for (int i = 0; i < args.Length; i++)
			{
				args[i].Write(bw);
			}

		}

		protected override void ReadFromBinary(BinaryReader br)
		{
			heap.ReadFromBinary(br);
			typeLocator.ReadFromBinary(br);
			args = new StackLocater[br.ReadInt32()];
			for (int i = 0; i < args.Length; i++)
			{
				args[i].ReadFromBinary(br);
			}
		}


		public override int Size
		{
			get
			{
				
				return 4 + 4 + 4 + 4 + 4 * args.Length;
			}
		}

		public ScopeHeapLocater heap;

		public StackLocater typeLocator;

		public StackLocater[] args;

		

		public INS_O_NewInstance_StoreVar(Token token) : base(token)
		{
		}

		public override string ToString()
		{
			return $"O_NewInstance_Var [offset:{heap.MemberIndex}] <-  class:[{typeLocator}]({string.Join(",", args)})->[{dst}]";
		}

		public override IEnumerable<StackLocater> GetDef()
		{
			//return new List<StackLocater> { dst };
			yield return dst;
		}

		public override IEnumerable<StackLocater> GetUse()
		{
			//var use = new List<StackLocater> { typeLocator };
			//use.AddRange(args);
			//return use;

			yield return typeLocator;
			foreach (var arg in args)
			{
				yield return arg;
			}

		}

		public override bool MaybeRaiseError()
		{
			return true;
		}

		public override void RemappingSlots(Dictionary<int, int> mapping)
		{
			if (mapping.TryGetValue(dst.index, out int newIndex))
				dst.index = newIndex;
			if (mapping.TryGetValue(typeLocator.index, out int newIndex1))
				typeLocator.index = newIndex1;
			for (int i = 0; i < args.Length; i++)
			{
				if (mapping.TryGetValue(args[i].index, out int newIdx))
					args[i].index = newIdx;
			}
		}
	}
}
