using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using juicescript.ABC.Locaters;

namespace juicescript.ABC.INS
{
    public sealed class INS_Ld_Const : Instruction
    {
        public override INS_Code INS_Code =>  INS_Code.ld_const;

        public override int Size
        {
            get 
            { 
               
                return 4 + 4;
            }
        }

       

        public int const_index;

        public INS_Ld_Const(Token token) : base(token)
        {
        }

        protected override void WriteByte(BinaryWriter bw)
        {
          
            bw.Write(const_index);

        }

		protected override void ReadFromBinary(BinaryReader br)
		{
			
            const_index = br.ReadInt32();
		}

        public override string ToString()
        {
            return $"Ld_Const   [{dst}] <- [const id: {const_index}]";
        }

        public override List<StackLocater> GetDef()
        {
            // 加载常量指令会将常量值写入目标栈位置
            return new List<StackLocater> { dst };
        }

        public override List<StackLocater> GetUse()
        {
            // 加载常量指令不读取任何栈位置
            return new List<StackLocater>();
        }

        public override bool MaybeRaiseError()
        {
            // 加载常量不会引发异常
            return false;
        }

    }
}
