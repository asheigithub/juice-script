using juicescript.ABC;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace juicescript.runtime
{
    /// <summary>
    /// 记录运行时错误堆栈
    /// </summary>
    public class ErrorStackTrace
    {
        public void Clear()
        {
            count = 0;
        }

        public void AddTrace(ASMethod method,int PC)
        {
            methods[count] = method;
            points[count] = PC;
            count++;
        }


        int count;

        ASMethod[] methods = new ASMethod[Context.MAX_BACKTRACE];
        int[] points = new int[Context.MAX_BACKTRACE];

        private int GetLine(int index)
        {
            ASMethodBody.MethodBodyInfo info = new ASMethodBody.MethodBodyInfo();
            methods[index].Body.GetInfo(ref info);

            unsafe
            {
                fixed (void* p = methods[index].Body.ByteCode)
                { 
                    Span<int> ints = new Span<int>( (int*)p + 3 ,info.instructions * 2 );
                    int pc_ptr = points[index];

                    //               for (int i = 0; i < ints.Length - 1; i += 2)
                    //               {
                    //                   if (pc_ptr >= ints[i] && pc_ptr < ints[i+2])
                    //                   {
                    //                       if (ints[i + 1] < 0)
                    //                       {
                    //                           return -1;
                    //                       }
                    //                       else
                    //                       {
                    //                           return ints[i + 1]
                    //                               + 1 //修正行数从1开始计数
                    //                               ;
                    //                       }
                    //                   }

                    //}

                    int lo = 2;
                    int hi = ints.Length - 1;

                    while (lo < hi)
                    {
                        int middle = (lo + ((hi - lo) / 2)) & (~1);

                        if (pc_ptr >= ints[middle - 2] && pc_ptr < ints[middle])
                        {
                            if (ints[middle - 1] < 0)
                            {
                                return -1;
                            }
                            else
                            {
                                return ints[middle - 1]
                                    + 1 //修正行数从1开始计数
                                    ;
                            }
                        }
                        else if (pc_ptr < ints[middle])
                        {
                            hi = middle;
                        }
                        else
                        {
                            lo = middle + 2;
                        }
                    }

#if DEBUG
                    //未找到？不可能的说
                    throw new InvalidOperationException();
#else
					Environment.FailFast("出错了，这里跑不到");
					return -1;
#endif
                }
            }

            
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < count; i++)
            {
                sb.Append("\t");

                int line;

                if (methods[i].Flags.HasFlag(MethodFlags.Native))
                {
                    line =  methods[i].Token.line + 1 ;
                }
                else
                {
                    line = GetLine(i);
                }
                if (methods[i].IsConstructor)
                {
                    string srcpath;
                    if (methods[i].Container is ASClass)
                    {
                        srcpath = ((ASClass)methods[i].Container).Token.sourceFileFullPath;
                    }
                    else if (methods[i].Container is ASScript)
                    {
#if DEBUG
                        throw new InvalidOperationException();
#else
                        Environment.FailFast("出错了，这里跑不到");return default;
#endif
                        //srcpath = methods[i].Container.Traits[0].Token.sourceFileFullPath;
                    }
                    else
                    {
                        srcpath = ((ASInstance)methods[i].Container)._link_codescope.Parent.Container.Traits[0].Token.sourceFileFullPath;
                    }

                    sb.AppendLine($"{methods[i].Container.QName.Namespace.Name}.{methods[i].Container.QName.Name}  at {srcpath}{(line >= 0 ? (":" + line.ToString()) : string.Empty)}");
                }
                else if (methods[i].Trait == null)
                {
                    if (string.IsNullOrEmpty(methods[i].Name))
                    {
                        sb.AppendLine($"Function/{System.IO.Path.GetFileName(methods[i].Token.sourceFileFullPath)}${methods[i].Token.line + 1}:anonymous({methods[i].Token.sourceFileFullPath}{(line >= 0 ? line : methods[i].Token.line + 1)})");
                    }
                    else if (methods[i].Container.QName == null)
                    {
                        if (methods[i].Container is ASMethodBody)
                        {
                            var containerqname = ((ASMethodBody)methods[i].Container).Method.Container.QName;
                            if (containerqname != null)
                            {
                                sb.AppendLine($"{containerqname.Namespace.Name}.{containerqname.Name}/{methods[i].Name}  at {methods[i].Container.Traits[0].Token.sourceFileFullPath}{(line >= 0 ? (":" + line.ToString()) : string.Empty)}");
                            }
                            else
                            {
								sb.AppendLine($"?/{methods[i].Name}  at {methods[i].Container.Traits[0].Token.sourceFileFullPath}{(line >= 0 ? (":" + line.ToString()) : string.Empty)}");
							}
						}
                        else
                        {
                            sb.AppendLine($"?/{methods[i].Name}  at {methods[i].Container.Traits[0].Token.sourceFileFullPath}{(line >= 0 ? (":" + line.ToString()) : string.Empty)}");
                        }
                    }
                    else
                    {
                        sb.AppendLine($"{methods[i].Container.QName.Namespace.Name}.{methods[i].Container.QName.Name}/{methods[i].Name}  at {methods[i].Container.Traits[0].Token.sourceFileFullPath}{(line >= 0 ? (":" + line.ToString()) : string.Empty)}");
                    }
                }
                else
                {
                    var t = methods[i].Trait;

                    switch (t.QName.Namespace.Kind)
                    {

                        case NamespaceKind.Package:
                        case NamespaceKind.Protected:
                        case NamespaceKind.StaticProtected:
                        case NamespaceKind.Private:

                            sb.AppendLine($"{methods[i].Container.QName.Namespace.Name}.{methods[i].Container.QName.Name}/{t.QName.Name} at {methods[i].Token.sourceFileFullPath}{(line >= 0 ? (":" + line.ToString()) : string.Empty)}");
                            break;
                        default:
                            if (string.IsNullOrEmpty(t.QName.Namespace.Name))
                            {
                                sb.AppendLine($"{methods[i].Container.QName.Namespace.Name}.{methods[i].Container.QName.Name}/{t.QName.Name} at {methods[i].Token.sourceFileFullPath}{(line >= 0 ? (":" + line.ToString()) : string.Empty)}");
                            }
                            else
                            {
                                sb.AppendLine($"{methods[i].Container.QName.Namespace.Name}.{methods[i].Container.QName.Name}/{t.QName.Namespace.Name}::{t.QName.Name} at {methods[i].Token.sourceFileFullPath}{(line >= 0 ? (":" + line.ToString()) : string.Empty)}");
                            }

                            break;
                    }
                }
                
            }


            return sb.ToString();


        }

    }
}
