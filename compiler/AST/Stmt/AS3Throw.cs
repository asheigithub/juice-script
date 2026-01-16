using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler.AST.Stmt
{
    public sealed class AS3Throw : IAS3SyntaxNode
    {
        private Token token;
        public AS3Throw(Token token)
        {
            this.token = token;
        }

        public Token Token
        {
            get { return token; }
        }

        public AS3Expression Expression;


        public void Write(int v, StringBuilder out_sb)
        {
            if (Expression == null)
            {
                out_sb.AppendLine("".PadLeft(v, '\t') + "throw;");
            }
            else
            {
                if (Expression.exprStepList.Count > 0)
                { 
                    Expression.Write(v, out_sb);                  
                }
                out_sb.AppendLine("".PadLeft(v, '\t') + "throw " + Expression.Value.ToString() + ";");
            }
        }
    }
}
