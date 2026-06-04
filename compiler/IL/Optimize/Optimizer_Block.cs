using juicescript.ABC;
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
		internal static void OptimizeStoreVar(BasicBlock basicBlock, ControlFlowGraph cfg)
		{
			//查找INS_Store_MethodVariable 。 将额外信息编码进ScopeId里。
			{
				for (int i = 0; i < basicBlock.Instructions.Count; i++)
				{
					var instruction = basicBlock.Instructions[i];
					if (instruction.INS_Code == INS_Code.storeMethodVariable)
					{
						INS_Store_MethodVariable store_MethodVariable = (INS_Store_MethodVariable)instruction;

						var scopeMember = cfg.Method.Body._link_codescope.Members[store_MethodVariable.heap.MemberIndex];

						bool encodetypekind = false;
						TypeKind typeKind;
						if (scopeMember.Kind == ScopeMemberKind.Parameter)
						{
							encodetypekind = scopeMember.TypeKind <= TypeKind.Namespace;
							typeKind = scopeMember.TypeKind;
						}
						else
						{
							ASTrait t = scopeMember.trait;
							encodetypekind = t.TypeKind <= TypeKind.Namespace;
							typeKind = t.TypeKind;
						}

						if (!encodetypekind)
						{
							typeKind = (TypeKind)0xff;
						}

						var _heaplocater = store_MethodVariable.heap;
						_heaplocater.ScopeIndex = (byte)typeKind;

						store_MethodVariable.heap = _heaplocater;


					}

				}
			}
		}


		private static void OptimizeBlock(BasicBlock basicBlock, ControlFlowGraph cfg, NaNBoxing[] constants)
		{
			OptimizeStoreVar(basicBlock,cfg);


			//查找ld_MultiNameL_Ref,再查找后续是否是把值保存到引用里。如果是，并且中间没有使用这个引用，则把指令移动到保存指令前面,然后合并为直接存值指令
			{
				for (int i = 0; i < basicBlock.Instructions.Count; i++)
				{
					var instruction = basicBlock.Instructions[i];
					if (instruction.INS_Code == ABC.INS.INS_Code.ld_MultiNameL_Ref)
					{
						var store = basicBlock.Instructions.Skip(i + 1).FirstOrDefault(
							(s) => s.INS_Code == INS_Code.storeHeapValueRef && s.dst.index == instruction.dst.index
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

							INS_Store_MultiNameL store_MultiNameL = new INS_Store_MultiNameL(store.token);
							store_MultiNameL.dst = ((INS_Store_HeapValueRef)store).source;
							store_MultiNameL.instance = ((INS_Ld_MultiNameL_Ref)instruction).instance;
							store_MultiNameL.name = ((INS_Ld_MultiNameL_Ref)instruction).name;
							store_MultiNameL.super_type_index = ((INS_Ld_MultiNameL_Ref)instruction).super_type_index;
							store_MultiNameL.tmp_holder = ((INS_Ld_MultiNameL_Ref)instruction).dst;


							basicBlock.Instructions.Insert(j, store_MultiNameL);

							basicBlock.Instructions.Remove(store);

						}

					}
				}
			}

			//查找ld_MultiNameL_Ref,在查找后面是否是Ld_ValueRef 。合并为直接读取值,并且move结果
			{
				List<Instruction> toremove = new List<Instruction>();
				for (int i = 0; i < basicBlock.Instructions.Count - 1; i++)
				{
					var instruction = basicBlock.Instructions[i];
					var next = basicBlock.Instructions[i + 1];
					if (instruction.INS_Code == ABC.INS.INS_Code.ld_MultiNameL_Ref && next.INS_Code == INS_Code.ld_ValueRef)
					{
						if (((INS_Ld_ValueRef)next).source.index == instruction.dst.index	
							&&
							! basicBlock.Instructions.Skip(i+2).Any( ins=>(ins.GetUse( ).Contains( instruction.dst ) || ins.GetDef().Contains(instruction.dst)  ) && ins.INS_Code != INS_Code.expression_barrier  )
							)
						{
							toremove.Add(next);

							//mapping.Add(next.dst.index, instruction.dst.index);

							INS_Ld_MultiNameL_Val ld_MultiNameL_Val = new INS_Ld_MultiNameL_Val(instruction.token);
							ld_MultiNameL_Val.dst = next.dst;
							ld_MultiNameL_Val.instance = ((INS_Ld_MultiNameL_Ref)instruction).instance;
							ld_MultiNameL_Val.name = ((INS_Ld_MultiNameL_Ref)instruction).name;
							ld_MultiNameL_Val.super_type_index = ((INS_Ld_MultiNameL_Ref)instruction).super_type_index;

							basicBlock.Instructions[i] = ld_MultiNameL_Val;

							//INS_Move move = new INS_Move(next.token);
							//move.dst = next.dst;
							//move.source = instruction.dst;

							//basicBlock.Instructions[i+1]=move;

						}

						
					}
				}

				if (toremove.Count > 0)
				{
					basicBlock.Instructions.RemoveAll(r => toremove.Contains(r));
					//foreach (var ins in basicBlock.Instructions)
					//{
					//	ins.RemappingSlots(mapping);
					//}
				}
			}










			//查找连续的Ld_MethodVar 。如果其中有个变量重复读了2次,改为move(因为这里有可能跨块，所以不能简单地删除!)
			{
				Dictionary<Instruction, Instruction> toreplace = new Dictionary<Instruction, Instruction>();
				for (int i = 0; i < basicBlock.Instructions.Count; i++)
				{
					var instruction = basicBlock.Instructions[i];
					if (instruction.INS_Code == INS_Code.ld_methodVariable)
					{
						var batch = basicBlock.Instructions.Skip(i).TakeWhile(ins => ins.INS_Code == INS_Code.ld_methodVariable).ToArray();
						if (batch.Length > 1)
						{
							var groups = batch.GroupBy(l => ((INS_Ld_MethodVariable)l).heap);
							foreach (var g in groups)
							{
								var l = g.First();
								foreach (var other in g.Skip(1))
								{
									INS_Move move = new INS_Move(other.token);
									move.dst = other.dst;
									move.source = l.dst;

									toreplace.Add(other, move);
								}
							}

							for (int j = 0; j < batch.Length; j++)
							{
								if (toreplace.ContainsKey(batch[j]))
								{
									batch[j] = toreplace[batch[j]];
								}
							}

							Array.Sort(batch, (a, b) => {
								if (a.INS_Code == b.INS_Code)
								{
									return 0;
								}
								else
								{
									if (a.INS_Code == INS_Code.move)
										return 1;
									else
										return -1;
								}
							
							});


							for (int j = 0; j < batch.Length; j++)
							{
								basicBlock.Instructions[i+j] = batch[j]; 
							}




							i += batch.Length;
						}
					}
				}

				
			}
		}






		private static void RemoveBlockMove(ControlFlowGraph cfg)
		{
			//如果move后的结果只有一个地方用，
			//并且只有一个move的目标是dst (也就是这不是三元运算符)
			//则直接使用move前的slot,然后移除move
			for (int i = 0;i< cfg.Blocks.Count ; i++)
			{
				var block = cfg.Blocks[i];
				List<Instruction> toremove = new List<Instruction>();
				
				for (int j = 0; j < block.Instructions.Count; j++)
				{
					var instruction = block.Instructions[j];
					if (instruction.INS_Code == INS_Code.move)
					{
						var dst = instruction.dst;

						if (cfg.Blocks.SelectMany(b => b.Instructions).Count(ins => ins.dst.index == dst.index && ins.INS_Code == INS_Code.move) == 1)
						{

							var useins = cfg.Blocks.SelectMany(b => b.Instructions).Where(ins => ins.GetUse().Contains(dst)
								&& !ins.GetUse().Contains(((INS_Move)instruction).source)

								).ToArray();
							if (useins.Length == 1)
							{
								Dictionary<int, int> map = new Dictionary<int, int>
								{
									{ dst.index, ((INS_Move)instruction).source.index }
								};

								useins[0].RemappingSlots(map);


								toremove.Add(instruction);
							}
						}
					}
				}
				if (toremove.Count > 0)
				{
					block.Instructions.RemoveAll(r => toremove.Contains(r));
				}

			}
		}


	}
}
