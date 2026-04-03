using juicescript.ABC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.runtime
{
	public class EvalConstOutOfStackException : RuntimeException
	{
		public EvalConstOutOfStackException(ASMethod method) : base( $" { Player.GetMethodKey( method)} use too much stackslots. Compute var default value failed." )
		{
		}
	}
}
