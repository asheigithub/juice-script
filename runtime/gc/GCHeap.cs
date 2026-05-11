using juicescript.ABC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.runtime.gc
{

    public class GCHeap
    {
        private List<RtHeapBase> Heap;
        private List<int> freeIndexes;

        public RtHeapBase this[int index]
        {
            get 
            { 
                return Heap[index];
            }
        }

#if DEBUG
        private static WeakReference<GCHeap> reference;

        static GCHeap()
        {
			NaNBoxing._setheapptr_validator = (ptr, kind) => {

				GCHeap heap;
				if (reference !=null && reference.TryGetTarget(out heap))
				{
					if (ptr != 0 && kind != NaNBoxing.UNKNOWN_HEAPKIND)
					{
						Debug.Assert((byte)heap[ptr].Kind == kind);
					}
				}
			};
		}

#endif




        public GCHeap()
        { 
            Heap = new List<RtHeapBase>(65536*2);


            //此处需要1个哨兵空槽
            //0 --表示尚未初始化的Class | Global,如果取值会返回null
            
            Heap.Add(null);
            
            freeIndexes = new List<int>(65536*2);

            //ASClass_obj = new Dictionary<ulong, RtHeapInstance>();

#if DEBUG
            reference = new WeakReference<GCHeap>(this);
#endif


		}

		private int cacheCount;

        internal void MarkCaches()
        {
            cacheCount = Heap.Count;
        }

        public IEnumerable<RtHeapBase> DumpHeap()
        {
            return Heap.Skip(cacheCount).Where( o=>o != null );
        }

        public bool IsClassProtoType(RtHeapBase obj)
        {
            return Heap.Any(
                o => o !=null && o.Kind == RtHeapTypeKind.CLASS && ((RtScriptClass)o).PROTO__PTR != 0 &&
                ReferenceEquals( Heap[((RtScriptClass)o).PROTO__PTR] , obj));
        }

        
        internal int AddHeapInstance(RtHeapBase instance)
        {
#if DEBUG
            if (instance == null)
                throw new ArgumentNullException();
#endif
            //此处如果不回收索引，当GC有bug时会出现null异常，便于排查
#if RELEASEPLAYER
            if (freeIndexes.Count > 0)
            {
                int index = freeIndexes[freeIndexes.Count - 1];
                freeIndexes.RemoveAt(freeIndexes.Count - 1);

                Heap[index] = instance;

                return index;
            }
            else
#endif
            { 
                Heap.Add(instance);
                return Heap.Count - 1;
            }

        }



        /// <summary>
        /// 清除未标记对象
        /// </summary>
        internal int Clean()
        {
            int sub = 0;
            for (int i = 0; i < Heap.Count; i++)
            {
                if (Heap[i] != null)
                {
                    if (Heap[i].gc_mark)
                    {
                        Heap[i].gc_mark = false;
                    }
                    else
                    {
                        sub += GC.CalculMemusage(Heap[i]);

                        if (Heap[i].Kind == RtHeapTypeKind.INSTANCE && ((RtInstance)Heap[i]).wapperedObject != null)
                        {
                            ((RtInstance)Heap[i]).wapperedObject.OnDelete();
							((RtInstance)Heap[i]).wapperedObject = null;
						}

                        //Heap[i].facility = null;
                        Heap[i] = null;
                        freeIndexes.Add(i);
                    }
                }
            }

            return sub;
        }

        
    }
}
