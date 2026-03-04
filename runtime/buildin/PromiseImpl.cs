using juicescript.ABC;
using juicescript.ABC.Locaters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using static juicescript.runtime.Player;

namespace juicescript.runtime.buildin
{
	internal class PromiseImpl
	{

		[NativeFunction(".Promise$public::Promise")]
		public static void Promise_Constructor(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			Debug.Assert(thisPtr.ValueType == NaNBoxing.BoxType.HeapPtr);

			RtHeapInstance _this = context.GC.Heap[ thisPtr.HeapPtr];
			Debug.Assert(_this.TypeKind == RtHeapTypeKind.INSTANCE);
			Debug.Assert(_this.Type.QName.Name == "Promise");

			var executor = ((RtPayloadMethodScope)context.GC.Heap[scope_ptr].facility).ReadSlot(0, context.player);

			RtHeapInstance executor_closure;
			// 2. 验证executor是否为函数
			if (!IsCallable(executor,context,out executor_closure))
			{
				context.player.RaiseTypeError(
					ref error,executor, TypeKind.Function);
				return;
			}

			if (stackStPos + 2 >= Context.STACK_LENGTH)
			{
				context.player.RaiseStackOverflow(ref error);
				return;
			}

			PromiseWapper wapper = new PromiseWapper();

			((RtPayloadInstance)_this.facility).wapperedObject = wapper;


			// 准备参数 _resolve
			{
				var _resolve = _this.Type._vtable.Items[2];
				int ptrIndex = stackStPos;
				int m_closurePtr = context.M_ClosurePtr + ptrIndex;

				context.GC.Heap[m_closurePtr].Type = _resolve.Trait.Method.Body;
				RtPayloadClosure closure = (RtPayloadClosure)context.GC.Heap[m_closurePtr].facility;
				closure.This = thisPtr;
				closure.ScopePtr = scope_ptr;
				closure.ScopeType = _resolve.DefineAt;
				closure._ref_as_type = _resolve.DefineAt;
				closure.methodscopeslot_ref_state = 0; closure.HEAPINSTANCE_PTR = 0;

				context.StackSlots[ptrIndex].SetHeapPtr(m_closurePtr);
			}
			// 准备参数 _reject
			{
				var _reject = _this.Type._vtable.Items[3];
				int ptrIndex = stackStPos+1;
				int m_closurePtr = context.M_ClosurePtr + ptrIndex;

				context.GC.Heap[m_closurePtr].Type = _reject.Trait.Method.Body;
				RtPayloadClosure closure = (RtPayloadClosure)context.GC.Heap[m_closurePtr].facility;
				closure.This = thisPtr;
				closure.ScopePtr = scope_ptr;
				closure.ScopeType = _reject.DefineAt;
				closure._ref_as_type = _reject.DefineAt;
				closure.methodscopeslot_ref_state = 0; closure.HEAPINSTANCE_PTR = 0;

				context.StackSlots[ptrIndex].SetHeapPtr(m_closurePtr);
			}

			var executor_method = ((ASMethodBody)executor_closure.Type).Method;


			unsafe
			{
				StackLocater* args = stackalloc StackLocater[2];
				for (int i = 0; i < 2; i++)
				{
					(args + i)->index = i;
				}

				var slots = context.StackSlots.AsSpan(context.StackPosition, 2);

				context.StackPosition += 2;

				context.player.RunMethod(executor_method, ((RtPayloadClosure)executor_closure.facility).This,
					((RtPayloadClosure)executor_closure.facility).ScopePtr,
					((RtPayloadClosure)executor_closure.facility).ScopeType,
					2, (byte*)args,
					slots,
					ref error,
					returnSlotIndex,
					thisPtr.HeapPtr
					);

				context.StackPosition -= 2;

				if (error.raised)
				{
					wapper._state = PromiseState.rejected;

					if (error.error.ValueType == NaNBoxing.BoxType.Fault)
					{
						return;
					}

					//吃掉异常，传给_reject
					NaNBoxing e = error.error;
					error.error.SetUndefined();
					error.raised = false;

					context.errorStack.Clear();
					
					NaNBoxing reason = context.player.GetSaveValue(e, ref error);
					if (error.raised)
					{
						error.error.setFault();// 这里无法恢复
						return;
					}

					wapper._reason = reason;

				}
				else
				{ 
					
				}
			}


		}

		[NativeFunction(".Promise$private::_resolve")]
		public static void Promise_resolve(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			// 1. 获取resolve的参数值
			var scope = (RtPayloadMethodScope)context.GC.Heap[scope_ptr].facility;
			var value = scope.ReadSlot(0, context.player);

			// 2. 获取Promise实例和状态
			Debug.Assert(thisPtr.ValueType == NaNBoxing.BoxType.HeapPtr);
			var promiseInstance = context.GC.Heap[thisPtr.HeapPtr];
			var promiseFacility = (RtPayloadInstance)promiseInstance.facility;
			var promiseWapper = (PromiseWapper)promiseFacility.wapperedObject;

			// 3. 状态检查 - 只能从pending转换为fulfilled
			if (promiseWapper._state != PromiseState.pending)
			{
				// Promise已经解决，直接返回（符合Promise规范）
				return;
			}

			// 3.1 value需要提升到堆里
			value = context.player.GetSaveValue(value, ref error);
			if (error.raised)
			{
				error.error.setFault(); // 这里无法恢复
				return;
			}


			// 4. 更新Promise状态和值
			promiseWapper._state = PromiseState.fulfilled;
			promiseWapper._value = value;

			// 5. 处理onFulfilled回调队列
			if (promiseWapper.reactions != null && promiseWapper.reactions.Count > 0)
			{
				// 创建微任务队列条目
				foreach (var callback in promiseWapper.reactions)
				{
					var microTask = new PromiseMicroTask
					{
						Type = MicroTaskType.PromiseFulfill,
						//PromiseInstance = thisPtr,
						NextPromiseInstance = callback.nextPromise,
						CallbackFunction = callback.onFulfilled,
						Value = value
					};

					// 调度微任务执行
					context.MicroTaskQueue.Enqueue(microTask);
				}

				// 清空回调列表，释放内存
				promiseWapper.reactions.Clear();
				promiseWapper.reactions = null;
				
			}

			// 6. 如果没有回调，Promise就保持fulfilled状态
			// 这是正常的，后续的then()调用会立即执行

		}

		[NativeFunction(".Promise$private::_reject")]
		public static void Promise_reject(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			// 1. 获取reject的参数reason
			var scope = (RtPayloadMethodScope)context.GC.Heap[scope_ptr].facility;
			var reason = scope.ReadSlot(0, context.player);

			// 2. 获取Promise实例和状态
			var promiseInstance = context.GC.Heap[thisPtr.HeapPtr];
			var promiseFacility = (RtPayloadInstance)promiseInstance.facility;
			var promiseWapper = (PromiseWapper)promiseFacility.wapperedObject;

			// 3. 状态检查
			if (promiseWapper._state != PromiseState.pending)
			{
				return;
			}
			// 3.1 reason提升到堆
			reason = context.player.GetSaveValue(reason, ref error);
			if (error.raised)
			{
				error.error.setFault(); // 无法恢复的异常
				return;
			}

			// 4. 更新Promise状态和reason
			promiseWapper._state = PromiseState.rejected;
			promiseWapper._reason = reason;

			// 5. 处理onRejected回调队列
			if (promiseWapper.reactions != null && promiseWapper.reactions.Count > 0)
			{
				foreach (var callback in promiseWapper.reactions)
				{
					var microTask = new PromiseMicroTask
					{
						Type = MicroTaskType.PromiseReject,
						//PromiseInstance = thisPtr,
						NextPromiseInstance = callback.nextPromise,
						CallbackFunction = callback.onRejected,
						Value = reason
					};

					context.MicroTaskQueue.Enqueue(microTask);
				}

				// 清空回调列表
				promiseWapper.reactions.Clear();
				promiseWapper.reactions = null;
			}
			else
			{
				// 如果没有onRejected回调，且没有catch处理，
				// 这个rejected Promise会在未来被unhandled rejection检测到
				// （可选实现）
			}
		}



		[NativeFunction(".Promise$public::then")]
		public static void Promise_then(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtPayloadMethodScope)context.GC.Heap[scope_ptr].facility;
			// 读取两个参数
			var onFulfilled = scope.ReadSlot(0, context.player);
			var onRejected = scope.ReadSlot(1, context.player);
			// 验证回调函数（如果提供）
			if (onFulfilled.ValueType != NaNBoxing.BoxType.Undefined &&
				onFulfilled.ValueType != NaNBoxing.BoxType.Null)
			{
				if (!IsCallable(onFulfilled, context, out _))
				{
					context.player.RaiseTypeError(ref error, onFulfilled, TypeKind.Function);
					return;
				}
			}

			if (onRejected.ValueType != NaNBoxing.BoxType.Undefined &&
				onRejected.ValueType != NaNBoxing.BoxType.Null)
			{
				if (!IsCallable(onRejected, context, out _))
				{
					context.player.RaiseTypeError(ref error, onRejected, TypeKind.Function);
					return;
				}
			}

			onRejected = context.player.GetSaveValue(onRejected, ref error);
			if (error.raised)
			{
				return;
			}

			onFulfilled = context.player.GetSaveValue(onFulfilled, ref error);
			if (error.raised)
			{
				return;
			}

			var promiseInstance = context.GC.Heap[thisPtr.HeapPtr];
			var promiseFacility = (RtPayloadInstance)promiseInstance.facility;
			var promiseWapper = (PromiseWapper)promiseFacility.wapperedObject;

			RtHeapInstance nextPromiseInstance;
			var nextPromise_ptr = context.GC.AllocInstance((ASInstance)promiseInstance.Type, out nextPromiseInstance);
			if (nextPromise_ptr == 0)
			{
				context.player.RaiseOutOfMemory(ref error);
				return;
			}

			PromiseWapper wapper = new PromiseWapper();
			((RtPayloadInstance)nextPromiseInstance.facility).wapperedObject = wapper;
			
			NaNBoxing nextPromise = default;nextPromise.SetHeapPtr(nextPromise_ptr);

			if (promiseWapper._state == PromiseState.pending)
			{
				Reaction reaction = new Reaction();
				reaction.nextPromise = nextPromise;
				reaction.onRejected = onRejected;
				reaction.onFulfilled = onFulfilled;
				if (promiseWapper.reactions == null)
				{
					promiseWapper.reactions = new List<Reaction>();
				}
				promiseWapper.reactions.Add(reaction);

			}
			else if (promiseWapper._state == PromiseState.fulfilled)
			{
				var microTask = new PromiseMicroTask
				{
					Type = MicroTaskType.PromiseFulfill,
					//PromiseInstance = thisPtr,
					CallbackFunction = onFulfilled,
					Value = promiseWapper._value,
					NextPromiseInstance = nextPromise
				};

				// 调度微任务执行
				context.MicroTaskQueue.Enqueue(microTask);
			}
			else
			{
				Debug.Assert(promiseWapper._state == PromiseState.rejected);

				var microTask = new PromiseMicroTask
				{
					Type = MicroTaskType.PromiseReject,
					//PromiseInstance = thisPtr,
					CallbackFunction = onRejected,
					Value = promiseWapper._reason,
					NextPromiseInstance = nextPromise
				};
				// 调度微任务执行
				context.MicroTaskQueue.Enqueue(microTask);
			}

			context.StackSlots[returnSlotIndex].SetHeapPtr(nextPromise_ptr);

		}

		[NativeFunction(".Promise$public::catch")]
		public static void Promise_catch(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtPayloadMethodScope)context.GC.Heap[scope_ptr].facility;
			
			var onRejected = scope.ReadSlot(0, context.player);
			
			if (onRejected.ValueType != NaNBoxing.BoxType.Undefined &&
				onRejected.ValueType != NaNBoxing.BoxType.Null)
			{
				if (!IsCallable(onRejected, context, out _))
				{
					context.player.RaiseTypeError(ref error, onRejected, TypeKind.Function);
					return;
				}
			}

			onRejected = context.player.GetSaveValue(onRejected, ref error);
			if (error.raised)
			{
				return;
			}

			var promiseInstance = context.GC.Heap[thisPtr.HeapPtr];
			var promiseFacility = (RtPayloadInstance)promiseInstance.facility;
			var promiseWapper = (PromiseWapper)promiseFacility.wapperedObject;

			RtHeapInstance nextPromiseInstance;
			var nextPromise_ptr = context.GC.AllocInstance((ASInstance)promiseInstance.Type, out nextPromiseInstance);
			if (nextPromise_ptr == 0)
			{
				context.player.RaiseOutOfMemory(ref error);
				return;
			}

			PromiseWapper wapper = new PromiseWapper();
			((RtPayloadInstance)nextPromiseInstance.facility).wapperedObject = wapper;

			NaNBoxing nextPromise = default; nextPromise.SetHeapPtr(nextPromise_ptr);

			if (promiseWapper._state == PromiseState.pending)
			{
				Reaction reaction = new Reaction();
				reaction.nextPromise = nextPromise;
				reaction.onRejected = onRejected;
				
				if (promiseWapper.reactions == null)
				{
					promiseWapper.reactions = new List<Reaction>();
				}
				promiseWapper.reactions.Add(reaction);

			}
			else if (promiseWapper._state == PromiseState.fulfilled)
			{
				var microTask = new PromiseMicroTask
				{
					Type = MicroTaskType.PromiseFulfill,
					//PromiseInstance = thisPtr,
					CallbackFunction = new NaNBoxing() ,
					Value = promiseWapper._value,
					NextPromiseInstance = nextPromise
				};

				// 调度微任务执行
				context.MicroTaskQueue.Enqueue(microTask);
			}
			else
			{
				Debug.Assert(promiseWapper._state == PromiseState.rejected);

				var microTask = new PromiseMicroTask
				{
					Type = MicroTaskType.PromiseReject,
					//PromiseInstance = thisPtr,
					CallbackFunction = onRejected,
					Value = promiseWapper._reason,
					NextPromiseInstance = nextPromise
				};
				// 调度微任务执行
				context.MicroTaskQueue.Enqueue(microTask);
			}

			context.StackSlots[returnSlotIndex].SetHeapPtr(nextPromise_ptr);
		}




		static bool IsCallable(NaNBoxing value,Context context,out RtHeapInstance closure)
		{
			switch (value.ValueType)
			{
				case NaNBoxing.BoxType.HeapPtr:
					var heapInstance = context.GC.Heap[value.HeapPtr];
					closure = heapInstance;
					return heapInstance.TypeKind == RtHeapTypeKind.CLOSURE;
				case NaNBoxing.BoxType.Null:
				case NaNBoxing.BoxType.Undefined:
					closure = null;
					return false;
				default:
					closure = null;
					return false;
			}
		}

		// 微任务类型枚举
		public enum MicroTaskType
		{
			PromiseFulfill,
			PromiseReject
		}
		// 微任务结构
		public struct PromiseMicroTask
		{
			public MicroTaskType Type;
			//public NaNBoxing PromiseInstance;    // 当前Promise
			public NaNBoxing CallbackFunction;    // 回调函数
			public NaNBoxing Value;               // 传递的值
			public NaNBoxing NextPromiseInstance;  // then()返回的新Promise
		}

		public class PromiseMicroTaskQueue
		{
			// 使用环形缓冲区减少GC压力
			private PromiseMicroTask[] _taskBuffer;
			private int _head = 0;
			private int _tail = 0;
			private int _count = 0;
			private const int DefaultCapacity = 64;

			public PromiseMicroTaskQueue()
			{
				_taskBuffer = new PromiseMicroTask[DefaultCapacity];
			}

			public void Enqueue(PromiseMicroTask task)
			{
				if (_count >= _taskBuffer.Length)
				{
					// 扩容
					Array.Resize(ref _taskBuffer, _taskBuffer.Length * 2);
				}

				_taskBuffer[_tail] = task;
				_tail = (_tail + 1) % _taskBuffer.Length;
				_count++;
			}

			public bool HasPendingTasks => _count > 0;

			public bool TryDequeue(out PromiseMicroTask task)
			{
				if (_count == 0)
				{
					task = default;
					return false;
				}

				task = _taskBuffer[_head];
				_head = (_head + 1) % _taskBuffer.Length;
				_count--;
				return true;
			}

			public void Clear()
			{
				_head = 0;
				_tail = 0;
				_count = 0;
			}

			internal void RunMicrotasks(Context context)
			{
				PromiseMicroTask task;
				while ( TryDequeue(out task) )
				{
					if (task.Type == MicroTaskType.PromiseFulfill)
					{
						NaNBoxing value;

						if (task.CallbackFunction.ValueType != NaNBoxing.BoxType.HeapPtr)
						{
							//直接透传
							value = task.Value;

							ResolvePromise(context, task.NextPromiseInstance, value);

						}
						else
						{
							throw new NotImplementedException();
						}

						
						
					}
					else
					{
						throw new NotImplementedException();
					}

				}
			}

			private void ResolvePromise(Context context, NaNBoxing nextPromiseInstance, NaNBoxing value)
			{
				var p = (PromiseWapper)((RtPayloadInstance)context.GC.Heap[nextPromiseInstance.HeapPtr].facility).wapperedObject;

				if (context.player.IsStrictlyEqual(nextPromiseInstance, value))
				{
					
					ReceiveError tempErr = default;
					context.player.RaiseError(ref tempErr, "Chaining cycle");
					if (tempErr.error.ValueType == NaNBoxing.BoxType.Fault)
					{
						return;
					}					
					p.Reject(context,tempErr.error);
					return;
				}

				if ( context.player.IsPrimitive( value))
				{
					p.FulFill(context, value);
				}

				//考虑then able



			}


		}


		enum PromiseState
		{ 
			pending,
			fulfilled,
			rejected
		}


		class Reaction { public NaNBoxing onFulfilled; public NaNBoxing onRejected; public NaNBoxing nextPromise; }

		class PromiseWapper : RtWapperBase
		{
			internal PromiseState _state = PromiseState.pending;
			internal NaNBoxing _value;
			internal NaNBoxing _reason;
			internal List<Reaction> reactions;


			public void Reject(Context context,NaNBoxing error)
			{
				_state = PromiseState.rejected;
				_reason = error;

				if (reactions != null)
				{
					foreach (var callback in reactions)
					{
						var microTask = new PromiseMicroTask
						{
							Type = MicroTaskType.PromiseReject,
							//PromiseInstance = thisPtr,
							NextPromiseInstance = callback.nextPromise,
							CallbackFunction = callback.onRejected,
							Value = error
						};

						context.MicroTaskQueue.Enqueue(microTask);
					}

					// 清空回调列表
					reactions.Clear();
					reactions = null;
				}


			}

			internal void FulFill(Context context, NaNBoxing value)
			{
				_state = PromiseState.fulfilled;
				_value = value;

				if (reactions != null)
				{
					foreach (var callback in reactions)
					{
						var microTask = new PromiseMicroTask
						{
							Type = MicroTaskType.PromiseFulfill,
							//PromiseInstance = thisPtr,
							NextPromiseInstance = callback.nextPromise,
							CallbackFunction = callback.onFulfilled,
							Value = value
						};

						context.MicroTaskQueue.Enqueue(microTask);
					}

					// 清空回调列表
					reactions.Clear();
					reactions = null;
				}
			}


			public override void OnDelete()
			{
				reactions = null;
			}

			public override void OnGCMark(Context context)
			{
				if (_value.ValueType == NaNBoxing.BoxType.HeapPtr)
				{
					context.GC.mark( context.GC.Heap[ _value.HeapPtr]);
				}

				if (_reason.ValueType == NaNBoxing.BoxType.HeapPtr)
				{
					context.GC.mark(context.GC.Heap[_reason.HeapPtr]);
				}

				if (reactions != null)
				{
					for (int i = 0; i < reactions.Count; i++)
					{
						var reaction = reactions[i];
						if (reaction.onRejected.ValueType == NaNBoxing.BoxType.HeapPtr)
						{
							context.GC.mark(context.GC.Heap[reaction.onRejected.HeapPtr]);
						}
						if (reaction.onFulfilled.ValueType == NaNBoxing.BoxType.HeapPtr)
						{
							context.GC.mark(context.GC.Heap[reaction.onFulfilled.HeapPtr]);
						}
						if (reaction.nextPromise.ValueType == NaNBoxing.BoxType.HeapPtr)
						{
							context.GC.mark(context.GC.Heap[reaction.nextPromise.HeapPtr]);
						}
					}
				}
			}

			
		}


	}
}
