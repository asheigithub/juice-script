using juicescript.ABC;
using juicescript.compiler.AST;
using juicescript.compiler.AST.Expr;
using juicescript.compiler.AST.Stmt;
using juicescript.compiler.parse;
using MyMD5;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace juicescript.compiler
{
    /// <summary>
    /// 初步编译.as源文件中的类型定义
    /// </summary>
    public class ScriptDefBuilder
    {

        private List<ASMultiname> _multinames;
        private List<ASNamespace> _namespaces;
        private List<ASNamespaceSet> _namespaceSets;
        private List<ASClass> _classes;
        private List<ASContainer> _containers;


        public static ulong GetClassId(string qname)
        {

            byte[] hash = System.IO.Hashing.XxHash64.Hash(System.Text.Encoding.UTF8.GetBytes(qname));
			return BitConverter.ToUInt64(hash, 0);
            //return SipHash.SipHash_2_4( System.Text.Encoding.UTF8.GetBytes(qname),0,0);

        }


        public ScriptDef Build(Parser parser, string srcFile, string projectDir,out AS3SrcFile as3src)
        {
            //Dictionary<ASClass,Token> class_token = new Dictionary<ASClass,Token>();


            List<Tuple<ASMethod, AS3Function>> scriptMethods = new List<Tuple<ASMethod, AS3Function>>();

            ASMultiname.NewMultiName += ASMultiname_NewMultiName;
            _multinames = new List<ASMultiname>();
            ASNamespace.NewNameSpace += ASNamespace_NewNameSpace;
            _namespaces = new List<ASNamespace>();
            ASClass.NewClass += ASClass_NewClass;
            _classes = new List<ASClass>();
            ASNamespaceSet.NewNamespaceSet += ASNamespaceSet_NewNamespaceSet;
            _namespaceSets = new List<ASNamespaceSet>();
            ASContainer.NewContainer += ASContainer_NewContainer;
            _containers = new List<ASContainer>();


            try
            {
                var fullpath = System.IO.Path.GetFullPath(srcFile);
                if (System.IO.File.Exists(fullpath))
                {
                    string input = System.IO.File.ReadAllText(fullpath);

                    if (srcFile.StartsWith(projectDir))
                    {
                        srcFile = srcFile.Substring(projectDir.Length).Replace("\\", "/");
                    }

                    parser.ErrorOut = new ParseErrOut();

                    ParseTree tree = parser.ParseTree(input, AS3LexKeywords.LEXKEYWORDS, AS3LexKeywords.LEXSKIPBLANKWORDS, srcFile,fullpath);
                    if (parser.hasError)
                    {
                        throw new ParseException( ((ParseErrOut) parser.ErrorOut).BuffData);
                    }

                    var ast = new AS3AbstractSyntaxTree();
                    var as3 = ast.Analyse(tree);

                    as3src = as3;

                    if (ast.SyntaxError != null)
                    {
                        throw ast.SyntaxError;
                    }

                    //StringBuilder sb = new StringBuilder();
                    //as3.Write(sb);

                    //Console.Write(sb.ToString());





                    ASScript script = new ASScript();
                    if (as3.Package.MainClass != null)
                    {
                        #region MainClass

                        CheckClassAccess(as3.Package.MainClass);

                        ASInstance instance = new ASInstance(
                            new ASMultiname()
                            {
                                Kind = MultinameKind.QName,
                                Name = as3.Package.MainClass.Name,
                                Namespace = new ASNamespace()
                                {
                                    Kind = as3.Package.MainClass.Access.IsPublic ? NamespaceKind.Package : NamespaceKind.PackageInternal,
                                    Name = as3.Package.Name
                                }

                            }
                            );

                        MakeInstanceFlagsAndSuperAndInterface(instance, as3.Package.MainClass);

                        MakeClassInstanceMembers(instance, as3.Package.MainClass, as3.Package.Name, NamespaceKind.PackageInternal, scriptMethods,as3);

                        ulong class_id = 0;
                        if (instance.QName.Name == "Object" && instance.QName.Namespace.Name == "")
                        {
                            class_id = (ulong)TypeKind.Object;
                        }
                        else if (instance.QName.Name == "Class" && instance.QName.Namespace.Name == "")
                        {
                            class_id = (ulong)TypeKind.Class;
                        }
                        else if (instance.QName.Name == "byte" && instance.QName.Namespace.Name == "")
                        {
                            class_id = (ulong)TypeKind.Byte;
                        }
                        else if (instance.QName.Name == "sbyte" && instance.QName.Namespace.Name == "")
                        {
                            class_id = (ulong)TypeKind.SByte;
                        }
                        else if (instance.QName.Name == "short" && instance.QName.Namespace.Name == "")
                        {
                            class_id = (ulong)TypeKind.Short;
                        }
                        else if (instance.QName.Name == "ushort" && instance.QName.Namespace.Name == "")
                        {
                            class_id = (ulong)TypeKind.UShort;
                        }

                        else if (instance.QName.Name == "int" && instance.QName.Namespace.Name == "")
                        {
                            class_id = (ulong)TypeKind.Int;
                        }
                        else if (instance.QName.Name == "uint" && instance.QName.Namespace.Name == "")
                        {
                            class_id = (ulong)TypeKind.Uint;
                        }
                        else if (instance.QName.Name == "Number" && instance.QName.Namespace.Name == "")
                        {
                            class_id = (ulong)TypeKind.Number;
                        }
                        else if (instance.QName.Name == "Array" && instance.QName.Namespace.Name == "")
                        {
                            class_id = (ulong)TypeKind.Array;
                        }
                        else if (instance.QName.Name == "String" && instance.QName.Namespace.Name == "")
                        {
                            class_id = (ulong)TypeKind.String;
                        }
                        else if (instance.QName.Name == "Vector" && instance.QName.Namespace.Name == "__AS3__.vec")
                        {
                            class_id = (ulong)TypeKind.Vector;
                        }
                        else if (instance.QName.Name == "Boolean" && instance.QName.Namespace.Name == "")
                        {
                            class_id = (ulong)TypeKind.Boolean;
                        }
                        else if (instance.QName.Name == "Function" && instance.QName.Namespace.Name == "")
                        {
                            class_id = (ulong)TypeKind.Function;
                        }
                        else if (instance.QName.Name == "Namespace" && instance.QName.Namespace.Name == "")
                        {
                            class_id = (ulong)TypeKind.Namespace;
                        }
                        else if (instance.QName.Name == "float" && instance.QName.Namespace.Name == "")
                        {
							class_id = (ulong)TypeKind.Float;
						}
                        else
                        {
                            class_id = GetClassId(instance.QName.ToString()); //CityHash.CityHash.CityHash64(instance.QName.ToString());  //new MyMD5.MyMD5().Hash(instance.QName.ToString()).ToIdentifier();
                        }
                        

                        ASClass @class = new ASClass(as3.Package.MainClass.Token, 
                            class_id
                            ); //class_token.Add(@class, as3.Package.MainClass.Token);
                        @class.Instance = instance;

                        for (int i = 0; i < as3.Package.MainClass.Members.Count; i++)
                        {
                            var member = as3.Package.MainClass.Members[i];


                            if (member is AS3NameSpace)
                            {
                                if (@class.Traits.Any(
                                    (t) => { return t.ValueKind == ConstantKind.Namespace && t.Kind == TraitKind.Constant && t.QName.Name == member.Name; }
                                    ))
                                {
                                    throw new SyntaxException(member.Token, "Duplicate namespace definition.");
                                }

                                if (member.Access.IsStatic)
                                {
                                    throw new SyntaxException(member.Token, "The static attribute is not allowed on namespace definitions.");
                                }
                                ASTrait trait = MakeTraitFromAS3Member(member, as3.Package.Name, as3.Package.Name + ":" + @class.QName.Name, true, NamespaceKind.PackageInternal);
                                trait.IsStatic = true;

                                AS3NameSpace ns = (AS3NameSpace)member;
                                ASNamespace @namespace = new ASNamespace();
                                @namespace.Name = @class.QName.Namespace.Name + ":" + @class.QName.Name + "/" + @class.QName.Namespace.Name + ":" + @class.QName.Name + ":" + ns.Name;
                                @namespace.Kind = NamespaceKind.PackageInternal;
                                @namespace.def_uri = ns.URI;

                                trait.Kind = TraitKind.Constant;

                                trait.Value =new ASTrait.TraitValue( @namespace);
                                trait.ValueKind = ConstantKind.Namespace;

                                BuildMeta(trait, member.Metas);

                                @class.Traits.Add(trait);

                            }
                            else if (member.Access.IsStatic)
                            {
                                ASTrait trait = MakeTraitFromAS3Member(member, as3.Package.Name, as3.Package.Name + ":" + @class.QName.Name, true, NamespaceKind.PackageInternal);
                                trait.IsStatic = true;
                                BuildClassMember(member, trait, @class, scriptMethods,as3);
                                @class.Traits.Add(trait);
                            }
                        }

                        for (int i = 0; i < as3.Package.MainClass.CAnonymousFunction.Count; i++)
                        {
                            var function = as3.Package.MainClass.CAnonymousFunction[i];

                            ASMethod f = new ASMethod(script,function.Token);
                            BuildMethod(f, function,as3);
                            f.SetContainer(@class);

                            scriptMethods.Add(new Tuple<ASMethod, AS3Function>(f, function));

                            ASTrait trait = new ASTrait(function.Token);
                            trait.Kind = TraitKind.Function;
                            trait.Function = f;
                            trait.QName = new ASMultiname()
                            {
                                Kind = MultinameKind.QName,
                                Name = "@#"+function.ClosureId.ToString(),
                                Namespace = @class.QName.Namespace
                            };
                            trait.Value = new ASTrait.TraitValue(ASTrait.TraitValueType.AS3Function, as3._functions.IndexOf(function));

                            @class.Traits.Add(@trait);

                        }


                        var clsTrait =
                            new ASTrait(@class.Token)
                            {
                                Attributes = TraitAttributes.None,
                                Class = @class,
                                Kind = TraitKind.Class,
                                QName = @class.QName
                            }
                            ;

                        //收集meta
                        BuildMeta(clsTrait, as3.Package.MainClass.Metas);

                        script.Traits.Add(clsTrait);
                        #endregion
                    }
                    else if (as3.Package.MainInterface != null)
                    {
                        #region MainInterface

                        CheckInterfaceAccess(as3.Package.MainInterface);


                        ASInstance instance = new ASInstance(
                            new ASMultiname()
                            {
                                Kind = MultinameKind.QName,
                                Name = as3.Package.MainInterface.Name,
                                Namespace = new ASNamespace()
                                {
                                    Kind = as3.Package.MainInterface.Access.IsPublic ? NamespaceKind.Package :
                                        (as3.Package.MainInterface.Access.IsPrivate ? NamespaceKind.Private :
                                        (as3.Package.MainInterface.Access.IsProtected ? NamespaceKind.Protected : NamespaceKind.PackageInternal))


                                    ,
                                    Name = as3.Package.Name
                                }

                            }
                            );
                        instance.Flags = ClassFlags.Interface;



                        for (int i = 0; i < as3.Package.MainInterface.ExtendsNames.Count; i++)
                        {
                            string extends = as3.Package.MainInterface.ExtendsNames[i];

                            instance.Interfaces.Add(
                                new ASMultiname()
                                {
                                    Kind = MultinameKind.TBD,
                                    Name = extends,
                                    Namespace = new ASNamespace()
                                    {
                                        Kind = NamespaceKind.TBD,
                                        Name = ""
                                    }
                                }
                                );
                        }

                        MakeInfMembers(instance, as3.Package.MainInterface, as3.Package.Name + ":" + instance.QName.Name, scriptMethods,as3);

                        ASClass @class = new ASClass(as3.Package.MainInterface.Token,
                            GetClassId(instance.QName.ToString())
                             //CityHash.CityHash.CityHash64(instance.QName.ToString())
                            ); //class_token.Add(@class, as3.Package.MainInterface.Token);
                        @class.Instance = instance;

                        var interfaceTrait =
                            new ASTrait(@class.Token)
                            {
                                Attributes = TraitAttributes.None,
                                Class = @class,
                                Kind = TraitKind.Class,
                                QName = @class.QName
                            }
                            ;

                        //收集meta
                        BuildMeta(interfaceTrait, as3.Package.MainInterface.Metas);

                        script.Traits.Add(interfaceTrait);
                        #endregion
                    }
                    else if (as3.Package.MainNamespace != null)
                    {
                        #region namespace
                        AS3NameSpace ns = as3.Package.MainNamespace;
                        if (ns.Access.IsStatic)
                        {
                            //The static attribute is not allowed on namespace definitions.
                            throw new SyntaxException(ns.Token, "The static attribute is not allowed on namespace definitions.");
                        }

                        ASNamespace @namespace = new ASNamespace();
                        @namespace.Name = as3.Package.Name + ":" + ns.Name;
                        @namespace.Kind = NamespaceKind.PackageInternal;
                        @namespace.def_uri = ns.URI;

                        var nsTrait =
                            new ASTrait(ns.Token)
                            {
                                Attributes = TraitAttributes.None,
                                Kind = TraitKind.Constant,

                                QName = new ASMultiname()
                                {
                                    Kind = MultinameKind.QName,
                                    Name = ns.Name,
                                    Namespace = new ASNamespace()
                                    {
                                        Kind = ns.Access.IsPublic ? NamespaceKind.Package :
                                        (ns.Access.IsPrivate ? NamespaceKind.Private :
                                        (ns.Access.IsProtected ? NamespaceKind.Protected : NamespaceKind.PackageInternal))
                                    ,
                                        Name = as3.Package.Name
                                    }
                                },

                                Value = new ASTrait.TraitValue( @namespace),
                                ValueKind = ConstantKind.Namespace
                            }
                            ;

                        BuildMeta(nsTrait, ns.Metas);

                        script.Traits.Add(nsTrait);
                        #endregion
                    }

                    string outscopeNS = "FilePrivateNS:";
                    if (as3.Package.MainClass != null)
                    {
                        outscopeNS += as3.Package.MainClass.Name;
                    }
                    else if (as3.Package.MainInterface != null)
                    {
                        outscopeNS += as3.Package.MainInterface.Name;
                    }
                    else if (as3.Package.MainNamespace != null)
                    {
                        outscopeNS += as3.Package.MainNamespace.Name;
                    }
                    else
                    {
                        outscopeNS += System.IO.Path.GetFileNameWithoutExtension(as3.sourceFile);
                    }


                    for (int i = 0; i < as3.OutPackage.outpackage_classes_interfaces.Count; i++)
                    {
                        var ci = as3.OutPackage.outpackage_classes_interfaces[i];
                        if (ci is AS3Class)
                        {
                            var cls = ci as AS3Class;

                            CheckClassAccess(cls);
                            if (cls.Access.IsPublic)
                            {
                                throw new SyntaxException(cls.Token, "The public attribute can only be used inside a package.");
                            }
                            ASInstance instance = new ASInstance(
                               new ASMultiname()
                               {
                                   Kind = MultinameKind.QName,
                                   Name = cls.Name,
                                   Namespace = new ASNamespace()
                                   {
                                       Kind = NamespaceKind.Private,
                                       Name = outscopeNS,
                                       in_package = as3.Package.Name
                                   }

                               }
                               );
                            MakeInstanceFlagsAndSuperAndInterface(instance, cls);
                            MakeClassInstanceMembers(instance, cls, outscopeNS, NamespaceKind.Private, scriptMethods, as3);

                            ASClass @class = new ASClass(as3.OutPackage.outpackage_classes_interfaces[i].Token,
                                 GetClassId(instance.QName.ToString())
                                ); //class_token.Add(@class, as3.OutPackage.outpackage_classes_interfaces[i].Token);
                            @class.Instance = instance;

                            for (int j = 0; j < cls.Members.Count; j++)
                            {
                                var member = cls.Members[j];

                                if (member is AS3NameSpace)
                                {
                                    if (@class.Traits.Any(
                                    (t) => { return t.ValueKind == ConstantKind.Namespace && t.Kind == TraitKind.Constant && t.QName.Name == member.Name; }
                                    ))
                                    {
                                        throw new SyntaxException(member.Token, "Duplicate namespace definition.");
                                    }

                                    if (member.Access.IsStatic)
                                    {
                                        throw new SyntaxException(member.Token, "The static attribute is not allowed on namespace definitions.");
                                    }
                                    ASTrait trait = MakeTraitFromAS3Member(member, outscopeNS, outscopeNS + ":" + cls.Name, true, NamespaceKind.Private);
                                    trait.IsStatic = true;

                                    AS3NameSpace ns = (AS3NameSpace)member;
                                    ASNamespace @namespace = new ASNamespace();

                                    string priv_ns = "private$" + as3.key;

                                    @namespace.Name = priv_ns + ":" + @class.QName.Name + "/" + priv_ns + ":" + ns.Name;
                                    @namespace.Kind = NamespaceKind.PackageInternal;
                                    @namespace.def_uri = ns.URI;

                                    trait.Kind = TraitKind.Constant;

                                    trait.Value = new ASTrait.TraitValue(@namespace);
                                    trait.ValueKind = ConstantKind.Namespace;

                                    BuildMeta(trait, member.Metas);

                                    @class.Traits.Add(trait);

                                }
                                else if (member.Access.IsStatic)
                                {
                                    ASTrait trait = MakeTraitFromAS3Member(member, outscopeNS, outscopeNS + ":" + cls.Name, true, NamespaceKind.Private);
                                    trait.IsStatic = true;
                                    BuildClassMember(member, trait, @class, scriptMethods, as3);
                                    @class.Traits.Add(trait);
                                }
                            }

                            for (int j = 0; j < cls.CAnonymousFunction.Count; j++)
                            {
                                var function = cls.CAnonymousFunction[j];

                                ASMethod f = new ASMethod(script, function.Token);
                                BuildMethod(f, function,as3);
                                
                                scriptMethods.Add(new Tuple<ASMethod, AS3Function>(f, function));

                                ASTrait trait = new ASTrait(function.Token);
                                trait.Kind = TraitKind.Function;
                                trait.Function = f;
                                trait.QName = new ASMultiname()
                                {
                                    Kind = MultinameKind.QName,
                                    Name = "@#" + function.ClosureId.ToString(),
                                    Namespace = @class.QName.Namespace
                                };
                                trait.Value = new ASTrait.TraitValue(ASTrait.TraitValueType.AS3Function, as3._functions.IndexOf(function));

                                @class.Traits.Add(@trait);

                            }


                            var clsTrait =
                                new ASTrait(@class.Token)
                                {
                                    Attributes = TraitAttributes.None,
                                    Class = @class,
                                    Kind = TraitKind.Class,
                                    QName = @class.QName
                                }
                                ;

                            //收集meta
                            BuildMeta(clsTrait, cls.Metas);

                            script.Traits.Add(clsTrait);
                        }
                        else if (ci is AS3Interface)
                        {
                            var inf = ci as AS3Interface;
                            CheckInterfaceAccess(inf);
                            if (inf.Access.IsPublic)
                            {
                                throw new SyntaxException(inf.Token, "The public attribute can only be used inside a package.");
                            }
                            if (inf.Access.IsPrivate)
                            {
                                throw new SyntaxException(inf.Token, "The private attribute may be used only on class property definitions.");
                            }

                            ASInstance instance = new ASInstance(
                               new ASMultiname()
                               {
                                   Kind = MultinameKind.QName,
                                   Name = inf.Name,
                                   Namespace = new ASNamespace()
                                   {
                                       Kind = NamespaceKind.Private,
                                       Name = outscopeNS,
                                       in_package = as3.Package.Name
                                   }

                               }
                               );
                            instance.Flags = ClassFlags.Interface;

                            for (int j = 0; j < ci.ExtendsNames.Count; j++)
                            {
                                string extends = ci.ExtendsNames[j];

                                instance.Interfaces.Add(
                                    new ASMultiname()
                                    {
                                        Kind = MultinameKind.TBD,
                                        Name = extends,
                                        Namespace = new ASNamespace()
                                        {
                                            Kind = NamespaceKind.TBD,
                                            Name = ""
                                        }
                                    }
                                    );
                            }

                            MakeInfMembers(instance, inf, outscopeNS + ":" + inf.Name, scriptMethods, as3);

                            ASClass @class = new ASClass(as3.OutPackage.outpackage_classes_interfaces[i].Token, 
                                GetClassId(instance.QName.ToString())); //class_token.Add(@class, as3.OutPackage.outpackage_classes_interfaces[i].Token);
                            @class.Instance = instance;

                            var interfaceTrait =
                                new ASTrait(@class.Token)
                                {
                                    Attributes = TraitAttributes.None,
                                    Class = @class,
                                    Kind = TraitKind.Class,
                                    QName = @class.QName
                                }
                                ;

                            //收集meta
                            BuildMeta(interfaceTrait, inf.Metas);

                            script.Traits.Add(interfaceTrait);


                        }
                    }

                    

                    for (int i = 0; i < as3.OutPackage.Members.Count; i++)
                    {
                        var member = as3.OutPackage.Members[i];
                        if (member.Access.IsStatic)
                        {
                            throw new SyntaxException(member.Token, "The static attribute may be used only on definitions inside a class.");
                        }
                        if (member.Access.IsPublic)
                        {
                            throw new SyntaxException(member.Token, "The public attribute can only be used inside a package.");
                        }
                        if (member.Access.IsPrivate)
                        {
                            throw new SyntaxException(member.Token, "The private attribute may be used only on class property definitions.");
                        }

                        if (member.Access.IsOverride)
                        {
                            throw new SyntaxException(member.Token, "The override attribute may be used only on class property definitions.");
                        }
                        if (member.Access.IsFinal)
                        {
                            throw new SyntaxException(member.Token, "The attribute final can only be used on a method defined in a class.");
                        }
                        if (member.Access.NameSpaceToken != null)
                        {
                            throw new SyntaxException(member.Token, "A user-defined namespace attribute can only be used at the top level of a class definition.");
                        }
                        ASTrait trait = new ASTrait(member.Token);
                        trait.QName = new ASMultiname()
                        {
                            Kind = MultinameKind.QName,
                            Name = member.Name,
                            Namespace = new ASNamespace()
                            {
                                Kind = NamespaceKind.Private,
                                Name = outscopeNS
                            }
                        };




                        if (member is AS3Variable)
                        {
                            MakeVarTrait(member, trait, as3);
                            script.Traits.Add(trait);
                        }
                        else if (member is AS3Const)
                        {
                            MakeConstTrait(member, trait, as3);
                            script.Traits.Add(trait);
                        }
                        else if (member is AS3Function)
                        {
                            AS3Function function = (AS3Function)member;

                            ASMethod f = new ASMethod(script, function.Token);

							if (!function.IsAnonymous)
							{
								if (script.Traits.Any(
										(t) => { return t.Kind == TraitKind.Slot && t.Value != null && t.Value.ValueType == ASTrait.TraitValueType.AS3Function && t.QName.Name == member.Name; }
										))
								{
									throw new SyntaxException(member.Token, "Duplicate function definition " + member.Name + ".");
								}
							}



							BuildMethod(f, function, as3);

                            if (function.IsAtPackageMemberScope)
                            {
                                f.Flags |= MethodFlags.PackageMemberScope;
                            }

                            scriptMethods.Add(new Tuple<ASMethod, AS3Function>(f, function));

                            if (!function.IsAnonymous)
                            {
                                trait.Kind = TraitKind.Slot;
                                //trait.Value = function;
                                trait.Value = new ASTrait.TraitValue(ASTrait.TraitValueType.AS3Function, as3._functions.IndexOf(function));
                                script.Traits.Add(trait);
                            }
                            else if (function.IsAnonymous && !string.IsNullOrWhiteSpace(function.Name))
                            {
                                trait.QName.Name = function.Name + "#anonymous:" + function.Token.line + ":" + function.Token.ptr;
								trait.Kind = TraitKind.Slot;
								//trait.Value = function;
								trait.Value = new ASTrait.TraitValue(ASTrait.TraitValueType.AS3Function, as3._functions.IndexOf(function));

                                f.Name = trait.QName.Name;

								script.Traits.Add(trait);
							}

                        }
                        else if (member is AS3NameSpace)
                        {
                            if (script.Traits.Any(
                                     (t) => { return t.ValueKind == ConstantKind.Namespace && t.Kind == TraitKind.Constant && t.QName.Name == member.Name; }
                                     ))
                            {
                                throw new SyntaxException(member.Token, "Duplicate namespace definition.");
                            }


                            AS3NameSpace ns = (AS3NameSpace)member;
                            ASNamespace @namespace = new ASNamespace();

                            string priv_ns = "private$" + as3.key;

                            @namespace.Name = priv_ns + ":" + ns.Name;
                            @namespace.Kind = NamespaceKind.PackageInternal;
                            @namespace.def_uri = ns.URI;

                            trait.Kind = TraitKind.Constant;

                            trait.Value = new ASTrait.TraitValue(@namespace);
                            trait.ValueKind = ConstantKind.Namespace;

                            BuildMeta(trait, member.Metas);

                            script.Traits.Add(trait);
                        }

                    }

                    //解析function内的变量和常量。
                    for (int i = 0; i < scriptMethods.Count; i++)
                    {
                        var m = scriptMethods[i].Item1;
                        var f = scriptMethods[i].Item2;

                        if (m.Body == null)
                        {
                            m.Body = new ASMethodBody(m);

                            if (f != null)
                            {
                                for (int j = 0; j < f.FunctionScope.Members.Count; j++)
                                {
                                    bool isoutscope = false;
                                    var parent = f.FunctionScope.ParentScope;
                                    while (true)
                                    {
                                        if (parent is AS3ClassInterfaceBase)
                                        {
                                            isoutscope = ((AS3ClassInterfaceBase)parent).IsOutPackage;
                                            break;
                                        }
                                        else if (parent is AS3Function.AS3FunctionScope)
                                        {
                                            parent = ((AS3Function.AS3FunctionScope)parent).ParentScope;
                                        }
                                        else if (parent is AS3OutPackage)
                                        {
                                            isoutscope = true;
                                            break;
                                        }
                                        else if (parent is AS3Package.PackageMemberScope)
                                        {
                                            isoutscope = false;
                                            break;
                                        }

                                    }


                                    var member = f.FunctionScope.Members[j];

                                    ASTrait trait = new ASTrait(member.Token);
                                    trait.QName = new ASMultiname()
                                    {
                                        Kind = MultinameKind.QName,
                                        Name = member.Name,
                                        Namespace = new ASNamespace()
                                        {
                                            Kind = NamespaceKind.PackageInternal,
                                            Name = isoutscope ? outscopeNS : as3.Package.Name
                                        }
                                    };

                                    if (member is AS3Variable)
                                    {
                                        MakeVarTrait(member, trait, as3);
                                        m.Body.Traits.Add(trait);
                                    }
                                    else if (member is AS3Const)
                                    {
                                        MakeConstTrait(member, trait, as3);
                                        m.Body.Traits.Add(trait);
                                    }
                                    else if (member is AS3Function)
                                    {
										AS3Function function = (AS3Function)member;

                                        if (!function.IsAnonymous)
                                        {
                                            if (m.Body.Traits.Any(
                                                    (t) => { return t.Kind == TraitKind.Slot && t.Value !=null && t.Value.ValueType == ASTrait.TraitValueType.AS3Function && t.QName.Name == member.Name; }
                                                    ))
                                            {
                                                throw new SyntaxException(member.Token, "Duplicate function definition " + member.Name + ".");
                                            }
                                        }

										ASMethod _fun = new ASMethod(m.Body,function.Token );
                                        BuildMethod(_fun, function, as3);
                                        scriptMethods.Add(new Tuple<ASMethod, AS3Function>(_fun, function));

                                        if (!function.IsAnonymous)
                                        {
                                            trait.Kind = TraitKind.Slot;
                                            trait.Value = new ASTrait.TraitValue(ASTrait.TraitValueType.AS3Function, as3._functions.IndexOf(function));
                                            m.Body.Traits.Add(trait);
                                        }
										else if (function.IsAnonymous && !string.IsNullOrWhiteSpace(function.Name))
										{
											trait.QName.Name = function.Name + "#anonymous:" + function.Token.line + ":" + function.Token.ptr;
											trait.Kind = TraitKind.Slot;
											//trait.Value = function;
											trait.Value = new ASTrait.TraitValue(ASTrait.TraitValueType.AS3Function, as3._functions.IndexOf(function));

											_fun.Name = trait.QName.Name;

											m.Body.Traits.Add(trait);
										}
									}
                                    else if (member is AS3NameSpace)
                                    {
                                        if (m.Body.Traits.Any(
                                                 (t) => { return t.ValueKind == ConstantKind.Namespace && t.Kind == TraitKind.Constant && t.QName.Name == member.Name; }
                                                 ))
                                        {
                                            throw new SyntaxException(member.Token, "Duplicate namespace definition.");
                                        }


                                        AS3NameSpace ns = (AS3NameSpace)member;
                                        ASNamespace @namespace = new ASNamespace();
                                        @namespace.def_uri = ns.URI;

                                        string priv_ns = "private$" + as3.key;

                                        @namespace.Name = ""  ;

                                        var ps = f.FunctionScope.ParentScope;

                                        while (ps != null)
                                        {
                                            if (ps is AS3Class)
                                            {
                                                @namespace.Name =( isoutscope?priv_ns+":" :"") + ((AS3Class)ps).Name + "/" + @namespace.Name;
                                                ps = null;
                                            }
                                            else if (ps is AS3Function.AS3FunctionScope)
                                            {
                                                var _f = ((AS3Function.AS3FunctionScope)ps).function;

                                                @namespace.Name = ((isoutscope && !_f.IsAnonymous) ? (priv_ns + ":") : "") + (_f.IsAnonymous?"anonymous":_f.Name ) + "/" + @namespace.Name;
                                                ps = ((AS3Function.AS3FunctionScope)ps).ParentScope;
                                            }
                                            else
                                            {
                                                ps = null;
                                            }
                                                

                                        }

                                        if (f.IsConstructor)
                                        {
                                            @namespace.Name += "$constructor/" + (isoutscope ? priv_ns + ":" : "") + ns.Name;
                                        }
                                        else if (f.IsAnonymous)
                                        {
                                            @namespace.Name += "anonymous/" + (isoutscope ? priv_ns + ":" : "") + ns.Name;
                                        }
                                        else
                                        {
                                            @namespace.Name += (isoutscope ? priv_ns + ":" : "") + f.Name + "/" + (isoutscope ? priv_ns + ":" : "") + ns.Name;
                                        }


                                        if (isoutscope)
                                        {
                                            
                                        }
                                        else
                                        {
                                            if (!string.IsNullOrEmpty(as3.Package.Name))
                                            {
                                                @namespace.Name = as3.Package.Name + "/" + @namespace.Name;
                                            }
                                        }
                                        
                                        



                                        @namespace.Kind = NamespaceKind.PackageInternal;

                                        trait.Kind = TraitKind.Constant;

                                        trait.Value = new ASTrait.TraitValue(@namespace);
                                        trait.ValueKind = ConstantKind.Namespace;

                                        BuildMeta(trait, member.Metas);

                                        m.Body.Traits.Add(trait);
                                    }


                                }
                            }
                        }
                    }

                    //合成或查找ASClass,ASInstance的构造函数
                    for (int i = 0; i < script.Traits.Count; i++)
                    {
                        var trait = script.Traits[i];
                        if (trait.Kind == TraitKind.Class)
                        {
                            var cls_initializer = new ASMethod(trait.Class ,trait.Class.Token ) //class必定没有构造函数，合成一个
                            {
                                IsAnonymous = false,
                                IsConstructor = true,
                                Name = "$cinit",
                                ReturnType = new ASMultiname()
                                {
                                    Kind = MultinameKind.QName,
                                    Name = "void",
                                    Namespace = new ASNamespace()
                                    {
                                        Kind = NamespaceKind.TBD, //函数返回类型会在下一步进行分析，因此自动合成的void函数类型设置成TBD
                                        Name = ""
                                    }
                                }
                            };
                            cls_initializer.Body = new ASMethodBody(cls_initializer);
                            trait.Class.Constructor = cls_initializer;

                            scriptMethods.Add(new Tuple<ASMethod, AS3Function>(cls_initializer, null));

                            var instance = trait.Class.Instance;
                            foreach (var item in instance.Traits)
                            {
                                if (item.Kind == TraitKind.Method)
                                {
                                    if (item.Method.IsConstructor)
                                    {
                                        instance.Constructor = item.Method;
                                        break;
                                    }
                                }
                            }

                            if (instance.Constructor == null && !instance.IsInterface)
                            {
                                var ins_ctor = new ASMethod(instance,trait.Class.Token) //没有找到构造函数，合成一个
                                {
                                    IsAnonymous = false,
                                    IsConstructor = true,
                                    Name = instance.QName.Name,
                                    ReturnType = new ASMultiname()
                                    {
                                        Kind = MultinameKind.QName,
                                        Name = "void",
                                        Namespace = new ASNamespace()
                                        {
                                            Kind = NamespaceKind.TBD,//函数返回类型会在下一步进行分析，因此自动合成的void函数类型设置成TBD
                                            Name = ""
                                        }
                                    }
                                };
                                ins_ctor.ast_function_index = -2;
                                ins_ctor.Body = new ASMethodBody(ins_ctor);
                                instance.Constructor = ins_ctor;

                                scriptMethods.Add(new Tuple<ASMethod, AS3Function>(ins_ctor, null));
                            }
                        }

                    }


                    var script_initializer = new ASMethod(script, null)
                    {
                        Name = "global::init",
                        IsAnonymous = true,
                        IsConstructor = false,
                        ReturnType = new ASMultiname()
                        {
                            Kind = MultinameKind.QName,
                            Name = "void",
                            Namespace = new ASNamespace()
                            {
                                Kind = NamespaceKind.TBD,//函数返回类型会在下一步进行分析，因此自动合成的void函数类型设置成TBD
                                Name = ""
                            }
                        },
                    };
                    script_initializer.Body = new ASMethodBody(script_initializer);
                    script.Initializer = script_initializer;

                    scriptMethods.Add(new Tuple<ASMethod, AS3Function>(script.Initializer, null));


                    List<List<Tuple<string, Token>>> method_use_ns = new List<List<Tuple<string, Token>>>();

                    //foreach (var item in scriptMethods)
                    for(int k=0;k<scriptMethods.Count;k++)
                    {
                        var item = scriptMethods[k];
                        var function = item.Item2;
                        List<Tuple<string, Token>> useNsSet = new List<Tuple<string, Token>>();
                        if (function == null)
                        {
                            if (item.Item1 == script.Initializer)
                            {

                                var ns = as3.OutPackage.UseNamespaceSet;
                                for (int i = 0; i < ns.Count; i++)
                                {
                                    useNsSet.Add(new Tuple<string, Token>(ns[i].UseNameSpace, ns[i].Token));
                                }
                            }

                        }
                        else
                        {
                            AS3MemberScope scope = function.FunctionScope;

                            while (scope != null)
                            {
                                var ns = scope.UseNamespaceSet;
                                for (int i = 0; i < ns.Count; i++)
                                {
                                    useNsSet.Add(new Tuple<string, Token>(ns[i].UseNameSpace, ns[i].Token));
                                }

                                if (scope is AS3OutPackage)
                                {
                                    scope = null;
                                }
                                else if (scope is AS3Package.PackageMemberScope)
                                {
                                    scope = null;
                                }
                                else if (scope is AS3Function.AS3FunctionScope)
                                {
                                    scope = ((AS3Function.AS3FunctionScope)scope).ParentScope;
                                }
                                else if (scope is AS3ClassInterfaceBase)
                                {
                                    if (((AS3ClassInterfaceBase)scope).IsOutPackage)
                                    {
                                        scope = as3.OutPackage;
                                    }
                                    else
                                    {
                                        scope = as3.Package.MemberScope;
                                    }
                                }
                            }
                        }

                        method_use_ns.Add(useNsSet);
                    }

                    if (script.Traits.Count == 0)
                    {
                        return null;
                    }
                    else
                    {
                        
                        for (int i = 0; i < _classes.Count; i++)
                        {
                            if (_classes.Any((c) => { return c != _classes[i] && c.QName == _classes[i].QName; }))
                            {
                                throw new SyntaxException((Token)_classes[i].Token , $"Duplicate {(_classes[i].Instance.IsInterface?"interface":"class")} definition: {_classes[i].QName.Name}");
                            }
                        }

                        //检查重名的Trait
                        foreach (var container in _containers)
                        {

                            if (container is ASScript)
                            {
								List<ASTrait> redefine = new List<ASTrait>();

								//如果是global,遇到 function f() {}; var f; ,就删除 f只保留 function f(){} Test262里说的。
								//https://github.com/tc39/test262/blob/main/test/language/block-scope/syntax/redeclaration-global/allowed-to-redeclare-function-declaration-with-var.js
								foreach (var t in container.Traits)
                                {
                                    if (t.Kind == TraitKind.Slot && (t.Type == null ||
                                        ((t.Type.Name == "*" || t.Type.Name == "Function") && t.Type.Namespace.Name == "" )  ) )
                                    {
                                        if (container.Traits.Any(
                                            (t2) => t2.QName == t.QName && t2 !=t &&
                                            t2.Type == null && t2.Value != null && t2.Value.ValueType == ASTrait.TraitValueType.AS3Function
                                            ))
                                        {
                                            redefine.Add(t);
                                        }
                                    }
                                }


								foreach (var item in redefine)
								{
									container.Traits.Remove(item);
								}
							}

							List<ASTrait> toremove=new List<ASTrait>();
                            foreach (var t in container.Traits)
                            {
                                if (toremove.Contains(t))
                                    continue;

                                if (container.Traits.Any((t2) => t2.QName == t.QName && t2 != t && 
                                    (t2.Type != t.Type || t2.TypeKind != t.TypeKind || t2.Kind != t.Kind || t2.ValueKind != t.ValueKind)
                                    &&
                                    !((t.Kind == TraitKind.Getter && t2.Kind == TraitKind.Setter) || (t.Kind == TraitKind.Setter && t2.Kind == TraitKind.Getter) )

                                    )
                                    
                                    )
                                {
                                    
                                    switch (t.QName.Namespace.Kind)
                                    {
                                        
                                        case NamespaceKind.PackageInternal:
                                            throw new SyntaxException(t.Token, $"A conflict exists with definition {t.QName.Name} in namespace internal.");
                                        case NamespaceKind.Private:
                                            throw new SyntaxException(t.Token, $"A conflict exists with definition {t.QName.Name} in namespace private.");
                                        case NamespaceKind.Package:
                                            throw new SyntaxException(t.Token, $"A conflict exists with definition {t.QName.Name} in namespace public.");
                                        case NamespaceKind.Protected:
                                            throw new SyntaxException(t.Token, $"A conflict exists with definition {t.QName.Name} in namespace protected.");
                                        case NamespaceKind.TBD:
                                            throw new SyntaxException(t.Token, $"A conflict exists with definition {t.QName.Name} in namespace {t.QName.Namespace.Name}.");
                                        default:
                                            throw new InvalidOperationException();
                                    }
                                }

                                 toremove.AddRange(container.Traits.Where((t2) => t2.QName == t.QName && t2 != t 
                                    &&
                                    !((t2.Type != t.Type || t2.TypeKind != t.TypeKind || t2.Kind != t.Kind || t2.ValueKind != t.ValueKind))
                                    &&
                                    (t.Kind == TraitKind.Slot)
                                 ));
                                
                            }

                            foreach (var item in toremove)
                            {
                                container.Traits.Remove(item);
                            }

                        }




                        for (int i = 0; i < scriptMethods.Count; i++)
                        {
                            if (scriptMethods[i].Item1.ast_function_index != -2)
                            {
                                scriptMethods[i].Item1.ast_function_index = as3._functions.IndexOf(scriptMethods[i].Item2);
                            }
                        }

                        script.allContainers = _containers.ToList();

                        return new ScriptDef()
                        {
                            Script = script,
                            scriptMethods = (new ASMethod[] { null }).Concat(scriptMethods.Select((o) => { return o.Item1; })).ToList(),
                            //ast_functions = (new int[] { -1 }).Concat(scriptMethods.Select((o) => { return as3._functions.IndexOf(o.Item2); })).ToList(),

                            method_use_namesapce = ( new List<Tuple<string, Token>>[] { null } ).Concat( method_use_ns ).ToList(),

                            package_imports = as3.Package.imports.ToList(),
                            script_imports = as3.OutPackage.imports.ToList(),

                            multinames = Distinct(_multinames),
                            namespaces = Distinct(_namespaces),
                            scriptClasses = Distinct(_classes),
                            namespaceSets = Distinct(_namespaceSets),

                            containers = script.allContainers, //_containers.ToList(),

                            AS3SrcFile = as3,

                            sourceFile = as3.sourceFile,
                            fullPath = as3.sourceFileFullpath
                            
                        };
                    }
                }
                else
                {
                    throw new FileException(srcFile);
                }
            }
            finally
            {
                ASMultiname.NewMultiName -= ASMultiname_NewMultiName;
                ASNamespace.NewNameSpace -= ASNamespace_NewNameSpace;
                ASClass.NewClass -= ASClass_NewClass;
                ASNamespaceSet.NewNamespaceSet -= ASNamespaceSet_NewNamespaceSet;
                ASContainer.NewContainer -= ASContainer_NewContainer;
            }
        }

        
        private List<T> Distinct<T>(List<T> list) where T : class, IEquatable<T>
        { 
            List<T> result = new List<T>();
            result.Add(null);
            for (int i = 0; i < list.Count; i++)
            {
                if (!result.Contains(list[i]))
                {
                    result.Add(list[i]);
                }
            }
            return result;
        }

        private void ASContainer_NewContainer(object sender, ASContainer e)
        {
            _containers.Add(e);
        }

        private void ASNamespaceSet_NewNamespaceSet(object sender, ASNamespaceSet e)
        {
           _namespaceSets.Add(e);
        }
        private void ASClass_NewClass(object sender, ASClass e)
        {
            _classes.Add(e);
        }

        private void ASNamespace_NewNameSpace(object sender, ASNamespace e)
        {
            _namespaces.Add(e);
        }

        private void ASMultiname_NewMultiName(object sender, ASMultiname e)
        {
            _multinames.Add(e);
        }

        private void MakeInfMembers(ASInstance instance, AS3Interface inf, string inf_qname, List<Tuple<ASMethod, AS3Function>> scriptMethods, AS3SrcFile as3)
        {
            for (int i = 0; i < inf.Members.Count; i++)
            {
                var member = inf.Members[i];
                if (member.Access.IsStatic)
                {
                    throw new SyntaxException(member.Token, "The static attribute may be used only on definitions inside a class.");
                }

                ASTrait trait = MakeTraitFromAS3Member(member, inf_qname, inf_qname, false, NamespaceKind.Namespace);

                AS3Function function = (AS3Function)member;
                if (!function.IsMethod)
                {
                    throw new InvalidOperationException();
                }

                if (member.Access.IsFinal)
                {
                    throw new SyntaxException(member.Token, "The attribute final can only be used on a method defined in a class.");
                }
                if (member.Access.IsOverride)
                {
                    throw new SyntaxException(member.Token, "The override attribute can only be used on a method defined in a class.");
                }

                BuildMeta(trait, function.Metas);

                ASMethod method = new ASMethod(instance, function.Token);
                method.Trait = trait;
                BuildMethod(method, function, as3);

                scriptMethods.Add(new Tuple<ASMethod, AS3Function>(method, function));

                if (function.IsGet)
                {
                    trait.Kind = TraitKind.Getter;
                }
                else if (function.IsSet)
                {
                    trait.Kind = TraitKind.Setter;
                }
                else
                {
                    trait.Kind = TraitKind.Method;
                }
                trait.Method = method;

                instance.Traits.Add(trait);


            }
        }

        private void CheckInterfaceAccess(AS3Interface inf)
        {
            if (inf.Access.IsStatic)
            {
                throw new SyntaxException(inf.Token, "The static attribute may be used only on definitions inside a class.");
            }
            if (inf.Access.IsOverride)
            {
                throw new SyntaxException(inf.Token, "The override attribute may be used only on definitions inside a class.");
            }
            if (inf.Access.IsDynamic)
            {
                throw new SyntaxException(inf.Token, "Interface attribute dynamic is invalid.");
            }
            if (inf.Access.IsFinal)
            {
                throw new SyntaxException(inf.Token, "Interface attribute final is invalid.");
            }
        }

        private void BuildClassMember(AS3Member member, ASTrait trait, ASContainer parent_activation, List<Tuple<ASMethod, AS3Function>> scriptMethods, AS3SrcFile as3)
        {

            if (member is AS3Function)
            {
                AS3Function function = (AS3Function)member;
                if (!function.IsMethod)
                {
                    throw new InvalidOperationException();
                }

                if (member.Access.IsFinal)
                {
                    throw new SyntaxException(member.Token, "The attribute final can only be used on a method defined in a class.");
                }
                if (member.Access.IsOverride)
                {
                    throw new SyntaxException(member.Token, "Functions cannot be both static and override.");
                }

                BuildMeta(trait, function.Metas);

                ASMethod method = new ASMethod(parent_activation, function.Token);
                method.Trait = trait;
                BuildMethod(method, function,  as3 );

                scriptMethods.Add(new Tuple<ASMethod, AS3Function>(method, function));

                if (function.IsGet)
                {
                    trait.Kind = TraitKind.Getter;
                }
                else if (function.IsSet)
                {
                    trait.Kind = TraitKind.Setter;
                }
                else
                {
                    trait.Kind = TraitKind.Method;
                }
                trait.Method = method;
            }
            else if (member is AS3Variable)
            {
                MakeVarTrait(member, trait, as3);
            }
            else if (member is AS3Const)
            {
                MakeConstTrait(member, trait, as3);
            }


        }

        private void MakeClassInstanceMembers(ASInstance instance, AS3Class @class, string qname_ns, NamespaceKind defaultkind, List<Tuple<ASMethod, AS3Function>> scriptMethods, AS3SrcFile as3)
        {
            bool hasprotected = false;
            for (int i = 0; i < @class.Members.Count; i++)
            {
                var member = @class.Members[i];
                if (member is AS3NameSpace)
                {
                    continue;
                }

                if (!member.Access.IsStatic)
                {
                    ASTrait trait = MakeTraitFromAS3Member(member, qname_ns, qname_ns + ":" + @class.Name, false, defaultkind);


                    if (member is AS3Function)
                    {
                        AS3Function function = (AS3Function)member;
                        if (!function.IsMethod)
                        {
                            throw new InvalidOperationException();
                        }

                        if (member.Access.IsFinal)
                        {
                            trait.Attributes = trait.Attributes | TraitAttributes.Final;
                        }
                        if (member.Access.IsOverride)
                        {
                            trait.Attributes = trait.Attributes | TraitAttributes.Override;
                        }
                        

                        BuildMeta(trait, function.Metas);

                        ASMethod method = new ASMethod(instance, function.Token);
                        method.Trait = trait;
                        BuildMethod(method, function, as3);

                        scriptMethods.Add(new Tuple<ASMethod, AS3Function>(method, function));

                        if (function.IsGet)
                        {
                            trait.Kind = TraitKind.Getter;
                        }
                        else if (function.IsSet)
                        {
                            trait.Kind = TraitKind.Setter;
                        }
                        else
                        {
                            trait.Kind = TraitKind.Method;
                        }
                        trait.Method = method;


                    }
                    else if (member is AS3Variable)
                    {
                        MakeVarTrait(member, trait,as3);
                    }
                    else if (member is AS3Const)
                    {
                        MakeConstTrait(member, trait, as3);
                    }

                    if (trait.QName.Namespace.Kind == NamespaceKind.Protected)
                    {
                        hasprotected = true;
                    }

                    instance.Traits.Add(trait);
                }
            }

            if (hasprotected)
            {
                instance.Flags |= ClassFlags.ProtectedNamespace;
                instance.ProtectedNamespace = new ASNamespace()
                {
                    Kind = NamespaceKind.Protected,
                    Name = qname_ns + ":" + @class.Name
                };

            }

        }

        private void MakeInstanceFlagsAndSuperAndInterface(ASInstance instance, AS3Class @class)
        {
            if (!@class.Access.IsDynamic)
            {
                instance.Flags = instance.Flags | ClassFlags.Sealed;
            }
            if (@class.Access.IsFinal)
            {
                instance.Flags = instance.Flags | ClassFlags.Final;
            }

            if (@class.ExtendsNames.Count == 1)
            {
                instance.Super = new ASMultiname()
                {
                    Kind = MultinameKind.TBD,
                    Name = @class.ExtendsNames[0],
                    Namespace = new ASNamespace()
                    {
                        Kind = NamespaceKind.TBD,
                        Name = ""
                    }
                };
            }
            else if (@class.ExtendsNames.Count == 0 && !(@class.Name == "Object" && string.IsNullOrEmpty(@class.Package.Name)))
            {
                //所有类都继承自Object
                instance.Super = new ASMultiname()
                {
                    Kind = MultinameKind.TBD,
                    Name = "Object",
                    Namespace = new ASNamespace()
                    {
                        Kind = NamespaceKind.TBD,
                        Name = ""
                    }
                };

            }


            for (int i = 0; i < @class.ImplementsNames.Count; i++)
            {
                string impl = @class.ImplementsNames[i];

                instance.Interfaces.Add(
                    new ASMultiname()
                    {
                        Kind = MultinameKind.TBD,
                        Name = impl,
                        Namespace = new ASNamespace()
                        {
                            Kind = NamespaceKind.TBD,
                            Name = ""
                        }
                    }
                    );

            }
        }

        private void CheckClassAccess(AS3Class cls)
        {
            if (cls.Access.IsStatic)
            {
                throw new SyntaxException(cls.Token, "The static attribute may be used only on definitions inside a class.");
            }
            if (cls.Access.IsOverride)
            {
                throw new SyntaxException(cls.Token, "The override attribute may be used only on definitions inside a class.");
            }
            if (cls.Access.IsPrivate)
            {
                throw new SyntaxException(cls.Token, "The private attribute may be used only on definitions inside a class.");
            }
            if (cls.Access.IsProtected)
            {
                throw new SyntaxException(cls.Token, "The protected attribute can only be used on class property definitions.");
            }
        }


        static internal ASMultiname ResolveTypeStr(string typeStr,Token token, bool invector=false)
        {
            if (invector && typeStr == "null")
            { 
                throw new SyntaxException(token, "'null' is not allowed here.");
            }
            else if (invector && typeStr == "void")
            {
                throw new SyntaxException(token, "'void' is not allowed here.");
            }
            else if (typeStr.StartsWith("Vector.<"))
            {
                return new ASMultiname()
                {
                    Kind = MultinameKind.TypeName,
                    Name = null,

                    QName = new ASMultiname()
                    {
                        Kind = MultinameKind.QName,
                        Name = "Vector",
                        Namespace = new ASNamespace()
                        {
                            Kind = NamespaceKind.Package,
                            Name = "__AS3__.vec"
                        }
                    },

                    Types = new List<ASMultiname>
                    {
                        ResolveTypeStr(typeStr.Substring(8, typeStr.Length -9 ),token,true)
                    }

                };
            }
            else
            { 
                return new ASMultiname()
                {
                    Kind = MultinameKind.QName,
                    Name = typeStr,
                    Namespace = new ASNamespace()
                    {
                        Kind = NamespaceKind.TBD,
                        Name = string.Empty
                    }
                };
            }
        }

        private void MakeConstTrait(AS3Member member, ASTrait trait,AS3SrcFile as3)
        {
            AS3Const @const = (AS3Const)member;
            if (member.Access.IsFinal)
            {
                throw new SyntaxException(@const.Token, "The attribute final can only be used on a method defined in a class.");
            }
            if (member.Access.IsOverride)
            {
                throw new SyntaxException(@const.Token, "The attribute override can only be used on a method defined in a class.");
            }

            trait.Kind = TraitKind.Constant;
            BuildMeta(trait, @const.Metas);

            trait.Type = ResolveTypeStr(@const.TypeStr,member.Token);
            

            if (@const.ValueExpr != null)
            {
                trait.Value = new ASTrait.TraitValue( ASTrait.TraitValueType.AS3Expression, as3._expressions.IndexOf( @const.ValueExpr));
            }
            else
            {
                trait.ValueKind = ConstantKind.Undefined;
            }

        }

        private void MakeVarTrait(AS3Member member, ASTrait trait, AS3SrcFile as3)
        {
            AS3Variable var = (AS3Variable)member;

            if (member.Access.IsFinal)
            {
                throw new SyntaxException(var.Token, "The attribute final can only be used on a method defined in a class.");
            }
            if (member.Access.IsOverride)
            {
                throw new SyntaxException(var.Token, "The attribute override can only be used on a method defined in a class.");
            }

            trait.Kind = TraitKind.Slot;

            BuildMeta(trait, var.Metas);

            trait.Type = ResolveTypeStr(var.TypeStr,member.Token);


            if (var.ValueExpr != null)
            {
                trait.Value = new ASTrait.TraitValue(ASTrait.TraitValueType.AS3Expression, as3._expressions.IndexOf( var.ValueExpr));
            }
            else
            {
                trait.ValueKind = ConstantKind.Undefined;
            }
        }

        private ASTrait MakeTraitFromAS3Member(AS3Member member, string qname_namespace, string qname_private_ns, bool isstatic, NamespaceKind defaultKind)
        {
            ASTrait trait = new ASTrait(member.Token);
            trait.Attributes = TraitAttributes.None;
            trait.QName = new ASMultiname()
            {
                Kind = MultinameKind.QName,
                Name = member.Name
            };

            if (member.Access.NameSpaceToken != null)
            {
                trait.QName.Namespace = new ASNamespace()
                {
                    Kind = NamespaceKind.TBD,
                    Name = member.Access.NameSpace
                };
            }
            else
            {
                if (member.Access.IsPublic)
                {
                    trait.QName.Namespace = new ASNamespace()
                    {
                        Kind = NamespaceKind.Package,
                        Name = ""
                    };
                }
                else if (member.Access.IsPrivate)
                {
                    trait.QName.Namespace = new ASNamespace()
                    {
                        Kind = NamespaceKind.Private,
                        Name = qname_private_ns
                    };
                }

                else if (member.Access.IsProtected)
                {
                    trait.QName.Namespace = new ASNamespace()
                    {
                        Kind = isstatic ? NamespaceKind.StaticProtected : NamespaceKind.Protected,
                        Name = qname_private_ns
                    };
                }
                else if (member.Access.IsInternal)
                {
                    trait.QName.Namespace = new ASNamespace()
                    {
                        Kind = NamespaceKind.PackageInternal,
                        Name = qname_private_ns
                    };
                }
                else
                {
                    //default internal
                    trait.QName.Namespace = new ASNamespace()
                    {
                        Kind = defaultKind,
                        Name = qname_namespace
                    };
                }
            }

            return trait;
        }

        private void BuildMethod(ASMethod method, AS3Function function,AS3SrcFile as3)
        {
            method.Name = function.Name;
            method.IsConstructor = function.IsConstructor;
            method.IsAnonymous = function.IsAnonymous;

            method.ReturnType = ResolveTypeStr(function.TypeStr,function.Token);
            
            if (method.IsConstructor)
            {
                if (!string.IsNullOrEmpty(function.TypeStr))
                {
                    if (function.TypeStr != "void")
                    {
                        throw new SyntaxException(function.Token, "A Constructor cannot specify a return type");
                    }
                }
            }

            method.Flags = MethodFlags.HasParamNames;

            if (function.Access.IsNative)
            {
                method.Flags |= MethodFlags.Native;
            }
            if (function.Access.IsOverride)
            {
                method.Flags |= MethodFlags.MarkOverride;
            }

            for (int j = 0; j < function.Parameters.Count; j++)
            {
                var p = function.Parameters[j];

                if (p.IsArrPara && j != function.Parameters.Count - 1)
                {
                    throw new SyntaxException(p.Token, "Rest parameters must be last.");
                }

                if (p.IsArrPara)
                {
                    if (p.TypeStr != "Array" && p.TypeStr != "*")
                    {
                        throw new SyntaxException(p.Token, "Parameters specified after the ...rest parameter definition keyword can only be an Array data type.");
                    }
                }

                if (function.IsGet)
                {
                    throw new SyntaxException(p.Token, "A getter definition must have no parameters.");
                }

                if (j == 1 && function.IsSet)
                {
                    throw new SyntaxException(p.Token, "A setter definition must have exactly one parameter.");
                }

                ASParameter parameter = new ASParameter(method);
                parameter.Name = p.Name;

                if (p.IsArrPara)
                {
                    parameter.Type = ResolveTypeStr("Array", p.Token);
                }
                else
                {
                    parameter.Type = ResolveTypeStr(p.TypeStr, p.Token);
                }

                parameter.IsOptional = (p.ValueExpr != null);
                if (parameter.IsOptional)
                {
                    parameter.ValueExprIndex = as3._expressions.IndexOf(p.ValueExpr);
                    method.Flags |= MethodFlags.HasOptional;
                    //parameter.Value = p.ValueExpr;
                }
                else if (p.IsArrPara)
                {
                    parameter.ValueExprIndex = -1;

                    method.Flags |= MethodFlags.NeedRest;
                    parameter.IsRest = true;


                }
                else
                {
                    parameter.ValueExprIndex = -1;

                    parameter.ValueKind = ConstantKind.Undefined;

                    if (j > 0)
                    {
                        if (function.Parameters[j - 1].ValueExpr != null)
                        {
                            throw new SyntaxException(p.Token, "Required parameters are not permitted after optional parameters.");
                        }
                    }
                }
                method.Parameters.Add(parameter);
            }


        }

        private void BuildMeta(ASTrait trait, List<AS3Expression> Metas)
        {
            if (Metas.Count > 0)
            {
                trait.Attributes |= TraitAttributes.Metadata;

                for (int i = 0; i < Metas.Count; i++)
                {

                    var meta = Metas[i];
                    if (meta.exprStepList.Count == 1)
                    {
                        ASMeta m = new ASMeta();
                        m.Name = meta.exprStepList[0].Arg2.Data.Value.ToString();

                        var args = (List<AS3DataStackElement>)meta.exprStepList[0].Arg3.Data.Value;
                        for (int j = 0; j < args.Count; j++)
                        {
                            m.Items.Add(new ASMetaItem() { Value = args[j].ToString() });

                        }

                        trait.ASMetadata.Add(m);
                        //out_sb.AppendLine("".PadLeft(v, '\t') + "[" + meta.exprStepList[0].Arg2.Data.Value + "(" + meta.exprStepList[0].Arg3 + ")]");
                    }
                    else if (meta.exprStepList.Count == 0)
                    {
                        ASMeta m = new ASMeta();
                        m.Name = ((List<AS3DataStackElement>)meta.Value.Data.Value)[0].ToString();
                        trait.ASMetadata.Add(m);
                    }
                    else
                    {
                        if (meta.exprStepList[0].OpCode == "Ld_Callable")
                        {
                            meta.exprStepList.RemoveAt(0);
                            meta.exprStepList[meta.exprStepList.Count - 1].Arg2 = meta.exprStepList[0].Arg2;
						}


                        ASMeta m = new ASMeta();
                        m.Name = meta.exprStepList[meta.exprStepList.Count - 1].Arg2.Data.Value.ToString();

                        Dictionary<AS3DataStackElement, ASMetaItem> dictArgItems = new Dictionary<AS3DataStackElement, ASMetaItem>();

                        for (int j = 0; j < meta.exprStepList.Count - 1; j++)
                        {
                            if (meta.exprStepList[j].Arg1.IsReg || meta.exprStepList[j].Arg2.IsReg)
                            {
                                throw new SyntaxException( meta.Token,"ASMeta Parse Error." );
                            }

                            dictArgItems.Add(meta.exprStepList[j].Arg1, new ASMetaItem()
                            {
                                Name = meta.exprStepList[j].Arg1.Data.Value.ToString(),
                                Value = meta.exprStepList[j].Arg2.Data.Value.ToString()
                            });

                            //m.Items.Add(
                            //    new ASMetaItem()
                            //    {
                            //        Name = meta.exprStepList[j].Arg1.Data.Value.ToString(),
                            //        Value = meta.exprStepList[j].Arg2.Data.Value.ToString()
                            //    });
                        }

                        var args = (List<AS3DataStackElement>)meta.exprStepList[meta.exprStepList.Count - 1].Arg3.Data.Value;
                        for (int j = 0; j < args.Count; j++)
                        {
                            if (dictArgItems.ContainsKey(args[j]))
                            {
                                m.Items.Add(new ASMetaItem()
                                {
                                    Name = args[j].ToString(),
                                    Value = dictArgItems[args[j]].Value.ToString()
                                });
                            }
                            else
                            {
                                m.Items.Add(new ASMetaItem() { Value = args[j].ToString() });
                            }
                        }


                        trait.ASMetadata.Add(m);
                    }

                }
            }

        }



        public class BuildDefineExcepition : CompilerException
        {
            public BuildDefineExcepition(string message) : base(message)
            {
            }
        }

        public static Dictionary<string, string> PrepareSrcProjPath(List<string> srcDirs)
        {
            Dictionary<string, string> src_proj = new Dictionary<string, string>();

            foreach (string dir in srcDirs)
            {
                var src = System.IO.Directory.GetFiles(dir, "*.as", SearchOption.AllDirectories);

                string proj = dir;
                if (!proj.EndsWith("\\") && !proj.EndsWith("/"))
                {
                    proj += System.IO.Path.DirectorySeparatorChar;
                }

                foreach (var s in src)
                {
                    if (!src_proj.ContainsKey(s))
                    {
                        src_proj.Add(s, proj);
                    }
                    else if (src_proj[s] != proj)
                    {
                        throw new BuildDefineExcepition($"'{s}' 重复加入不同的包:'{dir}','{src_proj[s]}'");
                    }
                }
            }

            return src_proj;

        }

        public static ScriptDef BuildDef(Parser parser, string src,string proj,string workDir,bool skipAST,bool validate,bool verbose)
        {
            string outfile = System.IO.Path.Combine(workDir, src.Substring(proj.Length)) + ".d";

            string disk_encoding = null;

            if (File.Exists(outfile))
            {
                //Try Load
                using (var fs = File.OpenRead(outfile))
                {
                    using (System.IO.StreamReader sr = new StreamReader(fs))
                    {
                        var md5 = sr.ReadLine();
                        string input = System.IO.File.ReadAllText(src);
                        var origin = new MyMD5.MyMD5().Hash(input).ToString();

                        if (md5 == origin)
                        {
                            AS3SrcFile as_srcfile = null;

                            if (!skipAST)
                            {
                                ParseTree tree = parser.ParseTree(input, AS3LexKeywords.LEXKEYWORDS, AS3LexKeywords.LEXSKIPBLANKWORDS, src);
                                if (parser.hasError)
                                {
                                    throw new ParseException(string.Empty);
                                }

                                var ast = new AS3AbstractSyntaxTree();
                                as_srcfile = ast.Analyse(tree);

                                if (ast.SyntaxError != null)
                                {
                                    throw ast.SyntaxError;
                                }
                            }

                            string store = sr.ReadLine();
                            using (StringReader reader = new StringReader(store))
                            {
                                ScriptDef script1;
                                var result = ScriptDef.Deserialize(reader, as_srcfile, out script1);

                                if (result == ScriptDef.ReadResult.Success)
                                {
                                    disk_encoding = script1.Encode();

                                    if (!validate)
                                    {
                                        return script1;
                                    }
                                }
                            }

                        }

                    }
                }
            }

            ScriptDefBuilder builder = new ScriptDefBuilder();
            AS3SrcFile as3;

            var script = builder.Build(parser,
                src, proj, out as3);

            if (script != null)
            {
                using (StringWriter sw = new StringWriter())
                {
                    if (disk_encoding != null)
                    {
                        if (script.Encode() != disk_encoding)
                        {
                            throw new BuildDefineExcepition($"'{src}'存在序列化文件，但校验失败。");
                        }

                        if (verbose)
                        {
                            Console.WriteLine("校验序列化文件成功。");
                        }
                    }
                    else
                    {
                        if (verbose)
                        {
                            Console.WriteLine("写入序列化文件。");
                        }
                    }

                    sw.WriteLine(as3.key.ToString());
                    script.Serialize(sw);


                    string outdir = System.IO.Path.GetDirectoryName(outfile);

                    Directory.CreateDirectory(outdir);

                    System.IO.File.WriteAllText(outfile, sw.ToString());

                    if (verbose)
                    {
                        Console.WriteLine(src.ToString());
                        Console.WriteLine(sw.ToString());
                    }

                }
            }
            return script;
        }

        public static int BuildDefines(List<string> srcDirs, string workDir, bool verbose ,List<ScriptDef> scriptDefs , 
            CompileContext context,
            bool skipASTExpression ,bool validate )
        {
            Dictionary<string, string> src_proj = PrepareSrcProjPath(srcDirs);

            var lex = new Lex(null);
            var tokens = lex.GetWords(AS3_LL1_GRAMMAR.GRAMMAR, false);
            Parser parser = new Parser(tokens);

            foreach (var item in src_proj)
            {
                var src = item.Key;
                var proj = item.Value;

                //特殊处理***排除airglobal中的顶级包对象：Infinity,isFinite,isNaN,parseFloat,parseInt,NaN,undefined,trace,flash.util.getDefinitionByName,flash.util.getQualifiedClassName

                string[] exclude = 
                    { "Infinity.as", "isFinite.as", 
                    "isNaN.as", "parseFloat.as", "parseInt.as",
                    "NaN.as","trace.as","undefined.as",
                    "flash\\utils\\getDefinitionByName.as",
                    "flash\\utils\\getQualifiedClassName.as"
                };

                if ( exclude.Contains( src.Replace(proj, "")))
                {
                    continue;
                }
                
                

                var def = BuildDef(parser, src, proj, workDir, skipASTExpression, validate, verbose);
                if (def != null)
                {
                    if (def.Script.QName.Name != System.IO.Path.GetFileNameWithoutExtension(src))
                    {
                        continue;
                    }


                    if (scriptDefs != null)
                    {
                        scriptDefs.Add(def);
                    }

                    if (context != null)
                    {
                        context.scriptInProj.Add(def, proj);
                    }
                }
            }


            return 0;
        }
    
    
    
    
    
    }
}
