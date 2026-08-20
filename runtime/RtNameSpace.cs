using juicescript.ABC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.runtime
{
	/// <summary>
	/// 负载Namespace实例
	/// </summary>
#if FORCOMPILER
	internal
#else
    public
#endif
		sealed class RtNameSpace : RtHeapBase
    {
        public RtNameSpace() : base( RtHeapTypeKind.NAMESPACE) { }

		public override int Size
        {
            get
            {
                return 4 + 4 + 4;
            }
        }

        public ASNamespace ASNamespace;

        public int prefixPtr;
        public int uriPtr;

        public override string ToString()
        {
            return $"payload: {ASNamespace}";
        }

    }
}
