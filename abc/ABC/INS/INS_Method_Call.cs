using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
    public sealed class INS_Method_Call : Instruction
    {
        public INS_Method_Call(Token token) : base(token)
        {
        }

        public override INS_Code INS_Code => INS_Code.method_call;

        public override int Size
        {
            get
            {
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
            return $"INS_Method_Call [{dst}] <-  call method:[{function}]({string.Join(",", args)})";
        }

    }
}
