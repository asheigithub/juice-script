using juicescript.compiler.AST.Expr;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace juicescript.compiler.AST.Stmt
{
    public abstract class AS3IterBlockBase :IAS3SyntaxNode
    {
		private Token token;
		public AS3IterBlockBase(Token token)
		{
			this.token = token;
		}

		public Token Token
		{
			get { return token; }
		}

		public string label;

		/// <summary>
		/// for in变量部分，可能是Variable或者是表达式列表
		/// </summary>
		public IAS3SyntaxNode ForArg;

		public AS3Expression ForInExpression;

		public List<IAS3SyntaxNode> Body = new List<IAS3SyntaxNode>();

        public AS3Variable HoldObjVar;

        public abstract void Write(int v, StringBuilder out_sb);
		
	}



    public sealed class AS3ForIn : AS3IterBlockBase
    {
		public AS3ForIn(Token token) : base(token)
		{
           
		}

		public override void Write(int v, StringBuilder out_sb)
        {
            ForInExpression.Write(v, out_sb);

            if (!string.IsNullOrEmpty(label))
            {
                out_sb.AppendLine("".PadLeft(v, '\t') + label + ":{");
                v = v + 1;
            }

            out_sb.Append("".PadLeft(v, '\t') + label + "for(");
            if (ForArg is AS3Variable)
            {
                out_sb.Append( ((AS3Variable)ForArg).Name + " in " );
            }
            else
            {
                out_sb.Append(((AS3Expression)ForArg).Value + " in ");
            }

            out_sb.AppendLine( ForInExpression.Value.ToString() + ")" );
            out_sb.AppendLine("".PadLeft(v, '\t') + "{");
            for (int i = 0; i < Body.Count; i++)
            {
                Body[i].Write(v+1, out_sb);
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
