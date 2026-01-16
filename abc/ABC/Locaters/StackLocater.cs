using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.Locaters
{
    /// <summary>
    /// 栈上数据定位器
    /// </summary>
    public struct StackLocater:IEquatable<StackLocater>
    {
        public int index;

        public bool Equals(StackLocater other)
        {
            return index == other.index;
        }

        public override int GetHashCode()
        {
            return index.GetHashCode();
        }

        public override string ToString()
        {
            return $"stack:{index}";
        }


        public void Write(BinaryWriter bw)
        {
            bw.Write(index);
        }

        public void ReadFromBinary(BinaryReader br)
        { 
            index = br.ReadInt32();
        }

    }
}
