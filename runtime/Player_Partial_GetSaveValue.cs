using juicescript.ABC;
using juicescript.ABC.Locaters;
using juicescript.runtime.buildin;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

namespace juicescript.runtime
{
	public partial class Player
	{

		/// <summary>
		/// 保存到堆前，如有缓存对象需要先复制到堆。
		/// 由于此操作可能会创建一个新对象，并且没有保存到栈里，所以凡是使用了此操作的指令，在使用新对象之前不能引发GC否则会出现意外。
		/// </summary>
		/// <param name="value"></param>
		/// <param name="error"></param>
		/// <returns></returns>
		internal NaNBoxing GetSaveValue(NaNBoxing value, ref ReceiveError error)
		{
			if (value.ValueType == NaNBoxing.BoxType.HeapPtr)
			{
				RtHeapInstance instance = Context.GC.Heap[value.HeapPtr];
				
				switch (instance.TypeKind)
				{
					case RtHeapTypeKind.CLASS:
					case RtHeapTypeKind.GLOBAL:
					case RtHeapTypeKind.STRING:
					case RtHeapTypeKind.DYNAMIC_PROPERTYS:
					case RtHeapTypeKind.NAMESPACE:
						break;
					case RtHeapTypeKind.INSTANCE:
						{
							if (((ASInstance)instance.Type).Flags.HasFlag(ClassFlags.Struct))
							{
								//结构体，必要复制一份。
								RtHeapInstance heapObj;
								int ptr = Context.GC.AllocInstance((ASInstance)instance.Type, out heapObj);
								if (ptr == 0)
								{
									//这种情况应该为致命错误，就不要再catch了
									RaiseFault(ref error);
									return value;
								}
								((RtPayloadInstance)heapObj.facility).CopyFrom(instance, this, instance.Type._link_codescope.TypeLayout.Size);
								value.SetHeapPtr(ptr);
							}
							else if (value.HeapPtr < Context.CacheInstancePtr + Context.STACK_LENGTH)//((RtPayloadInstance)instance.facility).isCache)
							{
								RtPayloadInstance target;
								var src_ptr = RtPayloadInstance.FindAndUpdateHeapInstancePtr(value.HeapPtr, this, out target); //查找最终指向的目标
								if (src_ptr < Context.CacheInstancePtr + Context.STACK_LENGTH)
								{

									RtHeapInstance heapObj;
									int ptr = Context.GC.AllocInstance((ASInstance)instance.Type, out heapObj);
									if (ptr == 0)
									{
										//这种情况应该为致命错误，就不要再catch了
										RaiseFault(ref error);
										return value;
									}

									
									((RtPayloadInstance)heapObj.facility).CopyFrom(target, (ASInstance)heapObj.Type ,this, instance.Type._link_codescope.TypeLayout.Size);
									target.HEAPINSTANCE_PTR = ptr;

									value.SetHeapPtr(ptr);

								}
								else
								{
									value.SetHeapPtr(src_ptr);
								}
							}
						}
						break;

					case RtHeapTypeKind.CLOSURE:
						{
#if FORCOMPILER
							if (IsComputeConstExpr)
							{
								throw new EvalConstException();
							}
#endif

							if (value.HeapPtr < Context.M_ClosurePtr + Context.STACK_LENGTH)
							{
								RtPayloadClosure cache = (RtPayloadClosure)instance.facility;
								var src_ptr = RtPayloadClosure.FindAndUpdateHeapInstancePtr(value.HeapPtr, this, out cache);

								if (src_ptr < Context.M_ClosurePtr + Context.STACK_LENGTH)
								{

									if (cache.cloneing_ptr != 0)
									{
										value.SetHeapPtr(cache.cloneing_ptr);
										break;
									}



									RtHeapInstance heapObj;
									int ptr = Context.GC.AllocClosure(((ASMethodBody)instance.Type).Method);
									if (ptr == 0)
									{
										//这种情况应该为致命错误，就不要再catch了
										RaiseFault(ref error);
										return value;
									}

									cache.cloneing_ptr = ptr;

									heapObj = Context.GC.Heap[ptr];
									RtPayloadClosure closure = (RtPayloadClosure)heapObj.facility;

									closure.CopyDataFrom(cache, this);

									//原cache的堆对象追踪到堆指针上
									cache.HEAPINSTANCE_PTR = ptr;

									//将缓存的MethodScope生成到堆里
									int scope_p = cache.ScopePtr;
									if (scope_p != 0)
									{
										NaNBoxing s = new NaNBoxing();
										s.SetHeapPtr(scope_p);
										s = GetSaveValue(s, ref error);
										if (error.raised)
										{
											cache.cloneing_ptr = 0;

											return new NaNBoxing();
										}

#if DEBUG
										if (s.ValueType != NaNBoxing.BoxType.HeapPtr)
											throw new InvalidOperationException();
#endif
										closure.ScopePtr = s.HeapPtr;

										//原cache也指向新生成的堆中.
										cache.ScopePtr = s.HeapPtr;
									}
									else
									{
										closure.ScopePtr = cache.ScopePtr;
									}

									cache.cloneing_ptr = 0;

									value.SetHeapPtr(ptr);


								}
								else
								{
									value.SetHeapPtr(src_ptr);
								}
							}

						}
						break;
					case RtHeapTypeKind.MethodScope:
						{
							if (value.HeapPtr < Context.M_MethodScopePtr + Context.MAX_BACKTRACE)
							{
								if (!((ASMethodBody)instance.Type).Method.Flags.HasFlag(MethodFlags.NeedActivation))
								{
									//不被引用的method,跳过
									RtPayloadMethodScope scope = (RtPayloadMethodScope)instance.facility;
									if (scope.ParentPtr == 0)
									{
										value.SetHeapPtr(0);
									}
									else
									{
										NaNBoxing p = new NaNBoxing();
										p.SetHeapPtr(scope.ParentPtr);
										p = GetSaveValue(p, ref error);
										if (error.raised)
										{
											return new NaNBoxing();
										}

										value.SetHeapPtr(p.HeapPtr);

									}

								}
								else
								{
									RtPayloadMethodScope cacheMscope = (RtPayloadMethodScope)instance.facility;
									if (cacheMscope.cloneout_ptr != 0)
									{
										value.SetHeapPtr(cacheMscope.cloneout_ptr);
										break;
									}

									var cacheSpan = cacheMscope.__get_slots_for_gc;

									RtHeapInstance heapObj;
									int ptr = Context.GC.AllocMethodScope(new NaNBoxing[cacheSpan.Length], 0, instance.Type._link_codescope);
									if (ptr == 0)
									{
										//这种情况应该为致命错误，就不要再catch了
										RaiseFault(ref error);
										return value;
									}

									cacheMscope.cloneout_ptr = ptr;

									heapObj = Context.GC.Heap[ptr];
									heapObj.Type = instance.Type;

									RtPayloadMethodScope heap_scope = (RtPayloadMethodScope)heapObj.facility;

									for (int i = 0; i < cacheSpan.Length; i++)
									{
										var oldSpanValue = cacheSpan[i];
										//cacheSpan[i].SetUndefined();//原值删除 最后会整体替代到新的堆的值。

										NaNBoxing slotV = GetSaveValue(oldSpanValue, ref error);
										if (error.raised)
										{
											cacheMscope.cloneout_ptr = 0;
											return new NaNBoxing();
										}

										//cacheSpan[i] = slotV;
										heap_scope.SetSlot(slotV, (ushort)i);
									}
									cacheMscope.ChangeStore(heap_scope);

									//cacheMscope.cloneing_ptr = 0;

									if (cacheMscope.ParentPtr != 0)
									{
										NaNBoxing p = new NaNBoxing();
										p.SetHeapPtr(cacheMscope.ParentPtr);
										p = GetSaveValue(p, ref error);
										if (error.raised)
										{
											return new NaNBoxing();
										}

#if DEBUG
										if (p.ValueType != NaNBoxing.BoxType.HeapPtr)
											throw new InvalidOperationException();
#endif

										heap_scope.ParentPtr = p.HeapPtr;

									}
									else
									{
										heap_scope.ParentPtr = 0;
									}

									value.SetHeapPtr(ptr);
								}
							}
						}
						break;
					case RtHeapTypeKind.ARRAY:
						{
							RtPayloadArray arrStore;// = (RtPayloadArray)instance.facility;
							int arr_ptr = RtPayloadArray.FindAndUpdateHeapInstancePtr(value.HeapPtr,this,out arrStore);

							if (arrStore.StoreMode == RtPayloadArray.ArrayStoreMode.normal)
							{
								value.SetHeapPtr(arr_ptr);
								break;
							}
							else if (arrStore.StoreMode == RtPayloadArray.ArrayStoreMode.cache_on_stack)
							{
								int arr_heap_ptr = arrStore.ChangeStoreToHeap(this, ref error);
								if (error.raised)
								{
									return value;
								}
								value.SetHeapPtr(arr_heap_ptr);
							}
							else
							{
#if DEBUG
								if (arrStore.StoreMode != RtPayloadArray.ArrayStoreMode.cache)
								{
									throw new InvalidOperationException();
								}
#endif
								
								int arr_heap_ptr = arrStore.ChangeStoreToHeap(this, ref error);
								if (error.raised)
								{
									return value;
								}
								value.SetHeapPtr(arr_heap_ptr);
							}
						}
						break;
					case RtHeapTypeKind.VECTOR:
						{
							RtPayloadVector vector;
							int vec_ptr = RtPayloadVector.FindAndUpdateHeapInstancePtr(value.HeapPtr, this, out vector);
							if (vec_ptr < Context.CacheVectorPtr + Context.STACK_LENGTH)
							{
								vec_ptr = vector.ChangeStoreToHeap( (ASInstance)instance.Type ,this,ref error);
								if (error.raised)
								{ 
									return value;	
								}
								value.SetHeapPtr(vec_ptr);
							}
						}
						
						break;
					case RtHeapTypeKind.SHAPE:
					case RtHeapTypeKind.STACK_CACHE_OBJ:
					default:
#if DEBUG
						throw new InvalidOperationException();
#else
						Environment.FailFast("出错了，这里跑不到"); return default;
#endif
				}

			}

			return value;

		}


		private unsafe void StoreReturnSlot(ref NaNBoxing returnSlot,int stackStPos, int returnSlotIndex, int calleelastpos, int scope_ptr, NaNBoxing value, ref ReceiveError error,bool isyieldreturn_or_holderror =false)
		{
#if DEBUG
			if (value.ValueType != NaNBoxing.BoxType.HeapPtr)
			{
				throw new InvalidOperationException();
			}
#endif

			//if (value.ValueType == NaNBoxing.BoxType.HeapPtr)
			{
				//返回对象必定是一个调用方的栈上的Slot.
				var obj = Context.GC.Heap[value.HeapPtr];
				if (obj.TypeKind == RtHeapTypeKind.INSTANCE)
				{
					if (!isyieldreturn_or_holderror && obj.Type == Context.GENERATOR.Instance)
					{
						RaiseTypeError(ref error, value, TypeKind.Function);
					}
					else if (!(value.HeapPtr < Context.CacheInstancePtr + Context.STACK_LENGTH) //堆里
						||
						(value.HeapPtr < Context.CacheInstancePtr + calleelastpos) //传入
						)
					{
						if (((RtPayloadInstance)obj.facility).IsRefVectorOrFromArrayOrStruct(this, (ASInstance)obj.Type))
						{
							//Clone结构体
							int clonedptr = returnSlotIndex + Context.CacheInstancePtr;
							var cacheObj = Context.GC.Heap[clonedptr];
							cacheObj.Type = obj.Type;

							((RtPayloadInstance)cacheObj.facility).methodscopeslot_ref_state = 0;
							((RtPayloadInstance)cacheObj.facility).HEAPINSTANCE_PTR = 0;
							((RtPayloadInstance)cacheObj.facility).CopyFrom(obj, this, obj.Type._link_codescope.TypeLayout.Size);

							returnSlot.SetHeapPtr(clonedptr);
						}
						else
						{
							RtPayloadInstance _temp;
							int t = RtPayloadInstance.FindAndUpdateHeapInstancePtr(value.HeapPtr, this, out _temp);
							returnSlot.SetHeapPtr(t);
						}
					}
					else
					{
						if (((ASInstance)obj.Type).Flags.HasFlag(ClassFlags.Struct))
						{
							//Clone结构体
							int clonedptr = returnSlotIndex + Context.CacheInstancePtr;
							var cacheObj = Context.GC.Heap[clonedptr];
							cacheObj.Type = obj.Type;

							((RtPayloadInstance)cacheObj.facility).methodscopeslot_ref_state = 0;
							((RtPayloadInstance)cacheObj.facility).HEAPINSTANCE_PTR = 0;
							((RtPayloadInstance)cacheObj.facility).CopyFrom(obj, this, obj.Type._link_codescope.TypeLayout.Size);

							returnSlot.SetHeapPtr(clonedptr);
						}
						else
						{

							int dstptr = returnSlotIndex + Context.CacheInstancePtr;
							var dstObj = Context.GC.Heap[dstptr];

							dstObj.Type = obj.Type;
							((RtPayloadInstance)dstObj.facility).methodscopeslot_ref_state = 0;
							((RtPayloadInstance)dstObj.facility).HEAPINSTANCE_PTR = 0;
							((RtPayloadInstance)dstObj.facility).CopyFrom(obj, this, obj.Type._link_codescope.TypeLayout.Size);

							((RtPayloadInstance)obj.facility).HEAPINSTANCE_PTR = dstptr;
							returnSlot.SetHeapPtr(dstptr);
						}
					}
				}
				else if (obj.TypeKind == RtHeapTypeKind.ARRAY)
				{
					RtPayloadArray arr;
					int arr_ptr = RtPayloadArray.FindAndUpdateHeapInstancePtr(value.HeapPtr, this, out arr);

					if (arr.StoreMode == RtPayloadArray.ArrayStoreMode.cache_on_stack)
					{
						var method = ((ASMethodBody)Context.GC.Heap[scope_ptr].Type).Method;
						int callee_slot_idx = stackStPos - method.Body._link_codescope.Members.Count - 2;

						if (arr.stack_store_startindex + arr.stack_store.Length + 1 < callee_slot_idx)
						{
							//传入的
							returnSlot.SetHeapPtr(arr_ptr);
						}
						else
						{
							value = GetSaveValue(value, ref error);
							if (error.raised)
							{
								return;
							}
							returnSlot.SetHeapPtr(value.HeapPtr);
							
						}
					}
					else if (arr.StoreMode == RtPayloadArray.ArrayStoreMode.cache)
					{
						if (arr_ptr < Context.CacheArrayPtr + calleelastpos) //传入
						{
							returnSlot.SetHeapPtr(arr_ptr);
						}
						else
						{
							var dstArrayPtr = returnSlotIndex + Context.CacheArrayPtr;
							var dst = (RtPayloadArray)Context.GC.Heap[dstArrayPtr].facility;

							Context.GC.Heap[dstArrayPtr].Type = Context.ARRAY.Instance;
							dst.CopyCacheFrom(arr, this);
							dst.HEAPINSTANCE_PTR = 0;
							dst.methodscopeslot_ref_state = 0;

							arr.HEAPINSTANCE_PTR = dstArrayPtr;

							returnSlot.SetHeapPtr(dstArrayPtr);
						}

					}
					else
					{
						//必然是普通堆里的对象
						returnSlot.SetHeapPtr(arr_ptr);
					}
				}
				else if (obj.TypeKind == RtHeapTypeKind.VECTOR)
				{
					RtPayloadVector vec;
					int vec_ptr = RtPayloadVector.FindAndUpdateHeapInstancePtr(value.HeapPtr, this, out vec);
					if (vec_ptr < Context.CacheVectorPtr + calleelastpos) // 传入
					{
						returnSlot.SetHeapPtr(vec_ptr);
					}
					else
					{
						var dstVecPtr = returnSlotIndex + Context.CacheVectorPtr;
						var dstObj = Context.GC.Heap[dstVecPtr];
						dstObj.Type = Context.GC.Heap[vec_ptr].Type;
						var dst = (RtPayloadVector)dstObj.facility;

						dst.HEAPINSTANCE_PTR = 0;
						dst.CopyCacheFrom(vec, this);
						dst.methodscopeslot_ref_state = 0;
						
						vec.HEAPINSTANCE_PTR += dstVecPtr;

						returnSlot.SetHeapPtr(dstVecPtr);

					}
				}
				else if (obj.TypeKind == RtHeapTypeKind.CLOSURE)
				{
					if (!(value.HeapPtr < Context.M_ClosurePtr + Context.STACK_LENGTH) //堆里
						||
						(value.HeapPtr < Context.M_ClosurePtr + calleelastpos) //传入
						)
					{
						RtPayloadClosure _temp;
						int t = RtPayloadClosure.FindAndUpdateHeapInstancePtr(value.HeapPtr, this, out _temp);

						returnSlot.SetHeapPtr(t);
					}
					else
					{
						var srcClosure = (RtPayloadClosure)obj.facility;

						int dstClosurePtr = returnSlotIndex + Context.M_ClosurePtr;
						var dstClosure = (RtPayloadClosure)Context.GC.Heap[dstClosurePtr].facility;

						Context.GC.Heap[dstClosurePtr].Type = obj.Type;

						dstClosure.CopyDataFrom(srcClosure, this);
						dstClosure.methodscopeslot_ref_state = 0;
						dstClosure.HEAPINSTANCE_PTR = 0;

						srcClosure.HEAPINSTANCE_PTR = dstClosurePtr;

						//处理 This 指针
						if (dstClosure.This.ValueType == NaNBoxing.BoxType.HeapPtr)
						{
							bool needupdatescopePtr = dstClosure.ScopePtr == dstClosure.This.HeapPtr;

							var _this = Context.GC.Heap[dstClosure.This.HeapPtr];
							if (_this.TypeKind == RtHeapTypeKind.INSTANCE)
							{
								StoreReturnSlot(ref dstClosure.This, stackStPos, returnSlotIndex, calleelastpos, scope_ptr, dstClosure.This, ref error);
								if (needupdatescopePtr)
								{
									dstClosure.ScopePtr = dstClosure.This.HeapPtr;
								}

							}
							else if (_this.TypeKind == RtHeapTypeKind.CLOSURE)
							{
								/* 只有apply或者call可能造成这种情况。
								* var a:Function= function ( ...rest ):void 
								*	{
								*	};
								*	var f1 = a.apply;
								*/
								//这里就只能分配到堆里了。
								var s_this = GetSaveValue(dstClosure.This, ref error);
								if (error.raised)
								{
									return;
								}
								dstClosure.This = s_this;
							}
							else if (_this.TypeKind == RtHeapTypeKind.ARRAY)
							{
								StoreReturnSlot(ref dstClosure.This, stackStPos, returnSlotIndex, calleelastpos, scope_ptr, dstClosure.This, ref error);
								if (needupdatescopePtr)
								{
									dstClosure.ScopePtr = dstClosure.This.HeapPtr;
								}

								//throw new NotImplementedException();
							}
							else if (_this.TypeKind == RtHeapTypeKind.VECTOR)
							{
								StoreReturnSlot(ref dstClosure.This, stackStPos, returnSlotIndex, calleelastpos, scope_ptr, dstClosure.This, ref error);
								if (needupdatescopePtr)
								{
									dstClosure.ScopePtr = dstClosure.This.HeapPtr;
								}

								//throw new NotImplementedException();
							}
#if DEBUG
							else if (_this.TypeKind == RtHeapTypeKind.MethodScope)
							{
								throw new InvalidOperationException();
							}
#endif
							else
							{
								//pass
							}
						}

						
						{
							//处理MethodScope
							if (!((ASMethodBody)obj.Type).Method.__ismethod)
							{
								if (dstClosure.ScopePtr != 0)
								{
									int sptr = dstClosure.ScopePtr;

									RtPayloadMethodScope last_scope = null;

								lbl_parent:
									var scope = Context.GC.Heap[sptr];

									bool needbreak = (sptr == scope_ptr);

									if (scope.TypeKind == RtHeapTypeKind.GLOBAL || scope.TypeKind == RtHeapTypeKind.CLASS || scope.TypeKind == RtHeapTypeKind.INSTANCE)
									{
										/*
										 const yCombinator = function (k) {
										  const f = function (g) {
											return g(g);
										  };

										  const p = function (r) {
											return function (n) {
											  return k(r(r))(n);
											};
										  };

										  return f(p);
										};

										const factProto = function (h) {
										  return function (x) {
											return x == 0 ? 1 : x * h(x - 1);
										  };
										};

										trace(yCombinator(factProto)(5));  // 120
										 */

										//这种代码可能会有
										needbreak = true;
										goto lbl_break;
									}

#if DEBUG
									if (scope.TypeKind != RtHeapTypeKind.MethodScope)
									{
										throw new InvalidOperationException();
									}
#endif
									

									if (sptr < Context.M_ClosurePtr + Context.STACK_LENGTH)
									{
										if (((ASMethodBody)scope.Type).Method.Flags.HasFlag(MethodFlags.NeedActivation))
										{

											RtPayloadMethodScope cacheMscope = (RtPayloadMethodScope)scope.facility;
											var cacheSpan = cacheMscope.__get_slots_for_gc;

											RtHeapInstance heapObj;
											int ptr = Context.GC.AllocMethodScope(new NaNBoxing[cacheSpan.Length], 0, scope.Type._link_codescope);
											if (ptr == 0)
											{
												//这种情况应该为致命错误，就不要再catch了
												RaiseFault(ref error);
												return;
											}

											cacheMscope.cloneout_ptr = ptr;

											heapObj = Context.GC.Heap[ptr];
											heapObj.Type = scope.Type;

											RtPayloadMethodScope heap_scope = (RtPayloadMethodScope)heapObj.facility;
											for (int i = 0; i < cacheSpan.Length; i++)
											{
												var oldSpanValue = cacheSpan[i];

												NaNBoxing slotV = GetSaveValue(oldSpanValue, ref error);
												if (error.raised)
												{
													cacheMscope.cloneout_ptr = 0;
													return;
												}

												heap_scope.SetSlot(slotV, (ushort)i);
											}
											cacheMscope.ChangeStore(heap_scope);
											//cacheMscope.cloneing_ptr = 0;

											if (last_scope != null)
											{
												last_scope.ParentPtr = ptr;
											}
											else
											{
												dstClosure.ScopePtr = ptr;
											}

											last_scope = heap_scope;

											sptr = ((RtPayloadMethodScope)scope.facility).ParentPtr;
										}
										else
										{
											sptr = ((RtPayloadMethodScope)scope.facility).ParentPtr;
										}
									}
									else
									{
										last_scope = (RtPayloadMethodScope)scope.facility;
										sptr = last_scope.ParentPtr;
									}

								lbl_break:

									if (needbreak)
									{
										if (last_scope != null)
										{
											last_scope.ParentPtr = sptr;
										}
										else
										{
											dstClosure.ScopePtr = sptr;
										}
									}
									else
									{
										goto lbl_parent;
									}

								}


							}



						}
						

						//将srcClosure覆盖为新的
						srcClosure.This = dstClosure.This;
						srcClosure.ScopePtr = dstClosure.ScopePtr;

						value.SetHeapPtr(dstClosurePtr);
						returnSlot = value;
					}
				}
				else
				{
					returnSlot = value;
				}
			}
			//else
			//{
			//	returnSlot = value;
			//}
		}


		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private unsafe int prepare_savemethodscope_updateref(RtPayloadMethodScope heap, int ptr, ref ScopeHeapLocater heapLocater, ASContainer type, int* m_scope, int* method_scopes)
		{
			int* __test = m_scope;
			int max = int.MinValue;
			int min = int.MaxValue;
			do
			{
				--__test;

				if (*__test - Context.M_MethodScopePtr < Context.MAX_BACKTRACE)
				{
					max = Math.Max(max, *__test);
					min = Math.Min(min, *__test);
				}

			} while (__test != method_scopes);


			Debug.Assert(max < Context.MAX_BACKTRACE);

			Debug.Assert(m_scope != null && method_scopes != null);
			int iptr = ptr;
			RtPayloadInstance oldPayload;
			ptr = RtPayloadInstance.FindAndUpdateHeapInstancePtr(ptr, this, out oldPayload); //更新最终指向的目标

			int copyed_ptr = 0;

			if (!(ptr < Context.CacheInstancePtr + Context.STACK_LENGTH)) //堆里的对象,无需拷贝
			{
				copyed_ptr = ptr;
			}


			RtPayloadInstance _toupdateref = null; //追踪对新拷贝对象的引用

			//				int* om_s = m_scope;

			//				m_scope = om_s;

			//				do
			//				{
			//					//扫描所有可能引用。
			//					--m_scope;

			//					RtPayloadMethodScope scope = (RtPayloadMethodScope)Context.GC.Heap[*m_scope].facility;
			//					for (int i = 0; i < scope.SlotCount ; ++i)
			//					{
			//						if (!(scope == heap && i == heapLocater.MemberIndex))
			//						{
			//							var v = scope.ReadSlot((ushort)i, this);
			//							if (v.ValueType == NaNBoxing.BoxType.HeapPtr)
			//							{
			//								var inmember = Context.GC.Heap[v.HeapPtr];

			//								if (v.HeapPtr == ptr || inmember.TypeKind == RtHeapTypeKind.INSTANCE)
			//								{
			//									RtPayloadInstance _temp;
			//									if (v.HeapPtr == ptr || RtPayloadInstance.FindAndUpdateHeapInstancePtr(v.HeapPtr, this, out _temp) == ptr)
			//									{
			//										if (copyed_ptr == 0)
			//										{
			//											//复制一份。
			//											copyed_ptr = scope.StackPos + i + Context.CacheInstancePtr;
			//											if (copyed_ptr == ptr)
			//											{
			//#if DEBUG
			//												throw new InvalidOperationException();
			//#else
			//											Environment.FailFast("出错了，这里跑不到"); return default;
			//#endif
			//												//copyed_ptr = 0;
			//												//continue;
			//											}

			//											var dstObj = Context.GC.Heap[copyed_ptr];

			//											dstObj.Type = type;
			//											((RtPayloadInstance)dstObj.facility).methodscopeslot_ref_state = 1;
			//											((RtPayloadInstance)dstObj.facility).HEAPINSTANCE_PTR = 0;
			//											((RtPayloadInstance)dstObj.facility).CopyFrom(oldPayload, (ASInstance)dstObj.Type, this, type._link_codescope.TypeLayout.Size);

			//											oldPayload.HEAPINSTANCE_PTR = copyed_ptr;

			//											_toupdateref = (RtPayloadInstance)dstObj.facility;

			//											//更新引用
			//											v.SetHeapPtr(copyed_ptr);
			//											scope.SetSlot(v, (ushort)i);
			//										}
			//										else
			//										{
			//											if (_toupdateref != null)
			//											{
			//#if DEBUG
			//												if (_toupdateref.methodscopeslot_ref_state != 1)
			//												{
			//													throw new InvalidOperationException();
			//												}
			//#endif
			//												_toupdateref.methodscopeslot_ref_state = 2;
			//											}

			//											//更新引用
			//											v.SetHeapPtr(copyed_ptr);
			//											scope.SetSlot(v, (ushort)i);
			//										}
			//									}
			//								}
			//								else if (inmember.TypeKind == RtHeapTypeKind.CLOSURE)
			//								{
			//									var closure = inmember;
			//									//lbl_flag:
			//									ref var This = ref ((RtPayloadClosure)closure.facility).This;
			//									if (This.ValueType == NaNBoxing.BoxType.HeapPtr)
			//									{
			//										bool needupdateScopePtr = This.HeapPtr == ((RtPayloadClosure)closure.facility).ScopePtr;

			//										var _this = Context.GC.Heap[This.HeapPtr];
			//										if (_this.TypeKind == RtHeapTypeKind.INSTANCE)
			//										{
			//											RtPayloadInstance _temp;
			//											if (This.HeapPtr == ptr || RtPayloadInstance.FindAndUpdateHeapInstancePtr(This.HeapPtr, this, out _temp) == ptr)
			//											{
			//												if (copyed_ptr == 0)
			//												{
			//													//复制一份。
			//													copyed_ptr = scope.StackPos + i + Context.CacheInstancePtr;
			//#if DEBUG
			//													if (copyed_ptr == ptr)
			//													{
			//														throw new InvalidOperationException();
			//													}
			//#endif
			//													var dstObj = Context.GC.Heap[copyed_ptr];

			//													dstObj.Type = type;
			//													((RtPayloadInstance)dstObj.facility).methodscopeslot_ref_state = 1;
			//													((RtPayloadInstance)dstObj.facility).HEAPINSTANCE_PTR = 0;
			//													((RtPayloadInstance)dstObj.facility).CopyFrom(oldPayload, (ASInstance)dstObj.Type, this, type._link_codescope.TypeLayout.Size);

			//													_toupdateref = (RtPayloadInstance)dstObj.facility;

			//													oldPayload.HEAPINSTANCE_PTR = copyed_ptr;
			//													This.SetHeapPtr(copyed_ptr);
			//													if (needupdateScopePtr)
			//													{
			//														((RtPayloadClosure)closure.facility).ScopePtr = copyed_ptr;
			//													}
			//												}
			//												else
			//												{
			//													if (_toupdateref != null)
			//													{
			//#if DEBUG
			//														if (_toupdateref.methodscopeslot_ref_state != 1)
			//														{
			//															throw new InvalidOperationException();
			//														}
			//#endif
			//														_toupdateref.methodscopeslot_ref_state = 2;
			//													}

			//													This.SetHeapPtr(copyed_ptr);
			//													if (needupdateScopePtr)
			//													{
			//														((RtPayloadClosure)closure.facility).ScopePtr = copyed_ptr;
			//													}
			//												}
			//											}
			//										}
			//#if DEBUG
			//										else if (_this.TypeKind == RtHeapTypeKind.CLOSURE)
			//										{
			//											throw new InvalidOperationException();
			//											//closure = _this;
			//											//goto lbl_flag;
			//										}
			//#endif
			//										else
			//										{
			//											//肯定不会和instance相同，过。 
			//											//throw new NotImplementedException();
			//										}

			//									}

			//								}
			//							}
			//						}
			//					}



			//				} while (m_scope != method_scopes);

			for (int k = min; k <= max; k++)
			{

				RtPayloadMethodScope scope = (RtPayloadMethodScope)Context.GC.Heap[k].facility;
				for (int i = 0; i < scope.SlotCount; ++i)
				{
					if (!(scope == heap && i == heapLocater.MemberIndex))
					{
						var v = scope.ReadSlot((ushort)i, this);
						if (v.ValueType == NaNBoxing.BoxType.HeapPtr)
						{
							var inmember = Context.GC.Heap[v.HeapPtr];

							if (v.HeapPtr == ptr || inmember.TypeKind == RtHeapTypeKind.INSTANCE)
							{
								RtPayloadInstance _temp;
								if (v.HeapPtr == ptr || RtPayloadInstance.FindAndUpdateHeapInstancePtr(v.HeapPtr, this, out _temp) == ptr)
								{
									if (copyed_ptr == 0)
									{
										//复制一份。
										copyed_ptr = scope.StackPos + i + Context.CacheInstancePtr;
										if (copyed_ptr == ptr)
										{
#if DEBUG
											throw new InvalidOperationException();
#else
											Environment.FailFast("出错了，这里跑不到"); return default;
#endif
											//copyed_ptr = 0;
											//continue;
										}

										var dstObj = Context.GC.Heap[copyed_ptr];

										dstObj.Type = type;
										((RtPayloadInstance)dstObj.facility).methodscopeslot_ref_state = 1;
										((RtPayloadInstance)dstObj.facility).HEAPINSTANCE_PTR = 0;
										((RtPayloadInstance)dstObj.facility).CopyFrom(oldPayload, (ASInstance)dstObj.Type, this, type._link_codescope.TypeLayout.Size);

										oldPayload.HEAPINSTANCE_PTR = copyed_ptr;

										_toupdateref = (RtPayloadInstance)dstObj.facility;

										//更新引用
										v.SetHeapPtr(copyed_ptr);
										scope.SetSlot(v, (ushort)i);
									}
									else
									{
										if (_toupdateref != null)
										{
#if DEBUG
											if (_toupdateref.methodscopeslot_ref_state != 1)
											{
												throw new InvalidOperationException();
											}
#endif
											_toupdateref.methodscopeslot_ref_state = 2;
										}

										//更新引用
										v.SetHeapPtr(copyed_ptr);
										scope.SetSlot(v, (ushort)i);
									}
								}
							}
							else if (inmember.TypeKind == RtHeapTypeKind.CLOSURE)
							{
								var closure = inmember;
								//lbl_flag:
								ref var This = ref ((RtPayloadClosure)closure.facility).This;
								if (This.ValueType == NaNBoxing.BoxType.HeapPtr)
								{
									bool needupdateScopePtr = This.HeapPtr == ((RtPayloadClosure)closure.facility).ScopePtr;

									var _this = Context.GC.Heap[This.HeapPtr];
									if (_this.TypeKind == RtHeapTypeKind.INSTANCE)
									{
										RtPayloadInstance _temp;
										if (This.HeapPtr == ptr || RtPayloadInstance.FindAndUpdateHeapInstancePtr(This.HeapPtr, this, out _temp) == ptr)
										{
											if (copyed_ptr == 0)
											{
												//复制一份。
												copyed_ptr = scope.StackPos + i + Context.CacheInstancePtr;
#if DEBUG
												if (copyed_ptr == ptr)
												{
													throw new InvalidOperationException();
												}
#endif
												var dstObj = Context.GC.Heap[copyed_ptr];

												dstObj.Type = type;
												((RtPayloadInstance)dstObj.facility).methodscopeslot_ref_state = 1;
												((RtPayloadInstance)dstObj.facility).HEAPINSTANCE_PTR = 0;
												((RtPayloadInstance)dstObj.facility).CopyFrom(oldPayload, (ASInstance)dstObj.Type, this, type._link_codescope.TypeLayout.Size);

												_toupdateref = (RtPayloadInstance)dstObj.facility;

												oldPayload.HEAPINSTANCE_PTR = copyed_ptr;
												This.SetHeapPtr(copyed_ptr);
												if (needupdateScopePtr)
												{
													((RtPayloadClosure)closure.facility).ScopePtr = copyed_ptr;
												}
											}
											else
											{
												if (_toupdateref != null)
												{
#if DEBUG
													if (_toupdateref.methodscopeslot_ref_state != 1)
													{
														throw new InvalidOperationException();
													}
#endif
													_toupdateref.methodscopeslot_ref_state = 2;
												}

												This.SetHeapPtr(copyed_ptr);
												if (needupdateScopePtr)
												{
													((RtPayloadClosure)closure.facility).ScopePtr = copyed_ptr;
												}
											}
										}
									}
#if DEBUG
									else if (_this.TypeKind == RtHeapTypeKind.CLOSURE)
									{
										throw new InvalidOperationException();
										//closure = _this;
										//goto lbl_flag;
									}
#endif
									else
									{
										//肯定不会和instance相同，过。 
										//throw new NotImplementedException();
									}

								}

							}
						}
					}
				}

				if (scope.ParentPtr == iptr)
				{
					Debug.Assert(copyed_ptr != 0);
					scope.ParentPtr = copyed_ptr;
				}
			}


			return copyed_ptr;
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private unsafe int prepare_savemethodscope_beforeSave(RtPayloadMethodScope heap, NaNBoxing old, ScopeHeapLocater heapLocater, int* m_scope, int* method_scopes)
		{
			int* __test = m_scope;
			int max = int.MinValue;
			int min = int.MaxValue;
			do
			{
				--__test;

				if (*__test - Context.M_MethodScopePtr < Context.MAX_BACKTRACE)
				{
					max = Math.Max(max, *__test);
					min = Math.Min(min, *__test);
				}

			} while (__test != method_scopes);

			Debug.Assert(max - Context.M_MethodScopePtr < Context.MAX_BACKTRACE);

			//lbl_redo:
			int ptr = old.HeapPtr;
			var oldObj = Context.GC.Heap[ptr];
			if (oldObj.TypeKind == RtHeapTypeKind.INSTANCE)
			{
				//if (((ASInstance)oldObj.Type).Flags.HasFlag(ClassFlags.Struct))
				//{
				//	//pass //结构体也可能在传参时是引用。
				//	return ptr;
				//}
				//else 
				if (ptr == heap.StackPos + heapLocater.MemberIndex + Context.CacheInstancePtr)
				{
					if (((RtPayloadInstance)oldObj.facility).methodscopeslot_ref_state == 2) //只有状态是2的情况才可能会被引用
					{
						/*
						var a = new O(3);
						var b = a;
						a.tag = a;        //类似此代码，如果a本身被存入堆中。其他对a的引用仍然还在缓存对象中，这时候需要跟踪其他引用都改成堆中。
						a = new Main();
						b.tag = 6;
						 */

						return prepare_savemethodscope_updateref(heap, ptr, ref heapLocater, oldObj.Type, m_scope, method_scopes);
					}
					else
					{
						//pass
						return ptr;
					}
				}
				else
				{
					return ptr;
				}
			}
			else if (oldObj.TypeKind == RtHeapTypeKind.CLOSURE)
			{
				//更新Closure的引用
				if (ptr == heap.StackPos + heapLocater.MemberIndex + Context.M_ClosurePtr)
				{

					if (((RtPayloadClosure)oldObj.facility).methodscopeslot_ref_state != 2)
					{
						return ptr;
					}
					else
					{
						Debug.Assert(m_scope != null && method_scopes != null);

						//函数闭包里保存的this,也需要拷贝一份引用到别的地方
						var This = ((RtPayloadClosure)oldObj.facility).This;
						if (This.ValueType == NaNBoxing.BoxType.HeapPtr)
						{
							int this_ptr = prepare_savemethodscope_beforeSave(heap, This, heapLocater,m_scope,method_scopes); //更新原this,然后下面才能正确更新.
							((RtPayloadClosure)oldObj.facility).This.SetHeapPtr(this_ptr);
							//goto lbl_redo;
						}

						int copyed_ptr = 0;

						RtPayloadClosure toupate_ref = null;


						//							int* __scope = m_scope;
						//							do
						//							{
						//								--__scope;
						//								RtPayloadMethodScope scope = (RtPayloadMethodScope)Context.GC.Heap[*__scope].facility;
						//								for (int i = 0 ; i < scope.SlotCount - 1 ; ++i)
						//								{
						//									if (!(scope == heap && i == heapLocater.MemberIndex))
						//									{
						//										var v = scope.ReadSlot((ushort)i, this);
						//										if (v.ValueType == NaNBoxing.BoxType.HeapPtr && v.HeapPtr == ptr)
						//										{
						//											//复制一份新的Clousure
						//											if (copyed_ptr == 0)
						//											{
						//												copyed_ptr = scope.StackPos + i + Context.M_ClosurePtr;
						//#if DEBUG
						//												if (copyed_ptr == ptr)
						//												{
						//													throw new InvalidOperationException();
						//												}
						//#endif
						//												var srcClosure = (RtPayloadClosure)oldObj.facility;

						//												int final_ptr = RtPayloadClosure.FindAndUpdateHeapInstancePtr(ptr, this, out srcClosure);
						//												if (!(final_ptr < Context.M_ClosurePtr + Context.STACK_LENGTH)) //追踪是否已经在堆中。
						//												{
						//													copyed_ptr = final_ptr;
						//												}
						//												else
						//												{
						//													var dstClosure = (RtPayloadClosure)Context.GC.Heap[copyed_ptr].facility;
						//													Context.GC.Heap[copyed_ptr].Type = oldObj.Type;

						//													dstClosure.methodscopeslot_ref_state = 1; //设置被其他对象引用的状态

						//													dstClosure.CopyDataFrom(srcClosure, this);
						//													dstClosure.HEAPINSTANCE_PTR = srcClosure.HEAPINSTANCE_PTR;

						//													srcClosure.HEAPINSTANCE_PTR = copyed_ptr;

						//													toupate_ref = dstClosure;
						//												}
						//												v.SetHeapPtr(copyed_ptr);
						//												scope.SetSlot(v, (ushort)i);
						//											}
						//											else
						//											{
						//												if (toupate_ref != null)
						//												{
						//													toupate_ref.methodscopeslot_ref_state = 2;

						//												}

						//												v.SetHeapPtr(copyed_ptr);
						//												scope.SetSlot(v, (ushort)i);
						//											}
						//										}
						//									}
						//								}
						//							} while (__scope != method_scopes);

						for (int k = min; k <= max; k++)
						{
							RtPayloadMethodScope scope = (RtPayloadMethodScope)Context.GC.Heap[k].facility;
							for (int i = 0; i < scope.SlotCount - 1; ++i)
							{
								if (!(scope == heap && i == heapLocater.MemberIndex))
								{
									var v = scope.ReadSlot((ushort)i, this);
									if (v.ValueType == NaNBoxing.BoxType.HeapPtr && v.HeapPtr == ptr)
									{
										//复制一份新的Clousure
										if (copyed_ptr == 0)
										{
											copyed_ptr = scope.StackPos + i + Context.M_ClosurePtr;
#if DEBUG
											if (copyed_ptr == ptr)
											{
												throw new InvalidOperationException();
											}
#endif
											var srcClosure = (RtPayloadClosure)oldObj.facility;

											int final_ptr = RtPayloadClosure.FindAndUpdateHeapInstancePtr(ptr, this, out srcClosure);
											if (!(final_ptr < Context.M_ClosurePtr + Context.STACK_LENGTH)) //追踪是否已经在堆中。
											{
												copyed_ptr = final_ptr;
											}
											else
											{
												var dstClosure = (RtPayloadClosure)Context.GC.Heap[copyed_ptr].facility;
												Context.GC.Heap[copyed_ptr].Type = oldObj.Type;

												dstClosure.methodscopeslot_ref_state = 1; //设置被其他对象引用的状态

												dstClosure.CopyDataFrom(srcClosure, this);
												dstClosure.HEAPINSTANCE_PTR = srcClosure.HEAPINSTANCE_PTR;

												srcClosure.HEAPINSTANCE_PTR = copyed_ptr;

												toupate_ref = dstClosure;
											}
											v.SetHeapPtr(copyed_ptr);
											scope.SetSlot(v, (ushort)i);
										}
										else
										{
											if (toupate_ref != null)
											{
												toupate_ref.methodscopeslot_ref_state = 2;

											}

											v.SetHeapPtr(copyed_ptr);
											scope.SetSlot(v, (ushort)i);
										}
									}
								}
							}

							if (scope.ParentPtr == old.HeapPtr)
							{
								Debug.Assert(copyed_ptr != 0);
								scope.ParentPtr = copyed_ptr;
							}
						}

						if (toupate_ref != null && toupate_ref.methodscopeslot_ref_state == 2) //如果methodscope有其他引用，那么它的this也需更新为需要扫描状态
						{
							if (toupate_ref.This.ValueType == NaNBoxing.BoxType.HeapPtr)
							{
								var newthis = Context.GC.Heap[toupate_ref.This.HeapPtr];
								if (newthis.TypeKind == RtHeapTypeKind.INSTANCE)
								{
									if (((RtPayloadInstance)newthis.facility).methodscopeslot_ref_state == 1)
									{
										((RtPayloadInstance)newthis.facility).methodscopeslot_ref_state = 2;
									}
								}
								else if (newthis.TypeKind == RtHeapTypeKind.CLOSURE)
								{
									if (((RtPayloadClosure)newthis.facility).methodscopeslot_ref_state == 1)
									{
										((RtPayloadClosure)newthis.facility).methodscopeslot_ref_state = 2;
									}
								}
							}
						}

						return copyed_ptr;
					}


				}
				else
				{
					return ptr;
				}

			}
			else if (oldObj.TypeKind == RtHeapTypeKind.ARRAY)
			{
				if (ptr == heap.StackPos + heapLocater.MemberIndex + Context.CacheArrayPtr)
				{
#if DEBUG
					if (((RtPayloadArray)oldObj.facility).StoreMode != RtPayloadArray.ArrayStoreMode.cache)
					{
						throw new InvalidOperationException();
					}
#endif
					if (((RtPayloadArray)oldObj.facility).methodscopeslot_ref_state != 2)
					{
						return ptr;
					}
					else
					{
						Debug.Assert(m_scope != null && method_scopes != null);

						//更新数组的引用
						RtPayloadArray oldPayload;
						ptr = RtPayloadArray.FindAndUpdateHeapInstancePtr(ptr, this, out oldPayload); //更新最终指向的目标
						int copyed_ptr = 0;
						if (!(ptr < Context.CacheArrayPtr + Context.STACK_LENGTH)) //堆里的对象,无需拷贝
						{
							copyed_ptr = ptr;
						}

						RtPayloadArray toupdateref = null; //追踪对新拷贝对象的引用


						//							int* __scope = m_scope;
						//							do
						//							{
						//								--__scope;
						//								RtPayloadMethodScope scope = (RtPayloadMethodScope)Context.GC.Heap[*__scope].facility;
						//								for (int i = 0; i < scope.SlotCount ; ++i)
						//								{
						//									if (!(scope == heap && i == heapLocater.MemberIndex))
						//									{
						//										var v = scope.ReadSlot((ushort)i, this);
						//										if (v.ValueType == NaNBoxing.BoxType.HeapPtr)
						//										{
						//											var inmember = Context.GC.Heap[v.HeapPtr];
						//											if (inmember.TypeKind == RtHeapTypeKind.ARRAY)
						//											{
						//												RtPayloadArray _temp;
						//												if (v.HeapPtr == ptr || RtPayloadArray.FindAndUpdateHeapInstancePtr(v.HeapPtr, this, out _temp) == ptr)
						//												{
						//													if (copyed_ptr == 0)
						//													{
						//														copyed_ptr = scope.StackPos + i + Context.CacheArrayPtr;
						//#if DEBUG
						//														if (copyed_ptr == ptr)
						//														{
						//															throw new InvalidOperationException();
						//														}
						//#endif

						//														var dst = Context.GC.Heap[copyed_ptr];

						//														dst.Type = Context.ARRAY.Instance;
						//														((RtPayloadArray)dst.facility).HEAPINSTANCE_PTR = 0;
						//														((RtPayloadArray)dst.facility).methodscopeslot_ref_state = 1;
						//														((RtPayloadArray)dst.facility).CopyCacheFrom(oldPayload, this);

						//														((RtPayloadArray)oldObj.facility).HEAPINSTANCE_PTR = copyed_ptr;

						//														v.SetHeapPtr(copyed_ptr);
						//														scope.SetSlot(v, (ushort)i);

						//														toupdateref = (RtPayloadArray)dst.facility;

						//													}
						//													else
						//													{
						//														if (toupdateref != null)
						//														{
						//#if DEBUG
						//															if (toupdateref.methodscopeslot_ref_state != 1)
						//															{
						//																throw new InvalidOperationException();
						//															}
						//#endif
						//															toupdateref.methodscopeslot_ref_state = 2;
						//														}

						//														//更新引用
						//														v.SetHeapPtr(copyed_ptr);
						//														scope.SetSlot(v, (ushort)i);
						//													}
						//												}
						//											}
						//										}
						//									}
						//								}

						//							} while (__scope != method_scopes);

						for (int k = min; k <= max; k++)
						{
							RtPayloadMethodScope scope = (RtPayloadMethodScope)Context.GC.Heap[k].facility;
							for (int i = 0; i < scope.SlotCount; ++i)
							{
								if (!(scope == heap && i == heapLocater.MemberIndex))
								{
									var v = scope.ReadSlot((ushort)i, this);
									if (v.ValueType == NaNBoxing.BoxType.HeapPtr)
									{
										var inmember = Context.GC.Heap[v.HeapPtr];
										if (inmember.TypeKind == RtHeapTypeKind.ARRAY)
										{
											RtPayloadArray _temp;
											if (v.HeapPtr == ptr || RtPayloadArray.FindAndUpdateHeapInstancePtr(v.HeapPtr, this, out _temp) == ptr)
											{
												if (copyed_ptr == 0)
												{
													copyed_ptr = scope.StackPos + i + Context.CacheArrayPtr;
#if DEBUG
													if (copyed_ptr == ptr)
													{
														throw new InvalidOperationException();
													}
#endif

													var dst = Context.GC.Heap[copyed_ptr];

													dst.Type = Context.ARRAY.Instance;
													((RtPayloadArray)dst.facility).HEAPINSTANCE_PTR = 0;
													((RtPayloadArray)dst.facility).methodscopeslot_ref_state = 1;
													((RtPayloadArray)dst.facility).CopyCacheFrom(oldPayload, this);

													((RtPayloadArray)oldObj.facility).HEAPINSTANCE_PTR = copyed_ptr;

													v.SetHeapPtr(copyed_ptr);
													scope.SetSlot(v, (ushort)i);

													toupdateref = (RtPayloadArray)dst.facility;

												}
												else
												{
													if (toupdateref != null)
													{
#if DEBUG
														if (toupdateref.methodscopeslot_ref_state != 1)
														{
															throw new InvalidOperationException();
														}
#endif
														toupdateref.methodscopeslot_ref_state = 2;
													}

													//更新引用
													v.SetHeapPtr(copyed_ptr);
													scope.SetSlot(v, (ushort)i);
												}
											}
										}
									}
								}
							}

							if (scope.ParentPtr == old.HeapPtr)
							{
								Debug.Assert(copyed_ptr != 0);
								scope.ParentPtr = copyed_ptr;
							}
						}

						return copyed_ptr;
					}
				}
				else
				{
					return ptr;
				}

			}
			else if (oldObj.TypeKind == RtHeapTypeKind.VECTOR)
			{
				if (ptr == heap.StackPos + heapLocater.MemberIndex + Context.CacheVectorPtr)
				{
					if (((RtPayloadVector)oldObj.facility).methodscopeslot_ref_state != 2)
					{
						return ptr;
					}
					else
					{
						Debug.Assert(m_scope != null && method_scopes != null);

						//更新数组的引用
						RtPayloadVector oldPayload;
						ptr = RtPayloadVector.FindAndUpdateHeapInstancePtr(ptr, this, out oldPayload);
						int copyed_ptr = 0;
						if (!(ptr < Context.CacheVectorPtr + Context.STACK_LENGTH))
						{
							copyed_ptr = ptr;
						}

						RtPayloadVector toupdateref = null; //追踪对新拷贝对象的引用


						//							int* __scope = m_scope;
						//							do
						//							{
						//								--__scope;
						//								RtPayloadMethodScope scope = (RtPayloadMethodScope)Context.GC.Heap[*__scope].facility;
						//								for (int i =0; i < scope.SlotCount; ++i)
						//								{
						//									if (!(scope == heap && i == heapLocater.MemberIndex))
						//									{
						//										var v = scope.ReadSlot((ushort)i, this);
						//										if (v.ValueType == NaNBoxing.BoxType.HeapPtr)
						//										{
						//											var inmember = Context.GC.Heap[v.HeapPtr];
						//											if (inmember.TypeKind == RtHeapTypeKind.VECTOR)
						//											{
						//												RtPayloadVector _temp;
						//												if (v.HeapPtr == ptr || RtPayloadVector.FindAndUpdateHeapInstancePtr(v.HeapPtr, this, out _temp) == ptr)
						//												{
						//													if (copyed_ptr == 0)
						//													{
						//														copyed_ptr = scope.StackPos + i + Context.CacheVectorPtr;
						//#if DEBUG
						//														if (copyed_ptr == ptr)
						//														{
						//															throw new InvalidOperationException();
						//														}
						//#endif

						//														var dst = Context.GC.Heap[copyed_ptr];

						//														dst.Type = oldObj.Type;
						//														((RtPayloadVector)dst.facility).HEAPINSTANCE_PTR = 0;
						//														((RtPayloadVector)dst.facility).methodscopeslot_ref_state = 1;
						//														((RtPayloadVector)dst.facility).CopyCacheFrom(oldPayload, this);

						//														((RtPayloadVector)oldObj.facility).HEAPINSTANCE_PTR = copyed_ptr;

						//														v.SetHeapPtr(copyed_ptr);
						//														scope.SetSlot(v, (ushort)i);

						//														toupdateref = (RtPayloadVector)dst.facility;

						//													}
						//													else
						//													{
						//														if (toupdateref != null)
						//														{
						//#if DEBUG
						//															if (toupdateref.methodscopeslot_ref_state != 1)
						//															{
						//																throw new InvalidOperationException();
						//															}
						//#endif
						//															toupdateref.methodscopeslot_ref_state = 2;
						//														}

						//														//更新引用
						//														v.SetHeapPtr(copyed_ptr);
						//														scope.SetSlot(v, (ushort)i);
						//													}
						//												}

						//											}
						//										}
						//									}
						//								}



						//							} while (__scope != method_scopes);


						for (int k = min; k <= max; k++)
						{

							RtPayloadMethodScope scope = (RtPayloadMethodScope)Context.GC.Heap[k].facility;
							for (int i = 0; i < scope.SlotCount; ++i)
							{
								if (!(scope == heap && i == heapLocater.MemberIndex))
								{
									var v = scope.ReadSlot((ushort)i, this);
									if (v.ValueType == NaNBoxing.BoxType.HeapPtr)
									{
										var inmember = Context.GC.Heap[v.HeapPtr];
										if (inmember.TypeKind == RtHeapTypeKind.VECTOR)
										{
											RtPayloadVector _temp;
											if (v.HeapPtr == ptr || RtPayloadVector.FindAndUpdateHeapInstancePtr(v.HeapPtr, this, out _temp) == ptr)
											{
												if (copyed_ptr == 0)
												{
													copyed_ptr = scope.StackPos + i + Context.CacheVectorPtr;
#if DEBUG
													if (copyed_ptr == ptr)
													{
														throw new InvalidOperationException();
													}
#endif

													var dst = Context.GC.Heap[copyed_ptr];

													dst.Type = oldObj.Type;
													((RtPayloadVector)dst.facility).HEAPINSTANCE_PTR = 0;
													((RtPayloadVector)dst.facility).methodscopeslot_ref_state = 1;
													((RtPayloadVector)dst.facility).CopyCacheFrom(oldPayload, this);

													((RtPayloadVector)oldObj.facility).HEAPINSTANCE_PTR = copyed_ptr;

													v.SetHeapPtr(copyed_ptr);
													scope.SetSlot(v, (ushort)i);

													toupdateref = (RtPayloadVector)dst.facility;

												}
												else
												{
													if (toupdateref != null)
													{
#if DEBUG
														if (toupdateref.methodscopeslot_ref_state != 1)
														{
															throw new InvalidOperationException();
														}
#endif
														toupdateref.methodscopeslot_ref_state = 2;
													}

													//更新引用
													v.SetHeapPtr(copyed_ptr);
													scope.SetSlot(v, (ushort)i);
												}
											}

										}
									}
								}
							}


							if (scope.ParentPtr == old.HeapPtr)
							{
								Debug.Assert(copyed_ptr != 0);
								scope.ParentPtr = copyed_ptr;
							}

						}


						return copyed_ptr;
					}
				}
				else
				{
					return ptr;
				}
			}

#if DEBUG
			else if (oldObj.TypeKind == RtHeapTypeKind.MethodScope)
			{
				throw new InvalidOperationException();
			}
#endif
			else
			{
				//pass
				return ptr;
			}
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private void prepare_savemethodscope_saveinstacne(RtPayloadMethodScope heap, ref NaNBoxing saveSlot, RtHeapInstance src, int srcPtr, ref ScopeHeapLocater heapLocater,bool is_prepare_arg)
		{
			if (((ASInstance)src.Type).Flags.HasFlag(ClassFlags.Struct)
						&&
						(!is_prepare_arg  // 传参时，假设结构体也是传引用。	
							||
							((RtPayloadInstance)src.facility).IsRefVectorOrFromArrayOrStruct(this, (ASInstance)src.Type)

						//但是对结构体内部的引用或Vector内部的结构体 ,或者刚从数组里取出的结构体是例外，
						//类似C#处理，
						//引用不能通过参数传进去，直接复制结构体 
						)                                                        


						)
			{
				//Clone结构体
				int clonedptr = heapLocater.MemberIndex + heap.StackPos + Context.CacheInstancePtr;
				var cacheObj = Context.GC.Heap[clonedptr];
				cacheObj.Type = src.Type;

				((RtPayloadInstance)cacheObj.facility).methodscopeslot_ref_state = 1;
				((RtPayloadInstance)cacheObj.facility).HEAPINSTANCE_PTR = 0;
				((RtPayloadInstance)cacheObj.facility).CopyFrom(src, this, src.Type._link_codescope.TypeLayout.Size);

				saveSlot.SetHeapPtr(clonedptr);

				if (srcPtr == heap.ParentPtr)
				{
					heap.ParentPtr = clonedptr;
				}

			}
			else if (!(srcPtr < Context.CacheInstancePtr + Context.STACK_LENGTH))
			{
				//堆中的对象，不管它直接存
			}
			else
			{
				//先追踪到最终的 HEAPINSTANCE_PTR.
				RtPayloadInstance srcPayload;
				int src_ptr = RtPayloadInstance.FindAndUpdateHeapInstancePtr(srcPtr, this, out srcPayload);
				//如果在堆里，直接存
				if (!(src_ptr < Context.CacheInstancePtr + Context.STACK_LENGTH))
				{
					saveSlot.SetHeapPtr(src_ptr);
				}
				else if (src_ptr < heap.StackPos + heap.SlotCount + Context.CacheInstancePtr)
				{
					//定义在上一层调用栈的对象，或者存在本层的变量里的对象,不去管他直接存
					saveSlot.SetHeapPtr(src_ptr);

					if (srcPayload.methodscopeslot_ref_state == 1) //如果==0说明是类似 A(new object()) 这样的没有保存到变量的对象
					{
#if DEBUG
									if (srcPayload.HEAPINSTANCE_PTR != 0) //说明这是一个结构体对Vector,或者父布局Struct引用, 它们不可能有变量引用
									{
										throw new InvalidOperationException();
									}
#endif

						//说明缓存对象被引用了。
						srcPayload.methodscopeslot_ref_state = 2;
					}

				}
				else
				{
					//否则，缓存对象复制到要存入的slot的缓存池里，然后将目标slot指向它的缓存池。最后，将原对象也设置成payload指向目标slot的缓存池。
					int dstptr = heapLocater.MemberIndex + heap.StackPos + Context.CacheInstancePtr;
					var dstObj = Context.GC.Heap[dstptr];

					dstObj.Type = src.Type;
					((RtPayloadInstance)dstObj.facility).methodscopeslot_ref_state = 1;
					((RtPayloadInstance)dstObj.facility).HEAPINSTANCE_PTR = 0;
					((RtPayloadInstance)dstObj.facility).CopyFrom(srcPayload, (ASInstance)dstObj.Type, this, src.Type._link_codescope.TypeLayout.Size);

					srcPayload.HEAPINSTANCE_PTR = dstptr;
					saveSlot.SetHeapPtr(dstptr);

				}

			}
		}


		/// <summary>
		///准备保存到methodscope.需要考虑各种缓存的情况
		///如果methodscope是一个stackslot上的缓存：
		///{
		/// 保存前，先处理被覆盖前原来的内容：
		/// 如果当前槽 记为 [A] 是heapptr，并且指向了slot对应的缓存池 
		/// 需要从当前methodscope出发，扫描所有同样指向这个heapptr的对象的引用,加入集合[S]，找到离这个槽最近的一个槽。记为[B]
		/// 如果找到[B],需要把[A] 指向的缓存对象复制到[B]Slot的缓存池里，然后更新[S]中（也包含B）所有槽指向[B]缓存池。
		/// 如果更新的对象是一个Closure,那么需要注意如果闭包的ScopePtr==This,那么当This更新时ScopePtr也要更新。
		/// 
		/// 
		/// 
		/// 然后处理要保存的内容：
		/// 如果要保存的内容，不是缓存对象，直接存入 细节：需要先追踪要存入对象的HEAPINSTANCE_PTR,看是否已经保存到堆中。
		/// 现在一定是缓存对象,看他缓存池的位置是否包含在要保存的methodscope的Slot缓存里，或者之前。如果是，直接存入
		/// 否则将缓存对象复制到要存入的slot的缓存池里，然后将目标slot指向它的缓存池。最后，将原对象也设置成payload指向目标slot的缓存池。
		/// 
		/// 
		/// 如果保存的对象是一个Closure,那么需要注意如果闭包的ScopePtr==This,那么当This更新时ScopePtr也要更新。
		/// 
		/// }
		///否则，复制到堆。
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private unsafe void PrepareSaveMethodScope(RtPayloadMethodScope heap, ref ScopeHeapLocater heapLocater, ref NaNBoxing value, int* m_scope, int* method_scopes, ref ReceiveError error , bool is_prepare_arg = false)
		{
			if (heap.IsStackSlot)
			{
				NaNBoxing old = heap.ReadSlot(heapLocater.MemberIndex
//#if FORCOMPILER
				, this
//#endif
					);

				if (value.Raw == old.Raw)
				{
					return;
				}

				if (old.ValueType == NaNBoxing.BoxType.HeapPtr)
				{
					prepare_savemethodscope_beforeSave( heap ,old,  heapLocater, m_scope,method_scopes);
				}

				//存储阶段
				if (value.ValueType == NaNBoxing.BoxType.HeapPtr)
				{

					var obj = Context.GC.Heap[value.HeapPtr];
					if (obj.TypeKind == RtHeapTypeKind.INSTANCE)
					{
						prepare_savemethodscope_saveinstacne(heap,ref value, obj, value.HeapPtr, ref heapLocater,is_prepare_arg);
					}
					else if (obj.TypeKind == RtHeapTypeKind.STRING)
					{
						//pass
					}
					else if (obj.TypeKind == RtHeapTypeKind.ARRAY)
					{
						RtPayloadArray array;
						int array_ptr = RtPayloadArray.FindAndUpdateHeapInstancePtr(value.HeapPtr, this, out array);

						if (array.StoreMode == RtPayloadArray.ArrayStoreMode.cache_on_stack)
						{
							if (array.stack_store_startindex < heap.StackPos + heap.SlotCount)
							{
								//pass
								value.SetHeapPtr(array_ptr);
							}
							else
							{
								value = GetSaveValue(value, ref error);
								if (error.raised)
								{
									return;
								}
								//throw new InvalidOperationException();
							}
						}
						else if (array.StoreMode == RtPayloadArray.ArrayStoreMode.cache)
						{
							if (array_ptr < heap.StackPos + heap.SlotCount + Context.CacheArrayPtr)
							{
								//定义在上一层调用栈的对象，或者存在本层的变量里的对象,直接存，需要更新引用状态。
								value.SetHeapPtr(array_ptr);
								if (array.methodscopeslot_ref_state == 1)
								{
									array.methodscopeslot_ref_state = 2;
								}
							}
							else
							{
								//否则，缓存对象复制到要存入的slot的缓存池里，然后将目标slot指向它的缓存池。最后，将原对象也设置成payload指向目标slot的缓存池。
								int dstptr = heapLocater.MemberIndex + heap.StackPos + Context.CacheArrayPtr;
								var dstObj = Context.GC.Heap[dstptr];
								dstObj.Type = Context.ARRAY.Instance;

								((RtPayloadArray)dstObj.facility).methodscopeslot_ref_state = 1;
								((RtPayloadArray)dstObj.facility).HEAPINSTANCE_PTR = 0;
								((RtPayloadArray)dstObj.facility).CopyCacheFrom(array, this);

								array.HEAPINSTANCE_PTR = dstptr;
								value.SetHeapPtr(dstptr);

							}
						}
						else
						{
							//pass.
							value.SetHeapPtr(array_ptr);
						}
					}
					else if (obj.TypeKind == RtHeapTypeKind.VECTOR)
					{
						RtPayloadVector vector;
						int vector_ptr = RtPayloadVector.FindAndUpdateHeapInstancePtr(value.HeapPtr, this, out vector);

						if (!(vector_ptr < Context.CacheVectorPtr + Context.STACK_LENGTH))
						{
							//pass
							value.SetHeapPtr(vector_ptr);
						}
						else if (vector_ptr < heap.StackPos + heap.SlotCount + Context.CacheVectorPtr)
						{
							//定义在上一层调用栈的对象，或者存在本层的变量里的对象,直接存，需要更新引用状态。
							value.SetHeapPtr(vector_ptr);
							if (vector.methodscopeslot_ref_state == 1)
							{
								vector.methodscopeslot_ref_state = 2;
							}
						}
						else
						{
							//否则，缓存对象复制到要存入的slot的缓存池里，然后将目标slot指向它的缓存池。最后，将原对象也设置成payload指向目标slot的缓存池。
							int dstptr = heapLocater.MemberIndex + heap.StackPos + Context.CacheVectorPtr;
							var dstObj = Context.GC.Heap[dstptr];
							dstObj.Type = obj.Type;


							((RtPayloadVector)dstObj.facility).methodscopeslot_ref_state = 1;
							((RtPayloadVector)dstObj.facility).HEAPINSTANCE_PTR = 0;
							((RtPayloadVector)dstObj.facility).CopyCacheFrom(vector, this);

							vector.HEAPINSTANCE_PTR = dstptr;
							value.SetHeapPtr(dstptr);

						}
					}
					else if (obj.TypeKind == RtHeapTypeKind.CLOSURE)
					{
						var srcClosure = (RtPayloadClosure)obj.facility;
						int final_ptr = RtPayloadClosure.FindAndUpdateHeapInstancePtr(value.HeapPtr, this, out srcClosure);

						if (!(final_ptr < Context.M_ClosurePtr + Context.STACK_LENGTH))
						{
							//它已经在堆里了。
							value.SetHeapPtr(final_ptr);
						}
						else if (value.HeapPtr < heap.StackPos + heap.SlotCount + Context.M_ClosurePtr)
						{

							if (srcClosure.methodscopeslot_ref_state == 1)
							{
								srcClosure.methodscopeslot_ref_state = 2;
							}

							//此处只需更新This指针的引用情况
							if (srcClosure.This.ValueType == NaNBoxing.BoxType.HeapPtr)
							{
								var _this = Context.GC.Heap[srcClosure.This.HeapPtr];
								if (_this.TypeKind == RtHeapTypeKind.INSTANCE)
								{
									if (((RtPayloadInstance)_this.facility).methodscopeslot_ref_state == 1)
									{
										((RtPayloadInstance)_this.facility).methodscopeslot_ref_state = 2;
									}
								}
#if DEBUG
								else if (_this.TypeKind == RtHeapTypeKind.CLOSURE)
								{
									if (srcClosure.This.HeapPtr < Context.M_ClosurePtr + Context.STACK_LENGTH)
									{
										throw new InvalidOperationException();
									}
								}
								else if (_this.TypeKind == RtHeapTypeKind.ARRAY)
								{

									if (((RtPayloadArray)_this.facility).StoreMode != RtPayloadArray.ArrayStoreMode.normal)
									{
										throw new InvalidOperationException();
									}
								}
								else if (_this.TypeKind == RtHeapTypeKind.VECTOR)
								{
									throw new InvalidOperationException();
								}

								else if (_this.TypeKind == RtHeapTypeKind.MethodScope)
								{
									throw new InvalidOperationException();
								}
#endif
								else
								{
									//pass
								}
							}

						}
						else
						{

							int dstClosurePtr = heapLocater.MemberIndex + heap.StackPos + Context.M_ClosurePtr;
							var dstClosure = (RtPayloadClosure)Context.GC.Heap[dstClosurePtr].facility;

							Context.GC.Heap[dstClosurePtr].Type = obj.Type;

							dstClosure.CopyDataFrom(srcClosure, this);
							dstClosure.methodscopeslot_ref_state = 1;
							dstClosure.HEAPINSTANCE_PTR = srcClosure.HEAPINSTANCE_PTR;

							srcClosure.HEAPINSTANCE_PTR = dstClosurePtr;

							//处理 This 指针
							if (dstClosure.This.ValueType == NaNBoxing.BoxType.HeapPtr)
							{
								bool needupdatescopePtr = dstClosure.ScopePtr == dstClosure.This.HeapPtr;

								var _this = Context.GC.Heap[dstClosure.This.HeapPtr];
								if (_this.TypeKind == RtHeapTypeKind.INSTANCE)
								{
									if (old.ValueType == NaNBoxing.BoxType.HeapPtr)
									{
										prepare_savemethodscope_beforeSave(heap, old, heapLocater, m_scope, method_scopes);
									}

									prepare_savemethodscope_saveinstacne( heap ,ref dstClosure.This, _this, dstClosure.This.HeapPtr, ref heapLocater,is_prepare_arg);

									if (needupdatescopePtr)
									{
										dstClosure.ScopePtr = dstClosure.This.HeapPtr;
									}
								}
								else if (_this.TypeKind == RtHeapTypeKind.CLOSURE)
								{
									/* 只有apply或者call可能造成这种情况。
									* var a:Function= function ( ...rest ):void 
									*	{
									*	};
									*	var f1 = a.apply;
									*/

									//这里就只能分配到堆里了。
									var s_this = GetSaveValue(dstClosure.This, ref error);
									if (error.raised)
									{
										return;
									}

									dstClosure.This = s_this;
									if (needupdatescopePtr)
									{
										dstClosure.ScopePtr = dstClosure.This.HeapPtr;
									}
								}
								else if (_this.TypeKind == RtHeapTypeKind.ARRAY)
								{
									/*
									 *	var a = new Array(1, 2,  3 );
									 *	var b = a.join;
									 */
									//这种代码不去管了，直接分配到堆里了事。
									var a_this = GetSaveValue(dstClosure.This, ref error);
									if (error.raised)
									{
										return;
									}
									dstClosure.This = a_this;

									if (needupdatescopePtr)
									{
										dstClosure.ScopePtr = dstClosure.This.HeapPtr;
									}

								}
								else if (_this.TypeKind == RtHeapTypeKind.VECTOR)
								{
									var v_this = GetSaveValue(dstClosure.This, ref error);
									if (error.raised)
									{
										return;
									}
									dstClosure.This = v_this;

									if (needupdatescopePtr)
									{
										dstClosure.ScopePtr = dstClosure.This.HeapPtr;
									}

									//throw new NotImplementedException();
								}
#if DEBUG
								else if (_this.TypeKind == RtHeapTypeKind.MethodScope)
								{
									throw new InvalidOperationException();
								}
#endif
								else
								{
									//pass
								}
							}


							{
								//处理MethodScope
								if (!((ASMethodBody)obj.Type).Method.__ismethod)
								{
									if (dstClosure.ScopePtr != 0)
									{
										int sptr = dstClosure.ScopePtr;

										RtPayloadMethodScope last_scope = null;

									lbl_parent:
										var scope = Context.GC.Heap[sptr];
										if (scope.TypeKind == RtHeapTypeKind.GLOBAL || scope.TypeKind == RtHeapTypeKind.CLASS || scope.TypeKind == RtHeapTypeKind.INSTANCE)
										{
											// Y组合子等：先遇到 global/class/instance，未遇到 heap。与 StoreReturnSlot 第 597 行一致。
											if (last_scope != null)
												last_scope.ParentPtr = sptr;
											else
												dstClosure.ScopePtr = sptr;
										}
										else if (scope.facility != heap)
										{
#if DEBUG
											if (scope.TypeKind != RtHeapTypeKind.MethodScope)
											{
												throw new InvalidOperationException();
											}
#endif
											if (sptr < Context.M_ClosurePtr + Context.STACK_LENGTH)
											{

												if (((ASMethodBody)scope.Type).Method.Flags.HasFlag(MethodFlags.NeedActivation))
												{
													RtPayloadMethodScope cacheMscope = (RtPayloadMethodScope)scope.facility;
													var cacheSpan = cacheMscope.__get_slots_for_gc;

													RtHeapInstance heapObj;
													int ptr = Context.GC.AllocMethodScope(new NaNBoxing[cacheSpan.Length], 0, scope.Type._link_codescope);
													if (ptr == 0)
													{
														//这种情况应该为致命错误，就不要再catch了
														RaiseFault(ref error);
														return;
													}

													cacheMscope.cloneout_ptr = ptr;

													heapObj = Context.GC.Heap[ptr];
													heapObj.Type = scope.Type;

													RtPayloadMethodScope heap_scope = (RtPayloadMethodScope)heapObj.facility;
													for (int i = 0; i < cacheSpan.Length; i++)
													{
														var oldSpanValue = cacheSpan[i];

														NaNBoxing slotV = GetSaveValue(oldSpanValue, ref error);
														if (error.raised)
														{
															cacheMscope.cloneout_ptr = 0;
															return;
														}

														heap_scope.SetSlot(slotV, (ushort)i);
													}
													cacheMscope.ChangeStore(heap_scope);
													//cacheMscope.cloneing_ptr = 0;

													if (last_scope != null)
													{
														last_scope.ParentPtr = ptr;
													}
													else
													{
														dstClosure.ScopePtr = ptr;
													}

													last_scope = heap_scope;

													sptr = ((RtPayloadMethodScope)scope.facility).ParentPtr;
												}
												else
												{
													sptr = ((RtPayloadMethodScope)scope.facility).ParentPtr;
												}
											}
											else
											{
												last_scope = (RtPayloadMethodScope)scope.facility;
												sptr = last_scope.ParentPtr;
											}

											goto lbl_parent;
										}
										else
										{
											if (last_scope != null)
											{
												last_scope.ParentPtr = sptr;
											}
										}
									}


								}

							}
							//将srcClosure覆盖为新的
							srcClosure.This = dstClosure.This;
							srcClosure.ScopePtr = dstClosure.ScopePtr;

							value.SetHeapPtr(dstClosurePtr);
						}

					}
					else
					{
						value = GetSaveValue(value, ref error);
					}
				}
			}
			else
			{
				//完全相同结构体可以不分配内存，就地覆盖
				NaNBoxing old = heap.__get_slots_for_gc[heapLocater.MemberIndex];
				if (CopyIfSameTypeStructAndReplaceSrc(old, ref value))
				{

				}
				else
				{
					value = GetSaveValue(value, ref error);
				}
			}

		}


		internal bool CopyIfSameTypeStructAndReplaceSrc(NaNBoxing dst,ref NaNBoxing src)
		{
			if (dst.Raw == src.Raw)
				return true;

			if (dst.ValueType == NaNBoxing.BoxType.HeapPtr && src.ValueType == NaNBoxing.BoxType.HeapPtr)
			{
				var oldv = Context.GC.Heap[dst.HeapPtr];
				var newv = Context.GC.Heap[src.HeapPtr];

				if (oldv.TypeKind == newv.TypeKind && oldv.TypeKind == RtHeapTypeKind.INSTANCE)
				{
					if (((ASInstance)oldv.Type).Flags.HasFlag(ClassFlags.Struct))
					{
						((RtPayloadInstance)oldv.facility).CopyFrom(newv, Context.player, oldv.Type._link_codescope.TypeLayout.Size);
						src.SetHeapPtr(dst.HeapPtr);
						return true;
					}
				}
				
			}

			return false;
		}



	}
}
