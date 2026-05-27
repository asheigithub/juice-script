using juicescript.ABC.INS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler.IL.Optimize
{
	internal partial class Optimizer
	{
		private static void OptimizeBlock(BasicBlock basicBlock)
		{

			//查找ld_MultiNameL_Ref,再查找后续是否是把值保存到引用里。如果是，并且中间没有使用这个引用，则把指令移动到保存指令前面
			for (int i = 0; i < basicBlock.Instructions.Count; i++) 
			{ 				
				var instruction = basicBlock.Instructions[i];
				if (instruction.INS_Code == ABC.INS.INS_Code.ld_MultiNameL_Ref)
				{
					var store = basicBlock.Instructions.Skip(i+1).FirstOrDefault(
						(s)=>s.INS_Code == INS_Code.storeHeapValueRef && s.dst.index == instruction.dst.index						
						);

					if (store != null)
					{
						int k = basicBlock.Instructions.IndexOf(store);
						if (basicBlock.Instructions.Skip(i).Take(k - i).Any((ins) => ins.GetUse().Contains(instruction.dst))) 
						{
							continue;
						}

						basicBlock.Instructions.RemoveAt(i);
						int j = basicBlock.Instructions.IndexOf(store);
						basicBlock.Instructions.Insert(j, instruction);
					}

				}
			}




		}

	}
}
