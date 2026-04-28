using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC
{
    [Flags]
    public enum MethodFlags
    {
        None = 0,

        /// <summary>
        /// 是否需要构造arguments数组对象
        /// </summary>
        NeedArguments = 1,

        /// <summary>
        /// 是否需要创建一个执行期上下文对象
        /// 举例来说，比如函数内声明一个变量，然后这个变量还被闭包使用了，那就需要创建 activation object
        /// </summary>
        NeedActivation = 2,

        /// <summary>
        /// 需要 ... rest 数组对象，不可能和NeedArguments同时设置
        /// </summary>
        NeedRest = 4,

        HasOptional = 8,

        IgnoreRest = 16,
        /// <summary>
        /// Native函数
        /// </summary>
        Native = 32,
        /// <summary>
        /// 结构体的方法，排除了各种危险操作。
        /// </summary>
        StructMethod = 0x40,
        /// <summary>
        /// 用于外部工具，Methodinfo中保存了参数名
        /// </summary>
        HasParamNames = 128,

        /// <summary>
        /// 编译时，指示这个方法用package的importlist
        /// </summary>
        PackageMemberScope = 256,

        /// <summary>
        /// 编译时指示标记了Override
        /// </summary>
        MarkOverride = 512,

        /// <summary>
        /// 代码里没有try语句
        /// </summary>
        NoTry = 1024,


        Generator = 2048,

        ASYNC =4096,
    }
}
