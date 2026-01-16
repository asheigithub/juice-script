using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.runtime
{
	public class EvalConstException : RuntimeException
	{
		public EvalConstException() : base("Parameter initializer unknown or is not a compile-time constant.")
		{
		}
	}
}
