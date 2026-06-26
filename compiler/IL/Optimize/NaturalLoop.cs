using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler.IL.Optimize
{
	/// <summary>
	/// 自然循环
	/// </summary>
	internal class NaturalLoop
	{
		public List<BackEdge> backedges;
		public List<BasicBlock> nodes;
		public BasicBlock firstNode
		{
			get
			{
				return backedges[0].to;
			}
		}


	}
}
