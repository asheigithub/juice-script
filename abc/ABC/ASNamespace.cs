using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC
{
    public sealed class ASNamespace : IEquatable<ASNamespace>
    {
        public static event EventHandler<ASNamespace> NewNameSpace;

        public ASNamespace()
        {
            if (NewNameSpace != null)
            {
                NewNameSpace(null, this);
            }
            in_package = string.Empty;
            def_uri = null;

            __instance_index__ = 0;
        }

        public string Name { get; set; }

        public NamespaceKind Kind { get; set; }


        public string in_package { get; set; }

        
        public string def_uri { get; set; }


        /// <summary>
        /// 记录是否被引擎初始化
        /// </summary>
        public int __instance_index__;

        public bool Equals(ASNamespace other)
        {
            if (other == null) return false;
            if (Name != other.Name) return false;
            if (Kind != other.Kind) return false;
            if(in_package != other.in_package) return false;

            if (def_uri != other.def_uri) return false;

            return true;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ASNamespace);
        }

        public override int GetHashCode()
        {
            HashCode hc = new HashCode();

            if (Name != null)
                hc.Add(Name);

            hc.Add(Kind);
            return hc.ToHashCode();
        }
        public static bool operator ==(ASNamespace left, ASNamespace right)
        {
            return EqualityComparer<ASNamespace>.Default.Equals(left, right);
        }
        public static bool operator !=(ASNamespace left, ASNamespace right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return $"{Kind}: \"{Name}\"";
        }
    }
}
