using juicescript.ABC;
using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static juicescript.runtime.Player;

namespace juicescript.runtime.buildin
{
	internal class TopLevel
	{
		


		[NativeFunction("$__AS3__.toplevel$public::isNaN")]
		public static void TopLevel_IsNaN(Context context,
			ASMethod method,
			int scope_ptr, 
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtPayloadMethodScope)context.GC.Heap[scope_ptr].facility;
			var arg0 = scope.ReadSlot(0, context.player);

#if DEBUG
			if (arg0.ValueType != NaNBoxing.BoxType.Number)
				throw new InvalidOperationException();
#endif


			context.StackSlots[returnSlotIndex].SetBoolean( double.IsNaN(arg0.Number) );
		}

		[NativeFunction("$__AS3__.toplevel$public::isFinite")]
		public static void TopLevel_IsFinite(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtPayloadMethodScope)context.GC.Heap[scope_ptr].facility;
			var arg0 = scope.ReadSlot(0, context.player);

#if DEBUG
			if (arg0.ValueType != NaNBoxing.BoxType.Number)
				throw new InvalidOperationException();
#endif


			context.StackSlots[returnSlotIndex].SetBoolean(double.IsFinite(arg0.Number));
		}



		private static void WritePrimitive(NaNBoxing arg, IPrint printer,Context context)
		{
			switch (arg.ValueType)
			{
				case NaNBoxing.BoxType.Number:
					double d = arg.Number;
					if (double.IsNaN(d)) { printer.Write("NaN"); return; }
					if (double.IsPositiveInfinity(d)) { printer.Write("Infinity"); return; }
					if (double.IsNegativeInfinity(d)) { printer.Write("-Infinity"); return; }
					if (d == 0 && double.IsNegative(d)) { printer.Write("-0"); return; } // 需要你提供 IsNegative(0.0)
					printer.Write(d.ToString(System.Globalization.CultureInfo.InvariantCulture));
					return;
				case NaNBoxing.BoxType.Undefined: printer.Write("undefined"); return;
				case NaNBoxing.BoxType.Null: printer.Write("null"); return;
				case NaNBoxing.BoxType.Boolean: printer.Write(arg.Boolean ? "true" : "false"); return;
				case NaNBoxing.BoxType.Int:
					printer.Write(arg.IntValue.ToString(System.Globalization.CultureInfo.InvariantCulture)); return;
				case NaNBoxing.BoxType.Uint:
					printer.Write(arg.UIntValue.ToString(System.Globalization.CultureInfo.InvariantCulture)); return;
				case NaNBoxing.BoxType.Sbyte:
					printer.Write(arg.SByteValue.ToString(System.Globalization.CultureInfo.InvariantCulture)); return;
				case NaNBoxing.BoxType.Byte:
					printer.Write(arg.ByteValue.ToString(System.Globalization.CultureInfo.InvariantCulture)); return;
				case NaNBoxing.BoxType.Short:
					printer.Write(arg.ShortValue.ToString(System.Globalization.CultureInfo.InvariantCulture)); return;
				case NaNBoxing.BoxType.UShort:
					printer.Write(arg.UShortValue.ToString(System.Globalization.CultureInfo.InvariantCulture)); return;
				case NaNBoxing.BoxType.Float:
					printer.Write(arg.FloatValue.ToString(System.Globalization.CultureInfo.InvariantCulture)); return;
				case NaNBoxing.BoxType.HeapPtr:
					printer.Write(((RtPayloadString)context.GC.Heap[arg.HeapPtr].facility).Str );return;
				case NaNBoxing.BoxType.LocalString:
					{
						Span<char> chars = stackalloc char[16]; // 5个UTF-8字节最多能解码出的字符数
						int charCount = arg.GetLocalStringChars(chars);
						if (charCount > 0)
						{
							//printer.Write(new string(chars.Slice(0, charCount)));
							printer.Write(chars.Slice(0, charCount));
						}
						return;
					}
			}
		}

		internal static void TraceElement(NaNBoxing arg,Context context, int stackStPos, ref ReceiveError error, int scope_ptr , NaNBoxing callee_bindthis ,IPrint printer)
		{
		lbl_retry:
			// 快路径：原始值
			if (context.player.IsPrimitive(arg))
			{
				WritePrimitive(arg, printer,context);
				return;
			}
#if DEBUG
			if (arg.ValueType == NaNBoxing.BoxType.Fault)
			{
				throw new InvalidOperationException();
			}
			else
#endif
			{
				var instance = context.GC.Heap[arg.HeapPtr];
				switch (instance.TypeKind)
				{
					case RtHeapTypeKind.CLASS:
						printer.Write($"[class {((RtPayloadScriptClass)instance.facility).Meta.QName.Name}]");
						break;
					case RtHeapTypeKind.GLOBAL:
						printer.Write("[object global]");
						break;
					case RtHeapTypeKind.STRING:
						printer.Write(((RtPayloadString)instance.facility).Str);
						break;
					case RtHeapTypeKind.INSTANCE:
						if (scope_ptr == 0)
						{
							printer.Write($"[object {instance.Type.QName.Name}]");
						}
						else
						{
							string mode = "toString";
							bool first = true;
						lbl_toprimitive:

							//查找是否有toString,如果有，调用它
							if (context.StackPosition + 1 >= Context.STACK_LENGTH)
							{
								context.player.RaiseStackOverflow(ref error);
								return;
							}
							var stackslots = context.StackSlots.AsSpan(context.StackPosition, 2); stackslots.Clear();
							var stPos = context.StackPosition;
							context.StackPosition += 2;

							var ns_set = context.GC.Heap[scope_ptr].Type._link_codescope.NamespaceSet;
							ASContainer as_type = instance.Type;
							int code = context.player.MultiNameLSearch(ns_set, instance.TypeKind,
								as_type, mode, new StackLocater() { index = 0 }, stackslots, stPos, arg, context.player.check_MultiNameLSearch_issameorinherit(arg, callee_bindthis.ValueType == NaNBoxing.BoxType.HeapPtr ? (context.GC.Heap[callee_bindthis.HeapPtr]) : null)  , ref error, true);
							switch (code)
							{
								case 0:
									break;
								case 1:
									//有异常产生
									context.StackPosition -= 2;
									return;
								case 2:
									context.StackPosition -= 2;
									context.GC.CheckGC(ref error);
									context.player.RaiseTypeError_Ambiguous(ref error, mode);
									return;
								default:
									throw new InvalidOperationException();
							}
							NaNBoxing fun = context.player.LoadValue(stackslots[0], -1, ref error, stackslots, stPos);
							if (error.raised) //由于object原型的存在，这里是肯定能找到的。找不到就报错吧
							{
								context.StackPosition -= 2;
								return;
							}
							if (fun.ValueType != NaNBoxing.BoxType.HeapPtr)
							{
								context.StackPosition -= 2;
								context.player.RaiseTypeError(ref error, fun, TypeKind.Function);
								return;
							}
							var funinstance = context.GC.Heap[fun.HeapPtr];
							if (funinstance.TypeKind != RtHeapTypeKind.CLOSURE)
							{
								context.StackPosition -= 2;
								context.player.RaiseTypeError(ref error, fun, TypeKind.Function);
								return;
							}
							if (((ASMethodBody)funinstance.Type).Method.Container == context.OBJECT._link_codescope.Parent.Container)
							{
								context.StackPosition -= 2;

								if (Extensions.IsExtend((ASInstance)instance.Type, context.ERROR.Instance))
								{
									printer.Write(instance.Type.QName.Name);
									printer.Write(": ");
									var msg = ((RtPayloadInstance)instance.facility).ReadSlot(0, instance.Type._link_codescope, context.player);
									//TraceElement(msg, context, stackStPos, ref error, scope_ptr, callee_bindthis, printer);
									arg = msg;
									goto lbl_retry;
								}
								else
								{
									printer.Write($"[object {instance.Type.QName.Name}]");
								}
							}
							else
							{
								//invoke_it
								unsafe
								{
									NaNBoxing conv = context.player.RunMethod(((ASMethodBody)funinstance.Type).Method,
										arg, ((RtPayloadClosure)funinstance.facility).ScopePtr, ((RtPayloadClosure)funinstance.facility).ScopeType, 0, null, null, ref error, stPos + 1, fun.HeapPtr);
									context.StackPosition -= 2;
									if (error.raised)
									{
										return;
									}

									if (context.player.IsPrimitive(conv))
									{
										scope_ptr = 0;
										arg = conv;
										goto lbl_retry;
									}
									else if (!first)
									{
										context.player.RaiseTypeError_ConvertToPrimitive(ref error, arg);
										return;
									}
									else
									{
										mode = "valueOf";
										first = false;
										goto lbl_toprimitive;
									}


								}


							}

						}
						break;
					case RtHeapTypeKind.NAMESPACE:
						ASNamespace ns = ((RtPayloadNameSpace)instance.facility).ASNamespace;
						printer.Write(string.IsNullOrEmpty(ns.def_uri) ? ns.Name : ns.def_uri);
						break;

					case RtHeapTypeKind.CLOSURE:
						printer.Write("function Function() {}");
						break;
					case RtHeapTypeKind.ARRAY:

						if (context.BackTraceIndex >= Context.MAX_BACKTRACE)
						{
							printer.WriteLine(string.Empty);
							context.player.RaiseStackOverflow(ref error);
							return;
						}
						context.BackTraceIndex++;
						((RtPayloadMethodScope)context.GC.Heap[context.M_MethodScopePtr + context.BackTraceIndex - 1].facility).EmptyStackSlot();
						((RtPayloadArray)instance.facility).Trace(context, stackStPos, ref error, scope_ptr, printer,instance);
						context.BackTraceIndex--;
						if (error.raised)
						{
							return;
						}
						break;
					case RtHeapTypeKind.VECTOR:
						//printer.Write($"[object .__AS3__.vec.vector<{((RtPayloadVector)instance.facility).element_asclass.QName.ToDebugTypeName()}>]");
						if (context.BackTraceIndex >= Context.MAX_BACKTRACE)
						{
							printer.WriteLine(string.Empty);
							context.player.RaiseStackOverflow(ref error);
							return;
						}

						
						RtPayloadVector vector = (RtPayloadVector)instance.facility;	
						context.BackTraceIndex++;
						((RtPayloadMethodScope)context.GC.Heap[context.M_MethodScopePtr + context.BackTraceIndex - 1].facility).EmptyStackSlot();
						vector.Trace(context, stackStPos, ref error, scope_ptr, printer);
						context.BackTraceIndex--;
						if (error.raised)
						{
							return;
						}

						break;
					case RtHeapTypeKind.STACK_CACHE_OBJ:
					case RtHeapTypeKind.DYNAMIC_PROPERTYS:
					case RtHeapTypeKind.SHAPE:
					case RtHeapTypeKind.MethodScope:
					default:
						throw new InvalidOperationException();
				}

			}


		}


		[NativeFunction("$__AS3__.toplevel$public::trace")]
		public static void Trace(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtPayloadMethodScope)context.GC.Heap[scope_ptr].facility;
			context.StackSlots[returnSlotIndex].SetUndefined();

			var rest = scope.ReadSlot(0, context.player);
			var rest_array = (RtPayloadArray)context.GC.Heap[rest.HeapPtr].facility;

#if DEBUG
			if (rest_array.StoreMode != RtPayloadArray.ArrayStoreMode.cache_on_stack)
				throw new InvalidOperationException();
#endif
			
			var arguments = rest_array.stack_store.Span;

			for ( var i = 0; i < arguments.Length; i++)
			{
				var arg = arguments[i];

				TraceElement(arg, context, stackStPos, ref error, scope_ptr ,thisPtr ,context.player.Print);
				if (error.raised)
				{
					return;
				}


				if (i < arguments.Length - 1)
				{
					context.player.Print.Write(" ");
				}
				else
				{
					context.player.Print.WriteLine(string.Empty);
				}

			}
		
		}


		[NativeFunction("$__AS3__.toplevel$public::fetch")]
		public static void HttpFetch(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			RtHeapInstance promise;
			int ptr = context.MicroTaskQueue.CreateNativePromise( context, out promise);
			if (ptr == 0)
			{
				context.player.RaiseOutOfMemory(ref error);
				return;
			}

			var scope = (RtPayloadMethodScope)context.GC.Heap[scope_ptr].facility;
			NaNBoxing url = scope.ReadSlot(0, context.player);
			if (url.ValueType == NaNBoxing.BoxType.Null || url.ValueType == NaNBoxing.BoxType.Undefined)
			{
				context.player.RaiseError(ref error, "url is null");
				return;
			}


			string uri;
			if (url.ValueType == NaNBoxing.BoxType.LocalString)
			{
				Span<char> buffers = stackalloc char[16];
				int l = url.GetLocalStringChars(buffers);
				uri = buffers.Slice(0, l).ToString();
			}
			else
			{
				Debug.Assert(url.ValueType == NaNBoxing.BoxType.HeapPtr);
				uri = ((RtPayloadString)context.GC.Heap[url.HeapPtr].facility).Str;
			}

			
			HttpClient httpClient = new HttpClient();
			context.AsyncCallbackQueue.OnAsyncBegin(ptr);

			try
			{
				
				httpClient.GetAsync(uri).ContinueWith(

					task =>
					{
						// 这在线程池线程执行
						if (task.IsFaulted)
						{
							//result = CreateError(context, task.Exception);
							// 调度 reject
							//context.Player.OnAsyncComplete(promisePtr, result, isReject: true);
							context.AsyncCallbackQueue.OnAsyncComplete(ptr,
										(AysncGetResult r) => {
											context.player.TryCreateStringValue(task.Exception.Message, out r.value, ref r.error);
										}
										, false);
							httpClient.Dispose();
						}
						else
						{
							try
							{
								task.Result.Content.ReadAsStringAsync().ContinueWith(
								str =>
								{
									if (str.IsFaulted)
									{
										context.AsyncCallbackQueue.OnAsyncComplete(ptr,
										(AysncGetResult r) => {
											context.player.TryCreateStringValue(str.Exception.Message, out r.value, ref r.error);
										}
										, false);
									}
									else
									{

										context.AsyncCallbackQueue.OnAsyncComplete(ptr,
										(AysncGetResult r) => {
											context.player.TryCreateStringValue(str.Result, out r.value, ref r.error);
										}
										, true);

									}

									httpClient.Dispose();
								}
							);
							}
							catch (AggregateException ex)
							{
								httpClient.Dispose();

								context.AsyncCallbackQueue.OnAsyncComplete(ptr,
										(AysncGetResult r) => {
											context.player.TryCreateStringValue(ex.Message, out r.value, ref r.error);
										}
										, false);

							}

							
						}
					}
					);
			}
			catch (Exception ex)
			{
				context.AsyncCallbackQueue.OnAsyncComplete(ptr,
					(AysncGetResult r) => {
						context.player.TryCreateStringValue(ex.Message, out r.value, ref r.error);
					}
					, false);
				httpClient.Dispose();
			}

			context.StackSlots[returnSlotIndex].SetHeapPtr(ptr);

		}

	}
}
