using juicescript.ABC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler.IL
{
    /// <summary>
    /// 编译期记录临时类型
    /// 分主类型和次类型
    /// 如果主类型为CLASS
    /// 则次类型保存了CLASS指向的具体类型
    /// 否则次类型为Unknown
    /// </summary>
    internal struct CompileTypeKind : IEquatable<CompileTypeKind>
    {        
        public TypeKind Maj;
        public TypeKind Mir;


        public override string ToString()
        {
            return $"Maj:{Maj} Mir:{Mir}";
        }

        public bool Equals(CompileTypeKind other)
        {
            return Maj == other.Maj && Mir == other.Mir;
        }

        public static bool operator==(CompileTypeKind left, CompileTypeKind right) 
        { 
            return left.Equals(right);
        }

        public static bool operator !=(CompileTypeKind left, CompileTypeKind right) 
        {
            return !left.Equals(right);
        }

        public override bool Equals(object obj)
        {
            return obj is CompileTypeKind && Equals((CompileTypeKind)obj);
        }

        public override int GetHashCode()
        {
            return Maj.GetHashCode() ^ Mir.GetHashCode();
        }
    }
}
