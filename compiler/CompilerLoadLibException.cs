using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler
{
    public class CompilerLoadLibException : CompilerException
    {
        public CompilerLoadLibException(string message) : base(message)
        {
        }

        public override string ToString()
        {
            return Message;
        }

    }
}
