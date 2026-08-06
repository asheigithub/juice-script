using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
	public sealed class INS_O_StoreMethodVar_Instance : Instruction
	{
		public override INS_Code INS_Code => INS_Code.O_StoreMethodVar_Instance;

		public override int Size
		{
			get
			{
				return 4 + 4 + 4;
			}
		}

		public ScopeHeapLocater heap;

		public StackLocater convertedloc;



		public INS_O_StoreMethodVar_Instance(Token token) : base(token)
		{
		}

		protected override void WriteByte(BinaryWriter bw)
		{
			heap.Write(bw);
			convertedloc.Write(bw);

		}

		protected override void ReadFromBinary(BinaryReader br)
		{
			heap.ReadFromBinary(br);
			convertedloc.ReadFromBinary(br);
		}


		public override string ToString()
		{
			return $"O_Store_MethodVar_Instance   [offset:{heap.MemberIndex}] <-( ctype({dst} ->{convertedloc} ) , [{convertedloc}])";
		}

		public override IEnumerable<StackLocater> GetDef()
		{
			//return new List<StackLocater> { convertedloc  };
			yield return convertedloc;
		}

		public override IEnumerable<StackLocater> GetUse()
		{
			//return new List<StackLocater> { dst };
			yield return dst;
		}

		public override bool MaybeRaiseError()
		{
			// Player.cs中调用PrepareSaveMethodScope可能抛出异常
			return true;
		}

		public override void RemappingSlots(Dictionary<int, int> mapping)
		{
			if (mapping.TryGetValue(dst.index, out int newIndex))
				dst.index = newIndex;

			if (mapping.TryGetValue(convertedloc.index, out newIndex))
				convertedloc.index = newIndex;

		}

	}
}
