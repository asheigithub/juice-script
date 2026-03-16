using juicescript.ABC;
using juicescript.ABC.INS;
using System;
using System.Collections.Generic;
using System.Linq;

namespace juicescript.compiler.IL.Optimize
{
    /// <summary>
    /// 控制流图(Control Flow Graph, CFG)构建器
    /// 
    /// 算法概述:
    /// 1. 识别所有基本块的入口点(Entry Points)
    ///    - 方法入口(指令索引0)
    ///    - flag标签位置(跳转目标)
    ///    - try/catch/finally块入口
    /// 
    /// 2. 根据入口点分割基本块
    ///    - 每个基本块从入口点开始,到下一个入口点前一条指令结束
    ///    - 基本块内不包含控制流跳转指令(作为块的最后一 条指令)
    /// 
    /// 3. 构建控制流边(Control Flow Edges)
    ///    - 分析每个基本块的最后一条指令
    ///    - 根据指令类型确定后继块(goto/条件跳转/返回/异常退出等)
    ///    - 特殊处理try-catch-finally的异常流
    /// 
    /// 4. 计算可达性
    ///    - 从入口块开始进行BFS,标记所有可达块
    /// </summary>
    public class ControlFlowGraphBuilder
    {
        /// <summary>
        /// 从指令序列构建控制流图
        /// </summary>
        /// <param name="instructions">原始指令序列</param>
        /// <param name="method">AS方法,包含方法元数据</param>
        /// <returns>构建完成的控制流图</returns>
        public static ControlFlowGraph Build(Instruction[] instructions, ASMethod method)
        {
            var cfg = new ControlFlowGraph(method);

            if (instructions == null || instructions.Length == 0)
                return cfg;

            // 第一步:构建字节码偏移到指令索引的映射表
            // 用于将字节码偏移(如try_enter中的finally_pc/catch_pc)转换为指令索引
            var byteOffsetToInstructionIndex = new Dictionary<int, int>();
            int currentOffset = 0;
            for (int i = 0; i < instructions.Length; i++)
            {
                byteOffsetToInstructionIndex[currentOffset] = i;
                currentOffset += instructions[i].Size;
            }

            // 构建指令索引到字节码偏移的反向映射(用于异常信息收集)
            var instructionIndexToByteOffset = new int[instructions.Length];
            currentOffset = 0;
            for (int i = 0; i < instructions.Length; i++)
            {
                instructionIndexToByteOffset[i] = currentOffset;
                currentOffset += instructions[i].Size;
            }

            // 第二步:识别所有基本块的入口点
            // 入口点是控制流可能转移到的新位置,每个入口点都是一个新基本块的开始
            
            // 首先收集所有被跳转指令引用的flag_id
            // 只有被引用的flag才是有效的跳转目标
            var referencedFlagIds = new HashSet<int>();
            for (int i = 0; i < instructions.Length; i++)
            {
                var ins = instructions[i];
                if (ins.INS_Code == INS_Code.goto_flag)
                {
                    referencedFlagIds.Add(((INS_Goto)ins).flag_id);
                }
                else if (ins.INS_Code == INS_Code.if_true_goto)
                {
                    referencedFlagIds.Add(((INS_If_True_Goto)ins).flag_id);
                }
                else if (ins.INS_Code == INS_Code.if_false_goto)
                {
                    referencedFlagIds.Add(((INS_If_False_Goto)ins).flag_id);
                }
            }

            var entryPoints = new HashSet<int>();
            
            // 方法的第一条指令始终是一个入口点
            entryPoints.Add(0);

            // 遍历所有指令,识别各类入口点
            for (int i = 0; i < instructions.Length; i++)
            {
                var ins = instructions[i];
                
                // flag标签:只有被跳转指令引用的flag才作为入口点
                // 空的flag标签(如未使用的代码块标记)不作为入口点
                if (ins.INS_Code == INS_Code.flag)
                {
                    var flag = (INS_Flag)ins;
                    if (referencedFlagIds.Contains(flag.flag_id))
                    {
                        entryPoints.Add(i);
                    }
                }
                // try_enter:try块的入口,同时处理finally和catch的目标
                else if (ins.INS_Code == INS_Code.try_enter)
                {
                    var tryEnter = (INS_Try_Enter)ins;
                    // finally块入口(如果存在)
                    if (tryEnter.finally_pc > 0 && byteOffsetToInstructionIndex.TryGetValue(tryEnter.finally_pc, out int finallyIdx))
                    {
                        entryPoints.Add(finallyIdx);
                    }
                    // catch块入口(如果存在)
                    if (tryEnter.catch_pc != null)
                    {
                        for (int j = 0; j < tryEnter.catch_pc.Length; j++)
                        {
                            if (byteOffsetToInstructionIndex.TryGetValue(tryEnter.catch_pc[j], out int catchIdx))
                            {
                                entryPoints.Add(catchIdx);
                            }
                        }
                    }
                }
                // catch_enter:catch块入口
                else if (ins.INS_Code == INS_Code.catch_enter)
                {
                    entryPoints.Add(i);
                }
                // finally_enter:finally块入口
                else if (ins.INS_Code == INS_Code.finally_enter)
                {
                    entryPoints.Add(i);
                }
            }

            // 第三步:根据入口点创建基本块
            // 入口点按索引排序后,每个入口点到下一个入口点前一条指令构成一个基本块
            var sortedEntries = entryPoints.OrderBy(e => e).ToList();
            var blockStarts = new List<int>();
            
            foreach (var entry in sortedEntries)
            {
                if (entry >= 0 && entry < instructions.Length)
                {
                    if (!blockStarts.Contains(entry))
                    {
                        blockStarts.Add(entry);
                    }
                }
            }
            blockStarts.Sort();

            // 遍历所有入口点,为每个入口点创建一个基本块
            for (int i = 0; i < blockStarts.Count; i++)
            {
                int startIdx = blockStarts[i];
                int endIdx;

                // 基本块从startIdx开始,到下一个入口点前一条指令结束
                if (i + 1 < blockStarts.Count)
                {
                    endIdx = blockStarts[i + 1] - 1;
                }
                else
                {
                    // 最后一个基本块延伸到方法末尾
                    endIdx = instructions.Length - 1;
                }

                // 创建基本块并建立指令到块的映射
                if (endIdx >= startIdx && endIdx < instructions.Length)
                {
                    var block = new BasicBlock
                    {
                        BlockId = cfg.Blocks.Count,
                        OriginalIndex = cfg.Blocks.Count,
                        StartIndex = startIdx,
                        EndIndex = endIdx
                    };

                    for (int j = startIdx; j <= endIdx; j++)
                    {
                        block.Instructions.Add(instructions[j]);
                        // 建立指令索引到基本块ID的映射,便于后续查找
                        cfg.InstructionToBlock[j] = block.BlockId;
                    }

                    cfg.Blocks.Add(block);
                }
            }

            // 边界情况:如果没有识别到任何入口点(如空方法),创建一个包含所有指令的默认块
            if (cfg.Blocks.Count == 0 && instructions.Length > 0)
            {
                var block = new BasicBlock
                {
                    BlockId = 0,
                    OriginalIndex = 0,
                    StartIndex = 0,
                    EndIndex = instructions.Length - 1
                };

                for (int j = 0; j < instructions.Length; j++)
                {
                    block.Instructions.Add(instructions[j]);
                    cfg.InstructionToBlock[j] = 0;
                }

                cfg.Blocks.Add(block);
            }

            // 第四步:收集异常处理信息
            CollectExceptionInfo(instructions, cfg, instructionIndexToByteOffset);
            
            // 第五步:构建控制流边
            BuildControlFlowEdges(instructions, cfg);
            
            // 第六步:计算可达性(从入口块开始遍历所有可达块)
            cfg.ComputeReachability();

            return cfg;
        }

        
        /// <summary>
        /// 收集异常处理块的信息
        /// 
        /// 使用栈来跟踪嵌套的try块:
        /// - 遇到try_enter时,将try块的索引压栈
        /// - 遇到catch_enter/finally_enter时,栈顶的try块就是对应的外层try
        /// - 遇到try_exit/catch_exit时,不弹出栈(因为正常执行流会继续)
        /// - 遇到finally_exit时,弹出栈(表示这个try-catch-finally结构结束)
        /// </summary>
        private static void CollectExceptionInfo(Instruction[] instructions, ControlFlowGraph cfg, int[] instructionIndexToByteOffset)
        {
            // 使用栈来跟踪当前嵌套的try块
            var tryEnterStack = new Stack<int>();

            for (int i = 0; i < instructions.Length; i++)
            {
                var ins = instructions[i];

                // try块入口:创建异常信息并压栈
                if (ins.INS_Code == INS_Code.try_enter)
                {
                    var tryEnter = (INS_Try_Enter)ins;
                    var excInfo = new ExceptionBlockInfo
                    {
                        BlockType = ExceptionBlockType.TryBlock,
                        TryEnterIndex = i,
                        CatchPc = tryEnter.catch_pc,
                        FinallyPc = tryEnter.finally_pc,
                        FinallyExitPc = tryEnter.finally_exit_pc
                    };
                    cfg.TryEnterToInfo[i] = excInfo;
                    tryEnterStack.Push(i);
                }
                // catch块入口:关联到栈顶的try块
                else if (ins.INS_Code == INS_Code.catch_enter)
                {
                    if (tryEnterStack.Count > 0)
                    {
                        int tryIdx = tryEnterStack.Peek();
                        if (cfg.TryEnterToInfo.TryGetValue(tryIdx, out var tryInfo))
                        {
                            tryInfo.CatchEnterIndex = i;
                        }
                    }
                }
                // finally块入口:关联到栈顶的try块
                else if (ins.INS_Code == INS_Code.finally_enter)
                {
                    if (tryEnterStack.Count > 0)
                    {
                        int tryIdx = tryEnterStack.Peek();
                        if (cfg.TryEnterToInfo.TryGetValue(tryIdx, out var tryInfo))
                        {
                            tryInfo.FinallyEnterIndex = i;
                        }
                    }
                }
                // try块退出:标记退出位置
                else if (ins.INS_Code == INS_Code.try_exit)
                {
                    if (tryEnterStack.Count > 0)
                    {
                        int tryIdx = tryEnterStack.Peek();
                        if (cfg.TryEnterToInfo.TryGetValue(tryIdx, out var tryInfo))
                        {
                            tryInfo.TryExitIndex = i;
                        }
                    }
                }
                // catch块退出:标记退出位置
                else if (ins.INS_Code == INS_Code.catch_exit)
                {
                    if (tryEnterStack.Count > 0)
                    {
                        int tryIdx = tryEnterStack.Peek();
                        if (cfg.TryEnterToInfo.TryGetValue(tryIdx, out var tryInfo))
                        {
                            tryInfo.CatchExitIndex = i;
                        }
                    }
                }
                // finally块退出:弹出栈(该try-catch-finally结构结束)
                else if (ins.INS_Code == INS_Code.finally_exit)
                {
                    if (tryEnterStack.Count > 0)
                    {
                        int tryIdx = tryEnterStack.Pop();
                        if (cfg.TryEnterToInfo.TryGetValue(tryIdx, out var tryInfo))
                        {
                            tryInfo.FinallyExitIndex = i;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 构建控制流边
        /// 
        /// 分析每个基本块的最后一条指令,确定该块的执行后继:
        /// - goto_flag:无条件跳转到flag目标
        /// - if_true_goto/if_false_goto:条件跳转 + 可能存在fall-through
        /// - return_void/return_value:方法返回,无后继
        /// - throw_error:异常抛出,无后继
        /// - try_exit:try块退出,可能跳转到catch或finally
        /// - catch_exit:catch块退出,可能跳转到finally
        /// - finally_exit:finally块退出,无额外后继
        /// - 其他指令:默认有fall-through到下一块
        /// </summary>
        private static void BuildControlFlowEdges(Instruction[] instructions, ControlFlowGraph cfg)
        {
            // 构建flag_id到指令索引的映射,用于跳转目标查找
            var flagToInstructionIndex = new Dictionary<int, int>();

            for (int i = 0; i < instructions.Length; i++)
            {
                if (instructions[i].INS_Code == INS_Code.flag)
                {
                    var flag = (INS_Flag)instructions[i];
                    flagToInstructionIndex[flag.flag_id] = i;
                }
            }

            // 构建入口索引到基本块的映射,用于快速查找fall-through块
            var blocksByStartIndex = new Dictionary<int, BasicBlock>();
            foreach (var block in cfg.Blocks)
            {
                blocksByStartIndex[block.StartIndex] = block;
            }

            // 遍历每个基本块,根据最后一条指令类型构建控制流边
            for (int i = 0; i < cfg.Blocks.Count; i++)
            {
                var block = cfg.Blocks[i];
                
                // 跳过空块
                if (block.Instructions.Count == 0)
                    continue;

                // 获取块的终止指令(最后一条指令)
                var lastIns = block.Instructions[block.Instructions.Count - 1];

                switch (lastIns.INS_Code)
                {
                    // 无条件跳转:只跳转到目标块,无fall-through
                    case INS_Code.goto_flag:
                        {
                            var gotoIns = (INS_Goto)lastIns;
                            int targetIdx;
                            if (flagToInstructionIndex.TryGetValue(gotoIns.flag_id, out targetIdx))
                            {
                                if (cfg.InstructionToBlock.TryGetValue(targetIdx, out int targetBlockId))
                                {
                                    var targetBlock = cfg.Blocks[targetBlockId];
                                    block.Successors.Add(targetBlock);
                                    targetBlock.Predecessors.Add(block);
                                    block.JumpTargetFlagId = gotoIns.flag_id;
                                }
                            }
                        }
                        break;

                    // 条件为真时跳转:有跳转目标 + fall-through(如果存在)
                    case INS_Code.if_true_goto:
                        {
                            var ifIns = (INS_If_True_Goto)lastIns;
                            int targetIdx;
                            if (flagToInstructionIndex.TryGetValue(ifIns.flag_id, out targetIdx))
                            {
                                if (cfg.InstructionToBlock.TryGetValue(targetIdx, out int targetBlockId))
                                {
                                    var targetBlock = cfg.Blocks[targetBlockId];
                                    block.Successors.Add(targetBlock);
                                    targetBlock.Predecessors.Add(block);
                                    block.JumpTargetFlagId = ifIns.flag_id;
                                }
                            }

                            // 尝试添加fall-through边(条件为假时继续执行下一块)
                            var fallThroughBlock = FindFallThroughBlock(block, cfg, blocksByStartIndex);
                            if (fallThroughBlock != null)
                            {
                                block.Successors.Add(fallThroughBlock);
                                fallThroughBlock.Predecessors.Add(block);
                                block.HasFallThrough = true;
                            }
                        }
                        break;

                    // 条件为假时跳转:有跳转目标 + fall-through(如果存在)
                    case INS_Code.if_false_goto:
                        {
                            var ifIns = (INS_If_False_Goto)lastIns;
                            int targetIdx;
                            if (flagToInstructionIndex.TryGetValue(ifIns.flag_id, out targetIdx))
                            {
                                if (cfg.InstructionToBlock.TryGetValue(targetIdx, out int targetBlockId))
                                {
                                    var targetBlock = cfg.Blocks[targetBlockId];
                                    block.Successors.Add(targetBlock);
                                    targetBlock.Predecessors.Add(block);
                                    block.JumpTargetFlagId = ifIns.flag_id;
                                }
                            }

                            // 尝试添加fall-through边(条件为真时继续执行下一块)
                            var fallThroughBlock = FindFallThroughBlock(block, cfg, blocksByStartIndex);
                            if (fallThroughBlock != null)
                            {
                                block.Successors.Add(fallThroughBlock);
                                fallThroughBlock.Predecessors.Add(block);
                                block.HasFallThrough = true;
                            }
                        }
                        break;

                    // 返回/异常/finally退出:控制流终止,无后继块
                    case INS_Code.return_void:
                    case INS_Code.return_value:
                    case INS_Code.throw_error:
                    case INS_Code.finally_exit:
                        break;

                    // try块退出:可能跳转到catch或finally
                    case INS_Code.try_exit:
                        {
                            ProcessTryExit(block, cfg);
                        }
                        break;

                    // catch块退出:可能跳转到finally
                    case INS_Code.catch_exit:
                        {
                            ProcessCatchExit(block, cfg);
                        }
                        break;

                    // 默认情况:非终止指令,存在fall-through到下一块
                    default:
                        var fallThroughBlock2 = FindFallThroughBlock(block, cfg, blocksByStartIndex);
                        if (fallThroughBlock2 != null)
                        {
                            block.Successors.Add(fallThroughBlock2);
                            fallThroughBlock2.Predecessors.Add(block);
                            block.HasFallThrough = true;
                        }
                        break;
                }
            }

            // 为异常块设置异常信息
            FixUpExceptionBlocks(cfg);
        }

        /// <summary>
        /// 查找块的fall-through后继
        /// 
        /// Fall-through块是指令序列中当前块结束后立即执行的下一个块。
        /// 通过检查当前块 EndIndex + 1 位置是否存在基本块入口来确定。
        /// </summary>
        /// <param name="block">当前基本块</param>
        /// <param name="cfg">控制流图</param>
        /// <param name="blocksByStartIndex">入口索引到块的映射</param>
        /// <returns>fall-through块,如果没有则返回null</returns>
        private static BasicBlock FindFallThroughBlock(BasicBlock block, ControlFlowGraph cfg, Dictionary<int, BasicBlock> blocksByStartIndex)
        {
            // 下一条指令的索引 = 当前块最后一条指令索引 + 1
            int nextInstructionIndex = block.EndIndex + 1;
            if (nextInstructionIndex >= cfg.InstructionToBlock.Count)
                return null;

            // 查找是否存在以该索引为入口的基本块
            if (blocksByStartIndex.TryGetValue(nextInstructionIndex, out var fallThroughBlock))
                return fallThroughBlock;

            return null;
        }

        /// <summary>
        /// 处理try块退出时的控制流
        /// 
        /// 当try块正常退出(exit)时:
        /// - 如果存在finally块,跳转到finally
        /// - 如果存在catch块,跳转到catch
        /// </summary>
        private static void ProcessTryExit(BasicBlock block, ControlFlowGraph cfg)
        {
            var tryExit = (INS_Try_Exit)block.Instructions[block.Instructions.Count - 1];

            // 查找包含此块的try-catch-finally结构
            var tryEnterInfo = FindEnclosingTryInfo(block, cfg);
            if (tryEnterInfo == null)
                return;

            // 添加到finally块的边
            if (tryEnterInfo.FinallyEnterIndex > 0 && cfg.InstructionToBlock.TryGetValue(tryEnterInfo.FinallyEnterIndex, out int finallyBlockId))
            {
                var finallyBlock = cfg.Blocks[finallyBlockId];
                block.Successors.Add(finallyBlock);
                finallyBlock.Predecessors.Add(block);
            }

            // 添加到catch块的边
            if (tryEnterInfo.CatchEnterIndex > 0 && cfg.InstructionToBlock.TryGetValue(tryEnterInfo.CatchEnterIndex, out int catchBlockId))
            {
                var catchBlock = cfg.Blocks[catchBlockId];
                block.Successors.Add(catchBlock);
                catchBlock.Predecessors.Add(block);
            }
        }

        /// <summary>
        /// 处理catch块退出时的控制流
        /// 
        /// 当catch块退出(exit)时:
        /// - 如果存在finally块,跳转到finally
        /// </summary>
        private static void ProcessCatchExit(BasicBlock block, ControlFlowGraph cfg)
        {
            // 查找包含此块的try-catch-finally结构
            var tryEnterInfo = FindEnclosingTryInfo(block, cfg);
            if (tryEnterInfo == null)
                return;

            // 添加到finally块的边
            if (tryEnterInfo.FinallyEnterIndex > 0 && cfg.InstructionToBlock.TryGetValue(tryEnterInfo.FinallyEnterIndex, out int finallyBlockId))
            {
                var finallyBlock = cfg.Blocks[finallyBlockId];
                block.Successors.Add(finallyBlock);
                finallyBlock.Predecessors.Add(block);
            }
        }

        /// <summary>
        /// 查找包围给定块的最内层try信息
        /// 
        /// 对于嵌套的try-catch-finally,需要返回最内层的try块。
        /// 通过以下条件筛选候选:
        /// - 块的起始索引 >= try入口索引
        /// - 块的结束索引 <= try退出索引
        /// 
        /// 返回时按TryEnterIndex降序排序,确保返回最内层的try块。
        /// </summary>
        /// <param name="block">要查找的基本块</param>
        /// <param name="cfg">控制流图</param>
        /// <returns>最内层的ExceptionBlockInfo,如果未找到则返回null</returns>
        private static ExceptionBlockInfo FindEnclosingTryInfo(BasicBlock block, ControlFlowGraph cfg)
        {
            var candidates = new List<ExceptionBlockInfo>();
            
            // 收集所有可能的候选try块
            foreach (var kvp in cfg.TryEnterToInfo)
            {
                var tryInfo = kvp.Value;
                if (block.StartIndex >= tryInfo.TryEnterIndex && 
                    (tryInfo.TryExitIndex == 0 || block.EndIndex <= tryInfo.TryExitIndex))
                {
                    candidates.Add(tryInfo);
                }
            }

            if (candidates.Count == 0)
                return null;

            // 按TryEnterIndex降序排序,返回最内层的try块
            candidates.Sort((a, b) => b.TryEnterIndex.CompareTo(a.TryEnterIndex));
            return candidates[0];
        }

        /// <summary>
        /// 为基本块设置异常信息
        /// 
        /// 遍历所有try-catch-finally结构,
        /// 为对应的try块、catch块、finally块设置ExceptionInfo。
        /// </summary>
        private static void FixUpExceptionBlocks(ControlFlowGraph cfg)
        {
            foreach (var kvp in cfg.TryEnterToInfo)
            {
                var tryInfo = kvp.Value;

                // 为try块设置异常信息
                if (tryInfo.TryEnterIndex >= 0 && cfg.InstructionToBlock.TryGetValue(tryInfo.TryEnterIndex, out int tryBlockId))
                {
                    cfg.Blocks[tryBlockId].ExceptionInfo = tryInfo;
                }

                // 为catch块创建并设置异常信息
                if (tryInfo.CatchEnterIndex > 0 && cfg.InstructionToBlock.TryGetValue(tryInfo.CatchEnterIndex, out int catchBlockId))
                {
                    var catchExcInfo = new ExceptionBlockInfo
                    {
                        BlockType = ExceptionBlockType.CatchBlock,
                        TryEnterIndex = tryInfo.TryEnterIndex,
                        CatchPc = tryInfo.CatchPc,
                        FinallyPc = tryInfo.FinallyPc
                    };
                    cfg.Blocks[catchBlockId].ExceptionInfo = catchExcInfo;
                }

                // 为finally块创建并设置异常信息
                if (tryInfo.FinallyEnterIndex > 0 && cfg.InstructionToBlock.TryGetValue(tryInfo.FinallyEnterIndex, out int finallyBlockId))
                {
                    var finallyExcInfo = new ExceptionBlockInfo
                    {
                        BlockType = ExceptionBlockType.FinallyBlock,
                        TryEnterIndex = tryInfo.TryEnterIndex,
                        CatchPc = tryInfo.CatchPc,
                        FinallyPc = tryInfo.FinallyPc
                    };
                    cfg.Blocks[finallyBlockId].ExceptionInfo = finallyExcInfo;
                }
            }
        }
    }
}
