using juicescript.ABC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.runtime
{
    public enum HeapKindFlag : byte
    { 
        NONE = 0,
		FLAG_STRUCT = 1,
	}


    public enum RtHeapTypeKind : byte
    { 
        /// <summary>
        /// 表示ASClass实例
        /// </summary>
        CLASS=1,
        /// <summary>
        /// 表示ASScript实例
        /// </summary>
        GLOBAL=2,

        /// <summary>
        /// 表示保存字符串
        /// </summary>
        STRING =3,

        /// <summary>
        /// 表示对象的实例
        /// </summary>
        INSTANCE = 4,

        ///// <summary>
        ///// 表示用于LD_CLASS的缓存对象
        ///// </summary>
        //CACHE_LD_CLASS = 10,

        /// <summary>
        /// 表示NAMESPACE实例
        /// </summary>
        NAMESPACE = 5,

        /// <summary>
        /// 表示ARRAY实例
        /// </summary>
        ARRAY = 6,

        /// <summary>
        /// 表示VECTOR实例
        /// </summary>
        VECTOR = 7,

        /// <summary>
        /// 表示栈上缓存功能性临时对象，比如成员的引用等
        /// </summary>
        STACK_CACHE_OBJ = 8,

        /// <summary>
        /// 动态属性
        /// </summary>
        DYNAMIC_PROPERTYS =9,

        /// <summary>
        /// Transaction  Shape节点 
        /// </summary>
        SHAPE = 10,

        /// <summary>
        /// 表示函数执行上下文
        /// </summary>
        MethodScope = 11,

        /// <summary>
        /// 表示创建的闭包
        /// </summary>
        CLOSURE = 12,

    }


    public abstract class RtHeapBase
    {
        
      

		public ASContainer Type;

		public readonly RtHeapTypeKind Kind;

		internal bool gc_mark;

		public abstract int Size { get; }

        public RtHeapBase(RtHeapTypeKind typeKind)
        {
            this.Kind = typeKind;
        }



		public override string ToString()
        {
            return $"RtHeap:{Kind}";
        }

    }

}
