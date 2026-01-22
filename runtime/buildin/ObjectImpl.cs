using juicescript.ABC;
using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static juicescript.runtime.Player;

namespace juicescript.runtime.buildin
{
	internal class ObjectImpl
	{
		[NativeFunction(".Object$@::valueOf")]
		public static void Object_ValueOf(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			context.StackSlots[returnSlotIndex] = thisPtr;
		}

		[NativeFunction(".Object$@::toString")]
		public static void Object_toString(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			context.player.ConvertValueType(ref error, thisPtr, TypeKind.String, context.STRING, ref context.StackSlots[returnSlotIndex], 0, default, true);
		}

		internal static bool Find_ASContainer_Prop(ASContainer type,string name)
		{
			var scope = type._link_codescope;
			for (int i = scope.Members.Count - 1; i >= 0; i--)
			{
				var member = scope.Members[i];
				if (string.CompareOrdinal(member.QName.Name, name) == 0 && member.QName.Namespace.Kind == NamespaceKind.Package)
				{
					return true;
				}
			}

			for (int i = 0; i < type._vtable.Items.Count; i++)
			{
				var f = type._vtable.Items[i];
				if (string.CompareOrdinal(f.Trait.QName.Name, name) == 0 && f.Trait.QName.Namespace.Kind == NamespaceKind.Package)
				{
					return true;
				}

			}
			return false;
		}



		[NativeFunction(".Object$@::hasOwnProperty")]
		public static void Object_hasOwnProperty(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var find_instance_prop = (RtHeapInstance _this , ASInstance type ,string name) => 
			{
				//var scope = type._link_codescope;
				//for (int i = scope.Members.Count - 1; i >= 0; i--)
				//{
				//	var member = scope.Members[i];
				//	if ( string.CompareOrdinal( member.QName.Name , name) == 0 && member.QName.Namespace.Kind == NamespaceKind.Package)
				//	{
				//		return true;
				//	}
				//}

				//for (int i = 0; i < type._vtable.Items.Count; i++)
				//{
				//	var f = type._vtable.Items[i];
				//	if ( string.CompareOrdinal( f.Trait.QName.Name, name) == 0 && f.Trait.QName.Namespace.Kind == NamespaceKind.Package)
				//	{
				//		return true;
				//	}

				//}

				if (Find_ASContainer_Prop(type, name))
				{ 
					return true;
				}

				NaNBoxing o; int match_shape; int slot; RtPayloadDynamic prop;
				return context.player.FindDynamicValue(_this, name, out o, out match_shape, out slot, out prop);
			};

			var find_class_prop = (RtHeapInstance _this,ASClass @class,string name) =>
			{
				//var scope = @class._link_codescope;
				//for (int i = scope.Members.Count - 1; i >= 0; i--)
				//{
				//	var member = scope.Members[i];
				//	if (string.CompareOrdinal(member.QName.Name, name) == 0 && member.QName.Namespace.Kind == NamespaceKind.Package)
				//	{
				//		return true;
				//	}
				//}

				//for (int i = 0; i < @class._vtable.Items.Count; i++)
				//{
				//	var f = @class._vtable.Items[i];
				//	if (string.CompareOrdinal(f.Trait.QName.Name, name) == 0 && f.Trait.QName.Namespace.Kind == NamespaceKind.Package)
				//	{
				//		return true;
				//	}

				//}

				if (Find_ASContainer_Prop(@class, name))
				{
					return true;
				}

				NaNBoxing o; int match_shape; int slot; RtPayloadDynamic prop;
				return context.player.FindDynamicValue(_this, name, out o, out match_shape, out slot, out prop);

			};



			if (thisPtr.ValueType == NaNBoxing.BoxType.HeapPtr)
			{
				NaNBoxing sName = ((RtPayloadMethodScope)context.GC.Heap[scope_ptr].facility).ReadSlot(0, context.player);
				if (sName.ValueType == NaNBoxing.BoxType.HeapPtr)
				{
					RtHeapInstance n = context.GC.Heap[sName.HeapPtr];
#if DEBUG
					if (n.TypeKind != RtHeapTypeKind.STRING)
					{
						throw new InvalidOperationException();
					}
#endif

					var name = ((RtPayloadString)n.facility).Str;


					RtHeapInstance _this = context.GC.Heap[thisPtr.HeapPtr];

					switch (_this.TypeKind)
					{
						case RtHeapTypeKind.CLASS:
							{
								context.StackSlots[returnSlotIndex].SetBoolean(find_class_prop(_this, (ASClass)((RtPayloadScriptClass)_this.facility).Meta, name));
							}
							break;
						case RtHeapTypeKind.GLOBAL:

							{
								context.StackSlots[returnSlotIndex].SetBoolean(find_instance_prop(_this, context.OBJECT.Instance, name));
							}

							break;
						case RtHeapTypeKind.STRING:
							{
								var finder = () => {
									for (int i = 0; i < context.STRING.Instance._vtable.Items.Count; i++)
									{
										var f = context.STRING.Instance._vtable.Items[i];
										if (string.CompareOrdinal(f.Trait.QName.Name, name) == 0 && f.Trait.QName.Namespace.Kind == NamespaceKind.Package)
										{
											return true;
										}
									}
									return false;
								};
								context.StackSlots[returnSlotIndex].SetBoolean(finder());
							}
							break;
						case RtHeapTypeKind.INSTANCE:

							{
								context.StackSlots[returnSlotIndex].SetBoolean(find_instance_prop(_this, (ASInstance)_this.Type, name));
							}

							break;
						case RtHeapTypeKind.NAMESPACE:

							{
								var finder = () => {
									for (int i = 0; i < context.NAMESPACE.Instance._vtable.Items.Count; i++)
									{
										var f = context.NAMESPACE.Instance._vtable.Items[i];
										if (string.CompareOrdinal(f.Trait.QName.Name, name) == 0 && f.Trait.QName.Namespace.Kind == NamespaceKind.Package)
										{
											return true;
										}
									}
									return false;
								};								
								context.StackSlots[returnSlotIndex].SetBoolean(finder());
								
							}

							break;
						case RtHeapTypeKind.ARRAY:
							{
								uint isindex;
								if (uint.TryParse(name, out isindex))
								{
									bool isoutofindex_or_ishole;
									//context.player.LoadSlotFromArray(isindex, _this, out isoutofindex_or_ishole);
									NaNBoxing result = ((RtPayloadArray)_this.facility).ReadSlot(isindex, context.player, out isoutofindex_or_ishole);

									if (!isoutofindex_or_ishole)
									{
										context.StackSlots[returnSlotIndex].SetBoolean(true);
										break;
									}
								}
								
								context.StackSlots[returnSlotIndex].SetBoolean(find_instance_prop(_this, context.ARRAY.Instance, name));
								
							}
							break;
						case RtHeapTypeKind.VECTOR:
							{
								var finder = () => {
									for (int i = 0; i <  _this.Type._vtable.Items.Count; i++)
									{
										var f = _this.Type._vtable.Items[i];
										if (string.CompareOrdinal(f.Trait.QName.Name, name) == 0 && f.Trait.QName.Namespace.Kind == NamespaceKind.Package)
										{
											return true;
										}
									}
									return false;
								};
								context.StackSlots[returnSlotIndex].SetBoolean(finder());
							}
							break;
						case RtHeapTypeKind.CLOSURE:

							{
								NaNBoxing o; int match_shape; int slot; RtPayloadDynamic prop;
								bool exists = context.player.FindDynamicValue(_this, name, out o, out match_shape, out slot, out prop);
								context.StackSlots[returnSlotIndex].SetBoolean(exists);
							}

							//throw new NotImplementedException();
							break;
						case RtHeapTypeKind.SHAPE:
						case RtHeapTypeKind.MethodScope:
						case RtHeapTypeKind.DYNAMIC_PROPERTYS:
						case RtHeapTypeKind.STACK_CACHE_OBJ:
						default:
							throw new InvalidOperationException();
					}



				}
				else
				{
					context.StackSlots[returnSlotIndex].SetBoolean(false);
				}
			}
			else if (thisPtr.ValueType == NaNBoxing.BoxType.Undefined || thisPtr.ValueType == NaNBoxing.BoxType.Null)
			{
				context.player.RaiseTypeError_AccessNull(ref error);
			}
			else
			{
				context.StackSlots[returnSlotIndex].SetBoolean(false);
			}

		}


		[NativeFunction(".Object$@::isPrototypeOf")]
		public static void Object_isPrototypeOf(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var theClass = ((RtPayloadMethodScope)context.GC.Heap[scope_ptr].facility).ReadSlot(0, context.player);


/*---
	esid: sec-object.prototype.isprototypeof
	description: >
	  Object.prototype.isPrototypeOf returns true if either parameter V
	  and O refer to the same object or O is in [[Prototype]] chain of V.
	info: |
	  Object.prototype.isPrototypeOf ( V )

	  ...
	  3. Repeat,
		a. Set V to ? V.[[GetPrototypeOf]]().
		b. If V is null, return false.
		c. If SameValue(O, V) is true, return true.
---*/

			if (thisPtr.ValueType == NaNBoxing.BoxType.HeapPtr)
			{
				if (context.player.IsPrimitive(theClass))
				{
					context.StackSlots[returnSlotIndex].SetBoolean(false);
				}
				else
				{
#if DEBUG
					if (theClass.ValueType != NaNBoxing.BoxType.HeapPtr)
					{
						throw new InvalidOperationException();
					}
#endif

					int i = 0;
					var V = context.GC.Heap[theClass.HeapPtr];
					
					while (i<32)
					{
						int V_ = context.player.GetProtoPtr(V);
						i++;
						if (V_ == 0)
						{
							context.StackSlots[returnSlotIndex].SetBoolean(false);
							return;
						}
						else if (V_ == thisPtr.HeapPtr)
						{
							context.StackSlots[returnSlotIndex].SetBoolean(true);
							return;
						}
						else
						{
							V = context.GC.Heap[V_];
						}

					}

					context.StackSlots[returnSlotIndex].SetBoolean(false);
				}
			}
			else if (thisPtr.ValueType == NaNBoxing.BoxType.Undefined || thisPtr.ValueType == NaNBoxing.BoxType.Null)
			{
				if ( context.player.IsPrimitive( theClass))
				{
					context.StackSlots[returnSlotIndex].SetBoolean(false);
				}
				else
				{
					context.player.RaiseTypeError_AccessNull(ref error);
				}
			}
			else
			{
				context.StackSlots[returnSlotIndex].SetBoolean(false);
			}
		}



		[NativeFunction("FilePrivateNS:IIterator.object_iterator$public::next")]
		public static void Iter_Next(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtPayloadMethodScope)context.GC.Heap[scope_ptr].facility;
			var iter_ins = context.GC.Heap[thisPtr.HeapPtr];
			var iter = (RtPayloadInstance)iter_ins.facility;

			
			var _index = iter.ReadSlot(0, iter_ins.Type._link_codescope, context.player);
			var _count = iter.ReadSlot(1, iter_ins.Type._link_codescope, context.player);
			
			var _result = scope.ReadSlot(1, context.player);
			var _obj = scope.ReadSlot(0,context.player);


#if DEBUG
			if (_obj.ValueType != NaNBoxing.BoxType.HeapPtr ||  _index.ValueType != NaNBoxing.BoxType.Int)
			{
				throw new InvalidOperationException();
			}

			if (_result.ValueType != NaNBoxing.BoxType.HeapPtr)
			{
				throw new InvalidOperationException();
			}

#endif
			var result_ins = context.GC.Heap[_result.HeapPtr];
			var result = (RtPayloadInstance)result_ins.facility;
			var obj_ins = context.GC.Heap[_obj.HeapPtr];

		
			if (obj_ins.TypeKind == RtHeapTypeKind.ARRAY)
			{
				if (_index.IntValue == 0 && _count.IntValue == 0) //初始进入
				{
					_index.SetInt(-1);_count.SetInt(0);					
				}

				if (_index.IntValue < 0)
				{
					
					uint k;NaNBoxing v;uint next_index;
					if(((RtPayloadArray)obj_ins.facility).TryReadIterItem(  _count.IntValue , out k,out next_index,out v, context) )
					{
						
						_count.SetInt((int)next_index);

						iter.SetSlot(_index, 0, iter_ins.Type._link_codescope, context.player);
						iter.SetSlot(_count, 1, iter_ins.Type._link_codescope, context.player);

						NaNBoxing done = default; done.SetBoolean(false);
						result.SetSlot(done, 0, result_ins.Type._link_codescope, context.player);

						NaNBoxing key = default; key.SetUInt(k);
						
						result.SetSlot(key, 1, result_ins.Type._link_codescope, context.player);
						result.SetSlot(v, 2, result_ins.Type._link_codescope, context.player);

						return;

					}
					else
					{ 
						_index.SetInt(0);
						_count.SetInt(0);
						//进入property阶段
					}
				}
				//throw new NotImplementedException();
			}
			else if (obj_ins.TypeKind == RtHeapTypeKind.VECTOR)
			{
				var vector = (RtPayloadVector)obj_ins.facility;
				
				int validid;int maxid;
				if (vector.IsValidIndexRange(_index,out validid,out maxid,context.player))
				{					
					var _value = vector.ReadSlot(validid, context.player, returnSlotIndex , _obj.HeapPtr);

					_index.SetInt(validid + 1);
					iter.SetSlot(_index, 0, iter_ins.Type._link_codescope, context.player);

					
					NaNBoxing done = default; done.SetBoolean(false);
					result.SetSlot(done, 0, result_ins.Type._link_codescope, context.player);

					NaNBoxing key = default; key.SetInt( validid);

					result.SetSlot(key, 1, result_ins.Type._link_codescope, context.player);
					result.SetSlot(_value, 2, result_ins.Type._link_codescope, context.player);

					return;
				}
				else
				{
					goto lbl_done;
				}
				//throw new NotImplementedException();
			}
			

			int property_ptr = context.player.GetPropertyPtr(obj_ins);

			if(property_ptr > 0)
			{
				var dynamic = context.GC.Heap[property_ptr];
#if DEBUG
				if (dynamic.TypeKind != RtHeapTypeKind.DYNAMIC_PROPERTYS)
				{
					throw new InvalidOperationException();
				}
#endif
				RtPayloadDynamic prop = (RtPayloadDynamic)dynamic.facility;

				if (_index.IntValue == 0)
				{
					_count.SetInt(prop.Slots.Count);
					iter.SetSlot(_count, 1, iter_ins.Type._link_codescope, context.player);
				}

				


			lbl_skip_not_enumerable:
				if (_index.IntValue < _count.IntValue && _index.IntValue < prop.Slots.Count)
				{

					int current = 0;
					var shape_ptr = prop.SHAPE_PTR;


					while (current < _index.IntValue)
					{
						var shape = context.GC.Heap[shape_ptr];
#if DEBUG
						if (shape.TypeKind != RtHeapTypeKind.SHAPE)
						{
							throw new InvalidOperationException();
						}
#endif
						shape_ptr = ((RtPayloadShape)shape.facility).PTR_PARENT;
						current++;
					}

					var shapepayload = ((RtPayloadShape)context.GC.Heap[shape_ptr].facility);
					if (!shapepayload.Attribute.HasFlag(RtPayloadShape.PropertyAttribute.Enumerable))
					{
						_index.SetInt(_index.IntValue + 1);
						goto lbl_skip_not_enumerable;
					}

					_index.SetInt(current + 1);
					iter.SetSlot(_index, 0, iter_ins.Type._link_codescope, context.player);

					NaNBoxing done = default; done.SetBoolean(false);
					result.SetSlot(done, 0, result_ins.Type._link_codescope, context.player);

					NaNBoxing key = default; key.SetHeapPtr(shapepayload.PTR_NAME);
					NaNBoxing value = prop.Slots[prop.Slots.Count - current - 1];

					result.SetSlot(key, 1, result_ins.Type._link_codescope, context.player);
					result.SetSlot(value, 2, result_ins.Type._link_codescope, context.player);

					return;
				}
			}


		lbl_done:
			{
				NaNBoxing done = default; done.SetBoolean(true);
				NaNBoxing u = default; u.SetUndefined();
				result.SetSlot(done, 0, result_ins.Type._link_codescope, context.player);
				result.SetSlot(u, 1, result_ins.Type._link_codescope, context.player);
				result.SetSlot(u, 2, result_ins.Type._link_codescope, context.player);
			}
		}


		[NativeFunction("FilePrivateNS:IIterator.object_iterator$public::close")]
		public static void Iter_Close(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtPayloadMethodScope)context.GC.Heap[scope_ptr].facility;
			var iter_ins = context.GC.Heap[thisPtr.HeapPtr];
			var iter = (RtPayloadInstance)iter_ins.facility;

			NaNBoxing zero = default; zero.SetInt(0);
			iter.SetSlot(zero, 0, iter_ins.Type._link_codescope, context.player);
			iter.SetSlot(zero, 1, iter_ins.Type._link_codescope, context.player);
			

		}


	}

	internal unsafe class IterContxt : RtWapperBase
	{
		public HashSet<int> visitedObjs = new HashSet<int>();

		//经测试，AS3不会屏蔽已经遍历过的Key,省事了。
		//public List<NaNBoxing> keys = new List<NaNBoxing>();

		public byte* PC;
		//public int cache_slot_index;

		public int heapPtr;

		public override void OnDelete()
		{
			//这里不应该会被GC执行到。
			throw new InvalidOperationException();
		}

		public override void OnGCMark(Context context)
		{
			//throw new NotImplementedException();
		}

		internal void Close()
		{
			visitedObjs.Clear();
			//keys.Clear();
		}
	}

}
