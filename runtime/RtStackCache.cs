using juicescript.ABC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.runtime
{
    internal sealed class RtStackCache : RtHeapBase
    {
        public RtStackCache() : base(RtHeapTypeKind.STACK_CACHE_OBJ) { }

		public NaNBoxing RefInstance; //int RefInstance_index;

        public ushort scopemember_index;

        public ASTrait[] trait = new ASTrait[2];

        public ASContainer as_type;

        

        public NaNBoxing searchPropertyName;

        public int searchNameSpacePtr;

       
        //遇到 a[id] 这样的访问时 的id   可能保存有 Array的下标。Array下标肯定是一个无符号整数
        public NaNBoxing indexer_key;

        public int g_index;
        public int s_index;


        public override int Size
        {
            get
            {
                return 4 /*+ 2*/ +2 + 8 + 4 + 4 + 4 + 4;
            }
        }
    }
}
