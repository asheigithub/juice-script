using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using juicescript.ABC.Locaters;

namespace juicescript.ABC.INS
{
    /// <summary>
    /// 将数据保存到当前方法的变量里 这里 dst承载要保存的数据的地址
    /// </summary>
    public sealed class INS_Store_MethodVariable : Instruction
    {
       
        public override INS_Code INS_Code => INS_Code.storeMethodVariable;

        public override int Size
        {
            get
            {
                return 4 + 4;
            }
        }

        public ScopeHeapLocater heap;



        

        public INS_Store_MethodVariable(Token token) : base(token)
        {
        }

        protected override void WriteByte(BinaryWriter bw)
        {
            heap.Write(bw);
            
        }

		protected override void ReadFromBinary(BinaryReader br)
		{
			heap.ReadFromBinary(br);
            
		}


        public override string ToString()
        {
            return $"Store_MethodVar   [offset:{heap.MemberIndex}] <- [{dst}]";
        }

        public override List<StackLocater> GetDef()
        {
            return new List<StackLocater>();
        }

        public override List<StackLocater> GetUse()
        {
            return new List<StackLocater> { dst };
        }

        public override bool MaybeRaiseError()
        {
            // Player.cs中storeMethodVariable调用ConvertValueType和PrepareSaveMethodScope可能抛出异常
            return true;
        }

        public override void RemappingSlots(Dictionary<int, int> mapping)
        {
            if (mapping.TryGetValue(dst.index, out int newIndex))
                dst.index = newIndex;
        }

    }
}
