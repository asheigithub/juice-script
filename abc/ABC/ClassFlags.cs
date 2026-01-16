using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC
{
    [Flags]
    public enum ClassFlags
    {
        /// <summary>
        /// Represents no flags for this class.
        /// </summary>
        None = 0x00,
        /// <summary>
        /// Represents a sealed class where properties can't be dynamically added to instances of the class.
        /// </summary>
        Sealed = 0x01,
        /// <summary>
        /// Represents a class that can't be used as a base class for any other class.
        /// </summary>
        Final = 0x02,
        /// <summary>
        /// Represents a class that is of interface type
        /// </summary>
        Interface = 0x04,
        /// <summary>
        /// Represents a class that uses its' protected namespace meaning the property <see cref="ASInstance.ProtectedNamespace"/> is present.
        /// </summary>
        ProtectedNamespace = 0x08,

        /// <summary>
        /// 表示这是一个Vector的类型
        /// </summary>
        Vector = 0x10,

        /// <summary>
        /// 扩展--表示是一个类似NullAble的结构体
        /// </summary>
        Struct = 0x20,

        /// <summary>
        /// 可缓存 
        /// 前提总成员小于16 * 8 字节
        /// 当初次分配时，使用一个栈上缓存对象，只有当将它保存到堆时，才实际用GC分配内存然后将内存复制过去。
        /// </summary>
        CacheAble = 0x40,

        /// <summary>
        /// 表示这是一个包装本地对象的类型 [wapper]
        /// </summary>
        Wapper = 0x80,

        /// <summary>
        /// 表示这个对象有索引器 (只能用于 Wapper对象)
        /// </summary>
        Indexer = 0x100,
        
        /// <summary>
        /// 表示对象无法实例化
        /// </summary>
        NoConstructor = 0x200

    }
}
