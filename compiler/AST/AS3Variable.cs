using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler.AST
{
    /// <summary>
    /// AS3变量
    /// </summary>
    public sealed class AS3Variable : AS3Member
    {
        public AS3Variable(Token token) : base(token)
        {
            TypeStr = "*";
        }

        /// <summary>
        /// 默认值表达式定义
        /// </summary>
        public AS3Expression ValueExpr;


        public override void Write(int v, StringBuilder out_sb)
        {
            base.Write(v, out_sb);

            out_sb.Append("var " + Name + ":" + TypeStr );

            out_sb.AppendLine(";");

        }

    }
}
