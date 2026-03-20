using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
	public sealed class INS_readPoperty_Interface : Instruction
	{
		public INS_readPoperty_Interface(Token token) : base(token)
		{
		}

		public override INS_Code INS_Code => INS_Code.read_property_interface;

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
			instance.Write(bw);
			bw.Write(class_id);
			bw.Write(const_index);
		}

		protected override void ReadFromBinary(BinaryReader br)
		{
			instance.ReadFromBinary(br);
			class_id = br.ReadInt32();
			const_index = br.ReadUInt32();
		}

		public override string ToString()
		{
			return $"Read_Property_Interface   [{dst}] <- (interface:{class_id})[instance:{instance}].vtable_getter[{const_index}].read()";
		}

        public override List<StackLocater> GetDef()
        {
            return new List<StackLocater> { dst };
        }

        public override List<StackLocater> GetUse()
        {
            return new List<StackLocater> { instance };
        }

        public override bool MaybeRaiseError()
        {
            return true;
        }

        public override void RemappingSlots(Dictionary<int, int> mapping)
        {
            if (mapping.TryGetValue(dst.index, out int newIndex))
                dst.index = newIndex;
            if (mapping.TryGetValue(instance.index, out int newIndex1))
                instance.index = newIndex1;
        }

	}
}
