using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{

	/// <summary>
	/// 这里dst不是返回的地址，而是要返回的值的地址。
	/// </summary>
	public sealed class INS_Return_Value : Instruction
	{
		public INS_Return_Value(Token token) : base(token)
		{
		}

		public override INS_Code INS_Code => INS_Code.return_value;

		
		public override int Size
		{
			get
			{
				return 4;
			}
		}

		protected override void WriteByte(BinaryWriter bw)
		{
			
		}

		protected override void ReadFromBinary(BinaryReader br)
		{
			
		}


		public override string ToString()
		{
			return $"Return [{dst}]";
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
			//return前会转换类型，转换类型可能会抛出错误。
            return true;
        }

	}
}
