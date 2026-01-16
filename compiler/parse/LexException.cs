using System;
using System.Runtime.Serialization;

namespace juicescript.compiler.parse
{
    public class LexException : Exception
    {
        //public string v;
        public int line;
        public int ptr;


        public LexException(string v, int cline, int linepos) : base(v)
        {
            line = cline;
            ptr = linepos;
        }

    }
}