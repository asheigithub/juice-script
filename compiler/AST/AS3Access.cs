using juicescript.compiler.parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler.AST
{
    /// <summary>
    /// 成员修饰符
    /// </summary>
    public sealed class AS3Access
    {
        public bool IsPublic;
        public bool IsPrivate;
        public bool IsInternal;
        public bool IsFinal;
        public bool IsDynamic;
        public bool IsStatic;
        public bool IsOverride;
        public bool IsProtected;
        public bool IsNative;
        public bool IsAsync;

        public string NameSpace;
        public Token NameSpaceToken;

        public void Set(List<Tuple<string,parse.ParseExpr>> strings)
        {
            IsPublic = false;
            IsPrivate = false;
            IsInternal = false;
            IsFinal = false;
            IsDynamic = false;
            IsStatic = false;
            IsOverride = false;
            IsProtected = false;
            IsNative = false;
            IsAsync = false;

            int c = 0;
            Token token = null;


            foreach (var s in strings) 
            {
                if (s.Item1 == "public")
                {
                    token = s.Item2.MatchedToken;
                    c++;
                    if (!IsPublic)
                    {
                        IsPublic = true;
                    }
                    else
                    {
                        throw new parse.SyntaxException(s.Item2.MatchedToken, "Only one of public, private, protected, or internal can be specified on a definition.");
                    }
                }
                else if (s.Item1 == "private")
                {
                    token = s.Item2.MatchedToken;
                    c++;
                    if (!IsPrivate)
                        IsPrivate = true;
                    else
                    {
                        throw new parse.SyntaxException(s.Item2.MatchedToken, "Only one of public, private, protected, or internal can be specified on a definition.");
                    }
                }
                else if (s.Item1 == "internal")
                {
                    token = s.Item2.MatchedToken;
                    c++;
                    if (!IsInternal)
                        IsInternal = true;
                    else
                    {
                        throw new parse.SyntaxException(s.Item2.MatchedToken, "Only one of public, private, protected, or internal can be specified on a definition.");
                    }
                }
                else if (s.Item1 == "final")
                {
                    if (!IsFinal)
                        IsFinal = true;
                    else
                    {
                        throw new parse.SyntaxException(s.Item2.MatchedToken, "Attribute final was specified multiple times.");
                    }
                }
                else if (s.Item1 == "dynamic")
                {
                    if (!IsDynamic)
                        IsDynamic = true;
                    else
                    {
                        throw new parse.SyntaxException(s.Item2.MatchedToken, "Attribute dynamic was specified multiple times.");
                    }
                }
                else if (s.Item1 == "static")
                {
                    if (!IsStatic)
                        IsStatic = true;
                    else
                    {
                        throw new parse.SyntaxException(s.Item2.MatchedToken, "Attribute static was specified multiple times.");
                    }
                }
                else if (s.Item1 == "override")
                {
                    if (!IsOverride)
                        IsOverride = true;
                    else
                    {
                        throw new parse.SyntaxException(s.Item2.MatchedToken, "Attribute override was specified multiple times.");
                    }
                }
                else if (s.Item1 == "protected")
                {
                    token = s.Item2.MatchedToken;
                    c++;
                    if (!IsProtected)
                        IsProtected = true;
                    else
                    {
                        throw new parse.SyntaxException(s.Item2.MatchedToken, "Only one of public, private, protected, or internal can be specified on a definition.");
                    }
                }
                else if (s.Item1 == "native")
                {
                    if (!IsNative)
                        IsNative = true;
                    else
                    {
                        throw new parse.SyntaxException(s.Item2.MatchedToken, "Attribute native was specified multiple times.");
                    }
                }
                else if (s.Item1 == "async")
                {
                    if (!IsAsync)
                        IsAsync = true;
                    else
                    {
                        throw new parse.SyntaxException(s.Item2.MatchedToken, "Attribute async was specified multiple times.");
                    }
                }

                else if (s.Item1 == "virtual")
                { 
                    
                }
            }

            //Syntax error: Only one of public, private, protected, or internal can be specified on a definition.
           
            if (c > 1)
            {
                throw new SyntaxException(token, "Only one of public, private, protected, or internal can be specified on a definition.");
            }

        }


        public void CheckNSException()
        {
            if (!string.IsNullOrEmpty(NameSpace))
            {
                if (IsPublic || IsPrivate || IsInternal || IsProtected)
                {
                    throw new SyntaxException(NameSpaceToken, "Access specifiers are not allowed with namespace attributes.");    
                }
            }
        }

        

        public override string ToString()
        {
            //return base.ToString();
            string result = "";

            if (IsPublic)
                result += "public ";
            if (IsPrivate)
                result += "private ";
            if (IsInternal)
                result += "internal ";
            if (IsFinal)
                result += "final ";
            if (IsDynamic)
                result += "dynamic ";
            if (IsStatic)
                result += "static ";
            if (IsOverride)
                result += "override ";
            if (IsProtected)
                result += "protected ";
            if (IsNative)
                result += "native ";
            if (IsAsync)
                result += "async";

            if (!string.IsNullOrEmpty(NameSpace))
                result += NameSpace + " ";

            return result;
        }

        internal void Write(int v, StringBuilder out_sb)
        {
            out_sb.Append("".PadLeft(v,'\t'));
            out_sb.Append(ToString());
        }
    }
}
