using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler
{
    public class ResolverException : CompilerException
    {
        public Token token;

        public ResolverException(Token token, string message) : base(message)
        {
            this.token = token;
        }

        public override string ToString()
        {
            return token.sourceFileFullPath + ":" + (token.line + 1) + ":Error: " + Message;
        }

    }
}
