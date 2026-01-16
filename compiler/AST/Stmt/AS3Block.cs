using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler.AST.Stmt
{
    public sealed class AS3Block : IAS3SyntaxNode
    {
        private Token token;
        public AS3Block(Token token)
        {
            this.token = token;
        }

        public Token Token
        {
            get { return token; }
        }


        public string label;

        public Token label_token;
        public Token exit_token;


        public List<IAS3SyntaxNode> Code = new List<IAS3SyntaxNode>();

        public void Write(int v, StringBuilder out_sb)
        {
            if (!string.IsNullOrEmpty(label))
            {
                out_sb.Append("".PadLeft(v, '\t'));

                out_sb.Append(label + ":");


                out_sb.AppendLine("{");

                for (int i = 0; i < Code.Count; i++)
                {
                    Code[i].Write(v + 1, out_sb);
                }

                out_sb.AppendLine("".PadLeft(v, '\t') + "}");
            }
            else
            {
                for (int i = 0; i < Code.Count; i++)
                {
                    Code[i].Write(v, out_sb);
                }
            }

        }
    }
}
