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


        internal int TryStmtId;

        /// <summary>
        /// 直接支配节点
        /// </summary>
		internal BasicBlock Idom;

        internal HashSet<BasicBlock> DominanceFrontier;


        

        //internal List<BasicBlock> Dom_Nodes;


		public bool IsReachable { get; set; }

        public HashSet<int> LiveIn { get; set; }
        public HashSet<int> LiveOut { get; set; }

        public Dictionary<int, HashSet<int>> InstructionLive { get; set; }

        public BasicBlock()
        {
            Instructions = new List<Instruction>();
            Predecessors = new List<BasicBlock>();
            Successors = new List<BasicBlock>();
            IsReachable = false;
            HasFallThrough = false;
            JumpTargetFlagId = null;
            TryStmtId = 0;
            LiveIn = new HashSet<int>();
            LiveOut = new HashSet<int>();
            InstructionLive = new Dictionary<int, HashSet<int>>();
           
        }

        public override string ToString()
        {
            return $"BB{BlockId}({StartIndex}-{EndIndex})";
        }
    }

    
}
