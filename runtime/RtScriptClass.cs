using juicescript.ABC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.runtime
{
    /// <summary>
    /// 运行时负载Global,Class的实例
    /// </summary>
    public sealed class RtScriptClass : RtHeapBase
    {
		private NaNBoxing[] Slots;

        public ASContainer Meta;

        /// <summary>
        /// 动态属性
        /// </summary>
        public int PROPERTY_PTR;

        /// <summary>
        ///prototype
        /// </summary>
        public int PROTO__PTR;


        public override int Size
        {
            get
            {
                unsafe
                {
                    return 
                        8 + // Meta pointer
                        8 + 
                        8 + Slots.Length * sizeof(NaNBoxing);
                }
            }
        }

        public RtScriptClass(ASClass cls, int _protoPtr):base(RtHeapTypeKind.CLASS)
        {
            Meta = cls;
            Slots = new NaNBoxing[cls._link_codescope.Members.Count];
            PROPERTY_PTR = 0;
            PROTO__PTR = _protoPtr;
            InitSlotDefaultValue();

        }

        public RtScriptClass(ASScript script):base(RtHeapTypeKind.GLOBAL) 
        {
            Meta = script;
            Slots = new NaNBoxing[script._link_codescope.Members.Count];

            InitSlotDefaultValue();
        }

        private void InitSlotDefaultValue()
        {
#if FORCOMPILER
           
            hasSetData = new bool[Slots.Length];
            
#endif
			var codescope = Meta._link_codescope;
            for (int i = 0; i < Slots.Length; i++)
            {
                var member = codescope.Members[i];

                if ((member.Kind == ScopeMemberKind.Constant) && member.trait.Value != null && member.trait.Value.initValue.HasValue)
                {
                    Slots[i] = member.trait.Value.initValue.Value;

#if FORCOMPILER
                    
                    hasSetData[i] = true;
                    
#endif

				}
				else
                {
                    switch (member.TypeKind)
                    {
                        case ABC.TypeKind.Any:
                            Slots[i].SetUndefined();
                            break;
                        case ABC.TypeKind.Boolean:
                            Slots[i].SetBoolean(false);
                            break;
                        case ABC.TypeKind.SByte:
                            Slots[i].SetSByte(0);
                            break;
                        case ABC.TypeKind.Byte:
                            Slots[i].SetByte(0);
                            break;
                        case ABC.TypeKind.Short:
                            Slots[i].SetShort(0);
                            break;
                        case ABC.TypeKind.UShort:
                            Slots[i].SetUShort(0);
                            break;
                        case ABC.TypeKind.Int:
                            Slots[i].SetInt(0);
                            break;
                        case ABC.TypeKind.Uint:
                            Slots[i].SetUInt(0);
                            break;
                        case ABC.TypeKind.Float:
                            Slots[i].SetFloat(float.NaN);
                            break;
                        case ABC.TypeKind.Number:
                            Slots[i].SetNumber(double.NaN);
                            break;
                        default:
                            Slots[i].SetNull();
                            break;
                    }

                }
            }

        }

        /// <summary>
        /// 如果原槽里就是一个struct,而要保存一个新的struct,两边类型完全一致，则不需要alloc新内存，直接覆盖。
        /// </summary>
        /// <param name="contxt"></param>
        /// <param name="index"></param>
        /// <param name="newValue"></param>
        /// <returns></returns>
        internal bool IsUpdateStructOrEqual(Context contxt,int index,NaNBoxing newValue)
        {
#if FORCOMPILER
			if (isCompiling)
			{
                return false;
			}
#endif

			var oldValue = Slots[index];

            if (oldValue.Raw == newValue.Raw)
            {
                return true;
            }

            return contxt.player.CopyIfSameTypeStructAndReplaceSrc(oldValue,ref newValue);

        }



        /// <summary>
        /// 向堆中存储值
        /// </summary>
        /// <param name="value"></param>
        /// <param name="index"></param>
        public void SetSlot(NaNBoxing value,int index)
        {
            Slots[index] = value;

#if FORCOMPILER
            if (isCompiling)
            { 
                hasSetData[index] = true;
            }
#endif

		}


#if FORCOMPILER
        internal bool isCompiling;

        bool[] hasSetData;
        //internal ScopeMember computing_member; //当前正在编译期计算的member,在Script中的function内定义的const,读取global的const时，如果代码位置在前面则读不出。。

#endif

        /// <summary>
        /// 从堆中读取值
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        [MethodImpl( MethodImplOptions.AggressiveInlining)]
        public NaNBoxing ReadSlot(int index)
        {
#if FORCOMPILER
            if (isCompiling)
            {
				if (Meta._link_codescope.Members[index].Kind != ScopeMemberKind.Constant || !hasSetData[index])
                {
                    throw new EvalConstException();
                }

			}
#endif

            return Slots[index];
        }


        internal NaNBoxing[] __get_slots_for_gc
        {
            get
            {
                return Slots;
            }
        }


        public override string ToString()
        {
            return Meta.QName.ToString();
        }

    }
}
