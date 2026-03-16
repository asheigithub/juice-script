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
        
        public ExceptionBlockInfo ExceptionInfo { get; set; }
        
        public bool IsReachable { get; set; }

        public BasicBlock()
        {
            Instructions = new List<Instruction>();
            Predecessors = new List<BasicBlock>();
            Successors = new List<BasicBlock>();
            IsReachable = false;
            HasFallThrough = false;
            JumpTargetFlagId = null;
            ExceptionInfo = null;
        }

        public override string ToString()
        {
            return $"BB{BlockId}({StartIndex}-{EndIndex})";
        }
    }

    public class ExceptionBlockInfo
    {
        public ExceptionBlockType BlockType { get; set; }
        public int TryEnterIndex { get; set; }
        public int[] CatchPc { get; set; }
        public int FinallyPc { get; set; }
        public int FinallyExitPc { get; set; }
        public int TryExitIndex { get; set; }
        public int CatchEnterIndex { get; set; }
        public int CatchExitIndex { get; set; }
        public int FinallyEnterIndex { get; set; }
        public int FinallyExitIndex { get; set; }
    }

    public enum ExceptionBlockType
    {
        None,
        TryBlock,
        CatchBlock,
        FinallyBlock
    }
}
