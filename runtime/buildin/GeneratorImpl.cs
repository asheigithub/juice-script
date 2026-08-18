using juicescript.ABC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static juicescript.runtime.Player;
using static System.Formats.Asn1.AsnWriter;

namespace juicescript.runtime.buildin
{
	internal class GeneratorImpl
	{
		internal class GeneratorWapper : RtWapperBase,Player.IResume_State
		{
			public int generator;

			/// <summary>
			/// 0 -- 刚初始化，未执行next
			/// 1 -- 已执行一次yield return,
			/// 2 -- 结束，遇到yield break ,或者运行完成
			/// 999 -- close，要求运行到结束
			/// </summary>
			public int state;
			internal NaNBoxing thisPtr;
			//internal ASContainer scopeType;

			internal Player.ExceptionContext[] exceptionContext;
			internal int exception_ctx_at;

			internal int RESUME_PC;

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
				return state == 999;
			}

			public override void OnDelete()
			{
				generator = 0;
			}

			public override void OnGCMark(Context context)
			{
				context.GC.mark(context.GC.Heap[generator]);

				if (thisPtr.ValueType == NaNBoxing.BoxType.HeapPtr)
				{
					context.GC.mark(context.GC.Heap[thisPtr.HeapPtr]);
				}

			}

			public unsafe void Resume(ExceptionContext* e_ctx, ExceptionContext** current_e_ctx, byte* PC_START, byte** PC,Span<NaNBoxing> stackslots)
			{
				if (state == 0)
					return;
				*PC = PC_START + RESUME_PC;

				for (int i = 1; i < exception_ctx_at+1; i++)
				{
					*(e_ctx + i) = exceptionContext[i];

					stackslots[exceptionContext[i].hold_error.index].setFault();

				}

				*current_e_ctx = e_ctx + exception_ctx_at;

			}
		}

		//FilePrivateNS:IIterator.generator$private::close
		[NativeFunction("FilePrivateNS:IIterator.generator$private::close")]
		public static void Generator_close(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{

			RtInstance generator_ins = (RtInstance)context.GC.Heap[thisPtr.HeapPtr];

			GeneratorWapper generatorWapper = generator_ins.wapperedObject as GeneratorWapper;
			Debug.Assert(generatorWapper !=null);

			if (generatorWapper.state == 1)
			{
				//需要正常让代码跑完
				generatorWapper.state = 999;

				var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
				var iter_ins = context.GC.Heap[thisPtr.HeapPtr];
				var iter = (RtInstance)iter_ins;

				var m = context.GC.Heap[generatorWapper.generator];

				ASMethod g_method = ((ASMethodBody)m.Type).Method;

				ASMethodBody.MethodBodyInfo info = new ASMethodBody.MethodBodyInfo();
				g_method.Body.GetInfo(ref info);

				int calleelastpos = context.StackPosition;

				int stPos = context.StackPosition;
				context.StackPosition += info.useSlots;

				context.BackTraceIndex++; ;
				((RtMethodScope)context.GC.Heap[Context.M_MethodScopePtr + context.BackTraceIndex - 1]).EmptyStackSlot();

				Span<NaNBoxing> slots = context.StackSlots.AsSpan(stPos, info.useSlots);
				slots.Clear(); //栈清空 -- 防止GC时错误访问
				int P_PC;
				context.player.Execute(ref info, m,  generatorWapper.generator,
					//generatorWapper.scopeType, 
					slots, stPos, out P_PC, ref error, returnSlotIndex, calleelastpos, generatorWapper);

				context.BackTraceIndex--;

				context.StackPosition -= info.useSlots;

				if (!error.raised)
				{

				}
				else
				{
					//记录当前报错堆栈，看上级调用是否处理这个错误
					context.errorStack.AddTrace(g_method, P_PC);

				}
			}
		}

		[NativeFunction("FilePrivateNS:IIterator.generator$private::next")]
		public static void Generator_next(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{

			//RtPayloadInstance generator_ins = (RtPayloadInstance)context.GC.Heap[thisPtr.HeapPtr];

			//GeneratorWapper generatorWapper = generator_ins.wapperedObject as GeneratorWapper;
			//Debug.Assert(generatorWapper != null);

			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
			var iter_ins = context.GC.Heap[thisPtr.HeapPtr];
			var iter = (RtInstance)iter_ins;

			var _result = scope.ReadSlot(1, context.player);
			var _obj = scope.ReadSlot(0, context.player);


			var result_ins = context.GC.Heap[_result.HeapPtr];
			var result = (RtInstance)result_ins;
			var obj_ins = context.GC.Heap[_obj.HeapPtr];

			Debug.Assert(_obj.Raw == thisPtr.Raw);

			RtInstance generator_ins = (RtInstance)obj_ins;
			GeneratorWapper generatorWapper = generator_ins.wapperedObject as GeneratorWapper;
			Debug.Assert(generatorWapper != null);

			var m = context.GC.Heap[generatorWapper.generator];
			

			ASMethod g_method = ((ASMethodBody)m.Type).Method;

			ASMethodBody.MethodBodyInfo info = new ASMethodBody.MethodBodyInfo();
			g_method.Body.GetInfo(ref info);

			int calleelastpos = context.StackPosition;
			
			int stPos = context.StackPosition;
			context.StackPosition += info.useSlots;

		
			context.BackTraceIndex++; ;

			((RtMethodScope)context.GC.Heap[Context.M_MethodScopePtr + context.BackTraceIndex - 1]).EmptyStackSlot();

			Span<NaNBoxing> slots = context.StackSlots.AsSpan(stPos, info.useSlots);
			slots.Clear(); //栈清空 -- 防止GC时错误访问
			int P_PC;
			context.player.Execute(ref info, m,  generatorWapper.generator  ,
				//generatorWapper.scopeType, 
				slots, stPos, out P_PC, ref error, returnSlotIndex, calleelastpos,generatorWapper);

			context.BackTraceIndex--;
			
			context.StackPosition -= info.useSlots;
			
			if (!error.raised)
			{

				NaNBoxing done = default;
				if (generatorWapper.state == 1)
				{
					done.SetBoolean(false);

					NaNBoxing key = context.StackSlots[returnSlotIndex];

					result.SetSlot(key, 1, context.player);
					result.SetSlot(key, 2, context.player);

				}
				else
				{
					done.SetBoolean(true);
				}
				result.SetSlot(done, 0, context.player);

			}
			else
			{
				//记录当前报错堆栈，看上级调用是否处理这个错误
				context.errorStack.AddTrace(g_method, P_PC);

			}



			
		}



	}
}
