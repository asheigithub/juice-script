using juicescript.ABC;
using juicescript.runtime.buildin;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static juicescript.NaNBoxing;
using static juicescript.runtime.Player;

namespace juicescript.runtime
{
#if FORCOMPILER
	internal
#else
    public
#endif
		sealed class RtArray : RtHeapBase
	{
		public RtArray() : base(RtHeapTypeKind.ARRAY)
		{
		}

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
			cache = 1,

			/// <summary>
			/// 分配在堆里
			/// </summary>
			normal = 2

		}


		public override int Size
		{
			get
			{
				return 4 + 4 + 4 + 8 + 8 + 8 + 4 + (
					StoreMode == ArrayStoreMode.cache_on_stack ? 0 :
					(StoreMode == ArrayStoreMode.cache ? (MAX_CACHE_ELEMENT * 8 + 16) : (sparse_map.Count * SPARSE_BLOCK_SIZE * 8 + (16 + 16) * sparse_map.Count))
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
				RtArray target;
				FindAndUpdateHeapInstancePtr(HEAPINSTANCE_PTR, player, out target);
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
				RtArray target;
				FindAndUpdateHeapInstancePtr(HEAPINSTANCE_PTR, player, out target);
				target.m_property_ptr = ptr;
			}
		}


		/// <summary>
		/// 如果是缓存对象(包括argements)，并且已经被保存到堆中，则保存堆中对象的指针
		/// 后续操作将直接对堆里的对象操作了。
		/// </summary>
		internal int HEAPINSTANCE_PTR { get; private set; }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void SetStoreRest(Memory<NaNBoxing> store,int store_startindex)
		{
			array_len = (uint)store.Length;
			store_memory = store;

			cache_struct_ptr = 0;

			stack_store_startindex = store_startindex;
			HEAPINSTANCE_PTR = 0;
			m_property_ptr = 0;
			nextframe_ref_state = default;


		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void SetStoreCacheZero(bool clear, Memory<NaNBoxing> store, int cache_struct_p )
		{
			StoreMode = ArrayStoreMode.cache;
			HEAPINSTANCE_PTR = 0;
			methodscopeslot_ref_state = 0;
			nextframe_ref_state = default;
			array_len = 0;

			m_property_ptr = 0;

			store_memory = store;
			cache_struct_ptr = cache_struct_p;

			if (clear)
			{
				var cache_store = store.Span;
				for (uint i = array_len; i < cache_store.Length; i++)
				{
					cache_store[(int)i].setFault();
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void LinkTo(RtArray dst, int dstptr)
		{ 
			HEAPINSTANCE_PTR = dstptr;
			
		}



		internal static int FindAndUpdateHeapInstancePtr(int ptr, Player player, out RtArray target)
		{
			var payload = ((RtArray)player.Context.GC.Heap[ptr]);
			var origin = payload;
			target = origin;


			while (payload.HEAPINSTANCE_PTR != 0)
			{
				ptr = payload.HEAPINSTANCE_PTR;
				payload = ((RtArray)player.Context.GC.Heap[ptr]);
				target = payload;

				origin.HEAPINSTANCE_PTR = ptr;//更新,避免后续跳转
				
			}

			return ptr;
		}



		internal int stack_store_startindex;
		internal Memory<NaNBoxing> store_memory;

		//private Span<NaNBoxing> cache_store { get => store_memory.Span; }

		//internal NaNBoxing[] cache_store;
		//internal int[] cache_structs;
		internal int cache_struct_ptr;


		private const int SPARSE_BLOCK_SIZE = 64;
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
			

			store_memory = default;
			cache_struct_ptr = 0;
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

		/// <summary>
		/// 被调用的下级函数引用情况,包含指针和版本
		/// </summary>
		internal refbynextframe nextframe_ref_state;
		


		internal uint array_len = 0;

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public uint GetLength(Player player,out RtArray target)
		{
			if (HEAPINSTANCE_PTR == 0)
			{
				target = this;
				return array_len;
			}
			else
			{
				//RtArray target;
				FindAndUpdateHeapInstancePtr(HEAPINSTANCE_PTR, player, out target);
				return target.array_len;
			}
		}


		public void SetLength(uint len, Player player, ref ReceiveError error)
		{
			if (HEAPINSTANCE_PTR == 0)
			{
				DoSetLength(len, player, ref error);
			}
			else
			{
				RtArray target;
				FindAndUpdateHeapInstancePtr(HEAPINSTANCE_PTR, player, out target);
				target.DoSetLength(len, player, ref error);
			}
		}

		private void DoSetLength(uint len, Player player, ref ReceiveError error)
		{
			switch (StoreMode)
			{
				case ArrayStoreMode.cache_on_stack:
					{
						if (len > store_memory.Length)
						{
							ChangeStoreToHeap(len, player, ref error,out RtArray arr);
							//throw new NotImplementedException();
						}
						else
						{
							array_len = len;
							var stack_span = store_memory.Span;
							for (uint i = array_len; i < store_memory.Length; i++)
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
							ChangeStoreToHeap(len, player, ref error, out RtArray arr);
							//throw new NotImplementedException();
						}
						else
						{
							array_len = len;
							var cache_store = store_memory.Span;
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
						if (array_len < len) //仅收缩时才需要释放。
						{
							int oldsize = Size;

							foreach (var id in sparse_map.Keys.ToArray())
							{
								if (id * SPARSE_BLOCK_SIZE >= array_len)
								{
									sparse_map.Remove(id);
								}
							}

							player.Context.GC.UpdateMemUsage_Change(oldsize - Size);
						}

						array_len = len;

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
			uint len = GetLength(player,out RtArray t);
			return ChangeStoreToHeap(len, player, ref error, out RtArray arr);

		}

		internal void InitHeapData(Span<NaNBoxing> init_data, Player player, ref ReceiveError error)
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

				if (v.IsStruct())//v.ValueType == NaNBoxing.BoxType.HeapPtr)
				{
					//if (v.HeapKind == (byte)RtHeapTypeKind.INSTANCE)
					{
						var ins = player.Context.GC.Heap[v.HeapPtr];
						Debug.Assert(((ASInstance)ins.Type).Flags.HasFlag(ClassFlags.Struct));
						{
							RtHeapBase struct_instance;
							//复制结构体
							int struct_ptr = player.Context.GC.AllocInstance((ASInstance)ins.Type, out struct_instance);
							if (struct_ptr == 0)
							{
								player.RaiseOutOfMemory(ref error);
								return;
							}

							CopyStruct(struct_instance, ins, player);
							v.SetHeapPtr(struct_ptr, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.FLAG_STRUCT);
						}
					}
				}

				block[i % SPARSE_BLOCK_SIZE] = v;

				i++;
			}


			if (usg + Size >= player.Context.GC.USAGE_LIMIT)
			{
				player.RaiseOutOfMemory(ref error);
				return;
			}

			player.Context.GC.UpdateMemUsage_Change(Size - oldsize);
		}


		private int ChangeStoreToHeap(uint newlen, Player player, ref ReceiveError error,out RtArray arr)
		{
			RtHeapBase arr_instance;
			int arr_ptr = player.Context.GC.AllocArray(out arr_instance, ArrayStoreMode.normal);
			if (arr_ptr == 0)
			{
				arr = null;
				player.RaiseOutOfMemory(ref error);
				return arr_ptr;
			}

			arr = (RtArray)arr_instance;

			
			Debug.Assert(StoreMode != ArrayStoreMode.normal);

			Span<NaNBoxing> store_span = store_memory.Span;

			//转换基本块
			//			if (StoreMode == ArrayStoreMode.cache_on_stack)
			//			{
			//				store_span = store_memory.Span;
			//			}
			//			else
			//			{
			//#if DEBUG
			//				if (StoreMode != ArrayStoreMode.cache)
			//				{
			//					throw new InvalidOperationException();
			//				}
			//#endif

			//				store_span = cache_store;
			//			}

			arr.array_len = array_len;
			arr.InitHeapData(store_span, player, ref error);
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
					int callee_slotat = stack_store_startindex + store_memory.Length + 1;
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

						NaNBoxing callee_str = default; callee_str.SetHeapPtr(player.CALLEE_STR, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
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
				case ArrayStoreMode.cache:
					{
						var elements = store_memory.Span;
						for (int i = 0; i < array_len; i++)
						{
							if (elements[i].ValueType == NaNBoxing.BoxType.HeapPtr)
							{
								context.GC.mark(context.GC.Heap[elements[i].HeapPtr]);
							}
						}
					}
					break;
				//case ArrayStoreMode.cache:
				//	{
				//		var elements = cache_store;
				//		for (int i = 0; i < array_len; i++)
				//		{
				//			if (elements[i].ValueType == NaNBoxing.BoxType.HeapPtr)
				//			{
				//				context.GC.mark(context.GC.Heap[elements[i].HeapPtr]);
				//			}
				//		}
				//	}
				//	break;
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

		public bool Delete(uint index, Player player)
		{
			if (HEAPINSTANCE_PTR == 0)
			{
				return DoDelete(index);
			}
			else
			{
				RtArray target;
				FindAndUpdateHeapInstancePtr(HEAPINSTANCE_PTR, player, out target);
				return target.DoDelete(index);
			}
		}

		private bool DoDelete(uint index)
		{
			if (StoreMode != ArrayStoreMode.normal)
			{
				var stack_span = store_memory.Span;
				if (index < stack_span.Length)
				{
					stack_span[(int)index].setFault();
				}

				return true;
			}
			//else if (StoreMode == ArrayStoreMode.cache)
			//{
			//	if (index < array_len)
			//	{
			//		cache_store[(int)index].setFault();
			//	}

			//	return true;
			//}
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



		internal void SetSlot(NaNBoxing box, uint array_index, Player player, ref ReceiveError error)
		{
			Debug.Assert(HEAPINSTANCE_PTR == 0);
			{
				DoSetSlot(box, array_index, ref error, player);
			}
			//else
			//{
			//	RtArray target;
			//	HEAPINSTANCE_PTR = FindAndUpdateHeapInstancePtr(HEAPINSTANCE_PTR, player, out target);
			//	target.DoSetSlot(box, array_index, ref error, player);
			//}
		}

		private void DoSetSlot(NaNBoxing box, uint array_index, ref ReceiveError error, Player player)
		{
			if (StoreMode != ArrayStoreMode.normal)
			{
#if DEBUG
				if (array_index >= store_memory.Length)
				{
					throw new InvalidOperationException();
				}
#endif
				var stack_span = store_memory.Span;
				stack_span[(int)array_index] = box;

				if (array_index + 1 > array_len)
				{
					array_len = array_index + 1;
				}
			}
//			else if (StoreMode == ArrayStoreMode.cache)
//			{
//#if DEBUG
//				if (array_index >= cache_store.Length)
//				{
//					throw new InvalidOperationException();
//				}
//#endif

//				cache_store[(int)array_index] = box;


//				if (array_index + 1 > array_len)
//				{
//					array_len = array_index + 1;
//				}

//			}
			else
			{
				var oldsize = Size;
				NaNBoxing[] block = GetOrCreateBlock(array_index);

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



		internal void DoReverse(Context context, ref ReceiveError error, int tempSlot)
		{
			Debug.Assert(context != null);
			if (StoreMode == ArrayStoreMode.cache_on_stack)
			{
				var stack_span = store_memory.Span;

				int st = 0;
				int ed = (int)array_len - 1;

				while (st < ed) //这里不存在 struct_cache问题，所以直接交换
				{
					var v1 = stack_span[st];
					var v2 = stack_span[ed];

					stack_span[st] = v2;
					stack_span[ed] = v1;

					st++;
					ed--;
				}

			}
			else if (StoreMode == ArrayStoreMode.cache)
			{
				int st = 0;
				int ed = (int)array_len - 1;
				var cache_store = store_memory.Span;
				while (st < ed) //可能存在struct_cache问题，所以要走全流程
				{
					var v1 = cache_store[st];
					var v2 = cache_store[ed];

					if (v1.IsStruct())//v1.ValueType == BoxType.HeapPtr && v1.HeapKind == (byte)RtHeapTypeKind.INSTANCE)
					{
						var check = context.GC.Heap[v1.HeapPtr];
						Debug.Assert(((ASInstance)check.Type).Flags.HasFlag(ClassFlags.Struct));
						{
							int clonedptr = tempSlot + Context.CacheInstancePtr;
							var cacheObj = (RtInstance)context.GC.Heap[clonedptr];
							//cacheObj.Type = check.Type;

							//((RtInstance)cacheObj).methodscopeslot_ref_state = 0;
							//((RtInstance)cacheObj).HEAPINSTANCE_PTR = 0;
							//((RtInstance)cacheObj).CopyFrom(check, context.player, check.Type._link_codescope.TypeLayout.Size);
							cacheObj.CloneOther((RtInstance)check, context.player);

							context.StackSlots[tempSlot].SetHeapPtr(clonedptr, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.FLAG_STRUCT);
						}
						//else
						//{
						//	context.StackSlots[tempSlot] = v1;
						//}
					}
					else
					{
						context.StackSlots[tempSlot] = v1;
					}

					DoSetSlot(v2, (uint)st, ref error, context.player);
					if (error.raised) return;

					DoSetSlot(context.StackSlots[tempSlot], (uint)ed, ref error, context.player);
					if (error.raised) return;

					st++;
					ed--;
				}
			}
			else
			{
				long st = 0;
				long ed = (long)array_len - 1;

				uint _lastv1_index = uint.MaxValue; NaNBoxing[] _lastv1_block = null;
				uint _lastv2_index = uint.MaxValue; NaNBoxing[] _lastv2_block = null;


				while (st < ed)
				{
					NaNBoxing v1 = default;
					{
						uint block_index = (uint)((st) / SPARSE_BLOCK_SIZE);
						NaNBoxing[] block;

						if (_lastv1_index == block_index)
						{
							v1 = _lastv1_block[st % SPARSE_BLOCK_SIZE];
						}
						else if (sparse_map.TryGetValue(block_index, out block))
						{
							v1 = block[(st) % SPARSE_BLOCK_SIZE];

							_lastv1_block = block;
							_lastv1_index = block_index;
						}
						else
						{
							v1.setFault();
						}
					}

					NaNBoxing v2 = default;
					{
						uint block_index = (uint)((ed) / SPARSE_BLOCK_SIZE);
						NaNBoxing[] block;



						if (_lastv2_index == block_index)
						{
							v2 = _lastv2_block[st % SPARSE_BLOCK_SIZE];

						}
						else if (sparse_map.TryGetValue(block_index, out block))
						{
							v2 = block[(ed) % SPARSE_BLOCK_SIZE];

							_lastv2_block = block;
							_lastv2_index = block_index;
						}
						else
						{
							v2.setFault();
						}
					}

					if (v1.Raw == v2.Raw)
					{

					}
					else //不存在struct_cache问题，直接交换
					{
						if (_lastv1_index == (uint)((st) / SPARSE_BLOCK_SIZE))
						{
							_lastv1_block[st % SPARSE_BLOCK_SIZE] = v2;
						}
						else
						{
							var oldsize = Size;
							NaNBoxing[] block = GetOrCreateBlock((uint)st);
							if (context.GC.MemUsage - oldsize + Size >= context.GC.USAGE_LIMIT)
							{
								context.player.RaiseOutOfMemory(ref error);
								return;
							}

							block[st % SPARSE_BLOCK_SIZE] = v2;

						}

						if (_lastv2_index == (uint)((ed) / SPARSE_BLOCK_SIZE))
						{
							_lastv2_block[ed % SPARSE_BLOCK_SIZE] = v1;
						}
						else
						{
							var oldsize = Size;
							NaNBoxing[] block = GetOrCreateBlock((uint)ed);
							if (context.GC.MemUsage - oldsize + Size >= context.GC.USAGE_LIMIT)
							{
								context.player.RaiseOutOfMemory(ref error);
								return;
							}

							block[ed % SPARSE_BLOCK_SIZE] = v1;

						}

					}

					st++;
					ed--;
				}
			}


		}


		internal void DoSplice(
			Context context,
			ref ReceiveError error,
			int start,
			uint deleteCount,
			long netChange)
		{
			Debug.Assert(HEAPINSTANCE_PTR == 0);

			long len = array_len;


			long newLen = len + netChange;

			Debug.Assert(newLen <= uint.MaxValue);

			switch (StoreMode)
			{
				case ArrayStoreMode.cache_on_stack:
					DoSplice_OnStack(context, ref error, start, deleteCount, (int)len, newLen, netChange);
					break;
				case ArrayStoreMode.cache:
					DoSplice_Cache(context, ref error, start, deleteCount, (int)len, newLen, netChange);
					break;
				case ArrayStoreMode.normal:
					DoSplice_Normal(context, ref error, start, deleteCount, (uint)len, (uint)newLen, netChange);
					break;
			}
		}

		private void DoSplice_OnStack(
			Context context, ref ReceiveError error,
			int start, uint deleteCount,
			int len, long newLen, long netChange)
		{
			var span = store_memory.Span;
			// 检查是否溢出缓存
			if (newLen > span.Length)
			{
				// 提升到堆存储（会把当前数据转移到 normal 存储）
				int heaparrayptr = ChangeStoreToHeap(context.player, ref error);
				if (error.raised) return;

				RtArray heap = (RtArray)context.player.Context.GC.Heap[heaparrayptr];
				heap.DoSplice(context, ref error, start, deleteCount, netChange);
				return;
			}
			if (netChange > 0)
			{
				// 情况A：插入比删除多 → 向右移（类似 DoUnshift）
				// 1. 从末尾开始，把 [start + deleteCount, len) 的元素向右移动 netChange 个位置
				for (long i = len - 1; i >= start + deleteCount; i--)
				{
					var box = span[(int)i];
					//CopyBoxTo_OnStack(span, (int)(i + netChange), box, context.player);
					span[(int)(i + netChange)] = box; //不存在结构体cache的情况


					span[(int)i].setFault();//头部要清除
				}


			}
			else if (netChange < 0)
			{
				// 情况B：删除比插入多 → 向左移（类似 DoShift）
				// 1. 把 [start + deleteCount, len) 的元素向左移动 |netChange| 个位置
				for (long i = start + deleteCount; i < len; i++)
				{
					var box = span[(int)i];
					//CopyBoxTo_OnStack(span, (int)(i + netChange), box, context.player);
					span[(int)(i + netChange)] = box;//不存在结构体cache的情况
				}
				// 2. 清除末尾 |netChange| 个位置
				for (int i = (int)(len + netChange); i < len; i++)
					span[i].setFault();
			}
			else
			{

			}
			array_len = (uint)newLen;
		}

		private void DoSplice_Cache(
			Context context, ref ReceiveError error,
			int start, uint deleteCount,
			int len, long newLen, long netChange)
		{
			var span = store_memory.Span; //cache_store;
			// 检查是否溢出缓存
			if (newLen > span.Length)
			{
				// 提升到堆存储（会把当前数据转移到 normal 存储）
				int heaparrayptr = ChangeStoreToHeap(context.player, ref error);
				if (error.raised) return;

				RtArray heap = (RtArray)context.player.Context.GC.Heap[heaparrayptr];
				heap.DoSplice(context, ref error, start, deleteCount, netChange);
				return;
			}
			if (netChange > 0)
			{
				// 情况A：插入比删除多 → 向右移（类似 DoUnshift）
				// 1. 从末尾开始，把 [start + deleteCount, len) 的元素向右移动 netChange 个位置
				for (long i = len - 1; i >= start + deleteCount; i--)
				{
					var box = span[(int)i];
					CopyBoxTo_OnStack(span, (int)(i + netChange), box, context.player);
					span[(int)i].setFault();//头部要清除
				}


			}
			else if (netChange < 0)
			{
				// 情况B：删除比插入多 → 向左移（类似 DoShift）
				// 1. 把 [start + deleteCount, len) 的元素向左移动 |netChange| 个位置
				for (long i = start + deleteCount; i < len; i++)
				{
					var box = span[(int)i];
					CopyBoxTo_OnStack(span, (int)(i + netChange), box, context.player);
				}
				// 2. 清除末尾 |netChange| 个位置
				for (int i = (int)(len + netChange); i < len; i++)
					span[i].setFault();

			}
			else
			{

			}
			array_len = (uint)newLen;
		}


		// 辅助方法：复制 box（处理结构体）
		private void CopyBoxTo_OnStack(Span<NaNBoxing> span, int dstIndex, NaNBoxing box, Player player)
		{
			Debug.Assert(StoreMode == ArrayStoreMode.cache);


			if (box.IsStruct())//box.ValueType == BoxType.HeapPtr && box.HeapKind == (byte)RtHeapTypeKind.INSTANCE)
			{
				var src = player.Context.GC.Heap[box.HeapPtr];
				Debug.Assert(((ASInstance)src.Type).Flags.HasFlag(ClassFlags.Struct));

				
				//if (dst_v.IsStruct())//dst_v.ValueType == BoxType.HeapPtr && dst_v.HeapKind == (byte)RtHeapTypeKind.INSTANCE)
				{
					var dst = player.Context.GC.Heap[cache_struct_ptr + dstIndex];//cache_structs[dstIndex]];
					//Debug.Assert(((ASInstance)dst.Type).Flags.HasFlag(ClassFlags.Struct));
					{
						CopyStruct(dst, src, player);
						span[dstIndex].SetHeapPtr(cache_struct_ptr + dstIndex, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.FLAG_STRUCT);
						return;
					}
				}

			}
			else
			{


				span[dstIndex] = box;
			}
		}

		private void DoSplice_Normal(
			Context context, ref ReceiveError error,
			int start, uint deleteCount,
			uint len, uint newLen, long netChange)
		{
			if (netChange > 0)
			{
				NaNBoxing[] _last_srcblock = null; uint _last_src_blockid = uint.MaxValue;
				NaNBoxing[] _last_dstblock = null; uint _last_dst_blockid = uint.MaxValue;

				// 情况A：插入比删除多 → 向右移（类似 DoUnshift）
				// 把 [start + deleteCount, len) 的元素向右移动 netChange 个位置
				for (long i = (long)len - 1; i >= start + (long)deleteCount; i--)
				{

					NaNBoxing src = default;
					{
						uint block_index = (uint)i / SPARSE_BLOCK_SIZE;
						NaNBoxing[] block;
						if (block_index == _last_src_blockid && _last_srcblock != null)
						{
							block = _last_srcblock;
							_last_src_blockid = block_index;

							src = block[i % SPARSE_BLOCK_SIZE];
						}
						else if (block_index == _last_src_blockid)
						{
							src.setFault();
						}
						else if (sparse_map.TryGetValue(block_index, out block))
						{
							src = block[i % SPARSE_BLOCK_SIZE];

							_last_src_blockid = block_index;
							_last_srcblock = block;

						}
						else
						{
							_last_src_blockid = block_index;
							_last_srcblock = null;
							src.setFault();
						}
					}

					if (src.ValueType == BoxType.Fault)
					{

						uint block_index = (uint)(i + netChange) / SPARSE_BLOCK_SIZE;
						NaNBoxing[] block;


						if (block_index == _last_dst_blockid && _last_dstblock != null)
						{

						}
						else if (block_index == _last_dst_blockid)
						{
							continue;
						}
						else if (sparse_map.TryGetValue(block_index, out block))
						{
							_last_dst_blockid = block_index;
							_last_dstblock = block;
						}
						else
						{
							continue; //跳过都是空洞.
						}

					}


					// 计算目标位置
					long dstIndex = i + netChange;

					Debug.Assert(dstIndex < newLen);

					// 写入目标位置（只为实际存在的索引分配块）
					{
						if (_last_dst_blockid == (uint)(i + netChange) / SPARSE_BLOCK_SIZE && _last_dstblock != null)
						{
							_last_dstblock[dstIndex % SPARSE_BLOCK_SIZE] = src;
						}
						else
						{
							var oldsize = Size;
							NaNBoxing[] dstBlock = GetOrCreateBlock((uint)dstIndex);
							if (context.player.Context.GC.MemUsage - oldsize + Size >= context.player.Context.GC.USAGE_LIMIT)
							{
								context.player.RaiseOutOfMemory(ref error);
								return;
							}
							dstBlock[dstIndex % SPARSE_BLOCK_SIZE] = src;

							_last_dst_blockid = (uint)(i + netChange) / SPARSE_BLOCK_SIZE;
							_last_dstblock = dstBlock;

						}

						if (src.ValueType != BoxType.Fault)
						{
							_last_srcblock[i % SPARSE_BLOCK_SIZE].setFault(); //清除头部
						}
					}
				}

			}
			else if (netChange < 0)
			{
				NaNBoxing[] _last_srcblock = null; uint _last_src_blockid = uint.MaxValue;
				NaNBoxing[] _last_dstblock = null; uint _last_dst_blockid = uint.MaxValue;


				// 情况B：删除比插入多 → 向左移（类似 DoShift）
				// 把 [start + deleteCount, len) 的元素向左移动 |netChange| 个位置
				for (long i = start + (long)deleteCount; i < len; i++)
				{
					NaNBoxing src = default;
					{
						uint block_index = (uint)i / SPARSE_BLOCK_SIZE;
						NaNBoxing[] block;
						if (block_index == _last_src_blockid && _last_srcblock != null)
						{
							block = _last_srcblock;
							_last_src_blockid = block_index;

							src = block[i % SPARSE_BLOCK_SIZE];
						}
						else if (block_index == _last_src_blockid)
						{
							src.setFault();
						}
						else if (sparse_map.TryGetValue(block_index, out block))
						{
							src = block[i % SPARSE_BLOCK_SIZE];

							_last_src_blockid = block_index;
							_last_srcblock = block;

						}
						else
						{
							_last_src_blockid = block_index;
							_last_srcblock = null;

							src.setFault();
						}
					}

					if (src.ValueType == BoxType.Fault)
					{

						uint block_index = (uint)(i + netChange) / SPARSE_BLOCK_SIZE;
						NaNBoxing[] block;
						if (block_index == _last_dst_blockid && _last_dstblock != null)
						{

						}
						else if (block_index == _last_dst_blockid)
						{
							continue;
						}
						else if (sparse_map.TryGetValue(block_index, out block))
						{
							_last_dst_blockid = block_index;
							_last_dstblock = block;
						}
						else
						{
							continue; //跳过都是空洞.
						}

					}

					// 计算目标位置
					long dstIndex = i + netChange; // netChange 是负数
					Debug.Assert(dstIndex >= 0);
					// 写入目标位置 
					{
						if (_last_dst_blockid == (uint)(i + netChange) / SPARSE_BLOCK_SIZE && _last_dstblock != null)
						{
							_last_dstblock[dstIndex % SPARSE_BLOCK_SIZE] = src;
						}
						else
						{
							var oldsize = Size;
							NaNBoxing[] dstBlock = GetOrCreateBlock((uint)dstIndex);
							if (context.player.Context.GC.MemUsage - oldsize + Size >= context.player.Context.GC.USAGE_LIMIT)
							{
								context.player.RaiseOutOfMemory(ref error);
								return;
							}
							dstBlock[dstIndex % SPARSE_BLOCK_SIZE] = src;

							_last_dst_blockid = (uint)(i + netChange) / SPARSE_BLOCK_SIZE;
							_last_dstblock = dstBlock;

						}
					}
				}
				// 清除末尾多余范围 [newLen, len)
				// 删除 sparse_map 中索引 >= newLen 的块
				var keysToRemove = new List<uint>();
				foreach (var kv in sparse_map)
				{
					if (kv.Key * SPARSE_BLOCK_SIZE >= newLen)
					{
						keysToRemove.Add(kv.Key);
					}
				}
				foreach (var key in keysToRemove)
				{
					sparse_map.Remove(key);
				}

			}
			else
			{
				// netChange == 0：删除和插入数量相同 → 直接覆盖
				// 清除 [start, start + deleteCount) 范围
				for (uint i = (uint)start; i < (uint)start + deleteCount; i++)
				{
					uint block_index = i / SPARSE_BLOCK_SIZE;
					NaNBoxing[] block;
					if (sparse_map.TryGetValue(block_index, out block))
					{
						block[i % SPARSE_BLOCK_SIZE].setFault();
					}
				}

			}

			array_len = (uint)newLen;
			// 清理最后一个块的超出 array_len 部分
			if (array_len > 0)
			{
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


		internal void DoUnshift(Player player, ref ReceiveError error, Span<NaNBoxing> restSpan)
		{
			Debug.Assert(HEAPINSTANCE_PTR == 0);
			if (StoreMode == ArrayStoreMode.cache_on_stack)
			{
				if (restSpan.Length + array_len > store_memory.Length)
				{
					int heaparrayptr = ChangeStoreToHeap(player, ref error);
					if (error.raised)
					{
						return;
					}

					RtArray heap = (RtArray)player.Context.GC.Heap[heaparrayptr];
					heap.DoUnshift(player, ref error, restSpan);
					return;
				}


				var stack_span = store_memory.Span;
				for (int i = (int)array_len - 1; i >= 0; i--)
				{
					var box = stack_span[i];
					
					stack_span[i + restSpan.Length] = box;
					
					stack_span[i].setFault();
				}

				array_len = array_len + (uint)restSpan.Length;

			}
			else if (StoreMode == ArrayStoreMode.cache)
			{
				var cache_store = store_memory.Span;

				if (restSpan.Length + array_len > cache_store.Length)
				{
					int heaparrayptr = ChangeStoreToHeap(player, ref error);
					if (error.raised)
					{
						return;
					}

					RtArray heap = (RtArray)player.Context.GC.Heap[heaparrayptr];
					heap.DoUnshift(player, ref error, restSpan);
					return;
				}



				for (int i = (int)array_len - 1; i >= 0; i--)
				{
					var box = cache_store[i];
					if (box.IsStruct())//box.ValueType == NaNBoxing.BoxType.HeapPtr && box.HeapKind == (byte)RtHeapTypeKind.INSTANCE)
					{
						var src = player.Context.GC.Heap[box.HeapPtr];
						Debug.Assert(((ASInstance)src.Type).Flags.HasFlag(ClassFlags.Struct));

						var dst = player.Context.GC.Heap[ cache_struct_ptr + i+restSpan.Length ];
								
						CopyStruct(dst, src, player);

						cache_store[i + restSpan.Length].SetHeapPtr(cache_struct_ptr + i+restSpan.Length, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.FLAG_STRUCT );

					}
					else
					{
						cache_store[i + restSpan.Length] = box;
					}
					cache_store[i].setFault();
				}


				array_len = array_len + (uint)restSpan.Length;

			}
			else
			{
				NaNBoxing[] _last_srcblock = null; uint _last_src_blockid = uint.MaxValue;
				NaNBoxing[] _last_dstblock = null; uint _last_dst_blockid = uint.MaxValue;


				for (long i = (long)array_len - 1; i >= 0; --i)
				{
					NaNBoxing dst = default;
					{
						uint block_index = ((uint)i + (uint)restSpan.Length) / SPARSE_BLOCK_SIZE;
						NaNBoxing[] block;

						if (_last_dst_blockid == block_index && _last_dstblock != null)
						{
							dst = _last_dstblock[(i + restSpan.Length) % SPARSE_BLOCK_SIZE];
						}
						else if (_last_dst_blockid == block_index)
						{
							dst.setFault();
						}
						else if (sparse_map.TryGetValue(block_index, out block))
						{
							dst = block[(i + restSpan.Length) % SPARSE_BLOCK_SIZE];

							_last_dstblock = block;
							_last_dst_blockid = block_index;

						}
						else
						{
							_last_dstblock = null;
							_last_dst_blockid = block_index;

							dst.setFault();
						}
					}

					NaNBoxing src = default;
					{
						uint block_index = (uint)(i) / SPARSE_BLOCK_SIZE;
						NaNBoxing[] block;

						if (_last_src_blockid == block_index && _last_srcblock != null)
						{
							src = _last_srcblock[(i) % SPARSE_BLOCK_SIZE];
						}
						else if (_last_src_blockid == block_index)
						{
							src.setFault();
						}
						else if (sparse_map.TryGetValue(block_index, out block))
						{
							src = block[(i) % SPARSE_BLOCK_SIZE];

							_last_srcblock = block;
							_last_src_blockid = block_index;

						}
						else
						{
							_last_srcblock = null;
							_last_src_blockid = block_index;

							src.setFault();
						}
					}

					if (dst.ValueType == NaNBoxing.BoxType.Fault && src.ValueType == NaNBoxing.BoxType.Fault)
					{
						continue;
					}

					//复制过去。
					{
						if (_last_dst_blockid == ((uint)i + (uint)restSpan.Length) / SPARSE_BLOCK_SIZE && _last_dstblock != null)
						{
							_last_dstblock[(i + (uint)restSpan.Length) % SPARSE_BLOCK_SIZE] = src;
						}
						else
						{
							var oldsize = Size;
							NaNBoxing[] block = GetOrCreateBlock(((uint)i + (uint)restSpan.Length));
							if (player.Context.GC.MemUsage - oldsize + Size >= player.Context.GC.USAGE_LIMIT)
							{
								player.RaiseOutOfMemory(ref error);
								return;
							}

							block[(i + (uint)restSpan.Length) % SPARSE_BLOCK_SIZE] = src;
						}

						if (src.ValueType != BoxType.Fault)
						{
							_last_srcblock[(i) % SPARSE_BLOCK_SIZE].setFault();
						}

					}

				}

				array_len = array_len + (uint)restSpan.Length;


			}



		}


		internal void DoShift(Player player, ref ReceiveError error)
		{
			Debug.Assert(HEAPINSTANCE_PTR == 0);
			if (StoreMode == ArrayStoreMode.cache_on_stack)
			{
				var stack_span = store_memory.Span;
				for (int i = 1; i < stack_span.Length; i++)
				{
					var box = stack_span[i]; // cache_on_stack和normal一样，结构体都在堆中，不需要考虑缓存问题.
					stack_span[i - 1] = box;
				}


			}
			else if (StoreMode == ArrayStoreMode.cache)
			{
				var cache_store = store_memory.Span;
				for (int i = 1; i < cache_store.Length; i++)
				{
					var box = cache_store[i];
					if (box.IsStruct())//box.ValueType == NaNBoxing.BoxType.HeapPtr && box.HeapKind == (byte)RtHeapTypeKind.INSTANCE)
					{
						var src = player.Context.GC.Heap[box.HeapPtr];
						Debug.Assert(((ASInstance)src.Type).Flags.HasFlag(ClassFlags.Struct));
						//{
						var dst = player.Context.GC.Heap[cache_struct_ptr + i - 1];
						CopyStruct(dst, src, player);
						cache_store[i - 1].SetHeapPtr( cache_struct_ptr + i - 1, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.FLAG_STRUCT);
						//}
						//else
						//{
						//	cache_store[i - 1] = box;
						//}
					}
					else
					{
						cache_store[i - 1] = box;
					}
				}


			}
			else
			{
				NaNBoxing[] _last_srcblock = null; uint _last_src_blockid = uint.MaxValue;
				NaNBoxing[] _last_dstblock = null; uint _last_dst_blockid = uint.MaxValue;


				for (uint array_index = 1; array_index < array_len; array_index++)
				{

					NaNBoxing dst = default;
					{
						uint block_index = (array_index - 1) / SPARSE_BLOCK_SIZE;
						NaNBoxing[] block;

						if (_last_dst_blockid == block_index && _last_dstblock != null)
						{
							dst = _last_dstblock[(array_index - 1) % SPARSE_BLOCK_SIZE];
						}
						else if (_last_dst_blockid == block_index)
						{
							dst.setFault();
						}
						else if (sparse_map.TryGetValue(block_index, out block))
						{
							dst = block[(array_index - 1) % SPARSE_BLOCK_SIZE];

							_last_dstblock = block;
							_last_dst_blockid = block_index;
						}
						else
						{
							_last_dstblock = null;
							_last_dst_blockid = block_index;
							dst.setFault();
						}
					}

					NaNBoxing src = default;
					{
						uint block_index = (array_index) / SPARSE_BLOCK_SIZE;
						NaNBoxing[] block;

						if (_last_src_blockid == block_index && _last_srcblock != null)
						{
							src = _last_srcblock[(array_index) % SPARSE_BLOCK_SIZE];
						}
						else if (_last_src_blockid == block_index)
						{
							src.setFault();
						}
						else if (sparse_map.TryGetValue(block_index, out block))
						{
							src = block[(array_index) % SPARSE_BLOCK_SIZE];

							_last_srcblock = block;
							_last_src_blockid = block_index;
						}
						else
						{
							_last_srcblock = null;
							_last_src_blockid = block_index;
							src.setFault();
						}
					}

					if (dst.ValueType == NaNBoxing.BoxType.Fault && src.ValueType == NaNBoxing.BoxType.Fault)
					{
						continue;
					}

					//复制过去。
					{
						if (_last_dst_blockid == (array_index - 1) / SPARSE_BLOCK_SIZE && _last_dstblock != null)
						{
							_last_dstblock[(array_index - 1) % SPARSE_BLOCK_SIZE] = src;
						}
						else
						{
							var oldsize = Size;
							NaNBoxing[] block = GetOrCreateBlock(array_index - 1);
							if (player.Context.GC.MemUsage - oldsize + Size >= player.Context.GC.USAGE_LIMIT)
							{
								player.RaiseOutOfMemory(ref error);
								return;
							}

							block[(array_index - 1) % SPARSE_BLOCK_SIZE] = src;
						}
					}

				}

				// 清理最后一个块的超出 array_len 部分
				if (array_len > 0)
				{
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

		}


		internal void Swap(uint i, uint j, Context context, ref ReceiveError error, int tempslot)
		{
			Debug.Assert(HEAPINSTANCE_PTR == 0);

			if (i == j)
			{
				return;
			}

			if (StoreMode == ArrayStoreMode.cache_on_stack)
			{
				var span = store_memory.Span; 

				var v1 = span[(int)i];
				var v2 = span[(int)j];

				span[(int)i] = v2;
				span[(int)j] = v1;
			}
			else if (StoreMode == ArrayStoreMode.cache)
			{
				var cache_store = store_memory.Span;
				
				var v1 = cache_store[(int)i];
				var v2 = cache_store[(int)j];

				bool v1isstruct = v1.IsStruct();
				if (v1isstruct)//v1.ValueType == NaNBoxing.BoxType.HeapPtr && v1.HeapKind == (byte)RtHeapTypeKind.INSTANCE)
				{
					var src = context.GC.Heap[v1.HeapPtr];
					Debug.Assert(((ASInstance)src.Type).Flags.HasFlag(ClassFlags.Struct));
					{
						//v1isstruct = true;

						int clonedptr = tempslot + Context.CacheInstancePtr;
						var dst = context.GC.Heap[clonedptr];

						CopyStruct(dst, src, context.player);
						context.StackSlots[tempslot].SetHeapPtr(clonedptr, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.FLAG_STRUCT);

					}
					//else
					//{
					//	context.StackSlots[tempslot] = v1;
					//}
				}
				else
				{
					context.StackSlots[tempslot] = v1;
				}

				//v2->v1
				if (v2.IsStruct())//v2.ValueType == NaNBoxing.BoxType.HeapPtr && v2.HeapKind == (byte)RtHeapTypeKind.INSTANCE)
				{
					var src = context.GC.Heap[v2.HeapPtr];
					Debug.Assert(((ASInstance)src.Type).Flags.HasFlag(ClassFlags.Struct));
					{
						var dst = context.GC.Heap[cache_struct_ptr + (int)i];
						CopyStruct(dst, src, context.player);
						cache_store[(int)i].SetHeapPtr(cache_struct_ptr + (int)i, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.FLAG_STRUCT);
					}
					//else
					//{
					//	cache_store[i] = v2;
					//}
				}
				else
				{
					cache_store[(int)i] = v2;
				}

				//context.StackSlots[context.StackPosition - 2] -> v2
				if (v1isstruct)
				{
					var src = context.GC.Heap[context.StackSlots[tempslot].HeapPtr];
					var dst = context.GC.Heap[cache_struct_ptr +(int)j];
					CopyStruct(dst, src, context.player);
					cache_store[(int)j].SetHeapPtr(cache_struct_ptr + (int)j, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.FLAG_STRUCT);

				}
				else
				{
					cache_store[(int)j] = context.StackSlots[tempslot];
				}

			}
			else
			{
				NaNBoxing v1 = default;
				NaNBoxing[] v1block;

				{
					uint block_index = i / SPARSE_BLOCK_SIZE;


					if (sparse_map.TryGetValue(block_index, out v1block))
					{
						v1 = v1block[(i) % SPARSE_BLOCK_SIZE];
					}
					else
					{
						v1.setFault();
					}
				}

				NaNBoxing v2 = default;
				NaNBoxing[] v2block;
				{
					uint block_index = j / SPARSE_BLOCK_SIZE;

					if (block_index == i / SPARSE_BLOCK_SIZE)
					{
						if (v1.ValueType == BoxType.Fault)
						{
							v2block = null;
							v2.setFault();
						}
						else
						{
							v2block = v1block;
							v2 = v2block[j % SPARSE_BLOCK_SIZE];
						}
					}
					else if (sparse_map.TryGetValue(block_index, out v2block))
					{
						v2 = v2block[j % SPARSE_BLOCK_SIZE];
					}
					else
					{
						v2.setFault();
					}
				}

				if (v1.Raw != v2.Raw)
				{
					if (v1block == null)
					{
						var oldsize = Size;
						v1block = GetOrCreateBlock(i);
						if (context.GC.MemUsage - oldsize + Size >= context.GC.USAGE_LIMIT)
						{
							context.player.RaiseOutOfMemory(ref error);
							return;
						}
					}
					if (v2block == null)
					{
						var oldsize = Size;
						v2block = GetOrCreateBlock(j);
						if (context.GC.MemUsage - oldsize + Size >= context.GC.USAGE_LIMIT)
						{
							context.player.RaiseOutOfMemory(ref error);
							return;
						}
					}
					v1block[i % SPARSE_BLOCK_SIZE] = v2;
					v2block[j % SPARSE_BLOCK_SIZE] = v1;
				}

			}


		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal bool TrySetSlotIfReplaceStructOrNotHeap(NaNBoxing box, uint array_index, Player player, out RtArray target, ref ReceiveError error)
		{
			Debug.Assert(HEAPINSTANCE_PTR == 0);

			//if (HEAPINSTANCE_PTR == 0)
			{
				return DoTrySetSlotIfReplaceStructOrNotHeap(box, array_index, player,out target, ref error);
			}
			//else
			//{
			//	RtArray target;
			//	HEAPINSTANCE_PTR = FindAndUpdateHeapInstancePtr(HEAPINSTANCE_PTR, player, out target);
			//	return target.DoTrySetSlotIfReplaceStructOrNotHeap(box, array_index, player, ref error);
			//}

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void CopyStruct(RtHeapBase dst, RtHeapBase src, Player player)
		{
			if(dst == src) return;

			//dst.Type = src.Type;
			//((RtInstance)dst).HEAPINSTANCE_PTR = 0;
			//((RtInstance)dst).methodscopeslot_ref_state = 0;
			//((RtInstance)dst).CopyFrom(src, player, src.Type._link_codescope.TypeLayout.Size);

			((RtInstance)dst).CloneOther((RtInstance)src, player);
		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private bool DoTrySetSlotIfReplaceStructOrNotHeap(NaNBoxing box, uint array_index, Player player,out RtArray arr ,ref ReceiveError error)
		{
			if (StoreMode == ArrayStoreMode.cache_on_stack)
			{
				if (array_index >= store_memory.Length)
				{
					ChangeStoreToHeap(array_index, player, ref error, out arr);
					if (error.raised)
					{
						return false;
					}

					//return TrySetSlotIfReplaceStructOrNotHeap(box, array_index, player, ref error);
					Debug.Assert(arr.HEAPINSTANCE_PTR == 0);
					return arr.DoTrySetSlotIfReplaceStructOrNotHeap(box, array_index, player,out arr, ref error);

					//throw new NotImplementedException();
				}
				else if (box.ValueType == NaNBoxing.BoxType.HeapPtr)
				{
					arr = this;
					if ( box.IsStruct() )//box.HeapKind == (byte)RtHeapTypeKind.INSTANCE)
					{
						var src = player.Context.GC.Heap[box.HeapPtr];
						Debug.Assert(((ASInstance)src.Type).Flags.HasFlag(ClassFlags.Struct));
						{
							var stack_span = store_memory.Span;

							var dst_v = stack_span[(int)array_index];
							if (dst_v.IsStruct())//dst_v.ValueType == NaNBoxing.BoxType.HeapPtr)
							{
								var dst = player.Context.GC.Heap[dst_v.HeapPtr];
								Debug.Assert(dst.Kind == RtHeapTypeKind.INSTANCE && ((ASInstance)dst.Type).Flags.HasFlag(ClassFlags.Struct));
								{
									CopyStruct(dst, src, player);
									if (array_index + 1 > array_len)
									{
										array_len = array_index + 1;
									}
									return true;
								}
								//else
								//{
								//	return false;
								//}
							}
							else
							{
								return false;
							}
						}
						//else
						//{
						//	return false;
						//}
					}
					else
					{
						return false;
					}
				}
				else
				{
					arr = this;

					var stack_span = store_memory.Span;
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
				var cache_store = store_memory.Span;
				if (array_index >= cache_store.Length)
				{
					ChangeStoreToHeap(array_index, player, ref error, out arr);
					if (error.raised)
					{
						return false;
					}
					Debug.Assert(arr.HEAPINSTANCE_PTR == 0);
					return arr.DoTrySetSlotIfReplaceStructOrNotHeap(box, array_index, player,out arr, ref error);
					//throw new NotImplementedException();
				}
				else if (box.ValueType == NaNBoxing.BoxType.HeapPtr)
				{
					arr = this;

					if (box.IsStruct())//box.HeapKind == (byte)RtHeapTypeKind.INSTANCE)
					{
						var src = player.Context.GC.Heap[box.HeapPtr];
						Debug.Assert(((ASInstance)src.Type).Flags.HasFlag(ClassFlags.Struct));
						{
							var dst = player.Context.GC.Heap[cache_struct_ptr + (int)array_index];
							CopyStruct(dst, src, player);
							cache_store[(int)array_index].SetHeapPtr(cache_struct_ptr + (int)array_index, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.FLAG_STRUCT);

							if (array_index + 1 > array_len)
							{
								array_len = array_index + 1;
							}

							return true;
						}
						//else
						//{
						//	return false;
						//}
					}
					else
					{
						return false;
					}
				}
				else
				{
					arr = this;

					cache_store[(int)array_index] = box;

					if (array_index + 1 > array_len)
					{
						array_len = array_index + 1;
					}

					return true;
				}
			}
			else
			{
				arr = this;

				var oldsize = Size;
				NaNBoxing[] block = GetOrCreateBlock(array_index);
				if (player.Context.GC.MemUsage - oldsize + Size >= player.Context.GC.USAGE_LIMIT)
				{
					player.RaiseOutOfMemory(ref error);
					return false;
				}




				if (box.ValueType == NaNBoxing.BoxType.HeapPtr)
				{
					if (box.HeapKind == (byte)RtHeapTypeKind.INSTANCE)
					{
						
						if (box.IsStruct() //((ASInstance)src.Type).Flags.HasFlag(ClassFlags.Struct)
							&&
							array_index < array_len
							)
						{
							var dst_v = block[array_index % SPARSE_BLOCK_SIZE];
							if (dst_v.IsStruct() )//dst_v.ValueType == NaNBoxing.BoxType.HeapPtr && dst_v.HeapKind == (byte)RtHeapTypeKind.INSTANCE)
							{
								var src = player.Context.GC.Heap[box.HeapPtr];
								var dst = player.Context.GC.Heap[dst_v.HeapPtr];
								Debug.Assert(((ASInstance)dst.Type).Flags.HasFlag(ClassFlags.Struct));
								Debug.Assert(((ASInstance)src.Type).Flags.HasFlag(ClassFlags.Struct));
								{
									CopyStruct(dst, src, player);
									return true;
								}
								//else
								//{
								//	return false;
								//}
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
					block[array_index % SPARSE_BLOCK_SIZE] = box;
					if (array_index + 1 > array_len)
					{
						array_len = array_index + 1;
					}

					return true;
				}



			}
		}



		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public NaNBoxing ReadSlot(uint array_index, Player player, out bool isoutofindex_or_ishole)
		{
			Debug.Assert(HEAPINSTANCE_PTR == 0);

			//if (HEAPINSTANCE_PTR == 0)
			{
			//	return DoReadSlot(array_index, player, out isoutofindex_or_ishole);
			}
			//else
			//{
			//	RtArray target;
			//	HEAPINSTANCE_PTR = FindAndUpdateHeapInstancePtr(HEAPINSTANCE_PTR, player, out target);
			//	return target.DoReadSlot(array_index, player, out isoutofindex_or_ishole);
			//}

			if (StoreMode != ArrayStoreMode.normal)
			{
				if (array_index < array_len)
				{
					isoutofindex_or_ishole = false;
					var v = store_memory.Span[(int)array_index];
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
			//else if (StoreMode == ArrayStoreMode.cache)
			//{
			//	if (array_index < array_len)
			//	{

			//		var v = cache_store[(int)array_index];
			//		if (v.ValueType == NaNBoxing.BoxType.Fault)
			//		{
			//			isoutofindex_or_ishole = true;
			//			v.SetUndefined();
			//			return v;
			//		}
			//		else
			//		{
			//			isoutofindex_or_ishole = false;
			//			return v;
			//		}

			//	}
			//	else
			//	{
			//		isoutofindex_or_ishole = true;
			//		NaNBoxing v = default;
			//		v.SetUndefined();
			//		return v;
			//	}

			//	//throw new NotImplementedException();
			//}
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

		//[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
		//private NaNBoxing DoReadSlot(uint array_index, Player player, out bool isoutofindex_or_ishole)
		//{
			
		//}




		//internal void SetIsRest(bool v)
		//{
		//	if (v)
		//	{
		//		storeMode = (short)((storeMode & 0xff) | 0x100);
		//	}
		//	else
		//	{
		//		storeMode &= 0xff;
		//	}
		//}

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

		//internal bool isRest()
		//{
		//	return (storeMode >> 8) == 0x1;
		//}

		internal void CopyCacheFrom(RtArray arr_store, Player player, Memory<NaNBoxing> cachestore, int cachestruct_p)
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
			store_memory = cachestore;
			cache_struct_ptr = cachestruct_p;
			
			
			HEAPINSTANCE_PTR = 0;
			m_property_ptr = arr_store.m_property_ptr;

			array_len = arr_store.array_len;

			var cache_store = store_memory.Span;
			var src_store = arr_store.store_memory.Span;

			for (int i = 0; i < MAX_CACHE_ELEMENT; i++)
			{
				if (src_store[i].ValueType == NaNBoxing.BoxType.HeapPtr)
				{
					if (src_store[i].HeapPtr == arr_store.cache_struct_ptr + i)
					{
						var dst = player.Context.GC.Heap[cache_struct_ptr + (int)i];
						var src = player.Context.GC.Heap[arr_store.cache_struct_ptr + i];

						CopyStruct(dst, src, player);

						cache_store[i].SetHeapPtr(cache_struct_ptr + (int)i, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.FLAG_STRUCT);
					}
					else
					{
						//除了struct,凡是存入array的对象都禁用cache对象，完事。。。
						cache_store[i] = src_store[i];
					}
				}
				else
				{
					cache_store[i] = src_store[i];
				}

			}


		}

		internal void Trace(Context context, int stackStPos, ref ReceiveError error, int scope_ptr, IPrint printer, RtHeapBase arrObj, ReadOnlySpan<char> sep)
		{
			if (HEAPINSTANCE_PTR == 0)
			{
				DoTrace(context, stackStPos, ref error, scope_ptr, printer, arrObj, sep);
			}
			else
			{
				RtArray target;
				FindAndUpdateHeapInstancePtr(HEAPINSTANCE_PTR, context.player, out target);
				target.DoTrace(context, stackStPos, ref error, scope_ptr, printer, arrObj, sep);
			}
		}

		private void DoTrace(Context context, int stackStPos, ref ReceiveError error, int scope_ptr, IPrint printer, RtHeapBase arrObj, ReadOnlySpan<char> sep)
		{
			if (StoreMode != ArrayStoreMode.normal)
			{
				var stack_span = store_memory.Span;
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
						NaNBoxing l = context.player.LoadSlotFromArray((uint)i, arrObj, out isoutofindex_or_ishole);
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
			//else if (StoreMode == ArrayStoreMode.cache)
			//{
			//	for (int i = 0; i < array_len; i++)
			//	{

			//		if (cache_store[i].ValueType != NaNBoxing.BoxType.Null && cache_store[i].ValueType != NaNBoxing.BoxType.Undefined && cache_store[i].ValueType != NaNBoxing.BoxType.Fault)
			//		{
			//			TopLevel.TraceElement(cache_store[i], context, stackStPos, ref error, scope_ptr, default, printer);
			//			if (error.raised)
			//			{
			//				return;
			//			}
			//		}
			//		else
			//		{
			//			bool isoutofindex_or_ishole;
			//			NaNBoxing l = context.player.LoadSlotFromArray((uint)i, arrObj, out isoutofindex_or_ishole);
			//			if (l.ValueType != NaNBoxing.BoxType.Fault && l.ValueType != NaNBoxing.BoxType.Null && l.ValueType != NaNBoxing.BoxType.Undefined)
			//			{
			//				TopLevel.TraceElement(l, context, stackStPos, ref error, scope_ptr, default, printer);
			//				if (error.raised)
			//				{
			//					return;
			//				}
			//			}
			//		}

			//		if (i < array_len - 1)
			//		{
			//			printer.Write(sep);
			//		}
			//	}
			//}
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

		internal bool TryReadIterItem(int index, out uint key, out uint next_index, out NaNBoxing v, Context context)
		{
			if (HEAPINSTANCE_PTR == 0)
			{
				return DoTryReadIterItem(index, out key, out next_index, out v);
			}
			else
			{
				RtArray target;
				FindAndUpdateHeapInstancePtr(HEAPINSTANCE_PTR, context.player, out target);
				return target.DoTryReadIterItem(index, out key, out next_index, out v);
			}
		}

		private bool DoTryReadIterItem(int index, out uint key, out uint next_index, out NaNBoxing v)
		{
			if (StoreMode != ArrayStoreMode.normal)
			{
				var stack_span = store_memory.Span;

				next_index = 0; key = 0;
				v = default;

				for (int i = index; i < array_len; i++)
				{
					if (//stack_span[i].ValueType != NaNBoxing.BoxType.Null &&
						//stack_span[i].ValueType != NaNBoxing.BoxType.Undefined && 
						stack_span[i].ValueType != NaNBoxing.BoxType.Fault)
					{
						key = (uint)i;
						next_index = (uint)i + 1;
						v = stack_span[i];

						return true;

					}

				}
				return false;
			}
			//else if (StoreMode == ArrayStoreMode.cache)
			//{
			//	key = 0;
			//	next_index = 0;
			//	v = default;
			//	for (int i = index; i < array_len; i++)
			//	{

			//		if (//cache_store[i].ValueType != NaNBoxing.BoxType.Null &&
			//			//cache_store[i].ValueType != NaNBoxing.BoxType.Undefined && 
			//			cache_store[i].ValueType != NaNBoxing.BoxType.Fault)
			//		{
			//			key = (uint)i;
			//			next_index = (uint)i + 1;
			//			v = cache_store[i];

			//			return true;

			//		}

			//	}
			//	return false;
			//}
			else
			{
				key = 0;
				next_index = 0;
				v = default;

				if (array_len > (uint)index)
				{
					uint last_block_id = (array_len - 1) / SPARSE_BLOCK_SIZE;

					uint j_start = ((uint)index) % SPARSE_BLOCK_SIZE;

					var maps = sparse_map.OrderBy(i => i.Key).Where(i => i.Key >= (uint)index / SPARSE_BLOCK_SIZE);

					foreach (var kv in maps)
					{
						uint i = kv.Key;

						NaNBoxing[] block = kv.Value;

						for (uint j = j_start; j < SPARSE_BLOCK_SIZE; j++)
						{
							uint current = i * SPARSE_BLOCK_SIZE + j;

							if (current >= array_len)
							{
								return false;
							}


							{
								if (//block[j].ValueType != NaNBoxing.BoxType.Null &&
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

		internal void CopyFromArray(Span<NaNBoxing> values, Player player, ref ReceiveError error)
		{
			Debug.Assert(StoreMode == ArrayStoreMode.normal);
			Debug.Assert(array_len == values.Length);



			NaNBoxing[] lastblock = null;
			uint last_block_index = uint.MaxValue;
			for (int i = 0; i < array_len; i++)
			{
				var v = values[i];

				uint block_index = (uint)i / SPARSE_BLOCK_SIZE;



				if (v.ValueType != BoxType.Fault)
				{
					if (last_block_index == block_index && lastblock != null)
					{
						lastblock[i % SPARSE_BLOCK_SIZE] = v;
					}
					else
					{

						NaNBoxing[] block = null;
						if (sparse_map.TryGetValue(block_index, out block))
						{

						}
						else
						{
							var oldsize = Size;
							block = GetOrCreateBlock((uint)i);

							if (player.Context.GC.MemUsage - oldsize + Size >= player.Context.GC.USAGE_LIMIT)
							{
								player.RaiseOutOfMemory(ref error);
								return;
							}
						}

						block[i % SPARSE_BLOCK_SIZE] = v;

						last_block_index = block_index;
						lastblock = block;
					}
				}
				else
				{
					if (last_block_index == block_index && lastblock != null)
					{
						lastblock[i % SPARSE_BLOCK_SIZE] = v;
					}
					else
					{
						NaNBoxing[] block = null;
						if (sparse_map.TryGetValue(block_index, out block))
						{
							lastblock[i % SPARSE_BLOCK_SIZE] = v;

							last_block_index = block_index;
							lastblock = block;
						}
						else
						{
							last_block_index = block_index;
							lastblock = null;
						}
					}
				}

			}

		}
	}
}
