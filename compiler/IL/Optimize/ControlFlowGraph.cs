using juicescript.ABC;
using juicescript.ABC.INS;
using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace juicescript.compiler.IL.Optimize
{
    public class ControlFlowGraph
    {
        
        public ASMethod Method { get; set; }
        public List<BasicBlock> Blocks { get; set; }
        
       
        public ControlFlowGraph(ASMethod method)
        {
            Method = method;
            Blocks = new List<BasicBlock>();
            
            
        }

		internal void DeathCodeErase()
		{
            //return;
            HashSet<int> unReachableTry = new HashSet<int>();

            foreach (BasicBlock block in Blocks.OrderBy(b => b.OriginalIndex)) 
            {
                if (!block.IsReachable)
                {
                    if (block.Instructions[0].INS_Code == INS_Code.END)
                    {
                        continue;
                    }

                    if (block.Instructions[0].INS_Code == INS_Code.try_enter)
                    {
                        unReachableTry.Add(block.TryBlockId);
                        
                    }

                    if (unReachableTry.Contains(block.TryBlockId))
                    {
                        block.Instructions.Clear();
                    }
                    else
                    {
                        var newinstructions = block.Instructions.Where(
                            b=>b.INS_Code == INS_Code.try_enter ||
                                b.INS_Code == INS_Code.try_exit ||
                                b.INS_Code == INS_Code.catch_enter ||
                                b.INS_Code == INS_Code.catch_exit ||
                                b.INS_Code == INS_Code.finally_enter ||
                                b.INS_Code == INS_Code.finally_exit
                            ).ToList();

                        block.Instructions = newinstructions;

                    }

                }
            }
		}

		/// <summary>
		/// 活跃槽分析+图着色
		/// </summary>
		/// <returns></returns>
		internal int GraphColoring()
		{
			var sortedBlocks = Blocks.OrderByDescending(b => b.OriginalIndex).ToArray();
            if (sortedBlocks.Length == 0)
                return 0;

			//ComputeLiveness(sortedBlocks);
			BuildInterferenceGraph();
			ColorGraph();

			if (allocation.Count == 0)
                return 0;
			return allocation.Values.Max() + 1;

		}

		internal void ReMapping()
		{
			var sortedBlocks = Blocks.OrderByDescending(b => b.OriginalIndex).ToArray();
			for (int i = 0; i < sortedBlocks.Length; i++)
			{
				var block = sortedBlocks[i];
				for (int j = 0; j < block.Instructions.Count; j++)
				{
					block.Instructions[j].RemappingSlots(allocation);
				}
			}
		}


		private void BuildInterferenceGraph()
		{
			interferenceGraph = new Dictionary<int, HashSet<int>>();

			var tempCFG = BuildTemporaryCFGForInstructionLevel();

			tempCFG.ComputeLivenessForAllBlocks();

			foreach (var block in tempCFG.Blocks.Where(b => b.IsReachable))
			{
				var ins = block.Instructions[0];
				var defs = ins.GetDef();
				var uses = ins.GetUse();

				HashSet<int> allSlotsInInstruction = new HashSet<int>();

				if (defs != null)
				{
					foreach (var d in defs)
					{
						if (d.index >= 0) allSlotsInInstruction.Add(d.index);
					}
				}
				if (uses != null)
				{
					foreach (var u in uses)
					{
						if (u.index >= 0) allSlotsInInstruction.Add(u.index);
					}
				}

				foreach (var d in allSlotsInInstruction)
				{
					foreach (var other in allSlotsInInstruction)
					{
						if (d != other)
						{
							AddInterferenceEdge(d, other);
						}
					}
				}

				if (defs != null)
				{
					foreach (var d in defs)
					{
						if (d.index < 0) continue;
						foreach (var l in block.LiveIn)
						{
							if (l < 0) continue;
							if (d.index != l)
							{
								AddInterferenceEdge(d.index, l);
							}
						}
					}
				}
			}

			HashSet<int> allUsedSlots = new HashSet<int>();
			foreach (var block in Blocks.Where(b => b.IsReachable))
			{
				foreach (var ins in block.Instructions)
				{
					var defs = ins.GetDef();
					if (defs != null)
					{
						foreach (var d in defs)
						{
							if (d.index >= 0)
								allUsedSlots.Add(d.index);
						}
					}
					var uses = ins.GetUse();
					if (uses != null)
					{
						foreach (var u in uses)
						{
							if (u.index >= 0)
								allUsedSlots.Add(u.index);
						}
					}
				}
			}

			foreach (var slot in allUsedSlots)
			{
				if (!interferenceGraph.ContainsKey(slot))
					interferenceGraph[slot] = new HashSet<int>();
			}
		}

		internal void ComputeLivenessForAllBlocks()
		{
			if (Blocks.Count == 0)
				return;

			var sortedBlocks = Blocks.OrderByDescending(b => b.OriginalIndex).ToArray();
			ComputeLivenessStatic(sortedBlocks);
		}

		internal static void ComputeLivenessStatic(BasicBlock[] sortedBlocks)
		{
			Dictionary<BasicBlock, HashSet<int>> liveInAnalysis = new Dictionary<BasicBlock, HashSet<int>>();
			Dictionary<BasicBlock, HashSet<int>> liveOutAnalysis = new Dictionary<BasicBlock, HashSet<int>>();
			
			foreach (var block in sortedBlocks)
			{
				liveInAnalysis[block] = new HashSet<int>();
				liveOutAnalysis[block] = new HashSet<int>();
			}
			
			bool changed;
			int iterations = 0;
			const int maxIterations = 1000;
			
			do
			{
				changed = false;
				iterations++;
				
				foreach (var block in sortedBlocks)
				{
					if (!block.IsReachable)
						continue;
					
					HashSet<int> newLiveOut = new HashSet<int>();
					foreach (var succ in block.Successors)
					{
						if (liveInAnalysis.ContainsKey(succ))
						{
							foreach (var slot in liveInAnalysis[succ])
							{
								newLiveOut.Add(slot);
							}
						}
					}
					
					HashSet<int> use = new HashSet<int>();
					HashSet<int> def = new HashSet<int>();
					HashSet<int> onlyUse = new HashSet<int>();
					ComputeBlockUseDef(block, use, def, onlyUse);
					
					foreach (var d in def)
					{
						if (use.Contains(d))
						{
							newLiveOut.Add(d);
						}
					}
					
					HashSet<int> newLiveIn = new HashSet<int>(onlyUse);
					foreach (var slot in newLiveOut)
					{
						if (!def.Contains(slot))
						{
							newLiveIn.Add(slot);
						}
					}
					
					if (!SetsEqual(liveInAnalysis[block], newLiveIn))
					{
						liveInAnalysis[block] = newLiveIn;
						changed = true;
					}
					if (!SetsEqual(liveOutAnalysis[block], newLiveOut))
					{
						liveOutAnalysis[block] = newLiveOut;
						changed = true;
					}
				}
			} while (changed && iterations < maxIterations);

			iterations = 0;
			bool changed2 = true;
			while (changed2 && iterations < maxIterations)
			{
				changed2 = false;
				iterations++;
				
				foreach (var block in sortedBlocks)
				{
					if (!block.IsReachable)
						continue;
					
					HashSet<int> use = new HashSet<int>();
					HashSet<int> def = new HashSet<int>();
					HashSet<int> onlyUse = new HashSet<int>();
					ComputeBlockUseDef(block, use, def, onlyUse);
					
					HashSet<int> newLiveOut = new HashSet<int>();
					foreach (var succ in block.Successors)
					{
						if (liveInAnalysis.ContainsKey(succ))
						{
							foreach (var slot in liveInAnalysis[succ])
							{
								newLiveOut.Add(slot);
							}
						}
					}
					
					foreach (var d in def)
					{
						if (use.Contains(d))
						{
							newLiveOut.Add(d);
						}
					}
					
					HashSet<int> newLiveIn = new HashSet<int>(onlyUse);
					foreach (var slot in newLiveOut)
					{
						if (!def.Contains(slot))
						{
							newLiveIn.Add(slot);
						}
					}
					
					if (!SetsEqual(liveInAnalysis[block], newLiveIn))
					{
						liveInAnalysis[block] = newLiveIn;
						changed2 = true;
					}
					if (!SetsEqual(liveOutAnalysis[block], newLiveOut))
					{
						liveOutAnalysis[block] = newLiveOut;
						changed2 = true;
					}
				}
			}

			foreach (var block in sortedBlocks)
			{
				block.LiveIn = liveInAnalysis[block];
				block.LiveOut = liveOutAnalysis[block];
			}
		}

		private ControlFlowGraph BuildTemporaryCFGForInstructionLevel()
		{
			var tempCFG = new ControlFlowGraph(Method);

			Dictionary<BasicBlock, BasicBlock> blockMapping = new Dictionary<BasicBlock, BasicBlock>();

			foreach (var block in Blocks)
			{
				var newBlock = new BasicBlock
				{
					BlockId = tempCFG.Blocks.Count,
					OriginalIndex = block.OriginalIndex,
					IsReachable = block.IsReachable,
					Instructions = new List<Instruction>(block.Instructions),
					Predecessors = new List<BasicBlock>(),
					Successors = new List<BasicBlock>()
				};
				newBlock.LiveIn = new HashSet<int>();
				newBlock.LiveOut = new HashSet<int>();
				tempCFG.Blocks.Add(newBlock);
				blockMapping[block] = newBlock;
			}

			int totalinstructions = Blocks.Sum(b => b.Instructions.Count) + Blocks.Count;

			foreach (var block in Blocks)
			{
				var newBlock = blockMapping[block];

				foreach (var pred in block.Predecessors)
				{
					if (blockMapping.TryGetValue(pred, out var newPred))
					{
						if (!newBlock.Predecessors.Contains(newPred))
							newBlock.Predecessors.Add(newPred);
						if (!newPred.Successors.Contains(newBlock))
							newPred.Successors.Add(newBlock);
					}
				}
				foreach (var succ in block.Successors)
				{
					if (blockMapping.TryGetValue(succ, out var newSucc))
					{
						if (!newBlock.Successors.Contains(newSucc))
							newBlock.Successors.Add(newSucc);
						if (!newSucc.Predecessors.Contains(newBlock))
							newSucc.Predecessors.Add(newBlock);
					}
				}
			}

			var blocksToSplit = tempCFG.Blocks.Where(b => b.IsReachable && b.Instructions.Count > 1).ToList();
			int seed = 0;
			foreach (var block in blocksToSplit)
			{
				int idx = tempCFG.Blocks.IndexOf(block);
				tempCFG.Blocks.RemoveAt(idx);

				var preds = block.Predecessors.ToList();
				var succs = block.Successors.ToList();

				foreach (var pred in preds)
				{
					pred.Successors.Remove(block);
				}
				foreach (var succ in succs)
				{
					succ.Predecessors.Remove(block);
				}

				BasicBlock prevBlock = null;
				for (int i = 0; i < block.Instructions.Count; i++)
				{
					var newB = new BasicBlock
					{
						BlockId = tempCFG.Blocks.Count,
						OriginalIndex =  totalinstructions  + (++seed),
						IsReachable = block.IsReachable,
						Instructions = new List<Instruction> { block.Instructions[i] },
						Predecessors = new List<BasicBlock>(),
						Successors = new List<BasicBlock>()
					};
					newB.LiveIn = new HashSet<int>();
					newB.LiveOut = new HashSet<int>();

					if (i == 0)
					{
						foreach (var pred in preds)
						{
							pred.Successors.Add(newB);
							newB.Predecessors.Add(pred);
						}
					}
					else
					{
						prevBlock.Successors.Add(newB);
						newB.Predecessors.Add(prevBlock);
					}

					if (i == block.Instructions.Count - 1)
					{
						foreach (var succ in succs)
						{
							succ.Predecessors.Add(newB);
							newB.Successors.Add(succ);
						}
					}

					prevBlock = newB;
					tempCFG.Blocks.Add(newB);
				}
			}

			return tempCFG;
		}


		private void AddInterferenceEdge(int a, int b)
		{
			if (!interferenceGraph.ContainsKey(a))
				interferenceGraph[a] = new HashSet<int>();
			if (!interferenceGraph.ContainsKey(b))
				interferenceGraph[b] = new HashSet<int>();

			interferenceGraph[a].Add(b);
			interferenceGraph[b].Add(a);
		}

		private int GetMaxSlotIndex()
		{
			int max = 0;
			foreach (var block in Blocks.Where(b => b.IsReachable))
			{
				foreach (var ins in block.Instructions)
				{
					var defs = ins.GetDef();
					if (defs != null)
					{
						foreach (var d in defs)
						{
							if (d.index > max) max = d.index;
						}
					}
					var uses = ins.GetUse();
					if (uses != null)
					{
						foreach (var u in uses)
						{
							if (u.index > max) max = u.index;
						}
					}
				}
			}
			return max;
		}

		private void ColorGraph()
		{
			int maxSlot = GetMaxSlotIndex();
			coloringK = maxSlot + 1;
			int K = coloringK;

			allocation = new Dictionary<int, int>();
			var remaining = new HashSet<int>(interferenceGraph.Keys);
			var stack = new Stack<int>();

			while (remaining.Count > 0)
			{
				var node = SelectNode(remaining, K);
				if (node == -1)
				{
					node = remaining.First();
				}
				stack.Push(node);
				remaining.Remove(node);
			}

			while (stack.Count > 0)
			{
				var node = stack.Pop();
				var usedColors = new HashSet<int>();
				if (interferenceGraph.ContainsKey(node))
				{
					foreach (var neighbor in interferenceGraph[node])
					{
						if (allocation.ContainsKey(neighbor))
						{
							usedColors.Add(allocation[neighbor]);
						}
					}
				}

				int color = 0;
				while (usedColors.Contains(color))
				{
					color++;
				}
				allocation[node] = color;
				if (color >= K)
				{
					K = color + 1;
				}
			}
		}

		private int SelectNode(HashSet<int> remaining, int K)
		{
			int bestNode = -1;
			int bestDegree = int.MaxValue;
			
			foreach (var node in remaining)
			{
				int degree = interferenceGraph.ContainsKey(node) 
					? interferenceGraph[node].Count(n => remaining.Contains(n)) 
					: 0;
				if (degree < bestDegree)
				{
					bestDegree = degree;
					bestNode = node;
					if (degree < K)
					{
						return node;
					}
				}
			}
			return bestNode;
		}

		//private Dictionary<BasicBlock, HashSet<int>> liveInAnalysis;
		//private Dictionary<BasicBlock, HashSet<int>> liveOutAnalysis;

		private Dictionary<int, HashSet<int>> interferenceGraph;
		private Dictionary<int, int> allocation;
		private int coloringK;

		//private void ComputeLiveness(BasicBlock[] sortedBlocks)
		//{
		//	// 初始化每个 block 的 LiveIn 和 LiveOut
		//	liveInAnalysis = new Dictionary<BasicBlock, HashSet<int>>();
		//	liveOutAnalysis = new Dictionary<BasicBlock, HashSet<int>>();
			
		//	foreach (var block in sortedBlocks)
		//	{
		//		liveInAnalysis[block] = new HashSet<int>();
		//		liveOutAnalysis[block] = new HashSet<int>();
		//	}
			
		//	// 迭代直到收敛
		//	bool changed;
		//	int iterations = 0;
		//	const int maxIterations = 100;
			
		//	// 第一次迭代计算基本的 LiveOut（基于后继的 LiveIn）
		//	do
		//	{
		//		changed = false;
		//		iterations++;
				
		//		foreach (var block in sortedBlocks)
		//		{
		//			if (!block.IsReachable)
		//				continue;
					
		//			HashSet<int> newLiveOut = new HashSet<int>();
		//			foreach (var succ in block.Successors)
		//			{
		//				if (liveInAnalysis.ContainsKey(succ))
		//				{
		//					foreach (var slot in liveInAnalysis[succ])
		//					{
		//						newLiveOut.Add(slot);
		//					}
		//				}
		//			}
					
		//			HashSet<int> use = new HashSet<int>();
		//			HashSet<int> def = new HashSet<int>();
		//			HashSet<int> onlyUse = new HashSet<int>();
		//			ComputeBlockUseDef(block, use, def, onlyUse);
					
		//			// Also add def slots that are also used within the block
		//			foreach (var d in def)
		//			{
		//				if (use.Contains(d))
		//				{
		//					newLiveOut.Add(d);
		//				}
		//			}
					
		//			HashSet<int> newLiveIn = new HashSet<int>(onlyUse);
		//			foreach (var slot in newLiveOut)
		//			{
		//				if (!def.Contains(slot))
		//				{
		//					newLiveIn.Add(slot);
		//				}
		//			}
					
		//			if (!SetsEqual(liveInAnalysis[block], newLiveIn))
		//			{
		//				liveInAnalysis[block] = newLiveIn;
		//				changed = true;
		//			}
		//			if (!SetsEqual(liveOutAnalysis[block], newLiveOut))
		//			{
		//				liveOutAnalysis[block] = newLiveOut;
		//				changed = true;
		//			}
		//		}
		//	} while (changed && iterations < maxIterations);

		//	iterations = 0;
		//	bool changed2 = true;
		//	while (changed2 && iterations < maxIterations)
		//	{
		//		changed2 = false;
		//		iterations++;
				
		//		foreach (var block in sortedBlocks)
		//		{
		//			if (!block.IsReachable)
		//				continue;
					
		//			HashSet<int> use = new HashSet<int>();
		//			HashSet<int> def = new HashSet<int>();
		//			HashSet<int> onlyUse = new HashSet<int>();
		//			ComputeBlockUseDef(block, use, def, onlyUse);
					
		//			HashSet<int> newLiveOut = new HashSet<int>();
		//			foreach (var succ in block.Successors)
		//			{
		//				if (liveInAnalysis.ContainsKey(succ))
		//				{
		//					foreach (var slot in liveInAnalysis[succ])
		//					{
		//						newLiveOut.Add(slot);
		//					}
		//				}
		//			}
					
		//			foreach (var d in def)
		//			{
		//				if (use.Contains(d))
		//				{
		//					newLiveOut.Add(d);
		//				}
		//			}
					
		//			HashSet<int> newLiveIn = new HashSet<int>(onlyUse);
		//			foreach (var slot in newLiveOut)
		//			{
		//				if (!def.Contains(slot))
		//				{
		//					newLiveIn.Add(slot);
		//				}
		//			}
					
		//			if (!SetsEqual(liveInAnalysis[block], newLiveIn))
		//			{
		//				liveInAnalysis[block] = newLiveIn;
		//				changed2 = true;
		//			}
		//			if (!SetsEqual(liveOutAnalysis[block], newLiveOut))
		//			{
		//				liveOutAnalysis[block] = newLiveOut;
		//				changed2 = true;
		//			}
		//		}
		//	}

		//	foreach (var block in sortedBlocks)
		//	{
		//		block.LiveIn = liveInAnalysis[block];
		//		block.LiveOut = liveOutAnalysis[block];
		//	}
		//}

		private static void ComputeBlockUseDef(BasicBlock block, HashSet<int> use, HashSet<int> def, HashSet<int> onlyUse)
		{
			// 对于每条指令：Use 是操作数，Def 是结果
			foreach (var ins in block.Instructions)
			{
				// 添加 Use
				var uses = ins.GetUse();
				if (uses != null)
				{
					foreach (var u in uses)
					{
						use.Add(u.index);
						onlyUse.Add(u.index);
					}
				}
				
				// 添加 Def
				var defs = ins.GetDef();
				if (defs != null)
				{
					foreach (var d in defs)
					{
						def.Add(d.index);
					}
				}
			}
			
			// OnlyUse = Use - Def (在块中使用但不在块中定义的 slot)
			onlyUse.ExceptWith(def);
		}

		private static bool SetsEqual(HashSet<int> a, HashSet<int> b)
		{
			if (a.Count != b.Count)
				return false;
			foreach (var item in a)
			{
				if (!b.Contains(item))
					return false;
			}
			return true;
		}


		public Instruction[] FlattenInstructions()
        {
            var sortedBlocks = Blocks.OrderBy(b => b.OriginalIndex).ToList();
            var result = new List<Instruction>();
            foreach (var block in sortedBlocks)
            {
                result.AddRange(block.Instructions);
            }
            return result.ToArray();
        }

        public string GetMermaid(string methodName)
        {
            
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("    <meta charset=\"utf-8\">");
            sb.AppendLine($"    <title>Control Flow Graph - {methodName}</title>");
            sb.AppendLine("    <script src=\"https://cdn.jsdelivr.net/npm/mermaid/dist/mermaid.min.js\"></script>");
            sb.AppendLine("    <style>");
            sb.AppendLine("        body { font-family: Arial, sans-serif; margin: 20px; background: #f5f5f5; }");
            sb.AppendLine("        h1 { color: #333; }");
            sb.AppendLine("        .info { background: white; padding: 15px; border-radius: 5px; margin-bottom: 20px; }");
            sb.AppendLine("        .mermaid { background: white; padding: 20px; border-radius: 5px; overflow: auto; resize: both; position: relative;}");
            sb.AppendLine("        table { border-collapse: collapse; width: 100%; margin-top: 20px; }");
            sb.AppendLine("        th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
            sb.AppendLine("        th { background: #4CAF50; color: white; }");
            sb.AppendLine("        tr:nth-child(even) { background: #f2f2f2; }");
            sb.AppendLine("        .unreachable { background: #ffcccc; color: #999; text-decoration: line-through; }");
            sb.AppendLine("        .unreachable-cell { background: #fff0f0; }");
            sb.AppendLine("    </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("    <h1>Control Flow Graph</h1>");
            sb.AppendLine($"    <div class=\"info\">");
            sb.AppendLine($"        <p><strong>Method:</strong> {Method?.Name ?? "Unknown"}</p>");
            sb.AppendLine($"        <p><strong>Total Blocks:</strong> {Blocks.Count}</p>");
            sb.AppendLine($"        <p><strong>Reachable Blocks:</strong> {Blocks.Count(b => b.IsReachable)}</p>");
            sb.AppendLine($"    </div>");

            // Mermaid diagram
            sb.AppendLine("    <div class=\"mermaid\">");
            sb.AppendLine("    flowchart TB");
            sb.AppendLine("    classDef reachable fill:#90EE90,stroke:#333,stroke-width:2px;");
            sb.AppendLine("    classDef unreachable fill:#ffcccc,stroke:#999,stroke-width:1px,stroke-dasharray:5 5;");

            foreach (var block in Blocks)
            {
                string label = GetBlockLabel(block);
                sb.AppendLine($"        BB{block.BlockId}[\"{label}\"]");
            }

            foreach (var block in Blocks)
            {
                string classDef = block.IsReachable ? "reachable" : "unreachable";
                sb.AppendLine($"        class BB{block.BlockId} {classDef};");
            }

            foreach (var block in Blocks)
            {
                foreach (var succ in block.Successors)
                {
                    string edgeLabel = "";
                    if (block.JumpTargetFlagId.HasValue && succ == block.Successors.FirstOrDefault())
                    {
                        edgeLabel = "|jmp|";
                    }
                    else if (block.HasFallThrough && succ == block.Successors.LastOrDefault())
                    {
                        edgeLabel = "|ft|";
                    }
                    sb.AppendLine($"        BB{block.BlockId} -->{edgeLabel} BB{succ.BlockId}");
                }
            }

            sb.AppendLine("    </div>");

            // Block details table
            sb.AppendLine("    <h2>Block Details</h2>");
            sb.AppendLine("    <table>");
            sb.AppendLine("        <tr><th>Block</th><th>Range</th><th>Predecessors</th><th>Successors</th><th>Instructions</th></tr>");

			int start_index = 0;
            foreach (var block in Blocks)
            {
                string preds = block.Predecessors.Count == 0 ? "(none)" : string.Join(", ", block.Predecessors.Select(p => $"BB{p.BlockId}"));
                string succs = block.Successors.Count == 0 ? "(none)" : string.Join(", ", block.Successors.Select(s => $"BB{s.BlockId}"));

                string instructions = "";
                for (int idx = 0; idx < block.Instructions.Count; idx++)
                {
                    var ins = block.Instructions[idx];
                    instructions += $"{start_index + idx}: {ins}<br>";
                }
				start_index += block.Instructions.Count;

                string rowClass = block.IsReachable ? "" : "class=\"unreachable-cell\"";
                sb.AppendLine($"        <tr {rowClass}>");
                sb.AppendLine($"            <td>BB{block.BlockId}{(block.IsReachable ? "" : " (unreachable)")}</td>");
                sb.AppendLine($"            <td>{block.StartIndex}-{block.EndIndex}</td>");
                sb.AppendLine($"            <td>{preds}</td>");
                sb.AppendLine($"            <td>{succs}</td>");
                sb.AppendLine($"            <td>{instructions}</td>");
                sb.AppendLine($"        </tr>");
            }

            sb.AppendLine("    </table>");

            if (interferenceGraph != null && interferenceGraph.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("    <h2>Interference Graph & Coloring</h2>");

                bool graphSymmetric = true;
                foreach (var node in interferenceGraph.Keys)
                {
                    foreach (var neighbor in interferenceGraph[node])
                    {
                        if (!interferenceGraph.ContainsKey(neighbor) || !interferenceGraph[neighbor].Contains(node))
                        {
                            graphSymmetric = false;
                            break;
                        }
                    }
                    if (!graphSymmetric) break;
                }

                sb.AppendLine($"    <p><strong>Graph symmetric:</strong> {(graphSymmetric ? "YES" : "NO - ERROR!")}</p>");

                sb.AppendLine("    <table>");
                sb.AppendLine("        <tr><th>Original Slot</th><th>New Slot</th><th>Degree</th><th>Conflicts With</th></tr>");

                var sortedSlots = interferenceGraph.Keys.OrderBy(x => x);
                bool coloringValid = true;
                foreach (var slot in sortedSlots)
                {
                    var conflicts = interferenceGraph[slot].OrderBy(x => x);
                    string conflictStr = "{" + string.Join(",", conflicts) + "}";
                    int degree = interferenceGraph[slot].Count;
                    int newSlot = allocation != null && allocation.ContainsKey(slot) ? allocation[slot] : slot;
                    sb.AppendLine($"        <tr>");
                    sb.AppendLine($"            <td>{slot}</td>");
                    sb.AppendLine($"            <td>{newSlot}</td>");
                    sb.AppendLine($"            <td>{degree}</td>");
					sb.AppendLine($"            <td>{conflictStr}</td>");
					sb.AppendLine($"        </tr>");

                    if (allocation != null)
                    {
                        int slotColor = allocation[slot];
                        foreach (var conflict in conflicts)
                        {
                            if (allocation.ContainsKey(conflict) && allocation[conflict] == slotColor)
                            {
                                coloringValid = false;
                            }
                        }
                    }
                }
                sb.AppendLine("    </table>");

                sb.AppendLine("    <h3>Summary</h3>");
                sb.AppendLine($"    <p><strong>K (colors available):</strong> {coloringK}</p>");
                sb.AppendLine($"    <p><strong>Total slots used:</strong> {interferenceGraph.Keys.Count}</p>");
                if (allocation != null && allocation.Count > 0)
                {
                    int maxUsed = allocation.Values.Max();
                    int origMax = interferenceGraph.Keys.Count > 0 ? interferenceGraph.Keys.Max() : 0;
                    sb.AppendLine($"    <p><strong>Max slot index after coloring:</strong> {maxUsed}</p>");
                    sb.AppendLine($"    <p><strong>Reduction:</strong> Original max: {origMax}, New max: {maxUsed}</p>");
                    sb.AppendLine($"    <p><strong>Coloring valid:</strong> <span style=\"color:{(coloringValid ? "green" : "red")}\">{(coloringValid ? "YES" : "NO - CONFLICT DETECTED!")}</span></p>");

                if (!coloringValid)
                {
                    sb.AppendLine("    <h4>Conflicts Found:</h4>");
                    sb.AppendLine("    <ul>");
                    foreach (var slot in sortedSlots)
                    {
                        int slotColor = allocation[slot];
                        foreach (var conflict in interferenceGraph[slot])
                        {
                            if (allocation.ContainsKey(conflict) && allocation[conflict] == slotColor)
                            {
                                sb.AppendLine($"        <li>Slot {slot} (color {slotColor}) conflicts with Slot {conflict}</li>");
                            }
                        }
                    }
                    sb.AppendLine("    </ul>");
                }
                }
            }

           
            sb.AppendLine("    <script>");
            sb.AppendLine("        mermaid.initialize({ startOnLoad: true });");
            sb.AppendLine("    </script>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }

		public string GetConsoleOutput()
		{
			var sb = new StringBuilder();

			sb.AppendLine("==========================================================================");
			sb.AppendLine(" Control Flow Graph");
			sb.AppendLine("  Method: " + (Method?.Name ?? "Unknown"));
			sb.AppendLine("  Total Blocks: " + Blocks.Count);
			sb.AppendLine("  Reachable Blocks: " + Blocks.Count(b => b.IsReachable));
			sb.AppendLine("==========================================================================");

			var sortedBlocks = Blocks.OrderBy(b => b.OriginalIndex).ToList();

			var edges = new List<(BasicBlock from, BasicBlock to, string label)>();
			foreach (var block in sortedBlocks)
			{
				if (block.Successors.Count == 0) continue;
				BasicBlock jumpTarget = block.JumpTargetFlagId.HasValue && block.Successors.Count >= 1
					? block.Successors[0] : null;
				BasicBlock fallThrough = block.HasFallThrough && block.Successors.Count >= 1
					? (block.Successors.Count == 1 ? (block.JumpTargetFlagId.HasValue ? null : block.Successors[0])
						: block.Successors.Last())
					: null;

				var shownSuccs = new HashSet<BasicBlock>();
				foreach (var succ in block.Successors)
				{
					if (shownSuccs.Contains(succ)) continue;
					shownSuccs.Add(succ);

					string label = "";
					if (succ == jumpTarget && block.JumpTargetFlagId.HasValue)
						label = "(jmp)";
					else if (succ == fallThrough)
						label = "(ft)";
					edges.Add((block, succ, label));
				}
			}

			if (edges.Count > 0)
			{
				sb.AppendLine();
				sb.AppendLine(" Edges:");
				var maxFromLen = edges.Max(e => ("BB" + e.from.BlockId).Length);
				var maxToLen = edges.Max(e => ("BB" + e.to.BlockId).Length);
				foreach (var edge in edges)
				{
					string fromStr = ("BB" + edge.from.BlockId).PadRight(maxFromLen);
					string toStr = ("BB" + edge.to.BlockId).PadRight(maxToLen);
					string marker = edge.label.Length > 0 ? " " + edge.label : "";
					sb.AppendLine("    " + fromStr + " ---> " + toStr + marker);
				}
			}

			sb.AppendLine();
			sb.AppendLine(" Block Details:");
			sb.AppendLine();

			for (int blockIdx = 0; blockIdx < sortedBlocks.Count; blockIdx++)
			{
				var block = sortedBlocks[blockIdx];
				string blockName = "BB" + block.BlockId + (block.IsReachable ? "" : "*");
				string range = block.StartIndex + "-" + block.EndIndex;
				string preds = block.Predecessors.Count == 0 ? "(none)"
					: string.Join(",", block.Predecessors.Select(p => "BB" + p.BlockId));
				string succs = block.Successors.Count == 0 ? "(none)"
					: string.Join(",", block.Successors.Select(s => "BB" + s.BlockId));

				sb.AppendLine("  +" + blockName + " range=" + range + " preds=" + preds + " succs=" + succs);

				if (block.Instructions.Count == 0)
				{
					sb.AppendLine("    (empty)");
				}
				else
				{
					for (int i = 0; i < block.Instructions.Count; i++)
					{
						sb.AppendLine("    [" + (block.StartIndex + i) + "] " + block.Instructions[i]);
					}
				}
				if (!block.IsReachable)
					sb.AppendLine("    (unreachable)");

				if (blockIdx < sortedBlocks.Count - 1)
					sb.AppendLine();
			}

			if (interferenceGraph != null && interferenceGraph.Count > 0)
			{
				sb.AppendLine();
				sb.AppendLine(" Interference Graph & Coloring:");
				sb.AppendLine("  " + MakeAsciiRow(
					new string[] { "Slot", "Conflicts", "NewSlot", "Degree" },
					new int[] { 8, 24, 10, 8 }, '-', '|'));
				sb.AppendLine("  " + MakeAsciiRowDivider(new int[] { 8, 24, 10, 8 }, '-', '+'));

				var sortedSlots = interferenceGraph.Keys.OrderBy(x => x);
				foreach (var slot in sortedSlots)
				{
					var conflicts = interferenceGraph[slot].OrderBy(x => x);
					string conflictStr = "{" + string.Join(",", conflicts) + "}";
					int degree = interferenceGraph[slot].Count;
					int newSlot = allocation != null && allocation.ContainsKey(slot) ? allocation[slot] : slot;
					sb.AppendLine("  " + MakeAsciiRow(
						new string[] { slot.ToString(), Truncate(conflictStr, 24), newSlot.ToString(), degree.ToString() },
						new int[] { 8, 24, 10, 8 }, ' ', '|'));
				}

				sb.AppendLine();
				sb.AppendLine("  K (colors available): " + coloringK);
				sb.AppendLine("  Total slots: " + interferenceGraph.Keys.Count);
				if (allocation != null && allocation.Count > 0)
				{
					int maxUsed = allocation.Values.Max();
					int origMax = interferenceGraph.Keys.Count > 0 ? interferenceGraph.Keys.Max() : 0;
					sb.AppendLine("  Max slot after coloring: " + maxUsed);
					sb.AppendLine("  Reduction: " + origMax + " -> " + maxUsed);

					bool coloringValid = true;
					foreach (var slot in sortedSlots)
					{
						int slotColor = allocation[slot];
						foreach (var conflict in interferenceGraph[slot])
						{
							if (allocation.ContainsKey(conflict) && allocation[conflict] == slotColor)
							{
								coloringValid = false;
								sb.AppendLine("  [CONFLICT] Slot " + slot + " (color " + slotColor + ") conflicts with Slot " + conflict);
							}
						}
					}
					sb.AppendLine("  Coloring valid: " + (coloringValid ? "YES" : "NO - CONFLICT DETECTED!"));
				}
			}

			sb.AppendLine("==========================================================================");
			return sb.ToString();
		}

		private string MakeAsciiRow(string[] cells, int[] widths, char padChar, char sep)
		{
			var sb = new StringBuilder();
			for (int i = 0; i < cells.Length; i++)
			{
				sb.Append(sep);
				string cell = i < cells.Length ? cells[i] : "";
				int w = i < widths.Length ? widths[i] : cell.Length;
				sb.Append(cell.PadRight(w, padChar));
			}
			sb.Append(sep);
			return sb.ToString();
		}

		private string MakeAsciiRowDivider(int[] widths, char h, char join)
		{
			var sb = new StringBuilder();
			for (int i = 0; i < widths.Length; i++)
			{
				sb.Append(join);
				int w = widths[i];
				sb.Append(new string(h, w));
			}
			sb.Append(join);
			return sb.ToString();
		}

		private string Truncate(string s, int maxLen)
		{
			if (string.IsNullOrEmpty(s)) return "";
			if (s.Length <= maxLen) return s;
			return s.Substring(0, maxLen - 1) + "~";
		}

		private string GetBlockLabel(BasicBlock block)
        {
            var sb = new StringBuilder();
            sb.Append($"BB{block.BlockId}\\n");

            if (block.Instructions.Count > 1)
            {
				sb.Append(block.Instructions[0].INS_Code.ToString() + "\\n");
			}
            if (block.Instructions.Count > 2)
            {
				sb.Append("...\\n");
			}
			if (block.Instructions.Count > 0)
            {
                var lastIns = block.Instructions[block.Instructions.Count - 1];
                sb.Append(lastIns.INS_Code.ToString());
            }

            return sb.ToString();
        }

		
	}
}
