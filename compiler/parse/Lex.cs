using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace juicescript.compiler.parse
{
    /// <summary>
    /// 词法分析器
    /// </summary>
    public class Lex
    {
        public string File;
        public List<string> defineWords;

        public List<string> defineSkipBlankWords;

        private Stack<Token> specBrackets = new Stack<Token>();

        public bool parseXML;

        public Lex(string file)
        {
            File = file;
            defineWords = new List<string>();
            defineSkipBlankWords = new List<string>();
        }

        public Lex(string file, IEnumerable<string> definewords, IEnumerable<string> defineSkipBlankWords, bool parseXML) : this(file)
        {
            if (definewords != null)
            {
                List<string> temp = new List<string>();
                temp.AddRange(definewords);
                temp.Sort((s1, s2) => { return s2.Length - s1.Length; });

                defineWords.AddRange(temp);
            }

            if (defineSkipBlankWords != null)
            {
                List<string> temp = new List<string>();
                temp.AddRange(defineSkipBlankWords);
                temp.Sort((s1, s2) => { return s2.Length - s1.Length; });

                this.defineSkipBlankWords.AddRange(temp);
            }

            this.parseXML = parseXML;

        }

        Token lastword;
        int cline;
        int linepos;

        Token last_notwhitespace_word;

        public TokenList GetWords(string input,bool combieNeg)
        {
            cline = 0;

            input = input.Replace("\r\n", "\n");
            input = input.Replace("\r", "\n");


            int ptr = 0;

            Token word;
            TokenList words = new TokenList();
            words.FileName = File;

            do
            {
                word = getNextToken(input, ref ptr , words);

                if (word != null)
                {
                    words.Add(word);
                }

            } while (word != null);

            cline = 0;
            return words;

        }

        private Token getNextToken(string input, ref int ptr,TokenList preWords)
        {
            int xmllen = 0;
            int currentptr = ptr;
            if (currentptr >= input.Length)
                return null;

            var result = new Token();
            result.sourceFile = File;

            char ch = '\0';

            string tempwhitespace = "";
            bool templf = false;

            while (currentptr < input.Length)
            {
                ch = input[currentptr];

                //处理 \u 开头的Token
				if (currentptr >= 0 && currentptr+1 <input.Length &&  input[currentptr] == '\\' && input[currentptr + 1] == 'u')
				{
					if (currentptr + 5 < input.Length)
					{
						string upattern = input.Substring(currentptr, 6);

						if (Regex.Match(upattern, @"\\u([0-9A-Fa-f]{4})").Success)
						{
							string hex = upattern.Substring(2);
							short code = Convert.ToInt16(hex, 16);

                            ch = (char)code;
                            currentptr += 5;
                            linepos += 5;
                            break;


						}
					}
				}

				if (isWhiteSpace(ch))
                {
                    if (ch == '\n')
                    {
                        templf = true;
                    }
                    else if (ch == '\u2028' || ch == '\u2029') //unicode 行分隔符
                    { 
                        templf = true;
                    }

                    getNextChar(input, ref currentptr);
                    tempwhitespace += " ";
                }
                else
                {
                    if (tempwhitespace.Length > 0)
                    {
                        result.line = cline;
                        result.ptr = linepos;
                        result.Type = Token.TokenType.whitespace;
                        result.StringValue = " ";
                        if (templf)
                        {
                            result.StringValue = "\n";
                        }

                        ptr = currentptr;
                        return result;
                    }
                    break;
                }
            }
            string match_def; int match_len;
            if (ch == '/')
            {
                char nextchar = seeNextChar(input, currentptr);
                if (nextchar == '/') //读注释
                {
                    getNextChar(input, ref currentptr);

                    result.line = cline;
                    result.ptr = linepos;
                    result.Type = Token.TokenType.comments;
                    result.StringValue += "//";

                    do
                    {
                        var nn = getNextChar(input, ref currentptr);
                        if (nn == '\n' || nn == '\0')
                        {
                            break;
                        }
                        else
                        {
                            result.StringValue += nn;
                        }

                    } while (true);
                }
                else if (nextchar == '*') //读注释
                {
                    getNextChar(input, ref currentptr);

                    result.line = cline;
                    result.ptr = linepos;
                    result.Type = Token.TokenType.comments;
                    result.StringValue += "/*";

                    do
                    {
                        var n1 = seeNextChar(input, currentptr);
                        getNextChar(input, ref currentptr);
                        var n2 = seeNextChar(input, currentptr);
                        if (n1 == '*' && n2 == '/')
                        {
                            result.StringValue += "*/";

                            getNextChar(input, ref currentptr);
                            getNextChar(input, ref currentptr);

                            break;
                        }

                        result.StringValue += n1;
                    } while (true);
                }
                else if (last_notwhitespace_word == null
                    || (last_notwhitespace_word.Type == Token.TokenType.other && ExpressionContextTokens.Contains(last_notwhitespace_word.StringValue))
					|| (last_notwhitespace_word.Type == Token.TokenType.other && last_notwhitespace_word.StringValue == ";")
					|| (last_notwhitespace_word.Type == Token.TokenType.identifier && last_notwhitespace_word.StringValue == "return")
					|| (last_notwhitespace_word.Type == Token.TokenType.identifier && last_notwhitespace_word.StringValue == "await")
					|| (last_notwhitespace_word.Type == Token.TokenType.other && last_notwhitespace_word.StringValue == "{")
                    || (last_notwhitespace_word.Type == Token.TokenType.other && last_notwhitespace_word.StringValue == ":" && IsTernary(last_notwhitespace_word, preWords) )
					|| (last_notwhitespace_word.Type == Token.TokenType.other && last_notwhitespace_word.StringValue == "}" && IsBlockStatement(preWords))
                    || (last_notwhitespace_word.Type == Token.TokenType.other && last_notwhitespace_word.StringValue == ">")
                    )
                {
                    result.line = cline;
                    result.ptr = linepos;
                    result.Type = Token.TokenType.const_string;
                    result.StringValue = "/";

                    //读正则模式
                    int s1 = 1;
                    while (s1 > 0)
                    {
                        var nc = getNextChar(input, ref currentptr);
                        if (nc == '/')
                        {
                            s1 -= 1;
                            result.StringValue += nc;

                            if (s1 == 0)
                            {
                                var gc = getNextChar(input, ref currentptr);

                                while (char.IsLetter(gc))
                                {
                                    result.StringValue += gc;
                                    gc = getNextChar(input, ref currentptr);
                                }

                                result.Type = Token.TokenType.const_regexp;
                                //Console.WriteLine("代码中发现内嵌正则表达式" + result.StringValue);
                                //Console.WriteLine("    " + File);
                            }
                        }
                        else if (nc == '\\')
                        {
                            result.StringValue += nc;

                            var s2 = getNextChar(input, ref currentptr);


                            if (s2 == '/')
                            {
                                throw new LexException("Syntax error: Invalid regular expression", cline, linepos);
                            }
                            else
                            {
                                result.StringValue += s2;
                            }

                        }
                        else if (nc == '\0')
                        {
                            throw new LexException("Syntax error: Invalid regular expression", result.line, result.ptr);
                        }
                        else if (isWhiteSpace(nc))
                        {
						
							if (nc == '\n')
							{
								throw new LexException("Syntax error: Invalid regular expression", result.line, linepos);
							}
							else if (nc == '\u2028' || nc == '\u2029') //unicode 行分隔符
							{
								throw new LexException("Syntax error: Invalid regular expression", result.line, linepos);
							}
						}
                        else
                        {
                            result.StringValue += nc;
                        }
                    }

                }
                else if (findIndefinewords(ch, input, currentptr, out match_def, out match_len))
                {
                    result.line = cline;
                    result.ptr = linepos;
                    result.Type = Token.TokenType.other;
                    result.StringValue = match_def;
                    for (int i = 0; i < match_len; i++) { getNextChar(input, ref currentptr); }

                    if (result.StringValue == "Vector.<")
                    {
                        specBrackets.Push(result);
                    }
                    //throw new NotImplementedException();
                }
                else
                {
                    result.line = cline;
                    result.ptr = linepos;
                    result.Type = Token.TokenType.other;
                    result.StringValue = ch.ToString();
                    getNextChar(input, ref currentptr);
                }
            }
            else if (ch == '"' || ch == '\'')
            {
                //读字符串
                result.line = cline;
                result.ptr = linepos;
                result.Type = Token.TokenType.const_string;
                do
                {
                    var nn = getNextChar(input, ref currentptr);
                    if (nn == '\r' || nn == '\0')
                    {
                        break;
                    }
                    if (nn == ch)
                    {
                        getNextChar(input, ref currentptr);
                        break;
                    }
                    else if (nn == '\\')
                    {
                        var n2 = getNextChar(input, ref currentptr);
                        if (n2 == ch)
                        {
                            result.StringValue += ch;
                        }
                        else if (n2 == '\\')
                        {
                            result.StringValue += '\\';
                        }
                        else if (n2 == 'b')
                        {
                            result.StringValue += '\b';
                        }
                        else if (n2 == 'f')
                        {
                            result.StringValue += '\f';
                        }
                        else if (n2 == 'n')
                        {
                            result.StringValue += '\n';
                        }
                        else if (n2 == 'r')
                        {
                            result.StringValue += '\r';
                        }
                        else if (n2 == 't')
                        {
                            result.StringValue += '\t';
                        }
                        else if (n2 == '\n')
                        {
							//result.StringValue += '\n';
						}
                        else
                        {
                            result.StringValue += '\\';
                            result.StringValue += n2;
                        }
                    }
                    else
                    {
                        result.StringValue += nn;
                    }

                } while (true);

				//string decoded = Regex.Replace( result.StringValue , @"\\u([0-9A-Fa-f]{4})", match =>
				//{
				//	string hex = match.Groups[1].Value;
				//	short code = Convert.ToInt16(hex, 16);
				//	return ((char)code).ToString();
				//});

				// 先处理 ES6 的 \u{XXXXXX}
				string decoded = Regex.Replace(result.StringValue, @"\\u\{([0-9A-Fa-f]{1,6})\}", match =>
				{
					string hex = match.Groups[1].Value;
					int codePoint = Convert.ToInt32(hex, 16);
					return char.ConvertFromUtf32(codePoint);
				});

				// 再处理传统的 \uXXXX
				decoded = Regex.Replace(decoded, @"\\u([0-9A-Fa-f]{4})", match =>
				{
					string hex = match.Groups[1].Value;
					short code = Convert.ToInt16(hex, 16);
					return ((char)code).ToString();
				});

				
				result.StringValue = decoded;

			}
			else if (findIndefinewords(ch, input, currentptr, out match_def, out match_len)) //读设定字符
            {
                result.line = cline;
                result.ptr = linepos;
                result.Type = Token.TokenType.other;
                result.StringValue = match_def;
                for (int i = 0; i < match_len; i++) { getNextChar(input, ref currentptr); }

                if (result.StringValue == "Vector.<")
                {
                    specBrackets.Push(result);
                }

            }
            else if (isIdStChar(ch))
            {
                //'***读取标识符
                result.Type = Token.TokenType.identifier;
                result.line = cline;
                result.ptr = linepos;
                result.StringValue += ch;

                do
                {
                    var nn = getNextChar(input, ref currentptr);

                    if (nn == '\\') //处理 \u0066  这种
                    {
                        string test = nn.ToString();int add = 0;
                        var nc = seeNextChar(input, currentptr+add++);
                        while (nc != '\0' && add<6)
                        {
                            test += nc;
                            nc = seeNextChar(input, currentptr + add++);
                        }

						if (Regex.Match(test, @"\\u([0-9A-Fa-f]{4})").Success)
						{
							string hex = test.Substring(2);
							short code = Convert.ToInt16(hex, 16);

							nn = (char)code;
							currentptr += 5;
                            linepos += 5;
						}
					}

                    if (nn == '\0' || !(isIdStChar(nn) || char.IsDigit(nn)))
                    {
                        break;
                    }
                    else
                    {
                        result.StringValue += nn;
                    }

                } while (true);
            }
            else if (ch == '<' && findxml(ch, input, currentptr, ref xmllen) > 0)
            {
                result.line = cline;
                result.ptr = linepos;
                result.Type = Token.TokenType.const_xml;

                result.StringValue = "<";
                for (int i = 1; i < xmllen; i++)
                {
                    result.StringValue += getNextChar(input, ref currentptr);
                }
                getNextChar(input, ref currentptr);

            }
            else if (ch == '0' && ('x' == seeNextChar(input, currentptr) || 'X' == seeNextChar(input, currentptr)))
            {
                result.Type = Token.TokenType.const_number;
                result.line = cline;
                result.ptr = linepos;
                result.StringValue += ch;

                result.StringValue += getNextChar(input, ref currentptr);
                do
                {
                    var nn = getNextChar(input, ref currentptr);
                    if (char.IsDigit(nn) || char.ToLower(nn) == 'a' || char.ToLower(nn) == 'b' || char.ToLower(nn) == 'c' || char.ToLower(nn) == 'd' || char.ToLower(nn) == 'e' || char.ToLower(nn) == 'f')
                    {
                        result.StringValue += nn;
                    }
                    else if (isIdStChar(nn))
                    {
                        throw new LexException("Expecting either a 'semicolon' or a 'new line' here.", cline, linepos);
                    }
                    else
                    {
                        break;
                    }

                } while (true);

            }
            else if (
                char.IsDigit(ch)
                ||
                (ch == '.' && seeNextChar(input, currentptr) != '\0' && char.IsDigit(seeNextChar(input, currentptr)))
                ||
                (ch == '-' && seeNextChar(input, currentptr) != '\0' && (
                                                char.IsDigit( seeNextChar(input,currentptr) ) 
                                                || ( 
                                                    seeNextChar(input,currentptr ) == '.'  &&  char.IsDigit( seeNextChar(input,currentptr + 1))
																							&& '\0' != (seeNextChar(input, currentptr + 1))
                                                    ) )
                                                   
                                                    &&
                                                    (
                                                    last_notwhitespace_word == null ||
                                                    last_notwhitespace_word.Type == Token.TokenType.other  
                                                    || last_notwhitespace_word.StringValue == "return"
                                                    || last_notwhitespace_word.StringValue == "await"
                                                    
                                                    )
                                                   
                                                    )
                 //要正确处理类似 ( -1.toString ) 和 int.Min - 1 ,前者需要解析为-1,后者需要解析为减法。 
                )
            {
                
                result.Type = Token.TokenType.const_number;
                result.line = cline;
                result.ptr = linepos;
                result.StringValue += ch;

                result.StringValue += getNumberSerial(input, ref currentptr, linepos);

                var csymobl = getNextChar(input, ref currentptr);
                if (csymobl == '.')
                {
                    if (result.StringValue[0] == '.')
                    {
                        throw new LexException("Expecting either a 'semicolon' or a 'new line' here.", cline, linepos);
                    }
                    result.StringValue += csymobl;
                    result.StringValue += getNumberSerial(input, ref currentptr, linepos);
                    csymobl = getNextChar(input, ref currentptr);

                }

                if (char.ToLower(csymobl) == 'e')
                {
                    var en1 = seeNextChar(input, currentptr);
                    if (en1 == '+' || en1 == '-')
                    {
                        var en2 = seeNextChar(input, currentptr + 1);
                        if (char.IsDigit(en2))
                        {
                            result.StringValue += csymobl;
                            result.StringValue += getNextChar(input, ref currentptr);
                            result.StringValue += getNumberSerial(input, ref currentptr, linepos);

                            var nextchar = seeNextChar(input, currentptr);
                            //if (!(char.IsWhiteSpace(nextchar) || nextchar == ';' || nextchar == 'f' || nextchar == 'F' ))
                            //{
                            //	throw new LexException("解析数值错误", cline, linepos);
                            //}
                            if ( char.ToLower( nextchar) != 'f')
                            {
                                if (!(char.IsWhiteSpace(nextchar) || nextchar == ';' || nextchar == '(' || nextchar == ')' || nextchar == ','))
                                {
                                    throw new LexException("Expecting either a 'semicolon' or a 'new line' here.", cline, linepos);
                                }
                            }
							csymobl = getNextChar(input, ref currentptr);
                        }
                    }
                    else if (char.IsDigit(en1))
                    {
                        result.StringValue += csymobl;
                        result.StringValue += getNumberSerial(input, ref currentptr, linepos);
                        csymobl = getNextChar(input, ref currentptr);
                    }
                   
                }
                
                if (char.ToLower(csymobl) == 'f')
                {
                    if (result.StringValue.EndsWith('.'))
                    {
                        throw new LexException("Expecting either a 'semicolon' or a 'new line' here.", cline, linepos);
                    }

                    result.StringValue += 'f';

                    csymobl = getNextChar(input, ref currentptr);

					if (char.ToLower(csymobl) != 'f')
					{
						if (!(char.IsWhiteSpace(csymobl) || csymobl == ';' || csymobl == '(' || csymobl == ')'))
						{
							throw new LexException("Expecting either a 'semicolon' or a 'new line' here.", cline, linepos);
						}
					}

				}

				

			}
            else
            {
                result.line = cline;
                result.ptr = linepos;
                result.Type = Token.TokenType.other;
                result.StringValue = ch + "";

                if (isWhiteSpace(ch))
                {
                    result.Type = Token.TokenType.whitespace;
                    if (templf)
                    {
                        result.StringValue = "\n";
                    }
                }

                getNextChar(input, ref currentptr);
            }

            ptr = currentptr;
            lastword = result;

            if (result.Type != Token.TokenType.comments && result.Type != Token.TokenType.whitespace)
            { 
                last_notwhitespace_word = result;
            }

            return result;
        }

		

		internal static readonly HashSet<string> ExpressionContextTokens = new HashSet<string> 
        { 
            "+", "-", "*", "/", "%", "|","&" ,"^", "~" ,"=", "==", "!=", "<", "<=", ">=", ",", "(", "return",
            "throw", "await" ,//"yield", "await",  "=>",
			"++", "--", "||", "||=" 
            , "?"
		    , "&&", "&&=", "<<", ">>", ">>>", "<=", ">="
		    , "==", "!=", "===", "!=="
		    , "+=", "-=", "*=", "/=", "%=", ">>=", "<<=", ">>>=", "&=", "^=", "|="
            , "in" , "instanceof" , "is" , "as"
            ,">"    // > 和 Vector.<*>

		};

		/// <summary>
		/// 检查}前是不是一个BlockStatement
		/// </summary>
		/// <param name="preWords"></param>
		/// <returns></returns>
		/// <exception cref="NotImplementedException"></exception>
		private bool IsBlockStatement(TokenList preWords)
		{

			HashSet<string> BlockKeywords = new HashSet<string> { "function", "if", "else", "for", "while", "switch", "try", "catch", "finally", "with" };

		    bool flag = false;

            Stack<Token> curlyBracket = new Stack<Token>();
			for (int i = preWords.Count -1; i >=0; i--)
            {
                var t = preWords[i];
                if (t.Type == Token.TokenType.whitespace || t.Type == Token.TokenType.comments)
                    continue;

                if (!flag)
                {
                    Debug.Assert(t.StringValue == "}");
                    flag = true;
                    curlyBracket.Push(t);
                }
                else
                {
                    if (t.StringValue == "}")
                    {
                        curlyBracket.Push(t);
                    }
                    else if (t.StringValue == "{")
                    {
                        curlyBracket.Pop();

                        if (curlyBracket.Count == 0)
                        {
                            //判断前面的东西...

                            for (int j = i - 1; j >= 0; j--)
                            {
                                var prev = preWords[j];
                                if (prev.Type == Token.TokenType.whitespace || prev.Type == Token.TokenType.comments) continue;

                                if (prev.StringValue == ";" || prev.StringValue == "{" || prev.StringValue == "}")
                                {
                                    return true;
                                }

                                // Block 关键字或语句边界
                                if (BlockKeywords.Contains(prev.StringValue)) return true;

                                // 表达式上下文
                                if (ExpressionContextTokens.Contains(prev.StringValue)) return false;

                                if (prev.StringValue == ":")
                                {
                                    if (IsTernary(prev , preWords)) //如果是三元运算符，则它是一个表达式.
                                    {
                                        return false;
                                    }
                                    else
                                    {
                                        return true; //要么是个label,要么是 case xx:{}, default :{},这肯定是块。
                                    }
                                }

                                // 默认当作 Block
                                return true;

                            }


						}
                    }
                }

            }

            return true;
		}
		

		private bool IsTernary(Token prev, TokenList preWords)
		{
			Stack<Token> needColon = new Stack<Token>();
			for (int index = 0; index < preWords.Count; index++)
			{
				var token = preWords[index];

				if (token.Type == Token.TokenType.whitespace || token.Type == Token.TokenType.comments) continue;


				if (token.StringValue == "?")
				{
					needColon.Push(token);
				}
				else if (token.StringValue == ":")
				{
                    if (needColon.Count > 0)
                    {
                        needColon.Pop();

                        if (prev == token)
                        {
                            return true;
                        }
                    }
                    else
                    {
						if (prev == token)
						{
							return false;
						}
					}
					
				}
			}

            return false;
		}




		const string partern = "<(?<HtmlTag>[\\w]+)[^>]*>.*?</\\k<HtmlTag>>" +
         "|" +
         "<!\\[CDATA\\[.*?]]>" +
         "|" +
         "<[\\w]+\\s*/>";

        private Regex expr = new Regex(partern, RegexOptions.Compiled | RegexOptions.Singleline);
        private int findxml(char ch, string input, int currentptr, ref int len)
        {
            if (!parseXML)
                return 0;
            if (!char.IsLetterOrDigit(seeNextChar(input, currentptr)) && '!' != seeNextChar(input, currentptr))
            {
                return 0;
            }

            var m = expr.Match(input, currentptr);
            if (m.Success && currentptr == m.Index)
            {
                //Console.WriteLine("代码中发现内嵌XML" + m.Value);

                len = m.Length;
                return m.Length;
            }
            else
            {
                return 0;
            }
        }


        private bool findIndefinewords(char ch, string input, int currentptr, out string match, out int len)
        {
            match = null;
            len = 0;
            if (ch == '>')
            {
                if (specBrackets.Count > 0 && specBrackets.Peek().StringValue == "Vector.<")
                {
                    specBrackets.Pop();

                    return false;
                }
            }

            foreach (var word in defineWords)
            {
                string test = ch.ToString();
                for (var k = 0; k < word.Length - 1; k++)
                {
                    test = test + seeNextChar(input, currentptr + k);
                }

                if (word == test)
                {
                    match = word;
                    len = word.Length;
                    return true;
                }

            }

            foreach (var w in defineSkipBlankWords)
            {
                int w_id = 0;
                int k = currentptr;
                while (w_id<w.Length)
                {
                    int w_c;
                    var w_str = seeNextWord(w,w_id,out w_c);

                    int count;
                    var next = seeNextWord(input, k, out count);
                    while (string.IsNullOrWhiteSpace(next) && count > 0)
                    {
                        k += count;
                        next = seeNextWord(input, k, out count);
                    }
                    

                    if (w_str != next)
                    {
                        break;
                    }
                    
                    w_id += w_c;
                    k += count;
                }

                if (w_id >= w.Length)
                {
                    match = w;
                    len = k-currentptr;
                    return true;
                }


                //var test = ch.ToString();
                //var real = ch.ToString();

                //int k = 0;
                //while (w.StartsWith(test))
                //{
                //    int count;
                //    var next = seeNextWord(input,currentptr + k,out count);

                //    test = test + next; //seeNextChar(input, currentptr + k);
                //    real = real + next; //seeNextChar(input, currentptr + k);
                //    k = k + count;

                //    test = test.TrimEnd();

                //    if (w == test)
                //    {
                //        match = test;
                //        len = real.Length;
                //        return true;
                //    }

                //    if (seeNextChar(input, currentptr + k) == '\0')
                //    {
                //        break;
                //    }
                //}
            }

            return false;
        }

        private string seeNextWord(string input,int currentptr,out int count)
        {
           
            if (currentptr  < input.Length)
            {
                var nn = input[currentptr];
                if (isIdStChar(nn))
                {
                    string result = nn.ToString();


                    int c = 0;
                    while (true)
                    {
                        ++c;
                        nn = seeNextChar(input, currentptr + c-1);

                        if (nn == '\0' || !(isIdStChar(nn) || char.IsDigit(nn)))
                        {
                            break;
                        }
                        else
                        {
                            result += nn;
                        }
                    }

                    count = c;
                    return result;
                }
                else
                {
                    count = 1;
                    return nn.ToString();
                }
            }
            else
            {
                count = 0;
                return "";
            }
        }

        private string getNumberSerial(string input, ref int currentptr, int lineptr)
        {
            string r = string.Empty;
            do
            {
                var nc = seeNextChar(input, currentptr);
                if (nc == '\0')
                {
                    return r;
                }

                if (char.IsDigit(nc))
                {
                    r += getNextChar(input, ref currentptr);
                }
                else if (isIdStChar(nc) && char.ToLower(nc) != 'e' && char.ToLower(nc) !='f')
                {
                    throw new LexException("Expected PAREN_CLOSE", cline, lineptr);
                }
                else
                {
                    return r;
                }

            } while (true);

        }

        private char seeNextChar(string input, int currentptr)
        {
            if (currentptr + 1 < input.Length)
            {
                return input[currentptr + 1];
            }
            else
            {
                return '\0';
            }
        }

        private bool isWhiteSpace(char ch)
        {
            return //ch == ' ' || ch == '　' || ch == ' ' || ch == '\t' || ch == '\r' || ch == '\n' || 
                char.IsWhiteSpace(ch);
        }

        private char getNextChar(string input, ref int currentptr)
        {
            currentptr += 1;

            if (currentptr < input.Length)
            {
                if (input[currentptr - 1] == '\n')
                {
                    cline += 1;
                    linepos = 0;
                }

                if (input[currentptr - 1] == '\t')
                {
                    linepos += 4;
                }
                else
                {
                    linepos += 1;
                }

                return input[currentptr];
            }
            else
            {
                return '\0';
            }

        }

        /// <summary>
        /// 是否可用作标识符起始的字符
        /// </summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        private bool isIdStChar(char ch)
        {
            if (char.IsLetter(ch) || ch == '_' || ch == '$')
            {
                return true;
            }
            else
            {
                return false;
            }
        }

    }
}
