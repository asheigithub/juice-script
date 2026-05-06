using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.runtime
{


    public sealed class RtDynamic : RtHeapBase
    {
        public RtDynamic() : base(RtHeapTypeKind.DYNAMIC_PROPERTYS) { }

		public override int Size => (8 + 8) * Slots.Count + 8 + 8;

        /// <summary>
        /// 指向Transation链的Shape节点
        /// </summary>
        public int SHAPE_PTR;

        public List<NaNBoxing> Slots = new List<NaNBoxing>();

    }
}
