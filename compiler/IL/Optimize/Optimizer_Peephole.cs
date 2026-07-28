using juicescript.ABC.INS;
using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler.IL.Optimize
{
	internal partial class Optimizer
	{

		private static Instruction[] FirstStep(Instruction[] instructions)
		{
			
			List<Instruction> toremve = new List<Instruction>();

			for (int i = 0; i < instructions.Length-1; i++)
			{
				var ins = instructions[i];
				if (ins.INS_Code == INS_Code.ld_true)
				{
					var next = instructions[i+1];

					
					if (next.INS_Code == INS_Code.if_true_goto && ((INS_If_True_Goto)next).condition.index == ins.dst.index)
					{
							
						INS_Goto _Goto = new INS_Goto(next.token);
						_Goto.flag_id = ((INS_If_True_Goto)next).flag_id;
						_Goto.jumpTrys = 0;
						instructions[i + 1] = _Goto;

					}
					else if (next.INS_Code == INS_Code.if_false_goto && ((INS_If_False_Goto)next).condition.index == ins.dst.index)
					{
							
						toremve.Add(next); //删除，不可能成立
					}
					

				}
				else if (ins.INS_Code == INS_Code.ld_false)
				{
					var next = instructions[i + 1];
					
					if (next.INS_Code == INS_Code.if_true_goto && ((INS_If_True_Goto)next).condition.index == ins.dst.index)
					{
							
						toremve.Add(next); //删除，不可能成立
					}
					else if (next.INS_Code == INS_Code.if_false_goto && ((INS_If_False_Goto)next).condition.index == ins.dst.index)
					{
							
						INS_Goto _Goto = new INS_Goto(next.token);
						_Goto.flag_id = ((INS_If_False_Goto)next).flag_id;
						_Goto.jumpTrys = 0;
						instructions[i+1]=_Goto;
					}
					

				}
			}

			var list = instructions.Where(i=> !toremve.Contains(i)).ToList();





			var useless = list.Where(i => i.GetDef().Count() > 0 && !list.Any(ii => ii != i && ii.GetUse().Intersect(i.GetDef()).Any()))
				.Where(i => i.INS_Code == INS_Code.ld_const ||
						i.INS_Code == INS_Code.ld_false ||
						i.INS_Code == INS_Code.ld_true ||
						i.INS_Code == INS_Code.ld_null ||
						i.INS_Code == INS_Code.ld_undefined ||
						i.INS_Code == INS_Code.ld_namespace
						)
				.ToArray();
			
			return list.Where(i => !useless.Contains(i)).ToArray();
		}


		private static void RemoveBarrier(ControlFlowGraph cfg, CompileContext context)
		{
			
			//移除无用的barrier,使得图着色优化
			var instructions = cfg.Blocks.OrderBy(b=>b.OriginalIndex).SelectMany(l => l.Instructions).Where(l => l.INS_Code != INS_Code.expression_barrier ).ToArray();
			
			var instructionType = DetectType(cfg.Method, new List<Instruction>(instructions), context);

			
			HashSet<StackLocater> searched = new HashSet<StackLocater>();

			List<StackLocater> safeStackLocaters = new List<StackLocater>();

			foreach (var ins in instructions)
			{
				
				var idef = ins.GetUse().ToList();

				for (int i = 0; i < idef.Count; i++)
				{
					if (searched.Add(idef[i]))
					{
						var defsourcelist = FindStackSlotDefAt(idef[i], cfg);

						
						if (
							defsourcelist.Count >0 &&
							defsourcelist.All(i => instructionType.ContainsKey(i.Item1) && (
												instructionType[i.Item1][i.Item2].DefType == InstructionDefType.primitive ||
												instructionType[i.Item1][i.Item2].DefType == InstructionDefType.obj ||
												instructionType[i.Item1][i.Item2].DefType == InstructionDefType.global ||
												instructionType[i.Item1][i.Item2].DefType == InstructionDefType.asclass
							))

							)
						{
							safeStackLocaters.Add(idef[i]);
							//changed = true;
						}
					}
				}
			}


			


			var barriers = cfg.Blocks.OrderBy(b => b.OriginalIndex).SelectMany(l => l.Instructions).Where(l => l.INS_Code == INS_Code.expression_barrier);
			foreach (var barrier in barriers)
			{
				var use = barrier.GetUse().ToList();//.RemoveAll( u => instructions.Any( i ) );

				var notuse = use.Where( u=> !instructions.Any( i=>i.GetUse().Contains(u) || i.GetDef().Contains(u) ) ).ToArray();

				use.RemoveAll( u=>notuse.Contains(u) || safeStackLocaters.Contains(u) );

				((INS_Barrier)barrier).uselist = use.ToArray();
			}


		}





		private static Instruction[] JmpJmp(Instruction[] input)
		{
			List<Instruction> instructions = new List<Instruction>(input);

			if (true)
			{
				var flags = instructions.Where(i => i.INS_Code == INS_Code.flag && ((INS_Flag)i).flag_id < 0xffffff);
				if (flags.Count() > 0)
				{
					int maxflag = flags.Max(i => ((INS_Flag)i).flag_id);

					for (int i = 0; i < instructions.Count; i++)
					{

						var instruction = instructions[i];
						if (instruction.INS_Code == INS_Code.if_true_goto)
						{
							INS_If_True_Goto _If_True_Goto = (INS_If_True_Goto)instruction;


							int flagindex = instructions.FindIndex(o => o.INS_Code == INS_Code.flag && ((INS_Flag)o).flag_id == _If_True_Goto.flag_id);

							{
								var next_ins = instructions.Skip(flagindex).SkipWhile(i => i.INS_Code == INS_Code.flag || i.INS_Code == INS_Code.expression_barrier).FirstOrDefault(); //instructions[flagindex + 1];
								if (
									next_ins != null &&
									next_ins.INS_Code == INS_Code.if_true_goto
									&&
									((INS_If_True_Goto)next_ins).condition.index == _If_True_Goto.condition.index
									)
								{
									//查找跳跃目标，如果下一个目标也是一个INS_If_True_Goto,并且条件一致，则直接连跳
									_If_True_Goto.flag_id = ((INS_If_True_Goto)next_ins).flag_id;

									//instructions.RemoveAt(flagindex);

								}
								else if (next_ins.INS_Code == INS_Code.if_false_goto
									&&
									((INS_If_False_Goto)next_ins).condition.index == _If_True_Goto.condition.index)
								{

									////!!不能对调，因为对调可能导致其他跳转过来的路径出错,改成创建一个新flag,插入到后面

									int next_index = instructions.IndexOf(next_ins);
									if (next_index > i)
									{
										int newflag = ++maxflag;
										_If_True_Goto.flag_id = newflag;

										INS_Flag flag = new INS_Flag(next_ins.token);
										flag.flag_id = newflag;



										instructions.Insert(next_index + 1, flag);
									}

									////这个判断一定不成立，和之前的Flag对调一下位置。
									//**Instruction ins_flag = instructions[flagindex];
									//**int next_index = instructions.IndexOf(next_ins);
									//**instructions[flagindex] = next_ins;
									//**instructions[next_index] = ins_flag;

								}

							}

						}
						else if (instruction.INS_Code == INS_Code.if_false_goto)
						{
							INS_If_False_Goto _If_False_Goto = (INS_If_False_Goto)instruction;
							int flagindex = instructions.FindIndex(o => o.INS_Code == INS_Code.flag && ((INS_Flag)o).flag_id == _If_False_Goto.flag_id);

							{
								var next_ins = instructions.Skip(flagindex).SkipWhile(i => i.INS_Code == INS_Code.flag || i.INS_Code == INS_Code.expression_barrier).FirstOrDefault();// [flagindex + 1];
								if (
									next_ins != null &&
									next_ins.INS_Code == INS_Code.if_true_goto
									&&
									((INS_If_True_Goto)next_ins).condition.index == _If_False_Goto.condition.index
									)
								{
									int next_index = instructions.IndexOf(next_ins);
									if (next_index > i)
									{
										int newflag = ++maxflag;
										_If_False_Goto.flag_id = newflag;

										INS_Flag flag = new INS_Flag(next_ins.token);
										flag.flag_id = newflag;



										instructions.Insert(next_index + 1, flag);
									}


									////!!不能对调，因为对调可能导致其他跳转过来的路径出错
									////这个判断一定不成立，和之前的Flag对调一下位置。
									//**Instruction ins_flag = instructions[flagindex];

									//**int next_index = instructions.IndexOf(next_ins);

									//**instructions[flagindex] = next_ins;
									//**instructions[next_index] = ins_flag;




								}
								else if (next_ins.INS_Code == INS_Code.if_false_goto
									&&
									((INS_If_False_Goto)next_ins).condition.index == _If_False_Goto.condition.index)
								{
									//相同条件，连跳
									_If_False_Goto.flag_id = ((INS_If_False_Goto)next_ins).flag_id;

									//删除flag
									//instructions.RemoveAt(flagindex);


								}

							}



						}

					}

				}
			}







			
			{
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
						if (ins.INS_Code == INS_Code.strict_eq && ((INS_Strict_Eq)ins).dst.index == condition?.index

							&& ((INS_Strict_Eq)ins).v1.index <= 255
							&& ((INS_Strict_Eq)ins).v2.index <= 255
							)
						{
							INS_If_LogicOp_Goto logicOp_Goto = new INS_If_LogicOp_Goto(instruction.token);
							logicOp_Goto.flag_id = jumpflag;
							logicOp_Goto.jump_mode = instruction.INS_Code == INS_Code.if_true_goto ? true : false;
							logicOp_Goto.compMode = INS_If_LogicOp_Goto.CompMode.strict_equal;
							logicOp_Goto.v1 = (byte)((INS_Strict_Eq)ins).v1.index;
							logicOp_Goto.v2 = (byte)((INS_Strict_Eq)ins).v2.index;
							logicOp_Goto.compResult = ins.dst;

							instructions[i] = logicOp_Goto;
							instructions.RemoveAt(i - 1);

							i--;
						}
						else if (ins.INS_Code == INS_Code.strict_neq)
						{
							INS_If_LogicOp_Goto logicOp_Goto = new INS_If_LogicOp_Goto(instruction.token);
							logicOp_Goto.flag_id = jumpflag;
							logicOp_Goto.jump_mode = instruction.INS_Code == INS_Code.if_true_goto ? true : false;
							logicOp_Goto.compMode = INS_If_LogicOp_Goto.CompMode.strict_neq;
							logicOp_Goto.v1 = (byte)((INS_Strict_Neq)ins).v1.index;
							logicOp_Goto.v2 = (byte)((INS_Strict_Neq)ins).v2.index;
							logicOp_Goto.compResult = ins.dst;

							instructions[i] = logicOp_Goto;
							instructions.RemoveAt(i - 1);

							i--;
						}
						else if (ins.INS_Code == INS_Code.equal && ((INS_Equal)ins).dst.index == condition?.index

							&& ((INS_Equal)ins).v1.index <= 255
							&& ((INS_Equal)ins).v2.index <= 255)
						{

							INS_If_LogicOp_Goto logicOp_Goto = new INS_If_LogicOp_Goto(instruction.token);
							logicOp_Goto.flag_id = jumpflag;
							logicOp_Goto.jump_mode = instruction.INS_Code == INS_Code.if_true_goto ? true : false;
							logicOp_Goto.compMode = INS_If_LogicOp_Goto.CompMode.equal;
							logicOp_Goto.v1 = (byte)((INS_Equal)ins).v1.index;
							logicOp_Goto.v2 = (byte)((INS_Equal)ins).v2.index;
							logicOp_Goto.compResult = ins.dst;

							instructions[i] = logicOp_Goto;
							instructions.RemoveAt(i - 1);

							i--;

						}
						else if (ins.INS_Code == INS_Code.not_equal)
						{
							INS_If_LogicOp_Goto logicOp_Goto = new INS_If_LogicOp_Goto(instruction.token);
							logicOp_Goto.flag_id = jumpflag;
							logicOp_Goto.jump_mode = instruction.INS_Code == INS_Code.if_true_goto ? true : false;
							logicOp_Goto.compMode = INS_If_LogicOp_Goto.CompMode.notequal;
							logicOp_Goto.v1 = (byte)((INS_NotEqual)ins).v1.index;
							logicOp_Goto.v2 = (byte)((INS_NotEqual)ins).v2.index;
							logicOp_Goto.compResult = ins.dst;

							instructions[i] = logicOp_Goto;
							instructions.RemoveAt(i - 1);

							i--;
						}
						else if (ins.INS_Code == INS_Code.logic_comparison && ((INS_Comparison)ins).dst.index == condition?.index

							&& ((INS_Comparison)ins).v1.index <= 255
							&& ((INS_Comparison)ins).v2.index <= 255)
						{
							INS_If_LogicOp_Goto logicOp_Goto = new INS_If_LogicOp_Goto(instruction.token);
							logicOp_Goto.flag_id = jumpflag;
							logicOp_Goto.jump_mode = instruction.INS_Code == INS_Code.if_true_goto ? true : false;
							logicOp_Goto.compMode = (INS_If_LogicOp_Goto.CompMode)(((INS_Comparison)ins).opMode + 4);
							logicOp_Goto.v1 = (byte)((INS_Comparison)ins).v1.index;
							logicOp_Goto.v2 = (byte)((INS_Comparison)ins).v2.index;
							logicOp_Goto.compResult = ins.dst;

							instructions[i] = logicOp_Goto;
							instructions.RemoveAt(i - 1);

							i--;



						}
					}
				}





			}







			return instructions.ToArray();
		}






	}
}
