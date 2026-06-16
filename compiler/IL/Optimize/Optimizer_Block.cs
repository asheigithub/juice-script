using juicescript.ABC;
using juicescript.ABC.INS;
using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler.IL.Optimize
{
	internal partial class Optimizer
	{

		struct TryInfo
		{
			public int tryid;
			/// <summary>
			/// 0--try
			/// 1--catch
			/// 2--finally
			/// </summary>
			public int trystate;
		}

		/// <summary>
		/// 获取某条指令所在的trycatchfinally结构
		/// </summary>
		/// <param name="instruction"></param>
		/// <param name="cfg"></param>
		/// <returns></returns>
		private static Stack<TryInfo> GetTryStmt(Instruction instruction, ControlFlowGraph cfg)
		{ 
			Stack<TryInfo> trystmt = new Stack<TryInfo>();

			for (int i = 0; i < cfg.Blocks.Count; i++)
			{
				var b = cfg.Blocks[i];

				for (int j = 0; j < b.Instructions.Count; j++)
				{
					var ins = b.Instructions[j];

					if (ins == instruction)
					{
						return trystmt;
					}
					else if (ins.INS_Code == INS_Code.try_enter)
					{
						trystmt.Push(new TryInfo() { tryid = ins.dst.index, trystate = 0 });
					}
					else if (ins.INS_Code == INS_Code.catch_enter)
					{
						TryInfo tryInfo = trystmt.Pop();
						tryInfo.trystate = 1;
						trystmt.Push(tryInfo);
					}
					else if (ins.INS_Code == INS_Code.finally_enter)
					{
						TryInfo tryInfo = trystmt.Pop();
						tryInfo.trystate = 2;
						trystmt.Push(tryInfo);
					}
					else if (ins.INS_Code == INS_Code.finally_exit)
					{
						trystmt.Pop();
					}
				}


			}



			return trystmt;
		}





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
			OptimizeStoreVar(basicBlock, cfg);


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
							!basicBlock.Instructions.Skip(i + 2).Any(ins => (ins.GetUse().Contains(instruction.dst) || ins.GetDef().Contains(instruction.dst)) && ins.INS_Code != INS_Code.expression_barrier)
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

							Array.Sort(batch, (a, b) =>
							{
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
								basicBlock.Instructions[i + j] = batch[j];
							}




							i += batch.Length;
						}
					}
				}


			}
		}


		private static void OptimizeBlockLdConst(ControlFlowGraph cfg)
		{
			if (cfg.Blocks.Count == 0)
				return;
			if (cfg.Method.Flags.HasFlag(MethodFlags.NeedActivation))
				return;

			
			//如果第一个基本块有Ld_Const,那么后续的就可以全部消除为move
			for (int i = 0; i < cfg.Blocks[0].Instructions.Count; i++)
			{
				if (cfg.Blocks[0].Instructions[i].INS_Code == INS_Code.ld_const)
				{
					INS_Ld_Const ld_Const = (INS_Ld_Const)cfg.Blocks[0].Instructions[i];
					for (int j = i+1; j < cfg.Blocks[0].Instructions.Count; j++)
					{
						if (cfg.Blocks[0].Instructions[j].INS_Code == INS_Code.ld_const
							&&
							((INS_Ld_Const)cfg.Blocks[0].Instructions[j]).const_index == ld_Const.const_index
							)
						{
							INS_Move _Move = new INS_Move(cfg.Blocks[0].Instructions[j].token );
							_Move.source = ld_Const.dst;
							_Move.dst = cfg.Blocks[0].Instructions[j].dst;

							cfg.Blocks[0].Instructions[j] = _Move;
						}
					}

					for (int k = 1; k < cfg.Blocks.Count; k++)
					{
						var block = cfg.Blocks[k];
						for (int j = 0; j < block.Instructions.Count; j++)
						{
							if (block.Instructions[j].INS_Code == INS_Code.ld_const
								&&
								((INS_Ld_Const)block.Instructions[j]).const_index == ld_Const.const_index
								)
							{
								INS_Move _Move = new INS_Move(block.Instructions[j].token);
								_Move.source = ld_Const.dst;
								_Move.dst = block.Instructions[j].dst;

								block.Instructions[j] = _Move;
							}
						}

					}

					for (int j = 0; j < i; j++)
					{
						if (cfg.Blocks[0].Instructions[j].MaybeRaiseError())
						{
							for (int k = i; k > j; k--)
							{
								cfg.Blocks[0].Instructions[k] = cfg.Blocks[0].Instructions[k - 1];
							}

							cfg.Blocks[0].Instructions[j] = ld_Const;
							break;
						}
					}

				}
				
			}

			
		}

		private static void OptimizeBlockAccessVariable(ControlFlowGraph cfg)
		{
			//基本块级优化变量读取。
			//如果这个方法不被闭包引用，同时也不是闭包 则变量除了赋值外不可能改变值
			if (!cfg.Method.Flags.HasFlag(MethodFlags.NeedActivation))
			{
				var m = cfg.Method.Body._link_codescope.Parent;
				while (m.Kind == CodeScopeKind.Method)
				{
					var pm = ((ASMethodBody)m.Container).Method;
					if (pm.Flags.HasFlag(MethodFlags.NeedActivation))
					{
						return;
					}

					m = pm.Body._link_codescope.Parent;

				}


				//传入参数优化。如果参数不会被赋值，而读取了多次，则将其中一次读取提前到入口，然后其他所有读取全部消除为move.
				for (int i = 0; i < cfg.Method.Body._link_codescope.Members.Count; i++)
				{
					var scopemember = cfg.Method.Body._link_codescope.Members[i];
					if (scopemember.Kind != ScopeMemberKind.Parameter)
					{
						break;
					}

					if (cfg.Blocks.SelectMany(b => b.Instructions)
						.Any(ins => ins.INS_Code == INS_Code.storeMethodVariable && ((INS_Store_MethodVariable)ins).heap.MemberIndex == i))
					{
						//有赋值，跳过
						continue;
					}

					var ldlist = cfg.Blocks.SelectMany(b => b.Instructions)
						.Where(ins => ins.INS_Code == INS_Code.ld_methodVariable && ((INS_Ld_MethodVariable)ins).heap.MemberIndex == i).ToList();

					if (ldlist.Count > 1)
					{
						StackLocater var_slot;

						if (cfg.Blocks[0].Instructions.Any(ld => ldlist.Contains(ld)))
						{
							var first_ldins = cfg.Blocks[0].Instructions.First(ld => ldlist.Contains(ld));
							ldlist.Remove(first_ldins);

							if (cfg.Blocks[0].Instructions.Take(cfg.Blocks[0].Instructions.IndexOf(first_ldins)).Any(ii => ii.MaybeRaiseError()))
							{
								//由于可能调函数抛出异常，所以需要提前！
								cfg.Blocks[0].Instructions.Remove(first_ldins);

								int idx = cfg.Blocks[0].Instructions.IndexOf(cfg.Blocks[0].Instructions.First(ii => ii.MaybeRaiseError()));

								cfg.Blocks[0].Instructions.Insert(idx, first_ldins);
							}

							var_slot = first_ldins.dst;

							foreach (var ld in ldlist)
							{
								foreach (var block in cfg.Blocks)
								{
									if (block.Instructions.Contains(ld))
									{
										int index = block.Instructions.IndexOf(ld);

										INS_Move _Move = new INS_Move(ld.token);
										_Move.source = var_slot;
										_Move.dst = ld.dst;

										block.Instructions[index] = _Move;
										goto lbl_nextld;
									}
								}

							lbl_nextld:
								;
							}
						}
						else
						{
							//非首块有，后续考虑。

						}

					}
				}

				//第一个基本块优化
				//如果某个变量只在第一个基本块被赋值一次，则后续所有读取都转为move
				//由于即使是缓存，这个变量也是缓存地址位置，只要不被多次赋值，后续就是安全的


				for (int i = 0; i < cfg.Method.Body._link_codescope.Members.Count; i++)
				{
					var scopemember = cfg.Method.Body._link_codescope.Members[i];
					if (scopemember.Kind == ScopeMemberKind.Parameter)
					{
						continue;
					}

					if (cfg.Blocks.SelectMany(b => b.Instructions)
						.Count(
						ins => (ins.INS_Code == INS_Code.storeMethodVariable && ((INS_Store_MethodVariable)ins).heap.MemberIndex == i)
						||
						(ins.INS_Code == INS_Code.ld_memberInitValue && ((INS_Ld_MemberInitValue)ins).heap.MemberIndex == i)
						) == 1)
					{
						var instruction = cfg.Blocks[0].Instructions.FirstOrDefault(ins => ins.INS_Code == INS_Code.storeMethodVariable && ((INS_Store_MethodVariable)ins).heap.MemberIndex == i);
						if (instruction != null)
						{
							INS_Store_MethodVariable store_MethodVariable = (INS_Store_MethodVariable)instruction;

							var ldlist = cfg.Blocks.Skip(1).SelectMany(b => b.Instructions)
								.Where(ins => ins.INS_Code == INS_Code.ld_methodVariable && ((INS_Ld_MethodVariable)ins).heap.MemberIndex == i).ToList();

							if (ldlist.Count > 1)
							{
								
								/*
									* function throwErr()
									{
										throw 3;
									}
									function G()
									{
										try 
										{
											var k;
											throwErr();        //考虑此代码，需要先在基本块顶部，预先加载store_MethodVariable.convertedloc 的初始值
											k = {};
		
										}
										finally
										{
											trace(k);
											trace(k);
										}
									}
									G();
									*/

								var head= cfg.Blocks[0].Instructions.TakeWhile(i => i != instruction);
								if (head.Any(i => i.MaybeRaiseError()))
								{
									//如果指令在try内
									var trystack = GetTryStmt(instruction, cfg);
									if (trystack.Count > 0)
									{
										foreach (var ld in ldlist)
										{ 
											var ldtry = GetTryStmt(ld, cfg);

											if (ldtry.Any(t => trystack.Any(tt => tt.tryid == t.tryid)))
											{
												var ld_info = ldtry.First(t => trystack.Any(tt => tt.tryid == t.tryid));
												var ins_info = trystack.First(t=>t.tryid == ld_info.tryid);


												if (ld_info.trystate != ins_info.trystate)
												{
													INS_Ld_MethodVariable iNS_Ld = new INS_Ld_MethodVariable(instruction.token);
													iNS_Ld.dst = store_MethodVariable.convertedloc;
													//iNS_Ld.heap = store_MethodVariable.heap;
													ScopeHeapLocater heapLocater = default;
													heapLocater.ScopeIndex = (ushort)cfg.Method.Body._link_codescope.index;
													heapLocater.MemberIndex = store_MethodVariable.heap.MemberIndex;
													iNS_Ld.heap = heapLocater;

													cfg.Blocks[0].Instructions.Insert(0, iNS_Ld);

													break;
												}
												
											}

										}
									}
								}

								

								StackLocater var_slot = store_MethodVariable.convertedloc;
								foreach (var ld in ldlist)
								{
									foreach (var block in cfg.Blocks)
									{
										if (block.Instructions.Contains(ld))
										{
											int index = block.Instructions.IndexOf(ld);

											INS_Move _Move = new INS_Move(ld.token);
											_Move.source = var_slot;
											_Move.dst = ld.dst;

											block.Instructions[index] = _Move;
											goto lbl_nextld;
										}
									}

								lbl_nextld:
									;
								}
							}

						}
					}
				}






				for (int i = 0; i < cfg.Blocks.Count; i++)
				{
					//基本块内优化
					var block = cfg.Blocks[i];

					for (int j = 0; j < block.Instructions.Count; j++)
					{
						var instruction = block.Instructions[j];
						if (instruction.INS_Code == INS_Code.ld_methodVariable)
						{
							INS_Ld_MethodVariable ld_MethodVariable = (INS_Ld_MethodVariable)instruction;

							var next = block.Instructions.Skip(j + 1).FirstOrDefault(ii => ii.INS_Code == INS_Code.ld_methodVariable &&
							 ((INS_Ld_MethodVariable)ii).heap.MemberIndex == ld_MethodVariable.heap.MemberIndex

							);

							if (next != null)
							{
								int next_index = block.Instructions.IndexOf(next);
								if (block.Instructions.Skip(j + 1).Take(next_index - j).Any(ii => ii.INS_Code == INS_Code.storeMethodVariable
									 ||
									 ii.INS_Code == INS_Code.ld_MethodVariableInitValue //由于这里可能有GC缓存对象问题，所以只能严格判断:如果出现对variable的赋值则可能导致缓存对象改变
									 ||
									 ii.GetDef().Contains(ld_MethodVariable.dst) //如果修改了取出来的值
									 )
								)
								{
									continue;
								}

								INS_Move _Move = new INS_Move(next.token);
								_Move.source = ld_MethodVariable.dst;
								_Move.dst = next.dst;

								block.Instructions[next_index] = _Move;
								j--;
							}

						}
						else if (instruction.INS_Code == INS_Code.storeMethodVariable)
						{
							INS_Store_MethodVariable store_MethodVariable = (INS_Store_MethodVariable)instruction;
							var next = block.Instructions.Skip(j + 1).FirstOrDefault(ii => ii.INS_Code == INS_Code.ld_methodVariable &&
							 ((INS_Ld_MethodVariable)ii).heap.MemberIndex == store_MethodVariable.heap.MemberIndex

							);
							if (next != null)
							{
								int next_index = block.Instructions.IndexOf(next);
								if (block.Instructions.Skip(j + 1).Take(next_index - j).Any(ii => ii.INS_Code == INS_Code.storeMethodVariable
									 ||
									 ii.INS_Code == INS_Code.ld_MethodVariableInitValue //由于这里可能有GC缓存对象问题，所以只能严格判断:如果出现对variable的赋值则可能导致缓存对象改变
									 ||
									 ii.GetDef().Contains(store_MethodVariable.dst) //如果修改了取出来的值
									 )
								)
								{
									continue;
								}

								INS_Move _Move = new INS_Move(next.token);
								_Move.source = store_MethodVariable.convertedloc;
								_Move.dst = next.dst;

								block.Instructions[next_index] = _Move;
								j--;
							}
						}

					}
				}
			}






		}



		private static void RemoveBlockMove(ControlFlowGraph cfg)
		{

			//查找每条指令的每个use
			//设有use [A],[A]的来源都是move 
			// 如果move只有一条，对所有使用[A]的instruction(可能有多个，可能来自switch): 该指令没有使用move.的source,   则把A改成move.source,删除move
			//
			// 如果move有多条 （） 这些move，每个move的来源指令只有一条 move的来源没有被其他地方使用
			//     将这些move来源的dst修改为[A]
			//     移除这些move
			// 迭代直到找不到

			List<Instruction> ping_moves = new List<Instruction>();

			bool flag;
			int i = 0;
			int j = 0;
			do
			{

				flag = false;
				var allins = cfg.Blocks.SelectMany(l => l.Instructions).Where(l => l.INS_Code != INS_Code.expression_barrier);

				if (!allins.Any(i => i.INS_Code == INS_Code.move && !ping_moves.Contains(i)))
					break;


				for (; i < cfg.Blocks.Count; i++)
				{
					var block = cfg.Blocks[i];

					for (; j < block.Instructions.Count; j++)
					{
						var instruction = block.Instructions[j];
						if (instruction.INS_Code == INS_Code.expression_barrier)
							continue;

						var uselist = instruction.GetUse();
						for (int k = 0; k < uselist.Count; k++)
						{
							var A = uselist[k];

							var sources = allins.Where(ins => ins.GetDef().Contains(A)).ToList();
							if (sources.Count > 0 && sources.All(ins => ins.INS_Code == INS_Code.move)
								&&
								sources.All(ins => allins.Count(ii => ii.GetDef().Contains(((INS_Move)ins).source) && ii.GetDef().Count == 1) == 1)
								)
							{
								if (sources.Count == 1)
								{
									var toreplace = allins.Where(ii => ii.GetUse().Contains(sources[0].dst)).ToArray();
									if (!toreplace.Any(ii => ii.GetUse().Contains(((INS_Move)sources[0]).source)))
									{

										foreach (var item in toreplace)
										{
											item.RemappingSlots(new Dictionary<int, int>() { { sources[0].dst.index, ((INS_Move)sources[0]).source.index } });
										}

										flag = true;
										var toremove = sources.ToArray();
										foreach (var b in cfg.Blocks)
										{
											b.Instructions.RemoveAll(p => toremove.Contains(p));
										}

										goto lbl_continue;
									}
									else
									{
										ping_moves.AddRange(sources);
									}

								}
								else
								{
									var otheruse = sources.SelectMany(mv => allins.Where(ii => ii != mv && ii.GetUse().Contains(((INS_Move)mv).source))).ToArray();

									if (otheruse.Length == 0)
									{
										//修改指令目标
										var movelist = sources.Select(mv => allins.First(ii => ii.GetDef().Contains(((INS_Move)mv).source))).ToList();

										foreach (var v in movelist)
										{
											v.RemappingSlots(new Dictionary<int, int>() { { v.GetDef()[0].index, A.index } });
										}

										flag = true;
										var toremove = sources.ToArray();
										foreach (var b in cfg.Blocks)
										{
											b.Instructions.RemoveAll(p => toremove.Contains(p));
										}

										goto lbl_continue;
									}
									else
									{
										ping_moves.AddRange(sources);
									}

								}
							}
						}
					}
					j = 0;
				}

			lbl_continue:
				;

			} while (flag);
		}


	}
}
