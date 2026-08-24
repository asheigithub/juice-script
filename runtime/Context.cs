using juicescript.ABC;
using juicescript.runtime.buildin;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static juicescript.runtime.buildin.PromiseImpl;
using static juicescript.runtime.Player;

namespace juicescript.runtime
{
    /// <summary>
    /// 运行时上下文
    /// </summary>
#if FORCOMPILER
    internal
#else
    public
#endif
    class Context
    {

        public const int STACK_LENGTH = 512;

#if DEBUG
		public const int MAX_BACKTRACE = 16;
#else
		public const int MAX_BACKTRACE = 50;
#endif
		public const int MAX_TRY_NESTED = 16;

        public readonly Player player;

        /// <summary>
        /// 已加载并已链接的库
        /// </summary>
        public List<SWCFile> libs = new List<SWCFile>();

        public Dictionary<ASMultiname, TypeLayout> dictTypeLayouts = new Dictionary<ASMultiname, TypeLayout>();

        public Dictionary<ulong,ASClass> dictTypes = new Dictionary<ulong,ASClass>();

        public Dictionary<ASMultiname,ASClass> dictTypeQNames = new Dictionary<ASMultiname, ASClass>();

        internal List<ASVector> Vectors;

        //internal List<ASClass> link_const_class;

        internal List<ASMethod> link_const_methods;
        internal List<VTableItem> link_const_vtableitems;


        public ASClass OBJECT;
        public ASClass CLASS;
        public ASClass STRING;
        public ASClass ERROR;
        public ASClass RANGE_ERROR;
        public ASClass TYPE_ERROR;
        public ASClass REFERENCE_ERROR;
        public ASClass ARGEMENT_ERROR;
        public ASClass ILLEGALOPERATION_ERROR;


		public ASClass FUNCTION;
        public ASClass METHOD_CLOSURE;

        public ASClass IITERATOR;


		public ASClass VECTOR;
        public ASClass ARRAY;

        public ASClass BYTE;
        public ASClass SBYTE;
        public ASClass SHORT;
        public ASClass USHORT;
        public ASClass INT;
        public ASClass UINT;
        public ASClass NUMBER;
        public ASClass FLOAT;
        public ASClass BOOLEAN;

        public ASClass NAMESPACE;

        public ASClass GENERATOR;

        public ASClass PROMISE;

        public ASClass VEC2;
        public ASClass MAT22;


        public SWCFile global_swc;

        public gc.GC GC { get; private set; }


        internal NaNBoxing[] StackSlots;

        internal int StackPosition;


        /// <summary>
        /// 第一个缓存MethodScope的地址。
        /// 由于连续创建，所以索引连续
        /// 因此当索引大于 CacheObjPointer 时，说明不是缓存的。
        /// </summary>
        internal const int M_MethodScopePtr = 1;

        /// <summary>
        /// 第一个StackCache对象的地址
        /// 由于是连续创建的，所以索引是连续的。
        /// </summary>
        internal const int CacheObjPtr = M_MethodScopePtr + MAX_BACKTRACE;

        /// <summary>
        /// 第一个缓存Instance的地址
        ///  由于是连续创建的，所以索引是连续的。
        /// </summary>
        internal const int CacheInstancePtr = CacheObjPtr + STACK_LENGTH;

        
        //internal BackTraceInfo[] BackTrace;
        internal int BackTraceIndex;

        internal ErrorStackTrace errorStack;

        /// <summary>
        /// 第一个缓存的闭包的地址
        /// 由于连续创建，所以索引连续
        /// 因此当索引大于 BlankShapePtr 时，说明不是缓存的。
        /// </summary>
        internal const int M_ClosurePtr = CacheInstancePtr + STACK_LENGTH;

        
        /// <summary>
        /// 第一个缓存Rest参数的Array的地址,共MAX_BACKTRACE个
        /// </summary>
        internal const int M_RestArrayPtr = M_ClosurePtr + STACK_LENGTH;

        /// <summary>
        /// 第一个缓存Array的地址
        /// </summary>
        internal const int CacheArrayPtr = M_RestArrayPtr + MAX_BACKTRACE + STACK_LENGTH * RtArray.MAX_CACHE_ELEMENT;

        /// <summary>
        /// 第一个缓存Vector的地址
        /// </summary>
        internal const int CacheVectorPtr = CacheArrayPtr + STACK_LENGTH;


		/// <summary>
		/// 根空Shape的指针
		/// </summary>
		internal const int BlankShapePtr = CacheVectorPtr + STACK_LENGTH;



		internal PromiseMicroTaskQueue MicroTaskQueue { get;private set; }

        internal TimerTaskImpl.TimerTaskList TimerTaskQueue { get; private set; }

        internal AsyncCallbackQueue AsyncCallbackQueue { get;private set; }

        internal DateTime starttime;


        /// <summary>
        /// 第一个非缓存的指针，只要小于这个的就都是缓存对象
        /// </summary>
        public const int MIN_HEAPPTR = 1+ MAX_BACKTRACE + STACK_LENGTH + STACK_LENGTH + STACK_LENGTH + MAX_BACKTRACE + STACK_LENGTH * RtArray.MAX_CACHE_ELEMENT + STACK_LENGTH + STACK_LENGTH;


		public Context(Player player, int gc_limit = int.MaxValue)
        {
            this.player = player;
            starttime = DateTime.Now;
            
            //link_const_class = new List<ASClass>();
            link_const_methods = new List<ASMethod>();
            link_const_vtableitems = new List<VTableItem>();
            Vectors = new List<ASVector> ();


            GC = new gc.GC(this,gc_limit);
            StackSlots = new NaNBoxing[STACK_LENGTH];

            int _MethodScopePtr = GC.AllocMethodScope(null,0,null);if (_MethodScopePtr == 0) { throw new LoaderException("alloc Method Scope failed,out of memory."); }
            Debug.Assert(_MethodScopePtr == M_MethodScopePtr);

            for (int i = 1; i < MAX_BACKTRACE; i++)
            {
                if(GC.AllocMethodScope(null, 0, null)==0)
                    throw new LoaderException("alloc Method Scope failed,out of memory.");
            }

            int _CacheObjPtr = GC.AllocStackCache();if (_CacheObjPtr == 0) { throw new LoaderException("alloc CacheObjPointer failed,out of memory."); }
            Debug.Assert(_CacheObjPtr == CacheObjPtr);
            for (int i = 1; i < STACK_LENGTH; i++)
            {
                if (GC.AllocStackCache() == 0)
                    throw new LoaderException("alloc CacheObjPointer failed,out of memory.");
            }

            int _CacheInstancePtr = GC.AllocCacheInstance();if (_CacheInstancePtr == 0) { throw new LoaderException("alloc CacheInstancePtr failed,out of memory."); }
            Debug.Assert(_CacheInstancePtr == CacheInstancePtr);
            for (int i = 1; i < STACK_LENGTH; i++)
            {
                if(GC.AllocCacheInstance() == 0)
                    throw new LoaderException("alloc CacheInstancePtr failed,out of memory.");
            }


            int _ClosurePtr = GC.AllocClosure(null);if (_ClosurePtr == 0) { throw new LoaderException("alloc Closure failed,out of memory."); }
            Debug.Assert(_ClosurePtr == M_ClosurePtr);

            for (int i = 1; i < STACK_LENGTH; i++)
            {
                if (GC.AllocClosure(null) == 0)
                    throw new LoaderException("alloc Closure failed,out of memory.");
            }

            RtHeapBase arr;
            int _RestArrayPtr = GC.AllocArray(out arr, RtArray.ArrayStoreMode.cache_on_stack);if(_RestArrayPtr == 0) { throw new LoaderException("alloc M_RestArrayPtr failed,out of memory."); }
            Debug.Assert(_RestArrayPtr == M_RestArrayPtr);

            for (int i = 1; i < MAX_BACKTRACE; i++)
            {
                if (GC.AllocArray(out arr, RtArray.ArrayStoreMode.cache_on_stack) == 0)
                    throw new LoaderException("alloc M_RestArrayPtr failed,out of memory.");
            }

            int cache_struct_p =0;
            //先分配Array的缓存struct
            for (int i = 0; i < STACK_LENGTH; i++)
            {
                for (int j = 0; j < RtArray.MAX_CACHE_ELEMENT; j++)
                {
                    int cache_struct = GC.AllocCacheInstance();
                    if (cache_struct == 0)
                    {
                        throw new LoaderException("alloc CacheArrayElements failed,out of memory");
                    }
                    else if (cache_struct_p == 0)
                    {
                        cache_struct_p = cache_struct;
                    }
                }
            }
           
            int _CacheArrayPtr = GC.AllocArray(out arr, RtArray.ArrayStoreMode.cache,cache_struct_p); if (_CacheArrayPtr == 0) { throw new LoaderException("alloc CacheArrayPtr failed,out of memory."); }
            Debug.Assert(_CacheArrayPtr == CacheArrayPtr);
            
            for (int i = 1; i < STACK_LENGTH; i++)
            {
                if (GC.AllocArray(out arr, RtArray.ArrayStoreMode.cache,cache_struct_p + i* RtArray.MAX_CACHE_ELEMENT) == 0)
                    throw new LoaderException("alloc CacheArrayPtr failed,out of memory.");
            }

            int _CacheVectorPtr = GC.AllocCacheVector();if (_CacheVectorPtr == 0) { throw new LoaderException("alloc CacheVector failed,out of memory"); }
            Debug.Assert(_CacheVectorPtr == CacheVectorPtr);

            for (int i = 1; i < STACK_LENGTH; i++)
            {
                if (GC.AllocCacheVector() == 0)
                {
					throw new LoaderException("alloc CacheVector failed,out of memory");
				}
            }


            GC.MarkCaches();

			int _BlankShapePtr = GC.AllocShape(); if (_BlankShapePtr == 0) { throw new LoaderException("alloc BlankShapePtr failed,out of memory."); }

            Debug.Assert(_BlankShapePtr == MIN_HEAPPTR);
            Debug.Assert(_BlankShapePtr == BlankShapePtr);

			StackPosition = 0;

            //BackTrace = new BackTraceInfo[MAX_BACKTRACE];

            BackTraceIndex = 0;

            errorStack = new ErrorStackTrace();

			MicroTaskQueue = new PromiseMicroTaskQueue();

            TimerTaskQueue = new TimerTaskImpl.TimerTaskList();

            AsyncCallbackQueue = new AsyncCallbackQueue();

		}


	}



}
