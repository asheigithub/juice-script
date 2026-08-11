using juicescript.ABC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.runtime
{
    public sealed class RtClosure : RtHeapBase
    {
		public RtClosure() : base(RtHeapTypeKind.CLOSURE) { }

        public override int Size => 4 + 4 + 4 + 8 + 4 + 4 + 4+ 1;

		public int ScopePtr;

		///// <summary>
		///// 作为method时，需在加载时确定是否继承
		///// </summary>
		//public ASContainer ScopeType;

        /// <summary>
        /// 被哪个类型查找到的
        /// </summary>
        public ASContainer _ref_as_type;

        public NaNBoxing This;

		//当在保存到堆中时，由于它本身还可能被methodscope中的变量引用，当已分配内存时，就把分配的内存记录在此，防止重复分配。
		internal int cloneing_ptr;



		private int m_property_ptr;


		private int m__proto__;


		//追踪动态属性和prototype.
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




		internal static int FindAndUpdateHeapInstancePtr(int ptr, Player player, out RtClosure target)
		{
			var payload = ((RtClosure)player.Context.GC.Heap[ptr]);
			var origin = payload;
			target = origin;
			while (payload.HEAPINSTANCE_PTR != 0)
			{
				ptr = payload.HEAPINSTANCE_PTR;
				payload = ((RtClosure)player.Context.GC.Heap[ptr]);
				target = payload;

				origin.HEAPINSTANCE_PTR = ptr;//更新,避免后续跳转
			}
			return ptr;
		}

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
				RtClosure target;
				HEAPINSTANCE_PTR = FindAndUpdateHeapInstancePtr(HEAPINSTANCE_PTR, player, out target);
				return target.m_property_ptr;
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
				RtClosure target;
				HEAPINSTANCE_PTR = FindAndUpdateHeapInstancePtr(HEAPINSTANCE_PTR, player, out target);
				target.m_property_ptr = ptr;
			}
		}


		public int PROTOTYPE(Player player)
		{
			if (HEAPINSTANCE_PTR == 0)
			{
				return m__proto__;
			}
			else
			{
				RtClosure target;
				HEAPINSTANCE_PTR = FindAndUpdateHeapInstancePtr(HEAPINSTANCE_PTR, player, out target);
				return target.m__proto__;
			}
		}

		public void Set_PROTOTYPE(int proto_ptr, Player player)
		{
			if (HEAPINSTANCE_PTR == 0)
			{
				m__proto__ = proto_ptr;
			}
			else
			{
				RtClosure target;
				HEAPINSTANCE_PTR = FindAndUpdateHeapInstancePtr(HEAPINSTANCE_PTR, player, out target);
				target.m__proto__ = proto_ptr;
			}
		}



		public void CopyDataFrom(RtClosure facility , Player player )
		{
			_ref_as_type = facility._ref_as_type;
			//ScopeType = facility.ScopeType;
			ScopePtr = facility.ScopePtr;
			This = facility.This;

			m_property_ptr = facility.m_property_ptr; //facility.m_property_ptr;

			m__proto__ = facility.m__proto__;
		}


	}
}
