using juicescript.compiler.AST.Stmt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler.AST
{
    /// <summary>
    /// 可定义AS3成员表的上下文
    /// </summary>
    public abstract class AS3MemberScope 
    {
        private List<AS3Member> members = new List<AS3Member>();
        public virtual  List<AS3Member> Members { get { return members ; } }

        public abstract List<AS3Use> UseNamespaceSet { get; } 


        public abstract string GetScopeName();

        private int closureid;

        public virtual string GetClosureId()
        { 
            return GetScopeName() + "/" + (closureid++);
        }

        private int regid;
        public virtual int NextRegId() 
        { 
            return regid++;
        }


        private int flagseed;

        public virtual int GetFlagId()
        {
            return flagseed++;
        }
    }
}
