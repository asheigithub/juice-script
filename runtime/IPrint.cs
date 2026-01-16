using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.runtime
{
	public interface IPrint
	{
		public void Write(string message);

		public void WriteLine(string message);
	}

	internal class DefaultPrint : IPrint
	{
		public static DefaultPrint Instance = new DefaultPrint();

		public void Write(string message)
		{
			Console.Write(message);
		}

		public void WriteLine(string message)
		{
			Console.WriteLine(message);
		}
	}


}
