using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler.AST.Stmt
{
    public sealed class AS3Return : IAS3SyntaxNode
    {
        private Token token;
        public AS3Return(Token token)
        {
            this.token = token;
        }

        public Token Token
        {
            get { return token; }
        }

        public List<IAS3SyntaxNode> ReturnValue = new List<IAS3SyntaxNode>();

        public void Write(int v, StringBuilder out_sb)
        {
            if (ReturnValue.Count > 1 || (ReturnValue.Count >0 && ((AS3Expression)ReturnValue[ReturnValue.Count - 1]).exprStepList.Count >0))
            { 
                for (int i = 0; i < ReturnValue.Count; i++) { ReturnValue[i].Write(v, out_sb); }
            }

            out_sb.Append("".PadLeft(v, '\t') + "return");

            if (ReturnValue.Count>0)
            {
                out_sb.Append(" " + ((AS3Expression)ReturnValue[ReturnValue.Count -1]).Value);
            }

            out_sb.AppendLine(";");

        }
    }
}
