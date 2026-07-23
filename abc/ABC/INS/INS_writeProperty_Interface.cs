using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
	/// <summary>
	/// dst 字段承载value
	/// </summary>
	public sealed class INS_writeProperty_Interface : Instruction
	{
		public INS_writeProperty_Interface(Token token) : base(token)
		{
		}

		public override INS_Code INS_Code => INS_Code.write_property_interface;

		public override int Size
		{
			get
			{
				return 4 + 4 + 4 + 4;
			}
		}

		
		public StackLocater instance;
		public int class_id;
		public uint const_index;


		protected override void WriteByte(BinaryWriter bw)
		{
			//value.Write(bw);
			instance.Write(bw);
			bw.Write(class_id);
			bw.Write(const_index);
		}

		protected override void ReadFromBinary(BinaryReader br)
		{
			//value.ReadFromBinary(br);
			instance.ReadFromBinary(br);
			class_id = br.ReadInt32();
			const_index = br.ReadUInt32();
		}


		public override string ToString()
		{
			return $"Write_Property_interface   [(interface:{class_id})[instance:{instance}].vtable_setter[{const_index}] <-[{dst}]";
		}

        public override IEnumerable<StackLocater> GetDef()
        {
            return new List<StackLocater>();
        }

        public override IEnumerable<StackLocater> GetUse()
        {
			//return new List<StackLocater> { instance, dst };
			yield return instance;
			yield return dst;
        }

        public override bool MaybeRaiseError()
        {
            return true;
        }

        public override void RemappingSlots(Dictionary<int, int> mapping)
        {
            if (mapping.TryGetValue(instance.index, out int newIndex))
                instance.index = newIndex;
            if (mapping.TryGetValue(dst.index, out int newIndex1))
                dst.index = newIndex1;
        }

	}
}
