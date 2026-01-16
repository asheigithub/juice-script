using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler.AST.Stmt
{
    public sealed class AS3With : IAS3SyntaxNode
    {
        private Token token;
        public AS3With(Token token)
        {
            this.token = token;
        }

        public Token Token
        {
            get { return token; }
        }

        public string label;

        public List<IAS3SyntaxNode> WithExpr = new List<IAS3SyntaxNode>();

        public List<IAS3SyntaxNode> Code = new List<IAS3SyntaxNode>();

        public void Write(int v, StringBuilder out_sb)
        {
            if (!string.IsNullOrEmpty(label))
            {
                out_sb.AppendLine("".PadLeft(v, '\t') + label + ":{");
                v = v + 1;
            }

            if (WithExpr.Count > 1 || ((AS3Expression)WithExpr[WithExpr.Count - 1]).exprStepList.Count > 0)
            {
                for (int i = 0; i < WithExpr.Count; i++)
                {
                    WithExpr[i].Write(v, out_sb);
                }
            }

            out_sb.AppendLine("".PadLeft(v, '\t') + "with(" + ((AS3Expression)WithExpr[WithExpr.Count - 1]).Value + ")");
            out_sb.AppendLine("".PadLeft(v, '\t') + "{");

            for (int i = 0; i < Code.Count; i++)
            {
                Code[i].Write(v + 1, out_sb);
            }


            out_sb.AppendLine("".PadLeft(v, '\t') + "}");

            if (!string.IsNullOrEmpty(label))
            {
                v = v - 1;
                out_sb.AppendLine("".PadLeft(v, '\t') + "}");
            }
        }
    }
}
