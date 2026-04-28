using juicescript.ABC;
using juicescript.ABC.INS;
using juicescript.ABC.Locaters;
using juicescript.compiler.AST;
using juicescript.compiler.AST.Expr;
using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

namespace juicescript.compiler.IL.Generator
{
    internal class CallFuncBuilder
    {
        internal static void CheckMethodArgs(List<StackLocater> arguments,CompileEnv compileEnv,AS3ExprStep step,ASMethod method)
        {
			bool interRest = false;
			{
				int i = 0;
				for (; i < arguments.Count; i++)
				{
					if (i < method.Parameters.Count)
					{
						var p = method.Parameters[i];
						if (p.IsRest)
						{
							interRest = true;
						}
						if (!interRest)
						{
							var arg = ExpressionIL.TestTypeConvert(compileEnv, arguments[i], new CompileTypeKind() { Maj = p.TypeKind }, step.token);
							arguments[i] = arg;
						}
					}
					else
					{
						if (interRest) //进入不固定长度参数
						{

						}
						else //参数过多抛出异常
						{
							throw new ResolverException(step.token, $"Incorrect number of arguments.  Expected no more than {method.Parameters.Count}");
						}
					}
				}

				if (i < method.Parameters.Count)
				{
					var p = method.Parameters[i];
					if (!p.IsOptional && !p.IsRest)
					{
                        if (step.OpCode == "autogen ctor")
                        {
							// No default constructor found in base class A.
							throw new ResolverException(step.token,
							   $"No default constructor found in base class { method.Container.QName.ToDebugTypeName() }."
							   );
						}
						else
                        {
                            throw new ResolverException(step.token,
                                $"Incorrect number of arguments.  Expected {method.Parameters.Count(p => !p.IsOptional && !p.IsRest)}"
                                );
                        }
                    }
                }
			}
		}


        internal void Build(AS3ExprStep step, AST.AS3Expression expression, CompileEnv compileEnv)
        {

            if (!step.Arg2.IsReg && step.Arg2.Data.FF1Type == FF1DataValueType.super_pointer)
            {
                //调父类构造函数
                List<StackLocater> arguments = new List<StackLocater>();
                if (step.Arg3 != null)
                {
                    //读取构造函数的参数
                    var args = (List<AS3DataStackElement>)step.Arg3.Data.Value;
                    for (int i = 0; i < args.Count; i++)
                    {
                        arguments.Add(ExpressionIL.LoadRightValue(args[i], compileEnv, step.token));
                    }
                }

                //上下文必然是构造函数
                var method = ((ASMethodBody)compileEnv.Scope.Container).Method;
                ASInstance instance = (ASInstance)method.Container;

                if (!method.IsConstructor)
                {
                    throw new ResolverException(step.token, $"A super statement can be used only inside class instance constructors.");
                }
                

                ASMethod super_ctor = instance._super_class_.Instance.Constructor;

                CheckMethodArgs(arguments, compileEnv, step, super_ctor);


                INS_SuperCtor superCtor = new INS_SuperCtor(step.token);
                superCtor.super_type = compileEnv.AddConstClassId(instance._super_class_); //instance._super_class_.Type_identifier;
                superCtor.args = arguments.ToArray();

                compileEnv.instructions.Add(superCtor);
            }
            else if (!step.Arg2.IsReg && step.Arg2.Data.FF1Type == FF1DataValueType.as3_function)
            {
                ASMethod method = compileEnv.CompileContext.dict_method_as3function[(AS3Function)step.Arg2.Data.Value];
                //( function(){} )(); 这样的匿名函数
                //不检查参数
                List<StackLocater> arguments = new List<StackLocater>();
                if (step.Arg3 != null)
                {
                    //读取传递的参数
                    var args = (List<AS3DataStackElement>)step.Arg3.Data.Value;
                    for (int i = 0; i < args.Count; i++)
                    {
                        arguments.Add(ExpressionIL.LoadRightValue(args[i], compileEnv, step.token));
                    }
                }

                if (string.IsNullOrEmpty(method.Name))
                {
                    int method_id = compileEnv.AddConstMethod(method);

                    INS_Ld_Function_Call ld_Function_Call = new INS_Ld_Function_Call(step.token);
                    ld_Function_Call.args = arguments.ToArray();
                    ld_Function_Call.const_index = method_id;
                    ld_Function_Call.dst = compileEnv.GetStackLocater(step.Arg1.Reg, method.ReturnTypeKind);

                    compileEnv.SetCallResult(ld_Function_Call.dst);
                    compileEnv.instructions.Add(ld_Function_Call);
                }
                else
                {
                    int method_id = compileEnv.AddConstMethod(method);

                    ScopeHeapLocater scopeHeapLocater;
                    TypeKind heapType;
                    ASTrait findtrait;
                    CodeScope findscope;
                    ScopeMember findmember;
                    VTableItem[] out_vtableItems;
                    var result = ExpressionIL.FindIdentifier(method.Name, null, compileEnv, step.token,
                        out scopeHeapLocater, out heapType, out findtrait, out findscope, out findmember, out out_vtableItems);
                    if (result != ExpressionIL.FindIdResultType.ScopeMember)
                    {
                        throw new InvalidOperationException();
                    }
                    //INS_Ld_Function ld_Function = new INS_Ld_Function(method.Token);
                    //ld_Function.const_index = method_id;
                    //ld_Function.heapLocater = scopeHeapLocater;
                    //ld_Function.dst = compileEnv.MakeStackLocater(TypeKind.Function);

                    //compileEnv.instructions.Add(ld_Function);

                    //INS_bindGlobal_Call iNS_BindGlobal_Call = new INS_bindGlobal_Call(method.Token);
                    //               iNS_BindGlobal_Call.args = arguments.ToArray();
                    //               iNS_BindGlobal_Call.dst = compileEnv.GetStackLocater(step.Arg1.Reg, method.ReturnTypeKind);
                    //               iNS_BindGlobal_Call.function = ld_Function.dst;

                    INS_Ld_Function_BindGlobal_Call ld_Function_BindGlobal_Call = new INS_Ld_Function_BindGlobal_Call(method.Token);
                    ld_Function_BindGlobal_Call.const_index = method_id;
                    ld_Function_BindGlobal_Call.heapLocater = scopeHeapLocater;
                    ld_Function_BindGlobal_Call.args = arguments.ToArray();
                    ld_Function_BindGlobal_Call.dst = compileEnv.GetStackLocater(step.Arg1.Reg, method.ReturnTypeKind);


                    compileEnv.SetCallResult(ld_Function_BindGlobal_Call.dst);
                    compileEnv.instructions.Add(ld_Function_BindGlobal_Call);

                }

            }
            else if (!step.Arg2.IsReg && step.Arg2.Data.FF1Type == FF1DataValueType.identifier)
            {

                ScopeHeapLocater scopeHeapLocater;
                TypeKind heapType;
                ASTrait findtrait;
                CodeScope findscope;
                ScopeMember findmember;
                VTableItem[] out_vtableItems;
                var result = ExpressionIL.FindIdentifier(step.Arg2.Data.Value.ToString(), null, compileEnv, step.token,
                    out scopeHeapLocater, out heapType, out findtrait, out findscope, out findmember, out out_vtableItems);

            lbl_loadclass:


                switch (result)
                {
                    case ExpressionIL.FindIdResultType.NotFound:
                        {

                            var t = compileEnv.imports.FirstOrDefault(item => item.Kind == TraitKind.Class && item.Class.QName.Name == step.Arg2.Data.Value.ToString());

                            if (t != null)
                            {
                                if (t.Class == compileEnv.CompileContext.player_for_compiler.Context.ARRAY)
                                {
                                    step.OpCode = "new";
                                    //转为调Array的构造函数。
                                    new ConstructorBuilder().Build(step, compileEnv);
                                }
                                else
                                {
                                    //强制类型转换

                                    List<StackLocater> arguments = new List<StackLocater>();
                                    if (step.Arg3 != null)
                                    {
                                        //读取传递的参数
                                        var args = (List<AS3DataStackElement>)step.Arg3.Data.Value;
                                        for (int i = 0; i < args.Count; i++)
                                        {
                                            arguments.Add(ExpressionIL.LoadRightValue(args[i], compileEnv, step.token));
                                        }
                                    }

                                    if (arguments.Count != 1)
                                    {
                                        throw new ResolverException(step.token, "Incorrect number of arguments.  Expected 1");
                                    }

                                    var class_id = compileEnv.AddConstClassId(t.Class);

                                    StackLocater ret = compileEnv.GetStackLocater(step.Arg1.Reg, (TypeKind)t.Class.Type_identifier);
                                    INS_TypeCast typeCast = new INS_TypeCast(step.token);
                                    typeCast.dst = ret;
                                    typeCast.value = arguments[0];
                                    typeCast.class_id = class_id;

                                    compileEnv.instructions.Add(typeCast);

                                }
                                //throw new NotImplementedException();
                                break;
                            }

                            //这里实在没有必要非要让运行时才出错。
                            //StackLocater ret = compileEnv.GetStackLocater(step.Arg1.Reg);

                            //StackLocater function = ExpressionIL.LoadRightValue(step.Arg2, compileEnv, step.token);

                            //List<StackLocater> argements = new List<StackLocater>();
                            //if (step.Arg3 != null)
                            //{
                            //	//读取传递的参数
                            //	var args = (List<AS3DataStackElement>)step.Arg3.Data.Value;
                            //	for (int i = 0; i < args.Count; i++)
                            //	{
                            //		argements.Add(ExpressionIL.LoadRightValue(args[i], compileEnv, step.token));
                            //	}
                            //}

                            //INS_bindGlobal_Call bindGlobal_Call = new INS_bindGlobal_Call(step.token);
                            //bindGlobal_Call.args = argements.ToArray();
                            //bindGlobal_Call.result = ret;
                            //bindGlobal_Call.function = function;

                            //compileEnv.SetCallResult(ret);
                            //compileEnv.instructions.Add(bindGlobal_Call);

                            //                     break;


                            throw new ResolverException(step.token, $"Call to a possibly undefined method {step.Arg2.Data.Value.ToString()}.");
                        }
                    case ExpressionIL.FindIdResultType.Ambiguous:
                        throw new ResolverException(step.token, $"Ambiguous reference to {step.Arg2.Data.Value.ToString()}");
                    case ExpressionIL.FindIdResultType.ScopeMember:

                        {
                            if (findmember.Kind == ScopeMemberKind.Parameter && (findmember.TypeKind == TypeKind.Function
                                ||
                                findmember.TypeKind == TypeKind.Any
                                ))
                            {
                                //function(a:Function){ a(); }
                                //function的this是 global 

                                StackLocater ret = compileEnv.GetStackLocater(step.Arg1.Reg);

                                StackLocater function = ExpressionIL.LoadRightValue(step.Arg2, compileEnv, step.token);

                                List<StackLocater> arguments = new List<StackLocater>();
                                if (step.Arg3 != null)
                                {
                                    //读取传递的参数
                                    var args = (List<AS3DataStackElement>)step.Arg3.Data.Value;
                                    for (int i = 0; i < args.Count; i++)
                                    {
                                        arguments.Add(ExpressionIL.LoadRightValue(args[i], compileEnv, step.token));
                                    }
                                }


                                INS_bindGlobal_Call bindGlobal_Call = new INS_bindGlobal_Call(step.token);
                                bindGlobal_Call.args = arguments.ToArray();
                                bindGlobal_Call.dst = ret;
                                bindGlobal_Call.function = function;

                                compileEnv.SetCallResult(ret);
                                compileEnv.instructions.Add(bindGlobal_Call);
                            }
                            else if (findtrait.TypeKind == TypeKind.Function)
                            {
                                //var a:Function; a();
                                //function的this 是变量a 所在global
                                StackLocater ret = compileEnv.GetStackLocater(step.Arg1.Reg);

                                StackLocater function = ExpressionIL.LoadRightValue(step.Arg2, compileEnv, step.token);

                                List<StackLocater> arguments = new List<StackLocater>();
                                if (step.Arg3 != null)
                                {
                                    //读取传递的参数
                                    var args = (List<AS3DataStackElement>)step.Arg3.Data.Value;
                                    for (int i = 0; i < args.Count; i++)
                                    {
                                        arguments.Add(ExpressionIL.LoadRightValue(args[i], compileEnv, step.token));
                                    }
                                }

                                INS_bindGlobal_Call bindGlobal_Call = new INS_bindGlobal_Call(step.token);
                                bindGlobal_Call.args = arguments.ToArray();
                                bindGlobal_Call.dst = ret;
                                bindGlobal_Call.function = function;

                                compileEnv.SetCallResult(ret);
                                compileEnv.instructions.Add(bindGlobal_Call);

                                //throw new NotImplementedException();

                            }
                            else if (findtrait.TypeKind == TypeKind.Any)
                            {
                                //                        var need_loadrightvalue = (ASMethod m) =>
                                //                        {

                                //	AS3Function function = compileEnv.CompileContext.dict_method_as3function.First((kv) => kv.Value == m).Key;
                                //                            //return !string.IsNullOrEmpty(function.Name) && function.IsAnonymous && m.Name.StartsWith( function.Name + "#anonymous:");

                                //                            return m.Body == compileEnv.Scope.Container // 直接递归调用!

                                //	|| !m.IsAnonymous ;  //直接返回命名函数！

                                //};


                                if (findtrait.Value != null && findtrait.Value.ValueType == ASTrait.TraitValueType.AS3Function

                                    //                           && 
                                    //                           !
                                    //                           ( //检查闭包返回,如这种变态代码: 这时比如走LoadRightValue
                                    ///*
                                    //                            function makeFact() {
                                    //                               return function f(n) {
                                    //                                   return n == 0 ? 1 : n * f(n - 1);
                                    //                               };
                                    //                           }

                                    //                           var fact = makeFact();
                                    //                           assert(fact(5) == 120);

                                    //                           ---
                                    //                           function outer() {
                                    //                               function inner() { return 42; }
                                    //                               return function() { return inner(); };
                                    //                           }


                                    //                            */
                                    //need_loadrightvalue(compileEnv.CompileContext.dict_method_as3function[(AS3Function)findtrait.Value._value])
                                    //							)
                                    )
                                {
                                    //function a(){}; a();
                                    //this是定义function的代码的this,可能是instance,class,global

                                    var method = compileEnv.CompileContext.dict_method_as3function[(AS3Function)findtrait.Value._value];

                                    List<StackLocater> arguments = new List<StackLocater>();
                                    if (step.Arg3 != null)
                                    {
                                        //读取传递的参数
                                        var args = (List<AS3DataStackElement>)step.Arg3.Data.Value;
                                        for (int i = 0; i < args.Count; i++)
                                        {
                                            arguments.Add(ExpressionIL.LoadRightValue(args[i], compileEnv, step.token));
                                        }
                                    }

                                    CheckMethodArgs(arguments, compileEnv, step, method);


                                    int method_id = compileEnv.AddConstMethod(method);

                                    //INS_Ld_Function ld_Function = new INS_Ld_Function(method.Token);
                                    //ld_Function.const_index = method_id;
                                    //ld_Function.heapLocater = scopeHeapLocater;
                                    //ld_Function.dst = compileEnv.MakeStackLocater(TypeKind.Function);

                                    //compileEnv.instructions.Add(ld_Function);



                                    //INS_bindGlobal_Call bindGlobal_Call = new INS_bindGlobal_Call(step.token);
                                    //bindGlobal_Call.args = arguments.ToArray();
                                    //bindGlobal_Call.dst = compileEnv.GetStackLocater(step.Arg1.Reg, method.ReturnTypeKind);
                                    //bindGlobal_Call.function = ld_Function.dst;

                                    //compileEnv.SetCallResult(bindGlobal_Call.dst);
                                    //compileEnv.instructions.Add(bindGlobal_Call);

                                    INS_Ld_Function_BindGlobal_Call ld_Function_BindGlobal_Call = new INS_Ld_Function_BindGlobal_Call(method.Token);
                                    ld_Function_BindGlobal_Call.const_index = method_id;
                                    ld_Function_BindGlobal_Call.heapLocater = scopeHeapLocater;
                                    ld_Function_BindGlobal_Call.args = arguments.ToArray();
                                    ld_Function_BindGlobal_Call.dst = compileEnv.GetStackLocater(step.Arg1.Reg, method.ReturnTypeKind);

                                    compileEnv.SetCallResult(ld_Function_BindGlobal_Call.dst);
                                    compileEnv.instructions.Add(ld_Function_BindGlobal_Call);



                                }
                                else
                                {
                                    //var a:*; a();
                                    //function的this 是变量a 所在global。
                                    StackLocater ret = compileEnv.GetStackLocater(step.Arg1.Reg);

                                    StackLocater function = ExpressionIL.LoadRightValue(step.Arg2, compileEnv, step.token);

                                    List<StackLocater> argements = new List<StackLocater>();
                                    if (step.Arg3 != null)
                                    {
                                        //读取传递的参数
                                        var args = (List<AS3DataStackElement>)step.Arg3.Data.Value;
                                        for (int i = 0; i < args.Count; i++)
                                        {
                                            argements.Add(ExpressionIL.LoadRightValue(args[i], compileEnv, step.token));
                                        }
                                    }

                                    INS_bindGlobal_Call bindGlobal_Call = new INS_bindGlobal_Call(step.token);
                                    bindGlobal_Call.args = argements.ToArray();
                                    bindGlobal_Call.dst = ret;
                                    bindGlobal_Call.function = function;

                                    compileEnv.SetCallResult(ret);
                                    compileEnv.instructions.Add(bindGlobal_Call);
                                    //throw new NotImplementedException();
                                }
                            }
                            else
                            {
                                throw new ResolverException(step.token, "value is not a function.");
                            }

                        }

                        break;
                    case ExpressionIL.FindIdResultType.VTableItems:

                        if (out_vtableItems.Length == 1)
                        {
                            var method = out_vtableItems[0].Trait.Method;

                            List<StackLocater> arguments = new List<StackLocater>();
                            if (step.Arg3 != null)
                            {
                                //读取传递的参数
                                var args = (List<AS3DataStackElement>)step.Arg3.Data.Value;
                                for (int i = 0; i < args.Count; i++)
                                {
                                    arguments.Add(ExpressionIL.LoadRightValue(args[i], compileEnv, step.token));
                                }
                            }

                            CheckMethodArgs(arguments, compileEnv, step, method);

                            //bool interRest = false;
                            //{
                            //    int i = 0;
                            //    for (; i < arguments.Count; i++)
                            //    {
                            //        if (i < method.Parameters.Count)
                            //        {
                            //            var p = method.Parameters[i];
                            //            if (p.IsRest)
                            //            {
                            //                interRest = true;
                            //            }
                            //            if (!interRest)
                            //            {
                            //                var arg = ExpressionIL.TestTypeConvert(compileEnv, arguments[i], new CompileTypeKind() { Maj = p.TypeKind }, step.token);
                            //                arguments[i] = arg;
                            //            }
                            //        }
                            //        else
                            //        {
                            //            if (interRest) //进入不固定长度参数
                            //            {

                            //            }
                            //            else //参数过多抛出异常
                            //            {
                            //                throw new ResolverException(step.token, $"Incorrect number of arguments.  Expected no more than {method.Parameters.Count}");
                            //            }
                            //        }
                            //    }

                            //    if (i < method.Parameters.Count)
                            //    {
                            //        var p = method.Parameters[i];
                            //        if (!p.IsOptional && !p.IsRest)
                            //        {
                            //            throw new ResolverException(step.token,
                            //                $"Incorrect number of arguments.  Expected {method.Parameters.Count(p => !p.IsOptional && !p.IsRest)}"
                            //                );
                            //        }
                            //    }
                            //}




                            StackLocater ret = compileEnv.GetStackLocater(step.Arg1.Reg, method.ReturnTypeKind);


                            StackLocater fun = ExpressionIL.LoadRightValue(step.Arg2, compileEnv, step.token);


                            INS_Method_Call method_Call = new INS_Method_Call(step.token);
                            method_Call.dst = ret;
                            method_Call.function = fun;
                            method_Call.args = arguments.ToArray();

                            compileEnv.SetCallResult(ret);
                            compileEnv.instructions.Add(method_Call);


                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                        break;
                    case ExpressionIL.FindIdResultType.NeedLoadClass_ScopeMember:

                        //ASClass @class = (ASClass)findscope.Container;

                        //var stackLoc = compileEnv.MakeStackLocater(TypeKind.Class, (TypeKind)@class.Type_identifier);
                        //INS_Ld_Class ld_Class = new INS_Ld_Class(step.token);
                        //ld_Class.stack = stackLoc;
                        //ld_Class.classid_index = compileEnv.AddConstClassId(@class);

                        //compileEnv.instructions.Add(ld_Class);

                        result = ExpressionIL.FindIdResultType.ScopeMember;

                        goto lbl_loadclass;
                    case ExpressionIL.FindIdResultType.NeedLoadClass_VTableItems:

                        result = ExpressionIL.FindIdResultType.VTableItems;

                        goto lbl_loadclass;
                    default:
                        throw new NotImplementedException();
                        break;
                }


            }
            else if (!step.Arg2.IsReg && step.Arg2.Data.FF1Type == FF1DataValueType.this_pointer)
            {
                //this();  这种代码
                StackLocater function = ExpressionIL.LoadRightValue(step.Arg2, compileEnv, step.token);
                StackLocater ret = compileEnv.GetStackLocater(step.Arg1.Reg);
                List<StackLocater> arguments = new List<StackLocater>();
                if (step.Arg3 != null)
                {
                    //读取传递的参数
                    var args = (List<AS3DataStackElement>)step.Arg3.Data.Value;
                    for (int i = 0; i < args.Count; i++)
                    {
                        arguments.Add(ExpressionIL.LoadRightValue(args[i], compileEnv, step.token));
                    }
                }

                INS_bindGlobal_Call bindGlobal_Call = new INS_bindGlobal_Call(step.token);
                bindGlobal_Call.args = arguments.ToArray();
                bindGlobal_Call.dst = ret;
                bindGlobal_Call.function = function;

                compileEnv.SetCallResult(ret);
                compileEnv.instructions.Add(bindGlobal_Call);


            }
            else if (step.Arg2.IsReg)
            {
                StackLocater function = compileEnv.GetStackLocater(step.Arg2.Reg);
            retry:

                if (compileEnv.IsCallResult(function))
                {
                    //function = ExpressionIL.LoadRightValue(step.Arg2, compileEnv, step.token); //尝试从property中读.
                    /*
					 类似如下代码：
					class C extends A
                    {
	                    public function M():int
	                    {
		                    return function ():void 
		                    {
			                    o = null;
		                    }
	                    }
                    }

                    var o:C = new C();
                    o.M()(); 

					 */
                    //function的this 是变量a 所在global
                    StackLocater ret = compileEnv.GetStackLocater(step.Arg1.Reg);
                    List<StackLocater> arguments = new List<StackLocater>();
                    if (step.Arg3 != null)
                    {
                        //读取传递的参数
                        var args = (List<AS3DataStackElement>)step.Arg3.Data.Value;
                        for (int i = 0; i < args.Count; i++)
                        {
                            arguments.Add(ExpressionIL.LoadRightValue(args[i], compileEnv, step.token));
                        }
                    }

                    INS_bindGlobal_Call bindGlobal_Call = new INS_bindGlobal_Call(step.token);
                    bindGlobal_Call.args = arguments.ToArray();
                    bindGlobal_Call.dst = ret;
                    bindGlobal_Call.function = function;

                    compileEnv.SetCallResult(ret);
                    compileEnv.instructions.Add(bindGlobal_Call);

                }
                else
                {
                    var ctype = compileEnv.ReadStackType(function);

                    switch (ctype.Maj)
                    {
                        case TypeKind.Any:
                            {
                                var trait = compileEnv.ReadTraitRef(step.Arg2.Reg);
                                if (trait.Item1.Length == 1)
                                {
                                    //function的this 是变量a 所在global
                                    StackLocater ret = compileEnv.GetStackLocater(step.Arg1.Reg);
                                    List<StackLocater> arguments = new List<StackLocater>();
                                    if (step.Arg3 != null)
                                    {
                                        //读取传递的参数
                                        var args = (List<AS3DataStackElement>)step.Arg3.Data.Value;
                                        for (int i = 0; i < args.Count; i++)
                                        {
                                            arguments.Add(ExpressionIL.LoadRightValue(args[i], compileEnv, step.token));
                                        }
                                    }

                                    INS_bindGlobal_Call bindGlobal_Call = new INS_bindGlobal_Call(step.token);
                                    bindGlobal_Call.args = arguments.ToArray();
                                    bindGlobal_Call.dst = ret;
                                    bindGlobal_Call.function = function;

                                    compileEnv.SetCallResult(ret);
                                    compileEnv.instructions.Add(bindGlobal_Call);

                                }
                                else
                                {
                                    throw new InvalidOperationException();
                                }
                            }
                            break;
                        case TypeKind.Boolean:
                        case TypeKind.SByte:
                        case TypeKind.Byte:
                        case TypeKind.Short:
                        case TypeKind.UShort:
                        case TypeKind.Int:
                        case TypeKind.Uint:
                        case TypeKind.Float:
                        case TypeKind.Number:
                        case TypeKind.String:
                        case TypeKind.Fun_Void:
                            throw new ResolverException(step.token, "value is not a function.");
                            //throw new InvalidOperationException();
                            break;
                        case TypeKind.TraitDataReference:
                            {
                                function = ExpressionIL.LoadRightValue(step.Arg2, compileEnv, step.token); //尝试从property中读.
                                if (compileEnv.IsCallResult(function))
                                    goto retry;

                                var t = compileEnv.ReadTraitRef(step.Arg2.Reg);
                                if (t.Item1[0].TypeKind != TypeKind.Any && t.Item1[0].TypeKind != TypeKind.Function)
                                {
                                    throw new ResolverException(step.token, $"{t.Item1[0].QName.Name} is not a function.");
                                }
                                var bindthis = compileEnv.ReadRefBindInstance(step.Arg2.Reg);
                                //StackLocater fun = compileEnv.GetStackLocater(step.Arg2.Reg);
                                StackLocater fun = ExpressionIL.LoadRightValue(step.Arg2, compileEnv, step.token);
                                var result = compileEnv.GetStackLocater(step.Arg1.Reg);



                                List<StackLocater> argements = new List<StackLocater>();
                                if (step.Arg3 != null)
                                {
                                    //读取传递的参数
                                    var args = (List<AS3DataStackElement>)step.Arg3.Data.Value;
                                    for (int i = 0; i < args.Count; i++)
                                    {
                                        argements.Add(ExpressionIL.LoadRightValue(args[i], compileEnv, step.token));
                                    }
                                }

                                if (
                                    !(expression.exprStepList.SkipWhile(o => o.Type == OpType.Load && o.OpCode == "Ld_R").First().Type == OpType.Access) //排除如 Main.XXX() 这样的访问形式

                                    &&
                                    bindthis.index >= 0 && compileEnv.ReadStackType(bindthis).Maj == TypeKind.Class)
                                {
                                    INS_bindGlobal_Call bindGlobal_Call = new INS_bindGlobal_Call(step.token);
                                    bindGlobal_Call.args = argements.ToArray();
                                    bindGlobal_Call.dst = result;
                                    bindGlobal_Call.function = fun;

                                    compileEnv.SetCallResult(result);
                                    compileEnv.instructions.Add(bindGlobal_Call);
                                }
                                else
                                {
                                    INS_BindThis_Call bindThis_Call = new INS_BindThis_Call(step.token);
                                    bindThis_Call.dst = result;
                                    bindThis_Call.function = fun;
                                    bindThis_Call._this_ = bindthis;
                                    bindThis_Call.args = argements.ToArray();

                                    compileEnv.SetCallResult(result);
                                    compileEnv.instructions.Add(bindThis_Call);
                                }
                                //throw new NotImplementedException();

                            }
                            break;
                        case TypeKind.RTQName_MultiName_DataReference:
                            {
                                var bindthis = compileEnv.ReadRefBindInstance(step.Arg2.Reg);
                                //StackLocater fun = compileEnv.GetStackLocater(step.Arg2.Reg);
                                StackLocater fun = ExpressionIL.LoadRightValue(step.Arg2, compileEnv, step.token);
                                var result = compileEnv.GetStackLocater(step.Arg1.Reg);

                                List<StackLocater> arguments = new List<StackLocater>();
                                if (step.Arg3 != null)
                                {
                                    //读取传递的参数
                                    var args = (List<AS3DataStackElement>)step.Arg3.Data.Value;
                                    for (int i = 0; i < args.Count; i++)
                                    {
                                        arguments.Add(ExpressionIL.LoadRightValue(args[i], compileEnv, step.token));
                                    }
                                }


                                if (
                                    !(expression.exprStepList.SkipWhile(o => o.Type == OpType.Load && o.OpCode == "Ld_R").First().Type == OpType.Access)  //排除如 Main.XXX() 这样的访问形式
                                    &&
                                    bindthis.index >= 0 && compileEnv.ReadStackType(bindthis).Maj == TypeKind.Class)
                                {
                                    INS_bindGlobal_Call bindGlobal_Call = new INS_bindGlobal_Call(step.token);
                                    bindGlobal_Call.args = arguments.ToArray();
                                    bindGlobal_Call.dst = result;
                                    bindGlobal_Call.function = fun;

                                    compileEnv.SetCallResult(result);
                                    compileEnv.instructions.Add(bindGlobal_Call);
                                }
                                else
                                {
                                    if (bindthis.index >= 0 && compileEnv.ReadStackType(bindthis).Maj == TypeKind.Class)
                                    {
                                        //   public function Main()
                                        //   {
                                        //                                      
                                        //      Main["F"]();

                                        //   }

                                        // public static var F:Function = function() :void 
                                        //{
                                        //o = this;

                                        //}
                                        //假设有这种代码，这种情况下this是instance,实测，所以非常蛋疼,按道理应该是Class才对。

                                        INS_Ld_this ld_This = new INS_Ld_this(step.token);
                                        ld_This.dst = compileEnv.MakeStackLocater(TypeKind.Any); //new StackLocater();
                                        compileEnv.instructions.Add(ld_This);

                                        INS_BindThis_Call bindThis_Call = new INS_BindThis_Call(step.token);
                                        bindThis_Call.dst = result;
                                        bindThis_Call.function = fun;
                                        bindThis_Call._this_ = ld_This.dst; 
                                        bindThis_Call.args = arguments.ToArray();

                                        compileEnv.SetCallResult(result);
                                        compileEnv.instructions.Add(bindThis_Call);
                                    }
                                    else
                                    {
                                        INS_BindThis_Call bindThis_Call = new INS_BindThis_Call(step.token);
                                        bindThis_Call.dst = result;
                                        bindThis_Call.function = fun;
                                        bindThis_Call._this_ = bindthis;
                                        bindThis_Call.args = arguments.ToArray();

                                        compileEnv.SetCallResult(result);
                                        compileEnv.instructions.Add(bindThis_Call);
                                    }

                                }
                            }

                            break;

                        case TypeKind.Function:
                            {
                                //method
                                var bind = compileEnv.ReadTraitRef(step.Arg2.Reg);
                                if (bind.Item1.Length == 1)
                                {
                                    var method = bind.Item1[0].Method;
                                    if (method == null)
                                    {
                                        var trait = bind.Item1[0];

                                        if (trait.Kind == TraitKind.Slot && trait.Value != null && trait.Value.ValueType == ASTrait.TraitValueType.AS3Function)
                                        {
                                            method = compileEnv.CompileContext.dict_method_as3function[(AS3Function)trait.Value._value];
                                        }
                                        else if (
                                            trait.Kind == TraitKind.Slot && trait.Value != null && trait.Value.ValueType == ASTrait.TraitValueType.AS3Expression
                                            &&
                                            !((AS3Expression)trait.Value._value).Value.IsReg
                                            &&
                                            ((AS3Expression)trait.Value._value).Value.Data.Value is AS3Function

                                            )
                                        {
                                            method = compileEnv.CompileContext.dict_method_as3function[(AS3Function)((AS3Expression)trait.Value._value).Value.Data.Value];
                                        }



                                    }
                                    if (method == null)
                                    {
                                        throw new InvalidOperationException();
                                    }

                                    List<StackLocater> arguments = new List<StackLocater>();
                                    if (step.Arg3 != null)
                                    {
                                        //读取传递的参数
                                        var args = (List<AS3DataStackElement>)step.Arg3.Data.Value;
                                        for (int i = 0; i < args.Count; i++)
                                        {
                                            arguments.Add(ExpressionIL.LoadRightValue(args[i], compileEnv, step.token));
                                        }
                                    }

                                    CheckMethodArgs(arguments, compileEnv, step, method);

                                    {
                                        string key = Player.GetMethodKey(method);
                                        if (key == "__AS3__.vec$Vector@every" || key == "__AS3__.vec$Vector@filter")
                                        {
                                            //检查传入的function的签名
                                            var cb = ((List<AS3DataStackElement>)step.Arg3.Data.Value)[0];
                                            if (!cb.IsReg )
                                            {
                                                if (cb.Data.FF1Type == FF1DataValueType.as3_function)
                                                {
                                                    var cbmethod = compileEnv.CompileContext.dict_method_as3function[((AS3Function)cb.Data.Value)];
                                                    if (cbmethod.ReturnTypeKind != TypeKind.Boolean)
                                                    {
                                                        throw new ResolverException(step.token, "callback must return Boolean.");
                                                    }
                                                }
                                                else
                                                {
                                                    var stack = arguments[0];
                                                    var ins = compileEnv.instructions.LastOrDefault(i => i.INS_Code == INS_Code.ld_function && ((INS_Ld_Function)i).dst.index == stack.index);
                                                    if (ins != null)
                                                    {
                                                        var m = compileEnv.Constants[((INS_Ld_Function)ins).const_index];
														int p = m.HeapPtr;
                                                        Debug.Assert(p >> 24 == (byte)ASMethodBody.PoolHeapPtrKind.Method);
														{
															RtHeapInstance heapInstance = compileEnv.CompileContext.player_for_compiler.Context.GC.Heap[p & 0xffffff];
                                                            Debug.Assert(heapInstance.TypeKind == RtHeapTypeKind.MethodScope);

                                                            var cbmethod = ((ASMethodBody)heapInstance.Type).Method;
															if (cbmethod.ReturnTypeKind != TypeKind.Boolean)
															{
																throw new ResolverException(step.token, "callback must return Boolean.");
															}
														}
													}

                                                }

                                            }
                                            
                                        }

                                    }


									StackLocater fun = compileEnv.GetStackLocater(step.Arg2.Reg);
                                    //call method
                                    var result = compileEnv.GetStackLocater(step.Arg1.Reg, method.ReturnTypeKind);


                                    if (method.__ismethod)
                                    {
                                        INS_Method_Call method_Call = new INS_Method_Call(step.token);
                                        method_Call.dst = result;
                                        method_Call.function = fun;
                                        method_Call.args = arguments.ToArray();

                                        compileEnv.SetCallResult(result);
                                        compileEnv.instructions.Add(method_Call);
                                    }
                                    else
                                    {
                                        INS_bindGlobal_Call bindGlobal_Call = new INS_bindGlobal_Call(step.token);
                                        bindGlobal_Call.args = arguments.ToArray();
                                        bindGlobal_Call.dst = result;
                                        bindGlobal_Call.function = fun;

                                        compileEnv.SetCallResult(result);
                                        compileEnv.instructions.Add(bindGlobal_Call);
                                    }


                                }
                                else
                                {
                                    throw new InvalidOperationException();
                                }
                            }
                            break;
                        case TypeKind.CParseNS_Traits:
                            throw new InvalidOperationException();
                            break;
                        case TypeKind.RTQNameRTQNameL_N:
                            {
                                throw new InvalidOperationException();
                                //var bindthis = compileEnv.ReadRefBindInstance(step.Arg2.Reg);
                            }
                            break;
                        case TypeKind.SearchNameSpaceFromImports:
                            throw new InvalidOperationException();
                            break;
                        case TypeKind.Class:
                            if (step.Arg2.Reg.isLd_callee_id)
                            {
                                var cls = ExpressionIL.FindClassById(compileEnv, (ulong)ctype.Mir);

                                if (cls == compileEnv.CompileContext.player_for_compiler.Context.ARRAY
                                    )
                                {
                                    step.OpCode = "new";

                                    //转为调Array的构造函数。
                                    new ConstructorBuilder().Build(step, compileEnv);


                                }
                                else if (
                                    Extensions.IsExtend(cls.Instance, compileEnv.CompileContext.player_for_compiler.Context.ERROR.Instance)
                                    )
                                {
                                    List<StackLocater> arguments = new List<StackLocater>();
                                    if (step.Arg3 != null)
                                    {
                                        //读取传递的参数
                                        var args = (List<AS3DataStackElement>)step.Arg3.Data.Value;
                                        for (int i = 0; i < args.Count; i++)
                                        {
                                            arguments.Add(ExpressionIL.LoadRightValue(args[i], compileEnv, step.token));
                                        }
                                    }

                                    if (arguments.Count != 1)
                                    {
                                        throw new ResolverException(step.token, "Incorrect number of arguments.  Expected 1");
                                    }

                                    StackLocater ret = compileEnv.GetStackLocater(step.Arg1.Reg);

                                    INS_bindGlobal_Call _call = new INS_bindGlobal_Call(step.token);
                                    _call.dst = ret;
                                    _call.function = function;
                                    _call.args = arguments.ToArray();

                                    compileEnv.SetCallResult(ret);
                                    compileEnv.instructions.Add(_call);

                                }
                                else
                                {
                                    //强制类型转换
                                    List<StackLocater> arguments = new List<StackLocater>();
                                    if (step.Arg3 != null)
                                    {
                                        //读取传递的参数
                                        var args = (List<AS3DataStackElement>)step.Arg3.Data.Value;
                                        for (int i = 0; i < args.Count; i++)
                                        {
                                            arguments.Add(ExpressionIL.LoadRightValue(args[i], compileEnv, step.token));
                                        }
                                    }

                                    if (arguments.Count != 1)
                                    {
                                        throw new ResolverException(step.token, "Incorrect number of arguments.  Expected 1");
                                    }

                                    var class_id = compileEnv.AddConstClassId(cls);

                                    StackLocater ret = compileEnv.GetStackLocater(step.Arg1.Reg, (TypeKind)cls.Type_identifier);
                                    INS_TypeCast typeCast = new INS_TypeCast(step.token);
                                    typeCast.dst = ret;
                                    typeCast.value = arguments[0];
                                    typeCast.class_id = class_id;

                                    compileEnv.instructions.Add(typeCast);

                                }
                            }
                            else
                            {
                                throw new InvalidOperationException();
                            }
                            break;
                        case TypeKind.Unknown:
                        case TypeKind.Null:
                        case TypeKind.Object:
                        case TypeKind.Array:
                        case TypeKind.Vector:
                        case TypeKind.Namespace:
                        default:
                            throw new InvalidOperationException();
                            break;
                    }
                }
            }
            else
            {
                //几乎不可能运行时成功，先编过

				StackLocater function = ExpressionIL.LoadRightValue(step.Arg2, compileEnv, step.token);
				StackLocater ret = compileEnv.GetStackLocater(step.Arg1.Reg);
				List<StackLocater> arguments = new List<StackLocater>();
				if (step.Arg3 != null)
				{
					//读取传递的参数
					var args = (List<AS3DataStackElement>)step.Arg3.Data.Value;
					for (int i = 0; i < args.Count; i++)
					{
						arguments.Add(ExpressionIL.LoadRightValue(args[i], compileEnv, step.token));
					}
				}

				INS_bindGlobal_Call bindGlobal_Call = new INS_bindGlobal_Call(step.token);
				bindGlobal_Call.args = arguments.ToArray();
				bindGlobal_Call.dst = ret;
				bindGlobal_Call.function = function;

				compileEnv.SetCallResult(ret);
				compileEnv.instructions.Add(bindGlobal_Call);


				//throw new NotImplementedException();
			}

        }
    }
}
