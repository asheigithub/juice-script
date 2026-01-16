using juicescript.ABC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler
{
    internal class CompilerResolveUtil
    {

        internal static List<ASClass> Find(CompileContext context, ASNamespace finder_namespace, ScriptDef finder, ASMultiname qname, IEnumerable<ASTrait> imports)
        {
            HashSet<ASClass> classes = new HashSet<ASClass>();

           
            foreach (var item in finder.Script.Traits)
            {
                if (item.Kind == TraitKind.Class && (item != finder.Script.Traits[0] || item.QName.Namespace.Name == "" ) )
                {
                    if (item.QName.Name == qname.Name)
                    {
                        classes.Add(item.Class);
                    }
                    else if (qname.Name.IndexOf(".") > -1)
                    {
                        string fullname = item.QName.Namespace.Name + "." + item.QName.Name;
                        if (fullname == qname.Name)
                        {
                            classes.Add(item.Class);
                        }
                    }

                }
            }

            foreach (var item in imports)
            {
                if (item.Kind == TraitKind.Class && item.QName.Namespace.Kind != NamespaceKind.Private)
                {
                    bool pass = false;
                    if (item.QName.Name == qname.Name)
                    {
                        pass = true;
                    }
                    else if (qname.Name.IndexOf(".") > -1)
                    {
                        string fullname = item.QName.Namespace.Name + "." + item.QName.Name;
                        if (fullname == qname.Name)
                        {
                            pass = true;
                        }
                    }

                    if (pass)
                    {
                        if (finder_namespace.Name == item.QName.Namespace.Name
                            ||
                            item.QName.Namespace.Kind == NamespaceKind.Package
                            )
                        {
                            classes.Add(item.Class);

                            if (context.import_trait_at.ContainsKey(item))
                            {
                                context.referenceAssembly.Add(context.import_trait_at[item].assemblyName);
                            }

                        }

                    }
                }
            }

            
            return classes.ToList();
        }


        internal static List<ASNamespace> FindNameSpace(CompileContext context, ASContainer container, ScriptDef findinscript, ASNamespace qname, IEnumerable<ASTrait> imports)
        {
            HashSet<ASNamespace> namespaces = new HashSet<ASNamespace>();

            var c = container;

            while (c != null)
            {
                foreach (var item in c.Traits)
                {
                    if (item.Kind == TraitKind.Constant && item.ValueKind == ConstantKind.Namespace)
                    {
                        if (item.QName.Name == qname.Name)
                        {
                            namespaces.Add(item.Value.Namespace);
                            return namespaces.ToList();
                        }
                    }
                }

                if (c is ASMethodBody)
                {
                    c = ((ASMethodBody)c).Method.Container;

                    container = c;
                }
                else
                {
                    c = null;
                }

            }

            if (container is ASInstance) //额外查找ASClass中定义的命名空间
            {
                var cls = findinscript.scriptClasses.First(c => c != null && c.Instance == container);
                foreach (var item in cls.Traits)
                {
                    if (item.Kind == TraitKind.Constant && item.QName.Namespace.Kind != NamespaceKind.Private && item.ValueKind == ConstantKind.Namespace)
                    {
                        if (item.QName.Name == qname.Name)
                        {
                            namespaces.Add(item.Value.Namespace);
                            return namespaces.ToList();
                        }
                    }
                }

            }

            if (container != findinscript.Script)
            {
                foreach (var item in findinscript.Script.Traits)
                {
                    if (item.Kind == TraitKind.Constant && item.ValueKind == ConstantKind.Namespace)
                    {
                        if (item.QName.Name == qname.Name)
                        {
                            namespaces.Add(item.Value.Namespace);
                            return namespaces.ToList();
                        }

                    }
                }
            }




            foreach (var item in imports)
            {
                if (item.Kind == TraitKind.Constant && item.QName.Namespace.Kind != NamespaceKind.Private && item.ValueKind == ConstantKind.Namespace)
                {
                   
                    if (item.QName.Name == qname.Name && item.QName.Namespace.Kind == NamespaceKind.Package)
                    {
                        namespaces.Add(item.Value.Namespace);

                        if (context.import_trait_at.ContainsKey(item))
                        {
                            context.referenceAssembly.Add(context.import_trait_at[item].assemblyName);
                        }

                    }
                    else if (qname.Name.IndexOf(".") > -1)
                    {
                        string fullname = item.QName.Namespace.Name + "." + item.QName.Name;
                        if (fullname == qname.Name)
                        {
                            namespaces.Add(item.Value.Namespace);

                            if (context.import_trait_at.ContainsKey(item))
                            {
                                context.referenceAssembly.Add(context.import_trait_at[item].assemblyName);
                            }

                        }
                    }
                }
            }

            

            return namespaces.ToList();
        }





        internal static void Replace_Ns_Multiname(ScriptDef script, ASMultiname vtype, ASMultiname out_name)
        {
            if (out_name.Namespace.Kind == NamespaceKind.TBD)
            {
                throw new InvalidOperationException();
            }

            script.namespaces.Remove(vtype.Namespace);
            script.multinames.Remove(vtype);

            if (!script.namespaces.Contains(out_name.Namespace))
            {
                script.namespaces.Add(out_name.Namespace);
            }
            if (!script.multinames.Contains(out_name))
            {
                script.multinames.Add(out_name);
            }
        }
    }
}
