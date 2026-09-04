using juicescript.ABC;
using juicescript.runtime.buildin;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.runtime.gc
{
#if FORCOMPILER
	internal
#else
    public
#endif
	class GC
    {
        public GCHeap Heap;

        public List<RtHeapBase> Root;

        //private int u;
        public int MemUsage;
        //{
        //    get { return u; }
        //    set { 
        //        u=value;
        //        Debug.Assert(u > CacheUsage);
        //    }
        //}

        private int CacheUsage;

        public readonly int USAGE_LIMIT;

        private Context context;

        public GC(Context context, int limit = int.MaxValue)
        {
            Heap = new GCHeap();
            Root = new List<RtHeapBase>();
            this.context = context;
            USAGE_LIMIT = limit;

            thresholed = 1024;

        }

        private int root_cache_count;
        internal void MarkCaches()
        {
            Heap.MarkCaches();
            root_cache_count = Root.Count;

            CacheUsage = MemUsage;

        }

        

        /// <summary>
        /// 更新内存使用 减去某个实例的占用数
        /// </summary>
        /// <param name="instance"></param>
        public void UpdateMemUsage_Sub(RtHeapBase instance)
        { 
            MemUsage -= CalculMemusage(instance);
        }

        /// <summary>
        /// 更新内存使用 加上某个实例的占用数
        /// </summary>
        /// <param name="instance"></param>
        public void UpdateMemUsage_Add(RtHeapBase instance)
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
            RtHeapBase heapInstance = new RtShape();
            //heapInstance.TypeKind = RtHeapTypeKind.SHAPE;
           // heapInstance = new RtPayloadShape();

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
            RtHeapBase heapInstance = new RtDynamic();
            //heapInstance.TypeKind = RtHeapTypeKind.DYNAMIC_PROPERTYS;
            //heapInstance = new RtPayloadDynamic();

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
            RtHeapBase _protoObj;
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
                if (((RtInstance)_protoObj).PROTOTYPE(context.player,(ASInstance)_protoObj.Type) != 0)
                {
                    throw new InvalidOperationException();
                }
            }

#endif

            RtHeapBase heapInstance = new RtScriptClass(cls, _protoPtr)
#if FORCOMPILER
            { isCompiling = context.player.IsComputeConstExpr }
#endif                 
                ;
            //heapInstance.TypeKind = RtHeapTypeKind.CLASS;
            heapInstance.Type = context.CLASS;
            //heapInstance = new RtPayloadScriptClass(cls, _protoPtr)


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
            RtHeapBase heapInstance = new RtScriptClass(script)
#if FORCOMPILER
            {  isCompiling = context.player.IsComputeConstExpr }
#endif                   
                ;
           
            heapInstance.Type = script.Initializer.Body; // type设定为初始化函数
           
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
        internal int AllocArray(out RtHeapBase instance, RtArray.ArrayStoreMode storeMode)
        {
            RtHeapBase heapInstance = new RtArray();
           
            RtArray payload = (RtArray)heapInstance;
            payload.StoreMode = storeMode;

            switch (storeMode)
            {
                case RtArray.ArrayStoreMode.cache_on_stack:
                    heapInstance.Type = null;
                    Root.Add(heapInstance);
                    break;
                case RtArray.ArrayStoreMode.cache:
                    heapInstance.Type = null;
                    
                    

                    Root.Add(heapInstance);
                    break;
                case RtArray.ArrayStoreMode.normal:
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
        public int AllocInstance(ASInstance type,out RtHeapBase out_instance)
        {
            RtHeapBase heapInstance;// = new RtHeapBase();
            
            if (type.Flags.HasFlag(ClassFlags.Vector))
            {

                heapInstance = new RtVector(type._element_class);
              
                heapInstance.Type = type;

            }
            else
            {

				heapInstance = new RtInstance()
#if FORCOMPILER
                {  isCompiling = context.player.IsComputeConstExpr}
#endif
                    ;
				heapInstance.Type = type;

				TypeLayout typeLayout = type._link_codescope.TypeLayout;

                RtInstance payload = (RtInstance)heapInstance;

				payload.GenStore(typeLayout.Size);

                if (typeLayout.ASType.__instance_index__ > 0)
                {
                    payload.Set_PROTOTYPE( ((RtScriptClass)Heap[ typeLayout.ASType.__instance_index__]).PROTO__PTR , context.player);
                }

                if (typeLayout.Size > 0)
                {
                    payload.Init(type._link_codescope,context.player,true);
                }
                //heapInstance = payload;
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
			
			RtVector payload = new RtVector(null);
			
            payload.SetStore( new buildin.VectorImpl.VectorStore() );

			int size = CalculMemusage(payload);
			if (MemUsage + size > USAGE_LIMIT)
			{
				return 0;
			}
			MemUsage += size;

			Root.Add(payload);

			return Heap.AddHeapInstance(payload);
		}

        public int AllocCacheInstance()
        {
            
            RtInstance payload = new RtInstance();//true);
            payload.GenStore(RtInstance.MAX_CACHEABLE_SIZE);
           
            
            int size = CalculMemusage(payload);
            if (MemUsage + size > USAGE_LIMIT)
            {
                return 0;
            }
            MemUsage += size;

            Root.Add(payload);

            return Heap.AddHeapInstance(payload);
        }


        /// <summary>
        /// 分配字符串
        /// </summary>
        /// <param name="str"></param>
        /// <returns>返回内存堆对象序号</returns>
        public int AllocString(string str)
        {
            RtHeapBase heapInstance = new RtString(str);
           
            heapInstance.Type = context.STRING;
            
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
          
			RtHeapBase heapInstance = new RtNameSpace()
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
			
			RtHeapBase heapInstance = new RtClosure();

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


        private List<RtHeapBase> cache_iter_ctx = new List<RtHeapBase>();
        private int cache_iter_ctx_index;

        internal int IterCtxIndex { get => cache_iter_ctx_index; }
        
        public int RentIterContext(out RtHeapBase ctx)
        {
            if (cache_iter_ctx_index < cache_iter_ctx.Count)
            {
                
                ctx = cache_iter_ctx[cache_iter_ctx_index];
				cache_iter_ctx_index++;

				return ((IterContxt)((RtInstance)ctx).wapperedObject).heapPtr;
            }
            else
            {
                
				var cls = context.IITERATOR._link_codescope.Parent.Container.Traits[2].Class;

				RtHeapBase iterctx;
				var p = AllocInstance(cls.Instance, out iterctx);
				if (p != 0)
				{
					((RtInstance)iterctx).wapperedObject = new IterContxt() { heapPtr=p };
				}

				ctx = iterctx;

				cache_iter_ctx.Add(iterctx);
				cache_iter_ctx_index = cache_iter_ctx.Count;

				return p;
			}

		}

        public void ReturnIterContext(RtHeapBase iter_ctx)
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

            ((IterContxt)((RtInstance)iter_ctx).wapperedObject).Close();

        }

        public void ReturnIterContextWhenGetIterFailed()
        {
			cache_iter_ctx_index--;
		}

        internal IterContxt CurrentIterContext()
        {
            return  (IterContxt)((RtInstance)cache_iter_ctx[cache_iter_ctx_index - 1]).wapperedObject;

		}

		internal void ResetIterContextPool()
		{
            cache_iter_ctx_index = 0;
		}


		public int AllocStackCache()
        {
			//RtHeapBase heapInstance = new RtHeapBase();
			//heapInstance.TypeKind = RtHeapTypeKind.STACK_CACHE_OBJ;
			//heapInstance.Type = null;
			RtHeapBase heapInstance = new RtStackCache();

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

			RtHeapBase heapInstance = new RtMethodScope()

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
                ((RtMethodScope)heapInstance).InitSlot(data, start, codescope,false);
            }
            else
            {
                //缓存Scope和标记用于常量池中的method
                Root.Add(heapInstance);
            }

           

            return Heap.AddHeapInstance(heapInstance) ;
        }

#if FORCOMPILER
        /// <summary>
        /// 编译时分配字符串常量
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public int Complie_AllocString(string v)
        {

			RtHeapBase heapInstance = new RtString(v);

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

#endif

        internal static int CalculMemusage(RtHeapBase instance)
        {
            int u =
                8 +  //TypeKind
                8 +  //元数据指针
                8 +      //facility 指针
                (instance == null ? 0 : instance.Size) //facility负载本身占用内存

                ;

            return u;
        }


       
        private int thresholed;

		[MethodImpl(MethodImplOptions.AggressiveInlining )]
		internal void CheckGC(ref Player.ReceiveError receiveError)
        {
            //还需要额外扫描receiveError里引用的堆对象,不要忘记


#if !DEBUG //|| true
            if (MemUsage - CacheUsage > thresholed)
#endif
            {
                //int before = MemUsage;

                Collect(ref receiveError);         
                thresholed = (MemUsage - CacheUsage) * 2;


                //Console.WriteLine($"GC {before}->{MemUsage}");

            }

        }

        internal void ForceGC(ref Player.ReceiveError receiveError)
        {
            
            Collect(ref receiveError);
            thresholed = (MemUsage - CacheUsage) * 2;
        }


        public void mark(RtHeapBase obj)
        {
            if (!obj.gc_mark)
            { 
                obj.gc_mark=true;

                switch (obj.Kind)
                {
                    case RtHeapTypeKind.CLASS:
                    case RtHeapTypeKind.GLOBAL:
                        {
                            //递归标记子对象
                            RtScriptClass rtPayload = (RtScriptClass)obj;
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
                            RtInstance rtPayload = (RtInstance)obj;
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
													case TypeKind.Object:

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
                            RtNameSpace rtPayload = (RtNameSpace)obj;
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
                            RtStackCache rtPayload = (RtStackCache)obj;
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
                            RtMethodScope rtPayload = (RtMethodScope)obj;

                            if (rtPayload.cloneout_ptr != 0)
                            {
                                mark(Heap[rtPayload.cloneout_ptr]);
                            }
                            else
                            {
                                var slots = rtPayload.__get_slots_internal;
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
                            RtClosure rtPayload = (RtClosure)obj;

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
                            RtVector rtPayload = (RtVector)obj;
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
                            RtArray rtPayload = (RtArray)obj;
                            
                            if (rtPayload.HEAPINSTANCE_PTR != 0)
                            {
                                mark(Heap[rtPayload.HEAPINSTANCE_PTR]);
                            }
                            else
                            {
                                if (rtPayload.array_len>0)//(context.player, out RtArray t) > 0)
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
							//RtPayloadShape rtPayload = (RtPayloadShape)obj;

#if DEBUG
                            throw new InvalidOperationException();
#else
							Environment.FailFast("出错了，这里跑不到"); break;
#endif
						}
						
                    case RtHeapTypeKind.DYNAMIC_PROPERTYS:
                        { 
                            RtDynamic rtPayload = (RtDynamic)obj;
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


        private Stack<NaNBoxing[]> temporyholder = new Stack<NaNBoxing[]>();

		internal void PushTemporyHolder(NaNBoxing[] sortfields,int len)
		{
            sortfields.AsSpan(len).Clear();
			temporyholder.Push(sortfields);
		}

		internal object PopTemporyHolder()
		{
            return temporyholder.Pop();
		}


		private void Collect(ref Player.ReceiveError receiveError)
        {

            if (receiveError.raised && receiveError.error.ValueType == NaNBoxing.BoxType.HeapPtr)
            {
                mark(Heap[receiveError.error.HeapPtr]);
            }

            //遍历临时保持对象
            foreach (var item in temporyholder)
            {
                for (int i = 0; i < item.Length; i++)
                {
                    if (item[i].ValueType == NaNBoxing.BoxType.HeapPtr)
                    {
                        mark(Heap[item[i].HeapPtr] );
                    }
                }
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
                var methodscope = Heap[Context.M_MethodScopePtr + i];

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

            //定时器任务回调
            context.TimerTaskQueue.OnGCMark(context);


			//遍历Root。
			for (int i = root_cache_count; i < Root.Count; i++)
            {
                var instance = Root[i];

#if FORCOMPILER
                bool marked = instance.gc_mark;
#endif
                instance.gc_mark = true;

                switch (instance.Kind)
                {
                    case RtHeapTypeKind.CLASS:
                    case RtHeapTypeKind.GLOBAL:
                        {
                            //递归标记子对象
                            RtScriptClass rtPayload = (RtScriptClass)instance;
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
                                if (i >= root_cache_count) //instance不可能直接加入到Root中    //!((RtPayloadInstance)instance).isCache)
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
                            if (i >= root_cache_count) //instance不可能直接加入到Root中    //!((RtPayloadInstance)instance).isCache)
                            {
                                throw new InvalidOperationException();
                            }
                            else if(((RtInstance)instance).wapperedObject != null)
                            {
                                throw new InvalidOperationException();
                            }

#endif

#endif
							break;
                        }
                    case RtHeapTypeKind.NAMESPACE:

                        {
                            RtNameSpace rtPayload = (RtNameSpace)instance;

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
                        RtShape shape = (RtShape)instance;
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
            int released = Heap.Clean(root_cache_count);
            MemUsage -= released;

        }

		
	}
}
