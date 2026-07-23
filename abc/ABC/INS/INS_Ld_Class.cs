using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using juicescript.ABC.Locaters;

namespace juicescript.ABC.INS
{
    public sealed class INS_Ld_Class : Instruction
    {
        public INS_Ld_Class(Token token):base(token) { }

        public override INS_Code INS_Code =>  INS_Code.ld_class;

        public override int Size
        {
            get 
            {
                return 4 + 4;
            }
        }

       

        public int classid_index;

        protected override void WriteByte(BinaryWriter bw)
        {
           
            bw.Write(classid_index);
        }

		protected override void ReadFromBinary(BinaryReader br)
		{
		    
            classid_index = br.ReadInt32();
		}


        public override string ToString()
        {
            return $"Ld_Class   [{dst}] <- [classid_index: {classid_index}]";
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
            return true;
        }

        public override void RemappingSlots(Dictionary<int, int> mapping)
        {
            if (mapping.TryGetValue(dst.index, out int newIndex))
                dst.index = newIndex;
        }

    }
}
