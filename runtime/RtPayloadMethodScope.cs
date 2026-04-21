using juicescript.ABC;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.runtime
{
	public sealed class RtPayloadMethodScope : FacilityBase
	{

		private Memory<NaNBoxing> Slots;

		public int ParentPtr;

		public override int Size
		{
			get
			{
				int size = 16 + 4;

				size += Slots.Length * 8;

				return size;
			}
		}

		
		internal bool IsStackSlot;
		internal int StackPos;
		internal int SlotCount;

		/// <summary>
		/// 在RunMethod 构造时，记录实际上代码里写了几个参数。
		/// </summary>
		internal int __sendargcount;

		public NaNBoxing ThisPtr
		{
			get {
				return Slots.Span[SlotCount - 1];
			}
		}


		/// <summary>
		/// 实际上，只有RunMethod里的调用才会是在栈帧上分配
		/// </summary>
		/// <param name="array"></param>
		/// <param name="start"></param>
		/// <param name="codescope"></param>
		/// <param name="isStackSlot"></param>
		public void InitSlot(NaNBoxing[] array, int start, CodeScope codescope,bool isStackSlot)
		{
			IsStackSlot = isStackSlot;
			StackPos = start;
			SlotCount = codescope.Members.Count
				+ 1 //追加一个槽保存this
				;

			Slots = new Memory<NaNBoxing>(array, start, codescope.Members.Count + 1);

			Slots.Span[codescope.Members.Count].SetUndefined(); //此处槽保留this

			cloneout_ptr = 0;
#if FORCOMPILER
			if (isCompiling)
			{
				hasSetData = new bool[Slots.Length];
				scope = codescope;
			}
#endif


			var span = Slots.Span;
			for (int i = codescope.ParameterCout; i < span.Length - 1; i++)
			{
				var member = codescope.Members[i];

				if (
					//((ASMethodBody)codescope.Container).Method.__ismethod &&
					(member.Kind == ScopeMemberKind.Constant) && member.trait.Value != null && member.trait.Value.initValue.HasValue)
				{
					span[i] = member.trait.Value.initValue.Value;
#if FORCOMPILER
					if (isCompiling)
					{
						hasSetData[i] = true;
					}
#endif
				}
				else
				{
#if FORCOMPILER
					if (isCompiling)
					{
						if (member.Kind != ScopeMemberKind.Parameter && member.trait.QName.Name.StartsWith("%"))
						{
							span[i].setFault();
							return;
						}
					}
					
#endif


					switch (member.TypeKind)
					{
						case TypeKind.Any:
							span[i].SetUndefined();
							break;
						case TypeKind.Boolean:
							span[i].SetBoolean(false);
							break;
						case TypeKind.SByte:
							span[i].SetSByte(0);
							break;
						case TypeKind.Byte:
							span[i].SetByte(0);
							break;
						case TypeKind.Short:
							span[i].SetShort(0);
							break;
						case TypeKind.UShort:
							span[i].SetUShort(0);
							break;
						case TypeKind.Int:
							span[i].SetInt(0);
							break;
						case TypeKind.Uint:
							span[i].SetUInt(0);
							break;
						case TypeKind.Float:
							span[i].SetFloat(float.NaN);
							break;
						case TypeKind.Number:
							span[i].SetNumber(double.NaN);
							break;
						default:
							span[i].SetNull();
							break;
					}
				}

			}

		}

#if FORCOMPILER
		internal bool isCompiling;
		bool[] hasSetData;
		CodeScope scope;
#endif

		/*
		 function makeCounter() {
				var count = 0;
	
				trace(count);
	
				return {
					inc: function() { count++; },
					get_: function() { return count; }
				};
			}

			var c = makeCounter();

			c.inc();
			c.inc();

			trace( c.get_() );
		*/
		/// <summary>
		/// 用于复制到堆时，避免重复处理。 注意它可能被多个下级函数引用，所以不要在clone完成后立即置0！仅在Init时置0
		/// </summary>
		internal int cloneout_ptr;


		internal void SetSlot(NaNBoxing value, ushort memberIndex)
		{
			Slots.Span[memberIndex] = value;

#if FORCOMPILER
			if (isCompiling)
			{
				hasSetData[memberIndex] = true;
			}
#endif

		}
		[MethodImpl( MethodImplOptions.AggressiveInlining)]
		internal NaNBoxing ReadSlot(ushort memberIndex
//#if FORCOMPILER
			, Player player
//#endif
			)
		{
#if FORCOMPILER
			if (isCompiling)
			{
				
				if ( memberIndex!= SlotCount-1 && (scope.Members[memberIndex].Kind != ScopeMemberKind.Constant || !hasSetData[memberIndex]))
				{
					throw new EvalConstException();
				}
			}

#endif

			return Slots.Span[memberIndex];
		}

		internal Span<NaNBoxing> __get_slots_for_gc
		{
			get
			{
				return Slots.Span;
			}
		}

		/// <summary>
		/// 在直接调用player.Execute时，没有用RunMethod生成methodscope。需要用这个把当前BackTraceIndex 指向的缓存MethocScope的槽清理掉
		/// 否则GC会去查找错误的引用。
		/// </summary>
		internal void EmptyStackSlot()
		{
			Slots = new Memory<NaNBoxing>();
		}


		/// <summary>
		/// 当加载到堆后，把槽位更新到使用new出来的堆对象的槽位
		/// </summary>
		/// <param name="newHeapScope"></param>
		internal void ChangeStore(RtPayloadMethodScope newHeapScope)
		{
			IsStackSlot = newHeapScope.IsStackSlot;
			StackPos = newHeapScope.StackPos;
			SlotCount = newHeapScope.SlotCount;
			Slots = newHeapScope.Slots;

		}

	}
}
