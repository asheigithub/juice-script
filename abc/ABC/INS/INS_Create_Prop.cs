using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
	public sealed class INS_Create_Prop : Instruction
	{
		public INS_Create_Prop(Token token) : base(token)
		{
		}

		public override INS_Code INS_Code => INS_Code.create_prop;

		public override int Size => 4 + 4 + 4;


		
		public StackLocater key;

		public StackLocater value;


		protected override void ReadFromBinary(BinaryReader br)
		{
			
			key.ReadFromBinary(br);
			value.ReadFromBinary(br);
		}

		protected override void WriteByte(BinaryWriter bw)
		{
			
			key.Write(bw);
			value.Write(bw);
		}

		public override string ToString()
		{
			return $"Create_Prop  {dst}.{key} = {value}"; 
		}

        public override List<StackLocater> GetDef()
        {
            return new List<StackLocater>();
        }

        public override List<StackLocater> GetUse()
        {
            return new List<StackLocater> { dst , key, value };
        }

        public override bool MaybeRaiseError()
        {
            return true;
        }

        public override void RemappingSlots(Dictionary<int, int> mapping)
        {
            if (mapping.TryGetValue(dst.index, out int newIndex))
                dst.index = newIndex;
            if (mapping.TryGetValue(key.index, out int newIndex1))
                key.index = newIndex1;
            if (mapping.TryGetValue(value.index, out int newIndex2))
                value.index = newIndex2;
        }

	}
}
