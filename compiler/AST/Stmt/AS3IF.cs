using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler.AST.Stmt
{
    public sealed class AS3IF : IAS3SyntaxNode
    {
        private Token token;
        public Token label_token;

        public AS3IF(Token token) 
        { 
            this.token = token;
        }

        public string label;
        public List<IAS3SyntaxNode> condition = new List<IAS3SyntaxNode>();
        public List<IAS3SyntaxNode> truepart = new List<IAS3SyntaxNode>();
        public List<IAS3SyntaxNode> falsepart = new List<IAS3SyntaxNode>();

        public Token Token
        {
            get {  return token; }
        }

        public void Write(int v, StringBuilder out_sb)
        {
            if (!string.IsNullOrEmpty(label))
            {
                out_sb.AppendLine("".PadLeft(v, '\t') + label + ":{");
                v = v + 1;
            }
            

            if (condition.Count > 1 || ((AS3Expression)condition[condition.Count-1]).exprStepList.Count >0 )
            {
                for (int i = 0; i < condition.Count; i++)
                {
                    condition[i].Write(v, out_sb);
                }
            }

            out_sb.AppendLine("".PadLeft(v, '\t') + "if (" +  ((AS3Expression) condition[condition.Count -1]).Value.ToString() + ")"  );
            out_sb.AppendLine("".PadLeft(v, '\t') + "{" );

            for (int i = 0;i < truepart.Count;i++)
            {
                truepart[i].Write(v + 1, out_sb);
            }

            out_sb.AppendLine("".PadLeft(v, '\t') + "}" );

            if (falsepart.Count > 0)
            {
                out_sb.AppendLine("".PadLeft(v, '\t') + "else");
                out_sb.AppendLine("".PadLeft(v, '\t') + "{");
                for (int i = 0; i < falsepart.Count; i++)
                {
                    falsepart[i].Write(v + 1, out_sb);
                }
                out_sb.AppendLine("".PadLeft(v, '\t') + "}");
            }

            if (!string.IsNullOrEmpty(label))
            {
                v = v - 1;
                out_sb.AppendLine("".PadLeft(v, '\t')+ "}");
            }
            out_sb.AppendLine();
        }
    }
}
