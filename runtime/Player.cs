using System.Linq;
using juicescript.ABC;
using juicescript.ABC.INS;
using juicescript.ABC.Locaters;
using juicescript.runtime.buildin;
using juicescript.runtime.gc;
using System;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Diagnostics.Metrics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static juicescript.ABC.INS.INS_If_LogicOp_Goto;

//using static juicescript.ABC.INS.INS_Op_stack_Var_ldConst;
using static juicescript.NaNBoxing;
using static System.Formats.Asn1.AsnWriter;
using System.Collections.Generic;
using System.IO;


namespace juicescript.runtime
{
#if FORCOMPILER
	internal partial class Player
#else
	public partial class Player
#endif

	{
#if FORCOMPILER
	internal
#else
    public
#endif
		struct ReceiveError
		{
			public NaNBoxing error;
			public bool raised;

		}

		static Player()
		{
			NativeFunctionRegistry.RegisterAllFromAssembly(typeof(NativeFun).Assembly);
		}

		public Context Context { get; }

		public Player(int gc_limit = int.MaxValue
#if FORCOMPILER
			,
			bool isComputeConstExpr = false
#endif
			)
		{
			Context = new Context(this, gc_limit);
#if FORCOMPILER
			IsComputeConstExpr = isComputeConstExpr;

		}
		public readonly bool IsComputeConstExpr;
#else
    }
#endif

		private IPrint print;
		public IPrint Print
		{
			get
			{
				if (print == null)
					return DefaultPrint.Instance;
				else
					return print;
			}

			set
			{
				print = value;
			}
		}

#if FORCOMPILER
		public 
#else
		private
#endif
		void CheckRequires()
		{
			if (waitforlink.Count != 0)
			{
				string msg = string.Empty;
				foreach (var item in waitforlink)
				{
					var requires = item.refAssemblys.Where(o => !Context.libs.Any(c => c.assemblyName == o))
						.Select(r => item.assemblyName + "==>" + r);

					msg += string.Join("\n", requires);
				}


				throw new LoaderException(
					"Dependent library not loaded:\n" + msg

					);
			}
		}


		private List<SWCFile> waitforlink = new List<SWCFile>();
		private Dictionary<ASMultiname, string> lib_types = new Dictionary<ASMultiname, string>();


		internal ASNamespaceSet nsSetIncludingPublicAndAS3;

		public SWCFile LoadLib(byte[] data)
		{
			using (MemoryStream ms = new MemoryStream(data))
			{
				var swc = SWCReader.Read(ms);

				//检查是否有命名冲突
				foreach (ASScript script in swc.Scripts)
				{
					if (!lib_types.ContainsKey(script.QName))
					{
						lib_types.Add(script.QName, swc.assemblyName);
					}
					else
					{
						throw new LoaderException($"Duplicate {script.QName} in {swc.assemblyName} and {lib_types[script.QName]}");
					}
				}

				foreach (var @class in swc.Classes)
				{
					if (@class != null)
					{
						Context.dictTypes.Add(@class.Type_identifier, @class);
					}
				}

				if (Context.libs.Any(l => l.assemblyName == swc.assemblyName)
					||
					waitforlink.Any(l => l.assemblyName == swc.assemblyName)
					)
				{
					throw new LoaderException("Duplicate [lib].assemblyName: " + swc.assemblyName);
				}




				//当load一个swc后，进行链接操作
				//将swc加入待链接列表
				//bool flag 
				//do 
				//  flag = false
				//   each s in 待链接列表
				//      if s的依赖都已解决 （在Context.libs） 中，或者没有依赖
				//          链接 s (包括计算布局等等)
				//          将s 移动到 Context.libs中
				//          flag = true
				//      end if
				//    end each
				//while (flag)

				waitforlink.Add(swc);
				bool flag;
				do
				{
					flag = false;
					foreach (var s in waitforlink.ToArray())
					{
						if (s.refAssemblys.All(a => Context.libs.Any(l => l.assemblyName == a)))
						{
							Link(s);
							waitforlink.Remove(s);

							Context.libs.Add(s);
							flag = true;
						}

					}

				} while (flag);


				if (nsSetIncludingPublicAndAS3 == null)
				{
					var nsPublic = swc.Namespaces.FirstOrDefault(n => n != null && n.Kind == NamespaceKind.Package && n.Name == "" && n.def_uri == null);
					var nsAS3 = swc.Namespaces.FirstOrDefault(n => n != null && n.Kind == NamespaceKind.PackageInternal && n.Name == ":AS3" && n.def_uri == "http://adobe.com/AS3/2006/builtin");

					if (nsPublic != null && nsAS3 != null)
					{
						nsSetIncludingPublicAndAS3 = new ASNamespaceSet();
						nsSetIncludingPublicAndAS3.Namespaces = new List<ASNamespace>();
						nsSetIncludingPublicAndAS3.Namespaces.Add(nsPublic);
						nsSetIncludingPublicAndAS3.Namespaces.Add(nsAS3);
					}

				}

				return swc;
			}
		}

#if FORCOMPILER
		public 
#else
		private
#endif
		 static void ComputeTypeSize_Align(ASTrait t, out int align, out int size)
		{
			align = 0;

			if (t.Type == null)
			{
				align = 8;
				size = 8;
			}
			else
			{
				var type = t.TypeKind; //context.dict_traittype[t];
				switch (type)
				{
					case ABC.TypeKind.Any:
						align = 8;
						size = 8;
						break;
					case ABC.TypeKind.Boolean:
					case TypeKind.SByte:
					case TypeKind.Byte:
						align = 1;
						size = 1;
						break;
					case TypeKind.Short:
					case TypeKind.UShort:
						align = 2;
						size = 2;
						break;
					case ABC.TypeKind.Int:
						align = 4;
						size = 4;
						break;
					case ABC.TypeKind.Uint:
						align = 4;
						size = 4;
						break;
					case TypeKind.Float:
						align = 4;
						size = 4;
						break;
					case ABC.TypeKind.Number:
						align = 8;
						size = 8;
						break;
					case ABC.TypeKind.Null:
					case ABC.TypeKind.String:
					case ABC.TypeKind.Function:
						align = 8;
						size = 8;
						break;
					case ABC.TypeKind.Fun_Void:
						throw new InvalidOperationException();
					case ABC.TypeKind.Array:
					case ABC.TypeKind.Vector:
					case ABC.TypeKind.Namespace:
						align = 8;
						size = 8;
						break;
					case ABC.TypeKind.Unknown:
						throw new InvalidOperationException();
					case ABC.TypeKind.Object:
						align = 8;
						size = 8;
						break;
					default:
						align = 8;
						size = 8;
						break;
				}
			}



		}

		private TypeLayout ComputeLayout(ASClass cls, SWCFile at_swc)
		{
			if (Context.dictTypeLayouts.ContainsKey(cls.QName))
			{
				return Context.dictTypeLayouts[cls.QName];
			}

			TypeLayout typeLayout = new TypeLayout();
			typeLayout.ASType = cls;

			TypeLayout superlayout = null;
			if (cls.Instance.Super != null)
			{
				var super = at_swc.Classes.FirstOrDefault(c => c != null && c.QName == cls.Instance.Super);
				if (super != null) //如果基类在当前swc里
				{
					superlayout = ComputeLayout(super, at_swc);


				}
				else
				{
					//基类不在当前swc里，那么就是在已经处理好的libs里，所以直接从字典里加载
					superlayout = Context.dictTypeLayouts[cls.Instance.Super];


				}
				cls.Instance._super_class_ = superlayout.ASType;

			}

			//typeLayout.Class = new Layout();
			//typeLayout.Class.Size = 0;

			//typeLayout.Instance = new Layout();
			typeLayout.Size = 0;

			//int max_class_slot_align = 0;
			int max_instance_slot_align = 0;

			if (superlayout != null)
			{
				//if (superlayout.Class.Size > 0)
				//{
				//    typeLayout.Class.Offset.AddRange(superlayout.Class.Offset);
				//    typeLayout.Class.SlotSize.AddRange(superlayout.Class.SlotSize);
				//    typeLayout.Class.SlotAlign.AddRange(superlayout.Class.SlotAlign);

				//    typeLayout.Class.Size = superlayout.Class.Offset.Last() + superlayout.Class.SlotSize.Last();
				//    max_class_slot_align = superlayout.Class.SlotAlign.Max();
				//}

				if (superlayout.Size > 0)
				{
					typeLayout.Offset.AddRange(superlayout.Offset);
					typeLayout.SlotSize.AddRange(superlayout.SlotSize);
					typeLayout.SlotAlign.AddRange(superlayout.SlotAlign);

					typeLayout.Size = superlayout.Offset.Last() + superlayout.SlotSize.Last();
					max_instance_slot_align = superlayout.SlotAlign.Max();
				}
			}

			//for (int j = 0; j < cls.Traits.Count; j++)
			//{
			//    var t = cls.Traits[j];

			//    if (t.Kind == TraitKind.Constant || t.Kind == TraitKind.Slot)
			//    {
			//        int align, size;
			//        ComputeTypeSize_Align( t, out align, out size);

			//        max_class_slot_align = Math.Max(max_class_slot_align, align);

			//        int baseOffset = typeLayout.Class.Size;
			//        if (baseOffset % align != 0)
			//        {
			//            baseOffset += (align - baseOffset % align);
			//        }
			//        typeLayout.Class.Size = baseOffset + size;
			//        typeLayout.Class.Offset.Add(baseOffset);
			//        typeLayout.Class.SlotSize.Add(size);
			//        typeLayout.Class.SlotAlign.Add(align);
			//    }
			//}
			////计算整体对齐值+padding
			//if (max_class_slot_align > 0 && typeLayout.Class.Size % max_class_slot_align != 0)
			//{
			//    typeLayout.Class.Size += (max_class_slot_align - typeLayout.Class.Size % max_class_slot_align);
			//}



			var instance = cls.Instance;
			for (int j = 0; j < instance.Traits.Count; j++)
			{
				var t = instance.Traits[j];

				if (t.Kind == TraitKind.Constant || t.Kind == TraitKind.Slot)
				{
					int align, size;
					if (instance.Flags.HasFlag(ClassFlags.Struct) && t.TypeKind > TypeKind.Object)
					{
						var member_type = at_swc.Classes.FirstOrDefault(c => c != null && c.QName == t.Type);
						TypeLayout member_layout;
						if (member_type != null)
						{
							member_layout = ComputeLayout(member_type, at_swc);
						}
						else
						{
							member_layout = Context.dictTypeLayouts[t.Type];
						}

						align = member_layout.SlotAlign.Max();
						size = member_layout.Size;
					}
					else
					{
						ComputeTypeSize_Align(t, out align, out size);
					}
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

			Context.dictTypeLayouts.Add(cls.QName, typeLayout);
			return typeLayout;
		}

		/// <summary>
		/// 计算代码上下文
		/// </summary>
		/// <param name="script"></param>
		/// <param name="namespaceSets"></param>
		/// <exception cref="InvalidOperationException"></exception>
		/// 
#if FORCOMPILER
		public 
#else
		private
#endif
		static void ComputeCodeScope(ASScript script, ASNamespaceSet[] namespaceSets)
		{
			if (script.codeScopes != null)
				throw new InvalidOperationException();

			script.codeScopes = new List<CodeScope>();

			for (int i = 0; i < script.allContainers.Count; i++)
			{
				var container = script.allContainers[i];

				CodeScope codeScope = new CodeScope();
				codeScope.Members = new List<ScopeMember>();
				codeScope.Container = container;
				container._link_codescope = codeScope;

				if (container is ASInstance)
				{
					codeScope.Kind = CodeScopeKind.Instance;
				}
				else if (container is ASClass)
				{
					codeScope.Kind = CodeScopeKind.Class;
				}
				else if (container is ASScript)
				{
					codeScope.Kind = CodeScopeKind.Script;
				}
				else if (container is ASMethodBody)
				{
					codeScope.Kind = CodeScopeKind.Method;

					ASMethod method = ((ASMethodBody)container).Method;
					for (int j = 0; j < method.Parameters.Count; j++)
					{
						var p = method.Parameters[j];
						ScopeMember member = new ScopeMember(container, null);
						member.Kind = ScopeMemberKind.Parameter;

						member.PName = p.Name;

						member.Type = p.Type;
						member.TypeKind = p.TypeKind;

						member.ValueKind = p.ValueKind;
						codeScope.Members.Add(member);
					}
					codeScope.ParameterCout = method.Parameters.Count;
					codeScope.NamespaceSet = namespaceSets[method.Body.NamespaceSetIndex];

				}
				else
				{
					throw new InvalidOperationException();
				}

				Stack<ASContainer> inheritStack = new Stack<ASContainer>();
				inheritStack.Push(container);

				if (container is ASInstance)
				{
					var super = ((ASInstance)container)._super_class_;
					while (super != null)
					{
						inheritStack.Push(super.Instance);
						super = super.Instance._super_class_;
					}
				}

				while (inheritStack.Count > 0)
				{
					var c = inheritStack.Pop();
					for (int j = 0; j < c.Traits.Count; j++)
					{
						var t = c.Traits[j];

						if (t.Kind == TraitKind.Constant || t.Kind == TraitKind.Slot)
						{
							ScopeMember member = new ScopeMember(c, t);

							if (t.Kind == TraitKind.Constant)
							{
								member.Kind = ScopeMemberKind.Constant;
							}
							else
							{
								member.Kind = ScopeMemberKind.Slot;
							}

							member.QName = t.QName;
							member.Type = t.Type;
							member.TypeKind = t.TypeKind;
							member.ValueKind = t.ValueKind;

							if (codeScope.Members.Any(
								(m) => m.QName != null && (m.QName == t.QName
								||
								(
								m.QName.Name == t.QName.Name
								&& m.QName.Namespace.Kind == NamespaceKind.Protected
								&& t.QName.Namespace.Kind == NamespaceKind.Protected
								)
								||
								(
								m.QName.Name == t.QName.Name
								&& m.QName.Namespace.Kind == NamespaceKind.PackageInternal
								&& t.QName.Namespace.Kind == NamespaceKind.PackageInternal

								&& t.QName.Namespace.def_uri == m.QName.Namespace.def_uri
								&& m.QName.Namespace.Name == t.QName.Namespace.Name
								)


								)
								))
							{
								if (codeScope.Container == container)
								{
									throw new LoaderException($"A conflict exists with definition {t.QName.Name} in namespace {t.QName.Namespace.ToDebugNameSpaceString()}.") { Token = t.Token };
								}
								else
								{
									throw new LoaderException($"A conflict exists with inherited definition {t.QName.Name} in namespace {t.QName.Namespace.ToDebugNameSpaceString()}.") { Token = t.Token };
								}
							}


							codeScope.Members.Add(member);
						}
					}


				}


				script.codeScopes.Add(codeScope);
				codeScope.index = script.codeScopes.Count;
			}

			for (int i = 0; i < script.codeScopes.Count; i++)
			{
				CodeScope codeScope = script.codeScopes[i];

				if (codeScope.Kind == CodeScopeKind.Instance || codeScope.Kind == CodeScopeKind.Class)
				{
					codeScope.Parent = script.codeScopes.First((s) => s.Kind == CodeScopeKind.Script);
					if (codeScope.Parent == null)
						throw new InvalidOperationException();

					if (codeScope.Kind == CodeScopeKind.Instance)
					{
						if (!((ASInstance)codeScope.Container).IsInterface)
							codeScope.NamespaceSet = ((ASInstance)codeScope.Container).Constructor.Body._link_codescope.NamespaceSet;
					}
					else
					{
						codeScope.NamespaceSet = ((ASClass)codeScope.Container).Constructor.Body._link_codescope.NamespaceSet;
					}

				}
				else if (codeScope.Kind == CodeScopeKind.Script)
				{
					//Script的NamespaceSet就是Initializer的NamespaceSet。
					codeScope.NamespaceSet = ((ASScript)codeScope.Container).Initializer.Body._link_codescope.NamespaceSet;
				}
				else if (codeScope.Kind == CodeScopeKind.Method)
				{
					var method = ((ASMethodBody)codeScope.Container).Method;

					var parent = script.codeScopes.First((s) => s.Container == method.Container);

					if (parent == null)
						throw new InvalidOperationException();

					if (parent.Kind == CodeScopeKind.Script && method != ((ASScript)parent.Container).Initializer)
					{
						parent = script.codeScopes.First((s) => s.Container == ((ASScript)parent.Container).Initializer.Body);
						if (parent == null)
							throw new InvalidOperationException();

					}

					codeScope.Parent = parent;

					if (parent.Kind == CodeScopeKind.Script && method == ((ASScript)parent.Container).Initializer)
					{
						//将父对象的catch变量移动到函数里来。

						for (int j = 0; j < parent.Members.Count; j++)
						{
							if (parent.Members[j].Kind == ScopeMemberKind.Slot && parent.Members[j].QName.Name.StartsWith("%"))
							{
								var m = parent.Members[j];
								codeScope.Members.Add(new ScopeMember(codeScope.Container, m.trait)
								{
									Kind = m.Kind,
									QName = m.QName,
									Type = m.Type,
									TypeKind = m.TypeKind,
									ValueKind = m.ValueKind,
									__rt_type_class__ = m.__rt_type_class__,
									_rt_type_flag = m._rt_type_flag

								});

								parent.Members.RemoveAt(j);
								j--;

							}
						}

					}
				}
				else
				{
					throw new InvalidOperationException();
				}
			}


		}


		/// <summary>
		/// 计算虚函数表
		/// </summary>
		/// <param name="script"></param>		
#if FORCOMPILER
		public 
#else
		private
#endif
		void ComputeVTable(ASScript script)
		{

			for (int i = 0; i < script.allContainers.Count; i++)
			{
				var container = script.allContainers[i];
				VTable vTable = new VTable();
				vTable.Items = new List<VTableItem>();

				container._vtable = vTable;
				if (container is ASInstance)
				{

				}
				else if (container is ASClass)
				{

				}
				else if (container is ASScript)
				{
					continue;
				}
				else if (container is ASMethodBody)
				{
					continue;
				}
				else
				{
					throw new InvalidOperationException();
				}

				Stack<ASContainer> inheritStack = new Stack<ASContainer>();
				inheritStack.Push(container);

				if (!(container is ASInstance && ((ASInstance)container).IsInterface))
				{

					if (container is ASInstance)
					{

						var super = ((ASInstance)container)._super_class_;
						while (super != null)
						{
							inheritStack.Push(super.Instance);
							super = super.Instance._super_class_;
						}

					}

					if (container is ASClass) //每个Class都有一个 prototype的 getter
					{
						if (container != Context.CLASS)
						{
							inheritStack.Push(Context.CLASS.Instance);
						}
					}


					while (inheritStack.Count > 0)
					{
						var c = inheritStack.Pop();

						List<VTableItem> c_tableitems = new List<VTableItem>();

						for (int j = 0; j < c.Traits.Count; j++)
						{
							var t = c.Traits[j];
							if (t.Kind == TraitKind.Method || t.Kind == TraitKind.Getter || t.Kind == TraitKind.Setter)
							{
								//note:私有方法也要从基类拷贝一份过来，否则序号就不对了
								t.Method.__ismethod = true;

								//构造函数不能主动调用
								if (t.Method.IsConstructor)
								{
									continue;
								}




								if (container._link_codescope.Members.Any(
									(m) => m.QName != null && (m.QName == t.QName
									||
									(
									m.QName.Name == t.QName.Name
									&& m.QName.Namespace.Kind == NamespaceKind.Protected
									&& t.QName.Namespace.Kind == NamespaceKind.Protected
									)
									||
									(
									m.QName.Name == t.QName.Name
									&& m.QName.Namespace.Kind == NamespaceKind.Package
									&& t.QName.Namespace.Kind == NamespaceKind.Package
									)
									||
									(
										m.QName.Name == t.QName.Name
										&&
										m.QName.Namespace.Kind == NamespaceKind.PackageInternal
										&&
										t.QName.Namespace.Kind == NamespaceKind.PackageInternal
										&&
										m.QName.Namespace.def_uri == t.QName.Namespace.def_uri
									)
									)
									))
								{
									throw new LoaderException($"Illegal override of {t.QName.Name} in {container.QName.Name}") { Token = t.Token };
								}

								//查找本类型中重复定义的方法
								if (c_tableitems.Any(
									(m) =>
									t.QName.Name == m.Trait.QName.Name
									&&
									(
										(
											t.Kind == TraitKind.Method
											&&
											t.QName.Namespace == m.Trait.QName.Namespace
										)
										||
										(
											t.Kind == TraitKind.Getter
											&&
											t.QName.Namespace == m.Trait.QName.Namespace
											&&
											m.Trait.Kind != TraitKind.Setter
										)
										||
										(
											t.Kind == TraitKind.Setter
											&&
											t.QName.Namespace == m.Trait.QName.Namespace
											&&
											m.Trait.Kind != TraitKind.Getter
										)
									)

									)
									)
								{
									throw new LoaderException($"A conflict exists with definition {t.QName.Name} in namespace {t.QName.Namespace.ToDebugNameSpaceString()}.") { Token = t.Token };
								}



								var tooverride = vTable.Items.FirstOrDefault((m) => m.Trait.QName != null
									&& m.Trait.Kind == t.Kind && (m.Trait.QName == t.QName
									||
									(
										m.Trait.QName.Name == t.QName.Name
										&& m.Trait.QName.Namespace.Kind == NamespaceKind.Protected
										&& t.QName.Namespace.Kind == NamespaceKind.Protected
									)
									 ||
									(
										m.Trait.QName.Name == t.QName.Name
										&& m.Trait.QName.Namespace.Kind == NamespaceKind.Package
										&& t.QName.Namespace.Kind == NamespaceKind.Package
									)
									 ||
									(
										m.Trait.QName.Name == t.QName.Name
										&& m.Trait.QName.Namespace.Kind == NamespaceKind.PackageInternal
										&& t.QName.Namespace.Kind == NamespaceKind.PackageInternal
										&& m.Trait.QName.Namespace.def_uri == t.QName.Namespace.def_uri
										&& m.DefineAt.QName.Namespace.Name == c.QName.Namespace.Name
									)
									)
								);

								if (tooverride == null)
								{
									if (t.Method.Flags.HasFlag(MethodFlags.MarkOverride))
									{
										throw new LoaderException("Method marked override must override another method.") { Token = t.Token };
									}

									c_tableitems.Add(new VTableItem() { Trait = t, DefineAt = c, InheritFrom = null });

								}
								else
								{
									if (t.Method.Flags.HasFlag(MethodFlags.MarkOverride))
									{
										//检查签名
										var m = tooverride.Trait.Method;
										if (m.ReturnTypeKind != t.Method.ReturnTypeKind)
										{
											throw new LoaderException("Incompatible override.") { Token = t.Token };
										}

										if (m.Parameters.Count != t.Method.Parameters.Count)
										{
											throw new LoaderException("Incompatible override.") { Token = t.Token };
										}

										for (int k = 0; k < m.Parameters.Count; k++)
										{
											var p1 = m.Parameters[k];
											var p2 = t.Method.Parameters[k];

											if (p1.IsRest != p2.IsRest)
											{
												throw new LoaderException("Incompatible override.") { Token = t.Token };
											}
											if (p1.IsOptional != p2.IsOptional)
											{
												throw new LoaderException("Incompatible override.") { Token = t.Token };
											}
											if (p1.TypeKind != p2.TypeKind)
											{
												throw new LoaderException("Incompatible override.") { Token = t.Token };
											}

										}



										tooverride.InheritFrom = tooverride.DefineAt;
										tooverride.Trait = t;
										tooverride.DefineAt = c;
									}
									else
									{
										throw new LoaderException($"Overriding a function that is not marked for override") { Token = t.Token };
									}
								}
							}
						}


						vTable.Items.AddRange(c_tableitems);

					}

				}
				else
				{
					//接口虚函数表
					ASInstance intf = (ASInstance)container;

					List<ASInstance> impls = new List<ASInstance>();
					impls.AddRange(intf._implements_.Select(i => i.Instance));

					while (impls.Count > 0)
					{
						List<ASInstance> impl2 = new List<ASInstance>();
						for (int j = 0; j < impls.Count; j++)
						{
							ASInstance impl = impls[j];
							inheritStack.Push(impl);
							for (int k = 0; k < impl._implements_.Count; k++)
							{
								if (!impl2.Contains(impl._implements_[k].Instance))
								{
									impl2.Add(impl._implements_[k].Instance);
								}
							}
						}
						impls = impl2;
					}

					while (inheritStack.Count > 0)
					{
						var c = inheritStack.Pop();
						List<VTableItem> c_tableitems = new List<VTableItem>();

						if (!(c is ASInstance && ((ASInstance)c).IsInterface))
						{
							throw new InvalidOperationException("意外错误，出现了非接口的对象");
						}

						for (int j = 0; j < c.Traits.Count; j++)
						{
							var t = c.Traits[j];
							if (t.Kind == TraitKind.Method || t.Kind == TraitKind.Getter || t.Kind == TraitKind.Setter)
							{
								t.Method.__ismethod = true;
								if (c == intf)
								{
									c_tableitems.Add(new VTableItem() { Trait = t, DefineAt = c, InheritFrom = null });
								}
								else
								{
									c_tableitems.Add(new VTableItem() { Trait = t, DefineAt = intf, InheritFrom = c });
								}
							}
						}

						vTable.Items.AddRange(c_tableitems);
					}

				}

			}

		}


		/// <summary>
		/// 计算接口和函数表的对应关系
		/// </summary>
		/// <param name="script"></param>
#if FORCOMPILER
		public 
#else
		private
#endif
		void ComputeInterface(ASScript script)
		{
			for (int i = 0; i < script.allContainers.Count; i++)
			{
				var container = script.allContainers[i];

				ASInstance instance = container as ASInstance;
				if (instance != null && !instance.IsInterface)
				{
					//收集这个类实现的所有接口，如果它的基类实现了某个接口，则它也实现了某个接口。。
					List<ASClass> interface_list = new List<ASClass>();

					ASInstance _s = instance;
					while (true)
					{
						List<ASClass> temp = _s._implements_;

						do
						{
							List<ASClass> temp2 = new List<ASClass>();
							for (int j = 0; j < temp.Count; j++)
							{
								if (!interface_list.Contains(temp[j]))
								{
									interface_list.Add(temp[j]);
									temp2.AddRange(temp[j].Instance._implements_);
								}
							}

							temp = temp2;

						}
						while (temp.Count > 0);

						if (_s._super_class_ != null)
						{
							_s = _s._super_class_.Instance;
						}
						else
						{
							break;
						}
					}

					for (int j = 0; j < interface_list.Count; j++)
					{
						ASClass intftype = interface_list[j];
						ASInstance intf = intftype.Instance;

						ASInstance.interface_impl method_index = new ASInstance.interface_impl(intftype);

						for (int k = 0; k < intf._vtable.Items.Count; k++)
						{
							var m = intf._vtable.Items[k];

							var checkpara = (List<ASParameter> a, List<ASParameter> b) =>
							{
								if (a.Count != b.Count)
								{
									return false;
								}
								else
								{
									for (int i = 0; i < a.Count; i++)
									{
										if (a[i].IsRest != b[i].IsRest)
											return false;

										if (a[i].IsOptional != b[i].IsOptional)
											return false;

										if (a[i].TypeKind != b[i].TypeKind)
											return false;
									}

									return true;
								}

							};

							int vt_index = container._vtable.Items.FindIndex(
								(t) =>
								{
									return
									t.Trait.QName.Namespace.Kind == NamespaceKind.Package &&
									t.Trait.Kind == m.Trait.Kind &&

									t.Trait.QName.Name == m.Trait.QName.Name &&

									t.Trait.Method.ReturnType == m.Trait.Method.ReturnType &&

								   checkpara(t.Trait.Method.Parameters, m.Trait.Method.Parameters);

								}

								);

							if (vt_index < 0)
							{
								throw new LoaderException($"interface method {m.Trait.QName.Name} in interface {intf.QName.Name} not implemented by class {instance.QName.Name}") { Token = instance.Constructor.Token };
							}

							//ulong type = intftype.Type_identifier;
							method_index.Add(vt_index);
						}

						instance._interface_impl_.Add(method_index);
					}

				}


			}

		}

		private unsafe void linkConstantValue(NaNBoxing* b, ASMethodBody.MethodHeapConstants hconstants)
		{
			if (b->ValueType == NaNBoxing.BoxType.HeapPtr)
			{
				Debug.Assert(b->HeapKind == NaNBoxing.UNKNOWN_HEAPKIND);
				var kind = hconstants.pool_kinds[b->HeapPtr];
				if (kind == ASMethodBody.PoolHeapPtrKind.String)
				{
					//*b = (NaNBoxing)hconstants.pool_values[b->HeapPtr];

					NaNBoxing v = (NaNBoxing)hconstants.pool_values[b->HeapPtr];
					b->SetHeapPtr(v.HeapPtr, v.HeapKind, v.HeapFlag);

				}
				else if (kind == ASMethodBody.PoolHeapPtrKind.Namespace)
				{
					NaNBoxing v = (NaNBoxing)hconstants.pool_values[b->HeapPtr];
					b->SetHeapPtr(v.HeapPtr, v.HeapKind, v.HeapFlag);
				}
				else if (kind == ASMethodBody.PoolHeapPtrKind.LD_Class)
				{
					Debug.Assert(hconstants.pool_values[b->HeapPtr] is ASClass);

					b->SetInt(b->HeapPtr);
				}
				else if (kind == ASMethodBody.PoolHeapPtrKind.Method)
				{
					Debug.Assert(hconstants.pool_values[b->HeapPtr] is ASMethod);
					b->SetInt(b->HeapPtr);
				}
				else if (kind == ASMethodBody.PoolHeapPtrKind.SuperMethod)
				{
					Debug.Assert(hconstants.pool_values[b->HeapPtr] is ASMethod);
					b->SetInt(b->HeapPtr);
				}
				else if (kind == ASMethodBody.PoolHeapPtrKind.VectorDef)
				{
					Debug.Assert(hconstants.pool_values[b->HeapPtr] is ASVector);
					var v = (ASVector)hconstants.pool_values[b->HeapPtr];
					MakeVectorType(v);
					int newindex = Context.Vectors.IndexOf(v);
					b->SetInt(newindex);
				}
				else
				{
					throw new InvalidOperationException();
				}

				//ASMethodBody.PoolHeapPtrKind kind = (ASMethodBody.PoolHeapPtrKind)(b->HeapPtr >> 24);
				//if (kind == ASMethodBody.PoolHeapPtrKind.String)
				//{
				//	if (swc.runtime_alloced_strings[b->HeapPtr & 0xffffff].ValueType == NaNBoxing.BoxType.HeapPtr)
				//	{
				//		b->SetHeapPtr(swc.runtime_alloced_strings[b->HeapPtr & 0xffffff].HeapPtr, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				//	}
				//	else
				//	{
				//		string str = swc.const_strings[b->HeapPtr & 0xffffff];
				//		int index = Context.GC.AllocString(str); if (index == 0) { throw new LoaderException("alloc string failed,out of memory"); }
				//		;
				//		swc.runtime_alloced_strings[b->HeapPtr & 0xffffff].SetHeapPtr(index, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);

				//		b->SetHeapPtr(index, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				//		Context.GC.Root.Add(Context.GC.Heap[index]);

				//	}
				//}
				//else if (kind == ASMethodBody.PoolHeapPtrKind.LD_Class)
				//{
				//	ulong id = swc.ld_classid[b->HeapPtr & 0xffffff];
				//	ASClass @class = Context.dictTypes[id];

				//	if (!Context.link_const_class.Contains(@class))
				//	{
				//		Context.link_const_class.Add(@class);
				//	}

				//	int index = Context.link_const_class.IndexOf(@class);
				//	b->SetUInt((uint)index);

				//}
				//else if (kind == ASMethodBody.PoolHeapPtrKind.Namespace)
				//{
				//	ASNamespace @namespace = swc.Namespaces[b->HeapPtr & 0xffffff];

				//	if (@namespace.__instance_index__ == 0)
				//	{
				//		string uri = @namespace.def_uri;
				//		if (string.IsNullOrEmpty(uri))
				//		{
				//			uri = @namespace.Name;
				//		}

				//		int uriPtr = Context.GC.AllocString(uri); if (uriPtr == 0) { throw new LoaderException("alloc uriPtr failed,out of memory"); }
				//		;

				//		int index = Context.GC.AllocNamespace(@namespace, 0, uriPtr); if (index == 0) { throw new LoaderException("alloc namespace failed,out of memory"); }
				//		;
				//		b->SetHeapPtr(index, (byte)RtHeapTypeKind.NAMESPACE, (byte)HeapKindFlag.NONE);

				//		@namespace.__instance_index__ = index;

				//		Context.GC.Root.Add(Context.GC.Heap[index]);

				//	}
				//	else
				//	{
				//		b->SetHeapPtr(@namespace.__instance_index__, (byte)RtHeapTypeKind.NAMESPACE, (byte)HeapKindFlag.NONE);
				//	}
				//}
				//else if (kind == ASMethodBody.PoolHeapPtrKind.VectorDef)
				//{
				//	int index = b->HeapPtr & 0xffffff;
				//	var v = swc.Vectors[index];

				//	MakeVectorType(v);

				//	int newindex = Context.Vectors.IndexOf(v);
				//	b->SetInt(newindex);

				//}
				//else if (kind == ASMethodBody.PoolHeapPtrKind.Method)
				//{
				//	int index = b->HeapPtr & 0xffffff;
				//	var m = swc.Methods[index];

				//	if (!Context.link_const_methods.Contains(m))
				//	{
				//		Context.link_const_methods.Add(m);
				//	}

				//	int m_index = Context.link_const_methods.IndexOf(m);
				//	b->SetUInt((uint)m_index);

				//}
				//else if (kind == ASMethodBody.PoolHeapPtrKind.SuperMethod)
				//{
				//	int index = b->HeapPtr & 0xffffff;
				//	var superconfig = swc.ld_supermethods[index];
				//	ASClass @class = Context.dictTypes[superconfig.Item1];

				//	var m = @class.Instance._vtable.Items[superconfig.Item2];

				//	if (!Context.link_const_vtableitems.Contains(m))
				//	{
				//		Context.link_const_vtableitems.Add(m);
				//	}

				//	int m_index = Context.link_const_vtableitems.IndexOf(m);
				//	b->SetUInt((uint)m_index);


				//}
				//else
				//{
				//	throw new InvalidOperationException();
				//}
			}

		}


		private void linkMethod(ASMethod method, SWCFile swc)
		{
			var heapconsts = method.Body.heapConstants;
			
			
			for (int i = 0; i < heapconsts.pool_kinds.Length; i++)
			{
				var kind = heapconsts.pool_kinds[i];
				if (kind == ASMethodBody.PoolHeapPtrKind.String)
				{
					if (swc.runtime_alloced_strings[(int)heapconsts.pool_values[i]].ValueType == NaNBoxing.BoxType.HeapPtr)
					{
						NaNBoxing v = default;
						v.SetHeapPtr(swc.runtime_alloced_strings[(int)heapconsts.pool_values[i]].HeapPtr, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
						heapconsts.pool_values[i] = v;
					}
					else
					{
						string str = swc.const_strings[(int)heapconsts.pool_values[i]];
						int index = Context.GC.AllocString(str); if (index == 0) { throw new LoaderException("alloc string failed,out of memory"); }
						;
						swc.runtime_alloced_strings[(int)heapconsts.pool_values[i]].SetHeapPtr(index, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);

						heapconsts.pool_values[i] = swc.runtime_alloced_strings[(int)heapconsts.pool_values[i]];

						Context.GC.Root.Add(Context.GC.Heap[index]);

					}
				}
				else if (kind == ASMethodBody.PoolHeapPtrKind.Namespace)
				{
					int vindex = (int)heapconsts.pool_values[i];
					ASNamespace @namespace = swc.Namespaces[vindex];

					if (@namespace.__instance_index__ == 0)
					{
						string uri = @namespace.def_uri;
						if (string.IsNullOrEmpty(uri))
						{
							uri = @namespace.Name;
						}

						int uriPtr = Context.GC.AllocString(uri); if (uriPtr == 0) { throw new LoaderException("alloc uriPtr failed,out of memory"); }
						;

						int index = Context.GC.AllocNamespace(@namespace, 0, uriPtr); if (index == 0) { throw new LoaderException("alloc namespace failed,out of memory"); }
						;

						NaNBoxing v = default;
						v.SetHeapPtr(index, (byte)RtHeapTypeKind.NAMESPACE, (byte)HeapKindFlag.NONE);
						heapconsts.pool_values[i] = v;

						@namespace.__instance_index__ = index;

						Context.GC.Root.Add(Context.GC.Heap[index]);

					}
					else
					{
						NaNBoxing v = default;
						v.SetHeapPtr(@namespace.__instance_index__, (byte)RtHeapTypeKind.NAMESPACE, (byte)HeapKindFlag.NONE);
						heapconsts.pool_values[i] = v;
					}
				}
				else if (kind == ASMethodBody.PoolHeapPtrKind.LD_Class)
				{
					ulong classid = (ulong)heapconsts.pool_values[i];
					ASClass @class = Context.dictTypes[classid];
					heapconsts.pool_values[i] = @class;
				}
				else if (kind == ASMethodBody.PoolHeapPtrKind.Method)
				{
					int index = (int)heapconsts.pool_values[i];
					heapconsts.pool_values[i] = swc.Methods[index];
				}
				else if (kind == ASMethodBody.PoolHeapPtrKind.SuperMethod)
				{
					Tuple<ulong, int> tuple = (Tuple<ulong, int>)heapconsts.pool_values[i];
					ASClass supercls = Context.dictTypes[tuple.Item1];

					heapconsts.pool_values[i] = supercls.Instance._vtable.Items[tuple.Item2].Trait.Method;

				}
				else if (kind == ASMethodBody.PoolHeapPtrKind.VectorDef)
				{
					ASVector v = (ASVector)heapconsts.pool_values[i];
					Debug.Assert(v.vector_class != null);

				}
				else
				{
					throw new InvalidOperationException();
				}
			}

			linkMethod_Consts(method);

			if (method.ReturnTypeKind != TypeKind.Fun_Void && method.ReturnTypeKind != TypeKind.Any)
			{
				method.__return_type_class__ = Context.dictTypes[(ulong)method.ReturnTypeKind];
			}
		}

		private void linkMethod_Consts(ASMethod method)
		{
			var init_const = (byte[] buffer) =>
			{
				unsafe
				{
					fixed (void* ptr = buffer)//handle.AddrOfPinnedObject().ToPointer();
					{
						//Span<int> count_span = new Span<int>(ptr, 2);
						//int count = count_span[1];
						//Span<NaNBoxing> boxings = new Span<NaNBoxing>((int*)ptr + 2, count);

						int count = *((int*)ptr + 1);
						int ins_count = *((int*)ptr + 2);

						NaNBoxing* b = (NaNBoxing*)((int*)ptr + 3 + 2 * ins_count);

						for (int j = 0; j < count; j++)
						{
							linkConstantValue(b, method.Body.heapConstants);
							++b;
						}


					}
				}
			};

			init_const(method.Body.ByteCode);

			if (method.Body.param_defaultvalues != null)
			{
				init_const(method.Body.param_defaultvalues);
			}

		}

		private void Link(SWCFile swc)
		{
			if (swc.assemblyName == "juice_global.swc")
			{
				foreach (var cls in swc.Classes)
				{
					if (cls != null && cls.QName.Name == "Object")
					{
						Context.OBJECT = cls;
						Context.global_swc = swc;
						if (cls.Type_identifier != (ulong)TypeKind.Object)
						{
							throw new InvalidCastException("Object类型错误");
						}
					}
					else if (cls != null && cls.QName.Name == "Class")
					{
						Context.CLASS = cls;

						if (cls.Type_identifier != (ulong)TypeKind.Class)
						{
							throw new InvalidCastException("Class类型错误");
						}

					}
					else if (cls != null && cls.QName.Name == "String")
					{
						Context.STRING = cls;
					}
					else if (cls != null && (TypeKind)cls.Type_identifier == TypeKind.Vector)
					{
						Context.VECTOR = cls;
					}
					else if (cls != null && (TypeKind)cls.Type_identifier == TypeKind.Array)
					{
						Context.ARRAY = cls;
					}
					else if (cls != null && (TypeKind)cls.Type_identifier == TypeKind.Number)
					{
						Context.NUMBER = cls;
					}
					else if (cls != null && (TypeKind)cls.Type_identifier == TypeKind.Float)
					{
						Context.FLOAT = cls;
					}
					else if (cls != null && (TypeKind)cls.Type_identifier == TypeKind.Function)
					{
						Context.FUNCTION = cls;
					}
					else if (cls != null && (TypeKind)cls.Type_identifier == TypeKind.Byte)
					{
						Context.BYTE = cls;
					}
					else if (cls != null && (TypeKind)cls.Type_identifier == TypeKind.SByte)
					{
						Context.SBYTE = cls;
					}
					else if (cls != null && (TypeKind)cls.Type_identifier == TypeKind.Short)
					{
						Context.SHORT = cls;
					}
					else if (cls != null && (TypeKind)cls.Type_identifier == TypeKind.UShort)
					{
						Context.USHORT = cls;
					}
					else if (cls != null && (TypeKind)cls.Type_identifier == TypeKind.Int)
					{
						Context.INT = cls;
					}
					else if (cls != null && (TypeKind)cls.Type_identifier == TypeKind.Uint)
					{
						Context.UINT = cls;
					}
					else if (cls != null && (TypeKind)cls.Type_identifier == TypeKind.Boolean)
					{
						Context.BOOLEAN = cls;
					}
					else if (cls != null && (TypeKind)cls.Type_identifier == TypeKind.Namespace)
					{
						Context.NAMESPACE = cls;
					}
					else if (cls != null && cls.QName.Name == "Error")
					{
						Context.ERROR = cls;
					}
					else if (cls != null && cls.QName.Name == "TypeError")
					{
						Context.TYPE_ERROR = cls;
					}
					else if (cls != null && cls.QName.Name == "ReferenceError")
					{
						Context.REFERENCE_ERROR = cls;
					}
					else if (cls != null && cls.QName.Name == "RangeError")
					{
						Context.RANGE_ERROR = cls;
					}
					else if (cls != null && cls.QName.Name == "ArgumentError")
					{
						Context.ARGEMENT_ERROR = cls;
					}
					else if (cls != null && cls.QName.Name == "IllegalOperationError" && cls.QName.Namespace.Name == "flash.errors")
					{
						Context.ILLEGALOPERATION_ERROR = cls;
					}
					else if (cls != null && cls.QName.Name == "MethodClosure" && cls.QName.Namespace.Name == "__AS3__")
					{
						Context.METHOD_CLOSURE = cls;
					}
					else if (cls != null && cls.QName.Name == "IIterator")
					{
						Context.IITERATOR = cls;
					}
					else if (cls != null && cls.QName.Name == "generator" && cls.QName.Namespace.Name == "FilePrivateNS:IIterator")
					{
						Context.GENERATOR = cls;
					}
					else if (cls != null && cls.QName.Name == "Promise")
					{
						Context.PROMISE = cls;
					}
					else if (cls != null && cls.QName.Name == "Vector2" && cls.QName.Namespace.Name == "geom")
					{
						Context.VEC2 = cls;
					}
					else if (cls != null && cls.QName.Name == "Matrix2x2" && cls.QName.Namespace.Name == "geom")
					{
						Context.MAT22 = cls;
					}
				}
			}


			///遍历所有class

			foreach (var cls in swc.Classes)
			{
				if (cls != null)
				{
					Context.dictTypeQNames.Add(cls.QName, cls);
				}

				////计算Class的内存布局
				if (cls != null && !cls.Instance.IsInterface)
				{
					var layout = ComputeLayout(cls, swc);

					if (layout.Size <= RtInstance.MAX_CACHEABLE_SIZE &&
						(cls.Type_identifier > (ulong)TypeKind.Namespace || cls.Type_identifier == (ulong)TypeKind.Object)
						)
					{
						bool extendwapper = cls.Instance.Flags.HasFlag(ClassFlags.Wapper);
						if (!extendwapper)
						{
							var super = cls.Instance._super_class_;//wapper对象不可缓存，继承自wapper对象的也不可缓存
							while (super != null)
							{
								if (super.Instance.Flags.HasFlag(ClassFlags.Wapper))
								{
									extendwapper = true;
									break;
								}
								super = super.Instance._super_class_;
							}
						}
						if (!extendwapper)
						{
							cls.Instance.Flags |= ClassFlags.CacheAble;
						}
					}

					if (layout.Size > RtInstance.MAX_CACHEABLE_SIZE && cls.Instance.Flags.HasFlag(ClassFlags.Struct))
					{
						throw new LoaderException($"struct {cls.QName} is too large");
					}

				}
			}

			foreach (var cls in swc.Classes)
			{
				////整备interface
				if (cls != null)
				{
					for (int i = 0; i < cls.Instance.Interfaces.Count; i++)
					{
						var iqname = cls.Instance.Interfaces[i];
						var ifc = Context.dictTypeQNames[iqname];

						cls.Instance._implements_.Add(ifc);
					}

				}
			}


			foreach (var cls in swc.Classes)
			{

				if (cls != null)
				{
					if (cls.Instance.Flags.HasFlag(ClassFlags.Indexer))
					{
						var findtype = cls.Instance;

						ASTrait i_get = null;
						ASTrait i_set = null;
						ASTrait i_delete = null;
						while (i_get == null)  //索引器可以继承
						{
							i_get = findtype.Traits.FirstOrDefault(t => t.Kind == TraitKind.Method && t.ASMetadata.Count(m => m.Name == "indexer_get" && m.Items.Count == 0) == 1);
							i_set = findtype.Traits.FirstOrDefault(t => t.Kind == TraitKind.Method && t.ASMetadata.Count(m => m.Name == "indexer_set" && m.Items.Count == 0) == 1);
							i_delete = findtype.Traits.FirstOrDefault(t => t.Kind == TraitKind.Method && t.ASMetadata.Count(m => m.Name == "indexer_delete" && m.Items.Count == 0) == 1);
							findtype = findtype._super_class_.Instance;
						}

						cls.Instance.indexer_get = i_get?.Method;
						cls.Instance.indexer_set = i_set?.Method;
						cls.Instance.indexer_delete = i_delete?.Method;

						if (cls.Instance.indexer_get == null || cls.Instance.indexer_set == null || cls.Instance.indexer_delete == null)
						{
							throw new LoaderException($"Type: {cls} indexer load failed");
						}

						//处理子类override:
						var basetype = findtype;

						findtype = cls.Instance;
						while (findtype != basetype)
						{
							var override_get = findtype.Traits.Reverse<ASTrait>().FirstOrDefault(t => t.Kind == TraitKind.Method && t.Attributes.HasFlag(TraitAttributes.Override) && t.QName.Name == i_get.QName.Name);
							if (override_get != null)
							{
								cls.Instance.indexer_get = override_get.Method;
								break;
							}
							findtype = findtype._super_class_.Instance;
						}

						findtype = cls.Instance;
						while (findtype != basetype)
						{
							var override_set = findtype.Traits.Reverse<ASTrait>().FirstOrDefault(t => t.Kind == TraitKind.Method && t.Attributes.HasFlag(TraitAttributes.Override) && t.QName.Name == i_set.QName.Name);
							if (override_set != null)
							{
								cls.Instance.indexer_set = override_set.Method;
								break;
							}
							findtype = findtype._super_class_.Instance;
						}

						findtype = cls.Instance;
						while (findtype != basetype)
						{
							var override_delete = findtype.Traits.Reverse<ASTrait>().FirstOrDefault(t => t.Kind == TraitKind.Method && t.Attributes.HasFlag(TraitAttributes.Override) && t.QName.Name == i_delete.QName.Name);
							if (override_delete != null)
							{
								cls.Instance.indexer_delete = override_delete.Method;
								break;
							}
							findtype = findtype._super_class_.Instance;
						}
					}

					//iterator
					if (!cls.Instance.IsInterface)
					{
						var findtype = cls.Instance;

						ASTrait iterator = null;
						while (iterator == null) //迭代器也可继承
						{
							iterator = findtype.Traits.FirstOrDefault(t => t.Kind == TraitKind.Method && t.ASMetadata.Count(m => m.Name == "iterator" && m.Items.Count == 0) == 1);
							if (findtype._super_class_ != null)
							{
								findtype = findtype._super_class_.Instance;
							}
							else
							{
								break;
							}
						}

						var basetype = findtype;
						cls.Instance.iterator = iterator?.Method;

						if (iterator != null)
						{
							findtype = cls.Instance;
							while (findtype != basetype)
							{
								var override_iterator =
									findtype.Traits.Reverse<ASTrait>().FirstOrDefault(
										t => t.Kind == TraitKind.Method && t.Attributes.HasFlag(TraitAttributes.Override) && t.QName.Name == iterator.QName.Name);
								if (override_iterator != null)
								{
									cls.Instance.iterator = override_iterator.Method;
									break;
								}
								findtype = findtype._super_class_.Instance;
							}
						}

					}

				}
			}



			//针对每个ASScript,计算每个ASScript内的Container对应的CodeScope
			foreach (var script in swc.Scripts)
			{
				ComputeCodeScope(script, swc.NamespaceSets);

				for (int i = 0; i < script.codeScopes.Count; i++)
				{
					var scope = script.codeScopes[i];
					if (scope.Kind == CodeScopeKind.Instance)
					{
						if (!((ASInstance)scope.Container).IsInterface)
						{
							scope.TypeLayout = Context.dictTypeLayouts[scope.Container.QName];
						}
					}

				}
				ComputeVTable(script);

			}

			foreach (var script in swc.Scripts)
			{
				ComputeInterface(script);
			}


			foreach (var item in swc.Vectors)
			{
				if (!Context.Vectors.Contains(item))
				{
					Context.Vectors.Add(item);
				}				
				MakeVectorType(item);
			}


			////构造常量池中的堆类型常量--比如字符串
			for (int i = 1; i < swc.Methods.Count; i++)
			{
				var method = swc.Methods[i];

				linkMethod(method, swc);

			}

			for (int i = 0; i < swc.Scripts.Count; i++)
			{
				var script = swc.Scripts[i];
				for (int j = 0; j < script.allContainers.Count; j++)
				{
					var c = script.allContainers[j];
					if (c != null)
					{
						for (int k = 0; k < c.Traits.Count; k++)
						{
							var t = c.Traits[k];
							if (t.Kind == TraitKind.Slot || t.Kind == TraitKind.Constant)
							{
								if (t.TypeKind >= TypeKind.Object)
								{
									t.__rt_type_class__ = Context.dictTypes[(ulong)t.TypeKind];
								}

								if (t.Value != null && t.Value.initValue.HasValue)
								{
									Debug.Assert(c._link_codescope.Kind == CodeScopeKind.Script 
										|| c._link_codescope.Kind == CodeScopeKind.Class
										|| c._link_codescope.Kind == CodeScopeKind.Instance
										|| c._link_codescope.Kind == CodeScopeKind.Method
										);
									
									unsafe
									{
										NaNBoxing v = t.Value.initValue.Value;
										if (c._link_codescope.Kind == CodeScopeKind.Script)
										{
											linkConstantValue(&v, ((ASScript)c).Initializer.Body.heapConstants);
										}
										else if (c._link_codescope.Kind == CodeScopeKind.Class)
										{
											linkConstantValue(&v, ((ASClass)c).Constructor.Body.heapConstants);
										}
										else if (c._link_codescope.Kind == CodeScopeKind.Instance)
										{
											linkConstantValue(&v, ((ASInstance)c).Constructor.Body.heapConstants);
										}
										else
										{
											linkConstantValue(&v, ((ASMethodBody)c).heapConstants);
										}

										t.Value.initValue = v;

									}

								}

							}

						}
					}
				}
			}

			for (int i = 0; i < swc.Scripts.Count; i++)
			{
				var script = swc.Scripts[i];
				for (int j = 0; j < script.codeScopes.Count; j++)
				{
					var codescope = script.codeScopes[j];
					for (int k = 0; k < codescope.Members.Count; k++)
					{
						var smember = codescope.Members[k];

						if (smember.TypeKind > TypeKind.Object)
						{
							smember.__rt_type_class__ = Context.dictTypes[(ulong)smember.TypeKind];
							smember._rt_type_flag = smember.__rt_type_class__.Instance.Flags;
						}

						if (smember.trait != null && smember.trait.Value != null && smember.trait.Value.initValue.HasValue)
						{
							
							smember._initvalue = smember.trait.Value.initValue;
							
						}

					}
				}
			}

			for (int i = 0; i < swc.Scripts.Count; i++)
			{
				ComputeOperatorTable(swc.Scripts[i], Context.dictTypes);
			}

		}

#if FORCOMPILER
		public 
#else
		private
#endif
		 void MakeVectorType(ASVector vector
#if FORCOMPILER
			, IEnumerable<ASClass> compilingClasses = null
#endif
			)
		{
			if (Context.OBJECT == null)
				throw new InvalidOperationException();

			if (vector.vector_class == null)
			{
				ASScript vScript = new ASScript();


				ASClass @class = new ASClass(new Token() { line = 0, ptr = 0, sourceFile = "", sourceFileFullPath = "" }, (ulong)vector.Identifier);

				ASInstance instance = new ASInstance(
					new ASMultiname()
					{
						Kind = MultinameKind.QName,
						Name = vector.Str,
						Namespace = new ASNamespace()
						{
							Kind = NamespaceKind.Package,
							Name = string.Empty
						}
					}
					);

				instance.Flags |= ClassFlags.Vector;
				instance.Flags |= ClassFlags.Sealed;
				instance.Flags |= ClassFlags.Indexer;
				@class.Instance = instance;

				var inner = Context.Vectors.FirstOrDefault(v => v.Identifier == vector.ElementType);
				if (inner != null)
				{
					MakeVectorType(inner);
					@class.Instance._element_class = inner.vector_class;

				}
				else
				{
					if (vector.ElementType == TypeKind.Any)
					{
						//说明是:*的任意类型
					}
					else
					{
						ASClass cls;
						if (Context.dictTypes.TryGetValue((ulong)vector.ElementType, out cls))
						{
							@class.Instance._element_class = cls;
						}
#if FORCOMPILER
						else if (compilingClasses != null)
						{
							@class.Instance._element_class = compilingClasses.First(c => c.Type_identifier == (ulong)vector.ElementType);
							if (@class.Instance._element_class == null)
							{
								throw new InvalidOperationException();
							}
						}
#endif

						else
						{
							throw new InvalidOperationException();
						}
					}
				}



				instance._super_class_ = Context.OBJECT;
				instance.Super = Context.OBJECT.QName;


				Context.dictTypes.Add(@class.Type_identifier, @class);
				Context.dictTypeQNames.Add(@class.QName, @class);


				vScript.Traits.Add(
					new ASTrait(@class.Token)
					{
						Kind = TraitKind.Class
					,
						Class = @class
					}
					);

				vScript.allContainers = new List<ASContainer>();


				@class.Constructor = new ASMethod(@class, @class.Token);
				@class.Constructor.IsConstructor = true;
				@class.Constructor.Body = new ASMethodBody(@class.Constructor);

				;

				instance.Constructor = new ASMethod(instance, @class.Token);
				instance.Constructor.IsConstructor = true;
				instance.Constructor.Body = new ASMethodBody(instance.Constructor);
				instance.Constructor.Body.ByteCode = Context.VECTOR.Instance.Constructor.Body.ByteCode;
				instance.Constructor.Flags = Context.VECTOR.Instance.Constructor.Flags | MethodFlags.NeedArguments;
				instance.Constructor.Name = Context.VECTOR.Instance.Constructor.Name;
				instance.Constructor.__ismethod = true;
				instance.Constructor.Parameters.AddRange(Context.VECTOR.Instance.Constructor.Parameters);
				instance.Constructor.Body.param_defaultvalues = Context.VECTOR.Instance.Constructor.Body.param_defaultvalues;
				instance.Constructor.__is_vector_method = true;


				vScript.Initializer = new ASMethod(vScript, @class.Token);
				vScript.Initializer.Body = new ASMethodBody(vScript.Initializer);


				vScript.allContainers.Add(@class);
				vScript.allContainers.Add(@class.Instance);
				vScript.allContainers.Add(@class.Constructor.Body);
				vScript.allContainers.Add(instance.Constructor.Body);
				vScript.allContainers.Add(vScript.Initializer.Body);
				vScript.allContainers.Add(vScript);


				//fixed getter
				{
					var t = Context.VECTOR.Instance.Traits.Find(t => t.QName.Name == "fixed" && t.Kind == TraitKind.Getter);
					ASTrait trait = new ASTrait(t.Token);
					trait.Kind = t.Kind;
					trait.Method = new ASMethod(instance, t.Token);
					trait.Method.Body = new ASMethodBody(trait.Method);
					trait.Method.Body.ByteCode = t.Method.Body.ByteCode;
					trait.Method.Flags = t.Method.Flags;
					trait.Method.__is_vector_method = true;
					trait.Method.Name = t.Method.Name;
					trait.Method.Parameters.AddRange(t.Method.Parameters);
					trait.Method.ReturnTypeKind = t.Method.ReturnTypeKind;
					trait.Method.ReturnType = t.Method.ReturnType;
					trait.Method.__ismethod = t.Method.__ismethod;
					trait.Method.Body.param_defaultvalues = t.Method.Body.param_defaultvalues;
					trait.Method.__return_type_class__ = t.Method.__return_type_class__;

					trait.QName = t.QName;
					trait.Method.Trait = trait;

					instance.Traits.Add(trait);
					vScript.allContainers.Add(trait.Method.Body);
				}
				//fixed setter
				{

					var t = Context.VECTOR.Instance.Traits.Find(t => t.QName.Name == "fixed" && t.Kind == TraitKind.Setter);
					ASTrait trait = new ASTrait(t.Token);
					trait.Kind = t.Kind;
					trait.Method = new ASMethod(instance, t.Token);
					trait.Method.Body = new ASMethodBody(trait.Method);
					trait.Method.Body.ByteCode = t.Method.Body.ByteCode;
					trait.Method.Flags = t.Method.Flags;
					trait.Method.__is_vector_method = true;
					trait.Method.Name = t.Method.Name;
					trait.Method.Parameters.AddRange(t.Method.Parameters);
					trait.Method.ReturnTypeKind = t.Method.ReturnTypeKind;
					trait.Method.ReturnType = t.Method.ReturnType;
					trait.Method.__ismethod = t.Method.__ismethod;
					trait.Method.Body.param_defaultvalues = t.Method.Body.param_defaultvalues;
					trait.Method.__return_type_class__ = t.Method.__return_type_class__;

					trait.QName = t.QName;
					trait.Method.Trait = trait;

					instance.Traits.Add(trait);
					vScript.allContainers.Add(trait.Method.Body);
				}
				//indexer_get
				{
					var t = Context.VECTOR.Instance.Traits.Find(t => t.QName.Name == "indexer_get" && t.Kind == TraitKind.Method);
					ASTrait trait = new ASTrait(t.Token);
					trait.Kind = t.Kind;
					trait.Method = new ASMethod(instance, t.Token);
					trait.Method.Body = new ASMethodBody(trait.Method);
					trait.Method.Body.ByteCode = t.Method.Body.ByteCode;
					trait.Method.Flags = t.Method.Flags;
					trait.Method.__is_vector_method = true;
					trait.Method.Name = t.Method.Name;
					trait.Method.Parameters.AddRange(t.Method.Parameters);

					trait.Method.ReturnTypeKind = vector.ElementType;
					trait.Method.ReturnType = null;
					trait.Method.__ismethod = t.Method.__ismethod;
					trait.Method.Body.param_defaultvalues = t.Method.Body.param_defaultvalues;
					trait.Method.__return_type_class__ = instance._element_class;
					trait.Method.Trait = trait;

					trait.QName = t.QName;

					instance.Traits.Add(trait);
					vScript.allContainers.Add(trait.Method.Body);

					instance.indexer_get = trait.Method;
				}
				//indexer_set
				{
					var t = Context.VECTOR.Instance.Traits.Find(t => t.QName.Name == "indexer_set" && t.Kind == TraitKind.Method);
					ASTrait trait = new ASTrait(t.Token);
					trait.Kind = t.Kind;
					trait.Method = new ASMethod(instance, t.Token);
					trait.Method.Body = new ASMethodBody(trait.Method);
					trait.Method.Body.ByteCode = t.Method.Body.ByteCode;
					trait.Method.Flags = t.Method.Flags;
					trait.Method.__is_vector_method = true;
					trait.Method.Name = t.Method.Name;

					trait.Method.Parameters.Add(t.Method.Parameters[0]);
					trait.Method.Parameters.Add(new ASParameter(trait.Method)
					{
						IsOptional = false,
						IsRest = false,
						Name = t.Method.Parameters[1].Name,
						TypeKind = vector.ElementType
					});

					trait.Method.ReturnTypeKind = t.Method.ReturnTypeKind;
					trait.Method.ReturnType = t.Method.ReturnType;
					trait.Method.__ismethod = t.Method.__ismethod;
					trait.Method.Body.param_defaultvalues = t.Method.Body.param_defaultvalues;
					trait.Method.__return_type_class__ = t.Method.__return_type_class__;
					trait.Method.Trait = trait;

					trait.QName = t.QName;

					instance.Traits.Add(trait);
					vScript.allContainers.Add(trait.Method.Body);

					instance.indexer_set = trait.Method;
				}
				//indexer_delete
				{
					var t = Context.VECTOR.Instance.Traits.Find(t => t.QName.Name == "indexer_delete" && t.Kind == TraitKind.Method);
					ASTrait trait = new ASTrait(t.Token);
					trait.Kind = t.Kind;
					trait.Method = new ASMethod(instance, t.Token);
					trait.Method.Body = new ASMethodBody(trait.Method);
					trait.Method.Body.ByteCode = t.Method.Body.ByteCode;
					trait.Method.Flags = t.Method.Flags;
					trait.Method.__is_vector_method = true;
					trait.Method.Name = t.Method.Name;
					trait.Method.Parameters.AddRange(t.Method.Parameters);

					trait.Method.ReturnTypeKind = t.Method.ReturnTypeKind;
					trait.Method.ReturnType = null;
					trait.Method.__ismethod = t.Method.__ismethod;
					trait.Method.Body.param_defaultvalues = t.Method.Body.param_defaultvalues;
					trait.Method.__return_type_class__ = t.Method.__return_type_class__;
					trait.Method.Trait = trait;

					trait.QName = t.QName;

					instance.Traits.Add(trait);
					vScript.allContainers.Add(trait.Method.Body);

					instance.indexer_delete = trait.Method;
				}
				//length_getter
				{
					var t = Context.VECTOR.Instance.Traits.Find(t => t.QName.Name == "length" && t.Kind == TraitKind.Getter);
					ASTrait trait = new ASTrait(t.Token);
					trait.Kind = t.Kind;
					trait.Method = new ASMethod(instance, t.Token);
					trait.Method.Body = new ASMethodBody(trait.Method);
					trait.Method.Body.ByteCode = t.Method.Body.ByteCode;
					trait.Method.Flags = t.Method.Flags;
					trait.Method.__is_vector_method = true;
					trait.Method.Name = t.Method.Name;
					trait.Method.Parameters.AddRange(t.Method.Parameters);

					trait.Method.ReturnTypeKind = t.Method.ReturnTypeKind;
					trait.Method.ReturnType = t.Method.ReturnType;
					trait.Method.__ismethod = t.Method.__ismethod;
					trait.Method.Body.param_defaultvalues = t.Method.Body.param_defaultvalues;
					trait.Method.__return_type_class__ = t.Method.__return_type_class__;
					trait.Method.Trait = trait;

					trait.QName = t.QName;

					instance.Traits.Add(trait);
					vScript.allContainers.Add(trait.Method.Body);
				}
				//length_setter
				{
					var t = Context.VECTOR.Instance.Traits.Find(t => t.QName.Name == "length" && t.Kind == TraitKind.Setter);
					ASTrait trait = new ASTrait(t.Token);
					trait.Kind = t.Kind;
					trait.Method = new ASMethod(instance, t.Token);
					trait.Method.Body = new ASMethodBody(trait.Method);
					trait.Method.Body.ByteCode = t.Method.Body.ByteCode;
					trait.Method.Flags = t.Method.Flags;
					trait.Method.__is_vector_method = true;
					trait.Method.Name = t.Method.Name;
					trait.Method.Parameters.AddRange(t.Method.Parameters);

					trait.Method.ReturnTypeKind = t.Method.ReturnTypeKind;
					trait.Method.ReturnType = t.Method.ReturnType;
					trait.Method.__ismethod = t.Method.__ismethod;
					trait.Method.Body.param_defaultvalues = t.Method.Body.param_defaultvalues;
					trait.Method.__return_type_class__ = t.Method.__return_type_class__;
					trait.Method.Trait = trait;

					trait.QName = t.QName;

					instance.Traits.Add(trait);
					vScript.allContainers.Add(trait.Method.Body);
				}
				//join
				{
					var t = Context.VECTOR.Instance.Traits.Find(t => t.QName.Name == "join" && t.Kind == TraitKind.Method);
					ASTrait trait = new ASTrait(t.Token);
					trait.Kind = t.Kind;
					trait.Method = new ASMethod(instance, t.Token);
					trait.Method.Body = new ASMethodBody(trait.Method);
					trait.Method.Body.ByteCode = t.Method.Body.ByteCode;
					trait.Method.Flags = t.Method.Flags;
					trait.Method.__is_vector_method = true;
					trait.Method.Name = t.Method.Name;
					trait.Method.Parameters.AddRange(t.Method.Parameters);

					trait.Method.ReturnTypeKind = t.Method.ReturnTypeKind;
					trait.Method.ReturnType = t.Method.ReturnType;
					trait.Method.__ismethod = t.Method.__ismethod;
					trait.Method.Body.param_defaultvalues = t.Method.Body.param_defaultvalues;
					trait.Method.__return_type_class__ = t.Method.__return_type_class__;
					trait.Method.Trait = trait;

					trait.QName = t.QName;

					instance.Traits.Add(trait);
					vScript.allContainers.Add(trait.Method.Body);
				}
				//concat
				{
					var t = Context.VECTOR.Instance.Traits.Find(t => t.QName.Name == "concat" && t.Kind == TraitKind.Method);
					ASTrait trait = new ASTrait(t.Token);
					trait.Kind = t.Kind;
					trait.Method = new ASMethod(instance, t.Token);
					trait.Method.Body = new ASMethodBody(trait.Method);
					trait.Method.Body.ByteCode = t.Method.Body.ByteCode;
					trait.Method.Flags = t.Method.Flags;
					trait.Method.__is_vector_method = true;
					trait.Method.Name = t.Method.Name;
					trait.Method.Parameters.AddRange(t.Method.Parameters);

					trait.Method.ReturnTypeKind = (TypeKind)@class.Type_identifier;
					trait.Method.ReturnType = @class.QName;
					trait.Method.__ismethod = t.Method.__ismethod;
					trait.Method.Body.param_defaultvalues = t.Method.Body.param_defaultvalues;
					trait.Method.__return_type_class__ = @class;
					trait.Method.Trait = trait;

					trait.QName = t.QName;

					instance.Traits.Add(trait);
					vScript.allContainers.Add(trait.Method.Body);

				}
				//push
				{
					var t = Context.VECTOR.Instance.Traits.Find(t => t.QName.Name == "push" && t.Kind == TraitKind.Method);
					ASTrait trait = new ASTrait(t.Token);
					trait.Kind = t.Kind;
					trait.Method = new ASMethod(instance, t.Token);
					trait.Method.Body = new ASMethodBody(trait.Method);
					trait.Method.Body.ByteCode = t.Method.Body.ByteCode;
					trait.Method.Flags = t.Method.Flags;
					trait.Method.__is_vector_method = true;
					trait.Method.Name = t.Method.Name;
					trait.Method.Parameters.AddRange(t.Method.Parameters);

					trait.Method.ReturnTypeKind = t.Method.ReturnTypeKind;
					trait.Method.ReturnType = t.Method.ReturnType;
					trait.Method.__ismethod = t.Method.__ismethod;
					trait.Method.Body.param_defaultvalues = t.Method.Body.param_defaultvalues;
					trait.Method.__return_type_class__ = t.Method.__return_type_class__;
					trait.Method.Trait = trait;

					trait.QName = t.QName;

					instance.Traits.Add(trait);
					vScript.allContainers.Add(trait.Method.Body);
				}
				//pop
				{
					var t = Context.VECTOR.Instance.Traits.Find(t => t.QName.Name == "pop" && t.Kind == TraitKind.Method);
					ASTrait trait = new ASTrait(t.Token);
					trait.Kind = t.Kind;
					trait.Method = new ASMethod(instance, t.Token);
					trait.Method.Body = new ASMethodBody(trait.Method);
					trait.Method.Body.ByteCode = t.Method.Body.ByteCode;
					trait.Method.Flags = t.Method.Flags;
					trait.Method.__is_vector_method = true;
					trait.Method.Name = t.Method.Name;
					trait.Method.Parameters.AddRange(t.Method.Parameters);

					trait.Method.ReturnTypeKind = vector.ElementType;
					trait.Method.ReturnType = null;
					trait.Method.__ismethod = t.Method.__ismethod;
					trait.Method.Body.param_defaultvalues = t.Method.Body.param_defaultvalues;
					trait.Method.__return_type_class__ = instance._element_class;
					trait.Method.Trait = trait;

					trait.QName = t.QName;

					instance.Traits.Add(trait);
					vScript.allContainers.Add(trait.Method.Body);
				}
				//unshift
				{
					var t = Context.VECTOR.Instance.Traits.Find(t => t.QName.Name == "unshift" && t.Kind == TraitKind.Method);
					ASTrait trait = new ASTrait(t.Token);
					trait.Kind = t.Kind;
					trait.Method = new ASMethod(instance, t.Token);
					trait.Method.Body = new ASMethodBody(trait.Method);
					trait.Method.Body.ByteCode = t.Method.Body.ByteCode;
					trait.Method.Flags = t.Method.Flags;
					trait.Method.__is_vector_method = true;
					trait.Method.Name = t.Method.Name;
					trait.Method.Parameters.AddRange(t.Method.Parameters);

					trait.Method.ReturnTypeKind = t.Method.ReturnTypeKind;
					trait.Method.ReturnType = t.Method.ReturnType;
					trait.Method.__ismethod = t.Method.__ismethod;
					trait.Method.Body.param_defaultvalues = t.Method.Body.param_defaultvalues;
					trait.Method.__return_type_class__ = t.Method.__return_type_class__;
					trait.Method.Trait = trait;

					trait.QName = t.QName;

					instance.Traits.Add(trait);
					vScript.allContainers.Add(trait.Method.Body);
				}

				//shift
				{
					var t = Context.VECTOR.Instance.Traits.Find(t => t.QName.Name == "shift" && t.Kind == TraitKind.Method);
					ASTrait trait = new ASTrait(t.Token);
					trait.Kind = t.Kind;
					trait.Method = new ASMethod(instance, t.Token);
					trait.Method.Body = new ASMethodBody(trait.Method);
					trait.Method.Body.ByteCode = t.Method.Body.ByteCode;
					trait.Method.Flags = t.Method.Flags;
					trait.Method.__is_vector_method = true;
					trait.Method.Name = t.Method.Name;
					trait.Method.Parameters.AddRange(t.Method.Parameters);

					trait.Method.ReturnTypeKind = vector.ElementType;
					trait.Method.ReturnType = null;
					trait.Method.__ismethod = t.Method.__ismethod;
					trait.Method.Body.param_defaultvalues = t.Method.Body.param_defaultvalues;
					trait.Method.__return_type_class__ = instance._element_class;
					trait.Method.Trait = trait;

					trait.QName = t.QName;

					instance.Traits.Add(trait);
					vScript.allContainers.Add(trait.Method.Body);
				}
				//indexOf
				{
					var t = Context.VECTOR.Instance.Traits.Find(t => t.QName.Name == "indexOf" && t.Kind == TraitKind.Method);
					ASTrait trait = new ASTrait(t.Token);
					trait.Kind = t.Kind;
					trait.Method = new ASMethod(instance, t.Token);
					trait.Method.Body = new ASMethodBody(trait.Method);
					trait.Method.Body.ByteCode = t.Method.Body.ByteCode;
					trait.Method.Flags = t.Method.Flags;
					trait.Method.__is_vector_method = true;
					trait.Method.Name = t.Method.Name;

					trait.Method.Parameters.AddRange(t.Method.Parameters);

					trait.Method.Parameters[0] = new ASParameter(trait.Method)
					{
						IsOptional = t.Method.Parameters[0].IsOptional,
						IsRest = t.Method.Parameters[0].IsRest,
						Name = t.Method.Parameters[0].Name,
						ValueKind = t.Method.Parameters[0].ValueKind,
						ValueExprIndex = t.Method.Parameters[0].ValueExprIndex,
						computeDefaultValue = t.Method.Parameters[0].computeDefaultValue,
						compute_result_index = t.Method.Parameters[0].compute_result_index,
						Type = null,
						TypeKind = vector.ElementType
					};


					trait.Method.ReturnTypeKind = t.Method.ReturnTypeKind;
					trait.Method.ReturnType = t.Method.ReturnType;
					trait.Method.__ismethod = t.Method.__ismethod;
					trait.Method.Body.param_defaultvalues = t.Method.Body.param_defaultvalues;
					trait.Method.__return_type_class__ = t.Method.__return_type_class__;
					trait.Method.Trait = trait;


					trait.QName = t.QName;

					instance.Traits.Add(trait);
					vScript.allContainers.Add(trait.Method.Body);
				}
				//lastIndexOf
				{
					var t = Context.VECTOR.Instance.Traits.Find(t => t.QName.Name == "lastIndexOf" && t.Kind == TraitKind.Method);
					ASTrait trait = new ASTrait(t.Token);
					trait.Kind = t.Kind;
					trait.Method = new ASMethod(instance, t.Token);
					trait.Method.Body = new ASMethodBody(trait.Method);
					trait.Method.Body.ByteCode = t.Method.Body.ByteCode;
					trait.Method.Flags = t.Method.Flags;
					trait.Method.__is_vector_method = true;
					trait.Method.Name = t.Method.Name;

					trait.Method.Parameters.AddRange(t.Method.Parameters);

					trait.Method.Parameters[0] = new ASParameter(trait.Method)
					{
						IsOptional = t.Method.Parameters[0].IsOptional,
						IsRest = t.Method.Parameters[0].IsRest,
						Name = t.Method.Parameters[0].Name,
						ValueKind = t.Method.Parameters[0].ValueKind,
						ValueExprIndex = t.Method.Parameters[0].ValueExprIndex,
						computeDefaultValue = t.Method.Parameters[0].computeDefaultValue,
						compute_result_index = t.Method.Parameters[0].compute_result_index,
						Type = null,
						TypeKind = vector.ElementType
					};


					trait.Method.ReturnTypeKind = t.Method.ReturnTypeKind;
					trait.Method.ReturnType = t.Method.ReturnType;
					trait.Method.__ismethod = t.Method.__ismethod;
					trait.Method.Body.param_defaultvalues = t.Method.Body.param_defaultvalues;
					trait.Method.__return_type_class__ = t.Method.__return_type_class__;
					trait.Method.Trait = trait;


					trait.QName = t.QName;

					instance.Traits.Add(trait);
					vScript.allContainers.Add(trait.Method.Body);
				}
				//removeAt
				{
					var t = Context.VECTOR.Instance.Traits.Find(t => t.QName.Name == "removeAt" && t.Kind == TraitKind.Method);
					ASTrait trait = new ASTrait(t.Token);
					trait.Kind = t.Kind;
					trait.Method = new ASMethod(instance, t.Token);
					trait.Method.Body = new ASMethodBody(trait.Method);
					trait.Method.Body.ByteCode = t.Method.Body.ByteCode;
					trait.Method.Flags = t.Method.Flags;
					trait.Method.__is_vector_method = true;
					trait.Method.Name = t.Method.Name;
					trait.Method.Parameters.AddRange(t.Method.Parameters);

					trait.Method.ReturnTypeKind = vector.ElementType;
					trait.Method.ReturnType = null;
					trait.Method.__ismethod = t.Method.__ismethod;
					trait.Method.Body.param_defaultvalues = t.Method.Body.param_defaultvalues;
					trait.Method.__return_type_class__ = instance._element_class;
					trait.Method.Trait = trait;

					trait.QName = t.QName;

					instance.Traits.Add(trait);
					vScript.allContainers.Add(trait.Method.Body);
				}
				//reverse
				{
					var t = Context.VECTOR.Instance.Traits.Find(t => t.QName.Name == "reverse" && t.Kind == TraitKind.Method);
					ASTrait trait = new ASTrait(t.Token);
					trait.Kind = t.Kind;
					trait.Method = new ASMethod(instance, t.Token);
					trait.Method.Body = new ASMethodBody(trait.Method);
					trait.Method.Body.ByteCode = t.Method.Body.ByteCode;
					trait.Method.Flags = t.Method.Flags;
					trait.Method.__is_vector_method = true;
					trait.Method.Name = t.Method.Name;
					trait.Method.Parameters.AddRange(t.Method.Parameters);

					trait.Method.ReturnTypeKind = (TypeKind)@class.Type_identifier;
					trait.Method.ReturnType = @class.QName;
					trait.Method.__ismethod = t.Method.__ismethod;
					trait.Method.Body.param_defaultvalues = t.Method.Body.param_defaultvalues;
					trait.Method.__return_type_class__ = @class;
					trait.Method.Trait = trait;

					trait.QName = t.QName;

					instance.Traits.Add(trait);
					vScript.allContainers.Add(trait.Method.Body);
				}
				//insertAt
				{
					var t = Context.VECTOR.Instance.Traits.Find(t => t.QName.Name == "insertAt" && t.Kind == TraitKind.Method);
					ASTrait trait = new ASTrait(t.Token);
					trait.Kind = t.Kind;
					trait.Method = new ASMethod(instance, t.Token);
					trait.Method.Body = new ASMethodBody(trait.Method);
					trait.Method.Body.ByteCode = t.Method.Body.ByteCode;
					trait.Method.Flags = t.Method.Flags;
					trait.Method.__is_vector_method = true;
					trait.Method.Name = t.Method.Name;

					trait.Method.Parameters.AddRange(t.Method.Parameters);

					trait.Method.Parameters[1] = new ASParameter(trait.Method)
					{
						IsOptional = t.Method.Parameters[1].IsOptional,
						IsRest = t.Method.Parameters[1].IsRest,
						Name = t.Method.Parameters[1].Name,
						ValueKind = t.Method.Parameters[1].ValueKind,
						ValueExprIndex = t.Method.Parameters[1].ValueExprIndex,
						computeDefaultValue = t.Method.Parameters[1].computeDefaultValue,
						compute_result_index = t.Method.Parameters[1].compute_result_index,
						Type = null,
						TypeKind = vector.ElementType
					};


					trait.Method.ReturnTypeKind = t.Method.ReturnTypeKind;
					trait.Method.ReturnType = t.Method.ReturnType;
					trait.Method.__ismethod = t.Method.__ismethod;
					trait.Method.Body.param_defaultvalues = t.Method.Body.param_defaultvalues;
					trait.Method.__return_type_class__ = t.Method.__return_type_class__;
					trait.Method.Trait = trait;


					trait.QName = t.QName;

					instance.Traits.Add(trait);
					vScript.allContainers.Add(trait.Method.Body);
				}

				//sort
				{
					var t = Context.VECTOR.Instance.Traits.Find(t => t.QName.Name == "sort" && t.Kind == TraitKind.Method);
					ASTrait trait = new ASTrait(t.Token);
					trait.Kind = t.Kind;
					trait.Method = new ASMethod(instance, t.Token);
					trait.Method.Body = new ASMethodBody(trait.Method);
					trait.Method.Body.ByteCode = t.Method.Body.ByteCode;
					trait.Method.Flags = t.Method.Flags;
					trait.Method.__is_vector_method = true;
					trait.Method.Name = t.Method.Name;
					trait.Method.Parameters.AddRange(t.Method.Parameters);

					trait.Method.ReturnTypeKind = (TypeKind)@class.Type_identifier;
					trait.Method.ReturnType = @class.QName;
					trait.Method.__ismethod = t.Method.__ismethod;
					trait.Method.Body.param_defaultvalues = t.Method.Body.param_defaultvalues;
					trait.Method.__return_type_class__ = @class;
					trait.Method.Trait = trait;

					trait.QName = t.QName;

					instance.Traits.Add(trait);
					vScript.allContainers.Add(trait.Method.Body);
				}
				//splice
				{
					var t = Context.VECTOR.Instance.Traits.Find(t => t.QName.Name == "splice" && t.Kind == TraitKind.Method);
					ASTrait trait = new ASTrait(t.Token);
					trait.Kind = t.Kind;
					trait.Method = new ASMethod(instance, t.Token);
					trait.Method.Body = new ASMethodBody(trait.Method);
					trait.Method.Body.ByteCode = t.Method.Body.ByteCode;
					trait.Method.Flags = t.Method.Flags;
					trait.Method.__is_vector_method = true;
					trait.Method.Name = t.Method.Name;
					trait.Method.Parameters.AddRange(t.Method.Parameters);

					trait.Method.ReturnTypeKind = (TypeKind)@class.Type_identifier;
					trait.Method.ReturnType = @class.QName;
					trait.Method.__ismethod = t.Method.__ismethod;
					trait.Method.Body.param_defaultvalues = t.Method.Body.param_defaultvalues;
					trait.Method.__return_type_class__ = @class;
					trait.Method.Trait = trait;

					trait.QName = t.QName;

					instance.Traits.Add(trait);
					vScript.allContainers.Add(trait.Method.Body);
				}
				//every
				{
					var t = Context.VECTOR.Instance.Traits.Find(t => t.QName.Name == "every" && t.Kind == TraitKind.Method);
					ASTrait trait = new ASTrait(t.Token);
					trait.Kind = t.Kind;
					trait.Method = new ASMethod(instance, t.Token);
					trait.Method.Body = new ASMethodBody(trait.Method);
					trait.Method.Body.ByteCode = t.Method.Body.ByteCode;
					trait.Method.Flags = t.Method.Flags;
					trait.Method.__is_vector_method = true;
					trait.Method.Name = t.Method.Name;

					trait.Method.Parameters.AddRange(t.Method.Parameters);


					trait.Method.ReturnTypeKind = t.Method.ReturnTypeKind;
					trait.Method.ReturnType = t.Method.ReturnType;
					trait.Method.__ismethod = t.Method.__ismethod;
					trait.Method.Body.param_defaultvalues = t.Method.Body.param_defaultvalues;
					trait.Method.__return_type_class__ = t.Method.__return_type_class__;
					trait.Method.Trait = trait;


					trait.QName = t.QName;

					instance.Traits.Add(trait);
					vScript.allContainers.Add(trait.Method.Body);
				}
				//filter
				{
					var t = Context.VECTOR.Instance.Traits.Find(t => t.QName.Name == "filter" && t.Kind == TraitKind.Method);
					ASTrait trait = new ASTrait(t.Token);
					trait.Kind = t.Kind;
					trait.Method = new ASMethod(instance, t.Token);
					trait.Method.Body = new ASMethodBody(trait.Method);
					trait.Method.Body.ByteCode = t.Method.Body.ByteCode;
					trait.Method.Flags = t.Method.Flags;
					trait.Method.__is_vector_method = true;
					trait.Method.Name = t.Method.Name;

					trait.Method.Parameters.AddRange(t.Method.Parameters);


					trait.Method.ReturnTypeKind = (TypeKind)@class.Type_identifier;
					trait.Method.ReturnType = @class.QName;
					trait.Method.__ismethod = t.Method.__ismethod;
					trait.Method.Body.param_defaultvalues = t.Method.Body.param_defaultvalues;
					trait.Method.__return_type_class__ = @class;
					trait.Method.Trait = trait;


					trait.QName = t.QName;

					instance.Traits.Add(trait);
					vScript.allContainers.Add(trait.Method.Body);
				}
				//forEach
				{
					var t = Context.VECTOR.Instance.Traits.Find(t => t.QName.Name == "forEach" && t.Kind == TraitKind.Method);
					ASTrait trait = new ASTrait(t.Token);
					trait.Kind = t.Kind;
					trait.Method = new ASMethod(instance, t.Token);
					trait.Method.Body = new ASMethodBody(trait.Method);
					trait.Method.Body.ByteCode = t.Method.Body.ByteCode;
					trait.Method.Flags = t.Method.Flags;
					trait.Method.__is_vector_method = true;
					trait.Method.Name = t.Method.Name;

					trait.Method.Parameters.AddRange(t.Method.Parameters);


					trait.Method.ReturnTypeKind = t.Method.ReturnTypeKind;
					trait.Method.ReturnType = t.Method.ReturnType;
					trait.Method.__ismethod = t.Method.__ismethod;
					trait.Method.Body.param_defaultvalues = t.Method.Body.param_defaultvalues;
					trait.Method.__return_type_class__ = t.Method.__return_type_class__;
					trait.Method.Trait = trait;


					trait.QName = t.QName;

					instance.Traits.Add(trait);
					vScript.allContainers.Add(trait.Method.Body);
				}
				//some
				{
					var t = Context.VECTOR.Instance.Traits.Find(t => t.QName.Name == "some" && t.Kind == TraitKind.Method);
					ASTrait trait = new ASTrait(t.Token);
					trait.Kind = t.Kind;
					trait.Method = new ASMethod(instance, t.Token);
					trait.Method.Body = new ASMethodBody(trait.Method);
					trait.Method.Body.ByteCode = t.Method.Body.ByteCode;
					trait.Method.Flags = t.Method.Flags;
					trait.Method.__is_vector_method = true;
					trait.Method.Name = t.Method.Name;

					trait.Method.Parameters.AddRange(t.Method.Parameters);


					trait.Method.ReturnTypeKind = t.Method.ReturnTypeKind;
					trait.Method.ReturnType = t.Method.ReturnType;
					trait.Method.__ismethod = t.Method.__ismethod;
					trait.Method.Body.param_defaultvalues = t.Method.Body.param_defaultvalues;
					trait.Method.__return_type_class__ = t.Method.__return_type_class__;
					trait.Method.Trait = trait;


					trait.QName = t.QName;

					instance.Traits.Add(trait);
					vScript.allContainers.Add(trait.Method.Body);
				}
				//map
				{
					var t = Context.VECTOR.Instance.Traits.Find(t => t.QName.Name == "map" && t.Kind == TraitKind.Method);
					ASTrait trait = new ASTrait(t.Token);
					trait.Kind = t.Kind;
					trait.Method = new ASMethod(instance, t.Token);
					trait.Method.Body = new ASMethodBody(trait.Method);
					trait.Method.Body.ByteCode = t.Method.Body.ByteCode;
					trait.Method.Flags = t.Method.Flags;
					trait.Method.__is_vector_method = true;
					trait.Method.Name = t.Method.Name;

					trait.Method.Parameters.AddRange(t.Method.Parameters);


					trait.Method.ReturnTypeKind = (TypeKind)@class.Type_identifier;
					trait.Method.ReturnType = @class.QName;
					trait.Method.__ismethod = t.Method.__ismethod;
					trait.Method.Body.param_defaultvalues = t.Method.Body.param_defaultvalues;
					trait.Method.__return_type_class__ = @class;
					trait.Method.Trait = trait;


					trait.QName = t.QName;

					instance.Traits.Add(trait);
					vScript.allContainers.Add(trait.Method.Body);
				}
				//toString
				{
					var t = Context.VECTOR.Instance.Traits.Find(t => t.QName.Name == "toString" && t.Kind == TraitKind.Method);
					ASTrait trait = new ASTrait(t.Token);
					trait.Kind = t.Kind;
					trait.Method = new ASMethod(instance, t.Token);
					trait.Method.Body = new ASMethodBody(trait.Method);
					trait.Method.Body.ByteCode = t.Method.Body.ByteCode;
					trait.Method.Flags = t.Method.Flags;
					trait.Method.__is_vector_method = true;
					trait.Method.Name = t.Method.Name;

					trait.Method.Parameters.AddRange(t.Method.Parameters);


					trait.Method.ReturnTypeKind = t.Method.ReturnTypeKind;
					trait.Method.ReturnType = t.Method.ReturnType;
					trait.Method.__ismethod = t.Method.__ismethod;
					trait.Method.Body.param_defaultvalues = t.Method.Body.param_defaultvalues;
					trait.Method.__return_type_class__ = t.Method.__return_type_class__;
					trait.Method.Trait = trait;


					trait.QName = t.QName;

					instance.Traits.Add(trait);
					vScript.allContainers.Add(trait.Method.Body);
				}
				//toLocaleString
				{
					var t = Context.VECTOR.Instance.Traits.Find(t => t.QName.Name == "toLocaleString" && t.Kind == TraitKind.Method);
					ASTrait trait = new ASTrait(t.Token);
					trait.Kind = t.Kind;
					trait.Method = new ASMethod(instance, t.Token);
					trait.Method.Body = new ASMethodBody(trait.Method);
					trait.Method.Body.ByteCode = t.Method.Body.ByteCode;
					trait.Method.Flags = t.Method.Flags;
					trait.Method.__is_vector_method = true;
					trait.Method.Name = "toString"; //和toString通用一个

					trait.Method.Parameters.AddRange(t.Method.Parameters);


					trait.Method.ReturnTypeKind = t.Method.ReturnTypeKind;
					trait.Method.ReturnType = t.Method.ReturnType;
					trait.Method.__ismethod = t.Method.__ismethod;
					trait.Method.Body.param_defaultvalues = t.Method.Body.param_defaultvalues;
					trait.Method.__return_type_class__ = t.Method.__return_type_class__;
					trait.Method.Trait = trait;


					trait.QName = t.QName;

					instance.Traits.Add(trait);
					vScript.allContainers.Add(trait.Method.Body);
				}


				ComputeLayout(@class, Context.global_swc);
				ComputeCodeScope(vScript, Context.global_swc.NamespaceSets);
				@class.Instance._link_codescope.TypeLayout = new TypeLayout() { ASType = @class };




				ComputeVTable(vScript);



				vector.vector_class = @class;
				vector.vScript = vScript;
			}

		}



		//int cache_error_instance_ptr;
		internal int cache_ERROR_NAME;
		int cache_STACKOVERFLOW_STR;

		internal void RaiseStackOverflow(ref ReceiveError error)
		{

			error.raised = true;


			RtHeapBase _temp = null;
			int errPtr = Context.GC.AllocInstance(Context.ERROR.Instance, out _temp);
			if (errPtr == 0)
			{
				error.error.setFault();
			}
			else
			{

				NaNBoxing errName = new NaNBoxing();
				errName.SetHeapPtr(cache_ERROR_NAME, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				((RtInstance)_temp).SetSlot(errName, 1, this);

				RtHeapBase error_instance = _temp;
				RtInstance payloadInstance = (RtInstance)error_instance;

				NaNBoxing naNBoxing = new NaNBoxing();
				naNBoxing.SetHeapPtr(cache_STACKOVERFLOW_STR, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				payloadInstance.SetSlot(naNBoxing, 0, this);


				error.error.SetHeapPtr(errPtr, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.NONE);
			}



		}

		internal void RaiseFault(ref ReceiveError error)
		{
			error.raised = true;
			error.error.setFault();
		}

		/// <summary>
		/// 安全地创建字符串，优先使用LocalString，失败时回退到堆分配
		/// </summary>
		/// <param name="str">要创建的字符串（不能为null）</param>
		/// <param name="result">创建的NaNBoxing结果</param>
		/// <param name="error">错误信息</param>
		/// <returns>是否成功创建</returns>
		public bool TryCreateStringValue(ReadOnlySpan<char> str, out NaNBoxing result, ref ReceiveError error)
		{
			Debug.Assert(str != null, "String cannot be null - use SetNull() for null values");

			result = default;

			// 首先尝试创建LocalString
			if (NaNBoxing.TryCreateLocalString(str, out result))
			{
				return true;
			}
			
			// 回退到堆分配
			int strptr = Context.GC.AllocString(new string(str));
			if (strptr == 0)
			{
				RaiseOutOfMemory(ref error);
				return false;
			}

			result.SetHeapPtr(strptr, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
			return true;
		}

		int cache_OUTOFMEMORY_STR;
		internal void RaiseOutOfMemory(ref ReceiveError error)
		{

			error.raised = true;

			RtHeapBase _temp = null;
			int errPtr = Context.GC.AllocInstance(Context.ERROR.Instance, out _temp);
			if (errPtr == 0)
			{
				error.error.setFault();
			}
			else
			{

				NaNBoxing errName = new NaNBoxing();
				errName.SetHeapPtr(cache_ERROR_NAME, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				((RtInstance)_temp).SetSlot(errName, 1, this);

				RtHeapBase error_instance = _temp;
				RtInstance payloadInstance = (RtInstance)error_instance;

				NaNBoxing naNBoxing = new NaNBoxing();
				naNBoxing.SetHeapPtr(cache_OUTOFMEMORY_STR, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				payloadInstance.SetSlot(naNBoxing, 0, this);


				error.error.SetHeapPtr(errPtr, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.NONE);
			}


		}

		private ASMultiname buildin_as_methodclosure = new ASMultiname() { Kind = MultinameKind.QName, Name = "MethodClosure", Namespace = new ASNamespace() { Name = "builtin.as" } };





		public void RaiseError(ref ReceiveError error, string message)
		{
			error.raised = true;


			RtHeapBase _temp = null;
			int errPtr = Context.GC.AllocInstance(Context.ERROR.Instance, out _temp);
			if (errPtr == 0)
			{
				error.error.setFault();
			}
			else
			{

				NaNBoxing errName = new NaNBoxing();
				errName.SetHeapPtr(cache_ERROR_NAME, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				((RtInstance)_temp).SetSlot(errName, 1, this);

				RtHeapBase error_instance = _temp;
				RtInstance payloadInstance = (RtInstance)error_instance;


				int messagePtr = Context.GC.AllocString(message);
				if (messagePtr != 0)
				{
					NaNBoxing naNBoxing = new NaNBoxing();
					naNBoxing.SetHeapPtr(messagePtr, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
					payloadInstance.SetSlot(naNBoxing, 0, this);
				}
				else
				{
					NaNBoxing naNBoxing = new NaNBoxing();
					naNBoxing.SetHeapPtr(cache_OUTOFMEMORY_STR, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
					payloadInstance.SetSlot(naNBoxing, 0, this);
				}

				error.error.SetHeapPtr(errPtr, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.NONE);
			}
		}
		internal int cache_Eval_ERROR_NAME;

		internal int cache_TYPE_ERROR_NAME;
		internal void RaiseTypeError(ref ReceiveError error, NaNBoxing value, TypeKind toType)
		{

			error.raised = true;
			RtHeapBase _temp = null;
			int errPtr = Context.GC.AllocInstance(Context.TYPE_ERROR.Instance, out _temp);
			if (errPtr == 0)
			{
				error.error.setFault();
			}
			else
			{
				NaNBoxing errName = new NaNBoxing();
				errName.SetHeapPtr(cache_TYPE_ERROR_NAME, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				((RtInstance)_temp).SetSlot(errName, 1, this);

				RtHeapBase error_instance = _temp;
				RtInstance payloadInstance = (RtInstance)error_instance;

				int messagePtr = Context.GC.AllocString($"Type Coercion failed: can not convert {value.ToDebugString(this)} to {toType.ToDebugString(this)}.");
				if (messagePtr != 0)
				{
					NaNBoxing naNBoxing = new NaNBoxing();
					naNBoxing.SetHeapPtr(messagePtr, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
					payloadInstance.SetSlot(naNBoxing, 0, this);
				}
				else
				{
					NaNBoxing naNBoxing = new NaNBoxing();
					naNBoxing.SetHeapPtr(cache_OUTOFMEMORY_STR, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
					payloadInstance.SetSlot(naNBoxing, 0, this);
				}

				error.error.SetHeapPtr(errPtr, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.NONE);
			}


		}

		internal void RaiseTypeError_Ambiguous(ref ReceiveError error, ReadOnlySpan<char> name)
		{

			error.raised = true;
			RtHeapBase _temp;
			int errPtr = Context.GC.AllocInstance(Context.TYPE_ERROR.Instance, out _temp);
			if (errPtr == 0)
			{
				error.error.setFault();
			}
			else
			{
				NaNBoxing errName = new NaNBoxing();
				errName.SetHeapPtr(cache_TYPE_ERROR_NAME, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				((RtInstance)_temp).SetSlot(errName, 1, this);

				RtHeapBase error_instance = _temp;
				RtInstance payloadInstance = (RtInstance)error_instance;

				int messagePtr = Context.GC.AllocString($"{name} is ambiguous; Found more than one matching binding.");
				if (messagePtr != 0)
				{
					NaNBoxing naNBoxing = new NaNBoxing();
					naNBoxing.SetHeapPtr(messagePtr, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
					payloadInstance.SetSlot(naNBoxing, 0, this);
				}
				else
				{
					NaNBoxing naNBoxing = new NaNBoxing();
					naNBoxing.SetHeapPtr(cache_OUTOFMEMORY_STR, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
					payloadInstance.SetSlot(naNBoxing, 0, this);
				}

				error.error.SetHeapPtr(errPtr, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.NONE);
			}
		}


		internal void RaiseTypeError_ConvertToPrimitive(ref ReceiveError error, NaNBoxing value)
		{
			error.raised = true;
			RtHeapBase _temp;
			int errPtr = Context.GC.AllocInstance(Context.TYPE_ERROR.Instance, out _temp);
			if (errPtr == 0)
			{
				error.error.setFault();
			}
			else
			{
				NaNBoxing errName = new NaNBoxing();
				errName.SetHeapPtr(cache_TYPE_ERROR_NAME, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				((RtInstance)_temp).SetSlot(errName, 1, this);

				RtHeapBase error_instance = _temp;
				RtInstance payloadInstance = (RtInstance)error_instance;

				int messagePtr = Context.GC.AllocString($"Cannot convert {value.ToDebugString(this)} to primitive.");
				if (messagePtr != 0)
				{
					NaNBoxing naNBoxing = new NaNBoxing();
					naNBoxing.SetHeapPtr(messagePtr, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
					payloadInstance.SetSlot(naNBoxing, 0, this);
				}
				else
				{
					NaNBoxing naNBoxing = new NaNBoxing();
					naNBoxing.SetHeapPtr(cache_OUTOFMEMORY_STR, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
					payloadInstance.SetSlot(naNBoxing, 0, this);
				}

				error.error.SetHeapPtr(errPtr, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.NONE);
			}
		}


		int cache_ILLEGALOPERATION_ERROR_NAME;
		internal void RaiseIllegaloperationError(ref ReceiveError error, string methodkey)
		{
			error.raised = true;
			RtHeapBase _temp;
			int errPtr = Context.GC.AllocInstance(Context.ILLEGALOPERATION_ERROR.Instance, out _temp);
			if (errPtr == 0)
			{
				error.error.setFault();
			}
			else
			{
				NaNBoxing errName = new NaNBoxing();
				errName.SetHeapPtr(cache_ILLEGALOPERATION_ERROR_NAME, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				((RtInstance)_temp).SetSlot(errName, 1, this);

				RtHeapBase error_instance = _temp;
				RtInstance payloadInstance = (RtInstance)error_instance;

				int messagePtr = Context.GC.AllocString($"native function {methodkey} is missing.");
				if (messagePtr != 0)
				{
					NaNBoxing naNBoxing = new NaNBoxing();
					naNBoxing.SetHeapPtr(messagePtr, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
					payloadInstance.SetSlot(naNBoxing, 0, this);
				}
				else
				{
					NaNBoxing naNBoxing = new NaNBoxing();
					naNBoxing.SetHeapPtr(cache_OUTOFMEMORY_STR, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
					payloadInstance.SetSlot(naNBoxing, 0, this);
				}

				error.error.SetHeapPtr(errPtr, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.NONE);
			}
		}


		int cache_CANNOT_ACCESS_NULL;
		public void RaiseTypeError_AccessNull(ref ReceiveError error)
		{

			error.raised = true;
			RtHeapBase _temp;
			int errPtr = Context.GC.AllocInstance(Context.TYPE_ERROR.Instance, out _temp);
			if (errPtr == 0)
			{
				error.error.setFault();
			}
			else
			{
				NaNBoxing errName = new NaNBoxing();
				errName.SetHeapPtr(cache_TYPE_ERROR_NAME, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				((RtInstance)_temp).SetSlot(errName, 1, this);

				RtHeapBase error_instance = _temp;
				RtInstance payloadInstance = (RtInstance)error_instance;

				NaNBoxing naNBoxing = new NaNBoxing();
				naNBoxing.SetHeapPtr(cache_CANNOT_ACCESS_NULL, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				payloadInstance.SetSlot(naNBoxing, 0, this);

				error.error.SetHeapPtr(errPtr, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.NONE);
			}
		}

		/// <summary>
		/// 比较Shape属性名，支持LocalString和HeapPtr两种存储方式
		/// </summary>
		/// <param name="shapeName">Shape中存储的属性名</param>
		/// <param name="searchName">要比较的属性名</param>
		/// <returns>0表示相等，非0表示不等</returns>
		private int CompareShapePropertyName(NaNBoxing shapeName, ReadOnlySpan<char> searchName)
		{
			if (shapeName.ValueType == NaNBoxing.BoxType.LocalString)
			{
				// LocalString比较
				Span<char> chars = stackalloc char[16];
				int charCount = shapeName.GetLocalStringChars(chars);
				if (charCount < 0) return -1; // 解码失败

				return searchName.CompareTo(chars.Slice(0, charCount), StringComparison.Ordinal);
			}
			else if (shapeName.ValueType == NaNBoxing.BoxType.HeapPtr && shapeName.HeapPtr != 0)
			{
				// HeapPtr字符串比较
				string str = ((RtString)Context.GC.Heap[shapeName.HeapPtr]).Str;
				return searchName.CompareTo(str.AsSpan(), StringComparison.Ordinal);
			}
			else
			{
				// 空属性名或其他类型，认为不匹配
				return -1;
			}
		}

		private int CompareShapePropertyName(NaNBoxing shapeName1, NaNBoxing shapeName2)
		{
			// 处理相同引用的情况
			if (shapeName1.Raw == shapeName2.Raw)
			{
				return 0;
			}

			// 处理 LocalString vs LocalString
			if (shapeName1.ValueType == NaNBoxing.BoxType.LocalString &&
				shapeName2.ValueType == NaNBoxing.BoxType.LocalString)
			{
				// 使用高效的字符比较，避免字符串分配
				Span<char> chars1 = stackalloc char[16];
				Span<char> chars2 = stackalloc char[16];

				int charCount1 = shapeName1.GetLocalStringChars(chars1);
				int charCount2 = shapeName2.GetLocalStringChars(chars2);

				if (charCount1 < 0 || charCount2 < 0)
				{
					// 解码失败，按类型比较
					return charCount1.CompareTo(charCount2);
				}

				ReadOnlySpan<char> c1 = chars1;
				ReadOnlySpan<char> c2 = chars2;

				return c1.Slice(0, charCount1).CompareTo(c2.Slice(0, charCount2), StringComparison.Ordinal);
			}

			// 处理 LocalString vs HeapPtr
			if (shapeName1.ValueType == NaNBoxing.BoxType.LocalString &&
				shapeName2.ValueType == NaNBoxing.BoxType.HeapPtr)
			{
				if (shapeName2.HeapPtr == 0) return 1; // LocalString > null

				Span<char> chars1 = stackalloc char[16];
				int charCount1 = shapeName1.GetLocalStringChars(chars1);
				if (charCount1 < 0) return -1; // 解码失败


				ReadOnlySpan<char> c1 = chars1;

				string str2 = ((RtString)Context.GC.Heap[shapeName2.HeapPtr]).Str;
				return c1.Slice(0, charCount1).CompareTo(str2.AsSpan(), StringComparison.Ordinal);
			}

			// 处理 HeapPtr vs LocalString
			if (shapeName1.ValueType == NaNBoxing.BoxType.HeapPtr &&
				shapeName2.ValueType == NaNBoxing.BoxType.LocalString)
			{
				if (shapeName1.HeapPtr == 0) return -1; // null < LocalString

				Span<char> chars2 = stackalloc char[16];
				int charCount2 = shapeName2.GetLocalStringChars(chars2);
				if (charCount2 < 0) return 1; // 解码失败

				ReadOnlySpan<char> c2 = chars2;

				string str1 = ((RtString)Context.GC.Heap[shapeName1.HeapPtr]).Str;
				return str1.AsSpan().CompareTo(c2.Slice(0, charCount2), StringComparison.Ordinal);
			}

			// 处理 HeapPtr vs HeapPtr
			if (shapeName1.ValueType == NaNBoxing.BoxType.HeapPtr &&
				shapeName2.ValueType == NaNBoxing.BoxType.HeapPtr)
			{
				// 处理null情况
				if (shapeName1.HeapPtr == 0 && shapeName2.HeapPtr == 0) return 0;
				if (shapeName1.HeapPtr == 0) return -1;
				if (shapeName2.HeapPtr == 0) return 1;

				// 比较堆字符串，使用AsSpan避免额外分配
				string str1 = ((RtString)Context.GC.Heap[shapeName1.HeapPtr]).Str;
				string str2 = ((RtString)Context.GC.Heap[shapeName2.HeapPtr]).Str;
				return str1.AsSpan().CompareTo(str2.AsSpan(), StringComparison.Ordinal);
			}

			// 处理其他类型组合 - 按类型优先级排序
			// LocalString < HeapPtr < 其他类型
			int priority1 = GetShapeNameTypePriority(shapeName1.ValueType);
			int priority2 = GetShapeNameTypePriority(shapeName2.ValueType);

			return priority1.CompareTo(priority2);
		}

		/// <summary>
		/// 获取Shape属性名类型的优先级，用于排序
		/// </summary>
		private int GetShapeNameTypePriority(NaNBoxing.BoxType type)
		{
			return type switch
			{
				NaNBoxing.BoxType.LocalString => 1,
				NaNBoxing.BoxType.HeapPtr => 2,
				_ => 3
			};
		}

		public void ForceGC()
		{
			ReceiveError err = new ReceiveError();
			Context.GC.ForceGC(ref err);
		}

		internal int EMPTY_STR;
		internal int TRUE_STR;
		internal int FALSE_STR;

		internal int NAN_STR;
		internal int POSITIVEINF_STR;
		internal int NEGATIVEINF_STR;
		internal int ZERO_STR;
		internal int CALLEE_STR;
		internal int FUNCTION_TOSTRING_STR;
		internal int OBJECT_FUNCTION_STR;

		internal int CONSTRUCTOR_STR;
		internal int TOSTRING_STR;
		internal int VALUEOF_STR;


		////boolean、function、number、object、string ,undefined。
		internal int TYPEOF_boolean_STR;
		internal int TYPEOF_function_STR;
		internal int TYPEOF_number_STR;
		internal int TYPEOF_object_STR;
		internal int TYPEOF_string_STR;
		internal int TYPEOF_undefined_STR;

		internal int NULL_STR;


		private void CreateObjectProto(ref ReceiveError error)
		{
			var proto_ptr = ((RtScriptClass)Context.GC.Heap[Context.OBJECT.__instance_index__]).PROTO__PTR;
			var proto = Context.GC.Heap[proto_ptr];

			// tostring
			{
				TOSTRING_STR = Context.GC.AllocString("toString");
				if (TOSTRING_STR == 0)
				{
					throw new LoaderException("TOSTRING_STR alloc failed");
				}
				Context.GC.Root.Add(Context.GC.Heap[TOSTRING_STR]);

				ASMethod tostring = new ASMethod(Context.OBJECT._link_codescope.Parent.Container, Context.OBJECT.Token);
				tostring.ReturnTypeKind = TypeKind.String;
				tostring.Flags = MethodFlags.Native;
				tostring.Name = "toString";
				tostring.Body = new ASMethodBody(tostring);
				tostring.Body.ByteCode = new byte[12];
				tostring.Body._link_codescope = new CodeScope() { Members = new List<ScopeMember>(), Parent = Context.OBJECT._link_codescope.Parent };
				tostring.IsAnonymous = true;
				tostring.__is_buildin_proto = true;

				int tostring_ptr = Context.GC.AllocClosure(tostring);
				if (tostring_ptr == 0)
				{
					throw new LoaderException("Object proto : toString alloc failed");
				}

				((RtClosure)Context.GC.Heap[tostring_ptr]).ScopePtr = ((ASScript)Context.OBJECT._link_codescope.Parent.Container).__global_index__;

				NaNBoxing v = default; v.SetHeapPtr(tostring_ptr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);

				NaNBoxing v_str = default; v_str.SetHeapPtr(TOSTRING_STR, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				CreateDynamic(ref error, proto, v_str, v, false, false, false);
				if (error.raised) return;
			}

			//valueOf
			{
				VALUEOF_STR = Context.GC.AllocString("valueOf");
				if (VALUEOF_STR == 0)
				{
					throw new LoaderException("VALUEOF_STR alloc failed");
				}
				Context.GC.Root.Add(Context.GC.Heap[VALUEOF_STR]);

				ASMethod valueof = new ASMethod(Context.OBJECT._link_codescope.Parent.Container, Context.OBJECT.Token);
				valueof.ReturnTypeKind = TypeKind.Any;
				valueof.Flags = MethodFlags.Native;
				valueof.Name = "valueOf";
				valueof.Body = new ASMethodBody(valueof);
				valueof.Body.ByteCode = new byte[12];
				valueof.Body._link_codescope = new CodeScope() { Members = new List<ScopeMember>(), Parent = Context.OBJECT._link_codescope.Parent };
				valueof.IsAnonymous = true;
				valueof.__is_buildin_proto = true;

				int valueof_ptr = Context.GC.AllocClosure(valueof);
				if (valueof_ptr == 0)
				{
					throw new LoaderException("Object proto : valueOf alloc failed");
				}

				((RtClosure)Context.GC.Heap[valueof_ptr]).ScopePtr = ((ASScript)Context.OBJECT._link_codescope.Parent.Container).__global_index__;

				NaNBoxing v = default; v.SetHeapPtr(valueof_ptr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);
				NaNBoxing v_str = default; v_str.SetHeapPtr(VALUEOF_STR, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				CreateDynamic(ref error, proto, v_str, v, false, false, false);
				if (error.raised) return;
			}

			//hasOwnProperty
			{
				var hasOwnProperty = Context.GC.AllocString("hasOwnProperty");
				if (hasOwnProperty == 0)
				{
					throw new LoaderException("hasOwnProperty alloc failed");
				}
				Context.GC.Root.Add(Context.GC.Heap[hasOwnProperty]);

				ASMethod m = new ASMethod(Context.OBJECT._link_codescope.Parent.Container, Context.OBJECT.Token);
				m.ReturnTypeKind = TypeKind.Boolean;
				m.Flags = MethodFlags.Native;
				m.Name = "hasOwnProperty";
				m.Body = new ASMethodBody(m);
				m.Body.ByteCode = new byte[12];
				m.Body._link_codescope = new CodeScope() { Members = new List<ScopeMember>(), Parent = Context.OBJECT._link_codescope.Parent };
				m.IsAnonymous = true;
				m.Parameters.Add(new ASParameter(m) { IsOptional = false, Name = "name", IsRest = false, Type = Context.STRING.QName, TypeKind = TypeKind.String });
				m.Body._link_codescope.Members.Add(new ScopeMember(m.Body, null)
				{
					Kind = ScopeMemberKind.Parameter,
					PName = "name",
					Type = m.Parameters[0].Type,
					TypeKind = TypeKind.String,
					__rt_type_class__ = Context.STRING,
					_rt_type_flag = Context.STRING.Instance.Flags
				});
				m.__is_hasOwnProperty = true;
				m.__is_buildin_proto = true;

				int hasownproperty_ptr = Context.GC.AllocClosure(m);
				if (hasownproperty_ptr == 0)
				{
					throw new LoaderException("Object proto: hasOwnProperty alloc failed");
				}

				((RtClosure)Context.GC.Heap[hasownproperty_ptr]).ScopePtr = ((ASScript)Context.OBJECT._link_codescope.Parent.Container).__global_index__;
				((RtClosure)Context.GC.Heap[hasownproperty_ptr]).Set_PROTOTYPE(-1, this); //设置prototype为undefined

				NaNBoxing v = default; v.SetHeapPtr(hasownproperty_ptr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);
				NaNBoxing v_str = default; v_str.SetHeapPtr(hasOwnProperty, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				CreateDynamic(ref error, proto, v_str, v, false, false, false);
				if (error.raised) return;


			}

			//isPrototypeOf
			{
				var isPrototypeOf = Context.GC.AllocString("isPrototypeOf");
				if (isPrototypeOf == 0)
				{
					throw new LoaderException("isPrototypeOf alloc failed");
				}
				Context.GC.Root.Add(Context.GC.Heap[isPrototypeOf]);

				ASMethod m = new ASMethod(Context.OBJECT._link_codescope.Parent.Container, Context.OBJECT.Token);
				m.ReturnTypeKind = TypeKind.Boolean;
				m.Flags = MethodFlags.Native;
				m.Name = "isPrototypeOf";
				m.Body = new ASMethodBody(m);
				m.Body.ByteCode = new byte[12];
				m.Body._link_codescope = new CodeScope() { Members = new List<ScopeMember>(), Parent = Context.OBJECT._link_codescope.Parent };
				m.IsAnonymous = true;
				m.Parameters.Add(new ASParameter(m) { IsOptional = false, Name = "theClass", IsRest = false, Type = Context.OBJECT.QName, TypeKind = TypeKind.Object });
				m.Body._link_codescope.Members.Add(new ScopeMember(m.Body, null)
				{
					Kind = ScopeMemberKind.Parameter,
					PName = "theClass",
					Type = m.Parameters[0].Type,
					TypeKind = TypeKind.Object,
					__rt_type_class__ = Context.OBJECT,
					_rt_type_flag = Context.OBJECT.Instance.Flags
				});
				m.__is_hasOwnProperty = true;
				m.__is_buildin_proto = true;

				int isprototypeof_ptr = Context.GC.AllocClosure(m);
				if (isprototypeof_ptr == 0)
				{
					throw new LoaderException("Object proto: isPrototypeOf alloc failed");
				}

				((RtClosure)Context.GC.Heap[isprototypeof_ptr]).ScopePtr = ((ASScript)Context.OBJECT._link_codescope.Parent.Container).__global_index__;
				((RtClosure)Context.GC.Heap[isprototypeof_ptr]).Set_PROTOTYPE(-1, this); //设置prototype为undefined

				NaNBoxing v = default; v.SetHeapPtr(isprototypeof_ptr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);
				NaNBoxing v_str = default; v_str.SetHeapPtr(isPrototypeOf, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				CreateDynamic(ref error, proto, v_str, v, false, false, false);
				if (error.raised) return;
			}

		}

		private void CreateStringProto(ref ReceiveError error)
		{
#if FORCOMPILER
			if (IsComputeConstExpr)
			{
				return;
			}
#endif
			var proto_ptr = ((RtScriptClass)Context.GC.Heap[Context.STRING.__instance_index__]).PROTO__PTR;
			var proto = Context.GC.Heap[proto_ptr];

			// tostring
			{
				ASMethod tostring = new ASMethod(Context.STRING._link_codescope.Parent.Container, Context.STRING.Token);
				tostring.ReturnTypeKind = TypeKind.String;
				tostring.Flags = MethodFlags.Native;
				tostring.Name = "toString";
				tostring.Body = new ASMethodBody(tostring);
				tostring.Body.ByteCode = new byte[12];
				tostring.Body._link_codescope = new CodeScope() { Members = new List<ScopeMember>(), Parent = Context.STRING._link_codescope.Parent };
				tostring.IsAnonymous = true;
				tostring.__is_buildin_proto = true;

				int tostring_ptr = Context.GC.AllocClosure(tostring);
				if (tostring_ptr == 0)
				{
					throw new LoaderException("String proto : toString alloc failed");
				}

				((RtClosure)Context.GC.Heap[tostring_ptr]).ScopePtr = ((ASScript)Context.STRING._link_codescope.Parent.Container).__global_index__;
				((RtClosure)Context.GC.Heap[tostring_ptr]).Set_PROTOTYPE(-1, this);

				NaNBoxing v = default; v.SetHeapPtr(tostring_ptr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);

				NaNBoxing v_str = default; v_str.SetHeapPtr(TOSTRING_STR, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				CreateDynamic(ref error, proto, v_str, v, false, false, false);
				if (error.raised) return;
			}
			//concat
			{
				var concat_str = Context.GC.AllocString("concat");
				if (concat_str == 0)
				{
					throw new LoaderException("concat_str alloc failed");
				}
				Context.GC.Root.Add(Context.GC.Heap[concat_str]);


				ASMethod concat = new ASMethod(Context.STRING._link_codescope.Parent.Container, Context.STRING.Token);
				concat.ReturnTypeKind = TypeKind.String;
				concat.Flags = MethodFlags.Native | MethodFlags.NeedRest;
				concat.Name = "concat";
				concat.Body = new ASMethodBody(concat);
				concat.Body.ByteCode = new byte[12];
				concat.Body._link_codescope = new CodeScope() { Members = new List<ScopeMember>(), Parent = Context.STRING._link_codescope.Parent };
				concat.IsAnonymous = true;

				concat.Parameters.Add(new ASParameter(concat) { IsOptional = false, Name = "args", IsRest = true, Type = null, TypeKind = TypeKind.Any });
				concat.Body._link_codescope.Members.Add(new ScopeMember(concat.Body, null)
				{
					Kind = ScopeMemberKind.Parameter,
					PName = "args",
					Type = null,
					TypeKind = TypeKind.Any,
					__rt_type_class__ = null,
					_rt_type_flag = ClassFlags.None
				});


				concat.__is_buildin_proto = true;

				int concat_ptr = Context.GC.AllocClosure(concat);
				if (concat_ptr == 0)
				{
					throw new LoaderException("String proto : concat alloc failed");
				}

				((RtClosure)Context.GC.Heap[concat_ptr]).ScopePtr = ((ASScript)Context.STRING._link_codescope.Parent.Container).__global_index__;
				((RtClosure)Context.GC.Heap[concat_ptr]).Set_PROTOTYPE(-1, this);

				NaNBoxing v = default; v.SetHeapPtr(concat_ptr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);

				NaNBoxing v_str = default; v_str.SetHeapPtr(concat_str, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				CreateDynamic(ref error, proto, v_str, v, false, false, false);

				if (error.raised) return;

			}

			//charAt
			{
				var name_str = Context.GC.AllocString("charAt");
				if (name_str == 0)
				{
					throw new LoaderException("charAt_str alloc failed");
				}
				Context.GC.Root.Add(Context.GC.Heap[name_str]);

				var template = Context.STRING.Instance.Traits.First(t => t.QName.Name == "charAt").Method;

				ASMethod m = new ASMethod(Context.STRING._link_codescope.Parent.Container, Context.STRING.Token);
				m.ReturnTypeKind = TypeKind.String;
				m.Flags = template.Flags;
				m.Name = template.Name;
				m.Body = new ASMethodBody(m);
				m.Body.ByteCode = (byte[])template.Body.ByteCode.Clone();
				m.Body.param_defaultvalues = (byte[])template.Body.param_defaultvalues.Clone();
				m.Body._link_codescope = template.Body._link_codescope;
				m.IsAnonymous = true;

				m.Parameters.AddRange(template.Parameters);

				m.__is_buildin_proto = true;

				int method_ptr = Context.GC.AllocClosure(m);
				if (method_ptr == 0)
				{
					throw new LoaderException("String proto : charAt alloc failed");
				}

				((RtClosure)Context.GC.Heap[method_ptr]).ScopePtr = ((ASScript)Context.STRING._link_codescope.Parent.Container).__global_index__;
				((RtClosure)Context.GC.Heap[method_ptr]).Set_PROTOTYPE(-1, this);

				NaNBoxing v = default; v.SetHeapPtr(method_ptr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);

				NaNBoxing v_str = default; v_str.SetHeapPtr(name_str, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				CreateDynamic(ref error, proto, v_str, v, false, false, false);
				if (error.raised) return;
			}

			//charCodeAt
			{
				var name_str = Context.GC.AllocString("charCodeAt");
				if (name_str == 0)
				{
					throw new LoaderException("charCodeAt_str alloc failed");
				}
				Context.GC.Root.Add(Context.GC.Heap[name_str]);

				var template = Context.STRING.Instance.Traits.First(t => t.QName.Name == "charCodeAt").Method;

				ASMethod m = new ASMethod(Context.STRING._link_codescope.Parent.Container, Context.STRING.Token);
				m.ReturnTypeKind = TypeKind.String;
				m.Flags = template.Flags;
				m.Name = template.Name;
				m.Body = new ASMethodBody(m);
				m.Body.ByteCode = (byte[])template.Body.ByteCode.Clone();
				m.Body.param_defaultvalues = (byte[])template.Body.param_defaultvalues.Clone();
				m.Body._link_codescope = template.Body._link_codescope;
				m.IsAnonymous = true;

				m.Parameters.AddRange(template.Parameters);

				m.__is_buildin_proto = true;

				int method_ptr = Context.GC.AllocClosure(m);
				if (method_ptr == 0)
				{
					throw new LoaderException("String proto : charCodeAt alloc failed");
				}

				((RtClosure)Context.GC.Heap[method_ptr]).ScopePtr = ((ASScript)Context.STRING._link_codescope.Parent.Container).__global_index__;
				((RtClosure)Context.GC.Heap[method_ptr]).Set_PROTOTYPE(-1, this);

				NaNBoxing v = default; v.SetHeapPtr(method_ptr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);

				NaNBoxing v_str = default; v_str.SetHeapPtr(name_str, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				CreateDynamic(ref error, proto, v_str, v, false, false, false);
				if (error.raised) return;
			}
			//indexOf
			{
				var name_str = Context.GC.AllocString("indexOf");
				if (name_str == 0)
				{
					throw new LoaderException("indexOf_str alloc failed");
				}
				Context.GC.Root.Add(Context.GC.Heap[name_str]);

				var template = Context.STRING.Instance.Traits.First(t => t.QName.Name == "indexOf").Method;

				ASMethod m = new ASMethod(Context.STRING._link_codescope.Parent.Container, Context.STRING.Token);
				m.ReturnTypeKind = TypeKind.String;
				m.Flags = template.Flags;
				m.Name = template.Name;
				m.Body = new ASMethodBody(m);
				m.Body.ByteCode = (byte[])template.Body.ByteCode.Clone();
				m.Body.param_defaultvalues = (byte[])template.Body.param_defaultvalues.Clone();
				m.Body._link_codescope = template.Body._link_codescope;
				m.IsAnonymous = true;

				m.Parameters.AddRange(template.Parameters);

				m.__is_buildin_proto = true;

				int method_ptr = Context.GC.AllocClosure(m);
				if (method_ptr == 0)
				{
					throw new LoaderException("String proto : indexOf alloc failed");
				}

				((RtClosure)Context.GC.Heap[method_ptr]).ScopePtr = ((ASScript)Context.STRING._link_codescope.Parent.Container).__global_index__;
				((RtClosure)Context.GC.Heap[method_ptr]).Set_PROTOTYPE(-1, this);

				NaNBoxing v = default; v.SetHeapPtr(method_ptr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);

				NaNBoxing v_str = default; v_str.SetHeapPtr(name_str, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				CreateDynamic(ref error, proto, v_str, v, false, false, false);
				if (error.raised) return;
			}
			//lastIndexOf
			{
				var name_str = Context.GC.AllocString("lastIndexOf");
				if (name_str == 0)
				{
					throw new LoaderException("lastIndexOf_str alloc failed");
				}
				Context.GC.Root.Add(Context.GC.Heap[name_str]);

				var template = Context.STRING.Instance.Traits.First(t => t.QName.Name == "lastIndexOf").Method;

				ASMethod m = new ASMethod(Context.STRING._link_codescope.Parent.Container, Context.STRING.Token);
				m.ReturnTypeKind = TypeKind.String;
				m.Flags = template.Flags;
				m.Name = template.Name;
				m.Body = new ASMethodBody(m);
				m.Body.ByteCode = (byte[])template.Body.ByteCode.Clone();
				m.Body.param_defaultvalues = (byte[])template.Body.param_defaultvalues.Clone();
				m.Body._link_codescope = template.Body._link_codescope;
				m.IsAnonymous = true;

				m.Parameters.AddRange(template.Parameters);

				m.__is_buildin_proto = true;

				int method_ptr = Context.GC.AllocClosure(m);
				if (method_ptr == 0)
				{
					throw new LoaderException("String proto : lastIndexOf alloc failed");
				}

				((RtClosure)Context.GC.Heap[method_ptr]).ScopePtr = ((ASScript)Context.STRING._link_codescope.Parent.Container).__global_index__;
				((RtClosure)Context.GC.Heap[method_ptr]).Set_PROTOTYPE(-1, this);

				NaNBoxing v = default; v.SetHeapPtr(method_ptr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);

				NaNBoxing v_str = default; v_str.SetHeapPtr(name_str, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				CreateDynamic(ref error, proto, v_str, v, false, false, false);
				if (error.raised) return;
			}

			//slice
			{
				var name_str = Context.GC.AllocString("slice");
				if (name_str == 0)
				{
					throw new LoaderException("slice_str alloc failed");
				}
				Context.GC.Root.Add(Context.GC.Heap[name_str]);

				var template = Context.STRING.Instance.Traits.First(t => t.QName.Name == "slice").Method;

				ASMethod m = new ASMethod(Context.STRING._link_codescope.Parent.Container, Context.STRING.Token);
				m.ReturnTypeKind = TypeKind.String;
				m.Flags = template.Flags;
				m.Name = template.Name;
				m.Body = new ASMethodBody(m);
				m.Body.ByteCode = (byte[])template.Body.ByteCode.Clone();
				m.Body.param_defaultvalues = (byte[])template.Body.param_defaultvalues.Clone();
				m.Body._link_codescope = template.Body._link_codescope;
				m.IsAnonymous = true;

				m.Parameters.AddRange(template.Parameters);

				m.__is_buildin_proto = true;

				int method_ptr = Context.GC.AllocClosure(m);
				if (method_ptr == 0)
				{
					throw new LoaderException("String proto : slice alloc failed");
				}

				((RtClosure)Context.GC.Heap[method_ptr]).ScopePtr = ((ASScript)Context.STRING._link_codescope.Parent.Container).__global_index__;
				((RtClosure)Context.GC.Heap[method_ptr]).Set_PROTOTYPE(-1, this);

				NaNBoxing v = default; v.SetHeapPtr(method_ptr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);

				NaNBoxing v_str = default; v_str.SetHeapPtr(name_str, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				CreateDynamic(ref error, proto, v_str, v, false, false, false);
				if (error.raised) return;
			}

			//split
			{
				var name_str = Context.GC.AllocString("split");
				if (name_str == 0)
				{
					throw new LoaderException("split_str alloc failed");
				}
				Context.GC.Root.Add(Context.GC.Heap[name_str]);

				var template = Context.STRING.Instance.Traits.First(t => t.QName.Name == "split").Method;

				ASMethod m = new ASMethod(Context.STRING._link_codescope.Parent.Container, Context.STRING.Token);
				m.ReturnTypeKind = TypeKind.String;
				m.Flags = template.Flags;
				m.Name = template.Name;
				m.Body = new ASMethodBody(m);
				m.Body.ByteCode = (byte[])template.Body.ByteCode.Clone();
				m.Body.param_defaultvalues = (byte[])template.Body.param_defaultvalues.Clone();
				m.Body._link_codescope = template.Body._link_codescope;
				m.IsAnonymous = true;

				m.Parameters.AddRange(template.Parameters);

				m.__is_buildin_proto = true;

				int method_ptr = Context.GC.AllocClosure(m);
				if (method_ptr == 0)
				{
					throw new LoaderException("String proto : split alloc failed");
				}

				((RtClosure)Context.GC.Heap[method_ptr]).ScopePtr = ((ASScript)Context.STRING._link_codescope.Parent.Container).__global_index__;
				((RtClosure)Context.GC.Heap[method_ptr]).Set_PROTOTYPE(-1, this);

				NaNBoxing v = default; v.SetHeapPtr(method_ptr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);

				NaNBoxing v_str = default; v_str.SetHeapPtr(name_str, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				CreateDynamic(ref error, proto, v_str, v, false, false, false);
				if (error.raised) return;
			}

			//substring
			{
				var name_str = Context.GC.AllocString("substring");
				if (name_str == 0)
				{
					throw new LoaderException("substring_str alloc failed");
				}
				Context.GC.Root.Add(Context.GC.Heap[name_str]);

				var template = Context.STRING.Instance.Traits.First(t => t.QName.Name == "substring").Method;

				ASMethod m = new ASMethod(Context.STRING._link_codescope.Parent.Container, Context.STRING.Token);
				m.ReturnTypeKind = TypeKind.String;
				m.Flags = template.Flags;
				m.Name = template.Name;
				m.Body = new ASMethodBody(m);
				m.Body.ByteCode = (byte[])template.Body.ByteCode.Clone();
				m.Body.param_defaultvalues = (byte[])template.Body.param_defaultvalues.Clone();
				m.Body._link_codescope = template.Body._link_codescope;
				m.IsAnonymous = true;

				m.Parameters.AddRange(template.Parameters);

				m.__is_buildin_proto = true;

				int method_ptr = Context.GC.AllocClosure(m);
				if (method_ptr == 0)
				{
					throw new LoaderException("String proto : substring alloc failed");
				}

				((RtClosure)Context.GC.Heap[method_ptr]).ScopePtr = ((ASScript)Context.STRING._link_codescope.Parent.Container).__global_index__;
				((RtClosure)Context.GC.Heap[method_ptr]).Set_PROTOTYPE(-1, this);

				NaNBoxing v = default; v.SetHeapPtr(method_ptr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);

				NaNBoxing v_str = default; v_str.SetHeapPtr(name_str, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				CreateDynamic(ref error, proto, v_str, v, false, false, false);
				if (error.raised) return;
			}

			//toLowerCase
			{
				var name_str = Context.GC.AllocString("toLowerCase");
				if (name_str == 0)
				{
					throw new LoaderException("toLowerCase_str alloc failed");
				}
				Context.GC.Root.Add(Context.GC.Heap[name_str]);

				var template = Context.STRING.Instance.Traits.First(t => t.QName.Name == "toLowerCase").Method;

				ASMethod m = new ASMethod(Context.STRING._link_codescope.Parent.Container, Context.STRING.Token);
				m.ReturnTypeKind = TypeKind.String;
				m.Flags = template.Flags;
				m.Name = template.Name;
				m.Body = new ASMethodBody(m);
				m.Body.ByteCode = (byte[])template.Body.ByteCode.Clone();
				if (template.Body.param_defaultvalues != null)
				{
					m.Body.param_defaultvalues = (byte[])template.Body.param_defaultvalues.Clone();
				}
				m.Body._link_codescope = template.Body._link_codescope;
				m.IsAnonymous = true;

				m.Parameters.AddRange(template.Parameters);

				m.__is_buildin_proto = true;

				int method_ptr = Context.GC.AllocClosure(m);
				if (method_ptr == 0)
				{
					throw new LoaderException("String proto : toLowerCase alloc failed");
				}

				((RtClosure)Context.GC.Heap[method_ptr]).ScopePtr = ((ASScript)Context.STRING._link_codescope.Parent.Container).__global_index__;
				((RtClosure)Context.GC.Heap[method_ptr]).Set_PROTOTYPE(-1, this);

				NaNBoxing v = default; v.SetHeapPtr(method_ptr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);

				NaNBoxing v_str = default; v_str.SetHeapPtr(name_str, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				CreateDynamic(ref error, proto, v_str, v, false, false, false);
				if (error.raised) return;
			}

			//toUpperCase
			{
				var name_str = Context.GC.AllocString("toUpperCase");
				if (name_str == 0)
				{
					throw new LoaderException("toUpperCase_str alloc failed");
				}
				Context.GC.Root.Add(Context.GC.Heap[name_str]);

				var template = Context.STRING.Instance.Traits.First(t => t.QName.Name == "toUpperCase").Method;

				ASMethod m = new ASMethod(Context.STRING._link_codescope.Parent.Container, Context.STRING.Token);
				m.ReturnTypeKind = TypeKind.String;
				m.Flags = template.Flags;
				m.Name = template.Name;
				m.Body = new ASMethodBody(m);
				m.Body.ByteCode = (byte[])template.Body.ByteCode.Clone();
				if (template.Body.param_defaultvalues != null)
				{
					m.Body.param_defaultvalues = (byte[])template.Body.param_defaultvalues.Clone();
				}
				m.Body._link_codescope = template.Body._link_codescope;
				m.IsAnonymous = true;

				m.Parameters.AddRange(template.Parameters);

				m.__is_buildin_proto = true;

				int method_ptr = Context.GC.AllocClosure(m);
				if (method_ptr == 0)
				{
					throw new LoaderException("String proto : toUpperCase alloc failed");
				}

				((RtClosure)Context.GC.Heap[method_ptr]).ScopePtr = ((ASScript)Context.STRING._link_codescope.Parent.Container).__global_index__;
				((RtClosure)Context.GC.Heap[method_ptr]).Set_PROTOTYPE(-1, this);

				NaNBoxing v = default; v.SetHeapPtr(method_ptr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);

				NaNBoxing v_str = default; v_str.SetHeapPtr(name_str, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				CreateDynamic(ref error, proto, v_str, v, false, false, false);
				if (error.raised) return;
			}

			//toLocaleLowerCase
			{
				var name_str = Context.GC.AllocString("toLocaleLowerCase");
				if (name_str == 0)
				{
					throw new LoaderException("toLocaleLowerCase_str alloc failed");
				}
				Context.GC.Root.Add(Context.GC.Heap[name_str]);

				var template = Context.STRING.Instance.Traits.First(t => t.QName.Name == "toLocaleLowerCase").Method;

				ASMethod m = new ASMethod(Context.STRING._link_codescope.Parent.Container, Context.STRING.Token);
				m.ReturnTypeKind = TypeKind.String;
				m.Flags = template.Flags;
				m.Name = template.Name;
				m.Body = new ASMethodBody(m);
				m.Body.ByteCode = (byte[])template.Body.ByteCode.Clone();
				if (template.Body.param_defaultvalues != null)
				{
					m.Body.param_defaultvalues = (byte[])template.Body.param_defaultvalues.Clone();
				}
				m.Body._link_codescope = template.Body._link_codescope;
				m.IsAnonymous = true;

				m.Parameters.AddRange(template.Parameters);

				m.__is_buildin_proto = true;

				int method_ptr = Context.GC.AllocClosure(m);
				if (method_ptr == 0)
				{
					throw new LoaderException("String proto : toLocaleLowerCase alloc failed");
				}

				((RtClosure)Context.GC.Heap[method_ptr]).ScopePtr = ((ASScript)Context.STRING._link_codescope.Parent.Container).__global_index__;
				((RtClosure)Context.GC.Heap[method_ptr]).Set_PROTOTYPE(-1, this);

				NaNBoxing v = default; v.SetHeapPtr(method_ptr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);

				NaNBoxing v_str = default; v_str.SetHeapPtr(name_str, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				CreateDynamic(ref error, proto, v_str, v, false, false, false);
				if (error.raised) return;
			}
			//toLocaleUpperCase
			{
				var name_str = Context.GC.AllocString("toLocaleUpperCase");
				if (name_str == 0)
				{
					throw new LoaderException("toLocaleUpperCase_str alloc failed");
				}
				Context.GC.Root.Add(Context.GC.Heap[name_str]);

				var template = Context.STRING.Instance.Traits.First(t => t.QName.Name == "toLocaleUpperCase").Method;

				ASMethod m = new ASMethod(Context.STRING._link_codescope.Parent.Container, Context.STRING.Token);
				m.ReturnTypeKind = TypeKind.String;
				m.Flags = template.Flags;
				m.Name = template.Name;
				m.Body = new ASMethodBody(m);
				m.Body.ByteCode = (byte[])template.Body.ByteCode.Clone();
				if (template.Body.param_defaultvalues != null)
				{
					m.Body.param_defaultvalues = (byte[])template.Body.param_defaultvalues.Clone();
				}
				m.Body._link_codescope = template.Body._link_codescope;
				m.IsAnonymous = true;

				m.Parameters.AddRange(template.Parameters);

				m.__is_buildin_proto = true;

				int method_ptr = Context.GC.AllocClosure(m);
				if (method_ptr == 0)
				{
					throw new LoaderException("String proto : toLocaleUpperCase alloc failed");
				}

				((RtClosure)Context.GC.Heap[method_ptr]).ScopePtr = ((ASScript)Context.STRING._link_codescope.Parent.Container).__global_index__;
				((RtClosure)Context.GC.Heap[method_ptr]).Set_PROTOTYPE(-1, this);

				NaNBoxing v = default; v.SetHeapPtr(method_ptr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);

				NaNBoxing v_str = default; v_str.SetHeapPtr(name_str, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				CreateDynamic(ref error, proto, v_str, v, false, false, false);
				if (error.raised) return;
			}

			//valueOf
			{
				var name_str = Context.GC.AllocString("valueOf");
				if (name_str == 0)
				{
					throw new LoaderException("valueOf_str alloc failed");
				}
				Context.GC.Root.Add(Context.GC.Heap[name_str]);

				var template = Context.STRING.Instance.Traits.First(t => t.QName.Name == "valueOf").Method;

				ASMethod m = new ASMethod(Context.STRING._link_codescope.Parent.Container, Context.STRING.Token);
				m.ReturnTypeKind = TypeKind.String;
				m.Flags = template.Flags;
				m.Name = template.Name;
				m.Body = new ASMethodBody(m);
				m.Body.ByteCode = (byte[])template.Body.ByteCode.Clone();
				if (template.Body.param_defaultvalues != null)
				{
					m.Body.param_defaultvalues = (byte[])template.Body.param_defaultvalues.Clone();
				}
				m.Body._link_codescope = template.Body._link_codescope;
				m.IsAnonymous = true;

				m.Parameters.AddRange(template.Parameters);

				m.__is_buildin_proto = true;

				int method_ptr = Context.GC.AllocClosure(m);
				if (method_ptr == 0)
				{
					throw new LoaderException("String proto : valueOf alloc failed");
				}

				((RtClosure)Context.GC.Heap[method_ptr]).ScopePtr = ((ASScript)Context.STRING._link_codescope.Parent.Container).__global_index__;
				((RtClosure)Context.GC.Heap[method_ptr]).Set_PROTOTYPE(-1, this);

				NaNBoxing v = default; v.SetHeapPtr(method_ptr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);

				NaNBoxing v_str = default; v_str.SetHeapPtr(name_str, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				CreateDynamic(ref error, proto, v_str, v, false, false, false);
				if (error.raised) return;
			}

			//replace
			{
				var name_str = Context.GC.AllocString("replace");
				if (name_str == 0)
				{
					throw new LoaderException("replace_str alloc failed");
				}
				Context.GC.Root.Add(Context.GC.Heap[name_str]);

				var template = Context.STRING.Instance.Traits.First(t => t.QName.Name == "replace").Method;

				ASMethod m = new ASMethod(Context.STRING._link_codescope.Parent.Container, Context.STRING.Token);
				m.ReturnTypeKind = TypeKind.String;
				m.Flags = template.Flags;
				m.Name = template.Name;
				m.Body = new ASMethodBody(m);
				m.Body.ByteCode = (byte[])template.Body.ByteCode.Clone();
				if (template.Body.param_defaultvalues != null)
				{
					m.Body.param_defaultvalues = (byte[])template.Body.param_defaultvalues.Clone();
				}
				m.Body._link_codescope = template.Body._link_codescope;
				m.IsAnonymous = true;

				m.Parameters.AddRange(template.Parameters);

				m.__is_buildin_proto = true;

				int method_ptr = Context.GC.AllocClosure(m);
				if (method_ptr == 0)
				{
					throw new LoaderException("String proto : replace alloc failed");
				}

				((RtClosure)Context.GC.Heap[method_ptr]).ScopePtr = ((ASScript)Context.STRING._link_codescope.Parent.Container).__global_index__;
				((RtClosure)Context.GC.Heap[method_ptr]).Set_PROTOTYPE(-1, this);

				NaNBoxing v = default; v.SetHeapPtr(method_ptr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);

				NaNBoxing v_str = default; v_str.SetHeapPtr(name_str, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				CreateDynamic(ref error, proto, v_str, v, false, false, false);
				if (error.raised) return;
			}

			//search
			{
				var name_str = Context.GC.AllocString("search");
				if (name_str == 0)
				{
					throw new LoaderException("search_str alloc failed");
				}
				Context.GC.Root.Add(Context.GC.Heap[name_str]);

				var template = Context.STRING.Instance.Traits.First(t => t.QName.Name == "search").Method;

				ASMethod m = new ASMethod(Context.STRING._link_codescope.Parent.Container, Context.STRING.Token);
				m.ReturnTypeKind = TypeKind.String;
				m.Flags = template.Flags;
				m.Name = template.Name;
				m.Body = new ASMethodBody(m);
				m.Body.ByteCode = (byte[])template.Body.ByteCode.Clone();
				if (template.Body.param_defaultvalues != null)
				{
					m.Body.param_defaultvalues = (byte[])template.Body.param_defaultvalues.Clone();
				}
				m.Body._link_codescope = template.Body._link_codescope;
				m.IsAnonymous = true;

				m.Parameters.AddRange(template.Parameters);

				m.__is_buildin_proto = true;

				int method_ptr = Context.GC.AllocClosure(m);
				if (method_ptr == 0)
				{
					throw new LoaderException("String proto : search alloc failed");
				}

				((RtClosure)Context.GC.Heap[method_ptr]).ScopePtr = ((ASScript)Context.STRING._link_codescope.Parent.Container).__global_index__;
				((RtClosure)Context.GC.Heap[method_ptr]).Set_PROTOTYPE(-1, this);

				NaNBoxing v = default; v.SetHeapPtr(method_ptr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);

				NaNBoxing v_str = default; v_str.SetHeapPtr(name_str, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				CreateDynamic(ref error, proto, v_str, v, false, false, false);
				if (error.raised) return;
			}
		}



		private void CreateFunctionProto(ref ReceiveError error)
		{
#if FORCOMPILER
			if (IsComputeConstExpr)
			{
				return;
			}
#endif

			var proto_ptr = ((RtScriptClass)Context.GC.Heap[Context.FUNCTION.__instance_index__]).PROTO__PTR;
			var proto = Context.GC.Heap[proto_ptr];

			// call
			{
				var call_ptr = Context.GC.AllocString("call");
				if (call_ptr == 0)
				{
					throw new LoaderException("CALL_PTR alloc failed");
				}
				Context.GC.Root.Add(Context.GC.Heap[call_ptr]);

				ASMethod call = Context.FUNCTION.Instance._vtable.Items[3].Trait.Method; call.__is_call_or_apply = true; call.__is_buildin_proto = true;

				int invokecall_ptr = Context.GC.AllocClosure(call);
				if (invokecall_ptr == 0)
				{
					throw new LoaderException("Function proto : call alloc failed");
				}

				((RtClosure)Context.GC.Heap[invokecall_ptr]).ScopePtr = ((ASScript)Context.FUNCTION._link_codescope.Parent.Container).__global_index__;
				((RtClosure)Context.GC.Heap[invokecall_ptr]).Set_PROTOTYPE(-1, this);

				NaNBoxing v = default; v.SetHeapPtr(invokecall_ptr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);
				NaNBoxing v_str = default; v_str.SetHeapPtr(call_ptr, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				CreateDynamic(ref error, proto, v_str, v, false, false, false);
				if (error.raised) return;
			}

			//apply
			{
				var apply_ptr = Context.GC.AllocString("apply");
				if (apply_ptr == 0)
				{
					throw new LoaderException("APPLY_PTR alloc failed");
				}
				Context.GC.Root.Add(Context.GC.Heap[apply_ptr]);

				ASMethod apply = Context.FUNCTION.Instance._vtable.Items[2].Trait.Method; apply.__is_call_or_apply = true; apply.__is_buildin_proto = true;

				int invokeapply_ptr = Context.GC.AllocClosure(apply);
				if (invokeapply_ptr == 0)
				{
					throw new LoaderException("Function proto : apply alloc failed");
				}

				((RtClosure)Context.GC.Heap[invokeapply_ptr]).ScopePtr = ((ASScript)Context.FUNCTION._link_codescope.Parent.Container).__global_index__;
				((RtClosure)Context.GC.Heap[invokeapply_ptr]).Set_PROTOTYPE(-1, this);


				NaNBoxing v = default; v.SetHeapPtr(invokeapply_ptr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);
				NaNBoxing v_str = default; v_str.SetHeapPtr(apply_ptr, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				CreateDynamic(ref error, proto, v_str, v, false, false, false);
				if (error.raised) return;
			}

			// tostring
			{

				ASMethod tostring = new ASMethod(Context.FUNCTION._link_codescope.Parent.Container, Context.FUNCTION.Token);
				tostring.ReturnTypeKind = TypeKind.String;
				tostring.Flags = MethodFlags.Native;
				tostring.Name = "toString";
				tostring.Body = new ASMethodBody(tostring);
				tostring.Body.ByteCode = new byte[12];
				tostring.Body._link_codescope = new CodeScope() { Members = new List<ScopeMember>(), Parent = Context.FUNCTION._link_codescope.Parent };
				tostring.__ismethod = false;
				tostring.IsAnonymous = true;
				tostring.__is_buildin_proto = true;

				int tostring_ptr = Context.GC.AllocClosure(tostring);
				if (tostring_ptr == 0)
				{
					throw new LoaderException("Function proto : toString alloc failed");
				}

				((RtClosure)Context.GC.Heap[tostring_ptr]).ScopePtr = ((ASScript)Context.FUNCTION._link_codescope.Parent.Container).__global_index__;

				NaNBoxing v = default; v.SetHeapPtr(tostring_ptr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);
				NaNBoxing v_str = default; v_str.SetHeapPtr(TOSTRING_STR, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				CreateDynamic(ref error, proto, v_str, v, false, false, false);
				if (error.raised) return;
			}
		}

		private void CreateArrayProto(ref ReceiveError error)
		{
#if FORCOMPILER
			if (IsComputeConstExpr)
			{
				return;
			}
#endif
			var proto_ptr = ((RtScriptClass)Context.GC.Heap[Context.ARRAY.__instance_index__]).PROTO__PTR;
			var proto = Context.GC.Heap[proto_ptr];

			//tostring
			{
				{
					Context.GC.Root.Add(Context.GC.Heap[TOSTRING_STR]);

					ASMethod tostring = new ASMethod(Context.ARRAY._link_codescope.Parent.Container, Context.ARRAY.Token);
					tostring.ReturnTypeKind = TypeKind.String;
					tostring.Flags = MethodFlags.Native;
					tostring.Name = "toString";
					tostring.Body = new ASMethodBody(tostring);
					tostring.Body.ByteCode = new byte[12];
					tostring.Body._link_codescope = new CodeScope() { Members = new List<ScopeMember>(), Parent = Context.ARRAY._link_codescope.Parent };
					tostring.IsAnonymous = true;
					tostring.__is_buildin_proto = true;

					int tostring_ptr = Context.GC.AllocClosure(tostring);
					if (tostring_ptr == 0)
					{
						throw new LoaderException("Array proto : toString alloc failed");
					}

				((RtClosure)Context.GC.Heap[tostring_ptr]).ScopePtr = ((ASScript)Context.ARRAY._link_codescope.Parent.Container).__global_index__;

					NaNBoxing v = default; v.SetHeapPtr(tostring_ptr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);
					NaNBoxing v_str = default; v_str.SetHeapPtr(TOSTRING_STR, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
					CreateDynamic(ref error, proto, v_str, v, false, false, true);
					if (error.raised) return;
				}

			}

			//concat
			{

				var concat = Context.GC.AllocString("concat");
				if (concat == 0)
				{
					throw new LoaderException("concat string alloc failed");
				}
				Context.GC.Root.Add(Context.GC.Heap[concat]);

				ASMethod m = new ASMethod(Context.ARRAY._link_codescope.Parent.Container, Context.ARRAY.Token);
				m.ReturnTypeKind = TypeKind.Array;
				m.Flags = MethodFlags.Native | MethodFlags.NeedRest;
				m.Name = "concat";
				m.Body = new ASMethodBody(m);
				m.Body.ByteCode = new byte[12];
				m.Body._link_codescope = new CodeScope() { Members = new List<ScopeMember>(), Parent = Context.ARRAY._link_codescope.Parent };
				m.IsAnonymous = true;
				m.Parameters.Add(new ASParameter(m) { IsOptional = false, Name = "rest", IsRest = true, TypeKind = TypeKind.Array, Type = Context.ARRAY.QName });
				m.Body._link_codescope.Members.Add(new ScopeMember(m.Body, null)
				{
					Kind = ScopeMemberKind.Parameter,
					PName = "rest",
					Type = m.Parameters[0].Type,
					TypeKind = m.Parameters[0].TypeKind,
					__rt_type_class__ = Context.ARRAY,
					_rt_type_flag = Context.ARRAY.Instance.Flags
				});

				m.__is_buildin_proto = true;

				int concat_ptr = Context.GC.AllocClosure(m);
				if (concat_ptr == 0)
				{
					throw new LoaderException("Array proto: concat alloc failed");
				}

				((RtClosure)Context.GC.Heap[concat_ptr]).ScopePtr = ((ASScript)Context.OBJECT._link_codescope.Parent.Container).__global_index__;
				((RtClosure)Context.GC.Heap[concat_ptr]).Set_PROTOTYPE(-1, this); //设置prototype为undefined

				NaNBoxing v = default; v.SetHeapPtr(concat_ptr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);
				NaNBoxing v_str = default; v_str.SetHeapPtr(concat, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				CreateDynamic(ref error, proto, v_str, v, false, false, false);
				if (error.raised) return;

			}

			//push
			{

				var push = Context.GC.AllocString("push");
				if (push == 0)
				{
					throw new LoaderException("push string alloc failed");
				}
				Context.GC.Root.Add(Context.GC.Heap[push]);

				ASMethod m = new ASMethod(Context.ARRAY._link_codescope.Parent.Container, Context.ARRAY.Token);
				m.ReturnTypeKind = TypeKind.Uint;
				m.Flags = MethodFlags.Native | MethodFlags.NeedRest;
				m.Name = "push";
				m.Body = new ASMethodBody(m);
				m.Body.ByteCode = new byte[12];
				m.Body._link_codescope = new CodeScope() { Members = new List<ScopeMember>(), Parent = Context.ARRAY._link_codescope.Parent };
				m.IsAnonymous = true;
				m.Parameters.Add(new ASParameter(m) { IsOptional = false, Name = "rest", IsRest = true, TypeKind = TypeKind.Array, Type = Context.ARRAY.QName });
				m.Body._link_codescope.Members.Add(new ScopeMember(m.Body, null)
				{
					Kind = ScopeMemberKind.Parameter,
					PName = "rest",
					Type = m.Parameters[0].Type,
					TypeKind = m.Parameters[0].TypeKind,
					__rt_type_class__ = Context.ARRAY,
					_rt_type_flag = Context.ARRAY.Instance.Flags
				});

				m.__is_buildin_proto = true;

				int push_ptr = Context.GC.AllocClosure(m);
				if (push_ptr == 0)
				{
					throw new LoaderException("Array proto: push alloc failed");
				}

				((RtClosure)Context.GC.Heap[push_ptr]).ScopePtr = ((ASScript)Context.OBJECT._link_codescope.Parent.Container).__global_index__;
				((RtClosure)Context.GC.Heap[push_ptr]).Set_PROTOTYPE(-1, this); //设置prototype为undefined

				NaNBoxing v2 = default; v2.SetHeapPtr(push_ptr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);
				NaNBoxing v_str = default; v_str.SetHeapPtr(push, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				CreateDynamic(ref error, proto, v_str, v2, false, false, false);
				if (error.raised) return;

			}
			//pop
			{
				var name_str = Context.GC.AllocString("pop");
				if (name_str == 0)
				{
					throw new LoaderException("pop_str alloc failed");
				}
				Context.GC.Root.Add(Context.GC.Heap[name_str]);

				var template = Context.ARRAY.Instance.Traits.First(t => t.QName.Name == "pop").Method;

				ASMethod m = new ASMethod(Context.ARRAY._link_codescope.Parent.Container, Context.ARRAY.Token);
				m.ReturnTypeKind = TypeKind.String;
				m.Flags = template.Flags;
				m.Name = template.Name;
				m.Body = new ASMethodBody(m);
				m.Body.ByteCode = (byte[])template.Body.ByteCode.Clone();
				if (template.Body.param_defaultvalues != null)
				{
					m.Body.param_defaultvalues = (byte[])template.Body.param_defaultvalues.Clone();
				}
				m.Body._link_codescope = template.Body._link_codescope;
				m.IsAnonymous = true;

				m.Parameters.AddRange(template.Parameters);

				m.__is_buildin_proto = true;

				int method_ptr = Context.GC.AllocClosure(m);
				if (method_ptr == 0)
				{
					throw new LoaderException("Array proto : pop alloc failed");
				}

				((RtClosure)Context.GC.Heap[method_ptr]).ScopePtr = ((ASScript)Context.ARRAY._link_codescope.Parent.Container).__global_index__;
				((RtClosure)Context.GC.Heap[method_ptr]).Set_PROTOTYPE(-1, this);

				NaNBoxing v = default; v.SetHeapPtr(method_ptr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);

				NaNBoxing v_str = default; v_str.SetHeapPtr(name_str, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				CreateDynamic(ref error, proto, v_str, v, false, false, false);
				if (error.raised) return;
			}

			//join
			{
				var name_str = Context.GC.AllocString("join");
				if (name_str == 0)
				{
					throw new LoaderException("join_str alloc failed");
				}
				Context.GC.Root.Add(Context.GC.Heap[name_str]);

				var template = Context.ARRAY.Instance.Traits.First(t => t.QName.Name == "join").Method;

				ASMethod m = new ASMethod(Context.ARRAY._link_codescope.Parent.Container, Context.ARRAY.Token);
				m.ReturnTypeKind = TypeKind.String;
				m.Flags = template.Flags;
				m.Name = template.Name;
				m.Body = new ASMethodBody(m);
				m.Body.ByteCode = (byte[])template.Body.ByteCode.Clone();
				if (template.Body.param_defaultvalues != null)
				{
					m.Body.param_defaultvalues = (byte[])template.Body.param_defaultvalues.Clone();
				}
				m.Body._link_codescope = template.Body._link_codescope;
				m.IsAnonymous = true;

				m.Parameters.AddRange(template.Parameters);

				m.__is_buildin_proto = true;

				int method_ptr = Context.GC.AllocClosure(m);
				if (method_ptr == 0)
				{
					throw new LoaderException("Array proto : join alloc failed");
				}

				((RtClosure)Context.GC.Heap[method_ptr]).ScopePtr = ((ASScript)Context.ARRAY._link_codescope.Parent.Container).__global_index__;
				((RtClosure)Context.GC.Heap[method_ptr]).Set_PROTOTYPE(-1, this);

				NaNBoxing v = default; v.SetHeapPtr(method_ptr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);

				NaNBoxing v_str = default; v_str.SetHeapPtr(name_str, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				CreateDynamic(ref error, proto, v_str, v, false, false, false);
				if (error.raised) return;
			}

			//shift
			{
				var name_str = Context.GC.AllocString("shift");
				if (name_str == 0)
				{
					throw new LoaderException("shift_str alloc failed");
				}
				Context.GC.Root.Add(Context.GC.Heap[name_str]);

				var template = Context.ARRAY.Instance.Traits.First(t => t.QName.Name == "shift").Method;

				ASMethod m = new ASMethod(Context.ARRAY._link_codescope.Parent.Container, Context.ARRAY.Token);
				m.ReturnTypeKind = TypeKind.String;
				m.Flags = template.Flags;
				m.Name = template.Name;
				m.Body = new ASMethodBody(m);
				m.Body.ByteCode = (byte[])template.Body.ByteCode.Clone();
				if (template.Body.param_defaultvalues != null)
				{
					m.Body.param_defaultvalues = (byte[])template.Body.param_defaultvalues.Clone();
				}
				m.Body._link_codescope = template.Body._link_codescope;
				m.IsAnonymous = true;

				m.Parameters.AddRange(template.Parameters);

				m.__is_buildin_proto = true;

				int method_ptr = Context.GC.AllocClosure(m);
				if (method_ptr == 0)
				{
					throw new LoaderException("Array proto : shift alloc failed");
				}

				((RtClosure)Context.GC.Heap[method_ptr]).ScopePtr = ((ASScript)Context.ARRAY._link_codescope.Parent.Container).__global_index__;
				((RtClosure)Context.GC.Heap[method_ptr]).Set_PROTOTYPE(-1, this);

				NaNBoxing v = default; v.SetHeapPtr(method_ptr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);

				NaNBoxing v_str = default; v_str.SetHeapPtr(name_str, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				CreateDynamic(ref error, proto, v_str, v, false, false, false);
				if (error.raised) return;
			}

			//unshift
			{
				var name_str = Context.GC.AllocString("unshift");
				if (name_str == 0)
				{
					throw new LoaderException("unshift_str alloc failed");
				}
				Context.GC.Root.Add(Context.GC.Heap[name_str]);

				var template = Context.ARRAY.Instance.Traits.First(t => t.QName.Name == "unshift").Method;

				ASMethod m = new ASMethod(Context.ARRAY._link_codescope.Parent.Container, Context.ARRAY.Token);
				m.ReturnTypeKind = TypeKind.String;
				m.Flags = template.Flags;
				m.Name = template.Name;
				m.Body = new ASMethodBody(m);
				m.Body.ByteCode = (byte[])template.Body.ByteCode.Clone();
				if (template.Body.param_defaultvalues != null)
				{
					m.Body.param_defaultvalues = (byte[])template.Body.param_defaultvalues.Clone();
				}
				m.Body._link_codescope = template.Body._link_codescope;
				m.IsAnonymous = true;

				m.Parameters.AddRange(template.Parameters);

				m.__is_buildin_proto = true;

				int method_ptr = Context.GC.AllocClosure(m);
				if (method_ptr == 0)
				{
					throw new LoaderException("Array proto : unshift alloc failed");
				}

				((RtClosure)Context.GC.Heap[method_ptr]).ScopePtr = ((ASScript)Context.ARRAY._link_codescope.Parent.Container).__global_index__;
				((RtClosure)Context.GC.Heap[method_ptr]).Set_PROTOTYPE(-1, this);

				NaNBoxing v = default; v.SetHeapPtr(method_ptr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);

				NaNBoxing v_str = default; v_str.SetHeapPtr(name_str, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				CreateDynamic(ref error, proto, v_str, v, false, false, false);
				if (error.raised) return;
			}

			//some
			{
				var name_str = Context.GC.AllocString("some");
				if (name_str == 0)
				{
					throw new LoaderException("some_str alloc failed");
				}
				Context.GC.Root.Add(Context.GC.Heap[name_str]);

				var template = Context.ARRAY.Instance.Traits.First(t => t.QName.Name == "some").Method;

				ASMethod m = new ASMethod(Context.ARRAY._link_codescope.Parent.Container, Context.ARRAY.Token);
				m.ReturnTypeKind = TypeKind.String;
				m.Flags = template.Flags;
				m.Name = template.Name;
				m.Body = new ASMethodBody(m);
				m.Body.ByteCode = (byte[])template.Body.ByteCode.Clone();
				if (template.Body.param_defaultvalues != null)
				{
					m.Body.param_defaultvalues = (byte[])template.Body.param_defaultvalues.Clone();
				}
				m.Body._link_codescope = template.Body._link_codescope;
				m.IsAnonymous = true;

				m.Parameters.AddRange(template.Parameters);

				m.__is_buildin_proto = true;

				int method_ptr = Context.GC.AllocClosure(m);
				if (method_ptr == 0)
				{
					throw new LoaderException("Array proto : some alloc failed");
				}

				((RtClosure)Context.GC.Heap[method_ptr]).ScopePtr = ((ASScript)Context.ARRAY._link_codescope.Parent.Container).__global_index__;
				((RtClosure)Context.GC.Heap[method_ptr]).Set_PROTOTYPE(-1, this);

				NaNBoxing v = default; v.SetHeapPtr(method_ptr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);

				NaNBoxing v_str = default; v_str.SetHeapPtr(name_str, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				CreateDynamic(ref error, proto, v_str, v, false, false, false);
				if (error.raised) return;
			}
			//every
			{
				var name_str = Context.GC.AllocString("every");
				if (name_str == 0)
				{
					throw new LoaderException("every_str alloc failed");
				}
				Context.GC.Root.Add(Context.GC.Heap[name_str]);

				var template = Context.ARRAY.Instance.Traits.First(t => t.QName.Name == "every").Method;

				ASMethod m = new ASMethod(Context.ARRAY._link_codescope.Parent.Container, Context.ARRAY.Token);
				m.ReturnTypeKind = TypeKind.String;
				m.Flags = template.Flags;
				m.Name = template.Name;
				m.Body = new ASMethodBody(m);
				m.Body.ByteCode = (byte[])template.Body.ByteCode.Clone();
				if (template.Body.param_defaultvalues != null)
				{
					m.Body.param_defaultvalues = (byte[])template.Body.param_defaultvalues.Clone();
				}
				m.Body._link_codescope = template.Body._link_codescope;
				m.IsAnonymous = true;

				m.Parameters.AddRange(template.Parameters);

				m.__is_buildin_proto = true;

				int method_ptr = Context.GC.AllocClosure(m);
				if (method_ptr == 0)
				{
					throw new LoaderException("Array proto : every alloc failed");
				}

				((RtClosure)Context.GC.Heap[method_ptr]).ScopePtr = ((ASScript)Context.ARRAY._link_codescope.Parent.Container).__global_index__;
				((RtClosure)Context.GC.Heap[method_ptr]).Set_PROTOTYPE(-1, this);

				NaNBoxing v = default; v.SetHeapPtr(method_ptr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);

				NaNBoxing v_str = default; v_str.SetHeapPtr(name_str, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				CreateDynamic(ref error, proto, v_str, v, false, false, false);
				if (error.raised) return;
			}
			//forEach
			{
				var name_str = Context.GC.AllocString("forEach");
				if (name_str == 0)
				{
					throw new LoaderException("forEach_str alloc failed");
				}
				Context.GC.Root.Add(Context.GC.Heap[name_str]);

				var template = Context.ARRAY.Instance.Traits.First(t => t.QName.Name == "forEach").Method;

				ASMethod m = new ASMethod(Context.ARRAY._link_codescope.Parent.Container, Context.ARRAY.Token);
				m.ReturnTypeKind = TypeKind.String;
				m.Flags = template.Flags;
				m.Name = template.Name;
				m.Body = new ASMethodBody(m);
				m.Body.ByteCode = (byte[])template.Body.ByteCode.Clone();
				if (template.Body.param_defaultvalues != null)
				{
					m.Body.param_defaultvalues = (byte[])template.Body.param_defaultvalues.Clone();
				}
				m.Body._link_codescope = template.Body._link_codescope;
				m.IsAnonymous = true;

				m.Parameters.AddRange(template.Parameters);

				m.__is_buildin_proto = true;

				int method_ptr = Context.GC.AllocClosure(m);
				if (method_ptr == 0)
				{
					throw new LoaderException("Array proto : forEach alloc failed");
				}

				((RtClosure)Context.GC.Heap[method_ptr]).ScopePtr = ((ASScript)Context.ARRAY._link_codescope.Parent.Container).__global_index__;
				((RtClosure)Context.GC.Heap[method_ptr]).Set_PROTOTYPE(-1, this);

				NaNBoxing v = default; v.SetHeapPtr(method_ptr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);

				NaNBoxing v_str = default; v_str.SetHeapPtr(name_str, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				CreateDynamic(ref error, proto, v_str, v, false, false, false);
				if (error.raised) return;
			}
			//filter
			{
				var name_str = Context.GC.AllocString("filter");
				if (name_str == 0)
				{
					throw new LoaderException("filter_str alloc failed");
				}
				Context.GC.Root.Add(Context.GC.Heap[name_str]);

				var template = Context.ARRAY.Instance.Traits.First(t => t.QName.Name == "filter").Method;

				ASMethod m = new ASMethod(Context.ARRAY._link_codescope.Parent.Container, Context.ARRAY.Token);
				m.ReturnTypeKind = TypeKind.String;
				m.Flags = template.Flags;
				m.Name = template.Name;
				m.Body = new ASMethodBody(m);
				m.Body.ByteCode = (byte[])template.Body.ByteCode.Clone();
				if (template.Body.param_defaultvalues != null)
				{
					m.Body.param_defaultvalues = (byte[])template.Body.param_defaultvalues.Clone();
				}
				m.Body._link_codescope = template.Body._link_codescope;
				m.IsAnonymous = true;

				m.Parameters.AddRange(template.Parameters);

				m.__is_buildin_proto = true;

				int method_ptr = Context.GC.AllocClosure(m);
				if (method_ptr == 0)
				{
					throw new LoaderException("Array proto : filter alloc failed");
				}

				((RtClosure)Context.GC.Heap[method_ptr]).ScopePtr = ((ASScript)Context.ARRAY._link_codescope.Parent.Container).__global_index__;
				((RtClosure)Context.GC.Heap[method_ptr]).Set_PROTOTYPE(-1, this);

				NaNBoxing v = default; v.SetHeapPtr(method_ptr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);

				NaNBoxing v_str = default; v_str.SetHeapPtr(name_str, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				CreateDynamic(ref error, proto, v_str, v, false, false, false);
				if (error.raised) return;
			}
			//map
			{
				var name_str = Context.GC.AllocString("map");
				if (name_str == 0)
				{
					throw new LoaderException("map_str alloc failed");
				}
				Context.GC.Root.Add(Context.GC.Heap[name_str]);

				var template = Context.ARRAY.Instance.Traits.First(t => t.QName.Name == "map").Method;

				ASMethod m = new ASMethod(Context.ARRAY._link_codescope.Parent.Container, Context.ARRAY.Token);
				m.ReturnTypeKind = TypeKind.String;
				m.Flags = template.Flags;
				m.Name = template.Name;
				m.Body = new ASMethodBody(m);
				m.Body.ByteCode = (byte[])template.Body.ByteCode.Clone();
				if (template.Body.param_defaultvalues != null)
				{
					m.Body.param_defaultvalues = (byte[])template.Body.param_defaultvalues.Clone();
				}
				m.Body._link_codescope = template.Body._link_codescope;
				m.IsAnonymous = true;

				m.Parameters.AddRange(template.Parameters);

				m.__is_buildin_proto = true;

				int method_ptr = Context.GC.AllocClosure(m);
				if (method_ptr == 0)
				{
					throw new LoaderException("Array proto : map alloc failed");
				}

				((RtClosure)Context.GC.Heap[method_ptr]).ScopePtr = ((ASScript)Context.ARRAY._link_codescope.Parent.Container).__global_index__;
				((RtClosure)Context.GC.Heap[method_ptr]).Set_PROTOTYPE(-1, this);

				NaNBoxing v = default; v.SetHeapPtr(method_ptr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);

				NaNBoxing v_str = default; v_str.SetHeapPtr(name_str, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				CreateDynamic(ref error, proto, v_str, v, false, false, false);
				if (error.raised) return;
			}
			//indexOf
			{
				var name_str = Context.GC.AllocString("indexOf");
				if (name_str == 0)
				{
					throw new LoaderException("indexOf_str alloc failed");
				}
				Context.GC.Root.Add(Context.GC.Heap[name_str]);

				var template = Context.ARRAY.Instance.Traits.First(t => t.QName.Name == "indexOf").Method;

				ASMethod m = new ASMethod(Context.ARRAY._link_codescope.Parent.Container, Context.ARRAY.Token);
				m.ReturnTypeKind = TypeKind.String;
				m.Flags = template.Flags;
				m.Name = template.Name;
				m.Body = new ASMethodBody(m);
				m.Body.ByteCode = (byte[])template.Body.ByteCode.Clone();
				if (template.Body.param_defaultvalues != null)
				{
					m.Body.param_defaultvalues = (byte[])template.Body.param_defaultvalues.Clone();
				}
				m.Body._link_codescope = template.Body._link_codescope;
				m.IsAnonymous = true;

				m.Parameters.AddRange(template.Parameters);

				m.__is_buildin_proto = true;

				int method_ptr = Context.GC.AllocClosure(m);
				if (method_ptr == 0)
				{
					throw new LoaderException("Array proto : indexOf alloc failed");
				}

				((RtClosure)Context.GC.Heap[method_ptr]).ScopePtr = ((ASScript)Context.ARRAY._link_codescope.Parent.Container).__global_index__;
				((RtClosure)Context.GC.Heap[method_ptr]).Set_PROTOTYPE(-1, this);

				NaNBoxing v = default; v.SetHeapPtr(method_ptr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);

				NaNBoxing v_str = default; v_str.SetHeapPtr(name_str, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				CreateDynamic(ref error, proto, v_str, v, false, false, false);
				if (error.raised) return;
			}
			//lastIndexOf
			{
				var name_str = Context.GC.AllocString("lastIndexOf");
				if (name_str == 0)
				{
					throw new LoaderException("lastIndexOf_str alloc failed");
				}
				Context.GC.Root.Add(Context.GC.Heap[name_str]);

				var template = Context.ARRAY.Instance.Traits.First(t => t.QName.Name == "lastIndexOf").Method;

				ASMethod m = new ASMethod(Context.ARRAY._link_codescope.Parent.Container, Context.ARRAY.Token);
				m.ReturnTypeKind = TypeKind.String;
				m.Flags = template.Flags;
				m.Name = template.Name;
				m.Body = new ASMethodBody(m);
				m.Body.ByteCode = (byte[])template.Body.ByteCode.Clone();
				if (template.Body.param_defaultvalues != null)
				{
					m.Body.param_defaultvalues = (byte[])template.Body.param_defaultvalues.Clone();
				}
				m.Body._link_codescope = template.Body._link_codescope;
				m.IsAnonymous = true;

				m.Parameters.AddRange(template.Parameters);

				m.__is_buildin_proto = true;

				int method_ptr = Context.GC.AllocClosure(m);
				if (method_ptr == 0)
				{
					throw new LoaderException("Array proto : lastIndexOf alloc failed");
				}

				((RtClosure)Context.GC.Heap[method_ptr]).ScopePtr = ((ASScript)Context.ARRAY._link_codescope.Parent.Container).__global_index__;
				((RtClosure)Context.GC.Heap[method_ptr]).Set_PROTOTYPE(-1, this);

				NaNBoxing v = default; v.SetHeapPtr(method_ptr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);

				NaNBoxing v_str = default; v_str.SetHeapPtr(name_str, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				CreateDynamic(ref error, proto, v_str, v, false, false, false);
				if (error.raised) return;
			}
			//slice
			{
				var name_str = Context.GC.AllocString("slice");
				if (name_str == 0)
				{
					throw new LoaderException("slice_str alloc failed");
				}
				Context.GC.Root.Add(Context.GC.Heap[name_str]);
				var template = Context.ARRAY.Instance.Traits.First(t => t.QName.Name == "slice").Method;
				ASMethod m = new ASMethod(Context.ARRAY._link_codescope.Parent.Container, Context.ARRAY.Token);
				m.ReturnTypeKind = TypeKind.Array;
				m.Flags = template.Flags;
				m.Name = template.Name;
				m.Body = new ASMethodBody(m);
				m.Body.ByteCode = (byte[])template.Body.ByteCode.Clone();
				if (template.Body.param_defaultvalues != null)
				{
					m.Body.param_defaultvalues = (byte[])template.Body.param_defaultvalues.Clone();
				}
				m.Body._link_codescope = template.Body._link_codescope;
				m.IsAnonymous = true;
				m.Parameters.AddRange(template.Parameters);
				m.__is_buildin_proto = true;
				int method_ptr = Context.GC.AllocClosure(m);
				if (method_ptr == 0)
				{
					throw new LoaderException("Array proto : slice alloc failed");
				}
				((RtClosure)Context.GC.Heap[method_ptr]).ScopePtr = ((ASScript)Context.ARRAY._link_codescope.Parent.Container).__global_index__;
				((RtClosure)Context.GC.Heap[method_ptr]).Set_PROTOTYPE(-1, this);
				NaNBoxing v = default; v.SetHeapPtr(method_ptr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);
				NaNBoxing v_str = default; v_str.SetHeapPtr(name_str, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				CreateDynamic(ref error, proto, v_str, v, false, false, false);
				if (error.raised) return;
			}
			//splice
			{
				var name_str = Context.GC.AllocString("splice");
				if (name_str == 0)
				{
					throw new LoaderException("splice_str alloc failed");
				}
				Context.GC.Root.Add(Context.GC.Heap[name_str]);
				var template = Context.ARRAY.Instance.Traits.First(t => t.QName.Name == "splice").Method;
				ASMethod m = new ASMethod(Context.ARRAY._link_codescope.Parent.Container, Context.ARRAY.Token);
				m.ReturnTypeKind = TypeKind.Array;
				m.Flags = template.Flags;
				m.Name = template.Name;
				m.Body = new ASMethodBody(m);
				m.Body.ByteCode = (byte[])template.Body.ByteCode.Clone();
				if (template.Body.param_defaultvalues != null)
				{
					m.Body.param_defaultvalues = (byte[])template.Body.param_defaultvalues.Clone();
				}
				m.Body._link_codescope = template.Body._link_codescope;
				m.IsAnonymous = true;
				m.Parameters.AddRange(template.Parameters);
				m.__is_buildin_proto = true;
				int method_ptr = Context.GC.AllocClosure(m);
				if (method_ptr == 0)
				{
					throw new LoaderException("Array proto : splice alloc failed");
				}
				((RtClosure)Context.GC.Heap[method_ptr]).ScopePtr = ((ASScript)Context.ARRAY._link_codescope.Parent.Container).__global_index__;
				((RtClosure)Context.GC.Heap[method_ptr]).Set_PROTOTYPE(-1, this);
				NaNBoxing v = default; v.SetHeapPtr(method_ptr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);
				NaNBoxing v_str = default; v_str.SetHeapPtr(name_str, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				CreateDynamic(ref error, proto, v_str, v, false, false, false);
				if (error.raised) return;
			}
			//sort
			{
				var name_str = Context.GC.AllocString("sort");
				if (name_str == 0)
				{
					throw new LoaderException("sort_str alloc failed");
				}
				Context.GC.Root.Add(Context.GC.Heap[name_str]);
				var template = Context.ARRAY.Instance.Traits.First(t => t.QName.Name == "sort").Method;
				ASMethod m = new ASMethod(Context.ARRAY._link_codescope.Parent.Container, Context.ARRAY.Token);
				m.ReturnTypeKind = TypeKind.Array;
				m.Flags = template.Flags;
				m.Name = template.Name;
				m.Body = new ASMethodBody(m);
				m.Body.ByteCode = (byte[])template.Body.ByteCode.Clone();
				if (template.Body.param_defaultvalues != null)
				{
					m.Body.param_defaultvalues = (byte[])template.Body.param_defaultvalues.Clone();
				}
				m.Body._link_codescope = template.Body._link_codescope;
				m.IsAnonymous = true;
				m.Parameters.AddRange(template.Parameters);
				m.__is_buildin_proto = true;
				int method_ptr = Context.GC.AllocClosure(m);
				if (method_ptr == 0)
				{
					throw new LoaderException("Array proto : sort alloc failed");
				}
				((RtClosure)Context.GC.Heap[method_ptr]).ScopePtr = ((ASScript)Context.ARRAY._link_codescope.Parent.Container).__global_index__;
				((RtClosure)Context.GC.Heap[method_ptr]).Set_PROTOTYPE(-1, this);
				NaNBoxing v = default; v.SetHeapPtr(method_ptr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);
				NaNBoxing v_str = default; v_str.SetHeapPtr(name_str, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				CreateDynamic(ref error, proto, v_str, v, false, false, false);
				if (error.raised) return;
			}

		}

		private bool _shutdownEvent = false;
		public void RequestShutdown()
		{
			_shutdownEvent = true;
		}

		public void Run(Action<PlayerException> onErrorRaised)
		{



			CheckRequires();

			if (Context.OBJECT == null)
			{
				throw new LoaderException("core lib not loaded.");
			}

			ReceiveError error = new ReceiveError();

			//InitScript必须初始化constructor字符串。。
			CONSTRUCTOR_STR = Context.GC.AllocString("constructor"); if (CONSTRUCTOR_STR == 0) { throw new LoaderException("CONSTRUCTORPTR alloc failed"); }
			Context.GC.Root.Add(Context.GC.Heap[CONSTRUCTOR_STR]);

			//初始化必须对象
			InitScript((ASScript)Context.OBJECT._link_codescope.Parent.Container, ref error); if (error.raised) { throw new LoaderException("OBJECT init failed"); }
			CreateObjectProto(ref error); if (error.raised) { throw new LoaderException("OBJECT init failed"); }

			InitScript((ASScript)Context.CLASS._link_codescope.Parent.Container, ref error); if (error.raised) { throw new LoaderException("CLASS init failed"); }
			InitScript((ASScript)Context.STRING._link_codescope.Parent.Container, ref error); if (error.raised) { throw new LoaderException("STRING init failed"); }
			CreateStringProto(ref error); if (error.raised) { throw new LoaderException("STRING init failed"); }

			InitScript((ASScript)Context.FUNCTION._link_codescope.Parent.Container, ref error); if (error.raised) { throw new LoaderException("FUNCTION init failed"); }
			CreateFunctionProto(ref error); if (error.raised) { throw new LoaderException("FUNCTION init failed"); }

			InitScript((ASScript)Context.ARRAY._link_codescope.Parent.Container, ref error); if (error.raised) { throw new LoaderException("ARRAY init failed"); }
			CreateArrayProto(ref error); if (error.raised) { throw new LoaderException("ARRAY init failed"); }
			;


			InitScript((ASScript)Context.METHOD_CLOSURE._link_codescope.Parent.Container, ref error); if (error.raised) { throw new LoaderException("METHOD_CLOSURE init failed"); }
			InitScript((ASScript)Context.NUMBER._link_codescope.Parent.Container, ref error); if (error.raised) { throw new LoaderException("NUMBER init failed"); }
			InitScript((ASScript)Context.FLOAT._link_codescope.Parent.Container, ref error); if (error.raised) { throw new LoaderException("FLOAT init failed"); }
			InitScript((ASScript)Context.SBYTE._link_codescope.Parent.Container, ref error); if (error.raised) { throw new LoaderException("SBYTE init failed"); }
			InitScript((ASScript)Context.BYTE._link_codescope.Parent.Container, ref error); if (error.raised) { throw new LoaderException("BYTE init failed"); }
			InitScript((ASScript)Context.SHORT._link_codescope.Parent.Container, ref error); if (error.raised) { throw new LoaderException("SHORT init failed"); }
			InitScript((ASScript)Context.USHORT._link_codescope.Parent.Container, ref error); if (error.raised) { throw new LoaderException("USHORT init failed"); }
			InitScript((ASScript)Context.INT._link_codescope.Parent.Container, ref error); if (error.raised) { throw new LoaderException("INT init failed"); }
			InitScript((ASScript)Context.UINT._link_codescope.Parent.Container, ref error); if (error.raised) { throw new LoaderException("UINT init failed"); }
			InitScript((ASScript)Context.BOOLEAN._link_codescope.Parent.Container, ref error); if (error.raised) { throw new LoaderException("BOOLEAN init failed"); }

			InitScript((ASScript)Context.NAMESPACE._link_codescope.Parent.Container, ref error); if (error.raised) { throw new LoaderException("NAMESPACE init failed"); }
			
			InitScript((ASScript)Context.VEC2._link_codescope.Parent.Container, ref error); if (error.raised) { throw new LoaderException("Vector2 init failed"); }
			InitScript((ASScript)Context.MAT22._link_codescope.Parent.Container, ref error); if (error.raised) { throw new LoaderException("Matrix2x2 init failed"); }


			InitScript((ASScript)Context.ERROR._link_codescope.Parent.Container, ref error); if (error.raised) { throw new LoaderException("ERROR instance init failed"); }
			InitScript((ASScript)Context.TYPE_ERROR._link_codescope.Parent.Container, ref error); if (error.raised) { throw new LoaderException("TYPE_ERROR instance init failed"); }
			InitScript((ASScript)Context.REFERENCE_ERROR._link_codescope.Parent.Container, ref error); if (error.raised) { throw new LoaderException("REFERENCE_ERROR instance init failed"); }
			InitScript((ASScript)Context.ARGEMENT_ERROR._link_codescope.Parent.Container, ref error); if (error.raised) { throw new LoaderException("ARGEMENT_ERROR instance init failed"); }
			InitScript((ASScript)Context.IITERATOR._link_codescope.Parent.Container, ref error); if (error.raised) { throw new LoaderException("IITERATOR instance init failed"); }
			InitScript((ASScript)Context.PROMISE._link_codescope.Parent.Container, ref error); if (error.raised) { throw new LoaderException("PROMISE instance init failed"); }
			InitScript((ASScript)Context.ILLEGALOPERATION_ERROR._link_codescope.Parent.Container, ref error); if (error.raised) { throw new LoaderException("PROMISE instance init failed"); }
			InitScript((ASScript)Context.RANGE_ERROR._link_codescope.Parent.Container, ref error); if (error.raised) { throw new LoaderException("PROMISE instance init failed"); }





			//InitScript((ASScript)Context.SBYTE._link_codescope.Parent.Container, ref error); if (error.raised) { throw new LoaderException(" instance init failed"); }
			EMPTY_STR = Context.GC.AllocString(""); if (EMPTY_STR == 0) { throw new LoaderException("EMPTY_STR alloc failed"); }
			Context.GC.Root.Add(Context.GC.Heap[EMPTY_STR]);
			TRUE_STR = Context.GC.AllocString("true"); if (TRUE_STR == 0) { throw new LoaderException("TRUESTR alloc failed"); }
			Context.GC.Root.Add(Context.GC.Heap[TRUE_STR]);

			FALSE_STR = Context.GC.AllocString("false"); if (FALSE_STR == 0) { throw new LoaderException("FALSESTR alloc failed"); }
			Context.GC.Root.Add(Context.GC.Heap[FALSE_STR]);

			NAN_STR = Context.GC.AllocString("NaN"); if (NAN_STR == 0) { throw new LoaderException("NANPTR alloc failed"); }
			Context.GC.Root.Add(Context.GC.Heap[NAN_STR]);

			ZERO_STR = Context.GC.AllocString("0"); if (ZERO_STR == 0) { throw new LoaderException("ZERO_STR alloc failed"); }
			Context.GC.Root.Add(Context.GC.Heap[ZERO_STR]);

			POSITIVEINF_STR = Context.GC.AllocString("Infinity"); if (POSITIVEINF_STR == 0) { throw new LoaderException("POSITIVEINFPTR alloc failed"); }
			Context.GC.Root.Add(Context.GC.Heap[POSITIVEINF_STR]);

			NEGATIVEINF_STR = Context.GC.AllocString("-Infinity"); if (NEGATIVEINF_STR == 0) { throw new LoaderException("NEGATIVEINFPTR alloc failed"); }
			Context.GC.Root.Add(Context.GC.Heap[NEGATIVEINF_STR]);

			CALLEE_STR = Context.GC.AllocString("callee"); if (CALLEE_STR == 0) { throw new LoaderException("CALLEEPTR alloc failed"); }
			Context.GC.Root.Add(Context.GC.Heap[CALLEE_STR]);

			FUNCTION_TOSTRING_STR = Context.GC.AllocString("function Function() {}"); if (FUNCTION_TOSTRING_STR == 0) { throw new LoaderException("FUNCTION_TOSTRING alloc failed"); }
			Context.GC.Root.Add(Context.GC.Heap[FUNCTION_TOSTRING_STR]);

			OBJECT_FUNCTION_STR = Context.GC.AllocString("[object Function]"); if (OBJECT_FUNCTION_STR == 0) { throw new LoaderException("OBJECT_FUNCTION_STR alloc failed"); }
			Context.GC.Root.Add(Context.GC.Heap[OBJECT_FUNCTION_STR]);

			TYPEOF_boolean_STR = Context.GC.AllocString("boolean"); if (TYPEOF_boolean_STR == 0) { throw new LoaderException("TYPEOF_boolean_STR alloc failed"); }
			Context.GC.Root.Add(Context.GC.Heap[TYPEOF_boolean_STR]);
			TYPEOF_function_STR = Context.GC.AllocString("function"); if (TYPEOF_function_STR == 0) { throw new LoaderException("TYPEOF_function_STR alloc failed"); }
			Context.GC.Root.Add(Context.GC.Heap[TYPEOF_function_STR]);
			TYPEOF_number_STR = Context.GC.AllocString("number"); if (TYPEOF_number_STR == 0) { throw new LoaderException("TYPEOF_number_STR alloc failed"); }
			Context.GC.Root.Add(Context.GC.Heap[TYPEOF_number_STR]);
			TYPEOF_object_STR = Context.GC.AllocString("object"); if (TYPEOF_object_STR == 0) { throw new LoaderException("TYPEOF_object_STR alloc failed"); }
			Context.GC.Root.Add(Context.GC.Heap[TYPEOF_object_STR]);
			TYPEOF_string_STR = Context.GC.AllocString("string"); if (TYPEOF_string_STR == 0) { throw new LoaderException("TYPEOF_string_STR alloc failed"); }
			Context.GC.Root.Add(Context.GC.Heap[TYPEOF_string_STR]);
			TYPEOF_undefined_STR = Context.GC.AllocString("undefined"); if (TYPEOF_undefined_STR == 0) { throw new LoaderException("TYPEOF_undefined_STR alloc failed"); }
			Context.GC.Root.Add(Context.GC.Heap[TYPEOF_undefined_STR]);

			NULL_STR = Context.GC.AllocString("null"); if (NULL_STR == 0) { throw new LoaderException("NULL_STR alloc failed"); }
			Context.GC.Root.Add(Context.GC.Heap[NULL_STR]);

			cache_ERROR_NAME = Context.GC.AllocString("Error"); if (cache_ERROR_NAME == 0) { throw new LoaderException("cache_ERROR_NAME alloc failed"); }
			Context.GC.Root.Add(Context.GC.Heap[cache_ERROR_NAME]);
			cache_STACKOVERFLOW_STR = Context.GC.AllocString("Stack overflow occurred."); if (cache_STACKOVERFLOW_STR == 0) { throw new LoaderException("cache_STACKOVERFLOW_STR alloc failed"); }
			Context.GC.Root.Add(Context.GC.Heap[cache_STACKOVERFLOW_STR]);

			cache_OUTOFMEMORY_STR = Context.GC.AllocString("Out of memory"); if (cache_OUTOFMEMORY_STR == 0) { throw new LoaderException("cache_OUTOFMEMORY_STR alloc failed"); }
			Context.GC.Root.Add(Context.GC.Heap[cache_OUTOFMEMORY_STR]);

			cache_TYPE_ERROR_NAME = Context.GC.AllocString("TypeError"); if (cache_TYPE_ERROR_NAME == 0) { throw new LoaderException("cache_TYPE_ERROR_NAME alloc failed"); }
			Context.GC.Root.Add(Context.GC.Heap[cache_TYPE_ERROR_NAME]);

			cache_Eval_ERROR_NAME = Context.GC.AllocString("EvalError"); if (cache_Eval_ERROR_NAME == 0) { throw new LoaderException("cache_Eval_ERROR_NAME alloc failed"); }
			Context.GC.Root.Add(Context.GC.Heap[cache_Eval_ERROR_NAME]);


			cache_CANNOT_ACCESS_NULL = Context.GC.AllocString("Cannot access a property or method of a null object reference."); if (cache_CANNOT_ACCESS_NULL == 0) { throw new LoaderException("cache_CANNOT_ACCESS_NULL alloc failed"); }
			Context.GC.Root.Add(Context.GC.Heap[cache_CANNOT_ACCESS_NULL]);

			cache_ATERM_UNDEFINED = Context.GC.AllocString("A term is undefined and has no properties."); if (cache_ATERM_UNDEFINED == 0) { throw new LoaderException("cache_ATERM_UNDEFINED alloc failed"); }
			Context.GC.Root.Add(Context.GC.Heap[cache_ATERM_UNDEFINED]);

			cache_MUSTVINALLA = Context.GC.AllocString("Prototype objects must be vanilla Objects."); if (cache_MUSTVINALLA == 0) { throw new LoaderException("cache_MUSTVINALLA alloc failed"); }
			Context.GC.Root.Add(Context.GC.Heap[cache_MUSTVINALLA]);

			cache_Instantiation_non_constructor = Context.GC.AllocString("Instantiation attempted on a non-constructor."); if (cache_Instantiation_non_constructor == 0) { throw new LoaderException("cache_Instantiation_non_constructor alloc failed"); }
			Context.GC.Root.Add(Context.GC.Heap[cache_Instantiation_non_constructor]);


			cache_instanceof_error = Context.GC.AllocString("The right-hand side of instanceof must be a class or function."); if (cache_instanceof_error == 0) { throw new LoaderException("cache_Instantiation_non_constructor alloc failed"); }
			Context.GC.Root.Add(Context.GC.Heap[cache_instanceof_error]);


			cache_REFERENCE_ERROR_NAME = Context.GC.AllocString("ReferenceError"); if (cache_REFERENCE_ERROR_NAME == 0) { throw new LoaderException("cache_REFERENCE_ERROR_NAME alloc failed"); }
			Context.GC.Root.Add(Context.GC.Heap[cache_REFERENCE_ERROR_NAME]);

			cache_ARGEMENT_ERROR_NAME = Context.GC.AllocString("ArgumentError"); if (cache_ARGEMENT_ERROR_NAME == 0) { throw new LoaderException("cache_ARGEMENT_ERROR_NAME alloc failed"); }
			Context.GC.Root.Add(Context.GC.Heap[cache_ARGEMENT_ERROR_NAME]);

			cache_RANGE_ERROR_NAME = Context.GC.AllocString("RangeError"); if (cache_RANGE_ERROR_NAME == 0) { throw new LoaderException("cache_RANGE_ERROR_NAME alloc failed"); }
			Context.GC.Root.Add(Context.GC.Heap[cache_RANGE_ERROR_NAME]);

			cache_ILLEGALOPERATION_ERROR_NAME = Context.GC.AllocString("IllegalOperationError"); if (cache_ILLEGALOPERATION_ERROR_NAME == 0) { throw new LoaderException("cache_ILLEGALOPERATION_ERROR_NAME alloc failed"); }
			Context.GC.Root.Add(Context.GC.Heap[cache_ILLEGALOPERATION_ERROR_NAME]);

			Context.MicroTaskQueue.InitMethods(Context);


			ASClass documentCls = null;
			foreach (var swc in Context.libs)
			{
				foreach (var script in swc.Scripts)
				{
					if (script.Traits[0].Kind == TraitKind.Class)
					{
						if (script.Traits[0].ASMetadata.Count > 0)
						{
							var meta = script.Traits[0].ASMetadata[0];
							if (meta.Name == "Doc" && meta.Items.Count == 0)
							{
								documentCls = script.Traits[0].Class;
								goto found_doc;
							}
						}

					}
				}
			}

		found_doc:
			if (documentCls == null)
			{
				throw new LoaderException("Document Class not found.");
			}




			ASScript sScript = (ASScript)documentCls._link_codescope.Parent.Container;

			InitScript(sScript, ref error);

			if (error.raised)
			{
				if (error.error.ValueType == BoxType.Fault)
				{
					Context.GC.ResetIterContextPool();
				}
				else
				{
#if DEBUG
					if (Context.GC.IterCtxIndex != 0)
						throw new InvalidOperationException();
#endif
				}


				var ex = new PlayerException(this, error.error, Context.errorStack.ToString());
				Context.errorStack.Clear();

				if (onErrorRaised != null)
				{
					onErrorRaised(ex);
				}
			}
			else
			{
#if DEBUG
				if (Context.GC.IterCtxIndex != 0)
					throw new InvalidOperationException();
#endif


			}


			while (true)
			{
				ReceiveError queueError = default;
				Context.AsyncCallbackQueue.RunQueue(Context, ref queueError);
				if (queueError.raised)
				{
					Debug.Assert(queueError.error.ValueType == BoxType.Fault);


					var ex = new PlayerException(this, queueError.error, " AsyncCallbackQueue Fault.");

					Context.errorStack.Clear();

					if (onErrorRaised != null)
					{
						onErrorRaised(ex);
					}

					break;
				}


				ReceiveError timertask_fault = default;
				Context.TimerTaskQueue.RunTimerTasks(Context, DateTime.UtcNow.Ticks, onErrorRaised, ref timertask_fault);
				if (timertask_fault.raised)
				{
					Debug.Assert(timertask_fault.error.ValueType == BoxType.Fault);

					Context.errorStack.Clear();
					break;
				}


				ReceiveError microtask_fault = default;
				Context.MicroTaskQueue.RunMicrotasks(Context, ref microtask_fault);
				if (microtask_fault.raised)
				{
					Debug.Assert(microtask_fault.error.ValueType == BoxType.Fault); //微任务循环会吃掉异常，除非是oom这种。

					var ex = new PlayerException(this, microtask_fault.error, "Run Microtask Fault.");
					Context.errorStack.Clear();

					if (onErrorRaised != null)
					{
						onErrorRaised(ex);
					}

					break;
				}



				if (!_shutdownEvent && (Context.AsyncCallbackQueue.HasPending || Context.TimerTaskQueue.HasWaitingTasks || Context.AsyncCallbackQueue.HasPending))
				{
					Context.AsyncCallbackQueue._wakeEvent.WaitOne(16); //16毫秒检查一次计时器是否过期
				}
				else
				{
					break;
				}
			}


		}

		internal void InitScript(ASScript script, ref ReceiveError error)
		{
			if (script.__global_index__ != 0)
			{
				return;
			}
			Context.GC.CheckGC(ref error);
			//构造script的global对象
			int index = Context.GC.AllocGlobal(script);
			if (index == 0)
			{
				RaiseOutOfMemory(ref error);
				return;
				//throw new NotImplementedException("out of memory");
			}

			script.__global_index__ = index;

			for (int i = 0; i < script.codeScopes.Count; i++)
			{
				var scope = script.codeScopes[i];
				if (scope.Kind == CodeScopeKind.Method)
				{
					ASMethodBody methodBody = (ASMethodBody)scope.Container;
					methodBody.rt__globalindex = index;
				}
			}

			for (int i = 0; i < script.codeScopes.Count; i++)
			{
				var scope = script.codeScopes[i];
				if (scope.Kind == CodeScopeKind.Class)
				{
					InitASClass((ASClass)scope.Container, ref error); if (error.raised) { return; }
				}
				else if (scope.Kind == CodeScopeKind.Script)
				{
					CodeScope scriptScope = scope;
				}
				
			}
			


			//执行script的初始化函数
			NaNBoxing thisPtr = new NaNBoxing();
			thisPtr.SetHeapPtr(index, (byte)RtHeapTypeKind.GLOBAL, (byte)HeapKindFlag.NONE);

			unsafe
			{
				RunMethod(script.Initializer, thisPtr, index, 0, null, null, ref error, -1);
			}


		}


		internal void InitASClass(ASClass cls, ref ReceiveError error)
		{

			if (cls.__instance_index__ != 0)
			{
				return;
			}
			Context.GC.CheckGC(ref error);
			int index = Context.GC.AllocASClassObj(cls, Context.OBJECT.Instance);
			if (index == 0)
			{
				RaiseOutOfMemory(ref error); //此种情况下认为这是不可恢复的错误
				error.error.setFault();
				return;
			}

			cls.__instance_index__ = index;

			//var script = (ASScript)cls._link_codescope.Parent.Container;
			//for (int i = 0; i < script.codeScopes.Count; i++)
			//{
			//	var scope = script.codeScopes[i];
			//	if (scope.Kind == CodeScopeKind.Method)
			//	{
			//		ASMethodBody methodBody = (ASMethodBody)scope.Container;
					
			//		while (scope.Kind != CodeScopeKind.Script)
			//		{
			//			if (scope == cls._link_codescope)
			//			{
			//				methodBody.rt__classindex = index;
			//				break;
			//			}

			//			scope = scope.Parent;
			//		}
			//	}
			//}


			//构造proto的constructor, 就是Class自己。
			var proto = Context.GC.Heap[((RtScriptClass)Context.GC.Heap[index]).PROTO__PTR];
			NaNBoxing constructor = new NaNBoxing(); constructor.SetHeapPtr(index, (byte)RtHeapTypeKind.CLASS, (byte)HeapKindFlag.NONE);
			NaNBoxing v_str = default; v_str.SetHeapPtr(CONSTRUCTOR_STR, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
			CreateDynamic(ref error, proto, v_str, constructor, true, false, true);
			if (error.raised)
			{
				error.error.setFault();
				return;
			}


			if (cls.Instance.Super != null)
			{
				var super = cls.Instance._super_class_; //Context.dictTypeLayouts[cls.Instance.Super].ASType;
														//如果super是本script内部定义的内部类,直接InitASClass,
														//如果super是另一个script定义的类，则Init另一个script。

				ASScript superscript = (ASScript)super._link_codescope.Parent.Container;
				if (superscript == cls._link_codescope.Parent.Container)
				{
					InitASClass(super, ref error);
					if (error.raised)
					{
						return;
					}
				}
				else
				{
					InitScript(superscript, ref error);
					if (error.raised)
						return;
				}

			}

			if (cls.Instance.Flags.HasFlag(ClassFlags.Struct)) //有可能内部成员也是struct
			{
				for (int i = 0; i < cls.Instance.Traits.Count; i++)
				{
					var t = cls.Instance.Traits[i];
					if (t.Kind == TraitKind.Slot && t.__rt_type_class__ != null)
					{
						InitASClass(t.__rt_type_class__, ref error);
						if (error.raised)
						{
							return;
						}
					}

				}

			}

			if (cls.Instance.Flags.HasFlag(ClassFlags.Vector))
			{
				if (cls.Instance._element_class != null) //如果为空则是任意类型
				{
					InitASClass(cls.Instance._element_class, ref error);
				}
			}
			else
			{

				unsafe
				{
					//执行Class的初始化函数
					RunMethod(cls.Constructor, new NaNBoxing(NaNBoxing.NULL), index, 0, null, null, ref error, -1);
				}

			}


		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private unsafe void LoadStackLocater(StackLocater* stacklocatoer, byte** P)
		{
			stacklocatoer->index = *(int*)(*P); (*P) += 4;

			//byte* _p = (byte*)&stacklocatoer->index;
			//*_p++ = *(*P)++;
			//*_p++ = *(*P)++;
			//*_p++ = *(*P)++;
			//*_p = *(*P)++;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private unsafe void LoadInt32(int* value, byte** P)
		{
			*value = *(int*)(*P); (*P) += 4;
			//byte* _p = (byte*)value;
			//*_p++ = *(*P)++;
			//*_p++ = *(*P)++;
			//*_p++ = *(*P)++;
			//*_p = *(*P)++;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private unsafe void LoadUInt(uint* value, byte** P)
		{
			*value = *(uint*)(*P); (*P) += 4;
			//byte* _p = (byte*)value;
			//*_p++ = *(*P)++;
			//*_p = *(*P)++;
		}



		private unsafe void BeforeWriteProperty()
		{
			//目前为空方法，用于标记哪里写了属性，将来优化为检查是否是简单的写字段。
		}

		private void WriteFunctionProto(NaNBoxing box, ref ReceiveError error, RtHeapBase closure, NaNBoxing closure_box)
		{

			if (((ASMethodBody)closure.Type).Method.__ismethod)
			{
				RaiseReferenceError_WriteToReadonlyProperty(ref error, Context.FUNCTION.Instance._vtable.Items[1].Trait.Method.Body, buildin_as_methodclosure);
			}
			else
			{
				if (box.ValueType == NaNBoxing.BoxType.Undefined || box.ValueType == NaNBoxing.BoxType.Null)
				{
					((RtClosure)closure).Set_PROTOTYPE(-1, this);
				}
				else if (box.ValueType == NaNBoxing.BoxType.HeapPtr
					&&
					((box.HeapFlag & (byte)HeapKindFlag.FLAG_STRUCT) == 0) //阻止Struct作为prototype
					)
				{
					//访问prototype.这时候只能把function保存到堆里了。
					//NaNBoxing v = new NaNBoxing(); v.SetHeapPtr(closure_ptr);
					NaNBoxing v = closure_box; //new NaNBoxing(); v.SetHeapPtr(closure_ptr);
					NaNBoxing clouse_heap = GetSaveValue(v, ref error);
					if (error.raised)
					{
						return;
					}


					switch ((RtHeapTypeKind)box.HeapKind)//Context.GC.Heap[box.HeapPtr].Kind)
					{
						case RtHeapTypeKind.CLASS:
						case RtHeapTypeKind.GLOBAL:
						case RtHeapTypeKind.ARRAY:
						case RtHeapTypeKind.INSTANCE:
						case RtHeapTypeKind.CLOSURE:
						case RtHeapTypeKind.VECTOR:

							box = GetSaveValue(box, ref error); // 保存到prototype,必须先保存。
							if (error.raised)
							{
								return;
							}




							((RtClosure)closure).Set_PROTOTYPE(box.HeapPtr, this);
							break;
						case RtHeapTypeKind.NAMESPACE:
						case RtHeapTypeKind.STRING:
							RaiseTypeError_MustVinallaObject(ref error);
							break;
						default:
#if DEBUG
							throw new InvalidOperationException();
#else
							Environment.FailFast("出错了，这里跑不到"); break;
#endif

					}

				}
				else
				{
					RaiseTypeError_MustVinallaObject(ref error);
				}

				//throw new NotImplementedException();
			}
		}

		private unsafe NaNBoxing InvokeReadProperty(ref ReceiveError error, NaNBoxing thisValue, int vtable_index,  Span<NaNBoxing> stackslots, int returnSlotIndex)
		{
			if (thisValue.ValueType == BoxType.LocalString)
			{
				Debug.Assert(vtable_index == 0);

				Span<char> buffer = stackalloc char[16];
				int len = thisValue.GetLocalStringChars(buffer);

				NaNBoxing result = default;
				result.SetInt(len);

				return result;
			}
			else if (thisValue.ValueType != NaNBoxing.BoxType.HeapPtr)
			{
				//讲道理，Number,Boolean之类好像没有属性
#if DEBUG
				throw new InvalidOperationException();
#else
				Environment.FailFast("出错了，这里跑不到"); return default;
#endif
			}
			else
			{
				//RtHeapBase ins = Context.GC.Heap[thisValue.HeapPtr];
				RtHeapTypeKind insKind = (RtHeapTypeKind)thisValue.HeapKind;
				if (insKind == RtHeapTypeKind.INSTANCE
					||
					insKind == RtHeapTypeKind.ARRAY
					||
					insKind == RtHeapTypeKind.VECTOR
					)
				{
					RtHeapBase ins = Context.GC.Heap[thisValue.HeapPtr];
					var vtableitem = ins.Type._vtable.Items[vtable_index];
					var function = vtableitem.Trait.Method;

					//var define = (ASInstance)vtableitem.DefineAt;

					NaNBoxing result = RunMethod(function,
					thisValue, thisValue.HeapPtr, 0, null, stackslots, ref error, returnSlotIndex);

					return result;

				}
				else if (insKind == RtHeapTypeKind.NAMESPACE)
				{
					var vtableitem = Context.NAMESPACE.Instance._vtable.Items[vtable_index];
					var function = vtableitem.Trait.Method;

					//var define = (ASInstance)vtableitem.DefineAt;

					NaNBoxing result = RunMethod(function,
					thisValue, thisValue.HeapPtr, 0, null, stackslots, ref error, returnSlotIndex);

					return result;
				}
				else if (thisValue.HeapPtr == Context.CLASS.__instance_index__)
				{
#if DEBUG
					if (vtable_index != 0)
						throw new InvalidOperationException();
#endif
					RtHeapBase ins = Context.GC.Heap[thisValue.HeapPtr];
					NaNBoxing result = new NaNBoxing();
					result.SetHeapPtr(((RtScriptClass)ins).PROTO__PTR, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.NONE);

					Debug.Assert(Context.GC.Heap[((RtScriptClass)ins).PROTO__PTR].Kind == RtHeapTypeKind.INSTANCE);

					return result;

				}
				else if (insKind == RtHeapTypeKind.STRING)
				{
					var vtableitem = Context.STRING.Instance._vtable.Items[vtable_index];
					var function = vtableitem.Trait.Method;

					//var define = (ASInstance)vtableitem.DefineAt;

					NaNBoxing result = RunMethod(function,
					thisValue, thisValue.HeapPtr, 0, null, stackslots, ref error, returnSlotIndex);

					return result;
				}
				else if (insKind == RtHeapTypeKind.CLASS)
				{
					RtHeapBase ins = Context.GC.Heap[thisValue.HeapPtr];
					var @class = ((RtScriptClass)ins).Meta;
					var function = @class._vtable.Items[vtable_index].Trait.Method;

					if (vtable_index == 0)
					{
#if DEBUG
						if (function.Name != "prototype")
						{
							throw new InvalidOperationException();
						}
#endif

						NaNBoxing result = new NaNBoxing();
						result.SetHeapPtr(((RtScriptClass)ins).PROTO__PTR, ((ASClass)@class).Type_identifier == (ulong)TypeKind.Function ? (byte)RtHeapTypeKind.CLOSURE : (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.NONE);

						Debug.Assert((byte)Context.GC.Heap[((RtScriptClass)ins).PROTO__PTR].Kind == result.HeapKind);

						return result;
					}
					else
					{

						NaNBoxing result = RunMethod(function,
							thisValue, thisValue.HeapPtr, 0, null, stackslots, ref error, returnSlotIndex);

						//if (error.raised)
						//{
						//    goto flag_handle_error;
						//}

						//stackslots[target.index] = result;

						return result;
					}
				}
				else if (insKind == RtHeapTypeKind.CLOSURE)
				{
					RtHeapBase ins = Context.GC.Heap[thisValue.HeapPtr];
					var vtableitem = Context.FUNCTION.Instance._vtable.Items[vtable_index];
					var function = vtableitem.Trait.Method;

					if (vtable_index == 4)
					{
#if DEBUG
						if (function.Name != "length")
						{
							throw new InvalidOperationException();
						}
#endif

						NaNBoxing r = new NaNBoxing();
						r.SetInt(((ASMethodBody)ins.Type).Method.Parameters.Count - (((ASMethodBody)ins.Type).Method.Flags.HasFlag(MethodFlags.NeedRest) ? 1 : 0));
						return r;
					}
					else if (vtable_index == 0)
					{
#if DEBUG
						if (function.Name != "prototype")
						{
							throw new InvalidOperationException();
						}
#endif
						if (((ASMethodBody)ins.Type).Method.__ismethod)
						{
							NaNBoxing r = default;
							//r.SetNull();
							r.SetUndefined();
							return r;
						}
						else
						{


							var closure = (RtClosure)ins;

							int proto = closure.PROTOTYPE(this);
							if (proto < 0) //被手动置为undefined.
							{
								NaNBoxing r = default;
								r.SetUndefined();
								return r;
							}
							else if (proto == 0)
							{
								//这了这一步，必须将function保存到堆里了，因为prototype还有一个constructor的属性指向function.

								Context.GC.CheckGC(ref error);

								NaNBoxing closure_heap = GetSaveValue(thisValue, ref error);
								if (error.raised)
								{
									return default;
								}


								RtHeapBase prototype;
								int ptr = Context.GC.AllocInstance(Context.OBJECT.Instance, out prototype);
								if (ptr == 0)
								{
									RaiseOutOfMemory(ref error);
									return default;
								}
								else
								{
									NaNBoxing v_str = default; v_str.SetHeapPtr(CONSTRUCTOR_STR, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
									//创建constructor   这个属性可以被删除。
									CreateDynamic(ref error, prototype, v_str, closure_heap, true, false, true);
									if (error.raised)
									{
										return default;
									}

									closure.Set_PROTOTYPE(ptr, this);
									NaNBoxing r = default;
									r.SetHeapPtr(ptr, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.NONE);
									return r;
								}

							}
							else
							{
								NaNBoxing r = default;

								var protoobj = Context.GC.Heap[proto];

								r.SetHeapPtr(proto, (byte)protoobj.Kind, (byte)(protoobj.Kind == RtHeapTypeKind.INSTANCE ? (((ASInstance)protoobj.Type).Flags.HasFlag(ClassFlags.Struct) ? HeapKindFlag.FLAG_STRUCT : HeapKindFlag.NONE) : HeapKindFlag.NONE));
								return r;
							}
						}
					}
					else
					{

						//var define = (ASInstance)vtableitem.DefineAt;

						NaNBoxing result = RunMethod(function,
						thisValue, thisValue.HeapPtr, 0, null, stackslots, ref error, returnSlotIndex);

						return result;
					}
				}
				else
				{
#if DEBUG
					throw new InvalidOperationException();
#else
					Environment.FailFast("出错了，这里跑不到"); return default;
#endif
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal NaNBoxing LoadSlotFromArray(uint uindex, RtHeapBase arrObj, out bool isoutofindex_or_ishole)
		{
			uint len = ((RtArray)arrObj).GetLength(this, out RtArray target);

			NaNBoxing result = default;

			if (uindex < len)
			{
				result = ((RtArray)target).ReadSlot(uindex, this, out isoutofindex_or_ishole);
			}
			else
			{
				isoutofindex_or_ishole = true;
			}

			if (isoutofindex_or_ishole) //如果索引超出了length或者是一个洞则查找原型链。。。
			{
				NaNBoxing sname = default; sname.SetUInt(uindex);
				ReadOnlySpan<char> searchName = Extensions.GetPrimitiveValueToString(this, sname, stackalloc char[120]);  //uindex.ToString();
				NaNBoxing value; int shape_ptr; int index; RtDynamic prop;

				if (FindDynamicValue(target, searchName, out value, out shape_ptr, out index, out prop))
				{
					result = value;
				}
				else
				{
					var proto = Context.GC.Heap[((RtScriptClass)Context.GC.Heap[Context.ARRAY.__instance_index__]).PROTO__PTR];
				lbl_searh_class_proto:
					if (FindDynamicValue(proto, searchName, out value, out shape_ptr, out index, out prop))
					{
						result = value;
					}
					else
					{
						int p = ((RtInstance)proto).PROTOTYPE(this, (ASInstance)proto.Type); //class的proto肯定是instance。
						if (p != 0)
						{
							proto = Context.GC.Heap[p];
							goto lbl_searh_class_proto;
						}
						result.setFault();
					}
				}

			}
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void SetArraySlot(NaNBoxing box, uint index, RtHeapBase instance, ref ReceiveError error)
		{
			((RtArray)instance).GetLength(this, out RtArray target);

			if (target.TrySetSlotIfReplaceStructOrNotHeap(box, index, this,out target, ref error))
			{

			}
			else
			{
				if (error.raised)
				{
					return;
				}

				Context.GC.CheckGC(ref error);
				box = GetSaveValue(box, ref error);
				if (error.raised)
				{
					return;
				}

				target.SetSlot(box, index, this, ref error);
				if (error.raised)
				{
					return;
				}
			}
		}


		internal void VisitArrayProto(RtHeapBase arrObj, Action<NaNBoxing, NaNBoxing> OnVisit)
		{
			VisitDynamicValue(arrObj, OnVisit);

			var proto = Context.GC.Heap[((RtScriptClass)Context.GC.Heap[Context.ARRAY.__instance_index__]).PROTO__PTR];

		lbl_searh_class_proto:
			VisitDynamicValue(proto, OnVisit);
			int p = ((RtInstance)proto).PROTOTYPE(this, (ASInstance)proto.Type); //class的proto肯定是instance。
			if (p != 0)
			{
				proto = Context.GC.Heap[p];
				goto lbl_searh_class_proto;
			}

		}




		

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private NaNBoxing Load_Indexer(NaNBoxing indexer_key,NaNBoxing RefInstance ,ref ReceiveError error,int returnslot)
		{
			NaNBoxing result = default;
			{
				RtHeapBase refObj = Context.GC.Heap[RefInstance.HeapPtr];

				Debug.Assert(((ASInstance)refObj.Type).Flags.HasFlag(ClassFlags.Indexer));

				if (Context.StackPosition + 1 >= Context.STACK_LENGTH)
				{
					RaiseStackOverflow(ref error);
					return default;
				}

				var argSpan = Context.StackSlots.AsSpan(Context.StackPosition, 1);
				argSpan[0] = indexer_key;
				StackLocater argLoc = new StackLocater() { index = 0 };

				Context.StackPosition++;

				//NaNBoxing _this = new NaNBoxing();
				//_this.SetHeapPtr(_obj.RefInstance.HeapPtr);
				NaNBoxing _this = RefInstance;

				unsafe
				{
					//Context.StackPosition++;

					RunMethod(((ASInstance)refObj.Type).indexer_get, _this,
						RefInstance.HeapPtr, 1, (byte*)&argLoc, argSpan, ref error, returnslot);

					result = Context.StackSlots[returnslot];

					//Context.StackPosition--;
				}

				Context.StackPosition--;
				if (error.raised)
				{
					return default;
				}
			}
			
			if (result.ValueType == BoxType.Fault) //表示需要继续原型链查找
			{
				

				Span<char> buffers = stackalloc char[128];
				ReadOnlySpan<char> searchName = buffers;

				Debug.Assert(Context.StackPosition + 1 < Context.STACK_LENGTH);

				int basepos = Context.StackPosition;
				Context.StackPosition++;

				if (IsPrimitive(indexer_key))
				{
					searchName = Extensions.GetPrimitiveValueToString(this, indexer_key, buffers);
				}
				else
				{
					
					
					ConvertValueType(ref error, indexer_key, TypeKind.String, Context.STRING, ref Context.StackSlots[Context.StackPosition - 1]);
					
					if (error.raised)
					{
						Context.StackPosition = basepos;
						return default;
					}

					searchName = Extensions.GetPrimitiveValueToString(this, Context.StackSlots[Context.StackPosition], buffers);

					//throw new NotImplementedException();
				}

				RtHeapBase refObj = Context.GC.Heap[RefInstance.HeapPtr];
				NaNBoxing value; int shape_ptr; int index; RtDynamic prop;
				var proto = Context.GC.Heap[((RtScriptClass)Context.GC.Heap[refObj.Type._link_codescope.TypeLayout.ASType.__instance_index__]).PROTO__PTR];
			lbl_searh_class_proto:
				if (FindDynamicValue(proto, searchName, out value, out shape_ptr, out index, out prop))
				{
					result = value;
				}
				else
				{
					Debug.Assert(((RtInstance)proto).HEAPINSTANCE_PTR == 0); //proto不可能在缓存里

					int p = ((RtInstance)proto).PROTOTYPE(this, (ASInstance)proto.Type); //class的proto肯定是instance。
					if (p != 0)
					{
						proto = Context.GC.Heap[p];
						goto lbl_searh_class_proto;
					}

					result.SetUndefined();

				
				}

				Context.StackPosition = basepos;
			}
			return result;
		}


		private unsafe NaNBoxing LoadValue_Slow(RtStackCache _obj, int callee_slotindex, ref ReceiveError error, Span<NaNBoxing> stackslots, int returnSlotIndex)
		{


			NaNBoxing result = default;

			{

				if (_obj.RefInstance.ValueType == BoxType.HeapPtr)
				{


					if (_obj.indexer_key.ValueType != NaNBoxing.BoxType.Fault)
					{
						Debug.Assert(_obj.RefInstance.HeapKind != (byte)RtHeapTypeKind.ARRAY);

						//						if (_obj.RefInstance.HeapKind == (byte)RtHeapTypeKind.ARRAY)// refObj.Kind == RtHeapTypeKind.ARRAY) //通过索引下标操作
						//						{
						//#if DEBUG
						//							if (_obj.indexer_key.ValueType != NaNBoxing.BoxType.Uint || !(_obj.trait[0] == null && _obj.trait[1] == null))
						//							{
						//								throw new InvalidOperationException();
						//							}


						//#endif
						//							RtHeapBase refObj = Context.GC.Heap[_obj.RefInstance.HeapPtr];
						//							bool isoutofindex_or_ishole;
						//							var v = LoadSlotFromArray(_obj.indexer_key.UIntValue, refObj, out isoutofindex_or_ishole);

						//							if (v.ValueType == BoxType.Fault)
						//							{
						//								v.SetUndefined();
						//							}
						//							else if (v.IsStruct())//v.ValueType == BoxType.HeapPtr && v.HeapKind == (byte)RtHeapTypeKind.INSTANCE && v.HeapFlag &)
						//							{
						//								v.SetHeapPtr(v.HeapPtr, (byte)RtHeapTypeKind.INSTANCE, (byte)(HeapKindFlag.FLAG_STRUCT | HeapKindFlag.FLAG_REFSTRUCT));

						//							}

						//							result = v;


						//						}
						//						else
						{
#if DEBUG
							{
								RtHeapBase refObj = Context.GC.Heap[_obj.RefInstance.HeapPtr];
								if (!(
										(refObj.Kind == RtHeapTypeKind.INSTANCE && ((ASInstance)refObj.Type).Flags.HasFlag(ClassFlags.Indexer))
										||
										refObj.Kind == RtHeapTypeKind.VECTOR

									)
									)
								{
									throw new InvalidOperationException();
								}
							}
#endif

							Debug.Assert(_obj.RefInstance.HeapKind != (byte)RtHeapTypeKind.VECTOR);

							//							if (_obj.RefInstance.HeapKind == (byte)RtHeapTypeKind.VECTOR)//refObj.Kind == RtHeapTypeKind.VECTOR)
							//							{
							//#if DEBUG
							//								if (!RtVector.IsValidIndexType(_obj.indexer_key))
							//								{
							//									throw new InvalidOperationException();
							//								}
							//								else
							//#endif
							//								{
							//									RtVector vector;
							//									int v_ptr = RtVector.FindAndUpdateHeapInstancePtr(_obj.RefInstance.HeapPtr, this, out vector);
							//									//int maxlen; int validid;
							//									var store = vector.GetStore();
							//									if (!(store.IsValidIndexRange(_obj.indexer_key, out int validid)))
							//									{
							//										Span<char> buffers = stackalloc char[128];
							//										RaiseRangeError(ref error, Extensions.GetPrimitiveValueToString(this, _obj.indexer_key, buffers), store.length);
							//										return default;
							//									}
							//									else
							//									{
							//										return store.ReadSlot(vector.element_type, validid, this, v_ptr, returnSlotIndex, vector.element_asclass);
							//										//return vector.ReadSlot(validid, this, returnSlotIndex, v_ptr);
							//										//throw new NotImplementedException();
							//									}
							//								}
							//							}
							//							else
							result = Load_Indexer(_obj.indexer_key,_obj.RefInstance, ref error,returnSlotIndex);


							//throw new NotImplementedException();
						}
					}
					else if (_obj.searchPropertyName.ValueType == BoxType.HeapPtr || _obj.searchPropertyName.ValueType == BoxType.LocalString) //动态属性
					{
						Context.GC.CheckGC(ref error);

						//string searchName = ((RtPayloadString)Context.GC.Heap[_obj.searchPropertyNamePtr]).Str;



						Span<char> temp = stackalloc char[16];
						ReadOnlySpan<char> searchName = temp;

						if (_obj.searchPropertyName.ValueType == BoxType.LocalString)
						{
							int l = _obj.searchPropertyName.GetLocalStringChars(temp);
							searchName = searchName.Slice(0, l);

						}
						else
						{
							searchName = ((RtString)Context.GC.Heap[_obj.searchPropertyName.HeapPtr]).Str.AsSpan();
						}




						NaNBoxing ns = new NaNBoxing();
						ASNamespace @namespace = null;
						if (_obj.searchNameSpacePtr > 0)
						{
							ns.SetHeapPtr(_obj.searchNameSpacePtr, (byte)RtHeapTypeKind.NAMESPACE, (byte)HeapKindFlag.NONE);
							RtHeapBase ns_instance = Context.GC.Heap[_obj.searchNameSpacePtr];
							@namespace = ((RtNameSpace)ns_instance).ASNamespace;

						}

						RtHeapBase refObj = Context.GC.Heap[_obj.RefInstance.HeapPtr];
						if (refObj.Kind == RtHeapTypeKind.INSTANCE
							&&
								(
									(((ASInstance)refObj.Type).Flags & ClassFlags.Sealed) == ClassFlags.Sealed
									||
									(
										@namespace != null &&
										@namespace.Kind != NamespaceKind.Package
									)

								)
							)
						{
							if (@namespace != null)
							{
								RaiseReferenceError_RTQNameNotFound(ref error, ns, searchName, _obj.RefInstance);
							}
							else
							{
								//到class的prototype里查找，再找不到就到Object的prototype里查找
								var proto = Context.GC.Heap[((RtScriptClass)Context.GC.Heap[refObj.Type._link_codescope.TypeLayout.ASType.__instance_index__]).PROTO__PTR];
							lbl_searh_class_proto:
								NaNBoxing value; int shape_ptr; int index; RtDynamic prop;
								if (FindDynamicValue(proto, searchName, out value, out shape_ptr, out index, out prop))
								{
									result = value;
								}
								else
								{
									int p = ((RtInstance)proto).PROTOTYPE(this, (ASInstance)proto.Type); //class的proto肯定是instance。
									if (p != 0)
									{
										proto = Context.GC.Heap[p];
										goto lbl_searh_class_proto;
									}

									RaiseReferenceError_MulitNameNotFound(ref error, searchName, _obj.as_type != null ? _obj.as_type.QName : refObj.Type.QName);
								}
								//throw new NotImplementedException("原型链查找");

							}
						}
						else if (refObj.Kind == RtHeapTypeKind.NAMESPACE)
						{
							if (@namespace != null)
							{
								RaiseReferenceError_RTQNameNotFound(ref error, ns, searchName, _obj.RefInstance);
							}
							else
							{
								//到class的prototype里查找，再找不到就到Object的prototype里查找
								var proto = Context.GC.Heap[((RtScriptClass)Context.GC.Heap[Context.NAMESPACE.__instance_index__]).PROTO__PTR];
							lbl_searh_class_proto:
								NaNBoxing value; int shape_ptr; int index; RtDynamic prop;
								if (FindDynamicValue(proto, searchName, out value, out shape_ptr, out index, out prop))
								{
									result = value;
								}
								else
								{
									int p = ((RtInstance)proto).PROTOTYPE(this, (ASInstance)proto.Type); //class的proto肯定是instance。
									if (p != 0)
									{
										proto = Context.GC.Heap[p];
										goto lbl_searh_class_proto;
									}

									RaiseReferenceError_MulitNameNotFound(ref error, searchName, _obj.as_type != null ? _obj.as_type.QName : refObj.Type.QName);
								}
							}
						}
						else if (refObj.Kind == RtHeapTypeKind.VECTOR)
						{
							if (@namespace != null)
							{
								RaiseReferenceError_RTQNameNotFound(ref error, ns, searchName, _obj.RefInstance);
							}
							else
							{
								var proto = Context.GC.Heap[((RtScriptClass)Context.GC.Heap[refObj.Type._link_codescope.TypeLayout.ASType.__instance_index__]).PROTO__PTR];
							lbl_searh_class_proto:
								NaNBoxing value; int shape_ptr; int index; RtDynamic prop;
								if (FindDynamicValue(proto, searchName, out value, out shape_ptr, out index, out prop))
								{
									result = value;
								}
								else
								{
									int p = ((RtInstance)proto).PROTOTYPE(this, (ASInstance)proto.Type); //class的proto肯定是instance。
									if (p != 0)
									{
										proto = Context.GC.Heap[p];
										goto lbl_searh_class_proto;
									}

									RaiseReferenceError_MulitNameNotFound(ref error, searchName, refObj.Type.QName);
								}
							}
						}
						else if (refObj.Kind == RtHeapTypeKind.STRING)
						{
							if (@namespace != null)
							{
								RaiseReferenceError_RTQNameNotFound(ref error, ns, searchName, _obj.RefInstance);
							}
							else
							{
								var proto = Context.GC.Heap[((RtScriptClass)Context.GC.Heap[Context.STRING.__instance_index__]).PROTO__PTR];
							lbl_searh_class_proto:
								NaNBoxing value; int shape_ptr; int index; RtDynamic prop;
								if (FindDynamicValue(proto, searchName, out value, out shape_ptr, out index, out prop))
								{
									result = value;
								}
								else
								{
									int p = ((RtInstance)proto).PROTOTYPE(this, (ASInstance)proto.Type); //class的proto肯定是instance。
									if (p != 0)
									{
										proto = Context.GC.Heap[p];
										goto lbl_searh_class_proto;
									}

									RaiseReferenceError_MulitNameNotFound(ref error, searchName, refObj.Type.QName);
								}
							}
						}
						else if (refObj.Kind == RtHeapTypeKind.ARRAY &&
								((RtArray)refObj).isArguments()
								&& @namespace == null
								&& "callee".AsSpan().CompareTo(searchName, StringComparison.Ordinal) == 0
							)
						{
							result = Context.StackSlots[callee_slotindex];
						}
						else if (refObj.Kind == RtHeapTypeKind.CLOSURE)
						{
							if (@namespace != null)
							{
								RaiseReferenceError_RTQNameNotFound(ref error, ns, searchName, _obj.RefInstance);
							}
							else
							{
								NaNBoxing value; int shape_ptr; int index; RtDynamic prop;
								RtHeapBase proto = null;
								do
								{
								lbl_retry_method:  //Function.prototype 是一个function,所以这里有可能重新回来

									if (!((ASMethodBody)refObj.Type).Method.__ismethod)
									{
										if (FindDynamicValue(refObj, searchName, out value, out shape_ptr, out index, out prop))
										{
											result = value;
											break;
										}
										else
										{
											int f_proto = ((RtScriptClass)Context.GC.Heap[Context.FUNCTION.__instance_index__]).PROTO__PTR;
											if (f_proto <= 0)
											{
												proto = Context.GC.Heap[((RtScriptClass)Context.GC.Heap[Context.OBJECT.__instance_index__]).PROTO__PTR];
											}
											else
											{
												proto = Context.GC.Heap[f_proto];
												if (refObj == proto) //Function.prototyoe是一个function,于是又转回来，这时转到OBJECT
												{
													proto = Context.GC.Heap[((RtScriptClass)Context.GC.Heap[Context.OBJECT.__instance_index__]).PROTO__PTR];

												}
											}
										}
									}
									else
									{
										proto = Context.GC.Heap[((RtScriptClass)Context.GC.Heap[Context.METHOD_CLOSURE.__instance_index__]).PROTO__PTR];
									}

								lbl_searh_class_proto:

									if (FindDynamicValue(proto, searchName, out value, out shape_ptr, out index, out prop))
									{
										result = value;
									}
									else
									{
										if (proto.Kind == RtHeapTypeKind.CLOSURE)
										{
											refObj = proto;
											goto lbl_retry_method;
										}

										int p = ((RtInstance)proto).PROTOTYPE(this, (ASInstance)proto.Type); //class的proto肯定是instance。
										if (p != 0)
										{
											proto = Context.GC.Heap[p];
											goto lbl_searh_class_proto;
										}

										if (((ASMethodBody)refObj.Type).Method.__ismethod)
										{
											RaiseReferenceError_MulitNameNotFound(ref error, searchName, buildin_as_methodclosure);
										}
										else
										{
											result.SetUndefined();
										}
									}

								} while (false);


							}
						}
#if DEBUG
						else if (refObj.Kind == RtHeapTypeKind.DYNAMIC_PROPERTYS || refObj.Kind == RtHeapTypeKind.SHAPE || refObj.Kind == RtHeapTypeKind.STACK_CACHE_OBJ)
						{
							throw new InvalidOperationException();
						}
#endif
						else
						{

							if (@namespace != null)
							{
								RaiseReferenceError_RTQNameNotFound(ref error, ns, searchName, _obj.RefInstance);
							}
							else
							{
								NaNBoxing value; int shape_ptr; int index; RtDynamic prop;

								int step = 0;

							lbl_instance_search_proto:

								if (step++ < 32)
								{

									if (refObj.Kind == RtHeapTypeKind.VECTOR)
									{
										refObj = Context.GC.Heap[((RtScriptClass)Context.GC.Heap[refObj.Type._link_codescope.TypeLayout.ASType.__instance_index__]).PROTO__PTR];
									}

									if (FindDynamicValue(refObj, searchName, out value, out shape_ptr, out index, out prop))
									{
										result = value;
									}
									else
									{
										if (refObj.Kind == RtHeapTypeKind.GLOBAL)
										{
											var proto = ((RtScriptClass)Context.GC.Heap[Context.OBJECT.__instance_index__]).PROTO__PTR;

											if (FindDynamicValue(Context.GC.Heap[proto], searchName, out value, out shape_ptr, out index, out prop))
											{
												result = value;
											}
											else
											{
												result.SetUndefined();
											}
										}
										else if (refObj.Kind == RtHeapTypeKind.ARRAY)
										{
											var proto = Context.GC.Heap[((RtScriptClass)Context.GC.Heap[Context.ARRAY.__instance_index__]).PROTO__PTR];
										lbl_searh_class_proto:
											if (FindDynamicValue(proto, searchName, out value, out shape_ptr, out index, out prop))
											{
												result = value;
											}
											else
											{
												int p = ((RtInstance)proto).PROTOTYPE(this, (ASInstance)proto.Type); //class的proto肯定是instance。
												if (p != 0)
												{
													proto = Context.GC.Heap[p];
													goto lbl_searh_class_proto;
												}

												result.SetUndefined();
											}
										}
										else if (refObj.Kind == RtHeapTypeKind.CLASS)
										{

											var proto = Context.GC.Heap[((RtScriptClass)Context.GC.Heap[Context.CLASS.__instance_index__]).PROTO__PTR];
										lbl_searh_class_proto:
											if (FindDynamicValue(proto, searchName, out value, out shape_ptr, out index, out prop))
											{
												result = value;
											}
											else
											{
												int p = ((RtInstance)proto).PROTOTYPE(this, (ASInstance)proto.Type); //class的proto肯定是instance。
												if (p != 0)
												{
													proto = Context.GC.Heap[p];
													goto lbl_searh_class_proto;
												}

												result.SetUndefined();
												//RaiseReferenceError_MulitNameNotFound(ref error, searchName, refObj.Type.QName);
											}



										}
										else if (refObj.Kind == RtHeapTypeKind.INSTANCE)
										{
											int protoptr = ((RtInstance)refObj).PROTOTYPE(this, (ASInstance)refObj.Type);
											if (protoptr == 0)
											{
												result.SetUndefined();
											}
											else
											{
												refObj = Context.GC.Heap[protoptr];
												goto lbl_instance_search_proto;
											}
										}
										else if (refObj.Kind == RtHeapTypeKind.CLOSURE)
										{
											if (!((ASMethodBody)refObj.Type).Method.__ismethod)
											{
												if (FindDynamicValue(refObj, searchName, out value, out shape_ptr, out index, out prop))
												{
													result = value;
												}
												else
												{
													int protoptr = ((RtClosure)refObj).PROTOTYPE(this);
													if (protoptr <= 0)
													{
														protoptr = ((RtScriptClass)Context.GC.Heap[Context.FUNCTION.__instance_index__]).PROTO__PTR;

														if (protoptr <= 0)
														{
															refObj = Context.GC.Heap[((RtScriptClass)Context.GC.Heap[Context.OBJECT.__instance_index__]).PROTO__PTR];
														}
														else
														{
															refObj = Context.GC.Heap[protoptr];
														}
													}
													else
													{
														refObj = Context.GC.Heap[((RtClosure)refObj).PROTOTYPE(this)];
													}


													goto lbl_instance_search_proto;
												}
											}
											else
											{
												refObj = Context.GC.Heap[((RtScriptClass)Context.GC.Heap[Context.METHOD_CLOSURE.__instance_index__]).PROTO__PTR];
												goto lbl_instance_search_proto;
											}
										}
										else
										{
											throw new InvalidOperationException("原型链查找");
										}
									}

								}
								else
								{
									result.SetUndefined();
								}

							}
						}

					}

					else if (_obj.trait[0] == null && _obj.trait[1] != null)
					{
						RaiseReferenceError_WriteToReadonlyProperty(ref error, _obj.trait[1].Method.Body, _obj.as_type.QName);
					}
					else if (_obj.trait[0].Kind == TraitKind.Slot || _obj.trait[0].Kind == TraitKind.Constant)
					{
						RtHeapBase refObj = Context.GC.Heap[_obj.RefInstance.HeapPtr];
						if (refObj.Kind == RtHeapTypeKind.GLOBAL || refObj.Kind == RtHeapTypeKind.CLASS)
						{
							result = ((RtScriptClass)refObj).ReadSlot(_obj.scopemember_index);
						}
						else if (refObj.Kind == RtHeapTypeKind.INSTANCE)
						{
							result = ((RtInstance)refObj).ReadSlot(_obj.scopemember_index, this, returnSlotIndex, _obj.RefInstance.HeapPtr);
						}
						else
						{
#if DEBUG
							throw new InvalidOperationException();
#else
							Environment.FailFast("出错了，这里跑不到") ; return default;
#endif
						}
					}
#if DEBUG
					else if (_obj.trait[0].Kind == TraitKind.Method && _obj.trait[1] == null)
					{
						throw new InvalidOperationException("Method不会构造到STACK_CACHE_OBJ中");

					}
#endif
					else
					{
						//NaNBoxing instance = new NaNBoxing();
						//instance.SetHeapPtr(_obj.RefInstance.HeapPtr);

						var instance = _obj.RefInstance;
						result = InvokeReadProperty(ref error, instance, _obj.g_index,  stackslots, returnSlotIndex);

						//throw new NotImplementedException("属性的引用未实现");
					}
				}
				else if (_obj.RefInstance.ValueType == BoxType.LocalString)
				{

					if (_obj.searchPropertyName.ValueType == BoxType.HeapPtr || _obj.searchPropertyName.ValueType == BoxType.LocalString) //动态属性
					{
						Context.GC.CheckGC(ref error);

						Span<char> temp = stackalloc char[16];
						ReadOnlySpan<char> searchName = temp;

						if (_obj.searchPropertyName.ValueType == BoxType.LocalString)
						{
							int l = _obj.searchPropertyName.GetLocalStringChars(temp);
							searchName = temp.Slice(0, l);

						}
						else
						{
							searchName = ((RtString)Context.GC.Heap[_obj.searchPropertyName.HeapPtr]).Str.AsSpan();
						}

						NaNBoxing ns = new NaNBoxing();
						ASNamespace @namespace = null;
						if (_obj.searchNameSpacePtr > 0)
						{
							ns.SetHeapPtr(_obj.searchNameSpacePtr, (byte)RtHeapTypeKind.NAMESPACE, (byte)HeapKindFlag.NONE);
							RtHeapBase ns_instance = Context.GC.Heap[_obj.searchNameSpacePtr];
							@namespace = ((RtNameSpace)ns_instance).ASNamespace;

						}



						if (@namespace != null)
						{
							RaiseReferenceError_RTQNameNotFound(ref error, ns, searchName, _obj.RefInstance);
						}
						else
						{
							var proto = Context.GC.Heap[((RtScriptClass)Context.GC.Heap[Context.STRING.__instance_index__]).PROTO__PTR];
						lbl_searh_class_proto:
							NaNBoxing value; int shape_ptr; int index; RtDynamic prop;
							if (FindDynamicValue(proto, searchName, out value, out shape_ptr, out index, out prop))
							{
								result = value;
							}
							else
							{
								int p = ((RtInstance)proto).PROTOTYPE(this, (ASInstance)proto.Type); //class的proto肯定是instance。
								if (p != 0)
								{
									proto = Context.GC.Heap[p];
									goto lbl_searh_class_proto;
								}

								RaiseReferenceError_MulitNameNotFound(ref error, searchName, Context.STRING.QName);
							}
						}



					}
					else if (_obj.indexer_key.ValueType != NaNBoxing.BoxType.Fault)
					{
#if DEBUG
						throw new InvalidOperationException();
#else
							Environment.FailFast("出错了，这里跑不到") ; return default;
#endif

					}
					else if (_obj.trait[0] == null && _obj.trait[1] != null)
					{
						RaiseReferenceError_WriteToReadonlyProperty(ref error, _obj.trait[1].Method.Body, _obj.as_type.QName);
					}
					else if (_obj.trait[0].Kind == TraitKind.Slot || _obj.trait[0].Kind == TraitKind.Constant)
					{
#if DEBUG
						throw new InvalidOperationException();
#else
							Environment.FailFast("出错了，这里跑不到") ; return default;
#endif

					}
#if DEBUG
					else if (_obj.trait[0].Kind == TraitKind.Method && _obj.trait[1] == null)
					{
						throw new InvalidOperationException("Method不会构造到STACK_CACHE_OBJ中");

					}
#endif
					else
					{
						NaNBoxing instance = new NaNBoxing();
						instance = _obj.RefInstance;
						result = InvokeReadProperty(ref error, instance, _obj.g_index,  stackslots, returnSlotIndex);

						//throw new NotImplementedException("属性的引用未实现");
					}
				}
				else
				{


					if (_obj.searchPropertyName.ValueType == BoxType.HeapPtr || _obj.searchPropertyName.ValueType == BoxType.LocalString) //动态属性
					{
						Context.GC.CheckGC(ref error);

						//string searchName = ((RtPayloadString)Context.GC.Heap[_obj.searchPropertyNamePtr]).Str;


						Span<char> temp = stackalloc char[16];
						ReadOnlySpan<char> searchName = temp;
						if (_obj.searchPropertyName.ValueType == BoxType.HeapPtr)
						{
							searchName = ((RtString)Context.GC.Heap[_obj.searchPropertyName.HeapPtr]).Str;
						}
						else
						{
							int l = _obj.searchPropertyName.GetLocalStringChars(temp);
							searchName = temp.Slice(0, l);
						}


						_obj.searchPropertyName.SetUndefined();

						ASClass primitiveCls = null;

						switch (_obj.RefInstance.ValueType)
						{
							case BoxType.Number:
								primitiveCls = Context.NUMBER;
								break;
							case BoxType.Sbyte:
								primitiveCls = Context.SBYTE;
								break;
							case BoxType.Byte:
								primitiveCls = Context.BYTE;
								break;
							case BoxType.Short:
								primitiveCls = Context.SHORT;
								break;
							case BoxType.UShort:
								primitiveCls = Context.USHORT;
								break;
							case BoxType.Int:
								primitiveCls = Context.INT;
								break;
							case BoxType.Uint:
								primitiveCls = Context.UINT;
								break;
							case BoxType.Float:
								primitiveCls = Context.FLOAT;
								break;
							case BoxType.Boolean:
								primitiveCls = Context.BOOLEAN;
								break;
							case BoxType.Undefined:
							case BoxType.Null:
							case BoxType.HeapPtr:
							case BoxType.Fault:
							default:
#if DEBUG
								throw new InvalidOperationException();
#else
								Environment.FailFast("出错了，这里跑不到"); break;
#endif
						}

						NaNBoxing ns = new NaNBoxing();
						ASNamespace @namespace = null;
						if (_obj.searchNameSpacePtr > 0)
						{
							ns.SetHeapPtr(_obj.searchNameSpacePtr, (byte)RtHeapTypeKind.NAMESPACE, (byte)HeapKindFlag.NONE);
							RtHeapBase ns_instance = Context.GC.Heap[_obj.searchNameSpacePtr];
							@namespace = ((RtNameSpace)ns_instance).ASNamespace;


							RaiseReferenceError_RTQNameNotFound(ref error, ns, searchName, _obj.RefInstance);

						}
						else
						{


							//查找原始类型的原型链
							var proto = Context.GC.Heap[((RtScriptClass)Context.GC.Heap[primitiveCls.__instance_index__]).PROTO__PTR];
						lbl_searh_class_proto:
							NaNBoxing value; int shape_ptr; int index; RtDynamic prop;
							if (FindDynamicValue(proto, searchName, out value, out shape_ptr, out index, out prop))
							{
								result = value;
							}
							else
							{
								int p = ((RtInstance)proto).PROTOTYPE(this, (ASInstance)proto.Type); //class的proto肯定是instance。
								if (p != 0)
								{
									proto = Context.GC.Heap[p];
									goto lbl_searh_class_proto;
								}

								RaiseReferenceError_MulitNameNotFound(ref error, searchName, primitiveCls.QName);
							}
						}
					}
					else
					{
#if DEBUG
						throw new InvalidOperationException();
#else
						Environment.FailFast("出错了，这里跑不到"); return default;
#endif
					}

					//throw new NotImplementedException("原始类型未实现");
				}
			}
			//#if DEBUG
			//			else if (
			//				//rtHeap.TypeKind == RtHeapTypeKind.CACHE_LD_CLASS || 
			//				rtHeapKind == RtHeapTypeKind.SHAPE)
			//			{
			//				throw new InvalidOperationException();
			//			}
			//#endif
			return result;

		}

		/// <summary>
		/// 从操作数中提取实际值。
		/// 由于有成员引用类型存在，必须先进行判断和解码
		/// 
		/// returnSlotIndex 用于某些ReadSlot()时必须的传入参数,和InvokeReadProperty
		/// </summary>
		/// <param name="box"></param>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal unsafe NaNBoxing LoadValue(RtStackCache _obj, int callee_slotindex, ref ReceiveError error, Span<NaNBoxing> stackslots, int returnSlotIndex)
		{



			if (_obj.RefInstance.HeapKind == (byte)RtHeapTypeKind.ARRAY && _obj.indexer_key.ValueType == BoxType.Uint)
			{
				RtHeapBase refObj = Context.GC.Heap[_obj.RefInstance.HeapPtr];
				bool isoutofindex_or_ishole;
				var v = LoadSlotFromArray(_obj.indexer_key.UIntValue, refObj, out isoutofindex_or_ishole);

				if (v.ValueType == BoxType.Fault)
				{
					v.SetUndefined();
				}
				else if (v.IsStruct())//v.ValueType == BoxType.HeapPtr && v.HeapKind == (byte)RtHeapTypeKind.INSTANCE && v.HeapFlag &)
				{
					v.SetHeapPtr(v.HeapPtr, (byte)RtHeapTypeKind.INSTANCE, (byte)(HeapKindFlag.FLAG_STRUCT | HeapKindFlag.FLAG_REFSTRUCT));

				}

				return v;
			}
			else if (_obj.RefInstance.HeapKind == (byte)RtHeapTypeKind.VECTOR && _obj.indexer_key.ValueType != BoxType.Fault)
			{

				RtVector vector;
				int v_ptr = RtVector.FindAndUpdateHeapInstancePtr(_obj.RefInstance.HeapPtr, this, out vector);
				//int maxlen; int validid;
				var store = vector.GetStore();
				if (!(store.IsValidIndexRange(_obj.indexer_key, out int validid)))
				{
					Span<char> buffers = stackalloc char[128];
					RaiseRangeError(ref error, Extensions.GetPrimitiveValueToString(this, _obj.indexer_key, buffers), store.length);
					return default;
				}
				else
				{
					return store.ReadSlot(vector.element_type, validid, this, v_ptr, returnSlotIndex, vector.element_asclass);
					//return vector.ReadSlot(validid, this, returnSlotIndex, v_ptr);
					//throw new NotImplementedException();
				}

			}
			else
			{
				return LoadValue_Slow(_obj, callee_slotindex, ref error, stackslots, returnSlotIndex);
			}

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private unsafe void SaveIndexer(NaNBoxing RefInstance,NaNBoxing o_indexer_key, NaNBoxing value, StackLocater* tmpArgLoc, ref ReceiveError error)
		{
			RtHeapBase instance = Context.GC.Heap[RefInstance.HeapPtr];
#if DEBUG
			if (!(instance.Kind == RtHeapTypeKind.INSTANCE && ((ASInstance)instance.Type).Flags.HasFlag(ClassFlags.Indexer)))
			{
				throw new InvalidOperationException();
			}
#endif

			if (Context.StackPosition + 2 >= Context.STACK_LENGTH)
			{
				RaiseStackOverflow(ref error);
				return;
			}

			var argSpan = Context.StackSlots.AsSpan(Context.StackPosition, 2);

			Context.StackPosition += 2;
			Context.GC.CheckGC(ref error);


			//var indexer_key = GetSaveValue(o_indexer_key, ref error);
			//if (error.raised)
			//{
			//	Context.StackPosition -= 2;
			//	return;
			//}

			argSpan[0] = o_indexer_key;

			NaNBoxing box = GetSaveValue(value, ref error);
			if (error.raised)
			{
				Context.StackPosition -= 2;
				return;
			}

			argSpan[1] = box;

			tmpArgLoc[0].index = 0;
			tmpArgLoc[1].index = 1; ;


			NaNBoxing _this = new NaNBoxing();
			_this = RefInstance; //.SetHeapPtr(cacheObj.RefInstance.HeapPtr);

			RunMethod(((ASInstance)instance.Type).indexer_set, _this,
				RefInstance.HeapPtr, 2, (byte*)tmpArgLoc, argSpan, ref error, -1);

			Context.StackPosition -= 2;
			
		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		internal unsafe void SaveHeapRef(RtHeapBase cache, StackLocater source, Span<NaNBoxing> stackslots, Span<char> frame_holdchars,
			StackLocater* tmpArgLoc, int scope_ptr, int stackStPos, RtHeapBase methodscope,
			ref ReceiveError error)
		{
			{

				NaNBoxing box = stackslots[source.index];
				RtStackCache cacheObj = (RtStackCache)cache;

				if (cacheObj.RefInstance.ValueType != BoxType.HeapPtr)
				{
#if DEBUG
					if (!(cacheObj.searchPropertyName.ValueType == BoxType.HeapPtr || cacheObj.searchPropertyName.ValueType == BoxType.LocalString))
					{
						throw new InvalidOperationException();
					}
#endif


					ReadOnlySpan<char> searchName = frame_holdchars;
					if (cacheObj.searchPropertyName.ValueType == BoxType.HeapPtr)
					{
						searchName = ((RtString)Context.GC.Heap[cacheObj.searchPropertyName.HeapPtr]).Str;
					}
					else
					{
						Span<char> temp = frame_holdchars; //stackalloc char[16];//用于从LocalString中提取值
						int l = cacheObj.searchPropertyName.GetLocalStringChars(temp);
						searchName = temp.Slice(0, l);
					}

					cacheObj.searchPropertyName.SetUndefined();


					ASNamespace @namespace = null;

					RaiseReferenceError_CanNotCreateProperty(ref error, @namespace, searchName, cacheObj.as_type.QName);

					goto flag_handle_error;

				}
				else
				{

					if (cacheObj.searchPropertyName.ValueType == BoxType.HeapPtr || cacheObj.searchPropertyName.ValueType == BoxType.LocalString)
					{
						Context.GC.CheckGC(ref error); //只能在此处先GC,否则后面会意外回收searchname_ptr。 

						ReadOnlySpan<char> searchName = frame_holdchars;
						if (cacheObj.searchPropertyName.ValueType == BoxType.HeapPtr)
						{

							searchName = ((RtString)Context.GC.Heap[cacheObj.searchPropertyName.HeapPtr]).Str;
						}
						else
						{
							Span<char> temp = frame_holdchars; //stackalloc char[16];//用于从LocalString中提取值
							int l = cacheObj.searchPropertyName.GetLocalStringChars(temp);
							searchName = temp.Slice(0, l);

						}
						//
						//cacheObj.searchPropertyName.SetUndefined(); 本应在这里清理,但是后面还有CreateDynamic需要用。

						ASNamespace @namespace = null;
						NaNBoxing ns = new NaNBoxing();
						if (cacheObj.searchNameSpacePtr > 0)
						{
							ns.SetHeapPtr(cacheObj.searchNameSpacePtr, (byte)RtHeapTypeKind.NAMESPACE, (byte)HeapKindFlag.NONE);
							RtHeapBase ns_instance = Context.GC.Heap[cacheObj.searchNameSpacePtr];
							@namespace = ((RtNameSpace)ns_instance).ASNamespace;

							cacheObj.searchNameSpacePtr = 0;

						}

						RtHeapBase instance = Context.GC.Heap[cacheObj.RefInstance.HeapPtr];

						if (instance.Kind == RtHeapTypeKind.INSTANCE
							&&
							(
								((ASInstance)instance.Type).Flags & ClassFlags.Sealed) == ClassFlags.Sealed
								||
								(
									@namespace != null &&
									@namespace.Kind != NamespaceKind.Package
								)
							)
						{

							RaiseReferenceError_CanNotCreateProperty(ref error, @namespace, searchName, cacheObj.as_type.QName);
							cacheObj.searchPropertyName.SetUndefined();
							goto flag_handle_error;
						}
						else if (instance.Kind == RtHeapTypeKind.NAMESPACE)
						{
							RaiseReferenceError_CanNotCreateProperty(ref error, @namespace, searchName, cacheObj.as_type.QName);
							cacheObj.searchPropertyName.SetUndefined();
							goto flag_handle_error;
						}
						else if (instance.Kind == RtHeapTypeKind.VECTOR)
						{

							RaiseReferenceError_CanNotCreateProperty(ref error, @namespace, searchName, instance.Type.QName);
							cacheObj.searchPropertyName.SetUndefined();
							goto flag_handle_error;
						}
						else if (instance.Kind == RtHeapTypeKind.CLOSURE
							&&
								((ASMethodBody)instance.Type).Method.__ismethod
							)
						{
							RaiseReferenceError_CanNotCreateProperty(ref error, @namespace, searchName, buildin_as_methodclosure);
							cacheObj.searchPropertyName.SetUndefined();
							goto flag_handle_error;
						}
						else if (instance.Kind == RtHeapTypeKind.STRING)
						{
							RaiseReferenceError_CanNotCreateProperty(ref error, @namespace, searchName, instance.Type.QName);
							cacheObj.searchPropertyName.SetUndefined();
							goto flag_handle_error;
						}
						else
						{
							//保存缓存到实体的代码已移动到CreateDynamic内部。
							CreateDynamic(ref error, instance, cacheObj.searchPropertyName, box, true, true, true);
							cacheObj.searchPropertyName.SetUndefined();
							if (error.raised)
							{
								goto flag_handle_error;
							}
							//throw new NotImplementedException("添加动态属性");
						}
					}
					else
					{
						if (cacheObj.indexer_key.ValueType != NaNBoxing.BoxType.Fault)
						{
							if (cacheObj.RefInstance.HeapKind == (byte)RtHeapTypeKind.ARRAY)
							{
#if DEBUG
								if (cacheObj.trait[0] == null && cacheObj.trait[1] == null

									&& cacheObj.indexer_key.ValueType == NaNBoxing.BoxType.Uint

									)
#endif

								{
									RtHeapBase instance = Context.GC.Heap[cacheObj.RefInstance.HeapPtr];

									SetArraySlot(box, cacheObj.indexer_key.UIntValue, instance, ref error);
									if (error.raised)
									{
										goto flag_handle_error;
									}


								}
#if DEBUG
								else
								{
									throw new InvalidOperationException();
								}
#endif
							}
							else if (cacheObj.RefInstance.HeapKind == (byte)RtHeapTypeKind.VECTOR)
							{
								//Vector不能动态创建属性
								if (!RtVector.IsValidIndexType(cacheObj.indexer_key))
								{
									Span<char> buffers = frame_holdchars;
									RaiseReferenceError_CanNotCreateProperty(ref error, null, Extensions.GetPrimitiveValueToString(this, cacheObj.indexer_key, buffers), Context.GC.Heap[cacheObj.RefInstance.HeapPtr].Type.QName);
									goto flag_handle_error;
								}

								Context.GC.CheckGC(ref error);
								if (Context.StackPosition >= Context.STACK_LENGTH)
								{
									RaiseStackOverflow(ref error);
									goto flag_handle_error;
								}

								//RtVector vector = ((RtVector)instance);
								RtVector vector;
								RtVector.FindAndUpdateHeapInstancePtr(cacheObj.RefInstance.HeapPtr, this, out vector);

								ref NaNBoxing conv = ref Context.StackSlots[Context.StackPosition];
								Context.StackPosition++;

								ConvertValueType(ref error, box, vector.element_type, vector.element_asclass, ref conv);//, scope_ptr, ((RtMethodScope)methodscope).ThisPtr);
								if (error.raised)
								{
									Context.StackPosition--;
									goto flag_handle_error;
								}
								//为性能考虑，阻止ConvertValueType调函数


								int validid;
								var store = ((RtVector)vector).GetStore();
								if (!(store.IsValidIndexRange(cacheObj.indexer_key, out validid)))
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
										Span<char> buffers = stackalloc char[128];
										RaiseRangeError(ref error, Extensions.GetPrimitiveValueToString(this, cacheObj.indexer_key, buffers), maxlen);
										goto flag_handle_error;
									}
								}

								vector.SetSlot(validid, this, cacheObj.RefInstance.HeapPtr, conv, ref error);

								Context.StackPosition--;

								if (error.raised)
								{
									goto flag_handle_error;
								}

								//throw new NotImplementedException();
							}
							else
							{
								SaveIndexer(cacheObj.RefInstance, cacheObj.indexer_key, box, tmpArgLoc, ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}
							}
							//else
							//{
							//	throw new NotImplementedException();
							//}
						}
						else if (cacheObj.trait[1] != null)
						{
							RtHeapBase instance = Context.GC.Heap[cacheObj.RefInstance.HeapPtr];

							BeforeWriteProperty();

							if (cacheObj.trait[1] == Context.FUNCTION.Instance._vtable.Items[1].Trait)
							{
								//写Function的 prototype属性。
								WriteFunctionProto(box, ref error, Context.GC.Heap[cacheObj.RefInstance.HeapPtr], cacheObj.RefInstance);
								if (error.raised)
								{
									goto flag_handle_error;
								}
							}
							else
							{
								if (Context.StackPosition >= Context.STACK_LENGTH)
								{
									RaiseStackOverflow(ref error);
									goto flag_handle_error;
								}

								//检查array.length是否超过最大允许值
								if (
									box.ValueType == BoxType.Number && box.Number > uint.MaxValue &&
									cacheObj.trait[1] == Context.ARRAY.Instance._vtable.Items[1].Trait)
								{
									Span<char> buffers = frame_holdchars;
									RaiseRangeError(ref error, Extensions.GetPrimitiveValueToString(this, box, buffers), uint.MaxValue);
									goto flag_handle_error;
								}


								Context.StackSlots[Context.StackPosition] = box;
								StackLocater argLoc = new StackLocater() { index = stackslots.Length };
								Context.StackPosition++;

								NaNBoxing _this = new NaNBoxing();
								_this = cacheObj.RefInstance; //.SetHeapPtr(cacheObj.RefInstance.HeapPtr);

								RunMethod(cacheObj.trait[1].Method, _this,
									cacheObj.RefInstance.HeapPtr, 1, (byte*)&argLoc, Context.StackSlots.AsSpan(stackStPos, stackslots.Length + 1), ref error, -1);

								Context.StackPosition--;
								if (error.raised)
								{
									goto flag_handle_error;
								}
							}

						}
						else if (cacheObj.RefInstance.HeapKind == (byte)RtHeapTypeKind.GLOBAL || cacheObj.RefInstance.HeapKind == (byte)RtHeapTypeKind.CLASS)
						{
							RtHeapBase instance = Context.GC.Heap[cacheObj.RefInstance.HeapPtr];
							RtScriptClass payload = (RtScriptClass)instance;

							ASTrait trait = cacheObj.trait[0];

							if (trait.Kind == TraitKind.Constant)
							{

								RaiseReferenceError_WriteConst(ref error, trait, instance.Type.QName);
								goto flag_handle_error;
							}
							else if (trait.Kind == TraitKind.Slot)
							{
								Context.GC.CheckGC(ref error);
								if (Context.StackPosition >= Context.STACK_LENGTH)
								{
									RaiseStackOverflow(ref error);
									goto flag_handle_error;
								}

								ref NaNBoxing conv = ref Context.StackSlots[Context.StackPosition];
								Context.StackPosition++;

								ConvertValueType(ref error, box, trait.TypeKind, trait.__rt_type_class__, ref conv, scope_ptr, ((RtMethodScope)methodscope).ThisPtr);
								if (error.raised)
								{
									Context.StackPosition--;
									goto flag_handle_error;
								}
								if (payload.IsUpdateStructOrEqual(Context, cacheObj.scopemember_index, conv))
								{
									Context.StackPosition--;
								}
								else
								{
									box = GetSaveValue(conv, ref error);
									Context.StackPosition--;
									if (error.raised)
									{
										goto flag_handle_error;
									}

									payload.SetSlot(box, cacheObj.scopemember_index);
								}
							}
#if DEBUG
							else
							{
								throw new InvalidOperationException();
							}
#endif
						}
						else if (cacheObj.RefInstance.HeapKind == (byte)RtHeapTypeKind.INSTANCE)
						{
							RtHeapBase instance = Context.GC.Heap[cacheObj.RefInstance.HeapPtr];
							RtInstance payload = (RtInstance)instance;

							ASTrait trait = cacheObj.trait[0];

							if (trait.Kind == TraitKind.Constant)
							{
								RaiseReferenceError_WriteConst(ref error, trait, cacheObj.as_type.QName);
								goto flag_handle_error;
							}
							else if (trait.Kind == TraitKind.Slot)
							{
								Context.GC.CheckGC(ref error);
								if (Context.StackPosition >= Context.STACK_LENGTH)
								{
									RaiseStackOverflow(ref error);
									goto flag_handle_error;
								}

								ref NaNBoxing conv = ref Context.StackSlots[Context.StackPosition];
								Context.StackPosition++;
								ConvertValueType(ref error, box, trait.TypeKind, trait.__rt_type_class__, ref conv, scope_ptr, ((RtMethodScope)methodscope).ThisPtr);
								if (error.raised)
								{
									Context.StackPosition--;
									goto flag_handle_error;
								}
								if (payload.IsUpdateStructOrEqual(Context, cacheObj.scopemember_index, conv))
								{
									Context.StackPosition--;
								}
								else
								{
									box = GetSaveValue(conv, ref error);
									Context.StackPosition--;

									if (error.raised)
									{
										goto flag_handle_error;
									}

									payload.SetSlot(box, cacheObj.scopemember_index, this);
								}
							}
							else
							{
#if DEBUG
								if (trait.Kind != TraitKind.Getter)
									throw new InvalidOperationException();
#endif
								RaiseReferenceError_WriteToReadonlyProperty(ref error, trait.Method.Body, cacheObj.as_type.QName);
								goto flag_handle_error;
							}
						}
#if DEBUG
						else
						{
							throw new InvalidOperationException();
						}
#endif
					}
				}
			}

		flag_handle_error:
			;


		}





		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private ASContainer GetASTypeFromValue(NaNBoxing instance)
		{
			switch ((RtHeapTypeKind)instance.HeapKind)
			{
				case RtHeapTypeKind.CLASS:
				case RtHeapTypeKind.GLOBAL:
					return ((RtScriptClass)Context.GC.Heap[instance.HeapPtr]).Meta;
				//return (RtScriptClass)instance
				//break;
				case RtHeapTypeKind.STRING:
					return Context.STRING.Instance;
				//break;
				case RtHeapTypeKind.INSTANCE:
					return Context.GC.Heap[instance.HeapPtr].Type;
				//break;
				case RtHeapTypeKind.VECTOR:
					return Context.GC.Heap[instance.HeapPtr].Type;
				//break;

				case RtHeapTypeKind.ARRAY:
					return Context.ARRAY.Instance;
				//type = Context.GC.Heap[instance.HeapPtr].Type;
				//break;
				case RtHeapTypeKind.MethodScope:
					return Context.GC.Heap[instance.HeapPtr].Type;
				//break;

				case RtHeapTypeKind.CLOSURE:
					return Context.GC.Heap[instance.HeapPtr].Type;
				//break;
				case RtHeapTypeKind.NAMESPACE:
					return Context.NAMESPACE.Instance;
				//break;
				//throw new NotImplementedException();
				case RtHeapTypeKind.STACK_CACHE_OBJ:
				//case RtHeapTypeKind.CACHE_LD_CLASS:
				default:
#if DEBUG
					throw new InvalidOperationException();
#else
					Environment.FailFast("出错了，这里跑不到");return null;
#endif

			}
		}
		private void ReadInstanceFromStacklocater(ref ReceiveError error, StackLocater src, Span<NaNBoxing> stackslots, int stackStPos, int scope_ptr,
			out RtHeapTypeKind kind, out NaNBoxing instance)
		{
			Debug.Assert(src.index < 0);

			//			if (src.index >= 0)
			//			{
			//				//***若instance是一个成员的引用，还需要在此解开。

			//				//instance = LoadValue(stackslots[src.index], ref error,ref stackslots,stackStPos);

			//				//if (error.raised)
			//				//{
			//				//    kind = (RtHeapTypeKind)255;

			//				//    type = null;
			//				//    instance = default(NaNBoxing);

			//				//    return;
			//				//}

			//				instance = stackslots[src.index];

			//				if (instance.ValueType == NaNBoxing.BoxType.HeapPtr)
			//				{
			//					//instancePtr = instance.HeapPtr;
			//#if DEBUG
			//					RtHeapBase rtHeap = Context.GC.Heap[instance.HeapPtr];

			//					if (rtHeap.Kind == RtHeapTypeKind.STACK_CACHE_OBJ
			//						//||
			//						//rtHeap.TypeKind == RtHeapTypeKind.CACHE_LD_CLASS
			//						||
			//						rtHeap.Kind == RtHeapTypeKind.SHAPE
			//						)
			//					{
			//						throw new InvalidOperationException();
			//					}
			//#endif

			//				}
			//				else
			//				{
			//					kind = (RtHeapTypeKind)255;
			//					//type = null;
			//					//instancePtr = 0;
			//					//return instance;
			//					return;
			//				}



			//			}
			//			else
			{
				//沿scope链查找

				int scopeid = -src.index - 1;

				var o = Context.GC.Heap[scope_ptr];
				int instancePtr = scope_ptr;
				do
				{
					if (o.Kind == RtHeapTypeKind.MethodScope)
					{
						if (o.Type._link_codescope.index != scopeid)
						{
							RtMethodScope rtPayload = (RtMethodScope)o;
							o = Context.GC.Heap[rtPayload.ParentPtr];
							instancePtr = rtPayload.ParentPtr;
						}
						else
						{
							break;
						}
					}
					else if (o.Kind == RtHeapTypeKind.INSTANCE)
					{
						RtInstance heap = (RtInstance)o;
						if (o.Type._link_codescope.index != scopeid)
						{
							//不可能到这里
#if DEBUG
							throw new InvalidOperationException();
#else
							Environment.FailFast("出错了，这里跑不到"); break;
#endif
						}
						else
						{
							break;
						}
					}
					else if (o.Kind == RtHeapTypeKind.CLASS || o.Kind == RtHeapTypeKind.GLOBAL)
					{
						var codeScope = ((RtScriptClass)o).Meta._link_codescope;
						if (codeScope.index != scopeid)
						{
#if DEBUG
							throw new InvalidOperationException();
#else
							Environment.FailFast("出错了，这里跑不到"); break;
#endif
						}
						else
						{
							break;
						}

					}
					else
					{
#if DEBUG
						throw new InvalidOperationException();
#else
						Environment.FailFast("出错了，这里跑不到"); break;
#endif
					}

				} while (true);


				instance = new NaNBoxing();
				instance.SetHeapPtr(instancePtr, (byte)o.Kind, (byte)(o.Kind == RtHeapTypeKind.INSTANCE ? (((ASInstance)o.Type).Flags.HasFlag(ClassFlags.Struct) ? HeapKindFlag.FLAG_STRUCT : HeapKindFlag.NONE) : HeapKindFlag.NONE));

				Debug.Assert(Context.GC.Heap[instancePtr].Kind == o.Kind);

			}
			kind = (RtHeapTypeKind)instance.HeapKind;
			//var temp = Context.GC.Heap[instance.HeapPtr];
			//kind = temp.Kind;


		}

		private static int ReadIntFromString(string str)
		{
			if (string.IsNullOrEmpty(str))
			{
				return 0;
			}

			var chars = str.AsSpan();

			unsafe
			{
				fixed (char* pp = chars)
				{
					char* p = pp;

					int testhex = 0;
					bool ishex = false;

					long n = 0;
					int e = 0;
					bool isEmode = false;

					int sign = 1;
					int esign = 1;
					bool haschecksign = false;

					bool blank = false;


					bool haschar = false;

					bool isDecimal = false;

					bool hasdigit = false;

					while (*p != '\0')
					{
						char c = *p;

						if (c != ' ')
						{
							haschar = true;


							if (blank)
							{
								return 0;
							}


							if (!haschecksign)
							{
								haschecksign = true;
								if (c == '-')
								{
									if (isEmode)
									{
										esign = -1;
									}
									else
									{
										sign = -1;
									}

									++p;
									continue;
								}
								else if (c == '+')
								{
									++p;
									continue;
								}
							}


							if (testhex == 0)
							{
								if (c == '0')
								{
									testhex = 1;
								}
								else
								{
									testhex = -1;
								}
							}
							else if (testhex == 1)
							{
								if (c == 'x')
								{
									hasdigit = false;
									ishex = true;
									n = 0;

									testhex = -1;
									++p;
									continue;
								}
								else
								{
									testhex = -1;
								}
							}

							if (c >= '0' && c <= '9')
							{
								hasdigit = true;

								if (isEmode)
								{
									e = e * 10 + (c - '0');
								}
								else if (ishex)
								{
									n = n * 16 + (c - '0');
								}
								else if (isDecimal)
								{

								}
								else
								{
									n = n * 10 + (c - '0');
								}
							}
							else if (c == 'e' || c == 'E')
							{
								hasdigit = true;

								if (ishex)
								{
									return 0;
								}
								else if (!isEmode)
								{
									isEmode = true;
									haschecksign = false;
								}
								else
								{
									return 0;
								}
							}
							else if (c >= 'a' && c <= 'f' && ishex)
							{
								hasdigit = true;
								n = n * 16 + (c - 'a' + 10);
							}
							else if (c >= 'A' && c <= 'F' && ishex)
							{
								n = n * 16 + (c - 'A' + 10);
							}
							else if (c == '.')
							{

								if (ishex || isDecimal)
								{
									return 0;
								}
								else
								{
									isDecimal = true;
								}
							}
							else
							{
								return 0;
							}

						}
						else if (testhex == 1)
						{
							blank = true;
						}
						else if (hasdigit)
						{
							blank = true;
						}


						++p;
					}

					if (esign > 0)
					{
						while (e > 0)
						{
							n = n * 10;
							--e;
						}
					}
					else
					{
						while (e > 0)
						{
							n = n / 10;
							--e;
						}
					}


					if (!haschar)
						return 0;
					else
						return (int)(n * sign);

				}
			}
		}

		private static uint ReadUIntFromString(string str)
		{
			if (string.IsNullOrEmpty(str))
			{
				return 0;
			}

			var chars = str.AsSpan();

			unsafe
			{
				fixed (char* pp = chars)
				{
					char* p = pp;

					int testhex = 0;
					bool ishex = false;

					Int64 n = 0;
					Int32 e = 0;
					bool isEmode = false;

					int sign = 1;
					int esign = 1;
					bool haschecksign = false;

					bool blank = false;


					bool haschar = false;

					bool isDecimal = false;

					bool hasdigit = false;

					while (*p != '\0')
					{
						char c = *p;

						if (c != ' ')
						{
							haschar = true;


							if (blank)
							{
								return 0;
							}


							if (!haschecksign)
							{
								haschecksign = true;
								if (c == '-')
								{
									if (isEmode)
									{
										esign = -1;
									}
									else
									{
										sign = -1;
									}

									++p;
									continue;
								}
								else if (c == '+')
								{
									++p;
									continue;
								}
							}


							if (testhex == 0)
							{
								if (c == '0')
								{
									testhex = 1;
								}
								else
								{
									testhex = -1;
								}
							}
							else if (testhex == 1)
							{
								if (c == 'x')
								{
									ishex = true;
									n = 0;

									testhex = -1;
									++p;
									continue;
								}
								else
								{
									testhex = -1;
								}
							}

							if (c >= '0' && c <= '9')
							{
								hasdigit = true;
								if (isEmode)
								{
									e = e * 10 + (c - '0');
								}
								else if (ishex)
								{
									n = n * 16 + (c - '0');
								}
								else if (isDecimal)
								{

								}
								else
								{
									n = n * 10 + (c - '0');
								}
							}
							else if (c == 'e' || c == 'E')
							{
								hasdigit = true;
								if (ishex)
								{
									return 0;
								}
								else if (!isEmode)
								{
									isEmode = true;
									haschecksign = false;
								}
								else
								{
									return 0;
								}
							}
							else if (c >= 'a' && c <= 'f' && ishex)
							{
								hasdigit = true;
								n = n * 16 + (c - 'a' + 10);
							}
							else if (c >= 'A' && c <= 'F' && ishex)
							{
								hasdigit = true;
								n = n * 16 + (c - 'A' + 10);
							}
							else if (c == '.')
							{
								if (ishex || isDecimal)
								{
									return 0;
								}
								else
								{
									isDecimal = true;
								}
							}
							else
							{
								return 0;
							}

						}
						else if (testhex == 1)
						{
							blank = true;
						}
						else if (hasdigit)
						{
							blank = true;
						}


						++p;
					}

					if (esign > 0)
					{
						while (e > 0)
						{
							n = n * 10;
							--e;
						}
					}
					else
					{
						while (e > 0)
						{
							n = n / 10;
							--e;
						}
					}


					if (!haschar)
						return 0;
					else
						return (UInt32)(n * sign);


				}
			}

		}

		private static double ReadDoubleFromString(string str)
		{
			if (string.IsNullOrEmpty(str))
			{
				return 0;
			}

			var chars = str.AsSpan();

			unsafe
			{
				fixed (char* pp = chars)
				{
					char* p = pp;

					int testhex = 0;
					bool ishex = false;

					double n = 0;
					Int32 e = 0;
					bool isEmode = false;

					int sign = 1;
					int esign = 1;
					bool haschecksign = false;

					bool blank = false;


					bool haschar = false;

					bool isDecimal = false;
					double d = 1;

					//Infinity

					ReadOnlySpan<char> infchar = "Infinity";

					int inftest = 0;

					bool hasdigit = false;

					while (*p != '\0')
					{
						char c = *p;

						if (c != ' ')
						{
							haschar = true;


							if (blank)
							{
								return double.NaN;
							}


							if (!haschecksign)
							{
								haschecksign = true;
								if (c == '-')
								{
									if (isEmode)
									{
										esign = -1;
									}
									else
									{
										sign = -1;
									}

									++p;
									continue;
								}
								else if (c == '+')
								{
									++p;
									continue;
								}
							}

							if (inftest < 8)
							{
								if (c == infchar[inftest])
								{
									++inftest;

									if (inftest == 8)
									{
										n = double.PositiveInfinity; //std::numeric_limits<double>::infinity();
										hasdigit = true;
										blank = true;
									}

									++p;
									continue;
								}
							}




							if (testhex == 0)
							{
								if (c == '0')
								{
									testhex = 1;
								}
								else
								{
									testhex = -1;
								}
							}
							else if (testhex == 1)
							{
								if (c == 'x')
								{
									hasdigit = false;
									ishex = true;
									n = 0;

									testhex = -1;
									++p;
									continue;
								}
								else
								{
									testhex = -1;
								}
							}

							if (c >= '0' && c <= '9')
							{
								hasdigit = true;
								if (isEmode)
								{
									e = e * 10 + (c - '0');
								}
								else if (ishex)
								{
									n = n * 16 + (c - '0');
								}
								else if (isDecimal)
								{
									d = d * 0.1;

									n = n + d * (c - '0');
								}
								else
								{
									n = n * 10 + (c - '0');
								}
							}
							else if (c == 'e' || c == 'E')
							{
								hasdigit = true;
								if (ishex)
								{
									return double.NaN;
								}
								else if (!isEmode)
								{
									isEmode = true;
									haschecksign = false;
								}
								else
								{
									return double.NaN;
								}
							}
							else if (c >= 'a' && c <= 'f' && ishex)
							{
								hasdigit = true;
								n = n * 16 + (c - 'a' + 10);
							}
							else if (c >= 'A' && c <= 'F' && ishex)
							{
								hasdigit = true;
								n = n * 16 + (c - 'A' + 10);
							}
							else if (c == '.')
							{

								if (ishex || isDecimal)
								{
									return double.NaN;
								}
								else
								{
									isDecimal = true;
								}
							}
							else
							{
								return double.NaN;
							}

						}
						else if (testhex == 1)
						{
							blank = true;
						}
						else if (inftest > 0)
						{
							blank = true;
						}
						else if (hasdigit)
						{
							blank = true;
						}


						++p;
					}

					if (esign > 0)
					{
						while (e > 0)
						{
							n = n * 10;
							--e;
						}
					}
					else
					{
						while (e > 0)
						{
							n = n * 0.1;
							--e;
						}
					}


					if (!haschar)
						return 0;
					else
						return n * sign;


				}
			}

		}


		private bool IsEqual_Slow(NaNBoxing v1, NaNBoxing v2, StackLocater tempStore, ref ReceiveError error,
			int scope_ptr, Span<NaNBoxing> stackslots, int stackStPos, NaNBoxing caller_bindthis_ptr)
		{

		/*
		 如果操作数具有相同的类型，则按如下方式进行比较：
				对象（Object）：仅当两个操作数引用同一个对象时返回 true。
				字符串（String）：仅当两个操作数具有相同的字符且顺序相同时返回 true。
				数字（Number）：如果两个操作数的值相同，则返回 true。+0 和 -0 被视为相同的值。如果任何一个操作数是 NaN，返回 false；所以，NaN 永远不等于 NaN。
				布尔值（Boolean）：仅当操作数都为 true 或都为 false 时返回 true。
				大整型（BigInt）：仅当两个操作数的值相同时返回 true。
				符号（Symbol）：仅当两个操作数引用相同的符号时返回 true。
		如果其中一个操作数为 null 或 undefined，另一个操作数也必须为 null 或 undefined 以返回 true。否则返回 false。

		如果其中一个操作数是对象，另一个是原始值，则将对象转换为原始值。
		在这一步，两个操作数都被转换为原始值（字符串、数字、布尔值、符号和大整型中的一个）。剩余的转换将分情况完成。
			如果是相同的类型，使用步骤 1 进行比较。

			如果其中一个操作数是符号而另一个不是，返回 false。
			如果其中一个操作数是布尔值而另一个不是，则将布尔值转换为数字：true 转换为 1，false 转换为 0。然后再次对两个操作数进行宽松比较。

			数字与字符串：将字符串转换为数字。转换失败将导致 NaN，这将保证相等比较为 false。
			数字与大整型：按数值进行比较。如果数字的值为 ±∞ 或 NaN，返回 false。
			字符串与大整型：使用与 BigInt() 构造函数相同的算法将字符串转换为大整型数。如果转换失败，返回 false。
		宽松相等是对称的：A == B 对于 A 和 B 的任何值总是具有与 B == A 相同的语义（应用转换的顺序除外）。
		 */

		flag_retest:
			//对象比较
			if (v1.ValueType == NaNBoxing.BoxType.HeapPtr && v2.ValueType == NaNBoxing.BoxType.HeapPtr)
			{


				if (v1.HeapKind == (byte)RtHeapTypeKind.STRING && v2.HeapKind == (byte)RtHeapTypeKind.STRING)
				{
					var ins1 = Context.GC.Heap[v1.HeapPtr];
					var ins2 = Context.GC.Heap[v2.HeapPtr];
					return string.CompareOrdinal(((RtString)ins1).Str, ((RtString)ins2).Str) == 0;
				}
				else if (v1.HeapKind == (byte)RtHeapTypeKind.INSTANCE && v2.HeapKind == (byte)RtHeapTypeKind.INSTANCE)
				{
					var ins1 = Context.GC.Heap[v1.HeapPtr];
					var ins2 = Context.GC.Heap[v2.HeapPtr];
					if (((ASInstance)ins1.Type).Flags.HasFlag(ClassFlags.Struct))
					{
						var layoutsize = ins1.Type._link_codescope.TypeLayout.Size;

						return ((RtInstance)ins1).GetStoreData(this, (ASInstance)ins1.Type).Slice(0, layoutsize).SequenceEqual(
								 ((RtInstance)ins2).GetStoreData(this, (ASInstance)ins2.Type).Slice(0, layoutsize))
							;

					}
					else if (((ASInstance)ins1.Type).Flags.HasFlag(ClassFlags.Wapper))
					{
						return ((RtInstance)ins1).wapperedObject.Equals(((RtInstance)ins2).wapperedObject);
					}
					else
					{
						RtInstance tmp1, tmp2;
						return RtInstance.FindAndUpdateHeapInstancePtr(v1.HeapPtr, this, out tmp1) ==
							RtInstance.FindAndUpdateHeapInstancePtr(v2.HeapPtr, this, out tmp2);

						//return v1.HeapPtr == v2.HeapPtr;
					}
				}
				else if (v1.HeapKind != (byte)RtHeapTypeKind.STRING && v2.HeapKind != (byte)RtHeapTypeKind.STRING)
				{
					if (v1.HeapKind != v2.HeapKind)
						return false;

					switch ((RtHeapTypeKind)v1.HeapKind)
					{
						case RtHeapTypeKind.CLASS:
						case RtHeapTypeKind.GLOBAL:
						case RtHeapTypeKind.NAMESPACE:
							return v1.HeapPtr == v2.HeapPtr;
						case RtHeapTypeKind.ARRAY:
							{
								RtArray tmp1, tmp2;
								return RtArray.FindAndUpdateHeapInstancePtr(v1.HeapPtr, this, out tmp1) ==
									RtArray.FindAndUpdateHeapInstancePtr(v2.HeapPtr, this, out tmp2);
							}
						case RtHeapTypeKind.VECTOR:
							return v1.HeapPtr == v2.HeapPtr;
						case RtHeapTypeKind.CLOSURE:
							{
								var ins1 = Context.GC.Heap[v1.HeapPtr];
								var ins2 = Context.GC.Heap[v2.HeapPtr];

								RtClosure tmp1, tmp2;
								int ptr1 = RtClosure.FindAndUpdateHeapInstancePtr(v1.HeapPtr, this, out tmp1);
								int ptr2 = RtClosure.FindAndUpdateHeapInstancePtr(v2.HeapPtr, this, out tmp2);

								if (((ASMethodBody)ins1.Type).Method.__ismethod)
								{
									return ins1.Type == ins2.Type && IsStrictlyEqual(tmp1.This, tmp2.This);
								}
								else
								{
									return ptr1 == ptr2;
								}

							}
						case RtHeapTypeKind.STACK_CACHE_OBJ:
						case RtHeapTypeKind.DYNAMIC_PROPERTYS:
						case RtHeapTypeKind.SHAPE:
						case RtHeapTypeKind.MethodScope:
						default:
#if DEBUG
							throw new InvalidOperationException();
#else
							Environment.FailFast("出错了，这里跑不到"); return default;
#endif
					}

					//return v1.HeapPtr == v2.HeapPtr;
				}
			}

			// LocalString与HeapPtr字符串比较
			if (v1.ValueType == NaNBoxing.BoxType.LocalString && v2.ValueType == NaNBoxing.BoxType.HeapPtr)
			{
				if (v2.HeapKind == (byte)RtHeapTypeKind.STRING)
				{
					var ins2 = Context.GC.Heap[v2.HeapPtr];
					string str2 = ((RtString)ins2).Str;

					// 使用高效的字符比较，避免创建LocalString的字符串
					Span<char> chars1 = stackalloc char[16];
					int charCount1 = v1.GetLocalStringChars(chars1);
					if (charCount1 < 0) return false; // 解码失败

					return str2.AsSpan().SequenceEqual(chars1.Slice(0, charCount1));
				}
			}
			else if (v1.ValueType == NaNBoxing.BoxType.HeapPtr && v2.ValueType == NaNBoxing.BoxType.LocalString)
			{
				if (v1.HeapKind == (byte)RtHeapTypeKind.STRING)
				{
					var ins1 = Context.GC.Heap[v1.HeapPtr];
					string str1 = ((RtString)ins1).Str;

					// 使用高效的字符比较，避免创建LocalString的字符串
					Span<char> chars2 = stackalloc char[16];
					int charCount2 = v2.GetLocalStringChars(chars2);
					if (charCount2 < 0) return false; // 解码失败

					return str1.AsSpan().SequenceEqual(chars2.Slice(0, charCount2));
				}
			}

			//比较数字值
			switch (v1.ValueType)
			{
				case BoxType.Number:
					switch (v2.ValueType)
					{
						case NaNBoxing.BoxType.Number:
							if (double.IsNaN(v1.Number) || double.IsNaN(v2.Number)) return false;
							return v1.Number == v2.Number;
						case NaNBoxing.BoxType.Int:
							if (double.IsNaN(v1.Number)) return false;
							return v1.Number == v2.IntValue;
						case NaNBoxing.BoxType.Uint:
							if (double.IsNaN(v1.Number)) return false;
							return v1.Number == v2.UIntValue;
						case NaNBoxing.BoxType.Sbyte:
							if (double.IsNaN(v1.Number)) return false;
							return v1.Number == v2.SByteValue;
						case NaNBoxing.BoxType.Byte:
							if (double.IsNaN(v1.Number)) return false;
							return v1.Number == v2.ByteValue;
						case NaNBoxing.BoxType.Short:
							if (double.IsNaN(v1.Number)) return false;
							return v1.Number == v2.ShortValue;
						case NaNBoxing.BoxType.UShort:
							if (double.IsNaN(v1.Number)) return false;
							return v1.Number == v2.UShortValue;
						case NaNBoxing.BoxType.Float:
							if (double.IsNaN(v1.Number) || float.IsNaN(v2.FloatValue)) return false;
							return v1.Number == v2.FloatValue;
						case NaNBoxing.BoxType.HeapPtr:
						case NaNBoxing.BoxType.Fault:
						case NaNBoxing.BoxType.Undefined:
						case NaNBoxing.BoxType.Null:
						case NaNBoxing.BoxType.Boolean:
						default:
							break;
					}
					break;
				case BoxType.Int:
					switch (v2.ValueType)
					{
						case NaNBoxing.BoxType.Number:
							if (double.IsNaN(v2.Number)) return false;
							return v1.IntValue == v2.Number;
						case NaNBoxing.BoxType.Int:
							return v1.IntValue == v2.IntValue;
						case NaNBoxing.BoxType.Uint:
							return v1.IntValue == v2.UIntValue;
						case NaNBoxing.BoxType.Sbyte:
							return v1.IntValue == v2.SByteValue;
						case NaNBoxing.BoxType.Byte:
							return v1.IntValue == v2.ByteValue;
						case NaNBoxing.BoxType.Short:
							return v1.IntValue == v2.ShortValue;
						case NaNBoxing.BoxType.UShort:
							return v1.IntValue == v2.UShortValue;
						case NaNBoxing.BoxType.Float:
							if (float.IsNaN(v2.FloatValue)) return false;
							return v1.IntValue == v2.FloatValue;
						case NaNBoxing.BoxType.HeapPtr:
						case NaNBoxing.BoxType.Fault:
						case NaNBoxing.BoxType.Undefined:
						case NaNBoxing.BoxType.Null:
						case NaNBoxing.BoxType.Boolean:
						default:
							break;
					}
					break;
				case BoxType.Uint:
					switch (v2.ValueType)
					{
						case NaNBoxing.BoxType.Number:
							if (double.IsNaN(v2.Number)) return false;
							return v1.UIntValue == v2.Number;
						case NaNBoxing.BoxType.Int:
							return v1.UIntValue == v2.IntValue;
						case NaNBoxing.BoxType.Uint:
							return v1.UIntValue == v2.UIntValue;
						case NaNBoxing.BoxType.Sbyte:
							return v1.UIntValue == v2.SByteValue;
						case NaNBoxing.BoxType.Byte:
							return v1.UIntValue == v2.ByteValue;
						case NaNBoxing.BoxType.Short:
							return v1.UIntValue == v2.ShortValue;
						case NaNBoxing.BoxType.UShort:
							return v1.UIntValue == v2.UShortValue;
						case NaNBoxing.BoxType.Float:
							if (float.IsNaN(v2.FloatValue)) return false;
							return v1.UIntValue == v2.FloatValue;
						case NaNBoxing.BoxType.HeapPtr:
						case NaNBoxing.BoxType.Fault:
						case NaNBoxing.BoxType.Undefined:
						case NaNBoxing.BoxType.Null:
						case NaNBoxing.BoxType.Boolean:
						default:
							break;
					}
					break;
				case BoxType.Sbyte:
					switch (v2.ValueType)
					{
						case NaNBoxing.BoxType.Number:
							if (double.IsNaN(v2.Number)) return false;
							return v1.SByteValue == v2.Number;
						case NaNBoxing.BoxType.Int:
							return v1.SByteValue == v2.IntValue;
						case NaNBoxing.BoxType.Uint:
							return v1.SByteValue == v2.UIntValue;
						case NaNBoxing.BoxType.Sbyte:
							return v1.SByteValue == v2.SByteValue;
						case NaNBoxing.BoxType.Byte:
							return v1.SByteValue == v2.ByteValue;
						case NaNBoxing.BoxType.Short:
							return v1.SByteValue == v2.ShortValue;
						case NaNBoxing.BoxType.UShort:
							return v1.SByteValue == v2.UShortValue;
						case NaNBoxing.BoxType.Float:
							if (float.IsNaN(v2.FloatValue)) return false;
							return v1.SByteValue == v2.FloatValue;
						case NaNBoxing.BoxType.HeapPtr:
						case NaNBoxing.BoxType.Fault:
						case NaNBoxing.BoxType.Undefined:
						case NaNBoxing.BoxType.Null:
						case NaNBoxing.BoxType.Boolean:
						default:
							break;
					}
					break;
				case BoxType.Byte:
					switch (v2.ValueType)
					{
						case NaNBoxing.BoxType.Number:
							if (double.IsNaN(v2.Number)) return false;
							return v1.ByteValue == v2.Number;
						case NaNBoxing.BoxType.Int:
							return v1.ByteValue == v2.IntValue;
						case NaNBoxing.BoxType.Uint:
							return v1.ByteValue == v2.UIntValue;
						case NaNBoxing.BoxType.Sbyte:
							return v1.ByteValue == v2.SByteValue;
						case NaNBoxing.BoxType.Byte:
							return v1.ByteValue == v2.ByteValue;
						case NaNBoxing.BoxType.Short:
							return v1.ByteValue == v2.ShortValue;
						case NaNBoxing.BoxType.UShort:
							return v1.ByteValue == v2.UShortValue;
						case NaNBoxing.BoxType.Float:
							if (float.IsNaN(v2.FloatValue)) return false;
							return v1.ByteValue == v2.FloatValue;
						case NaNBoxing.BoxType.HeapPtr:
						case NaNBoxing.BoxType.Fault:
						case NaNBoxing.BoxType.Undefined:
						case NaNBoxing.BoxType.Null:
						case NaNBoxing.BoxType.Boolean:
						default:
							break;
					}
					break;
				case BoxType.Short:
					switch (v2.ValueType)
					{
						case NaNBoxing.BoxType.Number:
							if (double.IsNaN(v2.Number)) return false;
							return v1.ShortValue == v2.Number;
						case NaNBoxing.BoxType.Int:
							return v1.ShortValue == v2.IntValue;
						case NaNBoxing.BoxType.Uint:
							return v1.ShortValue == v2.UIntValue;
						case NaNBoxing.BoxType.Sbyte:
							return v1.ShortValue == v2.SByteValue;
						case NaNBoxing.BoxType.Byte:
							return v1.ShortValue == v2.ByteValue;
						case NaNBoxing.BoxType.Short:
							return v1.ShortValue == v2.ShortValue;
						case NaNBoxing.BoxType.UShort:
							return v1.ShortValue == v2.UShortValue;
						case NaNBoxing.BoxType.Float:
							if (float.IsNaN(v2.FloatValue)) return false;
							return v1.ShortValue == v2.FloatValue;
						case NaNBoxing.BoxType.HeapPtr:
						case NaNBoxing.BoxType.Fault:
						case NaNBoxing.BoxType.Undefined:
						case NaNBoxing.BoxType.Null:
						case NaNBoxing.BoxType.Boolean:
						default:
							break;
					}
					break;
				case BoxType.UShort:
					switch (v2.ValueType)
					{
						case NaNBoxing.BoxType.Number:
							if (double.IsNaN(v2.Number)) return false;
							return v1.UShortValue == v2.Number;
						case NaNBoxing.BoxType.Int:
							return v1.UShortValue == v2.IntValue;
						case NaNBoxing.BoxType.Uint:
							return v1.UShortValue == v2.UIntValue;
						case NaNBoxing.BoxType.Sbyte:
							return v1.UShortValue == v2.SByteValue;
						case NaNBoxing.BoxType.Byte:
							return v1.UShortValue == v2.ByteValue;
						case NaNBoxing.BoxType.Short:
							return v1.UShortValue == v2.ShortValue;
						case NaNBoxing.BoxType.UShort:
							return v1.UShortValue == v2.UShortValue;
						case NaNBoxing.BoxType.Float:
							if (float.IsNaN(v2.FloatValue)) return false;
							return v1.UShortValue == v2.FloatValue;
						case NaNBoxing.BoxType.HeapPtr:
						case NaNBoxing.BoxType.Fault:
						case NaNBoxing.BoxType.Undefined:
						case NaNBoxing.BoxType.Null:
						case NaNBoxing.BoxType.Boolean:
						default:
							break;
					}
					break;
				case BoxType.Float:
					switch (v2.ValueType)
					{
						case NaNBoxing.BoxType.Number:
							if (float.IsNaN(v1.FloatValue) || double.IsNaN(v2.Number)) return false;
							return v1.FloatValue == v2.Number;
						case NaNBoxing.BoxType.Int:
							if (float.IsNaN(v1.FloatValue)) return false;
							return v1.FloatValue == v2.IntValue;
						case NaNBoxing.BoxType.Uint:
							if (float.IsNaN(v1.FloatValue)) return false;
							return v1.FloatValue == v2.UIntValue;
						case NaNBoxing.BoxType.Sbyte:
							if (float.IsNaN(v1.FloatValue)) return false;
							return v1.FloatValue == v2.SByteValue;
						case NaNBoxing.BoxType.Byte:
							if (float.IsNaN(v1.FloatValue)) return false;
							return v1.FloatValue == v2.ByteValue;
						case NaNBoxing.BoxType.Short:
							if (float.IsNaN(v1.FloatValue)) return false;
							return v1.FloatValue == v2.ShortValue;
						case NaNBoxing.BoxType.UShort:
							if (float.IsNaN(v1.FloatValue)) return false;
							return v1.FloatValue == v2.UShortValue;
						case NaNBoxing.BoxType.Float:
							if (float.IsNaN(v1.FloatValue) || float.IsNaN(v2.FloatValue)) return false;
							return v1.FloatValue == v2.FloatValue;
						case NaNBoxing.BoxType.HeapPtr:
						case NaNBoxing.BoxType.Fault:
						case NaNBoxing.BoxType.Undefined:
						case NaNBoxing.BoxType.Null:
						case NaNBoxing.BoxType.Boolean:
						default:
							break;
					}
					break;
				default:
					break;
			}

			if (v1.ValueType == BoxType.Boolean && v2.ValueType == BoxType.Boolean)
			{
				return v1.Boolean == v2.Boolean;
			}

			if (v1.ValueType == BoxType.Null || v2.ValueType == BoxType.Null || v1.ValueType == BoxType.Undefined || v2.ValueType == BoxType.Undefined)
			{
				if ((v1.ValueType == BoxType.Null || v1.ValueType == BoxType.Undefined) && (v2.ValueType == BoxType.Null || v2.ValueType == BoxType.Undefined))
					return true;
				else
					return false;
			}



			if (!IsPrimitive(v1))
			{
#if DEBUG
				if (!IsPrimitive(v2))
				{
					throw new InvalidOperationException();
				}
#endif
				if (v2.ValueType == BoxType.HeapPtr)
				{//v2肯定是字符串
					v1 = ToPrimitive(ref error, v1, HINT.h_string, scope_ptr, tempStore, tempStore, stackslots, stackStPos, caller_bindthis_ptr);
				}
				else
				{
					v1 = ToPrimitive(ref error, v1, HINT.h_number, scope_ptr, tempStore, tempStore, stackslots, stackStPos, caller_bindthis_ptr);
				}

				if (error.raised)
				{
					return false;
				}

			}

			if (!IsPrimitive(v2))
			{
#if DEBUG
				if (!IsPrimitive(v1))
				{
					throw new InvalidOperationException();
				}
#endif

				if (v1.ValueType == BoxType.HeapPtr)
				{//v1肯定是字符串
					v2 = ToPrimitive(ref error, v2, HINT.h_string, scope_ptr, tempStore, tempStore, stackslots, stackStPos, caller_bindthis_ptr);
				}
				else
				{
					v2 = ToPrimitive(ref error, v2, HINT.h_number, scope_ptr, tempStore, tempStore, stackslots, stackStPos, caller_bindthis_ptr);
				}

				if (error.raised)
				{
					return false;
				}

			}

			//已转成原始值
			//布尔转数字
			if (v1.ValueType == BoxType.Boolean && v2.ValueType != BoxType.Boolean)
			{
				v1.SetInt(v1.Boolean ? 1 : 0);
			}
			else if (v1.ValueType != BoxType.Boolean && v2.ValueType == BoxType.Boolean)
			{
				v2.SetInt(v2.Boolean ? 1 : 0);
			}

			//字符串转数字
			if ((v1.ValueType == BoxType.HeapPtr || v1.ValueType == BoxType.LocalString) && IsNumeric(v2))
			{
				ConvertValueType(ref error, v1, TypeKind.Number, Context.NUMBER, ref v1); //这里不会出错
			}

			if ((v2.ValueType == BoxType.HeapPtr || v2.ValueType == BoxType.LocalString) && IsNumeric(v1))
			{
				ConvertValueType(ref error, v2, TypeKind.Number, Context.NUMBER, ref v2);  //这里不会出错
			}


			goto flag_retest;
		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		internal bool IsEqual(NaNBoxing v1, NaNBoxing v2, int tempStore, ref ReceiveError error,
			int scope_ptr, Span<NaNBoxing> stackslots, int stackStPos, NaNBoxing caller_bindthis_ptr
			)
		{
			bool fast_comp;
			if (v1.FastTestComp(v2, out fast_comp))
			{
				return fast_comp;
			}
			else
			{ 
				return IsEqual_Slow(v1,v2, new StackLocater() { index = tempStore },ref error,scope_ptr,stackslots,stackStPos,caller_bindthis_ptr);
			}

		}


		private bool IsStrictlyEqSlow(NaNBoxing key1, NaNBoxing key2)
		{

			//如果操作数的类型不同，则返回 false。
			//如果两个操作数都是对象，则仅当它们引用同一个对象时才返回 true。
			//如果两个操作数都是 null 或两个操作数都是 undefined，则返回 true。
			//如果任一操作数为 NaN，则返回 false。
			//否则，比较两个操作数的值
			//数字必须具有相同的数值。+0 和 - 0 被认为是相同的值。
			//字符串必须具有相同的字符，并以相同的顺序排列。
			//布尔值必须都是 true 或都是 false。

			if (key1.ValueType == NaNBoxing.BoxType.HeapPtr && key2.ValueType == NaNBoxing.BoxType.HeapPtr)
			{

				if (key1.HeapKind != key2.HeapKind)
				{
					return false;
				}
				else if (key1.HeapKind == (byte)RtHeapTypeKind.STRING)
				{
					var ins1 = Context.GC.Heap[key1.HeapPtr];
					var ins2 = Context.GC.Heap[key2.HeapPtr];
					return string.CompareOrdinal(((RtString)ins1).Str, ((RtString)ins2).Str) == 0;
				}
				else if (key1.HeapKind == (byte)RtHeapTypeKind.INSTANCE)
				{
					var ins1 = Context.GC.Heap[key1.HeapPtr];
					var ins2 = Context.GC.Heap[key2.HeapPtr];
					if (((ASInstance)ins1.Type).Flags.HasFlag(ClassFlags.Struct))
					{
						var layoutsize = ins1.Type._link_codescope.TypeLayout.Size;

						return ((RtInstance)ins1).GetStoreData(this, (ASInstance)ins1.Type).Slice(0, layoutsize).SequenceEqual(
								 ((RtInstance)ins2).GetStoreData(this, (ASInstance)ins2.Type).Slice(0, layoutsize))
							;

					}
					else if (((ASInstance)ins1.Type).Flags.HasFlag(ClassFlags.Wapper))
					{
						return ((RtInstance)ins1).wapperedObject.Equals(((RtInstance)ins2).wapperedObject);
					}
					else
					{
						RtInstance tmp1, tmp2;
						return RtInstance.FindAndUpdateHeapInstancePtr(key1.HeapPtr, this, out tmp1) ==
							RtInstance.FindAndUpdateHeapInstancePtr(key2.HeapPtr, this, out tmp2);
						//return key1.HeapPtr == key2.HeapPtr;
					}
				}
				else
				{
					switch ((RtHeapTypeKind)key1.HeapKind)
					{
						case RtHeapTypeKind.CLASS:
						case RtHeapTypeKind.GLOBAL:
						case RtHeapTypeKind.NAMESPACE:
							return key1.HeapPtr == key2.HeapPtr;
						case RtHeapTypeKind.ARRAY:
							{
								RtArray tmp1, tmp2;
								return RtArray.FindAndUpdateHeapInstancePtr(key1.HeapPtr, this, out tmp1) ==
									RtArray.FindAndUpdateHeapInstancePtr(key2.HeapPtr, this, out tmp2);
							}
						case RtHeapTypeKind.VECTOR:

							{
								RtVector tmp1, tmp2;
								int v1 = RtVector.FindAndUpdateHeapInstancePtr(key1.HeapPtr, this, out tmp1);
								int v2 = RtVector.FindAndUpdateHeapInstancePtr(key2.HeapPtr, this, out tmp2);

								return v1 == v2;

							}

						case RtHeapTypeKind.CLOSURE:
							{
								var ins1 = Context.GC.Heap[key1.HeapPtr];
								var ins2 = Context.GC.Heap[key2.HeapPtr];
								RtClosure tmp1, tmp2;
								int ptr1 = RtClosure.FindAndUpdateHeapInstancePtr(key1.HeapPtr, this, out tmp1);
								int ptr2 = RtClosure.FindAndUpdateHeapInstancePtr(key2.HeapPtr, this, out tmp2);

								if (((ASMethodBody)ins1.Type).Method.__ismethod)
								{
									return ins1.Type == ins2.Type && IsStrictlyEqual(tmp1.This, tmp2.This);
								}
								else
								{
									return ptr1 == ptr2;
								}
							}
						case RtHeapTypeKind.STACK_CACHE_OBJ:
						case RtHeapTypeKind.DYNAMIC_PROPERTYS:
						case RtHeapTypeKind.SHAPE:
						case RtHeapTypeKind.MethodScope:
						default:
#if DEBUG
							throw new InvalidOperationException();
#else
							Environment.FailFast("出错了，这里跑不到"); return default;
#endif
					}
					//return key1.HeapPtr == key2.HeapPtr;
				}
			}
			else if (key1.ValueType != NaNBoxing.BoxType.HeapPtr && key2.ValueType != NaNBoxing.BoxType.HeapPtr)
			{
				if (key1.ValueType == NaNBoxing.BoxType.Null && key2.ValueType == NaNBoxing.BoxType.Null)
				{
					return true;
				}
				if (key1.ValueType == NaNBoxing.BoxType.Undefined && key2.ValueType == NaNBoxing.BoxType.Undefined)
				{
					return true;
				}
				if (key1.ValueType == NaNBoxing.BoxType.Boolean && key2.ValueType == NaNBoxing.BoxType.Boolean)
				{
					return key1.Boolean == key2.Boolean;
				}
				if ((key1.ValueType == NaNBoxing.BoxType.Float && float.IsNaN(key1.FloatValue))
					||
					(key2.ValueType == NaNBoxing.BoxType.Float && float.IsNaN(key2.FloatValue))
					||
					(key1.ValueType == NaNBoxing.BoxType.Number && double.IsNaN(key1.Number))
					||
					(key2.ValueType == NaNBoxing.BoxType.Number && double.IsNaN(key2.Number))
					)
				{
					return false;
				}

				switch (key1.ValueType)
				{
					case NaNBoxing.BoxType.Number:
						switch (key2.ValueType)
						{
							case NaNBoxing.BoxType.Number:
								return key1.Number == key2.Number;
							case NaNBoxing.BoxType.Int:
								return key1.Number == key2.IntValue;
							case NaNBoxing.BoxType.Uint:
								return key1.Number == key2.UIntValue;
							case NaNBoxing.BoxType.Sbyte:
								return key1.Number == key2.SByteValue;
							case NaNBoxing.BoxType.Byte:
								return key1.Number == key2.ByteValue;
							case NaNBoxing.BoxType.Short:
								return key1.Number == key2.ShortValue;
							case NaNBoxing.BoxType.UShort:
								return key1.Number == key2.UShortValue;
							case NaNBoxing.BoxType.Float:
								return key1.Number == key2.FloatValue;
							case NaNBoxing.BoxType.HeapPtr:
							case NaNBoxing.BoxType.Fault:
							case NaNBoxing.BoxType.Undefined:
							case NaNBoxing.BoxType.Null:
							case NaNBoxing.BoxType.Boolean:
							default:
								return false;
						}
					case NaNBoxing.BoxType.Int:
						switch (key2.ValueType)
						{
							case NaNBoxing.BoxType.Number:
								return key1.IntValue == key2.Number;
							case NaNBoxing.BoxType.Int:
								return key1.IntValue == key2.IntValue;
							case NaNBoxing.BoxType.Uint:
								return key1.IntValue == key2.UIntValue;
							case NaNBoxing.BoxType.Sbyte:
								return key1.IntValue == key2.SByteValue;
							case NaNBoxing.BoxType.Byte:
								return key1.IntValue == key2.ByteValue;
							case NaNBoxing.BoxType.Short:
								return key1.IntValue == key2.ShortValue;
							case NaNBoxing.BoxType.UShort:
								return key1.IntValue == key2.UShortValue;
							case NaNBoxing.BoxType.Float:
								return key1.IntValue == key2.FloatValue;
							case NaNBoxing.BoxType.HeapPtr:
							case NaNBoxing.BoxType.Fault:
							case NaNBoxing.BoxType.Undefined:
							case NaNBoxing.BoxType.Null:
							case NaNBoxing.BoxType.Boolean:
							default:
								return false;
						}
					case NaNBoxing.BoxType.Uint:
						switch (key2.ValueType)
						{
							case NaNBoxing.BoxType.Number:
								return key1.UIntValue == key2.Number;
							case NaNBoxing.BoxType.Int:
								return key1.UIntValue == key2.IntValue;
							case NaNBoxing.BoxType.Uint:
								return key1.UIntValue == key2.UIntValue;
							case NaNBoxing.BoxType.Sbyte:
								return key1.UIntValue == key2.SByteValue;
							case NaNBoxing.BoxType.Byte:
								return key1.UIntValue == key2.ByteValue;
							case NaNBoxing.BoxType.Short:
								return key1.UIntValue == key2.ShortValue;
							case NaNBoxing.BoxType.UShort:
								return key1.UIntValue == key2.UShortValue;
							case NaNBoxing.BoxType.Float:
								return key1.UIntValue == key2.FloatValue;
							case NaNBoxing.BoxType.HeapPtr:
							case NaNBoxing.BoxType.Fault:
							case NaNBoxing.BoxType.Undefined:
							case NaNBoxing.BoxType.Null:
							case NaNBoxing.BoxType.Boolean:
							default:
								return false;
						}
					case NaNBoxing.BoxType.Sbyte:
						switch (key2.ValueType)
						{
							case NaNBoxing.BoxType.Number:
								return key1.SByteValue == key2.Number;
							case NaNBoxing.BoxType.Int:
								return key1.SByteValue == key2.IntValue;
							case NaNBoxing.BoxType.Uint:
								return key1.SByteValue == key2.UIntValue;
							case NaNBoxing.BoxType.Sbyte:
								return key1.SByteValue == key2.SByteValue;
							case NaNBoxing.BoxType.Byte:
								return key1.SByteValue == key2.ByteValue;
							case NaNBoxing.BoxType.Short:
								return key1.SByteValue == key2.ShortValue;
							case NaNBoxing.BoxType.UShort:
								return key1.SByteValue == key2.UShortValue;
							case NaNBoxing.BoxType.Float:
								return key1.SByteValue == key2.FloatValue;
							case NaNBoxing.BoxType.HeapPtr:
							case NaNBoxing.BoxType.Fault:
							case NaNBoxing.BoxType.Undefined:
							case NaNBoxing.BoxType.Null:
							case NaNBoxing.BoxType.Boolean:
							default:
								return false;
						}
					case NaNBoxing.BoxType.Byte:
						switch (key2.ValueType)
						{
							case NaNBoxing.BoxType.Number:
								return key1.ByteValue == key2.Number;
							case NaNBoxing.BoxType.Int:
								return key1.ByteValue == key2.IntValue;
							case NaNBoxing.BoxType.Uint:
								return key1.ByteValue == key2.UIntValue;
							case NaNBoxing.BoxType.Sbyte:
								return key1.ByteValue == key2.SByteValue;
							case NaNBoxing.BoxType.Byte:
								return key1.ByteValue == key2.ByteValue;
							case NaNBoxing.BoxType.Short:
								return key1.ByteValue == key2.ShortValue;
							case NaNBoxing.BoxType.UShort:
								return key1.ByteValue == key2.UShortValue;
							case NaNBoxing.BoxType.Float:
								return key1.ByteValue == key2.FloatValue;
							case NaNBoxing.BoxType.HeapPtr:
							case NaNBoxing.BoxType.Fault:
							case NaNBoxing.BoxType.Undefined:
							case NaNBoxing.BoxType.Null:
							case NaNBoxing.BoxType.Boolean:
							default:
								return false;
						}
					case NaNBoxing.BoxType.Short:
						switch (key2.ValueType)
						{
							case NaNBoxing.BoxType.Number:
								return key1.ShortValue == key2.Number;
							case NaNBoxing.BoxType.Int:
								return key1.ShortValue == key2.IntValue;
							case NaNBoxing.BoxType.Uint:
								return key1.ShortValue == key2.UIntValue;
							case NaNBoxing.BoxType.Sbyte:
								return key1.ShortValue == key2.SByteValue;
							case NaNBoxing.BoxType.Byte:
								return key1.ShortValue == key2.ByteValue;
							case NaNBoxing.BoxType.Short:
								return key1.ShortValue == key2.ShortValue;
							case NaNBoxing.BoxType.UShort:
								return key1.ShortValue == key2.UShortValue;
							case NaNBoxing.BoxType.Float:
								return key1.ShortValue == key2.FloatValue;
							case NaNBoxing.BoxType.HeapPtr:
							case NaNBoxing.BoxType.Fault:
							case NaNBoxing.BoxType.Undefined:
							case NaNBoxing.BoxType.Null:
							case NaNBoxing.BoxType.Boolean:
							default:
								return false;
						}
					case NaNBoxing.BoxType.UShort:
						switch (key2.ValueType)
						{
							case NaNBoxing.BoxType.Number:
								return key1.UShortValue == key2.Number;
							case NaNBoxing.BoxType.Int:
								return key1.UShortValue == key2.IntValue;
							case NaNBoxing.BoxType.Uint:
								return key1.UShortValue == key2.UIntValue;
							case NaNBoxing.BoxType.Sbyte:
								return key1.UShortValue == key2.SByteValue;
							case NaNBoxing.BoxType.Byte:
								return key1.UShortValue == key2.ByteValue;
							case NaNBoxing.BoxType.Short:
								return key1.UShortValue == key2.ShortValue;
							case NaNBoxing.BoxType.UShort:
								return key1.UShortValue == key2.UShortValue;
							case NaNBoxing.BoxType.Float:
								return key1.UShortValue == key2.FloatValue;
							case NaNBoxing.BoxType.HeapPtr:
							case NaNBoxing.BoxType.Fault:
							case NaNBoxing.BoxType.Undefined:
							case NaNBoxing.BoxType.Null:
							case NaNBoxing.BoxType.Boolean:
							default:
								return false;
						}
					case NaNBoxing.BoxType.Float:
						switch (key2.ValueType)
						{
							case NaNBoxing.BoxType.Number:
								return key1.FloatValue == key2.Number;
							case NaNBoxing.BoxType.Int:
								return key1.FloatValue == key2.IntValue;
							case NaNBoxing.BoxType.Uint:
								return key1.FloatValue == key2.UIntValue;
							case NaNBoxing.BoxType.Sbyte:
								return key1.FloatValue == key2.SByteValue;
							case NaNBoxing.BoxType.Byte:
								return key1.FloatValue == key2.ByteValue;
							case NaNBoxing.BoxType.Short:
								return key1.FloatValue == key2.ShortValue;
							case NaNBoxing.BoxType.UShort:
								return key1.FloatValue == key2.UShortValue;
							case NaNBoxing.BoxType.Float:
								return key1.FloatValue == key2.FloatValue;
							case NaNBoxing.BoxType.HeapPtr:
							case NaNBoxing.BoxType.Fault:
							case NaNBoxing.BoxType.Undefined:
							case NaNBoxing.BoxType.Null:
							case NaNBoxing.BoxType.Boolean:
							default:
								return false;
						}
					case NaNBoxing.BoxType.HeapPtr:
					case NaNBoxing.BoxType.Fault:
					case NaNBoxing.BoxType.Undefined:
					case NaNBoxing.BoxType.Null:
					case NaNBoxing.BoxType.Boolean:
					default:
						return false;
				}
			}
			else
			{
				// LocalString与HeapPtr字符串比较
				if (key1.ValueType == NaNBoxing.BoxType.LocalString && key2.ValueType == NaNBoxing.BoxType.HeapPtr)
				{

					if (key2.HeapKind == (byte)RtHeapTypeKind.STRING)
					{
						var ins2 = Context.GC.Heap[key2.HeapPtr];
						string str2 = ((RtString)ins2).Str;

						// 使用高效的字符比较，避免创建LocalString的字符串
						Span<char> chars1 = stackalloc char[16];
						int charCount1 = key1.GetLocalStringChars(chars1);
						if (charCount1 < 0) return false; // 解码失败

						return str2.AsSpan().SequenceEqual(chars1.Slice(0, charCount1));
					}
				}
				else if (key1.ValueType == NaNBoxing.BoxType.HeapPtr && key2.ValueType == NaNBoxing.BoxType.LocalString)
				{

					if (key1.HeapKind == (byte)RtHeapTypeKind.STRING)
					{
						var ins1 = Context.GC.Heap[key1.HeapPtr];
						string str1 = ((RtString)ins1).Str;

						// 使用高效的字符比较，避免创建LocalString的字符串
						Span<char> chars2 = stackalloc char[16];
						int charCount2 = key2.GetLocalStringChars(chars2);
						if (charCount2 < 0) return false; // 解码失败

						return str1.AsSpan().SequenceEqual(chars2.Slice(0, charCount2));
					}
				}

				return false;
			}

			//throw new NotImplementedException();
		}
		/// <summary>
		/// 是否严格相等
		/// </summary>
		/// <param name="key1"></param>
		/// <param name="key2"></param>
		/// <returns></returns>
		/// <exception cref="NotImplementedException"></exception>
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		internal bool IsStrictlyEqual(NaNBoxing key1, NaNBoxing key2)
		{
			bool fast_comp;
			if (key1.FastTestComp(key2, out fast_comp))
			{
				return fast_comp;
			}
			else
			{ 
				return IsStrictlyEqSlow(key1, key2);
			}
		}



		private void ConvertValueSlow(ref ReceiveError error, NaNBoxing invalue, TypeKind totype, ASClass @totype_class, ref NaNBoxing outvalue, int scope_ptr , NaNBoxing callee_bindthis , bool is_from_objtostring )
		{

			RtHeapBase to_invoke = null;
			HINT hint = HINT.h_number;

			bool isRetry = false;

		lbl_retry:

			switch (totype)
			{
				case TypeKind.Any:
					outvalue = invalue;
					return;
				case TypeKind.Boolean:

					switch (invalue.ValueType)
					{
						case NaNBoxing.BoxType.Number:
							outvalue.SetBoolean(!(invalue.Number == 0 || double.IsNaN(invalue.Number)));
							return;
						case NaNBoxing.BoxType.Undefined:
							outvalue.SetBoolean(false);
							return;
						case NaNBoxing.BoxType.Null:
							outvalue.SetBoolean(false);
							return;
						case NaNBoxing.BoxType.Boolean:
							outvalue = invalue;
							return;
						case NaNBoxing.BoxType.Int:
							outvalue.SetBoolean(invalue.IntValue != 0);
							return;
						case NaNBoxing.BoxType.Uint:
							outvalue.SetBoolean(invalue.UIntValue != 0);
							return;
						case NaNBoxing.BoxType.Sbyte:
							outvalue.SetBoolean(invalue.SByteValue != 0);
							return;
						case NaNBoxing.BoxType.Byte:
							outvalue.SetBoolean(invalue.ByteValue != 0);
							return;
						case NaNBoxing.BoxType.Short:
							outvalue.SetBoolean(invalue.ShortValue != 0);
							return;
						case NaNBoxing.BoxType.UShort:
							outvalue.SetBoolean(invalue.UShortValue != 0);
							return;
						case NaNBoxing.BoxType.Float:
							outvalue.SetBoolean(!(invalue.FloatValue == 0 || float.IsNaN(invalue.FloatValue)));
							return;
						case NaNBoxing.BoxType.LocalString:
							{
								// LocalString to Boolean: false if empty string, true otherwise
								// 使用高效方法检查是否为空字符串，避免创建字符串对象
								Span<byte> bytes = stackalloc byte[5];
								int byteCount = invalue.GetLocalStringBytes(bytes);
								outvalue.SetBoolean(byteCount > 0);
								return;
							}
						case NaNBoxing.BoxType.HeapPtr:
							{
								if (invalue.HeapKind == (byte)RtHeapTypeKind.STRING
									&&
									string.IsNullOrEmpty(((RtString)Context.GC.Heap[invalue.HeapPtr]).Str)
									)
								{
									outvalue.SetBoolean(false);
								}
								else
								{
									outvalue.SetBoolean(true);
								}
								return;
							}
						case NaNBoxing.BoxType.Fault:
						default:
#if DEBUG
							throw new InvalidOperationException();
#else
							Environment.FailFast("出错了，这里跑不到"); return;
#endif
					}

				case TypeKind.SByte:


					switch (invalue.ValueType)
					{
						case NaNBoxing.BoxType.Number:
							if (double.IsNaN(invalue.Number) || double.IsInfinity(invalue.Number))
							{
								outvalue.SetSByte(0);
							}
							else
							{
								outvalue.SetSByte((sbyte)(long)invalue.Number);
							}
							return;
						case NaNBoxing.BoxType.Undefined:
							outvalue.SetSByte(0);
							return;
						case NaNBoxing.BoxType.Null:
							outvalue.SetSByte(0);
							return;
						case NaNBoxing.BoxType.Boolean:
							outvalue.SetSByte(invalue.Boolean ? (sbyte)1 : (sbyte)0);
							return;
						case NaNBoxing.BoxType.Int:
							outvalue.SetSByte((sbyte)invalue.IntValue);
							return;
						case NaNBoxing.BoxType.Uint:
							outvalue.SetSByte((sbyte)invalue.UIntValue);
							return;
						case NaNBoxing.BoxType.Sbyte:
							outvalue = invalue;
							return;
						case NaNBoxing.BoxType.Byte:
							outvalue.SetSByte((sbyte)invalue.ByteValue);
							return;
						case NaNBoxing.BoxType.Short:
							outvalue.SetSByte((sbyte)invalue.ShortValue);
							return;
						case NaNBoxing.BoxType.UShort:
							outvalue.SetSByte((sbyte)invalue.UShortValue);
							return;
						case NaNBoxing.BoxType.Float:
							if (float.IsNaN(invalue.FloatValue) || float.IsInfinity(invalue.FloatValue))
							{
								outvalue.SetSByte(0);
							}
							else
							{
								outvalue.SetSByte((sbyte)(long)invalue.FloatValue);
							}
							return;
						case NaNBoxing.BoxType.LocalString:
							{
								// LocalString to SByte: parse string as integer
								// 使用高效方法获取字符串，只在需要时创建
								Span<char> chars = stackalloc char[16];
								int charCount = invalue.GetLocalStringChars(chars);
								if (charCount <= 0)
								{
									outvalue.SetSByte(0);
									return;
								}
								string str = new string(chars.Slice(0, charCount));
								int v = ReadIntFromString(str);
								outvalue.SetSByte((sbyte)v);
								return;
							}
						case NaNBoxing.BoxType.HeapPtr:
							{
								if (invalue.HeapKind == (byte)RtHeapTypeKind.STRING)
								{
									var instance = Context.GC.Heap[invalue.HeapPtr];
									var str = ((RtString)instance).Str;
									int v = ReadIntFromString(str);
									outvalue.SetSByte((sbyte)v);
								}
								else if (invalue.HeapKind == (byte)RtHeapTypeKind.INSTANCE)
								{
									if (scope_ptr == 0)
									{
										outvalue.SetSByte(0);
									}
									else
									{
										var instance = Context.GC.Heap[invalue.HeapPtr];
										to_invoke = instance;
										hint = HINT.h_number;
										goto lbl_toprimitive;
									}
								}
								else
								{
									outvalue.SetSByte(0);
								}
								return;
							}
						case NaNBoxing.BoxType.Fault:
						default:
#if DEBUG
							throw new InvalidOperationException();
#else
							Environment.FailFast("出错了，这里跑不到"); return;
#endif
					}



				case TypeKind.Byte:
					switch (invalue.ValueType)
					{
						case NaNBoxing.BoxType.Number:
							if (double.IsNaN(invalue.Number) || double.IsInfinity(invalue.Number))
							{
								outvalue.SetByte(0);
							}
							else
							{
								outvalue.SetByte((byte)(long)invalue.Number);
							}
							return;
						case NaNBoxing.BoxType.Undefined:
							outvalue.SetByte(0);
							return;
						case NaNBoxing.BoxType.Null:
							outvalue.SetByte(0);
							return;
						case NaNBoxing.BoxType.Boolean:
							outvalue.SetByte(invalue.Boolean ? (byte)1 : (byte)0);
							return;
						case NaNBoxing.BoxType.Int:
							outvalue.SetByte((byte)invalue.IntValue);
							return;
						case NaNBoxing.BoxType.Uint:
							outvalue.SetByte((byte)invalue.UIntValue);
							return;
						case NaNBoxing.BoxType.Sbyte:
							outvalue.SetByte((byte)invalue.SByteValue);
							return;
						case NaNBoxing.BoxType.Byte:
							outvalue = invalue;
							return;
						case NaNBoxing.BoxType.Short:
							outvalue.SetByte((byte)invalue.ShortValue);
							return;
						case NaNBoxing.BoxType.UShort:
							outvalue.SetByte((byte)invalue.UShortValue);
							return;
						case NaNBoxing.BoxType.Float:
							if (float.IsNaN(invalue.FloatValue) || float.IsInfinity(invalue.FloatValue))
							{
								outvalue.SetByte(0);
							}
							else
							{
								outvalue.SetByte((byte)(long)invalue.FloatValue);
							}
							return;
						case NaNBoxing.BoxType.LocalString:
							{
								// LocalString to Byte: parse string as unsigned integer
								// 使用高效方法获取字符串，只在需要时创建
								Span<char> chars = stackalloc char[16];
								int charCount = invalue.GetLocalStringChars(chars);
								if (charCount <= 0)
								{
									outvalue.SetByte(0);
									return;
								}
								string str = new string(chars.Slice(0, charCount));
								uint v = ReadUIntFromString(str);
								outvalue.SetByte((byte)v);
								return;
							}
						case NaNBoxing.BoxType.HeapPtr:
							{
								if (invalue.HeapKind == (byte)RtHeapTypeKind.STRING)
								{
									var instance = Context.GC.Heap[invalue.HeapPtr];
									var str = ((RtString)instance).Str;
									uint v = ReadUIntFromString(str);
									outvalue.SetByte((byte)v);
								}
								else if (invalue.HeapKind == (byte)RtHeapTypeKind.INSTANCE)
								{
									if (scope_ptr == 0)
									{
										outvalue.SetByte(0);
									}
									else
									{
										var instance = Context.GC.Heap[invalue.HeapPtr];
										to_invoke = instance;
										hint = HINT.h_number;
										goto lbl_toprimitive;
									}
								}
								else
								{
									outvalue.SetByte(0);
								}
								return;
							}
						case NaNBoxing.BoxType.Fault:
						default:
#if DEBUG
							throw new InvalidOperationException();
#else
							Environment.FailFast("出错了，这里跑不到"); return;
#endif
					}
				case TypeKind.Short:
					switch (invalue.ValueType)
					{
						case NaNBoxing.BoxType.Number:
							if (double.IsNaN(invalue.Number) || double.IsInfinity(invalue.Number))
							{
								outvalue.SetShort(0);
							}
							else
							{
								outvalue.SetShort((short)(long)invalue.Number);
							}
							return;
						case NaNBoxing.BoxType.Undefined:
							outvalue.SetShort(0);
							return;
						case NaNBoxing.BoxType.Null:
							outvalue.SetShort(0);
							return;
						case NaNBoxing.BoxType.Boolean:
							outvalue.SetShort(invalue.Boolean ? (short)1 : (short)0);
							return;
						case NaNBoxing.BoxType.Int:
							outvalue.SetShort((short)invalue.IntValue);
							return;
						case NaNBoxing.BoxType.Uint:
							outvalue.SetShort((short)invalue.UIntValue);
							return;
						case NaNBoxing.BoxType.Sbyte:
							outvalue.SetShort((short)invalue.SByteValue);
							return;
						case NaNBoxing.BoxType.Byte:
							outvalue.SetShort((short)invalue.ByteValue);
							return;
						case NaNBoxing.BoxType.Short:
							outvalue = invalue;
							return;
						case NaNBoxing.BoxType.UShort:
							outvalue.SetShort((short)invalue.UShortValue);
							return;
						case NaNBoxing.BoxType.Float:
							if (float.IsNaN(invalue.FloatValue) || float.IsInfinity(invalue.FloatValue))
							{
								outvalue.SetShort(0);
							}
							else
							{
								outvalue.SetShort((short)(long)invalue.FloatValue);
							}
							return;
						case NaNBoxing.BoxType.LocalString:
							{
								// LocalString to Short: parse string as integer
								// 使用高效方法获取字符串，只在需要时创建
								Span<char> chars = stackalloc char[16];
								int charCount = invalue.GetLocalStringChars(chars);
								if (charCount <= 0)
								{
									outvalue.SetShort(0);
									return;
								}
								string str = new string(chars.Slice(0, charCount));
								int v = ReadIntFromString(str);
								outvalue.SetShort((short)v);
								return;
							}
						case NaNBoxing.BoxType.HeapPtr:
							{
								if (invalue.HeapKind == (byte)RtHeapTypeKind.STRING)
								{
									var instance = Context.GC.Heap[invalue.HeapPtr];
									var str = ((RtString)instance).Str;
									int v = ReadIntFromString(str);
									outvalue.SetShort((short)v);
								}
								else if (invalue.HeapKind == (byte)RtHeapTypeKind.INSTANCE)
								{
									if (scope_ptr == 0)
									{
										outvalue.SetShort(0);
									}
									else
									{
										var instance = Context.GC.Heap[invalue.HeapPtr];
										to_invoke = instance;
										hint = HINT.h_number;
										goto lbl_toprimitive;
									}
								}
								else
								{
									outvalue.SetShort(0);
								}
								return;
							}
						case NaNBoxing.BoxType.Fault:
						default:
#if DEBUG
							throw new InvalidOperationException();
#else
							Environment.FailFast("出错了，这里跑不到"); return;
#endif
					}

				case TypeKind.UShort:

					switch (invalue.ValueType)
					{
						case NaNBoxing.BoxType.Number:
							if (double.IsNaN(invalue.Number) || double.IsInfinity(invalue.Number))
							{
								outvalue.SetUShort(0);
							}
							else
							{
								outvalue.SetUShort((ushort)(long)invalue.Number);
							}
							return;
						case NaNBoxing.BoxType.Undefined:
							outvalue.SetUShort(0);
							return;
						case NaNBoxing.BoxType.Null:
							outvalue.SetUShort(0);
							return;
						case NaNBoxing.BoxType.Boolean:
							outvalue.SetUShort(invalue.Boolean ? (ushort)1 : (ushort)0);
							return;
						case NaNBoxing.BoxType.Int:
							outvalue.SetUShort((ushort)invalue.IntValue);
							return;
						case NaNBoxing.BoxType.Uint:
							outvalue.SetUShort((ushort)invalue.UIntValue);
							return;
						case NaNBoxing.BoxType.Sbyte:
							outvalue.SetUShort((ushort)invalue.SByteValue);
							return;
						case NaNBoxing.BoxType.Byte:
							outvalue.SetUShort((ushort)invalue.ByteValue);
							return;
						case NaNBoxing.BoxType.Short:
							outvalue.SetUShort((ushort)invalue.ShortValue);
							return;
						case NaNBoxing.BoxType.UShort:
							outvalue = invalue;
							return;
						case NaNBoxing.BoxType.Float:
							if (float.IsNaN(invalue.FloatValue) || float.IsInfinity(invalue.FloatValue))
							{
								outvalue.SetUShort(0);
							}
							else
							{
								outvalue.SetUShort((ushort)(long)invalue.FloatValue);
							}
							return;
						case NaNBoxing.BoxType.LocalString:
							{
								// LocalString to UShort: parse string as unsigned integer
								// Use efficient char-based parsing to avoid string allocation
								Span<char> chars = stackalloc char[16];
								int charCount = invalue.GetLocalStringChars(chars);
								if (charCount > 0)
								{
									uint v = ReadUIntFromString(new string(chars.Slice(0, charCount)));
									outvalue.SetUShort((ushort)v);
								}
								else
								{
									outvalue.SetUShort(0);
								}
								return;
							}
						case NaNBoxing.BoxType.HeapPtr:
							{
								if (invalue.HeapKind == (byte)RtHeapTypeKind.STRING)
								{
									var instance = Context.GC.Heap[invalue.HeapPtr];
									var str = ((RtString)instance).Str;
									uint v = ReadUIntFromString(str);
									outvalue.SetUShort((ushort)v);
								}
								else if (invalue.HeapKind == (byte)RtHeapTypeKind.INSTANCE)
								{
									if (scope_ptr == 0)
									{
										outvalue.SetUShort(0);
									}
									else
									{
										var instance = Context.GC.Heap[invalue.HeapPtr];
										to_invoke = instance;
										hint = HINT.h_number;
										goto lbl_toprimitive;
									}
								}
								else
								{
									outvalue.SetUShort(0);
								}
								return;
							}
						case NaNBoxing.BoxType.Fault:
						default:
#if DEBUG
							throw new InvalidOperationException();
#else
							Environment.FailFast("出错了，这里跑不到"); return;
#endif
					}

				case TypeKind.Int:
					switch (invalue.ValueType)
					{
						case NaNBoxing.BoxType.Number:
							if (double.IsNaN(invalue.Number) || double.IsInfinity(invalue.Number))
							{
								outvalue.SetInt(0);
							}
							else
							{
								outvalue.SetInt((int)(long)invalue.Number);
							}
							return;
						case NaNBoxing.BoxType.Undefined:
							outvalue.SetInt(0);
							return;
						case NaNBoxing.BoxType.Null:
							outvalue.SetInt(0);
							return;
						case NaNBoxing.BoxType.Boolean:
							outvalue.SetInt(invalue.Boolean ? (int)1 : (int)0);
							return;
						case NaNBoxing.BoxType.Int:
							outvalue = invalue;
							return;
						case NaNBoxing.BoxType.Uint:
							outvalue.SetInt((int)invalue.UIntValue);
							return;
						case NaNBoxing.BoxType.Sbyte:
							outvalue.SetInt(invalue.SByteValue);
							return;
						case NaNBoxing.BoxType.Byte:
							outvalue.SetInt(invalue.ByteValue);
							return;
						case NaNBoxing.BoxType.Short:
							outvalue.SetInt(invalue.ShortValue);
							return;
						case NaNBoxing.BoxType.UShort:
							outvalue.SetInt(invalue.UShortValue);
							return;
						case NaNBoxing.BoxType.Float:
							if (float.IsNaN(invalue.FloatValue) || float.IsInfinity(invalue.FloatValue))
							{
								outvalue.SetInt(0);
							}
							else
							{
								outvalue.SetInt((int)(long)invalue.FloatValue);
							}
							return;
						case NaNBoxing.BoxType.LocalString:
							{
								// LocalString to Int: parse string as integer
								// Use efficient char-based parsing to avoid string allocation
								Span<char> chars = stackalloc char[16];
								int charCount = invalue.GetLocalStringChars(chars);
								if (charCount > 0)
								{
									int v = ReadIntFromString(new string(chars.Slice(0, charCount)));
									outvalue.SetInt(v);
								}
								else
								{
									outvalue.SetInt(0);
								}
								return;
							}
						case NaNBoxing.BoxType.HeapPtr:
							{
								if (invalue.HeapKind == (byte)RtHeapTypeKind.STRING)
								{
									var instance = Context.GC.Heap[invalue.HeapPtr];
									var str = ((RtString)instance).Str;
									int v = ReadIntFromString(str);
									outvalue.SetInt(v);
								}
								else if (invalue.HeapKind == (byte)RtHeapTypeKind.INSTANCE)
								{
									if (scope_ptr == 0)
									{
										outvalue.SetInt(0);
									}
									else
									{
										var instance = Context.GC.Heap[invalue.HeapPtr];
										to_invoke = instance;
										hint = HINT.h_number;
										goto lbl_toprimitive;
									}
								}
								else
								{
									outvalue.SetInt(0);
								}
								return;
							}
						case NaNBoxing.BoxType.Fault:
						default:
#if DEBUG
							throw new InvalidOperationException();
#else
							Environment.FailFast("出错了，这里跑不到"); return;
#endif
					}
				case TypeKind.Uint:
					switch (invalue.ValueType)
					{
						case NaNBoxing.BoxType.Number:
							if (double.IsNaN(invalue.Number) || double.IsInfinity(invalue.Number))
							{
								outvalue.SetUInt(0);
							}
							else
							{
								outvalue.SetUInt((uint)(long)invalue.Number);
							}
							return;
						case NaNBoxing.BoxType.Undefined:
							outvalue.SetUInt(0);
							return;
						case NaNBoxing.BoxType.Null:
							outvalue.SetUInt(0);
							return;
						case NaNBoxing.BoxType.Boolean:
							outvalue.SetUInt(invalue.Boolean ? (uint)1 : 0);
							return;
						case NaNBoxing.BoxType.Int:
							outvalue.SetUInt((uint)invalue.IntValue);
							return;
						case NaNBoxing.BoxType.Uint:
							outvalue = invalue;
							return;
						case NaNBoxing.BoxType.Sbyte:
							outvalue.SetUInt((uint)invalue.SByteValue);
							return;
						case NaNBoxing.BoxType.Byte:
							outvalue.SetUInt((uint)invalue.ByteValue);
							return;
						case NaNBoxing.BoxType.Short:
							outvalue.SetUInt((uint)invalue.ShortValue);
							return;
						case NaNBoxing.BoxType.UShort:
							outvalue.SetUInt(invalue.UShortValue);
							return;
						case NaNBoxing.BoxType.Float:
							if (float.IsNaN(invalue.FloatValue) || float.IsInfinity(invalue.FloatValue))
							{
								outvalue.SetUInt(0);
							}
							else
							{
								outvalue.SetUInt((uint)(long)invalue.FloatValue);
							}
							return;
						case NaNBoxing.BoxType.LocalString:
							{
								// LocalString to Uint: parse string as unsigned integer
								// Use efficient char-based parsing to avoid string allocation
								Span<char> chars = stackalloc char[16];
								int charCount = invalue.GetLocalStringChars(chars);
								if (charCount > 0)
								{
									uint v = ReadUIntFromString(new string(chars.Slice(0, charCount)));
									outvalue.SetUInt(v);
								}
								else
								{
									outvalue.SetUInt(0);
								}
								return;
							}
						case NaNBoxing.BoxType.HeapPtr:
							{
								if (invalue.HeapKind == (byte)RtHeapTypeKind.STRING)
								{
									var instance = Context.GC.Heap[invalue.HeapPtr];
									var str = ((RtString)instance).Str;
									uint v = ReadUIntFromString(str);
									outvalue.SetUInt(v);
								}
								else if (invalue.HeapKind == (byte)RtHeapTypeKind.INSTANCE)
								{
									if (scope_ptr == 0)
									{
										outvalue.SetUInt(0);
									}
									else
									{
										var instance = Context.GC.Heap[invalue.HeapPtr];
										to_invoke = instance;
										hint = HINT.h_number;
										goto lbl_toprimitive;
									}
								}
								else
								{
									outvalue.SetUInt(0);
								}
								return;
							}
						case NaNBoxing.BoxType.Fault:
						default:
#if DEBUG
							throw new InvalidOperationException();
#else
							Environment.FailFast("出错了，这里跑不到"); return;
#endif
					}
				case TypeKind.Float:
					switch (invalue.ValueType)
					{
						case NaNBoxing.BoxType.Number:

							outvalue.SetFloat((float)invalue.Number);

							return;
						case NaNBoxing.BoxType.Undefined:
							outvalue.SetFloat(float.NaN);
							return;
						case NaNBoxing.BoxType.Null:
							outvalue.SetFloat(0);
							return;
						case NaNBoxing.BoxType.Boolean:
							outvalue.SetFloat(invalue.Boolean ? 1 : 0);
							return;
						case NaNBoxing.BoxType.Int:
							outvalue.SetFloat((float)invalue.IntValue);
							return;
						case NaNBoxing.BoxType.Uint:
							outvalue.SetFloat((float)invalue.UIntValue);
							return;
						case NaNBoxing.BoxType.Sbyte:
							outvalue.SetFloat(invalue.SByteValue);
							return;
						case NaNBoxing.BoxType.Byte:
							outvalue.SetFloat(invalue.ByteValue);
							return;
						case NaNBoxing.BoxType.Short:
							outvalue.SetFloat(invalue.ShortValue);
							return;
						case NaNBoxing.BoxType.UShort:
							outvalue.SetFloat(invalue.UShortValue);
							return;
						case NaNBoxing.BoxType.Float:
							outvalue = invalue;
							return;
						case NaNBoxing.BoxType.LocalString:
							{
								// LocalString to Float: parse string as double then convert to float
								// Use efficient char-based parsing to avoid string allocation
								Span<char> chars = stackalloc char[16];
								int charCount = invalue.GetLocalStringChars(chars);
								if (charCount > 0)
								{
									double v = ReadDoubleFromString(new string(chars.Slice(0, charCount)));
									outvalue.SetFloat((float)v);
								}
								else
								{
									outvalue.SetFloat(float.NaN);
								}
								return;
							}
						case NaNBoxing.BoxType.HeapPtr:
							{
								if (invalue.HeapKind == (byte)RtHeapTypeKind.STRING)
								{
									var instance = Context.GC.Heap[invalue.HeapPtr];
									var str = ((RtString)instance).Str;
									double v = ReadDoubleFromString(str);
									outvalue.SetFloat((float)v);
								}
								else if (invalue.HeapKind == (byte)RtHeapTypeKind.INSTANCE)
								{
									if (scope_ptr == 0)
									{
										outvalue.SetFloat(float.NaN);
									}
									else
									{
										var instance = Context.GC.Heap[invalue.HeapPtr];
										to_invoke = instance;
										hint = HINT.h_number;
										goto lbl_toprimitive;
									}
								}
								else
								{
									outvalue.SetFloat(float.NaN);
								}
								return;
							}
						case NaNBoxing.BoxType.Fault:
						default:
#if DEBUG
							throw new InvalidOperationException();
#else
							Environment.FailFast("出错了，这里跑不到"); return;
#endif
					}
				case TypeKind.Number:
					switch (invalue.ValueType)
					{
						case NaNBoxing.BoxType.Number:
							outvalue = invalue;
							return;
						case NaNBoxing.BoxType.Undefined:
							outvalue.SetNumber(double.NaN);
							return;
						case NaNBoxing.BoxType.Null:
							outvalue.SetNumber(0);
							return;
						case NaNBoxing.BoxType.Boolean:
							outvalue.SetNumber(invalue.Boolean ? 1 : 0);
							return;
						case NaNBoxing.BoxType.Int:
							outvalue.SetNumber(invalue.IntValue);
							return;
						case NaNBoxing.BoxType.Uint:
							outvalue.SetNumber(invalue.UIntValue);
							return;
						case NaNBoxing.BoxType.Sbyte:
							outvalue.SetNumber(invalue.SByteValue);
							return;
						case NaNBoxing.BoxType.Byte:
							outvalue.SetNumber(invalue.ByteValue);
							return;
						case NaNBoxing.BoxType.Short:
							outvalue.SetNumber(invalue.ShortValue);
							return;
						case NaNBoxing.BoxType.UShort:
							outvalue.SetNumber(invalue.UShortValue);
							return;
						case NaNBoxing.BoxType.Float:
							outvalue.SetNumber(invalue.FloatValue);
							return;
						case NaNBoxing.BoxType.LocalString:
							{
								// LocalString to Number: parse string as double
								// Use efficient char-based parsing to avoid string allocation
								Span<char> chars = stackalloc char[16];
								int charCount = invalue.GetLocalStringChars(chars);
								if (charCount > 0)
								{
									double v = ReadDoubleFromString(new string(chars.Slice(0, charCount)));
									outvalue.SetNumber(v);
								}
								else
								{
									outvalue.SetNumber(double.NaN);
								}
								return;
							}
						case NaNBoxing.BoxType.HeapPtr:
							{
								if (invalue.HeapKind == (byte)RtHeapTypeKind.STRING)
								{
									var instance = Context.GC.Heap[invalue.HeapPtr];
									var str = ((RtString)instance).Str;
									double v = ReadDoubleFromString(str);
									outvalue.SetNumber(v);
								}
								else if (invalue.HeapKind == (byte)RtHeapTypeKind.INSTANCE)
								{
									if (scope_ptr == 0)
									{
										outvalue.SetNumber(double.NaN);
									}
									else
									{
										var instance = Context.GC.Heap[invalue.HeapPtr];
										to_invoke = instance;
										hint = HINT.h_number;
										goto lbl_toprimitive;
									}
								}
								else
								{
									outvalue.SetNumber(double.NaN);
								}
								return;
							}
						case NaNBoxing.BoxType.Fault:
						default:
#if DEBUG
							throw new InvalidOperationException();
#else
							Environment.FailFast("出错了，这里跑不到"); return;
#endif
					}
				case TypeKind.Fun_Void:
				case TypeKind.TraitDataReference:
				case TypeKind.RTQName_MultiName_DataReference:
				case TypeKind.CParseNS_Traits:
				case TypeKind.RTQNameRTQNameL_N:
				case TypeKind.SearchNameSpaceFromImports:
				case TypeKind.Unknown:
				case TypeKind.Null:
					//不可能发生
#if DEBUG
					throw new InvalidOperationException();
#else
					Environment.FailFast("出错了，这里跑不到"); return;
#endif
				case TypeKind.Object:

					switch (invalue.ValueType)
					{
						case NaNBoxing.BoxType.Number:
							outvalue = invalue;
							return;
						case NaNBoxing.BoxType.Undefined:
							outvalue.SetNull();
							return;
						case NaNBoxing.BoxType.Null:
							outvalue.SetNull();
							return;
						case NaNBoxing.BoxType.Boolean:
						case NaNBoxing.BoxType.Int:
						case NaNBoxing.BoxType.Uint:
						case NaNBoxing.BoxType.Sbyte:
						case NaNBoxing.BoxType.Byte:
						case NaNBoxing.BoxType.Short:
						case NaNBoxing.BoxType.UShort:
						case NaNBoxing.BoxType.Float:
						case NaNBoxing.BoxType.LocalString:
						case NaNBoxing.BoxType.HeapPtr:
							outvalue = invalue;
							return;
						case NaNBoxing.BoxType.Fault:
						default:
#if DEBUG
							throw new InvalidOperationException();
#else
							Environment.FailFast("出错了，这里跑不到"); return;
#endif
					}
				case TypeKind.String:
					switch (invalue.ValueType)
					{
						case NaNBoxing.BoxType.Number:
							{
								if (double.IsNaN(invalue.Number))
								{
									outvalue.SetHeapPtr(NAN_STR, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
									return;
								}
								else if (double.IsPositiveInfinity(invalue.Number))
								{
									outvalue.SetHeapPtr(POSITIVEINF_STR, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
									return;
								}
								else if (double.IsNegativeInfinity(invalue.Number))
								{
									outvalue.SetHeapPtr(NEGATIVEINF_STR, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
									return;
								}
								else
								{
									Span<char> buffer1 = stackalloc char[128];

									var str = buildin.Numeric.ToNumberString(invalue.Number, buffer1); //invalue.Number.ToString();

									// 使用辅助函数优化字符串创建
									if (!TryCreateStringValue(str, out outvalue, ref error))
									{
										return; // 错误已经在TryCreateStringValue中处理
									}
									return;
								}
							}
						case NaNBoxing.BoxType.Undefined:
							if (isRetry)
							{
								outvalue.SetHeapPtr(TYPEOF_undefined_STR, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
							}
							else
							{
								outvalue.SetNull();
							}
							return;
						case NaNBoxing.BoxType.Null:
							outvalue.SetNull();
							return;
						case NaNBoxing.BoxType.Boolean:
							if (invalue.Boolean)
							{
								outvalue.SetHeapPtr(TRUE_STR, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
							}
							else
							{
								outvalue.SetHeapPtr(FALSE_STR, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
							}
							return;
						case NaNBoxing.BoxType.Int:
							{
								Span<char> _buffer = stackalloc char[128];
								bool success = invalue.IntValue.TryFormat(_buffer, out int l);
								ReadOnlySpan<char> str = _buffer.Slice(0, l);

								//string str = invalue.IntValue.ToString();
								// 使用辅助函数优化字符串创建
								if (!TryCreateStringValue(str, out outvalue, ref error))
								{
									return; // 错误已经在TryCreateStringValue中处理
								}
								return;
							}
						case NaNBoxing.BoxType.Uint:
							{
								Span<char> _buffer = stackalloc char[128];
								bool success = invalue.UIntValue.TryFormat(_buffer, out int l);
								ReadOnlySpan<char> str = _buffer.Slice(0, l);

								//string str = invalue.UIntValue.ToString();
								// 使用辅助函数优化字符串创建
								if (!TryCreateStringValue(str, out outvalue, ref error))
								{
									return; // 错误已经在TryCreateStringValue中处理
								}
								return;
							}
						case NaNBoxing.BoxType.Sbyte:
							{
								Span<char> _buffer = stackalloc char[128];
								bool success = invalue.SByteValue.TryFormat(_buffer, out int l);
								ReadOnlySpan<char> str = _buffer.Slice(0, l);

								//string str = invalue.SByteValue.ToString();
								// 使用辅助函数优化字符串创建
								if (!TryCreateStringValue(str, out outvalue, ref error))
								{
									return; // 错误已经在TryCreateStringValue中处理
								}
								return;
							}
						case NaNBoxing.BoxType.Byte:
							{
								Span<char> _buffer = stackalloc char[128];
								bool success = invalue.ByteValue.TryFormat(_buffer, out int l);
								ReadOnlySpan<char> str = _buffer.Slice(0, l);

								//string str = invalue.ByteValue.ToString();
								// 使用辅助函数优化字符串创建
								if (!TryCreateStringValue(str, out outvalue, ref error))
								{
									return; // 错误已经在TryCreateStringValue中处理
								}
								return;
							}
						case NaNBoxing.BoxType.Short:
							{
								Span<char> _buffer = stackalloc char[128];
								bool success = invalue.ShortValue.TryFormat(_buffer, out int l);
								ReadOnlySpan<char> str = _buffer.Slice(0, l);

								//string str = invalue.ShortValue.ToString();
								// 使用辅助函数优化字符串创建
								if (!TryCreateStringValue(str, out outvalue, ref error))
								{
									return; // 错误已经在TryCreateStringValue中处理
								}
								return;
							}
						case NaNBoxing.BoxType.UShort:
							{
								Span<char> _buffer = stackalloc char[128];
								bool success = invalue.UShortValue.TryFormat(_buffer, out int l);
								ReadOnlySpan<char> str = _buffer.Slice(0, l);

								//string str = invalue.UShortValue.ToString();
								// 使用辅助函数优化字符串创建
								if (!TryCreateStringValue(str, out outvalue, ref error))
								{
									return; // 错误已经在TryCreateStringValue中处理
								}
								return;
							}
						case NaNBoxing.BoxType.Float:
							{
								if (float.IsNaN(invalue.FloatValue))
								{
									outvalue.SetHeapPtr(NAN_STR, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
									return;
								}
								else if (float.IsPositiveInfinity(invalue.FloatValue))
								{
									outvalue.SetHeapPtr(POSITIVEINF_STR, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
									return;
								}
								else if (float.IsNegativeInfinity(invalue.FloatValue))
								{
									outvalue.SetHeapPtr(NEGATIVEINF_STR, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
									return;
								}
								else
								{
									Span<char> _buffer = stackalloc char[128];
									bool success = invalue.FloatValue.TryFormat(_buffer, out int l);
									ReadOnlySpan<char> str = _buffer.Slice(0, l);

									//string str = invalue.FloatValue.ToString();
									// 使用辅助函数优化字符串创建
									if (!TryCreateStringValue(str, out outvalue, ref error))
									{
										return; // 错误已经在TryCreateStringValue中处理
									}
									return;
								}
							}
						case NaNBoxing.BoxType.LocalString:
							// LocalString is already a string, just return it
							outvalue = invalue;
							return;
						case NaNBoxing.BoxType.HeapPtr:
							{
								switch ((RtHeapTypeKind)invalue.HeapKind)
								{
									case RtHeapTypeKind.CLASS:
										{
											var instance = Context.GC.Heap[invalue.HeapPtr];
											int ptr = Context.GC.AllocString($"[class {((RtScriptClass)instance).Meta.QName.Name}]");
											if (ptr == 0)
											{
												RaiseOutOfMemory(ref error);
												return;
											}
											else
											{
												outvalue.SetHeapPtr(ptr, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
												return;
											}
										}
									case RtHeapTypeKind.GLOBAL:
										{
											int ptr = Context.GC.AllocString("[object global]");
											if (ptr == 0)
											{
												RaiseOutOfMemory(ref error);
												return;
											}
											else
											{
												outvalue.SetHeapPtr(ptr, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
												return;
											}
										}
									case RtHeapTypeKind.STRING:
										outvalue = invalue;
										return;
									case RtHeapTypeKind.INSTANCE:
										{
											var instance = Context.GC.Heap[invalue.HeapPtr];
											if (scope_ptr == 0)
											{
												if (Extensions.IsExtend((ASInstance)instance.Type, Context.ERROR.Instance))
												{
													Span<char> buffers = stackalloc char[128];
													var msg = ((RtInstance)instance).ReadSlot(0, this);
													int ptr = Context.GC.AllocString($"{instance.Type.QName.Name}: {Extensions.GetPrimitiveValueToString(this, msg, buffers)}");
													if (ptr == 0)
													{
														RaiseOutOfMemory(ref error);
														return;
													}
													else
													{
														outvalue.SetHeapPtr(ptr, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
														return;
													}

												}
												else
												{
													int ptr = Context.GC.AllocString($"[object {instance.Type.QName.Name}]");
													if (ptr == 0)
													{
														RaiseOutOfMemory(ref error);
														return;
													}
													else
													{
														outvalue.SetHeapPtr(ptr, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
														return;
													}
												}
											}
											else
											{
												to_invoke = instance;
												hint = HINT.h_string;
												goto lbl_toprimitive;
											}
										}
									case RtHeapTypeKind.NAMESPACE:
										{
											var instance = Context.GC.Heap[invalue.HeapPtr];
											ASNamespace ns = ((RtNameSpace)instance).ASNamespace;
											int ptr = Context.GC.AllocString(string.IsNullOrEmpty(ns.def_uri) ? ns.Name : ns.def_uri);
											if (ptr == 0)
											{
												RaiseOutOfMemory(ref error);
												return;
											}
											else
											{
												outvalue.SetHeapPtr(ptr, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
												return;
											}
										}
									case RtHeapTypeKind.ARRAY:
										{

											if (scope_ptr == 0)
											{
												int ptr = Context.GC.AllocString("[object Array]");
												if (ptr == 0)
												{
													RaiseOutOfMemory(ref error);
													return;
												}
												else
												{
													outvalue.SetHeapPtr(ptr, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
													return;
												}
											}
											else
											{
												var instance = Context.GC.Heap[invalue.HeapPtr];
												to_invoke = instance;
												hint = HINT.h_string;
												goto lbl_toprimitive;
											}
										}
									case RtHeapTypeKind.VECTOR:
										{
											var instance = Context.GC.Heap[invalue.HeapPtr];
											if (scope_ptr == 0)
											{
												int ptr = Context.GC.AllocString($"[object Vector.<{(((RtVector)instance).element_asclass == null ? "*" : ((RtVector)instance).element_asclass.QName.ToDebugTypeName())}>]");
												if (ptr == 0)
												{
													RaiseOutOfMemory(ref error);
													return;
												}
												else
												{
													outvalue.SetHeapPtr(ptr, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
													return;
												}
											}
											else
											{
												to_invoke = instance;
												hint = HINT.h_string;
												goto lbl_toprimitive;
											}
										}
									case RtHeapTypeKind.CLOSURE:
										{
											if (scope_ptr == 0)
											{
												outvalue.SetHeapPtr(is_from_objtostring ? OBJECT_FUNCTION_STR : FUNCTION_TOSTRING_STR, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
												return;
											}
											else
											{
												var instance = Context.GC.Heap[invalue.HeapPtr];
												to_invoke = instance;
												hint = HINT.h_string;
												goto lbl_toprimitive;
											}
										}
									//case RtHeapTypeKind.CACHE_LD_CLASS:
									case RtHeapTypeKind.STACK_CACHE_OBJ:
									case RtHeapTypeKind.DYNAMIC_PROPERTYS:
									case RtHeapTypeKind.SHAPE:
									case RtHeapTypeKind.MethodScope:
									default:
#if DEBUG
										throw new InvalidOperationException();
#else
										Environment.FailFast("出错了，这里跑不到"); return;
#endif
								}
							}
						case NaNBoxing.BoxType.Fault:
						default:
#if DEBUG
							throw new InvalidOperationException();
#else
							Environment.FailFast("出错了，这里跑不到"); return;
#endif
					}


				case TypeKind.Function:
				case TypeKind.Class:
					switch (invalue.ValueType)
					{
						case NaNBoxing.BoxType.Number:

							RaiseTypeError(ref error, invalue, totype);
							return;
						case NaNBoxing.BoxType.Undefined:
							outvalue.SetNull();
							return;
						case NaNBoxing.BoxType.Null:
							outvalue.SetNull();
							return;
						case NaNBoxing.BoxType.Boolean:
						case NaNBoxing.BoxType.Int:
						case NaNBoxing.BoxType.Uint:
						case NaNBoxing.BoxType.Sbyte:
						case NaNBoxing.BoxType.Byte:
						case NaNBoxing.BoxType.Short:
						case NaNBoxing.BoxType.UShort:
						case NaNBoxing.BoxType.Float:
						case NaNBoxing.BoxType.LocalString:

							RaiseTypeError(ref error, invalue, totype);
							return;
						case NaNBoxing.BoxType.HeapPtr:
							{
								if (totype == TypeKind.Function)
								{

									if (invalue.HeapKind == (byte)RtHeapTypeKind.CLOSURE)
									{
										outvalue = invalue;
										return;
									}
									else
									{
										RaiseTypeError(ref error, invalue, totype);
										return;
									}
								}
								else
								{

									if (invalue.HeapKind == (byte)RtHeapTypeKind.CLASS)
									{
										outvalue = invalue;
										return;
									}
									else
									{
										RaiseTypeError(ref error, invalue, totype);
										return;
									}
								}

							}
						case NaNBoxing.BoxType.Fault:
						default:
#if DEBUG
							throw new InvalidOperationException();
#else
							Environment.FailFast("出错了，这里跑不到"); return;
#endif
					}
				case TypeKind.Array:
					switch (invalue.ValueType)
					{
						case NaNBoxing.BoxType.Number:

							RaiseTypeError(ref error, invalue, totype);
							return;
						case NaNBoxing.BoxType.Undefined:
							outvalue.SetNull();
							return;
						case NaNBoxing.BoxType.Null:
							outvalue.SetNull();
							return;
						case NaNBoxing.BoxType.Boolean:
						case NaNBoxing.BoxType.Int:
						case NaNBoxing.BoxType.Uint:
						case NaNBoxing.BoxType.Sbyte:
						case NaNBoxing.BoxType.Byte:
						case NaNBoxing.BoxType.Short:
						case NaNBoxing.BoxType.UShort:
						case NaNBoxing.BoxType.Float:
						case NaNBoxing.BoxType.LocalString:

							RaiseTypeError(ref error, invalue, totype);
							return;
						case NaNBoxing.BoxType.HeapPtr:
							{

								if (invalue.HeapKind == (byte)RtHeapTypeKind.ARRAY)
								{
									outvalue = invalue;
									return;
								}
								else
								{
									RaiseTypeError(ref error, invalue, totype);
									return;
								}
							}

						case NaNBoxing.BoxType.Fault:
						default:
#if DEBUG
							throw new InvalidOperationException();
#else
							Environment.FailFast("出错了，这里跑不到"); return;
#endif
					}


				case TypeKind.Vector:
				case TypeKind.Namespace:
					switch (invalue.ValueType)
					{
						case NaNBoxing.BoxType.Number:

							RaiseTypeError(ref error, invalue, totype);
							return;
						case NaNBoxing.BoxType.Undefined:
							outvalue.SetNull();
							return;
						case NaNBoxing.BoxType.Null:
							outvalue.SetNull();
							return;
						case NaNBoxing.BoxType.Boolean:
						case NaNBoxing.BoxType.Int:
						case NaNBoxing.BoxType.Uint:
						case NaNBoxing.BoxType.Sbyte:
						case NaNBoxing.BoxType.Byte:
						case NaNBoxing.BoxType.Short:
						case NaNBoxing.BoxType.UShort:
						case NaNBoxing.BoxType.Float:
						case NaNBoxing.BoxType.LocalString:

							RaiseTypeError(ref error, invalue, totype);
							return;
						case NaNBoxing.BoxType.HeapPtr:
							{

								if (totype == TypeKind.Namespace)
								{
									if (invalue.HeapKind == (byte)RtHeapTypeKind.NAMESPACE)
									{
										outvalue = invalue;
										return;
									}
									else
									{
										RaiseTypeError(ref error, invalue, totype);
										return;
									}
								}
								else
								{
									if (invalue.HeapKind == (byte)RtHeapTypeKind.VECTOR)
									{
										RtHeapBase obj = Context.GC.Heap[invalue.HeapPtr];

										if (
											((ASInstance)obj.Type).indexer_get.ReturnTypeKind == totype_class.Instance.indexer_get.ReturnTypeKind
											//(ulong)((RtPayloadVector)obj).element_type == totype_class.Instance._element_class.Type_identifier
											)
										{
											outvalue = invalue;
											return;
										}
										else
										{
											RaiseTypeError(ref error, invalue, (TypeKind)totype_class.Type_identifier);
											return;
										}
									}
									else
									{
										RaiseTypeError(ref error, invalue, (TypeKind)totype_class.Type_identifier);
										return;

										//throw new NotImplementedException();
									}
								}

							}

						case NaNBoxing.BoxType.Fault:
						default:
#if DEBUG
							throw new InvalidOperationException();
#else
							Environment.FailFast("出错了，这里跑不到"); return;
#endif
					}



				default:
					{
						if (invalue.ValueType == NaNBoxing.BoxType.Null || invalue.ValueType == NaNBoxing.BoxType.Undefined)
						{
							outvalue.SetNull();
							return;
						}
						else if (invalue.ValueType == NaNBoxing.BoxType.HeapPtr)
						{

							if (invalue.HeapKind == (byte)RtHeapTypeKind.INSTANCE) //只有对象实例才可能满足条件。
							{
								var obj = Context.GC.Heap[invalue.HeapPtr];
								ASClass valuetype = ((ASInstance)obj.Type)._link_codescope.TypeLayout.ASType;
								if (valuetype.Type_identifier == (ulong)totype)
								{
									outvalue = invalue;
									return;
								}

								ASClass @class = totype_class; //Context.dictTypes[(ulong)totype];
								if (valuetype.Instance.IsExtend(@class.Instance))
								{
									outvalue = invalue;
									return;
								}
								if (valuetype.Instance.IsImplements(@class.Instance))
								{
									outvalue = invalue;
									return;
								}
							}
							else if (invalue.HeapKind == (byte)RtHeapTypeKind.VECTOR)
							{
								var obj = Context.GC.Heap[invalue.HeapPtr];
								ASInstance valuetype = ((ASInstance)obj.Type);
								if (totype_class.Instance == valuetype)
								{
									outvalue = invalue;
									return;
								}

							}

							RaiseTypeError(ref error, invalue, totype);
							return;

						}
						else
						{

							RaiseTypeError(ref error, invalue, totype);
							return;
						}
					}


			}

		lbl_toprimitive:

			{
				outvalue = invalue;
				if (Context.StackPosition + 1 >= Context.STACK_LENGTH)
				{
					RaiseStackOverflow(ref error);
					return;
				}
				var stackslots = Context.StackSlots.AsSpan(Context.StackPosition, 2); stackslots.Clear();
				var stPos = Context.StackPosition;
				Context.StackPosition += 2;

				outvalue = ToPrimitive(ref error, invalue, hint, scope_ptr, new StackLocater() { index = 0 }, new StackLocater() { index = 1 }, stackslots, stPos, callee_bindthis);

				Context.StackPosition -= 2;
				scope_ptr = 0;

				if (error.raised)
				{
					return;
				}

				invalue = outvalue;
				isRetry = true;
				goto lbl_retry;

				//var ns_set = Context.GC.Heap[scope_ptr].Type._link_codescope.NamespaceSet;
				//ASContainer as_type = to_invoke.Type;
				//int code = MultiNameLSearch(ns_set, to_invoke.TypeKind,
				//	as_type, "toString", new StackLocater() { index = 0 }, stackslots, stPos, invalue, callee_bindthis, ref error, true);
				//switch (code)
				//{
				//	case 0:
				//		break;
				//	case 1:
				//		//有异常产生
				//		Context.StackPosition -= 2;
				//		return;
				//	case 2:
				//		Context.StackPosition -= 2;
				//		Context.GC.CheckGC(ref error);
				//		RaiseTypeError_Ambiguous(ref error, "toString");
				//		return;
				//	default:
				//		throw new InvalidOperationException();
				//}
				//NaNBoxing fun = LoadValue(stackslots[0], -1, ref error, stackslots, stPos);
				//if (error.raised) //由于object原型的存在，这里是肯定能找到的。找不到就报错吧
				//{
				//	Context.StackPosition -= 2;
				//	return;
				//}
				//if (fun.ValueType != NaNBoxing.BoxType.HeapPtr)
				//{
				//	Context.StackPosition -= 2;
				//	RaiseTypeError(ref error, fun, TypeKind.Function);
				//	return;
				//}
				//var funinstance = Context.GC.Heap[fun.HeapPtr];
				//if (funinstance.TypeKind != RtHeapTypeKind.CLOSURE)
				//{
				//	Context.StackPosition -= 2;
				//	RaiseTypeError(ref error, fun, TypeKind.Function);
				//	return;
				//}
				//if (((ASMethodBody)funinstance.Type).Method.Container == Context.OBJECT._link_codescope.Parent.Container)
				//{
				//	Context.StackPosition -= 2;
				//	scope_ptr = 0;
				//	goto lbl_retry;
				//}
				//else
				//{
				//	//invoke_it
				//	unsafe
				//	{
				//		invalue = RunMethod(((ASMethodBody)funinstance.Type).Method,
				//			invalue, ((RtPayloadClosure)funinstance).ScopePtr, ((RtPayloadClosure)funinstance).ScopeType, 0, null, null, ref error, stPos + 1, fun.HeapPtr);
				//		Context.StackPosition -= 2;
				//		if (error.raised)
				//		{
				//			return;
				//		}

				//		scope_ptr = 0;
				//		//再次trace
				//		goto lbl_retry;

				//	}


				//}
			}
			;
		}

		/// <summary>
		/// 由于可能出现转换操作中内部有新增对象的情况（如 Number->String）
		/// 在保存返回值前不能触发GC避免被异常回收
		/// </summary>
		/// <param name="error"></param>
		/// <param name="value"></param>
		/// <param name="totype"></param>
		/// <param name="totype_class"></param>
		/// <param name="outvalue"></param>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		/// <exception cref="NotImplementedException"></exception>
		[MethodImpl( MethodImplOptions.AggressiveInlining)]
		public void ConvertValueType(ref ReceiveError error, NaNBoxing invalue, TypeKind totype, ASClass @totype_class, ref NaNBoxing outvalue, int scope_ptr = 0, NaNBoxing callee_bindthis = default, bool is_from_objtostring = false)
		{
			BoxType intype = invalue.ValueType;
			if (totype == TypeKind.Any
				|| ((byte)intype - 3 == (byte)totype && totype <= TypeKind.Float)
				|| ((intype == BoxType.LocalString || invalue.HeapKind == (byte)RtHeapTypeKind.STRING ) && totype == TypeKind.String)
				|| (invalue.HeapKind == (byte)RtHeapTypeKind.ARRAY  && totype == TypeKind.Array)
				|| (intype == BoxType.Number && totype == TypeKind.Number)
				)
			{
				outvalue = invalue;
				return;
			}
			else if (totype < TypeKind.Float && totype >= TypeKind.Int && intype >= BoxType.Int && intype < BoxType.Float)
			{
				ulong mask = (((ulong)totype + 3) << 40) | NaNBoxing.QNAN;
				ulong raw = mask | (invalue.Raw & 0xffffffff);

				outvalue = new NaNBoxing(raw);
				return;

			}
			//else if ((totype == TypeKind.Int) && (byte)(invalue.ValueType - 1) < (byte)BoxType.UShort)
			//{
			//	outvalue.SetInt(invalue.IntValue);
			//	return;
			//}
			//else if (totype == TypeKind.Uint && (byte)(invalue.ValueType - 1) < (byte)BoxType.UShort)
			//{
			//	outvalue.SetUInt(invalue.UIntValue);
			//	return;
			//}
			else if (totype == TypeKind.Number && (byte)(intype - 2) < (byte)BoxType.UShort-1)
			{
				outvalue.SetNumber(((byte)(intype) % 2 == 1) ? invalue.IntValue : invalue.UIntValue);
				return;
			}
			else if (totype == TypeKind.Float && (byte)(intype - 2) < (byte)BoxType.UShort-1)
			{
				outvalue.SetFloat(((byte)(intype) % 2 == 1) ? invalue.IntValue : invalue.UIntValue);
				return;
			}
			else if (
				totype_class != null && (totype >= TypeKind.Object) &&
				(
				intype == BoxType.Null ||				
				(intype == BoxType.HeapPtr && (totype == TypeKind.Object || Context.GC.Heap[invalue.HeapPtr].Type == totype_class.Instance))
				
				))
			{
				outvalue = invalue;
				return;
			}
			else
			{
				ConvertValueSlow(ref error, invalue, totype, totype_class, ref outvalue, scope_ptr, callee_bindthis, is_from_objtostring);
			}

		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private unsafe void ExplicitConvert(ref ReceiveError error, ushort argsCount, StackLocater* arguments, Span<NaNBoxing> slots, TypeKind totype, ASClass @totype_class, ref NaNBoxing outvalue, int returnSlotindex,
			int scope_ptr = 0, NaNBoxing callee_bindthis = default, bool is_from_objtostring = false)
		{

			if (totype == TypeKind.Array)
			{
				if (Context.StackPosition + 1 >= Context.STACK_LENGTH)
				{
					RaiseStackOverflow(ref error);
					return;
				}

				int ptrIndex = returnSlotindex;
				int instancePtr = Context.CacheArrayPtr + ptrIndex;
				var instance = Context.GC.Heap[instancePtr];
				instance.Type = Context.ARRAY.Instance;

				//((RtArray)instance).array_len = 0;
				//((RtArray)instance).methodscopeslot_ref_state = 0;
				//((RtArray)instance).HEAPINSTANCE_PTR = 0;
				((RtArray)instance).SetStoreCacheZero(true);


				NaNBoxing invalue = default;
				invalue.SetHeapPtr(instancePtr, (byte)RtHeapTypeKind.ARRAY, (byte)HeapKindFlag.NONE);

				Context.StackPosition += 1;
				unsafe
				{
					//构造
					RunMethod(totype_class.Instance.Constructor, invalue, instancePtr,  argsCount, (byte*)arguments, slots, ref error,
						Context.StackPosition - 1);
				}
				Context.StackPosition -= 1;
				if (error.raised)
				{
					return;
				}
				outvalue.SetHeapPtr(instancePtr, (byte)RtHeapTypeKind.ARRAY, (byte)HeapKindFlag.NONE);
				return;
			}
			else if (
				argsCount < 2
				&&
				Extensions.IsExtend(totype_class.Instance, Context.ERROR.Instance))
			{
				if (Context.StackPosition + 1 >= Context.STACK_LENGTH)
				{
					RaiseStackOverflow(ref error);
					return;
				}

				//Span<NaNBoxing> slots = Context.StackSlots.AsSpan(Context.StackPosition, 2);

				Context.StackPosition += 1;

				int errPtr;

				if (totype_class.Instance.Flags.HasFlag(ClassFlags.CacheAble))
				{
					errPtr = InitCacheInstance(totype_class, returnSlotindex, true,out RtInstance _temp);
				}
				else
				{
					RtHeapBase _temp;
					errPtr = Context.GC.AllocInstance(totype_class.Instance, out _temp);
					if (errPtr == 0)
					{
						RaiseOutOfMemory(ref error);
						return;
					}
				}

				NaNBoxing invalue = default;
				invalue.SetHeapPtr(errPtr, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.NONE);

				//slots[0].SetHeapPtr(errPtr);
				//slots[1] = invalue;
				unsafe
				{
					//构造
					//StackLocater send_arg = new StackLocater() { index = 1 };
					RunMethod(totype_class.Instance.Constructor, invalue, errPtr, argsCount, (byte*)arguments, slots, ref error, Context.StackPosition - 1);
				}
				Context.StackPosition -= 1;
				if (error.raised)
				{
					return;
				}

				outvalue.SetHeapPtr(errPtr, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.NONE);


				return;
				//throw new NotImplementedException();
			}
			else if (argsCount == 1)
			{
				var invalue = slots[arguments->index];
				bool istovector = totype_class.Instance.Flags.HasFlag(ClassFlags.Vector);
				if (istovector && invalue.ValueType == BoxType.HeapPtr && invalue.HeapKind == (byte)RtHeapTypeKind.ARRAY)
				{
					//转化为Vector
					int ptrIndex = returnSlotindex;

					int instancePtr = Context.CacheVectorPtr + ptrIndex;
					var instance = Context.GC.Heap[instancePtr];

					//instance.Type = totype_class.Instance;
					//((RtVector)instance).HEAPINSTANCE_PTR = 0;
					//((RtVector)instance).element_asclass = totype_class.Instance._element_class;
					//((RtVector)instance).element_type = totype_class.Instance._element_class == null ? TypeKind.Any : (TypeKind)totype_class.Instance._element_class.Type_identifier;

					//((RtVector)instance).GetStore().length = 0;
					//((RtVector)instance).GetStore().elementSize = VectorImpl.VectorStore.GetElementSize(((RtVector)instance).element_type, ((RtVector)instance).element_asclass) ;
					((RtVector)instance).SetStoreCacheZero(totype_class.Instance);

					Context.StackSlots[returnSlotindex].SetHeapPtr(instancePtr, (byte)RtHeapTypeKind.VECTOR, (byte)HeapKindFlag.NONE);

					RtArray srcArr;
					RtArray.FindAndUpdateHeapInstancePtr(invalue.HeapPtr, this, out srcArr);
					Debug.Assert(srcArr.HEAPINSTANCE_PTR == 0);
					uint len = srcArr.array_len; //GetLength(this);

					unsafe
					{
						Span<NaNBoxing> _tmpslot = stackalloc NaNBoxing[1];
						_tmpslot[0].SetInt((int)len);

						StackLocater l; l.index = 0;
						//构造
						//StackLocater send_arg = new StackLocater() { index = 1 };
						RunMethod(totype_class.Instance.Constructor, Context.StackSlots[returnSlotindex], instancePtr,
							 1, (byte*)&l, _tmpslot, ref error, returnSlotindex);

						if (error.raised)
						{
							return;
						}
					}

					RtVector dst;
					int vptr = RtVector.FindAndUpdateHeapInstancePtr(instancePtr, this, out dst);
					for (int i = 0; i < (int)len; i++)
					{
						bool isoutindex;
						NaNBoxing e = srcArr.ReadSlot((uint)i, this, out isoutindex);

						ConvertValueType(ref error, e, dst.element_type, dst.element_asclass, ref e);

#if DEBUG
						NaNBoxing index = default; index.SetInt(i);
						int _i;
						Debug.Assert(dst.GetStore(this).IsValidIndexRange(index, out _i));
#endif
						dst.SetSlot(i, this, vptr, e, ref error);
						if (error.raised)
						{
							return;
						}
					}

					outvalue.SetHeapPtr(vptr, (byte)RtHeapTypeKind.VECTOR, (byte)HeapKindFlag.NONE);
				}
				else if (totype == TypeKind.String && invalue.ValueType == BoxType.Undefined)
				{
					outvalue.SetHeapPtr(TYPEOF_undefined_STR, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				}
				else if (totype == TypeKind.String && invalue.ValueType == BoxType.Null)
				{
					outvalue.SetHeapPtr(NULL_STR, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
				}
				else
				{
					ConvertValueType(ref error, invalue, totype, @totype_class, ref outvalue, scope_ptr, callee_bindthis);
				}
			}
			else
			{
				RaiseArgumentErrorCountMisMatch(ref error, null, 1, argsCount);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public bool ToBoolean(NaNBoxing invalue)
		{

			switch (invalue.ValueType)
			{
				case NaNBoxing.BoxType.Number:
					return (!(invalue.Number == 0 || double.IsNaN(invalue.Number)));

				case NaNBoxing.BoxType.Undefined:
					return (false);

				case NaNBoxing.BoxType.Null:
					return (false);

				case NaNBoxing.BoxType.Boolean:
					return invalue.Boolean;

				case NaNBoxing.BoxType.Int:
					return (invalue.IntValue != 0);

				case NaNBoxing.BoxType.Uint:
					return (invalue.UIntValue != 0);

				case NaNBoxing.BoxType.Sbyte:
					return (invalue.SByteValue != 0);

				case NaNBoxing.BoxType.Byte:
					return (invalue.ByteValue != 0);

				case NaNBoxing.BoxType.Short:
					return (invalue.ShortValue != 0);

				case NaNBoxing.BoxType.UShort:
					return (invalue.UShortValue != 0);

				case NaNBoxing.BoxType.Float:
					return (!(invalue.FloatValue == 0 || float.IsNaN(invalue.FloatValue)));

				case NaNBoxing.BoxType.HeapPtr:
					{

						Debug.Assert((Context.GC.Heap[invalue.HeapPtr].Kind != RtHeapTypeKind.STACK_CACHE_OBJ));

						if (invalue.HeapKind == (byte)RtHeapTypeKind.STRING
							&&
							string.IsNullOrEmpty(((RtString)Context.GC.Heap[invalue.HeapPtr]).Str)
							)
						{
							return (false);
						}
						else
						{
							return (true);
						}

					}
				case NaNBoxing.BoxType.Fault:
				default:
#if DEBUG
					throw new InvalidOperationException();
#else
					Environment.FailFast("出错了，这里跑不到"); return default;
#endif
			}

		}


		internal bool check_MultiNameLSearch_issameorinherit(NaNBoxing instance, RtHeapBase this_ins)
		{
			bool issameorinherit = this_ins != null && instance.ValueType == BoxType.HeapPtr &&

				(byte)this_ins.Kind == instance.HeapKind
				&&
				this_ins.Kind == RtHeapTypeKind.INSTANCE
				&&
				((ASInstance)Context.GC.Heap[instance.HeapPtr].Type).IsExtend((ASInstance)this_ins.Type)

				;

			if (instance.ValueType == BoxType.HeapPtr)
			{
				if (instance.HeapKind == (byte)RtHeapTypeKind.CLASS) // Class['aa'] 这种，可以访问protected属性
				{
					if (this_ins != null && this_ins.Kind == RtHeapTypeKind.INSTANCE)
					{
						if (
							((ASInstance)this_ins.Type).IsExtend(
							((ASClass)((RtScriptClass)Context.GC.Heap[instance.HeapPtr]).Meta).Instance)
							)
						{
							issameorinherit = true;
						}
					}
				}
			}
			return issameorinherit;
		}


		private int multinamelsearch_findmembers(CodeScope scope, ReadOnlySpan<char> name, out int index, ASNamespaceSet ns_set, bool issameorinherit, bool exclude_user_ns)
		{
			index = -1;
			int count = 0;
			ASContainer defat = null;
			for (int i = scope.Members.Count - 1; i >= 0; i--)
			{
				var member = scope.Members[i];
				if (name.CompareTo(member.QName.Name, StringComparison.Ordinal) == 0
				   && (
						(
							member.DefineAt.QName.Namespace == ns_set.Namespaces[0] && member.QName.Namespace.Kind == NamespaceKind.PackageInternal && member.QName.Namespace.def_uri == null
						)
						||
						ns_set.Namespaces.Contains(member.QName.Namespace)
					)

				   &&
				   (issameorinherit || (member.QName.Namespace.Kind != NamespaceKind.Protected && member.QName.Namespace.Kind != NamespaceKind.StaticProtected))
				   &&
				   (
					!exclude_user_ns ||
					!(member.QName.Namespace.def_uri != null && member.QName.Namespace.Kind == NamespaceKind.PackageInternal)
				   )

				)
				{
					if (defat == null)
					{
						defat = member.DefineAt;
					}
					else if (defat != member.DefineAt)
					{
						break;
					}
					index = i;
					count++;
				}
			}

			return count;
		}


		private void multinamelsearch_findvables(VTable table, ReadOnlySpan<char> name,
			out int m_index, out int g_index, out int s_index, out int m_count, out int g_count, out int s_count, ASNamespaceSet ns_set, bool issameorinherit, bool exclude_user_ns)
		{
			m_index = -1;
			g_index = -1;
			s_index = -1;
			m_count = 0;
			g_count = 0;
			s_count = 0;
			ASContainer defat = null;
			ASContainer defat_get = null;
			ASContainer defat_set = null;
			for (int i = table.Items.Count - 1; i >= 0; i--)
			{
				var item = table.Items[i];

				if (//item.Trait.QName.Name == name 
				name.CompareTo(item.Trait.QName.Name, StringComparison.Ordinal) == 0
				&&
					(
						(
							item.DefineAt.QName.Namespace == ns_set.Namespaces[0] &&
							item.Trait.QName.Namespace.Kind == NamespaceKind.PackageInternal &&
							item.Trait.QName.Namespace.def_uri == null
						)
						||
					ns_set.Namespaces.Contains(item.Trait.QName.Namespace)
					)
				 &&
				   (issameorinherit || (item.Trait.QName.Namespace.Kind != NamespaceKind.Protected && item.Trait.QName.Namespace.Kind != NamespaceKind.StaticProtected))
				 &&
				   (
					!exclude_user_ns ||
					!(item.Trait.QName.Namespace.def_uri != null && item.Trait.QName.Namespace.Kind == NamespaceKind.PackageInternal)
				   )
				)
				{

					if (item.Trait.Kind == TraitKind.Method)
					{
						if (defat == null)
						{
							defat = item.DefineAt;
						}
						else if (defat != item.DefineAt)
						{
							break;
						}

						m_index = i; m_count++;
					}
					else if (item.Trait.Kind == TraitKind.Getter)
					{
						if (defat_get == null)
						{
							defat_get = item.DefineAt;
						}
						else if (defat_get != item.DefineAt)
						{
							break;
						}

						g_index = i; g_count++;
					}
					else if (item.Trait.Kind == TraitKind.Setter)
					{
						if (defat_set == null)
						{
							defat_set = item.DefineAt;
						}
						else if (defat_set != item.DefineAt)
						{
							break;
						}

						s_index = i; s_count++;
					}


				}

			}
		}

		internal int MultiNameLSearch(ASNamespaceSet ns_set, RtHeapTypeKind kind, ASContainer as_type, ReadOnlySpan<char> name, int name_strptr, StackLocater stack,
			Span<NaNBoxing> stackslots, int stackStPos, NaNBoxing instance, bool issameorinherit, ref ReceiveError error, bool exclude_user_ns = false
			)
		{
#if FORCOMPILER
			if (IsComputeConstExpr)
			{
				throw new EvalConstException();
			}
#endif

			if (instance.HeapPtr == Context.CLASS.__instance_index__)
			{
				kind = RtHeapTypeKind.INSTANCE;
				as_type = Context.CLASS.Instance;
			}

			if (kind == RtHeapTypeKind.CLASS)
			{
				CodeScope cls = as_type._link_codescope;
				int i;
				var count = multinamelsearch_findmembers(cls, name, out i, ns_set, issameorinherit, exclude_user_ns);

				//查找虚函数表
				int m_idx, m_count, g_idx, g_count, s_idx, s_count;
				multinamelsearch_findvables(as_type._vtable, name, out m_idx, out g_idx, out s_idx, out m_count, out g_count, out s_count, ns_set, issameorinherit, exclude_user_ns);

				if (count + m_count + g_count > 1 || count + m_count + s_count > 1)
				{
					goto lbl_multiname_ambiguous;
				}
				else if (count == 1)
				{
					var member = cls.Members[i];

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
					cachePayload.RefInstance = instance;
					cachePayload.trait[0] = member.trait; cachePayload.trait[1] = null;
					cachePayload.scopemember_index = (ushort)i;
					cachePayload.searchPropertyName.SetUndefined(); cachePayload.as_type = as_type;
					cachePayload.searchNameSpacePtr = 0; cachePayload.indexer_key.setFault();

					stackslots[stack.index].SetHeapPtr(cacheobjpointer, (byte)RtHeapTypeKind.STACK_CACHE_OBJ, (byte)HeapKindFlag.NONE);

					goto lbl_multiname_success;
				}
				else if (m_count == 1)
				{
#if DEBUG
					if (instance.ValueType != BoxType.HeapPtr)
						throw new InvalidOperationException();
#endif

					var vitem = as_type._vtable.Items[m_idx];

					int ptrIndex = stackStPos + stack.index;
					int m_closurePtr = Context.M_ClosurePtr + ptrIndex;

					Context.GC.Heap[m_closurePtr].Type = vitem.Trait.Method.Body;
					RtClosure closure = (RtClosure)Context.GC.Heap[m_closurePtr];
					closure.This.SetNull();
					closure.ScopePtr = instance.HeapPtr;
					//closure.ScopeType = vitem.DefineAt;
					closure._ref_as_type = as_type;
					closure.methodscopeslot_ref_state = 0; closure.HEAPINSTANCE_PTR = 0;
					stackslots[stack.index].SetHeapPtr(m_closurePtr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);

					goto lbl_multiname_success;

				}
				else if (g_count == 1 || s_count == 1)
				{
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
					cachePayload.RefInstance = instance;
					if (g_idx > -1)
					{
						cachePayload.trait[0] = as_type._vtable.Items[g_idx].Trait;
						cachePayload.g_index = g_idx;
					}
					else
					{
						cachePayload.trait[0] = null;
					}

					if (s_idx > -1)
					{
						cachePayload.trait[1] = as_type._vtable.Items[s_idx].Trait;
						cachePayload.s_index = s_idx;
					}
					else
					{
						cachePayload.trait[1] = null;
					}

					cachePayload.scopemember_index = 0;
					cachePayload.searchPropertyName.SetUndefined(); cachePayload.as_type = as_type;
					cachePayload.searchNameSpacePtr = 0; cachePayload.indexer_key.setFault();

					stackslots[stack.index].SetHeapPtr(cacheobjpointer, (byte)RtHeapTypeKind.STACK_CACHE_OBJ, (byte)HeapKindFlag.NONE);

					goto lbl_multiname_success;

					//throw new NotImplementedException();
				}
				else
				{
					Context.GC.CheckGC(ref error);

					NaNBoxing searchPtr = default;
					//if (string.CompareOrdinal(name, "valueOf") == 0)
					if (name.CompareTo("valueOf", StringComparison.Ordinal) == 0)
					{
						searchPtr.SetHeapPtr(VALUEOF_STR, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
					}
					else if (
						//string.CompareOrdinal(name, "toString") == 0
						name.CompareTo("toString", StringComparison.Ordinal) == 0
						)
					{
						searchPtr.SetHeapPtr(TOSTRING_STR, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
					}
					else
					{
						//未找到，进行动态属性处理
						//searchPtr = Context.GC.AllocString(name);
						if (!TryCreateStringValue(name, out searchPtr, ref error))
						{
							goto flag_handle_error;
						}

					}
					//if (searchPtr == 0)
					//{
					//	RaiseOutOfMemory(ref error);
					//	goto flag_handle_error;
					//}

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
					cachePayload.RefInstance = instance;
					cachePayload.trait[0] = null; cachePayload.trait[1] = null;
					cachePayload.scopemember_index = 0;
					cachePayload.searchPropertyName = searchPtr; cachePayload.as_type = as_type;
					cachePayload.searchNameSpacePtr = 0; cachePayload.indexer_key.setFault();

					stackslots[stack.index].SetHeapPtr(cacheobjpointer, (byte)RtHeapTypeKind.STACK_CACHE_OBJ, (byte)HeapKindFlag.NONE);




					goto lbl_multiname_dynamicprop;
				}
			}
			else if (kind == RtHeapTypeKind.INSTANCE || kind == RtHeapTypeKind.STRING || kind == RtHeapTypeKind.VECTOR || kind == RtHeapTypeKind.ARRAY || kind == RtHeapTypeKind.NAMESPACE || (byte)kind == 255)
			{
				if (as_type == Context.OBJECT.Instance)
				{
					goto lbl_nomember;
				}

				CodeScope type = as_type._link_codescope;
				int i;
				var count = multinamelsearch_findmembers(type, name, out i, ns_set, issameorinherit, exclude_user_ns);

				//查找虚函数表
				int m_idx, m_count, g_idx, g_count, s_idx, s_count;
				multinamelsearch_findvables(as_type._vtable, name, out m_idx, out g_idx, out s_idx, out m_count, out g_count, out s_count, ns_set, issameorinherit, exclude_user_ns);

				//从写代码角度，不可能出现需要搜索基类的情况。

				if (count + m_count + g_count > 1 || count + m_count + s_count > 1)
				{
					goto lbl_multiname_ambiguous;
				}
				else if (count == 1)
				{
					var member = type.Members[i];

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
					cachePayload.RefInstance = instance;
					cachePayload.trait[0] = member.trait; cachePayload.trait[1] = null;
					cachePayload.scopemember_index = (ushort)i;
					cachePayload.searchPropertyName.SetUndefined(); cachePayload.as_type = as_type;
					cachePayload.searchNameSpacePtr = 0; cachePayload.indexer_key.setFault();

					stackslots[stack.index].SetHeapPtr(cacheobjpointer, (byte)RtHeapTypeKind.STACK_CACHE_OBJ, (byte)HeapKindFlag.NONE);
					goto lbl_multiname_success;
				}
				else if (m_count == 1)
				{
					var vitem = as_type._vtable.Items[m_idx];

					int ptrIndex = stackStPos + stack.index;
					int m_closurePtr = Context.M_ClosurePtr + ptrIndex;

					Context.GC.Heap[m_closurePtr].Type = vitem.Trait.Method.Body;
					RtClosure closure = (RtClosure)Context.GC.Heap[m_closurePtr];
					//closure.This.SetHeapPtr(instancePtr);
					//closure.ScopePtr = instancePtr;
					closure.This = instance;
					closure.ScopePtr = instance.ValueType == BoxType.HeapPtr ? instance.HeapPtr : 0;
					//closure.ScopeType = vitem.DefineAt;
					closure._ref_as_type = as_type;
					closure.methodscopeslot_ref_state = 0; closure.HEAPINSTANCE_PTR = 0;
					stackslots[stack.index].SetHeapPtr(m_closurePtr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);

					goto lbl_multiname_success;
				}
				else if (g_count == 1 || s_count == 1)
				{
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
					cachePayload.RefInstance = instance;
					if (g_idx > -1)
					{
						cachePayload.trait[0] = as_type._vtable.Items[g_idx].Trait;
						cachePayload.g_index = g_idx;
					}
					else
					{
						cachePayload.trait[0] = null;
					}

					if (s_idx > -1)
					{
						cachePayload.trait[1] = as_type._vtable.Items[s_idx].Trait;
						cachePayload.s_index = s_idx;
					}
					else
					{
						cachePayload.trait[1] = null;
					}

					cachePayload.scopemember_index = 0;
					cachePayload.searchPropertyName.SetUndefined(); cachePayload.as_type = as_type;
					cachePayload.searchNameSpacePtr = 0; cachePayload.indexer_key.setFault();

					stackslots[stack.index].SetHeapPtr(cacheobjpointer, (byte)RtHeapTypeKind.STACK_CACHE_OBJ, (byte)HeapKindFlag.NONE);

					goto lbl_multiname_success;

				}


			lbl_nomember:
				{


					ASInstance it = (ASInstance)as_type;
					//if ((it.Flags & ClassFlags.Sealed) == ClassFlags.Sealed)
					//{
					//    goto lbl_multiname_notfound;
					//}
					//else

					//if ((byte)kind == 255)
					//{
					//	RaiseError_CanNotCreateProperty(ref error, null, name, as_type.QName);
					//	goto flag_handle_error;
					//}
					//else


					{ //dynamic property
					  //未找到，进行动态属性处理

						Context.GC.CheckGC(ref error);

						NaNBoxing searchPtr = default;
						if (
							//string.CompareOrdinal(name, "valueOf") == 0
							name.CompareTo("valueOf", StringComparison.Ordinal) == 0
							)
						{
							searchPtr.SetHeapPtr(VALUEOF_STR, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
						}
						else if (
							//string.CompareOrdinal(name, "toString") == 0
							name.CompareTo("toString", StringComparison.Ordinal) == 0
							)
						{
							searchPtr.SetHeapPtr(TOSTRING_STR, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
						}
						else
						{
							//未找到，进行动态属性处理
							//searchPtr = Context.GC.AllocString(name);

							if (name_strptr != 0)
							{
								searchPtr.SetHeapPtr(name_strptr, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
							}
							else if (!TryCreateStringValue(name, out searchPtr, ref error))
							{
								goto flag_handle_error;
							}

						}
						//if (searchPtr == 0)
						//{
						//	RaiseOutOfMemory(ref error);
						//	goto flag_handle_error;
						//}



						int ptrIndex = stackStPos + stack.index;
						int cacheobjpointer = Context.CacheObjPtr + ptrIndex;  //Context.CacheObjectPointers[ptrIndex];
						RtHeapBase cache = Context.GC.Heap[cacheobjpointer];
#if DEBUG
						if (cache.Kind != RtHeapTypeKind.STACK_CACHE_OBJ)
						{
							throw new InvalidOperationException();
						}
#endif

						if (it.Flags.HasFlag(ClassFlags.Indexer)
							&&
							(
								!it.Flags.HasFlag(ClassFlags.Vector) //Vector的索引器不会处理字符串
							)
							)
						{
							RtStackCache cachePayload = (RtStackCache)cache;
							cachePayload.RefInstance = instance;
							cachePayload.trait[0] = null; cachePayload.trait[1] = null;
							cachePayload.scopemember_index = 0;
							cachePayload.searchPropertyName.SetUndefined(); cachePayload.as_type = as_type;
							cachePayload.searchNameSpacePtr = 0; cachePayload.indexer_key = searchPtr;

							stackslots[stack.index].SetHeapPtr(cacheobjpointer, (byte)RtHeapTypeKind.STACK_CACHE_OBJ, (byte)HeapKindFlag.NONE);


						}
						else
						{

							RtStackCache cachePayload = (RtStackCache)cache;
							cachePayload.RefInstance = instance;
							cachePayload.trait[0] = null; cachePayload.trait[1] = null;
							cachePayload.scopemember_index = 0;
							cachePayload.searchPropertyName = searchPtr; cachePayload.as_type = as_type;
							cachePayload.searchNameSpacePtr = 0; cachePayload.indexer_key.setFault();

							stackslots[stack.index].SetHeapPtr(cacheobjpointer, (byte)RtHeapTypeKind.STACK_CACHE_OBJ, (byte)HeapKindFlag.NONE);

						}

						goto lbl_multiname_dynamicprop;
					}

				}


			}
			else if (kind == RtHeapTypeKind.GLOBAL)
			{
				CodeScope type = as_type._link_codescope;
				int i;
				var count = multinamelsearch_findmembers(type, name, out i, ns_set, issameorinherit, exclude_user_ns);
				if (count == 0) //dynamic property
				{
					Context.GC.CheckGC(ref error);

					NaNBoxing searchPtr = default;
					if (
						//string.CompareOrdinal(name, "valueOf") == 0
						name.CompareTo("valueOf", StringComparison.Ordinal) == 0
						)
					{
						searchPtr.SetHeapPtr(VALUEOF_STR, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
					}
					else if (
						//string.CompareOrdinal(name, "toString") == 0
						name.CompareTo("toString", StringComparison.Ordinal) == 0
						)
					{
						searchPtr.SetHeapPtr(TOSTRING_STR, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
					}
					else
					{
						//未找到，进行动态属性处理
						//searchPtr = Context.GC.AllocString(name);
						if (!TryCreateStringValue(name, out searchPtr, ref error))
						{
							goto flag_handle_error;
						}

					}
					//if (searchPtr == 0)
					//{
					//	RaiseOutOfMemory(ref error);
					//	goto flag_handle_error;
					//}

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
					cachePayload.RefInstance = instance;
					cachePayload.trait[0] = null; cachePayload.trait[1] = null;
					cachePayload.scopemember_index = 0;
					cachePayload.searchPropertyName = searchPtr; cachePayload.as_type = as_type;
					cachePayload.searchNameSpacePtr = 0; cachePayload.indexer_key.setFault();

					stackslots[stack.index].SetHeapPtr(cacheobjpointer, (byte)RtHeapTypeKind.STACK_CACHE_OBJ, (byte)HeapKindFlag.NONE);

					goto lbl_multiname_dynamicprop;
				}
				else if (count == 1)
				{
					var member = type.Members[i];

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
					cachePayload.RefInstance = instance;
					cachePayload.trait[0] = member.trait; cachePayload.trait[1] = null;
					cachePayload.scopemember_index = (ushort)i;
					cachePayload.searchPropertyName.SetUndefined(); cachePayload.as_type = as_type;
					cachePayload.searchNameSpacePtr = 0; cachePayload.indexer_key.setFault();

					stackslots[stack.index].SetHeapPtr(cacheobjpointer, (byte)RtHeapTypeKind.STACK_CACHE_OBJ, (byte)HeapKindFlag.NONE);

					goto lbl_multiname_success;
				}
				else
				{
#if DEBUG
					throw new InvalidOperationException();
#else
					Environment.FailFast("出错了，这里跑不到"); return default;
#endif
				}
			}
			else if (kind == RtHeapTypeKind.CLOSURE)
			{
				//查找虚函数表
				int m_idx, m_count, g_idx, g_count, s_idx, s_count;
				multinamelsearch_findvables(Context.FUNCTION.Instance._vtable, name, out m_idx, out g_idx, out s_idx, out m_count, out g_count, out s_count, ns_set, issameorinherit, exclude_user_ns);
				if (m_count == 1)
				{
					var vitem = Context.FUNCTION.Instance._vtable.Items[m_idx];

					int ptrIndex = stackStPos + stack.index;
					int m_closurePtr = Context.M_ClosurePtr + ptrIndex;

					Context.GC.Heap[m_closurePtr].Type = vitem.Trait.Method.Body;
					RtClosure closure = (RtClosure)Context.GC.Heap[m_closurePtr];
					//closure.This.SetHeapPtr(instance.HeapPtr);
					closure.This = instance;
					closure.ScopePtr = instance.HeapPtr;
					//closure.ScopeType = vitem.DefineAt;
					closure._ref_as_type = Context.FUNCTION.Instance;
					closure.methodscopeslot_ref_state = 0; closure.HEAPINSTANCE_PTR = 0;
					stackslots[stack.index].SetHeapPtr(m_closurePtr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);

					goto lbl_multiname_success;
				}
				else if (g_count == 1 || s_count == 1)
				{
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
					cachePayload.RefInstance = instance;
					if (g_idx > -1)
					{
						cachePayload.trait[0] = Context.FUNCTION.Instance._vtable.Items[g_idx].Trait;
						cachePayload.g_index = g_idx;
					}
					else
					{
						cachePayload.trait[0] = null;
					}

					if (s_idx > -1)
					{
						cachePayload.trait[1] = Context.FUNCTION.Instance._vtable.Items[s_idx].Trait;
						cachePayload.s_index = s_idx;
					}
					else
					{
						cachePayload.trait[1] = null;
					}

					cachePayload.scopemember_index = 0;
					cachePayload.searchPropertyName.SetUndefined(); cachePayload.as_type = Context.FUNCTION.Instance;
					cachePayload.searchNameSpacePtr = 0; cachePayload.indexer_key.setFault();

					stackslots[stack.index].SetHeapPtr(cacheobjpointer, (byte)RtHeapTypeKind.STACK_CACHE_OBJ, (byte)HeapKindFlag.NONE);

					goto lbl_multiname_success;

				}
				else
				{

					Context.GC.CheckGC(ref error);

					NaNBoxing searchPtr = default;
					if (
						//string.CompareOrdinal(name, "valueOf") == 0
						name.CompareTo("valueOf", StringComparison.Ordinal) == 0
						)
					{
						searchPtr.SetHeapPtr(VALUEOF_STR, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
					}
					else if (
						//string.CompareOrdinal(name, "toString") == 0
						name.CompareTo("toString", StringComparison.Ordinal) == 0
						)
					{
						searchPtr.SetHeapPtr(TOSTRING_STR, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
					}
					else
					{
						//未找到，进行动态属性处理
						//searchPtr = Context.GC.AllocString(name);
						if (!TryCreateStringValue(name, out searchPtr, ref error))
						{
							goto flag_handle_error;
						}

					}
					//if (searchPtr == 0)
					//{
					//	RaiseOutOfMemory(ref error);
					//	goto flag_handle_error;
					//}

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
					cachePayload.RefInstance = instance;
					cachePayload.trait[0] = null; cachePayload.trait[1] = null;
					cachePayload.scopemember_index = 0;
					cachePayload.searchPropertyName = searchPtr; cachePayload.as_type = as_type;
					cachePayload.searchNameSpacePtr = 0; cachePayload.indexer_key.setFault();

					stackslots[stack.index].SetHeapPtr(cacheobjpointer, (byte)RtHeapTypeKind.STACK_CACHE_OBJ, (byte)HeapKindFlag.NONE);

					goto lbl_multiname_dynamicprop;
				}

			}
			else
			{
#if DEBUG
				throw new InvalidOperationException();
#else
				Environment.FailFast("出错了，这里跑不到"); return default;
#endif
			}


		lbl_multiname_dynamicprop:
		lbl_multiname_success:
			return 0;

		flag_handle_error:
			return 1;

		lbl_multiname_ambiguous:
			return 2;
			//lbl_multiname_notfound:
			//    return 3;
		}


		/// <summary>
		/// 给动态属性赋值，如果不存在，则创建它
		/// 保存实体的操作已移动至内部，外面不需要先GetSaveValue了
		/// </summary>
		/// <param name="error"></param>
		/// <param name="propname"></param>
		/// <param name="value"></param>
		internal void CreateDynamic(ref ReceiveError error, RtHeapBase instance, NaNBoxing propname, NaNBoxing value, bool configurable, bool enumerable, bool writeable)
		{
			findd_lastshapeptr = 0;
			
			int PROPERTY_PTR = GetPropertyPtr(instance);

#if DEBUG
			if (instance.Kind == RtHeapTypeKind.INSTANCE)
			{
				if (((ASInstance)instance.Type).Flags.HasFlag(ClassFlags.Struct))
				{
					throw new InvalidOperationException();
				}
			}

#endif
			//string name = ((RtPayloadString)Context.GC.Heap[propname_ptr]).Str;

			Span<char> temp = stackalloc char[16];
			ReadOnlySpan<char> name = temp;
			if (propname.ValueType == BoxType.LocalString)
			{
				int l = propname.GetLocalStringChars(temp);
				name = temp.Slice(0, l);
			}
			else
			{
				name = ((RtString)Context.GC.Heap[propname.HeapPtr]).Str;
			}


			RtShape.PropertyAttribute attribute = 0;
			if (configurable)
				attribute |= RtShape.PropertyAttribute.Configurable;
			if (enumerable)
				attribute |= RtShape.PropertyAttribute.Enumerable;
			if (writeable)
				attribute |= RtShape.PropertyAttribute.Writable;


			if (PROPERTY_PTR == 0)
			{


				//先创建或者查找Shape。(第一个属性就是)
				var blank_shape = (RtShape)Context.GC.Heap[Context.BlankShapePtr];

				var ptr = blank_shape.PTR_CHILD;
				RtShape shape;
				while (ptr != 0)
				{
					shape = (RtShape)Context.GC.Heap[ptr];
					if (
						//shape.Attribute.HasFlag(
						//RtPayloadShape.PropertyAttribute.Configurable |
						//RtPayloadShape.PropertyAttribute.Enumerable |
						//RtPayloadShape.PropertyAttribute.Writable)

						shape.Attribute == attribute

						&&

						//string.Equals(((RtPayloadString)Context.GC.Heap[shape.PTR_NAME]).Str,
						//	name,
						//	StringComparison.Ordinal)

						CompareShapePropertyName(shape.PTR_NAME, name) == 0

						)
					{
						break;
					}
					else
					{
						ptr = shape.PTR_BROTHER;
					}
				}

				if (ptr == 0)
				{

					ptr = Context.GC.AllocShape();
					if (ptr == 0)
					{
						RaiseOutOfMemory(ref error);
						return;
					}



					shape = (RtShape)Context.GC.Heap[ptr];

					shape.Attribute = attribute;
					//RtPayloadShape.PropertyAttribute.Configurable |
					//RtPayloadShape.PropertyAttribute.Enumerable |
					//RtPayloadShape.PropertyAttribute.Writable;


					shape.PTR_NAME = propname;

					shape.PTR_PARENT = Context.BlankShapePtr;
					shape.PTR_CHILD = 0;

					shape.PTR_BROTHER = blank_shape.PTR_CHILD;
					blank_shape.PTR_CHILD = ptr;
				}

				//缓存对象到实体
				value = GetSaveValue(value, ref error);
				if (error.raised)
				{
					return;
				}

				var prop_ptr = Context.GC.AllocDynamicSlot();
				if (prop_ptr == 0)
				{
					RaiseOutOfMemory(ref error);
					return;
				}

				Context.GC.UpdateMemUsage_Sub(Context.GC.Heap[prop_ptr]);

				var propDynamic = (RtDynamic)Context.GC.Heap[prop_ptr];
				propDynamic.SHAPE_PTR = ptr;
				propDynamic.Slots.Add(value);

				Context.GC.UpdateMemUsage_Add(Context.GC.Heap[prop_ptr]);

				if (instance.Kind == RtHeapTypeKind.INSTANCE)
				{
					((RtInstance)instance).Set_PROPERTY_PTR(prop_ptr, this, (ASInstance)instance.Type);
				}
				else if (instance.Kind == RtHeapTypeKind.CLASS)
				{
					((RtScriptClass)instance).PROPERTY_PTR = prop_ptr;
				}
				else if (instance.Kind == RtHeapTypeKind.GLOBAL)
				{
					((RtScriptClass)instance).PROPERTY_PTR = prop_ptr;
				}
				else if (instance.Kind == RtHeapTypeKind.ARRAY)
				{
					((RtArray)instance).Set_PROPERTY_PTR(prop_ptr, this);
				}
				else if (instance.Kind == RtHeapTypeKind.CLOSURE)
				{
					((RtClosure)instance).Set_PROPERTY_PTR(prop_ptr, this);
				}
				else
				{
#if DEBUG
					throw new InvalidOperationException();
#else
					Environment.FailFast("出错了，这里跑不到"); return;
#endif
				}
			}
			else
			{
				//查看是否在当前Transation链上。
				RtDynamic prop = (RtDynamic)Context.GC.Heap[PROPERTY_PTR];

				int index = prop.Slots.Count - 1;

				RtShape shape = null;

				int p = prop.SHAPE_PTR;

				while (p != Context.BlankShapePtr)
				{
					shape = (RtShape)Context.GC.Heap[p];

					if (
						CompareShapePropertyName(shape.PTR_NAME, name) == 0
						//	string.Equals(
						//((RtPayloadString)Context.GC.Heap[propname_ptr]).Str,
						//((RtPayloadString)Context.GC.Heap[shape.PTR_NAME]).Str,
						//StringComparison.Ordinal
						//)

						)
					{
						break;
					}
					else
					{

						p = shape.PTR_PARENT;
						index--;
					}
				}

				if (index >= 0)
				{
					if (!shape.Attribute.HasFlag(RtShape.PropertyAttribute.Writable))
					{
						RaiseError(ref error, $"not writeable property {name}");
						return;
					}

					if (CopyIfSameTypeStructAndReplaceSrc(prop.Slots[index], ref value))
					{

					}
					else
					{
						value = GetSaveValue(value, ref error);
						if (error.raised)
						{
							return;
						}

						prop.Slots[index] = value;
					}
				}
				else
				{
					//查找是否有现成的

					int p_child = ((RtShape)Context.GC.Heap[prop.SHAPE_PTR]).PTR_CHILD;

					while (p_child != 0)
					{
						shape = (RtShape)Context.GC.Heap[p_child];

						if (
							//string.Equals(
							//((RtPayloadString)Context.GC.Heap[propname_ptr]).Str,
							//((RtPayloadString)Context.GC.Heap[shape.PTR_NAME]).Str,
							//StringComparison.Ordinal
							//)
							CompareShapePropertyName(shape.PTR_NAME, name) == 0
							&&
							//shape.Attribute.HasFlag(RtPayloadShape.PropertyAttribute.Configurable | RtPayloadShape.PropertyAttribute.Writable | RtPayloadShape.PropertyAttribute.Enumerable)
							shape.Attribute == attribute
							)
						{

							value = GetSaveValue(value, ref error);
							if (error.raised)
							{
								return;
							}

							prop.SHAPE_PTR = p_child;
							index = prop.Slots.Count;
							prop.Slots.Add(value);
							break;
						}
						p_child = shape.PTR_BROTHER;

					}


					if (index < 0)
					{
						value = GetSaveValue(value, ref error);
						if (error.raised)
						{
							return;
						}

						//没有，创建一个，然后挂到当前的shape的子节点上然后将当前shape指向新建的shape
						var nshape_ptr = Context.GC.AllocShape();
						if (nshape_ptr == 0)
						{
							RaiseOutOfMemory(ref error);
							return;
						}
						var new_shape = (RtShape)Context.GC.Heap[nshape_ptr];

						new_shape.Attribute = attribute;


						new_shape.PTR_NAME = propname;

						//string pname = ((RtPayloadString)Context.GC.Heap[propname_ptr]).Str;

						var current_shape = (RtShape)Context.GC.Heap[prop.SHAPE_PTR];

						new_shape.PTR_PARENT = prop.SHAPE_PTR;
						new_shape.PTR_CHILD = 0;

						new_shape.PTR_BROTHER = current_shape.PTR_CHILD;
						current_shape.PTR_CHILD = nshape_ptr;

						prop.SHAPE_PTR = nshape_ptr;
						prop.Slots.Add(value);
					}



				}


			}

		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		internal int GetPropertyPtr(RtHeapBase instance)
		{
			int PROPERTY_PTR;
			if (instance.Kind == RtHeapTypeKind.INSTANCE)
			{
				if (((ASInstance)instance.Type).Flags.HasFlag(ClassFlags.Struct))
				{
					PROPERTY_PTR = 0;
				}
				else
				{
					PROPERTY_PTR = ((RtInstance)instance).PROPERTY_PTR(this, (ASInstance)instance.Type);
				}
			}
			else if (instance.Kind == RtHeapTypeKind.CLASS || instance.Kind == RtHeapTypeKind.GLOBAL)
			{
				PROPERTY_PTR = ((RtScriptClass)instance).PROPERTY_PTR;
			}
			else if (instance.Kind == RtHeapTypeKind.ARRAY)
			{
				PROPERTY_PTR = ((RtArray)instance).PROPERTY_PTR(this);
			}
			else if (instance.Kind == RtHeapTypeKind.CLOSURE)
			{
				PROPERTY_PTR = ((RtClosure)instance).PROPERTY_PTR(this);
			}
			else
			{
#if DEBUG
				throw new InvalidOperationException();
#else
					Environment.FailFast("出错了，这里跑不到"); return default;
#endif
			}
			return PROPERTY_PTR;
		}

		int findd_lastshapeptr = 0;
		Memory<char> findd_buffer = new Memory<char>(new char[16]);
		int findd_chars = 0;

		int findd_matchShapePtr = 0;

		int findd_slot = 0;


		internal bool FindDynamicValue(RtHeapBase instance, ReadOnlySpan<char> searchName, out NaNBoxing value, out int matchShapePtr, out int slotindex, out RtDynamic prop)
		{
			int PROPERTY_PTR = GetPropertyPtr(instance);
			if (PROPERTY_PTR != 0)
			{
				prop = (RtDynamic)Context.GC.Heap[PROPERTY_PTR];

				int p = prop.SHAPE_PTR;
				int index = prop.Slots.Count - 1;

				//这是一个简单的缓存机制，只要有任何修改动态属性操作就会失效，这样肯定不会出错。
				if (findd_lastshapeptr == p && searchName.CompareTo(findd_buffer.Span.Slice(0, findd_chars), StringComparison.Ordinal) == 0)
				{
					matchShapePtr = findd_matchShapePtr;

					slotindex = findd_slot;
					value = prop.Slots[slotindex];
					return true;
				}
				else
				{
					while (p != Context.BlankShapePtr)
					{
						var shape = (RtShape)Context.GC.Heap[p];

						//if (string.Equals(
						//	//((RtPayloadString)Context.GC.Heap[propname_ptr]).Str,
						//	searchName,
						//	((RtPayloadString)Context.GC.Heap[shape.PTR_NAME]).Str,
						//	StringComparison.Ordinal
						//	))
						if (CompareShapePropertyName(shape.PTR_NAME, searchName) == 0)
						{
							if (searchName.Length <= 16)
							{
								findd_lastshapeptr = prop.SHAPE_PTR;

								searchName.CopyTo(findd_buffer.Span);
								findd_chars = searchName.Length;
								findd_matchShapePtr = p;
								findd_slot = index;
							}
							else
							{
								findd_lastshapeptr = 0;
							}


							matchShapePtr = p;
							value = prop.Slots[index];
							slotindex = index;
							return true;
						}
						--index;
						p = shape.PTR_PARENT;
					}
				}
			}
			else
			{
				prop = null;
			}

			value = default; matchShapePtr = 0; slotindex = -1; return false;
		}

		private void VisitDynamicValue(RtHeapBase instance, Action<NaNBoxing, NaNBoxing> OnVisitProp)
		{
			int PROPERTY_PTR = GetPropertyPtr(instance);
			if (PROPERTY_PTR != 0)
			{
				var prop = (RtDynamic)Context.GC.Heap[PROPERTY_PTR];

				int p = prop.SHAPE_PTR;
				int index = prop.Slots.Count - 1;

				while (p != Context.BlankShapePtr)
				{
					var shape = (RtShape)Context.GC.Heap[p];

					if (shape.Attribute.HasFlag(RtShape.PropertyAttribute.Enumerable))
					{

						OnVisitProp(shape.PTR_NAME, prop.Slots[index]);
					}
					--index;
					p = shape.PTR_PARENT;
				}
			}

		}

		private void ChangeTranslation(RtDynamic dynamic, int shape_ptr, ref ReceiveError error)
		{
			findd_lastshapeptr = 0;

			if (dynamic.SHAPE_PTR == shape_ptr) //正好删除最后一个,使SHAPE指向上一个SHAPE即可
			{
				dynamic.SHAPE_PTR = ((RtShape)Context.GC.Heap[shape_ptr]).PTR_PARENT;
			}
			else
			{
				//先反向search到，要移除的shape的下一个shape。
				int p = dynamic.SHAPE_PTR;
				var parent_p = ((RtShape)Context.GC.Heap[dynamic.SHAPE_PTR]).PTR_PARENT;

				List<int> path = new List<int>();
				path.Add(p);

				while (parent_p != shape_ptr)
				{
					p = parent_p;
					parent_p = ((RtShape)Context.GC.Heap[p]).PTR_PARENT;

					path.Add(p);
				}

				//从shape_ptr的父节点开始找
				var chain_node = (RtShape)Context.GC.Heap[((RtShape)Context.GC.Heap[shape_ptr]).PTR_PARENT];

				int found_ptr = 0;
				for (int i = path.Count - 1; i >= 0; i--)
				{
					RtShape tomatch = (RtShape)Context.GC.Heap[path[i]];
					//string tomatch_name = GetShapePropertyNameAsString(tomatch.PTR_NAME);
					var tomatch_name = tomatch.PTR_NAME;

					bool found = false;

					var search_p = chain_node.PTR_CHILD;

					while (search_p != 0)
					{
						var shape = (RtShape)Context.GC.Heap[search_p];

						if (search_p != shape_ptr)
						{
							//string s_name = GetShapePropertyNameAsString(shape.PTR_NAME);
							var s_name = shape.PTR_NAME;

							if (
								tomatch.Attribute == shape.Attribute
								&&
								//string.Equals(s_name, tomatch_name, StringComparison.Ordinal))
								CompareShapePropertyName(s_name, tomatch_name) == 0)

							{
								//找到了，继续找下一个链
								chain_node = shape;
								found = true;
								found_ptr = search_p;
								break;
							}
						}
						search_p = shape.PTR_BROTHER;
					}

					if (!found)
					{
						chain_node = null;
						break;
					}
				}

				if (chain_node == null)
				{
					//需要新clone一条链
					//chain_node = (RtPayloadShape)Context.GC.Heap[((RtPayloadShape)Context.GC.Heap[shape_ptr]).PTR_PARENT];
					int chain_ptr = ((RtShape)Context.GC.Heap[shape_ptr]).PTR_PARENT;


					for (int i = path.Count - 1; i >= 0; i--)
					{
						var nshape_ptr = Context.GC.AllocShape();
						if (nshape_ptr == 0)
						{
							RaiseOutOfMemory(ref error);
							return;
						}

						var new_shape = (RtShape)Context.GC.Heap[nshape_ptr];
						var old_shape = (RtShape)Context.GC.Heap[path[i]];

						new_shape.Attribute = old_shape.Attribute;
						new_shape.PTR_NAME = old_shape.PTR_NAME;
						new_shape.PTR_PARENT = chain_ptr;

						chain_node = (RtShape)Context.GC.Heap[chain_ptr];
						new_shape.PTR_BROTHER = chain_node.PTR_CHILD;
						chain_node.PTR_CHILD = nshape_ptr;

						chain_ptr = nshape_ptr;

					}

					//chain_node = (RtPayloadShape)Context.GC.Heap[chain_ptr];
					//string foundname = ((RtPayloadString)Context.GC.Heap[chain_node.PTR_NAME]).Str;
					//string path_s = "";
					//do
					//{
					//    path_s = path_s + ((RtPayloadString)Context.GC.Heap[chain_node.PTR_NAME]).Str + ",";

					//    chain_node = (RtPayloadShape)Context.GC.Heap[chain_node.PTR_PARENT];

					//} while (chain_node.PTR_PARENT != 0);

					dynamic.SHAPE_PTR = chain_ptr;

				}
				else
				{
					//string foundname = ((RtPayloadString)Context.GC.Heap[chain_node.PTR_NAME]).Str;
					//string path_s ="";
					//do
					//{
					//    path_s = path_s + ((RtPayloadString)Context.GC.Heap[chain_node.PTR_NAME]).Str + ",";

					//    chain_node = (RtPayloadShape)Context.GC.Heap[chain_node.PTR_PARENT];

					//} while (chain_node.PTR_PARENT != 0);


					dynamic.SHAPE_PTR = found_ptr;
				}


			}

		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
#if FORCOMPILER
		public 
#else
		public
#endif
		int InitCacheInstance(ASClass @class, int slotindex, bool initmember,out RtInstance instance)
		{

			int cache_ptr = Context.CacheInstancePtr + slotindex;

			var cache = Context.GC.Heap[cache_ptr];
			cache.Type = @class.Instance;

			//((RtInstance)cache).HEAPINSTANCE_PTR = 0;
			//((RtInstance)cache).Set_PROPERTY_PTR(0, Context.player, @class.Instance);
			//((RtInstance)cache).Set_PROTOTYPE(((RtScriptClass)Context.GC.Heap[@class.__instance_index__]).PROTO__PTR, this);
			//((RtInstance)cache).methodscopeslot_ref_state = 0;
			((RtInstance)cache).SetDefaultCacheData(((RtScriptClass)Context.GC.Heap[@class.__instance_index__]).PROTO__PTR);


			CodeScope cscope = @class.Instance._link_codescope;
			if (cscope.TypeLayout.Size > 0)
			{
				((RtInstance)cache).Init(cscope, Context.player, initmember);
			}

			instance = (RtInstance)cache;

			Context.StackSlots[slotindex].SetHeapPtr(cache_ptr, (byte)RtHeapTypeKind.INSTANCE, (byte)(@class.Instance.Flags.HasFlag(ClassFlags.Struct) ? HeapKindFlag.FLAG_STRUCT : HeapKindFlag.NONE));
			return cache_ptr;
		}






		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private unsafe int Ld_function_and_store_member( ASMethod function, ScopeHeapLocater heapLocater, RtHeapBase mscope, int scope_ptr,  ref ReceiveError error,
			int stackStPos, StackLocater target, Span<NaNBoxing> stackslots, int* method_scopes, out RtHeapBase closure_instance)
		{
			if (!(heapLocater.MemberIndex == ushort.MaxValue && heapLocater.ScopeIndex == ushort.MaxValue))
			{
				var s = mscope; //Context.GC.Heap[scope_ptr];
				if (s.Type._link_codescope.index != heapLocater.ScopeIndex)
				{

					while (
						s.Kind == RtHeapTypeKind.MethodScope &&
						s.Type._link_codescope.index != heapLocater.ScopeIndex
					)
					{

						s = Context.GC.Heap[((RtMethodScope)s).ParentPtr];

					}

					NaNBoxing c = default;
					if (s.Kind == RtHeapTypeKind.GLOBAL)
					{
#if DEBUG
						if (((RtScriptClass)s).Meta._link_codescope.index != heapLocater.ScopeIndex)
						{
							throw new InvalidOperationException();
						}
#endif

						c = ((RtScriptClass)s).ReadSlot(heapLocater.MemberIndex);
					}
					else if (s.Kind == RtHeapTypeKind.MethodScope)
					{
						c = ((RtMethodScope)s).ReadSlot(heapLocater.MemberIndex);
					}
					else if (s.Kind == RtHeapTypeKind.INSTANCE)
					{
#if DEBUG
						if (s.Type._link_codescope.Parent.index != heapLocater.ScopeIndex)
						{
							throw new InvalidOperationException();
						}
#endif
						s = Context.GC.Heap[((ASScript)s.Type._link_codescope.Parent.Container).__global_index__];
						c = ((RtScriptClass)s).ReadSlot(heapLocater.MemberIndex);
					}
					else
					{
#if DEBUG
						throw new InvalidOperationException();
#else
						Environment.FailFast("出错了，这里跑不到"); closure_instance=null; return default;
#endif
					}

					if (c.ValueType == BoxType.Undefined)
					{
						//ASMethod function = Context.link_const_methods[(int)fbox];  //((ASMethodBody)obj.Type).Method;

						int ptrIndex = stackStPos + target.index;
						int closurePtr = Context.M_ClosurePtr + ptrIndex;

						var closure = Context.GC.Heap[closurePtr];
						closure.Type = function.Body;
						((RtClosure)closure).ScopePtr = scope_ptr;
						//((RtClosure)closure).ScopeType = null;
						((RtClosure)closure)._ref_as_type = null;
						((RtClosure)closure).This.SetNull(); ((RtClosure)closure).methodscopeslot_ref_state = 0;
						((RtClosure)closure).HEAPINSTANCE_PTR = 0;

						//stackslots[target.index].SetHeapPtr(closurePtr);

						NaNBoxing v = new NaNBoxing();
						v.SetHeapPtr(closurePtr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);
						v = GetSaveValue(v, ref error);
						if (error.raised)
						{
							closure_instance = null;
							return 0;
						}


						if (s.Kind == RtHeapTypeKind.GLOBAL)
						{
							((RtScriptClass)s).SetSlot(v, heapLocater.MemberIndex);
							stackslots[target.index] = v;
						}
						else if (s.Kind == RtHeapTypeKind.MethodScope)
						{
							((RtMethodScope)s).SetSlot(v, heapLocater.MemberIndex);
							stackslots[target.index] = v;
						}

						closure_instance = closure;
						return closurePtr;
					}
					else
					{
#if DEBUG
						if (c.ValueType != BoxType.HeapPtr)
							throw new InvalidOperationException();
						if (Context.GC.Heap[c.HeapPtr].Kind != RtHeapTypeKind.CLOSURE)
							throw new InvalidOperationException();

#endif

						stackslots[target.index] = c;
						closure_instance = Context.GC.Heap[c.HeapPtr];
						return c.HeapPtr;
					}

				}
				else
				{
					int* m_scope = method_scopes;
					*m_scope++ = scope_ptr;
#if DEBUG
					if (s.Type._link_codescope.index != heapLocater.ScopeIndex)
					{
						throw new InvalidOperationException();
					}
#endif
					var c = ((RtMethodScope)s).ReadSlot(heapLocater.MemberIndex);
					if (c.ValueType == BoxType.Undefined)
					{
						//ASMethod function = Context.link_const_methods[(int)fbox];  //((ASMethodBody)obj.Type).Method;

						int ptrIndex = stackStPos + target.index;
						int closurePtr = Context.M_ClosurePtr + ptrIndex;

						var closure = Context.GC.Heap[closurePtr];
						closure.Type = function.Body;
						((RtClosure)closure).ScopePtr = scope_ptr;
						//((RtClosure)closure).ScopeType = null;
						((RtClosure)closure)._ref_as_type = null;
						((RtClosure)closure).This.SetNull(); ((RtClosure)closure).methodscopeslot_ref_state = 0;
						((RtClosure)closure).HEAPINSTANCE_PTR = 0;

						
						
						NaNBoxing v = new NaNBoxing();
						v.SetHeapPtr(closurePtr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);

						//保存到method的成员中，可以考虑到缓存
						PrepareSaveMethodScope((RtMethodScope)s,  heapLocater, ref v, null, method_scopes, ref error);
						if (error.raised)
						{
							closure_instance = null;
							return 0;
						}


						((RtMethodScope)s).SetSlot(v, heapLocater.MemberIndex);
						stackslots[target.index] = v;
						closure_instance = Context.GC.Heap[v.HeapPtr]; ;
						return v.HeapPtr;
					}
					else
					{
#if DEBUG
						if (c.ValueType != BoxType.HeapPtr)
							throw new InvalidOperationException();
						if (Context.GC.Heap[c.HeapPtr].Kind != RtHeapTypeKind.CLOSURE)
							throw new InvalidOperationException();

#endif
						stackslots[target.index] = c;
						closure_instance = Context.GC.Heap[c.HeapPtr];
						return c.HeapPtr;
					}


				}


			}
			else
			{
				//ASMethod function = Context.link_const_methods[(int)fbox];  //((ASMethodBody)obj.Type).Method;

				int ptrIndex = stackStPos + target.index;
				int closurePtr = Context.M_ClosurePtr + ptrIndex;

				var closure = Context.GC.Heap[closurePtr];
				closure.Type = function.Body;
				((RtClosure)closure).ScopePtr = scope_ptr;
				//((RtClosure)closure).ScopeType = null;
				((RtClosure)closure)._ref_as_type = null;
				((RtClosure)closure).This.SetNull(); ((RtClosure)closure).methodscopeslot_ref_state = 0;
				((RtClosure)closure).HEAPINSTANCE_PTR = 0;

				stackslots[target.index].SetHeapPtr(closurePtr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);
				closure_instance = closure;
				return closurePtr;
			}
		}


		
		

		
		
		



		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private bool Is(NaNBoxing v, ASClass typeclass)
		{

			switch (v.ValueType)
			{

				case BoxType.Undefined:
				case BoxType.Null:
					return false;
				case BoxType.Boolean:
					return (typeclass == Context.BOOLEAN || typeclass == Context.OBJECT);
				case BoxType.Number:
					{
						if (typeclass == Context.NUMBER)
						{
							return true;
						}
						else if (typeclass == Context.FLOAT)
						{
							return Extensions.CanConvertToFloatLossless(v.Number);
						}
						else if (typeclass == Context.UINT)
						{
							if (Math.Truncate(v.Number) == v.Number)
							{
								return v.Number >= uint.MinValue && v.Number <= uint.MaxValue;
							}
							else
							{
								return false;
							}
						}
						else if (typeclass == Context.INT)
						{
							if (Math.Truncate(v.Number) == v.Number)
							{
								return v.Number >= int.MinValue && v.Number <= int.MaxValue;
							}
							else
							{
								return false;
							}
						}
						else if (typeclass == Context.USHORT)
						{
							if (Math.Truncate(v.Number) == v.Number)
							{
								return v.Number >= ushort.MinValue && v.Number <= ushort.MaxValue;
							}
							else
							{
								return false;
							}
						}
						else if (typeclass == Context.SHORT)
						{
							if (Math.Truncate(v.Number) == v.Number)
							{
								return v.Number >= short.MinValue && v.Number <= short.MaxValue;
							}
							else
							{
								return false;
							}
						}
						else if (typeclass == Context.BYTE)
						{
							if (Math.Truncate(v.Number) == v.Number)
							{
								return v.Number >= byte.MinValue && v.Number <= byte.MaxValue;
							}
							else
							{
								return false;
							}
						}
						else if (typeclass == Context.SBYTE)
						{
							if (Math.Truncate(v.Number) == v.Number)
							{
								return v.Number >= sbyte.MinValue && v.Number <= sbyte.MaxValue;
							}
							else
							{
								return false;
							}
						}
						else
						{
							return typeclass == Context.OBJECT;
						}
					}

				case BoxType.Int:
					{
						if (typeclass == Context.NUMBER)
						{
							return true;
						}
						else if (typeclass == Context.FLOAT)
						{
							const int MAX_LOSSLESS = 1 << 23; // 8388608
							return Math.Abs(v.IntValue) <= MAX_LOSSLESS;
						}
						else if (typeclass == Context.UINT)
						{
							return v.IntValue >= 0;
						}
						else if (typeclass == Context.INT)
						{
							return true;
						}
						else if (typeclass == Context.USHORT)
						{

							return v.IntValue >= ushort.MinValue && v.IntValue <= ushort.MaxValue;

						}
						else if (typeclass == Context.SHORT)
						{

							return v.IntValue >= short.MinValue && v.IntValue <= short.MaxValue;

						}
						else if (typeclass == Context.BYTE)
						{
							return v.IntValue >= byte.MinValue && v.IntValue <= byte.MaxValue;
						}
						else if (typeclass == Context.SBYTE)
						{
							return v.IntValue >= sbyte.MinValue && v.IntValue <= sbyte.MaxValue;
						}
						else
						{
							return typeclass == Context.OBJECT;
						}
					}
				case BoxType.Uint:
					{
						if (typeclass == Context.NUMBER)
						{
							return true;
						}
						else if (typeclass == Context.FLOAT)
						{
							const int MAX_LOSSLESS = 1 << 23; // 8388608
							return v.UIntValue <= MAX_LOSSLESS;
						}
						else if (typeclass == Context.UINT)
						{
							return true;
						}
						else if (typeclass == Context.INT)
						{
							return v.UIntValue <= int.MaxValue;
						}
						else if (typeclass == Context.USHORT)
						{
							return v.UIntValue >= ushort.MinValue && v.UIntValue <= ushort.MaxValue;
						}
						else if (typeclass == Context.SHORT)
						{
							return v.UIntValue <= short.MaxValue;
						}
						else if (typeclass == Context.BYTE)
						{
							return v.UIntValue >= byte.MinValue && v.UIntValue <= byte.MaxValue;
						}
						else if (typeclass == Context.SBYTE)
						{
							return v.UIntValue <= sbyte.MaxValue;
						}
						else
						{
							return typeclass == Context.OBJECT;
						}
					}
				case BoxType.Sbyte:
					{
						if (typeclass == Context.NUMBER)
						{
							return true;
						}
						else if (typeclass == Context.FLOAT)
						{
							return true;
						}
						else if (typeclass == Context.UINT)
						{
							return v.SByteValue >= 0;
						}
						else if (typeclass == Context.INT)
						{
							return true;
						}
						else if (typeclass == Context.USHORT)
						{
							return v.SByteValue >= 0;
						}
						else if (typeclass == Context.SHORT)
						{
							return true;
						}
						else if (typeclass == Context.BYTE)
						{
							return v.SByteValue >= 0;
						}
						else if (typeclass == Context.SBYTE)
						{
							return true;
						}
						else
						{
							return typeclass == Context.OBJECT;
						}
					}


				case BoxType.Byte:
					{
						if (typeclass == Context.NUMBER)
						{
							return true;
						}
						else if (typeclass == Context.FLOAT)
						{
							return true;
						}
						else if (typeclass == Context.UINT)
						{
							return true;
						}
						else if (typeclass == Context.INT)
						{
							return true;
						}
						else if (typeclass == Context.USHORT)
						{
							return true;
						}
						else if (typeclass == Context.SHORT)
						{
							return true;
						}
						else if (typeclass == Context.BYTE)
						{
							return true;
						}
						else if (typeclass == Context.SBYTE)
						{
							return true;
						}
						else
						{
							return typeclass == Context.OBJECT;
						}
					}

				case BoxType.Short:
					{
						if (typeclass == Context.NUMBER)
						{
							return true;
						}
						else if (typeclass == Context.FLOAT)
						{
							return true;
						}
						else if (typeclass == Context.UINT)
						{
							return v.ShortValue >= 0;
						}
						else if (typeclass == Context.INT)
						{
							return true;
						}
						else if (typeclass == Context.USHORT)
						{
							return v.ShortValue >= ushort.MinValue;
						}
						else if (typeclass == Context.SHORT)
						{
							return true;
						}
						else if (typeclass == Context.BYTE)
						{
							return v.ShortValue >= byte.MinValue && v.ShortValue <= byte.MaxValue;
						}
						else if (typeclass == Context.SBYTE)
						{
							return v.ShortValue >= sbyte.MinValue && v.ShortValue <= byte.MaxValue;
						}
						else
						{
							return typeclass == Context.OBJECT;
						}
					}
				case BoxType.UShort:
					{
						if (typeclass == Context.NUMBER)
						{
							return true;
						}
						else if (typeclass == Context.FLOAT)
						{
							return true;
						}
						else if (typeclass == Context.UINT)
						{
							return true;
						}
						else if (typeclass == Context.INT)
						{
							return true;
						}
						else if (typeclass == Context.USHORT)
						{
							return true;
						}
						else if (typeclass == Context.SHORT)
						{
							return v.UShortValue <= short.MaxValue;
						}
						else if (typeclass == Context.BYTE)
						{
							return v.UShortValue >= byte.MinValue && v.UShortValue <= byte.MaxValue;
						}
						else if (typeclass == Context.SBYTE)
						{
							return v.UShortValue <= byte.MaxValue;
						}
						else
						{
							return typeclass == Context.OBJECT;
						}
					}

				case BoxType.Float:

					{
						if (typeclass == Context.NUMBER)
						{
							return true;
						}
						else if (typeclass == Context.FLOAT)
						{
							return true;
						}
						else if (typeclass == Context.UINT)
						{
							if (MathF.Truncate(v.FloatValue) == v.FloatValue)
							{
								return v.FloatValue >= uint.MinValue && v.FloatValue <= uint.MaxValue;
							}
							else
							{
								return false;
							}
						}
						else if (typeclass == Context.INT)
						{
							if (MathF.Truncate(v.FloatValue) == v.FloatValue)
							{
								return v.FloatValue >= int.MinValue && v.FloatValue <= int.MaxValue;
							}
							else
							{
								return false;
							}
						}
						else if (typeclass == Context.USHORT)
						{
							if (MathF.Truncate(v.FloatValue) == v.FloatValue)
							{
								return v.FloatValue >= ushort.MinValue && v.FloatValue <= ushort.MaxValue;
							}
							else
							{
								return false;
							}
						}
						else if (typeclass == Context.SHORT)
						{
							if (MathF.Truncate(v.FloatValue) == v.FloatValue)
							{
								return v.FloatValue >= short.MinValue && v.FloatValue <= short.MaxValue;
							}
							else
							{
								return false;
							}
						}
						else if (typeclass == Context.BYTE)
						{
							if (MathF.Truncate(v.FloatValue) == v.FloatValue)
							{
								return v.FloatValue >= byte.MinValue && v.FloatValue <= byte.MaxValue;
							}
							else
							{
								return false;
							}
						}
						else if (typeclass == Context.SBYTE)
						{
							if (MathF.Truncate(v.FloatValue) == v.FloatValue)
							{
								return v.FloatValue >= sbyte.MinValue && v.FloatValue <= sbyte.MaxValue;
							}
							else
							{
								return false;
							}
						}
						else
						{
							return typeclass == Context.OBJECT;
						}
					}
				case BoxType.HeapPtr:
					{

						switch ((RtHeapTypeKind)v.HeapKind)
						{
							case RtHeapTypeKind.CLASS:
								return (typeclass == Context.OBJECT || typeclass == Context.CLASS);
							case RtHeapTypeKind.GLOBAL:
								return (typeclass == Context.OBJECT);
							case RtHeapTypeKind.STRING:
								return (typeclass == Context.STRING || typeclass == Context.OBJECT);
							case RtHeapTypeKind.INSTANCE:
								{
									var v_instance = Context.GC.Heap[v.HeapPtr];
									bool pass = typeclass == Context.OBJECT ||
										v_instance.Type == typeclass.Instance ||
										Extensions.IsExtend((ASInstance)v_instance.Type, typeclass.Instance) ||
										Extensions.IsImplements((ASInstance)v_instance.Type, typeclass.Instance);

									return pass;
								}
							case RtHeapTypeKind.NAMESPACE:
								return (typeclass == Context.OBJECT || ((ASClass)typeclass).Type_identifier == (ulong)TypeKind.Namespace);
							case RtHeapTypeKind.ARRAY:
								return (typeclass == Context.OBJECT || typeclass == Context.ARRAY);
							case RtHeapTypeKind.VECTOR:
								{
									if (typeclass == Context.OBJECT || typeclass == Context.VECTOR)
									{
										return (true);
									}

									if (typeclass.Instance.Flags.HasFlag(ClassFlags.Vector))
									{
										if (typeclass.Instance._element_class == null || typeclass.Instance._element_class == Context.OBJECT)
										{
											return (true);
										}
										var v_instance = Context.GC.Heap[v.HeapPtr];
										if (((RtVector)v_instance).element_asclass == typeclass.Instance._element_class)
										{
											return (true);
										}
									}

								}

								return (false);

							case RtHeapTypeKind.CLOSURE:
								return (typeclass == Context.OBJECT || typeclass == Context.FUNCTION);
#if DEBUG
							case RtHeapTypeKind.STACK_CACHE_OBJ:
							case RtHeapTypeKind.DYNAMIC_PROPERTYS:
							case RtHeapTypeKind.SHAPE:
							case RtHeapTypeKind.MethodScope:
							default:
								throw new InvalidOperationException();
#else
							default:
								Environment.FailFast("出错了，这里跑不到");
								return false;
#endif
						}


					}
				case BoxType.LocalString:
					// LocalString应该被视为String类型
					return (typeclass == Context.STRING || typeclass == Context.OBJECT);
#if DEBUG
				case BoxType.Fault:
				default:
					throw new InvalidOperationException();
#else
				default:
					Environment.FailFast("出错了，这里跑不到");
					return false;
#endif
			}



		}


		internal int GetProtoPtr(RtHeapBase obj)
		{
			int o_proto;
			switch (obj.Kind)
			{
				case RtHeapTypeKind.INSTANCE:
					o_proto = ((RtInstance)obj).PROTOTYPE(this, (ASInstance)obj.Type);
					break;
				case RtHeapTypeKind.CLOSURE:
					if (((ASMethodBody)obj.Type).Method.__ismethod)
					{
						o_proto = ((RtScriptClass)Context.GC.Heap[Context.METHOD_CLOSURE.__instance_index__]).PROTO__PTR;
					}
					else
					{
						o_proto = ((RtClosure)obj).PROTOTYPE(this);
						if (o_proto <= 0) //默认，指向$FUNCTION的proto
						{
							o_proto = ((RtScriptClass)Context.GC.Heap[Context.FUNCTION.__instance_index__]).PROTO__PTR;
							if (o_proto <= 0) // Function.prototype默认是一个closure,也可能<=0。那么就指向到Object.proto
							{
								o_proto = ((RtScriptClass)Context.GC.Heap[Context.OBJECT.__instance_index__]).PROTO__PTR;
							}
						}
					}
					break;
				case RtHeapTypeKind.ARRAY:
					o_proto = ((RtScriptClass)Context.GC.Heap[Context.ARRAY.__instance_index__]).PROTO__PTR;
					break;
				case RtHeapTypeKind.VECTOR:
					o_proto = ((RtScriptClass)Context.GC.Heap[obj.Type._link_codescope.TypeLayout.ASType.__instance_index__]).PROTO__PTR;
					break;
				case RtHeapTypeKind.CLASS:
					o_proto = ((RtScriptClass)Context.GC.Heap[Context.CLASS.__instance_index__]).PROTO__PTR;
					//o_proto = ((RtPayloadScriptClass)obj).PROTO__PTR;
					break;
				case RtHeapTypeKind.NAMESPACE:
					o_proto = ((RtScriptClass)Context.GC.Heap[Context.NAMESPACE.__instance_index__]).PROTO__PTR;
					break;

				default:
#if DEBUG
					throw new InvalidOperationException();
#else
					Environment.FailFast("出错了，这里跑不到");  return default;
#endif
			}

			return o_proto;
		}


		unsafe internal struct ExceptionContext
		{

			internal int catch_count;
			internal byte* CATCH;
			internal byte* FINALLY_PTR;
			internal byte* FINALLY_EXIT_PTR;

			internal int state;//!<-暂定，0 - 表示在 try 中， 1-表示在catch中, 2表示在finally中。
			internal byte* FINALLY_JUMPTO_PTR;

			internal StackLocater hold_error;
			//internal ScopeHeapLocater catched_error;
		}

		internal interface IResume_State
		{
			unsafe void Resume(ExceptionContext* e_ctx, ExceptionContext** current_e_ctx, byte* PC_START, byte** PC, Span<NaNBoxing> stackslots);

			unsafe void End();

			bool IsCallClose();


#if DEBUG
			unsafe void Debug_SaveOrLoadIterCtxIndex(int* iter_ctx_index);
#endif

		}

		[MethodImpl( MethodImplOptions.AggressiveOptimization)]
		internal unsafe void Execute(ref ASMethodBody.MethodBodyInfo info, RtHeapBase methodscope, int scope_ptr, //ASContainer scopeType,
			Span<NaNBoxing> stackslots,
			int stackStPos, out int PC_PTR, ref ReceiveError error, int returnSlotIndex, int calleelastPos, IResume_State resume_state)
		{
			//ASMethodBody.MethodBodyInfo info = new ASMethodBody.MethodBodyInfo();
			//method.Body.GetInfo(ref info);

#if DEBUG
			int iter_ctx_index = Context.GC.IterCtxIndex;

			if (resume_state != null)
			{
				resume_state.Debug_SaveOrLoadIterCtxIndex(&iter_ctx_index);
			}

#endif


			var method = ((ASMethodBody)methodscope.Type).Method;


			fixed (byte* p = method.Body.ByteCode)
			{
				ExceptionContext* exception_ctx_stack = stackalloc ExceptionContext[(method.Flags.HasFlag(MethodFlags.NoTry) ? 0 : Context.MAX_TRY_NESTED) + 2]; //加头尾2个哨兵
				ExceptionContext* NO_TRY = exception_ctx_stack;
				ExceptionContext* exception_ctx = NO_TRY;




				int* method_scopes = stackalloc int[64]; //64个，肯定不可能爆了。
				


				Span<NaNBoxing> constants = new Span<NaNBoxing>(p + 3 * sizeof(int) + 2 * sizeof(int) * info.instructions, info.constants); //(NaNBoxing*)((int*)p + 3);

				byte* PC = p + sizeof(int) * 3 + 2 * sizeof(int) * info.instructions + sizeof(NaNBoxing) * info.constants;
				byte* PC_START = PC;
				byte* PC_END = p + method.Body.ByteCode.Length - 4; //已经四字节对齐，所以最后一个END指令长度也是4

				if (resume_state != null)
				{
					resume_state.Resume(exception_ctx_stack, &exception_ctx, PC_START, &PC, stackslots);

					if (resume_state.IsCallClose()) //是否被要求关闭
					{
						if (exception_ctx != NO_TRY)
						{
							ExceptionContext* ctx = NO_TRY + 1;
							ctx->FINALLY_JUMPTO_PTR = PC_END;
							do
							{
								var finally_p = ctx->FINALLY_PTR;
								++ctx;

								ctx->FINALLY_JUMPTO_PTR = finally_p;

							} while (ctx < exception_ctx);

							PC = exception_ctx->FINALLY_PTR;
						}
						else
						{
							resume_state.End();
							goto flag_end;
						}


					}
				}

				//NaNBoxing global_obj = default;

				while (true)
				{


					int codeanddst = *(int*)PC; PC += 4;
					INS_Code opcode = (INS_Code)(byte)(codeanddst & 0xff);
					int dst_index = codeanddst >> 8;

#if PROFILEPLAYER
					InstructionProfiler.Profile_ActionStart(opcode);
#endif

				lbl_retry:

					switch (opcode)
					{
						case INS_Code.flag:

							break;
						case INS_Code.ld_const:
							{
								//StackLocater stackLocater;
								//stackLocater.index = dst_index;

								int const_id = *(int*)PC; PC += 4;

								stackslots[dst_index] = constants[const_id];

							}
							break;
						
						case INS_Code.ld_false:
							{
								//StackLocater stackLocater;
								//stackLocater.index = dst_index;

								stackslots[dst_index].SetBoolean(false);
							}
							break;
						case INS_Code.ld_true:
							{
								//StackLocater stackLocater;
								//stackLocater.index = dst_index;


								stackslots[dst_index].SetBoolean(true);
							}
							break;
						case INS_Code.ld_null:
							{
								//StackLocater stackLocater;
								//stackLocater.index = dst_index;
								stackslots[dst_index].SetNull();
							}
							break;
						case INS_Code.ld_undefined:
							{
								//StackLocater stackLocater;
								//stackLocater.index = dst_index;

								stackslots[dst_index].SetUndefined();
							}
							break;
						case INS_Code.ld_array_hole:
							{
								//StackLocater stackLocater;
								//stackLocater.index = dst_index;

								stackslots[dst_index].setFault();
							}
							break;
						case INS_Code.ld_class:
							{
								Ld_class(dst_index, &PC, method.Body.heapConstants  ,constants, stackslots, ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}
							}
							break;
						case INS_Code.ld_VectorType:
							{
								//StackLocater stackLocater;
								//stackLocater.index = dst_index;

								int vector_index = 0;
								LoadInt32(&vector_index, &PC);

								var boxing = constants[vector_index];

								Debug.Assert(boxing.ValueType == NaNBoxing.BoxType.Int);
								


								ASVector vector = Context.Vectors[boxing.IntValue];

								InitASClass(vector.vector_class, ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}

								stackslots[dst_index].SetHeapPtr(vector.vector_class.__instance_index__, (byte)RtHeapTypeKind.CLASS, (byte)HeapKindFlag.NONE);

							}
							break;
						case INS_Code.ld_namespace:
							{
								//StackLocater stackLocater;
								//stackLocater.index = dst_index;

								int namespace_instance_index = 0;
								LoadInt32(&namespace_instance_index, &PC);

								var boxing = constants[namespace_instance_index];

#if DEBUG
								if (boxing.ValueType != NaNBoxing.BoxType.HeapPtr)
								{
									throw new InvalidOperationException();
								}

								RtHeapBase instance = Context.GC.Heap[boxing.HeapPtr];
								if (instance.Kind != RtHeapTypeKind.NAMESPACE)
								{
									throw new InvalidOperationException();
								}

#endif

								stackslots[dst_index] = boxing; //.SetHeapPtr(boxing.HeapPtr);

							}
							break;
						case INS_Code.delete:
							{
								
								DELETE( dst_index, &PC, stackslots, stackStPos, method,  ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}

							}
							break;
						case INS_Code.ld_MultiName_Ref:
							{
								

								Ld_MultiName_Ref( dst_index, &PC,methodscope, constants, stackslots, stackStPos, scope_ptr, ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}
								break;

							}

						case INS_Code.ld_MultiNameL_Ref:
							{
								
								Ld_MulitNameL_Ref(dst_index, &PC, constants, stackslots, stackStPos, scope_ptr, methodscope, ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}

								break;

							}
						case INS_Code.ld_MultiName_Val:
							{
								Ld_MultiName_Val(dst_index,&PC, method,methodscope,constants,stackslots,stackStPos,scope_ptr,ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}

								break;
							}
						case INS_Code.ld_MultiNameL_Val:
							{
								

								Ld_MultiNameL_Val(dst_index,&PC, 
									  method, methodscope, stackslots, stackStPos, scope_ptr, ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}
								break;

							}						
						case INS_Code.store_MultiNameL:
							{

								Store_MultiNameL(dst_index,&PC, constants,  stackslots, stackStPos, scope_ptr, methodscope, ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}
								break;


							}
						case INS_Code.store_MultiName:
							{

								Store_MultiName(dst_index, &PC, constants, stackslots, stackStPos, scope_ptr, methodscope, ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}

								break;
							}
						case INS_Code.store_instanceMember:
							{
								Store_InstanceOrScopeMember(dst_index, &PC, stackslots, stackStPos, scope_ptr,methodscope , ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}
								break;
							}
						case INS_Code.ld_RTQNameL_Ref:
							{
								
								Ld_RTQNameL_Ref(dst_index,&PC, methodscope, stackslots, stackStPos, scope_ptr, ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}
								break;
							}
						case INS_Code.ld_InstanceOrScopeMemberValueRef:
							{
								

								Ld_InstanceOrScopeMemberValueRef( dst_index,&PC,stackslots, stackStPos, scope_ptr, ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}
							}
							break;
						case INS_Code.ld_instacneOrScopeMember_Val:
							{
								Ld_InstanceOrScopeMemberVal(dst_index, &PC, stackslots, stackStPos, scope_ptr,ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}
								break;
							}
						case INS_Code.ld_ScopeH:
							{
								Ld_ScopeH(dst_index, &PC, stackslots, methodscope,// scopeType,
									stackStPos);

								//NaNBoxing v = Ld_ScopeH(methodscope, heapLocater, scopeType, stackStPos + stackLocater.index);
								//stackslots[stackLocater.index] = v;
							}
							break;
						
						case INS_Code.ld_methodVariable:
							{
								//StackLocater stackLocater;
								//stackLocater.index = dst_index;

								//ScopeHeapLocater heapLocater = (*ScopeHeapLocater*)PC; PC += 4;
								ScopeHeapLocater* heapLocater = (ScopeHeapLocater*)PC; PC += 4;

								Debug.Assert(methodscope.Type._link_codescope.index == heapLocater->ScopeIndex);
								
								stackslots[dst_index] = ((RtMethodScope)methodscope).ReadSlot(heapLocater->MemberIndex);


							}
							break;
						
						case INS_Code.ld_ValueRef:
							{
								uint* opcodePtr = (uint*)PC - 1; Debug.Assert((*opcodePtr & 0xff) == (byte)INS_Code.ld_ValueRef);

								int sourc;
								//int target;
								LoadInt32(&sourc, &PC);
								//LoadStackLocater(&target, &PC);
								//target.index = dst_index;

								var refobj = stackslots[sourc];

								var v = refobj.HeapKind != (byte)RtHeapTypeKind.STACK_CACHE_OBJ ? refobj : LoadValue((RtStackCache)Context.GC.Heap[refobj.HeapPtr],
										 stackStPos - method.Body._link_codescope.Members.Count - 2, ref error, stackslots, stackStPos + dst_index);
								if (error.raised)
								{
									goto flag_handle_error;
								}

								stackslots[dst_index] = v;
							}
							break;

						case INS_Code.move:
							{
								int sourc;
								//StackLocater target;
								LoadInt32(&sourc, &PC);
								//LoadStackLocater(&target, &PC);
								//target.index = dst_index;

								stackslots[dst_index] = stackslots[sourc];

							}
							break;
						case INS_Code.ld_This:
							{
#if FORCOMPILER
								if (IsComputeConstExpr)
								{
									if (((RtMethodScope)methodscope).ThisPtr.ValueType == BoxType.Fault)
									{
										throw new EvalConstException();
									}
								}

#endif


								//StackLocater target;
								//target.index = dst_index;

								stackslots[dst_index] = ((RtMethodScope)methodscope).ThisPtr;

							}
							break;
						case INS_Code.ld_arguments:
							{
#if FORCOMPILER
								if (IsComputeConstExpr)
								{
									throw new EvalConstException();
								}

#endif
								//StackLocater target;
								//target.index = dst_index;

								int a_ptr = stackStPos - method.Body._link_codescope.Members.Count - 1
									- 2; /*
									      * arguments
									      * callee
									      */

								NaNBoxing arguments = Context.StackSlots[a_ptr];
#if DEBUG
								if (arguments.ValueType != NaNBoxing.BoxType.HeapPtr)
									throw new InvalidOperationException();
								if (Context.GC.Heap[arguments.HeapPtr].Kind != RtHeapTypeKind.ARRAY)
									throw new InvalidOperationException();
								if (((RtArray)Context.GC.Heap[arguments.HeapPtr]).StoreMode != RtArray.ArrayStoreMode.cache_on_stack)
									throw new InvalidOperationException();

#endif
								stackslots[dst_index] = arguments;

							}
							break;
						case INS_Code.ld_function:
							{

								Ld_function(dst_index, &PC, methodscope, constants, stackslots, scope_ptr, stackStPos, method_scopes, ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}
							}
							break;
						case INS_Code.ld_function_call: //此指令目标肯定是匿名函数，所以不需要考虑proto和动态属性问题。
							{

								Ld_function_call( &PC, dst_index, methodscope,constants, stackslots, 
									//scopeType, 
									scope_ptr, stackStPos, ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}
							}
							break;

						case INS_Code.ld_function_bindglobal_call:
							{
#if FORCOMPILER
								if (IsComputeConstExpr)
								{
									throw new EvalConstException();
								}

#endif

								Ld_function_bindglobal_call(&PC, methodscope, dst_index,constants, stackslots, scope_ptr, stackStPos, method_scopes,
									//ref global_obj, 
									ref error
									);
								if (error.raised)
								{
									goto flag_handle_error;
								}
							}
							break;
						case INS_Code.bindglobal_call:
							{
#if FORCOMPILER
								if (IsComputeConstExpr)
								{
									throw new EvalConstException();
								}

#endif
								Bindglobal_call(dst_index,&PC,(RtMethodScope)methodscope, stackslots, stackStPos, scope_ptr, 
									//ref global_obj,
									ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}
							}
							break;
						case INS_Code.bindthis_call:
							{								
								Bindthis_call(dst_index,&PC, methodscope,  stackslots, stackStPos, scope_ptr, ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}
							}
							break;

						case INS_Code.ld_supermethod:
							{
								Ld_supermethod(dst_index, &PC, methodscope, stackslots, constants, stackStPos);
							}
							break;
						case INS_Code.ld_method:
							{								
								Ld_method(dst_index,&PC, methodscope,  stackslots, scope_ptr, stackStPos, ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}
							}
							break;

						case INS_Code.ld_interface_method:
							{
								Ld_interface_method(dst_index, &PC, method.Body.heapConstants , stackslots, constants, stackStPos, ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}
							}
							break;
						case INS_Code.method_call:
							{
								//StackLocater target;
								//target.index = dst_index;

								//StackLocater function;
								//LoadStackLocater(&function, &PC);

								//int argsCount;
								//LoadInt32(&argsCount, &PC);

								////!!需要考虑对齐问题
								//byte* argementsPtr = PC;
								//PC += argsCount * 4;


								//Debug.Assert(stackslots[function.index].ValueType == NaNBoxing.BoxType.HeapPtr);


								//RtHeapBase _method_ = Context.GC.Heap[stackslots[function.index].HeapPtr];
								//RtClosure _methodclosure_ = (RtClosure)_method_;
								//NaNBoxing result = RunMethod(((ASMethodBody)_method_.Type).Method,
								//	_methodclosure_.This, _methodclosure_.ScopePtr, _methodclosure_.ScopeType, (ushort)argsCount, argementsPtr, stackslots, ref error, stackStPos + target.index, stackslots[function.index].HeapPtr);

								//if (error.raised)
								//{
								//	goto flag_handle_error;
								//}

								//stackslots[target.index] = result;

								M_Call(dst_index, &PC, (RtMethodScope)methodscope, stackslots, stackStPos, scope_ptr,ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}

							}
							break;
						case INS_Code.ld_length:
							{
#if FORCOMPILER
								if (IsComputeConstExpr)
								{
									throw new EvalConstException();
								}
#endif

								Ld_length(dst_index, &PC, stackslots,  ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}

							}
							break;
						case INS_Code.read_property:
							{
								Read_property(dst_index, &PC, methodscope, stackslots, stackStPos, scope_ptr, ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}
							}
							break;
						case INS_Code.read_property_interface:
							{
								Read_property_interface(dst_index, &PC, methodscope, constants, stackslots, stackStPos, ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}
							}
							break;
						case INS_Code.write_property:
							{
								Write_property(dst_index,&PC,methodscope,stackslots,stackStPos,scope_ptr,ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}

							}
							break;
						case INS_Code.write_property_interface:
							{
								Write_property_interface(dst_index, &PC, method.Body.heapConstants ,constants, stackslots, ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}
							}
							break;

						case INS_Code.storeScopeH:
							{

								StoreScopeH(dst_index,&PC, scope_ptr, methodscope, method_scopes, stackslots, 
									//scopeType,
									ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}
							}
							break;

						case INS_Code.storeMethodVariable:
							{
								StoreMethodVariable(dst_index, &PC, methodscope, stackslots, scope_ptr, method_scopes, ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}
							}
							break;

						case INS_Code.storeHeapValueRef:
							{
								StoreHeapValueRef(dst_index, &PC, methodscope, stackslots, stackStPos, scope_ptr, ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}
							}
							break;
						case INS_Code.ld_MethodVariableInitValue:
							{
								ScopeHeapLocater heapLocater;
								{
									heapLocater.ScopeIndex = *(ushort*)PC; PC += 2;
									heapLocater.MemberIndex = *(ushort*)PC; PC += 2;								
								}

								NaNBoxing value = *(NaNBoxing*)PC;PC += 8;

								var s = methodscope; //Context.GC.Heap[scope_ptr];
								
								if (value.ValueType == BoxType.HeapPtr)
								{
									ASTrait t = s.Type._link_codescope.Members[heapLocater.MemberIndex].trait;
									value = t.Value.initValue.Value;
								}

								stackslots[dst_index] = value;

								RtMethodScope heap = (RtMethodScope)s;
								ref NaNBoxing heapV = ref heap.ReadSlotRef(heapLocater.MemberIndex);

								if (value.ValueType != BoxType.HeapPtr && heapV.ValueType != BoxType.HeapPtr)
								{
									heapV = value;
								}
								else
								{
									int* m_scope = method_scopes;
									*m_scope++ = scope_ptr;

									PrepareSaveMethodScope(heap, heapLocater, ref value, null, method_scopes, ref error);
									Debug.Assert(!error.raised);
									heapV = value;
								}

#if FORCOMPILER
							((RtMethodScope)heap).SetSlot(value, heapLocater.MemberIndex);
#endif
							}
							break;
						case INS_Code.ld_memberInitValue:
							{
								
								Ld_memberInitValue( &PC, methodscope, method_scopes, scope_ptr, //scopeType,
																								ref error);
								Debug.Assert(!error.raised);

							}

							break;
						case INS_Code.new_instance:
							{
#if FORCOMPILER
								if (iscomputing_initvalue)
								{
									throw new EvalConstException();
								}
#endif

								NEW_INSTANCE( dst_index,&PC , stackStPos, scope_ptr, stackslots, //scopeType, 
									methodscope, ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}
							}
							break;
						case INS_Code.type_cast:
							{
								Type_cast(dst_index, &PC, methodscope, constants, stackslots, stackStPos, scope_ptr, ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}
							}
							break;
						case INS_Code.create_prop:
							{
								Create_prop(dst_index, &PC, stackslots, ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}
							}
							break;
						case INS_Code.super_ctor:
							{
								Super_ctor(&PC, methodscope, constants, stackslots, scope_ptr, ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}
							}
							break;
						case INS_Code.positive:
							{							
								POSITIVE( dst_index, &PC, methodscope, stackslots, stackStPos, scope_ptr, ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}
							}
							break;
						case INS_Code.neg:
							{
								
								NEG( dst_index,&PC, methodscope,  stackslots, stackStPos, scope_ptr, ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}

							}
							break;
						case INS_Code.add:
							{
								
								Exec_Add( dst_index, &PC ,ref error, scope_ptr, stackslots, stackStPos, ((RtMethodScope)methodscope).ThisPtr);
								if (error.raised)
								{
									goto flag_handle_error;
								}
							}
							break;
						//case INS_Code.short_add:
						//	{
						//		NaNBoxing n1 = stackslots[(int)((uint)dst_index >> 16 & 0xff)];
						//		NaNBoxing n2 = stackslots[(int)((uint)dst_index >> 8 & 0xff)];

						//		Exec_Add(ref error, n1, n2, new StackLocater() { index = (int)((uint)dst_index & 0xff) },
						//			scope_ptr,
						//			new StackLocater() { index = (int)((uint)dst_index >> 16 & 0xff) },
						//			stackslots, stackStPos, thisPtr);
						//		if (error.raised)
						//		{
						//			goto flag_handle_error;
						//		}
						//	}
						//	break;
						case INS_Code.sub:
							{

								
								Exec_Sub( dst_index,&PC, ref error, scope_ptr, stackslots, stackStPos, ((RtMethodScope)methodscope).ThisPtr);
								if (error.raised)
								{
									goto flag_handle_error;
								}

								break;
							}
						//case INS_Code.short_sub:
						//	{

						//		NaNBoxing n1 = stackslots[(int)((uint)dst_index >> 16 & 0xff)];
						//		NaNBoxing n2 = stackslots[(int)((uint)dst_index >> 8 & 0xff)];

						//		Exec_Sub(ref error, n1, n2, new StackLocater() { index = (int)((uint)dst_index & 0xff) },
						//			scope_ptr,
						//			new StackLocater() { index = (int)((uint)dst_index >> 16 & 0xff) },
						//			stackslots, stackStPos, thisPtr);
						//		if (error.raised)
						//		{
						//			goto flag_handle_error;
						//		}

						//	}
						//	break;
						case INS_Code.multiply:
							{
								
								Exec_Multiply( dst_index,&PC, ref error, scope_ptr,  stackslots, stackStPos, ((RtMethodScope)methodscope).ThisPtr);
								if (error.raised)
								{
									goto flag_handle_error;
								}
							}
							break;
						case INS_Code.div:
							{
								
								Exec_Division(dst_index,&PC, ref error, scope_ptr,  stackslots, stackStPos, ((RtMethodScope)methodscope).ThisPtr);
								if (error.raised)
								{
									goto flag_handle_error;
								}

							}
							break;
						case INS_Code.modulus:
							{
								
								Exec_Modulus( dst_index,&PC ,ref error,scope_ptr, stackslots, stackStPos, ((RtMethodScope)methodscope).ThisPtr);
								if (error.raised)
								{
									goto flag_handle_error;
								}
							}
							break;
						case INS_Code.bitwise:
							{
								
								Exec_bitWise( dst_index,&PC, ref error,scope_ptr,stackslots, stackStPos, ((RtMethodScope)methodscope).ThisPtr);
								if (error.raised)
								{
									goto flag_handle_error;
								}
							}
							break;
						case INS_Code.logic_comparison:
							{
								
								Exec_Comparse(dst_index,&PC,ref error,  scope_ptr,  stackslots, stackStPos, ((RtMethodScope)methodscope).ThisPtr);
								if (error.raised)
								{
									goto flag_handle_error;
								}
							}
							break;
						case INS_Code.logic_not:
							{
								//StackLocater dst;
								int src;

								//dst.index = dst_index;
								LoadInt32(&src, &PC);

								var v = stackslots[src];// LoadValue(stackslots[src.index], ref error, ref stackslots, stackStPos);
															  //if (error.raised)
															  //{
															  //    goto flag_handle_error;
															  //}

								ConvertValueType(ref error, v, TypeKind.Boolean, null, ref v);

#if DEBUG
								if (error.raised) throw new InvalidOperationException();//转Boolean，不可能失败
#endif

								stackslots[dst_index].SetBoolean(!v.Boolean);

							}
							break;
						case INS_Code.strict_eq:
							{
								//StackLocater dst;
								int v1;
								int v2;

								//dst.index = dst_index;
								LoadInt32(&v1, &PC);
								LoadInt32(&v2, &PC);


								stackslots[dst_index].SetBoolean(IsStrictlyEqual(stackslots[v1], stackslots[v2]));

							}
							break;
						//case INS_Code.short_strict_eq:
						//	{
						//		// return $"strict_Eq(===)(short)   [stack:{(uint)dst.index & 0xff}]<- [stack:{ (uint)dst.index>>16 & 0xff }] === [stack:{ (uint)dst.index>>8 & 0xff }]";
						//		stackslots[(int)((uint)dst_index & 0xff)].SetBoolean(
						//			IsStrictlyEqual(stackslots[(int)((uint)dst_index >> 16 & 0xff)], stackslots[(int)((uint)dst_index >> 8 & 0xff)]));
						//	}
						//	break;
						case INS_Code.strict_neq:
							{
								//StackLocater dst;
								int v1;
								int v2;

								//dst.index = dst_index;
								LoadInt32(&v1, &PC);
								LoadInt32(&v2, &PC);


								stackslots[dst_index].SetBoolean(!IsStrictlyEqual(stackslots[v1], stackslots[v2]));

							}
							break;
						case INS_Code.equal:
							{
								//StackLocater dst;
								int v1;
								int v2;

								//dst.index = dst_index;
								LoadInt32(&v1, &PC);
								LoadInt32(&v2, &PC);

								bool isEqual = IsEqual(stackslots[v1], stackslots[v2], dst_index, ref error, scope_ptr, stackslots, stackStPos, ((RtMethodScope)methodscope).ThisPtr);
								if (error.raised)
								{
									goto flag_handle_error;
								}
								stackslots[dst_index].SetBoolean(isEqual);

							}
							break;
						case INS_Code.not_equal:
							{
								//StackLocater dst;
								int v1;
								int v2;

								//dst.index = dst_index;
								LoadInt32(&v1, &PC);
								LoadInt32(&v2, &PC);

								bool isEqual = IsEqual(stackslots[v1], stackslots[v2], dst_index, ref error, scope_ptr, stackslots, stackStPos, ((RtMethodScope)methodscope).ThisPtr);
								if (error.raised)
								{
									goto flag_handle_error;
								}
								stackslots[dst_index].SetBoolean(!isEqual);
							}
							break;
						case INS_Code.get_in:
							{
								
								GET_IN( dst_index,&PC,  stackslots,  stackStPos, scope_ptr, methodscope, ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}
							}
							break;
						case INS_Code.get_typeof:
							{								
								GET_TYPEOF(dst_index,&PC, stackslots);
							}
							break;
						case INS_Code.get_is:
							{
								//StackLocater dst;
								StackLocater v1;
								StackLocater v2;

								//dst.index = dst_index;
								LoadStackLocater(&v1, &PC);
								LoadStackLocater(&v2, &PC);


								var type = stackslots[v2.index];
								if (type.ValueType != BoxType.HeapPtr)
								{
									RaiseTypeError(ref error, type, TypeKind.Class);
									goto flag_handle_error;
								}

								var obj = Context.GC.Heap[type.HeapPtr];
								if (obj.Kind != RtHeapTypeKind.CLASS)
								{
									RaiseTypeError(ref error, type, TypeKind.Class);
									goto flag_handle_error;
								}

								var typeclass = (ASClass)((RtScriptClass)obj).Meta;
								var v = stackslots[v1.index];

								stackslots[dst_index].SetBoolean(Is(v, typeclass));

							}
							break;
						case INS_Code.cast_as:
							{
								//StackLocater dst;
								int v1;
								int v2;

								//dst.index = dst_index;
								LoadInt32(&v1, &PC);
								LoadInt32(&v2, &PC);


								var type = stackslots[v2];
								if (type.ValueType != BoxType.HeapPtr)
								{
									RaiseTypeError(ref error, type, TypeKind.Class);
									goto flag_handle_error;
								}

								var obj = Context.GC.Heap[type.HeapPtr];
								if (obj.Kind != RtHeapTypeKind.CLASS)
								{
									RaiseTypeError(ref error, type, TypeKind.Class);
									goto flag_handle_error;
								}

								var typeclass = (ASClass)((RtScriptClass)obj).Meta;
								var v = stackslots[v1];

								bool v1isv2 = Is(v, typeclass);

								if (v1isv2)
								{
									stackslots[dst_index] = v;
								}
								else
								{
									stackslots[dst_index].SetNull();
								}
							}
							break;
						case INS_Code.increment_decrement:
							{
								Increment_decrement(dst_index, &PC, methodscope, stackslots, scope_ptr, stackStPos, ref error);
								if (error.raised)
								{ 
									goto flag_handle_error;
								}
							}
							break;
						case INS_Code.get_instanceof:
							{
								
								GET_INSTANCEOF(dst_index,&PC, stackslots, ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}
							}
							break;
						case INS_Code.O_ld_function_bindGlobal:
							{

#if FORCOMPILER
								if (IsComputeConstExpr)
								{
									throw new EvalConstException();
								}

#endif

								O_Ld_function_bindglobal(&PC, methodscope, dst_index, constants, stackslots, scope_ptr, stackStPos, method_scopes,
									//ref global_obj, 
									ref error
									);
								if (error.raised)
								{
									goto flag_handle_error;
								}


							}
							break;
						case INS_Code.O_ld_method:
							{
								O_ld_method(dst_index, &PC, methodscope, stackslots, scope_ptr,stackStPos);

								break;
							}
						case INS_Code.O_ld_interface_method:
							{
								O_Ld_interface_method(dst_index, &PC, method.Body.heapConstants, stackslots, constants, stackStPos);
								break;
							}
						case INS_Code.O_Call:
							{
								M_Call(dst_index, &PC, (RtMethodScope)methodscope, stackslots, stackStPos, scope_ptr, ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}
								break;
							}
						case INS_Code.O_IncrDecr_StoreVar:
							{
								IncrDecrStoreVar(dst_index, &PC, methodscope, stackslots, scope_ptr, stackStPos, method_scopes ,ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}

								break;
							}
						case INS_Code.O_NewStruct:
							{
								O_NewStruct(dst_index, &PC, stackStPos, stackslots,ref error);
								Debug.Assert(!error.raised);
								break;
							}
						case INS_Code.O_NewInstance_MethodVar:
							{
								O_NewInstance_Var(dst_index, &PC, stackStPos, scope_ptr, stackslots, methodscope, method_scopes ,ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}
								break;
							}
						case INS_Code.O_Ld_InstanceFiled:
							{
								O_Ld_InstanceField(dst_index,&PC,stackslots,stackStPos,scope_ptr,ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}
								break;
							}
						case INS_Code.O_StoreMethodVar_Instance:
							{
								O_StoreMethodVariable_Instance(dst_index, &PC, methodscope, stackslots, scope_ptr, method_scopes, ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}
								break;
							}
						case INS_Code.O_Ld_Array_Element:
							{
								O_Ld_ArrayElement(dst_index, &PC,
									  method, methodscope, stackslots, stackStPos, scope_ptr, ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}
								break;
								
							}
						case INS_Code.O_Store_Array_Element:
							{
								O_Store_ArrayElement(dst_index, &PC, stackslots, stackStPos, scope_ptr, methodscope, ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}
								break;
							}
						case INS_Code.O_Ld_Vector_Element:
							{
								O_Ld_VectorElement(dst_index, &PC,
									  method, methodscope, stackslots, stackStPos, scope_ptr, ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}
								break;

							}
						case INS_Code.O_Store_Vector_Element:
							{

								O_Store_VectorElement(dst_index, &PC, stackslots, stackStPos, scope_ptr, methodscope, ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}
								break;
							}
						case INS_Code.O_Ld_Indexer:
							{
								O_Ld_Indexer(dst_index, &PC,
									 stackslots, stackStPos, ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}
								break;
							}
						case INS_Code.O_Store_Indexer:
							{
								O_Store_Indexer(dst_index, &PC, stackslots, stackStPos, ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}

								break;
							}
						case INS_Code.O_Store_InstanceField:
							{
								O_Store_InstanceField(dst_index,&PC,stackslots,stackStPos,scope_ptr,methodscope,ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}
								break;
							}
						case INS_Code.add_Vec2_Vec2:
							{
								Exec_Add_Vec2_Vec2(dst_index, &PC, ref error, stackslots,stackStPos);
								if (error.raised)
								{
									goto flag_handle_error;
								}
								break;
							}
						case INS_Code.sub_Vec2_Vec2:
							{
								Exec_Sub_Vec2_Vec2(dst_index, &PC, ref error, stackslots, stackStPos);
								if (error.raised)
								{
									goto flag_handle_error;
								}
								break;
							}
						case INS_Code.scale_Vec2:
							{
								Exec_Scale_Vec2(dst_index, &PC, ref error, stackslots, stackStPos);
								if (error.raised)
								{
									goto flag_handle_error;
								}
								break;
							}
						case INS_Code.scale_Vec2_reciprocal:
							{
								Exec_Scale_Vec2_Reciprocal(dst_index, &PC, ref error, stackslots, stackStPos);
								if (error.raised)
								{
									goto flag_handle_error;
								}
								break;
							}
						case INS_Code.neg_pos_Vec2:
							{
								Exec_Neg_Pos_Vec2(dst_index, &PC, ref error, stackslots, stackStPos);
								if (error.raised)
								{
									goto flag_handle_error;
								}
								break;
							}
						case INS_Code.mul_Mat22_Vec2:
							{
								Exec_Mul_Mat22_Vec2(dst_index, &PC, ref error, stackslots, stackStPos);
								if (error.raised)
								{
									goto flag_handle_error;
								}
								break;
							}
						case INS_Code.mul_Mat22_Mat22:
							{
								Exec_Mul_Mat22_Mat22(dst_index, &PC, ref error, stackslots, stackStPos);
								if (error.raised)
								{
									goto flag_handle_error;
								}
								break;
							}
						case INS_Code.add_Mat22_Mat22:
							{
								Exec_Add_Mat22_Mat22(dst_index, &PC, ref error, stackslots, stackStPos);
								if (error.raised)
								{
									goto flag_handle_error;
								}
								break;
							}
						case INS_Code.if_logicOp_goto:
							{

								int compResult;LoadInt32(&compResult, &PC);							
								int offset;  LoadInt32(&offset,&PC); //br.ReadInt32();
								uint store;  LoadUInt(&store, &PC);

								var compMode = (CompMode)(store & 0xff);
								bool jump_mode = (store >> 8 & 0xff) > 0;
								byte v1 = (byte)(store >> 16 & 0xff);
								byte v2 = (byte)(store >> 24 & 0xff);

								if (compMode <= CompMode.strict_neq)
								{
									bool c = IsStrictlyEqual(stackslots[v1], stackslots[v2]);
									c = compMode == CompMode.strict_equal ? c : !c;
									stackslots[compResult].SetBoolean(c);
									if (c == jump_mode)
									{
										PC = PC_START + offset;
									}
								}								
								else if (compMode <= CompMode.notequal)
								{
									bool c = IsEqual(stackslots[v1], stackslots[v2], compResult, ref error, scope_ptr, stackslots, stackStPos, ((RtMethodScope)methodscope).ThisPtr);
									if (error.raised)
									{
										goto flag_handle_error;
									}

									c = compMode == CompMode.equal ? c : !c;

									stackslots[compResult].SetBoolean(c);
									if (c == jump_mode)
									{
										PC = PC_START + offset;
									}
								}								
								else
								{
									bool c = DoCompress((byte)(compMode - 4), compResult, stackslots[v1], stackslots[v2], scope_ptr, stackslots, stackStPos, ((RtMethodScope)methodscope).ThisPtr, ref error);
									if (error.raised)
									{
										goto flag_handle_error;
									}

									if (c == jump_mode)
									{
										PC = PC_START + offset;
									}

								}


								break;
							}


						case INS_Code.return_void:


							Context.StackSlots[returnSlotIndex].SetUndefined();

							if (exception_ctx != NO_TRY)
							{
								stackslots[exception_ctx->hold_error.index].setFault();//return 会吃掉异常

								ExceptionContext* ctx = NO_TRY + 1;
								ctx->FINALLY_JUMPTO_PTR = PC_END;
								do
								{
									var finally_p = ctx->state == 2 ? ctx->FINALLY_EXIT_PTR : ctx->FINALLY_PTR;
									++ctx;
									ctx->FINALLY_JUMPTO_PTR = finally_p;

								} while (ctx < exception_ctx);

								PC = exception_ctx->state == 2 ? exception_ctx->FINALLY_EXIT_PTR : exception_ctx->FINALLY_PTR;

								break;
							}
							else
							{
#if PROFILEPLAYER
								InstructionProfiler.Profile_ActionEnd(opcode);
#endif

								if (resume_state != null)
								{
									Debug.Assert(resume_state is PromiseImpl.AsyncGenWapper);
									resume_state.End();
								}

								goto flag_end;
							}
						case INS_Code.return_value:

							{
								Return_Value(dst_index, returnSlotIndex, method, stackslots, stackStPos, calleelastPos, scope_ptr, ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}


								if (exception_ctx != NO_TRY)
								{
									stackslots[exception_ctx->hold_error.index].setFault();//return 会吃掉异常

									ExceptionContext* ctx = NO_TRY + 1;
									ctx->FINALLY_JUMPTO_PTR = PC_END;
									do
									{
										var finally_p = ctx->state == 2 ? ctx->FINALLY_EXIT_PTR : ctx->FINALLY_PTR;
										++ctx;
										ctx->FINALLY_JUMPTO_PTR = finally_p;

									} while (ctx < exception_ctx);

									PC = exception_ctx->state == 2 ? exception_ctx->FINALLY_EXIT_PTR : exception_ctx->FINALLY_PTR;

									break;
								}
								else
								{
#if PROFILEPLAYER
									InstructionProfiler.Profile_ActionEnd(opcode);
#endif
									if (resume_state != null)
									{
										Debug.Assert(resume_state is PromiseImpl.AsyncGenWapper);
										resume_state.End();
									}
									goto flag_end;
								}
							}


						case INS_Code.goto_flag:
							{
								int flag_id = dst_index; //LoadInt32(&flag_id, &PC);
								int v; LoadInt32(&v, &PC);
								int trys = v >> 24;
								int offset = v & 0xffffff;

#if DEBUG
								if (trys < 0)
								{
									throw new InvalidOperationException();
								}
#endif

								if (trys == 0)
								{
									PC = PC_START + offset;
								}
								else
								{
									stackslots[exception_ctx->hold_error.index].setFault();// continue,break能吞掉异常？

									trys--;
									ExceptionContext* ex_cursor = exception_ctx - trys;
									ex_cursor->FINALLY_JUMPTO_PTR = PC_START + offset;

									while (ex_cursor != exception_ctx)
									{
										var finally_p = ex_cursor->state == 2 ? ex_cursor->FINALLY_EXIT_PTR : ex_cursor->FINALLY_PTR;
										++ex_cursor;
										ex_cursor->FINALLY_JUMPTO_PTR = finally_p;
									}

									PC = exception_ctx->state == 2 ? exception_ctx->FINALLY_EXIT_PTR : exception_ctx->FINALLY_PTR;

								}


							}
							break;
						case INS_Code.if_false_goto:
							{
								int flag_id = dst_index; //LoadInt32(&flag_id, &PC);
								int offset; LoadInt32(&offset, &PC);
								StackLocater condition;
								LoadStackLocater(&condition, &PC);

								NaNBoxing v = stackslots[condition.index];


								if (!ToBoolean(v))
								{
									PC = PC_START + offset;
								}

							}
							break;
						case INS_Code.if_true_goto:
							{
								int flag_id = dst_index; //LoadInt32(&flag_id, &PC);
								int offset; LoadInt32(&offset, &PC);
								StackLocater condition;
								LoadStackLocater(&condition, &PC);

								NaNBoxing v = stackslots[condition.index];
								//								ConvertValueType(ref error, v, TypeKind.Boolean, Context.BOOLEAN, ref v);
								//#if DEBUG
								//								if (error.raised) throw new InvalidOperationException();
								//								if (v.ValueType != BoxType.Boolean) throw new InvalidOperationException();

								//#endif
								if (ToBoolean(v))
								{
									PC = PC_START + offset;
								}
							}
							break;
						case INS_Code.array_vector_initelement:
							{
#if FORCOMPILER
								if (iscomputing_initvalue)
								{
									throw new EvalConstException();
								}
#endif

								Array_vector_initelement(dst_index, &PC, stackslots, ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}
							}

							break;

						case INS_Code.yield_break:
							{
#if FORCOMPILER
								if (IsComputeConstExpr)
								{
									throw new EvalConstException();
								}
#endif

								if (exception_ctx != NO_TRY)
								{
									Debug.Assert(exception_ctx->state == 0); // yield只能在try中发出
									Debug.Assert(stackslots[exception_ctx->hold_error.index].ValueType == BoxType.Fault);

									//stackslots[exception_ctx->hold_error.index].setFault();//这里反正也肯定是Fault

									ExceptionContext* ctx = NO_TRY + 1;
									ctx->FINALLY_JUMPTO_PTR = PC_END;
									do
									{
										Debug.Assert(ctx->state == 0); // yield只能在try中发出

										var finally_p = ctx->FINALLY_PTR;
										++ctx;

										ctx->FINALLY_JUMPTO_PTR = finally_p;

									} while (ctx < exception_ctx);

									PC = exception_ctx->FINALLY_PTR;

									break;
								}
								else
								{
#if PROFILEPLAYER
								InstructionProfiler.Profile_ActionEnd(opcode);
#endif
									resume_state.End();
									goto flag_end;
								}
							}

						case INS_Code.yield_return:
							{							
#if FORCOMPILER
								if (IsComputeConstExpr)
								{
									throw new EvalConstException();
								}
#endif

								Debug.Assert(returnSlotIndex >= 0);

								int refPC = 0;
								Yield_return(dst_index, (int)(PC - PC_START), method, stackslots, stackStPos, scope_ptr, 
									(int)(exception_ctx - exception_ctx_stack), NO_TRY, (GeneratorImpl.GeneratorWapper)resume_state,
									returnSlotIndex, calleelastPos, ref error, ref refPC
									);
								if (error.raised)
								{
									goto flag_handle_error;
								}

								PC_PTR = refPC;
								//中断运行
								return;
							}

						case INS_Code.await_return:
							{
#if FORCOMPILER
								if (IsComputeConstExpr)
								{
									throw new EvalConstException();
								}
#endif
								
								Debug.Assert(returnSlotIndex >= 0);

								int refPC = 0;
								Await_return(dst_index, (int)(PC - PC_START), method, stackslots, stackStPos, scope_ptr,
									(int)(exception_ctx - exception_ctx_stack), NO_TRY, (PromiseImpl.AsyncGenWapper)resume_state,
									returnSlotIndex, calleelastPos, ref error, ref refPC
									);
								if (error.raised)
								{
									goto flag_handle_error;
								}

								PC_PTR = refPC;
								//中断运行
								return;


							}
						case INS_Code.await_resume:
							{
#if FORCOMPILER
								if (IsComputeConstExpr)
								{
									throw new EvalConstException();
								}
#endif
								PromiseImpl.AsyncGenWapper state = (PromiseImpl.AsyncGenWapper)resume_state;

								if (state.isrejected)
								{
									error.raised = true;
									error.error = state.rejected_value;
									goto flag_handle_error;

								}
								else
								{
									stackslots[dst_index] = state.resolved_value;
								}
							}
							break;
						case INS_Code.iter_initctx:
							{
								ITER_INITCTX(dst_index, &PC, methodscope, ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}
							}
							break;
						case INS_Code.iter_get:
							{
								ITER_GET( dst_index, &PC, methodscope,stackslots, method_scopes, scope_ptr, ref error,
									PC_START);

								if (error.raised)
								{
									goto flag_handle_error;
								}

								break;
							}
						case INS_Code.iter_next:
							{
							
								ITER_NEXT( dst_index, &PC, methodscope, stackStPos,stackslots,  PC_START , ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}

								break;
							}
						case INS_Code.iter_close:
							{
								

								ITER_CLOSE( dst_index,&PC, methodscope,  stackslots, exception_ctx, ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}
								break;
							}
						case INS_Code.throw_error:
							{
								StackLocater err;
								err.index = dst_index;

								error.raised = true;
								error.error = stackslots[err.index];

								goto flag_handle_error;

							}
						case INS_Code.try_enter:
							{
								exception_ctx++;

								int finallypc;
								LoadInt32(&finallypc, &PC);

								int finally_exit_pc;
								LoadInt32(&finally_exit_pc, &PC);

								StackLocater hold_error;
								hold_error.index = dst_index;

								int catch_count;
								LoadInt32(&catch_count, &PC);

								exception_ctx->catch_count = catch_count;
								exception_ctx->hold_error = hold_error;
								exception_ctx->CATCH = PC;
								exception_ctx->FINALLY_PTR = PC_START + finallypc;
								exception_ctx->FINALLY_EXIT_PTR = PC_START + finally_exit_pc;
								exception_ctx->state = 0;
								exception_ctx->FINALLY_JUMPTO_PTR = null;

								PC += catch_count * 4;

								stackslots[exception_ctx->hold_error.index].setFault();

								break;
							}
						case INS_Code.try_exit:
							{
								PC = exception_ctx->FINALLY_PTR;

								break;
							}
						case INS_Code.catch_enter:
							{
								exception_ctx->state = 1;
								PC += 4;
								break;
							}
						case INS_Code.catch_exit:
							{
								/*
								 由于这种代码的存在，不能清理变量。
								var probe;
									try {
									  throw 'inside';
									} catch (x) {
									  probe = 
									  function () {
										  return x; 
									  };
									}

									trace( probe());  
								 */

								//								//清理catch变量
								//								var s = methodscope; //Context.GC.Heap[scope_ptr];
								//								int* m_scope = method_scopes;
								//								*m_scope++ = scope_ptr;

								//								var memberindex = exception_ctx->catched_error.MemberIndex;
								//								ASTrait t = s.Type._link_codescope.Members[memberindex].trait;
								//								RtPayloadMethodScope heap = (RtPayloadMethodScope)s;
								//								NaNBoxing value = default;

								//								ReceiveError store_err = default;
								//								PrepareSaveMethodScope(heap, ref exception_ctx->catched_error, ref value, m_scope, method_scopes, ref store_err);
								//								if (store_err.raised)
								//								{
								//									error.error.setFault();
								//#if PROFILEPLAYER
								//									InstructionProfiler.Profile_ActionEnd(opcode);
								//#endif
								//									goto flag_end;
								//								}
								//								heap.SetSlot(value, memberindex);

								PC = exception_ctx->FINALLY_PTR;//跳转到Finally

								break;
							}
						case INS_Code.finally_enter:
							{
								exception_ctx->state = 2;

								break;
							}
						case INS_Code.finally_exit:
							{
								NaNBoxing load_error = stackslots[exception_ctx->hold_error.index];
								if (load_error.ValueType != NaNBoxing.BoxType.Fault)
								{
									error.raised = true;
									error.error = load_error;

									exception_ctx--;
									goto flag_handle_error;
								}
								else
								{
									if (exception_ctx->FINALLY_JUMPTO_PTR != null)
									{
										PC = exception_ctx->FINALLY_JUMPTO_PTR; //--跳到指定位置		
									}
									exception_ctx--;
								}
								break;
							}

						case INS_Code.expression_barrier:
							{
								int argsCount;
								LoadInt32(&argsCount, &PC);
								PC += argsCount * 4;
							}

							break;

						//case INS_Code.Q_LD_ARR:
						//	{
								
						//		uint* opcodePtr = (uint*)PC - 1; Debug.Assert((*opcodePtr & 0xff) == (byte)INS_Code.Q_LD_ARR);
						//		StackLocater stack;
						//		stack.index = dst_index;

						//		StackLocater src;
						//		LoadStackLocater(&src, &PC);
						//		StackLocater _name;
						//		LoadStackLocater(&_name, &PC);

						//		StackLocater refholder_index;
						//		LoadStackLocater(&refholder_index, &PC);


						//		var instance_box = stackslots[src.index];
						//		var name_box = stackslots[_name.index];

						//		Debug.Assert(src.index >= 0);

						//		uint name_box_index = name_box.UIntValue;
						//		var name_box_type = name_box.ValueType;


						//		if (!(
						//			instance_box.HeapKind == (byte)RtHeapTypeKind.ARRAY
						//			&& name_box_type >= BoxType.Int && name_box_type <= BoxType.UShort &&
						//			((int)name_box_index >= 0 || (name_box_type == BoxType.Uint && name_box_index < uint.MaxValue)))
						//			)
						//		{
						//			*opcodePtr = ((uint)INS_Code.ld_MultiNameL_Val | (0xffffff00 & (*opcodePtr)));
						//			PC = (byte*)(opcodePtr + 1);
						//			opcode = INS_Code.ld_MultiNameL_Val;
						//			goto lbl_retry;
						//		}

						//		uint array_i = name_box_index; //name_box.ValueType == BoxType.Uint ? name_box.UIntValue : (uint)name_box.IntValue;

						//		bool isoutofindex_or_ishole;
						//		var a_element = LoadSlotFromArray(array_i, Context.GC.Heap[instance_box.HeapPtr], out isoutofindex_or_ishole);

						//		if (a_element.ValueType == BoxType.Fault)
						//		{
						//			a_element.SetUndefined();
						//		}
						//		else if (a_element.IsStruct())//v.ValueType == BoxType.HeapPtr && v.HeapKind == (byte)RtHeapTypeKind.INSTANCE && v.HeapFlag &)
						//		{
						//			a_element.SetHeapPtr(a_element.HeapPtr, (byte)RtHeapTypeKind.INSTANCE, (byte)(HeapKindFlag.FLAG_STRUCT | HeapKindFlag.FLAG_REFSTRUCT));
						//		}

						//		stackslots[dst_index] = a_element;


						//		break;
							
						//	}
							
						case INS_Code.END:
#if PROFILEPLAYER
							InstructionProfiler.Profile_ActionEnd(opcode);
#endif
							if (resume_state != null)
							{
								resume_state.End();
							}
							goto flag_end;
#if DEBUG
						default:
							throw new NotImplementedException($"{opcode} not implemented");
#endif
					}


#if PROFILEPLAYER
					InstructionProfiler.Profile_ActionEnd(opcode);
#endif
					continue;

				flag_handle_error:;

					if (error.error.ValueType != BoxType.Fault && exception_ctx != NO_TRY)
					{
						if (exception_ctx->state == 0) // try中
						{

							byte* c = exception_ctx->CATCH;
							for (int i = 0; i < exception_ctx->catch_count; i++)
							{
								int catch_enter_p;
								LoadInt32(&catch_enter_p, &c);

								byte* catch_enter = PC_START + catch_enter_p;

								catch_enter += 4;

								ScopeHeapLocater heapLocater;
								{
									heapLocater.ScopeIndex = *(ushort*)catch_enter; catch_enter += 2;
									heapLocater.MemberIndex = *(ushort*)catch_enter; catch_enter += 2;
									//byte* _p = (byte*)&heapLocater.ScopeIndex;
									//*_p++ = *catch_enter++;
									//*_p = *catch_enter++;

									//_p = (byte*)&heapLocater.MemberIndex;
									//*_p++ = *catch_enter++;
									//*_p = *catch_enter++;
								}
								var s = methodscope; //Context.GC.Heap[scope_ptr];
								int* m_scope = method_scopes;
								*m_scope++ = scope_ptr;
#if DEBUG
								if (s.Type._link_codescope.index != heapLocater.ScopeIndex)
								{
									throw new InvalidOperationException();
								}
#endif
								//将捕获到的error保存到变量中.
								ASTrait t = s.Type._link_codescope.Members[heapLocater.MemberIndex].trait;

								bool match = false;
								#region 捕获类型匹配
								switch (t.TypeKind)
								{
									case TypeKind.Any:
										match = true;
										break;
									case TypeKind.Boolean:
										match = error.error.ValueType == NaNBoxing.BoxType.Boolean;
										break;
									case TypeKind.SByte:
										match = error.error.ValueType == NaNBoxing.BoxType.Sbyte ||
											(error.error.ValueType == BoxType.Byte && error.error.ByteValue <= sbyte.MaxValue) ||
											(error.error.ValueType == BoxType.Short && error.error.ShortValue <= sbyte.MaxValue && error.error.ShortValue >= sbyte.MinValue) ||
											(error.error.ValueType == BoxType.UShort && error.error.UShortValue <= sbyte.MaxValue) ||
											(error.error.ValueType == BoxType.Int && error.error.IntValue <= sbyte.MaxValue && error.error.IntValue >= sbyte.MinValue) ||
											(error.error.ValueType == BoxType.Uint && error.error.UIntValue <= sbyte.MaxValue) ||
											(error.error.ValueType == BoxType.Float
											&&
											MathF.Truncate(error.error.FloatValue) == error.error.FloatValue
											&&
											error.error.FloatValue >= sbyte.MinValue && error.error.FloatValue <= sbyte.MaxValue
											)
											||
											(error.error.ValueType == BoxType.Number
											&&
											Math.Truncate(error.error.Number) == error.error.Number
											&&
											error.error.Number >= sbyte.MinValue && error.error.Number <= sbyte.MaxValue
											)
											;
										break;
									case TypeKind.Byte:
										match = error.error.ValueType == NaNBoxing.BoxType.Byte ||
											(error.error.ValueType == BoxType.Sbyte && error.error.SByteValue >= byte.MinValue) ||
											(error.error.ValueType == BoxType.Short && error.error.ShortValue <= byte.MaxValue && error.error.ShortValue >= byte.MinValue) ||
											(error.error.ValueType == BoxType.UShort && error.error.UShortValue <= byte.MaxValue) ||
											(error.error.ValueType == BoxType.Int && error.error.IntValue <= byte.MaxValue && error.error.IntValue >= byte.MinValue) ||
											(error.error.ValueType == BoxType.Uint && error.error.UIntValue <= byte.MaxValue) ||
											(error.error.ValueType == BoxType.Float
											&&
											MathF.Truncate(error.error.FloatValue) == error.error.FloatValue
											&&
											error.error.FloatValue >= byte.MinValue && error.error.FloatValue <= byte.MaxValue
											)
											||
											(error.error.ValueType == BoxType.Number
											&&
											Math.Truncate(error.error.Number) == error.error.Number
											&&
											error.error.Number >= byte.MinValue && error.error.Number <= byte.MaxValue
											)
											;
										break;
									case TypeKind.Short:
										match =
											error.error.ValueType == NaNBoxing.BoxType.Short ||
											error.error.ValueType == BoxType.Byte ||
											error.error.ValueType == BoxType.Sbyte ||
											(error.error.ValueType == BoxType.UShort && error.error.UIntValue <= short.MaxValue) ||
											(error.error.ValueType == BoxType.Int && error.error.IntValue <= short.MaxValue && error.error.IntValue >= short.MinValue) ||
											(error.error.ValueType == BoxType.Uint && error.error.UIntValue <= short.MaxValue) ||
											(error.error.ValueType == BoxType.Float
											&&
											MathF.Truncate(error.error.FloatValue) == error.error.FloatValue
											&&
											error.error.FloatValue >= short.MinValue && error.error.FloatValue <= short.MaxValue
											)
											||
											(error.error.ValueType == BoxType.Number
											&&
											Math.Truncate(error.error.Number) == error.error.Number
											&&
											error.error.Number >= short.MinValue && error.error.Number <= short.MaxValue
											)
											||
											(error.error.ValueType == BoxType.Number
											&&
											Math.Truncate(error.error.Number) == error.error.Number
											&&
											error.error.Number >= short.MinValue && error.error.Number <= short.MaxValue
											)
											;
										break;
									case TypeKind.UShort:
										match = error.error.ValueType == NaNBoxing.BoxType.UShort ||
											(error.error.ValueType == BoxType.Sbyte && error.error.SByteValue >= ushort.MinValue) ||
											(error.error.ValueType == BoxType.Short && error.error.ShortValue >= ushort.MinValue) ||
											(error.error.ValueType == BoxType.Byte) ||
											(error.error.ValueType == BoxType.Int && error.error.IntValue <= ushort.MaxValue && error.error.IntValue >= ushort.MinValue) ||
											(error.error.ValueType == BoxType.Uint && error.error.UIntValue <= ushort.MaxValue) ||
											(error.error.ValueType == BoxType.Float
												&&
												MathF.Truncate(error.error.FloatValue) == error.error.FloatValue
												&&
												error.error.FloatValue >= ushort.MinValue && error.error.FloatValue <= ushort.MaxValue
											)
											||
											(error.error.ValueType == BoxType.Number
												&&
												Math.Truncate(error.error.Number) == error.error.Number
												&&
												error.error.Number >= ushort.MinValue && error.error.Number <= ushort.MaxValue
											)
											;
										break;
									case TypeKind.Int:
										match = error.error.ValueType == NaNBoxing.BoxType.Int ||
											error.error.ValueType == NaNBoxing.BoxType.UShort ||
											error.error.ValueType == NaNBoxing.BoxType.Short ||
											error.error.ValueType == NaNBoxing.BoxType.Sbyte ||
											error.error.ValueType == NaNBoxing.BoxType.Byte ||
											(error.error.ValueType == BoxType.Number
												&&
												Math.Truncate(error.error.Number) == error.error.Number
												&&
												error.error.Number >= int.MinValue && error.error.Number <= int.MaxValue
											)
											||
											(error.error.ValueType == BoxType.Float
												&&
												MathF.Truncate(error.error.FloatValue) == error.error.FloatValue
												&&
												error.error.FloatValue >= -16777216 && error.error.FloatValue <= 16777216
											/*
											 * 32 位浮点数（单精度浮点数，IEEE 754 标准）能精确表达的整数范围是 -2²⁴ 到 2²⁴（即 -16777216 到 16777216）。
											 * */
											)

											;
										break;
									case TypeKind.Uint:
										match = error.error.ValueType == NaNBoxing.BoxType.Uint ||
											error.error.ValueType == NaNBoxing.BoxType.UShort ||
											error.error.ValueType == NaNBoxing.BoxType.Byte ||
											(error.error.ValueType == BoxType.Int && error.error.IntValue >= 0) ||
											(error.error.ValueType == BoxType.Short && error.error.ShortValue >= 0) ||
											(error.error.ValueType == BoxType.Sbyte && error.error.SByteValue >= 0) ||
											(error.error.ValueType == BoxType.Number
											&&
											Math.Truncate(error.error.Number) == error.error.Number
											&&
											error.error.Number >= uint.MinValue && error.error.Number <= uint.MaxValue
											)
											||
											(error.error.ValueType == BoxType.Float
											&&
											MathF.Truncate(error.error.FloatValue) == error.error.FloatValue
											&&
											error.error.FloatValue >= 0 && error.error.FloatValue <= 16777216
											/*
											 * 32 位浮点数（单精度浮点数，IEEE 754 标准）能精确表达的整数范围是 -2²⁴ 到 2²⁴（即 -16777216 到 16777216）。
											 * */
											)
											;

										break;
									case TypeKind.Float:
										{

											match = error.error.ValueType == NaNBoxing.BoxType.Float ||
												error.error.ValueType == NaNBoxing.BoxType.UShort ||
												error.error.ValueType == NaNBoxing.BoxType.Short ||
												error.error.ValueType == NaNBoxing.BoxType.Sbyte ||
												error.error.ValueType == NaNBoxing.BoxType.Byte ||
												(error.error.ValueType == BoxType.Int && error.error.IntValue >= -16777216 && error.error.IntValue <= 16777216) ||
												(error.error.ValueType == BoxType.Uint && error.error.UIntValue <= 16777216) ||
												(error.error.ValueType == BoxType.Number && Extensions.CanConvertToFloatLossless(error.error.Number))
												;
											break;
										}
									case TypeKind.Number:
										match =
											error.error.ValueType == NaNBoxing.BoxType.Number ||
											error.error.ValueType == NaNBoxing.BoxType.Int ||
											error.error.ValueType == NaNBoxing.BoxType.Uint ||
											error.error.ValueType == NaNBoxing.BoxType.Float ||
											error.error.ValueType == NaNBoxing.BoxType.UShort ||
											error.error.ValueType == NaNBoxing.BoxType.Short ||
											error.error.ValueType == NaNBoxing.BoxType.Sbyte ||
											error.error.ValueType == NaNBoxing.BoxType.Byte
											;
										break;
									case TypeKind.Fun_Void:
									case TypeKind.TraitDataReference:
									case TypeKind.RTQName_MultiName_DataReference:
									case TypeKind.CParseNS_Traits:
									case TypeKind.RTQNameRTQNameL_N:
									case TypeKind.SearchNameSpaceFromImports:
									case TypeKind.Unknown:
									case TypeKind.Super:
									case TypeKind.Null:
										break;
									case TypeKind.Object:
										//捕获非null,非undefined的任意对象
										match = (error.error.ValueType != NaNBoxing.BoxType.Undefined && error.error.ValueType != NaNBoxing.BoxType.Null);
										break;
									case TypeKind.Class:
										match = (error.error.ValueType == NaNBoxing.BoxType.HeapPtr && error.error.HeapKind == (byte)RtHeapTypeKind.CLASS);
										break;
									case TypeKind.String:
										match = (error.error.ValueType == NaNBoxing.BoxType.HeapPtr && error.error.HeapKind == (byte)RtHeapTypeKind.STRING);
										break;
									case TypeKind.Function:
										match = (error.error.ValueType == NaNBoxing.BoxType.HeapPtr && error.error.HeapKind == (byte)RtHeapTypeKind.CLOSURE);
										break;
									case TypeKind.Array:
										match = (error.error.ValueType == NaNBoxing.BoxType.HeapPtr && error.error.HeapKind == (byte)RtHeapTypeKind.ARRAY);
										break;
									case TypeKind.Vector:
										match = (error.error.ValueType == NaNBoxing.BoxType.HeapPtr && error.error.HeapKind == (byte)RtHeapTypeKind.VECTOR);
										break;
									case TypeKind.Namespace:
										match = (error.error.ValueType == NaNBoxing.BoxType.HeapPtr && error.error.HeapKind == (byte)RtHeapTypeKind.NAMESPACE);
										break;
									default:
										var check_type = t.__rt_type_class__; //Context.dictTypes[(ulong)c_type];
										if (error.error.ValueType == NaNBoxing.BoxType.HeapPtr)
										{

											if (error.error.HeapKind == (byte)RtHeapTypeKind.INSTANCE) //只有对象实例才可能满足条件。
											{
												var obj = Context.GC.Heap[error.error.HeapPtr];
												ASClass valuetype = ((ASInstance)obj.Type)._link_codescope.TypeLayout.ASType;
												if (valuetype.Type_identifier == (ulong)t.TypeKind)
												{
													match = true;
													break;
												}
												if (valuetype.Instance.IsExtend(check_type.Instance))
												{
													match = true;
													break;
												}
												if (valuetype.Instance.IsImplements(check_type.Instance))
												{
													match = true;
												}
											}
										}
										break;
								}
								#endregion

								if (match)
								{

									//exception_ctx->catched_error = heapLocater;
									RtMethodScope heap = (RtMethodScope)s;
									NaNBoxing value = default;

									ReceiveError store_err = default;
									ConvertValueType(ref store_err, error.error, t.TypeKind, t.__rt_type_class__, ref value);
#if DEBUG
									if (store_err.raised)
									{
										throw new InvalidOperationException(); // 这里的类型转换是不会失败的
									}
#endif
									PrepareSaveMethodScope(heap,  heapLocater, ref value, null, method_scopes, ref store_err);
									if (store_err.raised)
									{
										error.error.setFault();
#if PROFILEPLAYER
										InstructionProfiler.Profile_ActionEnd(opcode);
#endif
										goto flag_end;
									}
									heap.SetSlot(value, heapLocater.MemberIndex);

									error.raised = false;
									error.error = default;
									Context.errorStack.Clear();

									//进入catch块
									PC = PC_START + catch_enter_p;
									goto flag_hasintocatch;
								}
							}

							//未找到,进入finally块。	
							if (error.error.ValueType != BoxType.HeapPtr)
							{
								stackslots[exception_ctx->hold_error.index] = error.error; //异常信息暂存入hold_error;
							}
							else
							{
								StoreReturnSlot(ref stackslots[exception_ctx->hold_error.index], stackStPos, stackStPos + exception_ctx->hold_error.index, calleelastPos, scope_ptr, error.error, ref error, true);
							}

							error.raised = false;
							error.error = default;

							//跳转到finally
							PC = exception_ctx->FINALLY_PTR;
							continue;

						}
						else if (exception_ctx->state == 1)
						{
							//无法catch,跳转到finally.
							if (error.error.ValueType != BoxType.HeapPtr)
							{
								stackslots[exception_ctx->hold_error.index] = error.error; //异常信息暂存入hold_error;
							}
							else
							{
								StoreReturnSlot(ref stackslots[exception_ctx->hold_error.index], stackStPos, stackStPos + exception_ctx->hold_error.index, calleelastPos, scope_ptr, error.error, ref error, true);
							}

							error.raised = false;
							error.error = default;

							//跳转到finally
							PC = exception_ctx->FINALLY_PTR;
							continue;

						}
						else
						{
							exception_ctx--; //跳出本层try
							goto flag_handle_error;
						}
					}

					break;

				flag_hasintocatch:
					continue;
				}

			flag_end:
				;

				PC_PTR = (int)(PC - PC_START);
			}


#if DEBUG
			if ((error.raised && error.error.ValueType != BoxType.Fault) || !error.raised)
			{

				if (iter_ctx_index != Context.GC.IterCtxIndex)
				{
					throw new InvalidOperationException();
				}

			}
#endif


		}


	}
}
