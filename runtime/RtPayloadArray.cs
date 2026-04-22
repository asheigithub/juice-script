using juicescript.ABC;
using juicescript.runtime.buildin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static juicescript.runtime.Player;

namespace juicescript.runtime
{
    public sealed class RtPayloadArray : FacilityBase
    {
        /// <summary>
        /// 缓存数组的最大缓存数量
        /// </summary>
        public const int MAX_CACHE_ELEMENT = 16;

        public enum ArrayStoreMode 
        { 
            /// <summary>
            /// 传参时构造在栈上的数组参数
            /// </summary>
            cache_on_stack = 0,

            /// <summary>
            /// 分配在缓存对象里
            /// </summary>
            cache =1,

            /// <summary>
            /// 分配在堆里
            /// </summary>
            normal = 2

        }


        public override int Size
        {
            get
            {
                return  4 + 4 + 4 + 8 + 8 +8 + 4 +(
					StoreMode == ArrayStoreMode.cache_on_stack?0: 
					( StoreMode == ArrayStoreMode.cache?( MAX_CACHE_ELEMENT * 8 + 16 ):( sparse_map.Count * SPARSE_BLOCK_SIZE * 8 + (16 + 16) * sparse_map.Count)   ) 
					);
            }
        }


		private int m_property_ptr;
		/// <summary>
		/// 动态属性
		/// </summary>
		public int PROPERTY_PTR(Player player)
		{

			if (HEAPINSTANCE_PTR == 0)
			{
				return m_property_ptr;
			}
			else
			{
				RtPayloadArray target;
				HEAPINSTANCE_PTR = FindAndUpdateHeapInstancePtr(HEAPINSTANCE_PTR, player, out target);
				return target.m_property_ptr;

				//return ((RtPayloadInstance)player.Context.GC.Heap[HEAPINSTANCE_PTR].facility).PROPERTY_PTR(player);
			}

		}

		public void Set_PROPERTY_PTR(int ptr, Player player)
		{
			if (HEAPINSTANCE_PTR == 0)
			{
				m_property_ptr = ptr;
			}
			else
			{
				RtPayloadArray target;
				HEAPINSTANCE_PTR = FindAndUpdateHeapInstancePtr(HEAPINSTANCE_PTR, player, out target);
				target.m_property_ptr = ptr;
			}
		}


		/// <summary>
		/// 如果是缓存对象(包括argements)，并且已经被保存到堆中，则保存堆中对象的指针
		/// 后续操作将直接对堆里的对象操作了。
		/// </summary>
		internal int HEAPINSTANCE_PTR;


		internal static int FindAndUpdateHeapInstancePtr(int ptr, Player player, out RtPayloadArray target)
		{
			var payload = ((RtPayloadArray)player.Context.GC.Heap[ptr].facility);
			var origin = payload;
			target = origin;
			while (payload.HEAPINSTANCE_PTR != 0)
			{
				ptr = payload.HEAPINSTANCE_PTR;
				payload = ((RtPayloadArray)player.Context.GC.Heap[ptr].facility);
				target = payload;

				origin.HEAPINSTANCE_PTR = ptr;//更新,避免后续跳转
			}
			return ptr;
		}



		internal int stack_store_startindex;
        internal Memory<NaNBoxing> stack_store;
        

        internal NaNBoxing[] cache_store;
        internal int[] cache_structs;


		private const int SPARSE_BLOCK_SIZE = 16;
		private Dictionary<uint, NaNBoxing[]> sparse_map;

		private NaNBoxing[] GetOrCreateBlock(uint array_index)
		{
			
			uint block_index = array_index / SPARSE_BLOCK_SIZE;
			if (!sparse_map.TryGetValue(block_index, out var block))
			{
				block = new NaNBoxing[SPARSE_BLOCK_SIZE];
				for (int i = 0; i < SPARSE_BLOCK_SIZE; ++i)
					block[i].setFault();
				sparse_map[block_index] = block;
			}


			return block;
		}


		internal void InitNormalStore()
		{
			sparse_map = new Dictionary<uint, NaNBoxing[]>();
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


		internal uint array_len = 0;

        public uint GetLength(Player player)
        {
			if (HEAPINSTANCE_PTR == 0)
			{
				return array_len;
			}
			else
			{
				RtPayloadArray target;
				HEAPINSTANCE_PTR = FindAndUpdateHeapInstancePtr(HEAPINSTANCE_PTR, player, out target);
				return target.array_len;
			}
		}


		public void SetLength(uint len,Player player, ref ReceiveError error)
        {
			if (HEAPINSTANCE_PTR == 0)
			{
				DoSetLength(len, player,ref error);
			}
			else
			{
				RtPayloadArray target;
				HEAPINSTANCE_PTR = FindAndUpdateHeapInstancePtr(HEAPINSTANCE_PTR, player, out target);
				target.DoSetLength(len,player,ref error);
			}
		}

		private void DoSetLength(uint len, Player player, ref ReceiveError error)
		{
			switch (StoreMode)
			{
				case ArrayStoreMode.cache_on_stack:
					{
						if (len > stack_store.Length)
						{
							ChangeStoreToHeap(len, player,ref error);
							//throw new NotImplementedException();
						}
						else
						{
							array_len = len;
							var stack_span = stack_store.Span;
							for (uint i = array_len; i < stack_store.Length; i++)
							{
								stack_span[(int)i].setFault();
							}
						}
					}
					break;		
				case ArrayStoreMode.cache:
					{
						if (len > MAX_CACHE_ELEMENT)
						{
							ChangeStoreToHeap(len,player,ref error);
							//throw new NotImplementedException();
						}
						else
						{
							array_len = len;
							for (uint i = array_len; i < cache_store.Length; i++)
							{
								cache_store[(int)i].setFault();
							}
						}
					}
					break;
				case ArrayStoreMode.normal:
					//throw new NotImplementedException();
					{
						array_len = len;
						int oldsize = Size;

						
						foreach (var id in sparse_map.Keys.ToArray())
						{
							if (id * SPARSE_BLOCK_SIZE >= array_len)
							{ 
								sparse_map.Remove(id);
							}
						}

						player.Context.GC.UpdateMemUsage_Change(oldsize - Size);

						if (array_len > 0)
						{
							//清理最后一个块的超出array_len部分
							uint last_block = (array_len - 1) / SPARSE_BLOCK_SIZE;
							NaNBoxing[] block;
							if (sparse_map.TryGetValue(last_block, out block))
							{
								for (uint i = (array_len - 1) % SPARSE_BLOCK_SIZE + 1; i < SPARSE_BLOCK_SIZE; i++)
								{
									block[i].setFault();
								}
							}
						}

					}
					break;
				default:
#if DEBUG
					throw new InvalidOperationException();
#else
					Environment.FailFast("出错了，这里跑不到"); return;
#endif

			}
		}


		internal int ChangeStoreToHeap(Player player, ref ReceiveError error)
		{
#if DEBUG
			if (HEAPINSTANCE_PTR != 0)
				throw new InvalidOperationException();
#endif
			uint len = GetLength(player);
			return ChangeStoreToHeap(len,player,ref error);	

		}

		internal void InitHeapData(Span<NaNBoxing> init_data,Player player,ref ReceiveError error)
		{
			int usg = player.Context.GC.MemUsage - Size;
			int oldsize = Size;

			int i = 0;
			NaNBoxing[] block = null;

			//拷贝数据
			while (i < init_data.Length) //此时length有可能超出 init_data的范围。
			{
				if (i % SPARSE_BLOCK_SIZE == 0)
				{
					block = new NaNBoxing[SPARSE_BLOCK_SIZE];
					for (int j = 0; j < SPARSE_BLOCK_SIZE; j++)
						block[j].setFault();

					sparse_map.Add((uint)i / SPARSE_BLOCK_SIZE, block);
				}

				var v = init_data[i];

				if (v.ValueType == NaNBoxing.BoxType.HeapPtr)
				{
					var ins = player.Context.GC.Heap[v.HeapPtr];
					if (ins.TypeKind == RtHeapTypeKind.INSTANCE)
					{
						if (((ASInstance)ins.Type).Flags.HasFlag(ClassFlags.Struct))
						{
							RtHeapInstance struct_instance;
							//复制结构体
							int struct_ptr = player.Context.GC.AllocInstance((ASInstance)ins.Type, out struct_instance);
							if (struct_ptr == 0)
							{
								player.RaiseOutOfMemory(ref error);
								return;
							}

							CopyStruct(struct_instance, ins, player);
							v.SetHeapPtr(struct_ptr);
						}
					}
				}

				block[i % SPARSE_BLOCK_SIZE] = v;

				i++;
			}


			if (usg + Size >= player.Context.GC.USAGE_LIMIT)
			{
				player.RaiseOutOfMemory(ref error);
				return ;
			}

			player.Context.GC.UpdateMemUsage_Change(Size - oldsize);
		}


		private int ChangeStoreToHeap(uint newlen,Player player,ref ReceiveError error)
		{
			RtHeapInstance arr_instance;
			int arr_ptr = player.Context.GC.AllocArray(out arr_instance, ArrayStoreMode.normal);
			if (arr_ptr == 0)
			{
				player.RaiseOutOfMemory(ref error);
				return arr_ptr;
			}

			RtPayloadArray arr = (RtPayloadArray)arr_instance.facility;
			
			Span<NaNBoxing> store_span;

			//转换基本块
			if (StoreMode == ArrayStoreMode.cache_on_stack)
			{
				store_span = stack_store.Span;
			}
			else
			{
#if DEBUG
				if (StoreMode != ArrayStoreMode.cache)
				{
					throw new InvalidOperationException();
				}
#endif

				store_span = cache_store.AsSpan();
			}

			arr.array_len = array_len;
			arr.InitHeapData(store_span,player,ref error);
			if (error.raised)
			{
				return 0;
			}

			//把property接过来。
			arr.m_property_ptr = m_property_ptr;

			if (StoreMode == ArrayStoreMode.cache_on_stack)
			{
				if (isArguments())
				{
					//创建callee属性。
					int callee_slotat = stack_store_startindex + stack_store.Length + 1;
					NaNBoxing callee_v = player.Context.StackSlots[callee_slotat];
					if (callee_v.ValueType != NaNBoxing.BoxType.Undefined)
					{
						callee_v = player.GetSaveValue(callee_v, ref error);
						if (error.raised)
						{
							return 0;
						}

						//创建动态对象callee
						//throw new NotImplementedException();

						NaNBoxing callee_str = default;callee_str.SetHeapPtr(player.CALLEE_STR);
						player.CreateDynamic(ref error, arr_instance, callee_str, callee_v, true, false, true);
						if (error.raised)
						{
							return 0;
						}
					}
				}
			}

			arr.array_len = newlen;

			//链接GC
			HEAPINSTANCE_PTR = arr_ptr;

			return arr_ptr;
		}





		internal void GCMarkAllElements(Context context)
		{
#if DEBUG
			if (HEAPINSTANCE_PTR != 0)
			{
				throw new InvalidOperationException();
			}
#endif
			switch (StoreMode)
            {
                case ArrayStoreMode.cache_on_stack:

                    {
                        var elements = stack_store.Span;
                        for (int i = 0; i < array_len; i++)
                        {
                            if (elements[i].ValueType == NaNBoxing.BoxType.HeapPtr)
                            {
                                context.GC.mark(context.GC.Heap[elements[i].HeapPtr]);
                            }
                        }
                    }
                    break;
                case ArrayStoreMode.cache:
                    {
						var elements = cache_store;
						for (int i = 0; i < array_len; i++)
						{
							if (elements[i].ValueType == NaNBoxing.BoxType.HeapPtr)
							{
								context.GC.mark(context.GC.Heap[elements[i].HeapPtr]);
							}
						}
					}
                    break;
                case ArrayStoreMode.normal:

					{
						foreach (var item in sparse_map)
						{
							for (int i = 0; i < SPARSE_BLOCK_SIZE; i++)
							{
								uint index = item.Key * SPARSE_BLOCK_SIZE + (uint)i;
								if (index < array_len)
								{
									if (item.Value[i].ValueType == NaNBoxing.BoxType.HeapPtr)
									{
										context.GC.mark(context.GC.Heap[item.Value[i].HeapPtr]);
									}
								}
							}	

						}
					}

					break;
                default:

#if DEBUG
					throw new InvalidOperationException();
#else
					Environment.FailFast("出错了，这里跑不到"); return;
#endif
			}



		}



		private short storeMode;

		public ArrayStoreMode StoreMode 
        {
            get 
            { 
                return (ArrayStoreMode)(storeMode & 0xFF);
            }
            set
            { 
                storeMode = (short)value;
            }       
        }

        public bool Delete(uint index,Player player)
        {
			if (HEAPINSTANCE_PTR == 0)
			{
				return DoDelete(index);
			}
			else
			{
				RtPayloadArray target;
				HEAPINSTANCE_PTR = FindAndUpdateHeapInstancePtr(HEAPINSTANCE_PTR, player, out target);
				return target.DoDelete(index);
			}
		}

		private bool DoDelete(uint index)
		{
			if (StoreMode == ArrayStoreMode.cache_on_stack)
			{
				var stack_span = stack_store.Span;
				if (index < stack_span.Length)
				{
					stack_span[(int)index].setFault();
				}

				return true;
			}
			else if (StoreMode == ArrayStoreMode.cache)
			{
				if (index < array_len)
				{ 
					cache_store[(int)index].setFault();
				}

				return true;
			}
			else
			{
				if (index < array_len)
				{
					uint block_index = index / SPARSE_BLOCK_SIZE;
					NaNBoxing[] block;
					if (sparse_map.TryGetValue(block_index, out block))
					{
						block[index % SPARSE_BLOCK_SIZE].setFault();
					}

				}
				return true;
				//throw new NotImplementedException();
			}
		}



		internal void SetSlot(NaNBoxing box, uint array_index, Player player,ref ReceiveError error)
        {
			if (HEAPINSTANCE_PTR == 0)
			{
				 DoSetSlot(box, array_index,ref error,player);
			}
			else
			{
				RtPayloadArray target;
				HEAPINSTANCE_PTR = FindAndUpdateHeapInstancePtr(HEAPINSTANCE_PTR, player, out target);
				target.DoSetSlot(box, array_index,ref error,player	);
			}
		}

		private void DoSetSlot(NaNBoxing box, uint array_index,ref ReceiveError error,Player player)
		{
			if (StoreMode == ArrayStoreMode.cache_on_stack)
			{
#if DEBUG
				if (array_index >= stack_store.Length)
				{
					throw new InvalidOperationException();
				}
#endif
				var stack_span = stack_store.Span;
				stack_span[(int)array_index] = box;

				if (array_index + 1 > array_len)
				{
					array_len = array_index + 1;
				}
			}
			else if (StoreMode == ArrayStoreMode.cache)
			{
#if DEBUG
				if (array_index >= cache_store.Length)
				{
					throw new InvalidOperationException();
				}
#endif       

				cache_store[array_index] = box;
				

				if (array_index + 1 > array_len)
				{
					array_len = array_index + 1;
				}

			}
			else
			{
				var oldsize = Size;
				NaNBoxing[] block=GetOrCreateBlock(array_index);

				if (player.Context.GC.MemUsage - oldsize + Size >= player.Context.GC.USAGE_LIMIT)
				{
					player.RaiseOutOfMemory(ref error);
					return;
				}


				block[array_index % SPARSE_BLOCK_SIZE] = box;


				if (array_index + 1 > array_len)
				{
					array_len = array_index + 1;
				}
				
			}

		}


        internal bool TrySetSlotIfReplaceStructOrNotHeap(NaNBoxing box, uint array_index,Player player,ref ReceiveError error)
        {
            if (HEAPINSTANCE_PTR == 0)
            {
                return DoTrySetSlotIfReplaceStructOrNotHeap(box, array_index, player,ref error);
            }
            else
            {
				RtPayloadArray target;
				HEAPINSTANCE_PTR = FindAndUpdateHeapInstancePtr(HEAPINSTANCE_PTR, player, out target);
				return target.DoTrySetSlotIfReplaceStructOrNotHeap(box,array_index,player,ref error);
			}

        }

        private void CopyStruct(RtHeapInstance dst,RtHeapInstance src,Player player)
        {
			dst.Type = src.Type;
			((RtPayloadInstance)dst.facility).HEAPINSTANCE_PTR = 0;
			((RtPayloadInstance)dst.facility).methodscopeslot_ref_state = 0;
			((RtPayloadInstance)dst.facility).CopyFrom(src, player, src.Type._link_codescope.TypeLayout.Size);

		}

		private bool DoTrySetSlotIfReplaceStructOrNotHeap(NaNBoxing box, uint array_index, Player player, ref ReceiveError error)
        {
            if (StoreMode == ArrayStoreMode.cache_on_stack)
            {
				if (array_index >= stack_store.Length)
				{
					ChangeStoreToHeap(array_index, player,ref error);
					if (error.raised)
					{
						return false;
					}

					return TrySetSlotIfReplaceStructOrNotHeap(box,array_index,player,ref error);

					//throw new NotImplementedException();
				}
				else if (box.ValueType == NaNBoxing.BoxType.HeapPtr)
				{
					var src = player.Context.GC.Heap[box.HeapPtr];
					if (src.TypeKind == RtHeapTypeKind.INSTANCE && ((ASInstance)src.Type).Flags.HasFlag(ClassFlags.Struct))
					{
						var stack_span = stack_store.Span;

						var dst_v = stack_span[(int)array_index] ;
						if (dst_v.ValueType == NaNBoxing.BoxType.HeapPtr)
						{
							var dst = player.Context.GC.Heap[dst_v.HeapPtr];
							if (dst.TypeKind == RtHeapTypeKind.INSTANCE && ((ASInstance)dst.Type).Flags.HasFlag(ClassFlags.Struct))
							{
								CopyStruct(dst, src, player);
								if (array_index + 1 > array_len)
								{
									array_len = array_index + 1;
								}
								return true;
							}
							else
							{
								return false;
							}
						}
						else
						{
							return false;
						}
					}
					else
					{
						return false;
					}
				}
				else
				{
					var stack_span = stack_store.Span;
					stack_span[(int)array_index] = box;

					if (array_index + 1 > array_len)
					{
						array_len = array_index + 1;
					}

					return true;
				}
            }
            else if (StoreMode == ArrayStoreMode.cache)
            {
                if (array_index >= cache_store.Length)
                {
					ChangeStoreToHeap(array_index, player, ref error);
					if (error.raised)
					{
						return false;
					}

					return TrySetSlotIfReplaceStructOrNotHeap(box, array_index, player, ref error);
					//throw new NotImplementedException();
				}
                else if (box.ValueType == NaNBoxing.BoxType.HeapPtr)
                {
                    var src = player.Context.GC.Heap[box.HeapPtr];
                    if (src.TypeKind == RtHeapTypeKind.INSTANCE && ((ASInstance)src.Type).Flags.HasFlag(ClassFlags.Struct))
                    {
                        var dst = player.Context.GC.Heap[cache_structs[array_index]];
                        CopyStruct(dst, src, player);
                        cache_store[array_index].SetHeapPtr(cache_structs[array_index]);

						if (array_index + 1 > array_len)
						{
							array_len = array_index + 1;
						}

						return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    cache_store[array_index] = box;

                    if (array_index + 1 > array_len)
                    {
                        array_len = array_index + 1;
                    }

                    return true;
                }
            }
            else
            {

				var oldsize = Size;				
				NaNBoxing[] block=GetOrCreateBlock(array_index);
				if (player.Context.GC.MemUsage - oldsize + Size >= player.Context.GC.USAGE_LIMIT)
				{
					player.RaiseOutOfMemory(ref error);
					return false;
				}




				if (box.ValueType == NaNBoxing.BoxType.HeapPtr)
				{
					var src = player.Context.GC.Heap[box.HeapPtr];
					if (src.TypeKind == RtHeapTypeKind.INSTANCE && ((ASInstance)src.Type).Flags.HasFlag(ClassFlags.Struct)
						&&
						array_index<array_len
						)
					{
						var dst_v = block[array_index % SPARSE_BLOCK_SIZE];
						if (dst_v.ValueType == NaNBoxing.BoxType.HeapPtr)
						{
							var dst = player.Context.GC.Heap[dst_v.HeapPtr];
							if (dst.TypeKind == RtHeapTypeKind.INSTANCE && ((ASInstance)dst.Type).Flags.HasFlag(ClassFlags.Struct))
							{
								CopyStruct(dst, src,player);
								return true;
							}
							else
							{
								return false;
							}
						}
						else
						{ 
							return false;
						}
					}
					else
					{
						return false;
					}

				}
				else
				{
					block[ array_index % SPARSE_BLOCK_SIZE ] = box;
					if (array_index + 1 > array_len)
					{
						array_len = array_index + 1;
					}

					return true;
				}

				

            }
        }




		public NaNBoxing ReadSlot(uint array_index, Player player,out bool isoutofindex_or_ishole)
        {
			if (HEAPINSTANCE_PTR == 0)
			{
				return DoReadSlot( array_index, player,out isoutofindex_or_ishole);
			}
			else
			{
				RtPayloadArray target;
				HEAPINSTANCE_PTR = FindAndUpdateHeapInstancePtr(HEAPINSTANCE_PTR, player, out target);
				return target.DoReadSlot(array_index, player,out isoutofindex_or_ishole);
			}

		}


		private NaNBoxing DoReadSlot(uint array_index, Player player,out bool isoutofindex_or_ishole)
        {
			if (StoreMode == ArrayStoreMode.cache_on_stack)
			{
				if (array_index < array_len)
				{
					isoutofindex_or_ishole = false;
					var v = stack_store.Span[(int)array_index];
					if (v.ValueType == NaNBoxing.BoxType.Fault)
					{
						isoutofindex_or_ishole = true;
						v.SetUndefined();
						return v;
					}
					else
					{
						return v;
					}

				}
				else
				{
					isoutofindex_or_ishole = true;
					NaNBoxing v = default;
					v.SetUndefined();
					return v;
				}
			}
			else if (StoreMode == ArrayStoreMode.cache)
			{
				if (array_index < array_len)
				{
					
					var v = cache_store[array_index];
					if (v.ValueType == NaNBoxing.BoxType.Fault)
					{
						isoutofindex_or_ishole = true;
						v.SetUndefined();
						return v;
					}
					else
					{
						isoutofindex_or_ishole = false;
						return v;
					}

				}
				else
				{
					isoutofindex_or_ishole = true;
					NaNBoxing v = default;
					v.SetUndefined();
					return v;
				}

				//throw new NotImplementedException();
			}
			else
			{
				if (array_index < array_len)
				{
					isoutofindex_or_ishole = false;
					uint block_index = array_index / SPARSE_BLOCK_SIZE;
					NaNBoxing[] block;
					if (sparse_map.TryGetValue(block_index, out block))
					{
						var v = block[array_index % SPARSE_BLOCK_SIZE];
						if (v.ValueType == NaNBoxing.BoxType.Fault)
						{
							isoutofindex_or_ishole = true;
							v.SetUndefined();
							return v;
						}
						else
						{
							return v;
						}
					}
					else
					{
						isoutofindex_or_ishole = true;
						NaNBoxing v = default;
						v.SetUndefined();
						return v;
					}
				}
				else
				{
					isoutofindex_or_ishole = true;
					NaNBoxing v = default;
					v.SetUndefined();
					return v;
				}
			}
		}




		internal void SetIsRest(bool v)
		{
            if (v)
            {
                storeMode = (short)((storeMode & 0xff) | 0x100) ;
            }
            else
            {
				storeMode &= 0xff;
			}
		}

		internal void SetIsArguments(bool v)
		{
			if (v)
			{
				storeMode = (short)((storeMode & 0xff) | 0x200);
			}
			else
			{
				storeMode &= 0xff;
			}
		}

        internal bool isArguments()
        {
            return (storeMode >> 8) == 0x2;
        }

        internal bool isRest()
        {
			return (storeMode >> 8) == 0x1;
		}

		internal void CopyCacheFrom(RtPayloadArray arr_store, Player player)
		{
#if DEBUG
            if (arr_store.StoreMode != ArrayStoreMode.cache || StoreMode != ArrayStoreMode.cache)
            {
                throw new InvalidOperationException();
            }
            if (arr_store.HEAPINSTANCE_PTR != 0)
            {
                throw new InvalidOperationException();
            }
#endif

			m_property_ptr = arr_store.m_property_ptr;
			
			array_len = arr_store.array_len;
            for (int i = 0; i < MAX_CACHE_ELEMENT; i++)
            {
                if (arr_store.cache_store[i].ValueType == NaNBoxing.BoxType.HeapPtr)
                {
                    if (arr_store.cache_store[i].HeapPtr == arr_store.cache_structs[i])
                    {
                        var dst = player.Context.GC.Heap[cache_structs[i]];
                        var src = player.Context.GC.Heap[arr_store.cache_structs[i]];

                        CopyStruct(dst, src, player);

						cache_store[i].SetHeapPtr(cache_structs[i]);
                    }
                    else
                    {
                        //除了struct,凡是存入array的对象都禁用cache对象，完事。。。
						cache_store[i] = arr_store.cache_store[i];
					}
                }
                else
                {
                    cache_store[i] = arr_store.cache_store[i];
                }

            }


        }

		internal void Trace(Context context,int stackStPos, ref ReceiveError error,int scope_ptr ,IPrint printer,RtHeapInstance arrObj,ReadOnlySpan<char> sep)
		{
			if (HEAPINSTANCE_PTR == 0)
			{
				 DoTrace(context, stackStPos, ref error, scope_ptr ,printer,arrObj,sep);
			}
			else
			{
				RtPayloadArray target;
				HEAPINSTANCE_PTR = FindAndUpdateHeapInstancePtr(HEAPINSTANCE_PTR, context.player, out target);
				target.DoTrace(context, stackStPos, ref error,scope_ptr ,printer,arrObj,sep);
			}
		}

		private void DoTrace(Context context, int stackStPos, ref ReceiveError error, int scope_ptr ,IPrint printer, RtHeapInstance arrObj, ReadOnlySpan<char> sep)
		{
			if (StoreMode == ArrayStoreMode.cache_on_stack)
			{
				var stack_span = stack_store.Span;
				for (int i = 0; i < array_len; i++)
				{
					if (stack_span[i].ValueType != NaNBoxing.BoxType.Null && stack_span[i].ValueType != NaNBoxing.BoxType.Undefined && stack_span[i].ValueType != NaNBoxing.BoxType.Fault)
					{
						TopLevel.TraceElement(stack_span[i], context, stackStPos, ref error, scope_ptr, default, printer);
						if (error.raised)
						{
							return;
						}
					}
					else
					{
						bool isoutofindex_or_ishole;
						NaNBoxing l= context.player.LoadSlotFromArray((uint)i, arrObj,out isoutofindex_or_ishole);
						if (l.ValueType != NaNBoxing.BoxType.Fault && l.ValueType != NaNBoxing.BoxType.Null && l.ValueType != NaNBoxing.BoxType.Undefined)
						{
							TopLevel.TraceElement(l, context, stackStPos, ref error, scope_ptr, default, printer);
							if (error.raised)
							{
								return;
							}
						}
					}

					if (i < array_len - 1)
					{
						printer.Write(sep);
					}
				}

			}
			else if (StoreMode == ArrayStoreMode.cache)
			{
				for (int i = 0; i < array_len; i++)
				{
					
					if (cache_store[i].ValueType != NaNBoxing.BoxType.Null && cache_store[i].ValueType != NaNBoxing.BoxType.Undefined && cache_store[i].ValueType != NaNBoxing.BoxType.Fault )
					{
						TopLevel.TraceElement(cache_store[i], context, stackStPos, ref error,scope_ptr,default, printer);
						if (error.raised)
						{
							return;
						}
					}
					else
					{
						bool isoutofindex_or_ishole;
						NaNBoxing l = context.player.LoadSlotFromArray((uint)i, arrObj,out isoutofindex_or_ishole);
						if (l.ValueType != NaNBoxing.BoxType.Fault && l.ValueType != NaNBoxing.BoxType.Null && l.ValueType != NaNBoxing.BoxType.Undefined)
						{
							TopLevel.TraceElement(l, context, stackStPos, ref error, scope_ptr, default, printer);
							if (error.raised)
							{
								return;
							}
						}
					}

					if (i < array_len - 1)
					{
						printer.Write(sep);
					}
				}
			}
			else
			{
				if (array_len > 0)
				{
					uint last_block_id = (array_len - 1) / SPARSE_BLOCK_SIZE;

					for (uint i = 0; i < last_block_id + 1; i++)
					{
						NaNBoxing[] block;
						bool hasblock = sparse_map.TryGetValue(i, out block);

						for (uint j = 0; j < SPARSE_BLOCK_SIZE; j++)
						{
							uint current = i * SPARSE_BLOCK_SIZE + j;

							if (current >= array_len)
							{
								return;
							}

							if (hasblock)
							{
								if (block[j].ValueType != NaNBoxing.BoxType.Null && block[j].ValueType != NaNBoxing.BoxType.Undefined && block[j].ValueType != NaNBoxing.BoxType.Fault)
								{
									TopLevel.TraceElement(block[j], context, stackStPos, ref error, scope_ptr, default, printer);
									if (error.raised)
									{
										return;
									}
								}
								else
								{
									bool isoutofindex_or_ishole;
									NaNBoxing l = context.player.LoadSlotFromArray(current, arrObj, out isoutofindex_or_ishole);
									if (l.ValueType != NaNBoxing.BoxType.Fault && l.ValueType != NaNBoxing.BoxType.Null && l.ValueType != NaNBoxing.BoxType.Undefined)
									{
										TopLevel.TraceElement(l, context, stackStPos, ref error, scope_ptr, default, printer);
										if (error.raised)
										{
											return;
										}
									}
								}
							}
							else
							{
								bool isoutofindex_or_ishole;
								NaNBoxing l = context.player.LoadSlotFromArray(current, arrObj, out isoutofindex_or_ishole);
								if (l.ValueType != NaNBoxing.BoxType.Fault && l.ValueType != NaNBoxing.BoxType.Null && l.ValueType != NaNBoxing.BoxType.Undefined)
								{
									TopLevel.TraceElement(l, context, stackStPos, ref error, scope_ptr, default, printer);
									if (error.raised)
									{
										return;
									}
								}
							}

							if (current < array_len - 1)
							{
								printer.Write(sep);
							}
						}

					}
				}
				//throw new NotImplementedException();
			}
		}

		internal bool TryReadIterItem(int index, out uint key ,out uint next_index, out NaNBoxing v , Context context)
		{
			if (HEAPINSTANCE_PTR == 0)
			{
				return DoTryReadIterItem( index,out key ,out next_index, out v);
			}
			else
			{
				RtPayloadArray target;
				HEAPINSTANCE_PTR = FindAndUpdateHeapInstancePtr(HEAPINSTANCE_PTR, context.player, out target);
				return target.DoTryReadIterItem( index,out key, out next_index, out v );
			}
		}

		private bool DoTryReadIterItem(int index, out uint key,out uint next_index,out NaNBoxing v)
		{
			if (StoreMode == ArrayStoreMode.cache_on_stack)
			{
				var stack_span = stack_store.Span;
				
				next_index = 0;key = 0;
				v = default;

				for (int i = index; i < array_len; i++)
				{
					if (stack_span[i].ValueType != NaNBoxing.BoxType.Null && 
						//stack_span[i].ValueType != NaNBoxing.BoxType.Undefined && 
						stack_span[i].ValueType != NaNBoxing.BoxType.Fault)
					{
						key = (uint)i;
						next_index = (uint)i+1;
						v = stack_span[i];

						return true;
						
					}

				}
				return false;
			}
			else if (StoreMode == ArrayStoreMode.cache)
			{
				key = 0;
				next_index = 0;
				v = default;
				for (int i = index; i < array_len; i++)
				{

					if (cache_store[i].ValueType != NaNBoxing.BoxType.Null && 
						//cache_store[i].ValueType != NaNBoxing.BoxType.Undefined && 
						cache_store[i].ValueType != NaNBoxing.BoxType.Fault)
					{
						key = (uint)i;
						next_index = (uint)i+1;
						v = cache_store[i];

						return true;
						
					}

				}
				return false;
			}
			else
			{
				key = 0;
				next_index = 0;
				v = default;

				if (array_len > (uint)index)
				{
					uint last_block_id = (array_len - 1) / SPARSE_BLOCK_SIZE;

					uint j_start = ((uint)index) % SPARSE_BLOCK_SIZE;

					var maps = sparse_map.OrderBy(i => i.Key).Where(i=>i.Key>= (uint)index / SPARSE_BLOCK_SIZE );
					
					foreach(var kv in maps)
					{
						uint i = kv.Key;

						NaNBoxing[] block = kv.Value;
						
						for (uint j = j_start ; j < SPARSE_BLOCK_SIZE; j++)
						{
							uint current = i * SPARSE_BLOCK_SIZE + j;

							if (current >= array_len)
							{
								return false;
							}

							
							{
								if (block[j].ValueType != NaNBoxing.BoxType.Null && 
									//block[j].ValueType != NaNBoxing.BoxType.Undefined && 
									block[j].ValueType != NaNBoxing.BoxType.Fault)
								{
									key = current;
									next_index = current + 1;

									v = block[j];
									return true;
								}
							}
						}
						j_start = 0;
					}

				}




				return false;
			}
		}

	}
}
