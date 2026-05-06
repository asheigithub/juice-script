using juicescript.ABC;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.runtime
{
	public partial class Player
	{
#if FORCOMPILER //编译期求值相关

		public void PrepareComputeConstExpr()
		{
			CheckRequires();

			if (Context.OBJECT == null)
			{
				throw new LoaderException("core lib not loaded.");
			}

			if (Context.OBJECT == null)
			{
				throw new LoaderException("core lib not loaded.");
			}

			EMPTY_STR = Context.GC.AllocString(""); if (EMPTY_STR == 0) { throw new LoaderException("EMPTY_STR alloc failed"); }
			Context.GC.Root.Add(Context.GC.Heap[EMPTY_STR]);

			TRUE_STR = Context.GC.AllocString("true"); if (TRUE_STR == 0) { throw new LoaderException("TRUESTR alloc failed"); }
			Context.GC.Root.Add(Context.GC.Heap[TRUE_STR]);

			FALSE_STR = Context.GC.AllocString("false"); if (FALSE_STR == 0) { throw new LoaderException("FALSESTR alloc failed"); }
			Context.GC.Root.Add(Context.GC.Heap[FALSE_STR]);

			NAN_STR = Context.GC.AllocString("NaN"); if (NAN_STR == 0) { throw new LoaderException("NANPTR alloc failed"); }
			Context.GC.Root.Add(Context.GC.Heap[NAN_STR]);

			POSITIVEINF_STR = Context.GC.AllocString("Infinity"); if (POSITIVEINF_STR == 0) { throw new LoaderException("POSITIVEINFPTR alloc failed"); }
			Context.GC.Root.Add(Context.GC.Heap[POSITIVEINF_STR]);

			NEGATIVEINF_STR = Context.GC.AllocString("-Infinity"); if (NEGATIVEINF_STR == 0) { throw new LoaderException("NEGATIVEINFPTR alloc failed"); }
			Context.GC.Root.Add(Context.GC.Heap[NEGATIVEINF_STR]);


			foreach (var script in Context.libs.SelectMany(l => l.Scripts))
			{
				if (script.__global_index__ != 0)
				{
					return;
				}
				else
				{
					//构造script的global对象
					int index = Context.GC.AllocGlobal(script);
					if (index == 0)
					{
						throw new LoaderException("out of memory");
					}
					script.__global_index__ = index;
				}

				foreach (var cls in script.allContainers.Where(c => c is ASClass).Select(c => (ASClass)c))
				{
					if (cls.__instance_index__ != 0)
					{
						return;
					}

					int index = Context.GC.AllocASClassObj(cls, Context.OBJECT.Instance);
					if (index == 0)
					{
						throw new LoaderException("out of memory");
					}

					cls.__instance_index__ = index;
				}

			}

		}

		public void PrepareComputeMemberInitValue()
		{
			CheckRequires();

			if (Context.OBJECT == null)
			{
				throw new LoaderException("core lib not loaded.");
			}

			CONSTRUCTOR_STR = Context.GC.AllocString("constructor"); if (CONSTRUCTOR_STR == 0) { throw new LoaderException("CONSTRUCTORPTR alloc failed"); }
			Context.GC.Root.Add(Context.GC.Heap[CONSTRUCTOR_STR]);

			EMPTY_STR = Context.GC.AllocString(""); if (EMPTY_STR == 0) { throw new LoaderException("EMPTY_STR alloc failed"); }
			Context.GC.Root.Add(Context.GC.Heap[EMPTY_STR]);

			TRUE_STR = Context.GC.AllocString("true"); if (TRUE_STR == 0) { throw new LoaderException("TRUESTR alloc failed"); }
			Context.GC.Root.Add(Context.GC.Heap[TRUE_STR]);

			FALSE_STR = Context.GC.AllocString("false"); if (FALSE_STR == 0) { throw new LoaderException("FALSESTR alloc failed"); }
			Context.GC.Root.Add(Context.GC.Heap[FALSE_STR]);

			NAN_STR = Context.GC.AllocString("NaN"); if (NAN_STR == 0) { throw new LoaderException("NANPTR alloc failed"); }
			Context.GC.Root.Add(Context.GC.Heap[NAN_STR]);

			POSITIVEINF_STR = Context.GC.AllocString("Infinity"); if (POSITIVEINF_STR == 0) { throw new LoaderException("POSITIVEINFPTR alloc failed"); }
			Context.GC.Root.Add(Context.GC.Heap[POSITIVEINF_STR]);

			NEGATIVEINF_STR = Context.GC.AllocString("-Infinity"); if (NEGATIVEINF_STR == 0) { throw new LoaderException("NEGATIVEINFPTR alloc failed"); }
			Context.GC.Root.Add(Context.GC.Heap[NEGATIVEINF_STR]);


			foreach (var script in Context.libs.SelectMany(l => l.Scripts))
			{
				if (script.__global_index__ != 0)
				{
					return;
				}
				else
				{
					//构造script的global对象
					int index = Context.GC.AllocGlobal(script);
					if (index == 0)
					{
						throw new LoaderException("out of memory");
					}
					script.__global_index__ = index;
				}

				foreach (var cls in script.allContainers.Where(c => c is ASClass).Select(c => (ASClass)c))
				{
					if (cls.__instance_index__ != 0)
					{
						return;
					}
					
					int index = Context.GC.AllocASClassObj(cls, Context.OBJECT.Instance);
					if (index == 0)
					{
						throw new LoaderException("out of memory");
					}

					cls.__instance_index__ = index;
				}

			}

		}

		internal bool iscomputing_initvalue;

		private List<int> computemember_cacheinstance;
		private Dictionary<ASContainer, int> computermember_cachemethodscope;

		public void ComputeMemberInit_ResertStack()
		{
			Context.StackPosition = 0;
			Context.BackTraceIndex = 0;
		}

		
		//internal ScopeMember iscomputintg_closure_initmember;

		/// <summary>
		/// 在计算时，只要读取非Const的Slot就报错。
		/// </summary>
		/// <param name="member"></param>
		/// <param name="method"></param>
		/// <param name="testswc"></param>
		/// <param name="constpoolbytecode"></param>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		/// <exception cref="EvalConstException"></exception>
		/// <exception cref="NotImplementedException"></exception>
		public NaNBoxing ComputeMemberInitValue(ScopeMember member, ASMethod method, SWCFile testswc, byte[] constpoolbytecode, 
			List<NaNBoxing> method_const, List<Tuple<int, ASClass>> list,bool loadfromcache)
		{
			if (computemember_cacheinstance == null)
			{
				computemember_cacheinstance = new List<int>();
			}
			if (computermember_cachemethodscope == null)
			{ 
				computermember_cachemethodscope = new Dictionary<ASContainer, int>();
			}


			unsafe
			{
				fixed (void* ptrsrc = constpoolbytecode)//handle.AddrOfPinnedObject().ToPointer();
				{				
					int src_count = *((int*)ptrsrc + 1);
					int src_ins_count = *((int*)ptrsrc + 2);

					NaNBoxing* src_consts = (NaNBoxing*)((int*)ptrsrc + 3 + 2 * src_ins_count);

					fixed (void* ptrdst = method.Body.ByteCode)
					{ 
						int dst_count = *((int*)ptrdst + 1);
						int dst_ins_count = *((int*)ptrdst + 2);

						NaNBoxing* dst_consts = (NaNBoxing*)((int*)ptrdst + 3 + 2 * dst_ins_count);

						if (src_count != dst_count)
						{
							throw new InvalidOperationException();
						}

						for (int i = 0; i < src_count && loadfromcache; i++) //scriptDef从cache中读取时，需要重新对一下LD_Class
						{
							if (src_count != method_const.Count)
							{
								throw new InvalidOperationException();
							}

							NaNBoxing nb = method_const[i];
							if (nb.ValueType == NaNBoxing.BoxType.HeapPtr)
							{
								ASMethodBody.PoolHeapPtrKind kind = (ASMethodBody.PoolHeapPtrKind)(nb.HeapPtr >> 24);
								if (kind == ASMethodBody.PoolHeapPtrKind.LD_Class)
								{
									int index = nb.HeapPtr & 0xffffff;
									var cls = list.First(t=>t.Item1 == index);
									//ulong id =   Context.libs.ld_classid[nb.HeapPtr & 0xffffff];
									//ASClass @class = Context.dictTypes[id];

									if (!Context.link_const_class.Exists( c =>c.Type_identifier == cls.Item2.Type_identifier ))
									{
										Context.link_const_class.Add(cls.Item2);
									}

									int nindex = Context.link_const_class.FindIndex( c=>c.Type_identifier == cls.Item2.Type_identifier);
									if ((src_consts + i)->ValueType != NaNBoxing.BoxType.HeapPtr)
									{
										(src_consts + i)->SetUInt((uint)nindex);
									}
									else
									{ 
										
									}
								}

							}


						}

						Buffer.BlockCopy(constpoolbytecode, (3 + 2 * src_ins_count) * 4, method.Body.ByteCode, (3+2 * dst_ins_count)*4 ,src_count * sizeof(NaNBoxing));
					}
				}
			}

			
			unsafe
			{
				try
				{
					iscomputing_initvalue = true;

					ReceiveError error = default;
					Context.GC.ForceGC(ref error);


					var c = method.Container;
					if (c._link_codescope.Kind == CodeScopeKind.Class)
					{
						NaNBoxing nullP = default;
						nullP.SetNull();

						var @class = Context.GC.Heap[((ASClass)c._link_codescope.Container).__instance_index__];
						try
						{
							
							RunMethod(method, nullP, ((ASClass)c._link_codescope.Container).__instance_index__, null, 0, null, Context.StackSlots, ref error, -1);
						}
						finally
						{
							
						}

						if (error.raised)
						{
							throw new EvalConstException();
						}

						var clsinstance = Context.GC.Heap[((ASClass)c._link_codescope.Container).__instance_index__];
						var clspayload = (RtScriptClass)clsinstance;

						int idx = member.DefineAt._link_codescope.Members.IndexOf(member);
						return clspayload.__get_slots_for_gc[idx];

					}
					else if (c._link_codescope.Kind == CodeScopeKind.Script)
					{
						NaNBoxing thisP = default;
						

						var globalinstance = Context.GC.Heap[((ASScript)c._link_codescope.Container).__global_index__];

						thisP.SetHeapPtr(((ASScript)c._link_codescope.Container).__global_index__);

						try
						{
							
							RunMethod(method, thisP, ((ASScript)c._link_codescope.Container).__global_index__, null, 0, null, Context.StackSlots, ref error, -1);
						}
						finally
						{
						
						}
						if (error.raised)
						{
							throw new EvalConstException();
						}
						
						var globalpayload = (RtScriptClass)globalinstance;

						int idx = member.DefineAt._link_codescope.Members.IndexOf(member);
						return globalpayload.__get_slots_for_gc[idx];

					}
					else if (c._link_codescope.Kind == CodeScopeKind.Instance)
					{
						ASClass @type = c._link_codescope.TypeLayout.ASType;
						if (@type.Instance.IsInterface)
						{
							throw new InvalidOperationException();
						}

						int cache_idx = computemember_cacheinstance.FindIndex(p => Context.GC.Heap[p].Kind == RtHeapTypeKind.INSTANCE &&
							Context.GC.Heap[p].Type == @type.Instance
						);

						//构造实例
						RtHeapBase instance;
						int instancePtr;
						if (cache_idx > -1)
						{
							instancePtr = computemember_cacheinstance[cache_idx];
							instance = Context.GC.Heap[instancePtr];
						}
						else
						{
							if (@type.Type_identifier == (ulong)TypeKind.Array)
							{
								throw new InvalidOperationException();
								//instancePtr = Context.GC.AllocArray(out instance, RtPayloadArray.ArrayStoreMode.normal);
							}
							else
							{
								instancePtr = Context.GC.AllocInstance(@type.Instance, out instance);
							}
							if (instancePtr == 0)
							{
								throw new LoaderException("out of memory");
							}

							computemember_cacheinstance.Add(instancePtr);

							Context.GC.Root.Add(instance);

						}


						try
						{
							Context.StackPosition++;
							NaNBoxing thisP = default;
							thisP.SetHeapPtr(instancePtr);
							RunMethod(method, thisP, instancePtr, @type.Instance, 0, null, Context.StackSlots, ref error, -1);
							if (error.raised)
							{
								throw new EvalConstException();
							}

							var payload = (RtInstance)instance;

							int idx = member.DefineAt._link_codescope.Members.IndexOf(member);

							payload.isCompiling = false;
							var v = payload.ReadSlot((ushort)idx, c._link_codescope, this);
							payload.isCompiling = true;

							return v;
						}
						finally
						{
							Context.StackPosition--;
						}
					}

					else if (c._link_codescope.Kind == CodeScopeKind.Method)
					{
						ASMethodBody.MethodBodyInfo info = new ASMethodBody.MethodBodyInfo();
						method.Body.GetInfo(ref info);


						var scope = c._link_codescope;

						int methodscopeptr;

						if (computermember_cachemethodscope.ContainsKey(scope.Container)) //闭包肯定排序在外面的函数后面。所以这里必然存在
						{
							methodscopeptr = computermember_cachemethodscope[scope.Container];
						}
						else
						{
							methodscopeptr = Context.GC.AllocMethodScope(new NaNBoxing[scope.Members.Count+1], 0, scope, true);
							if (methodscopeptr == 0)
							{
								throw new LoaderException("out of memory");
							}

							Context.GC.Root.Add(Context.GC.Heap[methodscopeptr]);
							computermember_cachemethodscope.Add(scope.Container, methodscopeptr);
						}


						int run_methodscope = methodscopeptr;

						bool is_closure = false;
						bool hassetparent = false;
						scope = scope.Parent;

						List< Tuple< RtMethodScope,int>> link = new List<Tuple<RtMethodScope, int>>();
						link.Add( new Tuple<RtMethodScope, int>( (RtMethodScope)Context.GC.Heap[methodscopeptr],methodscopeptr));

						while (scope.Kind == CodeScopeKind.Method)
						{
							is_closure = true;
							var pre_scopeinstance = Context.GC.Heap[methodscopeptr];

							if (!computermember_cachemethodscope.ContainsKey(scope.Container))
							{
								hassetparent = true;
								

								//说明这个父scope没有初始值计算---但是可能会去参数求值！
								methodscopeptr = Context.GC.AllocMethodScope(new NaNBoxing[scope.Members.Count+1], 0, scope, true);
								if (methodscopeptr == 0)
								{
									throw new LoaderException("out of memory");
								}

								Context.GC.Heap[methodscopeptr].Type = scope.Container;
								Context.GC.Root.Add(Context.GC.Heap[methodscopeptr]);
								computermember_cachemethodscope.Add(scope.Container, methodscopeptr);
								((RtMethodScope)pre_scopeinstance).ParentPtr = methodscopeptr;
							}
							else
							{
								hassetparent = true;
								
								methodscopeptr = computermember_cachemethodscope[scope.Container];
								((RtMethodScope)pre_scopeinstance).ParentPtr = methodscopeptr;
							}
							link.Add( new Tuple<RtMethodScope, int>( (RtMethodScope)Context.GC.Heap[methodscopeptr], methodscopeptr));
							
							scope = scope.Parent;
						}

						for (int i = 0; i < link.Count -1; i++)
						{
							link[i].Item1.ParentPtr = link[i + 1].Item2;
						}
						link.Reverse();

						if (scope.Kind == CodeScopeKind.Script)
						{
							if (link[0].Item1.ParentPtr == 0)
							{
								link[0].Item1.ParentPtr = ((ASScript)scope.Container).__global_index__;
							}

							var scopeinstance = Context.GC.Heap[run_methodscope];
							if (!hassetparent)
							{
								((RtMethodScope)scopeinstance).ParentPtr = ((ASScript)scope.Container).__global_index__;
							}
							scopeinstance.Type = method.Body;

							NaNBoxing thisP = default;
							

							var global = Context.GC.Heap[((ASScript)scope.Container).__global_index__];
							thisP.SetHeapPtr(((ASScript)scope.Container).__global_index__);

							if (info.useSlots > Context.StackSlots.Length)
							{
								throw new EvalConstOutOfStackException(method); //槽溢出了，无法计算
							}

							((RtMethodScope)scopeinstance).SetSlot(thisP, (ushort)(((RtMethodScope)scopeinstance).SlotCount - 1));

							Span<NaNBoxing> slots = Context.StackSlots.AsSpan(Context.StackPosition, info.useSlots);
							slots.Clear(); //栈清空 -- 防止GC时错误访问
							int P_PC;

							try
							{
								if (is_closure)
								{
									//iscomputintg_closure_initmember = member;
								}
								//((RtPayloadScriptClass)global).computing_member = member;
								Execute(ref info, scopeinstance, run_methodscope, is_closure ? scopeinstance.Type : null, slots, Context.StackPosition, out P_PC, ref error, -1 , Context.StackPosition-1,null);
							}
							finally
							{
								//((RtPayloadScriptClass)global).computing_member = null;
								if (is_closure)
								{
									//iscomputintg_closure_initmember = null;
								}
							}

							if (error.raised)
							{
								throw new EvalConstException();
							}


							int idx = member.DefineAt._link_codescope.Members.IndexOf(member);
							return ((RtMethodScope)scopeinstance).__get_slots_for_gc[idx];
						}
						else if (scope.Kind == CodeScopeKind.Class)
						{
							if (link[0].Item1.ParentPtr == 0)
							{
								link[0].Item1.ParentPtr = ((ASClass)scope.Container).__instance_index__;
							}

							var scopeinstance = Context.GC.Heap[run_methodscope];
							if (!hassetparent)
							{
								((RtMethodScope)scopeinstance).ParentPtr = ((ASClass)scope.Container).__instance_index__;								
							}
							scopeinstance.Type = method.Body;

							NaNBoxing thisP = default;
							thisP.SetNull();

							var @class = Context.GC.Heap[((ASClass)scope.Container).__instance_index__];

							if (info.useSlots > Context.StackSlots.Length)
							{
								throw new EvalConstOutOfStackException(method); //槽溢出了，无法计算
							}

							((RtMethodScope)scopeinstance).SetSlot(thisP, (ushort)(((RtMethodScope)scopeinstance).SlotCount - 1));

							Span<NaNBoxing> slots = Context.StackSlots.AsSpan(Context.StackPosition, info.useSlots);
							slots.Clear(); //栈清空 -- 防止GC时错误访问
							int P_PC;

							try
							{
								if (is_closure)
								{
									//iscomputintg_closure_initmember = member;
								}
								//((RtPayloadScriptClass)@class).computing_member = member;
								Execute(ref info,scopeinstance, run_methodscope, is_closure ? scopeinstance.Type : null, slots, Context.StackPosition, out P_PC, ref error, -1, Context.StackPosition - 1,null);
							}
							finally
							{
								//((RtPayloadScriptClass)@class).computing_member = null;
								if (is_closure)
								{
									//iscomputintg_closure_initmember = null;
								}
							}

							if (error.raised)
							{
								throw new EvalConstException();
							}

							int idx = member.DefineAt._link_codescope.Members.IndexOf(member);
							return ((RtMethodScope)scopeinstance).__get_slots_for_gc[idx];

						}
						else if (scope.Kind == CodeScopeKind.Instance)
						{

							int cache_idx = computemember_cacheinstance.FindIndex(p => Context.GC.Heap[p].Kind == RtHeapTypeKind.INSTANCE &&
							Context.GC.Heap[p].Type == scope.TypeLayout.ASType.Instance);

							if (cache_idx < 0) // 基类定义在其他文件时，有可能....
							{
								//构造实例
								RtHeapBase instance;
								int instancePtr;
								if (cache_idx > -1)
								{
									instancePtr = computemember_cacheinstance[cache_idx];
									instance = Context.GC.Heap[instancePtr];
								}
								else
								{
									if (scope.TypeLayout.ASType.Type_identifier == (ulong)TypeKind.Array)
									{
										throw new InvalidOperationException();
										//instancePtr = Context.GC.AllocArray(out instance, RtPayloadArray.ArrayStoreMode.normal);
									}
									else
									{
										instancePtr = Context.GC.AllocInstance(scope.TypeLayout.ASType.Instance, out instance);
									}
									if (instancePtr == 0)
									{
										throw new LoaderException("out of memory");
									}

									cache_idx = computemember_cacheinstance.Count;
									computemember_cacheinstance.Add(instancePtr);

									Context.GC.Root.Add(instance);

								}
							}

							if (link[0].Item1.ParentPtr == 0)
							{
								link[0].Item1.ParentPtr = cache_idx ;
							}


							//构造实例
							RtHeapBase this_instance;
							int this_instancePtr;

							this_instancePtr = computemember_cacheinstance[cache_idx];
							this_instance = Context.GC.Heap[this_instancePtr];

							((RtMethodScope)Context.GC.Heap[methodscopeptr]).ParentPtr = this_instancePtr;
							
							var scopeinstance = Context.GC.Heap[run_methodscope];
							scopeinstance.Type = method.Body;

							NaNBoxing thisP = default;
							if (!hassetparent)
							{
								thisP.SetHeapPtr(this_instancePtr);
							}
							else
							{
								thisP.setFault();
							}

							((RtMethodScope)scopeinstance).SetSlot(thisP,(ushort)( ((RtMethodScope)scopeinstance).SlotCount - 1));

							if (info.useSlots > Context.StackSlots.Length)
							{
								throw new EvalConstOutOfStackException(method); //槽溢出了，无法计算
							}

							Span<NaNBoxing> slots = Context.StackSlots.AsSpan(Context.StackPosition, info.useSlots);
							slots.Clear(); //栈清空 -- 防止GC时错误访问
							int P_PC;

							try
							{
								if (is_closure)
								{
									//iscomputintg_closure_initmember = member;
								}
								Execute(ref info,scopeinstance, run_methodscope, 
									is_closure ? scopeinstance.Type : scope.TypeLayout.ASType.Instance
									, slots, Context.StackPosition, out P_PC, ref error, -1, Context.StackPosition - 1,null);
							}
							finally
							{
								if (is_closure)
								{
									//iscomputintg_closure_initmember = null;
								}
							}

							
							if (error.raised)
							{
								throw new EvalConstException();
							}

							int idx = member.DefineAt._link_codescope.Members.IndexOf(member);
							return ((RtMethodScope)scopeinstance).__get_slots_for_gc[idx];

						}
						else
						{
							throw new InvalidOperationException();
						}
					}
					else
					{ 
						throw new InvalidOperationException();
					}

				}
				finally
				{
					iscomputing_initvalue = false;
				}
				

			}
		}



		//private bool flag_pass;
		private void ComputeConstExprOnRunMethod(ASMethod method)
		{
			//if (flag_pass)
			//{
			//	if (method.IsConstructor)
			//	{
			//		return;
			//	}
			//}

			if (method.Body._link_codescope.Kind == (CodeScopeKind)255)
			{
				return;
			}

			if (method.Container._link_codescope.Kind == CodeScopeKind.Script)
			{
				if (((ASScript)method.Container).Initializer == method)
				{
					return;
				}
			}

			if (method.Container._link_codescope.Kind == CodeScopeKind.Class)
			{
				if (((ASClass)method.Container).Constructor == method)
				{
					return;
				}
			}

			throw new EvalConstException();
		}

		public NaNBoxing ComputeConstExpr(ASMethod method, SWCFile testswc, int compute_result_index)
		{
			if (computemember_cacheinstance == null)
			{
				computemember_cacheinstance = new List<int>();
			}
			if (computermember_cachemethodscope == null)
			{
				computermember_cachemethodscope = new Dictionary<ASContainer, int>();
			}
			linkMethod(method,testswc);
			unsafe
			{
				Context.StackPosition = 0;

				try
				{
					iscomputing_initvalue = true;

					ReceiveError error = default;
					Context.GC.ForceGC(ref error);

					var c = method.Container;

					
					
					ASMethodBody.MethodBodyInfo info = new ASMethodBody.MethodBodyInfo();
					method.Body.GetInfo(ref info);


					var scope = c._link_codescope;

					int methodscopeptr;

					if (computermember_cachemethodscope.ContainsKey(scope.Container)) //闭包肯定排序在外面的函数后面。所以这里必然存在
					{
						methodscopeptr = computermember_cachemethodscope[scope.Container];
					}
					else
					{
						methodscopeptr = Context.GC.AllocMethodScope(new NaNBoxing[scope.Members.Count+1], 0, scope, true);
						if (methodscopeptr == 0)
						{
							throw new LoaderException("out of memory");
						}

						Context.GC.Root.Add(Context.GC.Heap[methodscopeptr]);
						computermember_cachemethodscope.Add(scope.Container, methodscopeptr);
					}


					int run_methodscope = methodscopeptr;

					bool is_closure = false;
					bool hassetparent = false;
					scope = scope.Parent;
					while (scope.Kind == CodeScopeKind.Method)
					{
						is_closure = true;
						var pre_scopeinstance = Context.GC.Heap[methodscopeptr];

						if (!computermember_cachemethodscope.ContainsKey(scope.Container))
						{
							//说明这个父methodscope没有初始值计算
							hassetparent = true;


							//说明这个父scope没有初始值计算---但是可能会去参数求值！
							methodscopeptr = Context.GC.AllocMethodScope(new NaNBoxing[scope.Members.Count+1], 0, scope, true);
							if (methodscopeptr == 0)
							{
								throw new LoaderException("out of memory");
							}

							Context.GC.Heap[methodscopeptr].Type = scope.Container;
							Context.GC.Root.Add(Context.GC.Heap[methodscopeptr]);
							computermember_cachemethodscope.Add(scope.Container, methodscopeptr);
							((RtMethodScope)pre_scopeinstance).ParentPtr = methodscopeptr;
						}
						else
						{
							hassetparent = true;
							
							methodscopeptr = computermember_cachemethodscope[scope.Container];
							((RtMethodScope)pre_scopeinstance).ParentPtr = methodscopeptr;
						}
						scope = scope.Parent;
					}

					if (scope.Kind == CodeScopeKind.Script)
					{
						var scopeinstance = Context.GC.Heap[run_methodscope];
						if (!hassetparent)
						{
							((RtMethodScope)scopeinstance).ParentPtr = ((ASScript)scope.Container).__global_index__;
						}
						scopeinstance.Type = method.Body;

						NaNBoxing thisP = default;
						thisP.SetNull();

						((RtMethodScope)scopeinstance).SetSlot(thisP, (ushort)(((RtMethodScope)scopeinstance).SlotCount - 1));


						var global = Context.GC.Heap[((ASScript)scope.Container).__global_index__];

						if (info.useSlots > Context.StackSlots.Length)
						{
							throw new EvalConstOutOfStackException(method); //槽溢出了，无法计算
						}

						Span<NaNBoxing> slots = Context.StackSlots.AsSpan(Context.StackPosition, info.useSlots);
						slots.Clear(); //栈清空 -- 防止GC时错误访问
						int P_PC;

						
						Execute(ref info, scopeinstance, run_methodscope, is_closure ? scopeinstance.Type : null, slots, Context.StackPosition, out P_PC, ref error, -1, Context.StackPosition - 1,null);
						
						if (error.raised)
						{
							throw new EvalConstException();
						}
						return slots[compute_result_index];
					}
					else if (scope.Kind == CodeScopeKind.Class)
					{
						var scopeinstance = Context.GC.Heap[run_methodscope];
						if (!hassetparent)
						{
							((RtMethodScope)scopeinstance).ParentPtr = ((ASClass)scope.Container).__instance_index__;
						}
						scopeinstance.Type = method.Body;

						NaNBoxing thisP = default;
						thisP.SetNull();

						((RtMethodScope)scopeinstance).SetSlot(thisP, (ushort)(((RtMethodScope)scopeinstance).SlotCount - 1));

						var @class = Context.GC.Heap[((ASClass)scope.Container).__instance_index__];

						if (info.useSlots > Context.StackSlots.Length)
						{
							throw new EvalConstOutOfStackException(method); //槽溢出了，无法计算
						}

						Span<NaNBoxing> slots = Context.StackSlots.AsSpan(Context.StackPosition, info.useSlots);
						slots.Clear(); //栈清空 -- 防止GC时错误访问
						int P_PC;

						
						Execute(ref info, scopeinstance, run_methodscope, is_closure ? scopeinstance.Type : null, slots, Context.StackPosition, out P_PC, ref error, -1, Context.StackPosition - 1,null);
						

						if (error.raised)
						{
							throw new EvalConstException();
						}
						return slots[compute_result_index];
					}
					else if (scope.Kind == CodeScopeKind.Instance)
					{
						int this_instancePtr;
						if (((ASInstance)scope.Container).IsInterface)
						{
							this_instancePtr = 0;
						}
						else
						{
							int cache_idx = computemember_cacheinstance.FindIndex(p => Context.GC.Heap[p].Kind == RtHeapTypeKind.INSTANCE &&
							Context.GC.Heap[p].Type == scope.TypeLayout.ASType.Instance);

							if (cache_idx < 0) // 基类定义在其他文件时，有可能....
							{
								//构造实例
								RtHeapBase instance;
								int instancePtr;
								if (cache_idx > -1)
								{
									instancePtr = computemember_cacheinstance[cache_idx];
									instance = Context.GC.Heap[instancePtr];
								}
								else
								{
									if (scope.TypeLayout.ASType.Type_identifier == (ulong)TypeKind.Array)
									{
										//throw new InvalidOperationException();
										instancePtr = Context.GC.AllocArray(out instance, RtArray.ArrayStoreMode.normal);
									}
									else
									{
										instancePtr = Context.GC.AllocInstance(scope.TypeLayout.ASType.Instance, out instance);
									}
									if (instancePtr == 0)
									{
										throw new LoaderException("out of memory");
									}

									cache_idx = computemember_cacheinstance.Count;
									computemember_cacheinstance.Add(instancePtr);

									Context.GC.Root.Add(instance);

								}
							}

							//构造实例
							RtHeapBase this_instance;
							
							this_instancePtr = computemember_cacheinstance[cache_idx];
							this_instance = Context.GC.Heap[this_instancePtr];

							((RtMethodScope)Context.GC.Heap[methodscopeptr]).ParentPtr = this_instancePtr;
						}

						var scopeinstance = Context.GC.Heap[run_methodscope];
						scopeinstance.Type = method.Body;

						NaNBoxing thisP = default;
						if (!hassetparent && this_instancePtr !=0)
						{
							thisP.SetHeapPtr(this_instancePtr);
						}
						else
						{
							thisP.SetNull();
						}

						if (info.useSlots > Context.StackSlots.Length)
						{
							throw new EvalConstOutOfStackException(method); //槽溢出了，无法计算
						}

						((RtMethodScope)scopeinstance).SetSlot(thisP, (ushort)(((RtMethodScope)scopeinstance).SlotCount - 1));

						Span<NaNBoxing> slots = Context.StackSlots.AsSpan(Context.StackPosition, info.useSlots);
						slots.Clear(); //栈清空 -- 防止GC时错误访问
						int P_PC;

						
						Execute(ref info, scopeinstance, run_methodscope, 
							is_closure ? scopeinstance.Type : scope.Container
							//scope.Container
							, slots, Context.StackPosition, out P_PC, ref error, -1, Context.StackPosition - 1,null);
						


						if (error.raised)
						{
							throw new EvalConstException();
						}

						return slots[compute_result_index];

					}
					else
					{
						throw new InvalidOperationException();
					}
					
					

				}
				finally
				{
					iscomputing_initvalue = false;
					
				}


			}




		}

#endif
	}
}
