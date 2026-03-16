using juicescript.ABC;
using juicescript.ABC.INS;
using juicescript.ABC.Locaters;
using juicescript.compiler.AST;
using juicescript.compiler.AST.Stmt;
using juicescript.compiler.IL;
using juicescript.compiler.IL.Optimize;
using juicescript.compiler.parse;
using juicescript.runtime;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Formats.Asn1.AsnWriter;

namespace juicescript.compiler
{
	public class MethodResolver
	{
		internal static int BuildMethod(CompileContext context, string workDir, List<string> libs, bool force_rebuild_bcode, string outswcfile)
		{
			var lex = new Lex(null);
			var tokens = lex.GetWords(AS3_LL1_GRAMMAR.GRAMMAR, false);
			Parser parser = new Parser(tokens);

			Dictionary<ASMethod, byte[]> dict_scriptinit_onlyconst = new Dictionary<ASMethod, byte[]>();

			//先计算CodeScope
			foreach (var script in context.scriptDefs)
			{
				var fullpath = script.fullPath;
				if (!System.IO.File.Exists(fullpath))
				{
					throw new InvalidOperationException($"内部异常,源文件没有找到{fullpath}");
				}

				try
				{
					ASNamespace as3ns = new ASNamespace()
					{
						def_uri = "http://adobe.com/AS3/2006/builtin",
						in_package = "",
						Kind = NamespaceKind.PackageInternal,
						Name = ":AS3"
					};
					if (!script.namespaces.Contains(as3ns))
					{
						script.namespaces.Add(as3ns);
					}

					foreach (var item in context.scriptDef_packageimports[script])
					{
						if (item.Kind == TraitKind.Constant && item.ValueKind == ConstantKind.Namespace)
						{
							script.namespaces.Add(item.Value.Namespace);
						}
					}
					foreach (var item in context.scriptDef_scriptimports[script])
					{
						if (item.Kind == TraitKind.Constant && item.ValueKind == ConstantKind.Namespace)
						{
							script.namespaces.Add(item.Value.Namespace);
						}
					}


					ASNamespace n_private = new ASNamespace()
					{
						Kind = NamespaceKind.Private,
						Name = ""
					};
					if (!script.namespaces.Contains(n_private))
					{
						script.namespaces.Add(n_private);
					}

					ASNamespace n_public = new ASNamespace()
					{
						Kind = NamespaceKind.Package,
						Name = ""
					};
					if (!script.namespaces.Contains(n_public))
					{
						script.namespaces.Add(n_public);
					}

					ASNamespace n_protected = new ASNamespace()
					{
						Kind = NamespaceKind.Protected,
						Name = ""
					};
					if (!script.namespaces.Contains(n_protected))
					{
						script.namespaces.Add(n_protected);
					}
					ASNamespace n_internal = new ASNamespace()
					{
						Kind = NamespaceKind.PackageInternal,
						Name = ""
					};
					if (!script.namespaces.Contains(n_internal))
					{
						script.namespaces.Add(n_internal);
					}

					//重置Method的NameSpaceSet,遍历它所有能直接访问的成员，加入他们的namespace
					foreach (var m in script.scriptMethods)
					{
						if (m != null)
						{
							HashSet<ASNamespace> ns = new HashSet<ASNamespace>();

							ASNamespaceSet oset = null;

							if (m.Body.NamespaceSetIndex > 0)
							{
								oset = script.namespaceSets[m.Body.NamespaceSetIndex];
								foreach (var item in oset.Namespaces)
								{
									ns.Add(item);
								}
							}

							//默认打开AS3命名空间
							ns.Add(as3ns);

							ASNamespace _public_ = new ASNamespace()
							{
								Kind = NamespaceKind.Package,
								Name = ""
							};
							if (!script.namespaces.Contains(_public_))
								script.namespaces.Add(_public_);

							ns.Add(_public_);


							if (m != script.Script.Initializer)
							{
								if (script.Script.Traits.Count > 0) //仅加入script.namespaces.
								{
									ASNamespace _internal_ = new ASNamespace()
									{
										Kind = NamespaceKind.PackageInternal,
										Name = script.Script.Traits[0].QName.Namespace.Name
									};

									if (!script.namespaces.Contains(_internal_))
										script.namespaces.Add(_internal_);

								}
							}

							bool isInpackage = false;
							if (script.Script.Traits.Count == 1)
							{
								isInpackage = true;
							}


							//打开script的namespace , MainClass可以访问script里的internal...
							if (script.Script.Traits.Count > 1)
							{
								//ASNamespace _internal_ = new ASNamespace()
								//{
								//     Kind = NamespaceKind.PackageInternal,
								//     Name = script.Script.Traits[1].QName.Namespace.Name
								//};

								//if(!script.namespaces.Contains(_internal_))
								//    script.namespaces.Add(_internal_);

								//ns.Add(_internal_);



								ASContainer _c = m.Container;
								while (!(_c is ASScript))
								{
									if (_c is ASInstance || _c is ASClass)
									{
										if (_c.QName == script.Script.QName)
										{
											isInpackage = true;
										}

										var t = _c.Traits.FirstOrDefault((t) => t.QName.Namespace.Kind == NamespaceKind.PackageInternal && t.QName.Namespace.def_uri == null);
										if (t != null)
										{
											ns.Add(t.QName.Namespace);
										}

										break;
									}

									_c = ((ASMethodBody)_c).Method.Container;
								}


								if (!isInpackage)
								{
									for (int i = 1; i < script.Script.Traits.Count; i++)
									{
										if (script.Script.Traits[i].Kind == TraitKind.Class)
										{
											ASNamespace _internal_ = new ASNamespace()
											{
												Kind = NamespaceKind.PackageInternal,
												Name = script.Script.Traits[i].QName.Namespace.Name + ":" + script.Script.Traits[i].QName.Name
											};

											if (!script.namespaces.Contains(_internal_))
												script.namespaces.Add(_internal_);

											ns.Add(_internal_);
										}
									}
								}
							}


							ASContainer c = m.Body;

							while (true)
							{
								foreach (var t in c.Traits)
								{

									ns.Add(t.QName.Namespace);
								}

								c = ((ASMethodBody)c).Method.Container;
								if (!(c is ASMethodBody))
								{
									break;
								}
							}

							if (c is ASClass) // 静态函数可以访问Instance里的internal对象。。。
							{
								foreach (var t in ((ASClass)c).Instance.Traits)
								{
									if (
											(t.QName.Namespace.Kind == NamespaceKind.PackageInternal
											)

											)
									{
										ns.Add(t.QName.Namespace);
									}
								}

							}

							while (true)
							{

								//生成可能的私有script namespace
								var _private_ = new ASNamespace()
								{
									Kind = NamespaceKind.Private,
									Name = string.IsNullOrEmpty(c.QName.Namespace.Name) ?
								c.QName.Name :
								c.QName.Namespace.Name + ":" + c.QName.Name
								};

								if (!script.namespaces.Contains(_private_))
									script.namespaces.Add(_private_);


								if (c is ASScript)
								{
									if (isInpackage)
									{
										foreach (var t in c.Traits)
										{
											ns.Add(t.QName.Namespace);
										}
									}
									else
									{
										for (int i = 1; i < c.Traits.Count; i++)
										{
											ns.Add(c.Traits[i].QName.Namespace);
										}
									}

									break;
								}
								else if (c is ASClass)
								{
#if DEBUG
									if (((ASClass)c).Constructor != m && (m.Trait != null && !m.Trait.IsStatic))
									{
										throw new InvalidOperationException();
									}
#endif

									foreach (var t in c.Traits)
									{
										ns.Add(t.QName.Namespace);
									}

									var cls = ((ASClass)c).Instance._super_class_;
									while (cls != null)
									{
										foreach (var t in cls.Traits)
										{
											if (
												t.QName.Namespace.Kind == NamespaceKind.Package
												||
												t.QName.Namespace.Kind == NamespaceKind.StaticProtected
												||
												(t.QName.Namespace.Kind == NamespaceKind.PackageInternal && t.QName.Namespace.Name == c.QName.Namespace.Name)
												||
												(t.QName.Namespace.Kind == NamespaceKind.PackageInternal && t.QName.Namespace.def_uri == null && cls.QName.Namespace.Name == c.QName.Namespace.Name)
												||
												(oset != null && oset.Namespaces.Contains(t.QName.Namespace))
												)
											{
												ns.Add(t.QName.Namespace);
											}
										}
										cls = cls.Instance._super_class_;
									}


									c = script.Script;
									continue;

								}
								else if (c is ASInstance)
								{
									//查找顺序：自身-自身继承来的属性 - 静态对象-父类静态对象-Script
									foreach (var t in c.Traits)
									{
										if (
												t.QName.Namespace.Kind == NamespaceKind.Private
												||
												t.QName.Namespace.Kind == NamespaceKind.Package
												||
												t.QName.Namespace.Kind == NamespaceKind.Protected
												||
												(t.QName.Namespace.Kind == NamespaceKind.PackageInternal && t.QName.Namespace.Name == c.QName.Namespace.Name
												)
												||
												(oset != null && oset.Namespaces.Contains(t.QName.Namespace))
												)
										{
											ns.Add(t.QName.Namespace);
										}
									}

									ASClass cls = script.scriptClasses.First(cl => cl != null && cl.Instance == c);

									while (cls != null)
									{
										foreach (var t in cls.Traits)
										{
											if ((cls.Instance == c && !(t.QName.Namespace.Kind == NamespaceKind.PackageInternal && t.QName.Namespace.def_uri != null))
												||
												t.QName.Namespace.Kind == NamespaceKind.Package
												||
												t.QName.Namespace.Kind == NamespaceKind.StaticProtected
												||
												(t.QName.Namespace.Kind == NamespaceKind.PackageInternal && t.QName.Namespace.def_uri == null && cls.QName.Namespace.Name == c.QName.Namespace.Name)

												||
												(oset != null && oset.Namespaces.Contains(t.QName.Namespace))
												)
											{
												ns.Add(t.QName.Namespace);
											}
										}

										foreach (var t in cls.Instance.Traits)
										{
											if (
												(cls.Instance == c && !(t.QName.Namespace.Kind == NamespaceKind.PackageInternal && t.QName.Namespace.def_uri != null))
												||
												t.QName.Namespace.Kind == NamespaceKind.Package
												||
												t.QName.Namespace.Kind == NamespaceKind.Protected
												||
												(t.QName.Namespace.Kind == NamespaceKind.PackageInternal && t.QName.Namespace.def_uri == null && cls.QName.Namespace.Name == c.QName.Namespace.Name)

												||
												(oset != null && oset.Namespaces.Contains(t.QName.Namespace))
												)
											{
												ns.Add(t.QName.Namespace);
											}
										}

										cls = cls.Instance._super_class_;

									}

									c = script.Script;

									continue;
								}
							}

							//加入接口的方法的命名空间
							var imports = GetMethodImports(script, m, context);
							foreach (var item in imports)
							{
								if (item.Kind == TraitKind.Class && item.Class.Instance.IsInterface)
								{
									foreach (var imember in item.Class.Instance.Traits)
									{
										if (!script.namespaces.Contains(imember.QName.Namespace))
											script.namespaces.Add(imember.QName.Namespace);

										ns.Add(imember.QName.Namespace);
									}
								}
							}
							ASNamespace _selfns;
							//加入导入对象的NameSpace和自身Class的命名空间相同的命名空间。。
							if (isInpackage)
							{
								_selfns = script.Script.Traits[0].QName.Namespace;

								if (!script.namespaces.Contains(_selfns))
									script.namespaces.Add(_selfns);

								ns.Add(_selfns);

								ASContainer _c = m.Container;
								while (!(_c is ASScript))
								{
									if (_c is ASInstance || _c is ASClass)
									{
										break;
									}

									_c = ((ASMethodBody)_c).Method.Container;
								}

								foreach (var item in imports)
								{
									if (item.Kind == TraitKind.Class && !item.Class.Instance.IsInterface
										&&
									  ((_c is ASInstance && item.Class.Instance != _c)
										||
										(_c is ASClass && item.Class != _c)
									  )

										)
									{
										if (item.QName.Namespace.Name == _c.QName.Namespace.Name)
										{
											foreach (var imember in item.Class.Traits)
											{
												if (imember.QName.Namespace.Kind == NamespaceKind.PackageInternal
													)
												{
													if (imember.QName.Namespace.Kind == NamespaceKind.PackageInternal
														&&
														imember.QName.Namespace.Name.StartsWith(_c.QName.Namespace.Name + ":")
													)
													{
														if (!script.namespaces.Contains(imember.QName.Namespace))
															script.namespaces.Add(imember.QName.Namespace);

														ns.Add(imember.QName.Namespace);
													}
												}
											}

											foreach (var imember in item.Class.Instance.Traits)
											{
												if (imember.QName.Namespace.Kind == NamespaceKind.PackageInternal
													&&
													imember.QName.Namespace.Name.StartsWith(_c.QName.Namespace.Name + ":")
													)
												{
													if (!script.namespaces.Contains(imember.QName.Namespace))
														script.namespaces.Add(imember.QName.Namespace);

													ns.Add(imember.QName.Namespace);
												}
											}
										}
									}
								}
							}
							else
							{
								_selfns = script.Script.Traits[1].QName.Namespace;

								if (!script.namespaces.Contains(_selfns))
									script.namespaces.Add(_selfns);

								ns.Add(_selfns);
							}


							ASNamespaceSet namespaceSet = new ASNamespaceSet();

							var nslist = ns.ToList();
							nslist.Remove(_selfns);
							nslist.Insert(0, _selfns);



							namespaceSet.Namespaces = nslist;
							if (!script.namespaceSets.Contains(namespaceSet))
							{
								script.namespaceSets.Add(namespaceSet);
							}

							m.Body.NamespaceSetIndex = script.namespaceSets.IndexOf(namespaceSet);

						}
					}





					Player.ComputeCodeScope(script.Script, script.namespaceSets.ToArray());

					for (int i = 0; i < script.Script.codeScopes.Count; i++)
					{
						var scope = script.Script.codeScopes[i];
						if (scope.Kind == CodeScopeKind.Instance)
						{
							if (!((ASInstance)scope.Container).IsInterface)
							{
								scope.TypeLayout = context.dict_typelayout[scope.Container.QName];
							}
						}

					}

					context.player_for_compiler.ComputeVTable(script.Script);
					//检查Setter和Getter的类型是否匹配
					for (int i = 0; i < script.Script.codeScopes.Count; i++)
					{
						var scope = script.Script.codeScopes[i];
						if (scope.Container._vtable != null)
						{
							VTable vTable = (VTable)scope.Container._vtable;

							foreach (var setter in vTable.Items.Where(i => i.Trait.Kind == TraitKind.Setter))
							{
								if (setter.Trait.Method.Parameters.Count != 1)
								{
									throw new ResolverException(setter.Trait.Token, "A setter definition must have exactly one parameter.");
								}


								var getter = vTable.Items.FirstOrDefault(
									(m) => m.Trait.Kind == TraitKind.Getter
									&&
									m.Trait.QName.Name == setter.Trait.QName.Name
									);

								if (getter != null && (getter.Trait.QName.Namespace == setter.Trait.QName.Namespace
									||
									(
										getter.Trait.QName.Namespace.Kind == NamespaceKind.PackageInternal
										&&
										setter.Trait.QName.Namespace.Kind == NamespaceKind.PackageInternal
										&&
										getter.Trait.QName.Namespace.def_uri == setter.Trait.QName.Namespace.def_uri

									)

									))
								{
									if (getter.Trait.Method.ReturnTypeKind != setter.Trait.Method.Parameters[0].TypeKind
										&&
										!(setter.Trait.Method.Parameters[0].TypeKind == TypeKind.Any
											||
											getter.Trait.Method.ReturnTypeKind == TypeKind.Any
										)

										)
									{
										throw new ResolverException(setter.Trait.Token, "Accessor types must match.");
									}
								}
							}


						}
					}
				}
				catch (LoaderException e)
				{
					throw new ResolverException(e.Token, e.Message);
				}
			}

			//检查接口实现
			foreach (var script in context.scriptDefs)
			{
				try
				{
					context.player_for_compiler.ComputeInterface(script.Script);
				}
				catch (LoaderException e)
				{
					throw new ResolverException(e.Token, e.Message);
				}
			}


			foreach (var script in context.scriptDefs)
			{
				var fullpath = script.fullPath;

				string proj = context.scriptInProj[script];
				string sfile = System.IO.Path.Combine(workDir, script.fullPath.Substring(proj.Length)) + ".m";

				string input = System.IO.File.ReadAllText(fullpath);
				var origin = new MyMD5.MyMD5().Hash(input).ToString();

				if (File.Exists(sfile) && !force_rebuild_bcode)
				{
					//Try Load
					using (var fs = File.OpenRead(sfile))
					{
						using (System.IO.BinaryReader br = new BinaryReader(fs))
						{
							try
							{
								var md5 = br.ReadString();
								if (md5 == origin)
								{
									for (int i = 1; i < script.scriptMethods.Count; i++)
									{
										var method = script.scriptMethods[i];

										int flag = br.ReadInt32();
										method.Flags = (MethodFlags)flag;

										#region 读常量池

										int count = br.ReadInt32();
										List<NaNBoxing> constants = new List<NaNBoxing>();

										var reader = (BinaryReader br) =>
										{
											ulong raw = br.ReadUInt64();
											NaNBoxing box = new NaNBoxing(raw);

											if (box.ValueType == NaNBoxing.BoxType.HeapPtr)
											{
												RtHeapTypeKind type = (RtHeapTypeKind)br.ReadByte();
												if (type == RtHeapTypeKind.STRING)
												{
													int chars = br.ReadInt32();
													Memory<char> str = new Memory<char>(new char[chars]);
													for (int i = 0; i < chars; i++)
													{
														str.Span[i] = (char)br.ReadUInt16();
													}

													//string str = br.ReadString();
													int heap_ptr = context.player_for_compiler.Context.GC.Complie_AllocString(str.ToString());
													if (heap_ptr == 0)
														throw new InvalidOperationException();

													heap_ptr = (heap_ptr & 0xffffff) | ((byte)ASMethodBody.PoolHeapPtrKind.String << 24);

													box.SetHeapPtr(heap_ptr);

												}
												else if (type == (RtHeapTypeKind)10)
												{
													ulong class_id = br.ReadUInt64();
													var @class = context.scriptDefs.SelectMany(s => s.scriptClasses)
														.Union
														(
															context.player_for_compiler.Context.libs.SelectMany(l => l.Classes)
														).First(c => c != null && c.Type_identifier == class_id);

													int index = context.constpool_ldclass.IndexOf(class_id);
													if (index < 0)
													{
														index = context.constpool_ldclass.Count;
														context.constpool_ldclass.Add(class_id);
													}

													int heap_ptr = (index & 0xffffff) | ((byte)ASMethodBody.PoolHeapPtrKind.LD_Class << 24);
													box.SetHeapPtr(heap_ptr);

												}
												//else if (type == RtHeapTypeKind.CACHE_LD_CLASS)
												//{
												//	ulong class_id = br.ReadUInt64();

												//	var @class = context.scriptDefs.SelectMany(s => s.scriptClasses)
												//		.Union
												//		(
												//			context.player_for_compiler.Context.libs.SelectMany(l => l.Classes)
												//		).First(c => c != null && c.Type_identifier == class_id);

												//	int heap_ptr = context.player_for_compiler.Context.GC.AllocLD_Class(@class);
												//	if (heap_ptr == 0)
												//		throw new InvalidOperationException();

												//	heap_ptr = (heap_ptr & 0xffffff) | ((byte)ASMethodBody.PoolHeapPtrKind.LD_Class << 24);

												//	box.SetHeapPtr(heap_ptr);

												//}
												else if (type == RtHeapTypeKind.NAMESPACE)
												{
													int index = br.ReadInt32();
													var ns = script.namespaces[index];

													int heap_ptr = context.player_for_compiler.Context.GC.AllocNamespace(ns, 0, 0);
													if (heap_ptr == 0)
														throw new InvalidOperationException();
													heap_ptr = (heap_ptr & 0xffffff) | ((byte)ASMethodBody.PoolHeapPtrKind.Namespace << 24);

													box.SetHeapPtr(heap_ptr);
												}
												else if (type == RtHeapTypeKind.VECTOR)
												{
													int depth = br.ReadInt32();
													ulong ElementType = br.ReadUInt64();

													VectorDef vd = VectorDef.CreateOrGet(context, (TypeKind)ElementType, depth);

													int ptr = context.vectorDefs.IndexOf(vd);
													ptr = (ptr & 0xffffff) | ((byte)ASMethodBody.PoolHeapPtrKind.VectorDef << 24);

													box.SetHeapPtr(ptr);

												}
												else if (type == RtHeapTypeKind.MethodScope)
												{
													int index = br.ReadInt32();
													ASMethod m = script.scriptMethods[index];

													int heap_ptr = context.player_for_compiler.Context.GC.AllocMethodScope(null, 0, null);
													if (heap_ptr == 0)
														throw new InvalidOperationException();

													context.player_for_compiler.Context.GC.Heap[heap_ptr].Type = m.Body;

													heap_ptr = (heap_ptr & 0xffffff) | ((byte)ASMethodBody.PoolHeapPtrKind.Method << 24);
													box.SetHeapPtr(heap_ptr);
												}
												else if (type == (RtHeapTypeKind)200)
												{
													ulong class_id = br.ReadUInt64();
													int vtable_index = br.ReadInt32();

													var @class = context.scriptDefs.SelectMany(s => s.scriptClasses)
														.Union
														(
															context.player_for_compiler.Context.libs.SelectMany(l => l.Classes)
														).First(c => c != null && c.Type_identifier == class_id);

													int heap_ptr = context.player_for_compiler.Context.GC.AllocMethodScope(null, 0, null);
													if (heap_ptr == 0)
														throw new InvalidOperationException();

													context.player_for_compiler.Context.GC.Heap[heap_ptr].Type = @class;
													((RtPayloadMethodScope)context.player_for_compiler.Context.GC.Heap[heap_ptr].facility).ParentPtr = vtable_index;


													heap_ptr = (heap_ptr & 0xffffff) | ((byte)ASMethodBody.PoolHeapPtrKind.SuperMethod << 24);

													box.SetHeapPtr(heap_ptr);

												}
												else
												{
													throw new InvalidOperationException();
												}
											}
											return box;
										};

										for (int j = 0; j < count; j++)
										{
											var box = reader(br);
											constants.Add(box);

										}

										#endregion

										int len = br.ReadInt32();
										byte[] bytes = br.ReadBytes(len);

										method.Body.ByteCode = bytes;

										for (int a = 0; a < method.Parameters.Count; a++)
										{
											var para = method.Parameters[a];
											if (para.IsOptional)
											{
												//para.compute_constants = new List<NaNBoxing>();

												var compute_constants = new List<NaNBoxing>();

												len = br.ReadInt32();
												for (int j = 0; j < len; j++)
												{
													var box = reader(br);
													//para.compute_constants.Add(box);
													compute_constants.Add(box);
												}

												para.compute_result_index = br.ReadInt32();

												len = br.ReadInt32();
												bytes = br.ReadBytes(len);

												para.computeDefaultValue = bytes;


												ASMethodBody.CheckConstants( para.computeDefaultValue , compute_constants );

											}
										}

										if (script.Script.Initializer == method || method.IsConstructor)
										{
											//len = br.ReadInt32();
											//bytes = br.ReadBytes(len);

											//dict_scriptinit_onlyconst.Add(method, bytes);

											//for (int s = 0; s < method.Container._link_codescope.Members.Count; s++)
											//{
											//	var m = method.Container._link_codescope.Members[s];

											//	len = br.ReadInt32();
											//	if (len > 0)
											//	{
											//		bytes = br.ReadBytes(len);
											//		m.compiler_initvalue = bytes;
											//	}
											//}
										}

										int init_members_count = br.ReadInt32();
										for (int j = 0; j < init_members_count; j++)
										{
											int c_index = br.ReadInt32();
											int m_index = br.ReadInt32();

											var container = script.containers[c_index];
											var smember = container._link_codescope.Members[m_index];

											len = br.ReadInt32();
											smember.compiler_initvalue = br.ReadBytes(len);
											smember.compiler_initvalue_stpos = br.ReadInt32();

										}

										context.dict_method_constants.Add(method, constants);

									}


									continue;
								}
							}
							catch (EndOfStreamException)
							{
							}
							catch (IOException)
							{
							}
						}
					}
				}


				AS3SrcFile as_srcfile = null;
				string srcCode = input;

				ParseTree tree = parser.ParseTree(srcCode, AS3LexKeywords.LEXKEYWORDS, AS3LexKeywords.LEXSKIPBLANKWORDS, fullpath);
				if (parser.hasError)
				{
					throw new ParseException(string.Empty);
				}

				var ast = new AS3AbstractSyntaxTree();
				as_srcfile = ast.Analyse(tree);

				if (ast.SyntaxError != null)
				{
					throw ast.SyntaxError;
				}

				#region 源码对应
				if (script.scriptMethods[script.scriptMethods.Count - 1] != script.Script.Initializer)
				{
					throw new InvalidOperationException();
				}


				#region 对应源码

				foreach (var c in script.containers)
				{
					foreach (var t in c.Traits)
					{
						if (t.Value != null)
						{
							switch (t.Value.ValueType)
							{
								case ABC.ASTrait.TraitValueType.NameSpace:
									t.Value._value = t.Value.Namespace;
									break;
								case ABC.ASTrait.TraitValueType.AS3Function:
									t.Value._value = as_srcfile._functions[t.Value.FunctionOrExpression_Index];
									break;
								case ABC.ASTrait.TraitValueType.AS3Expression:
									t.Value._value = as_srcfile._expressions[t.Value.FunctionOrExpression_Index];
									break;
								default:
									break;
							}
						}
					}
				}

				#endregion



				#region 对应源码
				{
					var as3_srclist = ((new AS3ClassInterfaceBase[] { as_srcfile.Package.MainClass, as_srcfile.Package.MainInterface }).Union
							   (
								   as_srcfile.OutPackage.outpackage_classes_interfaces
							   )).Where((a) => a != null).ToArray();

					for (int i = 1; i < script.scriptMethods.Count; i++)
					{
						ASMethod method = script.scriptMethods[i];

						var cls = script.scriptClasses.FirstOrDefault((c) => c != null && c.Constructor == method);
						var instance = script.scriptClasses.Where((s) => s != null).Select((s) => s.Instance).FirstOrDefault((c) => c != null && c.Constructor == method);
						if (cls != null)
						{

						}
						else if (instance != null)
						{
							if (method.ast_function_index > -1)
							{
								var function = as_srcfile._functions[method.ast_function_index];
								context.dict_method_as3function.Add(function, method);
							}
						}
						else if (method == script.Script.Initializer)
						{

						}
						else
						{
							if (as_srcfile._functions[method.ast_function_index] == null)
								throw new InvalidOperationException();
							context.dict_method_as3function.Add(as_srcfile._functions[method.ast_function_index], method);
						}
					}


				}
				#endregion


				#endregion

				BuildScript(script, as_srcfile, context, sfile, origin, dict_scriptinit_onlyconst);
			}

			var pdefaults = context.scriptDefs.SelectMany(s => s.scriptMethods).Where(m => m != null).SelectMany(m => m.Parameters).Where(p => p.IsOptional).ToArray();
			Dictionary<ASParameter, byte[]> p_defaultcode = new Dictionary<ASParameter, byte[]>();
			foreach (var p in pdefaults)
			{
				p_defaultcode.Add(p, p.computeDefaultValue);
				p.computeDefaultValue = new byte[0];
			}


			ComputeMemberDefaultValue(context, workDir, libs, outswcfile, dict_scriptinit_onlyconst);

			foreach (var p in pdefaults)
			{
				p.computeDefaultValue = p_defaultcode[p];
			}


			ComputeFunctionDefaultValue(context, workDir, libs, outswcfile, dict_scriptinit_onlyconst);



			//优化Pass
			foreach (var script in context.scriptDefs)
			{
				for (int i = 1; i < script.scriptMethods.Count; i++)
				{
					ASMethod method = script.scriptMethods[i];

					Optimizer.Optimize(method);
					
					ComputeJump(method,true);
				}
			}

			return 0;
		}



		private static void ComputeMemberDefaultValue(CompileContext context, string workDir, List<string> libs, string outswcfile, Dictionary<ASMethod, byte[]> dict_scriptinit_onlyconst)
		{
			var testCode = SWCWriter.Encode(context, System.IO.Path.GetFileName(outswcfile) == "juice_global.swc" ? Path.GetFileName(outswcfile) : Guid.NewGuid().ToString());
			juicescript.runtime.Player computeplayer = new Player(int.MaxValue, true);
			SWCFile testswc = computeplayer.LoadLib(testCode);
			try
			{
				foreach (string lib in libs)
				{
					var libData = System.IO.File.ReadAllBytes(System.IO.Path.Combine(workDir, lib));
					computeplayer.LoadLib(libData);
				}

				computeplayer.PrepareComputeMemberInitValue();
			}
			catch (System.IO.IOException e)
			{
				throw new CompilerLoadLibException(e.Message);
			}
			catch (LoaderException e)
			{
				throw new CompilerLoadLibException(e.Message);
			}

			
			//任何初始值计算时如果需要读取变量则计算失败。
			//只需要不停的迭代计算，直到没有新的成功为止。此时所有初始化都计算成功
			Dictionary<string, NaNBoxing> dict_newstring = new Dictionary<string, NaNBoxing>();
			while (true)
			{
				bool hasSuccess = false;

				foreach (var script in context.scriptDefs.Select(s => s.Script))
				{
					var testswc_script = testswc.Scripts.Find(s => s.QName == script.QName);

					foreach (var container in script.allContainers.Where(c => c != null).OrderBy(c => (int)c._link_codescope.Kind))
					{
						foreach (var member in container._link_codescope.Members
							.Where(m => m.compiler_initvalue != null
							&&
							m.trait.Value !=null
							&&
							!m.trait.Value.initValue.HasValue
							))
						{

							ASMethod _temp; ASMethod test_method;
							ScopeMember t_member;
							if (member.DefineAt is ASClass)
							{
								t_member = member;
								
								var method = ((ASClass)member.DefineAt).Constructor;
								int method_idx = script.allContainers.IndexOf(method.Body);
								test_method = ((ASMethodBody)testswc_script.allContainers[method_idx]).Method;

								_temp = new ASMethod(test_method.Container, test_method.Token);
								_temp.Body = new ASMethodBody(_temp);
								_temp.ReturnTypeKind = TypeKind.Fun_Void;
								_temp.Body.ByteCode = (byte[])member.compiler_initvalue.Clone();

								_temp.Body._link_codescope = new CodeScope();
								_temp.Body._link_codescope.Members = new List<ScopeMember>();
								_temp.Body._link_codescope.Kind = (CodeScopeKind)255;
								_temp.Body._link_codescope.ParameterCout = 0;
								_temp.Body._link_codescope.NamespaceSet = member.DefineAt._link_codescope.NamespaceSet;
							}
							else if (member.DefineAt is ASScript)
							{
								t_member = member;

								var method = ((ASScript)member.DefineAt).Initializer;
								int method_idx = script.allContainers.IndexOf(method.Body);
								test_method = ((ASMethodBody)testswc_script.allContainers[method_idx]).Method;

								_temp = new ASMethod(test_method.Container, test_method.Token);
								_temp.Body = new ASMethodBody(_temp);
								_temp.ReturnTypeKind = TypeKind.Fun_Void;
								_temp.Body.ByteCode = (byte[])member.compiler_initvalue.Clone();

								_temp.Body._link_codescope = new CodeScope();
								_temp.Body._link_codescope.Members = test_method.Body._link_codescope.Members.ToList(); //new List<ScopeMember>();
								_temp.Body._link_codescope.Kind = (CodeScopeKind)255;
								_temp.Body._link_codescope.index = test_method.Body._link_codescope.index;
								_temp.Body._link_codescope.ParameterCout = 0;
								_temp.Body._link_codescope.NamespaceSet = member.DefineAt._link_codescope.NamespaceSet;
							}
							else if (member.DefineAt is ASInstance)
							{
								t_member = member;

								var method = ((ASInstance)member.DefineAt).Constructor;
								int method_idx = script.allContainers.IndexOf(method.Body);
								test_method = ((ASMethodBody)testswc_script.allContainers[method_idx]).Method;

								_temp = new ASMethod(test_method.Container, test_method.Token);
								_temp.Body = new ASMethodBody(_temp);
								_temp.ReturnTypeKind = TypeKind.Fun_Void;
								_temp.Body.ByteCode = (byte[])member.compiler_initvalue.Clone();

								_temp.Body._link_codescope = new CodeScope();
								_temp.Body._link_codescope.Members = new List<ScopeMember>();
								_temp.Body._link_codescope.Kind = (CodeScopeKind)255;
								_temp.Body._link_codescope.ParameterCout = 0;
								_temp.Body._link_codescope.NamespaceSet = member.DefineAt._link_codescope.NamespaceSet;
							}
							else if (member.DefineAt is ASMethodBody)
							{
								var method = ((ASMethodBody)member.DefineAt).Method;
								int method_idx = script.allContainers.IndexOf(method.Body);
								test_method = ((ASMethodBody)testswc_script.allContainers[method_idx]).Method;


								_temp = new ASMethod(test_method.Body, test_method.Token);
								_temp.Body = new ASMethodBody(_temp);
								_temp.ReturnTypeKind = TypeKind.Fun_Void;
								_temp.Body.ByteCode = (byte[])member.compiler_initvalue.Clone();

								_temp.Body._link_codescope = new CodeScope();
								_temp.Body._link_codescope.Members = test_method.Body._link_codescope.Members.ToList();
								_temp.Body._link_codescope.Kind = (CodeScopeKind)255;
								_temp.Body._link_codescope.index = test_method.Body._link_codescope.index;
								_temp.Body._link_codescope.ParameterCout = 0;
								_temp.Body._link_codescope.NamespaceSet = test_method.Body._link_codescope.NamespaceSet;
								_temp.Body._link_codescope.Container = test_method.Body._link_codescope.Container;
								_temp.Body._link_codescope.Parent = test_method.Body._link_codescope.Parent;

								int mid= member.DefineAt._link_codescope.Members.IndexOf(member);
								t_member = test_method.Body._link_codescope.Members[mid];
							}
							else
							{
								throw new InvalidOperationException();
							}

							try
							{
								
								NaNBoxing v = computeplayer.ComputeMemberInitValue(t_member, _temp, testswc, test_method.Body.ByteCode);
								if (v.ValueType == NaNBoxing.BoxType.HeapPtr)
								{
									var obj = computeplayer.Context.GC.Heap[v.HeapPtr];
									if (obj.TypeKind == RtHeapTypeKind.STRING)                        //堆中的对象只有String可以被作为初始化值
									{
										string str = ((RtPayloadString)obj.facility).Str;

										if (dict_newstring.ContainsKey(str))
										{
											v = dict_newstring[str];
										}
										else
										{
											int heapptr = context.player_for_compiler.Context.GC.Complie_AllocString(str);
											if (heapptr == 0)
												throw new InvalidOperationException();
											if (heapptr > 0xffffff)
											{
												throw new ParseException("heapptr > 0xffffff");
											}

											int ptr = (0xffffff & heapptr) | ((byte)ASMethodBody.PoolHeapPtrKind.String << 24);
											v.SetHeapPtr(ptr);

											dict_newstring.Add(str, v);
										}
									}
									else
									{
										continue;
									}
								}
								else if (v.ValueType == NaNBoxing.BoxType.LocalString)
								{
									string str = v.LocalStringValue;

									if (dict_newstring.ContainsKey(str))
									{
										v = dict_newstring[str];
									}
									else
									{
										int heapptr = context.player_for_compiler.Context.GC.Complie_AllocString(str);
										if (heapptr == 0)
											throw new InvalidOperationException();
										if (heapptr > 0xffffff)
										{
											throw new ParseException("heapptr > 0xffffff");
										}

										int ptr = (0xffffff & heapptr) | ((byte)ASMethodBody.PoolHeapPtrKind.String << 24);
										v.SetHeapPtr(ptr);

										dict_newstring.Add(str, v);
									}
								}
								else if (v.ValueType == NaNBoxing.BoxType.Fault)
								{
									continue;
								}
								else
								{
									if (member.trait.TypeKind == TypeKind.Any)
									{
										switch (v.ValueType)
										{
											case NaNBoxing.BoxType.Number:
												break;
											case NaNBoxing.BoxType.Undefined:
												break;
											case NaNBoxing.BoxType.Null:
												break;
											case NaNBoxing.BoxType.Boolean:
												break;
											case NaNBoxing.BoxType.Int:
												//将负数保存到较小的类型里。
												{
													int ivalue = v.IntValue;
													if (ivalue >= sbyte.MinValue && ivalue <= sbyte.MaxValue)
													{
														v.SetSByte((sbyte)ivalue);
													}
													else if (ivalue >= short.MinValue && ivalue <= short.MaxValue)
													{
														v.SetShort((short)ivalue);
													}

												}

												break;
											case NaNBoxing.BoxType.Uint:
												break;
											case NaNBoxing.BoxType.Sbyte:
												break;
											case NaNBoxing.BoxType.Byte:
												break;
											case NaNBoxing.BoxType.Short:
												break;
											case NaNBoxing.BoxType.UShort:
												break;
											case NaNBoxing.BoxType.Float:
												break;
											case NaNBoxing.BoxType.HeapPtr:
												break;
											case NaNBoxing.BoxType.Fault:
												break;
											default:
												break;
										}
									}
								}


								member.trait.Value.initValue = v;
								hasSuccess = true;
							}
							catch (EvalConstException)
							{

							}
							finally
							{
								computeplayer.ComputeMemberInit_ResertStack();
							}
						}

					}



				}

				if (!hasSuccess)
				{
					break;
				}
			}

			Dictionary<ASMethod,int> bytecode_shrink = new Dictionary<ASMethod, int>();

			foreach (var container in context.scriptDefs.SelectMany(s => s.containers)
				.Where(c => c._link_codescope.Members.Any(m => m.Kind != ScopeMemberKind.Parameter && m.trait.Value != null && m.trait.Value.initValue.HasValue))
				)
			{
				ASMethod method;
				int shrinked = 0;bool flag_might_shrinked = false;
				if (container._link_codescope.Kind == CodeScopeKind.Class)
				{
					method = ((ASClass)container).Constructor;
				}
				else if (container._link_codescope.Kind == CodeScopeKind.Script)
				{
					method = ((ASScript)container).Initializer;
				}
				else if (container._link_codescope.Kind == CodeScopeKind.Instance)
				{
					method = ((ASInstance)container).Constructor;
				}
				else if (container._link_codescope.Kind == CodeScopeKind.Method)
				{
					method = ((ASMethodBody)container).Method;

					if (bytecode_shrink.ContainsKey(method))
					{
						shrinked = bytecode_shrink[method];
						flag_might_shrinked = true;
					}
				}
				else
				{
					throw new InvalidOperationException();
				}

				int oldsize = method.Body.ByteCode.Length;

				int useslotcount; NaNBoxing[] constants;Instruction[] instructions;
				Disassembler.Disassemble(method.Body.ByteCode, out useslotcount, out constants, out instructions);
				List<Token> tokens = instructions.Select(i => i.token).ToList();

				
				List<Instruction> newinstructions = new List<Instruction>();

				int lastindex = 0;
				foreach (var scopeMember in container._link_codescope.Members.OrderBy(m => m.compiler_initvalue_stpos)
					.Where(m => m.DefineAt == container && m.Kind != ScopeMemberKind.Parameter && m.trait.Value != null && m.trait.Value.initValue.HasValue)
					)
				{
					int skipbytes = 0;int st_idx = 0;
					while (skipbytes < scopeMember.compiler_initvalue_stpos -shrinked)
					{
						skipbytes += instructions[st_idx].Size;
						st_idx++;
					}
#if DEBUG
					if (skipbytes != scopeMember.compiler_initvalue_stpos - shrinked)
						throw new InvalidOperationException();
#endif

					if (st_idx > lastindex)
					{
						for (int i = lastindex; i < st_idx; i++)
						{
							newinstructions.Add(instructions[i] );
							
						}
					}

					ASMethodBody.MethodBodyInfo info_src = new ASMethodBody.MethodBodyInfo();
					ASMethodBody.GetInfo(ref info_src, scopeMember.compiler_initvalue);

					if (scopeMember.Kind == ScopeMemberKind.Slot
						//|| (container._link_codescope.Kind == CodeScopeKind.Method //&& method.__ismethod
						//)					
						)
					{
						INS_Ld_MemberInitValue ld_MemberInitValue = new INS_Ld_MemberInitValue(scopeMember.trait.Token);
						unsafe
						{

							//最后一条指令肯定是store_scopeheap
							fixed (byte* p = scopeMember.compiler_initvalue)
							{
								byte* PC = p + scopeMember.compiler_initvalue.Length
									 - new INS_END().Size - new INS_Store_ScopeHeap(null).Size;
								;

								int codeanddst = *(int*)PC; PC += 4;

								INS_Code opcode =(INS_Code)( codeanddst & 0xff);
								if (opcode != INS_Code.storeScopeH && opcode != INS_Code.storeMethodVariable)
								{
									throw new InvalidOperationException();
								}
								ScopeHeapLocater heapLocater;
								{
									byte* _p = (byte*)&heapLocater.ScopeIndex;
									*_p++ = *PC++;
									*_p = *PC++;

									_p = (byte*)&heapLocater.MemberIndex;
									*_p++ = *PC++;
									*_p = *PC++;
								}

								ld_MemberInitValue.heap = heapLocater;
							}

						}

						newinstructions.Add(ld_MemberInitValue);
						
					}

					lastindex = st_idx + info_src.instructions -1;
				}
				for (int i = lastindex; i < instructions.Length; i++)
				{
					newinstructions.Add(instructions[i]);					
				}

				method.Body.ByteCode = Assembler.Assemble(useslotcount, constants, newinstructions.ToArray());

				unsafe
				{
					if (!flag_might_shrinked)
					{
						int headersize_dst = sizeof(int) * 3 + 2 * sizeof(int) * instructions.Length + sizeof(NaNBoxing) * constants.Length;
						int headersize_new = sizeof(int) * 3 + 2 * sizeof(int) * newinstructions.Count + sizeof(NaNBoxing) * constants.Length;

						bytecode_shrink.Add(method, oldsize - method.Body.ByteCode.Length 
							+ headersize_new - headersize_dst //需要补齐头部收缩							
							);
					}
				}

				//ASMethodBody.MethodBodyInfo info_dst = new ASMethodBody.MethodBodyInfo();
				//method.Body.GetInfo(ref info_dst);
				//int oldsize = method.Body.ByteCode.Length;

				//unsafe
				//{
				//	int headersize_dst = sizeof(int) * 3 + 2 * sizeof(int) * info_dst.instructions + sizeof(NaNBoxing) * info_dst.constants;

				//	using (System.IO.MemoryStream ms = new MemoryStream())
				//	{
				//		ms.Write(method.Body.ByteCode, 0, headersize_dst); //写入头

				//		int lastpos = 0;

				//		foreach (var scopeMember in container._link_codescope.Members.OrderBy(m => m.compiler_initvalue_stpos)
				//			.Where(m =>  m.DefineAt == container && m.Kind != ScopeMemberKind.Parameter && m.trait.Value != null && m.trait.Value.initValue.HasValue)
				//			)
				//		{
				//			ASMethodBody.MethodBodyInfo info_src = new ASMethodBody.MethodBodyInfo();
				//			ASMethodBody.GetInfo(ref info_src, scopeMember.compiler_initvalue);

				//			int headersize_src = sizeof(int) * 3 + 2 * sizeof(int) * info_src.instructions + sizeof(NaNBoxing) * info_src.constants;
				//			int src_bytecount = scopeMember.compiler_initvalue.Length - headersize_src - new INS_END().Size; //(减去最后一个END指令)


				//			if (scopeMember.compiler_initvalue_stpos - shrinked > lastpos)
				//			{
				//				ms.Write(method.Body.ByteCode, lastpos + headersize_dst, scopeMember.compiler_initvalue_stpos - shrinked - lastpos);
				//			}

				//			if (scopeMember.Kind == ScopeMemberKind.Slot 
				//				//|| (container._link_codescope.Kind == CodeScopeKind.Method //&& method.__ismethod
				//				//)					
				//				)
				//			{
				//				INS_Ld_MemberInitValue ld_MemberInitValue = new INS_Ld_MemberInitValue(scopeMember.trait.Token);

				//				//最后一条指令肯定是store_scopeheap
				//				fixed (byte* p = scopeMember.compiler_initvalue)
				//				{
				//					byte* PC = p + scopeMember.compiler_initvalue.Length
				//						 - new INS_END().Size - new INS_Store_ScopeHeap(null).Size;
				//					;

				//					INS_Code opcode = (INS_Code)(*PC++);
				//					if (opcode != INS_Code.storeScopeH)
				//					{
				//						throw new InvalidOperationException();
				//					}
				//					ScopeHeapLocater heapLocater;
				//					{
				//						byte* _p = (byte*)&heapLocater.ScopeIndex;
				//						*_p++ = *PC++;
				//						*_p = *PC++;

				//						_p = (byte*)&heapLocater.MemberIndex;
				//						*_p++ = *PC++;
				//						*_p = *PC++;
				//					}

				//					ld_MemberInitValue.heap = heapLocater;
				//				}
				//				using (System.IO.MemoryStream tempms = new MemoryStream())
				//				{
				//					using (BinaryWriter bw = new BinaryWriter(tempms))
				//					{
				//						ld_MemberInitValue.Write(bw);
				//					}

				//					var temp = tempms.ToArray();
				//					ms.Write(temp);
				//				}

				//			}

				//			lastpos = scopeMember.compiler_initvalue_stpos - shrinked + src_bytecount;
				//		}

				//		ms.Write(method.Body.ByteCode, lastpos + headersize_dst, method.Body.ByteCode.Length - lastpos - headersize_dst);

				//		method.Body.ByteCode = ms.ToArray();




				//		if (!flag_might_shrinked)
				//		{
				//			bytecode_shrink.Add(method, oldsize - method.Body.ByteCode.Length);
				//		}
				//	}
				//}


			}

			//由于可能收缩了bytecode,需要重新计算跳转
			foreach (var item in bytecode_shrink.Keys)
			{
				ComputeJump(item,false);
			}


		}



		private static void ComputeFunctionDefaultValue(CompileContext context, string workDir, List<string> libs, string outswcfile, Dictionary<ASMethod, byte[]> dict_scriptinit_onlyconst)
		{
			
			var testCode = SWCWriter.Encode(context, System.IO.Path.GetFileName(outswcfile) == "juice_global.swc" ? Path.GetFileName(outswcfile) : Guid.NewGuid().ToString());

			juicescript.runtime.Player computeplayer = new Player(int.MaxValue, true);
			SWCFile testswc = computeplayer.LoadLib(testCode);
			try
			{
				foreach (string lib in libs)
				{
					var libData = System.IO.File.ReadAllBytes(System.IO.Path.Combine(workDir, lib));
					computeplayer.LoadLib(libData);
				}

				computeplayer.PrepareComputeConstExpr();
			}
			catch (System.IO.IOException e)
			{
				throw new CompilerLoadLibException(e.Message);
			}
			catch (LoaderException e)
			{
				throw new CompilerLoadLibException(e.Message);
			}

			foreach (var script in context.scriptDefs)
			{
				for (int i = 1; i < script.scriptMethods.Count; i++)
				{
					ASMethod method = script.scriptMethods[i];
					if (method.Parameters.Count > 0)
					{

						var testMethod = testswc.Methods.Skip(1).First(
							t => t.Token != null && t.Token.line == method.Token.line && t.Token.sourceFileFullPath == method.Token.sourceFileFullPath
							&& t.Token.ptr == method.Token.ptr
						);

						if (testMethod.Body.QName != method.Body.QName

							)
						{
							throw new InvalidOperationException();
						}

						List<NaNBoxing> constants = new List<NaNBoxing>();

						for (int a = 0; a < method.Parameters.Count; a++)
						{
							var para = method.Parameters[a];
							if (para.IsOptional)
							{
								//para.compute_constants = null;

								ASMethod _temp = new ASMethod(testMethod.Body, method.Token);
								_temp.Body = new ASMethodBody(_temp);
								_temp.ReturnType = para.Type;
								_temp.ReturnTypeKind = para.TypeKind;
								_temp.Body.ByteCode = para.computeDefaultValue;

								_temp.Body._link_codescope = new CodeScope();
								_temp.Body._link_codescope.Members = new List<ScopeMember>();
								_temp.Body._link_codescope.Kind = (CodeScopeKind)255;
								_temp.Body._link_codescope.ParameterCout = 0;
								_temp.Body._link_codescope.NamespaceSet = testMethod.Body._link_codescope.NamespaceSet;


								_temp.Body._link_codescope.index = testMethod.Body._link_codescope.index;
								_temp.Body._link_codescope.ParameterCout = 0;
								_temp.Body._link_codescope.Container = testMethod.Body._link_codescope.Container;
								_temp.Body._link_codescope.Parent = testMethod.Body._link_codescope.Parent;



								try
								{

									//默认值只考虑基本数据类型,或者String。
									NaNBoxing value = computeplayer.ComputeConstExpr(_temp, testswc, para.compute_result_index);
									if (value.ValueType == NaNBoxing.BoxType.HeapPtr)
									{
										var obj = computeplayer.Context.GC.Heap[value.HeapPtr];
										if (obj.TypeKind == RtHeapTypeKind.STRING)
										{
											string str = ((RtPayloadString)obj.facility).Str;
											int k = constants.FindIndex(
												(n) => n.ValueType == NaNBoxing.BoxType.HeapPtr
												&&
												n.HeapPtr >> 24 == (byte)ASMethodBody.PoolHeapPtrKind.String
												&&
												context.player_for_compiler.Context.GC.Heap[n.HeapPtr & 0xffffff].TypeKind == RtHeapTypeKind.STRING
												&&
												string.CompareOrdinal(str, ((RtPayloadString)context.player_for_compiler.Context.GC.Heap[n.HeapPtr & 0xffffff].facility).Str) == 0
												);

											if (k >= 0)
											{
												para.ValueExprIndex = k;
											}
											else
											{
												int heapptr = context.player_for_compiler.Context.GC.Complie_AllocString(str);
												if (heapptr == 0)
													throw new InvalidOperationException();
												if (heapptr > 0xffffff)
												{
													throw new ParseException("heapptr > 0xffffff");
												}

												int ptr = (0xffffff & heapptr) | ((byte)ASMethodBody.PoolHeapPtrKind.String << 24);

												NaNBoxing boxing = new NaNBoxing();
												boxing.SetHeapPtr(ptr);

												constants.Add(boxing);
												para.ValueExprIndex = constants.Count - 1;
											}

										}
										else
										{
											throw new ResolverException(method.Token, "Parameter initializer unknown or is not a compile - time constant.");
										}
									}
									else if (value.ValueType == NaNBoxing.BoxType.LocalString)
									{
										string str = value.LocalStringValue;
										int k = constants.FindIndex(
											(n) => n.ValueType == NaNBoxing.BoxType.HeapPtr
											&&
											n.HeapPtr >> 24 == (byte)ASMethodBody.PoolHeapPtrKind.String
											&&
											context.player_for_compiler.Context.GC.Heap[n.HeapPtr & 0xffffff].TypeKind == RtHeapTypeKind.STRING
											&&
											string.CompareOrdinal(str, ((RtPayloadString)context.player_for_compiler.Context.GC.Heap[n.HeapPtr & 0xffffff].facility).Str) == 0
											);

										if (k >= 0)
										{
											para.ValueExprIndex = k;
										}
										else
										{
											int heapptr = context.player_for_compiler.Context.GC.Complie_AllocString(str);
											if (heapptr == 0)
												throw new InvalidOperationException();
											if (heapptr > 0xffffff)
											{
												throw new ParseException("heapptr > 0xffffff");
											}

											int ptr = (0xffffff & heapptr) | ((byte)ASMethodBody.PoolHeapPtrKind.String << 24);

											NaNBoxing boxing = new NaNBoxing();
											boxing.SetHeapPtr(ptr);

											constants.Add(boxing);
											para.ValueExprIndex = constants.Count - 1;
										}
									}
									else if (value.ValueType == NaNBoxing.BoxType.Fault)
									{
										continue;
									}
									else
									{
										int k = constants.FindIndex((n) => n.Raw == value.Raw);
										if (k >= 0)
										{
											para.ValueExprIndex = k;
										}
										else
										{
											constants.Add(value);
											para.ValueExprIndex = constants.Count - 1;
										}

									}

								}
								catch (EvalConstException ex)
								{
									throw new ResolverException(method.Token, ex.Message);
								}
								finally
								{
									//CompileEnv compileEnv = new CompileEnv(null, null, null, context);
									//compileEnv.Constants = constants;
									para.computeDefaultValue = new byte[0];
									//para.computeDefaultValue = originParaBytes[para];
								}
							}
						}


						if (constants.Count > 0)
						{
							CompileEnv compileEnv = new CompileEnv(null, null, null, context);
							compileEnv.Constants = constants;
							method.Body.param_defaultvalues = compileEnv.Encode();

							//method.Parameters[0].compute_constants = constants;

						}

					}
				}
			}
		}


		internal static HashSet<ASTrait> GetMethodImports(ScriptDef script, ASMethod method, CompileContext context)
		{
			HashSet<ASTrait> imports;

			List<ASTrait> container_def_ns = new List<ASTrait>();

			bool inpackagemethod = (method.Flags & MethodFlags.PackageMemberScope) == MethodFlags.PackageMemberScope;
			var container = method.Container;

			while (container is ASMethodBody)
			{
				container_def_ns.AddRange(
					container.Traits.Where(t => (t.Kind == TraitKind.Constant && t.ValueKind == ConstantKind.Namespace)
						||
						t.Kind == TraitKind.Class
					)
					);

				if ((((ASMethodBody)container).Method.Flags & MethodFlags.PackageMemberScope) == MethodFlags.PackageMemberScope)
				{
					inpackagemethod = true;
				}
				container = ((ASMethodBody)container).Method.Container;
			}

			container_def_ns.AddRange(
					container.Traits.Where(t => (t.Kind == TraitKind.Constant && t.ValueKind == ConstantKind.Namespace)
						||
						t.Kind == TraitKind.Class
					)
					);

			if (container is ASScript)
			{
				{
					imports =
						inpackagemethod ?
						context.scriptDef_packageimports[script] :
						context.scriptDef_scriptimports[script];
				}

				if (!inpackagemethod)
				{
					foreach (var item in container_def_ns)
					{
						if(//(item.QName.Namespace.Name == "" && item.QName.Namespace.Kind == NamespaceKind.Package)
							//||
							item != container.Traits[0]
							)
							imports.Add(item);
						
					}
				}




			}
			else if (container is ASClass || container is ASInstance)
			{
				imports =
					container.QName.Namespace.Kind == NamespaceKind.Private ?
						context.scriptDef_scriptimports[script] :
						context.scriptDef_packageimports[script];

				if (container.QName.Namespace.Kind == NamespaceKind.Private)
				{
					foreach (var item in container_def_ns)
					{
						imports.Add(item);
					}
				}
				else
				{

				}

			}
			else
			{
				throw new InvalidOperationException();
			}

			if (context.player_for_compiler.Context.VECTOR != null)
			{
				imports.Add(context.player_for_compiler.Context.VECTOR._link_codescope.Parent.Container.Traits[0]);
			}


			return imports;

		}


		private static void ComputeJump(ASMethod method,bool removeflag)
		{
			//调整跳转。
			int useslotcount; NaNBoxing[] constants; Instruction[] instructions;
			Disassembler.Disassemble(method.Body.ByteCode, out useslotcount, out constants, out instructions);

			Stack< Tuple< INS_Try_Enter, List<int>>> try_stack = new Stack<Tuple<INS_Try_Enter, List<int>>>();

			bool has_try_stmt = false;

			int offset = 0;
			for (int i = 0; i < instructions.Length; i++)
			{
				var instruction = instructions[i];
				if (instruction.INS_Code == INS_Code.try_enter)
				{
					has_try_stmt = true;
					try_stack.Push(new Tuple<INS_Try_Enter, List<int>>((INS_Try_Enter)instruction, new List<int>()));
				}
				else if (instruction.INS_Code == INS_Code.catch_enter)
				{
					var try_enter = try_stack.Peek();
					try_enter.Item2.Add(offset);
				}
				else if (instruction.INS_Code == INS_Code.finally_enter)
				{
					var try_enter = try_stack.Peek();
					try_enter.Item1.finally_pc = offset;
				}
				else if (instruction.INS_Code == INS_Code.finally_exit)
				{
					var try_enter = try_stack.Pop();
					try_enter.Item1.finally_exit_pc = offset;

#if DEBUG
					if (try_enter.Item1.catch_pc.Length != try_enter.Item2.Count)
						throw new InvalidOperationException();
#endif
					try_enter.Item1.catch_pc = try_enter.Item2.ToArray();

					
				}

				if (!removeflag || (instruction.INS_Code != INS_Code.flag && removeflag))
				{
					offset += instruction.Size;
				}
			}
#if DEBUG
			if (try_stack.Count != 0)
			{
				throw new InvalidOperationException();
			}
#endif

			if (!has_try_stmt)
			{
				method.Flags |= MethodFlags.NoTry;
			}


			for (int i = 0; i < instructions.Length; i++)
			{
				var instruction = instructions[i];
				if (instruction.INS_Code == INS_Code.goto_flag)
				{
					INS_Goto @goto = (INS_Goto)instruction;

					int flagid = @goto.flag_id;

					bool found = false;

					offset = 0;
					for (int j = 0; j < instructions.Length; j++)
					{
						if (instructions[j].INS_Code == INS_Code.flag && ((INS_Flag)instructions[j]).flag_id == flagid)
						{
							//@goto.jumpTrys =  - @goto.jumpTrys -1;
							@goto.jumpOffset = offset;

							if (offset > 0xfffff8)
							{
								throw new ResolverException(instructions[j].token, "jumpoffset too large" );
							}
							found = true;
							break;
						}
						else if (!removeflag || (instructions[j].INS_Code != INS_Code.flag && removeflag))
						{
							offset += instructions[j].Size;
						}
					}

					if (!found)
					{
						throw new InvalidOperationException();
					}

				}

				if (instruction.INS_Code == INS_Code.if_false_goto)
				{
					INS_If_False_Goto if_false_goto = (INS_If_False_Goto)instruction;
					
					int flagid = if_false_goto.flag_id ;

					bool found = false;
					offset = 0;
					for (int j = 0; j < instructions.Length; j++)
					{
						if (instructions[j].INS_Code == INS_Code.flag && ((INS_Flag)instructions[j]).flag_id == flagid)
						{
							found = true;
							if_false_goto.offset = offset;
							break;
						}
						else if (!removeflag || (instructions[j].INS_Code != INS_Code.flag && removeflag))
						{
							offset += instructions[j].Size;
						}
					}

					if (!found)
					{
						throw new InvalidOperationException();
					}
				}

				if (instruction.INS_Code == INS_Code.if_true_goto)
				{
					INS_If_True_Goto if_true_goto = (INS_If_True_Goto)instruction;

					int flagid = if_true_goto.flag_id;

					bool found = false;
					offset = 0;
					for (int j = 0; j < instructions.Length; j++)
					{
						if (instructions[j].INS_Code == INS_Code.flag && ((INS_Flag)instructions[j]).flag_id == flagid)
						{
							found = true;
							if_true_goto.offset = offset;
							break;
						}
						else if (!removeflag || (instructions[j].INS_Code != INS_Code.flag && removeflag))
						{
							offset += instructions[j].Size;
						}
					}

					if (!found)
					{
						throw new InvalidOperationException();
					}
				}

				if (instruction.INS_Code == INS_Code.if_logicOp_goto)
				{
					INS_If_LogicOp_Goto if_logicOp_goto = (INS_If_LogicOp_Goto)instruction;

					int flagid = if_logicOp_goto.flag_id;

					bool found = false;
					offset = 0;
					for (int j = 0; j < instructions.Length; j++)
					{
						if (instructions[j].INS_Code == INS_Code.flag && ((INS_Flag)instructions[j]).flag_id == flagid)
						{
							found = true;
							if_logicOp_goto.offset = offset;
							break;
						}
						else if (!removeflag || (instructions[j].INS_Code != INS_Code.flag && removeflag))
						{
							offset += instructions[j].Size;
						}
					}

					if (!found)
					{
						throw new InvalidOperationException();
					}
				}


				
				if (instruction.INS_Code == INS_Code.iter_get)
				{ 
					INS_Iter_Get iter_Get = (INS_Iter_Get)instruction;
					int flagid = iter_Get.flag_end_id;
					bool found = false;
					offset = 0;
					for (int j = 0; j < instructions.Length; j++)
					{
						if (instructions[j].INS_Code == INS_Code.flag && ((INS_Flag)instructions[j]).flag_id == flagid)
						{
							found = true;
							iter_Get.flag_offset = offset;
							break;
						}
						else if (!removeflag || (instructions[j].INS_Code != INS_Code.flag && removeflag))
						{
							offset += instructions[j].Size;
						}
					}

					if (!found)
					{
						throw new InvalidOperationException();
					}
				}

				if (instruction.INS_Code == INS_Code.iter_next)
				{
					INS_Iter_Next iter_next = (INS_Iter_Next)instruction;
					int flagid = iter_next.flag_next_end_id;
					bool found = false;
					offset = 0;
					for (int j = 0; j < instructions.Length; j++)
					{
						if (instructions[j].INS_Code == INS_Code.flag && ((INS_Flag)instructions[j]).flag_id == flagid)
						{
							found = true;
							iter_next.flag_offset = offset;
							break;
						}
						else if (!removeflag || (instructions[j].INS_Code != INS_Code.flag && removeflag))
						{
							offset += instructions[j].Size;
						}
					}

					if (!found)
					{
						throw new InvalidOperationException();
					}
				}


			}


			if (removeflag)
			{
				var temp = instructions.ToList();
				temp.RemoveAll(i => i.INS_Code == INS_Code.flag);
				instructions = temp.ToArray();
			}
			method.Body.ByteCode = Assembler.Assemble(useslotcount,constants,instructions);
		
		}

		private static void BuildScript(ScriptDef script, AS3SrcFile as_srcfile, CompileContext context, string sfile, string hash, Dictionary<ASMethod, byte[]> dict_scriptinit_onlyconst)
		{
			context.buildingScript = script;
			//查找源码对应的AS3ClassInterfaceBase  
			var as3_srclist = ((new AS3ClassInterfaceBase[] { as_srcfile.Package.MainClass, as_srcfile.Package.MainInterface }).Union
				(
					as_srcfile.OutPackage.outpackage_classes_interfaces
				)).Where((a) => a != null).ToArray();

			List<List<ScopeMember>> list_initedscopeMembers = new List<List<ScopeMember>>();

			for (int i = 1; i < script.scriptMethods.Count; i++)
			{
				List<ScopeMember> inited_scopeMembers = new List<ScopeMember>();
				list_initedscopeMembers.Add(inited_scopeMembers);

				ASMethod method = script.scriptMethods[i];

				var cls = script.scriptClasses.FirstOrDefault((c) => c != null && c.Constructor == method);
				var instance = script.scriptClasses.Where((s) => s != null).Select((s) => s.Instance).FirstOrDefault((c) => c != null && c.Constructor == method);
				if (cls != null)
				{
					/*
					* 编译ASClass的初始化代码
					*/
					var imports = GetMethodImports(script, cls.Constructor, context);

					var as3_src = as3_srclist.First((a) => a.Name == cls.QName.Name);
					IL.CompileEnv compileEnv = new IL.CompileEnv(cls.Constructor.Body._link_codescope,
						imports,
						(as3_src is AS3Class) ? ((AS3Class)as3_src).CInitCodes : new List<IAS3SyntaxNode>()

						, context);

					IL.ILBuilder.Build(compileEnv);

					method.Body.ByteCode = compileEnv.Encode();

					context.dict_method_constants.Add(method, compileEnv.Constants);

					for (int j = 0; j < compileEnv.initvalue_instructions.Count; j++)
					{
						var init = compileEnv.initvalue_instructions[j];
						if (init.member.DefineAt != cls)
						{
							throw new InvalidOperationException();
						}

						CompileEnv encoder = new CompileEnv(null, null, null, context, compileEnv);
						encoder.instructions.Clear();
						encoder.initvalue_instructions.Clear();
						encoder.instructions.AddRange(init.setValueInstructions);
						encoder.instructions.Add(new INS_END());

						init.member.compiler_initvalue = encoder.Encode();
						//init.member.compiler_initvalue_stpos = init.start_byte_pos;
						init.member.compiler_initvalue_stpos = init.FindStartBytePos(compileEnv);


						inited_scopeMembers.Add(init.member);

					}


				}
				else if (instance != null)
				{
					/*
					 *  类的构造函数，先编译成员赋值代码，然后再编译定义的构造函数代码
					 */
					var imports = GetMethodImports(script, method, context);
					var as3_src = as3_srclist.First((a) => a.Name == instance.QName.Name);

					if (method.ast_function_index > -1)
					{
						//有构造函数的情况
						if (method.Body._link_codescope.Parent != instance.Constructor.Container._link_codescope)
						{
							throw new InvalidOperationException();
						}

						var function = as_srcfile._functions[method.ast_function_index];

						CompileEnv compileEnv = new CompileEnv(instance._link_codescope, imports,
							((AS3Class)as3_src).Codes
							, context);
						ILBuilder.Build(compileEnv);

						var instance_initmember = compileEnv;


						//如果构造函数一行代码也没有，则自动合成一行super()
						if (function.FunctionScope.Codes.Count == 0)
						{
							if (method.Flags.HasFlag(MethodFlags.Native))
							{

							}
							else
							{
								AS3Expression expression = new AS3Expression(function.Token);
								expression.exprStepList = new List<AST.Expr.AS3ExprStep>();
								expression.exprStepList.Add(new AST.Expr.AS3ExprStep(function.Token)
								{
									Type = AST.Expr.OpType.CallFunc,
									Arg1 = new AST.Expr.AS3DataStackElement()
									{
										IsReg = true,
										Reg = new AST.Expr.AS3Reg(0)
									},
									Arg2 = new AST.Expr.AS3DataStackElement()
									{
										IsReg = false,
										Data = new AST.Expr.AS3DataValue(function.Token)
										{
											FF1Type = AST.Expr.FF1DataValueType.super_pointer
										}

									}
								});

								function.FunctionScope.Codes.Add(expression);
							}
						}

						compileEnv = new CompileEnv(method.Body._link_codescope, imports, function.FunctionScope.Codes, context, compileEnv);
						compileEnv.initvalue_instructions.Clear();
						ILBuilder.Build(compileEnv);

						method.Body.ByteCode = compileEnv.Encode();
						context.dict_method_constants.Add(method, compileEnv.Constants);


						for (int j = 0; j < instance_initmember.initvalue_instructions.Count; j++)
						{
							var init = instance_initmember.initvalue_instructions[j];
							if (init.member.DefineAt != instance)
							{
								throw new InvalidOperationException();
							}
							CompileEnv encoder = new CompileEnv(null, null, null, context, compileEnv);

							encoder.instructions.Clear();
							encoder.initvalue_instructions.Clear();
							encoder.instructions.AddRange(init.setValueInstructions);
							encoder.instructions.Add(new INS_END());

							init.member.compiler_initvalue = encoder.Encode();
							//init.member.compiler_initvalue_stpos = init.start_byte_pos;
							init.member.compiler_initvalue_stpos = init.FindStartBytePos(compileEnv);

							inited_scopeMembers.Add(init.member);
						}

						//构造函数本身
						for (int j = 0; j < compileEnv.initvalue_instructions.Count; j++)
						{
							var init = compileEnv.initvalue_instructions[j];
							if (init.member.DefineAt != instance.Constructor.Body._link_codescope.Container)
							{
								throw new InvalidOperationException();
							}
							CompileEnv encoder = new CompileEnv(null, null, null, context, compileEnv);

							encoder.instructions.Clear();
							encoder.initvalue_instructions.Clear();
							encoder.instructions.AddRange(init.setValueInstructions);
							encoder.instructions.Add(new INS_END());

							init.member.compiler_initvalue = encoder.Encode();
							//init.member.compiler_initvalue_stpos = init.start_byte_pos;
							init.member.compiler_initvalue_stpos = init.FindStartBytePos(compileEnv);

							inited_scopeMembers.Add(init.member);

						}


					}
					else
					{

						//无构造函数的情况

						IL.CompileEnv compileEnv = new IL.CompileEnv(instance._link_codescope,
							imports,
							(as3_src is AS3Class) ? ((AS3Class)as3_src).Codes : new List<IAS3SyntaxNode>()

							, context);

						IL.ILBuilder.Build(compileEnv);

						if (!(instance.QName.Name == "Object" && instance.QName.Namespace.Kind == NamespaceKind.Package && instance.QName.Namespace.Name == ""))
						{
							//合成一行super()		
							AS3Expression expression = new AS3Expression(as3_src.Token);
							expression.exprStepList = new List<AST.Expr.AS3ExprStep>();
							expression.exprStepList.Add(new AST.Expr.AS3ExprStep(as3_src.Token)
							{
								Type = AST.Expr.OpType.CallFunc,
								Arg1 = new AST.Expr.AS3DataStackElement()
								{
									IsReg = true,
									Reg = new AST.Expr.AS3Reg(0)
								},
								Arg2 = new AST.Expr.AS3DataStackElement()
								{
									IsReg = false,
									Data = new AST.Expr.AS3DataValue(as3_src.Token)
									{
										FF1Type = AST.Expr.FF1DataValueType.super_pointer
									}
								}
							});

							compileEnv = new CompileEnv(method.Body._link_codescope, imports, new List<IAS3SyntaxNode>() { expression }, context, compileEnv);
							ILBuilder.Build(compileEnv);
						}

						method.Body.ByteCode = compileEnv.Encode();
						context.dict_method_constants.Add(method, compileEnv.Constants);

						for (int j = 0; j < compileEnv.initvalue_instructions.Count; j++)
						{
							var init = compileEnv.initvalue_instructions[j];
							if (init.member.DefineAt != instance)
							{
								throw new InvalidOperationException();
							}

							CompileEnv encoder = new CompileEnv(null, null, null, context, compileEnv);
							encoder.instructions.Clear();
							encoder.initvalue_instructions.Clear();
							encoder.instructions.AddRange(init.setValueInstructions);
							encoder.instructions.Add(new INS_END());

							init.member.compiler_initvalue = encoder.Encode();
							//init.member.compiler_initvalue_stpos = init.start_byte_pos;
							init.member.compiler_initvalue_stpos = init.FindStartBytePos(compileEnv);

							inited_scopeMembers.Add(init.member);
						}
					}

				}
				else if (method == script.Script.Initializer)
				{
					/*
					 * 编译script的初始化代码
					 */

					var script_initialzier_codescope = method.Body._link_codescope;


					var imports = GetMethodImports(script, script.Script.Initializer, context);

					IL.CompileEnv compileEnv = new IL.CompileEnv(script_initialzier_codescope, imports, as_srcfile.OutPackage.Codes, context);
					IL.ILBuilder.Build(compileEnv);
					method.Body.ByteCode = compileEnv.Encode();

					context.dict_method_constants.Add(method, compileEnv.Constants);

					for (int j = 0; j < compileEnv.initvalue_instructions.Count; j++)
					{
						var init = compileEnv.initvalue_instructions[j];
						if (init.member.DefineAt != script.Script)
						{
							throw new InvalidOperationException();
						}

						CompileEnv encoder = new CompileEnv(null, null, null, context, compileEnv);
						encoder.instructions.Clear();
						encoder.initvalue_instructions.Clear();
						encoder.instructions.AddRange(init.setValueInstructions);
						encoder.instructions.Add(new INS_END());

						init.member.compiler_initvalue = encoder.Encode();
						//init.member.compiler_initvalue_stpos = init.start_byte_pos;
						init.member.compiler_initvalue_stpos = init.FindStartBytePos(compileEnv);


						inited_scopeMembers.Add(init.member);

					}

				}
				else
				{
					if (as_srcfile._functions[method.ast_function_index] == null)
						throw new InvalidOperationException();

					//****
					if (method.Flags.HasFlag(MethodFlags.Native))
					{
						context.dict_method_constants.Add(method, new List<NaNBoxing>());
						method.Body.ByteCode = new byte[12];
					}
					else
					{
						var imports = GetMethodImports(script, method, context);

						var as3Fscope = as_srcfile._functions[method.ast_function_index].FunctionScope;

						CompileEnv compileEnv = new CompileEnv(method.Body._link_codescope, imports,
							as3Fscope.Codes
							, context);

						var catch_vars = as3Fscope.catch_variables;
						

						compileEnv.parent_catching_variable = new List<AS3Variable> (catch_vars);
						


						ILBuilder.Build(compileEnv);
						method.Body.ByteCode = compileEnv.Encode();
						context.dict_method_constants.Add(method, compileEnv.Constants);


						for (int j = 0; j < compileEnv.initvalue_instructions.Count; j++)
						{
							var init = compileEnv.initvalue_instructions[j];


							CompileEnv encoder = new CompileEnv(null, null, null, context, compileEnv);
							encoder.instructions.Clear();
							encoder.initvalue_instructions.Clear();
							encoder.instructions.AddRange(init.setValueInstructions);
							encoder.instructions.Add(new INS_END());

							init.member.compiler_initvalue = encoder.Encode();
							//init.member.compiler_initvalue_stpos = init.start_byte_pos;
							init.member.compiler_initvalue_stpos = init.FindStartBytePos(compileEnv);

							inited_scopeMembers.Add(init.member);
						}


					}
				}

				//检查特殊函数如Generator等，是否满足条件
				{
					if (method.Flags.HasFlag(MethodFlags.Generator))
					{
						int useslotcount; NaNBoxing[] constants; Instruction[] instructions;
						Disassembler.Disassemble(method.Body.ByteCode, out useslotcount, out constants, out instructions);


						if (method.Flags.HasFlag(MethodFlags.NeedArguments))
						{
							throw new ResolverException(new Token() {
								sourceFile = method.Token.sourceFile,
								sourceFileFullPath = method.Token.sourceFileFullPath,
								line = instructions.First(i => i.INS_Code == INS_Code.ld_arguments).token.line ,
							     ptr = instructions.First(i => i.INS_Code == INS_Code.ld_arguments).token.ptr
							},

							"arguments not allow in generator function");
						}


						if (instructions.Any(
							i =>
							//i.INS_Code == INS_Code.return_op ||
							i.INS_Code == INS_Code.return_value || i.INS_Code == INS_Code.return_void))
						{
							throw new ResolverException(new Token()
							{
								sourceFile = method.Token.sourceFile,
								sourceFileFullPath = method.Token.sourceFileFullPath,
								line = instructions.First(i => //i.INS_Code == INS_Code.return_op ||
															   i.INS_Code == INS_Code.return_value || i.INS_Code == INS_Code.return_void).token.line,
								ptr = instructions.First(i => //i.INS_Code == INS_Code.return_op || 
																i.INS_Code == INS_Code.return_value || i.INS_Code == INS_Code.return_void).token.ptr
							},

							"return not allow in generator function");
						}
					}

					if(method.Flags.HasFlag( MethodFlags.ASYNC))
					{
						int useslotcount; NaNBoxing[] constants; Instruction[] instructions;
						Disassembler.Disassemble(method.Body.ByteCode, out useslotcount, out constants, out instructions);

						if (method.Flags.HasFlag(MethodFlags.NeedArguments))
						{
							throw new ResolverException(new Token()
							{
								sourceFile = method.Token.sourceFile,
								sourceFileFullPath = method.Token.sourceFileFullPath,
								line = instructions.First(i => i.INS_Code == INS_Code.ld_arguments).token.line,
								ptr = instructions.First(i => i.INS_Code == INS_Code.ld_arguments).token.ptr
							},

							"arguments not allow in async function");
						}


						//if (instructions.Any(
						//	i =>
						//	//i.INS_Code == INS_Code.return_op ||
						//	i.INS_Code == INS_Code.return_value || i.INS_Code == INS_Code.return_void))
						//{
						//	throw new InvalidOperationException();
						//}

						//**todo 检查是否所有路径都有return_promise返回值。


					}
				}

				



				ComputeJump(method,false);

				//编译给默认参数求值的临时代码。
				for (int a = 0; a < method.Parameters.Count; a++)
				{
					var para = method.Parameters[a];
					if (para.IsOptional)
					{
						var defaultvalue = as_srcfile._expressions[para.ValueExprIndex];

						defaultvalue.exprStepList.Add(
							new AST.Expr.AS3ExprStep(defaultvalue.Token)
							{
								Type = AST.Expr.OpType.Assigning,
								OpCode = "ComputeDefault",
								Arg2 = defaultvalue.Value,
								Arg3 = new AST.Expr.AS3DataStackElement() { Data = new AST.Expr.AS3DataValue(null) { Value = para.TypeKind } }
							}
							);
						defaultvalue.Value = defaultvalue.exprStepList[defaultvalue.exprStepList.Count - 1].Arg1;

						var imports = GetMethodImports(script, method, context);
						var code = new List<IAS3SyntaxNode>() { defaultvalue };

						CompileEnv compileEnv = new CompileEnv(method.Body._link_codescope, imports,
							code
							, context);

						List<NaNBoxing> constants = context.dict_method_constants[method];
						compileEnv.Constants = new List<NaNBoxing>(); //constants.ToList();

						context.computeConstExprState.Push(new object());
						try
						{
							ILBuilder.Build(compileEnv);
						}
						finally
						{
							context.computeConstExprState.Pop();
						}


						para.compute_result_index = ((INS_Ld_ValueRef)compileEnv.instructions[compileEnv.instructions.Count - 2]).dst.index;
						//para.compute_constants = compileEnv.Constants;
						para.computeDefaultValue = compileEnv.Encode();

					}

				}

			}

			context.buildingScript = null;

			using (System.IO.FileStream fs = new FileStream(sfile, FileMode.Create))
			{
				using (BinaryWriter bw = new BinaryWriter(fs))
				{
					bw.Write(hash);

					for (int i = 1; i < script.scriptMethods.Count; i++)
					{
						
						ASMethod method = script.scriptMethods[i];


						//写入.m文件
						bw.Write((int)method.Flags);
						var constansts = context.dict_method_constants[method];
						bw.Write(constansts.Count);

						var writer = (NaNBoxing box, BinaryWriter bw) =>
						{
							bw.Write(box.Raw);
							if (box.ValueType == NaNBoxing.BoxType.HeapPtr)
							{
								var p = box.HeapPtr;
								ASMethodBody.PoolHeapPtrKind kind = (ASMethodBody.PoolHeapPtrKind)(p >> 24);
								int ptr = p & 0xFFFFFF;

								if (kind == ASMethodBody.PoolHeapPtrKind.VectorDef)
								{
									var vecdef = context.vectorDefs[ptr];

									bw.Write((byte)RtHeapTypeKind.VECTOR);
									bw.Write(vecdef.depth);

									while (vecdef.depth > 0)
									{
										vecdef = context.vectorDefs.First(v => v.Identifier == vecdef.ElementTypeId);
									}
									bw.Write((ulong)vecdef.ElementTypeId);
								}
								else if (kind == ASMethodBody.PoolHeapPtrKind.LD_Class)
								{
									//bw.Write((byte)RtHeapTypeKind.CACHE_LD_CLASS);
									bw.Write((byte)10);
									bw.Write( context.constpool_ldclass[ptr]);
								}
								else
								{
									RtHeapInstance heapInstance = context.player_for_compiler.Context.GC.Heap[ptr];
									if (heapInstance.TypeKind == RtHeapTypeKind.STRING && kind == ASMethodBody.PoolHeapPtrKind.String)
									{
										bw.Write((byte)RtHeapTypeKind.STRING);

										var chars = ((RtPayloadString)heapInstance.facility).Str.AsSpan();
										bw.Write(chars.Length);
										for (int i = 0; i < chars.Length; i++)
										{
											bw.Write((ushort)chars[i]);
										}

										//bw.Write(((RtPayloadString)heapInstance.facility).Str);
									}
									//else if (heapInstance.TypeKind == RtHeapTypeKind.CACHE_LD_CLASS && kind == ASMethodBody.PoolHeapPtrKind.LD_Class)
									//{
									//	bw.Write((byte)RtHeapTypeKind.CACHE_LD_CLASS);
									//	bw.Write(((ASClass)heapInstance.Type).Type_identifier);
									//}
									else if (heapInstance.TypeKind == RtHeapTypeKind.NAMESPACE && kind == ASMethodBody.PoolHeapPtrKind.Namespace)
									{
										bw.Write((byte)RtHeapTypeKind.NAMESPACE);
										int index = script.namespaces.IndexOf(((RtPayloadNameSpace)heapInstance.facility).ASNamespace);
										if (index < 1)
										{
											throw new InvalidOperationException();
										}

										bw.Write(index);

									}
									else if (heapInstance.TypeKind == RtHeapTypeKind.MethodScope && kind == ASMethodBody.PoolHeapPtrKind.Method)
									{
										ASMethod m = ((ASMethodBody)heapInstance.Type).Method;

										int index = script.scriptMethods.IndexOf(m);
										if (index < 1)
										{
											throw new InvalidOperationException();
										}

										bw.Write((byte)RtHeapTypeKind.MethodScope);
										bw.Write(index);
									}
									else if (heapInstance.TypeKind == RtHeapTypeKind.MethodScope && kind == ASMethodBody.PoolHeapPtrKind.SuperMethod)
									{
										ASClass _this_ = (ASClass)heapInstance.Type;
										int vtable_index = ((RtPayloadMethodScope)heapInstance.facility).ParentPtr;


										bw.Write((byte)200);
										bw.Write(_this_.Type_identifier);
										bw.Write(vtable_index);

									}
									else
									{
										throw new InvalidOperationException();
									}
								}
							}
						};

						for (int j = 0; j < constansts.Count; j++)
						{
							NaNBoxing box = constansts[j];
							writer(box, bw);
						}
						bw.Write(method.Body.ByteCode.Length);
						bw.Write(method.Body.ByteCode);

						for (int a = 0; a < method.Parameters.Count; a++)
						{
							var para = method.Parameters[a];
							if (para.IsOptional)
							{
								var compute_constants = ASMethodBody.ReadConstants(para.computeDefaultValue);

								bw.Write(compute_constants.Count);
								for (int j = 0; j < compute_constants.Count; j++)
								{
									NaNBoxing cb = compute_constants[j];
									writer(cb, bw);
								}




								bw.Write(para.compute_result_index);
								bw.Write(para.computeDefaultValue.Length);
								bw.Write(para.computeDefaultValue);
							}

						}


						if (script.Script.Initializer == method || method.IsConstructor)
						{
							//                     bw.Write(dict_scriptinit_onlyconst[method].Length);
							//                     bw.Write(dict_scriptinit_onlyconst[method]);


							//for (int s = 0; s < method.Container._link_codescope.Members.Count; s++)
							//{
							//	var m = method.Container._link_codescope.Members[s];
							//	if (m.compiler_initvalue != null)
							//	{
							//		bw.Write(m.compiler_initvalue.Length);
							//		bw.Write(m.compiler_initvalue);
							//	}
							//	else
							//	{
							//		bw.Write(0);
							//	}
							//}

						}

						var inited_scopeMembers = list_initedscopeMembers[i-1];

						bw.Write(inited_scopeMembers.Count);
						for (int j = 0; j < inited_scopeMembers.Count; j++)
						{
							var scopemember = inited_scopeMembers[j];
							int container_index = script.containers.IndexOf(scopemember.DefineAt);
							int memberid = scopemember.DefineAt._link_codescope.Members.IndexOf(scopemember);
							if (container_index == -1 || memberid == -1)
							{
								throw new InvalidOperationException();
							}

							bw.Write(container_index);
							bw.Write(memberid);

							bw.Write(scopemember.compiler_initvalue.Length);
							bw.Write(scopemember.compiler_initvalue);

							bw.Write(scopemember.compiler_initvalue_stpos);

						}



					}


				}
			}

		}


	}
}
