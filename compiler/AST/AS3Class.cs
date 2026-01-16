using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using juicescript.compiler.AST.Expr;
using juicescript.runtime;

namespace juicescript.compiler.AST
{
    public sealed class AS3Class : AS3ClassInterfaceBase
    {
        public List<string> ImplementsNames = new List<string>();

        /// <summary>
        /// 暂存Class{  } 内部写的匿名函数
        /// </summary>
        public List<AS3Function> CAnonymousFunction = new List<AS3Function>();
        /// <summary>
        /// 在Class{  } 内部写的表达式，位于CInit代码块内，在类的Class对象初始化后执行一次
        /// Cinit代码块的代码
        /// </summary>
        public List<IAS3SyntaxNode> CInitCodes = new List<IAS3SyntaxNode>();

        /// <summary>
        /// 给类实例成员赋值的代码在实例的构造函数里的代码前执行
        /// </summary>
        public List<IAS3SyntaxNode> Codes = new List<IAS3SyntaxNode>();


        public AS3Class(Token token, AS3SrcFile as3SrcFile) : base(token, as3SrcFile)
        {
            Access.IsInternal = true;
        }

        public override string GetScopeName()
        {
            return Package.Name + "." + Name;
        }

        public override void Write(int v, StringBuilder out_sb)
        {
            for (int i = 0; i < Metas.Count; i++)
            {
                //out_sb.AppendLine("".PadLeft(v, '\t') + "[" + Metas[i].exprStepList[0].Arg2.Data.Value + "(" + Metas[i].exprStepList[0].Arg3 + ")]");
                var meta = Metas[i];
                if (meta.exprStepList.Count == 1)
                {
                    out_sb.AppendLine("".PadLeft(v, '\t') + "[" + Metas[i].exprStepList[0].Arg2.Data.Value + "(" + Metas[i].exprStepList[0].Arg3 + ")]");
                }
                else if (meta.exprStepList.Count == 0)
                {
                    out_sb.AppendLine("".PadLeft(v, '\t') + "[" + ((List<AS3DataStackElement>)meta.Value.Data.Value)[0] + "]");
                }
                else
                {
                    out_sb.Append("".PadLeft(v, '\t') + "[" + meta.exprStepList[meta.exprStepList.Count - 1].Arg2.Data.Value + "(");
                    for (int j = 0; j < meta.exprStepList.Count - 1; j++)
                    {
                        out_sb.Append(meta.exprStepList[j].Arg1.Data.Value.ToString() + " = " + meta.exprStepList[j].Arg2.Data.Value.ToString());
                        if (j < meta.exprStepList.Count - 2)
                        {
                            out_sb.Append(",");
                        }
                    }
                    out_sb.AppendLine(")]");
                }

            }

            Access.Write(v, out_sb);

            out_sb.Append("class " + Name );
            if (ExtendsNames.Count > 0)
            {
                out_sb.Append(" extends " + string.Join(',', ExtendsNames));
            }
            if (ImplementsNames.Count > 0)
            {
                out_sb.Append(" implements " + string.Join(',', ImplementsNames));
            }

            out_sb.AppendLine();

            out_sb.AppendLine( "".PadLeft(v,'\t')+ "{");

            for (int i = 0; i < imports.Count; i++)
            {
                out_sb.AppendLine("".PadLeft(v+1, '\t') + "import " + imports[i] + ";");
            }

            if (imports.Count > 0)
            {
                out_sb.AppendLine();
            }

            for (int i = 0; i < UseNamespaceSet.Count; i++)
            {
                out_sb.AppendLine("".PadLeft(v + 1, '\t') + "use namespace " + UseNamespaceSet[i].UseNameSpace + ";");
            }


            for (int i = 0; i < Members.Count; i++)
            {
                Members[i].Write(v + 1, out_sb);
                out_sb.AppendLine();
            }

            
            for (int i = 0; i < Codes.Count; i++)
            {
                Codes[i].Write(v + 1, out_sb);
                out_sb.AppendLine();
            }



            out_sb.AppendLine( "".PadLeft(v,'\t')+ "}");


            out_sb.AppendLine( "".PadLeft(v,'\t')+ Name + "$cinit // 当Class创建时调用");
            out_sb.AppendLine("".PadLeft(v, '\t') + "{");

            for (int i = 0; i < CAnonymousFunction.Count; i++)
            {
                CAnonymousFunction[i].Write(v + 1, out_sb);
                out_sb.AppendLine();
            }

            for (int i = 0; i < CInitCodes.Count; i++)
            {
                CInitCodes[i].Write(v + 1, out_sb);
                out_sb.AppendLine();
            }

            out_sb.AppendLine("".PadLeft(v, '\t') + "}");

        }
    }
}
