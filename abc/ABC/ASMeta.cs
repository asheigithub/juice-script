using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC
{
    public sealed class ASMetaItem
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public sealed class ASMeta
    {
        public string Name { get; set; }

        public List<ASMetaItem> Items { get; private set; }

        public ASMeta()
        {
            Items = new List<ASMetaItem>();
        }


    }
}
