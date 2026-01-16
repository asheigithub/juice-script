using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC
{
    [Flags]
    public enum TraitAttributes
    {
        None = 0x00,
        Final = 0x01,
        Override = 0x02,
        Metadata = 0x04
    }
}
