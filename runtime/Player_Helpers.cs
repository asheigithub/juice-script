using juicescript.ABC;
using juicescript.ABC.Locaters;
using juicescript.runtime.buildin;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static juicescript.NaNBoxing;

namespace juicescript.runtime
{
	public partial class Player
	{
		private unsafe void NEW_INSTANCE(StackLocater typeLocater, StackLocater target ,int stackStPos, int scope_ptr, byte* argementsPtr, int argsCount,
			Span<NaNBoxing> stackslots,ASContainer scopeType,
			RtHeapBase methodscope,
			ref ReceiveError error)
		{

			NaNBoxing type_box = stackslots[typeLocater.index];

			if (type_box.ValueType == BoxType.HeapPtr)
			{

				if (type_box.HeapKind == (byte)RtHeapTypeKind.CLASS)
				{
					RtHeapBase type = Context.GC.Heap[type_box.HeapPtr];
					ASClass @class = (ASClass)((RtScriptClass)type).Meta;
					//构造实例

					RtHeapBase instance;
					NaNBoxing instancePtr = default;

					if (@class.Instance.Flags.HasFlag(ClassFlags.NoConstructor))
					{
						stackslots[target.index].SetNull();
						if (@class != Context.METHOD_CLOSURE)
						{
							RaiseTypeError_Instantiation_non_constructor(ref error);
						}
						return;
					}
					else if (@class.Instance.Flags.HasFlag(ClassFlags.Vector))
					{
						int ptrIndex = stackStPos + target.index;

						//instancePtr = Context.CacheVectorPtr + ptrIndex;
						//instance = Context.GC.Heap[instancePtr];

						instancePtr.SetHeapPtr(Context.CacheVectorPtr + ptrIndex, (byte)RtHeapTypeKind.VECTOR, (byte)HeapKindFlag.NONE);
						instance = Context.GC.Heap[instancePtr.HeapPtr];



						instance.Type = @class.Instance;
						((RtVector)instance).HEAPINSTANCE_PTR = 0;
						((RtVector)instance).element_asclass = @class.Instance._element_class;
						((RtVector)instance).element_type = @class.Instance._element_class == null ? TypeKind.Any : (TypeKind)@class.Instance._element_class.Type_identifier;
						//((RtPayloadVector)instance).GetStore(this).SetBuffer(0);
						((RtVector)instance).GetStore().length = 0;

						stackslots[target.index] = instancePtr; //.SetHeapPtr(instancePtr , (byte)RtHeapTypeKind.VECTOR);

						//throw new NotImplementedException();
					}
					else if (
						(
#if FORCOMPILER
						!IsComputeConstExpr &&
#endif
						@class.Instance.Flags.HasFlag(ClassFlags.CacheAble)
						)
						||
						@class.Instance.Flags.HasFlag(ClassFlags.Struct)
						)
					{
						int ptrIndex = stackStPos + target.index;
						//instancePtr = Context.CacheInstancePtr + ptrIndex;
						instancePtr.SetHeapPtr(InitCacheInstance(@class, ptrIndex, true), (byte)RtHeapTypeKind.INSTANCE, (byte)(@class.Instance.Flags.HasFlag(ClassFlags.Struct) ? HeapKindFlag.FLAG_STRUCT : HeapKindFlag.NONE));

						instance = Context.GC.Heap[instancePtr.HeapPtr];

						//instance = Context.GC.Heap[instancePtr];
						//instance.Type = @class.Instance;

						//((RtPayloadInstance)instance).HEAPINSTANCE_PTR = 0;
						//((RtPayloadInstance)instance).Set_PROPERTY_PTR(0, this);
						//((RtPayloadInstance)instance).Set_PROTOTYPE(((RtPayloadScriptClass)Context.GC.Heap[@class.__instance_index__]).PROTO__PTR, this);
						//((RtPayloadInstance)instance).methodscopeslot_ref_state = 0;

						//CodeScope scope = @class.Instance._link_codescope;
						//if (scope.TypeLayout.Size > 0)
						//{
						//	((RtPayloadInstance)instance).Init(scope, this);
						//}

						//stackslots[target.index].SetHeapPtr(instancePtr);

					}
					else
					{

						Context.GC.CheckGC(ref error);

						if (@class.Type_identifier == (ulong)TypeKind.Array)
						{
							int ext_slot = 0;
							if (argsCount > 0)
							{
								var test = *(StackLocater*)argementsPtr;
								if (test.index == target.index)
								{
									ext_slot = 1;
								}
							}

							if (argsCount <= RtArray.MAX_CACHE_ELEMENT + ext_slot)
							{
								int ptrIndex = stackStPos + target.index;
								instancePtr.SetHeapPtr(Context.CacheArrayPtr + ptrIndex, (byte)RtHeapTypeKind.ARRAY, (byte)HeapKindFlag.NONE);
								instance = Context.GC.Heap[instancePtr.HeapPtr];
								instance.Type = Context.ARRAY.Instance;

								((RtArray)instance).array_len = 0;
								((RtArray)instance).methodscopeslot_ref_state = 0;
								((RtArray)instance).HEAPINSTANCE_PTR = 0;


							}
							else
							{
								instancePtr.SetHeapPtr(Context.GC.AllocArray(out instance, RtArray.ArrayStoreMode.normal), (byte)RtHeapTypeKind.ARRAY, (byte)HeapKindFlag.NONE);
							}
						}
						else if (@class.Type_identifier == (ulong)TypeKind.String)
						{
							if (argsCount == 0)
							{
								instancePtr.SetHeapPtr(EMPTY_STR, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
								stackslots[target.index] = instancePtr; //.SetHeapPtr(instancePtr, (byte)RtHeapTypeKind.STRING);

							}
							else if (argsCount >= 1)
							{
								byte* P = argementsPtr + sizeof(StackLocater) * (Context.STRING.Instance.Constructor.Parameters.Count - 1);
								StackLocater argLocater;
								LoadStackLocater(&argLocater, &P);

								NaNBoxing box = stackslots[argLocater.index];
								ConvertValueType(ref error, box, TypeKind.String, Context.STRING, ref stackslots[target.index], scope_ptr);
								if (error.raised)
								{
									goto flag_handle_error;
								}

							}

							return;

						}
						else if (@class.Type_identifier == (ulong)TypeKind.Boolean)
						{
							if (argsCount == 0)
							{
								stackslots[target.index].SetBoolean(false);
							}
							else if (argsCount >= 1)
							{
								byte* P = argementsPtr + sizeof(StackLocater) * (Context.BOOLEAN.Instance.Constructor.Parameters.Count - 1);
								StackLocater argLocater;
								LoadStackLocater(&argLocater, &P);

								NaNBoxing box = stackslots[argLocater.index];
								ConvertValueType(ref error, box, TypeKind.Boolean, Context.BOOLEAN, ref stackslots[target.index]);
#if DEBUG
								if (error.raised)
								{
									throw new InvalidOperationException();  //转BOOL不会失败
								}
#endif
							}

							return;
						}
						else if (@class.Type_identifier <= 7)
						{
							Debug.Assert(@class.Type_identifier > 0);

							if (argsCount == 0)
							{
								switch ((TypeKind)@class.Type_identifier)
								{
									case TypeKind.SByte:
										stackslots[target.index].SetSByte(0);
										break;
									case TypeKind.Byte:
										stackslots[target.index].SetByte(0);
										break;
									case TypeKind.Short:
										stackslots[target.index].SetShort(0);
										break;
									case TypeKind.UShort:
										stackslots[target.index].SetUShort(0);
										break;
									case TypeKind.Int:
										stackslots[target.index].SetInt(0);
										break;
									case TypeKind.Uint:
										stackslots[target.index].SetUInt(0);
										break;
									default:
#if DEBUG
										throw new InvalidOperationException();
#else
													Environment.FailFast("出错了，这里跑不到");  return;
#endif
								}


							}
							else if (argsCount >= 1)
							{
								byte* P = argementsPtr + sizeof(StackLocater) * (Context.NUMBER.Instance.Constructor.Parameters.Count - 1);
								StackLocater argLocater;
								LoadStackLocater(&argLocater, &P);

								NaNBoxing box = stackslots[argLocater.index];

								box = ToPrimitive(ref error, box, HINT.h_number, scope_ptr, target, target, stackslots, stackStPos, ((RtMethodScope)methodscope).ThisPtr);
								if (error.raised)
								{
									goto flag_handle_error;
								}

								//ConvertValueType(ref error, box, TypeKind.Number, Context.NUMBER, ref stackslots[target.index]);

								switch ((TypeKind)@class.Type_identifier)
								{
									case TypeKind.SByte:
										ConvertValueType(ref error, box, TypeKind.SByte, Context.SBYTE, ref stackslots[target.index]);
										break;
									case TypeKind.Byte:
										ConvertValueType(ref error, box, TypeKind.Byte, Context.BYTE, ref stackslots[target.index]);
										break;
									case TypeKind.Short:
										ConvertValueType(ref error, box, TypeKind.Short, Context.SHORT, ref stackslots[target.index]);
										break;
									case TypeKind.UShort:
										ConvertValueType(ref error, box, TypeKind.UShort, Context.USHORT, ref stackslots[target.index]);
										break;
									case TypeKind.Int:
										ConvertValueType(ref error, box, TypeKind.Int, Context.INT, ref stackslots[target.index]);
										break;
									case TypeKind.Uint:
										ConvertValueType(ref error, box, TypeKind.Uint, Context.UINT, ref stackslots[target.index]);
										break;
									default:
#if DEBUG
										throw new InvalidOperationException();
#else
													Environment.FailFast("出错了，这里跑不到");return;
#endif
								}


								if (error.raised)
								{
									goto flag_handle_error;
								}

							}

							return;



						}
						else if (@class.Type_identifier == (ulong)TypeKind.Number)
						{
							if (argsCount == 0)
							{
								stackslots[target.index].SetNumber(0);
							}
							else if (argsCount >= 1)
							{
								byte* P = argementsPtr + sizeof(StackLocater) * (Context.NUMBER.Instance.Constructor.Parameters.Count - 1);
								StackLocater argLocater;
								LoadStackLocater(&argLocater, &P);

								NaNBoxing box = stackslots[argLocater.index];

								box = ToPrimitive(ref error, box, HINT.h_number, scope_ptr, target, target, stackslots, stackStPos, ((RtMethodScope)methodscope).ThisPtr);
								if (error.raised)
								{
									goto flag_handle_error;
								}

								ConvertValueType(ref error, box, TypeKind.Number, Context.NUMBER, ref stackslots[target.index]);
								if (error.raised)
								{
									goto flag_handle_error;
								}

							}

							return;
						}
						else if (@class.Type_identifier == (ulong)TypeKind.Function)
						{
							if (argsCount > 0)
							{
								RaiseArgumentErrorCountMisMatch(ref error, Context.FUNCTION.Instance.Constructor, 0, argsCount);
								goto flag_handle_error;
							}
							else
							{
								var function = Context.FUNCTION.Constructor;
								function.__ismethod = false;//function  的类型的 Constructor不会被调用，这里就暂借它作为new Function这种操作的 ASMethod

								var define = (ASInstance)Context.FUNCTION.Instance;

								int ptrIndex = stackStPos + target.index;
								int closurePtr = Context.M_ClosurePtr + ptrIndex;

								var closure = Context.GC.Heap[closurePtr];
								closure.Type = function.Body;
								((RtClosure)closure).ScopePtr = scope_ptr;
								((RtClosure)closure).ScopeType = scopeType;
								((RtClosure)closure).This.SetNull();
								((RtClosure)closure)._ref_as_type = define;
								((RtClosure)closure).methodscopeslot_ref_state = 0;
								((RtClosure)closure).HEAPINSTANCE_PTR = 0;
								stackslots[target.index].SetHeapPtr(closurePtr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);


								return;
							}
						}
						else
						{
							instancePtr.SetHeapPtr(Context.GC.AllocInstance(@class.Instance, out instance), (byte)RtHeapTypeKind.INSTANCE, (byte)(@class.Instance.Flags.HasFlag(ClassFlags.Struct) ? HeapKindFlag.FLAG_STRUCT : HeapKindFlag.NONE));
						}

						if (instancePtr.HeapPtr == 0)
						{
							//throw new NotImplementedException("out of memory");
							RaiseOutOfMemory(ref error);
							goto flag_handle_error;
						}

						stackslots[target.index] = instancePtr; //.SetHeapPtr(instancePtr);



					}


					//执行构造函数
					RunMethod(((ASInstance)instance.Type).Constructor, stackslots[target.index], instancePtr.HeapPtr, @class.Instance, (ushort)argsCount, argementsPtr, stackslots, ref error, -1, 0, true);
					if (error.raised)
					{
						goto flag_handle_error;
					}

				}
				else if (type_box.HeapKind == (byte)RtHeapTypeKind.CLOSURE)
				{
					RtHeapBase type = Context.GC.Heap[type_box.HeapPtr];
					NaNBoxing constructor_box = GetSaveValue(type_box, ref error); //构造对象的函数，需要访问proto,所以只能先保存到堆里。
					if (error.raised)
					{
						goto flag_handle_error;
					}
					type_box = constructor_box; //.SetHeapPtr(constructor_box.HeapPtr);
					var constructor_closure = Context.GC.Heap[type_box.HeapPtr];

					if (((ASMethodBody)constructor_closure.Type).Method.__ismethod ||
						((ASMethodBody)constructor_closure.Type).Method.__is_buildin_proto ||
						constructor_closure.Type == Context.FUNCTION.Instance.Constructor.Body
						)
					{
						RaiseTypeError_RunMethodAsConstructor(ref error, ((ASMethodBody)constructor_closure.Type).Method);
						goto flag_handle_error;
					}



					var function_proto = ((RtClosure)constructor_closure).PROTOTYPE(this);

					if (function_proto == 0)
					{
						((RtClosure)constructor_closure).Set_PROTOTYPE(0, this);
						var proto = InvokeReadProperty(ref error, constructor_box, 0, ref stackslots, -1);
						if (error.raised)
						{
							goto flag_handle_error;
						}
						function_proto = proto.HeapPtr;
					}
					///// AIR 运行时在检测到手工把prototype赋值为空的时候会又创建一个Object
					else if (function_proto == -1)
					{
						RtHeapBase proto;
						function_proto = Context.GC.AllocInstance(Context.OBJECT.Instance, out proto);
						if (function_proto == 0)
						{
							RaiseOutOfMemory(ref error);
							goto flag_handle_error;
						}

						((RtClosure)constructor_closure).Set_PROTOTYPE(function_proto, this);
					}

					if (Context.StackPosition >= Context.STACK_LENGTH)
					{
						RaiseStackOverflow(ref error);
						goto flag_handle_error;
					}

					Context.StackPosition++;

					//构造一个dynamic object
					//因为函数有可能返回值，因此这个dynamic object构造在刚给的那个槽上。
					//函数返回后，如果确认用这个对象，需要再把那个槽搬到target上！。

					int ptrIndex = Context.StackPosition - 1; //stackStPos + target.index;
					var instancePtr = Context.CacheInstancePtr + ptrIndex;

					var instance = Context.GC.Heap[instancePtr];
					instance.Type = Context.OBJECT.Instance;

					((RtInstance)instance).HEAPINSTANCE_PTR = 0;
					((RtInstance)instance).Set_PROPERTY_PTR(0, this, Context.OBJECT.Instance);
					((RtInstance)instance).Set_PROTOTYPE(function_proto, this);
					((RtInstance)instance).methodscopeslot_ref_state = 0;

					Context.StackSlots[ptrIndex].SetHeapPtr(instancePtr, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.NONE);


					var constructor = ((ASMethodBody)type.Type).Method;
					NaNBoxing ret_constructor = RunMethod(constructor, Context.StackSlots[ptrIndex],
						((RtClosure)constructor_closure).ScopePtr,
						((RtClosure)constructor_closure).ScopeType, (ushort)argsCount, argementsPtr, stackslots, ref error, stackStPos + target.index, type_box.HeapPtr, true);

					if (error.raised)
					{
						Context.StackPosition--;
						goto flag_handle_error;
					}

					bool move = true;
					if (ret_constructor.ValueType == BoxType.HeapPtr)
					{
						if (Context.GC.Heap[ret_constructor.HeapPtr].Kind == RtHeapTypeKind.STRING)
						{

						}
						else if (ret_constructor.HeapPtr == instancePtr) //原封不动返回，需要拷过来
						{

						}
						else
						{
							move = false;
						}
					}

					if (move) //使用前面的 ,移动过来 
					{
						if (((RtInstance)instance).HEAPINSTANCE_PTR == 0)
						{
							int target_index = stackStPos + target.index;
							var target_instancePtr = Context.CacheInstancePtr + target_index;

							var target_ins = Context.GC.Heap[target_instancePtr];
							target_ins.Type = Context.OBJECT.Instance;

							if (target_ins.Type._link_codescope.TypeLayout.Size != 0)
							{
#if DEBUG
								throw new InvalidOperationException();
#else
													Environment.FailFast("出错了，这里跑不到");  return;
#endif
							}

							((RtInstance)target_ins).HEAPINSTANCE_PTR = 0;
							((RtInstance)target_ins).methodscopeslot_ref_state = 0;
							((RtInstance)target_ins).CopyFrom(instance, this, 0);

							stackslots[target.index].SetHeapPtr(target_instancePtr, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.NONE);
						}
						else
						{
							//这里只可能是在函数里被赋值到了其他变量，那么这时候跟踪到那个变量然后指过去。
							RtInstance src;
							int src_ptr = RtInstance.FindAndUpdateHeapInstancePtr(instancePtr, this, out src);

#if DEBUG
							if (!(src_ptr < Context.CacheInstancePtr + Context.STACK_LENGTH) //堆里
																							 //||
																							 //(src_ptr < Context.CacheInstancePtr + ((RtPayloadMethodScope)methodscope).StackPos +
																							 //((RtPayloadMethodScope)methodscope).SlotCount) //传入
									)
							{

							}
							else
							{
								// constructor前面已经保存到堆了。所以如果它把this保存到外面变量里，则外面的scope也肯定被保存到堆了。
								// 所以这里的object只能在堆里。
								throw new InvalidOperationException();
							}


#endif
							stackslots[target.index].SetHeapPtr(src_ptr, (byte)RtHeapTypeKind.INSTANCE, (byte)(((ASInstance)src.Type).Flags.HasFlag(ClassFlags.Struct) ? HeapKindFlag.FLAG_STRUCT : HeapKindFlag.NONE));
						}
					}


					Context.StackPosition--;
					//throw new NotImplementedException();
				}

				else
				{
#if DEBUG
					RtHeapBase type = Context.GC.Heap[type_box.HeapPtr];
					if (type.Kind == RtHeapTypeKind.MethodScope || type.Kind == RtHeapTypeKind.DYNAMIC_PROPERTYS
						||
						type.Kind == RtHeapTypeKind.STACK_CACHE_OBJ || type.Kind == RtHeapTypeKind.SHAPE
						)
					{
						throw new InvalidOperationException();
					}
#endif
					RaiseTypeError_Instantiation_non_constructor(ref error);
					goto flag_handle_error;
				}


			}
			else
			{
				RaiseTypeError_Instantiation_non_constructor(ref error);
				goto flag_handle_error;
			}

		flag_handle_error:
			;

		}



		private unsafe void DELETE( StackLocater stack, NaNBoxing box, Span<NaNBoxing> stackslots ,int stackStPos, ASMethod method , StackLocater* tmpArgLoc,  Span<char> frame_holdchars,ref ReceiveError error)
		{
			{
				

				if (box.ValueType == NaNBoxing.BoxType.HeapPtr)
				{
					if (box.HeapKind == (byte)RtHeapTypeKind.STACK_CACHE_OBJ)
					{
						RtHeapBase rtHeap = Context.GC.Heap[box.HeapPtr];
						RtStackCache _obj = (RtStackCache)rtHeap;

						if (_obj.RefInstance.ValueType != BoxType.HeapPtr)
						{
							RaiseReferenceError_CanNotDeleteProperty(ref error, _obj.RefInstance);
							goto flag_handle_error;
							//throw new NotImplementedException();
						}
						else
						{
							var refObjKind = _obj.RefInstance.HeapKind;
							if (_obj.searchPropertyName.ValueType == BoxType.HeapPtr || _obj.searchPropertyName.ValueType == BoxType.LocalString) //动态属性
							{

								//string searchName = ((RtPayloadString)Context.GC.Heap[_obj.searchPropertyNamePtr]).Str;

								ReadOnlySpan<char> searchName = frame_holdchars;
								if (_obj.searchPropertyName.ValueType == BoxType.HeapPtr)
								{
									searchName = ((RtString)Context.GC.Heap[_obj.searchPropertyName.HeapPtr]).Str;
								}
								else
								{
									Span<char> temp = frame_holdchars; //stackalloc char[16];//用于从LocalString中提取值
									int l = _obj.searchPropertyName.GetLocalStringChars(temp);
									searchName = temp.Slice(0, l);
								}

								_obj.searchPropertyName.SetUndefined();

								NaNBoxing ns = new NaNBoxing();
								ASNamespace @namespace = null;
								if (_obj.searchNameSpacePtr > 0)
								{
									ns.SetHeapPtr(_obj.searchNameSpacePtr, (byte)RtHeapTypeKind.NAMESPACE, (byte)HeapKindFlag.NONE);
									RtHeapBase ns_instance = Context.GC.Heap[_obj.searchNameSpacePtr];
									@namespace = ((RtNameSpace)ns_instance).ASNamespace;
									_obj.searchNameSpacePtr = 0;
								}

								if (refObjKind == (byte)RtHeapTypeKind.INSTANCE
									&&
										(
											(((ASInstance)Context.GC.Heap[_obj.RefInstance.HeapPtr].Type).Flags & ClassFlags.Sealed) == ClassFlags.Sealed
											||
											(
												@namespace != null &&
												@namespace.Kind != NamespaceKind.Package
											)

										)
									)
								{
									//不可删除，返回false
									stackslots[stack.index].SetBoolean(false);
								}
								else if (refObjKind == (byte)RtHeapTypeKind.VECTOR)
								{
									//不可删除，返回false
									stackslots[stack.index].SetBoolean(false);
								}
								else if (refObjKind == (byte)RtHeapTypeKind.ARRAY &&
										((RtArray)Context.GC.Heap[_obj.RefInstance.HeapPtr]).isArguments()
										&& @namespace == null
										&& "callee".AsSpan().CompareTo(searchName, StringComparison.Ordinal) == 0
									)
								{
									Context.StackSlots[stackStPos - method.Body._link_codescope.Members.Count - 2].SetUndefined();
									stackslots[stack.index].SetBoolean(true);
								}
								else
								{
									RtHeapBase refObj = Context.GC.Heap[_obj.RefInstance.HeapPtr];
									NaNBoxing value; int shape_ptr; int index; RtDynamic prop;
									if (FindDynamicValue(refObj, searchName, out value, out shape_ptr, out index, out prop))
									{
										RtShape shape = (RtShape)Context.GC.Heap[shape_ptr];

										if (shape.Attribute.HasFlag(RtShape.PropertyAttribute.Configurable))
										{
											ChangeTranslation(prop, shape_ptr, ref error);
											if (error.raised)
											{
												goto flag_handle_error;
											}
											//从槽中移除此属性
											prop.Slots.RemoveAt(index);
											stackslots[stack.index].SetBoolean(true);
										}
										else
										{
											//不可删除返回false
											stackslots[stack.index].SetBoolean(false);
										}
									}
									else
									{
										//进入这里，肯定不能正常访问到成员，所以返回true
										stackslots[stack.index].SetBoolean(true);
									}
								}
							}

							else if (_obj.indexer_key.ValueType != BoxType.Fault)
							{
								if (refObjKind == (byte)RtHeapTypeKind.ARRAY)
								{
									RtHeapBase refObj = Context.GC.Heap[_obj.RefInstance.HeapPtr];
#if DEBUG
													if (_obj.indexer_key.ValueType == BoxType.Uint)
#endif
									{
										stackslots[stack.index].SetBoolean(((RtArray)refObj).Delete(_obj.indexer_key.UIntValue, this));
									}
#if DEBUG
													else
													{
														throw new InvalidOperationException();
													}
#endif
								}
								else
								{

#if DEBUG
													if (!(
														(refObjKind == (byte)RtHeapTypeKind.INSTANCE && ((ASInstance)Context.GC.Heap[_obj.RefInstance.HeapPtr].Type).Flags.HasFlag(ClassFlags.Indexer))
														||
														refObjKind == (byte)RtHeapTypeKind.VECTOR
														)
														)
													{
														throw new InvalidOperationException();
													}
#endif


									if (refObjKind == (byte)RtHeapTypeKind.VECTOR)
									{
										if (!RtVector.IsValidIndexType(_obj.indexer_key))
										{
											stackslots[stack.index].SetBoolean(false);
										}
										else
										{
											//throw new NotImplementedException();
											stackslots[stack.index].SetBoolean(true);
										}
									}
									else
									{
										RtHeapBase refObj = Context.GC.Heap[_obj.RefInstance.HeapPtr];

										if (Context.StackPosition + 2 >= Context.STACK_LENGTH)
										{
											RaiseStackOverflow(ref error);
											goto flag_handle_error;
										}

										var argSpan = Context.StackSlots.AsSpan(Context.StackPosition, 1);

										Context.StackPosition += 1;
										Context.GC.CheckGC(ref error);


										var indexer_key = GetSaveValue(_obj.indexer_key, ref error);
										if (error.raised)
										{
											Context.StackPosition -= 1;
											goto flag_handle_error;
										}

										argSpan[0] = indexer_key;

										tmpArgLoc[0].index = 0;


										NaNBoxing _this = new NaNBoxing();
										_this = _obj.RefInstance; //.SetHeapPtr(_obj.RefInstance.HeapPtr);

										NaNBoxing result = RunMethod(((ASInstance)refObj.Type).indexer_delete, _this,
											_obj.RefInstance.HeapPtr, refObj.Type, 1, (byte*)tmpArgLoc, argSpan, ref error, stackStPos + stack.index);

										Context.StackPosition -= 1;
										if (error.raised)
										{
											goto flag_handle_error;
										}
									}

								}
							}
							else if (_obj.trait[0].Kind == TraitKind.Slot || _obj.trait[0].Kind == TraitKind.Constant)
							{
								//不可删除，返回false
								stackslots[stack.index].SetBoolean(false);
							}
#if DEBUG
											else if (_obj.trait[0].Kind == TraitKind.Method && _obj.trait[1] == null)
											{
												throw new InvalidOperationException();
											}
#endif
							else if (_obj.trait[0].Kind == TraitKind.Getter)
							{
								stackslots[stack.index].SetBoolean(false);
							}
#if DEBUG
											else
											{
												throw new InvalidOperationException();
												//throw new NotImplementedException("方法的引用未实现");
											}
#endif
						}
					}
					else if (box.HeapKind == (byte)RtHeapTypeKind.CLOSURE)
					{
						//不可删除，返回false
						stackslots[stack.index].SetBoolean(false);
					}
					else if (box.HeapKind == (byte)RtHeapTypeKind.INSTANCE)
					{
						/*
						 * //CHECK#2
						 *  if (delete new Object() !== true) {
						 *	  throw new Error('#2: delete new Object() === true');
						 *	}
						 * */

						stackslots[stack.index].SetBoolean(true);
					}
#if DEBUG
									else
									{
										throw new InvalidOperationException();
									}
#endif
				}
				else
				{
					//直接返回true
					stackslots[stack.index].SetBoolean(true);
				}

			}

		flag_handle_error:
			;

		}




		private void GET_IN(StackLocater v1,StackLocater v2, StackLocater dst, Span<NaNBoxing> stackslots, Span<char> frame_holdchars, int stackStPos, int scope_ptr , RtHeapBase methodscope,ref ReceiveError error)
		{
			var name_v = stackslots[v1.index];
			NaNBoxing name_n = ToPrimitive(ref error, name_v, HINT.h_string, scope_ptr, dst, dst, stackslots, stackStPos, ((RtMethodScope)methodscope).ThisPtr);
			if (error.raised)
			{
				goto flag_handle_error;
			}

			Span<char> buffers = frame_holdchars;
			ReadOnlySpan<char> name = Extensions.GetPrimitiveValueToString(this, name_n, buffers);

			var type = stackslots[v2.index];
			bool isvaluebox = false;
			if (type.ValueType != BoxType.HeapPtr)
			{
				switch (type.ValueType)
				{
					case BoxType.Number:
						type.SetHeapPtr(Context.NUMBER.__instance_index__, (byte)RtHeapTypeKind.CLASS, (byte)HeapKindFlag.NONE); isvaluebox = true;
						break;
					case BoxType.Undefined:
						RaiseTypeError_ATermUndefined(ref error);
						goto flag_handle_error;
					case BoxType.Null:
						RaiseTypeError_AccessNull(ref error);
						goto flag_handle_error;
					case BoxType.Boolean:
						type.SetHeapPtr(Context.BOOLEAN.__instance_index__, (byte)RtHeapTypeKind.CLASS, (byte)HeapKindFlag.NONE); isvaluebox = true;
						break;
					case BoxType.Int:
						type.SetHeapPtr(Context.INT.__instance_index__, (byte)RtHeapTypeKind.CLASS, (byte)HeapKindFlag.NONE); isvaluebox = true;
						break;
					case BoxType.Uint:
						type.SetHeapPtr(Context.UINT.__instance_index__, (byte)RtHeapTypeKind.CLASS, (byte)HeapKindFlag.NONE); isvaluebox = true;
						break;
					case BoxType.Sbyte:
						type.SetHeapPtr(Context.SBYTE.__instance_index__, (byte)RtHeapTypeKind.CLASS, (byte)HeapKindFlag.NONE); isvaluebox = true;
						break;
					case BoxType.Byte:
						type.SetHeapPtr(Context.BYTE.__instance_index__, (byte)RtHeapTypeKind.CLASS, (byte)HeapKindFlag.NONE); isvaluebox = true;
						break;
					case BoxType.Short:
						type.SetHeapPtr(Context.SHORT.__instance_index__, (byte)RtHeapTypeKind.CLASS, (byte)HeapKindFlag.NONE); isvaluebox = true;
						break;
					case BoxType.UShort:
						type.SetHeapPtr(Context.USHORT.__instance_index__, (byte)RtHeapTypeKind.CLASS, (byte)HeapKindFlag.NONE); isvaluebox = true;
						break;
					case BoxType.Float:
						type.SetHeapPtr(Context.FLOAT.__instance_index__, (byte)RtHeapTypeKind.CLASS, (byte)HeapKindFlag.NONE); isvaluebox = true;
						break;
					case BoxType.HeapPtr:
					case BoxType.Fault:
					default:
						break;
				}
			}

			var find =
				(ASContainer type, ReadOnlySpan<char> name, int proto) =>
				{
					if (ObjectImpl.Find_ASContainer_Prop(type, name))
					{
						return true;
					}
					else
					{
						int steps = 0;
						while (proto != 0 && steps < 32)
						{
							var proto_obj = Context.GC.Heap[proto];

							if (proto_obj.Kind != RtHeapTypeKind.VECTOR)
							{
								NaNBoxing value; int shape; int matchslot; RtDynamic prop;
								if (FindDynamicValue(proto_obj, name, out value, out shape, out matchslot, out prop))
								{
									return true;
								}
							}
							proto = GetProtoPtr(proto_obj);
							steps++;
						}
						return false;
					}
				};



			var obj = Context.GC.Heap[type.HeapPtr];
			switch (obj.Kind)
			{
				case RtHeapTypeKind.CLASS:
					{
						var @class = ((RtScriptClass)obj).Meta;
						if (find(@class, name, 0) || find(((ASClass)@class).Instance, name, 0))
						{
							stackslots[dst.index].SetBoolean(true);
						}
						else if (!isvaluebox) // "F" in Number  ,proto是Class
						{
							NaNBoxing value; int shape; int matchslot; RtDynamic prop;
							if (FindDynamicValue(obj, name, out value, out shape, out matchslot, out prop))
							{
								stackslots[dst.index].SetBoolean(true);
							}
							else
							{
								bool found = false;

								int proto = Context.CLASS.__instance_index__;
								int steps = 0;
								while (proto != 0 && steps < 32)
								{
									var proto_obj = Context.GC.Heap[proto];

									if (FindDynamicValue(proto_obj, name, out value, out shape, out matchslot, out prop))
									{
										found = true;
										break;
									}
									proto = GetProtoPtr(proto_obj);
									steps++;
								}
								stackslots[dst.index].SetBoolean(found);
							}
						}
						else // Number.prototype["F"]=1; "F" in 33.0  这种
						{
							bool found = false;
							int proto = ((RtScriptClass)obj).PROTO__PTR;
							int steps = 0;
							while (proto != 0 && steps < 32)
							{
								var proto_obj = Context.GC.Heap[proto];
								NaNBoxing value; int shape; int matchslot; RtDynamic prop;
								if (FindDynamicValue(proto_obj, name, out value, out shape, out matchslot, out prop))
								{
									found = true;
									break;
								}
								proto = GetProtoPtr(proto_obj);
								steps++;
							}
							stackslots[dst.index].SetBoolean(found);

						}

						break;
					}
				case RtHeapTypeKind.GLOBAL:
					{
						stackslots[dst.index].SetBoolean(find(Context.OBJECT.Instance, name, type.HeapPtr));
					}
					break;
				case RtHeapTypeKind.STRING:
					{
						stackslots[dst.index].SetBoolean(find(Context.STRING.Instance, name, type.HeapPtr));
					}
					break;
				case RtHeapTypeKind.INSTANCE:
					{
						if (((ASInstance)obj.Type).indexer_get != null)
						{
							if (Context.StackPosition + 1 >= Context.STACK_LENGTH)
							{
								RaiseStackOverflow(ref error);
								goto flag_handle_error;
							}

							var argSpan = Context.StackSlots.AsSpan(Context.StackPosition, 1);
							argSpan[0] = name_n;
							StackLocater argLoc = new StackLocater() { index = 0 };

							Context.StackPosition++;

							NaNBoxing _this = type;

							NaNBoxing find_by_index = default;

							unsafe
							{
								Context.StackPosition++;

								RunMethod(((ASInstance)obj.Type).indexer_get, _this,
									type.HeapPtr, obj.Type, 1, (byte*)&argLoc, argSpan, ref error, Context.StackPosition - 1);
								find_by_index = Context.StackSlots[Context.StackPosition - 1];
								Context.StackPosition--;
							}

							Context.StackPosition--;
							if (error.raised)
							{
								goto flag_handle_error;
							}

							if (find_by_index.ValueType == BoxType.Fault)
							{
								stackslots[dst.index].SetBoolean(find(obj.Type, name, type.HeapPtr));
							}
							else
							{
								stackslots[dst.index].SetBoolean(true);
							}

						}
						else
						{
							stackslots[dst.index].SetBoolean(find(obj.Type, name, type.HeapPtr));
						}
					}
					break;
				case RtHeapTypeKind.NAMESPACE:
					{
						stackslots[dst.index].SetBoolean(find(Context.NAMESPACE.Instance, name, type.HeapPtr));
					}
					break;
				case RtHeapTypeKind.ARRAY:
					{
						Debug.Assert(name_n.ValueType != BoxType.Fault);

						uint index;

						switch (name_n.ValueType)
						{
							case BoxType.Number:
								if (Math.Truncate(name_n.Number) == name_n.Number && name_n.Number >= 0 && name_n.Number < uint.MaxValue)
								{
									index = (uint)name_n.Number;
								}
								else
								{
									goto lbl_find_name;
								}
								break;
							case BoxType.Int:
								if (name_n.IntValue >= 0)
								{
									index = (uint)name_n.IntValue;
								}
								else
								{
									goto lbl_find_name;
								}
								break;
							case BoxType.Uint:
								index = name_n.UIntValue;
								break;
							case BoxType.Sbyte:
								if (name_n.SByteValue >= 0)
								{
									index = (uint)name_n.SByteValue;
								}
								else
								{
									goto lbl_find_name;
								}
								break;
							case BoxType.Byte:
								index = name_n.ByteValue;
								break;
							case BoxType.Short:
								if (name_n.ShortValue >= 0)
								{
									index = (uint)name_n.ShortValue;
								}
								else
								{
									goto lbl_find_name;
								}
								break;
							case BoxType.UShort:
								index = name_n.UShortValue;
								break;
							case BoxType.Float:

								if (MathF.Truncate(name_n.FloatValue) == name_n.FloatValue && name_n.FloatValue >= 0 && name_n.FloatValue < uint.MaxValue)
								{
									index = (uint)name_n.FloatValue;
								}
								else
								{
									goto lbl_find_name;
								}

								break;

							case BoxType.Undefined:
							case BoxType.Null:
							case BoxType.Boolean:
							case BoxType.HeapPtr:
							default:
								goto lbl_find_name;

						}

						bool isoutofindex_or_ishole;
						NaNBoxing result = LoadSlotFromArray(index, obj, out isoutofindex_or_ishole);

						if (result.ValueType != BoxType.Fault)
						{
							stackslots[dst.index].SetBoolean(true);
						}
						else
						{
							stackslots[dst.index].SetBoolean(false);
						}

						break;

					lbl_find_name:

						stackslots[dst.index].SetBoolean(find(Context.ARRAY.Instance, name, type.HeapPtr));

					}
					break;
				case RtHeapTypeKind.VECTOR:
					{

						int index;
						if (((RtVector)obj).GetStore(this).IsValidIndexRange(name_n, out index))
						{
							stackslots[dst.index].SetBoolean(true);
						}
						else if (IsNumeric(name_n))
						{
							stackslots[dst.index].SetBoolean(false);
						}
						else
						{
							stackslots[dst.index].SetBoolean(find(obj.Type, name, type.HeapPtr));
						}

					}
					break;
				case RtHeapTypeKind.CLOSURE:
					{
						stackslots[dst.index].SetBoolean(find(Context.FUNCTION.Instance, name, type.HeapPtr));
					}
					break;
				case RtHeapTypeKind.STACK_CACHE_OBJ:
				case RtHeapTypeKind.DYNAMIC_PROPERTYS:
				case RtHeapTypeKind.SHAPE:
				case RtHeapTypeKind.MethodScope:
				default:
#if DEBUG
					throw new InvalidOperationException();
#else
										Environment.FailFast("出错了，这里跑不到");
										stackslots[dst.index].SetBoolean(false);
										break;
#endif
			}

		flag_handle_error:
			;


		}

		private void GET_INSTANCEOF(StackLocater v1, StackLocater v2, StackLocater dst, Span<NaNBoxing> stackslots, 
			ref ReceiveError error
			)
		{
			do
			{


				var type = stackslots[v2.index];
				if (type.ValueType != BoxType.HeapPtr)
				{
					RaiseTypeError_InstanceOf(ref error);
					goto flag_handle_error;
				}

				int fun_proto;
				int o_proto;


				if (type.HeapKind == (byte)RtHeapTypeKind.CLASS)
				{
					var type_instance = Context.GC.Heap[type.HeapPtr];
					var @typeclass = (ASClass)((RtScriptClass)type_instance).Meta;
					if (typeclass.Instance.Flags.HasFlag(ClassFlags.NoConstructor) && !typeclass.Instance.IsInterface)
					{
						RaiseTypeError_InstanceOf(ref error);
						goto flag_handle_error;
					}


					var v = stackslots[v1.index];

					switch (v.ValueType)
					{

						case BoxType.Undefined:
						case BoxType.Null:
							stackslots[dst.index].SetBoolean(false);
							break;
						case BoxType.Boolean:
							stackslots[dst.index].SetBoolean(typeclass == Context.BOOLEAN || typeclass == Context.OBJECT);
							break;
						case BoxType.Number:
						case BoxType.Int:
						case BoxType.Uint:
						case BoxType.Sbyte:
						case BoxType.Byte:
						case BoxType.Short:
						case BoxType.UShort:
						case BoxType.Float:
							stackslots[dst.index].SetBoolean(Is(v, typeclass)); // 已改为按数值范围处理
							break;
						case BoxType.LocalString:
							// LocalString应该被视为String类型
							stackslots[dst.index].SetBoolean(typeclass == Context.STRING || typeclass == Context.OBJECT);
							break;
						case BoxType.HeapPtr:
							{

								switch ((RtHeapTypeKind)v.HeapKind)
								{
									case RtHeapTypeKind.CLASS:
										stackslots[dst.index].SetBoolean(typeclass == Context.OBJECT || typeclass == Context.CLASS);
										break;
									case RtHeapTypeKind.GLOBAL:
										stackslots[dst.index].SetBoolean(typeclass == Context.OBJECT);
										break;
									case RtHeapTypeKind.STRING:
										stackslots[dst.index].SetBoolean(typeclass == Context.STRING || typeclass == Context.OBJECT);
										break;
									case RtHeapTypeKind.INSTANCE:
										{
											var v_instance = Context.GC.Heap[v.HeapPtr];
											bool pass = typeclass == Context.OBJECT ||
												v_instance.Type == typeclass.Instance ||
												Extensions.IsExtend((ASInstance)v_instance.Type, typeclass.Instance) ||
												Extensions.IsImplements((ASInstance)v_instance.Type, typeclass.Instance);

											if (pass || ((ASInstance)v_instance.Type).Flags.HasFlag(ClassFlags.Sealed))
											{
												stackslots[dst.index].SetBoolean(
													pass
													);
											}
											else
											{
												o_proto = ((RtInstance)v_instance).PROTOTYPE(this, (ASInstance)v_instance.Type);
												fun_proto = ((RtScriptClass)Context.GC.Heap[typeclass.__instance_index__]).PROTO__PTR;
												goto lbl_do_proto;
											}
										}
										break;
									case RtHeapTypeKind.NAMESPACE:
										stackslots[dst.index].SetBoolean(typeclass == Context.OBJECT || ((ASClass)typeclass).Type_identifier == (ulong)TypeKind.Namespace);
										break;
									case RtHeapTypeKind.ARRAY:
										stackslots[dst.index].SetBoolean(typeclass == Context.OBJECT || typeclass == Context.ARRAY);
										break;
									case RtHeapTypeKind.VECTOR:
										{
											if (typeclass == Context.OBJECT || typeclass == Context.VECTOR)
											{
												stackslots[dst.index].SetBoolean(true);
												break;
											}

											if (typeclass.Instance.Flags.HasFlag(ClassFlags.Vector))
											{
												if (typeclass.Instance._element_class == null || typeclass.Instance._element_class == Context.OBJECT)
												{
													stackslots[dst.index].SetBoolean(true);
													break;
												}
												var v_instance = Context.GC.Heap[v.HeapPtr];
												if (((RtVector)v_instance).element_asclass == typeclass.Instance._element_class)
												{
													stackslots[dst.index].SetBoolean(true);
													break;
												}
											}

										}

										stackslots[dst.index].SetBoolean(false);

										//stackslots[dst.index].SetBoolean(typeclass == Context.OBJECT || typeclass.Instance == v_instance.Type );
										//throw new NotImplementedException();
										break;
									case RtHeapTypeKind.CLOSURE:
										stackslots[dst.index].SetBoolean(typeclass == Context.OBJECT || typeclass == Context.FUNCTION);
										break;
#if DEBUG
									case RtHeapTypeKind.STACK_CACHE_OBJ:
									case RtHeapTypeKind.DYNAMIC_PROPERTYS:
									case RtHeapTypeKind.SHAPE:
									case RtHeapTypeKind.MethodScope:
									default:
										throw new InvalidOperationException();
#endif
								}


							}

							break;
#if DEBUG
						case BoxType.Fault:
						default:
							throw new InvalidOperationException();
#endif
					}



				}
				else if (type.HeapKind == (byte)RtHeapTypeKind.CLOSURE)
				{
					var v = stackslots[v1.index];
					if (IsPrimitive(v))
					{
						stackslots[dst.index].SetBoolean(false);
						break;
					}
#if DEBUG
					if (v.ValueType != BoxType.HeapPtr)
						throw new InvalidOperationException();
#endif

					if (v.HeapKind != (byte)RtHeapTypeKind.INSTANCE)
					{
						stackslots[dst.index].SetBoolean(false);
						break;
					}
					var obj = Context.GC.Heap[v.HeapPtr];
					if (((ASInstance)obj.Type).Flags.HasFlag(ClassFlags.Sealed))
					{
						stackslots[dst.index].SetBoolean(false);
						break;
					}

					var type_instance = Context.GC.Heap[type.HeapPtr];
					int obj_proto = ((RtInstance)obj).PROTOTYPE(this, (ASInstance)obj.Type);

					int proto_ptr;
					if (((ASMethodBody)type_instance.Type).Method.__ismethod)
					{
						proto_ptr = ((RtScriptClass)Context.GC.Heap[Context.METHOD_CLOSURE.__instance_index__]).PROTO__PTR;
					}
					else
					{
						proto_ptr = ((RtClosure)type_instance).PROTOTYPE(this);
						if (proto_ptr <= 0) //默认，指向FUNCTION的proto
						{
							//按test262,此处应该跑TypeError(Function has non-object prototype 'undefined' in instanceof check)
							//RaiseTypeError_InstanceOf(ref error);
							//goto flag_handle_error;
							proto_ptr = ((RtScriptClass)Context.GC.Heap[Context.FUNCTION.__instance_index__]).PROTO__PTR;
							if (proto_ptr <= 0) //Function.prototype是一个function (){},所以如果还是空白的，就跳到Object.proto里去。
							{
								proto_ptr = ((RtScriptClass)Context.GC.Heap[Context.OBJECT.__instance_index__]).PROTO__PTR;
							}
						}
					}

					fun_proto = proto_ptr;
					o_proto = obj_proto;

					goto lbl_do_proto;
				}
				else
				{
					RaiseTypeError_InstanceOf(ref error);
					goto flag_handle_error;
				}

				break;

			lbl_do_proto:
				{
					RtHeapBase obj;

					bool instanceof = false;
					int steps = 0;
					while (o_proto != 0 && steps++ < 32)
					{
						if (o_proto == fun_proto)
						{
							instanceof = true;
							break;
						}
						else
						{
							obj = Context.GC.Heap[o_proto];
							o_proto = GetProtoPtr(obj);

						}
					}

					stackslots[dst.index].SetBoolean(instanceof);

				}
			}
			while (false);

		flag_handle_error:
			;


		}


		private unsafe void Ld_memberInitValue(RtHeapBase methodscope, int* method_scopes,int scope_ptr , ScopeHeapLocater heapLocater,ASContainer scopeType,ref ReceiveError error)
		{

			var s = methodscope; //Context.GC.Heap[scope_ptr];
			int* m_scope = method_scopes;
			*m_scope++ = scope_ptr;

		label_method_parent:

			switch (s.Kind)
			{
				case RtHeapTypeKind.CLASS:
				case RtHeapTypeKind.GLOBAL:
					{
						RtScriptClass heap = (RtScriptClass)s;
#if DEBUG
						if (heap.Meta._link_codescope.index != heapLocater.ScopeIndex)
						{
							throw new InvalidOperationException();
						}
#endif
						ASTrait t = heap.Meta._link_codescope.Members[heapLocater.MemberIndex].trait;
						heap.SetSlot(t.Value.initValue.Value, heapLocater.MemberIndex);
					}
					break;

				case RtHeapTypeKind.INSTANCE:
					{
#if DEBUG
						//这里只会在构造函数中进去，所以下面判断成立
						if (scopeType._link_codescope.index != heapLocater.ScopeIndex)
						{
							throw new InvalidOperationException();
						}
#endif
						RtInstance heap = (RtInstance)s;

						ASTrait t = scopeType._link_codescope.Members[heapLocater.MemberIndex].trait;
						heap.SetSlot(t.Value.initValue.Value, heapLocater.MemberIndex, scopeType._link_codescope, this);

					}
					break;
				case RtHeapTypeKind.MethodScope:
					{
						if (s.Type._link_codescope.index != heapLocater.ScopeIndex)
						{
							int parentPtr = ((RtMethodScope)s).ParentPtr;
							s = Context.GC.Heap[parentPtr];
							*m_scope++ = parentPtr;
							goto label_method_parent;
						}
						else
						{
							ASTrait t = s.Type._link_codescope.Members[heapLocater.MemberIndex].trait;

							RtMethodScope heap = (RtMethodScope)s;
							NaNBoxing value = t.Value.initValue.Value;

							if (t.TypeKind.IsHeapType())
							{

								PrepareSaveMethodScope(heap, ref heapLocater, ref value, m_scope, method_scopes, ref error);
#if DEBUG
								if (error.raised) //读取初始化值这里是不可能进入出错分支的。
								{
									throw new InvalidOperationException();
								}
#endif
							}
							heap.SetSlot(value, heapLocater.MemberIndex);
						}
					}
					break;
				default:
#if DEBUG
					throw new InvalidOperationException();
#else
										Environment.FailFast("出错了，这里跑不到");return;
#endif
			}



		}









		private unsafe void Ld_RTQNameL_Ref(StackLocater src, StackLocater _ns ,StackLocater _name, StackLocater stack, Span<char> frame_holdchars, RtHeapBase methodscope , Span<NaNBoxing> stackslots, int stackStPos,int scope_ptr, ref ReceiveError error )
		{

			NaNBoxing instance_box;
			RtHeapTypeKind kind;


			if (src.index >= 0)
			{
				instance_box = stackslots[src.index];
				kind = (RtHeapTypeKind)(instance_box.ValueType == BoxType.HeapPtr ? instance_box.HeapKind : 255);
			}
			else
			{
				ReadInstanceFromStacklocater(ref error, src, stackslots, stackStPos, scope_ptr, out kind, out instance_box);
				if (error.raised)
				{
					goto flag_handle_error;
				}
			}
			//var ns = LoadValue(stackslots[_ns.index], ref error, ref stackslots,stackStPos);
			//if (error.raised)
			//{
			//    goto flag_handle_error;
			//}
			var ns = stackslots[_ns.index];

			//var name = LoadValue(stackslots[_name.index], ref error, ref stackslots, stackStPos);
			//if (error.raised)
			//{
			//    goto flag_handle_error;
			//}
			var name = stackslots[_name.index];

			ASNamespace searchNs = null;

			Span<char> searchNameBuffer = frame_holdchars;
			ReadOnlySpan<char> searchName = searchNameBuffer;
			if (ns.ValueType != NaNBoxing.BoxType.HeapPtr)
			{
				goto lbl_rtqname_ns_not_a_namespace;
			}
			else
			{
				RtHeapBase ns_instance = Context.GC.Heap[ns.HeapPtr];
				if (ns_instance.Kind == RtHeapTypeKind.NAMESPACE)
				{
					searchNs = ((RtNameSpace)ns_instance).ASNamespace;

				}
				else
				{
					goto lbl_rtqname_ns_not_a_namespace;
				}
			}

			if (name.ValueType != NaNBoxing.BoxType.HeapPtr)
			{
				//throw new NotImplementedException("cast to string");
				searchName = Extensions.GetPrimitiveValueToString(this, name, searchNameBuffer);
			}
			else
			{
				if (name.HeapKind == (byte)RtHeapTypeKind.STRING)
				{
					RtHeapBase name_instance = Context.GC.Heap[name.HeapPtr];
					searchName = ((RtString)name_instance).Str;
				}
				else if (name.HeapKind == (byte)RtHeapTypeKind.NAMESPACE)
				{
					RtHeapBase name_instance = Context.GC.Heap[name.HeapPtr];
					var n = ((RtNameSpace)name_instance).ASNamespace;
					if (!string.IsNullOrEmpty(n.def_uri))
					{
						searchName = n.def_uri;
					}
					else
					{
						searchName = n.Name;
					}
				}
				else
				{
					Context.GC.CheckGC(ref error);
					if (Context.StackPosition >= Context.STACK_LENGTH)
					{
						RaiseStackOverflow(ref error);
						goto flag_handle_error;
					}

					ref NaNBoxing conv = ref Context.StackSlots[Context.StackPosition];
					Context.StackPosition++;
					ConvertValueType(ref error, name, TypeKind.String, Context.STRING, ref conv, scope_ptr, ((RtMethodScope)methodscope).ThisPtr);
					if (error.raised)
					{
						Context.StackPosition--;
						goto flag_handle_error;
					}

					searchName = Extensions.GetPrimitiveValueToString(this, conv, searchNameBuffer);

					//throw new NotImplementedException("cast to string");
				}
			}

			RtHeapBase instance = null;
			var c_scope = methodscope; //Context.GC.Heap[scope_ptr];
#if DEBUG
			if (c_scope.Kind != RtHeapTypeKind.MethodScope)
			{
				throw new InvalidOperationException();
			}
#endif

			var ns_set = c_scope.Type._link_codescope.NamespaceSet;

			bool deepsearch = false;//如果是从instance的methodscope开始查找说明要继续查找静态成员-基类静态成员
			NaNBoxing instancePtr = default; instancePtr.SetNull();
			NaNBoxing o_instancePtr = default; o_instancePtr.SetNull();
			RtHeapBase o_instance = null;

			CodeScope primitive_codescope = null;

			switch (instance_box.ValueType)
			{
				case NaNBoxing.BoxType.Null:
					Context.GC.CheckGC(ref error);
					RaiseTypeError_AccessNull(ref error);
					goto flag_handle_error;
				case NaNBoxing.BoxType.Undefined:
					Context.GC.CheckGC(ref error);
					RaiseTypeError_ATermUndefined(ref error);
					goto flag_handle_error;
				case NaNBoxing.BoxType.HeapPtr:
					break;
				case NaNBoxing.BoxType.Sbyte:
					primitive_codescope = Context.SBYTE.Instance._link_codescope;
					goto lbl_primitive;
				case NaNBoxing.BoxType.Byte:
					primitive_codescope = Context.BYTE.Instance._link_codescope;
					goto lbl_primitive;
				case NaNBoxing.BoxType.Short:
					primitive_codescope = Context.SHORT.Instance._link_codescope;
					goto lbl_primitive;
				case NaNBoxing.BoxType.UShort:
					primitive_codescope = Context.USHORT.Instance._link_codescope;
					goto lbl_primitive;
				case NaNBoxing.BoxType.Int:
					primitive_codescope = Context.INT.Instance._link_codescope;
					goto lbl_primitive;
				case NaNBoxing.BoxType.Uint:
					primitive_codescope = Context.UINT.Instance._link_codescope;
					goto lbl_primitive;
				case NaNBoxing.BoxType.Float:
					primitive_codescope = Context.FLOAT.Instance._link_codescope;
					goto lbl_primitive;
				case NaNBoxing.BoxType.Number:
					primitive_codescope = Context.NUMBER.Instance._link_codescope;
					goto lbl_primitive;
				case NaNBoxing.BoxType.Boolean:
					primitive_codescope = Context.BOOLEAN.Instance._link_codescope;
					goto lbl_primitive;
#if DEBUG
				case NaNBoxing.BoxType.Fault:
				default:
					throw new InvalidOperationException();
#endif
			}

			instance = Context.GC.Heap[instance_box.HeapPtr];
			o_instance = instance;

			instancePtr = instance_box;
			//RTQName查找 -- 由于自定义命名空间只会在class级别定义，所以实际上只需要查找 静态成员 或者 类成员-继承的类成员-静态成员-基类静态成员找即可。
			while (instance.Kind == RtHeapTypeKind.MethodScope)
			{
				int parent = ((RtMethodScope)instance).ParentPtr;
				instance = Context.GC.Heap[parent];

				instancePtr.SetHeapPtr(parent, (byte)instance.Kind, (byte)(instance.Kind == RtHeapTypeKind.INSTANCE ? (((ASInstance)instance.Type).Flags.HasFlag(ClassFlags.Struct) ? HeapKindFlag.FLAG_STRUCT : HeapKindFlag.NONE) : HeapKindFlag.NONE)); //= ((RtMethodScope)instance).ParentPtr;

				deepsearch = true;
			}
			o_instancePtr = instancePtr;




		lbl_primitive:
			var thisPtr = ((RtMethodScope)methodscope).ThisPtr;
			bool issameorinherit = thisPtr.ValueType == NaNBoxing.BoxType.HeapPtr && instance != null &&
				thisPtr.HeapKind == (byte)instance.Kind
				//Context.GC.Heap[thisPtr.HeapPtr].Kind == instance.Kind
				&&
				thisPtr.HeapKind == (byte)RtHeapTypeKind.INSTANCE
				//Context.GC.Heap[thisPtr.HeapPtr].Kind == RtHeapTypeKind.INSTANCE
				&&
				((ASInstance)instance.Type).IsExtend((ASInstance)Context.GC.Heap[thisPtr.HeapPtr].Type)
			;

			//lambda search member
			var searchmember = (CodeScope scope, ASNamespace ns, ReadOnlySpan<char> name, out int index) =>
			{
				for (int i = 0; i < scope.Members.Count; i++)
				{
					var member = scope.Members[i];
					if (name.CompareTo(member.QName.Name, StringComparison.Ordinal) == 0 && !((ns.Kind == NamespaceKind.Protected || ns.Kind == NamespaceKind.StaticProtected) && !issameorinherit) &&
						(
							member.QName.Namespace == ns
							||
							(
								ns.Kind == NamespaceKind.PackageInternal && ns.def_uri == null
								&&
								member.QName.Namespace.Kind == NamespaceKind.PackageInternal &&
								member.QName.Namespace.def_uri == null &&
								member.DefineAt.QName.Namespace == ns_set.Namespaces[0]
							)
							||
							(
								(ns.Kind == NamespaceKind.PackageInternal || ns.Kind == NamespaceKind.Private || ns.Kind == NamespaceKind.Protected)
								&&
								(string.IsNullOrEmpty(ns.Name) || ns.Kind == NamespaceKind.Private)
								&&
								ns_set.Namespaces.Contains(member.QName.Namespace)
								&&
								(
									member.QName.Namespace.Kind == ns.Kind
									||
									(member.QName.Namespace.Kind == NamespaceKind.StaticProtected && ns.Kind == NamespaceKind.Protected)
								)
								&&
								member.QName.Namespace.def_uri == null
							)

						)

					)
					{
						index = i;
						return member;
					}
				}

				index = -1;
				return null;
			};


			var searchvtable = (VTable vtable, ASNamespace ns, ReadOnlySpan<char> name, out int m_idx, out int g_idx, out int s_idx) =>
			{
				m_idx = -1; g_idx = -1; s_idx = -1;
				for (int i = 0; i < vtable.Items.Count; i++)
				{
					var v = vtable.Items[i];

					if (name.CompareTo(v.Trait.QName.Name, StringComparison.Ordinal) == 0 && !((ns.Kind == NamespaceKind.Protected || ns.Kind == NamespaceKind.StaticProtected) && !issameorinherit) &&
					(
						v.Trait.QName.Namespace == ns
						||
						(
							ns.Kind == NamespaceKind.PackageInternal && ns.def_uri == null
							&&
							v.Trait.QName.Namespace.Kind == NamespaceKind.PackageInternal &&
							v.Trait.QName.Namespace.def_uri == null &&
							v.DefineAt.QName.Namespace == ns_set.Namespaces[0]
						)
						||
						(
							(ns.Kind == NamespaceKind.PackageInternal || ns.Kind == NamespaceKind.Private || ns.Kind == NamespaceKind.Protected)
							&&
							(string.IsNullOrEmpty(ns.Name) || ns.Kind == NamespaceKind.Private)
							&&
							ns_set.Namespaces.Contains(v.Trait.QName.Namespace)
							&&
							(
								v.Trait.QName.Namespace.Kind == ns.Kind
								||
								(v.Trait.QName.Namespace.Kind == NamespaceKind.StaticProtected && ns.Kind == NamespaceKind.Protected)
							)
							&&
							v.Trait.QName.Namespace.def_uri == null
						)

					)
					)
					{
						if (v.Trait.Kind == TraitKind.Method)
						{
							m_idx = i;
							break;
						}
						else if (v.Trait.Kind == TraitKind.Getter)
						{
							g_idx = i;
							if (s_idx != -1)
								break;
						}
						else if (v.Trait.Kind == TraitKind.Setter)
						{
							s_idx = i;
							if (g_idx != -1)
								break;
						}
					}
				}

			};
			//查函数表

			if (primitive_codescope != null)
			{
				int i;
				var member = searchmember(primitive_codescope, searchNs, searchName, out i);
				int m_idx, g_idx, s_idx;
				searchvtable(primitive_codescope.Container._vtable, searchNs, searchName, out m_idx, out g_idx, out s_idx);

				if (member != null)
				{
					int ptrIndex = stackStPos + stack.index;
					int cacheobjpointer = Context.CacheObjPtr + ptrIndex;  //Context.CacheObjectPointers[ptrIndex];
					RtHeapBase cache = Context.GC.Heap[cacheobjpointer];
#if DEBUG
					if (cache.Kind != RtHeapTypeKind.STACK_CACHE_OBJ)
					{
						throw new InvalidOperationException();
					}
#endif

					RtStackCache cachePayload = (RtStackCache)cache;
					cachePayload.RefInstance = instance_box;
					cachePayload.trait[0] = member.trait; cachePayload.trait[1] = null;
					cachePayload.scopemember_index = (ushort)i;
					cachePayload.searchPropertyName.SetUndefined(); cachePayload.as_type = instance.Type;
					cachePayload.searchNameSpacePtr = 0; cachePayload.indexer_key.setFault();
					stackslots[stack.index].SetHeapPtr(cacheobjpointer, (byte)RtHeapTypeKind.STACK_CACHE_OBJ, (byte)HeapKindFlag.NONE);
					goto lbl_rtqname_success;
				}
				else if (m_idx > -1)
				{
					var vitem = primitive_codescope.Container._vtable.Items[m_idx];

					int ptrIndex = stackStPos + stack.index;
					int m_closurePtr = Context.M_ClosurePtr + ptrIndex;

					Context.GC.Heap[m_closurePtr].Type = vitem.Trait.Method.Body;
					RtClosure closure = (RtClosure)Context.GC.Heap[m_closurePtr];
					closure.This = instance_box;
					closure.ScopePtr = 0;
					closure.ScopeType = vitem.DefineAt;
					closure._ref_as_type = GetASTypeFromValue(instance_box); //as_type;
					closure.methodscopeslot_ref_state = 0; closure.HEAPINSTANCE_PTR = 0;
					stackslots[stack.index].SetHeapPtr(m_closurePtr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);
				}
				else
				{
					Context.GC.CheckGC(ref error);

					//int searchPtr = Context.GC.AllocString(searchName);
					//if (searchPtr == 0)
					//{
					//	RaiseOutOfMemory(ref error);
					//	goto flag_handle_error;
					//}

					NaNBoxing searchPtr;
					if (!TryCreateStringValue(searchName, out searchPtr, ref error))
					{
						goto flag_handle_error;
					}


					int ptrIndex = stackStPos + stack.index;
					int cacheobjpointer = Context.CacheObjPtr + ptrIndex;  //Context.CacheObjectPointers[ptrIndex];
					RtHeapBase cache = Context.GC.Heap[cacheobjpointer];
#if DEBUG
					if (cache.Kind != RtHeapTypeKind.STACK_CACHE_OBJ)
					{
						throw new InvalidOperationException();
					}
#endif

					RtStackCache cachePayload = (RtStackCache)cache;
					cachePayload.RefInstance = instance_box;
					cachePayload.trait[0] = null; cachePayload.trait[1] = null;
					cachePayload.scopemember_index = 0;
					cachePayload.searchPropertyName = searchPtr; cachePayload.searchNameSpacePtr = ns.HeapPtr; cachePayload.indexer_key.setFault(); cachePayload.as_type = primitive_codescope.TypeLayout.ASType.Instance;
					stackslots[stack.index].SetHeapPtr(cacheobjpointer, (byte)RtHeapTypeKind.STACK_CACHE_OBJ, (byte)HeapKindFlag.NONE);

					goto lbl_rtqname_dynamicprop;

				}

			}
			else if (instance.Kind == RtHeapTypeKind.INSTANCE
				|| instance.Kind == RtHeapTypeKind.VECTOR
				|| instance.Kind == RtHeapTypeKind.STRING
				|| instance.Kind == RtHeapTypeKind.ARRAY
				)
			{
				CodeScope scope = instance.Type._link_codescope;
				int i;
				var member = searchmember(scope, searchNs, searchName, out i);
				int m_idx, g_idx, s_idx;
				searchvtable(instance.Type._vtable, searchNs, searchName, out m_idx, out g_idx, out s_idx);

				if ((member == null && m_idx < 0 && g_idx < 0 && s_idx < 0) && deepsearch)
				{
					scope = instance.Type._link_codescope.TypeLayout.ASType._link_codescope;
					instancePtr.SetHeapPtr(instance.Type._link_codescope.TypeLayout.ASType.__instance_index__, (byte)RtHeapTypeKind.CLASS, (byte)HeapKindFlag.NONE);

					issameorinherit = false; //静态成员查找跳过 protected..
					member = searchmember(scope, searchNs, searchName, out i); //查找静态成员
					searchvtable(scope.Container._vtable, searchNs, searchName, out m_idx, out g_idx, out s_idx);


					while (member == null && m_idx < 0 && g_idx < 0 && s_idx < 0)
					{
						var superType = ((ASClass)scope.Container).Instance._super_class_; //查找基类的静态成员
						if (superType == null)
							break;

						scope = superType._link_codescope;
						instancePtr.SetHeapPtr(((ASClass)scope.Container).__instance_index__, (byte)RtHeapTypeKind.CLASS, (byte)HeapKindFlag.NONE);
						member = searchmember(scope, searchNs, searchName, out i);
						searchvtable(scope.Container._vtable, searchNs, searchName, out m_idx, out g_idx, out s_idx);

					}
				}

				if (member != null)
				{
					int ptrIndex = stackStPos + stack.index;
					int cacheobjpointer = Context.CacheObjPtr + ptrIndex;  //Context.CacheObjectPointers[ptrIndex];
					RtHeapBase cache = Context.GC.Heap[cacheobjpointer];
#if DEBUG
					if (cache.Kind != RtHeapTypeKind.STACK_CACHE_OBJ)
					{
						throw new InvalidOperationException();
					}
#endif

					RtStackCache cachePayload = (RtStackCache)cache;
					cachePayload.RefInstance = instancePtr;//.SetHeapPtr(instancePtr);
					cachePayload.trait[0] = member.trait; cachePayload.trait[1] = null;
					cachePayload.scopemember_index = (ushort)i;
					cachePayload.searchPropertyName.SetUndefined(); cachePayload.as_type = instance.Type;
					cachePayload.searchNameSpacePtr = 0; cachePayload.indexer_key.setFault();

					stackslots[stack.index].SetHeapPtr(cacheobjpointer, (byte)RtHeapTypeKind.STACK_CACHE_OBJ, (byte)HeapKindFlag.NONE);


					goto lbl_rtqname_success;
				}
				else if (m_idx > -1 || g_idx > -1 || s_idx > -1)
				{
					if (m_idx > -1)
					{
						var vitem = scope.Container._vtable.Items[m_idx];

						int ptrIndex = stackStPos + stack.index;
						int m_closurePtr = Context.M_ClosurePtr + ptrIndex;

						Context.GC.Heap[m_closurePtr].Type = vitem.Trait.Method.Body;
						RtClosure closure = (RtClosure)Context.GC.Heap[m_closurePtr];
						closure.This = instancePtr; //.SetHeapPtr(instancePtr);
						closure.ScopePtr = instancePtr.HeapPtr;
						closure.ScopeType = vitem.DefineAt;
						closure._ref_as_type = instance.Type;  //as_type;
						closure.methodscopeslot_ref_state = 0; closure.HEAPINSTANCE_PTR = 0;
						stackslots[stack.index].SetHeapPtr(m_closurePtr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);

					}
					else
					{
						//throw new NotImplementedException();
						int ptrIndex = stackStPos + stack.index;
						int cacheobjpointer = Context.CacheObjPtr + ptrIndex;
						RtHeapBase cache = Context.GC.Heap[cacheobjpointer];
#if DEBUG
						if (cache.Kind != RtHeapTypeKind.STACK_CACHE_OBJ)
						{
							throw new InvalidOperationException();
						}
#endif

						RtStackCache cachePayload = (RtStackCache)cache;
						cachePayload.RefInstance = instancePtr; //.SetHeapPtr(instancePtr);
						if (g_idx > -1)
						{
							cachePayload.trait[0] = scope.Container._vtable.Items[g_idx].Trait;
							cachePayload.g_index = g_idx;
						}
						else
						{
							cachePayload.trait[0] = null;
						}

						if (s_idx > -1)
						{
							cachePayload.trait[1] = scope.Container._vtable.Items[s_idx].Trait;
							cachePayload.s_index = s_idx;
						}
						else
						{
							cachePayload.trait[1] = null;
						}

						cachePayload.scopemember_index = 0;
						cachePayload.searchPropertyName.SetUndefined(); cachePayload.as_type = instance.Type;
						cachePayload.searchNameSpacePtr = 0; cachePayload.indexer_key.setFault();

						stackslots[stack.index].SetHeapPtr(cacheobjpointer, (byte)RtHeapTypeKind.STACK_CACHE_OBJ, (byte)HeapKindFlag.NONE);

					}
					goto lbl_rtqname_success;
				}
				else
				{
					Context.GC.CheckGC(ref error);

					//int searchPtr = Context.GC.AllocString(searchName);
					//if (searchPtr == 0)
					//{
					//	RaiseOutOfMemory(ref error);
					//	goto flag_handle_error;
					//}
					NaNBoxing searchPtr;
					if (!TryCreateStringValue(searchName, out searchPtr, ref error))
					{
						goto flag_handle_error;
					}

					int ptrIndex = stackStPos + stack.index;
					int cacheobjpointer = Context.CacheObjPtr + ptrIndex;  //Context.CacheObjectPointers[ptrIndex];
					RtHeapBase cache = Context.GC.Heap[cacheobjpointer];
#if DEBUG
					if (cache.Kind != RtHeapTypeKind.STACK_CACHE_OBJ)
					{
						throw new InvalidOperationException();
					}
#endif

					RtStackCache cachePayload = (RtStackCache)cache;
					cachePayload.RefInstance = o_instancePtr; //.SetHeapPtr(o_instancePtr);
					cachePayload.trait[0] = null; cachePayload.trait[1] = null;
					cachePayload.scopemember_index = 0;
					cachePayload.searchPropertyName = searchPtr; cachePayload.searchNameSpacePtr = ns.HeapPtr; cachePayload.indexer_key.setFault(); cachePayload.as_type = instance.Type;


					stackslots[stack.index].SetHeapPtr(cacheobjpointer, (byte)RtHeapTypeKind.STACK_CACHE_OBJ, (byte)HeapKindFlag.NONE);


					goto lbl_rtqname_dynamicprop;

				}
			}
			else if (instance.Kind == RtHeapTypeKind.CLASS)
			{
				CodeScope cls = ((RtScriptClass)instance).Meta._link_codescope;
				int i;
				var member = searchmember(cls, searchNs, searchName, out i);

				int m_idx, g_idx, s_idx;
				searchvtable(cls.Container._vtable, searchNs, searchName, out m_idx, out g_idx, out s_idx);

				if (member != null)
				{
					int ptrIndex = stackStPos + stack.index;
					int cacheobjpointer = Context.CacheObjPtr + ptrIndex;  //Context.CacheObjectPointers[ptrIndex];
					RtHeapBase cache = Context.GC.Heap[cacheobjpointer];
#if DEBUG
					if (cache.Kind != RtHeapTypeKind.STACK_CACHE_OBJ)
					{
						throw new InvalidOperationException();
					}
#endif

					RtStackCache cachePayload = (RtStackCache)cache;
					cachePayload.RefInstance = instancePtr; //.SetHeapPtr(instancePtr);
					cachePayload.trait[0] = member.trait; cachePayload.trait[1] = null;
					cachePayload.scopemember_index = (ushort)i;
					cachePayload.searchPropertyName.SetUndefined(); cachePayload.searchNameSpacePtr = 0; cachePayload.indexer_key.setFault(); cachePayload.as_type = instance.Type;

					stackslots[stack.index].SetHeapPtr(cacheobjpointer, (byte)RtHeapTypeKind.STACK_CACHE_OBJ, (byte)HeapKindFlag.NONE);


					goto lbl_rtqname_success;
				}
				else if (m_idx > -1 || g_idx > -1 || s_idx > -1)
				{
					if (m_idx > -1)
					{
						var vitem = cls.Container._vtable.Items[m_idx];

						int ptrIndex = stackStPos + stack.index;
						int m_closurePtr = Context.M_ClosurePtr + ptrIndex;

						Context.GC.Heap[m_closurePtr].Type = vitem.Trait.Method.Body;
						RtClosure closure = (RtClosure)Context.GC.Heap[m_closurePtr];
						closure.This.SetNull();
						closure.ScopePtr = instancePtr.HeapPtr;
						closure.ScopeType = vitem.DefineAt;
						closure._ref_as_type = cls.Container; //as_type;
						closure.methodscopeslot_ref_state = 0; closure.HEAPINSTANCE_PTR = 0;
						stackslots[stack.index].SetHeapPtr(m_closurePtr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);

					}
					else
					{
						//throw new NotImplementedException();
						int ptrIndex = stackStPos + stack.index;
						int cacheobjpointer = Context.CacheObjPtr + ptrIndex;
						RtHeapBase cache = Context.GC.Heap[cacheobjpointer];
#if DEBUG
						if (cache.Kind != RtHeapTypeKind.STACK_CACHE_OBJ)
						{
							throw new InvalidOperationException();
						}
#endif

						RtStackCache cachePayload = (RtStackCache)cache;
						cachePayload.RefInstance = instancePtr; //.SetHeapPtr(instancePtr);
						if (g_idx > -1)
						{
							cachePayload.trait[0] = cls.Container._vtable.Items[g_idx].Trait;
							cachePayload.g_index = g_idx;
						}
						else
						{
							cachePayload.trait[0] = null;
						}

						if (s_idx > -1)
						{
							cachePayload.trait[1] = cls.Container._vtable.Items[s_idx].Trait;
							cachePayload.s_index = s_idx;
						}
						else
						{
							cachePayload.trait[1] = null;
						}

						cachePayload.scopemember_index = 0;
						cachePayload.searchPropertyName.SetUndefined(); cachePayload.as_type = instance.Type;
						cachePayload.searchNameSpacePtr = 0; cachePayload.indexer_key.setFault();

						stackslots[stack.index].SetHeapPtr(cacheobjpointer, (byte)RtHeapTypeKind.STACK_CACHE_OBJ, (byte)HeapKindFlag.NONE);

					}
					goto lbl_rtqname_success;
				}


				else if (searchNs.Kind != NamespaceKind.Package)
				{
					goto lbl_rtqname_notfound;
				}
				else
				{
					Context.GC.CheckGC(ref error);

					//int searchPtr = Context.GC.AllocString(searchName);
					//if (searchPtr == 0)
					//{
					//	RaiseOutOfMemory(ref error);
					//	goto flag_handle_error;
					//}

					NaNBoxing searchPtr;
					if (!TryCreateStringValue(searchName, out searchPtr, ref error))
					{
						goto flag_handle_error;
					}

					int ptrIndex = stackStPos + stack.index;
					int cacheobjpointer = Context.CacheObjPtr + ptrIndex;  //Context.CacheObjectPointers[ptrIndex];
					RtHeapBase cache = Context.GC.Heap[cacheobjpointer];
#if DEBUG
					if (cache.Kind != RtHeapTypeKind.STACK_CACHE_OBJ)
					{
						throw new InvalidOperationException();
					}
#endif

					RtStackCache cachePayload = (RtStackCache)cache;
					cachePayload.RefInstance = o_instancePtr;//.SetHeapPtr(o_instancePtr);
					cachePayload.trait[0] = null; cachePayload.trait[1] = null;
					cachePayload.scopemember_index = 0;
					cachePayload.searchPropertyName = searchPtr; cachePayload.searchNameSpacePtr = 0; cachePayload.indexer_key.setFault(); cachePayload.as_type = instance.Type;

					stackslots[stack.index].SetHeapPtr(cacheobjpointer, (byte)RtHeapTypeKind.STACK_CACHE_OBJ, (byte)HeapKindFlag.NONE);


					goto lbl_rtqname_dynamicprop;
				}

			}
			else if (instance.Kind == RtHeapTypeKind.GLOBAL)
			{
				goto lbl_rtqname_notfound;
			}
			else if (instance.Kind == RtHeapTypeKind.CLOSURE)
			{
				goto lbl_rtqname_notfound;
			}
#if DEBUG
			else
			{
				throw new InvalidOperationException();
			}
#endif

		lbl_rtqname_success:;
		lbl_rtqname_dynamicprop:;
			return;
		lbl_rtqname_ns_not_a_namespace:
			//throw new NotImplementedException("输出命名空间类型转换异常");
			Context.GC.CheckGC(ref error);
			RaiseTypeError(ref error, ns, TypeKind.Namespace);
			goto flag_handle_error;

		lbl_rtqname_notfound:;
			Context.GC.CheckGC(ref error);
			//throw new NotImplementedException("输出未找到异常");
			RaiseReferenceError_RTQNameNotFound(ref error, ns, searchName, instance_box);
			goto flag_handle_error;



		flag_handle_error:
			;

		}


		private unsafe void StoreScopeH( StackLocater stackLocater, ScopeHeapLocater heapLocater ,
			int scope_ptr,  RtHeapBase methodscope, int* method_scopes  ,Span<NaNBoxing> stackslots,
			ASContainer scopeType,
			ref ReceiveError error
			)
		{
			NaNBoxing value = stackslots[stackLocater.index];
			var s = methodscope; //Context.GC.Heap[scope_ptr];

			int* m_scope = method_scopes;
			*m_scope++ = scope_ptr;

		label_method_parent:
			switch (s.Kind)
			{
				case RtHeapTypeKind.CLASS:
				case RtHeapTypeKind.GLOBAL:
					{
						RtScriptClass heap = (RtScriptClass)s;

						if (heap.Meta._link_codescope.index != heapLocater.ScopeIndex)
						{
#if DEBUG
							if (s.Kind != RtHeapTypeKind.CLASS)
							{
								throw new InvalidOperationException();
							}
							else
#endif
							{
								heap = (RtScriptClass)Context.GC.Heap[((ASScript)heap.Meta._link_codescope.Parent.Container).__global_index__]
										;
							}
						}

						ASTrait t = heap.Meta._link_codescope.Members[heapLocater.MemberIndex].trait;

						Context.GC.CheckGC(ref error);
						if (Context.StackPosition >= Context.STACK_LENGTH)
						{
							RaiseStackOverflow(ref error);
							goto flag_handle_error;
						}

						ref NaNBoxing conv = ref Context.StackSlots[Context.StackPosition];
						Context.StackPosition++;

						ConvertValueType(ref error, value, t.TypeKind, t.__rt_type_class__, ref conv, scope_ptr, ((RtMethodScope)methodscope).ThisPtr);
						if (error.raised)
						{
							Context.StackPosition--;
							goto flag_handle_error;
						}

						if (heap.IsUpdateStructOrEqual(Context, heapLocater.MemberIndex, conv))
						{
							Context.StackPosition--;
						}
						else
						{
							value = GetSaveValue(conv, ref error);
							Context.StackPosition--;

							if (error.raised)
							{
								goto flag_handle_error;
							}

							heap.SetSlot(value, heapLocater.MemberIndex);
						}
					}
					break;

				case RtHeapTypeKind.INSTANCE:
					{

						//考虑可能继承的情况，scopeType保存上下文堆内存用的布局类型
						//if (scopeType._link_codescope.index != heapLocater.ScopeIndex)
						if (
							scopeType._link_codescope.index != heapLocater.ScopeIndex  //子类调用基类的构造函数时，可能下面的条件不成立，这时判断一下scopeType的类型
							&&
							s.Type._link_codescope.index != heapLocater.ScopeIndex)
						{
							var sType = scopeType._link_codescope.Parent;    //这里还是必须用scopeType来查找global.
							while (sType.Kind != CodeScopeKind.Script)
							{
								sType = sType.Parent;
							}

							RtScriptClass heap = (RtScriptClass)Context.GC.Heap[((ASScript)sType.Container).__global_index__]
										;
							ASTrait t = heap.Meta._link_codescope.Members[heapLocater.MemberIndex].trait;


							Context.GC.CheckGC(ref error);
							if (Context.StackPosition >= Context.STACK_LENGTH)
							{
								RaiseStackOverflow(ref error);
								goto flag_handle_error;
							}

							ref NaNBoxing conv = ref Context.StackSlots[Context.StackPosition];
							Context.StackPosition++;

							ConvertValueType(ref error, value, t.TypeKind, t.__rt_type_class__, ref conv, scope_ptr, ((RtMethodScope)methodscope).ThisPtr);
							if (error.raised)
							{
								Context.StackPosition--;
								goto flag_handle_error;
							}

							if (heap.IsUpdateStructOrEqual(Context, heapLocater.MemberIndex, conv))
							{
								Context.StackPosition--;
							}
							else
							{
								value = GetSaveValue(conv, ref error);
								Context.StackPosition--;

								if (error.raised)
								{
									goto flag_handle_error;
								}

								heap.SetSlot(value, heapLocater.MemberIndex);
							}
						}
						else
						{
							RtInstance heap = (RtInstance)s;


							Context.GC.CheckGC(ref error);
							if (Context.StackPosition >= Context.STACK_LENGTH)
							{
								RaiseStackOverflow(ref error);
								goto flag_handle_error;
							}

							ref NaNBoxing conv = ref Context.StackSlots[Context.StackPosition];
							Context.StackPosition++;

							ConvertValueType(ref error, value,
								s.Type._link_codescope.Members[heapLocater.MemberIndex].TypeKind,
								s.Type._link_codescope.Members[heapLocater.MemberIndex].__rt_type_class__, ref conv, scope_ptr, ((RtMethodScope)methodscope).ThisPtr);

							if (error.raised)
							{
								Context.StackPosition--;
								goto flag_handle_error;
							}
							if (heap.IsUpdateStructOrEqual(Context, heapLocater.MemberIndex, conv, (ASInstance)s.Type))
							{
								Context.StackPosition--;
							}
							else
							{
								value = GetSaveValue(conv, ref error);
								Context.StackPosition--;
								if (error.raised)
								{
									goto flag_handle_error;
								}

								heap.SetSlot(value, heapLocater.MemberIndex, s.Type._link_codescope, this);
							}
						}
					}
					break;
				case RtHeapTypeKind.MethodScope:
					{

						if (s.Type._link_codescope.index != heapLocater.ScopeIndex)
						{
							int parentPtr = ((RtMethodScope)s).ParentPtr;
							s = Context.GC.Heap[parentPtr];
							*m_scope++ = parentPtr;

							goto label_method_parent;
						}
						else
						{
							var thisPtr = ((RtMethodScope)methodscope).ThisPtr;
							var scopemember = s.Type._link_codescope.Members[heapLocater.MemberIndex];


							Context.GC.CheckGC(ref error);
							if (Context.StackPosition >= Context.STACK_LENGTH)
							{
								RaiseStackOverflow(ref error);
								goto flag_handle_error;
							}

							ref NaNBoxing conv = ref Context.StackSlots[Context.StackPosition];
							Context.StackPosition++;

							bool isheaptype;
							if (scopemember.Kind == ScopeMemberKind.Parameter)
							{
								isheaptype = scopemember.TypeKind.IsHeapType();
								ConvertValueType(ref error, value, scopemember.TypeKind, scopemember.__rt_type_class__, ref conv, scope_ptr, thisPtr);
							}
							else
							{
								ASTrait t = s.Type._link_codescope.Members[heapLocater.MemberIndex].trait;
								isheaptype = t.TypeKind.IsHeapType();
								ConvertValueType(ref error, value, t.TypeKind, t.__rt_type_class__, ref conv, scope_ptr, thisPtr);
							}
							if (error.raised)
							{
								Context.StackPosition--;
								goto flag_handle_error;
							}


							value = conv;

							RtMethodScope heap = (RtMethodScope)s;
							if (isheaptype)
							{
								PrepareSaveMethodScope(heap, ref heapLocater, ref value, m_scope, method_scopes, ref error);

								if (error.raised)
								{
									Context.StackPosition--;
									goto flag_handle_error;
								}
							}
							heap.SetSlot(value, heapLocater.MemberIndex);
							Context.StackPosition--;
						}
					}
					break;
#if DEBUG
				default:
					throw new InvalidOperationException();
#endif
			}

		flag_handle_error:
			;

		}







		private  void Ld_InstanceOrScopeMemberValueRef(StackLocater src, StackLocater target ,Span<NaNBoxing> stackslots,
			int stackStPos,int scope_ptr,uint scopemember_index,
			ref ReceiveError error)
		{

			NaNBoxing instance_box;
			RtHeapTypeKind kind;
			//ASContainer as_type;

			if (src.index >= 0)
			{
				instance_box = stackslots[src.index];
				kind = (RtHeapTypeKind)(instance_box.ValueType == BoxType.HeapPtr ? instance_box.HeapKind : 255);
			}
			else
			{
				ReadInstanceFromStacklocater(ref error, src, stackslots, stackStPos, scope_ptr, out kind, out instance_box);
				if (error.raised)
				{
					goto flag_handle_error;
				}
			}

			switch (instance_box.ValueType)
			{
				case NaNBoxing.BoxType.Null:
					Context.GC.CheckGC(ref error);
					RaiseTypeError_AccessNull(ref error);
					goto flag_handle_error;
				case NaNBoxing.BoxType.HeapPtr:
					break;
#if DEBUG
				case NaNBoxing.BoxType.Number:
				case NaNBoxing.BoxType.Boolean:
				case NaNBoxing.BoxType.Int:
				case NaNBoxing.BoxType.Uint:
				case NaNBoxing.BoxType.Sbyte:
				case NaNBoxing.BoxType.Byte:
				case NaNBoxing.BoxType.Short:
				case NaNBoxing.BoxType.UShort:
				case NaNBoxing.BoxType.Float:
					throw new InvalidOperationException(); //这些东西没有成员

				case NaNBoxing.BoxType.Undefined:
				case NaNBoxing.BoxType.Fault:
				default:
					throw new InvalidOperationException();
#endif
			}




			do
			{
				if (instance_box.HeapKind == (byte)RtHeapTypeKind.CLASS || instance_box.HeapKind == (byte)RtHeapTypeKind.GLOBAL)
				{

					var instance = Context.GC.Heap[instance_box.HeapPtr];
					RtScriptClass heap = (RtScriptClass)instance;
					ASTrait trait = heap.Meta._link_codescope.Members[(ushort)scopemember_index].trait;
#if DEBUG

					if (!
						(trait.Kind == TraitKind.Slot ||
							trait.Kind == TraitKind.Constant
						)
						)
					{
						throw new InvalidOperationException();
					}
#endif

					int ptrIndex = stackStPos + target.index;
					int cacheobjpointer = Context.CacheObjPtr + ptrIndex;  //Context.CacheObjectPointers[ptrIndex];
					RtHeapBase cache = Context.GC.Heap[cacheobjpointer];
#if DEBUG
					if (cache.Kind != RtHeapTypeKind.STACK_CACHE_OBJ)
					{
						throw new InvalidOperationException();
					}

#endif
					RtStackCache cachePayload = (RtStackCache)cache;
					cachePayload.RefInstance = instance_box;
					cachePayload.trait[0] = trait; cachePayload.trait[1] = null;
					cachePayload.scopemember_index = (ushort)scopemember_index;
					cachePayload.searchPropertyName.SetUndefined(); cachePayload.searchNameSpacePtr = 0; cachePayload.indexer_key.setFault(); cachePayload.as_type = instance.Type;

					stackslots[target.index].SetHeapPtr(cacheobjpointer, (byte)RtHeapTypeKind.STACK_CACHE_OBJ, (byte)HeapKindFlag.NONE);

				}
				else if (instance_box.HeapKind == (byte)RtHeapTypeKind.INSTANCE)
				{

					var instance = Context.GC.Heap[instance_box.HeapPtr];
					ASTrait trait = instance.Type._link_codescope.Members[(ushort)scopemember_index].trait;
#if DEBUG

					if (!
						(trait.Kind == TraitKind.Slot ||
						trait.Kind == TraitKind.Constant
						)
						)
					{
						throw new InvalidOperationException();
					}
#endif
					int ptrIndex = stackStPos + target.index;
					int cacheobjpointer = Context.CacheObjPtr + ptrIndex;  //Context.CacheObjectPointers[ptrIndex];
					RtHeapBase cache = Context.GC.Heap[cacheobjpointer];
#if DEBUG
					if (cache.Kind != RtHeapTypeKind.STACK_CACHE_OBJ)
					{
						throw new InvalidOperationException();
					}

#endif

					RtStackCache cachePayload = (RtStackCache)cache;
					cachePayload.RefInstance = instance_box;
					cachePayload.trait[0] = trait; cachePayload.trait[1] = null;
					cachePayload.scopemember_index = (ushort)scopemember_index;
					cachePayload.searchPropertyName.SetUndefined(); cachePayload.searchNameSpacePtr = 0; cachePayload.indexer_key.setFault(); cachePayload.as_type = instance.Type;

					stackslots[target.index].SetHeapPtr(cacheobjpointer, (byte)RtHeapTypeKind.STACK_CACHE_OBJ, (byte)HeapKindFlag.NONE);

				}
#if DEBUG
				else if (instance_box.HeapKind == (byte)RtHeapTypeKind.STACK_CACHE_OBJ)
				{
					throw new InvalidOperationException();
					//                                        int instancePtr = instance_box.HeapPtr;
					//                                        NaNBoxing v = new NaNBoxing();
					//                                        v.SetHeapPtr(instancePtr);
					//                                        v = LoadValue(v, ref error  ,ref stackslots, stackStPos);

					//#if DEBUG
					//                                        if (error.raised)
					//                                        {
					//                                            throw new InvalidOperationException();
					//                                        }
					//#endif


					//                                        if (v.ValueType == NaNBoxing.BoxType.HeapPtr)
					//                                        {
					//                                            instancePtr = v.HeapPtr;
					//                                            instance = Context.GC.Heap[instancePtr];
					//                                            continue;
					//                                        }
					//                                        else
					//                                        {
					//                                            throw new NotImplementedException();
					//                                        }

				}
#endif
#if DEBUG
				else
				{
					throw new InvalidOperationException();
				}
#endif
				break;
			}
			while (true);

		flag_handle_error:
			;


		}




		private void Ld_MulitNameL_Ref(int super_const_index, Span<NaNBoxing> constants ,
			Span<char> frame_holdchars ,StackLocater src, StackLocater _name , StackLocater stack ,Span<NaNBoxing> stackslots,
			int stackStPos,int scope_ptr, RtHeapBase methodscope,
			ref ReceiveError error)
		{

			NaNBoxing instance_box;
			RtHeapTypeKind kind;
			ASContainer as_type = null;

			if (src.index >= 0)
			{
				instance_box = stackslots[src.index];
				kind = (RtHeapTypeKind)(instance_box.ValueType == BoxType.HeapPtr ? instance_box.HeapKind : 255);
			}
			else
			{
				ReadInstanceFromStacklocater(ref error, src, stackslots, stackStPos, scope_ptr, out kind, out instance_box);
				if (error.raised)
				{
					goto flag_handle_error;
				}
			}

			if (super_const_index != 0)
			{
				//读基类
				super_const_index -= 1;

				var vbox = constants[super_const_index];

#if DEBUG
				if (vbox.ValueType != NaNBoxing.BoxType.Uint)
					throw new InvalidOperationException();
#endif

				var super_class = Context.link_const_class[(int)vbox.UIntValue];

#if DEBUG
				var check = GetASTypeFromValue(instance_box);
				if (check is ASInstance)
				{
					if (!((ASInstance)check).IsExtend(super_class.Instance))
					{
						throw new InvalidOperationException();
					}
				}

#endif

				as_type = super_class.Instance;
			}

			//RtHeapBase instance = null;
			bool setinstance = false;
			switch (instance_box.ValueType)
			{
				case NaNBoxing.BoxType.Null:
					Context.GC.CheckGC(ref error);
					RaiseTypeError_AccessNull(ref error);
					goto flag_handle_error;
				case NaNBoxing.BoxType.Undefined:
					Context.GC.CheckGC(ref error);
					RaiseTypeError_ATermUndefined(ref error);
					goto flag_handle_error;
				case NaNBoxing.BoxType.HeapPtr:
					break;
				case NaNBoxing.BoxType.Sbyte:
					as_type = Context.SBYTE.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case NaNBoxing.BoxType.Byte:
					as_type = Context.BYTE.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case NaNBoxing.BoxType.Short:
					as_type = Context.SHORT.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case NaNBoxing.BoxType.UShort:
					as_type = Context.USHORT.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case NaNBoxing.BoxType.Int:
					as_type = Context.INT.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case NaNBoxing.BoxType.Uint:
					as_type = Context.UINT.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case NaNBoxing.BoxType.Float:
					as_type = Context.FLOAT.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case NaNBoxing.BoxType.Number:
					as_type = Context.NUMBER.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case NaNBoxing.BoxType.Boolean:
					as_type = Context.BOOLEAN.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case BoxType.LocalString:
					as_type = Context.STRING.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;

#if DEBUG
				case NaNBoxing.BoxType.Fault:
				default:
					throw new InvalidOperationException();
#endif
			}


			setinstance = true;
		lbl_instance_primitive:
			Span<char> buffers = frame_holdchars; //stackalloc char[16];
			ReadOnlySpan<char> name = buffers;

			NaNBoxing prop_name = stackslots[_name.index];

			if (setinstance && (
				instance_box.HeapKind == (byte)RtHeapTypeKind.INSTANCE
				&&
				((ASInstance)Context.GC.Heap[instance_box.HeapPtr].Type).Flags.HasFlag(ClassFlags.Indexer)
				)
				||

				(instance_box.HeapKind == (byte)RtHeapTypeKind.VECTOR && RtVector.IsValidIndexType(prop_name))

				)
			{
				//索引器处理
				int ptrIndex = stackStPos + stack.index;
				int cacheobjpointer = Context.CacheObjPtr + ptrIndex;  //Context.CacheObjectPointers[ptrIndex];
				RtHeapBase cache = Context.GC.Heap[cacheobjpointer];
#if DEBUG
				if (cache.Kind != RtHeapTypeKind.STACK_CACHE_OBJ)
				{
					throw new InvalidOperationException();
				}
#endif


				RtStackCache cachePayload = (RtStackCache)cache;
				cachePayload.RefInstance = instance_box;
				cachePayload.trait[0] = null; cachePayload.trait[1] = null;
				cachePayload.scopemember_index = 0;
				cachePayload.searchPropertyName.SetUndefined(); cachePayload.as_type = Context.GC.Heap[instance_box.HeapPtr].Type;
				cachePayload.searchNameSpacePtr = 0; cachePayload.indexer_key = prop_name;

				stackslots[stack.index].SetHeapPtr(cacheobjpointer, (byte)RtHeapTypeKind.STACK_CACHE_OBJ, (byte)HeapKindFlag.NONE);

				return;
			}
			else if (prop_name.ValueType != NaNBoxing.BoxType.HeapPtr)
			{
				if (setinstance && instance_box.HeapKind == (byte)RtHeapTypeKind.ARRAY)
				{
					long index;

					switch (prop_name.ValueType)
					{
						case BoxType.LocalString:
							// Use efficient char-based extraction to avoid string allocation

							int charCount = prop_name.GetLocalStringChars(frame_holdchars);
							name = charCount > 0 ? buffers.Slice(0, charCount) : ReadOnlySpan<char>.Empty;
							goto lbl_name_solved;
						case NaNBoxing.BoxType.Number:
							{
								double v = prop_name.Number;
								if (v >= 0 && (v == Math.Truncate(v)) && !Double.IsInfinity(v) && v < uint.MaxValue)
								{
									index = (long)v;
									if (index >= 0 && index < uint.MaxValue)
									{
										goto array_index;
									}
									else
									{
										name = index.ToString();
										goto array_prop;
									}
								}
								else
								{
									name = Extensions.GetPrimitiveValueToString(this, prop_name, buffers);
									goto array_prop;
								}
							}
						case NaNBoxing.BoxType.Float:
							{
								double v = prop_name.FloatValue;
								if (v >= 0 && (v == Math.Truncate(v)) && !Double.IsInfinity(v) && v < uint.MaxValue)
								{
									index = (long)v;
									if (index >= 0 && index < uint.MaxValue)
									{
										goto array_index;
									}
									else
									{
										name = index.ToString();
										goto array_prop;
									}
								}
								else
								{
									name = Extensions.GetPrimitiveValueToString(this, prop_name, buffers);
									goto array_prop;
								}
							}
						case NaNBoxing.BoxType.Undefined:
							name = "undefined";
							goto array_prop;
						case NaNBoxing.BoxType.Null:
							name = "null";
							goto array_prop;
						case NaNBoxing.BoxType.Boolean:
							name = prop_name.Boolean ? "true" : "false";
							goto array_prop;
						case NaNBoxing.BoxType.Int:
							{
								index = prop_name.IntValue;
								if (index >= 0)
								{
									goto array_index;
								}
								else
								{
									name = index.ToString();
									goto array_prop;
								}
							}
						case NaNBoxing.BoxType.Uint:
							{
								index = prop_name.UIntValue;
								if (index < uint.MaxValue)
								{
									goto array_index;
								}
								else
								{
									name = index.ToString();
									goto array_prop;
								}
							}
						case NaNBoxing.BoxType.Sbyte:
							{
								index = prop_name.SByteValue;
								if (index >= 0)
								{
									goto array_index;
								}
								else
								{
									name = index.ToString();
									goto array_prop;
								}
							}
						case NaNBoxing.BoxType.Byte:
							{
								index = prop_name.ByteValue;
								goto array_index;
							}
						case NaNBoxing.BoxType.Short:
							{
								index = prop_name.ShortValue;
								if (index >= 0)
								{
									goto array_index;
								}
								else
								{
									name = index.ToString();
									goto array_prop;
								}
							}
						case NaNBoxing.BoxType.UShort:
							{
								index = prop_name.UShortValue;
								goto array_index;
							}
#if DEBUG
						case NaNBoxing.BoxType.Fault:
						default:
							throw new InvalidOperationException();
#else
											default:
												Environment.FailFast("出错了，这里跑不到");

												error.error.setFault();
												goto flag_handle_error;
#endif
					}

				//索引处理
				array_index:
					uint array_i = (uint)index;
					int ptrIndex = stackStPos + stack.index;
					int cacheobjpointer = Context.CacheObjPtr + ptrIndex;  //Context.CacheObjectPointers[ptrIndex];
					RtHeapBase cache = Context.GC.Heap[cacheobjpointer];
#if DEBUG
					if (cache.Kind != RtHeapTypeKind.STACK_CACHE_OBJ)
					{
						throw new InvalidOperationException();
					}
#endif

					RtStackCache cachePayload = (RtStackCache)cache;
					cachePayload.RefInstance = instance_box;
					cachePayload.trait[0] = null; cachePayload.trait[1] = null;
					cachePayload.scopemember_index = 0;
					cachePayload.searchPropertyName.SetUndefined(); cachePayload.as_type = Context.GC.Heap[instance_box.HeapPtr].Type;
					cachePayload.searchNameSpacePtr = 0; cachePayload.indexer_key.SetUInt(array_i);

					stackslots[stack.index].SetHeapPtr(cacheobjpointer, (byte)RtHeapTypeKind.STACK_CACHE_OBJ, (byte)HeapKindFlag.NONE);


					//										//quickening
					//#if FORCOMPILER
					//										if (!IsComputeConstExpr)
					//										{
					//#endif
					//											if (super_const_index == 0 && src.index >=0 && 
					//											(prop_name.ValueType == BoxType.Int || prop_name.ValueType == BoxType.Byte 
					//												|| prop_name.ValueType == BoxType.Sbyte || prop_name.ValueType ==  BoxType.Short || prop_name.ValueType == BoxType.UShort) )
					//											{
					//												*opcodePtr = ((uint)INS_Code.ld_MultiNameL_Ref_ARR_INT | (0xffffff00 & (*opcodePtr)));
					//											}

					//#if FORCOMPILER
					//										}
					//#endif


					return;

				array_prop:;


				}

				else if (setinstance && instance_box.HeapKind == (byte)RtHeapTypeKind.VECTOR)
				{
					//不合理的索引范围
#if DEBUG
					if (RtVector.IsValidIndexType(prop_name))
					{
						throw new InvalidOperationException();
					}
#endif

					name = Extensions.GetPrimitiveValueToString(this, prop_name, buffers);
				}
				else
				{
					name = Extensions.GetPrimitiveValueToString(this, prop_name, buffers);
					//throw new NotImplementedException("转字符串？还是数组？");
				}
			}
			else
			{

				//RtHeapBase _n = Context.GC.Heap[prop_name.HeapPtr];
				if (prop_name.HeapKind != (byte)RtHeapTypeKind.STRING)
				{
					if (Context.StackPosition == Context.STACK_LENGTH)
					{
						RaiseStackOverflow(ref error);
						goto flag_handle_error;
					}

					var span = Context.StackSlots.AsSpan(Context.StackPosition, 1); span.Clear();
					StackLocater tmp = default; tmp.index = 0;
					int stpos = Context.StackPosition;
					Context.StackPosition++;
					NaNBoxing primitive_name = ToPrimitive(ref error, prop_name, HINT.h_string, scope_ptr, tmp, tmp, span, stpos, ((RtMethodScope)methodscope).ThisPtr);
					if (error.raised)
					{
						Context.StackPosition--;
						goto flag_handle_error;
					}

					name = Extensions.GetPrimitiveValueToString(this, primitive_name, buffers);
					Context.StackPosition--;


					//throw new NotImplementedException("转字符串？");
				}
				else
				{
					RtHeapBase _n = Context.GC.Heap[prop_name.HeapPtr];
					name = ((RtString)_n).Str;
				}

			}

		lbl_name_solved:

			var scope = methodscope; //Context.GC.Heap[scope_ptr];
#if DEBUG
			if (scope.Kind != RtHeapTypeKind.MethodScope)
			{
				throw new InvalidOperationException();
			}
#endif

			if (as_type == null)
			{
				as_type = GetASTypeFromValue(instance_box);
			}

			var ns_set = scope.Type._link_codescope.NamespaceSet;
			NaNBoxing thisPtr = ((RtMethodScope)methodscope).ThisPtr;
			int code = MultiNameLSearch(ns_set, kind, as_type, name, 0, stack, stackslots, stackStPos, instance_box, check_MultiNameLSearch_issameorinherit(instance_box, thisPtr.ValueType == BoxType.HeapPtr ? Context.GC.Heap[thisPtr.HeapPtr] : null), ref error);

			switch (code)
			{
				case 0:
					break;
				case 1:
					goto flag_handle_error;
				case 2:
					Context.GC.CheckGC(ref error);
					RaiseTypeError_Ambiguous(ref error, name);
					goto flag_handle_error;
#if DEBUG
				//case 3:
				//    RaiseReferenceError_MulitNameNotFound(ref error, name, as_type.QName);
				//    goto flag_handle_error;
				default:
					throw new InvalidOperationException();
#endif
			}


		flag_handle_error:
			;

		}






		private unsafe void Store_MultiNameL(int super_const_index, Span<NaNBoxing> constants,
			Span<char> frame_holdchars, StackLocater src, StackLocater _name, StackLocater source, StackLocater tmp_holder,  StackLocater* tmpArgLoc, Span<NaNBoxing> stackslots,
			int stackStPos, int scope_ptr, RtHeapBase methodscope,
			ref ReceiveError error)
		{

			NaNBoxing instance_box;
			RtHeapTypeKind kind;
			ASContainer as_type = null;

			if (src.index >= 0)
			{
				instance_box = stackslots[src.index];
				kind = (RtHeapTypeKind)(instance_box.ValueType == BoxType.HeapPtr ? instance_box.HeapKind : 255);
			}
			else
			{
				ReadInstanceFromStacklocater(ref error, src, stackslots, stackStPos, scope_ptr, out kind, out instance_box);
				if (error.raised)
				{
					goto flag_handle_error;
				}
			}

			if (super_const_index != 0)
			{
				//读基类
				super_const_index -= 1;

				var vbox = constants[super_const_index];

#if DEBUG
				if (vbox.ValueType != NaNBoxing.BoxType.Uint)
					throw new InvalidOperationException();
#endif

				var super_class = Context.link_const_class[(int)vbox.UIntValue];

#if DEBUG
				var check = GetASTypeFromValue(instance_box);
				if (check is ASInstance)
				{
					if (!((ASInstance)check).IsExtend(super_class.Instance))
					{
						throw new InvalidOperationException();
					}
				}

#endif

				as_type = super_class.Instance;
			}

			//RtHeapBase instance = null;
			bool setinstance = false;
			switch (instance_box.ValueType)
			{
				case NaNBoxing.BoxType.Null:
					Context.GC.CheckGC(ref error);
					RaiseTypeError_AccessNull(ref error);
					goto flag_handle_error;
				case NaNBoxing.BoxType.Undefined:
					Context.GC.CheckGC(ref error);
					RaiseTypeError_ATermUndefined(ref error);
					goto flag_handle_error;
				case NaNBoxing.BoxType.HeapPtr:
					break;
				case NaNBoxing.BoxType.Sbyte:
					as_type = Context.SBYTE.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case NaNBoxing.BoxType.Byte:
					as_type = Context.BYTE.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case NaNBoxing.BoxType.Short:
					as_type = Context.SHORT.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case NaNBoxing.BoxType.UShort:
					as_type = Context.USHORT.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case NaNBoxing.BoxType.Int:
					as_type = Context.INT.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case NaNBoxing.BoxType.Uint:
					as_type = Context.UINT.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case NaNBoxing.BoxType.Float:
					as_type = Context.FLOAT.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case NaNBoxing.BoxType.Number:
					as_type = Context.NUMBER.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case NaNBoxing.BoxType.Boolean:
					as_type = Context.BOOLEAN.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case BoxType.LocalString:
					as_type = Context.STRING.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;

#if DEBUG
				case NaNBoxing.BoxType.Fault:
				default:
					throw new InvalidOperationException();
#endif
			}


			setinstance = true;
		lbl_instance_primitive:
			Span<char> buffers = frame_holdchars; //stackalloc char[16];
			ReadOnlySpan<char> name = buffers;

			NaNBoxing prop_name = stackslots[_name.index];

			if (setinstance && (
				instance_box.HeapKind == (byte)RtHeapTypeKind.INSTANCE
				&&
				((ASInstance)Context.GC.Heap[instance_box.HeapPtr].Type).Flags.HasFlag(ClassFlags.Indexer)
				)
				||

				(instance_box.HeapKind == (byte)RtHeapTypeKind.VECTOR && RtVector.IsValidIndexType(prop_name))

				)
			{
				//索引器处理
				int ptrIndex = stackStPos + tmp_holder.index;
				int cacheobjpointer = Context.CacheObjPtr + ptrIndex;  //Context.CacheObjectPointers[ptrIndex];
				RtHeapBase cache = Context.GC.Heap[cacheobjpointer];
#if DEBUG
				if (cache.Kind != RtHeapTypeKind.STACK_CACHE_OBJ)
				{
					throw new InvalidOperationException();
				}
#endif

				if (instance_box.HeapKind == (byte)RtHeapTypeKind.VECTOR && RtVector.IsValidIndexType(prop_name))
				{

					Context.GC.CheckGC(ref error);
					if (Context.StackPosition >= Context.STACK_LENGTH)
					{
						RaiseStackOverflow(ref error);
						goto flag_handle_error;
					}

					//RtVector vector = ((RtVector)instance);
					RtVector vector;
					int vptr = RtVector.FindAndUpdateHeapInstancePtr(instance_box.HeapPtr, this, out vector);

					ref NaNBoxing conv = ref Context.StackSlots[Context.StackPosition];
					Context.StackPosition++;

					ConvertValueType(ref error, stackslots[source.index], vector.element_type, vector.element_asclass, ref conv);//, scope_ptr, ((RtMethodScope)methodscope).ThisPtr);
					if (error.raised)
					{
						Context.StackPosition--;
						goto flag_handle_error;
					}
					//为性能考虑，阻止ConvertValueType调函数


					int validid;
					var store = ((RtVector)vector).GetStore();
					if (!(store.IsValidIndexRange(prop_name, out validid)))
					{
						int maxlen = store.length;
						if (validid == maxlen && maxlen < int.MaxValue) //扩容
						{
							((RtVector)vector).Resize(validid + 1, ref error, this, (ASInstance)vector.Type, out VectorImpl.VectorStore resizedstore);

							if (error.raised)
							{
								Context.StackPosition--;
								goto flag_handle_error;
							}

							//throw new NotImplementedException();
						}
						else
						{
							Context.StackPosition--;
							RaiseRangeError(ref error, Extensions.GetPrimitiveValueToString(this, prop_name, buffers), maxlen);
							goto flag_handle_error;
						}
					}

					vector.SetSlot(validid, this, vptr, conv, ref error);

					Context.StackPosition--;

					if (error.raised)
					{
						goto flag_handle_error;
					}



				}
				else
				{
					RtStackCache cachePayload = (RtStackCache)cache;
					cachePayload.RefInstance = instance_box;
					cachePayload.trait[0] = null; cachePayload.trait[1] = null;
					cachePayload.scopemember_index = 0;
					cachePayload.searchPropertyName.SetUndefined(); cachePayload.as_type = Context.GC.Heap[instance_box.HeapPtr].Type;
					cachePayload.searchNameSpacePtr = 0; cachePayload.indexer_key = prop_name;

					SaveHeapRef(cachePayload, source, stackslots, frame_holdchars, tmpArgLoc, scope_ptr, stackStPos, methodscope, ref error);
					if (error.raised)
					{
						goto flag_handle_error;
					}
					//stackslots[stack.index].SetHeapPtr(cacheobjpointer, (byte)RtHeapTypeKind.STACK_CACHE_OBJ, (byte)HeapKindFlag.NONE);
				}
				return;
			}
			else if (prop_name.ValueType != NaNBoxing.BoxType.HeapPtr)
			{
				if (setinstance && instance_box.HeapKind == (byte)RtHeapTypeKind.ARRAY)
				{
					long index;

					switch (prop_name.ValueType)
					{
						case BoxType.LocalString:
							// Use efficient char-based extraction to avoid string allocation

							int charCount = prop_name.GetLocalStringChars(frame_holdchars);
							name = charCount > 0 ? buffers.Slice(0, charCount) : ReadOnlySpan<char>.Empty;
							goto lbl_name_solved;
						case NaNBoxing.BoxType.Number:
							{
								double v = prop_name.Number;
								if (v >= 0 && (v == Math.Truncate(v)) && !Double.IsInfinity(v) && v < uint.MaxValue)
								{
									index = (long)v;
									if (index >= 0 && index < uint.MaxValue)
									{
										goto array_index;
									}
									else
									{
										name = index.ToString();
										goto array_prop;
									}
								}
								else
								{
									name = Extensions.GetPrimitiveValueToString(this, prop_name, buffers);
									goto array_prop;
								}
							}
						case NaNBoxing.BoxType.Float:
							{
								double v = prop_name.FloatValue;
								if (v >= 0 && (v == Math.Truncate(v)) && !Double.IsInfinity(v) && v < uint.MaxValue)
								{
									index = (long)v;
									if (index >= 0 && index < uint.MaxValue)
									{
										goto array_index;
									}
									else
									{
										name = index.ToString();
										goto array_prop;
									}
								}
								else
								{
									name = Extensions.GetPrimitiveValueToString(this, prop_name, buffers);
									goto array_prop;
								}
							}
						case NaNBoxing.BoxType.Undefined:
							name = "undefined";
							goto array_prop;
						case NaNBoxing.BoxType.Null:
							name = "null";
							goto array_prop;
						case NaNBoxing.BoxType.Boolean:
							name = prop_name.Boolean ? "true" : "false";
							goto array_prop;
						case NaNBoxing.BoxType.Int:
							{
								index = prop_name.IntValue;
								if (index >= 0)
								{
									goto array_index;
								}
								else
								{
									name = index.ToString();
									goto array_prop;
								}
							}
						case NaNBoxing.BoxType.Uint:
							{
								index = prop_name.UIntValue;
								if (index < uint.MaxValue)
								{
									goto array_index;
								}
								else
								{
									name = index.ToString();
									goto array_prop;
								}
							}
						case NaNBoxing.BoxType.Sbyte:
							{
								index = prop_name.SByteValue;
								if (index >= 0)
								{
									goto array_index;
								}
								else
								{
									name = index.ToString();
									goto array_prop;
								}
							}
						case NaNBoxing.BoxType.Byte:
							{
								index = prop_name.ByteValue;
								goto array_index;
							}
						case NaNBoxing.BoxType.Short:
							{
								index = prop_name.ShortValue;
								if (index >= 0)
								{
									goto array_index;
								}
								else
								{
									name = index.ToString();
									goto array_prop;
								}
							}
						case NaNBoxing.BoxType.UShort:
							{
								index = prop_name.UShortValue;
								goto array_index;
							}
#if DEBUG
						case NaNBoxing.BoxType.Fault:
						default:
							throw new InvalidOperationException();
#else
											default:
												Environment.FailFast("出错了，这里跑不到");

												error.error.setFault();
												goto flag_handle_error;
#endif
					}

				//索引处理
				array_index:
					uint array_i = (uint)index;
					int ptrIndex = stackStPos + tmp_holder.index;
					int cacheobjpointer = Context.CacheObjPtr + ptrIndex;  //Context.CacheObjectPointers[ptrIndex];
					RtHeapBase cache = Context.GC.Heap[cacheobjpointer];
#if DEBUG
					if (cache.Kind != RtHeapTypeKind.STACK_CACHE_OBJ)
					{
						throw new InvalidOperationException();
					}
#endif

					RtHeapBase instance = Context.GC.Heap[instance_box.HeapPtr];

					SetArraySlot(stackslots[source.index], array_i, instance, ref error);
					if (error.raised)
					{
						goto flag_handle_error;
					}



					//RtStackCache cachePayload = (RtStackCache)cache;
					//cachePayload.RefInstance = instance_box;
					//cachePayload.trait[0] = null; cachePayload.trait[1] = null;
					//cachePayload.scopemember_index = 0;
					//cachePayload.searchPropertyName.SetUndefined(); cachePayload.as_type = Context.GC.Heap[instance_box.HeapPtr].Type;
					//cachePayload.searchNameSpacePtr = 0; cachePayload.indexer_key.SetUInt(array_i);

					//SaveHeapRef(cachePayload, source, stackslots, frame_holdchars, tmpArgLoc, scope_ptr, stackStPos, methodscope, ref error);
					//if (error.raised)
					//{
					//	goto flag_handle_error;
					//}



					return;

				array_prop:;


				}

				else if (setinstance && instance_box.HeapKind == (byte)RtHeapTypeKind.VECTOR)
				{
					//不合理的索引范围
#if DEBUG
					if (RtVector.IsValidIndexType(prop_name))
					{
						throw new InvalidOperationException();
					}
#endif

					name = Extensions.GetPrimitiveValueToString(this, prop_name, buffers);
				}
				else
				{
					name = Extensions.GetPrimitiveValueToString(this, prop_name, buffers);
					//throw new NotImplementedException("转字符串？还是数组？");
				}
			}
			else
			{

				//RtHeapBase _n = Context.GC.Heap[prop_name.HeapPtr];
				if (prop_name.HeapKind != (byte)RtHeapTypeKind.STRING)
				{
					if (Context.StackPosition == Context.STACK_LENGTH)
					{
						RaiseStackOverflow(ref error);
						goto flag_handle_error;
					}

					var span = Context.StackSlots.AsSpan(Context.StackPosition, 1); span.Clear();
					StackLocater tmp = default; tmp.index = 0;
					int stpos = Context.StackPosition;
					Context.StackPosition++;
					NaNBoxing primitive_name = ToPrimitive(ref error, prop_name, HINT.h_string, scope_ptr, tmp, tmp, span, stpos, ((RtMethodScope)methodscope).ThisPtr);
					if (error.raised)
					{
						Context.StackPosition--;
						goto flag_handle_error;
					}

					name = Extensions.GetPrimitiveValueToString(this, primitive_name, buffers);
					Context.StackPosition--;


					//throw new NotImplementedException("转字符串？");
				}
				else
				{
					RtHeapBase _n = Context.GC.Heap[prop_name.HeapPtr];
					name = ((RtString)_n).Str;
				}

			}

		lbl_name_solved:

			var scope = methodscope; //Context.GC.Heap[scope_ptr];
#if DEBUG
			if (scope.Kind != RtHeapTypeKind.MethodScope)
			{
				throw new InvalidOperationException();
			}
#endif

			if (as_type == null)
			{
				as_type = GetASTypeFromValue(instance_box);
			}

			if (Context.StackPosition == Context.STACK_LENGTH)
			{
				RaiseStackOverflow(ref error);
				goto flag_handle_error;
			}



			var ns_set = scope.Type._link_codescope.NamespaceSet;
			NaNBoxing thisPtr = ((RtMethodScope)methodscope).ThisPtr;
			int code = MultiNameLSearch(ns_set, kind, as_type, name, 0, tmp_holder, stackslots, stackStPos, instance_box, check_MultiNameLSearch_issameorinherit(instance_box, thisPtr.ValueType == BoxType.HeapPtr ? Context.GC.Heap[thisPtr.HeapPtr] : null), ref error);


			switch (code)
			{
				case 0:

					if (stackslots[tmp_holder.index].HeapKind == (byte)RtHeapTypeKind.STACK_CACHE_OBJ)
					{

						SaveHeapRef(Context.GC.Heap[stackslots[tmp_holder.index].HeapPtr], source, stackslots, frame_holdchars, tmpArgLoc, scope_ptr, stackStPos, methodscope, ref error);

						if (error.raised)
						{
							goto flag_handle_error;
						}
					}
					else
					{

						Debug.Assert(stackslots[tmp_holder.index].HeapKind == (byte)RtHeapTypeKind.CLOSURE);
						RaiseReferenceError_WriteToMethod(ref error, (ASMethodBody)Context.GC.Heap[stackslots[tmp_holder.index].HeapPtr].Type, ((RtClosure)Context.GC.Heap[stackslots[tmp_holder.index].HeapPtr])._ref_as_type.QName);
						//throw new NotImplementedException($"Cannot assign to a method { cache.Type.QName.Name } on { ((RtPayloadClosure)cache)._ref_as_type.QName.Name }.");
						goto flag_handle_error;

					}


					break;
				case 1:

					goto flag_handle_error;
				case 2:

					Context.GC.CheckGC(ref error);
					RaiseTypeError_Ambiguous(ref error, name);
					goto flag_handle_error;
#if DEBUG
				//case 3:
				//    RaiseReferenceError_MulitNameNotFound(ref error, name, as_type.QName);
				//    goto flag_handle_error;
				default:

					throw new InvalidOperationException();
#endif
			}

		flag_handle_error:
			;

		}




		private unsafe void Ld_MultiNameL_Val(int super_const_index, Span<NaNBoxing> constants,
			Span<char> frame_holdchars, StackLocater src, StackLocater _name, 
			int dst_index, ASMethod method,RtHeapBase methodscope,uint* opcodePtr,

			Span<NaNBoxing> stackslots,int stackStPos,int scope_ptr,ref ReceiveError error)
		{
			StackLocater stack = default;stack.index = dst_index;

			NaNBoxing instance_box;
			RtHeapTypeKind kind;
			ASContainer as_type = null;

			if (src.index >= 0)
			{
				instance_box = stackslots[src.index];
				kind = (RtHeapTypeKind)(instance_box.ValueType == BoxType.HeapPtr ? instance_box.HeapKind : 255);
			}
			else
			{
				ReadInstanceFromStacklocater(ref error, src, stackslots, stackStPos, scope_ptr, out kind, out instance_box);
				if (error.raised)
				{
					goto flag_handle_error;
				}
			}

			if (super_const_index != 0)
			{
				//读基类
				super_const_index -= 1;

				var vbox = constants[super_const_index];

#if DEBUG
				if (vbox.ValueType != NaNBoxing.BoxType.Uint)
					throw new InvalidOperationException();
#endif

				var super_class = Context.link_const_class[(int)vbox.UIntValue];

#if DEBUG
				var check = GetASTypeFromValue(instance_box);
				if (check is ASInstance)
				{
					if (!((ASInstance)check).IsExtend(super_class.Instance))
					{
						throw new InvalidOperationException();
					}
				}

#endif

				as_type = super_class.Instance;
			}

			//RtHeapBase instance = null;
			bool setinstance = false;
			switch (instance_box.ValueType)
			{
				case NaNBoxing.BoxType.Null:
					Context.GC.CheckGC(ref error);
					RaiseTypeError_AccessNull(ref error);
					goto flag_handle_error;
				case NaNBoxing.BoxType.Undefined:
					Context.GC.CheckGC(ref error);
					RaiseTypeError_ATermUndefined(ref error);
					goto flag_handle_error;
				case NaNBoxing.BoxType.HeapPtr:
					break;
				case NaNBoxing.BoxType.Sbyte:
					as_type = Context.SBYTE.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case NaNBoxing.BoxType.Byte:
					as_type = Context.BYTE.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case NaNBoxing.BoxType.Short:
					as_type = Context.SHORT.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case NaNBoxing.BoxType.UShort:
					as_type = Context.USHORT.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case NaNBoxing.BoxType.Int:
					as_type = Context.INT.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case NaNBoxing.BoxType.Uint:
					as_type = Context.UINT.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case NaNBoxing.BoxType.Float:
					as_type = Context.FLOAT.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case NaNBoxing.BoxType.Number:
					as_type = Context.NUMBER.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case NaNBoxing.BoxType.Boolean:
					as_type = Context.BOOLEAN.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case BoxType.LocalString:
					as_type = Context.STRING.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;

#if DEBUG
				case NaNBoxing.BoxType.Fault:
				default:
					throw new InvalidOperationException();
#endif
			}


			setinstance = true;
		lbl_instance_primitive:
			Span<char> buffers = frame_holdchars; //stackalloc char[16];
			ReadOnlySpan<char> name = buffers;

			NaNBoxing prop_name = stackslots[_name.index];

			if (setinstance && (
				instance_box.HeapKind == (byte)RtHeapTypeKind.INSTANCE
				&&
				((ASInstance)Context.GC.Heap[instance_box.HeapPtr].Type).Flags.HasFlag(ClassFlags.Indexer)
				)
				||

				(instance_box.HeapKind == (byte)RtHeapTypeKind.VECTOR && RtVector.IsValidIndexType(prop_name))

				)
			{
				//索引器处理
				int ptrIndex = stackStPos + stack.index;
				int cacheobjpointer = Context.CacheObjPtr + ptrIndex;  //Context.CacheObjectPointers[ptrIndex];
				RtHeapBase cache = Context.GC.Heap[cacheobjpointer];
#if DEBUG
				if (cache.Kind != RtHeapTypeKind.STACK_CACHE_OBJ)
				{
					throw new InvalidOperationException();
				}
#endif
				if (instance_box.HeapKind == (byte)RtHeapTypeKind.VECTOR)
				{
					RtVector vector;
					int v_ptr = RtVector.FindAndUpdateHeapInstancePtr(instance_box.HeapPtr, this, out vector);
					//int maxlen; int validid;
					var store = vector.GetStore();
					if (!(store.IsValidIndexRange(prop_name, out int validid)))
					{
						RaiseRangeError(ref error, Extensions.GetPrimitiveValueToString(this, prop_name, buffers), store.length);
						goto flag_handle_error;
					}
					else
					{
						stackslots[dst_index] = store.ReadSlot(vector.element_type, validid, this, v_ptr, stackStPos + dst_index, vector.element_asclass);
					}
				}
				else
				{
					RtStackCache cachePayload = (RtStackCache)cache;
					cachePayload.RefInstance = instance_box;
					cachePayload.trait[0] = null; cachePayload.trait[1] = null;
					cachePayload.scopemember_index = 0;
					cachePayload.searchPropertyName.SetUndefined(); cachePayload.as_type = Context.GC.Heap[instance_box.HeapPtr].Type;
					cachePayload.searchNameSpacePtr = 0; cachePayload.indexer_key = prop_name;

					stackslots[dst_index] = LoadValue(cachePayload,
						stackStPos - method.Body._link_codescope.Members.Count - 2, ref error, stackslots, stackStPos + dst_index, opcodePtr
						);
					if (error.raised)
					{
						goto flag_handle_error;
					}

				}






				return;
			}
			else if (prop_name.ValueType != NaNBoxing.BoxType.HeapPtr)
			{
				if (setinstance && instance_box.HeapKind == (byte)RtHeapTypeKind.ARRAY)
				{
					long index;

					switch (prop_name.ValueType)
					{
						case BoxType.LocalString:
							// Use efficient char-based extraction to avoid string allocation

							int charCount = prop_name.GetLocalStringChars(frame_holdchars);
							name = charCount > 0 ? buffers.Slice(0, charCount) : ReadOnlySpan<char>.Empty;
							goto lbl_name_solved;
						case NaNBoxing.BoxType.Number:
							{
								double v = prop_name.Number;
								if (v >= 0 && (v == Math.Truncate(v)) && !Double.IsInfinity(v) && v < uint.MaxValue)
								{
									index = (long)v;
									if (index >= 0 && index < uint.MaxValue)
									{
										goto array_index;
									}
									else
									{
										name = index.ToString();
										goto array_prop;
									}
								}
								else
								{
									name = Extensions.GetPrimitiveValueToString(this, prop_name, buffers);
									goto array_prop;
								}
							}
						case NaNBoxing.BoxType.Float:
							{
								double v = prop_name.FloatValue;
								if (v >= 0 && (v == Math.Truncate(v)) && !Double.IsInfinity(v) && v < uint.MaxValue)
								{
									index = (long)v;
									if (index >= 0 && index < uint.MaxValue)
									{
										goto array_index;
									}
									else
									{
										name = index.ToString();
										goto array_prop;
									}
								}
								else
								{
									name = Extensions.GetPrimitiveValueToString(this, prop_name, buffers);
									goto array_prop;
								}
							}
						case NaNBoxing.BoxType.Undefined:
							name = "undefined";
							goto array_prop;
						case NaNBoxing.BoxType.Null:
							name = "null";
							goto array_prop;
						case NaNBoxing.BoxType.Boolean:
							name = prop_name.Boolean ? "true" : "false";
							goto array_prop;
						case NaNBoxing.BoxType.Int:
							{
								index = prop_name.IntValue;
								if (index >= 0)
								{
									goto array_index;
								}
								else
								{
									name = index.ToString();
									goto array_prop;
								}
							}
						case NaNBoxing.BoxType.Uint:
							{
								index = prop_name.UIntValue;
								if (index < uint.MaxValue)
								{
									goto array_index;
								}
								else
								{
									name = index.ToString();
									goto array_prop;
								}
							}
						case NaNBoxing.BoxType.Sbyte:
							{
								index = prop_name.SByteValue;
								if (index >= 0)
								{
									goto array_index;
								}
								else
								{
									name = index.ToString();
									goto array_prop;
								}
							}
						case NaNBoxing.BoxType.Byte:
							{
								index = prop_name.ByteValue;
								goto array_index;
							}
						case NaNBoxing.BoxType.Short:
							{
								index = prop_name.ShortValue;
								if (index >= 0)
								{
									goto array_index;
								}
								else
								{
									name = index.ToString();
									goto array_prop;
								}
							}
						case NaNBoxing.BoxType.UShort:
							{
								index = prop_name.UShortValue;
								goto array_index;
							}
#if DEBUG
						case NaNBoxing.BoxType.Fault:
						default:
							throw new InvalidOperationException();
#else
											default:
												Environment.FailFast("出错了，这里跑不到");

												error.error.setFault();
												goto flag_handle_error;
#endif
					}

				//索引处理
				array_index:
					uint array_i = (uint)index;
					//int ptrIndex = stackStPos + stack.index;
					//int cacheobjpointer = Context.CacheObjPtr + ptrIndex;  //Context.CacheObjectPointers[ptrIndex];

					bool isoutofindex_or_ishole;
					var a_element = LoadSlotFromArray(array_i, Context.GC.Heap[instance_box.HeapPtr], out isoutofindex_or_ishole);

					if (a_element.ValueType == BoxType.Fault)
					{
						a_element.SetUndefined();
					}
					else if (a_element.IsStruct())//v.ValueType == BoxType.HeapPtr && v.HeapKind == (byte)RtHeapTypeKind.INSTANCE && v.HeapFlag &)
					{
						a_element.SetHeapPtr(a_element.HeapPtr, (byte)RtHeapTypeKind.INSTANCE, (byte)(HeapKindFlag.FLAG_STRUCT | HeapKindFlag.FLAG_REFSTRUCT));

					}

					stackslots[dst_index] = a_element;

					//										//quickening
					//#if FORCOMPILER
					//										if (!IsComputeConstExpr)
					//										{
					//#endif
					//											if (super_const_index == 0 && src.index >=0 && 
					//											(prop_name.ValueType == BoxType.Int || prop_name.ValueType == BoxType.Byte 
					//												|| prop_name.ValueType == BoxType.Sbyte || prop_name.ValueType ==  BoxType.Short || prop_name.ValueType == BoxType.UShort) )
					//											{
					//												*opcodePtr = ((uint)INS_Code.ld_MultiNameL_Ref_ARR_INT | (0xffffff00 & (*opcodePtr)));
					//											}

					//#if FORCOMPILER
					//										}
					//#endif


					return;

				array_prop:;


				}

				else if (setinstance && instance_box.HeapKind == (byte)RtHeapTypeKind.VECTOR)
				{
					//不合理的索引范围
#if DEBUG
					if (RtVector.IsValidIndexType(prop_name))
					{
						throw new InvalidOperationException();
					}
#endif

					name = Extensions.GetPrimitiveValueToString(this, prop_name, buffers);
				}
				else
				{
					name = Extensions.GetPrimitiveValueToString(this, prop_name, buffers);
					//throw new NotImplementedException("转字符串？还是数组？");
				}
			}
			else
			{

				//RtHeapBase _n = Context.GC.Heap[prop_name.HeapPtr];
				if (prop_name.HeapKind != (byte)RtHeapTypeKind.STRING)
				{
					if (Context.StackPosition == Context.STACK_LENGTH)
					{
						RaiseStackOverflow(ref error);
						goto flag_handle_error;
					}

					var span = Context.StackSlots.AsSpan(Context.StackPosition, 1); span.Clear();
					StackLocater tmp = default; tmp.index = 0;
					int stpos = Context.StackPosition;
					Context.StackPosition++;
					NaNBoxing primitive_name = ToPrimitive(ref error, prop_name, HINT.h_string, scope_ptr, tmp, tmp, span, stpos, ((RtMethodScope)methodscope).ThisPtr);
					if (error.raised)
					{
						Context.StackPosition--;
						goto flag_handle_error;
					}

					name = Extensions.GetPrimitiveValueToString(this, primitive_name, buffers);
					Context.StackPosition--;


					//throw new NotImplementedException("转字符串？");
				}
				else
				{
					RtHeapBase _n = Context.GC.Heap[prop_name.HeapPtr];
					name = ((RtString)_n).Str;
				}

			}

		lbl_name_solved:

			var scope = methodscope; //Context.GC.Heap[scope_ptr];
#if DEBUG
			if (scope.Kind != RtHeapTypeKind.MethodScope)
			{
				throw new InvalidOperationException();
			}
#endif

			if (as_type == null)
			{
				as_type = GetASTypeFromValue(instance_box);
			}

			var ns_set = scope.Type._link_codescope.NamespaceSet;
			NaNBoxing thisPtr = ((RtMethodScope)methodscope).ThisPtr;
			int code = MultiNameLSearch(ns_set, kind, as_type, name, 0, stack, stackslots, stackStPos, instance_box, check_MultiNameLSearch_issameorinherit(instance_box, thisPtr.ValueType == BoxType.HeapPtr ? Context.GC.Heap[thisPtr.HeapPtr] : null), ref error);

			switch (code)
			{
				case 0:

					if (stackslots[stack.index].HeapKind == (byte)RtHeapTypeKind.STACK_CACHE_OBJ)
					{
						stackslots[dst_index] = LoadValue((RtStackCache)Context.GC.Heap[stackslots[stack.index].HeapPtr],
							stackStPos - method.Body._link_codescope.Members.Count - 2, ref error, stackslots, stackStPos + dst_index, opcodePtr
						);
						if (error.raised)
						{
							goto flag_handle_error;
						}
					}
					else
					{
						stackslots[dst_index] = stackslots[stack.index];
					}

					break;
				case 1:
					goto flag_handle_error;
				case 2:
					Context.GC.CheckGC(ref error);
					RaiseTypeError_Ambiguous(ref error, name);
					goto flag_handle_error;
#if DEBUG
				//case 3:
				//    RaiseReferenceError_MulitNameNotFound(ref error, name, as_type.QName);
				//    goto flag_handle_error;
				default:
					throw new InvalidOperationException();
#endif
			}


		flag_handle_error:
			;

		}



	}
}
