using juicescript.ABC;
using juicescript.ABC.Locaters;
using juicescript.runtime.buildin;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using static juicescript.NaNBoxing;
using static juicescript.runtime.Player;

namespace juicescript.runtime
{
#if FORCOMPILER
	internal partial class Player
#else
	public partial class Player
#endif
	{

		public static string GetMethodKey(ASMethod method)
		{
			if (method.__is_vector_method)
			{
				var vecSuffix = (method.Trait?.Kind == TraitKind.Getter) ? "get#" :
								(method.Trait?.Kind == TraitKind.Setter) ? "set#" : "";
				return $"__AS3__.vec$Vector@{vecSuffix}{method.Name}";
			}

			var prefix = method.Trait?.IsStatic == true ? "$" : "";
			var ns = method.Container?.QName?.Namespace?.Name ?? "closure";
			var containerName = method.Container?.QName?.Name ?? method.Name;
			var bodyNs = method.Body?.QName == null ? "@" :
						 method.Body?.QName?.Namespace?.ToDebugNameSpaceString() ?? "@";
			var methodSuffix = (method.Trait?.Kind == TraitKind.Getter) ? "get#" :
							   (method.Trait?.Kind == TraitKind.Setter) ? "set#" : "";

			return $"{prefix}{ns}.{containerName}${bodyNs}::{methodSuffix}{method.Name}";
		}

		internal void SetNativeDelegate(ASMethod method, ref ReceiveError error)
		{
			if (method.nativefunction_delegate == null)
			{
				string key = GetMethodKey(method);

				var m = NativeFunctionRegistry.GetFunction(key);
				if (m != null)
				{
					method.nativefunction_delegate = (NativeFun)Delegate.CreateDelegate(typeof(NativeFun), m);
				}
				else
				{
					RaiseIllegaloperationError(ref error, key);

				}

			}
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void CleanRefThisSlotFlag(RtHeapBase _this)
		{
			switch (_this.Kind)
			{
				case RtHeapTypeKind.INSTANCE:
					break;
				case RtHeapTypeKind.ARRAY:
					((RtArray)_this).thisslot_ref_state = 0;
					break;
				case RtHeapTypeKind.VECTOR:
					break;
				case RtHeapTypeKind.CLOSURE:
					break;
				default:
					break;
			}
		}

		/// <summary>
		/// 特别注意在执行函数前，所有未保存的堆对象都需要保存，避免在接下来可能的GC中被意外回收。
		/// </summary>
		/// <param name="method"></param>
		/// <param name="thisPtr"></param>
		/// <param name="scope_ptr"></param>
		/// <param name="scopeType"></param>
		/// <param name="args"></param>
		/// <param name="argementPtr"></param>
		/// <param name="slot"></param>
		/// <param name="error"></param>
		/// <param name="returnSlotIndex"></param>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		/// <exception cref="NotImplementedException"></exception>
		//[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private unsafe NaNBoxing RunMethod_FullCheck(ASMethod method, NaNBoxing thisPtr, int scope_ptr,
			//ASContainer scopeType, 
			ushort args, byte* argementPtr,
			Span<NaNBoxing> slot, ref ReceiveError error, int returnSlotIndex, int callee_closure_ptr, bool skipcheckargscount)
		{






			ASMethodBody.MethodBodyInfo info = new ASMethodBody.MethodBodyInfo();
			method.Body.GetInfo(ref info);

			do
			{
				

				int para_argcount = 0;

				if (method.IsAnonymous || skipcheckargscount)//method.Trait == null && method.Parameters.Count == 0)
				{
					//不检查参数个数
				}
				else
				{
					//检查参数个数
					if (args > method.Parameters.Count)
					{
						if ((method.Parameters.Count == 0 || !((method.Flags & MethodFlags.NeedRest) != 0)) && !((method.Flags & MethodFlags.NeedArguments) != 0))
						{
							//throw new NotImplementedException("参数过多");

							int expected = method.Parameters.Count;
							do
							{
								--expected;
							} while (expected >= 0 && (method.Parameters[expected].IsOptional || method.Parameters[expected].IsRest));

							RaiseArgumentErrorCountMisMatch(ref error, method, expected + 1, args);

							goto lbl_handle_arg_err;

						}
					}
					else if (args < method.Parameters.Count)
					{
						if (!method.Parameters[args].IsOptional && !method.Parameters[args].IsRest)
						{
							int expected = method.Parameters.Count;
							do
							{
								--expected;
							} while (expected >= 0 && method.Parameters[expected].IsOptional);


							RaiseArgumentErrorCountMisMatch(ref error, method, expected + 1, args);

							goto lbl_handle_arg_err;
						}
					}
				}


				//先将 rest 部分元素序列放入
				if (
					((method.Flags & (MethodFlags.NeedRest)) != 0)
					)
				{
					int restCount = args - (method.Parameters.Count - 1);
					if (restCount > 0)
					{
						para_argcount = restCount;

						if (Context.StackPosition + restCount >= Context.STACK_LENGTH)
						{
							break;
						}

						byte* P = argementPtr + sizeof(StackLocater) * (method.Parameters.Count - 1);

						for (int i = 0; i < restCount; i++)
						{
							StackLocater argLocater;
							LoadStackLocater(&argLocater, &P);

							//考虑如下代码的存在，所以我们只能在存入数组时保存到实体
							//class A{}
							//class B{}
							//(function ():void 
							//{
							//	var b = new A();
							//	function k(...r):void 
							//	{
							//		b = new B();
							//		trace(r[0]);
							//	}
							//	k(b);
							//})();
							NaNBoxing box = slot[argLocater.index];
							if (!((method.Flags & (MethodFlags.Native)) != 0))
							{
								if (box.ValueType == NaNBoxing.BoxType.HeapPtr)
								{

									if (box.IsStruct())//box.HeapKind == (byte)RtHeapTypeKind.INSTANCE && ((ASInstance)Context.GC.Heap[box.HeapPtr].Type).Flags.HasFlag(ClassFlags.Struct))
									{
										var v = Context.GC.Heap[box.HeapPtr];
										var struct_ptr = InitCacheInstance(v.Type._link_codescope.TypeLayout.ASType, Context.StackPosition + i, false, out RtInstance struct_ins);
										//var struct_ins = Context.GC.Heap[struct_ptr];

										((RtInstance)struct_ins).CopyFrom(v, this, v.Type._link_codescope.TypeLayout.Size);
										box.SetHeapPtr(struct_ptr, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.FLAG_STRUCT);
									}
									else
									{
										box = GetSaveValue(box, ref error);
										if (error.raised)
										{
											goto lbl_handle_arg_err;
										}
									}
								}
							}
							Context.StackSlots[Context.StackPosition + i] = box;

						}
					}
				}
				else if (((method.Flags & (MethodFlags.NeedArguments)) != 0)) //构造argements数组
				{
					para_argcount = args + 2;

					if (Context.StackPosition + para_argcount >= Context.STACK_LENGTH)
					{
						break;
					}

					//Context.GC.CheckGC(ref error);

					//改在实际传参时赋值进去
					//由于可能 method.Parameters.Count小于 args的情况，所以这里先把超出的部分填入
					/*比如这种代码
					 * var a;
						var c;
						(function ():void   
						{
							a = arguments;
							c = arguments.callee;
						})(1,2,3);

						c(6,7);
					*/
					byte* P = argementPtr + method.Parameters.Count * sizeof(StackLocater);
					for (int i = method.Parameters.Count; i < args; i++)
					{
						StackLocater argLocater;
						LoadStackLocater(&argLocater, &(P));

						NaNBoxing box = slot[argLocater.index];
						if (box.ValueType == NaNBoxing.BoxType.HeapPtr)
						{
							if (box.IsStruct())
							{
								var v = Context.GC.Heap[box.HeapPtr];
								var struct_ptr = InitCacheInstance(v.Type._link_codescope.TypeLayout.ASType, Context.StackPosition + i, false, out RtInstance struct_ins);
								//var struct_ins = Context.GC.Heap[struct_ptr];

								((RtInstance)struct_ins).CopyFrom(v, this, v.Type._link_codescope.TypeLayout.Size);
								box.SetHeapPtr(struct_ptr, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.FLAG_STRUCT);
							}
							else
							{
								box = GetSaveValue(box, ref error);
								if (error.raised)
								{
									goto lbl_handle_arg_err;
								}
							}
						}
						Context.StackSlots[Context.StackPosition + i] = box;
					}


					Memory<NaNBoxing> arguments = Context.StackSlots.AsMemory(Context.StackPosition, args);

					int argumentsPtr = Context.M_RestArrayPtr + Context.BackTraceIndex;
					RtHeapBase arg_rest = Context.GC.Heap[argumentsPtr];
#if DEBUG
					if (((RtArray)arg_rest).StoreMode != RtArray.ArrayStoreMode.cache_on_stack)
					{
						throw new InvalidOperationException();
					}
#endif
					arg_rest.Type = Context.ARRAY.Instance;
					//((RtArray)arg_rest).array_len = (uint)arguments.Length;
					//((RtArray)arg_rest).stack_store = arguments;
					//((RtArray)arg_rest).stack_store_startindex = Context.StackPosition;
					//((RtArray)arg_rest).HEAPINSTANCE_PTR = 0;
					//((RtArray)arg_rest).Set_PROPERTY_PTR(0, this);
					((RtArray)arg_rest).SetStoreRest(arguments, Context.StackPosition);
					((RtArray)arg_rest).SetIsArguments(true);

					if (callee_closure_ptr != 0)
					{

						Context.StackSlots[Context.StackPosition + args + 1].SetHeapPtr(callee_closure_ptr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);
					}
					else
					{

						int calleePtr = Context.M_ClosurePtr + Context.StackPosition + args + 1;
#if DEBUG
						if (Context.GC.Heap[calleePtr].Kind != RtHeapTypeKind.CLOSURE)
						{
							throw new InvalidOperationException();
						}
#endif


						Context.GC.Heap[calleePtr].Type = method.Body;
						RtClosure payloadClosure = (RtClosure)Context.GC.Heap[calleePtr];
						payloadClosure.This = thisPtr;
						payloadClosure.ScopePtr = scope_ptr;
						//payloadClosure.ScopeType = null;
						payloadClosure._ref_as_type = null;

						Context.StackSlots[Context.StackPosition + args + 1].SetHeapPtr(calleePtr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);
					}
					//int calleePtr = Context.GC.AllocClosure(method);
					//if (calleePtr == 0)
					//{
					//	RaiseOutOfMemory(ref error);
					//	goto lbl_handle_arg_err;
					//}

					//RtPayloadClosure payloadClosure = (RtPayloadClosure)Context.GC.Heap[calleePtr];
					//payloadClosure.This = thisPtr;
					//payloadClosure.ScopePtr = scope_ptr;
					//payloadClosure.ScopeType = null; payloadClosure._ref_as_type = null;

					//NaNBoxing callee = new NaNBoxing();
					//callee.SetHeapPtr(calleePtr);

					//CreateDynamic(ref error, arg_rest, CALLEESTR_PTR, callee);
					//if (error.raised)
					//{
					//	goto lbl_handle_arg_err;
					//}

					Context.StackSlots[Context.StackPosition + args].SetHeapPtr(argumentsPtr, (byte)RtHeapTypeKind.ARRAY, (byte)HeapKindFlag.NONE);

				}

				var method_body_linkcodesocpe = method.Body._link_codescope;

				int scopeHoleSlots = method_body_linkcodesocpe.Members.Count
					+ 1 //holdthis
					;

				if (Context.StackPosition + para_argcount + scopeHoleSlots + info.useSlots
					//+ 1 
					>= Context.STACK_LENGTH)
				{
					break;
				}

				Context.StackPosition += para_argcount;

				int backTraceId = Context.BackTraceIndex;

				int mScopeId = backTraceId + Context.M_MethodScopePtr;
				RtHeapBase mScope = Context.GC.Heap[mScopeId];

				mScope.Type = method.Body;
				RtMethodScope m_scopePayload = (RtMethodScope)mScope;
				m_scopePayload.ParentPtr = scope_ptr;
				m_scopePayload.InitSlot(Context.StackSlots, Context.StackPosition, method_body_linkcodesocpe, true);

				m_scopePayload.__sendargcount = args;

				//save this --如果saved_this被赋值，在结束函数调用后需要将this_slot清空，因为this已经结束了
				RtHeapBase saved_this = null;
				{

					if ( thisPtr.ValueType == BoxType.HeapPtr && thisPtr.HeapKind >= (byte)RtHeapTypeKind.INSTANCE) //原this槽位肯定是空的
					{
						ScopeHeapLocater scopeHeapLocater;
						scopeHeapLocater.ScopeIndex = (ushort)method_body_linkcodesocpe.index;
						scopeHeapLocater.MemberIndex = (ushort)(m_scopePayload.SlotCount - 1);

						prepare_savescope_pass(ref thisPtr, m_scopePayload, scopeHeapLocater, default, 0, 0, mScopeId, out saved_this, ref error, true);						
						//PrepareSaveMethodScope(m_scopePayload, scopeHeapLocater, ref thisPtr, null, 0, ref error, true);//C#里 从容器访问结构体This就是直接拷了一份,构造函数会传引用			
						if (error.raised)
						{
							if (saved_this != null) CleanRefThisSlotFlag(saved_this);

							Context.StackPosition -= para_argcount;
							goto lbl_handle_arg_err;
						}
					}

					m_scopePayload.SetSlot(thisPtr, (ushort)(m_scopePayload.SlotCount - 1));



				}



#if FORCOMPILER
				m_scopePayload.isCompiling = false;
				if (IsComputeConstExpr)
				{
					if(method.Flags.HasFlag( MethodFlags.ASYNC))
					{
						throw new EvalConstException();
					}

					m_scopePayload.isCompiling = true;
					goto lbl_arguments_pass;
				}
#endif


				//***传参***

				fixed (byte* bp = ((method.Flags & (MethodFlags.HasOptional)) != 0) ? method.Body.param_defaultvalues : null)
				{
					Span<NaNBoxing> arguments_span = default;

					if ((method.Flags & (MethodFlags.NeedArguments)) != 0)
					{
						int argumentsPtr = Context.M_RestArrayPtr + Context.BackTraceIndex;
						RtHeapBase arg_arguments = Context.GC.Heap[argumentsPtr];
						arguments_span = ((RtArray)arg_arguments).stack_store.Span;
					}

					var pmembers = method_body_linkcodesocpe.Members;
					Span<NaNBoxing> param_slots = Context.StackSlots.AsSpan(Context.StackPosition, method.Parameters.Count);
					//param_slots.Clear(); //防止GC 错误意外访问
					for (ushort i = 0; i < param_slots.Length; i++)
					{
						var p = method.Parameters[i];
						if (p.IsRest)
						{
							Memory<NaNBoxing> rest = Context.StackSlots.AsMemory(
								Context.StackPosition

								- para_argcount, para_argcount);

							int restPtr = Context.M_RestArrayPtr + Context.BackTraceIndex;

							RtHeapBase arg_rest = Context.GC.Heap[restPtr];

#if DEBUG
							if (((RtArray)arg_rest).StoreMode != RtArray.ArrayStoreMode.cache_on_stack)
							{
								throw new InvalidOperationException();
							}
#endif
							arg_rest.Type = Context.ARRAY.Instance;
							//((RtArray)arg_rest).array_len = (uint)rest.Length;
							//((RtArray)arg_rest).stack_store = rest;
							//((RtArray)arg_rest).stack_store_startindex = Context.StackPosition - para_argcount;
							//((RtArray)arg_rest).HEAPINSTANCE_PTR = 0;
							//((RtArray)arg_rest).Set_PROPERTY_PTR(0, this);
							((RtArray)arg_rest).SetStoreRest(rest, Context.StackPosition - para_argcount);

							//((RtArray)arg_rest).SetIsRest(true);


							NaNBoxing box = new NaNBoxing();
							box.SetHeapPtr(restPtr, (byte)RtHeapTypeKind.ARRAY, (byte)HeapKindFlag.NONE);

							m_scopePayload.SetSlot(box, i);

						}
						else
						{

							if (i < args)
							{
								StackLocater argLocater;
								LoadStackLocater(&argLocater, &argementPtr);

								NaNBoxing box = slot[argLocater.index];

								var ptypekind = p.TypeKind;
								var boxtype = box.ValueType;

								if (ptypekind == TypeKind.Any
									|| ((byte)boxtype - 3 == (byte)ptypekind && ptypekind <= TypeKind.Float)
									|| (boxtype == BoxType.LocalString && ptypekind == TypeKind.String))
								{
									param_slots[i] = box;
								}
								else if (ptypekind < TypeKind.Float && ptypekind >= TypeKind.Int && boxtype >= BoxType.Int && boxtype < BoxType.Float)
								{
									ulong mask = (((ulong)ptypekind + 3) << 40) | NaNBoxing.QNAN;
									ulong raw = mask | (box.Raw & 0xffffffff);

									param_slots[i] = new NaNBoxing(raw);
								}
								else
								{
									Context.StackPosition += i;// method.Parameters.Count;
									Context.BackTraceIndex++;
									ConvertValueType(ref error, box, ptypekind, pmembers[i].__rt_type_class__, ref param_slots[i], scope_ptr, thisPtr);
									Context.BackTraceIndex--;
									Context.StackPosition -= i;// method.Parameters.Count;

									if (error.raised)
									{
										if (saved_this != null) CleanRefThisSlotFlag(saved_this);

										Context.StackPosition -= para_argcount;
										goto lbl_handle_arg_err;
									}

								}

								if (true)
								{
									//这里是保存到参数中，也需要预准备保存到方法体。
									box = param_slots[i];
									//if (box.ValueType == NaNBoxing.BoxType.HeapPtr) //仅在是堆对象时，才可能触发维护引用的操作
									if (box.HeapKind >= (byte)RtHeapTypeKind.INSTANCE) //仅在是堆对象时，才可能触发维护引用的操作
									{
										param_slots[i].SetUndefined();

										//if (box.ValueType == NaNBoxing.BoxType.HeapPtr)
										//if(box.HeapKind >= (byte)RtHeapTypeKind.INSTANCE)
										{
											ScopeHeapLocater scopeHeapLocater;
											scopeHeapLocater.ScopeIndex = (ushort)method_body_linkcodesocpe.index;
											scopeHeapLocater.MemberIndex = i;

											PrepareSaveMethodScope(m_scopePayload, scopeHeapLocater, ref box, null, &scope_ptr, ref error, false /*结构体拷贝传递*/);
#if DEBUG
											if (error.raised)
											{
												throw new InvalidOperationException();
											}
#endif
										}
										param_slots[i] = box;
									}
								}

								if (((method.Flags & (MethodFlags.NeedArguments)) != 0))
								{
									//有arguments,只能实例到堆。避免其他意外
									box = param_slots[i];

									if (box.ValueType == NaNBoxing.BoxType.HeapPtr)
									{

										if (box.IsStruct())
										{
											var v = Context.GC.Heap[box.HeapPtr];
											var struct_ptr = InitCacheInstance(v.Type._link_codescope.TypeLayout.ASType,
												Context.StackPosition - para_argcount + i //实例到arguments数组
												, false, out RtInstance struct_ins
												);
											//var struct_ins = Context.GC.Heap[struct_ptr];

											((RtInstance)struct_ins).CopyFrom(v, this, v.Type._link_codescope.TypeLayout.Size);
											box.SetHeapPtr(struct_ptr, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.FLAG_STRUCT);
										}
										else
										{
											box = GetSaveValue(box, ref error);
											if (error.raised)
											{
												Context.StackPosition -= para_argcount;
												goto lbl_handle_arg_err;
											}
											param_slots[i] = box;
										}
									}

									arguments_span[i] = box;
								}

							}
							else
							{
								if (!p.IsOptional)
								{
									if (method.IsAnonymous || skipcheckargscount)
									{
										param_slots[i].SetUndefined();
										continue;
									}
#if DEBUG
									else
									{
										throw new InvalidOperationException();
									}
#endif
								}

								Span<NaNBoxing> constants = new Span<NaNBoxing>(bp + 3 * sizeof(int) + 2 * sizeof(int) * 0, *((int*)bp + 1));
								NaNBoxing value = constants[p.ValueExprIndex];


								ConvertValueType(ref error, value, p.TypeKind, pmembers[i].__rt_type_class__, ref param_slots[i]);

								Debug.Assert(!error.raised);

								//if (error.raised)
								//{
								//	Context.StackPosition -= para_argcount;
								//	goto lbl_handle_arg_err;
								//}

								//throw new NotImplementedException("有默认值的参数");

							}
						}
					}
				}


#if FORCOMPILER
			lbl_arguments_pass:
#endif


#if PROFILEPLAYER
				InstructionProfiler.Profile_MethodStart(method);
#endif

				if (returnSlotIndex > -1)
				{
					Context.StackSlots[returnSlotIndex].setDefault(method.ReturnTypeKind);
				}

				
				if (((method.Flags & (MethodFlags.Generator)) != 0))
				{
					if (Context.StackPosition + 2
					//+ 1 
					>= Context.STACK_LENGTH)
					{
						if (saved_this != null) CleanRefThisSlotFlag(saved_this);
						Context.StackPosition -= para_argcount;
						break;
					}


					NaNBoxing g_scope = default;
					g_scope.SetHeapPtr(mScopeId, (byte)RtHeapTypeKind.MethodScope, (byte)HeapKindFlag.NONE);
					g_scope = GetSaveValue(g_scope, ref error);
					if (error.raised)
					{
						if (saved_this != null) CleanRefThisSlotFlag(saved_this);
						Context.StackPosition -= para_argcount;
						goto lbl_handle_arg_err;
					}

					mScope = Context.GC.Heap[g_scope.HeapPtr];


					Context.StackPosition += 2;
					Context.StackSlots[Context.StackPosition - 2] = g_scope; //.SetHeapPtr(g_scope.HeapPtr); //保存防止被GC

					NaNBoxing _this = GetSaveValue(thisPtr, ref error);
					if (error.raised)
					{
						if (saved_this != null) CleanRefThisSlotFlag(saved_this);
						Context.StackPosition -= 2;
						Context.StackPosition -= para_argcount;
						goto lbl_handle_arg_err;
					}

					Context.StackSlots[Context.StackPosition - 1] = _this; //.SetHeapPtr(_this.HeapPtr); //保存防止被GC

					//构造Generator类,并返回
					ASClass generator = ((ASScript)Context.IITERATOR._link_codescope.Parent.Container).Traits[3].Class;

					Debug.Assert(generator.QName.Name == "generator");

					RtHeapBase gen;
					int generator_ptr = Context.GC.AllocInstance(generator.Instance, out gen);

					if (generator_ptr == 0)
					{
						if (saved_this != null) CleanRefThisSlotFlag(saved_this);
						Context.StackPosition -= 2;
						Context.StackPosition -= para_argcount;
						RaiseOutOfMemory(ref error);
						goto lbl_handle_arg_err;
					}


					GeneratorImpl.GeneratorWapper wapper = new GeneratorImpl.GeneratorWapper();
					if (!((method.Flags & (MethodFlags.NoTry)) != 0))
					{
						wapper.exceptionContext = new ExceptionContext[Context.MAX_TRY_NESTED + 2];
					}
					wapper.exception_ctx_at = 0;

					wapper.generator = g_scope.HeapPtr;
					wapper.state = 0;
					wapper.thisPtr = _this;
					//wapper.scopeType = scopeType;


					((RtInstance)gen).wapperedObject = wapper;

					Context.StackPosition -= 2;
					Context.StackPosition -= para_argcount;


					NaNBoxing result = default;
					result.SetHeapPtr(generator_ptr, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.NONE);

					if (returnSlotIndex > -1)
					{
						Context.StackSlots[returnSlotIndex] = result;
					}


#if PROFILEPLAYER
					InstructionProfiler.Profile_MethodEnd();
#endif

					if (saved_this != null) CleanRefThisSlotFlag(saved_this);
					return result;

				}
				else if (((method.Flags & (MethodFlags.ASYNC)) != 0))
				{
					Debug.Assert(!method.Flags.HasFlag(MethodFlags.Native));
					//native方法只需要返回Promise即可，它里面没有await!

					//if ((method.Flags & MethodFlags.Native) == MethodFlags.Native)
					//{
					//	throw new NotImplementedException();
					//}

					/*
					 function foo():Promise {
						var gen = foo$gen();
						return new Promise(function(resolve, reject) {
							var r = new IteratorResult();

							function step(input) {
								try {
									gen.next(input, r);
								} catch (e) {
									reject(e);
									return;
								}

								if (r.done) {
									resolve(r.value);
								} else {
									Promise.resolve(r.value).then(
										function(v) { step(v); },
										function(e) {
											try { gen.throw(e, r); step(undefined); }
											catch (e2) { reject(e2); }
										}
									);
								}
							}

							step(undefined);
						});
					}

					 */

					if (Context.StackPosition + 4
						+ scopeHoleSlots + info.useSlots
					>= Context.STACK_LENGTH)
					{
						if (saved_this != null) CleanRefThisSlotFlag(saved_this);
						Context.StackPosition -= para_argcount;
						break;
					}

					NaNBoxing g_scope = default;
					g_scope.SetHeapPtr(mScopeId, (byte)RtHeapTypeKind.MethodScope, (byte)HeapKindFlag.NONE);
					g_scope = GetSaveValue(g_scope, ref error);
					if (error.raised)
					{
						if (saved_this != null) CleanRefThisSlotFlag(saved_this);
						Context.StackPosition -= para_argcount;
						goto lbl_handle_arg_err;
					}

					mScope = Context.GC.Heap[g_scope.HeapPtr];

					int basePos = Context.StackPosition;

					Context.StackPosition += 4;
					Context.StackSlots[basePos] = g_scope; //保存防止被GC

					NaNBoxing _this = GetSaveValue(thisPtr, ref error);
					if (error.raised)
					{
						if (saved_this != null) CleanRefThisSlotFlag(saved_this);
						Context.StackPosition = basePos;
						Context.StackPosition -= para_argcount;
						goto lbl_handle_arg_err;
					}

					Context.StackSlots[basePos + 1] = _this; //保存防止被GC

					//构造async::gen
					RtHeapBase gen;
					int generator_ptr = Context.GC.AllocInstance(Context.OBJECT.Instance, out gen);

					if (generator_ptr == 0)
					{
						if (saved_this != null) CleanRefThisSlotFlag(saved_this);
						Context.StackPosition = basePos;
						Context.StackPosition -= para_argcount;
						RaiseOutOfMemory(ref error);
						goto lbl_handle_arg_err;
					}

					PromiseImpl.AsyncGenWapper wapper = new PromiseImpl.AsyncGenWapper();
					if (!((method.Flags & (MethodFlags.NoTry)) != 0))
					{
						wapper.exceptionContext = new ExceptionContext[Context.MAX_TRY_NESTED + 2];
					}
					wapper.exception_ctx_at = 0;

					wapper.async_body = g_scope.HeapPtr;
					wapper.state = 0;
					wapper.thisPtr = _this;
					//wapper.scopeType = scopeType;
					((RtInstance)gen).wapperedObject = wapper;

					//构造promise
					RtHeapBase promise;
					int promise_ptr = Context.GC.AllocInstance(Context.PROMISE.Instance, out promise);
					if (promise_ptr == 0)
					{
						if (saved_this != null) CleanRefThisSlotFlag(saved_this);
						Context.StackPosition = basePos;
						Context.StackPosition -= para_argcount;
						RaiseOutOfMemory(ref error);
						goto lbl_handle_arg_err;
					}

					Context.StackSlots[basePos + 2].SetHeapPtr(promise_ptr, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.NONE); //保存防止被GC

					//创建构造函数闭包
					int template_ctor = Context.M_ClosurePtr + basePos + 3;

					RtClosure ctorClosure = (RtClosure)Context.GC.Heap[template_ctor];
					Context.GC.Heap[template_ctor].Type = Context.MicroTaskQueue.async_template_ctor.Body;
					ctorClosure.This.SetHeapPtr(promise_ptr, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.NONE);
					ctorClosure.ScopePtr = generator_ptr;
					//ctorClosure.ScopeType = null;
					ctorClosure._ref_as_type = Context.PROMISE;
					ctorClosure.methodscopeslot_ref_state = 0; ctorClosure.HEAPINSTANCE_PTR = 0;

					Context.StackSlots[basePos + 3].SetHeapPtr(template_ctor, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);

					Span<NaNBoxing> slots = Context.StackSlots.AsSpan(basePos + 3, 1);

					StackLocater stackLocater = default; stackLocater.index = 0;
					RunMethod(Context.PROMISE.Instance.Constructor, Context.StackSlots[basePos + 2], promise_ptr,
						//Context.PROMISE.Instance,
						1, (byte*)&stackLocater, slots, ref error, -1, 0, true);


					Context.StackPosition = basePos;
					Context.StackPosition -= para_argcount;

					if (error.raised)
					{
						if (saved_this != null) CleanRefThisSlotFlag(saved_this);
						goto lbl_handle_arg_err;
					}



					NaNBoxing result = default;
					if (returnSlotIndex > -1)
					{
						result.SetHeapPtr(promise_ptr, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.NONE);
						Context.StackSlots[returnSlotIndex] = result;
					}

#if PROFILEPLAYER
					InstructionProfiler.Profile_MethodEnd();
#endif
					if (saved_this != null) CleanRefThisSlotFlag(saved_this);
					return result;

					//throw new NotImplementedException();
				}
				else if (info.instructions > 0)
				{
					int calleelastpos = Context.StackPosition;
					Context.StackPosition += scopeHoleSlots;

					int stPos = Context.StackPosition;
					Context.StackPosition += info.useSlots;

					//Context.BackTrace[Context.BackTraceIndex].Method = method;
					Context.BackTraceIndex++; ;

					Span<NaNBoxing> slots = Context.StackSlots.AsSpan(stPos, info.useSlots);
					slots.Clear(); //栈清空 -- 防止GC时错误访问
					int P_PC;
					Execute(ref info, mScope, mScopeId, //scopeType, 
						slots, stPos, out P_PC, ref error, returnSlotIndex, calleelastpos, null);

					Context.BackTraceIndex--;
					//Context.BackTrace[Context.BackTraceIndex].Method = null;



					Context.StackPosition -= info.useSlots;
					Context.StackPosition -= scopeHoleSlots;

					m_scopePayload.ParentPtr = 0;
					mScope.Type = null;

					Context.StackPosition -= para_argcount;

					if (!error.raised)
					{
						if ((method.Flags & MethodFlags.Native) == MethodFlags.Native)
						{
							Context.StackPosition += para_argcount;

							Debug.Assert(method.IsConstructor); // 只有在有类成员初始化代码并且构造函数还是native的时候会触发

							goto run_native;
						}
						else
						{
#if PROFILEPLAYER
							InstructionProfiler.Profile_MethodEnd();
#endif

							if (returnSlotIndex >= 0)
							{
								if (saved_this != null) CleanRefThisSlotFlag(saved_this);
								return Context.StackSlots[returnSlotIndex];
							}
							else
							{
								if (saved_this != null) CleanRefThisSlotFlag(saved_this);
								return default(NaNBoxing);
							}
						}
					}
					else
					{
#if PROFILEPLAYER
						InstructionProfiler.Profile_MethodEnd();
#endif

						if (saved_this != null) CleanRefThisSlotFlag(saved_this);

						//记录当前报错堆栈，看上级调用是否处理这个错误
						Context.errorStack.AddTrace(method, P_PC);

						return new NaNBoxing();
					}

				}
				else if ((method.Flags & MethodFlags.Native) == MethodFlags.Native)
				{
					goto run_native;
				}
				else
				{
#if PROFILEPLAYER
					InstructionProfiler.Profile_MethodEnd();
#endif
					if (saved_this != null) CleanRefThisSlotFlag(saved_this);

					//Context.StackPosition--;
					Context.StackPosition -= para_argcount;
					return new NaNBoxing();
				}


			run_native:

				SetNativeDelegate(method, ref error);
				if (error.raised)
				{					
					goto lbl_native_called;
				}

				//Context.BackTrace[Context.BackTraceIndex].Method = method;
				Context.BackTraceIndex++; ;
				Context.StackPosition += scopeHoleSlots;
				((NativeFun)method.nativefunction_delegate)(Context, method, mScopeId, thisPtr, Context.StackPosition, ref error, returnSlotIndex);
				Context.StackPosition -= scopeHoleSlots;
				Context.BackTraceIndex--;
			//Context.BackTrace[Context.BackTraceIndex].Method = null;


#if PROFILEPLAYER
				InstructionProfiler.Profile_MethodEnd();
#endif



			lbl_native_called:

				if (saved_this != null) CleanRefThisSlotFlag(saved_this);

				m_scopePayload.ParentPtr = 0;
				mScope.Type = null;
				Context.StackPosition -= para_argcount;

				if (!error.raised)
				{
					if (returnSlotIndex >= 0)
					{
						return Context.StackSlots[returnSlotIndex];
					}
					else
					{
						return default(NaNBoxing);
					}
				}
				else
				{
					//记录当前报错堆栈，看上级调用是否处理这个错误
					Context.errorStack.AddTrace(method, 0);
					return new NaNBoxing();
				}

			} while (false);

			//stackoverflow
			Context.GC.CheckGC(ref error);
			RaiseStackOverflow(ref error);

			return new NaNBoxing();

		lbl_handle_arg_err:


			return new NaNBoxing();
			;

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private unsafe void RunMethod_MatchArgs(ASMethod method, NaNBoxing _this_, int scope_ptr, Span<NaNBoxing> stackslots, ushort argsCount, byte* argementsPtr,

			int returnSlotIndex,
			ref ReceiveError error,

			ref NaNBoxing result

			)
		{
			
			
			ASMethodBody.MethodBodyInfo info = new ASMethodBody.MethodBodyInfo();
			method.Body.GetInfo(ref info);

			if (info.instructions == 0 && argsCount == 0 && (method.Flags & MethodFlags.Native) == 0)
			{
				result.setDefault(method.ReturnTypeKind);
				return;
			}

			var method_body_linkcodesocpe = method.Body._link_codescope;

			int scopeHoleSlots = method_body_linkcodesocpe.Members.Count
				+ 1 //holdthis
				;


			if (Context.StackPosition + scopeHoleSlots + info.useSlots
				//+ 1 
				>= Context.STACK_LENGTH)
			{
				RaiseStackOverflow(ref error);
				goto flag_handle_error;
			}



			int backTraceId = Context.BackTraceIndex;

			int mScopeId = backTraceId + Context.M_MethodScopePtr;
			RtHeapBase mScope = Context.GC.Heap[mScopeId];

			mScope.Type = method.Body;
			RtMethodScope m_scopePayload = (RtMethodScope)mScope;
			m_scopePayload.ParentPtr = scope_ptr;
			m_scopePayload.InitSlot(Context.StackSlots, Context.StackPosition, method_body_linkcodesocpe, true);

			m_scopePayload.__sendargcount = argsCount;


			//save this
			RtHeapBase saved_this = null;
			{

				if (_this_.ValueType== BoxType.HeapPtr && _this_.HeapKind >= (byte)RtHeapTypeKind.INSTANCE) //原this槽位肯定是空的
				{
					ScopeHeapLocater scopeHeapLocater;
					scopeHeapLocater.ScopeIndex = (ushort)method_body_linkcodesocpe.index;
					scopeHeapLocater.MemberIndex = (ushort)(m_scopePayload.SlotCount - 1);

					prepare_savescope_pass(ref _this_, m_scopePayload, scopeHeapLocater, default, 0, 0, mScopeId, out saved_this, ref error, true);
					//PrepareSaveMethodScope(m_scopePayload, scopeHeapLocater, ref _this_, null, &mScopeId , ref error, true);//C#里 从容器访问结构体This就是直接拷了一份,构造函数会传引用			
					if (error.raised)
					{
						goto flag_handle_error;
					}
				}

				m_scopePayload.SetSlot(_this_, (ushort)(m_scopePayload.SlotCount - 1));
			}

			var pmembers = method_body_linkcodesocpe.Members;
			Span<NaNBoxing> param_slots = Context.StackSlots.AsSpan(Context.StackPosition, argsCount);
			//param_slots.Clear(); //防止GC 错误意外访问


			for (ushort i = 0; i < argsCount; i++)
			{
				var p = method.Parameters[i];

				//StackLocater argLocater;
				//LoadStackLocater(&argLocater, &argementsPtr);

				//StackLocater argLocater = *(StackLocater*)argementsPtr; argementsPtr += 4;
				int arg_index = *(int*)argementsPtr; argementsPtr += 4;


				NaNBoxing box = stackslots[arg_index];

				var ptypekind = p.TypeKind;
				var boxtype = box.ValueType;

				if (ptypekind == TypeKind.Any
					|| ((byte)boxtype - 3 == (byte)ptypekind && ptypekind <= TypeKind.Float)
					|| (boxtype == BoxType.LocalString && ptypekind == TypeKind.String))
				{
					param_slots[i] = box;
				}
				else if (ptypekind < TypeKind.Float && ptypekind >= TypeKind.Int && boxtype >= BoxType.Int && boxtype < BoxType.Float)
				{
					ulong mask = (((ulong)ptypekind + 3) << 40) | NaNBoxing.QNAN;
					ulong raw = mask | (box.Raw & 0xffffffff);

					param_slots[i] = new NaNBoxing(raw);
				}
				else
				{
					Context.StackPosition += i;// method.Parameters.Count;
					Context.BackTraceIndex++;
					ConvertValueType(ref error, box, ptypekind, pmembers[i].__rt_type_class__, ref param_slots[i], scope_ptr, _this_);
					Context.BackTraceIndex--;
					Context.StackPosition -= i;// method.Parameters.Count;

					if (error.raised)
					{
						if (saved_this != null) CleanRefThisSlotFlag(saved_this);
						goto flag_handle_error;
					}

				}


				//这里是保存到参数中，也需要预准备保存到方法体。
				box = param_slots[i];
				//if (box.ValueType == NaNBoxing.BoxType.HeapPtr) //仅在是堆对象时，才可能触发维护引用的操作
				if (box.HeapKind >= (byte)RtHeapTypeKind.INSTANCE) //仅在是堆对象时，才可能触发维护引用的操作
				{
					param_slots[i].SetUndefined();

					//if (box.ValueType == NaNBoxing.BoxType.HeapPtr)
					//if(box.HeapKind >= (byte)RtHeapTypeKind.INSTANCE)
					{
						ScopeHeapLocater scopeHeapLocater;
						scopeHeapLocater.ScopeIndex = (ushort)method_body_linkcodesocpe.index;
						scopeHeapLocater.MemberIndex = i;

						PrepareSaveMethodScope(m_scopePayload, scopeHeapLocater, ref box, null, &scope_ptr, ref error, false /*结构体拷贝传递*/);
						Debug.Assert(!error.raised);
					}
					param_slots[i] = box;
				}

			}

#if PROFILEPLAYER
				InstructionProfiler.Profile_MethodStart(method);
#endif

			if (returnSlotIndex > -1)
			{
				result.setDefault(method.ReturnTypeKind);
				//Context.StackSlots[returnSlotIndex].setDefault(method.ReturnTypeKind);
			}

			if (info.instructions > 0)
			{
				int calleelastpos = Context.StackPosition;
				Context.StackPosition += scopeHoleSlots;

				int stPos = Context.StackPosition;
				Context.StackPosition += info.useSlots;

				//Context.BackTrace[Context.BackTraceIndex].Method = method;
				Context.BackTraceIndex++; ;

				Span<NaNBoxing> slots = Context.StackSlots.AsSpan(stPos, info.useSlots);
				slots.Clear(); //栈清空 -- 防止GC时错误访问



				int P_PC;
				Execute(ref info, mScope, mScopeId, //scopeType, 
					slots, stPos, out P_PC, ref error, returnSlotIndex, calleelastpos, null);

				Context.BackTraceIndex--;
				//Context.BackTrace[Context.BackTraceIndex].Method = null;



				Context.StackPosition -= info.useSlots;
				Context.StackPosition -= scopeHoleSlots;

				m_scopePayload.ParentPtr = 0;
				mScope.Type = null;


				if (!error.raised)
				{
					if ((method.Flags & MethodFlags.Native) == MethodFlags.Native)
					{
						Debug.Assert(method.IsConstructor); // 只有在有类成员初始化代码并且构造函数还是native的时候会触发
						goto run_native;
					}
					else
					{
#if PROFILEPLAYER
							InstructionProfiler.Profile_MethodEnd();
#endif
						if (saved_this != null) CleanRefThisSlotFlag(saved_this);

					}
				}
				else
				{
#if PROFILEPLAYER
						InstructionProfiler.Profile_MethodEnd();
#endif

					if (saved_this != null) CleanRefThisSlotFlag(saved_this);

					//记录当前报错堆栈，看上级调用是否处理这个错误
					Context.errorStack.AddTrace(method, P_PC);


				}
				return ;
			}
			else if ((method.Flags & MethodFlags.Native) == MethodFlags.Native)
			{
				goto run_native;
			}
			else
			{
#if PROFILEPLAYER
					InstructionProfiler.Profile_MethodEnd();
#endif
				if (saved_this != null) CleanRefThisSlotFlag(saved_this);
				return;
			}

		run_native:

			SetNativeDelegate(method, ref error);
			if (error.raised)
			{
				goto lbl_native_called;
			}

			//Context.BackTrace[Context.BackTraceIndex].Method = method;
			Context.BackTraceIndex++; ;
			Context.StackPosition += scopeHoleSlots;
			((NativeFun)method.nativefunction_delegate)(Context, method, mScopeId, _this_, Context.StackPosition, ref error, returnSlotIndex);
			Context.StackPosition -= scopeHoleSlots;
			Context.BackTraceIndex--;
		//Context.BackTrace[Context.BackTraceIndex].Method = null;


#if PROFILEPLAYER
				InstructionProfiler.Profile_MethodEnd();
#endif

		lbl_native_called:

			if (saved_this != null) CleanRefThisSlotFlag(saved_this);

			m_scopePayload.ParentPtr = 0;
			mScope.Type = null;

			if (!error.raised)
			{

			}
			else
			{
				//记录当前报错堆栈，看上级调用是否处理这个错误
				Context.errorStack.AddTrace(method, 0);

			}

		flag_handle_error:

			return;
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining )]
		internal unsafe NaNBoxing RunMethod(ASMethod method, NaNBoxing thisPtr, int scope_ptr,
			//ASContainer scopeType, 
			ushort args, byte* argementPtr,
			Span<NaNBoxing> slot, ref ReceiveError error, int returnSlotIndex, int callee_closure_ptr = 0, bool skipcheckargscount = false)
		{
#if FORCOMPILER
			if (IsComputeConstExpr)
			{
				ComputeConstExprOnRunMethod(method);
			}
#endif

			if (Context.BackTraceIndex >= Context.MAX_BACKTRACE)
			{
				RaiseStackOverflow(ref error);
				return default;
			}

#if DEBUG && !DEBUG_PLAYER //&& false
			// 在执行函数前，所有未保存的堆对象都需要保存，避免在接下来可能的GC中被意外回收。
			// 测试时此处强行执行一次回收，如有问题，则可能会暴露。
			Context.GC.ForceGC(ref error);

#else
			Context.GC.CheckGC(ref error);
#endif


			if (((method.Flags & (MethodFlags.NeedRest | MethodFlags.NeedArguments | MethodFlags.Generator | MethodFlags.ASYNC)) == 0)
				&&
				method.Parameters.Count == args
				
				)
			{
				if (returnSlotIndex > -1)
				{
					ref NaNBoxing r = ref Context.StackSlots[returnSlotIndex];
					RunMethod_MatchArgs(method, thisPtr, scope_ptr, slot, args, argementPtr, returnSlotIndex, ref error, ref r);
					return r;
				}
				else
				{
					NaNBoxing t = default;
					ref NaNBoxing r = ref t;
					RunMethod_MatchArgs(method, thisPtr, scope_ptr, slot, args, argementPtr, returnSlotIndex, ref error, ref r);
					return r;
				}

			}
			else
			{
				return RunMethod_FullCheck(method, thisPtr, scope_ptr, args, argementPtr, slot, ref error, returnSlotIndex, callee_closure_ptr, skipcheckargscount);
			}

		}
	}
}
