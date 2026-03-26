using juicescript.ABC;
using juicescript.runtime.buildin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.runtime.gc
{
    public class GC
    {
        public GCHeap Heap;

        public List<RtHeapInstance> Root;

        public int MemUsage;

        public readonly int USAGE_LIMIT;

        private Context context;

        public GC(Context context, int limit = int.MaxValue)
        {
            Heap = new GCHeap();
            Root = new List<RtHeapInstance>();
            this.context = context;
            USAGE_LIMIT = limit;

            thresholed = 1024;

        }

        private int root_cache_count;
        internal void MarkCaches()
        {
            Heap.MarkCaches();
            root_cache_count = Root.Count;
        }

        

        /// <summary>
        /// 更新内存使用 减去某个实例的占用数
        /// </summary>
        /// <param name="instance"></param>
        public void UpdateMemUsage_Sub(RtHeapInstance instance)
        { 
            MemUsage -= CalculMemusage(instance);
        }

        /// <summary>
        /// 更新内存使用 加上某个实例的占用数
        /// </summary>
        /// <param name="instance"></param>
        public void UpdateMemUsage_Add(RtHeapInstance instance)
        {
            MemUsage += CalculMemusage(instance);
        }

        /// <summary>
        /// 更新内存使用量
        /// </summary>
        /// <param name="size"></param>
        public void UpdateMemUsage_Change(int size)
        {
            MemUsage += size;
        }


        /// <summary>
        /// 分配一个Shape的节点
        /// </summary>
        /// <returns></returns>
        public int AllocShape()
        { 
            RtHeapInstance heapInstance = new RtHeapInstance();
            heapInstance.TypeKind = RtHeapTypeKind.SHAPE;
            heapInstance.facility = new RtPayloadShape();

            int size = CalculMemusage(heapInstance);
            if (MemUsage + size > USAGE_LIMIT)
            {
                return 0;
            }
            MemUsage += size;

            Root.Add(heapInstance);
            return Heap.AddHeapInstance(heapInstance);

        }

        public int AllocDynamicSlot()
        {
            RtHeapInstance heapInstance = new RtHeapInstance();
            heapInstance.TypeKind = RtHeapTypeKind.DYNAMIC_PROPERTYS;
            heapInstance.facility = new RtPayloadDynamic();

            int size = CalculMemusage(heapInstance);
            if (MemUsage + size > USAGE_LIMIT)
            {
                return 0;
            }
            MemUsage += size;

            return Heap.AddHeapInstance(heapInstance);

        }

        /// <summary>
        /// 分配ASClass的实例对象
        /// </summary>
        /// <returns>返回内存堆对象的序号</returns>
        public int AllocASClassObj(ASClass cls, ASInstance OBJECT)
        {
            //先分配prototype Object
            RtHeapInstance _protoObj;
            int _protoPtr = 0;

            if (cls.Type_identifier == (ulong)TypeKind.Function)
            {
                cls.Instance.Constructor.__ismethod = false;
                _protoPtr = AllocClosure(cls.Instance.Constructor);
                if (_protoPtr == 0)
                {
                    return 0;
                }
                _protoObj = Heap[_protoPtr];

    //            RtHeapInstance _o;
    //            int c_proto = AllocInstance(OBJECT, out _o);
    //            if (c_proto == 0)
    //            {
    //                return 0;
    //            }
				//((RtPayloadClosure)_protoObj.facility).Set_PROTOTYPE( c_proto, context.player);

            }
            else
            {
                _protoPtr = AllocInstance(OBJECT, out _protoObj);
                if (_protoPtr == 0)
                {
                    return 0;
                }
            }
#if DEBUG
            //检测 Object.prototype的__proto__必须指向null.
            if (cls == context.OBJECT)
            {
                if (((RtPayloadInstance)_protoObj.facility).PROTOTYPE(context.player,(ASInstance)_protoObj.Type) != 0)
                {
                    throw new InvalidOperationException();
                }
            }

#endif

            RtHeapInstance heapInstance = new RtHeapInstance();
            heapInstance.TypeKind = RtHeapTypeKind.CLASS;
            heapInstance.Type = context.CLASS;
            heapInstance.facility = new RtPayloadScriptClass(cls, _protoPtr)
#if FORCOMPILER
            { isCompiling = context.player.IsComputeConstExpr }
#endif

                ;

            int size = CalculMemusage(heapInstance);
            if (MemUsage + size > USAGE_LIMIT)
            {
                return 0;
            }
            MemUsage += size;

            //ASClass实例是静态成员的容器。加入GCRoot中
            Root.Add(heapInstance);

            return Heap.AddHeapInstance(heapInstance);

        }

        /// <summary>
        /// 分配ASScript的global实例对象
        /// </summary>
        /// <param name="script"></param>
        /// <returns>返回内存堆对象的序号</returns>
        public int AllocGlobal(ASScript script)
        {
            RtHeapInstance heapInstance = new RtHeapInstance();
            heapInstance.TypeKind = RtHeapTypeKind.GLOBAL;
            heapInstance.Type = script.Initializer.Body; // type设定为初始化函数
            heapInstance.facility = new RtPayloadScriptClass(script)
#if FORCOMPILER
            {  isCompiling = context.player.IsComputeConstExpr }
#endif
                ;

            int size = CalculMemusage(heapInstance);
            if (MemUsage + size > USAGE_LIMIT)
            {
                return 0;
            }
            MemUsage += size;

            //ASScript的实例，加入GCRoot中
            Root.Add(heapInstance);

            return Heap.AddHeapInstance(heapInstance);
        }


        /// <summary>
        /// 分配数组对象的实例
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        internal int AllocArray(out RtHeapInstance instance, RtPayloadArray.ArrayStoreMode storeMode,int cache_struct_st = 0)
        {
            RtHeapInstance heapInstance = new RtHeapInstance();
            heapInstance.TypeKind = RtHeapTypeKind.ARRAY;
            

            RtPayloadArray payload = new RtPayloadArray();
            heapInstance.facility = payload;

            payload.StoreMode = storeMode;

            switch (storeMode)
            {
                case RtPayloadArray.ArrayStoreMode.cache_on_stack:
                    heapInstance.Type = null;
                    Root.Add(heapInstance);
                    break;
                case RtPayloadArray.ArrayStoreMode.cache:
                    heapInstance.Type = null;
                    payload.cache_store = new NaNBoxing[RtPayloadArray.MAX_CACHE_ELEMENT];
                    payload.cache_structs = new int[RtPayloadArray.MAX_CACHE_ELEMENT];
                    for (int i = 0; i < RtPayloadArray.MAX_CACHE_ELEMENT; i++)
                    {
                        payload.cache_structs[i] = cache_struct_st + i;
                    }

                    Root.Add(heapInstance);
                    break;
                case RtPayloadArray.ArrayStoreMode.normal:
                    heapInstance.Type = context.ARRAY.Instance;
                    payload.InitNormalStore();

                    break;
                default:
#if DEBUG
                    throw new InvalidOperationException();
#else
					Environment.FailFast("出错了，这里跑不到"); break;
#endif
			}

			int size = CalculMemusage(heapInstance);
            if (MemUsage + size > USAGE_LIMIT)
            {
                instance = null;
                return 0;
            }
            MemUsage += size;

            instance = heapInstance;

            return Heap.AddHeapInstance(heapInstance);
        }


        /// <summary>
        /// 分配对象的实例的内存
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public int AllocInstance(ASInstance type,out RtHeapInstance out_instance)
        {
            RtHeapInstance heapInstance = new RtHeapInstance();
            
            if (type.Flags.HasFlag(ClassFlags.Vector))
            {
                heapInstance.TypeKind = RtHeapTypeKind.VECTOR;
                heapInstance.Type = type;

                RtPayloadVector payload = new RtPayloadVector(type._element_class);
                heapInstance.facility = payload;

            }
            else
            {
                
                heapInstance.TypeKind = RtHeapTypeKind.INSTANCE;
                heapInstance.Type = type;
                RtPayloadInstance payload = new RtPayloadInstance()
#if FORCOMPILER
                {  isCompiling = context.player.IsComputeConstExpr}
#endif
                    ;

                
                TypeLayout typeLayout = type._link_codescope.TypeLayout;

                payload.GenStore(typeLayout.Size);

                if (typeLayout.ASType.__instance_index__ > 0)
                {
                    payload.Set_PROTOTYPE( ((RtPayloadScriptClass)Heap[ typeLayout.ASType.__instance_index__].facility).PROTO__PTR , context.player);
                }

                if (typeLayout.Size > 0)
                {
                    payload.Init(type._link_codescope,context.player,true);
                }
                heapInstance.facility = payload;
            }

            int size = CalculMemusage(heapInstance);
            if (MemUsage + size > USAGE_LIMIT)
            {
                out_instance = null;
                return 0;
            }
            MemUsage += size;

            out_instance = heapInstance;

            return Heap.AddHeapInstance(heapInstance);

        }

        public int AllocCacheVector()
        {
			RtHeapInstance heapInstance = new RtHeapInstance();
			heapInstance.TypeKind = RtHeapTypeKind.VECTOR;
			heapInstance.Type = null;

			RtPayloadVector payload = new RtPayloadVector(null);
			heapInstance.facility = payload;

            payload.SetStore( new buildin.VectorImpl.VectorStore() );

			int size = CalculMemusage(heapInstance);
			if (MemUsage + size > USAGE_LIMIT)
			{
				return 0;
			}
			MemUsage += size;

			Root.Add(heapInstance);

			return Heap.AddHeapInstance(heapInstance);
		}

        public int AllocCacheInstance()
        {
            RtHeapInstance heapInstance = new RtHeapInstance();
            heapInstance.TypeKind = RtHeapTypeKind.INSTANCE;
            heapInstance.Type = null;
            RtPayloadInstance payload = new RtPayloadInstance();//true);
            payload.GenStore(RtPayloadInstance.MAX_CACHEABLE_SIZE);
            //{
            //    bytes = new Memory<byte>(new byte[RtPayloadInstance.MAX_CACHEABLE_SIZE])
            //};
            heapInstance.facility = payload;
            
            int size = CalculMemusage(heapInstance);
            if (MemUsage + size > USAGE_LIMIT)
            {
                return 0;
            }
            MemUsage += size;

            Root.Add(heapInstance);

            return Heap.AddHeapInstance(heapInstance);
        }


        /// <summary>
        /// 分配字符串
        /// </summary>
        /// <param name="str"></param>
        /// <returns>返回内存堆对象序号</returns>
        public int AllocString(string str)
        {
            RtHeapInstance heapInstance = new RtHeapInstance();
            heapInstance.TypeKind = RtHeapTypeKind.STRING;
            heapInstance.Type = context.STRING;
            heapInstance.facility = new RtPayloadString(str);

            int size = CalculMemusage(heapInstance);
            if (MemUsage + size > USAGE_LIMIT)
            {
                return 0;
            }
            MemUsage += size;

            return Heap.AddHeapInstance(heapInstance);
        }

        public int AllocNamespace(ASNamespace @namespace,int prefixPtr,int uriPtr)
        {
            RtHeapInstance heapInstance = new RtHeapInstance();
            heapInstance.TypeKind = RtHeapTypeKind.NAMESPACE;
            heapInstance.Type = null;
            heapInstance.facility = new RtPayloadNameSpace()
            {
                ASNamespace = @namespace,
                prefixPtr = prefixPtr,
                uriPtr = uriPtr 
            };

            int size = CalculMemusage(heapInstance);
            if (MemUsage + size > USAGE_LIMIT)
            {
                return 0;
            }
            MemUsage += size;

            Root.Add(heapInstance);

            return Heap.AddHeapInstance(heapInstance);
        }


        public int AllocClosure(ASMethod method)
        {
            RtHeapInstance heapInstance = new RtHeapInstance();
            heapInstance.TypeKind = RtHeapTypeKind.CLOSURE;
            

            heapInstance.facility = new RtPayloadClosure();

            int size = CalculMemusage(heapInstance);
            if (MemUsage + size > USAGE_LIMIT)
            {
                return 0;
            }
            MemUsage += size;

            if (method == null)
            {
                Root.Add(heapInstance);
            }
            else
            {
                heapInstance.Type = method.Body;
            }


            return Heap.AddHeapInstance(heapInstance);

        }


        private List<RtHeapInstance> cache_iter_ctx = new List<RtHeapInstance>();
        private int cache_iter_ctx_index;

        internal int IterCtxIndex { get => cache_iter_ctx_index; }
        
        public int RentIterContext(out RtHeapInstance ctx)
        {
            if (cache_iter_ctx_index < cache_iter_ctx.Count)
            {
                
                ctx = cache_iter_ctx[cache_iter_ctx_index];
				cache_iter_ctx_index++;

				return ((IterContxt)((RtPayloadInstance)ctx.facility).wapperedObject).heapPtr;
            }
            else
            {
                
				var cls = context.IITERATOR._link_codescope.Parent.Container.Traits[2].Class;

				RtHeapInstance iterctx;
				var p = AllocInstance(cls.Instance, out iterctx);
				if (p != 0)
				{
					((RtPayloadInstance)iterctx.facility).wapperedObject = new IterContxt() { heapPtr=p };
				}

				ctx = iterctx;

				cache_iter_ctx.Add(iterctx);
				cache_iter_ctx_index = cache_iter_ctx.Count;

				return p;
			}

		}

        public void ReturnIterContext(RtHeapInstance iter_ctx)
        {
#if DEBUG
            if (cache_iter_ctx_index <0)
            {
				throw new InvalidOperationException();
			}

            if (iter_ctx != cache_iter_ctx[ cache_iter_ctx_index - 1])
            {
                throw new InvalidOperationException();
            }
#endif
            cache_iter_ctx_index--;

            ((IterContxt)((RtPayloadInstance)iter_ctx.facility).wapperedObject).Close();

        }

        public void ReturnIterContextWhenGetIterFailed()
        {
			cache_iter_ctx_index--;
		}

        internal IterContxt CurrentIterContext()
        {
            return  (IterContxt)((RtPayloadInstance)cache_iter_ctx[cache_iter_ctx_index - 1].facility).wapperedObject;

		}

		internal void ResetIterContextPool()
		{
            cache_iter_ctx_index = 0;
		}


		public int AllocStackCache()
        {
            RtHeapInstance heapInstance = new RtHeapInstance();
            heapInstance.TypeKind = RtHeapTypeKind.STACK_CACHE_OBJ;
            heapInstance.Type = null;
            heapInstance.facility = new RtPayloadStackCache();

            int size = CalculMemusage(heapInstance);
            if (MemUsage + size > USAGE_LIMIT)
            {
                return 0;
            }
            MemUsage += size;

            //一直存在的对象，需要加入GCRoot
            Root.Add(heapInstance);


            return Heap.AddHeapInstance(heapInstance);

        }

        public int AllocMethodScope(NaNBoxing[] data,int start,CodeScope codescope
#if FORCOMPILER
            ,bool isEvalInitValue = false
#endif
            )
        {
            RtHeapInstance heapInstance = new RtHeapInstance();
            heapInstance.TypeKind = RtHeapTypeKind.MethodScope;
            heapInstance.Type = null;
            heapInstance.facility = new RtPayloadMethodScope()

#if FORCOMPILER
            { isCompiling=isEvalInitValue }
#endif
				;

			int size = CalculMemusage(heapInstance);
            if (MemUsage + size > USAGE_LIMIT)
            {
                return 0;
            }
            MemUsage += size;




            if (data != null)
            {                
                ((RtPayloadMethodScope)heapInstance.facility).InitSlot(data, start, codescope,false);
            }
            else
            {
                //缓存Scope和标记用于常量池中的method
                Root.Add(heapInstance);
            }

           

            return Heap.AddHeapInstance(heapInstance) ;
        }

        /// <summary>
        /// 编译时分配字符串常量
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public int Complie_AllocString(string v)
        {
            RtHeapInstance heapInstance = new RtHeapInstance();
            heapInstance.TypeKind = RtHeapTypeKind.STRING;
            heapInstance.Type = null;
            heapInstance.facility = new RtPayloadString(v);

            int size = CalculMemusage(heapInstance);
            if (MemUsage + size > USAGE_LIMIT)
            {
                throw new InvalidOperationException();
            }
            MemUsage += size;

            //常量池中字符串，加入GCRoot
            Root.Add(heapInstance);

            return Heap.AddHeapInstance(heapInstance);
        }

        internal static int CalculMemusage(RtHeapInstance instance)
        {
            int u =
                8 +  //TypeKind
                8 +  //元数据指针
                8 +      //facility 指针
                (instance.facility == null ? 0 : instance.facility.Size) //facility负载本身占用内存

                ;

            return u;
        }


       
        private int thresholed;

        internal void CheckGC(ref Player.ReceiveError receiveError)
        {
            //还需要额外扫描receiveError里引用的堆对象,不要忘记

#if !DEBUG
            if (MemUsage > thresholed)
#endif
            {
                Collect(ref receiveError);         
                thresholed = MemUsage * 2;
            }

        }

        internal void ForceGC(ref Player.ReceiveError receiveError)
        {
            Collect(ref receiveError);
            thresholed = MemUsage * 2;
        }


        public void mark(RtHeapInstance obj)
        {
            if (!obj.gc_mark)
            { 
                obj.gc_mark=true;

                switch (obj.TypeKind)
                {
                    case RtHeapTypeKind.CLASS:
                    case RtHeapTypeKind.GLOBAL:
                        {
                            //递归标记子对象
                            RtPayloadScriptClass rtPayload = (RtPayloadScriptClass)obj.facility;
                            var slots = rtPayload.__get_slots_for_gc;

                            for (int j = 0; j < slots.Length; j++)
                            {
                                if (slots[j].ValueType == NaNBoxing.BoxType.HeapPtr)
                                {
                                    mark(Heap[slots[j].HeapPtr]);
                                }
                            }

                            if (rtPayload.PROPERTY_PTR != 0)
                            {
                                mark(Heap[rtPayload.PROPERTY_PTR]);
                            }

                            if (rtPayload.PROTO__PTR != 0)
                            {
                                mark(Heap[rtPayload.PROTO__PTR]);
                            }

                        }
                        break;
                    case RtHeapTypeKind.STRING:
                        break;
                    case RtHeapTypeKind.INSTANCE:
                        {
                            RtPayloadInstance rtPayload = (RtPayloadInstance)obj.facility;
                            if (rtPayload.HEAPINSTANCE_PTR != 0)
                            {
                                mark(Heap[rtPayload.HEAPINSTANCE_PTR]);
                            }
                            else
                            {
                                if (((ASInstance)obj.Type).Flags.HasFlag(ClassFlags.Struct))
                                {
                                    break;
                                }

                                CodeScope codeScope = obj.Type._link_codescope;
                                var storedate = rtPayload.GetStoreData(context.player,(ASInstance)obj.Type);
                                if (storedate.Length > 0)
                                {
                                    unsafe
                                    {
                                        fixed (byte* p = storedate)
                                        {

                                            for (int i = 0; i < codeScope.TypeLayout.Offset.Count; i++)
                                            {
                                                byte* ptr = p + codeScope.TypeLayout.Offset[i];

                                                var member = codeScope.Members[i];

                                                switch (member.TypeKind)
                                                {

                                                    case TypeKind.Boolean:
                                                    case TypeKind.SByte:
                                                    case TypeKind.Byte:
                                                    case TypeKind.Short:
                                                    case TypeKind.UShort:
                                                    case TypeKind.Int:
                                                    case TypeKind.Uint:
                                                    case TypeKind.Float:
                                                    case TypeKind.Number:
                                                        break;
                                                    case TypeKind.Fun_Void:
                                                    case TypeKind.Unknown:
#if DEBUG
                                                        throw new InvalidOperationException();
#else
														Environment.FailFast("出错了，这里跑不到"); break;
#endif
													case TypeKind.Null:
                                                        break;
                                                    case TypeKind.Any:
                                                        if (((NaNBoxing*)ptr)->ValueType == NaNBoxing.BoxType.HeapPtr)
                                                        {
                                                            mark(Heap[((NaNBoxing*)ptr)->HeapPtr]);
                                                        }
                                                        break;
                                                    case TypeKind.String:
                                                    case TypeKind.Function:
                                                    case TypeKind.Array:
                                                    case TypeKind.Vector:
                                                    case TypeKind.Namespace:
                                                    case TypeKind.Object:
                                                    case TypeKind.Class:
                                                        if (((NaNBoxing*)ptr)->ValueType == NaNBoxing.BoxType.LocalString)
                                                        {
                                                            break;
                                                        }
                                                            


#if DEBUG
                                                        if (((NaNBoxing*)ptr)->ValueType != NaNBoxing.BoxType.HeapPtr
                                                            &&
                                                            ((NaNBoxing*)ptr)->ValueType != NaNBoxing.BoxType.Null
                                                            )
                                                        {
                                                            throw new InvalidOperationException();
                                                        }

#endif
                                                        if (((NaNBoxing*)ptr)->ValueType != NaNBoxing.BoxType.Null)
                                                        {
                                                            mark(Heap[((NaNBoxing*)ptr)->HeapPtr]);
                                                        }
                                                        break;
                                                    default:
#if DEBUG
                                                        if ((ulong)member.TypeKind < (ulong)TypeKind.Object)
                                                        {
                                                            throw new InvalidOperationException();
                                                        }

                                                        if (((NaNBoxing*)ptr)->ValueType != NaNBoxing.BoxType.Null)
                                                        {
                                                            if (((NaNBoxing*)ptr)->ValueType != NaNBoxing.BoxType.HeapPtr)
                                                            {
                                                                throw new InvalidOperationException();
                                                            }
                                                        }
#endif
                                                        if (((NaNBoxing*)ptr)->ValueType != NaNBoxing.BoxType.Null)
                                                        {
                                                            mark(Heap[((NaNBoxing*)ptr)->HeapPtr]);
                                                        }
                                                        break;

                                                }


                                            }
                                        }
                                    }

                                }
                                int prop_ptr = rtPayload.PROPERTY_PTR(context.player,(ASInstance)obj.Type);
                                if (prop_ptr!= 0)
                                {
                                    mark(Heap[prop_ptr]);
                                }

                                int proto_ptr = rtPayload.PROTOTYPE(context.player,(ASInstance)obj.Type);
                                if (proto_ptr != 0)
                                { 
                                    mark(Heap[proto_ptr]);
                                }

                                if (rtPayload.wapperedObject != null)
                                {
                                    rtPayload.wapperedObject.OnGCMark(context);
                                }

                            }
                        }
                        break;
                    //case RtHeapTypeKind.CACHE_LD_CLASS:
                    //    break;
                    case RtHeapTypeKind.NAMESPACE:
                        {
                            RtPayloadNameSpace rtPayload = (RtPayloadNameSpace)obj.facility;
                            if (rtPayload.prefixPtr > 0)
                            {
                                Heap[rtPayload.prefixPtr].gc_mark = true;
                            }

                            if (rtPayload.uriPtr > 0)
                            {
                                Heap[rtPayload.uriPtr].gc_mark = true;
                            }
                        }
                        break;
                    case RtHeapTypeKind.STACK_CACHE_OBJ:
                        {
                            RtPayloadStackCache rtPayload = (RtPayloadStackCache)obj.facility;
                            if (rtPayload.searchPropertyName.ValueType == NaNBoxing.BoxType.HeapPtr )
                            {
                                mark(Heap[rtPayload.searchPropertyName.HeapPtr]);
                            }
                            if (rtPayload.searchNameSpacePtr > 0)
                            {
                                mark(Heap[rtPayload.searchNameSpacePtr]);
                            }
                            if (rtPayload.indexer_key.ValueType == NaNBoxing.BoxType.HeapPtr)
                            {
                                mark(Heap[rtPayload.indexer_key.HeapPtr]);
                            }

#if DEBUG
                            if (rtPayload.RefInstance.ValueType ==  NaNBoxing.BoxType.Fault)
                            {
                                throw new InvalidOperationException();
                            }

#endif
                            if(rtPayload.RefInstance.ValueType ==  NaNBoxing.BoxType.HeapPtr)
                            {
                                mark(Heap[rtPayload.RefInstance.HeapPtr]);
                            }

                        }
                        break;
                    case RtHeapTypeKind.MethodScope:
                        { 
                            //throw new NotImplementedException();
                            RtPayloadMethodScope rtPayload = (RtPayloadMethodScope)obj.facility;

                            if (rtPayload.cloneout_ptr != 0)
                            {
                                mark(Heap[rtPayload.cloneout_ptr]);
                            }
                            else
                            {
                                var slots = rtPayload.__get_slots_for_gc;
                                for (int i = 0; i < slots.Length; i++)
                                {
                                    if (slots[i].ValueType == NaNBoxing.BoxType.HeapPtr)
                                    {
                                        mark(Heap[slots[i].HeapPtr]);
                                    }
                                }

                                if (rtPayload.ParentPtr != 0)
                                {
                                    mark(Heap[rtPayload.ParentPtr]);
                                }

                            }

                        }
                        break;
                    case RtHeapTypeKind.CLOSURE:
                        { 
                            RtPayloadClosure rtPayload = (RtPayloadClosure)obj.facility;

                            if (rtPayload.HEAPINSTANCE_PTR != 0)
                            {
                                mark(Heap[rtPayload.HEAPINSTANCE_PTR]);
                            }
                            else
                            {
                                if (rtPayload.ScopePtr != 0)
                                {
                                    mark(Heap[rtPayload.ScopePtr]);
                                }
                                if (rtPayload.This.ValueType == NaNBoxing.BoxType.HeapPtr)
                                {
                                    mark(Heap[rtPayload.This.HeapPtr]);
                                }
                                int prop_ptr = rtPayload.PROPERTY_PTR(context.player);
                                if (prop_ptr != 0)
                                {
                                    mark(Heap[prop_ptr]);
                                }
                                int proto = rtPayload.PROTOTYPE(context.player);
                                if (proto > 0) //有可能小于0，表示用户手动设置成undefined或null
                                {
                                    mark(Heap[proto]);
                                }
                            }
						}
                        break;
                    case RtHeapTypeKind.VECTOR:
                        { 
                            RtPayloadVector rtPayload = (RtPayloadVector)obj.facility;
                            if (rtPayload.HEAPINSTANCE_PTR != 0)
                            {
                                mark(Heap[rtPayload.HEAPINSTANCE_PTR]);
                            }
                            else
                            {
                                if (rtPayload.element_type == 0 || (rtPayload.element_type >= TypeKind.Object))
                                {
                                    if (rtPayload.HEAPINSTANCE_PTR != 0)
                                    {
                                        mark(Heap[rtPayload.HEAPINSTANCE_PTR]);
                                    }
                                    else
                                    {
                                        rtPayload.GCMarkAllElements(context);
                                    }
                                }
                            }
                            //非指针类型，不用扫描
                        }
                        break;
                    case RtHeapTypeKind.ARRAY:
                        {
                            RtPayloadArray rtPayload = (RtPayloadArray)obj.facility;
                            
                            if (rtPayload.HEAPINSTANCE_PTR != 0)
                            {
                                mark(Heap[rtPayload.HEAPINSTANCE_PTR]);
                            }
                            else
                            {
                                if (rtPayload.GetLength(context.player) > 0)
                                {
                                    rtPayload.GCMarkAllElements( context );
                                    //throw new NotImplementedException();
                                }
                                int prop_ptr = rtPayload.PROPERTY_PTR(context.player);
                                if (prop_ptr != 0)
                                {
                                    mark(Heap[prop_ptr]);
                                }
                            }
                        }
                        break;
                    case RtHeapTypeKind.SHAPE:
                        {
							//RtPayloadShape rtPayload = (RtPayloadShape)obj.facility;

#if DEBUG
                            throw new InvalidOperationException();
#else
							Environment.FailFast("出错了，这里跑不到"); break;
#endif
						}
						
                    case RtHeapTypeKind.DYNAMIC_PROPERTYS:
                        { 
                            RtPayloadDynamic rtPayload = (RtPayloadDynamic)obj.facility;
                            for (int i = 0; i < rtPayload.Slots.Count; i++)
                            {
                                NaNBoxing box = rtPayload.Slots[i];
                                if (box.ValueType == NaNBoxing.BoxType.HeapPtr)
                                {
                                    mark(Heap[box.HeapPtr] );
                                }
                            }

                        }
                        break;
                    default:
#if DEBUG
                    throw new InvalidOperationException();
#else
						Environment.FailFast("出错了，这里跑不到"); break;
#endif
				}




			}
        }

        private void Collect(ref Player.ReceiveError receiveError)
        {

            if (receiveError.raised && receiveError.error.ValueType == NaNBoxing.BoxType.HeapPtr)
            {
                mark(Heap[receiveError.error.HeapPtr]);
            }

            //遍历stackslots
            for (int i = context.StackPosition-1;i>=0;i--) 
            {
                if (context.StackSlots[i].ValueType == NaNBoxing.BoxType.HeapPtr)
                {
                    mark(Heap[context.StackSlots[i].HeapPtr]);
                }
            }

            //遍历MethodScopes
            for (int i = 0; i < context.BackTraceIndex; i++)
            {
                var methodscope = Heap[context.M_MethodScopePtr + i];

                mark(methodscope);

            }

            //遍历IterCtx
            for (int i = 0; i < cache_iter_ctx.Count ; i++)
            {
                var iterctx = cache_iter_ctx[i];
                mark(iterctx);
            }

            //遍历回调
			context.AsyncCallbackQueue.OnGCMark(context);

			//遍历微任务循环
			context.MicroTaskQueue.OnGCMark(context);


			//遍历Root。
			for (int i = 0; i < Root.Count; i++)
            {
                var instance = Root[i];

#if FORCOMPILER
                bool marked = instance.gc_mark;
#endif
                instance.gc_mark = true;

                switch (instance.TypeKind)
                {
                    case RtHeapTypeKind.CLASS:
                    case RtHeapTypeKind.GLOBAL:
                        {
                            //递归标记子对象
                            RtPayloadScriptClass rtPayload = (RtPayloadScriptClass)instance.facility;
                            var slots = rtPayload.__get_slots_for_gc;

                            for (int j = 0; j < slots.Length; j++)
                            {
                                if (slots[j].ValueType == NaNBoxing.BoxType.HeapPtr)
                                {
                                    mark(Heap[slots[j].HeapPtr]);
                                }
                            }

                            if (rtPayload.PROPERTY_PTR != 0)
                            {
                                mark(Heap[rtPayload.PROPERTY_PTR]);
                            }

                            if (rtPayload.PROTO__PTR != 0)
                            {
                                mark(Heap[rtPayload.PROTO__PTR]);
                            }

                        }
                        break;
                    case RtHeapTypeKind.INSTANCE:
                        {
                            //在遍历Root时略过遍历子对象，因为InstanceCache只会在遍历堆栈时有效

#if FORCOMPILER
                            if (!context.player.iscomputing_initvalue) //计算初始化值时需要保存临时对象不被回收
                            {
                                if (i >= root_cache_count) //instance不可能直接加入到Root中    //!((RtPayloadInstance)instance.facility).isCache)
                                {
                                    throw new InvalidOperationException();
                                }
                            }
                            else
                            {
                                if (i >= root_cache_count)
                                {
                                    instance.gc_mark = marked;
                                    mark(instance);
                                }
                            }

#else

#if DEBUG
                            if (i >= root_cache_count) //instance不可能直接加入到Root中    //!((RtPayloadInstance)instance.facility).isCache)
                            {
                                throw new InvalidOperationException();
                            }
                            else if(((RtPayloadInstance)instance.facility).wapperedObject != null)
                            {
                                throw new InvalidOperationException();
                            }

#endif

#endif
							break;
                        }
                    case RtHeapTypeKind.NAMESPACE:

                        {
                            RtPayloadNameSpace rtPayload = (RtPayloadNameSpace)instance.facility;

                            if (rtPayload.prefixPtr > 0)
                            {
                                Heap[rtPayload.prefixPtr].gc_mark = true;
                            }

                            if (rtPayload.uriPtr > 0)
                            {
                                Heap[rtPayload.uriPtr].gc_mark = true;
                            }

                        }


                        break;
                    case RtHeapTypeKind.STRING:
                        break;
                    //case RtHeapTypeKind.CACHE_LD_CLASS:
                    //    break;
                    case RtHeapTypeKind.STACK_CACHE_OBJ:
                    case RtHeapTypeKind.MethodScope:
					case RtHeapTypeKind.CLOSURE:
                        //在遍历Root时略过遍历子对象，因为STACK_CACHE_OBJ只会在遍历堆栈时有效，
                        //MethodScope只会在变量遍历时有效。
                        //CLOSURE 只会在变量遍历时有效。
                        //这里只是防止回收Root里的缓存对象
                        break;
                    case RtHeapTypeKind.ARRAY:
                    case RtHeapTypeKind.VECTOR:
                        //Root中的Array和Vector只有在栈上被访问到时才表示引用了对象
                        break;
                    case RtHeapTypeKind.SHAPE:
                        RtPayloadShape shape = (RtPayloadShape)instance.facility;
                        // 标记属性名 - 支持LocalString和HeapPtr
                        if (shape.PTR_NAME.ValueType == NaNBoxing.BoxType.HeapPtr && shape.PTR_NAME.HeapPtr != 0)
                        {
                            Heap[shape.PTR_NAME.HeapPtr].gc_mark = true;
                        }
                        break;
                    default:
#if DEBUG
                        throw new InvalidOperationException();
#else
						Environment.FailFast("出错了，这里跑不到"); break;
#endif
				}

			}



            //清理未mark的对象
            int released = Heap.Clean();
            MemUsage -= released;

        }

		
	}
}
