using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler.AST.Stmt
{
    public sealed class AS3ForEach : AS3IterBlockBase
    {
        
        public AS3ForEach(Token token):base(token) { }
        public override void Write(int v, StringBuilder out_sb)
        {
            ForInExpression.Write(v, out_sb);

            if (!string.IsNullOrEmpty(label))
            {
                out_sb.AppendLine("".PadLeft(v, '\t') + label + ":{");
                v = v + 1;
            }

            out_sb.Append("".PadLeft(v, '\t') + label + "for each(");
            if (ForArg is AS3Variable)
            {
                out_sb.Append(((AS3Variable)ForArg).Name + " in ");
            }
            else
            {
                out_sb.Append(((AS3Expression)ForArg).Value + " in ");
            }

            out_sb.AppendLine(ForInExpression.Value.ToString() + ")");
            out_sb.AppendLine("".PadLeft(v, '\t') + "{");
            for (int i = 0; i < Body.Count; i++)
            {
                Body[i].Write(v + 1, out_sb);
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
