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





			var useless = list.Where(i => i.GetDef().Count > 0 && !list.Any(ii => ii != i && ii.GetUse().Intersect(i.GetDef()).Any()))
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
				
				var idef = ins.GetUse();

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
				var use = barrier.GetUse();//.RemoveAll( u => instructions.Any( i ) );

				var notuse = use.Where( u=> !instructions.Any( i=>i.GetUse().Contains(u) || i.GetDef().Contains(u) ) ).ToArray();

				use.RemoveAll( u=>notuse.Contains(u) || safeStackLocaters.Contains(u) );

				((INS_Barrier)barrier).uselist = use.ToArray();
			}


		}

	}
}
