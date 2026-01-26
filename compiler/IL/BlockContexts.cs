using juicescript.ABC.INS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler.IL
{
	internal class ContextBase
	{ 
		
	}

	internal class TryCatchContext : ContextBase
	{
		public enum State
		{ 
			Try,
			Catch,
			Finally
		}

		public State state;

	}


	internal class Block : ContextBase
	{
		public string Label;
		public Token token;


		public bool ContinueTarget;
		public bool BreakTarget;

		public INS_Flag continueFlag;
		public INS_Flag breakFlag;


	}


}
