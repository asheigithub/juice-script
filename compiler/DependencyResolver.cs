using juicescript.ABC;
using juicescript.compiler.parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace juicescript.compiler
{
    public class DependencyResolver
    {
        public static int BuildDependency(CompileContext context)
        {
            //to do-- 检查是否和 lib中的类冲突
            foreach (var scriptDef in context.scriptDefs)
            {
                var qname = scriptDef.Script.QName;
                foreach (var lib in context.player_for_compiler.Context.libs)
                {
                    if (lib.Scripts.Any(s => s.QName == qname))
                    {
                        throw new SyntaxException((Token)scriptDef.Script.Traits[0].Token,
                            $"Duplicate definition: {qname} "
                            );
                    }
                }
            }


            List<ABC.ASClass> classes = new List<ABC.ASClass>();

            foreach (var scriptDef in context.scriptDefs) 
            {
                classes.AddRange(scriptDef.scriptClasses.Where(o => o != null)); ;

                if (!context.scriptDef_packageimports.ContainsKey(scriptDef))
                {
                    var package_imports = new HashSet<ASTrait>();
                    var script_imports = new HashSet<ASTrait>();

                    ParseImports(scriptDef, context, package_imports, script_imports);

                    context.scriptDef_packageimports.Add(scriptDef, package_imports);
                    context.scriptDef_scriptimports.Add(scriptDef, script_imports);

                }
            }

            foreach (var classDef in classes) 
            {
                int n = classes.Count((o) => { return o.QName.Name == classDef.QName.Name && o.QName.Namespace == classDef.QName.Namespace; });
                if (n > 1)
                {
                    throw new SyntaxException( ((Token)classDef.Token) ,$"Duplicate {(classDef.Instance.IsInterface ? "interface" : "class")} definition: {classDef.QName.Name}");
                }
            }

            //检查继承关系
            List<ASClass> cls_list = classes.ToList();

            List<ASClass> sorted_list = new List<ASClass>();

            //已经解析过interface的class
            HashSet<ASClass> inf_resolvered = new HashSet<ASClass>();   

            while (cls_list.Count >0)
            {
                var c = cls_list.ElementAt(0);
                
                Stack<ASClass> resolve_stack = new Stack<ASClass>();

                Resolve(context, c, resolve_stack , inf_resolvered );

                while (resolve_stack.Count > 0)
                { 
                    var cls = resolve_stack.Pop(); 

                    sorted_list.Add(cls);
                    cls_list.Remove(cls);
                }

            }

            context.classDependSort = sorted_list.Distinct().ToList();

            //foreach (var item in context.classDependSort)
            //{
            //    Console.WriteLine(item.QName);
            //}


            //foreach (var item in scriptDefs)
            //{
            //    foreach (var c in item.containers)
            //    {
            //        foreach (var t in c.Traits)
            //        {
            //            Console.WriteLine(t.Type);
            //        }

            //    }

            //}



            return 0;

            //throw new NotImplementedException();
        }

       

        private static void ParseImports(ScriptDef importer,  CompileContext context ,HashSet<ASTrait> package_imports, HashSet<ASTrait> script_imports)
        {
            List<ScriptDef> scriptDefs = context.scriptDefs;

            List<ASScript> scripts = scriptDefs.Select( o=>o.Script ).ToList();

            foreach (var item in context.player_for_compiler.Context.libs)
            {
                scripts.AddRange(item.Scripts);
            }

            foreach (var t in importer.Script.Traits)
            {
                if (
                    (t.Kind == TraitKind.Constant && t.ValueKind == ConstantKind.Namespace)
                    ||
                    t.Kind == TraitKind.Class
                    )
                { 
                    package_imports.Add(t);
                    //script_imports.Add(t);
                }
                

            }


            foreach (var script in scripts)
            {
                var swc = context.player_for_compiler.Context.libs.FirstOrDefault(l => l.Scripts.Contains(script));
                int test = package_imports.Count + script_imports.Count;

                var t = script.QName;
                if (string.IsNullOrEmpty(t.Namespace.Name) && t.Namespace.Kind == NamespaceKind.Package)
                {
                    package_imports.Add(script.Traits[0]);
                    script_imports.Add(script.Traits[0]);

                }
                else if (t.Namespace.Name == importer.Script.QName.Namespace.Name && (t.Namespace.Kind == NamespaceKind.Package || t.Namespace.Kind == NamespaceKind.PackageInternal) )
                {
                    package_imports.Add(script.Traits[0]);
                }
                
                //导入显式 import 进来的
                {
                    foreach (var imp in importer.package_imports)
                    {
                        if (imp.EndsWith("*"))
                        {
                            string imp_ns = imp.Substring(0, imp.Length - 2);
                            if (t.Namespace.Name == imp_ns)
                            {
                                package_imports.Add(script.Traits[0]);
                            }
                        }
                        else
                        {
                            string qn = t.Namespace.Name;
                            if (!string.IsNullOrEmpty(qn))
                            {
                                qn += ".";
                            }
                            qn += t.Name;

                            if (qn == imp)
                            {
                                package_imports.Add(script.Traits[0]);
                            }
                        }
                    }

                    foreach (var imp in importer.script_imports)
                    {
                        if (imp.EndsWith("*"))
                        {
                            string imp_ns = imp.Substring(0, imp.Length - 2);
                            if (t.Namespace.Name == imp_ns)
                            {
                                script_imports.Add(script.Traits[0]);
                            }
                        }
                        else
                        {
                            string qn = t.Namespace.Name;
                            if (!string.IsNullOrEmpty(qn))
                            {
                                qn += ".";
                            }
                            qn += t.Name;

                            if (qn == imp)
                            {
                                script_imports.Add(script.Traits[0]);
                            }
                        }
                    }

                }

                if (package_imports.Count + script_imports.Count > test && swc != null)
                {
                    if (!context.import_trait_at.ContainsKey(script.Traits[0]))
                    {
                        context.import_trait_at.Add(script.Traits[0], swc);
                    }
                }


            }


        }


        private static void Resolve(CompileContext context,ASClass c, Stack<ASClass> resolve_stack, HashSet<ASClass> visited )
        {
            if (context.player_for_compiler.Context.libs.Any(s => s.Classes.Any( cls => cls==c )))
            {
                return;
            }


            if (visited.Contains(c))
            {
                return;
            }
            else
            { 
                visited.Add(c);
            }


            var script_def_at = context.scriptDefs.First(o => o.scriptClasses.Contains(c));

            HashSet<ASTrait> package_imports = null;
            HashSet<ASTrait> script_imports = null;


            if (!context.scriptDef_packageimports.ContainsKey(script_def_at))
            {
                package_imports = new HashSet<ASTrait>();
                script_imports = new HashSet<ASTrait>();

                ParseImports(script_def_at, context, package_imports, script_imports);

                context.scriptDef_packageimports.Add(script_def_at, package_imports);
                context.scriptDef_scriptimports.Add(script_def_at, script_imports);

            }
            else
            {
                package_imports = context.scriptDef_packageimports[script_def_at];
                script_imports = context.scriptDef_scriptimports[script_def_at];
            }



            resolve_stack.Push(c);

            HashSet<ASMultiname> resolvedInfs = new HashSet<ASMultiname>();

            foreach (var interf in c.Instance.Interfaces)
            {
                if (interf.Kind != MultinameKind.TBD)
                {
                    throw new InvalidOperationException();
                }


                var find = CompilerResolveUtil.Find( context, c.QName.Namespace, script_def_at, interf, c.QName.Namespace.Kind == NamespaceKind.Private ? script_imports :  package_imports)
                    .Where( o=>o.Instance.IsInterface )
                    .ToList();
                    ;

                if (find.Count == 0)
                {
                    throw new SyntaxException(((Token)c.Token), $"interface {interf.Name} was not found.");
                }
                else if (find.Count > 1)
                {
                    throw new SyntaxException(((Token)c.Token), $"Ambiguous reference to {interf.Name}.");
                }
                else if (resolve_stack.Contains(find[0]))
                {
                    throw new SyntaxException(((Token)c.Token), $"Circular type reference was detected in {interf.Name}.");
                }
                else
                {
                    //if (context.dict_super_interfaces.ContainsKey(interf))
                    //{
                    //    if (context.dict_super_interfaces[interf] != find[0])
                    //    {
                    //        throw new InvalidOperationException();
                    //    }
                    //}
                    //else
                    //{
                    //    context.dict_super_interfaces.Add(interf, find[0]);
                    //}

                    CompilerResolveUtil.Replace_Ns_Multiname(script_def_at, interf, find[0].QName);
                    if (context.dict_super_interfaces.ContainsKey(find[0].QName ))
                    {
                        if (context.dict_super_interfaces[find[0].QName] != find[0])
                        {
                            throw new InvalidOperationException();
                        }
                    }
                    else
                    {
                        context.dict_super_interfaces.Add(find[0].QName , find[0]);
                    }

                    resolvedInfs.Add(find[0].QName);
                    Resolve( context, find[0], resolve_stack ,visited);
                }

                if (c.Instance.IsInterface) //检查接口继承的情况.接口2继承接口1，接口2中不能有方法同名
                {
                    var t= c.Instance.Traits.Find((m) => { return find[0].Instance.Traits.Any((t) => t.QName.Name == m.QName.Name); } );
                    if (t != null)
                    {
						throw new SyntaxException(((Token)c.Token), $"Cannot override an interface method.  Method {t.QName.Name} conflicts with a method in base interface {find[0].QName.Name}.");
					}
                }
            }

            c.Instance.Interfaces.Clear();
            c.Instance.Interfaces.AddRange(resolvedInfs);


            if (c.Instance.Super != null)
            {
                if (c.Instance.Super.Kind != MultinameKind.TBD)
                {
                    throw new InvalidOperationException();
                }

                var find = CompilerResolveUtil.Find(context, c.QName.Namespace, script_def_at, c.Instance.Super, c.QName.Namespace.Kind == NamespaceKind.Private ? script_imports : package_imports)
                    .Where(o=>!o.Instance.IsInterface)
                    .ToList();

                if (find.Count == 0)
                {
                    var interf = CompilerResolveUtil.Find(context, c.QName.Namespace, script_def_at, c.Instance.Super, c.QName.Namespace.Kind == NamespaceKind.Private ? script_imports : package_imports)
					.Where(o => o.Instance.IsInterface)
					.ToList();

                    if (interf.Count > 0)
                    {
						//A class can only extend another class, not an interface.
						throw new SyntaxException(((Token)c.Token), $"A class can only extend another class, not an interface.\nclass {c.Instance.QName.Name} extends {interf[0].QName.Name}.");
					}
					else
                    {
                        throw new SyntaxException(((Token)c.Token), $"The definition of base class {c.Instance.Super.Name} was not found.");
                    }
                }
                else if (find.Count > 1)
                {
                    throw new SyntaxException(((Token)c.Token), $"Ambiguous reference to {c.Instance.Super.Name}.");
                }
                else if (resolve_stack.Contains(find[0]))
                {
                    throw new SyntaxException(((Token)c.Token), $"Circular type reference was detected in {c.Instance.Super.Name}.");
                }
                else if (find[0].QName.Namespace.Kind == NamespaceKind.Private && c.QName.Namespace.Kind != NamespaceKind.Private)
                {  
                   throw new SyntaxException(((Token)c.Token), $"Forward reference to base class {find[0].QName.Name}."); 
                }
                else if (find[0].Instance.Flags.HasFlag(ClassFlags.Final))
                {
                    throw new SyntaxException(((Token)c.Token), "Base class is final.");
                }
                else
                {
                    
                        
                    CompilerResolveUtil.Replace_Ns_Multiname(script_def_at, c.Instance.Super, find[0].QName);
                    c.Instance.Super = find[0].QName;

                    if (context.dict_super_interfaces.ContainsKey(c.Instance.Super))
                    {
                        if (context.dict_super_interfaces[c.Instance.Super] != find[0])
                        {
                            throw new InvalidOperationException();
                        }

                    }
                    else
                    {
                        context.dict_super_interfaces.Add(c.Instance.Super, find[0]);
                    }

                    Resolve(context, find[0], resolve_stack, visited);
                }
            }

        }



    }
}
