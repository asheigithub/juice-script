using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
    public sealed class INS_Ld_Method : Instruction
    {
        public INS_Ld_Method(Token token) : base(token)
        {
        }

        public override INS_Code INS_Code => INS_Code.ld_method;

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
            return $"Ld_Method   [{dst}] <- [instance:{instance}].vtable[{const_index}]";
        }

        public override List<StackLocater> GetDef()
        {
            return new List<StackLocater> { dst };
        }

        public override List<StackLocater> GetUse()
        {
            // 读取instance栈位置
            return new List<StackLocater> { instance };
        }

        public override bool MaybeRaiseError()
        {
            // 读取方法可能抛出异常（如访问null对象）
            return true;
        }

    }
}
