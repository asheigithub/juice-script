using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler
{
    public abstract class CompilerException : Exception
    {
        public CompilerException(string message) : base(message)
        {
        }
    }
}
