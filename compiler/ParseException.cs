using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler
{
	internal class ParseErrOut : TextWriter
	{
		public override Encoding Encoding => Encoding.UTF8 ;

        StringBuilder buffer = new StringBuilder ();

		public override void WriteLine(string value)
		{
			base.WriteLine(value);

            buffer.AppendLine(value);

		}

        public string BuffData
        {
            get
            { 
                return buffer.ToString ();
            }
        }


	}

	public class ParseException : CompilerException
    {
        public ParseException(string message) : base(message)
        {
            
        }

        public override string ToString()
        {
            return Message;
        }

    }
}
