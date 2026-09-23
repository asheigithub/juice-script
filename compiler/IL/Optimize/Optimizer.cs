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
	internal partial class Optimizer
	{
		internal static void Optimize(ASMethod method, List<string> displaycfg_files, string fullPath, string outfile_base, CompileContext context)
		{

			
			string key = CompileContext.CleanInvalidPathChars(Player.GetMethodKey(method));
			Disassembler.Disassemble(method.Body.ByteCode, out int slotCount, out NaNBoxing[] constants, out Instruction[] instructions);

			instructions = FirstStep(instructions);


			var cfg = ControlFlowGraphBuilder.Build(instructions, method); instructions = null;
			cfg.DeathCodeErase();

			cfg.FindNaturalLoop(true);

			//拆节点后，很难重建正确的关系，干脆重算
			var inssplited = cfg.FlattenInstructions();
			cfg = ControlFlowGraphBuilder.Build(inssplited, method);
			cfg.FindNaturalLoop(false);


			ControlFlowGraphBuilder.BuildDomTree(cfg);

			if (cfg.Method.IsConstructor && cfg.Method.Body._link_codescope.Parent.Kind == CodeScopeKind.Instance)
			{

				if (((ASInstance)cfg.Method.Body._link_codescope.Parent.Container).Flags.HasFlag(ClassFlags.Struct))
				{
					//结构体删除superCtor
					foreach (var item in cfg.Blocks)
					{
						item.Instructions.RemoveAll(i => i.INS_Code == INS_Code.super_ctor);
						
					}
				}
				else
				{
					//检测父类构造函数是否为空

					bool CheckIsBlankCtor(ASMethod ctor)
					{
						if (ctor.Flags.HasFlag(MethodFlags.Native))
							return false;

						Disassembler.Disassemble(ctor.Body.ByteCode, out int _s, out NaNBoxing[] _c, out Instruction[] inslist);

						for (int i = 0; i < inslist.Length; i++)
						{
							Instruction ins = inslist[i];
							if (ins.INS_Code == INS_Code.expression_barrier)
							{
								
							}
							else if (ins.INS_Code == INS_Code.super_ctor)
							{
								var super_class = ((ASInstance)(ctor.Container))._super_class_;
								if (super_class != null)
								{
									if (!CheckIsBlankCtor(super_class.Instance.Constructor))
									{
										return false;
									}
								}

							}
							else if (ins.INS_Code == INS_Code.END)
							{ 
								
							}
							else
							{
								return false;
							}
						}

						return true;
					}

					var super_class = ((ASInstance)(method.Container))._super_class_;
					if (super_class != null)
					{
						if (CheckIsBlankCtor(super_class.Instance.Constructor))
						{
							//父类构造函数为空，删了
							foreach (var item in cfg.Blocks)
							{
								item.Instructions.RemoveAll(i => i.INS_Code == INS_Code.super_ctor);
								
							}
						}
					}

				}

			}



			for (int i = 0; i < cfg.Blocks.Count; i++)
			{

				EncodeMessageIntoStoreVar(cfg.Blocks[i], cfg);

				OptimizeLDREF(cfg.Blocks[i], cfg, constants);

			}

			RemoveMoveFirst(cfg);

			slotCount = OptimizeBlockLdConst(cfg,slotCount);

			slotCount = OptimizeLdStaticMember(cfg,slotCount,context); //外提静态成员。

			slotCount = OptimizeDirect_Recurse_Call(cfg, constants,slotCount); //探测递归

			slotCount = OptimizeLdFunctionBindGlobal(cfg,slotCount);//提取ld_function_bindglobal的公共部分.[注意try catch的情况，必须提取到try块的头]

			slotCount = OptimizeInstance(cfg, slotCount, context); //特化对INSTANCE的存取

			slotCount = OptimizeBlockSSAVariable(cfg,slotCount,context);

			slotCount = OptimizeInstanceFieldAfterSSA(cfg, slotCount, context); //删除重复读等


			////SSA后,获得methodVar SSA版本，于是对methdVar 进行 ld_method 的结果可视为公共表达式
			slotCount = OptimizeLdMethod(cfg, slotCount, context);
			slotCount = OptimizeLdInterfaceMethod(cfg, slotCount, context);

			slotCount = OptimizeCommExpr(cfg, slotCount, context); //公共表达式
			
			
			
			slotCount = RemoveBlockMove(cfg,slotCount,context); //干涉图移除move
			

			slotCount = OptimizeConstruction(cfg,slotCount, context);//此步骤必须放在SSA 后，因为它也是一个变量赋值源   如果是new_instance,后面是构造到变量里，优化构造的目标直接到变量里
			
			slotCount = OptimizeStoreInstanceToVar_DetectMultiNameLType(cfg,slotCount, context);//此步骤必须放在SSA后，因为它也是一个变量赋值源会破坏SSA。 检查肯定类型匹配的情况，加速保存

			slotCount = OptimizeSuperInstruction(cfg, slotCount, context);//超级指令


			RemoveBarrier(cfg,context); 

			//着色法 槽复用
			int maxslots = slotCount;
			//if (key.IndexOf("closure") != -1)
			{
				maxslots = cfg.GraphColoring();				
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

			optimizedInstructions = JmpJmp(optimizedInstructions);


			if (optimizedInstructions.All(i => i.INS_Code == INS_Code.END || i.INS_Code == INS_Code.expression_barrier))
			{
				optimizedInstructions = new Instruction[0];
			}

			method.Body.ByteCode = Assembler.Assemble(maxslots, constants, optimizedInstructions);

		}

		

		internal static byte[] ReUseSlotsAndOptimizeVar(byte[] bytecode, ASMethod temp)
		{
			//return (byte[])bytecode.Clone();

			
			Disassembler.Disassemble(bytecode, out int slotCount, out NaNBoxing[] constants, out Instruction[] instructions);
			var cfg = ControlFlowGraphBuilder.Build(instructions, temp);

			for (int i = 0; i < cfg.Blocks.Count; i++)
			{
				EncodeMessageIntoStoreVar(cfg.Blocks[i], cfg);
			}

			int maxslots = slotCount;
			maxslots = cfg.GraphColoring();
			cfg.ReMapping();
			var optimizedInstructions = cfg.FlattenInstructions();
			return Assembler.Assemble(maxslots, constants, optimizedInstructions);
		}

	}
}
