using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC
{
    public sealed class ASVector : IEquatable<ASVector>
    {
        public int depth;

        public string Str;

        public TypeKind Identifier;

        public TypeKind ElementType;

        public ASClass vector_class;

        
        public ASScript vScript;


        public override string ToString()
        {
            return Str;
        }

        public bool Equals(ASVector other)
        {
            if (other == null)
                return false;
            else
                return depth == other.depth && Identifier == other.Identifier && ElementType == other.ElementType;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ASVector);
        }

        public override int GetHashCode()
        {
            return depth ^ Identifier.GetHashCode() ^ ElementType.GetHashCode();
        }

    }
}
