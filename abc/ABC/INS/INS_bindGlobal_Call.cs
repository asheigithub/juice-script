using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
    public sealed class INS_bindGlobal_Call : Instruction
    {
        public INS_bindGlobal_Call(Token token) : base(token)
        {
        }

        public override INS_Code INS_Code => INS_Code.bindglobal_call;

        public override int Size
        {
            get
            { //code+dst , function, args.len , 4*args.len
                return 4 + 4 + 4 + 4 * args.Length;
            }
        }

        public StackLocater function;
        
        public StackLocater[] args;


        protected override void WriteByte(BinaryWriter bw)
        {
            
            function.Write(bw);
            bw.Write(args.Length);
            for (int i = 0; i < args.Length; i++)
            {
                args[i].Write(bw);
            }
        }

		protected override void ReadFromBinary(BinaryReader br)
		{
			
            function.ReadFromBinary(br);
            args = new StackLocater[br.ReadInt32()];
            for (int i = 0; i < args.Length; i++)
            {
                args[i].ReadFromBinary(br);
            }

		}

        public override string ToString()
        {
            return $"INS_bindGlobal_Call [{dst}] <-  call function:[{function}](this:global,{string.Join(",", args)})";
        }

        public override IEnumerable<StackLocater> GetDef()
        {
            //return new List<StackLocater> { dst };
            yield return dst;
        }

        public override IEnumerable<StackLocater> GetUse()
        {
            // var use = new List<StackLocater> { function };
            // use.AddRange(args);
            // return use;

            yield return function;

            foreach (StackLocater l in args)
                yield return l;
        }

        public override bool MaybeRaiseError()
        {
            return true;
        }

        public override void RemappingSlots(Dictionary<int, int> mapping)
        {
            if (mapping.TryGetValue(dst.index, out int newIndex))
                dst.index = newIndex;
            if (mapping.TryGetValue(function.index, out int newIndex1))
                function.index = newIndex1;
            for (int i = 0; i < args.Length; i++)
            {
                if (mapping.TryGetValue(args[i].index, out int newIdx))
                    args[i].index = newIdx;
            }
        }
    }
}
