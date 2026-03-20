using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using juicescript.ABC.Locaters;

namespace juicescript.ABC.INS
{
    public sealed class INS_Ld_True : Instruction
    {
        public override INS_Code INS_Code => INS_Code.ld_true;

       
        public INS_Ld_True(Token token) : base(token)
        {
        }

        public override int Size
        {
            get
            {
                return 4;
            }
        }

        protected override void WriteByte(BinaryWriter bw)
        {
           
        }

		protected override void ReadFromBinary(BinaryReader br)
		{
			
		}

        public override string ToString()
        {
            return $"Ld_True   [{dst}] <- true";
        }

        public override List<StackLocater> GetDef()
        {
            // 加载true常量到目标栈位置
            return new List<StackLocater> { dst };
        }

        public override List<StackLocater> GetUse()
        {
            // 不读取任何栈位置
            return new List<StackLocater>();
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
