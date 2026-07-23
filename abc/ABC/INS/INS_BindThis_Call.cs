using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
    public sealed class INS_BindThis_Call : Instruction
    {
        
        public override INS_Code INS_Code => INS_Code.bindthis_call;

        public override int Size
        {
            get
            {
                //code+dst,function,this,args.len,4*args.len 
                return 4 + 4 + 4 + 4 + 4 * args.Length;
            }
        }

       
        public StackLocater function;
        public StackLocater _this_;

        public StackLocater[] args;


        protected override void WriteByte(BinaryWriter bw)
        {
           
            function.Write(bw);
            _this_.Write(bw);
            bw.Write(args.Length);
            for (int i = 0; i < args.Length; i++)
            {
                args[i].Write(bw);
            }
        }

		protected override void ReadFromBinary(BinaryReader br)
		{
			
            function.ReadFromBinary(br);
            _this_.ReadFromBinary(br);
			args = new StackLocater[br.ReadInt32()];
			for (int i = 0; i < args.Length; i++)
			{
				args[i].ReadFromBinary(br);
			}
		}


		public INS_BindThis_Call(Token token) : base(token)
        {
        }

        public override string ToString()
        {
            return $"INS_BindThis_Call [{dst}] <-  call function:[{function}](this:{_this_},{string.Join(",", args)})";
        }

        public override IEnumerable<StackLocater> GetDef()
        {
            //return new List<StackLocater> { dst };
            yield return dst;
        }

        public override IEnumerable<StackLocater> GetUse()
        {
            //var use = new List<StackLocater> { function, _this_ };
            //use.AddRange(args);
            //return use;

            yield return function;
            yield return _this_;

            foreach (var item in args)
            {
                yield return item;
            }


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
            if (mapping.TryGetValue(_this_.index, out int newIndex2))
                _this_.index = newIndex2;
            for (int i = 0; i < args.Length; i++)
            {
                if (mapping.TryGetValue(args[i].index, out int newIdx))
                    args[i].index = newIdx;
            }
        }

		
	}
}
