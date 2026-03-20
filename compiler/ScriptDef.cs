using juicescript.ABC;
using juicescript.compiler.AST;
using MyMD5;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler
{

    /// <summary>
    /// 一个as文件中的对象的定义
    /// </summary>
    public class ScriptDef
    {
        public ASScript Script;

        public List<ASMethod> scriptMethods;

        //internal List<int> ast_functions;

        internal List<List< Tuple< string, Token>>> method_use_namesapce;

        public List<ASClass> scriptClasses;

        public List<string> package_imports;

        public List<string> script_imports;

        internal AS3SrcFile AS3SrcFile;

        internal List<ASMultiname> multinames;

        internal List<ASNamespace> namespaces;

        internal List<ASNamespaceSet> namespaceSets;

        internal List<ASContainer> containers;


        internal string sourceFile;
        internal string fullPath;

        private string WriteString()
        {
            System.Text.StringBuilder sb = new StringBuilder();
            sb.AppendLine($"NameSpaces\t{namespaces.Count - 1}");
            for (int i = 1; i < namespaces.Count; i++)
            {
                sb.AppendLine($"{namespaces[i].Kind}\t{namespaces[i].Name}");
            }
            sb.AppendLine();

            sb.AppendLine($"NameSpaceSets\t{namespaceSets.Count - 1}");
            for (int i = 1; i < namespaceSets.Count; i++)
            {
                sb.AppendLine($"index:{string.Join(',', namespaceSets[i].Namespaces.Select((n) => { return namespaces.IndexOf(n); }))}");
            }


            sb.AppendLine($"MultiNames\t{multinames.Count - 1}");
            for (int i = 1; i < multinames.Count; i++)
            {
                var m = multinames[i];


                sb.AppendLine($"Kind:{m.Kind}\tName:{m.Name}\t" +
                    $"NameSpace:{namespaces.IndexOf(m.Namespace)}\t{{{m.Namespace}}}\t" +
                    $"QName:{multinames.IndexOf(m.QName)}\t" +
                    $"Types:{(m.Types == null ? string.Empty : string.Join(',', m.Types.Select((t) => { return multinames.IndexOf(t); })))}\t" +
                    $"NameSpaceSet:{namespaceSets.IndexOf(m.NamespaceSet)}"
                    );



            }
            sb.AppendLine();

            sb.AppendLine($"PackageImports\t{package_imports.Count}");
            for (int i = 0; i < package_imports.Count; i++)
            {
                sb.AppendLine(package_imports[i]);
            }
            sb.AppendLine();

            sb.AppendLine($"ScriptImports\t{script_imports.Count}");
            for (int i = 0; i < script_imports.Count; i++)
            {
                sb.AppendLine(script_imports[i]);
            }
            sb.AppendLine();

            sb.AppendLine($"Methods\t{scriptMethods.Count - 1}");
            for (int i = 1; i < scriptMethods.Count; i++)
            {
                sb.AppendLine($"AS3Function:{scriptMethods[i].ast_function_index}");

                var usens = method_use_namesapce[i];
                for (int j = 0; j < usens.Count; j++)
                {
                    sb.AppendLine($"Use NameSpace:{usens[j].Item1}");
                }


                var method = scriptMethods[i];
                sb.AppendLine($"Flags:{method.Flags}");
                sb.AppendLine($"Name:{method.Name}");
                sb.AppendLine($"IsAnonymous:{method.IsAnonymous}");
                sb.AppendLine($"IsConstructor:{method.IsConstructor}");
                sb.AppendLine($"Parameters:{method.Parameters.Count}");
                for (int j = 0; j < method.Parameters.Count; j++)
                {
                    var p = method.Parameters[j];
                    sb.AppendLine($" Name:{p.Name}");
                    sb.AppendLine($" IsOptional:{p.IsOptional}");
                    sb.AppendLine($" IsRest:{p.IsRest}");
                    sb.AppendLine($" Type:{multinames.IndexOf(p.Type)}\t[{p.Type}]");
                    sb.AppendLine($" ValueKind:{p.ValueKind}");
                    //if (p.Value == null)
                    //{
                    //    sb.AppendLine($" Value:null");
                    //}
                    //else
                    //{
                    //    sb.AppendLine($" Value:AS3Expression,{AS3SrcFile._expressions.IndexOf((AS3Expression)p.Value)}");
                    //}
                    if (p.ValueExprIndex == -1)
                    {
                        sb.AppendLine($" Value:null");
                    }
                    else
                    {
                        sb.AppendLine($" Value:AS3Expression,{p.ValueExprIndex}");
                    }
                }

                sb.AppendLine($"ReturnType:{multinames.IndexOf(method.ReturnType)}\t[{method.ReturnType}]");

                if (method.Trait != null)
                {
                    sb.AppendLine($"HasTrait:True");
                }
                else
                {
                    sb.AppendLine($"HasTrait:False");
                }

                WriteContainer(sb, method.Body.Traits);

                sb.AppendLine();
            }

            sb.AppendLine($"Classes\t{scriptClasses.Count - 1}");
            for (int i = 1; i < scriptClasses.Count; i++)
            {
                var cls = scriptClasses[i];
                sb.AppendLine($"QName:{multinames.IndexOf(cls.QName)}\t[{cls.QName}]");
                sb.AppendLine($"Constructor:{scriptMethods.IndexOf(cls.Constructor)}");
                sb.AppendLine($"TypeId:{cls.Type_identifier}");
                WriteContainer(sb, cls.Traits);

                var instance = cls.Instance;
                sb.AppendLine($"Flags:{instance.Flags}");
                sb.AppendLine($"Constructor:{scriptMethods.IndexOf(instance.Constructor)}");
                sb.AppendLine($"IsInterface:{instance.IsInterface}");
                sb.AppendLine($"Interfaces:{instance.Interfaces.Count}");
                for (int j = 0; j < instance.Interfaces.Count; j++)
                {
                    var iface = instance.Interfaces[j];
                    sb.AppendLine($"{multinames.IndexOf(iface)}\t[{iface}]");
                }
                sb.AppendLine($"Super:{multinames.IndexOf(instance.Super)}\t[{instance.Super}]");
                sb.AppendLine($"ProtectedNamespace:{namespaces.IndexOf(instance.ProtectedNamespace)}\t[{instance.ProtectedNamespace}]");
                WriteContainer(sb, instance.Traits);


                sb.AppendLine();
            }

            WriteContainer(sb, Script.Traits);

            return sb.ToString();
        }

        private void WriteContainer(StringBuilder sb, List<ASTrait> traitsList)
        {
            sb.AppendLine($"Traits\t{traitsList.Count}");
            for (int i = 0; i < traitsList.Count; i++)
            {
                var t = traitsList[i];
                sb.AppendLine($"Kind:{t.Kind}");
                sb.AppendLine($"Metas\t{t.ASMetadata.Count}");
                for (int j = 0; j < t.ASMetadata.Count; j++)
                {
                    var meta = t.ASMetadata[j];
                    sb.Append($"[{meta.Name}(");
                    for (int k = 0; k < meta.Items.Count; k++)
                    {
                        var metaitem = meta.Items[k];
                        if (string.IsNullOrEmpty(metaitem.Name))
                        {
                            sb.Append($"{metaitem.Value}");
                        }
                        else
                        {
                            sb.Append($"{metaitem.Name}={metaitem.Value}");
                        }
                        if (k < meta.Items.Count - 1)
                        {
                            sb.Append(',');
                        }
                    }
                    sb.AppendLine(")]");
                }

                sb.AppendLine($"Attributes:{t.Attributes}");
                sb.AppendLine($"IsStatic:{t.IsStatic}");
                sb.AppendLine($"QName:{multinames.IndexOf(t.QName)}\t[{t.QName}]");
                sb.AppendLine($"Type:{multinames.IndexOf(t.Type)}\t[{t.Type}]");
                sb.AppendLine($"ValueKind:{t.ValueKind}");
                sb.AppendLine($"TypeKind:{t.TypeKind}");

                //t.Value
                if (t.Value == null)
                {
                    sb.AppendLine($"Value:null");
                }
                else if (t.Value.ValueType == ASTrait.TraitValueType.NameSpace)
                {
                    sb.AppendLine($"Value:NameSpace,{namespaces.IndexOf(t.Value.Namespace)}\t[{t.Value}]");
                }
                else if (t.Value.ValueType == ASTrait.TraitValueType.AS3Function)
                {
                    sb.AppendLine($"Value:AS3Function,{ t.Value.FunctionOrExpression_Index }");
                }
                else if (t.Value.ValueType == ASTrait.TraitValueType.AS3Expression)
                {
                    sb.AppendLine($"Value:AS3Expression,{t.Value.FunctionOrExpression_Index}");
                }
                else
                {
                    throw new NotImplementedException();
                }

                sb.AppendLine($"Class:{scriptClasses.IndexOf(t.Class)}");
                sb.AppendLine($"Function:{scriptMethods.IndexOf(t.Function)}");
                sb.AppendLine($"Method:{scriptMethods.IndexOf(t.Method)}");



                sb.AppendLine();
            }
        }

        private void WriteContainer(BinaryWriter bw, List<ASTrait> traitsList)
        {
            bw.Write(traitsList.Count);
            for (int i = 0; i < traitsList.Count; i++)
            {
                var t = traitsList[i];

                bw.Write(((Token)t.Token).ptr);
                bw.Write(((Token)t.Token).line);

                bw.Write((byte)t.Kind);
                bw.Write(t.ASMetadata.Count);
                for (int j = 0; j < t.ASMetadata.Count; j++)
                {
                    var meta = t.ASMetadata[j];
                    bw.Write(meta.Name);
                    bw.Write(meta.Items.Count);
                    for (int k = 0; k < meta.Items.Count; k++)
                    {
                        WriteStringWithNull(bw, meta.Items[k].Name);
                        WriteStringWithNull(bw, meta.Items[k].Value);
                    }
                }

                bw.Write((uint)t.Attributes);
                bw.Write(t.IsStatic);
                bw.Write(multinames.IndexOf(t.QName));
                bw.Write(multinames.IndexOf(t.Type));
                bw.Write((byte)t.ValueKind);
                if (t.Value == null)
                {
                    bw.Write(false);
                }
                else
                {
                    bw.Write(true);
                    if (t.Value.ValueType == ASTrait.TraitValueType.NameSpace)
                    {
                        bw.Write((byte)0);
                        bw.Write(namespaces.IndexOf(t.Value.Namespace));
                    }
                    else if (t.Value.ValueType == ASTrait.TraitValueType.AS3Function)
                    {
                        bw.Write((byte)1);
                        bw.Write(t.Value.FunctionOrExpression_Index);
                    }
                    else if (t.Value.ValueType == ASTrait.TraitValueType.AS3Expression)
                    {
                        bw.Write((byte)2);
                        bw.Write(t.Value.FunctionOrExpression_Index);
                    }
                }

                bw.Write((ulong)t.TypeKind);

                bw.Write(scriptClasses.IndexOf(t.Class));
                bw.Write(scriptMethods.IndexOf(t.Function));
                bw.Write(scriptMethods.IndexOf(t.Method));
            }

        }


        private void WriteStringWithNull(BinaryWriter bw, string str)
        {
            if (str == null)
            {
                bw.Write(false);
            }
            else
            {
                bw.Write(true);
                bw.Write(str);
            }

        }
        private byte[] WriteByte()
        {
            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            {
                using (System.IO.BinaryWriter bw = new System.IO.BinaryWriter(ms))
                {
                    var thisassembly = typeof(ScriptDef).Assembly;
                    Version ver = thisassembly.GetName().Version;

                    bw.Write(ver.Major);
                    bw.Write(ver.Minor);
                    bw.Write(ver.Build);
                    bw.Write(ver.Revision);

                    //bw.Write(AS3SrcFile.key.P1);
                    //bw.Write(AS3SrcFile.key.P2);

                    WriteStringWithNull(bw,sourceFile);
                    WriteStringWithNull(bw,fullPath);
                    
                    bw.Write(namespaces.Count - 1);
                    for (int i = 1; i < namespaces.Count; i++)
                    {
                        bw.Write((byte)namespaces[i].Kind);
                        bw.Write(namespaces[i].Name);
                        WriteStringWithNull(bw,namespaces[i].in_package);
                        WriteStringWithNull(bw, namespaces[i].def_uri);
                    }

                    bw.Write(namespaceSets.Count - 1);
                    for (int i = 1; i < namespaceSets.Count; i++)
                    {
                        var ns = namespaceSets[i];
                        bw.Write(ns.Namespaces.Count);
                        for (int j = 0; j < ns.Namespaces.Count; j++)
                        {
                            bw.Write(namespaces.IndexOf(ns.Namespaces[j]));
                        }
                    }

                    bw.Write(multinames.Count - 1);
                    for (int i = 1; i < multinames.Count; i++)
                    {
                        var m = multinames[i];
                        bw.Write((byte)m.Kind);
                        WriteStringWithNull(bw, m.Name);
                        bw.Write(namespaces.IndexOf(m.Namespace));
                        bw.Write(multinames.IndexOf(m.QName));
                        if (m.Types == null)
                        {
                            bw.Write(0);
                        }
                        else
                        {
                            bw.Write(m.Types.Count);
                            for (int j = 0; j < m.Types.Count; j++)
                            {
                                bw.Write(multinames.IndexOf(m.Types[j]));
                            }
                        }
                        bw.Write(namespaceSets.IndexOf(m.NamespaceSet));
                    }

                    bw.Write(package_imports.Count);
                    for (int i = 0; i < package_imports.Count; i++)
                    {
                        bw.Write(package_imports[i]);
                    }
                    bw.Write(script_imports.Count);
                    for (int i = 0; i < script_imports.Count; i++)
                    {
                        bw.Write(script_imports[i]);
                    }

                    //先写入containers.Count
                    bw.Write(containers.Count);
                    //再写入methods.Count
                    bw.Write(scriptMethods.Count - 1);

                    for (int i = 1; i < scriptMethods.Count; i++)
                    {
                        var as3fun = scriptMethods[i].ast_function_index; //ast_functions[i];

                        bw.Write(as3fun);

                        var method = scriptMethods[i];

                        if (as3fun > -1)
                        {
                            bw.Write(((Token)method.Token).ptr);
                            bw.Write(((Token)method.Token).line);

                        }
                        else if (as3fun == -2)
                        {
							bw.Write(((Token)method.Token).ptr);
							bw.Write(((Token)method.Token).line);
						}


                        var usenamespace = method_use_namesapce[i];
                        bw.Write(usenamespace.Count);
                        for (int j = 0; j < usenamespace.Count; j++)
                        {
                            bw.Write(usenamespace[j].Item1);
                            bw.Write(usenamespace[j].Item2.ptr);
                            bw.Write(usenamespace[j].Item2.line);
                        }


                        bw.Write(containers.IndexOf(method.Container));
                        bw.Write((uint)method.Flags);
                        WriteStringWithNull(bw, method.Name);
                        bw.Write(method.IsAnonymous);
                        bw.Write(method.IsConstructor);
                        bw.Write(method.Parameters.Count);
                        for (int j = 0; j < method.Parameters.Count; j++)
                        {
                            var p = method.Parameters[j];
                            WriteStringWithNull(bw, p.Name);
                            bw.Write(p.IsOptional);
                            bw.Write(p.IsRest);
                            bw.Write(multinames.IndexOf(p.Type));
                            bw.Write((ulong)p.TypeKind);
                            bw.Write((byte)p.ValueKind);
                            //if (p.Value == null)
                            //{
                            //    bw.Write(false);
                            //}
                            //else
                            //{
                            //    bw.Write(true);
                            //    bw.Write(AS3SrcFile._expressions.IndexOf((AS3Expression)p.Value));
                            //}

                            bw.Write(p.ValueExprIndex);

                        }

                        bw.Write(multinames.IndexOf(method.ReturnType));
                        bw.Write((ulong)method.ReturnTypeKind);
                        if (method.Trait == null)
                        {
                            bw.Write(false);
                        }
                        else
                        {
                            bw.Write(true);
                        }

                        bw.Write(containers.IndexOf(method.Body));
                        bw.Write( method.Body.NamespaceSetIndex );
                        //WriteContainer(bw, method.Body.Traits);
                    }

                    bw.Write(scriptClasses.Count - 1);
                    for (int i = 1; i < scriptClasses.Count; i++)
                    {
                        var cls = scriptClasses[i];
                        bw.Write(((Token)cls.Token).ptr);
                        bw.Write(((Token)cls.Token).line);

                        bw.Write(cls.Type_identifier);


                        bw.Write(scriptMethods.IndexOf(cls.Constructor));
                        //WriteContainer(bw, cls.Traits);
                        bw.Write(containers.IndexOf(cls));
                        
                        var instance = cls.Instance;
                        bw.Write(multinames.IndexOf(instance.QName));
                        bw.Write((uint)instance.Flags);
                        bw.Write(scriptMethods.IndexOf(instance.Constructor));
                        bw.Write(instance.Interfaces.Count);
                        for (int j = 0; j < instance.Interfaces.Count; j++)
                        {
                            var iface = instance.Interfaces[j];
                            bw.Write(multinames.IndexOf(iface));
                        }
                        bw.Write(multinames.IndexOf(instance.Super));
                        bw.Write(namespaces.IndexOf(instance.ProtectedNamespace));
                        bw.Write(containers.IndexOf(instance));
                    }

                    bw.Write(scriptMethods.IndexOf(Script.Initializer));
                    bw.Write(containers.IndexOf(Script));
                    

                    //最后写containers的内容
                    for (int i = 0; i < containers.Count; i++)
                    {
                        WriteContainer(bw, containers[i].Traits);
                    }

                }


                return ms.ToArray();
            }
        }


        public string Encode()
        {
            var data = WriteByte();
            return System.Convert.ToBase64String(data);

            //Console.WriteLine(System.Convert.ToBase64String(data));
            //Console.WriteLine(WriteString());

            
            //ScriptDef testread;
            //Read(Convert.ToBase64String(data), AS3SrcFile ,out testread);

            //string str1 = WriteString();
            //string str2 = testread.WriteString();

            //if (str1 != str2)
            //{ 
                
            //}    
        }

        public void Serialize(StringWriter sw)
        { 
            sw.WriteLine(Encode());
            sw.WriteLine(WriteString());
        }

        public static ReadResult Deserialize(StringReader reader, AS3SrcFile as3, out ScriptDef script)
        {
            string decodeStr = reader.ReadLine();
            return Decode(decodeStr, as3, out script);
        }


        public enum ReadResult
        {

            Success,
            Expired

        }

        private static string ReadStringWithNull(BinaryReader br)
        {
            bool hasvalue = br.ReadBoolean();
            if (hasvalue)
            {
                return br.ReadString();
            }
            else
            {
                return null;
            }

        }
        public static ReadResult Decode(string encodeData, AS3SrcFile as3, out ScriptDef script)
        {
            var data = Convert.FromBase64String(encodeData);
            using (System.IO.MemoryStream ms = new MemoryStream(data, false))
            {
                using (BinaryReader br = new BinaryReader(ms))
                {
                    var thisassembly = typeof(ScriptDef).Assembly;
                    Version ver = thisassembly.GetName().Version;

                    int maj = br.ReadInt32();
                    int min = br.ReadInt32();
                    int build = br.ReadInt32();
                    int rev = br.ReadInt32();

                    if (ver.Major < maj)
                    {
                        script = null;
                        return ReadResult.Expired;
                    }

                    

                    //ulong p1 = br.ReadUInt64();
                    //ulong p2 = br.ReadUInt64(); 

                    //MD5Result key = new MD5Result(p1,p2);
                    //if (as3 != null)
                    //{
                    //    if (!key.Equals( as3.key))
                    //    {
                    //        script = null;
                    //        return ReadResult.Expired;
                    //    }
                    //}



                    script = new ScriptDef();
                    script.AS3SrcFile = as3;

                    script.sourceFile = ReadStringWithNull(br);
                    script.fullPath = ReadStringWithNull(br);

                    if (as3 != null)
                    {
                        if (as3.sourceFile != script.sourceFile || as3.sourceFileFullpath != script.fullPath)
                        {
                           
                            script = null;
                            return ReadResult.Expired;
                        }
                    }


                    script.namespaces = new List<ASNamespace>();
                    script.namespaces.Add(null);
                    int nsCount = br.ReadInt32();
                    for (int i = 0; i < nsCount; i++)
                    {
                        ASNamespace ns = new ASNamespace();
                        ns.Kind = (NamespaceKind)br.ReadByte();
                        ns.Name = br.ReadString();
                        ns.in_package = ReadStringWithNull(br);
                        ns.def_uri = ReadStringWithNull(br);

                        script.namespaces.Add(ns);
                    }

                    script.namespaceSets = new List<ASNamespaceSet>();
                    script.namespaceSets.Add(null);
                    int nsSetCount = br.ReadInt32();
                    for (int i = 0; i < nsSetCount; i++)
                    {
                        ASNamespaceSet namespaceSet = new ASNamespaceSet();
                        int c = br.ReadInt32();
                        for (int j = 0; j < c; j++)
                        {
                            int idx = br.ReadInt32();
                            namespaceSet.Namespaces.Add(script.namespaces[idx]);
                        }

                        script.namespaceSets.Add(namespaceSet);
                    }

                    script.multinames = new List<ASMultiname>();
                    script.multinames.Add(null);
                    int mnCount = br.ReadInt32();
                    for (int i = 0; i < mnCount; i++)
                    {
                        ASMultiname multiname = new ASMultiname();
                        script.multinames.Add(multiname);
                    }
                    for (int i = 0; i < mnCount; i++)
                    {
                        ASMultiname multiname = script.multinames[i + 1];
                        multiname.Kind = (MultinameKind)br.ReadByte();
                        multiname.Name = ReadStringWithNull(br);
                        multiname.Namespace = script.namespaces[br.ReadInt32()];
                        multiname.QName = script.multinames[br.ReadInt32()];

                        int types = br.ReadInt32();

                        if (types == 0)
                        {
                            multiname.Types = null;
                        }
                        else
                        {
                            multiname.Types = new List<ASMultiname>();
                            for (int j = 0; j < types; j++)
                            {
                                multiname.Types.Add(script.multinames[br.ReadInt32()]);
                            }
                        }
                        multiname.NamespaceSet = script.namespaceSets[br.ReadInt32()];
                    }

                    script.package_imports = new List<string>();
                    int pc = br.ReadInt32();
                    for (int i = 0; i < pc; i++)
                    {
                        script.package_imports.Add(br.ReadString());
                    }
                    script.script_imports = new List<string>();
                    int sc = br.ReadInt32();
                    for (int i = 0; i < sc; i++)
                    {
                        script.script_imports.Add(br.ReadString());
                    }

                    int containers = br.ReadInt32();
                    int methods = br.ReadInt32();

                    script.containers = new List<ASContainer>();
                    for (int i = 0; i < containers; i++)
                    {
                        script.containers.Add(null);
                    }

                    script.scriptMethods = new List<ASMethod>();
                    script.scriptMethods.Add(null);

                    //script.ast_functions = new List<int>();
                    //script.ast_functions.Add(-1);

                    script.method_use_namesapce = new List<List<Tuple<string, Token>>>();
                    script.method_use_namesapce.Add(null);

                    List<int> methodcontainerindex = new List<int>();
                    List<bool> methodhastraits = new List<bool>();

                    for (int i = 0; i < methods; i++)
                    {
                        //var as3fun = ast_functions[i];
                        //bw.Write(AS3SrcFile._functions.IndexOf(as3fun));

                        int as3function = br.ReadInt32();
                        //script.ast_functions.Add(as3function);

                        Token token = null;
                        if (as3function > -1 || as3function == -2)
                        {
                            int ptr = br.ReadInt32();
                            int line = br.ReadInt32();
                            token=new Token();
                            token.ptr = ptr;
                            token.line = line;
                            token.sourceFile = script.sourceFile;
                            token.sourceFileFullPath = script.fullPath;
                        }

                        List<Tuple<string, Token>> use = new List<Tuple<string, Token>>();
                        var use_count = br.ReadInt32();
                        for (int j = 0; j < use_count; j++)
                        {
                            string ns = br.ReadString();
                            int ptr = br.ReadInt32();
                            int line = br.ReadInt32();
                            var tk = new Token();
                            tk.ptr = ptr;
                            tk.line = line;
                            tk.sourceFile = script.sourceFile;
                            tk.sourceFileFullPath = script.fullPath;

                            use.Add(new Tuple<string, Token>(ns, tk));
                        }

                        script.method_use_namesapce.Add(use);

                        ASMethod method = new ASMethod(null,token);
                        script.scriptMethods.Add(method);

                        method.ast_function_index = as3function;

                        methodcontainerindex.Add(br.ReadInt32());
                        method.Flags = (MethodFlags)br.ReadUInt32();
                        method.Name = ReadStringWithNull(br);
                        method.IsAnonymous = br.ReadBoolean();
                        method.IsConstructor = br.ReadBoolean();
                        int paramcount = br.ReadInt32();
                        for (int j = 0; j < paramcount; j++)
                        {
                            ASParameter parameter = new ASParameter(method);
                            parameter.Name = ReadStringWithNull(br);
                            parameter.IsOptional = br.ReadBoolean();
                            parameter.IsRest = br.ReadBoolean();
                            parameter.Type = script.multinames[br.ReadInt32()];
                            parameter.TypeKind = (TypeKind)br.ReadUInt64();
                            parameter.ValueKind = (ConstantKind)br.ReadByte();
                            //if (br.ReadBoolean())
                            //{
                            //    int expressionindex = br.ReadInt32();

                            //    if (as3 != null)
                            //    { 
                            //        parameter.Value = as3._expressions[expressionindex];
                            //    }

                            //}
                            //else
                            //{
                            //    parameter.Value = null;
                            //}
                            parameter.ValueExprIndex = br.ReadInt32();

                            method.Parameters.Add(parameter);
                        }
                        method.ReturnType = script.multinames[br.ReadInt32()];
                        method.ReturnTypeKind = (TypeKind)br.ReadUInt64();

                        methodhastraits.Add(br.ReadBoolean());
                       
                        int bodyindex = br.ReadInt32();

                        if (script.containers[bodyindex] != null)
                        {
                            throw new InvalidOperationException();
                        }

                        int nsindex = br.ReadInt32();

                        script.containers[bodyindex] = new ASMethodBody(method) { NamespaceSetIndex = nsindex };
                        method.Body = (ASMethodBody)script.containers[bodyindex];
                       
                    }

                    int clsCount = br.ReadInt32();
                    script.scriptClasses = new List<ASClass>();
                    script.scriptClasses.Add(null);
                    for (int i = 0; i < clsCount; i++)
                    {
                        //bw.Write(cls.Token.ptr);
                        //bw.Write(cls.Token.line);
                        int ptr = br.ReadInt32();
                        int line = br.ReadInt32();
                        Token token = new Token();
                        token.ptr=ptr;
                        token.line=line;
                        token.sourceFile = script.sourceFile;
                        token.sourceFileFullPath =script.fullPath;

                        ulong type_id = br.ReadUInt64();

                        ASClass cls = new ASClass(token,type_id);
                        script.scriptClasses.Add(cls);
                        cls.Constructor = script.scriptMethods[br.ReadInt32()];

                        int containeridx = br.ReadInt32();
                        if (script.containers[containeridx] != null)
                        {
                            throw new InvalidOperationException();
                        }
                        script.containers[containeridx ] = cls;

                        cls.Instance = new ASInstance(script.multinames[br.ReadInt32()]);
                        cls.Instance.Flags = (ClassFlags)br.ReadUInt32();
                        cls.Instance.Constructor = script.scriptMethods[br.ReadInt32()];
                        var infcount = br.ReadInt32();
                        for (int j = 0; j < infcount; j++)
                        {
                            cls.Instance.Interfaces.Add(script.multinames[br.ReadInt32()]);
                        }
                        cls.Instance.Super = script.multinames[br.ReadInt32()];
                        cls.Instance.ProtectedNamespace = script.namespaces[br.ReadInt32()];
                        
                        containeridx = br.ReadInt32();
                        if (script.containers[containeridx] != null)
                        {
                            throw new InvalidOperationException();
                        }
                        script.containers[containeridx] = cls.Instance;
                    }

                    script.Script = new ASScript();
                    script.Script.Initializer = script.scriptMethods[br.ReadInt32()];
                    int cidx = br.ReadInt32();
                    if (script.containers[cidx] != null)
                    {
                        throw new InvalidOperationException();
                    }
                    script.containers[cidx] = script.Script;
                    script.Script.allContainers = script.containers;


                    for (int i = 0; i < containers; i++)
                    {
                        if (script.containers[i] == null)
                        {
                            throw new InvalidOperationException();
                        }

                        int ts = br.ReadInt32();
                        for (int j = 0; j < ts; j++)
                        {
                            int ptr = br.ReadInt32();
                            int line = br.ReadInt32();
                            Token token = new Token();
                            token.ptr = ptr;
                            token.line = line;
                            token.sourceFile = script.sourceFile;
                            token.sourceFileFullPath = script.fullPath;

                            ASTrait trait = new ASTrait(token);
                            script.containers[i].Traits.Add(trait);
                            trait.Kind = (TraitKind)br.ReadByte();
                            int metacount = br.ReadInt32();
                            for (int k = 0; k < metacount; k++)
                            {
                                ASMeta meta = new ASMeta();
                                meta.Name = br.ReadString();
                                int count = br.ReadInt32();
                                for (int l = 0; l < count; l++)
                                {
                                    ASMetaItem item = new ASMetaItem();
                                    item.Name = ReadStringWithNull(br);
                                    item.Value = ReadStringWithNull(br);
                                    meta.Items.Add(item);
                                }
                                trait.ASMetadata.Add(meta);
                            }
                            trait.Attributes = (TraitAttributes)br.ReadUInt32();
                            trait.IsStatic = br.ReadBoolean();
                            trait.QName = script.multinames[br.ReadInt32()];
                            trait.Type = script.multinames[br.ReadInt32()];
                            trait.ValueKind = (ConstantKind)br.ReadByte();
                            if (br.ReadBoolean())
                            {
                                byte type = br.ReadByte();
                                if (type == 0)
                                {
                                    trait.Value =new ASTrait.TraitValue( script.namespaces[br.ReadInt32()]);
                                }
                                else if (type == 1)
                                {
                                    int functionindex = br.ReadInt32();
                                    //if (as3 != null)
                                    //{
                                        trait.Value = new ASTrait.TraitValue(ASTrait.TraitValueType.AS3Function, functionindex);  //as3._functions[functionindex];
                                    //}

                                }
                                else if (type == 2)
                                { 
                                    int expressionindex = br.ReadInt32();
                                    //if (as3 != null)
                                    //{
                                        trait.Value = new ASTrait.TraitValue( ASTrait.TraitValueType.AS3Expression, expressionindex );// as3._expressions[expressionindex];
                                    //}
                                }
                            }
                            else
                            { 
                                trait.Value = null;
                            }

                            trait.TypeKind = (TypeKind)br.ReadUInt64();

                            trait.Class = script.scriptClasses[br.ReadInt32()];

                            int findex = br.ReadInt32();
                            if (findex > 0)
                            {
                                bool hastrait = methodhastraits[findex - 1];
                            }
                            trait.Function = script.scriptMethods[findex];

                            int mindex = br.ReadInt32();
                            if (mindex > 0)
                            {
                                bool hastrait = methodhastraits[mindex - 1];
                                if (!hastrait)
                                {
                                    throw new InvalidOperationException();
                                }

                                script.scriptMethods[mindex].Trait = trait;

                            }
                            trait.Method = script.scriptMethods[mindex];



                        }


                    }

                    for (int i = 0; i < methodhastraits.Count; i++)
                    {
                        if (methodhastraits[i])
                        {
                            if (script.scriptMethods[i + 1].Trait == null)
                            {
                                throw new InvalidOperationException();
                            }
                        }
                        else
                        {
                            if (script.scriptMethods[i + 1].Trait != null)
                            {
                                throw new InvalidOperationException();
                            }
                        }
                    }

                    for (int i = 0; i < methods; i++)
                    {
                        int containerindex = methodcontainerindex[i];
                        if (containerindex>=0)
                        {
                            script.scriptMethods[i + 1].SetContainer(script.containers[methodcontainerindex[i]]);
                        }
                       
                    }

                    return ReadResult.Success;
                }
            }

        }



    }
}
