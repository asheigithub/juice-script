using juicescript.ABC.INS;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler.IL
{
	internal class Assembler
	{
		public static byte[] Assemble(  int stackslotcount,  NaNBoxing[] constants,  Instruction[] instructions)
		{
			using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
			{
				using (System.IO.BinaryWriter bw = new System.IO.BinaryWriter(ms))
				{

					//占用栈空间
					bw.Write(stackslotcount);
					//常量
					bw.Write(constants.Length);
					//指令集
					bw.Write(instructions.Length);

					int p = 0;
					//指令行号
					for (int i = 0; i < instructions.Length; i++)
					{
						var instruction = instructions[i];
						p += instruction.Size;

						bw.Write(p);
						bw.Write(instruction.token == null ? -1 : instruction.token.line);
					}

					//常量表
					for (int i = 0; i < constants.Length; i++)
					{
						bw.Write(constants[i].Raw);
					}

					//字节码
					for (int i = 0; i < instructions.Length; i++)
					{
						instructions[i].Write(bw);
					}
				}
				return ms.ToArray();
			}


		}


	}
}
