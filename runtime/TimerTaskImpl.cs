using juicescript.ABC;
using juicescript.ABC.Locaters;
using System.Diagnostics;
using static juicescript.runtime.Player;

namespace juicescript.runtime
{
	internal class TimerTaskImpl
	{
		public struct TimerTask
		{
			public uint ID;
			public long TimeToRun;
			public int CallbackFunctionPtr;
			public int argumentsPtr;
			public int interval;
		}


		public class TimerTaskList
		{
			
			private TimerTask[] _taskBuffer;
			private int _count = 0;
			private const int DefaultCapacity = 64;

			public TimerTaskList()
			{
				_taskBuffer = new TimerTask[DefaultCapacity];
			}

			private uint seed = 1;

			public uint Insert(TimerTask task)
			{
				if (_count >= _taskBuffer.Length)
				{
					// 扩容
					Array.Resize(ref _taskBuffer, _taskBuffer.Length * 2);
				}

				task.ID = seed++;
				_taskBuffer[_count] = task;
				_count++;

				return task.ID;

			}

			public bool HasWaitingTasks => _count > 0;

			public void Clear()
			{
				_count = 0;
			}

			internal void OnGCMark(Context context)
			{

				var c = _count;
				while (c > 0)
				{
					var task = _taskBuffer[c - 1];
					c--;
					if (task.CallbackFunctionPtr != 0)
					{
						context.GC.mark(context.GC.Heap[task.CallbackFunctionPtr]);
					}

					if (task.argumentsPtr != 0)
					{
						context.GC.mark(context.GC.Heap[task.argumentsPtr]);
					}

				}
			}


			internal unsafe void RunTimerTasks(Context context, long nowticks, Action<PlayerException> onErrorRaised, ref ReceiveError task_fault)
			{
				StackLocater* args = stackalloc StackLocater[16];

				for (int i = 0; i < _count; i++)
				{
					var task = _taskBuffer[i];
					Debug.Assert(task.CallbackFunctionPtr != 0);

					if (task.TimeToRun < nowticks)
					{
						
						if (task.interval !=0)
						{
							_taskBuffer[i].TimeToRun += task.interval;
						}
						else
						{
							//将后面的task移动覆盖当前task
							//如果后面没有，正好将callbackfunctionptr清0.

							_taskBuffer[i].CallbackFunctionPtr = 0;
							_taskBuffer[i].argumentsPtr = 0;

							int j = i + 1;
							while (j < _count)
							{
								_taskBuffer[j - 1] = _taskBuffer[j];
								j++;
							}

							_count--;
							i--;

						}
						//运行函数
						var closureinstance = context.GC.Heap[task.CallbackFunctionPtr];

						Debug.Assert(closureinstance.TypeKind == RtHeapTypeKind.CLOSURE);

						var callmethod = ((ASMethodBody)closureinstance.Type).Method;


						int len; RtArray argArray = (RtArray)context.GC.Heap[task.argumentsPtr];

						len = (int)argArray.GetLength(context.player);

						if (context.StackPosition + len + 1 >= Context.STACK_LENGTH)
						{
							ReceiveError err = default;
							context.player.RaiseStackOverflow(ref err);



							PlayerException ex = new PlayerException(context.player, err.error, context.errorStack.ToString());
							context.errorStack.Clear();

							if (onErrorRaised != null)
							{
								onErrorRaised(ex);
							}

							if (err.error.ValueType == NaNBoxing.BoxType.Fault)
							{
								task_fault = err;
								Clear();
								return;
							}

							continue;
						}

						int returnslot = context.StackPosition;
						context.StackSlots[context.StackPosition].SetUndefined();

						context.StackPosition += 1;

						for (int k = 0; k < len; k++)
						{
							bool isoutofindex;
							context.StackSlots[context.StackPosition + k] = argArray.ReadSlot((uint)k, context.player, out isoutofindex);
							(args + k)->index = k;
						}

						var slots = context.StackSlots.AsSpan(context.StackPosition, len);

						context.StackPosition += len ;

						ReceiveError error = default;

						context.player.RunMethod(callmethod, ((RtClosure)closureinstance).This,
							((RtClosure)closureinstance).ScopePtr,
							((RtClosure)closureinstance).ScopeType,
							(ushort)len, (byte*)args,
							slots,
							ref error,
							returnslot
							);

						context.StackPosition -= len + 1;


						if (error.raised)
						{
							PlayerException ex = new PlayerException(context.player, error.error, context.errorStack.ToString());
							context.errorStack.Clear();

							if (onErrorRaised != null)
							{
								onErrorRaised(ex);
							}

							if (error.error.ValueType == NaNBoxing.BoxType.Fault)
							{
								task_fault = error;

								Clear();

								return;
							}

						}




					}

				}


			}

			internal void clearTimeOut(uint id)
			{
				
				for (int i = 0; i < _count; i++)
				{
					var task = _taskBuffer[i];
					Debug.Assert(task.CallbackFunctionPtr != 0);
					if (task.ID == id)
					{
						
						
						//移除任务
						//将后面的task移动覆盖当前task
						//如果后面没有，正好将callbackfunctionptr清0.
						_taskBuffer[i].CallbackFunctionPtr = 0;
						_taskBuffer[i].argumentsPtr = 0;

						int j = i + 1;
						while (j < _count)
						{
							_taskBuffer[j - 1] = _taskBuffer[j];
							j++;
						}

						--_count;

						
						break;
					}
				}

			}
		}

		[NativeFunction("$__AS3__.toplevel$public::clearTimeout")]
		public static void TimerTask_clearTimeout(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
			var id = scope.ReadSlot(0, context.player).UIntValue;

			context.TimerTaskQueue.clearTimeOut(id);

		}


		[NativeFunction("$__AS3__.toplevel$public::clearInterval")]
		public static void TimerTask_clearInterval(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
			var id = scope.ReadSlot(0, context.player).UIntValue;

			context.TimerTaskQueue.clearTimeOut(id);

		}



		[NativeFunction("$__AS3__.toplevel$public::setTimeout")]
		public static void TimerTask_setTimeout(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			if (context.StackPosition + 1 >= Context.STACK_LENGTH)
			{
				context.player.RaiseStackOverflow(ref error);
				return;
			}

			int basePos = context.StackPosition;


			var closure = scope.ReadSlot(0, context.player);
			var delay = scope.ReadSlot(1, context.player);
			var rest = scope.ReadSlot(2, context.player);

			Debug.Assert(rest.ValueType == NaNBoxing.BoxType.HeapPtr);

			var restArray = (RtArray)context.GC.Heap[rest.HeapPtr];
			if (restArray.GetLength(context.player) > 16)
			{
				context.player.RaiseError(ref error, "setTimeout(closure:Function, delay:Number, ... arguments),arguments.length must less 16.");
				return;
			}

			NaNBoxing heap_closure = context.player.GetSaveValue(closure, ref error);
			if (error.raised)
			{
				return;
			}
			context.StackSlots[context.StackPosition] = heap_closure;
			context.StackPosition += 1;

			int argumentPtr = restArray.ChangeStoreToHeap(context.player, ref error);
			if (error.raised)
			{
				context.StackPosition = basePos;
				return;
			}

			TimerTask task = new TimerTask();
			task.argumentsPtr = argumentPtr;
			task.CallbackFunctionPtr = heap_closure.HeapPtr;
			task.TimeToRun = DateTime.UtcNow.Ticks + (int)(delay.Number * 10000);
			task.interval = 0;

			uint id = context.TimerTaskQueue.Insert(task);

			context.StackSlots[returnSlotIndex].SetUInt(id);

			context.StackPosition = basePos;




		}



		//
		[NativeFunction("$__AS3__.toplevel$public::setInterval")]
		public static void TimerTask_setInterval(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			if (context.StackPosition + 1 >= Context.STACK_LENGTH)
			{
				context.player.RaiseStackOverflow(ref error);
				return;
			}

			int basePos = context.StackPosition;


			var closure = scope.ReadSlot(0, context.player);
			var delay = scope.ReadSlot(1, context.player);
			var rest = scope.ReadSlot(2, context.player);

			Debug.Assert(rest.ValueType == NaNBoxing.BoxType.HeapPtr);

			var restArray = (RtArray)context.GC.Heap[rest.HeapPtr];
			if (restArray.GetLength(context.player) > 16)
			{
				context.player.RaiseError(ref error, "setTimeout(closure:Function, delay:Number, ... arguments),arguments.length must less 16.");
				return;
			}

			NaNBoxing heap_closure = context.player.GetSaveValue(closure, ref error);
			if (error.raised)
			{
				return;
			}
			context.StackSlots[context.StackPosition] = heap_closure;
			context.StackPosition += 1;

			int argumentPtr = restArray.ChangeStoreToHeap(context.player, ref error);
			if (error.raised)
			{
				context.StackPosition = basePos;
				return;
			}

			TimerTask task = new TimerTask();
			task.argumentsPtr = argumentPtr;
			task.CallbackFunctionPtr = heap_closure.HeapPtr;
			task.TimeToRun = DateTime.UtcNow.Ticks + (int)(delay.Number * 10000);
			task.interval = (int)(delay.Number * 10000);

			uint id = context.TimerTaskQueue.Insert(task);

			context.StackSlots[returnSlotIndex].SetUInt(id);

			context.StackPosition = basePos;




		}




	}
}
