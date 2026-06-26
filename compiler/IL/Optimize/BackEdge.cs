using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler.IL.Optimize
{
	/// <summary>
	/// 回边
	/// </summary>
	internal class BackEdge
	{
		public BasicBlock from;
		public BasicBlock to;
	}
}
