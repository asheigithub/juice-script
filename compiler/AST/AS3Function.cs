using juicescript.compiler.AST.Expr;
using juicescript.compiler.AST.Stmt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler.AST
{
    public sealed class AS3Function : AS3Member
    {
        public class AS3FunctionScope : AS3MemberScope
        {
            public AS3Function function;

            public AS3MemberScope ParentScope;

            //这是一个在cache块里定义的function
			public List<AS3Variable> catch_variables = new List<AS3Variable>();


			public List<IAS3SyntaxNode> Codes = new List<IAS3SyntaxNode>();

            public AS3FunctionScope(AS3Function function)
            {
                this.function = function;
            }

            private List<AS3Use> _ns_set = new List<AS3Use>();
            public override List<AS3Use> UseNamespaceSet
            {
                get
                {
                    return _ns_set;
                }
            }


            public override string GetScopeName()
            {
                if (function.IsAnonymous)
                {
                    return ParentScope.GetScopeName() + "/@" + function.ClosureId;
                }
                else
                {
                    return ParentScope.GetScopeName() + "/" + function.Name;
                }
            }
        }

        public AS3Function(Token token) : base(token)
        {
            FunctionScope = new AS3FunctionScope(this);
            TypeStr = "*";
        }

        public AS3FunctionScope FunctionScope;

        /// <summary>
        /// 是否直接定义在 package{} 里
        /// </summary>
        public bool IsAtPackageMemberScope;

        /// <summary>
        /// 是否是绑定于类的方法
        /// </summary>
        public bool IsMethod;

        /// <summary>
        /// 是否是get方法
        /// </summary>
        public bool IsGet;

        /// <summary>
        /// 是否是set方法
        /// </summary>
        public bool IsSet;

        /// <summary>
        /// 形参
        /// </summary>
        public List<AS3Parameter> Parameters = new List<AS3Parameter>();

        /// <summary>
        /// 是否匿名
        /// </summary>
        public bool IsAnonymous;

        /// <summary>
        /// 是否是类的构造函数
        /// </summary>
        public bool IsConstructor;

        /// <summary>
        /// 如果是匿名函数，需提供匿名函数ID
        /// </summary>
        public string ClosureId;

        
        public override void Write(int v, StringBuilder out_sb)
        {

            if (IsAnonymous)
            {
                out_sb.AppendLine("".PadLeft(v, '\t') + "// closure @funid=" + ClosureId);
            }
            base.Write(v, out_sb);
            out_sb.Append( "function "  );

            if (IsGet)
            {
                out_sb.Append("get ");
            }
            if (IsSet)
            { 
                out_sb.Append("set ");
            }


            out_sb.Append( Name + "(");

            for (int i = 0; i < Parameters.Count; i++)
            {
                var p = Parameters[i];

                if (p.IsArrPara)
                {
                    out_sb.Append("...");
                    out_sb.Append(p.Name );
                }
                else
                {

                    out_sb.Append(p.Name + ":" + p.TypeStr);

                    if (p.ValueExpr != null)
                    {
                        out_sb.Append(" = " + p.ValueExpr.Value.ToString());
                    }
                }

                if (i < Parameters.Count - 1)
                { 
                    out_sb.Append(',');
                }
                
            }


            out_sb.Append(')');

            if (!IsConstructor)
            {
                out_sb.Append(":" + TypeStr);
            }

            if (!Access.IsNative)
            {
                out_sb.AppendLine();
                out_sb.AppendLine("".PadLeft(v, '\t') + "{");

                for (int i = 0; i < FunctionScope.UseNamespaceSet.Count; i++)
                {
                    out_sb.AppendLine("".PadLeft(v + 1, '\t') + "use namespace " + FunctionScope.UseNamespaceSet[i].UseNameSpace + ";");
                }


                for (int i = 0; i < FunctionScope.Members.Count; i++)
                {
                    FunctionScope.Members[i].Write(v + 1, out_sb);
                    out_sb.AppendLine();
                }

                for (int i = 0; i < FunctionScope.Codes.Count; i++)
                {
                    FunctionScope.Codes[i].Write(v + 1, out_sb);
                    out_sb.AppendLine();
                }


                out_sb.AppendLine("".PadLeft(v, '\t') + "}");
            }
            else
            {
                out_sb.AppendLine(";");
            }
        }

    }
}
