using juicescript.ABC;
using juicescript.ABC.INS;
using juicescript.ABC.Locaters;
using juicescript.runtime.buildin;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Diagnostics.Metrics;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters;
using System.Security.AccessControl;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static juicescript.ABC.INS.INS_If_LogicOp_Goto;
//using static juicescript.ABC.INS.INS_Op_stack_Var_ldConst;
using static juicescript.NaNBoxing;


namespace juicescript.runtime
{
	public partial class Player
	{
		public struct ReceiveError
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


		public void CheckRequires()
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
					var nsPublic = swc.Namespaces.FirstOrDefault(n => n !=null && n.Kind == NamespaceKind.Package && n.Name == "" && n.def_uri == null);
					var nsAS3 = swc.Namespaces.FirstOrDefault(n => n !=null && n.Kind == NamespaceKind.PackageInternal && n.Name == ":AS3" && n.def_uri == "http://adobe.com/AS3/2006/builtin");

					if(nsPublic !=null && nsAS3 != null)
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


		public static void ComputeTypeSize_Align(ASTrait t, out int align, out int size)
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
		public static void ComputeCodeScope(ASScript script, ASNamespaceSet[] namespaceSets)
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
									__rt_type_class__ = m.__rt_type_class__
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
		public void ComputeVTable(ASScript script)
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
		public void ComputeInterface(ASScript script)
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

		private unsafe void linkConstantValue(NaNBoxing* b, SWCFile swc)
		{
			if (b->ValueType == NaNBoxing.BoxType.HeapPtr)
			{
				ASMethodBody.PoolHeapPtrKind kind = (ASMethodBody.PoolHeapPtrKind)(b->HeapPtr >> 24);
				if (kind == ASMethodBody.PoolHeapPtrKind.String)
				{
					if (swc.runtime_alloced_strings[b->HeapPtr & 0xffffff].ValueType == NaNBoxing.BoxType.HeapPtr)
					{
						b->SetHeapPtr(swc.runtime_alloced_strings[b->HeapPtr & 0xffffff].HeapPtr);
					}
					else
					{
						string str = swc.const_strings[b->HeapPtr & 0xffffff];
						int index = Context.GC.AllocString(str); if (index == 0) { throw new LoaderException("alloc string failed,out of memory"); }
						;
						swc.runtime_alloced_strings[b->HeapPtr & 0xffffff].SetHeapPtr(index);

						b->SetHeapPtr(index);
						Context.GC.Root.Add(Context.GC.Heap[index]);

					}
				}
				else if (kind == ASMethodBody.PoolHeapPtrKind.LD_Class)
				{
					ulong id = swc.ld_classid[b->HeapPtr & 0xffffff];
					ASClass @class = Context.dictTypes[id];

					if (!Context.link_const_class.Contains(@class))
					{
						Context.link_const_class.Add(@class);
					}

					int index = Context.link_const_class.IndexOf(@class);
					b->SetUInt((uint)index);

				}
				else if (kind == ASMethodBody.PoolHeapPtrKind.Namespace)
				{
					ASNamespace @namespace = swc.Namespaces[b->HeapPtr & 0xffffff];

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
						b->SetHeapPtr(index);

						@namespace.__instance_index__ = index;

						Context.GC.Root.Add(Context.GC.Heap[index]);

					}
					else
					{
						b->SetHeapPtr(@namespace.__instance_index__);
					}
				}
				else if (kind == ASMethodBody.PoolHeapPtrKind.VectorDef)
				{
					int index = b->HeapPtr & 0xffffff;
					var v = swc.Vectors[index];

					MakeVectorType(v);

					int newindex = Context.Vectors.IndexOf(v);
					b->SetInt(newindex);

				}
				else if (kind == ASMethodBody.PoolHeapPtrKind.Method)
				{
					int index = b->HeapPtr & 0xffffff;
					var m = swc.Methods[index];

					if (!Context.link_const_methods.Contains(m))
					{
						Context.link_const_methods.Add(m);
					}

					int m_index = Context.link_const_methods.IndexOf(m);
					b->SetUInt((uint)m_index);

				}
				else if (kind == ASMethodBody.PoolHeapPtrKind.SuperMethod)
				{
					int index = b->HeapPtr & 0xffffff;
					var superconfig = swc.ld_supermethods[index];
					ASClass @class = Context.dictTypes[superconfig.Item1];

					var m = @class.Instance._vtable.Items[superconfig.Item2];

					if (!Context.link_const_vtableitems.Contains(m))
					{
						Context.link_const_vtableitems.Add(m);
					}

					int m_index = Context.link_const_vtableitems.IndexOf(m);
					b->SetUInt((uint)m_index);


				}
				else
				{
					throw new InvalidOperationException();
				}
			}

		}

		private void linkMethod(ASMethod method, SWCFile swc)
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
							linkConstantValue(b, swc);
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

			if (method.ReturnTypeKind != TypeKind.Fun_Void && method.ReturnTypeKind != TypeKind.Any)
			{
				method.__return_type_class__ = Context.dictTypes[(ulong)method.ReturnTypeKind];
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
					else if(cls !=null && cls.QName.Name == "Promise")
					{
						Context.PROMISE = cls;
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

					if (layout.Size <= RtPayloadInstance.MAX_CACHEABLE_SIZE &&
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

					if (layout.Size > RtPayloadInstance.MAX_CACHEABLE_SIZE && cls.Instance.Flags.HasFlag(ClassFlags.Struct))
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
									unsafe
									{
										NaNBoxing v = t.Value.initValue.Value;
										linkConstantValue(&v, swc);
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
						}

					}
				}
			}

			for (int i = 0; i < swc.Scripts.Count; i++)
			{
				ComputeOperatorTable(swc.Scripts[i],Context.dictTypes);
			}

		}

		public void MakeVectorType(ASVector vector
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
							@class.Instance._element_class = compilingClasses.First( c => c.Type_identifier == (ulong)vector.ElementType);
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
				instance.Constructor.Flags = Context.VECTOR.Instance.Constructor.Flags | MethodFlags.NeedArguments ;
				instance.Constructor.Name = Context.VECTOR.Instance.Constructor.Name;
				instance.Constructor.__ismethod = true;
				instance.Constructor.Parameters.AddRange ( Context.VECTOR.Instance.Constructor.Parameters);
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
					trait.Method = new ASMethod(instance,t.Token);
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

					trait.Method.ReturnTypeKind = vector.ElementType ;
					trait.Method.ReturnType = null ;
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
					var t = Context.VECTOR.Instance.Traits.Find(t=>t.QName.Name == "length" && t.Kind == TraitKind.Getter);
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
					trait.Method.ReturnType = t.Method.ReturnType ;
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

					trait.Method.ReturnTypeKind = (TypeKind)@class.Type_identifier ;
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


			RtHeapInstance _temp = null;
			int errPtr = Context.GC.AllocInstance(Context.ERROR.Instance, out _temp);
			if (errPtr == 0)
			{
				error.error.setFault();
			}
			else
			{

				NaNBoxing errName = new NaNBoxing();
				errName.SetHeapPtr(cache_ERROR_NAME);
				((RtPayloadInstance)_temp.facility).SetSlot(errName, 1, Context.ERROR.Instance._link_codescope, this);

				RtHeapInstance error_instance = _temp;
				RtPayloadInstance payloadInstance = (RtPayloadInstance)error_instance.facility;

				NaNBoxing naNBoxing = new NaNBoxing();
				naNBoxing.SetHeapPtr(cache_STACKOVERFLOW_STR);
				payloadInstance.SetSlot(naNBoxing, 0, error_instance.Type._link_codescope, this);


				error.error.SetHeapPtr(errPtr);
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
		public bool TryCreateStringValue(string str, out NaNBoxing result, ref ReceiveError error)
		{
			Debug.Assert(str != null, "String cannot be null - use SetNull() for null values");
			
			result = default;
			
			// 首先尝试创建LocalString
			if (NaNBoxing.TryCreateLocalString(str, out result))
			{
				return true;
			}
			
			// 回退到堆分配
			int strptr = Context.GC.AllocString(str);
			if (strptr == 0)
			{
				RaiseOutOfMemory(ref error);
				return false;
			}
			
			result.SetHeapPtr(strptr);
			return true;
		}

		int cache_OUTOFMEMORY_STR;
		internal void RaiseOutOfMemory(ref ReceiveError error)
		{

			error.raised = true;

			RtHeapInstance _temp = null;
			int errPtr = Context.GC.AllocInstance(Context.ERROR.Instance, out _temp);
			if (errPtr == 0)
			{
				error.error.setFault();
			}
			else
			{

				NaNBoxing errName = new NaNBoxing();
				errName.SetHeapPtr(cache_ERROR_NAME);
				((RtPayloadInstance)_temp.facility).SetSlot(errName, 1, Context.ERROR.Instance._link_codescope, this);

				RtHeapInstance error_instance = _temp;
				RtPayloadInstance payloadInstance = (RtPayloadInstance)error_instance.facility;

				NaNBoxing naNBoxing = new NaNBoxing();
				naNBoxing.SetHeapPtr(cache_OUTOFMEMORY_STR);
				payloadInstance.SetSlot(naNBoxing, 0, error_instance.Type._link_codescope, this);


				error.error.SetHeapPtr(errPtr);
			}


		}

		private ASMultiname buildin_as_methodclosure = new ASMultiname() { Kind = MultinameKind.QName, Name = "MethodClosure", Namespace = new ASNamespace() { Name = "builtin.as" } };

		



		public void RaiseError(ref ReceiveError error, string message)
		{
			error.raised = true;


			RtHeapInstance _temp = null;
			int errPtr = Context.GC.AllocInstance(Context.ERROR.Instance, out _temp);
			if (errPtr == 0)
			{
				error.error.setFault();
			}
			else
			{

				NaNBoxing errName = new NaNBoxing();
				errName.SetHeapPtr(cache_ERROR_NAME);
				((RtPayloadInstance)_temp.facility).SetSlot(errName, 1, Context.ERROR.Instance._link_codescope, this);

				RtHeapInstance error_instance = _temp;
				RtPayloadInstance payloadInstance = (RtPayloadInstance)error_instance.facility;

			
				int messagePtr = Context.GC.AllocString(message);
				if (messagePtr != 0)
				{
					NaNBoxing naNBoxing = new NaNBoxing();
					naNBoxing.SetHeapPtr(messagePtr);
					payloadInstance.SetSlot(naNBoxing, 0, error_instance.Type._link_codescope, this);
				}
				else
				{
					NaNBoxing naNBoxing = new NaNBoxing();
					naNBoxing.SetHeapPtr(cache_OUTOFMEMORY_STR);
					payloadInstance.SetSlot(naNBoxing, 0, _temp.Type._link_codescope, this);
				}

				error.error.SetHeapPtr(errPtr);
			}
		}
		internal int cache_Eval_ERROR_NAME;

		internal int cache_TYPE_ERROR_NAME;
		internal void RaiseTypeError(ref ReceiveError error, NaNBoxing value, TypeKind toType)
		{

			error.raised = true;
			RtHeapInstance _temp = null;
			int errPtr = Context.GC.AllocInstance(Context.TYPE_ERROR.Instance, out _temp);
			if (errPtr == 0)
			{
				error.error.setFault();
			}
			else
			{
				NaNBoxing errName = new NaNBoxing();
				errName.SetHeapPtr(cache_TYPE_ERROR_NAME);
				((RtPayloadInstance)_temp.facility).SetSlot(errName, 1, Context.TYPE_ERROR.Instance._link_codescope, this);

				RtHeapInstance error_instance = _temp;
				RtPayloadInstance payloadInstance = (RtPayloadInstance)error_instance.facility;

				int messagePtr = Context.GC.AllocString($"Type Coercion failed: can not convert {value.ToDebugString(this)} to {toType.ToDebugString(this)}.");
				if (messagePtr != 0)
				{
					NaNBoxing naNBoxing = new NaNBoxing();
					naNBoxing.SetHeapPtr(messagePtr);
					payloadInstance.SetSlot(naNBoxing, 0, error_instance.Type._link_codescope, this);
				}
				else
				{
					NaNBoxing naNBoxing = new NaNBoxing();
					naNBoxing.SetHeapPtr(cache_OUTOFMEMORY_STR);
					payloadInstance.SetSlot(naNBoxing, 0, _temp.Type._link_codescope, this);
				}

				error.error.SetHeapPtr(errPtr);
			}


		}

		internal void RaiseTypeError_Ambiguous(ref ReceiveError error, string name)
		{

			error.raised = true;
			RtHeapInstance _temp;
			int errPtr = Context.GC.AllocInstance(Context.TYPE_ERROR.Instance, out _temp);
			if (errPtr == 0)
			{
				error.error.setFault();
			}
			else
			{
				NaNBoxing errName = new NaNBoxing();
				errName.SetHeapPtr(cache_TYPE_ERROR_NAME);
				((RtPayloadInstance)_temp.facility).SetSlot(errName, 1, Context.TYPE_ERROR.Instance._link_codescope, this);

				RtHeapInstance error_instance = _temp;
				RtPayloadInstance payloadInstance = (RtPayloadInstance)error_instance.facility;

				int messagePtr = Context.GC.AllocString($"{name} is ambiguous; Found more than one matching binding.");
				if (messagePtr != 0)
				{
					NaNBoxing naNBoxing = new NaNBoxing();
					naNBoxing.SetHeapPtr(messagePtr);
					payloadInstance.SetSlot(naNBoxing, 0, error_instance.Type._link_codescope, this);
				}
				else
				{
					NaNBoxing naNBoxing = new NaNBoxing();
					naNBoxing.SetHeapPtr(cache_OUTOFMEMORY_STR);
					payloadInstance.SetSlot(naNBoxing, 0, _temp.Type._link_codescope, this);
				}

				error.error.SetHeapPtr(errPtr);
			}
		}


		internal void RaiseTypeError_ConvertToPrimitive(ref ReceiveError error, NaNBoxing value)
		{
			error.raised = true;
			RtHeapInstance _temp;
			int errPtr = Context.GC.AllocInstance(Context.TYPE_ERROR.Instance, out _temp);
			if (errPtr == 0)
			{
				error.error.setFault();
			}
			else
			{
				NaNBoxing errName = new NaNBoxing();
				errName.SetHeapPtr(cache_TYPE_ERROR_NAME);
				((RtPayloadInstance)_temp.facility).SetSlot(errName, 1, Context.TYPE_ERROR.Instance._link_codescope, this);

				RtHeapInstance error_instance = _temp;
				RtPayloadInstance payloadInstance = (RtPayloadInstance)error_instance.facility;

				int messagePtr = Context.GC.AllocString($"Cannot convert {value.ToDebugString(this)} to primitive.");
				if (messagePtr != 0)
				{
					NaNBoxing naNBoxing = new NaNBoxing();
					naNBoxing.SetHeapPtr(messagePtr);
					payloadInstance.SetSlot(naNBoxing, 0, error_instance.Type._link_codescope, this);
				}
				else
				{
					NaNBoxing naNBoxing = new NaNBoxing();
					naNBoxing.SetHeapPtr(cache_OUTOFMEMORY_STR);
					payloadInstance.SetSlot(naNBoxing, 0, _temp.Type._link_codescope, this);
				}

				error.error.SetHeapPtr(errPtr);
			}
		}


		int cache_ILLEGALOPERATION_ERROR_NAME;
		internal void RaiseIllegaloperationError(ref ReceiveError error, string methodkey)
		{
			error.raised = true;
			RtHeapInstance _temp;
			int errPtr = Context.GC.AllocInstance(Context.ILLEGALOPERATION_ERROR.Instance, out _temp);
			if (errPtr == 0)
			{
				error.error.setFault();
			}
			else
			{
				NaNBoxing errName = new NaNBoxing();
				errName.SetHeapPtr(cache_ILLEGALOPERATION_ERROR_NAME);
				((RtPayloadInstance)_temp.facility).SetSlot(errName, 1, Context.ILLEGALOPERATION_ERROR.Instance._link_codescope, this);

				RtHeapInstance error_instance = _temp;
				RtPayloadInstance payloadInstance = (RtPayloadInstance)error_instance.facility;

				int messagePtr = Context.GC.AllocString($"native function {methodkey} is missing.");
				if (messagePtr != 0)
				{
					NaNBoxing naNBoxing = new NaNBoxing();
					naNBoxing.SetHeapPtr(messagePtr);
					payloadInstance.SetSlot(naNBoxing, 0, error_instance.Type._link_codescope, this);
				}
				else
				{
					NaNBoxing naNBoxing = new NaNBoxing();
					naNBoxing.SetHeapPtr(cache_OUTOFMEMORY_STR);
					payloadInstance.SetSlot(naNBoxing, 0, _temp.Type._link_codescope, this);
				}

				error.error.SetHeapPtr(errPtr);
			}
		}


		int cache_CANNOT_ACCESS_NULL;
		internal void RaiseTypeError_AccessNull(ref ReceiveError error)
		{

			error.raised = true;
			RtHeapInstance _temp;
			int errPtr = Context.GC.AllocInstance(Context.TYPE_ERROR.Instance, out _temp);
			if (errPtr == 0)
			{
				error.error.setFault();
			}
			else
			{
				NaNBoxing errName = new NaNBoxing();
				errName.SetHeapPtr(cache_TYPE_ERROR_NAME);
				((RtPayloadInstance)_temp.facility).SetSlot(errName, 1, Context.TYPE_ERROR.Instance._link_codescope, this);

				RtHeapInstance error_instance = _temp;
				RtPayloadInstance payloadInstance = (RtPayloadInstance)error_instance.facility;

				NaNBoxing naNBoxing = new NaNBoxing();
				naNBoxing.SetHeapPtr(cache_CANNOT_ACCESS_NULL);
				payloadInstance.SetSlot(naNBoxing, 0, error_instance.Type._link_codescope, this);

				error.error.SetHeapPtr(errPtr);
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
				string str = ((RtPayloadString)Context.GC.Heap[shapeName.HeapPtr].facility).Str;
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
				
				string str2 = ((RtPayloadString)Context.GC.Heap[shapeName2.HeapPtr].facility).Str;
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

				string str1 = ((RtPayloadString)Context.GC.Heap[shapeName1.HeapPtr].facility).Str;
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
				string str1 = ((RtPayloadString)Context.GC.Heap[shapeName1.HeapPtr].facility).Str;
				string str2 = ((RtPayloadString)Context.GC.Heap[shapeName2.HeapPtr].facility).Str;
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




		private void CreateObjectProto(ref ReceiveError error)
		{
			var proto_ptr = ((RtPayloadScriptClass)Context.GC.Heap[Context.OBJECT.__instance_index__].facility).PROTO__PTR;
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
				tostring.Body._link_codescope = new CodeScope() { Members = new List<ScopeMember>() , Parent = Context.OBJECT._link_codescope.Parent };
				tostring.IsAnonymous = true;
				tostring.__is_buildin_proto = true;

				int tostring_ptr = Context.GC.AllocClosure(tostring);
				if (tostring_ptr == 0)
				{
					throw new LoaderException("Object proto : toString alloc failed");
				}

				((RtPayloadClosure)Context.GC.Heap[tostring_ptr].facility).ScopePtr = ((ASScript)Context.OBJECT._link_codescope.Parent.Container).__global_index__;

				NaNBoxing v = default; v.SetHeapPtr(tostring_ptr);

				NaNBoxing v_str = default;v_str.SetHeapPtr(TOSTRING_STR);
				CreateDynamic(ref error, proto, v_str, v, false, false, false);

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

				((RtPayloadClosure)Context.GC.Heap[valueof_ptr].facility).ScopePtr = ((ASScript)Context.OBJECT._link_codescope.Parent.Container).__global_index__;

				NaNBoxing v = default; v.SetHeapPtr(valueof_ptr);
				NaNBoxing v_str = default; v_str.SetHeapPtr(VALUEOF_STR);
				CreateDynamic(ref error, proto, v_str, v, false, false, false);
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
				m.Parameters.Add( new ASParameter(m) { IsOptional=false, Name = "name", IsRest =false , Type = Context.STRING.QName , TypeKind = TypeKind.String  } );
				m.Body._link_codescope.Members.Add(new ScopeMember(m.Body, null)
				{
					Kind = ScopeMemberKind.Parameter,
					PName = "name",
					Type = m.Parameters[0].Type,
					TypeKind = TypeKind.String,
					__rt_type_class__ = Context.STRING
				});
				m.__is_hasOwnProperty = true;
				m.__is_buildin_proto = true;

				int hasownproperty_ptr = Context.GC.AllocClosure(m);
				if (hasownproperty_ptr == 0)
				{
					throw new LoaderException("Object proto: hasOwnProperty alloc failed");
				}

				((RtPayloadClosure)Context.GC.Heap[hasownproperty_ptr].facility).ScopePtr = ((ASScript)Context.OBJECT._link_codescope.Parent.Container).__global_index__;
				((RtPayloadClosure)Context.GC.Heap[hasownproperty_ptr].facility).Set_PROTOTYPE(-1, this); //设置prototype为undefined

				NaNBoxing v = default; v.SetHeapPtr(hasownproperty_ptr);
				NaNBoxing v_str = default; v_str.SetHeapPtr(hasOwnProperty);
				CreateDynamic(ref error, proto, v_str, v, false, false, false);



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
					__rt_type_class__ = Context.OBJECT
				});
				m.__is_hasOwnProperty = true;
				m.__is_buildin_proto = true;

				int isprototypeof_ptr = Context.GC.AllocClosure(m);
				if (isprototypeof_ptr == 0)
				{
					throw new LoaderException("Object proto: isPrototypeOf alloc failed");
				}

				((RtPayloadClosure)Context.GC.Heap[isprototypeof_ptr].facility).ScopePtr = ((ASScript)Context.OBJECT._link_codescope.Parent.Container).__global_index__;
				((RtPayloadClosure)Context.GC.Heap[isprototypeof_ptr].facility).Set_PROTOTYPE(-1, this); //设置prototype为undefined

				NaNBoxing v = default; v.SetHeapPtr(isprototypeof_ptr);
				NaNBoxing v_str = default; v_str.SetHeapPtr(isPrototypeOf);
				CreateDynamic(ref error, proto, v_str, v, false, false, false);
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

			var proto_ptr = ((RtPayloadScriptClass)Context.GC.Heap[Context.FUNCTION.__instance_index__].facility).PROTO__PTR;
			var proto = Context.GC.Heap[proto_ptr];

			// call
			{
				var call_ptr = Context.GC.AllocString("call");
				if (call_ptr == 0)
				{
					throw new LoaderException("CALL_PTR alloc failed");
				}
				Context.GC.Root.Add(Context.GC.Heap[call_ptr]);

				ASMethod call = Context.FUNCTION.Instance._vtable.Items[3].Trait.Method; call.__is_call_or_apply = true;call.__is_buildin_proto = true;

				int invokecall_ptr = Context.GC.AllocClosure(call);
				if (invokecall_ptr == 0)
				{
					throw new LoaderException("Function proto : call alloc failed");
				}

				((RtPayloadClosure)Context.GC.Heap[invokecall_ptr].facility).ScopePtr = ((ASScript)Context.FUNCTION._link_codescope.Parent.Container).__global_index__;
				((RtPayloadClosure)Context.GC.Heap[invokecall_ptr].facility).Set_PROTOTYPE(-1, this);

				NaNBoxing v = default; v.SetHeapPtr(invokecall_ptr);
				NaNBoxing v_str = default; v_str.SetHeapPtr(call_ptr);
				CreateDynamic(ref error, proto, v_str, v, false, false, false);

			}

			//apply
			{
				var apply_ptr = Context.GC.AllocString("apply");
				if (apply_ptr == 0)
				{
					throw new LoaderException("APPLY_PTR alloc failed");
				}
				Context.GC.Root.Add(Context.GC.Heap[apply_ptr]);

				ASMethod apply = Context.FUNCTION.Instance._vtable.Items[2].Trait.Method; apply.__is_call_or_apply = true;apply.__is_buildin_proto = true;

				int invokeapply_ptr = Context.GC.AllocClosure(apply);
				if (invokeapply_ptr == 0)
				{
					throw new LoaderException("Function proto : apply alloc failed");
				}

				((RtPayloadClosure)Context.GC.Heap[invokeapply_ptr].facility).ScopePtr = ((ASScript)Context.FUNCTION._link_codescope.Parent.Container).__global_index__;
				((RtPayloadClosure)Context.GC.Heap[invokeapply_ptr].facility).Set_PROTOTYPE(-1, this);


				NaNBoxing v = default; v.SetHeapPtr(invokeapply_ptr);
				NaNBoxing v_str = default; v_str.SetHeapPtr(apply_ptr);
				CreateDynamic(ref error, proto, v_str, v, false, false, false);

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

				((RtPayloadClosure)Context.GC.Heap[tostring_ptr].facility).ScopePtr = ((ASScript)Context.FUNCTION._link_codescope.Parent.Container).__global_index__;

				NaNBoxing v = default; v.SetHeapPtr(tostring_ptr);
				NaNBoxing v_str = default; v_str.SetHeapPtr(TOSTRING_STR);
				CreateDynamic(ref error, proto, v_str, v, false, false, false);

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
			var proto_ptr = ((RtPayloadScriptClass)Context.GC.Heap[Context.ARRAY.__instance_index__].facility).PROTO__PTR;
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

				((RtPayloadClosure)Context.GC.Heap[tostring_ptr].facility).ScopePtr = ((ASScript)Context.ARRAY._link_codescope.Parent.Container).__global_index__;

					NaNBoxing v = default; v.SetHeapPtr(tostring_ptr);
					NaNBoxing v_str = default; v_str.SetHeapPtr(TOSTRING_STR);
					CreateDynamic(ref error, proto, v_str, v, false, false, true);
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
				m.Parameters.Add(new ASParameter(m) { IsOptional = false, Name = "rest", IsRest = true , TypeKind = TypeKind.Array, Type = Context.ARRAY.QName});
				m.Body._link_codescope.Members.Add(new ScopeMember(m.Body, null)
				{
					Kind = ScopeMemberKind.Parameter,
					PName = "rest",
					Type = m.Parameters[0].Type,
					TypeKind = m.Parameters[0].TypeKind,
					__rt_type_class__ = Context.ARRAY
				});

				m.__is_buildin_proto = true;

				int concat_ptr = Context.GC.AllocClosure(m);
				if (concat_ptr == 0)
				{
					throw new LoaderException("Array proto: concat alloc failed");
				}

				((RtPayloadClosure)Context.GC.Heap[concat_ptr].facility).ScopePtr = ((ASScript)Context.OBJECT._link_codescope.Parent.Container).__global_index__;
				((RtPayloadClosure)Context.GC.Heap[concat_ptr].facility).Set_PROTOTYPE(-1, this); //设置prototype为undefined

				NaNBoxing v = default; v.SetHeapPtr(concat_ptr);
				NaNBoxing v_str = default; v_str.SetHeapPtr(concat);
				CreateDynamic(ref error, proto, v_str, v, false, false, false);


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
				m.Parameters.Add(new ASParameter(m) { IsOptional = false, Name = "rest", IsRest = true , TypeKind = TypeKind.Array, Type = Context.ARRAY.QName});
				m.Body._link_codescope.Members.Add(new ScopeMember(m.Body, null)
				{
					Kind = ScopeMemberKind.Parameter,
					PName = "rest",
					Type = m.Parameters[0].Type,
					TypeKind = m.Parameters[0].TypeKind,
					__rt_type_class__ = Context.ARRAY
				});

				m.__is_buildin_proto = true;

				int push_ptr = Context.GC.AllocClosure(m);
				if (push_ptr == 0)
				{
					throw new LoaderException("Array proto: push alloc failed");
				}

				((RtPayloadClosure)Context.GC.Heap[push_ptr].facility).ScopePtr = ((ASScript)Context.OBJECT._link_codescope.Parent.Container).__global_index__;
				((RtPayloadClosure)Context.GC.Heap[push_ptr].facility).Set_PROTOTYPE(-1, this); //设置prototype为undefined

				NaNBoxing v2 = default; v2.SetHeapPtr(push_ptr);
				NaNBoxing v_str = default; v_str.SetHeapPtr(push);
				CreateDynamic(ref error, proto, v_str, v2, false, false, false);


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
			InitScript((ASScript)Context.FUNCTION._link_codescope.Parent.Container, ref error); if (error.raised) { throw new LoaderException("FUNCTION init failed"); }
			CreateFunctionProto(ref error); if (error.raised) { throw new LoaderException("FUNCTION init failed"); }

			InitScript((ASScript)Context.ARRAY._link_codescope.Parent.Container, ref error); if (error.raised) { throw new LoaderException("ARRAY init failed"); }
			CreateArrayProto(ref error); if (error.raised) { throw new LoaderException("ARRAY init failed"); };


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
				Context.AsyncCallbackQueue.RunQueue(Context,ref queueError);
				if (queueError.raised)
				{
					Debug.Assert(queueError.error.ValueType == BoxType.Fault);

					
					var	ex = new PlayerException(this, queueError.error, " AsyncCallbackQueue Fault.");
				
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
					
					var	ex = new PlayerException(this, microtask_fault.error, "Run Microtask Fault.");
					Context.errorStack.Clear();

					if (onErrorRaised != null)
					{
						onErrorRaised(ex);
					}
					
					break;
				}

				

				if (!_shutdownEvent && ( Context.AsyncCallbackQueue.HasPending || Context.TimerTaskQueue.HasWaitingTasks || Context.AsyncCallbackQueue.HasPending))
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
			thisPtr.SetHeapPtr(index);

			unsafe
			{
				RunMethod(script.Initializer, thisPtr, index, null, 0, null, null, ref error, -1);
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

			//构造proto的constructor, 就是Class自己。
			var proto = Context.GC.Heap[((RtPayloadScriptClass)Context.GC.Heap[index].facility).PROTO__PTR];
			NaNBoxing constructor = new NaNBoxing(); constructor.SetHeapPtr(index);
			NaNBoxing v_str = default; v_str.SetHeapPtr(CONSTRUCTOR_STR);
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
					if (t.Kind == TraitKind.Slot && t.__rt_type_class__ !=null)
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
					RunMethod(cls.Constructor, new NaNBoxing(NaNBoxing.NULL), index, null, 0, null, null, ref error, -1);
				}

			}


		}

		private unsafe void LoadStackLocater(StackLocater* stacklocatoer, byte** P)
		{
			stacklocatoer->index = *(int*)(*P); (*P) += 4;

			//byte* _p = (byte*)&stacklocatoer->index;
			//*_p++ = *(*P)++;
			//*_p++ = *(*P)++;
			//*_p++ = *(*P)++;
			//*_p = *(*P)++;
		}

		private unsafe void LoadInt32(int* value, byte** P)
		{
			*value = *(int*)(*P); (*P) += 4;
			//byte* _p = (byte*)value;
			//*_p++ = *(*P)++;
			//*_p++ = *(*P)++;
			//*_p++ = *(*P)++;
			//*_p = *(*P)++;
		}

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

		private void WriteFunctionProto(NaNBoxing box, ref ReceiveError error, RtHeapInstance closure, int closure_ptr)
		{

			if (((ASMethodBody)closure.Type).Method.__ismethod)
			{
				RaiseReferenceError_WriteToReadonlyProperty(ref error, Context.FUNCTION.Instance._vtable.Items[1].Trait.Method.Body, buildin_as_methodclosure);
			}
			else
			{
				if (box.ValueType == NaNBoxing.BoxType.Undefined || box.ValueType == NaNBoxing.BoxType.Null)
				{
					((RtPayloadClosure)closure.facility).Set_PROTOTYPE(-1, this);
				}
				else if (box.ValueType == NaNBoxing.BoxType.HeapPtr)
				{
					//访问prototype.这时候只能把function保存到堆里了。
					NaNBoxing v = new NaNBoxing(); v.SetHeapPtr(closure_ptr);
					NaNBoxing clouse_heap = GetSaveValue(v, ref error);
					if (error.raised)
					{
						return;
					}


					switch (Context.GC.Heap[box.HeapPtr].TypeKind)
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




							((RtPayloadClosure)closure.facility).Set_PROTOTYPE(box.HeapPtr, this);
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

		private unsafe NaNBoxing InvokeReadProperty(ref ReceiveError error, NaNBoxing thisValue, int vtable_index, ref Span<NaNBoxing> stackslots, int returnSlotIndex)
		{
			if (thisValue.ValueType != NaNBoxing.BoxType.HeapPtr)
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
				RtHeapInstance ins = Context.GC.Heap[thisValue.HeapPtr];

				if (ins.TypeKind == RtHeapTypeKind.INSTANCE
					||
					ins.TypeKind == RtHeapTypeKind.ARRAY
					||
					ins.TypeKind == RtHeapTypeKind.VECTOR
					)
				{
					var vtableitem = ins.Type._vtable.Items[vtable_index];
					var function = vtableitem.Trait.Method;

					var define = (ASInstance)vtableitem.DefineAt;

					NaNBoxing result = RunMethod(function,
					thisValue, thisValue.HeapPtr, define, 0, null, stackslots, ref error, returnSlotIndex);

					return result;

				}
				else if (ins.TypeKind == RtHeapTypeKind.NAMESPACE)
				{
					var vtableitem =  Context.NAMESPACE.Instance._vtable.Items[vtable_index];
					var function = vtableitem.Trait.Method;

					var define = (ASInstance)vtableitem.DefineAt;

					NaNBoxing result = RunMethod(function,
					thisValue, thisValue.HeapPtr, define, 0, null, stackslots, ref error, returnSlotIndex);

					return result;
				}
				else if (thisValue.HeapPtr == Context.CLASS.__instance_index__)
				{
#if DEBUG
					if (vtable_index != 0)
						throw new InvalidOperationException();
#endif
					NaNBoxing result = new NaNBoxing();
					result.SetHeapPtr(((RtPayloadScriptClass)ins.facility).PROTO__PTR);

					return result;

				}
				else if (ins.TypeKind == RtHeapTypeKind.STRING)
				{
					var vtableitem = Context.STRING.Instance._vtable.Items[vtable_index];
					var function = vtableitem.Trait.Method;

					var define = (ASInstance)vtableitem.DefineAt;

					NaNBoxing result = RunMethod(function,
					thisValue, thisValue.HeapPtr, define, 0, null, stackslots, ref error, returnSlotIndex);

					return result;
				}
				else if (ins.TypeKind == RtHeapTypeKind.CLASS)
				{
					var @class = ((RtPayloadScriptClass)ins.facility).Meta;
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
						result.SetHeapPtr(((RtPayloadScriptClass)ins.facility).PROTO__PTR);

						return result;
					}
					else
					{

						NaNBoxing result = RunMethod(function,
							thisValue, thisValue.HeapPtr, @class, 0, null, stackslots, ref error, returnSlotIndex);

						//if (error.raised)
						//{
						//    goto flag_handle_error;
						//}

						//stackslots[target.index] = result;

						return result;
					}
				}
				else if (ins.TypeKind == RtHeapTypeKind.CLOSURE)
				{
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


							var closure = (RtPayloadClosure)ins.facility;

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


								RtHeapInstance prototype;
								int ptr = Context.GC.AllocInstance(Context.OBJECT.Instance, out prototype);
								if (ptr == 0)
								{
									RaiseOutOfMemory(ref error);
									return default;
								}
								else
								{
									NaNBoxing v_str = default; v_str.SetHeapPtr(CONSTRUCTOR_STR);
									//创建constructor   这个属性可以被删除。
									CreateDynamic(ref error, prototype, v_str, closure_heap, true, false, true);
									if (error.raised)
									{
										return default;
									}

									closure.Set_PROTOTYPE(ptr, this);
									NaNBoxing r = default;
									r.SetHeapPtr(ptr);
									return r;
								}

							}
							else
							{
								NaNBoxing r = default;
								r.SetHeapPtr(proto);
								return r;
							}
						}
					}
					else
					{

						var define = (ASInstance)vtableitem.DefineAt;

						NaNBoxing result = RunMethod(function,
						thisValue, thisValue.HeapPtr, define, 0, null, stackslots, ref error, returnSlotIndex);

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

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		internal NaNBoxing LoadSlotFromArray(uint uindex,RtHeapInstance arrObj,out bool isoutofindex_or_ishole)
		{
			
			NaNBoxing result = ((RtPayloadArray)arrObj.facility).ReadSlot(uindex, this, out isoutofindex_or_ishole);

			if (isoutofindex_or_ishole) //如果索引超出了length或者是一个洞则查找原型链。。。
			{
				string searchName = uindex.ToString();
				NaNBoxing value; int shape_ptr; int index; RtPayloadDynamic prop;

				if (FindDynamicValue(arrObj, searchName, out value, out shape_ptr, out index, out prop))
				{
					result = value;
				}
				else
				{
					var proto = Context.GC.Heap[((RtPayloadScriptClass)Context.GC.Heap[Context.ARRAY.__instance_index__].facility).PROTO__PTR];
				lbl_searh_class_proto:
					if (FindDynamicValue(proto, searchName, out value, out shape_ptr, out index, out prop))
					{
						result = value;
					}
					else
					{
						int p = ((RtPayloadInstance)proto.facility).PROTOTYPE(this, (ASInstance)proto.Type); //class的proto肯定是instance。
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

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		internal void SetArraySlot(NaNBoxing box,uint index,RtHeapInstance instance,ref ReceiveError error)
		{
			if (((RtPayloadArray)instance.facility).TrySetSlotIfReplaceStructOrNotHeap(box, index, this, ref error))
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

				((RtPayloadArray)instance.facility).SetSlot(box, index, this, ref error);
				if (error.raised)
				{
					return;
				}
			}
		}


		internal void VisitArrayProto(RtHeapInstance arrObj,Action<NaNBoxing,NaNBoxing> OnVisit)
		{
			VisitDynamicValue(arrObj, OnVisit);

			var proto = Context.GC.Heap[((RtPayloadScriptClass)Context.GC.Heap[Context.ARRAY.__instance_index__].facility).PROTO__PTR];

		lbl_searh_class_proto:
			VisitDynamicValue(proto, OnVisit);
			int p = ((RtPayloadInstance)proto.facility).PROTOTYPE(this, (ASInstance)proto.Type); //class的proto肯定是instance。
			if (p != 0)
			{
				proto = Context.GC.Heap[p];
				goto lbl_searh_class_proto;
			}

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
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		internal unsafe NaNBoxing LoadValue(NaNBoxing box, int callee_slotindex, ref ReceiveError error,  Span<NaNBoxing> stackslots, int returnSlotIndex)
		{
			NaNBoxing result = box;
			if (box.ValueType == NaNBoxing.BoxType.HeapPtr)
			{
				RtHeapInstance rtHeap = Context.GC.Heap[box.HeapPtr];
				if (rtHeap.TypeKind == RtHeapTypeKind.STACK_CACHE_OBJ)
				{
					RtPayloadStackCache _obj = (RtPayloadStackCache)rtHeap.facility;

					if (_obj.RefInstance.ValueType == BoxType.HeapPtr)
					{


						RtHeapInstance refObj = Context.GC.Heap[_obj.RefInstance.HeapPtr];

						if (_obj.searchPropertyName.ValueType == BoxType.HeapPtr || _obj.searchPropertyName.ValueType ==  BoxType.LocalString ) //动态属性
						{
							Context.GC.CheckGC(ref error);

							//string searchName = ((RtPayloadString)Context.GC.Heap[_obj.searchPropertyNamePtr].facility).Str;



							Span<char> temp = stackalloc char[16];
							ReadOnlySpan<char> searchName;

							if (_obj.searchPropertyName.ValueType == BoxType.LocalString)
							{
								int l = _obj.searchPropertyName.GetLocalStringChars(temp);
								searchName = temp.Slice(0, l);

							}
							else
							{
								searchName = ((RtPayloadString)Context.GC.Heap[_obj.searchPropertyName.HeapPtr].facility).Str.AsSpan();
							}

							


							NaNBoxing ns = new NaNBoxing();
							ASNamespace @namespace = null;
							if (_obj.searchNameSpacePtr > 0)
							{
								ns.SetHeapPtr(_obj.searchNameSpacePtr);
								RtHeapInstance ns_instance = Context.GC.Heap[_obj.searchNameSpacePtr];
								@namespace = ((RtPayloadNameSpace)ns_instance.facility).ASNamespace;
								
							}



							if (refObj.TypeKind == RtHeapTypeKind.INSTANCE
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
									var proto = Context.GC.Heap[((RtPayloadScriptClass)Context.GC.Heap[refObj.Type._link_codescope.TypeLayout.ASType.__instance_index__].facility).PROTO__PTR];
								lbl_searh_class_proto:
									NaNBoxing value; int shape_ptr; int index; RtPayloadDynamic prop;
									if (FindDynamicValue(proto, searchName, out value, out shape_ptr, out index, out prop))
									{
										result = value;
									}
									else
									{
										int p = ((RtPayloadInstance)proto.facility).PROTOTYPE(this, (ASInstance)proto.Type); //class的proto肯定是instance。
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
							else if (refObj.TypeKind == RtHeapTypeKind.NAMESPACE)
							{
								if (@namespace != null)
								{
									RaiseReferenceError_RTQNameNotFound(ref error, ns, searchName, _obj.RefInstance);
								}
								else
								{
									//到class的prototype里查找，再找不到就到Object的prototype里查找
									var proto = Context.GC.Heap[((RtPayloadScriptClass)Context.GC.Heap[ Context.NAMESPACE.__instance_index__].facility).PROTO__PTR];
								lbl_searh_class_proto:
									NaNBoxing value; int shape_ptr; int index; RtPayloadDynamic prop;
									if (FindDynamicValue(proto, searchName, out value, out shape_ptr, out index, out prop))
									{
										result = value;
									}
									else
									{
										int p = ((RtPayloadInstance)proto.facility).PROTOTYPE(this, (ASInstance)proto.Type); //class的proto肯定是instance。
										if (p != 0)
										{
											proto = Context.GC.Heap[p];
											goto lbl_searh_class_proto;
										}

										RaiseReferenceError_MulitNameNotFound(ref error, searchName, _obj.as_type != null ? _obj.as_type.QName : refObj.Type.QName);
									}
								}
							}
							else if (refObj.TypeKind == RtHeapTypeKind.VECTOR)
							{
								if (@namespace != null)
								{
									RaiseReferenceError_RTQNameNotFound(ref error, ns, searchName, _obj.RefInstance);
								}
								else
								{
									var proto = Context.GC.Heap[((RtPayloadScriptClass)Context.GC.Heap[refObj.Type._link_codescope.TypeLayout.ASType.__instance_index__].facility).PROTO__PTR];
								lbl_searh_class_proto:
									NaNBoxing value; int shape_ptr; int index; RtPayloadDynamic prop;
									if (FindDynamicValue(proto, searchName, out value, out shape_ptr, out index, out prop))
									{
										result = value;
									}
									else
									{
										int p = ((RtPayloadInstance)proto.facility).PROTOTYPE(this, (ASInstance)proto.Type); //class的proto肯定是instance。
										if (p != 0)
										{
											proto = Context.GC.Heap[p];
											goto lbl_searh_class_proto;
										}

										RaiseReferenceError_MulitNameNotFound(ref error, searchName, refObj.Type.QName);
									}
								}
							}
							else if (refObj.TypeKind == RtHeapTypeKind.STRING)
							{
								if (@namespace != null)
								{
									RaiseReferenceError_RTQNameNotFound(ref error, ns, searchName, _obj.RefInstance);
								}
								else
								{
									var proto = Context.GC.Heap[((RtPayloadScriptClass)Context.GC.Heap[Context.STRING.__instance_index__].facility).PROTO__PTR];
								lbl_searh_class_proto:
									NaNBoxing value; int shape_ptr; int index; RtPayloadDynamic prop;
									if (FindDynamicValue(proto, searchName, out value, out shape_ptr, out index, out prop))
									{
										result = value;
									}
									else
									{
										int p = ((RtPayloadInstance)proto.facility).PROTOTYPE(this, (ASInstance)proto.Type); //class的proto肯定是instance。
										if (p != 0)
										{
											proto = Context.GC.Heap[p];
											goto lbl_searh_class_proto;
										}

										RaiseReferenceError_MulitNameNotFound(ref error, searchName, refObj.Type.QName);
									}
								}
							}
							else if (refObj.TypeKind == RtHeapTypeKind.ARRAY &&
									((RtPayloadArray)refObj.facility).isArguments()
									&& @namespace == null
									&& "callee".AsSpan().CompareTo( searchName, StringComparison.Ordinal) == 0
								)
							{
								result = Context.StackSlots[callee_slotindex];
							}
							else if (refObj.TypeKind == RtHeapTypeKind.CLOSURE)
							{
								if (@namespace != null)
								{
									RaiseReferenceError_RTQNameNotFound(ref error, ns, searchName, _obj.RefInstance);
								}
								else
								{
									NaNBoxing value; int shape_ptr; int index; RtPayloadDynamic prop;
									RtHeapInstance proto = null;
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
												int f_proto = ((RtPayloadScriptClass)Context.GC.Heap[Context.FUNCTION.__instance_index__].facility).PROTO__PTR;
												if (f_proto <= 0)
												{
													proto = Context.GC.Heap[((RtPayloadScriptClass)Context.GC.Heap[Context.OBJECT.__instance_index__].facility).PROTO__PTR];
												}
												else
												{
													proto = Context.GC.Heap[f_proto];
													if (refObj == proto) //Function.prototyoe是一个function,于是又转回来，这时转到OBJECT
													{
														proto = Context.GC.Heap[((RtPayloadScriptClass)Context.GC.Heap[Context.OBJECT.__instance_index__].facility).PROTO__PTR];

													}
												}
											}
										}
										else
										{
											proto = Context.GC.Heap[((RtPayloadScriptClass)Context.GC.Heap[Context.METHOD_CLOSURE.__instance_index__].facility).PROTO__PTR];
										}

									lbl_searh_class_proto:

										if (FindDynamicValue(proto, searchName, out value, out shape_ptr, out index, out prop))
										{
											result = value;
										}
										else
										{
											if (proto.TypeKind == RtHeapTypeKind.CLOSURE)
											{
												refObj = proto;
												goto lbl_retry_method;
											}

											int p = ((RtPayloadInstance)proto.facility).PROTOTYPE(this, (ASInstance)proto.Type); //class的proto肯定是instance。
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
							else if (refObj.TypeKind == RtHeapTypeKind.DYNAMIC_PROPERTYS || refObj.TypeKind == RtHeapTypeKind.SHAPE || refObj.TypeKind == RtHeapTypeKind.STACK_CACHE_OBJ)
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
									NaNBoxing value; int shape_ptr; int index; RtPayloadDynamic prop;

									int step = 0;

								lbl_instance_search_proto:

									if (step++ < 32)
									{

										if (refObj.TypeKind == RtHeapTypeKind.VECTOR)
										{
											refObj = Context.GC.Heap[((RtPayloadScriptClass)Context.GC.Heap[refObj.Type._link_codescope.TypeLayout.ASType.__instance_index__].facility).PROTO__PTR];
										}

										if (FindDynamicValue(refObj, searchName, out value, out shape_ptr, out index, out prop))
										{
											result = value;
										}
										else
										{
											if (refObj.TypeKind == RtHeapTypeKind.GLOBAL)
											{
												var proto = ((RtPayloadScriptClass)Context.GC.Heap[Context.OBJECT.__instance_index__].facility).PROTO__PTR;

												if (FindDynamicValue(Context.GC.Heap[proto], searchName, out value, out shape_ptr, out index, out prop))
												{
													result = value;
												}
												else
												{
													result.SetUndefined();
												}
											}
											else if (refObj.TypeKind == RtHeapTypeKind.ARRAY)
											{
												var proto = Context.GC.Heap[((RtPayloadScriptClass)Context.GC.Heap[Context.ARRAY.__instance_index__].facility).PROTO__PTR];
											lbl_searh_class_proto:
												if (FindDynamicValue(proto, searchName, out value, out shape_ptr, out index, out prop))
												{
													result = value;
												}
												else
												{
													int p = ((RtPayloadInstance)proto.facility).PROTOTYPE(this, (ASInstance)proto.Type); //class的proto肯定是instance。
													if (p != 0)
													{
														proto = Context.GC.Heap[p];
														goto lbl_searh_class_proto;
													}

													result.SetUndefined();
												}
											}
											else if (refObj.TypeKind == RtHeapTypeKind.CLASS)
											{

												var proto = Context.GC.Heap[((RtPayloadScriptClass)Context.GC.Heap[Context.CLASS.__instance_index__].facility).PROTO__PTR];
											lbl_searh_class_proto:
												if (FindDynamicValue(proto, searchName, out value, out shape_ptr, out index, out prop))
												{
													result = value;
												}
												else
												{
													int p = ((RtPayloadInstance)proto.facility).PROTOTYPE(this, (ASInstance)proto.Type); //class的proto肯定是instance。
													if (p != 0)
													{
														proto = Context.GC.Heap[p];
														goto lbl_searh_class_proto;
													}

													result.SetUndefined();
													//RaiseReferenceError_MulitNameNotFound(ref error, searchName, refObj.Type.QName);
												}



											}
											else if (refObj.TypeKind == RtHeapTypeKind.INSTANCE)
											{
												int protoptr = ((RtPayloadInstance)refObj.facility).PROTOTYPE(this, (ASInstance)refObj.Type);
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
											else if (refObj.TypeKind == RtHeapTypeKind.CLOSURE)
											{
												if (!((ASMethodBody)refObj.Type).Method.__ismethod)
												{
													if (FindDynamicValue(refObj, searchName, out value, out shape_ptr, out index, out prop))
													{
														result = value;
													}
													else
													{
														int protoptr = ((RtPayloadClosure)refObj.facility).PROTOTYPE(this);
														if (protoptr <= 0)
														{
															protoptr = ((RtPayloadScriptClass)Context.GC.Heap[Context.FUNCTION.__instance_index__].facility).PROTO__PTR;

															if (protoptr <= 0)
															{
																refObj = Context.GC.Heap[((RtPayloadScriptClass)Context.GC.Heap[Context.OBJECT.__instance_index__].facility).PROTO__PTR];
															}
															else
															{
																refObj = Context.GC.Heap[protoptr];
															}
														}
														else
														{
															refObj = Context.GC.Heap[((RtPayloadClosure)refObj.facility).PROTOTYPE(this)];
														}


														goto lbl_instance_search_proto;
													}
												}
												else
												{
													refObj = Context.GC.Heap[((RtPayloadScriptClass)Context.GC.Heap[Context.METHOD_CLOSURE.__instance_index__].facility).PROTO__PTR];
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
						else if (_obj.indexer_key.ValueType != NaNBoxing.BoxType.Fault)
						{
							if (refObj.TypeKind == RtHeapTypeKind.ARRAY) //通过索引下标操作
							{
#if DEBUG
								if (_obj.indexer_key.ValueType != NaNBoxing.BoxType.Uint || !(_obj.trait[0] == null && _obj.trait[1] == null))
								{
									throw new InvalidOperationException();
								}
#endif
								bool isoutofindex_or_ishole;
								var v = LoadSlotFromArray(_obj.indexer_key.UIntValue, refObj,out isoutofindex_or_ishole);

								if (v.ValueType == BoxType.Fault)
								{
									v.SetUndefined();
								}

								result = v;
							}
							else
							{
#if DEBUG
								if (!(
										(refObj.TypeKind == RtHeapTypeKind.INSTANCE  && ((ASInstance)refObj.Type).Flags.HasFlag(ClassFlags.Indexer))
										||
										refObj.TypeKind == RtHeapTypeKind.VECTOR

									)	
									)
								{
									throw new InvalidOperationException();
								}
#endif

								if (refObj.TypeKind == RtHeapTypeKind.VECTOR )
								{
#if DEBUG
									if (!RtPayloadVector.IsValidIndexType(_obj.indexer_key))
									{
										throw new InvalidOperationException();
									}
									else
#endif
									{
										RtPayloadVector vector;
										int v_ptr = RtPayloadVector.FindAndUpdateHeapInstancePtr(_obj.RefInstance.HeapPtr, this, out vector);
										int maxlen;int validid;

										if (!(vector.IsValidIndexRange(_obj.indexer_key,out validid ,out maxlen,this)))
										{
											RaiseRangeError(ref error, Extensions.GetPrimitiveValueToString(this, _obj.indexer_key), maxlen);
											return default;
										}
										else
										{
											return vector.ReadSlot( validid, this, returnSlotIndex , v_ptr);
											//throw new NotImplementedException();
										}
									}
								}
								else
								{
									if (Context.StackPosition + 1 >= Context.STACK_LENGTH)
									{
										RaiseStackOverflow(ref error);
										return default;
									}

									var argSpan = Context.StackSlots.AsSpan(Context.StackPosition, 1);
									argSpan[0] = _obj.indexer_key;
									StackLocater argLoc = new StackLocater() { index = 0 };

									Context.StackPosition++;

									NaNBoxing _this = new NaNBoxing();
									_this.SetHeapPtr(_obj.RefInstance.HeapPtr);

									unsafe
									{
										Context.StackPosition++;

										RunMethod(((ASInstance)refObj.Type).indexer_get, _this,
											_obj.RefInstance.HeapPtr, refObj.Type, 1, (byte*)&argLoc, argSpan, ref error, Context.StackPosition - 1);

										result = Context.StackSlots[Context.StackPosition - 1];

										Context.StackPosition--;
									}

									Context.StackPosition--;
									if (error.raised)
									{
										return default;
									}

								}
								if (result.ValueType == BoxType.Fault) //表示需要继续原型链查找
								{
									string searchName = null;
									//if (_obj.indexer_key.ValueType == BoxType.HeapPtr && Context.GC.Heap[_obj.indexer_key.HeapPtr].TypeKind == RtHeapTypeKind.STRING)
									//{
									//	searchName = ((RtPayloadString)Context.GC.Heap[_obj.indexer_key.HeapPtr].facility).Str;
									//}
									//else if (_obj.indexer_key.ValueType != BoxType.HeapPtr)
									if( IsPrimitive(_obj.indexer_key) )
									{
										searchName = Extensions.GetPrimitiveValueToString(this,_obj.indexer_key);
									}
									else
									{
										//这里应付有索引器的情况，所以这里不需要再触发toString了。
										if (Context.StackPosition + 1 >= Context.STACK_LENGTH)
										{
											RaiseStackOverflow(ref error);
											return default;
										}
										Context.StackPosition++;
										ConvertValueType(ref error, _obj.indexer_key, TypeKind.String, Context.STRING, ref Context.StackSlots[Context.StackPosition - 1]);
										Context.StackPosition--;
										if (error.raised)
										{
											return default;
										}

										searchName = Extensions.GetPrimitiveValueToString(this,Context.StackSlots[Context.StackPosition]);

										//throw new NotImplementedException();
									}

									NaNBoxing value; int shape_ptr; int index; RtPayloadDynamic prop;
									var proto = Context.GC.Heap[((RtPayloadScriptClass)Context.GC.Heap[refObj.Type._link_codescope.TypeLayout.ASType.__instance_index__].facility).PROTO__PTR];
								lbl_searh_class_proto:
									if (FindDynamicValue(proto, searchName, out value, out shape_ptr, out index, out prop))
									{
										result = value;
									}
									else
									{
										int p = ((RtPayloadInstance)proto.facility).PROTOTYPE(this,(ASInstance)proto.Type); //class的proto肯定是instance。
										if (p != 0)
										{
											proto = Context.GC.Heap[p];
											goto lbl_searh_class_proto;
										}

										result.SetUndefined();
									}


								}


								//throw new NotImplementedException();
							}
						}
						else if (_obj.trait[0] == null && _obj.trait[1] != null)
						{
							RaiseReferenceError_WriteToReadonlyProperty(ref error, _obj.trait[1].Method.Body, _obj.as_type.QName);
						}
						else if (_obj.trait[0].Kind == TraitKind.Slot || _obj.trait[0].Kind == TraitKind.Constant)
						{
							if (refObj.TypeKind == RtHeapTypeKind.GLOBAL || refObj.TypeKind == RtHeapTypeKind.CLASS)
							{
								result = ((RtPayloadScriptClass)refObj.facility).ReadSlot(_obj.scopemember_index);
							}
							else if (refObj.TypeKind == RtHeapTypeKind.INSTANCE)
							{
								result = ((RtPayloadInstance)refObj.facility).ReadSlot(_obj.scopemember_index, refObj.Type._link_codescope, this , returnSlotIndex ,_obj.RefInstance.HeapPtr);
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
							NaNBoxing instance = new NaNBoxing();
							instance.SetHeapPtr(_obj.RefInstance.HeapPtr);
							result = InvokeReadProperty(ref error, instance, _obj.g_index, ref stackslots, returnSlotIndex);

							//throw new NotImplementedException("属性的引用未实现");
						}
					}
					else
					{


						if (_obj.searchPropertyName.ValueType == BoxType.HeapPtr || _obj.searchPropertyName.ValueType == BoxType.LocalString) //动态属性
						{
							Context.GC.CheckGC(ref error);

							//string searchName = ((RtPayloadString)Context.GC.Heap[_obj.searchPropertyNamePtr].facility).Str;

							ReadOnlySpan<char> searchName;
							Span<char> temp = stackalloc char[16];
							if (_obj.searchPropertyName.ValueType == BoxType.HeapPtr)
							{
								searchName = ((RtPayloadString)Context.GC.Heap[_obj.searchPropertyName.HeapPtr].facility).Str;
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
								ns.SetHeapPtr(_obj.searchNameSpacePtr);
								RtHeapInstance ns_instance = Context.GC.Heap[_obj.searchNameSpacePtr];
								@namespace = ((RtPayloadNameSpace)ns_instance.facility).ASNamespace;


								RaiseReferenceError_RTQNameNotFound(ref error, ns, searchName, _obj.RefInstance);

							}
							else
							{


								//查找原始类型的原型链
								var proto = Context.GC.Heap[((RtPayloadScriptClass)Context.GC.Heap[primitiveCls.__instance_index__].facility).PROTO__PTR];
							lbl_searh_class_proto:
								NaNBoxing value; int shape_ptr; int index; RtPayloadDynamic prop;
								if (FindDynamicValue(proto, searchName, out value, out shape_ptr, out index, out prop))
								{
									result = value;
								}
								else
								{
									int p = ((RtPayloadInstance)proto.facility).PROTOTYPE(this,(ASInstance)proto.Type); //class的proto肯定是instance。
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
#if DEBUG
				else if (
					//rtHeap.TypeKind == RtHeapTypeKind.CACHE_LD_CLASS || 
					rtHeap.TypeKind == RtHeapTypeKind.SHAPE)
				{
					throw new InvalidOperationException();
				}
#endif
			}
			return result;
		}


		private void ReadInstanceFromStacklocater(ref ReceiveError error, StackLocater src, Span<NaNBoxing> stackslots, int stackStPos, int scope_ptr,
			out RtHeapTypeKind kind, out NaNBoxing instance, out ASContainer type)
		{
			//#if DEBUG
			//            if (src.index >= 0)
			//            {
			//                var test = LoadValue(stackslots[src.index], ref error);

			//                if (test.ValueType != NaNBoxing.BoxType.HeapPtr)
			//                {
			//                    throw new InvalidOperationException();
			//                }
			//            }
			//#endif


			if (src.index >= 0)
			{
				//***若instance是一个成员的引用，还需要在此解开。

				//instance = LoadValue(stackslots[src.index], ref error,ref stackslots,stackStPos);

				//if (error.raised)
				//{
				//    kind = (RtHeapTypeKind)255;

				//    type = null;
				//    instance = default(NaNBoxing);

				//    return;
				//}

				instance = stackslots[src.index];

				if (instance.ValueType == NaNBoxing.BoxType.HeapPtr)
				{
					//instancePtr = instance.HeapPtr;
#if DEBUG
					RtHeapInstance rtHeap = Context.GC.Heap[instance.HeapPtr];

					if (rtHeap.TypeKind == RtHeapTypeKind.STACK_CACHE_OBJ
						//||
						//rtHeap.TypeKind == RtHeapTypeKind.CACHE_LD_CLASS
						||
						rtHeap.TypeKind == RtHeapTypeKind.SHAPE
						)
					{
						throw new InvalidOperationException();
					}
#endif

				}
				else
				{
					kind = (RtHeapTypeKind)255;
					type = null;
					//instancePtr = 0;
					//return instance;
					return;
				}



			}
			else
			{
				//沿scope链查找

				int scopeid = -src.index - 1;

				var o = Context.GC.Heap[scope_ptr];
				int instancePtr = scope_ptr;
				do
				{
					if (o.TypeKind == RtHeapTypeKind.MethodScope)
					{
						if (o.Type._link_codescope.index != scopeid)
						{
							RtPayloadMethodScope rtPayload = (RtPayloadMethodScope)o.facility;
							o = Context.GC.Heap[rtPayload.ParentPtr];
							instancePtr = rtPayload.ParentPtr;
						}
						else
						{
							break;
						}
					}
					else if (o.TypeKind == RtHeapTypeKind.INSTANCE)
					{
						RtPayloadInstance heap = (RtPayloadInstance)o.facility;
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
					else if (o.TypeKind == RtHeapTypeKind.CLASS || o.TypeKind == RtHeapTypeKind.GLOBAL)
					{
						var codeScope = ((RtPayloadScriptClass)o.facility).Meta._link_codescope;
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
				instance.SetHeapPtr(instancePtr);

			}

			var temp = Context.GC.Heap[instance.HeapPtr];
			kind = temp.TypeKind;

			switch (kind)
			{
				case RtHeapTypeKind.CLASS:
				case RtHeapTypeKind.GLOBAL:
					type = ((RtPayloadScriptClass)temp.facility).Meta;
					break;
				case RtHeapTypeKind.STRING:
					type = Context.STRING.Instance;
					break;
				case RtHeapTypeKind.INSTANCE:
					type = temp.Type;
					break;
				case RtHeapTypeKind.VECTOR:
					type = temp.Type;
					break;
				case RtHeapTypeKind.ARRAY:
					type = temp.Type;
					break;
				case RtHeapTypeKind.MethodScope:
					type = temp.Type;
					break;
				case RtHeapTypeKind.CLOSURE:
					type = temp.Type;
					break;
				case RtHeapTypeKind.NAMESPACE:
					type = Context.NAMESPACE.Instance;
					break;
					//throw new NotImplementedException();
				case RtHeapTypeKind.STACK_CACHE_OBJ:
				//case RtHeapTypeKind.CACHE_LD_CLASS:
				default:
#if DEBUG
                    throw new InvalidOperationException();
#else
					Environment.FailFast("出错了，这里跑不到"); type = null; return;
#endif
			}

			return;// instance;
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


		internal bool IsEqual(NaNBoxing v1, NaNBoxing v2, StackLocater tempStore, ref ReceiveError error,
			int scope_ptr, Span<NaNBoxing> stackslots, int stackStPos, NaNBoxing caller_bindthis_ptr
			)
		{
			bool fast_comp;
			if (v1.FastTestComp(v2, out fast_comp))
			{
				return fast_comp;
			}


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
				var ins1 = Context.GC.Heap[v1.HeapPtr];
				var ins2 = Context.GC.Heap[v2.HeapPtr];

				if (ins1.TypeKind == RtHeapTypeKind.STRING && ins2.TypeKind == RtHeapTypeKind.STRING)
				{
					return string.CompareOrdinal(((RtPayloadString)ins1.facility).Str, ((RtPayloadString)ins2.facility).Str) == 0;
				}
				else if (ins1.TypeKind == RtHeapTypeKind.INSTANCE && ins2.TypeKind == RtHeapTypeKind.INSTANCE)
				{
					if (((ASInstance)ins1.Type).Flags.HasFlag(ClassFlags.Struct))
					{
						var layoutsize = ins1.Type._link_codescope.TypeLayout.Size;

						return ((RtPayloadInstance)ins1.facility).GetStoreData(this,(ASInstance)ins1.Type).Slice(0, layoutsize).SequenceEqual(
								 ((RtPayloadInstance)ins2.facility).GetStoreData(this,(ASInstance)ins2.Type).Slice(0, layoutsize))
							;

					}
					else if (((ASInstance)ins1.Type).Flags.HasFlag(ClassFlags.Wapper))
					{
						return ((RtPayloadInstance)ins1.facility).wapperedObject.Equals(((RtPayloadInstance)ins2.facility).wapperedObject);
					}
					else
					{
						RtPayloadInstance tmp1, tmp2;
						return RtPayloadInstance.FindAndUpdateHeapInstancePtr(v1.HeapPtr, this, out tmp1) ==
							RtPayloadInstance.FindAndUpdateHeapInstancePtr(v2.HeapPtr, this, out tmp2);

						//return v1.HeapPtr == v2.HeapPtr;
					}
				}
				else if (ins1.TypeKind != RtHeapTypeKind.STRING && ins2.TypeKind != RtHeapTypeKind.STRING)
				{
					if (ins1.TypeKind != ins2.TypeKind)
						return false;

					switch (ins1.TypeKind)
					{
						case RtHeapTypeKind.CLASS:
						case RtHeapTypeKind.GLOBAL:
						case RtHeapTypeKind.NAMESPACE:
							return v1.HeapPtr == v2.HeapPtr;
						case RtHeapTypeKind.ARRAY:
							{
								RtPayloadArray tmp1, tmp2;
								return RtPayloadArray.FindAndUpdateHeapInstancePtr(v1.HeapPtr, this, out tmp1) ==
									RtPayloadArray.FindAndUpdateHeapInstancePtr(v2.HeapPtr, this, out tmp2);
							}
						case RtHeapTypeKind.VECTOR:
							return v1.HeapPtr == v2.HeapPtr;
						case RtHeapTypeKind.CLOSURE:
							{
								RtPayloadClosure tmp1, tmp2;
								int ptr1 = RtPayloadClosure.FindAndUpdateHeapInstancePtr(v1.HeapPtr, this, out tmp1);
								int ptr2 = RtPayloadClosure.FindAndUpdateHeapInstancePtr(v2.HeapPtr, this, out tmp2);

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
				var ins2 = Context.GC.Heap[v2.HeapPtr];
				if (ins2.TypeKind == RtHeapTypeKind.STRING)
				{
					string str2 = ((RtPayloadString)ins2.facility).Str;
					
					// 使用高效的字符比较，避免创建LocalString的字符串
					Span<char> chars1 = stackalloc char[16];
					int charCount1 = v1.GetLocalStringChars(chars1);
					if (charCount1 < 0) return false; // 解码失败
					
					return str2.AsSpan().SequenceEqual(chars1.Slice(0, charCount1));
				}
			}
			else if (v1.ValueType == NaNBoxing.BoxType.HeapPtr && v2.ValueType == NaNBoxing.BoxType.LocalString)
			{
				var ins1 = Context.GC.Heap[v1.HeapPtr];
				if (ins1.TypeKind == RtHeapTypeKind.STRING)
				{
					string str1 = ((RtPayloadString)ins1.facility).Str;
					
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
				{//v2肯定是字符串
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
			if (v1.ValueType == BoxType.HeapPtr && IsNumeric(v2))
			{
				ConvertValueType(ref error, v1, TypeKind.Number, Context.NUMBER, ref v1); //这里不会出错
			}

			if (v2.ValueType == BoxType.HeapPtr && IsNumeric(v1))
			{
				ConvertValueType(ref error, v2, TypeKind.Number, Context.NUMBER, ref v2);  //这里不会出错
			}


			goto flag_retest;
		}



		/// <summary>
		/// 是否严格相等
		/// </summary>
		/// <param name="key1"></param>
		/// <param name="key2"></param>
		/// <returns></returns>
		/// <exception cref="NotImplementedException"></exception>
		
		internal bool IsStrictlyEqual(NaNBoxing key1, NaNBoxing key2)
		{
			bool fast_comp;
			if (key1.FastTestComp(key2,out fast_comp))
			{
				return fast_comp;
			}

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
				var ins1 = Context.GC.Heap[key1.HeapPtr];
				var ins2 = Context.GC.Heap[key2.HeapPtr];

				if (ins1.TypeKind != ins2.TypeKind)
				{
					return false;
				}
				else if (ins1.TypeKind == RtHeapTypeKind.STRING)
				{
					return string.CompareOrdinal(((RtPayloadString)ins1.facility).Str, ((RtPayloadString)ins2.facility).Str) == 0;
				}
				else if (ins1.TypeKind == RtHeapTypeKind.INSTANCE)
				{
					if (((ASInstance)ins1.Type).Flags.HasFlag(ClassFlags.Struct))
					{
						var layoutsize = ins1.Type._link_codescope.TypeLayout.Size;

						return ((RtPayloadInstance)ins1.facility).GetStoreData(this,(ASInstance)ins1.Type).Slice(0, layoutsize).SequenceEqual(
								 ((RtPayloadInstance)ins2.facility).GetStoreData(this,(ASInstance)ins2.Type).Slice(0, layoutsize))
							;

					}
					else if (((ASInstance)ins1.Type).Flags.HasFlag(ClassFlags.Wapper))
					{
						return ((RtPayloadInstance)ins1.facility).wapperedObject.Equals(((RtPayloadInstance)ins2.facility).wapperedObject);
					}
					else
					{
						RtPayloadInstance tmp1, tmp2;
						return RtPayloadInstance.FindAndUpdateHeapInstancePtr(key1.HeapPtr, this, out tmp1) ==
							RtPayloadInstance.FindAndUpdateHeapInstancePtr(key2.HeapPtr, this, out tmp2);
						//return key1.HeapPtr == key2.HeapPtr;
					}
				}
				else
				{
					switch (ins1.TypeKind)
					{
						case RtHeapTypeKind.CLASS:
						case RtHeapTypeKind.GLOBAL:
						case RtHeapTypeKind.NAMESPACE:
							return key1.HeapPtr == key2.HeapPtr;
						case RtHeapTypeKind.ARRAY:
							{
								RtPayloadArray tmp1, tmp2;
								return RtPayloadArray.FindAndUpdateHeapInstancePtr(key1.HeapPtr, this, out tmp1) ==
									RtPayloadArray.FindAndUpdateHeapInstancePtr(key2.HeapPtr, this, out tmp2);
							}
						case RtHeapTypeKind.VECTOR:

							{
								RtPayloadVector tmp1, tmp2;
								int v1 = RtPayloadVector.FindAndUpdateHeapInstancePtr(key1.HeapPtr, this, out tmp1);
								int v2 = RtPayloadVector.FindAndUpdateHeapInstancePtr(key2.HeapPtr, this, out tmp2);

								return v1 == v2;

							}

						case RtHeapTypeKind.CLOSURE:
							{
								RtPayloadClosure tmp1, tmp2;
								int ptr1 = RtPayloadClosure.FindAndUpdateHeapInstancePtr(key1.HeapPtr, this, out tmp1);
								int ptr2 = RtPayloadClosure.FindAndUpdateHeapInstancePtr(key2.HeapPtr, this, out tmp2);

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
					var ins2 = Context.GC.Heap[key2.HeapPtr];
					if (ins2.TypeKind == RtHeapTypeKind.STRING)
					{
						string str2 = ((RtPayloadString)ins2.facility).Str;
						
						// 使用高效的字符比较，避免创建LocalString的字符串
						Span<char> chars1 = stackalloc char[16];
						int charCount1 = key1.GetLocalStringChars(chars1);
						if (charCount1 < 0) return false; // 解码失败
						
						return str2.AsSpan().SequenceEqual(chars1.Slice(0, charCount1));
					}
				}
				else if (key1.ValueType == NaNBoxing.BoxType.HeapPtr && key2.ValueType == NaNBoxing.BoxType.LocalString)
				{
					var ins1 = Context.GC.Heap[key1.HeapPtr];
					if (ins1.TypeKind == RtHeapTypeKind.STRING)
					{
						string str1 = ((RtPayloadString)ins1.facility).Str;
						
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
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public void ConvertValueType(ref ReceiveError error, NaNBoxing invalue, TypeKind totype, ASClass @totype_class, ref NaNBoxing outvalue , int scope_ptr = 0,NaNBoxing callee_bindthis = default,bool is_from_objtostring=false)
		{
			RtHeapInstance to_invoke = null;
			HINT hint = HINT.h_number;

			if (totype_class !=null && totype_class.Instance.Flags.HasFlag(ClassFlags.Vector))
			{
				totype = TypeKind.Vector;
			}

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
								if (Context.GC.Heap[invalue.HeapPtr].TypeKind == RtHeapTypeKind.STRING
									&&
									string.IsNullOrEmpty(((RtPayloadString)Context.GC.Heap[invalue.HeapPtr].facility).Str)
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
								var instance = Context.GC.Heap[invalue.HeapPtr];
								if (instance.TypeKind == RtHeapTypeKind.STRING)
								{
									var str = ((RtPayloadString)instance.facility).Str;
									int v = ReadIntFromString(str);
									outvalue.SetSByte((sbyte)v);
								}
								else if (instance.TypeKind == RtHeapTypeKind.INSTANCE)
								{
									if (scope_ptr == 0)
									{
										outvalue.SetSByte(0);
									}
									else
									{
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
								var instance = Context.GC.Heap[invalue.HeapPtr];
								if (instance.TypeKind == RtHeapTypeKind.STRING)
								{
									var str = ((RtPayloadString)instance.facility).Str;
									uint v = ReadUIntFromString(str);
									outvalue.SetByte((byte)v);
								}
								else if (instance.TypeKind == RtHeapTypeKind.INSTANCE)
								{
									if (scope_ptr == 0)
									{
										outvalue.SetByte(0);
									}
									else
									{
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
								var instance = Context.GC.Heap[invalue.HeapPtr];
								if (instance.TypeKind == RtHeapTypeKind.STRING)
								{
									var str = ((RtPayloadString)instance.facility).Str;
									int v = ReadIntFromString(str);
									outvalue.SetShort((short)v);
								}
								else if (instance.TypeKind == RtHeapTypeKind.INSTANCE)
								{
									if (scope_ptr == 0)
									{
										outvalue.SetShort(0);
									}
									else
									{
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
								var instance = Context.GC.Heap[invalue.HeapPtr];
								if (instance.TypeKind == RtHeapTypeKind.STRING)
								{
									var str = ((RtPayloadString)instance.facility).Str;
									uint v = ReadUIntFromString(str);
									outvalue.SetUShort((ushort)v);
								}
								else if (instance.TypeKind == RtHeapTypeKind.INSTANCE)
								{
									if (scope_ptr == 0)
									{
										outvalue.SetUShort(0);
									}
									else
									{
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
								var instance = Context.GC.Heap[invalue.HeapPtr];
								if (instance.TypeKind == RtHeapTypeKind.STRING)
								{
									var str = ((RtPayloadString)instance.facility).Str;
									int v = ReadIntFromString(str);
									outvalue.SetInt(v);
								}
								else if (instance.TypeKind == RtHeapTypeKind.INSTANCE)
								{
									if (scope_ptr == 0)
									{
										outvalue.SetInt(0);
									}
									else
									{
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
								var instance = Context.GC.Heap[invalue.HeapPtr];
								if (instance.TypeKind == RtHeapTypeKind.STRING)
								{
									var str = ((RtPayloadString)instance.facility).Str;
									uint v = ReadUIntFromString(str);
									outvalue.SetUInt(v);
								}
								else if (instance.TypeKind == RtHeapTypeKind.INSTANCE)
								{
									if (scope_ptr == 0)
									{
										outvalue.SetUInt(0);
									}
									else
									{
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
								var instance = Context.GC.Heap[invalue.HeapPtr];
								if (instance.TypeKind == RtHeapTypeKind.STRING)
								{
									var str = ((RtPayloadString)instance.facility).Str;
									double v = ReadDoubleFromString(str);
									outvalue.SetFloat((float)v);
								}
								else if (instance.TypeKind == RtHeapTypeKind.INSTANCE)
								{
									if (scope_ptr == 0)
									{
										outvalue.SetFloat(float.NaN);
									}
									else
									{
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
								var instance = Context.GC.Heap[invalue.HeapPtr];
								if (instance.TypeKind == RtHeapTypeKind.STRING)
								{
									var str = ((RtPayloadString)instance.facility).Str;
									double v = ReadDoubleFromString(str);
									outvalue.SetNumber(v);
								}
								else if (instance.TypeKind == RtHeapTypeKind.INSTANCE)
								{
									if (scope_ptr == 0)
									{
										outvalue.SetNumber(double.NaN);
									}
									else
									{
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
									outvalue.SetHeapPtr(NAN_STR);
									return;
								}
								else if (double.IsPositiveInfinity(invalue.Number))
								{
									outvalue.SetHeapPtr(POSITIVEINF_STR);
									return;
								}
								else if (double.IsNegativeInfinity(invalue.Number))
								{
									outvalue.SetHeapPtr(NEGATIVEINF_STR);
									return;
								}
								else
								{
									string str = invalue.Number.ToString();
									// 使用辅助函数优化字符串创建
									if (!TryCreateStringValue(str, out outvalue, ref error))
									{
										return; // 错误已经在TryCreateStringValue中处理
									}
									return;
								}
							}
						case NaNBoxing.BoxType.Undefined:
							outvalue.SetNull();
							return;
						case NaNBoxing.BoxType.Null:
							outvalue.SetNull();
							return;
						case NaNBoxing.BoxType.Boolean:
							if (invalue.Boolean)
							{
								outvalue.SetHeapPtr(TRUE_STR);
							}
							else
							{
								outvalue.SetHeapPtr(FALSE_STR);
							}
							return;
						case NaNBoxing.BoxType.Int:
							{
								string str = invalue.IntValue.ToString();
								// 使用辅助函数优化字符串创建
								if (!TryCreateStringValue(str, out outvalue, ref error))
								{
									return; // 错误已经在TryCreateStringValue中处理
								}
								return;
							}
						case NaNBoxing.BoxType.Uint:
							{
								string str = invalue.UIntValue.ToString();
								// 使用辅助函数优化字符串创建
								if (!TryCreateStringValue(str, out outvalue, ref error))
								{
									return; // 错误已经在TryCreateStringValue中处理
								}
								return;
							}
						case NaNBoxing.BoxType.Sbyte:
							{
								string str = invalue.SByteValue.ToString();
								// 使用辅助函数优化字符串创建
								if (!TryCreateStringValue(str, out outvalue, ref error))
								{
									return; // 错误已经在TryCreateStringValue中处理
								}
								return;
							}
						case NaNBoxing.BoxType.Byte:
							{
								string str = invalue.ByteValue.ToString();
								// 使用辅助函数优化字符串创建
								if (!TryCreateStringValue(str, out outvalue, ref error))
								{
									return; // 错误已经在TryCreateStringValue中处理
								}
								return;
							}
						case NaNBoxing.BoxType.Short:
							{
								string str = invalue.ShortValue.ToString();
								// 使用辅助函数优化字符串创建
								if (!TryCreateStringValue(str, out outvalue, ref error))
								{
									return; // 错误已经在TryCreateStringValue中处理
								}
								return;
							}
						case NaNBoxing.BoxType.UShort:
							{
								string str = invalue.UShortValue.ToString();
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
									outvalue.SetHeapPtr(NAN_STR);
									return;
								}
								else if (float.IsPositiveInfinity(invalue.FloatValue))
								{
									outvalue.SetHeapPtr(POSITIVEINF_STR);
									return;
								}
								else if (float.IsNegativeInfinity(invalue.FloatValue))
								{
									outvalue.SetHeapPtr(NEGATIVEINF_STR);
									return;
								}
								else
								{
									string str = invalue.FloatValue.ToString();
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
								var instance = Context.GC.Heap[invalue.HeapPtr];
								switch (instance.TypeKind)
								{
									case RtHeapTypeKind.CLASS:
										{
											int ptr = Context.GC.AllocString($"[class {((RtPayloadScriptClass)instance.facility).Meta.QName.Name}]");
											if (ptr == 0)
											{
												RaiseOutOfMemory(ref error);
												return;
											}
											else
											{
												outvalue.SetHeapPtr(ptr);
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
												outvalue.SetHeapPtr(ptr);
												return;
											}
										}
									case RtHeapTypeKind.STRING:
										outvalue = invalue;
										return;
									case RtHeapTypeKind.INSTANCE:
										{
											if (scope_ptr == 0)
											{
												if (Extensions.IsExtend((ASInstance)instance.Type,Context.ERROR.Instance))
												{
													var msg = ((RtPayloadInstance)instance.facility).ReadSlot(0, instance.Type._link_codescope, this);
													int ptr = Context.GC.AllocString($"{instance.Type.QName.Name}: { Extensions.GetPrimitiveValueToString( this,msg) }");
													if (ptr == 0)
													{
														RaiseOutOfMemory(ref error);
														return;
													}
													else
													{
														outvalue.SetHeapPtr(ptr);
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
														outvalue.SetHeapPtr(ptr);
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
											ASNamespace ns = ((RtPayloadNameSpace)instance.facility).ASNamespace;
											int ptr = Context.GC.AllocString(string.IsNullOrEmpty(ns.def_uri) ? ns.Name : ns.def_uri);
											if (ptr == 0)
											{
												RaiseOutOfMemory(ref error);
												return;
											}
											else
											{
												outvalue.SetHeapPtr(ptr);
												return;
											}
										}
									case RtHeapTypeKind.ARRAY:
										{

											int ptr = Context.GC.AllocString("[object Array]");
											if (ptr == 0)
											{
												RaiseOutOfMemory(ref error);
												return;
											}
											else
											{
												outvalue.SetHeapPtr(ptr);
												return;
											}
										}
									case RtHeapTypeKind.VECTOR:
										{

											int ptr = Context.GC.AllocString($"[object Vector.<{(((RtPayloadVector)instance.facility).element_asclass == null? "*" : ((RtPayloadVector)instance.facility).element_asclass.QName.ToDebugTypeName())}>]");
											if (ptr == 0)
											{
												RaiseOutOfMemory(ref error);
												return;
											}
											else
											{
												outvalue.SetHeapPtr(ptr);
												return;
											}
										}
									case RtHeapTypeKind.CLOSURE:
										{
											outvalue.SetHeapPtr( is_from_objtostring? OBJECT_FUNCTION_STR: FUNCTION_TOSTRING_STR);
											return;
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
									RtHeapInstance obj = Context.GC.Heap[invalue.HeapPtr];
									if (obj.TypeKind == RtHeapTypeKind.CLOSURE)
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
									RtHeapInstance obj = Context.GC.Heap[invalue.HeapPtr];
									if (obj.TypeKind == RtHeapTypeKind.CLASS)
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
								RtHeapInstance obj = Context.GC.Heap[invalue.HeapPtr];
								if (obj.TypeKind == RtHeapTypeKind.ARRAY)
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
								RtHeapInstance obj = Context.GC.Heap[invalue.HeapPtr];
								if (totype == TypeKind.Namespace)
								{
									if (obj.TypeKind == RtHeapTypeKind.NAMESPACE)
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
									if (obj.TypeKind == RtHeapTypeKind.VECTOR)
									{
										

										if ( 
											((ASInstance)obj.Type).indexer_get.ReturnTypeKind == totype_class.Instance.indexer_get.ReturnTypeKind
											//(ulong)((RtPayloadVector)obj.facility).element_type == totype_class.Instance._element_class.Type_identifier
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
							var obj = Context.GC.Heap[invalue.HeapPtr];
							if (obj.TypeKind == RtHeapTypeKind.INSTANCE) //只有对象实例才可能满足条件。
							{
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
							else if (obj.TypeKind == RtHeapTypeKind.VECTOR)
							{
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
				//			invalue, ((RtPayloadClosure)funinstance.facility).ScopePtr, ((RtPayloadClosure)funinstance.facility).ScopeType, 0, null, null, ref error, stPos + 1, fun.HeapPtr);
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

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private unsafe void ExplicitConvert(ref ReceiveError error, ushort argsCount, StackLocater* arguments , Span<NaNBoxing> slots ,TypeKind totype, ASClass @totype_class, ref NaNBoxing outvalue, int returnSlotindex,			
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

				((RtPayloadArray)instance.facility).array_len = 0;
				((RtPayloadArray)instance.facility).methodscopeslot_ref_state = 0;
				((RtPayloadArray)instance.facility).HEAPINSTANCE_PTR = 0;

				//Span<NaNBoxing> slots = Context.StackSlots.AsSpan(Context.StackPosition, 2);
				//Context.StackPosition += 2;

				//slots[0].SetHeapPtr(instancePtr);
				//slots[1] = invalue;

				NaNBoxing invalue = default;
				invalue.SetHeapPtr(instancePtr);

				unsafe
				{
					//构造
					RunMethod(totype_class.Instance.Constructor, invalue, instancePtr, totype_class.Instance, argsCount, (byte*)arguments, slots, ref error, Context.StackPosition - 1);
				}
				Context.StackPosition -= 1;
				if (error.raised)
				{
					return;
				}
				outvalue.SetHeapPtr(instancePtr);
				return;
			}
			else if (
				argsCount <2
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
					errPtr = InitCacheInstance(totype_class, returnSlotindex, true);
				}
				else
				{
					RtHeapInstance _temp;
					errPtr = Context.GC.AllocInstance(totype_class.Instance, out _temp);
					if (errPtr == 0)
					{
						RaiseOutOfMemory(ref error);
						return;
					}
				}

				NaNBoxing invalue = default;
				invalue.SetHeapPtr(errPtr);

				//slots[0].SetHeapPtr(errPtr);
				//slots[1] = invalue;
				unsafe
				{
					//构造
					//StackLocater send_arg = new StackLocater() { index = 1 };
					RunMethod(totype_class.Instance.Constructor, invalue, errPtr, totype_class.Instance, argsCount, (byte*)arguments, slots, ref error, Context.StackPosition - 1);
				}
				Context.StackPosition -= 1;
				if (error.raised)
				{
					return;
				}

				outvalue.SetHeapPtr(errPtr);


				return;
				//throw new NotImplementedException();
			}
			else if (argsCount == 1)
			{
				var invalue = slots[arguments->index];
				bool istovector = totype_class.Instance.Flags.HasFlag(ClassFlags.Vector);
				if ( istovector && invalue.ValueType == BoxType.HeapPtr && Context.GC.Heap[invalue.HeapPtr].TypeKind == RtHeapTypeKind.ARRAY)
				{
					//转化为Vector
					int ptrIndex = returnSlotindex;

					int instancePtr = Context.CacheVectorPtr + ptrIndex;
					var instance = Context.GC.Heap[instancePtr];

					instance.Type = totype_class.Instance;
					((RtPayloadVector)instance.facility).HEAPINSTANCE_PTR = 0;
					((RtPayloadVector)instance.facility).element_asclass = totype_class.Instance._element_class;
					((RtPayloadVector)instance.facility).element_type = totype_class.Instance._element_class == null ? TypeKind.Any : (TypeKind)totype_class.Instance._element_class.Type_identifier;
					//((RtPayloadVector)instance.facility).GetStore(this).SetBuffer(0);
					((RtPayloadVector)instance.facility).GetStore(this).length = 0;

					Context.StackSlots[returnSlotindex].SetHeapPtr(instancePtr);

					RtPayloadArray srcArr;
					RtPayloadArray.FindAndUpdateHeapInstancePtr(invalue.HeapPtr, this, out srcArr);
					uint len = srcArr.GetLength(this);
					
					unsafe
					{
						Span<NaNBoxing> _tmpslot = stackalloc NaNBoxing[1];
						_tmpslot[0].SetInt((int)len);

						StackLocater l;l.index = 0;
						//构造
						//StackLocater send_arg = new StackLocater() { index = 1 };
						RunMethod(totype_class.Instance.Constructor, Context.StackSlots[returnSlotindex], instancePtr, 
							totype_class.Instance, 1, (byte*)&l, _tmpslot, ref error, returnSlotindex);

						if (error.raised)
						{
							return;
						}
					}

					RtPayloadVector dst;
					int vptr = RtPayloadVector.FindAndUpdateHeapInstancePtr(instancePtr, this, out dst);
					for (int i = 0; i < (int)len; i++)
					{
						bool isoutindex;
						NaNBoxing e = srcArr.ReadSlot((uint)i, this, out isoutindex);

						ConvertValueType(ref error, e, dst.element_type, dst.element_asclass, ref e);

#if DEBUG
						NaNBoxing index = default;index.SetInt(i);
						int _i;int max;
						Debug.Assert( dst.IsValidIndexRange(index, out _i, out max, this));
#endif
						dst.SetSlot(i, this, vptr, e, ref error);
						if (error.raised)
						{
							return;
						}
					}

					outvalue.SetHeapPtr(vptr);
				}
				else
				{
					ConvertValueType(ref error, invalue, totype, @totype_class, ref outvalue, scope_ptr, callee_bindthis);
				}
			}
			else
			{
				RaiseArgementErrorCountMisMatch(ref error, null, 1, argsCount);
			}
		}

		public bool ToBoolean(NaNBoxing invalue)
		{

			switch (invalue.ValueType)
			{
				case NaNBoxing.BoxType.Number:
					return(!(invalue.Number == 0 || double.IsNaN(invalue.Number)));
					
				case NaNBoxing.BoxType.Undefined:
					return	(false);
					
				case NaNBoxing.BoxType.Null:
					return(false);
					
				case NaNBoxing.BoxType.Boolean:
					return invalue.Boolean;
					
				case NaNBoxing.BoxType.Int:
					return(invalue.IntValue != 0);
					
				case NaNBoxing.BoxType.Uint:
					return(invalue.UIntValue != 0);
					
				case NaNBoxing.BoxType.Sbyte:
					return(invalue.SByteValue != 0);
					
				case NaNBoxing.BoxType.Byte:
					return(invalue.ByteValue != 0);
					
				case NaNBoxing.BoxType.Short:
					return(invalue.ShortValue != 0);
					
				case NaNBoxing.BoxType.UShort:
					return(invalue.UShortValue != 0);
					
				case NaNBoxing.BoxType.Float:
					return(!(invalue.FloatValue == 0 || float.IsNaN(invalue.FloatValue)));
					
				case NaNBoxing.BoxType.HeapPtr:
					{
						if (Context.GC.Heap[invalue.HeapPtr].TypeKind == RtHeapTypeKind.STRING
							&&
							string.IsNullOrEmpty(((RtPayloadString)Context.GC.Heap[invalue.HeapPtr].facility).Str)
							)
						{
							return(false);
						}
						else
						{
							return(true);
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


		internal int MultiNameLSearch(ASNamespaceSet ns_set, RtHeapTypeKind kind, ASContainer as_type, string name, StackLocater stack,
			Span<NaNBoxing> stackslots, int stackStPos, NaNBoxing instance, NaNBoxing this_ptr, ref ReceiveError error, bool exclude_user_ns = false
			)
		{
#if FORCOMPILER
			if (IsComputeConstExpr)
			{
				throw new EvalConstException();
			}
#endif

			bool issameorinherit = this_ptr.ValueType == NaNBoxing.BoxType.HeapPtr && instance.ValueType == BoxType.HeapPtr &&

				Context.GC.Heap[this_ptr.HeapPtr].TypeKind == Context.GC.Heap[instance.HeapPtr].TypeKind
				&&
				Context.GC.Heap[this_ptr.HeapPtr].TypeKind == RtHeapTypeKind.INSTANCE
				&&
				((ASInstance)Context.GC.Heap[instance.HeapPtr].Type).IsExtend((ASInstance)Context.GC.Heap[this_ptr.HeapPtr].Type)

				;

			if (instance.ValueType == BoxType.HeapPtr)
			{
				if (Context.GC.Heap[instance.HeapPtr].TypeKind == RtHeapTypeKind.CLASS) // Class['aa'] 这种，可以访问protected属性
				{
					if (this_ptr.ValueType == NaNBoxing.BoxType.HeapPtr && Context.GC.Heap[this_ptr.HeapPtr].TypeKind == RtHeapTypeKind.INSTANCE)
					{
						if (
							((ASInstance)Context.GC.Heap[this_ptr.HeapPtr].Type).IsExtend(
							((ASClass)((RtPayloadScriptClass)Context.GC.Heap[instance.HeapPtr].facility).Meta).Instance)
							)
						{
							issameorinherit = true;
						}
					}
				}
			}

			//lambda search member
			var findMembers = (CodeScope scope, string name, out int index) =>
			{
				index = -1;
				int count = 0;
				ASContainer defat = null;
				for (int i = scope.Members.Count - 1; i >= 0; i--)
				{
					var member = scope.Members[i];
					if (member.QName.Name == name
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
			};


			var findvtable = (VTable table, string name, out int m_index, out int g_index, out int s_index, out int m_count, out int g_count, out int s_count) =>
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

					if (item.Trait.QName.Name == name &&
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

			};

			if (instance.HeapPtr == Context.CLASS.__instance_index__)
			{
				kind = RtHeapTypeKind.INSTANCE;
				as_type = Context.CLASS.Instance;
			}

			if (kind == RtHeapTypeKind.CLASS)
			{
				CodeScope cls = as_type._link_codescope;
				int i;
				var count = findMembers(cls, name, out i);

				//查找虚函数表
				int m_idx, m_count, g_idx, g_count, s_idx, s_count;
				findvtable(as_type._vtable, name, out m_idx, out g_idx, out s_idx, out m_count, out g_count, out s_count);

				if (count + m_count + g_count > 1 || count + m_count + s_count > 1)
				{
					goto lbl_multiname_ambiguous;
				}
				else if (count == 1)
				{
					var member = cls.Members[i];

					int ptrIndex = stackStPos + stack.index;
					int cacheobjpointer = Context.CacheObjPtr + ptrIndex;  //Context.CacheObjectPointers[ptrIndex];
					RtHeapInstance cache = Context.GC.Heap[cacheobjpointer];
#if DEBUG
					if (cache.TypeKind != RtHeapTypeKind.STACK_CACHE_OBJ)
					{
						throw new InvalidOperationException();
					}
#endif

					RtPayloadStackCache cachePayload = (RtPayloadStackCache)cache.facility;
					cachePayload.RefInstance = instance;
					cachePayload.trait[0] = member.trait; cachePayload.trait[1] = null;
					cachePayload.scopemember_index = (ushort)i;
					cachePayload.searchPropertyName.SetUndefined(); cachePayload.as_type = as_type;
					cachePayload.searchNameSpacePtr = 0; cachePayload.indexer_key.setFault();

					stackslots[stack.index].SetHeapPtr(cacheobjpointer);

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
					RtPayloadClosure closure = (RtPayloadClosure)Context.GC.Heap[m_closurePtr].facility;
					closure.This.SetNull();
					closure.ScopePtr = instance.HeapPtr;
					closure.ScopeType = vitem.DefineAt;
					closure._ref_as_type = as_type;
					closure.methodscopeslot_ref_state = 0; closure.HEAPINSTANCE_PTR = 0;
					stackslots[stack.index].SetHeapPtr(m_closurePtr);

					goto lbl_multiname_success;

				}
				else if (g_count == 1 || s_count == 1)
				{
					int ptrIndex = stackStPos + stack.index;
					int cacheobjpointer = Context.CacheObjPtr + ptrIndex;
					RtHeapInstance cache = Context.GC.Heap[cacheobjpointer];
#if DEBUG
					if (cache.TypeKind != RtHeapTypeKind.STACK_CACHE_OBJ)
					{
						throw new InvalidOperationException();
					}
#endif

					RtPayloadStackCache cachePayload = (RtPayloadStackCache)cache.facility;
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

					stackslots[stack.index].SetHeapPtr(cacheobjpointer);

					goto lbl_multiname_success;

					//throw new NotImplementedException();
				}
				else
				{
					Context.GC.CheckGC(ref error);

					NaNBoxing searchPtr = default;
					if (string.CompareOrdinal(name, "valueOf") == 0)
					{
						searchPtr.SetHeapPtr( VALUEOF_STR);
					}
					else if (string.CompareOrdinal(name, "toString") == 0)
					{
						searchPtr.SetHeapPtr ( TOSTRING_STR);
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
					RtHeapInstance cache = Context.GC.Heap[cacheobjpointer];
#if DEBUG
					if (cache.TypeKind != RtHeapTypeKind.STACK_CACHE_OBJ)
					{
						throw new InvalidOperationException();
					}
#endif

					RtPayloadStackCache cachePayload = (RtPayloadStackCache)cache.facility;
					cachePayload.RefInstance = instance;
					cachePayload.trait[0] = null; cachePayload.trait[1] = null;
					cachePayload.scopemember_index = 0;
					cachePayload.searchPropertyName = searchPtr; cachePayload.as_type = as_type;
					cachePayload.searchNameSpacePtr = 0; cachePayload.indexer_key.setFault();

					stackslots[stack.index].SetHeapPtr(cacheobjpointer);




					goto lbl_multiname_dynamicprop;
				}
			}
			else if (kind == RtHeapTypeKind.INSTANCE || kind == RtHeapTypeKind.STRING || kind == RtHeapTypeKind.VECTOR || kind == RtHeapTypeKind.ARRAY || kind == RtHeapTypeKind.NAMESPACE || (byte)kind == 255)
			{
				CodeScope type = as_type._link_codescope;
				int i;
				var count = findMembers(type, name, out i);

				//查找虚函数表
				int m_idx, m_count, g_idx, g_count, s_idx, s_count;
				findvtable(as_type._vtable, name, out m_idx, out g_idx, out s_idx, out m_count, out g_count, out s_count);

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
					RtHeapInstance cache = Context.GC.Heap[cacheobjpointer];
#if DEBUG
					if (cache.TypeKind != RtHeapTypeKind.STACK_CACHE_OBJ)
					{
						throw new InvalidOperationException();
					}
#endif

					RtPayloadStackCache cachePayload = (RtPayloadStackCache)cache.facility;
					cachePayload.RefInstance = instance;
					cachePayload.trait[0] = member.trait; cachePayload.trait[1] = null;
					cachePayload.scopemember_index = (ushort)i;
					cachePayload.searchPropertyName.SetUndefined(); cachePayload.as_type = as_type;
					cachePayload.searchNameSpacePtr = 0; cachePayload.indexer_key.setFault();

					stackslots[stack.index].SetHeapPtr(cacheobjpointer);
					goto lbl_multiname_success;
				}
				else if (m_count == 1)
				{
					var vitem = as_type._vtable.Items[m_idx];

					int ptrIndex = stackStPos + stack.index;
					int m_closurePtr = Context.M_ClosurePtr + ptrIndex;

					Context.GC.Heap[m_closurePtr].Type = vitem.Trait.Method.Body;
					RtPayloadClosure closure = (RtPayloadClosure)Context.GC.Heap[m_closurePtr].facility;
					//closure.This.SetHeapPtr(instancePtr);
					//closure.ScopePtr = instancePtr;
					closure.This = instance;
					closure.ScopePtr = instance.ValueType == BoxType.HeapPtr ? instance.HeapPtr : 0;
					closure.ScopeType = vitem.DefineAt;
					closure._ref_as_type = as_type;
					closure.methodscopeslot_ref_state = 0; closure.HEAPINSTANCE_PTR = 0;
					stackslots[stack.index].SetHeapPtr(m_closurePtr);

					goto lbl_multiname_success;
				}
				else if (g_count == 1 || s_count == 1)
				{
					int ptrIndex = stackStPos + stack.index;
					int cacheobjpointer = Context.CacheObjPtr + ptrIndex;
					RtHeapInstance cache = Context.GC.Heap[cacheobjpointer];
#if DEBUG
					if (cache.TypeKind != RtHeapTypeKind.STACK_CACHE_OBJ)
					{
						throw new InvalidOperationException();
					}
#endif

					RtPayloadStackCache cachePayload = (RtPayloadStackCache)cache.facility;
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

					stackslots[stack.index].SetHeapPtr(cacheobjpointer);

					goto lbl_multiname_success;

				}
				else
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

						NaNBoxing searchPtr=default;
						if (string.CompareOrdinal(name, "valueOf") == 0)
						{
							searchPtr.SetHeapPtr( VALUEOF_STR);
						}
						else if (string.CompareOrdinal(name, "toString") == 0)
						{
							searchPtr.SetHeapPtr( TOSTRING_STR);
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
						RtHeapInstance cache = Context.GC.Heap[cacheobjpointer];
#if DEBUG
						if (cache.TypeKind != RtHeapTypeKind.STACK_CACHE_OBJ)
						{
							throw new InvalidOperationException();
						}
#endif

						if (it.Flags.HasFlag(ClassFlags.Indexer) 
							&&
							(
								!it.Flags.HasFlag( ClassFlags.Vector) //Vector的索引器不会处理字符串
							)
							)
						{
							RtPayloadStackCache cachePayload = (RtPayloadStackCache)cache.facility;
							cachePayload.RefInstance = instance;
							cachePayload.trait[0] = null; cachePayload.trait[1] = null;
							cachePayload.scopemember_index = 0;
							cachePayload.searchPropertyName.SetUndefined(); cachePayload.as_type = as_type;
							cachePayload.searchNameSpacePtr = 0; cachePayload.indexer_key = searchPtr;

							stackslots[stack.index].SetHeapPtr(cacheobjpointer);


						}
						else
						{

							RtPayloadStackCache cachePayload = (RtPayloadStackCache)cache.facility;
							cachePayload.RefInstance = instance;
							cachePayload.trait[0] = null; cachePayload.trait[1] = null;
							cachePayload.scopemember_index = 0;
							cachePayload.searchPropertyName = searchPtr; cachePayload.as_type = as_type;
							cachePayload.searchNameSpacePtr = 0; cachePayload.indexer_key.setFault();

							stackslots[stack.index].SetHeapPtr(cacheobjpointer);

						}

						goto lbl_multiname_dynamicprop;
					}

				}


			}
			else if (kind == RtHeapTypeKind.GLOBAL)
			{
				CodeScope type = as_type._link_codescope;
				int i;
				var count = findMembers(type, name, out i);
				if (count == 0) //dynamic property
				{
					Context.GC.CheckGC(ref error);

					NaNBoxing searchPtr = default;
					if (string.CompareOrdinal(name, "valueOf") == 0)
					{
						searchPtr.SetHeapPtr(VALUEOF_STR);
					}
					else if (string.CompareOrdinal(name, "toString") == 0)
					{
						searchPtr.SetHeapPtr(TOSTRING_STR);
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
					RtHeapInstance cache = Context.GC.Heap[cacheobjpointer];
#if DEBUG
					if (cache.TypeKind != RtHeapTypeKind.STACK_CACHE_OBJ)
					{
						throw new InvalidOperationException();
					}
#endif

					RtPayloadStackCache cachePayload = (RtPayloadStackCache)cache.facility;
					cachePayload.RefInstance = instance;
					cachePayload.trait[0] = null; cachePayload.trait[1] = null;
					cachePayload.scopemember_index = 0;
					cachePayload.searchPropertyName = searchPtr; cachePayload.as_type = as_type;
					cachePayload.searchNameSpacePtr = 0; cachePayload.indexer_key.setFault();

					stackslots[stack.index].SetHeapPtr(cacheobjpointer);

					goto lbl_multiname_dynamicprop;
				}
				else if (count == 1)
				{
					var member = type.Members[i];

					int ptrIndex = stackStPos + stack.index;
					int cacheobjpointer = Context.CacheObjPtr + ptrIndex;  //Context.CacheObjectPointers[ptrIndex];
					RtHeapInstance cache = Context.GC.Heap[cacheobjpointer];
#if DEBUG
					if (cache.TypeKind != RtHeapTypeKind.STACK_CACHE_OBJ)
					{
						throw new InvalidOperationException();
					}
#endif

					RtPayloadStackCache cachePayload = (RtPayloadStackCache)cache.facility;
					cachePayload.RefInstance = instance;
					cachePayload.trait[0] = member.trait; cachePayload.trait[1] = null;
					cachePayload.scopemember_index = (ushort)i;
					cachePayload.searchPropertyName.SetUndefined(); cachePayload.as_type = as_type;
					cachePayload.searchNameSpacePtr = 0; cachePayload.indexer_key.setFault();

					stackslots[stack.index].SetHeapPtr(cacheobjpointer);

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
				findvtable(Context.FUNCTION.Instance._vtable, name, out m_idx, out g_idx, out s_idx, out m_count, out g_count, out s_count);
				if (m_count == 1)
				{
					var vitem = Context.FUNCTION.Instance._vtable.Items[m_idx];

					int ptrIndex = stackStPos + stack.index;
					int m_closurePtr = Context.M_ClosurePtr + ptrIndex;

					Context.GC.Heap[m_closurePtr].Type = vitem.Trait.Method.Body;
					RtPayloadClosure closure = (RtPayloadClosure)Context.GC.Heap[m_closurePtr].facility;
					closure.This.SetHeapPtr(instance.HeapPtr);
					closure.ScopePtr = instance.HeapPtr;
					closure.ScopeType = vitem.DefineAt;
					closure._ref_as_type = Context.FUNCTION.Instance;
					closure.methodscopeslot_ref_state = 0; closure.HEAPINSTANCE_PTR = 0;
					stackslots[stack.index].SetHeapPtr(m_closurePtr);

					goto lbl_multiname_success;
				}
				else if (g_count == 1 || s_count == 1)
				{
					int ptrIndex = stackStPos + stack.index;
					int cacheobjpointer = Context.CacheObjPtr + ptrIndex;
					RtHeapInstance cache = Context.GC.Heap[cacheobjpointer];
#if DEBUG
					if (cache.TypeKind != RtHeapTypeKind.STACK_CACHE_OBJ)
					{
						throw new InvalidOperationException();
					}
#endif

					RtPayloadStackCache cachePayload = (RtPayloadStackCache)cache.facility;
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

					stackslots[stack.index].SetHeapPtr(cacheobjpointer);

					goto lbl_multiname_success;

				}
				else
				{

					Context.GC.CheckGC(ref error);

					NaNBoxing searchPtr = default;
					if (string.CompareOrdinal(name, "valueOf") == 0)
					{
						searchPtr.SetHeapPtr(VALUEOF_STR);
					}
					else if (string.CompareOrdinal(name, "toString") == 0)
					{
						searchPtr.SetHeapPtr(TOSTRING_STR);
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
					RtHeapInstance cache = Context.GC.Heap[cacheobjpointer];
#if DEBUG
					if (cache.TypeKind != RtHeapTypeKind.STACK_CACHE_OBJ)
					{
						throw new InvalidOperationException();
					}
#endif

					RtPayloadStackCache cachePayload = (RtPayloadStackCache)cache.facility;
					cachePayload.RefInstance = instance;
					cachePayload.trait[0] = null; cachePayload.trait[1] = null;
					cachePayload.scopemember_index = 0;
					cachePayload.searchPropertyName = searchPtr; cachePayload.as_type = as_type;
					cachePayload.searchNameSpacePtr = 0; cachePayload.indexer_key.setFault();

					stackslots[stack.index].SetHeapPtr(cacheobjpointer);

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
		internal unsafe void CreateDynamic(ref ReceiveError error, RtHeapInstance instance, NaNBoxing propname, NaNBoxing value, bool configurable, bool enumerable, bool writeable)
		{
			int PROPERTY_PTR = GetPropertyPtr(instance);

#if DEBUG
			if (instance.TypeKind == RtHeapTypeKind.INSTANCE)
			{
				if (((ASInstance)instance.Type).Flags.HasFlag(ClassFlags.Struct))
				{
					throw new InvalidOperationException();
				}
			}

#endif
			//string name = ((RtPayloadString)Context.GC.Heap[propname_ptr].facility).Str;
			ReadOnlySpan<char> name;
			Span<char> temp = stackalloc char[16];
			if (propname.ValueType == BoxType.LocalString)
			{
				int l = propname.GetLocalStringChars(temp);
				name = temp.Slice(0,l);
			}
			else
			{
				name = ((RtPayloadString)Context.GC.Heap[propname.HeapPtr].facility).Str;
			}


			RtPayloadShape.PropertyAttribute attribute = 0;
			if (configurable)
				attribute |= RtPayloadShape.PropertyAttribute.Configurable;
			if (enumerable)
				attribute |= RtPayloadShape.PropertyAttribute.Enumerable;
			if (writeable)
				attribute |= RtPayloadShape.PropertyAttribute.Writable;


			if (PROPERTY_PTR == 0)
			{
				

				//先创建或者查找Shape。(第一个属性就是)
				var blank_shape = (RtPayloadShape)Context.GC.Heap[Context.BlankShapePtr].facility;

				var ptr = blank_shape.PTR_CHILD;
				RtPayloadShape shape;
				while (ptr != 0)
				{
					shape = (RtPayloadShape)Context.GC.Heap[ptr].facility;
					if (
						//shape.Attribute.HasFlag(
						//RtPayloadShape.PropertyAttribute.Configurable |
						//RtPayloadShape.PropertyAttribute.Enumerable |
						//RtPayloadShape.PropertyAttribute.Writable)

						shape.Attribute == attribute

						&&

						//string.Equals(((RtPayloadString)Context.GC.Heap[shape.PTR_NAME].facility).Str,
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



					shape = (RtPayloadShape)Context.GC.Heap[ptr].facility;

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

				var propDynamic = (RtPayloadDynamic)Context.GC.Heap[prop_ptr].facility;
				propDynamic.SHAPE_PTR = ptr;
				propDynamic.Slots.Add(value);

				Context.GC.UpdateMemUsage_Add(Context.GC.Heap[prop_ptr]);

				if (instance.TypeKind == RtHeapTypeKind.INSTANCE)
				{
					((RtPayloadInstance)instance.facility).Set_PROPERTY_PTR(prop_ptr, this,(ASInstance)instance.Type);
				}
				else if (instance.TypeKind == RtHeapTypeKind.CLASS)
				{
					((RtPayloadScriptClass)instance.facility).PROPERTY_PTR = prop_ptr;
				}
				else if (instance.TypeKind == RtHeapTypeKind.GLOBAL)
				{
					((RtPayloadScriptClass)instance.facility).PROPERTY_PTR = prop_ptr;
				}
				else if (instance.TypeKind == RtHeapTypeKind.ARRAY)
				{
					((RtPayloadArray)instance.facility).Set_PROPERTY_PTR(prop_ptr, this);
				}
				else if (instance.TypeKind == RtHeapTypeKind.CLOSURE)
				{
					((RtPayloadClosure)instance.facility).Set_PROPERTY_PTR(prop_ptr, this);
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
				RtPayloadDynamic prop = (RtPayloadDynamic)Context.GC.Heap[PROPERTY_PTR].facility;

				int index = prop.Slots.Count - 1;

				RtPayloadShape shape = null;

				int p = prop.SHAPE_PTR;

				while (p != Context.BlankShapePtr)
				{
					shape = (RtPayloadShape)Context.GC.Heap[p].facility;

					if (
						CompareShapePropertyName(shape.PTR_NAME, name) == 0
					//	string.Equals(
					//((RtPayloadString)Context.GC.Heap[propname_ptr].facility).Str,
					//((RtPayloadString)Context.GC.Heap[shape.PTR_NAME].facility).Str,
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
					if (!shape.Attribute.HasFlag(RtPayloadShape.PropertyAttribute.Writable))
					{
						RaiseError(ref error,$"not writeable property {name}" );
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

					int p_child = ((RtPayloadShape)Context.GC.Heap[prop.SHAPE_PTR].facility).PTR_CHILD;

					while (p_child != 0)
					{
						shape = (RtPayloadShape)Context.GC.Heap[p_child].facility;

						if (
							//string.Equals(
							//((RtPayloadString)Context.GC.Heap[propname_ptr].facility).Str,
							//((RtPayloadString)Context.GC.Heap[shape.PTR_NAME].facility).Str,
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
						var new_shape = (RtPayloadShape)Context.GC.Heap[nshape_ptr].facility;

						new_shape.Attribute = attribute;


						new_shape.PTR_NAME = propname;
						
						//string pname = ((RtPayloadString)Context.GC.Heap[propname_ptr].facility).Str;

						var current_shape = (RtPayloadShape)Context.GC.Heap[prop.SHAPE_PTR].facility;

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

		internal int GetPropertyPtr(RtHeapInstance instance)
		{
			int PROPERTY_PTR;
			if (instance.TypeKind == RtHeapTypeKind.INSTANCE)
			{
				if (((ASInstance)instance.Type).Flags.HasFlag(ClassFlags.Struct))
				{
					PROPERTY_PTR = 0;
				}
				else
				{
					PROPERTY_PTR = ((RtPayloadInstance)instance.facility).PROPERTY_PTR(this, (ASInstance)instance.Type);
				}
			}
			else if (instance.TypeKind == RtHeapTypeKind.CLASS || instance.TypeKind == RtHeapTypeKind.GLOBAL)
			{
				PROPERTY_PTR = ((RtPayloadScriptClass)instance.facility).PROPERTY_PTR;
			}
			else if (instance.TypeKind == RtHeapTypeKind.ARRAY)
			{
				PROPERTY_PTR = ((RtPayloadArray)instance.facility).PROPERTY_PTR(this);
			}
			else if (instance.TypeKind == RtHeapTypeKind.CLOSURE)
			{
				PROPERTY_PTR = ((RtPayloadClosure)instance.facility).PROPERTY_PTR(this);
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

		internal bool FindDynamicValue(RtHeapInstance instance, ReadOnlySpan<char> searchName, out NaNBoxing value, out int matchShapePtr, out int slotindex, out RtPayloadDynamic prop)
		{
			int PROPERTY_PTR = GetPropertyPtr(instance);
			if (PROPERTY_PTR != 0)
			{
				prop = (RtPayloadDynamic)Context.GC.Heap[PROPERTY_PTR].facility;

				int p = prop.SHAPE_PTR;
				int index = prop.Slots.Count - 1;

				while (p != Context.BlankShapePtr)
				{
					var shape = (RtPayloadShape)Context.GC.Heap[p].facility;

					//if (string.Equals(
					//	//((RtPayloadString)Context.GC.Heap[propname_ptr].facility).Str,
					//	searchName,
					//	((RtPayloadString)Context.GC.Heap[shape.PTR_NAME].facility).Str,
					//	StringComparison.Ordinal
					//	))
					if(CompareShapePropertyName(shape.PTR_NAME, searchName) == 0)
					{
						matchShapePtr = p;
						value = prop.Slots[index];
						slotindex = index;
						return true;
					}
					--index;
					p = shape.PTR_PARENT;
				}
			}
			else
			{
				prop = null;
			}

			value = default; matchShapePtr = 0; slotindex = -1; return false;
		}

		private void VisitDynamicValue(RtHeapInstance instance,Action<NaNBoxing,NaNBoxing> OnVisitProp)
		{
			int PROPERTY_PTR = GetPropertyPtr(instance);
			if (PROPERTY_PTR != 0)
			{
				var prop = (RtPayloadDynamic)Context.GC.Heap[PROPERTY_PTR].facility;

				int p = prop.SHAPE_PTR;
				int index = prop.Slots.Count - 1;

				while (p != Context.BlankShapePtr)
				{
					var shape = (RtPayloadShape)Context.GC.Heap[p].facility;

					if (shape.Attribute.HasFlag(RtPayloadShape.PropertyAttribute.Enumerable))
					{
						
						OnVisitProp(shape.PTR_NAME, prop.Slots[index]);
					}
					--index;
					p = shape.PTR_PARENT;
				}
			}
			
		}

		private void ChangeTranslation(RtPayloadDynamic dynamic, int shape_ptr, ref ReceiveError error)
		{
			if (dynamic.SHAPE_PTR == shape_ptr) //正好删除最后一个,使SHAPE指向上一个SHAPE即可
			{
				dynamic.SHAPE_PTR = ((RtPayloadShape)Context.GC.Heap[shape_ptr].facility).PTR_PARENT;
			}
			else
			{
				//先反向search到，要移除的shape的下一个shape。
				int p = dynamic.SHAPE_PTR;
				var parent_p = ((RtPayloadShape)Context.GC.Heap[dynamic.SHAPE_PTR].facility).PTR_PARENT;

				List<int> path = new List<int>();
				path.Add(p);

				while (parent_p != shape_ptr)
				{
					p = parent_p;
					parent_p = ((RtPayloadShape)Context.GC.Heap[p].facility).PTR_PARENT;

					path.Add(p);
				}

				//从shape_ptr的父节点开始找
				var chain_node = (RtPayloadShape)Context.GC.Heap[((RtPayloadShape)Context.GC.Heap[shape_ptr].facility).PTR_PARENT].facility;

				int found_ptr = 0;
				for (int i = path.Count - 1; i >= 0; i--)
				{
					RtPayloadShape tomatch = (RtPayloadShape)Context.GC.Heap[path[i]].facility;
					//string tomatch_name = GetShapePropertyNameAsString(tomatch.PTR_NAME);
					var tomatch_name = tomatch.PTR_NAME;

					bool found = false;

					var search_p = chain_node.PTR_CHILD;

					while (search_p != 0)
					{
						var shape = (RtPayloadShape)Context.GC.Heap[search_p].facility;

						if (search_p != shape_ptr)
						{
							//string s_name = GetShapePropertyNameAsString(shape.PTR_NAME);
							var s_name = shape.PTR_NAME;

							if (
								tomatch.Attribute == shape.Attribute
								&&
								//string.Equals(s_name, tomatch_name, StringComparison.Ordinal))
								CompareShapePropertyName(s_name,tomatch_name) == 0)

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
					//chain_node = (RtPayloadShape)Context.GC.Heap[((RtPayloadShape)Context.GC.Heap[shape_ptr].facility).PTR_PARENT].facility;
					int chain_ptr = ((RtPayloadShape)Context.GC.Heap[shape_ptr].facility).PTR_PARENT;


					for (int i = path.Count - 1; i >= 0; i--)
					{
						var nshape_ptr = Context.GC.AllocShape();
						if (nshape_ptr == 0)
						{
							RaiseOutOfMemory(ref error);
							return;
						}

						var new_shape = (RtPayloadShape)Context.GC.Heap[nshape_ptr].facility;
						var old_shape = (RtPayloadShape)Context.GC.Heap[path[i]].facility;

						new_shape.Attribute = old_shape.Attribute;
						new_shape.PTR_NAME = old_shape.PTR_NAME;
						new_shape.PTR_PARENT = chain_ptr;

						chain_node = (RtPayloadShape)Context.GC.Heap[chain_ptr].facility;
						new_shape.PTR_BROTHER = chain_node.PTR_CHILD;
						chain_node.PTR_CHILD = nshape_ptr;

						chain_ptr = nshape_ptr;

					}

					//chain_node = (RtPayloadShape)Context.GC.Heap[chain_ptr].facility;
					//string foundname = ((RtPayloadString)Context.GC.Heap[chain_node.PTR_NAME].facility).Str;
					//string path_s = "";
					//do
					//{
					//    path_s = path_s + ((RtPayloadString)Context.GC.Heap[chain_node.PTR_NAME].facility).Str + ",";

					//    chain_node = (RtPayloadShape)Context.GC.Heap[chain_node.PTR_PARENT].facility;

					//} while (chain_node.PTR_PARENT != 0);

					dynamic.SHAPE_PTR = chain_ptr;

				}
				else
				{
					//string foundname = ((RtPayloadString)Context.GC.Heap[chain_node.PTR_NAME].facility).Str;
					//string path_s ="";
					//do
					//{
					//    path_s = path_s + ((RtPayloadString)Context.GC.Heap[chain_node.PTR_NAME].facility).Str + ",";

					//    chain_node = (RtPayloadShape)Context.GC.Heap[chain_node.PTR_PARENT].facility;

					//} while (chain_node.PTR_PARENT != 0);


					dynamic.SHAPE_PTR = found_ptr;
				}


			}

		}


		public int InitCacheInstance(ASClass @class, int slotindex,bool initmember)
		{

			int cache_ptr = Context.CacheInstancePtr + slotindex;

			var cache = Context.GC.Heap[cache_ptr];
			cache.Type = @class.Instance;

			((RtPayloadInstance)cache.facility).HEAPINSTANCE_PTR = 0;
			((RtPayloadInstance)cache.facility).Set_PROPERTY_PTR(0, Context.player,@class.Instance);
			((RtPayloadInstance)cache.facility).Set_PROTOTYPE(((RtPayloadScriptClass)Context.GC.Heap[@class.__instance_index__].facility).PROTO__PTR, this);
			((RtPayloadInstance)cache.facility).methodscopeslot_ref_state = 0;

			CodeScope cscope = @class.Instance._link_codescope;
			if (cscope.TypeLayout.Size > 0)
			{
				((RtPayloadInstance)cache.facility).Init(cscope, Context.player,initmember);
			}

			Context.StackSlots[slotindex].SetHeapPtr(cache_ptr);
			return cache_ptr;
		}



		


		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private unsafe int Ld_function_and_store_member(ScopeHeapLocater heapLocater, RtHeapInstance mscope, int scope_ptr, uint fbox, ref ReceiveError error,
			int stackStPos, StackLocater target, Span<NaNBoxing> stackslots, int* method_scopes, out RtHeapInstance closure_instance)
		{
			if (!(heapLocater.MemberIndex == ushort.MaxValue && heapLocater.ScopeIndex == ushort.MaxValue))
			{
				var s = mscope; //Context.GC.Heap[scope_ptr];
				if (s.Type._link_codescope.index != heapLocater.ScopeIndex)
				{
					do
					{
						s = Context.GC.Heap[((RtPayloadMethodScope)s.facility).ParentPtr];
					}
					while (
						s.TypeKind == RtHeapTypeKind.MethodScope && 
						s.Type._link_codescope.index != heapLocater.ScopeIndex
					);

					NaNBoxing c = default;
					if (s.TypeKind == RtHeapTypeKind.GLOBAL)
					{
#if DEBUG
						if (((RtPayloadScriptClass)s.facility).Meta._link_codescope.index != heapLocater.ScopeIndex)
						{
							throw new InvalidOperationException();
						}
#endif

						c = ((RtPayloadScriptClass)s.facility).ReadSlot(heapLocater.MemberIndex);
					}
					else if (s.TypeKind == RtHeapTypeKind.MethodScope)
					{
						c = ((RtPayloadMethodScope)s.facility).ReadSlot(heapLocater.MemberIndex, this);
					}
					else if (s.TypeKind == RtHeapTypeKind.INSTANCE)
					{
#if DEBUG
						if (s.Type._link_codescope.Parent.index != heapLocater.ScopeIndex)
						{
							throw new InvalidOperationException();
						}
#endif
						s = Context.GC.Heap[((ASScript)s.Type._link_codescope.Parent.Container).__global_index__];
						c = ((RtPayloadScriptClass)s.facility).ReadSlot(heapLocater.MemberIndex);
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
						ASMethod function = Context.link_const_methods[(int)fbox];  //((ASMethodBody)obj.Type).Method;

						int ptrIndex = stackStPos + target.index;
						int closurePtr = Context.M_ClosurePtr + ptrIndex;

						var closure = Context.GC.Heap[closurePtr];
						closure.Type = function.Body;
						((RtPayloadClosure)closure.facility).ScopePtr = scope_ptr;
						((RtPayloadClosure)closure.facility).ScopeType = null; ((RtPayloadClosure)closure.facility)._ref_as_type = null;
						((RtPayloadClosure)closure.facility).This.SetNull(); ((RtPayloadClosure)closure.facility).methodscopeslot_ref_state = 0;
						((RtPayloadClosure)closure.facility).HEAPINSTANCE_PTR = 0;

						//stackslots[target.index].SetHeapPtr(closurePtr);

						NaNBoxing v = new NaNBoxing();
						v.SetHeapPtr(closurePtr);
						v = GetSaveValue(v, ref error);
						if (error.raised)
						{
							closure_instance = null;
							return 0;
						}


						if (s.TypeKind == RtHeapTypeKind.GLOBAL)
						{
							((RtPayloadScriptClass)s.facility).SetSlot(v, heapLocater.MemberIndex);
							stackslots[target.index] = v;
						}
						else if (s.TypeKind == RtHeapTypeKind.MethodScope)
						{
							((RtPayloadMethodScope)s.facility).SetSlot(v, heapLocater.MemberIndex);
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
						if (Context.GC.Heap[c.HeapPtr].TypeKind != RtHeapTypeKind.CLOSURE)
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
					var c = ((RtPayloadMethodScope)s.facility).ReadSlot(heapLocater.MemberIndex, this);
					if (c.ValueType == BoxType.Undefined)
					{
						ASMethod function = Context.link_const_methods[(int)fbox];  //((ASMethodBody)obj.Type).Method;

						int ptrIndex = stackStPos + target.index;
						int closurePtr = Context.M_ClosurePtr + ptrIndex;

						var closure = Context.GC.Heap[closurePtr];
						closure.Type = function.Body;
						((RtPayloadClosure)closure.facility).ScopePtr = scope_ptr;
						((RtPayloadClosure)closure.facility).ScopeType = null; ((RtPayloadClosure)closure.facility)._ref_as_type = null;
						((RtPayloadClosure)closure.facility).This.SetNull(); ((RtPayloadClosure)closure.facility).methodscopeslot_ref_state = 0;
						((RtPayloadClosure)closure.facility).HEAPINSTANCE_PTR = 0;

						//保存到method的成员中，可以考虑到缓存
						ASTrait t = s.Type._link_codescope.Members[heapLocater.MemberIndex].trait;

						NaNBoxing v = new NaNBoxing();
						v.SetHeapPtr(closurePtr);

						PrepareSaveMethodScope((RtPayloadMethodScope)s.facility, ref heapLocater, ref v, m_scope, method_scopes, ref error);
						if (error.raised)
						{
							closure_instance = null;
							return 0;
						}


						((RtPayloadMethodScope)s.facility).SetSlot(v, heapLocater.MemberIndex);
						stackslots[target.index] = v;
						closure_instance = Context.GC.Heap[v.HeapPtr]; ;
						return v.HeapPtr;
					}
					else
					{
#if DEBUG
						if (c.ValueType != BoxType.HeapPtr)
							throw new InvalidOperationException();
						if (Context.GC.Heap[c.HeapPtr].TypeKind != RtHeapTypeKind.CLOSURE)
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
				ASMethod function = Context.link_const_methods[(int)fbox];  //((ASMethodBody)obj.Type).Method;

				int ptrIndex = stackStPos + target.index;
				int closurePtr = Context.M_ClosurePtr + ptrIndex;

				var closure = Context.GC.Heap[closurePtr];
				closure.Type = function.Body;
				((RtPayloadClosure)closure.facility).ScopePtr = scope_ptr;
				((RtPayloadClosure)closure.facility).ScopeType = null; ((RtPayloadClosure)closure.facility)._ref_as_type = null;
				((RtPayloadClosure)closure.facility).This.SetNull(); ((RtPayloadClosure)closure.facility).methodscopeslot_ref_state = 0;
				((RtPayloadClosure)closure.facility).HEAPINSTANCE_PTR = 0;

				stackslots[target.index].SetHeapPtr(closurePtr);
				closure_instance = closure;
				return closurePtr;
			}
		}


		[MethodImpl(MethodImplOptions.AggressiveOptimization )]
		private unsafe NaNBoxing Ld_ScopeH(RtHeapInstance scope,ScopeHeapLocater heapLocater,  ASContainer scopeType , int returnSlotIndex)
		{

			var s = scope;int _parent_ptr = 0;
		label_method_parent:

			switch (s.TypeKind)
			{
				case RtHeapTypeKind.CLASS:
					{
						var codeScope = ((RtPayloadScriptClass)s.facility).Meta._link_codescope;
						if (codeScope.index != heapLocater.ScopeIndex)
						{
							codeScope = codeScope.Parent;
#if DEBUG
							if (codeScope.Kind != CodeScopeKind.Script)
								throw new InvalidOperationException();
							if (codeScope.index != heapLocater.ScopeIndex)
								throw new InvalidOperationException();
#endif

							RtHeapInstance sInstance = Context.GC.Heap[
							((ASScript)((RtPayloadScriptClass)s.facility).Meta._link_codescope.Parent.Container).__global_index__];

							RtPayloadScriptClass heap = (RtPayloadScriptClass)sInstance.facility;
							NaNBoxing value = heap.ReadSlot(heapLocater.MemberIndex);

							//stackslots[stackLocater.index] = value;
							return value;
						}
						else
						{
							RtPayloadScriptClass heap = (RtPayloadScriptClass)s.facility;
							NaNBoxing value = heap.ReadSlot(heapLocater.MemberIndex);

							//stackslots[stackLocater.index] = value;
							return value;
						}

					}
					break;
				case RtHeapTypeKind.GLOBAL:
					{
#if DEBUG
						var codeScope = ((RtPayloadScriptClass)s.facility).Meta._link_codescope;
						if (codeScope.index != heapLocater.ScopeIndex)
							throw new InvalidOperationException();
#endif

						RtPayloadScriptClass heap = (RtPayloadScriptClass)s.facility;
						NaNBoxing value = heap.ReadSlot(heapLocater.MemberIndex);

						//stackslots[stackLocater.index] = value;
						return value;
					}
					break;
				case RtHeapTypeKind.INSTANCE:
					{
						//考虑可能继承的情况，scopeType保存上下文堆内存用的布局类型
						if (
							scopeType._link_codescope.index != heapLocater.ScopeIndex
							&&
							s.Type._link_codescope.index != heapLocater.ScopeIndex
							)
						{
							var sType = scopeType._link_codescope.Parent; //这里还是必须用scopeType来查找global.
							while (sType.Kind != CodeScopeKind.Script)
							{
								sType = sType.Parent;
							}

							//const KKK = 7; 在这种情况下发生。
							//class C extends Main
							//{
							//	/* INTERFACE II */
							//	public function B()
							//	{
							//		return function iii()
							//		{
							//				o = KKK;			
							//		}
							//	}
							//} 

							RtHeapInstance sInstance = Context.GC.Heap[
									((ASScript)(sType.Container)).__global_index__];

							RtPayloadScriptClass heap = (RtPayloadScriptClass)sInstance.facility;
							NaNBoxing value = heap.ReadSlot(heapLocater.MemberIndex);

							//stackslots[stackLocater.index] = value;

							return value;
						}
						else
						{
							NaNBoxing value = ((RtPayloadInstance)s.facility).ReadSlot(heapLocater.MemberIndex, s.Type._link_codescope, this, returnSlotIndex ,_parent_ptr);
							//stackslots[stackLocater.index] = value;

							return value;
						}

					}
					break;
				case RtHeapTypeKind.MethodScope:
					{
						if (s.Type._link_codescope.index != heapLocater.ScopeIndex)
						{
							_parent_ptr = ((RtPayloadMethodScope)s.facility).ParentPtr;
							s = Context.GC.Heap[_parent_ptr];
							goto label_method_parent;
						}
						else
						{
							RtPayloadMethodScope heap = (RtPayloadMethodScope)s.facility;
							NaNBoxing value = heap.ReadSlot(heapLocater.MemberIndex
								//#if FORCOMPILER
								, this
								//#endif
								);

							//stackslots[stackLocater.index] = value;
							return value;
						}
					}
					break;
				case RtHeapTypeKind.STRING:
				//case RtHeapTypeKind.CACHE_LD_CLASS:
				case RtHeapTypeKind.STACK_CACHE_OBJ:
				default:
#if DEBUG
					throw new InvalidOperationException();
#else
					Environment.FailFast("出错了，这里跑不到");  return default;
#endif
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private unsafe void Exec_Add(ref ReceiveError error, NaNBoxing n1, NaNBoxing n2, StackLocater dst, int scope_ptr, StackLocater tmp, Span<NaNBoxing> stackslots, int stackStPos, NaNBoxing thisPtr)
		{
			NaNBoxing sum;
			if (NaNBoxing.FastAdd(n1, n2, out sum))
			{
				stackslots[dst.index] = sum;
				return;
			}

			ASClass t1;ASClass t2;
			//操作符重载
			int op_override_id1 = GetOpOverrideTypeId(n1,out t1);
			int op_override_id2 = GetOpOverrideTypeId(n2,out t2);
			if (op_override_id1 != -1 && op_override_id2 != -1)
			{
				var method = overrideOperatorMethods[(int)OverrideOperator.add][op_override_id1][op_override_id2];
				if (method != null)
				{
#if FORCOMPILER
					if (IsComputeConstExpr)
					{
						throw new EvalConstException();
					}
#endif

					if (t1 != null)
					{
						InitScript((ASScript)t1._link_codescope.Parent.Container, ref error);
						if (error.raised)
						{
							return;
						}
					}
					if (t2 != null)
					{
						InitScript((ASScript)t2._link_codescope.Parent.Container, ref error);
						if (error.raised)
						{
							return;
						}
					}


					var @class = (ASClass)method.Container;
					
					if (Context.StackPosition + 2 >= Context.STACK_LENGTH)
					{
						RaiseStackOverflow(ref error);
						return;
					}

					Span<NaNBoxing> slots = Context.StackSlots.AsSpan(Context.StackPosition, 2);
					slots[0] = n1;
					slots[1] = n2;

					Context.StackPosition += 2;

					NaNBoxing cls = default; cls.SetHeapPtr(@class.__instance_index__);
					unsafe
					{
						StackLocater* args = stackalloc StackLocater[2];
						args->index = 0;
						(args + 1)->index = 1;
						RunMethod(method, cls, scope_ptr, @class, 2, (byte*)args, slots, ref error, stackStPos + dst.index);
					}
					Context.StackPosition -= 2;

					return;
				}
			}

			HINT hint; //这里还是按AIR的实现来，如果有字符串则用 string
			if ((n1.ValueType == BoxType.HeapPtr && Context.GC.Heap[n1.HeapPtr].TypeKind == RtHeapTypeKind.STRING)
				||
				(n2.ValueType == BoxType.HeapPtr && Context.GC.Heap[n2.HeapPtr].TypeKind == RtHeapTypeKind.STRING)
				||
				n1.ValueType == BoxType.LocalString
				||
				n2.ValueType == BoxType.LocalString
				)
			{
				hint = HINT.h_string;
			}
			else
			{
				hint = HINT.h_number;
			}

			n1 = ToPrimitive(ref error, n1, hint, scope_ptr, tmp, dst, stackslots, stackStPos, thisPtr);
			if (error.raised)
			{
				return;
			}

			n2 = ToPrimitive(ref error, n2, hint, scope_ptr, tmp, dst, stackslots, stackStPos, thisPtr);
			if (error.raised)
			{
				return;
			}

			switch (n1.ValueType)
			{
				case NaNBoxing.BoxType.Number:
					{
						switch (n2.ValueType)
						{
							case BoxType.Number:
							case BoxType.Undefined:
							case BoxType.Null:
							case BoxType.Boolean:
							case BoxType.Int:
							case BoxType.Uint:
							case BoxType.Sbyte:
							case BoxType.Byte:
							case BoxType.Short:
							case BoxType.UShort:
							case BoxType.Float:
								stackslots[dst.index].SetNumber(n1.Number + Extensions.GetDoubleValue(n2));
								break;
							case BoxType.HeapPtr:
								goto lbL_primtive_add_heap;
							case BoxType.LocalString:
								{
									// Use efficient char-based concatenation to avoid string allocation
									Span<char> chars2 = stackalloc char[16];
									int charCount2 = n2.GetLocalStringChars(chars2);
									if (charCount2 > 0)
									{
										string str2 = new string(chars2.Slice(0, charCount2));
										string concatenated = Extensions.GetPrimitiveValueToString(this, n1) + str2;
										
										// 使用安全的字符串创建方法
										if (!TryCreateStringValue(concatenated, out stackslots[dst.index], ref error))
										{
											return; // 错误已经在TryCreateStringValue中处理
										}
									}
									else
									{
										// Empty LocalString, just convert n1 to string
										string concatenated = Extensions.GetPrimitiveValueToString(this, n1);
										if (!TryCreateStringValue(concatenated, out stackslots[dst.index], ref error))
										{
											return;
										}
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
					break;
				case NaNBoxing.BoxType.Undefined:
					{
						switch (n2.ValueType)
						{
							case BoxType.Number:
							case BoxType.Undefined:
							case BoxType.Null:
							case BoxType.Boolean:
							case BoxType.Int:
							case BoxType.Uint:
							case BoxType.Sbyte:
							case BoxType.Byte:
							case BoxType.Short:
							case BoxType.UShort:
							case BoxType.Float:
								stackslots[dst.index].SetNumber(double.NaN + Extensions.GetDoubleValue(n2));
								break;
							case BoxType.HeapPtr:
								goto lbL_primtive_add_heap;
							case BoxType.LocalString:
								{
									// Use efficient char-based concatenation to avoid string allocation
									Span<char> chars2 = stackalloc char[16];
									int charCount2 = n2.GetLocalStringChars(chars2);
									if (charCount2 > 0)
									{
										string str2 = new string(chars2.Slice(0, charCount2));
										string concatenated = Extensions.GetPrimitiveValueToString(this, n1) + str2;
										
										// 使用安全的字符串创建方法
										if (!TryCreateStringValue(concatenated, out stackslots[dst.index], ref error))
										{
											return; // 错误已经在TryCreateStringValue中处理
										}
									}
									else
									{
										// Empty LocalString, just convert n1 to string
										string concatenated = Extensions.GetPrimitiveValueToString(this, n1);
										if (!TryCreateStringValue(concatenated, out stackslots[dst.index], ref error))
										{
											return;
										}
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
					break;
				case NaNBoxing.BoxType.Null:
					{
						switch (n2.ValueType)
						{
							case BoxType.Number:
							case BoxType.Undefined:
							case BoxType.Null:
							case BoxType.Boolean:
							case BoxType.Int:
							case BoxType.Uint:
							case BoxType.Sbyte:
							case BoxType.Byte:
							case BoxType.Short:
							case BoxType.UShort:
							case BoxType.Float:
								stackslots[dst.index].SetNumber(0.0 + Extensions.GetDoubleValue(n2));
								break;
							case BoxType.HeapPtr:
								goto lbL_primtive_add_heap;
#if DEBUG
							case BoxType.Fault:
							default:
								throw new InvalidOperationException();
#endif
						}
					}
					break;
				case NaNBoxing.BoxType.Boolean:
					{
						switch (n2.ValueType)
						{
							case BoxType.Number:
								stackslots[dst.index].SetNumber((n1.Boolean ? 1 : 0) + Extensions.GetDoubleValue(n2));
								break;
							case BoxType.Undefined:
								stackslots[dst.index].SetNumber(double.NaN);
								break;
							case BoxType.Null:
								stackslots[dst.index].SetNumber((n1.Boolean ? 1 : 0) + 0.0);
								break;
							case BoxType.Boolean:
								stackslots[dst.index].SetInt((n1.Boolean ? 1 : 0) + (n2.Boolean ? 1 : 0));
								break;
							case BoxType.Int:
								stackslots[dst.index].SetInt((n1.Boolean ? 1 : 0) + n2.IntValue);
								break;
							case BoxType.Uint:
								stackslots[dst.index].SetNumber((n1.Boolean ? 1U : 0U) + n2.UIntValue);
								break;
							case BoxType.Sbyte:
							case BoxType.Byte:
							case BoxType.Short:
							case BoxType.UShort:
								stackslots[dst.index].SetInt((n1.Boolean ? 1 : 0) + Extensions.GetIntValue(n2));
								break;
							case BoxType.Float:
								stackslots[dst.index].SetFloat((n1.Boolean ? 1 : 0) + n2.FloatValue);
								break;
							case BoxType.HeapPtr:
								goto lbL_primtive_add_heap;
#if DEBUG
							case BoxType.Fault:
							default:
								throw new InvalidOperationException();
#endif
						}
					}
					break;
				case NaNBoxing.BoxType.Int:
					{
						switch (n2.ValueType)
						{
							case BoxType.Undefined:
								stackslots[dst.index].SetNumber(double.NaN);
								break;
							case BoxType.Number:
							case BoxType.Null:
								stackslots[dst.index].SetNumber(n1.IntValue + Extensions.GetDoubleValue(n2));
								break;
							case BoxType.Boolean:
								stackslots[dst.index].SetInt(n1.IntValue + (n2.Boolean ? 1 : 0));
								break;
							case BoxType.Uint:
								stackslots[dst.index].SetNumber((double)n1.IntValue + n2.UIntValue);
								break;
							case BoxType.Float:
								stackslots[dst.index].SetFloat(n1.IntValue + n2.FloatValue);
								break;
							case BoxType.Int:
							case BoxType.Sbyte:
							case BoxType.Byte:
							case BoxType.Short:
							case BoxType.UShort:
								stackslots[dst.index].SetInt(n1.IntValue + Extensions.GetIntValue(n2));
								break;
							case BoxType.HeapPtr:
								goto lbL_primtive_add_heap;
#if DEBUG
							case BoxType.Fault:
							default:
								throw new InvalidOperationException();
#endif
						}
					}
					break;
				case NaNBoxing.BoxType.Uint:
					{
						switch (n2.ValueType)
						{
							case BoxType.Undefined:
								stackslots[dst.index].SetNumber(double.NaN);
								break;
							case BoxType.Number:
							case BoxType.Null:
								stackslots[dst.index].SetNumber(n1.UIntValue + Extensions.GetDoubleValue(n2));
								break;
							case BoxType.Boolean:
								stackslots[dst.index].SetNumber(n1.UIntValue + (n2.Boolean ? 1U : 0U));
								break;
							case BoxType.Int:
								stackslots[dst.index].SetNumber((double)n1.UIntValue + n2.IntValue);
								break;
							case BoxType.Uint:
								stackslots[dst.index].SetUInt(n1.UIntValue + n2.UIntValue);
								break;
							case BoxType.Sbyte:
								stackslots[dst.index].SetNumber((double)n1.UIntValue + n2.SByteValue);
								break;
							case BoxType.Byte:
								stackslots[dst.index].SetUInt(n1.UIntValue + n2.ByteValue);
								break;
							case BoxType.Short:
								stackslots[dst.index].SetNumber((double)n1.UIntValue + n2.ShortValue);
								break;
							case BoxType.UShort:
								stackslots[dst.index].SetUInt(n1.UIntValue + n2.UShortValue);
								break;
							case BoxType.Float:
								stackslots[dst.index].SetFloat((float)n1.UIntValue + n2.FloatValue);
								break;
							case BoxType.HeapPtr:
								goto lbL_primtive_add_heap;
#if DEBUG
							case BoxType.Fault:
							default:
								throw new InvalidOperationException();
#endif
						}
					}
					break;
				case NaNBoxing.BoxType.Sbyte:
				case NaNBoxing.BoxType.Byte:
				case NaNBoxing.BoxType.Short:
				case NaNBoxing.BoxType.UShort:
					switch (n2.ValueType)
					{
						case BoxType.Undefined:
							stackslots[dst.index].SetNumber(double.NaN);
							break;
						case BoxType.Number:
						case BoxType.Null:
							stackslots[dst.index].SetNumber(Extensions.GetDoubleValue(n1) + Extensions.GetDoubleValue(n2));
							break;
						case BoxType.Uint:
							stackslots[dst.index].SetNumber(Extensions.GetDoubleValue(n1) + n2.UIntValue);
							break;
						case BoxType.Sbyte:
						case BoxType.Byte:
						case BoxType.Short:
						case BoxType.UShort:
						case BoxType.Boolean:
						case BoxType.Int:
							stackslots[dst.index].SetInt(Extensions.GetIntValue(n1) + Extensions.GetIntValue(n2));
							break;
						case BoxType.Float:
							stackslots[dst.index].SetFloat(Extensions.GetFloatValue(n1) + n2.FloatValue);
							break;
						case BoxType.HeapPtr:
							goto lbL_primtive_add_heap;
#if DEBUG
						case BoxType.Fault:
						default:
							throw new InvalidOperationException();
#endif
					}

					break;
				case NaNBoxing.BoxType.Float:
					{
						switch (n2.ValueType)
						{
							case BoxType.Number:
							case BoxType.Undefined:
							case BoxType.Null:

								stackslots[dst.index].SetNumber(Extensions.GetDoubleValue(n1) + Extensions.GetDoubleValue(n2));
								break;
							case BoxType.Boolean:
							case BoxType.Int:
							case BoxType.Sbyte:
							case BoxType.Byte:
							case BoxType.Short:
							case BoxType.UShort:
							case BoxType.Float:
							case BoxType.Uint:
								stackslots[dst.index].SetFloat(Extensions.GetFloatValue(n1) + Extensions.GetFloatValue(n2));
								break;
							case BoxType.HeapPtr:
								goto lbL_primtive_add_heap;
#if DEBUG
							case BoxType.Fault:
							default:
								throw new InvalidOperationException();
#endif
						}
					}
					break;
				case NaNBoxing.BoxType.LocalString:
					{
						// Use efficient char-based operations to avoid string allocation
						Span<char> chars1 = stackalloc char[16];
						int charCount1 = n1.GetLocalStringChars(chars1);
						string str1 = charCount1 > 0 ? new string(chars1.Slice(0, charCount1)) : string.Empty;
						
						switch (n2.ValueType)
						{
							case BoxType.Number:
							case BoxType.Undefined:
							case BoxType.Null:
							case BoxType.Boolean:
							case BoxType.Int:
							case BoxType.Uint:
							case BoxType.Sbyte:
							case BoxType.Byte:
							case BoxType.Short:
							case BoxType.UShort:
							case BoxType.Float:
								{
									var str2 = Extensions.GetPrimitiveValueToString(this, n2);
									string concatenated = str1 + str2;
									
									// 使用安全的字符串创建方法
									if (!TryCreateStringValue(concatenated, out stackslots[dst.index], ref error))
									{
										return; // 错误已经在TryCreateStringValue中处理
									}
								}
								break;
							case BoxType.HeapPtr:
								{
									var instance2 = Context.GC.Heap[n2.HeapPtr];
									if (instance2.TypeKind == RtHeapTypeKind.STRING)
									{
										var str2 = ((RtPayloadString)instance2.facility).Str;
										string concatenated = str1 + str2;
										
										// 使用安全的字符串创建方法
										if (!TryCreateStringValue(concatenated, out stackslots[dst.index], ref error))
										{
											return; // 错误已经在TryCreateStringValue中处理
										}
									}
#if DEBUG
									else
									{
										throw new InvalidOperationException();
									}
#endif
								}
								break;
							case BoxType.LocalString:
								{
									// Use efficient char-based concatenation for LocalString + LocalString
									Span<char> chars2 = stackalloc char[16];
									int charCount2 = n2.GetLocalStringChars(chars2);
									if (charCount2 > 0)
									{
										string str2 = new string(chars2.Slice(0, charCount2));
										string concatenated = str1 + str2;
										
										// 使用安全的字符串创建方法
										if (!TryCreateStringValue(concatenated, out stackslots[dst.index], ref error))
										{
											return; // 错误已经在TryCreateStringValue中处理
										}
									}
									else
									{
										// n2 is empty LocalString, result is just str1
										if (!TryCreateStringValue(str1, out stackslots[dst.index], ref error))
										{
											return;
										}
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
					break;
				case NaNBoxing.BoxType.HeapPtr:
					{
						var instance1 = Context.GC.Heap[n1.HeapPtr];
						if (instance1.TypeKind == RtHeapTypeKind.STRING)
						{
							var str1 = ((RtPayloadString)instance1.facility).Str;

							switch (n2.ValueType)
							{
								case BoxType.Number:
								case BoxType.Undefined:
								case BoxType.Null:
								case BoxType.Boolean:
								case BoxType.Int:
								case BoxType.Uint:
								case BoxType.Sbyte:
								case BoxType.Byte:
								case BoxType.Short:
								case BoxType.UShort:
								case BoxType.Float:
									{
										var str2 = Extensions.GetPrimitiveValueToString(this,n2);
										string concatenated = str1 + str2;
										
										// 使用安全的字符串创建方法
										if (!TryCreateStringValue(concatenated, out stackslots[dst.index], ref error))
										{
											return; // 错误已经在TryCreateStringValue中处理
										}
									}
									break;
								case BoxType.LocalString:
									{
										// Use efficient char-based concatenation to avoid string allocation
										Span<char> chars2 = stackalloc char[16];
										int charCount2 = n2.GetLocalStringChars(chars2);
										if (charCount2 > 0)
										{
											string str2 = new string(chars2.Slice(0, charCount2));
											string concatenated = str1 + str2;
											
											// 使用安全的字符串创建方法
											if (!TryCreateStringValue(concatenated, out stackslots[dst.index], ref error))
											{
												return; // 错误已经在TryCreateStringValue中处理
											}
										}
										else
										{
											// n2 is empty LocalString, result is just str1
											if (!TryCreateStringValue(str1, out stackslots[dst.index], ref error))
											{
												return;
											}
										}
									}
									break;
								case BoxType.HeapPtr:
									{
										var instance2 = Context.GC.Heap[n2.HeapPtr];
										if (instance2.TypeKind == RtHeapTypeKind.STRING)
										{
											var str2 = ((RtPayloadString)instance2.facility).Str;
											string concatenated = str1 + str2;
											
											// 使用安全的字符串创建方法
											if (!TryCreateStringValue(concatenated, out stackslots[dst.index], ref error))
											{
												return; // 错误已经在TryCreateStringValue中处理
											}
										}
#if DEBUG
										else
										{
											throw new InvalidOperationException();
										}
#endif
									}
									break;
#if DEBUG
								case BoxType.Fault:
								default:
									throw new InvalidOperationException();
#endif
							}

						}
#if DEBUG
						else
						{
							throw new InvalidOperationException();
						}
#endif
					}
					break;
#if DEBUG
				case NaNBoxing.BoxType.Fault:
				default:
					throw new InvalidOperationException();
#endif
			}
			return;
		lbL_primtive_add_heap:;
			{
				var instance = Context.GC.Heap[n2.HeapPtr];
				if (instance.TypeKind == RtHeapTypeKind.STRING)
				{
					string str = Extensions.GetPrimitiveValueToString(this, n1);
					var str2 = ((RtPayloadString)instance.facility).Str;
					Context.GC.CheckGC(ref error);

					string concatenated = str + str2;
					
					// 使用安全的字符串创建方法
					if (!TryCreateStringValue(concatenated, out stackslots[dst.index], ref error))
					{
						return; // 错误已经在TryCreateStringValue中处理
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



		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private unsafe void Exec_Sub(ref ReceiveError error,NaNBoxing n1,NaNBoxing n2,StackLocater dst,int scope_ptr,StackLocater tmp , Span<NaNBoxing> stackslots, int stackStPos,NaNBoxing thisPtr )
		{
			NaNBoxing sub;
			if (NaNBoxing.FastMinus(n1, n2, out sub))
			{
				stackslots[dst.index] = sub;
				return;
			}

			//操作符重载
			ASClass t1;ASClass t2;
			int op_override_id1 = GetOpOverrideTypeId(n1,out t1);
			int op_override_id2 = GetOpOverrideTypeId(n2,out t2);
			if (op_override_id1 != -1 && op_override_id2 != -1)
			{
				var method = overrideOperatorMethods[(int)OverrideOperator.sub][op_override_id1][op_override_id2];
				if (method != null)
				{
#if FORCOMPILER
					if (IsComputeConstExpr)
					{
						throw new EvalConstException();
					}
#endif

					if (t1 != null)
					{
						InitScript((ASScript)t1._link_codescope.Parent.Container, ref error);
						if (error.raised)
						{
							return;
						}
					}
					if (t2 != null)
					{
						InitScript((ASScript)t2._link_codescope.Parent.Container, ref error);
						if (error.raised)
						{
							return;
						}
					}

					var @class = (ASClass)method.Container;
					
					if (Context.StackPosition + 2 >= Context.STACK_LENGTH)
					{
						RaiseStackOverflow(ref error);
						return;
					}

					Span<NaNBoxing> slots = Context.StackSlots.AsSpan(Context.StackPosition, 2);
					slots[0] = n1;
					slots[1] = n2;

					Context.StackPosition += 2;

					NaNBoxing cls = default; cls.SetHeapPtr(@class.__instance_index__);
					unsafe
					{
						StackLocater* args = stackalloc StackLocater[2];
						args->index = 0;
						(args + 1)->index = 1;
						RunMethod(method, cls, scope_ptr, @class, 2, (byte*)args, slots, ref error, stackStPos + dst.index);
					}
					Context.StackPosition -= 2;

					return;
				}
			}





			if (!IsPrimitive(n1))
			{
				n1 = ToPrimitive(ref error, n1, HINT.h_number, scope_ptr, tmp, dst, stackslots, stackStPos, thisPtr);
				if (error.raised)
				{
					return;
				}
			}

			if (n1.ValueType == BoxType.HeapPtr)
			{
				ConvertValueType(ref error, n1, TypeKind.Number, Context.NUMBER, ref n1); //这里不会出错 这里肯定是字符串
			}

			if (!IsPrimitive(n2))
			{
				n2 = ToPrimitive(ref error, n2, HINT.h_number, scope_ptr, tmp, dst, stackslots, stackStPos, thisPtr);
				if (error.raised)
				{
					return;
				}
			}

			if (n2.ValueType == BoxType.HeapPtr)
			{
				ConvertValueType(ref error, n2, TypeKind.Number, Context.NUMBER, ref n2); //这里不会出错 这里肯定是字符串
			}

			switch (n1.ValueType)
			{
				case BoxType.Number:
					stackslots[dst.index].SetNumber(n1.Number - Extensions.GetDoubleValue(n2));
					break;
				case BoxType.Undefined:
					stackslots[dst.index].SetNumber(double.NaN - Extensions.GetDoubleValue(n2));
					break;
				case BoxType.Null:
					stackslots[dst.index].SetNumber(0.0 - Extensions.GetDoubleValue(n2));
					break;
				case BoxType.Boolean:
					switch (n2.ValueType)
					{
						case BoxType.Number:
							stackslots[dst.index].SetNumber((n1.Boolean ? 1 : 0) - Extensions.GetDoubleValue(n2));
							break;
						case BoxType.Undefined:
							stackslots[dst.index].SetNumber(double.NaN);
							break;
						case BoxType.Null:
							stackslots[dst.index].SetNumber((n1.Boolean ? 1 : 0) - 0.0);
							break;
						case BoxType.Boolean:
							stackslots[dst.index].SetInt((n1.Boolean ? 1 : 0) - (n2.Boolean ? 1 : 0));
							break;
						case BoxType.Int:
							stackslots[dst.index].SetInt((n1.Boolean ? 1 : 0) - n2.IntValue);
							break;
						case BoxType.Uint:
							stackslots[dst.index].SetNumber((n1.Boolean ? 1U : 0U) - n2.UIntValue);
							break;
						case BoxType.Sbyte:
						case BoxType.Byte:
						case BoxType.Short:
						case BoxType.UShort:
							stackslots[dst.index].SetInt((n1.Boolean ? 1 : 0) - Extensions.GetIntValue(n2));
							break;
						case BoxType.Float:
							stackslots[dst.index].SetFloat((n1.Boolean ? 1 : 0) - n2.FloatValue);
							break;
#if DEBUG
						case BoxType.HeapPtr:
						case BoxType.Fault:
						default:
							throw new InvalidOperationException();
#endif
					}
					break;
				case BoxType.Int:
					switch (n2.ValueType)
					{
						case BoxType.Undefined:
							stackslots[dst.index].SetNumber(double.NaN);
							break;
						case BoxType.Number:
						case BoxType.Null:
							stackslots[dst.index].SetNumber(n1.IntValue - Extensions.GetDoubleValue(n2));
							break;
						case BoxType.Boolean:
							stackslots[dst.index].SetInt(n1.IntValue - (n2.Boolean ? 1 : 0));
							break;
						case BoxType.Uint:
							stackslots[dst.index].SetNumber((double)n1.IntValue - n2.UIntValue);
							break;
						case BoxType.Float:
							stackslots[dst.index].SetFloat(n1.IntValue - n2.FloatValue);
							break;
						case BoxType.Int:
						case BoxType.Sbyte:
						case BoxType.Byte:
						case BoxType.Short:
						case BoxType.UShort:
							stackslots[dst.index].SetInt(n1.IntValue - Extensions.GetIntValue(n2));
							break;
#if DEBUG
						case BoxType.Fault:
						default:
							throw new InvalidOperationException();
#endif
					}
					break;
				case BoxType.Uint:
					switch (n2.ValueType)
					{
						case BoxType.Undefined:
							stackslots[dst.index].SetNumber(double.NaN);
							break;
						case BoxType.Number:
						case BoxType.Null:
							stackslots[dst.index].SetNumber(n1.UIntValue - Extensions.GetDoubleValue(n2));
							break;
						case BoxType.Boolean:
							stackslots[dst.index].SetUInt(n1.UIntValue - (n2.Boolean ? 1U : 0U));
							break;
						case BoxType.Int:
							stackslots[dst.index].SetNumber((double)n1.UIntValue - n2.IntValue);
							break;
						case BoxType.Uint:
							stackslots[dst.index].SetUInt(n1.UIntValue - n2.UIntValue);
							break;
						case BoxType.Sbyte:
							stackslots[dst.index].SetNumber((double)n1.UIntValue - n2.SByteValue);
							break;
						case BoxType.Byte:
							stackslots[dst.index].SetUInt(n1.UIntValue - n2.ByteValue);
							break;
						case BoxType.Short:
							stackslots[dst.index].SetNumber((double)n1.UIntValue - n2.ShortValue);
							break;
						case BoxType.UShort:
							stackslots[dst.index].SetUInt(n1.UIntValue - n2.UShortValue);
							break;
						case BoxType.Float:
							stackslots[dst.index].SetFloat((float)n1.UIntValue - n2.FloatValue);
							break;
#if DEBUG
						case BoxType.Fault:
						default:
							throw new InvalidOperationException();
#endif
					}
					break;
				case BoxType.Sbyte:
				case BoxType.Byte:
				case BoxType.Short:
				case BoxType.UShort:
					switch (n2.ValueType)
					{
						case BoxType.Undefined:
							stackslots[dst.index].SetNumber(double.NaN);
							break;
						case BoxType.Number:
						case BoxType.Null:
							stackslots[dst.index].SetNumber(Extensions.GetDoubleValue(n1) - Extensions.GetDoubleValue(n2));
							break;
						case BoxType.Uint:
							stackslots[dst.index].SetNumber(Extensions.GetDoubleValue(n1) - n2.UIntValue);
							break;
						case BoxType.Sbyte:
						case BoxType.Byte:
						case BoxType.Short:
						case BoxType.UShort:
						case BoxType.Boolean:
						case BoxType.Int:
							stackslots[dst.index].SetInt(Extensions.GetIntValue(n1) - Extensions.GetIntValue(n2));
							break;
						case BoxType.Float:
							stackslots[dst.index].SetFloat(Extensions.GetFloatValue(n1) - n2.FloatValue);
							break;
#if DEBUG
						case BoxType.HeapPtr:
						case BoxType.Fault:
						default:
							throw new InvalidOperationException();
#endif
					}
					break;
				case BoxType.Float:
					switch (n2.ValueType)
					{
						case BoxType.Number:
						case BoxType.Undefined:
						case BoxType.Null:

							stackslots[dst.index].SetNumber(Extensions.GetDoubleValue(n1) - Extensions.GetDoubleValue(n2));
							break;
						case BoxType.Boolean:
						case BoxType.Int:
						case BoxType.Sbyte:
						case BoxType.Byte:
						case BoxType.Short:
						case BoxType.UShort:
						case BoxType.Float:
						case BoxType.Uint:
							stackslots[dst.index].SetFloat(Extensions.GetFloatValue(n1) - Extensions.GetFloatValue(n2));
							break;
#if DEBUG
						case BoxType.HeapPtr:
						case BoxType.Fault:
						default:
							throw new InvalidOperationException();
#endif
					}
					break;
#if DEBUG
				case BoxType.HeapPtr:
				case BoxType.Fault:
				default:
					throw new InvalidOperationException();
#endif
			}

		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private unsafe void Exec_Multiply(ref ReceiveError error, NaNBoxing n1, NaNBoxing n2, StackLocater dst, int scope_ptr, StackLocater tmp, Span<NaNBoxing> stackslots, int stackStPos, NaNBoxing thisPtr)
		{

			//操作符重载
			ASClass t1;ASClass t2;
			int op_override_id1 = GetOpOverrideTypeId(n1,out t1);
			int op_override_id2 = GetOpOverrideTypeId(n2,out t2);
			if (op_override_id1 != -1 && op_override_id2 != -1)
			{
				var method = overrideOperatorMethods[(int)OverrideOperator.mul][op_override_id1][op_override_id2];
				if (method != null)
				{
#if FORCOMPILER
					if (IsComputeConstExpr)
					{
						throw new EvalConstException();
					}
#endif
					if (t1 != null)
					{
						InitScript((ASScript)t1._link_codescope.Parent.Container, ref error);
						if (error.raised)
						{
							return;
						}
					}
					if (t2 != null)
					{
						InitScript((ASScript)t2._link_codescope.Parent.Container, ref error);
						if (error.raised)
						{
							return;
						}
					}

					var @class = (ASClass)method.Container;
					
					if (Context.StackPosition + 2 >= Context.STACK_LENGTH)
					{
						RaiseStackOverflow(ref error);
						return;
					}

					Span<NaNBoxing> slots = Context.StackSlots.AsSpan(Context.StackPosition, 2);
					slots[0] = n1;
					slots[1] = n2;

					Context.StackPosition += 2;

					NaNBoxing cls = default; cls.SetHeapPtr(@class.__instance_index__);
					unsafe
					{
						StackLocater* args = stackalloc StackLocater[2];
						args->index = 0;
						(args + 1)->index = 1;
						RunMethod(method, cls, scope_ptr, @class, 2, (byte*)args, slots, ref error, stackStPos + dst.index);
					}
					Context.StackPosition -= 2;

					return;
				}
			}


			if (!IsPrimitive(n1))
			{
				n1 = ToPrimitive(ref error, n1, HINT.h_number, scope_ptr, tmp, dst, stackslots, stackStPos, thisPtr);
				if (error.raised)
				{
					return;
				}
			}

			if (n1.ValueType == BoxType.HeapPtr)
			{
				ConvertValueType(ref error, n1, TypeKind.Number, Context.NUMBER, ref n1); //这里不会出错 这里肯定是字符串
			}

			if (!IsPrimitive(n2))
			{
				n2 = ToPrimitive(ref error, n2, HINT.h_number, scope_ptr, tmp, dst, stackslots, stackStPos, thisPtr);
				if (error.raised)
				{
					return;
				}
			}

			if (n2.ValueType == BoxType.HeapPtr)
			{
				ConvertValueType(ref error, n2, TypeKind.Number, Context.NUMBER, ref n2); //这里不会出错 这里肯定是字符串
			}

			switch (n1.ValueType)
			{
				case BoxType.Number:
				case BoxType.Undefined:
				case BoxType.Null:
					stackslots[dst.index].SetNumber(Extensions.GetDoubleValue(n1) * Extensions.GetDoubleValue(n2));
					break;
				case BoxType.Int:
				case BoxType.Uint:
					switch (n2.ValueType)
					{
						case BoxType.Number:
						case BoxType.Undefined:
						case BoxType.Null:
						case BoxType.Int:
						case BoxType.Uint:
						case BoxType.Boolean:
						case BoxType.Sbyte:
						case BoxType.Byte:
						case BoxType.Short:
						case BoxType.UShort:
							stackslots[dst.index].SetNumber(Extensions.GetDoubleValue(n1) * Extensions.GetDoubleValue(n2));
							break;
						case BoxType.Float:
							stackslots[dst.index].SetFloat( Extensions.GetFloatValue( n1) * Extensions.GetFloatValue(n2));
							break;
#if DEBUG
						default:
							throw new InvalidOperationException();
#endif
					}
					break;
				case BoxType.Boolean:		
				case BoxType.Sbyte:
				case BoxType.Byte:
				case BoxType.Short:
				case BoxType.UShort:

					{
						switch (n2.ValueType)
						{
							case BoxType.Number:
							case BoxType.Undefined:
							case BoxType.Null:
							case BoxType.Int:
							case BoxType.Uint:
								stackslots[dst.index].SetNumber(Extensions.GetDoubleValue(n1) * Extensions.GetDoubleValue(n2));
								break;
							case BoxType.Boolean:
							case BoxType.Sbyte:
							case BoxType.Byte:
							case BoxType.Short:
							case BoxType.UShort:
								stackslots[dst.index].SetInt(Extensions.GetIntValue(n1) * Extensions.GetIntValue(n2));
								break;
							case BoxType.Float:
								stackslots[dst.index].SetFloat( Extensions.GetFloatValue( n1) * Extensions.GetFloatValue(n2));
								break;
#if DEBUG
							default:
								throw new InvalidOperationException();
#endif
						}
					}

					break;
				case BoxType.Float:
					{
						switch (n2.ValueType)
						{
							case BoxType.Number:
							case BoxType.Undefined:
							case BoxType.Null:
								stackslots[dst.index].SetNumber(Extensions.GetDoubleValue(n1) * Extensions.GetDoubleValue(n2));
								break;
							case BoxType.Int:
							case BoxType.Uint:
							case BoxType.Boolean:
							case BoxType.Sbyte:
							case BoxType.Byte:
							case BoxType.Short:
							case BoxType.UShort:
							case BoxType.Float:
								stackslots[dst.index].SetFloat(n1.FloatValue * Extensions.GetFloatValue(n2));
								break;
#if DEBUG
							default:
								throw new InvalidOperationException();
#endif
						}
					}
					break;
#if DEBUG
				default:
					throw new InvalidOperationException();
#endif
			}


		}
		
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private unsafe void Exec_Division(ref ReceiveError error, NaNBoxing n1, NaNBoxing n2, StackLocater dst, int scope_ptr, StackLocater tmp, Span<NaNBoxing> stackslots, int stackStPos, NaNBoxing thisPtr)
		{
			//操作符重载
			ASClass t1;ASClass t2;
			int op_override_id1 = GetOpOverrideTypeId(n1,out t1);
			int op_override_id2 = GetOpOverrideTypeId(n2,out t2);
			if (op_override_id1 != -1 && op_override_id2 != -1)
			{
				var method = overrideOperatorMethods[(int)OverrideOperator.div][op_override_id1][op_override_id2];
				if (method != null)
				{
#if FORCOMPILER
					if (IsComputeConstExpr)
					{
						throw new EvalConstException();
					}
#endif
					if (t1 != null)
					{
						InitScript((ASScript)t1._link_codescope.Parent.Container, ref error);
						if (error.raised)
						{
							return;
						}
					}
					if (t2 != null)
					{
						InitScript((ASScript)t2._link_codescope.Parent.Container, ref error);
						if (error.raised)
						{
							return;
						}
					}

					var @class = (ASClass)method.Container;
					
					if (Context.StackPosition + 2 >= Context.STACK_LENGTH)
					{
						RaiseStackOverflow(ref error);
						return;
					}

					Span<NaNBoxing> slots = Context.StackSlots.AsSpan(Context.StackPosition, 2);
					slots[0] = n1;
					slots[1] = n2;

					Context.StackPosition += 2;

					NaNBoxing cls = default; cls.SetHeapPtr(@class.__instance_index__);
					unsafe
					{
						StackLocater* args = stackalloc StackLocater[2];
						args->index = 0;
						(args + 1)->index = 1;
						RunMethod(method, cls, scope_ptr, @class, 2, (byte*)args, slots, ref error, stackStPos + dst.index);
					}
					Context.StackPosition -= 2;

					return;
				}
			}


			if (!IsPrimitive(n1))
			{
				n1 = ToPrimitive(ref error, n1, HINT.h_number, scope_ptr, tmp, dst, stackslots, stackStPos, thisPtr);
				if (error.raised)
				{
					return;
				}
			}

			if (n1.ValueType == BoxType.HeapPtr)
			{
				ConvertValueType(ref error, n1, TypeKind.Number, Context.NUMBER, ref n1); //这里不会出错 这里肯定是字符串
			}

			if (!IsPrimitive(n2))
			{
				n2 = ToPrimitive(ref error, n2, HINT.h_number, scope_ptr, tmp, dst, stackslots, stackStPos, thisPtr);
				if (error.raised)
				{
					return;
				}
			}

			if (n2.ValueType == BoxType.HeapPtr)
			{
				ConvertValueType(ref error, n2, TypeKind.Number, Context.NUMBER, ref n2); //这里不会出错 这里肯定是字符串
			}

			switch (n1.ValueType)
			{
				case BoxType.Number:
				case BoxType.Undefined:
				case BoxType.Null:
					stackslots[dst.index].SetNumber(Extensions.GetDoubleValue(n1) / Extensions.GetDoubleValue(n2));
					break;
				case BoxType.Boolean:
				case BoxType.Int:
				case BoxType.Uint:
				case BoxType.Sbyte:
				case BoxType.Byte:
				case BoxType.Short:
				case BoxType.UShort:
					switch (n2.ValueType)
					{
						case BoxType.Number:	
						case BoxType.Undefined:
						case BoxType.Null:
						case BoxType.Boolean:
						case BoxType.Int:
						case BoxType.Uint:
						case BoxType.Sbyte:
						case BoxType.Byte:
						case BoxType.Short:
						case BoxType.UShort:
							stackslots[dst.index].SetNumber(Extensions.GetDoubleValue(n1) / Extensions.GetDoubleValue(n2));
							break;
						case BoxType.Float:
							stackslots[dst.index].SetFloat(Extensions.GetFloatValue(n1) / Extensions.GetFloatValue(n2));
							break;
#if DEBUG
						default:
							throw new InvalidOperationException();
#endif
					}
					break;
				case BoxType.Float:

					switch (n2.ValueType)
					{
						case BoxType.Number:
						case BoxType.Undefined:
						case BoxType.Null:
							stackslots[dst.index].SetNumber(Extensions.GetDoubleValue(n1) / Extensions.GetDoubleValue(n2));
							break;
						case BoxType.Boolean:
						case BoxType.Int:
						case BoxType.Uint:
						case BoxType.Sbyte:
						case BoxType.Byte:
						case BoxType.Short:
						case BoxType.UShort:
						case BoxType.Float:
							stackslots[dst.index].SetFloat(Extensions.GetFloatValue(n1) / Extensions.GetFloatValue(n2));
							break;
#if DEBUG
						default:
							throw new InvalidOperationException();
#endif
					}

					break;
#if DEBUG
				default:
					throw new InvalidOperationException();
#endif
			}

		}
		
		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private unsafe void Exec_Modulus(ref ReceiveError error, NaNBoxing n1, NaNBoxing n2, StackLocater dst, int scope_ptr, StackLocater tmp, Span<NaNBoxing> stackslots, int stackStPos, NaNBoxing thisPtr)
		{
			//操作符重载
			ASClass t1;ASClass t2;
			int op_override_id1 = GetOpOverrideTypeId(n1,out t1);
			int op_override_id2 = GetOpOverrideTypeId(n2,out t2);
			if (op_override_id1 != -1 && op_override_id2 != -1)
			{
				var method = overrideOperatorMethods[(int)OverrideOperator.mod][op_override_id1][op_override_id2];
				if (method != null)
				{
#if FORCOMPILER
					if (IsComputeConstExpr)
					{
						throw new EvalConstException();
					}
#endif
					if (t1 != null)
					{
						InitScript((ASScript)t1._link_codescope.Parent.Container, ref error);
						if (error.raised)
						{
							return;
						}
					}
					if (t2 != null)
					{
						InitScript((ASScript)t2._link_codescope.Parent.Container, ref error);
						if (error.raised)
						{
							return;
						}
					}

					var @class = (ASClass)method.Container;
					
					if (Context.StackPosition + 2 >= Context.STACK_LENGTH)
					{
						RaiseStackOverflow(ref error);
						return;
					}

					Span<NaNBoxing> slots = Context.StackSlots.AsSpan(Context.StackPosition, 2);
					slots[0] = n1;
					slots[1] = n2;

					Context.StackPosition += 2;

					NaNBoxing cls = default; cls.SetHeapPtr(@class.__instance_index__);
					unsafe
					{
						StackLocater* args = stackalloc StackLocater[2];
						args->index = 0;
						(args + 1)->index = 1;
						RunMethod(method, cls, scope_ptr, @class, 2, (byte*)args, slots, ref error, stackStPos + dst.index);
					}
					Context.StackPosition -= 2;

					return;
				}
			}

			if (!IsPrimitive(n1))
			{
				n1 = ToPrimitive(ref error, n1, HINT.h_number, scope_ptr, tmp, dst, stackslots, stackStPos, thisPtr);
				if (error.raised)
				{
					return;
				}
			}

			if (n1.ValueType == BoxType.HeapPtr)
			{
				ConvertValueType(ref error, n1, TypeKind.Number, Context.NUMBER, ref n1); //这里不会出错 这里肯定是字符串
			}

			if (!IsPrimitive(n2))
			{
				n2 = ToPrimitive(ref error, n2, HINT.h_number, scope_ptr, tmp, dst, stackslots, stackStPos, thisPtr);
				if (error.raised)
				{
					return;
				}
			}

			if (n2.ValueType == BoxType.HeapPtr)
			{
				ConvertValueType(ref error, n2, TypeKind.Number, Context.NUMBER, ref n2); //这里不会出错 这里肯定是字符串
			}

			switch (n1.ValueType)
			{
				case BoxType.Number:
				case BoxType.Undefined:
				case BoxType.Null:
					stackslots[dst.index].SetNumber(Extensions.GetDoubleValue(n1) % Extensions.GetDoubleValue(n2));
					break;
				case BoxType.Uint:
					
				case BoxType.Byte:

				case BoxType.UShort:

				case BoxType.Boolean:
				case BoxType.Int:
				case BoxType.Sbyte:
				case BoxType.Short:
					switch (n2.ValueType)
					{
						case BoxType.Number:
						case BoxType.Undefined:
						case BoxType.Null:
						case BoxType.Boolean:
						//stackslots[dst.index].SetNumber(Extensions.GetDoubleValue(n1) % Extensions.GetDoubleValue(n2));
						//break;
						case BoxType.Int:
						case BoxType.Sbyte:
						case BoxType.Short:
						//stackslots[dst.index].SetNumber(n1.UIntValue % Extensions.GetIntValue(n2));
						//break;
						case BoxType.Uint:
						//stackslots[dst.index].SetUInt(n1.UIntValue % n2.UIntValue);
						//break;
						case BoxType.Byte:
						//stackslots[dst.index].SetUInt(n1.UIntValue % n2.ByteValue);
						//break;
						case BoxType.UShort:
							//stackslots[dst.index].SetUInt(n1.UIntValue % n2.UShortValue); 因为有 % 0 除以0问题，所以只能都用Number
							stackslots[dst.index].SetNumber(Extensions.GetDoubleValue(n1) % Extensions.GetDoubleValue(n2));
							break;
						case BoxType.Float:
							stackslots[dst.index].SetFloat(Extensions.GetFloatValue(n1) % Extensions.GetFloatValue(n2));
							break;
#if DEBUG
						default:
							throw new InvalidOperationException();
#endif
					}

					break;
				case BoxType.Float:

					switch (n2.ValueType)
					{
						case BoxType.Number:
						case BoxType.Undefined:
						case BoxType.Null:
							stackslots[dst.index].SetNumber(Extensions.GetDoubleValue(n1) % Extensions.GetDoubleValue(n2));
							break;
						case BoxType.Boolean:
						case BoxType.Int:
						case BoxType.Uint:
						case BoxType.Sbyte:
						case BoxType.Byte:
						case BoxType.Short:
						case BoxType.UShort:
						case BoxType.Float:
							stackslots[dst.index].SetFloat(Extensions.GetFloatValue(n1) % Extensions.GetFloatValue(n2));
							break;
#if DEBUG
						default:
							throw new InvalidOperationException();
#endif
					}

					break;
#if DEBUG
				default:
					throw new InvalidOperationException();
#endif

			}


		}


		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private unsafe void Exec_bitWise(ref ReceiveError error, NaNBoxing n1, NaNBoxing n2, StackLocater dst, byte opMode, int scope_ptr, StackLocater v1, StackLocater v2, Span<NaNBoxing> stackslots, int stackStPos, NaNBoxing thisPtr)
		{
			

			switch (opMode)
			{
				case 0: // &
					{
						if (!IsPrimitive(n1))
						{
							n1 = ToPrimitive(ref error, n1, HINT.h_number, scope_ptr, v1, dst, stackslots, stackStPos, thisPtr);
							if (error.raised)
							{
								return;
							}
						}

						if (!IsPrimitive(n2))
						{
							n2 = ToPrimitive(ref error, n2, HINT.h_number, scope_ptr, v2, dst, stackslots, stackStPos, thisPtr);
							if (error.raised)
							{
								return;
							}
						}

						ConvertValueType(ref error, n1, TypeKind.Uint, Context.UINT, ref n1);
						ConvertValueType(ref error, n2, TypeKind.Uint, Context.UINT, ref n2);

						stackslots[dst.index].SetInt( (int)(n1.UIntValue & n2.UIntValue) );

					}
					break;
				case 1:
					{
						if (!IsPrimitive(n1))
						{
							n1 = ToPrimitive(ref error, n1, HINT.h_number, scope_ptr, v1, dst, stackslots, stackStPos, thisPtr);
							if (error.raised)
							{
								return;
							}
						}

						if (!IsPrimitive(n2))
						{
							n2 = ToPrimitive(ref error, n2, HINT.h_number, scope_ptr, v2, dst, stackslots, stackStPos, thisPtr);
							if (error.raised)
							{
								return;
							}
						}

						ConvertValueType(ref error, n1, TypeKind.Int, Context.INT, ref n1);
						ConvertValueType(ref error, n2, TypeKind.Int, Context.INT, ref n2);

						stackslots[dst.index].SetInt((int)(n1.IntValue << n2.IntValue));
					}
					break;
				case 2: // ~
					{
						if (!IsPrimitive(n1))
						{
							n1 = ToPrimitive(ref error, n1, HINT.h_number, scope_ptr, v1, dst, stackslots, stackStPos, thisPtr);
							if (error.raised)
							{
								return;
							}
						}

						ConvertValueType(ref error, n1, TypeKind.Int, Context.INT, ref n1);


						stackslots[dst.index].SetInt( ~n1.IntValue );
					}
					break;
				case 3: // |
					{
						if (!IsPrimitive(n1))
						{
							n1 = ToPrimitive(ref error, n1, HINT.h_number, scope_ptr, v1, dst, stackslots, stackStPos, thisPtr);
							if (error.raised)
							{
								return;
							}
						}

						if (!IsPrimitive(n2))
						{
							n2 = ToPrimitive(ref error, n2, HINT.h_number, scope_ptr, v2, dst, stackslots, stackStPos, thisPtr);
							if (error.raised)
							{
								return;
							}
						}

						ConvertValueType(ref error, n1, TypeKind.Uint, Context.UINT, ref n1);
						ConvertValueType(ref error, n2, TypeKind.Uint, Context.UINT, ref n2);

						stackslots[dst.index].SetInt((int)(n1.UIntValue | n2.UIntValue));
					}
					break;
				case 4:
					{
						if (!IsPrimitive(n1))
						{
							n1 = ToPrimitive(ref error, n1, HINT.h_number, scope_ptr, v1, dst, stackslots, stackStPos, thisPtr);
							if (error.raised)
							{
								return;
							}
						}

						if (!IsPrimitive(n2))
						{
							n2 = ToPrimitive(ref error, n2, HINT.h_number, scope_ptr, v2, dst, stackslots, stackStPos, thisPtr);
							if (error.raised)
							{
								return;
							}
						}

						ConvertValueType(ref error, n1, TypeKind.Int, Context.INT, ref n1);
						ConvertValueType(ref error, n2, TypeKind.Int, Context.INT, ref n2);

						
						stackslots[dst.index].SetInt((n1.IntValue >> n2.IntValue));
					}
					break;
				case 5:
					{
						if (!IsPrimitive(n1))
						{
							n1 = ToPrimitive(ref error, n1, HINT.h_number, scope_ptr, v1, dst, stackslots, stackStPos, thisPtr);
							if (error.raised)
							{
								return;
							}
						}

						if (!IsPrimitive(n2))
						{
							n2 = ToPrimitive(ref error, n2, HINT.h_number, scope_ptr, v2, dst, stackslots, stackStPos, thisPtr);
							if (error.raised)
							{
								return;
							}
						}

						ConvertValueType(ref error, n1, TypeKind.Uint, Context.UINT, ref n1);
						ConvertValueType(ref error, n2, TypeKind.Int, Context.INT, ref n2);

						stackslots[dst.index].SetUInt((n1.UIntValue >> n2.IntValue));
					}
					break;
				case 6: //xor
					{
						if (!IsPrimitive(n1))
						{
							n1 = ToPrimitive(ref error, n1, HINT.h_number, scope_ptr, v1, dst, stackslots, stackStPos, thisPtr);
							if (error.raised)
							{
								return;
							}
						}

						if (!IsPrimitive(n2))
						{
							n2 = ToPrimitive(ref error, n2, HINT.h_number, scope_ptr, v2, dst, stackslots, stackStPos, thisPtr);
							if (error.raised)
							{
								return;
							}
						}

						ConvertValueType(ref error, n1, TypeKind.Uint, Context.UINT, ref n1);
						ConvertValueType(ref error, n2, TypeKind.Uint, Context.UINT, ref n2);

						stackslots[dst.index].SetInt((int)(n1.UIntValue ^ n2.UIntValue));
					}
					break;
#if DEBUG
				default:
					throw new NotImplementedException();
					break;
#endif
			}




		}


		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private unsafe void Exec_Comparse(ref ReceiveError error, NaNBoxing n1, NaNBoxing n2, StackLocater dst, byte opMode ,int scope_ptr, StackLocater v1, StackLocater v2 , Span<NaNBoxing> stackslots, int stackStPos, NaNBoxing thisPtr)
		{
			if (!IsPrimitive(n1))
			{
				n1 = ToPrimitive(ref error, n1, HINT.h_number, scope_ptr, v1, dst, stackslots, stackStPos, thisPtr);
				if (error.raised)
				{
					return;
				}
			}

			if (!IsPrimitive(n2))
			{
				n2 = ToPrimitive(ref error, n2, HINT.h_number, scope_ptr, v2, dst, stackslots, stackStPos, thisPtr);
				if (error.raised)
				{
					return;
				}
			}

			int c_r;

			// 处理字符串比较的各种情况
			if ((n1.ValueType == BoxType.HeapPtr && Context.GC.Heap[n1.HeapPtr].TypeKind == RtHeapTypeKind.STRING) ||
			    n1.ValueType == BoxType.LocalString)
			{
				if ((n2.ValueType == BoxType.HeapPtr && Context.GC.Heap[n2.HeapPtr].TypeKind == RtHeapTypeKind.STRING) ||
				    n2.ValueType == BoxType.LocalString)
				{
					// 两个都是字符串类型，进行字符串比较
					string str1, str2;
					
					if (n1.ValueType == BoxType.LocalString)
					{
						// Use efficient char-based extraction to avoid string allocation when possible
						Span<char> chars1 = stackalloc char[16];
						int charCount1 = n1.GetLocalStringChars(chars1);
						str1 = charCount1 > 0 ? new string(chars1.Slice(0, charCount1)) : string.Empty;
					}
					else
					{
						str1 = ((RtPayloadString)Context.GC.Heap[n1.HeapPtr].facility).Str;
					}
					
					if (n2.ValueType == BoxType.LocalString)
					{
						// Use efficient char-based extraction to avoid string allocation when possible
						Span<char> chars2 = stackalloc char[16];
						int charCount2 = n2.GetLocalStringChars(chars2);
						str2 = charCount2 > 0 ? new string(chars2.Slice(0, charCount2)) : string.Empty;
					}
					else
					{
						str2 = ((RtPayloadString)Context.GC.Heap[n2.HeapPtr].facility).Str;
					}

					int c = string.CompareOrdinal(str1, str2);
					c_r = c;
				}
				else
				{
					// n1是字符串，n2不是字符串，转换为数字比较
					ConvertValueType(ref error, n1, TypeKind.Number, Context.NUMBER, ref n1);
#if DEBUG
					if (error.raised)
					{
						throw new InvalidOperationException();
					}
#endif
					ConvertValueType(ref error, n2, TypeKind.Number, Context.NUMBER, ref n2);
#if DEBUG
					if (error.raised)
					{
						throw new InvalidOperationException();
					}
#endif

					if (double.IsNaN(n1.Number) || double.IsNaN(n2.Number))
					{
						stackslots[dst.index].SetBoolean(false);
						return;
					}

					if (n1.Number < n2.Number)
						c_r = -1;
					else if (n1.Number == n2.Number)
						c_r = 0;
					else
						c_r = 1;
				}
			}
			else if ((n2.ValueType == BoxType.HeapPtr && Context.GC.Heap[n2.HeapPtr].TypeKind == RtHeapTypeKind.STRING) ||
			         n2.ValueType == BoxType.LocalString)
			{
				// n1不是字符串，n2是字符串，转换为数字比较
				ConvertValueType(ref error, n1, TypeKind.Number, Context.NUMBER, ref n1);
#if DEBUG
				if (error.raised)
				{
					throw new InvalidOperationException();
				}
#endif
				ConvertValueType(ref error, n2, TypeKind.Number, Context.NUMBER, ref n2);
#if DEBUG
				if (error.raised)
				{
					throw new InvalidOperationException();
				}
#endif

				if (double.IsNaN(n1.Number) || double.IsNaN(n2.Number))
				{
					stackslots[dst.index].SetBoolean(false);
					return;
				}

				if (n1.Number < n2.Number)
					c_r = -1;
				else if (n1.Number == n2.Number)
					c_r = 0;
				else
					c_r = 1;
			}
			else
			{
				// 两个都不是字符串，转换为数字比较
				ConvertValueType(ref error, n1, TypeKind.Number, Context.NUMBER, ref n1); //这里不会失败
#if DEBUG
				if (error.raised)
				{
					throw new InvalidOperationException();
				}
#endif
				ConvertValueType(ref error, n2, TypeKind.Number, Context.NUMBER, ref n2); //这里不会失败
#if DEBUG
				if (error.raised)
				{
					throw new InvalidOperationException();
				}
#endif

				if (double.IsNaN(n1.Number) || double.IsNaN(n2.Number))
				{
					stackslots[dst.index].SetBoolean(false);
					return;
				}

				if (n1.Number < n2.Number)
					c_r = -1;
				else if (n1.Number == n2.Number)
					c_r = 0;
				else
					c_r = 1;
			}

			switch (opMode)
			{
				case 0:
					stackslots[dst.index].SetBoolean(c_r < 0);
					break;
				case 1:
					stackslots[dst.index].SetBoolean(c_r > 0);
					break;
				case 2:
					stackslots[dst.index].SetBoolean((c_r <= 0));
					break;
				case 3:
					stackslots[dst.index].SetBoolean((c_r >= 0));
					break;
				default:
#if DEBUG
					throw new InvalidOperationException();
#else
					Environment.FailFast("出错了，这里跑不到");
					break;
#endif

			}

		}



		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private bool Is(NaNBoxing v,ASClass typeclass)
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
							return Math.Abs( v.IntValue ) <= MAX_LOSSLESS;
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
							return v.SByteValue>=0;
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
							return v.ShortValue>=0;
						}
						else if (typeclass == Context.INT)
						{
							return true;
						}
						else if (typeclass == Context.USHORT)
						{
							return v.ShortValue>= ushort.MinValue ;
						}
						else if (typeclass == Context.SHORT)
						{
							return true;
						}
						else if (typeclass == Context.BYTE)
						{
							return v.ShortValue >= byte.MinValue && v.ShortValue<=byte.MaxValue;
						}
						else if (typeclass == Context.SBYTE)
						{
							return v.ShortValue >= sbyte.MinValue && v.ShortValue <=byte.MaxValue;
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
							return  v.UShortValue <= short.MaxValue ;
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
						var v_instance = Context.GC.Heap[v.HeapPtr];
						switch (v_instance.TypeKind)
						{
							case RtHeapTypeKind.CLASS:
								return (typeclass == Context.OBJECT || typeclass == Context.CLASS);
							case RtHeapTypeKind.GLOBAL:
								return (typeclass == Context.OBJECT);
							case RtHeapTypeKind.STRING:
								return (typeclass == Context.STRING || typeclass == Context.OBJECT);
							case RtHeapTypeKind.INSTANCE:

								bool pass = typeclass == Context.OBJECT ||
									v_instance.Type == typeclass.Instance ||
									Extensions.IsExtend((ASInstance)v_instance.Type, typeclass.Instance) ||
									Extensions.IsImplements((ASInstance)v_instance.Type, typeclass.Instance);

								return pass;

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

										if (((RtPayloadVector)v_instance.facility).element_asclass == typeclass.Instance._element_class)
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


		internal int GetProtoPtr(RtHeapInstance obj)
		{
			int o_proto;
			switch (obj.TypeKind)
			{
				case RtHeapTypeKind.INSTANCE:
					o_proto = ((RtPayloadInstance)obj.facility).PROTOTYPE(this, (ASInstance)obj.Type);
					break;
				case RtHeapTypeKind.CLOSURE:
					if (((ASMethodBody)obj.Type).Method.__ismethod)
					{
						o_proto = ((RtPayloadScriptClass)Context.GC.Heap[Context.METHOD_CLOSURE.__instance_index__].facility).PROTO__PTR;
					}
					else
					{
						o_proto = ((RtPayloadClosure)obj.facility).PROTOTYPE(this);
						if (o_proto <= 0) //默认，指向$FUNCTION的proto
						{
							o_proto = ((RtPayloadScriptClass)Context.GC.Heap[Context.FUNCTION.__instance_index__].facility).PROTO__PTR;
							if (o_proto <= 0) // Function.prototype默认是一个closure,也可能<=0。那么就指向到Object.proto
							{
								o_proto = ((RtPayloadScriptClass)Context.GC.Heap[Context.OBJECT.__instance_index__].facility).PROTO__PTR;
							}
						}
					}
					break;
				case RtHeapTypeKind.ARRAY:
					o_proto = ((RtPayloadScriptClass)Context.GC.Heap[Context.ARRAY.__instance_index__].facility).PROTO__PTR;
					break;
				case RtHeapTypeKind.VECTOR:
					o_proto = ((RtPayloadScriptClass)Context.GC.Heap[obj.Type._link_codescope.TypeLayout.ASType.__instance_index__].facility).PROTO__PTR;
					break;
				case RtHeapTypeKind.CLASS:
					o_proto = ((RtPayloadScriptClass)Context.GC.Heap[Context.CLASS.__instance_index__].facility).PROTO__PTR;
					//o_proto = ((RtPayloadScriptClass)obj.facility).PROTO__PTR;
					break;
				case RtHeapTypeKind.NAMESPACE:
					o_proto = ((RtPayloadScriptClass)Context.GC.Heap[Context.NAMESPACE.__instance_index__].facility).PROTO__PTR;
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
			unsafe void Resume(ExceptionContext* e_ctx,  ExceptionContext** current_e_ctx , byte* PC_START, byte** PC , Span<NaNBoxing> stackslots);

			unsafe void End();

			bool IsCallClose();


#if DEBUG
			unsafe void Debug_SaveOrLoadIterCtxIndex(int* iter_ctx_index);
#endif

		}

		private Memory<char> frame_holdchars = new Memory<char>(new char[16]);
		internal unsafe void Execute(ref ASMethodBody.MethodBodyInfo info, RtHeapInstance methodscope, NaNBoxing thisPtr, int scope_ptr, ASContainer scopeType,
			Span<NaNBoxing> stackslots,
			int stackStPos, out int PC_PTR, ref ReceiveError error, int returnSlotIndex, int calleelastPos,IResume_State resume_state)
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
				ExceptionContext* exception_ctx_stack = stackalloc ExceptionContext[ (method.Flags.HasFlag( MethodFlags.NoTry)?0: Context.MAX_TRY_NESTED) + 2]; //加头尾2个哨兵
				ExceptionContext* NO_TRY = exception_ctx_stack;
				ExceptionContext* exception_ctx = NO_TRY;



				int* method_scopes = stackalloc int[64]; //64个，肯定不可能爆了。
				StackLocater* tmpArgLoc = stackalloc StackLocater[2]; //getter setter等的临时空间


				Span<NaNBoxing> constants = new Span<NaNBoxing>(p + 3 * sizeof(int) + 2 * sizeof(int) * info.instructions, info.constants); //(NaNBoxing*)((int*)p + 3);

				byte* PC = p + sizeof(int) * 3 + 2 * sizeof(int) * info.instructions + sizeof(NaNBoxing) * info.constants;
				byte* PC_START = PC;
				byte* PC_END = p + method.Body.ByteCode.Length - 4; //已经四字节对齐，所以最后一个END指令长度也是4

				if (resume_state != null)
				{
					resume_state.Resume(exception_ctx_stack, &exception_ctx , PC_START ,&PC,stackslots);

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

				NaNBoxing global_obj = default;
				
				while (true)
				{

					int codeanddst = *(int*)PC; PC += 4;
					INS_Code opcode = (INS_Code)(byte)(codeanddst & 0xff);
					int dst_index = codeanddst >> 8;

#if PROFILEPLAYER
					InstructionProfiler.Profile_ActionStart(opcode);
#endif

					switch (opcode)
					{
						case INS_Code.flag:

							break;
						case INS_Code.ld_const:
							{
								StackLocater stackLocater;
								stackLocater.index = dst_index;

								int const_id = *(int*)PC; PC += 4;

								stackslots[stackLocater.index] = constants[const_id];

							}
							break;
						//case INS_Code.short_ld_const:
						//	{
						//		int const_id = (int)((uint)dst_index >> 8);
						//		stackslots[(int)((uint)dst_index & 0xff)] = constants[const_id];
						//	}
						//	break;
						case INS_Code.ld_false:
							{
								StackLocater stackLocater;
								stackLocater.index = dst_index;

								stackslots[stackLocater.index].SetBoolean(false);
							}
							break;
						case INS_Code.ld_true:
							{
								StackLocater stackLocater;
								stackLocater.index = dst_index;


								stackslots[stackLocater.index].SetBoolean(true);
							}
							break;
						case INS_Code.ld_null:
							{
								StackLocater stackLocater;
								stackLocater.index = dst_index;
								stackslots[stackLocater.index].SetNull();
							}
							break;
						case INS_Code.ld_undefined:
							{
								StackLocater stackLocater;
								stackLocater.index = dst_index;

								stackslots[stackLocater.index].SetUndefined();
							}
							break;
						case INS_Code.ld_array_hole:
							{
								StackLocater stackLocater;
								stackLocater.index = dst_index;

								stackslots[stackLocater.index].setFault();
							}
							break;
						case INS_Code.ld_class:
							{
								StackLocater stackLocater;
								stackLocater.index = dst_index;

								int classid_index = 0;
								LoadInt32(&classid_index, &PC);

								var boxing = constants[classid_index];


#if DEBUG
								if (boxing.ValueType != NaNBoxing.BoxType.Uint)
								{
									throw new InvalidOperationException();
								}
#endif

								//RtHeapInstance instance = Context.GC.Heap[boxing.HeapPtr];
#if DEBUG
								//if (instance.TypeKind != RtHeapTypeKind.CACHE_LD_CLASS)
								//{
								//    throw new InvalidOperationException();
								//}
#endif
								//InitASClass((ASClass)instance.Type, ref error);
								var @class = Context.link_const_class[(int)boxing.UIntValue];
								InitScript((ASScript)@class._link_codescope.Parent.Container, ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}
								stackslots[stackLocater.index].SetHeapPtr(@class.__instance_index__);

							}
							break;
						case INS_Code.ld_VectorType:
							{
								StackLocater stackLocater;
								stackLocater.index = dst_index;

								int vector_index = 0;
								LoadInt32(&vector_index, &PC);

								var boxing = constants[vector_index];
#if DEBUG
								if (boxing.ValueType != NaNBoxing.BoxType.Int)
								{
									throw new InvalidOperationException();
								}
#endif

								ASVector vector = Context.Vectors[boxing.IntValue];

								InitASClass(vector.vector_class, ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}

								stackslots[stackLocater.index].SetHeapPtr(vector.vector_class.__instance_index__);

							}
							break;
						case INS_Code.ld_namespace:
							{
								StackLocater stackLocater;
								stackLocater.index = dst_index;

								int namespace_instance_index = 0;
								LoadInt32(&namespace_instance_index, &PC);

								var boxing = constants[namespace_instance_index];

#if DEBUG
								if (boxing.ValueType != NaNBoxing.BoxType.HeapPtr)
								{
									throw new InvalidOperationException();
								}

								RtHeapInstance instance = Context.GC.Heap[boxing.HeapPtr];
								if (instance.TypeKind != RtHeapTypeKind.NAMESPACE)
								{
									throw new InvalidOperationException();
								}

#endif

								stackslots[stackLocater.index].SetHeapPtr(boxing.HeapPtr);

							}
							break;
						case INS_Code.delete:
							{
								StackLocater stack;
								stack.index = dst_index;

								StackLocater todelete;
								LoadStackLocater(&todelete, &PC);

								NaNBoxing box = stackslots[todelete.index];

								if (box.ValueType == NaNBoxing.BoxType.HeapPtr)
								{
									RtHeapInstance rtHeap = Context.GC.Heap[box.HeapPtr];
									if (rtHeap.TypeKind == RtHeapTypeKind.STACK_CACHE_OBJ)
									{
										RtPayloadStackCache _obj = (RtPayloadStackCache)rtHeap.facility;

										if (_obj.RefInstance.ValueType != BoxType.HeapPtr)
										{
											RaiseReferenceError_CanNotDeleteProperty(ref error,_obj.RefInstance);
											goto flag_handle_error;
											//throw new NotImplementedException();
										}
										else
										{
											RtHeapInstance refObj = Context.GC.Heap[_obj.RefInstance.HeapPtr];

											if (_obj.searchPropertyName.ValueType == BoxType.HeapPtr || _obj.searchPropertyName.ValueType == BoxType.LocalString) //动态属性
											{

												//string searchName = ((RtPayloadString)Context.GC.Heap[_obj.searchPropertyNamePtr].facility).Str;
												
												ReadOnlySpan<char> searchName;
												if (_obj.searchPropertyName.ValueType == BoxType.HeapPtr)
												{
													searchName = ((RtPayloadString)Context.GC.Heap[_obj.searchPropertyName.HeapPtr].facility).Str;
												}
												else
												{
													Span<char> temp = frame_holdchars.Span; //stackalloc char[16];//用于从LocalString中提取值
													int l = _obj.searchPropertyName.GetLocalStringChars(temp);
													searchName = temp.Slice(0, l);
												}

												_obj.searchPropertyName.SetUndefined();

												NaNBoxing ns = new NaNBoxing();
												ASNamespace @namespace = null;
												if (_obj.searchNameSpacePtr > 0)
												{
													ns.SetHeapPtr(_obj.searchNameSpacePtr);
													RtHeapInstance ns_instance = Context.GC.Heap[_obj.searchNameSpacePtr];
													@namespace = ((RtPayloadNameSpace)ns_instance.facility).ASNamespace;
													_obj.searchNameSpacePtr = 0;
												}

												if (refObj.TypeKind == RtHeapTypeKind.INSTANCE
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
													//不可删除，返回false
													stackslots[stack.index].SetBoolean(false);
												}
												else if (refObj.TypeKind == RtHeapTypeKind.VECTOR)
												{
													//不可删除，返回false
													stackslots[stack.index].SetBoolean(false);
												}
												else if (refObj.TypeKind == RtHeapTypeKind.ARRAY &&
														((RtPayloadArray)refObj.facility).isArguments()
														&& @namespace == null
														&& "callee".AsSpan().CompareTo( searchName, StringComparison.Ordinal) == 0
													)
												{
													Context.StackSlots[stackStPos - method.Body._link_codescope.Members.Count - 1].SetUndefined();
													stackslots[stack.index].SetBoolean(true);
												}
												else
												{
													NaNBoxing value; int shape_ptr; int index; RtPayloadDynamic prop;
													if (FindDynamicValue(refObj, searchName, out value, out shape_ptr, out index, out prop))
													{
														RtPayloadShape shape = (RtPayloadShape)Context.GC.Heap[shape_ptr].facility;

														if (shape.Attribute.HasFlag(RtPayloadShape.PropertyAttribute.Configurable))
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
												if (refObj.TypeKind == RtHeapTypeKind.ARRAY)
												{
#if DEBUG
													if (_obj.indexer_key.ValueType == BoxType.Uint)
#endif
													{
														stackslots[stack.index].SetBoolean(((RtPayloadArray)refObj.facility).Delete(_obj.indexer_key.UIntValue,this));
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
														(refObj.TypeKind == RtHeapTypeKind.INSTANCE  && ((ASInstance)refObj.Type).Flags.HasFlag(ClassFlags.Indexer))
														||
														refObj.TypeKind == RtHeapTypeKind.VECTOR
														)
														)
													{
														throw new InvalidOperationException();
													}
#endif


													if (refObj.TypeKind == RtHeapTypeKind.VECTOR )
													{
														if (!RtPayloadVector.IsValidIndexType(_obj.indexer_key))
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
														_this.SetHeapPtr(_obj.RefInstance.HeapPtr);

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
									else if (rtHeap.TypeKind == RtHeapTypeKind.CLOSURE)
									{
										//不可删除，返回false
										stackslots[stack.index].SetBoolean(false);
									}
									else if (rtHeap.TypeKind == RtHeapTypeKind.INSTANCE)
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
							break;
						case INS_Code.ld_MultiName_Ref:
							{
								StackLocater stack;
								stack.index = dst_index;

								StackLocater src;
								LoadStackLocater(&src, &PC);

								int const_id;
								{
									LoadInt32(&const_id, &PC);
									//byte* _p = (byte*)&const_id;
									//*_p++ = *PC++;
									//*_p++ = *PC++;
									//*_p++ = *PC++;
									//*_p = *PC++;
								}

								string name = ((RtPayloadString)Context.GC.Heap[constants[const_id].HeapPtr].facility).Str;

								//int instancePtr;
								NaNBoxing instance;
								RtHeapTypeKind kind;
								ASContainer as_type;
								ReadInstanceFromStacklocater(ref error, src, stackslots, stackStPos, scope_ptr, out kind, out instance, out as_type);
								if (error.raised)
								{
									goto flag_handle_error;
								}

								switch (instance.ValueType)
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
										break;
									case NaNBoxing.BoxType.Byte:
										as_type = Context.BYTE.Instance;
										kind = (RtHeapTypeKind)255;
										break;
									case NaNBoxing.BoxType.Number:
										as_type = Context.NUMBER.Instance;
										kind = (RtHeapTypeKind)255;
										break;
									case NaNBoxing.BoxType.Short:
										as_type = Context.SHORT.Instance;
										kind = (RtHeapTypeKind)255;
										break;
									case NaNBoxing.BoxType.UShort:
										as_type = Context.USHORT.Instance;
										kind = (RtHeapTypeKind)255;
										break;
									case NaNBoxing.BoxType.Int:
										as_type = Context.INT.Instance;
										kind = (RtHeapTypeKind)255;
										break;
									case NaNBoxing.BoxType.Uint:
										as_type = Context.INT.Instance;
										kind = (RtHeapTypeKind)255;
										break;
									case NaNBoxing.BoxType.Float:
										as_type = Context.FLOAT.Instance;
										kind = (RtHeapTypeKind)255;
										break;
									case NaNBoxing.BoxType.Boolean:
										as_type = Context.BOOLEAN.Instance;
										kind = (RtHeapTypeKind)255;
										break;
									case BoxType.LocalString:
										as_type = Context.STRING.Instance;
										kind = (RtHeapTypeKind)255;
										break;
#if DEBUG
									case NaNBoxing.BoxType.Fault:
									default:
										throw new InvalidOperationException();
#endif
								}



								var scope = methodscope; //Context.GC.Heap[scope_ptr];
#if DEBUG
								if (scope.TypeKind != RtHeapTypeKind.MethodScope)
								{
									throw new InvalidOperationException();
								}
#endif

								var ns_set = scope.Type._link_codescope.NamespaceSet;

								int code = MultiNameLSearch(ns_set, kind, as_type, name, stack, stackslots, stackStPos, instance, thisPtr, ref error);

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
									//case 3:
									//   RaiseReferenceError_MulitNameNotFound(ref error, name, as_type.QName);
									//   goto flag_handle_error;
#if DEBUG
									default:
										throw new InvalidOperationException();
#endif
								}

								break;
								//lbl_multiname_dynamicprop:
								//lbl_multiname_success:
								//    break;
								//lbl_multiname_notfound:
								//    RaiseReferenceError_MulitNameNotFound(ref error, name,as_type.QName);
								//    goto flag_handle_error;
								//lbl_multiname_ambiguous:
								//    RaiseTypeError_Ambiguous(ref error, name );
								//    goto flag_handle_error;
							}
						case INS_Code.ld_MultiNameL_Ref:
							{
								StackLocater stack;
								stack.index = dst_index;

								StackLocater src;
								LoadStackLocater(&src, &PC);

								StackLocater _name;
								LoadStackLocater(&_name, &PC);

								int super_const_index;
								LoadInt32(&super_const_index, &PC);



								NaNBoxing instance_box;
								RtHeapTypeKind kind;
								ASContainer as_type;

								ReadInstanceFromStacklocater(ref error, src, stackslots, stackStPos, scope_ptr, out kind, out instance_box, out as_type);
								if (error.raised)
								{
									goto flag_handle_error;
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
									if (as_type is ASInstance)
									{
										if (!((ASInstance)as_type).IsExtend(super_class.Instance))
										{
											throw new InvalidOperationException();
										}
									}

#endif

									as_type = super_class.Instance;
								}

								RtHeapInstance instance = null;

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

								instance = Context.GC.Heap[instance_box.HeapPtr];

							lbl_instance_primitive:

								string name;

								NaNBoxing prop_name = stackslots[_name.index];

								if (instance != null && (instance.TypeKind == RtHeapTypeKind.INSTANCE || 
									
									(instance.TypeKind == RtHeapTypeKind.VECTOR && RtPayloadVector.IsValidIndexType(prop_name))
									
									) && ((ASInstance)instance.Type).Flags.HasFlag(ClassFlags.Indexer))
								{
									//索引器处理
									int ptrIndex = stackStPos + stack.index;
									int cacheobjpointer = Context.CacheObjPtr + ptrIndex;  //Context.CacheObjectPointers[ptrIndex];
									RtHeapInstance cache = Context.GC.Heap[cacheobjpointer];
#if DEBUG
									if (cache.TypeKind != RtHeapTypeKind.STACK_CACHE_OBJ)
									{
										throw new InvalidOperationException();
									}
#endif


									RtPayloadStackCache cachePayload = (RtPayloadStackCache)cache.facility;
									cachePayload.RefInstance = instance_box;
									cachePayload.trait[0] = null; cachePayload.trait[1] = null;
									cachePayload.scopemember_index = 0;
									cachePayload.searchPropertyName.SetUndefined(); cachePayload.as_type = instance.Type;
									cachePayload.searchNameSpacePtr = 0; cachePayload.indexer_key = prop_name;

									stackslots[stack.index].SetHeapPtr(cacheobjpointer);

									break;
								}
								else if (prop_name.ValueType != NaNBoxing.BoxType.HeapPtr)
								{
									if (instance != null && instance.TypeKind == RtHeapTypeKind.ARRAY)
									{
										long index;

										switch (prop_name.ValueType)
										{
											case BoxType.LocalString:
												// Use efficient char-based extraction to avoid string allocation
												Span<char> chars = stackalloc char[16];
												int charCount = prop_name.GetLocalStringChars(chars);
												name = charCount > 0 ? new string(chars.Slice(0, charCount)) : string.Empty;
												goto lbl_name_solved;
											case NaNBoxing.BoxType.Number:
												{
													double v = prop_name.Number;
													if (v >=0 && (v == Math.Truncate(v)) && !Double.IsInfinity(v) && v< uint.MaxValue)
													{
														index = (long)v;
														if (index >= 0 && index< uint.MaxValue)
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
														name = Extensions.GetPrimitiveValueToString(this, prop_name) ;
														goto array_prop;
													}
												}
											case NaNBoxing.BoxType.Float:
												{
													double v = prop_name.FloatValue;
													if ( v >=0 && (v == Math.Truncate(v)) && !Double.IsInfinity(v) && v<uint.MaxValue)
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
														name = Extensions.GetPrimitiveValueToString(this, prop_name);
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
										RtHeapInstance cache = Context.GC.Heap[cacheobjpointer];
#if DEBUG
										if (cache.TypeKind != RtHeapTypeKind.STACK_CACHE_OBJ)
										{
											throw new InvalidOperationException();
										}
#endif

										RtPayloadStackCache cachePayload = (RtPayloadStackCache)cache.facility;
										cachePayload.RefInstance = instance_box;
										cachePayload.trait[0] = null; cachePayload.trait[1] = null;
										cachePayload.scopemember_index = 0;
										cachePayload.searchPropertyName.SetUndefined(); cachePayload.as_type = instance.Type;
										cachePayload.searchNameSpacePtr = 0; cachePayload.indexer_key.SetUInt(array_i);

										stackslots[stack.index].SetHeapPtr(cacheobjpointer);

										break;

									array_prop:;


									}

									else if (instance != null && instance.TypeKind == RtHeapTypeKind.VECTOR)
									{
										//不合理的索引范围
#if DEBUG
										if (RtPayloadVector.IsValidIndexType(prop_name))
										{
											throw new InvalidOperationException();
										}
#endif

										name = Extensions.GetPrimitiveValueToString(this, prop_name);
									}
									else
									{
										name = Extensions.GetPrimitiveValueToString(this,prop_name);
										//throw new NotImplementedException("转字符串？还是数组？");
									}
								}
								else
								{
									RtHeapInstance _n = Context.GC.Heap[prop_name.HeapPtr];
									if (_n.TypeKind != RtHeapTypeKind.STRING)
									{
										if (Context.StackPosition == Context.STACK_LENGTH)
										{
											RaiseStackOverflow(ref error);
											goto flag_handle_error;
										}

										var span = Context.StackSlots.AsSpan(Context.StackPosition , 1);span.Clear();
										StackLocater tmp = default;tmp.index = 0;
										Context.StackPosition++;
										NaNBoxing primitive_name = ToPrimitive(ref error, prop_name, HINT.h_string, scope_ptr, tmp, tmp,span  , stackStPos, thisPtr);
										if (error.raised)
										{
											Context.StackPosition--;
											goto flag_handle_error;
										}

										name = Extensions.GetPrimitiveValueToString(this, primitive_name);
										Context.StackPosition--;


										//throw new NotImplementedException("转字符串？");
									}
									else
									{
										name = ((RtPayloadString)_n.facility).Str;
									}

								}

							lbl_name_solved:

								var scope = methodscope; //Context.GC.Heap[scope_ptr];
#if DEBUG
								if (scope.TypeKind != RtHeapTypeKind.MethodScope)
								{
									throw new InvalidOperationException();
								}
#endif

								var ns_set = scope.Type._link_codescope.NamespaceSet;

								int code = MultiNameLSearch(ns_set, kind, as_type, name, stack, stackslots, stackStPos, instance_box, thisPtr, ref error);

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

								break;

							}
						case INS_Code.ld_RTQNameL_Ref:
							{
								StackLocater stack;
								stack.index = dst_index;

								StackLocater src;
								LoadStackLocater(&src, &PC);

								StackLocater _ns;
								LoadStackLocater(&_ns, &PC);

								StackLocater _name;
								LoadStackLocater(&_name, &PC);

								NaNBoxing instance_box;
								RtHeapTypeKind kind;
								ASContainer as_type;

								ReadInstanceFromStacklocater(ref error, src, stackslots, stackStPos, scope_ptr, out kind, out instance_box, out as_type);
								if (error.raised)
								{
									goto flag_handle_error;
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
								string searchName = null;
								if (ns.ValueType != NaNBoxing.BoxType.HeapPtr)
								{
									goto lbl_rtqname_ns_not_a_namespace;
								}
								else
								{
									RtHeapInstance ns_instance = Context.GC.Heap[ns.HeapPtr];
									if (ns_instance.TypeKind == RtHeapTypeKind.NAMESPACE)
									{
										searchNs = ((RtPayloadNameSpace)ns_instance.facility).ASNamespace;

									}
									else
									{
										goto lbl_rtqname_ns_not_a_namespace;
									}
								}

								if (name.ValueType != NaNBoxing.BoxType.HeapPtr)
								{
									//throw new NotImplementedException("cast to string");
									searchName = Extensions.GetPrimitiveValueToString(this, name);
								}
								else
								{
									RtHeapInstance name_instance = Context.GC.Heap[name.HeapPtr];
									if (name_instance.TypeKind == RtHeapTypeKind.STRING)
									{
										searchName = ((RtPayloadString)name_instance.facility).Str;
									}
									else if (name_instance.TypeKind == RtHeapTypeKind.NAMESPACE)
									{
										var n = ((RtPayloadNameSpace)name_instance.facility).ASNamespace;
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
										ConvertValueType(ref error, name, TypeKind.String, Context.STRING, ref conv, scope_ptr, thisPtr);
										if (error.raised)
										{
											Context.StackPosition--;
											goto flag_handle_error;
										}

										searchName = Extensions.GetPrimitiveValueToString(this,conv);

										//throw new NotImplementedException("cast to string");
									}
								}

								RtHeapInstance instance = null;
								var c_scope = methodscope; //Context.GC.Heap[scope_ptr];
#if DEBUG
								if (c_scope.TypeKind != RtHeapTypeKind.MethodScope)
								{
									throw new InvalidOperationException();
								}
#endif

								var ns_set = c_scope.Type._link_codescope.NamespaceSet;

								bool deepsearch = false;//如果是从instance的methodscope开始查找说明要继续查找静态成员-基类静态成员
								int instancePtr = 0;
								int o_instancePtr = 0;
								RtHeapInstance o_instance = null;

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

								instancePtr = instance_box.HeapPtr;
								//RTQName查找 -- 由于自定义命名空间只会在class级别定义，所以实际上只需要查找 静态成员 或者 类成员-继承的类成员-静态成员-基类静态成员找即可。
								while (instance.TypeKind == RtHeapTypeKind.MethodScope)
								{
									instancePtr = ((RtPayloadMethodScope)instance.facility).ParentPtr;
									instance = Context.GC.Heap[instancePtr];
									deepsearch = true;
								}
								o_instancePtr = instancePtr;




							lbl_primitive:

								bool issameorinherit = thisPtr.ValueType == NaNBoxing.BoxType.HeapPtr && instance != null &&

									Context.GC.Heap[thisPtr.HeapPtr].TypeKind == instance.TypeKind
									&&
									Context.GC.Heap[thisPtr.HeapPtr].TypeKind == RtHeapTypeKind.INSTANCE
									&&
									((ASInstance)instance.Type).IsExtend((ASInstance)Context.GC.Heap[thisPtr.HeapPtr].Type)
								;

								//lambda search member
								var searchmember = (CodeScope scope, ASNamespace ns, string name, out int index) =>
								{
									for (int i = 0; i < scope.Members.Count; i++)
									{
										var member = scope.Members[i];
										if (member.QName.Name == name && !((ns.Kind == NamespaceKind.Protected || ns.Kind == NamespaceKind.StaticProtected) && !issameorinherit) &&
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


								var searchvtable = (VTable vtable, ASNamespace ns, string name, out int m_idx, out int g_idx, out int s_idx) =>
									{
										m_idx = -1; g_idx = -1; s_idx = -1;
										for (int i = 0; i < vtable.Items.Count; i++)
										{
											var v = vtable.Items[i];

											if (v.Trait.QName.Name == name && !((ns.Kind == NamespaceKind.Protected || ns.Kind == NamespaceKind.StaticProtected) && !issameorinherit) &&
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
										RtHeapInstance cache = Context.GC.Heap[cacheobjpointer];
#if DEBUG
										if (cache.TypeKind != RtHeapTypeKind.STACK_CACHE_OBJ)
										{
											throw new InvalidOperationException();
										}
#endif

										RtPayloadStackCache cachePayload = (RtPayloadStackCache)cache.facility;
										cachePayload.RefInstance = instance_box;
										cachePayload.trait[0] = member.trait; cachePayload.trait[1] = null;
										cachePayload.scopemember_index = (ushort)i;
										cachePayload.searchPropertyName.SetUndefined(); cachePayload.as_type = instance.Type;
										cachePayload.searchNameSpacePtr = 0; cachePayload.indexer_key.setFault();
										stackslots[stack.index].SetHeapPtr(cacheobjpointer);
										goto lbl_rtqname_success;
									}
									else if (m_idx > -1)
									{
										var vitem = primitive_codescope.Container._vtable.Items[m_idx];

										int ptrIndex = stackStPos + stack.index;
										int m_closurePtr = Context.M_ClosurePtr + ptrIndex;

										Context.GC.Heap[m_closurePtr].Type = vitem.Trait.Method.Body;
										RtPayloadClosure closure = (RtPayloadClosure)Context.GC.Heap[m_closurePtr].facility;
										closure.This = instance_box;
										closure.ScopePtr = 0;
										closure.ScopeType = vitem.DefineAt;
										closure._ref_as_type = as_type;
										closure.methodscopeslot_ref_state = 0; closure.HEAPINSTANCE_PTR = 0;
										stackslots[stack.index].SetHeapPtr(m_closurePtr);
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
										RtHeapInstance cache = Context.GC.Heap[cacheobjpointer];
#if DEBUG
										if (cache.TypeKind != RtHeapTypeKind.STACK_CACHE_OBJ)
										{
											throw new InvalidOperationException();
										}
#endif

										RtPayloadStackCache cachePayload = (RtPayloadStackCache)cache.facility;
										cachePayload.RefInstance = instance_box;
										cachePayload.trait[0] = null; cachePayload.trait[1] = null;
										cachePayload.scopemember_index = 0;
										cachePayload.searchPropertyName = searchPtr; cachePayload.searchNameSpacePtr = ns.HeapPtr; cachePayload.indexer_key.setFault(); cachePayload.as_type = primitive_codescope.TypeLayout.ASType.Instance;
										stackslots[stack.index].SetHeapPtr(cacheobjpointer);

										goto lbl_rtqname_dynamicprop;

									}

								}
								else if (instance.TypeKind == RtHeapTypeKind.INSTANCE
									|| instance.TypeKind == RtHeapTypeKind.VECTOR
									|| instance.TypeKind == RtHeapTypeKind.STRING
									|| instance.TypeKind == RtHeapTypeKind.ARRAY
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
										instancePtr = instance.Type._link_codescope.TypeLayout.ASType.__instance_index__;

										issameorinherit = false; //静态成员查找跳过 protected..
										member = searchmember(scope, searchNs, searchName, out i); //查找静态成员
										searchvtable(scope.Container._vtable, searchNs, searchName, out m_idx, out g_idx, out s_idx);


										while (member == null && m_idx < 0 && g_idx < 0 && s_idx < 0)
										{
											var superType = ((ASClass)scope.Container).Instance._super_class_; //查找基类的静态成员
											if (superType == null)
												break;

											scope = superType._link_codescope;
											instancePtr = ((ASClass)scope.Container).__instance_index__;
											member = searchmember(scope, searchNs, searchName, out i);
											searchvtable(scope.Container._vtable, searchNs, searchName, out m_idx, out g_idx, out s_idx);

										}
									}

									if (member != null)
									{
										int ptrIndex = stackStPos + stack.index;
										int cacheobjpointer = Context.CacheObjPtr + ptrIndex;  //Context.CacheObjectPointers[ptrIndex];
										RtHeapInstance cache = Context.GC.Heap[cacheobjpointer];
#if DEBUG
										if (cache.TypeKind != RtHeapTypeKind.STACK_CACHE_OBJ)
										{
											throw new InvalidOperationException();
										}
#endif

										RtPayloadStackCache cachePayload = (RtPayloadStackCache)cache.facility;
										cachePayload.RefInstance.SetHeapPtr(instancePtr);
										cachePayload.trait[0] = member.trait; cachePayload.trait[1] = null;
										cachePayload.scopemember_index = (ushort)i;
										cachePayload.searchPropertyName.SetUndefined(); cachePayload.as_type = instance.Type;
										cachePayload.searchNameSpacePtr = 0; cachePayload.indexer_key.setFault();

										stackslots[stack.index].SetHeapPtr(cacheobjpointer);


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
											RtPayloadClosure closure = (RtPayloadClosure)Context.GC.Heap[m_closurePtr].facility;
											closure.This.SetHeapPtr(instancePtr);
											closure.ScopePtr = instancePtr;
											closure.ScopeType = vitem.DefineAt;
											closure._ref_as_type = as_type;
											closure.methodscopeslot_ref_state = 0; closure.HEAPINSTANCE_PTR = 0;
											stackslots[stack.index].SetHeapPtr(m_closurePtr);

										}
										else
										{
											//throw new NotImplementedException();
											int ptrIndex = stackStPos + stack.index;
											int cacheobjpointer = Context.CacheObjPtr + ptrIndex;
											RtHeapInstance cache = Context.GC.Heap[cacheobjpointer];
#if DEBUG
											if (cache.TypeKind != RtHeapTypeKind.STACK_CACHE_OBJ)
											{
												throw new InvalidOperationException();
											}
#endif

											RtPayloadStackCache cachePayload = (RtPayloadStackCache)cache.facility;
											cachePayload.RefInstance.SetHeapPtr(instancePtr);
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

											stackslots[stack.index].SetHeapPtr(cacheobjpointer);

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
										RtHeapInstance cache = Context.GC.Heap[cacheobjpointer];
#if DEBUG
										if (cache.TypeKind != RtHeapTypeKind.STACK_CACHE_OBJ)
										{
											throw new InvalidOperationException();
										}
#endif

										RtPayloadStackCache cachePayload = (RtPayloadStackCache)cache.facility;
										cachePayload.RefInstance.SetHeapPtr(o_instancePtr);
										cachePayload.trait[0] = null; cachePayload.trait[1] = null;
										cachePayload.scopemember_index = 0;
										cachePayload.searchPropertyName = searchPtr; cachePayload.searchNameSpacePtr = ns.HeapPtr; cachePayload.indexer_key.setFault(); cachePayload.as_type = instance.Type;


										stackslots[stack.index].SetHeapPtr(cacheobjpointer);


										goto lbl_rtqname_dynamicprop;

									}
								}
								else if (instance.TypeKind == RtHeapTypeKind.CLASS)
								{
									CodeScope cls = ((RtPayloadScriptClass)instance.facility).Meta._link_codescope;
									int i;
									var member = searchmember(cls, searchNs, searchName, out i);

									int m_idx, g_idx, s_idx;
									searchvtable(cls.Container._vtable, searchNs, searchName, out m_idx, out g_idx, out s_idx);

									if (member != null)
									{
										int ptrIndex = stackStPos + stack.index;
										int cacheobjpointer = Context.CacheObjPtr + ptrIndex;  //Context.CacheObjectPointers[ptrIndex];
										RtHeapInstance cache = Context.GC.Heap[cacheobjpointer];
#if DEBUG
										if (cache.TypeKind != RtHeapTypeKind.STACK_CACHE_OBJ)
										{
											throw new InvalidOperationException();
										}
#endif

										RtPayloadStackCache cachePayload = (RtPayloadStackCache)cache.facility;
										cachePayload.RefInstance.SetHeapPtr(instancePtr);
										cachePayload.trait[0] = member.trait; cachePayload.trait[1] = null;
										cachePayload.scopemember_index = (ushort)i;
										cachePayload.searchPropertyName.SetUndefined(); cachePayload.searchNameSpacePtr = 0; cachePayload.indexer_key.setFault(); cachePayload.as_type = instance.Type;

										stackslots[stack.index].SetHeapPtr(cacheobjpointer);


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
											RtPayloadClosure closure = (RtPayloadClosure)Context.GC.Heap[m_closurePtr].facility;
											closure.This.SetNull();
											closure.ScopePtr = instancePtr;
											closure.ScopeType = vitem.DefineAt;
											closure._ref_as_type = as_type;
											closure.methodscopeslot_ref_state = 0; closure.HEAPINSTANCE_PTR = 0;
											stackslots[stack.index].SetHeapPtr(m_closurePtr);

										}
										else
										{
											//throw new NotImplementedException();
											int ptrIndex = stackStPos + stack.index;
											int cacheobjpointer = Context.CacheObjPtr + ptrIndex;
											RtHeapInstance cache = Context.GC.Heap[cacheobjpointer];
#if DEBUG
											if (cache.TypeKind != RtHeapTypeKind.STACK_CACHE_OBJ)
											{
												throw new InvalidOperationException();
											}
#endif

											RtPayloadStackCache cachePayload = (RtPayloadStackCache)cache.facility;
											cachePayload.RefInstance.SetHeapPtr(instancePtr);
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

											stackslots[stack.index].SetHeapPtr(cacheobjpointer);

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
										RtHeapInstance cache = Context.GC.Heap[cacheobjpointer];
#if DEBUG
										if (cache.TypeKind != RtHeapTypeKind.STACK_CACHE_OBJ)
										{
											throw new InvalidOperationException();
										}
#endif

										RtPayloadStackCache cachePayload = (RtPayloadStackCache)cache.facility;
										cachePayload.RefInstance.SetHeapPtr(o_instancePtr);
										cachePayload.trait[0] = null; cachePayload.trait[1] = null;
										cachePayload.scopemember_index = 0;
										cachePayload.searchPropertyName = searchPtr; cachePayload.searchNameSpacePtr = 0; cachePayload.indexer_key.setFault(); cachePayload.as_type = instance.Type;

										stackslots[stack.index].SetHeapPtr(cacheobjpointer);


										goto lbl_rtqname_dynamicprop;
									}

								}
								else if (instance.TypeKind == RtHeapTypeKind.GLOBAL)
								{
									goto lbl_rtqname_notfound;
								}
								else if (instance.TypeKind == RtHeapTypeKind.CLOSURE)
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
								break;
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
							}
						case INS_Code.ld_InstanceOrScopeMemberValueRef:
							{
								StackLocater target;
								target.index = dst_index;

								StackLocater src;
								LoadStackLocater(&src, &PC);

								//ushort trait_index;
								uint scopemember_index;

								//LoadUShort(&trait_index, &PC);
								LoadUInt(&scopemember_index, &PC);


								NaNBoxing instance_box;
								RtHeapTypeKind kind;
								ASContainer as_type;

								ReadInstanceFromStacklocater(ref error, src, stackslots, stackStPos, scope_ptr, out kind, out instance_box, out as_type);
								if (error.raised)
								{
									goto flag_handle_error;
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


								var instance = Context.GC.Heap[instance_box.HeapPtr];

								do
								{
									if (instance.TypeKind == RtHeapTypeKind.CLASS || instance.TypeKind == RtHeapTypeKind.GLOBAL)
									{


										RtPayloadScriptClass heap = (RtPayloadScriptClass)instance.facility;
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
										RtHeapInstance cache = Context.GC.Heap[cacheobjpointer];
#if DEBUG
										if (cache.TypeKind != RtHeapTypeKind.STACK_CACHE_OBJ)
										{
											throw new InvalidOperationException();
										}

#endif
										RtPayloadStackCache cachePayload = (RtPayloadStackCache)cache.facility;
										cachePayload.RefInstance = instance_box;
										cachePayload.trait[0] = trait; cachePayload.trait[1] = null;
										cachePayload.scopemember_index = (ushort)scopemember_index;
										cachePayload.searchPropertyName.SetUndefined(); cachePayload.searchNameSpacePtr = 0; cachePayload.indexer_key.setFault(); cachePayload.as_type = instance.Type;

										stackslots[target.index].SetHeapPtr(cacheobjpointer);

									}
									else if (instance.TypeKind == RtHeapTypeKind.INSTANCE)
									{


										RtPayloadInstance heap = (RtPayloadInstance)instance.facility;
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
										RtHeapInstance cache = Context.GC.Heap[cacheobjpointer];
#if DEBUG
										if (cache.TypeKind != RtHeapTypeKind.STACK_CACHE_OBJ)
										{
											throw new InvalidOperationException();
										}

#endif

										RtPayloadStackCache cachePayload = (RtPayloadStackCache)cache.facility;
										cachePayload.RefInstance = instance_box;
										cachePayload.trait[0] = trait; cachePayload.trait[1] = null;
										cachePayload.scopemember_index = (ushort)scopemember_index;
										cachePayload.searchPropertyName.SetUndefined(); cachePayload.searchNameSpacePtr = 0; cachePayload.indexer_key.setFault(); cachePayload.as_type = instance.Type;

										stackslots[target.index].SetHeapPtr(cacheobjpointer);

									}
#if DEBUG
									else if (instance.TypeKind == RtHeapTypeKind.STACK_CACHE_OBJ)
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
							}
							break;
						case INS_Code.ld_ScopeH:
							{
								StackLocater stackLocater;
								stackLocater.index = dst_index;

								ScopeHeapLocater heapLocater;
								{
									heapLocater.ScopeIndex = *(ushort*)PC; PC += 2;
									heapLocater.MemberIndex = *(ushort*)PC; PC += 2;
									//byte* _p = (byte*)&heapLocater.ScopeIndex;
									//*_p++ = *PC++;
									//*_p = *PC++;

									//_p = (byte*)&heapLocater.MemberIndex;
									//*_p++ = *PC++;
									//*_p = *PC++;
								}
								
								NaNBoxing v = Ld_ScopeH(methodscope,heapLocater,scopeType , stackStPos + stackLocater.index );
								stackslots[stackLocater.index] = v;
							}
							break;
						case INS_Code.ld_methodVariable:
							{
								//StackLocater stackLocater;
								//stackLocater.index = dst_index;

								//ScopeHeapLocater heapLocater = (*ScopeHeapLocater*)PC; PC += 4;
								ScopeHeapLocater* heapLocater = (ScopeHeapLocater*)PC;PC+=4;

#if DEBUG
								if (methodscope.Type._link_codescope.index != heapLocater->ScopeIndex)
									throw new InvalidOperationException();
#endif

								stackslots[dst_index] = ((RtPayloadMethodScope)methodscope.facility).ReadSlot(heapLocater->MemberIndex, this);


							}
							break;
//						case INS_Code.short_ld_methodVariable:
//							{
//								//[stack:{(uint)dst.index & 0xff}] <- [ scope:{(uint)dst.index>>16 & 0xff},member{ (uint)dst.index >> 8 & 0xff }]";
//#if DEBUG
//								if (methodscope.Type._link_codescope.index != (ushort)((uint)dst_index >> 16 & 0xff))
//									throw new InvalidOperationException();
//#endif
//								stackslots[(int)((uint)dst_index & 0xff)] = ((RtPayloadMethodScope)methodscope.facility).ReadSlot((ushort)((uint)dst_index >> 8 & 0xff), this);
//							}
//							break;
						case INS_Code.ld_ValueRef:
							{
								StackLocater sourc;
								StackLocater target;
								LoadStackLocater(&sourc, &PC);
								//LoadStackLocater(&target, &PC);
								target.index = dst_index;

								var v = LoadValue(stackslots[sourc.index],
									 stackStPos - method.Body._link_codescope.Members.Count - 1, ref error,  stackslots, stackStPos + target.index);
								if (error.raised)
								{
									goto flag_handle_error;
								}

								stackslots[target.index] = v;
							}
							break;
						case INS_Code.move:
							{
								StackLocater sourc;
								StackLocater target;
								LoadStackLocater(&sourc, &PC);
								//LoadStackLocater(&target, &PC);
								target.index = dst_index;

								stackslots[target.index] = stackslots[sourc.index];

							}
							break;
						case INS_Code.ld_This:
							{
#if FORCOMPILER
								if (IsComputeConstExpr)
								{
									if (thisPtr.ValueType == BoxType.Fault)
									{
										throw new EvalConstException();
									}
								}

#endif


								StackLocater target;
								target.index = dst_index;

								stackslots[target.index] = thisPtr;
								
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
								StackLocater target;
								target.index = dst_index;

								int a_ptr = stackStPos - method.Body._link_codescope.Members.Count
									- 2; /*
									      * arguments
									      * callee
									      */

								NaNBoxing arguments = Context.StackSlots[a_ptr];
#if DEBUG
								if (arguments.ValueType != NaNBoxing.BoxType.HeapPtr)
									throw new InvalidOperationException();
								if (Context.GC.Heap[arguments.HeapPtr].TypeKind != RtHeapTypeKind.ARRAY)
									throw new InvalidOperationException();
								if (((RtPayloadArray)Context.GC.Heap[arguments.HeapPtr].facility).StoreMode != RtPayloadArray.ArrayStoreMode.cache_on_stack)
									throw new InvalidOperationException();

#endif
								stackslots[target.index] = arguments;

							}
							break;
						case INS_Code.ld_function:
							{
								StackLocater target;
								target.index = dst_index;

								ScopeHeapLocater heapLocater;
								{
									heapLocater.ScopeIndex = *(ushort*)PC; PC += 2;
									heapLocater.MemberIndex = *(ushort*)PC; PC += 2;
									//byte* _p = (byte*)&heapLocater.ScopeIndex;
									//*_p++ = *PC++;
									//*_p = *PC++;

									//_p = (byte*)&heapLocater.MemberIndex;
									//*_p++ = *PC++;
									//*_p = *PC++;
								}

								int function_id = 0;
								LoadInt32(&function_id, &PC);

								NaNBoxing fbox = constants[function_id];
#if DEBUG
								if (fbox.ValueType != NaNBoxing.BoxType.Uint)
									throw new InvalidOperationException();
#endif
								RtHeapInstance closure;
								Ld_function_and_store_member(heapLocater, methodscope ,scope_ptr, fbox.UIntValue, ref error, stackStPos, target, stackslots, method_scopes, out closure);
								if (error.raised)
								{
									goto flag_handle_error;
								}
							}
							break;
						case INS_Code.ld_function_call: //此指令目标肯定是匿名函数，所以不需要考虑proto和动态属性问题。
							{
								StackLocater target;
								target.index = dst_index;

								int function_id = 0;
								LoadInt32(&function_id, &PC);

								int argsCount;
								LoadInt32(&argsCount, &PC);

								//!!需要考虑对齐问题
								byte* argementsPtr = PC;
								PC += argsCount * 4;


								NaNBoxing fbox = constants[function_id];
#if DEBUG
								if (fbox.ValueType != NaNBoxing.BoxType.Uint)
									throw new InvalidOperationException();
#endif

								ASMethod function = Context.link_const_methods[(int)fbox.UIntValue]; //((ASMethodBody)obj.Type).Method;

								////加载global。或者instance。
								//var s = function.Body._link_codescope.Parent;
								//while (s.Kind != CodeScopeKind.Script && s.Kind != CodeScopeKind.Instance)
								//{
								//    s = s.Parent;
								//}
								//var globalptr =((ASScript)s.Container).__global_index__;

								//NaNBoxing _this_ = new NaNBoxing();
								//_this_.SetHeapPtr(globalptr);

								var o = methodscope; //Context.GC.Heap[scope_ptr];
								int instancePtr = scope_ptr;
								do
								{
									if (o.TypeKind == RtHeapTypeKind.MethodScope)
									{
										RtPayloadMethodScope rtPayload = (RtPayloadMethodScope)o.facility;
										o = Context.GC.Heap[rtPayload.ParentPtr];
										instancePtr = rtPayload.ParentPtr;
									}
									else
									{
										break;
									}

								} while (true);

								NaNBoxing _this_ = new NaNBoxing();
								_this_.SetHeapPtr(instancePtr);


								NaNBoxing result = RunMethod(function, _this_, scope_ptr, scopeType, (ushort)argsCount, argementsPtr, stackslots, ref error, stackStPos + target.index);
								if (error.raised)
								{
									goto flag_handle_error;
								}

								stackslots[target.index] = result;

							}
							break;

						case INS_Code.ld_function_bindglobal_call:
							{
								StackLocater target;
								target.index = dst_index;

								int function_id; LoadInt32(&function_id, &PC);
								ScopeHeapLocater heapLocater = *(ScopeHeapLocater*)PC; PC += 4;
								//{
								//	heapLocater.ScopeIndex = *(ushort*)PC; PC += 2;
								//	heapLocater.MemberIndex = *(ushort*)PC; PC += 2;
								//}
								int argsCount;
								LoadInt32(&argsCount, &PC);

								//!!需要考虑对齐问题
								byte* argementsPtr = PC;
								PC += argsCount * 4;



								NaNBoxing fbox = constants[function_id];
#if DEBUG
								if (fbox.ValueType != NaNBoxing.BoxType.Uint)
									throw new InvalidOperationException();
#endif
								RtHeapInstance closure;
								int closure_ptr = Ld_function_and_store_member(heapLocater, methodscope, scope_ptr, fbox.UIntValue, ref error, stackStPos, target, stackslots, method_scopes, out closure);
								if (error.raised)
								{
									goto flag_handle_error;
								}

								NaNBoxing _this_ = default;
								if (global_obj.ValueType != BoxType.HeapPtr)
								{
									//加载global。
									var s = methodscope.Type._link_codescope.Parent; //Context.GC.Heap[scope_ptr].Type._link_codescope.Parent;
									while (s.Kind != CodeScopeKind.Script)
									{
										s = s.Parent;
									}

									var globalptr = ((ASScript)s.Container).__global_index__;
									global_obj.SetHeapPtr(globalptr);
									_this_.SetHeapPtr(globalptr);
								}
								else
								{
									_this_ = global_obj;
								}

								var _scopeType = Context.GC.Heap[((RtPayloadClosure)closure.facility).ScopePtr].Type;

								NaNBoxing ret = RunMethod(((ASMethodBody)closure.Type).Method, _this_,
									((RtPayloadClosure)closure.facility).ScopePtr, _scopeType,
									(ushort)argsCount, argementsPtr, stackslots, ref error, stackStPos + target.index, closure_ptr);
								if (error.raised)
								{
									goto flag_handle_error;
								}

								stackslots[target.index] = ret;


							}
							break;
						case INS_Code.bindglobal_call:
							{
								StackLocater result;
								result.index = dst_index;

								StackLocater function;
								LoadStackLocater(&function, &PC);

								int argsCount;
								LoadInt32(&argsCount, &PC);

								//!!需要考虑对齐问题
								byte* argementsPtr = PC;
								PC += argsCount * 4;

								NaNBoxing funValue = stackslots[function.index];

								RtHeapInstance funinstance = null;
								if (funValue.ValueType == BoxType.HeapPtr)           
								{ 
									funinstance = Context.GC.Heap[funValue.HeapPtr];
									if (funinstance.TypeKind == RtHeapTypeKind.CLASS)
									{
										var @class = ((ASClass)((RtPayloadScriptClass)funinstance.facility).Meta);

										if (@class.Instance.Flags.HasFlag(ClassFlags.NoConstructor))
										{
											RaiseTypeError(ref error, funValue, TypeKind.Function);
											goto flag_handle_error;
										}
										else if (@class.Type_identifier == (ulong)TypeKind.String && argsCount == 0)
										{
											RaiseTypeError(ref error, funValue, TypeKind.Function);
											goto flag_handle_error;
										}
										else
										{
											//if (argsCount == 1)
											{
												//stackslots[result.index] = ret;

												//throw new NotImplementedException("强制类型转换");
												//break;
												ExplicitConvert(ref error,(ushort)argsCount,(StackLocater*)argementsPtr ,stackslots,
													(TypeKind)@class.Type_identifier, @class, ref stackslots[result.index],stackStPos + result.index, scope_ptr, thisPtr,false
													);
												if (error.raised)
												{
													goto flag_handle_error;
												}
												break;

											}
											//else
											//{
											//	RaiseArgementErrorCountMisMatch(ref error, null, 1, argsCount);
											//	goto flag_handle_error;
											//}
										}
									}
								}

								ConvertValueType(ref error, funValue, TypeKind.Function, null, ref funValue); //转换到function,不可能触发 valueOf()调用.
								if (error.raised)
								{
									goto flag_handle_error;
								}
								if (funValue.ValueType == NaNBoxing.BoxType.Null)
								{
									RaiseTypeError(ref error,funValue, TypeKind.Function);
									goto flag_handle_error;
								}
								
								//funinstance肯定不为空，如果为空前面就失败了。
								var func = ((ASMethodBody)funinstance.Type).Method;
								RtPayloadClosure closure = (RtPayloadClosure)funinstance.facility;


								NaNBoxing _this_ = new NaNBoxing();
								ASContainer _scopeType;
								if (func.__ismethod)
								{
									_this_ = closure.This;
									_scopeType = closure.ScopeType;
								}
								else
								{
									////加载global。
									//var s = methodscope.Type._link_codescope.Parent; //Context.GC.Heap[scope_ptr].Type._link_codescope.Parent;
									//while (s.Kind != CodeScopeKind.Script)
									//{
									//	s = s.Parent;
									//}

									//var globalptr = ((ASScript)s.Container).__global_index__;

									//_this_.SetHeapPtr(globalptr);
									if (global_obj.ValueType != BoxType.HeapPtr)
									{
										//加载global。
										var s = methodscope.Type._link_codescope.Parent; //Context.GC.Heap[scope_ptr].Type._link_codescope.Parent;
										while (s.Kind != CodeScopeKind.Script)
										{
											s = s.Parent;
										}

										var globalptr = ((ASScript)s.Container).__global_index__;
										global_obj.SetHeapPtr(globalptr);
										_this_.SetHeapPtr(globalptr);
									}
									else
									{
										_this_ = global_obj;
									}


									_scopeType = Context.GC.Heap[closure.ScopePtr].Type;
								}


								NaNBoxing ret = RunMethod(func, _this_, closure.ScopePtr, _scopeType, (ushort)argsCount, argementsPtr, stackslots, ref error, stackStPos + result.index, funValue.HeapPtr);
								if (error.raised)
								{
									goto flag_handle_error;
								}

								stackslots[result.index] = ret;

							}
							break;
						case INS_Code.bindthis_call:
							{
								StackLocater result;
								result.index = dst_index;

								StackLocater function;
								LoadStackLocater(&function, &PC);

								StackLocater _this_;
								LoadStackLocater(&_this_, &PC);

								int argsCount;
								LoadInt32(&argsCount, &PC);

								//!!需要考虑对齐问题
								byte* argementsPtr = PC;
								PC += argsCount * 4;

								NaNBoxing thisValue;
								if (_this_.index >= 0)
								{
									thisValue = stackslots[_this_.index];
#if DEBUG
									if (thisValue.ValueType != NaNBoxing.BoxType.HeapPtr)
									{
									}
									else
									{
										RtHeapInstance ins = Context.GC.Heap[thisValue.HeapPtr];
										if (ins.TypeKind == RtHeapTypeKind.STACK_CACHE_OBJ
											//||
											//ins.TypeKind == RtHeapTypeKind.CACHE_LD_CLASS
											||
											ins.TypeKind == RtHeapTypeKind.SHAPE
											)
										{
											throw new InvalidOperationException();
										}

									}

#endif

								}
								else
								{
									//加载global
									var s = methodscope.Type._link_codescope.Parent; //Context.GC.Heap[scope_ptr].Type._link_codescope.Parent;
									while (s.Kind != CodeScopeKind.Script)
									{
										s = s.Parent;
									}

									var globalptr = ((ASScript)s.Container).__global_index__;

									thisValue = new NaNBoxing();
									thisValue.SetHeapPtr(globalptr);
								}


								NaNBoxing funValue = stackslots[function.index];

								RtHeapInstance funinstance = null;
								if (funValue.ValueType == BoxType.HeapPtr)
								{
									funinstance = Context.GC.Heap[funValue.HeapPtr];
									if (funinstance.TypeKind == RtHeapTypeKind.CLASS)
									{
										var @class = ((ASClass)((RtPayloadScriptClass)funinstance.facility).Meta);

										if (@class.Instance.Flags.HasFlag(ClassFlags.NoConstructor))
										{
											RaiseTypeError(ref error, funValue, TypeKind.Function);
											goto flag_handle_error;
										}
										else if (@class.Type_identifier == (ulong)TypeKind.String && argsCount == 0)
										{
											RaiseTypeError(ref error, funValue, TypeKind.Function);
											goto flag_handle_error;
										}
										else
										{
											//if (argsCount == 1)
											//{
												//stackslots[result.index] = ret;
												//throw new NotImplementedException("强制类型转换");
												//break;
												ExplicitConvert(ref error,(ushort)argsCount , (StackLocater*)argementsPtr ,stackslots,
													(TypeKind)@class.Type_identifier, @class, ref stackslots[result.index], stackStPos + result.index ,scope_ptr, thisPtr,false
													);
												if (error.raised)
												{
													goto flag_handle_error;
												}
												break;

											//}
											//else
											//{
											//	RaiseArgementErrorCountMisMatch(ref error, null, 1, argsCount);
											//	goto flag_handle_error;
											//}
										}


									}
								}

								ConvertValueType(ref error, funValue, TypeKind.Function, null, ref funValue); //转换到Function,不可能触发valueOf()调用
								if (error.raised)
								{
									goto flag_handle_error;
								}
								if (funValue.ValueType == NaNBoxing.BoxType.Null)
								{
									RaiseTypeError(ref error,funValue, TypeKind.Function);
									goto flag_handle_error;
								}


#if DEBUG
								if (funValue.ValueType != NaNBoxing.BoxType.HeapPtr)
									throw new InvalidOperationException();

								if (Context.GC.Heap[funValue.HeapPtr].TypeKind != RtHeapTypeKind.CLOSURE)
									throw new InvalidOperationException();

#endif
								//funinstance = Context.GC.Heap[funValue.HeapPtr];

								//执行到这里 funinstance肯定不为空
								var func = ((ASMethodBody)funinstance.Type).Method;
								RtPayloadClosure closure = (RtPayloadClosure)funinstance.facility;



								NaNBoxing ret = RunMethod(
									func,
									((func.__ismethod && !func.__is_call_or_apply) ? closure.This : thisValue),
									closure.ScopePtr,

									(func.__ismethod && !func.__is_call_or_apply) ? closure.ScopeType : Context.GC.Heap[closure.ScopePtr].Type,

									(ushort)argsCount, argementsPtr, stackslots, ref error,
									stackStPos + result.index,
									funValue.HeapPtr
									);

								if (error.raised)
								{
									goto flag_handle_error;
								}

								stackslots[result.index] = ret;

							}
							break;

						case INS_Code.ld_supermethod:
							{
								StackLocater target;
								target.index = dst_index;

								StackLocater instance;
								LoadStackLocater(&instance, &PC);

								int method_id = 0;
								LoadInt32(&method_id, &PC);


								NaNBoxing thisValue = stackslots[instance.index];

#if DEBUG
								if (thisValue.ValueType != NaNBoxing.BoxType.HeapPtr)
								{
									throw new InvalidOperationException();
								}
								else
								{
									RtHeapInstance ins = Context.GC.Heap[thisValue.HeapPtr];
									if (ins.TypeKind != RtHeapTypeKind.INSTANCE)
									{
										throw new InvalidOperationException();
									}

								}

#endif



								NaNBoxing fbox = constants[method_id];
#if DEBUG
								if (fbox.ValueType != NaNBoxing.BoxType.Uint)
									throw new InvalidOperationException();
#endif

								var vtableitem = Context.link_const_vtableitems[(int)fbox.UIntValue];
								var function = vtableitem.Trait.Method;
								var define = (ASInstance)vtableitem.DefineAt;

								int ptrIndex = stackStPos + target.index;
								int closurePtr = Context.M_ClosurePtr + ptrIndex;

								var closure = Context.GC.Heap[closurePtr];
								closure.Type = function.Body;
								((RtPayloadClosure)closure.facility).ScopePtr = thisValue.HeapPtr;
								((RtPayloadClosure)closure.facility).ScopeType = define;
								((RtPayloadClosure)closure.facility).This = thisValue;
								((RtPayloadClosure)closure.facility)._ref_as_type = define;
								((RtPayloadClosure)closure.facility).methodscopeslot_ref_state = 0;
								((RtPayloadClosure)closure.facility).HEAPINSTANCE_PTR = 0;
								stackslots[target.index].SetHeapPtr(closurePtr);

							}
							break;
						case INS_Code.ld_method:
							{
								StackLocater target;
								target.index = dst_index;

								StackLocater instance;
								LoadStackLocater(&instance, &PC);

								uint vtable_ = 0;
								LoadUInt(&vtable_, &PC);
								ushort vtable_index = (ushort)vtable_;


								NaNBoxing thisValue;
								if (instance.index >= 0)
								{
									thisValue = stackslots[instance.index];
								}
								else
								{
									var o = methodscope; //Context.GC.Heap[scope_ptr];
									int instancePtr = scope_ptr;
									do
									{
										if (o.TypeKind == RtHeapTypeKind.MethodScope)
										{
											RtPayloadMethodScope rtPayload = (RtPayloadMethodScope)o.facility;
											o = Context.GC.Heap[rtPayload.ParentPtr];
											instancePtr = rtPayload.ParentPtr;
										}
										else
										{
											break;
										}

									} while (true);
									thisValue = new NaNBoxing();
									thisValue.SetHeapPtr(instancePtr);
								}


								if (thisValue.ValueType == NaNBoxing.BoxType.Null)
								{
									RaiseTypeError_AccessNull(ref error);
									goto flag_handle_error;
								}

								if (thisValue.ValueType != NaNBoxing.BoxType.HeapPtr)
								{
									ASInstance @instacne = null;
									switch (thisValue.ValueType)
									{
										case BoxType.Number:
											instacne = Context.NUMBER.Instance;
											break;
										case BoxType.Boolean:
											instacne = Context.BOOLEAN.Instance;
											break;
										case BoxType.Int:
											instacne = Context.INT.Instance;
											break;
										case BoxType.Uint:
											instacne = Context.UINT.Instance;
											break;
										case BoxType.Sbyte:
											instacne = Context.SBYTE.Instance;
											break;
										case BoxType.Byte:
											instacne = Context.BYTE.Instance;
											break;
										case BoxType.Short:
											instacne = Context.SHORT.Instance;
											break;
										case BoxType.UShort:
											instacne = Context.USHORT.Instance;
											break;
										case BoxType.Float:
											instacne = Context.FLOAT.Instance;
											break;
									}

									Debug.Assert(instacne != null);

									var vtableitem = instacne._vtable.Items[vtable_index];
									var function = vtableitem.Trait.Method;

									var define = (ASInstance)vtableitem.DefineAt;

									int ptrIndex = stackStPos + target.index;
									int closurePtr = Context.M_ClosurePtr + ptrIndex;

									var closure = Context.GC.Heap[closurePtr];
									closure.Type = function.Body;
									((RtPayloadClosure)closure.facility).ScopePtr = 0;////thisValue.HeapPtr;
									((RtPayloadClosure)closure.facility).ScopeType = define;
									((RtPayloadClosure)closure.facility).This = thisValue;
									((RtPayloadClosure)closure.facility)._ref_as_type = define;
									((RtPayloadClosure)closure.facility).methodscopeslot_ref_state = 0;
									((RtPayloadClosure)closure.facility).HEAPINSTANCE_PTR = 0;
									stackslots[target.index].SetHeapPtr(closurePtr);

								}
								else
								{
									RtHeapInstance ins = Context.GC.Heap[thisValue.HeapPtr];

									if (ins.TypeKind == RtHeapTypeKind.INSTANCE || ins.TypeKind == RtHeapTypeKind.VECTOR)
									{
										var vtableitem = ins.Type._vtable.Items[vtable_index];
										var function = vtableitem.Trait.Method;

										var define = (ASInstance)vtableitem.DefineAt;

										int ptrIndex = stackStPos + target.index;
										int closurePtr = Context.M_ClosurePtr + ptrIndex;

										var closure = Context.GC.Heap[closurePtr];
										closure.Type = function.Body;
										((RtPayloadClosure)closure.facility).ScopePtr = thisValue.HeapPtr;
										((RtPayloadClosure)closure.facility).ScopeType = define;
										((RtPayloadClosure)closure.facility).This = thisValue;
										((RtPayloadClosure)closure.facility)._ref_as_type = define;
										((RtPayloadClosure)closure.facility).methodscopeslot_ref_state = 0;
										((RtPayloadClosure)closure.facility).HEAPINSTANCE_PTR = 0;
										stackslots[target.index].SetHeapPtr(closurePtr);
									}
									else if (ins.TypeKind == RtHeapTypeKind.STRING)
									{
										var vtableitem = Context.STRING.Instance._vtable.Items[vtable_index];
										var function = vtableitem.Trait.Method;
										var define = (ASInstance)vtableitem.DefineAt;

										int ptrIndex = stackStPos + target.index;
										int closurePtr = Context.M_ClosurePtr + ptrIndex;

										var closure = Context.GC.Heap[closurePtr];
										closure.Type = function.Body;
										((RtPayloadClosure)closure.facility).ScopePtr = thisValue.HeapPtr;
										((RtPayloadClosure)closure.facility).ScopeType = define;
										((RtPayloadClosure)closure.facility).This = thisValue;
										((RtPayloadClosure)closure.facility)._ref_as_type = define;
										((RtPayloadClosure)closure.facility).methodscopeslot_ref_state = 0;
										((RtPayloadClosure)closure.facility).HEAPINSTANCE_PTR = 0;
										stackslots[target.index].SetHeapPtr(closurePtr);
									}
									else if (ins.TypeKind == RtHeapTypeKind.CLASS)
									{
										var @class = ((RtPayloadScriptClass)ins.facility).Meta;
										var function = @class._vtable.Items[vtable_index].Trait.Method;

										int ptrIndex = stackStPos + target.index;
										int closurePtr = Context.M_ClosurePtr + ptrIndex;

										var closure = Context.GC.Heap[closurePtr];
										closure.Type = function.Body;
										((RtPayloadClosure)closure.facility).ScopePtr = thisValue.HeapPtr;
										((RtPayloadClosure)closure.facility).ScopeType = @class;
										((RtPayloadClosure)closure.facility)._ref_as_type = @class;
										((RtPayloadClosure)closure.facility).This = thisValue;
										((RtPayloadClosure)closure.facility).methodscopeslot_ref_state = 0;
										((RtPayloadClosure)closure.facility).HEAPINSTANCE_PTR = 0;

										stackslots[target.index].SetHeapPtr(closurePtr);
									}
									else if (ins.TypeKind == RtHeapTypeKind.CLOSURE)
									{
										var function = Context.FUNCTION.Instance._vtable.Items[vtable_index].Trait.Method;

										int ptrIndex = stackStPos + target.index;
										int closurePtr = Context.M_ClosurePtr + ptrIndex;

										var closure = Context.GC.Heap[closurePtr];
										closure.Type = function.Body;
										((RtPayloadClosure)closure.facility).ScopePtr = thisValue.HeapPtr;
										((RtPayloadClosure)closure.facility).ScopeType = Context.FUNCTION.Instance;
										((RtPayloadClosure)closure.facility).This = thisValue;
										((RtPayloadClosure)closure.facility)._ref_as_type = Context.FUNCTION.Instance;
										((RtPayloadClosure)closure.facility).methodscopeslot_ref_state = 0;
										((RtPayloadClosure)closure.facility).HEAPINSTANCE_PTR = 0;
										stackslots[target.index].SetHeapPtr(closurePtr);
									}
									else if (ins.TypeKind == RtHeapTypeKind.ARRAY)
									{
										var function = Context.ARRAY.Instance._vtable.Items[vtable_index].Trait.Method;

										int ptrIndex = stackStPos + target.index;
										int closurePtr = Context.M_ClosurePtr + ptrIndex;

										var closure = Context.GC.Heap[closurePtr];
										closure.Type = function.Body;
										((RtPayloadClosure)closure.facility).ScopePtr = thisValue.HeapPtr;
										((RtPayloadClosure)closure.facility).ScopeType = Context.ARRAY.Instance;
										((RtPayloadClosure)closure.facility).This = thisValue;
										((RtPayloadClosure)closure.facility)._ref_as_type = Context.ARRAY.Instance;
										((RtPayloadClosure)closure.facility).methodscopeslot_ref_state = 0;
										((RtPayloadClosure)closure.facility).HEAPINSTANCE_PTR = 0;
										stackslots[target.index].SetHeapPtr(closurePtr);
									}
#if DEBUG
									else
									{
										throw new InvalidOperationException();
									}
#endif
								}


							}
							break;

						case INS_Code.ld_interface_method:
							{
								StackLocater target;
								target.index = dst_index;

								StackLocater instance;
								LoadStackLocater(&instance, &PC);

								int class_id;
								LoadInt32(&class_id, &PC);

								uint vtable_ = 0;
								LoadUInt(&vtable_, &PC);
								ushort vtable_index = (ushort)vtable_;

								NaNBoxing thisValue;
#if DEBUG
								if (instance.index >= 0)
#endif
								{
									thisValue = stackslots[instance.index];
								}
#if DEBUG
								else
								{
									throw new InvalidOperationException();
								}
#endif
								if (thisValue.ValueType == NaNBoxing.BoxType.Null)
								{
									RaiseTypeError_AccessNull(ref error);
									goto flag_handle_error;
								}

#if DEBUG
								if (thisValue.ValueType != NaNBoxing.BoxType.HeapPtr)
								{
									throw new InvalidOperationException(); //接口，肯定不是基本类型
								}
								else
#endif
								{
									RtHeapInstance ins = Context.GC.Heap[thisValue.HeapPtr];

									if (ins.TypeKind == RtHeapTypeKind.INSTANCE)
									{
										var boxing = constants[class_id];
#if DEBUG
										if (boxing.ValueType != NaNBoxing.BoxType.Uint)
										{
											throw new InvalidOperationException();
										}
#endif
										var @class = Context.link_const_class[(int)boxing.UIntValue];
#if DEBUG
										if (!@class.Instance.IsInterface)
										{
											throw new InvalidOperationException();
										}
#endif

										int m_idx = ((ASInstance)ins.Type)._interface_impl_.First((i) => i.interface_type == @class.Type_identifier)[vtable_index];

										var vtableitem = ins.Type._vtable.Items[m_idx];
										var function = vtableitem.Trait.Method;

										var define = (ASInstance)vtableitem.DefineAt;

										int ptrIndex = stackStPos + target.index;
										int closurePtr = Context.M_ClosurePtr + ptrIndex;

										var closure = Context.GC.Heap[closurePtr];
										closure.Type = function.Body;
										((RtPayloadClosure)closure.facility).ScopePtr = thisValue.HeapPtr;
										((RtPayloadClosure)closure.facility).ScopeType = define;
										((RtPayloadClosure)closure.facility).This = thisValue;
										((RtPayloadClosure)closure.facility)._ref_as_type = define;
										((RtPayloadClosure)closure.facility).methodscopeslot_ref_state = 0;
										((RtPayloadClosure)closure.facility).HEAPINSTANCE_PTR = 0;
										stackslots[target.index].SetHeapPtr(closurePtr);
									}
#if DEBUG
									else
									{
										throw new InvalidOperationException();
									}
#endif
								}


							}
							break;
						case INS_Code.method_call:
							{
								StackLocater target;
								target.index = dst_index;

								StackLocater function;
								LoadStackLocater(&function, &PC);

								int argsCount;
								LoadInt32(&argsCount, &PC);

								//!!需要考虑对齐问题
								byte* argementsPtr = PC;
								PC += argsCount * 4;

#if DEBUG
								if (stackslots[function.index].ValueType != NaNBoxing.BoxType.HeapPtr)
								{
									throw new InvalidOperationException();
								}
#endif
								RtHeapInstance _method_ = Context.GC.Heap[stackslots[function.index].HeapPtr];
								RtPayloadClosure _methodclosure_ = (RtPayloadClosure)_method_.facility;
								NaNBoxing result = RunMethod(((ASMethodBody)_method_.Type).Method,
									_methodclosure_.This, _methodclosure_.ScopePtr, _methodclosure_.ScopeType, (ushort)argsCount, argementsPtr, stackslots, ref error, stackStPos + target.index, stackslots[function.index].HeapPtr);

								if (error.raised)
								{
									goto flag_handle_error;
								}

								stackslots[target.index] = result;

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

								StackLocater target;
								target.index = dst_index;

								StackLocater instance;
								LoadStackLocater(&instance, &PC);

								NaNBoxing thisValue = stackslots[instance.index];
								if (thisValue.ValueType == NaNBoxing.BoxType.Null)
								{
									RaiseTypeError_AccessNull(ref error);
									goto flag_handle_error;
								}

								var obj = Context.GC.Heap[thisValue.HeapPtr];
								if (obj.TypeKind == RtHeapTypeKind.ARRAY)
								{
									uint len = ((RtPayloadArray)obj.facility).GetLength(this);
									stackslots[target.index].SetUInt(len);

								}
								else
								{
									Debug.Assert(obj.TypeKind == RtHeapTypeKind.VECTOR);
									int len = ((RtPayloadVector)obj.facility).GetStore(this).length;
									stackslots[target.index].SetInt(len);
								}

							}
							break;
						case INS_Code.read_property:
							{

								StackLocater target;
								target.index = dst_index;

								StackLocater instance;
								LoadStackLocater(&instance, &PC);

								uint vtable_ = 0;
								LoadUInt(&vtable_, &PC);
								ushort vtable_index = (ushort)vtable_;

								NaNBoxing thisValue;
								if (instance.index >= 0)
								{
									thisValue = stackslots[instance.index];
								}
								else
								{
									var o = methodscope; //Context.GC.Heap[scope_ptr];
									int instancePtr = scope_ptr;
									do
									{
										if (o.TypeKind == RtHeapTypeKind.MethodScope)
										{
											RtPayloadMethodScope rtPayload = (RtPayloadMethodScope)o.facility;
											o = Context.GC.Heap[rtPayload.ParentPtr];
											instancePtr = rtPayload.ParentPtr;
										}
										else
										{
											break;
										}

									} while (true);
									thisValue = new NaNBoxing();
									thisValue.SetHeapPtr(instancePtr);
								}

								if (thisValue.ValueType == NaNBoxing.BoxType.Null)
								{
									RaiseTypeError_AccessNull(ref error);
									goto flag_handle_error;
								}

								NaNBoxing result = InvokeReadProperty(ref error, thisValue, vtable_index, ref stackslots, stackStPos + target.index);
								if (error.raised)
								{
									goto flag_handle_error;
								}

								stackslots[target.index] = result;

							}
							break;
						case INS_Code.read_property_interface:
							{
								StackLocater target;
								target.index = dst_index;

								StackLocater instance;
								LoadStackLocater(&instance, &PC);

								int class_id = 0;
								LoadInt32(&class_id, &PC);

								uint vtable_ = 0;
								LoadUInt(&vtable_, &PC);
								ushort vtable_index = (ushort)vtable_;

								NaNBoxing thisValue;
#if DEBUG
								if (instance.index >= 0)
#endif
								{
									thisValue = stackslots[instance.index];
								}
#if DEBUG
								else
								{
									throw new InvalidOperationException();
								}
#endif
								if (thisValue.ValueType == NaNBoxing.BoxType.Null)
								{
									RaiseTypeError_AccessNull(ref error);
									goto flag_handle_error;
								}
#if DEBUG
								if (thisValue.ValueType != NaNBoxing.BoxType.HeapPtr)
								{
									throw new InvalidOperationException();
								}
#endif

								{
									RtHeapInstance ins = Context.GC.Heap[thisValue.HeapPtr];

#if DEBUG
									if (ins.TypeKind == RtHeapTypeKind.INSTANCE)
									{
#endif
										var boxing = constants[class_id];
#if DEBUG
										if (boxing.ValueType != NaNBoxing.BoxType.Uint)
										{
											throw new InvalidOperationException();
										}
#endif
										var @class = Context.link_const_class[(int)boxing.UIntValue];
#if DEBUG
										if (!@class.Instance.IsInterface)
										{
											throw new InvalidOperationException();
										}
#endif

										int m_idx = ((ASInstance)ins.Type)._interface_impl_.First((i) => i.interface_type == @class.Type_identifier)[vtable_index];
										var vtableitem = ins.Type._vtable.Items[m_idx];

										var function = vtableitem.Trait.Method;
										var define = (ASInstance)vtableitem.DefineAt;

										NaNBoxing result = RunMethod(function,
										thisValue, thisValue.HeapPtr, define, 0, null, stackslots, ref error, stackStPos + target.index);

										if (error.raised)
										{
											goto flag_handle_error;
										}

										stackslots[target.index] = result;


#if DEBUG
									}
									else
									{
										throw new InvalidOperationException();
									}
#endif
								}

							}
							break;
						case INS_Code.write_property:
							{
								StackLocater valueLoc;
								valueLoc.index = dst_index;

								StackLocater instance;
								LoadStackLocater(&instance, &PC);

								uint vtable_ = 0;
								LoadUInt(&vtable_, &PC);
								ushort vtable_index = (ushort)vtable_;

								void* argementsPtr = &valueLoc;

								NaNBoxing thisValue;
								if (instance.index >= 0)
								{
									thisValue = stackslots[instance.index];
								}
								else
								{
									var o = methodscope; //Context.GC.Heap[scope_ptr];
									int instancePtr = scope_ptr;
									do
									{
										if (o.TypeKind == RtHeapTypeKind.MethodScope)
										{
											RtPayloadMethodScope rtPayload = (RtPayloadMethodScope)o.facility;
											o = Context.GC.Heap[rtPayload.ParentPtr];
											instancePtr = rtPayload.ParentPtr;
										}
										else
										{
											break;
										}

									} while (true);
									thisValue = new NaNBoxing();
									thisValue.SetHeapPtr(instancePtr);
								}

								if (thisValue.ValueType == NaNBoxing.BoxType.Null)
								{
									RaiseTypeError_AccessNull(ref error);
									goto flag_handle_error;
								}
#if DEBUG
								if (thisValue.ValueType != NaNBoxing.BoxType.HeapPtr)
								{
									throw new InvalidOperationException(); //非堆对象不可能有要写的属性
								}
								else
#endif
								{
									BeforeWriteProperty();

									RtHeapInstance ins = Context.GC.Heap[thisValue.HeapPtr];

									if (ins.TypeKind == RtHeapTypeKind.INSTANCE
										||
										ins.TypeKind == RtHeapTypeKind.VECTOR
										)
									{
										var vtableitem = ins.Type._vtable.Items[vtable_index];
										var function = vtableitem.Trait.Method;

										var define = (ASInstance)vtableitem.DefineAt;

										RunMethod(function,
										thisValue, thisValue.HeapPtr, define, 1, (byte*)argementsPtr, stackslots, ref error, -1);

										if (error.raised)
										{
											goto flag_handle_error;
										}

									}
									else if (ins.TypeKind == RtHeapTypeKind.ARRAY)
									{
										var vtableitem = ins.Type._vtable.Items[vtable_index];
										var function = vtableitem.Trait.Method;

										var define = (ASInstance)vtableitem.DefineAt;

										RunMethod(function,
										thisValue, thisValue.HeapPtr, define, 1, (byte*)argementsPtr, stackslots, ref error, -1);

										if (error.raised)
										{
											goto flag_handle_error;
										}
									}
									else if (ins.TypeKind == RtHeapTypeKind.CLASS)
									{
										var @class = ((RtPayloadScriptClass)ins.facility).Meta;
										var function = @class._vtable.Items[vtable_index].Trait.Method;


										RunMethod(function,
											thisValue, thisValue.HeapPtr, @class, 1, (byte*)argementsPtr, stackslots, ref error, -1);

										if (error.raised)
										{
											goto flag_handle_error;
										}

									}
									else if (ins.TypeKind == RtHeapTypeKind.CLOSURE)
									{
#if DEBUG
										if (vtable_index == 1)
										{

											var prop = Context.FUNCTION.Instance._vtable.Items[vtable_index].Trait.Method;
											if (prop.Name != "prototype" || prop.Trait.Kind != TraitKind.Setter)
											{
												throw new InvalidOperationException();
											}
#endif


											byte* aPtr = (byte*)argementsPtr;
											StackLocater argLocater;
											LoadStackLocater(&argLocater, &aPtr);

											NaNBoxing box = stackslots[argLocater.index];

											WriteFunctionProto(box, ref error, ins, thisValue.HeapPtr);
											if (error.raised)
											{
												goto flag_handle_error;
											}
#if DEBUG
										}

										else
										{
											throw new InvalidOperationException();
										}
#endif
									}
#if DEBUG
									else
									{
										throw new InvalidOperationException();//其他类型应该没有要写的属性
									}
#endif

								}


							}
							break;
						case INS_Code.write_property_interface:
							{
								StackLocater valueLoc;
								valueLoc.index = dst_index;

								StackLocater instance;
								LoadStackLocater(&instance, &PC);

								int class_id = 0;
								LoadInt32(&class_id, &PC);

								uint vtable_ = 0;
								LoadUInt(&vtable_, &PC);
								ushort vtable_index = (ushort)vtable_;

								void* argementsPtr = &valueLoc;

								NaNBoxing thisValue;
#if DEBUG
								if (instance.index >= 0)
#endif
								{
									thisValue = stackslots[instance.index];
									//LoadValue(stackslots[instance.index], ref error, ref stackslots, stackStPos);
									//if (error.raised)
									//{
									//	goto flag_handle_error;
									//}
								}
#if DEBUG
								else
								{
									throw new InvalidOperationException();
								}
#endif

								if (thisValue.ValueType == NaNBoxing.BoxType.Null)
								{
									RaiseTypeError_AccessNull(ref error);
									goto flag_handle_error;
								}

#if DEBUG
								if (thisValue.ValueType != NaNBoxing.BoxType.HeapPtr)
								{
									throw new InvalidOperationException(); //接口，肯定不是基本类型
								}
								else
#endif
								{
									BeforeWriteProperty();

									RtHeapInstance ins = Context.GC.Heap[thisValue.HeapPtr];

									if (ins.TypeKind == RtHeapTypeKind.INSTANCE)
									{
										var boxing = constants[class_id];
#if DEBUG
										if (boxing.ValueType != NaNBoxing.BoxType.Uint)
										{
											throw new InvalidOperationException();
										}
#endif
										var @class = Context.link_const_class[(int)boxing.UIntValue];
#if DEBUG
										if (!@class.Instance.IsInterface)
										{
											throw new InvalidOperationException();
										}
#endif

										int m_idx = ((ASInstance)ins.Type)._interface_impl_.First((i) => i.interface_type == @class.Type_identifier)[vtable_index];
										var vtableitem = ins.Type._vtable.Items[m_idx];

										var function = vtableitem.Trait.Method;

										var define = (ASInstance)vtableitem.DefineAt;

										RunMethod(function,
										thisValue, thisValue.HeapPtr, define, 1, (byte*)argementsPtr, stackslots, ref error, -1);

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

								//throw new NotImplementedException();
							}
							break;

						case INS_Code.storeScopeH:
							{
								ScopeHeapLocater heapLocater;
								{
									heapLocater.ScopeIndex = *(ushort*)PC; PC += 2;
									heapLocater.MemberIndex = *(ushort*)PC; PC += 2;

									//byte* _p = (byte*)&heapLocater.ScopeIndex;
									//*_p++ = *PC++;
									//*_p = *PC++;

									//_p = (byte*)&heapLocater.MemberIndex;
									//*_p++ = *PC++;
									//*_p = *PC++;
								}
								StackLocater stackLocater;
								stackLocater.index = dst_index;

								NaNBoxing value = stackslots[stackLocater.index];
								var s = methodscope; //Context.GC.Heap[scope_ptr];

								int* m_scope = method_scopes;
								*m_scope++ = scope_ptr;

							label_method_parent:
								switch (s.TypeKind)
								{
									case RtHeapTypeKind.CLASS:
									case RtHeapTypeKind.GLOBAL:
										{
											RtPayloadScriptClass heap = (RtPayloadScriptClass)s.facility;

											if (heap.Meta._link_codescope.index != heapLocater.ScopeIndex)
											{
#if DEBUG
												if (s.TypeKind != RtHeapTypeKind.CLASS)
												{
													throw new InvalidOperationException();
												}
												else
#endif
												{
													heap = (RtPayloadScriptClass)Context.GC.Heap[((ASScript)heap.Meta._link_codescope.Parent.Container).__global_index__]
															.facility;
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

											ConvertValueType(ref error, value, t.TypeKind, t.__rt_type_class__, ref conv, scope_ptr, thisPtr);
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

												RtPayloadScriptClass heap = (RtPayloadScriptClass)Context.GC.Heap[((ASScript)sType.Container).__global_index__]
															.facility;
												ASTrait t = heap.Meta._link_codescope.Members[heapLocater.MemberIndex].trait;


												Context.GC.CheckGC(ref error);
												if (Context.StackPosition >= Context.STACK_LENGTH)
												{
													RaiseStackOverflow(ref error);
													goto flag_handle_error;
												}

												ref NaNBoxing conv = ref Context.StackSlots[Context.StackPosition];
												Context.StackPosition++;

												ConvertValueType(ref error, value, t.TypeKind, t.__rt_type_class__, ref conv, scope_ptr, thisPtr);
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
												RtPayloadInstance heap = (RtPayloadInstance)s.facility;


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
													s.Type._link_codescope.Members[heapLocater.MemberIndex].__rt_type_class__, ref conv , scope_ptr, thisPtr);

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
												int parentPtr = ((RtPayloadMethodScope)s.facility).ParentPtr;
												s = Context.GC.Heap[parentPtr];
												*m_scope++ = parentPtr;

												goto label_method_parent;
											}
											else
											{

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
													ConvertValueType(ref error, value, scopemember.TypeKind, scopemember.__rt_type_class__, ref conv,scope_ptr,thisPtr);
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

												RtPayloadMethodScope heap = (RtPayloadMethodScope)s.facility;
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


							}
							break;

						case INS_Code.storeMethodVariable:
							{
								ScopeHeapLocater heapLocater;
								{
									heapLocater.ScopeIndex = *(ushort*)PC; PC += 2;
									heapLocater.MemberIndex = *(ushort*)PC; PC += 2;
								}
								NaNBoxing value = stackslots[dst_index];

#if DEBUG
								if (methodscope.Type._link_codescope.index != heapLocater.ScopeIndex)
									throw new InvalidOperationException();
#endif

								var scopemember = methodscope.Type._link_codescope.Members[heapLocater.MemberIndex];

								Context.GC.CheckGC(ref error);
								if (Context.StackPosition >= Context.STACK_LENGTH)
								{
									RaiseStackOverflow(ref error);
									goto flag_handle_error;
								}

								ref NaNBoxing conv = ref Context.StackSlots[Context.StackPosition];
								Context.StackPosition++;

								bool isheaptype = false;

								if (scopemember.Kind == ScopeMemberKind.Parameter)
								{
									isheaptype = scopemember.TypeKind.IsHeapType();

									ConvertValueType(ref error, value, scopemember.TypeKind, scopemember.__rt_type_class__, ref conv, scope_ptr, thisPtr);
								}
								else
								{
									ASTrait t = scopemember.trait;

									isheaptype = t.TypeKind.IsHeapType();

									ConvertValueType(ref error, value, t.TypeKind, t.__rt_type_class__, ref conv, scope_ptr, thisPtr);
								}
								if (error.raised)
								{
									Context.StackPosition--;
									goto flag_handle_error;
								}

								value = conv;

								
								

								RtPayloadMethodScope heap = (RtPayloadMethodScope)methodscope.facility;

								if (isheaptype)
								{
									int* m_scope = method_scopes;
									*m_scope++ = scope_ptr;
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
							break;
						case INS_Code.storeHeapValueRef:
							{
								StackLocater target;
								StackLocater source;
								target.index = dst_index;
								LoadStackLocater(&source, &PC);

#if DEBUG
								if (stackslots[target.index].ValueType != NaNBoxing.BoxType.HeapPtr)
								{
									throw new InvalidOperationException();
								}
#endif
								RtHeapInstance cache = Context.GC.Heap[stackslots[target.index].HeapPtr];

								if (cache.TypeKind == RtHeapTypeKind.CLOSURE)
								{
									RaiseReferenceError_WriteToMethod(ref error, (ASMethodBody)cache.Type, ((RtPayloadClosure)cache.facility)._ref_as_type.QName);

									//throw new NotImplementedException($"Cannot assign to a method { cache.Type.QName.Name } on { ((RtPayloadClosure)cache.facility)._ref_as_type.QName.Name }.");
									goto flag_handle_error;
								}

#if DEBUG
								if (cache.TypeKind != RtHeapTypeKind.STACK_CACHE_OBJ)
								{
									throw new InvalidOperationException();
								}
#endif

								NaNBoxing box = stackslots[source.index];


								RtPayloadStackCache cacheObj = (RtPayloadStackCache)cache.facility;

								if (cacheObj.RefInstance.ValueType != BoxType.HeapPtr)
								{
#if DEBUG
									if (!(cacheObj.searchPropertyName.ValueType == BoxType.HeapPtr || cacheObj.searchPropertyName.ValueType == BoxType.LocalString))
									{
										throw new InvalidOperationException();
									}
#endif

									
									
									ReadOnlySpan<char> searchName;
									if (cacheObj.searchPropertyName.ValueType == BoxType.HeapPtr)
									{
										searchName = ((RtPayloadString)Context.GC.Heap[cacheObj.searchPropertyName.HeapPtr].facility).Str;
									}
									else
									{
										Span<char> temp = frame_holdchars.Span; //stackalloc char[16];//用于从LocalString中提取值
										int l = cacheObj.searchPropertyName.GetLocalStringChars(temp);
										searchName = temp.Slice(0, l);
									}
									



									cacheObj.searchPropertyName.SetUndefined();


									ASNamespace @namespace = null;
									//NaNBoxing ns = new NaNBoxing();
									//if (cacheObj.searchNameSpacePtr > 0)
									//{
									//	ns.SetHeapPtr(cacheObj.searchNameSpacePtr);
									//	RtHeapInstance ns_instance = Context.GC.Heap[cacheObj.searchNameSpacePtr];
									//	@namespace = ((RtPayloadNameSpace)ns_instance.facility).ASNamespace;

									//	cacheObj.searchNameSpacePtr = 0;

									//}
									RaiseReferenceError_CanNotCreateProperty(ref error, @namespace, searchName, cacheObj.as_type.QName);

									goto flag_handle_error;

								}
								else
								{
									RtHeapInstance instance = Context.GC.Heap[cacheObj.RefInstance.HeapPtr];

									if (cacheObj.searchPropertyName.ValueType == BoxType.HeapPtr || cacheObj.searchPropertyName.ValueType == BoxType.LocalString )
									{
										Context.GC.CheckGC(ref error); //只能在此处先GC,否则后面会意外回收searchname_ptr。 

										//int searchname_ptr;// = cacheObj.searchPropertyNamePtr;
										//string searchName = ((RtPayloadString)Context.GC.Heap[cacheObj.searchPropertyNamePtr].facility).Str;
										
										ReadOnlySpan<char> searchName;
										if (cacheObj.searchPropertyName.ValueType == BoxType.HeapPtr)
										{
											//searchname_ptr = cacheObj.searchPropertyName.HeapPtr;
											searchName = ((RtPayloadString)Context.GC.Heap[cacheObj.searchPropertyName.HeapPtr].facility).Str;
										}
										else
										{
											Span<char> temp = frame_holdchars.Span; //stackalloc char[16];//用于从LocalString中提取值
											int l = cacheObj.searchPropertyName.GetLocalStringChars(temp);
											searchName = temp.Slice(0, l);

											//searchname_ptr = Context.GC.AllocString(cacheObj.searchPropertyName.LocalStringValue);
											//if (searchname_ptr == 0)
											//{
											//	RaiseOutOfMemory(ref error);
											//	goto flag_handle_error;
											//}
											//cacheObj.searchPropertyName.SetHeapPtr(searchname_ptr);
										}
										//
										//cacheObj.searchPropertyName.SetUndefined(); 本应在这里清理,但是后面还有CreateDynamic需要用。



										ASNamespace @namespace = null;
										NaNBoxing ns = new NaNBoxing();
										if (cacheObj.searchNameSpacePtr > 0)
										{
											ns.SetHeapPtr(cacheObj.searchNameSpacePtr);
											RtHeapInstance ns_instance = Context.GC.Heap[cacheObj.searchNameSpacePtr];
											@namespace = ((RtPayloadNameSpace)ns_instance.facility).ASNamespace;

											cacheObj.searchNameSpacePtr = 0;

										}


										if (instance.TypeKind == RtHeapTypeKind.INSTANCE
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
										else if (instance.TypeKind == RtHeapTypeKind.NAMESPACE)
										{
											RaiseReferenceError_CanNotCreateProperty(ref error, @namespace, searchName, cacheObj.as_type.QName);
											cacheObj.searchPropertyName.SetUndefined();
											goto flag_handle_error;
										}
										else if (instance.TypeKind == RtHeapTypeKind.VECTOR)
										{

											RaiseReferenceError_CanNotCreateProperty(ref error, @namespace, searchName, instance.Type.QName);
											cacheObj.searchPropertyName.SetUndefined();
											goto flag_handle_error;
										}
										else if (instance.TypeKind == RtHeapTypeKind.CLOSURE
											&&
												((ASMethodBody)instance.Type).Method.__ismethod
											)
										{
											RaiseReferenceError_CanNotCreateProperty(ref error, @namespace, searchName, buildin_as_methodclosure);
											cacheObj.searchPropertyName.SetUndefined();
											goto flag_handle_error;
										}
										else if (instance.TypeKind == RtHeapTypeKind.STRING)
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
											if (instance.TypeKind == RtHeapTypeKind.ARRAY)
											{
#if DEBUG
												if (cacheObj.trait[0] == null && cacheObj.trait[1] == null

													&& cacheObj.indexer_key.ValueType == NaNBoxing.BoxType.Uint

													)
#endif

												{

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
											else if (instance.TypeKind == RtHeapTypeKind.VECTOR)
											{
												//Vector不能动态创建属性
												if (!RtPayloadVector.IsValidIndexType(cacheObj.indexer_key))
												{
													RaiseReferenceError_CanNotCreateProperty(ref error, null, Extensions.GetPrimitiveValueToString(this, cacheObj.indexer_key), instance.Type.QName);
													goto flag_handle_error;
												}

												int maxlen;int validid;
												if (!((RtPayloadVector)instance.facility).IsValidIndexRange(cacheObj.indexer_key,out validid, out maxlen,this))
												{
													if (validid == maxlen && maxlen<int.MaxValue) //扩容
													{
														((RtPayloadVector)instance.facility).Resize(validid+1, ref error,this, (ASInstance)instance.Type);
														
														if (error.raised)
														{
															goto flag_handle_error;
														}

														//throw new NotImplementedException();
													}
													else
													{
														RaiseRangeError(ref error, Extensions.GetPrimitiveValueToString(this, cacheObj.indexer_key), maxlen);
														goto flag_handle_error;
													}
												}

												RtPayloadVector vector = ((RtPayloadVector)instance.facility);

												Context.GC.CheckGC(ref error);
												if (Context.StackPosition >= Context.STACK_LENGTH)
												{
													RaiseStackOverflow(ref error);
													goto flag_handle_error;
												}

												ref NaNBoxing conv = ref Context.StackSlots[Context.StackPosition];
												Context.StackPosition++;

												ConvertValueType(ref error, box, vector.element_type, vector.element_asclass, ref conv, scope_ptr, thisPtr);
												if (error.raised)
												{
													Context.StackPosition--;
													goto flag_handle_error;
												}

												vector.SetSlot(validid, this, cacheObj.RefInstance.HeapPtr, conv,ref error);

												Context.StackPosition--;

												if (error.raised)
												{
													goto flag_handle_error;
												}

												//throw new NotImplementedException();
											}
											else
											{
#if DEBUG
												if (!(instance.TypeKind == RtHeapTypeKind.INSTANCE && ((ASInstance)instance.Type).Flags.HasFlag(ClassFlags.Indexer)))
												{
													throw new InvalidOperationException();
												}
#endif
												
												if (Context.StackPosition + 2 >= Context.STACK_LENGTH)
												{
													RaiseStackOverflow(ref error);
													goto flag_handle_error;
												}

												var argSpan = Context.StackSlots.AsSpan(Context.StackPosition, 2);

												Context.StackPosition += 2;
												Context.GC.CheckGC(ref error);


												var indexer_key = GetSaveValue(cacheObj.indexer_key, ref error);
												if (error.raised)
												{
													Context.StackPosition -= 2;
													goto flag_handle_error;
												}

												argSpan[0] = indexer_key;

												box = GetSaveValue(box, ref error);
												if (error.raised)
												{
													Context.StackPosition -= 2;
													goto flag_handle_error;
												}

												argSpan[1] = box;

												tmpArgLoc[0].index = 0;
												tmpArgLoc[1].index = 1; ;


												NaNBoxing _this = new NaNBoxing();
												_this.SetHeapPtr(cacheObj.RefInstance.HeapPtr);

												RunMethod(((ASInstance)instance.Type).indexer_set, _this,
													cacheObj.RefInstance.HeapPtr, instance.Type, 2, (byte*)tmpArgLoc, argSpan, ref error, -1);

												Context.StackPosition -= 2;
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
											BeforeWriteProperty();

											if (cacheObj.trait[1] == Context.FUNCTION.Instance._vtable.Items[1].Trait)
											{
												//写Function的 prototype属性。
												WriteFunctionProto(box, ref error, Context.GC.Heap[cacheObj.RefInstance.HeapPtr], cacheObj.RefInstance.HeapPtr);
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
													RaiseRangeError(ref error, Extensions.GetPrimitiveValueToString(this, box), uint.MaxValue);
													goto flag_handle_error;
												}


												Context.StackSlots[Context.StackPosition] = box;
												StackLocater argLoc = new StackLocater() { index = stackslots.Length };
												Context.StackPosition++;

												NaNBoxing _this = new NaNBoxing();
												_this.SetHeapPtr(cacheObj.RefInstance.HeapPtr);

												RunMethod(cacheObj.trait[1].Method, _this,
													cacheObj.RefInstance.HeapPtr, instance.Type, 1, (byte*)&argLoc, Context.StackSlots.AsSpan(stackStPos, stackslots.Length + 1), ref error, -1);

												Context.StackPosition--;
												if (error.raised)
												{
													goto flag_handle_error;
												}
											}

										}
										else if (instance.TypeKind == RtHeapTypeKind.GLOBAL || instance.TypeKind == RtHeapTypeKind.CLASS)
										{
											RtPayloadScriptClass payload = (RtPayloadScriptClass)instance.facility;

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

												ConvertValueType(ref error, box, trait.TypeKind, trait.__rt_type_class__, ref conv,scope_ptr,thisPtr);
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
										else if (instance.TypeKind == RtHeapTypeKind.INSTANCE)
										{
											RtPayloadInstance payload = (RtPayloadInstance)instance.facility;

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
												ConvertValueType(ref error, box, trait.TypeKind, trait.__rt_type_class__, ref conv, scope_ptr, thisPtr);
												if (error.raised)
												{
													Context.StackPosition--;
													goto flag_handle_error;
												}
												if (payload.IsUpdateStructOrEqual(Context, cacheObj.scopemember_index, conv, (ASInstance)instance.Type))
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

													payload.SetSlot(box, cacheObj.scopemember_index, instance.Type._link_codescope, this);
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
							break;
						
						case INS_Code.ld_memberInitValue:
							{
								ScopeHeapLocater heapLocater;
								{
									heapLocater.ScopeIndex = *(ushort*)PC; PC += 2;
									heapLocater.MemberIndex = *(ushort*)PC; PC += 2;
									//byte* _p = (byte*)&heapLocater.ScopeIndex;
									//*_p++ = *PC++;
									//*_p = *PC++;

									//_p = (byte*)&heapLocater.MemberIndex;
									//*_p++ = *PC++;
									//*_p = *PC++;
								}

								var s = methodscope; //Context.GC.Heap[scope_ptr];
								int* m_scope = method_scopes;
								*m_scope++ = scope_ptr;

							label_method_parent:

								switch (s.TypeKind)
								{
									case RtHeapTypeKind.CLASS:
									case RtHeapTypeKind.GLOBAL:
										{
											RtPayloadScriptClass heap = (RtPayloadScriptClass)s.facility;
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
											RtPayloadInstance heap = (RtPayloadInstance)s.facility;

											ASTrait t = scopeType._link_codescope.Members[heapLocater.MemberIndex].trait;
											heap.SetSlot(t.Value.initValue.Value, heapLocater.MemberIndex, scopeType._link_codescope, this);

										}
										break;
									case RtHeapTypeKind.MethodScope:
										{
											if (s.Type._link_codescope.index != heapLocater.ScopeIndex)
											{
												int parentPtr = ((RtPayloadMethodScope)s.facility).ParentPtr;
												s = Context.GC.Heap[parentPtr];
												*m_scope++ = parentPtr;
												goto label_method_parent;
											}
											else
											{
												ASTrait t = s.Type._link_codescope.Members[heapLocater.MemberIndex].trait;

												RtPayloadMethodScope heap = (RtPayloadMethodScope)s.facility;
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
										Environment.FailFast("出错了，这里跑不到"); PC_PTR = 0;return;
#endif
								}
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

								StackLocater target;
								StackLocater typeLocater;
								target.index = dst_index;
								LoadStackLocater(&typeLocater, &PC);
								int argsCount;
								LoadInt32(&argsCount, &PC);

								//StackLocater* argements = (StackLocater*)PC;
								//!!需要考虑对齐问题
								byte* argementsPtr = PC;
								PC += argsCount * 4;

								NaNBoxing type_box = stackslots[typeLocater.index];

								if (type_box.ValueType == BoxType.HeapPtr)
								{
									RtHeapInstance type = Context.GC.Heap[type_box.HeapPtr];

									if (type.TypeKind == RtHeapTypeKind.CLASS)
									{

										ASClass @class = (ASClass)((RtPayloadScriptClass)type.facility).Meta;
										//构造实例

										RtHeapInstance instance;
										int instancePtr;

										if (@class.Instance.Flags.HasFlag(ClassFlags.NoConstructor))
										{
											stackslots[target.index].SetNull();
											if (@class != Context.METHOD_CLOSURE)
											{
												RaiseTypeError_Instantiation_non_constructor(ref error);
											}
											break;
										}
										else if (@class.Instance.Flags.HasFlag( ClassFlags.Vector ))
										{
											int ptrIndex = stackStPos + target.index;

											instancePtr = Context.CacheVectorPtr + ptrIndex;
											instance = Context.GC.Heap[instancePtr];
											
											instance.Type = @class.Instance;
											((RtPayloadVector)instance.facility).HEAPINSTANCE_PTR = 0;
											((RtPayloadVector)instance.facility).element_asclass = @class.Instance._element_class  ;
											((RtPayloadVector)instance.facility).element_type = @class.Instance._element_class == null? TypeKind.Any: (TypeKind)@class.Instance._element_class.Type_identifier;
											//((RtPayloadVector)instance.facility).GetStore(this).SetBuffer(0);
											((RtPayloadVector)instance.facility).GetStore(this).length = 0;

											stackslots[target.index].SetHeapPtr(instancePtr);

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
											instancePtr = InitCacheInstance(@class, ptrIndex, true);

											instance = Context.GC.Heap[instancePtr];

											//instance = Context.GC.Heap[instancePtr];
											//instance.Type = @class.Instance;

											//((RtPayloadInstance)instance.facility).HEAPINSTANCE_PTR = 0;
											//((RtPayloadInstance)instance.facility).Set_PROPERTY_PTR(0, this);
											//((RtPayloadInstance)instance.facility).Set_PROTOTYPE(((RtPayloadScriptClass)Context.GC.Heap[@class.__instance_index__].facility).PROTO__PTR, this);
											//((RtPayloadInstance)instance.facility).methodscopeslot_ref_state = 0;

											//CodeScope scope = @class.Instance._link_codescope;
											//if (scope.TypeLayout.Size > 0)
											//{
											//	((RtPayloadInstance)instance.facility).Init(scope, this);
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

												if (argsCount <= RtPayloadArray.MAX_CACHE_ELEMENT + ext_slot)
												{
													int ptrIndex = stackStPos + target.index;
													instancePtr = Context.CacheArrayPtr + ptrIndex;
													instance = Context.GC.Heap[instancePtr];
													instance.Type = Context.ARRAY.Instance;

													((RtPayloadArray)instance.facility).array_len = 0;
													((RtPayloadArray)instance.facility).methodscopeslot_ref_state = 0;
													((RtPayloadArray)instance.facility).HEAPINSTANCE_PTR = 0;


												}
												else
												{
													instancePtr = Context.GC.AllocArray(out instance, RtPayloadArray.ArrayStoreMode.normal);
												}
											}
											else if (@class.Type_identifier == (ulong)TypeKind.String)
											{
												if (argsCount == 0)
												{
													instancePtr = EMPTY_STR;
													stackslots[target.index].SetHeapPtr(instancePtr);

												}
												else if (argsCount >= 1)
												{
													byte* P = argementsPtr + sizeof(StackLocater) * (Context.STRING.Instance.Constructor.Parameters.Count - 1);
													StackLocater argLocater;
													LoadStackLocater(&argLocater, &P);

													NaNBoxing box = stackslots[argLocater.index];
													ConvertValueType(ref error, box, TypeKind.String, Context.STRING, ref stackslots[target.index]);
													if (error.raised)
													{
														goto flag_handle_error;
													}

												}

												break;

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

												break;
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
													Environment.FailFast("出错了，这里跑不到"); PC_PTR = 0; return;
#endif
													}

													
												}
												else if (argsCount >= 1)
												{
													byte* P = argementsPtr + sizeof(StackLocater) * (Context.NUMBER.Instance.Constructor.Parameters.Count - 1);
													StackLocater argLocater;
													LoadStackLocater(&argLocater, &P);

													NaNBoxing box = stackslots[argLocater.index];

													box = ToPrimitive(ref error, box, HINT.h_number, scope_ptr, target, target, stackslots, stackStPos, thisPtr);
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
													Environment.FailFast("出错了，这里跑不到"); PC_PTR = 0; return;
#endif
													}


													if (error.raised)
													{
														goto flag_handle_error;
													}

												}

												break;



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

													box = ToPrimitive(ref error, box, HINT.h_number, scope_ptr, target, target, stackslots, stackStPos, thisPtr);
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

												break;
											}
											else if (@class.Type_identifier == (ulong)TypeKind.Function)
											{
												if (argsCount > 0)
												{
													RaiseArgementErrorCountMisMatch(ref error, Context.FUNCTION.Instance.Constructor, 0, argsCount);
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
													((RtPayloadClosure)closure.facility).ScopePtr = scope_ptr;
													((RtPayloadClosure)closure.facility).ScopeType = scopeType;
													((RtPayloadClosure)closure.facility).This.SetNull();
													((RtPayloadClosure)closure.facility)._ref_as_type = define;
													((RtPayloadClosure)closure.facility).methodscopeslot_ref_state = 0;
													((RtPayloadClosure)closure.facility).HEAPINSTANCE_PTR = 0;
													stackslots[target.index].SetHeapPtr(closurePtr);


													break;
												}
											}
											else
											{
												instancePtr = Context.GC.AllocInstance(@class.Instance, out instance);
											}

											if (instancePtr == 0)
											{
												//throw new NotImplementedException("out of memory");
												RaiseOutOfMemory(ref error);
												goto flag_handle_error;
											}

											stackslots[target.index].SetHeapPtr(instancePtr);



										}


										//执行构造函数
										RunMethod(((ASInstance)instance.Type).Constructor, stackslots[target.index], instancePtr, @class.Instance, (ushort)argsCount, argementsPtr, stackslots, ref error, -1, 0, true);
										if (error.raised)
										{
											goto flag_handle_error;
										}

									}
									else if (type.TypeKind == RtHeapTypeKind.CLOSURE)
									{
										NaNBoxing constructor_box = GetSaveValue(type_box, ref error); //构造对象的函数，需要访问proto,所以只能先保存到堆里。
										if (error.raised)
										{
											goto flag_handle_error;
										}
										type_box.SetHeapPtr(constructor_box.HeapPtr);
										var constructor_closure = Context.GC.Heap[type_box.HeapPtr];

										if (((ASMethodBody)constructor_closure.Type).Method.__ismethod ||
											constructor_closure.Type == Context.FUNCTION.Instance.Constructor.Body
											)
										{
											RaiseTypeError_RunMethodAsConstructor(ref error, ((ASMethodBody)constructor_closure.Type).Method);
											goto flag_handle_error;
										}



										var function_proto = ((RtPayloadClosure)constructor_closure.facility).PROTOTYPE(this);

										if (function_proto == 0)
										{
											((RtPayloadClosure)constructor_closure.facility).Set_PROTOTYPE(0, this);
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
											RtHeapInstance proto;
											function_proto = Context.GC.AllocInstance(Context.OBJECT.Instance, out proto);
											if (function_proto == 0)
											{
												RaiseOutOfMemory(ref error);
												goto flag_handle_error;
											}

											((RtPayloadClosure)constructor_closure.facility).Set_PROTOTYPE(function_proto, this);
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

										((RtPayloadInstance)instance.facility).HEAPINSTANCE_PTR = 0;
										((RtPayloadInstance)instance.facility).Set_PROPERTY_PTR(0, this,Context.OBJECT.Instance);
										((RtPayloadInstance)instance.facility).Set_PROTOTYPE(function_proto, this);
										((RtPayloadInstance)instance.facility).methodscopeslot_ref_state = 0;

										Context.StackSlots[ptrIndex].SetHeapPtr(instancePtr);


										var constructor = ((ASMethodBody)type.Type).Method;
										NaNBoxing ret_constructor = RunMethod(constructor, Context.StackSlots[ptrIndex],
											((RtPayloadClosure)constructor_closure.facility).ScopePtr,
											((RtPayloadClosure)constructor_closure.facility).ScopeType, (ushort)argsCount, argementsPtr, stackslots, ref error,stackStPos + target.index  , type_box.HeapPtr, true);

										if (error.raised)
										{
											Context.StackPosition--;
											goto flag_handle_error;
										}

										bool move = true;
										if (ret_constructor.ValueType == BoxType.HeapPtr)
										{
											if (Context.GC.Heap[ret_constructor.HeapPtr].TypeKind == RtHeapTypeKind.STRING)
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
											if(((RtPayloadInstance)instance.facility).HEAPINSTANCE_PTR == 0)
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
													Environment.FailFast("出错了，这里跑不到"); PC_PTR = 0; return;
#endif
												}

												((RtPayloadInstance)target_ins.facility).HEAPINSTANCE_PTR = 0;
												((RtPayloadInstance)target_ins.facility).methodscopeslot_ref_state = 0;
												((RtPayloadInstance)target_ins.facility).CopyFrom(instance, this, 0);

												stackslots[target.index].SetHeapPtr(target_instancePtr);
											}
											else
											{
												//这里只可能是在函数里被赋值到了其他变量，那么这时候跟踪到那个变量然后指过去。
												RtPayloadInstance src;
												int src_ptr = RtPayloadInstance.FindAndUpdateHeapInstancePtr(instancePtr, this, out src);

#if DEBUG
												if (!(src_ptr < Context.CacheInstancePtr + Context.STACK_LENGTH) //堆里
														//||
														//(src_ptr < Context.CacheInstancePtr + ((RtPayloadMethodScope)methodscope.facility).StackPos +
														//((RtPayloadMethodScope)methodscope.facility).SlotCount) //传入
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
												stackslots[target.index].SetHeapPtr(src_ptr);
											}
										}


										Context.StackPosition--;
										//throw new NotImplementedException();
									}

									else
									{
#if DEBUG
										if(type.TypeKind == RtHeapTypeKind.MethodScope || type.TypeKind == RtHeapTypeKind.DYNAMIC_PROPERTYS 
											||
											type.TypeKind == RtHeapTypeKind.STACK_CACHE_OBJ || type.TypeKind == RtHeapTypeKind.SHAPE
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

							}
							break;
						case INS_Code.type_cast:
							{
								
								StackLocater value;
								int classid_index;
								LoadStackLocater(&value, &PC);
								LoadInt32(&classid_index, &PC);


								var boxing = constants[classid_index];
#if DEBUG
								if (boxing.ValueType != NaNBoxing.BoxType.Uint)
								{
									throw new InvalidOperationException();
								}
#endif
								var @class = Context.link_const_class[(int)boxing.UIntValue];
								var v = LoadValue(stackslots[value.index], -1, ref error, stackslots, stackStPos + value.index);

								ExplicitConvert(ref error,1, &value , stackslots ,  (TypeKind)@class.Type_identifier, @class, ref stackslots[dst_index], stackStPos + dst_index,scope_ptr,thisPtr,false);
								if (error.raised)
								{ 
									goto flag_handle_error;
								}								
							}
							break;
						case INS_Code.create_prop:
							{
								StackLocater instance;
								StackLocater key;
								StackLocater value;

								instance.index = dst_index;
								LoadStackLocater(&key, &PC);
								LoadStackLocater(&value, &PC);

								var ins_v = stackslots[instance.index];
								var key_v = stackslots[key.index];
								var value_v = stackslots[value.index];

#if DEBUG
								if (ins_v.ValueType != BoxType.HeapPtr) throw new InvalidOperationException();
								if (key_v.ValueType != BoxType.HeapPtr) throw new InvalidOperationException();
#endif
								var ins = Context.GC.Heap[ins_v.HeapPtr];

#if DEBUG
								var k = Context.GC.Heap[key_v.HeapPtr];
								if (ins.TypeKind != RtHeapTypeKind.INSTANCE) throw new InvalidOperationException();
								if (k.TypeKind != RtHeapTypeKind.STRING) throw new InvalidOperationException();
#endif
								CreateDynamic(ref error, ins, key_v, value_v, true, true, true);
								if (error.raised)
								{
									goto flag_handle_error;
								}

							}
							break;
						case INS_Code.super_ctor:
							{
								//执行基类构造函数

								int classid_index = 0;
								LoadInt32(&classid_index, &PC);

								int argsCount;
								LoadInt32(&argsCount, &PC);

								//StackLocater* argements = (StackLocater*)PC;
								//PC += argsCount * 4;
								//!!需要考虑对齐问题
								byte* argementsPtr = PC;
								PC += argsCount * 4;



								var boxing = constants[classid_index];
#if DEBUG
								if (boxing.ValueType != NaNBoxing.BoxType.Uint)
								{
									throw new InvalidOperationException();
								}
#endif

								var super_class = Context.link_const_class[(int)boxing.UIntValue];
								var ctor = super_class.Instance.Constructor;
								RunMethod(ctor, thisPtr, scope_ptr, (super_class).Instance, (ushort)argsCount, argementsPtr, stackslots, ref error, -1);
								if (error.raised)
								{
									goto flag_handle_error;
								}
							}
							break;
						case INS_Code.positive:
							{
								StackLocater dst;
								StackLocater src;

								dst.index = dst_index;
								LoadStackLocater(&src, &PC);

								var v = stackslots[src.index];// LoadValue(stackslots[src.index], ref error, ref stackslots, stackStPos);
															  //if (error.raised)
															  //{
															  //    goto flag_handle_error;
															  //}

								{
									//操作符重载
									ASClass t1;
									int op_override_id1 = GetOpOverrideTypeId(v, out t1);

									if (op_override_id1 != -1)
									{
										var negmethod = overrideOperatorMethods[(int)OverrideOperator.positive][op_override_id1][op_override_id1];
										if (negmethod != null)
										{
#if FORCOMPILER
											if (IsComputeConstExpr)
											{
												throw new EvalConstException();
											}
#endif

											var @class = (ASClass)negmethod.Container;
											Debug.Assert(@class.__instance_index__ != -1);

											if (Context.StackPosition + 1 >= Context.STACK_LENGTH)
											{
												RaiseStackOverflow(ref error);
												goto flag_handle_error;
											}

											Span<NaNBoxing> slots = Context.StackSlots.AsSpan(Context.StackPosition, 1);
											slots[0] = v;

											Context.StackPosition += 1;

											NaNBoxing cls = default; cls.SetHeapPtr(@class.__instance_index__);

											StackLocater args = default; args.index = 0;
											RunMethod(negmethod, cls, scope_ptr, @class, 1, (byte*)&args, slots, ref error, stackStPos + dst.index);

											Context.StackPosition -= 1;
											if (error.raised)
											{
												goto flag_handle_error;
											}
											break;
										}
									}

								}

								v = ToPrimitive(ref error, v, HINT.h_number, scope_ptr, dst, dst, stackslots, stackStPos, thisPtr);
								if (error.raised)
								{
									goto flag_handle_error;
								}

								switch (v.ValueType)
								{
									case NaNBoxing.BoxType.Undefined:
										stackslots[dst.index].SetNumber(double.NaN);
										break;
									case NaNBoxing.BoxType.Null:
										stackslots[dst.index].SetNumber(0.0);
										break;
									case NaNBoxing.BoxType.Boolean:
										if (v.Boolean)
										{
											stackslots[dst.index].SetInt(1);
										}
										else
										{
											stackslots[dst.index].SetInt(0);
										}
										break;
									case NaNBoxing.BoxType.Number:
									case NaNBoxing.BoxType.Int:
									case NaNBoxing.BoxType.Uint:
									case NaNBoxing.BoxType.Sbyte:
									case NaNBoxing.BoxType.Byte:
									case NaNBoxing.BoxType.Short:
									case NaNBoxing.BoxType.UShort:
									case NaNBoxing.BoxType.Float:
										stackslots[dst.index] = v;
										break;
									case NaNBoxing.BoxType.HeapPtr:
										ConvertValueType(ref error, v, TypeKind.Number, Context.NUMBER, ref stackslots[dst.index]); //这里肯定是字符串
#if DEBUG
										if (error.raised)
										{
											throw new InvalidOperationException();
										}
#endif
										break;
#if DEBUG
									case NaNBoxing.BoxType.Fault:
									default:
										throw new InvalidOperationException();
#endif
								}

							}
							break;
						case INS_Code.neg:
							{
								StackLocater dst;
								StackLocater src;

								//LoadStackLocater(&dst, &PC);
								dst.index = dst_index;
								LoadStackLocater(&src, &PC);

								var v = stackslots[src.index];// LoadValue(stackslots[src.index], ref error, ref stackslots, stackStPos);
															  //if (error.raised)
															  //{
															  //    goto flag_handle_error;
															  //}

								{
									//操作符重载
									ASClass t1;
									int op_override_id1 = GetOpOverrideTypeId(v, out t1);
									
									if (op_override_id1 != -1)
									{
										var negmethod = overrideOperatorMethods[(int)OverrideOperator.neg][op_override_id1][op_override_id1];
										if (negmethod != null)
										{
#if FORCOMPILER
											if (IsComputeConstExpr)
											{
												throw new EvalConstException();
											}
#endif
											
											var @class = (ASClass)negmethod.Container;
											Debug.Assert(@class.__instance_index__ != -1);

											if (Context.StackPosition + 1 >= Context.STACK_LENGTH)
											{
												RaiseStackOverflow(ref error);
												goto flag_handle_error;
											}

											Span<NaNBoxing> slots = Context.StackSlots.AsSpan(Context.StackPosition, 1);
											slots[0] = v;
											
											Context.StackPosition += 1;

											NaNBoxing cls = default; cls.SetHeapPtr(@class.__instance_index__);

											StackLocater args = default;args.index = 0;
											RunMethod(negmethod, cls, scope_ptr, @class, 1, (byte*)&args, slots, ref error, stackStPos + dst.index);
											
											Context.StackPosition -= 1;
											if (error.raised)
											{
												goto flag_handle_error;
											}
											break;
										}
									}

								}
								
								v = ToPrimitive(ref error, v, HINT.h_number, scope_ptr, dst, dst, stackslots, stackStPos, thisPtr);
								if (error.raised)
								{
									goto flag_handle_error;
								}

								switch (v.ValueType)
								{
									case NaNBoxing.BoxType.Number:
										stackslots[dst.index].SetNumber(-v.Number);
										break;
									case NaNBoxing.BoxType.Undefined:
										stackslots[dst.index].SetNumber(double.NaN);
										break;
									case NaNBoxing.BoxType.Null:
										stackslots[dst.index].SetNumber(0.0);
										break;
									case NaNBoxing.BoxType.Boolean:
										if (v.Boolean)
										{
											stackslots[dst.index].SetInt(-1);
										}
										else
										{
											stackslots[dst.index].SetInt(0);
										}
										break;
									case NaNBoxing.BoxType.Int:
										if (v.IntValue == 0)
										{
											//有这种代码 : 1 / (-0) , 所以结果需要区分正负无穷。。。
											stackslots[dst.index].SetNumber(-0.0);
										}
										else
										{
											stackslots[dst.index].SetInt(-v.IntValue);
										}
										break;
									case NaNBoxing.BoxType.Uint:
										stackslots[dst.index].SetNumber(-(double)v.UIntValue);
										break;
									case NaNBoxing.BoxType.Sbyte:
										if (v.SByteValue == 0)
										{
											stackslots[dst.index].SetNumber(-0.0);
										}
										else
										{
											stackslots[dst.index].SetInt(-(int)v.SByteValue);
										}
										break;
									case NaNBoxing.BoxType.Byte:
										if (v.ByteValue == 0)
										{
											stackslots[dst.index].SetNumber(-0.0);
										}
										else
										{
											stackslots[dst.index].SetInt(-(int)v.ByteValue);
										}
										break;
									case NaNBoxing.BoxType.Short:
										if (v.ShortValue == 0)
										{
											stackslots[dst.index].SetNumber(-0.0);
										}
										else
										{
											stackslots[dst.index].SetInt(-(int)v.ShortValue);
										}
										break;
									case NaNBoxing.BoxType.UShort:
										if (v.UShortValue == 0)
										{
											stackslots[dst.index].SetNumber(-0.0);
										}
										else
										{
											stackslots[dst.index].SetInt(-(int)v.UShortValue);
										}
										break;
									case NaNBoxing.BoxType.Float:
										stackslots[dst.index].SetFloat(-v.FloatValue);
										break;
									case NaNBoxing.BoxType.HeapPtr:
										ConvertValueType(ref error, v, TypeKind.Number, Context.NUMBER, ref stackslots[dst.index]); //这里肯定是字符串

										Debug.Assert(!error.raised);

										stackslots[dst.index].SetNumber(-stackslots[dst.index].Number);

										break;
#if DEBUG
									case NaNBoxing.BoxType.Fault:
									default:
										throw new InvalidOperationException();
#endif
								}


							}
							break;
						case INS_Code.add:		
							{
								StackLocater dst;
								StackLocater v1;
								StackLocater v2;

								
								dst.index = dst_index;
								LoadStackLocater(&v1, &PC);
								LoadStackLocater(&v2, &PC);
								

								NaNBoxing n1 = stackslots[v1.index];
								NaNBoxing n2 = stackslots[v2.index];

								Exec_Add(ref error, n1, n2, dst, scope_ptr, v1, stackslots, stackStPos, thisPtr);
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

								StackLocater dst;
								StackLocater v1;
								StackLocater v2;

								dst.index = dst_index;
								LoadStackLocater(&v1, &PC);
								LoadStackLocater(&v2, &PC);

								NaNBoxing n1 = stackslots[v1.index];
								NaNBoxing n2 = stackslots[v2.index];

								Exec_Sub(ref error, n1, n2, dst, scope_ptr, v1, stackslots, stackStPos, thisPtr);
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
								StackLocater dst;
								StackLocater v1;
								StackLocater v2;

								dst.index = dst_index;
								LoadStackLocater(&v1, &PC);
								LoadStackLocater(&v2, &PC);

								NaNBoxing n1 = stackslots[v1.index];
								NaNBoxing n2 = stackslots[v2.index];

								Exec_Multiply(ref error, n1, n2, dst, scope_ptr, v1, stackslots, stackStPos, thisPtr);
								if (error.raised)
								{
									goto flag_handle_error;
								}
							}
							break;
						case INS_Code.div:
							{
								StackLocater dst;
								StackLocater v1;
								StackLocater v2;

								dst.index = dst_index;
								LoadStackLocater(&v1, &PC);
								LoadStackLocater(&v2, &PC);


								NaNBoxing n1 = stackslots[v1.index];
								NaNBoxing n2 = stackslots[v2.index];

								Exec_Division(ref error, n1, n2, dst, scope_ptr, v1, stackslots, stackStPos, thisPtr);
								if (error.raised)
								{
									goto flag_handle_error;
								}

							}
							break;
						case INS_Code.modulus:
							{
								StackLocater dst;
								StackLocater v1;
								StackLocater v2;

								dst.index = dst_index;
								LoadStackLocater(&v1, &PC);
								LoadStackLocater(&v2, &PC);


								NaNBoxing n1 = stackslots[v1.index];
								NaNBoxing n2 = stackslots[v2.index];

								Exec_Modulus(ref error, n1, n2, dst, scope_ptr, v1, stackslots, stackStPos, thisPtr);
								if (error.raised)
								{
									goto flag_handle_error;
								}
							}
							break;
						case INS_Code.bitwise:
							{
								StackLocater dst;
								StackLocater v1;
								StackLocater v2;

								dst.index = dst_index;

								uint v = *(uint*)PC; PC += 4;
								byte opMode = (byte)(v & 0xff);
								v1.index = (int)(v >> 8);

								LoadStackLocater(&v2, &PC);

								NaNBoxing n1 = stackslots[v1.index];
								NaNBoxing n2 = stackslots[v2.index];

								Exec_bitWise(ref error, n1, n2, dst, opMode, scope_ptr, v1, v2, stackslots, stackStPos, thisPtr);
								if (error.raised)
								{
									goto flag_handle_error;
								}
							}
							break;
						case INS_Code.logic_comparison:
							{
								StackLocater dst;
								StackLocater v1;
								StackLocater v2;

								dst.index = dst_index;

								uint v = *(uint*)PC; PC += 4;
								byte opMode = (byte)(v & 0xff);
								v1.index = (int)(v >> 8);

								LoadStackLocater(&v2, &PC);

								NaNBoxing n1 = stackslots[v1.index];
								NaNBoxing n2 = stackslots[v2.index];

								Exec_Comparse(ref error,n1,n2,dst,opMode,scope_ptr,v1,v2,stackslots,stackStPos,thisPtr);
								if (error.raised)
								{
									goto flag_handle_error;
								}
							}
							break;
						case INS_Code.logic_not:
							{
								StackLocater dst;
								StackLocater src;

								dst.index = dst_index;
								LoadStackLocater(&src, &PC);

								var v = stackslots[src.index];// LoadValue(stackslots[src.index], ref error, ref stackslots, stackStPos);
															  //if (error.raised)
															  //{
															  //    goto flag_handle_error;
															  //}

								ConvertValueType(ref error, v, TypeKind.Boolean, null, ref v);

#if DEBUG
								if (error.raised) throw new InvalidOperationException();//转Boolean，不可能失败
#endif

								stackslots[dst.index].SetBoolean(!v.Boolean);

							}
							break;
						case INS_Code.strict_eq:
							{
								StackLocater dst;
								StackLocater v1;
								StackLocater v2;

								dst.index = dst_index;
								LoadStackLocater(&v1, &PC);
								LoadStackLocater(&v2, &PC);

								
								stackslots[dst.index].SetBoolean(IsStrictlyEqual(stackslots[v1.index], stackslots[v2.index]));

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
								StackLocater dst;
								StackLocater v1;
								StackLocater v2;

								dst.index = dst_index;
								LoadStackLocater(&v1, &PC);
								LoadStackLocater(&v2, &PC);


								stackslots[dst.index].SetBoolean(!IsStrictlyEqual(stackslots[v1.index], stackslots[v2.index]));

							}
							break;
						case INS_Code.equal:
							{
								StackLocater dst;
								StackLocater v1;
								StackLocater v2;

								dst.index = dst_index;
								LoadStackLocater(&v1, &PC);
								LoadStackLocater(&v2, &PC);

								bool isEqual = IsEqual(stackslots[v1.index], stackslots[v2.index], dst, ref error, scope_ptr, stackslots, stackStPos, thisPtr);
								if (error.raised)
								{
									goto flag_handle_error;
								}
								stackslots[dst.index].SetBoolean(isEqual);

							}
							break;
						case INS_Code.not_equal:
							{
								StackLocater dst;
								StackLocater v1;
								StackLocater v2;

								dst.index = dst_index;
								LoadStackLocater(&v1, &PC);
								LoadStackLocater(&v2, &PC);

								bool isEqual = IsEqual(stackslots[v1.index], stackslots[v2.index], dst, ref error, scope_ptr, stackslots, stackStPos, thisPtr);
								if (error.raised)
								{
									goto flag_handle_error;
								}
								stackslots[dst.index].SetBoolean(!isEqual);
							}
							break;
						case INS_Code.get_in:
							{
								StackLocater dst;
								StackLocater v1;
								StackLocater v2;

								dst.index = dst_index;
								LoadStackLocater(&v1, &PC);
								LoadStackLocater(&v2, &PC);

								var name_v = stackslots[v1.index];
								NaNBoxing name_n = ToPrimitive(ref error, name_v, HINT.h_string, scope_ptr, dst, dst, stackslots, stackStPos, thisPtr);
								if (error.raised)
								{
									goto flag_handle_error;
								}

								string name = Extensions.GetPrimitiveValueToString(this, name_n);

								var type = stackslots[v2.index];
								bool isvaluebox = false;
								if (type.ValueType != BoxType.HeapPtr)
								{
									switch (type.ValueType)
									{
										case BoxType.Number:
											type.SetHeapPtr( Context.NUMBER.__instance_index__); isvaluebox = true;
											break;
										case BoxType.Undefined:
											RaiseTypeError_ATermUndefined(ref error);
											goto flag_handle_error;
										case BoxType.Null:
											RaiseTypeError_AccessNull(ref error);
											goto flag_handle_error;
										case BoxType.Boolean:
											type.SetHeapPtr(Context.BOOLEAN.__instance_index__); isvaluebox = true;
											break;
										case BoxType.Int:
											type.SetHeapPtr(Context.INT.__instance_index__); isvaluebox = true;
											break;
										case BoxType.Uint:
											type.SetHeapPtr(Context.UINT.__instance_index__); isvaluebox = true;
											break;
										case BoxType.Sbyte:
											type.SetHeapPtr(Context.SBYTE.__instance_index__); isvaluebox = true;
											break;
										case BoxType.Byte:
											type.SetHeapPtr(Context.BYTE.__instance_index__); isvaluebox = true;
											break;
										case BoxType.Short:
											type.SetHeapPtr(Context.SHORT.__instance_index__); isvaluebox = true;
											break;
										case BoxType.UShort:
											type.SetHeapPtr(Context.USHORT.__instance_index__); isvaluebox = true;
											break;
										case BoxType.Float:
											type.SetHeapPtr(Context.FLOAT.__instance_index__); isvaluebox = true;
											break;
										case BoxType.HeapPtr:
										case BoxType.Fault:
										default:
											break;
									}
								}

								var find = 
									(ASContainer type,string name,int proto) => 
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

											if (proto_obj.TypeKind != RtHeapTypeKind.VECTOR)
											{
												NaNBoxing value; int shape; int matchslot; RtPayloadDynamic prop;
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
								switch (obj.TypeKind)
								{
									case RtHeapTypeKind.CLASS:
										{
											var @class = ((RtPayloadScriptClass)obj.facility).Meta;
											if (find(@class, name, 0) || find( ((ASClass)@class).Instance ,name,0 ))
											{
												stackslots[dst.index].SetBoolean(true);
											}
											else if (!isvaluebox) // "F" in Number  ,proto是Class
											{
												NaNBoxing value; int shape; int matchslot; RtPayloadDynamic prop;
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
												int proto =  ((RtPayloadScriptClass)obj.facility).PROTO__PTR;
												int steps = 0;
												while (proto != 0 && steps < 32)
												{
													var proto_obj = Context.GC.Heap[proto];
													NaNBoxing value; int shape; int matchslot; RtPayloadDynamic prop;
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
											stackslots[dst.index].SetBoolean(find( Context.OBJECT.Instance , name, type.HeapPtr ));
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
											if (((RtPayloadVector)obj.facility).GetStore(this).IsValidIndexRange(name_n, out index))
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

									
															
							}
							break;
						case INS_Code.get_typeof:
							{
								StackLocater dst;
								StackLocater src;

								dst.index = dst_index;
								LoadStackLocater(&src, &PC);

								var v = stackslots[src.index];

								switch (v.ValueType)
								{
									case BoxType.Undefined:
										stackslots[dst.index].SetHeapPtr(TYPEOF_undefined_STR);
										break;
									case BoxType.Null:
										stackslots[dst.index].SetHeapPtr(TYPEOF_object_STR);
										break;
									case BoxType.Boolean:
										stackslots[dst.index].SetHeapPtr(TYPEOF_boolean_STR);
										break;
									case BoxType.Number:
									case BoxType.Int:
									case BoxType.Uint:
									case BoxType.Sbyte:
									case BoxType.Byte:
									case BoxType.Short:
									case BoxType.UShort:
									case BoxType.Float:
										stackslots[dst.index].SetHeapPtr(TYPEOF_number_STR);
										break;
									case BoxType.LocalString:
										stackslots[dst.index].SetHeapPtr(TYPEOF_string_STR);
										break;
									case BoxType.HeapPtr:
										RtHeapInstance instance = Context.GC.Heap[v.HeapPtr];
										switch (instance.TypeKind)
										{
											case RtHeapTypeKind.STRING:
												stackslots[dst.index].SetHeapPtr(TYPEOF_string_STR);
												break;
											case RtHeapTypeKind.CLASS:
											case RtHeapTypeKind.GLOBAL:
											case RtHeapTypeKind.INSTANCE:
											case RtHeapTypeKind.NAMESPACE:
											case RtHeapTypeKind.ARRAY:
											case RtHeapTypeKind.VECTOR:
												stackslots[dst.index].SetHeapPtr(TYPEOF_object_STR);
												break;
											case RtHeapTypeKind.CLOSURE:
												stackslots[dst.index].SetHeapPtr(TYPEOF_function_STR);
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

										break;
#if DEBUG
									case BoxType.Fault:
									default:
										throw new InvalidOperationException();
#endif
								}
							}
							break;
						case INS_Code.get_is:
							{
								StackLocater dst;
								StackLocater v1;
								StackLocater v2;

								dst.index = dst_index;
								LoadStackLocater(&v1, &PC);
								LoadStackLocater(&v2, &PC);


								var type = stackslots[v2.index];
								if (type.ValueType != BoxType.HeapPtr)
								{
									RaiseTypeError(ref error,type, TypeKind.Class);
									goto flag_handle_error;
								}

								var obj = Context.GC.Heap[type.HeapPtr];
								if (obj.TypeKind != RtHeapTypeKind.CLASS)
								{
									RaiseTypeError(ref error, type, TypeKind.Class);
									goto flag_handle_error;
								}

								var typeclass = (ASClass)((RtPayloadScriptClass)obj.facility).Meta;
								var v = stackslots[v1.index];

								stackslots[dst.index].SetBoolean( Is(v,typeclass) );

							}
							break;
						case INS_Code.cast_as:
							{
								StackLocater dst;
								StackLocater v1;
								StackLocater v2;

								dst.index = dst_index;
								LoadStackLocater(&v1, &PC);
								LoadStackLocater(&v2, &PC);


								var type = stackslots[v2.index];
								if (type.ValueType != BoxType.HeapPtr)
								{
									RaiseTypeError(ref error, type, TypeKind.Class);
									goto flag_handle_error;
								}

								var obj = Context.GC.Heap[type.HeapPtr];
								if (obj.TypeKind != RtHeapTypeKind.CLASS)
								{
									RaiseTypeError(ref error, type, TypeKind.Class);
									goto flag_handle_error;
								}

								var typeclass = (ASClass)((RtPayloadScriptClass)obj.facility).Meta;
								var v = stackslots[v1.index];

								bool v1isv2 = Is(v, typeclass);

								if (v1isv2)
								{
									stackslots[dst.index] = v;
								}
								else
								{
									stackslots[dst.index].SetNull();
								}
							}
							break;
						case INS_Code.increment_decrement:
							{
								
								StackLocater dst;
								StackLocater src;
								StackLocater result;

								dst.index = dst_index;

								LoadStackLocater(&src, &PC);
								LoadStackLocater(&result, &PC);

								int addvalue = *(int*)PC; PC += 4;
								NaNBoxing n1 = stackslots[src.index];

								n1 = ToPrimitive(ref error, n1, HINT.h_number, scope_ptr, result, result, stackslots, stackStPos, thisPtr);
								if (error.raised)
								{
									goto flag_handle_error;
								}

								if(! IsNumeric(n1) ) 
								{
									ConvertValueType(ref error, n1, TypeKind.Number, Context.NUMBER, ref n1); //这里不会出错。
								}

								NaNBoxing n2 = default; n2.SetInt(addvalue);

								Exec_Add(ref error, n1, n2, dst, scope_ptr, result, stackslots, stackStPos, thisPtr);
								if (error.raised)
								{
									goto flag_handle_error;
								}

								if (dst.index != result.index)
								{
									stackslots[result.index] = n1;
								}

							}
							break;
						case INS_Code.get_instanceof:
							{
								StackLocater dst;
								StackLocater v1;
								StackLocater v2;

								dst.index = dst_index;
								LoadStackLocater(&v1, &PC);
								LoadStackLocater(&v2, &PC);


								var type = stackslots[v2.index];
								if (type.ValueType != BoxType.HeapPtr)
								{
									RaiseTypeError_InstanceOf(ref error);
									goto flag_handle_error;
								}

								int fun_proto;
								int o_proto;

								var type_instance = Context.GC.Heap[type.HeapPtr];

								if (type_instance.TypeKind == RtHeapTypeKind.CLASS)
								{
									var @typeclass = (ASClass)((RtPayloadScriptClass)type_instance.facility).Meta;
									if (typeclass.Instance.Flags.HasFlag(ClassFlags.NoConstructor) && !typeclass.Instance.IsInterface )
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
											stackslots[dst.index].SetBoolean( Is(v,typeclass) ); // 已改为按数值范围处理
											break;
										case BoxType.LocalString:
											// LocalString应该被视为String类型
											stackslots[dst.index].SetBoolean(typeclass == Context.STRING || typeclass == Context.OBJECT);
											break;
										case BoxType.HeapPtr:
											{
												var v_instance = Context.GC.Heap[v.HeapPtr];
												switch (v_instance.TypeKind)
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
															o_proto = ((RtPayloadInstance)v_instance.facility).PROTOTYPE(this,(ASInstance)v_instance.Type);
															fun_proto = ((RtPayloadScriptClass)Context.GC.Heap[typeclass.__instance_index__].facility).PROTO__PTR;
															goto lbl_do_proto;
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

																if (((RtPayloadVector)v_instance.facility).element_asclass == typeclass.Instance._element_class)
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
								else if (type_instance.TypeKind == RtHeapTypeKind.CLOSURE)
								{
									var v = stackslots[v1.index];
									if (IsPrimitive(v))
									{
										stackslots[dst_index].SetBoolean(false);
										break;
									}
#if DEBUG
									if (v.ValueType != BoxType.HeapPtr)
										throw new InvalidOperationException();
#endif
									var obj = Context.GC.Heap[v.HeapPtr];
									if (obj.TypeKind != RtHeapTypeKind.INSTANCE)
									{
										stackslots[dst_index].SetBoolean(false);
										break;
									}

									if (((ASInstance)obj.Type).Flags.HasFlag(ClassFlags.Sealed))
									{
										stackslots[dst_index].SetBoolean(false);
										break;
									}

									int obj_proto = ((RtPayloadInstance)obj.facility).PROTOTYPE(this,(ASInstance)obj.Type);

									int proto_ptr;
									if (((ASMethodBody)type_instance.Type).Method.__ismethod)
									{
										proto_ptr = ((RtPayloadScriptClass)Context.GC.Heap[Context.METHOD_CLOSURE.__instance_index__].facility).PROTO__PTR;
									}
									else
									{
										proto_ptr = ((RtPayloadClosure)type_instance.facility).PROTOTYPE(this);
										if (proto_ptr <= 0) //默认，指向FUNCTION的proto
										{
											//按test262,此处应该跑TypeError(Function has non-object prototype 'undefined' in instanceof check)
											//RaiseTypeError_InstanceOf(ref error);
											//goto flag_handle_error;
											proto_ptr = ((RtPayloadScriptClass)Context.GC.Heap[Context.FUNCTION.__instance_index__].facility).PROTO__PTR;
											if (proto_ptr <= 0) //Function.prototype是一个function (){},所以如果还是空白的，就跳到Object.proto里去。
											{
												proto_ptr = ((RtPayloadScriptClass)Context.GC.Heap[Context.OBJECT.__instance_index__].facility).PROTO__PTR;
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
									RtHeapInstance obj;

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

									stackslots[dst_index].SetBoolean(instanceof);

								}
							}
							break;
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
#if DEBUG
								if (returnSlotIndex < 0)
								{
									throw new InvalidOperationException();
								}
#endif
								
								Context.GC.CheckGC(ref error);

								StackLocater value;
								value.index = dst_index;

								var lv = LoadValue(stackslots[value.index],
									 stackStPos - method.Body._link_codescope.Members.Count - 1, ref error, stackslots, stackStPos + value.index);
								if (error.raised)
								{
									goto flag_handle_error;
								}

								stackslots[value.index] = lv;

								ref var v = ref stackslots[value.index];

								if (method.Flags.HasFlag(MethodFlags.ASYNC))
								{
								}
								else
								{
									ConvertValueType(ref error, v, method.ReturnTypeKind, method.__return_type_class__, ref v);
									if (error.raised)
									{
										goto flag_handle_error;
									}
								}

								//v = GetSaveValue(v, ref error);/* 这里应该改造为返回缓存 cache_object.  */
								//if (error.raised)
								//{
								//	goto flag_handle_error;
								//}

								if (v.ValueType == BoxType.HeapPtr)
								{
									StoreReturnSlot(ref Context.StackSlots[returnSlotIndex], stackStPos, returnSlotIndex, calleelastPos, scope_ptr, v, ref error);
									if (error.raised)
									{
										Context.StackSlots[returnSlotIndex].SetUndefined();
										goto flag_handle_error;
									}
								}
								else
								{
									Context.StackSlots[returnSlotIndex] = v;
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

//						case INS_Code.return_async_promise:
//							{
//								InitScript((ASScript)Context.PROMISE._link_codescope.Parent.Container, ref error);
//								if (error.raised)
//								{
//									goto flag_handle_error;
//								}
//#if DEBUG
//								if (returnSlotIndex < 0)
//								{
//									throw new InvalidOperationException();
//								}
//#endif

//								if (Context.StackPosition + 1 >= Context.STACK_LENGTH)
//								{
//									RaiseStackOverflow(ref error);
//									goto flag_handle_error;
//								}

//								int basePos = Context.StackPosition;

//								Context.GC.CheckGC(ref error);

//								StackLocater value;
//								value.index = dst_index;

//								var lv = LoadValue(stackslots[value.index],
//									 stackStPos - method.Body._link_codescope.Members.Count - 1, ref error, stackslots, stackStPos + value.index);
//								if (error.raised)
//								{
//									goto flag_handle_error;
//								}

//								var m = Context.PROMISE._vtable.Items[1].Trait.Method;

//								var argSpan = Context.StackSlots.AsSpan(Context.StackPosition, 1);
//								argSpan[0] = lv;
//								StackLocater argLoc = new StackLocater() { index = 0 };

//								NaNBoxing _this = default;
//								_this.SetHeapPtr( Context.PROMISE.__instance_index__ );

//								Context.StackPosition++;

//								RunMethod(m, _this, scope_ptr, Context.PROMISE, 1, (byte*)&argLoc, argSpan, ref error, returnSlotIndex, calleelastPos);

//								Context.StackPosition = basePos;

//								if (error.raised)
//								{
//									Context.StackSlots[returnSlotIndex].SetUndefined();
//									goto flag_handle_error;
//								}

//								if (exception_ctx != NO_TRY)
//								{
//									stackslots[exception_ctx->hold_error.index].setFault();//return 会吃掉异常

//									ExceptionContext* ctx = NO_TRY + 1;
//									ctx->FINALLY_JUMPTO_PTR = PC_END;
//									do
//									{
//										var finally_p = ctx->state == 2 ? ctx->FINALLY_EXIT_PTR : ctx->FINALLY_PTR;
//										++ctx;
//										ctx->FINALLY_JUMPTO_PTR = finally_p;

//									} while (ctx < exception_ctx);

//									PC = exception_ctx->state == 2 ? exception_ctx->FINALLY_EXIT_PTR : exception_ctx->FINALLY_PTR;

//									break;
//								}
//								else
//								{
//#if PROFILEPLAYER
//									InstructionProfiler.Profile_ActionEnd(opcode);
//#endif
//									goto flag_end;
//								}


//							}
							
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

									PC = exception_ctx->state == 2 ? exception_ctx->FINALLY_EXIT_PTR :  exception_ctx->FINALLY_PTR;
									
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
//								ConvertValueType(ref error, v, TypeKind.Boolean, Context.BOOLEAN, ref v);
//#if DEBUG
//								if (error.raised) throw new InvalidOperationException();
//								if (v.ValueType != BoxType.Boolean) throw new InvalidOperationException();

//#endif

								if (! ToBoolean( v))
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
								if (ToBoolean( v))
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

								StackLocater instance;
								LoadStackLocater(&instance, &PC);

								int index; LoadInt32(&index, &PC);

								var arr = stackslots[instance.index];
								Debug.Assert(arr.ValueType == BoxType.HeapPtr);
								Debug.Assert(Context.GC.Heap[arr.HeapPtr].TypeKind == RtHeapTypeKind.VECTOR || Context.GC.Heap[arr.HeapPtr].TypeKind == RtHeapTypeKind.ARRAY);

								var obj = Context.GC.Heap[arr.HeapPtr];
								if (obj.TypeKind == RtHeapTypeKind.ARRAY)
								{
									var arr_payload = (RtPayloadArray)Context.GC.Heap[arr.HeapPtr].facility;
									if (arr_payload.StoreMode != RtPayloadArray.ArrayStoreMode.normal)
									{
										int heaparr = arr_payload.ChangeStoreToHeap(Context.player, ref error);
										if (error.raised)
										{
											goto flag_handle_error;
										}
										stackslots[instance.index].SetHeapPtr(heaparr);
									}

									SetArraySlot(stackslots[dst_index], (uint)index, Context.GC.Heap[arr.HeapPtr], ref error);
									if (error.raised)
									{
										goto flag_handle_error;
									}
								}
								else
								{ 
									var vec_payload = (RtPayloadVector)Context.GC.Heap[arr.HeapPtr].facility;

									ConvertValueType(ref error, stackslots[dst_index], vec_payload.element_type, vec_payload.element_asclass, ref stackslots[dst_index]);
									if (error.raised)
									{
										goto flag_handle_error;
									}
									vec_payload.SetSlot(index, this, arr.HeapPtr, stackslots[dst_index], ref error);
									if (error.raised)
									{
										goto flag_handle_error;
									}

								}
							}

							break;
						//case INS_Code.op_stack_Variable_ldconst:
						//	{
						//		uint store;LoadUInt(&store, &PC);

						//		byte const_index = (byte)((uint)store & 0xff) ;
						//		byte heap_member = (byte)((uint)store >> 8 & 0xff);
						//		OpMode mode = (OpMode)((uint)store >> 16 & 0xff);
						//		byte tempLoc = (byte)((uint)store >> 24 & 0xff);



						//		var v1 = ((RtPayloadMethodScope)methodscope.facility).ReadSlot(heap_member, this);
						//		var v2 = constants[const_index];


						//		//ref NaNBoxing dst_v = ref stackslots[dst_index];

						//		switch (mode)
						//		{
						//			case OpMode.strict_eq:
						//				stackslots[dst_index].SetBoolean( IsStrictlyEqual(v1,v2) );
						//				break;
						//			case OpMode.sub:
						//				Exec_Sub(ref error, v1, v2, new StackLocater() { index = dst_index }, scope_ptr, new StackLocater() { index = tempLoc }, stackslots, stackStPos, thisPtr);
						//				if (error.raised)
						//				{
						//					goto flag_handle_error;
						//				}
						//				break;
						//			default:
						//				throw new NotImplementedException();
						//		}



						//	}
						//	break;
						//case INS_Code.if_logicOp_goto:
						//	{
						//		int offset;LoadInt32(&offset, &PC);
						//		uint store; LoadUInt(&store, &PC);

						//		var compMode =(CompMode)( store & 0xff);
						//		bool jump_mode = (store >> 8 & 0xff) > 0;
						//		byte v1_idx = (byte)(store >> 16 & 0xff);
						//		byte v2_idx = (byte)(store >> 24 & 0xff);


						//		NaNBoxing v1 = stackslots[v1_idx];
						//		NaNBoxing v2 = stackslots[v2_idx];

						//		bool result;

						//		switch (compMode)
						//		{
						//			case CompMode.strict_equal:
						//				result = IsStrictlyEqual(v1, v2);
						//				break;
						//			default:
						//				throw new NotImplementedException();
						//		}

						//		if (jump_mode)
						//		{
						//			if (result)
						//			{
						//				PC = PC_START + offset;
						//			}
						//		}
						//		else
						//		{
						//			if (!result)
						//			{
						//				PC = PC_START + offset;
						//			}
						//		}

						//	}
						//	break;

						//						case INS_Code.return_op:
						//							{

						//								INS_Return_Oper.OperMode operMode = (INS_Return_Oper.OperMode)(dst_index & 0xff);

						//								switch (operMode)
						//								{
						//									case INS_Return_Oper.OperMode.ld_const:
						//										{
						//											NaNBoxing v = constants[(int)((uint)dst_index>>8 & 0xff)];

						//											ref NaNBoxing result = ref Context.StackSlots[returnSlotIndex];
						//											ConvertValueType(ref error, v, method.ReturnTypeKind, method.__return_type_class__, ref result);
						//											if (error.raised)
						//											{
						//												goto flag_handle_error;
						//											}

						//											//常量池的对象，肯定不需要检查
						//										}

						//										break;
						//									case INS_Return_Oper.OperMode.add_stack_stack:
						//										{

						//											var n1 = stackslots[(int)((uint)dst_index >> 16 & 0xff)];
						//											var n2 = stackslots[(int)((uint)dst_index >> 8 & 0xff)];

						//											if (Context.StackPosition + 2 >= Context.STACK_LENGTH)
						//											{
						//												RaiseStackOverflow(ref error);
						//												goto flag_handle_error;
						//											}

						//											StackLocater dst = new StackLocater();dst.index = stackslots.Length;
						//											StackLocater tmp = new StackLocater(); tmp.index = stackslots.Length + 1;

						//											var exec_span = Context.StackSlots.AsSpan(stackStPos, stackslots.Length + 2);
						//											Context.StackPosition += 2;
						//											Exec_Add(ref error, n1, n2,dst, scope_ptr,tmp, exec_span , stackStPos, thisPtr);								
						//											if (error.raised)
						//											{
						//												Context.StackPosition -= 2;
						//												goto flag_handle_error;
						//											}

						//											NaNBoxing v = exec_span[dst.index];

						//											if (v.ValueType == BoxType.HeapPtr)
						//											{
						//												StoreReturnSlot(ref Context.StackSlots[returnSlotIndex], stackStPos, returnSlotIndex, calleelastPos, scope_ptr, v, ref error);
						//												if (error.raised)
						//												{
						//													Context.StackPosition -= 2;
						//													Context.StackSlots[returnSlotIndex].SetUndefined();
						//													goto flag_handle_error;
						//												}
						//											}
						//											else
						//											{
						//												Context.StackSlots[returnSlotIndex] = v;
						//											}

						//											Context.StackPosition -= 2;

						//										}
						//										break;
						//									default:
						//										throw new InvalidOperationException();
						//								}

						//								if (exception_ctx != NO_TRY)
						//								{
						//									ExceptionContext* ctx = NO_TRY + 1;
						//									ctx->FINALLY_JUMPTO_PTR = PC_END;
						//									do
						//									{
						//										var finally_p = ctx->FINALLY_PTR;
						//										++ctx;
						//										ctx->FINALLY_JUMPTO_PTR = finally_p;

						//									} while (ctx < exception_ctx);

						//									PC = exception_ctx->FINALLY_PTR;
						//									break;
						//								}
						//								else
						//								{
						//#if PROFILEPLAYER
						//									InstructionProfiler.Profile_ActionEnd(opcode);
						//#endif
						//									goto flag_end;
						//								}


						//							}
						//							break;
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

									PC =  exception_ctx->FINALLY_PTR;

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
#if DEBUG
								if (returnSlotIndex < 0)
								{
									throw new InvalidOperationException();
								}
#endif
#if FORCOMPILER
								if (IsComputeConstExpr)
								{
									throw new EvalConstException();
								}
#endif

								Context.GC.CheckGC(ref error);

								StackLocater value;
								value.index = dst_index;

								var lv = LoadValue(stackslots[value.index],
									 stackStPos - method.Body._link_codescope.Members.Count - 1, ref error, stackslots, stackStPos + value.index);
								if (error.raised)
								{
									//如果有异常，那就不会保存上下文
									goto flag_handle_error;
								}

								if (lv.ValueType == BoxType.HeapPtr)
								{
									StoreReturnSlot(ref Context.StackSlots[returnSlotIndex], stackStPos, returnSlotIndex, calleelastPos, scope_ptr, lv, ref error,true);
									if (error.raised)
									{
										Context.StackSlots[returnSlotIndex].SetUndefined();
										goto flag_handle_error;
									}
								}
								else
								{
									Context.StackSlots[returnSlotIndex] = lv;
								}


								//保存上下文状态
								int exception_ctx_count = (method.Flags.HasFlag(MethodFlags.NoTry) ? 0 : Context.MAX_TRY_NESTED) + 2;
								int exception_at = (int)(exception_ctx - exception_ctx_stack);

								GeneratorImpl.GeneratorWapper generatorWapper = (GeneratorImpl.GeneratorWapper)resume_state;
								generatorWapper.exception_ctx_at = exception_at;
								if (exception_ctx_count > 0)
								{
									for (int i = 1; i < exception_at+1; i++)
									{
										generatorWapper.exceptionContext[i] = *(NO_TRY + i);
#if DEBUG
										if (stackslots[generatorWapper.exceptionContext[i].hold_error.index].ValueType != BoxType.Fault)
										{
											//yield禁止在catch块内使用，所以不可能有hold的异常。
											throw new InvalidOperationException();
										}
#endif
									}
								}

								generatorWapper.state = 1;
								generatorWapper.RESUME_PC = (int)(PC - PC_START);

								PC_PTR = generatorWapper.RESUME_PC;
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
#if DEBUG
								if (returnSlotIndex < 0)
								{
									throw new InvalidOperationException();
								}
#endif


								Context.GC.CheckGC(ref error);

								StackLocater value;
								value.index = dst_index;

								var lv = LoadValue(stackslots[value.index],
									 stackStPos - method.Body._link_codescope.Members.Count - 1, ref error, stackslots, stackStPos + value.index);
								if (error.raised)
								{
									//如果有异常，那就不会保存上下文
									goto flag_handle_error;
								}

								if (lv.ValueType == BoxType.HeapPtr)
								{
									StoreReturnSlot(ref Context.StackSlots[returnSlotIndex], stackStPos, returnSlotIndex, calleelastPos, scope_ptr, lv, ref error, true);
									if (error.raised)
									{
										Context.StackSlots[returnSlotIndex].SetUndefined();
										goto flag_handle_error;
									}
								}
								else
								{
									Context.StackSlots[returnSlotIndex] = lv;
								}


								//保存上下文状态
								int exception_ctx_count = (method.Flags.HasFlag(MethodFlags.NoTry) ? 0 : Context.MAX_TRY_NESTED) + 2;
								int exception_at = (int)(exception_ctx - exception_ctx_stack);

								PromiseImpl.AsyncGenWapper asyncGenWapper = (PromiseImpl.AsyncGenWapper)resume_state;
								asyncGenWapper.exception_ctx_at = exception_at;
								if (exception_ctx_count > 0)
								{
									for (int i = 1; i < exception_at + 1; i++)
									{
										asyncGenWapper.exceptionContext[i] = *(NO_TRY + i);
#if DEBUG
										if (stackslots[asyncGenWapper.exceptionContext[i].hold_error.index].ValueType != BoxType.Fault)
										{
											//await禁止在finally块内使用，所以不可能有hold的异常。
											throw new InvalidOperationException();
										}
#endif
									}
								}

								asyncGenWapper.state = 1;
								asyncGenWapper.RESUME_PC = (int)(PC - PC_START);

								PC_PTR = asyncGenWapper.RESUME_PC;
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
								InitScript((ASScript)Context.IITERATOR.Instance._vtable.Items[0].Trait.Method.Body._link_codescope.Members[1].__rt_type_class__._link_codescope.Parent.Container, ref error);
								if (error.raised)
								{
									goto flag_handle_error;
								}

								// 从 dst_index 中解码 iterContextVar（复用存储空间）
								ScopeHeapLocater iterContextVar;
								iterContextVar.ScopeIndex = (ushort)(dst_index >> 16);
								iterContextVar.MemberIndex = (ushort)(dst_index & 0xFFFF);

								//if (Context.StackPosition + 1 >= Context.STACK_LENGTH)
								//{
								//	RaiseStackOverflow(ref error);
								//	goto flag_handle_error;
								//}

								RtHeapInstance iterctx;
								int iter_context_ptr = Context.GC.RentIterContext(out iterctx);
								if (iter_context_ptr == 0)
								{
									RaiseOutOfMemory(ref error);
									goto flag_handle_error;
								}

								//Context.StackPosition++; //执行iter.next时，保留给它当返回值槽用

								((IterContxt)((RtPayloadInstance)iterctx.facility).wapperedObject).PC = PC;
								
								// 将迭代器上下文存储到方法变量中
								RtPayloadMethodScope heap = (RtPayloadMethodScope)methodscope.facility;
#if DEBUG
								if (methodscope.Type._link_codescope.index != iterContextVar.ScopeIndex)
									throw new InvalidOperationException();
#endif
								NaNBoxing iterCtxValue = default;
								iterCtxValue.SetHeapPtr(iter_context_ptr);
								heap.SetSlot(iterCtxValue, iterContextVar.MemberIndex);
							}
							break;
						case INS_Code.iter_get:
							{
								StackLocater iterSrcLoc;
								//StackLocater iteratorLoc;
								//StackLocater iter_contextLoc;

								ScopeHeapLocater iterSrcObj_Holder;
								{
									iterSrcObj_Holder.ScopeIndex = *(ushort*)PC; PC += 2;
									iterSrcObj_Holder.MemberIndex = *(ushort*)PC; PC += 2;
								}

								int flag_end_id;
								int flag_offset;

								//iteratorLoc.index = dst_index;
								LoadStackLocater(&iterSrcLoc, &PC);
								


								LoadInt32(&flag_end_id, &PC);
								LoadInt32(&flag_offset, &PC);

								ScopeHeapLocater iterVar;
								{
									iterVar.ScopeIndex = (ushort)(dst_index >> 16);
									iterVar.MemberIndex = (ushort)(dst_index & 0xFFFF);
								}


								var ins = stackslots[iterSrcLoc.index];

								if (ins.ValueType == BoxType.HeapPtr)
								{
									if (Context.StackPosition + 1 >= Context.STACK_LENGTH)
									{
										RaiseStackOverflow(ref error);
										goto flag_handle_error;
									}


									int* m_scope = method_scopes;
									*m_scope++ = scope_ptr;

									RtPayloadMethodScope heap = (RtPayloadMethodScope)methodscope.facility;
#if DEBUG
									if (methodscope.Type._link_codescope.index != iterSrcObj_Holder.ScopeIndex)
									{
										throw new InvalidOperationException();
									}

									if (methodscope.Type._link_codescope.index != iterVar.ScopeIndex)
									{
										throw new InvalidOperationException();
									}

#endif


									PrepareSaveMethodScope(heap, ref iterSrcObj_Holder, ref ins, m_scope, method_scopes, ref error);
									if (error.raised)
									{
										Context.GC.ReturnIterContextWhenGetIterFailed();
										goto flag_handle_error;
									}								
									heap.SetSlot( ins, iterSrcObj_Holder.MemberIndex); //Context.GC.Heap[ins.HeapPtr];

									var obj = Context.GC.Heap[ins.HeapPtr];
									int iter_slot = Context.StackPosition;

									if (obj.TypeKind == RtHeapTypeKind.INSTANCE)
									{
										
										var type = (ASInstance)obj.Type;

										if (type == Context.GENERATOR.Instance)
										{
											heap.SetSlot(ins, iterVar.MemberIndex);
										}
										else if (type.iterator == null)
										{

											var obj_iter = Context.IITERATOR._link_codescope.Parent.Container.Traits[1].Class;
											InitCacheInstance(obj_iter, iter_slot , false);

											PrepareSaveMethodScope(heap, ref iterVar, ref Context.StackSlots[iter_slot] , m_scope, method_scopes, ref error);
											if (error.raised)
											{
												Context.GC.ReturnIterContextWhenGetIterFailed();
												goto flag_handle_error;
											}
											heap.SetSlot(Context.StackSlots[iter_slot],iterVar.MemberIndex);

											var iter = (RtPayloadInstance)Context.GC.Heap[Context.StackSlots[iter_slot].HeapPtr].facility;

											NaNBoxing index = default; index.SetInt(0);
											NaNBoxing count = default; count.SetInt(0);


											iter.SetSlot(index, 0, obj_iter.Instance._link_codescope, this);
											iter.SetSlot(count, 1, obj_iter.Instance._link_codescope, this);

											//throw new NotImplementedException();

										}
										else
										{
											
											Context.StackPosition++;
											RunMethod(type.iterator, ins, scope_ptr, type, 0, null, null, ref error, iter_slot);
											Context.StackPosition--;

											if (error.raised)
											{
												Context.GC.ReturnIterContextWhenGetIterFailed();
												goto flag_handle_error;
											}


											if (Context.StackSlots[iter_slot].ValueType != BoxType.HeapPtr) //return null?
											{
												Context.GC.ReturnIterContextWhenGetIterFailed();
												PC = PC_START + flag_offset;
											}
											else
											{
#if DEBUG

												var iter_instance = Context.GC.Heap[Context.StackSlots[iter_slot].HeapPtr];
												if (!Extensions.IsImplements((ASInstance)iter_instance.Type, Context.IITERATOR.Instance))
												{
													throw new InvalidOperationException();
												}
#endif

												PrepareSaveMethodScope(heap, ref iterVar, ref Context.StackSlots[iter_slot], m_scope, method_scopes, ref error);
												if (error.raised)
												{
													Context.GC.ReturnIterContextWhenGetIterFailed();
													goto flag_handle_error;
												}
												heap.SetSlot(Context.StackSlots[iter_slot], iterVar.MemberIndex);


											}
										}
									}
									else if (obj.TypeKind == RtHeapTypeKind.GLOBAL || obj.TypeKind == RtHeapTypeKind.CLASS || obj.TypeKind == RtHeapTypeKind.CLOSURE
										||
										obj.TypeKind == RtHeapTypeKind.ARRAY
										||
										obj.TypeKind == RtHeapTypeKind.VECTOR
										)
									{
										var obj_iter = Context.IITERATOR._link_codescope.Parent.Container.Traits[1].Class;
										InitCacheInstance(obj_iter, iter_slot , false);

										PrepareSaveMethodScope(heap, ref iterVar, ref Context.StackSlots[iter_slot], m_scope, method_scopes, ref error);
										if (error.raised)
										{
											Context.GC.ReturnIterContextWhenGetIterFailed();
											goto flag_handle_error;
										}
										heap.SetSlot(Context.StackSlots[iter_slot], iterVar.MemberIndex);

										var iter = (RtPayloadInstance)Context.GC.Heap[Context.StackSlots[iter_slot].HeapPtr].facility;


										NaNBoxing index = default; index.SetInt(0);
										NaNBoxing count = default; count.SetInt(0);


										iter.SetSlot(index, 0, obj_iter.Instance._link_codescope, this);
										iter.SetSlot(count, 1, obj_iter.Instance._link_codescope, this);

									}
									else
									{
#if DEBUG
										throw new InvalidOperationException();
#else
										Environment.FailFast("出错了，这里跑不到"); PC_PTR = 0; return;
#endif
									}




								}
								else
								{

									Context.GC.ReturnIterContextWhenGetIterFailed(); //需要返回
									PC = PC_START + flag_offset;
								}
								break;
							}
						case INS_Code.iter_next:
							{
								int mode;

								ScopeHeapLocater iterVar;
								
								StackLocater resultLoc;
								
								int flag_next_end_id;
								int flag_offset;

								LoadInt32(&mode, &PC);

								{
									iterVar.ScopeIndex = *(ushort*)PC; PC += 2;
									iterVar.MemberIndex = *(ushort*)PC; PC += 2;
								}

								LoadStackLocater(&resultLoc, &PC);
								
								LoadInt32(&flag_next_end_id, &PC);
								LoadInt32(&flag_offset, &PC);

								ScopeHeapLocater iterSrcObjSaveInVar;
								{
									iterSrcObjSaveInVar.ScopeIndex = (ushort)(dst_index >> 16);
									iterSrcObjSaveInVar.MemberIndex = (ushort)(dst_index & 0xFFFF);
								}


								RtPayloadMethodScope heap = (RtPayloadMethodScope)methodscope.facility;

#if DEBUG
								if (methodscope.Type._link_codescope.index != iterSrcObjSaveInVar.ScopeIndex)
								{
									throw new InvalidOperationException();
								}
#endif
								var obj_h = heap.ReadSlot( iterSrcObjSaveInVar.MemberIndex , this);
#if DEBUG
								
								if (obj_h.ValueType != BoxType.HeapPtr)
									throw new InvalidOperationException();

								if (methodscope.Type._link_codescope.index != iterVar.ScopeIndex)
								{
									throw new InvalidOperationException();
								}

#endif
								NaNBoxing iter_v = heap.ReadSlot(iterVar.MemberIndex, this);


								var obj = Context.GC.Heap[obj_h.HeapPtr];
								var iter = Context.GC.Heap[iter_v.HeapPtr];
								

#if DEBUG
								if (Context.IITERATOR.Instance._vtable.Items[0].Trait.QName.Name != "next")
								{
									throw new InvalidOperationException();
								}

#endif
								//int cache_slot_index = Context.StackPosition - 1;

								var resulttype = Context.IITERATOR.Instance._vtable.Items[0].Trait.Method.Body._link_codescope.Members[1].__rt_type_class__;
								int result_ptr = InitCacheInstance(resulttype, stackStPos + resultLoc.index,true);
								RtHeapInstance result = Context.GC.Heap[stackslots[resultLoc.index].HeapPtr];


								int m_idx = 
									iter.Type == Context.GENERATOR.Instance ? 0 :
									((ASInstance)iter.Type)._interface_impl_.First((i) => i.interface_type == Context.IITERATOR.Type_identifier)[0];
								var vtableitem = iter.Type._vtable.Items[m_idx];
								var function = vtableitem.Trait.Method;


								if (Context.StackPosition + 3 >= Context.STACK_LENGTH)
								{
									RaiseStackOverflow(ref error);
									goto flag_handle_error;
								}

								var argSpan = Context.StackSlots.AsSpan(Context.StackPosition, 2);
								Context.StackPosition += 3;

								int reseveSlot = Context.StackPosition - 1;

								Context.StackSlots[reseveSlot].SetUndefined();

								argSpan[0] = obj_h; //stackslots[insLoc.index]; //obj
								argSpan[1] = stackslots[resultLoc.index];//result
								

								tmpArgLoc[0].index = 0;
								tmpArgLoc[1].index = 1;

								RunMethod(function, iter_v, iter_v.HeapPtr, iter.Type, 2, (byte*)tmpArgLoc, argSpan, ref error,reseveSlot );

								
								if (error.raised)
								{
									Context.StackPosition -= 3;
									goto flag_handle_error;
								}

								RtPayloadInstance result_payload = (RtPayloadInstance)result.facility;
								var done = result_payload.ReadSlot(0, result.Type._link_codescope, this);
#if DEBUG
								if (done.ValueType != BoxType.Boolean) throw new InvalidOperationException();
#endif

								if (done.Boolean)
								{
									PC = PC_START + flag_offset;
								}
								else
								{
									if (mode == 0)
									{
										var key = result_payload.ReadSlot(1, result.Type._link_codescope, this);
										//检查这里是否是一个struct!如果是，需要从Context.StackPosition-1槽里复制到stackslots[resultLoc.index]里!
										if (key.ValueType == BoxType.HeapPtr)
										{
											var check = Context.GC.Heap[key.HeapPtr];
											if (check.TypeKind == RtHeapTypeKind.INSTANCE && ((ASInstance)check.Type).Flags.HasFlag(ClassFlags.Struct))
											{
												Debug.Assert(reseveSlot != stackStPos + resultLoc.index);
												//clone结构体
												int clonedptr = stackStPos + resultLoc.index + Context.CacheInstancePtr;
												var cacheObj = Context.GC.Heap[clonedptr];
												cacheObj.Type = check.Type;

												((RtPayloadInstance)cacheObj.facility).methodscopeslot_ref_state = 0;
												((RtPayloadInstance)cacheObj.facility).HEAPINSTANCE_PTR = 0;
												((RtPayloadInstance)cacheObj.facility).CopyFrom(check, this, check.Type._link_codescope.TypeLayout.Size);

												key.SetHeapPtr(clonedptr);

											}
										}



										stackslots[resultLoc.index] = key;
									}
									else
									{
										var value = result_payload.ReadSlot(2, result.Type._link_codescope, this);
										//检查这里是否是一个struct!如果是，需要从Context.StackPosition-1槽里复制到stackslots[resultLoc.index]里!
										if (value.ValueType == BoxType.HeapPtr)
										{
											var check = Context.GC.Heap[value.HeapPtr];
											if (check.TypeKind == RtHeapTypeKind.INSTANCE && ((ASInstance)check.Type).Flags.HasFlag( ClassFlags.Struct  ))
											{
												//clone结构体
												int clonedptr = stackStPos + resultLoc.index + Context.CacheInstancePtr;
												var cacheObj = Context.GC.Heap[clonedptr];
												cacheObj.Type = check.Type;

												((RtPayloadInstance)cacheObj.facility).methodscopeslot_ref_state = 0;
												((RtPayloadInstance)cacheObj.facility).HEAPINSTANCE_PTR = 0;
												((RtPayloadInstance)cacheObj.facility).CopyFrom(check, this, check.Type._link_codescope.TypeLayout.Size);

												value.SetHeapPtr(clonedptr);

											}
										}

										stackslots[resultLoc.index] = value;

									}
								}


								Context.StackPosition -= 3; //将可能从Vector中读取的struct保留到拷贝之后

								break;
							}
						case INS_Code.iter_close:
							{
								StackLocater insLoc;
								ScopeHeapLocater iterVar;
								ScopeHeapLocater iterContextVar;

								insLoc.index = dst_index;
								ScopeHeapLocater holderLoc;
								{
									holderLoc.ScopeIndex = *(ushort*)PC; PC += 2;
									holderLoc.MemberIndex = *(ushort*)PC; PC += 2;
								}

								{
									iterVar.ScopeIndex = *(ushort*)PC; PC += 2;
									iterVar.MemberIndex = *(ushort*)PC; PC += 2;
								}

								{
									iterContextVar.ScopeIndex = *(ushort*)PC; PC += 2;
									iterContextVar.MemberIndex = *(ushort*)PC; PC += 2;
								}

								RtPayloadMethodScope heap = (RtPayloadMethodScope)methodscope.facility;

#if DEBUG
								if (methodscope.Type._link_codescope.index != holderLoc.ScopeIndex)
									throw new InvalidOperationException();
								if (methodscope.Type._link_codescope.index != iterContextVar.ScopeIndex)
									throw new InvalidOperationException();
								if (methodscope.Type._link_codescope.index != iterVar.ScopeIndex)
									throw new InvalidOperationException();

#endif
								var obj_h = heap.ReadSlot(holderLoc.MemberIndex, this);
								// 从方法变量读取迭代器上下文
								var iter_ctx_value = heap.ReadSlot(iterContextVar.MemberIndex, this);
								// 读iter对象
								var iter_v = heap.ReadSlot(iterVar.MemberIndex, this);

#if DEBUG
								if (obj_h.ValueType != BoxType.HeapPtr)
									throw new InvalidOperationException();
								if (iter_v.ValueType != BoxType.HeapPtr)
									throw new InvalidOperationException();
								if (iter_ctx_value.ValueType != BoxType.HeapPtr)
									throw new InvalidOperationException();
#endif
#if DEBUG
								if (Context.IITERATOR.Instance._vtable.Items[1].Trait.QName.Name != "close")
								{
									throw new InvalidOperationException();
								}
#endif

								var obj = Context.GC.Heap[obj_h.HeapPtr];
								var iter = Context.GC.Heap[iter_v.HeapPtr];
								var iter_ctx = Context.GC.Heap[iter_ctx_value.HeapPtr];

								stackslots[insLoc.index] = obj_h;


								int m_idx = iter.Type == Context.GENERATOR.Instance ? 1:									
									((ASInstance)iter.Type)._interface_impl_.First((i) => i.interface_type == Context.IITERATOR.Type_identifier)[1];
								var vtableitem = iter.Type._vtable.Items[m_idx];
								var function = vtableitem.Trait.Method;


								RunMethod(function, iter_v, iter_v.HeapPtr, iter.Type,
									 1, (byte*)&insLoc, stackslots, ref error, -1
									);

								if (error.raised)
								{
									//Context.StackPosition--;//在获取Context时，保留了一个槽位
									Context.GC.ReturnIterContext(iter_ctx);
									// 清空方法变量中的迭代器上下文
									NaNBoxing undefined = default;
									undefined.SetUndefined();
									heap.SetSlot(undefined, iterContextVar.MemberIndex);
									goto flag_handle_error;
								}

								NaNBoxing load_error = stackslots[exception_ctx->hold_error.index];
								if (load_error.ValueType != BoxType.Fault)
								{
									
									//说明有异常存在，中止访问proto
									Context.GC.ReturnIterContext(iter_ctx);
									// 清空方法变量中的迭代器上下文
									NaNBoxing undefined = default;
									undefined.SetUndefined();
									heap.SetSlot(undefined, iterContextVar.MemberIndex);
								}
								else if (obj.Type == Context.GENERATOR.Instance) //结束
								{
									Context.GC.ReturnIterContext(iter_ctx); 
									// 清空方法变量中的迭代器上下文
									NaNBoxing undefined = default;
									undefined.SetUndefined();
									heap.SetSlot(undefined, iterContextVar.MemberIndex);
								}
								else
								{
									var proto = GetProtoPtr(obj);
									var iter_ctx_wapper = (IterContxt)((RtPayloadInstance)iter_ctx.facility).wapperedObject;

									iter_ctx_wapper.visitedObjs.Add(obj_h.HeapPtr);

									if (iter_ctx_wapper.visitedObjs.Contains(proto))
									{
										//环只有function才可能产生，所以如果出现就跳到Function的proto里去
										proto = ((RtPayloadScriptClass)Context.GC.Heap[Context.FUNCTION.__instance_index__].facility).PROTO__PTR;
										if (iter_ctx_wapper.visitedObjs.Contains(proto))
										{
											//循环访问Function.prototype 跳到Object.prototype.
											proto = ((RtPayloadScriptClass)Context.GC.Heap[Context.OBJECT.__instance_index__].facility).PROTO__PTR;
										}
									}

#if DEBUG
									if (iter_ctx_wapper.visitedObjs.Contains(proto))
										throw new InvalidOperationException();
#endif

									if (proto != 0)
									{
										if (exception_ctx->FINALLY_JUMPTO_PTR == null) //如果不为空，说明有迭代过程中出现了跳转到外部的情况
										{
											stackslots[insLoc.index].SetHeapPtr(proto);
											//跳回get_iter,访问_proto_.
											exception_ctx->FINALLY_JUMPTO_PTR = iter_ctx_wapper.PC;
										}
										else
										{
											//Context.StackPosition--;//在获取Context时，保留了一个槽位
											Context.GC.ReturnIterContext(iter_ctx);
											// 清空方法变量中的迭代器上下文
											NaNBoxing undefined = default;
											undefined.SetUndefined();
											heap.SetSlot(undefined, iterContextVar.MemberIndex);
										}
									}
									else
									{
										//Context.StackPosition--;//在获取Context时，保留了一个槽位
										Context.GC.ReturnIterContext(iter_ctx);
										// 清空方法变量中的迭代器上下文
										NaNBoxing undefined = default;
										undefined.SetUndefined();
										heap.SetSlot(undefined, iterContextVar.MemberIndex);
									}
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
								//								RtPayloadMethodScope heap = (RtPayloadMethodScope)s.facility;
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
										match = (error.error.ValueType == NaNBoxing.BoxType.HeapPtr && Context.GC.Heap[error.error.HeapPtr].TypeKind == RtHeapTypeKind.CLASS);
										break;
									case TypeKind.String:
										match = (error.error.ValueType == NaNBoxing.BoxType.HeapPtr && Context.GC.Heap[error.error.HeapPtr].TypeKind == RtHeapTypeKind.STRING);
										break;
									case TypeKind.Function:
										match = (error.error.ValueType == NaNBoxing.BoxType.HeapPtr && Context.GC.Heap[error.error.HeapPtr].TypeKind == RtHeapTypeKind.CLOSURE);
										break;
									case TypeKind.Array:
										match = (error.error.ValueType == NaNBoxing.BoxType.HeapPtr && Context.GC.Heap[error.error.HeapPtr].TypeKind == RtHeapTypeKind.ARRAY);
										break;
									case TypeKind.Vector:
										match = (error.error.ValueType == NaNBoxing.BoxType.HeapPtr && Context.GC.Heap[error.error.HeapPtr].TypeKind == RtHeapTypeKind.VECTOR);
										break;
									case TypeKind.Namespace:
										match = (error.error.ValueType == NaNBoxing.BoxType.HeapPtr && Context.GC.Heap[error.error.HeapPtr].TypeKind == RtHeapTypeKind.NAMESPACE);
										break;
									default:
										var check_type = t.__rt_type_class__; //Context.dictTypes[(ulong)c_type];
										if (error.error.ValueType == NaNBoxing.BoxType.HeapPtr)
										{
											var obj = Context.GC.Heap[error.error.HeapPtr];
											if (obj.TypeKind == RtHeapTypeKind.INSTANCE) //只有对象实例才可能满足条件。
											{
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
									RtPayloadMethodScope heap = (RtPayloadMethodScope)s.facility;
									NaNBoxing value = default;

									ReceiveError store_err = default;
									ConvertValueType(ref store_err, error.error, t.TypeKind, t.__rt_type_class__, ref value);
#if DEBUG
									if (store_err.raised)
									{
										throw new InvalidOperationException(); // 这里的类型转换是不会失败的
									}
#endif
									PrepareSaveMethodScope(heap, ref heapLocater, ref value, m_scope, method_scopes, ref store_err);
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
								StoreReturnSlot(ref stackslots[exception_ctx->hold_error.index], stackStPos, stackStPos + exception_ctx->hold_error.index, calleelastPos, scope_ptr, error.error, ref error,true);
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
								StoreReturnSlot(ref stackslots[exception_ctx->hold_error.index], stackStPos, stackStPos + exception_ctx->hold_error.index, calleelastPos, scope_ptr, error.error, ref error,true);
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
