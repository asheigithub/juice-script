using juicescript.ABC;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.runtime
{
    /// <summary>
    /// 负载对象实例ASInstance
    /// </summary>
    public sealed class RtPayloadInstance : FacilityBase
    {
        /// <summary>
        /// 缓存对象的最大成员大小
        /// </summary>
        public const int MAX_CACHEABLE_SIZE = 16 * 8;

		private static int DoFindAndUpdatePtr( int ptr, Player player, ASInstance type , out RtPayloadInstance target)
		{
			
			var ref_instance = player.Context.GC.Heap[ptr];

			
			if (ref_instance.TypeKind == RtHeapTypeKind.VECTOR)
			{
				target = null;
				return 0;
			}
			


			var payload = ((RtPayloadInstance)ref_instance.facility);
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
				if (player.Context.GC.Heap[payload.HEAPINSTANCE_PTR].TypeKind == RtHeapTypeKind.VECTOR)
				{
					throw new InvalidOperationException(); //vector 的struct引用不可能多次跳转
				}
#endif

				ptr = payload.HEAPINSTANCE_PTR;
				payload = ((RtPayloadInstance)player.Context.GC.Heap[ptr].facility);
				target = payload;

				origin.HEAPINSTANCE_PTR = ptr;//更新,避免后续跳转
			}

			return ptr;
		}
		

        internal static int FindAndUpdateHeapInstancePtr(int ptr, Player player,out RtPayloadInstance target)
        {
			RtHeapInstance tmp = player.Context.GC.Heap[ptr];
			RtPayloadInstance check = (RtPayloadInstance)tmp.facility;

			if (((ASInstance)tmp.Type).Flags.HasFlag(ClassFlags.Struct))
			{
				if (check.HEAPINSTANCE_PTR != 0)
				{

					
					var tmp2 = player.Context.GC.Heap[check.HEAPINSTANCE_PTR];

					if (tmp2.TypeKind == RtHeapTypeKind.INSTANCE && tmp2.Type != tmp.Type)
					{
#if DEBUG
						if (((RtPayloadInstance)tmp2.facility).HEAPINSTANCE_PTR != 0)
						{
							throw new InvalidOperationException();
						}
#endif
						target = check;
						return ptr;
					}
					else if (tmp2.TypeKind == RtHeapTypeKind.VECTOR)
					{
#if DEBUG
						if (((RtPayloadVector)tmp2.facility).HEAPINSTANCE_PTR != 0)
						{
							throw new InvalidOperationException();
						}
#endif

						target = check;
						return ptr;
					}
#if DEBUG

					if (tmp2.TypeKind == RtHeapTypeKind.VECTOR)
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

        internal Span<byte> GetStoreData(Player player,ASInstance type)
        {
			bool is_ref_vector;bool is_ref_struct;
			return GetStoreData(player, type, out is_ref_vector,out is_ref_struct);
        }

		private Span<byte> GetStoreData(Player player,ASInstance type , out bool is_ref_vector,out bool is_ref_struct)
		{
			if (HEAPINSTANCE_PTR == 0)
			{
				is_ref_struct = false;
				is_ref_vector = false;
				return store.Span;
			}
			else
			{
				
				RtPayloadInstance target;
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

					RtPayloadVector vector = (RtPayloadVector)player.Context.GC.Heap[target.HEAPINSTANCE_PTR].facility;
					is_ref_vector = true;
					is_ref_struct = false;

					

					return vector.ReadStoreOffset(target.m_property_ptr, player,type._link_codescope.TypeLayout.Size);

				}

			}
		}

		internal bool IsRefVectorOrStruct(Player player,ASInstance type)
		{
			if (!type.Flags.HasFlag(ClassFlags.Struct))
			{
				return false;
			}

			bool is_ref_vector;bool is_ref_struct;
			GetStoreData(player, type ,out is_ref_vector,out is_ref_struct);
			return is_ref_vector || is_ref_struct;
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
            store = new Memory<byte>(new byte[size]);
        }

        private int m_property_ptr;
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
                RtPayloadInstance target;
                HEAPINSTANCE_PTR = DoFindAndUpdatePtr(HEAPINSTANCE_PTR, player, type,out target);
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
				RtPayloadInstance target;
				HEAPINSTANCE_PTR = DoFindAndUpdatePtr(HEAPINSTANCE_PTR,  player, type ,out target);
                target.m_property_ptr = ptr;

				//((RtPayloadInstance)player.Context.GC.Heap[HEAPINSTANCE_PTR].facility).Set_PROPERTY_PTR(ptr, player);
			}
        }

		private int m__proto__;

		public int PROTOTYPE(Player player,ASInstance type)
		{
			if (HEAPINSTANCE_PTR == 0)
			{
				return m__proto__;
			}
			else
			{
				RtPayloadInstance target;
				HEAPINSTANCE_PTR = DoFindAndUpdatePtr(HEAPINSTANCE_PTR, player, type ,out target);
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
		internal int HEAPINSTANCE_PTR;



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




       


		public RtPayloadInstance()
        { 
            
        }
        

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
				unsafe
				{
					fixed (byte* p = store.Span)
					{

						for (int i = 0; i < link_codescope.TypeLayout.Offset.Count; i++)
						{

							byte* ptr = p + link_codescope.TypeLayout.Offset[i];
							var member = link_codescope.Members[i];

							if ((member.Kind == ScopeMemberKind.Constant || member.Kind == ScopeMemberKind.Slot) && member.trait.Value != null && member.trait.Value.initValue.HasValue)
							{
								SetSlot(member.trait.Value.initValue.Value, (ushort)i, link_codescope, player);

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
			}
        }

		private unsafe static void InitAtBuffer(void* span,CodeScope link_codescope)
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
				case TypeKind.Any:
					((NaNBoxing*)ptr)->SetUndefined();
					break;
				case TypeKind.Boolean:
					*(bool*)ptr = false;
					break;
				case TypeKind.SByte:
					*(sbyte*)ptr = 0;
					break;
				case TypeKind.Byte:
					*(byte*)ptr = 0;
					break;
				case TypeKind.Short:
					*(short*)ptr = 0;
					break;
				case TypeKind.UShort:
					*(ushort*)ptr = 0;
					break;
				case TypeKind.Int:
					*(int*)ptr = 0;
					break;
				case TypeKind.Uint:
					*(uint*)ptr = 0;
					break;
				case TypeKind.Float:
					*(float*)ptr = 0;
					break;
				case TypeKind.Number:
					*(double*)ptr = double.NaN;
					break;

				case TypeKind.Fun_Void:
				case TypeKind.Unknown:
#if DEBUG
					throw new InvalidOperationException();
#else
					Environment.FailFast("出错了，这里跑不到"); return;
#endif

				case TypeKind.Null:
				case TypeKind.String:
				case TypeKind.Function:
				case TypeKind.Array:
				case TypeKind.Vector:
				case TypeKind.Namespace:
				case TypeKind.Object:
				case TypeKind.Class:
					((NaNBoxing*)ptr)->SetNull();
					break;
				default:
#if DEBUG
					if ((ulong)member.TypeKind < (ulong)TypeKind.Object)
					{
						throw new InvalidOperationException();
					}

#endif
					if (((ASInstance)member.DefineAt).Flags.HasFlag(ClassFlags.Struct)
						&&
						member.__rt_type_class__.Instance.Flags.HasFlag(ClassFlags.Struct)
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

		public NaNBoxing ReadSlot(ushort memberIndex, CodeScope codescope, Player player , int returnSlotIndex ,int this_instance_Ptr)
		{
			var member = codescope.Members[memberIndex];

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
				fixed (byte* p = GetStoreData(player, codescope.TypeLayout.ASType.Instance ))
				{
					NaNBoxing result = new NaNBoxing();
					byte* ptr = p + codescope.TypeLayout.Offset[memberIndex];

					switch (member.TypeKind)
					{
						case TypeKind.Any:
							result = *(NaNBoxing*)ptr;
							break;
						case TypeKind.Boolean:
							result.SetBoolean(*(bool*)ptr);
							break;
						case TypeKind.SByte:
							result.SetSByte(*(sbyte*)ptr);
							break;
						case TypeKind.Byte:
							result.SetByte(*(byte*)ptr);
							break;
						case TypeKind.Short:
							result.SetShort(*(short*)ptr);
							break;
						case TypeKind.UShort:
							result.SetUShort(*(ushort*)ptr);
							break;
						case TypeKind.Int:
							result.SetInt(*(int*)ptr);
							break;
						case TypeKind.Uint:
							result.SetUInt(*(uint*)ptr);
							break;
						case TypeKind.Float:
							result.SetFloat(*(float*)ptr);
							break;
						case TypeKind.Number:
							result.SetNumber(*(double*)ptr);
							break;
						case TypeKind.Null:
						case TypeKind.String:
						case TypeKind.Function:
						case TypeKind.Array:
						case TypeKind.Vector:
						case TypeKind.Namespace:
						case TypeKind.Object:
						case TypeKind.Class:
							result = *(NaNBoxing*)ptr;
							break;
						case TypeKind.Fun_Void:
						case TypeKind.Unknown:
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

							if (((ASInstance)member.DefineAt).Flags.HasFlag(ClassFlags.Struct) &&
									member.__rt_type_class__.Instance.Flags.HasFlag(ClassFlags.Struct)
								)
							{
								int cache_ptr = player.Context.CacheInstancePtr + returnSlotIndex;
								var cache = player.Context.GC.Heap[cache_ptr];

								cache.Type = member.__rt_type_class__.Instance;
								RtPayloadInstance struct_payload = (RtPayloadInstance)cache.facility;

								struct_payload.methodscopeslot_ref_state = 0;
								struct_payload.m_property_ptr = m_property_ptr + codescope.TypeLayout.Offset[memberIndex]; //标记index.
								struct_payload.HEAPINSTANCE_PTR = HEAPINSTANCE_PTR == 0? this_instance_Ptr : HEAPINSTANCE_PTR ; //指向当前对象.

								result.SetHeapPtr(cache_ptr);
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

		public NaNBoxing ReadSlot(ushort memberIndex, CodeScope codescope,Player player)
        {
			return ReadSlot(memberIndex, codescope, player, -1,0);
        }

        internal void SetSlot(NaNBoxing value, ushort memberIndex, CodeScope codeScope,Player player)
        {
#if FORCOMPILER
            if (isCompiling)
            {
                hasSetData[memberIndex] = true;
            }
#endif

            var member = codeScope.Members[memberIndex];
            unsafe
            {
                fixed (byte* p = GetStoreData(player,codeScope.TypeLayout.ASType.Instance))
                { 
                    byte* ptr = p + codeScope.TypeLayout.Offset[memberIndex];
					SetSlotDataByValue(member,ptr, value);
                }
            }

        }

        internal unsafe static void SetSlotDataByValue( ScopeMember member,void* ptr, NaNBoxing value)
        {

			switch (member.TypeKind)
			{
				case TypeKind.Any:
					*(NaNBoxing*)ptr = value;
					break;
				case TypeKind.Boolean:
#if DEBUG
					if (value.ValueType != NaNBoxing.BoxType.Boolean)
						throw new InvalidOperationException();
#endif
					*(bool*)ptr = value.Boolean;
					break;

				case TypeKind.SByte:
#if DEBUG
					if (value.ValueType != NaNBoxing.BoxType.Sbyte)
						throw new InvalidOperationException();
#endif
					*(sbyte*)ptr = value.SByteValue;
					break;

				case TypeKind.Byte:
#if DEBUG
					if (value.ValueType != NaNBoxing.BoxType.Byte)
						throw new InvalidOperationException();
#endif
					*(byte*)ptr = value.ByteValue;
					break;
				case TypeKind.Short:
#if DEBUG
					if (value.ValueType != NaNBoxing.BoxType.Short)
						throw new InvalidOperationException();
#endif
					*(short*)ptr = value.ShortValue;
					break;
				case TypeKind.UShort:
#if DEBUG
					if (value.ValueType != NaNBoxing.BoxType.UShort)
						throw new InvalidOperationException();
#endif
					*(ushort*)ptr = value.UShortValue;
					break;

				case TypeKind.Int:
#if DEBUG
					if (value.ValueType != NaNBoxing.BoxType.Int)
						throw new InvalidOperationException();
#endif
					*(int*)ptr = value.IntValue;
					break;
				case TypeKind.Uint:
#if DEBUG
					if (value.ValueType != NaNBoxing.BoxType.Uint)
						throw new InvalidOperationException();
#endif
					*(uint*)ptr = value.UIntValue;
					break;

				case TypeKind.Float:
#if DEBUG
					if (value.ValueType != NaNBoxing.BoxType.Float)
						throw new InvalidOperationException();
#endif
					*(float*)ptr = value.FloatValue;
					break;
				case TypeKind.Number:
#if DEBUG
					if (value.ValueType != NaNBoxing.BoxType.Number)
						throw new InvalidOperationException();
#endif
					*(double*)ptr = value.Number;
					break;

				case TypeKind.Fun_Void:
				case TypeKind.Unknown:
#if DEBUG
					throw new InvalidOperationException();
#else
					Environment.FailFast("出错了，这里跑不到"); return;
#endif
				case TypeKind.Null:
				case TypeKind.String:
				case TypeKind.Function:
				case TypeKind.Array:
				case TypeKind.Vector:
				case TypeKind.Namespace:
				case TypeKind.Object:
				case TypeKind.Class:
					*(NaNBoxing*)ptr = value;
					break;

				default:
#if DEBUG
					if (value.ValueType != NaNBoxing.BoxType.HeapPtr && value.ValueType != NaNBoxing.BoxType.Null)
					{
						throw new InvalidOperationException();
					}

					if (((ASInstance)member.DefineAt).Flags.HasFlag(ClassFlags.Struct) &&
						member.__rt_type_class__.Instance.Flags.HasFlag(ClassFlags.Struct)
						) //断言，禁止对结构体的嵌套结构体走这里赋值
					{
						throw new InvalidOperationException();
					}
#endif

					*(NaNBoxing*)ptr = value;
					break;

			}

		}

		internal bool IsUpdateStructOrEqual(Context contxt, ushort memberIndex, NaNBoxing newValue, ASInstance type)
        {
            

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
					type._link_codescope.Members[memberIndex].__rt_type_class__.Instance.Flags.HasFlag(ClassFlags.Struct)
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
				var src = contxt.GC.Heap[newValue.HeapPtr];
				if (src.TypeKind == RtHeapTypeKind.INSTANCE)
				{
					RtPayloadInstance srcPayload = (RtPayloadInstance)src.facility;


					if (((ASInstance)src.Type).Flags.HasFlag(ClassFlags.Struct))
					{
#if DEBUG
						if (type._link_codescope.Members[memberIndex].__rt_type_class__.Instance != src.Type)
						{
							throw new InvalidOperationException();
						}
#endif

						var data = GetStoreData(contxt.player, type).Slice(type._link_codescope.TypeLayout.Offset[memberIndex], type._link_codescope.TypeLayout.SlotSize[memberIndex]);
						var srcdata = srcPayload.GetStoreData(contxt.player, (ASInstance)src.Type).Slice(0, src.Type._link_codescope.TypeLayout.Size);
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
				var oldValue = ReadSlot(memberIndex, type._link_codescope, contxt.player);
				return contxt.player.CopyIfSameTypeStructAndReplaceSrc(oldValue,ref newValue);
			}


		}


		internal void CopyFrom(RtPayloadInstance facility, ASInstance type , Player player,int size)
		{
#if DEBUG
			if (HEAPINSTANCE_PTR != 0)
			{
				throw new InvalidOperationException();
			}

			if (facility.HEAPINSTANCE_PTR != 0 && player.Context.GC.Heap[facility.HEAPINSTANCE_PTR].facility == this)
			{
				throw new InvalidOperationException();
			}

#endif


			bool isref_vector = false;bool isref_struct;

			//if (size > 0)
			//{
			facility.GetStoreData(player, type, out isref_vector,out isref_struct).Slice(0, size).CopyTo(store.Span);
			//}

			if (!isref_vector && !isref_struct)
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
		internal void CopyFrom(RtHeapInstance src,Player player,int size)
        {
			RtPayloadInstance facility = (RtPayloadInstance)src.facility;
			CopyFrom(facility, (ASInstance)src.Type, player, size);
		}


		
	}
}
