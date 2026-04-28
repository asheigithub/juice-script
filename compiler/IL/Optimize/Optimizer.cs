using juicescript.ABC;
using juicescript.ABC.INS;
using juicescript.ABC.Locaters;
using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler.IL.Optimize
{
	internal class Optimizer
	{
		internal static void FastPeephole(ASMethod method)
		{
			
			int useslotcount; NaNBoxing[] constants; Instruction[] temp;
			Disassembler.Disassemble(method.Body.ByteCode, out useslotcount, out constants, out temp);

			List<Instruction> instructions = temp.ToList();
			

		

	
#if FALSE
			bool flag;
			#region remove "move"
			do
			{
				flag=false;
				for (int i = 0; i < instructions.Count; i++)
				{
					var instruction = instructions[i];
					//移除move
					if (instruction.INS_Code == INS_Code.move)
					{
						bool needremove = false;
						for (int j = i - 1; j >= i-1; j--)
						{
							var search = instructions[j];

							if (search.dst.index == ((INS_Move)instruction).source.index)
							{
								search.dst.index = instruction.dst.index;
								needremove = true;
								break;

							}
						}

						if (needremove)
						{
							flag = true;
							instructions.RemoveAt(i);
							break;
						}
					}
				}

			} while (flag);
			#endregion

	
			#region 连续if跳跃
			if (true)
			{
				do
				{
					flag = false;

					for (int i = 0; i < instructions.Count; i++)
					{

						var instruction = instructions[i];
						if (instruction.INS_Code == INS_Code.if_true_goto)
						{
							INS_If_True_Goto _If_True_Goto = (INS_If_True_Goto)instruction;


							int flagindex = instructions.FindIndex(o => o.INS_Code == INS_Code.flag && ((INS_Flag)o).flag_id == _If_True_Goto.flag_id);
							if (flagindex + 1 < instructions.Count)
							{
								var next_ins = instructions[flagindex + 1];
								if (next_ins.INS_Code == INS_Code.if_true_goto
									&&
									((INS_If_True_Goto)next_ins).condition.index == _If_True_Goto.condition.index
									)
								{
									//查找跳跃目标，如果下一个目标也是一个INS_If_True_Goto,并且条件一致，则直接连跳
									_If_True_Goto.flag_id = ((INS_If_True_Goto)next_ins).flag_id;

									instructions.RemoveAt(flagindex);

									flag = true;
									break;
								}
								else if (next_ins.INS_Code == INS_Code.if_false_goto
									&&
									((INS_If_False_Goto)next_ins).condition.index == _If_True_Goto.condition.index)
								{
									//这个判断一定不成立，和之前的Flag对调一下位置。
									Instruction ins_flag = instructions[flagindex];

									instructions[flagindex] = next_ins;
									instructions[flagindex + 1] = ins_flag;

									flag = true;
									break;
								}

							}

						}
						else if (instruction.INS_Code == INS_Code.if_false_goto)
						{
							INS_If_False_Goto _If_False_Goto = (INS_If_False_Goto)instruction;
							int flagindex = instructions.FindIndex(o => o.INS_Code == INS_Code.flag && ((INS_Flag)o).flag_id == _If_False_Goto.flag_id);
							if (flagindex + 1 < instructions.Count)
							{
								var next_ins = instructions[flagindex + 1];
								if (next_ins.INS_Code == INS_Code.if_true_goto
									&&
									((INS_If_True_Goto)next_ins).condition.index == _If_False_Goto.condition.index
									)
								{
									//这个判断一定不成立，和之前的Flag对调一下位置。
									Instruction ins_flag = instructions[flagindex];

									instructions[flagindex] = next_ins;
									instructions[flagindex + 1] = ins_flag;
								}
								else if (next_ins.INS_Code == INS_Code.if_false_goto
									&&
									((INS_If_False_Goto)next_ins).condition.index == _If_False_Goto.condition.index)
								{
									//相同条件，连跳
									_If_False_Goto.flag_id = ((INS_If_False_Goto)next_ins).flag_id;

									//删除flag
									instructions.RemoveAt(flagindex);

									flag = true;
									break;
								}

							}



						}

					}

				} while (flag);
			}
			#endregion



			#region 改为短指令

			if (true)
			{
				do
				{
					flag = false;
					for (int i = 0; i < instructions.Count; i++)
					{
						var instruction = instructions[i];
						//移除move
						if (instruction.INS_Code == INS_Code.ld_const)
						{
							INS_Ld_Const ld_Const = (INS_Ld_Const)instruction;

							if (ld_Const.dst.index <= byte.MaxValue && ld_Const.const_index <= short.MaxValue)
							{
								INS_Short_Ld_Const short_Ld_Const = new INS_Short_Ld_Const(instruction.token);
								int v = (int)(((uint)ld_Const.dst.index & 0xff) | ((uint)ld_Const.const_index << 8));
								short_Ld_Const.dst.index = v;

								instructions[i] = short_Ld_Const;
								flag = true;
								break;
							}
						}
						else if (instruction.INS_Code == INS_Code.ld_methodVariable)
						{
							INS_Ld_MethodVariable ins = (INS_Ld_MethodVariable)instruction;

							if (ins.dst.index < byte.MaxValue && ins.heap.ScopeIndex <= byte.MaxValue && ins.heap.MemberIndex <= byte.MaxValue)
							{
								INS_Short_Ld_MethodVariable short_Ld_MethodVariable = new INS_Short_Ld_MethodVariable(instruction.token);

								var v = (uint)ins.dst.index & 0xff | ((uint)ins.heap.MemberIndex << 8) | ((uint)ins.heap.ScopeIndex << 16);
								short_Ld_MethodVariable.dst.index = (int)v;

								instructions[i] = short_Ld_MethodVariable;

								flag = true;
								break;
							}
						}
						else if (instruction.INS_Code == INS_Code.strict_eq)
						{
							INS_Strict_Eq ins = (INS_Strict_Eq)instruction;
							if (ins.dst.index < byte.MaxValue && ins.v1.index <= byte.MaxValue && ins.v2.index <= byte.MaxValue)
							{
								INS_Short_Strict_Eq short_Strict_Eq = new INS_Short_Strict_Eq(instruction.token);

								var v = (uint)ins.dst.index & 0xff | ((uint)ins.v1.index << 16) | ((uint)ins.v2.index << 8);
								short_Strict_Eq.dst.index = (int)v;

								instructions[i] = short_Strict_Eq;

								flag = true;
								break;
							}
						}
						else if (instruction.INS_Code == INS_Code.sub)
						{
							INS_Sub ins = (INS_Sub)instruction;
							if (ins.dst.index < byte.MaxValue && ins.v1.index <= byte.MaxValue && ins.v2.index <= byte.MaxValue)
							{
								INS_Short_Sub short_Sub = new INS_Short_Sub(instruction.token);

								var v = (uint)ins.dst.index & 0xff | ((uint)ins.v1.index << 16) | ((uint)ins.v2.index << 8);
								short_Sub.dst.index = (int)v;

								instructions[i] = short_Sub;

								flag = true;
								break;
							}
						}
						else if (instruction.INS_Code == INS_Code.add)
						{
							INS_Add ins = (INS_Add)instruction;
							if (ins.dst.index < byte.MaxValue && ins.v1.index <= byte.MaxValue && ins.v2.index <= byte.MaxValue)
							{
								INS_Short_Add short_Add = new INS_Short_Add(instruction.token);

								var v = (uint)ins.dst.index & 0xff | ((uint)ins.v1.index << 16) | ((uint)ins.v2.index << 8);
								short_Add.dst.index = (int)v;

								instructions[i] = short_Add;

								flag = true;
								break;
							}
						}



					}

				} while (flag);

			}


			#endregion


			#region "op_stack_Variable_ldconst"
			if (true)
			{
				do
				{
					flag = false;

					for (int i = 0; i < instructions.Count; i++)
					{
						byte? v1 = null;
						byte? v2 = null;
						INS_Op_stack_Var_ldConst.OpMode op = (INS_Op_stack_Var_ldConst.OpMode)255;

						var instruction = instructions[i];
						if (instruction.INS_Code == INS_Code.short_strict_eq)
						{
							INS_Short_Strict_Eq strict_Eq = (INS_Short_Strict_Eq)instruction;
							v1 = (byte)((uint)strict_Eq.dst.index >> 16 & 0xff) ; v2 = (byte)((uint)strict_Eq.dst.index >> 8 & 0xff);
							op = INS_Op_stack_Var_ldConst.OpMode.strict_eq;
						}
						else if (instruction.INS_Code == INS_Code.short_sub)
						{
							INS_Short_Sub ins_sub = (INS_Short_Sub)instruction;
							v1 = (byte)((uint)ins_sub.dst.index >> 16 & 0xff); v2 = (byte)((uint)ins_sub.dst.index >> 8 & 0xff);
							op = INS_Op_stack_Var_ldConst.OpMode.sub;
						}

						if (v1.HasValue && v2.HasValue)
						{
							var scope_v1 = (INS_Short_Ld_MethodVariable)instructions.FirstOrDefault((o) => o.INS_Code == INS_Code.short_ld_methodVariable && ((uint)o.dst.index & 0xff)== v1);
							var const_v2 = (INS_Short_Ld_Const)instructions.FirstOrDefault((o) => o.INS_Code == INS_Code.short_ld_const && ((uint)o.dst.index & 0xff )== v2);

							if (scope_v1 != null && const_v2 != null)
							{
								INS_Op_stack_Var_ldConst ins = new INS_Op_stack_Var_ldConst(instruction.token);
								//ins.dst = instruction.dst;
								//ins.heap = scope_v1.heap;
								//ins.tempLoc = scope_v1.dst;
								//ins.const_index = const_v2.const_index;
								ins.dst.index = (int)((uint)instruction.dst.index & 0xff);
								ins.heap_member = (byte)((uint)scope_v1.dst.index >> 8 & 0xff);
								ins.tempLoc = (byte)((uint)scope_v1.dst.index & 0xff);
								ins.const_index = (byte)(const_v2.dst.index >> 8 & 0xff);
								ins.mode = op;


								flag = true;
								instructions[i] = ins;

								instructions.Remove(scope_v1);
								instructions.Remove(const_v2);



								break;
							}

							if (flag)
								break;
						}
					}




				} while (flag);
			}
			#endregion


	

			#region if_logicOp_goto

			if (true)
			{
				do
				{
					flag = false;

					for (int i = 1; i < instructions.Count; i++)
					{
						StackLocater? condition = null;
						int jumpflag = -1;
						var instruction = instructions[i];
						if (instruction.INS_Code == INS_Code.if_true_goto)
						{
							condition = ((INS_If_True_Goto)instruction).condition;
							jumpflag = ((INS_If_True_Goto)instruction).flag_id;
						}
						else if (instruction.INS_Code == INS_Code.if_false_goto)
						{
							condition = ((INS_If_False_Goto)instruction).condition;
							jumpflag = ((INS_If_False_Goto)instruction).flag_id;
						}

						if (condition.HasValue)
						{
							var ins = instructions[i - 1];
							if (ins.INS_Code == INS_Code.short_strict_eq && (int)((uint)((INS_Short_Strict_Eq)ins).dst.index & 0xff) == condition?.index)
							{
								INS_If_LogicOp_Goto logicOp_Goto = new INS_If_LogicOp_Goto(instruction.token);
								logicOp_Goto.flag_id = jumpflag;
								logicOp_Goto.jump_mode = instruction.INS_Code == INS_Code.if_true_goto ? true : false;
								logicOp_Goto.compMode = INS_If_LogicOp_Goto.CompMode.strict_equal;
								logicOp_Goto.v1 = (byte)((uint)((INS_Short_Strict_Eq)ins).dst.index >> 16);
								logicOp_Goto.v2 = (byte)((uint)((INS_Short_Strict_Eq)ins).dst.index >> 8);

								instructions[i] = logicOp_Goto;
								instructions.RemoveAt(i - 1);

								flag = true;
								break;
							}
						}
					}

				} while (flag);
			}

			#endregion




			#region return operater


			if (true)
			{
				do
				{
					flag = false;

					for (int i = 1; i < instructions.Count; i++)
					{
						
						var instruction = instructions[i];
						if (instruction.INS_Code == INS_Code.return_value)
						{
							INS_Return_Value return_Value = (INS_Return_Value)instruction;
							int value_index = return_Value.dst.index;

							var ins_op_index = instructions.FindIndex( o => o.dst.index == value_index || (o.dst.index >> 8 & 0xff) == value_index || (o.dst.index & 0xff) ==value_index );
							var ins_op = instructions[ins_op_index];

							if (ins_op.INS_Code == INS_Code.short_ld_const)
							{

								INS_Return_Oper return_Oper = new INS_Return_Oper(return_Value.token);
								return_Oper.dst.index = ins_op.dst.index;

								return_Oper.mode = INS_Return_Oper.OperMode.ld_const;
								

								instructions[i] = return_Oper;
								instructions.RemoveAt(ins_op_index);

								flag = true;
								break;
							}
							else if (ins_op.INS_Code == INS_Code.short_add)
							{
								INS_Return_Oper return_Oper = new INS_Return_Oper(return_Value.token);
								return_Oper.dst.index = ins_op.dst.index;
								return_Oper.mode = INS_Return_Oper.OperMode.add_stack_stack;
								
								instructions[i] = return_Oper;
								instructions.RemoveAt(ins_op_index);

								flag = true;
								break;
							}
						}
						
					}

				} while (flag);
			}


			#endregion

#endif


			method.Body.ByteCode = Assembler.Assemble(useslotcount, constants, instructions.ToArray());

		}

		internal static void Optimize(ASMethod method, List<string> displaycfg_files, string fullPath, string outfile_base)
		{
			//FastPeephole(method);
			//return;

			//if (method.Name.IndexOf("SSS") >= 0)
			//	return;


			Disassembler.Disassemble(method.Body.ByteCode, out int slotCount, out NaNBoxing[] constants, out Instruction[] instructions);

			var cfg = ControlFlowGraphBuilder.Build(instructions, method);

			string key = CompileContext.CleanInvalidPathChars( Player.GetMethodKey(method));

			cfg.DeathCodeErase();

			int maxslots = slotCount;// cfg.ReuseSlot();
			//if (key.IndexOf("closure") != -1)
			{
				maxslots = cfg.GraphColoring();
				//cfg.ReuseSlot();
			}
			if (displaycfg_files != null && displaycfg_files.Any(f => key.IndexOf(f) >= 0))
			{
				string cfg_display = cfg.GetMermaid(key);

				string opath = System.IO.Path.GetDirectoryName(outfile_base);
				System.IO.Directory.CreateDirectory(opath);

				System.IO.File.WriteAllText( outfile_base + "_CFG_" + key +".html" , cfg_display);

				Console.WriteLine($"CFG HTML generated: {outfile_base + "." + key + ".html"}");

				Console.WriteLine( cfg.GetConsoleOutput() );
				Console.WriteLine();

			}

			cfg.ReMapping();


			var optimizedInstructions = cfg.FlattenInstructions();		
			method.Body.ByteCode = Assembler.Assemble(maxslots, constants, optimizedInstructions);

		}


		internal static byte[] ReUseSlots(byte[] bytecode)
		{
			//return (byte[])bytecode.Clone();

			
			Disassembler.Disassemble(bytecode, out int slotCount, out NaNBoxing[] constants, out Instruction[] instructions);
			var cfg = ControlFlowGraphBuilder.Build(instructions, null);
			int maxslots = slotCount;
			maxslots = cfg.GraphColoring();
			cfg.ReMapping();
			var optimizedInstructions = cfg.FlattenInstructions();
			return Assembler.Assemble(maxslots, constants, optimizedInstructions);
		}

	}
}
