using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler
{
    internal class FileException : CompilerException
    {
        public FileException(string file) : base("Error: " + file + ":No such file.")
        {

        }
    }
}
