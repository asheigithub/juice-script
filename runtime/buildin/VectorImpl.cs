using juicescript.ABC;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static juicescript.NaNBoxing;
using static juicescript.runtime.Player;
using static System.Formats.Asn1.AsnWriter;

namespace juicescript.runtime.buildin
{
	internal class VectorImpl
	{
		[NativeFunction("__AS3__.vec$Vector@Vector")]
		public static void Vector(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtPayloadMethodScope)context.GC.Heap[scope_ptr].facility;
			var vecinstance = context.GC.Heap[thisPtr.HeapPtr];
			//var rest = scope.ReadSlot(0, context.player);


			int a_ptr = stackStPos 
									- 2; /*
									      * arguments
									      * callee
									      */
			NaNBoxing arguments = context.StackSlots[a_ptr];

			var rest_array = (RtPayloadArray)context.GC.Heap[arguments.HeapPtr].facility;

			if (rest_array.StoreMode != RtPayloadArray.ArrayStoreMode.cache_on_stack)
				throw new InvalidOperationException();

			var vector = (RtPayloadVector)vecinstance.facility;

			int element_size = VectorStore.GetElementSize(vector.element_type, vector.element_asclass);

			var rest_span = rest_array.stack_store.Span;

			if (rest_span.Length > 2)
			{				
				rest_span = rest_span.Slice(2);  //initData.				

				goto flag_initdata;
			}

			uint initLen = 0;
			bool isfixed = false;

			initLen = scope.ReadSlot(0, context.player).UIntValue;
			isfixed = scope.ReadSlot(1, context.player).Boolean;

			if (initLen > int.MaxValue)
			{
				context.player.RaiseRangeError(ref error, initLen.ToString(), int.MaxValue);
				return;
			}


			if (context.GC.MemUsage + element_size * initLen > context.GC.USAGE_LIMIT)
			{
				context.player.RaiseOutOfMemory(ref error);
				return;
			}

			if (element_size * initLen > RtPayloadVector.MAX_CACHE_SIZE) //超出缓存限制，要保存到堆
			{
				vector.ChangeStoreToHeap((ASInstance)vecinstance.Type, context.player, ref error);
				if (error.raised)
				{
					return;
				}

				var store = vector.GetStore(context.player);
				store.SetBuffer(element_size * (int)initLen);
				store.length = (int)initLen;
				store.elementSize = element_size;
				store.SetDefault(vector.element_type, vector.element_asclass,0, (int)initLen);

				return;
				//throw new NotImplementedException();
			}
			else
			{
				var store = vector.GetStore(context.player);
				store.SetBuffer(element_size * (int)initLen);
				store.length = (int)initLen;
				store.elementSize = element_size;
				store.SetDefault(vector.element_type, vector.element_asclass, 0, (int)initLen);

				//vector.SetStore(new VectorStore(vector.element_type, vector.element_asclass, (int)initLen, isfixed));
				return;
			}
		flag_initdata:
			;

			initLen = (uint)rest_span.Length;
			isfixed = false;

			for (int i = 0; i < rest_span.Length; i++) 
			{
				context.player.ConvertValueType(ref error, rest_span[i], vector.element_type, vector.element_asclass , ref rest_span[i],scope_ptr,thisPtr);
				if (error.raised)
				{
					return;
				}

				if (vector.element_asclass != null && vector.element_asclass.Instance.Flags.HasFlag(ClassFlags.Struct))
				{
					//Struct类型，无需实例化到堆，需要拷贝内存到Vector里。
					//如果是 Vector<*>,Vector<Object>这种保存结构体，那么它需要被装箱成堆对象。
				}
				else
				{
					rest_span[i] = context.player.GetSaveValue(rest_span[i], ref error);
					if (error.raised)
					{
						return;
					}
				}
			}

			if (element_size * initLen > RtPayloadVector.MAX_CACHE_SIZE) //超出缓存限制，要保存到堆
			{
				if (context.GC.MemUsage + element_size * initLen > context.GC.USAGE_LIMIT)
				{
					context.player.RaiseOutOfMemory(ref error);
					return;
				}

				vector.ChangeStoreToHeap((ASInstance)vecinstance.Type, context.player, ref error);
				if (error.raised)
				{
					return;
				}

				var store = vector.GetStore(context.player);
				store.SetBuffer(element_size * (int)initLen);
				store.length = (int)initLen;
				store.elementSize = element_size;
				//store.SetDefault(vector.element_type, vector.element_asclass, (int)initLen);

				store.CopySpan(vector.element_type, vector.element_asclass, rest_span, context);
			}
			else
			{
				var store = vector.GetStore(context.player);
				store.SetBuffer(element_size * (int)initLen);
				store.length = (int)initLen;
				store.elementSize = element_size;
				store.CopySpan(vector.element_type, vector.element_asclass, rest_span, context);
				//vector.SetStore(new VectorStore(vector.element_type, vector.element_asclass, rest_span, context));
			}
		}

		//__AS3__.vec$Vector@set#fixed
		[NativeFunction("__AS3__.vec$Vector@set#fixed")]
		public static void Vector_set_fixed(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtPayloadMethodScope)context.GC.Heap[scope_ptr].facility;
			var vecinstance = context.GC.Heap[thisPtr.HeapPtr];

			var isfixed = scope.ReadSlot(0, context.player);
			Debug.Assert(isfixed.ValueType == NaNBoxing.BoxType.Boolean);

			((RtPayloadVector)vecinstance.facility).GetStore(context.player).isFixed = isfixed.Boolean;
			
		}
		//__AS3__.vec$Vector@get#fixed
		[NativeFunction("__AS3__.vec$Vector@get#fixed")]
		public static void Vector_get_fixed(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtPayloadMethodScope)context.GC.Heap[scope_ptr].facility;
			var vecinstance = context.GC.Heap[thisPtr.HeapPtr];
		    context.StackSlots[returnSlotIndex].SetBoolean(  	((RtPayloadVector)vecinstance.facility).GetStore(context.player).isFixed );

		}

		//function __AS3__.vec$Vector@length
		[NativeFunction("__AS3__.vec$Vector@get#length")]
		public static void Vector_get_length(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtPayloadMethodScope)context.GC.Heap[scope_ptr].facility;
			var vecinstance = context.GC.Heap[thisPtr.HeapPtr];


			int len = ((RtPayloadVector)vecinstance.facility).GetStore(context.player).length;
			context.StackSlots[returnSlotIndex].SetInt(len);
		}
		//__AS3__.vec$Vector@set#length
		[NativeFunction("__AS3__.vec$Vector@set#length")]
		public static void Vector_set_length(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtPayloadMethodScope)context.GC.Heap[scope_ptr].facility;
			var vecinstance = context.GC.Heap[thisPtr.HeapPtr];

			NaNBoxing newlen = scope.ReadSlot(0, context.player);
#if DEBUG
			if (newlen.ValueType != NaNBoxing.BoxType.Int)
			{
				throw new InvalidOperationException();
			}
#endif
			if (newlen.IntValue < 0)
			{
				context.player.RaiseRangeError(ref error, ((uint)newlen.IntValue).ToString(), int.MaxValue);
				return;
			}

			((RtPayloadVector)vecinstance.facility).Resize(newlen.IntValue, ref error, context.player,(ASInstance)vecinstance.Type);
				//throw new NotImplementedException();
			
		}


		
		[NativeFunction("__AS3__.vec$Vector@concat")]
		public static void Vector_concat(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			if (context.StackPosition + 1 >= Context.STACK_LENGTH)
			{
				context.player.RaiseStackOverflow(ref error);
				return;
			}


			var scope = (RtPayloadMethodScope)context.GC.Heap[scope_ptr].facility;
			var vecinstance = context.GC.Heap[thisPtr.HeapPtr];

			//在目标槽初始化vector
			int ptrIndex = returnSlotIndex;

			int instancePtr = context.CacheVectorPtr + ptrIndex;
			var instance = context.GC.Heap[instancePtr];

			
			instance.Type = (ASInstance)vecinstance.Type;
			((RtPayloadVector)instance.facility).HEAPINSTANCE_PTR = 0;
			((RtPayloadVector)instance.facility).element_asclass = ((ASInstance)vecinstance.Type)._element_class;
			((RtPayloadVector)instance.facility).element_type = ((ASInstance)vecinstance.Type)._element_class == null ? TypeKind.Any : (TypeKind)((ASInstance)vecinstance.Type)._element_class.Type_identifier;
			((RtPayloadVector)instance.facility).GetStore(context.player).SetBuffer(0);
			((RtPayloadVector)instance.facility).GetStore(context.player).length = 0;

			context.StackSlots[returnSlotIndex].SetHeapPtr(instancePtr);

			




			var rest = scope.ReadSlot(0, context.player);
			var rest_array = (RtPayloadArray)context.GC.Heap[rest.HeapPtr].facility;

#if DEBUG
			if (rest_array.StoreMode != RtPayloadArray.ArrayStoreMode.cache_on_stack)
				throw new InvalidOperationException();
#endif

			int len = 0;

			var arguments = rest_array.stack_store.Span;
			for (var i = -1; i < arguments.Length; i++)
			{
				RtPayloadVector srcVec;
				int srcVecPtr;
				if (i >= 0)
				{
					NaNBoxing a = arguments[i];
					if (a.ValueType == NaNBoxing.BoxType.Null || a.ValueType == NaNBoxing.BoxType.Undefined)
					{
						context.player.RaiseTypeError_AccessNull(ref error);
						return;
					}

					if (a.ValueType != NaNBoxing.BoxType.HeapPtr)
					{
						context.player.RaiseTypeError(ref error, a, (TypeKind)((ASInstance)instance.Type)._link_codescope.TypeLayout.ASType.Type_identifier);
						return;
					}

					var obj = context.GC.Heap[a.HeapPtr];
					if (obj.TypeKind != RtHeapTypeKind.VECTOR)
					{
						context.player.RaiseTypeError(ref error, a, (TypeKind)((ASInstance)instance.Type)._link_codescope.TypeLayout.ASType.Type_identifier);
						return;
					}

					srcVec = (RtPayloadVector)obj.facility;
					srcVecPtr = RtPayloadVector.FindAndUpdateHeapInstancePtr(a.HeapPtr, context.player, out srcVec);

					if (((ASInstance)vecinstance.Type)._element_class != null)
					{
						if (srcVec.element_asclass == null)
						{
							context.player.RaiseTypeError(ref error, a, (TypeKind)((ASInstance)instance.Type)._link_codescope.TypeLayout.ASType.Type_identifier);
							return;
						}

						if (!srcVec.element_asclass.Instance.IsExtend(((ASInstance)vecinstance.Type)._element_class.Instance)
							&&
							!srcVec.element_asclass.Instance.IsImplements(((ASInstance)vecinstance.Type)._element_class.Instance)
							)
						{
							context.player.RaiseTypeError(ref error, a, (TypeKind)((ASInstance)instance.Type)._link_codescope.TypeLayout.ASType.Type_identifier);
							return;
						}
					}
				}
				else
				{
					srcVec = (RtPayloadVector)vecinstance.facility;
					srcVecPtr = RtPayloadVector.FindAndUpdateHeapInstancePtr(thisPtr.HeapPtr, context.player, out srcVec);
				}

				//pass

				var dstVec = (RtPayloadVector)instance.facility;
				int count = srcVec.GetStore(context.player).length;
				dstVec.Resize( len + count, ref error, context.player, (ASInstance)instance.Type);
				if (error.raised)
				{
					return;
				}				
				int vptr = RtPayloadVector.FindAndUpdateHeapInstancePtr(instancePtr, context.player, out dstVec);

				int sindex = context.StackPosition;
				context.StackPosition++;

				context.StackSlots[sindex].SetUndefined();

				for (int j = 0; j < count; j++)
				{

					NaNBoxing value = srcVec.ReadSlot(j, context.player, sindex, srcVecPtr);
					dstVec.SetSlot(len + j, context.player, vptr, value, ref error); //里面可能有分配内存的操作
					if (error.raised)
					{
						context.StackPosition--;
						return;
					}
				}

				context.StackPosition--;

				len += count;
			}


		}



		//__AS3__.vec$Vector@push
		[NativeFunction("__AS3__.vec$Vector@push")]
		public static void Vector_push(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			

			var scope = (RtPayloadMethodScope)context.GC.Heap[scope_ptr].facility;
			var vecinstance = context.GC.Heap[thisPtr.HeapPtr];

			var rest = scope.ReadSlot(0, context.player);
			var rest_array = (RtPayloadArray)context.GC.Heap[rest.HeapPtr].facility;

#if DEBUG
			if (rest_array.StoreMode != RtPayloadArray.ArrayStoreMode.cache_on_stack)
				throw new InvalidOperationException();
#endif

			var vector = ((RtPayloadVector)vecinstance.facility);

			if (vector.GetStore(context.player).isFixed)
			{
				context.player.RaiseRangeError(ref error, "Cannot change the length of a fixed Vector.");
				return;
			}

			int len = vector.GetStore(context.player).length;

			var arguments = rest_array.stack_store.Span;

			vector.Resize(len + arguments.Length , ref error, context.player, (ASInstance)vecinstance.Type);
			if (error.raised)
			{
				return;
			}

			for (int i = 0; i < arguments.Length; i++)
			{
				NaNBoxing a = arguments[i];

				context.player.ConvertValueType(ref error, a,
					 ((ASInstance)vecinstance.Type)._element_class == null ? TypeKind.Any : (TypeKind)((ASInstance)vecinstance.Type)._element_class.Type_identifier,
					  ((ASInstance)vecinstance.Type)._element_class, ref context.StackSlots[returnSlotIndex],scope_ptr
					);

				if (error.raised)
				{
					return;
				}
				
				int vptr = RtPayloadVector.FindAndUpdateHeapInstancePtr(thisPtr.HeapPtr, context.player, out vector);
				vector.SetSlot(len + i, context.player, vptr, context.StackSlots[returnSlotIndex], ref error);

				if (error.raised)
				{
					return;
				}


			}


			context.StackSlots[returnSlotIndex].SetUInt( (uint)(len + arguments.Length) );
		}




		//__AS3__.vec$Vector@pop
		[NativeFunction("__AS3__.vec$Vector@pop")]
		public static void Vector_pop(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtPayloadMethodScope)context.GC.Heap[scope_ptr].facility;
			var vecinstance = context.GC.Heap[thisPtr.HeapPtr];

			RtPayloadVector vector;
			int vecptr = RtPayloadVector.FindAndUpdateHeapInstancePtr(thisPtr.HeapPtr, context.player, out vector);

			if (vector.GetStore(context.player).isFixed)
			{
				context.player.RaiseRangeError(ref error, "Cannot change the length of a fixed Vector.");
				return;
			}

			int len = vector.GetStore(context.player).length;
			if (len == 0)
			{
				if (((ASInstance)vecinstance.Type)._element_class == null)
				{
					context.StackSlots[returnSlotIndex].SetUndefined();
				}
				else if (((ASInstance)vecinstance.Type)._element_class.Instance.Flags.HasFlag(ClassFlags.Struct))
				{					
					int cache_ptr = context.player.InitCacheInstance(((ASInstance)vecinstance.Type)._element_class, returnSlotIndex, true);
					context.StackSlots[returnSlotIndex].SetHeapPtr(cache_ptr);
				}
				else
				{
					context.StackSlots[returnSlotIndex].setDefault(
						((ASInstance)vecinstance.Type)._element_class != null ?
						(TypeKind)((ASInstance)vecinstance.Type)._element_class.Type_identifier :

						TypeKind.Any

						);

					

				}
			}
			else
			{
				if (context.StackPosition + 1 >= Context.STACK_LENGTH)
				{
					context.player.RaiseStackOverflow(ref error);
					return;
				}
				context.StackPosition++;

				Debug.Assert(context.StackPosition - 1 != returnSlotIndex);

				NaNBoxing v = vector.ReadSlot(len-1, context.player, context.StackPosition-1, vecptr);
				context.StackSlots[returnSlotIndex] = v;

				context.StackPosition--;

				if (v.ValueType == BoxType.HeapPtr)
				{
					var check = context.GC.Heap[v.HeapPtr];
					if (check.TypeKind == RtHeapTypeKind.INSTANCE && ((ASInstance)check.Type).Flags.HasFlag(ClassFlags.Struct))
					{
						//clone结构体
						int clonedptr = returnSlotIndex + context.CacheInstancePtr;
						var cacheObj = context.GC.Heap[clonedptr];
						cacheObj.Type = check.Type;

						((RtPayloadInstance)cacheObj.facility).methodscopeslot_ref_state = 0;
						((RtPayloadInstance)cacheObj.facility).HEAPINSTANCE_PTR = 0;
						((RtPayloadInstance)cacheObj.facility).CopyFrom(check, context.player, check.Type._link_codescope.TypeLayout.Size);

						context.StackSlots[returnSlotIndex].SetHeapPtr(clonedptr);

					}
				}

				vector.Resize(len-1, ref error, context.player, (ASInstance)vecinstance.Type);
				Debug.Assert(!error.raised); // 这里不可能发生
			}

		}


		//__AS3__.vec$Vector@shift
		[NativeFunction("__AS3__.vec$Vector@shift")]
		public static void Vector_shift(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtPayloadMethodScope)context.GC.Heap[scope_ptr].facility;
			var vecinstance = context.GC.Heap[thisPtr.HeapPtr];

			RtPayloadVector vector;
			int vecptr = RtPayloadVector.FindAndUpdateHeapInstancePtr(thisPtr.HeapPtr, context.player, out vector);

			if (vector.GetStore(context.player).isFixed)
			{
				context.player.RaiseRangeError(ref error, "Cannot change the length of a fixed Vector.");
				return;
			}

			int len = vector.GetStore(context.player).length;
			if (len == 0)
			{
				if (((ASInstance)vecinstance.Type)._element_class == null)
				{
					context.StackSlots[returnSlotIndex].SetUndefined();
				}
				else if (((ASInstance)vecinstance.Type)._element_class.Instance.Flags.HasFlag(ClassFlags.Struct))
				{
					int cache_ptr = context.player.InitCacheInstance(((ASInstance)vecinstance.Type)._element_class, returnSlotIndex, true);
					context.StackSlots[returnSlotIndex].SetHeapPtr(cache_ptr);
				}
				else
				{
					context.StackSlots[returnSlotIndex].setDefault(
						((ASInstance)vecinstance.Type)._element_class != null ?
						(TypeKind)((ASInstance)vecinstance.Type)._element_class.Type_identifier :

						TypeKind.Any

						);
				}
			}
			else
			{
				if (context.StackPosition + 1 >= Context.STACK_LENGTH)
				{
					context.player.RaiseStackOverflow(ref error);
					return;
				}
				context.StackPosition++;

				Debug.Assert(context.StackPosition - 1 != returnSlotIndex);

				NaNBoxing v = vector.ReadSlot(0, context.player, context.StackPosition - 1, vecptr);
				context.StackSlots[returnSlotIndex] = v;

				context.StackPosition--;

				if (v.ValueType == BoxType.HeapPtr)
				{
					var check = context.GC.Heap[v.HeapPtr];
					if (check.TypeKind == RtHeapTypeKind.INSTANCE && ((ASInstance)check.Type).Flags.HasFlag(ClassFlags.Struct))
					{
						int clonedptr = returnSlotIndex + context.CacheInstancePtr;
						var cacheObj = context.GC.Heap[clonedptr];
						cacheObj.Type = check.Type;

						((RtPayloadInstance)cacheObj.facility).methodscopeslot_ref_state = 0;
						((RtPayloadInstance)cacheObj.facility).HEAPINSTANCE_PTR = 0;
						((RtPayloadInstance)cacheObj.facility).CopyFrom(check, context.player, check.Type._link_codescope.TypeLayout.Size);

						context.StackSlots[returnSlotIndex].SetHeapPtr(clonedptr);
					}
				}

				var store = vector.GetStore(context.player);
				if (len > 1)
				{
					var span = CollectionsMarshal.AsSpan(store.buffer);
					int elementSize = store.elementSize;
					for (int i = 1; i < len; i++)
					{
						var srcSlice = span.Slice(i * elementSize, elementSize);
						var dstSlice = span.Slice((i - 1) * elementSize, elementSize);
						srcSlice.CopyTo(dstSlice);
					}
				}

				vector.Resize(len - 1, ref error, context.player, (ASInstance)vecinstance.Type);
				Debug.Assert(!error.raised);
			}

		}

		class JoinPrinter : IPrint
		{
			public StringBuilder stringBuilder;
			public void Write(string message)
			{
				stringBuilder.Append(message);
			}

			public void Write(ReadOnlySpan<char> chars)
			{
				stringBuilder.Append(chars);
			}

			public void WriteLine(string message)
			{
				stringBuilder.AppendLine(message);
			}
		}

		private static JoinPrinter joinPrinter = new JoinPrinter() { stringBuilder = new StringBuilder() };

		[NativeFunction("__AS3__.vec$Vector@join")]
		public static void Vector_join(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtPayloadMethodScope)context.GC.Heap[scope_ptr].facility;
			var vecinstance = context.GC.Heap[thisPtr.HeapPtr];

			var vector = (RtPayloadVector)vecinstance.facility;
			var store = vector.GetStore(context.player);
			if (store.length == 0)
			{
				context.StackSlots[returnSlotIndex].SetHeapPtr(context.player.EMPTY_STR);
			}
			else
			{
				string sepStr;

				NaNBoxing sep = scope.ReadSlot(0, context.player);
				if (sep.ValueType == NaNBoxing.BoxType.Null)
				{
					sepStr = "";
				}
				else
				{
					sepStr = Extensions.GetPrimitiveValueToString(context.player, sep);
				}

				joinPrinter.stringBuilder.Clear();
				store.DoTrace(vector.element_type, vector.element_asclass, context, stackStPos, ref error, scope_ptr, joinPrinter, sepStr);
				if (error.raised)
				{
					return;
				}

				int str = context.GC.AllocString(joinPrinter.stringBuilder.ToString());
				if (str == 0)
				{
					context.player.RaiseOutOfMemory(ref error);
					return;
				}

				context.StackSlots[returnSlotIndex].SetHeapPtr(str);

			}

		}




		internal class VectorStore
		{
			internal static void InitStructSpan(Span<byte> struct_data,ASClass element)
			{
				unsafe
				{
					fixed (byte* p = struct_data)
					{
						for (int i = 0; i < element.Instance._link_codescope.TypeLayout.Offset.Count; i++)
						{

							byte* ptr = p + element.Instance._link_codescope.TypeLayout.Offset[i];
							var member = element.Instance._link_codescope.Members[i];

							if ((member.Kind == ScopeMemberKind.Constant || member.Kind == ScopeMemberKind.Slot) && member.trait.Value != null && member.trait.Value.initValue.HasValue)
							{
								RtPayloadInstance.SetSlotDataByValue(member, ptr, member.trait.Value.initValue.Value);
							}
							else
							{
								RtPayloadInstance.InitSlotData(member, ptr , element.Instance._link_codescope.TypeLayout.SlotSize[i]);
							}
						}
					}
				}
			}

			internal void SetBuffer(int size)
			{
				if (!IsCache)
				{
					if (buffer == null)
					{
						buffer = new List<byte>(size);
						buffer.AddRange(Enumerable.Repeat<byte>(0,size));
						
					}
					else 
					{
#if DEBUG
						if (buffer.Count > 0)
						{
							throw new InvalidOperationException();
						}
#endif

						
						buffer.AddRange(Enumerable.Repeat<byte>(0, size));
						
					}
					
				}
				else
				{
					//throw new InvalidOperationException();
					Debug.Assert(size < RtPayloadVector.MAX_CACHE_SIZE);
					buffer.AddRange(Enumerable.Repeat<byte>(0, size));

					//buffer.Clear();
				}
			}

			public VectorStore()
			{
				IsCache = true;
				buffer = new List<byte>( RtPayloadVector.MAX_CACHE_SIZE );//这里只是保留了内存，而不是实际元素个数！
				
			}

			public VectorStore(VectorStore vectorStore)
			{
				IsCache = false;

				isFixed = vectorStore.isFixed;
				length = vectorStore.length;
				elementSize = vectorStore.elementSize;
				
				buffer = new List<byte>(vectorStore.buffer.Count);
				buffer.AddRange(vectorStore.buffer);

			}

			//public VectorStore(TypeKind elementkind, ASClass element , int len,bool isfixed)
			//{
			//	IsCache = true;
			//	isFixed = isfixed;
			//	elementSize = GetElementSize(elementkind, element);

			//	SetBuffer(len * elementSize);

			//	this.length = len;

			//	SetDefault(elementkind, element,0,len);
			//}

			internal void SetDefault(TypeKind elementkind, ASClass element,int start ,int len)
			{
				Span<byte> struct_data = stackalloc byte[RtPayloadInstance.MAX_CACHEABLE_SIZE];
				if (element != null && element.Instance.Flags.HasFlag(ClassFlags.Struct))
				{
					InitStructSpan(struct_data, element);
				}

				var span = CollectionsMarshal.AsSpan(buffer);
				//初始化默认值
				for (int i = start; i <start + len; i++)
				{
					var slice = span.Slice(i * elementSize, elementSize);
					switch (elementkind)
					{
						case TypeKind.Any:
							{
								NaNBoxing v = default; v.SetUndefined();
								MemoryMarshal.Write(slice, ref v);
							}
							break;
						case TypeKind.Boolean:
						case TypeKind.SByte:
						case TypeKind.Byte:
							{
								slice.Clear();
								//byte v = 0;
								//MemoryMarshal.Write(slice, ref v);
							}
							break;
						case TypeKind.Short:
						case TypeKind.UShort:
							{
								slice.Clear();
								//buffer.Add(0);
								//buffer.Add(0);
							}
							break;
						case TypeKind.Int:
						case TypeKind.Uint:
						case TypeKind.Float:
							{
								slice.Clear();
								//buffer.Add(0);
								//buffer.Add(0);
								//buffer.Add(0);
								//buffer.Add(0);
							}
							break;
						case TypeKind.Number:
							{
								slice.Clear();
								//buffer.Add(0);
								//buffer.Add(0);
								//buffer.Add(0);
								//buffer.Add(0);
								//buffer.Add(0);
								//buffer.Add(0);
								//buffer.Add(0);
								//buffer.Add(0);
							}
							break;

						case TypeKind.Object:
						case TypeKind.Class:
						case TypeKind.String:
						case TypeKind.Function:
						case TypeKind.Array:
						case TypeKind.Vector:
						case TypeKind.Namespace:
							{
								NaNBoxing v = default; v.SetNull();
								MemoryMarshal.Write(slice, ref v);
							}
							break;
						case TypeKind.Fun_Void:
						case TypeKind.TraitDataReference:
						case TypeKind.RTQName_MultiName_DataReference:
						case TypeKind.CParseNS_Traits:
						case TypeKind.RTQNameRTQNameL_N:
						case TypeKind.SearchNameSpaceFromImports:
						case TypeKind.Unknown:
						case TypeKind.Null:
						case TypeKind.Super:
							throw new InvalidOperationException();
						default:
							if (element.Instance.Flags.HasFlag(ClassFlags.Struct))
							{
								var size = element.Instance._link_codescope.TypeLayout.Size;
								struct_data.Slice(0, size).CopyTo(slice);
							}
							else
							{
								NaNBoxing v = default; v.SetNull();
								MemoryMarshal.Write(slice, ref v);
							}

							break;
					}


				}

			}

			//public VectorStore(TypeKind element_type, ASClass element, Span<NaNBoxing> rest_span,Context context)
			//{
			//	IsCache = true;
			//	isFixed = false;
			//	length = rest_span.Length;
			//	elementSize = GetElementSize(element_type, element);

			//	SetBuffer(rest_span.Length * elementSize);

			//	CopySpan(element_type, element, rest_span, context);
			//}

			internal void CopySpan(TypeKind element_type, ASClass element , Span<NaNBoxing> rest_span, Context context)
			{
				var span = CollectionsMarshal.AsSpan(buffer);
				for (int i = 0; i < rest_span.Length; i++)
				{
					var slice = span.Slice(i * elementSize, elementSize);
					switch (element_type)
					{

						case TypeKind.Boolean:
							{
								bool v = rest_span[i].Boolean;
								MemoryMarshal.Write(slice, ref v);
							}
							break;
						case TypeKind.SByte:
						case TypeKind.Byte:
							{
								byte v = rest_span[i].ByteValue;
								MemoryMarshal.Write(slice, ref v);
							}
							break;
						case TypeKind.Short:
						case TypeKind.UShort:
							{
								ushort v = rest_span[i].UShortValue;
								MemoryMarshal.Write(slice, ref v);
							}
							break;
						case TypeKind.Int:
						case TypeKind.Uint:
						case TypeKind.Float:
							{
								uint v = rest_span[i].UIntValue;
								MemoryMarshal.Write(slice, ref v);
							}
							break;
						case TypeKind.Number:
							{
								double v = rest_span[i].Number;
								MemoryMarshal.Write(slice, ref v);
							}
							break;
						case TypeKind.Any:
						case TypeKind.Object:
						case TypeKind.Class:
						case TypeKind.String:
						case TypeKind.Function:
						case TypeKind.Array:
						case TypeKind.Vector:
						case TypeKind.Namespace:
							{
								NaNBoxing v = rest_span[i];
								MemoryMarshal.Write(slice, ref v);
							}
							break;
						case TypeKind.Super:
						case TypeKind.Fun_Void:
						case TypeKind.TraitDataReference:
						case TypeKind.RTQName_MultiName_DataReference:
						case TypeKind.CParseNS_Traits:
						case TypeKind.RTQNameRTQNameL_N:
						case TypeKind.SearchNameSpaceFromImports:
						case TypeKind.Unknown:
						case TypeKind.Null:
							throw new InvalidOperationException();
						default:
							{
								if (element.Instance.Flags.HasFlag(ClassFlags.Struct))
								{

									if (rest_span[i].ValueType != NaNBoxing.BoxType.HeapPtr)
									{
										InitStructSpan(slice, element);
									}
									else
									{

										RtPayloadInstance struct_payload = (RtPayloadInstance)context.GC.Heap[rest_span[i].HeapPtr].facility;

										struct_payload.GetStoreData(context.player, element.Instance).Slice(0, elementSize).CopyTo(slice);

									}

								}
								else
								{
									NaNBoxing v = rest_span[i];
									MemoryMarshal.Write(slice, ref v);
								}
							}
							break;
					}
				}

			}

			public static int GetElementSize(TypeKind elementkind, ASClass element)
			{
				switch (elementkind)
				{
					case ABC.TypeKind.Any:
						return 8;
					case ABC.TypeKind.Boolean:
					case TypeKind.SByte:
					case TypeKind.Byte:
						return 1;
					case TypeKind.Short:
					case TypeKind.UShort:
						return 2;
					case ABC.TypeKind.Int:
					case ABC.TypeKind.Uint:
						return 4;
					case TypeKind.Float:
						return 4;
					case ABC.TypeKind.Number:
						return 8;
					case ABC.TypeKind.Null:
					case ABC.TypeKind.String:
					case ABC.TypeKind.Function:
						return 8;
					case ABC.TypeKind.Fun_Void:
						throw new InvalidOperationException();
					case ABC.TypeKind.Array:
					case ABC.TypeKind.Vector:
					case ABC.TypeKind.Namespace:
						return 8;
					case ABC.TypeKind.Unknown:
						throw new InvalidOperationException();
					case ABC.TypeKind.Object:
						return 8;
					default:
						if (element.Instance.Flags.HasFlag(ClassFlags.Struct))
						{
							return element.Instance._link_codescope.TypeLayout.Size;
						}
						else
						{
							return 8;
						}


				}

			}

			internal readonly bool IsCache;

			internal List<byte> buffer;


			public int Size
			{
				get
				{
					return buffer.Count;
				}
			}

			internal int elementSize;

			internal int length;

			internal bool isFixed;

			

			internal bool IsValidIndexRange(NaNBoxing index,out int valided_index)
			{

				switch (index.ValueType)
				{
					case NaNBoxing.BoxType.Number:
						{
							if (Math.Truncate(index.Number) == index.Number)
							{
								valided_index = (int)index.Number;
								return index.Number < length && index.Number >= 0;
							}
							else
							{
								valided_index = -1;
								return false;
							}
						}
					case NaNBoxing.BoxType.Undefined:
					case NaNBoxing.BoxType.Null:
					case NaNBoxing.BoxType.Boolean:
						valided_index = -1;
						return false;
					case NaNBoxing.BoxType.Int:
						valided_index = index.IntValue;
						return index.IntValue < length && index.IntValue >= 0;
					case NaNBoxing.BoxType.Uint:
						valided_index = (int)index.UIntValue;
						return index.UIntValue < length;
					case NaNBoxing.BoxType.Sbyte:
						valided_index = index.SByteValue;
						return index.SByteValue <length && index.SByteValue >= 0;
					case NaNBoxing.BoxType.Byte:
						valided_index = index.ByteValue;
						return index.ByteValue < length;
					case NaNBoxing.BoxType.Short:
						valided_index = index.ShortValue;
						return index.ShortValue < length && index.ShortValue >= 0;	
					case NaNBoxing.BoxType.UShort:
						valided_index = index.UShortValue;
						return index.UShortValue < length;
					case NaNBoxing.BoxType.Float:
						if (MathF.Truncate(index.FloatValue) == index.FloatValue)
						{
							valided_index = (int)index.FloatValue;
							return index.FloatValue < length && index.FloatValue >= 0;
						}
						else
						{
							valided_index = -1;
							return false;
						}
					case NaNBoxing.BoxType.HeapPtr:
					case NaNBoxing.BoxType.Fault:
					default:
						valided_index = -1;
						return false;
				}


			}

			internal Span<byte> ReadStoreAt(int validid)
			{
				return CollectionsMarshal.AsSpan(buffer).Slice(validid * elementSize, elementSize);
			}

			internal Span<byte> ReadStoreOffset(int offset,int size)
			{
				return CollectionsMarshal.AsSpan(buffer).Slice(offset, size);
			}


			internal void GCMarkAllElements(Context context)
			{


				var span = CollectionsMarshal.AsSpan(buffer);

				for (int i = 0; i <length; i++)
				{
					var slice = span.Slice(i * elementSize, elementSize);

					NaNBoxing boxing = MemoryMarshal.Read<NaNBoxing>(slice);

					if (boxing.ValueType == NaNBoxing.BoxType.HeapPtr)
					{
						context.GC.mark(context.GC.Heap[boxing.HeapPtr]);
					}

				}


			}

			internal void DoTrace(TypeKind element_type, ASClass element , Context context, int stackStPos, ref ReceiveError error, int scope_ptr, IPrint printer,string sep=",")
			{
				var span = CollectionsMarshal.AsSpan(buffer);
				for (int i = 0; i < length; i++)
				{
					var slice = span.Slice(i * elementSize, elementSize);
					switch (element_type)
					{					
						case TypeKind.Boolean:
							{
								bool v = MemoryMarshal.Read<bool>(slice);
								NaNBoxing box = default;box.SetBoolean(v);
								TopLevel.TraceElement(box, context, stackStPos, ref error, scope_ptr, default, printer);
							}
							break;
						case TypeKind.SByte:
							{ 
								sbyte v = MemoryMarshal.Read<sbyte>(slice);
								NaNBoxing box = default;box.SetSByte(v);
								TopLevel.TraceElement(box,context, stackStPos, ref error,scope_ptr, default, printer);
							}
							break;
						case TypeKind.Byte:
							{ 
								byte v = MemoryMarshal.Read<byte>(slice);
								NaNBoxing box = default;box.SetByte(v);
								TopLevel.TraceElement(box, context, stackStPos, ref error, scope_ptr, default, printer);
							}
							break;
						case TypeKind.Short:
							{ 
								short v = MemoryMarshal.Read<short>(slice);
								NaNBoxing box = default; box.SetShort(v);
								TopLevel.TraceElement(box, context, stackStPos, ref error, scope_ptr, default, printer);
							}
							break;
						case TypeKind.UShort:
							{ 
								ushort v = MemoryMarshal.Read<ushort>(slice);
								NaNBoxing box = default; box.SetUShort(v);
								TopLevel.TraceElement(box, context, stackStPos, ref error, scope_ptr, default, printer);
							}
							break;
						case TypeKind.Int:
							{ 
								int v = MemoryMarshal.Read<int>(slice);
								NaNBoxing box=default;box.SetInt(v);
								TopLevel.TraceElement(box, context, stackStPos, ref error, scope_ptr, default, printer);
							}
							break;
						case TypeKind.Uint:
							{ 
								uint v = MemoryMarshal.Read<uint>(slice);
								NaNBoxing box = default;box.SetUInt(v);
								TopLevel.TraceElement(box, context, stackStPos, ref error, scope_ptr, default, printer);
							}
							break;
						case TypeKind.Float:
							{ 
								float v = MemoryMarshal.Read<float>(slice);
								NaNBoxing box = default; box.SetFloat(v);
								TopLevel.TraceElement(box, context, stackStPos, ref error, scope_ptr, default, printer);
							}
							break;
						case TypeKind.Number:
							{ 
								double v = MemoryMarshal.Read<double>(slice);
								NaNBoxing box = default; box.SetNumber(v);
								TopLevel.TraceElement(box, context, stackStPos, ref error, scope_ptr, default, printer);
							}
							break;
						case TypeKind.Fun_Void:
						case TypeKind.TraitDataReference:
						case TypeKind.RTQName_MultiName_DataReference:
						case TypeKind.CParseNS_Traits:
						case TypeKind.RTQNameRTQNameL_N:	
						case TypeKind.SearchNameSpaceFromImports:	
						case TypeKind.Unknown:
						case TypeKind.Super:
						case TypeKind.Null:
							throw new InvalidOperationException();
						case TypeKind.Object:
						case TypeKind.Class:
						case TypeKind.String:
						case TypeKind.Function:
						case TypeKind.Array:
						case TypeKind.Vector:
						case TypeKind.Namespace:
						case TypeKind.Any:
							{
								NaNBoxing v = MemoryMarshal.Read<NaNBoxing>(slice);
								TopLevel.TraceElement(v, context, stackStPos, ref error, scope_ptr, default, printer);
								if (error.raised)
								{
									return;
								}
							}
							break;
						default:
							{
								if (element.Instance.Flags.HasFlag(ClassFlags.Struct))
								{
									if (context.StackPosition >= Context.STACK_LENGTH)
									{
										context.player.RaiseStackOverflow(ref error);
										return;
									}

									int p =context.player.InitCacheInstance(element, context.StackPosition, false);
									slice.CopyTo(((RtPayloadInstance)context.GC.Heap[p].facility).GetStoreData(context.player,(ASInstance)element.Instance));

									context.StackPosition++;
									TopLevel.TraceElement(context.StackSlots[context.StackPosition-1], context, stackStPos, ref error, scope_ptr,default,printer);
									context.StackPosition--;

									if (error.raised)
									{
										return;
									}

								}
								else
								{
									NaNBoxing v = MemoryMarshal.Read<NaNBoxing>(slice);
									TopLevel.TraceElement(v, context, stackStPos, ref error, scope_ptr, default, printer);
									if (error.raised)
									{
										return;
									}
								}
							}
							break;
					}

					if (i < length - 1)
					{
						printer.Write(sep);
					}


				}
			}

			internal void CopyFrom(VectorStore store)
			{
				elementSize = store.elementSize;
				length = store.length;
				isFixed = store.isFixed;

				buffer.Clear();
				buffer.AddRange(store.buffer);
				
			}

			

			
		}
	}
}
