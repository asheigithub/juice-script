using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler.AST.Stmt
{
    public sealed class AS3Break : IAS3SyntaxNode
    {
        private Token token;
        public AS3Break(Token token)
        {
            this.token = token;
        }

        public Token Token
        {
            get { return token; }
        }


        public string breakTarget;

        public void Write(int v, StringBuilder out_sb)
        {
            out_sb.Append( "".PadLeft(v,'\t') + "break" );

            if (!string.IsNullOrWhiteSpace(breakTarget))
            { 
                out_sb.Append(" " + breakTarget);
            }

            out_sb.AppendLine(";");
        }
    }
}
