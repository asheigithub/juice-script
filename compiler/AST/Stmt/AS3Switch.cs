using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler.AST.Stmt
{
    public sealed class AS3Switch : IAS3SyntaxNode
    {
        private Token token;
        public AS3Switch(Token token)
        {
            this.token = token;
        }

        public Token Token
        {
            get { return token; }
        }

        public Token default_part_token;


        public string label;

        public AS3Expression Expr;

        public List<AS3Expression > CaseTestList= new List<AS3Expression>();

        public List<List<IAS3SyntaxNode>> CaseBodyList=new List<List<IAS3SyntaxNode>>();

        

        public void Write(int v, StringBuilder out_sb)
        {
            if (!string.IsNullOrEmpty(label))
            {
                out_sb.AppendLine("".PadLeft(v, '\t') + label + ":{");
                v = v + 1;
            }

            if (Expr.exprStepList.Count > 0)
            {
                Expr.Write(v, out_sb);
            }

            out_sb.AppendLine("".PadLeft(v, '\t') + "switch(" + Expr.Value.ToString() + ")" );
            out_sb.AppendLine("".PadLeft(v, '\t') + "{" );


            for (int i = 0;i<CaseTestList.Count;i++) 
            {
                if (CaseTestList[i] != null)
                {
                    out_sb.AppendLine("".PadLeft(v + 1, '\t') + "case (");
                    CaseTestList[i].Write(v+2, out_sb);
                    out_sb.AppendLine("".PadLeft(v + 1, '\t') + "):");

                }
                else
                {
                    out_sb.AppendLine("".PadLeft(v + 1, '\t') + "default :");
                    
                }

                out_sb.AppendLine("".PadLeft(v + 1, '\t') + "{");
                for (int j = 0; j < CaseBodyList[i].Count; j++)
                {
                    CaseBodyList[i][j].Write(v + 2, out_sb);

                }
                out_sb.AppendLine("".PadLeft(v + 1, '\t') + "}");
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
