using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
    public sealed class INS_Store_HeapValueRef : Instruction
    {
        public override INS_Code INS_Code =>  INS_Code.storeHeapValueRef;

        public override int Size
        {
            get
            { 
                return 4 + 4;
            }
        }

        

        public StackLocater source;

        public INS_Store_HeapValueRef(Token token) : base(token)
        {
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
            return $"Store_HeapValueRef [{dst}] <- [{source}]";
        }

        public override IEnumerable<StackLocater> GetDef()
        {
            //return new List<StackLocater>();
            yield break;
        }

        public override IEnumerable<StackLocater> GetUse()
        {
            //return new List<StackLocater> { source, dst };
            yield return source;
            yield return dst;
        }

        public override bool MaybeRaiseError()
        {
            return true;
        }

        public override void RemappingSlots(Dictionary<int, int> mapping)
        {
            if (mapping.TryGetValue(source.index, out int newIndex))
                source.index = newIndex;
            if (mapping.TryGetValue(dst.index, out int newIndex1))
                dst.index = newIndex1;
        }

    }
}
