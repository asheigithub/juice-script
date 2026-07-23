using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
    public sealed class INS_Ld_Namespace : Instruction
    {
        public override INS_Code INS_Code =>  INS_Code.ld_namespace;

        public override int Size
        {
            get
            {
                return 4 + 4;
            }
        }


       

        public int namespace_index;

        public INS_Ld_Namespace(Token token) : base(token)
        {
        }

        protected override void WriteByte(BinaryWriter bw)
        {
            
            bw.Write(namespace_index);
        }

		protected override void ReadFromBinary(BinaryReader br)
		{
			
            namespace_index = br.ReadInt32();
		}

        public override string ToString()
        {
            return $"Ld_Namespace   [{dst}] <- [namespace_index: {namespace_index}]";
        }

        public override IEnumerable<StackLocater> GetDef()
        {
            //return new List<StackLocater> { dst };
            yield return dst;
        }

        public override IEnumerable<StackLocater> GetUse()
        {
            //return new List<StackLocater>();
            yield break;
        }

        public override bool MaybeRaiseError()
        {
            return false;
        }

        public override void RemappingSlots(Dictionary<int, int> mapping)
        {
            if (mapping.TryGetValue(dst.index, out int newIndex))
                dst.index = newIndex;
        }

    }
}
