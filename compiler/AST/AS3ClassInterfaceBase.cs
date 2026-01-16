using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using juicescript.compiler.AST.Stmt;

namespace juicescript.compiler.AST
{
    public abstract class AS3ClassInterfaceBase : AS3MemberScope, IAS3SyntaxNode , IAS3ImportList
    {
        private Token token;
        public AS3ClassInterfaceBase(Token token,AS3SrcFile as3SrcFile)
        {
            this.token = token;
            this.as3SrcFile = as3SrcFile;
        }

        private List<AS3Use> _ns_set = new List<AS3Use>();
        public override List<AS3Use> UseNamespaceSet
        {
            get
            {
                return _ns_set;
            }
        }


        public Token Token { get { return token; } }

        public AS3Access Access = new AS3Access();

        public List<AS3Expression> Metas = new List<AS3Expression>();


        public string Name;

        public AS3Package Package { get { return as3SrcFile.Package; } }

        public List<string> ExtendsNames = new List<string>();

        
        public readonly AS3SrcFile as3SrcFile;

        /// <summary>
        /// 是否是包外类
        /// </summary>
        public bool IsOutPackage;


        public List<string> imports = new List<string>();
        public List<string> Imports
        {
            get { return imports; }
        }

        public abstract void Write(int v, StringBuilder out_sb);
        
    }
}
