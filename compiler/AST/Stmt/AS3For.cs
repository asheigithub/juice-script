using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler.AST.Stmt
{
    public sealed class AS3For : IAS3SyntaxNode
    {
        private Token token;
        public AS3For(Token token)
        {
            this.token = token;
        }

        public Token Token
        {
            get { return token; }
        }

        public string label;


        public List<IAS3SyntaxNode> Part2 = new List<IAS3SyntaxNode>();
        public List<IAS3SyntaxNode> Part3 = new List<IAS3SyntaxNode>();
        public List<IAS3SyntaxNode> Body = new List<IAS3SyntaxNode>();

        public void Write(int v, StringBuilder out_sb)
        {
            if (!string.IsNullOrEmpty(label))
            {
                out_sb.AppendLine("".PadLeft(v, '\t') + label + ":{");
                v = v + 1;
            }

            out_sb.AppendLine("".PadLeft(v, '\t') + "for(;" );
            for (int i = 0; i < Part2.Count; i++)
            {
                Part2[i].Write(v+1, out_sb);
            }
            out_sb.AppendLine("".PadLeft(v, '\t') + ";");
            for (int i = 0; i < Part3.Count; i++)
            {
                Part3[i].Write(v + 1, out_sb);
            }
            out_sb.AppendLine("".PadLeft(v, '\t') + ")");
            out_sb.AppendLine("".PadLeft(v, '\t') + "{");

            for (int i = 0;i < Body.Count; i++)
            {
                Body[i].Write(v + 1, out_sb);
            }
            out_sb.AppendLine("".PadLeft(v, '\t') + "}");


            if (!string.IsNullOrEmpty(label))
            {
                v = v - 1;
                out_sb.AppendLine("".PadLeft(v, '\t') + "}");
            }
            out_sb.AppendLine();
        }
    }
}
