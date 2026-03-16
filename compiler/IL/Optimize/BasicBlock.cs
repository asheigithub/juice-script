using juicescript.ABC.INS;
using System;
using System.Collections.Generic;

namespace juicescript.compiler.IL.Optimize
{
    public class BasicBlock
    {
        public int BlockId { get; set; }
        public int OriginalIndex { get; set; }
        public int StartIndex { get; set; }
        public int EndIndex { get; set; }
        public List<Instruction> Instructions { get; set; }
        
        public List<BasicBlock> Predecessors { get; set; }
        public List<BasicBlock> Successors { get; set; }
        
        public int? JumpTargetFlagId { get; set; }
        public bool HasFallThrough { get; set; }
        
        
        public bool IsReachable { get; set; }

        public ExceptionBlockInfo ExceptionInfo { get; set; }

        public BasicBlock()
        {
            Instructions = new List<Instruction>();
            Predecessors = new List<BasicBlock>();
            Successors = new List<BasicBlock>();
            IsReachable = false;
            HasFallThrough = false;
            JumpTargetFlagId = null;
           
        }

        public override string ToString()
        {
            return $"BB{BlockId}({StartIndex}-{EndIndex})";
        }
    }

    
}
