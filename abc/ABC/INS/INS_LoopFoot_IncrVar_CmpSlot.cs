using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Text;
using static juicescript.ABC.INS.INS_If_LogicOp_Goto;

namespace juicescript.ABC.INS
{
	public sealed class INS_LoopFoot_IncrVar_CmpSlot : Instruction
	{
		public INS_LoopFoot_IncrVar_CmpSlot(Token token) : base(token)
		{
		}

		public override int Size => 4 + 4 + 4 + 4 + 4 + 4 ;

		public override INS_Code INS_Code => INS_Code.LoopFoot_IncrVar_CmpSlot;

		public int flag_id;
		public int offset;

		//public StackLocater source;
		//public StackLocater result;


		public int src_index;
		public int addvalue;

		public ScopeHeapLocater heap;
		//public StackLocater convertedloc;

		public StackLocater compareto;
		public INS_If_LogicOp_Goto.CompMode compmode;


		protected override void ReadFromBinary(BinaryReader br)
		{
			flag_id = br.ReadInt32();
			offset = br.ReadInt32();

			//source.ReadFromBinary(br);
			//result.ReadFromBinary(br);
			uint addvalue_andresulttemp = br.ReadUInt32();
			addvalue = (sbyte)(addvalue_andresulttemp >> 24);
			src_index = (int)(addvalue_andresulttemp & 0xffffff);

			heap.ReadFromBinary(br);
			//convertedloc.ReadFromBinary(br);

			uint store = br.ReadUInt32();

			compmode = (INS_If_LogicOp_Goto.CompMode)(store >> 24);
			compareto.index =(int)( store & 0xffffff);

		}
		protected override void WriteByte(BinaryWriter bw)
		{
			bw.Write(flag_id);
			bw.Write(offset);

			//source.Write(bw);
			//result.Write(bw);
			//bw.Write(addvalue);
			uint addvalue_andresulttemp = (uint)src_index | ((uint)addvalue << 24);
			bw.Write(addvalue_andresulttemp);

			heap.Write(bw);
			//convertedloc.Write(bw);

			uint store = (uint)compareto.index | ((uint)compmode << 24);
			bw.Write(store);
		}

		public override IEnumerable<StackLocater> GetDef()
		{
			//if (dst.index != result.index)
			//{
			//	yield return result;
			//	yield return dst;

			//}
			//else
			{

				yield return dst;
			}
			//yield return convertedloc;

		}

		public override IEnumerable<StackLocater> GetUse()
		{
			
			yield return compareto;
			yield return new StackLocater() { index = src_index };
		}

		public override bool MaybeRaiseError()
		{
			return true;
		}

		public override void RemappingSlots(Dictionary<int, int> mapping)
		{
			if (mapping.TryGetValue(dst.index, out int newIndex))
				dst.index = newIndex;
			//if (mapping.TryGetValue(source.index, out int newIndex1))
			//	source.index = newIndex1;
			//if (mapping.TryGetValue(result.index, out int newIndex2))
			//	result.index = newIndex2;
			//if (mapping.TryGetValue(convertedloc.index, out int newIndex3))
			//	convertedloc.index = newIndex3;

			if (mapping.TryGetValue(src_index, out int newIndex5))
				src_index = newIndex5;

			if (mapping.TryGetValue(compareto.index, out int newIndex4))
				compareto.index = newIndex4;

		}
		private string GetCompModeString()
		{
			switch (compmode)
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

		public override string ToString()
		{
			return $"LoopFoot [offset:{heap.MemberIndex}]<-([{src_index}] + {addvalue}), convertto[{dst}] , if( [{dst}] {GetCompModeString()} {compareto} ) goto FLAG_{flag_id}  ";
		}
	}
}
