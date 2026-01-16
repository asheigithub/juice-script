using juicescript.ABC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript
{
    public class TypeLayout
    {
        public ASClass ASType;
        
        public int Size;

        public List<int> Offset = new List<int>();
        public List<int> SlotSize = new List<int>();
        public List<int> SlotAlign = new List<int>();

    }
}
