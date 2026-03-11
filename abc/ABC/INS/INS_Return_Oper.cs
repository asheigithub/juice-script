//using juicescript.ABC.Locaters;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace juicescript.ABC.INS
//{

//	/// <summary>
//	/// 返回某个操作的值 其中dst用作mode
//	/// </summary>
//	public sealed class INS_Return_Oper : Instruction
//	{
//		public enum OperMode:byte
//		{ 
//			ld_const,
//			add_stack_stack,
//		}


//		public INS_Return_Oper(Token token) : base(token)
//		{
//		}

//		public override INS_Code INS_Code => INS_Code.return_op;

//		public OperMode mode
//		{
//			get { return (OperMode)(dst.index & 0xff); }
//			set 
//			{
//				dst.index &= 0xffff00;
//				dst.index |= (byte)value;
//			}
//		}


//		public override int Size
//		{
//			get
//			{
//				return 4;
//			}
//		}

//		protected override void WriteByte(BinaryWriter bw)
//		{
			
//		}

//		protected override void ReadFromBinary(BinaryReader br)
//		{
		
//		}


//		public override string ToString()
//		{
//			switch (mode)
//			{
//				case OperMode.ld_const:
//					return $"Return Oper: [ld_const : {(uint)dst.index>>8 & 0xff }] ";	
//				case OperMode.add_stack_stack:
//					return $"Return Oper: [ stackloc:{(uint)dst.index >> 16 & 0xff } + stackloc:{(uint)dst.index >> 8 & 0xff } ] ";
//				default:
//					return "Return Oper: ERR";
//			}
			
//		}

//	}
//}
