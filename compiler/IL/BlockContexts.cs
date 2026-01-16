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
		//public Instruction Try_Enter;
		//public Instruction Try_Exit;

		//public Instruction Finally_Enter;
		//public Instruction Finally_Exit;

		//public List<Tuple<Instruction, Instruction>> CatchList;


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
