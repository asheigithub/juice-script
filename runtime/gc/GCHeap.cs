using juicescript.ABC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.runtime.gc
{

    public class GCHeap
    {
        private List<RtHeapInstance> Heap;
        private List<int> freeIndexes;

        public RtHeapInstance this[int index]
        {
            get 
            { 
                return Heap[index];
            }
        }


        public GCHeap()
        { 
            Heap = new List<RtHeapInstance>(65536*2);


            //此处需要1个哨兵空槽
            //0 --表示尚未初始化的Class | Global,如果取值会返回null
            
            Heap.Add(null);
            
            freeIndexes = new List<int>(65536*2);

            //ASClass_obj = new Dictionary<ulong, RtHeapInstance>();
        }

        private int cacheCount;

        internal void MarkCaches()
        {
            cacheCount = Heap.Count;
        }

        public IEnumerable<RtHeapInstance> DumpHeap()
        {
            return Heap.Skip(cacheCount).Where( o=>o != null );
        }

        public bool IsClassProtoType(RtHeapInstance obj)
        {
            return Heap.Any(
                o => o !=null && o.TypeKind == RtHeapTypeKind.CLASS && ((RtPayloadScriptClass)o.facility).PROTO__PTR != 0 &&
                ReferenceEquals( Heap[((RtPayloadScriptClass)o.facility).PROTO__PTR] , obj));
        }

        
        internal int AddHeapInstance(RtHeapInstance instance)
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

                        if (Heap[i].TypeKind == RtHeapTypeKind.INSTANCE && ((RtPayloadInstance)Heap[i].facility).wapperedObject != null)
                        {
                            ((RtPayloadInstance)Heap[i].facility).wapperedObject.OnDelete();
							((RtPayloadInstance)Heap[i].facility).wapperedObject = null;
						}

                        Heap[i].facility = null;
                        Heap[i] = null;
                        freeIndexes.Add(i);
                    }
                }
            }

            return sub;
        }

        
    }
}
