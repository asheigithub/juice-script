using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler.AST
{
    /// <summary>
    /// AS3常量
    /// </summary>
    public sealed class AS3Const : AS3Member
    {
        public AS3Const(Token token) : base(token)
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
            out_sb.Append("const " + Name + ":" + TypeStr);
            out_sb.AppendLine(";");
        }

    }
}
