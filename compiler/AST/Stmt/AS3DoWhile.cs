using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler.AST.Stmt
{
    public sealed class AS3DoWhile :IAS3SyntaxNode
    {
        private Token token;
        public AS3DoWhile(Token token)
        {
            this.token = token;
        }

        public Token Token
        {
            get { return token; }
        }

        public string label;

        public List<IAS3SyntaxNode> Condition = new List<IAS3SyntaxNode>();

        public List<IAS3SyntaxNode> Body = new List<IAS3SyntaxNode>();



        public void Write(int v, StringBuilder out_sb)
        {
            if (!string.IsNullOrEmpty(label))
            {
                out_sb.AppendLine("".PadLeft(v, '\t') + label + ":{");
                v = v + 1;
            }

            out_sb.AppendLine("".PadLeft(v, '\t') + "do");
            out_sb.AppendLine("".PadLeft(v, '\t') + "{");

            for (int i = 0; i < Body.Count; i++)
            {
                Body[i].Write(v + 1, out_sb);
            }

            if (Condition.Count > 1 || ((AS3Expression)Condition[Condition.Count - 1]).exprStepList.Count > 0)
            {
                for (int i = 0; i < Condition.Count; i++)
                {
                    Condition[i].Write(v + 1, out_sb);
                }
            }

            out_sb.AppendLine("".PadLeft(v, '\t') + "}");

            out_sb.AppendLine("".PadLeft(v, '\t') + "while(" + ((AS3Expression)Condition[Condition.Count - 1]).Value + ")");

           

            if (!string.IsNullOrEmpty(label))
            {
                v = v - 1;
                out_sb.AppendLine("".PadLeft(v, '\t') + "}");
            }
            out_sb.AppendLine();
        }
    }
}
