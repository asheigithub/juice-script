using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler.AST.Stmt
{
    public sealed class AS3Continue : IAS3SyntaxNode
    {
        private Token token;
        public AS3Continue(Token token)
        {
            this.token = token;
        }

        public Token Token
        {
            get { return token; }
        }

        public string continueTarget;

        public void Write(int v, StringBuilder out_sb)
        {
            out_sb.Append("".PadLeft(v, '\t') + "continue");

            if (!string.IsNullOrWhiteSpace(continueTarget))
            {
                out_sb.Append(" " + continueTarget);
            }

            out_sb.AppendLine(";");
        }
    }
}
