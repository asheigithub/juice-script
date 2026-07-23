using juicescript.ABC.Locaters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
    public sealed class INS_Ld_ValueRef : Instruction
    {
        public INS_Ld_ValueRef(Token token) : base(token)
        {
        }

        public override INS_Code INS_Code => INS_Code.ld_ValueRef;

		
		public StackLocater source;
        
        public override int Size
        {
            get
            {
                return  4 + 4;
            }
        }

        protected override void WriteByte(BinaryWriter bw)
        {
            source.Write(bw);
            
        }

		protected override void ReadFromBinary(BinaryReader br)
		{
			source.ReadFromBinary(br);
            
		}

        public override string ToString()
        {
            return $"Ld_ValueRef   [{dst}]<-[{source}]";
        }

        public override IEnumerable<StackLocater> GetDef()
        {
            //return new List<StackLocater> { dst };
            yield return dst;
        }

        public override IEnumerable<StackLocater> GetUse()
        {
            //return new List<StackLocater> { source };
            yield return source;
        }

        public override bool MaybeRaiseError()
        {
            return true;
        }

        public override void RemappingSlots(Dictionary<int, int> mapping)
        {
            if (mapping.TryGetValue(dst.index, out int newIndex))
                dst.index = newIndex;
            if (mapping.TryGetValue(source.index, out int newIndex1))
                source.index = newIndex1;
        }

    }
}
