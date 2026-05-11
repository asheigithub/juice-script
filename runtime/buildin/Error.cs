using juicescript.ABC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static juicescript.runtime.Player;

namespace juicescript.runtime.buildin
{
	internal class Error
	{
		[NativeFunction(".Error$public::Error")]
		public static void Error_Error(Context context,
			ASMethod method,
			int scope_ptr, 
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
#if DEBUG
			if (thisPtr.ValueType != NaNBoxing.BoxType.HeapPtr)
				throw new InvalidOperationException();
#endif

			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			var err = (RtInstance)context.GC.Heap[thisPtr.HeapPtr];

			err.SetSlot(scope.ReadSlot(0,context.player), 0, context.ERROR.Instance._link_codescope, context.player);
			err.SetSlot(scope.ReadSlot(1,context.player), 2, context.ERROR.Instance._link_codescope, context.player);

			NaNBoxing name = default; name.SetHeapPtr(context.player.cache_ERROR_NAME, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
			err.SetSlot(name, 1, context.ERROR.Instance._link_codescope, context.player);
		}

		[NativeFunction(".TypeError$public::TypeError")]
		public static void TypeError_Error(Context context,
			ASMethod method,
			int scope_ptr, 
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
#if DEBUG
			if (thisPtr.ValueType != NaNBoxing.BoxType.HeapPtr)
				throw new InvalidOperationException();
#endif

			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			var err = (RtInstance)context.GC.Heap[thisPtr.HeapPtr];

			err.SetSlot(scope.ReadSlot(0,context.player), 0, context.TYPE_ERROR.Instance._link_codescope, context.player);
			err.SetSlot(scope.ReadSlot(1,context.player), 2, context.TYPE_ERROR.Instance._link_codescope, context.player);

			NaNBoxing name=default;name.SetHeapPtr(context.player.cache_TYPE_ERROR_NAME, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE	);
			err.SetSlot( name , 1, context.TYPE_ERROR.Instance._link_codescope, context.player);

		}
		//.EvalError$public::EvalError 
		[NativeFunction(".EvalError$public::EvalError")]
		public static void EvalError_Error(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
#if DEBUG
			if (thisPtr.ValueType != NaNBoxing.BoxType.HeapPtr)
				throw new InvalidOperationException();
#endif

			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			var err = (RtInstance)context.GC.Heap[thisPtr.HeapPtr];

			err.SetSlot(scope.ReadSlot(0, context.player), 0, context.ERROR.Instance._link_codescope, context.player);
			err.SetSlot(scope.ReadSlot(1, context.player), 2, context.ERROR.Instance._link_codescope, context.player);
	
			NaNBoxing name = default; name.SetHeapPtr(context.player.cache_Eval_ERROR_NAME, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
			err.SetSlot(name, 1, context.ERROR.Instance._link_codescope, context.player);

		}

		[NativeFunction(".RangeError$public::RangeError")]
		public static void RangeError_Error(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
#if DEBUG
			if (thisPtr.ValueType != NaNBoxing.BoxType.HeapPtr)
				throw new InvalidOperationException();
#endif

			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			var err = (RtInstance)context.GC.Heap[thisPtr.HeapPtr];

			err.SetSlot(scope.ReadSlot(0, context.player), 0, context.ERROR.Instance._link_codescope, context.player);
			err.SetSlot(scope.ReadSlot(1, context.player), 2, context.ERROR.Instance._link_codescope, context.player);

			NaNBoxing name = default; name.SetHeapPtr(context.player.cache_RANGE_ERROR_NAME, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
			err.SetSlot(name, 1, context.ERROR.Instance._link_codescope, context.player);

		}

		[NativeFunction(".ReferenceError$public::ReferenceError")]
		public static void ReferenceError_Error(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
#if DEBUG
			if (thisPtr.ValueType != NaNBoxing.BoxType.HeapPtr)
				throw new InvalidOperationException();
#endif

			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			var err = (RtInstance)context.GC.Heap[thisPtr.HeapPtr];

			err.SetSlot(scope.ReadSlot(0, context.player), 0, context.ERROR.Instance._link_codescope, context.player);
			err.SetSlot(scope.ReadSlot(1, context.player), 2, context.ERROR.Instance._link_codescope, context.player);

			NaNBoxing name = default; name.SetHeapPtr(context.player.cache_REFERENCE_ERROR_NAME, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
			err.SetSlot(name, 1, context.ERROR.Instance._link_codescope, context.player);

		}



		[NativeFunction(".URIError$public::URIError")]
		public static void URIError_Error(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
#if DEBUG
			if (thisPtr.ValueType != NaNBoxing.BoxType.HeapPtr)
				throw new InvalidOperationException();
#endif

			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			var err = (RtInstance)context.GC.Heap[thisPtr.HeapPtr];

			err.SetSlot(scope.ReadSlot(0, context.player), 0, context.ERROR.Instance._link_codescope, context.player);

			var scriptid = ((ASScript)context.GC.Heap[thisPtr.HeapPtr].Type._link_codescope.Parent.Container).__global_index__	;
			NaNBoxing name = ((RtScriptClass)context.GC.Heap[scriptid]).ReadSlot(0);

			Debug.Assert(name.ValueType == NaNBoxing.BoxType.HeapPtr);
			Debug.Assert(context.GC.Heap[name.HeapPtr].Kind == RtHeapTypeKind.STRING );

			err.SetSlot(name, 1, context.ERROR.Instance._link_codescope, context.player);

		}













		[NativeFunction(".Error$public::get#errorID")]
		public static void Error_errorId(Context context,
			ASMethod method,
			int scope_ptr, 
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
#if DEBUG
			if (thisPtr.ValueType != NaNBoxing.BoxType.HeapPtr)
				throw new InvalidOperationException();
#endif
			var err = (RtInstance)context.GC.Heap[thisPtr.HeapPtr];

			NaNBoxing id = err.ReadSlot(2, context.ERROR.Instance._link_codescope, context.player);

			context.StackSlots[returnSlotIndex] = id;
		}
	}
}
