using juicescript.ABC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static juicescript.runtime.Player;

namespace juicescript.runtime.buildin
{
	internal class GeomImpl
	{
//		[NativeFunction("geom.Vector2$public::Vector2")]
//		public static void Vector2(Context context,
//			ASMethod method,
//			int scope_ptr,
//			NaNBoxing thisPtr,
//			int stackStPos, ref ReceiveError error, int returnSlotIndex)
//		{
//			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

//			var vector2 = context.GC.Heap[thisPtr.HeapPtr];

//			NaNBoxing x = scope.ReadSlot(0, context.player);
//			NaNBoxing y = scope.ReadSlot(1, context.player);

//#if DEBUG
//			if (x.ValueType != NaNBoxing.BoxType.Float || y.ValueType != NaNBoxing.BoxType.Float)
//			{
//				throw new InvalidOperationException();
//			}

//#endif


//			//((RtInstance)vector2).SetSlot(x, 0, ((ASInstance)vector2.Type)._link_codescope  , context.player);
//			//((RtInstance)vector2).SetSlot(y, 1, ((ASInstance)vector2.Type)._link_codescope  , context.player);

//			var store = ((RtInstance)vector2).GetStoreData(context.player, (ASInstance)vector2.Type);
//			unsafe
//			{
//				fixed (byte* p = store)
//				{
//					*(float*)p = x.FloatValue;
//					*((float*)p + 1) = y.FloatValue;
//				}
//			}
//		}

		//geom.Vector2$public::toString
		[NativeFunction("geom.Vector2$public::toString")]
		public static void Vector2_toString(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			var vector2 = context.GC.Heap[thisPtr.HeapPtr];
			var payload = (RtInstance)vector2;


			//NaNBoxing x = payload.ReadSlot(0, vector2.Type._link_codescope, context.player);
			//NaNBoxing y = payload.ReadSlot(1, vector2.Type._link_codescope, context.player);

			float x;
			float y;

			var store = ((RtInstance)vector2).GetStoreData(context.player, (ASInstance)vector2.Type);
			unsafe
			{
				fixed (byte* p = store)
				{
					x = *(float*)p ;
					y = *((float*)p + 1) ;
				}
			}
			
			


			if (context.player.TryCreateStringValue($"({x.ToString("F2")},{y.ToString("F2")})", out NaNBoxing result, ref error))
			{
				context.StackSlots[returnSlotIndex] = result;
				//context.StackSlots[returnSlotIndex].SetHeapPtr(str, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
			}
		}

		[NativeFunction("$geom.Vector2$private::Vec2addVec2")]
		public static void Vector2_Vec2addVec2(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			var vec2 = (ASClass)((RtScriptClass)context.GC.Heap[thisPtr.HeapPtr]).Meta;

			NaNBoxing v1 = scope.ReadSlot(0, context.player);
			NaNBoxing v2 = scope.ReadSlot(1, context.player);

			if (v1.ValueType == NaNBoxing.BoxType.Null || v2.ValueType == NaNBoxing.BoxType.Null)
			{
				context.player.RaiseTypeError_AccessNull(ref  error);	
				return;
			}


			Debug.Assert(v1.ValueType == NaNBoxing.BoxType.HeapPtr);
			Debug.Assert(v2.ValueType == NaNBoxing.BoxType.HeapPtr);

			var vector2_a = context.GC.Heap[v1.HeapPtr];
			var payload_a = (RtInstance)vector2_a;

			//NaNBoxing x1 = payload_a.ReadSlot(0, vec2.Instance._link_codescope, context.player);
			//NaNBoxing y1 = payload_a.ReadSlot(1, vec2.Instance._link_codescope, context.player);

			float x1;
			float y1;

			var store1 = ((RtInstance)payload_a).GetStoreData(context.player, (ASInstance)payload_a.Type);
			unsafe
			{
				fixed (byte* p = store1)
				{
					x1 = *(float*)p;
					y1 = *((float*)p + 1);
				}
			}



			var vector2_b = context.GC.Heap[v2.HeapPtr];
			var payload_b = (RtInstance)vector2_b;

			//NaNBoxing x2 = payload_b.ReadSlot(0, vec2.Instance._link_codescope, context.player);
			//NaNBoxing y2 = payload_b.ReadSlot(1, vec2.Instance._link_codescope, context.player);

			float x2;
			float y2;

			var store2 = ((RtInstance)payload_b).GetStoreData(context.player, (ASInstance)payload_b.Type);
			unsafe
			{
				fixed (byte* p = store2)
				{
					x2 = *(float*)p;
					y2 = *((float*)p + 1);
				}
			}



			int resultptr = context.player.InitCacheInstance(vec2, returnSlotIndex, false, out RtInstance payload_result);
			//var vector2_result = context.GC.Heap[resultptr];
			//var payload_result = (RtInstance)vector2_result;

			//NaNBoxing x = default;x.SetFloat( x1.FloatValue + x2.FloatValue );
			//NaNBoxing y = default;y.SetFloat( y1.FloatValue + y2.FloatValue );

			//payload_result.SetSlot(x, 0, vec2.Instance._link_codescope, context.player);
			//payload_result.SetSlot(y, 1, vec2.Instance._link_codescope, context.player);

			var store = ((RtInstance)payload_result).GetStoreData(context.player, (ASInstance)payload_result.Type);
			unsafe
			{
				fixed (byte* p = store)
				{
					*(float*)p = x1 + x2;
					*((float*)p + 1) = y1 + y2;
				}
			}



		}

		[NativeFunction("$geom.Vector2$private::Vec2subVec2")]
		public static void Vector2_Vec2subVec2(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			var vec2 = (ASClass)((RtScriptClass)context.GC.Heap[thisPtr.HeapPtr]).Meta;

			NaNBoxing v1 = scope.ReadSlot(0, context.player);
			NaNBoxing v2 = scope.ReadSlot(1, context.player);

			if (v1.ValueType == NaNBoxing.BoxType.Null || v2.ValueType == NaNBoxing.BoxType.Null)
			{
				context.player.RaiseTypeError_AccessNull(ref error);
				return;
			}

			Debug.Assert(v1.ValueType == NaNBoxing.BoxType.HeapPtr);
			Debug.Assert(v2.ValueType == NaNBoxing.BoxType.HeapPtr);

			var vector2_a = context.GC.Heap[v1.HeapPtr];
			var payload_a = (RtInstance)vector2_a;

			float x1;
			float y1;

			var store1 = ((RtInstance)payload_a).GetStoreData(context.player, (ASInstance)payload_a.Type);
			unsafe
			{
				fixed (byte* p = store1)
				{
					x1 = *(float*)p;
					y1 = *((float*)p + 1);
				}
			}



			var vector2_b = context.GC.Heap[v2.HeapPtr];
			var payload_b = (RtInstance)vector2_b;

			float x2;
			float y2;

			var store2 = ((RtInstance)payload_b).GetStoreData(context.player, (ASInstance)payload_b.Type);
			unsafe
			{
				fixed (byte* p = store2)
				{
					x2 = *(float*)p;
					y2 = *((float*)p + 1);
				}
			}



			int resultptr = context.player.InitCacheInstance(vec2, returnSlotIndex, false, out RtInstance payload_result);
			//var vector2_result = context.GC.Heap[resultptr];
			//var payload_result = (RtInstance)vector2_result;

			var store = ((RtInstance)payload_result).GetStoreData(context.player, (ASInstance)payload_result.Type);
			unsafe
			{
				fixed (byte* p = store)
				{
					*(float*)p = x1 - x2;
					*((float*)p + 1) = y1 - y2;
				}
			}



		}

		[NativeFunction("geom.Vector2$public::dot")]
		public static void Vector2_Dot(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			var vector2_this = context.GC.Heap[thisPtr.HeapPtr];
			var payload_this = (RtInstance)vector2_this;

			//NaNBoxing x1 = payload_this.ReadSlot(0, vector2_this.Type._link_codescope, context.player);
			//NaNBoxing y1 = payload_this.ReadSlot(1, vector2_this.Type._link_codescope, context.player);

			float x1;
			float y1;

			var store1 = ((RtInstance)payload_this).GetStoreData(context.player, (ASInstance)payload_this.Type);
			unsafe
			{
				fixed (byte* p = store1)
				{
					x1 = *(float*)p;
					y1 = *((float*)p + 1);
				}
			}


			NaNBoxing v_arg = scope.ReadSlot(0, context.player);

			if (v_arg.ValueType == NaNBoxing.BoxType.Null)
			{
				context.player.RaiseTypeError_AccessNull(ref error);
				return;
			}

			Debug.Assert(v_arg.ValueType == NaNBoxing.BoxType.HeapPtr);

			var vector2_arg = context.GC.Heap[v_arg.HeapPtr];
			var payload_arg = (RtInstance)vector2_arg;

			//NaNBoxing x2 = payload_arg.ReadSlot(0, vector2_arg.Type._link_codescope, context.player);
			//NaNBoxing y2 = payload_arg.ReadSlot(1, vector2_arg.Type._link_codescope, context.player);

			float x2;
			float y2;

			var store2 = ((RtInstance)payload_arg).GetStoreData(context.player, (ASInstance)payload_arg.Type);
			unsafe
			{
				fixed (byte* p = store2)
				{
					x2 = *(float*)p;
					y2 = *((float*)p + 1);
				}
			}


			float result = x1 * x2 + y1 * y2;

			NaNBoxing nan_result = default;
			nan_result.SetFloat(result);
			context.StackSlots[returnSlotIndex] = nan_result;
		}

		[NativeFunction("geom.Vector2$public::cross")]
		public static void Vector2_Cross(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			var vector2_this = context.GC.Heap[thisPtr.HeapPtr];
			var payload_this = (RtInstance)vector2_this;

			//NaNBoxing x1 = payload_this.ReadSlot(0, vector2_this.Type._link_codescope, context.player);
			//NaNBoxing y1 = payload_this.ReadSlot(1, vector2_this.Type._link_codescope, context.player);


			float x1;
			float y1;

			var store1 = ((RtInstance)payload_this).GetStoreData(context.player, (ASInstance)payload_this.Type);
			unsafe
			{
				fixed (byte* p = store1)
				{
					x1 = *(float*)p;
					y1 = *((float*)p + 1);
				}
			}



			NaNBoxing v_arg = scope.ReadSlot(0, context.player);//.StackSlots[stackStPos];

			if (v_arg.ValueType == NaNBoxing.BoxType.Null)
			{
				context.player.RaiseTypeError_AccessNull(ref error);
				return;
			}

			Debug.Assert(v_arg.ValueType == NaNBoxing.BoxType.HeapPtr);

			var vector2_arg = context.GC.Heap[v_arg.HeapPtr];
			var payload_arg = (RtInstance)vector2_arg;

			//NaNBoxing x2 = payload_arg.ReadSlot(0, vector2_arg.Type._link_codescope, context.player);
			//NaNBoxing y2 = payload_arg.ReadSlot(1, vector2_arg.Type._link_codescope, context.player);
			float x2;
			float y2;

			var store2 = ((RtInstance)payload_arg).GetStoreData(context.player, (ASInstance)payload_arg.Type);
			unsafe
			{
				fixed (byte* p = store2)
				{
					x2 = *(float*)p;
					y2 = *((float*)p + 1);
				}
			}


			float result = x1 * y2 - y1 * x2;

			NaNBoxing nan_result = default;
			nan_result.SetFloat(result);
			context.StackSlots[returnSlotIndex] = nan_result;
		}

		[NativeFunction("$geom.Vector2$private::Vec2mulFloat")]
		public static void Vector2_Vec2mulFloat(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			var vec2 = (ASClass)((RtScriptClass)context.GC.Heap[thisPtr.HeapPtr]).Meta;

			NaNBoxing v1 = scope.ReadSlot(0, context.player);
			NaNBoxing s = scope.ReadSlot(1, context.player);

			if (v1.ValueType == NaNBoxing.BoxType.Null)
			{
				context.player.RaiseTypeError_AccessNull(ref error);
				return;
			}


			Debug.Assert(v1.ValueType == NaNBoxing.BoxType.HeapPtr);
			Debug.Assert(s.ValueType == NaNBoxing.BoxType.Float);

			var vector2_a = context.GC.Heap[v1.HeapPtr];
			var payload_a = (RtInstance)vector2_a;

			//NaNBoxing x1 = payload_a.ReadSlot(0, vec2.Instance._link_codescope, context.player);
			//NaNBoxing y1 = payload_a.ReadSlot(1, vec2.Instance._link_codescope, context.player);

			float x1;
			float y1;

			var store1 = ((RtInstance)payload_a).GetStoreData(context.player, (ASInstance)payload_a.Type);
			unsafe
			{
				fixed (byte* p = store1)
				{
					x1 = *(float*)p;
					y1 = *((float*)p + 1);
				}
			}


			float scalar = s.FloatValue;

			int resultptr = context.player.InitCacheInstance(vec2, returnSlotIndex, false,out RtInstance payload_result);
			//var vector2_result = context.GC.Heap[resultptr];
			//var payload_result = (RtInstance)vector2_result;

			//NaNBoxing x = default; x.SetFloat(x1.FloatValue * scalar);
			//NaNBoxing y = default; y.SetFloat(y1.FloatValue * scalar);

			//payload_result.SetSlot(x, 0, vec2.Instance._link_codescope, context.player);
			//payload_result.SetSlot(y, 1, vec2.Instance._link_codescope, context.player);

			var store = ((RtInstance)payload_result).GetStoreData(context.player, (ASInstance)payload_result.Type);
			unsafe
			{
				fixed (byte* p = store)
				{
					*(float*)p = x1 * scalar;
					*((float*)p + 1) = y1 * scalar;
				}
			}


		}

		[NativeFunction("$geom.Vector2$private::Vec2mulNumber")]
		public static void Vector2_Vec2mulNumber(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			var vec2 = (ASClass)((RtScriptClass)context.GC.Heap[thisPtr.HeapPtr]).Meta;

			NaNBoxing v1 = scope.ReadSlot(0, context.player);
			NaNBoxing s = scope.ReadSlot(1, context.player);

			if (v1.ValueType == NaNBoxing.BoxType.Null)
			{
				context.player.RaiseTypeError_AccessNull(ref error);
				return;
			}

			Debug.Assert(v1.ValueType == NaNBoxing.BoxType.HeapPtr);

			var vector2_a = context.GC.Heap[v1.HeapPtr];
			var payload_a = (RtInstance)vector2_a;

			//NaNBoxing x1 = payload_a.ReadSlot(0, vec2.Instance._link_codescope, context.player);
			//NaNBoxing y1 = payload_a.ReadSlot(1, vec2.Instance._link_codescope, context.player);

			float x1;
			float y1;

			var store1 = ((RtInstance)payload_a).GetStoreData(context.player, (ASInstance)payload_a.Type);
			unsafe
			{
				fixed (byte* p = store1)
				{
					x1 = *(float*)p;
					y1 = *((float*)p + 1);
				}
			}



			float scalar = (float)s.Number;

			int resultptr = context.player.InitCacheInstance(vec2, returnSlotIndex, false,out RtInstance payload_result);
			//var vector2_result = context.GC.Heap[resultptr];
			//var payload_result = (RtInstance)vector2_result;

			//NaNBoxing x = default; x.SetFloat(x1.FloatValue * scalar);
			//NaNBoxing y = default; y.SetFloat(y1.FloatValue * scalar);

			//payload_result.SetSlot(x, 0, vec2.Instance._link_codescope, context.player);
			//payload_result.SetSlot(y, 1, vec2.Instance._link_codescope, context.player);

			var store = ((RtInstance)payload_result).GetStoreData(context.player, (ASInstance)payload_result.Type);
			unsafe
			{
				fixed (byte* p = store)
				{
					*(float*)p = x1 * scalar;
					*((float*)p + 1) = y1 * scalar;
				}
			}

		}

		[NativeFunction("$geom.Vector2$private::Vec2divFloat")]
		public static void Vector2_Vec2divFloat(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			var vec2 = (ASClass)((RtScriptClass)context.GC.Heap[thisPtr.HeapPtr]).Meta;

			NaNBoxing v1 = scope.ReadSlot(0, context.player);
			NaNBoxing s = scope.ReadSlot(1, context.player);

			if (v1.ValueType == NaNBoxing.BoxType.Null)
			{
				context.player.RaiseTypeError_AccessNull(ref error);
				return;
			}


			Debug.Assert(v1.ValueType == NaNBoxing.BoxType.HeapPtr);
			Debug.Assert(s.ValueType == NaNBoxing.BoxType.Float);

			var vector2_a = context.GC.Heap[v1.HeapPtr];
			var payload_a = (RtInstance)vector2_a;

			//NaNBoxing x1 = payload_a.ReadSlot(0, vec2.Instance._link_codescope, context.player);
			//NaNBoxing y1 = payload_a.ReadSlot(1, vec2.Instance._link_codescope, context.player);

			float x1;
			float y1;

			var store1 = ((RtInstance)payload_a).GetStoreData(context.player, (ASInstance)payload_a.Type);
			unsafe
			{
				fixed (byte* p = store1)
				{
					x1 = *(float*)p;
					y1 = *((float*)p + 1);
				}
			}

			float scalar = s.FloatValue;

			int resultptr = context.player.InitCacheInstance(vec2, returnSlotIndex, false,out RtInstance payload_result);
			//var vector2_result = context.GC.Heap[resultptr];
			//var payload_result = (RtInstance)vector2_result;

			//NaNBoxing x = default; x.SetFloat(x1.FloatValue / scalar);
			//NaNBoxing y = default; y.SetFloat(y1.FloatValue / scalar);

			//payload_result.SetSlot(x, 0, vec2.Instance._link_codescope, context.player);
			//payload_result.SetSlot(y, 1, vec2.Instance._link_codescope, context.player);

			var store = ((RtInstance)payload_result).GetStoreData(context.player, (ASInstance)payload_result.Type);
			unsafe
			{
				fixed (byte* p = store)
				{
					*(float*)p = x1 / scalar;
					*((float*)p + 1) = y1 / scalar;
				}
			}

		}

		[NativeFunction("$geom.Vector2$private::Vec2divNumber")]
		public static void Vector2_Vec2divNumber(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			var vec2 = (ASClass)((RtScriptClass)context.GC.Heap[thisPtr.HeapPtr]).Meta;

			NaNBoxing v1 = scope.ReadSlot(0, context.player);
			NaNBoxing s = scope.ReadSlot(1, context.player);

			if (v1.ValueType == NaNBoxing.BoxType.Null)
			{
				context.player.RaiseTypeError_AccessNull(ref error);
				return;
			}

			Debug.Assert(v1.ValueType == NaNBoxing.BoxType.HeapPtr);

			var vector2_a = context.GC.Heap[v1.HeapPtr];
			var payload_a = (RtInstance)vector2_a;

			//NaNBoxing x1 = payload_a.ReadSlot(0, vec2.Instance._link_codescope, context.player);
			//NaNBoxing y1 = payload_a.ReadSlot(1, vec2.Instance._link_codescope, context.player);

			float x1;
			float y1;

			var store1 = ((RtInstance)payload_a).GetStoreData(context.player, (ASInstance)payload_a.Type);
			unsafe
			{
				fixed (byte* p = store1)
				{
					x1 = *(float*)p;
					y1 = *((float*)p + 1);
				}
			}

			float scalar = (float)s.Number;

			int resultptr = context.player.InitCacheInstance(vec2, returnSlotIndex, false,out RtInstance payload_result);
			//var vector2_result = context.GC.Heap[resultptr];
			//var payload_result = (RtInstance)vector2_result;

			//NaNBoxing x = default; x.SetFloat(x1.FloatValue / scalar);
			//NaNBoxing y = default; y.SetFloat(y1.FloatValue / scalar);

			//payload_result.SetSlot(x, 0, vec2.Instance._link_codescope, context.player);
			//payload_result.SetSlot(y, 1, vec2.Instance._link_codescope, context.player);

			var store = ((RtInstance)payload_result).GetStoreData(context.player, (ASInstance)payload_result.Type);
			unsafe
			{
				fixed (byte* p = store)
				{
					*(float*)p = x1 / scalar;
					*((float*)p + 1) = y1 / scalar;
				}
			}

		}

		[NativeFunction("$geom.Vector2$private::FloatmulVec2")]
		public static void Vector2_FloatmulVec2(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			var vec2 = (ASClass)((RtScriptClass)context.GC.Heap[thisPtr.HeapPtr]).Meta;

			NaNBoxing s = scope.ReadSlot(0, context.player);
			NaNBoxing v1 = scope.ReadSlot(1, context.player);

			if (v1.ValueType == NaNBoxing.BoxType.Null)
			{
				context.player.RaiseTypeError_AccessNull(ref error);
				return;
			}

			Debug.Assert(s.ValueType == NaNBoxing.BoxType.Float);
			Debug.Assert(v1.ValueType == NaNBoxing.BoxType.HeapPtr);

			float scalar = s.FloatValue;

			var vector2_a = context.GC.Heap[v1.HeapPtr];
			var payload_a = (RtInstance)vector2_a;

			//NaNBoxing x1 = payload_a.ReadSlot(0, vec2.Instance._link_codescope, context.player);
			//NaNBoxing y1 = payload_a.ReadSlot(1, vec2.Instance._link_codescope, context.player);

			float x1;
			float y1;

			var store1 = ((RtInstance)payload_a).GetStoreData(context.player, (ASInstance)payload_a.Type);
			unsafe
			{
				fixed (byte* p = store1)
				{
					x1 = *(float*)p;
					y1 = *((float*)p + 1);
				}
			}

			int resultptr = context.player.InitCacheInstance(vec2, returnSlotIndex, false,out RtInstance payload_result);
			//var vector2_result = context.GC.Heap[resultptr];
			//var payload_result = (RtInstance)vector2_result;

			//NaNBoxing x = default; x.SetFloat(scalar * x1.FloatValue);
			//NaNBoxing y = default; y.SetFloat(scalar * y1.FloatValue);

			//payload_result.SetSlot(x, 0, vec2.Instance._link_codescope, context.player);
			//payload_result.SetSlot(y, 1, vec2.Instance._link_codescope, context.player);

			var store = ((RtInstance)payload_result).GetStoreData(context.player, (ASInstance)payload_result.Type);
			unsafe
			{
				fixed (byte* p = store)
				{
					*(float*)p = scalar * x1;
					*((float*)p + 1) = scalar * y1 ;
				}
			}

		}

		[NativeFunction("$geom.Vector2$private::NumbermulVec2")]
		public static void Vector2_NumbermulVec2(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			var vec2 = (ASClass)((RtScriptClass)context.GC.Heap[thisPtr.HeapPtr]).Meta;

			NaNBoxing s = scope.ReadSlot(0, context.player);
			NaNBoxing v1 = scope.ReadSlot(1, context.player);

			if (v1.ValueType == NaNBoxing.BoxType.Null)
			{
				context.player.RaiseTypeError_AccessNull(ref error);
				return;
			}

			Debug.Assert(v1.ValueType == NaNBoxing.BoxType.HeapPtr);

			float scalar = (float)s.Number;

			var vector2_a = context.GC.Heap[v1.HeapPtr];
			var payload_a = (RtInstance)vector2_a;

			//NaNBoxing x1 = payload_a.ReadSlot(0, vec2.Instance._link_codescope, context.player);
			//NaNBoxing y1 = payload_a.ReadSlot(1, vec2.Instance._link_codescope, context.player);

			float x1;
			float y1;

			var store1 = ((RtInstance)payload_a).GetStoreData(context.player, (ASInstance)payload_a.Type);
			unsafe
			{
				fixed (byte* p = store1)
				{
					x1 = *(float*)p;
					y1 = *((float*)p + 1);
				}
			}

			int resultptr = context.player.InitCacheInstance(vec2, returnSlotIndex, false,out RtInstance payload_result);
			//var vector2_result = context.GC.Heap[resultptr];
			//var payload_result = (RtInstance)vector2_result;

			//NaNBoxing x = default; x.SetFloat(scalar * x1.FloatValue);
			//NaNBoxing y = default; y.SetFloat(scalar * y1.FloatValue);

			//payload_result.SetSlot(x, 0, vec2.Instance._link_codescope, context.player);
			//payload_result.SetSlot(y, 1, vec2.Instance._link_codescope, context.player);

			var store = ((RtInstance)payload_result).GetStoreData(context.player, (ASInstance)payload_result.Type);
			unsafe
			{
				fixed (byte* p = store)
				{
					*(float*)p = scalar * x1;
					*((float*)p + 1) = scalar * y1;
				}
			}

		}


		[NativeFunction("$geom.Vector2$private::Vec2Neg")]
		public static void Vector2_Vec2Neg(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			var vec2 = (ASClass)((RtScriptClass)context.GC.Heap[thisPtr.HeapPtr]).Meta;

			NaNBoxing v = scope.ReadSlot(0, context.player);

			if (v.ValueType == NaNBoxing.BoxType.Null)
			{
				context.player.RaiseTypeError_AccessNull(ref error);
				return;
			}

			Debug.Assert(v.ValueType == NaNBoxing.BoxType.HeapPtr);

			
			var vector2_a = context.GC.Heap[v.HeapPtr];
			var payload_a = (RtInstance)vector2_a;

			//NaNBoxing x1 = payload_a.ReadSlot(0, vec2.Instance._link_codescope, context.player);
			//NaNBoxing y1 = payload_a.ReadSlot(1, vec2.Instance._link_codescope, context.player);

			float x1;
			float y1;

			var store1 = ((RtInstance)payload_a).GetStoreData(context.player, (ASInstance)payload_a.Type);
			unsafe
			{
				fixed (byte* p = store1)
				{
					x1 = *(float*)p;
					y1 = *((float*)p + 1);
				}
			}

			int resultptr = context.player.InitCacheInstance(vec2, returnSlotIndex, false,out RtInstance payload_result);
			//var vector2_result = context.GC.Heap[resultptr];
			//var payload_result = (RtInstance)vector2_result;

			//x1.SetFloat(-x1.FloatValue);
			//y1.SetFloat(-y1.FloatValue);

			//payload_result.SetSlot(x1, 0, vec2.Instance._link_codescope, context.player);
			//payload_result.SetSlot(y1, 1, vec2.Instance._link_codescope, context.player);

			var store = ((RtInstance)payload_result).GetStoreData(context.player, (ASInstance)payload_result.Type);
			unsafe
			{
				fixed (byte* p = store)
				{
					*(float*)p = -x1;
					*((float*)p + 1) = -y1;
				}
			}

		}


		[NativeFunction("$geom.Vector2$private::Vec2Positive")]
		public static void Vector2_Vec2Positive(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			var vec2 = (ASClass)((RtScriptClass)context.GC.Heap[thisPtr.HeapPtr]).Meta;

			NaNBoxing v = scope.ReadSlot(0, context.player);

			if (v.ValueType == NaNBoxing.BoxType.Null)
			{
				context.player.RaiseTypeError_AccessNull(ref error);
				return;
			}

			Debug.Assert(v.ValueType == NaNBoxing.BoxType.HeapPtr);

			
			var vector2_a = context.GC.Heap[v.HeapPtr];
			var payload_a = (RtInstance)vector2_a;

			//NaNBoxing x1 = payload_a.ReadSlot(0, vec2.Instance._link_codescope, context.player);
			//NaNBoxing y1 = payload_a.ReadSlot(1, vec2.Instance._link_codescope, context.player);

			float x1;
			float y1;

			var store1 = ((RtInstance)payload_a).GetStoreData(context.player, (ASInstance)payload_a.Type);
			unsafe
			{
				fixed (byte* p = store1)
				{
					x1 = *(float*)p;
					y1 = *((float*)p + 1);
				}
			}


			int resultptr = context.player.InitCacheInstance(vec2, returnSlotIndex, false,out RtInstance payload_result);
			//var vector2_result = context.GC.Heap[resultptr];
			//var payload_result = (RtInstance)vector2_result;

			//x1.SetFloat(+x1.FloatValue);
			//y1.SetFloat(+y1.FloatValue);

			//payload_result.SetSlot(x1, 0, vec2.Instance._link_codescope, context.player);
			//payload_result.SetSlot(y1, 1, vec2.Instance._link_codescope, context.player);

			var store = ((RtInstance)payload_result).GetStoreData(context.player, (ASInstance)payload_result.Type);
			unsafe
			{
				fixed (byte* p = store)
				{
					*(float*)p = +x1;
					*((float*)p + 1) = +y1;
				}
			}

		}




		//$.Mat22$public::FromAngle
		[NativeFunction("$geom.Matrix2x2$public::FromAngle")]
		public static void Mat22_FromAngle(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
			var mat2 = method.__return_type_class__;

			float angle = scope.ReadSlot(0, context.player).FloatValue;
			float c = MathF.Cos(angle);
			float s = MathF.Sin(angle);

			int resultptr = context.player.InitCacheInstance(mat2, returnSlotIndex, false, out RtInstance payload_result);
			var store = ((RtInstance)payload_result).GetStoreData(context.player, (ASInstance)payload_result.Type);
			unsafe
			{
				fixed (byte* p = store)
				{
					*(float*)p = c;
					*((float*)p + 1) = s;
					*((float*)p + 2) = -s;
					*((float*)p + 3) = c;
				}
			}


		}





		[NativeFunction("geom.Matrix2x2$public::Transpose")]
		public static void Mat22_Transpose(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
			var mat2 = method.__return_type_class__;


			var m2_a = context.GC.Heap[thisPtr.HeapPtr];
			var payload_a = (RtInstance)m2_a;

			//NaNBoxing x1 = payload_a.ReadSlot(0, vec2.Instance._link_codescope, context.player);
			//NaNBoxing y1 = payload_a.ReadSlot(1, vec2.Instance._link_codescope, context.player);

			float a;
			float b;

			float c;
			float d;

			var store1 = ((RtInstance)payload_a).GetStoreData(context.player, (ASInstance)payload_a.Type);
			unsafe
			{
				fixed (byte* p = store1)
				{
					a = *(float*)p;
					c = *((float*)p + 1);

					b = *((float*)p + 2);
					d = *((float*)p + 3);
				}
			}



			int resultptr = context.player.InitCacheInstance(mat2, returnSlotIndex, false, out RtInstance payload_result);
			var store = ((RtInstance)payload_result).GetStoreData(context.player, (ASInstance)payload_result.Type);
			unsafe
			{
				fixed (byte* p = store)
				{

					*(float*)p = a;
					*((float*)p + 1) = b;
					*((float*)p + 2) = c;
					*((float*)p + 3) = d;

				}
			}


		}




		//.Mat22$public::Invert
		[NativeFunction("geom.Matrix2x2$public::Invert")]
		public static void Mat22_Invert(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
			var mat2 = method.__return_type_class__;


			var m2_a = context.GC.Heap[thisPtr.HeapPtr];
			var payload_a = (RtInstance)m2_a;

			//NaNBoxing x1 = payload_a.ReadSlot(0, vec2.Instance._link_codescope, context.player);
			//NaNBoxing y1 = payload_a.ReadSlot(1, vec2.Instance._link_codescope, context.player);

			float a;
			float b;

			float c;
			float d;

			var store1 = ((RtInstance)payload_a).GetStoreData(context.player, (ASInstance)payload_a.Type);
			unsafe
			{
				fixed (byte* p = store1)
				{
					a = *(float*)p;
					c = *((float*)p + 1);

					b = *((float*)p + 2);
					d = *((float*)p + 3);
				}
			}

			//var a:float = col1.x;
			//var b:float = col2.x;
			//var c:float = col1.y;
			//var d:float = col2.y;
			//
			//var B:Mat22 = new Mat22();
			//var det:float = a * d - b * c;
			//
			//if (det == 0.0f)
			//throw new Error("det != 0.0f");
			//
			//det = 1.0f / det;
			//
			//B.col1.x =  det * d;	B.col2.x = -det * b;
			//B.col1.y = -det * c;	B.col2.y =  det * a;
			//return B;
			//
			//

			float det = a * d - b * c;
			if (det == 0.0f)
			{
				context.player.RaiseError(ref error, "det != 0.0f");
				return;
			}
			det = 1.0f / det;


			int resultptr = context.player.InitCacheInstance(mat2, returnSlotIndex, false, out RtInstance payload_result);
			var store = ((RtInstance)payload_result).GetStoreData(context.player, (ASInstance)payload_result.Type);
			unsafe
			{
				fixed (byte* p = store)
				{

					*(float*)p = det * d;
					*((float*)p + 1) = -det * c;
					*((float*)p + 2) = -det * b;
					*((float*)p + 3) = det * a;

				}
			}


		}


		//$.Mat22$private::Mat22mulMat22
		[NativeFunction("$geom.Matrix2x2$private::Mat22mulMat22")]
		public static void Mat22mulMat22(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
			var mat2 = (ASClass)((RtScriptClass)context.GC.Heap[thisPtr.HeapPtr]).Meta;

			NaNBoxing m1 = scope.ReadSlot(0, context.player);
			NaNBoxing m2 = scope.ReadSlot(1, context.player);

			if (m1.ValueType == NaNBoxing.BoxType.Null || m2.ValueType == NaNBoxing.BoxType.Null)
			{
				context.player.RaiseTypeError_AccessNull(ref error);
				return;
			}

			var m2_a = context.GC.Heap[m1.HeapPtr];
			var payload_a = (RtInstance)m2_a;

			//NaNBoxing x1 = payload_a.ReadSlot(0, vec2.Instance._link_codescope, context.player);
			//NaNBoxing y1 = payload_a.ReadSlot(1, vec2.Instance._link_codescope, context.player);

			float Acol1_x;
			float Acol1_y;

			float Acol2_x;
			float Acol2_y;

			var store1 = ((RtInstance)payload_a).GetStoreData(context.player, (ASInstance)payload_a.Type);
			unsafe
			{
				fixed (byte* p = store1)
				{
					Acol1_x = *(float*)p;
					Acol1_y = *((float*)p + 1);

					Acol2_x = *((float*)p + 2);
					Acol2_y = *((float*)p + 3);
				}
			}

			var m2_b = context.GC.Heap[m2.HeapPtr];
			var payload_b = (RtInstance)m2_b;
			float Bcol1_x;
			float Bcol1_y;

			float Bcol2_x;
			float Bcol2_y;
			var store2 = ((RtInstance)payload_b).GetStoreData(context.player, (ASInstance)payload_b.Type);
			unsafe
			{
				fixed (byte* p = store2)
				{
					Bcol1_x = *(float*)p;
					Bcol1_y = *((float*)p + 1);

					Bcol2_x = *((float*)p + 2);
					Bcol2_y = *((float*)p + 3);
				}
			}

			int resultptr = context.player.InitCacheInstance(mat2, returnSlotIndex, false, out RtInstance payload_result);
			var store = ((RtInstance)payload_result).GetStoreData(context.player, (ASInstance)payload_result.Type);
			unsafe
			{
				fixed (byte* p = store)
				{
					//return new Mat22( new Vector2( A.col1.x * B.col1.x + A.col2.x * B.col1.y,A.col1.y * B.col1.x + A.col2.y * B.col1.y ),
					//new Vector2(A.col1.x * B.col2.x + A.col2.x * B.col2.y ,A.col1.y * B.col2.x + A.col2.y * B.col2.y  ) );

					*(float*)p = Acol1_x * Bcol1_x + Acol2_x * Bcol1_y;
					*((float*)p + 1) = Acol1_y * Bcol1_x + Acol2_y * Bcol1_y;
					*((float*)p + 2) = Acol1_x * Bcol2_x + Acol2_x * Bcol2_y;
					*((float*)p + 3) = Acol1_y * Bcol2_x + Acol2_y * Bcol2_y;

				}
			}


		}

		//$.Mat22$private::Mat22addMat22
		[NativeFunction("$geom.Matrix2x2$private::Mat22addMat22")]
		public static void Mat22addMat22(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
			var mat2 = (ASClass)((RtScriptClass)context.GC.Heap[thisPtr.HeapPtr]).Meta;

			NaNBoxing m1 = scope.ReadSlot(0, context.player);
			NaNBoxing m2 = scope.ReadSlot(1, context.player);

			if (m1.ValueType == NaNBoxing.BoxType.Null || m2.ValueType == NaNBoxing.BoxType.Null)
			{
				context.player.RaiseTypeError_AccessNull(ref error);
				return;
			}

			var m2_a = context.GC.Heap[m1.HeapPtr];
			var payload_a = (RtInstance)m2_a;

			//NaNBoxing x1 = payload_a.ReadSlot(0, vec2.Instance._link_codescope, context.player);
			//NaNBoxing y1 = payload_a.ReadSlot(1, vec2.Instance._link_codescope, context.player);

			float Acol1_x;
			float Acol1_y;

			float Acol2_x;
			float Acol2_y;

			var store1 = ((RtInstance)payload_a).GetStoreData(context.player, (ASInstance)payload_a.Type);
			unsafe
			{
				fixed (byte* p = store1)
				{
					Acol1_x = *(float*)p;
					Acol1_y = *((float*)p + 1);

					Acol2_x = *((float*)p + 2);
					Acol2_y = *((float*)p + 3);
				}
			}

			var m2_b = context.GC.Heap[m2.HeapPtr];
			var payload_b = (RtInstance)m2_b;
			float Bcol1_x;
			float Bcol1_y;

			float Bcol2_x;
			float Bcol2_y;
			var store2 = ((RtInstance)payload_b).GetStoreData(context.player, (ASInstance)payload_b.Type);
			unsafe
			{
				fixed (byte* p = store2)
				{
					Bcol1_x = *(float*)p;
					Bcol1_y = *((float*)p + 1);

					Bcol2_x = *((float*)p + 2);
					Bcol2_y = *((float*)p + 3);
				}
			}

			int resultptr = context.player.InitCacheInstance(mat2, returnSlotIndex, false, out RtInstance payload_result);
			var store = ((RtInstance)payload_result).GetStoreData(context.player, (ASInstance)payload_result.Type);
			unsafe
			{
				fixed (byte* p = store)
				{

					*(float*)p = Acol1_x + Bcol1_x;
					*((float*)p + 1) = Acol1_y + Bcol1_y;
					*((float*)p + 2) = Acol2_x + Bcol2_x;
					*((float*)p + 3) = Acol2_y + Bcol2_y;

				}
			}


		}

		//
		[NativeFunction("geom.Matrix2x2$public::toString")]
		public static void Mat22_toString(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			var mat22 = context.GC.Heap[thisPtr.HeapPtr];
			var payload = (RtInstance)mat22;


			//NaNBoxing x = payload.ReadSlot(0, vector2.Type._link_codescope, context.player);
			//NaNBoxing y = payload.ReadSlot(1, vector2.Type._link_codescope, context.player);

			float col1_x;
			float col1_y;

			float col2_x;
			float col2_y;

			var store1 = ((RtInstance)payload).GetStoreData(context.player, (ASInstance)payload.Type);
			unsafe
			{
				fixed (byte* p = store1)
				{
					col1_x = *(float*)p;
					col1_y = *((float*)p + 1);

					col2_x = *((float*)p + 2);
					col2_y = *((float*)p + 3);
				}
			}




			if (context.player.TryCreateStringValue($"({col1_x.ToString("F2")},{col1_y.ToString("F2")}),({col2_x.ToString("F2")},{col2_y.ToString("F2")})", out NaNBoxing result, ref error))
			{
				context.StackSlots[returnSlotIndex] = result;
				//context.StackSlots[returnSlotIndex].SetHeapPtr(str, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
			}
		}

		//$.Mat22$private::Mat22mulVec2
		[NativeFunction("$geom.Matrix2x2$private::Mat22mulVec2")]
		public static void Mat22mulVec2(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
			var vec2 = method.__return_type_class__;

			NaNBoxing mat = scope.ReadSlot(0, context.player);
			NaNBoxing vec = scope.ReadSlot(1, context.player);

			if (mat.ValueType == NaNBoxing.BoxType.Null || vec.ValueType == NaNBoxing.BoxType.Null)
			{
				context.player.RaiseTypeError_AccessNull(ref error);
				return;
			}

			var m2_a = context.GC.Heap[mat.HeapPtr];
			var payload_a = (RtInstance)m2_a;

			float col1_x;
			float col1_y;

			float col2_x;
			float col2_y;

			var store1 = ((RtInstance)payload_a).GetStoreData(context.player, (ASInstance)payload_a.Type);
			unsafe
			{
				fixed (byte* p = store1)
				{
					col1_x = *(float*)p;
					col1_y = *((float*)p + 1);

					col2_x = *((float*)p + 2);
					col2_y = *((float*)p + 3);
				}
			}

			var vec_b = context.GC.Heap[vec.HeapPtr];
			var payload_b = (RtInstance)vec_b;

			float x;
			float y;
			var store2 = ((RtInstance)payload_b).GetStoreData(context.player, (ASInstance)payload_b.Type);
			unsafe
			{
				fixed (byte* p = store2)
				{
					x = *(float*)p;
					y = *((float*)p + 1);
				}
			}

			int resultptr = context.player.InitCacheInstance(vec2, returnSlotIndex, false, out RtInstance payload_result);
			var store = ((RtInstance)payload_result).GetStoreData(context.player, (ASInstance)payload_result.Type);
			unsafe
			{
				fixed (byte* p = store)
				{
					//A.col1.x * v.x + A.col2.x * v.y, A.col1.y * v.x + A.col2.y * v.y
					*(float*)p = col1_x * x + col2_x * y;
					*((float*)p + 1) = col1_y * x + col2_y * y;
				}
			}


		}





	}
}
