using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler.AST.Expr
{
    /// <summary>
    /// 表达式的值
    /// </summary>
    public class AS3DataValue
    {
        public FF1DataValueType FF1Type;

        public Token token;

        public AS3DataValue(Token token)
        { 
            this.token = token;
        }


        private object _value;
        public object Value
        {
            get { return _value; }

            set 
            { 
                _value = value; 
            }
        }

    }
}
