using juicescript.ABC;
using juicescript.ABC.Locaters;
using juicescript.runtime.buildin;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Security.Cryptography;

namespace juicescript.runtime
{
#if FORCOMPILER
	internal partial class Player
#else
	public partial class Player
#endif
	{
		internal struct refbynextframe
		{
			public uint version;
			public int scope_ptr;
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void GetSaveValue_Instance( ref NaNBoxing value, ref ReceiveError error)
		{
			Debug.Assert(value.ValueType == NaNBoxing.BoxType.HeapPtr);

			RtInstance src_obj;
			var src_ptr = RtInstance.FindAndUpdateHeapInstancePtr(value.HeapPtr, this, out src_obj); //查找最终指向的目标
			if (src_ptr < Context.CacheInstancePtr + Context.STACK_LENGTH)
			{

				RtHeapBase heapObj;
				int ptr = Context.GC.AllocInstance((ASInstance)src_obj.Type, out heapObj);
				if (ptr == 0)
				{
					//这种情况应该为致命错误，就不要再catch了
					RaiseFault(ref error);
					return;
				}


				((RtInstance)heapObj).CopyFrom(src_obj, (ASInstance)heapObj.Type, this, src_obj.Type._link_codescope.TypeLayout.Size);
				//target.HEAPINSTANCE_PTR = ptr;
				src_obj.LinkTo( (RtInstance)heapObj ,ptr);

				value.SetHeapPtr(ptr, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.NONE);

			}
			else
			{
				value.SetHeapPtr(src_ptr, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.NONE);
			}
		}



		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void GetSaveValue_InstanceType(ref NaNBoxing value, ref ReceiveError error)
		{
			Debug.Assert(value.ValueType == NaNBoxing.BoxType.HeapPtr);


			//RtHeapBase instance = Context.GC.Heap[value.HeapPtr];
			if ( value.IsStruct() )//((ASInstance)instance.Type).Flags.HasFlag(ClassFlags.Struct))
			{
				RtHeapBase instance = Context.GC.Heap[value.HeapPtr];
				//结构体，必要复制一份。
				RtHeapBase heapObj;
				int ptr = Context.GC.AllocInstance((ASInstance)instance.Type, out heapObj);
				if (ptr == 0)
				{
					//这种情况应该为致命错误，就不要再catch了
					RaiseFault(ref error);
					return;
				}
				((RtInstance)heapObj).CopyFrom(instance, this, instance.Type._link_codescope.TypeLayout.Size);
				value.SetHeapPtr(ptr, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.FLAG_STRUCT);
			}
			else if (value.HeapPtr < Context.CacheInstancePtr + Context.STACK_LENGTH)//((RtPayloadInstance)instance).isCache)
			{
				//RtHeapBase instance = Context.GC.Heap[value.HeapPtr];
				GetSaveValue_Instance(ref value, ref error);
			}
		}


		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private void DoGetSaveValue(RtHeapTypeKind htype, ref NaNBoxing value, ref ReceiveError error)
		{

			switch (htype)
			{
				case RtHeapTypeKind.CLASS:
				case RtHeapTypeKind.GLOBAL:
				case RtHeapTypeKind.STRING:
				case RtHeapTypeKind.DYNAMIC_PROPERTYS:
				case RtHeapTypeKind.NAMESPACE:
					break;
				case RtHeapTypeKind.INSTANCE:
					{
						GetSaveValue_InstanceType(ref value, ref error);
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
							RtHeapBase instance = Context.GC.Heap[value.HeapPtr];
							RtClosure cache = (RtClosure)instance;
							var src_ptr = RtClosure.FindAndUpdateHeapInstancePtr(value.HeapPtr, this, out cache);

							if (src_ptr < Context.M_ClosurePtr + Context.STACK_LENGTH)
							{

								if (cache.cloneing_ptr != 0)
								{
									value.SetHeapPtr(cache.cloneing_ptr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);
									break;
								}



								RtHeapBase heapObj;
								int ptr = Context.GC.AllocClosure(((ASMethodBody)instance.Type).Method);
								if (ptr == 0)
								{
									//这种情况应该为致命错误，就不要再catch了
									RaiseFault(ref error);
									return ;
								}

								cache.cloneing_ptr = ptr;

								heapObj = Context.GC.Heap[ptr];
								RtClosure closure = (RtClosure)heapObj;

								closure.CopyDataFrom(cache, this);

								//原cache的堆对象追踪到堆指针上
								//cache.HEAPINSTANCE_PTR = ptr;
								cache.LinkTo( closure, ptr);

								//将缓存的MethodScope生成到堆里
								int scope_p = cache.ScopePtr;
								if (scope_p != 0)
								{
									NaNBoxing s = new NaNBoxing();
									s.SetHeapPtr(scope_p, NaNBoxing.UNKNOWN_HEAPKIND, (byte)HeapKindFlag.NONE);//反正会马上递归
									s = GetSaveValue(s, ref error);
									if (error.raised)
									{
										cache.cloneing_ptr = 0;
										value = default;
										return;
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

								value.SetHeapPtr(ptr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);


							}
							else
							{
								value.SetHeapPtr(src_ptr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);
							}
						}

					}
					break;
				case RtHeapTypeKind.MethodScope:
					{
						if (value.HeapPtr < Context.M_MethodScopePtr + Context.MAX_BACKTRACE)
						{
							RtHeapBase instance = Context.GC.Heap[value.HeapPtr];
							if (!((ASMethodBody)instance.Type).Method.Flags.HasFlag(MethodFlags.NeedActivation))
							{
								//不被引用的method,跳过
								RtMethodScope scope = (RtMethodScope)instance;
								if (scope.ParentPtr == 0)
								{
									value.SetHeapPtr(0, (byte)RtHeapTypeKind.MethodScope, (byte)HeapKindFlag.NONE);
								}
								else
								{
									NaNBoxing p = new NaNBoxing();
									p.SetHeapPtr(scope.ParentPtr, NaNBoxing.UNKNOWN_HEAPKIND, (byte)HeapKindFlag.NONE);//反正马上递归
									p = GetSaveValue(p, ref error);
									if (error.raised)
									{
										value = default;
										return ;
									}

									value = p; //.SetHeapPtr(p.HeapPtr);

								}

							}
							else
							{
								RtMethodScope cacheMscope = (RtMethodScope)instance;
								if (cacheMscope.cloneout_ptr != 0)
								{
									value.SetHeapPtr(cacheMscope.cloneout_ptr, (byte)RtHeapTypeKind.MethodScope, (byte)HeapKindFlag.NONE);
									break;
								}

								var cacheSpan = cacheMscope.__get_slots_internal;

								RtHeapBase heapObj;
								int ptr = Context.GC.AllocMethodScope(new NaNBoxing[cacheSpan.Length], 0, instance.Type._link_codescope);
								if (ptr == 0)
								{
									//这种情况应该为致命错误，就不要再catch了
									RaiseFault(ref error);
									value = default;
									return;
								}

								cacheMscope.cloneout_ptr = ptr;

								heapObj = Context.GC.Heap[ptr];
								heapObj.Type = instance.Type;

								RtMethodScope heap_scope = (RtMethodScope)heapObj;

								for (int i = 0; i < cacheSpan.Length; i++)
								{
									var oldSpanValue = cacheSpan[i];
									//cacheSpan[i].SetUndefined();//原值删除 最后会整体替代到新的堆的值。

									NaNBoxing slotV = GetSaveValue(oldSpanValue, ref error);
									if (error.raised)
									{
										cacheMscope.cloneout_ptr = 0;
										value = default;
										return;
									}

									//cacheSpan[i] = slotV;
									heap_scope.SetSlot(slotV, (ushort)i);
								}
								cacheMscope.ChangeStore(heap_scope);

								//cacheMscope.cloneing_ptr = 0;

								if (cacheMscope.ParentPtr != 0)
								{
									NaNBoxing p = new NaNBoxing();
									p.SetHeapPtr(cacheMscope.ParentPtr, NaNBoxing.UNKNOWN_HEAPKIND, (byte)HeapKindFlag.NONE);//马上递归
									p = GetSaveValue(p, ref error);
									if (error.raised)
									{
										value = default;
										return ;
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

								value.SetHeapPtr(ptr, (byte)RtHeapTypeKind.MethodScope, (byte)HeapKindFlag.NONE);
							}
						}
					}
					break;
				case RtHeapTypeKind.ARRAY:
					{
						RtArray arrStore;// = (RtPayloadArray)instance;
						int arr_ptr = RtArray.FindAndUpdateHeapInstancePtr(value.HeapPtr, this, out arrStore);

						if (arrStore.StoreMode == RtArray.ArrayStoreMode.normal)
						{
							value.SetHeapPtr(arr_ptr, (byte)RtHeapTypeKind.ARRAY, (byte)HeapKindFlag.NONE);
							break;
						}
						else if (arrStore.StoreMode == RtArray.ArrayStoreMode.cache_on_stack)
						{
							int arr_heap_ptr = arrStore.ChangeStoreToHeap(this, ref error);
							if (error.raised)
							{
								return;
							}
							value.SetHeapPtr(arr_heap_ptr, (byte)RtHeapTypeKind.ARRAY, (byte)HeapKindFlag.NONE);
						}
						else
						{
#if DEBUG
							if (arrStore.StoreMode != RtArray.ArrayStoreMode.cache)
							{
								throw new InvalidOperationException();
							}
#endif

							int arr_heap_ptr = arrStore.ChangeStoreToHeap(this, ref error);
							if (error.raised)
							{
								return;
							}
							value.SetHeapPtr(arr_heap_ptr, (byte)RtHeapTypeKind.ARRAY, (byte)HeapKindFlag.NONE);
						}
					}
					break;
				case RtHeapTypeKind.VECTOR:
					{
						RtVector vector;
						int vec_ptr = RtVector.FindAndUpdateHeapInstancePtr(value.HeapPtr, this, out vector);
						if (vec_ptr < Context.CacheVectorPtr + Context.STACK_LENGTH)
						{
							RtHeapBase instance = Context.GC.Heap[value.HeapPtr];
							vec_ptr = vector.ChangeStoreToHeap((ASInstance)instance.Type, this, ref error, out VectorImpl.VectorStore newstore);
							if (error.raised)
							{
								return;
							}

						}
						value.SetHeapPtr(vec_ptr, (byte)RtHeapTypeKind.VECTOR, (byte)HeapKindFlag.NONE);
					}

					break;
				case RtHeapTypeKind.SHAPE:
				case RtHeapTypeKind.STACK_CACHE_OBJ:
				default:
#if DEBUG
					throw new InvalidOperationException();
#else
						Environment.FailFast("出错了，这里跑不到"); return;
#endif
			}

		}

		/// <summary>
		/// 保存到堆前，如有缓存对象需要先复制到堆。
		/// 由于此操作可能会创建一个新对象，并且没有保存到栈里，所以凡是使用了此操作的指令，在使用新对象之前不能引发GC否则会出现意外。
		/// </summary>
		/// <param name="value"></param>
		/// <param name="error"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal NaNBoxing GetSaveValue(NaNBoxing value, ref ReceiveError error)
		{
			if (value.ValueType == NaNBoxing.BoxType.HeapPtr)
			{
				//RtHeapBase instance = Context.GC.Heap[value.HeapPtr];

				RtHeapTypeKind htype = (RtHeapTypeKind)value.HeapKind;
				if ((byte)htype == NaNBoxing.UNKNOWN_HEAPKIND)
				{ 
					RtHeapBase o = Context.GC.Heap[value.HeapPtr];

					htype = o.Kind;
					if (htype == RtHeapTypeKind.INSTANCE)
					{
						value.SetHeapPtr(value.HeapPtr, (byte)htype, (byte)(o.Kind == RtHeapTypeKind.INSTANCE ? (((ASInstance)o.Type).Flags.HasFlag(ClassFlags.Struct) ? HeapKindFlag.FLAG_STRUCT : HeapKindFlag.NONE) : HeapKindFlag.NONE));
					}
				}

				DoGetSaveValue(htype,ref value,ref error);
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
				
				if (value.HeapKind == (byte)RtHeapTypeKind.INSTANCE)
				{

					//if (!isyieldreturn_or_holderror && obj.Type == Context.GENERATOR.Instance)
					//{
					//	RaiseTypeError(ref error, value, TypeKind.Function);
					//}
					//else 

					if (value.IsStruct())
					{
						var obj = Context.GC.Heap[value.HeapPtr];
						//Clone结构体
						int clonedptr = returnSlotIndex + Context.CacheInstancePtr;
						var cacheObj = Context.GC.Heap[clonedptr];
						//cacheObj.Type = obj.Type;

						//((RtInstance)cacheObj).methodscopeslot_ref_state = 0;
						//((RtInstance)cacheObj).HEAPINSTANCE_PTR = 0;
						//((RtInstance)cacheObj).CopyFrom(obj, this, obj.Type._link_codescope.TypeLayout.Size);
						((RtInstance)cacheObj).CloneOther((RtInstance)obj, this);
						
						returnSlot.SetHeapPtr(clonedptr, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.FLAG_STRUCT);
					}
					else
					{
						if (!(value.HeapPtr < Context.CacheInstancePtr + Context.STACK_LENGTH))//堆里 --挡住后面的查询
						{
							returnSlot = value;
							Debug.Assert(RtInstance.FindAndUpdateHeapInstancePtr(value.HeapPtr, this, out RtInstance _temp) == returnSlot.HeapPtr);

						}
						else
						{
							//查找
							RtInstance obj;
							int t = RtInstance.FindAndUpdateHeapInstancePtr(value.HeapPtr, this, out obj);
							
							if (
								(t < Context.CacheInstancePtr + calleelastpos) //传入
								|| !(t < Context.CacheInstancePtr + Context.STACK_LENGTH) //实际就在堆里
							)
							{
								////if (((RtInstance)obj).IsRefVectorOrFromContainerOrRefStruct(this, (ASInstance)obj.Type))
								////if (((ASInstance)obj.Type).Flags.HasFlag(ClassFlags.Struct))
								//{
								//	RtInstance _temp;
								//	int t = RtInstance.FindAndUpdateHeapInstancePtr(value.HeapPtr, this, out _temp);
								//	returnSlot.SetHeapPtr(t, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.NONE);
								//}

								returnSlot.SetHeapPtr(t, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.NONE);
							}
							else
							{
								//var obj = Context.GC.Heap[value.HeapPtr];
								//if (((ASInstance)obj.Type).Flags.HasFlag(ClassFlags.Struct))
								//{
								//	//Clone结构体
								//	int clonedptr = returnSlotIndex + Context.CacheInstancePtr;
								//	var cacheObj = Context.GC.Heap[clonedptr];
								//	cacheObj.Type = obj.Type;

								//	((RtInstance)cacheObj).methodscopeslot_ref_state = 0;
								//	((RtInstance)cacheObj).HEAPINSTANCE_PTR = 0;
								//	((RtInstance)cacheObj).CopyFrom(obj, this, obj.Type._link_codescope.TypeLayout.Size);

								//	returnSlot.SetHeapPtr(clonedptr, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.FLAG_STRUCT);
								//}
								//else
								{

									int dstptr = returnSlotIndex + Context.CacheInstancePtr;
									var dstObj = Context.GC.Heap[dstptr];

									//dstObj.Type = obj.Type;
									//((RtInstance)dstObj).methodscopeslot_ref_state = 0;
									//((RtInstance)dstObj).HEAPINSTANCE_PTR = 0;
									//((RtInstance)dstObj).CopyFrom(obj, this, obj.Type._link_codescope.TypeLayout.Size);
									((RtInstance)dstObj).CloneOther((RtInstance)obj, this);

									obj.LinkTo((RtInstance)dstObj, dstptr);
									//((RtInstance)obj).HEAPINSTANCE_PTR = dstptr;
									returnSlot.SetHeapPtr(dstptr, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.NONE);
								}
							}


						}
					}
				}
				else if (value.HeapKind == (byte)RtHeapTypeKind.ARRAY)
				{
					if (!(value.HeapPtr < Context.CacheArrayPtr + Context.STACK_LENGTH)) //堆里
					{
						returnSlot = value;

#if DEBUG
						RtArray arr;
						int arr_ptr = RtArray.FindAndUpdateHeapInstancePtr(value.HeapPtr, this, out arr);
						Debug.Assert(arr.StoreMode == RtArray.ArrayStoreMode.normal);
						Debug.Assert(arr_ptr == returnSlot.HeapPtr);

#endif

					}
					else
					{
						RtArray arr;
						int arr_ptr = RtArray.FindAndUpdateHeapInstancePtr(value.HeapPtr, this, out arr);

						if (arr.StoreMode == RtArray.ArrayStoreMode.cache_on_stack)
						{
							var method = ((ASMethodBody)Context.GC.Heap[scope_ptr].Type).Method;
							int callee_slot_idx = stackStPos - method.Body._link_codescope.Members.Count - 2;

							if (arr.stack_store_startindex + arr.stack_store.Length + 1 < callee_slot_idx)
							{
								//传入的
								returnSlot.SetHeapPtr(arr_ptr, (byte)RtHeapTypeKind.ARRAY, (byte)HeapKindFlag.NONE);
							}
							else
							{
								value = GetSaveValue(value, ref error);
								if (error.raised)
								{
									return;
								}
								returnSlot.SetHeapPtr(value.HeapPtr, (byte)RtHeapTypeKind.ARRAY, (byte)HeapKindFlag.NONE);

							}
						}
						else if (arr.StoreMode == RtArray.ArrayStoreMode.cache)
						{
							if (arr_ptr < Context.CacheArrayPtr + calleelastpos) //传入
							{
								returnSlot.SetHeapPtr(arr_ptr, (byte)RtHeapTypeKind.ARRAY, (byte)HeapKindFlag.NONE);
							}
							else
							{
								var dstArrayPtr = returnSlotIndex + Context.CacheArrayPtr;
								var dst = (RtArray)Context.GC.Heap[dstArrayPtr];

								Context.GC.Heap[dstArrayPtr].Type = Context.ARRAY.Instance;
								dst.CopyCacheFrom(arr, this);
								//dst.HEAPINSTANCE_PTR = 0;

								dst.methodscopeslot_ref_state = 0;
								dst.nextframe_ref_state = default;

								//arr.HEAPINSTANCE_PTR = dstArrayPtr;
								arr.LinkTo(dst, dstArrayPtr);

								returnSlot.SetHeapPtr(dstArrayPtr, (byte)RtHeapTypeKind.ARRAY, (byte)HeapKindFlag.NONE);
							}

						}
						else
						{
							//必然是普通堆里的对象
							returnSlot.SetHeapPtr(arr_ptr, (byte)RtHeapTypeKind.ARRAY, (byte)HeapKindFlag.NONE);
						}
					}
				}
				else if (value.HeapKind == (byte)RtHeapTypeKind.VECTOR)
				{
					if (!(value.HeapPtr < Context.CacheVectorPtr + Context.STACK_LENGTH)
						) //堆里
					{
						returnSlot = value;

						Debug.Assert(RtVector.FindAndUpdateHeapInstancePtr(value.HeapPtr, this, out RtVector _temp) == returnSlot.HeapPtr);

						//returnSlot.SetHeapPtr(vec_ptr, (byte)RtHeapTypeKind.VECTOR, (byte)HeapKindFlag.NONE);
					}
					else
					{
						RtVector vec;
						int vec_ptr = RtVector.FindAndUpdateHeapInstancePtr(value.HeapPtr, this, out vec);
						if ((vec_ptr < Context.CacheVectorPtr + calleelastpos
							) // 传入
							||
							(!(vec_ptr < Context.CacheVectorPtr + Context.STACK_LENGTH)) //堆里
						 )
						{
							returnSlot.SetHeapPtr(vec_ptr, (byte)RtHeapTypeKind.VECTOR, (byte)HeapKindFlag.NONE);
						}
						else
						{
							var dstVecPtr = returnSlotIndex + Context.CacheVectorPtr;
							var dstObj = Context.GC.Heap[dstVecPtr];
							dstObj.Type = Context.GC.Heap[vec_ptr].Type;
							var dst = (RtVector)dstObj;

							
							dst.CopyCacheFrom(vec, this);
							dst.methodscopeslot_ref_state = 0;
							dst.nextframe_ref_state = default;

							//vec.HEAPINSTANCE_PTR = dstVecPtr;
							vec.LinkTo(dst,dstVecPtr);

							returnSlot.SetHeapPtr(dstVecPtr, (byte)RtHeapTypeKind.VECTOR, (byte)HeapKindFlag.NONE);

						}
					}
				}
				else if (value.HeapKind == (byte)RtHeapTypeKind.CLOSURE)
				{
					if (!(value.HeapPtr < Context.M_ClosurePtr + Context.STACK_LENGTH) //堆里

						)
					{
						returnSlot = value;

						Debug.Assert( RtClosure.FindAndUpdateHeapInstancePtr(value.HeapPtr, this, out RtClosure _temp) == returnSlot.HeapPtr);



						//RtClosure _temp;
						//int t = RtClosure.FindAndUpdateHeapInstancePtr(value.HeapPtr, this, out _temp);

						//returnSlot.SetHeapPtr(t, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);
					}
					else
					{
						//查询
						RtClosure obj;
						int t = RtClosure.FindAndUpdateHeapInstancePtr(value.HeapPtr, this, out obj);

						if ((t < Context.M_ClosurePtr + calleelastpos) //传入
							||
							!(t < Context.M_ClosurePtr + Context.STACK_LENGTH) //堆里
																	   )
						{
							
							returnSlot.SetHeapPtr(t, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);
						}
						else
						{
							var srcClosure = (RtClosure)obj;

							int dstClosurePtr = returnSlotIndex + Context.M_ClosurePtr;
							var dstClosure = (RtClosure)Context.GC.Heap[dstClosurePtr];

							Context.GC.Heap[dstClosurePtr].Type = obj.Type;

							dstClosure.CopyDataFrom(srcClosure, this);
							dstClosure.methodscopeslot_ref_state = 0;
							dstClosure.nextframe_ref_state = default;

							//srcClosure.HEAPINSTANCE_PTR = dstClosurePtr;
							srcClosure.LinkTo(dstClosure, dstClosurePtr);

							//处理 This 指针
							if (dstClosure.This.ValueType == NaNBoxing.BoxType.HeapPtr)
							{
								bool needupdatescopePtr = dstClosure.ScopePtr == dstClosure.This.HeapPtr;

								//var _this = Context.GC.Heap[dstClosure.This.HeapPtr];
								var _thisKind = (RtHeapTypeKind)dstClosure.This.HeapKind;
								if (_thisKind == RtHeapTypeKind.INSTANCE)
								{
									StoreReturnSlot(ref dstClosure.This, stackStPos, returnSlotIndex, calleelastpos, scope_ptr, dstClosure.This, ref error);
									if (needupdatescopePtr)
									{
										dstClosure.ScopePtr = dstClosure.This.HeapPtr;
									}

								}
								else if (_thisKind == RtHeapTypeKind.CLOSURE)
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
								else if (_thisKind == RtHeapTypeKind.ARRAY)
								{
									StoreReturnSlot(ref dstClosure.This, stackStPos, returnSlotIndex, calleelastpos, scope_ptr, dstClosure.This, ref error);
									if (needupdatescopePtr)
									{
										dstClosure.ScopePtr = dstClosure.This.HeapPtr;
									}

									//throw new NotImplementedException();
								}
								else if (_thisKind == RtHeapTypeKind.VECTOR)
								{
									StoreReturnSlot(ref dstClosure.This, stackStPos, returnSlotIndex, calleelastpos, scope_ptr, dstClosure.This, ref error);
									if (needupdatescopePtr)
									{
										dstClosure.ScopePtr = dstClosure.This.HeapPtr;
									}

									//throw new NotImplementedException();
								}
#if DEBUG
								else if (_thisKind == RtHeapTypeKind.MethodScope)
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

										RtMethodScope last_scope = null;

									lbl_parent:
										var scope = Context.GC.Heap[sptr];

										bool needbreak = (sptr == scope_ptr);

										if (scope.Kind == RtHeapTypeKind.GLOBAL || scope.Kind == RtHeapTypeKind.CLASS || scope.Kind == RtHeapTypeKind.INSTANCE)
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
										if (scope.Kind != RtHeapTypeKind.MethodScope)
										{
											throw new InvalidOperationException();
										}
#endif


										if (sptr < Context.M_ClosurePtr + Context.STACK_LENGTH)
										{
											if (((ASMethodBody)scope.Type).Method.Flags.HasFlag(MethodFlags.NeedActivation))
											{

												RtMethodScope cacheMscope = (RtMethodScope)scope;
												var cacheSpan = cacheMscope.__get_slots_internal;

												RtHeapBase heapObj;
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

												RtMethodScope heap_scope = (RtMethodScope)heapObj;
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

												sptr = ((RtMethodScope)scope).ParentPtr;
											}
											else
											{
												sptr = ((RtMethodScope)scope).ParentPtr;
											}
										}
										else
										{
											last_scope = (RtMethodScope)scope;
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

							value.SetHeapPtr(dstClosurePtr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);
							returnSlot = value;
						}
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
		private unsafe int prepare_savemethodscope_updateref(RtMethodScope heap, int ptr, ref ScopeHeapLocater heapLocater, int min, int max)
		{
			
			
			int iptr = ptr;
			RtInstance oldPayload;
			ptr = RtInstance.FindAndUpdateHeapInstancePtr(ptr, this, out oldPayload); //更新最终指向的目标

			int copyed_ptr = 0;

			if (!(ptr < Context.CacheInstancePtr + Context.STACK_LENGTH)) //堆里的对象,无需拷贝
			{
				copyed_ptr = ptr;
			}


			RtInstance _toupdateref = null; //追踪对新拷贝对象的引用

			for (int k = min; k <= max; k++)
			{

				RtMethodScope scope = (RtMethodScope)Context.GC.Heap[k];
				var scope_span = scope.__get_slots_internal;


				for (int i = 0; i < scope.SlotCount; ++i)
				{
					
					if (!(scope == heap && i == heapLocater.MemberIndex))
					{
						var v = scope_span[i];
						if (v.ValueType == NaNBoxing.BoxType.HeapPtr)
						{
							

							if (v.HeapPtr == ptr || 
								(v.HeapKind == (byte)RtHeapTypeKind.INSTANCE && RtInstance.FindAndUpdateHeapInstancePtr(v.HeapPtr, this, out RtInstance _temp) == ptr) )
							{
								
								if (copyed_ptr == 0)
								{
									//复制一份。
									copyed_ptr = scope.StackPos + i + Context.CacheInstancePtr;
									var dstObj = (RtInstance)Context.GC.Heap[copyed_ptr];

									if (copyed_ptr != ptr)
									{
										//dstObj.Type = type;										
										//((RtInstance)dstObj).HEAPINSTANCE_PTR = 0;
										//((RtInstance)dstObj).CopyFrom(oldPayload, (ASInstance)dstObj.Type, this, type._link_codescope.TypeLayout.Size);

										Debug.Assert(k >= heap.mScopePtr);

										((RtInstance)dstObj).CloneOther(oldPayload, this);

										if (k > heap.mScopePtr)
										{
											(dstObj).nextframe_ref_state.scope_ptr = k;
											(dstObj).nextframe_ref_state.version = scope.version;

											(dstObj).methodscopeslot_ref_state = 0;
										}
										else
										{
											(dstObj).methodscopeslot_ref_state = 1;
											_toupdateref = dstObj;
										}

											
										oldPayload.LinkTo((RtInstance)dstObj, copyed_ptr);
										//oldPayload.HEAPINSTANCE_PTR = copyed_ptr;
									}
									else
									{
										//没有拷到this槽的先例
										Debug.Assert(i != scope.SlotCount - 1);
										Debug.Assert(dstObj.methodscopeslot_ref_state != 0);
									}

									//更新引用
									v.SetHeapPtr(copyed_ptr, (byte)RtHeapTypeKind.INSTANCE, (byte)(v.HeapFlag & (byte)HeapKindFlag.FLAG_STRUCT));
									//scope.SetSlot(v, (ushort)i);
									scope_span[i] = v;
										

								}
								else
								{
									if (_toupdateref != null && k == heap.mScopePtr)
									{
										Debug.Assert(_toupdateref.methodscopeslot_ref_state == 1);
										_toupdateref.methodscopeslot_ref_state = 2;
										_toupdateref = null;
									}

									//更新引用
									v.SetHeapPtr(copyed_ptr, (byte)RtHeapTypeKind.INSTANCE, (byte)(v.HeapFlag & (byte)HeapKindFlag.FLAG_STRUCT));
									//scope.SetSlot(v, (ushort)i);
									scope_span[i] = v;
								}
								
							}
							else if ( v.HeapKind == (byte)RtHeapTypeKind.CLOSURE )
							{
								var closure = Context.GC.Heap[v.HeapPtr];
								//lbl_flag:
								ref var This = ref ((RtClosure)closure).This;
								if (This.ValueType == NaNBoxing.BoxType.HeapPtr && This.HeapKind == (byte)RtHeapTypeKind.INSTANCE)
								{
									bool needupdateScopePtr = This.HeapPtr == ((RtClosure)closure).ScopePtr;

									//var _this = Context.GC.Heap[This.HeapPtr];
									Debug.Assert(Context.GC.Heap[This.HeapPtr].Kind == RtHeapTypeKind.INSTANCE);
												
									if (This.HeapPtr == ptr || RtInstance.FindAndUpdateHeapInstancePtr(This.HeapPtr, this, out _temp) == ptr)
									{
										if (copyed_ptr == 0)
										{
											//复制一份。
											copyed_ptr = scope.StackPos + i + Context.CacheInstancePtr;
											var dstObj = (RtInstance)Context.GC.Heap[copyed_ptr];
											if (copyed_ptr != ptr)
											{
												
												((RtInstance)dstObj).CloneOther(oldPayload, this);
												if (k > heap.mScopePtr)
												{
													(dstObj).nextframe_ref_state.scope_ptr = k;
													(dstObj).nextframe_ref_state.version = scope.version;

													(dstObj).methodscopeslot_ref_state = 0;
												}
												else
												{
													(dstObj).methodscopeslot_ref_state = 1;
													_toupdateref = dstObj;
												}



												oldPayload.LinkTo((RtInstance)dstObj, copyed_ptr);
											}
											else
											{
												Debug.Assert(i != scope.SlotCount - 1);
												Debug.Assert(dstObj.methodscopeslot_ref_state != 0 || (dstObj.nextframe_ref_state.scope_ptr == scope.mScopePtr && dstObj.nextframe_ref_state.version == scope.version ));
											}

											This.SetHeapPtr(copyed_ptr, (byte)RtHeapTypeKind.INSTANCE, (byte)(This.HeapFlag & (byte)HeapKindFlag.FLAG_STRUCT));
											if (needupdateScopePtr)
											{
												((RtClosure)closure).ScopePtr = copyed_ptr;
											}
										}
										else
										{
											if (_toupdateref != null && k == heap.mScopePtr)
											{
												Debug.Assert(_toupdateref.methodscopeslot_ref_state == 1);
												_toupdateref.methodscopeslot_ref_state = 2;
												_toupdateref = null;
											}

											This.SetHeapPtr(copyed_ptr, (byte)RtHeapTypeKind.INSTANCE, (byte)(This.HeapFlag & (byte)HeapKindFlag.FLAG_STRUCT));
											if (needupdateScopePtr)
											{
												((RtClosure)closure).ScopePtr = copyed_ptr;
											}
										}
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

			//if (copyed_ptr == 0)
			//{
			//	Debug.Assert( heap.ReadSlot(heapLocater.MemberIndex).HeapPtr == ptr);
			//	copyed_ptr = ptr;
			//}

			Debug.Assert( copyed_ptr !=0 || heap.ReadSlot(heapLocater.MemberIndex).HeapPtr == ptr);

			return copyed_ptr;
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private  NaNBoxing prepare_savemethodscope_beforeSave(RtMethodScope heap, NaNBoxing old, ScopeHeapLocater heapLocater, ref int min, ref int max,int scope_ptr)
		{
			
			//lbl_redo:
			int ptr = old.HeapPtr;
			
			if (old.HeapKind == (byte)RtHeapTypeKind.INSTANCE)
			{
				//if (((ASInstance)oldObj.Type).Flags.HasFlag(ClassFlags.Struct))
				//{
				//	//pass //结构体也可能在传参时是引用。
				//	return ptr;
				//}
				//else 
				if (ptr == heap.StackPos + heapLocater.MemberIndex + Context.CacheInstancePtr)
				{
					var oldObj = (RtInstance)Context.GC.Heap[ptr];

					int ref_nextframe = 0;
					if (heap.mScopePtr < scope_ptr && oldObj.nextframe_ref_state.scope_ptr > 0
						&& ((RtMethodScope)Context.GC.Heap[oldObj.nextframe_ref_state.scope_ptr]).version == oldObj.nextframe_ref_state.version)
					{
						ref_nextframe = 1;
					}

					if (oldObj.methodscopeslot_ref_state + ref_nextframe == 2 ) //只有状态是2的情况才可能会被引用
					{
						/*
						var a = new O(3);
						var b = a;
						a.tag = a;        //类似此代码，如果a本身被存入堆中。其他对a的引用仍然还在缓存对象中，这时候需要跟踪其他引用都改成堆中。
						a = new Main();
						b.tag = 6;
						 */

						/*			这是最差情况，确实会导致没有引用了但是还需要查找一次	 但为了避免指针查找，权衡之下先保留，因为引用计数会导致必须到堆里查找旧对象		 
						(function k1()
						{
							var i = {};
							var j = i;
							var k = i;
	
							k = null;
							j = null;
							i = null;
	
						})();	
						 */


						//return prepare_savemethodscope_updateref(heap, ptr, ref heapLocater, oldObj.Type, m_scope, method_scopes);

						if (min == 0 && max == 0)
						{
							ComputeMinMaxMethodScope(ref min, ref max, scope_ptr);
						}

						NaNBoxing r = default;

						int copy_ptr = prepare_savemethodscope_updateref(heap, ptr, ref heapLocater, min, max);
						if (copy_ptr != 0)
						{
							r.SetHeapPtr(copy_ptr
								, (byte)RtHeapTypeKind.INSTANCE,
								(byte)(old.HeapFlag & (byte)HeapKindFlag.FLAG_STRUCT));
						}
						else
							r.SetNull();
						return r;
					}
					else
					{
						//pass
						//return ptr;
						//NaNBoxing r = default;
						//r.SetHeapPtr(ptr, (byte)RtHeapTypeKind.INSTANCE,old.HeapFlag);
						//return r;

						return old;
					}
				}
				else
				{
					//return ptr;
					//NaNBoxing r = default;
					//r.SetHeapPtr(ptr, (byte)RtHeapTypeKind.INSTANCE);
					//return r;
					return old;
				}
			}
			else if (old.HeapKind == (byte)RtHeapTypeKind.CLOSURE)
			{
				//更新Closure的引用
				if (ptr == heap.StackPos + heapLocater.MemberIndex + Context.M_ClosurePtr)
				{
					var oldObj = (RtClosure)Context.GC.Heap[ptr];

					int ref_nextframe = 0;
					if (heap.mScopePtr < scope_ptr && oldObj.nextframe_ref_state.scope_ptr > 0
						&& ((RtMethodScope)Context.GC.Heap[oldObj.nextframe_ref_state.scope_ptr]).version == oldObj.nextframe_ref_state.version)
					{
						ref_nextframe = 1;
					}


					if ((oldObj).methodscopeslot_ref_state + ref_nextframe != 2)
					{
						NaNBoxing r = default;
						r.SetHeapPtr(ptr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);
						return r;
						//return ptr;
					}
					else
					{
						
						//Debug.Assert(m_scope != null && method_scopes != null);
						//Debug.Assert(method_scopes != null);

						//函数闭包里保存的this,也需要拷贝一份引用到别的地方
						var This = ((RtClosure)oldObj).This;
						if (!This.IsStruct() && This.ValueType == NaNBoxing.BoxType.HeapPtr && This.HeapKind >= (byte)RtHeapTypeKind.INSTANCE)
						{
							if (min == 0 && max == 0)
							{
								ComputeMinMaxMethodScope(ref min, ref max, scope_ptr);
							}
							var this_ptr = prepare_savemethodscope_beforeSave(heap, This, heapLocater,ref min,ref max, scope_ptr); //更新原this,然后下面才能正确更新.
							((RtClosure)oldObj).This = this_ptr; //.SetHeapPtr(this_ptr);
							//goto lbl_redo;
						}

						int copyed_ptr = 0;
						int final_ptr = RtClosure.FindAndUpdateHeapInstancePtr(ptr, this, out oldObj);

						if (!(final_ptr < Context.M_ClosurePtr + Context.STACK_LENGTH)) //追踪是否已经在堆中。
						{
							copyed_ptr = final_ptr;
						}
						
						RtClosure toupate_ref = null;

						if (min == 0 && max == 0)
						{
							ComputeMinMaxMethodScope(ref min, ref max, scope_ptr);
						}
						for (int k = min; k <= max; k++)
						{
							RtMethodScope scope = (RtMethodScope)Context.GC.Heap[k];
							for (int i = 0; i < scope.SlotCount - 1; ++i)
							{
								if (!(scope == heap && i == heapLocater.MemberIndex))
								{
									var v = scope.ReadSlot((ushort)i);
									if (v.ValueType == NaNBoxing.BoxType.HeapPtr && v.HeapKind == (byte)RtHeapTypeKind.CLOSURE 
										&&
										(
											v.HeapPtr == ptr ||
											RtClosure.FindAndUpdateHeapInstancePtr(v.HeapPtr, this, out RtClosure _temp) == ptr)
										)
									{
										//复制一份新的Clousure
										if (copyed_ptr == 0)
										{
											copyed_ptr = scope.StackPos + i + Context.M_ClosurePtr;

											if (copyed_ptr != ptr)
											{
												var srcClosure = (RtClosure)oldObj;



												var dstClosure = (RtClosure)Context.GC.Heap[copyed_ptr];
												dstClosure.Type = oldObj.Type;






												dstClosure.CopyDataFrom(srcClosure, this);

												Debug.Assert(k >= heap.mScopePtr);

												if (k > heap.mScopePtr)
												{
													(dstClosure).nextframe_ref_state.scope_ptr = k;
													(dstClosure).nextframe_ref_state.version = scope.version;

													(dstClosure).methodscopeslot_ref_state = 0;
												}
												else
												{
													(dstClosure).methodscopeslot_ref_state = 1;
													toupate_ref = dstClosure;
												}

												//srcClosure.HEAPINSTANCE_PTR = copyed_ptr;
												srcClosure.LinkTo(dstClosure, copyed_ptr);
											}
#if DEBUG	
											else
											{

												throw new InvalidOperationException("找不到触发这个分支的案例.");

											}

#endif
											v.SetHeapPtr(copyed_ptr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);
											scope.SetSlot(v, (ushort)i);
										}
										else
										{
											if (toupate_ref != null && k == heap.mScopePtr)
											{
												Debug.Assert(toupate_ref.methodscopeslot_ref_state != 0);
												toupate_ref.methodscopeslot_ref_state = 2;

												if (toupate_ref.This.ValueType == NaNBoxing.BoxType.HeapPtr && 
													toupate_ref.This.HeapKind == (int)RtHeapTypeKind.INSTANCE
													&&
													toupate_ref.This.HeapPtr < Context.CacheInstancePtr + Context.STACK_LENGTH
													)
												{
													var newthis = (RtInstance)Context.GC.Heap[toupate_ref.This.HeapPtr];
													Debug.Assert(newthis.methodscopeslot_ref_state != 0);
													newthis.methodscopeslot_ref_state = 2;

												}


												toupate_ref = null;
											}

											v.SetHeapPtr(copyed_ptr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);
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

						//if (toupate_ref != null && toupate_ref.methodscopeslot_ref_state == 2) //如果methodscope有其他引用，那么它的this也需更新为需要扫描状态
						//{
						//	if (toupate_ref.This.ValueType == NaNBoxing.BoxType.HeapPtr)
						//	{
						//		var newthis = Context.GC.Heap[toupate_ref.This.HeapPtr];
						//		if (newthis.Kind == RtHeapTypeKind.INSTANCE)
						//		{
						//			if (((RtInstance)newthis).methodscopeslot_ref_state == 1)
						//			{
						//				((RtInstance)newthis).methodscopeslot_ref_state = 2;
						//			}
						//		}
						//		else if (newthis.Kind == RtHeapTypeKind.CLOSURE)
						//		{
						//			if (((RtClosure)newthis).methodscopeslot_ref_state == 1)
						//			{
						//				((RtClosure)newthis).methodscopeslot_ref_state = 2;
						//			}
						//		}
						//	}
						//}

						//return copyed_ptr;
						NaNBoxing r = default;

						if (copyed_ptr == 0)
							r.SetNull();
						else
							r.SetHeapPtr(copyed_ptr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);
						return r;
					}


				}
				else
				{
					//return ptr;
					NaNBoxing r = default;
					r.SetHeapPtr(ptr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);
					return r;
				}

			}
			else if (old.HeapKind == (byte)RtHeapTypeKind.ARRAY)
			{
				if (ptr == heap.StackPos + heapLocater.MemberIndex + Context.CacheArrayPtr)
				{
					var oldObj = (RtArray)Context.GC.Heap[ptr];
#if DEBUG
					if (((RtArray)oldObj).StoreMode != RtArray.ArrayStoreMode.cache)
					{
						throw new InvalidOperationException();
					}
#endif

					int ref_nextframe = 0;
					if (heap.mScopePtr <scope_ptr && oldObj.nextframe_ref_state.scope_ptr > 0
						&& ((RtMethodScope)Context.GC.Heap[oldObj.nextframe_ref_state.scope_ptr]).version == oldObj.nextframe_ref_state.version	)
					{
						ref_nextframe = 1;
					}

					if (oldObj.methodscopeslot_ref_state + ref_nextframe <2 )
					{
						//return ptr;
						NaNBoxing r = default;
						r.SetHeapPtr(ptr, (byte)RtHeapTypeKind.ARRAY, (byte)HeapKindFlag.NONE);
						return r;
					}
					else
					{
						//有2个或以上的引用。变量算一个,this也算一个。

						//更新数组的引用
						RtArray oldPayload;
						ptr = RtArray.FindAndUpdateHeapInstancePtr(ptr, this, out oldPayload); //更新最终指向的目标

						int copyed_ptr = 0;
						if (!(ptr < Context.CacheArrayPtr + Context.STACK_LENGTH)) //堆里的对象,无需拷贝
						{
							copyed_ptr = ptr;
						}

						RtArray toupdateref = null; //追踪对新拷贝对象的引用

						if (min == 0 && max == 0)
						{
							ComputeMinMaxMethodScope(ref min, ref max, scope_ptr);
						}
						for (int k = min; k <= max; k++)
						{
							RtMethodScope scope = (RtMethodScope)Context.GC.Heap[k];
							var scope_span = scope.__get_slots_internal;
							for (int i = 0; i < scope.SlotCount; ++i)
							{								
								if (!(scope == heap && i == heapLocater.MemberIndex))
								{
									var v = scope_span[i];
									if (v.ValueType == NaNBoxing.BoxType.HeapPtr && v.HeapKind == (byte)RtHeapTypeKind.ARRAY)
									{
										//var inmember = Context.GC.Heap[v.HeapPtr];
										Debug.Assert(Context.GC.Heap[v.HeapPtr].Kind == RtHeapTypeKind.ARRAY);
										
											
										if (v.HeapPtr == ptr || RtArray.FindAndUpdateHeapInstancePtr(v.HeapPtr, this, out RtArray _temp) == ptr)
										{
											if (copyed_ptr == 0)
											{
												copyed_ptr = scope.StackPos + i + Context.CacheArrayPtr;
#if DEBUG
												if (copyed_ptr == ptr && scope == heap)
												{
													throw new InvalidOperationException();
												}
#endif
													
												var dst = (RtArray)Context.GC.Heap[copyed_ptr];
												if (copyed_ptr != ptr)
												{
													dst.Type = Context.ARRAY.Instance;
													//if (i == scope.SlotCount - 1)
													//{
													//	//说明是this槽。
													//	((RtArray)dst).nextframe_ref_state = (byte)k;
													//	((RtArray)dst).methodscopeslot_ref_state = 0;
													//}
													//else
													//{
													//	((RtArray)dst).methodscopeslot_ref_state = 1;
													//}

													Debug.Assert(k >= heap.mScopePtr);

													if (k > heap.mScopePtr)
													{
														((RtArray)dst).nextframe_ref_state.scope_ptr = k;
														((RtArray)dst).nextframe_ref_state.version = scope.version;

														((RtArray)dst).methodscopeslot_ref_state = 0;
													}
													else
													{
														((RtArray)dst).methodscopeslot_ref_state = 1;
														toupdateref = (RtArray)dst;
													}

													((RtArray)dst).CopyCacheFrom(oldPayload, this);


													((RtArray)oldObj).LinkTo((RtArray)dst, copyed_ptr);
												}
												else
												{
													//没有拷到this槽的先例
													Debug.Assert(i != scope.SlotCount - 1);
													Debug.Assert(dst.methodscopeslot_ref_state != 0);
												}

												v.SetHeapPtr(copyed_ptr, (byte)RtHeapTypeKind.ARRAY, (byte)HeapKindFlag.NONE);
												//scope.SetSlot(v, (ushort)i);
												scope_span[i] = v;

												
												
											}
											else
											{
												if (toupdateref != null && k == heap.mScopePtr)
												{
													Debug.Assert(toupdateref.methodscopeslot_ref_state == 1);
													toupdateref.methodscopeslot_ref_state = 2;
													toupdateref = null;
												}


												//更新引用
												v.SetHeapPtr(copyed_ptr, (byte)RtHeapTypeKind.ARRAY, (byte)HeapKindFlag.NONE);
												//scope.SetSlot(v, (ushort)i);
												scope_span[i] = v;
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

						//return copyed_ptr;
						NaNBoxing r = default;
						if (copyed_ptr > 0)
						{
							r.SetHeapPtr(copyed_ptr, (byte)RtHeapTypeKind.ARRAY, (byte)HeapKindFlag.NONE);
						}
						else
						{
						//**此类代码确实会导致找不到：所以这里只能这样保底
						//							(function k1()
						//{
						//								var i = [1];
						//								var j = i;
						//								var k = i;

						//								k = null;
						//								j = null;
						//								i = null;
						//							})();
						//**//

							r.SetNull();
						}
						return r;
					}
				}
				else
				{
					NaNBoxing r = default;
					r.SetHeapPtr(ptr, (byte)RtHeapTypeKind.ARRAY, (byte)HeapKindFlag.NONE);
					return r;
					//return ptr;
				}

			}
			else if (old.HeapKind == (byte)RtHeapTypeKind.VECTOR)
			{
				if (ptr == heap.StackPos + heapLocater.MemberIndex + Context.CacheVectorPtr)
				{
					var oldObj = (RtVector)Context.GC.Heap[ptr];

					int ref_nextframe = 0;
					if (heap.mScopePtr < scope_ptr && oldObj.nextframe_ref_state.scope_ptr > 0
						&& ((RtMethodScope)Context.GC.Heap[oldObj.nextframe_ref_state.scope_ptr]).version == oldObj.nextframe_ref_state.version)
					{
						ref_nextframe = 1;
					}

					if (oldObj.methodscopeslot_ref_state + ref_nextframe < 2)
					{
						NaNBoxing r = default;
						r.SetHeapPtr(ptr, (byte)RtHeapTypeKind.VECTOR, (byte)HeapKindFlag.NONE);
						return r;
						//return ptr;
					}
					else
					{
						//Debug.Assert(m_scope != null && method_scopes != null);
						//Debug.Assert(method_scopes != null);

						//更新Vector的引用
						RtVector oldPayload;
						ptr = RtVector.FindAndUpdateHeapInstancePtr(ptr, this, out oldPayload);
						int copyed_ptr = 0;
						if (!(ptr < Context.CacheVectorPtr + Context.STACK_LENGTH))
						{
							copyed_ptr = ptr;
						}

						RtVector toupdateref = null; //追踪对新拷贝对象的引用

						if (min == 0 && max == 0)
						{
							ComputeMinMaxMethodScope(ref min, ref max, scope_ptr);
						}
						for (int k = min; k <= max; k++)
						{
							RtMethodScope scope = (RtMethodScope)Context.GC.Heap[k];
							var scope_span = scope.__get_slots_internal;
							for (int i = 0; i < scope.SlotCount; ++i)
							{								
								if (!(scope == heap && i == heapLocater.MemberIndex))
								{
									var v = scope_span[i];
									if (v.ValueType == NaNBoxing.BoxType.HeapPtr && v.HeapKind == (byte)RtHeapTypeKind.VECTOR)
									{
										//var inmember = Context.GC.Heap[v.HeapPtr];
										Debug.Assert(Context.GC.Heap[v.HeapPtr].Kind == RtHeapTypeKind.VECTOR);
										
										RtVector _temp;
										if (v.HeapPtr == ptr || RtVector.FindAndUpdateHeapInstancePtr(v.HeapPtr, this, out _temp) == ptr)
										{
											if (copyed_ptr == 0)
											{
												copyed_ptr = scope.StackPos + i + Context.CacheVectorPtr;

												var dst = (RtVector)Context.GC.Heap[copyed_ptr];

												if (copyed_ptr != ptr)
												{

													dst.Type = oldObj.Type;

													Debug.Assert(k >= heap.mScopePtr);

													if (k > heap.mScopePtr)
													{
														(dst).nextframe_ref_state.scope_ptr = k;
														(dst).nextframe_ref_state.version = scope.version;

														(dst).methodscopeslot_ref_state = 0;
													}
													else
													{
														(dst).methodscopeslot_ref_state = 1;
														toupdateref = dst;
													}


													((RtVector)dst).CopyCacheFrom(oldPayload, this);

													//((RtVector)oldObj).HEAPINSTANCE_PTR = copyed_ptr;
													((RtVector)oldObj).LinkTo(dst, copyed_ptr);

												}
												else
												{
													//没有拷到this槽的先例
													Debug.Assert(i != scope.SlotCount - 1);
													Debug.Assert(dst.methodscopeslot_ref_state != 0);
												}


												v.SetHeapPtr(copyed_ptr, (byte)RtHeapTypeKind.VECTOR, (byte)HeapKindFlag.NONE);
												//scope.SetSlot(v, (ushort)i);
												scope_span[i] = v;
											}
											else
											{
												if (toupdateref != null && k == heap.mScopePtr)
												{
													Debug.Assert(toupdateref.methodscopeslot_ref_state == 1);
													toupdateref.methodscopeslot_ref_state = 2;
													toupdateref = null;
												}

												//更新引用
												v.SetHeapPtr(copyed_ptr, (byte)RtHeapTypeKind.VECTOR, (byte)HeapKindFlag.NONE);
												//scope.SetSlot(v, (ushort)i);
												scope_span[i] = v;
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


						//return copyed_ptr;
						NaNBoxing r = default;
						if (copyed_ptr > 0)
						{
							r.SetHeapPtr(copyed_ptr, (byte)RtHeapTypeKind.VECTOR, (byte)HeapKindFlag.NONE);
						}
						else
							r.SetNull();
						return r;
					}
				}
				else
				{
					//return ptr;
					NaNBoxing r = default;
					r.SetHeapPtr(ptr, (byte)RtHeapTypeKind.VECTOR, (byte)HeapKindFlag.NONE);
					return r;
				}
			}

#if DEBUG
			else if (old.HeapKind == (byte)RtHeapTypeKind.MethodScope)
			{
				throw new InvalidOperationException();
			}
#endif
			else
			{
				//pass
				//return ptr;
				return old;
			}
		}
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private void prepare_savemethodscope_saveinstacne(RtMethodScope heap, ref NaNBoxing saveSlot,   ScopeHeapLocater heapLocater,bool is_pass_this)
		{
			int srcPtr = saveSlot.HeapPtr;


			if ( //(saveSlot.HeapFlag & (byte)HeapKindFlag.FLAG_STRUCT) == (byte)HeapKindFlag.FLAG_STRUCT  //((ASInstance)src.Type).Flags.HasFlag(ClassFlags.Struct)
					saveSlot.IsStruct()	                                        
				)
			{

				if (
						(!is_pass_this  // 传This时传引用	
							||
							(((HeapKindFlag)saveSlot.HeapFlag & HeapKindFlag.FLAG_REFSTRUCT) == HeapKindFlag.FLAG_REFSTRUCT)
						//||
						//((RtInstance)src).IsRefVectorOrFromContainerOrRefStruct(this, (ASInstance)src.Type)

						//但是对结构体内部的引用或Vector内部的结构体 ,或者刚从数组,字典等里取出的结构体是例外，
						//类似C#处理，
						//此时This也要直接复制结构体 
						))
				{
					var src = Context.GC.Heap[saveSlot.HeapPtr];
					//Clone结构体
					int clonedptr = heapLocater.MemberIndex + heap.StackPos + Context.CacheInstancePtr;
					var cacheObj = Context.GC.Heap[clonedptr];
					//cacheObj.Type = src.Type;

					//((RtInstance)cacheObj).HEAPINSTANCE_PTR = 0;
					//((RtInstance)cacheObj).CopyFrom(src, this, src.Type._link_codescope.TypeLayout.Size);

					((RtInstance)cacheObj).CloneOther((RtInstance)src, this);

					((RtInstance)cacheObj).methodscopeslot_ref_state = 1;
					

					saveSlot.SetHeapPtr(clonedptr, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.FLAG_STRUCT);

					if (srcPtr == heap.ParentPtr)
					{
						heap.ParentPtr = clonedptr;
					}

					return;
				}
				else
				{
					Debug.Assert(((RtInstance)Context.GC.Heap[saveSlot.HeapPtr]).IsRefVectorOrFromContainerOrRefStruct(this, (ASInstance)Context.GC.Heap[saveSlot.HeapPtr].Type)
						== ((((HeapKindFlag)saveSlot.HeapFlag & HeapKindFlag.FLAG_REFSTRUCT) == HeapKindFlag.FLAG_REFSTRUCT))
						);
				}


			}



			if (!(srcPtr < Context.CacheInstancePtr + Context.STACK_LENGTH))
			{
				//堆中的对象，不管它直接存
			}
			else
			{
				//先追踪到最终的 HEAPINSTANCE_PTR.
				RtInstance srcPayload;
				int src_ptr = RtInstance.FindAndUpdateHeapInstancePtr(srcPtr, this, out srcPayload);
				//如果在堆里，直接存
				if (!(src_ptr < Context.CacheInstancePtr + Context.STACK_LENGTH))
				{
					saveSlot.SetHeapPtr(src_ptr, (byte)RtHeapTypeKind.INSTANCE, (byte)(((ASInstance)srcPayload.Type).Flags.HasFlag(ClassFlags.Struct) ? HeapKindFlag.FLAG_STRUCT : HeapKindFlag.NONE));
				}
				else if (src_ptr < heap.StackPos + Context.CacheInstancePtr)
				{
					//定义在上一层调用栈的对象，直接存,标记被下一层引用
					saveSlot.SetHeapPtr(src_ptr, (byte)RtHeapTypeKind.INSTANCE, (byte)(((ASInstance)srcPayload.Type).Flags.HasFlag(ClassFlags.Struct) ? HeapKindFlag.FLAG_STRUCT : HeapKindFlag.NONE));
					
					if (srcPayload.nextframe_ref_state.scope_ptr > 0 && ((RtMethodScope)Context.GC.Heap[srcPayload.nextframe_ref_state.scope_ptr]).version
								== srcPayload.nextframe_ref_state.version)
					{
						//那个引用帧还在
					}
					else
					{
						//引用已失效
						srcPayload.nextframe_ref_state.scope_ptr = heap.mScopePtr;
						srcPayload.nextframe_ref_state.version = heap.version;
					}

				}
				else if (src_ptr < heap.StackPos + heap.SlotCount + Context.CacheInstancePtr)
				{
					//定义在本层的对象,更新引用状态
					saveSlot.SetHeapPtr(src_ptr, (byte)RtHeapTypeKind.INSTANCE, (byte)(((ASInstance)srcPayload.Type).Flags.HasFlag(ClassFlags.Struct) ? HeapKindFlag.FLAG_STRUCT : HeapKindFlag.NONE));

					Debug.Assert(srcPayload.methodscopeslot_ref_state != 0);
					if (srcPayload.methodscopeslot_ref_state == 1) 
					{
						Debug.Assert(srcPayload.HEAPINSTANCE_PTR == 0); //说明这是一个结构体对Vector,或者父布局Struct引用, 它们不可能有变量引用
						//说明缓存对象被引用了。
						srcPayload.methodscopeslot_ref_state = 2;
					}

				}
				else
				{
					//否则，缓存对象复制到要存入的slot的缓存池里，然后将目标slot指向它的缓存池。最后，将原对象也设置成payload指向目标slot的缓存池。
					int dstptr = heapLocater.MemberIndex + heap.StackPos + Context.CacheInstancePtr;
					var dstObj = Context.GC.Heap[dstptr];

					//dstObj.Type = srcPayload.Type;

					//((RtInstance)dstObj).HEAPINSTANCE_PTR = 0;
					//((RtInstance)dstObj).CopyFrom(srcPayload, (ASInstance)dstObj.Type, this, srcPayload.Type._link_codescope.TypeLayout.Size);

					((RtInstance)dstObj).CloneOther((RtInstance)srcPayload, this);


					((RtInstance)dstObj).methodscopeslot_ref_state = 1;
					
					//srcPayload.HEAPINSTANCE_PTR = dstptr;

					srcPayload.LinkTo(((RtInstance)dstObj), dstptr);

					saveSlot.SetHeapPtr(dstptr, (byte)RtHeapTypeKind.INSTANCE, (byte)(((ASInstance)srcPayload.Type).Flags.HasFlag(ClassFlags.Struct) ? HeapKindFlag.FLAG_STRUCT : HeapKindFlag.NONE));

				}

			}
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private void ComputeMinMaxMethodScope(ref int min,ref int max,int scopeptr)
		{
			max = int.MinValue;
			min = int.MaxValue;

			//if (m_scope == null)
			{
				max = scopeptr; //*method_scopes;
				min = max;

				var s = (RtMethodScope)Context.GC.Heap[max];
				while (s.ParentPtr - Context.M_MethodScopePtr < Context.MAX_BACKTRACE)
				{
					Debug.Assert(s.ParentPtr < max);

					int p = s.ParentPtr;
					s = (RtMethodScope)Context.GC.Heap[p];
					if (!s.IsStackSlot)
						break;

					min = min < p ? min : p; //Math.Min(min, p);
				}

			}
			//else
			//{
			//	int* __test = m_scope;
			//	do
			//	{
			//		--__test;

			//		if (*__test - Context.M_MethodScopePtr < Context.MAX_BACKTRACE)
			//		{
			//			max = max > *__test ? max : *__test;  //Math.Max(max, *__test);
			//			min = min < *__test ? min : *__test; //Math.Min(min, *__test);
			//		}

			//	} while (__test != method_scopes);
			//}
			Debug.Assert(min > 0);
			Debug.Assert(max - Context.M_MethodScopePtr < Context.MAX_BACKTRACE);

		}


		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private void prepare_savescope_pass(ref NaNBoxing value, RtMethodScope heap,ScopeHeapLocater heapLocater,
			NaNBoxing old,int min,int max, int scope_ptr, ref ReceiveError error ,bool is_pass_this)
		{
			
			{

				if (value.HeapKind == (byte)RtHeapTypeKind.INSTANCE)
				{
					
					//var obj = Context.GC.Heap[value.HeapPtr];
					prepare_savemethodscope_saveinstacne(heap, ref value, heapLocater, is_pass_this);
				}				
				else if (value.HeapKind == (byte)RtHeapTypeKind.ARRAY)
				{
					RtArray array;
					int array_ptr = RtArray.FindAndUpdateHeapInstancePtr(value.HeapPtr, this, out array);

					if (array.StoreMode == RtArray.ArrayStoreMode.cache_on_stack)
					{
						if (array.stack_store_startindex < heap.StackPos + heap.SlotCount)
						{
							
							//pass
							value.SetHeapPtr(array_ptr, (byte)RtHeapTypeKind.ARRAY, (byte)HeapKindFlag.NONE);
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
					else if (array.StoreMode == RtArray.ArrayStoreMode.cache)
					{
						
						if (array_ptr < heap.StackPos + Context.CacheArrayPtr)
						{
							//定义在上一层调用栈的对象, 直接存，并且标记被下一层函数栈引用
							value.SetHeapPtr(array_ptr, (byte)RtHeapTypeKind.ARRAY, (byte)HeapKindFlag.NONE);

							if (array.nextframe_ref_state.scope_ptr > 0 && ((RtMethodScope)Context.GC.Heap[array.nextframe_ref_state.scope_ptr]).version
								== array.nextframe_ref_state.version)
							{
								//那个引用帧还在
							}
							else
							{
								//引用已失效
								array.nextframe_ref_state.scope_ptr = heap.mScopePtr;
								array.nextframe_ref_state.version = heap.version;
							}

						}
						else if (array_ptr < heap.StackPos + heap.SlotCount + Context.CacheArrayPtr)
						{
							//存在本层的变量里的对象,直接存，需要更新引用状态。
							value.SetHeapPtr(array_ptr, (byte)RtHeapTypeKind.ARRAY, (byte)HeapKindFlag.NONE);

							Debug.Assert(array.methodscopeslot_ref_state != 0);

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

							Debug.Assert(!is_pass_this);

							((RtArray)dstObj).methodscopeslot_ref_state = 1;
							((RtArray)dstObj).nextframe_ref_state = default;
							

							//((RtArray)dstObj).HEAPINSTANCE_PTR = 0;
							((RtArray)dstObj).CopyCacheFrom(array, this);

							//array.HEAPINSTANCE_PTR = dstptr;
							array.LinkTo((RtArray)dstObj, dstptr);
							value.SetHeapPtr(dstptr, (byte)RtHeapTypeKind.ARRAY, (byte)HeapKindFlag.NONE);

						}
					}
					else
					{
						
						//pass.
						value.SetHeapPtr(array_ptr, (byte)RtHeapTypeKind.ARRAY, (byte)HeapKindFlag.NONE);
					}
				}
				else if (value.HeapKind == (byte)RtHeapTypeKind.VECTOR)
				{
					
					RtVector vector;
					int vector_ptr = RtVector.FindAndUpdateHeapInstancePtr(value.HeapPtr, this, out vector);

					if (!(vector_ptr < Context.CacheVectorPtr + Context.STACK_LENGTH))
					{
						
						//pass
						value.SetHeapPtr(vector_ptr, (byte)RtHeapTypeKind.VECTOR, (byte)HeapKindFlag.NONE);
					}
					else if (vector_ptr < heap.StackPos + Context.CacheVectorPtr)
					{
						//定义在上一层调用栈的对象, 直接存，并且标记被下一层函数栈引用
						value.SetHeapPtr(vector_ptr, (byte)RtHeapTypeKind.VECTOR, (byte)HeapKindFlag.NONE);

						if (vector.nextframe_ref_state.scope_ptr > 0 && ((RtMethodScope)Context.GC.Heap[vector.nextframe_ref_state.scope_ptr]).version
							== vector.nextframe_ref_state.version)
						{
							//那个引用帧还在
						}
						else
						{
							//引用已失效
							vector.nextframe_ref_state.scope_ptr = heap.mScopePtr;
							vector.nextframe_ref_state.version = heap.version;
						}
					}
					else if (vector_ptr < heap.StackPos + heap.SlotCount + Context.CacheVectorPtr)
					{

						//存在本层的变量里的对象,直接存，需要更新引用状态。
						value.SetHeapPtr(vector_ptr, (byte)RtHeapTypeKind.VECTOR, (byte)HeapKindFlag.NONE);

						Debug.Assert(vector.methodscopeslot_ref_state != 0);

						if (vector.methodscopeslot_ref_state == 1)
						{
							vector.methodscopeslot_ref_state = 2;
						}
						
					}
					else
					{
						
						var obj = Context.GC.Heap[value.HeapPtr];
						//否则，缓存对象复制到要存入的slot的缓存池里，然后将目标slot指向它的缓存池。最后，将原对象也设置成payload指向目标slot的缓存池。
						int dstptr = heapLocater.MemberIndex + heap.StackPos + Context.CacheVectorPtr;
						var dstObj = Context.GC.Heap[dstptr];
						dstObj.Type = obj.Type;

						((RtVector)dstObj).CopyCacheFrom(vector, this);
						((RtVector)dstObj).methodscopeslot_ref_state = 1;
						((RtVector)dstObj).nextframe_ref_state = default;

						//vector.HEAPINSTANCE_PTR = dstptr;
						vector.LinkTo((RtVector)dstObj, dstptr);

						value.SetHeapPtr(dstptr, (byte)RtHeapTypeKind.VECTOR, (byte)HeapKindFlag.NONE);

					}
				}
				else if (value.HeapKind == (byte)RtHeapTypeKind.CLOSURE)
				{
					
					var obj = Context.GC.Heap[value.HeapPtr];
					var srcClosure = (RtClosure)obj;
					int final_ptr = RtClosure.FindAndUpdateHeapInstancePtr(value.HeapPtr, this, out srcClosure);

					if (!(final_ptr < Context.M_ClosurePtr + Context.STACK_LENGTH))
					{
						//它已经在堆里了。
						value.SetHeapPtr(final_ptr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);
					}
					else if (final_ptr < heap.StackPos + Context.M_ClosurePtr)
					{
						//定义在上一层调用栈的对象, 直接存，并且标记被下一层函数栈引用
						value.SetHeapPtr(final_ptr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);

						if (srcClosure.nextframe_ref_state.scope_ptr > 0 && ((RtMethodScope)Context.GC.Heap[srcClosure.nextframe_ref_state.scope_ptr]).version
							== srcClosure.nextframe_ref_state.version)
						{
							//那个引用帧还在
						}
						else
						{
							//引用已失效
							srcClosure.nextframe_ref_state.scope_ptr = heap.mScopePtr;
							srcClosure.nextframe_ref_state.version = heap.version;
						}


						if (srcClosure.This.ValueType == NaNBoxing.BoxType.HeapPtr && srcClosure.This.HeapKind == (byte)RtHeapTypeKind.INSTANCE)
						{
							//更新this指针
							prepare_savemethodscope_saveinstacne(heap,ref srcClosure.This,heapLocater,false);
						}
#if DEBUG
						else if (srcClosure.This.HeapKind == (byte)RtHeapTypeKind.CLOSURE)
						{
							if (srcClosure.This.HeapPtr < Context.M_ClosurePtr + Context.STACK_LENGTH)
							{
								throw new InvalidOperationException();
							}
						}
						else if (srcClosure.This.HeapKind == (byte)RtHeapTypeKind.ARRAY)
						{
							var _this = Context.GC.Heap[srcClosure.This.HeapPtr];
							if (((RtArray)_this).StoreMode != RtArray.ArrayStoreMode.normal)
							{
								throw new InvalidOperationException();
							}
						}
						else if (srcClosure.This.HeapKind == (byte)RtHeapTypeKind.VECTOR)
						{
							Debug.Assert(srcClosure.This.HeapPtr >= Context.CacheVectorPtr + Context.STACK_LENGTH);
							//throw new InvalidOperationException();
						}
						else if (srcClosure.This.HeapKind == (byte)RtHeapTypeKind.MethodScope)
						{
							throw new InvalidOperationException();
						}
#endif


					}
					else if (final_ptr < heap.StackPos + heap.SlotCount + Context.M_ClosurePtr)
					{
						//存在本层的变量里的对象,直接存，需要更新引用状态。
						value.SetHeapPtr(final_ptr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);

						Debug.Assert(srcClosure.methodscopeslot_ref_state != 0);
						if (srcClosure.methodscopeslot_ref_state == 1)
						{
							srcClosure.methodscopeslot_ref_state = 2;
						}

						if (srcClosure.This.ValueType == NaNBoxing.BoxType.HeapPtr && srcClosure.This.HeapKind == (byte)RtHeapTypeKind.INSTANCE)
						{
							//更新this指针
							prepare_savemethodscope_saveinstacne(heap, ref srcClosure.This, heapLocater, false);
						}
#if DEBUG
						else if (srcClosure.This.HeapKind == (byte)RtHeapTypeKind.CLOSURE)
						{
							if (srcClosure.This.HeapPtr < Context.M_ClosurePtr + Context.STACK_LENGTH)
							{
								throw new InvalidOperationException();
							}
						}
						else if (srcClosure.This.HeapKind == (byte)RtHeapTypeKind.ARRAY)
						{
							var _this = Context.GC.Heap[srcClosure.This.HeapPtr];
							if (((RtArray)_this).StoreMode != RtArray.ArrayStoreMode.normal)
							{
								throw new InvalidOperationException();
							}
						}
						else if (srcClosure.This.HeapKind == (byte)RtHeapTypeKind.VECTOR)
						{
							Debug.Assert(srcClosure.This.HeapPtr >= Context.CacheVectorPtr + Context.STACK_LENGTH);
							//throw new InvalidOperationException();
						}
						else if (srcClosure.This.HeapKind == (byte)RtHeapTypeKind.MethodScope)
						{
							throw new InvalidOperationException();
						}
#endif



					}
					else
					{

						int dstClosurePtr = heapLocater.MemberIndex + heap.StackPos + Context.M_ClosurePtr;
						var dstClosure = (RtClosure)Context.GC.Heap[dstClosurePtr];

						Context.GC.Heap[dstClosurePtr].Type = obj.Type;

						dstClosure.CopyDataFrom(srcClosure, this);
						dstClosure.methodscopeslot_ref_state = 1;


						//srcClosure.HEAPINSTANCE_PTR = dstClosurePtr;
						srcClosure.LinkTo(dstClosure, dstClosurePtr);

						//处理 This 指针
						if (dstClosure.This.ValueType == NaNBoxing.BoxType.HeapPtr)
						{
							bool needupdatescopePtr = dstClosure.ScopePtr == dstClosure.This.HeapPtr;
							//var _this = Context.GC.Heap[dstClosure.This.HeapPtr];
							var _thisKind = (RtHeapTypeKind)dstClosure.This.HeapKind;
							if (_thisKind == RtHeapTypeKind.INSTANCE)
							{

								//if (old.ValueType == NaNBoxing.BoxType.HeapPtr)
								if (!old.IsStruct() && old.ValueType == NaNBoxing.BoxType.HeapPtr && old.HeapKind >= (byte)RtHeapTypeKind.INSTANCE)
								{
									prepare_savemethodscope_beforeSave(heap, old, heapLocater, ref min, ref max, scope_ptr);
								}

								prepare_savemethodscope_saveinstacne(heap, ref dstClosure.This, heapLocater, is_pass_this);

								if (needupdatescopePtr)
								{
									dstClosure.ScopePtr = dstClosure.This.HeapPtr;
								}
							}
							else if (_thisKind == RtHeapTypeKind.CLOSURE)
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
							else if (_thisKind == RtHeapTypeKind.ARRAY)
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
							else if (_thisKind == RtHeapTypeKind.VECTOR)
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
							else if (_thisKind == RtHeapTypeKind.MethodScope)
							{
								throw new InvalidOperationException();
							}
#endif
							
						}


						{
							//处理MethodScope
							if (!((ASMethodBody)obj.Type).Method.__ismethod)
							{
								if (dstClosure.ScopePtr != 0)
								{
									int sptr = dstClosure.ScopePtr;

									RtMethodScope last_scope = null;

								lbl_parent:
									var scope = Context.GC.Heap[sptr];
									if (scope.Kind == RtHeapTypeKind.GLOBAL || scope.Kind == RtHeapTypeKind.CLASS || scope.Kind == RtHeapTypeKind.INSTANCE)
									{
										// Y组合子等：先遇到 global/class/instance，未遇到 heap。与 StoreReturnSlot 第 597 行一致。
										if (last_scope != null)
											last_scope.ParentPtr = sptr;
										else
											dstClosure.ScopePtr = sptr;
									}
									else if (scope != heap)
									{
#if DEBUG
										if (scope.Kind != RtHeapTypeKind.MethodScope)
										{
											throw new InvalidOperationException();
										}
#endif
										if (sptr < Context.M_ClosurePtr + Context.STACK_LENGTH)
										{

											if (((ASMethodBody)scope.Type).Method.Flags.HasFlag(MethodFlags.NeedActivation))
											{
												RtMethodScope cacheMscope = (RtMethodScope)scope;
												var cacheSpan = cacheMscope.__get_slots_internal;

												RtHeapBase heapObj;
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

												RtMethodScope heap_scope = (RtMethodScope)heapObj;
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

												sptr = ((RtMethodScope)scope).ParentPtr;
											}
											else
											{
												sptr = ((RtMethodScope)scope).ParentPtr;
											}
										}
										else
										{
											last_scope = (RtMethodScope)scope;
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

						value.SetHeapPtr(dstClosurePtr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);
					}

				}
				else
				{
					Debug.Assert(!(value.HeapKind == (byte)RtHeapTypeKind.STRING ||
								   value.HeapKind == (byte)RtHeapTypeKind.CLASS ||
								   value.HeapKind == (byte)RtHeapTypeKind.GLOBAL));
					

					
					value = GetSaveValue(value, ref error);
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
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private unsafe void PrepareSaveMethodScope(RtMethodScope heap,  ScopeHeapLocater heapLocater, ref NaNBoxing value, int* m_scope, int* method_scopes, ref ReceiveError error , bool is_pass_this = false)
		{
			if (heap.IsStackSlot)
			{
				NaNBoxing old = heap.ReadSlot(heapLocater.MemberIndex

					);

				if (value.Raw == old.Raw)
				{
					return;
				}

				int min = 0;int max = 0;

				if (old.ValueType == NaNBoxing.BoxType.HeapPtr && old.HeapKind >= (byte)RtHeapTypeKind.INSTANCE)
				{
					Debug.Assert(*method_scopes != 0);
					prepare_savemethodscope_beforeSave( heap ,old,  heapLocater, ref min,ref max,*method_scopes);

				}
				if (value.ValueType == NaNBoxing.BoxType.HeapPtr && value.HeapKind >= (byte)RtHeapTypeKind.INSTANCE)
				{
					//存储阶段
					prepare_savescope_pass(ref value, heap, heapLocater, old, min, max, *method_scopes, ref error, is_pass_this);
				}
			}
			else
			{
				//完全相同结构体可以不分配内存，就地覆盖
				NaNBoxing old = heap.__get_slots_internal[heapLocater.MemberIndex];
				if (CopyIfSameTypeStructAndReplaceSrc(old, ref value))
				{

				}
				else
				{
					value = GetSaveValue(value, ref error);
				}
			}

		}


		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		internal bool CopyIfSameTypeStructAndReplaceSrc(NaNBoxing dst,ref NaNBoxing src)
		{
			if (dst.Raw == src.Raw)
				return true;

			if (dst.IsStruct() && src.IsStruct() )//dst.ValueType == NaNBoxing.BoxType.HeapPtr && src.ValueType == NaNBoxing.BoxType.HeapPtr && dst.HeapKind == src.HeapKind && dst.HeapKind == (byte)RtHeapTypeKind.INSTANCE )
			{
				var oldv = Context.GC.Heap[dst.HeapPtr];
				var newv = Context.GC.Heap[src.HeapPtr];

				//if (oldv.Kind == newv.Kind && oldv.Kind == RtHeapTypeKind.INSTANCE)
				{
					Debug.Assert(((ASInstance)oldv.Type).Flags.HasFlag(ClassFlags.Struct));

					if(oldv.Type == newv.Type)
					{
						((RtInstance)oldv).CopyFrom(newv, Context.player, oldv.Type._link_codescope.TypeLayout.Size);
						src.SetHeapPtr(dst.HeapPtr, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.FLAG_STRUCT);
						return true;
					}
				}
				
			}

			return false;
		}



	}
}
