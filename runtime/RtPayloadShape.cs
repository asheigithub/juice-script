using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.runtime
{
    /// <summary>
    /// 负载动态对象的Transition链节点
    /// </summary>
    public sealed class RtPayloadShape : FacilityBase
    {
        [Flags]
        public enum PropertyAttribute
        {
            Writable = 1,
            Enumerable = 2,
            Configurable = 4,
        }


        public override int Size => 8 + 4 + 4 + 4 + 4 + 8;

        /// <summary>
        /// 指向下一个子节点
        /// </summary>
        public int PTR_CHILD;

        /// <summary>
        /// 指向下一个兄弟节点
        /// </summary>
        public int PTR_BROTHER;

        /// <summary>
        /// 指向父节点
        /// </summary>
        public int PTR_PARENT;

        /// <summary>
        /// 节点属性
        /// </summary>
        public PropertyAttribute Attribute;

        /// <summary>
        /// 属性名 - 使用LocalString优化，短名称内联存储，长名称使用堆分配
        /// </summary>
        public NaNBoxing PTR_NAME;


    }
}
