using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler.AST.Stmt
{
    public sealed class AS3YieldBreak : IAS3SyntaxNode
    {
        private Token token;
        public AS3YieldBreak(Token token)
        {
            this.token = token;
        }

        public Token Token
        {
            get { return token; }
        }

        public void Write(int v, StringBuilder out_sb)
        {
            out_sb.AppendLine("".PadLeft(v, '\t') + "yield break;");
        }
    }
}
