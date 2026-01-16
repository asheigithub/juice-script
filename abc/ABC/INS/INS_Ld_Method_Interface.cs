using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
	public sealed class INS_Ld_Method_Interface : Instruction
	{
		public INS_Ld_Method_Interface(Token token) : base(token)
		{
		}


		public override INS_Code INS_Code => INS_Code.ld_interface_method;

		public override int Size
		{
			get
			{
				return  4 + 4 + 4 + 4;
			}
		}

		
		public StackLocater instance;
		public int class_id;
		public uint const_index;

		protected override void WriteByte(BinaryWriter bw)
		{
			
			instance.Write(bw);
			bw.Write(class_id);
			bw.Write(const_index);
		}

		protected override void ReadFromBinary(BinaryReader br)
		{
			
			instance.ReadFromBinary(br);
			class_id = br.ReadInt32();
			const_index = br.ReadUInt32();
		}


		public override string ToString()
		{
			return $"Ld_Method_Interface   [{dst}] <- ((interface:{class_id})[instance:{instance}]).vtable[{const_index}]";
		}

	}
}
