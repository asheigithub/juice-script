using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler.AST.Expr
{
    /// <summary>
    /// AST 解析出的数据类型
    /// </summary>
    public enum FF1DataValueType
    {
        dynamicobj,
        e4xxml,
        identifier,
        this_pointer,
        super_pointer,

        const_number,

        const_string,

        const_regexp,

        as3_function,
        as3_array,
        as3_vector,
        as3_callarguments,

        as3_expressionlist,

        compiler_const,

    }
}
