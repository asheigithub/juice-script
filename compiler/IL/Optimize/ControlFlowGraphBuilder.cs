using juicescript.ABC;
using juicescript.ABC.INS;
using System;
using System.Collections.Generic;
using System.Linq;

namespace juicescript.compiler.IL.Optimize
{
	/// <summary>
	/// 控制流图(Control Flow Graph, CFG)构建器
	/// 算法说明:
	/// 1 分割基本块。
	///   编译器确保除非没有任何指令，否则最后一条指令肯定是 END.
	///   这条END指令是唯一的退出指令，它作为独立的最后一个基本块。先检查指令序列是否符合此约束，不符合就抛出异常。
	///   基本块分割：跳转指令有：goto,if_true_goto,if_false_goto, throw error, return [value].
	///              先确定它们的跳转目标。goto类指令的跳转目标是查找某个 INS_flag的 flagid.
	///              throw error的跳转目标是：如果不在 try catch finally 结构内，则直接跳转到 END。
	///                                      如果在 try catch finally内,则跳转到每一个对应的catch_enter.如果没有catch,则跳转到finally_enter.
	///              return 类指令的跳转目标是：如果不在 try catch finally 结构内，直接跳到 END。
	///                                      如果在 try catch finally内,则跳转到每一个对应的catch_enter.如果没有catch,则跳转到finally_enter.
	///              基本块的入口，包括编号为0的第一条指令，跳转类指令的跳转目标，跳转类指令的下一条指令,和 try_enter,catch_enter,finally_enter.
	///              确定入口后，按照指令序列，从入口指令开始（含入口指令）到下一条入口指令（不含下一条入口指令）之间的指令序列归为一个基本块。
	///              特殊情况：END指令自己就是最后一个基本块.finally_enter到对应的finally_exit(包含finally_exit)之间的指令是一个基本块.              
	///              
	/// 
	/// </summary>
	public class ExceptionBlockInfo
	{
		public string BlockType { get; set; }
		public int TryEnterIndex { get; set; }
		public int TryExitIndex { get; set; }
		public int CatchEnterIndex { get; set; }
		public int CatchExitIndex { get; set; }
		public int FinallyEnterIndex { get; set; }
		public int FinallyExitIndex { get; set; }
		public int[] CatchPcList { get; set; }
	}

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

			var cfg = new ControlFlowGraph(method);
			int n = instructions.Length;

			Dictionary<int, int> flagIdToIndex = new Dictionary<int, int>();
			List<int> tryEnterIndices = new List<int>();
			List<int> catchEnterIndices = new List<int>();
			List<int> finallyEnterIndices = new List<int>();
			Dictionary<int, ExceptionBlockInfo> tryEnterToInfo = new Dictionary<int, ExceptionBlockInfo>();

			for (int i = 0; i < n; i++)
			{
				var ins = instructions[i];
				if (ins is INS_Flag flag)
				{
					flagIdToIndex[flag.flag_id] = i;
				}
			else if (ins is INS_Try_Enter tryEnter)
			{
				tryEnterIndices.Add(i);
				var info = new ExceptionBlockInfo
				{
					TryEnterIndex = i,
					FinallyEnterIndex = tryEnter.finally_pc,
					FinallyExitIndex = tryEnter.finally_exit_pc,
					CatchPcList = tryEnter.catch_pc
				};
				tryEnterToInfo[i] = info;
			}
				else if (ins is INS_Try_Exit)
				{
					foreach (var kvp in tryEnterToInfo)
					{
						if (kvp.Value.TryEnterIndex < i && kvp.Value.TryExitIndex == 0)
						{
							kvp.Value.TryExitIndex = i;
						}
					}
				}
				else if (ins is INS_Catch_Enter)
				{
					catchEnterIndices.Add(i);
					int tryEnterIdx = FindEnclosingTryEnter(i, tryEnterToInfo);
					if (tryEnterIdx >= 0 && tryEnterToInfo.TryGetValue(tryEnterIdx, out var info))
					{
						info.CatchEnterIndex = i;
					}
				}
				else if (ins is INS_Finally_Enter)
				{
					finallyEnterIndices.Add(i);
					int tryEnterIdx = FindEnclosingTryEnter(i, tryEnterToInfo);
					if (tryEnterIdx >= 0 && tryEnterToInfo.TryGetValue(tryEnterIdx, out var info))
					{
						info.FinallyEnterIndex = i;
					}
				}
				else if (ins is INS_Finally_Exit)
				{
					int tryEnterIdx = FindEnclosingTryEnter(i, tryEnterToInfo);
					if (tryEnterIdx >= 0 && tryEnterToInfo.TryGetValue(tryEnterIdx, out var info))
					{
						info.FinallyExitIndex = i;
					}
				}
			}

			HashSet<int> entryPoints = new HashSet<int>();
			entryPoints.Add(0);

			Dictionary<int, List<int>> tryEnterJumps = new Dictionary<int, List<int>>();
			foreach (var kvp in tryEnterToInfo)
			{
				tryEnterJumps[kvp.Key] = new List<int>();
			}

			for (int i = 0; i < n; i++)
			{
				var ins = instructions[i];
				int? jumpTarget = GetJumpTarget(ins, flagIdToIndex, i, tryEnterToInfo, instructions);

				if (jumpTarget.HasValue)
				{
					entryPoints.Add(jumpTarget.Value);

					int enclosingTryEnter = FindEnclosingTryEnter(i, tryEnterToInfo);
					if (enclosingTryEnter >= 0 && tryEnterJumps.ContainsKey(enclosingTryEnter))
					{
						if (ins is INS_Throw || ins is INS_Return_Value || ins is INS_Return_Void)
						{
							var info = tryEnterToInfo[enclosingTryEnter];
							if (info.CatchPcList != null && info.CatchPcList.Length > 0)
							{
								foreach (var catchPc in info.CatchPcList)
								{
									int catchIdx = FindCatchEnterIndex(catchPc, catchEnterIndices);
									if (catchIdx >= 0)
									{
										tryEnterJumps[enclosingTryEnter].Add(catchIdx);
										entryPoints.Add(catchIdx);
									}
								}
							}
							else if (info.FinallyEnterIndex > 0)
							{
								tryEnterJumps[enclosingTryEnter].Add(info.FinallyEnterIndex);
								entryPoints.Add(info.FinallyEnterIndex);
							}
						}
					}
				}

				if (IsJumpInstruction(ins))
				{
					if (i + 1 < n)
					{
						entryPoints.Add(i + 1);
					}
				}
			}

			foreach (var info in tryEnterToInfo.Values)
			{
				if (info.TryEnterIndex >= 0)
					entryPoints.Add(info.TryEnterIndex);
				if (info.CatchEnterIndex > 0)
					entryPoints.Add(info.CatchEnterIndex);
				if (info.FinallyEnterIndex > 0)
					entryPoints.Add(info.FinallyEnterIndex);
			}

			foreach (var idx in tryEnterIndices)
			{
				entryPoints.Add(idx);
			}

			foreach (var idx in catchEnterIndices)
			{
				entryPoints.Add(idx);
			}

			foreach (var idx in finallyEnterIndices)
			{
				entryPoints.Add(idx);
			}

			if (n > 0)
			{
				entryPoints.Add(n - 1);
			}

			List<int> sortedEntries = entryPoints.OrderBy(x => x).ToList();

			for (int i = 0; i < sortedEntries.Count; i++)
			{
				int startIdx = sortedEntries[i];
				int endIdx = (i + 1 < sortedEntries.Count) ? sortedEntries[i + 1] : n;

				if (startIdx >= n)
					continue;
				if (endIdx > n)
					endIdx = n;

				bool isFinallyBlock = false;
				ExceptionBlockInfo enclosingInfo = null;
				int enclosingTryEnter = FindEnclosingTryEnter(startIdx, tryEnterToInfo);
				if (enclosingTryEnter >= 0 && tryEnterToInfo.TryGetValue(enclosingTryEnter, out enclosingInfo))
				{
					if (enclosingInfo.FinallyEnterIndex > 0 && enclosingInfo.FinallyExitIndex > 0)
					{
						if (startIdx >= enclosingInfo.FinallyEnterIndex && startIdx <= enclosingInfo.FinallyExitIndex)
						{
							isFinallyBlock = true;
							endIdx = enclosingInfo.FinallyExitIndex + 1;
						}
					}
				}

				var block = new BasicBlock
				{
					BlockId = cfg.Blocks.Count,
					OriginalIndex = cfg.Blocks.Count,
					StartIndex = startIdx,
					EndIndex = endIdx - 1,
					ExceptionInfo = isFinallyBlock ? enclosingInfo : null
				};

				for (int j = startIdx; j < endIdx && j < n; j++)
				{
					block.Instructions.Add(instructions[j]);
				}

				cfg.Blocks.Add(block);
			}

			cfg.TryEnterToInfo = tryEnterToInfo;

			for (int i = 0; i < n; i++)
			{
				int blockIdx = FindBlockContaining(cfg.Blocks, i);
				if (blockIdx >= 0)
				{
					cfg.InstructionToBlock[i] = blockIdx;
				}
			}

			return cfg;
        }

		private static bool IsJumpInstruction(Instruction ins)
		{
			return ins is INS_Goto ||
				   ins is INS_If_True_Goto ||
				   ins is INS_If_False_Goto ||
				   ins is INS_If_LogicOp_Goto ||
				   ins is INS_Throw ||
				   ins is INS_Return_Value ||
				   ins is INS_Return_Void;
		}

		private static int? GetJumpTarget(Instruction ins, Dictionary<int, int> flagIdToIndex, int currentIndex, Dictionary<int, ExceptionBlockInfo> tryEnterToInfo, Instruction[] instructions)
		{
			if (ins is INS_Goto gotoIns)
			{
				if (flagIdToIndex.TryGetValue(gotoIns.flag_id, out int targetIdx))
				{
					return targetIdx;
				}
			}
			else if (ins is INS_If_True_Goto ifTrueGoto)
			{
				if (flagIdToIndex.TryGetValue(ifTrueGoto.flag_id, out int targetIdx))
				{
					return targetIdx;
				}
			}
			else if (ins is INS_If_False_Goto ifFalseGoto)
			{
				if (flagIdToIndex.TryGetValue(ifFalseGoto.flag_id, out int targetIdx))
				{
					return targetIdx;
				}
			}
			else if (ins is INS_If_LogicOp_Goto ifLogicOpGoto)
			{
				if (flagIdToIndex.TryGetValue(ifLogicOpGoto.flag_id, out int targetIdx))
				{
					return targetIdx;
				}
			}
			else if (ins is INS_Throw || ins is INS_Return_Value || ins is INS_Return_Void)
			{
				int enclosingTry = FindEnclosingTryEnter(currentIndex, tryEnterToInfo);
				if (enclosingTry >= 0 && tryEnterToInfo.TryGetValue(enclosingTry, out var info))
				{
					if (info.CatchPcList != null && info.CatchPcList.Length > 0 && info.CatchEnterIndex > 0)
					{
						return info.CatchEnterIndex;
					}
					else if (info.FinallyEnterIndex > 0)
					{
						return info.FinallyEnterIndex;
					}
				}
				return instructions.Length - 1;
			}

			return null;
		}

		private static int? GetFlagId(Instruction ins)
		{
			if (ins is INS_Goto gotoIns)
				return gotoIns.flag_id;
			if (ins is INS_If_True_Goto ifTrueGoto)
				return ifTrueGoto.flag_id;
			if (ins is INS_If_False_Goto ifFalseGoto)
				return ifFalseGoto.flag_id;
			if (ins is INS_If_LogicOp_Goto ifLogicOpGoto)
				return ifLogicOpGoto.flag_id;
			return null;
		}

		private static int FindEnclosingTryEnter(int index, Dictionary<int, ExceptionBlockInfo> tryEnterToInfo)
		{
			int enclosingTry = -1;
			int bestStart = -1;
			foreach (var kvp in tryEnterToInfo)
			{
				int tryStart = kvp.Value.TryEnterIndex;
				int tryEnd = kvp.Value.TryExitIndex;
				int catchStart = kvp.Value.CatchEnterIndex;
				int finallyStart = kvp.Value.FinallyEnterIndex;
				int finallyEnd = kvp.Value.FinallyExitIndex;

				bool isInTry = tryStart >= 0 && tryStart <= index && (tryEnd == 0 || tryEnd >= index);
				bool isInCatch = catchStart > 0 && catchStart <= index && (kvp.Value.CatchExitIndex == 0 || kvp.Value.CatchExitIndex >= index);
				bool isInFinally = finallyStart > 0 && finallyStart <= index && (finallyEnd == 0 || (kvp.Value.TryExitIndex > 0 && index <= GetOuterTryEnd(kvp.Key, tryEnterToInfo)));

				if (isInTry || isInCatch || isInFinally)
				{
					if (tryStart > bestStart)
					{
						bestStart = tryStart;
						enclosingTry = kvp.Key;
					}
				}
			}
			return enclosingTry;
		}

		private static int GetOuterTryEnd(int tryEnterIndex, Dictionary<int, ExceptionBlockInfo> tryEnterToInfo)
		{
			if (!tryEnterToInfo.TryGetValue(tryEnterIndex, out var info))
				return 0;
			if (info.TryExitIndex > 0)
				return info.TryExitIndex;
			if (info.FinallyExitIndex > 0)
				return info.FinallyExitIndex;
			return 0;
		}

		private static int FindCatchEnterIndex(int catchPc, List<int> catchEnterIndices)
		{
			foreach (var idx in catchEnterIndices)
			{
				if (idx == catchPc)
					return idx;
			}
			return -1;
		}

		private static int FindBlockContaining(List<BasicBlock> blocks, int instructionIndex)
		{
			for (int i = 0; i < blocks.Count; i++)
			{
				if (instructionIndex >= blocks[i].StartIndex && instructionIndex <= blocks[i].EndIndex)
				{
					return i;
				}
			}
			return -1;
		}
	}
}
