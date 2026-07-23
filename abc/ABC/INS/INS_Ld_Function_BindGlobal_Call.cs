using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
    public sealed class INS_Ld_Function_BindGlobal_Call : Instruction
    {
        public override INS_Code INS_Code => INS_Code.ld_function_bindglobal_call;

        
        public int const_index;
		public ScopeHeapLocater heapLocater;

		public StackLocater[] args;
        public override int Size
        {
            get
            {
                return 4 + 4 + 4 + 4 + 4 * args.Length;
            }
        }

        protected override void WriteByte(BinaryWriter bw)
        {
           
            bw.Write(const_index);
            heapLocater.Write(bw);
            bw.Write(args.Length);
            for (int i = 0; i < args.Length; i++)
            {
                args[i].Write(bw);
            }
        }

		protected override void ReadFromBinary(BinaryReader br)
		{
			
            const_index = br.ReadInt32();
            heapLocater.ReadFromBinary(br);
            args = new StackLocater[br.ReadInt32()];
            for (int i = 0; i < args.Length; i++)
            {
                args[i].ReadFromBinary(br);
            }
		}


        public INS_Ld_Function_BindGlobal_Call(Token token) : base(token)
        {
        }

        public override string ToString()
        {
            return $"Ld_function_bindglobal_call [{dst}] <-  function:[{const_index} at {heapLocater}]({string.Join(",", args)})";
        }

        public override IEnumerable<StackLocater> GetDef()
        {
            //return new List<StackLocater> { dst };
            yield return dst;
        }

        public override IEnumerable<StackLocater> GetUse()
        {
            //return new List<StackLocater>(args);
            return args;
        }

        public override bool MaybeRaiseError()
        {
            return true;
        }

        public override void RemappingSlots(Dictionary<int, int> mapping)
        {
            if (mapping.TryGetValue(dst.index, out int newIndex))
                dst.index = newIndex;
            for (int i = 0; i < args.Length; i++)
            {
                if (mapping.TryGetValue(args[i].index, out int newIdx))
                    args[i].index = newIdx;
            }
        }


    }
}
