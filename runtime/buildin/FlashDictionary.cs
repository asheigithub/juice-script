using juicescript.ABC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using static juicescript.runtime.Player;

namespace juicescript.runtime.buildin
{
	internal class FlashDictionary
	{
		[NativeFunction("flash.utils.Dictionary$public::Dictionary")]
		public static void Constructor(Context context,
			ASMethod method,
			int scope_ptr, 
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var this_ins = context.GC.Heap[thisPtr.HeapPtr];
			((RtInstance)this_ins).wapperedObject = new Dict();

		}


		private static bool try_convertkey2Number(ReadOnlySpan<char> input, out double v)
		{
			ulong result = 0;
			bool isValid = true;

			const ulong MaxSafeInteger = 9007199254740992UL;

			foreach (char c in input)
			{
				if (c >= '0' && c <= '9')
				{
					ulong digit = (ulong)(c - '0');

					// 检查是否会超过 double 的安全整数范围
					if (result > (MaxSafeInteger - digit) / 10)
					{
						isValid = false;
						break;
					}

					result = result * 10 + digit;
				}
				else
				{
					isValid = false;
					break;
				}
			}

			if (isValid)
			{
				v = result;
				return true;
			}
			else
			{
				v = 0;
				return false;
			}
		}


		private static void ReturnValue(int returnSlotIndex,Context context ,NaNBoxing src )
		{
			if (src.ValueType == NaNBoxing.BoxType.HeapPtr)
			{
				var obj = context.GC.Heap[src.HeapPtr];
				if (obj.Kind == RtHeapTypeKind.INSTANCE && ((ASInstance)obj.Type).Flags.HasFlag(ClassFlags.Struct))
				{
					((RtInstance)obj).MarkFromContainer();
					context.StackSlots[returnSlotIndex] = src;
				}
				else
				{
					context.StackSlots[returnSlotIndex] = src;
				}
			}
			else
			{
				context.StackSlots[returnSlotIndex] = src;
			}
		}



		[NativeFunction("flash.utils.Dictionary$private::indexer_get")]
		public static void Getter(Context context,
			ASMethod method,
			int scope_ptr, 
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			var this_ins = context.GC.Heap[thisPtr.HeapPtr];
			Dict dict = (Dict)((RtInstance)this_ins).wapperedObject;

			NaNBoxing key = scope.ReadSlot(0, context.player);

			if (key.ValueType == NaNBoxing.BoxType.HeapPtr)
			{
				var key_i = context.GC.Heap[key.HeapPtr];
				if (key_i.Kind == RtHeapTypeKind.STRING)
				{
					var str = ((RtString)key_i).Str;
					double v;
					if (try_convertkey2Number(str, out v))
					{
						key.SetNumber(v);
					}
					NaNBoxing lk;
					if (NaNBoxing.TryCreateLocalString(str, out lk))
					{
						key = lk;
					}
				}


				NaNBoxing value;
				if (dict.dict.TryGetValue(new DictKey() { context = context, key = key }, out value))
				{
					//context.StackSlots[returnSlotIndex] = value;
					ReturnValue(returnSlotIndex, context, value);
				}
				else
				{
					//context.StackSlots[returnSlotIndex].SetUndefined();

					context.StackSlots[returnSlotIndex].setFault(); // 未找到！继续原型链查找。

				}
			}
			else if (key.ValueType == NaNBoxing.BoxType.LocalString)
			{
				Span<char> buffer = stackalloc char[16];
				int len = key.GetLocalStringChars(buffer);

				double v;
				if (try_convertkey2Number(buffer.Slice(0, len), out v))
				{
					key.SetNumber(v);
				}

				NaNBoxing value;
				if (dict.dict.TryGetValue(new DictKey() { context = context, key = key }, out value))
				{
					//context.StackSlots[returnSlotIndex] = value;
					ReturnValue(returnSlotIndex, context, value);
				}
				else
				{
					//context.StackSlots[returnSlotIndex].SetUndefined();
					context.StackSlots[returnSlotIndex].setFault(); // 未找到！继续原型链查找。
				}
			}
			else if (key.ValueType == NaNBoxing.BoxType.Null || key.ValueType == NaNBoxing.BoxType.Undefined
				||
				(key.ValueType == NaNBoxing.BoxType.Number && double.IsNaN(key.Number))
				||
				(key.ValueType == NaNBoxing.BoxType.Float && float.IsNaN(key.FloatValue))
				)
			{
				context.GC.CheckGC(ref error);
				if (context.StackPosition >= Context.STACK_LENGTH)
				{
					context.player.RaiseStackOverflow(ref error);
					return;
				}

				ref NaNBoxing conv = ref context.StackSlots[context.StackPosition];
				context.StackPosition++;
				context.player.ConvertValueType(ref error, key, TypeKind.String, context.STRING, ref conv);
				if (error.raised)
				{
					context.StackPosition--;
					return;
				}

				NaNBoxing value;
				if (dict.dict.TryGetValue(new DictKey() { context = context, key = conv }, out value))
				{
					//context.StackSlots[returnSlotIndex] = value;
					ReturnValue(returnSlotIndex, context, value);
				}
				else
				{
					//context.StackSlots[returnSlotIndex].SetUndefined();
					context.StackSlots[returnSlotIndex].setFault(); // 未找到！继续原型链查找。
				}

				context.StackPosition--;
			}
			else
			{
				

				NaNBoxing value;
				if (dict.dict.TryGetValue(new DictKey() { context = context, key = key }, out value))
				{
					ReturnValue(returnSlotIndex, context, value);
				}
				else
				{
					//context.StackSlots[returnSlotIndex].SetUndefined();
					context.StackSlots[returnSlotIndex].setFault(); // 未找到！继续原型链查找。

				}
			}

		}

		[NativeFunction("flash.utils.Dictionary$private::indexer_set")]
		public static void Setter(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			var this_ins = context.GC.Heap[thisPtr.HeapPtr];
			Dict dict = (Dict)((RtInstance)this_ins).wapperedObject;

			NaNBoxing key = scope.ReadSlot(0, context.player);
			NaNBoxing value = scope.ReadSlot(1, context.player);

			if (key.ValueType == NaNBoxing.BoxType.HeapPtr)
			{
				var key_i = context.GC.Heap[key.HeapPtr];
				if (key_i.Kind == RtHeapTypeKind.STRING)
				{
					var str = ((RtString)key_i).Str;
					double v;
					if (try_convertkey2Number(str, out v))
					{
						key.SetNumber(v);
					}
					NaNBoxing lk;
					if (NaNBoxing.TryCreateLocalString(str, out lk))
					{
						key = lk;
					}
				}

				dict.dict[new DictKey() { key = key, context = context }] = value;

			}
			else if (key.ValueType == NaNBoxing.BoxType.LocalString)
			{ 
				Span<char> buffer = stackalloc char[16];
				int len = key.GetLocalStringChars(buffer);

				double v;
				if (try_convertkey2Number(buffer.Slice(0, len), out v))
				{
					key.SetNumber(v);
				}

				dict.dict[new DictKey() { key = key, context = context }] = value;
			}
			else if (key.ValueType == NaNBoxing.BoxType.Null || key.ValueType == NaNBoxing.BoxType.Undefined
				||
				(key.ValueType == NaNBoxing.BoxType.Number && double.IsNaN(key.Number))
				||
				(key.ValueType == NaNBoxing.BoxType.Float && float.IsNaN(key.FloatValue))
				)
			{
				context.GC.CheckGC(ref error);
				if (context.StackPosition >= Context.STACK_LENGTH)
				{
					context.player.RaiseStackOverflow(ref error);
					return;
				}

				ref NaNBoxing conv = ref context.StackSlots[context.StackPosition];
				context.StackPosition++;
				context.player.ConvertValueType(ref error, key, TypeKind.String, context.STRING, ref conv);
				if (error.raised)
				{
					context.StackPosition--;
					return;
				}

				dict.dict[new DictKey() { key = conv, context = context }] = value;
				context.StackPosition--;
			}
			else
			{

				dict.dict[new DictKey() { key = key, context = context }] = value;
			}

		}




		[NativeFunction("flash.utils.Dictionary$private::indexer_delete")]
		public static void Delete(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{

			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			var this_ins = context.GC.Heap[thisPtr.HeapPtr];
			Dict dict = (Dict)((RtInstance)this_ins).wapperedObject;

			NaNBoxing key = scope.ReadSlot(0, context.player);

			if (key.ValueType == NaNBoxing.BoxType.HeapPtr)
			{
				var key_i = context.GC.Heap[key.HeapPtr];
				if (key_i.Kind == RtHeapTypeKind.STRING)
				{
					var str = ((RtString)key_i).Str;
					double v;
					if (try_convertkey2Number(str, out v))
					{
						key.SetNumber(v);
					}

					NaNBoxing lk;
					if (NaNBoxing.TryCreateLocalString(str, out lk))
					{
						key = lk;
					}

				}
				dict.dict.Remove(new DictKey() { context = context, key = key });
				context.StackSlots[returnSlotIndex].SetBoolean( true);
				
			}
			else if (key.ValueType == NaNBoxing.BoxType.LocalString)
			{
				Span<char> buffer = stackalloc char[16];
				int len = key.GetLocalStringChars(buffer);

				double v;
				if (try_convertkey2Number(buffer.Slice(0, len), out v))
				{
					key.SetNumber(v);
				}

				dict.dict.Remove(new DictKey() { context = context, key = key });
				context.StackSlots[returnSlotIndex].SetBoolean(true);
			}
			else if (key.ValueType == NaNBoxing.BoxType.Null || key.ValueType == NaNBoxing.BoxType.Undefined
				||
				(key.ValueType == NaNBoxing.BoxType.Number && double.IsNaN(key.Number))
				||
				(key.ValueType == NaNBoxing.BoxType.Float && float.IsNaN(key.FloatValue))
				)
			{
				context.GC.CheckGC(ref error);
				if (context.StackPosition >= Context.STACK_LENGTH)
				{
					context.player.RaiseStackOverflow(ref error);
					return;
				}

				ref NaNBoxing conv = ref context.StackSlots[context.StackPosition];
				context.StackPosition++;
				context.player.ConvertValueType(ref error, key, TypeKind.String, context.STRING, ref conv);
				if (error.raised)
				{
					context.StackPosition--;
					return;
				}
				dict.dict.Remove(new DictKey() { context = context, key = conv });
				context.StackSlots[returnSlotIndex].SetBoolean(true);
				context.StackPosition--;
			}
			else
			{
				dict.dict.Remove(new DictKey() { context = context, key = key });
				context.StackSlots[returnSlotIndex].SetBoolean(true);
			}
		}





		[NativeFunction("flash.utils.Dictionary$private::getIterator")]
		public static void GetIterator(Context context,
			ASMethod method,
			int scope_ptr, 
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];

			var this_ins = context.GC.Heap[thisPtr.HeapPtr];

			var dictScript = (ASScript)this_ins.Type._link_codescope.Parent.Container;

			var iter_class = dictScript.Traits[1].Class;

			context.player.InitASClass(iter_class, ref error);
			if (error.raised)
			{
				return;
			}

			context.player.InitCacheInstance(iter_class, returnSlotIndex,true);

		}


		[NativeFunction("FilePrivateNS:Dictionary.dict_iter$public::next")]
		public static void Iter_Next(Context context,
			ASMethod method,
			int scope_ptr, 
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
			var iter_ins = context.GC.Heap[thisPtr.HeapPtr];

			var dict_ptr = scope.ReadSlot(0, context.player);
			var dict_ins = context.GC.Heap[dict_ptr.HeapPtr];

			Dict dict = (Dict)((RtInstance)dict_ins).wapperedObject;

			var result_ptr = scope.ReadSlot(1, context.player);
			var result = context.GC.Heap[result_ptr.HeapPtr];
			RtInstance result_obj = (RtInstance)result;

			var index = ((RtInstance)iter_ins).ReadSlot(0, iter_ins.Type._link_codescope, context.player);
#if DEBUG
			if (index.ValueType != NaNBoxing.BoxType.Int)
			{
				throw new InvalidOperationException();
			}
#endif

			int i = index.IntValue;

			if (i < dict.dict.Count)
			{
				
				//迭代器 递增
				index.SetInt(i + 1);
				((RtInstance)iter_ins).SetSlot(index, 0, iter_ins.Type._link_codescope, context.player);


				NaNBoxing f = default; f.SetBoolean(false);
				result_obj.SetSlot(f, 0, result.Type._link_codescope, context.player);

				var kv = dict.dict.Skip(i).First();
#if DEBUG
				if (kv.Key.context != context)
					throw new InvalidOperationException();
#endif

				

				result_obj.SetSlot(kv.Key.key, 1, result.Type._link_codescope, context.player);
				result_obj.SetSlot(kv.Value, 2, result.Type._link_codescope, context.player);

			}
			else
			{
				NaNBoxing f = default;f.SetBoolean(true);
				result_obj.SetSlot(f, 0, result.Type._link_codescope, context.player);
			}
		}


		[NativeFunction("FilePrivateNS:Dictionary.dict_iter$public::close")]
		public static void Iter_Close(Context context,
			ASMethod method,
			int scope_ptr, 
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			//空实现，没有需要释放的资源
			
		}


		struct DictKey : IEquatable<DictKey>
		{
			public NaNBoxing key;
			public Context context;


			public override int GetHashCode()
			{
				//return key.Raw.GetHashCode();

				if (key.ValueType != NaNBoxing.BoxType.HeapPtr)
				{
					switch (key.ValueType)
					{
						case NaNBoxing.BoxType.Int:
							return ((double)key.IntValue).GetHashCode();
						case NaNBoxing.BoxType.Uint:
							return ((double)key.UIntValue).GetHashCode();
						case NaNBoxing.BoxType.Sbyte:
							return ((double)key.SByteValue).GetHashCode();
						case NaNBoxing.BoxType.Byte:
							return ((double)key.ByteValue).GetHashCode();
						case NaNBoxing.BoxType.Short:
							return ((double)key.ShortValue).GetHashCode();
						case NaNBoxing.BoxType.UShort:
							return ((double)key.UShortValue).GetHashCode();
						case NaNBoxing.BoxType.Float:
							return ((double)key.FloatValue).GetHashCode();	
						case NaNBoxing.BoxType.Number:
							return key.Number.GetHashCode();
						case NaNBoxing.BoxType.Undefined:
						case NaNBoxing.BoxType.Null:
						case NaNBoxing.BoxType.Boolean:
						case NaNBoxing.BoxType.HeapPtr:
						case NaNBoxing.BoxType.Fault:
						default:
							return key.Raw.GetHashCode();
					}
				}
				else
				{
					//进入这里的key,肯定都经过了GetSaveValue的保存到堆操作了。

					var ins = context.GC.Heap[key.HeapPtr];
					switch (ins.Kind)
					{
						case RtHeapTypeKind.CLASS:
						case RtHeapTypeKind.GLOBAL:
						case RtHeapTypeKind.NAMESPACE:
						case RtHeapTypeKind.ARRAY:
						case RtHeapTypeKind.CLOSURE:
							return key.HeapPtr.GetHashCode();
							
						case RtHeapTypeKind.STRING:
							return ((RtString)ins).Str.GetHashCode();
						case RtHeapTypeKind.INSTANCE:
							{ 
								RtInstance rt = (RtInstance)ins;
								if (((ASInstance)ins.Type).Flags.HasFlag(ClassFlags.Wapper))
								{
									return rt.wapperedObject.GetHashCode();
								}
								else if (((ASInstance)ins.Type).Flags.HasFlag(ClassFlags.Struct))
								{
									//throw new NotImplementedException(); //计算struct的hashcode
									var layoutsize = ins.Type._link_codescope.TypeLayout.Size;
									var data = rt.GetStoreData(context.player,(ASInstance)ins.Type).Slice(0, layoutsize);

									HashCode hash = new HashCode();

									while (data.Length>4)
									{
										hash.Add(data[0] << 24 | data[1] << 16 | data[2] << 8 | data[3]);
										data = data.Slice(4);
									}

									if (data.Length == 3)
									{
										hash.Add(data[0] << 16 | data[1] << 8 | data[2]);
									}
									else if (data.Length == 2)
									{
										hash.Add(data[0] << 8 | data[1]);
									}
									else
									{
										hash.Add(data[0]);
									}

									return hash.ToHashCode();

								}
								else
								{ 
									return key.HeapPtr.GetHashCode();
								}
							}

						case RtHeapTypeKind.VECTOR:
							//throw new NotImplementedException();
							return key.HeapPtr.GetHashCode();

						case RtHeapTypeKind.STACK_CACHE_OBJ:
						case RtHeapTypeKind.DYNAMIC_PROPERTYS:
						case RtHeapTypeKind.SHAPE:
						case RtHeapTypeKind.MethodScope:
						default:
							throw new InvalidOperationException();
					}

				}


			}

			public bool Equals(DictKey other)
			{
#if DEBUG
				if (other.context != context)
					throw new InvalidOperationException();
#endif

				if (key.Raw == other.key.Raw )
				{
					return true;
				}
				else 
				{
					//执行 === 操作[IsStrictlyEqual]
					return context.player.IsStrictlyEqual(key, other.key);

				}
				
			}
		}

		class Dict : RtWapperBase
		{
			
			public Dictionary<DictKey,NaNBoxing> dict = new Dictionary<DictKey, NaNBoxing>();

			public override void OnDelete()
			{
				dict = null;
			}

			public override void OnGCMark(Context context)
			{
				//throw new NotImplementedException();

				foreach (var item in dict)
				{
#if DEBUG
					if (item.Key.context != context)
						throw new InvalidOperationException();
#endif

					if (item.Key.key.ValueType == NaNBoxing.BoxType.HeapPtr)
					{
						context.GC.mark(context.GC.Heap[item.Key.key.HeapPtr]);
					}

					if (item.Value.ValueType == NaNBoxing.BoxType.HeapPtr)
					{
						context.GC.mark(context.GC.Heap[item.Value.HeapPtr]);
					}
					


				}

			}
		}



	}
}
