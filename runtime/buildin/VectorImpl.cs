using juicescript.ABC;
using juicescript.ABC.Locaters;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
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
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr].facility;
			var vecinstance = context.GC.Heap[thisPtr.HeapPtr];
			//var rest = scope.ReadSlot(0, context.player);


			int a_ptr = stackStPos - scope.SlotCount
									- 2; /*
									      * arguments
									      * callee
									      */
			NaNBoxing arguments = context.StackSlots[a_ptr];

			var rest_array = (RtArray)context.GC.Heap[arguments.HeapPtr].facility;

			if (rest_array.StoreMode != RtArray.ArrayStoreMode.cache_on_stack)
				throw new InvalidOperationException();

			var vector = (RtVector)vecinstance.facility;

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

			if (element_size * initLen > RtVector.MAX_CACHE_SIZE) //超出缓存限制，要保存到堆
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
				store.SetDefault(vector.element_type, vector.element_asclass, 0, (int)initLen);

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
				context.player.ConvertValueType(ref error, rest_span[i], vector.element_type, vector.element_asclass, ref rest_span[i], scope_ptr, thisPtr);
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

			if (element_size * initLen > RtVector.MAX_CACHE_SIZE) //超出缓存限制，要保存到堆
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
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr].facility;
			var vecinstance = context.GC.Heap[thisPtr.HeapPtr];

			var isfixed = scope.ReadSlot(0, context.player);
			Debug.Assert(isfixed.ValueType == NaNBoxing.BoxType.Boolean);

			((RtVector)vecinstance.facility).GetStore(context.player).isFixed = isfixed.Boolean;

		}
		//__AS3__.vec$Vector@get#fixed
		[NativeFunction("__AS3__.vec$Vector@get#fixed")]
		public static void Vector_get_fixed(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr].facility;
			var vecinstance = context.GC.Heap[thisPtr.HeapPtr];
			context.StackSlots[returnSlotIndex].SetBoolean(((RtVector)vecinstance.facility).GetStore(context.player).isFixed);

		}

		//function __AS3__.vec$Vector@length
		[NativeFunction("__AS3__.vec$Vector@get#length")]
		public static void Vector_get_length(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr].facility;
			var vecinstance = context.GC.Heap[thisPtr.HeapPtr];


			int len = ((RtVector)vecinstance.facility).GetStore(context.player).length;
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
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr].facility;
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

			((RtVector)vecinstance.facility).Resize(newlen.IntValue, ref error, context.player, (ASInstance)vecinstance.Type);
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


			var scope = (RtMethodScope)context.GC.Heap[scope_ptr].facility;
			var vecinstance = context.GC.Heap[thisPtr.HeapPtr];

			//在目标槽初始化vector
			int ptrIndex = returnSlotIndex;

			int instancePtr = context.CacheVectorPtr + ptrIndex;
			var instance = context.GC.Heap[instancePtr];


			instance.Type = (ASInstance)vecinstance.Type;
			((RtVector)instance.facility).HEAPINSTANCE_PTR = 0;
			((RtVector)instance.facility).element_asclass = ((ASInstance)vecinstance.Type)._element_class;
			((RtVector)instance.facility).element_type = ((ASInstance)vecinstance.Type)._element_class == null ? TypeKind.Any : (TypeKind)((ASInstance)vecinstance.Type)._element_class.Type_identifier;
			((RtVector)instance.facility).GetStore(context.player).SetBuffer(0);
			((RtVector)instance.facility).GetStore(context.player).length = 0;

			context.StackSlots[returnSlotIndex].SetHeapPtr(instancePtr);






			var rest = scope.ReadSlot(0, context.player);
			var rest_array = (RtArray)context.GC.Heap[rest.HeapPtr].facility;

#if DEBUG
			if (rest_array.StoreMode != RtArray.ArrayStoreMode.cache_on_stack)
				throw new InvalidOperationException();
#endif

			int len = 0;

			var arguments = rest_array.stack_store.Span;
			for (var i = -1; i < arguments.Length; i++)
			{
				RtVector srcVec;
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

					srcVec = (RtVector)obj.facility;
					srcVecPtr = RtVector.FindAndUpdateHeapInstancePtr(a.HeapPtr, context.player, out srcVec);

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
					srcVec = (RtVector)vecinstance.facility;
					srcVecPtr = RtVector.FindAndUpdateHeapInstancePtr(thisPtr.HeapPtr, context.player, out srcVec);
				}

				//pass

				var dstVec = (RtVector)instance.facility;
				int count = srcVec.GetStore(context.player).length;
				dstVec.Resize(len + count, ref error, context.player, (ASInstance)instance.Type);
				if (error.raised)
				{
					return;
				}
				int vptr = RtVector.FindAndUpdateHeapInstancePtr(instancePtr, context.player, out dstVec);

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


			var scope = (RtMethodScope)context.GC.Heap[scope_ptr].facility;
			var vecinstance = context.GC.Heap[thisPtr.HeapPtr];

			var rest = scope.ReadSlot(0, context.player);
			var rest_array = (RtArray)context.GC.Heap[rest.HeapPtr].facility;

#if DEBUG
			if (rest_array.StoreMode != RtArray.ArrayStoreMode.cache_on_stack)
				throw new InvalidOperationException();
#endif

			var vector = ((RtVector)vecinstance.facility);

			if (vector.GetStore(context.player).isFixed)
			{
				context.player.RaiseRangeError(ref error, "Cannot change the length of a fixed Vector.");
				return;
			}

			int len = vector.GetStore(context.player).length;

			var arguments = rest_array.stack_store.Span;

			vector.Resize(len + arguments.Length, ref error, context.player, (ASInstance)vecinstance.Type);
			if (error.raised)
			{
				return;
			}

			for (int i = 0; i < arguments.Length; i++)
			{
				NaNBoxing a = arguments[i];

				context.player.ConvertValueType(ref error, a,
					 ((ASInstance)vecinstance.Type)._element_class == null ? TypeKind.Any : (TypeKind)((ASInstance)vecinstance.Type)._element_class.Type_identifier,
					  ((ASInstance)vecinstance.Type)._element_class, ref context.StackSlots[returnSlotIndex], scope_ptr
					);

				if (error.raised)
				{
					return;
				}

				int vptr = RtVector.FindAndUpdateHeapInstancePtr(thisPtr.HeapPtr, context.player, out vector);
				vector.SetSlot(len + i, context.player, vptr, context.StackSlots[returnSlotIndex], ref error);

				if (error.raised)
				{
					return;
				}


			}


			context.StackSlots[returnSlotIndex].SetUInt((uint)(len + arguments.Length));
		}




		//__AS3__.vec$Vector@pop
		[NativeFunction("__AS3__.vec$Vector@pop")]
		public static void Vector_pop(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr].facility;
			var vecinstance = context.GC.Heap[thisPtr.HeapPtr];

			RtVector vector;
			int vecptr = RtVector.FindAndUpdateHeapInstancePtr(thisPtr.HeapPtr, context.player, out vector);

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

				NaNBoxing v = vector.ReadSlot(len - 1, context.player, context.StackPosition - 1, vecptr);
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

						((RtInstance)cacheObj.facility).methodscopeslot_ref_state = 0;
						((RtInstance)cacheObj.facility).HEAPINSTANCE_PTR = 0;
						((RtInstance)cacheObj.facility).CopyFrom(check, context.player, check.Type._link_codescope.TypeLayout.Size);

						context.StackSlots[returnSlotIndex].SetHeapPtr(clonedptr);

					}
				}

				vector.Resize(len - 1, ref error, context.player, (ASInstance)vecinstance.Type);
				Debug.Assert(!error.raised); // 这里不可能发生
			}

		}


		//
		[NativeFunction("__AS3__.vec$Vector@unshift")]
		public static void Vector_unshift(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr].facility;
			var vecinstance = context.GC.Heap[thisPtr.HeapPtr];

			var rest = scope.ReadSlot(0, context.player);
			var rest_array = (RtArray)context.GC.Heap[rest.HeapPtr].facility;

#if DEBUG
			if (rest_array.StoreMode != RtArray.ArrayStoreMode.cache_on_stack)
				throw new InvalidOperationException();
#endif

			var vector = ((RtVector)vecinstance.facility);

			if (vector.GetStore(context.player).isFixed)
			{
				context.player.RaiseRangeError(ref error, "Cannot change the length of a fixed Vector.");
				return;
			}

			int len = vector.GetStore(context.player).length;
			var arguments = rest_array.stack_store.Span;
			int newElements = arguments.Length;

			if (newElements == 0)
			{
				context.StackSlots[returnSlotIndex].SetUInt((uint)len);
				return;
			}

			vector.Resize(len + newElements, ref error, context.player, (ASInstance)vecinstance.Type);
			if (error.raised)
			{
				return;
			}

			RtVector vectorAfterResize;
			int vptr = RtVector.FindAndUpdateHeapInstancePtr(thisPtr.HeapPtr, context.player, out vectorAfterResize);

			var store = vectorAfterResize.GetStore(context.player);
			var span = CollectionsMarshal.AsSpan(store.buffer);
			int elementSize = store.elementSize;

			if (len > 0)
			{
				for (int i = len - 1; i >= 0; i--)
				{
					var srcSlice = span.Slice(i * elementSize, elementSize);
					var dstSlice = span.Slice((i + newElements) * elementSize, elementSize);
					srcSlice.CopyTo(dstSlice);
				}
			}

			for (int i = 0; i < newElements; i++)
			{
				NaNBoxing a = arguments[i];

				context.player.ConvertValueType(ref error, a,
					((ASInstance)vecinstance.Type)._element_class == null ? TypeKind.Any : (TypeKind)((ASInstance)vecinstance.Type)._element_class.Type_identifier,
					((ASInstance)vecinstance.Type)._element_class, ref context.StackSlots[returnSlotIndex], scope_ptr
				);

				if (error.raised)
				{
					return;
				}

				vectorAfterResize.SetSlot(i, context.player, vptr, context.StackSlots[returnSlotIndex], ref error);

				if (error.raised)
				{
					return;
				}
			}

			context.StackSlots[returnSlotIndex].SetUInt((uint)(len + newElements));
		}





		//__AS3__.vec$Vector@shift
		[NativeFunction("__AS3__.vec$Vector@shift")]
		public static void Vector_shift(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr].facility;
			var vecinstance = context.GC.Heap[thisPtr.HeapPtr];

			RtVector vector;
			int vecptr = RtVector.FindAndUpdateHeapInstancePtr(thisPtr.HeapPtr, context.player, out vector);

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

						((RtInstance)cacheObj.facility).methodscopeslot_ref_state = 0;
						((RtInstance)cacheObj.facility).HEAPINSTANCE_PTR = 0;
						((RtInstance)cacheObj.facility).CopyFrom(check, context.player, check.Type._link_codescope.TypeLayout.Size);

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

		//__AS3__.vec$Vector@indexOf
		[NativeFunction("__AS3__.vec$Vector@indexOf")]
		public static void Vector_indexOf(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr].facility;
			var vecinstance = context.GC.Heap[thisPtr.HeapPtr];

			RtVector vector;
			int vecPtr = RtVector.FindAndUpdateHeapInstancePtr(thisPtr.HeapPtr, context.player, out vector);
			var store = vector.GetStore(context.player);
			int len = store.length;

			if (len == 0)
			{
				context.StackSlots[returnSlotIndex].SetInt(-1);
				return;
			}

			if (context.StackPosition + 1 >= Context.STACK_LENGTH)
			{
				context.player.RaiseStackOverflow(ref error);
				return;
			}

			NaNBoxing searchElement = scope.ReadSlot(0, context.player);
			NaNBoxing fromIndexBox = scope.ReadSlot(1, context.player);

			int startIndex;
			if (fromIndexBox.ValueType == BoxType.Undefined || fromIndexBox.ValueType == BoxType.Null)
			{
				startIndex = 0;
			}
			else
			{
				Debug.Assert(fromIndexBox.ValueType == BoxType.Int);
				int fromIndex = fromIndexBox.IntValue;
				if (fromIndex < 0)
				{
					startIndex = len + fromIndex;
					if (startIndex < 0) startIndex = 0;
				}
				else
				{
					startIndex = fromIndex;
				}
			}


			int sindex = context.StackPosition;
			context.StackPosition++;
			context.StackSlots[sindex].SetUndefined();

			for (int i = startIndex; i < len; i++)
			{
				NaNBoxing element = vector.ReadSlot(i, context.player, sindex, vecPtr);
				if (context.player.IsStrictlyEqual(searchElement, element))
				{
					context.StackPosition--;
					context.StackSlots[returnSlotIndex].SetInt(i);
					return;
				}
			}

			context.StackPosition--;
			context.StackSlots[returnSlotIndex].SetInt(-1);
		}


		[NativeFunction("__AS3__.vec$Vector@lastIndexOf")]
		public static void Vector_lastIndexOf(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr].facility;
			var vecinstance = context.GC.Heap[thisPtr.HeapPtr];

			RtVector vector;
			int vecPtr = RtVector.FindAndUpdateHeapInstancePtr(thisPtr.HeapPtr, context.player, out vector);
			var store = vector.GetStore(context.player);
			int len = store.length;

			if (len == 0)
			{
				context.StackSlots[returnSlotIndex].SetInt(-1);
				return;
			}

			if (context.StackPosition + 1 >= Context.STACK_LENGTH)
			{
				context.player.RaiseStackOverflow(ref error);
				return;
			}

			NaNBoxing searchElement = scope.ReadSlot(0, context.player);
			NaNBoxing fromIndexBox = scope.ReadSlot(1, context.player);

			int startIndex;

			Debug.Assert(fromIndexBox.ValueType == BoxType.Int);
			int fromIndex = fromIndexBox.IntValue;
			if (fromIndex < 0)
			{
				startIndex = len + fromIndex;
				if (startIndex < 0) startIndex = -1;
			}
			else
			{
				startIndex = fromIndex;
				if (startIndex >= len) startIndex = len - 1;
			}


			int sindex = context.StackPosition;
			context.StackPosition++;
			context.StackSlots[sindex].SetUndefined();

			for (int i = startIndex; i >= 0; i--)
			{
				NaNBoxing element = vector.ReadSlot(i, context.player, sindex, vecPtr);
				if (context.player.IsStrictlyEqual(searchElement, element))
				{
					context.StackPosition--;
					context.StackSlots[returnSlotIndex].SetInt(i);
					return;
				}
			}

			context.StackPosition--;
			context.StackSlots[returnSlotIndex].SetInt(-1);
		}


		//__AS3__.vec$Vector@removeAt
		[NativeFunction("__AS3__.vec$Vector@removeAt")]
		public static void Vector_removeAt(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr].facility;
			var vecinstance = context.GC.Heap[thisPtr.HeapPtr];

			RtVector vector;
			int vecPtr = RtVector.FindAndUpdateHeapInstancePtr(thisPtr.HeapPtr, context.player, out vector);

			if (vector.GetStore(context.player).isFixed)
			{
				context.player.RaiseRangeError(ref error, "Cannot change the length of a fixed Vector.");
				return;
			}

			int len = vector.GetStore(context.player).length;
			int index = scope.ReadSlot(0, context.player).IntValue;

			if (index < 0)
			{
				index = len + index;
			}

			if (index < 0 || index >= len)
			{
				context.player.RaiseRangeError(ref error, "Index out of range.");
				return;
			}

			if (context.StackPosition + 1 >= Context.STACK_LENGTH)
			{
				context.player.RaiseStackOverflow(ref error);
				return;
			}
			context.StackPosition++;

			NaNBoxing v = vector.ReadSlot(index, context.player, context.StackPosition - 1, vecPtr);
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

					((RtInstance)cacheObj.facility).methodscopeslot_ref_state = 0;
					((RtInstance)cacheObj.facility).HEAPINSTANCE_PTR = 0;
					((RtInstance)cacheObj.facility).CopyFrom(check, context.player, check.Type._link_codescope.TypeLayout.Size);

					context.StackSlots[returnSlotIndex].SetHeapPtr(clonedptr);
				}
			}

			if (index < len - 1)
			{
				var store = vector.GetStore(context.player);
				var span = CollectionsMarshal.AsSpan(store.buffer);
				int elementSize = store.elementSize;
				for (int i = index; i < len - 1; i++)
				{
					var srcSlice = span.Slice((i + 1) * elementSize, elementSize);
					var dstSlice = span.Slice(i * elementSize, elementSize);
					srcSlice.CopyTo(dstSlice);
				}
			}

			vector.Resize(len - 1, ref error, context.player, (ASInstance)vecinstance.Type);
			Debug.Assert(!error.raised);
		}

		//__AS3__.vec$Vector@insertAt
		[NativeFunction("__AS3__.vec$Vector@insertAt")]
		public static void Vector_insertAt(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr].facility;
			var vecinstance = context.GC.Heap[thisPtr.HeapPtr];

			RtVector vector;
			int vecPtr = RtVector.FindAndUpdateHeapInstancePtr(thisPtr.HeapPtr, context.player, out vector);

			if (vector.GetStore(context.player).isFixed)
			{
				context.player.RaiseRangeError(ref error, "Cannot change the length of a fixed Vector.");
				return;
			}

			int len = vector.GetStore(context.player).length;
			int index = scope.ReadSlot(0, context.player).IntValue;
			NaNBoxing element = scope.ReadSlot(1, context.player);

			if (index < 0)
			{
				index = len + index;
			}

			if (index < 0 || index > len)
			{
				context.player.RaiseRangeError(ref error, "Index out of range.");
				return;
			}

			context.player.ConvertValueType(ref error, element,
				((ASInstance)vecinstance.Type)._element_class == null ? TypeKind.Any : (TypeKind)((ASInstance)vecinstance.Type)._element_class.Type_identifier,
				((ASInstance)vecinstance.Type)._element_class, ref context.StackSlots[returnSlotIndex], scope_ptr
			);

			if (error.raised)
			{
				return;
			}

			vector.Resize(len + 1, ref error, context.player, (ASInstance)vecinstance.Type);
			if (error.raised)
			{
				return;
			}

			RtVector vectorAfterResize;
			int vptr = RtVector.FindAndUpdateHeapInstancePtr(thisPtr.HeapPtr, context.player, out vectorAfterResize);

			var store = vectorAfterResize.GetStore(context.player);
			var span = CollectionsMarshal.AsSpan(store.buffer);
			int elementSize = store.elementSize;

			if (index < len)
			{
				for (int i = len - 1; i >= index; i--)
				{
					var srcSlice = span.Slice(i * elementSize, elementSize);
					var dstSlice = span.Slice((i + 1) * elementSize, elementSize);
					srcSlice.CopyTo(dstSlice);
				}
			}

			var slice = span.Slice(index * elementSize, elementSize);
			NaNBoxing value = context.StackSlots[returnSlotIndex];

			context.player.ConvertValueType(ref error, value,
				((ASInstance)vecinstance.Type)._element_class == null ? TypeKind.Any : (TypeKind)((ASInstance)vecinstance.Type)._element_class.Type_identifier,
				((ASInstance)vecinstance.Type)._element_class, ref context.StackSlots[returnSlotIndex], scope_ptr
			);

			if (error.raised) //但是这里肯定不会失败的,因为传参时已经做过类型转换了
			{
				return;
			}

			vectorAfterResize.SetSlot(index, context.player, vptr, context.StackSlots[returnSlotIndex], ref error);
		}


		//__AS3__.vec$Vector@reverse
		[NativeFunction("__AS3__.vec$Vector@reverse")]
		public static void Vector_reverse(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr].facility;
			var vecinstance = context.GC.Heap[thisPtr.HeapPtr];

			RtVector vector;
			int vecPtr = RtVector.FindAndUpdateHeapInstancePtr(thisPtr.HeapPtr, context.player, out vector);

			int last = vector.GetStore(context.player).length - 1;
			int st = 0;


			var store = vector.GetStore(context.player);
			var span = CollectionsMarshal.AsSpan(store.buffer);
			int elementSize = store.elementSize;

			Span<byte> temp = stackalloc byte[elementSize];

			while (st < last)
			{
				var a = span.Slice(st * elementSize, elementSize);
				var b = span.Slice(last * elementSize, elementSize);

				a.CopyTo(temp);
				b.CopyTo(a);
				temp.CopyTo(b);

				st++;
				last--;
			}

			//if (index < len - 1)
			//{
			//	var store = vector.GetStore(context.player);
			//	var span = CollectionsMarshal.AsSpan(store.buffer);
			//	int elementSize = store.elementSize;
			//	for (int i = index; i < len - 1; i++)
			//	{
			//		var srcSlice = span.Slice((i + 1) * elementSize, elementSize);
			//		var dstSlice = span.Slice(i * elementSize, elementSize);
			//		srcSlice.CopyTo(dstSlice);
			//	}
			//}

			//vector.Resize(len - 1, ref error, context.player, (ASInstance)vecinstance.Type);
			//Debug.Assert(!error.raised);
		}

		//__AS3__.vec$Vector@sort
		[NativeFunction("__AS3__.vec$Vector@sort")]
		public static void Vector_sort(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr].facility;
			
			
			RtVector vector;
			int vecPtr = RtVector.FindAndUpdateHeapInstancePtr(scope.ThisPtr.HeapPtr, context.player, out vector);

			var store = vector.GetStore(context.player);
			int len = store.length;


			context.StackSlots[returnSlotIndex].SetHeapPtr(vecPtr); //保持到槽，防止GC

			if (context.StackPosition + 3 >= Context.STACK_LENGTH)
			{
				
				context.player.RaiseStackOverflow(ref error);
				return;
			}

			NaNBoxing sortBehavior = scope.ReadSlot(0, context.player);

			if (sortBehavior.ValueType == BoxType.LocalString || sortBehavior.ValueType == BoxType.Null || sortBehavior.ValueType == BoxType.Undefined)
			{
				
				context.player.RaiseTypeError(ref error, sortBehavior, TypeKind.Function);
				return;
			}

			int basePos = context.StackPosition;
			context.StackPosition += 1;

			context.StackSlots[basePos].SetUndefined();

			if (sortBehavior.ValueType == BoxType.HeapPtr)
			{
				context.player.ConvertValueType(ref error, sortBehavior, TypeKind.Function, context.FUNCTION, ref context.StackSlots[basePos]);
				if (error.raised)
				{
					
					context.StackPosition = basePos;
					return;
				}
			}

			SortHelper.QuickSort( scope, scope_ptr, context, ref error, sortBehavior);

			context.StackPosition = basePos;

			
		}

		private static int comparer(NaNBoxing a, NaNBoxing b, NaNBoxing sortBehavior, Context context, int scope_ptr, ref ReceiveError error)
		{
			if (sortBehavior.ValueType == BoxType.HeapPtr)
			{
				RtHeapBase func = context.GC.Heap[sortBehavior.HeapPtr];
				RtClosure closure = (RtClosure)func.facility;

				ASMethod method = ((ASMethodBody)func.Type).Method;

				if (context.StackPosition + 3 >= Context.STACK_LENGTH)
				{
					context.player.RaiseStackOverflow(ref error);
					return 0;
				}

				unsafe
				{
					StackLocater* args = stackalloc StackLocater[2];
					args->index = 0;
					(args + 1)->index = 1;

					var slots = context.StackSlots.AsSpan(context.StackPosition + 1, 2);
					slots.Clear();

					slots[0] = a;
					slots[1] = b;

					int basePos = context.StackPosition;

					context.StackPosition += 3;

					context.player.RunMethod(method, closure.This, closure.ScopePtr, closure.ScopeType, 2, (byte*)args, slots, ref error, basePos);

					if (error.raised)
					{
						context.StackPosition = basePos;
						return 0;
					}

					context.player.ConvertValueType(ref error, context.StackSlots[basePos], TypeKind.Number, context.NUMBER, ref slots[0], scope_ptr);
					context.StackPosition = basePos;

					double v = slots[0].Number;
					if (v > 0)
						return 1;
					else if (v == 0 || double.IsNaN(v))
						return 0;
					else
						return -1;

				}

			}
			else
			{
				context.player.ConvertValueType(ref error, sortBehavior, TypeKind.Int, context.INT, ref sortBehavior); //这里不可能出错
				int option = sortBehavior.IntValue;

				if ((option & 16) == 16)
				{
					//转数字
					context.StackPosition++;
					context.StackSlots[context.StackPosition - 1].SetUndefined();
					context.player.ConvertValueType(ref error, a, TypeKind.Number, context.NUMBER, ref context.StackSlots[context.StackPosition - 1], scope_ptr);
					if (error.raised)
					{
						context.StackPosition--;
						return 0;
					}

					double v1 = context.StackSlots[context.StackPosition - 1].Number;

					context.player.ConvertValueType(ref error, b, TypeKind.Number, context.NUMBER, ref context.StackSlots[context.StackPosition - 1], scope_ptr);
					if (error.raised)
					{
						context.StackPosition--;
						return 0;
					}

					double v2 = context.StackSlots[context.StackPosition - 1].Number;
					if (error.raised)
					{
						context.StackPosition--;
						return 0;
					}

					context.StackPosition--;

					if (double.IsNaN(v1) && double.IsNaN(v2))
					{
						return 0;
					}
					else if (double.IsNaN(v1))
					{
						return 1;
					}
					else if (double.IsNaN(v2))
					{
						return -1;
					}
					else if ((option & 2) == 2)
					{
						if (v1 == v2)
							return 0;
						else if (v1 < v2)
							return 1;
						else
							return -1;
					}
					else
					{
						if (v1 == v2)
							return 0;
						else if (v1 > v2)
							return 1;
						else
							return -1;
					}
				}
				else
				{
					//字符串比较


					context.StackSlots[context.StackPosition].SetUndefined();
					context.StackSlots[context.StackPosition + 1].SetUndefined();

					context.StackPosition += 2;
					context.GC.CheckGC(ref error);

					context.player.ConvertValueType(ref error, a, TypeKind.String, context.STRING, ref context.StackSlots[context.StackPosition - 2], scope_ptr);
					if (error.raised)
					{
						context.StackPosition -= 2;
						return 0;
					}

					context.player.ConvertValueType(ref error, b, TypeKind.String, context.STRING, ref context.StackSlots[context.StackPosition - 1], scope_ptr);
					if (error.raised)
					{
						context.StackPosition -= 2;
						return 0;
					}

					//unsafe
					{

						
						Span<char> temp1 = stackalloc char[16];
						ReadOnlySpan<char> chars1 = temp1;
						NaNBoxing box1 = context.StackSlots[context.StackPosition - 2];
						if (box1.ValueType == BoxType.HeapPtr)
						{
							string v1 = ((RtString)context.GC.Heap[box1.HeapPtr].facility).Str;
							chars1 = v1.AsSpan();
						}
						else
						{
							Debug.Assert(box1.ValueType == BoxType.LocalString);
							int len = box1.GetLocalStringChars(temp1);
							chars1 = temp1.Slice(0, len);
						}


						
						Span<char> temp2 = stackalloc char[16];
						ReadOnlySpan<char> chars2 = temp2;
						ref NaNBoxing box2 = ref context.StackSlots[context.StackPosition - 1];
						if (box2.ValueType == BoxType.HeapPtr)
						{
							string v = ((RtString)context.GC.Heap[box2.HeapPtr].facility).Str;
							chars2 = v.AsSpan();
						}
						else
						{
							Debug.Assert(box2.ValueType == BoxType.LocalString);
							int len = box2.GetLocalStringChars(temp2);
							chars2 = temp2.Slice(0, len);
						}

						context.StackPosition -= 2;

						int comp = chars1.CompareTo(chars2, (option & 1) == 1 ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal); //.Compare(v1, v2, (option & 1) == 1);
						if ((option & 2) == 2)
							return -comp;
						else
							return comp;

					}
				}


				//throw new NotImplementedException();
			}


		}

		static class SortHelper
		{
			public static void QuickSort(RtMethodScope scope, int scope_ptr, Context context, ref ReceiveError error, NaNBoxing sortBehavior)
			{
				
				RtVector vpayload;
				int vecPtr = RtVector.FindAndUpdateHeapInstancePtr(scope.ThisPtr.HeapPtr, context.player, out vpayload);
				var store = vpayload.GetStore(context.player);

				if (store == null || store.length <= 1) return;
				QuickSort(scope,vpayload,vecPtr, scope_ptr, 0, store.length - 1, context, ref error, sortBehavior);
			}

			private static void QuickSort(RtMethodScope scope, RtVector vpayload, int vecptr ,int scope_ptr, int left, int right, Context context, ref ReceiveError error, NaNBoxing sortBehavior)
			{
				if (left >= right) return;

				int pivotIndex = Partition(scope, scope_ptr, vecptr, left, right, context, ref error, sortBehavior);
				if (error.raised)
				{
					return;
				}

				vecptr = RtVector.FindAndUpdateHeapInstancePtr(vecptr, context.player, out vpayload);

				QuickSort(scope, vpayload,vecptr, scope_ptr, left, pivotIndex - 1, context, ref error, sortBehavior);
				if (error.raised)
				{
					return;
				}

				QuickSort(scope,vpayload,vecptr, scope_ptr, pivotIndex + 1, right, context, ref error, sortBehavior);
				if (error.raised)
				{
					return;
				}
			}

			private static int Partition(RtMethodScope scope, int scope_ptr, int vecptr, int left, int right,
				Context context, ref ReceiveError error, NaNBoxing sortBehavior)
			{
				RtVector vpayload;
				int vecPtr = RtVector.FindAndUpdateHeapInstancePtr(scope.ThisPtr.HeapPtr, context.player, out vpayload);
				var store = vpayload.GetStore(context.player);


				if (context.StackPosition + 2 >= Context.STACK_LENGTH)
				{
					context.player.RaiseStackOverflow(ref error);
					return 0;
				}

				int basePos = context.StackPosition;

				context.StackPosition += 2;
				//T pivot = arr[right];
				NaNBoxing pivot = vpayload.ReadSlot(right, context.player, basePos, vecptr);

				int i = left - 1;
				for (int j = left; j < right; j++)
				{
					NaNBoxing test = vpayload.ReadSlot(j, context.player, basePos + 1, vecptr);

					int olen = store.length;

					int comp = VectorImpl.comparer(test, pivot, sortBehavior, context, scope_ptr, ref error);
					if (error.raised)
					{
						context.StackPosition -= 2;
						return 0;
					}

					vecPtr = RtVector.FindAndUpdateHeapInstancePtr(scope.ThisPtr.HeapPtr, context.player, out vpayload);
					store = vpayload.GetStore(context.player);
					if (store.length != olen)
					{
						context.player.RaiseError(ref error, "vector length changed!");
						context.StackPosition -= 2;
						return 0;
					}

					if (comp < 0)
					{
						i++;
						Swap(store, i, j);
					}
				}

				Swap(store, i + 1, right);

				context.StackPosition -= 2;

				return i + 1;
			}

			private static void Swap(VectorStore store, int a, int b)
			{
				var span = CollectionsMarshal.AsSpan(store.buffer);
				int elementSize = store.elementSize;

				Span<byte> temp = stackalloc byte[elementSize];

				var v1 = span.Slice(a * elementSize, elementSize);
				var v2 = span.Slice(b * elementSize, elementSize);

				v1.CopyTo(temp);
				v2.CopyTo(v1);
				temp.CopyTo(v2);

			}
		}

		//__AS3__.vec$Vector@splice
		[NativeFunction("__AS3__.vec$Vector@splice")]
		public static void Vector_splice(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{

			var scope = (RtMethodScope)context.GC.Heap[scope_ptr].facility;
			var vecinstance = context.GC.Heap[thisPtr.HeapPtr];

			TypeKind elementkind =
			((ASInstance)vecinstance.Type)._element_class == null ? TypeKind.Any : (TypeKind)((ASInstance)vecinstance.Type)._element_class.Type_identifier;
			ASClass elementcls = ((ASInstance)vecinstance.Type)._element_class;
			ASInstance vType = (ASInstance)vecinstance.Type;
			vecinstance = null;


			RtVector vector;
			int vecPtr = RtVector.FindAndUpdateHeapInstancePtr(thisPtr.HeapPtr, context.player, out vector);

			var store = vector.GetStore(context.player);
			int len = store.length;
			int elementSize = store.elementSize;

			var span = CollectionsMarshal.AsSpan(store.buffer);

			int startIndex = scope.ReadSlot(0, context.player).IntValue;

			uint deleteCount;
			if (scope.SlotCount > 1)
			{
				deleteCount = scope.ReadSlot(1, context.player).UIntValue;
			}
			else
			{
				deleteCount = uint.MaxValue;
			}

			if (startIndex < 0)
			{
				startIndex = len + startIndex;
			}

			if (startIndex < 0)
			{
				startIndex = 0;
			}

			if (startIndex > len)
			{
				startIndex = len;
			}

			uint actualDeleteCount = (uint)(len - startIndex);
			if (deleteCount < actualDeleteCount)
			{
				actualDeleteCount = deleteCount;
			}

			bool willChangeLength = actualDeleteCount > 0;

			var rest_array = new RtArray();
			int insertCount = 0;

			if (scope.SlotCount > 2)
			{
				var rest = scope.ReadSlot(2, context.player);
				rest_array = (RtArray)context.GC.Heap[rest.HeapPtr].facility;
				insertCount = rest_array.stack_store.Span.Length;
			}

			willChangeLength = willChangeLength || insertCount > 0;

			if (willChangeLength && store.isFixed)
			{
				context.player.RaiseRangeError(ref error, "Cannot change the length of a fixed Vector.");
				return;
			}


			int resultVecPtr = context.CacheVectorPtr + returnSlotIndex;
			var resultInstance = context.GC.Heap[resultVecPtr];
			resultInstance.Type = vType;
			((RtVector)resultInstance.facility).HEAPINSTANCE_PTR = 0;
			((RtVector)resultInstance.facility).element_asclass = elementcls;
			((RtVector)resultInstance.facility).element_type = elementkind;
			((RtVector)resultInstance.facility).GetStore(context.player).SetBuffer(0);
			((RtVector)resultInstance.facility).GetStore(context.player).length = 0;
			((RtVector)resultInstance.facility).GetStore(context.player).elementSize = elementSize;

			var resultVector = (RtVector)resultInstance.facility;

			if (actualDeleteCount > 0)
			{
				resultVector.Resize((int)actualDeleteCount, ref error, context.player, (ASInstance)resultInstance.Type);
				if (error.raised)
				{
					return;
				}

				RtVector.FindAndUpdateHeapInstancePtr(resultVecPtr, context.player, out resultVector);

				var resultStore = resultVector.GetStore(context.player);
				var resultSpan = CollectionsMarshal.AsSpan(resultStore.buffer);

				for (int i = 0; i < actualDeleteCount; i++)
				{
					var srcSlice = span.Slice((startIndex + i) * elementSize, elementSize);
					var dstSlice = resultSpan.Slice(i * elementSize, elementSize);
					srcSlice.CopyTo(dstSlice);
				}
			}

			context.StackSlots[returnSlotIndex].SetHeapPtr(resultVecPtr);

			if (actualDeleteCount > 0 || insertCount > 0)
			{
				int newLen = len - (int)actualDeleteCount + insertCount;

				RtVector vectorAfterResize;
				int vptr = RtVector.FindAndUpdateHeapInstancePtr(thisPtr.HeapPtr, context.player, out vectorAfterResize);

			
				

				int moveOffset = -(int)actualDeleteCount + insertCount;
				if (moveOffset < 0)
				{
					var newStore = vectorAfterResize.GetStore(context.player);
					var newSpan = CollectionsMarshal.AsSpan(newStore.buffer);


					for (int i = 0; i < len - startIndex - (int)actualDeleteCount; i++)
					{
						var srcSlice = newSpan.Slice((startIndex  + i - moveOffset) * elementSize, elementSize);
						var dstSlice = newSpan.Slice((startIndex  + i) * elementSize, elementSize);


						srcSlice.CopyTo(dstSlice);
					}

					vectorAfterResize.Resize(newLen, ref error, context.player, (ASInstance)resultInstance.Type); //整体变少
					if (error.raised)
					{
						return;
					}
				}
				else if (moveOffset > 0)
				{
					vectorAfterResize.Resize(newLen, ref error, context.player, (ASInstance)resultInstance.Type); //数量变多
					if (error.raised)
					{
						return;
					}
					var newStore = vectorAfterResize.GetStore(context.player);
					var newSpan = CollectionsMarshal.AsSpan(newStore.buffer);


					for (int i = len - startIndex - (int)actualDeleteCount -1; i >=0; i--) //拷贝方向不同
					{
						var srcSlice = newSpan.Slice((startIndex + (int)actualDeleteCount + i  ) * elementSize, elementSize);
						var dstSlice = newSpan.Slice((startIndex + (int)actualDeleteCount + i + moveOffset) * elementSize, elementSize);


						srcSlice.CopyTo(dstSlice);
					}

				}


				if (insertCount > 0)
				{
					if (context.StackPosition + 1 >= Context.STACK_LENGTH)
					{
						context.player.RaiseStackOverflow(ref error);
						return;
					}

					int basePos = context.StackPosition;
					context.StackPosition += 1;

					var argsSpan = rest_array.stack_store.Span;
					for (int i = 0; i < insertCount; i++)
					{
						NaNBoxing item = argsSpan[i];
						context.player.ConvertValueType(ref error, item,
							 elementkind, elementcls , ref context.StackSlots[basePos], scope_ptr
						);

						if (error.raised)
						{
							context.StackPosition = basePos;
							return;
						}

						vectorAfterResize.SetSlot(startIndex + i, context.player, vptr, context.StackSlots[basePos], ref error);
						if (error.raised)
						{
							context.StackPosition = basePos;
							return;
						}
					}

					context.StackPosition = basePos;
				}
			}

		}

		//__AS3__.vec$Vector@every
		[NativeFunction("__AS3__.vec$Vector@every")]
		public static void Vector_every(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr].facility;
			
			RtVector vector;
			int vecPtr = RtVector.FindAndUpdateHeapInstancePtr(scope.ThisPtr.HeapPtr, context.player, out vector);

			var store = vector.GetStore(context.player);
			int len = store.length;
			


			context.StackSlots[returnSlotIndex].SetHeapPtr(vecPtr); //保持到槽，防止GC

			if (context.StackPosition + 5 >= Context.STACK_LENGTH)
			{
				context.player.RaiseStackOverflow(ref error);
				return;
			}

			var cb = scope.ReadSlot(0, context.player);
			if (cb.ValueType != BoxType.HeapPtr)
			{
				context.player.RaiseTypeError(ref error, cb, TypeKind.Function);
				return;
			}

			var _this = scope.ReadSlot(1,context.player);

			
			var cbmethod = ((ASMethodBody)context.GC.Heap[cb.HeapPtr].Type).Method;
			var cbclosure = (RtClosure)context.GC.Heap[cb.HeapPtr].facility;

			if (cbmethod.__ismethod && !cbmethod.__is_call_or_apply)
			{
				_this = cbclosure.This;
			}
			else if (cbmethod.__is_hasOwnProperty)
			{

			}
			else if (_this.ValueType == NaNBoxing.BoxType.Undefined || _this.ValueType == NaNBoxing.BoxType.Null)
			{

				var sss = context.GC.Heap[cb.HeapPtr].Type._link_codescope.Parent; //Context.GC.Heap[scope_ptr].Type._link_codescope.Parent;
				while (sss.Kind != CodeScopeKind.Script)
				{
					sss = sss.Parent;
				}

				var globalptr = ((ASScript)sss.Container).__global_index__;
				_this.SetHeapPtr(globalptr);

			}



			int basePos = context.StackPosition;			
			var argSlots = context.StackSlots.AsSpan(basePos,5);
			argSlots.Clear();

			context.StackPosition += 5;

			unsafe
			{


				StackLocater* args = stackalloc StackLocater[3];
				args[0].index = 2;
				args[1].index = 3;
				args[2].index = 4;

				bool isEvery = true;
				int olen = len;
				for (int i = 0; i < len && i<olen; i++)
				{
					NaNBoxing v = vector.ReadSlot(i, context.player, basePos, vecPtr);

					argSlots[2] = v;
					argSlots[3].SetInt(i);
					argSlots[4].SetHeapPtr(vecPtr);

					NaNBoxing r = context.player.RunMethod(cbmethod, _this, cbclosure.ScopePtr, cbclosure.ScopeType, 3, (byte*)args, argSlots, ref error, basePos+1);
					if (error.raised)
					{
						context.StackPosition -= 5;
						return;
					}

					
					vecPtr = RtVector.FindAndUpdateHeapInstancePtr(scope.ThisPtr.HeapPtr, context.player, out vector);

					store = vector.GetStore(context.player);
					len = store.length;

					context.player.ConvertValueType(ref error, r, TypeKind.Boolean, context.BOOLEAN, ref r);
					Debug.Assert(!error.raised); //转BOOLEAN不会失败


					isEvery = isEvery & r.Boolean;
					if (!isEvery)
						break;
				}

				context.StackSlots[returnSlotIndex].SetBoolean(isEvery);

			}


			context.StackPosition -= 5;

		}


		//__AS3__.vec$Vector@some
		[NativeFunction("__AS3__.vec$Vector@some")]
		public static void Vector_some(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr].facility;
			
			RtVector vector;
			int vecPtr = RtVector.FindAndUpdateHeapInstancePtr(scope.ThisPtr.HeapPtr, context.player, out vector);

			var store = vector.GetStore(context.player);
			int len = store.length;



			context.StackSlots[returnSlotIndex].SetHeapPtr(vecPtr); //保持到槽，防止GC

			if (context.StackPosition + 5 >= Context.STACK_LENGTH)
			{
				context.player.RaiseStackOverflow(ref error);
				return;
			}

			var cb = scope.ReadSlot(0, context.player);
			if (cb.ValueType != BoxType.HeapPtr)
			{
				context.player.RaiseTypeError(ref error, cb, TypeKind.Function);
				return;
			}

			var _this = scope.ReadSlot(1, context.player);


			var cbmethod = ((ASMethodBody)context.GC.Heap[cb.HeapPtr].Type).Method;
			var cbclosure = (RtClosure)context.GC.Heap[cb.HeapPtr].facility;

			if (cbmethod.__ismethod && !cbmethod.__is_call_or_apply)
			{
				_this = cbclosure.This;
			}
			else if (cbmethod.__is_hasOwnProperty)
			{

			}
			else if (_this.ValueType == NaNBoxing.BoxType.Undefined || _this.ValueType == NaNBoxing.BoxType.Null)
			{

				var sss = context.GC.Heap[cb.HeapPtr].Type._link_codescope.Parent; //Context.GC.Heap[scope_ptr].Type._link_codescope.Parent;
				while (sss.Kind != CodeScopeKind.Script)
				{
					sss = sss.Parent;
				}

				var globalptr = ((ASScript)sss.Container).__global_index__;
				_this.SetHeapPtr(globalptr);

			}



			int basePos = context.StackPosition;
			var argSlots = context.StackSlots.AsSpan(basePos, 5);
			argSlots.Clear();

			context.StackPosition += 5;

			unsafe
			{


				StackLocater* args = stackalloc StackLocater[3];
				args[0].index = 2;
				args[1].index = 3;
				args[2].index = 4;

				bool issome = false;
				int olen = len;
				for (int i = 0; i < len && i < olen; i++)
				{
					NaNBoxing v = vector.ReadSlot(i, context.player, basePos, vecPtr);

					argSlots[2] = v;
					argSlots[3].SetInt(i);
					argSlots[4].SetHeapPtr(vecPtr);

					NaNBoxing r = context.player.RunMethod(cbmethod, _this, cbclosure.ScopePtr, cbclosure.ScopeType, 3, (byte*)args, argSlots, ref error, basePos + 1);
					if (error.raised)
					{
						context.StackPosition -= 5;
						return;
					}


					vecPtr = RtVector.FindAndUpdateHeapInstancePtr(scope.ThisPtr.HeapPtr, context.player, out vector);

					store = vector.GetStore(context.player);
					len = store.length;

					context.player.ConvertValueType(ref error, r, TypeKind.Boolean, context.BOOLEAN, ref r);
					Debug.Assert(!error.raised); //转BOOLEAN不会失败

					if (r.Boolean)
					{
						issome = true;
						break;
					}
					
				}

				context.StackSlots[returnSlotIndex].SetBoolean(issome);

			}


			context.StackPosition -= 5;

		}


		//__AS3__.vec$Vector@filter
		[NativeFunction("__AS3__.vec$Vector@filter")]
		public static void Vector_filter(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr].facility;
			var vecinstance = context.GC.Heap[thisPtr.HeapPtr];
			TypeKind elementkind =
			((ASInstance)vecinstance.Type)._element_class == null ? TypeKind.Any : (TypeKind)((ASInstance)vecinstance.Type)._element_class.Type_identifier;
			ASClass elementcls = ((ASInstance)vecinstance.Type)._element_class;
			ASInstance vType = (ASInstance)vecinstance.Type;
			vecinstance = null;

			RtVector vector;
			int vecPtr = RtVector.FindAndUpdateHeapInstancePtr(scope.ThisPtr.HeapPtr, context.player, out vector);

			var store = vector.GetStore(context.player);
			int len = store.length;
			int elementSize = store.elementSize;

			
			context.StackSlots[returnSlotIndex].SetHeapPtr(vecPtr); //保持到槽，防止GC

			if (context.StackPosition + 5 >= Context.STACK_LENGTH)
			{
				context.player.RaiseStackOverflow(ref error);
				return;
			}

			var cb = scope.ReadSlot(0, context.player);
			if (cb.ValueType != BoxType.HeapPtr)
			{
				context.player.RaiseTypeError(ref error, cb, TypeKind.Function);
				return;
			}

			var _this = scope.ReadSlot(1, context.player);
			var cbmethod = ((ASMethodBody)context.GC.Heap[cb.HeapPtr].Type).Method;
			var cbclosure = (RtClosure)context.GC.Heap[cb.HeapPtr].facility;

			if (cbmethod.__ismethod && !cbmethod.__is_call_or_apply)
			{
				_this = cbclosure.This;
			}
			else if (cbmethod.__is_hasOwnProperty)
			{

			}
			else if (_this.ValueType == NaNBoxing.BoxType.Undefined || _this.ValueType == NaNBoxing.BoxType.Null)
			{

				var sss = context.GC.Heap[cb.HeapPtr].Type._link_codescope.Parent; //Context.GC.Heap[scope_ptr].Type._link_codescope.Parent;
				while (sss.Kind != CodeScopeKind.Script)
				{
					sss = sss.Parent;
				}

				var globalptr = ((ASScript)sss.Container).__global_index__;
				_this.SetHeapPtr(globalptr);

			}

			//目标
			int resultVecPtr = context.CacheVectorPtr + returnSlotIndex;
			var resultInstance = context.GC.Heap[resultVecPtr];
			resultInstance.Type = vType;
			((RtVector)resultInstance.facility).HEAPINSTANCE_PTR = 0;
			((RtVector)resultInstance.facility).element_asclass = elementcls;
			((RtVector)resultInstance.facility).element_type = elementkind;
			((RtVector)resultInstance.facility).GetStore(context.player).SetBuffer(0);
			((RtVector)resultInstance.facility).GetStore(context.player).length = 0;
			((RtVector)resultInstance.facility).GetStore(context.player).elementSize = elementSize;

			var resultVector = (RtVector)resultInstance.facility;

			int basePos = context.StackPosition;
			var argSlots = context.StackSlots.AsSpan(basePos, 5);
			argSlots.Clear();



			context.StackPosition += 5;

			unsafe
			{


				StackLocater* args = stackalloc StackLocater[3];
				args[0].index = 2;
				args[1].index = 3;
				args[2].index = 4;

				int newlen = 0;int olen = len;
				for (int i = 0; i < len && i<olen; i++)
				{
					NaNBoxing v = vector.ReadSlot(i, context.player, basePos, vecPtr);

					argSlots[2] = v;
					argSlots[3].SetInt(i);
					argSlots[4].SetHeapPtr(vecPtr);
					NaNBoxing r = context.player.RunMethod(cbmethod, _this, cbclosure.ScopePtr, cbclosure.ScopeType, 3, (byte*)args, argSlots, ref error, basePos + 1);
					if (error.raised)
					{
						context.StackPosition -= 5;
						return;
					}

					
					vecPtr = RtVector.FindAndUpdateHeapInstancePtr(scope.ThisPtr.HeapPtr, context.player, out vector);

					store = vector.GetStore(context.player);
					len = store.length;

					context.player.ConvertValueType(ref error, r, TypeKind.Boolean, context.BOOLEAN, ref r);
					Debug.Assert(!error.raised); //转BOOLEAN不会失败

					
					if (r.Boolean)
					{
						resultVector.Resize(newlen + 1, ref error, context.player, vType);
						if (error.raised)
						{
							context.StackPosition -= 5;
							return;
						}
						resultVecPtr = RtVector.FindAndUpdateHeapInstancePtr(resultVecPtr, context.player, out resultVector);
						resultVector.SetSlot(newlen, context.player, resultVecPtr, v, ref error);
						if (error.raised)
						{
							context.StackPosition -= 5;
							return;
						}
						newlen++;
					}
				}

				context.StackSlots[returnSlotIndex].SetHeapPtr(resultVecPtr);

			}


			context.StackPosition -= 5;




		}

		//__AS3__.vec$Vector@map
		[NativeFunction("__AS3__.vec$Vector@map")]
		public static void Vector_map(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr].facility;
			var vecinstance = context.GC.Heap[thisPtr.HeapPtr];
			TypeKind elementkind =
			((ASInstance)vecinstance.Type)._element_class == null ? TypeKind.Any : (TypeKind)((ASInstance)vecinstance.Type)._element_class.Type_identifier;
			ASClass elementcls = ((ASInstance)vecinstance.Type)._element_class;
			ASInstance vType = (ASInstance)vecinstance.Type;
			vecinstance = null;

			RtVector vector;
			int vecPtr = RtVector.FindAndUpdateHeapInstancePtr(scope.ThisPtr.HeapPtr, context.player, out vector);

			var store = vector.GetStore(context.player);
			int len = store.length;
			int elementSize = store.elementSize;


			context.StackSlots[returnSlotIndex].SetHeapPtr(vecPtr); //保持到槽，防止GC

			if (context.StackPosition + 6 >= Context.STACK_LENGTH)
			{
				context.player.RaiseStackOverflow(ref error);
				return;
			}

			var cb = scope.ReadSlot(0, context.player);
			if (cb.ValueType != BoxType.HeapPtr)
			{
				context.player.RaiseTypeError(ref error, cb, TypeKind.Function);
				return;
			}

			var _this = scope.ReadSlot(1, context.player);
			var cbmethod = ((ASMethodBody)context.GC.Heap[cb.HeapPtr].Type).Method;
			var cbclosure = (RtClosure)context.GC.Heap[cb.HeapPtr].facility;

			if (cbmethod.__ismethod && !cbmethod.__is_call_or_apply)
			{
				_this = cbclosure.This;
			}
			else if (cbmethod.__is_hasOwnProperty)
			{

			}
			else if (_this.ValueType == NaNBoxing.BoxType.Undefined || _this.ValueType == NaNBoxing.BoxType.Null)
			{

				var sss = context.GC.Heap[cb.HeapPtr].Type._link_codescope.Parent; //Context.GC.Heap[scope_ptr].Type._link_codescope.Parent;
				while (sss.Kind != CodeScopeKind.Script)
				{
					sss = sss.Parent;
				}

				var globalptr = ((ASScript)sss.Container).__global_index__;
				_this.SetHeapPtr(globalptr);

			}

			//目标
			int resultVecPtr = context.CacheVectorPtr + returnSlotIndex;
			var resultInstance = context.GC.Heap[resultVecPtr];
			resultInstance.Type = vType;
			((RtVector)resultInstance.facility).HEAPINSTANCE_PTR = 0;
			((RtVector)resultInstance.facility).element_asclass = elementcls;
			((RtVector)resultInstance.facility).element_type = elementkind;
			((RtVector)resultInstance.facility).GetStore(context.player).SetBuffer(0);
			((RtVector)resultInstance.facility).GetStore(context.player).length = 0;
			((RtVector)resultInstance.facility).GetStore(context.player).elementSize = elementSize;

			var resultVector = (RtVector)resultInstance.facility;

			int basePos = context.StackPosition;
			var argSlots = context.StackSlots.AsSpan(basePos, 6);
			argSlots.Clear();



			context.StackPosition += 6;

			unsafe
			{


				StackLocater* args = stackalloc StackLocater[3];
				args[0].index = 2;
				args[1].index = 3;
				args[2].index = 4;

				int newlen = 0; int olen = len;
				for (int i = 0; i < len && i < olen; i++)
				{
					NaNBoxing v = vector.ReadSlot(i, context.player, basePos, vecPtr);

					argSlots[2] = v;
					argSlots[3].SetInt(i);
					argSlots[4].SetHeapPtr(vecPtr);
					NaNBoxing r = context.player.RunMethod(cbmethod, _this, cbclosure.ScopePtr, cbclosure.ScopeType, 3, (byte*)args, argSlots, ref error, basePos + 1);
					if (error.raised)
					{
						context.StackPosition -= 6;
						return;
					}


					vecPtr = RtVector.FindAndUpdateHeapInstancePtr(scope.ThisPtr.HeapPtr, context.player, out vector);

					store = vector.GetStore(context.player);
					len = store.length;



					context.player.ConvertValueType(ref error, r, elementkind , elementcls, ref argSlots[5]);
					if (error.raised)
					{
						context.StackPosition -= 6;
						return;
					}

					resultVector.Resize(newlen + 1, ref error, context.player, vType);
					if (error.raised)
					{
						context.StackPosition -= 6;
						return;
					}
					resultVecPtr = RtVector.FindAndUpdateHeapInstancePtr(resultVecPtr, context.player, out resultVector);
					resultVector.SetSlot(newlen, context.player, resultVecPtr, argSlots[5] , ref error);
					if (error.raised)
					{
						context.StackPosition -= 6;
						return;
					}
					newlen++;
					
				}

				context.StackSlots[returnSlotIndex].SetHeapPtr(resultVecPtr);

			}


			context.StackPosition -= 6;




		}


		//__AS3__.vec$Vector@forEach
		[NativeFunction("__AS3__.vec$Vector@forEach")]
		public static void Vector_forEach(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{

			var scope = (RtMethodScope)context.GC.Heap[scope_ptr].facility;
			

			RtVector vector;
			int vecPtr = RtVector.FindAndUpdateHeapInstancePtr(scope.ThisPtr.HeapPtr, context.player, out vector);

			var store = vector.GetStore(context.player);

			//if (store.IsCache)
			//{
			//	vecPtr = vector.ChangeStoreToHeap((ASInstance)vecinstance.Type, context.player, ref error);
			//	store = vector.GetStore(context.player);

			//	NaNBoxing t = default; t.SetHeapPtr(vecPtr);
			//	scope.SetSlot(t, (ushort)(scope.SlotCount - 1));
			//}

			int len = store.length;

			context.StackSlots[returnSlotIndex].SetHeapPtr(vecPtr); //保持到槽，防止GC

			if (context.StackPosition + 5 >= Context.STACK_LENGTH)
			{
				context.player.RaiseStackOverflow(ref error);
				return;
			}

			var cb = scope.ReadSlot(0, context.player);
			if (cb.ValueType != BoxType.HeapPtr)
			{
				context.player.RaiseTypeError(ref error, cb, TypeKind.Function);
				return;
			}

			var _this = scope.ReadSlot(1, context.player);


			var cbmethod = ((ASMethodBody)context.GC.Heap[cb.HeapPtr].Type).Method;
			var cbclosure = (RtClosure)context.GC.Heap[cb.HeapPtr].facility;

			if (cbmethod.__ismethod && !cbmethod.__is_call_or_apply)
			{
				_this = cbclosure.This;
			}
			else if (cbmethod.__is_hasOwnProperty)
			{

			}
			else if (_this.ValueType == NaNBoxing.BoxType.Undefined || _this.ValueType == NaNBoxing.BoxType.Null)
			{

				var sss = context.GC.Heap[cb.HeapPtr].Type._link_codescope.Parent; //Context.GC.Heap[scope_ptr].Type._link_codescope.Parent;
				while (sss.Kind != CodeScopeKind.Script)
				{
					sss = sss.Parent;
				}

				var globalptr = ((ASScript)sss.Container).__global_index__;
				_this.SetHeapPtr(globalptr);

			}



			int basePos = context.StackPosition;
			var argSlots = context.StackSlots.AsSpan(basePos, 5);
			argSlots.Clear();

			context.StackPosition += 5;

			unsafe
			{


				StackLocater* args = stackalloc StackLocater[3];
				args[0].index = 2;
				args[1].index = 3;
				args[2].index = 4;


				int olen = len;
				for (int i = 0; i < len && i < olen; i++)
				{
					NaNBoxing v = vector.ReadSlot(i, context.player, basePos, vecPtr);

					argSlots[2] = v;
					argSlots[3].SetInt(i);
					argSlots[4].SetHeapPtr(vecPtr);
					NaNBoxing r = context.player.RunMethod(cbmethod, _this, cbclosure.ScopePtr, cbclosure.ScopeType, 3, (byte*)args, argSlots, ref error, basePos + 1);
					if (error.raised)
					{
						context.StackPosition -= 5;
						return;
					}

					context.player.ConvertValueType(ref error, r, TypeKind.Boolean, context.BOOLEAN, ref r);
					Debug.Assert(!error.raised); //转BOOLEAN不会失败

					//len = vector.GetStore(context.player).length;
					vecPtr = RtVector.FindAndUpdateHeapInstancePtr(scope.ThisPtr.HeapPtr, context.player, out vector);

					store = vector.GetStore(context.player);
					len =  store.length;
				}

			}


			context.StackPosition -= 5;


		}

		class VectorToString : IPrint
		{
			internal StringBuilder sb;
			public void Write(string message)
			{
				sb.Append(message);
			}

			public void Write(ReadOnlySpan<char> chars)
			{
				sb.Append(chars);
			}

			public void WriteLine(string message)
			{
				sb.AppendLine(message);
			}
		}

		//__AS3__.vec$Vector@toString
		[NativeFunction("__AS3__.vec$Vector@toString")]
		public static void Vector_toString(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{

			var scope = (RtMethodScope)context.GC.Heap[scope_ptr].facility;
			var vecinstance = context.GC.Heap[thisPtr.HeapPtr];

			RtVector vector;
			int vecPtr = RtVector.FindAndUpdateHeapInstancePtr(scope.ThisPtr.HeapPtr, context.player, out vector);

			var store = vector.GetStore(context.player);

			TypeKind elementkind =
			((ASInstance)vecinstance.Type)._element_class == null ? TypeKind.Any : (TypeKind)((ASInstance)vecinstance.Type)._element_class.Type_identifier;
			ASClass elementcls = ((ASInstance)vecinstance.Type)._element_class;


			StringBuilder sb = new StringBuilder();
			VectorToString arrayToString = new VectorToString();
			arrayToString.sb = sb;

			store.DoTrace( elementkind, elementcls, context, stackStPos, ref error, scope_ptr, arrayToString,",");

			string str = sb.ToString();
			if (string.IsNullOrEmpty(str))
			{
				context.StackSlots[returnSlotIndex].SetHeapPtr(context.player.EMPTY_STR);
			}
			else
			{
				int p = context.GC.AllocString(str);
				if (p == 0)
				{
					context.player.RaiseOutOfMemory(ref error);
					return;
				}
				else
				{
					context.StackSlots[returnSlotIndex].SetHeapPtr(p);
				}
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
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr].facility;
			var vecinstance = context.GC.Heap[thisPtr.HeapPtr];

			var vector = (RtVector)vecinstance.facility;
			var store = vector.GetStore(context.player);
			if (store.length == 0)
			{
				context.StackSlots[returnSlotIndex].SetHeapPtr(context.player.EMPTY_STR);
			}
			else
			{
				Span<char> buffer = stackalloc char[16];
				ReadOnlySpan<char> sepStr = buffer;

				NaNBoxing sep = scope.ReadSlot(0, context.player);
				if (sep.ValueType == NaNBoxing.BoxType.Null)
				{
					sepStr = "";
				}
				else
				{
					sepStr = Extensions.GetPrimitiveValueToString(context.player, sep,buffer);
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
			internal static void InitStructSpan(Span<byte> struct_data, ASClass element)
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
								RtInstance.SetSlotDataByValue(member, ptr, member.trait.Value.initValue.Value);
							}
							else
							{
								RtInstance.InitSlotData(member, ptr, element.Instance._link_codescope.TypeLayout.SlotSize[i]);
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
						buffer.AddRange(Enumerable.Repeat<byte>(0, size));

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
					Debug.Assert(size < RtVector.MAX_CACHE_SIZE);
					buffer.AddRange(Enumerable.Repeat<byte>(0, size));

					//buffer.Clear();
				}
			}

			public VectorStore()
			{
				IsCache = true;
				buffer = new List<byte>(RtVector.MAX_CACHE_SIZE);//这里只是保留了内存，而不是实际元素个数！

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

			internal void SetDefault(TypeKind elementkind, ASClass element, int start, int len)
			{
				Span<byte> struct_data = stackalloc byte[RtInstance.MAX_CACHEABLE_SIZE];
				if (element != null && element.Instance.Flags.HasFlag(ClassFlags.Struct))
				{
					InitStructSpan(struct_data, element);
				}

				var span = CollectionsMarshal.AsSpan(buffer);
				//初始化默认值
				for (int i = start; i < start + len; i++)
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

			internal void CopySpan(TypeKind element_type, ASClass element, Span<NaNBoxing> rest_span, Context context)
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

										RtInstance struct_payload = (RtInstance)context.GC.Heap[rest_span[i].HeapPtr].facility;

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



			internal bool IsValidIndexRange(NaNBoxing index, out int valided_index)
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
						return index.SByteValue < length && index.SByteValue >= 0;
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

			internal Span<byte> ReadStoreOffset(int offset, int size)
			{
				return CollectionsMarshal.AsSpan(buffer).Slice(offset, size);
			}


			internal void GCMarkAllElements(Context context)
			{


				var span = CollectionsMarshal.AsSpan(buffer);

				for (int i = 0; i < length; i++)
				{
					var slice = span.Slice(i * elementSize, elementSize);

					NaNBoxing boxing = MemoryMarshal.Read<NaNBoxing>(slice);

					if (boxing.ValueType == NaNBoxing.BoxType.HeapPtr)
					{
						context.GC.mark(context.GC.Heap[boxing.HeapPtr]);
					}

				}


			}

			internal void DoTrace(TypeKind element_type, ASClass element, Context context, int stackStPos, ref ReceiveError error, int scope_ptr, IPrint printer, ReadOnlySpan<char> sep)
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
								NaNBoxing box = default; box.SetBoolean(v);
								TopLevel.TraceElement(box, context, stackStPos, ref error, scope_ptr, default, printer);
							}
							break;
						case TypeKind.SByte:
							{
								sbyte v = MemoryMarshal.Read<sbyte>(slice);
								NaNBoxing box = default; box.SetSByte(v);
								TopLevel.TraceElement(box, context, stackStPos, ref error, scope_ptr, default, printer);
							}
							break;
						case TypeKind.Byte:
							{
								byte v = MemoryMarshal.Read<byte>(slice);
								NaNBoxing box = default; box.SetByte(v);
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
								NaNBoxing box = default; box.SetInt(v);
								TopLevel.TraceElement(box, context, stackStPos, ref error, scope_ptr, default, printer);
							}
							break;
						case TypeKind.Uint:
							{
								uint v = MemoryMarshal.Read<uint>(slice);
								NaNBoxing box = default; box.SetUInt(v);
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

									int p = context.player.InitCacheInstance(element, context.StackPosition, false);
									slice.CopyTo(((RtInstance)context.GC.Heap[p].facility).GetStoreData(context.player, (ASInstance)element.Instance));

									context.StackPosition++;
									TopLevel.TraceElement(context.StackSlots[context.StackPosition - 1], context, stackStPos, ref error, scope_ptr, default, printer);
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
