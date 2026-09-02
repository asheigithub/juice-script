using juicescript.ABC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using static juicescript.runtime.Player;

namespace juicescript.runtime
{
	/// <summary>
	/// 负载对象实例ASInstance
	/// </summary>
#if FORCOMPILER
    internal
#else
	public
#endif
		sealed class RtInstance : RtHeapBase
    {
		public RtInstance() : base(RtHeapTypeKind.INSTANCE) { }

		/// <summary>
		/// 缓存对象的最大成员大小
		/// </summary>
		public const int MAX_CACHEABLE_SIZE = 16 * 8;

		private static int DoFindAndUpdatePtr( int ptr, Player player, ASInstance type , out RtInstance target)
		{
			
			var ref_instance = player.Context.GC.Heap[ptr];

			
			if (ref_instance.Kind == RtHeapTypeKind.VECTOR)
			{
				target = null;
				return 0;
			}
			


			var payload = ((RtInstance)ref_instance);
			var origin = payload;
			target = origin;


			if (type.Flags.HasFlag(ClassFlags.Struct) && ((ASInstance)ref_instance.Type).Flags.HasFlag( ClassFlags.Struct ) && type != ref_instance.Type )
			{
#if DEBUG
				if (payload.HEAPINSTANCE_PTR != 0)   //struct 的内部成员引用不可能有多级
				{
					throw new InvalidOperationException();
				}
#endif
				
				return -ptr ;
			}



			while (payload.HEAPINSTANCE_PTR != 0)
			{
#if DEBUG
				if (player.Context.GC.Heap[payload.HEAPINSTANCE_PTR].Kind == RtHeapTypeKind.VECTOR)
				{
					throw new InvalidOperationException(); //vector 的struct引用不可能多次跳转
				}
#endif

				ptr = payload.HEAPINSTANCE_PTR;
				payload = ((RtInstance)player.Context.GC.Heap[ptr]);
				target = payload;

				origin.HEAPINSTANCE_PTR = ptr;//更新,避免后续跳转
			}

			return ptr;
		}

		internal int FindAndUpdateHeapInstancePtr(Player player, out RtInstance target)
		{
			Debug.Assert(HEAPINSTANCE_PTR != 0);
			return DoFindAndUpdatePtr(HEAPINSTANCE_PTR, player, (ASInstance)Type, out target);
			
		}

        internal static int FindAndUpdateHeapInstancePtr(int ptr, Player player,out RtInstance target)
        {
			RtHeapBase tmp = player.Context.GC.Heap[ptr];
			RtInstance check = (RtInstance)tmp;

			if (((ASInstance)tmp.Type).Flags.HasFlag(ClassFlags.Struct))
			{
				if (check.HEAPINSTANCE_PTR != 0)
				{

					
					var tmp2 = player.Context.GC.Heap[check.HEAPINSTANCE_PTR];

					if (tmp2.Kind == RtHeapTypeKind.INSTANCE && tmp2.Type != tmp.Type)
					{
#if DEBUG
						if (((RtInstance)tmp2).HEAPINSTANCE_PTR != 0)
						{
							throw new InvalidOperationException();
						}
#endif
						target = check;
						return ptr;
					}
					else if (tmp2.Kind == RtHeapTypeKind.VECTOR)
					{
#if DEBUG
						if (((RtVector)tmp2).HEAPINSTANCE_PTR != 0)
						{
							throw new InvalidOperationException();
						}
#endif

						target = check;
						return ptr;
					}
#if DEBUG

					if (tmp2.Kind == RtHeapTypeKind.VECTOR)
					{
						throw new InvalidOperationException();
					}

#endif

					return DoFindAndUpdatePtr( check.HEAPINSTANCE_PTR, player, (ASInstance)tmp.Type, out target);
				}
				else
				{
					target = check;
					return ptr;
				}
			}
			else
			{
				if (check.HEAPINSTANCE_PTR == 0)
				{
					target = check;
					return ptr;
				}
				else
				{
					return DoFindAndUpdatePtr( check.HEAPINSTANCE_PTR , player, (ASInstance)tmp.Type, out target);
				}
			}
		}


        private Memory<byte> store;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Span<byte> GetStoreData(Player player,ASInstance type)
        {
			if (HEAPINSTANCE_PTR == 0)
			{
				return store.Span;
			}
			else
			{
				return GetStoreData(player, type, out bool is_ref_vector, out bool is_ref_struct, out RtInstance target);
			}
        }

		private Span<byte> GetStoreData(Player player,ASInstance type , out bool is_ref_vector,out bool is_ref_struct,out RtInstance target)
		{
			if (HEAPINSTANCE_PTR == 0)
			{
				target = this;
				is_ref_struct = false;
				is_ref_vector = false;
				return store.Span;
			}
			else
			{
				
				//RtPayloadInstance target;
				int p = DoFindAndUpdatePtr(  HEAPINSTANCE_PTR, player, type, out target);

				if (p < 0)
				{
					is_ref_struct = true;
					is_ref_vector = false;
					return target.store.Span.Slice( m_property_ptr , type._link_codescope.TypeLayout.Size );
				}
				else if (target != null && target.HEAPINSTANCE_PTR == 0)
				{
					is_ref_struct = false;
					is_ref_vector = false;
					return target.store.Span;
				}
				else
				{
					if (target == null)
					{
						target = this;
					}

					RtVector vector = (RtVector)player.Context.GC.Heap[target.HEAPINSTANCE_PTR];
					is_ref_vector = true;
					is_ref_struct = false;

					

					return vector.ReadStoreOffset(target.m_property_ptr, player,type._link_codescope.TypeLayout.Size);

				}

			}
		}

		//internal void MarkFromContainer()
		//{
		//	Debug.Assert(HEAPINSTANCE_PTR == 0);
		//	m_property_ptr = int.MinValue;
		//}

		internal bool IsRefVectorOrFromContainerOrRefStruct(Player player,ASInstance type)
		{
			if (!type.Flags.HasFlag(ClassFlags.Struct))
			{
				return false;
			}

			
			bool is_ref_vector;bool is_ref_struct;RtInstance target;
			GetStoreData(player, type ,out is_ref_vector,out is_ref_struct, out target);
			return is_ref_vector || is_ref_struct; 
				//|| 
				//target.m_property_ptr == int.MinValue/*标记是数组中获取的struct*/ ;
		}

		//internal bool IsRefStruct(Player player, ASInstance type)
		//{
		//	if (!type.Flags.HasFlag(ClassFlags.Struct))
		//	{
		//		return false;
		//	}

		//	bool is_ref_vector; bool is_ref_struct;
		//	GetStoreData(player, type, out is_ref_vector, out is_ref_struct);
		//	return is_ref_struct;
		//}



		public void GenStore(int size)
        {
			if (size > 0)
			{
				store = new Memory<byte>(new byte[size]);
			}
        }

        private int m_property_ptr;

		internal int inner_struct_ptr
		{
			get { return m_property_ptr; }
			set { m_property_ptr = value; }
		}


        /// <summary>
        /// 动态属性
        /// </summary>
        public int PROPERTY_PTR(Player player,ASInstance type)
        {
            
            if (HEAPINSTANCE_PTR == 0)
            {
                return m_property_ptr;
            }
            else
            {
                RtInstance target;
                DoFindAndUpdatePtr(HEAPINSTANCE_PTR, player, type,out target);
                return target.m_property_ptr;

				//return ((RtPayloadInstance)player.Context.GC.Heap[HEAPINSTANCE_PTR].facility).PROPERTY_PTR(player);
			}
            
        }

        public void Set_PROPERTY_PTR(int ptr, Player player,ASInstance type)
        {
            if (HEAPINSTANCE_PTR == 0)
            {
                m_property_ptr = ptr;
            }
            else
            {
				RtInstance target;
				DoFindAndUpdatePtr(HEAPINSTANCE_PTR, player, type, out target);
                target.m_property_ptr = ptr;

				//((RtPayloadInstance)player.Context.GC.Heap[HEAPINSTANCE_PTR].facility).Set_PROPERTY_PTR(ptr, player);
			}
        }

		private int m__proto__;

		[MethodImpl( MethodImplOptions.AggressiveInlining)]
		public int PROTOTYPE(Player player,ASInstance type)
		{
			if (HEAPINSTANCE_PTR == 0)
			{
				return m__proto__;
			}
			else
			{
				RtInstance target;
				DoFindAndUpdatePtr(HEAPINSTANCE_PTR, player, type, out target);
				return target.m__proto__;
			}
		}

		public void Set_PROTOTYPE(int proto_ptr, Player player)
		{
#if DEBUG
			if (HEAPINSTANCE_PTR != 0)
			{
				throw new InvalidOperationException();
			}
#endif

			
			m__proto__ = proto_ptr;
			
		}

        /// <summary>
        /// wapper的对象不会是cache
        /// </summary>
		internal RtWapperBase wapperedObject;


		/// <summary>
		/// 如果是缓存对象，并且已经被保存到堆中，则保存堆中对象的指针
		/// 后续操作将直接对堆里的对象操作了。
		/// </summary>
		internal int HEAPINSTANCE_PTR { get; private set; }

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		internal void CloneOther(RtInstance other,Player player)
		{
			
			Type = other.Type;

			methodscopeslot_ref_state = 0;
			nextframe_ref_state = default;
			HEAPINSTANCE_PTR = 0;
			CopyFrom(other, player, Type._link_codescope.TypeLayout.Size);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void LinkToPayloadOffset(int offset,int vecPtr)
		{			
			methodscopeslot_ref_state = 0;
			nextframe_ref_state = default;
			m_property_ptr = offset; //标记偏移量.
			HEAPINSTANCE_PTR = vecPtr; //指向Vector.
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		internal void SetDefaultCacheData(int _proto_)
		{
			HEAPINSTANCE_PTR = 0;
			m_property_ptr = 0;
			m__proto__ = _proto_;
			
			methodscopeslot_ref_state = 0;
			nextframe_ref_state = default;
		}


		internal void LinkTo(RtInstance dst, int dstPtr)
		{
			Debug.Assert(HEAPINSTANCE_PTR == 0);
			HEAPINSTANCE_PTR = dstPtr;
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







		public override int Size
        {
            get
            { 
                return store.Length  + 8 + 8 + 8 + 4 + 1;
            }
        }

        /// <summary>
        /// 成员值初始化
        /// </summary>
        /// <param name="typeLayout"></param>
        /// <exception cref="NotImplementedException"></exception>
        internal void Init(CodeScope link_codescope,Player player,bool initmember)
        {
#if DEBUG
            if (HEAPINSTANCE_PTR != 0)
            {
                throw new InvalidOperationException();
            }
#endif

#if FORCOMPILER
            if (isCompiling)
            {
                hasSetData = new bool[link_codescope.TypeLayout.Offset.Count];
            }
#endif

			if (initmember)
			{
#if !FORCOMPILER

				if (!link_codescope._rt_cache_instance_data.IsEmpty)
				{
					link_codescope._rt_cache_instance_data.Span.CopyTo(store.Span);
					return;
				}

#endif

				unsafe
				{
					fixed (byte* p = store.Span)
					{

						for (int i = 0; i < link_codescope.TypeLayout.Offset.Count; i++)
						{

							byte* ptr = p + link_codescope.TypeLayout.Offset[i];
							var member = link_codescope.Members[i];

							if ((member.Kind == ScopeMemberKind.Constant || member.Kind == ScopeMemberKind.Slot) && member._initvalue.HasValue )
							{
								//SetSlot(member.trait.Value.initValue.Value, (ushort)i, player);

								SetSlotDataByValue(member, ptr, member._initvalue.Value);

#if FORCOMPILER
								if (isCompiling)
								{
									hasSetData[i] = true;
								}
#endif

							}
							else
							{
								InitSlotData(member, ptr, link_codescope.TypeLayout.SlotSize[i]);
							}
						}
					}
				}

#if !FORCOMPILER
				link_codescope._rt_cache_instance_data = new Memory<byte>( new byte[ link_codescope.TypeLayout.Size] );
				store.Span.Slice(0,link_codescope.TypeLayout.Size).CopyTo(link_codescope._rt_cache_instance_data.Span);
#endif
			}
        }

		internal unsafe static void InitAtBuffer(void* span,CodeScope link_codescope)
		{

			byte* p = (byte*)span;
			
			for (int i = 0; i < link_codescope.TypeLayout.Offset.Count; i++)
			{

				byte* ptr = p + link_codescope.TypeLayout.Offset[i];
				var member = link_codescope.Members[i];

				if ((member.Kind == ScopeMemberKind.Constant || member.Kind == ScopeMemberKind.Slot) && member.trait.Value != null && member.trait.Value.initValue.HasValue)
				{
					SetSlotDataByValue(member, ptr, member.trait.Value.initValue.Value);
				}
				else
				{
					InitSlotData(member, ptr, link_codescope.TypeLayout.SlotSize[i]);
				}
			}
			
			
		}



        internal static unsafe void InitSlotData(ScopeMember member,void* ptr, int slotSize)
        {
			switch (member.TypeKind)
			{
				case ABC.TypeKind.Any:
					((NaNBoxing*)ptr)->SetUndefined();
					break;
				case ABC.TypeKind.Boolean:
					*(bool*)ptr = false;
					break;
				case ABC.TypeKind.SByte:
					*(sbyte*)ptr = 0;
					break;
				case ABC.TypeKind.Byte:
					*(byte*)ptr = 0;
					break;
				case ABC.TypeKind.Short:
					*(short*)ptr = 0;
					break;
				case ABC.TypeKind.UShort:
					*(ushort*)ptr = 0;
					break;
				case ABC.TypeKind.Int:
					*(int*)ptr = 0;
					break;
				case ABC.TypeKind.Uint:
					*(uint*)ptr = 0;
					break;
				case ABC.TypeKind.Float:
					*(float*)ptr = 0;
					break;
				case ABC.TypeKind.Number:
					*(double*)ptr = double.NaN;
					break;

				case ABC.TypeKind.Fun_Void:
				case ABC.TypeKind.Unknown:
#if DEBUG
					throw new InvalidOperationException();
#else
					Environment.FailFast("出错了，这里跑不到"); return;
#endif

				case ABC.TypeKind.Null:
				case ABC.TypeKind.String:
				case ABC.TypeKind.Function:
				case ABC.TypeKind.Array:
				case ABC.TypeKind.Vector:
				case ABC.TypeKind.Namespace:
				case ABC.TypeKind.Object:
				case ABC.TypeKind.Class:
					((NaNBoxing*)ptr)->SetNull();
					break;
				default:
#if DEBUG
					if ((ulong)member.TypeKind < (ulong)ABC.TypeKind.Object)
					{
						throw new InvalidOperationException();
					}

#endif
					if (((ASInstance)member.DefineAt).Flags.HasFlag(ClassFlags.Struct)
						&&
						//member.__rt_type_class__.Instance.Flags.HasFlag(ClassFlags.Struct)
						member._rt_type_flag.HasFlag(ClassFlags.Struct)
						)
					{
#if DEBUG
						if (slotSize != member.__rt_type_class__.Instance._link_codescope.TypeLayout.Size)
						{
							throw new InvalidOperationException();
						}
#endif

						InitAtBuffer(ptr, member.__rt_type_class__.Instance._link_codescope);
					}
					else
					{
						((NaNBoxing*)ptr)->SetNull();
					}
					break;

			}


		}



#if FORCOMPILER
		internal bool isCompiling;

		bool[] hasSetData;

#endif

		public NaNBoxing ReadSlot(ushort memberIndex,  Player player , int returnSlotIndex ,int this_instance_Ptr)
		{
			//Debug.Assert(Type._link_codescope == codescope);

			var member = Type._link_codescope.Members[memberIndex];
			var layout = Type._link_codescope.TypeLayout;
			
#if FORCOMPILER
			if (isCompiling)
			{
				if (member.Kind != ScopeMemberKind.Constant || !hasSetData[memberIndex])
				{
					throw new EvalConstException();
				}
			}
#endif
			unsafe
			{
				fixed (byte* p = GetStoreData(player, layout.ASType.Instance ))
				{
					NaNBoxing result = new NaNBoxing();
					byte* ptr = p + layout.Offset[memberIndex];

					switch (member.TypeKind)
					{
						case ABC.TypeKind.Any:
							result = *(NaNBoxing*)ptr;
							break;
						case ABC.TypeKind.Boolean:
							result.SetBoolean(*(bool*)ptr);
							break;
						case ABC.TypeKind.SByte:
							result.SetSByte(*(sbyte*)ptr);
							break;
						case ABC.TypeKind.Byte:
							result.SetByte(*(byte*)ptr);
							break;
						case ABC.TypeKind.Short:
							result.SetShort(*(short*)ptr);
							break;
						case ABC.TypeKind.UShort:
							result.SetUShort(*(ushort*)ptr);
							break;
						case ABC.TypeKind.Int:
							result.SetInt(*(int*)ptr);
							break;
						case ABC.TypeKind.Uint:
							result.SetUInt(*(uint*)ptr);
							break;
						case ABC.TypeKind.Float:
							result.SetFloat(*(float*)ptr);
							break;
						case ABC.TypeKind.Number:
							result.SetNumber(*(double*)ptr);
							break;
						case ABC.TypeKind.Null:
						case ABC.TypeKind.String:
						case ABC.TypeKind.Function:
						case ABC.TypeKind.Array:
						case ABC.TypeKind.Vector:
						case ABC.TypeKind.Namespace:
						case ABC.TypeKind.Object:
						case ABC.TypeKind.Class:
							result = *(NaNBoxing*)ptr;
							break;
						case ABC.TypeKind.Fun_Void:
						case ABC.TypeKind.Unknown:
#if DEBUG
							throw new InvalidOperationException();
#else
							Environment.FailFast("出错了，这里跑不到"); return default;
#endif

						//case TypeKind.TraitDataReference:
						//    throw new InvalidOperationException();

						default:

#if DEBUG
							if (this_instance_Ptr == 0)
							{
								if (((ASInstance)member.DefineAt).Flags.HasFlag(ClassFlags.Struct) &&
									member.__rt_type_class__.Instance.Flags.HasFlag(ClassFlags.Struct)
								) //断言，禁止对结构体的嵌套结构体取值走这里
								{
									throw new InvalidOperationException();
								}
							}
#endif

							Debug.Assert(member.DefineAt == Type);

							if (((ASInstance)Type).Flags.HasFlag(ClassFlags.Struct) &&
									//member.__rt_type_class__.Instance.Flags.HasFlag(ClassFlags.Struct)
									member._rt_type_flag.HasFlag(ClassFlags.Struct)
								)
							{
								int cache_ptr = Context.CacheInstancePtr + returnSlotIndex;
								var cache = player.Context.GC.Heap[cache_ptr];

								cache.Type = member.__rt_type_class__.Instance;
								RtInstance struct_payload = (RtInstance)cache;

								struct_payload.methodscopeslot_ref_state = 0;
								struct_payload.nextframe_ref_state = default;
								struct_payload.m_property_ptr = m_property_ptr + layout.Offset[memberIndex]; //标记index.
								struct_payload.HEAPINSTANCE_PTR = HEAPINSTANCE_PTR == 0? this_instance_Ptr : HEAPINSTANCE_PTR ; //指向当前对象.
								

								result.SetHeapPtr(cache_ptr, (byte)RtHeapTypeKind.INSTANCE, (byte)(HeapKindFlag.FLAG_STRUCT | HeapKindFlag.FLAG_REFSTRUCT));
								return result;

							}
							else
							{
								result = *(NaNBoxing*)ptr;
							}
							break;
					}

					return result;
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public NaNBoxing ReadSlot(ushort memberIndex, Player player)
        {
			return ReadSlot(memberIndex, player, -1,0);
        }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetSlot(NaNBoxing value, ushort memberIndex, Player player)
        {
#if FORCOMPILER
            if (isCompiling)
            {
                hasSetData[memberIndex] = true;
            }
#endif

			//Debug.Assert(Type._link_codescope == codeScope || (Extensions.IsExtend(  (ASInstance)Type , codeScope.TypeLayout.ASType.Instance) && memberIndex < Type._link_codescope.Members.Count ) );

			var codeScope = Type._link_codescope;

            var member = codeScope.Members[memberIndex];
            unsafe
            {
                fixed (byte* p = GetStoreData(player, (ASInstance)Type ))//codeScope.TypeLayout.ASType.Instance))
                { 
                    byte* ptr = p + codeScope.TypeLayout.Offset[memberIndex];
					SetSlotDataByValue(member,ptr, value);
                }
            }

        }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal unsafe static void SetSlotDataByValue(TypeKind kind, void* ptr, NaNBoxing value)
		{
			switch (kind)
			{
				case ABC.TypeKind.Any:
					*(NaNBoxing*)ptr = value;
					break;
				case ABC.TypeKind.Boolean:
#if DEBUG
					if (value.ValueType != NaNBoxing.BoxType.Boolean)
						throw new InvalidOperationException();
#endif
					*(bool*)ptr = value.Boolean;
					break;

				case ABC.TypeKind.SByte:
#if DEBUG
					if (value.ValueType != NaNBoxing.BoxType.Sbyte)
						throw new InvalidOperationException();
#endif
					*(sbyte*)ptr = value.SByteValue;
					break;

				case ABC.TypeKind.Byte:
#if DEBUG
					if (value.ValueType != NaNBoxing.BoxType.Byte)
						throw new InvalidOperationException();
#endif
					*(byte*)ptr = value.ByteValue;
					break;
				case ABC.TypeKind.Short:
#if DEBUG
					if (value.ValueType != NaNBoxing.BoxType.Short)
						throw new InvalidOperationException();
#endif
					*(short*)ptr = value.ShortValue;
					break;
				case ABC.TypeKind.UShort:
#if DEBUG
					if (value.ValueType != NaNBoxing.BoxType.UShort)
						throw new InvalidOperationException();
#endif
					*(ushort*)ptr = value.UShortValue;
					break;

				case ABC.TypeKind.Int:
#if DEBUG
					if (value.ValueType != NaNBoxing.BoxType.Int)
						throw new InvalidOperationException();
#endif
					*(int*)ptr = value.IntValue;
					break;
				case ABC.TypeKind.Uint:
#if DEBUG
					if (value.ValueType != NaNBoxing.BoxType.Uint)
						throw new InvalidOperationException();
#endif
					*(uint*)ptr = value.UIntValue;
					break;

				case ABC.TypeKind.Float:
#if DEBUG
					if (value.ValueType != NaNBoxing.BoxType.Float)
						throw new InvalidOperationException();
#endif
					*(float*)ptr = value.FloatValue;
					break;
				case ABC.TypeKind.Number:
#if DEBUG
					if (value.ValueType != NaNBoxing.BoxType.Number)
						throw new InvalidOperationException();
#endif
					*(double*)ptr = value.Number;
					break;

				case ABC.TypeKind.Fun_Void:
				case ABC.TypeKind.Unknown:
#if DEBUG
					throw new InvalidOperationException();
#else
					Environment.FailFast("出错了，这里跑不到"); return;
#endif
				case ABC.TypeKind.Null:
				case ABC.TypeKind.String:
				case ABC.TypeKind.Function:
				case ABC.TypeKind.Array:
				case ABC.TypeKind.Vector:
				case ABC.TypeKind.Namespace:
				case ABC.TypeKind.Object:
				case ABC.TypeKind.Class:
					*(NaNBoxing*)ptr = value;
					break;

				default:
#if DEBUG
					if (value.ValueType != NaNBoxing.BoxType.HeapPtr && value.ValueType != NaNBoxing.BoxType.Null)
					{
						throw new InvalidOperationException();
					}

					
#endif

					*(NaNBoxing*)ptr = value;
					break;

			}
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal unsafe static void SetSlotDataByValue( ScopeMember member,void* ptr, NaNBoxing value)
        {
			SetSlotDataByValue(member.TypeKind, ptr, value);

#if DEBUG
			if (
				member.TypeKind > TypeKind.Class &&
				((ASInstance)member.DefineAt).Flags.HasFlag(ClassFlags.Struct) &&
						member.__rt_type_class__ != null &&
						member.__rt_type_class__.Instance.Flags.HasFlag(ClassFlags.Struct)
						) //断言，禁止对结构体的嵌套结构体走这里赋值
			{
				throw new InvalidOperationException();
			}
#endif

		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		internal bool IsUpdateStructOrEqual(Context contxt, ushort memberIndex, NaNBoxing newValue)
        {
			var type = (ASInstance)Type;
			
#if FORCOMPILER
			if (isCompiling)
			{
				return false;
			}
#endif

			if (newValue.ValueType == NaNBoxing.BoxType.Null)
			{
				if (type.Flags.HasFlag(ClassFlags.Struct)
					&&
					//type._link_codescope.Members[memberIndex].__rt_type_class__.Instance.Flags.HasFlag(ClassFlags.Struct)
					type._link_codescope.Members[memberIndex]._rt_type_flag.HasFlag(ClassFlags.Struct)
					)
				{


					var data = GetStoreData(contxt.player, type).Slice(type._link_codescope.TypeLayout.Offset[memberIndex], type._link_codescope.TypeLayout.SlotSize[memberIndex]);
					unsafe
					{
						fixed (byte* ptr = data)
						{
							InitAtBuffer(ptr, type._link_codescope.Members[memberIndex].__rt_type_class__.Instance._link_codescope);
						}
					}

					return true;
				}
				else
				{
					return false;
				}
			}
			else if (newValue.ValueType != NaNBoxing.BoxType.HeapPtr)
			{
				return false;
			}

			if (type.Flags.HasFlag(ClassFlags.Struct))
			{
				
				if (newValue.HeapKind == (byte)RtHeapTypeKind.INSTANCE)
				{
					var src = contxt.GC.Heap[newValue.HeapPtr];
					RtInstance srcPayload = (RtInstance)src;


					if (((ASInstance)src.Type).Flags.HasFlag(ClassFlags.Struct))
					{
#if DEBUG
						if (type._link_codescope.Members[memberIndex].__rt_type_class__.Instance != src.Type)
						{
							throw new InvalidOperationException();
						}
#endif
						Debug.Assert(type._link_codescope.TypeLayout.SlotSize[memberIndex] == src.Type._link_codescope.TypeLayout.Size);
						var data = GetStoreData(contxt.player, type).Slice(type._link_codescope.TypeLayout.Offset[memberIndex], type._link_codescope.TypeLayout.SlotSize[memberIndex]);
						var srcdata = srcPayload.GetStoreData(contxt.player, (ASInstance)src.Type).Slice(0, data.Length);
						srcdata.CopyTo(data);

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
				var oldValue = ReadSlot(memberIndex, contxt.player);
				return oldValue.Raw == newValue.Raw; //contxt.player.CopyIfSameTypeStructAndReplaceSrc(oldValue,ref newValue);
			}


		}


		internal void CopyFrom(RtInstance facility, ASInstance type , Player player,int size)
		{
#if DEBUG
			if (HEAPINSTANCE_PTR != 0)
			{
				throw new InvalidOperationException();
			}

			if (facility.HEAPINSTANCE_PTR != 0 && player.Context.GC.Heap[facility.HEAPINSTANCE_PTR] == this)
			{
				throw new InvalidOperationException();
			}

#endif


			bool isref_vector = false;bool isref_struct;

			//if (size > 0)
			//{
			facility.GetStoreData(player, type, out isref_vector,out isref_struct,out RtInstance target).Slice(0, size).CopyTo(store.Span);
			//}

			//if (!isref_vector && !isref_struct && facility.m_property_ptr != int.MinValue)
			if(! type.Flags.HasFlag( ClassFlags.Struct) ) //非结构体才需要复制property
			{
				m_property_ptr = facility.m_property_ptr;
			}
			else
			{
				m_property_ptr = 0; //拷贝值过来，偏移必然为0.
			}


			m__proto__ = facility.m__proto__;
		}

		/// <summary>
		/// 从缓存对象复制值过来
		/// </summary>
		/// <param name="facility"></param>
		/// <exception cref="NotImplementedException"></exception>
		internal void CopyFrom(RtHeapBase src,Player player,int size)
        {
			RtInstance facility = (RtInstance)src;
			CopyFrom(facility, (ASInstance)src.Type, player, size);
		}


		
	}
}
