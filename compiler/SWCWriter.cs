using juicescript.ABC;
using juicescript.compiler.AST;
using juicescript.runtime;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace juicescript.compiler
{
    internal class SWCWriter
    {
        internal static void Write(CompileContext context, string outSwcFile)
        {
            var data = Encode(context, outSwcFile);

            System.IO.File.WriteAllBytes(outSwcFile, data);

            //SWCReader.Read(new System.IO.MemoryStream(data), (int ptr,int line,string p,string fp) => { return new Token() { ptr=ptr,line=line, sourceFile=p, sourceFileFullPath=fp }; } );

        }

        internal static byte[] Encode(CompileContext context, string outSwcFile)
        {
            if (outSwcFile == null)
                throw new ArgumentNullException(nameof(outSwcFile));

            HashSet<string> _string_ = new HashSet<string>();

            List<ASMultiname> _multinames = new List<ASMultiname>();
            List<ASNamespace> _namespaces = new List<ASNamespace>();
            List<ASNamespaceSet> _namespaceSets = new List<ASNamespaceSet>();
            List<ASContainer> _containers = new List<ASContainer>();

            List<string> _stringPool_ = new List<string>();
            List<ASTrait> _traits = new List<ASTrait>();


            _multinames.Add(null);
            _namespaces.Add(null);
            _namespaceSets.Add(null);


            SWCFile swc = new SWCFile();
            swc.assemblyName = System.IO.Path.GetFileName(outSwcFile);
            swc.refAssemblys.AddRange(context.referenceAssembly);
            swc.UID = Guid.NewGuid();

            swc.Methods.Add(null);
            swc.Classes.Add(null);

            foreach (var script in context.scriptDefs)
            {
                foreach (var item in script.multinames)
                {
                    if (!_multinames.Contains(item))
                    {
                        _multinames.Add(item);
                    }
                }

                foreach (var item in script.namespaces)
                {
                    if (!_namespaces.Contains(item))
                    {
                        _namespaces.Add(item);
                    }
                }

                foreach (var item in script.namespaceSets)
                {
                    if (item != null)
                    {
                        item.Namespaces.RemoveAll(
                            ns => ns.def_uri == null && ns.Kind == NamespaceKind.PackageInternal
                            );
                    }

                    if (!_namespaceSets.Contains(item))
                    {
                        _namespaceSets.Add(item);
                    }
                }

                _containers.AddRange(script.containers);

                for (int i = 0; i < _containers.Count; i++)
                {
                    _traits.AddRange(_containers[i].Traits);
                }



                swc.Scripts.Add(script.Script);
                swc.Methods.AddRange(script.scriptMethods.Where(o => o != null));
                swc.Classes.AddRange(script.scriptClasses.Where(o => o != null));
            }


            foreach (var s in _namespaces.Where(o => o != null).Select(o => o.Name))
            {
                _string_.Add(s);
            }

            foreach (var s in _namespaces.Where(o => o != null).Select(o => o.def_uri))
            {
                _string_.Add(s);
            }


            foreach (var s in _multinames.Where(o => o != null).Select(o => o.Name))
            {
                _string_.Add(s);
            }

            foreach (var ns in _namespaceSets.Where(o => o != null))
            {
                foreach (var s in ns.Namespaces)
                {
                    _string_.Add(s.Name);
                    _string_.Add(s.in_package);
                    _string_.Add(s.def_uri);
                }
            }

            foreach (var item in context.scriptDefs.Select(o => o.sourceFile))
            {
                _string_.Add(item);
            }

            foreach (var item in context.scriptDefs.Select(o => o.fullPath))
            {
                _string_.Add(item);
            }
            foreach (var item in swc.Methods.Where(o => o != null))
            {
                if (item.Token != null)
                {
                    _string_.Add(((Token)item.Token).sourceFile);
                    _string_.Add(((Token)item.Token).sourceFileFullPath);
                }

                _string_.Add(item.Name);
                foreach (var p in item.Parameters)
                {
                    _string_.Add(p.Name);
                }
            }
            foreach (var t in _traits)
            {
                for (int j = 0; j < t.ASMetadata.Count; j++)
                {
                    var meta = t.ASMetadata[j];
                    _string_.Add(meta.Name);

                    for (int k = 0; k < meta.Items.Count; k++)
                    {
                        _string_.Add(meta.Items[k].Name);
                        _string_.Add(meta.Items[k].Value);
                    }
                }
            }

            HashSet<ulong> _ld_classes = new HashSet<ulong>();

            List<Tuple<ulong, int>> _ld_supermethod = new List<Tuple<ulong, int>>();

            var initValueConstants = context.scriptDefs.SelectMany(s => s.Script.allContainers)
                .Where(c => c._link_codescope.Members.Any(m => m.Kind != ScopeMemberKind.Parameter && m.trait.Value != null && m.trait.Value.initValue.HasValue))
                .SelectMany(c => c._link_codescope.Members)
                .Where(m => m.Kind != ScopeMemberKind.Parameter && m.trait.Value != null && m.trait.Value.initValue.HasValue)
                .Select(m => m.trait.Value.initValue.Value)
                ;

            var para_constans = swc.Methods.Where(o => o != null).SelectMany(o => o.Parameters).Where(p => p.IsOptional && p.computeDefaultValue.Length>0)
                .Select(p => ASMethodBody.ReadConstants(p.computeDefaultValue));

            var param_defaults = swc.Methods.Where(o => o != null && o.Body.param_defaultvalues != null).Select(p => ASMethodBody.ReadConstants(p.Body.param_defaultvalues));

			var  mConstants = context.dict_method_constants.Values
                .Concat(
                para_constans
                )
               .Concat( new List<IEnumerable<NaNBoxing>>() { initValueConstants } )
               .Concat(param_defaults)
                ;

            foreach (var p in mConstants)
            {
                foreach (var c in p)
                {
                    if (c.ValueType == NaNBoxing.BoxType.HeapPtr
                        )
                    {
                        ASMethodBody.PoolHeapPtrKind kind = (ASMethodBody.PoolHeapPtrKind)(c.HeapPtr >> 24);
                        if (kind == ASMethodBody.PoolHeapPtrKind.String)
                        {
                            _string_.Add(((RtString)context.player_for_compiler.Context.GC.Heap[c.HeapPtr & 0xFFFFFF]).Str);
                        }
                        else if (kind == ASMethodBody.PoolHeapPtrKind.LD_Class)
                        {
                            //ulong cid = ((ASClass)context.player_for_compiler.Context.GC.Heap[c.HeapPtr & 0xFFFFFF].Type).Type_identifier;
                            ulong cid = context.constpool_ldclass[ c.HeapPtr & 0xFFFFFF ];
                            _ld_classes.Add(cid);
                        }
                        else if (kind == ASMethodBody.PoolHeapPtrKind.Namespace)
                        {
                            //do nothing
                        }
                        else if (kind == ASMethodBody.PoolHeapPtrKind.VectorDef)
                        {
                            //do nothing
                        }
                        else if (kind == ASMethodBody.PoolHeapPtrKind.Method)
                        {
                            //do nothing
                        }
                        else if (kind == ASMethodBody.PoolHeapPtrKind.SuperMethod)
                        {
                            var heapInstance = context.player_for_compiler.Context.GC.Heap[c.HeapPtr & 0xFFFFFF];
                            ulong type = ((ASClass)heapInstance.Type).Type_identifier;
                            int vtable_index = ((RtMethodScope)heapInstance).ParentPtr;

                            if (!_ld_supermethod.Exists((t) => t.Item1 == type && t.Item2 == vtable_index))
                            {
                                _ld_supermethod.Add(new Tuple<ulong, int>(type,vtable_index));
                            }
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                    }
                }
            }


            _stringPool_ = new List<string>();
            _stringPool_.Add(null);
            _stringPool_.AddRange(_string_.Where(o => o != null));
            _stringPool_.AddRange(context.vectorDefs.Select(v => v.ToString()));


            var ld_cls = _ld_classes.ToList();


            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            {
                using (System.IO.BinaryWriter bw = new System.IO.BinaryWriter(ms))
                {

                    var thisassembly = typeof(SWCWriter).Assembly;
                    Version ver = thisassembly.GetName().Version;

                    bw.Write(ver.Major);
                    bw.Write(ver.Minor);
                    bw.Write(ver.Build);
                    bw.Write(ver.Revision);

                    bw.Write(swc.UID.ToByteArray());
                    bw.Write(swc.assemblyName);
                    bw.Write(swc.refAssemblys.Count);
                    for (int i = 0; i < swc.refAssemblys.Count; i++)
                    {
                        bw.Write(swc.refAssemblys[i]);
                    }


                    #region STRING POOL

                    bw.Write(_stringPool_.Count - 1);
                    for (int i = 1; i < _stringPool_.Count; i++)
                    {
                        var chars = _stringPool_[i].AsSpan();
                        bw.Write(chars.Length);
                        for (int j = 0; j < chars.Length; j++)
                        {
                            bw.Write((ushort)chars[j]);
                        }
                    }

                    #endregion

                    #region LD_CLASS_POOL

                   
                    bw.Write(ld_cls.Count);

                    for (int i = 0; i < ld_cls.Count; i++)
                    {
                        bw.Write(ld_cls[i]);
                    }

                    #endregion

                    #region LD_SUPERMETHOD_POOL

                    bw.Write(_ld_supermethod.Count);

                    for (int i = 0; i < _ld_supermethod.Count; i++)
                    {
                        bw.Write(_ld_supermethod[i].Item1);
                        bw.Write(_ld_supermethod[i].Item2);
                    }

                    #endregion


                    #region VectorDEFS
                    bw.Write(context.vectorDefs.Count);
                    for (int i = 0; i < context.vectorDefs.Count; i++)
                    {
                        var def = context.vectorDefs[i];
                        bw.Write(def.depth);
                        bw.Write((ulong)def.Identifier);
                        bw.Write((ulong)def.ElementTypeId);
                        bw.Write(_stringPool_.IndexOf(def.ToString()) );

                    }

                    #endregion


                    #region namespace

                    bw.Write(_namespaces.Count - 1);
                    for (int i = 1; i < _namespaces.Count; i++)
                    {
                        var n = _namespaces[i];
                        bw.Write((byte)n.Kind);
                        bw.Write(_stringPool_.IndexOf(n.Name));
                        bw.Write(_stringPool_.IndexOf(n.in_package));
                        bw.Write(_stringPool_.IndexOf(n.def_uri));
                    }

                    bw.Write(_namespaceSets.Count - 1);
                    for (int i = 1; i < _namespaceSets.Count; i++)
                    {
                        var ns = _namespaceSets[i];
                        bw.Write(ns.Namespaces.Count);
                        for (int j = 0; j < ns.Namespaces.Count; j++)
                        {
                            bw.Write(_namespaces.IndexOf(ns.Namespaces[j]));
                        }
                    }

                    bw.Write(_multinames.Count - 1);
                    for (int i = 1; i < _multinames.Count; i++)
                    {
                        var m = _multinames[i];
                        bw.Write((byte)m.Kind);
                        bw.Write(_stringPool_.IndexOf(m.Name));

                        bw.Write(_namespaces.IndexOf(m.Namespace));
                        bw.Write(_multinames.IndexOf(m.QName));
                        if (m.Types == null)
                        {
                            bw.Write(0);
                        }
                        else
                        {
                            bw.Write(m.Types.Count);
                            for (int j = 0; j < m.Types.Count; j++)
                            {
                                bw.Write(_multinames.IndexOf(m.Types[j]));
                            }
                        }
                        bw.Write(_namespaceSets.IndexOf(m.NamespaceSet));
                    }

                    #endregion


                    bw.Write(_containers.Count);

                    bw.Write(swc.Methods.Count);
                    for (int i = 1; i < swc.Methods.Count; i++)
                    {


                        var method = swc.Methods[i];


                        bw.Write(method.ast_function_index);
                        if (method.ast_function_index > -1)
                        {
                            bw.Write(((Token)method.Token).ptr);
                            bw.Write(((Token)method.Token).line);
                            bw.Write(_stringPool_.IndexOf(((Token)method.Token).sourceFile));
                            bw.Write(_stringPool_.IndexOf(((Token)method.Token).sourceFileFullPath));
                        }

                        bw.Write(_containers.IndexOf(method.Container));
                        bw.Write((uint)method.Flags);
                        bw.Write(_stringPool_.IndexOf(method.Name));
                        bw.Write(method.IsAnonymous);
                        bw.Write(method.IsConstructor);
                       
                        if (method.Body.param_defaultvalues != null)
                        {
							//byte[] buffer = new byte[method.Body.param_defaultvalues.Length];
							//Array.Copy(method.Body.param_defaultvalues, buffer, method.Body.param_defaultvalues.Length);

							UpdateMethodConstants(method.Body.param_defaultvalues, context, _stringPool_, ld_cls, _namespaces, swc, _ld_supermethod);

							bw.Write(method.Body.param_defaultvalues.Length);
                            bw.Write(method.Body.param_defaultvalues);
                        }
                        else
                        {
                            bw.Write(0);
                        }

						bw.Write(method.Parameters.Count);

						for (int j = 0; j < method.Parameters.Count; j++)
                        {
                            var p = method.Parameters[j];
                            bw.Write(_stringPool_.IndexOf(p.Name));
                            bw.Write(p.IsOptional);
                            bw.Write(p.IsRest);
                            bw.Write(_multinames.IndexOf(p.Type));
                            bw.Write((ulong)p.TypeKind);
                            bw.Write((byte)p.ValueKind);

                            bw.Write(p.ValueExprIndex);

                            if (p.IsOptional)
                            {
                                if (p.computeDefaultValue.Length > 0)
                                {
									UpdateMethodConstants(p.computeDefaultValue, context, _stringPool_, ld_cls, _namespaces, swc, _ld_supermethod);
                                }
                            }

                        }

                        bw.Write(_multinames.IndexOf(method.ReturnType));
                        bw.Write((ulong)method.ReturnTypeKind);

                        if (method.Trait == null)
                        {
                            bw.Write(false);
                        }
                        else
                        {
                            bw.Write(true);
                        }

                        bw.Write(_containers.IndexOf(method.Body));

                        byte[] bytecode = new byte[method.Body.ByteCode.Length];
                        Array.Copy(method.Body.ByteCode,bytecode, method.Body.ByteCode.Length);
                        //**整理常量中的引用类型***
                        UpdateMethodConstants(bytecode, context, _stringPool_, ld_cls, _namespaces, swc, _ld_supermethod);

                        bw.Write(bytecode.Length);
                        bw.Write(bytecode, 0, bytecode.Length);


                        //命名空间集索引
                        //bw.Write(method.Body.NamespaceSetIndex);
                        var def = context.scriptDefs.First(o => o.scriptMethods.Contains(method));
                        var ns = def.namespaceSets[method.Body.NamespaceSetIndex];

                        bw.Write(_namespaceSets.IndexOf(ns));
                    }



                    bw.Write(swc.Classes.Count);
                    for (int i = 1; i < swc.Classes.Count; i++)
                    {
                        var cls = swc.Classes[i];
                        bw.Write(((Token)cls.Token).ptr);
                        bw.Write(((Token)cls.Token).line);
                        bw.Write(_stringPool_.IndexOf(((Token)cls.Token).sourceFile));
                        bw.Write(_stringPool_.IndexOf(((Token)cls.Token).sourceFileFullPath));

                        bw.Write(cls.Type_identifier);

                        bw.Write(swc.Methods.IndexOf(cls.Constructor));
                        bw.Write(_containers.IndexOf(cls));

                        var instance = cls.Instance;
                        bw.Write(_multinames.IndexOf(instance.QName));
                        bw.Write((uint)instance.Flags);
                        bw.Write(swc.Methods.IndexOf(instance.Constructor));
                        bw.Write(instance.Interfaces.Count);
                        for (int j = 0; j < instance.Interfaces.Count; j++)
                        {
                            var iface = instance.Interfaces[j];
                            bw.Write(_multinames.IndexOf(iface));
                        }
                        bw.Write(_multinames.IndexOf(instance.Super));
                        bw.Write(_namespaces.IndexOf(instance.ProtectedNamespace));
                        bw.Write(_containers.IndexOf(instance));
                    }



                    bw.Write(swc.Scripts.Count);
                    for (int i = 0; i < swc.Scripts.Count; i++)
                    {
                        var script = swc.Scripts[i];

                        bw.Write(script.allContainers.Count);
                        bw.Write(swc.Methods.IndexOf(script.Initializer));
                        bw.Write(_containers.IndexOf(script));

                        var package_imps = context.scriptDef_packageimports.First(o => o.Key.Script == script).Value.ToList();
                        bw.Write(package_imps.Count);
                        for (int j = 0; j < package_imps.Count; j++)
                        {
                            bw.Write(_traits.IndexOf(package_imps[j]));
                        }

                        var script_imps = context.scriptDef_scriptimports.First(o => o.Key.Script == script).Value.ToList();
                        bw.Write(script_imps.Count);
                        for (int j = 0; j < script_imps.Count; j++)
                        {
                            bw.Write(_traits.IndexOf(script_imps[j]));
                        }


                    }
                    #region traits
                    bw.Write(_traits.Count);
                    for (int i = 0; i < _traits.Count; i++)
                    {
                        var t = _traits[i];
                        bw.Write(((Token)t.Token).ptr);
                        bw.Write(((Token)t.Token).line);
                        bw.Write(_stringPool_.IndexOf(((Token)t.Token).sourceFile));
                        bw.Write(_stringPool_.IndexOf(((Token)t.Token).sourceFileFullPath));

                        bw.Write((byte)t.Kind);
                        bw.Write(t.ASMetadata.Count);
                        for (int j = 0; j < t.ASMetadata.Count; j++)
                        {
                            var meta = t.ASMetadata[j];
                            bw.Write(_stringPool_.IndexOf(meta.Name));
                            bw.Write(meta.Items.Count);
                            for (int k = 0; k < meta.Items.Count; k++)
                            {
                                bw.Write(_stringPool_.IndexOf(meta.Items[k].Name));
                                bw.Write(_stringPool_.IndexOf(meta.Items[k].Value));
                            }
                        }

                        bw.Write((uint)t.Attributes);
                        bw.Write(t.IsStatic);
                        bw.Write(_multinames.IndexOf(t.QName));
                        bw.Write(_multinames.IndexOf(t.Type));
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
                                bw.Write(_namespaces.IndexOf(t.Value.Namespace));
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

                            if (t.Value.initValue.HasValue)
                            {
                                NaNBoxing v = (NaNBoxing)t.Value.initValue.Value;
                                UpdateConstantSlot(ref v, context, _stringPool_, ld_cls, _namespaces, swc, _ld_supermethod);

                                bw.Write((byte)3);
                                bw.Write(v.Raw);

                            }
                            else
                            {
                                bw.Write((byte)255);
                            }
                        }



                        bw.Write((ulong)t.TypeKind);
                        bw.Write(swc.Classes.IndexOf(t.Class));
                        bw.Write(swc.Methods.IndexOf(t.Function));
                        bw.Write(swc.Methods.IndexOf(t.Method));

                    }
                    #endregion

                    for (int i = 0; i < _containers.Count; i++)
                    {
                        var container = _containers[i];
                        bw.Write(container.Traits.Count);
                        for (int j = 0; j < container.Traits.Count; j++)
                        {
                            bw.Write(_traits.IndexOf(container.Traits[j]));
                        }

                    }


                }

                return ms.ToArray();
            }

        }

        internal static void UpdateConstantSlot(ref NaNBoxing boxing,
			 CompileContext context, List<string> _stringPool_, List<ulong> ld_cls, List<ASNamespace> _namespaces,
			    SWCFile swc, List<Tuple<ulong, int>> _ld_supermethod
			)
        {
			if (boxing.ValueType == NaNBoxing.BoxType.HeapPtr)
			{
				ASMethodBody.PoolHeapPtrKind kind = (ASMethodBody.PoolHeapPtrKind)(boxing.HeapPtr >> 24);

                if (kind == ASMethodBody.PoolHeapPtrKind.VectorDef)
                {
                    //int vector_index = boxings[j].HeapPtr & 0xFFFFFF;
                }
                else if (kind == ASMethodBody.PoolHeapPtrKind.LD_Class)
                {
                    
                    ulong classid = context.constpool_ldclass[boxing.HeapPtr & 0xFFFFFF];

                    int pool_index = ld_cls.IndexOf(classid);
                    if (pool_index > 0xFFFFFF)
                    {
                        throw new ParseException("pool_index > 0xffffff");
                    }
                    if (pool_index < 0)
                        throw new InvalidOperationException();

                    boxing.SetHeapPtr((0xffffff & pool_index) | ((byte)ASMethodBody.PoolHeapPtrKind.LD_Class << 24),NaNBoxing.UNKNOWN_HEAPKIND, (byte)HeapKindFlag.NONE);
                }
				else
                {
                    RtHeapBase heapInstance = context.player_for_compiler.Context.GC.Heap[boxing.HeapPtr & 0xFFFFFF];
                    if (heapInstance.Kind == RtHeapTypeKind.STRING && kind == ASMethodBody.PoolHeapPtrKind.String)
                    {
                        int pool_index = _stringPool_.IndexOf(((RtString)heapInstance).Str);

                        if (pool_index > 0xFFFFFF)
                        {
                            throw new ParseException("pool_index > 0xffffff");
                        }
                        if (pool_index < 0)
                            throw new InvalidOperationException();

                        boxing.SetHeapPtr((0xffffff & pool_index) | ((byte)ASMethodBody.PoolHeapPtrKind.String << 24), NaNBoxing.UNKNOWN_HEAPKIND, (byte)HeapKindFlag.NONE);
                    }
                    //else if (heapInstance.TypeKind == RtHeapTypeKind.CACHE_LD_CLASS && kind == ASMethodBody.PoolHeapPtrKind.LD_Class)
                    //{
                    //	int pool_index = ld_cls.IndexOf(((ASClass)heapInstance.Type).Type_identifier);
                    //	if (pool_index > 0xFFFFFF)
                    //	{
                    //		throw new ParseException("pool_index > 0xffffff");
                    //	}
                    //	if (pool_index < 0)
                    //		throw new InvalidOperationException();

                    //	boxing.SetHeapPtr((0xffffff & pool_index) | ((byte)ASMethodBody.PoolHeapPtrKind.LD_Class << 24));

                    //}
                    else if (heapInstance.Kind == RtHeapTypeKind.NAMESPACE && kind == ASMethodBody.PoolHeapPtrKind.Namespace)
                    {
                        int pool_index = _namespaces.IndexOf(((RtNameSpace)heapInstance).ASNamespace);
                        if (pool_index > 0xFFFFFF)
                        {
                            throw new ParseException("pool_index > 0xffffff");
                        }
                        if (pool_index < 1)
                        {
                            throw new InvalidOperationException();
                        }

                        boxing.SetHeapPtr((0xffffff & pool_index) | ((byte)ASMethodBody.PoolHeapPtrKind.Namespace << 24), NaNBoxing.UNKNOWN_HEAPKIND, (byte)HeapKindFlag.NONE);
                    }
                    else if (heapInstance.Kind == RtHeapTypeKind.MethodScope && kind == ASMethodBody.PoolHeapPtrKind.Method)
                    {
                        int pool_index = swc.Methods.IndexOf(((ASMethodBody)heapInstance.Type).Method);

                        if (pool_index > 0xFFFFFF)
                        {
                            throw new ParseException("pool_index > 0xffffff");
                        }
                        if (pool_index < 1)
                        {
                            throw new InvalidOperationException();
                        }

                        boxing.SetHeapPtr((0xffffff & pool_index) | ((byte)ASMethodBody.PoolHeapPtrKind.Method << 24), NaNBoxing.UNKNOWN_HEAPKIND, (byte)HeapKindFlag.NONE);

                    }
                    else if (heapInstance.Kind == RtHeapTypeKind.MethodScope && kind == ASMethodBody.PoolHeapPtrKind.SuperMethod)
                    {
                        ulong type = ((ASClass)heapInstance.Type).Type_identifier;
                        int vtable_index = ((RtMethodScope)heapInstance).ParentPtr;

                        int pool_index = _ld_supermethod.FindIndex((t) => { return t.Item1 == type && t.Item2 == vtable_index; });
                        if (pool_index > 0xFFFFFF)
                        {
                            throw new ParseException("pool_index > 0xffffff");
                        }
                        if (pool_index < 0)
                        {
                            throw new InvalidOperationException();
                        }

                        boxing.SetHeapPtr((0xffffff & pool_index) | ((byte)ASMethodBody.PoolHeapPtrKind.SuperMethod << 24), NaNBoxing.UNKNOWN_HEAPKIND, (byte)HeapKindFlag.NONE);

                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                }
			}
		}

        internal static void UpdateMethodConstants(byte[] bytecode,
            CompileContext context,List<string> _stringPool_, List<ulong> ld_cls, List<ASNamespace> _namespaces,
            SWCFile swc, List<Tuple<ulong, int>> _ld_supermethod
			)
        {
			unsafe
			{

				fixed (void* ptr = bytecode)//handle.AddrOfPinnedObject().ToPointer();
				{
					Span<int> count_span = new Span<int>(ptr, 3);
					int count = count_span[1];
					int ins_count = count_span[2];


					Span<NaNBoxing> boxings = new Span<NaNBoxing>((int*)ptr + 3 + 2 * ins_count, count);

					for (int j = 0; j < count; j++)
					{
                        UpdateConstantSlot(ref boxings[j], context, _stringPool_, ld_cls, _namespaces, swc, _ld_supermethod);
					}
				}
			}

		}
	}

}
