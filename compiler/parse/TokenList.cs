using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler.parse
{
    public class TokenList : List<Token>
    {
        public string FileName;

        private int currentindx;


        public void Reset()
        {
            currentindx = -1;

            for (int i = 1; i < Count; i++)
            {
                var token = this[i];
                token.preToken = this[i-1];
                token.preToken.nextToken = this[i];
            }
        }

        /// <summary>
        ///  获取当前TOKEN
        /// </summary>
        public Token CurrentToken
        {
            get 
            {
                if (currentindx == -1)
                    return null;

                if (currentindx < Count)
                {
                    return this[currentindx];
                }
                else
                {
                    Token eof = new Token();
                    eof.Type = Token.TokenType.eof;
                    eof.sourceFile = FileName;

                    if (Count > 0)
                    {
                        eof.line = this[Count - 1].line;
                        eof.ptr = this[Count - 1].ptr;
                    }


                    return eof;
                }

            }
        }

        /// <summary>
        /// 移动到下一个非空白非注释的TOKEN并返回
        /// </summary>
        /// <returns></returns>
        public Token GetNextToken()
        {
            do
            {
                currentindx += 1;
                if (CurrentToken.Type == Token.TokenType.eof)
                {
                    return CurrentToken;
                }

                if (CurrentToken.Type != Token.TokenType.comments && CurrentToken.Type != Token.TokenType.whitespace)
                {
                    return CurrentToken;
                }

            }
            while (true);
        }


        /// <summary>
        /// 返回下一个符号包括空白
        /// </summary>
        /// <returns></returns>
        public Token GetNextTokenWithWhiteBlank()
        {
            currentindx += 1;
            return CurrentToken;
        }

        public string StringValueToString(Token token)
        {
            if (token.Type == Token.TokenType.const_string)
            {
                string r = "\"";

                for (int i = 0; i < token.StringValue.Length; i++)
                {
                    //    If token.StringValue(index) = """" Then
                    //    r = r + "\"""
                    //ElseIf token.StringValue(index) = vbCr Then
                    //    r = r + "\r"
                    //ElseIf token.StringValue(index) = vbLf Then
                    //    r = r + "\n"
                    //ElseIf token.StringValue(index) = vbFormFeed Then
                    //    r = r + "\f"
                    //ElseIf token.StringValue(index) = vbBack Then
                    //    r = r + "\b"
                    //ElseIf token.StringValue(index) = vbTab Then
                    //    r = r + "\t"
                    //ElseIf token.StringValue(index) = "\" Then
                    //    r = r + "\\"
                    //Else
                    //    r = r + token.StringValue(index)
                    //End If

                    if (token.StringValue[i] == '\"')
                    {
                        r = r + "\\\"";
                    }
                    else if (token.StringValue[i] == '\r')
                    {
                        r = r + "\\r";
                    }
                    else if (token.StringValue[i] == '\n')
                    {
                        r = r + "\\n";
                    }
                    else if (token.StringValue[i] == '\f')
                    {
                        r = r + "\\f";
                    }
                    else if (token.StringValue[i] == '\b')
                    {
                        r = r + "\\b";
                    }
                    else if (token.StringValue[i] == '\t')
                    {
                        r = r + "\\t";
                    }
                    else if (token.StringValue[i] == '\\')
                    {
                        r = r + "\\";
                    }
                    else
                    {
                        r = r + token.StringValue[i];
                    }

                }



                return r + "\"";
            }
            else
            {
                return token.StringValue;
            }
        }


    }
}
