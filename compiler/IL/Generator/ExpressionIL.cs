using juicescript.ABC;
using juicescript.ABC.INS;
using juicescript.ABC.Locaters;
using juicescript.compiler.AST;
using juicescript.compiler.AST.Expr;
using juicescript.compiler.parse;
using juicescript.runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;

namespace juicescript.compiler.IL.Generator
{
	internal class ExpressionIL
	{

		public enum FindIdResultType
		{
			NotFound,
			Ambiguous,
			ScopeMember,
			NeedLoadClass_ScopeMember,

			VTableItems,
			NeedLoadClass_VTableItems,

		}


		public static FindIdResultType FindIdentifier(string id, ASNamespace match_namespace, CompileEnv env, Token token,
			out ScopeHeapLocater heapLocater, out TypeKind heapType,
			out ASTrait findtrait,
			out CodeScope findScope,
			out ScopeMember findmember,
			out VTableItem[] vTableItems
			)
		{
			var scope = env.Scope;


			Queue<CodeScope> searchQueue = new Queue<CodeScope>();
			CodeScope scope_script = null;

		lbl_start:

			if (scope.Kind == CodeScopeKind.Script)
			{
				searchQueue.Enqueue(scope);
				scope_script = scope;
			}
			else if (scope.Kind == CodeScopeKind.Class)
			{
				searchQueue.Enqueue(scope);

				var @class = ((ASClass)scope.Container).Instance._super_class_;
				while (@class != null)
				{
					searchQueue.Enqueue(@class._link_codescope);
					@class = @class.Instance._super_class_;
				}

				searchQueue.Enqueue(scope.Parent);

				searchQueue.Enqueue(scope.Parent);
				scope_script = scope.Parent; //env.Scope.Parent;
			}
			else if (scope.Kind == CodeScopeKind.Method)
			{
				searchQueue.Enqueue(scope);
				scope = scope.Parent;

				while (scope.Kind == CodeScopeKind.Method)
				{

					searchQueue.Enqueue(scope);
					scope = scope.Parent;
				}

				goto lbl_start;
			}
			else if (scope.Kind == CodeScopeKind.Instance)
			{
				//查找顺序：自身-静态对象-父类静态对象-Script
				searchQueue.Enqueue(scope);
				ASClass @class =
						env.CompileContext.player_for_compiler.Context.libs.SelectMany(o => o.Classes)
						.Union
						(
							env.CompileContext.scriptDefs.SelectMany(o => o.scriptClasses)
						).Where(c => c != null)
									.First(i => i.Instance == scope.Container);

				searchQueue.Enqueue(@class._link_codescope);

				@class = @class.Instance._super_class_;
				while (@class != null)
				{
					searchQueue.Enqueue(@class._link_codescope);
					@class = @class.Instance._super_class_;
				}

				searchQueue.Enqueue(scope.Parent);
				scope_script = scope.Parent;
			}
#if DEBUG
			if (scope_script.Kind != CodeScopeKind.Script)
				throw new InvalidOperationException();
#endif

			// 查找buildin函数
			var toplevel = env.CompileContext.player_for_compiler.Context.libs.SelectMany(s => s.Classes).FirstOrDefault(s => s != null && s.QName.Namespace.Name == "__AS3__" && s.QName.Name == "toplevel");
			if (toplevel != null)
			{
				searchQueue.Enqueue(toplevel._link_codescope);
			}

			while (searchQueue.Count > 0)
			{
				scope = searchQueue.Dequeue();

				var find_linq = scope.Members.Reverse<ScopeMember>().Where(
					(t) =>
					(t.Kind == ScopeMemberKind.Parameter && t.PName == id) //找到参数 或者
					||
					(
						t.Kind != ScopeMemberKind.Parameter &&

						t.QName.Name == id &&
						(
							(env.Scope.NamespaceSet.Namespaces.Contains(t.QName.Namespace) && match_namespace == null)
							||
							(t.QName.Namespace == match_namespace && match_namespace != null)
							||
							(
								match_namespace != null
								&&
								(match_namespace.Kind == NamespaceKind.PackageInternal || match_namespace.Kind == NamespaceKind.Private || match_namespace.Kind == NamespaceKind.Protected)
								&&
								(string.IsNullOrEmpty(match_namespace.Name) || match_namespace.Kind == NamespaceKind.Private)
								&&
								env.Scope.NamespaceSet.Namespaces.Contains(t.QName.Namespace)
								&&
								(t.QName.Namespace.Kind == match_namespace.Kind || (t.QName.Namespace.Kind == NamespaceKind.StaticProtected && match_namespace.Kind == NamespaceKind.Protected))
								&&
								t.QName.Namespace.def_uri == null
								)


							)

						&&
						(
							match_namespace == null
							||
							(
								match_namespace.Kind == t.QName.Namespace.Kind
							)
						//||
						//(
						//    (t.QName.Namespace.Kind == NamespaceKind.Protected || t.QName.Namespace.Kind == NamespaceKind.StaticProtected)
						//    &&
						//    match_namespace.Kind == NamespaceKind.Protected
						//)
						)
						)
					).GroupBy(t => t.DefineAt).FirstOrDefault()
					;
				var find = find_linq == null ? new ScopeMember[0] : find_linq.ToArray();

				if (env.catching_variable.Count > 0) // 当前在catch中
				{
					if (scope == env.Scope) // 拦截在catch中定义的变量
					{
						var v = env.Scope.Members.Reverse<ScopeMember>().FirstOrDefault(v => v.Kind != ScopeMemberKind.Parameter && v.QName.Name.StartsWith($"%{id}%") && v.QName.Name.EndsWith("@--") && v.Kind == ScopeMemberKind.Slot);
						if (v != null)
						{
							var t = v;
							find = new ScopeMember[1] { t };
						}
						else
						{
							//拦截是否访问catch 的变量
							var cv = env.catching_variable.FirstOrDefault(v => v.Name.StartsWith($"%{id}%"));
							if (cv != null)
							{
								var t = env.Scope.Members.Reverse<ScopeMember>().First(t => t.QName.Name == cv.Name && t.Kind == ScopeMemberKind.Slot);
								find = new ScopeMember[1] { t };

							}

						}
					}
				}

				if (find.Length == 0)
				{
					if (scope.Kind == CodeScopeKind.Method)
					{
						var method = ((ASMethodBody)scope.Container).Method;
						if (method.IsAnonymous && method.Name.StartsWith(id + "#anonymous:") && match_namespace == null)
						{
							var t = method.Container._link_codescope.Members.Reverse<ScopeMember>().First(t => t.Kind == ScopeMemberKind.Slot && t.QName.Name == method.Name);

							find = new ScopeMember[1] { t };
							scope = method.Container._link_codescope;
						}

						if (find.Length == 0 && env.parent_catching_variable != null && match_namespace == null)
						{
							if (env.parent_catching_variable.Count > 0 && method.IsAnonymous && method.Name.IndexOf("@--#anonymous:")>0)
							{
								string mn = method.Name.Substring(0, method.Name.IndexOf("@--#anonymous:"));

								if (env.parent_catching_variable.Any(v => v.Name == mn))
								{
									/*这种代码
									 try 
									{
										throw 2;
									}
									catch (e)
									{
										f = function e()  
										{
											return e;
										}   
									}
									 */

									var t = method.Container._link_codescope.Members.Reverse<ScopeMember>().First(t => t.Kind == ScopeMemberKind.Slot && t.QName.Name == method.Name);

									Debug.Assert(t != null);

									find = new ScopeMember[1] { t };
									scope = method.Container._link_codescope;


								}
							}
						}

					}
				}



				if (find.Length == 0 && env.parent_catching_variable != null)
				{
					if (env.parent_catching_variable.Count > 0)
					{
						var v = scope.Members.Reverse<ScopeMember>().FirstOrDefault(
							v => v.Kind != ScopeMemberKind.Parameter && v.Kind == ScopeMemberKind.Slot &&
							env.parent_catching_variable.Any(cv => cv.Name == v.QName.Name)
							&&
							v.QName.Name.StartsWith($"%{id}%")
							);

						if (v != null)
						{
							var t = v;
							find = new ScopeMember[1] { t };
						}
					}
				}


				//if (find.Length == 0 && match_namespace == null)
				//{
				//    if (searchQueue.Count == 0)
				//    {
				//        if (env.catching_variable.Count > 0)
				//        {
				//            var v = env.catching_variable.FirstOrDefault(v => v.Name.StartsWith($"%{id}%"));
				//            if (v != null)
				//            {
				//                var t = env.Scope.Members.Reverse<ScopeMember>().First(t => t.QName.Name == v.Name && t.Kind == ScopeMemberKind.Slot);
				//                find = new ScopeMember[1] { t };
				//                scope = env.Scope;
				//            }
				//        }
				//    }

				//}


				var _findmethods_linq = scope.Container._vtable.Items.Reverse<VTableItem>().Where(
					(t) =>

						t.Trait.Kind == TraitKind.Method &&

						t.Trait.QName.Name == id &&
						(
							(env.Scope.NamespaceSet.Namespaces.Contains(t.Trait.QName.Namespace) && match_namespace == null)
							||
							(t.Trait.QName.Namespace == match_namespace && match_namespace != null)
							||
							(
								match_namespace != null
								&&
								(match_namespace.Kind == NamespaceKind.PackageInternal || match_namespace.Kind == NamespaceKind.Private || match_namespace.Kind == NamespaceKind.Protected)
								&&
								(string.IsNullOrEmpty(match_namespace.Name) || match_namespace.Kind == NamespaceKind.Private)
								&&
								env.Scope.NamespaceSet.Namespaces.Contains(t.Trait.QName.Namespace)
								&&
								(t.Trait.QName.Namespace.Kind == match_namespace.Kind || (t.Trait.QName.Namespace.Kind == NamespaceKind.StaticProtected && match_namespace.Kind == NamespaceKind.Protected))
								&&
								t.Trait.QName.Namespace.def_uri == null
								)
							)

						&&
						(
							match_namespace == null
							||
							(
								match_namespace.Kind == t.Trait.QName.Namespace.Kind
							)
							||
							(
								(t.Trait.QName.Namespace.Kind == NamespaceKind.Protected || t.Trait.QName.Namespace.Kind == NamespaceKind.StaticProtected)
								&&
								match_namespace.Kind == NamespaceKind.Protected
							)
						)

					).GroupBy(t => t.DefineAt).FirstOrDefault()
					;

				var findmethods = _findmethods_linq == null ? new VTableItem[0] : _findmethods_linq.ToArray();

				var _findgetters_linq_ = scope.Container._vtable.Items.Reverse<VTableItem>().Where(
					(t) =>

						t.Trait.Kind == TraitKind.Getter &&

						t.Trait.QName.Name == id &&
						(
							(env.Scope.NamespaceSet.Namespaces.Contains(t.Trait.QName.Namespace) && match_namespace == null)
							||
							(t.Trait.QName.Namespace == match_namespace && match_namespace != null)
							||
							(
								match_namespace != null
								&&
								(match_namespace.Kind == NamespaceKind.PackageInternal || match_namespace.Kind == NamespaceKind.Private || match_namespace.Kind == NamespaceKind.Protected)
								&&
								(string.IsNullOrEmpty(match_namespace.Name) || match_namespace.Kind == NamespaceKind.Private)
								&&
								env.Scope.NamespaceSet.Namespaces.Contains(t.Trait.QName.Namespace)
								&&
								(t.Trait.QName.Namespace.Kind == match_namespace.Kind || (t.Trait.QName.Namespace.Kind == NamespaceKind.StaticProtected && match_namespace.Kind == NamespaceKind.Protected))
								&&
								t.Trait.QName.Namespace.def_uri == null
								)
							)

						&&
						(
							match_namespace == null
							||
							(
								match_namespace.Kind == t.Trait.QName.Namespace.Kind
							)
							||
							(
								(t.Trait.QName.Namespace.Kind == NamespaceKind.Protected || t.Trait.QName.Namespace.Kind == NamespaceKind.StaticProtected)
								&&
								match_namespace.Kind == NamespaceKind.Protected
							)
						)

					).GroupBy(t => t.DefineAt).FirstOrDefault()
					;

				var findgetters = _findgetters_linq_ == null ? new VTableItem[0] : _findgetters_linq_.ToArray();

				var _findsetters_linq_ = scope.Container._vtable.Items.Reverse<VTableItem>().Where(
					(t) =>

						t.Trait.Kind == TraitKind.Setter &&

						t.Trait.QName.Name == id &&
						(
							(env.Scope.NamespaceSet.Namespaces.Contains(t.Trait.QName.Namespace) && match_namespace == null)
							||
							(t.Trait.QName.Namespace == match_namespace && match_namespace != null)
							||
							(
								match_namespace != null
								&&
								(match_namespace.Kind == NamespaceKind.PackageInternal || match_namespace.Kind == NamespaceKind.Private || match_namespace.Kind == NamespaceKind.Protected)
								&&
								(string.IsNullOrEmpty(match_namespace.Name) || match_namespace.Kind == NamespaceKind.Private)
								&&
								env.Scope.NamespaceSet.Namespaces.Contains(t.Trait.QName.Namespace)
								&&
								(t.Trait.QName.Namespace.Kind == match_namespace.Kind || (t.Trait.QName.Namespace.Kind == NamespaceKind.StaticProtected && match_namespace.Kind == NamespaceKind.Protected))
								&&
								t.Trait.QName.Namespace.def_uri == null
								)
							)

						&&
						(
							match_namespace == null
							||
							(
								match_namespace.Kind == t.Trait.QName.Namespace.Kind
							)
							||
							(
								(t.Trait.QName.Namespace.Kind == NamespaceKind.Protected || t.Trait.QName.Namespace.Kind == NamespaceKind.StaticProtected)
								&&
								match_namespace.Kind == NamespaceKind.Protected
							)
						)

					).GroupBy(t => t.DefineAt).FirstOrDefault()
					;

				var findsetters = _findsetters_linq_ == null ? new VTableItem[0] : _findsetters_linq_.ToArray();

				if (find.Length == 2)
				{
					if (find[1].Kind == ScopeMemberKind.Parameter && find[0].Kind != ScopeMemberKind.Parameter)
					{
						find = new ScopeMember[1] { find[0] };
					}

				}


				if (find.Length == 1 && (findmethods.Length + findgetters.Length + findsetters.Length) == 0)
				{
					var f = find[0];
					var i = scope.Members.IndexOf(f);//f[0]);

					if (scope.index > 65535)
					{
						throw new ParseException("scope count > 65535");
					}
					if (i > 65535)
					{
						throw new ParseException("scope members count > 65535");
					}

					if ((scope.Kind == CodeScopeKind.Class && env.Scope.Kind != CodeScopeKind.Class) || (toplevel != null && scope == toplevel._link_codescope))
					{
						//需返回静态对象的引用
						//var stackLoc = env.MakeStackLocater(TypeKind.Class, (TypeKind)((ASClass)scope.Container).Type_identifier);

						//INS_Ld_Class ld_Class = new INS_Ld_Class();
						//ld_Class.stack = stackLoc;
						//ld_Class.classid_index = env.AddConstClassId((ASClass)scope.Container);

						//env.instructions.Add(ld_Class);

						//var reg = env.MakeStackLocater(TypeKind.TraitDataReference, scope.Members[i].TypeKind);


						//int scopemember_index = i;

						//ASTrait t = scope.Container.Traits.First((t) => t.QName == scope.Members[i].QName);
						//int trait_index = scope.Container.Traits.IndexOf(t);

						//if (trait_index > ushort.MaxValue || scopemember_index > ushort.MaxValue)
						//{
						//    throw new ParseException("scope count > 65535");
						//}

						//INS_Ld_HeapValueRef ld_HeapRef = new INS_Ld_HeapValueRef();
						//ld_HeapRef.reg = reg;
						//ld_HeapRef.instance = stackLoc;
						//ld_HeapRef.trait_index = (ushort)trait_index;
						//ld_HeapRef.scopemember_index = (ushort)scopemember_index;

						//env.instructions.Add(ld_HeapRef);

						heapLocater = new ScopeHeapLocater();
						heapType = scope.Members[i].TypeKind;

						findtrait = scope.Container.Traits.First((t) => t.QName == scope.Members[i].QName);
						findScope = scope;
						findmember = scope.Members[i];
						vTableItems = null;

						return FindIdResultType.NeedLoadClass_ScopeMember;
						//return reg;
					}
					else
					{

						heapLocater.ScopeIndex = (ushort)scope.index;
						heapLocater.MemberIndex = (ushort)i;

						heapType = scope.Members[i].TypeKind;

						findtrait = f.trait;
						findScope = scope;
						findmember = scope.Members[i];

						vTableItems = null;

						return FindIdResultType.ScopeMember;
						//return scope.Members[i];
					}
				}
				else if (find.Length == 0 && findmethods.Length == 1 && findgetters.Length + findsetters.Length == 0)
				{
					heapType = findmethods[0].Trait.TypeKind;

					heapLocater = default;
					findtrait = findmethods[0].Trait;
					findScope = scope;
					findmember = null;

					vTableItems = new VTableItem[] { findmethods[0] };

					if (findmethods[0].DefineAt is ASClass)
					{
						return FindIdResultType.NeedLoadClass_VTableItems;
					}
					else
					{
						return FindIdResultType.VTableItems;
					}
				}
				else if (find.Length == 0 && findmethods.Length == 0 && findgetters.Length <= 1 && findsetters.Length <= 1 && findgetters.Length + findsetters.Length >= 1)
				{
					heapType = TypeKind.Unknown;
					heapLocater = default;
					findtrait = null;
					findScope = scope;
					findmember = null;

					vTableItems = new VTableItem[] {
						(findgetters.Length>0 ? findgetters[0] : null),
						(findsetters.Length>0 ? findsetters[0] : null),
					};

					if (
						(vTableItems[0] != null && vTableItems[0].DefineAt is ASClass) ||
						(vTableItems[1] != null && vTableItems[1].DefineAt is ASClass)

						)
					{
						return FindIdResultType.NeedLoadClass_VTableItems;
					}
					else
					{
						return FindIdResultType.VTableItems;
					}
				}
				else if (find.Length + findmethods.Length + findgetters.Length > 1
							||
							find.Length + findmethods.Length + findsetters.Length > 1
					)
				{
					heapType = TypeKind.Unknown;
					heapLocater = default;
					findtrait = null;
					findScope = scope;
					findmember = null;

					vTableItems = null;

					return FindIdResultType.Ambiguous;
				}

			}






			heapType = TypeKind.Unknown;
			heapLocater = default;
			findtrait = null;
			findScope = null;
			findmember = null;

			vTableItems = null;

			return FindIdResultType.NotFound;
		}


		/// <summary>
		/// 隐式类型转换代码
		/// </summary>
		/// <param name="env"></param>
		/// <param name="src"></param>
		/// <param name="dstType"></param>
		/// <param name="token"></param>
		/// <returns></returns>
		public static StackLocater TestTypeConvert(CompileEnv env, StackLocater src, CompileTypeKind targetType,
		   Token token)
		{
			//首先检查是否可以隐式类型转换，在编译时不能转的会报错。

			TypeKind from = default;
			TypeKind to = default;


			var srctype = env.ReadStackType(src);

			if (srctype.Maj > TypeKind.Null && srctype.Maj != TypeKind.Super)
			{
				var c = FindClassById(env, (ulong)srctype.Maj);
				if (c.Instance.Flags.HasFlag(ClassFlags.Vector))
				{
					srctype.Maj = TypeKind.Vector;
					srctype.Mir = (TypeKind)c.Type_identifier;
				}
			}
			if (targetType.Maj > TypeKind.Null && srctype.Maj != TypeKind.Super)
			{
				var c = FindClassById(env, (ulong)targetType.Maj);
				if (c.Instance.Flags.HasFlag(ClassFlags.Vector))
				{
					targetType.Maj = TypeKind.Vector;
					targetType.Mir = (TypeKind)c.Type_identifier;
				}
			}


			if (srctype.Maj == targetType.Maj)
			{
				if (srctype.Maj == TypeKind.Vector) //Vector必须完全相等
				{
					if (srctype.Mir != targetType.Mir)
					{

						from = srctype.Mir;
						to = targetType.Mir;

						goto lbl_throw;
					}
				}

				return src;
			}

			

			if (srctype.Maj == TypeKind.TraitDataReference || srctype.Maj == TypeKind.Vector)
			{
				from = srctype.Mir;
			}
			else
			{
				from = srctype.Maj;
			}

			if (targetType.Maj == TypeKind.TraitDataReference || targetType.Maj == TypeKind.Vector)
			{
				to = targetType.Mir;
			}
			else
			{
				to = targetType.Maj;
			}

			if (srctype.Maj == TypeKind.RTQName_MultiName_DataReference)
			{
				from = TypeKind.Any;
			}

			if (targetType.Maj == TypeKind.RTQName_MultiName_DataReference)
			{
				to = TypeKind.Any;
			}

			


			if (TypeUtils.TestImplicitConvert(from, to, env.CompileContext))
			{
				return src;
			}

		lbl_throw:

			var toTypeStr = (TypeKind t) =>
			{
				if (t <= TypeKind.Namespace)
					return TypeUtils.ToTypeString(t,env.CompileContext);
				else
				{
					if (env.CompileContext.vectorDefs.Any(v => v.Identifier == t))
					{
						return env.CompileContext.vectorDefs.First(v => v.Identifier == t).ToString().Replace("__AS3__.vec.", "");
					}
					else
					{
						var alltypes = env.CompileContext.scriptDefs.SelectMany(
							s => s.scriptClasses).Union(env.CompileContext.player_for_compiler.Context.dictTypes.Select(p => p.Value)).Where(t => t != null);

						var type = alltypes.First(c => c.Type_identifier == (ulong)t);
						if (string.IsNullOrEmpty(type.QName.Namespace.Name))
						{
							return type.QName.Name;
						}
						else
						{
							return type.QName.Namespace.Name + "." + type.QName.Name;
						}
					}
				}
			};



			throw new ResolverException(token,
				$"Implicit coercion of a value with static type {toTypeStr(from)} to a possibly unrelated type {toTypeStr(to)}.");

		}


		internal static StackLocater BuildAssigning(AS3ExprStep step, CompileEnv compileEnv,string errheader = "Target of assignment")
		{
			if (step.OpCode == "=")
			{
				var lv = step.Arg1;
				if (lv.IsReg)
				{
					var stack = compileEnv.GetStackLocater(lv.Reg);
					var ctype = compileEnv.ReadStackType(stack);

					if (ctype.Maj == TypeKind.TraitDataReference)
					{

						var t = compileEnv.ReadTraitRef(lv.Reg).Item1;
						if (t.Length == 1)
						{
							var trait = t[0];
							if (trait.Kind == TraitKind.Constant)
							{
								throw new ResolverException(
									  step.token,
									   "Illegal assignment to a variable specified as constant.");


							}
							else if (trait.Kind == TraitKind.Slot)
							{
								var target = compileEnv.GetStackLocater(lv.Reg);
								var targetType = compileEnv.ReadStackType(target);

								StackLocater stackLocater = LoadRightValue(step.Arg2, compileEnv, step.token);
								stackLocater = TestTypeConvert(compileEnv, stackLocater, targetType, step.token);

								INS_Store_HeapValueRef store_HeapValueRef = new INS_Store_HeapValueRef(step.token);
								store_HeapValueRef.dst = target;
								store_HeapValueRef.source = stackLocater;

								compileEnv.instructions.Add(store_HeapValueRef);

								return stackLocater;
							}
							else
							{
								throw new InvalidOperationException();
							}
						}
						else if (t.Length == 2)
						{
							if (t[1] == null)
							{
								throw new ResolverException(step.token, $"Property {t[0].QName.Name} is read-only.");
							}
							StackLocater result;
							do
							{

								var instance = compileEnv.ReadRefBindInstance(lv.Reg);
								var instance_type = compileEnv.ReadStackType(instance);
								VTable vTable;

								StackLocater valueLocater = LoadRightValue(step.Arg2, compileEnv, step.token);
								valueLocater = TestTypeConvert(compileEnv, valueLocater, new CompileTypeKind() { Maj = t[1].Method.Parameters[0].TypeKind, Mir = TypeKind.Unknown }, step.token);


								if (instance_type.Maj == TypeKind.Class)
								{
									vTable = FindClassById(compileEnv, (ulong)instance_type.Mir)._vtable;

									if (FindClassById(compileEnv, (ulong)instance_type.Mir).Instance.IsInterface)
									{
										throw new InvalidOperationException("不可能是接口");
									}
								}
								else if (instance_type.Maj == TypeKind.Super)
								{
									//调父类的写入属性
									ASClass astype = FindClassById(compileEnv, (ulong)instance_type.Mir);
									vTable = astype.Instance._vtable;

									if (FindClassById(compileEnv, (ulong)instance_type.Mir).Instance.IsInterface)
									{
										throw new InvalidOperationException("不可能是接口");
									}

									var _index = vTable.Items.FindIndex(v => v.Trait == t[1]);
									if (_index < 0)
										throw new InvalidOperationException();
									if (_index > 65535)
										throw new ResolverException(step.token, "vtable.items.count > 65535");

									int mid = compileEnv.AddSuperMethod(astype, _index);

									INS_Ld_SuperMethod ld_SuperMethod = new INS_Ld_SuperMethod(step.token);
									ld_SuperMethod.dst = compileEnv.MakeStackLocater( TypeKind.Function);
									ld_SuperMethod.instance = instance;
									ld_SuperMethod.const_index = mid;

									compileEnv.instructions.Add(ld_SuperMethod);

									INS_BindThis_Call bindThis_Call = new INS_BindThis_Call(step.token);
									bindThis_Call.dst = valueLocater;
									bindThis_Call.function = ld_SuperMethod.dst;
									bindThis_Call._this_ = instance;
									bindThis_Call.args = new StackLocater[1];
									bindThis_Call.args[0] = valueLocater;

									compileEnv.instructions.Add(bindThis_Call);

									result = valueLocater;

									break;
								}
								else
								{
									ASClass astype = FindClassById(compileEnv, (ulong)instance_type.Maj);
									vTable = astype.Instance._vtable;

									if (FindClassById(compileEnv, (ulong)instance_type.Maj).Instance.IsInterface)
									{
										int class_id = compileEnv.AddConstClassId(astype);

										var i_index = vTable.Items.FindIndex(v => v.Trait == t[1]);
										if (i_index < 0)
											throw new InvalidOperationException();
										if (i_index > 65535)
											throw new ResolverException(step.token, "vtable.items.count > 65535");

										INS_writeProperty_Interface writeProperty_Interface = new INS_writeProperty_Interface(step.token);
										writeProperty_Interface.dst = valueLocater;
										writeProperty_Interface.class_id = class_id;
										writeProperty_Interface.const_index = (ushort)i_index;
										writeProperty_Interface.instance = instance;

										compileEnv.instructions.Add(writeProperty_Interface);

										result = valueLocater;
										//throw new NotImplementedException();
										break;
									}
								}

								var v_index = vTable.Items.FindIndex(v => v.Trait == t[1]);
								if (v_index < 0)
									throw new InvalidOperationException();
								if (v_index > 65535)
									throw new ResolverException(step.token, "vtable.items.count > 65535");





								INS_writeProperty writeProperty = new INS_writeProperty(step.token);
								writeProperty.dst = valueLocater;
								writeProperty.const_index = (ushort)v_index;
								writeProperty.instance = instance;

								compileEnv.instructions.Add(writeProperty);

								result = valueLocater;
							}
							while (false);

							return result;
							//throw new NotImplementedException();
						}
						else
						{
							throw new NotImplementedException();
						}
					}
					else if (ctype.Maj == TypeKind.RTQName_MultiName_DataReference)
					{
						var target = compileEnv.GetStackLocater(lv.Reg);
						var targetType = compileEnv.ReadStackType(target);

						StackLocater stackLocater = LoadRightValue(step.Arg2, compileEnv, step.token);
						stackLocater = TestTypeConvert(compileEnv, stackLocater, targetType, step.token);

						INS_Store_HeapValueRef store_HeapValueRef = new INS_Store_HeapValueRef(step.token);
						store_HeapValueRef.dst = target;
						store_HeapValueRef.source = stackLocater;

						compileEnv.instructions.Add(store_HeapValueRef);

						return stackLocater;
					}
					else if (ctype.Maj == TypeKind.Class)
					{
						ASClass cls = FindClassById(compileEnv, (ulong)ctype.Mir);
						throw new ResolverException(step.token, $"Illegal assignment to class {cls.QName.Name}.");
					}
					else if (ctype.Maj == TypeKind.Function)
					{
						//ASClass cls = FindClassById(compileEnv, (ulong)ctype.Mir);
						//Cannot assign to a method EE on Main.

						var bindthis = compileEnv.ReadRefBindInstance(lv.Reg);
						var type = compileEnv.ReadStackType(bindthis);
						ASClass cls = FindClassById(compileEnv, (ulong)type.Maj);

						throw new ResolverException(step.token, $"Cannot assign to a method {compileEnv.ReadTraitRef(lv.Reg).Item1[0].QName.Name} on {cls.QName.Name}.");
					}
					else
					{
						throw new ResolverException(step.token, $"{errheader} must be a reference value.");
					}

				}
				else if (lv.Data.FF1Type == AST.Expr.FF1DataValueType.identifier)
				{
					string id = (string)lv.Data.Value;

					if (id.StartsWith("{")) //赋默认值的情况
					{
						int split = id.IndexOf('}');

						string name = id.Substring(split + 1);

						string[] ns = id.Substring(1, split - 1 - 2).Split(' ');

						ScopeMember scopeMember = null;
						CodeScope scope = compileEnv.Scope;

						while (scopeMember == null)
						{
							if (scope == null)
							{
								break;
							}

							if (ns.Any((n) => n == "internal")
							||
							ns.Length == 0
							||
							ns.All((n) => n == "static")
							)
							{

								for (int j = 0; j < scope.Members.Count; j++)
								{
									var member = scope.Members[j];
									if (
										(

										(member.QName.Namespace.Kind == ABC.NamespaceKind.PackageInternal
											&&
										 member.QName.Namespace.def_uri == null)


										||

										(
											scope.Kind == CodeScopeKind.Script
											&&
											member.QName.Namespace.Kind == NamespaceKind.Private
										)

										||

										(
											scope.Kind == CodeScopeKind.Class
											&&
											member.QName.Namespace.Kind == NamespaceKind.Private
										)

										)


										&&
										member.QName.Name == name
										&&
										member.DefineAt == scope.Container
										)
									{
										scopeMember = member;
										break;
									}
								}
							}
							else if (ns.Any((n) => n == "public"))
							{
								for (int j = 0; j < scope.Members.Count; j++)
								{
									var member = scope.Members[j];
									if (member.QName.Namespace.Kind == ABC.NamespaceKind.Package
										&&
										member.QName.Name == name
										&&
										member.DefineAt == scope.Container
										)
									{
										scopeMember = member;
										break;
									}
								}
							}
							else if (ns.Any((n) => n == "private"))
							{
								for (int j = 0; j < scope.Members.Count; j++)
								{
									var member = scope.Members[j];
									if (member.QName.Namespace.Kind == ABC.NamespaceKind.Private
										&&
										member.QName.Name == name
										&&
										member.DefineAt == scope.Container
										)
									{
										scopeMember = member;
										break;
									}
								}
							}
							else if (ns.Any(n => n == "protected"))
							{
								for (int j = 0; j < scope.Members.Count; j++)
								{
									var member = scope.Members[j];

									if (member.DefineAt == scope.Container)
									{
										if (scope.Container.IsStatic)
										{
											if (member.QName.Namespace.Kind == ABC.NamespaceKind.StaticProtected
											&&
											member.QName.Name == name
											)
											{
												scopeMember = member;
												break;
											}
										}
										else
										{
											if (member.QName.Namespace.Kind == ABC.NamespaceKind.Protected
											&&
											member.QName.Name == name
											)
											{
												scopeMember = member;
												break;
											}
										}
									}
								}
							}
							else
							{
								string nsName = ns.First(n => n != "static");

								if (!string.IsNullOrEmpty(nsName))
								{
									//阻止上下文中有字段和命名空间重名
									ScopeHeapLocater hL;
									TypeKind heapType;
									ASTrait out_trait;
									ScopeMember out_scopemember;
									CodeScope out_scope;
									VTableItem[] out_vtableItems;

									var result = FindIdentifier(nsName, null, compileEnv, step.token, out hL, out heapType, out out_trait, out out_scope, out out_scopemember, out out_vtableItems);
									if (result == FindIdResultType.ScopeMember || result == FindIdResultType.NeedLoadClass_ScopeMember)
									{
										if (!(out_scopemember.Kind == ScopeMemberKind.Constant && out_scopemember.ValueKind == ConstantKind.Namespace))
										{
											throw new ResolverException(step.token, "Namespace was not found or is not a compile-time constant.");
										}
									}
								}

								for (int j = scope.Members.Count - 1; j >= 0; j--)
								{
									var member = scope.Members[j];

									if (member.QName.Name == name
										&&
										member.DefineAt == scope.Container
										&&
										(
											member.QName.Namespace.Name.EndsWith(":" + nsName)
											||

											string.IsNullOrEmpty(nsName)


										)
										)
									{
										scopeMember = member;
										break;
									}
								}





							}

							if (scopeMember == null)
							{
								scope = scope.Parent;
							}

						}



						if (scopeMember == null)
						{
							throw new InvalidOperationException();
						}

						if (scope.index > 65535)
						{
							throw new ParseException("scope count > 65535");
						}

						compileEnv.initvalue_instructions[compileEnv.initvalue_instructions.Count - 1].member = scopeMember;

						if (scopeMember.Kind != ScopeMemberKind.Slot && scopeMember.Kind != ScopeMemberKind.Constant)
						{
							throw new InvalidOperationException();
						}


						//                  if (scopeMember.Kind == ScopeMemberKind.Constant && compileEnv.CompileContext.only_const_scriptinit.Count>0)
						//                  {
						//                      compileEnv.CompileContext.constant_init_expr.Add(expression);
						//                      compileEnv.CompileContext.only_const_scriptinit.Peek().memberCodes.Add(expression, new Tuple<ScopeMember, List<Instruction>>(scopeMember,new List<Instruction>()));
						//                  }
						//if (scopeMember.Kind == ScopeMemberKind.Slot && compileEnv.CompileContext.only_const_scriptinit.Count > 0)
						//{
						//	compileEnv.CompileContext.only_const_scriptinit.Peek().memberCodes.Add(expression, new Tuple<ScopeMember, List<Instruction>>(scopeMember, new List<Instruction>()));
						//}


						ScopeHeapLocater heapLocater = new ScopeHeapLocater()
						{
							ScopeIndex = (ushort)scope.index,
							MemberIndex = (ushort)scope.Members.IndexOf(scopeMember)
						};

						StackLocater stackLocater = LoadRightValue(step.Arg2, compileEnv, step.token);

						stackLocater = TestTypeConvert(compileEnv, stackLocater, new CompileTypeKind() { Maj = scopeMember.TypeKind, Mir = TypeKind.Unknown }, step.token);

						if (!step.Arg2.IsReg && step.Arg2.Data.FF1Type == FF1DataValueType.as3_function)
						{
							/* 处理这种代码，使得func和f指向同一个槽
							 *	var probe;

								var func = function f() {
								  probe = function() { return f; };
								};
								var f = 'outside';

								func();

								assert.sameValue(f, 'outside');

								assert.sameValue(probe(), func);
							 */

							ASMethod method = compileEnv.CompileContext.dict_method_as3function[(AS3Function)step.Arg2.Data.Value];

							if (method.IsAnonymous && method.Name.IndexOf("#anonymous:") > 0)
							{
								if (scope.Members.FindIndex(s => s.Kind == ScopeMemberKind.Slot && s.QName.Name == method.Name) >= 0)
								{
									var slotmember = scope.Members.First(s => s.Kind == ScopeMemberKind.Slot && s.QName.Name == method.Name);

									ScopeHeapLocater slotlocater = new ScopeHeapLocater()
									{
										ScopeIndex = (ushort)scope.index,
										MemberIndex = (ushort)scope.Members.IndexOf(slotmember)
									};

									INS_Store_ScopeHeap store_slotVariable = new INS_Store_ScopeHeap(step.token);
									store_slotVariable.dst = stackLocater;
									store_slotVariable.heap = slotlocater;

									compileEnv.instructions.Add(store_slotVariable);

									INS_Ld_ScopeHeap ld_method = new INS_Ld_ScopeHeap(step.token);
									ld_method.heap = slotlocater;
									ld_method.dst = compileEnv.MakeStackLocater(TypeKind.Function);
									compileEnv.instructions.Add(ld_method);

									stackLocater = ld_method.dst;
									
								}
							}
						}


						if (scopeMember.DefineAt._link_codescope.index == heapLocater.ScopeIndex
								&& scopeMember.DefineAt._link_codescope.Kind == CodeScopeKind.Method
								&& compileEnv.Scope.Kind == CodeScopeKind.Method && compileEnv.Scope.index == heapLocater.ScopeIndex
							)
						{
							INS_Store_MethodVariable store_MethodVariable = new INS_Store_MethodVariable(step.token);
							store_MethodVariable.dst = stackLocater;
							store_MethodVariable.heap = heapLocater;

							compileEnv.instructions.Add(store_MethodVariable);
						}
						else
						{

							INS_Store_ScopeHeap iNS = new INS_Store_ScopeHeap(step.token);
							iNS.dst = stackLocater;
							iNS.heap = heapLocater;


							compileEnv.instructions.Add(iNS);
						}
						return stackLocater;
					}
					else
					{
						ScopeHeapLocater heapLocater;
						TypeKind heapType;
						ASTrait out_trait;
						ScopeMember out_scopemember;
						CodeScope out_scope;
						VTableItem[] out_vtableItems;
						var result = FindIdentifier(id, null, compileEnv, step.token, out heapLocater, out heapType, out out_trait, out out_scope, out out_scopemember, out out_vtableItems);
						if (result == FindIdResultType.NotFound)
						{
							CodeScope s;
							int _type = juicescript.runtime.Extensions.IsInaccessibleOrUndefinedOrInScript(compileEnv.Scope.Container, id, out s);
							if (_type == 0)
							{
								throw
									new ResolverException(step.token,
									"Access of possibly undefined property " + step.Arg1.Data.Value + ".");
							}
							else if (_type == 1)
							{
								throw new ResolverException(step.token,

									$"Attempted access of inaccessible property {id} through a reference with static type {s.Container.QName.Name}."
									);
							}
							else
							{
								throw new InvalidOperationException(); //_type只会是 0或1
							}
						}
						else if (result == FindIdResultType.Ambiguous)
						{
							throw new ResolverException(step.token,
									 $"Ambiguous reference to {step.Arg1.Data.Value}");
						}
						else if (result == FindIdResultType.NeedLoadClass_ScopeMember)
						{
							if (out_scopemember.Kind == ScopeMemberKind.Constant)
							{
								throw new ResolverException(
									step.token,
									"Illegal assignment to a variable specified as constant. (" + step.Arg1.Data.Value + ")");
							}
							else if (out_scopemember.Kind == ScopeMemberKind.Slot
							   ||
							   out_scopemember.Kind == ScopeMemberKind.Parameter
							   )
							{

								ASClass @class = (ASClass)out_scope.Container;

								var stackLoc = compileEnv.MakeStackLocater(TypeKind.Class, (TypeKind)@class.Type_identifier);
								INS_Ld_Class ld_Class = new INS_Ld_Class(step.token);
								ld_Class.dst = stackLoc;
								ld_Class.classid_index = compileEnv.AddConstClassId(@class);

								compileEnv.instructions.Add(ld_Class);

								var reg = compileEnv.MakeStackLocater(TypeKind.TraitDataReference, out_scopemember.TypeKind);
								int scopemember_index = out_scope.Members.IndexOf(out_scopemember);
								ASTrait t = out_scope.Container.Traits.First((t) => t.QName == out_scopemember.QName);
								int trait_index = out_scope.Container.Traits.IndexOf(t);

								if (trait_index > ushort.MaxValue || scopemember_index > ushort.MaxValue)
								{
									throw new ParseException("scope count > 65535");
								}

								INS_Ld_InstanceOrSocpeMemberRef ld_HeapRef = new INS_Ld_InstanceOrSocpeMemberRef(step.token);
								ld_HeapRef.dst = reg;
								ld_HeapRef.instance = stackLoc;
								ld_HeapRef.scopemember_index = (ushort)scopemember_index;

								compileEnv.instructions.Add(ld_HeapRef);

								StackLocater valueLoc = LoadRightValue(step.Arg2, compileEnv, step.token);
								valueLoc = TestTypeConvert(compileEnv, valueLoc, new CompileTypeKind() { Maj = heapType, Mir = TypeKind.Unknown }, step.token);

								INS_Store_HeapValueRef store_HeapValueRef = new INS_Store_HeapValueRef(step.token);
								store_HeapValueRef.dst = reg;
								store_HeapValueRef.source = valueLoc;

								compileEnv.instructions.Add(store_HeapValueRef);

								return valueLoc;

							}
							else
							{
								throw new NotImplementedException();
							}
						}
						else if (result == FindIdResultType.ScopeMember)
						{
							var member = out_scopemember;
							if (member.Kind == ScopeMemberKind.Constant)
							{
								throw new ResolverException(
									step.token,
									"Illegal assignment to a variable specified as constant. (" + step.Arg1.Data.Value + ")");
							}
							else if (member.Kind == ScopeMemberKind.Slot
								||
								member.Kind == ScopeMemberKind.Parameter
								)
							{
								ScopeMember scopeMember = (ScopeMember)member;
								if (scopeMember.trait != null && scopeMember.trait.Value != null)
								{
									if (scopeMember.trait.Value.ValueType == ASTrait.TraitValueType.AS3Function)
									{
										throw new ResolverException(scopeMember.trait.Token, "Illegal assignment to function " + id + ".");
									}
								}

								StackLocater valueLocater = LoadRightValue(step.Arg2, compileEnv, step.token);

								valueLocater = TestTypeConvert(compileEnv, valueLocater, new CompileTypeKind() { Maj = heapType, Mir = TypeKind.Unknown }, step.token);


								if (scopeMember.DefineAt._link_codescope.index == heapLocater.ScopeIndex
										&& scopeMember.DefineAt._link_codescope.Kind == CodeScopeKind.Method
										&& compileEnv.Scope.Kind == CodeScopeKind.Method && compileEnv.Scope.index == heapLocater.ScopeIndex
									)
								{
									INS_Store_MethodVariable store_MethodVariable = new INS_Store_MethodVariable(step.token);
									store_MethodVariable.dst = valueLocater;
									store_MethodVariable.heap = heapLocater;

									compileEnv.instructions.Add(store_MethodVariable);

								}
								else
								{
									INS_Store_ScopeHeap iNS = new INS_Store_ScopeHeap(step.token);
									iNS.dst = valueLocater;
									iNS.heap = heapLocater;


									compileEnv.instructions.Add(iNS);
								}
								return valueLocater;

							}
							else
							{
								throw new NotImplementedException();
							}
						}
						else if (result == FindIdResultType.VTableItems)
						{
							if (out_vtableItems.Length == 1)
							{
								throw new ResolverException(
									step.token,
									$"Illegal assignment to function {out_vtableItems[0].Trait.QName.Name}."
									);
							}
							else if (out_vtableItems[1] == null)
							{
								throw new ResolverException(
									step.token,
									$"Property {out_vtableItems[0].Trait.QName.Name} is read-only."
									);
							}
							else
							{
								if (out_vtableItems[1].Trait.QName.Namespace.Kind == NamespaceKind.Namespace)
								{
									throw new InvalidOperationException("接口不可有代码实现");
								}


								StackLocater valueLocater = LoadRightValue(step.Arg2, compileEnv, step.token);
								valueLocater = TestTypeConvert(compileEnv, valueLocater,
									new CompileTypeKind() { Maj = out_vtableItems[1].Trait.Method.Parameters[0].TypeKind, Mir = TypeKind.Unknown }, step.token);



								int v_index = out_scope.Container._vtable.Items.IndexOf(out_vtableItems[1]);
								if (v_index < 0)
									throw new InvalidOperationException();
								if (v_index > 65535)
									throw new ResolverException(step.token, $" {out_scope.Container.QName}.vtable.items.count > 65535");

								INS_writeProperty writeProperty = new INS_writeProperty(step.token);
								writeProperty.dst = valueLocater;
								writeProperty.const_index = (ushort)v_index;
								writeProperty.instance = new StackLocater() { index = -1 - out_scope.index };

								compileEnv.instructions.Add(writeProperty);


								return valueLocater;
							}
						}
						else if (result == FindIdResultType.NeedLoadClass_VTableItems)
						{
							if (out_vtableItems.Length == 1)
							{
								throw new ResolverException(
									step.token,
									$"Illegal assignment to function {out_vtableItems[0].Trait.QName.Name}."
									);
							}
							else if (out_vtableItems[1] == null)
							{
								//F:\GitHub\juice-script-2\juice-script-2\fd_projs\dev_scripts\dev1\src\Main.as:42:Error: Property B is read-only.
								throw new ResolverException(
									step.token,
									$"Property {out_vtableItems[0].Trait.QName.Name} is read-only."
									);
							}
							else
							{
								ASClass @class = (ASClass)out_scope.Container;

								if (@class.Instance.IsInterface)
								{
									throw new InvalidOperationException("不可能出现接口");
								}

								var stackLoc = compileEnv.MakeStackLocater(TypeKind.Class, (TypeKind)@class.Type_identifier);
								INS_Ld_Class ld_Class = new INS_Ld_Class(step.token);
								ld_Class.dst = stackLoc;
								ld_Class.classid_index = compileEnv.AddConstClassId(@class);

								compileEnv.instructions.Add(ld_Class);



								StackLocater valueLocater = LoadRightValue(step.Arg2, compileEnv, step.token);
								valueLocater = TestTypeConvert(compileEnv, valueLocater,
									new CompileTypeKind() { Maj = out_vtableItems[1].Trait.Method.Parameters[0].TypeKind, Mir = TypeKind.Unknown }, step.token);

								int v_index = out_scope.Container._vtable.Items.IndexOf(out_vtableItems[1]);
								if (v_index < 0)
									throw new InvalidOperationException();
								if (v_index > 65535)
									throw new ResolverException(step.token, $" {out_scope.Container.QName}.vtable.items.count > 65535");

								INS_writeProperty writeProperty = new INS_writeProperty(step.token);
								writeProperty.dst = valueLocater;
								writeProperty.const_index = (ushort)v_index;
								writeProperty.instance = stackLoc;

								compileEnv.instructions.Add(writeProperty);

								return valueLocater;

								//throw new NotImplementedException();
							}
						}
						else
						{
							throw new NotImplementedException();
						}
					}

				}
				else
				{
					throw new ResolverException(step.token, $"{errheader} must be a reference value.");
				}
			}

			else if (step.OpCode == "={&&}") //logicand的临时赋值
			{
				StackLocater valueLocater = LoadRightValue(step.Arg2, compileEnv, step.token);
				var type = compileEnv.ReadStackType(valueLocater);
				StackLocater stackLocater = compileEnv.GetStackLocater(step.Arg1.Reg);

				INS_Move ld = new INS_Move(step.token);
				ld.source = valueLocater;
				ld.dst = stackLocater;

				compileEnv.instructions.Add(ld);

				return stackLocater;


				//throw new NotImplementedException();
			}
			else if (step.OpCode == "={||}") //logicor的临时赋值
			{
				StackLocater valueLocater = LoadRightValue(step.Arg2, compileEnv, step.token);
				var type = compileEnv.ReadStackType(valueLocater);
				StackLocater stackLocater = compileEnv.GetStackLocater(step.Arg1.Reg);

				INS_Move ld = new INS_Move(step.token);
				ld.source = valueLocater;
				ld.dst = stackLocater;

				compileEnv.instructions.Add(ld);

				return stackLocater;


				//throw new NotImplementedException();
			}
			else if (step.OpCode == "move")  //三元运算符的赋值
			{
				StackLocater valueLocater = LoadRightValue(step.Arg2, compileEnv, step.token);
				var type = compileEnv.ReadStackType(valueLocater);
				StackLocater stackLocater = compileEnv.GetStackLocater(step.Arg1.Reg);

				INS_Move ld = new INS_Move(step.token);
				ld.source = valueLocater;
				ld.dst = stackLocater;

				compileEnv.instructions.Add(ld);

				return stackLocater;
			}
			else if (step.OpCode == "ComputeDefault")
			{
				//编译用于计算函数参数默认值的代码

				TypeKind para_type = (TypeKind)step.Arg3.Data.Value;


				StackLocater valueLocater = LoadRightValue(step.Arg2, compileEnv, step.token);
				valueLocater = TestTypeConvert(compileEnv, valueLocater, new CompileTypeKind() { Maj = para_type, Mir = TypeKind.Unknown }, step.token);

				StackLocater stackLocater = compileEnv.MakeStackLocater(para_type);

				INS_Ld_ValueRef ld = new INS_Ld_ValueRef(step.token);
				ld.source = valueLocater;
				ld.dst = stackLocater;

				compileEnv.instructions.Add(ld);

				return stackLocater;

			}
			else
			{
				throw new NotImplementedException();
			}
		}



		private void BuildUnary(AS3ExprStep step, AS3Expression expression, CompileEnv compileEnv)
		{
			if (step.OpCode == "+")
			{
				StackLocater rightvalue = LoadRightValue(step.Arg2, compileEnv, step.token);

				TypeKind typeKind = TypeKind.Any;
				TypeKind rvtype = compileEnv.ReadStackType(rightvalue).Maj;

				if (build_operator_override(Player.OverrideOperator.positive, compileEnv.ReadStackType(rightvalue), compileEnv.ReadStackType(rightvalue), rightvalue, rightvalue, compileEnv, step))
				{
					return;
				}

				if (rvtype.IsNumericType())
				{
					typeKind = rvtype;
				}
				else if (rvtype == TypeKind.Any || rvtype == TypeKind.Null)
				{
					typeKind = TypeKind.Number;
				}
				else
				{
					throw new ResolverException(step.token, $"Implicit coercion of a value of type {TypeUtils.ToTypeString(rvtype,compileEnv.CompileContext)} to an unrelated type Number.");
				}


				StackLocater dst = compileEnv.GetStackLocater(step.Arg1.Reg, typeKind);
				INS_Positive positive = new INS_Positive(step.token);
				positive.dst = dst;
				positive.src = rightvalue;

				compileEnv.instructions.Add(positive);
			}
			else if (step.OpCode == "-")
			{

				StackLocater rightvalue = LoadRightValue(step.Arg2, compileEnv, step.token);

				TypeKind typeKind = TypeKind.Any;
				TypeKind rvtype = compileEnv.ReadStackType(rightvalue).Maj;

				if (build_operator_override(Player.OverrideOperator.neg, compileEnv.ReadStackType(rightvalue), compileEnv.ReadStackType(rightvalue), rightvalue, rightvalue, compileEnv, step))
				{
					return;
				}


				if (rvtype == TypeKind.Uint)
				{
					typeKind = TypeKind.Number;
				}
				else if (rvtype == TypeKind.Int)
				{
					typeKind = TypeKind.Int;
				}
				else if (rvtype == TypeKind.Number || rvtype == TypeKind.Null)
				{
					typeKind = TypeKind.Number;
				}
				else if (rvtype == TypeKind.SByte || rvtype == TypeKind.Byte || rvtype == TypeKind.Short || rvtype == TypeKind.UShort)
				{
					typeKind = TypeKind.Int;
				}
				else if (rvtype == TypeKind.Float)
				{
					typeKind = TypeKind.Float;
				}
				else if (rvtype == TypeKind.Any)
				{
					typeKind = TypeKind.Number;
				}
				else
				{
					throw new ResolverException(step.token, $"Implicit coercion of a value of type {TypeUtils.ToTypeString(rvtype,compileEnv.CompileContext)} to an unrelated type Number.");
				}


				StackLocater dst = compileEnv.GetStackLocater(step.Arg1.Reg, typeKind);
				INS_Neg neg = new INS_Neg(step.token);
				neg.dst = dst;
				neg.src = rightvalue;

				compileEnv.instructions.Add(neg);



			}
			else if (step.OpCode == "!")
			{
				StackLocater rightvalue = LoadRightValue(step.Arg2, compileEnv, step.token);
				StackLocater dst = compileEnv.GetStackLocater(step.Arg1.Reg, TypeKind.Boolean);
				INS_LogicNot lnot = new INS_LogicNot(step.token);
				lnot.dst = dst;
				lnot.src = rightvalue;

				compileEnv.instructions.Add(lnot);
			}
			else if (step.OpCode == "typeof")
			{
				StackLocater rightvalue = LoadRightValue(step.Arg2, compileEnv, step.token);
				StackLocater dst = compileEnv.GetStackLocater(step.Arg1.Reg, TypeKind.String);
				INS_Typeof ltypeof = new INS_Typeof(step.token);
				ltypeof.dst = dst;
				ltypeof.src = rightvalue;

				compileEnv.instructions.Add(ltypeof);
			}
			else if (step.OpCode == "delete")
			{
				if (step.Arg2.IsReg)
				{

					StackLocater rv;

					Tuple<ASTrait[], AS3ExprStep> traitRef;
					if (!compileEnv.TryReadTraitRef(step.Arg2.Reg, out traitRef))
					{
						rv = compileEnv.GetStackLocater(step.Arg2.Reg);
					}
					else
					{
						if (traitRef.Item1.Length > 0 && traitRef.Item1.Length != 3) //3个成员说明是 indexer,包含delete功能
						{
							throw new ResolverException(step.token, $"Attempt to delete the fixed property {traitRef.Item1[0].QName.Name}.  Only dynamically defined properties can be deleted.");
						}
						else
						{
							rv = compileEnv.GetStackLocater(step.Arg2.Reg);
						}
					}
					CompileTypeKind typeKind = compileEnv.ReadStackType(rv);

					switch (typeKind.Maj)
					{

						case TypeKind.TraitDataReference:
							{
								var t = compileEnv.ReadTraitRef(step.Arg2.Reg);
								throw new ResolverException(step.token, $"Attempt to delete the fixed property {t.Item1[0].QName.Name}.  Only dynamically defined properties can be deleted.");
							}
						case TypeKind.RTQNameRTQNameL_N:
							throw new InvalidOperationException("不可能发生");
						case TypeKind.CParseNS_Traits:
							throw new InvalidOperationException("不可能发生");
							break;
						case TypeKind.SearchNameSpaceFromImports:
							{
								string ns = compileEnv.ReadSearchNSImport(rv);
								throw new ResolverException(step.token, $"Package cannot be used as a value: '{ns}'.");
							}
						case TypeKind.Namespace:
							{
								var t = compileEnv.ReadRegNameSpace(step.Arg2.Reg);
								throw new ResolverException(step.token, $"Attempt to delete the fixed property {t.Name}.  Only dynamically defined properties can be deleted.");
							}
						case TypeKind.Function:
							{
								var t = compileEnv.ReadTraitRef(step.Arg2.Reg);
								throw new ResolverException(step.token, $"Attempt to delete the fixed property {t.Item1[0].QName.Name}.  Only dynamically defined properties can be deleted.");
							}
						case TypeKind.RTQName_MultiName_DataReference:
						// 运行时访问成员
						//throw new NotImplementedException("检查通过，发送delete指令");
						//break;
						case TypeKind.Any:
						case TypeKind.Boolean:
						case TypeKind.SByte:
						case TypeKind.Byte:
						case TypeKind.Short:
						case TypeKind.UShort:
						case TypeKind.Int:
						case TypeKind.Uint:
						case TypeKind.Float:
						case TypeKind.Number:
						case TypeKind.Fun_Void:
						case TypeKind.Unknown:
						case TypeKind.Null:
						case TypeKind.Object:
						case TypeKind.Class:
						case TypeKind.String:

						case TypeKind.Array:
						case TypeKind.Vector:
						default:
							//表达式的计算结果
							//可以执行delete操作
							{
								StackLocater result = compileEnv.GetStackLocater(step.Arg1.Reg, TypeKind.Boolean);
								INS_Delete delete = new INS_Delete(step.token);
								delete.dst = result;
								delete.todelete = rv;

								compileEnv.instructions.Add(delete);



							}

							break;
					}


				}
				else
				{
					switch (step.Arg2.Data.FF1Type)
					{
						case FF1DataValueType.dynamicobj:
							{ //直接删除一个dynamicobj,所以直接返回true即可

								StackLocater dst = compileEnv.GetStackLocater(step.Arg1.Reg, TypeKind.Boolean);
								INS_Ld_True ld_True = new INS_Ld_True(step.token);
								ld_True.dst = dst;
								compileEnv.instructions.Add(ld_True);



							}
							break;
						case FF1DataValueType.e4xxml:
							{ //直接删除一个内嵌xml,所以直接返回true即可

								StackLocater dst = compileEnv.GetStackLocater(step.Arg1.Reg, TypeKind.Boolean);
								INS_Ld_True ld_True = new INS_Ld_True(step.token);
								ld_True.dst = dst;
								compileEnv.instructions.Add(ld_True);


							}
							break;
						case FF1DataValueType.identifier:
							{
								string identifier = step.Arg2.Data.Value.ToString();
								{
									if (Equals(identifier, "Infinity"))
									{
										//StackLocater dst = compileEnv.GetStackLocater(step.Arg1.Reg, TypeKind.Boolean);
										//INS_Ld_True ld_True = new INS_Ld_True(step.token);
										//ld_True.stack = dst;
										//compileEnv.instructions.Add(ld_True);

										throw new ResolverException(step.token, "Attempt to delete the fixed property Infinity.  Only dynamically defined properties can be deleted.");

									}
									else if (Equals(identifier, "NaN"))
									{
										//StackLocater dst = compileEnv.GetStackLocater(step.Arg1.Reg, TypeKind.Boolean);
										//INS_Ld_True ld_True = new INS_Ld_True(step.token);
										//ld_True.stack = dst;
										//compileEnv.instructions.Add(ld_True);

										throw new ResolverException(step.token, "Attempt to delete the fixed property NaN.  Only dynamically defined properties can be deleted.");

									}
									else if (Equals(identifier, "false"))
									{
										StackLocater dst = compileEnv.GetStackLocater(step.Arg1.Reg, TypeKind.Boolean);
										INS_Ld_True ld_True = new INS_Ld_True(step.token);
										ld_True.dst = dst;
										compileEnv.instructions.Add(ld_True);
									}
									else if (Equals(identifier, "true"))
									{
										StackLocater dst = compileEnv.GetStackLocater(step.Arg1.Reg, TypeKind.Boolean);
										INS_Ld_True ld_True = new INS_Ld_True(step.token);
										ld_True.dst = dst;
										compileEnv.instructions.Add(ld_True);


									}
									else if (Equals(identifier, "null"))
									{
										StackLocater dst = compileEnv.GetStackLocater(step.Arg1.Reg, TypeKind.Boolean);
										INS_Ld_True ld_True = new INS_Ld_True(step.token);
										ld_True.dst = dst;
										compileEnv.instructions.Add(ld_True);


									}
									else if (Equals(identifier, "undefined"))
									{
										//StackLocater dst = compileEnv.GetStackLocater(step.Arg1.Reg, TypeKind.Boolean);
										//INS_Ld_True ld_True = new INS_Ld_True(step.token);
										//ld_True.stack = dst;
										//compileEnv.instructions.Add(ld_True);

										throw new ResolverException(step.token, "Attempt to delete the fixed property undefined.  Only dynamically defined properties can be deleted.");

									}
									else if (Equals(identifier, "arguments") && compileEnv.Scope.Kind == CodeScopeKind.Method)
									{
										var method = ((ASMethodBody)compileEnv.Scope.Container).Method;
										if (method.Parameters.Count > 0 && method.Parameters[method.Parameters.Count - 1].IsRest)
										{
											throw new ResolverException(step.token, "'arguments' can not be used in a function with '...' paramaters.");
										}

										throw new ResolverException(step.token, "Attempt to delete the fixed property arguments.  Only dynamically defined properties can be deleted.");

									}
									else
									{
										ScopeHeapLocater heapLocater; TypeKind heapType; ASTrait out_trait; CodeScope codescope; ScopeMember scopeMember; VTableItem[] out_vtableItems;
										var _Rv = FindIdentifier(identifier, null, compileEnv, step.token,
											out heapLocater, out heapType, out out_trait, out codescope, out scopeMember, out out_vtableItems);

										if (_Rv == FindIdResultType.NotFound)
										{
											//查找导入的类
											var match = compileEnv.imports.Where((t) => { return t.QName.Name == identifier; }).ToArray();
											if (match.Length > 1)
											{
												throw new ResolverException(step.token, $"Ambiguous reference to {match[0].QName.Name}");
											}
											else if (match.Length == 1)
											{
												throw new ResolverException(step.token, $"Attempt to delete the fixed property {identifier}.  Only dynamically defined properties can be deleted.");
											}
											else
											{
												throw new ResolverException(step.token, $"Access of possibly undefined property {identifier}.");
											}
										}
										else if (
											_Rv == FindIdResultType.ScopeMember &&
												(
													(scopeMember).Kind == ScopeMemberKind.Constant ||
													(scopeMember).Kind == ScopeMemberKind.Slot ||

													(scopeMember).Kind == ScopeMemberKind.Parameter
												)
											)
										{
											if ((scopeMember).Kind == ScopeMemberKind.Constant && (scopeMember).ValueKind == ConstantKind.Namespace)
											{
												throw new ResolverException(step.token, $"Attempt to delete the fixed property {identifier}.  Only dynamically defined properties can be deleted.");
											}
											else
											{
												throw new ResolverException(step.token, $"Attempt to delete the fixed property {identifier}.  Only dynamically defined properties can be deleted.");
											}
										}
										else if (_Rv == FindIdResultType.NeedLoadClass_ScopeMember
											||
											_Rv == FindIdResultType.VTableItems
											||
											_Rv == FindIdResultType.NeedLoadClass_VTableItems
											)
										{
											throw new ResolverException(step.token, $"Attempt to delete the fixed property {identifier}.  Only dynamically defined properties can be deleted.");
										}
										else if (_Rv == FindIdResultType.Ambiguous)
										{
											throw new ResolverException(step.token, $"Ambiguous reference to {identifier}");
										}
										else
										{
											var match = compileEnv.imports.Where((t) => { return t.QName.Name == identifier; }).ToArray();
											if (match.Length > 0)
											{
												throw new ResolverException(step.token, $"Ambiguous reference to {match[0].QName.Name}");
											}

											throw new InvalidOperationException();
										}
									}
								}

							}
							break;
						case FF1DataValueType.this_pointer:
						case FF1DataValueType.super_pointer:
							throw new ResolverException(step.token, "Cannot assign to a non-reference value.");
						case FF1DataValueType.const_number:
						case FF1DataValueType.const_string:
						case FF1DataValueType.const_regexp:
							{ //直接删除一个常量

								StackLocater dst = compileEnv.GetStackLocater(step.Arg1.Reg, TypeKind.Boolean);
								INS_Ld_True ld_True = new INS_Ld_True(step.token);
								ld_True.dst = dst;
								compileEnv.instructions.Add(ld_True);

							}
							break;
						case FF1DataValueType.as3_function:
							{ //直接删除一个内嵌function

								StackLocater dst = compileEnv.GetStackLocater(step.Arg1.Reg, TypeKind.Boolean);
								INS_Ld_True ld_True = new INS_Ld_True(step.token);
								ld_True.dst = dst;
								compileEnv.instructions.Add(ld_True);

							}
							break;
						case FF1DataValueType.as3_array:
							{ //直接删除一个常量

								StackLocater dst = compileEnv.GetStackLocater(step.Arg1.Reg, TypeKind.Boolean);
								INS_Ld_True ld_True = new INS_Ld_True(step.token);
								ld_True.dst = dst;
								compileEnv.instructions.Add(ld_True);

							}
							break;
						case FF1DataValueType.as3_vector:
							{ //直接删除一个常量

								StackLocater dst = compileEnv.GetStackLocater(step.Arg1.Reg, TypeKind.Boolean);
								INS_Ld_True ld_True = new INS_Ld_True(step.token);
								ld_True.dst = dst;
								compileEnv.instructions.Add(ld_True);

							}
							break;
						case FF1DataValueType.as3_callarguments:
						case FF1DataValueType.as3_expressionlist:
						case FF1DataValueType.compiler_const:
						default:
							throw new InvalidOperationException();
					}
				}

				//throw new NotImplementedException();
			}
			else if (step.OpCode == "void")
			{
				StackLocater rightvalue = LoadRightValue(step.Arg2, compileEnv, step.token);

				StackLocater dst = compileEnv.GetStackLocater(step.Arg1.Reg, TypeKind.Any);
				INS_Ld_Undefined ld_Const = new INS_Ld_Undefined(step.token);
				ld_Const.dst = dst;

				compileEnv.instructions.Add(ld_Const);
			}
			else if (step.OpCode == "--" || step.OpCode == "++")
			{
				var ovalue = LoadRightValue(step.Arg2, compileEnv, step.token);

				StackLocater newvalue = compileEnv.GetStackLocater(step.Arg1.Reg, TypeKind.Number);
				StackLocater result = compileEnv.GetStackLocater(step.Arg1.Reg, TypeKind.Number);

				INS_Incr_Decr Incr_Decr = new INS_Incr_Decr(step.token);
				Incr_Decr.source = ovalue;
				Incr_Decr.dst = newvalue;
				Incr_Decr.result = result;

				Incr_Decr.addvalue = step.OpCode == "++" ? 1 : -1;


				compileEnv.instructions.Add(Incr_Decr);


				AS3ExprStep stepAssign = new AS3ExprStep(step.token);
				stepAssign.Arg1 = step.Arg2;
				stepAssign.Arg2 = step.Arg1;
				stepAssign.Type = OpType.Assigning;
				stepAssign.OpCode = "=";

				BuildAssigning(stepAssign, compileEnv, "Operand of " + (step.OpCode == "++" ? "increment" : "decrement"));

				//throw new NotImplementedException();

			}
			else if (step.OpCode == "~")
			{
				StackLocater rightvalue = LoadRightValue(step.Arg2, compileEnv, step.token);
				{
					TypeKind rvtype = compileEnv.ReadStackType(rightvalue).Maj;
					if (rvtype.IsNumericType() || rvtype == TypeKind.Any || rvtype == TypeKind.Null)
					{

					}
					else
					{
						throw new ResolverException(step.token, $"Implicit coercion of a value of type {TypeUtils.ToTypeString(rvtype,compileEnv.CompileContext)} to an unrelated type Number.");
					}

				}


				TypeKind typeKind = TypeKind.Int;
				
				StackLocater dst = compileEnv.GetStackLocater(step.Arg1.Reg, typeKind);
				INS_BitWise bitnot = new INS_BitWise(step.token);
				bitnot.dst = dst;
				bitnot.v1 = rightvalue;
				bitnot.v2 = rightvalue;
				bitnot.wiseMode = INS_BitWise.BItWiseMode.bitwise_not;

				compileEnv.instructions.Add(bitnot);
			}
			else if (step.OpCode == "await")
			{
				// await is only valid in async functions

				var m = ((ASMethodBody)compileEnv.Scope.Container).Method;

				if (m.Container is ASScript) //允许在包外代码await
				{
					m.Flags = m.Flags | MethodFlags.ASYNC;
					m.Flags = m.Flags | MethodFlags.NeedActivation;

				}

				else if (!m.Flags.HasFlag(MethodFlags.ASYNC))
				{
					throw new ResolverException(step.token, "await is only valid in async functions");
				}

				var trycatchState = compileEnv.stack_trycatchctx.Peek();
				if (trycatchState.Any((t) =>
					//t.state == TryCatchContext.State.Catch || 
					t.state == TryCatchContext.State.Finally))
				{
					throw new ResolverException(m.Token, "Can't await in catch block or finally block");
				}

				StackLocater rightvalue = LoadRightValue(step.Arg2, compileEnv, step.token);
				INS_Await_Return iNS_Await = new INS_Await_Return(step.token);
				iNS_Await.dst = rightvalue;

				compileEnv.instructions.Add(iNS_Await);

				StackLocater dst = compileEnv.GetStackLocater(step.Arg1.Reg);
				INS_Await_Resume _Await_Resume = new INS_Await_Resume(step.token);
				_Await_Resume.dst = dst;

				compileEnv.instructions.Add(_Await_Resume);
			}
			else
			{
				throw new NotImplementedException();
			}
		}


		private void BuildLogic(AS3ExprStep step, AS3Expression expression, CompileEnv compileEnv)
		{
			if (step.OpCode == "instanceof")
			{
				StackLocater v1 = LoadRightValue(step.Arg2, compileEnv, step.token);
				StackLocater v2 = LoadRightValue(step.Arg3, compileEnv, step.token);
				StackLocater dst = compileEnv.GetStackLocater(step.Arg1.Reg, TypeKind.Boolean);

				var type = compileEnv.ReadStackType(v2);

				//if (type.Maj == TypeKind.Vector)
				//{
				//	VectorDef vd = compileEnv.CompileContext.vectorDefs.First(v => v.Identifier == type.Mir);
				//	vd.
				//}

				if (type.Maj == TypeKind.Any || type.Maj == TypeKind.Class || type.Maj == TypeKind.Function || type.Maj == TypeKind.Vector

					)
				{
					INS_InstanceOf _instanceof = new INS_InstanceOf(step.token);
					_instanceof.dst = dst;
					_instanceof.src = v1;
					_instanceof.type = v2;
					compileEnv.instructions.Add(_instanceof);
				}
				else
				{
					throw new ResolverException(step.token, "The right-hand side of instanceof must be a class or function.");
				}
			}
			else if (step.OpCode == "<")
			{
				StackLocater v1 = LoadRightValue(step.Arg2, compileEnv, step.token);
				StackLocater v2 = LoadRightValue(step.Arg3, compileEnv, step.token);
				StackLocater dst = compileEnv.GetStackLocater(step.Arg1.Reg, TypeKind.Boolean);

				INS_Comparison comparison = new INS_Comparison(step.token);
				comparison.dst = dst;
				comparison.v1 = v1;
				comparison.v2 = v2;
				comparison.opMode = 0;

				compileEnv.instructions.Add(comparison);

			}
			else if (step.OpCode == "<=")
			{
				StackLocater v1 = LoadRightValue(step.Arg2, compileEnv, step.token);
				StackLocater v2 = LoadRightValue(step.Arg3, compileEnv, step.token);
				StackLocater dst = compileEnv.GetStackLocater(step.Arg1.Reg, TypeKind.Boolean);

				INS_Comparison comparison = new INS_Comparison(step.token);
				comparison.dst = dst;
				comparison.v1 = v1;
				comparison.v2 = v2;
				comparison.opMode = 2;

				compileEnv.instructions.Add(comparison);

			}
			else if (step.OpCode == ">")
			{
				StackLocater v1 = LoadRightValue(step.Arg2, compileEnv, step.token);
				StackLocater v2 = LoadRightValue(step.Arg3, compileEnv, step.token);
				StackLocater dst = compileEnv.GetStackLocater(step.Arg1.Reg, TypeKind.Boolean);

				INS_Comparison comparison = new INS_Comparison(step.token);
				comparison.dst = dst;
				comparison.v1 = v1;
				comparison.v2 = v2;
				comparison.opMode = 1;

				compileEnv.instructions.Add(comparison);

			}
			else if (step.OpCode == ">=")
			{
				StackLocater v1 = LoadRightValue(step.Arg2, compileEnv, step.token);
				StackLocater v2 = LoadRightValue(step.Arg3, compileEnv, step.token);
				StackLocater dst = compileEnv.GetStackLocater(step.Arg1.Reg, TypeKind.Boolean);

				INS_Comparison comparison = new INS_Comparison(step.token);
				comparison.dst = dst;
				comparison.v1 = v1;
				comparison.v2 = v2;
				comparison.opMode = 3;

				compileEnv.instructions.Add(comparison);

			}
			else if (step.OpCode == "in")
			{
				StackLocater v1 = LoadRightValue(step.Arg2, compileEnv, step.token);
				StackLocater v2 = LoadRightValue(step.Arg3, compileEnv, step.token);
				StackLocater dst = compileEnv.GetStackLocater(step.Arg1.Reg, TypeKind.Boolean);

				INS_In isin = new INS_In(step.token);
				isin.dst = dst;
				isin.v1 = v1;
				isin.v2 = v2;

				compileEnv.instructions.Add(isin);
			}
			else if (step.OpCode == "is")
			{
				StackLocater v1 = LoadRightValue(step.Arg2, compileEnv, step.token);
				StackLocater v2 = LoadRightValue(step.Arg3, compileEnv, step.token);

				var istype = TestTypeConvert(compileEnv, v2, new CompileTypeKind() { Maj = TypeKind.Class }, step.token);

				StackLocater dst = compileEnv.GetStackLocater(step.Arg1.Reg, TypeKind.Boolean);

				INS_Is insIs = new INS_Is(step.token);
				insIs.dst = dst;
				insIs.v1 = v1;
				insIs.v2 = istype;

				compileEnv.instructions.Add(insIs);
			}
			else if (step.OpCode == "as")
			{
				StackLocater v1 = LoadRightValue(step.Arg2, compileEnv, step.token);
				StackLocater v2 = LoadRightValue(step.Arg3, compileEnv, step.token);

				var istype = TestTypeConvert(compileEnv, v2, new CompileTypeKind() { Maj = TypeKind.Class }, step.token);

				var t = compileEnv.ReadStackType(v2);
				if (t.Maj != TypeKind.Class)
				{
					t.Maj = TypeKind.Any;
					t.Mir = TypeKind.Any;
				}

				StackLocater dst = compileEnv.GetStackLocater(step.Arg1.Reg,t.Maj,t.Mir );

				INS_As insAs = new INS_As(step.token);
				insAs.dst = dst;
				insAs.v1 = v1;
				insAs.v2 = istype;

				compileEnv.instructions.Add(insAs);
			}

			else
			{
				throw new NotImplementedException();
			}
		}

		private void BuildLogicEQ(AS3ExprStep step, AS3Expression expression, CompileEnv compileEnv)
		{
			if (step.OpCode == "!==")
			{
				StackLocater v1 = LoadRightValue(step.Arg2, compileEnv, step.token);
				StackLocater v2 = LoadRightValue(step.Arg3, compileEnv, step.token);
				StackLocater dst = compileEnv.GetStackLocater(step.Arg1.Reg, TypeKind.Boolean);

				INS_Strict_Neq neq = new INS_Strict_Neq(step.token);
				neq.dst = dst;
				neq.v1 = v1;
				neq.v2 = v2;
				compileEnv.instructions.Add(neq);

			}
			else if (step.OpCode == "===")
			{
				StackLocater v1 = LoadRightValue(step.Arg2, compileEnv, step.token);
				StackLocater v2 = LoadRightValue(step.Arg3, compileEnv, step.token);
				StackLocater dst = compileEnv.GetStackLocater(step.Arg1.Reg, TypeKind.Boolean);

				INS_Strict_Eq eq = new INS_Strict_Eq(step.token);
				eq.dst = dst;
				eq.v1 = v1;
				eq.v2 = v2;
				compileEnv.instructions.Add(eq);
			}
			else if (step.OpCode == "!=")
			{
				StackLocater v1 = LoadRightValue(step.Arg2, compileEnv, step.token);
				StackLocater v2 = LoadRightValue(step.Arg3, compileEnv, step.token);
				StackLocater dst = compileEnv.GetStackLocater(step.Arg1.Reg, TypeKind.Boolean);

				INS_NotEqual eq = new INS_NotEqual(step.token);
				eq.dst = dst;
				eq.v1 = v1;
				eq.v2 = v2;
				compileEnv.instructions.Add(eq);
			}
			else if (step.OpCode == "==")
			{
				StackLocater v1 = LoadRightValue(step.Arg2, compileEnv, step.token);
				StackLocater v2 = LoadRightValue(step.Arg3, compileEnv, step.token);
				StackLocater dst = compileEnv.GetStackLocater(step.Arg1.Reg, TypeKind.Boolean);

				INS_Equal eq = new INS_Equal(step.token);
				eq.dst = dst;
				eq.v1 = v1;
				eq.v2 = v2;
				compileEnv.instructions.Add(eq);
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		private void BuildBitAnd(AS3ExprStep step, AS3Expression expression, CompileEnv compileEnv)
		{
			StackLocater v1 = LoadRightValue(step.Arg2, compileEnv, step.token);
			{
				TypeKind rvtype = compileEnv.ReadStackType(v1).Maj;
				if (rvtype.IsNumericType() || rvtype == TypeKind.Any || rvtype == TypeKind.Null)
				{

				}
				else
				{
					throw new ResolverException(step.token, $"Implicit coercion of a value of type {TypeUtils.ToTypeString(rvtype,compileEnv.CompileContext)} to an unrelated type Number.");
				}

			}

			StackLocater v2 = LoadRightValue(step.Arg3, compileEnv, step.token);
			{
				TypeKind rvtype = compileEnv.ReadStackType(v2).Maj;
				if (rvtype.IsNumericType() || rvtype == TypeKind.Any || rvtype == TypeKind.Null)
				{

				}
				else
				{
					throw new ResolverException(step.token, $"Implicit coercion of a value of type {TypeUtils.ToTypeString(rvtype,compileEnv.CompileContext)} to an unrelated type Number.");
				}
			}

			var t1 = compileEnv.ReadStackType(v1);
			var t2 = compileEnv.ReadStackType(v2);

		
			StackLocater dst = compileEnv.GetStackLocater(step.Arg1.Reg, TypeKind.Int );
			INS_BitWise bitwise = new INS_BitWise(step.token);
			bitwise.dst = dst;
			bitwise.v1 = v1;
			bitwise.v2 = v2;
			bitwise.wiseMode = INS_BitWise.BItWiseMode.bitwise_and;

			
			compileEnv.instructions.Add(bitwise);
		}

		

		private void BuildBitOr(AS3ExprStep step, AS3Expression expression, CompileEnv compileEnv)
		{
			StackLocater v1 = LoadRightValue(step.Arg2, compileEnv, step.token);
			{
				TypeKind rvtype = compileEnv.ReadStackType(v1).Maj;
				if (rvtype.IsNumericType() || rvtype == TypeKind.Any || rvtype == TypeKind.Null)
				{

				}
				else
				{
					throw new ResolverException(step.token, $"Implicit coercion of a value of type {TypeUtils.ToTypeString(rvtype,compileEnv.CompileContext)} to an unrelated type Number.");
				}

			}

			StackLocater v2 = LoadRightValue(step.Arg3, compileEnv, step.token);
			{
				TypeKind rvtype = compileEnv.ReadStackType(v2).Maj;
				if (rvtype.IsNumericType() || rvtype == TypeKind.Any || rvtype == TypeKind.Null)
				{

				}
				else
				{
					throw new ResolverException(step.token, $"Implicit coercion of a value of type {TypeUtils.ToTypeString(rvtype,compileEnv.CompileContext)} to an unrelated type Number.");
				}
			}

			var t1 = compileEnv.ReadStackType(v1);
			var t2 = compileEnv.ReadStackType(v2);


			StackLocater dst = compileEnv.GetStackLocater(step.Arg1.Reg, TypeKind.Int);
			INS_BitWise bitwise = new INS_BitWise(step.token);
			bitwise.dst = dst;
			bitwise.v1 = v1;
			bitwise.v2 = v2;
			bitwise.wiseMode = INS_BitWise.BItWiseMode.bitwise_or;


			compileEnv.instructions.Add(bitwise);
		}

		private void BuildBitXor(AS3ExprStep step, AS3Expression expression, CompileEnv compileEnv)
		{
			StackLocater v1 = LoadRightValue(step.Arg2, compileEnv, step.token);
			{
				TypeKind rvtype = compileEnv.ReadStackType(v1).Maj;
				if (rvtype.IsNumericType() || rvtype == TypeKind.Any || rvtype == TypeKind.Null)
				{

				}
				else
				{
					throw new ResolverException(step.token, $"Implicit coercion of a value of type {TypeUtils.ToTypeString(rvtype,compileEnv.CompileContext)} to an unrelated type Number.");
				}

			}

			StackLocater v2 = LoadRightValue(step.Arg3, compileEnv, step.token);
			{
				TypeKind rvtype = compileEnv.ReadStackType(v2).Maj;
				if (rvtype.IsNumericType() || rvtype == TypeKind.Any || rvtype == TypeKind.Null)
				{

				}
				else
				{
					throw new ResolverException(step.token, $"Implicit coercion of a value of type {TypeUtils.ToTypeString(rvtype,compileEnv.CompileContext)} to an unrelated type Number.");
				}
			}

			var t1 = compileEnv.ReadStackType(v1);
			var t2 = compileEnv.ReadStackType(v2);


			StackLocater dst = compileEnv.GetStackLocater(step.Arg1.Reg, TypeKind.Int);
			INS_BitWise bitwise = new INS_BitWise(step.token);
			bitwise.dst = dst;
			bitwise.v1 = v1;
			bitwise.v2 = v2;
			bitwise.wiseMode = INS_BitWise.BItWiseMode.xor;


			compileEnv.instructions.Add(bitwise);
		}

		private void BuildBitShift(AS3ExprStep step, AS3Expression expression, CompileEnv compileEnv)
		{
			StackLocater v1 = LoadRightValue(step.Arg2, compileEnv, step.token);
			{
				TypeKind rvtype = compileEnv.ReadStackType(v1).Maj;
				if (rvtype.IsNumericType() || rvtype == TypeKind.Any || rvtype == TypeKind.Null)
				{

				}
				else
				{
					throw new ResolverException(step.token, $"Implicit coercion of a value of type {TypeUtils.ToTypeString(rvtype,compileEnv.CompileContext)} to an unrelated type Number.");
				}

			}

			StackLocater v2 = LoadRightValue(step.Arg3, compileEnv, step.token);
			{
				TypeKind rvtype = compileEnv.ReadStackType(v2).Maj;
				if (rvtype.IsNumericType() || rvtype == TypeKind.Any || rvtype == TypeKind.Null)
				{

				}
				else
				{
					throw new ResolverException(step.token, $"Implicit coercion of a value of type {TypeUtils.ToTypeString(rvtype,compileEnv.CompileContext)} to an unrelated type Number.");
				}
			}

			var t1 = compileEnv.ReadStackType(v1);
			var t2 = compileEnv.ReadStackType(v2);


			StackLocater dst = compileEnv.GetStackLocater(step.Arg1.Reg, step.OpCode == ">>>" ? TypeKind.Uint : TypeKind.Int);
			INS_BitWise bitwise = new INS_BitWise(step.token);
			bitwise.dst = dst;
			bitwise.v1 = v1;
			bitwise.v2 = v2;

			if (step.OpCode == "<<")
			{
				bitwise.wiseMode = INS_BitWise.BItWiseMode.left_shift;
			}
			else if (step.OpCode == ">>")
			{
				bitwise.wiseMode = INS_BitWise.BItWiseMode.right_shift;
			}
			else if (step.OpCode == ">>>")
			{
				bitwise.wiseMode = INS_BitWise.BItWiseMode.unsigned_right_shift;
			}
			else
			{
				throw new InvalidOperationException();
			}

			

			compileEnv.instructions.Add(bitwise);
		}


		private void BuildMultiply(AS3ExprStep step, AS3Expression expression, CompileEnv compileEnv)
		{
			if (step.OpCode == "/")
			{
				StackLocater v1 = LoadRightValue(step.Arg2, compileEnv, step.token);
				StackLocater v2 = LoadRightValue(step.Arg3, compileEnv, step.token);

				var t1 = compileEnv.ReadStackType(v1);
				var t2 = compileEnv.ReadStackType(v2);

				if (build_operator_override(Player.OverrideOperator.div, t1, t2, v1, v2, compileEnv, step))
				{
					return;
				}

				{
					TypeKind rvtype = t1.Maj;
					if (rvtype.IsNumericType() || rvtype == TypeKind.Any || rvtype == TypeKind.Null)
					{

					}
					else
					{
						throw new ResolverException(step.token, $"Implicit coercion of a value of type {TypeUtils.ToTypeString(rvtype,compileEnv.CompileContext)} to an unrelated type Number.");
					}

				}

				{
					TypeKind rvtype = t2.Maj;
					if (rvtype.IsNumericType() || rvtype == TypeKind.Any || rvtype == TypeKind.Null)
					{

					}
					else
					{
						throw new ResolverException(step.token, $"Implicit coercion of a value of type {TypeUtils.ToTypeString(rvtype,compileEnv.CompileContext)} to an unrelated type Number.");
					}
				}

				TypeKind rtype;
				if (t1.Maj == TypeKind.Any || t2.Maj == TypeKind.Any)
				{
					rtype = TypeKind.Any;
				}
				else if (t1.Maj == TypeKind.Null || t2.Maj == TypeKind.Null)
				{
					rtype = TypeKind.Any;
				}
				else if (t1.Maj == TypeKind.Number || t2.Maj == TypeKind.Number)
				{
					rtype = TypeKind.Number;
				}
				else if (t1.Maj == TypeKind.Float || t2.Maj == TypeKind.Float)
				{
					rtype = TypeKind.Float;
				}
				else
				{
					rtype = TypeKind.Number;
				}


				StackLocater dst = compileEnv.GetStackLocater(step.Arg1.Reg, rtype);
				INS_Div div = new INS_Div(step.token);
				div.dst = dst;
				div.v1 = v1;
				div.v2 = v2;

				compileEnv.instructions.Add(div);
			}
			else if (step.OpCode == "*")
			{
				StackLocater v1 = LoadRightValue(step.Arg2, compileEnv, step.token);
				StackLocater v2 = LoadRightValue(step.Arg3, compileEnv, step.token);

				var t1 = compileEnv.ReadStackType(v1);
				var t2 = compileEnv.ReadStackType(v2);

				if (build_operator_override(Player.OverrideOperator.mul, t1, t2, v1, v2, compileEnv, step))
				{
					return;
				}

				{
					TypeKind rvtype = t1.Maj;
					if (rvtype.IsNumericType() || rvtype == TypeKind.Any || rvtype == TypeKind.Null)
					{

					}
					else
					{
						throw new ResolverException(step.token, $"Implicit coercion of a value of type {TypeUtils.ToTypeString(rvtype,compileEnv.CompileContext)} to an unrelated type Number.");
					}

				}

				{
					TypeKind rvtype = t2.Maj;
					if (rvtype.IsNumericType() || rvtype == TypeKind.Any || rvtype == TypeKind.Null)
					{

					}
					else
					{
						throw new ResolverException(step.token, $"Implicit coercion of a value of type {TypeUtils.ToTypeString(rvtype,compileEnv.CompileContext)} to an unrelated type Number.");
					}
				}

				TypeKind rtype;
				if (t1.Maj == TypeKind.Any || t2.Maj == TypeKind.Any)
				{
					rtype = TypeKind.Any;
				}
				else if (t1.Maj == TypeKind.Null || t2.Maj == TypeKind.Null)
				{
					rtype = TypeKind.Any;
				}
				else if (t1.Maj == TypeKind.Number || t2.Maj == TypeKind.Number)
				{
					rtype = TypeKind.Number;
				}
				else if (t1.Maj == TypeKind.Float || t2.Maj == TypeKind.Float)
				{
					rtype = TypeKind.Float;
				}
				else if (t1.Maj == TypeKind.Uint || t2.Maj == TypeKind.Uint
						|| t1.Maj == TypeKind.Int || t2.Maj == TypeKind.Int
					)
				{
					rtype = TypeKind.Number;
				}
				else
				{
					rtype = TypeKind.Int;
				}

				StackLocater dst = compileEnv.GetStackLocater(step.Arg1.Reg, rtype);
				INS_Multiply add = new INS_Multiply(step.token);
				add.dst = dst;
				add.v1 = v1;
				add.v2 = v2;

				compileEnv.instructions.Add(add);

			}
			else if (step.OpCode == "%")
			{
				StackLocater v1 = LoadRightValue(step.Arg2, compileEnv, step.token);
				StackLocater v2 = LoadRightValue(step.Arg3, compileEnv, step.token);

				var t1 = compileEnv.ReadStackType(v1);
				var t2 = compileEnv.ReadStackType(v2);

				if (build_operator_override(Player.OverrideOperator.mod, t1, t2, v1, v2, compileEnv, step))
				{
					return;
				}

				{
					TypeKind rvtype = t1.Maj;
					if (rvtype.IsNumericType() || rvtype == TypeKind.Any || rvtype == TypeKind.Null)
					{

					}
					else
					{
						throw new ResolverException(step.token, $"Implicit coercion of a value of type {TypeUtils.ToTypeString(rvtype,compileEnv.CompileContext)} to an unrelated type Number.");
					}

				}

				{
					TypeKind rvtype = t2.Maj;
					if (rvtype.IsNumericType() || rvtype == TypeKind.Any || rvtype == TypeKind.Null)
					{

					}
					else
					{
						throw new ResolverException(step.token, $"Implicit coercion of a value of type {TypeUtils.ToTypeString(rvtype,compileEnv.CompileContext)} to an unrelated type Number.");
					}
				}

				TypeKind rtype;
				if (t1.Maj == TypeKind.Any || t2.Maj == TypeKind.Any)
				{
					rtype = TypeKind.Any;
				}
				else if (t1.Maj == TypeKind.Null || t2.Maj == TypeKind.Null)
				{
					rtype = TypeKind.Any;
				}
				else if (t1.Maj == TypeKind.Number || t2.Maj == TypeKind.Number)
				{
					rtype = TypeKind.Number;
				}
				else if (t1.Maj == TypeKind.Float || t2.Maj == TypeKind.Float)
				{
					rtype = TypeKind.Float;
				}
				//else if (t1.Maj == TypeKind.Uint || t2.Maj == TypeKind.Uint)
				//{
				//	if (t1.Maj == TypeKind.Boolean || t2.Maj == TypeKind.Boolean ||
				//		t1.Maj == TypeKind.Null || t2.Maj == TypeKind.Null ||
				//		t1.Maj == TypeKind.Int || t2.Maj == TypeKind.Int ||
				//		t1.Maj == TypeKind.SByte || t2.Maj == TypeKind.SByte ||
				//		t1.Maj == TypeKind.Short || t2.Maj == TypeKind.Short

				//		)
				//	{
				//		rtype = TypeKind.Number;
				//	}
				//	else
				//	{
				//		rtype = TypeKind.Uint;
				//	}
				//}
				else
				{
					rtype = TypeKind.Number;
				}

				
				StackLocater dst = compileEnv.GetStackLocater(step.Arg1.Reg, rtype);
				INS_Modulus modulus = new INS_Modulus(step.token);
				modulus.dst = dst;
				modulus.v1 = v1;
				modulus.v2 = v2;

				compileEnv.instructions.Add(modulus);
			}
			else
			{
				throw new NotImplementedException();
			}
		}


		private void BuildPlus(AS3ExprStep step, CompileEnv compileEnv)
		{
			
			if (step.OpCode == "+")
			{
				StackLocater v1 = LoadRightValue(step.Arg2, compileEnv, step.token);
				StackLocater v2 = LoadRightValue(step.Arg3, compileEnv, step.token);

				var t1 = compileEnv.ReadStackType(v1);
				var t2 = compileEnv.ReadStackType(v2);

				if (build_operator_override(Player.OverrideOperator.add, t1, t2,v1,v2,compileEnv,step))
				{
					goto lbl_complete;
				}

				TypeKind rtype;

				if (t1.Maj == TypeKind.String || t2.Maj == TypeKind.String)
				{
					rtype = TypeKind.String;
				}
				else if ((t1.Maj.IsNumericType() || t1.Maj == TypeKind.Boolean) && (t2.Maj.IsNumericType() || t2.Maj == TypeKind.Boolean))
				{
					if (t1.Maj == TypeKind.SByte || t1.Maj == TypeKind.Byte || t1.Maj == TypeKind.Short || t1.Maj == TypeKind.UShort || t1.Maj == TypeKind.Boolean)
					{
						switch (t2.Maj)
						{
							case TypeKind.Number:
							case TypeKind.Uint:
								rtype = TypeKind.Number;
								break;
							case TypeKind.SByte:
							case TypeKind.Byte:
							case TypeKind.Short:
							case TypeKind.UShort:
							case TypeKind.Boolean:
							case TypeKind.Int:
								rtype = TypeKind.Int;
								break;
							case TypeKind.Float:
								rtype = TypeKind.Float;
								break;
							default:
								throw new InvalidOperationException();
						}
					}
					else if (t1.Maj == TypeKind.Int)
					{
						if (t2.Maj == TypeKind.Float)
						{
							rtype = TypeKind.Float;
						}
						else if (t2.Maj == TypeKind.Number || t2.Maj == TypeKind.Uint)
						{
							rtype = TypeKind.Number;
						}
						else
						{
							rtype = TypeKind.Int;
						}
					}
					else if (t1.Maj == TypeKind.Uint)
					{
						switch (t2.Maj)
						{
							case TypeKind.Number:
							case TypeKind.Int:
							case TypeKind.SByte:
							case TypeKind.Short:
							case TypeKind.Boolean:
								rtype = TypeKind.Number;
								break;
							case TypeKind.Uint:
							case TypeKind.Byte:
							case TypeKind.UShort:
								rtype = TypeKind.Uint;
								break;
							case TypeKind.Float:
								rtype = TypeKind.Float;
								break;
							default:
								throw new InvalidOperationException();
						}
					}
					else if (t1.Maj == TypeKind.Float)
					{
						if (t2.Maj == TypeKind.Number || t2.Maj == TypeKind.Uint)
						{
							rtype = TypeKind.Number;
						}
						else
						{
							rtype = TypeKind.Float;
						}
					}
					else
					{
						//Number
						rtype = TypeKind.Number;
					}

				}
				else
				{
					rtype = TypeKind.Any; //Number或字符串，不知道。
				}

				StackLocater dst = compileEnv.GetStackLocater(step.Arg1.Reg, rtype);
				INS_Add add = new INS_Add(step.token);
				add.dst = dst;
				add.v1 = v1;
				add.v2 = v2;

				compileEnv.instructions.Add(add);

			lbl_complete:
				;
			}
			else if (step.OpCode == "-")
			{
				StackLocater v1 = LoadRightValue(step.Arg2, compileEnv, step.token);
				StackLocater v2 = LoadRightValue(step.Arg3, compileEnv, step.token);

				var t1 = compileEnv.ReadStackType(v1);
				var t2 = compileEnv.ReadStackType(v2);

				if (build_operator_override(Player.OverrideOperator.sub, t1, t2, v1, v2, compileEnv, step))
				{
					goto lbl_complete;
				}

				TypeKind rtype;

				if (t1.Maj == TypeKind.Object || t2.Maj == TypeKind.Object)
				{
					rtype = TypeKind.Any;
				}
				else if (t1.Maj == TypeKind.Boolean || t2.Maj == TypeKind.Boolean)
				{
					throw new ResolverException(step.token, $"Implicit coercion of a value of type Boolean to an unrelated type Number.");
				}
				else if (t1.Maj == TypeKind.String || t2.Maj == TypeKind.String)
				{
					throw new ResolverException(step.token, $"Implicit coercion of a value of type String to an unrelated type Number.");
				}
				else if (t1.Maj == TypeKind.Any || t2.Maj == TypeKind.Any)
				{
					rtype = TypeKind.Any;
				}
				else if (t1.Maj == TypeKind.Null || t2.Maj == TypeKind.Null)
				{
					rtype = TypeKind.Any;
				}
				else if ((t1.Maj.IsNumericType()) && (t2.Maj.IsNumericType()))
				{
					if (t1.Maj == TypeKind.SByte || t1.Maj == TypeKind.Byte || t1.Maj == TypeKind.Short || t1.Maj == TypeKind.UShort)
					{
						switch (t2.Maj)
						{
							case TypeKind.Number:
							case TypeKind.Uint:
								rtype = TypeKind.Number;
								break;
							case TypeKind.SByte:
							case TypeKind.Byte:
							case TypeKind.Short:
							case TypeKind.UShort:
							case TypeKind.Boolean:
							case TypeKind.Int:
								rtype = TypeKind.Int;
								break;
							case TypeKind.Float:
								rtype = TypeKind.Float;
								break;
							default:
								throw new InvalidOperationException();
						}
					}
					else if (t1.Maj == TypeKind.Int)
					{
						if (t2.Maj == TypeKind.Float)
						{
							rtype = TypeKind.Float;
						}
						else if (t2.Maj == TypeKind.Number || t2.Maj == TypeKind.Uint)
						{
							rtype = TypeKind.Number;
						}
						else
						{
							rtype = TypeKind.Int;
						}
					}
					else if (t1.Maj == TypeKind.Uint)
					{
						switch (t2.Maj)
						{
							case TypeKind.Number:
							case TypeKind.Int:
							case TypeKind.SByte:
							case TypeKind.Short:
								rtype = TypeKind.Number;
								break;
							case TypeKind.Uint:
							case TypeKind.Byte:
							case TypeKind.UShort:
							case TypeKind.Boolean:
								rtype = TypeKind.Uint;
								break;
							case TypeKind.Float:
								rtype = TypeKind.Float;
								break;
							default:
								throw new InvalidOperationException();
						}
					}
					else if (t1.Maj == TypeKind.Float)
					{
						if (t2.Maj == TypeKind.Number || t2.Maj == TypeKind.Uint)
						{
							rtype = TypeKind.Number;
						}
						else
						{
							rtype = TypeKind.Float;
						}
					}
					else
					{
						//Number
						rtype = TypeKind.Number;
					}
				}
				else
				{
					throw new ResolverException(step.token, $"Implicit coercion of a value of type {(t1.Maj.IsNumericType() ? TypeUtils.ToTypeString( t2.Maj,compileEnv.CompileContext) : TypeUtils.ToTypeString( t1.Maj,compileEnv.CompileContext))} to an unrelated type Number.");
					//rtype = TypeKind.Number; //减法结果一定是数字。
				}

				StackLocater dst = compileEnv.GetStackLocater(step.Arg1.Reg, rtype);
				INS_Sub sub = new INS_Sub(step.token);
				sub.dst = dst;
				sub.v1 = v1;
				sub.v2 = v2;

				compileEnv.instructions.Add(sub);
			lbl_complete:
				;
			}
			else
			{
				throw new NotImplementedException();
			}
		}


		private static bool build_operator_override(Player.OverrideOperator op, CompileTypeKind t1, CompileTypeKind t2, StackLocater v1, StackLocater v2,CompileEnv compileEnv,AS3ExprStep step)
		{
			if (t1.Maj != TypeKind.Any && t2.Maj != TypeKind.Any
						)
			{
				var c1 = TypeUtils.FindOverrideTypeId(t1.Maj, compileEnv.CompileContext);
				var c2 = TypeUtils.FindOverrideTypeId(t2.Maj, compileEnv.CompileContext);

				if (c1 != -1 && c2 != -1)
				{
					var method =
						compileEnv.CompileContext.player_for_compiler.overrideOperatorMethods[(int)op]
						[c1][c2];

					if (method != null)
					{
						var vtableitem = method.Container._vtable.Items.Find(i => i.Trait.Method == method);

						INS_Ld_Class ld_cls = new INS_Ld_Class(step.token);
						ld_cls.dst = compileEnv.MakeStackLocater(TypeKind.Class);
						ld_cls.classid_index = compileEnv.AddConstClassId((ASClass)method.Container);
						compileEnv.instructions.Add(ld_cls);

						INS_Ld_Method Ld_Method = new INS_Ld_Method(step.token);
						Ld_Method.dst = compileEnv.MakeStackLocater(TypeKind.Function);
						Ld_Method.const_index = (uint)method.Container._vtable.Items.IndexOf(vtableitem);
						Ld_Method.instance = ld_cls.dst;
						compileEnv.instructions.Add(Ld_Method);

						INS_Method_Call method_Call = new INS_Method_Call(step.token);
						method_Call.dst = compileEnv.GetStackLocater(step.Arg1.Reg, method.ReturnTypeKind);
						method_Call.function = Ld_Method.dst;

						if (op == Player.OverrideOperator.neg || op == Player.OverrideOperator.positive)
						{
							method_Call.args = new StackLocater[1] { v1 };
						}
						else
						{
							method_Call.args = new StackLocater[2] { v1, v2 };
						}

						compileEnv.instructions.Add(method_Call);
						return true;
					}
					else if (c1 > 10 || c2 > 10)
					{
						throw new ResolverException(step.token, $"operator override({op.ToString()}) found.But type not match.");
					}
					else
					{
						return false;
					}
				}
				else if (c1 > 10 || c2 > 10)
				{
					throw new ResolverException(step.token, $"operator override({op.ToString()}) found.But type not match.");

				}
				else
				{
					return false;
				}
			}
			else
			{
				return false;
			}

		}



		private static StackLocater MakeContainerTraitReference(AS3Reg Reg, AS3ExprStep step, StackLocater instance, ASContainer container, ASTrait[] traits, CompileEnv compileEnv)
		{
			if (traits.Length == 1 && traits[0].Kind != TraitKind.Getter && traits[0].Kind != TraitKind.Setter)
			{
				var trait = traits[0];
				switch (trait.Kind)
				{
					case TraitKind.Slot:
					case TraitKind.Constant:
						{
							var reg = compileEnv.GetStackLocater(Reg, TypeKind.TraitDataReference, trait.TypeKind);


							var scopeMember =
								container._link_codescope.Members.First(o => o.Kind != ScopeMemberKind.Parameter && o.QName == trait.QName);

							int scopemember_index = container._link_codescope.Members.IndexOf(scopeMember);

							if (scopemember_index > ushort.MaxValue)
							{
								throw new ParseException("scope count > 65535");
							}

							if (compileEnv.CompileContext.computeConstExprState.Count > 0)
							{
								if (
									//!scopeMember.trait.IsStatic || 
									trait.Kind != TraitKind.Constant)
								{
									throw new ResolverException(step.token, "Parameter initializer unknown or is not a compile-time constant.");
								}
							}

							compileEnv.SetRegTraitRef(Reg, new ASTrait[] { trait }, step);

							INS_Ld_InstanceOrSocpeMemberRef ld_HeapRef = new INS_Ld_InstanceOrSocpeMemberRef(step.token);
							ld_HeapRef.dst = reg;
							ld_HeapRef.instance = instance;
							//ld_HeapRef.trait_index = (ushort)trait_index;
							ld_HeapRef.scopemember_index = (ushort)scopemember_index;

							compileEnv.instructions.Add(ld_HeapRef);

							compileEnv.SetReferenceBindInstance(Reg, instance);

							return reg;
						}
					case TraitKind.Method:

						{
							var instance_type = compileEnv.ReadStackType(instance);

							VTableItem tableItem = container._vtable.Items.First(i => i.Trait == trait);
							int index = container._vtable.Items.IndexOf(tableItem);
							if (index < 0)
								throw new InvalidOperationException();

							if (index > ushort.MaxValue)
							{
								throw new ParseException("vtable.items.count > 65535");
							}

							compileEnv.SetRegTraitRef(Reg, new ASTrait[] { trait }, step);

							var reg = compileEnv.GetStackLocater(Reg, TypeKind.Function);

							compileEnv.SetReferenceBindInstance(Reg, instance);

							if (container is ASInstance && ((ASInstance)container).IsInterface)
							{
								ASClass astype = compileEnv.CompileContext.dict_typelayout[container.QName].ASType;

								int class_id = compileEnv.AddConstClassId(astype);

								INS_Ld_Method_Interface ld_Method_Interface = new INS_Ld_Method_Interface(step.token);
								ld_Method_Interface.dst = reg;
								ld_Method_Interface.instance = instance;
								ld_Method_Interface.class_id = class_id;
								ld_Method_Interface.const_index = (ushort)index;


								compileEnv.instructions.Add(ld_Method_Interface);


								//throw new NotImplementedException();
							}
							else
							{

								if (instance_type.Maj != TypeKind.Super)
								{
									INS_Ld_Method ld_Method = new INS_Ld_Method(step.token);
									ld_Method.dst = reg;
									ld_Method.instance = instance;
									ld_Method.const_index = (ushort)index;

									compileEnv.instructions.Add(ld_Method);
								}
								else
								{
									var supercls = FindClassById(compileEnv, (ulong)instance_type.Mir);
									if (supercls.Instance != container)
									{
										throw new InvalidOperationException();
									}

									int mid = compileEnv.AddSuperMethod(supercls, index);

									INS_Ld_SuperMethod ld_SuperMethod = new INS_Ld_SuperMethod(step.token);
									ld_SuperMethod.dst = reg;
									ld_SuperMethod.instance = instance;
									ld_SuperMethod.const_index = mid;

									compileEnv.instructions.Add(ld_SuperMethod);

								}
							}
							return reg;
						}

					case TraitKind.Getter:
					case TraitKind.Setter:
					case TraitKind.Function:
						throw new InvalidOperationException();
						break;
					case TraitKind.Class:
					default:
						throw new InvalidOperationException();
				}


			}
			else
			{
				ASTrait getter = traits.FirstOrDefault(t => t.Kind == TraitKind.Getter);
				ASTrait setter = traits.FirstOrDefault(t => t.Kind == TraitKind.Setter);

				TypeKind typeKind;
				if (getter != null)
				{
					typeKind = getter.Method.ReturnTypeKind;
				}
				else
				{
					typeKind = setter.Method.Parameters[0].TypeKind;
				}


				var reg = compileEnv.GetStackLocater(Reg, TypeKind.TraitDataReference, typeKind);


				compileEnv.SetRegTraitRef(Reg, new ASTrait[] { getter, setter }, step);

				//INS_Ld_InstanceOrSocpeMemberRef ld_HeapRef = new INS_Ld_InstanceOrSocpeMemberRef(step.token);
				//ld_HeapRef.reg = reg;
				//ld_HeapRef.instance = instance;
				////ld_HeapRef.trait_index = (ushort)trait_index;
				//ld_HeapRef.scopemember_index = (ushort)scopemember_index;

				//compileEnv.instructions.Add(ld_HeapRef);

				compileEnv.SetReferenceBindInstance(Reg, instance);

				return reg;



			}

		}

		/// <summary>
		/// 检查是否是一个命名空间查找
		/// 如果这个结果后面被 :: 操作符操作，则是，否则不是
		/// </summary>
		/// <param name="reg"></param>
		/// <param name="compileEnv"></param>
		/// <returns></returns>
		private static bool IsRTQNameL(AS3Reg reg, AS3ExprStep step, AS3Expression expression, CompileEnv compileEnv)
		{
			for (int i = 0; i < expression.exprStepList.Count; i++)
			{
				var s = expression.exprStepList[i];
				if (s.Type == OpType.NameSpaceAccess)
				{
					if (s.Arg2.IsReg && s.Arg2.Reg == reg)
					{
						return true;
					}
				}

			}

			return false;

		}

		private void BuildNSAccess(AS3ExprStep step, AS3Expression expression, CompileEnv compileEnv)
		{
			if (step.OpCode == "::")
			{
				//判断是否是这种情况 (uuu.public::M)::k ;
				bool isNsPart = IsRTQNameL(step.Arg1.Reg, step, expression, compileEnv);
				TypeKind regKind = isNsPart ? TypeKind.RTQNameRTQNameL_N : TypeKind.RTQName_MultiName_DataReference;

				if (step.Arg2.IsReg)
				{
					StackLocater ns_part = compileEnv.GetStackLocater(step.Arg2.Reg);

					CompileTypeKind compileTypeKind = compileEnv.ReadStackType(ns_part);

				lbl_retry:

					if (compileTypeKind.Maj == TypeKind.RTQNameRTQNameL_N)
					{
						//运行时组装QName

						Tuple<ASContainer, ASNamespace> ctx = null;
						if (isNsPart)
						{
							compileEnv.ReadNsAccessContext(step.Arg2.Reg);
						}

						StackLocater namepart;

						if (step.Arg3.IsReg) //RTQNameL
						{
							namepart = compileEnv.GetStackLocater(step.Arg3.Reg);
						}
						else if (step.Arg3.Data.FF1Type == FF1DataValueType.as3_array)
						{
							var arr = (List<AS3DataStackElement>)step.Arg3.Data.Value;
							if (arr.Count == 0)
							{
								throw new SyntaxException(step.token, "']' is not allowed here");
							}
							for (int i = 0; i < arr.Count - 1; i++)
							{
								LoadRightValue(arr[i], compileEnv, step.token);
							}

							namepart = LoadRightValue(arr[arr.Count - 1], compileEnv, step.token);
						}
						else
						{                   //RTQName
							string name = step.Arg3.Data.Value.ToString();

							int constindex = compileEnv.AddConstString(name);

							namepart = compileEnv.MakeStackLocater(TypeKind.String);

							INS_Ld_Const ld_Const = new INS_Ld_Const(step.token);
							ld_Const.dst = namepart;
							ld_Const.const_index = constindex;

							compileEnv.instructions.Add(ld_Const);


						}

						//生成运行时访问RTQName,RTQNameL的代码

						//var instance = ctx.Item1;

						var reg = compileEnv.GetStackLocater(step.Arg1.Reg, regKind);

						if (!accessInstance.HasValue)
						{
							accessInstance = new StackLocater()
							{
								index = -1 - compileEnv.Scope.index
							};

							//throw new InvalidOperationException();
						}

						//ns_part = LoadRightValue(step.Arg2, compileEnv, step.token);

						var load_ns = compileEnv.MakeStackLocater(TypeKind.Any);
						INS_Ld_ValueRef ld_ValueRef = new INS_Ld_ValueRef(step.token);
						ld_ValueRef.dst = load_ns;
						ld_ValueRef.source = ns_part;
						compileEnv.instructions.Add(ld_ValueRef);


						if (compileEnv.CompileContext.computeConstExprState.Count > 0) { throw new ResolverException(step.token, "Parameter initializer unknown or is not a compile-time constant."); }
						//从当前scope链访问RTQName,RTQNameL
						INS_Ld_RTQNameL_Ref ld_RTQNameL_Ref = new INS_Ld_RTQNameL_Ref(step.token);
						ld_RTQNameL_Ref.dst = reg;
						ld_RTQNameL_Ref.ns = load_ns;
						ld_RTQNameL_Ref.name = namepart;
						ld_RTQNameL_Ref.instance = accessInstance.Value; //new StackLocater() { index = -1 - compileEnv.Scope.index };

						compileEnv.instructions.Add(ld_RTQNameL_Ref);

						compileEnv.SetReferenceBindInstance(step.Arg1.Reg, accessInstance.Value);


						if (isNsPart)
						{
							compileEnv.SetReg_NsAccessContext(step.Arg1.Reg, ctx.Item1, null);
						}
						else
						{
							accessInstance = reg;
							compileEnv.SetRegTraitRef(step.Arg1.Reg, new ASTrait[] { }, step);
						}



						//throw new NotImplementedException();
					}
					else if (compileTypeKind.Maj == TypeKind.CParseNS_Traits)
					{
						var nsContext = compileEnv.ReadNsAccessContext(step.Arg2.Reg);
						if (nsContext.Item2 == null)
							throw new InvalidOperationException();

						if (step.Arg3.Data.FF1Type == FF1DataValueType.as3_array)
						{

							//RTQNameL ,名称在运行时解析

							ns_part = compileEnv.MakeStackLocater(TypeKind.Namespace);
							var ns_index = compileEnv.AddConstNamespaceId(nsContext.Item2);

							INS_Ld_Namespace ld_Namespace = new INS_Ld_Namespace(step.token);
							ld_Namespace.dst = ns_part;
							ld_Namespace.namespace_index = ns_index;
							compileEnv.instructions.Add(ld_Namespace);

							List<AS3DataStackElement> array = (List<AS3DataStackElement>)step.Arg3.Data.Value;
							if (array.Count == 0)
							{
								throw new SyntaxException(step.token, "']' is not allowed here");
							}
							for (int i = 0; i < array.Count - 1; i++)
							{
								LoadRightValue(array[i], compileEnv, step.token);
							}

							var namepart = LoadRightValue(array[array.Count - 1], compileEnv, step.token);

							//var instance = nsContext.Item1;

							var reg = compileEnv.GetStackLocater(step.Arg1.Reg, regKind);

							if (!accessInstance.HasValue)
								throw new InvalidOperationException();

							if (compileEnv.CompileContext.computeConstExprState.Count > 0) { throw new ResolverException(step.token, "Parameter initializer unknown or is not a compile-time constant."); }
							INS_Ld_RTQNameL_Ref ld_RTQNameL_Ref = new INS_Ld_RTQNameL_Ref(step.token);
							ld_RTQNameL_Ref.dst = reg;
							ld_RTQNameL_Ref.ns = ns_part;
							ld_RTQNameL_Ref.name = namepart;
							ld_RTQNameL_Ref.instance = accessInstance.Value; //new StackLocater() { index = -1 - compileEnv.Scope.index };

							compileEnv.instructions.Add(ld_RTQNameL_Ref);

							compileEnv.SetReferenceBindInstance(step.Arg1.Reg, accessInstance.Value);

							if (isNsPart)
							{
								compileEnv.SetReg_NsAccessContext(step.Arg1.Reg, nsContext.Item1, null);
							}
							else
							{
								compileEnv.SetRegTraitRef(step.Arg1.Reg, new ASTrait[] { }, step);
								accessInstance = reg;
							}



						}
						else if (step.Arg3.Data.FF1Type == FF1DataValueType.identifier)
						{
							string name = step.Arg3.Data.Value.ToString();

							bool issametype = false;
							var envS = compileEnv.Scope;
							while (envS != null)
							{
								if (envS.Container == nsContext.Item1)
								{
									issametype = true;
									break;
								}
								envS = envS.Parent;
							}

							if (nsContext.Item1 != null && nsContext.Item1._link_codescope.Kind == CodeScopeKind.Class)
							{
								envS = compileEnv.Scope;
								while (envS != null)
								{
									if (envS.Kind == CodeScopeKind.Instance)
									{
										if (((ASInstance)envS.Container) == (((ASClass)nsContext.Item1._link_codescope.Container).Instance))
										//if (((ASInstance)envS.Container).IsExtend( ((ASClass)nsContext.Item1._link_codescope.Container).Instance  ))
										{
											issametype = true;
										}

										break;
									}
									envS = envS.Parent;
								}
							}

							bool isthis =
								compileEnv.instructions.Any(i => i.INS_Code == INS_Code.ld_This && ((INS_Ld_this)i).dst.index == accessInstance.Value.index)
								||
								issametype

								;

							if (nsContext.Item1 != null)
							{
								var nsmatch = (ASNamespace t) =>
								{
									return (
												(
													nsContext.Item2.Kind == NamespaceKind.Protected && t.Kind == NamespaceKind.Protected
													&&
													isthis
												)
												||
												(
													nsContext.Item2.Kind == NamespaceKind.Protected && t.Kind == NamespaceKind.StaticProtected
													&&
													isthis
												)
												||
												(
													nsContext.Item2.Kind == NamespaceKind.Package && t.Kind == NamespaceKind.Package
												)
												||
												(
													nsContext.Item2.Kind == NamespaceKind.PackageInternal && nsContext.Item2.def_uri == null
													&&
													t.Kind == NamespaceKind.PackageInternal && t.def_uri == null
												)
												||
												(
													nsContext.Item2.Kind == NamespaceKind.Private && t.Kind == NamespaceKind.Private
												)
											);
								};

								var find = nsContext.Item1._link_codescope.Members.Where(
									t =>
										t.QName.Name == name
										//&&
										//compileEnv.Scope.NamespaceSet.Namespaces.Contains( t.QName.Namespace) 
										&&
										(
											(t.QName.Namespace == nsContext.Item2)
											||
											nsmatch(t.QName.Namespace)
										)
									).Select(t => t.trait).ToList()
									;


								var findvtable = nsContext.Item1._vtable.Items.Reverse<VTableItem>().FirstOrDefault(
									(t) =>
										t.Trait.QName.Name == name && t.Trait.Kind == TraitKind.Method
									&&
									(
										t.Trait.QName.Namespace == nsContext.Item2
										||
										nsmatch(t.Trait.QName.Namespace)
									)

									);
								if (findvtable != null)
								{
									find.Add(findvtable.Trait);
								}

								var findgetter = nsContext.Item1._vtable.Items.Reverse<VTableItem>().FirstOrDefault(
									(t) =>
										t.Trait.QName.Name == name && t.Trait.Kind == TraitKind.Getter
									   &&
									(
										t.Trait.QName.Namespace == nsContext.Item2
										||
										nsmatch(t.Trait.QName.Namespace)
									)

									);

								if (findgetter != null)
								{
									find.Add(findgetter.Trait);
								}

								var findsetter = nsContext.Item1._vtable.Items.Reverse<VTableItem>().FirstOrDefault(
									(t) =>
										t.Trait.QName.Name == name && t.Trait.Kind == TraitKind.Setter
										&&
									(
										t.Trait.QName.Namespace == nsContext.Item2
										||
										nsmatch(t.Trait.QName.Namespace)
									)

									);
								if (findsetter != null)
								{
									find.Add(findsetter.Trait);
								}



								if (find.Count == 0)
								{
									if (nsContext.Item1 is ASClass || nsContext.Item1 is ASInstance)
									{
										int _type = juicescript.runtime.Extensions.IsInaccessibleOrUndefinedOrInScript(nsContext.Item1, name, out CodeScope s);
										if (_type == 1)
										{
											throw new ResolverException(step.token, $"Attempted access of inaccessible property {name} through a reference with static type {nsContext.Item1.QName.Name}.");
										}
										else
										{
											throw new ResolverException(step.token, $"Access of possibly undefined property {name} through a reference with static type {nsContext.Item1.QName.Name}.");
										}

									}
									else
									{
										throw new ResolverException(step.token, $"Access of possibly undefined property {name}.");
									}
								}

								if (find.Count == 2 && find[0].Kind == TraitKind.Getter && find[1].Kind == TraitKind.Setter)
								{
									// getter , setter
								}
								else if (find.Count > 1)
								{
									throw new InvalidOperationException();
								}


								if (!accessInstance.HasValue)
									throw new InvalidOperationException();

								accessInstance = MakeContainerTraitReference(step.Arg1.Reg, step, accessInstance.Value, nsContext.Item1, find.ToArray(), compileEnv);


							}
							else
							{
								//从当前scope链访问RTQName,RTQNameL
								//var instance = nsContext.Item1;
								if (compileEnv.CompileContext.computeConstExprState.Count > 0) { throw new ResolverException(step.token, "Parameter initializer unknown or is not a compile-time constant."); }
								if (!accessInstance.HasValue)
									throw new InvalidOperationException();

								ns_part = compileEnv.MakeStackLocater(TypeKind.Namespace);
								var ns_index = compileEnv.AddConstNamespaceId(nsContext.Item2);

								INS_Ld_Namespace ld_Namespace = new INS_Ld_Namespace(step.token);
								ld_Namespace.dst = ns_part;
								ld_Namespace.namespace_index = ns_index;
								compileEnv.instructions.Add(ld_Namespace);

								var reg = compileEnv.GetStackLocater(step.Arg1.Reg, regKind);


								int constindex = compileEnv.AddConstString(name);
								var namepart = compileEnv.MakeStackLocater(TypeKind.String);


								INS_Ld_Const ld_Const = new INS_Ld_Const(step.token);
								ld_Const.dst = namepart;
								ld_Const.const_index = constindex;

								compileEnv.instructions.Add(ld_Const);

								//从当前scope链访问RTQName,RTQNameL
								INS_Ld_RTQNameL_Ref ld_RTQNameL_Ref = new INS_Ld_RTQNameL_Ref(step.token);
								ld_RTQNameL_Ref.dst = reg;
								ld_RTQNameL_Ref.ns = ns_part;
								ld_RTQNameL_Ref.name = namepart;
								ld_RTQNameL_Ref.instance = accessInstance.Value; //instance; //new StackLocater() { index = -1 - compileEnv.Scope.index };

								compileEnv.instructions.Add(ld_RTQNameL_Ref);

								compileEnv.SetReferenceBindInstance(step.Arg1.Reg, accessInstance.Value);

								if (isNsPart)
								{
									compileEnv.SetReg_NsAccessContext(step.Arg1.Reg, null, null);
								}
								else
								{
									compileEnv.SetRegTraitRef(step.Arg1.Reg, new ASTrait[] { }, step);
									accessInstance = reg;
								}



							}
						}
						else
						{
							throw new ResolverException(step.token, $"'{step.Arg3.Data.Value}' is not allowed here");
						}

					}
					else if (compileEnv.IsCallResult(ns_part))
					{
						if (!isNsPart)
						{
							compileTypeKind.Maj = TypeKind.RTQNameRTQNameL_N;
							goto lbl_retry;
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
				else
				{
					if (step.Arg2.Data.FF1Type == FF1DataValueType.identifier)
					{


						ScopeHeapLocater scopeHeapLocater;
						TypeKind heaptype;

						ASTrait trait;
						ASNamespace importNS;
						ASClass loadclass;
						ScopeMember nsMember;
						FindNSPart(step.Arg2.Data.Value.ToString(), compileEnv, step.token, out trait, out importNS, out nsMember, out scopeHeapLocater, out heaptype, out loadclass);

						if (trait != null)
						{
							if (compileEnv.CompileContext.computeConstExprState.Count > 0)
							{
								if (trait.Kind != TraitKind.Constant || !trait.IsStatic) //编译期求值不能访问除常量外的成员
								{
									throw new ResolverException(step.token, "Parameter initializer unknown or is not a compile-time constant.");
								}
							}

							StackLocater ns_locater;

							if (loadclass == null)
							{
								ns_locater = compileEnv.MakeStackLocater(heaptype);

								INS_Ld_ScopeHeap ld_ScopeHeap = new INS_Ld_ScopeHeap(step.token);
								ld_ScopeHeap.dst = ns_locater;
								ld_ScopeHeap.heap = scopeHeapLocater;


								compileEnv.instructions.Add(ld_ScopeHeap);
							}
							else
							{
								var stackLoc = compileEnv.MakeStackLocater(TypeKind.Class, (TypeKind)loadclass.Type_identifier);
								INS_Ld_Class ld_Class = new INS_Ld_Class(step.token);
								ld_Class.dst = stackLoc;
								ld_Class.classid_index = compileEnv.AddConstClassId(loadclass);

								compileEnv.instructions.Add(ld_Class);


								ns_locater = compileEnv.MakeStackLocater(TypeKind.RTQNameRTQNameL_N, heaptype);
								int scopemember_index = loadclass._link_codescope.Members.IndexOf(nsMember);

								if (scopemember_index > ushort.MaxValue)
								{
									throw new ParseException("scope count > 65535");
								}

								INS_Ld_InstanceOrSocpeMemberRef ld_HeapRef = new INS_Ld_InstanceOrSocpeMemberRef(step.token);
								ld_HeapRef.dst = ns_locater;
								ld_HeapRef.instance = stackLoc;
								//ld_HeapRef.trait_index = (ushort)trait_index;
								ld_HeapRef.scopemember_index = (ushort)scopemember_index;
								compileEnv.instructions.Add(ld_HeapRef);



							}


							if (step.Arg3.Data.FF1Type == FF1DataValueType.as3_array)
							{
								if (compileEnv.CompileContext.computeConstExprState.Count > 0) { throw new ResolverException(step.token, "Parameter initializer unknown or is not a compile-time constant."); }
								//名称在运行时解析RTQNameL
								var arr = (List<AS3DataStackElement>)step.Arg3.Data.Value;
								if (arr.Count == 0)
								{
									throw new SyntaxException(step.token, "']' is not allowed here");
								}
								for (int i = 0; i < arr.Count - 1; i++)
								{
									LoadRightValue(arr[i], compileEnv, step.token);
								}

								var namepart = LoadRightValue(arr[arr.Count - 1], compileEnv, step.token);

								var reg = compileEnv.GetStackLocater(step.Arg1.Reg, regKind);

								//从当前scope链访问RTQName,RTQNameL
								INS_Ld_RTQNameL_Ref ld_RTQNameL_Ref = new INS_Ld_RTQNameL_Ref(step.token);
								ld_RTQNameL_Ref.dst = reg;
								ld_RTQNameL_Ref.ns = ns_locater;
								ld_RTQNameL_Ref.name = namepart;
								ld_RTQNameL_Ref.instance = new StackLocater() { index = -1 - compileEnv.Scope.index };

								compileEnv.instructions.Add(ld_RTQNameL_Ref);

								compileEnv.SetReferenceBindInstance(step.Arg1.Reg, ld_RTQNameL_Ref.instance);

								if (isNsPart)
								{
									compileEnv.SetReg_NsAccessContext(step.Arg1.Reg, null, null);
									accessInstance = ld_RTQNameL_Ref.instance;
								}
								else
								{
									compileEnv.SetRegTraitRef(step.Arg1.Reg, new ASTrait[] { }, step);
									accessInstance = reg;
								}


							}
							else
							{
								if (compileEnv.CompileContext.computeConstExprState.Count > 0) { throw new ResolverException(step.token, "Parameter initializer unknown or is not a compile-time constant."); }
								string name = step.Arg3.Data.Value.ToString();
								//RTQName
								int constindex = compileEnv.AddConstString(name);

								StackLocater name_locater = compileEnv.MakeStackLocater(TypeKind.String);

								INS_Ld_Const ld_Const = new INS_Ld_Const(step.token);
								ld_Const.dst = name_locater;
								ld_Const.const_index = constindex;

								compileEnv.instructions.Add(ld_Const);


								var reg = compileEnv.GetStackLocater(step.Arg1.Reg, regKind);


								//从当前scope链访问RTQName,RTQNameL
								INS_Ld_RTQNameL_Ref ld_RTQNameL_Ref = new INS_Ld_RTQNameL_Ref(step.token);
								ld_RTQNameL_Ref.dst = reg;
								ld_RTQNameL_Ref.ns = ns_locater;
								ld_RTQNameL_Ref.name = name_locater;
								ld_RTQNameL_Ref.instance = new StackLocater() { index = -1 - compileEnv.Scope.index };

								compileEnv.instructions.Add(ld_RTQNameL_Ref);

								compileEnv.SetReferenceBindInstance(step.Arg1.Reg, ld_RTQNameL_Ref.instance);

								if (isNsPart)
								{
									compileEnv.SetReg_NsAccessContext(step.Arg1.Reg, null, null);
									accessInstance = ld_RTQNameL_Ref.instance;
								}
								else
								{
									compileEnv.SetRegTraitRef(step.Arg1.Reg, new ASTrait[] { }, step);
									accessInstance = reg;
								}


							}

						}
						else if (importNS == null)
						{
							throw new InvalidOperationException();
						}
						else
						{
							if (step.Arg3.Data.FF1Type == FF1DataValueType.as3_array)
							{
								if (compileEnv.CompileContext.computeConstExprState.Count > 0) { throw new ResolverException(step.token, "Parameter initializer unknown or is not a compile-time constant."); }
								// 在当前scope上名称在运行时解析
								//**ld_namespace
								//**ld_name

								var ns_part = compileEnv.MakeStackLocater(TypeKind.Namespace);
								var ns_index = compileEnv.AddConstNamespaceId(importNS);

								INS_Ld_Namespace ld_Namespace = new INS_Ld_Namespace(step.token);
								ld_Namespace.dst = ns_part;
								ld_Namespace.namespace_index = ns_index;
								compileEnv.instructions.Add(ld_Namespace);

								List<AS3DataStackElement> array = (List<AS3DataStackElement>)step.Arg3.Data.Value;
								if (array.Count == 0)
								{
									throw new SyntaxException(step.token, "']' is not allowed here");
								}
								for (int i = 0; i < array.Count - 1; i++)
								{
									LoadRightValue(array[i], compileEnv, step.token);
								}

								var namepart = LoadRightValue(array[array.Count - 1], compileEnv, step.token);

								var reg = compileEnv.GetStackLocater(step.Arg1.Reg, TypeKind.RTQName_MultiName_DataReference);
								compileEnv.SetRegTraitRef(step.Arg1.Reg, new ASTrait[] { }, step);

								//从当前scope链访问RTQName,RTQNameL
								INS_Ld_RTQNameL_Ref ld_RTQNameL_Ref = new INS_Ld_RTQNameL_Ref(step.token);
								ld_RTQNameL_Ref.dst = reg;
								ld_RTQNameL_Ref.ns = ns_part;
								ld_RTQNameL_Ref.name = namepart;
								ld_RTQNameL_Ref.instance = new StackLocater() { index = -1 - compileEnv.Scope.index };

								compileEnv.instructions.Add(ld_RTQNameL_Ref);

								compileEnv.SetReferenceBindInstance(step.Arg1.Reg, ld_RTQNameL_Ref.instance);

								accessInstance = reg;


							}
							else
							{
								string name = step.Arg3.Data.Value.ToString();
								ASTrait out_trait;
								CodeScope out_scope;
								ScopeMember out_member;
								VTableItem[] out_vtableItems;
								var _Rv = FindIdentifier(name, importNS, compileEnv, step.token, out scopeHeapLocater, out heaptype, out out_trait, out out_scope, out out_member, out out_vtableItems);
								if (_Rv == FindIdResultType.NotFound)
								{
									var test = FindIdentifier(name, null, compileEnv, step.token, out scopeHeapLocater, out heaptype, out out_trait, out out_scope, out out_member, out out_vtableItems);

									if (test == FindIdResultType.NotFound)
									{
										var topscope = compileEnv.Scope;
										while (topscope.Kind == CodeScopeKind.Method)
										{
											topscope = topscope.Parent;
										}

										if (topscope.Kind == CodeScopeKind.Class || topscope.Kind == CodeScopeKind.Instance)
										{
											throw new ResolverException(step.token, $"Access of possibly undefined property {name} through a reference with static type {topscope.Container.QName.Name}.");
										}
										else
										{
											throw new ResolverException(step.token, $"Access of possibly undefined property {name}.");
										}
									}
									else
									{
										if (out_scope.Kind == CodeScopeKind.Class || out_scope.Kind == CodeScopeKind.Instance)
										{
											throw new ResolverException(step.token, $"Attempted access of inaccessible property {name} through a reference with static type {out_scope.Container.QName.Name}.");
										}
										else
										{
											throw new ResolverException(step.token, $"Access of possibly undefined property {name}.");
										}
									}

								}
								else if (_Rv == FindIdResultType.NeedLoadClass_ScopeMember)
								{
									if (compileEnv.CompileContext.computeConstExprState.Count > 0)
									{
										if (!out_member.trait.IsStatic || out_member.Kind != ScopeMemberKind.Constant)
										{
											throw new ResolverException(step.token, "Parameter initializer unknown or is not a compile-time constant.");
										}
									}

									ASClass @class = (ASClass)out_scope.Container;


									var stackLoc = compileEnv.MakeStackLocater(TypeKind.Class, (TypeKind)@class.Type_identifier);
									INS_Ld_Class ld_Class = new INS_Ld_Class(step.token);
									ld_Class.dst = stackLoc;
									ld_Class.classid_index = compileEnv.AddConstClassId(@class);

									compileEnv.instructions.Add(ld_Class);




									var reg = compileEnv.GetStackLocater(step.Arg1.Reg, TypeKind.TraitDataReference, out_member.TypeKind);
									int scopemember_index = out_scope.Members.IndexOf(out_member);

									if (scopemember_index > ushort.MaxValue)
									{
										throw new ParseException("scope count > 65535");
									}

									INS_Ld_InstanceOrSocpeMemberRef ld_HeapRef = new INS_Ld_InstanceOrSocpeMemberRef(step.token);
									ld_HeapRef.dst = reg;
									ld_HeapRef.instance = stackLoc;
									//ld_HeapRef.trait_index = (ushort)trait_index;
									ld_HeapRef.scopemember_index = (ushort)scopemember_index;

									compileEnv.instructions.Add(ld_HeapRef);

									compileEnv.SetRegTraitRef(step.Arg1.Reg, new ASTrait[] { out_trait }, step);

									compileEnv.SetReferenceBindInstance(step.Arg1.Reg, stackLoc);

									accessInstance = reg;


								}
								else if (_Rv == FindIdResultType.ScopeMember)
								{
									if (compileEnv.CompileContext.computeConstExprState.Count > 0)
									{
										if (out_member.Kind != ScopeMemberKind.Constant || !out_member.trait.IsStatic)
										{
											throw new ResolverException(step.token, "Parameter initializer unknown or is not a compile-time constant.");
										}
									}

									var reg = compileEnv.GetStackLocater(step.Arg1.Reg, TypeKind.TraitDataReference, out_member.TypeKind);
									int scopemember_index = out_scope.Members.IndexOf(out_member);



									if (scopemember_index > ushort.MaxValue)
									{
										throw new ParseException("scope count > 65535");
									}



									INS_Ld_InstanceOrSocpeMemberRef ld_HeapRef = new INS_Ld_InstanceOrSocpeMemberRef(step.token);
									ld_HeapRef.dst = reg;
									ld_HeapRef.instance = new StackLocater() { index = -1 - out_scope.index };
									//ld_HeapRef.trait_index = (ushort)trait_index;
									ld_HeapRef.scopemember_index = (ushort)scopemember_index;

									compileEnv.instructions.Add(ld_HeapRef);

									compileEnv.SetRegTraitRef(step.Arg1.Reg, new ASTrait[] { out_trait }, step);

									compileEnv.SetReferenceBindInstance(step.Arg1.Reg, ld_HeapRef.instance);

									accessInstance = reg;


								}
								else if (_Rv == FindIdResultType.Ambiguous)
								{
									throw new ResolverException(step.token,
										 $"Ambiguous reference to {name}");
								}
								else
								{
									throw new InvalidOperationException();
								}

							}

						}

					}
					else
					{
						throw new InvalidOperationException();
					}
				}

				//if (IsRTQNameL(step.Arg1.Reg,step,expression,compileEnv))
				//{
				//    throw new NotImplementedException();
				//}

				;
			}
			else
			{
				throw new InvalidOperationException();
			}
		}


		private void FindNSPart(string name, CompileEnv compileEnv, Token token, out ASTrait findtrait, out ASNamespace findns, out ScopeMember nsMember, out ScopeHeapLocater heapLocater, out TypeKind heapType, out ASClass @class)
		{
			ASTrait o;
			CodeScope codeScope;
			ScopeMember scopeMember;
			VTableItem[] out_vtableItems;
			var find = FindIdentifier(name, null, compileEnv, token, out heapLocater, out heapType, out o, out codeScope, out scopeMember, out out_vtableItems);

			if (

				find == FindIdResultType.ScopeMember &&

				!(
				scopeMember.trait != null &&
				scopeMember.trait.ValueKind == ConstantKind.Namespace &&
				scopeMember.trait.Kind == TraitKind.Constant
				)
				)
			{
				//RTQName 或 RTQNameL 命名空间在运行时确定
				findtrait = scopeMember.trait;
				nsMember = scopeMember;
				findns = null;
				@class = null;
			}
			else if (find == FindIdResultType.NeedLoadClass_ScopeMember)
			{
				@class = (ASClass)codeScope.Container;

				if (codeScope != scopeMember.DefineAt._link_codescope)
					throw new InvalidOperationException();

				findtrait = scopeMember.trait;
				findns = null;
				nsMember = scopeMember;



			}
			else if (find == FindIdResultType.Ambiguous)
			{
				throw new ResolverException(token,
					   $"Ambiguous reference to {name}");
			}
			else
			{

				ASNamespace importNs = null;
				if (name != "public" && name != "internal" && name != "private" && name != "protected")
				{
					if (!compileEnv.imports.Any((t) => t.Kind == TraitKind.Constant && t.ValueKind == ConstantKind.Namespace && t.QName.Name == name))
					{
						throw new ResolverException(token, $"Access of possibly undefined property {name}.");
					}

					importNs = compileEnv.imports
						.First((t) => t.Kind == TraitKind.Constant && t.ValueKind == ConstantKind.Namespace && t.QName.Name == name).Value.Namespace;

				}
				else
				{
					if (name == "public")
					{
						importNs = new ASNamespace() { Kind = NamespaceKind.Package, Name = "" };
					}
					else if (name == "internal")
					{
						var s = compileEnv.Scope;
						while (s.Kind == CodeScopeKind.Method)
						{
							s = s.Parent;
						}

						if (s.Kind == CodeScopeKind.Script)
						{
							importNs = new ASNamespace() { Kind = NamespaceKind.PackageInternal, Name = "FilePrivateNS:" + s.Container.Traits[0].QName.Name };
						}
						else
						{
							importNs = new ASNamespace() { Kind = NamespaceKind.PackageInternal, Name = s.Container.QName.Namespace.Name };
						}
					}
					else if (name == "private")
					{
						var s = compileEnv.Scope;
						while (s.Kind == CodeScopeKind.Method)
						{
							s = s.Parent;
						}

						if (s.Kind == CodeScopeKind.Script)
						{
							throw new ResolverException(token, "private can only be used as a namespace inside a class.");
						}

						importNs = new ASNamespace()
						{
							Kind = NamespaceKind.Private,
							Name = string.IsNullOrEmpty(s.Container.QName.Namespace.Name) ?
								s.Container.QName.Name :
								s.Container.QName.Namespace.Name + ":" + s.Container.QName.Name
						};

					}
					else if (name == "protected")
					{
						var s = compileEnv.Scope;
						while (s.Kind == CodeScopeKind.Method)
						{
							s = s.Parent;
						}

						if (s.Kind == CodeScopeKind.Script)
						{
							throw new ResolverException(token, "protected can only be used as a namespace inside a class.");
						}

						importNs = new ASNamespace() { Kind = NamespaceKind.Protected, Name = "" };
					}

				}

				findtrait = null;
				findns = importNs;
				@class = null;
				nsMember = null;
			}
		}


		private void BuildContainerAccess(AS3ExprStep step, AS3Expression expression, CompileEnv compileEnv,
			ASContainer container, StackLocater instance
			)
		{
			bool isRTQName = IsRTQNameL(step.Arg1.Reg, step, expression, compileEnv);

			if (isRTQName)
			{
				//编译时查找命名空间
				string name = step.Arg3.Data.Value.ToString();

				ASTrait findtrait;
				ASNamespace importNs;

				ScopeHeapLocater heapLocater;
				TypeKind heapType;
				ScopeMember nsMember;
				ASClass loadclass;
				FindNSPart(name, compileEnv, step.token, out findtrait, out importNs, out nsMember, out heapLocater, out heapType, out loadclass);

				if (findtrait != null)
				{
					if (compileEnv.CompileContext.computeConstExprState.Count > 0)
					{
						if ((findtrait.Kind) != TraitKind.Constant || !findtrait.IsStatic) //编译期求值不能访问除常量外的成员
						{
							throw new ResolverException(findtrait.Token, "Parameter initializer unknown or is not a compile-time constant.");
						}
					}

					if (loadclass != null)
					{
						if (container != null)
						{
							//等同于失败
							if (container is ASClass || container is ASInstance)
							{
								int _type = juicescript.runtime.Extensions.IsInaccessibleOrUndefinedOrInScript(container, name, out CodeScope s);
								if (_type == 1)
								{
									throw new ResolverException(step.token, $"Attempted access of inaccessible property {name} through a reference with static type {container.QName.Name}.");
								}
								else
								{
									throw new ResolverException(step.token, $"Access of possibly undefined property {name} through a reference with static type {container.QName.Name}.");
								}
							}
							else
							{
								throw new ResolverException(step.token, $"Access of possibly undefined property {name}.");
							}
						}
						else
						{
							var stackLoc = compileEnv.MakeStackLocater(TypeKind.Class, (TypeKind)loadclass.Type_identifier);
							INS_Ld_Class ld_Class = new INS_Ld_Class(step.token);
							ld_Class.dst = stackLoc;
							ld_Class.classid_index = compileEnv.AddConstClassId(loadclass);

							compileEnv.instructions.Add(ld_Class);


							var ns_locater = compileEnv.GetStackLocater(step.Arg1.Reg, TypeKind.RTQNameRTQNameL_N, heapType);
							int scopemember_index = loadclass._link_codescope.Members.IndexOf(nsMember);

							if (scopemember_index > ushort.MaxValue)
							{
								throw new ParseException("scope count > 65535");
							}

							INS_Ld_InstanceOrSocpeMemberRef ld_HeapRef = new INS_Ld_InstanceOrSocpeMemberRef(step.token);
							ld_HeapRef.dst = ns_locater;
							ld_HeapRef.instance = stackLoc;
							//ld_HeapRef.trait_index = (ushort)trait_index;
							ld_HeapRef.scopemember_index = (ushort)scopemember_index;
							compileEnv.instructions.Add(ld_HeapRef);


							compileEnv.SetReg_NsAccessContext(step.Arg1.Reg, container, null);

							compileEnv.SetReferenceBindInstance(step.Arg1.Reg, stackLoc);

							accessInstance = instance;
						}
					}
					else
					{
						var stackLoc = compileEnv.GetStackLocater(step.Arg1.Reg, TypeKind.RTQNameRTQNameL_N, heapType); //compileEnv.MakeStackLocater(heapType);



						INS_Ld_ScopeHeap ld_ScopeHeap = new INS_Ld_ScopeHeap(step.token);
						ld_ScopeHeap.dst = stackLoc;
						ld_ScopeHeap.heap = heapLocater;

						compileEnv.instructions.Add(ld_ScopeHeap);

						compileEnv.SetReg_NsAccessContext(step.Arg1.Reg, container, null);
						accessInstance = instance;
					}
				}
				else if (importNs == null)
				{
					throw new InvalidOperationException();
				}
				else
				{
					compileEnv.GetStackLocater(step.Arg1.Reg, TypeKind.CParseNS_Traits);
					compileEnv.SetReg_NsAccessContext(step.Arg1.Reg, container, importNs);
					accessInstance = instance;
				}
			}
			else if (step.Arg3.Data.FF1Type == FF1DataValueType.as3_array)
			{
				throw new SyntaxException(step.token, "'[' is not allowed here.");
			}
			else
			{
				//成员查找
				string name = step.Arg3.Data.Value.ToString();

				var ctype = compileEnv.ReadStackType(instance);

				if (container != null
					&&
					!((ctype.Maj == TypeKind.Array && ctype.Mir == TypeKind.Array && name == "callee")) //不是访问callee
					)
				{
					bool issametype = false;
					var envS = compileEnv.Scope;
					while (envS != null)
					{
						if (envS.Container == container)
						{
							issametype = true;
							break;
						}
						envS = envS.Parent;
					}

					bool isthis = compileEnv.instructions.Any(i => i.INS_Code == INS_Code.ld_This && ((INS_Ld_this)i).dst.index == accessInstance.Value.index)
						||
						issametype
						;

					var find = container._link_codescope.Members.Where(
						(t) =>

						t.QName.Name == name
						&& compileEnv.Scope.NamespaceSet.Namespaces.Contains(t.QName.Namespace)


						&& ((t.QName.Namespace.Kind != NamespaceKind.Protected && t.QName.Namespace.Kind != NamespaceKind.Protected) || isthis)

						).Select(t => t.trait).ToList();



					var findvtable = container._vtable.Items.Reverse<VTableItem>().Where(
						(t) =>
							t.Trait.QName.Name == name && t.Trait.Kind == TraitKind.Method
						&& compileEnv.Scope.NamespaceSet.Namespaces.Contains(t.Trait.QName.Namespace)

						).GroupBy(t => t.DefineAt).ToArray();

					if (findvtable.Length > 0)
					{
						find.AddRange(findvtable[0].Select(v => v.Trait));
					}

					//if (findvtable != null)
					//{
					//    find.Add(findvtable.Trait);
					//}

					var findgetter = container._vtable.Items.Reverse<VTableItem>().Where(
						(t) =>
							t.Trait.QName.Name == name && t.Trait.Kind == TraitKind.Getter
						&& compileEnv.Scope.NamespaceSet.Namespaces.Contains(t.Trait.QName.Namespace)

						).GroupBy(t => t.DefineAt).ToArray();

					if (findgetter.Length > 0)
					{
						find.AddRange(findgetter[0].Select(v => v.Trait));
					}

					var findsetter = container._vtable.Items.Reverse<VTableItem>().Where(
						(t) =>
							t.Trait.QName.Name == name && t.Trait.Kind == TraitKind.Setter
						&& compileEnv.Scope.NamespaceSet.Namespaces.Contains(t.Trait.QName.Namespace)

						).GroupBy(t => t.DefineAt).ToArray();
					if (findsetter.Length > 0)
					{
						find.AddRange(findsetter[0].Select(v => v.Trait));
					}



					if (find.Count == 0)
					{
						if (container is ASInstance && !((ASInstance)container).Flags.HasFlag(ClassFlags.Sealed))
						{
							//dynamic对象，编译动态代码
							int constindex = compileEnv.AddConstString(name);


							var reg = compileEnv.GetStackLocater(step.Arg1.Reg, TypeKind.RTQName_MultiName_DataReference);
							compileEnv.SetRegTraitRef(step.Arg1.Reg, new ASTrait[] { }, step);

							INS_Ld_MultiName_Ref ld_MultiName_Ref = new INS_Ld_MultiName_Ref(step.token);
							ld_MultiName_Ref.dst = reg;
							ld_MultiName_Ref.instance = instance;
							ld_MultiName_Ref.name_index = constindex;

							compileEnv.instructions.Add(ld_MultiName_Ref);
							accessInstance = reg;

							compileEnv.SetReferenceBindInstance(step.Arg1.Reg, instance);
							return;
						}
						else if (container is ASClass || container is ASInstance)
						{
							string typename = container.QName.Name;
							if (container is ASClass)
							{
								if (((ASClass)container).Instance.Flags.HasFlag(ClassFlags.Vector))
								{ 
									typename = compileEnv.CompileContext.vectorDefs.Find(v => v.buildVector.vector_class == container).ToString();
								}
							}

							int _type = juicescript.runtime.Extensions.IsInaccessibleOrUndefinedOrInScript(container, name, out CodeScope s);
							if (_type == 1)
							{
								throw new ResolverException(step.token, $"Attempted access of inaccessible property {name} through a reference with static type {typename}.");
							}
							else
							{
								throw new ResolverException(step.token, $"Access of possibly undefined property {name} through a reference with static type {typename}.");
							}

						}
						else
						{
							throw new ResolverException(step.token, $"Access of possibly undefined property {name}.");
						}


					}
					if (find.Count == 2 && find[0].Kind == TraitKind.Getter && find[1].Kind == TraitKind.Setter)
					{
						// getter , setter

					}
					else if (find.Count > 1)
					{
						throw new ResolverException(step.token,
							$"Ambiguous reference to {name}");
					}
					accessInstance = MakeContainerTraitReference(step.Arg1.Reg, step, instance, container, find.ToArray(), compileEnv);
				}
				else
				{
					int constindex = compileEnv.AddConstString(name);


					var reg = compileEnv.GetStackLocater(step.Arg1.Reg, TypeKind.RTQName_MultiName_DataReference);
					compileEnv.SetRegTraitRef(step.Arg1.Reg, new ASTrait[] { }, step);


					INS_Ld_MultiName_Ref ld_MultiName_Ref = new INS_Ld_MultiName_Ref(step.token);
					ld_MultiName_Ref.dst = reg;
					ld_MultiName_Ref.instance = instance;
					ld_MultiName_Ref.name_index = constindex;

					compileEnv.instructions.Add(ld_MultiName_Ref);
					accessInstance = reg;

					compileEnv.SetReferenceBindInstance(step.Arg1.Reg, instance);



				}
			}
		}

		private StackLocater? accessInstance = null;

		private void BuildAccess(AS3ExprStep step, AS3Expression expression, CompileEnv compileEnv)
		{
			if (step.OpCode == ".")
			{
				if (IsRTQNameL(step.Arg1.Reg, step, expression, compileEnv))
				{
					if (!step.Arg2.IsReg && step.Arg2.Data.FF1Type == FF1DataValueType.super_pointer)
					{
						var s = expression.exprStepList.First((e) => e.Type == OpType.NameSpaceAccess && e.Arg2.IsReg && e.Arg2.Reg == step.Arg1.Reg);

						if (s.Arg3.IsReg || s.Arg3.Data.FF1Type != FF1DataValueType.identifier)
						{
							throw new ResolverException(
							   step.token,
								"Syntax error: 'super' is not allowed here");
						}
					}
				}

				if (step.Arg3.Data.FF1Type == FF1DataValueType.identifier)
				{
					if (step.Arg3.Data.Value.ToString() == "null")
					{
						throw new ResolverException(
						   step.token,
							"Syntax error: 'null' is not allowed here");
					}
				}

				if (step.Arg3.Data.FF1Type == FF1DataValueType.as3_array)
				{
					throw new ResolverException(
						  step.token,
						   "Syntax error: '[' is not allowed here");
				}

				var v1 = LoadRightValue(step.Arg2, compileEnv, step.token);
				accessInstance = v1;

				var ctype = compileEnv.ReadStackType(v1);

				//静态成员查找
				if (ctype.Maj == TypeKind.Class)
				{
					ASClass @class = FindClassById(compileEnv, (ulong)ctype.Mir);

					BuildContainerAccess(step, expression, compileEnv, @class, v1);
				}
				else if (ctype.Maj == TypeKind.Vector)
				{
					//ASClass @class = FindClassById(compileEnv, (ulong)ctype.Maj);
					VectorDef vdef = compileEnv.CompileContext.vectorDefs.Find(v => v.Identifier == ctype.Mir);

					BuildContainerAccess(step, expression, compileEnv, vdef.buildVector.vector_class , v1);
				}
				else if (ctype.Maj == TypeKind.Super)
				{
					ASClass @class = FindClassById(compileEnv, (ulong)ctype.Mir);

					BuildContainerAccess(step, expression, compileEnv, @class.Instance, v1);

				}
				else if (ctype.Maj == TypeKind.Any)
				{
					BuildContainerAccess(step, expression, compileEnv, null, v1);
				}
				else if (ctype.Maj == TypeKind.RTQName_MultiName_DataReference)
				{
					BuildContainerAccess(step, expression, compileEnv, null, v1);
				}
				else if (ctype.Maj == TypeKind.TraitDataReference
					)
				{
					var traitref = compileEnv.ReadTraitRef(step.Arg2.Reg);
					if (traitref.Item1.Length == 1)
					{
						if (ctype.Mir >= TypeKind.Object)
						{
							ASClass @class = FindClassById(compileEnv, (ulong)ctype.Mir);

							BuildContainerAccess(step, expression, compileEnv, @class.Instance, v1);
						}
						else
						{
							BuildContainerAccess(step, expression, compileEnv, null, v1);
						}
					}
					else
					{
						if (traitref.Item1[0] == null)
						{
							throw new InvalidOperationException();
						}

						if (traitref.Item1[0].Method.ReturnTypeKind >= TypeKind.Object)
						{
							ASClass @class = FindClassById(compileEnv, (ulong)traitref.Item1[0].Method.ReturnTypeKind);

							BuildContainerAccess(step, expression, compileEnv, @class.Instance, v1);
						}
						else
						{
							BuildContainerAccess(step, expression, compileEnv, null, v1);
						}

						//throw new NotImplementedException();
					}
				}
				else if (ctype.Maj == TypeKind.SearchNameSpaceFromImports)
				{
					string ns = compileEnv.ReadSearchNSImport(v1);// + "." + step.Arg3.Data.Value.ToString();
					string ns_ns = ns + "." + step.Arg3.Data.Value.ToString();

					var find = compileEnv.imports.Where((t) =>
						t.QName.Namespace.Name == ns &&
						t.QName.Name == step.Arg3.Data.Value.ToString()

						).ToList();

					if (find.Count == 1) //找到
					{
						if (find[0].Kind == TraitKind.Class)
						{
							StackLocater locater = compileEnv.GetStackLocater(step.Arg1.Reg, TypeKind.Class, (TypeKind)find[0].Class.Type_identifier);

							INS_Ld_Class ld_Class = new INS_Ld_Class(step.token);
							ld_Class.dst = locater;
							ld_Class.classid_index = compileEnv.AddConstClassId(find[0].Class);

							compileEnv.instructions.Add(ld_Class);

							accessInstance = locater;

						}
						else if (find[0].Kind == TraitKind.Constant && find[0].ValueKind == ConstantKind.Namespace)
						{
							var stackLoc = compileEnv.GetStackLocater(step.Arg1.Reg, TypeKind.Namespace);
							compileEnv.SetRegNameSpace(step.Arg1.Reg, find[0].Value.Namespace);

							var ns_index = compileEnv.AddConstNamespaceId(find[0].Value.Namespace);

							INS_Ld_Namespace ld_Namespace = new INS_Ld_Namespace(step.token);
							ld_Namespace.dst = stackLoc;
							ld_Namespace.namespace_index = ns_index;

							compileEnv.instructions.Add(ld_Namespace);

							accessInstance = stackLoc;
						}
						else
						{
							throw new InvalidOperationException();
						}
					}
					else
					{
						find = compileEnv.imports.Where((t) =>
						t.QName.Namespace.Name == ns_ns ||
						t.QName.Namespace.Name.StartsWith(ns_ns + ".")
						).ToList();

						if (find.Count > 0)
						{
							StackLocater locater = compileEnv.GetStackLocater(step.Arg1.Reg, TypeKind.SearchNameSpaceFromImports);
							compileEnv.SetReg_SearchNSImport(locater, ns_ns);
						}
						else
						{
							throw new ResolverException(step.token, $"Access of possibly undefined property {ns}.");
						}
					}



				}
				
				else if (ctype.Maj >= TypeKind.Object 
					|| ctype.Maj == TypeKind.SByte
					|| ctype.Maj == TypeKind.Number
					|| ctype.Maj == TypeKind.Boolean
					|| ctype.Maj == TypeKind.Int
					|| ctype.Maj == TypeKind.Uint
					|| ctype.Maj == TypeKind.Float
					|| ctype.Maj == TypeKind.Byte
					|| ctype.Maj == TypeKind.Short
					|| ctype.Maj == TypeKind.UShort
					
					)
				{
					ASClass @class = FindClassById(compileEnv, (ulong)ctype.Maj);

					BuildContainerAccess(step, expression, compileEnv, @class.Instance, v1);
				}
				else
				{
					throw new InvalidOperationException();
				}


			}
			else if (step.OpCode == "[")
			{
				bool isNspart = IsRTQNameL(step.Arg1.Reg, step, expression, compileEnv);
				TypeKind kind = isNspart ? TypeKind.RTQNameRTQNameL_N : TypeKind.RTQName_MultiName_DataReference;

				var v1 = LoadRightValue(step.Arg2, compileEnv, step.token);
				var ctype = compileEnv.ReadStackType(v1);

				if (ctype.Maj == TypeKind.TraitDataReference)
				{
					throw new InvalidOperationException();
					var traitref = compileEnv.ReadTraitRef(step.Arg2.Reg);
					if (traitref.Item1.Length == 1)
					{
					}
					else
					{
						throw new NotImplementedException("处理Getter Setter");
					}
				}

				if (ctype.Maj == TypeKind.SearchNameSpaceFromImports)
				{
					throw new InvalidOperationException("不可能出现这种情况");
				}

				List<AS3DataStackElement> list = (List<AS3DataStackElement>)step.Arg3.Data.Value;
				for (int i = 0; i < list.Count - 1; i++)
				{
					LoadRightValue(list[i], compileEnv, step.token);
				}

				var prop_name = LoadRightValue(list[list.Count - 1], compileEnv, step.token);
				var prop_type = compileEnv.ReadStackType(prop_name);

				ASClass itype = null;
				if (ctype.Maj == TypeKind.Vector)
				{
					itype = FindClassById(compileEnv, (ulong)ctype.Mir);
				}
				else if (ctype.Maj > TypeKind.Null && ctype.Maj != TypeKind.Super)
				{
					itype = FindClassById(compileEnv, (ulong)ctype.Maj);
				}

				//if (prop_type.Maj.IsUnsigned())
				//{
				//	throw new NotImplementedException("数组下标？");
				//}
				//else
				{


					//MulitNameL 运行时解析名称
					var reg = compileEnv.GetStackLocater(step.Arg1.Reg, kind);

					INS_Ld_MultiNameL_Ref ld_MultiNameL_Ref = new INS_Ld_MultiNameL_Ref(step.token);
					ld_MultiNameL_Ref.dst = reg;
					ld_MultiNameL_Ref.instance = v1;
					ld_MultiNameL_Ref.name = prop_name;

					if (ctype.Maj == TypeKind.Super)
					{
						var super_type = FindClassById(compileEnv, (ulong)ctype.Mir);
						int super_type_id = compileEnv.AddConstClassId(super_type) + 1;

						ld_MultiNameL_Ref.super_type_index = super_type_id;
					}
					else
					{
						ld_MultiNameL_Ref.super_type_index = 0;
					}

					compileEnv.instructions.Add(ld_MultiNameL_Ref);

					if (isNspart)
					{
						compileEnv.SetReg_NsAccessContext(step.Arg1.Reg, null, null);
					}
					else
					{
						if (itype != null && itype.Instance.Flags.HasFlag(ClassFlags.Indexer) )
						{
							var keytype = itype.Instance.indexer_get.Parameters[0].TypeKind;

							if (TypeUtils.IsIndexTypePass(prop_type.Maj== TypeKind.Vector? prop_type.Mir : prop_type.Maj , keytype, compileEnv.CompileContext))
							{
								compileEnv.SetRegTraitRef(step.Arg1.Reg, new ASTrait[] { itype.Instance.indexer_get.Trait, itype.Instance.indexer_set.Trait, itype.Instance.indexer_delete.Trait }, step);
							}
							else
							{
								compileEnv.SetRegTraitRef(step.Arg1.Reg, new ASTrait[] { }, step);
							}	
						}
						else
						{
							compileEnv.SetRegTraitRef(step.Arg1.Reg, new ASTrait[] { }, step);
						}
						accessInstance = reg;
					}

					compileEnv.SetReferenceBindInstance(step.Arg1.Reg, v1);

				}

			}
			else
			{
				throw new InvalidOperationException();
			}



		}


		public StackLocater Build(AST.AS3Expression expression, CompileEnv compileEnv, ref int flag_seed)
		{

			try
			{
				compileEnv.stack_loaded_heapunit.Push(new Dictionary<object, StackLocater>());


				if (expression.exprStepList.Count == 0)
				{
					#region 编译单纯一个字符的情况
					if (expression.Value.Data.FF1Type == FF1DataValueType.super_pointer)
					{
						throw new ResolverException(expression.Token, $"Unable to generate code for 'super'");
					}
					else if (expression.Value.Data.FF1Type == FF1DataValueType.const_number
						||
						expression.Value.Data.FF1Type == FF1DataValueType.const_string
						||
						expression.Value.Data.FF1Type == FF1DataValueType.const_regexp
						)
					{
						return LoadRightValue(expression.Value, compileEnv, expression.Token);
					}
					else if (expression.Value.Data.FF1Type == FF1DataValueType.as3_array)
					{
						return LoadRightValue(expression.Value, compileEnv, expression.Token);
					}
					else if (expression.Value.Data.FF1Type == FF1DataValueType.as3_vector)
					{
						return LoadRightValue(expression.Value, compileEnv, expression.Token);
					}
					else if (expression.Value.Data.FF1Type == FF1DataValueType.as3_function)
					{
						return LoadRightValue(expression.Value, compileEnv, expression.Token);
					}
					else if (expression.Value.Data.FF1Type == FF1DataValueType.this_pointer)
					{
						return LoadRightValue(expression.Value, compileEnv, expression.Token);
					}
					else if (expression.Value.Data.FF1Type == FF1DataValueType.dynamicobj)
					{
						return LoadRightValue(expression.Value, compileEnv, expression.Token);
					}
					else if (expression.Value.Data.FF1Type == FF1DataValueType.identifier)
					{
						return LoadRightValue(expression.Value, compileEnv, expression.Token);
						//               string identifier = expression.Value.Data.Value.ToString();
						//               var token = expression.Token;
						//if (Equals(identifier, "Infinity"))
						//               {
						//                   int constindex = compileEnv.AddConstNumber(double.PositiveInfinity);

						//                   StackLocater stackLocater = compileEnv.MakeStackLocater(TypeKind.Number);

						//                   INS_Ld_Const ld_Const = new INS_Ld_Const(token);
						//                   ld_Const.stack = stackLocater;
						//                   ld_Const.const_index = constindex;

						//                   compileEnv.instructions.Add(ld_Const);

						//                   return stackLocater;
						//               }
						//               else if (Equals(identifier, "NaN"))
						//               {
						//                   int constindex = compileEnv.AddConstNumber(double.NaN);

						//                   StackLocater stackLocater = compileEnv.MakeStackLocater(TypeKind.Number);

						//                   INS_Ld_Const ld_Const = new INS_Ld_Const(token);
						//                   ld_Const.stack = stackLocater;
						//                   ld_Const.const_index = constindex;

						//                   compileEnv.instructions.Add(ld_Const);

						//                   return stackLocater;
						//               }
						//               else if (Equals(identifier, "false"))
						//               {
						//                   StackLocater stackLocater = compileEnv.MakeStackLocater(TypeKind.Boolean);

						//                   INS_Ld_False ld_False = new INS_Ld_False(token);
						//                   ld_False.stack = stackLocater;

						//                   compileEnv.instructions.Add(ld_False);

						//                   return stackLocater;
						//               }
						//               else if (Equals(identifier, "true"))
						//               {
						//                   StackLocater stackLocater = compileEnv.MakeStackLocater(TypeKind.Boolean);

						//                   INS_Ld_True ld_True = new INS_Ld_True(token);
						//                   ld_True.stack = stackLocater;

						//                   compileEnv.instructions.Add(ld_True);

						//                   return stackLocater;
						//               }
						//               else if (Equals(identifier, "null"))
						//               {
						//                   StackLocater stackLocater = compileEnv.MakeStackLocater(TypeKind.Null);

						//                   INS_Ld_Null ld_Null = new INS_Ld_Null(token);
						//                   ld_Null.stack = stackLocater;

						//                   compileEnv.instructions.Add(ld_Null);

						//                   return stackLocater;
						//               }
						//               else if (Equals(identifier, "undefined"))
						//               {
						//                   StackLocater stackLocater = compileEnv.MakeStackLocater(TypeKind.Null);

						//                   INS_Ld_Undefined ld_undefined = new INS_Ld_Undefined(token);
						//                   ld_undefined.stack = stackLocater;

						//                   compileEnv.instructions.Add(ld_undefined);

						//                   return stackLocater;
						//               }
						//               else if (Equals(identifier, "arguments") && compileEnv.Scope.Kind == CodeScopeKind.Method)
						//               {
						//                   var method = ((ASMethodBody)compileEnv.Scope.Container).Method;
						//                   if (method.Parameters.Count > 0 && method.Parameters[method.Parameters.Count - 1].IsRest)
						//                   {
						//                       throw new ResolverException(token, "'arguments' can not be used in a function with '...' paramaters.");
						//                   }

						//                   method.Flags |= MethodFlags.NeedArguments;

						//                   StackLocater stackLocater = compileEnv.MakeStackLocater(TypeKind.Array, TypeKind.Array);

						//                   INS_Ld_Arguments ld_Arguments = new INS_Ld_Arguments(token);
						//                   ld_Arguments.target = stackLocater;

						//                   compileEnv.instructions.Add(ld_Arguments);

						//                   return stackLocater;
						//               }
						//               else
						//               {
						//                   ScopeHeapLocater heapLocater; TypeKind heapType; ASTrait out_trait; CodeScope codescope; ScopeMember scopeMember; VTableItem[] out_vtableItems;
						//                   var _Rv = FindIdentifier(expression.Value.Data.Value.ToString(), null, compileEnv, expression.Token,
						//                       out heapLocater, out heapType, out out_trait, out codescope, out scopeMember, out out_vtableItems);

						//                   if (_Rv == FindIdResultType.NotFound)
						//                   {
						//                       //查找导入的类
						//                       var match = compileEnv.imports.Where((t) => { return t.QName.Name == expression.Value.Data.Value.ToString(); }).ToArray();
						//                       if (match.Length > 1)
						//                       {
						//                           throw new ResolverException(expression.Token, $"Ambiguous reference to {match[0].QName.Name}");
						//                       }
						//                       else if (match.Length == 1)
						//                       {
						//                           //加载类型元数据
						//                           if (match[0].Kind == TraitKind.Class)
						//                           {
						//                               var stackLoc = compileEnv.MakeStackLocater(TypeKind.Class, (TypeKind)match[0].Class.Type_identifier);

						//                               INS_Ld_Class ld_Class = new INS_Ld_Class(expression.Token);
						//                               ld_Class.stack = stackLoc;
						//                               ld_Class.classid_index = compileEnv.AddConstClassId(match[0].Class);

						//                               compileEnv.instructions.Add(ld_Class);

						//                               return stackLoc;

						//                           }
						//                           else if (match[0].Kind == TraitKind.Constant
						//                               &&
						//                               match[0].ValueKind == ConstantKind.Namespace
						//                               )
						//                           {
						//                               var stackLoc = compileEnv.MakeStackLocater(TypeKind.Namespace);

						//                               var ns_index = compileEnv.AddConstNamespaceId(match[0].Value.Namespace);

						//                               INS_Ld_Namespace ld_Namespace = new INS_Ld_Namespace(expression.Token);
						//                               ld_Namespace.stack = stackLoc;
						//                               ld_Namespace.namespace_index = ns_index;

						//                               compileEnv.instructions.Add(ld_Namespace);

						//                               return stackLoc;
						//                           }
						//                           else
						//                           {
						//                               throw new InvalidOperationException();
						//                           }
						//                       }
						//                       else
						//                       {
						//                           throw new ResolverException(expression.Token, $"Access of possibly undefined property {expression.Value.Data.Value}.");
						//                       }
						//                   }
						//                   else if (
						//                       _Rv == FindIdResultType.ScopeMember
						//                       )
						//                   {
						//                       if ((scopeMember).Kind == ScopeMemberKind.Constant && (scopeMember).ValueKind == ConstantKind.Namespace)
						//                       {
						//                           //命名空间
						//                           ASNamespace ns = scopeMember.trait.Value.Namespace;

						//                           var stackLoc = compileEnv.MakeStackLocater(TypeKind.Namespace);
						//                           var ns_index = compileEnv.AddConstNamespaceId(ns);

						//                           INS_Ld_Namespace ld_Namespace = new INS_Ld_Namespace(expression.Token);
						//                           ld_Namespace.stack = stackLoc;
						//                           ld_Namespace.namespace_index = ns_index;

						//                           compileEnv.instructions.Add(ld_Namespace);

						//                           return stackLoc;
						//                       }
						//                       else if (scopeMember.Kind == ScopeMemberKind.Parameter || scopeMember.Kind == ScopeMemberKind.Slot || scopeMember.Kind == ScopeMemberKind.Constant)
						//                       {
						//                           var stackLoc = compileEnv.MakeStackLocater(heapType);

						//                           INS_Ld_ScopeHeap ld_ScopeHeap = new INS_Ld_ScopeHeap(expression.Token);
						//                           ld_ScopeHeap.stack = stackLoc;
						//                           ld_ScopeHeap.heap = heapLocater;

						//                           compileEnv.instructions.Add(ld_ScopeHeap);

						//                           return stackLoc;
						//                       }
						//                       else
						//                       {
						//                           throw new InvalidOperationException();
						//                       }
						//                   }
						//                   else if (_Rv == FindIdResultType.NeedLoadClass_ScopeMember)
						//                   {
						//                       ASClass @class = (ASClass)codescope.Container;


						//                       var stackLoc = compileEnv.MakeStackLocater(TypeKind.Class, (TypeKind)@class.Type_identifier);
						//                       INS_Ld_Class ld_Class = new INS_Ld_Class(expression.Token);
						//                       ld_Class.stack = stackLoc;
						//                       ld_Class.classid_index = compileEnv.AddConstClassId(@class);

						//                       compileEnv.instructions.Add(ld_Class);

						//                       return stackLoc;
						//                   }
						//                   else if (_Rv == FindIdResultType.Ambiguous)
						//                   {
						//                       throw new ResolverException(expression.Token,
						//                           $"Ambiguous reference to {expression.Value.Data.Value.ToString()}");
						//                   }
						//                   else if (_Rv == FindIdResultType.NeedLoadClass_VTableItems || _Rv == FindIdResultType.VTableItems)
						//                   {
						//                       if (out_vtableItems[0] == null)
						//                       {
						//                           throw new ResolverException(
						//                               expression.Token,
						//                               $"Illegal read of write-only property {out_vtableItems[1].Trait.QName.ToDebugTypeName()} on {codescope.Container.QName.Name}."
						//                               );
						//                       }

						//                       //throw new NotImplementedException();
						//                       // do load from property

						//                       return LoadRightValue(expression.Value, compileEnv, expression.Token);


						//                   }
						//                   else
						//                   {
						//                       throw new InvalidOperationException();
						//                   }
						//               }
					}
					else
					{
						throw new InvalidOperationException();
					}
					#endregion
				}

				else
				{
					StackLocater expressionValue = new StackLocater() { index = int.MinValue };

					Dictionary<string, INS_Flag> expression_flags = new Dictionary<string, INS_Flag>();

					for (int i = 0; i < expression.exprStepList.Count; i++)
					{
						var step = expression.exprStepList[i];

						if (step.Type != OpType.Access && step.Type != OpType.CallFunc)
						{
							if (step.Arg2 != null && !step.Arg2.IsReg)
							{
								if (step.Arg2.Data.FF1Type == FF1DataValueType.super_pointer)
								{
									throw new ResolverException(expression.Token,
										$"'super' is not allowed here");
								}
							}
						}

						switch (step.Type)
						{
							case OpType.Assigning:
								expressionValue = BuildAssigning(step, compileEnv);
								break;
							case OpType.Load:

								if (step.OpCode == "Ld")
								{
									expressionValue = LoadRightValue(step.Arg2, compileEnv, step.token);
								}
								else if (step.OpCode == "Ld_R")
								{
									expressionValue = LoadRightValue(step.Arg2, compileEnv, step.token, step.Arg1.Reg);
								}
								else if (step.OpCode == "Ld_Callable")
								{
									ASTrait o;
									CodeScope codeScope;
									ScopeMember scopeMember;
									VTableItem[] out_vtableItems;
									TypeKind heapType;
									ScopeHeapLocater heapLocater;
									var find = FindIdentifier(step.Arg2.Data.Value.ToString(), null, compileEnv, step.token, out heapLocater, out heapType, out o, out codeScope, out scopeMember, out out_vtableItems);
									//如果不是可以改变的变量，就跳过Ld步骤
									if (find == FindIdResultType.ScopeMember)
									{
										if (
											o != null
											&& (
											(o.TypeKind == TypeKind.Any && o.Value != null && o.Value.ValueType == ASTrait.TraitValueType.AS3Function)
											||
											o.Kind == TraitKind.Constant
											)
											)
										{
											step.Arg1.IsReg = false;
											step.Arg1.Reg = null;
											step.Arg1.Data = step.Arg2.Data;
											break;
										}
									}
									else if (find == FindIdResultType.NeedLoadClass_VTableItems ||
										find == FindIdResultType.VTableItems
										)
									{
										step.Arg1.IsReg = false;
										step.Arg1.Reg = null;
										step.Arg1.Data = step.Arg2.Data;
										break;
									}
									else if (find == FindIdResultType.NeedLoadClass_ScopeMember)
									{
										if (o.Kind == TraitKind.Constant)
										{
											step.Arg1.IsReg = false;
											step.Arg1.Reg = null;
											step.Arg1.Data = step.Arg2.Data;
											break;
										}
									}

									expressionValue = LoadRightValue(step.Arg2, compileEnv, step.token, step.Arg1.Reg);
								}
								else
								{
									throw new InvalidOperationException();
								}
								break;
							case OpType.BitOr:
								BuildBitOr(step, expression, compileEnv);
								expressionValue = compileEnv.GetStackLocater(step.Arg1.Reg);
								break;
							case OpType.BitXor:
								BuildBitXor(step, expression, compileEnv);
								expressionValue = compileEnv.GetStackLocater(step.Arg1.Reg);
								break;
							case OpType.BitAnd:
								BuildBitAnd(step, expression, compileEnv);
								expressionValue = compileEnv.GetStackLocater(step.Arg1.Reg);
								break;
							case OpType.LogicEQ:
								BuildLogicEQ(step, expression, compileEnv);
								expressionValue = compileEnv.GetStackLocater(step.Arg1.Reg);
								break;
							case OpType.Logic:
								//throw new NotImplementedException();
								BuildLogic(step, expression, compileEnv);
								expressionValue = compileEnv.GetStackLocater(step.Arg1.Reg);
								break;
							case OpType.BitShift:
								BuildBitShift(step, expression, compileEnv);
								expressionValue = compileEnv.GetStackLocater(step.Arg1.Reg);
								break;
							case OpType.Plus:
								BuildPlus(step, compileEnv);
								expressionValue = compileEnv.GetStackLocater(step.Arg1.Reg);
								break;
							case OpType.Multiply:
								BuildMultiply(step, expression, compileEnv);
								expressionValue = compileEnv.GetStackLocater(step.Arg1.Reg);
								break;
							case OpType.Unary:
								BuildUnary(step, expression, compileEnv);
								expressionValue = compileEnv.GetStackLocater(step.Arg1.Reg);
								break;
							case OpType.Constructor:
								new ConstructorBuilder().Build(step, compileEnv);
								expressionValue = compileEnv.GetStackLocater(step.Arg1.Reg);

								break;
							case OpType.Access:
								BuildAccess(step, expression, compileEnv);
								expressionValue = compileEnv.GetStackLocater(step.Arg1.Reg);
								break;
							case OpType.E4XAccess:
								throw new NotImplementedException();
								break;
							case OpType.E4XFilter:
								throw new NotImplementedException();
								break;
							case OpType.NameSpaceAccess:
								BuildNSAccess(step, expression, compileEnv);
								expressionValue = compileEnv.GetStackLocater(step.Arg1.Reg);
								//throw new NotImplementedException();
								break;
							case OpType.CallFunc:
								new CallFuncBuilder().Build(step, expression, compileEnv);
								expressionValue = compileEnv.GetStackLocater(step.Arg1.Reg);
								break;
							case OpType.Suffix:

								var ovalue = LoadRightValue(step.Arg2, compileEnv, step.token);

								StackLocater newvalue = compileEnv.GetStackLocater(step.Arg3.Reg, TypeKind.Number);
								StackLocater result = compileEnv.GetStackLocater(step.Arg1.Reg, TypeKind.Number);



								INS_Incr_Decr Incr_Decr = new INS_Incr_Decr(step.token);
								Incr_Decr.source = ovalue;
								Incr_Decr.dst = newvalue;
								Incr_Decr.result = result;
								if (step.OpCode == "++")
								{
									Incr_Decr.addvalue = 1;
								}
								else if (step.OpCode == "--")
								{
									Incr_Decr.addvalue = -1;
								}
								else
								{
									throw new InvalidOperationException();
								}

								compileEnv.instructions.Add(Incr_Decr);


								AS3ExprStep stepAssign = new AS3ExprStep(step.token);
								stepAssign.Arg1 = step.Arg2;
								stepAssign.Arg2 = step.Arg3;
								stepAssign.Type = OpType.Assigning;
								stepAssign.OpCode = "=";

								BuildAssigning(stepAssign, compileEnv, "Operand of " + (step.OpCode == "++" ? "increment" : "decrement"));
								expressionValue = ovalue;


								break;

							case OpType.Flag:
								//throw new NotImplementedException();

								compileEnv.instructions.Add(expression_flags[step.OpCode]);

								break;
							case OpType.IF_True_Goto:
								//throw new NotImplementedException();
								{
									string flag_str = step.OpCode;
									INS_Flag flag;
									if (!expression_flags.ContainsKey(flag_str))
									{
										flag = new INS_Flag(step.token);
										flag.flag_id = flag_seed++;
										expression_flags.Add(flag_str, flag);
									}
									else
									{
										flag = expression_flags[flag_str];
									}

									INS_If_True_Goto if_True_Goto = new INS_If_True_Goto(step.token);
									if_True_Goto.flag_id = flag.flag_id;
									if_True_Goto.condition = LoadRightValue(step.Arg1, compileEnv, step.token);
									compileEnv.instructions.Add(if_True_Goto);

								}
								break;
							case OpType.IF_False_Goto:
								//throw new NotImplementedException();
								{
									string flag_str = step.OpCode;
									INS_Flag flag;
									if (!expression_flags.ContainsKey(flag_str))
									{
										flag = new INS_Flag(step.token);
										flag.flag_id = flag_seed++;
										expression_flags.Add(flag_str, flag);
									}
									else
									{
										flag = expression_flags[flag_str];
									}


									INS_If_False_Goto if_False_Goto = new INS_If_False_Goto(step.token);
									if_False_Goto.flag_id = flag.flag_id;
									if_False_Goto.condition = LoadRightValue(step.Arg1, compileEnv, step.token);
									compileEnv.instructions.Add(if_False_Goto);

								}
								break;
							case OpType.GotoFlag:

								{
									string flag_str = step.OpCode;
									INS_Flag flag = new INS_Flag(step.token);
									flag.flag_id = flag_seed++;
									expression_flags.Add(flag_str, flag);

									INS_Goto @goto = new INS_Goto(step.token);
									@goto.jumpTrys = 0;
									@goto.flag_id = flag.flag_id;
									compileEnv.instructions.Add(@goto);
								}

								//throw new NotImplementedException();
								break;
							default:
								throw new NotImplementedException();
						}

					}

					if (expressionValue.index == int.MinValue)
					{
						throw new InvalidOperationException();
					}

					if (expression.Value == null)
					{
						return expressionValue;
					}
					else if (expression.Value.IsReg)
					{
						return compileEnv.GetStackLocater(expression.Value.Reg);
					}
					else if (expression.Value.Data.FF1Type == FF1DataValueType.dynamicobj
						||
						expression.Value.Data.FF1Type == FF1DataValueType.as3_array
						||
						expression.Value.Data.FF1Type == FF1DataValueType.as3_vector
						)
					{
						return LoadRightValue(expression.Value, compileEnv, expression.Token);
					}
					else
					{
						return expressionValue;
					}
					//return expressionValue;

				}

			}
			finally
			{
				compileEnv.stack_loaded_heapunit.Pop();
			}

		}

		

		internal static ASClass FindClassById(CompileEnv compileEnv, ulong typeid)
		{
			ASClass @class =

						compileEnv.CompileContext.player_for_compiler.Context.libs.SelectMany(o => o.Classes)
						.Union
						(
							compileEnv.CompileContext.vectorDefs.Select(v=>v.buildVector.vector_class)
						)
						.Union
						(
							compileEnv.CompileContext.scriptDefs.SelectMany(o => o.scriptClasses)
						).Where(c => c != null)
						.First(i => i.Type_identifier == typeid);

			return @class;
		}




		internal static StackLocater LoadRightValue(AS3DataStackElement data, CompileEnv compileEnv, Token token, AS3Reg targetReg_onlyforLd = null , AS3ExprStep newop = null )
		{
			var makeOrGetLocater = (TypeKind maj) =>
			{
				if (targetReg_onlyforLd == null)
					return compileEnv.MakeStackLocater(maj);
				else
				{
					return compileEnv.GetStackLocater(targetReg_onlyforLd, maj);
				}
			};

			var makeOrGetLocater2 = (TypeKind maj, TypeKind mir) =>
			{
				if (targetReg_onlyforLd == null)
					return compileEnv.MakeStackLocater(maj, mir);
				else
					return compileEnv.GetStackLocater(targetReg_onlyforLd, maj, mir);
			};


			var parseIdentifier = (string identifier) =>
			{
				if (Equals(identifier, "Infinity"))
				{
					int constindex = compileEnv.AddConstNumber(double.PositiveInfinity);

					StackLocater stackLocater = makeOrGetLocater(TypeKind.Number);

					INS_Ld_Const ld_Const = new INS_Ld_Const(token);
					ld_Const.dst = stackLocater;
					ld_Const.const_index = constindex;

					compileEnv.instructions.Add(ld_Const);

					return stackLocater;
				}
				else if (Equals(identifier, "NaN"))
				{
					int constindex = compileEnv.AddConstNumber(double.NaN);

					StackLocater stackLocater = makeOrGetLocater(TypeKind.Number);

					INS_Ld_Const ld_Const = new INS_Ld_Const(token);
					ld_Const.dst = stackLocater;
					ld_Const.const_index = constindex;

					compileEnv.instructions.Add(ld_Const);

					return stackLocater;
				}
				else if (Equals(identifier, "false"))
				{
					StackLocater stackLocater = makeOrGetLocater(TypeKind.Boolean);

					INS_Ld_False ld_False = new INS_Ld_False(token);
					ld_False.dst = stackLocater;

					compileEnv.instructions.Add(ld_False);

					return stackLocater;
				}
				else if (Equals(identifier, "true"))
				{
					StackLocater stackLocater = makeOrGetLocater(TypeKind.Boolean);

					INS_Ld_True ld_True = new INS_Ld_True(token);
					ld_True.dst = stackLocater;

					compileEnv.instructions.Add(ld_True);

					return stackLocater;
				}
				else if (Equals(identifier, "null"))
				{
					StackLocater stackLocater = makeOrGetLocater(TypeKind.Null);

					INS_Ld_Null ld_Null = new INS_Ld_Null(token);
					ld_Null.dst = stackLocater;

					compileEnv.instructions.Add(ld_Null);

					return stackLocater;
				}
				else if (Equals(identifier, "undefined"))
				{
					StackLocater stackLocater = makeOrGetLocater(TypeKind.Any);

					INS_Ld_Undefined ld_undefined = new INS_Ld_Undefined(token);
					ld_undefined.dst = stackLocater;

					compileEnv.instructions.Add(ld_undefined);

					return stackLocater;
				}
				else if (Equals(identifier, "@array_hole"))
				{
					StackLocater stackLocater = makeOrGetLocater(TypeKind.Unknown);

					INS_Ld_ArrayHole ld_ArrayHole = new INS_Ld_ArrayHole(token);
					ld_ArrayHole.dst = stackLocater;

					compileEnv.instructions.Add(ld_ArrayHole);

					return stackLocater;
				}
				else if (Equals(identifier, "arguments") && compileEnv.Scope.Kind == CodeScopeKind.Method)
				{
					var method = ((ASMethodBody)compileEnv.Scope.Container).Method;
					if (method.Parameters.Count > 0 && method.Parameters[method.Parameters.Count - 1].IsRest)
					{
						throw new ResolverException(token, "'arguments' can not be used in a function with '...' paramaters.");
					}

					method.Flags |= MethodFlags.NeedArguments;

					StackLocater stackLocater = makeOrGetLocater2(TypeKind.Array, TypeKind.Array);

					INS_Ld_Arguments ld_Arguments = new INS_Ld_Arguments(token);
					ld_Arguments.dst = stackLocater;

					compileEnv.instructions.Add(ld_Arguments);

					return stackLocater;
				}
				else
				{
					ScopeHeapLocater heapLocater; TypeKind heapType; ASTrait out_trait; CodeScope codescope; ScopeMember scopeMember; VTableItem[] out_vtableItems;
					var _Rv = FindIdentifier(identifier, null, compileEnv, token,
						out heapLocater, out heapType, out out_trait, out codescope, out scopeMember, out out_vtableItems);

					if (_Rv == FindIdResultType.NotFound)
					{
						//查找导入的类
						var match = compileEnv.imports.Where((t) => { return t.QName.Name == identifier; }).ToArray();
						if (match.Length > 1)
						{
							throw new ResolverException(token, $"Ambiguous reference to {match[0].QName.Name}");
						}
						else if (match.Length == 1)
						{
							//加载类型元数据
							if (match[0].Kind == TraitKind.Class)
							{


								var stackLoc = makeOrGetLocater2(TypeKind.Class, (TypeKind)match[0].Class.Type_identifier);

								INS_Ld_Class ld_Class = new INS_Ld_Class(token);
								ld_Class.dst = stackLoc;
								ld_Class.classid_index = compileEnv.AddConstClassId(match[0].Class);

								compileEnv.instructions.Add(ld_Class);


								return stackLoc;
							}
							else if (match[0].Kind == TraitKind.Constant
								&&
								match[0].ValueKind == ConstantKind.Namespace
								)
							{
								var stackLoc = makeOrGetLocater(TypeKind.Namespace);

								var ns_index = compileEnv.AddConstNamespaceId(match[0].Value.Namespace);

								INS_Ld_Namespace ld_Namespace = new INS_Ld_Namespace(token);
								ld_Namespace.dst = stackLoc;
								ld_Namespace.namespace_index = ns_index;

								compileEnv.instructions.Add(ld_Namespace);


								return stackLoc;
							}
							else
							{
								throw new InvalidOperationException();
							}
						}
						else
						{
							//检查是否是 a.b.C 这种全命名空间。。。
							var ns_list = compileEnv.imports.Where(
								(n) => n.QName.Namespace.Name.StartsWith(identifier + ".") ||
								n.QName.Namespace.Name == identifier

								).ToList();

							if (ns_list.Count > 0)
							{
								var stackLoc = makeOrGetLocater(TypeKind.SearchNameSpaceFromImports);
								compileEnv.SetReg_SearchNSImport(stackLoc, identifier);
								return stackLoc;
							}
							else
							{
								throw new ResolverException(token, $"Access of possibly undefined property {identifier}.");
							}
						}
					}
					else if (
						_Rv == FindIdResultType.ScopeMember &&
							(
								(scopeMember).Kind == ScopeMemberKind.Constant ||
								(scopeMember).Kind == ScopeMemberKind.Slot ||

								(scopeMember).Kind == ScopeMemberKind.Parameter
							)
						)
					{
						if (compileEnv.CompileContext.computeConstExprState.Count > 0)
						{

							if ((scopeMember).Kind != ScopeMemberKind.Constant
								|| (!scopeMember.trait.IsStatic &&
								!(scopeMember.DefineAt._link_codescope.Kind == CodeScopeKind.Script || scopeMember.DefineAt._link_codescope.Kind == CodeScopeKind.Instance))
							) //编译期求值不能访问除常量外的成员
							{
								throw new ResolverException(token, "Parameter initializer unknown or is not a compile-time constant.");
							}
						}


						if ((scopeMember).Kind == ScopeMemberKind.Constant && (scopeMember).ValueKind == ConstantKind.Namespace)
						{
							//命名空间
							ASNamespace ns = scopeMember.trait.Value.Namespace;

							var stackLoc = makeOrGetLocater(TypeKind.Namespace);
							var ns_index = compileEnv.AddConstNamespaceId(ns);

							INS_Ld_Namespace ld_Namespace = new INS_Ld_Namespace(token);
							ld_Namespace.dst = stackLoc;
							ld_Namespace.namespace_index = ns_index;

							compileEnv.instructions.Add(ld_Namespace);

							return stackLoc;


							//throw new InvalidOperationException();
						}
						else if (scopeMember.Kind == ScopeMemberKind.Slot && scopeMember.trait.Value != null && scopeMember.trait.Value.ValueType == ASTrait.TraitValueType.AS3Function)
						{



							ASMethod method = compileEnv.CompileContext.dict_method_as3function[(AS3Function)scopeMember.trait.Value._value];

							int methodid = compileEnv.AddConstMethod(method);


							StackLocater stackLocater = makeOrGetLocater(TypeKind.Function);



							INS_Ld_Function ld_Function = new INS_Ld_Function(token);
							ld_Function.dst = stackLocater;
							ld_Function.const_index = methodid;
							ld_Function.heapLocater = heapLocater;

							compileEnv.instructions.Add(ld_Function);

							if (targetReg_onlyforLd != null)
							{
								compileEnv.SetRegTraitRef(targetReg_onlyforLd, new ASTrait[] { scopeMember.trait }, null);
							}
							return stackLocater;

						}
						else
						{
							var stackLoc = makeOrGetLocater(heapType);

							if (scopeMember.DefineAt._link_codescope.index == heapLocater.ScopeIndex
								&& scopeMember.DefineAt._link_codescope.Kind == CodeScopeKind.Method
								&& compileEnv.Scope.Kind == CodeScopeKind.Method && compileEnv.Scope.index == heapLocater.ScopeIndex
							)
							{
								INS_Ld_MethodVariable ld_MethodVariable = new INS_Ld_MethodVariable(token);
								ld_MethodVariable.dst = stackLoc;
								ld_MethodVariable.heap = heapLocater;

								compileEnv.instructions.Add(ld_MethodVariable);
							}
							else
							{
								INS_Ld_ScopeHeap ld_ScopeHeap = new INS_Ld_ScopeHeap(token);
								ld_ScopeHeap.dst = stackLoc;
								ld_ScopeHeap.heap = heapLocater;

								compileEnv.instructions.Add(ld_ScopeHeap);
							}

							if (targetReg_onlyforLd != null)
							{
								compileEnv.SetRegTraitRef(targetReg_onlyforLd, new ASTrait[] { scopeMember.trait }, null);
							}
							return stackLoc;
						}
					}
					else if (_Rv == FindIdResultType.NeedLoadClass_ScopeMember)
					{
						if (compileEnv.CompileContext.computeConstExprState.Count > 0)
						{
							if (scopeMember.Kind != ScopeMemberKind.Constant || !scopeMember.trait.IsStatic
							)
							{
								throw new ResolverException(token, "Parameter initializer unknown or is not a compile-time constant.");
							}
						}


						ASClass @class = (ASClass)codescope.Container;


						var stackLoc = compileEnv.MakeStackLocater(TypeKind.Class, (TypeKind)@class.Type_identifier);
						INS_Ld_Class ld_Class = new INS_Ld_Class(token);
						ld_Class.dst = stackLoc;
						ld_Class.classid_index = compileEnv.AddConstClassId(@class);

						compileEnv.instructions.Add(ld_Class);




						var reg = compileEnv.MakeStackLocater(TypeKind.TraitDataReference, scopeMember.TypeKind);
						int scopemember_index = codescope.Members.IndexOf(scopeMember);
						ASTrait t = codescope.Container.Traits.First((t) => t.QName == scopeMember.QName);
						int trait_index = codescope.Container.Traits.IndexOf(t);


						if (trait_index > ushort.MaxValue || scopemember_index > ushort.MaxValue)
						{
							throw new ParseException("scope count > 65535");
						}

						INS_Ld_InstanceOrSocpeMemberRef ld_HeapRef = new INS_Ld_InstanceOrSocpeMemberRef(token);
						ld_HeapRef.dst = reg;
						ld_HeapRef.instance = stackLoc;
						//ld_HeapRef.trait_index = (ushort)trait_index;
						ld_HeapRef.scopemember_index = (ushort)scopemember_index;

						compileEnv.instructions.Add(ld_HeapRef);


						var target = makeOrGetLocater(scopeMember.TypeKind);

						INS_Ld_ValueRef ld_ValueRef = new INS_Ld_ValueRef(token);
						ld_ValueRef.dst = target;
						ld_ValueRef.source = reg;

						compileEnv.instructions.Add(ld_ValueRef);

						if (targetReg_onlyforLd != null)
						{
							compileEnv.SetRegTraitRef(targetReg_onlyforLd, new ASTrait[] { scopeMember.trait }, null);
						}

						return target;
					}
					else if (_Rv == FindIdResultType.Ambiguous)
					{
						throw new ResolverException(token, $"Ambiguous reference to {identifier}");
					}
					else if (_Rv == FindIdResultType.VTableItems)
					{
						var scope = compileEnv.Scope;
						while (scope.Kind == CodeScopeKind.Method)
						{
							scope = scope.Parent;
						}

						StackLocater method_instance = new StackLocater() { index = -1 - scope.index };

						if (out_vtableItems.Length == 1)
						{
							if (out_vtableItems[0].Trait.QName.Namespace.Kind == NamespaceKind.Namespace)
							{
								throw new InvalidOperationException("接口方法不会出现在这里");
							}

							var reg = makeOrGetLocater(TypeKind.Function);

							INS_Ld_Method ld_Method = new INS_Ld_Method(token);
							ld_Method.dst = reg;
							ld_Method.instance = method_instance;
							ld_Method.const_index = (ushort)codescope.Container._vtable.Items.IndexOf(out_vtableItems[0]);

							unchecked
							{
								if (ld_Method.const_index == (ushort)(-1))
								{
									throw new InvalidOperationException();
								}
							}

							if (targetReg_onlyforLd != null)
							{
								compileEnv.SetRegTraitRef(targetReg_onlyforLd, new ASTrait[] { out_vtableItems[0].Trait }, null);
							}

							compileEnv.instructions.Add(ld_Method);
							return reg;
						}
						else
						{
							if (out_vtableItems[0] == null) //没有Getter
							{
								throw new ResolverException(
								   token,
								   "Property " + out_vtableItems[1].Trait.QName.Name + " is write-only.");
							}

							if (out_vtableItems[0].Trait.QName.Namespace.Kind == NamespaceKind.Namespace)
							{
								throw new InvalidOperationException("接口方法不会出现在这里");
							}

							int t_index = codescope.Container._vtable.Items.IndexOf(out_vtableItems[0]);
							if (t_index < 0)
								throw new InvalidOperationException();
							if (t_index > 65535)
							{
								throw new ParseException(codescope.Container.QName.ToDebugTypeName() + ".vtable.items.count > 65535");
							}

							var reg = makeOrGetLocater(out_vtableItems[0].Trait.Method.ReturnTypeKind);

							INS_readPoperty readPoperty = new INS_readPoperty(token);
							readPoperty.dst = reg;
							readPoperty.instance = method_instance;
							readPoperty.const_index = (ushort)t_index;

							compileEnv.SetCallResult(reg);
							compileEnv.instructions.Add(readPoperty);

							return reg;
						}
					}
					else if (_Rv == FindIdResultType.NeedLoadClass_VTableItems)
					{
						if (out_vtableItems.Length == 1)
						{
							ASClass @class = (ASClass)out_vtableItems[0].DefineAt;

							if (@class.Instance.IsInterface)
							{
								throw new InvalidOperationException("不可能出现接口");
							}

							var stackLoc = compileEnv.MakeStackLocater(TypeKind.Class, (TypeKind)@class.Type_identifier);
							INS_Ld_Class ld_Class = new INS_Ld_Class(token);
							ld_Class.dst = stackLoc;
							ld_Class.classid_index = compileEnv.AddConstClassId(@class);

							compileEnv.instructions.Add(ld_Class);

							var reg = makeOrGetLocater(TypeKind.Function);

							INS_Ld_Method ld_Method = new INS_Ld_Method(token);
							ld_Method.dst = reg;
							ld_Method.instance = stackLoc;
							ld_Method.const_index = (ushort)@class._vtable.Items.IndexOf(out_vtableItems[0]);

							compileEnv.instructions.Add(ld_Method);

							if (targetReg_onlyforLd != null)
							{
								compileEnv.SetRegTraitRef(targetReg_onlyforLd, new ASTrait[] { out_vtableItems[0].Trait }, null);
							}
							return reg;
						}
						else
						{
							if (out_vtableItems[0] == null) //没有Getter
							{
								throw new ResolverException(
								   token,
								   "Illegal read of write-only property " + out_vtableItems[1].Trait.QName.ToDebugTypeName() + " on class " + codescope.Container.QName.Name + ".");
							}

							ASClass @class = (ASClass)out_vtableItems[0].DefineAt;

							if (@class.Instance.IsInterface)
							{
								throw new InvalidOperationException("不可能出现接口");
							}


							var stackLoc = compileEnv.MakeStackLocater(TypeKind.Class, (TypeKind)@class.Type_identifier);
							INS_Ld_Class ld_Class = new INS_Ld_Class(token);
							ld_Class.dst = stackLoc;
							ld_Class.classid_index = compileEnv.AddConstClassId(@class);

							compileEnv.instructions.Add(ld_Class);

							int t_index = codescope.Container._vtable.Items.IndexOf(out_vtableItems[0]);
							if (t_index < 0)
								throw new InvalidOperationException();
							if (t_index > 65535)
							{
								throw new ParseException(codescope.Container.QName.ToDebugTypeName() + ".vtable.items.count > 65535");
							}

							var reg = makeOrGetLocater(out_vtableItems[0].Trait.Method.ReturnTypeKind);

							INS_readPoperty readPoperty = new INS_readPoperty(token);
							readPoperty.dst = reg;
							readPoperty.instance = stackLoc;
							readPoperty.const_index = (ushort)t_index;

							compileEnv.SetCallResult(reg);
							compileEnv.instructions.Add(readPoperty);

							return reg;
						}
					}
					else
					{
						var match = compileEnv.imports.Where((t) => { return t.QName.Name == identifier; }).ToArray();
						if (match.Length > 0)
						{
							throw new ResolverException(token, $"Ambiguous reference to {match[0].QName.Name}");
						}

						throw new InvalidOperationException();
					}




				}
			};




			if (data.IsReg)
			{

				if (data.Reg.isLd_R)
				{
					return compileEnv.GetStackLocater(data.Reg);
				}

				Tuple<ASTrait[], AS3ExprStep> traitRef;
				if (!compileEnv.TryReadTraitRef(data.Reg, out traitRef))
				{
					if (targetReg_onlyforLd != null && targetReg_onlyforLd.ID != data.Reg.ID)
					{
						throw new InvalidOperationException();
					}

					return compileEnv.GetStackLocater(data.Reg);
				}

				if (traitRef.Item1.Length == 2 && traitRef.Item1[0] == null && traitRef.Item1[1].Kind == TraitKind.Setter)
				{
					throw new ResolverException(
						token,
						"Property " + traitRef.Item1[1].QName.Name + " is write-only.");
				}

				var instance = compileEnv.ReadRefBindInstance(data.Reg);

				if (traitRef.Item1.Length == 3)
				{
					//检查索引器返回类型
					
					var src = compileEnv.GetStackLocater(data.Reg);
					var target = makeOrGetLocater(traitRef.Item1[0].Method.ReturnTypeKind);

					INS_Ld_ValueRef ld_ValueRef = new INS_Ld_ValueRef(token);
					ld_ValueRef.dst = target;
					ld_ValueRef.source = src;

					compileEnv.instructions.Add(ld_ValueRef);

					return target;


				}
				else if (traitRef.Item1.Length == 2)
				{
					var instancetype = compileEnv.ReadStackType(instance);
					if (instancetype.Maj == TypeKind.Class)
					{
						var type = FindClassById(compileEnv, (ulong)instancetype.Mir);
						int t_index = type._vtable.Items.FindIndex(t => t.Trait == traitRef.Item1[0]);
						if (t_index < 0)
							throw new InvalidOperationException();
						if (t_index > 65535)
						{
							throw new ParseException(type.QName.ToDebugTypeName() + ".vtable.items.count > 65535");
						}

						var stacklocator = makeOrGetLocater(traitRef.Item1[0].Method.ReturnTypeKind);

						INS_readPoperty readPoperty = new INS_readPoperty(token);
						readPoperty.dst = stacklocator;
						readPoperty.instance = instance;
						readPoperty.const_index = (ushort)t_index;

						compileEnv.SetCallResult(stacklocator);
						compileEnv.instructions.Add(readPoperty);

						return stacklocator;
					}
					else if (instancetype.Maj == TypeKind.Vector)
					{
						VectorDef vd = compileEnv.CompileContext.vectorDefs.Find(v => v.Identifier == instancetype.Mir);

						var type = vd.buildVector.vector_class;
						int t_index = type._vtable.Items.FindIndex(t => t.Trait == traitRef.Item1[0]);
						if (t_index < 0)
							throw new InvalidOperationException();
						if (t_index > 65535)
						{
							throw new ParseException(type.QName.ToDebugTypeName() + ".vtable.items.count > 65535");
						}

						var stacklocator = makeOrGetLocater(traitRef.Item1[0].Method.ReturnTypeKind);

						INS_readPoperty readPoperty = new INS_readPoperty(token);
						readPoperty.dst = stacklocator;
						readPoperty.instance = instance;
						readPoperty.const_index = (ushort)t_index;

						compileEnv.SetCallResult(stacklocator);
						compileEnv.instructions.Add(readPoperty);

						return stacklocator;
					}
					else if (instancetype.Maj == TypeKind.Array || instancetype.Maj == TypeKind.String)
					{
						Debug.Assert(instance.index >= 0);

						ASClass ttype = FindClassById(compileEnv, (ulong)instancetype.Maj);
						var type = ttype.Instance;
						Debug.Assert(traitRef.Item1[0].QName.Name == "length");
						
						var stacklocator = makeOrGetLocater(traitRef.Item1[0].Method.ReturnTypeKind);

						INS_Ld_Length ld_Length = new INS_Ld_Length(token);
						ld_Length.instacnce = instance;
						ld_Length.dst = stacklocator;

						compileEnv.instructions.Add(ld_Length);


						return stacklocator;
					}
					else
					{
						ASClass ttype;
						if (instancetype.Maj != TypeKind.Super)
						{
							ttype = FindClassById(compileEnv, (ulong)instancetype.Maj);

							var type = ttype.Instance;
							int t_index = type._vtable.Items.FindIndex(t => t.Trait == traitRef.Item1[0]);
							if (t_index < 0)
								throw new InvalidOperationException();
							if (t_index > 65535)
							{
								throw new ParseException(type.QName.ToDebugTypeName() + ".vtable.items.count > 65535");
							}

							var stacklocator = makeOrGetLocater(traitRef.Item1[0].Method.ReturnTypeKind);

							if (ttype.Instance.Flags.HasFlag(ClassFlags.Vector) && traitRef.Item1[0].QName.Name == "length")
							{
								Debug.Assert(instance.index >= 0);
								
								INS_Ld_Length ld_Length = new INS_Ld_Length(token);
								ld_Length.instacnce = instance;
								ld_Length.dst = stacklocator;

								compileEnv.instructions.Add(ld_Length);


								return stacklocator;
							}
							else if (type.IsInterface)
							{
								//throw new NotImplementedException();

								int class_id = compileEnv.AddConstClassId(ttype);

								INS_readPoperty_Interface readPoperty_Interface = new INS_readPoperty_Interface(token);
								readPoperty_Interface.dst = stacklocator;
								readPoperty_Interface.instance = instance;
								readPoperty_Interface.class_id = class_id;
								readPoperty_Interface.const_index = (ushort)t_index;

								compileEnv.SetCallResult(stacklocator);
								compileEnv.instructions.Add(readPoperty_Interface);


							}
							else
							{
								INS_readPoperty readPoperty = new INS_readPoperty(token);
								readPoperty.dst = stacklocator;
								readPoperty.instance = instance;
								readPoperty.const_index = (ushort)t_index;

								compileEnv.SetCallResult(stacklocator);
								compileEnv.instructions.Add(readPoperty);
							}
							return stacklocator;
						}
						else
						{
							ttype = FindClassById(compileEnv, (ulong)instancetype.Mir);
							var type = ttype.Instance;
							int t_index = type._vtable.Items.FindIndex(t => t.Trait == traitRef.Item1[0]);
							if (t_index < 0)
								throw new InvalidOperationException();
							if (t_index > 65535)
							{
								throw new ParseException(type.QName.ToDebugTypeName() + ".vtable.items.count > 65535");
							}

							var stacklocator = makeOrGetLocater(traitRef.Item1[0].Method.ReturnTypeKind);

							int mid = compileEnv.AddSuperMethod(ttype, t_index);

							INS_Ld_SuperMethod ld_SuperMethod = new INS_Ld_SuperMethod(token);
							ld_SuperMethod.dst = compileEnv.MakeStackLocater(TypeKind.Function);
							ld_SuperMethod.instance = instance;
							ld_SuperMethod.const_index = mid;

							compileEnv.instructions.Add(ld_SuperMethod);

							INS_BindThis_Call bindThis_Call = new INS_BindThis_Call(token);
							bindThis_Call.dst = stacklocator;
							bindThis_Call.args = new StackLocater[0];
							bindThis_Call.function = ld_SuperMethod.dst;
							bindThis_Call._this_ = instance;

							compileEnv.instructions.Add(bindThis_Call);

							return stacklocator;

						}
					}

				}
				else if (traitRef.Item1.Length == 1)
				{
					var src = compileEnv.GetStackLocater(data.Reg);

					TypeKind targettype;

					switch (traitRef.Item1[0].Kind)
					{
						case TraitKind.Slot:
							targettype = traitRef.Item1[0].TypeKind;
							break;
						case TraitKind.Method:
							targettype = TypeKind.Function;
							break;
						case TraitKind.Getter:
						case TraitKind.Setter:
							throw new InvalidOperationException();
						case TraitKind.Class:
							//targettype = TypeKind.Class;
							//break;
							throw new InvalidOperationException();
						case TraitKind.Function:
							//targettype = TypeKind.Function;
							//break;
							throw new InvalidOperationException();
						case TraitKind.Constant:
							targettype = traitRef.Item1[0].TypeKind;
							break;
						default:
							throw new NotImplementedException();

					}

					var target = makeOrGetLocater(targettype);

					INS_Ld_ValueRef ld_ValueRef = new INS_Ld_ValueRef(token);
					ld_ValueRef.dst = target;
					ld_ValueRef.source = src;

					compileEnv.instructions.Add(ld_ValueRef);

					return target;


					//return compileEnv.GetStackLocater(data.Reg);
				}
				else
				{
					var src = compileEnv.GetStackLocater(data.Reg);
					var target = makeOrGetLocater(TypeKind.Any);

					INS_Ld_ValueRef ld_ValueRef = new INS_Ld_ValueRef(token);
					ld_ValueRef.dst = target;
					ld_ValueRef.source = src;

					compileEnv.instructions.Add(ld_ValueRef);

					return target;

					//return compileEnv.GetStackLocater(data.Reg);
				}
			}
			else
			{
				if (data.Data.FF1Type == FF1DataValueType.const_number)
				{
					if (((string)data.Data.Value).StartsWith("0x"))
					{
						try
						{
							int v1 = int.Parse(((string)data.Data.Value).Substring(2), System.Globalization.NumberStyles.AllowHexSpecifier);

							if (v1 < 0) //需要用uint存储
							{
								uint v = uint.Parse(((string)data.Data.Value).Substring(2), System.Globalization.NumberStyles.AllowHexSpecifier);
								int constindex = compileEnv.AddConstUInt(v);

								StackLocater stackLocater = makeOrGetLocater(TypeKind.Uint);

								INS_Ld_Const ld_Const = new INS_Ld_Const(token);
								ld_Const.dst = stackLocater;
								ld_Const.const_index = constindex;

								compileEnv.instructions.Add(ld_Const);

								return stackLocater;
							}
							else
							{
								int constindex;
								TypeKind typeKind;
								if (v1 <= sbyte.MaxValue)
								{
									constindex = compileEnv.AddConstSbyte((sbyte)v1);
									typeKind = TypeKind.SByte;
								}
								else if (v1 <= byte.MaxValue)
								{
									constindex = compileEnv.AddConstbyte((byte)v1);
									typeKind = TypeKind.Byte;
								}
								else if (v1 <= short.MaxValue)
								{
									constindex = compileEnv.AddConstShort((short)v1);
									typeKind = TypeKind.Short;
								}
								else if (v1 <= ushort.MaxValue)
								{
									constindex = compileEnv.AddConstUShort((ushort)v1);
									typeKind = TypeKind.UShort;
								}
								else
								{
									constindex = compileEnv.AddConstInt(v1);
									typeKind = TypeKind.Int;
								}

								StackLocater stackLocater = makeOrGetLocater(typeKind);

								INS_Ld_Const ld_Const = new INS_Ld_Const(token);
								ld_Const.dst = stackLocater;
								ld_Const.const_index = constindex;

								compileEnv.instructions.Add(ld_Const);

								return stackLocater;
							}

						}
						catch (OverflowException)
						{
							try
							{
								//只能用BigInteger读取然后转double.
								BigInteger bi = BigInteger.Parse("0" + ((string)data.Data.Value).Substring(2), System.Globalization.NumberStyles.AllowHexSpecifier);
								double v = (double)bi;
								int constindex = compileEnv.AddConstNumber(v);

								StackLocater stackLocater = makeOrGetLocater(TypeKind.Number);

								INS_Ld_Const ld_Const = new INS_Ld_Const(token);
								ld_Const.dst = stackLocater;
								ld_Const.const_index = constindex;

								compileEnv.instructions.Add(ld_Const);

								return stackLocater;

							}
							catch (Exception ex)
							{
								throw new ResolverException(token, ex.Message);
							}

						}

					}

					else if (((string)data.Data.Value).EndsWith("f"))
					{
						string str = ((string)data.Data.Value);
						float outv;
						if (float.TryParse(str.Substring(0, str.Length - 1), out outv))
						{
							int constindex = compileEnv.AddConstFloat(outv);

							StackLocater stackLocater = makeOrGetLocater(TypeKind.Float);
							INS_Ld_Const ld_Const = new INS_Ld_Const(token);
							ld_Const.dst = stackLocater;
							ld_Const.const_index = constindex;

							compileEnv.instructions.Add(ld_Const);
							return stackLocater;
						}
						else
						{
							throw new ResolverException(token, "parse float failed: " + str);
						}
					}
					else
					{
						sbyte t_sbytev;
						if (sbyte.TryParse((string)data.Data.Value, out t_sbytev))
						{
							if (t_sbytev == 0 && ((string)data.Data.Value).StartsWith("-"))
							{
								int constindex = compileEnv.AddConstNumber(-0.0);

								StackLocater stackLocater = makeOrGetLocater(TypeKind.Number);
								INS_Ld_Const ld_Const = new INS_Ld_Const(token);
								ld_Const.dst = stackLocater;
								ld_Const.const_index = constindex;

								compileEnv.instructions.Add(ld_Const);
								return stackLocater;
							}
							else
							{
								int constindex = compileEnv.AddConstSbyte(t_sbytev);

								StackLocater stackLocater = makeOrGetLocater(TypeKind.SByte);
								INS_Ld_Const ld_Const = new INS_Ld_Const(token);
								ld_Const.dst = stackLocater;
								ld_Const.const_index = constindex;

								compileEnv.instructions.Add(ld_Const);
								return stackLocater;
							}
						}
						else
						{
							byte t_bytev;
							if (byte.TryParse((string)data.Data.Value, out t_bytev))
							{
								int constindex = compileEnv.AddConstbyte(t_bytev);

								StackLocater stackLocater = makeOrGetLocater(TypeKind.Byte);
								INS_Ld_Const ld_Const = new INS_Ld_Const(token);
								ld_Const.dst = stackLocater;
								ld_Const.const_index = constindex;

								compileEnv.instructions.Add(ld_Const);
								return stackLocater;
							}
							else
							{
								short t_shortv;
								if (short.TryParse((string)data.Data.Value, out t_shortv))
								{
									int constindex = compileEnv.AddConstShort(t_shortv);

									StackLocater stackLocater = makeOrGetLocater(TypeKind.Short);
									INS_Ld_Const ld_Const = new INS_Ld_Const(token);
									ld_Const.dst = stackLocater;
									ld_Const.const_index = constindex;

									compileEnv.instructions.Add(ld_Const);
									return stackLocater;
								}
								else
								{
									ushort t_ushortv;
									if (ushort.TryParse((string)data.Data.Value, out t_ushortv))
									{
										int constindex = compileEnv.AddConstUShort(t_ushortv);

										StackLocater stackLocater = makeOrGetLocater(TypeKind.UShort);
										INS_Ld_Const ld_Const = new INS_Ld_Const(token);
										ld_Const.dst = stackLocater;
										ld_Const.const_index = constindex;

										compileEnv.instructions.Add(ld_Const);
										return stackLocater;
									}
									else
									{
										int t_intv;
										if (int.TryParse((string)data.Data.Value, out t_intv))
										{
											int constindex = compileEnv.AddConstInt(t_intv);

											StackLocater stackLocater = makeOrGetLocater(TypeKind.Int);
											INS_Ld_Const ld_Const = new INS_Ld_Const(token);
											ld_Const.dst = stackLocater;
											ld_Const.const_index = constindex;

											compileEnv.instructions.Add(ld_Const);
											return stackLocater;
										}
										else
										{
											uint t_uintv; if (uint.TryParse((string)data.Data.Value, out t_uintv))
											{
												int constindex = compileEnv.AddConstUInt(t_uintv);

												StackLocater stackLocater = makeOrGetLocater(TypeKind.Uint);
												INS_Ld_Const ld_Const = new INS_Ld_Const(token);
												ld_Const.dst = stackLocater;
												ld_Const.const_index = constindex;

												compileEnv.instructions.Add(ld_Const);
												return stackLocater;
											}
											else
											{
												double v; 
												if (double.TryParse((string)data.Data.Value, out v))
												{
													if (((string)data.Data.Value).StartsWith("-") && v == 0)
													{
														v = -0.0;
													}

													int constindex = compileEnv.AddConstNumber(v);

													StackLocater stackLocater = makeOrGetLocater(TypeKind.Number);
													INS_Ld_Const ld_Const = new INS_Ld_Const(token);
													ld_Const.dst = stackLocater;
													ld_Const.const_index = constindex;

													compileEnv.instructions.Add(ld_Const);
													return stackLocater;
												}
												else
												{
													throw new ResolverException(token, $"cannot parse '{(string)data.Data.Value}' to Number.");
												}
											}
										}
									}
								}
							}

						}


					}
				}
				else if (data.Data.FF1Type == FF1DataValueType.identifier)
				{
					return parseIdentifier(data.Data.Value.ToString());
				}
				else if (data.Data.FF1Type == FF1DataValueType.const_string)
				{
					string v = data.Data.Value.ToString();

					int constindex = compileEnv.AddConstString(v);

					StackLocater stackLocater = makeOrGetLocater(TypeKind.String);

					INS_Ld_Const ld_Const = new INS_Ld_Const(token);
					ld_Const.dst = stackLocater;
					ld_Const.const_index = constindex;

					compileEnv.instructions.Add(ld_Const);

					return stackLocater;


					//throw new NotImplementedException();
				}
				else if (data.Data.FF1Type == FF1DataValueType.this_pointer)
				{
					var scope = compileEnv.Scope;
					while (scope.Kind == CodeScopeKind.Method)
					{
						if (!((ASMethodBody)scope.Container).Method.__ismethod)
						{
							break;
						}

						scope = scope.Parent;
					}
					TypeKind this_type;
					switch (scope.Kind)
					{
						case CodeScopeKind.Script:
							this_type = TypeKind.Any;
							break;
						case CodeScopeKind.Instance:
							this_type = (TypeKind)scope.TypeLayout.ASType.Type_identifier;
							break;
						case CodeScopeKind.Class:
							if (((ASMethodBody)compileEnv.Scope.Container).Method.IsAnonymous)
							{
								this_type = TypeKind.Any;
								break;
							}
							else
							{
								throw new ResolverException(token, "The 'this' keyword can not be used in static methods. It can only be used in instance methods, function closures, and global code.");
							}
						case CodeScopeKind.Method:
							this_type = TypeKind.Any;
							break;
						default:
							throw new InvalidOperationException();
					}


					StackLocater stackLocater = makeOrGetLocater(this_type);

					
					INS_Ld_this ld_This = new INS_Ld_this(token);
					ld_This.dst = stackLocater;
					compileEnv.instructions.Add(ld_This);
					
					return stackLocater;
				}
				else if (data.Data.FF1Type == FF1DataValueType.super_pointer)
				{
					var scope = compileEnv.Scope;

					if (scope.Kind == CodeScopeKind.Instance)
					{
						var super_type = (TypeKind)((ASInstance)scope.Container)._super_class_.Type_identifier;

						StackLocater stackLocater = makeOrGetLocater2(TypeKind.Super, super_type);

						INS_Ld_this ld_This = new INS_Ld_this(token);
						ld_This.dst = stackLocater;
						compileEnv.instructions.Add(ld_This);

						return stackLocater;
					}
					else
					{
						if (scope.Kind != CodeScopeKind.Method)
						{
							throw new ResolverException(token, "A super expression can be used only inside class instance methods.");
						}
						else
						{
							if (scope.Parent.Kind != CodeScopeKind.Instance)
							{
								throw new ResolverException(token, "A super expression can be used only inside class instance methods.");
							}
						}

						var super_type = (TypeKind)((ASInstance)scope.Parent.Container)._super_class_.Type_identifier;

						StackLocater stackLocater = makeOrGetLocater2(TypeKind.Super, super_type);

						INS_Ld_this ld_This = new INS_Ld_this(token);
						ld_This.dst = stackLocater;
						compileEnv.instructions.Add(ld_This);

						return stackLocater;
					}
				}
				else if (data.Data.FF1Type == FF1DataValueType.as3_vector)
				{

					if (compileEnv.stack_loaded_heapunit.Peek().ContainsKey(data.Data.Value))
					{
						return compileEnv.stack_loaded_heapunit.Peek()[data.Data.Value];
					}
					else
					{

						AS3Vector as3vector = (AS3Vector)data.Data.Value;

						string vec = as3vector.VectorTypeStr;

						List<TypeKind> type_path = new List<TypeKind>();
						while (vec.StartsWith("Vector.<"))
						{
							type_path.Add(TypeKind.Vector);

							vec = vec.Substring(8, vec.Length - 9);
						}

						if (vec == "*")
						{
							type_path.Add(TypeKind.Any);
						}
						else
						{
							StackLocater vector_type = parseIdentifier(vec);

							CompileTypeKind typeKind = compileEnv.ReadStackType(vector_type);
							if (typeKind.Maj != TypeKind.Class)
							{
								throw new ResolverException(token, "Type was not found or was not a compile-time constant: " + vec + ".");
							}

							type_path.Add(typeKind.Mir);
						}
						VectorDef vector = VectorDef.CreateOrGet(compileEnv.CompileContext, type_path);

						int constindex = compileEnv.AddVectorDef(vector);





						StackLocater stackLocater = makeOrGetLocater2(TypeKind.Vector, vector.Identifier);

						INS_Ld_VectorType ld_Const = new INS_Ld_VectorType(token);
						ld_Const.dst = stackLocater;
						ld_Const.vectortype_index = constindex;

						compileEnv.instructions.Add(ld_Const);

						if (as3vector.Constructor == null)
						{
							compileEnv.stack_loaded_heapunit.Peek().Add(data.Data.Value, stackLocater);
							return stackLocater;
						}
						else
						{
							if (newop != null)
							{
								newop.Arg3 = as3vector.Constructor;

								compileEnv.stack_loaded_heapunit.Peek().Add(data.Data.Value, stackLocater);

								return stackLocater;

							}
							else
							{
								var items = (List<AS3DataStackElement>)as3vector.Constructor.Data.Value;
								List<StackLocater> arguments = new List<StackLocater>();
								for (int i = 0; i < items.Count; i++)
								{
									var v = LoadRightValue(items[i], compileEnv, items[i].IsReg ? data.Data.token : items[i].Data.token);

									var srctype = compileEnv.ReadStackType(v);
									if (srctype.Maj == TypeKind.Array && items.Count == 1)
									{
										arguments.Add(v);
									}
									else if (srctype.Maj == TypeKind.Object && items.Count == 1)
									{
										arguments.Add(v);
									}
									else
									{
										var arg = ExpressionIL.TestTypeConvert(compileEnv, v, new CompileTypeKind() { Maj = vector.buildVector.ElementType }, items[i].IsReg ? data.Data.token : items[i].Data.token);
										arguments.Add(arg);
									}

								}

								ld_Const.dst = compileEnv.MakeStackLocater(TypeKind.Vector, vector.Identifier);

								INS_bindGlobal_Call method_Call = new INS_bindGlobal_Call(token);
								method_Call.dst = stackLocater;
								method_Call.function = ld_Const.dst;
								method_Call.args = arguments.ToArray();

								compileEnv.instructions.Add(method_Call);


								compileEnv.stack_loaded_heapunit.Peek().Add(data.Data.Value, stackLocater);
								return stackLocater;

								//构造Vector并初始化元素。
								//throw new NotImplementedException();
							}
						}

					}
				}
				else if (data.Data.FF1Type == FF1DataValueType.as3_function)
				{
					if (compileEnv.stack_loaded_heapunit.Peek().ContainsKey(data.Data.Value))
					{
						return compileEnv.stack_loaded_heapunit.Peek()[data.Data.Value];
					}
					else
					{

						ASMethod method = compileEnv.CompileContext.dict_method_as3function[(AS3Function)data.Data.Value];

						int methodid = compileEnv.AddConstMethod(method);

						StackLocater stackLocater = makeOrGetLocater(TypeKind.Function);

						INS_Ld_Function ld_Function = new INS_Ld_Function(token);
						ld_Function.dst = stackLocater;
						ld_Function.const_index = methodid;
						ld_Function.heapLocater.ScopeIndex = ushort.MaxValue;
						ld_Function.heapLocater.MemberIndex = ushort.MaxValue;

						compileEnv.instructions.Add(ld_Function);


						compileEnv.stack_loaded_heapunit.Peek().Add(data.Data.Value, stackLocater);

						return stackLocater;
					}

				}
				else if (data.Data.FF1Type == FF1DataValueType.dynamicobj)
				{
					if (compileEnv.stack_loaded_heapunit.Peek().ContainsKey(data.Data.Value))
					{
						return compileEnv.stack_loaded_heapunit.Peek()[data.Data.Value];
					}
					else
					{

						var items = (Hashtable)data.Data.Value;


						INS_Ld_Class ld_Class = new INS_Ld_Class(token);
						ld_Class.dst = compileEnv.MakeStackLocater(TypeKind.Class, (TypeKind)compileEnv.CompileContext.player_for_compiler.Context.OBJECT.Type_identifier);
						ld_Class.classid_index = compileEnv.AddConstClassId(compileEnv.CompileContext.player_for_compiler.Context.OBJECT);
						compileEnv.instructions.Add(ld_Class);


						StackLocater dst = makeOrGetLocater((TypeKind)compileEnv.CompileContext.player_for_compiler.Context.OBJECT.Type_identifier);

						INS_New_Instance new_Instance = new INS_New_Instance(data.Data.token);
						new_Instance.dst = dst;
						new_Instance.typeLocator = ld_Class.dst;
						new_Instance.args = new StackLocater[0];

						compileEnv.instructions.Add(new_Instance);

						if (items.Count > 0)
						{
							HashSet<string> addedprops = new HashSet<string>();
							List<object> keys = new List<object>();
							foreach (var key in items.Keys)
							{
								keys.Add(key);	
							}
							keys.Sort((a, b) => {
								Token ta = (Token)a;
								Token tb = (Token)b;
								if (ta.line != tb.line)
								{
									return ta.line - tb.line;
								}
								else
								{ 
									return ta.ptr - tb.ptr;
								}
							
							} ); //将key排序


							foreach (var key in keys)
							{
								var v = items[key];
								StackLocater local = LoadRightValue((AS3DataStackElement)v, compileEnv, (Token)key);

								if (!addedprops.Contains(((Token)key).StringValue))
								{
									addedprops.Add(((Token)key).StringValue);

									int const_index = compileEnv.AddConstString(((Token)key).StringValue);
									var prop_name = compileEnv.MakeStackLocater(TypeKind.String);

									INS_Ld_Const ld_Const = new INS_Ld_Const((Token)key);
									ld_Const.dst = prop_name;
									ld_Const.const_index = const_index;
									compileEnv.instructions.Add(ld_Const);

									INS_Create_Prop create_Prop = new INS_Create_Prop((Token)key);
									create_Prop.dst = dst;
									create_Prop.key = prop_name;
									create_Prop.value = local;

									compileEnv.instructions.Add(create_Prop);
								}

							}
							//throw new NotImplementedException();
						}

						compileEnv.stack_loaded_heapunit.Peek().Add(data.Data.Value, dst);

						return dst;
					}
				}

				else if (data.Data.FF1Type == FF1DataValueType.as3_array)
				{
					if (compileEnv.stack_loaded_heapunit.Peek().ContainsKey(data.Data.Value))
					{
						return compileEnv.stack_loaded_heapunit.Peek()[data.Data.Value];
					}
					else
					{

						var items = (List<AS3DataStackElement>)data.Data.Value;


						/**
						 var tetrominoes:Array = [
                [[1,1,1,1]],                               
                [[2,2],[2,2]],                              
                [[3,3,3],[0,3,0]],                         
                [[4,0,0],[4,4,4]],                         
                [[5,5,5],[5,0,0]],                         
                [[0,6,6],[6,6,0]],                         
                [[7,7,0],[0,7,7]]      遇到这种代码会出现栈槽复用问题，全部改成逐个插入                     
            ];
						 */
						//if (items.Count < 4)
						//{
						//	INS_Ld_Class ld_Class = new INS_Ld_Class(token);
						//	ld_Class.dst = compileEnv.MakeStackLocater(TypeKind.Class, TypeKind.Array);
						//	ld_Class.classid_index = compileEnv.AddConstClassId((ulong)TypeKind.Array);
						//	compileEnv.instructions.Add(ld_Class);

						//	StackLocater dst = makeOrGetLocater(TypeKind.Array);
						//	List<StackLocater> arguments = new List<StackLocater>();
						//	arguments.Add(dst);

						//	for (int i = 0; i < items.Count; i++)
						//	{
						//		var v = LoadRightValue(items[i], compileEnv, items[i].IsReg ? data.Data.token : items[i].Data.token);
						//		arguments.Add(v);
						//	}

						//	INS_New_Instance new_Instance = new INS_New_Instance(data.Data.token);
						//	new_Instance.dst = dst;
						//	new_Instance.typeLocator = ld_Class.dst;
						//	new_Instance.args = arguments.ToArray();

						//	compileEnv.instructions.Add(new_Instance);

						//	compileEnv.stack_loaded_heapunit.Peek().Add(data.Data.Value, dst);

						//	return dst;
						//}
						//else
						{
							var firstreg = items.FirstOrDefault(i => i.IsReg);

							int instance_at = 0;

							if (firstreg == null)
							{
								instance_at = compileEnv.instructions.Count;
							}
							else
							{
								var stack = compileEnv.GetStackLocater(firstreg.Reg);
								var at = compileEnv.instructions.Last(i => i.GetDef().Contains(stack));

								int index = compileEnv.instructions.IndexOf(at) ;
								instance_at = index ;
							}

							INS_Ld_Class ld_Class = new INS_Ld_Class(token);
							ld_Class.dst = compileEnv.MakeStackLocater(TypeKind.Class, TypeKind.Array);
							ld_Class.classid_index = compileEnv.AddConstClassId((ulong)TypeKind.Array);
							compileEnv.instructions.Insert(instance_at, ld_Class);

							StackLocater dst = makeOrGetLocater(TypeKind.Array);
							INS_New_Instance new_Instance = new INS_New_Instance(data.Data.token);
							new_Instance.dst = dst;
							new_Instance.typeLocator = ld_Class.dst;
							new_Instance.args = new StackLocater[0];

							compileEnv.instructions.Insert(instance_at + 1, new_Instance);

							for (int i = 0; i < items.Count; i++)
							{
								var v = LoadRightValue(items[i], compileEnv, items[i].IsReg ? data.Data.token : items[i].Data.token);

								INS_Array_Vector_InitElement pushelement = new INS_Array_Vector_InitElement(items[i].IsReg ? data.Data.token : items[i].Data.token);
								pushelement.dst = v;
								pushelement.instance = dst;
								pushelement.index = i;

								if (items[i].IsReg)
								{
									//var stack = compileEnv.GetStackLocater(items[i].Reg);
									var at = compileEnv.instructions.Last(i => i.GetDef().Contains(v));

									int index = compileEnv.instructions.IndexOf(at) + 1;
									compileEnv.instructions.Insert(index, pushelement);

								}
								else
								{
									compileEnv.instructions.Add(pushelement);
								}

							}

							compileEnv.stack_loaded_heapunit.Peek().Add(data.Data.Value, dst);
							return dst;

						}
					}
				}
				else if (data.Data.FF1Type == FF1DataValueType.const_regexp)
				{
					throw new ResolverException( data.Data.token, "regexp is not support." );
				}
				else
				{
					throw new NotImplementedException();
				}

			}

		}


	}
}
