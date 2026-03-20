using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
    /// <summary>
    /// IL中间代码
    /// 便于编译时优化
    /// 最后再生成纯二进制字节
    /// 运行时直接执行二进制字节
    /// 
    /// 改动：改为4字节对齐。INS_Code和target合用第一个int。
    /// code = data & 0xff, dst.index = data >> 24. dst.index肯定不是负数
    /// 
    /// 
    /// </summary>
    public abstract class Instruction
    {
        public abstract INS_Code INS_Code { get;  }

        public StackLocater dst;

        /// <summary>
        /// 本条指令占用几个字节
        /// </summary>
        public abstract int Size { get; }

        public Token token;

        public Instruction(Token token)
        { 
            this.token = token;
        }

        /// <summary>
        /// 返回本指令对StackSlot槽的赋值目标(如果有)
        /// </summary>
        /// <returns></returns>
        public abstract List<StackLocater> GetDef();

        /// <summary>
        /// 返回本指令使用了栈上的哪些槽。
        /// </summary>
        /// <returns></returns>
        public abstract List<StackLocater> GetUse();

        /// <summary>
        /// 返回本指令是否有引发异常的可能。
        /// </summary>
        /// <returns></returns>
        public abstract bool MaybeRaiseError();

        /// <summary>
        /// 根据传入的映射表，重新分配槽编号
        /// </summary>
        /// <param name="mapping"></param>
        public abstract void RemappingSlots(Dictionary<int,int> mapping);


        protected abstract void WriteByte(BinaryWriter bw);
        
        protected abstract void ReadFromBinary(BinaryReader br);

        public void Write(System.IO.BinaryWriter bw)
        {

            if (Size % 4 != 0)
            {
                throw new InvalidOperationException();
            }

#if DEBUG
            if (dst.index < 0 || dst.index > 0xffffff)
            {
                throw new InvalidOperationException();
            }

#endif
            long p1 = bw.BaseStream.Position;

            uint head =(uint)dst.index << 8 | (byte)INS_Code;
            bw.Write(head);

            //bw.Write((byte)INS_Code);
            WriteByte(bw);

#if DEBUG
            if (bw.BaseStream.Position - p1 != Size)
            {
                throw new InvalidOperationException();
            }
#endif

        }

        public void Read(BinaryReader br)
        {
			long p1 = br.BaseStream.Position;
            ReadFromBinary(br);

#if DEBUG
			if (br.BaseStream.Position - p1 != Size - 4 ) 
			{
				throw new InvalidOperationException();
			}
#endif
		}

	}
}
