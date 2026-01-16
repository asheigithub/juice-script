using juicescript.ABC;
using juicescript.compiler.parse;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler
{
    /// <summary>
    /// 用户定义命名空间解析
    /// </summary>
    public class NameSpaceResolver
    {
        internal static int BuildNamespace(CompileContext context)
        {
            foreach (var script in context.scriptDefs)
            {         
                foreach (var c in script.containers)
                {
                    foreach (var trait in c.Traits)
                    {
                        if (trait.QName.Namespace.Kind == ABC.NamespaceKind.TBD)
                        {
                            var package_imports = context.scriptDef_packageimports[script];
                            var script_imports = context.scriptDef_scriptimports[script];

                            if (c is ASInstance || c is ASClass)
                            {
                                if (script.Script.Traits[0].QName == c.QName  )
                                {
                                    //trait.QName.Namespace.Name 不可和script中的trait.QName.Name重名，否则报错。。。
                                    if (script.Script.Traits.Any((t) => t.QName.Name == trait.QName.Namespace.Name))
                                    {
                                        throw new SyntaxException((Token)trait.Token, "Namespace was not found or is not a compile-time constant.");
                                    }
                                }
                            }

                            var ns = CompilerResolveUtil.FindNameSpace(context,c, script,trait.QName.Namespace,
                                c.QName.Namespace.Kind == NamespaceKind.Private ? script_imports : package_imports
                                );

                            if (ns.Count == 0)
                            {
                                throw new SyntaxException((Token)trait.Token, "Namespace was not found or is not a compile-time constant.");
                            }

                            if (ns.Count > 1)
                            {
                                throw new SyntaxException(((Token)trait.Token), $"Ambiguous reference to {trait.QName.Namespace.Name}.");
                            }

                            ASMultiname qname = new ASMultiname()
                            {
                                Kind = MultinameKind.QName,
                                Name = trait.QName.Name,
                                Namespace = ns[0]
                            };

                            CompilerResolveUtil.Replace_Ns_Multiname(script, trait.QName, qname);

                            trait.QName = qname;

                        }


                    }
                }

                for (int i = 1; i < script.scriptMethods.Count; i++)
                {
                    var method = script.scriptMethods[i];
                    var use = script.method_use_namesapce[i];

                    HashSet<ASTrait> imports = MethodResolver.GetMethodImports(script, method, context);
                    HashSet<ASNamespace> use_list = new HashSet<ASNamespace>();

                    foreach (var u in use)
                    {
                        var ns_str = u.Item1;
                        
                        var find = CompilerResolveUtil.FindNameSpace(context, method.Body, script, new ASNamespace() { Kind = NamespaceKind.TBD, Name = u.Item1 } , imports);

                        if (find.Count == 0)
                        {
                            throw new SyntaxException(u.Item2, $"Unknown namespace {ns_str}.");
                        }

                        if (find.Count > 1)
                        {
                            throw new SyntaxException(u.Item2, $"Ambiguous reference to {ns_str}.");
                        }

                        use_list.Add(find[0]);
                    }

                    if (use_list.Count > 0)
                    {
                        var set = new ASNamespaceSet();
                        set.Namespaces = use_list.ToList();
                        if (!script.namespaceSets.Contains(set))
                        {
                            script.namespaceSets.Add(set);
                        }

                        method.Body.NamespaceSetIndex = script.namespaceSets.IndexOf(set);

                    }
                }
            }



            return 0;
            //throw new NotImplementedException();
        }
    }
}
