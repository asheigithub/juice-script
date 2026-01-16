using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace juicescript.ABC.INS
{
    public sealed class INS_Ld_MultiName_Ref : Instruction
    {
        public INS_Ld_MultiName_Ref(Token token) : base(token)
        {
        }

        public override INS_Code INS_Code => INS_Code.ld_MultiName_Ref;

        public override int Size
        {
            get
            {
                return 4 + 4 +4;
            }
        }

        
        public StackLocater instance;
        public int name_index;

        protected override void WriteByte(BinaryWriter bw)
        {
           
            instance.Write(bw);
            bw.Write(name_index);
        }

		protected override void ReadFromBinary(BinaryReader br)
		{
			
            instance.ReadFromBinary(br);
            name_index = br.ReadInt32();
		}

        public override string ToString()
        {
            return $"Ld_MultiName_Ref [{dst}] <- [{instance}.(name_index:{name_index})]";
        }


    }
}
