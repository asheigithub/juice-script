using juicescript.ABC;
using juicescript.ABC.INS;
using juicescript.ABC.Locaters;
using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace juicescript.compiler.IL.Optimize
{
	internal partial class Optimizer
	{
		internal enum InstructionDefType
		{
			/// <summary>
			/// 不能确定
			/// </summary>
			unkown,

			primitive,
			/// <summary>
			/// 堆中对象，可能能缓存的，具体能否缓存得运行时确定，所以不确定的都当可以缓存 可以缓存的约束更多更安全
			/// </summary>
			obj_maybeCacheable,
			/// <summary>
			/// 堆中对象 不可缓存的
			/// </summary>
			obj,

			/// <summary>
			/// 当前的ASScript
			/// </summary>
			global,

			/// <summary>
			/// 结构体对象
			/// </summary>
			Struct,

			/// <summary>
			/// ASClass对象
			/// </summary>
			asclass,

			/// <summary>
			/// 编译期确定的函数
			/// </summary>
			method,

			/// <summary>
			/// 编译期间确定是array
			/// </summary>
			array,

			/// <summary>
			/// 编译期间确定是vector
			/// </summary>
			vector,


		}

		internal class InstructionDef
		{
			internal InstructionDefType DefType;
			internal object Obj;

			public InstructionDef(InstructionDefType type, object obj)
			{
				Debug.Assert( (type == InstructionDefType.method && (obj is ASMethod || obj is null)) || type != InstructionDefType.method );



				this.DefType = type;
				this.Obj = obj;
			}
		}


		private static Dictionary<Instruction, List<InstructionDef>> DetectType(ASMethod method, List<Instruction> instructions, CompileContext context)
		{
			var allClasses = context.scriptDefs.SelectMany(
						s => s.scriptClasses).Union(context.player_for_compiler.Context.dictTypes.Select(p => p.Value)).Where(t => t != null);


			NaNBoxing[] constants = null;
			unsafe
			{
				fixed (byte* p = method.Body.ByteCode)
				{
					ASMethodBody.MethodBodyInfo info = new ASMethodBody.MethodBodyInfo();
					method.Body.GetInfo(ref info);
					Span<NaNBoxing> _constants = new Span<NaNBoxing>(p + 3 * sizeof(int) + 2 * sizeof(int) * info.instructions, info.constants); //(NaNBoxing*)((int*)p + 3);
					constants = new NaNBoxing[_constants.Length];
					_constants.CopyTo(constants);
				}
			}

			InstructionDef FromTypeKind(TypeKind kind)
			{
				switch (kind)
				{
					case TypeKind.Any:
						return new InstructionDef(InstructionDefType.unkown, null);
					case TypeKind.Boolean:
					case TypeKind.Int:
					case TypeKind.Uint:
					case TypeKind.SByte:
					case TypeKind.Byte:
					case TypeKind.Short:
					case TypeKind.UShort:
					case TypeKind.Float:
					case TypeKind.Number:
						return new InstructionDef(InstructionDefType.primitive,  allClasses.First(c=>c.Type_identifier == (ulong)kind).Instance );
					case TypeKind.Fun_Void:
						return new InstructionDef(InstructionDefType.primitive, TypeKind.Any);
					case TypeKind.TraitDataReference:
					case TypeKind.RTQName_MultiName_DataReference:
					case TypeKind.CParseNS_Traits:
					case TypeKind.RTQNameRTQNameL_N:
					case TypeKind.SearchNameSpaceFromImports:
					case TypeKind.Unknown:
						return new InstructionDef(InstructionDefType.unkown, null);
					case TypeKind.Null:
						return new InstructionDef(InstructionDefType.primitive, TypeKind.Null);
					case TypeKind.Object:
						return new InstructionDef(InstructionDefType.obj_maybeCacheable, null);
					case TypeKind.Class:
						return new InstructionDef(InstructionDefType.asclass, null);
					case TypeKind.Super:
						return new InstructionDef(InstructionDefType.obj_maybeCacheable, null);
					case TypeKind.String:
						return new InstructionDef(InstructionDefType.primitive, allClasses.First(c => c.Type_identifier == (ulong)kind).Instance);
					case TypeKind.Function:
						return new InstructionDef(InstructionDefType.method, null);
					case TypeKind.Array:
						return new InstructionDef(InstructionDefType.array, null);
					case TypeKind.Vector:
						return new InstructionDef(InstructionDefType.vector, null);
					case TypeKind.Namespace:
						return new InstructionDef(InstructionDefType.obj, null);
					default:
						ASClass @class = allClasses.First(c => c.Type_identifier == (ulong)kind);

						if (@class.Instance.Flags.HasFlag(ClassFlags.Struct))
						{
							return new InstructionDef(InstructionDefType.Struct, @class.Instance);
						}
						else if (@class.Instance.Flags.HasFlag(ClassFlags.Vector))
						{
							return new InstructionDef(InstructionDefType.vector, @class.Instance);
						}
						else
						{
							return new InstructionDef(InstructionDefType.obj_maybeCacheable, @class.Instance);
						}
				}

			}




			Dictionary<Instruction, List<InstructionDef>> result = new Dictionary<Instruction, List<InstructionDef>>();
			Dictionary<int, ASClass> stack_classes = new Dictionary<int, ASClass>();

			
			bool flag;

			do
			{
				flag = false;


				for (int i = 0; i < instructions.Count; i++)
				{
					var instruction = instructions[i];
					if (result.ContainsKey(instruction))
						continue;



					switch (instruction.INS_Code)
					{
						case INS_Code.flag:
							break;
						case INS_Code.ld_const:
							{
								var v = constants[((INS_Ld_Const)instruction).const_index];
								result.Add(instruction, new List<InstructionDef>() { new InstructionDef(InstructionDefType.primitive, v) });
								flag = true;
							}
							break;
						case INS_Code.ld_false:
						case INS_Code.ld_true:
							{
								result.Add(instruction, new List<InstructionDef>() { new InstructionDef(InstructionDefType.primitive, TypeKind.Boolean) });
								flag = true;
							}
							break;
						case INS_Code.ld_class:
							{
								INS_Ld_Class ld_Class = (INS_Ld_Class)instruction;
								var boxing = constants[ld_Class.classid_index];

								//ASMethodBody.PoolHeapPtrKind kind = (ASMethodBody.PoolHeapPtrKind)(boxing.HeapPtr >> 24);
								//Debug.Assert(kind == ASMethodBody.PoolHeapPtrKind.LD_Class);

								//ulong classid = context.constpool_ldclass[boxing.HeapPtr & 0xFFFFFF];

								Debug.Assert(method.Body.heapConstants.pool_kinds[boxing.HeapPtr] == ASMethodBody.PoolHeapPtrKind.LD_Class);
								ulong classid = (ulong)method.Body.heapConstants.pool_values[boxing.HeapPtr];

								ASClass @class = allClasses.First(c => c.Type_identifier == classid);
								stack_classes.Add(ld_Class.dst.index, @class);

								result.Add(instruction, new List<InstructionDef>() { new InstructionDef(InstructionDefType.asclass, @class) });
								flag = true;

							}
							break;
						case INS_Code.ld_ScopeH:
							{
								INS_Ld_ScopeHeap ld_ScopeHeap = (INS_Ld_ScopeHeap)instruction;

								var scope = method.Body._link_codescope;

								while (scope.index != ld_ScopeHeap.heap.ScopeIndex)
								{
									scope = scope.Parent;
								}

								var member = scope.Members[ld_ScopeHeap.heap.MemberIndex];
								result.Add(instruction, new List<InstructionDef>() { FromTypeKind(member.TypeKind) });

								flag = true;
							}
							break;
						
						case INS_Code.ld_InstanceOrScopeMemberValueRef:
							{
								result.Add(instruction, new List<InstructionDef>() { new InstructionDef(InstructionDefType.unkown, null) });
								flag = true;
							}
							break;
						case INS_Code.ld_methodVariable:
							{
								INS_Ld_MethodVariable ld_MethodVariable = (INS_Ld_MethodVariable)instruction;
								var member = method.Body._link_codescope.Members[ld_MethodVariable.heap.MemberIndex];

								result.Add(instruction, new List<InstructionDef>() { FromTypeKind(member.TypeKind) });
								flag = true;
							}
							break;
						case INS_Code.ld_namespace:
							{
								result.Add(instruction, new List<InstructionDef>() { new InstructionDef(InstructionDefType.obj, null) });
								flag = true;
							}
							break;
						case INS_Code.ld_RTQNameL_Ref:
							{
								result.Add(instruction, new List<InstructionDef>() { new InstructionDef(InstructionDefType.unkown, null) });
								flag = true;
							}
							break;
						case INS_Code.ld_MultiName_Ref:
							{
								result.Add(instruction, new List<InstructionDef>() { new InstructionDef(InstructionDefType.unkown, null) });
								flag = true;
							}
							break;
						case INS_Code.ld_MultiNameL_Ref:
							{
								result.Add(instruction, new List<InstructionDef>() { new InstructionDef(InstructionDefType.unkown, null) });
								flag = true;
							}
							break;
						case INS_Code.ld_MultiNameL_Val:
							{
								INS_Ld_MultiNameL_Val ld_MultiNameL_Val = (INS_Ld_MultiNameL_Val)instruction;
								if (stack_classes.ContainsKey(ld_MultiNameL_Val.instance.index))
								{
									ASClass cls = stack_classes[ld_MultiNameL_Val.instance.index];
									result.Add(instruction, new List<InstructionDef>() { new InstructionDef(InstructionDefType.unkown, null) });
									flag = true;
								}
								else
								{

									if (result.Any(r => r.Key.GetDef().Contains(ld_MultiNameL_Val.instance)))
									{
										var v = result.FirstOrDefault(r => r.Key.GetDef().Contains(ld_MultiNameL_Val.instance)).Value;
										if (v.Count == 1 && v[0].Obj != null && v[0].DefType == InstructionDefType.vector) //count==1,防止出现多个def的情况 多def还有可能多个mv到同一个目标，SSA后可能出现
										{
											Debug.Assert(v[0].Obj is ASInstance);

											var t = (ASInstance)v[0].Obj; //检查Vector的强类型
											if (t.Flags.HasFlag(ClassFlags.Vector) && t._element_class != null)
											{
												if (result.Any(r => r.Key.GetDef().Contains(ld_MultiNameL_Val.name)))
												{
													var name = result.FirstOrDefault(r => r.Key.GetDef().Contains(ld_MultiNameL_Val.name)).Value;
													if (name.Count == 1 && name[0].DefType == InstructionDefType.primitive && name[0].Obj is ASInstance)
													{
														var typekind = (TypeKind)((ASInstance)name[0].Obj)._link_codescope.TypeLayout.ASType.Type_identifier;

														if (typekind >= TypeKind.Int && typekind <= TypeKind.Number)
														{
															result.Add(instruction, new List<InstructionDef>() { FromTypeKind( (TypeKind)t._element_class.Type_identifier )  });
															flag = true;
														}
														else
														{
															result.Add(instruction, new List<InstructionDef>() { new InstructionDef(InstructionDefType.unkown, null) });
															flag = true;
														}
													}
													else
													{
														result.Add(instruction, new List<InstructionDef>() { new InstructionDef(InstructionDefType.unkown, null) });
														flag = true;
													}
												}
												else
												{
													result.Add(instruction, new List<InstructionDef>() { new InstructionDef(InstructionDefType.unkown, null) });
													flag = true;
												}
											}
											else
											{
												result.Add(instruction, new List<InstructionDef>() { new InstructionDef(InstructionDefType.unkown, null) });
												flag = true;
											}
										}
										else
										{
											result.Add(instruction, new List<InstructionDef>() { new InstructionDef(InstructionDefType.unkown, null) });
											flag = true;
										}
									}
									else
									{
										//等下一轮
										//throw new NotImplementedException();
									}
								}

								

								
							}
							break;
						case INS_Code.ld_MultiName_Val:
							{
								result.Add(instruction, new List<InstructionDef>() { new InstructionDef(InstructionDefType.unkown, null) });
								flag = true;
							}
							break;
						case INS_Code.ld_instacneOrScopeMember_Val:
							{
								INS_Ld_InstanceOrScopeMember_Val ld_instanceMeberVal = (INS_Ld_InstanceOrScopeMember_Val)instruction;
								//Optimizer_Block.cs 里，已经排除instance.index<0的情况

								//if (ld_instanceMeberVal.instance.index < 0)
								//{
								//	int scopeid = -ld_instanceMeberVal.instance.index - 1;
								//	var scope = method.Body._link_codescope;

								//	while (scope.index != scopeid)
								//	{
								//		scope = scope.Parent;
								//	}

								//	switch (scope.Kind)
								//	{
								//		case CodeScopeKind.Script:
								//			result.Add(instruction, new List<InstructionDef>() { new InstructionDef(InstructionDefType.global, scope.Container) });
								//			flag = true;
								//			break;
								//		case CodeScopeKind.Instance:
								//			{
								//				var asinstance = (ASInstance)scope.Container;
								//				if (asinstance.Flags.HasFlag(ClassFlags.Struct))
								//				{
								//					result.Add(instruction, new List<InstructionDef>() { new InstructionDef(InstructionDefType.Struct, asinstance) });
								//					flag = true;
								//				}
								//				else if (asinstance.Flags.HasFlag(ClassFlags.Vector))
								//				{
								//					throw new InvalidOperationException();
								//				}
								//				else
								//				{
								//					result.Add(instruction, new List<InstructionDef>() { new InstructionDef(InstructionDefType.obj_maybeCacheable, scope.Container) });
								//					flag = true;
								//				}
								//			}
								//			break;
								//		case CodeScopeKind.Class:
								//			result.Add(instruction, new List<InstructionDef>() { new InstructionDef(InstructionDefType.asclass, scope.Container) });
								//			flag = true;
								//			break;
								//		case CodeScopeKind.Method:
								//			result.Add(instruction, new List<InstructionDef>() { new InstructionDef(InstructionDefType.method, scope.Container) });
								//			flag = true;
								//			break;
								//		default:
								//			break;
								//	}

								//}
								//else
								{
									if (stack_classes.ContainsKey(ld_instanceMeberVal.instance.index))
									{
										ASClass cls = stack_classes[ld_instanceMeberVal.instance.index];
										var member = cls._link_codescope.Members[(int)ld_instanceMeberVal.scopemember_index];
										result.Add(instruction, new List<InstructionDef>() { FromTypeKind(member.TypeKind) });

										flag = true;
									}
									else
									{

										if (result.Any(r => r.Key.GetDef().Contains(ld_instanceMeberVal.instance)))
										{
											var v = result.FirstOrDefault(r => r.Key.GetDef().Contains(ld_instanceMeberVal.instance)).Value;
											if (v.Count == 1 && v[0].Obj != null) //count==1,防止出现多个def的情况 多def还有可能多个mv到同一个目标，SSA后可能出现
											{
												Debug.Assert(v[0].Obj is ASInstance);

												var member = ((ASInstance)v[0].Obj)._link_codescope.Members[(int)ld_instanceMeberVal.scopemember_index];
												result.Add(instruction, new List<InstructionDef>() { FromTypeKind(member.TypeKind) });

												flag = true;
											}
											else
											{
												result.Add(instruction, new List<InstructionDef>() { new InstructionDef(InstructionDefType.unkown, null) });
												flag = true;
											}
										}
										else
										{
											//等下一轮
											//throw new NotImplementedException();
										}
									}
								}


								flag = true;
							}
							break;
						case INS_Code.ld_This:
							{
								if (method.__ismethod)
								{
									var instance = (ASInstance)method.Body._link_codescope.Parent.Container;

									if (instance.Flags.HasFlag(ClassFlags.Struct))
									{
										result.Add(instruction, new List<InstructionDef>() { new InstructionDef(InstructionDefType.Struct, instance) });
									}
									//ld_this里不可能出现Vector,因为Vector全是native函数
									//else if (instance.Flags.HasFlag(ClassFlags.Vector))
									//{
									//	result.Add(instruction, new List<InstructionDef>() { new InstructionDef(InstructionDefType.vector, instance) });
									//}
									//ld_this里不可能出现Array,因为Array全是native函数

									else
									{
										result.Add(instruction, new List<InstructionDef>() { new InstructionDef(InstructionDefType.obj_maybeCacheable, instance) });
									}
								}
								else
								{
									result.Add(instruction, new List<InstructionDef>() { new InstructionDef(InstructionDefType.unkown, null) });
								}
							}
							break;
						case INS_Code.ld_VectorType:
							{
								INS_Ld_VectorType ld_VectorType = (INS_Ld_VectorType)instruction;
								var boxing = constants[ld_VectorType.vectortype_index];
								//ASMethodBody.PoolHeapPtrKind kind = (ASMethodBody.PoolHeapPtrKind)(boxing.HeapPtr >> 24);
								//Debug.Assert((kind == ASMethodBody.PoolHeapPtrKind.VectorDef));
								Debug.Assert(method.Body.heapConstants.pool_kinds[boxing.HeapPtr] == ASMethodBody.PoolHeapPtrKind.VectorDef);

								var vectorDef = (VectorDef)method.Body.heapConstants.pool_values[boxing.HeapPtr];  //context.vectorDefs[boxing.HeapPtr & 0xFFFFFF];
								var @class = allClasses.First(c => c.Type_identifier == (ulong)vectorDef.Identifier);

								stack_classes.Add(ld_VectorType.dst.index, @class);

								result.Add(instruction, new List<InstructionDef>() { new InstructionDef(InstructionDefType.asclass, @class) });
								flag = true;

							}
							break;
						case INS_Code.ld_null:
							{
								result.Add(instruction, new List<InstructionDef>() { new InstructionDef(InstructionDefType.primitive, TypeKind.Null) });
								flag = true;
							}
							break;
						case INS_Code.ld_undefined:
							{
								result.Add(instruction, new List<InstructionDef>() { new InstructionDef(InstructionDefType.primitive, TypeKind.Any) });
								flag = true;
							}
							break;
						case INS_Code.ld_array_hole:
							{
								result.Add(instruction, new List<InstructionDef>() { new InstructionDef(InstructionDefType.primitive, TypeKind.Any) });
								flag = true;
							}
							break;
						case INS_Code.ld_function:
							{
								INS_Ld_Function ld_Function = (INS_Ld_Function)instruction;
								var boxing = constants[ld_Function.const_index];

								Debug.Assert(method.Body.heapConstants.pool_kinds[boxing.HeapPtr] == ASMethodBody.PoolHeapPtrKind.Method);
								var m = (ASMethod)method.Body.heapConstants.pool_values[boxing.HeapPtr];
								//ASMethodBody.PoolHeapPtrKind kind = (ASMethodBody.PoolHeapPtrKind)(boxing.HeapPtr >> 24);
								//Debug.Assert((kind == ASMethodBody.PoolHeapPtrKind.Method));

								//int ptr = boxing.HeapPtr & 0xFFFFFF;
								//var methodscope = context.player_for_compiler.Context.GC.Heap[ptr];
								//Debug.Assert(methodscope.Kind == RtHeapTypeKind.MethodScope);
								//var m = ((ASMethodBody)methodscope.Type).Method;

								result.Add(instruction, new List<InstructionDef>() { new InstructionDef(InstructionDefType.method, m) });
								flag = true;

							}
							break;
						case INS_Code.ld_arguments:
							{
								result.Add(instruction, new List<InstructionDef>() { new InstructionDef(InstructionDefType.array, null) });
								flag = true;
							}
							break;
						case INS_Code.ld_method:
							{
								//必须先确认instance,再从虚函数表里查询
								INS_Ld_Method ld_Method = (INS_Ld_Method)instruction;

								if (result.Any(r => r.Key.GetDef().Contains(ld_Method.instance)))
								{
									var v = result.FirstOrDefault(r => r.Key.GetDef().Contains(ld_Method.instance));
									var index = v.Key.GetDef().TakeWhile(k => !k.Equals(ld_Method.instance)).Count(); //.IndexOf(ld_Method.instance);
									var d = v.Value[index];

									switch (d.DefType)
									{
										case InstructionDefType.unkown:
											result.Add(instruction, new List<InstructionDef>() { new InstructionDef(InstructionDefType.unkown, null) });
											flag = true;
											break;
										case InstructionDefType.primitive:
											//throw new NotImplementedException();
											{
												if (d.Obj is ASInstance)
												{
													result.Add(instruction, new List<InstructionDef>()
												{
													new InstructionDef(InstructionDefType.method,
													((ASContainer)d.Obj)._vtable.Items[ (int)ld_Method.const_index ].Trait.Method )
												});
													flag = true;
												}
												else if (d.Obj is NaNBoxing)
												{
													NaNBoxing val = (NaNBoxing)d.Obj;
													switch (val.ValueType)
													{
														case NaNBoxing.BoxType.Number:
															result.Add(instruction, new List<InstructionDef>()
														{
															new InstructionDef(InstructionDefType.method,
															(allClasses.First(c => c.Type_identifier ==(ulong) TypeKind.Number)).Instance._vtable.Items[(int) ld_Method.const_index].Trait.Method )
														});
															flag = true;
															break;
														case NaNBoxing.BoxType.Undefined:
															throw new InvalidOperationException();
															break;
														case NaNBoxing.BoxType.Null:
															throw new InvalidOperationException();
															break;
														case NaNBoxing.BoxType.Boolean:
															result.Add(instruction, new List<InstructionDef>()
														{
															new InstructionDef(InstructionDefType.method,
															(allClasses.First(c => c.Type_identifier ==(ulong) TypeKind.Boolean)).Instance._vtable.Items[(int) ld_Method.const_index].Trait.Method )
														});
															flag = true;
															break;
														case NaNBoxing.BoxType.Int:
															result.Add(instruction, new List<InstructionDef>()
														{
															new InstructionDef(InstructionDefType.method,
															(allClasses.First(c => c.Type_identifier ==(ulong) TypeKind.Int)).Instance._vtable.Items[(int) ld_Method.const_index].Trait.Method )
														});
															flag = true;
															break;
														case NaNBoxing.BoxType.Uint:
															result.Add(instruction, new List<InstructionDef>()
														{
															new InstructionDef(InstructionDefType.method,
															(allClasses.First(c => c.Type_identifier ==(ulong) TypeKind.Uint)).Instance._vtable.Items[(int) ld_Method.const_index].Trait.Method )
														});
															flag = true;
															break;
														case NaNBoxing.BoxType.Sbyte:
															result.Add(instruction, new List<InstructionDef>()
														{
															new InstructionDef(InstructionDefType.method,
															(allClasses.First(c => c.Type_identifier ==(ulong) TypeKind.SByte)).Instance._vtable.Items[(int) ld_Method.const_index].Trait.Method )
														});
															flag = true;
															break;
														case NaNBoxing.BoxType.Byte:
															result.Add(instruction, new List<InstructionDef>()
														{
															new InstructionDef(InstructionDefType.method,
															(allClasses.First(c => c.Type_identifier ==(ulong) TypeKind.Byte)).Instance._vtable.Items[(int) ld_Method.const_index].Trait.Method )
														});
															flag = true;
															break;
														case NaNBoxing.BoxType.Short:
															result.Add(instruction, new List<InstructionDef>()
														{
															new InstructionDef(InstructionDefType.method,
															(allClasses.First(c => c.Type_identifier ==(ulong) TypeKind.Short)).Instance._vtable.Items[(int) ld_Method.const_index].Trait.Method )
														});
															flag = true;
															break;
														case NaNBoxing.BoxType.UShort:
															result.Add(instruction, new List<InstructionDef>()
														{
															new InstructionDef(InstructionDefType.method,
															(allClasses.First(c => c.Type_identifier ==(ulong) TypeKind.UShort)).Instance._vtable.Items[(int) ld_Method.const_index].Trait.Method )
														});
															flag = true;
															break;
														case NaNBoxing.BoxType.Float:
															result.Add(instruction, new List<InstructionDef>()
														{
															new InstructionDef(InstructionDefType.method,
															(allClasses.First(c => c.Type_identifier ==(ulong) TypeKind.Float)).Instance._vtable.Items[(int) ld_Method.const_index].Trait.Method )
														});
															flag = true;
															break;
														case NaNBoxing.BoxType.HeapPtr:

															//ASMethodBody.PoolHeapPtrKind kind = (ASMethodBody.PoolHeapPtrKind)(val.HeapPtr >> 24);
															//Debug.Assert((kind == ASMethodBody.PoolHeapPtrKind.String));
															Debug.Assert(method.Body.heapConstants.pool_kinds[val.HeapPtr] == ASMethodBody.PoolHeapPtrKind.String );

															result.Add(instruction, new List<InstructionDef>()
														{
															new InstructionDef(InstructionDefType.method,
															(allClasses.First(c => c.Type_identifier ==(ulong) TypeKind.String)).Instance._vtable.Items[(int) ld_Method.const_index].Trait.Method )
														});
															flag = true;
															//throw new InvalidOperationException();
															break;
														case NaNBoxing.BoxType.LocalString:
															result.Add(instruction, new List<InstructionDef>()
														{
															new InstructionDef(InstructionDefType.method,
															(allClasses.First(c => c.Type_identifier ==(ulong) TypeKind.String)).Instance._vtable.Items[(int) ld_Method.const_index].Trait.Method )
														});
															flag = true;
															break;
														case NaNBoxing.BoxType.Fault:
															throw new InvalidOperationException();
															break;
														default:
															break;
													}

												}
												else
												{
													//无法确认
												}
											}
											break;
										case InstructionDefType.obj_maybeCacheable:
										case InstructionDefType.obj:
											result.Add(instruction, new List<InstructionDef>()
										{
											new InstructionDef(InstructionDefType.method,
											((ASContainer) d.Obj)._vtable.Items[(int) ld_Method.const_index].Trait.Method )
										});
											flag = true;
											break;
										case InstructionDefType.global:
											throw new InvalidOperationException();
											break;
										case InstructionDefType.Struct:
											result.Add(instruction, new List<InstructionDef>()
										{
											new InstructionDef(InstructionDefType.method,
											((ASContainer) d.Obj)._vtable.Items[(int) ld_Method.const_index].Trait.Method )
										});
											flag = true;
											break;
										case InstructionDefType.asclass:
											result.Add(instruction, new List<InstructionDef>()
										{
											new InstructionDef(InstructionDefType.method,
											((ASContainer) d.Obj)._vtable.Items[(int) ld_Method.const_index].Trait.Method )
										});
											flag = true;
											break;
										case InstructionDefType.method:
											result.Add(instruction, new List<InstructionDef>()
										{
											new InstructionDef(InstructionDefType.method,
											allClasses.First(c => c.Type_identifier ==(ulong) TypeKind.Function).Instance._vtable.Items[(int) ld_Method.const_index].Trait.Method )
										});
											flag = true;
											break;
										case InstructionDefType.array:

											result.Add(instruction, new List<InstructionDef>()
										{
											new InstructionDef(InstructionDefType.method,
											allClasses.First(c => c.Type_identifier ==(ulong) TypeKind.Array).Instance._vtable.Items[(int) ld_Method.const_index].Trait.Method )
										});
											flag = true;
											break;
										case InstructionDefType.vector:

											result.Add(instruction, new List<InstructionDef>()
										{
											new InstructionDef(InstructionDefType.method,
											((ASContainer) d.Obj)._vtable.Items[(int) ld_Method.const_index].Trait.Method )
										});
											flag = true;
											break;
										default:
											break;
									}


								}
								else
								{
									//等下一轮 
								}
							}
							break;
						case INS_Code.ld_supermethod:
							{
								INS_Ld_SuperMethod ld_SuperMethod = (INS_Ld_SuperMethod)instruction;
								NaNBoxing fbox = constants[ld_SuperMethod.const_index];
								Debug.Assert(method.Body.heapConstants.pool_kinds[fbox.HeapPtr] == ASMethodBody.PoolHeapPtrKind.SuperMethod);
								Tuple<ASClass,int> tuple = (Tuple<ASClass, int>)method.Body.heapConstants.pool_values[fbox.HeapPtr];
								ASMethod m = tuple.Item1.Instance._vtable.Items[tuple.Item2].Trait.Method;

								//Debug.Assert(fbox.ValueType == NaNBoxing.BoxType.HeapPtr);
								//Debug.Assert(fbox.HeapPtr >> 24 == (byte)ASMethodBody.PoolHeapPtrKind.SuperMethod);

								//RtHeapBase heapInstance = context.player_for_compiler.Context.GC.Heap[fbox.HeapPtr & 0xffffff];
								//Debug.Assert(heapInstance.Kind == RtHeapTypeKind.MethodScope);

								//var m = ((ASClass)heapInstance.Type).Instance._vtable.Items[((RtMethodScope)heapInstance).ParentPtr].Trait.Method;
								result.Add(instruction, new List<InstructionDef>()
							{
								new InstructionDef(InstructionDefType.method,m )
							});
								flag = true;
							}
							break;
						case INS_Code.ld_interface_method:
							{
								INS_Ld_Method_Interface ld_Method_Interface = (INS_Ld_Method_Interface)instruction;
								var box = constants[ld_Method_Interface.class_id];

								Debug.Assert(method.Body.heapConstants.pool_kinds[box.HeapPtr] == ASMethodBody.PoolHeapPtrKind.LD_Class);
								ulong classid = (ulong)method.Body.heapConstants.pool_values[box.HeapPtr];

								//ASMethodBody.PoolHeapPtrKind kind = (ASMethodBody.PoolHeapPtrKind)(box.HeapPtr >> 24);
								//Debug.Assert(kind == ASMethodBody.PoolHeapPtrKind.LD_Class);

								//ulong classid = context.constpool_ldclass[box.HeapPtr & 0xFFFFFF];
								ASClass @class = allClasses.First(c => c.Type_identifier == classid);

								result.Add(instruction, new List<InstructionDef>()
							{
								new InstructionDef(InstructionDefType.method, @class.Instance._vtable.Items[(int) ld_Method_Interface.const_index ].Trait.Method  )
							});

								flag = true;
							}
							break;
						case INS_Code.storeScopeH:
							{
								//不定值
							}
							break;
						case INS_Code.storeMethodVariable:
							{
								INS_Store_MethodVariable store_MethodVariable = (INS_Store_MethodVariable)instruction;
								//method.Body._link_codescope.Members[store_MethodVariable.heap.MemberIndex];
								//

								if (method.Body._link_codescope.Members[store_MethodVariable.heap.MemberIndex].TypeKind == TypeKind.Any)
								{
									if (result.Any(r => r.Key.GetDef().Contains(store_MethodVariable.dst)))
									{
										var kv = result.First(r => r.Key.GetDef().Contains(store_MethodVariable.dst));
												int index = kv.Key.GetDef().TakeWhile(k => !k.Equals(store_MethodVariable.dst)).Count();

												var d = kv.Value[index];

												result.Add(instruction, new List<InstructionDef>() {
											 new InstructionDef( d.DefType,d.Obj)
											});

										flag = true;

									}
									else
									{
										//等下一轮
										//throw new NotImplementedException();
									}
								}
								else
								{
									result.Add(instruction, new List<InstructionDef>() {
											FromTypeKind( method.Body._link_codescope.Members[ store_MethodVariable.heap.MemberIndex ].TypeKind )

										});
									flag = true;
								}
								

							}
							break;
						case INS_Code.storeHeapValueRef:
							{
								//不定值
							}
							break;
						case INS_Code.ld_MethodVariableInitValue:
							{

								result.Add(instruction, new List<InstructionDef>() {
							 new InstructionDef( InstructionDefType.primitive,TypeKind.Any)
							});

								flag = true;
							}
							break;
						case INS_Code.ld_memberInitValue:
							{
								//不定值
							}
							break;
						case INS_Code.ld_ValueRef:
							{
								result.Add(instruction, new List<InstructionDef>() {
							 new InstructionDef( InstructionDefType.unkown,null)
							});

								flag = true;
							}
							break;
						case INS_Code.move:
							{
								INS_Move move = (INS_Move)instruction;
								if (result.Any(r => r.Key.GetDef().Contains(move.source)))
								{
									var kv = result.First(r => r.Key.GetDef().Contains(move.source));
									int index = kv.Key.GetDef().TakeWhile(k => !k.Equals(move.source)).Count();

									var d = kv.Value[index];

									result.Add(instruction, new List<InstructionDef>() {
									 new InstructionDef( d.DefType,d.Obj)
									});

									flag = true;

								}
								else
								{
									//等下一轮
								}
							}
							break;
						case INS_Code.delete:
							{
								result.Add(instruction, new List<InstructionDef>() {

									 new InstructionDef( InstructionDefType.primitive,TypeKind.Any)
									});
								flag = true;

							}
							break;
						case INS_Code.positive:
							{
								//result.Add(instruction, new List<InstructionDef>() {
								//		 new InstructionDef( InstructionDefType.primitive,TypeKind.Any)
								//		});

								//flag = true;
								INS_Positive positive = (INS_Positive)instruction;

								if (result.Any(r => r.Key.GetDef().Contains(positive.src)))
								{
									var kv = result.First(r => r.Key.GetDef().Contains(positive.src));
									int index = kv.Key.GetDef().TakeWhile(k => !k.Equals(positive.src)).Count();

									var d = kv.Value[index];

									if (d.DefType == InstructionDefType.primitive)
									{
										result.Add(instruction, new List<InstructionDef>() {
									 new InstructionDef( InstructionDefType.primitive , TypeKind.Any)
									});
									}
									else
									{
										result.Add(instruction, new List<InstructionDef>() {
									 new InstructionDef( InstructionDefType.unkown , null)
									});
									}

									flag = true;

								}
								else
								{
									//等下一轮
								}


							}
							break;
						case INS_Code.neg:
							{
								INS_Neg neg = (INS_Neg)instruction;

								if (result.Any(r => r.Key.GetDef().Contains(neg.src)))
								{
									var kv = result.First(r => r.Key.GetDef().Contains(neg.src));
									int index = kv.Key.GetDef().TakeWhile(k => !k.Equals(neg.src)).Count();

									var d = kv.Value[index];

									if (d.DefType == InstructionDefType.primitive)
									{
										result.Add(instruction, new List<InstructionDef>() {
									 new InstructionDef( InstructionDefType.primitive , TypeKind.Any)
									});
									}
									else
									{
										//由于操作符重载的存在，所以这里暂时先不去考虑这些。
										result.Add(instruction, new List<InstructionDef>() {
									 new InstructionDef( InstructionDefType.unkown , null)
									});
									}

									flag = true;

								}
								else
								{
									//等下一轮
								}
							}
							break;
						case INS_Code.multiply:
							{
								INS_Multiply multiply = (INS_Multiply)instruction;

								if (result.Any(r => r.Key.GetDef().Contains(multiply.v1)) && result.Any(r => r.Key.GetDef().Contains(multiply.v2)))
								{
									var kv1 = result.First(r => r.Key.GetDef().Contains(multiply.v1));
									var kv2 = result.First(r => r.Key.GetDef().Contains(multiply.v2));

									int index1 = kv1.Key.GetDef().TakeWhile(k => !k.Equals(multiply.v1)).Count();
									int index2 = kv2.Key.GetDef().TakeWhile(k => !k.Equals(multiply.v2)).Count();

									var d1 = kv1.Value[index1];
									var d2 = kv2.Value[index2];

									if (d1.DefType == InstructionDefType.primitive && d2.DefType == InstructionDefType.primitive)
									{
										result.Add(instruction, new List<InstructionDef>() {
									 new InstructionDef( InstructionDefType.primitive , TypeKind.Any)
									});
									}
									else
									{
										//由于操作符重载的存在，所以这里暂时先不去考虑这些。
										result.Add(instruction, new List<InstructionDef>() {
									 new InstructionDef( InstructionDefType.unkown , null)
									});
									}

									flag = true;
								}
								else
								{

								}
							}
							break;
						case INS_Code.div:
							{
								INS_Div div = (INS_Div)instruction;
								if (result.Any(r => r.Key.GetDef().Contains(div.v1)) && result.Any(r => r.Key.GetDef().Contains(div.v2)))
								{
									var kv1 = result.First(r => r.Key.GetDef().Contains(div.v1));
									var kv2 = result.First(r => r.Key.GetDef().Contains(div.v2));

									int index1 = kv1.Key.GetDef().TakeWhile(k => !k.Equals(div.v1)).Count();
									int index2 = kv2.Key.GetDef().TakeWhile(k => !k.Equals(div.v2)).Count();

									var d1 = kv1.Value[index1];
									var d2 = kv2.Value[index2];

									if (d1.DefType == InstructionDefType.primitive && d2.DefType == InstructionDefType.primitive)
									{
										result.Add(instruction, new List<InstructionDef>() {
									 new InstructionDef( InstructionDefType.primitive , TypeKind.Any)
									});
									}
									else
									{
										//由于操作符重载的存在，所以这里暂时先不去考虑这些。
										result.Add(instruction, new List<InstructionDef>() {
									 new InstructionDef( InstructionDefType.unkown , null)
									});
									}

									flag = true;
								}
								else
								{

								}
							}
							break;
						case INS_Code.add_Vec2_Vec2:
						case INS_Code.sub_Vec2_Vec2:
						case INS_Code.scale_Vec2:
						case INS_Code.scale_Vec2_reciprocal:
						case INS_Code.neg_pos_Vec2:
						case INS_Code.mul_Mat22_Vec2:
							{
								result.Add(instruction, new List<InstructionDef>() {
									 FromTypeKind(TypeKind.Vector2) });
							}
							break;
						case INS_Code.mul_Mat22_Mat22:
						case INS_Code.add_Mat22_Mat22:
							{
								result.Add(instruction, new List<InstructionDef>() {
									 FromTypeKind(TypeKind.Matrix2x2) });
							}
							break;
						case INS_Code.add:
							{
								INS_Add add = (INS_Add)instruction;
								if (result.Any(r => r.Key.GetDef().Contains(add.v1)) && result.Any(r => r.Key.GetDef().Contains(add.v2)))
								{
									var kv1 = result.First(r => r.Key.GetDef().Contains(add.v1));
									var kv2 = result.First(r => r.Key.GetDef().Contains(add.v2));

									int index1 = kv1.Key.GetDef().TakeWhile(k => !k.Equals(add.v1)).Count();
									int index2 = kv2.Key.GetDef().TakeWhile(k => !k.Equals(add.v2)).Count();

									var d1 = kv1.Value[index1];
									var d2 = kv2.Value[index2];

									if (d1.DefType == InstructionDefType.primitive && d2.DefType == InstructionDefType.primitive)
									{
										result.Add(instruction, new List<InstructionDef>() {
									 new InstructionDef( InstructionDefType.primitive , TypeKind.Any)
									});
									}
									else
									{
										//由于操作符重载的存在，所以这里暂时先不去考虑这些。
										result.Add(instruction, new List<InstructionDef>() {
									 new InstructionDef( InstructionDefType.unkown , null)
									});
									}

									flag = true;
								}
								else
								{

								}
							}
							break;
						
						case INS_Code.sub:
							{
								INS_Sub sub = (INS_Sub)instruction;
								if (result.Any(r => r.Key.GetDef().Contains(sub.v1)) && result.Any(r => r.Key.GetDef().Contains(sub.v2)))
								{
									var kv1 = result.First(r => r.Key.GetDef().Contains(sub.v1));
									var kv2 = result.First(r => r.Key.GetDef().Contains(sub.v2));

									int index1 = kv1.Key.GetDef().TakeWhile(k => !k.Equals(sub.v1)).Count();
									int index2 = kv2.Key.GetDef().TakeWhile(k => !k.Equals(sub.v2)).Count();

									var d1 = kv1.Value[index1];
									var d2 = kv2.Value[index2];

									if (d1.DefType == InstructionDefType.primitive && d2.DefType == InstructionDefType.primitive)
									{
										result.Add(instruction, new List<InstructionDef>() {
									 new InstructionDef( InstructionDefType.primitive , TypeKind.Any)
									});
									}
									else
									{
										//由于操作符重载的存在，所以这里暂时先不去考虑这些。
										result.Add(instruction, new List<InstructionDef>() {
									 new InstructionDef( InstructionDefType.unkown , null)
									});
									}

									flag = true;
								}
								else
								{

								}
							}
							break;
						case INS_Code.modulus:
							{
								INS_Modulus modulus = (INS_Modulus)instruction;
								if (result.Any(r => r.Key.GetDef().Contains(modulus.v1)) && result.Any(r => r.Key.GetDef().Contains(modulus.v2)))
								{
									var kv1 = result.First(r => r.Key.GetDef().Contains(modulus.v1));
									var kv2 = result.First(r => r.Key.GetDef().Contains(modulus.v2));

									int index1 = kv1.Key.GetDef().TakeWhile(k => !k.Equals(modulus.v1)).Count();
									int index2 = kv2.Key.GetDef().TakeWhile(k => !k.Equals(modulus.v2)).Count();

									var d1 = kv1.Value[index1];
									var d2 = kv2.Value[index2];

									if (d1.DefType == InstructionDefType.primitive && d2.DefType == InstructionDefType.primitive)
									{
										result.Add(instruction, new List<InstructionDef>() {
									 new InstructionDef( InstructionDefType.primitive , TypeKind.Any)
									});
									}
									else
									{
										//由于操作符重载的存在，所以这里暂时先不去考虑这些。
										result.Add(instruction, new List<InstructionDef>() {
									 new InstructionDef( InstructionDefType.unkown , null)
									});
									}

									flag = true;
								}
								else
								{

								}


							}
							break;
						case INS_Code.bitwise:  //位操作的结果只能是数值
							{
								INS_BitWise bitWise = (INS_BitWise)instruction;

								result.Add(instruction, new List<InstructionDef>() {
								new InstructionDef( InstructionDefType.primitive , TypeKind.Any)
							});
								flag = true;
							}
							break;
						case INS_Code.logic_not:
						case INS_Code.logic_comparison:
						case INS_Code.strict_eq:
						case INS_Code.strict_neq:
						case INS_Code.equal:
						case INS_Code.not_equal:
							{
								//这些的结果肯定是布尔值

								result.Add(instruction, new List<InstructionDef>() {
								new InstructionDef( InstructionDefType.primitive , TypeKind.Boolean)
							});
								flag = true;

							}
							break;
						case INS_Code.get_in:
							{
								result.Add(instruction, new List<InstructionDef>() {
								new InstructionDef( InstructionDefType.primitive , TypeKind.Boolean)
							});
								flag = true;
							}
							break;
						case INS_Code.get_typeof:
							{
								result.Add(instruction, new List<InstructionDef>() {
								new InstructionDef( InstructionDefType.primitive , TypeKind.Boolean)
							});
								flag = true;
							}
							break;
						case INS_Code.get_instanceof:
							{
								result.Add(instruction, new List<InstructionDef>() {
								new InstructionDef( InstructionDefType.primitive , TypeKind.Boolean)
							});
								flag = true;
							}
							break;
						case INS_Code.get_is:
							{
								result.Add(instruction, new List<InstructionDef>() {
								new InstructionDef( InstructionDefType.primitive , TypeKind.Boolean)
							});
								flag = true;
							}
							break;
						case INS_Code.cast_as:
							{
								INS_As iNS_As = (INS_As)instruction;
								if (result.Any((r => r.Key.GetDef().Contains(iNS_As.v2))))
								{
									var kv = result.First(r => r.Key.GetDef().Contains(iNS_As.v2));

									int index = kv.Key.GetDef().TakeWhile(k => !k.Equals(iNS_As.v2)).Count();
									var d = kv.Value[index];

									if (d.DefType == InstructionDefType.asclass)
									{
										var dd = FromTypeKind((TypeKind)((ASClass)d.Obj).Type_identifier);

										result.Add(instruction, new List<InstructionDef>() {
										dd
									});
										flag = true;

									}
									else
									{
										result.Add(instruction, new List<InstructionDef>() {
										new InstructionDef( InstructionDefType.unkown , null)
									});
										flag = true;
									}
								}
								else
								{
									//等下一轮
								}
							}
							break;
						case INS_Code.increment_decrement:
							{
								INS_Incr_Decr incr_Decr = (INS_Incr_Decr)instruction;
								if (result.Any((r => r.Key.GetDef().Contains(incr_Decr.source))))
								{
									var incr_def = incr_Decr.GetDef();

									var out_def = new List<InstructionDef>();


									if (incr_def.Count() == 1)
									{
										out_def.Add(new InstructionDef(InstructionDefType.primitive, TypeKind.Number));
									}
									else
									{
										var kv = result.First(r => r.Key.GetDef().Contains(incr_Decr.source));
										int index = kv.Key.GetDef().TakeWhile(k => !k.Equals(incr_Decr.source)).Count();
										var dsrc = kv.Value[index];

										out_def.Add(new InstructionDef(dsrc.DefType, dsrc.Obj));
										out_def.Add(new InstructionDef(InstructionDefType.primitive, TypeKind.Number));
									}


									result.Add(instruction, out_def);
									flag = true;
									//throw new NotImplementedException();

								}
								else
								{

								}
							}
							break;
						case INS_Code.new_instance:
							{
								INS_New_Instance new_Instance = (INS_New_Instance)instruction;
								if (result.Any((r => r.Key.GetDef().Contains(new_Instance.typeLocator))))
								{
									var kv = result.First(r => r.Key.GetDef().Contains(new_Instance.typeLocator));
									int index = kv.Key.GetDef().TakeWhile(k => !k.Equals(new_Instance.typeLocator)).Count();
									var typeDef = kv.Value[index];

									if (typeDef.DefType == InstructionDefType.asclass)
									{
										if (typeDef.Obj != null)
										{
											var dd = FromTypeKind((TypeKind)((ASClass)typeDef.Obj).Type_identifier);

											result.Add(instruction, new List<InstructionDef>() {
											dd
										});
											flag = true;
										}
										else
										{   //Class本身
											result.Add(instruction, new List<InstructionDef>() {
											new InstructionDef(InstructionDefType.unkown,null)
										});
											flag = true;
										}
									}									
									else
									{
										result.Add(instruction, new List<InstructionDef>() {
										new InstructionDef(InstructionDefType.unkown,null)
									});
										flag = true;
									}
								}
								else
								{

								}

							}
							break;
						case INS_Code.create_prop:
							{
								//无定值
							}
							break;
						case INS_Code.type_cast:
							{
								INS_TypeCast typeCast = (INS_TypeCast)instruction;

								var box = constants[typeCast.class_id];

								Debug.Assert(method.Body.heapConstants.pool_kinds[box.HeapPtr] == ASMethodBody.PoolHeapPtrKind.LD_Class);
								ulong classid = (ulong)method.Body.heapConstants.pool_values[box.HeapPtr];

								//ASMethodBody.PoolHeapPtrKind kind = (ASMethodBody.PoolHeapPtrKind)(box.HeapPtr >> 24);
								//Debug.Assert(kind == ASMethodBody.PoolHeapPtrKind.LD_Class);

								//ulong classid = context.constpool_ldclass[box.HeapPtr & 0xFFFFFF];
								ASClass @class = allClasses.First(c => c.Type_identifier == classid);

								result.Add(instruction, new List<InstructionDef>()
							{
								FromTypeKind((TypeKind)@class.Type_identifier)
							});

								flag = true;


							}

							break;
						case INS_Code.super_ctor:
							{
								//无定值
							}
							break;
						case INS_Code.ld_length:
							{
								//基本都是number
								result.Add(instruction, new List<InstructionDef>()
							{
								new InstructionDef(InstructionDefType.primitive, TypeKind.Number)
							});

								flag = true;

							}
							break;
						case INS_Code.ld_function_call:
							{
								INS_Ld_Function_Call ld_Function_Call = (INS_Ld_Function_Call)instruction;

								var boxing = constants[ld_Function_Call.const_index];
								Debug.Assert(method.Body.heapConstants.pool_kinds[boxing.HeapPtr] == ASMethodBody.PoolHeapPtrKind.Method);
								var m = (ASMethod)method.Body.heapConstants.pool_values[boxing.HeapPtr];

								//ASMethodBody.PoolHeapPtrKind kind = (ASMethodBody.PoolHeapPtrKind)(boxing.HeapPtr >> 24);
								//Debug.Assert((kind == ASMethodBody.PoolHeapPtrKind.Method));

								//int ptr = boxing.HeapPtr & 0xFFFFFF;
								//var methodscope = context.player_for_compiler.Context.GC.Heap[ptr];
								//Debug.Assert(methodscope.Kind == RtHeapTypeKind.MethodScope);
								//var m = ((ASMethodBody)methodscope.Type).Method;

								result.Add(instruction, new List<InstructionDef>() { FromTypeKind(m.ReturnTypeKind) });
								flag = true;

							}
							break;
						case INS_Code.ld_function_bindglobal_call:
							{
								INS_Ld_Function_BindGlobal_Call ld_Function_BindGlobal_Call = (INS_Ld_Function_BindGlobal_Call)instruction;

								var boxing = constants[ld_Function_BindGlobal_Call.const_index];
								Debug.Assert(method.Body.heapConstants.pool_kinds[boxing.HeapPtr] == ASMethodBody.PoolHeapPtrKind.Method);
								var m = (ASMethod)method.Body.heapConstants.pool_values[boxing.HeapPtr];

								//ASMethodBody.PoolHeapPtrKind kind = (ASMethodBody.PoolHeapPtrKind)(boxing.HeapPtr >> 24);
								//Debug.Assert((kind == ASMethodBody.PoolHeapPtrKind.Method));

								//int ptr = boxing.HeapPtr & 0xFFFFFF;
								//var methodscope = context.player_for_compiler.Context.GC.Heap[ptr];
								//Debug.Assert(methodscope.Kind == RtHeapTypeKind.MethodScope);

								//var m = ((ASMethodBody)methodscope.Type).Method;
								result.Add(instruction, new List<InstructionDef>() { FromTypeKind(m.ReturnTypeKind) });
								flag = true;

							}
							break;
						case INS_Code.bindthis_call:
							{
								INS_BindThis_Call bindThis_Call = (INS_BindThis_Call)instruction;

								if (result.Any((r => r.Key.GetDef().Contains(bindThis_Call.function))))
								{
									var kv = result.First(r => r.Key.GetDef().Contains(bindThis_Call.function));
									int index = kv.Key.GetDef().TakeWhile(k => !k.Equals(bindThis_Call.function)).Count();
									var typeDef = kv.Value[index];

									if (typeDef.DefType == InstructionDefType.method && typeDef.Obj != null)
									{

										result.Add(instruction, new List<InstructionDef>() { FromTypeKind(((ASMethod)typeDef.Obj).ReturnTypeKind) });
										flag = true;
									}
									else
									{
										result.Add(instruction, new List<InstructionDef>() {
										new InstructionDef(InstructionDefType.unkown,null)
									});
										flag = true;
									}

								}
								else
								{
									//等下一轮
								}
							}
							break;
						case INS_Code.bindglobal_call:
							{
								INS_bindGlobal_Call bindGlobal_Call = (INS_bindGlobal_Call)instruction;

								if (result.Any((r => r.Key.GetDef().Contains(bindGlobal_Call.function))))
								{
									var kv = result.First(r => r.Key.GetDef().Contains(bindGlobal_Call.function));
									int index = kv.Key.GetDef().TakeWhile(k => !k.Equals(bindGlobal_Call.function)).Count();
									var typeDef = kv.Value[index];

									if (typeDef.DefType == InstructionDefType.method && typeDef.Obj != null)
									{
										result.Add(instruction, new List<InstructionDef>() { FromTypeKind(((ASMethod)typeDef.Obj).ReturnTypeKind) });
										flag = true;
									}
									else
									{
										result.Add(instruction, new List<InstructionDef>() {
										new InstructionDef(InstructionDefType.unkown,null)
									});
										flag = true;
									}

								}
								else
								{
									//等下一轮
								}

							}
							break;
						case INS_Code.method_call:
							{
								INS_Method_Call method_Call = (INS_Method_Call)instruction;
								if (result.Any((r => r.Key.GetDef().Contains(method_Call.function))))
								{
									var kv = result.First(r => r.Key.GetDef().Contains(method_Call.function));
									int index = kv.Key.GetDef().TakeWhile(k => !k.Equals(method_Call.function)).Count();
									var typeDef = kv.Value[index];

									if (typeDef.DefType == InstructionDefType.method && typeDef.Obj != null)
									{
										result.Add(instruction, new List<InstructionDef>() { FromTypeKind(((ASMethod)typeDef.Obj).ReturnTypeKind) });
										flag = true;
									}
									else
									{
										result.Add(instruction, new List<InstructionDef>() {
										new InstructionDef(InstructionDefType.unkown,null)
									});
										flag = true;
									}

								}
								else
								{
									//等下一轮
								}

							}
							break;
						case INS_Code.read_property:
							{
								INS_readPoperty readPoperty = (INS_readPoperty)instruction;
								//必须先确认instance,再查询虚函数表
								if (result.Any(r => r.Key.GetDef().Contains(readPoperty.instance)))
								{
									var v = result.FirstOrDefault(r => r.Key.GetDef().Contains(readPoperty.instance));
									var index = v.Key.GetDef().TakeWhile(k => !k.Equals(readPoperty.instance)).Count();
									var d = v.Value[index];

									switch (d.DefType)
									{
										case InstructionDefType.unkown:
											result.Add(instruction, new List<InstructionDef>() { new InstructionDef(InstructionDefType.unkown, null) });
											break;
										case InstructionDefType.primitive:
											{
												if (d.Obj is ASInstance)
												{
													result.Add(instruction, new List<InstructionDef>()
												{
													FromTypeKind(
													((ASContainer)d.Obj)._vtable.Items[ (int)readPoperty.const_index ].Trait.Method.ReturnTypeKind )
												});
													flag = true;
												}
												else if (d.Obj is NaNBoxing)
												{
													NaNBoxing val = (NaNBoxing)d.Obj;
													switch (val.ValueType)
													{
														case NaNBoxing.BoxType.Number:
															result.Add(instruction, new List<InstructionDef>()
														{
															FromTypeKind(
															allClasses.First(c => c.Type_identifier ==(ulong) TypeKind.Number).Instance._vtable.Items[(int) readPoperty.const_index].Trait.Method.ReturnTypeKind )
														});
															flag = true;
															break;
														case NaNBoxing.BoxType.Undefined:
															throw new InvalidOperationException();
															break;
														case NaNBoxing.BoxType.Null:
															throw new InvalidOperationException();
															break;
														case NaNBoxing.BoxType.Boolean:
															result.Add(instruction, new List<InstructionDef>()
														{
															FromTypeKind(
															(allClasses.First(c => c.Type_identifier ==(ulong) TypeKind.Boolean)).Instance._vtable.Items[(int) readPoperty.const_index].Trait.Method.ReturnTypeKind )
														});
															flag = true;
															break;
														case NaNBoxing.BoxType.Int:
															result.Add(instruction, new List<InstructionDef>()
														{
															FromTypeKind(
															(allClasses.First(c => c.Type_identifier ==(ulong) TypeKind.Int)).Instance._vtable.Items[(int) readPoperty.const_index].Trait.Method.ReturnTypeKind )
														});
															flag = true;
															break;
														case NaNBoxing.BoxType.Uint:
															result.Add(instruction, new List<InstructionDef>()
														{
															FromTypeKind(
															(allClasses.First(c => c.Type_identifier ==(ulong) TypeKind.Uint)).Instance._vtable.Items[(int) readPoperty.const_index].Trait.Method.ReturnTypeKind )
														});
															flag = true;
															break;
														case NaNBoxing.BoxType.Sbyte:
															result.Add(instruction, new List<InstructionDef>()
														{
															FromTypeKind(
															(allClasses.First(c => c.Type_identifier ==(ulong) TypeKind.SByte)).Instance._vtable.Items[(int) readPoperty.const_index].Trait.Method.ReturnTypeKind )
														});
															flag = true;
															break;
														case NaNBoxing.BoxType.Byte:
															result.Add(instruction, new List<InstructionDef>()
														{
															FromTypeKind(
															(allClasses.First(c => c.Type_identifier ==(ulong) TypeKind.Byte)).Instance._vtable.Items[(int) readPoperty.const_index].Trait.Method.ReturnTypeKind )
														});
															flag = true;
															break;
														case NaNBoxing.BoxType.Short:
															result.Add(instruction, new List<InstructionDef>()
														{
															FromTypeKind(
															(allClasses.First(c => c.Type_identifier ==(ulong) TypeKind.Short)).Instance._vtable.Items[(int) readPoperty.const_index].Trait.Method.ReturnTypeKind )
														});
															flag = true;
															break;
														case NaNBoxing.BoxType.UShort:
															result.Add(instruction, new List<InstructionDef>()
														{
															FromTypeKind(
															(allClasses.First(c => c.Type_identifier ==(ulong) TypeKind.UShort)).Instance._vtable.Items[(int) readPoperty.const_index].Trait.Method.ReturnTypeKind )
														});
															flag = true;
															break;
														case NaNBoxing.BoxType.Float:
															result.Add(instruction, new List<InstructionDef>()
														{
															FromTypeKind(
															(allClasses.First(c => c.Type_identifier ==(ulong) TypeKind.Float)).Instance._vtable.Items[(int) readPoperty.const_index].Trait.Method.ReturnTypeKind )
														});
															flag = true;
															break;
														case NaNBoxing.BoxType.HeapPtr:

															ASMethodBody.PoolHeapPtrKind kind = (ASMethodBody.PoolHeapPtrKind)(val.HeapPtr >> 24);
															Debug.Assert((kind == ASMethodBody.PoolHeapPtrKind.String));


															result.Add(instruction, new List<InstructionDef>()
														{
															FromTypeKind(
															(allClasses.First(c => c.Type_identifier ==(ulong) TypeKind.String)).Instance._vtable.Items[(int) readPoperty.const_index].Trait.Method.ReturnTypeKind )
														});
															flag = true;
															//throw new InvalidOperationException();
															break;
														case NaNBoxing.BoxType.LocalString:
															result.Add(instruction, new List<InstructionDef>()
														{
															FromTypeKind(
															(allClasses.First(c => c.Type_identifier ==(ulong) TypeKind.String)).Instance._vtable.Items[(int) readPoperty.const_index].Trait.Method.ReturnTypeKind )
														});
															flag = true;
															break;
														case NaNBoxing.BoxType.Fault:
															throw new InvalidOperationException();
															break;
														default:
															break;
													}

												}
												else
												{
													//无法确认
												}
											}

											break;
										case InstructionDefType.obj_maybeCacheable:
										case InstructionDefType.obj:
											result.Add(instruction, new List<InstructionDef>()
										{
											FromTypeKind(
											((ASContainer) d.Obj)._vtable.Items[(int) readPoperty.const_index].Trait.Method.ReturnTypeKind
											)
										});
											flag = true;

											break;
										case InstructionDefType.global:
											throw new InvalidOperationException();
											break;
										case InstructionDefType.Struct:

											result.Add(instruction, new List<InstructionDef>()
										{
											FromTypeKind(
											((ASContainer) d.Obj)._vtable.Items[(int) readPoperty.const_index].Trait.Method.ReturnTypeKind
											)
										});
											flag = true;

											break;
										case InstructionDefType.asclass:

											result.Add(instruction, new List<InstructionDef>()
										{

											FromTypeKind(
											((ASContainer) d.Obj)._vtable.Items[(int) readPoperty.const_index].Trait.Method.ReturnTypeKind

											)

										});
											flag = true;


											break;
										case InstructionDefType.method:
											result.Add(instruction, new List<InstructionDef>()
										{

											FromTypeKind( allClasses.First(c=>c.Type_identifier == (ulong)TypeKind.Function).Instance._vtable.Items[ (int)readPoperty.const_index ]
											.Trait.Method.ReturnTypeKind)

										});
											flag = true;
											break;
										case InstructionDefType.array:
											result.Add(instruction, new List<InstructionDef>()
										{

											FromTypeKind( allClasses.First(c=>c.Type_identifier == (ulong)TypeKind.Array).Instance._vtable.Items[ (int)readPoperty.const_index ]
											.Trait.Method.ReturnTypeKind)

										});
											flag = true;
											break;
										case InstructionDefType.vector:

											result.Add(instruction, new List<InstructionDef>()
										{

											FromTypeKind(
											((ASContainer) d.Obj)._vtable.Items[(int) readPoperty.const_index].Trait.Method.ReturnTypeKind

											)

										});
											flag = true;

											break;
										default:
											break;
									}


								}
								else
								{

								}

							}
							break;
						case INS_Code.read_property_interface:
							{
								INS_readPoperty_Interface readPoperty_Interface = (INS_readPoperty_Interface)instruction;


								var box = constants[readPoperty_Interface.class_id];
								Debug.Assert(method.Body.heapConstants.pool_kinds[box.HeapPtr] == ASMethodBody.PoolHeapPtrKind.LD_Class);
								ulong classid = (ulong)method.Body.heapConstants.pool_values[box.HeapPtr];

								//ASMethodBody.PoolHeapPtrKind kind = (ASMethodBody.PoolHeapPtrKind)(box.HeapPtr >> 24);
								//Debug.Assert(kind == ASMethodBody.PoolHeapPtrKind.LD_Class);

								//ulong classid = context.constpool_ldclass[box.HeapPtr & 0xFFFFFF];
								ASClass @class = allClasses.First(c => c.Type_identifier == classid);

								result.Add(instruction, new List<InstructionDef>()
							{
								FromTypeKind(@class.Instance._vtable.Items[(int) readPoperty_Interface.const_index ].Trait.Method.ReturnTypeKind  )
							});

								flag = true;

							}


							break;
						case INS_Code.write_property:
						case INS_Code.write_property_interface:
							{
								//写属性，不会定值
							}
							break;
						case INS_Code.goto_flag:

						case INS_Code.if_false_goto:

						case INS_Code.if_true_goto:

						case INS_Code.if_logicOp_goto:
							{
								//无定值
							}
							break;
						case INS_Code.store_MultiNameL:
							{
								//无定值
							}
							break;
						case INS_Code.store_MultiName:
							{ 
								//无定值
							}
							break;
						case INS_Code.store_instanceMember:
							{
								//无定值
							}
							break;
						case INS_Code.array_vector_initelement:
							{
								//无定值
							}
							break;

						case INS_Code.O_ld_function_bindGlobal:
							{
								INS_O_Ld_Function_BindGLobal ld_Function = (INS_O_Ld_Function_BindGLobal)instruction;
								var boxing = constants[ld_Function.const_index];
								Debug.Assert(method.Body.heapConstants.pool_kinds[boxing.HeapPtr] == ASMethodBody.PoolHeapPtrKind.Method);
								var m = (ASMethod)method.Body.heapConstants.pool_values[boxing.HeapPtr];

								//ASMethodBody.PoolHeapPtrKind kind = (ASMethodBody.PoolHeapPtrKind)(boxing.HeapPtr >> 24);
								//Debug.Assert((kind == ASMethodBody.PoolHeapPtrKind.Method));

								//int ptr = boxing.HeapPtr & 0xFFFFFF;
								//var methodscope = context.player_for_compiler.Context.GC.Heap[ptr];
								//Debug.Assert(methodscope.Kind == RtHeapTypeKind.MethodScope);

								//var m = ((ASMethodBody)methodscope.Type).Method;
								result.Add(instruction, new List<InstructionDef>() { new InstructionDef(InstructionDefType.method, m) });
								flag = true;

							}
							break;
						case INS_Code.O_ld_method:
							{
								//逻辑同 ld_method.
								INS_O_Ld_Method ld_O_Method = (INS_O_Ld_Method)instruction;

								if (result.Any(r => r.Key.GetDef().Contains(ld_O_Method.instance)))
								{
									var v = result.FirstOrDefault(r => r.Key.GetDef().Contains(ld_O_Method.instance));
									var index = v.Key.GetDef().TakeWhile(k => !k.Equals(ld_O_Method.instance)).Count();
									var d = v.Value[index];

									switch (d.DefType)
									{
										case InstructionDefType.unkown:
											result.Add(instruction, new List<InstructionDef>() { new InstructionDef(InstructionDefType.unkown, null) });
											flag = true;
											break;
										case InstructionDefType.primitive:
											//throw new NotImplementedException();
											{
												if (d.Obj is ASInstance)
												{
													result.Add(instruction, new List<InstructionDef>()
												{
													new InstructionDef(InstructionDefType.method,
													((ASContainer)d.Obj)._vtable.Items[ (int)ld_O_Method.const_index ].Trait.Method )
												});
													flag = true;
												}
												else if (d.Obj is NaNBoxing)
												{
													NaNBoxing val = (NaNBoxing)d.Obj;
													switch (val.ValueType)
													{
														case NaNBoxing.BoxType.Number:
															result.Add(instruction, new List<InstructionDef>()
														{
															new InstructionDef(InstructionDefType.method,
															(allClasses.First(c => c.Type_identifier ==(ulong) TypeKind.Number)).Instance._vtable.Items[(int) ld_O_Method.const_index].Trait.Method )
														});
															flag = true;
															break;
														case NaNBoxing.BoxType.Undefined:
															throw new InvalidOperationException();
															break;
														case NaNBoxing.BoxType.Null:
															throw new InvalidOperationException();
															break;
														case NaNBoxing.BoxType.Boolean:
															result.Add(instruction, new List<InstructionDef>()
														{
															new InstructionDef(InstructionDefType.method,
															(allClasses.First(c => c.Type_identifier ==(ulong) TypeKind.Boolean)).Instance._vtable.Items[(int) ld_O_Method.const_index].Trait.Method )
														});
															flag = true;
															break;
														case NaNBoxing.BoxType.Int:
															result.Add(instruction, new List<InstructionDef>()
														{
															new InstructionDef(InstructionDefType.method,
															(allClasses.First(c => c.Type_identifier ==(ulong) TypeKind.Int)).Instance._vtable.Items[(int) ld_O_Method.const_index].Trait.Method )
														});
															flag = true;
															break;
														case NaNBoxing.BoxType.Uint:
															result.Add(instruction, new List<InstructionDef>()
														{
															new InstructionDef(InstructionDefType.method,
															(allClasses.First(c => c.Type_identifier ==(ulong) TypeKind.Uint)).Instance._vtable.Items[(int) ld_O_Method.const_index].Trait.Method )
														});
															flag = true;
															break;
														case NaNBoxing.BoxType.Sbyte:
															result.Add(instruction, new List<InstructionDef>()
														{
															new InstructionDef(InstructionDefType.method,
															(allClasses.First(c => c.Type_identifier ==(ulong) TypeKind.SByte)).Instance._vtable.Items[(int) ld_O_Method.const_index].Trait.Method )
														});
															flag = true;
															break;
														case NaNBoxing.BoxType.Byte:
															result.Add(instruction, new List<InstructionDef>()
														{
															new InstructionDef(InstructionDefType.method,
															(allClasses.First(c => c.Type_identifier ==(ulong) TypeKind.Byte)).Instance._vtable.Items[(int) ld_O_Method.const_index].Trait.Method )
														});
															flag = true;
															break;
														case NaNBoxing.BoxType.Short:
															result.Add(instruction, new List<InstructionDef>()
														{
															new InstructionDef(InstructionDefType.method,
															(allClasses.First(c => c.Type_identifier ==(ulong) TypeKind.Short)).Instance._vtable.Items[(int) ld_O_Method.const_index].Trait.Method )
														});
															flag = true;
															break;
														case NaNBoxing.BoxType.UShort:
															result.Add(instruction, new List<InstructionDef>()
														{
															new InstructionDef(InstructionDefType.method,
															(allClasses.First(c => c.Type_identifier ==(ulong) TypeKind.UShort)).Instance._vtable.Items[(int) ld_O_Method.const_index].Trait.Method )
														});
															flag = true;
															break;
														case NaNBoxing.BoxType.Float:
															result.Add(instruction, new List<InstructionDef>()
														{
															new InstructionDef(InstructionDefType.method,
															(allClasses.First(c => c.Type_identifier ==(ulong) TypeKind.Float)).Instance._vtable.Items[(int) ld_O_Method.const_index].Trait.Method )
														});
															flag = true;
															break;
														case NaNBoxing.BoxType.HeapPtr:

															ASMethodBody.PoolHeapPtrKind kind = (ASMethodBody.PoolHeapPtrKind)(val.HeapPtr >> 24);
															Debug.Assert((kind == ASMethodBody.PoolHeapPtrKind.String));


															result.Add(instruction, new List<InstructionDef>()
														{
															new InstructionDef(InstructionDefType.method,
															(allClasses.First(c => c.Type_identifier ==(ulong) TypeKind.String)).Instance._vtable.Items[(int) ld_O_Method.const_index].Trait.Method )
														});
															flag = true;
															//throw new InvalidOperationException();
															break;
														case NaNBoxing.BoxType.LocalString:
															result.Add(instruction, new List<InstructionDef>()
														{
															new InstructionDef(InstructionDefType.method,
															(allClasses.First(c => c.Type_identifier ==(ulong) TypeKind.String)).Instance._vtable.Items[(int) ld_O_Method.const_index].Trait.Method )
														});
															flag = true;
															break;
														case NaNBoxing.BoxType.Fault:
															throw new InvalidOperationException();
															break;
														default:
															break;
													}

												}
												else
												{
													//无法确认
												}
											}
											break;
										case InstructionDefType.obj_maybeCacheable:
										case InstructionDefType.obj:
											result.Add(instruction, new List<InstructionDef>()
										{
											new InstructionDef(InstructionDefType.method,
											((ASContainer) d.Obj)._vtable.Items[(int) ld_O_Method.const_index].Trait.Method )
										});
											flag = true;
											break;
										case InstructionDefType.global:
											throw new InvalidOperationException();
											break;
										case InstructionDefType.Struct:
											result.Add(instruction, new List<InstructionDef>()
										{
											new InstructionDef(InstructionDefType.method,
											((ASContainer) d.Obj)._vtable.Items[(int) ld_O_Method.const_index].Trait.Method )
										});
											flag = true;
											break;
										case InstructionDefType.asclass:
											result.Add(instruction, new List<InstructionDef>()
										{
											new InstructionDef(InstructionDefType.method,
											((ASContainer) d.Obj)._vtable.Items[(int) ld_O_Method.const_index].Trait.Method )
										});
											flag = true;
											break;
										case InstructionDefType.method:
											result.Add(instruction, new List<InstructionDef>()
										{
											new InstructionDef(InstructionDefType.method,
											allClasses.First(c => c.Type_identifier ==(ulong) TypeKind.Function).Instance._vtable.Items[(int) ld_O_Method.const_index].Trait.Method )
										});
											flag = true;
											break;
										case InstructionDefType.array:

											result.Add(instruction, new List<InstructionDef>()
										{
											new InstructionDef(InstructionDefType.method,
											allClasses.First(c => c.Type_identifier ==(ulong) TypeKind.Array).Instance._vtable.Items[(int) ld_O_Method.const_index].Trait.Method )
										});
											flag = true;
											break;
										case InstructionDefType.vector:

											result.Add(instruction, new List<InstructionDef>()
										{
											new InstructionDef(InstructionDefType.method,
											((ASContainer) d.Obj)._vtable.Items[(int) ld_O_Method.const_index].Trait.Method )
										});
											flag = true;
											break;
										default:
											break;
									}


								}
								else
								{
									//等下一轮 
								}
							}
							break;
						case INS_Code.O_ld_interface_method:
							{
								INS_O_Ld_Method_Interface ld_O_Method_Interface = (INS_O_Ld_Method_Interface)instruction;
								var box = constants[ld_O_Method_Interface.class_id];

								Debug.Assert(method.Body.heapConstants.pool_kinds[box.HeapPtr] == ASMethodBody.PoolHeapPtrKind.LD_Class);
								ulong classid = (ulong)method.Body.heapConstants.pool_values[box.HeapPtr];


								//ASMethodBody.PoolHeapPtrKind kind = (ASMethodBody.PoolHeapPtrKind)(box.HeapPtr >> 24);
								//Debug.Assert(kind == ASMethodBody.PoolHeapPtrKind.LD_Class);

								//ulong classid = context.constpool_ldclass[box.HeapPtr & 0xFFFFFF];
								ASClass @class = allClasses.First(c => c.Type_identifier == classid);

								result.Add(instruction, new List<InstructionDef>()
							{
								new InstructionDef(InstructionDefType.method, @class.Instance._vtable.Items[(int) ld_O_Method_Interface.const_index ].Trait.Method  )
							});

								flag = true;
							}
							break;
						case INS_Code.O_Call:
							{
								INS_O_Call o_Call = (INS_O_Call)instruction;
								if (result.Any((r => r.Key.GetDef().Contains(o_Call.function))))
								{
									var kv = result.First(r => r.Key.GetDef().Contains(o_Call.function));
									int index = kv.Key.GetDef().TakeWhile(k => !k.Equals(o_Call.function)).Count();
									var typeDef = kv.Value[index];

									if (typeDef.DefType == InstructionDefType.method && typeDef.Obj != null)
									{
										result.Add(instruction, new List<InstructionDef>() { FromTypeKind(((ASMethod)typeDef.Obj).ReturnTypeKind) });
										flag = true;
									}
									else
									{
										result.Add(instruction, new List<InstructionDef>() {
										new InstructionDef(InstructionDefType.unkown,null)
									});
										flag = true;
									}

								}
								else
								{
									//等下一轮
								}

							}
							break;
						case INS_Code.O_NewStruct:
							{
								INS_O_NewStruct new_struct = (INS_O_NewStruct)instruction;
								if (result.Any((r => r.Key.GetDef().Contains(new_struct.typeLocator))))
								{
									var kv = result.First(r => r.Key.GetDef().Contains(new_struct.typeLocator));
									int index = kv.Key.GetDef().TakeWhile(k => !k.Equals(new_struct.typeLocator)).Count();
									var typeDef = kv.Value[index];

									if (typeDef.DefType == InstructionDefType.asclass)
									{
										if (typeDef.Obj != null)
										{
											var dd = FromTypeKind((TypeKind)((ASClass)typeDef.Obj).Type_identifier);

											result.Add(instruction, new List<InstructionDef>() {
											dd
										});
											flag = true;
										}
										else
										{   //Class本身
											result.Add(instruction, new List<InstructionDef>() {
											new InstructionDef(InstructionDefType.unkown,null)
										});
											flag = true;
										}
									}
									else
									{
										result.Add(instruction, new List<InstructionDef>() {
										new InstructionDef(InstructionDefType.unkown,null)
									});
										flag = true;
									}
								}
								else
								{

								}

							}
							break;
						case INS_Code.O_NewInstance_MethodVar:
							{
								{
									INS_O_NewInstance_StoreVar new_Instance_storevar = (INS_O_NewInstance_StoreVar)instruction;
									if (result.Any((r => r.Key.GetDef().Contains(new_Instance_storevar.typeLocator))))
									{
										var kv = result.First(r => r.Key.GetDef().Contains(new_Instance_storevar.typeLocator));
										int index = kv.Key.GetDef().TakeWhile(k => !k.Equals(new_Instance_storevar.typeLocator)).Count();
										var typeDef = kv.Value[index];

										Debug.Assert( typeDef.DefType == InstructionDefType.asclass && typeDef.Obj is ASClass );
										//if (typeDef.DefType == InstructionDefType.asclass)
										{
											//if (typeDef.Obj != null)
											{
												var dd = FromTypeKind((TypeKind)((ASClass)typeDef.Obj).Type_identifier);

												result.Add(instruction, new List<InstructionDef>() {
													dd
												});
												flag = true;
											}
											
											
										}
										
									}
									else
									{

									}

								}
							}
							break;
						case INS_Code.O_Ld_InstanceFiled:
							{
								INS_O_Ld_InstanceFiled ld_instancefield = (INS_O_Ld_InstanceFiled)instruction;
								
								{
									//if (stack_classes.ContainsKey(ld_instancefield.instance.index))
									//{
									//	ASClass cls = stack_classes[ld_instancefield.instance.index];
									//	var member = cls._link_codescope.Members[(int)ld_instancefield.scopemember_index];
									//	result.Add(instruction, new List<InstructionDef>() { FromTypeKind(member.TypeKind) });

									//	flag = true;
									//}
									//else
									{

										if (result.Any(r => r.Key.GetDef().Contains(ld_instancefield.instance)))
										{
											var v = result.FirstOrDefault(r => r.Key.GetDef().Contains(ld_instancefield.instance)).Value;
											if (v.Count == 1 && v[0].Obj != null) //count==1,防止出现多个def的情况 多def还有可能多个mv到同一个目标，SSA后可能出现
											{
												Debug.Assert(v[0].Obj is ASInstance);

												if (ld_instancefield.mask == NaNBoxing.NULL >> 32)
												{
													var member = ((ASInstance)v[0].Obj)._link_codescope.Members[(int)ld_instancefield.scopemember_index];
													result.Add(instruction, new List<InstructionDef>() { FromTypeKind(member.TypeKind) });
												}
												else if (ld_instancefield.mask == NaNBoxing.FALSE >> 32)
												{
													var member = ((ASInstance)v[0].Obj)._link_codescope.Members[(int)ld_instancefield.scopemember_index];
													result.Add(instruction, new List<InstructionDef>() { FromTypeKind(TypeKind.Boolean) });
												}
												else if (ld_instancefield.mask == NaNBoxing.UNDEFINED >> 32)
												{
													var member = ((ASInstance)v[0].Obj)._link_codescope.Members[(int)ld_instancefield.scopemember_index];
													result.Add(instruction, new List<InstructionDef>() { FromTypeKind(TypeKind.Any) });
												}
												else if (ld_instancefield.mask == 0)
												{
													var member = ((ASInstance)v[0].Obj)._link_codescope.Members[(int)ld_instancefield.scopemember_index];
													result.Add(instruction, new List<InstructionDef>() { FromTypeKind(TypeKind.Number) });
												}
												else
												{ 
													NaNBoxing vv = new NaNBoxing( (ulong)ld_instancefield.mask <<32 );

													switch (vv.ValueType)
													{
														
														case NaNBoxing.BoxType.Int:
															result.Add(instruction, new List<InstructionDef>() { FromTypeKind(TypeKind.Int) });
															break;
														case NaNBoxing.BoxType.Uint:
															result.Add(instruction, new List<InstructionDef>() { FromTypeKind(TypeKind.Uint) });
															break;
														case NaNBoxing.BoxType.Sbyte:
															result.Add(instruction, new List<InstructionDef>() { FromTypeKind(TypeKind.SByte) });
															break;
														case NaNBoxing.BoxType.Byte:
															result.Add(instruction, new List<InstructionDef>() { FromTypeKind(TypeKind.Byte) });
															break;
														case NaNBoxing.BoxType.Short:
															result.Add(instruction, new List<InstructionDef>() { FromTypeKind(TypeKind.Short) });
															break;
														case NaNBoxing.BoxType.UShort:
															result.Add(instruction, new List<InstructionDef>() { FromTypeKind(TypeKind.UShort) });
															break;
														case NaNBoxing.BoxType.Float:
															result.Add(instruction, new List<InstructionDef>() { FromTypeKind(TypeKind.Float) });
															break;
														case NaNBoxing.BoxType.Number:
														case NaNBoxing.BoxType.Undefined:
														case NaNBoxing.BoxType.Null:
														case NaNBoxing.BoxType.Boolean:
														case NaNBoxing.BoxType.HeapPtr:
														case NaNBoxing.BoxType.LocalString:
														case NaNBoxing.BoxType.Fault:
														default:
															throw new InvalidOperationException();
															break;
													}

												}


												flag = true;
											}
											else
											{
												result.Add(instruction, new List<InstructionDef>() { new InstructionDef(InstructionDefType.unkown, null) });
												flag = true;
											}
										}
										else
										{
											//等下一轮
											//throw new NotImplementedException();
										}
									}
								}


								flag = true;
							}
							break;
						case INS_Code.O_StoreMethodVar_Instance:
							{
								INS_O_StoreMethodVar_Instance store_MethodVariable = (INS_O_StoreMethodVar_Instance)instruction;
								//method.Body._link_codescope.Members[store_MethodVariable.heap.MemberIndex];
								//

								if (method.Body._link_codescope.Members[store_MethodVariable.heap.MemberIndex].TypeKind == TypeKind.Any)
								{
									if (result.Any(r => r.Key.GetDef().Contains(store_MethodVariable.dst)))
									{
										var kv = result.First(r => r.Key.GetDef().Contains(store_MethodVariable.dst));
										int index = kv.Key.GetDef().TakeWhile(k => !k.Equals(store_MethodVariable.dst)).Count();

										var d = kv.Value[index];

										result.Add(instruction, new List<InstructionDef>() {
											 new InstructionDef( d.DefType,d.Obj)
											});

										flag = true;

									}
									else
									{
										//等下一轮
										//throw new NotImplementedException();
									}
								}
								else
								{
									
									result.Add(instruction, new List<InstructionDef>() {
											FromTypeKind( method.Body._link_codescope.Members[ store_MethodVariable.heap.MemberIndex ].TypeKind )

										});
									flag = true;
								}


							}
							break;

						case INS_Code.O_Ld_Array_Element:
							{
								result.Add(instruction, new List<InstructionDef>() {
											 FromTypeKind(TypeKind.Any)
											});
							}
							break;
						case INS_Code.O_Store_Array_Element:
							break;
						case INS_Code.iter_initctx:

						case INS_Code.iter_get:

						case INS_Code.iter_close:

						case INS_Code.iter_next:
							{
								//特殊，跳过
							}
							break;
						case INS_Code.await_return:
							{
								//特殊，跳过
							}
							break;
						case INS_Code.await_resume:
							{
								//特殊，跳过
							}
							break;
						case INS_Code.yield_return:
							{
								//特殊，跳过
							}
							break;
						case INS_Code.yield_break:
							{
								//特殊，跳过
							}
							break;
						case INS_Code.return_void:
							{
								//特殊，跳过
							}
							break;
						case INS_Code.return_value:
							{
								//特殊，跳过
							}
							break;
						case INS_Code.throw_error:
							{
								//无定值
							}
							break;
						case INS_Code.try_enter:
							{
								//无定值
							}
							break;
						case INS_Code.try_exit:
							{
								//无定值
							}
							break;
						case INS_Code.catch_enter:
							{
								//无定值
							}
							break;
						case INS_Code.catch_exit:
							{
								//无定值
							}
							break;
						case INS_Code.finally_enter:
							{
								//无定值
							}
							break;
						case INS_Code.finally_exit:
							{
								//无定值
							}
							break;
						case INS_Code.expression_barrier:
							{
								//无定值
							}
							break;
						case INS_Code.END:
							{
								//无定值
							}
							break;
						default:
							break;
					}



				}

			} while (flag);


			return result;
		}






		/// <summary>
		/// 查找某个槽在哪些地方被定义
		/// 返回定义它的指令，已经那个指令的GetDef()列表中的序号
		/// </summary>
		/// <param name="target"></param>
		/// <param name="cfg"></param>
		/// <returns></returns>
		private static List<Tuple<Instruction, int>> FindStackSlotDefAt(StackLocater target, ControlFlowGraph cfg )
		{
			Stack<StackLocater> source = new Stack<StackLocater>();
			source.Push(target);

			HashSet<int> searched = new HashSet<int>();
			List<Tuple<Instruction, int>> defsourcelist = new List<Tuple<Instruction, int>>();

			while (source.Count > 0)
			{
				var test = source.Pop();
				if (searched.Contains(test.index))
					continue;

				searched.Add(test.index);

				var deflist = cfg.Blocks.SelectMany(b => b.Instructions).Where(i => i.GetDef().Any(d => d.index == test.index)).ToList();
				defsourcelist.AddRange(deflist.Where(i => i.INS_Code != INS_Code.move).Select(i => new Tuple<Instruction, int>(i, i.GetDef().TakeWhile(k => !k.Equals(test)).Count())));

				foreach (var def in deflist.Where(i => i.INS_Code == INS_Code.move))
				{
					source.Push(((INS_Move)def).source);
				}
			}

			return defsourcelist;

		}




	}
}
