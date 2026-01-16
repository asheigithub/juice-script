using System;
using System.Runtime.Serialization;

namespace juicescript.compiler.parse
{
    [Serializable]
    public class SyntaxException : CompilerException
    {
        public Token token;

        public SyntaxException(Token matchedToken, string v):base(v)
        {
            this.token = matchedToken;
            
        }

        public override string ToString()
        {
            return (token.sourceFileFullPath == null ? token.sourceFile : token.sourceFileFullPath) + ":" + (token.line + 1) + ":Syntax error: " + Message;
        }

    }
}