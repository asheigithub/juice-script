using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
    public sealed class INS_Delete : Instruction
    {
        public override INS_Code INS_Code => INS_Code.delete;

       

        public StackLocater todelete;

        public INS_Delete(Token token) : base(token)
        {
        }

        public override int Size
        {
            get
            {
                return 4 + 4 ;
            }
        }

        protected override void WriteByte(BinaryWriter bw)
        {
           
            todelete.Write(bw);
        }

		protected override void ReadFromBinary(BinaryReader br)
		{
			
            todelete.ReadFromBinary(br);
		}

        public override string ToString()
        {
            return $"Delete(/)   [{dst}]<- delete [{todelete}]";
        }

        public override IEnumerable<StackLocater> GetDef()
        {
            //return new List<StackLocater> { dst };
            yield return dst;
        }

        public override IEnumerable<StackLocater> GetUse()
        {
            //return new List<StackLocater> { todelete };
            yield return todelete;
        }

        public override bool MaybeRaiseError()
        {
            return true;
        }

        public override void RemappingSlots(Dictionary<int, int> mapping)
        {
            if (mapping.TryGetValue(dst.index, out int newIndex))
                dst.index = newIndex;
            if (mapping.TryGetValue(todelete.index, out int newIndex1))
                todelete.index = newIndex1;
        }

    }
}
