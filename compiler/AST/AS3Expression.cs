using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler.AST
{
    /// <summary>
    /// AS3表达式
    /// </summary>
    public class AS3Expression:IAS3SyntaxNode
    {
        private Token token; 
        public AS3Expression(Token token) {  this.token = token; }

        public Expr.AS3DataStackElement Value;

        /// <summary>
        /// 表达式代码步骤
        /// </summary>
        public List<Expr.AS3ExprStep> exprStepList;

        public Token Token
        { 
            get 
            { 
                return token; 
            }
        }



        public override string ToString()
        {
            
            return Value.ToString();
            
        }

        public void Write(int v, StringBuilder out_sb)
        {
            if (exprStepList != null && exprStepList.Count > 0)
            {
                for (int i = 0; i < exprStepList.Count; i++)
                {
                    out_sb.AppendLine("".PadLeft(v, '\t') + exprStepList[i].ToString());
                }
            }
            else
            { 
                out_sb.AppendLine( "".PadLeft(v,'\t') + Value.ToString());
            }
            //out_sb.AppendLine();
        }
    }
}
