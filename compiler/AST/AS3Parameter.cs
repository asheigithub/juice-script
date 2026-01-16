using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler.AST
{
    public sealed class AS3Parameter : AS3Member
    {
        public AS3Parameter(Token token) : base(token)
        {
            TypeStr = "*";
        }

        /// <summary>
        /// 是否是...参数
        /// </summary>
        public bool IsArrPara;


        /// <summary>
        /// 默认值表达式定义
        /// </summary>
        public AS3Expression ValueExpr;

    }
}
