using juicescript.ABC;
using juicescript.ABC.INS;
using juicescript.ABC.Locaters;
using juicescript.compiler.AST;
using juicescript.compiler.AST.Expr;
using juicescript.compiler.AST.Stmt;
using juicescript.compiler.IL.Generator;
using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Emit;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace juicescript.compiler.IL
{
    internal class ILBuilder
    {
        public static void Build(CompileEnv compileEnv)
        {
            //实例化内部定义的命名空间
			var nsScope = compileEnv.Scope;
			if (compileEnv.Scope.Kind == CodeScopeKind.Method && compileEnv.Scope.Parent.Kind == CodeScopeKind.Script)
			{ 
				nsScope = nsScope.Parent;
			}
            for (int j = 0; j < nsScope.Members.Count; j++)
            {
                var m = nsScope.Members[j];
                if (m.Kind == ScopeMemberKind.Constant && m.trait.ValueKind == ConstantKind.Namespace)
                {
                    var nsid = compileEnv.AddConstNamespaceId(m.trait.Value.Namespace);
                    ScopeHeapLocater heapLocater = new ScopeHeapLocater();

                    if (nsScope.index > 65535)
                    {
                        throw new ParseException("scope count > 65535");
                    }
                    if (j > 65535)
                    {
                        throw new ParseException("scope members count > 65535");
                    }

                    heapLocater.ScopeIndex = (ushort)nsScope.index;
                    heapLocater.MemberIndex = (ushort)j;

                    INS_Ld_Namespace ld_Namespace = new INS_Ld_Namespace(m.trait.Token);
                    ld_Namespace.namespace_index = nsid;
                    ld_Namespace.dst = compileEnv.MakeStackLocater(TypeKind.Namespace);
                    compileEnv.instructions.Add(ld_Namespace);


                    INS_Store_ScopeHeap store_ScopeHeap = new INS_Store_ScopeHeap(m.trait.Token);
                    store_ScopeHeap.dst = ld_Namespace.dst;
                    store_ScopeHeap.heap = heapLocater;
                    compileEnv.instructions.Add(store_ScopeHeap);
                }
            }

			//保存当前的trycatch块状态
			Stack<TryCatchContext> trycatchState = new Stack<TryCatchContext>();
			Stack<ContextBase> blockcontexts = new Stack<ContextBase>();
			int flagseed = 1;

			for (int i = 0; i < compileEnv.Codes.Count; i++)
			{
				try
				{
					compileEnv.stack_loaded_heapunit.Push(new Dictionary<object, StackLocater>());

					compileEnv.stack_trycatchctx.Push(trycatchState);

					AST.IAS3SyntaxNode code = compileEnv.Codes[i];
					BuildAS3SyntaxNode(compileEnv, code, trycatchState, blockcontexts, ref flagseed);
				}
				finally
				{
					compileEnv.stack_trycatchctx.Pop();

					compileEnv.stack_loaded_heapunit.Pop();
				}

            }

			#region MethodFlags

			if (trycatchState.Count > 0) // trycatch状态肯定为空
			{
				throw new InvalidOperationException();
			}
			
            if (compileEnv.instructions.Count > 0)
            {
                compileEnv.instructions.Add(new INS_END());
            }

			//确认method是否引用其他method中的 参数或者变量。

			var check = compileEnv.Scope.Parent;
			while (check != null)
			{
				if (check.Kind != CodeScopeKind.Method)
				{
					break;
				}

				for (int i = 0; i < compileEnv.instructions.Count; i++)
				{
					bool flag = false;

					var instruction = compileEnv.instructions[i];
					if (instruction.INS_Code == INS_Code.ld_ScopeH)
					{
						INS_Ld_ScopeHeap ld_ScopeHeap = (INS_Ld_ScopeHeap)instruction;
						if (check.index == ld_ScopeHeap.heap.ScopeIndex)
						{
							flag = true;
						}
					}
					else if (instruction.INS_Code == INS_Code.storeScopeH)
					{
						INS_Store_ScopeHeap store_ScopeHeap = (INS_Store_ScopeHeap)instruction;
						if (check.index == store_ScopeHeap.heap.ScopeIndex)
						{
							flag = true;
						}
					}
					else if (instruction.INS_Code == INS_Code.ld_function)
					{
						INS_Ld_Function ld_Function = (INS_Ld_Function)instruction;
						if (check.index == ld_Function.heapLocater.ScopeIndex)
						{
							flag = true;
						}
					}
					else if (instruction.INS_Code == INS_Code.ld_function_bindglobal_call)
					{
						INS_Ld_Function_BindGlobal_Call ld_function_bindg_call = (INS_Ld_Function_BindGlobal_Call)instruction;
						if (check.index == ld_function_bindg_call.heapLocater.ScopeIndex)
						{
							flag = true;
						}
					}
					
					
					



					
					//仅有这2个之类能访问到method方法的scopemember。
					//那个用 stacklocator的负值找到的是 class,instance,global中的成员。

					if (flag)
					{
						((ASMethodBody)check.Container).Method.Flags |= MethodFlags.NeedActivation;
						break;
					}
				}

				check = check.Parent;	

			}

			#endregion


		}

		private static void BuildAS3SyntaxNode(CompileEnv compileEnv, IAS3SyntaxNode code, Stack<TryCatchContext> trycatchState, Stack<ContextBase> blockcontexts, ref int flagseed)
		{
			Action throwIfDuplicateLabel = () => {
				var group = (blockcontexts.Where(b => b is Block && !string.IsNullOrWhiteSpace(((Block)b).Label) &&
				((Block)b).token !=null //有token的才参与检查，因为 forin 结构 同一个label,需要 continue和break两个block。所以其中一个不设置token，可跳过这个检查
				).GroupBy(b => ((Block)b).Label));
				if (group != null)
				{
					if (group.Any(g => g.Count() > 1))
					{
						Token token = group.Where(g=>g.Count()>1).First().Select(g => ((Block)g).token).First();
						throw new ResolverException(token, "Duplicate label definition.");
					}
				}
			};


			if (code is AST.AS3Expression)
			{
				int count = compileEnv.instructions.Count;

				AS3Expression expression = (AS3Expression)code;

				bool flag = false;

				int initmember_index = -1;

				if (expression.exprStepList.Count > 0) //收集成员赋值语句。
				{
					var step = expression.exprStepList[expression.exprStepList.Count - 1];
					if (step.Type == OpType.Assigning && step.OpCode == "=")
					{
						if (!step.Arg1.IsReg && step.Arg1.Data.FF1Type == FF1DataValueType.identifier
							&&
							((string)step.Arg1.Data.Value).StartsWith("{")
							)
						{
							flag = true;
							initmember_index = compileEnv.initvalue_instructions.Count;
							compileEnv.initvalue_instructions.Add(
								new CompileEnv.MemberInitValue()
								{
									member = null,
									setValueInstructions = new List<Instruction>(),
									start_byte_pos = compileEnv.instructions.Sum(i => i.Size)
								}
							);
						}
					}
				}

				BuildExpression(compileEnv, expression, ref flagseed);

				//if (compileEnv.CompileContext.only_const_scriptinit.Count > 0)
				{
					if (flag)
					{
						if (initmember_index != compileEnv.initvalue_instructions.Count - 1)
							throw new InvalidOperationException();
						if (compileEnv.initvalue_instructions[initmember_index].member == null)
							throw new InvalidOperationException();

						compileEnv.initvalue_instructions[initmember_index].setValueInstructions.AddRange(compileEnv.instructions.Skip(count));
					}
					
				}

			}
			else if (code is AS3Throw)
			{
				AS3Throw @throw = (AS3Throw)code;
				StackLocater tothrow = BuildExpression(compileEnv, @throw.Expression, ref flagseed);
				INS_Throw iNS_Throw = new INS_Throw(@throw.Token);
				iNS_Throw.dst = tothrow;

				compileEnv.instructions.Add(iNS_Throw);

			}
			else if (code is AS3Try)
			{
				if (trycatchState.Count + 1 > runtime.Context.MAX_TRY_NESTED)
				{
					throw new ResolverException(code.Token, "Nesting Level of Try exceeds the limit :" + Context.MAX_TRY_NESTED);
				}

				AS3Try @try = (AS3Try)code;

				TryCatchContext tryCatchContext = new TryCatchContext(@try);
				//tryCatchContext.CatchList = new List<Tuple<Instruction, Instruction>>();
				trycatchState.Push(tryCatchContext);

				if (!string.IsNullOrEmpty(@try.label))
				{
					Block block = new Block();
					block.Label = @try.label;
					block.token = @try.label_token;

					block.BreakTarget = true;
					block.breakFlag = new INS_Flag(@try.finally_exit_token);
					block.breakFlag.flag_id = flagseed++;

					blockcontexts.Push(block);


					throwIfDuplicateLabel();
				}
				blockcontexts.Push(tryCatchContext);



				INS_Try_Enter try_Enter = new INS_Try_Enter(@try.try_enter_token);
				try_Enter.catch_pc = new int[@try.CatchList.Count];
				try_Enter.finally_pc = 0;
				try_Enter.dst = compileEnv.MakeStackLocater(TypeKind.Any);

				//tryCatchContext.Try_Enter = try_Enter;

				INS_Try_Exit try_Exit = new INS_Try_Exit(@try.try_exit_token);
				//tryCatchContext.Try_Exit = try_Exit;


				INS_Finally_Enter finally_Enter = new INS_Finally_Enter(@try.finally_enter_token);
				//tryCatchContext.Finally_Enter = finally_Enter;

				INS_Finally_Exit finally_Exit = new INS_Finally_Exit(@try.finally_exit_token);
				finally_Exit.HoldError = try_Enter.dst;

				//tryCatchContext.Finally_Exit = finally_Exit;


				tryCatchContext.state = TryCatchContext.State.Try;

				//try 块
				compileEnv.instructions.Add(try_Enter);
				for (int i = 0; i < @try.TryBlock.Count; i++)
				{
					BuildAS3SyntaxNode(compileEnv, @try.TryBlock[i], trycatchState, blockcontexts, ref flagseed);
				}
				compileEnv.instructions.Add(try_Exit);


				tryCatchContext.state = TryCatchContext.State.Catch;

				//catch块
				for (int i = 0; i < @try.CatchList.Count; i++)
				{
					var catch_block = @try.CatchList[i];
					var add_var = @try.CatchVarList[i];

					compileEnv.catching_variable.Push(add_var);

					ScopeHeapLocater heapLocater; TypeKind heaptype; ASTrait ex; CodeScope ex_scope; ScopeMember ex_member; VTableItem[] tableItem;
					var _RV = ExpressionIL.FindIdentifier(add_var.Name, null, compileEnv, add_var.Token, out heapLocater, out heaptype, out ex, out ex_scope, out ex_member, out tableItem);
					if (_RV != ExpressionIL.FindIdResultType.ScopeMember || ex_scope != compileEnv.Scope)
					{
						throw new InvalidOperationException();
					}



					INS_Catch_Enter catch_Enter = new INS_Catch_Enter(add_var.Token);
					//catch_Enter.catch_type = (ulong)heaptype;
					catch_Enter.catch_exception = heapLocater;

					INS_Catch_Exit catch_Exit = new INS_Catch_Exit(@try.catch_exit_tokens[i]);
					
					//tryCatchContext.CatchList.Add(new Tuple<Instruction, Instruction>( catch_Enter,catch_Exit));

					compileEnv.instructions.Add(catch_Enter);

					for (int j = 0; j < catch_block.Count; j++)
					{
						BuildAS3SyntaxNode(compileEnv, catch_block[j], trycatchState, blockcontexts, ref flagseed);
					}

					compileEnv.instructions.Add(catch_Exit);

					compileEnv.catching_variable.Pop();
				}


				tryCatchContext.state = TryCatchContext.State.Finally;
				//finally块			
				compileEnv.instructions.Add(finally_Enter);
				if (@try.FinallyBlock != null)
				{
					for (int i = 0; i < @try.FinallyBlock.Count; i++)
					{
						BuildAS3SyntaxNode(compileEnv, @try.FinallyBlock[i], trycatchState, blockcontexts, ref flagseed);
					}
				}
				compileEnv.instructions.Add(finally_Exit);


				blockcontexts.Pop();

				if (!string.IsNullOrEmpty(@try.label))
				{
					var block = (Block)blockcontexts.Pop();
					compileEnv.instructions.Add(block.breakFlag);
				}



				trycatchState.Pop();




			}
			else if (code is AS3Block)
			{
				AS3Block block = (AS3Block)code;
				if (!string.IsNullOrWhiteSpace(block.label))
				{
					Block blockctx = new Block();
					blockctx.Label = block.label;
					blockctx.token = block.label_token;

					blockctx.BreakTarget = true;
					blockctx.breakFlag = new INS_Flag(block.exit_token);
					blockctx.breakFlag.flag_id = flagseed++;

					blockcontexts.Push(blockctx);
					throwIfDuplicateLabel();
				}

				for (int i = 0; i < block.Code.Count; i++)
				{
					BuildAS3SyntaxNode(compileEnv, block.Code[i], trycatchState, blockcontexts, ref flagseed);
				}

				if (!string.IsNullOrWhiteSpace(block.label))
				{
					Block blockctx = (Block)blockcontexts.Pop();
					compileEnv.instructions.Add(blockctx.breakFlag);
				}


			}
			else if (code is AS3IF)
			{
				AS3IF @if = (AS3IF)code;


				var lastnode = @if.truepart.Union(@if.falsepart).Reverse().FirstOrDefault();

				if (!string.IsNullOrWhiteSpace(@if.label))
				{
					Block blockctx = new Block();
					blockctx.Label = @if.label;
					blockctx.token = @if.label_token;

					blockctx.BreakTarget = true;
					blockctx.breakFlag = new INS_Flag(null);
					blockctx.breakFlag.flag_id = flagseed++;

					blockcontexts.Push(blockctx);
					throwIfDuplicateLabel();
				}


				//for (int i = 0; i < @if.condition.Count; i++)
				//{
				//	BuildAS3SyntaxNode(compileEnv, @if.condition[i], trycatchState, blockcontexts, ref flagseed);
				//}

				//AS3Expression expr = (AS3Expression)@if.condition[@if.condition.Count - 1];
				//StackLocater condition = ExpressionIL.LoadRightValue(expr.Value, compileEnv, expr.Token);

				StackLocater condition = default;

				for (int i = 0; i < @if.condition.Count; i++)
				{
					condition = BuildExpression(compileEnv, (AS3Expression)@if.condition[i], ref flagseed);
				}


				bool truepart_blank = @if.truepart.Count == 0 || (@if.truepart.Count == 1 && @if.truepart[0] is AS3Block && ((AS3Block)@if.truepart[0]).Code.Count == 0);
				bool falsepart_blank = @if.falsepart.Count == 0 || (@if.falsepart.Count == 1 && @if.falsepart[0] is AS3Block && ((AS3Block)@if.falsepart[0]).Code.Count == 0);


				if (truepart_blank && falsepart_blank
					)
				{
					//么用
				}
				else if (!truepart_blank && !falsepart_blank)
				{
					//If_False_Goto flag_else
					//****
					//goto flag_end
					//flag_else
					//****
					//flag_end

					INS_Flag flag_else = new INS_Flag(null);
					flag_else.flag_id = flagseed++;

					INS_Flag flag_end = new INS_Flag(null);
					flag_end.flag_id = flagseed++;

					INS_If_False_Goto if_False_Goto = new INS_If_False_Goto(@if.Token);
					if_False_Goto.condition = condition;
					if_False_Goto.flag_id = flag_else.flag_id;
					compileEnv.instructions.Add(if_False_Goto);

					for (int i = 0; i < @if.truepart.Count; i++)
					{
						BuildAS3SyntaxNode(compileEnv, @if.truepart[i], trycatchState, blockcontexts, ref flagseed);
					}

					flag_else.token = compileEnv.instructions.Where(i => i.token != null).GroupBy(i => i.token.line).OrderByDescending(g => g.Key).First().OrderByDescending(i => i.token.ptr).First().token.nextToken;

					INS_Goto goto_end = new INS_Goto(flag_else.token);
					goto_end.jumpTrys = 0;
					goto_end.jumpOffset = 0;
					goto_end.flag_id = flag_end.flag_id;

					compileEnv.instructions.Add(goto_end);
					compileEnv.instructions.Add(flag_else);

					for (int i = 0; i < @if.falsepart.Count; i++)
					{
						BuildAS3SyntaxNode(compileEnv, @if.falsepart[i], trycatchState, blockcontexts, ref flagseed);
					}

					flag_end.token = compileEnv.instructions.Where(i => i.token != null).GroupBy(i => i.token.line).OrderByDescending(g => g.Key).First().OrderByDescending(i => i.token.ptr).First().token.nextToken;
					compileEnv.instructions.Add(flag_end);



				}
				else if (!truepart_blank && falsepart_blank)
				{
					//If_False_Goto flag
					//...
					//flag
					//此类指令不可能跨try

					INS_Flag flag = new INS_Flag(null);
					flag.flag_id = flagseed++;

					INS_If_False_Goto if_False_Goto = new INS_If_False_Goto(@if.Token);
					if_False_Goto.condition = condition;
					if_False_Goto.flag_id = flag.flag_id;

					compileEnv.instructions.Add(if_False_Goto);

					for (int i = 0; i < @if.truepart.Count; i++)
					{
						BuildAS3SyntaxNode(compileEnv, @if.truepart[i], trycatchState, blockcontexts, ref flagseed);
					}

					flag.token = compileEnv.instructions.Where(i => i.token != null).GroupBy(i => i.token.line).OrderByDescending(g => g.Key).First().OrderByDescending(i => i.token.ptr).First().token.nextToken;
					compileEnv.instructions.Add(flag);

				}
				else
				{
					//IF_True_Goto flag
					//***false_partc_code
					//flag
					//此类指令不可能跨try
					INS_Flag flag = new INS_Flag(null);
					flag.flag_id = flagseed++;

					INS_If_True_Goto if_true_Goto = new INS_If_True_Goto(@if.Token);
					if_true_Goto.condition = condition;
					if_true_Goto.flag_id = flag.flag_id;

					compileEnv.instructions.Add(if_true_Goto);

					for (int i = 0; i < @if.falsepart.Count; i++)
					{
						BuildAS3SyntaxNode(compileEnv, @if.falsepart[i], trycatchState, blockcontexts, ref flagseed);
					}

					flag.token = compileEnv.instructions.Where(i => i.token != null).GroupBy(i => i.token.line).OrderByDescending(g => g.Key).First().OrderByDescending(i => i.token.ptr).First().token.nextToken;
					compileEnv.instructions.Add(flag);

				}

				if (!string.IsNullOrWhiteSpace(@if.label))
				{
					Block blockctx = (Block)blockcontexts.Pop();
					blockctx.breakFlag.token = compileEnv.instructions.Where(i => i.token != null).GroupBy(i => i.token.line).OrderByDescending(g => g.Key).First().OrderByDescending(i => i.token.ptr).First().token.nextToken;
					compileEnv.instructions.Add(blockctx.breakFlag);
				}


			}
			else if (code is AS3YieldReturn)
			{
				AS3YieldReturn yieldReturn = (AS3YieldReturn)code;

				if (compileEnv.Scope.Kind == CodeScopeKind.Script)
				{
					throw new ResolverException(code.Token, "The yield statement cannot be used in global initialization code.");
				}
				else if (compileEnv.Scope.Kind == CodeScopeKind.Class || compileEnv.Scope.Kind == CodeScopeKind.Instance)
				{
					throw new InvalidOperationException();
				}

				var m = ((ASMethodBody)compileEnv.Scope.Container).Method;
				if (m.ReturnTypeKind != TypeKind.Any)
				{
					throw new ResolverException(m.Token, "generator must return *");
				}

				if (m.Flags.HasFlag(MethodFlags.ASYNC))
				{
					throw new ResolverException(m.Token, "The yield statement cannot be used in async code");
				}

				//yield 不能在 catch和finally块中发起。

				if (trycatchState.Any((t) => t.state == TryCatchContext.State.Catch || t.state == TryCatchContext.State.Finally))
				{
					throw new ResolverException(m.Token, "Can't yield in catch block or finally block");
				}

				//这个关系不大，只是仿照C#的限制。只要保证不在 catch,finally块里发动 yield即可。
				if (trycatchState.Any((t) => t.tryStatement != null && t.tryStatement.CatchList.Count > 0))
				{
					throw new ResolverException(m.Token, "Can't yield in [try_catch_finally] that have [catch] blocks");
				}



				((ASMethodBody)compileEnv.Scope.Container).Method.Flags |= MethodFlags.Generator;
				((ASMethodBody)compileEnv.Scope.Container).Method.Flags |= MethodFlags.NeedActivation; //构造器必然需要构造闭包。

				StackLocater ret = default;

				if (yieldReturn.ReturnValue.Count > 0)
				{
					for (int r = 0; r < yieldReturn.ReturnValue.Count; r++)
					{
						ret = BuildExpression(compileEnv, (AS3Expression)yieldReturn.ReturnValue[r], ref flagseed);
					}
				}
				else
				{
					throw new ResolverException(m.Token, "yield must return a value");
				}


				INS_Yield_Return yield_Return = new INS_Yield_Return(m.Token);
				yield_Return.dst = ret;

				compileEnv.instructions.Add(yield_Return);

				//throw new NotImplementedException();
			}
			else if (code is AS3YieldBreak)
			{
				AS3YieldBreak yieldBreak = (AS3YieldBreak)code;

				if (compileEnv.Scope.Kind == CodeScopeKind.Script)
				{
					throw new ResolverException(code.Token, "The yield statement cannot be used in global initialization code.");
				}
				else if (compileEnv.Scope.Kind == CodeScopeKind.Class || compileEnv.Scope.Kind == CodeScopeKind.Instance)
				{
					throw new InvalidOperationException();
				}

				var m = ((ASMethodBody)compileEnv.Scope.Container).Method;
				if (m.ReturnTypeKind != TypeKind.Any)
				{
					throw new ResolverException(m.Token, "generator must return *");
				}

				if (m.Flags.HasFlag(MethodFlags.ASYNC))
				{
					throw new ResolverException(m.Token, "The yield statement cannot be used in async code");
				}

				//yield 不能在 catch和finally块中发起。

				if (trycatchState.Any((t) => t.state == TryCatchContext.State.Catch || t.state == TryCatchContext.State.Finally))
				{
					throw new ResolverException(m.Token, "Can't yield in catch block or finally block");
				}

				//这个关系不大，只是仿照C#的限制。只要保证不在 catch,finally块里发动 yield即可。
				if (trycatchState.Any((t) => t.tryStatement != null && t.tryStatement.CatchList.Count > 0))
				{
					throw new ResolverException(m.Token, "Can't yield in [try_catch_finally] that have [catch] blocks");
				}

				
				((ASMethodBody)compileEnv.Scope.Container).Method.Flags |= MethodFlags.Generator;
				((ASMethodBody)compileEnv.Scope.Container).Method.Flags |= MethodFlags.NeedActivation; //构造器必然需要构造闭包。

				compileEnv.instructions.Add(new INS_Yield_Break(yieldBreak.Token));

			}
			else if (code is AS3Return)
			{
				AS3Return @return = (AS3Return)code;

				StackLocater ret = new StackLocater() { index = int.MinValue };

				if (compileEnv.Scope.Kind == CodeScopeKind.Script)
				{
					throw new ResolverException(code.Token, "The return statement cannot be used in global initialization code.");
				}
				else if (compileEnv.Scope.Kind == CodeScopeKind.Class || compileEnv.Scope.Kind == CodeScopeKind.Instance)
				{
					throw new InvalidOperationException();
				}
				else
				{
					var m = ((ASMethodBody)compileEnv.Scope.Container).Method;
					if (m.Container is ASClass && ((ASClass)m.Container).Constructor == m)
					{
						throw new ResolverException(code.Token, "The return statement cannot be used in static initialization code.");
					}

					if (m.ReturnTypeKind == TypeKind.Fun_Void && @return.ReturnValue.Count > 0)
					{
						throw new ResolverException(code.Token, "A return value is not allowed because the return type of this function is 'void'.");
					}

					//if (m.Flags.HasFlag(MethodFlags.ASYNC))
					//{
					//	if (@return.ReturnValue.Count > 0)
					//	{
					//		for (int r = 0; r < @return.ReturnValue.Count; r++)
					//		{
					//			ret = BuildExpression(compileEnv, (AS3Expression)@return.ReturnValue[r], ref flagseed);
					//		}
					//	}
					//	else
					//	{
					//		var ld_undefined = new INS_Ld_Undefined(@return.Token);
					//		ld_undefined.dst = ret;

					//		compileEnv.instructions.Add(ld_undefined);
					//	}

					//	INS_Return_Promise return_Promise = new INS_Return_Promise(@return.Token);
					//	return_Promise.dst = ret;
					//	compileEnv.instructions.Add(return_Promise);
					//}
					//else

					if (@return.ReturnValue.Count > 0)
					{
						//ExpressionIL expressionIL = new ExpressionIL();
						for (int r = 0; r < @return.ReturnValue.Count; r++)
						{
							ret = BuildExpression(compileEnv, (AS3Expression)@return.ReturnValue[r], ref flagseed);
						}

						if (!m.Flags.HasFlag(MethodFlags.ASYNC)) //asyn函数会走特殊处理，不必检查返回类型
						{
							ret = ExpressionIL.TestTypeConvert(compileEnv, ret, new CompileTypeKind() { Maj = m.ReturnTypeKind }, @return.Token);
						}
					}
					else if (m.ReturnTypeKind != TypeKind.Fun_Void && m.ReturnTypeKind != TypeKind.Any)
					{
						if (!m.Flags.HasFlag(MethodFlags.ASYNC)) //asyn函数会走特殊处理，不必检查返回
						{
							throw new ResolverException(code.Token, "Function does not return a value.");
						}
					}

					if (ret.index == int.MinValue && @return.ReturnValue.Count > 0)
					{
						throw new InvalidOperationException();
					}

					if (@return.ReturnValue.Count == 0)
					{
						compileEnv.instructions.Add(new INS_Return_Void(@return.Token));
					}
					else
					{
						INS_Return_Value return_Value = new INS_Return_Value(@return.Token);
						return_Value.dst = ret;
						compileEnv.instructions.Add(return_Value);
					}

					
				}

				
				
			}
			else if (code is AS3Break)
			{
				AS3Break @break = (AS3Break)code;

				if (string.IsNullOrWhiteSpace(@break.breakTarget))
				{
					//throw new NotImplementedException();

					var blocks = blockcontexts.ToArray();

					int jumptrys = 0;

					INS_Flag breakflag = null;

					for (int i = 0; i < blocks.Length; i++)
					{
						if (blocks[i] is TryCatchContext)
						{
							jumptrys++;
						}
						else if (((Block)blocks[i]).BreakTarget)
						{
							breakflag = ((Block)blocks[i]).breakFlag;
							break;
						}
					}

					if (breakflag == null)
					{
						throw new ResolverException(@break.Token, "Target of break statement was not found.");
					}
					else
					{
						INS_Goto @goto = new INS_Goto(@break.Token);

						if (jumptrys > 126)
						{
							throw new ResolverException(@break.Token, "too much try catch layers");
						}

						if (breakflag.flag_id > 0xfffff8)
						{
							throw new ResolverException(@break.Token, "flag_id is too large");
						}

						@goto.jumpTrys = jumptrys;
						@goto.jumpOffset = 0;
						@goto.flag_id = breakflag.flag_id;

						compileEnv.instructions.Add(@goto);

					}



				}
				else
				{
					string breaklabel = @break.breakTarget;

					var blocks = blockcontexts.ToArray();

					int jumptrys = 0;

					INS_Flag breakflag = null;

					for (int i = 0; i < blocks.Length; i++)
					{
						if (blocks[i] is TryCatchContext)
						{
							jumptrys++;
						}
						else if (((Block)blocks[i]).BreakTarget && ((Block)blocks[i]).Label != null && ((Block)blocks[i]).Label.Split("@").Contains(breaklabel))
						{
							breakflag = ((Block)blocks[i]).breakFlag;
							break;
						}
					}

					if (breakflag == null)
					{
						throw new ResolverException(@break.Token, "Target of break statement was not found.");
					}
					else
					{
						INS_Goto @goto = new INS_Goto(@break.Token);

						if (jumptrys > 126)
						{
							throw new ResolverException(@break.Token, "too much try catch layers");
						}

						if (breakflag.flag_id > 0xfffff8)
						{
							throw new ResolverException(@break.Token, "flag_id is too large");
						}

						@goto.jumpTrys = jumptrys;
						@goto.jumpOffset = 0;
						@goto.flag_id = breakflag.flag_id;

						compileEnv.instructions.Add(@goto);

					}

				}
			}
			else if (code is AS3Continue)
			{
				AS3Continue @continue = (AS3Continue)code;

				if (string.IsNullOrWhiteSpace(@continue.continueTarget))
				{
					var blocks = blockcontexts.ToArray();

					int jumptrys = 0;

					INS_Flag continueflag = null;

					for (int i = 0; i < blocks.Length; i++)
					{
						if (blocks[i] is TryCatchContext)
						{
							jumptrys++;
						}
						else if (((Block)blocks[i]).ContinueTarget)
						{
							continueflag = ((Block)blocks[i]).continueFlag;
							break;
						}
					}

					if (continueflag == null)
					{
						throw new ResolverException(@continue.Token, "Target of continue statement was not found.");
					}
					else
					{
						INS_Goto @goto = new INS_Goto(@continue.Token);

						if (jumptrys > 126)
						{
							throw new ResolverException(@continue.Token, "too much try catch layers");
						}

						if (continueflag.flag_id > 0xfffff8)
						{
							throw new ResolverException(@continue.Token, "flag_id is too large");
						}

						@goto.jumpTrys = jumptrys;
						@goto.jumpOffset = 0;
						@goto.flag_id = continueflag.flag_id;

						compileEnv.instructions.Add(@goto);

					}
				}
				else
				{
					var blocks = blockcontexts.ToArray();

					int jumptrys = 0;

					INS_Flag continueflag = null;

					for (int i = 0; i < blocks.Length; i++)
					{
						if (blocks[i] is TryCatchContext)
						{
							jumptrys++;
						}
						else if (((Block)blocks[i]).ContinueTarget && ((Block)blocks[i]).Label != null && ((Block)blocks[i]).Label.Split("@").Contains(@continue.continueTarget))
						{
							continueflag = ((Block)blocks[i]).continueFlag;
							break;
						}
					}

					if (continueflag == null)
					{
						throw new ResolverException(@continue.Token, "Target of continue statement was not found.");
					}
					else
					{
						INS_Goto @goto = new INS_Goto(@continue.Token);

						if (jumptrys > 126)
						{
							throw new ResolverException(@continue.Token, "too much try catch layers");
						}

						if (continueflag.flag_id > 0xfffff8)
						{
							throw new ResolverException(@continue.Token, "flag_id is too large");
						}

						@goto.jumpTrys = jumptrys;
						@goto.jumpOffset = 0;
						@goto.flag_id = continueflag.flag_id;

						compileEnv.instructions.Add(@goto);

					}
				}

			}
			else if (code is AS3IterBlockBase)
			{
				AS3IterBlockBase forin = (AS3IterBlockBase)code;

				if (forin.ForArg is AS3Variable)
				{
				}
				else
				{
					AS3Expression forvar = (AS3Expression)forin.ForArg;
					if (forvar.exprStepList.Count > 0)
					{
						new ExpressionIL().Build(forvar, compileEnv, ref flagseed);
					}
				}

				Block for_body = new Block();
				for_body.Label = forin.label;
				for_body.ContinueTarget = false;
				for_body.BreakTarget = true;
				for_body.continueFlag = null; //new INS_Flag(forin.ForArg.Token) { flag_id = flagseed++ };
				for_body.breakFlag = new INS_Flag(null) { flag_id = flagseed++ };
				for_body.token = forin.Token;

				blockcontexts.Push(for_body);

				throwIfDuplicateLabel();


				//由于这种代码的存在，这里必须把对象暂存到methodscope的变量中。否则对象就错乱了。另外由于缓存对象机制存在，只能缓存到变量中。
				//var o = new ccc();
				//for each(var v in o)
				//{
				//	o = {};
				//	trace(v) ;					
				//}


				ScopeHeapLocater iterSrcObjSaveAtVar; TypeKind heaptype; ASTrait ex; CodeScope ex_scope; ScopeMember ex_member; VTableItem[] tableItem;
				var _RV = ExpressionIL.FindIdentifier(forin.HoldObjVar.Name, null, compileEnv, forin.HoldObjVar.Token, out iterSrcObjSaveAtVar, out heaptype, out ex, out ex_scope, out ex_member, out tableItem);
				if (_RV != ExpressionIL.FindIdResultType.ScopeMember || ex_scope != compileEnv.Scope)
				{
					throw new InvalidOperationException();
				}

				ScopeHeapLocater iterSaveAtVar;
				_RV = ExpressionIL.FindIdentifier(forin.HoldIterVar.Name, null, compileEnv, forin.HoldObjVar.Token, out iterSaveAtVar, out heaptype, out ex, out ex_scope, out ex_member, out tableItem);
				if (_RV != ExpressionIL.FindIdResultType.ScopeMember || ex_scope != compileEnv.Scope)
				{
					throw new InvalidOperationException();
				}



				//iter = get  obj.iter 
				// if no iter goto Flag_End
				//try
				//Flag
				//  current ;
				//  if iter.next(ref current) 
				//    do something
				//    goto Flag   
				//  
				//Flag_Next_failed       
				//finally
				//{
				//   close iter
				//}
				//Flag_End

				StackLocater objLoc = BuildExpression(compileEnv, forin.ForInExpression, ref flagseed);
				//StackLocater iter = compileEnv.MakeStackLocater((TypeKind)compileEnv.CompileContext.player_for_compiler.Context.IITERATOR.Type_identifier);

				// 获取迭代器上下文变量的位置
				ScopeHeapLocater iterContextVarLocater; TypeKind iterCtxHeaptype; ASTrait iterCtxEx; CodeScope iterCtxEx_scope; ScopeMember iterCtxEx_member; VTableItem[] iterCtxTableItem;
				var iterCtx_RV = ExpressionIL.FindIdentifier(forin.IterContextVar.Name, null, compileEnv, forin.IterContextVar.Token, out iterContextVarLocater, out iterCtxHeaptype, out iterCtxEx, out iterCtxEx_scope, out iterCtxEx_member, out iterCtxTableItem);
				if (iterCtx_RV != ExpressionIL.FindIdResultType.ScopeMember || iterCtxEx_scope != compileEnv.Scope)
				{
					throw new InvalidOperationException();
				}

				INS_Iter_GetCtx iter_ctx = new INS_Iter_GetCtx(forin.ForInExpression.Token);
				iter_ctx.iterContextVar = iterContextVarLocater;
				compileEnv.instructions.Add(iter_ctx);

				//add get iter
				INS_Iter_Get iter_Get = new INS_Iter_Get(forin.ForInExpression.Token);
				iter_Get.iterSrcObj_HoldInHeap = iterSrcObjSaveAtVar;
				iter_Get.iterVar = iterSaveAtVar;
				iter_Get.iterSrcobj = objLoc;

				iter_Get.flag_end_id = for_body.breakFlag.flag_id;
				compileEnv.instructions.Add(iter_Get);

				TryCatchContext tryCatchContext = new TryCatchContext(null);
				trycatchState.Push(tryCatchContext);
				blockcontexts.Push(tryCatchContext);

				INS_Try_Enter try_Enter = new INS_Try_Enter(forin.ForInExpression.Token);
				try_Enter.catch_pc = new int[0];
				try_Enter.finally_pc = 0;
				try_Enter.dst = compileEnv.MakeStackLocater(TypeKind.Any);

				compileEnv.instructions.Add(try_Enter);

				Block for_continue = new Block();
				for_continue.BreakTarget = false;
				for_continue.ContinueTarget = true;
				for_continue.continueFlag = new INS_Flag(forin.ForArg.Token) { flag_id = flagseed++ };
				for_continue.Label = forin.label;

				compileEnv.instructions.Add(for_continue.continueFlag);
				//add call next
				blockcontexts.Push(for_continue);


				INS_Flag next_failed = new INS_Flag(null);
				next_failed.flag_id = flagseed++;


				string varname;
				if (forin.ForArg is AS3Variable)
				{
					varname = ((AS3Variable)forin.ForArg).Name;
				}
				else
				{
					AS3Expression forvar = (AS3Expression)forin.ForArg;

					if (forvar.Value.IsReg || forvar.exprStepList.Count > 0)
					{
						throw new ResolverException(forvar.Token, "Target of assignment must be a reference value.");
					}
					else if (forvar.Value.Data.FF1Type == FF1DataValueType.identifier)
					{
						varname = forvar.Value.Data.Value.ToString();
					}
					else
					{
						throw new ResolverException(forvar.Token, "Target of assignment must be a reference value.");
					}
				}


				AS3ExprStep setvaluetovar = new AS3ExprStep(forin.ForInExpression.Token);
				setvaluetovar.OpCode = "=";
				setvaluetovar.Type = OpType.Assigning;
				setvaluetovar.Arg1 = new AS3DataStackElement()
				{
					IsReg = false,
					Data = new AS3DataValue(forin.ForArg.Token)
					{
						FF1Type = FF1DataValueType.identifier,
						Value = varname
					}
				};
				setvaluetovar.Arg2 = new AS3DataStackElement() { IsReg = true, Reg = new AS3Reg(-1) };

				StackLocater nextvalue = compileEnv.GetStackLocater(setvaluetovar.Arg2.Reg, TypeKind.Any);

				INS_Iter_Next iter_Next = new INS_Iter_Next(forin.ForInExpression.Token);
				if (code is AS3ForIn)
				{
					iter_Next.mode = 0;
				}
				else
				{
					iter_Next.mode = 1;
				}

				iter_Next.flag_next_end_id = next_failed.flag_id;

				iter_Next.iterSrcObjSaveInVar = iterSrcObjSaveAtVar;
				iter_Next.iterator = iterSaveAtVar;
				iter_Next.result = nextvalue;

				compileEnv.instructions.Add(iter_Next);

				ExpressionIL.BuildAssigning(setvaluetovar, compileEnv);



				for (int i = 0; i < forin.Body.Count; i++)
				{
					BuildAS3SyntaxNode(compileEnv, forin.Body[i], trycatchState, blockcontexts, ref flagseed);
				}

				INS_Goto goto_next = new INS_Goto(compileEnv.instructions.Where(i => i.token != null).GroupBy(i => i.token.line).OrderByDescending(g => g.Key).First().OrderByDescending(i => i.token.ptr).First().token.nextToken);
				goto_next.jumpTrys = 0;
				goto_next.flag_id = for_continue.continueFlag.flag_id;
				compileEnv.instructions.Add(goto_next);



				next_failed.token = goto_next.token;
				compileEnv.instructions.Add(next_failed);

				blockcontexts.Pop();

				INS_Try_Exit try_Exit = new INS_Try_Exit(goto_next.token);
				compileEnv.instructions.Add(try_Exit);


				INS_Finally_Enter finally_Enter = new INS_Finally_Enter(goto_next.token);
				compileEnv.instructions.Add(finally_Enter);
				//add call close
				INS_Iter_Close iter_Close = new INS_Iter_Close(goto_next.token);
				iter_Close.holdObj = iterSrcObjSaveAtVar;
				iter_Close.dst = objLoc;                  //当枚举结束，还需要继续枚举proto时，用proto覆盖objLoc,跳回去继续枚举。
				iter_Close.iterator = iterSaveAtVar;
				iter_Close.iterContextVar = iterContextVarLocater;
				compileEnv.instructions.Add(iter_Close);

				INS_Finally_Exit finally_Exit = new INS_Finally_Exit(compileEnv.instructions.Where(i => i.token != null).GroupBy(i => i.token.line).OrderByDescending(g => g.Key).First().OrderByDescending(i => i.token.ptr).First().token.nextToken);
				finally_Exit.HoldError = try_Enter.dst;
				compileEnv.instructions.Add(finally_Exit);

				compileEnv.instructions.Add(for_body.breakFlag);
				for_body.breakFlag.token = finally_Exit.token;


				blockcontexts.Pop();
				trycatchState.Pop();

				blockcontexts.Pop();


				//throw new NotImplementedException();
			}

			else if (code is AS3For)
			{
				/*
				 Part1
				 
				_flag
				 Part2
				 if_false_Part2 goto end_flag
				     Body
				     ....
				     
				     
				    continue_Flag
				     Part3
				     goto _flag
				 end_flag

				 */

				AS3For as3For = (AS3For)code;

				INS_Flag _flag = new INS_Flag(as3For.Token);
				_flag.flag_id = flagseed++;
				compileEnv.instructions.Add(_flag);


				Block break_body = new Block();
				break_body.Label = as3For.label;
				break_body.ContinueTarget = false;
				break_body.BreakTarget = true;
				break_body.continueFlag = null; //new INS_Flag(forin.ForArg.Token) { flag_id = flagseed++ };
				break_body.breakFlag = new INS_Flag(null) { flag_id = flagseed++ };
				break_body.token = as3For.Token;

				blockcontexts.Push(break_body);
				throwIfDuplicateLabel();

				for (int i = 0; i < as3For.Part2.Count; i++)
				{
					BuildAS3SyntaxNode(compileEnv, as3For.Part2[i], trycatchState, blockcontexts, ref flagseed);
				}

				if (as3For.Part2.Count > 0)
				{
					var condition = (AS3Expression)as3For.Part2[as3For.Part2.Count - 1];
					StackLocater condition_reg = ExpressionIL.LoadRightValue(condition.Value, compileEnv, condition.Token);

					INS_If_False_Goto if_False_Goto = new INS_If_False_Goto(condition.Token);
					if_False_Goto.condition = condition_reg;
					if_False_Goto.flag_id = break_body.breakFlag.flag_id;

					compileEnv.instructions.Add(if_False_Goto);
				}

				Block continue_body = new Block();
				continue_body.BreakTarget = false;
				continue_body.ContinueTarget = true;
				continue_body.continueFlag = new INS_Flag(as3For.Token) { flag_id = flagseed++ };
				continue_body.Label = as3For.label;

				blockcontexts.Push(continue_body);
				throwIfDuplicateLabel();

				for (int i = 0; i < as3For.Body.Count; i++)
				{
					BuildAS3SyntaxNode(compileEnv, as3For.Body[i], trycatchState, blockcontexts, ref flagseed);
				}

				compileEnv.instructions.Add(continue_body.continueFlag);
				blockcontexts.Pop();

				int searchSt = compileEnv.instructions.Count;

				for (int i = 0; i < as3For.Part3.Count; i++)
				{
					BuildAS3SyntaxNode(compileEnv, as3For.Part3[i], trycatchState, blockcontexts, ref flagseed);
				}
				if (as3For.Part3.Count > 0)
				{
					//这是最特殊的 for(;;[update]) 部分，实际上需要检查新增的指令，是否有抛出异常的可能。
					//如果可能抛出，追加一个flag,flagid为0xffffff，构造cfg时，遇到他，就添加一个当作throw的处理

					for (int i = searchSt; i < compileEnv.instructions.Count; i++)
					{
						var op = compileEnv.instructions[i];


						if (op.MaybeRaiseError())
						{
							INS_Flag flag = new INS_Flag(as3For.Token);
							flag.flag_id = 0xffffff;
							compileEnv.instructions.Add(flag);

							break;
						}
					}


				}


				INS_Goto @goto = new INS_Goto(as3For.Token);
				@goto.flag_id = _flag.flag_id;
				compileEnv.instructions.Add(@goto);


				compileEnv.instructions.Add(break_body.breakFlag);
				blockcontexts.Pop();

				//throw new NotImplementedException();
			}
			else if (code is AS3While)
			{
				AS3While @while = (AS3While)code;

				/*
					continue_flag
					if_false_condition goto _break 
						
						body
						....
						
						goto continue_flag
						
					_break
					
				 */

				Block while_body = new Block();
				while_body.BreakTarget = true;
				while_body.ContinueTarget = true;
				while_body.continueFlag = new INS_Flag(@while.Token) { flag_id = flagseed++ };
				while_body.breakFlag = new INS_Flag(@while.Token) { flag_id = flagseed++ };

				while_body.Label = @while.label;


				blockcontexts.Push(while_body);
				throwIfDuplicateLabel();

				compileEnv.instructions.Add(while_body.continueFlag);



				StackLocater _c = default;
				for (int i = 0; i < @while.Condition.Count; i++)
				{
					_c = BuildExpression(compileEnv, (AS3Expression)@while.Condition[i], ref flagseed);
				}

				if (@while.Condition.Count > 0)
				{
					var condition = (AS3Expression)@while.Condition[@while.Condition.Count - 1];
					//StackLocater condition_reg = ExpressionIL.LoadRightValue(condition.Value, compileEnv, condition.Token);

					var condition_reg = _c;

					INS_If_False_Goto if_False_Goto = new INS_If_False_Goto(condition.Token);
					if_False_Goto.condition = condition_reg;
					if_False_Goto.flag_id = while_body.breakFlag.flag_id;

					compileEnv.instructions.Add(if_False_Goto);
				}

				for (int i = 0; i < @while.Body.Count; i++)
				{
					BuildAS3SyntaxNode(compileEnv, @while.Body[i], trycatchState, blockcontexts, ref flagseed);
				}


				INS_Goto @goto = new INS_Goto(@while.Token);
				@goto.flag_id = while_body.continueFlag.flag_id;
				compileEnv.instructions.Add(@goto);

				compileEnv.instructions.Add(while_body.breakFlag);
				blockcontexts.Pop();


			}

			else if (code is AS3DoWhile)
			{
				AS3DoWhile doWhile = (AS3DoWhile)code;
				/*
					loop_flag
						body
						....
						
						continue_flag;
						compute_condition
						if_true_condition goto loop_flag 						
					_break
				 */

				Block while_body = new Block();
				while_body.BreakTarget = true;
				while_body.ContinueTarget = true;
				while_body.continueFlag = new INS_Flag(doWhile.Token) { flag_id = flagseed++ };
				while_body.breakFlag = new INS_Flag(doWhile.Token) { flag_id = flagseed++ };

				while_body.Label = doWhile.label;

				blockcontexts.Push(while_body);
				throwIfDuplicateLabel();

				INS_Flag loopflag = new INS_Flag(doWhile.Token);
				loopflag.flag_id = flagseed++;

				compileEnv.instructions.Add(loopflag);

				for (int i = 0; i < doWhile.Body.Count; i++)
				{
					BuildAS3SyntaxNode(compileEnv, doWhile.Body[i], trycatchState, blockcontexts, ref flagseed);
				}


				compileEnv.instructions.Add(while_body.continueFlag);



				StackLocater _c = default;
				for (int i = 0; i < doWhile.Condition.Count; i++)
				{
					_c = BuildExpression(compileEnv, (AS3Expression)doWhile.Condition[i], ref flagseed);
				}

				if (doWhile.Condition.Count > 0)
				{

					var condition = (AS3Expression)doWhile.Condition[doWhile.Condition.Count - 1];
					//StackLocater condition_reg = ExpressionIL.LoadRightValue(condition.Value, compileEnv, condition.Token);
					StackLocater condition_reg = _c;

					INS_If_True_Goto if_true_goto = new INS_If_True_Goto(condition.Token);
					if_true_goto.condition = condition_reg;
					if_true_goto.flag_id = loopflag.flag_id;

					compileEnv.instructions.Add(if_true_goto);
				}
				else
				{
					//这种只可能是 do{}while(function f(){}) 这种奇怪写法，那么就是死循环

					INS_Goto _Goto = new INS_Goto(doWhile.Token);
					_Goto.flag_id = loopflag.flag_id;

					compileEnv.instructions.Add(_Goto);
				}


				compileEnv.instructions.Add(while_body.breakFlag);

				blockcontexts.Pop();

			}
			else if (code is AS3Switch)
			{
				AS3Switch @switch = (AS3Switch)code;




				/*
				 
				     switch_var
				     compute case_0
				     is_case0 = switch_var === case_0
				     if_false_is_case0 goto case_1_start
				        ...
				       
				        [break ]=>goto switch_[label]_end
				        
						goto case_1_pass:  
					 
				     case_1_start
				     compute case_1
				     is_case1 = switch_var === case_1
				     if_false_is_case1 goto case_2_start
				      case_1_pass
				      ...
				      goto case_2_pass
				     
				     ...
				     case_2_start
				       ...

				     [default]
				         defaultbody...
				         [break ]=>goto switch_[label]_end

				     switch_[label]_end

				 */

				Block switch_body = new Block();
				switch_body.BreakTarget = true;
				switch_body.ContinueTarget = false;
				switch_body.breakFlag = new INS_Flag(@switch.Token) { flag_id = flagseed++ };
				switch_body.Label = @switch.label;

				blockcontexts.Push(switch_body);
				throwIfDuplicateLabel();


				INS_Flag next_case_start = null;

				INS_Flag default_start = null;
				if (@switch.CaseTestList.Exists(c => c == null))
				{
					default_start = new INS_Flag(@switch.default_part_token);
					default_start.flag_id = flagseed++;
				}


				Dictionary<int, INS_Flag> flag_case_pass = new Dictionary<int, INS_Flag>();

				//for (int i = 0; i < @switch.Expr.exprStepList.Count; i++)
				//{
				//	BuildExpression(compileEnv, @switch.Expr, ref flagseed);
				//}

				//StackLocater var_loc = ExpressionIL.LoadRightValue(@switch.Expr.Value, compileEnv, @switch.Expr.Token);

				StackLocater var_loc = BuildExpression(compileEnv, @switch.Expr, ref flagseed);

				for (int i = 0; i < @switch.CaseTestList.Count; i++)
				{
					if (@switch.CaseTestList[i] != null)
					{
						if (next_case_start != null)
						{
							compileEnv.instructions.Add(next_case_start);
						}

						BuildExpression(compileEnv, @switch.CaseTestList[i], ref flagseed);

						var case_cond = ExpressionIL.LoadRightValue(@switch.CaseTestList[i].Value, compileEnv, @switch.CaseTestList[i].Token);
						INS_Strict_Eq strict_Eq = new INS_Strict_Eq(@switch.CaseTestList[i].Token);
						strict_Eq.v1 = var_loc;
						strict_Eq.v2 = case_cond;
						strict_Eq.dst = compileEnv.MakeStackLocater(TypeKind.Boolean);
						compileEnv.instructions.Add(strict_Eq);


						//如果不匹配，则前往下一个case的开始位置
						var next_case = @switch.CaseTestList.Skip(i + 1).FirstOrDefault(c => c != null);
						if (next_case != null)
						{
							next_case_start = new INS_Flag(next_case.Token);
							next_case_start.flag_id = flagseed++;

							INS_If_False_Goto if_False_Goto = new INS_If_False_Goto(strict_Eq.token);
							if_False_Goto.condition = strict_Eq.dst;
							if_False_Goto.flag_id = next_case_start.flag_id;

							compileEnv.instructions.Add(if_False_Goto);
						}
						else if (default_start != null)
						{
							//有default则前往default;
							INS_If_False_Goto if_False_Goto = new INS_If_False_Goto(strict_Eq.token);
							if_False_Goto.condition = strict_Eq.dst;
							if_False_Goto.flag_id = default_start.flag_id;
							compileEnv.instructions.Add(if_False_Goto);
						}
						else
						{
							//结束
							INS_If_False_Goto if_False_Goto = new INS_If_False_Goto(strict_Eq.token);
							if_False_Goto.condition = strict_Eq.dst;
							if_False_Goto.flag_id = switch_body.breakFlag.flag_id;
							compileEnv.instructions.Add(if_False_Goto);
						}

						if (i > 0)
						{
							compileEnv.instructions.Add(flag_case_pass[i]);
						}

						for (int j = 0; j < @switch.CaseBodyList[i].Count; j++)
						{
							BuildAS3SyntaxNode(compileEnv, @switch.CaseBodyList[i][j], trycatchState, blockcontexts, ref flagseed);
						}

					}
					else
					{
						compileEnv.instructions.Add(default_start);
						for (int j = 0; j < @switch.CaseBodyList[i].Count; j++)
						{
							BuildAS3SyntaxNode(compileEnv, @switch.CaseBodyList[i][j], trycatchState, blockcontexts, ref flagseed);
						}
					}

					if (i < @switch.CaseTestList.Count - 1) //不是最后一个块，则漏到下面块里去
					{

						if (@switch.CaseTestList[i + 1] == null) // default,不管
						{

						}
						else
						{
							INS_Flag flag = new INS_Flag(@switch.CaseTestList[i + 1].Token);
							flag.flag_id = flagseed++;
							flag_case_pass[i + 1] = flag;

							INS_Goto goto_next = new INS_Goto(flag.token);
							goto_next.flag_id = flag.flag_id;

							compileEnv.instructions.Add(goto_next);

						}
					}





				}



				compileEnv.instructions.Add(switch_body.breakFlag);
				blockcontexts.Pop();

			}
			else if (code is AS3With)
			{
				throw new ResolverException(code.Token, "[with] is not support");
			}
			else
			{
				throw new NotImplementedException();
			}


		}



		private static StackLocater BuildExpression(CompileEnv compileEnv,AS3Expression expression , ref int flag_seed)
        {
			compileEnv.BeginCollectRef();
			ExpressionIL expressionIL = new ExpressionIL();

			int start = compileEnv.instructions.Count;
			
			
			var ret = expressionIL.Build(expression, compileEnv,ref flag_seed);

			List<StackLocater> expression_defs = new List<StackLocater>();
			int end = compileEnv.instructions.Count; //新增指令
			for (int i = start; i < end; i++)   
			{
				var ins = compileEnv.instructions[i];

				//这些指令的加载目标都在安全的地方（比如methodScope,const pool）等,或者它们将目标设置为非heap的值
				if (
					ins.INS_Code != INS_Code.ld_array_hole &&
					ins.INS_Code != INS_Code.ld_arguments &&
					ins.INS_Code != INS_Code.ld_class &&
					ins.INS_Code != INS_Code.ld_const &&
					ins.INS_Code != INS_Code.ld_false &&
					ins.INS_Code != INS_Code.ld_true &&
					ins.INS_Code != INS_Code.ld_undefined &&
					ins.INS_Code != INS_Code.ld_This &&
					ins.INS_Code != INS_Code.ld_null &&
					ins.INS_Code != INS_Code.ld_namespace &&
					ins.INS_Code != INS_Code.ld_memberInitValue  &&
					ins.INS_Code != INS_Code.ld_methodVariable	&&
					ins.INS_Code != INS_Code.ld_ScopeH
					)
				{
					expression_defs.AddRange(ins.GetDef());
				}

				if (ins.INS_Code == INS_Code.array_vector_initelement)
				{
					expression_defs.Remove(ins.dst); //元素插入完毕后那个stackslot就可以释放了。
				}

			}

			if (expression_defs.Count > 1 && 
				(
				(compileEnv.instructions[end-1].INS_Code != INS_Code.storeScopeH &&
				compileEnv.instructions[end-1].INS_Code != INS_Code.storeMethodVariable
				)
				//||
				//(!expression.Value.IsReg && expression.Value.Data.FF1Type == FF1DataValueType.as3_array ) 
				//||
				//(!expression.Value.IsReg && expression.Value.Data.FF1Type == FF1DataValueType.as3_vector)
				//||
				//(!expression.Value.IsReg && expression.Value.Data.FF1Type == FF1DataValueType.dynamicobj)
				)
				)
			{
				INS_Barrier barrier = new INS_Barrier(expression.Token);
				barrier.uselist = expression_defs.ToArray();

				compileEnv.instructions.Add(barrier);
			}


			var memberRefs = compileEnv.EndCollectRef();
			foreach (var memberRef in memberRefs)
			{
				if (memberRef.isLd_R)
					continue;

				var genStep = compileEnv.ReadTraitRef(memberRef).Item2;

				var refTrais = compileEnv.ReadTraitRef(memberRef).Item1;

				if (refTrais.Length == 1 &&
					refTrais[0].Kind != ABC.TraitKind.Getter &&
					refTrais[0].Kind != ABC.TraitKind.Setter
					) //不用管
				{

				}
				else if (refTrais.Length == 2)
				{
					//需要追加一条测试读取，用于万一是Getter的情况

					//查找是否没有对此引用的使用
					if (!expression.exprStepList.Any(s =>
						(s.Arg2 != null && s.Arg2.Reg == memberRef) ||
						(s.Arg3 != null && s.Arg3.Reg == memberRef) ||
						(s.Arg1 != null && s.Arg1.Reg == memberRef && genStep != s) ||
						(
							s.Arg3 != null && s.Arg3.Data !=null && s.Arg3.Data.FF1Type == AST.Expr.FF1DataValueType.as3_array
							&&
							((List<AS3DataStackElement>)s.Arg3.Data.Value).Any(a => a.Reg == memberRef)
						)
					))
					{
						StackLocater locater = compileEnv.GetStackLocater(memberRef);
						var instance = compileEnv.ReadRefBindInstance(memberRef);

						if (refTrais[0] == null)
						{
							throw new ResolverException(
								expression.Token,
								"Property " + refTrais[1].QName.Name + " is write-only."
								);
						}

						var tkind = compileEnv.ReadStackType(instance);

						if (tkind.Maj == TypeKind.Class)
						{
							var ttype = ExpressionIL.FindClassById(compileEnv, (ulong)tkind.Mir);
							int t_index = ttype._vtable.Items.FindIndex(i => i.Trait == refTrais[0]);
							if (t_index < 0)
								throw new InvalidOperationException();
							if (t_index > 65535)
							{
								throw new ParseException(ttype.QName.ToDebugTypeName() + ".vtable.items.count > 65535");
							}

							INS_readPoperty readPoperty = new INS_readPoperty(expression.Token);
							readPoperty.dst = compileEnv.MakeStackLocater(TypeKind.Any);
							readPoperty.instance = instance;
							readPoperty.const_index = (ushort)t_index;

							compileEnv.SetCallResult(readPoperty.dst);
							compileEnv.instructions.Add(readPoperty);

							if(memberRef == expression.Value.Reg)
								ret = readPoperty.dst;

						}
						else
						{
							var ttype = ExpressionIL.FindClassById(compileEnv, (ulong)tkind.Maj);

							int t_index = ttype.Instance._vtable.Items.FindIndex(i => i.Trait == refTrais[0]);
							if (t_index < 0)
								throw new InvalidOperationException();

							if (t_index > 65535)
							{
								throw new ParseException(ttype.QName.ToDebugTypeName() + ".vtable.items.count > 65535");
							}

							if (ttype.Instance.IsInterface)
							{
								int class_id = compileEnv.AddConstClassId(ttype);

								INS_readPoperty_Interface readPoperty_Interface = new INS_readPoperty_Interface(expression.Token);
								readPoperty_Interface.dst = compileEnv.MakeStackLocater(TypeKind.Any);
								readPoperty_Interface.instance = instance;
								readPoperty_Interface.class_id = class_id;
								readPoperty_Interface.const_index = (ushort)t_index;

								compileEnv.SetCallResult(readPoperty_Interface.dst);
								compileEnv.instructions.Add(readPoperty_Interface);

								if (memberRef == expression.Value.Reg)
									ret = readPoperty_Interface.dst;
							}
							else
							{

								INS_readPoperty readPoperty = new INS_readPoperty(expression.Token);
								readPoperty.dst = compileEnv.MakeStackLocater(TypeKind.Any);
								readPoperty.instance = instance;
								readPoperty.const_index = (ushort)t_index;

								compileEnv.SetCallResult(readPoperty.dst);
								compileEnv.instructions.Add(readPoperty);

								if (memberRef == expression.Value.Reg)
									ret = readPoperty.dst;
							}

						}


					}
				}
				else
				{
					//查找是否没有对此引用的使用
					if (!expression.exprStepList.Any(s =>
						(s.Arg2 != null && s.Arg2.Reg == memberRef) ||
						(s.Arg3 != null && s.Arg3.Reg == memberRef) ||
						(s.Arg1 != null && s.Arg1.Reg == memberRef && genStep != s) ||
						(
							s.Arg3 != null && s.Arg3.Data !=null && s.Arg3.Data.FF1Type == AST.Expr.FF1DataValueType.as3_array
							&&
							((List<AS3DataStackElement>)s.Arg3.Data.Value).Any(a => a.Reg == memberRef)
						)
					))
					{


						StackLocater locater = compileEnv.GetStackLocater(memberRef);
						var instance = compileEnv.ReadRefBindInstance(memberRef);


						INS_Ld_ValueRef readValueRef = new INS_Ld_ValueRef(expression.Token);
						readValueRef.source = locater;
						readValueRef.dst = compileEnv.MakeStackLocater( TypeKind.Any  );
						compileEnv.instructions.Add(readValueRef);

						if (memberRef == expression.Value.Reg)
							ret = readValueRef.dst;
					}
				}
			}


			return ret;

		}


        
    }
}
