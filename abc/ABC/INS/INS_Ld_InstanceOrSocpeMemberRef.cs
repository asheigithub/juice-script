using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
    public sealed class INS_Ld_InstanceOrSocpeMemberRef : Instruction
    {
        public override INS_Code INS_Code => INS_Code.ld_InstanceOrScopeMemberValueRef;

        public override int Size
        {
            get
            {
                return 4 + 4 + 4;
            }
        }

      

        public StackLocater instance;

        public uint scopemember_index;

        public INS_Ld_InstanceOrSocpeMemberRef(Token token) : base(token)
        {
        }

        protected override void WriteByte(BinaryWriter bw)
        {
           
            instance.Write(bw);
            bw.Write(scopemember_index);
        }

		protected override void ReadFromBinary(BinaryReader br)
		{
		
            instance.ReadFromBinary(br);
            scopemember_index = br.ReadUInt32();
		}

		public override string ToString()
        {
            return $"Ld_InstanceOrSocpe_MemberRef   [{dst}] <- [{instance} . scopemember: {scopemember_index}]&";
        }

        public override List<StackLocater> GetDef()
        {
            return new List<StackLocater> { dst };
        }

        public override List<StackLocater> GetUse()
        {
            return new List<StackLocater> { instance };
        }

        public override bool MaybeRaiseError()
        {
            return true;
        }

        public override void RemappingSlots(Dictionary<int, int> mapping)
        {
            if (mapping.TryGetValue(dst.index, out int newIndex))
                dst.index = newIndex;
            if (mapping.TryGetValue(instance.index, out int newIndex1))
                instance.index = newIndex1;
        }

    }
}
