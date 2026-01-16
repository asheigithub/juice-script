using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using juicescript.compiler.AST.Expr;

namespace juicescript.compiler.AST
{
    public abstract class AS3Member : IAS3SyntaxNode
    {
        private Token token = null;
        public AS3Member(Token token) { this.token = token; }

        public string Name { get; set; }

        public Token Token { get { return token; } }

        public string TypeStr { get; set; }

        public AS3Access Access = new AS3Access();

        public List<AS3Expression> Metas = new List<AS3Expression>();

        public virtual void Write(int v, StringBuilder out_sb)
        {
            for (int i = 0; i < Metas.Count; i++)
            {
                var meta = Metas[i];
                if (meta.exprStepList.Count == 1)
                {
                    out_sb.AppendLine("".PadLeft(v, '\t') + "[" + Metas[i].exprStepList[0].Arg2.Data.Value + "(" + Metas[i].exprStepList[0].Arg3 + ")]");
                }
                else if (meta.exprStepList.Count == 0)
                {
                    out_sb.AppendLine("".PadLeft(v, '\t') + "[" + ((List<AS3DataStackElement>)meta.Value.Data.Value)[0] + "]");
                }
                else
                {
                    out_sb.Append("".PadLeft(v, '\t') + "[" + meta.exprStepList[meta.exprStepList.Count - 1].Arg2.Data.Value + "(");
                    for (int j = 0; j < meta.exprStepList.Count - 1; j++)
                    {
                        out_sb.Append(meta.exprStepList[j].Arg1.Data.Value.ToString() + " = " + meta.exprStepList[j].Arg2.Data.Value.ToString());
                        if (j < meta.exprStepList.Count - 2)
                        {
                            out_sb.Append(",");
                        }
                    }
                    out_sb.AppendLine(")]");
                }

            }
            Access.Write(v, out_sb);

        }

    }
}
