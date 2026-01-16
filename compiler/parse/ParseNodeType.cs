using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler.parse
{

    /// <summary>
    /// 文法节点类型
    /// </summary>
    public enum ParseNodeType
    {
        /// <summary>
        /// 非终结符
        /// </summary>
        non_terminal,

        /// <summary>
        /// 终结符 如"+" "("
        /// </summary>
        terminal,

        /// <summary>
        /// 检测出的label
        /// </summary>
        label,

        /// <summary>
        /// 检测出的没用的label
        /// </summary>
        useless_label,

        /// <summary>
        /// 关键字this
        /// </summary>
        _this,

        /// <summary>
        ///  关键字super
        /// </summary>
        super,

        /// <summary>
        /// 终结符-数字 number
        /// </summary>
        number,

        /// <summary>
        /// 终结符-标识符 identifier
        /// </summary>
        identifier,

        /// <summary>
        /// 终结符-字符串 string
        /// </summary>
        conststring,

        /// <summary>
        /// 终结符-空匹配 null
        /// </summary>
        _null,

        /// <summary>
        /// 空白符号S 
        /// </summary>
        whitespace,


        /// <summary>
        /// 右端输入结束符 $$
        /// </summary>
        eof,

        /// <summary>
        /// 错误 wrong
        /// </summary>
        wrong
    }
}
