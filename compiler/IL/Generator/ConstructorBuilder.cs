using juicescript.ABC;
using juicescript.ABC.INS;
using juicescript.ABC.Locaters;
using juicescript.compiler.AST;
using juicescript.compiler.AST.Expr;
using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler.IL.Generator
{
    /// <summary>
    /// 编译OpType.Constructor
    /// </summary>
    internal class ConstructorBuilder
    {
        internal void Build(AS3ExprStep step, CompileEnv compileEnv)
        {
            if (step.OpCode != "new")
            {
                throw new InvalidOperationException();
            }

            //if (step.Arg2.IsReg && !step.Arg2.Reg.isLd_R)
            //{
            //    throw new NotImplementedException();
            //}
            //else
            {
                StackLocater type_locater = ExpressionIL.LoadRightValue(step.Arg2, compileEnv, step.token,null,step);
                var type = compileEnv.ReadStackType(type_locater);
                if (type.Maj == ABC.TypeKind.Class
                    ||
                    type.Maj == TypeKind.Vector

                    )
                {
                    if (type.Maj == TypeKind.Vector && ((AS3Vector)step.Arg2.Data.Value).isInitData && step.Arg3 != null && ((List<AS3DataStackElement>)step.Arg3.Data.Value).Count > 8)
                    {
                        var len = compileEnv.AddConstUInt((uint)((List<AS3DataStackElement>)step.Arg3.Data.Value).Count);
                        INS_Ld_Const ld_len = new INS_Ld_Const(step.token);
                        ld_len.dst = compileEnv.MakeStackLocater(TypeKind.Uint);
                        ld_len.const_index = len;
                        compileEnv.instructions.Add(ld_len);

						StackLocater dst = compileEnv.GetStackLocater(step.Arg1.Reg, type.Mir == TypeKind.Unknown ? TypeKind.Any : type.Mir);
						INS_New_Instance new_Instance = new INS_New_Instance(step.token);
						new_Instance.dst = dst;
						new_Instance.typeLocator = type_locater;
                        new_Instance.args = new StackLocater[1] { ld_len.dst };
						compileEnv.instructions.Add(new_Instance);

						VectorDef vd = compileEnv.CompileContext.vectorDefs.First(v => v.Identifier == type.Mir);
						var args = (List<AS3DataStackElement>)step.Arg3.Data.Value;
						for (int i = 0; i < args.Count; i++)
						{
							var lv =(ExpressionIL.LoadRightValue(args[i], compileEnv, step.token));
							lv = ExpressionIL.TestTypeConvert(compileEnv, lv,
									new CompileTypeKind() { Maj = vd.buildVector.ElementType }, step.token);

                            INS_Array_Vector_InitElement arrayInitElement = new INS_Array_Vector_InitElement(step.token);
                            arrayInitElement.instance = dst;
                            arrayInitElement.index = i;
                            arrayInitElement.dst = lv; 

                            compileEnv.instructions.Add(arrayInitElement);

						}

					}
					else
                    {

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

                        bool isVectorInitData = false;

                        if (type.Maj == TypeKind.Class)
                        {
                            if (type.Mir != TypeKind.Unknown)
                            {
                                ASClass @class = ExpressionIL.FindClassById(compileEnv, (ulong)type.Mir);
                                if (@class.Instance.IsInterface)
                                {
                                    throw new ResolverException(
                                            step.token,
                                            @class.QName.Name + " Interfaces cannot be instantiated with the new operator.");
                                }

                                var ctor = @class.Instance.Constructor.Body.Method;

                                CallFuncBuilder.CheckMethodArgs(arguments, compileEnv, step, ctor);

                            }
                        }
                        else
                        {
                            if (((AS3Vector)step.Arg2.Data.Value).isInitData)
                            {
                                isVectorInitData = true;
                            }
                            else
                            {
                                VectorDef vd = compileEnv.CompileContext.vectorDefs.First(v => v.Identifier == type.Mir);
                                var ctor = vd.buildVector.vector_class.Instance.Constructor.Body.Method;
                                CallFuncBuilder.CheckMethodArgs(arguments, compileEnv, step, ctor);
                            }

                        }


                        //type.Mir == Unkown会在如下代码里出现：
                        //var t:Class = test(null);
                        //j = (new t()).Tsss();

                        StackLocater dst = compileEnv.GetStackLocater(step.Arg1.Reg, type.Mir == TypeKind.Unknown ? TypeKind.Any : type.Mir);

                        if (isVectorInitData == true)
                        {
                            VectorDef vd = compileEnv.CompileContext.vectorDefs.First(v => v.Identifier == type.Mir);
                            for (int i = 0; i < arguments.Count; i++)
                            {
                                var arg = ExpressionIL.TestTypeConvert(compileEnv, arguments[i],
                                    new CompileTypeKind() { Maj = vd.buildVector.ElementType }, step.token);

                            }

                            arguments.Insert(0, dst); //构造特殊参数,头两个参数绕过原本的两个参数
                            arguments.Insert(0, dst);
                        }

                        INS_New_Instance new_Instance = new INS_New_Instance(step.token);
                        new_Instance.dst = dst;
                        new_Instance.typeLocator = type_locater;
                        new_Instance.args = arguments.ToArray();


                        compileEnv.instructions.Add(new_Instance);

                    }
                }

                //           else if (type.Maj == TypeKind.Function)
                //           {
                //               //var c = new (function(){
                //               //this.bb = 555;
                //               //})();
                //               //形如这样的代码
                //               ASMethod method = compileEnv.CompileContext.dict_method_as3function[(AS3Function)step.Arg2.Data.Value];

                //List<StackLocater> arguments = new List<StackLocater>();
                //if (step.Arg3 != null)
                //{
                //	//读取传递的参数
                //	var args = (List<AS3DataStackElement>)step.Arg3.Data.Value;
                //	for (int i = 0; i < args.Count; i++)
                //	{
                //		arguments.Add(ExpressionIL.LoadRightValue(args[i], compileEnv, step.token));
                //	}
                //}

                //int method_id = compileEnv.AddConstMethod(method);




                //throw new NotImplementedException();
                //           }
                else if (type.Maj == TypeKind.Any || type.Maj == TypeKind.Function)
                {
                    if (type.Maj == TypeKind.Function && step.Arg2.IsReg)
                    {
                        Tuple<ASTrait[], AS3ExprStep> tref;
                        if (compileEnv.TryReadTraitRef(step.Arg2.Reg, out tref))
                        {
                            if (tref.Item1.Length > 0 && tref.Item1[0] !=null && tref.Item1[0].Kind == TraitKind.Method && tref.Item1[0].Method.__ismethod)
                            {
                                throw new ResolverException(step.token, "Method cannot be used as a constructor.");
                            }
                        }
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

                    StackLocater dst = compileEnv.GetStackLocater(step.Arg1.Reg, type.Mir == TypeKind.Unknown ? TypeKind.Any : type.Mir);

                    INS_New_Instance new_Instance = new INS_New_Instance(step.token);
                    new_Instance.dst = dst;
                    new_Instance.typeLocator = type_locater;
                    new_Instance.args = arguments.ToArray();

                    compileEnv.instructions.Add(new_Instance);

                }
                else if (type.Maj == TypeKind.Boolean || type.Maj.IsNumericType() || type.Maj == TypeKind.Null)
                {
                    throw new ResolverException(step.token, "Instantiation attempted on a non-constructor."); // 编译期检查
                }
                else if (type.Maj >= TypeKind.Object)
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

					StackLocater dst = compileEnv.GetStackLocater(step.Arg1.Reg, type.Mir == TypeKind.Unknown ? TypeKind.Any : type.Mir);

					INS_New_Instance new_Instance = new INS_New_Instance(step.token);
					new_Instance.dst = dst;
					new_Instance.typeLocator = type_locater;
					new_Instance.args = arguments.ToArray();

					compileEnv.instructions.Add(new_Instance);
				}
                else
                {
                    throw new InvalidOperationException();
                }
			}

        }
    }
}
