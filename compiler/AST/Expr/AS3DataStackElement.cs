using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace juicescript.compiler.AST.Expr
{
    public class AS3DataStackElement
    {
        /// <summary>
        /// 是否寄存器
        /// </summary>
        public bool IsReg;


        public AS3DataValue Data;


        public AS3Reg Reg;

        public static AS3DataStackElement MakeReg(int regno)
        { 
            var r = new AS3DataStackElement();
            r.IsReg = true;
            r.Reg = new AS3Reg(regno);

            return r;
        }


        public override string ToString()
        {
            if (IsReg)
            {
                return "<#" + Reg.ID + ">";
            }
            else if (Data.Value != null)
            {
                if (Data.FF1Type == FF1DataValueType.const_string)
                {
                    return
                        "\"" + Data.Value.ToString().Replace("\\", "\\\\").Replace("\b", "\\b").Replace("\f", "\\f").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t").Replace("\"", "\"\"") + "\"";
                }
                else if (Data.Value is List<AS3DataStackElement>)
                {
                    string result = "[";
                    for (int i = 0; i < ((List<AS3DataStackElement>)Data.Value).Count; i++)
                    {
                        result += ((List<AS3DataStackElement>)Data.Value)[i].ToString();
                        if (i < ((List<AS3DataStackElement>)Data.Value).Count - 1)
                        {
                            result += ",";
                        }
                    }

                    result += "]";

                    return result;
                }
                else if (Data.Value is Hashtable)
                {
                    var hashtable = (Hashtable)Data.Value;
                    string result = "{";
                    foreach (var k in hashtable.Keys)
                    {
                        result += ((Token)k).StringValue + ":" + hashtable[k].ToString();
                        result += ",";
                    }
                    if (result.Length > 1)
                    {
                        result = result.Substring(0, result.Length - 1);
                    }
                    result = result + "}";
                    return result;
                }
                else if (Data.Value is AS3Vector)
                {
                    string result = Data.Value.ToString() + "(";
                    if (((AS3Vector)Data.Value).Constructor != null)
                    {
                        List<AS3DataStackElement> args = ((AS3Vector)Data.Value).Constructor.Data.Value as List<AS3DataStackElement>;
                        for (int i = 0; i < args.Count; i++)
                        {
                            result += args[i].ToString();
                            if (i < args.Count - 1)
                            {
                                result += ",";
                            }
                        }

                    }

                    result += ")";

                    return result;
                }
                else if (Data.Value is AS3Function)
                {
                    AS3Function f = (AS3Function)Data.Value;

                    if (f.IsAnonymous)
                    {
                        return "closure function @id = " + f.ClosureId;
                    }
                    else
                    {
                        return f.Name;
                    }
                }
                else
                {
                    return Data.Value.ToString();
                }
            }
            else
            {
                return "not parsed";
            }
        }

    }
}
