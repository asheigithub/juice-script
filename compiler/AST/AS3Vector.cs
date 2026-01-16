using juicescript.compiler.AST.Expr;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler.AST
{
    public sealed class AS3Vector
    {
        /// <summary>
        /// 是否构造文法 new &lt;T>[E0, ..., En-1 ,]; 
        /// </summary>
        public bool isInitData;

        public string VectorTypeStr;

        public AS3DataStackElement Constructor;


        public override string ToString()
        {
            return "Vector.<" + VectorTypeStr + ">";
        }

    }
}
