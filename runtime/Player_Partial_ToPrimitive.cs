using juicescript.ABC;
using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace juicescript.runtime
{
	public partial class Player
	{
		internal enum HINT
		{
			h_number,
			h_string
		}

		internal bool IsNumeric(NaNBoxing value)
		{
			switch (value.ValueType)
			{
				case NaNBoxing.BoxType.Number:
				case NaNBoxing.BoxType.Int:
				case NaNBoxing.BoxType.Uint:
				case NaNBoxing.BoxType.Sbyte:
				case NaNBoxing.BoxType.Byte:
				case NaNBoxing.BoxType.Short:
				case NaNBoxing.BoxType.UShort:
				case NaNBoxing.BoxType.Float:
					return true;
				default:
					return false;
			}
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		internal bool IsPrimitive(NaNBoxing value)
		{
			return (value.ValueType != NaNBoxing.BoxType.HeapPtr || Context.GC.Heap[value.HeapPtr].TypeKind == RtHeapTypeKind.STRING);
		}

		/// <summary>
		/// ToPrimitive操作
		/// 切勿将value所在的槽当作 tmp 槽传入，否则可能被valueOf或toString的返回值覆盖，切记切记
		/// </summary>
		/// <param name="error"></param>
		/// <param name="value"></param>
		/// <param name="hint"></param>
		/// <param name="scope_ptr"></param>
		/// <param name="result"></param>
		/// <param name="tmp"></param>
		/// <param name="stackslots"></param>
		/// <param name="stackStPos"></param>
		/// <param name="caller_bindthis_ptr"></param>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		/// <exception cref="NotImplementedException"></exception>
		internal NaNBoxing ToPrimitive(ref ReceiveError error, NaNBoxing value , HINT hint , int scope_ptr ,StackLocater result, StackLocater tmp,
			Span<NaNBoxing> stackslots, int stackStPos , NaNBoxing caller_bindthis_ptr)
		{

			if (IsPrimitive(value))
			{
				return value;
			}

			var scope = Context.GC.Heap[scope_ptr];
//#if DEBUG
//			if (scope.TypeKind != RtHeapTypeKind.MethodScope)
//			{
//				throw new InvalidOperationException();
//			}
//#endif
			


			var instance = Context.GC.Heap[value.HeapPtr];
			var ns_set = scope.Type._link_codescope.NamespaceSet;

			ASContainer as_type = null;

			switch (instance.TypeKind)
			{
				case RtHeapTypeKind.CLASS:
				case RtHeapTypeKind.GLOBAL:
					as_type = ((RtPayloadScriptClass)instance.facility).Meta;
					break;
				case RtHeapTypeKind.STRING:
					as_type = Context.STRING.Instance;
					break;
				case RtHeapTypeKind.INSTANCE:
				case RtHeapTypeKind.VECTOR:
				case RtHeapTypeKind.ARRAY:
				case RtHeapTypeKind.CLOSURE:
					as_type = instance.Type;
					break;
				case RtHeapTypeKind.NAMESPACE:
					throw new NotImplementedException();
				case RtHeapTypeKind.MethodScope:
				case RtHeapTypeKind.STACK_CACHE_OBJ:
				default:
					throw new InvalidOperationException();
			}

			int code = MultiNameLSearch(ns_set, instance.TypeKind, as_type, hint == HINT.h_string ? "toString":"valueOf", tmp, stackslots, stackStPos, value, caller_bindthis_ptr, ref error,true);
			switch (code)
			{
				case 0:
					break;
				case 1:
					//有异常产生
					return default;
				case 2:
					Context.GC.CheckGC(ref error);
					RaiseTypeError_Ambiguous(ref error,  hint == HINT.h_string ? "toString" : "valueOf");
					return default;
				default:
					throw new InvalidOperationException();
			}
			NaNBoxing fun = LoadValue(stackslots[tmp.index], -1, ref error, stackslots, stackStPos + tmp.index);
			
			if (error.raised) //由于object原型的存在，这里是肯定能找到的。找不到就报错吧，不管了
			{
				return default;
			}

			if (fun.ValueType != NaNBoxing.BoxType.HeapPtr)
			{
				RaiseTypeError(ref error, fun, TypeKind.Function);
				return default;
			}

			var funinstance = Context.GC.Heap[fun.HeapPtr];
			if (funinstance.TypeKind != RtHeapTypeKind.CLOSURE)
			{
				RaiseTypeError(ref error, fun, TypeKind.Function);
				return default;
			}

			unsafe
			{
				NaNBoxing ret= RunMethod(((ASMethodBody)funinstance.Type).Method, value, ((RtPayloadClosure)funinstance.facility).ScopePtr , ((RtPayloadClosure)funinstance.facility).ScopeType , 0, null, null, ref error, stackStPos + tmp.index,fun.HeapPtr);
				if (error.raised)
				{
					return default;
				}

				if (IsPrimitive(ret))
				{
					stackslots[result.index] = ret;
					return ret;
				}
				
				

			}

			//查找tostring,如果tostring后还不是primitive，则报错。
			code = MultiNameLSearch(ns_set, instance.TypeKind, as_type, hint == HINT.h_string ? "valueOf" : "toString", tmp, stackslots, stackStPos, value, caller_bindthis_ptr, ref error, true);
			switch (code)
			{
				case 0:
					break;
				case 1:
					//有异常产生
					return default;
				case 2:
					Context.GC.CheckGC(ref error);
					RaiseTypeError_Ambiguous(ref error, hint == HINT.h_string ? "valueOf" : "toString");
					return default;
				default:
					throw new InvalidOperationException();
			}

			fun = LoadValue(stackslots[tmp.index], -1, ref error,  stackslots, stackStPos + tmp.index);
#if DEBUG
			if (error.raised) //由于object原型的存在，这里是肯定能找到的。
			{
				throw new NotImplementedException();
			}
#endif
			if (fun.ValueType != NaNBoxing.BoxType.HeapPtr)
			{
				RaiseTypeError(ref error, fun, TypeKind.Function);
				return default;
			}

			funinstance = Context.GC.Heap[fun.HeapPtr];
			if (funinstance.TypeKind != RtHeapTypeKind.CLOSURE)
			{
				RaiseTypeError(ref error, fun, TypeKind.Function);
				return default;
			}

			unsafe
			{
				NaNBoxing ret = RunMethod(((ASMethodBody)funinstance.Type).Method, value, ((RtPayloadClosure)funinstance.facility).ScopePtr, ((RtPayloadClosure)funinstance.facility).ScopeType, 0, null, null, ref error, stackStPos + tmp.index,fun.HeapPtr);
				if (error.raised)
				{
					return default;
				}
				
				if (IsPrimitive(ret))
				{
					stackslots[result.index] = ret;
					return ret;
				}

			}

			RaiseTypeError_ConvertToPrimitive(ref error, value);
			return default;

		}

	}
}
