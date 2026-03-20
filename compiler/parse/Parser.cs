using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace juicescript.compiler.parse
{
    /// <summary>
    /// LL(1)文法分析器
    /// </summary>
    public class Parser
    {
        private Dictionary<string, ParseNode> gnodes = new Dictionary<string, ParseNode>();
        private Dictionary<ParseNode, ParseNode> termianlnodes = new Dictionary<ParseNode, ParseNode>();
        private List<ParseNode> _terminals = new List<ParseNode>();
        private List<ParseNode> _identifiers = new List<ParseNode>();
        private List<ParseNode> _other = new List<ParseNode>();
        private List<ParseLine> glines = new List<ParseLine>();

        /// <summary>
        /// 预测分析表
        /// </summary>
        public Dictionary<ParseNode, Dictionary<ParseNode, ParseLine>> M;

        public bool hasError;

        public bool hasErrorFF;
        public string ErrorFFStr;

        public TextWriter ErrorOut;

        public Parser(TokenList tokenlist)
        {
            hasError = false;
            ErrorOut = Console.Error;

            tokenlist.Reset();
            tokenlist.GetNextToken();

            while (tokenlist.CurrentToken.Type != Token.TokenType.eof)
            {
                glines.AddRange(ParseLine(tokenlist));
            }

            //计算FIRST集
            while (true)
            {
                bool found = false;
                foreach (var line in glines)
                {
                    for (int i = 0; i < line.Derivation.Count; i++)
                    {
                        var ff = line.Derivation[i].FIRST;

                        var oldcount = line.Main.FIRST.Count;

                        for (int k = 0; k < ff.Count; k++)
                        {
                            line.Main.FIRST.Add(ff[k]);
                        }

                        if (line.Main.FIRST.Count != oldcount)
                        {
                            found = true;
                        }

                        if (!ff.Contains(ParseNode.GNodeNull))
                        {
                            break;
                        }
                    }
                }
                if (!found)
                {
                    break;
                }
            }

            //计算FOLLOW集
            glines[0].Main.FOLLOW.Add(ParseNode.GNodeEOF);
            while (true) 
            {
                bool found = false;
                foreach (var line in glines)
                {
                    for (int i = 0; i < line.Derivation.Count - 1; i++)
                    {
                        if (line.Derivation[i].Type == ParseNodeType.non_terminal)
                        {
                            var ff = line.Derivation[i + 1].FIRST;
                            var oldcount = line.Derivation[i].FOLLOW.Count;

                            foreach (var symbol in ff)
                            {
                                if (symbol.Type != ParseNodeType._null)
                                {
                                    line.Derivation[i].FOLLOW.Add(symbol);
                                }
                            }

                            if (line.Derivation[i].FOLLOW.Count > oldcount)
                            { 
                                found=true;
                            }
                        }
                    }

                    for (int i = 0; i < line.Derivation.Count; i++)
                    {
                        if (line.Derivation[i].Type == ParseNodeType.non_terminal)
                        {
                            if ((i < line.Derivation.Count - 1 && line.Derivation[i + 1].FIRST.Contains(ParseNode.GNodeNull))
                                ||
                                i == line.Derivation.Count - 1
                                )
                            {
                                var oldcount = line.Derivation[i].FOLLOW.Count;
                                foreach (var symbol in line.Main.FOLLOW)
                                {
                                    line.Derivation[i].FOLLOW.Add(symbol);
                                }
                                if (line.Derivation[i].FOLLOW.Count > oldcount)
                                {
                                    found = true;
                                }
                            }
                        }
                    }
                }

                if (!found)
                    break;

            }

            //Console.WriteLine("非终结符");
            //foreach (var e in gnodes.Values)
            //{
            //    Console.WriteLine(e.Name + "\t" + "FIRST{ " + string.Join(",", e.FIRST.Select((n) => { return n.Type == GrammarNodeType.terminal ? "\"" + n.Name + "\"" : n.Name; })) + "}");
            //    Console.WriteLine("\t" + "FOLLOW{ " + string.Join(",", e.FOLLOW.Select((n) => { return n.Type == GrammarNodeType.terminal ? "\"" + n.Name + "\"" : n.Name; })) + "}");
            //    Console.WriteLine();
            //}
            //Console.WriteLine("终结符:");
            //foreach (var e in termianlnodes.Values)
            //{
            //    Console.WriteLine(e.Name + "\t" + "FIRST{ " + string.Join(",", e.FIRST.Select((n) => { return n.Type == GrammarNodeType.terminal ? "\"" + n.Name + "\"" : n.Name; })) + "}");
            //}

            //***生成预测分析表***
            M = new Dictionary<ParseNode, Dictionary<ParseNode, ParseLine>>();
            foreach (var nt in gnodes.Values)
            {
                var ML = new Dictionary<ParseNode, ParseLine>();
                foreach (var t in termianlnodes.Values)
                {
                    if (t.Type != ParseNodeType._null)
                    {
                        ML.Add(t, null);
                    }
                }
                ML.Add(ParseNode.GNodeEOF, null);
                M.Add(nt, ML);
            }

            foreach (var line in glines)
            {
                var first = line.Derivation[0].FIRST;
               
                foreach (var k in first)
                {
                    if (k.Type != ParseNodeType._null)
                    {
                        if (M[line.Main][k] != null && !M[line.Main][k].Equals(line))
                        {
                            ErrorFFStr += "发现二义文法! 行[" + line.Main.Name + "] 输入[" + k.Name + "] 原来是" + M[line.Main][k].ToString() + "\n";
                            ErrorFFStr += "            " + line.ToString() + "\n";
                            hasErrorFF = true;

                            for (int index = 0; index < glines.Count; index++)
                            {
                                if (glines[index].Equals(line))
                                {
                                    M[line.Main][k] = line;
                                    ErrorFFStr += "   新加入的优先级高，替换原处理\n";
                                    break;
                                }
                                else if (glines[index].Equals(M[line.Main][k]))
                                {
                                    ErrorFFStr += "   原优先级高，不处理\n";
                                    break;
                                }
                            }
                        }
                        else
                        {
                            M[line.Main][k] = line;
                        }
                    }

                    if (k.Type == ParseNodeType._null)
                    {
                        var follow = line.Main.FOLLOW;
                        foreach (var b in follow) 
                        {
                            if (M[line.Main][b] != null && !M[line.Main][b].Equals(line))
                            {
                                ErrorFFStr += "发现二义文法! 行[" + line.Main.Name + "] 输入[" + b.Name + "] 原来是" + M[line.Main][b].ToString() + "\n";
                                ErrorFFStr += "            " + line.ToString() + "\n";
                                hasErrorFF = true;

                                for (int index = 0; index < glines.Count; index++)
                                {
                                    if (glines[index].Equals(line))
                                    {
                                        M[line.Main][b] = line;
                                        ErrorFFStr += "    新加入的优先级高，替换原处理\n";
                                        break;
                                    }
                                    else if (glines[index].Equals(M[line.Main][b]))
                                    {
                                        ErrorFFStr += "    原优先级高，不处理\n";
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                M[line.Main][b] = line;
                            }
                        }
                    }

                }
            }

            foreach (var k in termianlnodes.Values)
            {
                if(k.Type == ParseNodeType.terminal)
                {
                    _terminals.Add(k);
                }
            }
            foreach (var k in termianlnodes.Values)
            {
                if(k.Type == ParseNodeType.identifier)
                {
                    _identifiers.Add(k);
                }
            }
            foreach (var k in termianlnodes.Values)
            {
                if (k.Type != ParseNodeType._null)
                {
                    if (k.Type != ParseNodeType.terminal && k.Type != ParseNodeType.identifier)
                    { 
                        _other.Add(k);
                    }
                }
            }

            //Console.WriteLine("预测分析表:");
            //foreach (var t in termianlnodes.Values)
            //{
            //    if (t.Type == GrammarNodeType._null)
            //        continue;
            //    Console.Write("\t|");
            //    Console.Write(t.Type == GrammarNodeType.terminal ? "\"" + t.Name + "\"" : t.Name);
            //}
            //Console.WriteLine("\t|" + GrammarNode.GNodeEOF.Name);
            //foreach (var f in M.Keys)
            //{
            //    Console.Write(f.Name);
            //    foreach (var t in termianlnodes.Values)
            //    {
            //        if(t.Type == GrammarNodeType._null)
            //            continue;
            //        Console.Write("\t");
            //        Console.Write("|");
            //        Console.Write(M[f][t]);
            //    }

            //    Console.WriteLine("\t|" + M[f][GrammarNode.GNodeEOF] );
            //}

       

        }

        private List<ParseLine> ParseLine(TokenList words)
        {
            var tk = words.CurrentToken;
            var result = new List<ParseLine>();

            var grammarline = new ParseLine();
            result.Add(grammarline);

            if (tk.Type == Token.TokenType.other && tk.StringValue == "<")
            {
                tk = words.GetNextToken();
                if (tk.Type == Token.TokenType.identifier)
                {
                    ParseNode node;
                    if (!gnodes.ContainsKey(tk.StringValue))
                    {
                        node = new ParseNode(tk.StringValue, ParseNodeType.non_terminal);
                        gnodes.Add(node.Name, node);
                    }
                    else
                    {
                        node = gnodes[tk.StringValue];
                    }
                    grammarline.Main = node;
                    tk = words.GetNextToken();
                }
                else
                {
                    throw new Exception("期望标识符");
                }
            }
            else
            {
                throw new Exception("期望<");
            }

            if (tk.Type == Token.TokenType.other && tk.StringValue == ">")
            {
                Match(words.GetNextToken(), Token.TokenType.other, ":");
                Match(words.GetNextToken(), Token.TokenType.other, ":");
                Match(words.GetNextToken(), Token.TokenType.other, "=");
            }
            else
            {
                throw new Exception("期望>");
            }

            tk = words.GetNextToken();

            while (!(tk.Type == Token.TokenType.other && tk.StringValue == ";"))
            {
                //分析导出式
                if (tk.Type == Token.TokenType.other && tk.StringValue == "<")
                {
                    tk = words.GetNextToken();
                    if (tk.Type == Token.TokenType.identifier)
                    {
                        ParseNode node;
                        if (!gnodes.ContainsKey(tk.StringValue))
                        {
                            node = new ParseNode(tk.StringValue, ParseNodeType.non_terminal);
                            gnodes.Add(node.Name, node);
                        }
                        else
                        {
                            node = gnodes[tk.StringValue];
                        }
                        grammarline.Derivation.Add(node);
                        Match(words.GetNextToken(), Token.TokenType.other, ">");
                        tk = words.GetNextToken();
                    }
                    else
                    {
                        throw new Exception("期望标识符");
                    }
                }
                else if (tk.Type == Token.TokenType.identifier)
                {
                    ParseNode node;
                    if (tk.StringValue == "null")
                        node = ParseNode.GNodeNull;
                    else if (tk.StringValue == "number")
                        node = ParseNode.GNodeNumber;
                    else if (tk.StringValue == "string")
                        node = ParseNode.GNodeString;
                    else if (tk.StringValue == "identifier")
                        node = ParseNode.GNodeIdentifier;
                    else if (tk.StringValue == "S")
                        node = ParseNode.GNodeWhiteSpace;
                    else if (tk.StringValue == "label")
                        node = ParseNode.GNodeLabel;
                    else if (tk.StringValue == "useless_label")
                        node = ParseNode.GNodeUseLessLabel;
                    else if (tk.StringValue == "this")
                        node = ParseNode.GNodeThis;
                    else if (tk.StringValue == "super")
                        node = ParseNode.GNodeSuper;
                    else
                        throw new Exception("错误的符号" + tk.StringValue);

                    if (!termianlnodes.ContainsKey(node))
                    {
                        node.FIRST.Add(node);
                        termianlnodes.Add(node, node);
                    }

                    grammarline.Derivation.Add(termianlnodes[node]);

                    tk = words.GetNextToken();
                }
                else if (tk.Type == Token.TokenType.const_string)
                {
                    ParseNode node = new ParseNode(tk.StringValue, ParseNodeType.terminal);
                    if (!termianlnodes.ContainsKey(node))
                    {
                        node.FIRST.Add(node);
                        termianlnodes.Add(node, node);
                    }
                    grammarline.Derivation.Add(termianlnodes[node]);
                    tk = words.GetNextToken();
                }
                else if (tk.Type == Token.TokenType.other && tk.StringValue == "|")
                {
                    grammarline = new ParseLine();
                    result.Add(grammarline);
                    grammarline.Main = result[0].Main;

                    tk = words.GetNextToken();
                }
                else
                {
                    throw new Exception("错误的符号" + tk.StringValue);
                }
            }

            words.GetNextToken();
            return result;
        }

        private void Match(Token token, Token.TokenType type, string value)
        {
            if (token.Type != type)
            {
                throw new Exception(token.StringValue + "不能出现在这里");
            }

            if (value != null)
            {
                if (token.StringValue != value)
                {
                    throw new Exception("期望" + value);
                }
            }

        }

		private bool IsVector(Token lastword, TokenList preWords)
		{
			int i = preWords.IndexOf(lastword);

			for (int j = i - 1; j >= 0; j--)
			{
				var prev = preWords[j];
				if (prev.Type == Token.TokenType.whitespace || prev.Type == Token.TokenType.comments) continue;

				if (prev.Type == Token.TokenType.identifier)
				{
					continue;
				}

				if (prev.Type == Token.TokenType.other && prev.StringValue == ".")
				{
					continue;
				}

				if (prev.StringValue == "Vector.<")
				{
					return true;
				}
				else
				{
                    return false;

				}
			}


			return false;
		}


		public ParseTree ParseTree(string input,
            IEnumerable<string> definekeywords ,
            IEnumerable<string> defineSkipBlankWords , 
            string srcfile = "", 
            string srcfileFullPath = null)
        {
            if (srcfileFullPath == null && srcfile != null)
            { 
                srcfileFullPath = srcfile;
            }
            hasError = false;
            var key = new MyMD5.MyMD5().Hash(input);
            var tree = new ParseTree( ref key  );

            tree.Root = new ParseExpr() { GrammerLeftNode = glines[0].Main };

            Stack<ParseExpr> treenodestack =new Stack<ParseExpr>();
            treenodestack.Push(tree.Root);

            TokenList words;
            try
            {
                words = new Lex(srcfile, definekeywords, defineSkipBlankWords, true).GetWords(input,true);
                for (int i = 0; i < words.Count; i++)
                {
                    words[i].sourceFileFullPath = srcfileFullPath;
                }
            }
            catch (LexException ex)
            {
                hasError = true;

                Console.Error.WriteLine(srcfileFullPath + ":" + (ex.line + 1) + ":Error: " + ex.Message);

                input = input.Replace("\r\n", "\n");
                input = input.Replace("\r", "\n");

                var lines = input.Split('\n');
                Console.Error.Write("\t\t" + lines[ex.line]);
                Console.Error.Write("\n");
                Console.Error.Write("\t\t^".PadLeft(ex.ptr));
                Console.Error.Write("\n");
                Console.Error.Write("\n");

                return tree;
            }

			#region "检查 this 和 super关键字"
			for (int i = 0; i < words.Count; i++)
            {
                var token = words[i];
                if (token.Type == Token.TokenType.identifier)
                {
                    if (token.StringValue == "this")
                    {
                        token.Type = Token.TokenType.this_pointer;
                    }
                    else if (token.StringValue == "super")
                    {
                        token.Type = Token.TokenType.super_pointer;
                    }
                }
            }
            #endregion

            #region continue、break、return和throw后跟换行符会添加一个分号
            for (int index = 0; index < words.Count; index++)
            {
                var token = words[index];
                if (token.Type == Token.TokenType.identifier)
                {
                    if (token.StringValue == "continue" || 
                        token.StringValue == "break"    ||
                        token.StringValue == "return"   ||
                        token.StringValue == "throw"
                        )
                    {
                        Token toreplace = null;


                        //查询后面到换行直接是否全是空白, 并且换行后不是 ";"   如果 满足 则在换行处插入一个分号
                        for (int k = index+1; k < words.Count; k++)
                        {
                            var nt = words[k];
                            if (nt.Type == Token.TokenType.other && nt.StringValue == ";")
                                break;
                            else if (nt.Type == Token.TokenType.whitespace && nt.StringValue == "\n")
                            {
                                if (toreplace == null)
                                {
                                    toreplace = nt;
                                }
                                //nt.Type = Token.TokenType.other;
                                //nt.StringValue = ";";
                                //break;
                            }
                            else if (nt.Type == Token.TokenType.comments)
                            {

                            }
                            else
                            {
                                if (toreplace != null)
                                {
                                    toreplace.Type = Token.TokenType.other;
                                    toreplace.StringValue = ";";
                                }
                                break;
                            }
                        }
                    }
                }
            }
            #endregion

            #region 在do while() 尾部不管有没有插入一个分号再说

            {
                Stack<Token> do_tokens = new Stack<Token>();

                int skips = 0;

                for (int index = 0; index < words.Count; index++)
                {
                    var token = words[index];
                    if (token.Type == Token.TokenType.identifier && token.StringValue == "do")
                    {
                        skips = 0;
                        do_tokens.Push(token);
                    }
                    else if (token.Type == Token.TokenType.identifier && token.StringValue == "while")
                    {
                        if (skips == 0)
                        {
                            throw new SyntaxException( token, "Unable to generate code for 'do'");
                        }

                        Token toreplace = null;
                        if (do_tokens.Count > 0)
                        {
                            do_tokens.Pop();

                            Stack<Token> parenthesis = new Stack<Token>();

                            int step = 0;
                            for (int j = index + 1; j < words.Count; j++)
                            {
                                var test = words[j];

                                if (step != 2)
                                {
                                    if (test.Type == Token.TokenType.whitespace || test.Type == Token.TokenType.comments)
                                        continue;
                                }

                                if (step == 0)
                                {
                                    if (test.StringValue != "(")
                                    {
                                        throw new SyntaxException(test, "need a '(' here");
                                    }
                                    step = 1;

                                    parenthesis.Push(test);
                                }
                                else if (step == 1)
                                {
                                    if (test.StringValue == "(")
                                    {
                                        parenthesis.Push(test);
                                    }
                                    else if (test.StringValue == ")")
                                    {
                                        if (parenthesis.Count > 0)
                                        {
                                            parenthesis.Pop();

                                            if (parenthesis.Count == 0)
                                            {
                                                step = 2;
                                            }
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }
                                }
                                else if (step == 2)
                                {
                                    if (test.Type == Token.TokenType.other && test.StringValue == ";")
                                    {
                                        break;
                                    }
                                    else if (test.Type == Token.TokenType.whitespace && test.StringValue == "\n")
                                    {
                                        toreplace = test;
                                    }
                                    else
                                    {
                                        if (toreplace != null)
                                        {
                                            toreplace.Type = Token.TokenType.other;
                                            toreplace.StringValue = ";";
                                            break;
                                        }
                                    }
                                }


                            }
                        }
                    }
                    else if (token.Type == Token.TokenType.comments || token.Type == Token.TokenType.whitespace)
                    {

                    }
                    else
                    {
                        skips++;
                    }
                }
            }


            #endregion

            #region 换行后 ++,-- // a ++ b

            {
                for (int index = 0; index < words.Count; index++)
                {
                    var token = words[index];
                    if (token.Type == Token.TokenType.other && token.StringValue == "++"
                        ||
						token.Type == Token.TokenType.other && token.StringValue == "--"
						)
                    {
                        //先看是不是行首。
                        Token ln = null;int ln_id=0;
                        for (int j = index-1; j >= 0; j--)
                        {
                            var test = words[j];
                            if (test.Type == Token.TokenType.comments)
                            {
                                continue;
                            }
                            if (test.Type == Token.TokenType.whitespace)
                            {
                                if (test.StringValue == "\n")
                                {
                                    ln = test;
                                    ln_id =j;
                                    break;
                                }
                                continue;
                            }
                            break;
                        }


                        if (ln != null)
                        {

							HashSet<string> ExpressionContextTokens = new HashSet<string>
		                    {
			                    "+", "-", "*", "/", "%", "|","&" ,"^","=", "==", "!=", "<", "<=", ">=", ",", "(", 
			                    "||", "||=" , "~"
			                    , "?" , ":"
			                    , "&&", "&&=", "<<", ">>", ">>>", "<=", ">="
			                    , "==", "!=", "===", "!=="
			                    , "+=", "-=", "*=", "/=", "%=", ">>=", "<<=", ">>>=", "&=", "^=", "|="
			                    

		                    };

							//前面也要是一个 identifier.
							for (int j = ln_id - 1; j >= 0; j--)
							{
								var test = words[j];
								if (test.Type == Token.TokenType.comments || test.Type == Token.TokenType.whitespace)
								{
                                    ln = test;
									continue;
								}

                                if (test.StringValue == ">")
                                {
                                    //如果前面是 vector,则加
                                    if (IsVector(test, words))
                                    {
                                       
                                    }
                                    else
                                    { 
                                        ln = null;
                                    }

                                    //ln = null;
                                    break;
                                }
                                else if ( ExpressionContextTokens.Contains( test.StringValue))
                                {
									ln = null;
                                    break;
                                }
                                else
                                {
                                    break;
                                }
							}


						}


                        if (ln != null)
                        {
							ln.Type = Token.TokenType.other;
							ln.StringValue = ";";


							//如果后面是一个 identifier ,则把ln替换为分号

							//                     for (int j = index+1; j < words.Count; j++)
							//                     {
							//	var test = words[j];
							//	if (test.Type == Token.TokenType.comments || test.Type == Token.TokenType.whitespace)
							//	{
							//		continue;
							//	}

							//                         if (test.Type == Token.TokenType.identifier)
							//                         {
							//                             ln.Type = Token.TokenType.other;
							//                             ln.StringValue = ";";

							//                         }
							//                         break;
							//}

						}
					}

                }
			}


			#endregion

			#region label 处理

			HashSet<Token> mustNotLabel = new HashSet<Token>();
            Stack<Token> needColon = new Stack<Token>();
            for (int index = 0; index < words.Count; index++)
            { 
                var token = words[index];
                if (token.StringValue == "?")
                {
                    needColon.Push(token);
                }
                else if (token.StringValue == "case")
                {
                    needColon.Push(token);
                }
                else if (token.StringValue == "default")
                {
                    needColon.Push(token);
                }
                else if (token.StringValue == ":")
                {
                    if (needColon.Count > 0)
                    {
                        needColon.Pop();
                        mustNotLabel.Add(token);
                    }
                    else
                    {
                        if (index > 0) // 排除 var a:Number  , (a:Number) ,(a,i:Number)
                        {
                            int fidx = index - 1;
							bool flag = false;
							while (fidx > 0)
                            { 
                                var test = words[fidx];
                                fidx--;

								if (test.Type == Token.TokenType.whitespace || test.Type == Token.TokenType.comments)
									continue;

                                if (!flag)
                                {
                                    if (test.Type == Token.TokenType.identifier)
                                    {
                                        //找到了 a:Number, 再向前看是不是 var 之类必然排除的。
                                        flag = true;
                                    }
                                }
                                else
                                {
                                    if (
                                        test.StringValue == "(" || 
                                        test.StringValue == "," || 
                                        test.StringValue == "var" ||
                                        test.StringValue == "const" 
                                        )
                                    {
										mustNotLabel.Add(token);
                                    }
                                    break;
                                }


							}


                        }
                    }
                }
            }




            #region 检测语句label
            {
                Dictionary<Token, Token> s_label = new Dictionary<Token, Token>();

                bool exists;

                do
                {
                    exists=false;

					for (int index = 2; index < words.Count; index++)
					{
						var token = words[index];
						if (token.Type == Token.TokenType.identifier)
						{
							if (token.StringValue == "for" || token.StringValue == "while" || token.StringValue == "do" || token.StringValue == "switch"
								|| token.StringValue == "if" || token.StringValue == "try" || token.StringValue == "with"
								)
							{
								//前向查找
								bool found = false;
								var fidx = index;
								int fstep = 0;
								int stepidx = 0;
								while (!found && fidx > 0)
								{
									fidx = fidx - 1;
									var test = words[fidx];
									if (fstep == 0) //查找":"
									{
										if (test.Type == Token.TokenType.whitespace || test.Type == Token.TokenType.comments)
											continue;
										else if (test.Type == Token.TokenType.other && test.StringValue == ":")
										{
											if (!mustNotLabel.Contains(test))
											{
												fstep = 1;
												stepidx = fidx;
												continue;
											}
											else
											{
												break;
											}
										}
										else
											break;
									}
									if (fstep == 1)//查找 identifier为 label名
									{
                                        if (test.Type == Token.TokenType.whitespace || test.Type == Token.TokenType.comments)
                                            continue;
                                        else if (test.Type == Token.TokenType.identifier)
                                        { //前一个必须是空或者;
                                            if (fidx > 0)
                                            {
                                                var test2 = words[fidx - 1];
                                                if (!(test2.Type == Token.TokenType.whitespace || (test2.Type == Token.TokenType.other && test2.StringValue == ";")))
                                                    break;

                                                if (fidx > 1) // 排除  case A: if(XX) 这种情况
                                                {
                                                    var test3 = words[fidx - 2];
                                                    if (test2.Type == Token.TokenType.whitespace && test3.Type == Token.TokenType.identifier && test3.StringValue == "case")
                                                    {
                                                        break;
                                                    }
                                                }

                                            }
                                            found = true;


                                            if (!s_label.ContainsKey(words[index]))
                                            {
												s_label.Add(token, test);

												test.Type = Token.TokenType.label;
                                                //*将label:移动到关键字后面去
                                                words[fidx] = words[index]; //关键字向前移动
                                                words[index] = words[stepidx]; //冒号后移
                                                words[stepidx] = test;
                                            }
                                            else
                                            {
                                                //将label设为无用

                                                test.Type = Token.TokenType.comments;
                                                words[stepidx].Type = Token.TokenType.comments;

                                                var olbl = s_label[words[index]];
                                                olbl.StringValue += "@" + test.StringValue;
											}


                                            index = index + 3;

                                            exists = true;

											break;
										}
										else
										{
											break;
										}
									}
								}
							}
						}
					}

				} while (exists);

            }
            #endregion

            #region 查找代码块label

            {
				Dictionary<Token, Token> s_label = new Dictionary<Token, Token>();

				bool exists;

                do
                {
                    exists = false;


					//' label:{
					for (int i = 2; i < words.Count; i++)
					{
						var token = words[i];
						if (token.Type == Token.TokenType.other)
						{
							if (token.StringValue == "{")
							{
								//前向查找:
								int fidx = i;
								while (fidx > 0)
								{
									fidx = fidx - 1;
									var test = words[fidx];
									if (test.Type == Token.TokenType.whitespace || test.Type == Token.TokenType.comments)
										continue;
									else
										break;
								}
								if (!(words[fidx].Type == Token.TokenType.other && words[fidx].StringValue == ":"))
								{
									continue;
								}
								if (mustNotLabel.Contains(words[fidx]))
								{
									continue;
								}

								var stepidx = fidx;
								//查找identifier;
								while (fidx > 0)
								{
									fidx = fidx - 1;
									var test = words[fidx];
									if (test.Type == Token.TokenType.whitespace || test.Type == Token.TokenType.comments)
										continue;
									else if (test.Type == Token.TokenType.identifier)
										break;
									else
										break;
								}
								if (words[fidx].Type != Token.TokenType.identifier)
								{
									continue;
								}

								var identifieridx = fidx;
								if (words[identifieridx].StringValue == "default")
									continue;

								//向前查找其他排除项
								while (fidx > 0)
								{
									fidx = fidx - 1;
									var test = words[fidx];

									if (test.Type == Token.TokenType.whitespace || test.Type == Token.TokenType.comments)
										continue;
									if (test.Type == Token.TokenType.other && (test.StringValue == "." || test.StringValue == "?" || findinkeywords(definekeywords, test.StringValue)))
										goto continue_for;
									else if (test.Type == Token.TokenType.other && test.StringValue == ";")
									{
										break;
									}
									else if (test.Type == Token.TokenType.other && test.StringValue == ":")
									{
										//如果前面又是一个label或者: 则可以通过
										break;
									}
									//else if (test.Type == Token.TokenType.other && test.StringValue == ":")
									//{
									//    //再向前查看一个token,如果不是label则取消
									//    bool pass = false;
									//    var t = fidx;
									//    while (t > 0)
									//    {
									//        t = t - 1;
									//        var tt = words[t];
									//        if (tt.Type == Token.TokenType.whitespace || tt.Type == Token.TokenType.comments)
									//            continue;
									//        else if (tt.Type == Token.TokenType.label)
									//        {
									//            pass = true; break;
									//        }
									//        else
									//            break;
									//    }
									//    if (pass)
									//    {
									//        break;
									//    }
									//    else
									//    {
									//        goto continue_for;
									//    }
									//}
									else if (test.Type == Token.TokenType.identifier && test.StringValue == "case")
									{
										goto continue_for;
									}
									else
									{
										break;
									}
								}


                                if (!s_label.ContainsKey(words[i]))
                                {
									s_label.Add(words[i], words[identifieridx]);

									words[identifieridx].Type = Token.TokenType.label;
                                    var temp = words[identifieridx];
                                    //'**将label:移动到关键字后面去
                                    words[identifieridx] = words[i]; //'关键字向前移动
                                    words[i] = words[stepidx];       //'冒号后移
                                    words[stepidx] = temp;


                                }
                                else
                                {
									//将label设为无用
									words[identifieridx].Type = Token.TokenType.comments;
									words[stepidx].Type = Token.TokenType.comments;

									var olbl = s_label[words[i]];
									olbl.StringValue += "@" + words[identifieridx].StringValue;
								}

                                exists = true;

							}
						}
					continue_for:
						;
					}



				} while (exists);


            }
            #endregion

            #region 查找行首label
            bool isnewline = true;

            Stack<Stack<Token>> key_tokens = new Stack<Stack<Token>>(); //if (xxx) lbl: while(xxx) lbl:lbl:
            Token last_newline = null; 
            HashSet<Token> is_singleline = new HashSet<Token>();// if (xxx) lbl: 这样的label,如果后面\n后还是一个label,则不管这个换行，仍然算一行
            Token lastlabel = null;
            for (int i = 0; i < words.Count; i++)
            {
                var token = words[i];

                if (token.Type == Token.TokenType.identifier && token.StringValue == "if")
                {
                    key_tokens.Push(new Stack<Token>());
                }
				else if (token.Type == Token.TokenType.identifier && token.StringValue == "while")
				{
					key_tokens.Push(new Stack<Token>());
				}
				else if (token.Type == Token.TokenType.identifier && token.StringValue == "for")
				{
					key_tokens.Push(new Stack<Token>());
				}


                if (token.Type == Token.TokenType.comments)
                {
                    continue;
                }
                else if (token.Type == Token.TokenType.whitespace && token.StringValue == "\n")
                {
                    isnewline = true;

                    if (!is_singleline.Contains(last_newline))
                    {
                        last_newline = token;
                    }
                }
                else if (token.Type == Token.TokenType.other && token.StringValue == "{")
                {
                    //function fn() {}{x: 42};  x 是一个 label;
                    isnewline = true; last_newline = token;
                }
                else if (token.Type == Token.TokenType.other && token.StringValue == ";")
                {
                    isnewline = true; last_newline = token;
                }
                else if (token.Type == Token.TokenType.identifier && token.StringValue == "else")
                {
                    isnewline = true; last_newline = token; is_singleline.Add(token);
                }
                else if (token.Type == Token.TokenType.identifier && token.StringValue == "do")
                {
                    isnewline = true; last_newline = token; is_singleline.Add(token);
                }
                else if (token.Type == Token.TokenType.other && token.StringValue == "(")
                {
                    if (key_tokens.Count > 0)
                    {
                        key_tokens.Peek().Push(token);
                    }
                }
                else if (token.Type == Token.TokenType.other && token.StringValue == ")")
                {
                    if (key_tokens.Count > 0)
                    {
                        var b = key_tokens.Peek();
                        if (b.Count == 0)
                        {
                            throw new SyntaxException(token, ") not match");
                        }
                        else
                        {
                            b.Pop();

                            if (b.Count == 0)
                            {
                                isnewline = true; last_newline = token; is_singleline.Add(token);
                                key_tokens.Pop();

                            }
                            else
                            {
                                isnewline = true; last_newline = token;
                            }
                        }
                    }
                    else
                    {
                        isnewline = true; last_newline = token;
                    }
                }
                else if (token.Type == Token.TokenType.whitespace)
                {
                    continue;
                }
                else if (token.Type == Token.TokenType.label) // aa:do XXX 这种可能出现
                {
                    while (words[++i].StringValue != ":") ;
                    continue;
                }
                else
                {
                    if (isnewline)
                    {
                        if (token.Type == Token.TokenType.identifier && token.StringValue != "default")
                        {
                            int fidx = i + 1;
                            int stepidx = 0;
                            //向后找 ":"
                            while (fidx < words.Count)
                            {
                                var test = words[fidx];
                                if (test.Type == Token.TokenType.comments || test.Type == Token.TokenType.whitespace)
                                {
                                    fidx++;
                                    continue;
                                }
                                else if (test.Type == Token.TokenType.other && test.StringValue == ":")
                                {
                                    if (!mustNotLabel.Contains(test))
                                    {
                                        //可能是一个label,向前找是否需要排除
                                        stepidx = fidx;
                                    }
                                    break;
                                }
                                else
                                {
                                    break;
                                }
                            }

                            if (stepidx > 0) //已经排除了所有不可能项。
                            {
                                //fidx = i;
                                ////向前查找其他排除项
                                //while (fidx > 0)
                                //{
                                //    fidx = fidx - 1;
                                //    var test = words[fidx];

                                //    if (test.Type == Token.TokenType.whitespace || test.Type == Token.TokenType.comments)
                                //        continue;
                                //    if (test.Type == Token.TokenType.other && 
                                //        ( 
                                //            test.StringValue == "(" ||
                                //            test.StringValue == "," ||
                                //            test.StringValue == "." || 
                                //            test.StringValue == "?" || 
                                //            findinkeywords(definekeywords, test.StringValue))

                                //            )
                                //        goto continue_for;
                                //    else if (test.Type == Token.TokenType.other && test.StringValue == ";")
                                //    {
                                //        break;
                                //    }
                                //    else if ((test.Type == Token.TokenType.other && test.StringValue == ":") || test.Type == Token.TokenType.useless_label)
                                //    {
                                //        //如果前面又是一个label或者: 则可以通过
                                //        break;

                                //    }
                                //    else if (test.Type == Token.TokenType.identifier && test.StringValue == "case")
                                //    {
                                //        goto continue_for;
                                //    }
                                //    else
                                //    {
                                //        break;
                                //    }
                                //}

                                if (is_singleline.Contains(last_newline))
                                {
                                    if (lastlabel?.StringValue == token.StringValue)
                                    {
                                        throw new ResolverException(token, "Duplicate label definition.");
                                    }

                                    token.Type = Token.TokenType.comments;
                                }
                                else
                                {
                                    token.Type = Token.TokenType.useless_label;//{ a:XX}  这种，必须保留为useless_labed;
                                }

                                words[stepidx].Type = Token.TokenType.comments;
                                //words[stepidx].StringValue = "";

                                lastlabel = token;

                            }
                            else
                            {
                                isnewline = false;
                            }

                        }
                        else
                        {
                            isnewline = false;
                        }
                    }


                }

            //continue_for:
                ;
            }

			#endregion


			#endregion

			#region 查找 形如 public 命名空间 var，将命名空间移动到public 前面

			for (int i = 0; i < words.Count -2; i++)
            {
                var token = words[i];
                if (token.Type == Token.TokenType.identifier)
                {
                    if (token.StringValue == "public"
                        ||
                        token.StringValue == "private"
                        ||
                        token.StringValue == "internal"
                        ||
                        token.StringValue == "protected"
                        ||
                        token.StringValue == "final"
                         ||
                        token.StringValue == "static"
                        ||
                        token.StringValue == "override"
                        ||
                        token.StringValue == "dynamic"
                        ||
                        token.StringValue == "native"
                        )
                    {

                        int j = 1;int nsindex = 0;bool isbreak = false;
                        var nexttoken = words[i + j];
                        Token ns = null;
                        while (nexttoken.Type == Token.TokenType.identifier || nexttoken.Type == Token.TokenType.whitespace || nexttoken.Type == Token.TokenType.comments)
                        {
                            if (nexttoken.Type == Token.TokenType.whitespace)
                            {
                                j++;
                                nexttoken = words[i + j];
                                continue;
                            }
                            if (nexttoken.Type == Token.TokenType.comments)
                            {
                                j++;
                                nexttoken = words[i + j];
                                continue;
                            }

                            if (nexttoken.StringValue == "var"
                                ||
                                nexttoken.StringValue == "function"
                                ||
                                nexttoken.StringValue == "const"
                                ||
                                nexttoken.StringValue == "class"
                                ||
                                nexttoken.StringValue == "interface"
                                )
                            {
                                isbreak = true;
                                break;
                            }
                            if (nexttoken.Type == Token.TokenType.identifier)
                            {
                                ns = nexttoken;nsindex = i + j;
                            }
                            j++;
                            nexttoken = words[i + j];
                        }

                        if (ns != null && ns.line == nexttoken.line && isbreak)
                        {
                            words[i] = ns;
                            words[nsindex] = token;
                        }


                    }
                }

            }


            #endregion

            #region 禁止在Vector.<> 后直接放 { 除非这是一个 function a:Vector<> {

            {
                Stack<Token> vector = new Stack<Token>();
                bool checkflag = false;

                Token pre = null;

                bool istypedef = false;

                for (int i = 0; i < words.Count; i++)
                {
                    var t = words[i];

                    if (t.Type == Token.TokenType.comments || t.Type == Token.TokenType.whitespace)
                        continue;
                    if (checkflag)
                    {
                        if (!istypedef && t.Type == Token.TokenType.other && t.StringValue == "{")
                        {
                            throw new SyntaxException(t, "Expecting either a 'semicolon' or a 'new line' here.");
                        }

                        checkflag = false;
                    }

                    if (t.Type == Token.TokenType.other && t.StringValue == "Vector.<")
                    {
                        istypedef = false;
                        if (pre != null)
                        {
                            if (pre.Type == Token.TokenType.other && pre.StringValue == ":")
                            {
                                istypedef = true;
                            }
                        }

                        vector.Push(t);
                    }

					pre = t;

					if (t.Type == Token.TokenType.other && t.StringValue == ">")
                    {
                        if (vector.Count > 0)
                        {
                            vector.Pop();
							if (vector.Count == 0)
							{
                                checkflag = true;
							}
						}
                    }


                }

            }
			#endregion


			#region 再次检查do while中间是不是全是空的
			{
				Stack<Token> do_tokens = new Stack<Token>();

				int skips = 0;

				for (int index = 0; index < words.Count; index++)
				{
					var token = words[index];
                    if (token.Type == Token.TokenType.identifier && token.StringValue == "do")
                    {
                        skips = 0;
                        do_tokens.Push(token);
                    }
                    else if (token.Type == Token.TokenType.identifier && token.StringValue == "while")
                    {
                        if (skips == 0)
                        {
                            throw new SyntaxException(token, "Unable to generate code for 'do'");
                        }

                    }
                    else if (token.Type == Token.TokenType.comments || token.Type == Token.TokenType.whitespace)
                    {

                    }
                    else if (token.Type == Token.TokenType.label)
                    {
                        while (words[++index].StringValue != ":") ; //吃掉紧随的:
                    }
                    else
                    {
                        skips++;
                    }
				}
			}
			#endregion



			input = input.Replace("\r\n", "\n");
            input = input.Replace("\r", "\n");

            var ls = input.Split("\n");
            words.Reset();
            words.GetNextToken();



            var stack = new Stack<ParseNode>();
            stack.Push(ParseNode.GNodeEOF);
            stack.Push(glines[0].Main);

            var X = stack.Peek();
            string matched = "";

            while (!X.Equals( ParseNode.GNodeEOF))
            {
                if (MathGNodeAndToken(X, words.CurrentToken))
                {
                    var tnode = treenodestack.Pop();
                    tnode.MatchedToken = words.CurrentToken;
                    stack.Pop();
                    matched = matched + words.CurrentToken.StringValue;
                    words.GetNextTokenWithWhiteBlank();
                }
                else if (termianlnodes.ContainsKey(X))
                {
                    if (GetGNode(words.CurrentToken).Type == ParseNodeType.whitespace)
                    {
                        //吃掉无用空白
                        words.GetNextTokenWithWhiteBlank();
                    }
                    else
                    {
                        ThrowError("无法匹配", words.CurrentToken, ls);
                        var node = treenodestack.Pop();
                        node.InputToken = words.CurrentToken;
                        break;
                    }
                }
                else if (!M[X].ContainsKey(GetGNode(words.CurrentToken)))
                {
                    if (GetGNode(words.CurrentToken).Type == ParseNodeType.whitespace)
                    {
                        //吃掉无用空白
                        words.GetNextTokenWithWhiteBlank();
                    }
                    else
                    {
                        ThrowError("不可接受", words.CurrentToken, ls);
                        var node = treenodestack.Pop();
                        node.InputToken = words.CurrentToken;
                        break;
                    }
                }
                else if (M[X][GetGNode(words.CurrentToken)] == null)
                {
                    if (GetGNode(words.CurrentToken).Type == ParseNodeType.whitespace)
                    {
                        //吃掉无用空白
                        words.GetNextTokenWithWhiteBlank();
                    }
                    else
                    {
                        ThrowError("无法匹配", words.CurrentToken, ls);
                        var node = treenodestack.Pop();
                        node.InputToken = words.CurrentToken;
                        break;
                    }
                }
                else
                {
                    //输出产生式
                    var line = M[X][GetGNode(words.CurrentToken)];
                    var node = treenodestack.Pop();
                    node.MatchedToken = words.CurrentToken;
                    node.SelectGrammerLine = line;

                    if (!(line.Derivation.Count == 1 && line.Derivation[0].Type == ParseNodeType._null))
                    { 
                        for (int index = 0; index < line.Derivation.Count; index++)
                        {
                            var tnode = new ParseExpr();
                            tnode.GrammerLeftNode = line.Derivation[index];
                            node.Nodes.Add(tnode);
                            tnode.Parent = node;
                        }

                        for (int index = node.Nodes.Count - 1; index >= 0; index--)
                        {
                            treenodestack.Push(node.Nodes[index]);
                        }
                    }

                    stack.Pop();

                    if (!(line.Derivation.Count == 1 && line.Derivation[0].Type == ParseNodeType._null))
                    {
                        for (int index = line.Derivation.Count-1; index >=0; index--)
                        {
                            stack.Push(line.Derivation[index]);
                        }
                    }
                }

                X = stack.Peek();
            }

            return tree;
        }

        private bool findinkeywords(IEnumerable<string> definekeywords, string stringValue)
        {
            foreach (string keyword in definekeywords) 
            {
                if(stringValue == keyword)
                { 
                    return true; 
                }
            }
            return false;     
        }

        private void ThrowError(string msg, Token token, string[] lines)
        {
            hasError = true;

            ErrorOut.WriteLine(token.sourceFileFullPath + ":" + (token.line + 1) + ":Error:" + msg);
            ErrorOut.WriteLine(lines[token.line].Replace("\t","    "));
            ErrorOut.WriteLine("^".PadLeft(token.ptr));
            ErrorOut.WriteLine();
        }

        private ParseNode GetGNode(Token token)
        {
            if (token.Type == Token.TokenType.eof)
                return ParseNode.GNodeEOF;

            if( token.Type == Token.TokenType.whitespace || token.Type == Token.TokenType.comments )
                return ParseNode.GNodeWhiteSpace;

            //定义的关键字优先
            foreach (var k in _terminals)
            {
                if(MathGNodeAndToken(k,token))
                { 
                    return k; 
                }
            }

            //标识符其次
            foreach (var k in _identifiers)
            {
                if(MathGNodeAndToken(k,token))
                {  
                    return k;
                }
            }

            foreach (var k in _other)
            {
                if (MathGNodeAndToken(k, token))
                {
                    return k;
                }
            }

            return ParseNode.GNodeWrong;

        }

        private bool MathGNodeAndToken(ParseNode node, Token token)
        {
            switch (node.Type)
            {
                case ParseNodeType._null:
                    return true;
                case ParseNodeType.whitespace:
                    if (token.Type == Token.TokenType.eof || token.Type == Token.TokenType.whitespace || token.Type == Token.TokenType.comments)
                        return true;               
                    break;
                case ParseNodeType.conststring:
                    if (token.Type == Token.TokenType.const_string || token.Type == Token.TokenType.const_regexp || token.Type == Token.TokenType.const_xml)
                        return true;             
                    break;
                case ParseNodeType.eof:
                    if(token.Type == Token.TokenType.eof) return true;
                    break;
                case ParseNodeType.identifier:
                    if(token.Type== Token.TokenType.identifier) return true;
                    break;
                case ParseNodeType.label:
                    if(token.Type == Token.TokenType.label) return true;
                    break;
                case ParseNodeType.useless_label:
                    if(token.Type == Token.TokenType.useless_label) return true;
                    break;
                case ParseNodeType._this:
                    if(token.Type == Token.TokenType.this_pointer) return true;
                    break;
                case ParseNodeType.super:
                    if(token.Type == Token.TokenType.super_pointer) return true;
                    break;
                case ParseNodeType.number:
                    if (token.Type == Token.TokenType.const_number) return true;   
                    break;
                case ParseNodeType.terminal:
                    if ((token.Type == Token.TokenType.other || token.Type == Token.TokenType.identifier)
                        &&
                        string.Equals(node.Name,token.StringValue, StringComparison.Ordinal)
                        )
                    { 
                        return true;
                    }
                    break;               
                default:
                    break;
                    
            }
            return false;
        }
    }
}
