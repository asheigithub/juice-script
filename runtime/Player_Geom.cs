using juicescript.ABC;
using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

namespace juicescript.runtime
{
	public partial class Player
	{

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private unsafe void Exec_Add_Vec2_Vec2(int dst_index, byte** PC, ref ReceiveError error, Span<NaNBoxing> stackslots,int stackStPos
			)
		{
			StackLocater dst;
			StackLocater v1;
			StackLocater v2;


			dst.index = dst_index;
			LoadStackLocater(&v1, PC);
			LoadStackLocater(&v2, PC);


			NaNBoxing n1 = stackslots[v1.index];
			NaNBoxing n2 = stackslots[v2.index];

			if (n1.ValueType == NaNBoxing.BoxType.Null || n2.ValueType == NaNBoxing.BoxType.Null)
			{ 
				RaiseTypeError_AccessNull(ref error);
				return;
			}

			Debug.Assert(n1.ValueType == NaNBoxing.BoxType.HeapPtr);
			Debug.Assert(n2.ValueType == NaNBoxing.BoxType.HeapPtr);

			var vector2_a = Context.GC.Heap[n1.HeapPtr];
			var payload_a = (RtInstance)vector2_a;

			//NaNBoxing x1 = payload_a.ReadSlot(0, vec2.Instance._link_codescope, context.player);
			//NaNBoxing y1 = payload_a.ReadSlot(1, vec2.Instance._link_codescope, context.player);

			float x1;
			float y1;

			var store1 = ((RtInstance)payload_a).GetStoreData(this, (ASInstance)payload_a.Type);
			unsafe
			{
				fixed (byte* p = store1)
				{
					x1 = *(float*)p;
					y1 = *((float*)p + 1);
				}
			}



			var vector2_b = Context.GC.Heap[n2.HeapPtr];
			var payload_b = (RtInstance)vector2_b;

			//NaNBoxing x2 = payload_b.ReadSlot(0, vec2.Instance._link_codescope, context.player);
			//NaNBoxing y2 = payload_b.ReadSlot(1, vec2.Instance._link_codescope, context.player);

			float x2;
			float y2;

			var store2 = ((RtInstance)payload_b).GetStoreData(this, (ASInstance)payload_b.Type);
			unsafe
			{
				fixed (byte* p = store2)
				{
					x2 = *(float*)p;
					y2 = *((float*)p + 1);
				}
			}

			int resultptr = InitCacheInstance(Context.VEC2 , stackStPos + dst_index , false, out RtInstance payload_result);
			
			var store = ((RtInstance)payload_result).GetStoreData(this, (ASInstance)payload_result.Type);
			unsafe
			{
				fixed (byte* p = store)
				{
					*(float*)p = x1 + x2;
					*((float*)p + 1) = y1 + y2;
				}
			}


		}



		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private unsafe void Exec_Sub_Vec2_Vec2(int dst_index, byte** PC, ref ReceiveError error, Span<NaNBoxing> stackslots, int stackStPos
			)
		{
			StackLocater dst;
			StackLocater v1;
			StackLocater v2;


			dst.index = dst_index;
			LoadStackLocater(&v1, PC);
			LoadStackLocater(&v2, PC);


			NaNBoxing n1 = stackslots[v1.index];
			NaNBoxing n2 = stackslots[v2.index];

			if (n1.ValueType == NaNBoxing.BoxType.Null || n2.ValueType == NaNBoxing.BoxType.Null)
			{
				RaiseTypeError_AccessNull(ref error);
				return;
			}

			Debug.Assert(n1.ValueType == NaNBoxing.BoxType.HeapPtr);
			Debug.Assert(n2.ValueType == NaNBoxing.BoxType.HeapPtr);

			var vector2_a = Context.GC.Heap[n1.HeapPtr];
			var payload_a = (RtInstance)vector2_a;

			//NaNBoxing x1 = payload_a.ReadSlot(0, vec2.Instance._link_codescope, context.player);
			//NaNBoxing y1 = payload_a.ReadSlot(1, vec2.Instance._link_codescope, context.player);

			float x1;
			float y1;

			var store1 = ((RtInstance)payload_a).GetStoreData(this, (ASInstance)payload_a.Type);
			unsafe
			{
				fixed (byte* p = store1)
				{
					x1 = *(float*)p;
					y1 = *((float*)p + 1);
				}
			}



			var vector2_b = Context.GC.Heap[n2.HeapPtr];
			var payload_b = (RtInstance)vector2_b;

			//NaNBoxing x2 = payload_b.ReadSlot(0, vec2.Instance._link_codescope, context.player);
			//NaNBoxing y2 = payload_b.ReadSlot(1, vec2.Instance._link_codescope, context.player);

			float x2;
			float y2;

			var store2 = ((RtInstance)payload_b).GetStoreData(this, (ASInstance)payload_b.Type);
			unsafe
			{
				fixed (byte* p = store2)
				{
					x2 = *(float*)p;
					y2 = *((float*)p + 1);
				}
			}

			int resultptr = InitCacheInstance(Context.VEC2, stackStPos + dst_index, false, out RtInstance payload_result);

			var store = ((RtInstance)payload_result).GetStoreData(this, (ASInstance)payload_result.Type);
			unsafe
			{
				fixed (byte* p = store)
				{
					*(float*)p = x1 - x2;
					*((float*)p + 1) = y1 - y2;
				}
			}


		}



		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private unsafe void Exec_Scale_Vec2(int dst_index, byte** PC, ref ReceiveError error, Span<NaNBoxing> stackslots, int stackStPos
			)
		{
			StackLocater dst;
			StackLocater v1;
			StackLocater v2;


			dst.index = dst_index;
			LoadStackLocater(&v1, PC);
			LoadStackLocater(&v2, PC);


			NaNBoxing n1 = stackslots[v1.index];
			NaNBoxing n2 = stackslots[v2.index];

			if (n1.ValueType == NaNBoxing.BoxType.Null)
			{
				RaiseTypeError_AccessNull(ref error);
				return;
			}

			Debug.Assert(n1.ValueType == NaNBoxing.BoxType.HeapPtr);
			Debug.Assert(n2.ValueType >= NaNBoxing.BoxType.Int && n2.ValueType <= NaNBoxing.BoxType.Float || n2.ValueType == NaNBoxing.BoxType.Number);
			
			var vector2_a = Context.GC.Heap[n1.HeapPtr];
			var payload_a = (RtInstance)vector2_a;

			//NaNBoxing x1 = payload_a.ReadSlot(0, vec2.Instance._link_codescope, context.player);
			//NaNBoxing y1 = payload_a.ReadSlot(1, vec2.Instance._link_codescope, context.player);

			float x1;
			float y1;

			var store1 = ((RtInstance)payload_a).GetStoreData(this, (ASInstance)payload_a.Type);
			unsafe
			{
				fixed (byte* p = store1)
				{
					x1 = *(float*)p;
					y1 = *((float*)p + 1);
				}
			}

			if (n2.ValueType == NaNBoxing.BoxType.Number)
			{
				double factor = Extensions.GetDoubleValue(n2);

				int resultptr = InitCacheInstance(Context.VEC2, stackStPos + dst_index, false, out RtInstance payload_result);

				var store = ((RtInstance)payload_result).GetStoreData(this, (ASInstance)payload_result.Type);
				unsafe
				{
					fixed (byte* p = store)
					{
						*(float*)p = (float)(x1 * factor);
						*((float*)p + 1) = (float)(y1 * factor);
					}
				}
			}
			else
			{
				float factor = Extensions.GetFloatValue(n2);

				int resultptr = InitCacheInstance(Context.VEC2, stackStPos + dst_index, false, out RtInstance payload_result);

				var store = ((RtInstance)payload_result).GetStoreData(this, (ASInstance)payload_result.Type);
				unsafe
				{
					fixed (byte* p = store)
					{
						*(float*)p = x1 * factor;
						*((float*)p + 1) = y1 * factor;
					}
				}
			}

		}


		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private unsafe void Exec_Scale_Vec2_Reciprocal(int dst_index, byte** PC, ref ReceiveError error, Span<NaNBoxing> stackslots, int stackStPos
			)
		{
			StackLocater dst;
			StackLocater v1;
			StackLocater v2;


			dst.index = dst_index;
			LoadStackLocater(&v1, PC);
			LoadStackLocater(&v2, PC);


			NaNBoxing n1 = stackslots[v1.index];
			NaNBoxing n2 = stackslots[v2.index];

			if (n1.ValueType == NaNBoxing.BoxType.Null)
			{
				RaiseTypeError_AccessNull(ref error);
				return;
			}

			Debug.Assert(n1.ValueType == NaNBoxing.BoxType.HeapPtr);
			Debug.Assert(n2.ValueType >= NaNBoxing.BoxType.Int && n2.ValueType <= NaNBoxing.BoxType.Float || n2.ValueType == NaNBoxing.BoxType.Number);

			var vector2_a = Context.GC.Heap[n1.HeapPtr];
			var payload_a = (RtInstance)vector2_a;

			//NaNBoxing x1 = payload_a.ReadSlot(0, vec2.Instance._link_codescope, context.player);
			//NaNBoxing y1 = payload_a.ReadSlot(1, vec2.Instance._link_codescope, context.player);

			float x1;
			float y1;

			var store1 = ((RtInstance)payload_a).GetStoreData(this, (ASInstance)payload_a.Type);
			unsafe
			{
				fixed (byte* p = store1)
				{
					x1 = *(float*)p;
					y1 = *((float*)p + 1);
				}
			}

			if (n2.ValueType == NaNBoxing.BoxType.Number)
			{
				double factor = 1.0/Extensions.GetDoubleValue(n2);

				int resultptr = InitCacheInstance(Context.VEC2, stackStPos + dst_index, false, out RtInstance payload_result);

				var store = ((RtInstance)payload_result).GetStoreData(this, (ASInstance)payload_result.Type);
				unsafe
				{
					fixed (byte* p = store)
					{
						*(float*)p = (float)(x1 * factor);
						*((float*)p + 1) = (float)(y1 * factor);
					}
				}
			}
			else
			{
				float factor = 1.0f/Extensions.GetFloatValue(n2);

				int resultptr = InitCacheInstance(Context.VEC2, stackStPos + dst_index, false, out RtInstance payload_result);

				var store = ((RtInstance)payload_result).GetStoreData(this, (ASInstance)payload_result.Type);
				unsafe
				{
					fixed (byte* p = store)
					{
						*(float*)p = x1 * factor;
						*((float*)p + 1) = y1 * factor;
					}
				}
			}

		}



		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private unsafe void Exec_Neg_Pos_Vec2(int dst_index, byte** PC, ref ReceiveError error, Span<NaNBoxing> stackslots, int stackStPos
			)
		{
			StackLocater dst;
			StackLocater v1;
			

			dst.index = dst_index;

			int _store;
			LoadInt32(&_store, PC);

			bool is_positive = (_store & 1) == 0;

			v1.index = _store >> 1;

			NaNBoxing n1 = stackslots[v1.index];
			
			if (n1.ValueType == NaNBoxing.BoxType.Null)
			{
				RaiseTypeError_AccessNull(ref error);
				return;
			}

			Debug.Assert(n1.ValueType == NaNBoxing.BoxType.HeapPtr);
			
			var vector2_a = Context.GC.Heap[n1.HeapPtr];
			var payload_a = (RtInstance)vector2_a;

			//NaNBoxing x1 = payload_a.ReadSlot(0, vec2.Instance._link_codescope, context.player);
			//NaNBoxing y1 = payload_a.ReadSlot(1, vec2.Instance._link_codescope, context.player);

			float x1;
			float y1;

			var store1 = ((RtInstance)payload_a).GetStoreData(this, (ASInstance)payload_a.Type);
			unsafe
			{
				fixed (byte* p = store1)
				{
					x1 = *(float*)p;
					y1 = *((float*)p + 1);
				}
			}

			if (is_positive)
			{
				
				int resultptr = InitCacheInstance(Context.VEC2, stackStPos + dst_index, false, out RtInstance payload_result);

				var store = ((RtInstance)payload_result).GetStoreData(this, (ASInstance)payload_result.Type);
				unsafe
				{
					fixed (byte* p = store)
					{
						*(float*)p = (float)(+x1 );
						*((float*)p + 1) = (float)(+y1 );
					}
				}
			}
			else
			{
				int resultptr = InitCacheInstance(Context.VEC2, stackStPos + dst_index, false, out RtInstance payload_result);

				var store = ((RtInstance)payload_result).GetStoreData(this, (ASInstance)payload_result.Type);
				unsafe
				{
					fixed (byte* p = store)
					{
						*(float*)p = -x1;
						*((float*)p + 1) = -y1;
					}
				}
			}

		}


		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private unsafe void Exec_Mul_Mat22_Vec2(int dst_index, byte** PC, ref ReceiveError error, Span<NaNBoxing> stackslots, int stackStPos
			)
		{
			StackLocater dst;
			StackLocater v1;
			StackLocater v2;


			dst.index = dst_index;
			LoadStackLocater(&v1, PC);
			LoadStackLocater(&v2, PC);

			
			NaNBoxing mat = stackslots[v1.index];
			NaNBoxing vec = stackslots[v2.index];

			if (mat.ValueType == NaNBoxing.BoxType.Null || vec.ValueType == NaNBoxing.BoxType.Null)
			{
				RaiseTypeError_AccessNull(ref error);
				return;
			}

			var m2_a = Context.GC.Heap[mat.HeapPtr];
			var payload_a = (RtInstance)m2_a;

			float col1_x;
			float col1_y;

			float col2_x;
			float col2_y;

			var store1 = ((RtInstance)payload_a).GetStoreData(this, (ASInstance)payload_a.Type);
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

			var vec_b = Context.GC.Heap[vec.HeapPtr];
			var payload_b = (RtInstance)vec_b;

			float x;
			float y;
			var store2 = ((RtInstance)payload_b).GetStoreData(this, (ASInstance)payload_b.Type);
			unsafe
			{
				fixed (byte* p = store2)
				{
					x = *(float*)p;
					y = *((float*)p + 1);
				}
			}

			int resultptr = InitCacheInstance(Context.VEC2, stackStPos + dst_index , false, out RtInstance payload_result);
			var store = ((RtInstance)payload_result).GetStoreData(this, (ASInstance)payload_result.Type);
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


		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private unsafe void Exec_Mul_Mat22_Mat22(int dst_index, byte** PC, ref ReceiveError error, Span<NaNBoxing> stackslots, int stackStPos
			)
		{
			StackLocater dst;
			StackLocater v1;
			StackLocater v2;


			dst.index = dst_index;
			LoadStackLocater(&v1, PC);
			LoadStackLocater(&v2, PC);


			NaNBoxing m1 = stackslots[v1.index];
			NaNBoxing m2 = stackslots[v2.index];
	
			if (m1.ValueType == NaNBoxing.BoxType.Null || m2.ValueType == NaNBoxing.BoxType.Null)
			{
				RaiseTypeError_AccessNull(ref error);
				return;
			}

			var m2_a = Context.GC.Heap[m1.HeapPtr];
			var payload_a = (RtInstance)m2_a;

			//NaNBoxing x1 = payload_a.ReadSlot(0, vec2.Instance._link_codescope, context.player);
			//NaNBoxing y1 = payload_a.ReadSlot(1, vec2.Instance._link_codescope, context.player);

			float Acol1_x;
			float Acol1_y;

			float Acol2_x;
			float Acol2_y;

			var store1 = ((RtInstance)payload_a).GetStoreData(this, (ASInstance)payload_a.Type);
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

			var m2_b = Context.GC.Heap[m2.HeapPtr];
			var payload_b = (RtInstance)m2_b;
			float Bcol1_x;
			float Bcol1_y;

			float Bcol2_x;
			float Bcol2_y;
			var store2 = ((RtInstance)payload_b).GetStoreData(this, (ASInstance)payload_b.Type);
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

			int resultptr = InitCacheInstance(Context.MAT22, stackStPos + dst_index, false, out RtInstance payload_result);
			var store = ((RtInstance)payload_result).GetStoreData(this, (ASInstance)payload_result.Type);
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

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private unsafe void Exec_Add_Mat22_Mat22(int dst_index, byte** PC, ref ReceiveError error, Span<NaNBoxing> stackslots, int stackStPos
			)
		{
			StackLocater dst;
			StackLocater v1;
			StackLocater v2;


			dst.index = dst_index;
			LoadStackLocater(&v1, PC);
			LoadStackLocater(&v2, PC);


			NaNBoxing m1 = stackslots[v1.index];
			NaNBoxing m2 = stackslots[v2.index];
	
			if (m1.ValueType == NaNBoxing.BoxType.Null || m2.ValueType == NaNBoxing.BoxType.Null)
			{
				RaiseTypeError_AccessNull(ref error);
				return;
			}

			var m2_a = Context.GC.Heap[m1.HeapPtr];
			var payload_a = (RtInstance)m2_a;

			//NaNBoxing x1 = payload_a.ReadSlot(0, vec2.Instance._link_codescope, context.player);
			//NaNBoxing y1 = payload_a.ReadSlot(1, vec2.Instance._link_codescope, context.player);

			float Acol1_x;
			float Acol1_y;

			float Acol2_x;
			float Acol2_y;

			var store1 = ((RtInstance)payload_a).GetStoreData(this, (ASInstance)payload_a.Type);
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

			var m2_b = Context.GC.Heap[m2.HeapPtr];
			var payload_b = (RtInstance)m2_b;
			float Bcol1_x;
			float Bcol1_y;

			float Bcol2_x;
			float Bcol2_y;
			var store2 = ((RtInstance)payload_b).GetStoreData(this, (ASInstance)payload_b.Type);
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

			int resultptr = InitCacheInstance(Context.MAT22, stackStPos + dst_index, false, out RtInstance payload_result);
			var store = ((RtInstance)payload_result).GetStoreData(this, (ASInstance)payload_result.Type);
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


	}
}
