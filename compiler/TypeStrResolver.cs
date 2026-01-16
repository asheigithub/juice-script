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
    public class TypeStrResolver
    {
        internal static int BuildStrType(CompileContext context)
        {
            foreach (var script in context.scriptDefs)
            {
                HashSet<ASTrait> package_imports = null;
                HashSet<ASTrait> script_imports = null;

                package_imports = context.scriptDef_packageimports[script];
                script_imports = context.scriptDef_scriptimports[script];
                


                foreach (var c in script.containers)
                {
                    foreach (var trait in c.Traits)
                    {
                        if (trait.Kind == ABC.TraitKind.Constant || trait.Kind == ABC.TraitKind.Slot)
                        {

                            if (trait.Type == null)
                            {
                                trait.TypeKind = TypeKind.Any;
                                //context.dict_traittype.Add(trait, ABC.TypeKind.Undefined);
                            }
                            else if (trait.Type.Kind == ABC.MultinameKind.TypeName)
                            {

                                List<TypeKind> vector_type = new List<TypeKind>();

                                var vtype = trait.Type.Types[0];

                                var vector = trait.Type;

                                while (vtype.Kind == MultinameKind.TypeName)
                                {
                                    vector_type.Add(TypeKind.Vector);
                                    vector = vtype;
                                    vtype = vtype.Types[0];
                                }

                                ASMultiname out_name;

                                vector_type.Add(ResolveTypeKind(context, vtype, script, c, (Token)trait.Token, out out_name));
                                //context.dict_traittype.Add(trait,  VectorDef.CreateOrGet(context,vector_type).ElementTypeId );
                                trait.TypeKind = VectorDef.CreateOrGet(context, vector_type).Identifier;

                                CompilerResolveUtil.Replace_Ns_Multiname(script, vtype, out_name);

                                vector.Types[0] = out_name;



                            }
                            else
                            {
                                ASMultiname out_name;
                                var typekind = ResolveTypeKind(context, trait.Type, script, c, (Token)trait.Token, out out_name);
                                //context.dict_traittype.Add(trait, typekind);
                                trait.TypeKind = typekind;

                                CompilerResolveUtil.Replace_Ns_Multiname(script, trait.Type, out_name);

                                trait.Type = out_name;
                            }
                            
                        }

                        if (trait.Kind == TraitKind.Constant && trait.ValueKind == ConstantKind.Namespace)
                        {
                            ASNamespace n = trait.Value.Namespace;
                            if (n.def_uri.StartsWith("1:"))
                            {
                                List<ASNamespace> search = new List<ASNamespace>();

                                search.Add(n);

                                string findns = n.def_uri.Substring(2);

                                var find = CompilerResolveUtil.FindNameSpace(context, c, script, new ASNamespace() { Name = findns },
                                     c.QName.Namespace.Kind == NamespaceKind.Private ? script_imports : package_imports
                                );

                            lblresearch:

                                if (find.Count == 1)
                                {

                                    if (find[0].def_uri.StartsWith("1:"))
                                    {
                                        if (search.Contains(find[0]))
                                        {
                                            throw new SyntaxException(trait.Token, "A namespace initializer must be either a literal string or another namespace.");
                                        }
                                        else
                                        {
                                            search.Add(find[0]);
                                        }

                                        findns = find[0].def_uri.Substring(2);

                                        find = CompilerResolveUtil.FindNameSpace(context, c, script, new ASNamespace() { Name = findns },
                                            c.QName.Namespace.Kind == NamespaceKind.Private ? script_imports : package_imports
                                        );

                                        goto lblresearch;
                                    }
                                    else
                                    {
                                        n.def_uri = find[0].def_uri;
                                    }

                                }
                                else
                                {
                                    throw new SyntaxException(trait.Token, "A namespace initializer must be either a literal string or another namespace.");
                                }



                            }
                            else if (n.def_uri.StartsWith("0:"))
                            {
                                n.def_uri = n.def_uri.Substring(2);
                            }
                        }

                    }
                }


                for (int i = 1; i < script.scriptMethods.Count; i++)
                {
                    var method = script.scriptMethods[i];
                    //var as3function = script.AS3SrcFile._functions[ script.ast_functions[i]];
                    if (method.Container != null)
                    {
                        if (method.ReturnType.Kind == MultinameKind.TypeName)
                        {
                            List<TypeKind> vector_type = new List<TypeKind>();

                            var vtype = method.ReturnType.Types[0];
                            var vector = method.ReturnType;
                            while (vtype.Kind == MultinameKind.TypeName)
                            {
                                vector_type.Add(TypeKind.Vector);
                                vector = vtype;
                                vtype = vtype.Types[0];
                            }
                            ASMultiname out_name;
                            vector_type.Add( ResolveTypeKind(context, vtype, script, method.Container, (Token)method.Token, out out_name));
                            var vd = VectorDef.CreateOrGet(context, vector_type);

                            method.ReturnTypeKind = vd.Identifier;
                            CompilerResolveUtil.Replace_Ns_Multiname(script, vtype, out_name);
                            vector.Types[0] = out_name;

                        }
                        else
                        {
                            ASMultiname out_name;
                            var returntype = ResolveTypeKind(context, method.ReturnType, script, method.Container, (Token)method.Token, out out_name);
                            method.ReturnTypeKind = returntype;
                            CompilerResolveUtil.Replace_Ns_Multiname(script, method.ReturnType, out_name);
                            method.ReturnType = out_name;

                        }

                        for (int j = 0;j< method.Parameters.Count; j++)
                        { 
                            var p = method.Parameters[j];

                            if (p.Type.Kind == MultinameKind.TypeName)
                            {
                                List<TypeKind> vector_type = new List<TypeKind>();

                                var vtype = p.Type.Types[0];
                                var vector = p.Type;
                                while (vtype.Kind == MultinameKind.TypeName)
                                {
                                    vector_type.Add(TypeKind.Vector);
                                    vector = vtype;
                                    vtype = vtype.Types[0];

                                }
                                ASMultiname out_name;
                                vector_type.Add(ResolveTypeKind(context, vtype, script, method.Container, (Token)method.Token, out out_name));
                                var vd = VectorDef.CreateOrGet(context, vector_type);

                                //context.dict_parametartype.Add(p, vd.Identifier);
                                p.TypeKind = vd.Identifier;
                                CompilerResolveUtil.Replace_Ns_Multiname(script, vtype, out_name);
                                vector.Types[0] = out_name;
                            }
                            else
                            {
                                ASMultiname out_name;
                                var returntype = ResolveTypeKind(context, p.Type, script, method.Container, (Token)method.Token, out out_name);
                                //context.dict_parametartype.Add(p, returntype);
                                p.TypeKind = returntype;

                                CompilerResolveUtil.Replace_Ns_Multiname(script, p.Type, out_name);
                                p.Type = out_name;
                            }
                            
                        }

                    }
                    else
                    {
                        ASMultiname out_name;
                        var returntype = ResolveTypeKind(context, method.ReturnType, script, method.Container, (Token)method.Token, out out_name);

                        //必定就是script_initializer
                        method.ReturnTypeKind = returntype;
                        CompilerResolveUtil.Replace_Ns_Multiname(script, method.ReturnType, out_name);
                        method.ReturnType = out_name;



                    }
                }

                //到这里所有待定multinames应该已经全部消除了
                if (script.multinames.Any((o) =>o !=null && o.Kind == MultinameKind.TBD))
                {
                    throw new InvalidOperationException();
                }

                if (script.namespaces.Any((o) => o != null && o.Kind == NamespaceKind.TBD))
                {
                    throw new InvalidOperationException();
                }

                
            }

            foreach (var script in context.scriptDefs)
            {
                //if (script.namespaces.Any((o) => o != null && o.def_uri.StartsWith("1:")))
                //{
                //    throw new InvalidOperationException();
                //}
                //检查container中的重命名空间traits;
                foreach (var container in script.containers)
                {
                    foreach (var t in container.Traits)
                    {
                        if (container.Traits.Any((t2) => t2.QName.Name == t.QName.Name && t2 != t &&

                                   t.QName.Namespace.def_uri !=null  &&  t2.QName.Namespace.def_uri == t.QName.Namespace.def_uri 
                                  
                                   &&
                                   !((t.Kind == TraitKind.Getter && t2.Kind == TraitKind.Setter) || (t.Kind == TraitKind.Setter && t2.Kind == TraitKind.Getter))

                                   )

                                   )
                        {
                            throw new SyntaxException(t.Token, $"A conflict exists with definition {t.QName.Name} in namespace .");
                        }
                    }

                }

            }

            
            

            return 0;
        }

       

        internal static TypeKind ResolveTypeKind(CompileContext ctx, ASMultiname type, ScriptDef script, ASContainer c, Token token,out ASMultiname out_typename)
        {
            
            string typeStr = type.Name.ToString();
            if (typeStr == "*")
            {
                out_typename = new ASMultiname()
                {
                    Kind = MultinameKind.QName,
                    Name = "*",
                    Namespace = new ASNamespace()
                    {
                        Kind = NamespaceKind.Package,
                        Name = ""
                    }
                };


                return TypeKind.Any;
            }
            else if (typeStr == "Boolean")
            {
                out_typename = new ASMultiname()
                {
                    Kind = MultinameKind.QName,
                    Name = "Boolean",
                    Namespace = new ASNamespace()
                    {
                        Kind = NamespaceKind.Package,
                        Name = ""
                    }
                };
                return TypeKind.Boolean;
            }
            else if (typeStr == "sbyte")
            {
                out_typename = new ASMultiname()
                {
                    Kind = MultinameKind.QName,
                    Name = "sbyte",
                    Namespace = new ASNamespace()
                    {
                        Kind = NamespaceKind.Package,
                        Name = ""
                    }
                };
                return TypeKind.SByte;
            }
            else if (typeStr == "byte")
            {
                out_typename = new ASMultiname()
                {
                    Kind = MultinameKind.QName,
                    Name = "byte",
                    Namespace = new ASNamespace()
                    {
                        Kind = NamespaceKind.Package,
                        Name = ""
                    }
                };
                return TypeKind.Byte;
            }
            else if (typeStr == "short")
            {
                out_typename = new ASMultiname()
                {
                    Kind = MultinameKind.QName,
                    Name = "short",
                    Namespace = new ASNamespace()
                    {
                        Kind = NamespaceKind.Package,
                        Name = ""
                    }
                };
                return TypeKind.Short;
            }
            else if (typeStr == "ushort")
            {
                out_typename = new ASMultiname()
                {
                    Kind = MultinameKind.QName,
                    Name = "ushort",
                    Namespace = new ASNamespace()
                    {
                        Kind = NamespaceKind.Package,
                        Name = ""
                    }
                };
                return TypeKind.UShort;
            }
            else if (typeStr == "int")
            {
                out_typename = new ASMultiname()
                {
                    Kind = MultinameKind.QName,
                    Name = "int",
                    Namespace = new ASNamespace()
                    {
                        Kind = NamespaceKind.Package,
                        Name = ""
                    }
                };
                return TypeKind.Int;
            }
            else if (typeStr == "uint")
            {
                out_typename = new ASMultiname()
                {
                    Kind = MultinameKind.QName,
                    Name = "uint",
                    Namespace = new ASNamespace()
                    {
                        Kind = NamespaceKind.Package,
                        Name = ""
                    }
                };
                return TypeKind.Uint;
            }
            else if (typeStr == "float")
            {
                out_typename = new ASMultiname()
                {
                    Kind = MultinameKind.QName,
                    Name = "float",
                    Namespace = new ASNamespace()
                    {
                        Kind = NamespaceKind.Package,
                        Name = ""
                    }
                };
                return TypeKind.Float;
            }
            else if (typeStr == "Number")
            {
                out_typename = new ASMultiname()
                {
                    Kind = MultinameKind.QName,
                    Name = "Number",
                    Namespace = new ASNamespace()
                    {
                        Kind = NamespaceKind.Package,
                        Name = ""
                    }
                };
                return TypeKind.Number;
            }
            else if (typeStr == "String")
            {
                out_typename = new ASMultiname()
                {
                    Kind = MultinameKind.QName,
                    Name = "String",
                    Namespace = new ASNamespace()
                    {
                        Kind = NamespaceKind.Package,
                        Name = ""
                    }
                };
                return TypeKind.String;
            }
            else if (typeStr == "Function")
            {
                out_typename = new ASMultiname()
                {
                    Kind = MultinameKind.QName,
                    Name = "Function",
                    Namespace = new ASNamespace()
                    {
                        Kind = NamespaceKind.Package,
                        Name = ""
                    }
                };
                return TypeKind.Function;
            }
            else if (typeStr == "Array")
            {
                out_typename = new ASMultiname()
                {
                    Kind = MultinameKind.QName,
                    Name = "Array",
                    Namespace = new ASNamespace()
                    {
                        Kind = NamespaceKind.Package,
                        Name = ""
                    }
                };
                return TypeKind.Array;
            }
            else if (typeStr == "Namespace")
            {
                out_typename = new ASMultiname()
                {
                    Kind = MultinameKind.QName,
                    Name = "Namespace",
                    Namespace = new ASNamespace()
                    {
                        Kind = NamespaceKind.Package,
                        Name = ""
                    }
                };
                return TypeKind.Namespace;
            }
            else if (typeStr == "void")
            {
                out_typename = new ASMultiname()
                {
                    Kind = MultinameKind.QName,
                    Name = "void",
                    Namespace = new ASNamespace()
                    {
                        Kind = NamespaceKind.Package,
                        Name = ""
                    }
                };
                return TypeKind.Fun_Void;
            }
            else
            {

               
                var container = c;
                bool inpackagemethod=false;
                while (container is ASMethodBody)
                {
                    if ((((ASMethodBody)container).Method.Flags & MethodFlags.PackageMemberScope) == MethodFlags.PackageMemberScope)
                    {
                        inpackagemethod = true;
                    }

                    container = ((ASMethodBody)container).Method.Container;
                }

                List<ASClass> find;
                if (container is ASScript)
                {
                    find = CompilerResolveUtil.Find(ctx, container.QName.Namespace, script, type,
                        inpackagemethod?
                        ctx.scriptDef_packageimports[script]:
                        ctx.scriptDef_scriptimports[script]);
                }
                else if (container is ASClass || container is ASInstance)
                {
                    find = CompilerResolveUtil.Find(ctx, container.QName.Namespace, script, type,
                        container.QName.Namespace.Kind == NamespaceKind.Private ?
                            ctx.scriptDef_scriptimports[script] :
                            ctx.scriptDef_packageimports[script]);
                }
                else
                {
                    throw new InvalidOperationException();
                }

                if (find.Count == 0)
                {
                    throw new ResolverException(token, $"Type was not found or was not a compile-time constant: {typeStr}.");
                }

                if (find.Count > 1)
                {
                    throw new ResolverException((Token)token, $"Ambiguous reference to {find[0].QName.Name}.");
                }

                out_typename = find[0].QName;

                return (TypeKind)find[0].Type_identifier;

            }
        }
    }
}
