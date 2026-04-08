using juicescript.ABC;
using juicescript.ABC.INS;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace juicescript.compiler.IL.Optimize
{
	/// <summary>
	/// 控制流图(Control Flow Graph, CFG)构建器
	/// 算法说明:
	/// 1 分割基本块。
	///   编译器确保除非没有任何指令，否则最后一条指令肯定是 END.
	///   这条END指令是唯一的退出指令，它作为独立的最后一个基本块。先检查指令序列是否符合此约束，不符合就抛出异常。
	///   基本块分割：跳转指令有：goto,if_true_goto,if_false_goto,iter_get,iter_next, throw error, return [value].
	///              先确定它们的跳转目标。goto类指令的跳转目标是查找某个 INS_flag的 flagid.
	///              throw error的跳转目标是：如果不在 try catch finally 结构内，则直接跳转到 END。
	///                                      如果在 try catch 内,则跳转到每一个对应的catch_enter.如果没有catch,则跳转到finally_enter.
	///                                      如果在 finally内，则跳转到finally_exit.
	///              return 类指令的跳转目标是：如果不在 try catch finally 结构内，直接跳到 END。
	///                                      如果在 try catch 内,则跳转到每一个对应的catch_enter.如果没有catch,则跳转到finally_enter.
	///                                      如果在 finally内，则跳转到finally_exit.
	///              基本块的入口，包括编号为0的第一条指令，跳转类指令的跳转目标，跳转类指令的下一条指令,和 try_enter,catch_enter,finally_enter,finally_exit.
	///              确定入口后，按照指令序列，从入口指令开始（含入口指令）到下一条入口指令（不含下一条入口指令）之间的指令序列归为一个基本块。
	///              特殊情况：END指令自己就是最后一个基本块.finally_exit自己也是一个基本块.
	///              
	/// 2 计算控制流
	///    编译器确保任何try_enter,try_exit都有一个匹配的finally_enter,finally_exit.即使脚本代码没有finally块，也会生成一个空的finally_enter,finally_exit对。
	///    由于有END块存在，控制流图一定有唯一的出口就是END块。
	///    常规goto,if_XX_goto,iter_get,iter_next的跳转算法和经典算法一致，每个基本块都会连接到它每个可能的后续块。
	///    按如下描述处理指令队列：
	///        首先查找指令队列中没有嵌套其他try catch finally 结构的 try catch finally 基本块序列，即从 try_enter开始，到finally_exit结束的基本块序列，它们作为一个子控制流
	///        这个子控制流也满足唯一入口(try_enter),唯一出口(finally_exit)。对这个子控制流计算控制流图。
	///         不可能出现if_XX_goto的目标不在子控制流的情况。如出现抛出编译异常。
	///         goto 指令，如果跳出子控制流，则 如果在try ,catch内，连接到finally_enter,如果在finally内，连接到 finally_exit.
	///         if_XX_goto,iter_get,iter_next,  连接跳转目标块和直接后续块。它不可能跨try,catch,finally块。
	///         throw error, 如果在try内,连接到每个catch_enter. 如果没有catch块，或者就在catch内，直接连接到finally_enter. 如果在finally块内，直接跳到finally_exit指令。
	///         return 指令, 如果在try内,连接到每个catch_enter. 如果没有catch块，或者就在catch内，直接连接到finally_enter. 如果在finally块内，直接跳到finally_exit指令。
	///         特例!!!在try ,catch内return,但是finally里出现了throw,则此throw会覆盖return!
	///         每个catch_exit连接到finally_enter.
	///         如果遇到前一次获取的子控制流，
	///             如果子控制流必然抛出异常，当作throw error处理
	///             否则，连接每个catch_enter,再连接finally_enter.  然后对每个子控制流的目标，当作goto指令的目标处理。
	///             
	/// 
	///         子控制流确定后，从子控制流入口运行所有执行路径的遍历。
	///             如果途中有goto ,并且跳出了子控制流， 添加 子控制流的目标为 goto 的目标块.
	///             如果所有路径都有throw error,且没有catch块，标记 子控制流 必然抛出异常。
	///             如果中途有return , 添加子控制流的目标为 END块。
	///             如果存在一条正常运行到finally_exit的路径，则添加标记 子控制流为正常结束。
	///             
	///		   将子控制流当作一个整体考虑，重新查找令队列中没有嵌套其他try catch finally 结构,(子控制流)现在不算try 结构了。重复执行入上算法，直到找不到try结构。最后进行一轮控制流处理。
	///		   然后再展开子控制流，得到最终结果。
	/// </summary>
	public class ControlFlowGraphBuilder
	{
		public static ControlFlowGraph Build(Instruction[] instructions, ASMethod method)
		{
			if (instructions == null || instructions.Length == 0)
			{
				return new ControlFlowGraph(method);
			}

			if (!(instructions[instructions.Length - 1] is INS_END))
			{
				throw new InvalidOperationException("最后一条指令必须是END指令");
			}
			if (instructions.Any(i => i.INS_Code == INS_Code.END && i != instructions[instructions.Length - 1]))
			{
				throw new InvalidOperationException();
			}
			if (instructions.Any(i => i.INS_Code == INS_Code.if_logicOp_goto))
			{
				throw new InvalidOperationException("此指令只会在后续窥孔优化中生成");
			}


			Dictionary<int, int> flagid_index = new Dictionary<int, int>();
			for (int i = 0; i < instructions.Length; i++)
			{
				if (instructions[i].INS_Code == INS_Code.flag)
				{
					if (((INS_Flag)instructions[i]).flag_id != 0xffffff)
					{
						flagid_index.Add(((INS_Flag)instructions[i]).flag_id, i);
					}
				}
			}

			var cfg = new ControlFlowGraph(method);
			HashSet<int> enterIndices = new HashSet<int>();
			enterIndices.Add(0);
			for (int i = 0; i < instructions.Length; i++)
			{
				Instruction instruction = instructions[i];
				if (instruction.INS_Code == INS_Code.goto_flag ||
					instruction.INS_Code == INS_Code.if_false_goto ||
					instruction.INS_Code == INS_Code.if_true_goto ||
					instruction.INS_Code == INS_Code.iter_get ||
					instruction.INS_Code == INS_Code.iter_next
					)
				{
					enterIndices.Add(i + 1);

					int flagid = GetFlagId(instruction);
					enterIndices.Add(
						flagid_index[flagid]
						);
				}
				else if (instruction.INS_Code == INS_Code.throw_error 
					|| instruction.INS_Code == INS_Code.return_value || instruction.INS_Code == INS_Code.return_void
					
					|| (instruction.INS_Code == INS_Code.flag && ((INS_Flag)(instruction)).flag_id == 0xffffff)
					)
				{
					enterIndices.Add(i + 1);
				}
				else if (instruction.INS_Code == INS_Code.try_enter || instruction.INS_Code == INS_Code.catch_enter || instruction.INS_Code == INS_Code.finally_enter)
				{
					enterIndices.Add(i);
				}
				else if (instruction.INS_Code == INS_Code.finally_exit)
				{
					enterIndices.Add(i);
					enterIndices.Add(i + 1);
				}
				else if (instruction.INS_Code == INS_Code.END)
				{
					enterIndices.Add(i);
				}

			}

			var sortedIndices = enterIndices.ToArray().OrderBy(i => i).ToArray();
			int o_id = 0;
			for (int i = 0; i < sortedIndices.Length - 1; i++)
			{
				BasicBlock bb = new BasicBlock();
				bb.OriginalIndex = o_id++;
				bb.StartIndex = sortedIndices[i];
				bb.EndIndex = sortedIndices[i + 1] - 1;
				bb.BlockId = bb.OriginalIndex;

				for (int j = bb.StartIndex; j < bb.EndIndex + 1; j++)
				{
					bb.Instructions.Add(instructions[j]);
				}
				cfg.Blocks.Add(bb);
			}
			BasicBlock end = new BasicBlock();
			end.OriginalIndex = o_id++;
			end.StartIndex = instructions.Length - 1;
			end.EndIndex = instructions.Length - 1;
			end.BlockId = end.OriginalIndex;
			end.Instructions.Add(instructions[end.EndIndex]);
			cfg.Blocks.Add(end);


			ComputeFlow(cfg);

			return cfg;
		}




		private static int GetFlagId(Instruction ins)
		{
			if (ins is INS_Goto gotoIns)
				return gotoIns.flag_id;
			if (ins is INS_If_True_Goto ifTrueGoto)
				return ifTrueGoto.flag_id;
			if (ins is INS_If_False_Goto ifFalseGoto)
				return ifFalseGoto.flag_id;
			if (ins is INS_Iter_Get iterGet)
				return iterGet.flag_end_id;
			if (ins is INS_Iter_Next iterNext)
				return iterNext.flag_next_end_id;

			throw new InvalidOperationException();
		}


		enum try_state
		{

			Try,
			Catch,
			Finally,
		}


		class TryCtx
		{
			internal try_state state;

			internal BasicBlock[] cfg_blocks;

			internal HashSet<int> successors = new HashSet<int>();

			internal bool must_throw;

			internal bool may_normal_exit;

			internal HashSet<Tuple<int, BasicBlock>> finally_exit_goto = new HashSet<Tuple<int, BasicBlock>>();

			
			internal int tryid;
		}

		private static void ComputeFlow(ControlFlowGraph cfg)
		{
			var sortedblocks = cfg.Blocks.OrderBy(b => b.BlockId).ToArray();

			//确定每个块的TryId
			int tryid = 0;
			for (int i = 0; i < sortedblocks.Length; i++)
			{
				var ins = sortedblocks[i].Instructions[0];
				if (ins.INS_Code == INS_Code.try_enter)
				{
					tryid++;
					sortedblocks[i].TryBlockId = tryid;

				}
				else if (ins.INS_Code == INS_Code.finally_exit)
				{
					sortedblocks[i].TryBlockId = tryid;
					tryid--;
				}
				else
				{
					sortedblocks[i].TryBlockId = tryid;
				}
			}



			Stack<TryCtx> try_States = new Stack<TryCtx>();
			ComputeBlockList(sortedblocks, try_States);

			foreach (var block in sortedblocks)
			{
				block.Successors = block.Successors.Distinct().ToList();
			}

			ComputeBlockIsReachable(sortedblocks);

			foreach (var block in sortedblocks)
			{
				if (!block.IsReachable)
				{
					block.Successors.RemoveAll( s =>s.IsReachable ) ; //如果块不可达，删除所有它连接可达块的连接。
				}
			}

			//计算前驱块。
			foreach (var block in sortedblocks)
			{
				block.Predecessors.AddRange( sortedblocks.Where( b=>b.Successors.Contains(block) )  );
			}


		}

		/// <summary>
		/// 从第一个块进入，探索所有路径，计算所有基本块是否可达
		/// </summary>
		/// <param name="blocks"></param>
		private static void ComputeBlockIsReachable(BasicBlock[] blocks)
		{
			if (blocks == null || blocks.Length == 0)
			{
				return;
			}

			Queue<BasicBlock> worklist = new Queue<BasicBlock>();

			blocks[0].IsReachable = true;
			worklist.Enqueue(blocks[0]);

			while (worklist.Count > 0)
			{
				BasicBlock current = worklist.Dequeue();

				foreach (BasicBlock successor in current.Successors)
				{
					if (!successor.IsReachable)
					{
						successor.IsReachable = true;
						worklist.Enqueue(successor);
					}
				}
			}
		}


		private static void ComputeBlockList(BasicBlock[] blocks, Stack<TryCtx> try_States)
		{
			INS_Code lastOpCode;
			Instruction ins_last;

			Dictionary<BasicBlock, TryCtx> dict_childcfg = new Dictionary<BasicBlock, TryCtx>();


			for (int i = 0; i < blocks.Length; i++)
			{

				if (blocks[i].Instructions[0].INS_Code == INS_Code.try_enter)
				{
					if (i > 0 || try_States.Count == 0)
					{
						int tryid = blocks[i].TryBlockId;
						TryCtx tryCtx = new TryCtx();
						tryCtx.state = try_state.Try;
						tryCtx.tryid = tryid;

						try_States.Push(tryCtx);

						List<BasicBlock> childcfg = new List<BasicBlock>();
						childcfg.Add(blocks[i]);

						do
						{
							childcfg.Add(blocks[++i]);
						}
						while (!(blocks[i].TryBlockId == tryid && blocks[i].Instructions[0].INS_Code == INS_Code.finally_exit));

						tryCtx.cfg_blocks =childcfg.ToArray();
						ComputeBlockList(tryCtx.cfg_blocks, try_States);

						try_States.Pop();

						dict_childcfg.Add(childcfg[0], tryCtx);

						//根据子控制流的结果确定如何连。

						if (tryCtx.must_throw)
						{
							//当throw 处理
							lastOpCode = INS_Code.throw_error;
							ins_last = null;
							goto lbl_do;
						}
						else
						{
							if (tryCtx.may_normal_exit)
							{
								blocks[i].Successors.Add(blocks[i + 1]);
							}

							var childcfg_successors = tryCtx.successors.ToArray();
							for (int j = 0; j < childcfg_successors.Length; j++)
							{
								if (childcfg_successors[j] == int.MaxValue) //连接到END
								{
									if (blocks[blocks.Length - 1].Instructions[0].INS_Code == INS_Code.END)
									{
										blocks[i].Successors.Add(blocks[blocks.Length - 1]);
									}
									else
									{
										var try_state = try_States.Peek();
										if (try_state.state == ControlFlowGraphBuilder.try_state.Try || try_state.state == ControlFlowGraphBuilder.try_state.Catch)
										{
											var f = blocks.First(b => b.Instructions[0].INS_Code == INS_Code.finally_enter && b.TryBlockId == try_state.tryid);//最后一个才是匹配的
											blocks[i].Successors.Add(f);
										}
										else
										{
											Debug.Assert(try_state.state == ControlFlowGraphBuilder.try_state.Finally);
											var f = blocks.First(b => b.Instructions[0].INS_Code == INS_Code.finally_exit && b.TryBlockId == try_state.tryid);//最后一个才是匹配的
											blocks[i].Successors.Add(f);
										}

										try_state.finally_exit_goto.Add(new Tuple<int, BasicBlock>(int.MaxValue, childcfg[0]));

									}
								}
								else
								{
									blocks[i].JumpTargetFlagId = childcfg_successors[j];

									var target = blocks.FirstOrDefault(b => b.Instructions[0].INS_Code == INS_Code.flag && ((INS_Flag)b.Instructions[0]).flag_id == childcfg_successors[j]);
									if (target != null)
									{
										blocks[i].Successors.Add(target);
									}
									else
									{
										var try_state = try_States.Peek();
										if (try_state.state == ControlFlowGraphBuilder.try_state.Try || try_state.state == ControlFlowGraphBuilder.try_state.Catch)
										{
											var f = blocks.First(b => b.Instructions[0].INS_Code == INS_Code.finally_enter && b.TryBlockId == try_state.tryid);//最后一个才是匹配的
											blocks[i].Successors.Add(f);
										}
										else
										{
											Debug.Assert(try_state.state == ControlFlowGraphBuilder.try_state.Finally);
											var f = blocks.First(b => b.Instructions[0].INS_Code == INS_Code.finally_exit && b.TryBlockId == try_state.tryid);//最后一个才是匹配的
											blocks[i].Successors.Add(f);
										}

										try_state.finally_exit_goto.Add(new Tuple<int, BasicBlock>(childcfg_successors[j], childcfg[0]));

									}


								}
							}
						}
						continue;
					}
					else
					{

					}
				}
				else if (blocks[i].Instructions[0].INS_Code == INS_Code.catch_enter)
				{
					try_States.Peek().state = try_state.Catch;
				}
				else if (blocks[i].Instructions[0].INS_Code == INS_Code.finally_enter)
				{
					try_States.Peek().state = try_state.Finally;
				}
				else if (blocks[i].Instructions[0].INS_Code == INS_Code.finally_exit)
				{
					Debug.Assert(i == blocks.Length - 1);
					var tryctx = try_States.Peek();
					//确定子控制流状态
					UpdateTryCtxState(tryctx, blocks, blocks[0].TryBlockId,dict_childcfg);

					return;
				}
				


				ins_last = blocks[i].Instructions[blocks[i].Instructions.Count - 1];
				lastOpCode = ins_last.INS_Code;

			lbl_do:

				if (lastOpCode == INS_Code.goto_flag)
				{
					blocks[i].JumpTargetFlagId = GetFlagId(ins_last);

					var target = blocks.FirstOrDefault(b => b.Instructions[0].INS_Code == INS_Code.flag && ((INS_Flag)b.Instructions[0]).flag_id == GetFlagId(ins_last));
					if (target != null)
					{
						blocks[i].Successors.Add(target);
					}
					else
					{
						var try_state = try_States.Peek();
						if (try_state.state == ControlFlowGraphBuilder.try_state.Try || try_state.state == ControlFlowGraphBuilder.try_state.Catch)
						{
							var f = blocks.First(b => b.Instructions[0].INS_Code == INS_Code.finally_enter && b.TryBlockId == try_state.tryid);//最后一个才是匹配的
							blocks[i].Successors.Add(f);

						}
						else
						{
							Debug.Assert(try_state.state == ControlFlowGraphBuilder.try_state.Finally);
							var f = blocks.First(b => b.Instructions[0].INS_Code == INS_Code.finally_exit && b.TryBlockId == try_state.tryid);//最后一个才是匹配的
							blocks[i].Successors.Add(f);
						}

					}
				}
				else if (lastOpCode == INS_Code.if_true_goto || lastOpCode == INS_Code.if_false_goto || lastOpCode == INS_Code.iter_get || lastOpCode == INS_Code.iter_next)
				{
					blocks[i].JumpTargetFlagId = GetFlagId(ins_last);

					var target = blocks.First(b => b.Instructions[0].INS_Code == INS_Code.flag && ((INS_Flag)b.Instructions[0]).flag_id == GetFlagId(ins_last));
					blocks[i].Successors.Add(target);

					blocks[i].Successors.Add(blocks[i + 1]);//直接后续

					blocks[i].HasFallThrough = true;

				}
				else if (lastOpCode == INS_Code.flag && ((INS_Flag)(ins_last)).flag_id == 0xffffff)
				{
					blocks[i].Successors.Add(blocks[i + 1]);//直接后续

					//连接一个throw，和它的下一条。
					if (try_States.Count > 0)
					{
						var try_state = try_States.Peek();
						if (try_state.state == ControlFlowGraphBuilder.try_state.Try)
						{
							var f = blocks.First(b => b.Instructions[0].INS_Code == INS_Code.finally_enter && b.TryBlockId == try_state.tryid);
							blocks[i].Successors.Add(f);
							for (int j = i + 1; j < blocks.Length - 1; j++)
							{
								if (blocks[j].Instructions[0].INS_Code == INS_Code.catch_enter && blocks[j].TryBlockId == try_state.tryid)
								{
									blocks[i].Successors.Add(blocks[j]);
								}
							}

						}
						else if (try_state.state == ControlFlowGraphBuilder.try_state.Catch)
						{
							var f = blocks.First(b => b.Instructions[0].INS_Code == INS_Code.finally_enter && b.TryBlockId == try_state.tryid);
							blocks[i].Successors.Add(f);
						}
						else
						{
							Debug.Assert(try_state.state == ControlFlowGraphBuilder.try_state.Finally);
							var f = blocks.First(b => b.Instructions[0].INS_Code == INS_Code.finally_exit && b.TryBlockId == try_state.tryid);//最后一个才是匹配的
							blocks[i].Successors.Add(f);
						}

					}
					else
					{
						blocks[i].Successors.Add(blocks.First(b => b.Instructions[0].INS_Code == INS_Code.END)); //跳到结束
					}
				}

				else if (lastOpCode == INS_Code.throw_error || lastOpCode == INS_Code.return_value || lastOpCode == INS_Code.return_void)
				{
					if (try_States.Count > 0)
					{
						var try_state = try_States.Peek();
						if (try_state.state == ControlFlowGraphBuilder.try_state.Try)
						{
							var f = blocks.First(b => b.Instructions[0].INS_Code == INS_Code.finally_enter && b.TryBlockId == try_state.tryid);
							blocks[i].Successors.Add(f);


							for (int j = i + 1; j < blocks.Length - 1; j++)
							{
								if (blocks[j].Instructions[0].INS_Code == INS_Code.catch_enter && blocks[j].TryBlockId == try_state.tryid)
								{
									blocks[i].Successors.Add(blocks[j]);
								}
							}

						}
						else if (try_state.state == ControlFlowGraphBuilder.try_state.Catch)
						{
							var f = blocks.First(b => b.Instructions[0].INS_Code == INS_Code.finally_enter && b.TryBlockId == try_state.tryid);
							blocks[i].Successors.Add(f);
						}
						else
						{
							Debug.Assert(try_state.state == ControlFlowGraphBuilder.try_state.Finally);
							var f = blocks.First(b => b.Instructions[0].INS_Code == INS_Code.finally_exit && b.TryBlockId == try_state.tryid);//最后一个才是匹配的
							blocks[i].Successors.Add(f);
						}

					}
					else
					{
						blocks[i].Successors.Add(blocks.First(b => b.Instructions[0].INS_Code == INS_Code.END)); //跳到结束
					}
				}
				else if (lastOpCode == INS_Code.try_exit)
				{
					var f = blocks.First(b => b.Instructions[0].INS_Code == INS_Code.finally_enter && b.TryBlockId == try_States.Peek().tryid);//最后一个才是匹配的
					blocks[i].Successors.Add(f);

					for (int j = i + 1; j < blocks.Length - 1; j++)
					{
						if (blocks[j].Instructions[0].INS_Code == INS_Code.catch_enter && blocks[j].TryBlockId == try_States.Peek().tryid)
						{
							blocks[i].Successors.Add(blocks[j]);
						}
					}

				}
				else if (lastOpCode == INS_Code.catch_exit)
				{
					var f = blocks.First(b => b.Instructions[0].INS_Code == INS_Code.finally_enter && b.TryBlockId == try_States.Peek().tryid);//最后一个才是匹配的
					blocks[i].Successors.Add(f);
				}
				else if (i < blocks.Length - 1)
				{
					blocks[i].Successors.Add(blocks[i + 1]);
				}

			}

		}


		/// <summary>
		/// 遍历所有blocks的流程，确定出口处的状态。
		/// finally块会覆盖之前的行为，所以要先遍历finally块。
		///     比如，finally中return,会覆盖之前的throw,finally中throw会覆盖之前的return,finally中goto到其他块，也会覆盖之前的return和throw!!
		///     所以，只有在finally能正常结束的情况下，才继续计算从try_enter->运行到finally_enter.
		/// 
		/// 是否可能正常结束(运行到finally_exit 没有遇到throw 和 return）.
		/// 是否必然报错 (运行到finally_exit前 所有路径都会遇到 throw.
		/// 是否有跳出blocks的块的goto跳转。如果有，tryctx的successors添加goto的flagid.
		/// 是否路径上有return.如果有，tryctx的successors添加int.max.
		/// </summary>
		/// <param name="tryctx"></param>
		/// <param name="blocks"></param>
		/// <exception cref="NotImplementedException"></exception>
		private static void UpdateTryCtxState(TryCtx tryctx, BasicBlock[] cfgblocks, int TryId, Dictionary<BasicBlock, TryCtx> dict_childcfg)
		{
			bool flag_needchecktry = false;

			{ //finally path
				Debug.Assert(cfgblocks[0].Instructions[0].INS_Code == INS_Code.try_enter);
				var finallypass = cfgblocks.SkipWhile(b => !(b.Instructions[0].INS_Code == INS_Code.finally_enter && b.TryBlockId == TryId)).ToArray();

				GraphPathFinder pathFinder = new GraphPathFinder();
				for (int i = 0; i < finallypass.Length; i++)
				{
					var b = finallypass[i];
					List<BasicBlock> successors;
					if (dict_childcfg.ContainsKey(b))
					{
						var childctx = dict_childcfg[b];
						successors = childctx.cfg_blocks[childctx.cfg_blocks.Length - 1].Successors;
					}
					else
					{
						successors = b.Successors;
					}
					for (int j = 0; j < successors.Count; j++)
					{
						var s = successors[j];
						pathFinder.AddEdge(i, Array.IndexOf(finallypass, s));
					}
				}

				var paths = pathFinder.FindAllPaths(0, finallypass.Length - 1);
				//Debug.Assert(paths.Count > 0);
				//paths.Count == 0 说明有死循环! 



				int throwpath = 0;
				int returnpath = 0;
				int normalpath = 0;
				int gotopath = 0;

				for (int i = 0; i < paths.Count; i++)
				{
					var path = paths[i];

					
					bool mustthrow_path = false;
					bool early_return = false;
					bool jump_out = false;

					for (int j = 0; j < path.Count; j++)
					{
						var b = finallypass[path[j]];
						if (dict_childcfg.ContainsKey(b))
						{
							//throw new NotImplementedException();
							if (dict_childcfg[b].must_throw)
							{
								
								mustthrow_path = true;
									
							}

							else if (!dict_childcfg[b].may_normal_exit)
							{
								Debug.Assert(dict_childcfg[b].successors.Contains(int.MaxValue));
								if (dict_childcfg[b].successors.All(i => i == int.MaxValue)) //全部都是退出指令
								{
									early_return = true;
									goto lbl_nextpath;
								}
							}
						}
						else
						{
							Debug.Assert(b.TryBlockId == tryctx.tryid);
							for (int ii = 0; ii < b.Instructions.Count; ii++)
							{
								var ins = b.Instructions[ii];
								

								if (ins.INS_Code == INS_Code.throw_error)
								{
									
									mustthrow_path = true;
									goto lbl_nextpath;
									
								}

								if (ins.INS_Code == INS_Code.return_value || ins.INS_Code == INS_Code.return_void)
								{
									early_return = true;
									goto lbl_nextpath;
								}

								if (ins.INS_Code == INS_Code.goto_flag)
								{

									int jumptarget = GetFlagId(ins);

									var target = finallypass.FirstOrDefault(b => b.Instructions[0].INS_Code == INS_Code.flag && ((INS_Flag)b.Instructions[0]).flag_id == jumptarget);
									if (target == null)
									{
										tryctx.successors.Add(jumptarget);
										jump_out = true;
										goto lbl_nextpath;
									}

								}
							}

						}
					}

				lbl_nextpath:

					if (mustthrow_path)
					{
						throwpath++;
					}
					else if (early_return)
					{
						returnpath++;
					}
					else if (jump_out)
					{
						gotopath++;
					}
					else
					{
						normalpath++;
					}

				}

				Debug.Assert(throwpath + returnpath + normalpath + gotopath == paths.Count);

				if (returnpath > 0)
				{
					tryctx.successors.Add(int.MaxValue);
				}

				if (throwpath == paths.Count)
				{
					tryctx.must_throw = true;
				}
				else if (normalpath > 0)
				{
					flag_needchecktry = true;
				}
			}


			if(flag_needchecktry)
			{
				var f_enter = cfgblocks.First(b => b.Instructions[0].INS_Code == INS_Code.finally_enter && b.TryBlockId == TryId);
				
				var try_catch_pass = cfgblocks.Take( Array.IndexOf(cfgblocks,f_enter) + 1 ).ToArray();

				GraphPathFinder pathFinder = new GraphPathFinder();
				for (int i = 0; i < try_catch_pass.Length; i++)
				{
					var b = try_catch_pass[i];

					List<BasicBlock> successors;

					if (dict_childcfg.ContainsKey(b))
					{
						var childctx = dict_childcfg[b];
						successors = childctx.cfg_blocks[childctx.cfg_blocks.Length - 1].Successors;
					}
					else
					{
						successors = b.Successors;
					}

					for (int j = 0; j < successors.Count; j++)
					{
						var s = successors[j];
						pathFinder.AddEdge(i, Array.IndexOf(try_catch_pass, s));
					}
				}

				var paths = pathFinder.FindAllPaths(0, try_catch_pass.Length - 1);

				//Debug.Assert(paths.Count > 0);
				//paths.Count == 0 说明有死循环！

				int throwpath = 0;
				int returnpath = 0;
				int normalpath = 0;
				int gotopath = 0;

				for (int i = 0; i < paths.Count; i++)
				{
					var path = paths[i];

					try_state? try_ = null;

					bool mustthrow_path = false;
					bool early_return = false;
					bool jump_out = false;

					for (int j = 0; j < path.Count; j++)
					{
						var b = try_catch_pass[path[j]];
						if (dict_childcfg.ContainsKey(b))
						{
							//throw new NotImplementedException();
							if (dict_childcfg[b].must_throw)
							{
								Debug.Assert(try_state.Finally != try_);

								if (try_ == null || try_ == try_state.Catch )
								{
									mustthrow_path = true;
									goto lbl_nextpath;
								}
								else if (try_ == try_state.Try)
								{
									if (try_catch_pass.Any(k => k.TryBlockId == tryctx.tryid && k.Instructions[0].INS_Code == INS_Code.catch_enter))
									{

									}
									else
									{
										mustthrow_path = true;
										goto lbl_nextpath;
									}
								}
							}

							else if (!dict_childcfg[b].may_normal_exit)
							{
								Debug.Assert(dict_childcfg[b].successors.Count>0);

								if (dict_childcfg[b].successors.All(i => i == int.MaxValue))
								{
									early_return = true;
									goto lbl_nextpath;
								}

							}
						}
						else
						{
							Debug.Assert(b.TryBlockId == tryctx.tryid);
							for (int ii = 0; ii < b.Instructions.Count; ii++)
							{
								var ins = b.Instructions[ii];
								if (ins.INS_Code == INS_Code.try_enter)
								{
									try_ = try_state.Try;
								}
								if (ins.INS_Code == INS_Code.catch_enter)
								{
									try_ = try_state.Catch;
								}
								if (ins.INS_Code == INS_Code.finally_enter)
								{
									try_ = try_state.Finally;
								}
								Debug.Assert(ins.INS_Code != INS_Code.finally_exit);
								

								if (ins.INS_Code == INS_Code.throw_error)
								{
									Debug.Assert(try_state.Finally != try_);
									if (try_ == null || try_ == try_state.Catch )
									{
										mustthrow_path = true;
										goto lbl_nextpath;
									}
									else if (try_ == try_state.Try)
									{
										if (try_catch_pass.Any(k => k.TryBlockId == tryctx.tryid && k.Instructions[0].INS_Code == INS_Code.catch_enter))
										{

										}
										else
										{
											mustthrow_path = true;
											goto lbl_nextpath;
										}
									}
								}

								if (ins.INS_Code == INS_Code.return_value || ins.INS_Code == INS_Code.return_void)
								{
									early_return = true;
									goto lbl_nextpath;
								}

								if (ins.INS_Code == INS_Code.goto_flag)
								{

									int jumptarget = GetFlagId(ins);

									var target = try_catch_pass.FirstOrDefault(b => b.Instructions[0].INS_Code == INS_Code.flag && ((INS_Flag)b.Instructions[0]).flag_id == jumptarget);
									if (target == null)
									{
										tryctx.successors.Add(jumptarget);
										jump_out = true;
										goto lbl_nextpath;
									}

								}
							}

						}
					}

				lbl_nextpath:

					if (mustthrow_path)
					{
						throwpath++;
					}
					else if (early_return)
					{
						returnpath++;
					}
					else if (jump_out)
					{
						gotopath++;
					}
					else
					{
						normalpath++;
					}

				}

				Debug.Assert(throwpath + returnpath + normalpath + gotopath == paths.Count);

				if (throwpath == paths.Count)
				{
					tryctx.must_throw = true;
				}
				else if (normalpath > 0)
				{
					tryctx.may_normal_exit = true;
				}
				else if (gotopath > 0)
				{

				}

				if (returnpath > 0)
				{
					tryctx.successors.Add(int.MaxValue);
				}


			}


			foreach (var item in tryctx.finally_exit_goto)
			{
				tryctx.successors.Add(item.Item1);
			}
			
			//tryctx.successors.Add(int.MaxValue);
			//tryctx.may_normal_exit = true;

			//tryctx.must_throw = true;
			//throw new NotImplementedException();
		}
	}

	public class GraphPathFinder
	{
		// 邻接表存储图结构
		private readonly Dictionary<int, List<int>> _adjacencyList;

		// 定义栈中存储的状态对象：包含当前节点、已访问节点集合、当前路径
		private class DfsState
		{
			public int CurrentNode { get; set; }          // 当前遍历的节点
			public HashSet<int> Visited { get; set; }     // 到当前节点为止已访问的节点（防回路）
			public List<int> CurrentPath { get; set; }    // 到当前节点为止的路径
			public int NeighborIndex { get; set; }        // 下一个要遍历的邻接节点索引（标记遍历进度）
		}

		public GraphPathFinder()
		{
			_adjacencyList = new Dictionary<int, List<int>>();
		}

		/// <summary>
		/// 添加有向边（无向边需双向添加）
		/// </summary>
		public void AddEdge(int fromNode, int toNode)
		{
			if (!_adjacencyList.ContainsKey(fromNode))
			{
				_adjacencyList[fromNode] = new List<int>();
			}
			if (!_adjacencyList[fromNode].Contains(toNode))
			{
				_adjacencyList[fromNode].Add(toNode);
			}
		}

		/// <summary>
		/// 非递归方式查找起点到终点的所有路径
		/// </summary>
		public List<List<int>> FindAllPaths(int start, int end)
		{
			List<List<int>> allPaths = new List<List<int>>();
			// 初始化栈，压入起点状态
			Stack<DfsState> stack = new Stack<DfsState>();
			stack.Push(new DfsState
			{
				CurrentNode = start,
				Visited = new HashSet<int> { start },  // 起点标记为已访问
				CurrentPath = new List<int> { start }, // 初始路径包含起点
				NeighborIndex = 0                      // 从第0个邻接节点开始遍历
			});

			while (stack.Count > 0)
			{
				DfsState currentState = stack.Peek(); // 取栈顶元素（不弹出，后续根据进度处理）
				int currentNode = currentState.CurrentNode;

				// 1. 终止条件：当前节点是终点 → 保存路径，弹出栈顶（该分支遍历完成）
				if (currentNode == end)
				{
					allPaths.Add(new List<int>(currentState.CurrentPath));
					stack.Pop();
					continue;
				}

				// 2. 若当前节点无邻接节点 → 弹出栈顶（无后续路径）
				if (!_adjacencyList.ContainsKey(currentNode))
				{
					stack.Pop();
					continue;
				}

				List<int> neighbors = _adjacencyList[currentNode];
				// 3. 遍历当前节点的邻接节点（按NeighborIndex标记的进度）
				if (currentState.NeighborIndex < neighbors.Count)
				{
					int nextNeighbor = neighbors[currentState.NeighborIndex];
					// 先标记当前进度+1（下次处理该状态时，从下一个邻接节点开始）
					currentState.NeighborIndex++;

					// 4. 跳过已访问的节点（防回路）
					if (!currentState.Visited.Contains(nextNeighbor))
					{
						// 复制已访问集合和路径（避免不同分支互相干扰）
						HashSet<int> newVisited = new HashSet<int>(currentState.Visited);
						newVisited.Add(nextNeighbor);

						List<int> newPath = new List<int>(currentState.CurrentPath);
						newPath.Add(nextNeighbor);

						// 5. 压入新状态到栈（继续深度遍历）
						stack.Push(new DfsState
						{
							CurrentNode = nextNeighbor,
							Visited = newVisited,
							CurrentPath = newPath,
							NeighborIndex = 0
						});
					}
				}
				else
				{
					// 6. 所有邻接节点遍历完毕 → 弹出栈顶（回溯）
					stack.Pop();
				}
			}

			return allPaths;
		}


		
	}

	
}
