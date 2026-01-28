using juicescript.compiler.AST;
using juicescript.compiler.AST.Expr;
using juicescript.compiler.AST.Stmt;
using juicescript.compiler.IL;
using juicescript.compiler.IL.Generator;
using MyMD5;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http.Headers;
using System.Numerics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using System.Xml.Linq;

namespace juicescript.compiler.parse
{
    /// <summary>
    /// 生成抽象语法树
    /// </summary>
    public class AS3AbstractSyntaxTree
    {
        static MethodInfo[] methods;
        static AS3AbstractSyntaxTree()
        {
            methods = typeof(AS3AbstractSyntaxTree).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);
        }


        private AS3SrcFile srcFile = null;

        public SyntaxException SyntaxError;


        private Dictionary<ParseExpr, Action<ParseExpr>> enter_events = new Dictionary<ParseExpr, Action<ParseExpr>>();
        private Dictionary<ParseExpr, Action<ParseExpr>> quit_events = new Dictionary<ParseExpr, Action<ParseExpr>>();

        private MD5Result _treekey;
        public AS3SrcFile Analyse(ParseTree parseTree)
        {
            _treekey = parseTree.Key;
            SyntaxError = null;

            Dictionary<string, Action<ParseExpr>> handles = new Dictionary<string, Action<ParseExpr>>();
            foreach (var method in methods) 
            {
                if (method.ReturnType == typeof(void)
                    &&
                    method.GetParameters().Length == 1
                    &&
                    method.GetParameters()[0].ParameterType == typeof(ParseExpr)
                    )
                {

                    var handle = method.CreateDelegate<Action<ParseExpr>>(this);                  
                    handles.Add(method.Name, handle);
                    
                }
            }


            var stack = new Stack< Tuple<ParseExpr,bool> >();
            stack.Push( new Tuple<ParseExpr, bool>( parseTree.Root,false));

            try
            {
                //int tab = 0;
                while (stack.Count > 0)
                {
                    var tuple = stack.Pop();
                    var node = tuple.Item1;

                    if (!tuple.Item2)
                    {
                        stack.Push(new Tuple<ParseExpr, bool>(node, true));
                        for (int i = node.Nodes.Count - 1; i >= 0; i--)
                        {
                            stack.Push(new Tuple<ParseExpr, bool>(node.Nodes[i], false));
                        }
                    }

                    var name = node.GrammerLeftNode.Name;
                    if (name.StartsWith("K_") || name.StartsWith("F_"))
                    {
                        name = name.Substring(2);
                    }

                    if (!tuple.Item2)
                    {
                        Action<ParseExpr> _event_;
                        if (enter_events.TryGetValue(node, out _event_))
                        {
                            _event_(node);
                        }

                        if (handles.ContainsKey("ENTER_" + name))
                        {
                            handles["ENTER_" + name].Invoke(node);
                        }

                        

                        //Console.WriteLine( "".PadLeft(tab, ' ') + "enter " + node.GrammerLeftNode.Name);
                        //++tab;
                    }
                    else
                    {
                        

                        if (handles.ContainsKey("QUIT_" + name))
                        {
                            handles["QUIT_" + name].Invoke(node);
                        }

                        Action<ParseExpr> _event_;
                        if (quit_events.TryGetValue(node, out _event_))
                        {
                            _event_(node);
                        }

                        //--tab;
                        //Console.WriteLine( "".PadLeft(tab, ' ') + "quit  " + node.GrammerLeftNode.Name);

                    }

                }
                
                return srcFile;
            }
            catch (SyntaxException syntaxErr)
            {
                SyntaxError = syntaxErr;

                return null;
            }

            

        }

        Stack<IAS3ImportList> importscope = new Stack<IAS3ImportList>();

        Stack<AS3MemberScope> memberscope = new Stack<AS3MemberScope>();   

        void ENTER_AS3File(ParseExpr node)
        {
            srcFile = new AS3SrcFile(node.MatchedToken.sourceFile, node.MatchedToken.sourceFileFullPath , _treekey);

        }
        void QUIT_AS3File(ParseExpr node)
        { 
            
        }

        /// <summary>
        /// 标记当前是否处于package内
        /// </summary>
        bool inpackage = false;
        void ENTER_PACKAGE(ParseExpr node)
        { 
            srcFile.Package=new AS3Package(srcFile);

            //code_stack.Push(srcFile.Package.Codes);

            code_stack.Push(srcFile.OutPackage.Codes);

            memberscope.Push(srcFile.Package.MemberScope);
        }
        HashSet<AS3Expression> notMetaSet = new HashSet<AS3Expression>();
        void QUIT_PACKAGE(ParseExpr node)
        {
            
            memberscope.Pop();
            code_stack.Pop();
        }

        void ENTER_PACKAGEBODY(ParseExpr node)
        {
            inpackage = true;

            importscope.Push(srcFile.Package);

        }
        void QUIT_PACKAGEBODY(ParseExpr node)
        {
            foreach (var item in code_stack.Peek())
            {
                if (item is AS3Expression)
                {
                    notMetaSet.Add((AS3Expression)item);
                }
            }

            inpackage = false;

            importscope.Pop();
        }


        void QUIT_PACKAGE_NAME(ParseExpr node)
        {
            srcFile.Package.Name = ParseExpr.getNodeValue(node);

        }

        void ENTER_HOUT_PACKAGE(ParseExpr node)
        {
            

            memberscope.Push( srcFile.OutPackage );

            importscope.Push(srcFile.OutPackage);

            code_stack.Push(srcFile.OutPackage.Codes);

        }

        void QUIT_HOUT_PACKAGE(ParseExpr node)
        {
            if (memberscope.Peek() != srcFile.OutPackage)
            {
                throw new Exception("错了");
            }

            code_stack.Pop();

            importscope.Pop();

            memberscope.Pop();
        }
        void ENTER_OUT_PACKAGE(ParseExpr node)
        {
            

           

            memberscope.Push( srcFile.OutPackage );

            importscope.Push( srcFile.OutPackage );

            code_stack.Push(srcFile.OutPackage.Codes);
        }

        void QUIT_OUT_PACKAGE(ParseExpr node)
        {
            if (memberscope.Peek() != srcFile.OutPackage)
            {
                throw new Exception("错了");
            }

            code_stack.Pop();

            importscope.Pop();

            memberscope.Pop();

        }

        void extract_namespace(AS3Access access , int checkline)
        { 
            var codes = code_stack.Peek();
            if (codes.Count > 0)
            {
                var test = codes[codes.Count - 1];
                if (test is AS3Expression)
                { 
                    AS3Expression t = (AS3Expression)test;
                    if (t.exprStepList.Count == 0)
                    {
                        if (t.Value.Data.FF1Type == FF1DataValueType.identifier)
                        {
                            if (t.Token.line == checkline )
                            {
                                var nt = t.Token.nextToken;
                                while (nt.Type == Token.TokenType.whitespace || nt.Type == Token.TokenType.comments)
                                {
                                    nt = nt.nextToken;
                                }

                                if (nt.StringValue == ";")
                                {
                                    return;
                                }

                                codes.RemoveAt(codes.Count - 1);
                                access.NameSpace = t.Value.Data.Value.ToString();
                                access.NameSpaceToken = t.Token;
                            }
                        }
                    }
      //              else if (t.exprStepList.Count == 1)
      //              {
      //                  if (t.exprStepList[0].Type == OpType.Load && t.exprStepList[0].OpCode == "Ld_R")
      //                  {
      //                      var testdata = t.exprStepList[0].Arg2;

						//	if (testdata.Data.FF1Type == FF1DataValueType.identifier)
						//	{
						//		if (t.Token.line == checkline)
						//		{
						//			codes.RemoveAt(codes.Count - 1);
						//			access.NameSpace = testdata.Data.Value.ToString();
						//			access.NameSpaceToken = t.Token;
						//		}
						//	}
						//}
      //              }
                }
            }


        }

        void extract_metas(List<AS3Expression> metas)
        {
            var codes = code_stack.Peek();//提取meta
            for (int i = codes.Count -1; i >=0; i--)
            {
                if (codes[i] is AS3Expression)
                {
                    var expr = (AS3Expression)codes[i];
                    if (!expr.Value.IsReg)
                    {
                        if (expr.exprStepList != null && expr.exprStepList.Count > 0)
                        {
        //                    if (expr.exprStepList[0].Type == OpType.Load && expr.exprStepList[0].OpCode == "Ld_R")
        //                    {
        //                        if (expr.Value.Data.FF1Type == FF1DataValueType.as3_array)
        //                        {
        //                            var arr = (List<AS3DataStackElement>)expr.Value.Data.Value;
        //                            if (arr.All(a =>a.IsReg && a.Reg.ID == expr.exprStepList[0].Arg1.Reg.ID) && arr.Count ==1)
        //                            {
        //                                arr[0] = expr.exprStepList[0].Arg2;
        //                            }
        //                        }

        //                        if (expr.exprStepList[expr.exprStepList.Count - 1].Type == OpType.CallFunc)
        //                        {
								//	expr.exprStepList[expr.exprStepList.Count - 1].Arg2 = expr.exprStepList[0].Arg2;
								//}

        //                        expr.exprStepList.RemoveAt(0);

                                

        //                    }
                        }
                    }

                    if (
                        !notMetaSet.Contains(expr) &&
                        !expr.Value.IsReg && expr.Value.Data.FF1Type == FF1DataValueType.as3_array &&
                        ((expr.exprStepList.Count > 0
                        &&
                        expr.exprStepList[expr.exprStepList.Count - 1].Type == OpType.CallFunc)
                        ||
                        (expr.exprStepList.Count == 0
                            &&
                            ((List<AS3DataStackElement>)expr.Value.Data.Value).Count == 1
                        )
                        )
                        )
                    {
                        for (int j = 0; j < expr.exprStepList.Count - 1; j++)
                        {
                            var step = expr.exprStepList[j];

                            if (!(step.OpCode == "=" && !step.Arg1.IsReg && !step.Arg2.IsReg))
                            {
                                break;
                            }

                        }
                        metas.Insert(0, expr);
                        codes.RemoveAt(i);
                    }
                    else
                    {
                        break;
                    }
                }
                else
                { 
                    break; 
                }
            }
        }

        


		AS3ClassInterfaceBase _current_class_interface;
        List<string> _current_externs;
        void ENTER_DefClass(ParseExpr node)
        {
            

            var accesslist = access_attributes.Peek();
            AS3Access access = new AS3Access();
            access.Set(accesslist);

            if (access.IsNative)
            {
                throw new SyntaxException(node.MatchedToken, "The native attribute can only be used with function definitions.");
            }

            //if (access.IsStatic)
            //{
            //    throw new SyntaxException(node.MatchedToken, "The static attribute may be used only on definitions inside a class.");
            //}
            //if (access.IsOverride)
            //{
            //    throw new SyntaxException(node.MatchedToken, "The override attribute may be used only on class property definitions.");
            //}

            var cls = new AS3Class(node.MatchedToken, srcFile);
            memberscope.Push(cls);

            cls.Access = access;
            cls.Name = ParseExpr.getNodeValue(node.Nodes[1]);

			if (cls.Name == "null" || cls.Name == "undefined")
			{
				throw new SyntaxException(node.MatchedToken, $"'{cls.Name}' is not allowed here'.");
			}


			if (inpackage)
            {
                if (cls.Package.MainClass != null || cls.Package.MainInterface != null || cls.Package.MainNamespace != null)
                {
                    throw new SyntaxException(node.MatchedToken, "An externally-visible definition with the name '" + cls.Name + "' was unexpectedly found.");
                }

                cls.IsOutPackage = false;
                cls.Package.MainClass = cls;
            }
            else
            {
                cls.IsOutPackage = true;
                srcFile.OutPackage.outpackage_classes_interfaces.Add(cls);
            }

            extract_namespace(cls.Access,cls.Token.line);
            extract_metas(cls.Metas);
            cls.Access.CheckNSException();
            if (cls.Access.NameSpaceToken != null)
            {
                throw new SyntaxException(cls.Token, "A user-defined namespace attribute can only be used at the top level of a class definition.");
            }



            importscope.Push(cls);

            _current_class_interface = cls;

            code_stack.Push(cls.CInitCodes);

        }

        void QUIT_DefClass(ParseExpr node)
        {
            code_stack.Pop();

            importscope.Pop();

            _current_class_interface = null;
            memberscope.Pop();
        }

        void ENTER_DefInterface(ParseExpr node)
        {
            var accesslist = access_attributes.Peek();
            AS3Access access = new AS3Access();
            access.Set(accesslist);

            if (access.IsNative)
            {
                throw new SyntaxException(node.MatchedToken, "The native attribute can only be used with function definitions.");
            }

            var _interface_ = new AS3Interface(node.MatchedToken, srcFile);
            memberscope.Push(_interface_);

            _interface_.Access = access;
            _interface_.Name = ParseExpr.getNodeValue(node.Nodes[1]);

			if (_interface_.Name == "null" || _interface_.Name == "undefined")
			{
				throw new SyntaxException(node.MatchedToken, $"'{_interface_.Name}' is not allowed here'.");
			}

			if (inpackage)
            {
                if (_interface_.Package.MainClass != null || _interface_.Package.MainInterface != null || _interface_.Package.MainNamespace != null)
                {
                    throw new SyntaxException(node.MatchedToken, "An externally-visible definition with the name '" + _interface_.Name + "' was unexpectedly found.");
                }


                _interface_.IsOutPackage = false;
                _interface_.Package.MainInterface = _interface_;
            }
            else
            { 
                _interface_.IsOutPackage=true;
                srcFile.OutPackage.outpackage_classes_interfaces.Add(_interface_);
            }

            extract_namespace(access, _interface_.Token.line);
            extract_metas(_interface_.Metas);
            access.CheckNSException();

            if (access.NameSpaceToken != null)
            {
                throw new SyntaxException(_interface_.Token, "A user-defined namespace attribute can only be used at the top level of a class definition.");
            }

            importscope.Push(_interface_);

            _current_class_interface = _interface_;

        }

        void QUIT_DefInterface(ParseExpr node)
        {
            importscope.Pop();

            _current_class_interface = null;
            memberscope.Pop();
        }


        void ENTER_Extends(ParseExpr node)
        {
            _current_externs = _current_class_interface.ExtendsNames;
        }

        void QUIT_Extends(ParseExpr node)
        {
            _current_externs = null;
        }

        void ENTER_Implements(ParseExpr node)
        {
            _current_externs = ((AS3Class)_current_class_interface).ImplementsNames;
        }

        void QUIT_Implements(ParseExpr node)
        {
            _current_externs = null;
        }

        void ENTER_ImplList(ParseExpr node)
        {
            if (_current_class_interface is AS3Class)
            {
                if (Object.ReferenceEquals(_current_externs, ((AS3Class)_current_class_interface).ExtendsNames))
                {
                    if (_current_externs.Count > 0)
                    {
                        throw new SyntaxException(node.Parent.MatchedToken, "',' is not allowed here");
                    }
                }
            }
            _current_externs.Add(ParseExpr.getNodeValue(node.Nodes[0]));
        }



        void ENTER_Import(ParseExpr node)
        {
            importscope.Peek().Imports.Add(ParseExpr.getNodeValue(node.Nodes[1]) );
        }

        void QUIT_Import(ParseExpr node)
        { 
            
        }

      
        Stack<List<IAS3SyntaxNode>> code_stack = new Stack<List<IAS3SyntaxNode>>();

        Stack<List<Tuple<string,ParseExpr>>> access_attributes = new Stack<List<Tuple<string, ParseExpr>>>();

        Stack<ParseExpr> access_member = new Stack<ParseExpr>();

        void ENTER_PACKAGE_EXPR(ParseExpr node)
        {
            access_attributes.Push(new List<Tuple<string, ParseExpr>>());
        }

        void QUIT_PACKAGE_EXPR(ParseExpr node)
        { 
            access_attributes.Pop();
        }

        void ENTER_NameSpaceDefaultValue(ParseExpr node)
        {
            code_stack.Push(new List<IAS3SyntaxNode>());
            //if (node.Nodes.Count > 0)
            //{
            //    ns_stack.Peek().URI = ParseExpr.getNodeValue(node.Nodes[1]);
            //}
        }

        void QUIT_NameSpaceDefaultValue(ParseExpr node)
        {
            var ns_defaultvalue = code_stack.Pop();

            if (node.Nodes.Count > 0)
            {
                if (ns_defaultvalue.Count == 1 && ns_defaultvalue[0] is AS3Expression && ((AS3Expression)ns_defaultvalue[0]).exprStepList.Count == 0
                    &&
                    !((AS3Expression)ns_defaultvalue[0]).Value.IsReg
                    &&
                    ((AS3Expression)ns_defaultvalue[0]).Value.Data.FF1Type == FF1DataValueType.const_string
                    )
                {
                    ns_stack.Peek().URI = "0:" + ((AS3Expression)ns_defaultvalue[0]).Value.Data.Value.ToString();
                }
                else if (ns_defaultvalue.Count == 1 && ns_defaultvalue[0] is AS3Expression && ((AS3Expression)ns_defaultvalue[0]).exprStepList.Count == 0
                    &&
                    !((AS3Expression)ns_defaultvalue[0]).Value.IsReg
                    &&
                    ((AS3Expression)ns_defaultvalue[0]).Value.Data.FF1Type == FF1DataValueType.identifier
                    )
                {
                    ns_stack.Peek().URI = "1:" + ((AS3Expression)ns_defaultvalue[0]).Value.Data.Value.ToString();
                }
                else
                {
                    throw new SyntaxException(node.MatchedToken, "A namespace initializer must be either a literal string or another namespace.");
                }
            }

        }

        Stack<AS3NameSpace> ns_stack = new Stack<AS3NameSpace>();
        void ENTER_NameSpace(ParseExpr node)
        {
            var accesslist = access_attributes.Peek();
            if (accesslist.Count > 0 && memberscope.Peek() is AS3Function.AS3FunctionScope)
            {
                throw new SyntaxException(node.MatchedToken, "Access modifier not allowed on declarations inside a function.");
            }

            AS3Access access = new AS3Access();
            access.Set(accesslist);

            if (access.IsNative)
            {
                throw new SyntaxException(node.MatchedToken, "The native attribute can only be used with function definitions.");
            }

            AS3NameSpace ns = new AS3NameSpace(node.MatchedToken);
            ns.Access = access;
            ns.Name = ParseExpr.getNodeValue(node.Nodes[1]);

            if (memberscope.Peek() is AS3Interface)
            {
                throw new SyntaxException(node.MatchedToken, "Namespace declarations are not permitted in interfaces.");
            }

            //if (memberscope.Peek() is AS3ClassInterfaceBase)
            {
                extract_metas(ns.Metas);
            }

            if (memberscope.Peek() is AS3Package.PackageMemberScope)
            {
                if (srcFile.Package.MainClass != null || srcFile.Package.MainInterface != null || srcFile.Package.MainNamespace != null)
                {
                    throw new SyntaxException(ns.Token, "An externally-visible definition with the name '" + srcFile.Package.Name + "." + ns.Name + "' was unexpectedly found.");
                }
                else
                {
                    srcFile.Package.MainNamespace = ns;
                }
            }
            else
            {
                memberscope.Peek().Members.Add(ns);
            }
            if (var_stack.Count != 0)
                throw new Exception("错啦");

            ns_stack.Push(ns);
        }

        void QUIT_NameSpace(ParseExpr node)
        { 
            ns_stack.Pop();
        }


        void ENTER_ACCESS_KEYWORD(ParseExpr node)
        {
            if (node.Nodes[0].GrammerLeftNode.Type == ParseNodeType.terminal)
            {
                access_attributes.Peek().Add( new Tuple<string, ParseExpr>( ParseExpr.getNodeValue(node),node));
            }
            else 
            {
                //throw new NotImplementedException();
            }
        }

        void ENTER_Stmt(ParseExpr node)
        {
            if (node.SelectGrammerLine.Derivation[0].Name == "ACCESS_MEMBER")
            {
                access_attributes.Push(new List<Tuple<string, ParseExpr>>());

                access_member.Push(node);
            }
        }
        void QUIT_Stmt(ParseExpr node)
        {
            if (node.SelectGrammerLine.Derivation[0].Name == "ACCESS_MEMBER")
            {
                access_member.Pop();

                access_attributes.Pop();
            }
        }

        void ENTER_ACCESS_MEMBER_KEYWORD(ParseExpr node)
        {
            access_attributes.Peek().Add( new Tuple<string, ParseExpr>( ParseExpr.getNodeValue(node),node));
        }


        void ENTER_ACCESS_MEMBER(ParseExpr node)
        {
            if (access_attributes.Peek().Count > 0)
            {
                if (node.SelectGrammerLine.Derivation[0].Name == "ExpressionList")
                {
                    if (node.MatchedToken.StringValue != "function")
                    {
                        throw new SyntaxException(node.MatchedToken, "'" + node.MatchedToken.StringValue + "' is not allowed here\r\n");
                    }
                }
            }

        }

        void QUIT_ACCESS_MEMBER(ParseExpr node)
        {
            
        }


        //function 是否匿名--- 被()括起来 例如 if(XXX),传参数等  ，或者处于 = 赋值号右边可以匿名，并且会忽略命名。
        //否则，必须要有名字。
        Stack<bool> flag_fun_anonymous = new Stack<bool>();

        void ENTER_Stmts(ParseExpr node)
        { 
            flag_fun_anonymous.Push(false);

        }

        void QUIT_Stmts(ParseExpr node)
        {
            flag_fun_anonymous.Pop();

		}

		Stack<bool> flag_fun_notallow = new Stack<bool>();


		//Stack<ParseExpr> current_visit_Assing = new Stack<ParseExpr>();
		void ENTER_Assigning(ParseExpr node)
        {
            //current_visit_Assing.Push(node);

            if (node.Nodes[1].Nodes.Count > 0)
            {
                unit_is_right.Push(false);
            }

        }

        void QUIT_Assigning(ParseExpr node)
        {
			if (node.Nodes[1].Nodes.Count > 0)
			{
                unit_is_right.Pop();
			}
			//current_visit_Assing.Pop();
        }

       

       


		void ENTER_AssigningOpt(ParseExpr node)
        { 
            flag_fun_anonymous.Push(true);

            if (node.SelectGrammerLine.Derivation.Count > 1)
            {
                if (node.SelectGrammerLine.Derivation[0].Name == "||=" || node.SelectGrammerLine.Derivation[0].Name == "&&=")
                {
                    expr_steps.Push(new List<AS3ExprStep>()); //会在QUIT_AssigningOpt的相应情况下Pop。
				}

                enter_events.Add(node.Nodes[1], (n => { flag_fun_notallow.Push(false);  }));
                quit_events.Add(node.Nodes[1], (n => { flag_fun_notallow.Pop();  }));


			}

        }

        void QUIT_AssigningOpt(ParseExpr node)
        { 
            flag_fun_anonymous.Pop();

            if (node.Nodes.Count > 0)
            {
                var arg2 = parseing_units.Peek().Pop();
                var arg1 = parseing_units.Peek().Pop();

                var expression = parseing_expression.Peek();

                

                AS3ExprStep step = new AS3ExprStep(node.MatchedToken);
                step.Type = OpType.Assigning;
                step.OpCode = node.MatchedToken.StringValue;
                step.Arg1 = arg1;
                step.Arg2 = arg2;

                if (step.OpCode == "=")
                {
					var steps = expr_steps.Peek();
					if (steps.Count > 0)
                    {
                        var per = steps[steps.Count - 1];

                        if (per.OpCode == "=" && step.OpCode == "=")
                        {
                            // A = B = 0; 这样的连续赋值;
                            if (per.Arg1 == step.Arg2)
                            {
                                step.Arg2 = per.Arg2;
                            }

                        }

                    }
                }
                

                if (step.OpCode.IndexOf("=") <= 0)
                {
					var steps = expr_steps.Peek(); //new List<AS3ExprStep>();
					steps.Add(step);
					parseing_units.Peek().Push(step.Arg2);
                }
                else // += ,-= ...
                {
					
					step.Arg3 = arg2;
                    step.Arg2 = arg1;
                    step.Arg1 = AS3DataStackElement.MakeReg(memberscope.Peek().NextRegId());

                    
                    string op = step.OpCode.Substring(0, step.OpCode.IndexOf("=") );
                    step.OpCode = op;
                    if (op == "+" || op == "-")
                    {
						var steps = expr_steps.Peek(); //new List<AS3ExprStep>();
						step.Type = OpType.Plus;
						steps.Add(step);
					}
                    else if (op == "*" || op == "/" || op == "%")
                    {
						var steps = expr_steps.Peek(); //new List<AS3ExprStep>();
						step.Type = OpType.Multiply;
						steps.Add(step);
					}
                    else if (op == "<<" || op == ">>" || op == ">>>")
                    {
						var steps = expr_steps.Peek(); //new List<AS3ExprStep>();
						step.Type = OpType.BitShift;
						steps.Add(step);
					}
                    else if (op == "&")
                    {
						var steps = expr_steps.Peek(); //new List<AS3ExprStep>();
						step.Type = OpType.BitAnd;
						steps.Add(step);
					}
                    else if (op == "|")
                    {
						var steps = expr_steps.Peek(); //new List<AS3ExprStep>();
						step.Type = OpType.BitOr;
						steps.Add(step);
					}
                    else if (op == "^")
                    {
						var steps = expr_steps.Peek(); //new List<AS3ExprStep>();
						step.Type = OpType.BitXor;
						steps.Add(step);
					}
                    else if (op == "||")
                    {
                        var right_steps = expr_steps.Pop();

						var flag = new AS3ExprStep(node.MatchedToken);
						flag.Type = OpType.Flag;
						flag.OpCode = "logicOr" + memberscope.Peek().GetFlagId() + "_true_";

                        var check = arg1;

						var set_v = new AS3ExprStep(node.MatchedToken);
						set_v.Arg1 = AS3DataStackElement.MakeReg(memberscope.Peek().NextRegId());
						set_v.Arg2 = check;
						set_v.Type = OpType.Assigning;
						set_v.OpCode = "={||}";

						var if_true_jum = new AS3ExprStep(node.MatchedToken);
						if_true_jum.Arg1 = set_v.Arg1;
						if_true_jum.Type = OpType.IF_True_Goto;
						if_true_jum.OpCode = flag.OpCode;

						expr_steps.Peek().Add(set_v);
						expr_steps.Peek().Add(if_true_jum);

                        expr_steps.Peek().AddRange(right_steps);

						var false_part = new AS3ExprStep(node.MatchedToken);
                        false_part.Type = OpType.Assigning;
                        false_part.OpCode = "={||}";
                        false_part.Arg1 = set_v.Arg1;
                        false_part.Arg2 = arg2;

                        expr_steps.Peek().Add(false_part);
                        expr_steps.Peek().Add(flag);

                        arg1 = check;

                        step.Arg1 = set_v.Arg1;

					}
                    else if (op == "&&")
                    {
						var right_steps = expr_steps.Pop();

						var flag = new AS3ExprStep(node.MatchedToken);
						flag.Type = OpType.Flag;
						flag.OpCode = "logicAnd" + memberscope.Peek().GetFlagId() + "_false_";

						logic_flag.Push(flag);


						var check = arg1;
						var set_v = new AS3ExprStep(node.MatchedToken);
						set_v.Arg1 = AS3DataStackElement.MakeReg(memberscope.Peek().NextRegId());
						set_v.Arg2 = check;
						set_v.Type = OpType.Assigning;
						set_v.OpCode = "={&&}";

						var if_false_jum = new AS3ExprStep(node.MatchedToken);
						if_false_jum.Arg1 = set_v.Arg1;
						if_false_jum.Type = OpType.IF_False_Goto;
						if_false_jum.OpCode = flag.OpCode;


						expr_steps.Peek().Add(set_v);
						expr_steps.Peek().Add(if_false_jum);
                        expr_steps.Peek().AddRange(right_steps);

						var true_part = new AS3ExprStep(node.MatchedToken);
						true_part.Type = OpType.Assigning;
						true_part.OpCode = "={&&}";
						true_part.Arg1 = set_v.Arg1;
						true_part.Arg2 = arg2;

						expr_steps.Peek().Add(true_part);
						expr_steps.Peek().Add(flag);

						arg1 = check;

						step.Arg1 = set_v.Arg1;


					}
                    else
                    {
                        throw new InvalidOperationException();
                    }

                    if(true) // 最后步骤
                    {
                        var steps = expr_steps.Peek(); //new List<AS3ExprStep>();
                        AS3ExprStep s2 = new AS3ExprStep(node.MatchedToken);
                        s2.Type = OpType.Assigning;
                        s2.OpCode = "=";
                        s2.Arg1 = arg1;
                        s2.Arg2 = step.Arg1;

                        steps.Add(s2);

                        parseing_units.Peek().Push(step.Arg1);
                    }
				}
                
            }
        }

        Stack<Tuple<ParseExpr, List<AS3DataStackElement>>> receive_elements = new Stack<Tuple<ParseExpr, List<AS3DataStackElement>>>();

        void __enter_child_Expression()
        {
            code_stack.Push(new List<IAS3SyntaxNode>());
        }

        void __quit_child_Expression(ParseExpr node)
        {
            bool isreceive=false;
            var expressions = code_stack.Pop();
            if (receive_elements.Count > 0 && receive_elements.Peek().Item1 == node)
            {
                isreceive=true;
                for (int i = 0; i < expressions.Count; i++)
                {
                    receive_elements.Peek().Item2.Add(((AS3Expression)expressions[i]).Value );
                }
            }

            if (expressions.Count > 0)
            {


                var codes = code_stack.Peek();
                for (int i = 0; i < expressions.Count; i++)
                {
                    //codes.Add((AS3Expression)expressions[i]);
                    var expression = (AS3Expression)expressions[i];
                    expr_steps.Peek().AddRange(expression.exprStepList);

                    if (!isreceive && expression.exprStepList.Count == 0 && i < expressions.Count - 1)
                    {
                        AS3ExprStep load = new AS3ExprStep(expression.Value.Data.token);
                        load.Type = OpType.Load;
                        load.OpCode = "Ld";
                        load.Arg2 = expression.Value;

                        expr_steps.Peek().Add(load);
                    }


                }

                parseing_units.Peek().Push(((AS3Expression)expressions[expressions.Count - 1]).Value);
            }
            else
            {
                throw new InvalidOperationException("no elements?");
            }
        }



        void ENTER_AExprList(ParseExpr node)
        {
            flag_fun_anonymous.Push(true);

            __enter_child_Expression();
        }

        void QUIT_AExprList(ParseExpr node)
        {

            __quit_child_Expression(node);


            flag_fun_anonymous.Pop();



        }

        void ENTER_ACCESS_DEF(ParseExpr node)
        {
            //如果匹配了一个Function。则给这个Function一个临时AS3Expression保存值，实际上这个值没有用。
            parseing_expression.Push(new AS3Expression(null));
            parseing_units.Push(new Stack<AS3DataStackElement>());

        }

        void QUIT_ACCESS_DEF(ParseExpr node)
        {
            parseing_units.Pop();
            parseing_expression.Pop();
        }


        void ENTER_ObjMember(ParseExpr node)
        {
            if (node.SelectGrammerLine.Derivation[0].Name == "useless_label")
            {
				enter_events.Add(node.Nodes[1], (n) =>
				{
					flag_fun_anonymous.Push(true);

				});

				quit_events.Add(
					node.Nodes[1], (n) =>
					{
						flag_fun_anonymous.Pop();
					}
					);
			}
            else if (node.SelectGrammerLine.Derivation[2].Name == "Assigning")
            {
                enter_events.Add(node.Nodes[2], (n) =>
                {
                    flag_fun_anonymous.Push(true);

                });

                quit_events.Add(
					node.Nodes[2], (n) =>
					{
                        flag_fun_anonymous.Pop();
					}
					);

            }

        }



		Stack<AS3Function> function_stack = new Stack<AS3Function>();

        Dictionary<ParseExpr, AS3Function> dictFunctionNode = new Dictionary<ParseExpr, AS3Function>();
        void ENTER_Function(ParseExpr node)
        {
            AS3Function function = new AS3Function(node.MatchedToken);
            srcFile._functions.Add(function);

            if (memberscope.Count == 0)
            {
                throw new SyntaxException(node.MatchedToken, "'function' is not allowed here");
            }

            if (flag_fun_notallow.Count > 0 && flag_fun_notallow.Peek())
            {
				throw new SyntaxException(node.MatchedToken, "'function' is not allowed here");
			}

            var accesslist = access_attributes.Peek();


			
			if (memberscope.Peek() is AS3Interface)
			{
                if (accesslist.Count > 0)
                {
                    throw new SyntaxException(node.MatchedToken, "Members of an interface cannot be declared public, private, protected, or internal.");
	            }
            }



			AS3Access access = new AS3Access();
            access.Set(accesslist);

            function.FunctionScope.ParentScope = memberscope.Peek();
            function.Access = access;

            var fprop = node.Nodes[1];
            if (fprop.Nodes.Count == 1)
            {
                function.Name = ParseExpr.getNodeValue(fprop.Nodes[0]);

				if (function.Name == "null" || function.Name == "undefined")
				{
					throw new SyntaxException(node.MatchedToken, $"'{function.Name}' is not allowed here.");
				}

			}
            else
            {
                var name = ParseExpr.getNodeValue(fprop.Nodes[1]);

                if (!string.IsNullOrEmpty(name))
                {
                    if (fprop.Nodes[0].MatchedToken.StringValue == "get")
                    {
                        function.IsGet = true;
                    }
                    if (fprop.Nodes[0].MatchedToken.StringValue == "set")
                    {
                        function.IsSet = true;
                    }

                    function.Name = name;
                }
                else
                {
                    function.Name = fprop.Nodes[0].MatchedToken.StringValue;
                }

            }

            if (access.IsNative && memberscope.Peek() is AS3Function.AS3FunctionScope)
            {
                throw new SyntaxException(node.MatchedToken, "native is not allowed here.");
            }
            if (accesslist.Count > 0 && memberscope.Peek() is AS3Function.AS3FunctionScope)
            {
                throw new SyntaxException(node.MatchedToken, "Access modifier not allowed on declarations inside a function.");
            }

            //if (access.IsStatic && memberscope.Peek() is AS3Interface)
            //{
            //    throw new SyntaxException(node.MatchedToken, "The static attribute can only be used on a method defined in a class.");
            //}

            //if (access.IsOverride && memberscope.Peek() is AS3Interface)
            //{
            //    throw new SyntaxException(node.MatchedToken, "The override attribute can only be used on a method defined in a class.");
            //}

            //if (access.IsFinal && memberscope.Peek() is AS3Interface)
            //{
            //    throw new SyntaxException(node.MatchedToken, "The final attribute can only be used on a method defined in a class.");
            //}


            //if (accesslist.Count > 0 && memberscope.Peek() is AS3Interface)
            //{
            //    throw new SyntaxException(node.MatchedToken, "Members of an interface cannot be declared public, private, protected, or internal.");
            //}

            if (flag_fun_anonymous.Count > 0 && flag_fun_anonymous.Peek())
            {
                function.IsAnonymous=true;
                if (memberscope.Peek() is AS3Package.PackageMemberScope)
                { 
                    function.IsAtPackageMemberScope = true;
                }
               
            }
            else
            {
                if (string.IsNullOrEmpty(function.Name))
                {
                    throw new SyntaxException(node.MatchedToken, "'function' is not allowed here");
                }
                else if (memberscope.Peek() is AS3Package.PackageMemberScope)
                {
                    throw new SyntaxException(function.Token, "An externally-visible definition with the name '" + function.Name + "' was unexpectedly found.");
                }

            }

            var typenode = node.Nodes[5];
            if (typenode.Nodes.Count == 0)
            {
                function.TypeStr = "*";
            }
            else if (typenode.Nodes.Count == 2)
            {
                function.TypeStr = ParseExpr.getNodeValue(typenode.Nodes[1]);
            }
            else
            {
                function.TypeStr = typenode.MatchedToken.StringValue.Substring(1);
            }


			if (!(memberscope.Peek() is temp_memberscope))
			{
				if (catch_scope.Count > 0)
				{
					//var cache_V = catch_scope.Peek();

                    //if (cache_V.Token.StringValue == function.Name)
                    if(catch_scope.Any( v => v.Token.StringValue == function.Name ))
                    {
                        var cache_V = catch_scope.First(v=>v.Token.StringValue == function.Name);

						
                        AS3Variable nv = new AS3Variable(function.Token);
                        nv.TypeStr = "*";
                        nv.Name = function.Name;
                        nv.Access = new AS3Access();
                        memberscope.Peek().Members.Add(nv);
                        //Flash AIR中还需要在外部生成一个undefined的变量

                        function.Name = cache_V.Name + "@--";



                    }

                }
			}

            if (catch_scope.Count > 0)
            {
                function.FunctionScope.catch_variables.AddRange( catch_scope );
            }


			if (memberscope.Peek() is AS3Class || memberscope.Peek() is AS3Interface)
            {
                


                if (!function.IsAnonymous)
                {
                    function.IsMethod = true;


                    if (memberscope.Peek() is AS3Class)
                    {
                        if (function.Name == ((AS3Class)memberscope.Peek()).Name)
                        {
                            function.IsConstructor = true;
                        }
                    }
                    extract_namespace(access, function.Token.line);
                    extract_metas(function.Metas);
                    access.CheckNSException();

                    memberscope.Peek().Members.Add(function);
                }
                else
                {
                    if (memberscope.Peek() is AS3Interface)
                    {
                        throw new SyntaxException(node.MatchedToken, "'AnonymousFunction' is not allowed here");
                    }

                    //srcFile.OutPackage.Members.Add(function);

                    ((AS3Class)memberscope.Peek()).CAnonymousFunction.Add(function);

                    function.ClosureId = ((AS3Class)memberscope.Peek()).GetClosureId();
                }

                if (memberscope.Peek() is AS3Interface)
                {
                    if (node.Nodes[6].Nodes.Count > 0)
                    {
                        throw new SyntaxException(node.MatchedToken, "Methods defined in an interface must not have a body.");
                    }
                    if (access.NameSpaceToken != null)
                    {
                        throw new SyntaxException(node.MatchedToken, "Namespace attributes are not permitted on interface methods.");
                    }
                }

                if (access.IsNative)
                {
                    if (node.Nodes[6].Nodes.Count > 0)
                    {
                        throw new SyntaxException(node.MatchedToken, "Native methods cannot have a body.");
                    }
                }

               

            }
            else
            {
                function.IsMethod = false;

                function.ClosureId = memberscope.Peek().GetClosureId();           
                memberscope.Peek().Members.Add(function);

                extract_metas(function.Metas);
            }

            if (function.IsConstructor)
            {
                if (function.Access.IsStatic)
                {
                    throw new SyntaxException(function.Token, "Constructor functions must be instance methods");
                }

                if (function.Access.IsPrivate || function.Access.IsInternal || function.Access.IsProtected || function.Access.IsOverride)
                {
                    throw new SyntaxException(function.Token, "A constructor can only be declared public");
                }

                if (typenode.Nodes.Count == 0)
                {
                    function.TypeStr = "void";
                }

                //if (typenode.Nodes.Count > 0)
                //{
                //    throw new SyntaxException(function.Token, "A Constructor cannot specify a return type");
                //}

            }


            accesslist.Clear();

            memberscope.Push(function.FunctionScope);
            
            function_stack.Push(function);

            dictFunctionNode.Add(node,function);

            code_stack.Push(function.FunctionScope.Codes);

        }

        void QUIT_Function(ParseExpr node)
        {
            code_stack.Pop();

            var function = function_stack.Pop();

            parseing_units.Peek().Push(
            new AS3DataStackElement() 
            { 
                Data = new AS3DataValue(node.MatchedToken) 
                { 
                    FF1Type = FF1DataValueType.as3_function, 
                    Value=function } , 
                IsReg = false,
                Reg = null 
            });

            memberscope.Pop();
        }


        void ENTER_Parameter(ParseExpr node)
        {
            var function = function_stack.Peek();

            AS3Parameter parameter = new AS3Parameter(node.MatchedToken);
            if (node.Nodes.Count == 3)
            {
                parameter.Name = node.Nodes[0].MatchedToken.StringValue;

                var ptype = ParseExpr.getNodeValue(node.Nodes[1]);
                if (!string.IsNullOrEmpty(ptype))
                {
                    parameter.TypeStr = ptype.Substring(1);
                }
            }
            else
            { 
                parameter.IsArrPara = true;
                parameter.Name = ParseExpr.getNodeValue( node.Nodes[0].Nodes[1]);

                var ptype = ParseExpr.getNodeValue(node.Nodes[0].Nodes[2]);
                if (!string.IsNullOrEmpty(ptype))
                {
                    parameter.TypeStr = ptype.Substring(1);
                }
            }

            function.Parameters.Add(parameter);
        }

        void ENTER_ParameterDefaultValue(ParseExpr node)
        {
            access_attributes.Peek().Clear();
            code_stack.Push(new List<IAS3SyntaxNode>());
        }

        void QUIT_ParameterDefaultValue(ParseExpr node)
        {
            var exprlist = code_stack.Pop();
            if (exprlist.Count > 1)
            {
                throw new Exception("错了");
            }

            if (exprlist.Count == 1)
            {
                function_stack.Peek().Parameters[function_stack.Peek().Parameters.Count - 1].ValueExpr = (AS3Expression)exprlist[0];
            }

        }



        Stack<AS3Variable> var_stack = new Stack<AS3Variable>();
        void ENTER_VariableDefine(ParseExpr node)
        {
            if (memberscope.Peek() is AS3Package.PackageMemberScope )
            {
                throw new SyntaxException(node.MatchedToken, "An externally-visible definition with the name '" + ParseExpr.getNodeValue( node.Nodes[0]) + "' was unexpectedly found.");
            }

            var accesslist = access_attributes.Peek();

            if (accesslist.Count > 0 && memberscope.Peek() is AS3Function.AS3FunctionScope)
            {
                throw new SyntaxException(node.MatchedToken, "Access modifier not allowed on declarations inside a function.");
            }

            //if (memberscope.Peek() == srcFile.OutPackage)
            //{
            //    foreach (var attribute in accesslist)
            //    {
            //        if (attribute == "public")
            //        {
            //            throw new SyntaxException(node.MatchedToken, "The public attribute can only be used inside a package.");
            //        }
            //        if (attribute == "private")
            //        {
            //            throw new SyntaxException(node.MatchedToken, "The private attribute may be used only on class property definitions.");
            //        }
            //        if (attribute == "static")
            //        {
            //            throw new SyntaxException(node.MatchedToken, "The static attribute may be used only on definitions inside a class.");
            //        }
            //        if (attribute == "final")
            //        {
            //            throw new SyntaxException(node.MatchedToken, "The attribute final can only be used on a method defined in a class.");
            //        }
            //        if (attribute == "override")
            //        {
            //            throw new SyntaxException(node.MatchedToken, "The override attribute may be used only on class property definitions.");
            //        }
            //        if (attribute == "protected")
            //        {
            //            throw new SyntaxException(node.MatchedToken, "The protected attribute may be used only on class property definitions.");
            //        }
            //        if (attribute == "dynamic")
            //        {
            //            throw new SyntaxException(node.MatchedToken, "The dynamic attribute can only be used with class definitions.");
            //        }
            //    }
            //}


            AS3Access access = new AS3Access();
            access.Set(accesslist);

            if (access.IsNative)
            {
                throw new SyntaxException(node.MatchedToken, "Variables cannot be NATIVE.");
            }

           

            AS3Variable variable = new AS3Variable(node.MatchedToken);
            variable.Access = access;
            variable.Name = ParseExpr.getNodeValue(node.Nodes[0]);

            if (variable.Name == "null" || variable.Name == "undefined")
            {
				throw new SyntaxException(node.MatchedToken, $"Expected IDENTIFIER but got '{variable.Name}'.");
			}
           
            if (memberscope.Peek() is AS3Interface)
            {
                throw new SyntaxException(node.MatchedToken, "A 'var' declaration is not permitted in an interface.");
            }

            extract_namespace(access, variable.Token.line);
            if (memberscope.Peek() is AS3ClassInterfaceBase)
            {
                
                extract_metas(variable.Metas);
                access.CheckNSException();
            }


            if (access.NameSpaceToken != null && memberscope.Peek() is AS3Function.AS3FunctionScope)
            {
                throw new SyntaxException(node.MatchedToken, "Namespace override not allowed on declarations inside a function.");
            }

            if (access.NameSpaceToken != null && !(memberscope.Peek() is AS3Class))
            {
                throw new SyntaxException(node.MatchedToken, "A user-defined namespace attribute can only be used at the top level of a class definition.");
            }

            if (!(memberscope.Peek() is temp_memberscope))
            {
                if (catch_scope.Count > 0)
                {
                    var cache_V = catch_scope.Peek();

                    if (cache_V.Token.StringValue == variable.Name) //阻止变量泄露，使得就是catch的变量
                    {
						AS3Variable nv = new AS3Variable(variable.Token);
						nv.TypeStr = "*";
						nv.Name = variable.Name;
						nv.Access = new AS3Access();
						memberscope.Peek().Members.Add(nv);

                        //variable.Name = cache_V.Name;
                        variable.Name = cache_V.Name;// + "@--";
					}
                }
            }

            memberscope.Peek().Members.Add(variable);

            
            var_stack.Push(variable);
            //code_stack.Push(new List<IAS3SyntaxNode>());
            
        }

        void ENTER_VariableDefaultValue(ParseExpr node)
        {
            if (node.Nodes.Count > 0)
            {
                flag_fun_anonymous.Push(true);
                access_attributes.Push(new List<Tuple<string, ParseExpr>>());

                if (memberscope.Peek() is AS3Class)
                {
                    var variable = var_stack.Peek();
                    if (variable.Access.IsStatic)
                    {
                        code_stack.Push(((AS3Class)memberscope.Peek()).CInitCodes);
                    }
                    else
                    {
                        code_stack.Push(((AS3Class)memberscope.Peek()).Codes);
                    }
                }

            }
        }

        void QUIT_VariableDefaultValue(ParseExpr node)
        {
            var variable = var_stack.Peek();
            if (node.Nodes.Count > 0)
            {
                AS3Expression expression = (AS3Expression)code_stack.Peek()[code_stack.Peek().Count - 1];

                variable.ValueExpr = expression;

                AS3ExprStep step = new AS3ExprStep(expression.Token);
                step.OpCode = "=";
                step.Type = OpType.Assigning;
                step.Arg1 = new AS3DataStackElement()
                {
                    Data = new AS3DataValue(variable.Token)
                    {
                        FF1Type = FF1DataValueType.identifier,
                        Value = "{" + variable.Access.ToString().TrimEnd() + "::}" + variable.Name
                    }
                };
                step.Arg2 = expression.Value;

                expression.exprStepList.Add(step);

                if (memberscope.Peek() is AS3Class)
                {
                    code_stack.Pop();
                }

                access_attributes.Pop();
                flag_fun_anonymous.Pop();

            }

        }

        void QUIT_VariableDefine(ParseExpr node)
        {
            var_stack.Pop();
        }

        void ENTER_VariableType(ParseExpr node)
        { 
            var_stack.Peek().TypeStr = ParseExpr.getNodeValue(node);
        }


		Stack<ParseExpr> current_visit_access = new Stack<ParseExpr>();

		Dictionary<ParseExpr, ParseExpr> dictA_FC_check = new Dictionary<ParseExpr, ParseExpr>(); 
        void ENTER_Access(ParseExpr node)
        {
            if (node.Nodes[0].SelectGrammerLine.Main.Name == "Function")
            {
                dictA_FC_check.Add(node.Nodes[1], node.Nodes[0]);
            }

            current_visit_access.Push(node);

            if (node.Nodes[1].SelectGrammerLine.Main.Name == "AccessOpt")
            {
                if (node.Nodes[1].SelectGrammerLine.Derivation.Count > 1)
                {
                    if (node.Nodes[1].SelectGrammerLine.Derivation[0].Name == "Call")
                    { 
                        unit_is_right.Push(false);
                    }
				}

            }


		}

        void QUIT_Access(ParseExpr node)
        {
			if (node.Nodes[1].SelectGrammerLine.Main.Name == "AccessOpt")
			{
				if (node.Nodes[1].SelectGrammerLine.Derivation.Count > 1)
				{
					if (node.Nodes[1].SelectGrammerLine.Derivation[0].Name == "Call")
					{
                        unit_is_right.Pop();
					}
				}

			}

			current_visit_access.Pop();
        }
       

        void QUIT_A_FC(ParseExpr node)
        {
            if (node.Nodes.Count != 0)
            {
                //检查是否允许就地调用函数
                var function = dictFunctionNode[dictA_FC_check[node]];
                
                var args = node.Nodes[0].Nodes[1];
                if (args.Nodes.Count == 0)
                {
                    if (!function.IsAnonymous)
                    {
                        throw new SyntaxException(node.MatchedToken, "')' is not allowed here");
                    }
                    else
                    {
                        //call 

                        var codes = code_stack.Peek();



                    }
                }
                else
                {
                    if (function.IsAnonymous)
                    {
                        //call

                        var codes = code_stack.Peek();


                    }
                    else
                    {
                        //do nothing
                    }
                }


            }

        }



        Stack<AS3Const> const_stack = new Stack<AS3Const>();

        void ENTER_ConstDefine(ParseExpr node)
        {
            if (memberscope.Peek() is AS3Package.PackageMemberScope)
            {
                throw new SyntaxException(node.MatchedToken, "An externally-visible definition with the name '" + ParseExpr.getNodeValue(node.Nodes[0]) + "' was unexpectedly found.");
            }
            var accesslist = access_attributes.Peek();

            if (accesslist.Count > 0 && memberscope.Peek() is AS3Function.AS3FunctionScope)
            {
                throw new SyntaxException(node.MatchedToken, "Access modifier not allowed on declarations inside a function.");
            }

            AS3Access access = new AS3Access();
            access.Set(accesslist);

            if (access.IsNative)
            {
                throw new SyntaxException(node.MatchedToken, "Variables cannot be NATIVE.");
            }

            AS3Const @const = new AS3Const(node.MatchedToken);
            @const.Access = access;
            @const.Name = ParseExpr.getNodeValue(node.Nodes[0]);

			if (@const.Name == "null" || @const.Name == "undefined")
			{
				throw new SyntaxException(node.MatchedToken, $"Expected IDENTIFIER but got '{@const.Name}'.");
			}


			if (memberscope.Peek() is AS3Interface)
            {
                throw new SyntaxException(node.MatchedToken, "A 'const' declaration is not permitted in an interface.");
            }

            extract_namespace(access, @const.Token.line);
            if (memberscope.Peek() is AS3ClassInterfaceBase)
            {
                
                extract_metas(@const.Metas);
                access.CheckNSException();
            }

            if (access.NameSpaceToken != null && memberscope.Peek() is AS3Function.AS3FunctionScope)
            {
                throw new SyntaxException(node.MatchedToken, "Namespace override not allowed on declarations inside a function.");
            }

            if (access.NameSpaceToken != null && !(memberscope.Peek() is AS3Class))
            {
                throw new SyntaxException(node.MatchedToken, "A user-defined namespace attribute can only be used at the top level of a class definition.");
            }


            memberscope.Peek().Members.Add(@const);
            
            const_stack.Push(@const);
        }

        void QUIT_ConstDefine(ParseExpr node)
        { 
            const_stack.Pop();
        }

        void ENTER_ConstType(ParseExpr node)
        {
            const_stack.Peek().TypeStr = ParseExpr.getNodeValue(node);
        }

        void ENTER_ConstDefaultValue(ParseExpr node)
        {
            if (node.Nodes.Count > 0)
            {
                flag_fun_anonymous.Push(true);
                access_attributes.Push(new List<Tuple<string, ParseExpr>>());

                if (memberscope.Peek() is AS3Class)
                {
                    var @const = const_stack.Peek();
                    if (@const.Access.IsStatic)
                    {
                        code_stack.Push(((AS3Class)memberscope.Peek()).CInitCodes);
                    }
                    else
                    {
                        code_stack.Push(((AS3Class)memberscope.Peek()).Codes);
                    }
                }

            }
            else
            {
                throw new SyntaxException(node.MatchedToken, "Missing initializer in const declaration");
            }
        }

        void QUIT_ConstDefaultValue(ParseExpr node)
        {
            var @const = const_stack.Peek();
            if (node.Nodes.Count > 0)
            {
                AS3Expression expression = (AS3Expression)code_stack.Peek()[code_stack.Peek().Count - 1];

                @const.ValueExpr = expression;

                AS3ExprStep step = new AS3ExprStep(expression.Token);
                step.OpCode = "=";
                step.Type = OpType.Assigning;
                step.Arg1 = new AS3DataStackElement()
                {
                    Data = new AS3DataValue(@const.Token)
                    {
                        FF1Type = FF1DataValueType.identifier,
                        Value = "{" + @const.Access.ToString().TrimEnd() + "::}" + @const.Name
                    }
                };
                step.Arg2 = expression.Value;

                expression.exprStepList.Add(step);

                if (memberscope.Peek() is AS3Class)
                {
                    code_stack.Pop();
                }

                access_attributes.Pop();
                flag_fun_anonymous.Pop();
            }
        }




        Stack<AS3Expression> parseing_expression = new Stack<AS3Expression>();

        Dictionary<ParseExpr,AS3Expression> dictNodeExpression = new Dictionary<ParseExpr, AS3Expression> ();


        Stack<ParseExpr> current_new_operator = new Stack<ParseExpr>();
        Stack<int> new_operator_count = new Stack<int> ();

        

		void ENTER_Unit(ParseExpr node)
        {
            
            if (node.Nodes[0].GrammerLeftNode.Name == "new" && node.Nodes[0].GrammerLeftNode.Type == ParseNodeType.terminal)
            {
                current_new_operator.Push(node.Nodes[1]);
                new_operator_count.Push(current_new_operator.Count);

				enter_events.Add(node.Nodes[1], (n) =>
				{
					unit_is_right.Push(false);
				});

				quit_events.Add(node.Nodes[1], (n) =>
				{
					unit_is_right.Pop();
				});

			}

           
        }


        Stack<bool> unit_is_right = new Stack<bool> ();

        void QUIT_Unit(ParseExpr node)
        {
            
            if (node.Nodes[0].GrammerLeftNode.Type == ParseNodeType.identifier
                ||
                node.Nodes[0].GrammerLeftNode.Name == "ID_EABLED_KEYWORD"
                )
            {
                bool isright = unit_is_right.Peek();
                
                if (isright)
                {
                    var unit = new AS3DataStackElement()
                    {
                        Data = new AS3DataValue(node.MatchedToken)
                        {
                            FF1Type = FF1DataValueType.identifier,
                            Value = node.Nodes[0].MatchedToken.StringValue
                        },
                    };

                    //var express = new AS3Expression(node.MatchedToken);
                    //express.Value = AS3DataStackElement.MakeReg(memberscope.Peek().NextRegId());
                    //express.exprStepList = new List<AS3ExprStep>();


                    AS3ExprStep load = new AS3ExprStep(node.MatchedToken);
                    load.Type = OpType.Load;
                    load.OpCode = "Ld_R";
                    load.Arg1 = AS3DataStackElement.MakeReg(memberscope.Peek().NextRegId())
                    ;
                    load.Arg2 = unit;

                    load.Arg1.Reg.isLd_R = true;
                    //express.exprStepList.Add(load);

                    expr_steps.Peek().Add(load);
                    //code_stack.Peek().Add(express);

                    parseing_units.Peek().Push(load.Arg1);

                }
                else
                {
                    parseing_units.Peek().Push(
                       new AS3DataStackElement()
                       {
                           Data = new AS3DataValue(node.MatchedToken)
                           {
                               FF1Type = FF1DataValueType.identifier,
                               Value = node.Nodes[0].MatchedToken.StringValue
                           },

                       }
                       );
                }
			}
            else if (node.Nodes[0].GrammerLeftNode.Type == ParseNodeType.number)
            {

                parseing_units.Peek().Push(
                    new AS3DataStackElement()
                    {
                        Data = new AS3DataValue(node.MatchedToken)
                        {
                            FF1Type = FF1DataValueType.const_number,
                            Value = node.Nodes[0].MatchedToken.StringValue
                        },

                    }
               );
            }
            else if (node.Nodes[0].GrammerLeftNode.Type == ParseNodeType.conststring)
            {

                parseing_units.Peek().Push(
                    new AS3DataStackElement()
                    {
                        Data = new AS3DataValue(node.MatchedToken)
                        {
                            FF1Type = (node.Nodes[0].MatchedToken.Type == Token.TokenType.const_regexp ? FF1DataValueType.const_regexp : 
                            (node.Nodes[0].MatchedToken.Type == Token.TokenType.const_xml? FF1DataValueType.e4xxml: FF1DataValueType.const_string )),  //FF1DataValueType.const_string,
                            Value = node.Nodes[0].MatchedToken.StringValue
                        },

                    }
               );
            }
            else if (node.Nodes[0].GrammerLeftNode.Name == "new")
            {
                int c = new_operator_count.Pop();
                if (c == current_new_operator.Count)
                {
                    //new 没有被后续Call吃掉
                    var op = new AS3ExprStep(node.MatchedToken);
                    op.Arg1 = AS3DataStackElement.MakeReg(memberscope.Peek().NextRegId());
                    op.Arg2 = parseing_units.Peek().Pop();
                    op.Type = OpType.Constructor;
                    op.OpCode = "new";

                    expr_steps.Peek().Add(op);

                    parseing_units.Peek().Push(op.Arg1);

                    current_new_operator.Pop();
                }

            }
            else if (node.Nodes[0].GrammerLeftNode.Type == ParseNodeType.terminal && node.Nodes[0].GrammerLeftNode.Name == "CONFIG::")
            {
                parseing_units.Peek().Push(
                    new AS3DataStackElement()
                    {
                        Data = new AS3DataValue(node.MatchedToken)
                        {
                            FF1Type = FF1DataValueType.compiler_const,
                            Value = node.Nodes[1].MatchedToken.StringValue
                        },

                    }
               );
            }

        }

        Stack<Stack<AS3DataStackElement>> parseing_units = new Stack<Stack<AS3DataStackElement>>();
        Stack<List<AS3ExprStep>> expr_steps = new Stack<List<AS3ExprStep>>();

        void ENTER_ArrayElements(ParseExpr node)
        {
            if (node.Nodes.Count > 1)
            {
                receive_elements.Push(new Tuple<ParseExpr, List<AS3DataStackElement>>(node.Nodes[0], new List<AS3DataStackElement>()));
            }
            else
            {
                parseing_units.Peek().Push(
                    new AS3DataStackElement()
                    {
                        Data = new AS3DataValue(node.MatchedToken)
                        {
                            FF1Type = FF1DataValueType.as3_array,
                            Value = new List<AS3DataStackElement>()
                        },

                    }
               );
            }
        }

        void QUIT_ArrayElements(ParseExpr node)
        {
            if (node.Nodes.Count > 1)
            {
                var elements = receive_elements.Pop();

                parseing_units.Peek().Pop();

                parseing_units.Peek().Push(
                    new AS3DataStackElement()
                    {
                        Data = new AS3DataValue(node.MatchedToken)
                        {
                            FF1Type =  FF1DataValueType.as3_array,
                            Value = elements.Item2
                        },

                    }
               );

            }
        }

        Stack<List<bool>> arrayelement_require_element = new Stack<List<bool>>();

		void ENTER_ArrayExprList(ParseExpr node)
		{
			

			flag_fun_anonymous.Push(true);

			__enter_child_Expression();

            arrayelement_require_element.Push(new List<bool>());
		}

		void QUIT_ArrayExprList(ParseExpr node)
		{

			var flag = arrayelement_require_element.Pop();
			
			__quit_child_Expression(node);

			flag_fun_anonymous.Pop();

		}

        void ENTER_ArrayElem(ParseExpr node)
        {
            var flag = arrayelement_require_element.Peek();

            if (node.SelectGrammerLine.Derivation[0].Name == ",")
            {

                if (flag.Count == 0 || flag[flag.Count - 1])
                {
                    AS3Expression undefined = new AS3Expression(node.MatchedToken);

                    undefined.Value = new AS3DataStackElement()
                    {
                        Data = new AS3DataValue(node.MatchedToken)
                        {
                            FF1Type = FF1DataValueType.identifier,
                            Value = "@array_hole"
                        }
                         ,
                        IsReg = false,

                    }
                        ;
                    undefined.exprStepList = new List<AS3ExprStep>();
                    code_stack.Peek().Add(undefined);

                  
                }
               
                flag.Add(true);
                
			}
            else
            { 
                flag.Add(false);
            }
        }



		//void ENTER_ACommaOpt_1(ParseExpr node)
		//{
		//    if (node.Nodes.Count == 0)
		//    { 
		//        AS3Expression undefined = new AS3Expression(node.MatchedToken);

		//        undefined.Value = new AS3DataStackElement()
		//        {
		//            Data = new AS3DataValue(node.MatchedToken)
		//            {
		//                FF1Type = FF1DataValueType.identifier,
		//                Value = "undefined"
		//            }
		//             ,
		//            IsReg = false,

		//        }
		//            ;
		//        undefined.exprStepList = new List<AS3ExprStep>();
		//        code_stack.Peek().Add(undefined);

		//    }
		//}



		void ENTER_Argements(ParseExpr node)
        {
            if(node.Nodes.Count > 0)
            {
                receive_elements.Push(new Tuple<ParseExpr, List<AS3DataStackElement>>(node.Nodes[0], new List<AS3DataStackElement>()));
            }
        }

        void QUIT_Argements(ParseExpr node)
        {
            if(node.Nodes.Count > 0)
            {
                var elements = receive_elements.Pop();

                parseing_units.Peek().Pop();

                parseing_units.Peek().Push(
                    new AS3DataStackElement()
                    {
                        Data = new AS3DataValue(node.MatchedToken)
                        {
                            FF1Type = FF1DataValueType.as3_array,
                            Value = elements.Item2
                        },

                    }
               );
            }
        }

        void ENTER_Call(ParseExpr node)
        {

        }

        void QUIT_Call(ParseExpr node)
        {
            bool isNew = false;
            if (current_new_operator.Count > 0 && current_new_operator.Peek().Equals(current_visit_access.Peek()))
            {
                current_new_operator.Pop();
                isNew=true;
            }


            if (node.Nodes[1].Nodes.Count > 0)
            {
                var argements = parseing_units.Peek().Pop();
                var fun = parseing_units.Peek().Pop();

                var op = new AS3ExprStep(node.MatchedToken);
                if (isNew)
                {
                    op.Type = OpType.Constructor;
                    op.OpCode = "new";
                }
                else
                {
                    op.Type = OpType.CallFunc;
                    op.OpCode = node.Nodes[0].MatchedToken.StringValue;
                }

                if (!fun.IsReg && fun.Data.FF1Type == FF1DataValueType.identifier)
                {
                    /* 考虑如下代码。。。如果出现，需要先加载x,再跑后面的
                      * var x = function() {
                      *  this.foo = 42;
                      *  };
                      *
                      *  var result = new x(x = 1);
                     */

                    //if (expr_steps.Peek().Any(

                    //    s => (s.Type == OpType.Assigning && s.OpCode == "="
                    //    &&
                    //    !s.Arg1.IsReg && s.Arg1.Data.FF1Type == FF1DataValueType.identifier
                    //    &&
                    //    s.Arg1.Data.Value.ToString() == fun.Data.Value.ToString())
                    //    ||
                    //    (
                    //        s.Type == OpType.CallFunc
                    //    )
                    //    ||
                    //    (
                    //        s.Type == OpType.Constructor
                    //    )
                    //    ))
                    //   编译时先测试identifier是不是变量，如果是，则需要提前load,否则不用
                    {
						AS3ExprStep load = new AS3ExprStep(node.MatchedToken);
						load.Type = OpType.Load;
						load.OpCode = "Ld_Callable";
						load.Arg1 = AS3DataStackElement.MakeReg(memberscope.Peek().NextRegId())
						;
						load.Arg2 = fun;
                        load.Arg1.Reg.isLd_R = true;
                        load.Arg1.Reg.isLd_callee_id = true;

						expr_steps.Peek().Insert(0,load);

                        fun = load.Arg1;

					}
                }

                op.Arg1 = AS3DataStackElement.MakeReg(memberscope.Peek().NextRegId());
                op.Arg2 = fun;
                op.Arg3 = argements;

                expr_steps.Peek().Add(op);

                parseing_units.Peek().Push(op.Arg1);

            }
            else
            {
                var fun = parseing_units.Peek().Pop();


                var op = new AS3ExprStep(node.MatchedToken);
                if (isNew)
                {
                    op.Type = OpType.Constructor;
                    op.OpCode = "new";
                }
                else
                {
                    op.Type = OpType.CallFunc;
                    op.OpCode = node.Nodes[0].MatchedToken.StringValue;
                }
                op.Arg1 = AS3DataStackElement.MakeReg(memberscope.Peek().NextRegId());
                op.Arg2 = fun;
                op.Arg3 = new AS3DataStackElement()
                {
                    Data = new AS3DataValue(node.MatchedToken)
                    {
                        FF1Type = FF1DataValueType.as3_array,
                        Value = new List<AS3DataStackElement>()
                    },

                };

                expr_steps.Peek().Add(op);

                parseing_units.Peek().Push(op.Arg1);
            }

            

        }

        #region JSON部分

        Stack<Hashtable> dynamic_objects = new Stack<Hashtable> ();
        void ENTER_ObjectBody(ParseExpr node)
        { 
            dynamic_objects.Push(new Hashtable ());
        }

        void QUIT_ObjectBody(ParseExpr node)
        {
            AS3DataStackElement json = new AS3DataStackElement();
            json.Data = new AS3DataValue(node.MatchedToken) { FF1Type = FF1DataValueType.dynamicobj, Value = dynamic_objects.Pop() };
            parseing_units.Peek().Push(json);

        }


        void QUIT_ObjMember(ParseExpr node)
        {
            Hashtable obj;
            Token key;
            obj = dynamic_objects.Peek();
            if (node.Nodes.Count == 4)
            {   
                key = node.Nodes[1].MatchedToken;
            }
            else
            { 
                key = node.Nodes[0].MatchedToken;   
            }

            var value = parseing_units.Peek().Pop();
            obj.Add(key, value);

        }


        #endregion

        void QUIT_Vector(ParseExpr node)
        {
            var vector = new AS3Vector();

            if (node.Nodes.Count == 2)
            {
                vector.VectorTypeStr = ParseExpr.getNodeValue(node.Nodes[0].Nodes[1]);
                if (node.Nodes[1].Nodes.Count > 0)
                {
                    if (node.Nodes[1].Nodes[1].Nodes.Count > 0)
                    {
                        vector.Constructor = parseing_units.Peek().Pop();
                    }
                }

            }
            else
            {
                vector.isInitData = true;
                vector.VectorTypeStr = ParseExpr.getNodeValue(node.Nodes[1]);
                vector.Constructor = parseing_units.Peek().Pop();

                //必须有一个new 
                if (current_new_operator.Count > 0 && current_new_operator.Peek().Equals(current_visit_access.Peek()))
                {

                }
                else
                {
                    throw new SyntaxException(node.MatchedToken, "XML does not have matching begin and end tags.");
                }
            }


            parseing_units.Peek().Push(
                    new AS3DataStackElement()
                    {
                        Data = new AS3DataValue(node.MatchedToken)
                        {
                            FF1Type = FF1DataValueType.as3_vector,
                            Value = vector
                        }
                    }

                    );


        }

        void ENTER_ThisSuper(ParseExpr node)
        {
            

            if (node.Nodes[0].MatchedToken.StringValue == "this")
            {
                parseing_units.Peek().Push(
                    new AS3DataStackElement()
                    {
                        Data = new AS3DataValue(node.MatchedToken)
                        {
                            FF1Type = FF1DataValueType.this_pointer,
                            Value = node.Nodes[0].MatchedToken.StringValue
                        }
                    }

                    );

            }
            else
            {
                parseing_units.Peek().Push(
                    new AS3DataStackElement()
                    {
                        Data = new AS3DataValue(node.MatchedToken)
                        {
                            FF1Type = FF1DataValueType.super_pointer,
                            Value = node.Nodes[0].MatchedToken.StringValue
                        }
                    }

                    );

            }
        }

		

		void ENTER_Plus(ParseExpr node)
		{
			if (node.Nodes[1].SelectGrammerLine.Derivation.Count > 1)
			{
				enter_events.Add(node.Nodes[0], (n) => {
					unit_is_right.Push(true);
				});

				quit_events.Add(node.Nodes[0], (n) => {
					unit_is_right.Pop();
				});
			}
		}
		void ENTER_Multiply(ParseExpr node)
		{
			if (node.Nodes[1].SelectGrammerLine.Derivation.Count > 1)
			{
				enter_events.Add(node.Nodes[0], (n) => {
					unit_is_right.Push(true);
				});

				quit_events.Add(node.Nodes[0], (n) => {
					unit_is_right.Pop();
				});
			}
		}

		void ENTER_BitShift(ParseExpr node)
		{
			if (node.Nodes[1].SelectGrammerLine.Derivation.Count > 1)
			{
				enter_events.Add(node.Nodes[0], (n) => {
					unit_is_right.Push(true);
				});

				quit_events.Add(node.Nodes[0], (n) => {
					unit_is_right.Pop();
				});
			}
		}

		void ENTER_Logic(ParseExpr node)
		{
			if (node.Nodes[1].SelectGrammerLine.Derivation.Count > 1)
			{
				enter_events.Add(node.Nodes[0], (n) => {
					unit_is_right.Push(true);
				});

				quit_events.Add(node.Nodes[0], (n) => {
					unit_is_right.Pop();
				});
			}
		}

		void ENTER_LogicEQ(ParseExpr node)
		{
			if (node.Nodes[1].SelectGrammerLine.Derivation.Count > 1)
			{
				enter_events.Add(node.Nodes[0], (n) => {
					unit_is_right.Push(true);
				});

				quit_events.Add(node.Nodes[0], (n) => {
					unit_is_right.Pop();
				});
			}
		}

		void ENTER_BitAnd(ParseExpr node)
		{
			if (node.Nodes[1].SelectGrammerLine.Derivation.Count > 1)
			{
				enter_events.Add(node.Nodes[0], (n) => {
					unit_is_right.Push(true);
				});

				quit_events.Add(node.Nodes[0], (n) => {
					unit_is_right.Pop();
				});
			}
		}

		void ENTER_BitXor(ParseExpr node)
		{
			if (node.Nodes[1].SelectGrammerLine.Derivation.Count > 1)
			{
				enter_events.Add(node.Nodes[0], (n) => {
					unit_is_right.Push(true);
				});

				quit_events.Add(node.Nodes[0], (n) => {
					unit_is_right.Pop();
				});
			}
		}

		void ENTER_BitOr(ParseExpr node)
		{
			if (node.Nodes[1].SelectGrammerLine.Derivation.Count > 1)
			{
				enter_events.Add(node.Nodes[0], (n) => {
					unit_is_right.Push(true);
				});

				quit_events.Add(node.Nodes[0], (n) => {
					unit_is_right.Pop();
				});
			}
		}

		void ENTER_LogicAnd(ParseExpr node)
		{
			//if (node.Nodes[1].SelectGrammerLine.Derivation.Count > 1)
			//{
			//	enter_events.Add(node.Nodes[0], (n) => {
			//		unit_is_right.Push(true);
			//	});

			//	quit_events.Add(node.Nodes[0], (n) => {
			//		unit_is_right.Pop();
			//	});
			//}
		}




		void ENTER_Unary(ParseExpr node)
        {
            if (node.Nodes.Count == 2)
            {
                if (
                    node.SelectGrammerLine.Derivation[0].Name == "delete"
                    ||
					node.SelectGrammerLine.Derivation[0].Name == "++"
                    ||
					node.SelectGrammerLine.Derivation[0].Name == "--"
                    ||
					node.SelectGrammerLine.Derivation[0].Name == "typeof"
                    ||
					node.SelectGrammerLine.Derivation[0].Name == "void"
					)
                {
                    enter_events.Add(node.Nodes[1], (n) => {

                        unit_is_right.Push(false);
                    });

					quit_events.Add(node.Nodes[1], (n) => {

                        unit_is_right.Pop();
					});
				}
            }


			if (node.Nodes.Count > 1)
            {
                flag_fun_anonymous.Push(true);
            }
        }

        void QUIT_Unary(ParseExpr node)
        {
            if (node.Nodes.Count > 1)
            {
                flag_fun_anonymous.Pop();
            }
            if (node.Nodes.Count == 2)
            { 
                var arg2 = parseing_units.Peek().Pop();

                var expression = parseing_expression.Peek();

                var steps = expr_steps.Peek(); //new List<AS3ExprStep>();

                AS3ExprStep step = new AS3ExprStep(node.MatchedToken);
                step.Type = OpType.Unary;
                step.OpCode = node.MatchedToken.StringValue;
                step.Arg1 = AS3DataStackElement.MakeReg(memberscope.Peek().NextRegId());
                step.Arg2 = arg2;
                
                steps.Add(step);
                

                parseing_units.Peek().Push(step.Arg1);
            }
        }


        void _parse_math_expr(ParseExpr node, OpType type)
        {
			

			if (node.Parent.Nodes.Count == 3)
            {
                var arg3 = parseing_units.Peek().Pop();
                var arg2 = parseing_units.Peek().Pop();

                var expression = parseing_expression.Peek();






                var steps = expr_steps.Peek();
                AS3ExprStep step = new AS3ExprStep(node.MatchedToken);
                step.Type = type;
                step.OpCode = node.Parent.MatchedToken.StringValue;
                step.Arg1 = AS3DataStackElement.MakeReg(memberscope.Peek().NextRegId());
                step.Arg2 = arg2;
                step.Arg3 = arg3;

                steps.Add(step);


                parseing_units.Peek().Push(step.Arg1);
            }
        }

        void ENTER_PlusOpt(ParseExpr node)
        {
           _parse_math_expr(node, OpType.Plus);

        }
        void ENTER_MultiplyOpt(ParseExpr node)
        {
            _parse_math_expr(node, OpType.Multiply);
        }

        void ENTER_BitOrOpt(ParseExpr node)
        {
            _parse_math_expr(node, OpType.BitOr);
        }

        void ENTER_BitXorOpt(ParseExpr node)
        {
            _parse_math_expr(node, OpType.BitXor);
        }

        void ENTER_BitAndOpt(ParseExpr node)
        {
            _parse_math_expr(node, OpType.BitAnd);
        }

        void ENTER_LogicEQOpt(ParseExpr node)
        {
            _parse_math_expr(node, OpType.LogicEQ);
        }
        void ENTER_LogicOpt(ParseExpr node)
        {
            _parse_math_expr(node, OpType.Logic);
        }
        void ENTER_BitShiftOpt(ParseExpr node)
        {
            _parse_math_expr(node, OpType.BitShift);
        }

       

        Stack<AS3ExprStep> logic_flag = new Stack<AS3ExprStep>();
        void ENTER_LogicOrOpt(ParseExpr node)
        {
            if (node.Parent.SelectGrammerLine.Main.Name == "LogicOr" && node.Nodes.Count > 0)
            {
                var flag = new AS3ExprStep(node.MatchedToken);
                flag.Type = OpType.Flag;
                flag.OpCode = "logicOr" + memberscope.Peek().GetFlagId() + "_true_"  ;

                logic_flag.Push(flag);


                var check = parseing_units.Peek().Pop();

                var set_v = new AS3ExprStep(node.MatchedToken);
                set_v.Arg1 = AS3DataStackElement.MakeReg(memberscope.Peek().NextRegId());
                set_v.Arg2 = check;
                set_v.Type = OpType.Assigning;
                set_v.OpCode = "={||}";

                var if_true_jum = new AS3ExprStep(node.MatchedToken);
                if_true_jum.Arg1 = set_v.Arg1;
                if_true_jum.Type = OpType.IF_True_Goto;
                if_true_jum.OpCode = flag.OpCode;


                expr_steps.Peek().Add(set_v);
                expr_steps.Peek().Add(if_true_jum);

                parseing_units.Peek().Push(set_v.Arg1);

                
            }
            else if (node.Parent.SelectGrammerLine.Main.Name == "LogicOrOpt" && node.Nodes.Count == 0)
            {
                
                var arg2 = parseing_units.Peek().Pop();

                var set_v = new AS3ExprStep(node.MatchedToken);
                set_v.Arg1 = parseing_units.Peek().Peek();
                set_v.Arg2 = arg2;
                set_v.Type = OpType.Assigning;
                set_v.OpCode = "={||}";


                expr_steps.Peek().Add(set_v);
                
                var flag = logic_flag.Pop();
                expr_steps.Peek().Add(flag);

            }
            else if (node.Nodes.Count == 3)
            {
                
                var arg2 = parseing_units.Peek().Pop();


                var set_v = new AS3ExprStep(node.MatchedToken);
                set_v.Arg1 = parseing_units.Peek().Peek();
                set_v.Arg2 = arg2;
                set_v.Type = OpType.Assigning;
                set_v.OpCode = "={||}";


                var if_true_jum = new AS3ExprStep(node.MatchedToken);
                if_true_jum.Arg1 = set_v.Arg1;
                if_true_jum.Type = OpType.IF_True_Goto;
                if_true_jum.OpCode = logic_flag.Peek().OpCode;

                expr_steps.Peek().Add(set_v);
                expr_steps.Peek().Add(if_true_jum);

            }

        }

        void ENTER_LogicAndOpt(ParseExpr node)
        {
            if (node.Parent.SelectGrammerLine.Main.Name == "LogicAnd" && node.Nodes.Count > 0)
            {
                var flag = new AS3ExprStep(node.MatchedToken);
                flag.Type = OpType.Flag;
                flag.OpCode = "logicAnd" + memberscope.Peek().GetFlagId() +  "_false_";

                logic_flag.Push(flag);


                var check = parseing_units.Peek().Pop();
                var set_v = new AS3ExprStep(node.MatchedToken);
                set_v.Arg1 = AS3DataStackElement.MakeReg(memberscope.Peek().NextRegId());
                set_v.Arg2 = check;
                set_v.Type = OpType.Assigning;
                set_v.OpCode = "={&&}";

                var if_false_jum = new AS3ExprStep(node.MatchedToken);
                if_false_jum.Arg1 = set_v.Arg1;
                if_false_jum.Type = OpType.IF_False_Goto;
                if_false_jum.OpCode = flag.OpCode;


                expr_steps.Peek().Add(set_v);
                expr_steps.Peek().Add(if_false_jum);

                parseing_units.Peek().Push(set_v.Arg1);
            }
            else if (node.Parent.SelectGrammerLine.Main.Name == "LogicAndOpt" && node.Nodes.Count == 0)
            {

                var arg2 = parseing_units.Peek().Pop();

                var set_v = new AS3ExprStep(node.MatchedToken);
                set_v.Arg1 = parseing_units.Peek().Peek();
                set_v.Arg2 = arg2;
                set_v.Type = OpType.Assigning;
                set_v.OpCode = "={&&}";


                expr_steps.Peek().Add(set_v);

                var flag = logic_flag.Pop();
                expr_steps.Peek().Add(flag);

            }
            else if (node.Nodes.Count == 3)
            {

                var arg2 = parseing_units.Peek().Pop();


                var set_v = new AS3ExprStep(node.MatchedToken);
                set_v.Arg1 = parseing_units.Peek().Peek();
                set_v.Arg2 = arg2;
                set_v.Type = OpType.Assigning;
                set_v.OpCode = "={&&}";


                var if_false_jum = new AS3ExprStep(node.MatchedToken);
                if_false_jum.Arg1 = set_v.Arg1;
                if_false_jum.Type = OpType.IF_False_Goto;
                if_false_jum.OpCode = logic_flag.Peek().OpCode;

                expr_steps.Peek().Add(set_v);
                expr_steps.Peek().Add(if_false_jum);

            }
        }

        void ENTER_TernaryOpt(ParseExpr node)
        {
            if (node.Parent.SelectGrammerLine.Main.Name == "Ternary" && node.Nodes.Count > 0)
            { 
                var flagid = memberscope.Peek().GetFlagId();

                var testvalue = parseing_units.Peek().Pop();

                var outvalue = AS3DataStackElement.MakeReg(memberscope.Peek().NextRegId()); //目标值
                parseing_units.Peek().Push(outvalue);

                var flag_false = new AS3ExprStep(node.MatchedToken);
                flag_false.Type = OpType.Flag;
                flag_false.OpCode = "ternay" + flagid + "_false" ;



                var jmp = new AS3ExprStep(node.MatchedToken);
                jmp.Arg1 = testvalue;
                jmp.Type = OpType.IF_False_Goto;
                jmp.OpCode = flag_false.OpCode;

                expr_steps.Peek().Add(jmp);


                var flag_end = new AS3ExprStep(node.MatchedToken);
                flag_end.Type = OpType.Flag;
                flag_end.OpCode = "ternay" + flagid+ "_end" ;


                logic_flag.Push(flag_false);
                logic_flag.Push(flag_end);
            }
        }

        void ENTER_Ternary_True(ParseExpr node)
        {
            //__enter_child_Expression();

        }
        void QUIT_Ternary_True(ParseExpr node)
        {
            //__quit_child_Expression();
     
            var true_part_value = parseing_units.Peek().Pop();

            var set_v = new AS3ExprStep(node.MatchedToken);
            set_v.Arg1 = parseing_units.Peek().Peek();
            set_v.Arg2 = true_part_value;
            set_v.Type = OpType.Assigning;
            set_v.OpCode = "move";

            expr_steps.Peek().Add(set_v);

            var jmp = new AS3ExprStep(node.MatchedToken);
            jmp.Type = OpType.GotoFlag;
            jmp.OpCode = logic_flag.Peek().OpCode;

            expr_steps.Peek().Add(jmp);

            var end_flag = logic_flag.Pop();
            var false_flag = logic_flag.Pop();


            expr_steps.Peek().Add(false_flag);

            logic_flag.Push(end_flag);
        }


        void ENTER_Ternary_False(ParseExpr node)
        {
            //__enter_child_Expression();
        }

        void QUIT_Ternary_False(ParseExpr node)
        {
            //__quit_child_Expression();

            var false_part_value = parseing_units.Peek().Pop();

            

            var set_v = new AS3ExprStep(node.MatchedToken);
            set_v.Arg1 = parseing_units.Peek().Peek();
            set_v.Arg2 = false_part_value;
            set_v.Type = OpType.Assigning;
            set_v.OpCode = "move";

            
            expr_steps.Peek().Add(set_v);

            expr_steps.Peek().Add( logic_flag.Pop() );

        }

        void ENTER_UnitSuffix(ParseExpr node)
        {
            if (node.Nodes.Count == 1)
            { 
                var data = parseing_units.Peek().Pop();

                var suffix = new AS3ExprStep(node.MatchedToken);
                suffix.Arg1 = AS3DataStackElement.MakeReg(memberscope.Peek().NextRegId());
                suffix.Arg2 = data;
                suffix.Arg3 = AS3DataStackElement.MakeReg(memberscope.Peek().NextRegId()); //保留一个临时地址
				suffix.Type = OpType.Suffix;
                suffix.OpCode = node.MatchedToken.StringValue;
                
                expr_steps.Peek().Add(suffix);

                parseing_units.Peek().Push(suffix.Arg1);

            }
        }


        void QUIT_NSAccess(ParseExpr node)
        {
            if (node.SelectGrammerLine.Derivation[0].Name == "Unit"
                &&
                node.Nodes[0].SelectGrammerLine.Derivation.Count > 1

                )
            {

            }
            else if(node.SelectGrammerLine.Derivation[0].Name == "Unit")
            {
                if (unit_is_right.Peek())
                {
                    var v = parseing_units.Peek().Peek();
                    if (v.IsReg && !v.Reg.isLd_R)
                    {
                        parseing_units.Peek().Pop();

                        var step = expr_steps.Peek();

                        AS3ExprStep load = new AS3ExprStep(node.MatchedToken);
                        load.Type = OpType.Load;
                        load.OpCode = "Ld_R";
                        load.Arg1 = AS3DataStackElement.MakeReg(memberscope.Peek().NextRegId());
                        load.Arg2 = v;

                        load.Arg1.Reg.isLd_R = true;
                        //express.exprStepList.Add(load);

                        step.Add(load);
                        //code_stack.Peek().Add(express);

                        parseing_units.Peek().Push(load.Arg1);

                    }
                }
            }


			if (node.Nodes.Count == 2)
            {
                var member = parseing_units.Peek().Pop();

                var ns = node.Nodes[0].MatchedToken.StringValue;

                var access2 = new AS3ExprStep(node.MatchedToken);
                access2.Arg1 = AS3DataStackElement.MakeReg(memberscope.Peek().NextRegId());
                access2.Arg2 = new AS3DataStackElement()
                {
                    Data = new AS3DataValue(node.Parent.Nodes[0].MatchedToken)
                    {
                        FF1Type = FF1DataValueType.identifier,
                        Value = ns.Substring(0,ns.Length-2)
                    },
                };
                access2.Arg3 = member;

                access2.OpCode = "::";
                access2.Type = OpType.NameSpaceAccess;

                expr_steps.Peek().Add(access2);
                parseing_units.Peek().Push(access2.Arg1);

            }
        }

        Stack<  HashSet<AS3DataStackElement>> ns_targets =new Stack<  HashSet<AS3DataStackElement>>();
        int nsaccess_line = -1;
        void ENTER_NSAccess(ParseExpr node)
        {
            nsaccess_line = node.MatchedToken.line;

            if (node.Parent.SelectGrammerLine.Derivation[1].Name == "AccessOpt")
            {
                if (node.Parent.Nodes[1].SelectGrammerLine.Derivation[0].Name == "UnitSuffix")
                {
					unit_is_right.Push(false);

					quit_events.Add(node, (n) =>
					{
						unit_is_right.Pop();
					});
				}
            }


            if (node.Nodes.Count == 2)
            {
				enter_events.Add(node.Nodes[1], (n) =>
				{
					unit_is_right.Push(false);
				});

				quit_events.Add(node.Nodes[1], (n) =>
				{
					unit_is_right.Pop();
				});
			}
        }

		void ENTER_AccessOpt_AfterFun(ParseExpr node)
		{
			ENTER_AccessOpt(node);
		}

		void ENTER_AccessOpt(ParseExpr node)
        {
            //检查是否错误的吃了;
            if (node.SelectGrammerLine.Derivation[0].Name == "null")
            {
                if (memberscope.Peek() is AS3ClassInterfaceBase)
                {

                }
                else if (memberscope.Peek() is AS3Package.PackageMemberScope)
                {

                }
                else
                {
                    
                    //if (p.SelectGrammerLine.Main.Name == "Parameters")
                    //{
                    //    goto lbl_pass;  
                    //}
                    //if (p.SelectGrammerLine.Main.Name.EndsWith("Assigning"))
                    //{
                    //    goto lbl_pass;
                    //}

                    if (Lex.ExpressionContextTokens.Contains(node.MatchedToken.StringValue))
                    {
                        goto lbl_pass;
                    }

                    
                    if (node.MatchedToken.StringValue == ")" && node.MatchedToken.Type == Token.TokenType.other)
                    {
                        goto lbl_pass;
                    }
                    if (node.MatchedToken.StringValue == "}" && node.MatchedToken.Type == Token.TokenType.other)
                    {
                        goto lbl_pass;
                    }
                    if (node.MatchedToken.StringValue == "]" && node.MatchedToken.Type == Token.TokenType.other)
                    {
                        goto lbl_pass;
                    }
                    if (node.MatchedToken.StringValue == ";" && node.MatchedToken.Type == Token.TokenType.other)
                    {
                        goto lbl_pass;
                    }
                       
                    if (node.MatchedToken.StringValue == ":" && node.MatchedToken.Type == Token.TokenType.other)
                    {
                        goto lbl_pass;
                    }

					

					if (nsaccess_line != node.MatchedToken.line)
                    {
                        goto lbl_pass;
                    }
					//if (node.MatchedToken.StringValue == "if")
					//{
     //                   if (nsaccess_line != node.MatchedToken.line)
     //                   {
     //                       goto lbl_pass;
     //                   }
					//}
					//if (node.MatchedToken.StringValue == "for")
					//{
     //                   if (nsaccess_line != node.MatchedToken.line)
     //                   {
     //                       goto lbl_pass;
     //                   }
					//}
					//if (node.MatchedToken.StringValue == "do")
					//{
					//	if (nsaccess_line != node.MatchedToken.line)
					//	{
					//		goto lbl_pass;
					//	}
					//}
					//if (node.MatchedToken.StringValue == "while")
					//{
					//	if (nsaccess_line != node.MatchedToken.line)
					//	{
					//		goto lbl_pass;
					//	}
					//}
					//if (node.MatchedToken.StringValue == "switch")
					//{
					//	if (nsaccess_line != node.MatchedToken.line)
					//	{
					//		goto lbl_pass;
					//	}
					//}
					//if (node.MatchedToken.StringValue == "class")
					//{
					//	if (nsaccess_line != node.MatchedToken.line)
					//	{
					//		goto lbl_pass;
					//	}
					//}
					//if (node.MatchedToken.StringValue == "interface")
					//{
					//	if (nsaccess_line != node.MatchedToken.line)
					//	{
					//		goto lbl_pass;
					//	}
					//}


					throw new SyntaxException(node.MatchedToken, "Expecting either a 'semicolon' or a 'new line' here.");
                }
            }

            lbl_pass:




            var grammer = (node.SelectGrammerLine).Derivation;
            if (grammer[0].Name == "." || grammer[0].Name.StartsWith( ":") || grammer[0].Name == "UnitSuffix")
            {
                enter_events.Add(node.Nodes[1], (n) =>
                {
                    unit_is_right.Push(false);
                });

                quit_events.Add(node.Nodes[1], (n) =>
                {
                    unit_is_right.Pop();
                });
            }
            else if (grammer[0].Name.StartsWith(".") && grammer[0].Name.Length>1 )
            {
				enter_events.Add(node.Nodes[2], (n) =>
				{
					unit_is_right.Push(false);
				});

				quit_events.Add(node.Nodes[2], (n) =>
				{
					unit_is_right.Pop();
				});
			}


            //成员访问
            if ((node.Parent.SelectGrammerLine.Main.Name == "AccessOpt" || node.Parent.SelectGrammerLine.Main.Name == "AccessOpt_AfterFun")
                &&
                node.Parent.Nodes.Count > 2
                )
            {

                if (node.Parent.Nodes.Count == 4)
                {
                    if (node.Parent.Nodes[0].MatchedToken.StringValue == "[")
                    {
                        if (node.Parent.Nodes[1].Nodes.Count == 0)
                        {
                            throw new SyntaxException(node.Parent.Nodes[1].MatchedToken, "']' is not allowed here.");
                        }


                        var member = parseing_units.Peek().Pop();
                        var data = parseing_units.Peek().Pop();

                        var access = new AS3ExprStep(node.MatchedToken);
                        access.Arg1 = AS3DataStackElement.MakeReg(memberscope.Peek().NextRegId());
                        access.Arg2 = data;
                        access.Arg3 = member;

                        access.OpCode = "[";
                        access.Type = OpType.Access;

                        expr_steps.Peek().Add(access);

                        parseing_units.Peek().Push(access.Arg1);
                    }
                    else
                    {
                        string NS = node.Parent.Nodes[0].MatchedToken.StringValue.Substring(1);

                        var member = parseing_units.Peek().Pop();
                        var data = parseing_units.Peek().Pop();

                        var nsmember = new AS3DataStackElement()
                        {
                            Data = new AS3DataValue(node.Parent.Nodes[0].MatchedToken)
                            {
                                FF1Type = FF1DataValueType.identifier,
                                Value = NS
                            },
                        };

                        var access = new AS3ExprStep(node.Parent.Nodes[0].MatchedToken);
                        access.Arg1 = AS3DataStackElement.MakeReg(memberscope.Peek().NextRegId());
                        access.Arg2 = data;
                        access.Arg3 = nsmember;

                        access.OpCode = ".";
                        access.Type = OpType.Access;

                        expr_steps.Peek().Add(access);


                        //***



                        var access2 = new AS3ExprStep(node.MatchedToken);
                        access2.Arg1 = AS3DataStackElement.MakeReg(memberscope.Peek().NextRegId());
                        access2.Arg2 = access.Arg1;
                        access2.Arg3 = member;

                        access2.OpCode = "::";
                        access2.Type = OpType.NameSpaceAccess;

                        expr_steps.Peek().Add(access2);
                        parseing_units.Peek().Push(access2.Arg1);

                        if (ns_targets.Peek().Contains(access.Arg1))
                        {
                            throw new SyntaxException(node.MatchedToken, "Expected IDENTIFIER but got '::'");
                        }
                        ns_targets.Peek().Add(access2.Arg1);

                    }

                }
                else if (node.Parent.Nodes[1].Nodes[0].SelectGrammerLine.Main.Name == "E4XFilter"
                    ||
                    node.Parent.Nodes[1].Nodes[0].SelectGrammerLine.Main.Name == "E4XAccess"
                    )
                {

                }
                else
                {
                    var member = parseing_units.Peek().Pop();
                    var data = parseing_units.Peek().Pop();

                    var access = new AS3ExprStep(node.MatchedToken);
                    access.Arg1 = AS3DataStackElement.MakeReg(memberscope.Peek().NextRegId());
                    access.Arg2 = data;
                    access.Arg3 = member;

                    access.OpCode = node.Parent.MatchedToken.StringValue;
                    if (access.OpCode == "::")
                    {
                        access.Type = OpType.NameSpaceAccess;
                        if (ns_targets.Peek().Contains(access.Arg2))
                        {
                            throw new SyntaxException(node.MatchedToken, "Expected IDENTIFIER but got '::'");
                        }
                        ns_targets.Peek().Add(access.Arg1);



                    }
                    else
                    {
                        access.Type = OpType.Access;
                    }
                    expr_steps.Peek().Add(access);

                    parseing_units.Peek().Push(access.Arg1);
                }



            }
        }

        void ENTER_E4XAccess(ParseExpr node)
        { 
        
        }

        void QUIT_E_E4XAccess(ParseExpr node)
        {
            AS3DataStackElement member = null;

            if (node.Nodes.Count == 2)
            {
                member = parseing_units.Peek().Pop();
            }



            var e4x = parseing_units.Peek().Pop();

            var e4xaccess = new AS3ExprStep(node.MatchedToken);
            e4xaccess.Arg1 = AS3DataStackElement.MakeReg(memberscope.Peek().NextRegId());
            e4xaccess.Arg2 = e4x;
            e4xaccess.Arg3 = member;
            

            e4xaccess.Type = OpType.E4XAccess;
            e4xaccess.OpCode = node.Nodes[0].MatchedToken.StringValue;

            expr_steps.Peek().Add(e4xaccess);

            parseing_units.Peek().Push(e4xaccess.Arg1);

        }

        void QUIT_E4XAccess_1(ParseExpr node)
        {
            AS3DataStackElement e4x = null;
            if (node.Nodes.Count == 1)
            {
                if (node.Parent.Parent.SelectGrammerLine.Main.Name == "F_NSAccess")
                {
                    e4x = parseing_units.Peek().Pop();
                }
                else
                {
                    var p = node.Parent;

                    while (p != null && !node_xml.ContainsKey(p))
                    {
                        p = p.Parent;
                    }

                    if (p != null)
                    {
                        e4x = node_xml[p];
                        node_xml.Remove(p);
                    }
                }

                var e4xaccess = new AS3ExprStep(node.MatchedToken);
                e4xaccess.Arg1 = AS3DataStackElement.MakeReg(memberscope.Peek().NextRegId());
                e4xaccess.Arg2 = e4x;

                e4xaccess.Type = OpType.E4XAccess;
                e4xaccess.OpCode = "@" + node.Nodes[0].MatchedToken.StringValue;

                expr_steps.Peek().Add(e4xaccess);

                parseing_units.Peek().Push(e4xaccess.Arg1);

            }
            else
            {
                if (node.Parent.Parent.SelectGrammerLine.Main.Name == "F_NSAccess")
                {
                    e4x = parseing_units.Peek().Pop();
                }
                else
                {
                    var p = node.Parent;

                    while (p != null && !node_xml.ContainsKey(p))
                    {
                        p = p.Parent;
                    }

                    if (p != null)
                    {
                        e4x = node_xml[p];
                        node_xml.Remove(p);
                    }
                }

                var e4xaccess = new AS3ExprStep(node.MatchedToken);
                e4xaccess.Arg1 = AS3DataStackElement.MakeReg(memberscope.Peek().NextRegId());
                e4xaccess.Arg2 = e4x;
                e4xaccess.Arg3 = parseing_units.Peek().Pop();

                e4xaccess.Type = OpType.E4XAccess;
                e4xaccess.OpCode = "@";

                expr_steps.Peek().Add(e4xaccess);

                parseing_units.Peek().Push(e4xaccess.Arg1);

            }
        }

        Dictionary<ParseExpr, AS3DataStackElement> node_xml = new Dictionary<ParseExpr, AS3DataStackElement>();
        void ENTER_E4XFilter(ParseExpr node)
        {
            node_xml.Add(node.Nodes[1], parseing_units.Peek().Peek());
            expr_steps.Push(new List<AS3ExprStep>());
        }

        void QUIT_E4XFilter(ParseExpr node)
        {
            var filter_exprlist =  expr_steps.Pop();
            var member = parseing_units.Peek().Pop();
            if (member.Data == null)
            {
                member.Data = new AS3DataValue(node.MatchedToken)
                {
                    FF1Type = FF1DataValueType.as3_expressionlist,
                    Value = filter_exprlist
                };
            }
            

            var xml = parseing_units.Peek().Pop();

            var access = new AS3ExprStep(node.MatchedToken);
            access.Arg1 = AS3DataStackElement.MakeReg(memberscope.Peek().NextRegId());
            access.Arg2 = xml;
            access.Arg3 = member;
            access.Type = OpType.E4XFilter;
            access.OpCode = "Filter";

            expr_steps.Peek().Add(access);

            parseing_units.Peek().Push(access.Arg1);
        }

        
		void ENTER_Expression(ParseExpr node)
        {
           
            AS3Expression expression = new AS3Expression(node.MatchedToken);
            srcFile._expressions.Add(expression);

            expr_steps.Push(new List<AS3ExprStep>());

            parseing_units.Push(new Stack<AS3DataStackElement>());

            parseing_expression.Push(expression);
            dictNodeExpression.Add(node, expression);

            ns_targets.Push(new HashSet<AS3DataStackElement>());

            unit_is_right.Push(false);


            if (access_member.Count == 0)
            {
                if (node.Parent.SelectGrammerLine.Main.Name == "CommaOpt_1")
                {
                    flag_fun_anonymous.Push(true);
                }
            }
        }

        void QUIT_Expression(ParseExpr node)
        {
			if (access_member.Count == 0)
			{
				if (node.Parent.SelectGrammerLine.Main.Name == "CommaOpt_1")
				{
                    flag_fun_anonymous.Pop();
				}
			}


			unit_is_right.Pop();

            ns_targets.Pop();
            var units = parseing_units.Pop();

            var expression = parseing_expression.Pop();
            expression.exprStepList = expr_steps.Pop();

            


            if (units.Count !=1)
            {
                throw new Exception("表达式值生成异常");
            }

            expression.Value = units.Pop();

            foreach (var item in expression.exprStepList)
            {
                if (item.Arg1 != null && !item.Arg1.IsReg && item.Arg1.Data.Value is AS3Function)
                {
                    if (((AS3Function)item.Arg1.Data.Value).IsMethod)
                    {
                        throw new SyntaxException(item.token, "Expecting either a 'semicolon' or a 'new line' here.");
                    }
                }
                if (item.Arg2 != null && !item.Arg2.IsReg && item.Arg2.Data.Value is AS3Function)
                {
                    if (((AS3Function)item.Arg2.Data.Value).IsMethod)
                    {
                        throw new SyntaxException(item.token, "Expecting either a 'semicolon' or a 'new line' here.");
                    }
                }
                if (item.Arg3 !=null && !item.Arg3.IsReg && item.Arg3.Data.Value is AS3Function)
                {
                    if (((AS3Function)item.Arg3.Data.Value).IsMethod)
                    {
                        throw new SyntaxException(item.token, "Expecting either a 'semicolon' or a 'new line' here.");
                    }
                }


            }


            if (expression.Value.Data != null && expression.Value.Data.Value is AS3Function)
            {
                if (((AS3Function)expression.Value.Data.Value).IsMethod)
                {
                    if (expression.exprStepList.Count > 0)
                    {
                        throw new SyntaxException(expression.exprStepList[0].token, "Expecting either a 'semicolon' or a 'new line' here.");
                    }

                    goto lbl_end;
                }
                else if (!((AS3Function)expression.Value.Data.Value).IsAnonymous)
                {
                    goto lbl_end;
                }
            }

            code_stack.Peek().Add(expression);


        lbl_end:
            ;
        }


        string GetLabel(ParseExpr node)
        {
            if (node.SelectGrammerLine.Main.Name != "Label")
            {
                throw new ArgumentException();
            }

            if (node.Nodes.Count > 0)
            {
                return node.Nodes[0].MatchedToken.StringValue;
            }
            else
            {
                return null;
            }
        }

        void ENTER_PACKAGE_BLOCK(ParseExpr node)
        {
            if (node.Nodes.Count > 1)
            {
                AS3Block block = new AS3Block(node.MatchedToken);
                block.label = GetLabel(node.Nodes[1]);
                block.label_token = node.Nodes[1].MatchedToken;
                block.exit_token = node.Nodes[2].Nodes[ node.Nodes[2].Nodes.Count -1 ].MatchedToken;

                code_stack.Push(block.Code);

                quit_events.Add(node, (e) =>
                {
                    code_stack.Pop();
                    code_stack.Peek().Add(block);

                });
            }
		}


		void ENTER_BLOCK(ParseExpr node)
        {
            AS3Block block = new AS3Block(node.MatchedToken);
            block.label = GetLabel(node.Nodes[1]);
            block.label_token = node.Nodes[1].MatchedToken;
            block.exit_token = node.Nodes[2].Nodes[node.Nodes[2].Nodes.Count - 1].MatchedToken;

			code_stack.Push(block.Code);

			flag_fun_notallow.Push(false);

			quit_events.Add(node, (e) => 
            { 
                code_stack.Pop();
                code_stack.Peek().Add(block);

                flag_fun_notallow.Pop();
			});

        }

        void ENTER_WITH(ParseExpr node)
        { 
            AS3With with = new AS3With(node.MatchedToken);
            with.label = GetLabel(node.Nodes[1]);

            enter_events.Add(node.Nodes[3], (e) => { code_stack.Push(with.WithExpr); });
            quit_events.Add(node.Nodes[3], (e) => { code_stack.Pop(); });

            enter_events.Add(node.Nodes[6], (e) => { code_stack.Push(with.Code); });
            quit_events.Add(node.Nodes[6], (e) => { code_stack.Pop(); });


            quit_events.Add(node, (e) =>
            {
                code_stack.Peek().Add(with);
            });
        }

        void ENTER_WHILE(ParseExpr node)
        { 
            AS3While @while = new AS3While(node.MatchedToken);
            @while.label = GetLabel(node.Nodes[1]);

            enter_events.Add(node.Nodes[3], (e) => { flag_fun_anonymous.Push(true);  code_stack.Push(@while.Condition); });
            quit_events.Add(node.Nodes[3], (e) => { code_stack.Pop();  flag_fun_anonymous.Pop();});

            enter_events.Add(node.Nodes[5], (e) => { flag_fun_notallow.Push(true);  code_stack.Push(@while.Body); });
            quit_events.Add(node.Nodes[5], (e) => { code_stack.Pop(); flag_fun_notallow.Pop(); });

            quit_events.Add(node, (e) =>
            {
                code_stack.Peek().Add(@while);
            });
        }

        void ENTER_DO(ParseExpr node)
        { 
            AS3DoWhile doWhile = new AS3DoWhile(node.MatchedToken);
            doWhile.label = GetLabel(node.Nodes[1]);

            bool fun_notallow = false;
            if (node.Nodes[2].Nodes[0].SelectGrammerLine !=null && node.Nodes[2].Nodes[0].SelectGrammerLine.Main.Name != "BLOCK")
            {
                fun_notallow = true;
            }

            enter_events.Add(node.Nodes[2], (e) => { flag_fun_notallow.Push(fun_notallow);  code_stack.Push(doWhile.Body); });
            quit_events.Add(node.Nodes[2], (e) => { code_stack.Pop(); flag_fun_notallow.Pop(); });

            enter_events.Add(node.Nodes[3], (e) => { code_stack.Push(doWhile.Condition); });
            quit_events.Add(node.Nodes[3], (e) => { code_stack.Pop(); });

            quit_events.Add(node, (e) =>
            {
                code_stack.Peek().Add(doWhile);
            });

        }

        void ENTER_DO_CONDITION(ParseExpr node)
        {
			enter_events.Add(node.Nodes[2], 
                (e) => { 
                    flag_fun_anonymous.Push(true); 
                });
			quit_events.Add(node.Nodes[2], (e) => { 
                flag_fun_anonymous.Pop(); 
            });

		}


		void ENTER_Break(ParseExpr node)
        {
            AS3Break aS3Break = new AS3Break(node.MatchedToken);

            aS3Break.breakTarget = ParseExpr.getNodeValue(node.Nodes[1]);

            code_stack.Peek().Add(aS3Break);
        }

        void ENTER_Continue(ParseExpr node)
        {
            AS3Continue aS3Continue = new AS3Continue(node.MatchedToken);

            aS3Continue.continueTarget = ParseExpr.getNodeValue(node.Nodes[1]);

            code_stack.Peek().Add(aS3Continue);
        }

        void ENTER_Use(ParseExpr node)
        { 
            AS3Use use = new AS3Use(node.MatchedToken);
            use.UseNameSpace = ParseExpr.getNodeValue(node.Nodes[2]);

            //code_stack.Peek().Add(use);
            memberscope.Peek().UseNamespaceSet.Add(use);

        }

        void ENTER_Return(ParseExpr node) 
        {
            AS3Return aS3Return = new AS3Return(node.MatchedToken);       
            code_stack.Push(aS3Return.ReturnValue);

            if (memberscope.Peek() is AS3Interface)
            {
				throw new SyntaxException(node.MatchedToken, "'return' is not allowed here");
			}
            if (memberscope.Peek() is AS3Package.PackageMemberScope)
            {
				throw new SyntaxException(node.MatchedToken, "The return statement cannot be used in package initialization code.");
			}
            if (memberscope.Peek() is AS3OutPackage)
            {
				throw new SyntaxException(node.MatchedToken, "The return statement cannot be used in package initialization code.");
			}



			flag_fun_anonymous.Push(true);

            quit_events.Add(node, (e) => 
            {
                flag_fun_anonymous.Pop();

                code_stack.Pop();
                code_stack.Peek().Add(aS3Return);

            });

        }

        void ENTER_YIELD_RB(ParseExpr node)
        {
			if (memberscope.Peek() is AS3Interface)
			{
				throw new SyntaxException(node.MatchedToken, "'yield' is not allowed here");
			}
			if (memberscope.Peek() is AS3Package.PackageMemberScope)
			{
				throw new SyntaxException(node.MatchedToken, "The yield statement cannot be used in package initialization code.");
			}
			if (memberscope.Peek() is AS3OutPackage)
			{
				throw new SyntaxException(node.MatchedToken, "The yield statement cannot be used in package initialization code.");
			}



			if (node.Nodes.Count == 1)
            {
                code_stack.Peek().Add(new AS3YieldBreak(node.MatchedToken));
            }
            else
            {
                AS3YieldReturn aS3YieldReturn = new AS3YieldReturn(node.MatchedToken);

                code_stack.Push(aS3YieldReturn.ReturnValue);
                quit_events.Add(node.Nodes[1], (e) => 
                {
                    code_stack.Pop();
                    code_stack.Peek().Add(aS3YieldReturn);
                } );

            }

        }

        void ENTER_THROWEXCEPTION(ParseExpr node)
        {
            AS3Throw @throw = new AS3Throw(node.Parent.Nodes[0].MatchedToken);

            

            if (node.Nodes.Count > 0)
            {
                flag_fun_anonymous.Push(true);

                code_stack.Push(new List<IAS3SyntaxNode>());

                quit_events.Add(node.Nodes[0],(e)=>
                    {
                        flag_fun_anonymous.Pop();

                        var expr = code_stack.Pop();
                        @throw.Expression = (AS3Expression)expr[0];
                        code_stack.Peek().Add(@throw);
                    }
                );
            }
            else
            { 
                code_stack.Peek().Add(@throw);
            }
        }

        


		Stack<AS3Try> tries = new Stack<AS3Try>();
        void ENTER_TRY(ParseExpr node)
        { 
            AS3Try @try = new AS3Try(node.MatchedToken);
            @try.label = GetLabel(node.Nodes[1]);

            @try.label_token = node.Nodes[1].MatchedToken;

            enter_events.Add(node.Nodes[3], (e) => { code_stack.Push( @try.TryBlock ); });
            quit_events.Add(node.Nodes[3], (e) => { code_stack.Pop(); });

            @try.try_enter_token = node.Nodes[2].MatchedToken;
            @try.try_exit_token = node.Nodes[4].MatchedToken;


            tries.Push(@try);
        }

        void QUIT_TRY(ParseExpr node)
        {
            var @try = tries.Pop();
            if (@try.CatchList.Count == 0 && @try.FinallyBlock == null)
            {
                throw new SyntaxException(node.MatchedToken, "expecting a catch or a finally clause.");
            }

            code_stack.Peek().Add( @try );
        }

        class temp_memberscope : AS3MemberScope
        {
            public override string GetScopeName()
            {
                throw new NotImplementedException();
            }

            public override List<AS3Use> UseNamespaceSet
            {
                get
                {  
                    throw new NotImplementedException();
                }
            }

        }

        Stack<AS3Variable> catch_scope = new Stack<AS3Variable>();

        void ENTER_CATCH(ParseExpr node)
        { 
            temp_memberscope temp_Memberscope = new temp_memberscope();
            enter_events.Add(node.Nodes[2], 
                (e) => { 
                memberscope.Push(temp_Memberscope); 
                });
            quit_events.Add(node.Nodes[2], (e) => 
            { 
                memberscope.Pop();
                AS3Variable v = (AS3Variable)temp_Memberscope.Members[0];
                tries.Peek().CatchVarList.Add(v);

                tries.Peek().catch_exit_tokens.Add(node.Nodes[6].MatchedToken );

                if (v.ValueExpr != null)
                {
                    throw new SyntaxException(node.MatchedToken, "'=' is not allowed here");
                }


                v.Name = "%" + v.Name + $"%{node.MatchedToken.line}:{node.MatchedToken.ptr}";

                memberscope.Peek().Members.Add(v);


            } );

            enter_events.Add(node.Nodes[5], (e) => { code_stack.Push(new List<IAS3SyntaxNode>()) ;  catch_scope.Push(tries.Peek().CatchVarList.Last()); });
            quit_events.Add(node.Nodes[5], (e) => 
            {
                catch_scope.Pop();
                tries.Peek().CatchList.Add( code_stack.Pop() );
            } );
        }

        void ENTER_FINALLY(ParseExpr node)
        {
            if (node.Nodes.Count > 0)
            {
                tries.Peek().FinallyBlock = new List<IAS3SyntaxNode>();
                code_stack.Push(tries.Peek().FinallyBlock);


                tries.Peek().finally_enter_token = node.Nodes[1].MatchedToken;
                tries.Peek().finally_exit_token = node.Nodes[3].MatchedToken;

			}
        }

        void QUIT_FINALLY(ParseExpr node)
        {
            if (node.Nodes.Count > 0)
            {
                code_stack.Pop();
            }
        }


        Stack<AS3Switch> switches= new Stack<AS3Switch>();
        void ENTER_SWITCH(ParseExpr node)
        { 
            AS3Switch @switch = new AS3Switch(node.MatchedToken);
            @switch.label = GetLabel(node.Nodes[1]);

            enter_events.Add(node.Nodes[3], (e) => 
            {
                code_stack.Push(new List<IAS3SyntaxNode>());
            });

            quit_events.Add(node.Nodes[3], (e) => 
            {
                var exprlist = code_stack.Pop();
                @switch.Expr = (AS3Expression)exprlist[0];
            });

            switches.Push(@switch);
        }

        void QUIT_SWITCH(ParseExpr node)
        {
            code_stack.Peek().Add( switches.Pop() );
        }

        void ENTER_SWITCH_CASE(ParseExpr node)
        {
            var @switch = switches.Peek();
            if (node.Nodes.Count == 4)
            {
                enter_events.Add(node.Nodes[1], (e) =>
                {
                    code_stack.Push(new List<IAS3SyntaxNode>());
                });

                quit_events.Add(node.Nodes[1], (e) =>
                {
                    var exprlist = code_stack.Pop();
                    @switch.CaseTestList.Add((AS3Expression)exprlist[0]);
                });


                enter_events.Add(node.Nodes[3], (e) =>
                {
                    code_stack.Push(new List<IAS3SyntaxNode>());
                    flag_fun_notallow.Push(true);
                });

                quit_events.Add(node.Nodes[3], (e) =>
                {
                    flag_fun_notallow.Pop();
                    @switch.CaseBodyList.Add(code_stack.Pop());
                });

            }
            else
            {
                @switch.default_part_token = node.Nodes[0].MatchedToken;

                enter_events.Add(node.Nodes[1], (e) =>
                {
                   
                    code_stack.Push(new List<IAS3SyntaxNode>());
                    flag_fun_notallow.Push(true);

                });

                quit_events.Add(node.Nodes[1], (e) =>
                {
                    flag_fun_notallow.Pop();

                    if (@switch.CaseTestList.Contains(null))
                    {
                        throw new SyntaxException(node.MatchedToken, "The switch has more than one default, but only one default is allowed.");
                    }

                    @switch.CaseTestList.Add(null);

                    @switch.CaseBodyList.Add(code_stack.Pop());
                });
            }
        }




        void ENTER_IF(ParseExpr node)
        {
            AS3IF _if = new AS3IF(node.MatchedToken);
            _if.label = GetLabel(node.Nodes[1]);

            _if.label_token = node.Nodes[1].MatchedToken;

            enter_events.Add(node.Nodes[3], (e)=>
            {
                code_stack.Push(_if.condition);               
            } );

            quit_events.Add(node.Nodes[3], (e) =>
            {
                code_stack.Pop();
            });

            enter_events.Add(node.Nodes[5], (e)=>
            {
                if (e.MatchedToken.StringValue != "{" && e.MatchedToken.StringValue !="(")
				{
                    flag_fun_notallow.Push(true);
                }
                code_stack.Push(_if.truepart);               
            } );

            quit_events.Add(node.Nodes[5], (e) =>
            {
                code_stack.Pop();


                if (e.MatchedToken.StringValue != "{" && e.MatchedToken.StringValue !="(")
                {
                    flag_fun_notallow.Pop();
                }
            });

            enter_events.Add(node.Nodes[6], (e) => 
            {
                code_stack.Push(_if.falsepart);
            } );

            quit_events.Add(node.Nodes[6], (e) =>
			{
				code_stack.Pop();
            });

            code_stack.Peek().Add(_if);
        }

        void ENTER_IFElse(ParseExpr node)
        {
            if (node.Nodes.Count > 1)
            {
				enter_events.Add(node.Nodes[1], (e) =>
				{
					if (e.MatchedToken.StringValue != "{" && e.MatchedToken.StringValue != "(")
					{
						flag_fun_notallow.Push(true);
					}
				});

				quit_events.Add(node.Nodes[1], (e) =>
				{
					if (e.MatchedToken.StringValue != "{" && e.MatchedToken.StringValue != "(")
					{
						flag_fun_notallow.Pop();
					}
				});
			}
        }


        Stack<string> for_label = new Stack<string>();
        void ENTER_FOR_STMT(ParseExpr node)
        {
            for_label.Push(GetLabel(node.Nodes[1]));
        }

        void QUIT_FOR_STMT(ParseExpr node)
        {
            for_label.Pop();
        }

        void ENTER_FOR(ParseExpr node)
        {
            AS3For @for = new AS3For(node.MatchedToken);
            @for.label = for_label.Peek();

            enter_events.Add(node.Nodes[1], (e) =>
            {
                code_stack.Push(@for.Part2);
            });

            quit_events.Add(node.Nodes[1], (e) =>
            {
                code_stack.Pop();
            });

            enter_events.Add(node.Nodes[3], (e) =>
            {
                code_stack.Push(@for.Part3);
            });

            quit_events.Add(node.Nodes[3], (e) =>
            {
                code_stack.Pop();
            });

            enter_events.Add(node.Nodes[5], (e) =>
            {
                flag_fun_notallow.Push(true);

                code_stack.Push(@for.Body);
            });

            quit_events.Add(node.Nodes[5], (e) =>
            {
                code_stack.Pop();

                flag_fun_notallow.Pop();
            });


            code_stack.Peek().Add(@for);

        }


        void ENTER_ForVar(ParseExpr node)
        {
            if (node.Parent.SelectGrammerLine.Main.Name == "FOR_TEMP1")
            {
                if (node.Parent.Nodes[1].Nodes[0].SelectGrammerLine.Main.Name == "FORIN")
                {
                    AS3ForIn forIn = new AS3ForIn(node.MatchedToken);
                    forIn.label = for_label.Peek();

                    forIn.HoldObjVar = new AS3Variable(node.MatchedToken);
                    forIn.HoldObjVar.Name = "%&" + "IterObjHolder" +   $"%{node.MatchedToken.line}:{node.MatchedToken.ptr}";
					memberscope.Peek().Members.Add(forIn.HoldObjVar);

					// 添加迭代器上下文临时变量
					AS3Variable iterCtxVar = new AS3Variable(node.MatchedToken);
					iterCtxVar.Name = "%&" + "IterContext" + $"%{node.MatchedToken.line}:{node.MatchedToken.ptr}";
					memberscope.Peek().Members.Add(iterCtxVar);
					forIn.IterContextVar = iterCtxVar;


					// 添加保存Iter_get获取的迭代器的临时变量
					AS3Variable iterVar = new AS3Variable(node.MatchedToken);
					iterVar.Name = "%&" + "IterHolder" + $"%{node.MatchedToken.line}:{node.MatchedToken.ptr}";
					memberscope.Peek().Members.Add(iterVar);
					forIn.HoldIterVar = iterVar;


					//提取for in 
					if (node.SelectGrammerLine.Derivation[0].Name == "F_Variable")
                    {
                        quit_events.Add(node.Nodes[0], (e) =>
                        {
                            forIn.ForArg = (AS3Variable)memberscope.Peek().Members[memberscope.Peek().Members.Count - 1];
                        });

                    }
                    else if (node.SelectGrammerLine.Derivation[0].Name == "F_ExpressionList")
                    {
                        enter_events.Add(node.Nodes[0], (e) =>
                        {
                            code_stack.Push(new List<IAS3SyntaxNode>());
                        }
                        );


                        quit_events.Add(node.Nodes[0], (e) =>
                        {
                            forIn.ForArg = code_stack.Peek()[code_stack.Peek().Count - 1];

                            code_stack.Pop();

                        });

                    }


                    enter_events.Add(node.Parent.Nodes[1].Nodes[0].Nodes[1], (e) =>
                    {
                        code_stack.Push(new List<IAS3SyntaxNode>());
                    });

                    quit_events.Add(node.Parent.Nodes[1].Nodes[0].Nodes[1], (e) =>
                    {
                        var expr = code_stack.Pop();

                        forIn.ForInExpression = (AS3Expression)expr[expr.Count-1];

                    });

                    enter_events.Add(node.Parent.Nodes[1].Nodes[0].Nodes[3], (e) =>
                    {
                        code_stack.Push(forIn.Body);
                    });

                    quit_events.Add(node.Parent.Nodes[1].Nodes[0].Nodes[3], (e) =>
                    {
                        code_stack.Pop();
                        code_stack.Peek().Add(forIn);
                    });



                }
            }
            else if (node.Parent.SelectGrammerLine.Main.Name == "Each_TEMP1")
            {
                AS3ForEach forEach = new AS3ForEach(node.MatchedToken);
                forEach.label = for_label.Peek();

				forEach.HoldObjVar = new AS3Variable(node.MatchedToken);
				forEach.HoldObjVar.Name = "%&" + "IterObjHolder" + $"%{node.MatchedToken.line}:{node.MatchedToken.ptr}";
				memberscope.Peek().Members.Add(forEach.HoldObjVar);

				// 添加迭代器上下文临时变量
				AS3Variable iterCtxVar = new AS3Variable(node.MatchedToken);
				iterCtxVar.Name = "%&" + "IterContext" + $"%{node.MatchedToken.line}:{node.MatchedToken.ptr}";
				memberscope.Peek().Members.Add(iterCtxVar);
				forEach.IterContextVar = iterCtxVar;

				// 添加保存Iter_get获取的迭代器的临时变量
				AS3Variable iterVar = new AS3Variable(node.MatchedToken);
				iterVar.Name = "%&" + "IterHolder" + $"%{node.MatchedToken.line}:{node.MatchedToken.ptr}";
				memberscope.Peek().Members.Add(iterVar);
				forEach.HoldIterVar = iterVar;


				if (node.SelectGrammerLine.Derivation[0].Name == "F_Variable")
                {
                    quit_events.Add(node.Nodes[0], (e) =>
                    {
                        forEach.ForArg = (AS3Variable)memberscope.Peek().Members[memberscope.Peek().Members.Count - 1];
                    });

                }
                else if (node.SelectGrammerLine.Derivation[0].Name == "F_ExpressionList")
                {
					enter_events.Add(node.Nodes[0], (e) =>
					{
						code_stack.Push(new List<IAS3SyntaxNode>());
					}
					);

					quit_events.Add(node.Nodes[0], (e) =>
                    {
                        forEach.ForArg = code_stack.Peek()[code_stack.Peek().Count - 1];

                        code_stack.Pop();

                    });
                }

                enter_events.Add(node.Parent.Nodes[1].Nodes[1], (e) =>
                {
                    code_stack.Push(new List<IAS3SyntaxNode>());
                });

                quit_events.Add(node.Parent.Nodes[1].Nodes[1], (e) =>
                {
                    var expr = code_stack.Pop();
                    forEach.ForInExpression = (AS3Expression)expr[expr.Count-1];

                });

                enter_events.Add(node.Parent.Nodes[1].Nodes[3], (e) =>
                {
                    code_stack.Push(forEach.Body);
                });

                quit_events.Add(node.Parent.Nodes[1].Nodes[3], (e) =>
                {
                    code_stack.Pop();
                    code_stack.Peek().Add(forEach);
                });
            }
        }


    }
}
