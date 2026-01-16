using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler.AST
{
    public sealed class AS3Interface : AS3ClassInterfaceBase
    {
        public AS3Interface(Token token, AS3SrcFile as3SrcFile) : base(token, as3SrcFile)
        {
        }

        public override string GetScopeName()
        {
            return Package.Name + "." + Name;
        }

        public override void Write(int v, StringBuilder out_sb)
        {
            for (int i = 0; i < Metas.Count; i++)
            {
                out_sb.AppendLine("".PadLeft(v, '\t') + "[" + Metas[i].exprStepList[0].Arg2.Data.Value + "(" + Metas[i].exprStepList[0].Arg3 + ")]");
            }
            Access.Write(v, out_sb);

            out_sb.Append("interface " + Name);
            if (ExtendsNames.Count > 0)
            {
                out_sb.Append(" extends " + string.Join(',', ExtendsNames));
            }
            

            out_sb.AppendLine();

            out_sb.AppendLine("".PadLeft(v, '\t') + "{");

            for (int i = 0; i < imports.Count; i++)
            {
                out_sb.AppendLine("".PadLeft(v + 1, '\t') + "import " + imports[i] + ";");
            }

            if (Members.Count > 0)
            {
                out_sb.AppendLine();

                for (int i = 0; i < Members.Count; i++)
                {
                    Members[i].Write(v + 1, out_sb);
                }
            }

            out_sb.AppendLine("".PadLeft(v, '\t') + "}");
        }
    }
}
