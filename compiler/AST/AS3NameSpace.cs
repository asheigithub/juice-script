using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler.AST
{
    public sealed class AS3NameSpace : AS3Member
    {
        public AS3NameSpace(Token token) : base(token)
        {
            URI = string.Empty;
        }

        public string URI;

        public override void Write(int v, StringBuilder out_sb)
        {
            base.Write(v, out_sb);
            out_sb.Append("namespace " + Name);

            if (!string.IsNullOrEmpty(URI))
            {
                out_sb.Append(" = '" + URI + "'");
            }

            out_sb.AppendLine(";");

        }

    }
}
