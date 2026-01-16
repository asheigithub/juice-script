using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler.AST.Expr
{
    /// <summary>
    /// 抽象语法树中的操作指令类型
    /// </summary>
    public enum OpType
    {
        /// <summary>
        /// 赋值 = 
        /// </summary>
        Assigning,

        /// <summary>
        /// 加载
        /// </summary>
        Load,

        /// <summary>
        /// 位或|
        /// </summary>
        BitOr,
        /// <summary>
        /// 异或 ^
        /// </summary>
        BitXor,
        /// <summary>
        /// 位与
        /// </summary>
        BitAnd,
        /// <summary>
        /// 逻辑相等 ==, ===, !=, !==
        /// </summary>
        LogicEQ,
        /// <summary>
        /// 逻辑运算 >,>= ...
        /// </summary>
        Logic,
        /// <summary>
        /// 移位
        /// </summary>
        BitShift,

        /// <summary>
        /// 加减 + -
        /// </summary>
        Plus,

        /// <summary>
        /// 乘除 * / %
        /// </summary>
        Multiply,

        /// <summary>
        /// 前置运算符 ++ -- new等
        /// </summary>
        Unary,

        /// <summary>
        /// new 构造实例
        /// </summary>
        Constructor,

        /// <summary>
        /// 成员访问 . , [] 
        /// </summary>
        Access,

        E4XAccess,

        E4XFilter,

        NameSpaceAccess,

        CallFunc,

        Suffix,

        Flag,

        IF_True_Goto,

        IF_False_Goto,

        GotoFlag
    }



    /// <summary>
    /// 表达式一条步骤
    /// </summary>
    public class AS3ExprStep
    {

        public OpType Type;

        public string OpCode;

        public AS3DataStackElement Arg1;
        public AS3DataStackElement Arg2;
        public AS3DataStackElement Arg3;

        public Token token;

        public AS3ExprStep(Token token)
        { 
            this.token = token; 
        }


        public override string ToString()
        {
            if (Type == OpType.GotoFlag)
            {
                return ("[" + Type.ToString() + "]").PadRight(15) + "Goto " + OpCode + ";";
            }
            else if (Type == OpType.Flag)
            {
                return ("[" + Type.ToString() + "]").PadRight(15) + OpCode + ":";
            }
            else if (Type == OpType.IF_True_Goto)
            {
                return ("[" + Type.ToString() + "]").PadRight(15) + "IF \t (" + Arg1.ToString() + ") Goto " + OpCode + ";";
            }
            else if (Type == OpType.IF_False_Goto)
            {
                return ("[" + Type.ToString() + "]").PadRight(15) + "IF \t!(" + Arg1.ToString() + ") Goto " + OpCode + ";";
            }
            else if (Type == OpType.E4XFilter)
            {
                return
                    ("[" + Type.ToString() + "]").PadRight(15) + OpCode + " \t" + Arg1.ToString() + "\t" + Arg2.ToString() + "\n" +
                        "\t\t(\n\t\t\t" + (!(Arg3.Data.Value is List<AS3ExprStep>) ? Arg3.ToString() : string.Join("\n\t\t\t", (List<AS3ExprStep>)Arg3.Data.Value)) + "\n\t\t)";
            }
            else if (Type == OpType.E4XAccess)
            {
                string result = ("[" + Type.ToString() + "]").PadRight(15) + OpCode + " \t";
                if (Arg1 != null)
                {
                    result += Arg1.ToString();
                }
                if (Arg2 != null)
                {
                    result += " \t" + Arg2.ToString();
                }
                else
                {
                    result += " \t<null>";
                }
                if (Arg3 != null)
                {
                    result += " \t" + Arg3.ToString();
                }


                return result;
            }
            else
            {

                string result = ("[" + Type.ToString() + "]").PadRight(15) + OpCode + " \t";
                if (Arg1 != null)
                {
                    result += Arg1.ToString();
                }
                if (Arg2 != null)
                {
                    result += " \t" + Arg2.ToString();
                }
                if (Arg3 != null)
                {
                    result += " \t" + Arg3.ToString();
                }


                return result;
            }
        }

    }
}
