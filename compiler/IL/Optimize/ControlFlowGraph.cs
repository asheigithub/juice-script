using juicescript.ABC;
using juicescript.ABC.INS;
using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace juicescript.compiler.IL.Optimize
{
    public class ControlFlowGraph
    {
        public ASMethod Method { get; set; }
        public List<BasicBlock> Blocks { get; set; }
        
        public Dictionary<int, int> InstructionToBlock { get; set; }
        public Dictionary<int, ExceptionBlockInfo> TryEnterToInfo { get; set; }

        
        public ControlFlowGraph(ASMethod method)
        {
            Method = method;
            Blocks = new List<BasicBlock>();
            
            InstructionToBlock = new Dictionary<int, int>();
            TryEnterToInfo = new Dictionary<int, ExceptionBlockInfo>();
            
        }

        public List<BasicBlock> GetPredecessors(int blockId)
        {
            if (blockId >= 0 && blockId < Blocks.Count)
                return Blocks[blockId].Predecessors;
            return new List<BasicBlock>();
        }

        public List<BasicBlock> GetSuccessors(int blockId)
        {
            if (blockId >= 0 && blockId < Blocks.Count)
                return Blocks[blockId].Successors;
            return new List<BasicBlock>();
        }

        public bool IsReachable(int blockId)
        {
            if (blockId >= 0 && blockId < Blocks.Count)
                return Blocks[blockId].IsReachable;
            return false;
        }

        public void ComputeReachability()
        {
            if (Blocks.Count == 0)
                return;

            var queue = new Queue<int>();
            for (int i = 0; i < Blocks.Count; i++)
            {
                Blocks[i].IsReachable = false;
            }

            Blocks[0].IsReachable = true;
            queue.Enqueue(0);

            while (queue.Count > 0)
            {
                int blockId = queue.Dequeue();
                var block = Blocks[blockId];

                foreach (var succ in block.Successors)
                {
                    if (!succ.IsReachable)
                    {
                        succ.IsReachable = true;
                        queue.Enqueue(succ.BlockId);
                    }
                }
            }
        }

        public void RemoveUnreachableBlocks()
        {
            var reachableBlocks = Blocks.Where(b => b.IsReachable).ToList();
            Blocks.Clear();
            Blocks.AddRange(reachableBlocks);

            for (int i = 0; i < Blocks.Count; i++)
            {
                Blocks[i].BlockId = i;
                InstructionToBlock[Blocks[i].StartIndex] = i;
            }
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

    }
}
