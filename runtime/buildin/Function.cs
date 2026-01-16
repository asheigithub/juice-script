using juicescript.ABC;
using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static juicescript.runtime.Player;

namespace juicescript.runtime.buildin
{
	internal class Function
	{
		[NativeFunction(".Function$public::Function")]
		public static void Constructor(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{ 
			//nothing to do
		}


		
		[NativeFunction(".Function$@::toString")]
		public static void ToString(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			NaNBoxing v = default;
			context.player.ConvertValueType(ref error, thisPtr, TypeKind.Function, context.FUNCTION, ref v);
			if (error.raised)
			{
				return;
			}

			context.StackSlots[returnSlotIndex].SetHeapPtr(context.player.FUNCTION_TOSTRING_STR);

		}



		[NativeFunction(".Function$:AS3::call")]
		public static void Call(Context context,
			ASMethod method,
			int scope_ptr, 
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtPayloadMethodScope)context.GC.Heap[scope_ptr].facility;

			ref var funslot = ref context.StackSlots[returnSlotIndex];
			context.player.ConvertValueType(ref error, thisPtr, TypeKind.Function, context.FUNCTION, ref funslot);
			if (error.raised)
			{
				return;
			}
			if (funslot.ValueType == NaNBoxing.BoxType.Null)
			{
				context.player.RaiseTypeError_AccessNull(ref error);
				return;
			}

			var closureinstance = context.GC.Heap[funslot.HeapPtr];


			var _this = scope.ReadSlot(0,context.player);
			var rest = scope.ReadSlot(1,context.player);

			var rest_array = (RtPayloadArray)context.GC.Heap[rest.HeapPtr].facility;

			if (rest_array.StoreMode != RtPayloadArray.ArrayStoreMode.cache_on_stack)
				throw new InvalidOperationException();

			var arguments = rest_array.stack_store.Span;

			if (stackStPos + arguments.Length >= Context.STACK_LENGTH)
			{
				context.player.RaiseStackOverflow(ref error);
				return;
			}

			
			var callmethod = ((ASMethodBody)closureinstance.Type).Method;
			if (callmethod.__ismethod && !callmethod.__is_call_or_apply)
			{
				_this = ((RtPayloadClosure)closureinstance.facility).This;
			}
			else if (callmethod.__is_hasOwnProperty)
			{ 
				//特殊方法，不去自动填充
			}
			else if (_this.ValueType == NaNBoxing.BoxType.Undefined || _this.ValueType == NaNBoxing.BoxType.Null)
			{

				var sss = closureinstance.Type._link_codescope.Parent; //Context.GC.Heap[scope_ptr].Type._link_codescope.Parent;
				while (sss.Kind != CodeScopeKind.Script)
				{
					sss = sss.Parent;
				}

				var globalptr = ((ASScript)sss.Container).__global_index__;
				_this.SetHeapPtr(globalptr);

			}

			unsafe
			{
				StackLocater* args = stackalloc StackLocater[arguments.Length];		
				for (int i = 0; i < arguments.Length; i++)
				{
					context.StackSlots[context.StackPosition + i] = arguments[i];
					(args + i)->index = i;
				}

				var slots = context.StackSlots.AsSpan(context.StackPosition, arguments.Length);
				
				context.StackPosition += arguments.Length;

				context.player.RunMethod(callmethod, _this,
					((RtPayloadClosure)closureinstance.facility).ScopePtr,
					((RtPayloadClosure)closureinstance.facility).ScopeType,
					(ushort)rest_array.stack_store.Length , (byte*)args,
					slots,
					ref error,
					returnSlotIndex,
					thisPtr.HeapPtr
					);

				context.StackPosition -= arguments.Length;

			}


		}


		[NativeFunction(".Function$:AS3::apply")]
		public static void Apply(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtPayloadMethodScope)context.GC.Heap[scope_ptr].facility;
			

			ref var funslot = ref context.StackSlots[returnSlotIndex];
			context.player.ConvertValueType(ref error, thisPtr, TypeKind.Function, context.FUNCTION, ref funslot);
			if (error.raised)
			{
				return;
			}
			if (funslot.ValueType == NaNBoxing.BoxType.Null)
			{
				context.player.RaiseTypeError_AccessNull(ref error);
				return;
			}

			var closureinstance = context.GC.Heap[funslot.HeapPtr];


			var _this = scope.ReadSlot(0, context.player);
			var _arr = scope.ReadSlot(1, context.player);


			var callmethod = ((ASMethodBody)closureinstance.Type).Method;
			if (callmethod.__ismethod && !callmethod.__is_call_or_apply)
			{
				_this = ((RtPayloadClosure)closureinstance.facility).This;
			}
			else if (callmethod.__is_hasOwnProperty)
			{

			}
			else if(_this.ValueType == NaNBoxing.BoxType.Undefined || _this.ValueType == NaNBoxing.BoxType.Null)
			{
				
				var sss = closureinstance.Type._link_codescope.Parent; //Context.GC.Heap[scope_ptr].Type._link_codescope.Parent;
				while (sss.Kind != CodeScopeKind.Script)
				{
					sss = sss.Parent;
				}

				var globalptr = ((ASScript)sss.Container).__global_index__;				
				_this.SetHeapPtr(globalptr);

			}


			int len; RtPayloadArray argArray = null;
			if (_arr.ValueType != NaNBoxing.BoxType.Null)
			{
				argArray = (RtPayloadArray)context.GC.Heap[_arr.HeapPtr].facility;
				len = (int)argArray.GetLength(context.player);
			}
			else
			{
				len = 0;
			}


			if (stackStPos + len >= Context.STACK_LENGTH)
			{
				context.player.RaiseStackOverflow(ref error);
				return;
			}


			unsafe
			{
				StackLocater* args = stackalloc StackLocater[len];
				for (int i = 0; i < len; i++)
				{
					bool isoutofindex;
					context.StackSlots[context.StackPosition + i] = argArray.ReadSlot((uint)i,context.player,out isoutofindex);
					(args + i)->index = i;
				}

				var slots = context.StackSlots.AsSpan(context.StackPosition, len);

				context.StackPosition += len;

				context.player.RunMethod(callmethod, _this,
					((RtPayloadClosure)closureinstance.facility).ScopePtr,
					((RtPayloadClosure)closureinstance.facility).ScopeType,
					(ushort)len, (byte*)args,
					slots,
					ref error,
					returnSlotIndex,
					thisPtr.HeapPtr
					);

				context.StackPosition -= len;

			}


		}



	}
}
