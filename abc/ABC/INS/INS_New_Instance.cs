using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
    public sealed class INS_New_Instance : Instruction
    {
        public override INS_Code INS_Code => INS_Code.new_instance;

        protected override void WriteByte(BinaryWriter bw)
        {
            
            typeLocator.Write(bw);
            bw.Write(args.Length);
            for (int i = 0; i < args.Length; i++)
            {
                args[i].Write(bw);
            }

        }

		protected override void ReadFromBinary(BinaryReader br)
		{
			
            typeLocator.ReadFromBinary(br);
            args = new StackLocater[br.ReadInt32()];
            for (int i = 0; i < args.Length; i++)
            {
                args[i].ReadFromBinary(br);
            }
		}


		public override int Size
        {
            get
            {
                // opcode (1) + dest_locator (4) + class_locator (4) + args_count (2) + StackLocater(4) * count 
                return 4 + 4 + 4 + 4 * args.Length;
            }
        }

        public StackLocater typeLocator;

        public StackLocater[] args;

        public INS_New_Instance(Token token) : base(token)
        {
        }

        public override string ToString()
        {
            return $"New_Instance [{dst}] <-  class:[{typeLocator}]({ string.Join(",", args)})";
        }

    }
}
