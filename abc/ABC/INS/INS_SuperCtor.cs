using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
    public sealed class INS_SuperCtor : Instruction
    {
        public override INS_Code INS_Code =>  INS_Code.super_ctor;

        public int super_type;
        public StackLocater[] args;

        public INS_SuperCtor(Token token) : base(token)
        {
        }

        public override int Size
        {
            get
            {
                return 4 + 4 + 4 + 4 * args.Length;
            }
        }

        protected override void WriteByte(BinaryWriter bw)
        {
            bw.Write(super_type);
            bw.Write(args.Length);
            for (int i = 0; i < args.Length; i++)
            {
                args[i].Write(bw);
            }
        }

		protected override void ReadFromBinary(BinaryReader br)
		{
			super_type = br.ReadInt32();
            args = new StackLocater[br.ReadInt32()];
            for (int i = 0; i < args.Length; i++)
            { 
                args[i].ReadFromBinary(br);
            }
		}

        public override string ToString()
        {
            return $"SuperCtor super_type:{super_type} args:({string.Join(",", args)})";
        }

        public override List<StackLocater> GetDef()
        {
            return new List<StackLocater>();
        }

        public override List<StackLocater> GetUse()
        {
            return new List<StackLocater>(args);
        }

        public override bool MaybeRaiseError()
        {
            return true;
        }

    }
}
