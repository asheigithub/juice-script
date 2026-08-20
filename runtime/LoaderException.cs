using juicescript.ABC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.runtime
{
#if FORCOMPILER
	internal
#else
    public
#endif
		class LoaderException : RuntimeException
    {
        public LoaderException(string message) : base( message )
        {
        }

        public Token Token;

    }
}
