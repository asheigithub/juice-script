using juicescript.runtime.buildin;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static juicescript.runtime.buildin.PromiseImpl;

namespace juicescript.runtime
{
	public partial class Player
	{
		public class AsyncCallbackQueue
		{
			// 线程安全的就绪队列
			private ConcurrentQueue<PendingCallback> _readyQueue = new();

			private int mainthreadid = Thread.CurrentThread.ManagedThreadId;

			// 事件，用于异步完成时通知
			internal AutoResetEvent _wakeEvent = new(false);

			private int _backtasks = 0;

			private HashSet<int> holdpromises = new HashSet<int>();

			internal void OnGCMark(Context context)
			{
				foreach (var pid in holdpromises)
				{
					context.GC.mark(context.GC.Heap[pid]);
				}
			}


			// C#异步回调调用这个方法
			public void OnAsyncComplete(int promisePtr, Action<AysncGetResult> get_result,bool resolve_or_reject)
			{
				_readyQueue.Enqueue(new PendingCallback
				{
					PromisePtr = promisePtr,
					get_value = get_result,
					resolve_or_reject = resolve_or_reject
					
				});

				Interlocked.MemoryBarrier();

				Interlocked.Decrement(ref _backtasks);

				_wakeEvent.Set();  // 立即通知
			}

			// C# native promise函数在异步正式开始前，加一个计数
			public void OnAsyncBegin(int promisePtr)
			{
				Debug.Assert(Thread.CurrentThread.ManagedThreadId == mainthreadid);

				holdpromises.Add(promisePtr);
				Interlocked.Increment(ref _backtasks);
			}

			public bool HasPending
			{
				get
				{ 
					return !_readyQueue.IsEmpty || Interlocked.CompareExchange(ref _backtasks,0,0) != 0 ;
				}
			}


			// 主线程调用：尝试处理一个回调
			private bool TryDequeue(out PendingCallback callback)
			{
				return _readyQueue.TryDequeue(out callback);
			}


			private AysncGetResult r = new AysncGetResult();
			internal void RunQueue(Context context,ref ReceiveError queueError)
			{
				Debug.Assert(Thread.CurrentThread.ManagedThreadId == mainthreadid);

				
				PendingCallback callback;
				while (TryDequeue(out callback))
				{
					RtHeapInstance promise_instance = context.GC.Heap[callback.PromisePtr];

					r.value = default;
					r.error = default;

					callback.get_value(r);

					if (callback.resolve_or_reject && !r.error.raised)
					{
						//resolve
						PromiseWapper promise = (PromiseWapper)((RtPayloadInstance)promise_instance.facility).wapperedObject;
						promise.FulFill(context, r.value);

					}
					else if (r.error.raised)
					{
						if (r.error.error.ValueType == NaNBoxing.BoxType.Fault)
						{
							queueError = r.error;
							return;
						}

						context.errorStack.Clear();

						PromiseWapper promise = (PromiseWapper)((RtPayloadInstance)promise_instance.facility).wapperedObject;
						promise.Reject(context, r.error.error);
					}
					else
					{
						//reject
						PromiseWapper promise = (PromiseWapper)((RtPayloadInstance)promise_instance.facility).wapperedObject;
						promise.Reject(context, r.value);

					}

					holdpromises.Remove(callback.PromisePtr);
				}
			}
		}
		public struct PendingCallback
		{
			public int PromisePtr;
			public Action<AysncGetResult> get_value;
			public bool resolve_or_reject;
		}

		public class AysncGetResult
		{
			public NaNBoxing value;
			public ReceiveError error;
		}

	}
}
