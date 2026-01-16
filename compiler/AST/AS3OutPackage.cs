using juicescript.compiler.AST.Expr;
using juicescript.compiler.AST.Stmt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler.AST
{
    /// <summary>
    /// 包外代码
    /// </summary>
    public sealed class AS3OutPackage : AS3MemberScope , IAS3ImportList
    {
        public AS3SrcFile AS3SrcFile;

       
        public List<AS3ClassInterfaceBase> outpackage_classes_interfaces = new List<AS3ClassInterfaceBase>();
        //public List<AS3Interface> outpackage_interfaces = new List<AS3Interface>();

        public List<IAS3SyntaxNode> Codes = new List<IAS3SyntaxNode>();


        public AS3OutPackage(AS3SrcFile aS3SrcFile)
        {
            AS3SrcFile = aS3SrcFile;
        }

        public List<string> imports = new List<string>();
        public List<string> Imports
        {
            get { return imports; }
        }

        private List<AS3Use> _ns_set = new List<AS3Use>();
        public override List<AS3Use> UseNamespaceSet
        {
            get
            {
                return _ns_set;
            }
        }



        internal void Write(int v, StringBuilder out_sb)
        {
            for (int i = 0; i < imports.Count; i++)
            {
                out_sb.AppendLine("".PadLeft(v , '\t') + "import " + imports[i] + ";");
            }
            for (int i = 0; i < UseNamespaceSet.Count; i++)
            {
                out_sb.AppendLine("".PadLeft(v, '\t') + "use namespace " + UseNamespaceSet[i].UseNameSpace + ";");
            }

            for (int i = 0; i < outpackage_classes_interfaces.Count; i++)
            {
                outpackage_classes_interfaces[i].Write(v, out_sb);
            }

            
            

            for (int i = 0; i < Members.Count; i++)
            {
                Members[i].Write(v,out_sb);
                out_sb.AppendLine();
            }


            for (int i = 0; i < Codes.Count; i++)
            {
                Codes[i].Write(v, out_sb);
                out_sb.AppendLine();
            }

        }

        public override string GetScopeName()
        {
            return  "FilePrivateNS:" + (string.IsNullOrEmpty(AS3SrcFile.sourceFile)?AS3SrcFile.key.ToString(): AS3SrcFile.sourceFile);
        }
    }
}
