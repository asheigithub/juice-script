using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC
{
    public sealed class ASMultiname : IEquatable<ASMultiname>
    {
        public static event EventHandler<ASMultiname> NewMultiName;

        public ASMultiname()
        {
            if (NewMultiName != null)
            {
                NewMultiName(null, this);
            }
        }

        public MultinameKind Kind { get; set; }

        public string Name { get; set; }

        public ASMultiname QName { get; set; }


        public ASNamespace Namespace { get; set; }

        public ASNamespaceSet NamespaceSet { get; set; }


        public List<ASMultiname> Types { get; set; }

        public bool Equals(ASMultiname other)
        {
            if (other == null) return false;

            if (Kind != other.Kind) return false;

            if (Name != other.Name) return false;
            if (QName != other.QName) return false;
            if (Namespace != other.Namespace) return false;
            if (NamespaceSet != other.NamespaceSet) return false;

            if (Types == null && other.Types != null) return false;
            if (Types != null && other.Types == null) return false;
            if (Types != null && other.Types != null)
            {
                if (Types.Count != other.Types.Count) return false;

                for (int i = 0; i < Types.Count; i++)
                {
                    if (Types[i] != other.Types[i])
                        return false;
                }
            }

            return true;
        }
        public override bool Equals(object obj)
        {
            return Equals(obj as ASMultiname);
        }

        public override int GetHashCode()
        {
            HashCode hashCode = new HashCode();

            hashCode.Add(Kind);
            if (Name != null)
                hashCode.Add(Name);

            if (QName != null)
                hashCode.Add(QName);

            if (Namespace != null)
                hashCode.Add(Namespace);

            if (NamespaceSet != null)
                hashCode.Add(NamespaceSet);

            return hashCode.ToHashCode();
        }

        public static bool operator ==(ASMultiname left, ASMultiname right)
        {
            return EqualityComparer<ASMultiname>.Default.Equals(left, right);
        }
        public static bool operator !=(ASMultiname left, ASMultiname right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            if (Kind == MultinameKind.TypeName)
            {
                return $"{Kind}: Vector.<{Types[0]}>";
            }
            else
            {
                return $"{Kind}: \"{Namespace.Name}.{Name}\"";
            }
        }

        

    }
}
