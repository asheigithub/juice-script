using juicescript.compiler.AST.Stmt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler.AST
{
    public sealed class AS3Package : IAS3ImportList
    {
        public string Name;

        public AS3SrcFile AS3File;


        public AS3Class MainClass;
        public AS3Interface MainInterface;
        public AS3NameSpace MainNamespace;

        //public List<IAS3SyntaxNode> Codes = new List<IAS3SyntaxNode>();

        public class PackageMemberScope : AS3MemberScope
        {
            private AS3OutPackage script;
            public PackageMemberScope(AS3OutPackage script)
            {
                this.script = script;
            }

            public override List<AS3Member> Members
            {
                get
                { 
                    return script.Members;
                }
            }

            private List<AS3Use> _ns_set = new List<AS3Use>();
            public override List<AS3Use> UseNamespaceSet
            {
                get
                {
                    return _ns_set;
                }
            }



            public override string GetClosureId()
            {
                return script.GetClosureId();
            }

            public override int GetFlagId()
            {
                return script.GetFlagId();
            }

            public override int NextRegId()
            {
                return script.NextRegId();
            }

            public override string GetScopeName()
            {
                return script.GetScopeName();
            }
        }

        public PackageMemberScope MemberScope;


        public AS3Package(AS3SrcFile src)
        { 
            AS3File = src;
            MemberScope = new PackageMemberScope(src.OutPackage);

        }

        public List<string> imports = new List<string>();
        public List<string> Imports
        { 
            get { return imports; }
        }

        internal void Write(int v, StringBuilder out_sb)
        {
            out_sb.AppendLine( "".PadLeft(v, '\t') + "package " + Name );
            out_sb.AppendLine("".PadLeft(v, '\t') + "{".PadLeft(v, '\t'));

            for (int i = 0; i < imports.Count; i++)
            {
                out_sb.AppendLine("".PadLeft(v+1, '\t') + "import " +  imports[i] + ";" );
            }
            for (int i = 0; i < MemberScope.UseNamespaceSet.Count ; i++)
            {
                out_sb.AppendLine("".PadLeft(v + 1, '\t') + "use namespace " + MemberScope.UseNamespaceSet[i].UseNameSpace + ";");
            }


            if (MainClass != null)
            {
                MainClass.Write(v + 1, out_sb);
            }
            if (MainInterface != null)
            {
                MainInterface.Write(v + 1, out_sb);
            }
            if (MainNamespace != null)
            { 
                MainNamespace.Write(v + 1, out_sb);
            }


            //for (int i = 0; i < MemberScope.Members.Count; i++)
            //{
            //    MemberScope.Members[i].Write(v+1, out_sb);
            //    out_sb.AppendLine();
            //}
            //for (int i = 0; i < Codes.Count; i++)
            //{
            //    Codes[i].Write(v + 1, out_sb);
            //    out_sb.AppendLine();
            //}


            out_sb.AppendLine("".PadLeft(v, '\t') + "}");

        }
    }
}
