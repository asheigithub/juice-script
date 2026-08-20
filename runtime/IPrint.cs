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
		interface IPrint
	{
		//public void Write(string message);

		public void WriteLine(ReadOnlySpan<char> chars);


		public void Write(ReadOnlySpan<char> chars);
	}

	internal class DefaultPrint : IPrint
	{
		public static DefaultPrint Instance = new DefaultPrint();

		public void Write(ReadOnlySpan<char> chars)
		{ 
			Console.Out.Write(chars);
		}

		//public void Write(string message)
		//{
		//	Console.Write(message);
		//}

		public void WriteLine(ReadOnlySpan<char> chars)
		{
			Console.Out.WriteLine(chars);
		}
	}


}
