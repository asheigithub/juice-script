using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler.AST.Stmt
{
    public sealed class AS3Try : IAS3SyntaxNode
    {
        private Token token;
        public AS3Try(Token token)
        {
            this.token = token;
        }

        public Token Token
        {
            get { return token; }
        }


        public string label;

        public List<IAS3SyntaxNode> TryBlock = new List<IAS3SyntaxNode>();

        public List<IAS3SyntaxNode> FinallyBlock;

        public List<AS3Variable> CatchVarList = new List<AS3Variable>() ;

        public List<List<IAS3SyntaxNode>> CatchList = new List<List<IAS3SyntaxNode>>();

        public Token try_enter_token;
        public Token try_exit_token;

        public Token finally_enter_token;
        public Token finally_exit_token;

        public Token label_token;

        public List<Token> catch_exit_tokens = new List<Token>();

        public void Write(int v, StringBuilder out_sb)
        {
            if (!string.IsNullOrEmpty(label))
            {
                out_sb.AppendLine("".PadLeft(v, '\t') + label + ":{");
                v = v + 1;
            }

            out_sb.AppendLine("".PadLeft(v, '\t') + "try" );
            out_sb.AppendLine("".PadLeft(v, '\t') + "{" );

            for (int i = 0;i < TryBlock.Count; i++) 
            {
                TryBlock[i].Write(v+1, out_sb);
            }

            out_sb.AppendLine("".PadLeft(v, '\t') + "}" );

            for (int i = 0; i < CatchVarList.Count; i++)
            {
                out_sb.Append("".PadLeft(v, '\t') + "catch(");
                out_sb.Append(CatchVarList[i].Name);
                if (!string.IsNullOrEmpty(CatchVarList[i].TypeStr))
                {
                    out_sb.Append(":"+CatchVarList[i].TypeStr);
                }
                out_sb.AppendLine(")");

                out_sb.AppendLine("".PadLeft(v, '\t') + "{");

                for (int j = 0; j < CatchList[i].Count; j++)
                {
                    CatchList[i][j].Write(v + 1, out_sb);
                }

                out_sb.AppendLine("".PadLeft(v, '\t') + "}");
            }

            if (FinallyBlock != null)
            {
                out_sb.AppendLine("".PadLeft(v, '\t') + "finally");
                out_sb.AppendLine("".PadLeft(v, '\t') + "{");

                for (int i = 0; i < FinallyBlock.Count; i++)
                {
                    FinallyBlock[i].Write(v + 1, out_sb);
                }

                out_sb.AppendLine("".PadLeft(v, '\t') + "}");
            }


            if (!string.IsNullOrEmpty(label))
            {
                v = v - 1;
                out_sb.AppendLine("".PadLeft(v, '\t') + "}");
            }
        }
    }
}
