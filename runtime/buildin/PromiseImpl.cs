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
using static juicescript.runtime.buildin.GeneratorImpl;
using static juicescript.runtime.Player;
using static System.Formats.Asn1.AsnWriter;

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

			RtHeapBase _this = context.GC.Heap[thisPtr.HeapPtr];
			Debug.Assert(_this.Kind == RtHeapTypeKind.INSTANCE);
			Debug.Assert(_this.Type.QName.Name == "Promise");

			var executor = ((RtMethodScope)context.GC.Heap[scope_ptr]).ReadSlot(0, context.player);

			RtHeapBase executor_closure;
			// 2. 验证executor是否为函数
			if (!IsCallable(executor, context, out executor_closure))
			{
				context.player.RaiseTypeError(
					ref error, executor, TypeKind.Function);
				return;
			}

			if (stackStPos + 2 >= Context.STACK_LENGTH)
			{
				context.player.RaiseStackOverflow(ref error);
				return;
			}

			PromiseWapper wapper = new PromiseWapper();

			((RtInstance)_this).wapperedObject = wapper;


			// 准备参数 _resolve
			{
				var _resolve = _this.Type._vtable.Items[2];
				int ptrIndex = stackStPos;
				int m_closurePtr = Context.M_ClosurePtr + ptrIndex;

				context.GC.Heap[m_closurePtr].Type = _resolve.Trait.Method.Body;
				RtClosure closure = (RtClosure)context.GC.Heap[m_closurePtr];
				closure.This = thisPtr;
				closure.ScopePtr = scope_ptr;
				//closure.ScopeType = _resolve.DefineAt;
				closure._ref_as_type = _resolve.DefineAt;
				closure.methodscopeslot_ref_state = 0; closure.HEAPINSTANCE_PTR = 0;

				context.StackSlots[ptrIndex].SetHeapPtr(m_closurePtr,(byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);
			}
			// 准备参数 _reject
			{
				var _reject = _this.Type._vtable.Items[3];
				int ptrIndex = stackStPos + 1;
				int m_closurePtr = Context.M_ClosurePtr + ptrIndex;

				context.GC.Heap[m_closurePtr].Type = _reject.Trait.Method.Body;
				RtClosure closure = (RtClosure)context.GC.Heap[m_closurePtr];
				closure.This = thisPtr;
				closure.ScopePtr = scope_ptr;
				//closure.ScopeType = _reject.DefineAt;
				closure._ref_as_type = _reject.DefineAt;
				closure.methodscopeslot_ref_state = 0; closure.HEAPINSTANCE_PTR = 0;

				context.StackSlots[ptrIndex].SetHeapPtr(m_closurePtr,(byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);
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

				context.player.RunMethod(executor_method, ((RtClosure)executor_closure).This,
					((RtClosure)executor_closure).ScopePtr,
					//((RtClosure)executor_closure).ScopeType,
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
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
			var value = scope.ReadSlot(0, context.player);

			// 2. 获取Promise实例和状态
			Debug.Assert(thisPtr.ValueType == NaNBoxing.BoxType.HeapPtr);
			var promiseInstance = context.GC.Heap[thisPtr.HeapPtr];
			var promiseFacility = (RtInstance)promiseInstance;
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

			context.MicroTaskQueue.ResolvePromise(context, thisPtr, value, ref error);

			//if (value.ValueType == NaNBoxing.BoxType.HeapPtr)
			//{
			//	var heapInstance = context.GC.Heap[value.HeapPtr];
			//	if (heapInstance.TypeKind == RtHeapTypeKind.INSTANCE &&
			//		heapInstance.Type is ASInstance asInstance &&
			//		asInstance._link_codescope.TypeLayout.ASType.Type_identifier == context.PROMISE.Type_identifier
			//		)
			//	{
			//		var p = (PromiseWapper)((RtPayloadInstance)context.GC.Heap[thisPtr.HeapPtr]).wapperedObject;
			//		// Value is a Promise, adopt its state
			//		var valuePromise = (PromiseWapper)((RtPayloadInstance)heapInstance).wapperedObject;

			//		if (valuePromise._state == PromiseState.fulfilled)
			//		{
			//			p.FulFill(context, valuePromise._value);
			//			return;
			//		}
			//		else if (valuePromise._state == PromiseState.rejected)
			//		{
			//			p.Reject(context, valuePromise._reason);
			//			return;
			//		}
			//		else // pending
			//		{
			//			// Add reaction to wait for value Promise to settle
			//			Reaction reaction = new Reaction();
			//			reaction.nextPromise = thisPtr;

			//			// Create callbacks that will resolve/reject nextPromise
			//			// when valuePromise settles
			//			// (Implementation details in Algorithm 4)

			//			if (valuePromise.reactions == null)
			//			{
			//				valuePromise.reactions = new List<Reaction>();
			//			}
			//			valuePromise.reactions.Add(reaction);
			//			return;
			//		}
			//	}
			//}

			//// 4. 更新Promise状态和值
			//promiseWapper._state = PromiseState.fulfilled;
			//promiseWapper._value = value;

			//// 5. 处理onFulfilled回调队列
			//if (promiseWapper.reactions != null && promiseWapper.reactions.Count > 0)
			//{
			//	// 创建微任务队列条目
			//	foreach (var callback in promiseWapper.reactions)
			//	{
			//		var microTask = new PromiseMicroTask
			//		{
			//			Type = MicroTaskType.PromiseFulfill,
			//			//PromiseInstance = thisPtr,
			//			NextPromiseInstance = callback.nextPromise,
			//			CallbackFunction = callback.onFulfilled,
			//			Value = value
			//		};

			//		// 调度微任务执行
			//		context.MicroTaskQueue.Enqueue(microTask);
			//	}

			//	// 清空回调列表，释放内存
			//	promiseWapper.reactions.Clear();
			//	promiseWapper.reactions = null;

			//}

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
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
			var reason = scope.ReadSlot(0, context.player);

			// 2. 获取Promise实例和状态
			var promiseInstance = context.GC.Heap[thisPtr.HeapPtr];
			var promiseFacility = (RtInstance)promiseInstance;
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
				// 如果没有onRejected回调(这是默认回调)，且没有catch处理，
				// 这个rejected Promise会在未来被unhandled rejection检测到
				// （可选实现）

				//Console.Error.WriteLine($"Uncaught in {thisPtr.ToDebugString(context.player)}  ({reason.ToDebugString(context.player)})");

			}
		}



		[NativeFunction(".Promise$public::then")]
		public static void Promise_then(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
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
			var promiseFacility = (RtInstance)promiseInstance;
			var promiseWapper = (PromiseWapper)promiseFacility.wapperedObject;

			RtHeapBase nextPromiseInstance;
			var nextPromise_ptr = context.GC.AllocInstance((ASInstance)promiseInstance.Type, out nextPromiseInstance);
			if (nextPromise_ptr == 0)
			{
				context.player.RaiseOutOfMemory(ref error);
				return;
			}

			PromiseWapper wapper = new PromiseWapper();
			((RtInstance)nextPromiseInstance).wapperedObject = wapper;

			NaNBoxing nextPromise = default; nextPromise.SetHeapPtr(nextPromise_ptr, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.NONE);

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

			context.StackSlots[returnSlotIndex].SetHeapPtr(nextPromise_ptr, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.NONE);

		}

		[NativeFunction(".Promise$public::catch")]
		public static void Promise_catch(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

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
			var promiseFacility = (RtInstance)promiseInstance;
			var promiseWapper = (PromiseWapper)promiseFacility.wapperedObject;

			RtHeapBase nextPromiseInstance;
			var nextPromise_ptr = context.GC.AllocInstance((ASInstance)promiseInstance.Type, out nextPromiseInstance);
			if (nextPromise_ptr == 0)
			{
				context.player.RaiseOutOfMemory(ref error);
				return;
			}

			PromiseWapper wapper = new PromiseWapper();
			((RtInstance)nextPromiseInstance).wapperedObject = wapper;

			NaNBoxing nextPromise = default; nextPromise.SetHeapPtr(nextPromise_ptr, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.NONE);

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
					CallbackFunction = new NaNBoxing(),
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

			context.StackSlots[returnSlotIndex].SetHeapPtr(nextPromise_ptr, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.NONE);
		}

		[NativeFunction("$.Promise$public::resolve")]
		public static void Promise_static_resolve(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
			var value = scope.ReadSlot(0, context.player);

			if (value.ValueType == NaNBoxing.BoxType.HeapPtr)
			{
				var heapobj = context.GC.Heap[value.HeapPtr];
				if (heapobj.Kind == RtHeapTypeKind.INSTANCE && heapobj.Type == context.PROMISE.Instance)
				{
					context.StackSlots[returnSlotIndex] = value;
					return;
				}
			}

			RtHeapBase promise;
			var p = context.GC.AllocInstance(context.PROMISE.Instance, out promise);
			if (p == 0)
			{
				context.player.RaiseOutOfMemory(ref error);
				return;
			}

			value = context.player.GetSaveValue(value, ref error);
			if (error.raised)
			{
				return;
			}

			PromiseWapper wapper = new PromiseWapper();

			((RtInstance)promise).wapperedObject = wapper;
			wapper._state = PromiseState.pending;

			NaNBoxing promise_store = default;
			promise_store.SetHeapPtr(p, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.NONE);


			context.StackSlots[returnSlotIndex] = promise_store;

			context.MicroTaskQueue.ResolvePromise(context, promise_store, value, ref error);
			if (error.raised)
			{
				context.StackSlots[returnSlotIndex].SetUndefined();
			}
			else
			{
				context.StackSlots[returnSlotIndex] = promise_store;
			}
		}


		[NativeFunction("$.Promise$public::reject")]
		public static void Promise_static_reject(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
			var reason = scope.ReadSlot(0, context.player);

			// 提升 reason 到堆
			reason = context.player.GetSaveValue(reason, ref error);
			if (error.raised)
			{
				return;
			}

			// 创建一个新的 Promise
			RtHeapBase pInstance;
			int pPtr = context.GC.AllocInstance(context.PROMISE.Instance, out pInstance);
			if (pPtr == 0)
			{
				context.player.RaiseOutOfMemory(ref error);
				return;
			}

			PromiseWapper w = new PromiseWapper();
			((RtInstance)pInstance).wapperedObject = w;

			// 直接 reject
			w.Reject(context, reason);
			context.StackSlots[returnSlotIndex].SetHeapPtr(pPtr, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.NONE);


		}


		static bool IsCallable(NaNBoxing value, Context context, out RtHeapBase closure)
		{
			switch (value.ValueType)
			{
				case NaNBoxing.BoxType.HeapPtr:
					var heapInstance = context.GC.Heap[value.HeapPtr];
					closure = heapInstance;
					return heapInstance.Kind == RtHeapTypeKind.CLOSURE;
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

			internal void InitMethods(Context context)
			{
				Debug.Assert(context.PROMISE != null);

				thenableResolve = new ASMethod(context.PROMISE._link_codescope.Container, context.PROMISE.Token);
				thenableResolve.ReturnTypeKind = TypeKind.Fun_Void;
				thenableResolve.__ismethod = true;
				thenableResolve.Flags = MethodFlags.Native;
				thenableResolve.Name = "@thenableResolve";
				thenableResolve.Body = new ASMethodBody(thenableResolve);
				thenableResolve.Body.ByteCode = new byte[12];
				thenableResolve.Body._link_codescope = new CodeScope() { Members = new List<ScopeMember>(), Parent = context.PROMISE._link_codescope.Parent };
				thenableResolve.IsAnonymous = true;
				thenableResolve.Parameters.Add(new ASParameter(thenableResolve) { IsOptional = false, Name = "value", IsRest = false, TypeKind = TypeKind.Any });
				thenableResolve.Body._link_codescope.Members.Add(new ScopeMember(thenableResolve.Body, null)
				{
					Kind = ScopeMemberKind.Parameter,
					PName = "value",
					Type = thenableResolve.Parameters[0].Type,
					TypeKind = TypeKind.Any
				});

				// 注册 ThenableResolve native function
				//var resolveNativeFunc = NativeFunctionRegistry.GetFunction("__ThenableResolve__");
				//if (resolveNativeFunc != null)
				{
					thenableResolve.nativefunction_delegate = (NativeFun)ThenableResolve; //Delegate.CreateDelegate(typeof(NativeFun), resolveNativeFunc);
				}

				thenableReject = new ASMethod(context.PROMISE._link_codescope.Container, context.PROMISE.Token);
				thenableReject.ReturnTypeKind = TypeKind.Fun_Void;
				thenableReject.__ismethod = true;
				thenableReject.Flags = MethodFlags.Native;
				thenableReject.Name = "@thenableReject";
				thenableReject.Body = new ASMethodBody(thenableReject);
				thenableReject.Body.ByteCode = new byte[12];
				thenableReject.Body._link_codescope = new CodeScope() { Members = new List<ScopeMember>(), Parent = context.PROMISE._link_codescope.Parent };
				thenableReject.IsAnonymous = true;
				thenableReject.Parameters.Add(new ASParameter(thenableReject) { IsOptional = false, Name = "reason", IsRest = false, TypeKind = TypeKind.Any });
				thenableReject.Body._link_codescope.Members.Add(new ScopeMember(thenableReject.Body, null)
				{
					Kind = ScopeMemberKind.Parameter,
					PName = "value",
					Type = thenableReject.Parameters[0].Type,
					TypeKind = TypeKind.Any
				});

				// 注册 ThenableReject native function
				//var rejectNativeFunc = NativeFunctionRegistry.GetFunction("__ThenableReject__");
				//if (rejectNativeFunc != null)
				{
					thenableReject.nativefunction_delegate = (NativeFun)ThenableReject; //Delegate.CreateDelegate(typeof(NativeFun), rejectNativeFunc);
				}


				async_template_ctor = new ASMethod(context.PROMISE._link_codescope.Container, context.PROMISE.Token);
				async_template_ctor.ReturnTypeKind = TypeKind.Fun_Void;
				async_template_ctor.__ismethod = true;
				async_template_ctor.Flags = MethodFlags.Native;
				async_template_ctor.Name = "@async_template_ctor";
				async_template_ctor.Body = new ASMethodBody(async_template_ctor);
				async_template_ctor.Body.ByteCode = new byte[12];
				async_template_ctor.Body._link_codescope = new CodeScope() { Members = new List<ScopeMember>(), Parent = context.PROMISE._link_codescope.Parent };
				async_template_ctor.IsAnonymous = true;
				async_template_ctor.Parameters.Add(new ASParameter(async_template_ctor) { IsOptional = false, Name = "resolve", IsRest = false, TypeKind = TypeKind.Any });
				async_template_ctor.Parameters.Add(new ASParameter(async_template_ctor) { IsOptional = false, Name = "reject", IsRest = false, TypeKind = TypeKind.Any });
				async_template_ctor.Body._link_codescope.Members.Add(new ScopeMember(async_template_ctor.Body, null)
				{
					Kind = ScopeMemberKind.Parameter,
					PName = "resolve",
					Type = async_template_ctor.Parameters[0].Type,
					TypeKind = TypeKind.Any
				});
				async_template_ctor.Body._link_codescope.Members.Add(new ScopeMember(async_template_ctor.Body, null)
				{
					Kind = ScopeMemberKind.Parameter,
					PName = "reject",
					Type = async_template_ctor.Parameters[1].Type,
					TypeKind = TypeKind.Any
				});
				async_template_ctor.nativefunction_delegate = (NativeFun)AsyncTemplate_Ctor; //Delegate.CreateDelegate(typeof(NativeFun), rejectNativeFunc);



				async_then_onfulfilled = new ASMethod(context.PROMISE._link_codescope.Container, context.PROMISE.Token);
				async_then_onfulfilled.ReturnTypeKind = TypeKind.Fun_Void;
				async_then_onfulfilled.__ismethod = true;
				async_then_onfulfilled.Flags = MethodFlags.Native;
				async_then_onfulfilled.Name = "@async_template_then_onfulfilled";
				async_then_onfulfilled.Body = new ASMethodBody(async_then_onfulfilled);
				async_then_onfulfilled.Body.ByteCode = new byte[12];
				async_then_onfulfilled.Body._link_codescope = new CodeScope() { Members = new List<ScopeMember>(), Parent = context.PROMISE._link_codescope.Parent };
				async_then_onfulfilled.IsAnonymous = true;
				async_then_onfulfilled.Parameters.Add(new ASParameter(async_then_onfulfilled) { IsOptional = false, Name = "onFulfilled", IsRest = false, TypeKind = TypeKind.Any });
				async_then_onfulfilled.Body._link_codescope.Members.Add(new ScopeMember(async_then_onfulfilled.Body, null)
				{
					Kind = ScopeMemberKind.Parameter,
					PName = "onFulfilled",
					Type = async_then_onfulfilled.Parameters[0].Type,
					TypeKind = TypeKind.Any
				});
				async_then_onfulfilled.nativefunction_delegate = (NativeFun)AsyncTemplate_ThenOnfulfilled;



				async_then_onrejected = new ASMethod(context.PROMISE._link_codescope.Container, context.PROMISE.Token);
				async_then_onrejected.ReturnTypeKind = TypeKind.Fun_Void;
				async_then_onrejected.__ismethod = true;
				async_then_onrejected.Flags = MethodFlags.Native;
				async_then_onrejected.Name = "@async_template_then_onrejected";
				async_then_onrejected.Body = new ASMethodBody(async_then_onrejected);
				async_then_onrejected.Body.ByteCode = new byte[12];
				async_then_onrejected.Body._link_codescope = new CodeScope() { Members = new List<ScopeMember>(), Parent = context.PROMISE._link_codescope.Parent };
				async_then_onrejected.IsAnonymous = true;
				async_then_onrejected.Parameters.Add(new ASParameter(async_then_onrejected) { IsOptional = false, Name = "onRejected", IsRest = false, TypeKind = TypeKind.Any });
				async_then_onrejected.Body._link_codescope.Members.Add(new ScopeMember(async_then_onrejected.Body, null)
				{
					Kind = ScopeMemberKind.Parameter,
					PName = "onRejected",
					Type = async_then_onrejected.Parameters[0].Type,
					TypeKind = TypeKind.Any
				});
				async_then_onrejected.nativefunction_delegate = (NativeFun)AsyncTemplate_ThenOnRejected	;


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

			internal void RunMicrotasks(Context context, ref ReceiveError task_fault)
			{
				
				PromiseMicroTask task;
				unsafe
				{
					StackLocater* args1 = stackalloc StackLocater[1];
					args1->index = 0;

					while (TryDequeue(out task))
					{
						if (task.Type == MicroTaskType.PromiseFulfill)
						{
							if (task.CallbackFunction.ValueType != NaNBoxing.BoxType.HeapPtr)
							{
								if (context.StackPosition + 4 >= Context.STACK_LENGTH)
								{
									ReceiveError tempErr = default;
									context.player.RaiseStackOverflow(ref tempErr);
									if (tempErr.error.ValueType == NaNBoxing.BoxType.Fault)
									{
										task_fault = tempErr;
										return;
									}
								}
								int _bpos = context.StackPosition;
								context.StackSlots[_bpos] = task.Value;
								context.StackSlots[_bpos + 1].SetUndefined();
								context.StackSlots[_bpos + 2] = task.NextPromiseInstance;
								context.StackSlots[_bpos + 3] = task.CallbackFunction;
								context.StackPosition += 4;

								// onFulfilled 未提供，直接透传
								ResolvePromise(context, task.NextPromiseInstance, task.Value, ref task_fault);

								context.StackPosition = _bpos;


								continue;
							}

							var cbInstance = context.GC.Heap[task.CallbackFunction.HeapPtr];
							if (cbInstance.Kind != RtHeapTypeKind.CLOSURE)
							{
								if (context.StackPosition + 4 >= Context.STACK_LENGTH)
								{
									ReceiveError tempErr = default;
									context.player.RaiseStackOverflow(ref tempErr);
									if (tempErr.error.ValueType == NaNBoxing.BoxType.Fault)
									{
										task_fault = tempErr;
										return;
									}
								}
								int _bpos = context.StackPosition;
								context.StackSlots[_bpos] = task.Value;
								context.StackSlots[_bpos + 1].SetUndefined();
								context.StackSlots[_bpos + 2] = task.NextPromiseInstance;
								context.StackSlots[_bpos + 3] = task.CallbackFunction;
								context.StackPosition += 4;

								// 理论上不会发生；降级为透传
								ResolvePromise(context, task.NextPromiseInstance, task.Value, ref task_fault);

								context.StackPosition = _bpos;


								continue;
							}

							var cbClosure = (RtClosure)cbInstance;
							var cbMethod = ((ASMethodBody)cbInstance.Type).Method;

							if (context.StackPosition + 4 >= Context.STACK_LENGTH)
							{
								ReceiveError tempErr = default;
								context.player.RaiseStackOverflow(ref tempErr);
								if (tempErr.error.ValueType == NaNBoxing.BoxType.Fault)
								{
									task_fault = tempErr;
									return;
								}

								((PromiseWapper)((RtInstance)context.GC.Heap[task.NextPromiseInstance.HeapPtr]).wapperedObject)
									.Reject(context, tempErr.error);

								continue;
							}

							int basePos = context.StackPosition;
							context.StackSlots[basePos] = task.Value;
							context.StackSlots[basePos + 1].SetUndefined();
							context.StackSlots[basePos + 2] = task.NextPromiseInstance;
							context.StackSlots[basePos + 3] = task.CallbackFunction;
							context.StackPosition += 4;

							ReceiveError error = default;
							var slots = context.StackSlots.AsSpan(basePos, 1);

							context.player.RunMethod(
								cbMethod,
								cbClosure.This,
								cbClosure.ScopePtr,
								//cbClosure.ScopeType,
								1,
								(byte*)args1,
								slots,
								ref error,
								basePos + 1,
								task.CallbackFunction.HeapPtr
							);

							context.StackPosition -= 4;

							if (error.raised)
							{
								if (error.error.ValueType == NaNBoxing.BoxType.Fault)
								{
									task_fault = error;
									return;
								}

								NaNBoxing e = error.error;
								error.error.SetUndefined();
								error.raised = false;
								context.errorStack.Clear();

								ReceiveError tempErr = default;
								NaNBoxing reason = context.player.GetSaveValue(e, ref tempErr);
								if (tempErr.raised)
								{
									tempErr.error.setFault();
									task_fault = tempErr;
									return;
								}

								((PromiseWapper)((RtInstance)context.GC.Heap[task.NextPromiseInstance.HeapPtr]).wapperedObject)
									.Reject(context, reason);

								continue;
							}

							NaNBoxing ret = context.StackSlots[basePos + 1];
							ReceiveError saveErr = default;
							ret = context.player.GetSaveValue(ret, ref saveErr);
							if (saveErr.raised)
							{
								saveErr.error.setFault();
								task_fault = saveErr;
								return;
							}

							ResolvePromise(context, task.NextPromiseInstance, ret, ref error);
						}
						else
						{
							Debug.Assert(task.Type == MicroTaskType.PromiseReject);

							if (task.CallbackFunction.ValueType != NaNBoxing.BoxType.HeapPtr)
							{
								// onRejected 未提供，直接向后透传拒绝
								((PromiseWapper)((RtInstance)context.GC.Heap[task.NextPromiseInstance.HeapPtr]).wapperedObject)
									.Reject(context, task.Value);
								continue;
							}

							var cbInstance = context.GC.Heap[task.CallbackFunction.HeapPtr];
							if (cbInstance.Kind != RtHeapTypeKind.CLOSURE)
							{
								((PromiseWapper)((RtInstance)context.GC.Heap[task.NextPromiseInstance.HeapPtr]).wapperedObject)
									.Reject(context, task.Value);
								continue;
							}

							var cbClosure = (RtClosure)cbInstance;
							var cbMethod = ((ASMethodBody)cbInstance.Type).Method;

							if (context.StackPosition + 4 >= Context.STACK_LENGTH)
							{
								ReceiveError tempErr = default;
								context.player.RaiseStackOverflow(ref tempErr);
								if (tempErr.error.ValueType == NaNBoxing.BoxType.Fault)
								{
									task_fault = tempErr;
									return;
								}

								((PromiseWapper)((RtInstance)context.GC.Heap[task.NextPromiseInstance.HeapPtr]).wapperedObject)
									.Reject(context, tempErr.error);

								continue;
							}

							int basePos = context.StackPosition;
							context.StackSlots[basePos] = task.Value;
							context.StackSlots[basePos + 1].SetUndefined();
							context.StackSlots[basePos + 2] = task.NextPromiseInstance;
							context.StackSlots[basePos + 3] = task.CallbackFunction;

							context.StackPosition += 4;

							ReceiveError error = default;
							var slots = context.StackSlots.AsSpan(basePos, 1);

							context.player.RunMethod(
								cbMethod,
								cbClosure.This,
								cbClosure.ScopePtr,
								//cbClosure.ScopeType,
								1,
								(byte*)args1,
								slots,
								ref error,
								basePos + 1,
								task.CallbackFunction.HeapPtr
							);

							context.StackPosition -= 4;

							if (error.raised)
							{
								if (error.error.ValueType == NaNBoxing.BoxType.Fault)
								{
									task_fault = error;
									return;
								}

								NaNBoxing e = error.error;
								error.error.SetUndefined();
								error.raised = false;
								context.errorStack.Clear();

								ReceiveError tempErr = default;
								NaNBoxing reason = context.player.GetSaveValue(e, ref tempErr);
								if (tempErr.raised)
								{
									tempErr.error.setFault();
									task_fault = tempErr;
									return;
								}

								((PromiseWapper)((RtInstance)context.GC.Heap[task.NextPromiseInstance.HeapPtr]).wapperedObject)
									.Reject(context, reason);

								continue;
							}

							NaNBoxing ret = context.StackSlots[basePos + 1];
							ReceiveError saveErr = default;
							ret = context.player.GetSaveValue(ret, ref saveErr);
							if (saveErr.raised)
							{
								saveErr.error.setFault();
								task_fault = saveErr;
								return;
							}

							// onRejected 返回值会使 nextPromise 走 resolve 流程（通常转为 fulfilled）
							ResolvePromise(context, task.NextPromiseInstance, ret, ref error);
						}

					}
				}
			}

			internal void ResolvePromise(Context context, NaNBoxing nextPromiseInstance, NaNBoxing value, ref ReceiveError resolve_falut)
			{
				var p = (PromiseWapper)((RtInstance)context.GC.Heap[nextPromiseInstance.HeapPtr]).wapperedObject;

				if (context.player.IsStrictlyEqual(nextPromiseInstance, value))
				{

					ReceiveError tempErr = default;
					context.player.RaiseError(ref tempErr, "Chaining cycle");
					if (tempErr.error.ValueType == NaNBoxing.BoxType.Fault)
					{
						resolve_falut = tempErr;
						return;
					}
					p.Reject(context, tempErr.error);
					return;
				}

				if (context.player.IsPrimitive(value))
				{
					p.FulFill(context, value);
					return;
				}

				// Step 5: Check if value is a Promise instance
				var heapInstance = context.GC.Heap[value.HeapPtr];
				if (heapInstance.Kind == RtHeapTypeKind.INSTANCE &&
					heapInstance.Type is ASInstance asInstance &&
					asInstance._link_codescope.TypeLayout.ASType.Type_identifier == context.PROMISE.Type_identifier
					)
				{
					// Value is a Promise, adopt its state
					var valuePromise = (PromiseWapper)((RtInstance)heapInstance).wapperedObject;

					if (valuePromise._state == PromiseState.fulfilled)
					{
						p.FulFill(context, valuePromise._value);
						return;
					}
					else if (valuePromise._state == PromiseState.rejected)
					{
						p.Reject(context, valuePromise._reason);
						return;
					}
					else // pending
					{
						// Add reaction to wait for value Promise to settle
						Reaction reaction = new Reaction();
						reaction.nextPromise = nextPromiseInstance;

						// Create callbacks that will resolve/reject nextPromise
						// when valuePromise settles
						// (Implementation details in Algorithm 4)

						if (valuePromise.reactions == null)
						{
							valuePromise.reactions = new List<Reaction>();
						}
						valuePromise.reactions.Add(reaction);
						return;
					}
				}
				//考虑then able
				Debug.Assert(context.player.nsSetIncludingPublicAndAS3 != null); //必须的命名空间已经准备好。globalswc里就有，不可能没有。

				// Step 6: Try to get "then" property
				ReceiveError thenErr = default;
				NaNBoxing thenValue;
				if (!TryGetThenProperty(context, value, out thenValue, ref thenErr))
				{
					// Error accessing "then" property
					if (thenErr.error.ValueType == NaNBoxing.BoxType.Fault)
					{
						resolve_falut = thenErr;
						return; // Unrecoverable fault
					}

					ReceiveError err2 = default;
					NaNBoxing reason = context.player.GetSaveValue(thenErr.error, ref err2);
					if (err2.raised)
					{
						err2.error.setFault();
						resolve_falut = err2;
						return; // Unrecoverable fault
					}

					p.Reject(context, reason);
					return;
				}

				// Step 7: Check if "then" is callable
				RtHeapBase thenClosure;
				if (!IsCallable(thenValue, context, out thenClosure))
				{
					// "then" is not a function, fulfill with value
					p.FulFill(context, value);
					return;
				}

				// Step 8: Call thenable.then(resolveCallback, rejectCallback)
				ReceiveError callErr = default;
				CallThenable(context, thenValue, value, nextPromiseInstance, ref callErr);

				if (callErr.raised)
				{
					if (callErr.error.ValueType == NaNBoxing.BoxType.Fault)
					{
						resolve_falut = callErr;
						return; // Unrecoverable fault
					}

					// 提升 error 到堆
					ReceiveError err2 = default;
					NaNBoxing reason = context.player.GetSaveValue(callErr.error, ref err2);
					if (err2.raised)
					{
						err2.error.setFault();
						resolve_falut = err2;
						return; // Unrecoverable fault
					}

					p.Reject(context, reason);
					return;
				}

			}

			private bool TryGetThenProperty(
				Context context,
				NaNBoxing value,
				out NaNBoxing thenValue,
				ref ReceiveError error)
			{
				thenValue = default;
				thenValue.SetUndefined();

				// Validate input
				if (value.ValueType != NaNBoxing.BoxType.HeapPtr)
				{
					return true; // Not an object, "then" is undefined
				}

				var heapInstance = context.GC.Heap[value.HeapPtr];

				// Get type information
				RtHeapTypeKind kind = heapInstance.Kind;
				ASContainer as_type = heapInstance.Type as ASContainer;

				if (as_type == null)
				{
					return true; // No type info, "then" is undefined
				}

				if (kind != RtHeapTypeKind.INSTANCE)
				{
					return true;
				}

				// Get namespace set for property resolution
				ASNamespaceSet ns_set = context.player.nsSetIncludingPublicAndAS3;

				// Check stack space
				if (context.StackPosition + 4 >= Context.STACK_LENGTH)
				{
					context.player.RaiseStackOverflow(ref error);
					return false;
				}

				int basePos = context.StackPosition;
				context.StackPosition += 4; // Reserve space for property lookup


				// Use MultiNameLSearch to find "then" property
				StackLocater stack = new StackLocater { index = 0 };
				var stackslots = context.StackSlots.AsSpan(basePos, 4);
				stackslots.Clear();

				int code = context.player.MultiNameLSearch(
					ns_set,
					kind,
					as_type,
					"then",0,
					stack,
					stackslots,
					basePos,
					value,
					context.player.check_MultiNameLSearch_issameorinherit(value,heapInstance) , // this_ptr same as instance
					ref error,
					true   // exclude_user_ns
				);

				if (code == 1)
				{
					context.StackPosition = basePos;
					// Error during property search
					return false;
				}
				else if (code == 2)
				{
					context.StackPosition = basePos;
					// Ambiguous property - treat as error
					context.player.RaiseError(ref error, "Ambiguous property 'then'");
					return false;
				}
				else if (code == 0)
				{
					// Property found - result is in stackslots[0]
					NaNBoxing result = stackslots[0];

					// Check if result is a cache object (getter/setter)
					if (result.ValueType == NaNBoxing.BoxType.HeapPtr)
					{
						var resultHeap = context.GC.Heap[result.HeapPtr];

						if (resultHeap.Kind == RtHeapTypeKind.STACK_CACHE_OBJ)
						{
							// This is a property accessor, need to invoke getter
							var cache = (RtStackCache)resultHeap;

							if (cache.trait[0] != null && cache.trait[0].Kind == TraitKind.Getter)
							{
								// Invoke getter
								ASMethod getterMethod = cache.trait[0].Method;
								NaNBoxing thisPtr = value; // Use the object as 'this'

								try
								{
									unsafe
									{
										thenValue = context.player.RunMethod(
											getterMethod,
											thisPtr,
											value.HeapPtr,
											//(ASContainer)heapInstance.Type,
											0,        // No arguments
											null,     // No argument pointer
											stackslots,
											ref error,
											basePos   // Return slot
										);
									}

									if (error.raised)
									{
										context.StackPosition = basePos;
										return false;
									}

									context.StackPosition = basePos;
									return true;
								}
								catch
								{
									context.StackPosition = basePos;
									return false;
								}
							}
							else if (cache.searchPropertyName.ValueType != NaNBoxing.BoxType.Undefined)
							{
								// Dynamic property - search through prototype chain
								NaNBoxing dynValue;
								int matchShapePtr;
								int slotIndex;
								RtDynamic dynProp;

								// First search on the object itself
								if (context.player.FindDynamicValue(heapInstance, "then", out dynValue, out matchShapePtr, out slotIndex, out dynProp))
								{
									thenValue = dynValue;
									context.StackPosition = basePos;
									return true;
								}

								// Not found on object, search prototype chain
								int protoPtr = 0;

								// Get prototype based on object type
								if (heapInstance.Kind == RtHeapTypeKind.INSTANCE)
								{
									protoPtr = ((RtInstance)heapInstance).PROTOTYPE(context.player, (ASInstance)heapInstance.Type);
								}
								else if (heapInstance.Kind == RtHeapTypeKind.CLOSURE)
								{
									protoPtr = ((RtClosure)heapInstance).PROTOTYPE(context.player);
								}
								else if (heapInstance.Kind == RtHeapTypeKind.ARRAY)
								{
									protoPtr = ((RtScriptClass)context.GC.Heap[context.ARRAY.__instance_index__]).PROTO__PTR;
								}
								else if (heapInstance.Kind == RtHeapTypeKind.GLOBAL)
								{
									protoPtr = ((RtScriptClass)context.GC.Heap[context.OBJECT.__instance_index__]).PROTO__PTR;
								}

								// Walk the prototype chain
								int maxSteps = 32; // Prevent infinite loops
								while (protoPtr > 0 && maxSteps > 0)
								{
									var protoObj = context.GC.Heap[protoPtr];

									if (context.player.FindDynamicValue(protoObj, "then", out dynValue, out matchShapePtr, out slotIndex, out dynProp))
									{
										thenValue = dynValue;
										context.StackPosition = basePos;
										return true;
									}

									// Move to next prototype
									if (protoObj.Kind == RtHeapTypeKind.INSTANCE)
									{
										protoPtr = ((RtInstance)protoObj).PROTOTYPE(context.player, (ASInstance)protoObj.Type);
									}
									else
									{
										break; // Can't continue
									}

									maxSteps--;
								}

								// Not found in prototype chain, "then" is undefined
								thenValue.SetUndefined();
								context.StackPosition = basePos;
								return true;
							}
						}
						else if (resultHeap.Kind == RtHeapTypeKind.CLOSURE)
						{
							// Found a method closure
							thenValue = result;
							context.StackPosition = basePos;
							return true;
						}
					}

					// Direct value
					thenValue = result;
					context.StackPosition = basePos;
					return true;
				}
				else
				{
					Debug.Assert(false, "返回code超过预期");

					context.StackPosition = basePos;
					return false;
				}
			}
			ASMethod thenableResolve;
			ASMethod thenableReject;

			internal ASMethod async_template_ctor;
			internal ASMethod async_then_onfulfilled;
			internal ASMethod async_then_onrejected;

			// CallThenable - 调用thenable的then方法
			private void CallThenable(
				Context context,
				NaNBoxing thenFunction,
				NaNBoxing thenableObject,
				NaNBoxing targetPromise,
				ref ReceiveError error)
			{
				// 确保 thenableResolve 和 thenableReject 已初始化
				Debug.Assert(thenableResolve != null);
				Debug.Assert(thenableReject != null);


				// 创建共享状态对象
				RtHeapBase stateObj;
				int statePtr = context.GC.AllocInstance(context.OBJECT.Instance, out stateObj);
				if (statePtr == 0)
				{
					context.player.RaiseOutOfMemory(ref error);
					return;
				}

				var callbackState = new ThenableCallbackState
				{
					alreadyCalled = false,
					targetPromise = targetPromise,
				};
				((RtInstance)stateObj).wapperedObject = callbackState;

				// 检查栈空间
				if (context.StackPosition + 2 >= Context.STACK_LENGTH)
				{
					context.player.RaiseStackOverflow(ref error);
					return;
				}

				int basePos = context.StackPosition;
				context.StackPosition += 2;



				// 创建 resolve 回调闭包
				int resolveCb = Context.M_ClosurePtr + basePos;

				RtClosure resolveClosure = (RtClosure)context.GC.Heap[resolveCb];
				context.GC.Heap[resolveCb].Type = thenableResolve.Body;
				resolveClosure.This.SetHeapPtr(statePtr, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.NONE);
				resolveClosure.ScopePtr = statePtr;
				//resolveClosure.ScopeType = stateObj.Type;
				resolveClosure._ref_as_type = context.PROMISE;
				resolveClosure.methodscopeslot_ref_state = 0; resolveClosure.HEAPINSTANCE_PTR = 0;

				NaNBoxing resolveCallback = default;
				resolveCallback.SetHeapPtr(resolveCb, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);

				// 创建 reject 回调闭包
				int rejectCb = Context.M_ClosurePtr + basePos + 1;

				RtClosure rejectClosure = (RtClosure)context.GC.Heap[rejectCb];
				context.GC.Heap[rejectCb].Type = thenableReject.Body;
				rejectClosure.This.SetHeapPtr(statePtr, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.NONE);
				rejectClosure.ScopePtr = statePtr;
				//rejectClosure.ScopeType = stateObj.Type;
				rejectClosure._ref_as_type = context.PROMISE;
				rejectClosure.methodscopeslot_ref_state = 0; rejectClosure.HEAPINSTANCE_PTR = 0;

				NaNBoxing rejectCallback = default;
				rejectCallback.SetHeapPtr(rejectCb, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);


				// 将回调放到栈上
				context.StackSlots[basePos] = resolveCallback;
				context.StackSlots[basePos + 1] = rejectCallback;

				// 调用 thenable.then(resolveCallback, rejectCallback)
				var thenClosure = context.GC.Heap[thenFunction.HeapPtr];
				var thenMethod = ((ASMethodBody)thenClosure.Type).Method;
				var thenPayload = (RtClosure)thenClosure;

				unsafe
				{
					StackLocater* args = stackalloc StackLocater[2];
					args[0].index = 0;
					args[1].index = 1;

					var slots = context.StackSlots.AsSpan(basePos, 2);

					context.player.RunMethod(
						thenMethod,
						thenableObject,  // this 指向 thenable 对象
						thenPayload.ScopePtr,
						//thenPayload.ScopeType,
						2,  // 两个参数
						(byte*)args,
						slots,
						ref error,
						-1  // 不需要返回值
					);
				}



				if (error.raised)
				{
					// 调用 then 时发生错误，拒绝 Promise
					if (!callbackState.alreadyCalled)
					{
						callbackState.alreadyCalled = true;

						// 提升 error 到堆
						ReceiveError err2 = default;
						NaNBoxing reason = context.player.GetSaveValue(error.error, ref err2);
						if (err2.raised)
						{
							context.StackPosition = basePos;
							return; // Unrecoverable fault
						}

						var targetWapper = (PromiseWapper)((RtInstance)context.GC.Heap[targetPromise.HeapPtr]).wapperedObject;
						targetWapper.Reject(context, reason);
					}
				}

				context.StackPosition = basePos;

			}


			internal void OnGCMark(Context context)
			{
				var h = _head;
				var c = _count;

				while (c > 0)
				{
					var task = _taskBuffer[h];
					h = (h + 1) % _taskBuffer.Length;
					c--;


					if (task.NextPromiseInstance.ValueType == NaNBoxing.BoxType.HeapPtr)
					{
						context.GC.mark(context.GC.Heap[task.NextPromiseInstance.HeapPtr]);
					}
					if (task.Value.ValueType == NaNBoxing.BoxType.HeapPtr)
					{
						context.GC.mark(context.GC.Heap[task.Value.HeapPtr]);
					}
					if (task.CallbackFunction.ValueType == NaNBoxing.BoxType.HeapPtr)
					{
						context.GC.mark(context.GC.Heap[task.CallbackFunction.HeapPtr]);
					}


				}


			}

			internal int CreateNativePromise(Context context,out RtHeapBase promise)
			{
				int ptr = context.GC.AllocInstance(context.PROMISE.Instance, out promise);
				if (ptr != 0)
				{
					PromiseWapper wapper = new PromiseWapper();
					((RtInstance)promise).wapperedObject = wapper;
				}

				return ptr;
			}
		}


		internal enum PromiseState
		{
			pending,
			fulfilled,
			rejected
		}


		internal class Reaction { public NaNBoxing onFulfilled; public NaNBoxing onRejected; public NaNBoxing nextPromise; }

		internal class PromiseWapper : RtWapperBase
		{
			internal PromiseState _state = PromiseState.pending;
			internal NaNBoxing _value;
			internal NaNBoxing _reason;
			internal List<Reaction> reactions;


			public void Reject(Context context, NaNBoxing error)
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
					context.GC.mark(context.GC.Heap[_value.HeapPtr]);
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

		// Thenable回调状态类 - 用于存储thenable解析时的回调状态
		class ThenableCallbackState : RtWapperBase
		{
			internal bool alreadyCalled;
			internal NaNBoxing targetPromise;

			public override void OnGCMark(Context context)
			{
				if (targetPromise.ValueType == NaNBoxing.BoxType.HeapPtr)
				{
					context.GC.mark(context.GC.Heap[targetPromise.HeapPtr]);
				}
			}

			public override void OnDelete()
			{

			}

		}

		// Thenable Resolve 回调 - 作为 native function 被 ActionScript 调用
		public static void ThenableResolve(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			// thisPtr 指向包含 ThenableCallbackState 的对象
			if (thisPtr.ValueType != NaNBoxing.BoxType.HeapPtr)
			{
				return;
			}

			var stateObj = context.GC.Heap[thisPtr.HeapPtr];
			var state = ((RtInstance)stateObj).wapperedObject as ThenableCallbackState;

			if (state == null || state.alreadyCalled)
			{
				return;
			}

			state.alreadyCalled = true;

			// 从 scope 读取参数
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
			var value = scope.ReadSlot(0, context.player);

			// 递归调用 ResolvePromise
			context.MicroTaskQueue.ResolvePromise(context, state.targetPromise, value, ref error);
		}

		// Thenable Reject 回调 - 作为 native function 被 ActionScript 调用
		public static void ThenableReject(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			// thisPtr 指向包含 ThenableCallbackState 的对象
			if (thisPtr.ValueType != NaNBoxing.BoxType.HeapPtr)
			{
				return;
			}

			var stateObj = context.GC.Heap[thisPtr.HeapPtr];
			var state = ((RtInstance)stateObj).wapperedObject as ThenableCallbackState;

			if (state == null || state.alreadyCalled)
			{
				return;
			}

			state.alreadyCalled = true;

			// 从 scope 读取参数
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
			var reason = scope.ReadSlot(0, context.player);

			// reason 提升到堆
			reason = context.player.GetSaveValue(reason, ref error);
			if (error.raised)
			{
				error.error.setFault(); // 无法恢复的异常
				return;
			}

			// 拒绝目标 Promise
			var targetWapper = (PromiseWapper)((RtInstance)context.GC.Heap[state.targetPromise.HeapPtr]).wapperedObject;
			targetWapper.Reject(context, reason);
		}



		internal class AsyncGenWapper : RtWapperBase, Player.IResume_State
		{
			public int async_body;

			/// <summary>
			/// 0 -- 刚初始化，未执行next
			/// 1 -- 已执行一次await,
			/// 2 -- 结束，运行完成            
			/// </summary>
			public int state;
			internal NaNBoxing thisPtr;
			//internal ASContainer scopeType;

			internal Player.ExceptionContext[] exceptionContext;
			internal int exception_ctx_at;

			internal int RESUME_PC;


			internal NaNBoxing resolved_value;
			internal bool isrejected;
			internal NaNBoxing rejected_value;

#if DEBUG
			private int _iter_ctx_index_;
			public unsafe void Debug_SaveOrLoadIterCtxIndex(int* iter_ctx_index)
			{
				if (state == 0)
				{
					_iter_ctx_index_ = *iter_ctx_index;
				}
				else
				{
					*iter_ctx_index = _iter_ctx_index_;
				}
			}
#endif
			public void End()
			{
				state = 2;
			}

			public bool IsCallClose()
			{
				return false;
			}

			public override void OnDelete()
			{
				async_body = 0;
			}

			public override void OnGCMark(Context context)
			{
				context.GC.mark(context.GC.Heap[async_body]);

				if (thisPtr.ValueType == NaNBoxing.BoxType.HeapPtr)
				{
					context.GC.mark(context.GC.Heap[thisPtr.HeapPtr]);
				}

			}

			public unsafe void Resume(ExceptionContext* e_ctx, ExceptionContext** current_e_ctx, byte* PC_START, byte** PC, Span<NaNBoxing> stackslots)
			{
				if (state == 0)
					return;
				*PC = PC_START + RESUME_PC;

				for (int i = 1; i < exception_ctx_at + 1; i++)
				{
					*(e_ctx + i) = exceptionContext[i];

					stackslots[exceptionContext[i].hold_error.index].setFault();

				}

				*current_e_ctx = e_ctx + exception_ctx_at;

			}
		}


		internal static void AsyncTemplate_ThenOnRejected(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var promiseInstance = context.GC.Heap[thisPtr.HeapPtr];
			var promiseFacility = (RtInstance)promiseInstance;
			var promiseWapper = (PromiseWapper)promiseFacility.wapperedObject;

			Debug.Assert(promiseWapper._state == PromiseState.pending);

			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			var v = scope.ReadSlot(0, context.player);
			int genwapper_ptr = scope.ParentPtr;

			var genwapper = (PromiseImpl.AsyncGenWapper)((RtInstance)context.GC.Heap[genwapper_ptr]).wapperedObject;
			genwapper.rejected_value = v;
			genwapper.isrejected = true;

			AsyncTemplate_Step(context, genwapper_ptr, thisPtr, ref error);

		}

		internal static void AsyncTemplate_ThenOnfulfilled(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var promiseInstance = context.GC.Heap[thisPtr.HeapPtr];
			var promiseFacility = (RtInstance)promiseInstance;
			var promiseWapper = (PromiseWapper)promiseFacility.wapperedObject;

			Debug.Assert(promiseWapper._state == PromiseState.pending);

			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			var v = scope.ReadSlot(0, context.player);
			int genwapper_ptr = scope.ParentPtr;

			var genwapper = (PromiseImpl.AsyncGenWapper)((RtInstance)context.GC.Heap[genwapper_ptr]).wapperedObject;
			genwapper.resolved_value = v;
			genwapper.isrejected = false;

			AsyncTemplate_Step(context,  genwapper_ptr, thisPtr, ref error);
			
		}


		private static void AsyncTemplate_Step(Context context,
			
			int genwapper_ptr,
			NaNBoxing promisePtr,
			ref ReceiveError error)
		{
			var genwapper = (PromiseImpl.AsyncGenWapper)((RtInstance)context.GC.Heap[genwapper_ptr]).wapperedObject;

			var m = context.GC.Heap[genwapper.async_body];
			ASMethod g_method = ((ASMethodBody)m.Type).Method;

			Debug.Assert(!g_method.Flags.HasFlag( MethodFlags.Native) );


			ASMethodBody.MethodBodyInfo info = new ASMethodBody.MethodBodyInfo();
			g_method.Body.GetInfo(ref info);
			int calleelastpos = context.StackPosition;


			if (context.StackPosition + info.useSlots
				+ 1 //retslot
				+ 1 //_reject|_resolve|resolve  arg
				+ 1 //resolve ret
				+ 1 //then_result
				+ 1 //then_onfullfiled
				+ 1 //then_onrejected


				>= Context.STACK_LENGTH)
			{
				context.player.RaiseStackOverflow(ref error);
				return;
			}



			int stPos = context.StackPosition;
			context.StackPosition += info.useSlots;
			context.BackTraceIndex++; ;

			Span<NaNBoxing> slots = context.StackSlots.AsSpan(stPos, info.useSlots + 1);
			slots.Clear(); //栈清空 -- 防止GC时错误访问
			int P_PC;

			ReceiveError asyncErr = default;

			int retslot = context.StackPosition;
			context.StackPosition += 1;
			context.StackSlots[retslot].SetUndefined();

			((RtMethodScope)context.GC.Heap[Context.M_MethodScopePtr + context.BackTraceIndex - 1]).EmptyStackSlot();
			//if (!g_method.Flags.HasFlag(MethodFlags.Native))
			//{
				context.player.Execute(ref info, m, genwapper.async_body,
					//genwapper.scopeType,
					slots, stPos, out P_PC, ref asyncErr, retslot, calleelastpos, genwapper);
			//}
			//else
			//{
			//	context.player.SetNativeDelegate(g_method, ref asyncErr);

			//	if (!asyncErr.raised)
			//	{
			//		((NativeFun)g_method.nativefunction_delegate)(context, g_method, genwapper.async_body , genwapper.thisPtr, context.StackPosition,
			//			ref asyncErr, retslot);
			//	}
				
			//}



			if (asyncErr.raised)
			{
				if (asyncErr.error.ValueType == NaNBoxing.BoxType.Fault)
				{
					context.StackPosition = stPos;
					context.BackTraceIndex--;

					error = asyncErr;
					return;
				}

				//invoke reject
				ASMethod private_reject = context.PROMISE.Instance._vtable.Items[3].Trait.Method;
				Debug.Assert(private_reject.Name == "_reject");

				ReceiveError reject_err = default;

				StackLocater arg = default; arg.index = 0;

				context.StackSlots[context.StackPosition] = asyncErr.error;
				slots = context.StackSlots.AsSpan(context.StackPosition, 1);
				unsafe
				{
					context.StackPosition++;
					context.player.RunMethod(private_reject, promisePtr, 0,// context.PROMISE.Instance,
						1, (byte*)&arg, slots, ref reject_err, -1);
					context.StackPosition--;
				}

				context.StackPosition = stPos;
				context.BackTraceIndex--;

				if (reject_err.raised)
				{

					error = reject_err;
					error.error.setFault();
					return;
				}
			}
			else
			{
				context.BackTraceIndex--;

				if (genwapper.state == 2
					//|| g_method.Flags.HasFlag(MethodFlags.Native)
					)
				{
					ASMethod private_resolve = context.PROMISE.Instance._vtable.Items[2].Trait.Method;
					Debug.Assert(private_resolve.Name == "_resolve");

					NaNBoxing v = context.StackSlots[retslot];

					ReceiveError resolve_err = default;

					StackLocater arg = default; arg.index = 0;

					context.StackSlots[context.StackPosition] = v;
					slots = context.StackSlots.AsSpan(context.StackPosition, 1);
					unsafe
					{
						context.StackPosition++;
						context.player.RunMethod(private_resolve, promisePtr, 0,// context.PROMISE.Instance,
							1, (byte*)&arg, slots, ref resolve_err, -1);
						context.StackPosition--;
					}

					context.StackPosition = stPos;

					if (resolve_err.raised)
					{
						error = resolve_err;
						error.error.setFault();
						return;
					}

				}
				else
				{
					ASMethod static_resolve = context.PROMISE._vtable.Items[1].Trait.Method;
					Debug.Assert(static_resolve.Name == "resolve");

					NaNBoxing v = context.StackSlots[retslot];
					ReceiveError resolve_err = default;

					int resolved_promise = context.StackPosition;

					slots = context.StackSlots.AsSpan(context.StackPosition, 2);
					slots[0].SetUndefined();
					slots[1] = v;
					context.StackPosition += 2;

					StackLocater arg = default; arg.index = 1;

					unsafe
					{
						context.player.RunMethod(static_resolve, promisePtr, 0, //context.PROMISE,
							1, (byte*)&arg, slots, ref resolve_err, resolved_promise);
					}

					if (resolve_err.raised)
					{
						context.StackPosition = stPos;
						error = resolve_err;
						return;
					}
					NaNBoxing resolved = context.StackSlots[resolved_promise];
					PromiseWapper resolvedPromise = (PromiseWapper)((RtInstance)context.GC.Heap[resolved.HeapPtr]).wapperedObject;

					ASMethod private_then = context.PROMISE.Instance._vtable.Items[0].Trait.Method;
					Debug.Assert(private_then.Name == "then");

					int then_result = context.StackPosition;
					context.StackPosition++;

					int onfulfilled = Context.M_ClosurePtr + context.StackPosition;

					RtClosure onfulfilledClosure = (RtClosure)context.GC.Heap[onfulfilled];
					context.GC.Heap[onfulfilled].Type = context.MicroTaskQueue.async_then_onfulfilled.Body;
					onfulfilledClosure.This = promisePtr;
					onfulfilledClosure.ScopePtr = genwapper_ptr;
					//onfulfilledClosure.ScopeType = genwapper.scopeType;
					onfulfilledClosure._ref_as_type = context.PROMISE;
					onfulfilledClosure.methodscopeslot_ref_state = 0; onfulfilledClosure.HEAPINSTANCE_PTR = 0;


					slots = context.StackSlots.AsSpan(context.StackPosition, 2);
					slots[0].SetHeapPtr(onfulfilled, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);


					int onrejected = Context.M_ClosurePtr + context.StackPosition + 1;
					RtClosure onrejectedClosure = (RtClosure)context.GC.Heap[onrejected];
					context.GC.Heap[onrejected].Type = context.MicroTaskQueue.async_then_onrejected.Body;
					onrejectedClosure.This = promisePtr;
					onrejectedClosure.ScopePtr = genwapper_ptr;
					//onrejectedClosure.ScopeType = genwapper.scopeType;
					onrejectedClosure._ref_as_type = context.PROMISE;
					onrejectedClosure.methodscopeslot_ref_state = 0; onrejectedClosure.HEAPINSTANCE_PTR = 0;

					slots[1].SetHeapPtr(onrejected, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);

					context.StackPosition += 2;

					ReceiveError then_err = default;
					unsafe
					{
						StackLocater* passthrough = stackalloc StackLocater[2];
						passthrough->index = 0;
						(passthrough + 1)->index = 1;

						context.player.RunMethod(private_then, resolved, 0, //context.PROMISE.Instance, 
							2, (byte*)passthrough, slots, ref then_err, then_result);
					}

					context.StackPosition = stPos;

					if (then_err.raised)
					{
						then_err.error.setFault();
						error = then_err;
						return;
					}


					//throw new NotImplementedException();
				}
			}
		}


		public static void AsyncTemplate_Ctor(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var promiseInstance = context.GC.Heap[thisPtr.HeapPtr];
			var promiseFacility = (RtInstance)promiseInstance;
			var promiseWapper = (PromiseWapper)promiseFacility.wapperedObject;

			Debug.Assert(promiseWapper._state == PromiseState.pending);

			var mscope = context.GC.Heap[scope_ptr];
			var gen =  context.GC.Heap[ ((RtMethodScope)mscope).ParentPtr];
			var genwapper = (AsyncGenWapper)((RtInstance)gen).wapperedObject;

			AsyncTemplate_Step(context, ((RtMethodScope)mscope).ParentPtr, thisPtr, ref error);
		}

		
	}
}
