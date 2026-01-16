using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC
{
    public enum NamespaceKind : byte
    {
        /// <summary>
        /// 初次编译定义时，尚未确定的
        /// </summary>
        TBD = 0,
        Namespace = 0x08,
        Package = 0x16,
        PackageInternal = 0x17,
        Protected = 0x18,
        Explicit = 0x19,
        StaticProtected = 0x1A,
        Private = 0x05
    }
}
