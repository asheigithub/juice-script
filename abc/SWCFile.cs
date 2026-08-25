using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using juicescript.ABC;

namespace juicescript
{
    public sealed class SWCFile
    {
        public int Major;
        public int Minor;
        public int Build;
        public int Revision;
        public Guid UID;

        public string assemblyName;
        public List<string> refAssemblys { get; }

        public List<ASMethod> Methods { get; }
        public List<ASClass> Classes { get; }
        public List<ASScript> Scripts { get; }

        public ASNamespaceSet[] NamespaceSets { get; internal set; }

        public ASNamespace[] Namespaces { get; internal set; }
        
        public string[] const_strings { get; internal set; }
        //public ulong[] ld_classid { get; internal set; }

        //public Tuple<ulong,int>[] ld_supermethods { get; internal set; }

        public ASVector[] Vectors { get; internal set; }



        public NaNBoxing[] runtime_alloced_strings;

        public SWCFile()
        { 
            refAssemblys = new List<string>();
            Methods = new List<ASMethod>();
            Classes = new List<ASClass>();
            Scripts = new List<ASScript>();
        }

        public override string ToString()
        {
            return $"SWC:{assemblyName},Scripts:{Scripts.Count},UID:{UID}";
        }

    }
}
