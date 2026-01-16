using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace juicescript.ABC
{
    /// <summary>
    /// AVM 中的名称由非限定名称和一个或多个命名空间的组合表示。
    /// 这些统称为多重名称。 多名称条目通常由名称索引和名称空间或命名空间集索引。 
    /// 某些多重名称可以在运行时解析名称 和/或 命名空间部分。 
    /// 如下所述是多种不同类型的多重名称，
    /// 对象的属性总是由简单的 QName（一对名称和命名空间）。
    /// 其他类型的多重名称用于在运行时解析属性。
    /// RTQName、RTQNameL 和 MultinameL 统称为运行时多重名称。
    /// </summary>
    public enum MultinameKind : byte
    {
        /// <summary>
        /// 用于编译时尚未确定  编译完成后不可能出现
        /// </summary>
        TBD = 0,

        /// <summary>
        /// 这是最简单的多重名称形式。 它是一个只有一个NameSpace的Name，因此 QName 代表合格的命名。 
        /// QName 条目将有一个名称索引，后跟一个名称空间索引。 
        /// 名称索引是一个索引字符串常量池，
        /// 命名空间索引是命名空间常量池的索引。
        /// 
        /// public var s : String;
        /// 
        /// 此代码将生成两个 QName 条目，一个用于变量 s（public namespace，name “s”），另一个用于 类型 String（public namespace，name“String”）。
        /// </summary>
        QName = 0x07,
        QNameA = 0x0D,

        /// <summary>
        /// 这是一个运行时 QName，其中namespace直到运行时才会解析。 RTQName 条目将有只有一个name索引，它是字符串常量池中的索引。 命名空间是在运行时确定的。
        /// 当 RTQName 是操作码的操作数时，RTQName 应该使用的堆栈上应该有一个命名空间值。 因此，当使用RTQName时，堆栈顶部的值将被弹出，并且RTQName 将使用它作为其命名空间。
        /// 
        /// var ns = getANamespace();
        /// x = ns::r;
        /// This code will produce a RTQName entry for ns::r.
        /// It will have a name of "r" and code will be generated topush the value of ns onto the stack.
        /// </summary>
        RTQName = 0x0F,
        RTQNameA = 0x10,

        /// <summary>
        /// 这是一个运行时 QName，其中名称和命名空间都是在运行时解析的。 
        /// 当 RTQNameL是操作码的操作数，堆栈上将有一个名称和一个命名空间值。 
        /// 上的名称值堆栈必须是 String 类型，并且堆栈上的命名空间值必须是 Namespace 类型。
        /// RTQNameL 通常用于当编译时名称和限定符都不知道时的限定名称。
        /// 
        /// var x = getAName();
        /// var ns = getANamespace();
        /// w = ns::[x];
        /// This code will produce a RTQNameL entry in the constant pool for ns::[x]. 
        /// It has neither a name nor a namespace, but code will be generated to push the value of ns and x onto the stack.
        /// </summary>
        RTQNameL = 0x11,
        RTQNameLA = 0X12,

        /// <summary>
        /// 这是一个具有名称和命名空间集的多重名称。 命名空间集用于表示一组命名空间。 
        /// 多名称条目将有一个name索引，后跟一个namespace 集索引。 name索引是字符串常量池的索引，namespace 集索引是 命名空间集 常量池的索引。
        /// 多重名称通常用于非限定名称。 在这些情况下，所有打开的命名空间都用于Multiname。
        /// 
        /// use namespace t;
        /// trace(f);
        /// 
        /// This code will produce a multiname entry for f. It will have a name of "f" and a namespace set for all the open 
        /// namespaces(the public namespace, the namespace t, and any private or internal namespaces open in that context). 
        /// At runtime f could be resolved in any of the namespaces specified by the multiname.
        /// </summary>
        Multiname = 0x09,
        MultinameA = 0x0E,

        /// <summary>
        /// 这是一个运行时多重名称，其中name在运行时解析。
        /// namespace集用于表示一个namespace的集合。 
        /// MultinameL 条目具有命名空间集索引。 命名空间集索引是一个索引放入命名空间集合常量池中。
        /// 当 MultinameL 是操作码的操作数时，将会有一个name堆栈上的值。 堆栈上的name值必须是 String 类型。
        /// MultinameL 通常用于非限定名称，其中name在编译时未知。
        /// 
        /// use namespace t;
        /// trace(o[x]);
        /// 
        /// This code will produce a MultinameL entry.
        /// It will have no name, and will have a namespace set for all the open namespaces in that context. 
        /// Code will be generated to push the value of x onto the stack, and that value will be used as the name.
        /// </summary>
        MultinameL = 0x1B,
        MultinameLA = 0x1C,

        /// <summary>
        /// 用于Vector.&lt;T> 
        /// Multiname的QName指向 _AS3_.vec.Vector,Type指向类型
        /// </summary>
        TypeName = 0x1D
    }
}
