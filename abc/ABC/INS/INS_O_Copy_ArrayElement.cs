using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace juicescript.ABC.INS
{
	public sealed class INS_O_Copy_ArrayElement : Instruction
	{
		public INS_O_Copy_ArrayElement(Token token) : base(token)
		{
		}

		public override INS_Code INS_Code => INS_Code.O_Copy_ArrayElement;

		public override int Size
		{
			get
			{
				return 4 + 4 + 4 + 4 + 4 ;
			}
		}


		public StackLocater src_arr;
		public StackLocater dst_arr;
		public StackLocater src_index;
		//public StackLocater dst_index;
		public StackLocater tmp_holder;

		public ref StackLocater dst_index
		{
			get
			{
				return ref dst;
			}
		}


		protected override void WriteByte(BinaryWriter bw)
		{
			src_arr.Write(bw);
			dst_arr.Write(bw);
			src_index.Write(bw);
			//dst_index.Write(bw);		
			tmp_holder.Write(bw);
		}

		protected override void ReadFromBinary(BinaryReader br)
		{

			src_arr.ReadFromBinary(br);
			dst_arr.ReadFromBinary(br);
			src_index.ReadFromBinary(br);
			//dst_index.ReadFromBinary(br);
			tmp_holder.ReadFromBinary(br);
		}



		public override IEnumerable<StackLocater> GetDef()
		{
			//return new List<StackLocater> { dst};
			yield break;
		}

		public override IEnumerable<StackLocater> GetUse()
		{
			//return new List<StackLocater> { instance, name, refholder };
			yield return src_arr;
			yield return dst_arr;
			yield return src_index;
			yield return dst_index;
			yield return tmp_holder;
		}

		public override bool MaybeRaiseError()
		{
			return true;
		}

		public override void RemappingSlots(Dictionary<int, int> mapping)
		{
			if (mapping.TryGetValue(dst.index, out int newIndex))
				dst.index = newIndex;

			if (mapping.TryGetValue(src_arr.index, out int newIndex1))
				src_arr.index = newIndex1;
			if (mapping.TryGetValue(src_index.index, out int newIndex2))
				src_index.index = newIndex2;

			
			if (mapping.TryGetValue(tmp_holder.index, out int newIndex3))
				tmp_holder.index = newIndex3;


			if (mapping.TryGetValue(dst_arr.index, out int newIndex4))
				dst_arr.index = newIndex4;
			//if (mapping.TryGetValue(dst_index.index, out int newIndex5))
			//	dst_index.index = newIndex5;



		}








		public override string ToString()
		{
			return $"Copy_Array_Element ({dst_arr})[{dst_index}] = ({src_arr})[{src_index}]  ";
		}




	}
}
