using juicescript.ABC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static juicescript.NaNBoxing;

namespace juicescript.runtime
{
	public partial class Player
	{

		int cache_ATERM_UNDEFINED;
		private void RaiseTypeError_ATermUndefined(ref ReceiveError error)
		{

			error.raised = true;
			RtHeapBase _temp;
			int errPtr = Context.GC.AllocInstance(Context.TYPE_ERROR.Instance, out _temp);
			if (errPtr == 0)
			{
				error.error.setFault();
			}
			else
			{
				NaNBoxing errName = new NaNBoxing();
				errName.SetHeapPtr(cache_TYPE_ERROR_NAME, (byte)RtHeapTypeKind.STRING);
				((RtInstance)_temp).SetSlot(errName, 1, Context.TYPE_ERROR.Instance._link_codescope, this);

				RtHeapBase error_instance = _temp;
				RtInstance payloadInstance = (RtInstance)error_instance;

				NaNBoxing naNBoxing = new NaNBoxing();
				naNBoxing.SetHeapPtr(cache_ATERM_UNDEFINED, (byte)RtHeapTypeKind.STRING);
				payloadInstance.SetSlot(naNBoxing, 0, error_instance.Type._link_codescope, this);

				error.error.SetHeapPtr(errPtr, (byte)RtHeapTypeKind.INSTANCE);
			}
		}

		int cache_MUSTVINALLA;
		private void RaiseTypeError_MustVinallaObject(ref ReceiveError error)
		{

			error.raised = true;
			RtHeapBase _temp;
			int errPtr = Context.GC.AllocInstance(Context.TYPE_ERROR.Instance, out _temp);
			if (errPtr == 0)
			{
				error.error.setFault();
			}
			else
			{
				NaNBoxing errName = new NaNBoxing();
				errName.SetHeapPtr(cache_TYPE_ERROR_NAME, (byte)RtHeapTypeKind.STRING);
				((RtInstance)_temp).SetSlot(errName, 1, Context.TYPE_ERROR.Instance._link_codescope, this);

				RtHeapBase error_instance = _temp;
				RtInstance payloadInstance = (RtInstance)error_instance;

				NaNBoxing naNBoxing = new NaNBoxing();
				naNBoxing.SetHeapPtr(cache_MUSTVINALLA, (byte)RtHeapTypeKind.STRING);
				payloadInstance.SetSlot(naNBoxing, 0, error_instance.Type._link_codescope, this);

				error.error.SetHeapPtr(errPtr, (byte)RtHeapTypeKind.INSTANCE);
			}

		}

		int cache_Instantiation_non_constructor;
		private void RaiseTypeError_Instantiation_non_constructor(ref ReceiveError error)
		{
			error.raised = true;
			RtHeapBase _temp;
			int errPtr = Context.GC.AllocInstance(Context.TYPE_ERROR.Instance, out _temp);
			if (errPtr == 0)
			{
				error.error.setFault();
			}
			else
			{
				NaNBoxing errName = new NaNBoxing();
				errName.SetHeapPtr(cache_TYPE_ERROR_NAME, (byte)RtHeapTypeKind.STRING);
				((RtInstance)_temp).SetSlot(errName, 1, Context.TYPE_ERROR.Instance._link_codescope, this);

				RtHeapBase error_instance = _temp;
				RtInstance payloadInstance = (RtInstance)error_instance;

				NaNBoxing naNBoxing = new NaNBoxing();
				naNBoxing.SetHeapPtr(cache_Instantiation_non_constructor, (byte)RtHeapTypeKind.STRING);
				payloadInstance.SetSlot(naNBoxing, 0, error_instance.Type._link_codescope, this);

				error.error.SetHeapPtr(errPtr, (byte)RtHeapTypeKind.INSTANCE);
			}
		}


		private void RaiseTypeError_RunMethodAsConstructor(ref ReceiveError error, ASMethod method)
		{
			// Cannot call method Function/http://adobe.com/AS3/2006/builtin::apply() as constructor.
			error.raised = true;
			RtHeapBase _temp;
			int errPtr = Context.GC.AllocInstance(Context.TYPE_ERROR.Instance, out _temp);
			if (errPtr == 0)
			{
				error.error.setFault();
			}
			else
			{
				NaNBoxing errName = new NaNBoxing();
				errName.SetHeapPtr(cache_TYPE_ERROR_NAME, (byte)RtHeapTypeKind.STRING);
				((RtInstance)_temp).SetSlot(errName, 1, Context.TYPE_ERROR.Instance._link_codescope, this);

				RtHeapBase error_instance = _temp;
				RtInstance payloadInstance = (RtInstance)error_instance;

				NaNBoxing naNBoxing = new NaNBoxing();

				int messagePtr = Context.GC.AllocString($"Cannot call method Function/{( method.Trait == null ? GetMethodKey(method)  : method.Trait.ToDebugPropertyName() )} as constructor."); ;
				if (messagePtr != 0)
				{
					naNBoxing.SetHeapPtr(messagePtr, (byte)RtHeapTypeKind.STRING);
					payloadInstance.SetSlot(naNBoxing, 0, _temp.Type._link_codescope, this);
				}
				else
				{
					naNBoxing.SetHeapPtr(cache_OUTOFMEMORY_STR, (byte)RtHeapTypeKind.STRING);
					payloadInstance.SetSlot(naNBoxing, 0, _temp.Type._link_codescope, this);
				}

				error.error.SetHeapPtr(errPtr, (byte)RtHeapTypeKind.INSTANCE);
			}
		}


		int cache_instanceof_error;
		private void RaiseTypeError_InstanceOf(ref ReceiveError error)
		{
			error.raised = true;
			RtHeapBase _temp;
			int errPtr = Context.GC.AllocInstance(Context.TYPE_ERROR.Instance, out _temp);
			if (errPtr == 0)
			{
				error.error.setFault();
			}
			else
			{
				NaNBoxing errName = new NaNBoxing();
				errName.SetHeapPtr(cache_TYPE_ERROR_NAME, (byte)RtHeapTypeKind.STRING);
				((RtInstance)_temp).SetSlot(errName, 1, Context.TYPE_ERROR.Instance._link_codescope, this);

				RtHeapBase error_instance = _temp;
				RtInstance payloadInstance = (RtInstance)error_instance;

				NaNBoxing naNBoxing = new NaNBoxing();
				naNBoxing.SetHeapPtr(cache_instanceof_error, (byte)RtHeapTypeKind.STRING);
				payloadInstance.SetSlot(naNBoxing, 0, error_instance.Type._link_codescope, this);

				error.error.SetHeapPtr(errPtr, (byte)RtHeapTypeKind.INSTANCE);
			}
		}


		internal int cache_REFERENCE_ERROR_NAME;

		private void RaiseReferenceError_RTQNameNotFound(ref ReceiveError error, NaNBoxing ns, ReadOnlySpan<char> searchName, NaNBoxing instance)//RtHeapInstance instance)
		{

			error.raised = true;
			RtHeapBase _temp = null;
			int errPtr = Context.GC.AllocInstance(Context.REFERENCE_ERROR.Instance, out _temp);
			if (errPtr == 0)
			{
				error.error.setFault();
			}
			else
			{
				NaNBoxing errName = new NaNBoxing();
				errName.SetHeapPtr(cache_REFERENCE_ERROR_NAME, (byte)RtHeapTypeKind.STRING);
				((RtInstance)_temp).SetSlot(errName, 1, _temp.Type._link_codescope, this);

				RtInstance payloadInstance = (RtInstance)_temp;


				int messagePtr;

				if (instance.ValueType == BoxType.HeapPtr)
				{
					var ins = Context.GC.Heap[instance.HeapPtr];

					if (ins.Kind == RtHeapTypeKind.CLASS)
					{
						messagePtr = Context.GC.AllocString($"Property {ns.ToDebugString(this)}{(string.IsNullOrEmpty(ns.ToDebugString(this)) ? "" : "::")}{searchName} not found on class {ins.Type.QName.ToDebugTypeName()} and there is no default value.");
					}
					else if (ins.Kind == RtHeapTypeKind.INSTANCE || ins.Kind == RtHeapTypeKind.VECTOR || ins.Kind == RtHeapTypeKind.STRING || ins.Kind == RtHeapTypeKind.ARRAY)
					{
						messagePtr = Context.GC.AllocString($"Property {ns.ToDebugString(this)}{(string.IsNullOrEmpty(ns.ToDebugString(this)) ? "" : "::")}{searchName} not found on {ins.Type.QName.ToDebugTypeName()} and there is no default value.");
					}
					else if (ins.Kind == RtHeapTypeKind.CLOSURE)
					{
						messagePtr = Context.GC.AllocString($"Property {ns.ToDebugString(this)}{(string.IsNullOrEmpty(ns.ToDebugString(this)) ? "" : "::")}{searchName} not found on builtin.as$.MethodClosure and there is no default value.");
					}
					else
					{
						messagePtr = Context.GC.AllocString($"Variable {ns.ToDebugString(this)}{(string.IsNullOrEmpty(ns.ToDebugString(this)) ? "" : "::")}{searchName} is not defined.");
					}
				}
				else
				{
					switch (instance.ValueType)
					{
						case BoxType.Number:
							messagePtr = Context.GC.AllocString($"Property {ns.ToDebugString(this)}{(string.IsNullOrEmpty(ns.ToDebugString(this)) ? "" : "::")}{searchName} not found on Number and there is no default value.");
							break;
						case BoxType.Undefined:
							messagePtr = Context.GC.AllocString($"Property {ns.ToDebugString(this)}{(string.IsNullOrEmpty(ns.ToDebugString(this)) ? "" : "::")}{searchName} not found on undefined and there is no default value.");
							break;
						case BoxType.Null:
#if DEBUG
							throw new InvalidOperationException();
#else
							Environment.FailFast("出错了，这里跑不到"); return;
#endif
							break;
						case BoxType.Boolean:
							messagePtr = Context.GC.AllocString($"Property {ns.ToDebugString(this)}{(string.IsNullOrEmpty(ns.ToDebugString(this)) ? "" : "::")}{searchName} not found on Boolean and there is no default value.");
							break;
						case BoxType.Int:
							messagePtr = Context.GC.AllocString($"Property {ns.ToDebugString(this)}{(string.IsNullOrEmpty(ns.ToDebugString(this)) ? "" : "::")}{searchName} not found on int and there is no default value.");
							break;
						case BoxType.Uint:
							messagePtr = Context.GC.AllocString($"Property {ns.ToDebugString(this)}{(string.IsNullOrEmpty(ns.ToDebugString(this)) ? "" : "::")}{searchName} not found on uint and there is no default value.");
							break;
						case BoxType.Sbyte:
							messagePtr = Context.GC.AllocString($"Property {ns.ToDebugString(this)}{(string.IsNullOrEmpty(ns.ToDebugString(this)) ? "" : "::")}{searchName} not found on sbyte and there is no default value.");
							break;
						case BoxType.Byte:
							messagePtr = Context.GC.AllocString($"Property {ns.ToDebugString(this)}{(string.IsNullOrEmpty(ns.ToDebugString(this)) ? "" : "::")}{searchName} not found on byte and there is no default value.");
							break;
						case BoxType.Short:
							messagePtr = Context.GC.AllocString($"Property {ns.ToDebugString(this)}{(string.IsNullOrEmpty(ns.ToDebugString(this)) ? "" : "::")}{searchName} not found on short and there is no default value.");
							break;
						case BoxType.UShort:
							messagePtr = Context.GC.AllocString($"Property {ns.ToDebugString(this)}{(string.IsNullOrEmpty(ns.ToDebugString(this)) ? "" : "::")}{searchName} not found on ushort and there is no default value.");
							break;
						case BoxType.Float:
							messagePtr = Context.GC.AllocString($"Property {ns.ToDebugString(this)}{(string.IsNullOrEmpty(ns.ToDebugString(this)) ? "" : "::")}{searchName} not found on float and there is no default value.");
							break;
						case BoxType.HeapPtr:
						case BoxType.Fault:
						default:
#if DEBUG
							throw new InvalidOperationException();
#else
							Environment.FailFast("出错了，这里跑不到"); return;
#endif
					}
				}


				if (messagePtr != 0)
				{
					NaNBoxing naNBoxing = new NaNBoxing();
					naNBoxing.SetHeapPtr(messagePtr, (byte)RtHeapTypeKind.STRING);
					payloadInstance.SetSlot(naNBoxing, 0, _temp.Type._link_codescope, this);
				}
				else
				{
					NaNBoxing naNBoxing = new NaNBoxing();
					naNBoxing.SetHeapPtr(cache_OUTOFMEMORY_STR, (byte)RtHeapTypeKind.STRING);
					payloadInstance.SetSlot(naNBoxing, 0, _temp.Type._link_codescope, this);
				}

				error.error.SetHeapPtr(errPtr, (byte)RtHeapTypeKind.INSTANCE);

			}



		}

		private void RaiseReferenceError_MulitNameNotFound(ref ReceiveError error, ReadOnlySpan<char> name, ASMultiname typename)
		{

			error.raised = true;
			RtHeapBase _temp;
			int errPtr = Context.GC.AllocInstance(Context.REFERENCE_ERROR.Instance, out _temp);
			if (errPtr == 0)
			{
				error.error.setFault();
			}
			else
			{
				NaNBoxing errName = new NaNBoxing();
				errName.SetHeapPtr(cache_REFERENCE_ERROR_NAME, (byte)RtHeapTypeKind.STRING);
				((RtInstance)_temp).SetSlot(errName, 1, _temp.Type._link_codescope, this);

				RtInstance payloadInstance = (RtInstance)_temp;


				int messagePtr;

				messagePtr = Context.GC.AllocString($"Property {name} not found on {typename.ToDebugTypeName()} and there is no default value.");

				if (messagePtr != 0)
				{
					NaNBoxing naNBoxing = new NaNBoxing();
					naNBoxing.SetHeapPtr(messagePtr, (byte)RtHeapTypeKind.STRING);
					payloadInstance.SetSlot(naNBoxing, 0, _temp.Type._link_codescope, this);
				}
				else
				{
					NaNBoxing naNBoxing = new NaNBoxing();
					naNBoxing.SetHeapPtr(cache_OUTOFMEMORY_STR, (byte)RtHeapTypeKind.STRING);
					payloadInstance.SetSlot(naNBoxing, 0, _temp.Type._link_codescope, this);
				}

				error.error.SetHeapPtr(errPtr, (byte)RtHeapTypeKind.INSTANCE);
			}


		}

		private void RaiseReferenceError_WriteConst(ref ReceiveError error, ASTrait trait, ASMultiname container)
		{

			error.raised = true;
			RtHeapBase _temp = null;
			int errPtr = Context.GC.AllocInstance(Context.REFERENCE_ERROR.Instance, out _temp);
			if (errPtr == 0)
			{
				error.error.setFault();
			}
			else
			{
				NaNBoxing errName = new NaNBoxing();
				errName.SetHeapPtr(cache_REFERENCE_ERROR_NAME, (byte)RtHeapTypeKind.STRING);
				((RtInstance)_temp).SetSlot(errName, 1, _temp.Type._link_codescope, this);

				RtInstance payloadInstance = (RtInstance)_temp;

				int messagePtr = Context.GC.AllocString($"Illegal write to read-only property {trait.ToDebugPropertyName()} on {container.ToDebugTypeName()}.");
				if (messagePtr != 0)
				{
					NaNBoxing naNBoxing = new NaNBoxing();
					naNBoxing.SetHeapPtr(messagePtr, (byte)RtHeapTypeKind.STRING);
					payloadInstance.SetSlot(naNBoxing, 0, _temp.Type._link_codescope, this);
				}
				else
				{
					NaNBoxing naNBoxing = new NaNBoxing();
					naNBoxing.SetHeapPtr(cache_OUTOFMEMORY_STR, (byte)RtHeapTypeKind.STRING);
					payloadInstance.SetSlot(naNBoxing, 0, _temp.Type._link_codescope, this);
				}

				error.error.SetHeapPtr(errPtr, (byte)RtHeapTypeKind.INSTANCE);

			}


		}

		private void RaiseReferenceError_WriteToMethod(ref ReceiveError error, ASMethodBody body, ASMultiname container)
		{
			//$"Cannot assign to a method { cache.Type.QName.Name } on { ((RtPayloadClosure)cache)._ref_as_type.QName.Name }."
			error.raised = true;
			RtHeapBase _temp = null;
			int errPtr = Context.GC.AllocInstance(Context.REFERENCE_ERROR.Instance, out _temp);
			if (errPtr == 0)
			{
				error.error.setFault();
			}
			else
			{
				NaNBoxing errName = new NaNBoxing();
				errName.SetHeapPtr(cache_REFERENCE_ERROR_NAME, (byte)RtHeapTypeKind.STRING);
				((RtInstance)_temp).SetSlot(errName, 1, _temp.Type._link_codescope, this);

				RtInstance payloadInstance = (RtInstance)_temp;

				int messagePtr = Context.GC.AllocString($"Cannot assign to a method {body.QName.Name} on {container.Name}.");
				if (messagePtr != 0)
				{
					NaNBoxing naNBoxing = new NaNBoxing();
					naNBoxing.SetHeapPtr(messagePtr, (byte)RtHeapTypeKind.STRING);
					payloadInstance.SetSlot(naNBoxing, 0, _temp.Type._link_codescope, this);
				}
				else
				{
					NaNBoxing naNBoxing = new NaNBoxing();
					naNBoxing.SetHeapPtr(cache_OUTOFMEMORY_STR, (byte)RtHeapTypeKind.STRING);
					payloadInstance.SetSlot(naNBoxing, 0, _temp.Type._link_codescope, this);
				}

				error.error.SetHeapPtr(errPtr, (byte)RtHeapTypeKind.INSTANCE);

			}

		}
		private void RaiseReferenceError_WriteToReadonlyProperty(ref ReceiveError error, ASMethodBody body, ASMultiname container)
		{
			//$"Illegal write to read-only property B on Main."
			error.raised = true;
			RtHeapBase _temp = null;
			int errPtr = Context.GC.AllocInstance(Context.REFERENCE_ERROR.Instance, out _temp);
			if (errPtr == 0)
			{
				error.error.setFault();
			}
			else
			{
				NaNBoxing errName = new NaNBoxing();
				errName.SetHeapPtr(cache_REFERENCE_ERROR_NAME, (byte)RtHeapTypeKind.STRING);
				((RtInstance)_temp).SetSlot(errName, 1, _temp.Type._link_codescope, this);

				RtInstance payloadInstance = (RtInstance)_temp;

				int messagePtr = Context.GC.AllocString($"Illegal write to read-only property {body.QName.Name} on {container.Name}.");
				if (messagePtr != 0)
				{
					NaNBoxing naNBoxing = new NaNBoxing();
					naNBoxing.SetHeapPtr(messagePtr, (byte)RtHeapTypeKind.STRING);
					payloadInstance.SetSlot(naNBoxing, 0, _temp.Type._link_codescope, this);
				}
				else
				{
					NaNBoxing naNBoxing = new NaNBoxing();
					naNBoxing.SetHeapPtr(cache_OUTOFMEMORY_STR, (byte)RtHeapTypeKind.STRING);
					payloadInstance.SetSlot(naNBoxing, 0, _temp.Type._link_codescope, this);
				}

				error.error.SetHeapPtr(errPtr, (byte)RtHeapTypeKind.INSTANCE);

			}

		}

		private void RaiseReferenceError_CanNotCreateProperty(ref ReceiveError error, ASNamespace ns, ReadOnlySpan<char> searchName, ASMultiname qName)
		{

			error.raised = true;


			RtHeapBase _temp = null;
			int errPtr = Context.GC.AllocInstance(Context.REFERENCE_ERROR.Instance, out _temp);
			if (errPtr == 0)
			{
				error.error.setFault();
			}
			else
			{

				NaNBoxing errName = new NaNBoxing();
				errName.SetHeapPtr(cache_ERROR_NAME, (byte)RtHeapTypeKind.STRING);
				((RtInstance)_temp).SetSlot(errName, 1, Context.REFERENCE_ERROR.Instance._link_codescope, this);

				RtHeapBase error_instance = _temp;
				RtInstance payloadInstance = (RtInstance)error_instance;

				var debugNs = () =>
				{
					if (ns == null)
						return null;

					if (string.IsNullOrEmpty(ns.def_uri))
					{
						return ns.Name;
					}
					else
					{
						return ns.def_uri;
					}

				};

				string nsname = debugNs();

				int messagePtr = Context.GC.AllocString($"Cannot create property {(nsname == null ? "" : nsname + "::")}{searchName} on {qName.ToDebugTypeName()}.");
				if (messagePtr != 0)
				{
					NaNBoxing naNBoxing = new NaNBoxing();
					naNBoxing.SetHeapPtr(messagePtr, (byte)RtHeapTypeKind.STRING);
					payloadInstance.SetSlot(naNBoxing, 0, error_instance.Type._link_codescope, this);
				}
				else
				{
					NaNBoxing naNBoxing = new NaNBoxing();
					naNBoxing.SetHeapPtr(cache_OUTOFMEMORY_STR, (byte)RtHeapTypeKind.STRING);
					payloadInstance.SetSlot(naNBoxing, 0, _temp.Type._link_codescope, this);
				}

				error.error.SetHeapPtr(errPtr, (byte)RtHeapTypeKind.INSTANCE);
			}


		}

		private void RaiseReferenceError_CanNotDeleteProperty(ref ReceiveError error, NaNBoxing refInstance)
		{
			error.raised = true;
			RtHeapBase _temp = null;
			int errPtr = Context.GC.AllocInstance(Context.REFERENCE_ERROR.Instance, out _temp);
			if (errPtr == 0)
			{
				error.error.setFault();
			}
			else
			{
				NaNBoxing errName = new NaNBoxing();
				errName.SetHeapPtr(cache_REFERENCE_ERROR_NAME, (byte)RtHeapTypeKind.STRING);
				((RtInstance)_temp).SetSlot(errName, 1, _temp.Type._link_codescope, this);

				RtInstance payloadInstance = (RtInstance)_temp;

				int messagePtr = Context.GC.AllocString($"Cannot delete property aaa on {refInstance.ValueType}.");
				if (messagePtr != 0)
				{
					NaNBoxing naNBoxing = new NaNBoxing();
					naNBoxing.SetHeapPtr(messagePtr, (byte)RtHeapTypeKind.STRING);
					payloadInstance.SetSlot(naNBoxing, 0, _temp.Type._link_codescope, this);
				}
				else
				{
					NaNBoxing naNBoxing = new NaNBoxing();
					naNBoxing.SetHeapPtr(cache_OUTOFMEMORY_STR, (byte)RtHeapTypeKind.STRING);
					payloadInstance.SetSlot(naNBoxing, 0, _temp.Type._link_codescope, this);
				}

				error.error.SetHeapPtr(errPtr, (byte)RtHeapTypeKind.INSTANCE);

			}

		}

		//Variable int1 is not defined.
		internal void RaiseReferenceError_TypeNotFound(ref ReceiveError error, ReadOnlySpan<char> name)
		{
			error.raised = true;
			RtHeapBase _temp = null;
			int errPtr = Context.GC.AllocInstance(Context.REFERENCE_ERROR.Instance, out _temp);
			if (errPtr == 0)
			{
				error.error.setFault();
			}
			else
			{
				NaNBoxing errName = new NaNBoxing();
				errName.SetHeapPtr(cache_REFERENCE_ERROR_NAME, (byte)RtHeapTypeKind.STRING);
				((RtInstance)_temp).SetSlot(errName, 1, _temp.Type._link_codescope, this);

				RtInstance payloadInstance = (RtInstance)_temp;

				int messagePtr = Context.GC.AllocString($"Variable {name} is not defined.");
				if (messagePtr != 0)
				{
					NaNBoxing naNBoxing = new NaNBoxing();
					naNBoxing.SetHeapPtr(messagePtr, (byte)RtHeapTypeKind.STRING);
					payloadInstance.SetSlot(naNBoxing, 0, _temp.Type._link_codescope, this);
				}
				else
				{
					NaNBoxing naNBoxing = new NaNBoxing();
					naNBoxing.SetHeapPtr(cache_OUTOFMEMORY_STR, (byte)RtHeapTypeKind.STRING);
					payloadInstance.SetSlot(naNBoxing, 0, _temp.Type._link_codescope, this);
				}

				error.error.SetHeapPtr(errPtr, (byte)RtHeapTypeKind.INSTANCE);

			}

		}



		int cache_ARGEMENT_ERROR_NAME;
		private void RaiseArgumentErrorCountMisMatch(ref ReceiveError error, ASMethod method, int expected, int got)
		{
			error.raised = true;
			RtHeapBase _temp = null;
			int errPtr = Context.GC.AllocInstance(Context.ARGEMENT_ERROR.Instance, out _temp);
			if (errPtr == 0)
			{
				error.error.setFault();
			}
			else
			{
				NaNBoxing errName = new NaNBoxing();
				errName.SetHeapPtr(cache_ARGEMENT_ERROR_NAME, (byte)RtHeapTypeKind.STRING);
				((RtInstance)_temp).SetSlot(errName, 1, _temp.Type._link_codescope, this);

				RtInstance payloadInstance = (RtInstance)_temp;

				int messagePtr = Context.GC.AllocString(
					method != null ?
					$"Argument count mismatch on {(method.__ismethod ? method.Container.QName.Name + "/" + method.Body.QName.Name : "Function/" + method.Name)}(). Expected {expected}, got {got}."
					:
					$"Incorrect number of arguments.  Expected no more than 1"
					);
				if (messagePtr != 0)
				{
					NaNBoxing naNBoxing = new NaNBoxing();
					naNBoxing.SetHeapPtr(messagePtr, (byte)RtHeapTypeKind.STRING);
					payloadInstance.SetSlot(naNBoxing, 0, _temp.Type._link_codescope, this);
				}
				else
				{
					NaNBoxing naNBoxing = new NaNBoxing();
					naNBoxing.SetHeapPtr(cache_OUTOFMEMORY_STR, (byte)RtHeapTypeKind.STRING);
					payloadInstance.SetSlot(naNBoxing, 0, _temp.Type._link_codescope, this);
				}

				error.error.SetHeapPtr(errPtr, (byte)RtHeapTypeKind.INSTANCE);
			}
		}




		internal void RaiseArgumentNotNull(ref ReceiveError error,ReadOnlySpan<char> name)
		{
			//Argument name cannot be null.

			error.raised = true;
			RtHeapBase _temp = null;
			int errPtr = Context.GC.AllocInstance(Context.ARGEMENT_ERROR.Instance, out _temp);
			if (errPtr == 0)
			{
				error.error.setFault();
			}
			else
			{
				NaNBoxing errName = new NaNBoxing();
				errName.SetHeapPtr(cache_ARGEMENT_ERROR_NAME, (byte)RtHeapTypeKind.STRING);
				((RtInstance)_temp).SetSlot(errName, 1, _temp.Type._link_codescope, this);

				RtInstance payloadInstance = (RtInstance)_temp;

				int messagePtr = Context.GC.AllocString(
					$"Argument {name} cannot be null."
					);
				if (messagePtr != 0)
				{
					NaNBoxing naNBoxing = new NaNBoxing();
					naNBoxing.SetHeapPtr(messagePtr, (byte)RtHeapTypeKind.STRING);
					payloadInstance.SetSlot(naNBoxing, 0, _temp.Type._link_codescope, this);
				}
				else
				{
					NaNBoxing naNBoxing = new NaNBoxing();
					naNBoxing.SetHeapPtr(cache_OUTOFMEMORY_STR, (byte)RtHeapTypeKind.STRING);
					payloadInstance.SetSlot(naNBoxing, 0, _temp.Type._link_codescope, this);
				}

				error.error.SetHeapPtr(errPtr, (byte)RtHeapTypeKind.INSTANCE);
			}
		}








		internal int cache_RANGE_ERROR_NAME;
		internal void RaiseRangeError(ref ReceiveError error, ReadOnlySpan<char> index, long maxrange)
		{
			error.raised = true;
			RtHeapBase _temp = null;
			int errPtr = Context.GC.AllocInstance(Context.RANGE_ERROR.Instance, out _temp);
			if (errPtr == 0)
			{
				error.error.setFault();
			}
			else
			{
				NaNBoxing errName = new NaNBoxing();
				errName.SetHeapPtr(cache_RANGE_ERROR_NAME, (byte)RtHeapTypeKind.STRING);
				((RtInstance)_temp).SetSlot(errName, 1, _temp.Type._link_codescope, this);


				RtInstance payloadInstance = (RtInstance)_temp;
				int messagePtr = Context.GC.AllocString(

					$"The index {index} is out of range {maxrange}."

					);
				if (messagePtr != 0)
				{
					NaNBoxing naNBoxing = new NaNBoxing();
					naNBoxing.SetHeapPtr(messagePtr, (byte)RtHeapTypeKind.STRING);
					payloadInstance.SetSlot(naNBoxing, 0, _temp.Type._link_codescope, this);
				}
				else
				{
					NaNBoxing naNBoxing = new NaNBoxing();
					naNBoxing.SetHeapPtr(cache_OUTOFMEMORY_STR, (byte)RtHeapTypeKind.STRING);
					payloadInstance.SetSlot(naNBoxing, 0, _temp.Type._link_codescope, this);
				}

				error.error.SetHeapPtr(errPtr, (byte)RtHeapTypeKind.INSTANCE);
			}


		}
		internal void RaiseRangeError(ref ReceiveError error, string index)
		{
			error.raised = true;
			RtHeapBase _temp = null;
			int errPtr = Context.GC.AllocInstance(Context.RANGE_ERROR.Instance, out _temp);
			if (errPtr == 0)
			{
				error.error.setFault();
			}
			else
			{
				NaNBoxing errName = new NaNBoxing();
				errName.SetHeapPtr(cache_RANGE_ERROR_NAME, (byte)RtHeapTypeKind.STRING);
				((RtInstance)_temp).SetSlot(errName, 1, _temp.Type._link_codescope, this);


				RtInstance payloadInstance = (RtInstance)_temp;
				int messagePtr = Context.GC.AllocString(
					index
					);
				if (messagePtr != 0)
				{
					NaNBoxing naNBoxing = new NaNBoxing();
					naNBoxing.SetHeapPtr(messagePtr, (byte)RtHeapTypeKind.STRING);
					payloadInstance.SetSlot(naNBoxing, 0, _temp.Type._link_codescope, this);
				}
				else
				{
					NaNBoxing naNBoxing = new NaNBoxing();
					naNBoxing.SetHeapPtr(cache_OUTOFMEMORY_STR, (byte)RtHeapTypeKind.STRING);
					payloadInstance.SetSlot(naNBoxing, 0, _temp.Type._link_codescope, this);
				}

				error.error.SetHeapPtr(errPtr, (byte)RtHeapTypeKind.INSTANCE);
			}


		}

	}
}
