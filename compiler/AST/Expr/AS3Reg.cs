using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler.AST.Expr
{
    /// <summary>
    /// AS3表达式求值寄存器
    /// </summary>
    public class AS3Reg
    {
        public readonly int ID;

        public AS3Reg(int ID)
        {
            this.ID = ID;       
        }

        public bool isLd_R;
        public bool isLd_callee_id;
    }
}
