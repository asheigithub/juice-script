using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
	/// <summary>
	/// dst中实际保存的是iterSrcObjSaveAtHeap
	/// </summary>
	public sealed class INS_Iter_Next : Instruction
	{
		public INS_Iter_Next(Token token) : base(token)
		{
		}

		public override INS_Code INS_Code => INS_Code.iter_next;

		public override int Size => 4 + 4 + 4 + 4 + 4 + 4;

		public int mode;

		
		public ScopeHeapLocater iterator;
		public StackLocater result;

		
		public int flag_next_end_id;
		public int flag_offset;

		public ScopeHeapLocater iterSrcObjSaveInVar
		{
			get
			{
				return new ScopeHeapLocater
				{
					ScopeIndex = (ushort)(dst.index >> 16),
					MemberIndex = (ushort)(dst.index & 0xFFFF)
				};
			}
			set
			{
				dst.index = (value.ScopeIndex << 16) | value.MemberIndex;
			}
		}

		protected override void ReadFromBinary(BinaryReader br)
		{
			mode = br.ReadInt32();
			
			iterator.ReadFromBinary(br);
			result.ReadFromBinary(br);
			
			flag_next_end_id = br.ReadInt32();
			flag_offset = br.ReadInt32();
		}

		protected override void WriteByte(BinaryWriter bw)
		{
			bw.Write(mode);
			
			iterator.Write(bw);
			result.Write(bw);
			
			bw.Write(flag_next_end_id);
			bw.Write(flag_offset);
		}

		public override string ToString()
		{
			return $"ITER_Next { iterSrcObjSaveInVar }.{iterator}.next( in {result} mode:{(mode == 0 ? "key" : "value")})  if {result}->false GOTO Flag_{flag_next_end_id} ";
		}

        public override List<StackLocater> GetDef()
        {
            return new List<StackLocater> { result };
        }

        public override List<StackLocater> GetUse()
        {
            return new List<StackLocater>() { result };
        }

        public override bool MaybeRaiseError()
        {
            return true;
        }

        public override void RemappingSlots(Dictionary<int, int> mapping)
        {
            if (mapping.TryGetValue(result.index, out int newIndex))
                result.index = newIndex;
        }

	}
}
