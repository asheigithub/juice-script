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

        public override List<StackLocater> GetDef()
        {
            return new List<StackLocater> { dst };
        }

        public override List<StackLocater> GetUse()
        {
            var use = new List<StackLocater> { function, _this_ };
            use.AddRange(args);
            return use;
        }

        public override bool MaybeRaiseError()
        {
            return true;
        }

		
	}
}
