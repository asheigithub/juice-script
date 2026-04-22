using juicescript.ABC;
using juicescript.runtime.buildin;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace juicescript.runtime
{
    /// <summary>
    /// 运行时Vector对象的负载
    /// </summary>
    internal class RtPayloadVector : FacilityBase
    {

        /// <summary>
        /// Vector缓存的最大大小 --Vector是原始内存，所以根据元素大小，可保存的元素数量也不同。 
        /// </summary>
		public const int MAX_CACHE_SIZE = 16 * 8 * 4;

		public ASClass element_asclass;

        public TypeKind element_type;


        public RtPayloadVector( ASClass element_type ) 
        { 
            if (element_type == null)
            {
                this.element_asclass = null;
                this.element_type = TypeKind.Any;
            }
            else
            {
                this.element_asclass = element_type;
                this.element_type = (TypeKind)element_type.Type_identifier;
            }
        }


        private VectorImpl.VectorStore store;
        internal void SetStore(VectorImpl.VectorStore store)
        {
            this.store = store;
        }


        internal VectorImpl.VectorStore GetStore(Player player)
        {
			RtPayloadVector target;
			FindAndUpdateHeapInstancePtr(player, out target);
            return target.store;

		}



        public override int Size
        {
            get
            {
                return 8 + 8 + (store ==null ? 0 : store.Size );
            }
        }


		/// <summary>
		/// 如果是缓存对象，并且已经被保存到堆中，则保存堆中对象的指针
		/// 后续操作将直接对堆里的对象操作了。
		/// </summary>
		internal int HEAPINSTANCE_PTR;
		internal static int FindAndUpdateHeapInstancePtr(int ptr, Player player, out RtPayloadVector target)
		{
			var payload = ((RtPayloadVector)player.Context.GC.Heap[ptr].facility);
			var origin = payload;
			target = origin;
			while (payload.HEAPINSTANCE_PTR != 0)
			{
				ptr = payload.HEAPINSTANCE_PTR;
				payload = ((RtPayloadVector)player.Context.GC.Heap[ptr].facility);
				target = payload;
				origin.HEAPINSTANCE_PTR = ptr;//更新,避免后续跳转
			}
			return ptr;
		}

        private void FindAndUpdateHeapInstancePtr(Player player, out RtPayloadVector target)
        {
            if (HEAPINSTANCE_PTR == 0)
            {
                target = this;
                
            }
            else
            {
                FindAndUpdateHeapInstancePtr(HEAPINSTANCE_PTR, player, out target);
            }
        }


		/// <summary>
		/// 仅用于跟踪缓存对象被function的slot引用的情况
		/// 见PrepareSaveMethodScope的保存逻辑
		/// 0 --表示在栈里还没有复制到任何slot的对象
		/// 1 --表示刚被复制到到slot中
		/// 2 --当状态是1的对象被另一个slot引用时，改为2.
		/// 如果状态不是2，那么 PrepareSaveMethodScope中，【保存前，先处理被覆盖前原来的内容】这步可以跳过，因为没有其他引用。
		/// 
		/// </summary>
		internal byte methodscopeslot_ref_state;




		internal Span<byte> ReadStoreAt(int validid, Player player)
        {			
			Span<byte> bytes = GetStore(player).ReadStoreAt(validid);
            return bytes;
		}

        internal Span<byte> ReadStoreOffset(int offset, Player player,int size)
        {
            Span<byte> bytes = GetStore(player).ReadStoreOffset(offset,size);
            return bytes;
		}

        /// <summary>
        /// reseveSlot需要外部保留一个槽，用于保存读取出的Vector中的struct结构体.
        /// </summary>
        /// <param name="validid"></param>
        /// <param name="player"></param>
        /// <param name="reseveSlot"></param>
        /// <param name="vector_ptr"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
		internal NaNBoxing ReadSlot(int validid, Player player, int reseveSlot , int vector_ptr)
		{
            NaNBoxing result = default; 

            var bytes = ReadStoreAt(validid, player);

            switch (element_type)
            {
                case TypeKind.Any:
                    result = MemoryMarshal.Read<NaNBoxing>(bytes);
                    return result;
                case TypeKind.Boolean:
                    result.SetBoolean( MemoryMarshal.Read<bool>(bytes) );
                    return result;
                    
                case TypeKind.SByte:
					result.SetSByte(MemoryMarshal.Read<sbyte>(bytes));
					return result;
				case TypeKind.Byte:
					result.SetByte(MemoryMarshal.Read<byte>(bytes));
					return result;
				case TypeKind.Short:
					result.SetShort(MemoryMarshal.Read<short>(bytes));
                    return result;
                case TypeKind.UShort:
					result.SetUShort(MemoryMarshal.Read<ushort>(bytes));
					return result;
				case TypeKind.Int:
                    result.SetInt(MemoryMarshal.Cast<byte, int>(bytes)[0] );
                    return result;
                case TypeKind.Uint:
					result.SetUInt(MemoryMarshal.Cast<byte, uint>(bytes)[0]);
					return result;
				case TypeKind.Float:
					result.SetFloat(MemoryMarshal.Cast<byte, float>(bytes)[0]);
					return result;
				case TypeKind.Number:
					result.SetNumber(MemoryMarshal.Cast<byte, double>(bytes)[0]);
					return result;
                case TypeKind.Fun_Void:
                case TypeKind.TraitDataReference:
                case TypeKind.RTQName_MultiName_DataReference:
                case TypeKind.CParseNS_Traits:
                case TypeKind.RTQNameRTQNameL_N:
                case TypeKind.SearchNameSpaceFromImports:
                case TypeKind.Unknown:
                case TypeKind.Null:
				case TypeKind.Super:
#if DEBUG
					throw new InvalidOperationException();
#else
					Environment.FailFast("出错了，这里跑不到"); return default;
#endif
				case TypeKind.Object:
                case TypeKind.Class:
                case TypeKind.String:
                case TypeKind.Function:
                case TypeKind.Array:
                case TypeKind.Vector:
                case TypeKind.Namespace:
                    {
                        result = MemoryMarshal.Cast<byte, NaNBoxing>(bytes)[0];
                        return result;
                    }
                default:
                    {
                        if (element_asclass.Instance.Flags.HasFlag(ClassFlags.Struct))
                        {
							//在reseveSlot位置上
							int cache_ptr = player.Context.CacheInstancePtr + reseveSlot;
                            var cache = player.Context.GC.Heap[cache_ptr];

                            cache.Type = element_asclass.Instance;
                            RtPayloadInstance struct_payload = (RtPayloadInstance)cache.facility;
                            struct_payload.HEAPINSTANCE_PTR = 0;

                            struct_payload.methodscopeslot_ref_state = 0;
                            struct_payload.Set_PROPERTY_PTR(validid * bytes.Length, player,element_asclass.Instance); //标记偏移量.
                            struct_payload.HEAPINSTANCE_PTR = vector_ptr; //指向Vector.



                            result.SetHeapPtr(cache_ptr);
                            return result;
                        }
                        else
                        {
							result = MemoryMarshal.Cast<byte, NaNBoxing>(bytes)[0];
							return result;

						}
                    }
                    
            }

		}


        internal void SetSlot(int validid,Player player,int vector_ptr,NaNBoxing value , ref Player.ReceiveError error)
        {
			var bytes = ReadStoreAt(validid, player);

            switch (element_type)
            {
                case TypeKind.Boolean:
                    {
                        bool v = value.Boolean;
                        MemoryMarshal.Write(bytes, ref v);
                    }
                    return;
                case TypeKind.SByte:
                    {
                        sbyte v = value.SByteValue;
                        MemoryMarshal.Write(bytes, ref v);
                    }
                    return;
                case TypeKind.Byte:
					{
						byte v = value.ByteValue;
						MemoryMarshal.Write(bytes, ref v);
					}
					return;
				case TypeKind.Short:
					{
						short v = value.ShortValue;
						MemoryMarshal.Write(bytes, ref v);
					}
                    return;
                case TypeKind.UShort:
                    {
                        ushort v = value.UShortValue;
                        MemoryMarshal.Write(bytes, ref v);
                    }
                    return;
                case TypeKind.Int:
					{
						int v = value.IntValue;
						MemoryMarshal.Write(bytes, ref v);
					}
					return;
				case TypeKind.Uint:
					{
						uint v = value.UIntValue;
						MemoryMarshal.Write(bytes, ref v);
					}
					return;
				case TypeKind.Float:
					{
						float v = value.FloatValue;
						MemoryMarshal.Write(bytes, ref v);
					}
					return;
				case TypeKind.Number:
					{
						double v = value.Number;
						MemoryMarshal.Write(bytes, ref v);
					}
					return;
				case TypeKind.Fun_Void:
                case TypeKind.TraitDataReference:
                case TypeKind.RTQName_MultiName_DataReference:
                case TypeKind.CParseNS_Traits:
                case TypeKind.RTQNameRTQNameL_N:
                case TypeKind.SearchNameSpaceFromImports:
                case TypeKind.Unknown:
				case TypeKind.Super:
                case TypeKind.Null:
#if DEBUG
					throw new InvalidOperationException();
#else
					Environment.FailFast("出错了，这里跑不到"); return;
#endif
				case TypeKind.Any:
				case TypeKind.Object:
                case TypeKind.Class:
                case TypeKind.String:
                case TypeKind.Function:
                case TypeKind.Array:
                case TypeKind.Vector:
                case TypeKind.Namespace:
					{
                        NaNBoxing v = player.GetSaveValue(value, ref error);
                        if (error.raised)
                        {
                            return;
                        }
						MemoryMarshal.Write(bytes, ref v);
						return;
					}
				default:
					{
						if (element_asclass.Instance.Flags.HasFlag(ClassFlags.Struct))
						{
                            if (value.ValueType != NaNBoxing.BoxType.HeapPtr)
                            {
                                VectorImpl.VectorStore.InitStructSpan(bytes, element_asclass);
                                return;
                            }
                            else
                            {
#if DEBUG
                                if (value.ValueType != NaNBoxing.BoxType.HeapPtr)
                                {
                                    throw new InvalidOperationException();
                                }
#endif

								RtPayloadInstance src = ((RtPayloadInstance)player.Context.GC.Heap[value.HeapPtr].facility);
                                src.GetStoreData(player,(ASInstance)element_asclass.Instance).Slice(0, bytes.Length).CopyTo(bytes);
                                return;
                            }
						}
						else
						{
							NaNBoxing v = player.GetSaveValue(value, ref error);
							if (error.raised)
							{
								return;
							}
							MemoryMarshal.Write(bytes, ref v);
                            return;
						}
					}
			}

          
        }






		public static bool IsValidIndexType(NaNBoxing index)
        {
            switch (index.ValueType)
            {
                case NaNBoxing.BoxType.Number:
                case NaNBoxing.BoxType.Int:
                case NaNBoxing.BoxType.Uint:
                case NaNBoxing.BoxType.Sbyte:
                case NaNBoxing.BoxType.Byte:
                case NaNBoxing.BoxType.Short:
                case NaNBoxing.BoxType.UShort:
                case NaNBoxing.BoxType.Float:
                    return true;
				case NaNBoxing.BoxType.Boolean:
				case NaNBoxing.BoxType.HeapPtr:
                case NaNBoxing.BoxType.Fault:
				case NaNBoxing.BoxType.Undefined:
				case NaNBoxing.BoxType.Null:
				default:
                    return false;
            }
        }

        public bool IsValidIndexRange(NaNBoxing index, out int valididx, out int maxlen , Player player)
        {
            var store = GetStore(player);
            maxlen = store.length;
            return store.IsValidIndexRange(index,out valididx) ;
        }

		
        internal void GCMarkAllElements(Context context)
        {
#if DEBUG
            if (HEAPINSTANCE_PTR != 0)
            {
                throw new InvalidOperationException();
            }
#endif
            if (store == null)
            {
                return;
            }

			switch (element_type)
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
                    return;
				case TypeKind.Any:
				case TypeKind.Object:
				case TypeKind.Class:
				case TypeKind.String:
				case TypeKind.Function:
				case TypeKind.Array:
				case TypeKind.Vector:
				case TypeKind.Namespace:
					break;
				case TypeKind.Super:
				case TypeKind.Fun_Void:
				case TypeKind.TraitDataReference:
				case TypeKind.RTQName_MultiName_DataReference:
				case TypeKind.CParseNS_Traits:
				case TypeKind.RTQNameRTQNameL_N:
				case TypeKind.SearchNameSpaceFromImports:
				case TypeKind.Unknown:
				case TypeKind.Null:
#if DEBUG
					throw new InvalidOperationException();
#else
					Environment.FailFast("出错了，这里跑不到"); return;
#endif
				default:
					{
						if (element_asclass.Instance.Flags.HasFlag(ClassFlags.Struct))
						{
                            return;
						}
					}
					break;
			}

            store.GCMarkAllElements(context);
			
        }

		internal void Trace(Context context, int stackStPos, ref Player.ReceiveError error, int scope_ptr, IPrint printer)
		{
            var store = GetStore(context.player);

            store.DoTrace(element_type, element_asclass,  context,  stackStPos, ref error,  scope_ptr, printer);

		}

		internal void CopyCacheFrom(RtPayloadVector vector, Player player)
		{
#if DEBUG
            if (vector.HEAPINSTANCE_PTR != 0)
            {
                throw new InvalidOperationException();
            }

#endif
            
            element_asclass = vector.element_asclass;
            element_type = vector.element_type;

            store.CopyFrom(vector.store);

		}

		internal int ChangeStoreToHeap(ASInstance type, Player player, ref Player.ReceiveError error)
		{
            RtHeapInstance heap_vector;
            int heap_ptr = player.Context.GC.AllocInstance(type, out heap_vector);
            if (heap_ptr == 0)
            { 
                player.RaiseOutOfMemory(ref error);
                return 0;
            }

            Debug.Assert(store == GetStore(player));

			if (player.Context.GC.MemUsage +  store.length * store.elementSize > player.Context.GC.USAGE_LIMIT)
			{
				player.RaiseOutOfMemory(ref error);
				return 0;
			}

			((RtPayloadVector)heap_vector.facility).SetStore ( new VectorImpl.VectorStore(store));

            //链接到堆对象, 堆对象此时被此对象链接
            HEAPINSTANCE_PTR = heap_ptr;

            return heap_ptr;
		}

		internal void Resize(int newlen, ref Player.ReceiveError error, Player player , ASInstance vtype)
		{
            var store = GetStore(player);
            if (newlen <= store.length)
            {
                store.buffer.RemoveRange(newlen * store.elementSize, (store.length - newlen) * store.elementSize);

                if (store.IsCache)
                {
					player.Context.GC.MemUsage += (newlen - store.length) * store.elementSize; //更新内存占用计数
				}

                store.length = newlen;
            }
            else if (store.IsCache)
            {
                if (store.elementSize == 0)
                {
                    store.length = newlen;
                }
                else
                {
                    if (newlen * store.elementSize <= MAX_CACHE_SIZE)
                    {
                        store.buffer.AddRange( Enumerable.Repeat<byte>(0, (newlen - store.length) * store.elementSize) );
                        
                        store.SetDefault(element_type, element_asclass, store.length, newlen - store.length);
                        store.length = newlen;
                    }
                    else
                    {
                        ChangeStoreToHeap(vtype, player, ref error);
                        if (error.raised)
                        {
                            return;
                        }

                        store = GetStore(player); //获取新的store;
                        goto lbl_heap;

                    }
                }
            }
            else
            {
                goto lbl_heap;
            }

            return;
        lbl_heap:

            if (player.Context.GC.MemUsage + (newlen - store.length) * store.elementSize > player.Context.GC.USAGE_LIMIT)
            {
                player.RaiseOutOfMemory(ref error);
                return;
            }

			store.buffer.AddRange(Enumerable.Repeat<byte>(0, (newlen - store.length) * store.elementSize));
			player.Context.GC.MemUsage += (newlen - store.length) * store.elementSize; //更新内存占用计数


			store.SetDefault(element_type, element_asclass, store.length, newlen - store.length);
			store.length = newlen;

            

		}
	}
}
