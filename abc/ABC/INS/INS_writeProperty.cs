using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
    /// <summary>
    /// dst字段承载value
    /// </summary>
    public sealed class INS_writeProperty : Instruction
    {
        public INS_writeProperty(Token token) : base(token)
        {
        }

        public override INS_Code INS_Code => INS_Code.write_property;

        public override int Size
        {
            get
            {
                return 4 + 4 + 4;
            }
        }


       
        public StackLocater instance;
        public uint const_index;


        protected override void WriteByte(BinaryWriter bw)
        {
           
            instance.Write(bw);
            bw.Write(const_index);
        }

		protected override void ReadFromBinary(BinaryReader br)
		{
			
            instance.ReadFromBinary(br);
            const_index = br.ReadUInt32();
		}

        public override string ToString()
        {
            return $"Write_Property   [[instance:{instance}].vtable_setter[{const_index}] <-[{dst}]";
        }

        public override IEnumerable<StackLocater> GetDef()
        {
            //return new List<StackLocater>();
            yield break;
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
