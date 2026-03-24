using juicescript.ABC;
using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace juicescript.compiler
{
	public class LayoutResolver
	{
		private static void CheckStructTag(CompileContext context, List<ASClass> structs)
		{
			for (int i = 0; i < context.scriptDefs.Count; i++)
			{
				var script = context.scriptDefs[i];
				for (int j = 0; j < script.containers.Count; j++)
				{
					var container = script.containers[j];

					for (int k = 0; k < container.Traits.Count; k++)
					{
						var t = container.Traits[k];

						if (t.Kind == TraitKind.Class)
						{
							ASClass cls = t.Class;
							if (!cls.Instance.IsInterface)
							{
								if (t.ASMetadata.Any(m => m.Name == "struct" && m.Items.Count > 0))
								{
									throw new ResolverException(cls.Token, "[struct] has too many parameters.");
								}
								else if (t.ASMetadata.Count(m => m.Name == "struct" && m.Items.Count == 0) > 1)
								{
									throw new ResolverException(cls.Token, "[struct] only can be defined once.");
								}
								else if (t.ASMetadata.Count(m => m.Name == "struct" && m.Items.Count == 0) == 1)
								{
									var instance = cls.Instance;
									if (!instance.Flags.HasFlag(ClassFlags.Final))
									{
										throw new ResolverException(cls.Token, "struct must is final.");
									}
									if (!instance.Flags.HasFlag(ClassFlags.Sealed))
									{
										throw new ResolverException(cls.Token, "struct cann't dynamic.");
									}
									if (!(instance.Super.Name == "Object" && string.IsNullOrEmpty(instance.Super.Namespace.Name)))
									{
										throw new ResolverException(cls.Token, "struct cann't extends other class.");
									}

									structs.Add(cls);

								}
							}
							else
							{
								if (t.ASMetadata.Any(m => m.Name == "struct"))
								{
									throw new ResolverException(cls.Token, "[struct] cann't be defined on interface.");
								}

							}

						}
					}
				}
			}

		}

		internal static int BuildLayout(CompileContext context)
		{
			Dictionary<ASClass, TypeLayout> class_layout = new Dictionary<ASClass, TypeLayout>();

			for (int i = 0; i < context.classDependSort.Count; i++)
			{
				var cls = context.classDependSort[i];

				//填充各自实现的接口。
				for (int j = 0; j < cls.Instance.Interfaces.Count; j++)
				{
					var inf = cls.Instance.Interfaces[j];
					ASClass icls = context.dict_super_interfaces[inf];

					cls.Instance._implements_.Add(icls);
				}

			}

			//检查no_constructor
			for (int i = 0; i < context.scriptDefs.Count; i++)
			{
				var script = context.scriptDefs[i];
				for (int j = 0; j < script.containers.Count; j++)
				{
					var container = script.containers[j];

					for (int k = 0; k < container.Traits.Count; k++)
					{
						var t = container.Traits[k];

						if (t.Kind == TraitKind.Class)
						{
							ASClass cls = t.Class;
							if (!cls.Instance.IsInterface)
							{
								if (t.ASMetadata.Any(m => m.Name == "no_constructor" && m.Items.Count > 0))
								{
									throw new ResolverException(cls.Token, "[no_constructor] has too many parameters.");
								}
								else if (t.ASMetadata.Count(m => m.Name == "no_constructor" && m.Items.Count == 0) > 1)
								{
									throw new ResolverException(cls.Token, "[no_constructor] only can be defined once.");
								}
								else if (t.ASMetadata.Count(m => m.Name == "no_constructor" && m.Items.Count == 0) == 1)
								{
									var instance = cls.Instance;
									instance.Flags |= ClassFlags.NoConstructor;
								}
							}
							else
							{
								if (t.ASMetadata.Any(m => m.Name == "no_constructor"))
								{
									throw new ResolverException(cls.Token, "[no_constructor] cann't be defined on interface.");
								}

							}
						}
					}
				}
			}


			List<ASClass> structs = new List<ASClass>();
			CheckStructTag( context ,structs);

			//先计算非Struct的布局
			for (int i = 0; i < context.classDependSort.Count; i++)
			{
				var cls = context.classDependSort[i];
				if (!structs.Contains(cls))
				{

					if (!cls.Instance.IsInterface)
					{
						TypeLayout typeLayout = new TypeLayout();
						typeLayout.ASType = cls;

						typeLayout.Size = 0;

						int max_instance_slot_align = 0;

						//先拷贝基类的布局
						if (cls.Instance.Super != null)
						{
							TypeLayout superlayout;

							var super = context.dict_super_interfaces[cls.Instance.Super];

							if (class_layout.ContainsKey(super))
							{
								superlayout = class_layout[super];
							}
							else
							{
								//基类肯定在已加载的lib里。
								superlayout = context.player_for_compiler.Context.dictTypeLayouts[cls.Instance.Super];
							}

							cls.Instance._super_class_ = super;

							if (superlayout.Size > 0)
							{
								typeLayout.Offset.AddRange(superlayout.Offset);
								typeLayout.SlotSize.AddRange(superlayout.SlotSize);
								typeLayout.SlotAlign.AddRange(superlayout.SlotAlign);

								typeLayout.Size = superlayout.Offset.Last() + superlayout.SlotSize.Last();
								max_instance_slot_align = superlayout.SlotAlign.Max();
							}

						}

						var instance = cls.Instance;
						for (int j = 0; j < instance.Traits.Count; j++)
						{
							var t = instance.Traits[j];

							if (t.Kind == TraitKind.Constant || t.Kind == TraitKind.Slot)
							{
								int align, size;
								Player.ComputeTypeSize_Align(t, out align, out size);
								max_instance_slot_align = Math.Max(max_instance_slot_align, align);

								int baseOffset = typeLayout.Size;
								if (baseOffset % align != 0)
								{
									baseOffset += (align - baseOffset % align);
								}
								typeLayout.Size = baseOffset + size;
								typeLayout.Offset.Add(baseOffset);
								typeLayout.SlotSize.Add(size);
								typeLayout.SlotAlign.Add(align);
							}

						}
						//计算整体对齐值+padding
						if (max_instance_slot_align > 0 && typeLayout.Size % max_instance_slot_align != 0)
						{
							typeLayout.Size += (max_instance_slot_align - typeLayout.Size % max_instance_slot_align);
						}

						class_layout.Add(cls, typeLayout);


						context.dict_typelayout.Add(cls.QName, typeLayout);
					}
					else
					{
						TypeLayout typeLayout = new TypeLayout();
						typeLayout.ASType = cls;

						typeLayout.Size = 0;

						context.dict_typelayout.Add(cls.QName, typeLayout);

					}

				}
			}


			//结构体基类固定为Object.
			//检查是否有非结构体或基本类型的成员
			//结构体的定义
			//结构体必须是Final.结构体不能继承自其他对象。结构体成员类型只能包含不分配在堆上的基础类型(bool,Number,int,...)
			//结构体被赋值给变量或者对象成员时，会自动复制一份全新的。
			for (int i = 0; i < structs.Count; i++)
			{
				var cls = structs[i];
				cls.Instance._super_class_ = context.dict_super_interfaces[cls.Instance.Super];

				var instance = structs[i].Instance;
				for (int l = 0; l < instance.Traits.Count; l++)
				{
					var itrait = instance.Traits[l];
					if (itrait.Kind == TraitKind.Constant || itrait.Kind == TraitKind.Slot)
					{
						if (itrait.TypeKind >= TypeKind.Object)
						{
							if (context.classDependSort.Any(c => c.Type_identifier == (ulong)itrait.TypeKind))
							{
								var mtype = context.classDependSort.Find(c => c.Type_identifier == (ulong)itrait.TypeKind);
								if (!structs.Contains(mtype))
								{
									throw new ResolverException(itrait.Token, "struct only use primitive type.");
								}
							}
							else
							{
								var mtype = context.player_for_compiler.Context.dictTypeQNames[itrait.Type];
								if (!mtype.Instance.Flags.HasFlag(ClassFlags.Struct))
								{
									throw new ResolverException(itrait.Token, "struct only use primitive type.");
								}
							}
						}
						else if (itrait.TypeKind == TypeKind.Any)
						{
							throw new ResolverException(itrait.Token, "struct only use primitive type.");
						}
					}
				}
			}

			Dictionary<ASClass, List<ASClass>> struct_compute_sort = new Dictionary<ASClass, List<ASClass>>();
			//检查布局循环
			for (int i = 0; i < structs.Count; i++)
			{
				var cls = structs[i];
				List<ASClass> visited = new List<ASClass>();

				Stack<ASClass> stack = new Stack<ASClass>();
				stack.Push(cls);

				while (stack.Count>0)
				{
					var c = stack.Pop();
					visited.Add(c);

					var instance = c.Instance;
					for (int l = 0; l < instance.Traits.Count; l++)
					{
						var itrait = instance.Traits[l];
						if (itrait.Kind == TraitKind.Constant || itrait.Kind == TraitKind.Slot)
						{
							if (itrait.TypeKind >= TypeKind.Object)
							{
								ASClass member;
								if (context.classDependSort.Any(c => c.Type_identifier == (ulong)itrait.TypeKind))
								{
									member = context.classDependSort.Find(c => c.Type_identifier == (ulong)itrait.TypeKind);

									if (visited.Contains(member))
									{
										throw new ResolverException(itrait.Token, $"{itrait.QName.Name} cyclic layout.");
									}

									stack.Push(member);

								}
								else
								{
									member = context.player_for_compiler.Context.dictTypeQNames[itrait.Type];
									//已经计算，所以不需要再次深入
								}
								
							}
						}
					}
				}

				visited.Reverse();
				struct_compute_sort.Add(cls, visited);
			}

			//计算结构体的布局
			for (int i = 0; i < structs.Count; i++)
			{		
				var computeSort = struct_compute_sort[structs[i]];

				for (int z = 0; z < computeSort.Count; z++)
				{
					var cls = computeSort[z];

					if (class_layout.ContainsKey(cls))
					{
						continue;
					}


					TypeLayout typeLayout = new TypeLayout();
					typeLayout.ASType = cls;

					typeLayout.Size = 0;
					int max_instance_slot_align = 0;

					var instance = cls.Instance;
					for (int j = 0; j < instance.Traits.Count; j++)
					{
						var t = instance.Traits[j];

						if (t.Kind == TraitKind.Constant || t.Kind == TraitKind.Slot)
						{
							int align, size;

							if (t.TypeKind > TypeKind.Object)
							{
								if (structs.Exists(s => s.Type_identifier == (ulong)t.TypeKind))
								{
									var l = class_layout.First(k => k.Key.Type_identifier == (ulong)t.TypeKind).Value;
									align = l.SlotAlign.Max();
									size = l.Size;
								}
								else
								{
									var l =context.player_for_compiler.Context.dictTypeLayouts[t.Type];
									align = l.SlotAlign.Max();
									size = l.Size;
								}

								max_instance_slot_align = Math.Max(max_instance_slot_align, align);
							}
							else
							{
								Player.ComputeTypeSize_Align(t, out align, out size);
								max_instance_slot_align = Math.Max(max_instance_slot_align, align);
							}

							int baseOffset = typeLayout.Size;
							if (baseOffset % align != 0)
							{
								baseOffset += (align - baseOffset % align);
							}
							typeLayout.Size = baseOffset + size;
							typeLayout.Offset.Add(baseOffset);
							typeLayout.SlotSize.Add(size);
							typeLayout.SlotAlign.Add(align);
						}

					}
					//计算整体对齐值+padding
					if (max_instance_slot_align > 0 && typeLayout.Size % max_instance_slot_align != 0)
					{
						typeLayout.Size += (max_instance_slot_align - typeLayout.Size % max_instance_slot_align);
					}

					if (typeLayout.Size > RtPayloadInstance.MAX_CACHEABLE_SIZE)
					{
						throw new ResolverException(cls.Token,$"struct {cls.QName} is too large");
					}


					class_layout.Add(cls, typeLayout);
					context.dict_typelayout.Add(cls.QName, typeLayout);


					instance.Flags |= ClassFlags.Struct;

				}

			}



			
			//for (int i = 0; i < structs.Count; i++)
			//{
			//	var instance = structs[i].Instance;
			//	for (int l = 0; l < instance.Traits.Count; l++)
			//	{
			//		var itrait = instance.Traits[l];
			//		if (itrait.Kind == TraitKind.Constant || itrait.Kind == TraitKind.Slot)
			//		{
			//			switch (itrait.TypeKind)
			//			{
			//				case TypeKind.Boolean:
			//					break;
			//				case TypeKind.SByte:
			//					break;
			//				case TypeKind.Byte:
			//					break;
			//				case TypeKind.Short:
			//					break;
			//				case TypeKind.UShort:
			//					break;
			//				case TypeKind.Int:
			//					break;
			//				case TypeKind.Uint:
			//					break;
			//				case TypeKind.Float:
			//					break;
			//				case TypeKind.Number:
			//					break;
			//				case TypeKind.String:
			//					break;
			//				case TypeKind.Any:
			//				case TypeKind.Fun_Void:
			//				case TypeKind.TraitDataReference:
			//				case TypeKind.RTQName_MultiName_DataReference:
			//				case TypeKind.CParseNS_Traits:
			//				case TypeKind.RTQNameRTQNameL_N:
			//				case TypeKind.SearchNameSpaceFromImports:
			//				case TypeKind.Unknown:
			//				case TypeKind.Null:
			//				case TypeKind.Object:
			//				case TypeKind.Class:
			//				case TypeKind.Super:
			//				case TypeKind.Function:
			//				case TypeKind.Array:
			//				case TypeKind.Vector:
			//				case TypeKind.Namespace:
			//				default:
			//					throw new ResolverException(itrait.Token, "struct only use primitive type.");
			//			}
			//		}

			//	}


			//	instance.Flags |= ClassFlags.Struct;

			//}




			//检查wapper对象的定义
			//wapper对象的成员只能是native的。
			//wapper对象不能继承自非wapper对象。
			{
				for (int i = 0; i < context.scriptDefs.Count; i++)
				{
					var script = context.scriptDefs[i];
					for (int j = 0; j < script.containers.Count; j++)
					{
						var container = script.containers[j];
						for (int k = 0; k < container.Traits.Count; k++)
						{
							var t = container.Traits[k];
							if (t.Kind == TraitKind.Class)
							{
								ASClass cls = t.Class;
								if (!cls.Instance.IsInterface)
								{
									if (t.ASMetadata.Any(m => m.Name == "wapper" && m.Items.Count > 0))
									{
										throw new ResolverException(cls.Token, "[wapper] has too many parameters.");
									}
									else if (t.ASMetadata.Count(m => m.Name == "wapper" && m.Items.Count == 0) > 1)
									{
										throw new ResolverException(cls.Token, "[wapper] only can be defined once.");
									}
									else if (t.ASMetadata.Count(m => m.Name == "wapper" && m.Items.Count == 0) == 1)
									{
										var instance = cls.Instance;
										for (int l = 0; l < instance.Traits.Count; l++)
										{
											var itrait = instance.Traits[l];
											if (itrait.Kind == TraitKind.Method || itrait.Kind == TraitKind.Getter || itrait.Kind == TraitKind.Setter)
											{
												if (!itrait.Method.Flags.HasFlag(MethodFlags.Native))
												{
													throw new ResolverException(cls.Token, "[wapper] only contains native method.");
												}
											}
											else
											{
												throw new ResolverException(cls.Token, "[wapper] only contains native method.");
											}
										}

										instance.Flags |= ClassFlags.Wapper;
									}
								}
								else
								{
									if (t.ASMetadata.Any(m => m.Name == "wapper"))
									{
										throw new ResolverException(cls.Token, "[wapper] cann't be defined on interface.");
									}

								}
							}


						}
					}
				}

				//检查继承关系
				for (int i = 0; i < context.scriptDefs.Count; i++)
				{
					var script = context.scriptDefs[i];
					for (int j = 0; j < script.containers.Count; j++)
					{
						var container = script.containers[j];
						for (int k = 0; k < container.Traits.Count; k++)
						{
							var t = container.Traits[k];
							if (t.Kind == TraitKind.Class)
							{
								ASClass cls = t.Class;
								if (cls.Instance.Flags.HasFlag(ClassFlags.Wapper))
								{
									if (!(cls.Instance._super_class_.QName.Name == "Object" && string.IsNullOrEmpty(cls.Instance._super_class_.QName.Namespace.Name)))
									{
										if (!cls.Instance.Flags.HasFlag(ClassFlags.Wapper))
										{
											throw new ResolverException(cls.Token, "wapper type cann't extends other that not wapper type.");
										}
									}
								}
							}
						}
					}
				}

			}


			//检查是否有索引器代码
			for (int i = 0; i < context.scriptDefs.Count; i++)
			{
				var script = context.scriptDefs[i];
				for (int j = 0; j < script.containers.Count; j++)
				{
					var container = script.containers[j];
					for (int k = 0; k < container.Traits.Count; k++)
					{
						var t = container.Traits[k];
						if (t.Kind == TraitKind.Class)
						{
							ASClass cls = t.Class;
							if (!cls.Instance.IsInterface)
							{
								var instance = cls.Instance;
								for (int l = 0; l < instance.Traits.Count; l++)
								{
									var itrait = instance.Traits[l];
									if (itrait.Kind == TraitKind.Method )
									{
										if (itrait.ASMetadata.Any(m => m.Name == "indexer_set" && m.Items.Count > 0))
										{
											throw new ResolverException(itrait.Token, "[indexer_set] has too many parameters.");
										}
										else if (itrait.ASMetadata.Count(m => m.Name == "indexer_set" && m.Items.Count == 0) > 1)
										{
											throw new ResolverException(itrait.Token, "[indexer_set] only can be defined once.");
										}

										if (itrait.ASMetadata.Any(m => m.Name == "indexer_get" && m.Items.Count > 0))
										{
											throw new ResolverException(itrait.Token, "[indexer_get] has too many parameters.");
										}
										else if (itrait.ASMetadata.Count(m => m.Name == "indexer_get" && m.Items.Count == 0) > 1)
										{
											throw new ResolverException(itrait.Token, "[indexer_get] only can be defined once.");
										}

										if (itrait.ASMetadata.Any(m => m.Name == "indexer_delete" && m.Items.Count > 0))
										{
											throw new ResolverException(itrait.Token, "[indexer_delete] has too many parameters.");
										}
										else if (itrait.ASMetadata.Count(m => m.Name == "indexer_delete" && m.Items.Count == 0) > 1)
										{
											throw new ResolverException(itrait.Token, "[indexer_delete] only can be defined once.");
										}

									}
									
								}

								var index_get = instance.Traits.Where(t => t.Kind == TraitKind.Method && t.ASMetadata.Count(m => m.Name == "indexer_get" && m.Items.Count == 0) == 1).ToArray();
								var index_set = instance.Traits.Where(t => t.Kind == TraitKind.Method && t.ASMetadata.Count(m => m.Name == "indexer_set" && m.Items.Count == 0) == 1).ToArray();
								var index_delete = instance.Traits.Where(t => t.Kind == TraitKind.Method && t.ASMetadata.Count(m => m.Name == "indexer_delete" && m.Items.Count == 0) == 1).ToArray();

								if (index_get.Length > 1)
								{
									throw new ResolverException(cls.Token, "[indexer_get] only can be defined once.");
								}
								if (index_set.Length > 1)
								{
									throw new ResolverException(cls.Token, "[indexer_set] only can be defined once.");
								}
								if (index_delete.Length > 1)
								{
									throw new ResolverException(cls.Token, "[index_delete] only can be defined once.");
								}


								if (index_get.Length + index_set.Length + index_delete.Length > 0)
								{
									if (index_get.Length != 1 )
									{
										throw new ResolverException(cls.Token, "miss [indexer_get].");
									}
									if (index_set.Length != 1)
									{
										throw new ResolverException(cls.Token, "miss [indexer_set].");
									}
									if (index_delete.Length != 1)
									{
										throw new ResolverException(cls.Token, "miss [index_delete].");
									}

									if (index_get[0].Method.ReturnTypeKind == TypeKind.Fun_Void)
									{
										throw new ResolverException(cls.Token, "[indexer_get] must return a value.");
									}

									if (index_get[0].Method.Parameters.Count != 1)
									{
										throw new ResolverException(cls.Token, "[indexer_get] has one parameter ,and not (...rest) .");
									}

									if (index_set[0].Method.ReturnTypeKind != TypeKind.Fun_Void)
									{
										throw new ResolverException(cls.Token, "[index_set] must void.");
									}

									if (index_set[0].Method.Parameters.Count != 2)
									{
										throw new ResolverException(cls.Token, "[indexer_get] has two parameters ,and not (key,...rest) .");
									}

									if (index_delete[0].Method.ReturnTypeKind != TypeKind.Boolean)
									{
										throw new ResolverException(cls.Token, "[index_delete] must return a Boolean.");
									}
									if (index_delete[0].Method.Parameters.Count != 1)
									{
										throw new ResolverException(cls.Token, "[index_delete] has one parameter ,and not (...rest) .");
									}


									if (index_get[0].Method.Parameters[0].IsRest)
									{
										throw new ResolverException(cls.Token, "[indexer_get] has one parameter ,and not (...rest) .");
									}

									if (index_set[0].Method.Parameters[1].IsRest)
									{
										throw new ResolverException(cls.Token, "[indexer_get] has two parameters ,and not (key,...rest) .");
									}

									if (index_delete[0].Method.Parameters[0].IsRest)
									{
										throw new ResolverException(cls.Token, "[index_delete] has two parameters ,and not (key,...rest) .");
									}

									instance.Flags |= ClassFlags.Indexer;
								}

							}
							
						}


					}
				}
			}


			//如果某个类是dynamic,并且继承了某个有索引器的基类，则会继承索引器。
			for (int i = 0; i < context.scriptDefs.Count; i++)
			{
				var script = context.scriptDefs[i];
				for (int j = 0; j < script.containers.Count; j++)
				{
					var container = script.containers[j];
					for (int k = 0; k < container.Traits.Count; k++)
					{
						var t = container.Traits[k];
						if (t.Kind == TraitKind.Class)
						{
							ASClass cls = t.Class;
							if (!cls.Instance.IsInterface)
							{
								var instance = cls.Instance;
								if (!instance.Flags.HasFlag(ClassFlags.Sealed) && !instance.Flags.HasFlag(ClassFlags.Indexer))
								{
									var super = instance._super_class_;
									while (super != null)
									{
										if (super.Instance.Flags.HasFlag(ClassFlags.Indexer))
										{
											instance.Flags |= ClassFlags.Indexer;
											break;
										}
										super = super.Instance._super_class_;
									}

								}

							}
						}
					}
				}
			}


			//检查迭代器代码
			for (int i = 0; i < context.scriptDefs.Count; i++)
			{
				var script = context.scriptDefs[i];
				for (int j = 0; j < script.containers.Count; j++)
				{
					var container = script.containers[j];
					for (int k = 0; k < container.Traits.Count; k++)
					{
						var t = container.Traits[k];
						if (t.Kind == TraitKind.Class)
						{
							ASClass cls = t.Class;
							if (!cls.Instance.IsInterface)
							{
								var instance = cls.Instance;
								for (int l = 0; l < instance.Traits.Count; l++)
								{
									var itrait = instance.Traits[l];
									if (itrait.Kind == TraitKind.Method)
									{
										if (itrait.ASMetadata.Any(m => m.Name == "iterator" && m.Items.Count > 0))
										{
											throw new ResolverException(itrait.Token, "[iterator] has too many parameters.");
										}
										else if (itrait.ASMetadata.Count(m => m.Name == "iterator" && m.Items.Count == 0) > 1)
										{
											throw new ResolverException(itrait.Token, "[iterator] only can be defined once.");
										}
									}
								}

								var iterators = instance.Traits.Where(t => t.Kind == TraitKind.Method && t.ASMetadata.Count(m => m.Name == "iterator" && m.Items.Count == 0) == 1).ToArray();
								if (iterators.Length > 1)
								{
									throw new ResolverException(cls.Token, "[iterator] only can be defined once.");
								}
								if (iterators.Length == 1)
								{
									if (iterators[0].Method.ReturnTypeKind == TypeKind.Fun_Void)
									{
										throw new ResolverException(cls.Token, "[iterator] must return an instance of IIterator.");
									}

									if (iterators[0].Method.Parameters.Count != 0)
									{
										throw new ResolverException(cls.Token, "[iterator] has no parameter .");
									}

									var returntype = iterators[0].Method.ReturnType;
									if (returntype.Name != "IIterator" || returntype.Namespace.Name != "" || returntype.Namespace.Kind != NamespaceKind.Package)
									{
										throw new ResolverException(cls.Token, "[iterator] must return an instance of IIterator.");
									}

								}

							}
						}
					}
				}
			}


			//检查操作符重载代码
			for (int i = 0; i < context.scriptDefs.Count; i++)
			{
				var script = context.scriptDefs[i];
				for (int j = 0; j < script.containers.Count; j++)
				{
					var container = script.containers[j];
					for (int k = 0; k < container.Traits.Count; k++)
					{ 
						var t = container.Traits[k];

						if (t.ASMetadata.Exists(m => m.Name == "operator"))
						{
							if (t.Kind == TraitKind.Method)
							{
								var method = t.Method;
								if (!(method.Container is ASClass))
								{
									throw new ResolverException(t.Token, "[operator] only can be defined on static method.");
								}
								ASClass cls = (ASClass) method.Container;
								if (!cls.Instance.Flags.HasFlag(ClassFlags.Final))
								{
									throw new ResolverException(t.Token, "[operator] only can be defined on final class.");
								}

								if (cls.Instance.Flags.HasFlag(ClassFlags.Interface))
								{
									throw new ResolverException(t.Token, "[operator] only can be defined on final class.");
								}

								if (t.ASMetadata.Count( m=>m.Name == "operator" )>1)
								{
									throw new ResolverException(t.Token, "too many [operator] .");
								}

								var operatorMeta = t.ASMetadata.First( m=>m.Name == "operator" );
								if (operatorMeta.Items.Count != 1)
								{
									throw new ResolverException(t.Token, "use [operator(\"<+|-|*|/|%>\")] .");
								}
								var Op = operatorMeta.Items[0].Value;
								if (Op != "\"+\"" && Op != "\"-\"" && Op != "\"*\"" && Op != "\"/\"" && Op != "\"%\"")
								{
									throw new ResolverException(t.Token, "use [operator(\"<+|-|*|/|%>\")] .");
								}

								if (method.Parameters.Count != 2 || method.Parameters[0].IsOptional || method.Parameters[1].IsOptional || method.Parameters[1].IsRest)
								{
									throw new ResolverException(t.Token, "[operator] have two Parameters,not optional or ...rest ");
								}

								if (method.Parameters[0].TypeKind == TypeKind.Any || method.Parameters[1].TypeKind == TypeKind.Any)
								{
									throw new ResolverException(t.Token, "Illegal [operator] parameter type : * ");
								}

								
								if ((ulong)method.Parameters[0].TypeKind != cls.Type_identifier && (ulong)method.Parameters[1].TypeKind != cls.Type_identifier)
								{
									throw new ResolverException(t.Token, "Illegal [operator] parameter type.");
								}

								if (method.ReturnTypeKind == TypeKind.Fun_Void || method.ReturnTypeKind == TypeKind.Any)
								{
									throw new ResolverException(t.Token, "Illegal [operator] return type.");
								}




							}
							else
							{
								throw new ResolverException(t.Token, "[operator] only can be defined on static method.");
							}
						}
					}
				}

			}



			return 0;
		}




	}
}
