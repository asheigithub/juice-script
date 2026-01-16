using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC
{
    public enum ConstantKind : byte
    {
        Null = 0x0C,
        Undefined = 0x00,

        String = 0x01,
        Double = 0x06,
        Integer = 0x03,
        UInteger = 0x04,

        True = 0x0B,
        False = 0x0A,

        Namespace = 0x08,
        PackageNamespace = 0x16,
        PackageInternalNs = 0x17,
        ProtectedNamespace = 0x18,
        ExplicitNamespace = 0x19,
        StaticProtectedNs = 0x1A,
        PrivateNs = 0x05

    }
}
