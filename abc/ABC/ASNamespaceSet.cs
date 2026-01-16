using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC
{
    public sealed class ASNamespaceSet : IEquatable<ASNamespaceSet>
    {
        public static event EventHandler<ASNamespaceSet> NewNamespaceSet;
        public ASNamespaceSet()
        {
            if (NewNamespaceSet != null)
            {
                NewNamespaceSet(null, this);
            }
        }


        public List<ASNamespace> Namespaces;


        public static bool operator ==(ASNamespaceSet left, ASNamespaceSet right)
        {
            return EqualityComparer<ASNamespaceSet>.Default.Equals(left, right);
        }
        public static bool operator !=(ASNamespaceSet left, ASNamespaceSet right)
        {
            return !(left == right);
        }
        public bool Equals(ASNamespaceSet other)
        {
            if (other == null) return false;
            if (!ReferenceEquals(this, other))
            {
                if (Namespaces.Count != other.Namespaces.Count) return false;
                for (int i = 0; i < Namespaces.Count; i++)
                {
                    if (Namespaces[i] != other.Namespaces[i]) return false;
                }
            }
            return true;
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            foreach (ASNamespace @namespace in Namespaces)
            {
                hash.Add(@namespace);
            }
            return hash.ToHashCode();
        }
        public override bool Equals(object obj)
        {
            return Equals(obj as ASNamespaceSet);
        }
    }
}
