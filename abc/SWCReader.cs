using juicescript.ABC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace juicescript
{
    public class SWCReader
    {
        public static SWCFile Read(System.IO.Stream stm )
        {
            SWCFile file = new SWCFile();
            List<string> _stringPool_ = new List<string>();
            _stringPool_.Add(null);
            ASNamespace[] namespaces = null;
            ASNamespaceSet[] namespacesSets = null;
            ASMultiname[] multinames = null;
            ASContainer[] containers = null;
            ASMethod[] scriptMethods = null;
            ASClass[] scriptClasses = null;
            ASScript[] scripts = null;
            ASTrait[] aSTraits = null;

            ASVector[] aSVectors = null;

            List<ulong> ld_cls = new List<ulong>();
            List<Tuple<ulong, int>> ld_supermethods = new List<Tuple<ulong, int>>(); 

            Dictionary<ASScript,List<int>> _script_pkg_imps=new Dictionary<ASScript,List<int>>();
            Dictionary<ASScript,List<int>> _script_s_imps=new Dictionary<ASScript,List<int>>();

            using (System.IO.BinaryReader br = new BinaryReader(stm))
            { 
                file.Major = br.ReadInt32();
                file.Minor = br.ReadInt32();
                file.Build = br.ReadInt32();
                file.Revision = br.ReadInt32();

                file.assemblyName = br.ReadString();
                int refCount = br.ReadInt32();
                for (int i = 0; i < refCount; i++)
                {
                    file.refAssemblys.Add(br.ReadString());
                }


                int strCount = br.ReadInt32();
                for (int i = 0; i < strCount; i++)
                {
                    int chars = br.ReadInt32();
                    Memory<char> str = new Memory<char>(new char[chars]);
                    for (int j = 0; j < chars; j++)
                    {
                        char c = (char)br.ReadUInt16();

                        str.Span[j] = c;
                    }
                    _stringPool_.Add(str.ToString());
                }

                int ld_cls_count = br.ReadInt32();
                for (int i = 0; i < ld_cls_count; i++)
                {
                    ld_cls.Add(br.ReadUInt64());
                }

                int ld_supermethod_count = br.ReadInt32();
                for (int i = 0; i < ld_supermethod_count; i++)
                {
                    ulong type = br.ReadUInt64();
                    int   vtable_idx = br.ReadInt32();
                    ld_supermethods.Add( new Tuple<ulong, int>(type,vtable_idx) );
                }


                int vectors_count = br.ReadInt32();
                aSVectors = new ASVector[vectors_count];
                for (int i = 0; i < vectors_count; i++)
                {
                    //bw.Write(def.depth);
                    //bw.Write((ulong)def.Identifier);
                    //bw.Write((ulong)def.ElementTypeId);
                    //bw.Write(_stringPool_.IndexOf(def.ToString()));

                    int depth = br.ReadInt32();
                    ulong identifier = br.ReadUInt64();
                    ulong element = br.ReadUInt64();
                    string str = _stringPool_[br.ReadInt32()];


                    aSVectors[i] = new ASVector()
                    {
                        depth = depth,
                        Str = str,
                        Identifier = (TypeKind)identifier,
                        ElementType = (TypeKind)element,
                        vector_class = null,
                        vScript = null,
                        
                    };

                }


                int namespaceCount = br.ReadInt32()+1;
                namespaces=new ASNamespace[namespaceCount];
                for (int i = 1; i < namespaceCount; i++)
                {
                    ASNamespace ns = new ASNamespace();
                    ns.Kind = (NamespaceKind)br.ReadByte();
                    ns.Name = _stringPool_[br.ReadInt32()];
                    ns.in_package = _stringPool_[br.ReadInt32()];
                    ns.def_uri = _stringPool_[br.ReadInt32()];
                    namespaces[i] = ns;
                }

                int namespaceSetCount = br.ReadInt32()+1;
                namespacesSets = new ASNamespaceSet[namespaceSetCount];
                for (int i = 1; i < namespaceSetCount; i++)
                {
                    ASNamespaceSet set = new ASNamespaceSet();
                    set.Namespaces = new List<ASNamespace>();
                    int c = br.ReadInt32();
                    for (int j = 0; j < c; j++)
                    {
                        set.Namespaces.Add(namespaces[br.ReadInt32()]);
                    }

                    namespacesSets[i] = set;

                }

                int multinameCount = br.ReadInt32()+1;
                multinames = new ASMultiname[multinameCount];
                for (int i = 1; i < multinameCount; i++)
                {
                    multinames[i] = new ASMultiname();
                }

                for (int i = 1; i < multinameCount; i++)
                {
                    ASMultiname multiname = multinames[i];
                    multiname.Kind = (MultinameKind)br.ReadByte();
                    multiname.Name = _stringPool_[br.ReadInt32()];
                    multiname.Namespace = namespaces[br.ReadInt32()];
                    multiname.QName = multinames[br.ReadInt32()];

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
                            multiname.Types.Add(multinames[br.ReadInt32()]);
                        }
                    }
                    multiname.NamespaceSet = namespacesSets[br.ReadInt32()];
                }

               
                containers= new ASContainer[br.ReadInt32()];
                int methods = br.ReadInt32();
                scriptMethods = new ASMethod[methods];

                List<int> methodcontainerindex = new List<int>();
                List<bool> methodhastraits = new List<bool>();

                for (int i = 0; i < methods-1; i++)
                {
                    int ast_index = br.ReadInt32();
                    Token token = null;
                    if (ast_index > -1)
                    {
                        int ptr = br.ReadInt32();
                        int line = br.ReadInt32();

                        var sourceFile = _stringPool_[br.ReadInt32()];
                        var sourceFileFullPath = _stringPool_[br.ReadInt32()];

                        //token = //makeToken(ptr,line,sourceFile,sourceFileFullPath);
                        token = new Token()
                        {
                             ptr= ptr,
                             line= line,
                             sourceFile= sourceFile,
                             sourceFileFullPath= sourceFileFullPath,
                        };

                    }

                    ASMethod method = new ASMethod(null,token);
                    scriptMethods[i+1] = method;

                    methodcontainerindex.Add(br.ReadInt32());
                    method.Flags = (MethodFlags)br.ReadUInt32();
                    method.Name = _stringPool_[br.ReadInt32()];
                    method.IsAnonymous = br.ReadBoolean();
                    method.IsConstructor = br.ReadBoolean();

                    byte[] param_dv = null;
                    { 
                        int l = br.ReadInt32();
                        if (l > 0)
                        {
							param_dv = br.ReadBytes(l);
                        }
                    }
                    
                    int paramcount = br.ReadInt32();
                    for (int j = 0; j < paramcount; j++)
                    {
                        ASParameter parameter = new ASParameter(method);
                        parameter.Name = _stringPool_[br.ReadInt32()];
                        parameter.IsOptional = br.ReadBoolean();
                        parameter.IsRest = br.ReadBoolean();
                        parameter.Type = multinames[br.ReadInt32()];
                        parameter.TypeKind = (TypeKind)br.ReadUInt64();
                        parameter.ValueKind = (ConstantKind)br.ReadByte();
                        
                        
                        parameter.ValueExprIndex = br.ReadInt32();

                        method.Parameters.Add(parameter);
                    }

                    method.ReturnType = multinames[br.ReadInt32()];
                    method.ReturnTypeKind = (TypeKind)br.ReadUInt64();
                    methodhastraits.Add(br.ReadBoolean());

                    int bodyindex = br.ReadInt32();

                    if (containers[bodyindex] != null)
                    {
                        throw new InvalidOperationException();
                    }

                    int bytes = br.ReadInt32();
                    byte[] byteCode = br.ReadBytes(bytes);  

                    var nsSetIndex = br.ReadInt32();

                    containers[bodyindex] = new ASMethodBody(method) { NamespaceSetIndex = nsSetIndex };
                    method.Body = (ASMethodBody)containers[bodyindex];
                    method.Body.ByteCode = byteCode;
                    method.Body.param_defaultvalues = param_dv;
                }

                int clsCount = br.ReadInt32();
                scriptClasses = new ASClass[clsCount];
                
                for (int i = 0; i < clsCount-1; i++)
                {

                    int ptr = br.ReadInt32();
                    int line = br.ReadInt32();

                    var sourceFile = _stringPool_[br.ReadInt32()];
                    var sourceFileFullPath = _stringPool_[br.ReadInt32()];

                    ulong type_id = br.ReadUInt64();

                    ASClass cls = new ASClass(new Token()
                    {
                        ptr = ptr,
                        line = line,
                        sourceFile = sourceFile,
                        sourceFileFullPath = sourceFileFullPath,
                    }, type_id);
                    scriptClasses[i+1] = cls;
                    cls.Constructor = scriptMethods[br.ReadInt32()];

                    int containeridx = br.ReadInt32();
                    if (containers[containeridx] != null)
                    {
                        throw new InvalidOperationException();
                    }
                    containers[containeridx] = cls;

                    cls.Instance = new ASInstance(multinames[br.ReadInt32()]);
                    cls.Instance.Flags = (ClassFlags)br.ReadUInt32();
                    cls.Instance.Constructor = scriptMethods[br.ReadInt32()];
                    var infcount = br.ReadInt32();
                    for (int j = 0; j < infcount; j++)
                    {
                        cls.Instance.Interfaces.Add(multinames[br.ReadInt32()]);
                    }
                    cls.Instance.Super = multinames[br.ReadInt32()];
                    cls.Instance.ProtectedNamespace = namespaces[br.ReadInt32()];

                    containeridx = br.ReadInt32();
                    if (containers[containeridx] != null)
                    {
                        throw new InvalidOperationException();
                    }
                    containers[containeridx] = cls.Instance;
                }

                int _container_index = 0;

                int scriptCount = br.ReadInt32();
                scripts = new ASScript[scriptCount];
                for (int i = 0; i < scriptCount; i++)
                {
                    var script = new ASScript();

                    int containerCount = br.ReadInt32();

                    script.Initializer = scriptMethods[br.ReadInt32()];

                    int cidx = br.ReadInt32();
                    if (containers[cidx] != null)
                    {
                        throw new InvalidOperationException();
                    }

                    _script_pkg_imps.Add(script, new List<int>());
                    int imps = br.ReadInt32();
                    for (int j = 0; j < imps; j++)
                    {
                        _script_pkg_imps[script].Add(br.ReadInt32());
                    }

                    _script_s_imps.Add(script, new List<int>());
                    imps = br.ReadInt32();
                    for (int j = 0; j < imps; j++)
                    {
                        _script_s_imps[script].Add(br.ReadInt32());
                    }

                    containers[cidx] = script;
                    scripts[i] = script;

                    script.allContainers = new List<ASContainer>();
                    for (int k = 0; k < containerCount; k++)
                    {
                        if(containers[_container_index + k] == null)
                            throw new InvalidOperationException();

                        script.allContainers.Add(containers[ _container_index + k ]);
                    }

                    _container_index += containerCount;
                }

                int traitcount = br.ReadInt32();
                aSTraits = new ASTrait[traitcount];
                for (int i = 0; i < traitcount; i++)
                {
                    int ptr = br.ReadInt32();
                    int line = br.ReadInt32();
                    string path = _stringPool_[ br.ReadInt32()];
                    string fullpath = _stringPool_[br.ReadInt32()];

                    ASTrait trait = new ASTrait(new Token()
                    {
                        ptr = ptr,
                        line = line,
                        sourceFile = path,
                        sourceFileFullPath = fullpath,
                    });
                    aSTraits[i] = trait;

                    trait.Kind = (TraitKind)br.ReadByte();
                    int metacount = br.ReadInt32();
                    for (int k = 0; k < metacount; k++)
                    {
                        ASMeta meta = new ASMeta();
                        meta.Name = _stringPool_[br.ReadInt32()];
                        int count = br.ReadInt32();
                        for (int l = 0; l < count; l++)
                        {
                            ASMetaItem item = new ASMetaItem();
                            item.Name = _stringPool_[br.ReadInt32()];
                            item.Value = _stringPool_[br.ReadInt32()];
                            meta.Items.Add(item);
                        }
                        trait.ASMetadata.Add(meta);
                    }
                    trait.Attributes = (TraitAttributes)br.ReadUInt32();
                    trait.IsStatic = br.ReadBoolean();
                    trait.QName = multinames[br.ReadInt32()];
                    trait.Type = multinames[br.ReadInt32()];
                    trait.ValueKind = (ConstantKind)br.ReadByte();
                    //to do  values
                    if (br.ReadBoolean())
                    {
                        byte type = br.ReadByte();
                        if (type == 0)
                        {
                            trait.Value = new ASTrait.TraitValue(namespaces[br.ReadInt32()]);
                        }
                        else if (type == 1)
                        {
                            int functionindex = br.ReadInt32();

                            trait.Value = new ASTrait.TraitValue(ASTrait.TraitValueType.AS3Function, functionindex);  //as3._functions[functionindex];

                        }
                        else if (type == 2)
                        {
                            int expressionindex = br.ReadInt32();

                            trait.Value = new ASTrait.TraitValue(ASTrait.TraitValueType.AS3Expression, expressionindex);// as3._expressions[expressionindex];
							
						}
                         type = br.ReadByte();
                        if (type == 3)
                        { 
                            ulong raw = br.ReadUInt64();
                            trait.Value.initValue = new NaNBoxing(raw);

                            
                        }
                    }
                    else
                    { 
                        trait.Value = null;
                    }

                    trait.TypeKind = (TypeKind)br.ReadUInt64();
                    trait.Class = scriptClasses[br.ReadInt32()];

                    int findex = br.ReadInt32();
                    if (findex > 0)
                    {
                        bool hastrait = methodhastraits[findex - 1];
                    }
                    trait.Function = scriptMethods[findex];

                    int mindex = br.ReadInt32();
                    if (mindex > 0)
                    {
                        bool hastrait = methodhastraits[mindex - 1];
                        if (!hastrait)
                        {
                            throw new InvalidOperationException();
                        }

                        scriptMethods[mindex].Trait = trait;

                    }
                    trait.Method = scriptMethods[mindex];

                }
                for (int i = 0; i < containers.Length; i++)
                {
                    int ts = br.ReadInt32();
                    for (int j = 0; j < ts; j++)
                    {
                        containers[i].Traits.Add(aSTraits[br.ReadInt32()] );
                    }

                }

                for (int i = 0; i < methodhastraits.Count; i++)
                {
                    if (methodhastraits[i])
                    {
                        if (scriptMethods[i + 1].Trait == null)
                        {
                            throw new InvalidOperationException();
                        }
                    }
                    else
                    {
                        if (scriptMethods[i + 1].Trait != null)
                        {
                            throw new InvalidOperationException();
                        }
                    }
                }

                for (int i = 0; i < methods-1; i++)
                {
                    int containerindex = methodcontainerindex[i];
                    if (containerindex >= 0)
                    {
                        scriptMethods[i + 1].SetContainer(containers[methodcontainerindex[i]]);
                    }

                }
            }

            file.Scripts.AddRange(scripts);
            file.Methods.AddRange(scriptMethods);
            file.Classes.AddRange(scriptClasses);
            file.NamespaceSets = namespacesSets;
            file.Namespaces = namespaces;
            //file.Constants = constants.ToArray();
            file.const_strings = _stringPool_.ToArray();
            file.runtime_alloced_strings = new NaNBoxing[file.const_strings.Length];

            file.ld_classid = ld_cls.ToArray();
            file.ld_supermethods = ld_supermethods.ToArray();
            file.Vectors = aSVectors;
            return file;
        }
    }
}
