using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC
{
    public sealed class ASMethodBody : ASContainer
    {
        /// <summary>
        /// 常量池中的堆指针的类型
        /// </summary>
        public enum PoolHeapPtrKind :byte
        { 
            UnKnown = 0,
            String = 1,
            LD_Class = 2,
            Namespace =3,
            VectorDef =4,
            Method =5,
            SuperMethod=6
        }



        public override ASMultiname QName
        {
            get
            {
                if (Method.Trait != null)
                    return Method.Trait.QName;
                else
                    return null;
            }
        }

        public ASMethod Method { get; private set; }

        public int NamespaceSetIndex { get;  set; }


        public byte[] ByteCode { get; set; }

		public byte[] param_defaultvalues;

        /// <summary>
        /// 当所在Script初始化后，赋值给内部method,这样就不用查找了!
        /// </summary>
        public int rt__globalindex;



        /// <summary>
        /// 保存method级的堆常量（比如字符串等）
        /// </summary>
        public class MethodHeapConstants
        {
            public ASMethodBody.PoolHeapPtrKind[] pool_kinds = new PoolHeapPtrKind[0] ;

            public object[] pool_values = new object[0] ;

           
            //public NaNBoxing[] constants = new NaNBoxing[0];

        }

        public MethodHeapConstants heapConstants;


        [StructLayout(LayoutKind.Explicit)]
		public struct MethodBodyInfo
        {
            [FieldOffset(0)]
            public int useSlots;
            [FieldOffset(4)]
            public int constants;
            [FieldOffset(8)]
            public int instructions;
        }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void GetInfo(ref MethodBodyInfo info)
        {
            GetInfo(ref info, ByteCode);
        }
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe void GetInfo(ref MethodBodyInfo info, byte[] ByteCode)
        {
			fixed (void* p = ByteCode)
			{
                //info.useSlots = *((int*)p);
                //info.constants = *((int*)p + 1);
                //info.instructions = *((int*)p + 2);

                info = *((MethodBodyInfo*)p);                
			}
		}

        public static unsafe List<NaNBoxing> ReadConstants( byte[] ByteCode)
        {
            fixed (void* p = ByteCode)
            {
				 int constantscount = *((int*)p + 1);
                int ins_count = *((int*)p + 2);

                List<NaNBoxing> contants = new List< NaNBoxing>();

				NaNBoxing* src_consts = (NaNBoxing*)((int*)p + 3 + 2 * ins_count);

                for (int i = 0; i < constantscount; i++)
                {
                    contants.Add(*src_consts);

                    src_consts++;
                }



				return contants;
			}

        }

		public static unsafe bool CheckConstants(byte[] computeDefaultValue, List<NaNBoxing> compute_constants)
		{
            bool flag = true;

			fixed (void* p = computeDefaultValue)
			{
				int constantscount = *((int*)p + 1);
				int ins_count = *((int*)p + 2);

                if (constantscount != compute_constants.Count)
                    throw new InvalidOperationException();


				NaNBoxing* src_consts = (NaNBoxing*)((int*)p + 3 + 2 * ins_count);

				for (int i = 0; i < constantscount; i++)
				{
                    if ((*(src_consts + i)).Raw != compute_constants[i].Raw)
                    {
                        (*(src_consts + i)) = compute_constants[i];
                        flag = false;
					}
                    //throw new InvalidOperationException();

                    //(*src_consts+i) = compute_constants[i];

				}


                return flag;
			}
		}

		public ASMethodBody(ASMethod method)
        {
            Method = method;
            NamespaceSetIndex = 0;
            heapConstants = new MethodHeapConstants();

        }

    }
}
