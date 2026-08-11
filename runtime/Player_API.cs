using juicescript.ABC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.runtime
{
	public partial class  Player
	{

		public NaNBoxing InvokeStaticMethod(ASMethod method)
		{
			unsafe
			{
				ReceiveError error = default;
				NaNBoxing _this = default; _this.SetNull();
				NaNBoxing r = RunMethod(
					method, _this, ((ASClass)method.Container).__instance_index__ ,  0, null, new Span<NaNBoxing>(), ref error, Context.StackPosition);

				if (error.raised)
				{
					var ex = new PlayerException(this,error.error, Context.errorStack.ToString());

					throw ex;
				}

				

				return r;

			}
		}

		public int GetVectorLen(NaNBoxing vector)
		{
			RtVector rtVector = (RtVector)Context.GC.Heap[vector.HeapPtr];
			return rtVector.GetStore(this).length;
		}

		public NaNBoxing GetVectorElement(NaNBoxing vector, int index)
		{
			RtVector rtVector = (RtVector)Context.GC.Heap[vector.HeapPtr];
			var store = rtVector.GetStore(this);

			return store.ReadSlot(rtVector.element_type, index, this, vector.HeapPtr, Context.StackPosition, rtVector.element_asclass);

		}

	}
}
