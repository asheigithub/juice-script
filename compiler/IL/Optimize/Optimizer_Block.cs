using juicescript.ABC;
using juicescript.ABC.INS;
using juicescript.ABC.Locaters;
using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using static System.Reflection.Metadata.BlobBuilder;

namespace juicescript.compiler.IL.Optimize
{
	internal partial class Optimizer
	{

		struct TryInfo
		{
			public int tryid;
			/// <summary>
			/// 0--try
			/// 1--catch
			/// 2--finally
			/// </summary>
			public int trystate;
		}

		/// <summary>
		/// 获取某条指令所在的trycatchfinally结构
		/// </summary>
		/// <param name="instruction"></param>
		/// <param name="cfg"></param>
		/// <returns></returns>
		private static Stack<TryInfo> GetTryStmt(Instruction instruction, ControlFlowGraph cfg)
		{ 
			Stack<TryInfo> trystmt = new Stack<TryInfo>();

			for (int i = 0; i < cfg.Blocks.Count; i++)
			{
				var b = cfg.Blocks[i];

				for (int j = 0; j < b.Instructions.Count; j++)
				{
					var ins = b.Instructions[j];

					if (ins == instruction)
					{
						return trystmt;
					}
					else if (ins.INS_Code == INS_Code.try_enter)
					{
						trystmt.Push(new TryInfo() { tryid = ins.dst.index, trystate = 0 });
					}
					else if (ins.INS_Code == INS_Code.catch_enter)
					{
						TryInfo tryInfo = trystmt.Pop();
						tryInfo.trystate = 1;
						trystmt.Push(tryInfo);
					}
					else if (ins.INS_Code == INS_Code.finally_enter)
					{
						TryInfo tryInfo = trystmt.Pop();
						tryInfo.trystate = 2;
						trystmt.Push(tryInfo);
					}
					else if (ins.INS_Code == INS_Code.finally_exit)
					{
						trystmt.Pop();
					}
				}


			}



			return trystmt;
		}





		internal static void EncodeMessageIntoStoreVar(BasicBlock basicBlock, ControlFlowGraph cfg)
		{
			//查找INS_Store_MethodVariable 。 将额外信息编码进ScopeId里。
			{
				for (int i = 0; i < basicBlock.Instructions.Count; i++)
				{
					var instruction = basicBlock.Instructions[i];
					if (instruction.INS_Code == INS_Code.storeMethodVariable)
					{
						INS_Store_MethodVariable store_MethodVariable = (INS_Store_MethodVariable)instruction;

						var scopeMember = cfg.Method.Body._link_codescope.Members[store_MethodVariable.heap.MemberIndex];

						bool encodetypekind = false;
						TypeKind typeKind;
						if (scopeMember.Kind == ScopeMemberKind.Parameter)
						{
							encodetypekind = scopeMember.TypeKind <= TypeKind.Namespace;
							typeKind = scopeMember.TypeKind;
						}
						else
						{
							ASTrait t = scopeMember.trait;
							encodetypekind = t.TypeKind <= TypeKind.Namespace;
							typeKind = t.TypeKind;
						}

						if (!encodetypekind)
						{
							typeKind = (TypeKind)0xff;
						}

						var _heaplocater = store_MethodVariable.heap;
						_heaplocater.ScopeIndex = (byte)typeKind;

						store_MethodVariable.heap = _heaplocater;


					}

				}
			}
		}


		private static void OptimizeLDREF(BasicBlock basicBlock, ControlFlowGraph cfg, NaNBoxing[] constants)
		{
			

			//查找ld_MultiNameL_Ref,再查找后续是否是把值保存到引用里。如果是，并且中间没有使用这个引用，则把指令移动到保存指令前面,然后合并为直接存值指令
			{
				for (int i = 0; i < basicBlock.Instructions.Count; i++)
				{
					var instruction = basicBlock.Instructions[i];
					if (instruction.INS_Code == ABC.INS.INS_Code.ld_MultiNameL_Ref)
					{
						var store = basicBlock.Instructions.Skip(i + 1).FirstOrDefault(
							(s) => s.INS_Code == INS_Code.storeHeapValueRef && s.dst.index == instruction.dst.index
							&&
							((INS_Ld_MultiNameL_Ref)instruction).instance.index >= 0
							);

						if (store != null)
						{
							int k = basicBlock.Instructions.IndexOf(store);
							if (basicBlock.Instructions.Skip(i).Take(k - i).Any((ins) => ins.GetUse().Contains(instruction.dst)))
							{
								continue;
							}

							basicBlock.Instructions.RemoveAt(i);
							int j = basicBlock.Instructions.IndexOf(store);

							INS_Store_MultiNameL store_MultiNameL = new INS_Store_MultiNameL(store.token);
							store_MultiNameL.dst = ((INS_Store_HeapValueRef)store).source;
							store_MultiNameL.instance = ((INS_Ld_MultiNameL_Ref)instruction).instance;
							store_MultiNameL.name = ((INS_Ld_MultiNameL_Ref)instruction).name;
							store_MultiNameL.super_type_index = ((INS_Ld_MultiNameL_Ref)instruction).super_type_index;
							store_MultiNameL.tmp_holder = ((INS_Ld_MultiNameL_Ref)instruction).dst;


							basicBlock.Instructions.Insert(j, store_MultiNameL);

							basicBlock.Instructions.Remove(store);
							i--;
						}

					}
				}
			}

			//查找ld_MultiNameL_Ref,在查找后面是否是Ld_ValueRef 。合并为直接读取值
			{
				//List<Instruction> toremove = new List<Instruction>();
				for (int i = 0; i < basicBlock.Instructions.Count - 1; i++)
				{
					var instruction = basicBlock.Instructions[i];
					if (instruction.INS_Code == ABC.INS.INS_Code.ld_MultiNameL_Ref)
					{
						var ld = basicBlock.Instructions.Skip(i).FirstOrDefault(
							(s) => s.INS_Code == INS_Code.ld_ValueRef && ((INS_Ld_ValueRef)s).source.index == instruction.dst.index
							&&
							((INS_Ld_MultiNameL_Ref)instruction).super_type_index == 0
							&&
							((INS_Ld_MultiNameL_Ref)instruction).instance.index >= 0
							);

						if (ld != null)
						{
							int k = basicBlock.Instructions.IndexOf(ld);
							if (basicBlock.Instructions.Skip(i).Take(k - i).Any((ins) => ins.GetUse().Contains(instruction.dst)))
							{
								continue;
							}
							if (basicBlock.Instructions.Skip(k + 1).Any(ins => (ins.GetUse().Contains(instruction.dst) || ins.GetDef().Contains(instruction.dst)) && ins.INS_Code != INS_Code.expression_barrier))
							{
								continue;
							}


							basicBlock.Instructions.RemoveAt(i);
							int j = basicBlock.Instructions.IndexOf(ld);

							INS_Ld_MultiNameL_Val ld_MultiNameL_Val = new INS_Ld_MultiNameL_Val(instruction.token);
							ld_MultiNameL_Val.dst = ld.dst;
							ld_MultiNameL_Val.instance = ((INS_Ld_MultiNameL_Ref)instruction).instance;
							ld_MultiNameL_Val.name = ((INS_Ld_MultiNameL_Ref)instruction).name;
							ld_MultiNameL_Val.refholder = ((INS_Ld_MultiNameL_Ref)instruction).dst;


							basicBlock.Instructions.Insert(i, ld_MultiNameL_Val);
							basicBlock.Instructions.Remove(ld);
						}

					}



					//var next = basicBlock.Instructions[i + 1];
					//if (instruction.INS_Code == ABC.INS.INS_Code.ld_MultiNameL_Ref && next.INS_Code == INS_Code.ld_ValueRef)
					//{
					//	if (((INS_Ld_ValueRef)next).source.index == instruction.dst.index
					//		&&
					//		((INS_Ld_MultiNameL_Ref)instruction).super_type_index == 0
					//		&&
					//		((INS_Ld_MultiNameL_Ref)instruction).instance.index >=0
					//		&&
					//		!basicBlock.Instructions.Skip(i + 2).Any(ins => (ins.GetUse().Contains(instruction.dst) || ins.GetDef().Contains(instruction.dst)) && ins.INS_Code != INS_Code.expression_barrier)
					//		)
					//	{
					//		toremove.Add(next);

						//		//mapping.Add(next.dst.index, instruction.dst.index);

						//		INS_Ld_MultiNameL_Val ld_MultiNameL_Val = new INS_Ld_MultiNameL_Val(instruction.token);
						//		ld_MultiNameL_Val.dst = next.dst;
						//		ld_MultiNameL_Val.instance = ((INS_Ld_MultiNameL_Ref)instruction).instance;
						//		ld_MultiNameL_Val.name = ((INS_Ld_MultiNameL_Ref)instruction).name;
						//		ld_MultiNameL_Val.refholder = ((INS_Ld_MultiNameL_Ref)instruction).dst;

						//		basicBlock.Instructions[i] = ld_MultiNameL_Val;

						//		//INS_Move move = new INS_Move(next.token);
						//		//move.dst = next.dst;
						//		//move.source = instruction.dst;

						//		//basicBlock.Instructions[i+1]=move;

						//	}


						//}
				}

				//if (toremove.Count > 0)
				//{
				//	basicBlock.Instructions.RemoveAll(r => toremove.Contains(r));
				//	//foreach (var ins in basicBlock.Instructions)
				//	//{
				//	//	ins.RemappingSlots(mapping);
				//	//}
				//}
			}

			//查找ld_MultiName_Ref,再查找后续是否是把值保存到引用里。如果是，并且中间没有使用这个引用，则把指令移动到保存指令前面,然后合并为直接存值指令
			{
				for (int i = 0; i < basicBlock.Instructions.Count; i++)
				{
					var instruction = basicBlock.Instructions[i];
					if (instruction.INS_Code == ABC.INS.INS_Code.ld_MultiName_Ref)
					{
						var store = basicBlock.Instructions.Skip(i + 1).FirstOrDefault(
							(s) => s.INS_Code == INS_Code.storeHeapValueRef && s.dst.index == instruction.dst.index
							&&
							((INS_Ld_MultiName_Ref)instruction).instance.index >= 0
							);

						if (store != null)
						{
							int k = basicBlock.Instructions.IndexOf(store);
							if (basicBlock.Instructions.Skip(i).Take(k - i).Any((ins) => ins.GetUse().Contains(instruction.dst)))
							{
								continue;
							}

							basicBlock.Instructions.RemoveAt(i);
							int j = basicBlock.Instructions.IndexOf(store);

							INS_Store_MultiName store_MultiName = new INS_Store_MultiName(store.token);
							store_MultiName.dst = ((INS_Store_HeapValueRef)store).source;
							store_MultiName.instance = ((INS_Ld_MultiName_Ref)instruction).instance;
							store_MultiName.name_index = ((INS_Ld_MultiName_Ref)instruction).name_index;							
							store_MultiName.refholder = ((INS_Ld_MultiName_Ref)instruction).dst;


							basicBlock.Instructions.Insert(j, store_MultiName);
							basicBlock.Instructions.Remove(store);
							i--;
						}

					}
				}
			}

			//Ld_MultiName_Ref + Ld_ValueRef 
			{
				//List<Instruction> toremove = new List<Instruction>();
				for (int i = 0; i < basicBlock.Instructions.Count - 1; i++)
				{
					var instruction = basicBlock.Instructions[i];
					//var next = basicBlock.Instructions[i + 1];
					if (instruction.INS_Code == ABC.INS.INS_Code.ld_MultiName_Ref)// && next.INS_Code == INS_Code.ld_ValueRef)
					{
						var ld = basicBlock.Instructions.Skip(i).FirstOrDefault(
							(s) => s.INS_Code == INS_Code.ld_ValueRef && ((INS_Ld_ValueRef)s).source.index == instruction.dst.index
							&&
							((INS_Ld_MultiName_Ref)instruction).instance.index >= 0
							);

						if (ld != null)
						{
							int k = basicBlock.Instructions.IndexOf(ld);
							if (basicBlock.Instructions.Skip(i).Take(k - i).Any((ins) => ins.GetUse().Contains(instruction.dst)))
							{
								continue;
							}
							if (basicBlock.Instructions.Skip(k + 1).Any(
								ins => (ins.GetUse().Contains(instruction.dst)
								|| ins.GetDef().Contains(instruction.dst)) && ins.INS_Code != INS_Code.expression_barrier))
							{
								continue;
							}
							

							basicBlock.Instructions.RemoveAt(i);
							int j = basicBlock.Instructions.IndexOf(ld);

							INS_Ld_MultiName_Val ld_MultiName_Val = new INS_Ld_MultiName_Val(instruction.token);
							ld_MultiName_Val.dst = ld.dst;
							ld_MultiName_Val.instance = ((INS_Ld_MultiName_Ref)instruction).instance;
							ld_MultiName_Val.name_index = ((INS_Ld_MultiName_Ref)instruction).name_index;
							ld_MultiName_Val.refholder = ((INS_Ld_MultiName_Ref)instruction).dst;


							basicBlock.Instructions.Insert(i, ld_MultiName_Val);
							basicBlock.Instructions.Remove(ld);
							

						}


						//if (((INS_Ld_ValueRef)next).source.index == instruction.dst.index					
						//	&&
						//	((INS_Ld_MultiName_Ref)instruction).instance.index >= 0
						//	&&
						//	!basicBlock.Instructions.Skip(i + 2).Any(ins => (ins.GetUse().Contains(instruction.dst) || ins.GetDef().Contains(instruction.dst)) && ins.INS_Code != INS_Code.expression_barrier)
						//	)
						//{
						//	toremove.Add(next);

						//	INS_Ld_MultiName_Val ld_MultiName_Val = new INS_Ld_MultiName_Val(instruction.token);
						//	ld_MultiName_Val.dst = next.dst;
						//	ld_MultiName_Val.instance = ((INS_Ld_MultiName_Ref)instruction).instance;
						//	ld_MultiName_Val.name_index = ((INS_Ld_MultiName_Ref)instruction).name_index;
						//	ld_MultiName_Val.refholder = ((INS_Ld_MultiName_Ref)instruction).dst;

						//	basicBlock.Instructions[i] = ld_MultiName_Val;

						//}


					}
				}

				//if (toremove.Count > 0)
				//{
				//	basicBlock.Instructions.RemoveAll(r => toremove.Contains(r));
				//	//foreach (var ins in basicBlock.Instructions)
				//	//{
				//	//	ins.RemappingSlots(mapping);
				//	//}
				//}
			}

			//ld_InstanceOrScopeMemberValueRef +Ld_ValueRef 
			{
				
				for (int i = 0; i < basicBlock.Instructions.Count - 1; i++)
				{
					var instruction = basicBlock.Instructions[i];
					//var next = basicBlock.Instructions[i + 1];

					if (instruction.INS_Code == ABC.INS.INS_Code.ld_InstanceOrScopeMemberValueRef)// && next.INS_Code == INS_Code.ld_ValueRef)
					{
						var ld = basicBlock.Instructions.Skip(i ).FirstOrDefault(
							(s) => s.INS_Code == INS_Code.ld_ValueRef && ((INS_Ld_ValueRef)s).source.index == instruction.dst.index
							&&
							((INS_Ld_InstanceOrSocpeMemberRef)instruction).instance.index >= 0
							);

						if (ld != null)
						{
							int k = basicBlock.Instructions.IndexOf(ld);
							if (basicBlock.Instructions.Skip(i).Take(k - i).Any((ins) => ins.GetUse().Contains(instruction.dst)))
							{
								continue;
							}
							if (basicBlock.Instructions.Skip(k + 1).Any(ins => (ins.GetUse().Contains(instruction.dst) || ins.GetDef().Contains(instruction.dst)) && ins.INS_Code != INS_Code.expression_barrier))
							{
								continue;
							}


							basicBlock.Instructions.RemoveAt(i);
							int j = basicBlock.Instructions.IndexOf(ld);

							INS_Ld_InstanceMember_Val ld_InstanceMember_Val = new INS_Ld_InstanceMember_Val(instruction.token);
							ld_InstanceMember_Val.dst = ld.dst;
							ld_InstanceMember_Val.instance = ((INS_Ld_InstanceOrSocpeMemberRef)instruction).instance;
							ld_InstanceMember_Val.scopemember_index = ((INS_Ld_InstanceOrSocpeMemberRef)instruction).scopemember_index;


							basicBlock.Instructions.Insert(i, ld_InstanceMember_Val);
							basicBlock.Instructions.Remove(ld);
							
						}

						//if (((INS_Ld_ValueRef)next).source.index == instruction.dst.index
						//	&&
						//	((INS_Ld_InstanceOrSocpeMemberRef)instruction).instance.index >= 0
						//	&&
						//	!basicBlock.Instructions.Skip(i + 2).Any(ins => (ins.GetUse().Contains(instruction.dst) || ins.GetDef().Contains(instruction.dst)) && ins.INS_Code != INS_Code.expression_barrier)
						//	)
						//{
						//	toremove.Add(next);

						//	INS_Ld_InstanceMember_Val ld_InstanceMember_Val = new INS_Ld_InstanceMember_Val(instruction.token);
						//	ld_InstanceMember_Val.dst = next.dst;
						//	ld_InstanceMember_Val.instance = ((INS_Ld_InstanceOrSocpeMemberRef)instruction).instance;
						//	ld_InstanceMember_Val.scopemember_index = ((INS_Ld_InstanceOrSocpeMemberRef)instruction).scopemember_index;
							
						//	basicBlock.Instructions[i] = ld_InstanceMember_Val;

						//}
					}
				}

				

			}

			//ld_InstanceOrScopeMemberValueRef,再查找后续是否把值保存到引用里.如果是，并且中间没有使用这个引用，则把指令移动到保存指令前面,然后合并为直接存值指令
			{
				for (int i = 0; i < basicBlock.Instructions.Count; i++)
				{
					var instruction = basicBlock.Instructions[i];
					if (instruction.INS_Code == ABC.INS.INS_Code.ld_InstanceOrScopeMemberValueRef)
					{
						var store = basicBlock.Instructions.Skip(i + 1).FirstOrDefault(
							(s) => s.INS_Code == INS_Code.storeHeapValueRef && s.dst.index == instruction.dst.index
							&&
							((INS_Ld_InstanceOrSocpeMemberRef)instruction).instance.index >= 0
							);

						if (store != null)
						{
							int k = basicBlock.Instructions.IndexOf(store);
							if (basicBlock.Instructions.Skip(i).Take(k - i).Any((ins) => ins.GetUse().Contains(instruction.dst)))
							{
								continue;
							}

							basicBlock.Instructions.RemoveAt(i);
							int j = basicBlock.Instructions.IndexOf(store);

							INS_Store_InstanceMember store_InstanceMember = new INS_Store_InstanceMember(store.token);
							store_InstanceMember.dst = ((INS_Store_HeapValueRef)store).source;
							store_InstanceMember.instance = ((INS_Ld_InstanceOrSocpeMemberRef)instruction).instance;
							store_InstanceMember.scopemember_index = ((INS_Ld_InstanceOrSocpeMemberRef)instruction).scopemember_index;


							basicBlock.Instructions.Insert(j, store_InstanceMember);
							basicBlock.Instructions.Remove(store);
							i--;
						}

					}
				}
			}

		}


		




		/// <summary>
		/// 查找公共的支配节点
		/// </summary>
		/// <param name="atblocks"></param>
		/// <returns></returns>
		private static BasicBlock FindCommDom(List<BasicBlock> atblocks)
		{
			HashSet<BasicBlock> idoms = new HashSet<BasicBlock>();
			foreach (var item in atblocks)
			{
				idoms.Add(item);
			}

			while (idoms.Count > 1)
			{
				var temp = idoms.ToList();
				var max = temp.OrderByDescending(b => b.OriginalIndex).First();

				temp.Remove(max);
				temp.Add(max.Idom);

				idoms.Clear();
				foreach (var item in temp)
					idoms.Add(item);
			}
			var dom = idoms.First();
			if (dom.TryStmtId != 0) // try有可能意外进入catch和finally,所以需要移动到try_enter里
			{
				int tryid = dom.TryStmtId;

				while (!(dom.Instructions.Count > 0 && dom.Instructions[0].INS_Code == INS_Code.try_enter))
				{
					dom = dom.Idom;
				}

				Debug.Assert(dom.TryStmtId == tryid);
				//throw new NotImplementedException();
			}

			return dom;
		}

		private static int OptimizeBlockLdConst(ControlFlowGraph cfg,int slotcount)
		{
			if (cfg.Blocks.Count == 0)
				return slotcount;
			if (cfg.Method.Flags.HasFlag(MethodFlags.ASYNC) || cfg.Method.Flags.HasFlag( MethodFlags.Generator)) //async里有问题，yield里有问题，需要在变量里保持值
				return slotcount;
			
			void MoveInstructions(BasicBlock dom,List<Instruction> ld_list)
			{

				var ld = ld_list.First();
				foreach (var block in cfg.Blocks)
				{
					block.Instructions.RemoveAll(ins => ld_list.Contains(ins));
				}

				int newslot = slotcount++;
				foreach (var l in ld_list)
				{
					Dictionary<int, int> replace = new Dictionary<int, int> { { l.dst.index, newslot } };

					foreach (var ins in cfg.Blocks.SelectMany(bb => bb.Instructions))
					{
						ins.RemappingSlots(replace);
					}
				}

				ld.dst.index = newslot;
				if (dom.Instructions.Count > 0 &&
										(dom.Instructions[0].INS_Code == INS_Code.flag
										||
										dom.Instructions[0].INS_Code == INS_Code.try_enter
										||
										dom.Instructions[0].INS_Code == INS_Code.catch_enter
										||
										dom.Instructions[0].INS_Code == INS_Code.finally_enter
										)
										)
				{
					dom.Instructions.Insert(1, ld);
				}
				else
				{
					dom.Instructions.Insert(0, ld);
				}
			}



			//ld_const
			{

				var all = cfg.Blocks.SelectMany(b => b.Instructions).Where(i => i.INS_Code == INS_Code.ld_const).Select(i => (INS_Ld_Const)i).ToList();
				var const_idxs = all.GroupBy(i => i.const_index).ToList();
				foreach (var i in const_idxs)
				{
					var ld_list = i.ToList();

					if (ld_list.Count > 1)
					{
						var atblocks = cfg.Blocks.Where(b => b.Instructions.Any(i => ld_list.Contains(i))).ToList();
						var dom = FindCommDom(atblocks);

						var loop = cfg.toplevelloops.Where(l => l.FindLoop(dom) != null).FirstOrDefault();
						if (loop != null)
						{
							Debug.Assert(loop.loop.firstNode.Predecessors.Contains(loop.loop.firstNode.Idom));
							dom = loop.loop.firstNode.Idom;
						}


						MoveInstructions(dom, ld_list.Select(i => (Instruction)i).ToList());
					}
					else if(ld_list.Count == 1)
					{
						var at = cfg.Blocks.First(b => b.Instructions.Any(i => ld_list.Contains(i)));

						var loop = cfg.toplevelloops.Where( l=>l.FindLoop(at) != null ).FirstOrDefault();
						if (loop != null)
						{
							Debug.Assert(loop.loop.firstNode.Predecessors.Contains( loop.loop.firstNode.Idom ));

							MoveInstructions(loop.loop.firstNode.Idom, ld_list.Select(i => (Instruction)i).ToList());

						}
					}
				}

			}
			//ld_class
			{
				var all = cfg.Blocks.SelectMany(b => b.Instructions).Where(i => i.INS_Code == INS_Code.ld_class).Select(i => (INS_Ld_Class)i).ToList();
				var classid_list = all.GroupBy(i => i.classid_index).ToList();
				foreach (var i in classid_list)
				{
					var ld_list = i.ToList();
					if (ld_list.Count > 1)
					{
						var atblocks = cfg.Blocks.Where(b => b.Instructions.Any(i => ld_list.Contains(i))).ToList();
						var dom = FindCommDom(atblocks);

						var loop = cfg.toplevelloops.Where(l => l.FindLoop(dom) != null).FirstOrDefault();
						if (loop != null)
						{
							Debug.Assert(loop.loop.firstNode.Predecessors.Contains(loop.loop.firstNode.Idom));
							dom = loop.loop.firstNode.Idom;
						}

						MoveInstructions(dom, ld_list.Select(i => (Instruction)i).ToList());
					}
					else if(ld_list.Count == 1)
					{
						var at = cfg.Blocks.First(b => b.Instructions.Any(i => ld_list.Contains(i)));

						var loop = cfg.toplevelloops.Where(l => l.FindLoop(at) != null).FirstOrDefault();
						if (loop != null)
						{
							Debug.Assert(loop.loop.firstNode.Predecessors.Contains(loop.loop.firstNode.Idom));

							MoveInstructions(loop.loop.firstNode.Idom, ld_list.Select(i => (Instruction)i).ToList());

						}
					}
				}
			}
			//ld_true
			{
				var all = cfg.Blocks.SelectMany(b => b.Instructions).Where(i => i.INS_Code == INS_Code.ld_true).Select(i => (INS_Ld_True)i).ToList();
				
				var ld_list = all;
				if (ld_list.Count > 1)
				{
					var atblocks = cfg.Blocks.Where(b => b.Instructions.Any(i => ld_list.Contains(i))).ToList();
					var dom = FindCommDom(atblocks);

					var loop = cfg.toplevelloops.Where(l => l.FindLoop(dom) != null).FirstOrDefault();
					if (loop != null)
					{
						Debug.Assert(loop.loop.firstNode.Predecessors.Contains(loop.loop.firstNode.Idom));
						dom = loop.loop.firstNode.Idom;
					}

					MoveInstructions(dom, ld_list.Select(i => (Instruction)i).ToList());
				}
				else if (ld_list.Count == 1)
				{
					var at = cfg.Blocks.First(b => b.Instructions.Any(i => ld_list.Contains(i)));

					var loop = cfg.toplevelloops.Where(l => l.FindLoop(at) != null).FirstOrDefault();
					if (loop != null)
					{
						Debug.Assert(loop.loop.firstNode.Predecessors.Contains(loop.loop.firstNode.Idom));

						MoveInstructions(loop.loop.firstNode.Idom, ld_list.Select(i => (Instruction)i).ToList());

					}
				}
			}
			{
				var all = cfg.Blocks.SelectMany(b => b.Instructions).Where(i => i.INS_Code == INS_Code.ld_false).Select(i => (INS_Ld_False)i).ToList();

				var ld_list = all;
				if (ld_list.Count > 1)
				{
					var atblocks = cfg.Blocks.Where(b => b.Instructions.Any(i => ld_list.Contains(i))).ToList();
					var dom = FindCommDom(atblocks);

					var loop = cfg.toplevelloops.Where(l => l.FindLoop(dom) != null).FirstOrDefault();
					if (loop != null)
					{
						Debug.Assert(loop.loop.firstNode.Predecessors.Contains(loop.loop.firstNode.Idom));
						dom = loop.loop.firstNode.Idom;
					}

					MoveInstructions(dom, ld_list.Select(i => (Instruction)i).ToList());
				}
				else if (ld_list.Count == 1)
				{
					var at = cfg.Blocks.First(b => b.Instructions.Any(i => ld_list.Contains(i)));

					var loop = cfg.toplevelloops.Where(l => l.FindLoop(at) != null).FirstOrDefault();
					if (loop != null)
					{
						Debug.Assert(loop.loop.firstNode.Predecessors.Contains(loop.loop.firstNode.Idom));

						MoveInstructions(loop.loop.firstNode.Idom, ld_list.Select(i => (Instruction)i).ToList());

					}
				}
			}
			{
				var all = cfg.Blocks.SelectMany(b => b.Instructions).Where(i => i.INS_Code == INS_Code.ld_undefined).Select(i => (INS_Ld_Undefined)i).ToList();

				var ld_list = all;
				if (ld_list.Count > 1)
				{
					var atblocks = cfg.Blocks.Where(b => b.Instructions.Any(i => ld_list.Contains(i))).ToList();
					var dom = FindCommDom(atblocks);

					var loop = cfg.toplevelloops.Where(l => l.FindLoop(dom) != null).FirstOrDefault();
					if (loop != null)
					{
						Debug.Assert(loop.loop.firstNode.Predecessors.Contains(loop.loop.firstNode.Idom));
						dom = loop.loop.firstNode.Idom;
					}

					MoveInstructions(dom, ld_list.Select(i => (Instruction)i).ToList());
				}
				else if (ld_list.Count == 1)
				{
					var at = cfg.Blocks.First(b => b.Instructions.Any(i => ld_list.Contains(i)));

					var loop = cfg.toplevelloops.Where(l => l.FindLoop(at) != null).FirstOrDefault();
					if (loop != null)
					{
						Debug.Assert(loop.loop.firstNode.Predecessors.Contains(loop.loop.firstNode.Idom));

						MoveInstructions(loop.loop.firstNode.Idom, ld_list.Select(i => (Instruction)i).ToList());

					}
				}
			}


			return slotcount;



			
		}


		private static int OptimizeLdStaticMember(ControlFlowGraph cfg, int slotcount,CompileContext context)
		{
			if (cfg.Blocks.Count == 0)
				return slotcount;
			if (cfg.Method.Flags.HasFlag(MethodFlags.ASYNC) || cfg.Method.Flags.HasFlag(MethodFlags.Generator)) //async里有问题，yield里有问题，需要在变量里保持值
				return slotcount;


			var instructions = cfg.Blocks.OrderBy(b => b.OriginalIndex).SelectMany(l => l.Instructions).Where(l => l.INS_Code != INS_Code.expression_barrier).ToArray();
			var instructionType = DetectType(cfg.Method, new List<Instruction>(instructions), context);


			//静态方法
			{
				var all = cfg.Blocks.SelectMany(b => b.Instructions)
					.Where(i => i.INS_Code == INS_Code.ld_method)
					.Select(i => (INS_Ld_Method)i)
					.Where((k) =>
					{
						var deflist = FindStackSlotDefAt(k.instance, cfg);
						return deflist.Count > 0 && deflist.All(
							d =>
							instructionType.ContainsKey(d.Item1) && (
													instructionType[d.Item1][d.Item2].DefType == InstructionDefType.asclass
								)

							);

					})
					.ToList();

				var groupbyclass = all.GroupBy(c => c.instance);
				foreach (var group in groupbyclass)
				{
					var ld_cls = FindStackSlotDefAt(group.Key, cfg);
					Debug.Assert(ld_cls.Count == 1);
					var ld_cls_block = cfg.Blocks.First(b => b.Instructions.Contains(ld_cls[0].Item1));


					void MoveLdStaticMethod(BasicBlock dom, List<Instruction> ld_list)
					{
						var tdom = dom;
						while (tdom != ld_cls_block) //class必须已经加载
						{
							tdom = tdom.Idom;
							if (tdom == null)
							{
								throw new InvalidOperationException();
							}
						}


						var ld = ld_list.First();
						foreach (var block in cfg.Blocks)
						{
							block.Instructions.RemoveAll(ins => ld_list.Contains(ins));
						}

						int newslot = slotcount++;
						foreach (var l in ld_list)
						{
							Dictionary<int, int> replace = new Dictionary<int, int> { { l.dst.index, newslot } };

							foreach (var ins in cfg.Blocks.SelectMany(bb => bb.Instructions))
							{
								ins.RemappingSlots(replace);
							}
						}

						ld.dst.index = newslot;

						if (dom.Instructions.Contains(ld_cls[0].Item1))
						{
							int _at = dom.Instructions.IndexOf(ld_cls[0].Item1);

							_at++;

							if (_at < dom.Instructions.Count)
							{
								dom.Instructions.Insert(_at, ld);
							}
							else
							{
								dom.Instructions.Add(ld);
							}

						}
						else if (dom.Instructions.Count > 0 &&
												(dom.Instructions[0].INS_Code == INS_Code.flag
												||
												dom.Instructions[0].INS_Code == INS_Code.try_enter
												||
												dom.Instructions[0].INS_Code == INS_Code.catch_enter
												||
												dom.Instructions[0].INS_Code == INS_Code.finally_enter
												)
												)
						{
							dom.Instructions.Insert(1, ld);
						}
						else
						{
							dom.Instructions.Insert(0, ld);
						}
					}







					var groupbymember = group.GroupBy(c => c.const_index);
					foreach (var staticmethod in groupbymember)
					{
						var ld_list = staticmethod.ToList();
						if (ld_list.Count > 1)
						{
							var atblocks = cfg.Blocks.Where(b => b.Instructions.Any(i => ld_list.Contains(i))).ToList();
							var dom = FindCommDom(atblocks);

							var loop = cfg.toplevelloops.Where(l => l.FindLoop(dom) != null).FirstOrDefault();
							if (loop != null)
							{
								Debug.Assert(loop.loop.firstNode.Predecessors.Contains(loop.loop.firstNode.Idom));
								dom = loop.loop.firstNode.Idom;
							}

							MoveLdStaticMethod(dom, ld_list.Select(i => (Instruction)i).ToList());
						}
						else if (ld_list.Count == 1)
						{
							var at = cfg.Blocks.First(b => b.Instructions.Any(i => ld_list.Contains(i)));

							var loop = cfg.toplevelloops.Where(l => l.FindLoop(at) != null).FirstOrDefault();
							if (loop != null)
							{
								Debug.Assert(loop.loop.firstNode.Predecessors.Contains(loop.loop.firstNode.Idom));

								MoveLdStaticMethod(loop.loop.firstNode.Idom, ld_list.Select(i => (Instruction)i).ToList());

							}
						}
					}
				}
			}

			//静态常量
			{
				var all = cfg.Blocks.SelectMany(b => b.Instructions)
					.Where(i => i.INS_Code == INS_Code.ld_instacneMember_Val)
					.Select(i=> (INS_Ld_InstanceMember_Val)i)
					.Where((k) => {

						var deflist = FindStackSlotDefAt(k.instance, cfg);
						if (deflist.Count == 1 && instructionType.ContainsKey(deflist[0].Item1  ))
						{
							var deftype = instructionType[ deflist[0].Item1][deflist[0].Item2 ];

							if (deftype.DefType == InstructionDefType.asclass)
							{
								ASClass @class = (ASClass)deftype.Obj;
								var member = @class._link_codescope.Members[(int)k.scopemember_index];
								return member.trait.Kind == TraitKind.Constant;

							}
							else
							{
								return false;
							}

						}
						else
						{
							return false;
						}
					}).ToList()					
					;

				var groupbyclass = all.GroupBy(c => c.instance);
				foreach (var group in groupbyclass)
				{
					var ld_cls = FindStackSlotDefAt(group.Key, cfg);
					Debug.Assert(ld_cls.Count == 1);
					var ld_cls_block = cfg.Blocks.First(b => b.Instructions.Contains(ld_cls[0].Item1));



					void MoveLdStaticConst(BasicBlock dom, List<Instruction> ld_list)
					{
						var tdom = dom;
						while (tdom != ld_cls_block) //class必须已经加载
						{
							tdom = tdom.Idom;
							if (tdom == null)
							{
								throw new InvalidOperationException();
							}
						}


						var ld = ld_list.First();
						foreach (var block in cfg.Blocks)
						{
							block.Instructions.RemoveAll(ins => ld_list.Contains(ins));
						}

						int newslot = slotcount++;
						foreach (var l in ld_list)
						{
							Dictionary<int, int> replace = new Dictionary<int, int> { { l.dst.index, newslot } };

							foreach (var ins in cfg.Blocks.SelectMany(bb => bb.Instructions))
							{
								ins.RemappingSlots(replace);
							}
						}

						ld.dst.index = newslot;

						if (dom.Instructions.Contains(ld_cls[0].Item1))
						{
							int _at = dom.Instructions.IndexOf(ld_cls[0].Item1);

							_at++;

							if (_at < dom.Instructions.Count)
							{
								dom.Instructions.Insert(_at, ld);
							}
							else
							{
								dom.Instructions.Add(ld);
							}

						}
						else if (dom.Instructions.Count > 0 &&
												(dom.Instructions[0].INS_Code == INS_Code.flag
												||
												dom.Instructions[0].INS_Code == INS_Code.try_enter
												||
												dom.Instructions[0].INS_Code == INS_Code.catch_enter
												||
												dom.Instructions[0].INS_Code == INS_Code.finally_enter
												)
												)
						{
							dom.Instructions.Insert(1, ld);
						}
						else
						{
							dom.Instructions.Insert(0, ld);
						}
					}


					var groupbyconst = group.GroupBy(c => c.scopemember_index);
					foreach (var static_const in groupbyconst)
					{
						var ld_list = static_const.ToList();
						if (ld_list.Count > 1)
						{
							var atblocks = cfg.Blocks.Where(b => b.Instructions.Any(i => ld_list.Contains(i))).ToList();
							var dom = FindCommDom(atblocks);

							var loop = cfg.toplevelloops.Where(l => l.FindLoop(dom) != null).FirstOrDefault();
							if (loop != null)
							{
								Debug.Assert(loop.loop.firstNode.Predecessors.Contains(loop.loop.firstNode.Idom));
								dom = loop.loop.firstNode.Idom;
							}
							MoveLdStaticConst(dom, ld_list.Select(i => (Instruction)i).ToList());
						}
						else if (ld_list.Count == 1)
						{
							var at = cfg.Blocks.First(b => b.Instructions.Any(i => ld_list.Contains(i)));

							var loop = cfg.toplevelloops.Where(l => l.FindLoop(at) != null).FirstOrDefault();
							if (loop != null)
							{
								Debug.Assert(loop.loop.firstNode.Predecessors.Contains(loop.loop.firstNode.Idom));
								MoveLdStaticConst(loop.loop.firstNode.Idom, ld_list.Select(i => (Instruction)i).ToList());
							}
						}
					}
				}
			}



			return slotcount;
		}




		private static int OptimizeLdFunctionBindGlobal(ControlFlowGraph cfg, int slotcount)
		{
			if (cfg.Blocks.Count == 0)
				return slotcount;
			if (cfg.Method.Flags.HasFlag(MethodFlags.ASYNC) || cfg.Method.Flags.HasFlag(MethodFlags.Generator)) //async里有问题，yield里有问题，需要在变量里保持值
				return slotcount;


			var all = cfg.Blocks.SelectMany(b => b.Instructions)
				.Where(i => i.INS_Code == INS_Code.ld_function_bindglobal_call)
				.Select(i => (INS_Ld_Function_BindGlobal_Call)i)
				.ToList();

			var groups = all.GroupBy(i=>i.const_index); //实际上function和heaplocater是绑死的
			foreach (var group in groups)
			{
				
				{
					var heap = group.First().heapLocater;
					Debug.Assert(group.All(i => i.heapLocater.ScopeIndex == heap.ScopeIndex && i.heapLocater.MemberIndex == heap.MemberIndex));


					var atblocks = cfg.Blocks.Where(b => b.Instructions.Any(i => group.Contains(i))).ToList();


					var dom = FindCommDom(atblocks);

					var loop = cfg.toplevelloops.Where(l => l.FindLoop(dom) != null).FirstOrDefault();
					if (loop != null)
					{
						Debug.Assert(loop.loop.firstNode.Predecessors.Contains(loop.loop.firstNode.Idom));
						dom = loop.loop.firstNode.Idom;
					}
					else
					{
						//非循环量，不必要的修改取消
						if (atblocks.Count == 1 && group.Count() < 3)
						{
							continue;
						}
					}

					var lineat = dom.Instructions.FirstOrDefault(i => group.Contains(i));

					int newslot = slotcount++;
					INS_O_Ld_Function_BindGLobal o_Ld_Function_BindGLobal = new INS_O_Ld_Function_BindGLobal(group.First().token);
					o_Ld_Function_BindGLobal.heapLocater = heap;
					o_Ld_Function_BindGLobal.const_index = group.Key;
					o_Ld_Function_BindGLobal.dst.index = newslot; 

					if (lineat != null && dom.TryStmtId == 0)
					{
						int insert_at = dom.Instructions.IndexOf(lineat);
						dom.Instructions.Insert(insert_at, o_Ld_Function_BindGLobal);
					}
					else
					{
						if (dom.Instructions.Count > 0 &&
										(dom.Instructions[0].INS_Code == INS_Code.flag
										||
										dom.Instructions[0].INS_Code == INS_Code.try_enter
										||
										dom.Instructions[0].INS_Code == INS_Code.catch_enter
										||
										dom.Instructions[0].INS_Code == INS_Code.finally_enter
										)
										)
						{
							dom.Instructions.Insert(1, o_Ld_Function_BindGLobal);
						}
						else
						{
							dom.Instructions.Insert(0, o_Ld_Function_BindGLobal);
						}



					}

					//将所有INS_Ld_Function_BindGlobal_Call 替换为INS_Method_Call ,省去查找function。
					foreach (var ins in group)
					{
						var block = cfg.Blocks.First(b => b.Instructions.Contains(ins));
						int at = block.Instructions.IndexOf(ins);

						INS_O_Call method_Call = new INS_O_Call(ins.token);
						method_Call.dst = ins.dst;
						method_Call.function = o_Ld_Function_BindGLobal.dst;
						method_Call.args = ins.args;
						
						block.Instructions.Insert(at, method_Call);

						block.Instructions.Remove(ins);

					}


				}

			}



			return slotcount;
		}




		private static int OptimizeLdMethod(ControlFlowGraph cfg, int slotCount, CompileContext context)
		{
			//这个步骤在SSA后。
			if (cfg.Blocks.Count == 0)
				return slotCount;
			if (cfg.Method.Flags.HasFlag(MethodFlags.ASYNC) || cfg.Method.Flags.HasFlag(MethodFlags.Generator)) //async里有问题，yield里有问题，需要在变量里保持值
				return slotCount;


			var call = cfg.Blocks.SelectMany(b => b.Instructions).Where(i => i.INS_Code == INS_Code.method_call).Select(i=>(INS_Method_Call)i).ToList();

			//查询instance来源是 ld_methodvar 或者 store_methodvar
			//如果数量多，则在每个来源处ld_method,然后复用。
			var all = cfg.Blocks.SelectMany(b => b.Instructions).Where(i => i.INS_Code == INS_Code.ld_method)
				.Select(i => (INS_Ld_Method)i)
				.Where((i) =>
				{
					var defat = FindStackSlotDefAt(i.instance, cfg);
					// defat 必须等于1，否则就是SSA的多路合并,不可提取
					//若instance小于0，则在上下文读取，那么绝不可能被改变。

					return i.instance.index<0 || (defat.Count == 1 && (defat.All(d => d.Item1.INS_Code == INS_Code.ld_methodVariable
					|| d.Item1.INS_Code == INS_Code.ld_MethodVariableInitValue
					|| d.Item1.INS_Code == INS_Code.storeMethodVariable
					))
					);


				})
				.Where((i) => {
					//它被INS_Method_Call 引用。
					return call.Any(c => c.function.index == i.dst.index);

				})

				.ToList()
				;

#if DEBUG
			// ---从当前字节码生成方式来看，如果被 methoc_call引用了，那么就不可能被其他类型指令引用。确保这点成立
			foreach (var ins in cfg.Blocks.SelectMany(b => b.Instructions).Where(i => i.INS_Code != INS_Code.method_call && i.INS_Code != INS_Code.expression_barrier))
			{
				var use = ins.GetUse();
				foreach (var item in all)
				{
					if (use.Contains(item.dst))
						throw new InvalidOperationException();
				}
			}

#endif


			var groupbyinstance = all.GroupBy(i => i.instance);
			foreach (var instance_group in groupbyinstance)
			{
				var instance = instance_group.Key;

				var groupby_method = instance_group.GroupBy(i => i.const_index).ToList();

				var instancedef = FindStackSlotDefAt(instance,cfg);

				Debug.Assert(instancedef.Count == 1 || instance.index< 0);

				foreach (var method in groupby_method)
				{
					
					if (method.Count() < 2)
					{
						//是否在循环里！
						if (instance.index >= 0)
						{
							var ib = cfg.Blocks.Where(b => b.Instructions.Contains(instancedef[0].Item1)).First();
							var ibatloop = cfg.toplevelloops.Where(l => l.FindLoop(ib) != null).Select(l => l.FindLoop(ib)).FirstOrDefault();

							var useat = cfg.Blocks.Where(b => b.Instructions.Any(i => method.Contains(i)));
							var useatloop = useat.Select(u => cfg.toplevelloops.Where(l => l.FindLoop(u) != null).Select(l => l.FindLoop(u)).FirstOrDefault());

							if (useatloop.Any(u => u != ibatloop)) //call在循环里，外提!
							{

							}
							else
							{
								continue;
							}
						}
						else
						{
							var useat = cfg.Blocks.Where(b => b.Instructions.Any(i => method.Contains(i)));
							var useatloop = useat.Select(u => cfg.toplevelloops.Where(l => l.FindLoop(u) != null).Select(l => l.FindLoop(u)).FirstOrDefault());

							if (useatloop.Any(u => u != null)) //call在循环里，外提!
							{

							}
							else
							{
								continue;
							}
						}
					}

					var methodid = method.Key;


					//新建一个O_ld_method,并放置到instancedef[0]的后面 | 如果instance.index<0,则放到 idom.（O_ld_method 不检查instance是否为null,等后面O_call时再抛）
					int newslotindex = slotCount++;

					INS_O_Ld_Method o_Ld_Method = new INS_O_Ld_Method(method.First().token);
					o_Ld_Method.dst.index = newslotindex;
					o_Ld_Method.instance = instance;
					o_Ld_Method.const_index = method.Key;

					if (instance.index < 0)
					{
						var atblocks = cfg.Blocks.Where(b => b.Instructions.Any(i => method.Contains(i))).ToList();
						var dom = FindCommDom(atblocks);

						var loop = cfg.toplevelloops.Where(l => l.FindLoop(dom) != null).FirstOrDefault();
						if (loop != null)
						{
							Debug.Assert(loop.loop.firstNode.Predecessors.Contains(loop.loop.firstNode.Idom));
							dom = loop.loop.firstNode.Idom;
						}
						var lineat = dom.Instructions.FirstOrDefault(i => method.Contains(i));

						if (lineat != null) 
						{
							int insert_at = dom.Instructions.IndexOf(lineat);
							dom.Instructions.Insert(insert_at, o_Ld_Method);
						}
						else
						{
							if (dom.Instructions.Count > 0 &&
											(dom.Instructions[0].INS_Code == INS_Code.flag
											||
											dom.Instructions[0].INS_Code == INS_Code.try_enter
											||
											dom.Instructions[0].INS_Code == INS_Code.catch_enter
											||
											dom.Instructions[0].INS_Code == INS_Code.finally_enter
											)
											)
							{
								dom.Instructions.Insert(1, o_Ld_Method);
							}
							else
							{
								dom.Instructions.Insert(0, o_Ld_Method);
							}



						}


						//throw new NotImplementedException();
					}
					else
					{
						var b = cfg.Blocks.Where(b => b.Instructions.Contains(instancedef[0].Item1 )).First();

						int at = b.Instructions.IndexOf(instancedef[0].Item1) + 1;

						b.Instructions.Insert(at, o_Ld_Method);


					}


					//删除所有原ld_method
					foreach (var b in cfg.Blocks)
					{
						b.Instructions.RemoveAll(i => method.Contains(i));
					}


					//查找引用它的INS_Method_Call,将此INS_Method_Call替换为O_call。
					var toreplace = call.Where(c => method.Any(l => l.dst.index == c.function.index)).ToList();
					Debug.Assert(toreplace.Count == method.Count());

					foreach (var mcall in toreplace)
					{
						var b = cfg.Blocks.Where(b => b.Instructions.Contains(mcall)).First();

						int at = b.Instructions.IndexOf(mcall);

						INS_O_Call method_Call = new INS_O_Call(mcall.token);
						method_Call.dst = mcall.dst;
						method_Call.function.index = newslotindex ;
						method_Call.args = mcall.args;

						b.Instructions.Insert(at, method_Call);

						b.Instructions.Remove(mcall);

					}



				}


			}



			return slotCount;
		}

		private static int OptimizeLdInterfaceMethod(ControlFlowGraph cfg, int slotCount, CompileContext context)
		{
			if (cfg.Blocks.Count == 0)
				return slotCount;
			if (cfg.Method.Flags.HasFlag(MethodFlags.ASYNC) || cfg.Method.Flags.HasFlag(MethodFlags.Generator)) //async里有问题，yield里有问题，需要在变量里保持值
				return slotCount;

			var call = cfg.Blocks.SelectMany(b => b.Instructions).Where(i => i.INS_Code == INS_Code.method_call).Select(i => (INS_Method_Call)i).ToList();


			//查询instance来源是 ld_methodvar 或者 store_methodvar
			//如果数量多，则在每个来源处ld_method,然后复用。
			var all = cfg.Blocks.SelectMany(b => b.Instructions).Where(i => i.INS_Code == INS_Code.ld_interface_method)
				.Select(i => (INS_Ld_Method_Interface)i)
				.Where((i) =>
				{
					var defat = FindStackSlotDefAt(i.instance, cfg);

					return (defat.Count == 1 && (defat.All(d => d.Item1.INS_Code == INS_Code.ld_methodVariable
					|| d.Item1.INS_Code == INS_Code.ld_MethodVariableInitValue
					|| d.Item1.INS_Code == INS_Code.storeMethodVariable
					))
					);


				})
				.Where((i) => {
					//它被INS_Method_Call 引用。
					return call.Any(c => c.function.index == i.dst.index);

				})

				.ToList()
				;

#if DEBUG
			// ---从当前字节码生成方式来看，如果被 methoc_call引用了，那么就不可能被其他类型指令引用。确保这点成立
			foreach (var ins in cfg.Blocks.SelectMany(b => b.Instructions).Where(i => i.INS_Code != INS_Code.method_call && i.INS_Code != INS_Code.expression_barrier))
			{
				var use = ins.GetUse();
				foreach (var item in all)
				{
					if (use.Contains(item.dst))
						throw new InvalidOperationException();
				}
			}

#endif

			var groupbyinstance = all.GroupBy(i => i.instance);
			foreach (var instance_group in groupbyinstance)
			{
				var instance = instance_group.Key;

				var groupby_method = instance_group.GroupBy(i => i.const_index).ToList();

				var instancedef = FindStackSlotDefAt(instance, cfg);

				Debug.Assert(instancedef.Count == 1);


				foreach (var method in groupby_method)
				{
					var ib = cfg.Blocks.Where(b => b.Instructions.Contains(instancedef[0].Item1)).First();

					if (method.Count() < 2)
					{
						//是否在循环里！

						var ibatloop = cfg.toplevelloops.Where(l => l.FindLoop(ib) != null).Select(l => l.FindLoop(ib)).FirstOrDefault();

						var useat = cfg.Blocks.Where( b=>b.Instructions.Any( i => method.Contains(i) ) );
						var useatloop = useat.Select( u=> cfg.toplevelloops.Where( l=>l.FindLoop(u) != null ).Select( l=>l.FindLoop(u) ).FirstOrDefault() );

						if (useatloop.Any(u => u != ibatloop)) //call在循环里，外提!
						{

						}
						else
						{
							continue;
						}
					}
					int class_id = method.First().class_id;
					Debug.Assert(method.All(m => m.class_id == class_id));




					var methodid = method.Key;


					//新建一个O_ld_interface_method,并放置到instancedef[0]的后面 | 如果instance.index<0,则放到 idom.（O_ld_method 不检查instance是否为null,等后面O_call时再抛）
					int newslotindex = slotCount++;

					INS_O_Ld_Method_Interface o_Ld_InterfaceMethod = new INS_O_Ld_Method_Interface(method.First().token);
					o_Ld_InterfaceMethod.dst.index = newslotindex;
					o_Ld_InterfaceMethod.instance = instance;
					o_Ld_InterfaceMethod.const_index = method.Key;
					o_Ld_InterfaceMethod.class_id = class_id;

					
					int i_at = ib.Instructions.IndexOf(instancedef[0].Item1) + 1;
					ib.Instructions.Insert(i_at, o_Ld_InterfaceMethod);


					//删除所有原ld_method
					foreach (var b in cfg.Blocks)
					{
						b.Instructions.RemoveAll(i => method.Contains(i));
					}


					//查找引用它的INS_Method_Call,将此INS_Method_Call替换为O_call。
					var toreplace = call.Where(c => method.Any(l => l.dst.index == c.function.index)).ToList();
					Debug.Assert(toreplace.Count == method.Count());

					foreach (var mcall in toreplace)
					{
						var b = cfg.Blocks.Where(b => b.Instructions.Contains(mcall)).First();

						int at = b.Instructions.IndexOf(mcall);

						INS_O_Call method_Call = new INS_O_Call(mcall.token);
						method_Call.dst = mcall.dst;
						method_Call.function.index = newslotindex;
						method_Call.args = mcall.args;

						b.Instructions.Insert(at, method_Call);

						b.Instructions.Remove(mcall);

					}



				}






			}


			return slotCount;
		}

/// <summary>
/// 触发可能性过低，不管了
/// </summary>
/// <param name="ins"></param>
/// <param name="loop"></param>
/// <returns></returns>
////		private static int OptimizeCommLdRef(ControlFlowGraph cfg, int slotcount, CompileContext context)
////		{
////			/* 目前看来，只有这种代码可能触发
////			 (
////function()
////{var perm;// = Array(3);
////var k;


//// while (0) {
	 
////	k= ((k = perm[0]) == 0);
	 
////	perm[0] = 0;
////	trace(k);
//// }
 
 
////})();
////			 */


////			if (cfg.Blocks.Count == 0)
////				return slotcount;
////			if (cfg.Method.Flags.HasFlag(MethodFlags.ASYNC) || cfg.Method.Flags.HasFlag(MethodFlags.Generator)) //async里有问题，yield里有问题，需要在变量里保持值
////				return slotcount;


////			foreach (var l in cfg.toplevelloops)
////			{
////				Queue<ControlFlowGraph.looptreenode> loops = new Queue<ControlFlowGraph.looptreenode>();
////				loops.Enqueue(l);

////				Stack<NaturalLoop> stack=new Stack<NaturalLoop>();
////				while (loops.Count>0)
////				{
////					var n = loops.Dequeue();
////					foreach (var c in n.children)
////					{
////						loops.Enqueue(c);
////					}
////					stack.Push(n.loop);
////				}

////				while (stack.Count > 0)
////				{
////					var loop = stack.Pop();

////					var check = loop.nodes.SelectMany(b => b.Instructions).Where( i=>i.INS_Code == INS_Code.ld_MultiNameL_Ref ).Select(i=>(INS_Ld_MultiNameL_Ref)i).ToList();

////					foreach (var ins in check)
////					{
////						bool canmoveup = true;

////						var changes = loop.nodes.SelectMany(b => b.Instructions).Where(i =>
////						{
////							var def = i.GetDef();
////							return def.Contains(ins.name) || def.Contains(ins.instance);
////						}).ToArray();

						
////						foreach (var test in changes )
////						{
////							if (AllReachable(test, loop).Contains(ins))
////							{ 
////								canmoveup=false;
////								break;
////							}
////						}

////						if (canmoveup)
////						{
////							var startblock = loop.nodes.First(b => b.Instructions.Contains(ins));
////							startblock.Instructions.Remove(ins);

////							Debug.Assert(loop.firstNode.Predecessors.Contains(loop.firstNode.Idom));
////							var toinsert = loop.firstNode.Idom;

////							Debug.Assert(toinsert.Instructions[toinsert.Instructions.Count - 1].INS_Code != INS_Code.goto_flag);

////							toinsert.Instructions.Add(ins);
////						}
////					}				
////				}

////			}


////			return slotcount;
////		}

		private static IEnumerable<Instruction> AllReachable(Instruction ins, NaturalLoop loop)
		{
			var startblock = loop.nodes.First(b => b.Instructions.Contains(ins));

			int at = startblock.Instructions.IndexOf(ins);

			Stack<BasicBlock> path = new Stack<BasicBlock>();

			for (int i = at + 1; i < startblock.Instructions.Count; i++)
			{
				yield return startblock.Instructions[i];
			}

			foreach (var item in startblock.Successors)
			{
				if(item != startblock && loop.nodes.Contains(item))
					path.Push(item);
				else if (item == startblock)
				{
					for (int i = 0; i < at; i++)
					{
						yield return startblock.Instructions[i];
					}
				}
			}

			HashSet<BasicBlock> visited=new HashSet<BasicBlock>();

			while (path.Count>0)
			{
				var b = path.Pop();

				visited.Add(b);

				for (int i = 0; i < b.Instructions.Count; i++)
				{
					yield return b.Instructions[i];
				}

				foreach (var item in b.Successors)
				{
					if (item != startblock && loop.nodes.Contains(item) && !visited.Contains(item))
						path.Push(item);
					else if (item == startblock)
					{
						for (int i = 0; i < at; i++)
						{
							yield return startblock.Instructions[i];
						}
					}
				}
			}





		}




		class PhiNode
		{
			internal int ResultVersion;
			internal Dictionary<BasicBlock, int> Incoming;
		}

		class SSA_Split
		{
			internal BasicBlock succ;
			internal BasicBlock pred;

			internal BasicBlock inserted;
			internal BasicBlock beforeinserted;

		}

		/*
		 * 
		 * # 🧩 SSA 的整体流程（你只需要 4 步）

		对每个局部变量 v：

		1. **收集所有定义点（Store_MethodVar）**  
		2. **计算支配边界（dominance frontier）**  
		3. **在支配边界插入 φ（phi）节点**  
		4. **做 rename pass（重命名变量版本）**  
		   - 把所有 Ld/St 重写成 SSA 变量  
		   - 生成 v0, v1, v2…  
		   - φ 节点也会生成新版本  

		做完这四步，你的变量就变成 SSA 形式了。

		下面我给你每一步的可直接写成 C# 的版本。

		---

		# ① 收集变量的定义点（Store_MethodVar）

		对每个函数：

		```csharp
		Dictionary<int, List<BasicBlock>> DefSites = new();
		```

		遍历所有基本块：

		```csharp
		foreach (var block in Blocks)
		{
			foreach (var inst in block.Instructions)
			{
				if (inst.Op == Store_MethodVar)
				{
					int varId = inst.VarIndex;
					if (!DefSites.ContainsKey(varId))
						DefSites[varId] = new List<BasicBlock>();
					DefSites[varId].Add(block);
				}
			}
		}
		```

		---

		# ② 计算支配边界（Dominance Frontier）

		你已经有支配树（idom），所以 DF 很容易算。

		对每个块 b：

		```csharp
		DF[b] = new HashSet<BasicBlock>();
		```

		算法（标准版）：

		```csharp
		foreach (var b in Blocks)
		{
			if (b.Predecessors.Count >= 2)
			{
				foreach (var p in b.Predecessors)
				{
					var runner = p;
					while (runner != Idom[b])
					{
						DF[runner].Add(b);
						runner = Idom[runner];
					}
				}
			}
		}
		```

		这段代码你可以直接用。

		---

		# ③ 在支配边界插入 φ 节点

		对每个变量 v：

		- WorkList = 所有定义点（DefSites[v]）
		- 已插入 φ 的集合 PhiInserted

		伪代码：

		```csharp
		Queue<BasicBlock> W = new Queue<BasicBlock>(DefSites[v]);
		HashSet<BasicBlock> PhiInserted = new();

		while (W.Count > 0)
		{
			var b = W.Dequeue();
			foreach (var y in DF[b])
			{
				if (!PhiInserted.Contains(y))
				{
					InsertPhi(y, v); // 在 y 的开头插入 φ
					PhiInserted.Add(y);

					// φ 本身也算一次“定义”
					W.Enqueue(y);
				}
			}
		}
		```

		`InsertPhi(y, v)` 就是在基本块 y 的开头插入：

		```
		v = φ(v_from_pred1, v_from_pred2, ...)
		```

		你可以用一个专门的 Instruction 类型表示 φ。

		---

		# ④ Rename Pass（重命名变量版本）

		这是 SSA 的核心，也是最容易写错的地方。  
		但我给你一个 **最小可用、完全适合你字节码的版本**。

		你需要：

		```csharp
		Dictionary<int, Stack<int>> VersionStack; // varId -> stack of versions
		Dictionary<int, int> VersionCounter;      // varId -> next version number
		```

		初始化：

		```csharp
		foreach (var varId in AllVars)
		{
			VersionStack[varId] = new Stack<int>();
			VersionCounter[varId] = 0;

			// 初始版本 v0
			VersionStack[varId].Push(0);
		}
		```

		### rename 函数（递归遍历支配树）

		```csharp
		void Rename(BasicBlock b)
		{
			// 1. 重写 φ 节点
			foreach (var phi in b.PhiNodes)
			{
				int v = phi.VarId;
				int newVersion = VersionCounter[v]++;
				phi.ResultVersion = newVersion;
				VersionStack[v].Push(newVersion);
			}

			// 2. 重写普通指令
			foreach (var inst in b.Instructions)
			{
				if (inst.Op == Ld_MethodVar)
				{
					int v = inst.VarIndex;
					inst.SsaVersion = VersionStack[v].Peek();
				}
				else if (inst.Op == Store_MethodVar)
				{
					int v = inst.VarIndex;
					int newVersion = VersionCounter[v]++;
					inst.SsaVersion = newVersion;
					VersionStack[v].Push(newVersion);
				}
			}

			// 3. 更新后继块的 φ 参数
			foreach (var succ in b.Successors)
			{
				foreach (var phi in succ.PhiNodes)
				{
					int v = phi.VarId;
					phi.AddIncoming(b, VersionStack[v].Peek());
				}
			}

			// 4. 递归处理支配树的子节点
			foreach (var child in DomTreeChildren[b])
				Rename(child);

			// 5. 回溯（pop）
			foreach (var inst in b.Instructions)
				if (inst.Op == Store_MethodVar)
					VersionStack[inst.VarIndex].Pop();

			foreach (var phi in b.PhiNodes)
				VersionStack[phi.VarId].Pop();
		}
		```

		这段代码你可以直接翻译成 C#。

		---
		 * 

		### 一、目标再确认一下

		当前状态（SSA 之后）：

		- 每个变量有多个版本：`a0, a1, a2...`
		- 合流块里有 φ：

		```text
		B1: a1 = ...
			goto M

		B2: a2 = ...
			goto M

		M:  a3 = φ(a1, a2)
			x1 = a3 + 1
		```

		**目标：**

		- 消灭所有 φ
		- 把 SSA 变量映射回“普通变量 + 赋值”
		- 最终字节码里只有普通指令（包括你之后的 stackSlot load/store）

		---

		### 二、核心思想：φ 变成前驱块里的 copy

		上面这个例子，φ 的语义是：

		> 如果从 B1 来，就用 a1；如果从 B2 来，就用 a2。

		SSA destruction 的标准做法是：

		- 在 **每个前驱块的末尾** 插入一条 copy，把对应版本写到“合流版本”上。

		也就是变成：

		```text
		B1: a1 = ...
			a3 = a1
			goto M

		B2: a2 = ...
			a3 = a2
			goto M

		M:  x1 = a3 + 1
		```

		此时：

		- φ 消失了
		- 语义保持不变
		- 解释器只需要执行普通赋值

		---

		### 三、实现步骤（按块和 φ 来处理）

		假设你在 SSA 阶段有这样的结构：

		```csharp
		class Phi
		{
			public int VarId; // 哪个原始变量，比如 local #3
			public int ResultVersion; // φ 产生的 SSA 版本，比如 a3
			public List<(BasicBlock Pred, int IncomingVersion)> Inputs;
		}
		```

		#### 步骤 1：遍历所有基本块的 φ

		伪代码：

		```csharp
		foreach (var block in Blocks)
		{
			foreach (var phi in block.PhiNodes)
			{
				int targetVersion = phi.ResultVersion; // 比如 a3

				foreach (var (pred, incomingVersion) in phi.Inputs)
				{
					// 在 pred 的末尾插入一条 copy：a3 = aX
					InsertCopyAtEnd(pred, targetVersion, incomingVersion, block);
				}
			}

			// φ 自己从 block.PhiNodes 里删掉
			block.PhiNodes.Clear();
		}
		```

		这里有两个细节要注意。

		---

		### 四、细节 1：避免在“关键边”上插入指令（critical edge）

		如果某条边是：

		- `pred.Successors.Count > 1`（pred 有多个后继）
		- `block.Predecessors.Count > 1`（block 有多个前驱）

		这条边就是 **critical edge**，在它上面插入 copy 会影响别的路径。

		标准做法：

		- 对每条 critical edge `pred → block`：
		  - 新建一个中间块 `mid`
		  - 把边改成：`pred → mid → block`
		  - 把 copy 插到 `mid` 里

		伪代码：

		```csharp
		BasicBlock EnsureNonCriticalEdge(BasicBlock pred, BasicBlock succ)
		{
			bool critical = pred.Successors.Count > 1 && succ.Predecessors.Count > 1;
			if (!critical)
				return pred;

			// 创建新块 mid
			var mid = new BasicBlock { ... };

			// 修改 CFG：pred -> mid -> succ
			pred.Successors.Remove(succ);
			pred.Successors.Add(mid);
			mid.Predecessors.Add(pred);

			mid.Successors.Add(succ);
			succ.Predecessors.Remove(pred);
			succ.Predecessors.Add(mid);

			// mid 里只放一条跳转到 succ 的指令
			mid.Instructions.Add(new Instruction { Op = Jump, Target = succ });

			return mid;
		}
		```

		然后在插 copy 时：

		```csharp
		var place = EnsureNonCriticalEdge(pred, block);
		InsertCopyAtEnd(place, targetVersion, incomingVersion);
		```

						 */

		private static int OptimizeBlockSSAVariable(ControlFlowGraph cfg,int slotcount , CompileContext context)
		{
			if (cfg.Blocks.Count == 0)
				return slotcount;



			//基本块级优化变量读取。
			//如果这个方法不被闭包引用，同时也不是闭包 则变量除了赋值外不可能改变值

			//额外，如果变量类型是基本类型，则不可能发生cache问题：
			// var a = [1,2];
			// var b = a;
			// a = 1;
			// 这种情况下，a = 1时，b的stackslot就失效了。如果b的类型是primitive,或者b的赋值来源都是primitive,则不会发生cache


			if (!cfg.Method.Flags.HasFlag( MethodFlags.Generator )|| cfg.Method.Flags.HasFlag( MethodFlags.ASYNC ))//!cfg.Method.Flags.HasFlag(MethodFlags.NeedActivation))
			{
				

				#region 查找是否有修改上级变量的地方。如果有必须保守
				bool isModifyParentVar = false;
				foreach (var instruction in cfg.Blocks.SelectMany(b => b.Instructions))
				{
					if (instruction.INS_Code == INS_Code.ld_ScopeH)
					{
						//INS_Ld_ScopeHeap ld_ScopeHeap = (INS_Ld_ScopeHeap)instruction;
						//if (cfg.Method.Body._link_codescope.index != ld_ScopeHeap.heap.ScopeIndex)
						//{
						//	isModifyParentVar = true;
						//	break;
						//}
					}
					else if (instruction.INS_Code == INS_Code.storeScopeH) //只有这一个途径!
					{
						INS_Store_ScopeHeap store_ScopeHeap = (INS_Store_ScopeHeap)instruction;
						if (cfg.Method.Body._link_codescope.index != store_ScopeHeap.heap.ScopeIndex)
						{
							isModifyParentVar = true;
							break;
						}
					}
					else if (instruction.INS_Code == INS_Code.ld_function)
					{
						//INS_Ld_Function ld_Function = (INS_Ld_Function)instruction;
						//if (cfg.Method.Body._link_codescope.index != ld_Function.heapLocater.ScopeIndex)
						//{
						//	isModifyParentVar = true;
						//	break;
						//}
					}
					else if (instruction.INS_Code == INS_Code.ld_function_bindglobal_call)
					{
						//INS_Ld_Function_BindGlobal_Call ld_function_bindg_call = (INS_Ld_Function_BindGlobal_Call)instruction;
						//if (cfg.Method.Body._link_codescope.index != ld_function_bindg_call.heapLocater.ScopeIndex)
						//{
						//	isModifyParentVar = true;
						//	break;
						//}
					}

				}
				#endregion

				#region 再查找被下级闭包修改的变量，这些变量无法优化。
				HashSet<int> refByChild = new HashSet<int>();
				if (cfg.Method.Flags.HasFlag(MethodFlags.NeedActivation))
				{
					var p = cfg.Method.Container;
					while (!(p is ASScript))
					{
						p = p._link_codescope.Parent.Container;
					}

					ASScript script = (ASScript)p;

					foreach (var item in script.allContainers)
					{
						if (item._link_codescope.Kind == CodeScopeKind.Method &&
							((ASMethodBody)item)._link_codescope.index != cfg.Method.Body._link_codescope.index &&
							item._link_codescope.Parent.Kind == CodeScopeKind.Method
							)
						{
							Disassembler.Disassemble(((ASMethodBody)item).ByteCode, out int slotCount, out NaNBoxing[] constants, out Instruction[] instructions);
							foreach (var instruction in instructions)
							{
								if (instruction.INS_Code == INS_Code.ld_ScopeH)
								{
									//INS_Ld_ScopeHeap ld_ScopeHeap = (INS_Ld_ScopeHeap)instruction;
									//if (cfg.Method.Body._link_codescope.index == ld_ScopeHeap.heap.ScopeIndex)
									//{
									//	refByChild.Add(ld_ScopeHeap.heap.MemberIndex);
									//}
								}
								else if (instruction.INS_Code == INS_Code.storeScopeH)
								{
									INS_Store_ScopeHeap store_ScopeHeap = (INS_Store_ScopeHeap)instruction;
									if (cfg.Method.Body._link_codescope.index == store_ScopeHeap.heap.ScopeIndex)
									{
										refByChild.Add(store_ScopeHeap.heap.MemberIndex);
									}
								}
								else if (instruction.INS_Code == INS_Code.ld_function)
								{
									INS_Ld_Function ld_Function = (INS_Ld_Function)instruction;
									if (cfg.Method.Body._link_codescope.index == ld_Function.heapLocater.ScopeIndex)
									{
										refByChild.Add(ld_Function.heapLocater.MemberIndex);
									}
								}
								else if (instruction.INS_Code == INS_Code.ld_function_bindglobal_call)
								{
									INS_Ld_Function_BindGlobal_Call ld_function_bindg_call = (INS_Ld_Function_BindGlobal_Call)instruction;
									if (cfg.Method.Body._link_codescope.index == ld_function_bindg_call.heapLocater.ScopeIndex)
									{
										refByChild.Add(ld_function_bindg_call.heapLocater.MemberIndex);
									}
								}

							}

						}
					}

				}

				#endregion



				Dictionary<int, Dictionary<Instruction, int>> variables_ssa = new Dictionary<int, Dictionary<Instruction, int>>();
				Dictionary<int , Dictionary<BasicBlock, PhiNode>> variables_phi = new Dictionary<int, Dictionary<BasicBlock, PhiNode>>();

				int SSA_slot = slotcount;
				var flags = cfg.Blocks.SelectMany(b => b.Instructions).Where(i => i.INS_Code == INS_Code.flag).Select(i => (INS_Flag)i)
					.Where(i => i.flag_id < 0xfffff8);
				int flagseed = flags.Any() ? flags.Max(i => i.flag_id) + 1 : 0;

				List<SSA_Split> splitblocks = new List<SSA_Split>();

				for (int i = 0; i < cfg.Method.Body._link_codescope.Members.Count; i++)
				{
					var scopemember = cfg.Method.Body._link_codescope.Members[i];

					if (refByChild.Contains(i)) //闭包引用变量，跳过
					{
						continue;
					}

					//SSA
					var DefSites = new List<BasicBlock>(); //变量定义点
					if (scopemember.Kind == ScopeMemberKind.Parameter)
					{
						DefSites.Add(cfg.Blocks[0]); // 参数是传入的，第一个块就是v0
					}
					else if (
						scopemember.QName.Name.IndexOf("%&IterObjHolder%") >= 0
						||
						scopemember.QName.Name.IndexOf("%&IterHolder%") >= 0
						||
						scopemember.QName.Name.IndexOf("%&IterContext%") >= 0
						
						)
					{
						continue;
					}



					foreach (var block in cfg.Blocks)
					{
						foreach (var inst in block.Instructions)
						{
							if (inst.INS_Code == INS_Code.storeMethodVariable && ((INS_Store_MethodVariable)inst).heap.MemberIndex == i)
							{
								if (!DefSites.Contains(block))
									DefSites.Add(block);
							}
							else if (inst.INS_Code == INS_Code.ld_memberInitValue && ((INS_Ld_MemberInitValue)inst).heap.MemberIndex == i)
							{
								if (!DefSites.Contains(block))
									DefSites.Add(block);
							}
						}
					}

					//在支配边界插入 φ 节点				
					Queue<BasicBlock> W = new Queue<BasicBlock>(DefSites);
					Dictionary<BasicBlock, PhiNode> PhiInserted = new();
					while (W.Count > 0)
					{
						var b = W.Dequeue();
						foreach (var y in b.DominanceFrontier)
						{
							if (!PhiInserted.ContainsKey(y))
							{
								//InsertPhi(y, v); // 在 y 的开头插入 φ
								PhiInserted.Add(y, new PhiNode() { Incoming = new Dictionary<BasicBlock, int>() });
								// φ 本身也算一次“定义”
								W.Enqueue(y);
							}
						}
					}

					//Rename Pass

					Stack<int> VersionStack = new(); // varId -> stack of versions
					VersionStack.Push(0);
					int VersionCounter = 1;      // varId -> next version number
												 // 由于AS3语言特性，变量没有赋值也可以使用，所以第一次赋值前，版本为0,第一次赋值版本+1。
												 // 版本0时获取的值 如果是参数就是传入的值，否则是默认值

					Dictionary<Instruction, int> SSA_Version = new();

					
					void Rename(BasicBlock b)
					{
						// 1. 重写 φ 节点
						if (PhiInserted.ContainsKey(b))
						{
							var phi = PhiInserted[b];

							int newVersion = VersionCounter++;
							phi.ResultVersion = newVersion;
							VersionStack.Push(newVersion);

						}

						// 2. 重写普通指令
						foreach (var inst in b.Instructions)
						{
							if (inst.INS_Code == INS_Code.ld_methodVariable && ((INS_Ld_MethodVariable)inst).heap.MemberIndex == i)
							{
								SSA_Version.Add(inst, VersionStack.Peek());
								//inst.SsaVersion = VersionStack.Peek();
							}
							else if ((inst.INS_Code == INS_Code.storeMethodVariable && ((INS_Store_MethodVariable)inst).heap.MemberIndex == i)
								||
								(inst.INS_Code == INS_Code.ld_MethodVariableInitValue && ((INS_Ld_MethodVariableInitValue)inst).heap.MemberIndex == i)
								)
							{
								int newVersion = VersionCounter++;
								SSA_Version.Add(inst, newVersion);
								VersionStack.Push(newVersion);
							}
						}

						// 3. 更新后继块的 φ 参数
						foreach (var succ in b.Successors)
						{
							if (PhiInserted.ContainsKey(succ))
							{
								var phi = PhiInserted[succ];
								phi.Incoming.Add(b, VersionStack.Peek());
							}
						}

						// 4. 递归处理支配树的子节点
						foreach (var child in cfg.Blocks.Where(bl => bl.Idom == b && bl != b))//DomTreeChildren[b])
							Rename(child);

						// 5. 回溯（pop）
						foreach (var inst in b.Instructions)
						{
							if ((inst.INS_Code == INS_Code.storeMethodVariable && ((INS_Store_MethodVariable)inst).heap.MemberIndex == i)
								||
								(inst.INS_Code == INS_Code.ld_MethodVariableInitValue && ((INS_Ld_MethodVariableInitValue)inst).heap.MemberIndex == i)
								)
							{
								VersionStack.Pop();
							}
						}

						if (PhiInserted.ContainsKey(b))
						{
							VersionStack.Pop();
						}
					}

					if (scopemember.Kind == ScopeMemberKind.Slot && scopemember.QName.Name.StartsWith("%") && !scopemember.QName.Name.EndsWith("@--"))
					{
						var catchblock = cfg.Blocks.First(b => b.Instructions.Count > 0 && b.Instructions[0].INS_Code == INS_Code.catch_enter && ((INS_Catch_Enter)b.Instructions[0]).catch_exception.MemberIndex == i);
						Rename(catchblock);
					}
					else
					{
						Rename(cfg.Blocks[0]);
					}


					//分配SSA版本的stackslot
					{
						Dictionary<int, int> replace = new Dictionary<int, int>();
						foreach (var item in SSA_Version)
						{
							int slot = SSA_slot + item.Value;


							if (item.Key.INS_Code == INS_Code.ld_methodVariable)
							{
								replace.Add(item.Key.dst.index, slot);
							}
							else if (item.Key.INS_Code == INS_Code.storeMethodVariable)
							{
								replace.Add(((INS_Store_MethodVariable)item.Key).convertedloc.index, slot);
							}
							else
							{
								replace.Add(((INS_Ld_MethodVariableInitValue)item.Key).dst.index, slot);
							}

						}

						if (cfg.Blocks.SelectMany(b => b.Instructions).Any(ii => ii.INS_Code == INS_Code.iter_get &&
							ii.GetUse().Any(d => replace.ContainsKey(d.index)) //iter_get是特殊情况，暂时不能SSA
						))
						{
							continue;
						}

						foreach (var ins in cfg.Blocks.SelectMany(b => b.Instructions))
						{
							ins.RemappingSlots(replace);
						}
					}

					variables_ssa.Add(i, SSA_Version);
					variables_phi.Add(i, PhiInserted);

					//φ改成copy
					foreach (var phi in PhiInserted)
					{
						var succ = phi.Key;
						if (succ == cfg.Blocks[cfg.Blocks.Count - 1])
						{
							continue;
						}

						int targetVersion = phi.Value.ResultVersion; // 比如 a3

						foreach (var (pred, incomingVersion) in phi.Value.Incoming)
						{
							
							// 在 pred 的合适位置插入move
							if (pred.Successors.Count > 1 && succ.Predecessors.Count > 1 &&
								(succ.Instructions[0].INS_Code == INS_Code.flag
									&&
									((pred.Instructions[pred.Instructions.Count - 1].INS_Code == INS_Code.if_false_goto
										||
										pred.Instructions[pred.Instructions.Count - 1].INS_Code == INS_Code.if_true_goto
										||
										pred.Instructions[pred.Instructions.Count - 1].INS_Code == INS_Code.goto_flag	
										)
										&&
										((INS_Flag) succ.Instructions[0] ).flag_id == ControlFlowGraphBuilder.GetFlagId(pred.Instructions[pred.Instructions.Count -1])
										)
									)
								)
							{
								//拆边。
								
								var ssablock = splitblocks.FirstOrDefault(s=>s.succ == succ && s.pred == pred);
								if (ssablock == null)
								{
									int flag = flagseed++;
									BasicBlock iblock = new BasicBlock();
									iblock.BlockId = succ.BlockId - 5;
									iblock.OriginalIndex = succ.OriginalIndex - 5;
									iblock.TryStmtId = pred.TryStmtId;
									iblock.Instructions = new List<Instruction>();
									iblock.IsReachable = true;


									INS_Flag _Flag = new INS_Flag(succ.Instructions[0].token);
									_Flag.flag_id = flag;
									iblock.Instructions.Add(_Flag);


									BasicBlock gotoblock = new BasicBlock();
									gotoblock.BlockId = iblock.BlockId - 1;
									gotoblock.OriginalIndex = iblock.OriginalIndex - 1;
									gotoblock.TryStmtId = iblock.TryStmtId;
									gotoblock.Instructions = new List<Instruction>();
									

									INS_Goto _Goto = new INS_Goto(succ.Instructions[0].token);
									_Goto.flag_id = ((INS_Flag)succ.Instructions[0]).flag_id;
									gotoblock.Instructions.Add(_Goto);
									gotoblock.JumpTargetFlagId = _Goto.flag_id;



									//iblock.Predecessors.Add(gotoblock);
									gotoblock.Successors.Add(succ);


									ssablock = new SSA_Split() { succ = succ, pred = pred, inserted = iblock,beforeinserted = gotoblock };
									splitblocks.Add(ssablock);
								}

								INS_Move move = new INS_Move(succ.Instructions[0].token);
								move.source.index = SSA_slot + incomingVersion;
								move.dst.index = SSA_slot + targetVersion;

								ssablock.inserted.Instructions.Insert(1, move);

							}
							else
							{
								//用一个简单办法：如果pred里面有 SSA_Version的指令，
								//                            如果没有定值，就添加到最后一个ld后面，
								//                            如果有定值,就修改定值的slot
								//  如果没有SSA_version里的指令，则插入到第一个可能抛出异常和跳转的指令的前面

								var def = pred.Instructions.FirstOrDefault(ins => SSA_Version.ContainsKey(ins) && SSA_Version[ins] == incomingVersion &&
																	(ins.INS_Code == INS_Code.ld_MethodVariableInitValue
																	||
																	ins.INS_Code == INS_Code.storeMethodVariable
																	));

								if (def != null)
								{
									Dictionary<int, int> replace = new Dictionary<int, int>();
									replace.Add(SSA_slot + incomingVersion, SSA_slot + targetVersion);
									foreach (var ins in cfg.Blocks.SelectMany(b => b.Instructions))
									{
										ins.RemappingSlots(replace);
									}

									//int index = pred.Instructions.IndexOf(def);
									//INS_Move move = new INS_Move(def.token);
									//move.source.index = SSA_slot + incomingVersion;
									//move.dst.index = SSA_slot + targetVersion;

									//pred.Instructions.Insert(index + 1, move);
								}
								else
								{
									var use = pred.Instructions.LastOrDefault(ins => SSA_Version.ContainsKey(ins) && SSA_Version[ins] == incomingVersion &&
																	(ins.INS_Code == INS_Code.ld_methodVariable));

									if (use != null)
									{
										int index = pred.Instructions.IndexOf(use);
										INS_Move move = new INS_Move(use.token);
										move.source.index = SSA_slot + incomingVersion;
										move.dst.index = SSA_slot + targetVersion;

										pred.Instructions.Insert(index + 1, move);

									}
									else
									{
										INS_Move move = new INS_Move(pred.Instructions[0].token);
										move.source.index = SSA_slot + incomingVersion;
										move.dst.index = SSA_slot + targetVersion;

										if (pred.Instructions.Count > 0 &&
											(pred.Instructions[0].INS_Code == INS_Code.flag
											||
											pred.Instructions[0].INS_Code == INS_Code.try_enter
											||
											pred.Instructions[0].INS_Code == INS_Code.catch_enter
											||
											pred.Instructions[0].INS_Code == INS_Code.finally_enter
											)
											)
										{
											pred.Instructions.Insert(1, move);
										}
										else
										{
											pred.Instructions.Insert(0, move);
										}
									}
								}
							}
						}
					}

					int ssa_insmaxversion = 0; if (SSA_Version.Count > 0) ssa_insmaxversion = SSA_Version.Max(ins => ins.Value);
					int phi_resmaxversion = 0; if (PhiInserted.Count > 0) phi_resmaxversion = PhiInserted.Max(phi => phi.Value.ResultVersion);
					SSA_slot += Math.Max(ssa_insmaxversion, phi_resmaxversion) + 1;
				}

				//将新增边加入cfg
				foreach (var split in splitblocks)
				{
					cfg.Blocks.Add(split.beforeinserted);
					cfg.Blocks.Add(split.inserted);

					split.pred.Successors.Remove(split.succ);
					split.succ.Predecessors.Remove(split.pred);

					split.inserted.Successors.Add(split.succ);
					split.inserted.Predecessors.Add(split.pred);

					split.pred.Successors.Add(split.inserted);
					split.succ.Predecessors.Add(split.inserted);

					

					foreach (var pre in split.succ.Predecessors)
					{
						if (pre.OriginalIndex == split.succ.OriginalIndex - 10)
						{
							pre.Successors.Add(split.beforeinserted);
							pre.Successors.Remove(split.succ);
							
							split.beforeinserted.Predecessors.Add(pre);

							split.beforeinserted.IsReachable = true;

							split.succ.Predecessors.Add(split.beforeinserted);

							break;
						}
					}




					int flag = ((INS_Flag)split.inserted.Instructions[0]).flag_id;

					split.pred.JumpTargetFlagId = flag;

					var ins = split.pred.Instructions[split.pred.Instructions.Count - 1];
					if (ins.INS_Code == INS_Code.if_true_goto)
					{
						((INS_If_True_Goto)ins).flag_id = flag;
					}
					else if (ins.INS_Code == INS_Code.if_false_goto)
					{
						((INS_If_False_Goto)ins).flag_id = flag;
					}
					else if (ins.INS_Code == INS_Code.goto_flag)
					{
						((INS_Goto)ins).flag_id = flag;
					}
					else
					{
						throw new InvalidOperationException();
					}

				}


				slotcount = SSA_slot;



				var instructionType = DetectType( cfg.Method ,cfg.Blocks.OrderBy(b=>b.OriginalIndex).SelectMany(b => b.Instructions).ToList(), context);

				
				HashSet<Instruction> safeInstructions = new(); //安全类型
				HashSet<Instruction> newinstanceDefSite = new(); //使用newinstance初始化值


				#region 查询变量类型是否安全。

				//bool changed = false;
				//do
				{
					//changed = false;

					foreach (var item in variables_ssa)
					{
						var SSA = item.Value;
						var scopemember = cfg.Method.Body._link_codescope.Members[item.Key];
						if (!scopemember.TypeKind.IsHeapType() || scopemember.TypeKind == TypeKind.String)
						{							
							//类型安全。
							foreach (var ins in SSA.Keys)
							{
								//changed = safeInstructions.Add(ins);
								safeInstructions.Add(ins);
							}
							continue;
						}



						foreach (var testIns in SSA.Keys.Where(i => i.INS_Code == INS_Code.ld_MethodVariableInitValue || i.INS_Code == INS_Code.storeMethodVariable))
						{
							if (safeInstructions.Contains(testIns) || newinstanceDefSite.Contains(testIns)) 
								continue;

							if (testIns.INS_Code == INS_Code.ld_MethodVariableInitValue)
							{
								//changed = true;
								safeInstructions.Add(testIns);
								continue;
							}

							INS_Store_MethodVariable storeVar = (INS_Store_MethodVariable)testIns;

							var defsourcelist = FindStackSlotDefAt(storeVar.dst,cfg);

							Debug.Assert(defsourcelist.Count > 0);

							if (defsourcelist.All(i => i.Item1.INS_Code == INS_Code.new_instance))
							{
								//if (!isModifyParentVar)
								{
									newinstanceDefSite.Add(testIns);
									//changed = true;
								}
							}
							else if( 
							//	defsourcelist.All( i=> safeInstructions.Contains(i)								
							//					|| i.INS_Code == INS_Code.ld_const
							//					|| i.INS_Code == INS_Code.ld_class
							//					|| i.INS_Code == INS_Code.ld_true
							//					|| i.INS_Code == INS_Code.ld_false
							//					|| i.INS_Code == INS_Code.ld_undefined
							//					|| i.INS_Code == INS_Code.ld_MethodVariableInitValue
							//					|| i.INS_Code == INS_Code.increment_decrement
							//					|| i.INS_Code == INS_Code.delete
							//					|| i.INS_Code == INS_Code.get_is
							//					|| i.INS_Code == INS_Code.get_in
							//					|| i.INS_Code == INS_Code.get_instanceof
							//					|| i.INS_Code== INS_Code.bitwise
							//					|| i.INS_Code == INS_Code.logic_comparison
							//					|| i.INS_Code == INS_Code.logic_not
							//					|| i.INS_Code == INS_Code.strict_eq
							//					|| i.INS_Code == INS_Code.strict_neq
							//					|| i.INS_Code == INS_Code.equal
												
												
							//) 
								
								defsourcelist.All( i=> safeInstructions.Contains(i.Item1) || ( instructionType.ContainsKey(i.Item1) &&(
													instructionType[i.Item1][i.Item2].DefType == InstructionDefType.primitive	||							
													instructionType[i.Item1][i.Item2].DefType == InstructionDefType.Struct	||							
													instructionType[i.Item1][i.Item2].DefType == InstructionDefType.obj	||							
													instructionType[i.Item1][i.Item2].DefType == InstructionDefType.global	||							
													instructionType[i.Item1][i.Item2].DefType == InstructionDefType.asclass							
								)) )

								)
							{
								

								safeInstructions.Add(storeVar);								
								//changed = true;
							}


						}


					}


				}// while (changed);

				#endregion


				//SSA优化
				foreach (var item in variables_ssa)
				{
					var SSA = item.Value;
					var scopemember = cfg.Method.Body._link_codescope.Members[item.Key];

					//version 0: 版本0，可以像ld_const那样优化					
					{
						var zero = SSA.Where(s => s.Value == 0).Select(i => i.Key).ToList();
						if (zero.Count > 1 && 
							!(scopemember.Kind == ScopeMemberKind.Slot && scopemember.QName.Name.StartsWith("%") && !scopemember.QName.Name.EndsWith("@--")) //排除 catch(e)
							)
						{
							Debug.Assert(zero.All(i => i.INS_Code == INS_Code.ld_methodVariable));

							var atblocks = cfg.Blocks.Where(b => b.Instructions.Any(i => zero.Contains(i))).ToList();
							var dom = FindCommDom(atblocks);

							var loop = cfg.toplevelloops.Where(l => l.FindLoop(dom) != null).FirstOrDefault();
							if (loop != null)
							{
								Debug.Assert(loop.loop.firstNode.Predecessors.Contains(loop.loop.firstNode.Idom));
								dom = loop.loop.firstNode.Idom;
							}


							var ld = zero.First();
							foreach (var block in cfg.Blocks)
							{
								block.Instructions.RemoveAll(ins => zero.Contains(ins));
							}

							int newslot = slotcount++;
							foreach (var l in zero)
							{
								Dictionary<int, int> replace = new Dictionary<int, int> { { l.dst.index, newslot } };

								foreach (var ins in cfg.Blocks.SelectMany(bb => bb.Instructions))
								{
									ins.RemappingSlots(replace);
								}
							}

							ld.dst.index = newslot;
							if (dom.Instructions.Count > 0 &&
													(dom.Instructions[0].INS_Code == INS_Code.flag
													||
													dom.Instructions[0].INS_Code == INS_Code.try_enter
													||
													dom.Instructions[0].INS_Code == INS_Code.catch_enter
													||
													dom.Instructions[0].INS_Code == INS_Code.finally_enter
													)
													)
							{
								dom.Instructions.Insert(1, ld);
							}
							else
							{
								dom.Instructions.Insert(0, ld);
							}


						}

					}
					if (SSA.Count > 0)
					{
						
						int maxversion = SSA.Max(s => s.Value);

						for (int v = 1; v < maxversion + 1; v++)
						{
							var version_ins = SSA.Where(s => s.Value == v).Select(i => i.Key).ToList();

							if (version_ins.Count > 1)
							{

								void RemoveInBlock()
								{
									//基本块内，如果一行ld和下一行ld之间没有危险代码，就删除下一行

									foreach (var b in cfg.Blocks)
									{
										var first = b.Instructions.FirstOrDefault( i=>version_ins.Contains(i) );
										if (first != null)
										{
											
											int index = b.Instructions.IndexOf(first);
											

											void DoRemove(int stindex,BasicBlock bb)
											{
												int i = stindex + 1;
												for (; i < bb.Instructions.Count; i++)
												{
													var ins = bb.Instructions[i];
													if (version_ins.Contains(ins))
													{
														bb.Instructions.Remove(ins);
														i--;
													}
													else
													{
														//如果是危险代码，则break.

														//对其他变量的赋值都是危险代码。（在非闭包且没有下级闭包引用时，除了这2个指令没有其他方法可以修改其他变量,）
														if ((ins.INS_Code == INS_Code.ld_MethodVariableInitValue || ins.INS_Code == INS_Code.storeMethodVariable)
															&&
															!safeInstructions.Contains(ins)
															)
														{
															break;
														}

														if (!isModifyParentVar && refByChild.Count == 0)
														{
															//在非闭包且没有下级闭包引用
														}
														else
														{
															//如果必须保守，那么抛异常同意危险
															if (ins.MaybeRaiseError())
																break;
														}

													}

												}

												if (i == bb.Instructions.Count) //正常结束，可以带到后续
												{

													foreach (var dom in bb.Successors.Where(c => c.Idom == bb && c != bb))
													{ 
														DoRemove(-1,dom);
													}
												}

											}


											DoRemove(index, b);
											
											
											
										}
									}


								}


								var vdefs = SSA.Select(k => k.Key).Where(i => i.INS_Code != INS_Code.ld_methodVariable);
								if ( vdefs.Count() >0 &&  vdefs.All( d=>safeInstructions.Contains(d) ) )
								{
									var toremove = version_ins.Where(i => i.INS_Code == INS_Code.ld_methodVariable).ToList();
									foreach (var block in cfg.Blocks)
									{
										block.Instructions.RemoveAll(ins => toremove.Contains(ins));
									}
								}
								else
								{
									var def = version_ins.FirstOrDefault(i => i.INS_Code == INS_Code.ld_MethodVariableInitValue || i.INS_Code == INS_Code.storeMethodVariable);
									if (def != null)
									{
										var deftry = GetTryStmt(def, cfg);
										bool IsTrySafe(Instruction instruction) //还必须考虑Try Catch的影响！
										{
											if (deftry.Count == 0)
											{
												return true;
											}
											else
											{

												var itry = GetTryStmt(instruction, cfg);

												if (itry.Count < deftry.Count)
													return false;

												var ii = itry.Peek();
												return deftry.Any(d => d.tryid == ii.tryid && d.trystate == ii.trystate);
											}
										}

										if (safeInstructions.Contains(def) || newinstanceDefSite.Contains(def))
										{
											var toremove = version_ins.Where(i => i.INS_Code == INS_Code.ld_methodVariable && IsTrySafe(i)).ToList();
											foreach (var block in cfg.Blocks)
											{
												block.Instructions.RemoveAll(ins => toremove.Contains(ins));
											}
										}
										else
										{
											//单block优化
											RemoveInBlock();
										}
									}
									else
									{
										//从phi中来
										var phi = variables_phi[item.Key].Where( p=>p.Value.ResultVersion == v ).ToList();
										Debug.Assert(phi.Count == 1);

										var incoming_vers = phi[0].Value.Incoming.Values.Where(v=>v>0).ToList(); //v0没有定值位置
										
										List<Instruction> income_def = new List<Instruction>();

										HashSet<int> visited = new HashSet<int>();

										while (incoming_vers.Count > 0)
										{
											int ver = incoming_vers[0];
											incoming_vers.RemoveAt(0);

											visited.Add(ver);


											var defsite = SSA.Where(s => s.Value == ver).Select(ssa_list => ssa_list.Key)
												.Where(i => i.INS_Code == INS_Code.ld_MethodVariableInitValue || i.INS_Code == INS_Code.storeMethodVariable).ToList()
												;
											if (defsite.Count > 0)
											{
												income_def.AddRange(defsite);
											}
											else
											{
												var nphi = variables_phi[item.Key].Where(p => p.Value.ResultVersion == ver).ToList();
												Debug.Assert(nphi.Count == 1);

												foreach (var come in nphi[0].Value.Incoming.Values.Where(v => v > 0))
												{
													if (!visited.Contains(come))
													{
														incoming_vers.Add(come);
														visited.Add(come);
													}
												}
											}
										}

										Debug.Assert(income_def.Count > 0);

										if (income_def.All(d => safeInstructions.Contains(d) || newinstanceDefSite.Contains(d)))
										{
											var toremove = version_ins.Where(i => i.INS_Code == INS_Code.ld_methodVariable).ToList();
											foreach (var block in cfg.Blocks)
											{
												block.Instructions.RemoveAll(ins => toremove.Contains(ins));
											}
										}
										else
										{
											RemoveInBlock();
										}
									}

								}
							}

						}
					}
				}




				cfg.Blocks.Sort((b1, b2) => { return b1.OriginalIndex - b2.OriginalIndex; });

				return slotcount;
			}
			else
			{
				return slotcount;
			}
		}



		private static int RemoveBlockMove(ControlFlowGraph cfg,int slotCount)
		{
			//算法：用干涉图计算 mv 的src和dst之间是不是没有干涉。如果没有，则直接使用同一个槽然后把mv删掉。


			var tmp = cfg.BuildTemporaryCFGForInstructionLevel();
			//tmp中移除所有的move。然后计算干涉图
			foreach (var block in tmp.Blocks)
			{
				block.Instructions.RemoveAll(i => i.INS_Code == INS_Code.move);
			}
			var interference =  tmp.ComputeInterferenceGraph();

			var all = cfg.Blocks.SelectMany(bb => bb.Instructions).Where(i => i.INS_Code == INS_Code.move).Select(i=>(INS_Move)i).ToList();
			var toremove = new List<INS_Move>();

			foreach (var mv in all)
			{
				if (!interference.ContainsKey(mv.source.index))
				{
					toremove.Add(mv);
				}
				else
				{
					if (!interference[mv.source.index].Contains(mv.dst.index))
					{
						toremove.Add(mv);
					}
				}
			}

			foreach (var mv in toremove)
			{
				if (mv.source.index != mv.dst.index)
				{
					int newslot = slotCount++;

					Dictionary<int, int> toreplace = new Dictionary<int, int>();
					toreplace.Add(mv.source.index, newslot);
					toreplace.Add(mv.dst.index, newslot);

					foreach (var b in cfg.Blocks.SelectMany(bb => bb.Instructions))
					{
						b.RemappingSlots(toreplace);
					}
				}
			}

			foreach (var b in cfg.Blocks)
			{
				b.Instructions.RemoveAll(i => toremove.Contains(i));
			}



			//foreach (var mv in toremove.Select(r=>new Tuple<int,int>( r.source.index,r.dst.index )).ToArray() )
			//{
			//	if (mv.Item1 != mv.Item2)
			//	{
			//		var insmv = toremove.Where( r=>r.source.index == mv.Item1 && r.dst.index == mv.Item2 ).ToArray();

			//		int newslot = slotCount++;

			//		Dictionary<int, int> toreplace = new Dictionary<int, int>();
			//		toreplace.Add(mv.Item1, newslot);
			//		toreplace.Add(mv.Item2, newslot);

			//		foreach (var b in cfg.Blocks.SelectMany(bb => bb.Instructions))
			//		{

			//			b.RemappingSlots(toreplace);

			//		}

			//		foreach (var b in cfg.Blocks)
			//		{
			//			b.Instructions.Remove(insmv[0]);
			//		}



			//	}
			//}


			//foreach (var b in cfg.Blocks)
			//{
			//	b.Instructions.RemoveAll(i => toremove.Contains(i) );
			//}


			return slotCount;


		}


	}
}
