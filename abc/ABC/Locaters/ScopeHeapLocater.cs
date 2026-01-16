using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.Locaters
{
    /// <summary>
    /// 定位CodeScope中的一个成员 <br/>
    /// ScopeIndex 从每一个脚本Script中开始编号 <br/>
    /// MemberIndex从每一个Scope中开始编号 <br/>
    /// </summary>
    public struct ScopeHeapLocater
    {
        public ushort ScopeIndex;
        public ushort MemberIndex;

        public void Write(BinaryWriter bw)
        {
            bw.Write(ScopeIndex);
            bw.Write(MemberIndex);
        }

        public void ReadFromBinary(BinaryReader br)
        { 
            ScopeIndex = br.ReadUInt16();
            MemberIndex = br.ReadUInt16();
        }

        public override string ToString()
        {
            return $"scope:{ScopeIndex},member:{MemberIndex}";
        }

    }
}
