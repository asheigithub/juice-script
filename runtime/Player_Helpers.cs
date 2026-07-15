using juicescript.ABC;
using juicescript.ABC.INS;
using juicescript.ABC.Locaters;
using juicescript.runtime.buildin;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static juicescript.NaNBoxing;
using static juicescript.runtime.Player;

namespace juicescript.runtime
{
	public partial class Player
	{

		private unsafe void Ld_class(int dst_index,byte** PC, Span<NaNBoxing> constants,Span<NaNBoxing> stackslots,ref ReceiveError error)
		{
			StackLocater stackLocater;
			stackLocater.index = dst_index;

			int classid_index = 0;
			LoadInt32(&classid_index, PC);

			var boxing = constants[classid_index];


			Debug.Assert(boxing.ValueType == NaNBoxing.BoxType.Uint);


			//InitASClass((ASClass)instance.Type, ref error);
			var @class = Context.link_const_class[(int)boxing.UIntValue];
			InitScript((ASScript)@class._link_codescope.Parent.Container, ref error);
			if (error.raised)
			{
				goto flag_handle_error;
			}
			if (@class.__instance_index__ == 0)
			{
				//在@class就在当前正在初始化的script中，却又没有初始化到的情况。
				InitASClass(@class, ref error);
				if (error.raised)
				{
					goto flag_handle_error;
				}
			}

			stackslots[stackLocater.index].SetHeapPtr(@class.__instance_index__, (byte)RtHeapTypeKind.CLASS, (byte)HeapKindFlag.NONE);

		flag_handle_error:
			;

		}










		private unsafe void POSITIVE( int dst_index,byte** PC, RtHeapBase methodscope, Span<NaNBoxing> stackslots,int stackStPos,int scope_ptr , ref ReceiveError error)
		{
			StackLocater dst;
			StackLocater src;

			dst.index = dst_index;
			LoadStackLocater(&src, PC);

			var v = stackslots[src.index];// LoadValue(stackslots[src.index], ref error, ref stackslots, stackStPos);
										  //if (error.raised)
										  //{
										  //    goto flag_handle_error;
										  //}

			{
				//操作符重载
				ASClass t1;
				int op_override_id1 = GetOpOverrideTypeId(v, out t1);

				if (op_override_id1 != -1)
				{
					var negmethod = overrideOperatorMethods[(int)OverrideOperator.positive][op_override_id1][op_override_id1];
					if (negmethod != null)
					{
#if FORCOMPILER
						if (IsComputeConstExpr)
						{
							throw new EvalConstException();
						}
#endif

						var @class = (ASClass)negmethod.Container;
						Debug.Assert(@class.__instance_index__ != -1);

						if (Context.StackPosition + 1 >= Context.STACK_LENGTH)
						{
							RaiseStackOverflow(ref error);
							goto flag_handle_error;
						}

						Span<NaNBoxing> slots = Context.StackSlots.AsSpan(Context.StackPosition, 1);
						slots[0] = v;

						Context.StackPosition += 1;

						NaNBoxing cls = default; cls.SetHeapPtr(@class.__instance_index__, (byte)RtHeapTypeKind.CLASS, (byte)HeapKindFlag.NONE);

						StackLocater args = default; args.index = 0;
						unsafe
						{
							RunMethod(negmethod, cls, scope_ptr, @class, 1, (byte*)&args, slots, ref error, stackStPos + dst.index);
						}
						Context.StackPosition -= 1;
						if (error.raised)
						{
							goto flag_handle_error;
						}
						return;
					}
				}

			}

			v = ToPrimitive(ref error, v, HINT.h_number, scope_ptr, dst, dst, stackslots, stackStPos, ((RtMethodScope)methodscope).ThisPtr);
			if (error.raised)
			{
				goto flag_handle_error;
			}

			switch (v.ValueType)
			{
				case NaNBoxing.BoxType.Undefined:
					stackslots[dst.index].SetNumber(double.NaN);
					break;
				case NaNBoxing.BoxType.Null:
					stackslots[dst.index].SetNumber(0.0);
					break;
				case NaNBoxing.BoxType.Boolean:
					if (v.Boolean)
					{
						stackslots[dst.index].SetInt(1);
					}
					else
					{
						stackslots[dst.index].SetInt(0);
					}
					break;
				case NaNBoxing.BoxType.Number:
				case NaNBoxing.BoxType.Int:
				case NaNBoxing.BoxType.Uint:
				case NaNBoxing.BoxType.Sbyte:
				case NaNBoxing.BoxType.Byte:
				case NaNBoxing.BoxType.Short:
				case NaNBoxing.BoxType.UShort:
				case NaNBoxing.BoxType.Float:
					stackslots[dst.index] = v;
					break;
				case NaNBoxing.BoxType.HeapPtr:
					ConvertValueType(ref error, v, TypeKind.Number, Context.NUMBER, ref stackslots[dst.index]); //这里肯定是字符串
#if DEBUG
					if (error.raised)
					{
						throw new InvalidOperationException();
					}
#endif
					break;
#if DEBUG
				case NaNBoxing.BoxType.Fault:
				default:
					throw new InvalidOperationException();
#endif
			}


		flag_handle_error:
			;


		}



		private unsafe void NEG( int dst_index,byte** PC, RtHeapBase methodscope,Span<NaNBoxing> stackslots, int stackStPos, int scope_ptr, ref ReceiveError error)
		{
			StackLocater dst;
			StackLocater src;

			//LoadStackLocater(&dst, &PC);
			dst.index = dst_index;
			LoadStackLocater(&src, PC);


			var v = stackslots[src.index];// LoadValue(stackslots[src.index], ref error, ref stackslots, stackStPos);
										  //if (error.raised)
										  //{
										  //    goto flag_handle_error;
										  //}

			{
				//操作符重载
				ASClass t1;
				int op_override_id1 = GetOpOverrideTypeId(v, out t1);

				if (op_override_id1 != -1)
				{
					var negmethod = overrideOperatorMethods[(int)OverrideOperator.neg][op_override_id1][op_override_id1];
					if (negmethod != null)
					{
#if FORCOMPILER
						if (IsComputeConstExpr)
						{
							throw new EvalConstException();
						}
#endif

						var @class = (ASClass)negmethod.Container;
						Debug.Assert(@class.__instance_index__ != -1);

						if (Context.StackPosition + 1 >= Context.STACK_LENGTH)
						{
							RaiseStackOverflow(ref error);
							goto flag_handle_error;
						}

						Span<NaNBoxing> slots = Context.StackSlots.AsSpan(Context.StackPosition, 1);
						slots[0] = v;

						Context.StackPosition += 1;

						NaNBoxing cls = default; cls.SetHeapPtr(@class.__instance_index__, (byte)RtHeapTypeKind.CLASS, (byte)HeapKindFlag.NONE);

						StackLocater args = default; args.index = 0;
						unsafe
						{
							RunMethod(negmethod, cls, scope_ptr, @class, 1, (byte*)&args, slots, ref error, stackStPos + dst.index);
						}
						Context.StackPosition -= 1;
						if (error.raised)
						{
							goto flag_handle_error;
						}
						return;
					}
				}

			}

			v = ToPrimitive(ref error, v, HINT.h_number, scope_ptr, dst, dst, stackslots, stackStPos, ((RtMethodScope)methodscope).ThisPtr);
			if (error.raised)
			{
				goto flag_handle_error;
			}

			switch (v.ValueType)
			{
				case NaNBoxing.BoxType.Number:
					stackslots[dst.index].SetNumber(-v.Number);
					break;
				case NaNBoxing.BoxType.Undefined:
					stackslots[dst.index].SetNumber(double.NaN);
					break;
				case NaNBoxing.BoxType.Null:
					stackslots[dst.index].SetNumber(0.0);
					break;
				case NaNBoxing.BoxType.Boolean:
					if (v.Boolean)
					{
						stackslots[dst.index].SetInt(-1);
					}
					else
					{
						stackslots[dst.index].SetInt(0);
					}
					break;
				case NaNBoxing.BoxType.Int:
					if (v.IntValue == 0)
					{
						//有这种代码 : 1 / (-0) , 所以结果需要区分正负无穷。。。
						stackslots[dst.index].SetNumber(-0.0);
					}
					else
					{
						stackslots[dst.index].SetInt(-v.IntValue);
					}
					break;
				case NaNBoxing.BoxType.Uint:
					stackslots[dst.index].SetNumber(-(double)v.UIntValue);
					break;
				case NaNBoxing.BoxType.Sbyte:
					if (v.SByteValue == 0)
					{
						stackslots[dst.index].SetNumber(-0.0);
					}
					else
					{
						stackslots[dst.index].SetInt(-(int)v.SByteValue);
					}
					break;
				case NaNBoxing.BoxType.Byte:
					if (v.ByteValue == 0)
					{
						stackslots[dst.index].SetNumber(-0.0);
					}
					else
					{
						stackslots[dst.index].SetInt(-(int)v.ByteValue);
					}
					break;
				case NaNBoxing.BoxType.Short:
					if (v.ShortValue == 0)
					{
						stackslots[dst.index].SetNumber(-0.0);
					}
					else
					{
						stackslots[dst.index].SetInt(-(int)v.ShortValue);
					}
					break;
				case NaNBoxing.BoxType.UShort:
					if (v.UShortValue == 0)
					{
						stackslots[dst.index].SetNumber(-0.0);
					}
					else
					{
						stackslots[dst.index].SetInt(-(int)v.UShortValue);
					}
					break;
				case NaNBoxing.BoxType.Float:
					stackslots[dst.index].SetFloat(-v.FloatValue);
					break;
				case NaNBoxing.BoxType.HeapPtr:
					ConvertValueType(ref error, v, TypeKind.Number, Context.NUMBER, ref stackslots[dst.index]); //这里肯定是字符串

					Debug.Assert(!error.raised);

					stackslots[dst.index].SetNumber(-stackslots[dst.index].Number);

					break;
#if DEBUG
				case NaNBoxing.BoxType.Fault:
				default:
					throw new InvalidOperationException();
#endif
			}


		flag_handle_error:
			;

		}


		private unsafe void Exec_AddSlow(StackLocater dst,
		
		NaNBoxing n1,NaNBoxing n2,int scope_ptr,int stackStPos,Span<NaNBoxing> stackslots,NaNBoxing thisPtr,
		ref ReceiveError error
		)
		{
			ASClass t1; ASClass t2;
			//操作符重载
			int op_override_id1 = GetOpOverrideTypeId(n1, out t1);
			int op_override_id2 = GetOpOverrideTypeId(n2, out t2);
			if (op_override_id1 != -1 && op_override_id2 != -1)
			{
				var method = overrideOperatorMethods[(int)OverrideOperator.add][op_override_id1][op_override_id2];
				if (method != null)
				{
#if FORCOMPILER
					if (IsComputeConstExpr)
					{
						throw new EvalConstException();
					}
#endif

					if (t1 != null)
					{
						InitScript((ASScript)t1._link_codescope.Parent.Container, ref error);
						if (error.raised)
						{
							return;
						}
					}
					if (t2 != null)
					{
						InitScript((ASScript)t2._link_codescope.Parent.Container, ref error);
						if (error.raised)
						{
							return;
						}
					}


					var @class = (ASClass)method.Container;

					if (Context.StackPosition + 2 >= Context.STACK_LENGTH)
					{
						RaiseStackOverflow(ref error);
						return;
					}

					Span<NaNBoxing> runmethd_slots = Context.StackSlots.AsSpan(Context.StackPosition, 2);
					runmethd_slots[0] = n1;
					runmethd_slots[1] = n2;

					Context.StackPosition += 2;

					NaNBoxing cls = default; cls.SetHeapPtr(@class.__instance_index__, (byte)RtHeapTypeKind.CLASS, (byte)HeapKindFlag.NONE);
					unsafe
					{
						StackLocater* args = stackalloc StackLocater[2];
						args->index = 0;
						(args + 1)->index = 1;
						RunMethod(method, cls, scope_ptr, @class, 2, (byte*)args, runmethd_slots, ref error, stackStPos + dst.index);
					}
					Context.StackPosition -= 2;

					return;
				}
			}

			HINT hint; //这里还是按AIR的实现来，如果有字符串则用 string
			if ((n1.ValueType == BoxType.HeapPtr && n1.HeapKind == (byte)RtHeapTypeKind.STRING)
				||
				(n2.ValueType == BoxType.HeapPtr && n2.HeapKind == (byte)RtHeapTypeKind.STRING)
				||
				n1.ValueType == BoxType.LocalString
				||
				n2.ValueType == BoxType.LocalString
				)
			{
				hint = HINT.h_string;
			}
			else
			{
				hint = HINT.h_number;
			}


			//不能修改v1,v2的输入值
			if (Context.StackPosition + 3 >= Context.STACK_LENGTH)
			{
				RaiseStackOverflow(ref error);
				return;
			}

			int basePos = Context.StackPosition;
			Span<NaNBoxing> slots = Context.StackSlots.AsSpan(Context.StackPosition, 3);
			slots.Clear();
			Context.StackPosition += 3;
			StackLocater conv_result1 = default;conv_result1.index = 0;
			StackLocater conv_result2 = default;conv_result2.index = 1;
			StackLocater tmpslot = default; tmpslot.index = 2;

			n1 = ToPrimitive(ref error, n1, hint, scope_ptr, conv_result1, tmpslot, slots, Context.StackPosition, thisPtr);
			if (error.raised)
			{
				Context.StackPosition = basePos;
				return;
			}

			n2 = ToPrimitive(ref error, n2, hint, scope_ptr, conv_result2, tmpslot, slots, Context.StackPosition, thisPtr);
			if (error.raised)
			{
				Context.StackPosition = basePos;
				return;
			}

			switch (n1.ValueType)
			{
				case NaNBoxing.BoxType.Number:
					{
						switch (n2.ValueType)
						{
							case BoxType.Number:
							case BoxType.Undefined:
							case BoxType.Null:
							case BoxType.Boolean:
							case BoxType.Int:
							case BoxType.Uint:
							case BoxType.Sbyte:
							case BoxType.Byte:
							case BoxType.Short:
							case BoxType.UShort:
							case BoxType.Float:
								stackslots[dst.index].SetNumber(n1.Number + Extensions.GetDoubleValue(n2));
								break;
							case BoxType.HeapPtr:
								goto lbL_primtive_add_heap;
							case BoxType.LocalString:
								{
									// Use efficient char-based concatenation to avoid string allocation
									Span<char> chars2 = stackalloc char[16];
									int charCount2 = n2.GetLocalStringChars(chars2);
									if (charCount2 > 0)
									{
										var str2 = chars2.Slice(0, charCount2);

										Span<char> buffers = stackalloc char[128];
										var concatenated = $"{Extensions.GetPrimitiveValueToString(this, n1, buffers)}{str2}";

										// 使用安全的字符串创建方法
										if (!TryCreateStringValue(concatenated, out stackslots[dst.index], ref error))
										{
											Context.StackPosition = basePos;
											return; // 错误已经在TryCreateStringValue中处理
										}
									}
									else
									{
										// Empty LocalString, just convert n1 to string
										Span<char> buffers = stackalloc char[128];
										var concatenated = Extensions.GetPrimitiveValueToString(this, n1, buffers);
										if (!TryCreateStringValue(concatenated, out stackslots[dst.index], ref error))
										{
											Context.StackPosition = basePos;
											return;
										}
									}
								}
								break;
#if DEBUG
							case BoxType.Fault:
							default:
								throw new InvalidOperationException();
#endif
						}
					}
					break;
				case NaNBoxing.BoxType.Undefined:
					{
						switch (n2.ValueType)
						{
							case BoxType.Number:
							case BoxType.Undefined:
							case BoxType.Null:
							case BoxType.Boolean:
							case BoxType.Int:
							case BoxType.Uint:
							case BoxType.Sbyte:
							case BoxType.Byte:
							case BoxType.Short:
							case BoxType.UShort:
							case BoxType.Float:
								stackslots[dst.index].SetNumber(double.NaN + Extensions.GetDoubleValue(n2));
								break;
							case BoxType.HeapPtr:
								goto lbL_primtive_add_heap;
							case BoxType.LocalString:
								{
									// Use efficient char-based concatenation to avoid string allocation
									Span<char> chars2 = stackalloc char[16];
									int charCount2 = n2.GetLocalStringChars(chars2);
									if (charCount2 > 0)
									{
										var str2 = chars2.Slice(0, charCount2);
										Span<char> buffers = stackalloc char[128];
										string concatenated = $"{Extensions.GetPrimitiveValueToString(this, n1, buffers)}{str2}";

										// 使用安全的字符串创建方法
										if (!TryCreateStringValue(concatenated, out stackslots[dst.index], ref error))
										{
											Context.StackPosition = basePos;
											return; // 错误已经在TryCreateStringValue中处理
										}
									}
									else
									{
										Span<char> buffers = stackalloc char[128];
										// Empty LocalString, just convert n1 to string
										var concatenated = Extensions.GetPrimitiveValueToString(this, n1, buffers);
										if (!TryCreateStringValue(concatenated, out stackslots[dst.index], ref error))
										{
											Context.StackPosition = basePos;
											return;
										}
									}
								}
								break;
#if DEBUG
							case BoxType.Fault:
							default:
								throw new InvalidOperationException();
#endif
						}
					}
					break;
				case NaNBoxing.BoxType.Null:
					{
						switch (n2.ValueType)
						{
							case BoxType.Number:
							case BoxType.Undefined:
							case BoxType.Null:
							case BoxType.Boolean:
							case BoxType.Int:
							case BoxType.Uint:
							case BoxType.Sbyte:
							case BoxType.Byte:
							case BoxType.Short:
							case BoxType.UShort:
							case BoxType.Float:
								stackslots[dst.index].SetNumber(0.0 + Extensions.GetDoubleValue(n2));
								break;
							case BoxType.HeapPtr:
								goto lbL_primtive_add_heap;
#if DEBUG
							case BoxType.Fault:
							default:
								throw new InvalidOperationException();
#endif
						}
					}
					break;
				case NaNBoxing.BoxType.Boolean:
					{
						switch (n2.ValueType)
						{
							case BoxType.Number:
								stackslots[dst.index].SetNumber((n1.Boolean ? 1 : 0) + Extensions.GetDoubleValue(n2));
								break;
							case BoxType.Undefined:
								stackslots[dst.index].SetNumber(double.NaN);
								break;
							case BoxType.Null:
								stackslots[dst.index].SetNumber((n1.Boolean ? 1 : 0) + 0.0);
								break;
							case BoxType.Boolean:
								stackslots[dst.index].SetInt((n1.Boolean ? 1 : 0) + (n2.Boolean ? 1 : 0));
								break;
							case BoxType.Int:
								stackslots[dst.index].SetInt((n1.Boolean ? 1 : 0) + n2.IntValue);
								break;
							case BoxType.Uint:
								stackslots[dst.index].SetNumber((n1.Boolean ? 1U : 0U) + n2.UIntValue);
								break;
							case BoxType.Sbyte:
							case BoxType.Byte:
							case BoxType.Short:
							case BoxType.UShort:
								stackslots[dst.index].SetInt((n1.Boolean ? 1 : 0) + Extensions.GetIntValue(n2));
								break;
							case BoxType.Float:
								stackslots[dst.index].SetFloat((n1.Boolean ? 1 : 0) + n2.FloatValue);
								break;
							case BoxType.HeapPtr:
								goto lbL_primtive_add_heap;
#if DEBUG
							case BoxType.Fault:
							default:
								throw new InvalidOperationException();
#endif
						}
					}
					break;
				case NaNBoxing.BoxType.Int:
					{
						switch (n2.ValueType)
						{
							case BoxType.Undefined:
								stackslots[dst.index].SetNumber(double.NaN);
								break;
							case BoxType.Number:
							case BoxType.Null:
								stackslots[dst.index].SetNumber(n1.IntValue + Extensions.GetDoubleValue(n2));
								break;
							case BoxType.Boolean:
								stackslots[dst.index].SetInt(n1.IntValue + (n2.Boolean ? 1 : 0));
								break;
							case BoxType.Uint:
								stackslots[dst.index].SetNumber((double)n1.IntValue + n2.UIntValue);
								break;
							case BoxType.Float:
								stackslots[dst.index].SetFloat(n1.IntValue + n2.FloatValue);
								break;
							case BoxType.Int:
							case BoxType.Sbyte:
							case BoxType.Byte:
							case BoxType.Short:
							case BoxType.UShort:
								stackslots[dst.index].SetInt(n1.IntValue + Extensions.GetIntValue(n2));
								break;
							case BoxType.HeapPtr:
								goto lbL_primtive_add_heap;
#if DEBUG
							case BoxType.Fault:
							default:
								throw new InvalidOperationException();
#endif
						}
					}
					break;
				case NaNBoxing.BoxType.Uint:
					{
						switch (n2.ValueType)
						{
							case BoxType.Undefined:
								stackslots[dst.index].SetNumber(double.NaN);
								break;
							case BoxType.Number:
							case BoxType.Null:
								stackslots[dst.index].SetNumber(n1.UIntValue + Extensions.GetDoubleValue(n2));
								break;
							case BoxType.Boolean:
								stackslots[dst.index].SetNumber(n1.UIntValue + (n2.Boolean ? 1U : 0U));
								break;
							case BoxType.Int:
								stackslots[dst.index].SetNumber((double)n1.UIntValue + n2.IntValue);
								break;
							case BoxType.Uint:
								stackslots[dst.index].SetUInt(n1.UIntValue + n2.UIntValue);
								break;
							case BoxType.Sbyte:
								stackslots[dst.index].SetNumber((double)n1.UIntValue + n2.SByteValue);
								break;
							case BoxType.Byte:
								stackslots[dst.index].SetUInt(n1.UIntValue + n2.ByteValue);
								break;
							case BoxType.Short:
								stackslots[dst.index].SetNumber((double)n1.UIntValue + n2.ShortValue);
								break;
							case BoxType.UShort:
								stackslots[dst.index].SetUInt(n1.UIntValue + n2.UShortValue);
								break;
							case BoxType.Float:
								stackslots[dst.index].SetFloat((float)n1.UIntValue + n2.FloatValue);
								break;
							case BoxType.HeapPtr:
								goto lbL_primtive_add_heap;
#if DEBUG
							case BoxType.Fault:
							default:
								throw new InvalidOperationException();
#endif
						}
					}
					break;
				case NaNBoxing.BoxType.Sbyte:
				case NaNBoxing.BoxType.Byte:
				case NaNBoxing.BoxType.Short:
				case NaNBoxing.BoxType.UShort:
					switch (n2.ValueType)
					{
						case BoxType.Undefined:
							stackslots[dst.index].SetNumber(double.NaN);
							break;
						case BoxType.Number:
						case BoxType.Null:
							stackslots[dst.index].SetNumber(Extensions.GetDoubleValue(n1) + Extensions.GetDoubleValue(n2));
							break;
						case BoxType.Uint:
							stackslots[dst.index].SetNumber(Extensions.GetDoubleValue(n1) + n2.UIntValue);
							break;
						case BoxType.Sbyte:
						case BoxType.Byte:
						case BoxType.Short:
						case BoxType.UShort:
						case BoxType.Boolean:
						case BoxType.Int:
							stackslots[dst.index].SetInt(Extensions.GetIntValue(n1) + Extensions.GetIntValue(n2));
							break;
						case BoxType.Float:
							stackslots[dst.index].SetFloat(Extensions.GetFloatValue(n1) + n2.FloatValue);
							break;
						case BoxType.HeapPtr:
							goto lbL_primtive_add_heap;
#if DEBUG
						case BoxType.Fault:
						default:
							throw new InvalidOperationException();
#endif
					}

					break;
				case NaNBoxing.BoxType.Float:
					{
						switch (n2.ValueType)
						{
							case BoxType.Number:
							case BoxType.Undefined:
							case BoxType.Null:

								stackslots[dst.index].SetNumber(Extensions.GetDoubleValue(n1) + Extensions.GetDoubleValue(n2));
								break;
							case BoxType.Boolean:
							case BoxType.Int:
							case BoxType.Sbyte:
							case BoxType.Byte:
							case BoxType.Short:
							case BoxType.UShort:
							case BoxType.Float:
							case BoxType.Uint:
								stackslots[dst.index].SetFloat(Extensions.GetFloatValue(n1) + Extensions.GetFloatValue(n2));
								break;
							case BoxType.HeapPtr:
								goto lbL_primtive_add_heap;
#if DEBUG
							case BoxType.Fault:
							default:
								throw new InvalidOperationException();
#endif
						}
					}
					break;
				case NaNBoxing.BoxType.LocalString:
					{
						// Use efficient char-based operations to avoid string allocation
						Span<char> chars1 = stackalloc char[16];
						int charCount1 = n1.GetLocalStringChars(chars1);
						var str1 = charCount1 > 0 ? chars1.Slice(0, charCount1) : ReadOnlySpan<char>.Empty;

						switch (n2.ValueType)
						{
							case BoxType.Number:
							case BoxType.Undefined:
							case BoxType.Null:
							case BoxType.Boolean:
							case BoxType.Int:
							case BoxType.Uint:
							case BoxType.Sbyte:
							case BoxType.Byte:
							case BoxType.Short:
							case BoxType.UShort:
							case BoxType.Float:
								{
									Span<char> buffers = stackalloc char[128];
									var str2 = Extensions.GetPrimitiveValueToString(this, n2, buffers);
									string concatenated = $"{str1}{str2}";

									// 使用安全的字符串创建方法
									if (!TryCreateStringValue(concatenated, out stackslots[dst.index], ref error))
									{
										Context.StackPosition = basePos;
										return; // 错误已经在TryCreateStringValue中处理
									}
								}
								break;
							case BoxType.HeapPtr:
								{
									if (n2.HeapKind == (byte)RtHeapTypeKind.STRING)
									{
										var instance2 = Context.GC.Heap[n2.HeapPtr];
										var str2 = ((RtString)instance2).Str;
										string concatenated = $"{str1}{str2}";

										// 使用安全的字符串创建方法
										if (!TryCreateStringValue(concatenated, out stackslots[dst.index], ref error))
										{
											Context.StackPosition = basePos;
											return; // 错误已经在TryCreateStringValue中处理
										}
									}
#if DEBUG
									else
									{
										throw new InvalidOperationException();
									}
#endif
								}
								break;
							case BoxType.LocalString:
								{
									// Use efficient char-based concatenation for LocalString + LocalString
									Span<char> chars2 = stackalloc char[16];
									int charCount2 = n2.GetLocalStringChars(chars2);
									if (charCount2 > 0)
									{
										ReadOnlySpan<char> str2 = chars2.Slice(0, charCount2);
										string concatenated = $"{str1}{str2}";

										// 使用安全的字符串创建方法
										if (!TryCreateStringValue(concatenated, out stackslots[dst.index], ref error))
										{
											Context.StackPosition = basePos;
											return; // 错误已经在TryCreateStringValue中处理
										}
									}
									else
									{
										// n2 is empty LocalString, result is just str1
										if (!TryCreateStringValue(str1, out stackslots[dst.index], ref error))
										{
											Context.StackPosition = basePos;
											return;
										}
									}
								}
								break;
#if DEBUG
							case BoxType.Fault:
							default:
								throw new InvalidOperationException();
#endif
						}
					}
					break;
				case NaNBoxing.BoxType.HeapPtr:
					{
						if (n1.HeapKind == (byte)RtHeapTypeKind.STRING)
						{
							var instance1 = Context.GC.Heap[n1.HeapPtr];
							var str1 = ((RtString)instance1).Str;

							switch (n2.ValueType)
							{
								case BoxType.Number:
								case BoxType.Undefined:
								case BoxType.Null:
								case BoxType.Boolean:
								case BoxType.Int:
								case BoxType.Uint:
								case BoxType.Sbyte:
								case BoxType.Byte:
								case BoxType.Short:
								case BoxType.UShort:
								case BoxType.Float:
									{
										Span<char> buffers = stackalloc char[128];
										var str2 = Extensions.GetPrimitiveValueToString(this, n2, buffers);
										string concatenated = $"{str1}{str2}";

										// 使用安全的字符串创建方法
										if (!TryCreateStringValue(concatenated, out stackslots[dst.index], ref error))
										{
											Context.StackPosition = basePos;
											return; // 错误已经在TryCreateStringValue中处理
										}
									}
									break;
								case BoxType.LocalString:
									{
										// Use efficient char-based concatenation to avoid string allocation
										Span<char> chars2 = stackalloc char[16];
										int charCount2 = n2.GetLocalStringChars(chars2);
										if (charCount2 > 0)
										{
											string str2 = new string(chars2.Slice(0, charCount2));
											string concatenated = str1 + str2;

											// 使用安全的字符串创建方法
											if (!TryCreateStringValue(concatenated, out stackslots[dst.index], ref error))
											{
												Context.StackPosition = basePos;
												return; // 错误已经在TryCreateStringValue中处理
											}
										}
										else
										{
											// n2 is empty LocalString, result is just str1
											if (!TryCreateStringValue(str1, out stackslots[dst.index], ref error))
											{
												Context.StackPosition = basePos;
												return;
											}
										}
									}
									break;
								case BoxType.HeapPtr:
									{
										if (n2.HeapKind == (byte)RtHeapTypeKind.STRING)
										{
											var instance2 = Context.GC.Heap[n2.HeapPtr];
											var str2 = ((RtString)instance2).Str;
											string concatenated = str1 + str2;

											// 使用安全的字符串创建方法
											if (!TryCreateStringValue(concatenated, out stackslots[dst.index], ref error))
											{
												Context.StackPosition = basePos;
												return; // 错误已经在TryCreateStringValue中处理
											}
										}
#if DEBUG
										else
										{
											throw new InvalidOperationException();
										}
#endif
									}
									break;
#if DEBUG
								case BoxType.Fault:
								default:
									throw new InvalidOperationException();
#endif
							}

						}
#if DEBUG
						else
						{
							throw new InvalidOperationException();
						}
#endif
					}
					break;
#if DEBUG
				case NaNBoxing.BoxType.Fault:
				default:
					throw new InvalidOperationException();
#endif
			}
			Context.StackPosition = basePos;
			return;
		lbL_primtive_add_heap:;
			{
				Context.GC.CheckGC(ref error);

				if (n2.HeapKind == (byte)RtHeapTypeKind.STRING)
				{
					var instance = Context.GC.Heap[n2.HeapPtr];
					Span<char> buffers = stackalloc char[128];
					var str = Extensions.GetPrimitiveValueToString(this, n1, buffers);
					var str2 = ((RtString)instance).Str;
					Context.GC.CheckGC(ref error);

					string concatenated = $"{str}{str2}";

					// 使用安全的字符串创建方法
					if (!TryCreateStringValue(concatenated, out stackslots[dst.index], ref error))
					{
						Context.StackPosition = basePos;
						return; // 错误已经在TryCreateStringValue中处理
					}
					Context.StackPosition = basePos;
				}
#if DEBUG
				else
				{
					throw new InvalidOperationException();
				}
#endif
			}
		}



		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private unsafe void Exec_Add(int dst_index,byte** PC, ref ReceiveError error,  int scope_ptr,  Span<NaNBoxing> stackslots, int stackStPos, NaNBoxing thisPtr)
		{
			StackLocater dst;
			StackLocater v1;
			StackLocater v2;


			dst.index = dst_index;
			LoadStackLocater(&v1, PC);
			LoadStackLocater(&v2, PC);


			NaNBoxing n1 = stackslots[v1.index];
			NaNBoxing n2 = stackslots[v2.index];



			NaNBoxing sum;
			if (NaNBoxing.FastAdd(n1, n2, out sum))
			{
				stackslots[dst.index] = sum;
				return;
			}
			else
			{
				Exec_AddSlow(dst,  n1, n2, scope_ptr, stackStPos, stackslots, thisPtr, ref error);
			}
			
		}



		private unsafe void Exec_SubSlow(StackLocater dst,
		StackLocater v1,
		StackLocater v2, NaNBoxing n1,NaNBoxing n2,int scope_ptr,int stackStPos , Span<NaNBoxing> stackslots, NaNBoxing thisPtr, ref ReceiveError error)
		{

			//操作符重载
			ASClass t1; ASClass t2;
			int op_override_id1 = GetOpOverrideTypeId(n1, out t1);
			int op_override_id2 = GetOpOverrideTypeId(n2, out t2);
			if (op_override_id1 != -1 && op_override_id2 != -1)
			{
				var method = overrideOperatorMethods[(int)OverrideOperator.sub][op_override_id1][op_override_id2];
				if (method != null)
				{
#if FORCOMPILER
					if (IsComputeConstExpr)
					{
						throw new EvalConstException();
					}
#endif

					if (t1 != null)
					{
						InitScript((ASScript)t1._link_codescope.Parent.Container, ref error);
						if (error.raised)
						{
							return;
						}
					}
					if (t2 != null)
					{
						InitScript((ASScript)t2._link_codescope.Parent.Container, ref error);
						if (error.raised)
						{
							return;
						}
					}

					var @class = (ASClass)method.Container;

					if (Context.StackPosition + 2 >= Context.STACK_LENGTH)
					{
						RaiseStackOverflow(ref error);
						return;
					}

					Span<NaNBoxing> slots = Context.StackSlots.AsSpan(Context.StackPosition, 2);
					slots[0] = n1;
					slots[1] = n2;

					Context.StackPosition += 2;

					NaNBoxing cls = default; cls.SetHeapPtr(@class.__instance_index__, (byte)RtHeapTypeKind.CLASS, (byte)HeapKindFlag.NONE);
					unsafe
					{
						StackLocater* args = stackalloc StackLocater[2];
						args->index = 0;
						(args + 1)->index = 1;
						RunMethod(method, cls, scope_ptr, @class, 2, (byte*)args, slots, ref error, stackStPos + dst.index);
					}
					Context.StackPosition -= 2;

					return;
				}
			}





			if (!IsPrimitive(n1))
			{
				n1 = ToPrimitive(ref error, n1, HINT.h_number, scope_ptr, dst, dst, stackslots, stackStPos, thisPtr);
				if (error.raised)
				{
					return;
				}
			}

			if (n1.ValueType == BoxType.HeapPtr)
			{
				ConvertValueType(ref error, n1, TypeKind.Number, Context.NUMBER, ref n1); //这里不会出错 这里肯定是字符串
			}

			if (!IsPrimitive(n2))
			{
				n2 = ToPrimitive(ref error, n2, HINT.h_number, scope_ptr, dst, dst, stackslots, stackStPos, thisPtr);
				if (error.raised)
				{
					return;
				}
			}

			if (n2.ValueType == BoxType.HeapPtr)
			{
				ConvertValueType(ref error, n2, TypeKind.Number, Context.NUMBER, ref n2); //这里不会出错 这里肯定是字符串
			}

			switch (n1.ValueType)
			{
				case BoxType.Number:
					stackslots[dst.index].SetNumber(n1.Number - Extensions.GetDoubleValue(n2));
					break;
				case BoxType.Undefined:
					stackslots[dst.index].SetNumber(double.NaN - Extensions.GetDoubleValue(n2));
					break;
				case BoxType.Null:
					stackslots[dst.index].SetNumber(0.0 - Extensions.GetDoubleValue(n2));
					break;
				case BoxType.Boolean:
					switch (n2.ValueType)
					{
						case BoxType.Number:
							stackslots[dst.index].SetNumber((n1.Boolean ? 1 : 0) - Extensions.GetDoubleValue(n2));
							break;
						case BoxType.Undefined:
							stackslots[dst.index].SetNumber(double.NaN);
							break;
						case BoxType.Null:
							stackslots[dst.index].SetNumber((n1.Boolean ? 1 : 0) - 0.0);
							break;
						case BoxType.Boolean:
							stackslots[dst.index].SetInt((n1.Boolean ? 1 : 0) - (n2.Boolean ? 1 : 0));
							break;
						case BoxType.Int:
							stackslots[dst.index].SetInt((n1.Boolean ? 1 : 0) - n2.IntValue);
							break;
						case BoxType.Uint:
							stackslots[dst.index].SetNumber((n1.Boolean ? 1U : 0U) - n2.UIntValue);
							break;
						case BoxType.Sbyte:
						case BoxType.Byte:
						case BoxType.Short:
						case BoxType.UShort:
							stackslots[dst.index].SetInt((n1.Boolean ? 1 : 0) - Extensions.GetIntValue(n2));
							break;
						case BoxType.Float:
							stackslots[dst.index].SetFloat((n1.Boolean ? 1 : 0) - n2.FloatValue);
							break;
#if DEBUG
						case BoxType.HeapPtr:
						case BoxType.Fault:
						default:
							throw new InvalidOperationException();
#endif
					}
					break;
				case BoxType.Int:
					switch (n2.ValueType)
					{
						case BoxType.Undefined:
							stackslots[dst.index].SetNumber(double.NaN);
							break;
						case BoxType.Number:
						case BoxType.Null:
							stackslots[dst.index].SetNumber(n1.IntValue - Extensions.GetDoubleValue(n2));
							break;
						case BoxType.Boolean:
							stackslots[dst.index].SetInt(n1.IntValue - (n2.Boolean ? 1 : 0));
							break;
						case BoxType.Uint:
							stackslots[dst.index].SetNumber((double)n1.IntValue - n2.UIntValue);
							break;
						case BoxType.Float:
							stackslots[dst.index].SetFloat(n1.IntValue - n2.FloatValue);
							break;
						case BoxType.Int:
						case BoxType.Sbyte:
						case BoxType.Byte:
						case BoxType.Short:
						case BoxType.UShort:
							stackslots[dst.index].SetInt(n1.IntValue - Extensions.GetIntValue(n2));
							break;
#if DEBUG
						case BoxType.Fault:
						default:
							throw new InvalidOperationException();
#endif
					}
					break;
				case BoxType.Uint:
					switch (n2.ValueType)
					{
						case BoxType.Undefined:
							stackslots[dst.index].SetNumber(double.NaN);
							break;
						case BoxType.Number:
						case BoxType.Null:
							stackslots[dst.index].SetNumber(n1.UIntValue - Extensions.GetDoubleValue(n2));
							break;
						case BoxType.Boolean:
							stackslots[dst.index].SetUInt(n1.UIntValue - (n2.Boolean ? 1U : 0U));
							break;
						case BoxType.Int:
							stackslots[dst.index].SetNumber((double)n1.UIntValue - n2.IntValue);
							break;
						case BoxType.Uint:
							stackslots[dst.index].SetUInt(n1.UIntValue - n2.UIntValue);
							break;
						case BoxType.Sbyte:
							stackslots[dst.index].SetNumber((double)n1.UIntValue - n2.SByteValue);
							break;
						case BoxType.Byte:
							stackslots[dst.index].SetUInt(n1.UIntValue - n2.ByteValue);
							break;
						case BoxType.Short:
							stackslots[dst.index].SetNumber((double)n1.UIntValue - n2.ShortValue);
							break;
						case BoxType.UShort:
							stackslots[dst.index].SetUInt(n1.UIntValue - n2.UShortValue);
							break;
						case BoxType.Float:
							stackslots[dst.index].SetFloat((float)n1.UIntValue - n2.FloatValue);
							break;
#if DEBUG
						case BoxType.Fault:
						default:
							throw new InvalidOperationException();
#endif
					}
					break;
				case BoxType.Sbyte:
				case BoxType.Byte:
				case BoxType.Short:
				case BoxType.UShort:
					switch (n2.ValueType)
					{
						case BoxType.Undefined:
							stackslots[dst.index].SetNumber(double.NaN);
							break;
						case BoxType.Number:
						case BoxType.Null:
							stackslots[dst.index].SetNumber(Extensions.GetDoubleValue(n1) - Extensions.GetDoubleValue(n2));
							break;
						case BoxType.Uint:
							stackslots[dst.index].SetNumber(Extensions.GetDoubleValue(n1) - n2.UIntValue);
							break;
						case BoxType.Sbyte:
						case BoxType.Byte:
						case BoxType.Short:
						case BoxType.UShort:
						case BoxType.Boolean:
						case BoxType.Int:
							stackslots[dst.index].SetInt(Extensions.GetIntValue(n1) - Extensions.GetIntValue(n2));
							break;
						case BoxType.Float:
							stackslots[dst.index].SetFloat(Extensions.GetFloatValue(n1) - n2.FloatValue);
							break;
#if DEBUG
						case BoxType.HeapPtr:
						case BoxType.Fault:
						default:
							throw new InvalidOperationException();
#endif
					}
					break;
				case BoxType.Float:
					switch (n2.ValueType)
					{
						case BoxType.Number:
						case BoxType.Undefined:
						case BoxType.Null:

							stackslots[dst.index].SetNumber(Extensions.GetDoubleValue(n1) - Extensions.GetDoubleValue(n2));
							break;
						case BoxType.Boolean:
						case BoxType.Int:
						case BoxType.Sbyte:
						case BoxType.Byte:
						case BoxType.Short:
						case BoxType.UShort:
						case BoxType.Float:
						case BoxType.Uint:
							stackslots[dst.index].SetFloat(Extensions.GetFloatValue(n1) - Extensions.GetFloatValue(n2));
							break;
#if DEBUG
						case BoxType.HeapPtr:
						case BoxType.Fault:
						default:
							throw new InvalidOperationException();
#endif
					}
					break;
#if DEBUG
				case BoxType.HeapPtr:
				case BoxType.Fault:
				default:
					throw new InvalidOperationException();
#endif
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private unsafe void Exec_Sub(int dst_index,byte** PC, ref ReceiveError error,  int scope_ptr, Span<NaNBoxing> stackslots, int stackStPos, NaNBoxing thisPtr)
		{
			StackLocater dst;
			StackLocater v1;
			StackLocater v2;

			dst.index = dst_index;
			LoadStackLocater(&v1, PC);
			LoadStackLocater(&v2, PC);

			NaNBoxing n1 = stackslots[v1.index];
			NaNBoxing n2 = stackslots[v2.index];



			//NaNBoxing sub;
			if (NaNBoxing.FastMinus(n1, n2, ref stackslots[dst.index])) //out sub))
			{
				//stackslots[dst.index] = sub;
				return;
			}
			else
			{ 
				Exec_SubSlow(dst,v1,v2,n1,n2,scope_ptr,stackStPos,stackslots,thisPtr,ref error);
			}

		}



		private unsafe void Exec_Multiply( int dst_index,byte** PC, ref ReceiveError error, int scope_ptr, Span<NaNBoxing> stackslots, int stackStPos, NaNBoxing thisPtr)
		{
			StackLocater dst;
			StackLocater v1;
			StackLocater v2;

			dst.index = dst_index;
			LoadStackLocater(&v1, PC);
			LoadStackLocater(&v2, PC);

			NaNBoxing n1 = stackslots[v1.index];
			NaNBoxing n2 = stackslots[v2.index];



			//操作符重载
			ASClass t1; ASClass t2;
			int op_override_id1 = GetOpOverrideTypeId(n1, out t1);
			int op_override_id2 = GetOpOverrideTypeId(n2, out t2);
			if (op_override_id1 != -1 && op_override_id2 != -1)
			{
				var method = overrideOperatorMethods[(int)OverrideOperator.mul][op_override_id1][op_override_id2];
				if (method != null)
				{
#if FORCOMPILER
					if (IsComputeConstExpr)
					{
						throw new EvalConstException();
					}
#endif
					if (t1 != null)
					{
						InitScript((ASScript)t1._link_codescope.Parent.Container, ref error);
						if (error.raised)
						{
							return;
						}
					}
					if (t2 != null)
					{
						InitScript((ASScript)t2._link_codescope.Parent.Container, ref error);
						if (error.raised)
						{
							return;
						}
					}

					var @class = (ASClass)method.Container;

					if (Context.StackPosition + 2 >= Context.STACK_LENGTH)
					{
						RaiseStackOverflow(ref error);
						return;
					}

					Span<NaNBoxing> slots = Context.StackSlots.AsSpan(Context.StackPosition, 2);
					slots[0] = n1;
					slots[1] = n2;

					Context.StackPosition += 2;

					NaNBoxing cls = default; cls.SetHeapPtr(@class.__instance_index__, (byte)RtHeapTypeKind.CLASS, (byte)HeapKindFlag.NONE);
					unsafe
					{
						StackLocater* args = stackalloc StackLocater[2];
						args->index = 0;
						(args + 1)->index = 1;
						RunMethod(method, cls, scope_ptr, @class, 2, (byte*)args, slots, ref error, stackStPos + dst.index);
					}
					Context.StackPosition -= 2;

					return;
				}
			}


			if (!IsPrimitive(n1))
			{
				n1 = ToPrimitive(ref error, n1, HINT.h_number, scope_ptr, dst, dst, stackslots, stackStPos, thisPtr);
				if (error.raised)
				{
					return;
				}
			}

			if (n1.ValueType == BoxType.HeapPtr)
			{
				ConvertValueType(ref error, n1, TypeKind.Number, Context.NUMBER, ref n1); //这里不会出错 这里肯定是字符串
			}

			if (!IsPrimitive(n2))
			{
				n2 = ToPrimitive(ref error, n2, HINT.h_number, scope_ptr, dst, dst, stackslots, stackStPos, thisPtr);
				if (error.raised)
				{
					return;
				}
			}

			if (n2.ValueType == BoxType.HeapPtr)
			{
				ConvertValueType(ref error, n2, TypeKind.Number, Context.NUMBER, ref n2); //这里不会出错 这里肯定是字符串
			}

			switch (n1.ValueType)
			{
				case BoxType.Number:
				case BoxType.Undefined:
				case BoxType.Null:
					stackslots[dst.index].SetNumber(Extensions.GetDoubleValue(n1) * Extensions.GetDoubleValue(n2));
					break;
				case BoxType.Int:
				case BoxType.Uint:
					switch (n2.ValueType)
					{
						case BoxType.Number:
						case BoxType.Undefined:
						case BoxType.Null:
						case BoxType.Int:
						case BoxType.Uint:
						case BoxType.Boolean:
						case BoxType.Sbyte:
						case BoxType.Byte:
						case BoxType.Short:
						case BoxType.UShort:
							stackslots[dst.index].SetNumber(Extensions.GetDoubleValue(n1) * Extensions.GetDoubleValue(n2));
							break;
						case BoxType.Float:
							stackslots[dst.index].SetFloat(Extensions.GetFloatValue(n1) * Extensions.GetFloatValue(n2));
							break;
#if DEBUG
						default:
							throw new InvalidOperationException();
#endif
					}
					break;
				case BoxType.Boolean:
				case BoxType.Sbyte:
				case BoxType.Byte:
				case BoxType.Short:
				case BoxType.UShort:

					{
						switch (n2.ValueType)
						{
							case BoxType.Number:
							case BoxType.Undefined:
							case BoxType.Null:
							case BoxType.Int:
							case BoxType.Uint:
								stackslots[dst.index].SetNumber(Extensions.GetDoubleValue(n1) * Extensions.GetDoubleValue(n2));
								break;
							case BoxType.Boolean:
							case BoxType.Sbyte:
							case BoxType.Byte:
							case BoxType.Short:
							case BoxType.UShort:
								stackslots[dst.index].SetInt(Extensions.GetIntValue(n1) * Extensions.GetIntValue(n2));
								break;
							case BoxType.Float:
								stackslots[dst.index].SetFloat(Extensions.GetFloatValue(n1) * Extensions.GetFloatValue(n2));
								break;
#if DEBUG
							default:
								throw new InvalidOperationException();
#endif
						}
					}

					break;
				case BoxType.Float:
					{
						switch (n2.ValueType)
						{
							case BoxType.Number:
							case BoxType.Undefined:
							case BoxType.Null:
								stackslots[dst.index].SetNumber(Extensions.GetDoubleValue(n1) * Extensions.GetDoubleValue(n2));
								break;
							case BoxType.Int:
							case BoxType.Uint:
							case BoxType.Boolean:
							case BoxType.Sbyte:
							case BoxType.Byte:
							case BoxType.Short:
							case BoxType.UShort:
							case BoxType.Float:
								stackslots[dst.index].SetFloat(n1.FloatValue * Extensions.GetFloatValue(n2));
								break;
#if DEBUG
							default:
								throw new InvalidOperationException();
#endif
						}
					}
					break;
#if DEBUG
				default:
					throw new InvalidOperationException();
#endif
			}


		}



		private unsafe void Exec_Division(int dst_index,byte** PC, ref ReceiveError error, int scope_ptr, Span<NaNBoxing> stackslots, int stackStPos, NaNBoxing thisPtr)
		{
			StackLocater dst;
			StackLocater v1;
			StackLocater v2;

			dst.index = dst_index;
			LoadStackLocater(&v1, PC);
			LoadStackLocater(&v2, PC);


			NaNBoxing n1 = stackslots[v1.index];
			NaNBoxing n2 = stackslots[v2.index];


			//操作符重载
			ASClass t1; ASClass t2;
			int op_override_id1 = GetOpOverrideTypeId(n1, out t1);
			int op_override_id2 = GetOpOverrideTypeId(n2, out t2);
			if (op_override_id1 != -1 && op_override_id2 != -1)
			{
				var method = overrideOperatorMethods[(int)OverrideOperator.div][op_override_id1][op_override_id2];
				if (method != null)
				{
#if FORCOMPILER
					if (IsComputeConstExpr)
					{
						throw new EvalConstException();
					}
#endif
					if (t1 != null)
					{
						InitScript((ASScript)t1._link_codescope.Parent.Container, ref error);
						if (error.raised)
						{
							return;
						}
					}
					if (t2 != null)
					{
						InitScript((ASScript)t2._link_codescope.Parent.Container, ref error);
						if (error.raised)
						{
							return;
						}
					}

					var @class = (ASClass)method.Container;

					if (Context.StackPosition + 2 >= Context.STACK_LENGTH)
					{
						RaiseStackOverflow(ref error);
						return;
					}

					Span<NaNBoxing> slots = Context.StackSlots.AsSpan(Context.StackPosition, 2);
					slots[0] = n1;
					slots[1] = n2;

					Context.StackPosition += 2;

					NaNBoxing cls = default; cls.SetHeapPtr(@class.__instance_index__, (byte)RtHeapTypeKind.CLASS, (byte)HeapKindFlag.NONE);
					unsafe
					{
						StackLocater* args = stackalloc StackLocater[2];
						args->index = 0;
						(args + 1)->index = 1;
						RunMethod(method, cls, scope_ptr, @class, 2, (byte*)args, slots, ref error, stackStPos + dst.index);
					}
					Context.StackPosition -= 2;

					return;
				}
			}


			if (!IsPrimitive(n1))
			{
				n1 = ToPrimitive(ref error, n1, HINT.h_number, scope_ptr, dst, dst, stackslots, stackStPos, thisPtr);
				if (error.raised)
				{
					return;
				}
			}

			if (n1.ValueType == BoxType.HeapPtr)
			{
				ConvertValueType(ref error, n1, TypeKind.Number, Context.NUMBER, ref n1); //这里不会出错 这里肯定是字符串
			}

			if (!IsPrimitive(n2))
			{
				n2 = ToPrimitive(ref error, n2, HINT.h_number, scope_ptr, dst, dst, stackslots, stackStPos, thisPtr);
				if (error.raised)
				{
					return;
				}
			}

			if (n2.ValueType == BoxType.HeapPtr)
			{
				ConvertValueType(ref error, n2, TypeKind.Number, Context.NUMBER, ref n2); //这里不会出错 这里肯定是字符串
			}

			switch (n1.ValueType)
			{
				case BoxType.Number:
				case BoxType.Undefined:
				case BoxType.Null:
					stackslots[dst.index].SetNumber(Extensions.GetDoubleValue(n1) / Extensions.GetDoubleValue(n2));
					break;
				case BoxType.Boolean:
				case BoxType.Int:
				case BoxType.Uint:
				case BoxType.Sbyte:
				case BoxType.Byte:
				case BoxType.Short:
				case BoxType.UShort:
					switch (n2.ValueType)
					{
						case BoxType.Number:
						case BoxType.Undefined:
						case BoxType.Null:
						case BoxType.Boolean:
						case BoxType.Int:
						case BoxType.Uint:
						case BoxType.Sbyte:
						case BoxType.Byte:
						case BoxType.Short:
						case BoxType.UShort:
							stackslots[dst.index].SetNumber(Extensions.GetDoubleValue(n1) / Extensions.GetDoubleValue(n2));
							break;
						case BoxType.Float:
							stackslots[dst.index].SetFloat(Extensions.GetFloatValue(n1) / Extensions.GetFloatValue(n2));
							break;
#if DEBUG
						default:
							throw new InvalidOperationException();
#endif
					}
					break;
				case BoxType.Float:

					switch (n2.ValueType)
					{
						case BoxType.Number:
						case BoxType.Undefined:
						case BoxType.Null:
							stackslots[dst.index].SetNumber(Extensions.GetDoubleValue(n1) / Extensions.GetDoubleValue(n2));
							break;
						case BoxType.Boolean:
						case BoxType.Int:
						case BoxType.Uint:
						case BoxType.Sbyte:
						case BoxType.Byte:
						case BoxType.Short:
						case BoxType.UShort:
						case BoxType.Float:
							stackslots[dst.index].SetFloat(Extensions.GetFloatValue(n1) / Extensions.GetFloatValue(n2));
							break;
#if DEBUG
						default:
							throw new InvalidOperationException();
#endif
					}

					break;
#if DEBUG
				default:
					throw new InvalidOperationException();
#endif
			}

		}

		private unsafe void Exec_Modulus(int dst_index,byte** PC, ref ReceiveError error,  int scope_ptr, Span<NaNBoxing> stackslots, int stackStPos, NaNBoxing thisPtr)
		{
			StackLocater dst;
			StackLocater v1;
			StackLocater v2;

			dst.index = dst_index;
			LoadStackLocater(&v1, PC);
			LoadStackLocater(&v2, PC);


			NaNBoxing n1 = stackslots[v1.index];
			NaNBoxing n2 = stackslots[v2.index];

			//操作符重载
			ASClass t1; ASClass t2;
			int op_override_id1 = GetOpOverrideTypeId(n1, out t1);
			int op_override_id2 = GetOpOverrideTypeId(n2, out t2);
			if (op_override_id1 != -1 && op_override_id2 != -1)
			{
				var method = overrideOperatorMethods[(int)OverrideOperator.mod][op_override_id1][op_override_id2];
				if (method != null)
				{
#if FORCOMPILER
					if (IsComputeConstExpr)
					{
						throw new EvalConstException();
					}
#endif
					if (t1 != null)
					{
						InitScript((ASScript)t1._link_codescope.Parent.Container, ref error);
						if (error.raised)
						{
							return;
						}
					}
					if (t2 != null)
					{
						InitScript((ASScript)t2._link_codescope.Parent.Container, ref error);
						if (error.raised)
						{
							return;
						}
					}

					var @class = (ASClass)method.Container;

					if (Context.StackPosition + 2 >= Context.STACK_LENGTH)
					{
						RaiseStackOverflow(ref error);
						return;
					}

					Span<NaNBoxing> slots = Context.StackSlots.AsSpan(Context.StackPosition, 2);
					slots[0] = n1;
					slots[1] = n2;

					Context.StackPosition += 2;

					NaNBoxing cls = default; cls.SetHeapPtr(@class.__instance_index__, (byte)RtHeapTypeKind.CLASS, (byte)HeapKindFlag.NONE);
					unsafe
					{
						StackLocater* args = stackalloc StackLocater[2];
						args->index = 0;
						(args + 1)->index = 1;
						RunMethod(method, cls, scope_ptr, @class, 2, (byte*)args, slots, ref error, stackStPos + dst.index);
					}
					Context.StackPosition -= 2;

					return;
				}
			}

			if (!IsPrimitive(n1))
			{
				n1 = ToPrimitive(ref error, n1, HINT.h_number, scope_ptr, dst, dst, stackslots, stackStPos, thisPtr);
				if (error.raised)
				{
					return;
				}
			}

			if (n1.ValueType == BoxType.HeapPtr)
			{
				ConvertValueType(ref error, n1, TypeKind.Number, Context.NUMBER, ref n1); //这里不会出错 这里肯定是字符串
			}

			if (!IsPrimitive(n2))
			{
				n2 = ToPrimitive(ref error, n2, HINT.h_number, scope_ptr, dst, dst, stackslots, stackStPos, thisPtr);
				if (error.raised)
				{
					return;
				}
			}

			if (n2.ValueType == BoxType.HeapPtr)
			{
				ConvertValueType(ref error, n2, TypeKind.Number, Context.NUMBER, ref n2); //这里不会出错 这里肯定是字符串
			}

			switch (n1.ValueType)
			{
				case BoxType.Number:
				case BoxType.Undefined:
				case BoxType.Null:
					stackslots[dst.index].SetNumber(Extensions.GetDoubleValue(n1) % Extensions.GetDoubleValue(n2));
					break;
				case BoxType.Uint:

				case BoxType.Byte:

				case BoxType.UShort:

				case BoxType.Boolean:
				case BoxType.Int:
				case BoxType.Sbyte:
				case BoxType.Short:
					switch (n2.ValueType)
					{
						case BoxType.Number:
						case BoxType.Undefined:
						case BoxType.Null:
						case BoxType.Boolean:
						//stackslots[dst.index].SetNumber(Extensions.GetDoubleValue(n1) % Extensions.GetDoubleValue(n2));
						//break;
						case BoxType.Int:
						case BoxType.Sbyte:
						case BoxType.Short:
						//stackslots[dst.index].SetNumber(n1.UIntValue % Extensions.GetIntValue(n2));
						//break;
						case BoxType.Uint:
						//stackslots[dst.index].SetUInt(n1.UIntValue % n2.UIntValue);
						//break;
						case BoxType.Byte:
						//stackslots[dst.index].SetUInt(n1.UIntValue % n2.ByteValue);
						//break;
						case BoxType.UShort:
							//stackslots[dst.index].SetUInt(n1.UIntValue % n2.UShortValue); 因为有 % 0 除以0问题，所以只能都用Number
							stackslots[dst.index].SetNumber(Extensions.GetDoubleValue(n1) % Extensions.GetDoubleValue(n2));
							break;
						case BoxType.Float:
							stackslots[dst.index].SetFloat(Extensions.GetFloatValue(n1) % Extensions.GetFloatValue(n2));
							break;
#if DEBUG
						default:
							throw new InvalidOperationException();
#endif
					}

					break;
				case BoxType.Float:

					switch (n2.ValueType)
					{
						case BoxType.Number:
						case BoxType.Undefined:
						case BoxType.Null:
							stackslots[dst.index].SetNumber(Extensions.GetDoubleValue(n1) % Extensions.GetDoubleValue(n2));
							break;
						case BoxType.Boolean:
						case BoxType.Int:
						case BoxType.Uint:
						case BoxType.Sbyte:
						case BoxType.Byte:
						case BoxType.Short:
						case BoxType.UShort:
						case BoxType.Float:
							stackslots[dst.index].SetFloat(Extensions.GetFloatValue(n1) % Extensions.GetFloatValue(n2));
							break;
#if DEBUG
						default:
							throw new InvalidOperationException();
#endif
					}

					break;
#if DEBUG
				default:
					throw new InvalidOperationException();
#endif

			}


		}



		private unsafe void Exec_bitWise(int dst_index,byte** PC, ref ReceiveError error,int scope_ptr, Span<NaNBoxing> stackslots, int stackStPos, NaNBoxing thisPtr)
		{
			StackLocater dst;
			StackLocater v1;
			StackLocater v2;

			dst.index = dst_index;

			uint v = *(uint*)*PC; *PC += 4;
			byte opMode = (byte)(v & 0xff);
			v1.index = (int)(v >> 8);

			LoadStackLocater(&v2, PC);

			NaNBoxing n1 = stackslots[v1.index];
			NaNBoxing n2 = stackslots[v2.index];


			switch (opMode)
			{
				case 0: // &
					{
						if (!IsPrimitive(n1))
						{
							n1 = ToPrimitive(ref error, n1, HINT.h_number, scope_ptr, dst, dst, stackslots, stackStPos, thisPtr);
							if (error.raised)
							{
								return;
							}
						}
						ConvertValueType(ref error, n1, TypeKind.Uint, Context.UINT, ref n1);

						if (!IsPrimitive(n2))
						{
							n2 = ToPrimitive(ref error, n2, HINT.h_number, scope_ptr, dst, dst, stackslots, stackStPos, thisPtr);
							if (error.raised)
							{
								return;
							}
						}

						
						ConvertValueType(ref error, n2, TypeKind.Uint, Context.UINT, ref n2);

						stackslots[dst.index].SetInt((int)(n1.UIntValue & n2.UIntValue));

					}
					break;
				case 1:
					{
						if (!IsPrimitive(n1))
						{
							n1 = ToPrimitive(ref error, n1, HINT.h_number, scope_ptr, dst, dst, stackslots, stackStPos, thisPtr);
							if (error.raised)
							{
								return;
							}
						}
						ConvertValueType(ref error, n1, TypeKind.Int, Context.INT, ref n1);
						if (!IsPrimitive(n2))
						{
							n2 = ToPrimitive(ref error, n2, HINT.h_number, scope_ptr, dst, dst, stackslots, stackStPos, thisPtr);
							if (error.raised)
							{
								return;
							}
						}
						ConvertValueType(ref error, n2, TypeKind.Int, Context.INT, ref n2);

						stackslots[dst.index].SetInt((int)(n1.IntValue << n2.IntValue));
					}
					break;
				case 2: // ~
					{
						if (!IsPrimitive(n1))
						{
							n1 = ToPrimitive(ref error, n1, HINT.h_number, scope_ptr, dst, dst, stackslots, stackStPos, thisPtr);
							if (error.raised)
							{
								return;
							}
						}

						ConvertValueType(ref error, n1, TypeKind.Int, Context.INT, ref n1);


						stackslots[dst.index].SetInt(~n1.IntValue);
					}
					break;
				case 3: // |
					{
						if (!IsPrimitive(n1))
						{
							n1 = ToPrimitive(ref error, n1, HINT.h_number, scope_ptr, dst, dst, stackslots, stackStPos, thisPtr);
							if (error.raised)
							{
								return;
							}
						}

						ConvertValueType(ref error, n1, TypeKind.Uint, Context.UINT, ref n1);

						if (!IsPrimitive(n2))
						{
							n2 = ToPrimitive(ref error, n2, HINT.h_number, scope_ptr, dst, dst, stackslots, stackStPos, thisPtr);
							if (error.raised)
							{
								return;
							}
						}

						
						ConvertValueType(ref error, n2, TypeKind.Uint, Context.UINT, ref n2);

						stackslots[dst.index].SetInt((int)(n1.UIntValue | n2.UIntValue));
					}
					break;
				case 4:
					{
						if (!IsPrimitive(n1))
						{
							n1 = ToPrimitive(ref error, n1, HINT.h_number, scope_ptr, dst, dst, stackslots, stackStPos, thisPtr);
							if (error.raised)
							{
								return;
							}
						}
						ConvertValueType(ref error, n1, TypeKind.Int, Context.INT, ref n1);

						if (!IsPrimitive(n2))
						{
							n2 = ToPrimitive(ref error, n2, HINT.h_number, scope_ptr, dst, dst, stackslots, stackStPos, thisPtr);
							if (error.raised)
							{
								return;
							}
						}

						ConvertValueType(ref error, n2, TypeKind.Int, Context.INT, ref n2);


						stackslots[dst.index].SetInt((n1.IntValue >> n2.IntValue));
					}
					break;
				case 5:
					{
						if (!IsPrimitive(n1))
						{
							n1 = ToPrimitive(ref error, n1, HINT.h_number, scope_ptr, dst, dst, stackslots, stackStPos, thisPtr);
							if (error.raised)
							{
								return;
							}
						}
						ConvertValueType(ref error, n1, TypeKind.Uint, Context.UINT, ref n1);

						if (!IsPrimitive(n2))
						{
							n2 = ToPrimitive(ref error, n2, HINT.h_number, scope_ptr, dst, dst, stackslots, stackStPos, thisPtr);
							if (error.raised)
							{
								return;
							}
						}

						
						ConvertValueType(ref error, n2, TypeKind.Int, Context.INT, ref n2);

						stackslots[dst.index].SetUInt((n1.UIntValue >> n2.IntValue));
					}
					break;
				case 6: //xor
					{
						if (!IsPrimitive(n1))
						{
							n1 = ToPrimitive(ref error, n1, HINT.h_number, scope_ptr, dst, dst, stackslots, stackStPos, thisPtr);
							if (error.raised)
							{
								return;
							}
						}
						ConvertValueType(ref error, n1, TypeKind.Uint, Context.UINT, ref n1);

						if (!IsPrimitive(n2))
						{
							n2 = ToPrimitive(ref error, n2, HINT.h_number, scope_ptr, dst, dst, stackslots, stackStPos, thisPtr);
							if (error.raised)
							{
								return;
							}
						}

						
						ConvertValueType(ref error, n2, TypeKind.Uint, Context.UINT, ref n2);

						stackslots[dst.index].SetInt((int)(n1.UIntValue ^ n2.UIntValue));
					}
					break;
#if DEBUG
				default:
					throw new NotImplementedException();

#endif
			}




		}


		private void Comparse_Slow(
			byte opMode,
			StackLocater dst,
		//StackLocater v1,
		//StackLocater v2,
		NaNBoxing n1,NaNBoxing n2, Span<NaNBoxing> stackslots,int stackStPos  ,int scope_ptr , NaNBoxing thisPtr ,ref ReceiveError error)
		{

			//不能修改v1,v2的输入值
			if (Context.StackPosition + 3 >= Context.STACK_LENGTH)
			{
				RaiseStackOverflow(ref error);
				return;
			}

			int basePos = Context.StackPosition;
			Span<NaNBoxing> slots = Context.StackSlots.AsSpan(Context.StackPosition, 3);
			slots.Clear();
			Context.StackPosition += 3;
			StackLocater conv_result1 = default; conv_result1.index = 0;
			StackLocater conv_result2 = default; conv_result2.index = 1;
			StackLocater tempslot = default;tempslot.index = 2;


			if (!IsPrimitive(n1))
			{
				n1 = ToPrimitive(ref error, n1, HINT.h_number, scope_ptr, conv_result1, tempslot, slots, Context.StackPosition, thisPtr);
				if (error.raised)
				{
					Context.StackPosition = basePos;
					return;
				}
			}

			if (!IsPrimitive(n2))
			{
				n2 = ToPrimitive(ref error, n2, HINT.h_number, scope_ptr, conv_result2, tempslot, slots, Context.StackPosition, thisPtr);
				if (error.raised)
				{
					Context.StackPosition = basePos;
					return;
				}
			}

			int c_r;

			// 处理字符串比较的各种情况
			if ((n1.ValueType == BoxType.HeapPtr && n1.HeapKind == (byte)RtHeapTypeKind.STRING) ||
				n1.ValueType == BoxType.LocalString)
			{
				if ((n2.ValueType == BoxType.HeapPtr && n2.HeapKind == (byte)RtHeapTypeKind.STRING) ||
					n2.ValueType == BoxType.LocalString)
				{
					// 两个都是字符串类型，进行字符串比较
					ReadOnlySpan<char> str1, str2;

					if (n1.ValueType == BoxType.LocalString)
					{
						// Use efficient char-based extraction to avoid string allocation when possible
						Span<char> chars1 = stackalloc char[16];
						int charCount1 = n1.GetLocalStringChars(chars1);
						str1 = charCount1 > 0 ? new string(chars1.Slice(0, charCount1)) : string.Empty;
					}
					else
					{
						str1 = ((RtString)Context.GC.Heap[n1.HeapPtr]).Str;
					}

					if (n2.ValueType == BoxType.LocalString)
					{
						// Use efficient char-based extraction to avoid string allocation when possible
						Span<char> chars2 = stackalloc char[16];
						int charCount2 = n2.GetLocalStringChars(chars2);
						str2 = charCount2 > 0 ? new string(chars2.Slice(0, charCount2)) : string.Empty;
					}
					else
					{
						str2 = ((RtString)Context.GC.Heap[n2.HeapPtr]).Str;
					}

					int c = str1.CompareTo(str2, StringComparison.Ordinal); //string.CompareOrdinal(str1, str2);
					c_r = c;
				}
				else
				{
					// n1是字符串，n2不是字符串，转换为数字比较
					ConvertValueType(ref error, n1, TypeKind.Number, Context.NUMBER, ref n1);
#if DEBUG
					if (error.raised)
					{
						throw new InvalidOperationException();
					}
#endif
					ConvertValueType(ref error, n2, TypeKind.Number, Context.NUMBER, ref n2);
#if DEBUG
					if (error.raised)
					{
						throw new InvalidOperationException();
					}
#endif

					if (double.IsNaN(n1.Number) || double.IsNaN(n2.Number))
					{
						Context.StackPosition = basePos;

						stackslots[dst.index].SetBoolean(false);
						return;
					}

					if (n1.Number < n2.Number)
						c_r = -1;
					else if (n1.Number == n2.Number)
						c_r = 0;
					else
						c_r = 1;
				}
			}
			else if ((n2.ValueType == BoxType.HeapPtr && n2.HeapKind == (byte)RtHeapTypeKind.STRING) ||
					 n2.ValueType == BoxType.LocalString)
			{
				// n1不是字符串，n2是字符串，转换为数字比较
				ConvertValueType(ref error, n1, TypeKind.Number, Context.NUMBER, ref n1);
#if DEBUG
				if (error.raised)
				{
					throw new InvalidOperationException();
				}
#endif
				ConvertValueType(ref error, n2, TypeKind.Number, Context.NUMBER, ref n2);
#if DEBUG
				if (error.raised)
				{
					throw new InvalidOperationException();
				}
#endif

				if (double.IsNaN(n1.Number) || double.IsNaN(n2.Number))
				{
					Context.StackPosition = basePos;

					stackslots[dst.index].SetBoolean(false);
					return;
				}

				if (n1.Number < n2.Number)
					c_r = -1;
				else if (n1.Number == n2.Number)
					c_r = 0;
				else
					c_r = 1;
			}
			else
			{


				//				// 两个都不是字符串，转换为数字比较
				//				ConvertValueType(ref error, n1, TypeKind.Number, Context.NUMBER, ref n1); //这里不会失败
				//#if DEBUG
				//				if (error.raised)
				//				{
				//					throw new InvalidOperationException();
				//				}
				//#endif
				//				ConvertValueType(ref error, n2, TypeKind.Number, Context.NUMBER, ref n2); //这里不会失败
				//#if DEBUG
				//				if (error.raised)
				//				{
				//					throw new InvalidOperationException();
				//				}
				//#endif

				double d1 = Extensions.GetDoubleValue(n1);
				double d2 = Extensions.GetDoubleValue(n2);


				if (double.IsNaN(d1) || double.IsNaN(d2))
				{
					Context.StackPosition = basePos;

					stackslots[dst.index].SetBoolean(false);
					return;
				}

				if (d1 < d2)
					c_r = -1;
				else if (d1 == d2)
					c_r = 0;
				else
					c_r = 1;
			}


			Context.StackPosition = basePos;

			switch (opMode)
			{
				case 0:
					stackslots[dst.index].SetBoolean(c_r < 0);
					break;
				case 1:
					stackslots[dst.index].SetBoolean(c_r > 0);
					break;
				case 2:
					stackslots[dst.index].SetBoolean((c_r <= 0));
					break;
				case 3:
					stackslots[dst.index].SetBoolean((c_r >= 0));
					break;
				default:
#if DEBUG
					throw new InvalidOperationException();
#else
					Environment.FailFast("出错了，这里跑不到");
					break;
#endif

			}
		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private unsafe void Exec_Comparse(int dst_index,byte** PC, ref ReceiveError error, int scope_ptr, Span<NaNBoxing> stackslots, int stackStPos, NaNBoxing thisPtr)
		{
			StackLocater dst;
			StackLocater v1;
			StackLocater v2;

			dst.index = dst_index;

			uint v = *(uint*)*PC; *PC += 4;
			byte opMode = (byte)(v & 0xff);
			v1.index = (int)(v >> 8);

			LoadStackLocater(&v2, PC);

			NaNBoxing n1 = stackslots[v1.index];
			NaNBoxing n2 = stackslots[v2.index];

			if (((n1.ValueType == BoxType.Int || n1.ValueType > BoxType.Uint) && n1.ValueType < BoxType.Float)
				&&
				((n2.ValueType == BoxType.Int || n2.ValueType > BoxType.Uint) && n2.ValueType < BoxType.Float)
				)
			{
				int c_r = n1.IntValue - n2.IntValue;
				switch (opMode)
				{
					case 0:
						stackslots[dst.index].SetBoolean(c_r < 0);
						break;
					case 1:
						stackslots[dst.index].SetBoolean(c_r > 0);
						break;
					case 2:
						stackslots[dst.index].SetBoolean((c_r <= 0));
						break;
					case 3:
						stackslots[dst.index].SetBoolean((c_r >= 0));
						break;
					default:
#if DEBUG
					throw new InvalidOperationException();
#else
						Environment.FailFast("出错了，这里跑不到");
						break;
#endif

				}
			}
			else if ((n1.ValueType == BoxType.Number || n1.ValueType >= BoxType.Int && n1.ValueType <= BoxType.Float)
				&&
				(n2.ValueType == BoxType.Number || n2.ValueType >= BoxType.Int && n2.ValueType <= BoxType.Float)
				)
			{
				//数值快速比较
				double d1 = Extensions.GetDoubleValue(n1);
				double d2 = Extensions.GetDoubleValue(n2);


				if (double.IsNaN(d1) || double.IsNaN(d2))
				{
					stackslots[dst.index].SetBoolean(false);
					return;
				}

				int c_r;

				if (d1 < d2)
					c_r = -1;
				else if (d1 == d2)
					c_r = 0;
				else
					c_r = 1;

				switch (opMode)
				{
					case 0:
						stackslots[dst.index].SetBoolean(c_r < 0);
						break;
					case 1:
						stackslots[dst.index].SetBoolean(c_r > 0);
						break;
					case 2:
						stackslots[dst.index].SetBoolean((c_r <= 0));
						break;
					case 3:
						stackslots[dst.index].SetBoolean((c_r >= 0));
						break;
					default:
#if DEBUG
					throw new InvalidOperationException();
#else
						Environment.FailFast("出错了，这里跑不到");
						break;
#endif

				}

			}
			else
			{
				Comparse_Slow(opMode, dst, n1, n2, stackslots, stackStPos, scope_ptr, thisPtr, ref error);
			}
		}



		private void Incr_Decr_Slow(RtHeapBase methodscope,int addvalue, StackLocater dst,StackLocater result, NaNBoxing n1,Span<NaNBoxing> stackslots,  int scope_ptr, int stackStPos, ref ReceiveError error)
		{
			n1 = ToPrimitive(ref error, n1, HINT.h_number, scope_ptr, result, result, stackslots, stackStPos, ((RtMethodScope)methodscope).ThisPtr);
			if (error.raised)
			{
				goto flag_handle_error;
			}

			if (!IsNumeric(n1))
			{
				if (n1.ValueType == BoxType.LocalString || (n1.HeapKind == (byte)RtHeapTypeKind.STRING && n1.ValueType == BoxType.HeapPtr))
				{
					ConvertValueType(ref error, n1, TypeKind.Number, Context.NUMBER, ref n1); //这里不会出错。
				}
				else
				{
					n1.SetNumber(Extensions.GetDoubleValue(n1));
				}

			}

			NaNBoxing n2 = default; n2.SetInt(addvalue);

			bool fa = NaNBoxing.FastAdd(n1, n2, out NaNBoxing r);
			Debug.Assert(fa);
			stackslots[dst.index] = r;

			//Exec_Add(ref error, n1, n2, dst, scope_ptr, result, stackslots, stackStPos, ((RtMethodScope)methodscope).ThisPtr);
			//if (error.raised)
			//{
			//	goto flag_handle_error;
			//}

			if (dst.index != result.index)
			{
				stackslots[result.index] = n1;
			}

		flag_handle_error:
			;
		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private unsafe void Increment_decrement(int dst_index,byte** PC, RtHeapBase methodscope ,Span<NaNBoxing> stackslots,int scope_ptr,int stackStPos,ref ReceiveError error)
		{
			StackLocater dst;
			StackLocater src;
			StackLocater result;

			dst.index = dst_index;

			LoadStackLocater(&src, PC);
			LoadStackLocater(&result, PC);

			int addvalue = *(int*)*PC; *PC += 4;
			NaNBoxing n1 = stackslots[src.index];

			if ((n1.ValueType == BoxType.Int || n1.ValueType > BoxType.Uint) && n1.ValueType < BoxType.Float)
			{
				stackslots[dst.index].SetInt(n1.IntValue + addvalue);

				if (dst.index != result.index)
				{
					stackslots[result.index] = n1;
				}
			}
			else if (n1.ValueType == BoxType.Uint || n1.ValueType == BoxType.Number)
			{
				stackslots[dst.index].SetNumber(Extensions.GetDoubleValue(n1) + addvalue);
				if (dst.index != result.index)
				{
					stackslots[result.index] = n1;
				}
			}
			else if (n1.ValueType == BoxType.Float)
			{
				stackslots[dst.index].SetFloat(n1.FloatValue + addvalue);
				if (dst.index != result.index)
				{
					stackslots[result.index] = n1;
				}
			}
			else
			{
				Incr_Decr_Slow(methodscope, addvalue, dst, result, n1, stackslots, scope_ptr, stackStPos, ref error);
			}
		}



		private unsafe void GET_TYPEOF(int dst_index,byte** PC ,Span<NaNBoxing> stackslots)
		{
			StackLocater dst;
			StackLocater src;

			dst.index = dst_index;
			LoadStackLocater(&src, PC);


			var v = stackslots[src.index];

			switch (v.ValueType)
			{
				case BoxType.Undefined:
					stackslots[dst.index].SetHeapPtr(TYPEOF_undefined_STR, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
					break;
				case BoxType.Null:
					stackslots[dst.index].SetHeapPtr(TYPEOF_object_STR, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
					break;
				case BoxType.Boolean:
					stackslots[dst.index].SetHeapPtr(TYPEOF_boolean_STR, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
					break;
				case BoxType.Number:
				case BoxType.Int:
				case BoxType.Uint:
				case BoxType.Sbyte:
				case BoxType.Byte:
				case BoxType.Short:
				case BoxType.UShort:
				case BoxType.Float:
					stackslots[dst.index].SetHeapPtr(TYPEOF_number_STR, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
					break;
				case BoxType.LocalString:
					stackslots[dst.index].SetHeapPtr(TYPEOF_string_STR, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
					break;
				case BoxType.HeapPtr:

					switch ((RtHeapTypeKind)v.HeapKind)
					{
						case RtHeapTypeKind.STRING:
							stackslots[dst.index].SetHeapPtr(TYPEOF_string_STR, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
							break;
						case RtHeapTypeKind.CLASS:
						case RtHeapTypeKind.GLOBAL:
						case RtHeapTypeKind.INSTANCE:
						case RtHeapTypeKind.NAMESPACE:
						case RtHeapTypeKind.ARRAY:
						case RtHeapTypeKind.VECTOR:
							stackslots[dst.index].SetHeapPtr(TYPEOF_object_STR, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
							break;
						case RtHeapTypeKind.CLOSURE:
							stackslots[dst.index].SetHeapPtr(TYPEOF_function_STR, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
							break;
#if DEBUG
						case RtHeapTypeKind.STACK_CACHE_OBJ:
						case RtHeapTypeKind.DYNAMIC_PROPERTYS:
						case RtHeapTypeKind.SHAPE:
						case RtHeapTypeKind.MethodScope:
						default:
							throw new InvalidOperationException();
#endif
					}

					break;
#if DEBUG
				case BoxType.Fault:
				default:
					throw new InvalidOperationException();
#endif
			}
		}


		private unsafe void ITER_INITCTX(int dst_index, byte** PC, RtHeapBase methodscope ,ref ReceiveError error)
		{
			InitScript((ASScript)Context.IITERATOR.Instance._vtable.Items[0].Trait.Method.Body._link_codescope.Members[1].__rt_type_class__._link_codescope.Parent.Container, ref error);
			if (error.raised)
			{
				goto flag_handle_error;
			}

			// 从 dst_index 中解码 iterContextVar（复用存储空间）
			ScopeHeapLocater iterContextVar;
			iterContextVar.ScopeIndex = (ushort)(dst_index >> 16);
			iterContextVar.MemberIndex = (ushort)(dst_index & 0xFFFF);

			//if (Context.StackPosition + 1 >= Context.STACK_LENGTH)
			//{
			//	RaiseStackOverflow(ref error);
			//	goto flag_handle_error;
			//}

			RtHeapBase iterctx;
			int iter_context_ptr = Context.GC.RentIterContext(out iterctx);
			if (iter_context_ptr == 0)
			{
				RaiseOutOfMemory(ref error);
				goto flag_handle_error;
			}

			//Context.StackPosition++; //执行iter.next时，保留给它当返回值槽用

			((IterContxt)((RtInstance)iterctx).wapperedObject).PC = *PC;

			// 将迭代器上下文存储到方法变量中
			RtMethodScope heap = (RtMethodScope)methodscope;

			Debug.Assert(methodscope.Type._link_codescope.index == iterContextVar.ScopeIndex);
		

			NaNBoxing iterCtxValue = default;
			iterCtxValue.SetHeapPtr(iter_context_ptr, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.NONE);
			heap.SetSlot(iterCtxValue, iterContextVar.MemberIndex);



		flag_handle_error:
			;

		}



		private unsafe void ITER_GET( int dst_index, byte** PC, RtHeapBase methodscope, 
			
			Span<NaNBoxing> stackslots , int* method_scopes,int scope_ptr,
			ref ReceiveError error,
			byte* PC_START

			)
		{
			StackLocater iterSrcLoc;
			//StackLocater iteratorLoc;
			//StackLocater iter_contextLoc;

			ScopeHeapLocater iterSrcObj_Holder;
			{
				iterSrcObj_Holder.ScopeIndex = *(ushort*)*PC; *PC += 2;
				iterSrcObj_Holder.MemberIndex = *(ushort*)*PC; *PC += 2;
			}

			int flag_end_id;
			int flag_offset;

			//iteratorLoc.index = dst_index;
			LoadStackLocater(&iterSrcLoc, PC);



			LoadInt32(&flag_end_id, PC);
			LoadInt32(&flag_offset, PC);

			ScopeHeapLocater iterVar;
			{
				iterVar.ScopeIndex = (ushort)(dst_index >> 16);
				iterVar.MemberIndex = (ushort)(dst_index & 0xFFFF);
			}



			var ins = stackslots[iterSrcLoc.index];

			if (ins.ValueType == BoxType.HeapPtr)
			{
				if (Context.StackPosition + 1 >= Context.STACK_LENGTH)
				{
					RaiseStackOverflow(ref error);
					goto flag_handle_error;
				}


				int* m_scope = method_scopes;
				*m_scope++ = scope_ptr;

				RtMethodScope heap = (RtMethodScope)methodscope;
#if DEBUG
									if (methodscope.Type._link_codescope.index != iterSrcObj_Holder.ScopeIndex)
									{
										throw new InvalidOperationException();
									}

									if (methodscope.Type._link_codescope.index != iterVar.ScopeIndex)
									{
										throw new InvalidOperationException();
									}

#endif


				PrepareSaveMethodScope(heap, iterSrcObj_Holder, ref ins, m_scope, method_scopes, ref error);
				if (error.raised)
				{
					Context.GC.ReturnIterContextWhenGetIterFailed();
					goto flag_handle_error;
				}
				heap.SetSlot(ins, iterSrcObj_Holder.MemberIndex); //Context.GC.Heap[ins.HeapPtr];


				int iter_slot = Context.StackPosition;

				if (ins.HeapKind == (byte)RtHeapTypeKind.INSTANCE)
				{
					var obj = Context.GC.Heap[ins.HeapPtr];
					var type = (ASInstance)obj.Type;

					if (type == Context.GENERATOR.Instance)
					{
						heap.SetSlot(ins, iterVar.MemberIndex);
					}
					else if (type.iterator == null)
					{

						var obj_iter = Context.IITERATOR._link_codescope.Parent.Container.Traits[1].Class;
						InitCacheInstance(obj_iter, iter_slot, false);

						PrepareSaveMethodScope(heap, iterVar, ref Context.StackSlots[iter_slot], m_scope, method_scopes, ref error);
						if (error.raised)
						{
							Context.GC.ReturnIterContextWhenGetIterFailed();
							goto flag_handle_error;
						}
						heap.SetSlot(Context.StackSlots[iter_slot], iterVar.MemberIndex);

						var iter = (RtInstance)Context.GC.Heap[Context.StackSlots[iter_slot].HeapPtr];

						NaNBoxing index = default; index.SetInt(0);
						NaNBoxing count = default; count.SetInt(0);


						iter.SetSlot(index, 0, obj_iter.Instance._link_codescope, this);
						iter.SetSlot(count, 1, obj_iter.Instance._link_codescope, this);

						//throw new NotImplementedException();

					}
					else
					{

						Context.StackPosition++;
						RunMethod(type.iterator, ins, scope_ptr, type, 0, null, null, ref error, iter_slot);
						Context.StackPosition--;

						if (error.raised)
						{
							Context.GC.ReturnIterContextWhenGetIterFailed();
							goto flag_handle_error;
						}


						if (Context.StackSlots[iter_slot].ValueType != BoxType.HeapPtr) //return null?
						{
							Context.GC.ReturnIterContextWhenGetIterFailed();
							*PC = PC_START + flag_offset;
						}
						else
						{
#if DEBUG

												var iter_instance = Context.GC.Heap[Context.StackSlots[iter_slot].HeapPtr];
												if (!Extensions.IsImplements((ASInstance)iter_instance.Type, Context.IITERATOR.Instance))
												{
													throw new InvalidOperationException();
												}
#endif

							PrepareSaveMethodScope(heap, iterVar, ref Context.StackSlots[iter_slot], m_scope, method_scopes, ref error);
							if (error.raised)
							{
								Context.GC.ReturnIterContextWhenGetIterFailed();
								goto flag_handle_error;
							}
							heap.SetSlot(Context.StackSlots[iter_slot], iterVar.MemberIndex);


						}
					}
				}
				else if (ins.HeapKind == (byte)RtHeapTypeKind.GLOBAL || ins.HeapKind == (byte)RtHeapTypeKind.CLASS || ins.HeapKind == (byte)RtHeapTypeKind.CLOSURE
					||
					ins.HeapKind == (byte)RtHeapTypeKind.ARRAY
					||
					ins.HeapKind == (byte)RtHeapTypeKind.VECTOR
					)
				{
					var obj_iter = Context.IITERATOR._link_codescope.Parent.Container.Traits[1].Class;
					InitCacheInstance(obj_iter, iter_slot, false);

					PrepareSaveMethodScope(heap, iterVar, ref Context.StackSlots[iter_slot], m_scope, method_scopes, ref error);
					if (error.raised)
					{
						Context.GC.ReturnIterContextWhenGetIterFailed();
						goto flag_handle_error;
					}
					heap.SetSlot(Context.StackSlots[iter_slot], iterVar.MemberIndex);

					var iter = (RtInstance)Context.GC.Heap[Context.StackSlots[iter_slot].HeapPtr];


					NaNBoxing index = default; index.SetInt(0);
					NaNBoxing count = default; count.SetInt(0);


					iter.SetSlot(index, 0, obj_iter.Instance._link_codescope, this);
					iter.SetSlot(count, 1, obj_iter.Instance._link_codescope, this);

				}
				else
				{
#if DEBUG
										throw new InvalidOperationException();
#else
					Environment.FailFast("出错了，这里跑不到");  return;
#endif
				}




			}
			else
			{

				Context.GC.ReturnIterContextWhenGetIterFailed(); //需要返回
				*PC = PC_START + flag_offset;
			}

		flag_handle_error:
			;
		}


		private unsafe void ITER_NEXT(int dst_index, byte** PC, RtHeapBase methodscope,
			int stackStPos,
			Span<NaNBoxing > stackslots,
			byte* PC_START,
			ref ReceiveError error
			)
		{

			int mode;

			ScopeHeapLocater iterVar;

			StackLocater resultLoc;

			int flag_next_end_id;
			int flag_offset;

			LoadInt32(&mode, PC);

			{
				iterVar.ScopeIndex = *(ushort*)*PC; *PC += 2;
				iterVar.MemberIndex = *(ushort*)*PC; *PC += 2;
			}

			LoadStackLocater(&resultLoc, PC);

			LoadInt32(&flag_next_end_id, PC);
			LoadInt32(&flag_offset, PC);

			ScopeHeapLocater iterSrcObjSaveInVar;
			{
				iterSrcObjSaveInVar.ScopeIndex = (ushort)(dst_index >> 16);
				iterSrcObjSaveInVar.MemberIndex = (ushort)(dst_index & 0xFFFF);
			}


			RtMethodScope heap = (RtMethodScope)methodscope;

#if DEBUG
			if (methodscope.Type._link_codescope.index != iterSrcObjSaveInVar.ScopeIndex)
			{
				throw new InvalidOperationException();
			}
#endif
			var obj_h = heap.ReadSlot(iterSrcObjSaveInVar.MemberIndex, this);
#if DEBUG

			if (obj_h.ValueType != BoxType.HeapPtr)
				throw new InvalidOperationException();

			if (methodscope.Type._link_codescope.index != iterVar.ScopeIndex)
			{
				throw new InvalidOperationException();
			}

#endif
			NaNBoxing iter_v = heap.ReadSlot(iterVar.MemberIndex, this);


			var obj = Context.GC.Heap[obj_h.HeapPtr];
			var iter = Context.GC.Heap[iter_v.HeapPtr];


#if DEBUG
			if (Context.IITERATOR.Instance._vtable.Items[0].Trait.QName.Name != "next")
			{
				throw new InvalidOperationException();
			}

#endif
			//int cache_slot_index = Context.StackPosition - 1;

			var resulttype = Context.IITERATOR.Instance._vtable.Items[0].Trait.Method.Body._link_codescope.Members[1].__rt_type_class__;
			int result_ptr = InitCacheInstance(resulttype, stackStPos + resultLoc.index, true);
			RtHeapBase result = Context.GC.Heap[stackslots[resultLoc.index].HeapPtr];


			int m_idx =
				iter.Type == Context.GENERATOR.Instance ? 0 :
				((ASInstance)iter.Type)._interface_impl_.First((i) => i.interface_type == Context.IITERATOR.Type_identifier)[0];
			var vtableitem = iter.Type._vtable.Items[m_idx];
			var function = vtableitem.Trait.Method;


			if (Context.StackPosition + 3 >= Context.STACK_LENGTH)
			{
				RaiseStackOverflow(ref error);
				goto flag_handle_error;
			}

			var argSpan = Context.StackSlots.AsSpan(Context.StackPosition, 2);
			Context.StackPosition += 3;

			int reseveSlot = Context.StackPosition - 1;

			Context.StackSlots[reseveSlot].SetUndefined();

			argSpan[0] = obj_h; //stackslots[insLoc.index]; //obj
			argSpan[1] = stackslots[resultLoc.index];//result

			StackLocater* tmpArgLoc = stackalloc StackLocater[2];
			tmpArgLoc[0].index = 0;
			tmpArgLoc[1].index = 1;

			RunMethod(function, iter_v, iter_v.HeapPtr, iter.Type, 2, (byte*)tmpArgLoc, argSpan, ref error, reseveSlot);


			if (error.raised)
			{
				Context.StackPosition -= 3;
				goto flag_handle_error;
			}

			RtInstance result_payload = (RtInstance)result;
			var done = result_payload.ReadSlot(0, result.Type._link_codescope, this);
#if DEBUG
			if (done.ValueType != BoxType.Boolean) throw new InvalidOperationException();
#endif

			if (done.Boolean)
			{
				*PC = PC_START + flag_offset;
			}
			else
			{
				if (mode == 0)
				{
					var key = result_payload.ReadSlot(1, result.Type._link_codescope, this);
					//检查这里是否是一个struct!如果是，需要从Context.StackPosition-1槽里复制到stackslots[resultLoc.index]里!
					if (key.ValueType == BoxType.HeapPtr && key.HeapKind == (byte)RtHeapTypeKind.INSTANCE)
					{
						var check = Context.GC.Heap[key.HeapPtr];
						if (((ASInstance)check.Type).Flags.HasFlag(ClassFlags.Struct))
						{
							Debug.Assert(reseveSlot != stackStPos + resultLoc.index);
							//clone结构体
							int clonedptr = stackStPos + resultLoc.index + Context.CacheInstancePtr;
							var cacheObj = Context.GC.Heap[clonedptr];
							cacheObj.Type = check.Type;

							((RtInstance)cacheObj).methodscopeslot_ref_state = 0;
							((RtInstance)cacheObj).HEAPINSTANCE_PTR = 0;
							((RtInstance)cacheObj).CopyFrom(check, this, check.Type._link_codescope.TypeLayout.Size);

							key.SetHeapPtr(clonedptr, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.FLAG_STRUCT);

						}
					}



					stackslots[resultLoc.index] = key;
				}
				else
				{
					var value = result_payload.ReadSlot(2, result.Type._link_codescope, this);
					//检查这里是否是一个struct!如果是，需要从Context.StackPosition-1槽里复制到stackslots[resultLoc.index]里!
					if (value.ValueType == BoxType.HeapPtr && value.HeapKind == (byte)RtHeapTypeKind.INSTANCE)
					{
						var check = Context.GC.Heap[value.HeapPtr];
						if (((ASInstance)check.Type).Flags.HasFlag(ClassFlags.Struct))
						{
							//clone结构体
							int clonedptr = stackStPos + resultLoc.index + Context.CacheInstancePtr;
							var cacheObj = Context.GC.Heap[clonedptr];
							cacheObj.Type = check.Type;

							((RtInstance)cacheObj).methodscopeslot_ref_state = 0;
							((RtInstance)cacheObj).HEAPINSTANCE_PTR = 0;
							((RtInstance)cacheObj).CopyFrom(check, this, check.Type._link_codescope.TypeLayout.Size);

							value.SetHeapPtr(clonedptr, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.FLAG_STRUCT);

						}
					}

					stackslots[resultLoc.index] = value;

				}
			}


			Context.StackPosition -= 3; //将可能从Vector中读取的struct保留到拷贝之后


		flag_handle_error:
			;

		}

		private unsafe void ITER_CLOSE(int dst_index, byte** PC, RtHeapBase methodscope, 
			Span<NaNBoxing> stackslots,
			ExceptionContext* exception_ctx,
			ref ReceiveError error
			)
		{
			StackLocater insLoc;
			ScopeHeapLocater iterVar;
			ScopeHeapLocater iterContextVar;

			insLoc.index = dst_index;
			ScopeHeapLocater holderLoc;
			{
				holderLoc.ScopeIndex = *(ushort*)*PC; *PC += 2;
				holderLoc.MemberIndex = *(ushort*)*PC; *PC += 2;
			}

			{
				iterVar.ScopeIndex = *(ushort*)*PC; *PC += 2;
				iterVar.MemberIndex = *(ushort*)*PC; *PC += 2;
			}

			{
				iterContextVar.ScopeIndex = *(ushort*)*PC; *PC += 2;
				iterContextVar.MemberIndex = *(ushort*)*PC; *PC += 2;
			}


			RtMethodScope heap = (RtMethodScope)methodscope;

#if DEBUG
			if (methodscope.Type._link_codescope.index != holderLoc.ScopeIndex)
				throw new InvalidOperationException();
			if (methodscope.Type._link_codescope.index != iterContextVar.ScopeIndex)
				throw new InvalidOperationException();
			if (methodscope.Type._link_codescope.index != iterVar.ScopeIndex)
				throw new InvalidOperationException();

#endif
			var obj_h = heap.ReadSlot(holderLoc.MemberIndex, this);
			// 从方法变量读取迭代器上下文
			var iter_ctx_value = heap.ReadSlot(iterContextVar.MemberIndex, this);
			// 读iter对象
			var iter_v = heap.ReadSlot(iterVar.MemberIndex, this);

#if DEBUG
			if (obj_h.ValueType != BoxType.HeapPtr)
				throw new InvalidOperationException();
			if (iter_v.ValueType != BoxType.HeapPtr)
				throw new InvalidOperationException();
			if (iter_ctx_value.ValueType != BoxType.HeapPtr)
				throw new InvalidOperationException();
#endif
#if DEBUG
			if (Context.IITERATOR.Instance._vtable.Items[1].Trait.QName.Name != "close")
			{
				throw new InvalidOperationException();
			}
#endif

			var obj = Context.GC.Heap[obj_h.HeapPtr];
			var iter = Context.GC.Heap[iter_v.HeapPtr];
			var iter_ctx = Context.GC.Heap[iter_ctx_value.HeapPtr];

			stackslots[insLoc.index] = obj_h;


			int m_idx = iter.Type == Context.GENERATOR.Instance ? 1 :
				((ASInstance)iter.Type)._interface_impl_.First((i) => i.interface_type == Context.IITERATOR.Type_identifier)[1];
			var vtableitem = iter.Type._vtable.Items[m_idx];
			var function = vtableitem.Trait.Method;


			RunMethod(function, iter_v, iter_v.HeapPtr, iter.Type,
				 1, (byte*)&insLoc, stackslots, ref error, -1
				);

			if (error.raised)
			{
				//Context.StackPosition--;//在获取Context时，保留了一个槽位
				Context.GC.ReturnIterContext(iter_ctx);
				// 清空方法变量中的迭代器上下文
				NaNBoxing undefined = default;
				undefined.SetUndefined();
				heap.SetSlot(undefined, iterContextVar.MemberIndex);
				goto flag_handle_error;
			}

			NaNBoxing load_error = stackslots[exception_ctx->hold_error.index];
			if (load_error.ValueType != BoxType.Fault)
			{

				//说明有异常存在，中止访问proto
				Context.GC.ReturnIterContext(iter_ctx);
				// 清空方法变量中的迭代器上下文
				NaNBoxing undefined = default;
				undefined.SetUndefined();
				heap.SetSlot(undefined, iterContextVar.MemberIndex);
			}
			else if (obj.Type == Context.GENERATOR.Instance) //结束
			{
				Context.GC.ReturnIterContext(iter_ctx);
				// 清空方法变量中的迭代器上下文
				NaNBoxing undefined = default;
				undefined.SetUndefined();
				heap.SetSlot(undefined, iterContextVar.MemberIndex);
			}
			else
			{
				var proto = GetProtoPtr(obj);
				var iter_ctx_wapper = (IterContxt)((RtInstance)iter_ctx).wapperedObject;

				iter_ctx_wapper.visitedObjs.Add(obj_h.HeapPtr);

				if (iter_ctx_wapper.visitedObjs.Contains(proto))
				{
					//环只有function才可能产生，所以如果出现就跳到Function的proto里去
					proto = ((RtScriptClass)Context.GC.Heap[Context.FUNCTION.__instance_index__]).PROTO__PTR;
					if (iter_ctx_wapper.visitedObjs.Contains(proto))
					{
						//循环访问Function.prototype 跳到Object.prototype.
						proto = ((RtScriptClass)Context.GC.Heap[Context.OBJECT.__instance_index__]).PROTO__PTR;
					}
				}

#if DEBUG
				if (iter_ctx_wapper.visitedObjs.Contains(proto))
					throw new InvalidOperationException();
#endif

				if (proto != 0)
				{
					if (exception_ctx->FINALLY_JUMPTO_PTR == null) //如果不为空，说明有迭代过程中出现了跳转到外部的情况
					{
						var protoobj = Context.GC.Heap[proto];
						stackslots[insLoc.index].SetHeapPtr(proto, (byte)protoobj.Kind, (byte)(protoobj.Kind == RtHeapTypeKind.INSTANCE ? (((ASInstance)protoobj.Type).Flags.HasFlag(ClassFlags.Struct) ? HeapKindFlag.FLAG_STRUCT : HeapKindFlag.NONE) : HeapKindFlag.NONE));
						//跳回get_iter,访问_proto_.
						exception_ctx->FINALLY_JUMPTO_PTR = iter_ctx_wapper.PC;
					}
					else
					{
						//Context.StackPosition--;//在获取Context时，保留了一个槽位
						Context.GC.ReturnIterContext(iter_ctx);
						// 清空方法变量中的迭代器上下文
						NaNBoxing undefined = default;
						undefined.SetUndefined();
						heap.SetSlot(undefined, iterContextVar.MemberIndex);
					}
				}
				else
				{
					//Context.StackPosition--;//在获取Context时，保留了一个槽位
					Context.GC.ReturnIterContext(iter_ctx);
					// 清空方法变量中的迭代器上下文
					NaNBoxing undefined = default;
					undefined.SetUndefined();
					heap.SetSlot(undefined, iterContextVar.MemberIndex);
				}
			}

		flag_handle_error:
			;

		}




		private unsafe void Bindglobal_call(int dst_index,byte** PC, RtMethodScope methodscope, 
			Span<NaNBoxing> stackslots, int stackStPos,int scope_ptr,
			ref NaNBoxing global_obj,
			ref ReceiveError error)
		{
			StackLocater result;
			result.index = dst_index;

			StackLocater function;
			LoadStackLocater(&function, PC);

			int argsCount;
			LoadInt32(&argsCount, PC);

			//!!需要考虑对齐问题
			byte* argementsPtr = *PC;
			*PC += argsCount * 4;


			NaNBoxing funValue = stackslots[function.index];

			RtHeapBase funinstance = null;
			if (funValue.ValueType == BoxType.HeapPtr)
			{
				funinstance = Context.GC.Heap[funValue.HeapPtr];
				if (funinstance.Kind == RtHeapTypeKind.CLASS)
				{
					var @class = ((ASClass)((RtScriptClass)funinstance).Meta);

					if (@class.Instance.Flags.HasFlag(ClassFlags.NoConstructor))
					{
						RaiseTypeError(ref error, funValue, TypeKind.Function);
						goto flag_handle_error;
					}
					else if (@class.Type_identifier == (ulong)TypeKind.String && argsCount == 0)
					{
						RaiseTypeError(ref error, funValue, TypeKind.Function);
						goto flag_handle_error;
					}
					else
					{
						//if (argsCount == 1)
						{
							//stackslots[result.index] = ret;

							//throw new NotImplementedException("强制类型转换");
							//break;
							ExplicitConvert(ref error, (ushort)argsCount, (StackLocater*)argementsPtr, stackslots,
								(TypeKind)@class.Type_identifier, @class, ref stackslots[result.index], stackStPos + result.index, scope_ptr, ((RtMethodScope)methodscope).ThisPtr, false
								);
							if (error.raised)
							{
								goto flag_handle_error;
							}
							return;

						}
						//else
						//{
						//	RaiseArgementErrorCountMisMatch(ref error, null, 1, argsCount);
						//	goto flag_handle_error;
						//}
					}
				}
			}

			ConvertValueType(ref error, funValue, TypeKind.Function, null, ref funValue); //转换到function,不可能触发 valueOf()调用.
			if (error.raised)
			{
				goto flag_handle_error;
			}
			if (funValue.ValueType == NaNBoxing.BoxType.Null)
			{
				RaiseTypeError(ref error, funValue, TypeKind.Function);
				goto flag_handle_error;
			}

			//funinstance肯定不为空，如果为空前面就失败了。
			var func = ((ASMethodBody)funinstance.Type).Method;
			RtClosure closure = (RtClosure)funinstance;


			NaNBoxing _this_ = new NaNBoxing();
			ASContainer _scopeType;
			if (func.__ismethod)
			{
				_this_ = closure.This;
				_scopeType = closure.ScopeType;
			}
			else
			{
				////加载global。
				//var s = methodscope.Type._link_codescope.Parent; //Context.GC.Heap[scope_ptr].Type._link_codescope.Parent;
				//while (s.Kind != CodeScopeKind.Script)
				//{
				//	s = s.Parent;
				//}

				//var globalptr = ((ASScript)s.Container).__global_index__;

				//_this_.SetHeapPtr(globalptr);
				if (global_obj.ValueType != BoxType.HeapPtr)
				{
					//加载global。
					var s = methodscope.Type._link_codescope.Parent; //Context.GC.Heap[scope_ptr].Type._link_codescope.Parent;
					while (s.Kind != CodeScopeKind.Script)
					{
						s = s.Parent;
					}

					var globalptr = ((ASScript)s.Container).__global_index__;
					global_obj.SetHeapPtr(globalptr, (byte)RtHeapTypeKind.GLOBAL, (byte)HeapKindFlag.NONE);
					_this_.SetHeapPtr(globalptr, (byte)RtHeapTypeKind.GLOBAL, (byte)HeapKindFlag.NONE);
				}
				else
				{
					_this_ = global_obj;
				}


				_scopeType = Context.GC.Heap[closure.ScopePtr].Type;
			}


			NaNBoxing ret = RunMethod(func, _this_, closure.ScopePtr, _scopeType, (ushort)argsCount, argementsPtr, stackslots, ref error, stackStPos + result.index, funValue.HeapPtr);
			if (error.raised)
			{
				goto flag_handle_error;
			}

			stackslots[result.index] = ret;


		flag_handle_error:
			;

		}


		private unsafe void Bindthis_call(int dst_index, byte** PC ,RtHeapBase methodscope,
			
			Span<NaNBoxing> stackslots,
			int stackStPos,
			int scope_ptr,
			
			
			ref ReceiveError error
			)
		{

			StackLocater result;
			result.index = dst_index;

			StackLocater function;
			LoadStackLocater(&function, PC);

			StackLocater _this_;
			LoadStackLocater(&_this_, PC);

			int argsCount;
			LoadInt32(&argsCount, PC);

			//!!需要考虑对齐问题
			byte* argementsPtr = *PC;
			*PC += argsCount * 4;



			NaNBoxing thisValue;
			if (_this_.index >= 0)
			{
				thisValue = stackslots[_this_.index];
#if DEBUG
				if (thisValue.ValueType != NaNBoxing.BoxType.HeapPtr)
				{
				}
				else
				{
					RtHeapBase ins = Context.GC.Heap[thisValue.HeapPtr];
					if (ins.Kind == RtHeapTypeKind.STACK_CACHE_OBJ
						//||
						//ins.TypeKind == RtHeapTypeKind.CACHE_LD_CLASS
						||
						ins.Kind == RtHeapTypeKind.SHAPE
						)
					{
						throw new InvalidOperationException();
					}

				}

#endif

			}
			else
			{
				//加载global
				var s = methodscope.Type._link_codescope.Parent; //Context.GC.Heap[scope_ptr].Type._link_codescope.Parent;
				while (s.Kind != CodeScopeKind.Script)
				{
					s = s.Parent;
				}

				var globalptr = ((ASScript)s.Container).__global_index__;

				thisValue = new NaNBoxing();
				thisValue.SetHeapPtr(globalptr, (byte)RtHeapTypeKind.GLOBAL, (byte)HeapKindFlag.NONE);
			}


			NaNBoxing funValue = stackslots[function.index];

			RtHeapBase funinstance = null;
			if (funValue.ValueType == BoxType.HeapPtr)
			{
				funinstance = Context.GC.Heap[funValue.HeapPtr];
				if (funinstance.Kind == RtHeapTypeKind.CLASS)
				{
					var @class = ((ASClass)((RtScriptClass)funinstance).Meta);

					if (@class.Instance.Flags.HasFlag(ClassFlags.NoConstructor))
					{
						RaiseTypeError(ref error, funValue, TypeKind.Function);
						goto flag_handle_error;
					}
					else if (@class.Type_identifier == (ulong)TypeKind.String && argsCount == 0)
					{
						RaiseTypeError(ref error, funValue, TypeKind.Function);
						goto flag_handle_error;
					}
					else
					{
						//if (argsCount == 1)
						//{
						//stackslots[result.index] = ret;
						//throw new NotImplementedException("强制类型转换");
						//break;
						ExplicitConvert(ref error, (ushort)argsCount, (StackLocater*)argementsPtr, stackslots,
							(TypeKind)@class.Type_identifier, @class, ref stackslots[result.index], stackStPos + result.index, scope_ptr, ((RtMethodScope)methodscope).ThisPtr, false
							);
						if (error.raised)
						{
							goto flag_handle_error;
						}
						return;

						//}
						//else
						//{
						//	RaiseArgementErrorCountMisMatch(ref error, null, 1, argsCount);
						//	goto flag_handle_error;
						//}
					}


				}
			}

			ConvertValueType(ref error, funValue, TypeKind.Function, null, ref funValue); //转换到Function,不可能触发valueOf()调用
			if (error.raised)
			{
				goto flag_handle_error;
			}
			if (funValue.ValueType == NaNBoxing.BoxType.Null)
			{
				RaiseTypeError(ref error, funValue, TypeKind.Function);
				goto flag_handle_error;
			}


#if DEBUG
			if (funValue.ValueType != NaNBoxing.BoxType.HeapPtr)
				throw new InvalidOperationException();

			if (Context.GC.Heap[funValue.HeapPtr].Kind != RtHeapTypeKind.CLOSURE)
				throw new InvalidOperationException();

#endif
			//funinstance = Context.GC.Heap[funValue.HeapPtr];

			//执行到这里 funinstance肯定不为空
			var func = ((ASMethodBody)funinstance.Type).Method;
			RtClosure closure = (RtClosure)funinstance;



			NaNBoxing ret = RunMethod(
				func,
				((func.__ismethod && !func.__is_call_or_apply) ? closure.This : thisValue),
				closure.ScopePtr,

				(func.__ismethod && !func.__is_call_or_apply) ? closure.ScopeType : Context.GC.Heap[closure.ScopePtr].Type,

				(ushort)argsCount, argementsPtr, stackslots, ref error,
				stackStPos + result.index,
				funValue.HeapPtr
				);

			if (error.raised)
			{
				goto flag_handle_error;
			}

			stackslots[result.index] = ret;


		flag_handle_error:
			;

		}




		private unsafe void Ld_function_call(byte** PC,int dst_index, RtHeapBase methodscope,Span<NaNBoxing> constants, Span<NaNBoxing> stackslots ,ASContainer scopeType,int scope_ptr,
			int stackStPos,
			
			ref ReceiveError error
			)
		{
			StackLocater target;
			target.index = dst_index;

			int function_id = 0;
			LoadInt32(&function_id, PC);

			int argsCount;
			LoadInt32(&argsCount, PC);

			//!!需要考虑对齐问题
			byte* argementsPtr = *PC;
			*PC += argsCount * 4;


			NaNBoxing fbox = constants[function_id];
#if DEBUG
			if (fbox.ValueType != NaNBoxing.BoxType.Uint)
				throw new InvalidOperationException();
#endif

			ASMethod function = Context.link_const_methods[(int)fbox.UIntValue]; //((ASMethodBody)obj.Type).Method;


			////加载global。或者instance。
			//var s = function.Body._link_codescope.Parent;
			//while (s.Kind != CodeScopeKind.Script && s.Kind != CodeScopeKind.Instance)
			//{
			//    s = s.Parent;
			//}
			//var globalptr =((ASScript)s.Container).__global_index__;

			//NaNBoxing _this_ = new NaNBoxing();
			//_this_.SetHeapPtr(globalptr);

			var o = methodscope; //Context.GC.Heap[scope_ptr];
								 //int instancePtr = scope_ptr;
			NaNBoxing instancePtr = default; instancePtr.SetHeapPtr(scope_ptr, (byte)o.Kind, (byte)(o.Kind == RtHeapTypeKind.INSTANCE ? (((ASInstance)o.Type).Flags.HasFlag(ClassFlags.Struct) ? HeapKindFlag.FLAG_STRUCT : HeapKindFlag.NONE) : HeapKindFlag.NONE));
			do
			{
				if (o.Kind == RtHeapTypeKind.MethodScope)
				{
					RtMethodScope rtPayload = (RtMethodScope)o;
					o = Context.GC.Heap[rtPayload.ParentPtr];
					instancePtr.SetHeapPtr(rtPayload.ParentPtr, (byte)o.Kind, (byte)(o.Kind == RtHeapTypeKind.INSTANCE ? (((ASInstance)o.Type).Flags.HasFlag(ClassFlags.Struct) ? HeapKindFlag.FLAG_STRUCT : HeapKindFlag.NONE) : HeapKindFlag.NONE));
				}
				else
				{
					break;
				}

			} while (true);

			NaNBoxing _this_ = new NaNBoxing();
			_this_ = instancePtr; //.SetHeapPtr(instancePtr);


			NaNBoxing result = RunMethod(function, _this_, scope_ptr, scopeType, (ushort)argsCount, argementsPtr, stackslots, ref error, stackStPos + target.index);
			if (error.raised)
			{
				goto flag_handle_error;
			}

			stackslots[target.index] = result;

		flag_handle_error:
			;

		}


		private unsafe void Ld_function_bindglobal_call(byte** PC, RtHeapBase methodscope,
			int dst_index,
			Span<NaNBoxing> constants,
			Span<NaNBoxing> stackslots,
			int scope_ptr,int stackStPos,
			
			int * method_scopes,
			ref NaNBoxing global_obj,
			ref ReceiveError error
			)
		{
			StackLocater target;
			target.index = dst_index;

			int function_id; LoadInt32(&function_id, PC);
			ScopeHeapLocater heapLocater = *(ScopeHeapLocater*)(*PC); *PC += 4;
			
			int argsCount;
			LoadInt32(&argsCount, PC);

			//!!需要考虑对齐问题
			byte* argementsPtr = *PC;
			*PC += argsCount * 4;



			NaNBoxing fbox = constants[function_id];


#if DEBUG
			if (fbox.ValueType != NaNBoxing.BoxType.Uint)
				throw new InvalidOperationException();
#endif

			ASMethod function = Context.link_const_methods[(int)fbox.UIntValue];

			RtHeapBase closure;
			int closure_ptr;

			
			closure_ptr = Ld_function_and_store_member(function, heapLocater, methodscope, scope_ptr, ref error, stackStPos, target, stackslots, method_scopes, out closure);
			if (error.raised)
			{
				goto flag_handle_error;
			}
			


			NaNBoxing _this_ = default;
			if (global_obj.ValueType != BoxType.HeapPtr)
			{
				//加载global。
				var s = methodscope.Type._link_codescope.Parent; //Context.GC.Heap[scope_ptr].Type._link_codescope.Parent;
				while (s.Kind != CodeScopeKind.Script)
				{
					s = s.Parent;
				}

				var globalptr = ((ASScript)s.Container).__global_index__;
				global_obj.SetHeapPtr(globalptr, (byte)RtHeapTypeKind.GLOBAL, (byte)HeapKindFlag.NONE);
				_this_.SetHeapPtr(globalptr, (byte)RtHeapTypeKind.GLOBAL, (byte)HeapKindFlag.NONE);
			}
			else
			{
				_this_ = global_obj;
			}

			var _scopeType = methodscope.Type; //Context.GC.Heap[((RtClosure)closure).ScopePtr].Type;

			NaNBoxing ret = RunMethod(((ASMethodBody)closure.Type).Method, _this_,
				((RtClosure)closure).ScopePtr, _scopeType,
				(ushort)argsCount, argementsPtr, stackslots, ref error, stackStPos + target.index, closure_ptr);
			if (error.raised)
			{
				goto flag_handle_error;
			}

			stackslots[target.index] = ret;


		flag_handle_error:
			;

		}





		private unsafe void NEW_INSTANCE(int dst_index,byte** PC, int stackStPos, int scope_ptr, 
			Span<NaNBoxing> stackslots,ASContainer scopeType,
			RtHeapBase methodscope,
			ref ReceiveError error)
		{
			StackLocater target;
			StackLocater typeLocater;
			target.index = dst_index;
			LoadStackLocater(&typeLocater, PC);
			int argsCount;
			LoadInt32(&argsCount, PC);

			//StackLocater* argements = (StackLocater*)PC;
			//!!需要考虑对齐问题
			byte* argementsPtr = *PC;
			*PC += argsCount * 4;


			NaNBoxing type_box = stackslots[typeLocater.index];

			if (type_box.ValueType == BoxType.HeapPtr)
			{

				if (type_box.HeapKind == (byte)RtHeapTypeKind.CLASS)
				{
					RtHeapBase type = Context.GC.Heap[type_box.HeapPtr];
					ASClass @class = (ASClass)((RtScriptClass)type).Meta;
					//构造实例

					RtHeapBase instance;
					NaNBoxing instancePtr = default;

					if (@class.Instance.Flags.HasFlag(ClassFlags.NoConstructor))
					{
						stackslots[target.index].SetNull();
						if (@class != Context.METHOD_CLOSURE)
						{
							RaiseTypeError_Instantiation_non_constructor(ref error);
						}
						return;
					}
					else if (@class.Instance.Flags.HasFlag(ClassFlags.Vector))
					{
						int ptrIndex = stackStPos + target.index;

						//instancePtr = Context.CacheVectorPtr + ptrIndex;
						//instance = Context.GC.Heap[instancePtr];

						instancePtr.SetHeapPtr(Context.CacheVectorPtr + ptrIndex, (byte)RtHeapTypeKind.VECTOR, (byte)HeapKindFlag.NONE);
						instance = Context.GC.Heap[instancePtr.HeapPtr];



						instance.Type = @class.Instance;
						((RtVector)instance).HEAPINSTANCE_PTR = 0;
						((RtVector)instance).element_asclass = @class.Instance._element_class;
						((RtVector)instance).element_type = @class.Instance._element_class == null ? TypeKind.Any : (TypeKind)@class.Instance._element_class.Type_identifier;
						//((RtPayloadVector)instance).GetStore(this).SetBuffer(0);
						((RtVector)instance).GetStore().length = 0;

						stackslots[target.index] = instancePtr; //.SetHeapPtr(instancePtr , (byte)RtHeapTypeKind.VECTOR);

						//throw new NotImplementedException();
					}
					else if (
						(
#if FORCOMPILER
						!IsComputeConstExpr &&
#endif
						@class.Instance.Flags.HasFlag(ClassFlags.CacheAble)
						)
						||
						@class.Instance.Flags.HasFlag(ClassFlags.Struct)
						)
					{
						int ptrIndex = stackStPos + target.index;
						//instancePtr = Context.CacheInstancePtr + ptrIndex;
						instancePtr.SetHeapPtr(InitCacheInstance(@class, ptrIndex, true), (byte)RtHeapTypeKind.INSTANCE, (byte)(@class.Instance.Flags.HasFlag(ClassFlags.Struct) ? HeapKindFlag.FLAG_STRUCT : HeapKindFlag.NONE));

						instance = Context.GC.Heap[instancePtr.HeapPtr];

						//instance = Context.GC.Heap[instancePtr];
						//instance.Type = @class.Instance;

						//((RtPayloadInstance)instance).HEAPINSTANCE_PTR = 0;
						//((RtPayloadInstance)instance).Set_PROPERTY_PTR(0, this);
						//((RtPayloadInstance)instance).Set_PROTOTYPE(((RtPayloadScriptClass)Context.GC.Heap[@class.__instance_index__]).PROTO__PTR, this);
						//((RtPayloadInstance)instance).methodscopeslot_ref_state = 0;

						//CodeScope scope = @class.Instance._link_codescope;
						//if (scope.TypeLayout.Size > 0)
						//{
						//	((RtPayloadInstance)instance).Init(scope, this);
						//}

						//stackslots[target.index].SetHeapPtr(instancePtr);

					}
					else
					{

						Context.GC.CheckGC(ref error);

						if (@class.Type_identifier == (ulong)TypeKind.Array)
						{
							int ext_slot = 0;
							if (argsCount > 0)
							{
								var test = *(StackLocater*)argementsPtr;
								if (test.index == target.index)
								{
									ext_slot = 1;
								}
							}

							if (argsCount <= RtArray.MAX_CACHE_ELEMENT + ext_slot)
							{
								int ptrIndex = stackStPos + target.index;
								instancePtr.SetHeapPtr(Context.CacheArrayPtr + ptrIndex, (byte)RtHeapTypeKind.ARRAY, (byte)HeapKindFlag.NONE);
								instance = Context.GC.Heap[instancePtr.HeapPtr];
								instance.Type = Context.ARRAY.Instance;

								((RtArray)instance).array_len = 0;
								((RtArray)instance).methodscopeslot_ref_state = 0;
								((RtArray)instance).HEAPINSTANCE_PTR = 0;


							}
							else
							{
								instancePtr.SetHeapPtr(Context.GC.AllocArray(out instance, RtArray.ArrayStoreMode.normal), (byte)RtHeapTypeKind.ARRAY, (byte)HeapKindFlag.NONE);
							}
						}
						else if (@class.Type_identifier == (ulong)TypeKind.String)
						{
							if (argsCount == 0)
							{
								instancePtr.SetHeapPtr(EMPTY_STR, (byte)RtHeapTypeKind.STRING, (byte)HeapKindFlag.NONE);
								stackslots[target.index] = instancePtr; //.SetHeapPtr(instancePtr, (byte)RtHeapTypeKind.STRING);

							}
							else if (argsCount >= 1)
							{
								byte* P = argementsPtr + sizeof(StackLocater) * (Context.STRING.Instance.Constructor.Parameters.Count - 1);
								StackLocater argLocater;
								LoadStackLocater(&argLocater, &P);

								NaNBoxing box = stackslots[argLocater.index];
								ConvertValueType(ref error, box, TypeKind.String, Context.STRING, ref stackslots[target.index], scope_ptr);
								if (error.raised)
								{
									goto flag_handle_error;
								}

							}

							return;

						}
						else if (@class.Type_identifier == (ulong)TypeKind.Boolean)
						{
							if (argsCount == 0)
							{
								stackslots[target.index].SetBoolean(false);
							}
							else if (argsCount >= 1)
							{
								byte* P = argementsPtr + sizeof(StackLocater) * (Context.BOOLEAN.Instance.Constructor.Parameters.Count - 1);
								StackLocater argLocater;
								LoadStackLocater(&argLocater, &P);

								NaNBoxing box = stackslots[argLocater.index];
								ConvertValueType(ref error, box, TypeKind.Boolean, Context.BOOLEAN, ref stackslots[target.index]);
#if DEBUG
								if (error.raised)
								{
									throw new InvalidOperationException();  //转BOOL不会失败
								}
#endif
							}

							return;
						}
						else if (@class.Type_identifier <= 7)
						{
							Debug.Assert(@class.Type_identifier > 0);

							if (argsCount == 0)
							{
								switch ((TypeKind)@class.Type_identifier)
								{
									case TypeKind.SByte:
										stackslots[target.index].SetSByte(0);
										break;
									case TypeKind.Byte:
										stackslots[target.index].SetByte(0);
										break;
									case TypeKind.Short:
										stackslots[target.index].SetShort(0);
										break;
									case TypeKind.UShort:
										stackslots[target.index].SetUShort(0);
										break;
									case TypeKind.Int:
										stackslots[target.index].SetInt(0);
										break;
									case TypeKind.Uint:
										stackslots[target.index].SetUInt(0);
										break;
									default:
#if DEBUG
										throw new InvalidOperationException();
#else
													Environment.FailFast("出错了，这里跑不到");  return;
#endif
								}


							}
							else if (argsCount >= 1)
							{
								byte* P = argementsPtr + sizeof(StackLocater) * (Context.NUMBER.Instance.Constructor.Parameters.Count - 1);
								StackLocater argLocater;
								LoadStackLocater(&argLocater, &P);

								NaNBoxing box = stackslots[argLocater.index];

								box = ToPrimitive(ref error, box, HINT.h_number, scope_ptr, target, target, stackslots, stackStPos, ((RtMethodScope)methodscope).ThisPtr);
								if (error.raised)
								{
									goto flag_handle_error;
								}

								//ConvertValueType(ref error, box, TypeKind.Number, Context.NUMBER, ref stackslots[target.index]);

								switch ((TypeKind)@class.Type_identifier)
								{
									case TypeKind.SByte:
										ConvertValueType(ref error, box, TypeKind.SByte, Context.SBYTE, ref stackslots[target.index]);
										break;
									case TypeKind.Byte:
										ConvertValueType(ref error, box, TypeKind.Byte, Context.BYTE, ref stackslots[target.index]);
										break;
									case TypeKind.Short:
										ConvertValueType(ref error, box, TypeKind.Short, Context.SHORT, ref stackslots[target.index]);
										break;
									case TypeKind.UShort:
										ConvertValueType(ref error, box, TypeKind.UShort, Context.USHORT, ref stackslots[target.index]);
										break;
									case TypeKind.Int:
										ConvertValueType(ref error, box, TypeKind.Int, Context.INT, ref stackslots[target.index]);
										break;
									case TypeKind.Uint:
										ConvertValueType(ref error, box, TypeKind.Uint, Context.UINT, ref stackslots[target.index]);
										break;
									default:
#if DEBUG
										throw new InvalidOperationException();
#else
													Environment.FailFast("出错了，这里跑不到");return;
#endif
								}


								if (error.raised)
								{
									goto flag_handle_error;
								}

							}

							return;



						}
						else if (@class.Type_identifier == (ulong)TypeKind.Number)
						{
							if (argsCount == 0)
							{
								stackslots[target.index].SetNumber(0);
							}
							else if (argsCount >= 1)
							{
								byte* P = argementsPtr + sizeof(StackLocater) * (Context.NUMBER.Instance.Constructor.Parameters.Count - 1);
								StackLocater argLocater;
								LoadStackLocater(&argLocater, &P);

								NaNBoxing box = stackslots[argLocater.index];

								box = ToPrimitive(ref error, box, HINT.h_number, scope_ptr, target, target, stackslots, stackStPos, ((RtMethodScope)methodscope).ThisPtr);
								if (error.raised)
								{
									goto flag_handle_error;
								}

								ConvertValueType(ref error, box, TypeKind.Number, Context.NUMBER, ref stackslots[target.index]);
								if (error.raised)
								{
									goto flag_handle_error;
								}

							}

							return;
						}
						else if (@class.Type_identifier == (ulong)TypeKind.Function)
						{
							if (argsCount > 0)
							{
								RaiseArgumentErrorCountMisMatch(ref error, Context.FUNCTION.Instance.Constructor, 0, argsCount);
								goto flag_handle_error;
							}
							else
							{
								var function = Context.FUNCTION.Constructor;
								function.__ismethod = false;//function  的类型的 Constructor不会被调用，这里就暂借它作为new Function这种操作的 ASMethod

								var define = (ASInstance)Context.FUNCTION.Instance;

								int ptrIndex = stackStPos + target.index;
								int closurePtr = Context.M_ClosurePtr + ptrIndex;

								var closure = Context.GC.Heap[closurePtr];
								closure.Type = function.Body;
								((RtClosure)closure).ScopePtr = scope_ptr;
								((RtClosure)closure).ScopeType = scopeType;
								((RtClosure)closure).This.SetNull();
								((RtClosure)closure)._ref_as_type = define;
								((RtClosure)closure).methodscopeslot_ref_state = 0;
								((RtClosure)closure).HEAPINSTANCE_PTR = 0;
								stackslots[target.index].SetHeapPtr(closurePtr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);


								return;
							}
						}
						else
						{
							instancePtr.SetHeapPtr(Context.GC.AllocInstance(@class.Instance, out instance), (byte)RtHeapTypeKind.INSTANCE, (byte)(@class.Instance.Flags.HasFlag(ClassFlags.Struct) ? HeapKindFlag.FLAG_STRUCT : HeapKindFlag.NONE));
						}

						if (instancePtr.HeapPtr == 0)
						{
							//throw new NotImplementedException("out of memory");
							RaiseOutOfMemory(ref error);
							goto flag_handle_error;
						}

						stackslots[target.index] = instancePtr; //.SetHeapPtr(instancePtr);



					}


					//执行构造函数
					RunMethod(((ASInstance)instance.Type).Constructor, stackslots[target.index], instancePtr.HeapPtr, @class.Instance, (ushort)argsCount, argementsPtr, stackslots, ref error, -1, 0, true);
					if (error.raised)
					{
						goto flag_handle_error;
					}

					if (instancePtr.HeapKind == (byte)RtHeapTypeKind.VECTOR)
					{
						int vec_ptr = RtVector.FindAndUpdateHeapInstancePtr(instancePtr.HeapPtr, this, out RtVector t);
						stackslots[target.index].SetHeapPtr(vec_ptr, (byte)RtHeapTypeKind.VECTOR, (byte)HeapKindFlag.NONE);
					}

					else if (instancePtr.HeapKind == (byte)RtHeapTypeKind.ARRAY)
					{
						int arr_ptr = RtArray.FindAndUpdateHeapInstancePtr(instancePtr.HeapPtr, this, out RtArray t);
						stackslots[target.index].SetHeapPtr(arr_ptr, (byte)RtHeapTypeKind.ARRAY, (byte)HeapKindFlag.NONE);
					}
					else if (instancePtr.HeapKind == (byte)RtHeapTypeKind.INSTANCE && !instancePtr.IsStruct())
					{ 
						int obj_ptr = RtInstance.FindAndUpdateHeapInstancePtr(instancePtr.HeapPtr,this,out RtInstance t);
						stackslots[target.index].SetHeapPtr(obj_ptr, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.NONE);
					}

				}
				else if (type_box.HeapKind == (byte)RtHeapTypeKind.CLOSURE)
				{
					RtHeapBase type = Context.GC.Heap[type_box.HeapPtr];
					NaNBoxing constructor_box = GetSaveValue(type_box, ref error); //构造对象的函数，需要访问proto,所以只能先保存到堆里。
					if (error.raised)
					{
						goto flag_handle_error;
					}
					type_box = constructor_box; //.SetHeapPtr(constructor_box.HeapPtr);
					var constructor_closure = Context.GC.Heap[type_box.HeapPtr];

					if (((ASMethodBody)constructor_closure.Type).Method.__ismethod ||
						((ASMethodBody)constructor_closure.Type).Method.__is_buildin_proto ||
						constructor_closure.Type == Context.FUNCTION.Instance.Constructor.Body
						)
					{
						RaiseTypeError_RunMethodAsConstructor(ref error, ((ASMethodBody)constructor_closure.Type).Method);
						goto flag_handle_error;
					}



					var function_proto = ((RtClosure)constructor_closure).PROTOTYPE(this);

					if (function_proto == 0)
					{
						((RtClosure)constructor_closure).Set_PROTOTYPE(0, this);
						var proto = InvokeReadProperty(ref error, constructor_box, 0,  stackslots, -1);
						if (error.raised)
						{
							goto flag_handle_error;
						}
						function_proto = proto.HeapPtr;
					}
					///// AIR 运行时在检测到手工把prototype赋值为空的时候会又创建一个Object
					else if (function_proto == -1)
					{
						RtHeapBase proto;
						function_proto = Context.GC.AllocInstance(Context.OBJECT.Instance, out proto);
						if (function_proto == 0)
						{
							RaiseOutOfMemory(ref error);
							goto flag_handle_error;
						}

						((RtClosure)constructor_closure).Set_PROTOTYPE(function_proto, this);
					}

					if (Context.StackPosition >= Context.STACK_LENGTH)
					{
						RaiseStackOverflow(ref error);
						goto flag_handle_error;
					}

					Context.StackPosition++;

					//构造一个dynamic object
					//因为函数有可能返回值，因此这个dynamic object构造在刚给的那个槽上。
					//函数返回后，如果确认用这个对象，需要再把那个槽搬到target上！。

					int ptrIndex = Context.StackPosition - 1; //stackStPos + target.index;
					var instancePtr = Context.CacheInstancePtr + ptrIndex;

					var instance = Context.GC.Heap[instancePtr];
					instance.Type = Context.OBJECT.Instance;

					((RtInstance)instance).HEAPINSTANCE_PTR = 0;
					((RtInstance)instance).Set_PROPERTY_PTR(0, this, Context.OBJECT.Instance);
					((RtInstance)instance).Set_PROTOTYPE(function_proto, this);
					((RtInstance)instance).methodscopeslot_ref_state = 0;

					Context.StackSlots[ptrIndex].SetHeapPtr(instancePtr, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.NONE);


					var constructor = ((ASMethodBody)type.Type).Method;
					NaNBoxing ret_constructor = RunMethod(constructor, Context.StackSlots[ptrIndex],
						((RtClosure)constructor_closure).ScopePtr,
						((RtClosure)constructor_closure).ScopeType, (ushort)argsCount, argementsPtr, stackslots, ref error, stackStPos + target.index, type_box.HeapPtr, true);

					if (error.raised)
					{
						Context.StackPosition--;
						goto flag_handle_error;
					}

					bool move = true;
					if (ret_constructor.ValueType == BoxType.HeapPtr)
					{
						if (Context.GC.Heap[ret_constructor.HeapPtr].Kind == RtHeapTypeKind.STRING)
						{

						}
						else if (ret_constructor.HeapPtr == instancePtr) //原封不动返回，需要拷过来
						{

						}
						else
						{
							move = false;
							//提升到堆里, 这样优化时就不用考虑cache问题了。

							stackslots[target.index] = GetSaveValue(ret_constructor, ref error);
							if (error.raised)
							{
								Context.StackPosition--;
								goto flag_handle_error;
							}
						}
					}

					if (move) //使用前面的 ,移动过来 
					{
						if (((RtInstance)instance).HEAPINSTANCE_PTR == 0)
						{
							int target_index = stackStPos + target.index;
							var target_instancePtr = Context.CacheInstancePtr + target_index;

							var target_ins = Context.GC.Heap[target_instancePtr];
							target_ins.Type = Context.OBJECT.Instance;

							if (target_ins.Type._link_codescope.TypeLayout.Size != 0)
							{
#if DEBUG
								throw new InvalidOperationException();
#else
													Environment.FailFast("出错了，这里跑不到");  return;
#endif
							}

							((RtInstance)target_ins).HEAPINSTANCE_PTR = 0;
							((RtInstance)target_ins).methodscopeslot_ref_state = 0;
							((RtInstance)target_ins).CopyFrom(instance, this, 0);

							stackslots[target.index].SetHeapPtr(target_instancePtr, (byte)RtHeapTypeKind.INSTANCE, (byte)HeapKindFlag.NONE);
						}
						else
						{
							//这里只可能是在函数里被赋值到了其他变量，那么这时候跟踪到那个变量然后指过去。
							RtInstance src;
							int src_ptr = RtInstance.FindAndUpdateHeapInstancePtr(instancePtr, this, out src);

#if DEBUG
							if (!(src_ptr < Context.CacheInstancePtr + Context.STACK_LENGTH) //堆里
																							 //||
																							 //(src_ptr < Context.CacheInstancePtr + ((RtPayloadMethodScope)methodscope).StackPos +
																							 //((RtPayloadMethodScope)methodscope).SlotCount) //传入
									)
							{

							}
							else
							{
								// constructor前面已经保存到堆了。所以如果它把this保存到外面变量里，则外面的scope也肯定被保存到堆了。
								// 所以这里的object只能在堆里。
								throw new InvalidOperationException();
							}


#endif
							stackslots[target.index].SetHeapPtr(src_ptr, (byte)RtHeapTypeKind.INSTANCE, (byte)(((ASInstance)src.Type).Flags.HasFlag(ClassFlags.Struct) ? HeapKindFlag.FLAG_STRUCT : HeapKindFlag.NONE));
						}
					}


					Context.StackPosition--;
					//throw new NotImplementedException();
				}

				else
				{
#if DEBUG
					RtHeapBase type = Context.GC.Heap[type_box.HeapPtr];
					if (type.Kind == RtHeapTypeKind.MethodScope || type.Kind == RtHeapTypeKind.DYNAMIC_PROPERTYS
						||
						type.Kind == RtHeapTypeKind.STACK_CACHE_OBJ || type.Kind == RtHeapTypeKind.SHAPE
						)
					{
						throw new InvalidOperationException();
					}
#endif
					RaiseTypeError_Instantiation_non_constructor(ref error);
					goto flag_handle_error;
				}


			}
			else
			{
				RaiseTypeError_Instantiation_non_constructor(ref error);
				goto flag_handle_error;
			}

		flag_handle_error:
			;

		}




		private unsafe void Type_cast(int dst_index, byte** PC ,RtHeapBase methodscope, Span<NaNBoxing> constants,Span<NaNBoxing> stackslots,int stackStPos,int scope_ptr,ref ReceiveError error)
		{
			StackLocater value;
			int classid_index;
			LoadStackLocater(&value, PC);
			LoadInt32(&classid_index, PC);


			var boxing = constants[classid_index];
#if DEBUG
			if (boxing.ValueType != NaNBoxing.BoxType.Uint)
			{
				throw new InvalidOperationException();
			}
#endif
			var @class = Context.link_const_class[(int)boxing.UIntValue];
			var v = stackslots[value.index].HeapKind != (byte)RtHeapTypeKind.STACK_CACHE_OBJ ? stackslots[value.index] : LoadValue((RtStackCache)Context.GC.Heap[stackslots[value.index].HeapPtr], -1, ref error, stackslots, stackStPos + value.index);

			ExplicitConvert(ref error, 1, &value, stackslots, (TypeKind)@class.Type_identifier, @class, ref stackslots[dst_index], stackStPos + dst_index, scope_ptr, ((RtMethodScope)methodscope).ThisPtr, false);
			
		}

		private unsafe void Create_prop(int dst_index,byte** PC,Span<NaNBoxing> stackslots,ref ReceiveError error)
		{
			StackLocater instance;
			StackLocater key;
			StackLocater value;

			instance.index = dst_index;
			LoadStackLocater(&key, PC);
			LoadStackLocater(&value, PC);

			var ins_v = stackslots[instance.index];
			var key_v = stackslots[key.index];
			var value_v = stackslots[value.index];

#if DEBUG
			if (ins_v.ValueType != BoxType.HeapPtr) throw new InvalidOperationException();
			if (key_v.ValueType != BoxType.HeapPtr) throw new InvalidOperationException();
#endif
			var ins = Context.GC.Heap[ins_v.HeapPtr];

#if DEBUG
			var k = Context.GC.Heap[key_v.HeapPtr];
			if (ins.Kind != RtHeapTypeKind.INSTANCE) throw new InvalidOperationException();
			if (k.Kind != RtHeapTypeKind.STRING) throw new InvalidOperationException();
#endif
			CreateDynamic(ref error, ins, key_v, value_v, true, true, true);
			
		}

		private unsafe void Super_ctor(byte** PC, RtHeapBase methodscope ,Span<NaNBoxing> constants, Span<NaNBoxing> stackslots, int scope_ptr,ref ReceiveError error)
		{
			//执行基类构造函数

			int classid_index = 0;
			LoadInt32(&classid_index, PC);

			int argsCount;
			LoadInt32(&argsCount, PC);

			//StackLocater* argements = (StackLocater*)PC;
			//PC += argsCount * 4;
			//!!需要考虑对齐问题
			byte* argementsPtr = *PC;
			*PC += argsCount * 4;



			var boxing = constants[classid_index];
#if DEBUG
			if (boxing.ValueType != NaNBoxing.BoxType.Uint)
			{
				throw new InvalidOperationException();
			}
#endif

			var super_class = Context.link_const_class[(int)boxing.UIntValue];
			var ctor = super_class.Instance.Constructor;
			RunMethod(ctor, ((RtMethodScope)methodscope).ThisPtr, scope_ptr, (super_class).Instance, (ushort)argsCount, argementsPtr, stackslots, ref error, -1);
			
		}



		private unsafe void DELETE(int dst_index, byte** PC , Span<NaNBoxing> stackslots ,int stackStPos, ASMethod method ,   ref ReceiveError error)
		{
			{
				Span<char> frame_holdchars = stackalloc char[128];
				StackLocater* tmpArgLoc = stackalloc StackLocater[2];

				StackLocater stack;
				stack.index = dst_index;

				StackLocater todelete;
				LoadStackLocater(&todelete, PC);

				NaNBoxing box = stackslots[todelete.index];


				if (box.ValueType == NaNBoxing.BoxType.HeapPtr)
				{
					if (box.HeapKind == (byte)RtHeapTypeKind.STACK_CACHE_OBJ)
					{
						RtHeapBase rtHeap = Context.GC.Heap[box.HeapPtr];
						RtStackCache _obj = (RtStackCache)rtHeap;

						if (_obj.RefInstance.ValueType != BoxType.HeapPtr)
						{
							RaiseReferenceError_CanNotDeleteProperty(ref error, _obj.RefInstance);
							goto flag_handle_error;
							//throw new NotImplementedException();
						}
						else
						{
							var refObjKind = _obj.RefInstance.HeapKind;
							if (_obj.searchPropertyName.ValueType == BoxType.HeapPtr || _obj.searchPropertyName.ValueType == BoxType.LocalString) //动态属性
							{

								//string searchName = ((RtPayloadString)Context.GC.Heap[_obj.searchPropertyNamePtr]).Str;

								ReadOnlySpan<char> searchName = frame_holdchars;
								if (_obj.searchPropertyName.ValueType == BoxType.HeapPtr)
								{
									searchName = ((RtString)Context.GC.Heap[_obj.searchPropertyName.HeapPtr]).Str;
								}
								else
								{
									Span<char> temp = frame_holdchars; //stackalloc char[16];//用于从LocalString中提取值
									int l = _obj.searchPropertyName.GetLocalStringChars(temp);
									searchName = temp.Slice(0, l);
								}

								_obj.searchPropertyName.SetUndefined();

								NaNBoxing ns = new NaNBoxing();
								ASNamespace @namespace = null;
								if (_obj.searchNameSpacePtr > 0)
								{
									ns.SetHeapPtr(_obj.searchNameSpacePtr, (byte)RtHeapTypeKind.NAMESPACE, (byte)HeapKindFlag.NONE);
									RtHeapBase ns_instance = Context.GC.Heap[_obj.searchNameSpacePtr];
									@namespace = ((RtNameSpace)ns_instance).ASNamespace;
									_obj.searchNameSpacePtr = 0;
								}

								if (refObjKind == (byte)RtHeapTypeKind.INSTANCE
									&&
										(
											(((ASInstance)Context.GC.Heap[_obj.RefInstance.HeapPtr].Type).Flags & ClassFlags.Sealed) == ClassFlags.Sealed
											||
											(
												@namespace != null &&
												@namespace.Kind != NamespaceKind.Package
											)

										)
									)
								{
									//不可删除，返回false
									stackslots[stack.index].SetBoolean(false);
								}
								else if (refObjKind == (byte)RtHeapTypeKind.VECTOR)
								{
									//不可删除，返回false
									stackslots[stack.index].SetBoolean(false);
								}
								else if (refObjKind == (byte)RtHeapTypeKind.ARRAY &&
										((RtArray)Context.GC.Heap[_obj.RefInstance.HeapPtr]).isArguments()
										&& @namespace == null
										&& "callee".AsSpan().CompareTo(searchName, StringComparison.Ordinal) == 0
									)
								{
									Context.StackSlots[stackStPos - method.Body._link_codescope.Members.Count - 2].SetUndefined();
									stackslots[stack.index].SetBoolean(true);
								}
								else
								{
									RtHeapBase refObj = Context.GC.Heap[_obj.RefInstance.HeapPtr];
									NaNBoxing value; int shape_ptr; int index; RtDynamic prop;
									if (FindDynamicValue(refObj, searchName, out value, out shape_ptr, out index, out prop))
									{
										RtShape shape = (RtShape)Context.GC.Heap[shape_ptr];

										if (shape.Attribute.HasFlag(RtShape.PropertyAttribute.Configurable))
										{
											ChangeTranslation(prop, shape_ptr, ref error);
											if (error.raised)
											{
												goto flag_handle_error;
											}
											//从槽中移除此属性
											prop.Slots.RemoveAt(index);
											stackslots[stack.index].SetBoolean(true);
										}
										else
										{
											//不可删除返回false
											stackslots[stack.index].SetBoolean(false);
										}
									}
									else
									{
										//进入这里，肯定不能正常访问到成员，所以返回true
										stackslots[stack.index].SetBoolean(true);
									}
								}
							}

							else if (_obj.indexer_key.ValueType != BoxType.Fault)
							{
								if (refObjKind == (byte)RtHeapTypeKind.ARRAY)
								{
									RtHeapBase refObj = Context.GC.Heap[_obj.RefInstance.HeapPtr];
#if DEBUG
													if (_obj.indexer_key.ValueType == BoxType.Uint)
#endif
									{
										stackslots[stack.index].SetBoolean(((RtArray)refObj).Delete(_obj.indexer_key.UIntValue, this));
									}
#if DEBUG
													else
													{
														throw new InvalidOperationException();
													}
#endif
								}
								else
								{

#if DEBUG
													if (!(
														(refObjKind == (byte)RtHeapTypeKind.INSTANCE && ((ASInstance)Context.GC.Heap[_obj.RefInstance.HeapPtr].Type).Flags.HasFlag(ClassFlags.Indexer))
														||
														refObjKind == (byte)RtHeapTypeKind.VECTOR
														)
														)
													{
														throw new InvalidOperationException();
													}
#endif


									if (refObjKind == (byte)RtHeapTypeKind.VECTOR)
									{
										if (!RtVector.IsValidIndexType(_obj.indexer_key))
										{
											stackslots[stack.index].SetBoolean(false);
										}
										else
										{
											//throw new NotImplementedException();
											stackslots[stack.index].SetBoolean(true);
										}
									}
									else
									{
										RtHeapBase refObj = Context.GC.Heap[_obj.RefInstance.HeapPtr];

										if (Context.StackPosition + 2 >= Context.STACK_LENGTH)
										{
											RaiseStackOverflow(ref error);
											goto flag_handle_error;
										}

										var argSpan = Context.StackSlots.AsSpan(Context.StackPosition, 1);

										Context.StackPosition += 1;
										Context.GC.CheckGC(ref error);


										var indexer_key = GetSaveValue(_obj.indexer_key, ref error);
										if (error.raised)
										{
											Context.StackPosition -= 1;
											goto flag_handle_error;
										}

										argSpan[0] = indexer_key;

										tmpArgLoc[0].index = 0;


										NaNBoxing _this = new NaNBoxing();
										_this = _obj.RefInstance; //.SetHeapPtr(_obj.RefInstance.HeapPtr);

										NaNBoxing result = RunMethod(((ASInstance)refObj.Type).indexer_delete, _this,
											_obj.RefInstance.HeapPtr, refObj.Type, 1, (byte*)tmpArgLoc, argSpan, ref error, stackStPos + stack.index);

										Context.StackPosition -= 1;
										if (error.raised)
										{
											goto flag_handle_error;
										}
									}

								}
							}
							else if (_obj.trait[0].Kind == TraitKind.Slot || _obj.trait[0].Kind == TraitKind.Constant)
							{
								//不可删除，返回false
								stackslots[stack.index].SetBoolean(false);
							}
#if DEBUG
											else if (_obj.trait[0].Kind == TraitKind.Method && _obj.trait[1] == null)
											{
												throw new InvalidOperationException();
											}
#endif
							else if (_obj.trait[0].Kind == TraitKind.Getter)
							{
								stackslots[stack.index].SetBoolean(false);
							}
#if DEBUG
											else
											{
												throw new InvalidOperationException();
												//throw new NotImplementedException("方法的引用未实现");
											}
#endif
						}
					}
					else if (box.HeapKind == (byte)RtHeapTypeKind.CLOSURE)
					{
						//不可删除，返回false
						stackslots[stack.index].SetBoolean(false);
					}
					else if (box.HeapKind == (byte)RtHeapTypeKind.INSTANCE)
					{
						/*
						 * //CHECK#2
						 *  if (delete new Object() !== true) {
						 *	  throw new Error('#2: delete new Object() === true');
						 *	}
						 * */

						stackslots[stack.index].SetBoolean(true);
					}
#if DEBUG
									else
									{
										throw new InvalidOperationException();
									}
#endif
				}
				else
				{
					//直接返回true
					stackslots[stack.index].SetBoolean(true);
				}

			}

		flag_handle_error:
			;

		}




		private unsafe void GET_IN(int dst_index,byte** PC, Span<NaNBoxing> stackslots, int stackStPos, int scope_ptr , RtHeapBase methodscope,ref ReceiveError error)
		{
			StackLocater dst;
			StackLocater v1;
			StackLocater v2;

			dst.index = dst_index;
			LoadStackLocater(&v1, PC);
			LoadStackLocater(&v2, PC);



			var name_v = stackslots[v1.index];
			NaNBoxing name_n = ToPrimitive(ref error, name_v, HINT.h_string, scope_ptr, dst, dst, stackslots, stackStPos, ((RtMethodScope)methodscope).ThisPtr);
			if (error.raised)
			{
				goto flag_handle_error;
			}

			Span<char> buffers = stackalloc char[128];
			ReadOnlySpan<char> name = Extensions.GetPrimitiveValueToString(this, name_n, buffers);

			var type = stackslots[v2.index];
			bool isvaluebox = false;
			if (type.ValueType != BoxType.HeapPtr)
			{
				switch (type.ValueType)
				{
					case BoxType.Number:
						type.SetHeapPtr(Context.NUMBER.__instance_index__, (byte)RtHeapTypeKind.CLASS, (byte)HeapKindFlag.NONE); isvaluebox = true;
						break;
					case BoxType.Undefined:
						RaiseTypeError_ATermUndefined(ref error);
						goto flag_handle_error;
					case BoxType.Null:
						RaiseTypeError_AccessNull(ref error);
						goto flag_handle_error;
					case BoxType.Boolean:
						type.SetHeapPtr(Context.BOOLEAN.__instance_index__, (byte)RtHeapTypeKind.CLASS, (byte)HeapKindFlag.NONE); isvaluebox = true;
						break;
					case BoxType.Int:
						type.SetHeapPtr(Context.INT.__instance_index__, (byte)RtHeapTypeKind.CLASS, (byte)HeapKindFlag.NONE); isvaluebox = true;
						break;
					case BoxType.Uint:
						type.SetHeapPtr(Context.UINT.__instance_index__, (byte)RtHeapTypeKind.CLASS, (byte)HeapKindFlag.NONE); isvaluebox = true;
						break;
					case BoxType.Sbyte:
						type.SetHeapPtr(Context.SBYTE.__instance_index__, (byte)RtHeapTypeKind.CLASS, (byte)HeapKindFlag.NONE); isvaluebox = true;
						break;
					case BoxType.Byte:
						type.SetHeapPtr(Context.BYTE.__instance_index__, (byte)RtHeapTypeKind.CLASS, (byte)HeapKindFlag.NONE); isvaluebox = true;
						break;
					case BoxType.Short:
						type.SetHeapPtr(Context.SHORT.__instance_index__, (byte)RtHeapTypeKind.CLASS, (byte)HeapKindFlag.NONE); isvaluebox = true;
						break;
					case BoxType.UShort:
						type.SetHeapPtr(Context.USHORT.__instance_index__, (byte)RtHeapTypeKind.CLASS, (byte)HeapKindFlag.NONE); isvaluebox = true;
						break;
					case BoxType.Float:
						type.SetHeapPtr(Context.FLOAT.__instance_index__, (byte)RtHeapTypeKind.CLASS, (byte)HeapKindFlag.NONE); isvaluebox = true;
						break;
					case BoxType.HeapPtr:
					case BoxType.Fault:
					default:
						break;
				}
			}

			var find =
				(ASContainer type, ReadOnlySpan<char> name, int proto) =>
				{
					if (ObjectImpl.Find_ASContainer_Prop(type, name))
					{
						return true;
					}
					else
					{
						int steps = 0;
						while (proto != 0 && steps < 32)
						{
							var proto_obj = Context.GC.Heap[proto];

							if (proto_obj.Kind != RtHeapTypeKind.VECTOR)
							{
								NaNBoxing value; int shape; int matchslot; RtDynamic prop;
								if (FindDynamicValue(proto_obj, name, out value, out shape, out matchslot, out prop))
								{
									return true;
								}
							}
							proto = GetProtoPtr(proto_obj);
							steps++;
						}
						return false;
					}
				};



			var obj = Context.GC.Heap[type.HeapPtr];
			switch (obj.Kind)
			{
				case RtHeapTypeKind.CLASS:
					{
						var @class = ((RtScriptClass)obj).Meta;
						if (find(@class, name, 0) || find(((ASClass)@class).Instance, name, 0))
						{
							stackslots[dst.index].SetBoolean(true);
						}
						else if (!isvaluebox) // "F" in Number  ,proto是Class
						{
							NaNBoxing value; int shape; int matchslot; RtDynamic prop;
							if (FindDynamicValue(obj, name, out value, out shape, out matchslot, out prop))
							{
								stackslots[dst.index].SetBoolean(true);
							}
							else
							{
								bool found = false;

								int proto = Context.CLASS.__instance_index__;
								int steps = 0;
								while (proto != 0 && steps < 32)
								{
									var proto_obj = Context.GC.Heap[proto];

									if (FindDynamicValue(proto_obj, name, out value, out shape, out matchslot, out prop))
									{
										found = true;
										break;
									}
									proto = GetProtoPtr(proto_obj);
									steps++;
								}
								stackslots[dst.index].SetBoolean(found);
							}
						}
						else // Number.prototype["F"]=1; "F" in 33.0  这种
						{
							bool found = false;
							int proto = ((RtScriptClass)obj).PROTO__PTR;
							int steps = 0;
							while (proto != 0 && steps < 32)
							{
								var proto_obj = Context.GC.Heap[proto];
								NaNBoxing value; int shape; int matchslot; RtDynamic prop;
								if (FindDynamicValue(proto_obj, name, out value, out shape, out matchslot, out prop))
								{
									found = true;
									break;
								}
								proto = GetProtoPtr(proto_obj);
								steps++;
							}
							stackslots[dst.index].SetBoolean(found);

						}

						break;
					}
				case RtHeapTypeKind.GLOBAL:
					{
						stackslots[dst.index].SetBoolean(find(Context.OBJECT.Instance, name, type.HeapPtr));
					}
					break;
				case RtHeapTypeKind.STRING:
					{
						stackslots[dst.index].SetBoolean(find(Context.STRING.Instance, name, type.HeapPtr));
					}
					break;
				case RtHeapTypeKind.INSTANCE:
					{
						if (((ASInstance)obj.Type).indexer_get != null)
						{
							if (Context.StackPosition + 1 >= Context.STACK_LENGTH)
							{
								RaiseStackOverflow(ref error);
								goto flag_handle_error;
							}

							var argSpan = Context.StackSlots.AsSpan(Context.StackPosition, 1);
							argSpan[0] = name_n;
							StackLocater argLoc = new StackLocater() { index = 0 };

							Context.StackPosition++;

							NaNBoxing _this = type;

							NaNBoxing find_by_index = default;

							unsafe
							{
								Context.StackPosition++;

								RunMethod(((ASInstance)obj.Type).indexer_get, _this,
									type.HeapPtr, obj.Type, 1, (byte*)&argLoc, argSpan, ref error, Context.StackPosition - 1);
								find_by_index = Context.StackSlots[Context.StackPosition - 1];
								Context.StackPosition--;
							}

							Context.StackPosition--;
							if (error.raised)
							{
								goto flag_handle_error;
							}

							if (find_by_index.ValueType == BoxType.Fault)
							{
								stackslots[dst.index].SetBoolean(find(obj.Type, name, type.HeapPtr));
							}
							else
							{
								stackslots[dst.index].SetBoolean(true);
							}

						}
						else
						{
							stackslots[dst.index].SetBoolean(find(obj.Type, name, type.HeapPtr));
						}
					}
					break;
				case RtHeapTypeKind.NAMESPACE:
					{
						stackslots[dst.index].SetBoolean(find(Context.NAMESPACE.Instance, name, type.HeapPtr));
					}
					break;
				case RtHeapTypeKind.ARRAY:
					{
						Debug.Assert(name_n.ValueType != BoxType.Fault);

						uint index;

						switch (name_n.ValueType)
						{
							case BoxType.Number:
								if (Math.Truncate(name_n.Number) == name_n.Number && name_n.Number >= 0 && name_n.Number < uint.MaxValue)
								{
									index = (uint)name_n.Number;
								}
								else
								{
									goto lbl_find_name;
								}
								break;
							case BoxType.Int:
								if (name_n.IntValue >= 0)
								{
									index = (uint)name_n.IntValue;
								}
								else
								{
									goto lbl_find_name;
								}
								break;
							case BoxType.Uint:
								index = name_n.UIntValue;
								break;
							case BoxType.Sbyte:
								if (name_n.SByteValue >= 0)
								{
									index = (uint)name_n.SByteValue;
								}
								else
								{
									goto lbl_find_name;
								}
								break;
							case BoxType.Byte:
								index = name_n.ByteValue;
								break;
							case BoxType.Short:
								if (name_n.ShortValue >= 0)
								{
									index = (uint)name_n.ShortValue;
								}
								else
								{
									goto lbl_find_name;
								}
								break;
							case BoxType.UShort:
								index = name_n.UShortValue;
								break;
							case BoxType.Float:

								if (MathF.Truncate(name_n.FloatValue) == name_n.FloatValue && name_n.FloatValue >= 0 && name_n.FloatValue < uint.MaxValue)
								{
									index = (uint)name_n.FloatValue;
								}
								else
								{
									goto lbl_find_name;
								}

								break;

							case BoxType.Undefined:
							case BoxType.Null:
							case BoxType.Boolean:
							case BoxType.HeapPtr:
							default:
								goto lbl_find_name;

						}

						bool isoutofindex_or_ishole;
						NaNBoxing result = LoadSlotFromArray(index, obj, out isoutofindex_or_ishole);

						if (result.ValueType != BoxType.Fault)
						{
							stackslots[dst.index].SetBoolean(true);
						}
						else
						{
							stackslots[dst.index].SetBoolean(false);
						}

						break;

					lbl_find_name:

						stackslots[dst.index].SetBoolean(find(Context.ARRAY.Instance, name, type.HeapPtr));

					}
					break;
				case RtHeapTypeKind.VECTOR:
					{

						int index;
						if (((RtVector)obj).GetStore(this).IsValidIndexRange(name_n, out index))
						{
							stackslots[dst.index].SetBoolean(true);
						}
						else if (IsNumeric(name_n))
						{
							stackslots[dst.index].SetBoolean(false);
						}
						else
						{
							stackslots[dst.index].SetBoolean(find(obj.Type, name, type.HeapPtr));
						}

					}
					break;
				case RtHeapTypeKind.CLOSURE:
					{
						stackslots[dst.index].SetBoolean(find(Context.FUNCTION.Instance, name, type.HeapPtr));
					}
					break;
				case RtHeapTypeKind.STACK_CACHE_OBJ:
				case RtHeapTypeKind.DYNAMIC_PROPERTYS:
				case RtHeapTypeKind.SHAPE:
				case RtHeapTypeKind.MethodScope:
				default:
#if DEBUG
					throw new InvalidOperationException();
#else
										Environment.FailFast("出错了，这里跑不到");
										stackslots[dst.index].SetBoolean(false);
										break;
#endif
			}

		flag_handle_error:
			;


		}

		private unsafe void GET_INSTANCEOF( int dst_index,byte** PC , Span<NaNBoxing> stackslots, 
			ref ReceiveError error
			)
		{
			StackLocater dst;
			StackLocater v1;
			StackLocater v2;

			dst.index = dst_index;
			LoadStackLocater(&v1, PC);
			LoadStackLocater(&v2, PC);




			do
			{


				var type = stackslots[v2.index];
				if (type.ValueType != BoxType.HeapPtr)
				{
					RaiseTypeError_InstanceOf(ref error);
					goto flag_handle_error;
				}

				int fun_proto;
				int o_proto;


				if (type.HeapKind == (byte)RtHeapTypeKind.CLASS)
				{
					var type_instance = Context.GC.Heap[type.HeapPtr];
					var @typeclass = (ASClass)((RtScriptClass)type_instance).Meta;
					if (typeclass.Instance.Flags.HasFlag(ClassFlags.NoConstructor) && !typeclass.Instance.IsInterface)
					{
						RaiseTypeError_InstanceOf(ref error);
						goto flag_handle_error;
					}


					var v = stackslots[v1.index];

					switch (v.ValueType)
					{

						case BoxType.Undefined:
						case BoxType.Null:
							stackslots[dst.index].SetBoolean(false);
							break;
						case BoxType.Boolean:
							stackslots[dst.index].SetBoolean(typeclass == Context.BOOLEAN || typeclass == Context.OBJECT);
							break;
						case BoxType.Number:
						case BoxType.Int:
						case BoxType.Uint:
						case BoxType.Sbyte:
						case BoxType.Byte:
						case BoxType.Short:
						case BoxType.UShort:
						case BoxType.Float:
							stackslots[dst.index].SetBoolean(Is(v, typeclass)); // 已改为按数值范围处理
							break;
						case BoxType.LocalString:
							// LocalString应该被视为String类型
							stackslots[dst.index].SetBoolean(typeclass == Context.STRING || typeclass == Context.OBJECT);
							break;
						case BoxType.HeapPtr:
							{

								switch ((RtHeapTypeKind)v.HeapKind)
								{
									case RtHeapTypeKind.CLASS:
										stackslots[dst.index].SetBoolean(typeclass == Context.OBJECT || typeclass == Context.CLASS);
										break;
									case RtHeapTypeKind.GLOBAL:
										stackslots[dst.index].SetBoolean(typeclass == Context.OBJECT);
										break;
									case RtHeapTypeKind.STRING:
										stackslots[dst.index].SetBoolean(typeclass == Context.STRING || typeclass == Context.OBJECT);
										break;
									case RtHeapTypeKind.INSTANCE:
										{
											var v_instance = Context.GC.Heap[v.HeapPtr];
											bool pass = typeclass == Context.OBJECT ||
												v_instance.Type == typeclass.Instance ||
												Extensions.IsExtend((ASInstance)v_instance.Type, typeclass.Instance) ||
												Extensions.IsImplements((ASInstance)v_instance.Type, typeclass.Instance);

											if (pass || ((ASInstance)v_instance.Type).Flags.HasFlag(ClassFlags.Sealed))
											{
												stackslots[dst.index].SetBoolean(
													pass
													);
											}
											else
											{
												o_proto = ((RtInstance)v_instance).PROTOTYPE(this, (ASInstance)v_instance.Type);
												fun_proto = ((RtScriptClass)Context.GC.Heap[typeclass.__instance_index__]).PROTO__PTR;
												goto lbl_do_proto;
											}
										}
										break;
									case RtHeapTypeKind.NAMESPACE:
										stackslots[dst.index].SetBoolean(typeclass == Context.OBJECT || ((ASClass)typeclass).Type_identifier == (ulong)TypeKind.Namespace);
										break;
									case RtHeapTypeKind.ARRAY:
										stackslots[dst.index].SetBoolean(typeclass == Context.OBJECT || typeclass == Context.ARRAY);
										break;
									case RtHeapTypeKind.VECTOR:
										{
											if (typeclass == Context.OBJECT || typeclass == Context.VECTOR)
											{
												stackslots[dst.index].SetBoolean(true);
												break;
											}

											if (typeclass.Instance.Flags.HasFlag(ClassFlags.Vector))
											{
												if (typeclass.Instance._element_class == null || typeclass.Instance._element_class == Context.OBJECT)
												{
													stackslots[dst.index].SetBoolean(true);
													break;
												}
												var v_instance = Context.GC.Heap[v.HeapPtr];
												if (((RtVector)v_instance).element_asclass == typeclass.Instance._element_class)
												{
													stackslots[dst.index].SetBoolean(true);
													break;
												}
											}

										}

										stackslots[dst.index].SetBoolean(false);

										//stackslots[dst.index].SetBoolean(typeclass == Context.OBJECT || typeclass.Instance == v_instance.Type );
										//throw new NotImplementedException();
										break;
									case RtHeapTypeKind.CLOSURE:
										stackslots[dst.index].SetBoolean(typeclass == Context.OBJECT || typeclass == Context.FUNCTION);
										break;
#if DEBUG
									case RtHeapTypeKind.STACK_CACHE_OBJ:
									case RtHeapTypeKind.DYNAMIC_PROPERTYS:
									case RtHeapTypeKind.SHAPE:
									case RtHeapTypeKind.MethodScope:
									default:
										throw new InvalidOperationException();
#endif
								}


							}

							break;
#if DEBUG
						case BoxType.Fault:
						default:
							throw new InvalidOperationException();
#endif
					}



				}
				else if (type.HeapKind == (byte)RtHeapTypeKind.CLOSURE)
				{
					var v = stackslots[v1.index];
					if (IsPrimitive(v))
					{
						stackslots[dst.index].SetBoolean(false);
						break;
					}
#if DEBUG
					if (v.ValueType != BoxType.HeapPtr)
						throw new InvalidOperationException();
#endif

					if (v.HeapKind != (byte)RtHeapTypeKind.INSTANCE)
					{
						stackslots[dst.index].SetBoolean(false);
						break;
					}
					var obj = Context.GC.Heap[v.HeapPtr];
					if (((ASInstance)obj.Type).Flags.HasFlag(ClassFlags.Sealed))
					{
						stackslots[dst.index].SetBoolean(false);
						break;
					}

					var type_instance = Context.GC.Heap[type.HeapPtr];
					int obj_proto = ((RtInstance)obj).PROTOTYPE(this, (ASInstance)obj.Type);

					int proto_ptr;
					if (((ASMethodBody)type_instance.Type).Method.__ismethod)
					{
						proto_ptr = ((RtScriptClass)Context.GC.Heap[Context.METHOD_CLOSURE.__instance_index__]).PROTO__PTR;
					}
					else
					{
						proto_ptr = ((RtClosure)type_instance).PROTOTYPE(this);
						if (proto_ptr <= 0) //默认，指向FUNCTION的proto
						{
							//按test262,此处应该跑TypeError(Function has non-object prototype 'undefined' in instanceof check)
							//RaiseTypeError_InstanceOf(ref error);
							//goto flag_handle_error;
							proto_ptr = ((RtScriptClass)Context.GC.Heap[Context.FUNCTION.__instance_index__]).PROTO__PTR;
							if (proto_ptr <= 0) //Function.prototype是一个function (){},所以如果还是空白的，就跳到Object.proto里去。
							{
								proto_ptr = ((RtScriptClass)Context.GC.Heap[Context.OBJECT.__instance_index__]).PROTO__PTR;
							}
						}
					}

					fun_proto = proto_ptr;
					o_proto = obj_proto;

					goto lbl_do_proto;
				}
				else
				{
					RaiseTypeError_InstanceOf(ref error);
					goto flag_handle_error;
				}

				break;

			lbl_do_proto:
				{
					RtHeapBase obj;

					bool instanceof = false;
					int steps = 0;
					while (o_proto != 0 && steps++ < 32)
					{
						if (o_proto == fun_proto)
						{
							instanceof = true;
							break;
						}
						else
						{
							obj = Context.GC.Heap[o_proto];
							o_proto = GetProtoPtr(obj);

						}
					}

					stackslots[dst.index].SetBoolean(instanceof);

				}
			}
			while (false);

		flag_handle_error:
			;


		}



		private unsafe void Ld_method(int dst_index,byte** PC, RtHeapBase methodscope,  Span<NaNBoxing> stackslots,int scope_ptr,int stackStPos ,  ref ReceiveError error)
		{
			StackLocater target;
			target.index = dst_index;

			StackLocater instance;
			LoadStackLocater(&instance, PC);

			uint vtable_ = 0;
			LoadUInt(&vtable_, PC);
			ushort vtable_index = (ushort)vtable_;


			NaNBoxing thisValue;
			if (instance.index >= 0)
			{
				thisValue = stackslots[instance.index];
			}
			else
			{
				var o = methodscope; //Context.GC.Heap[scope_ptr];
									 //int instancePtr = scope_ptr;
				NaNBoxing instancePtr = default; instancePtr.SetHeapPtr(scope_ptr, (byte)o.Kind, (byte)(o.Kind == RtHeapTypeKind.INSTANCE ? (((ASInstance)o.Type).Flags.HasFlag(ClassFlags.Struct) ? HeapKindFlag.FLAG_STRUCT : HeapKindFlag.NONE) : HeapKindFlag.NONE));
				do
				{
					if (o.Kind == RtHeapTypeKind.MethodScope)
					{
						RtMethodScope rtPayload = (RtMethodScope)o;
						o = Context.GC.Heap[rtPayload.ParentPtr];
						instancePtr.SetHeapPtr(rtPayload.ParentPtr, (byte)o.Kind, (byte)(o.Kind == RtHeapTypeKind.INSTANCE ? (((ASInstance)o.Type).Flags.HasFlag(ClassFlags.Struct) ? HeapKindFlag.FLAG_STRUCT : HeapKindFlag.NONE) : HeapKindFlag.NONE)); //= rtPayload.ParentPtr;
					}
					else
					{
						break;
					}

				} while (true);
				thisValue = new NaNBoxing();
				thisValue = instancePtr; //.SetHeapPtr(instancePtr);
			}


			if (thisValue.ValueType == NaNBoxing.BoxType.Null)
			{
				RaiseTypeError_AccessNull(ref error);
				goto flag_handle_error;
			}

			if (thisValue.ValueType != NaNBoxing.BoxType.HeapPtr)
			{
				ASInstance @instacne = null;
				switch (thisValue.ValueType)
				{
					case BoxType.Number:
						instacne = Context.NUMBER.Instance;
						break;
					case BoxType.Boolean:
						instacne = Context.BOOLEAN.Instance;
						break;
					case BoxType.Int:
						instacne = Context.INT.Instance;
						break;
					case BoxType.Uint:
						instacne = Context.UINT.Instance;
						break;
					case BoxType.Sbyte:
						instacne = Context.SBYTE.Instance;
						break;
					case BoxType.Byte:
						instacne = Context.BYTE.Instance;
						break;
					case BoxType.Short:
						instacne = Context.SHORT.Instance;
						break;
					case BoxType.UShort:
						instacne = Context.USHORT.Instance;
						break;
					case BoxType.Float:
						instacne = Context.FLOAT.Instance;
						break;
					case BoxType.LocalString:
						instacne = Context.STRING.Instance;
						break;
				}

				Debug.Assert(instacne != null);

				var vtableitem = instacne._vtable.Items[vtable_index];
				var function = vtableitem.Trait.Method;

				var define = (ASInstance)vtableitem.DefineAt;

				int ptrIndex = stackStPos + target.index;
				int closurePtr = Context.M_ClosurePtr + ptrIndex;

				var closure = Context.GC.Heap[closurePtr];
				closure.Type = function.Body;
				((RtClosure)closure).ScopePtr = 0;////thisValue.HeapPtr;
				((RtClosure)closure).ScopeType = define;
				((RtClosure)closure).This = thisValue;
				((RtClosure)closure)._ref_as_type = define;
				((RtClosure)closure).methodscopeslot_ref_state = 0;
				((RtClosure)closure).HEAPINSTANCE_PTR = 0;
				stackslots[target.index].SetHeapPtr(closurePtr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);

			}
			else
			{

				if (thisValue.HeapKind == (byte)RtHeapTypeKind.INSTANCE || thisValue.HeapKind == (byte)RtHeapTypeKind.VECTOR)
				{
					RtHeapBase ins = Context.GC.Heap[thisValue.HeapPtr];
					var vtableitem = ins.Type._vtable.Items[vtable_index];
					var function = vtableitem.Trait.Method;

					var define = (ASInstance)vtableitem.DefineAt;

					int ptrIndex = stackStPos + target.index;
					int closurePtr = Context.M_ClosurePtr + ptrIndex;

					var closure = Context.GC.Heap[closurePtr];
					closure.Type = function.Body;
					((RtClosure)closure).ScopePtr = thisValue.HeapPtr;
					((RtClosure)closure).ScopeType = define;
					((RtClosure)closure).This = thisValue;
					((RtClosure)closure)._ref_as_type = define;
					((RtClosure)closure).methodscopeslot_ref_state = 0;
					((RtClosure)closure).HEAPINSTANCE_PTR = 0;
					stackslots[target.index].SetHeapPtr(closurePtr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);
				}
				else if (thisValue.HeapKind == (byte)RtHeapTypeKind.STRING)
				{
					var vtableitem = Context.STRING.Instance._vtable.Items[vtable_index];
					var function = vtableitem.Trait.Method;
					var define = (ASInstance)vtableitem.DefineAt;

					int ptrIndex = stackStPos + target.index;
					int closurePtr = Context.M_ClosurePtr + ptrIndex;

					var closure = Context.GC.Heap[closurePtr];
					closure.Type = function.Body;
					((RtClosure)closure).ScopePtr = thisValue.HeapPtr;
					((RtClosure)closure).ScopeType = define;
					((RtClosure)closure).This = thisValue;
					((RtClosure)closure)._ref_as_type = define;
					((RtClosure)closure).methodscopeslot_ref_state = 0;
					((RtClosure)closure).HEAPINSTANCE_PTR = 0;
					stackslots[target.index].SetHeapPtr(closurePtr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);
				}
				else if (thisValue.HeapKind == (byte)RtHeapTypeKind.CLASS)
				{
					RtHeapBase ins = Context.GC.Heap[thisValue.HeapPtr];
					var @class = ((RtScriptClass)ins).Meta;
					var function = @class._vtable.Items[vtable_index].Trait.Method;

					int ptrIndex = stackStPos + target.index;
					int closurePtr = Context.M_ClosurePtr + ptrIndex;

					var closure = Context.GC.Heap[closurePtr];
					closure.Type = function.Body;
					((RtClosure)closure).ScopePtr = thisValue.HeapPtr;
					((RtClosure)closure).ScopeType = @class;
					((RtClosure)closure)._ref_as_type = @class;
					((RtClosure)closure).This = thisValue;
					((RtClosure)closure).methodscopeslot_ref_state = 0;
					((RtClosure)closure).HEAPINSTANCE_PTR = 0;

					stackslots[target.index].SetHeapPtr(closurePtr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);
				}
				else if (thisValue.HeapKind == (byte)RtHeapTypeKind.CLOSURE)
				{
					var function = Context.FUNCTION.Instance._vtable.Items[vtable_index].Trait.Method;

					int ptrIndex = stackStPos + target.index;
					int closurePtr = Context.M_ClosurePtr + ptrIndex;

					var closure = Context.GC.Heap[closurePtr];
					closure.Type = function.Body;
					((RtClosure)closure).ScopePtr = thisValue.HeapPtr;
					((RtClosure)closure).ScopeType = Context.FUNCTION.Instance;
					((RtClosure)closure).This = thisValue;
					((RtClosure)closure)._ref_as_type = Context.FUNCTION.Instance;
					((RtClosure)closure).methodscopeslot_ref_state = 0;
					((RtClosure)closure).HEAPINSTANCE_PTR = 0;
					stackslots[target.index].SetHeapPtr(closurePtr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);
				}
				else if (thisValue.HeapKind == (byte)RtHeapTypeKind.ARRAY)
				{
					var function = Context.ARRAY.Instance._vtable.Items[vtable_index].Trait.Method;

					int ptrIndex = stackStPos + target.index;
					int closurePtr = Context.M_ClosurePtr + ptrIndex;

					var closure = Context.GC.Heap[closurePtr];
					closure.Type = function.Body;
					((RtClosure)closure).ScopePtr = thisValue.HeapPtr;
					((RtClosure)closure).ScopeType = Context.ARRAY.Instance;
					((RtClosure)closure).This = thisValue;
					((RtClosure)closure)._ref_as_type = Context.ARRAY.Instance;
					((RtClosure)closure).methodscopeslot_ref_state = 0;
					((RtClosure)closure).HEAPINSTANCE_PTR = 0;
					stackslots[target.index].SetHeapPtr(closurePtr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);
				}
#if DEBUG
				else
				{
					throw new InvalidOperationException();
				}
#endif
			}


		flag_handle_error:
			;

		}



		private unsafe void Read_property(int dst_index,byte** PC, RtHeapBase methodscope, Span<NaNBoxing> stackslots,int stackStPos, int scope_ptr,ref ReceiveError error )
		{
			StackLocater target;
			target.index = dst_index;

			StackLocater instance;
			LoadStackLocater(&instance, PC);

			uint vtable_ = 0;
			LoadUInt(&vtable_, PC);
			ushort vtable_index = (ushort)vtable_;

			NaNBoxing thisValue;
			if (instance.index >= 0)
			{
				thisValue = stackslots[instance.index];
			}
			else
			{
				var o = methodscope; //Context.GC.Heap[scope_ptr];
									 //int instancePtr = scope_ptr;
				NaNBoxing instancePtr = default; instancePtr.SetHeapPtr(scope_ptr, (byte)o.Kind, (byte)(o.Kind == RtHeapTypeKind.INSTANCE ? (((ASInstance)o.Type).Flags.HasFlag(ClassFlags.Struct) ? HeapKindFlag.FLAG_STRUCT : HeapKindFlag.NONE) : HeapKindFlag.NONE));
				do
				{
					if (o.Kind == RtHeapTypeKind.MethodScope)
					{
						RtMethodScope rtPayload = (RtMethodScope)o;
						o = Context.GC.Heap[rtPayload.ParentPtr];
						instancePtr.SetHeapPtr(rtPayload.ParentPtr, (byte)o.Kind, (byte)(o.Kind == RtHeapTypeKind.INSTANCE ? (((ASInstance)o.Type).Flags.HasFlag(ClassFlags.Struct) ? HeapKindFlag.FLAG_STRUCT : HeapKindFlag.NONE) : HeapKindFlag.NONE)); //= rtPayload.ParentPtr;
					}
					else
					{
						break;
					}

				} while (true);
				thisValue = new NaNBoxing();
				thisValue = instancePtr; //.SetHeapPtr(instancePtr);
			}

			if (thisValue.ValueType == NaNBoxing.BoxType.Null)
			{
				RaiseTypeError_AccessNull(ref error);
				goto flag_handle_error;
			}

			NaNBoxing result = InvokeReadProperty(ref error, thisValue, vtable_index,  stackslots, stackStPos + target.index);
			if (error.raised)
			{
				goto flag_handle_error;
			}

			stackslots[target.index] = result;

		flag_handle_error:
			;

		}


		private unsafe void Read_property_interface(int dst_index, byte** PC, RtHeapBase methodscope, Span<NaNBoxing> constants, Span<NaNBoxing> stackslots, int stackStPos, ref ReceiveError error)
		{
			StackLocater target;
			target.index = dst_index;

			StackLocater instance;
			LoadStackLocater(&instance, PC);

			int class_id = 0;
			LoadInt32(&class_id, PC);

			uint vtable_ = 0;
			LoadUInt(&vtable_, PC);
			ushort vtable_index = (ushort)vtable_;


			Debug.Assert(instance.index >= 0);

			NaNBoxing thisValue = stackslots[instance.index];


			if (thisValue.ValueType == NaNBoxing.BoxType.Null)
			{
				RaiseTypeError_AccessNull(ref error);
				goto flag_handle_error;
			}

			Debug.Assert(thisValue.HeapKind == (byte)RtHeapTypeKind.INSTANCE);


			{
				RtHeapBase ins = Context.GC.Heap[thisValue.HeapPtr];
				Debug.Assert(ins.Kind == RtHeapTypeKind.INSTANCE);

				var boxing = constants[class_id];

				Debug.Assert(boxing.ValueType == NaNBoxing.BoxType.Uint);


				var @class = Context.link_const_class[(int)boxing.UIntValue];

				Debug.Assert(@class.Instance.IsInterface);



				int m_idx = ((ASInstance)ins.Type)._interface_impl_.First((i) => i.interface_type == @class.Type_identifier)[vtable_index];
				var vtableitem = ins.Type._vtable.Items[m_idx];

				var function = vtableitem.Trait.Method;
				var define = (ASInstance)vtableitem.DefineAt;

				NaNBoxing result = RunMethod(function,
				thisValue, thisValue.HeapPtr, define, 0, null, stackslots, ref error, stackStPos + target.index);

				if (error.raised)
				{
					goto flag_handle_error;
				}

				stackslots[target.index] = result;


			}

		flag_handle_error:
			;
		}


		private unsafe void Write_property(int dst_index, byte** PC, RtHeapBase methodscope, Span<NaNBoxing> stackslots, int stackStPos,int scope_ptr, ref ReceiveError error)
		{
			StackLocater valueLoc;
			valueLoc.index = dst_index;

			StackLocater instance;
			LoadStackLocater(&instance, PC);

			uint vtable_ = 0;
			LoadUInt(&vtable_, PC);
			ushort vtable_index = (ushort)vtable_;

			void* argementsPtr = &valueLoc;

			NaNBoxing thisValue;
			if (instance.index >= 0)
			{
				thisValue = stackslots[instance.index];
			}
			else
			{
				var o = methodscope; //Context.GC.Heap[scope_ptr];
									 //int instancePtr = scope_ptr;
				NaNBoxing instancePtr = default; instancePtr.SetHeapPtr(scope_ptr, (byte)o.Kind, (byte)(o.Kind == RtHeapTypeKind.INSTANCE ? (((ASInstance)o.Type).Flags.HasFlag(ClassFlags.Struct) ? HeapKindFlag.FLAG_STRUCT : HeapKindFlag.NONE) : HeapKindFlag.NONE));
				do
				{
					if (o.Kind == RtHeapTypeKind.MethodScope)
					{
						RtMethodScope rtPayload = (RtMethodScope)o;
						o = Context.GC.Heap[rtPayload.ParentPtr];
						instancePtr.SetHeapPtr(rtPayload.ParentPtr, (byte)o.Kind, (byte)(o.Kind == RtHeapTypeKind.INSTANCE ? (((ASInstance)o.Type).Flags.HasFlag(ClassFlags.Struct) ? HeapKindFlag.FLAG_STRUCT : HeapKindFlag.NONE) : HeapKindFlag.NONE)); //= rtPayload.ParentPtr;
					}
					else
					{
						break;
					}

				} while (true);
				thisValue = new NaNBoxing();
				thisValue = instancePtr;//.SetHeapPtr(instancePtr);
			}

			if (thisValue.ValueType == NaNBoxing.BoxType.Null)
			{
				RaiseTypeError_AccessNull(ref error);
				goto flag_handle_error;
			}

			Debug.Assert(thisValue.ValueType == NaNBoxing.BoxType.HeapPtr); //非堆对象不可能有要写的属性


			BeforeWriteProperty();

			RtHeapBase ins = Context.GC.Heap[thisValue.HeapPtr];

			if (ins.Kind == RtHeapTypeKind.INSTANCE
				||
				ins.Kind == RtHeapTypeKind.VECTOR
				)
			{
				var vtableitem = ins.Type._vtable.Items[vtable_index];
				var function = vtableitem.Trait.Method;

				var define = (ASInstance)vtableitem.DefineAt;

				RunMethod(function,
				thisValue, thisValue.HeapPtr, define, 1, (byte*)argementsPtr, stackslots, ref error, -1);

				if (error.raised)
				{
					goto flag_handle_error;
				}

			}
			else if (ins.Kind == RtHeapTypeKind.ARRAY)
			{
				var vtableitem = ins.Type._vtable.Items[vtable_index];
				var function = vtableitem.Trait.Method;

				var define = (ASInstance)vtableitem.DefineAt;

				RunMethod(function,
				thisValue, thisValue.HeapPtr, define, 1, (byte*)argementsPtr, stackslots, ref error, -1);

				if (error.raised)
				{
					goto flag_handle_error;
				}
			}
			else if (ins.Kind == RtHeapTypeKind.CLASS)
			{
				var @class = ((RtScriptClass)ins).Meta;
				var function = @class._vtable.Items[vtable_index].Trait.Method;


				RunMethod(function,
					thisValue, thisValue.HeapPtr, @class, 1, (byte*)argementsPtr, stackslots, ref error, -1);

				if (error.raised)
				{
					goto flag_handle_error;
				}

			}
			else if (ins.Kind == RtHeapTypeKind.CLOSURE)
			{
#if DEBUG
									if (vtable_index == 1)
									{

										var prop = Context.FUNCTION.Instance._vtable.Items[vtable_index].Trait.Method;
										if (prop.Name != "prototype" || prop.Trait.Kind != TraitKind.Setter)
										{
											throw new InvalidOperationException();
										}
#endif


				byte* aPtr = (byte*)argementsPtr;
				StackLocater argLocater;
				LoadStackLocater(&argLocater, &aPtr);

				NaNBoxing box = stackslots[argLocater.index];

				WriteFunctionProto(box, ref error, ins, thisValue);
				if (error.raised)
				{
					goto flag_handle_error;
				}
#if DEBUG
									}

									else
									{
										throw new InvalidOperationException();
									}
#endif
			}
#if DEBUG
								else
								{
									throw new InvalidOperationException();//其他类型应该没有要写的属性
								}
#endif


		flag_handle_error:
			;

		}


		private unsafe void Write_property_interface(int dst_index, byte** PC, 
			 Span<NaNBoxing> constants, Span<NaNBoxing> stackslots, ref ReceiveError error)
		{
			StackLocater valueLoc;
			valueLoc.index = dst_index;

			StackLocater instance;
			LoadStackLocater(&instance, PC);

			int class_id = 0;
			LoadInt32(&class_id, PC);

			uint vtable_ = 0;
			LoadUInt(&vtable_, PC);
			ushort vtable_index = (ushort)vtable_;

			void* argementsPtr = &valueLoc;

			NaNBoxing thisValue;
#if DEBUG
			if (instance.index >= 0)
#endif
			{
				thisValue = stackslots[instance.index];
				//LoadValue(stackslots[instance.index], ref error, ref stackslots, stackStPos);
				//if (error.raised)
				//{
				//	goto flag_handle_error;
				//}
			}
#if DEBUG
			else
			{
				throw new InvalidOperationException();
			}
#endif

			if (thisValue.ValueType == NaNBoxing.BoxType.Null)
			{
				RaiseTypeError_AccessNull(ref error);
				goto flag_handle_error;
			}

#if DEBUG
			if (thisValue.ValueType != NaNBoxing.BoxType.HeapPtr)
			{
				throw new InvalidOperationException(); //接口，肯定不是基本类型
			}
			else
#endif
			{
				BeforeWriteProperty();

				Debug.Assert(thisValue.HeapKind == (byte)RtHeapTypeKind.INSTANCE);
				RtHeapBase ins = Context.GC.Heap[thisValue.HeapPtr];

				//if (ins.Kind == RtHeapTypeKind.INSTANCE)
				{
					var boxing = constants[class_id];
#if DEBUG
					if (boxing.ValueType != NaNBoxing.BoxType.Uint)
					{
						throw new InvalidOperationException();
					}
#endif
					var @class = Context.link_const_class[(int)boxing.UIntValue];
#if DEBUG
					if (!@class.Instance.IsInterface)
					{
						throw new InvalidOperationException();
					}
#endif

					int m_idx = ((ASInstance)ins.Type)._interface_impl_.First((i) => i.interface_type == @class.Type_identifier)[vtable_index];
					var vtableitem = ins.Type._vtable.Items[m_idx];

					var function = vtableitem.Trait.Method;

					var define = (ASInstance)vtableitem.DefineAt;

					RunMethod(function,
					thisValue, thisValue.HeapPtr, define, 1, (byte*)argementsPtr, stackslots, ref error, -1);

					if (error.raised)
					{
						goto flag_handle_error;
					}

				}
#if DEBUG
				//else
				//{
				//	throw new InvalidOperationException();
				//}
#endif
			}

		flag_handle_error:
			;

		}






		private unsafe void Ld_memberInitValue(byte** PC, RtHeapBase methodscope, int* method_scopes,int scope_ptr , ASContainer scopeType,ref ReceiveError error)
		{

			ScopeHeapLocater heapLocater;
			{
				heapLocater.ScopeIndex = *(ushort*)*PC; *PC += 2;
				heapLocater.MemberIndex = *(ushort*)*PC; *PC += 2;
				//byte* _p = (byte*)&heapLocater.ScopeIndex;
				//*_p++ = *PC++;
				//*_p = *PC++;

				//_p = (byte*)&heapLocater.MemberIndex;
				//*_p++ = *PC++;
				//*_p = *PC++;
			}

			var s = methodscope; //Context.GC.Heap[scope_ptr];
			int* m_scope = method_scopes;
			*m_scope++ = scope_ptr;

		label_method_parent:

			switch (s.Kind)
			{
				case RtHeapTypeKind.CLASS:
				case RtHeapTypeKind.GLOBAL:
					{
						RtScriptClass heap = (RtScriptClass)s;
#if DEBUG
						if (heap.Meta._link_codescope.index != heapLocater.ScopeIndex)
						{
							throw new InvalidOperationException();
						}
#endif
						ASTrait t = heap.Meta._link_codescope.Members[heapLocater.MemberIndex].trait;
						heap.SetSlot(t.Value.initValue.Value, heapLocater.MemberIndex);
					}
					break;

				case RtHeapTypeKind.INSTANCE:
					{
#if DEBUG
						//这里只会在构造函数中进去，所以下面判断成立
						if (scopeType._link_codescope.index != heapLocater.ScopeIndex)
						{
							throw new InvalidOperationException();
						}
#endif
						RtInstance heap = (RtInstance)s;

						ASTrait t = scopeType._link_codescope.Members[heapLocater.MemberIndex].trait;
						heap.SetSlot(t.Value.initValue.Value, heapLocater.MemberIndex, scopeType._link_codescope, this);

					}
					break;
				case RtHeapTypeKind.MethodScope:
					{
						if (s.Type._link_codescope.index != heapLocater.ScopeIndex)
						{
							int parentPtr = ((RtMethodScope)s).ParentPtr;
							s = Context.GC.Heap[parentPtr];
							*m_scope++ = parentPtr;
							goto label_method_parent;
						}
						else
						{
							ASTrait t = s.Type._link_codescope.Members[heapLocater.MemberIndex].trait;

							RtMethodScope heap = (RtMethodScope)s;
							NaNBoxing value = t.Value.initValue.Value;


							ref NaNBoxing heapV = ref heap.ReadSlotRef(heapLocater.MemberIndex);

							if (value.ValueType != BoxType.HeapPtr && heapV.ValueType != BoxType.HeapPtr)
							{
								heapV = value;
							}
							else
							{

								PrepareSaveMethodScope(heap, heapLocater, ref value, m_scope, method_scopes, ref error);
								Debug.Assert(!error.raised);
								heapV = value;
							}

#if FORCOMPILER
							((RtMethodScope)heap).SetSlot(value, heapLocater.MemberIndex);
#endif
						}
					}
					break;
				default:
#if DEBUG
					throw new InvalidOperationException();
#else
										Environment.FailFast("出错了，这里跑不到");return;
#endif
			}



		}


		private unsafe void Ld_MultiName_Ref( int dst_index,byte** PC,  RtHeapBase methodscope,  Span<NaNBoxing> constants, Span<NaNBoxing> stackslots,int stackStPos,int scope_ptr,ref ReceiveError error)
		{
			StackLocater dst;
			dst.index = dst_index;

			StackLocater src;
			LoadStackLocater(&src, PC);

			int const_id;
			{
				LoadInt32(&const_id, PC);
				//byte* _p = (byte*)&const_id;
				//*_p++ = *PC++;
				//*_p++ = *PC++;
				//*_p++ = *PC++;
				//*_p = *PC++;
			}

			string name = ((RtString)Context.GC.Heap[constants[const_id].HeapPtr]).Str;
			//int instancePtr;
			NaNBoxing instance;
			RtHeapTypeKind kind;
			ASContainer as_type;
			if (src.index >= 0)
			{
				instance = stackslots[src.index];
				kind = (RtHeapTypeKind)(instance.ValueType == BoxType.HeapPtr ? instance.HeapKind : 255);
			}
			else
			{
				ReadInstanceFromStacklocater(ref error, src, stackslots, stackStPos, scope_ptr, out kind, out instance);
				if (error.raised)
				{
					goto flag_handle_error;
				}
			}
			switch (instance.ValueType)
			{
				case NaNBoxing.BoxType.Null:
					Context.GC.CheckGC(ref error);
					RaiseTypeError_AccessNull(ref error);
					goto flag_handle_error;
				case NaNBoxing.BoxType.Undefined:
					Context.GC.CheckGC(ref error);
					RaiseTypeError_ATermUndefined(ref error);
					goto flag_handle_error;
				case NaNBoxing.BoxType.HeapPtr:
					as_type = GetASTypeFromValue(instance);
					break;
				case NaNBoxing.BoxType.Sbyte:
					as_type = Context.SBYTE.Instance;
					kind = (RtHeapTypeKind)255;
					break;
				case NaNBoxing.BoxType.Byte:
					as_type = Context.BYTE.Instance;
					kind = (RtHeapTypeKind)255;
					break;
				case NaNBoxing.BoxType.Number:
					as_type = Context.NUMBER.Instance;
					kind = (RtHeapTypeKind)255;
					break;
				case NaNBoxing.BoxType.Short:
					as_type = Context.SHORT.Instance;
					kind = (RtHeapTypeKind)255;
					break;
				case NaNBoxing.BoxType.UShort:
					as_type = Context.USHORT.Instance;
					kind = (RtHeapTypeKind)255;
					break;
				case NaNBoxing.BoxType.Int:
					as_type = Context.INT.Instance;
					kind = (RtHeapTypeKind)255;
					break;
				case NaNBoxing.BoxType.Uint:
					as_type = Context.INT.Instance;
					kind = (RtHeapTypeKind)255;
					break;
				case NaNBoxing.BoxType.Float:
					as_type = Context.FLOAT.Instance;
					kind = (RtHeapTypeKind)255;
					break;
				case NaNBoxing.BoxType.Boolean:
					as_type = Context.BOOLEAN.Instance;
					kind = (RtHeapTypeKind)255;
					break;
				case BoxType.LocalString:
					as_type = Context.STRING.Instance;
					kind = (RtHeapTypeKind)255;
					break;
#if DEBUG
				case NaNBoxing.BoxType.Fault:
				default:
					throw new InvalidOperationException();
#else
									default:
										as_type = null;break;
#endif
			}



			var scope = methodscope; //Context.GC.Heap[scope_ptr];
#if DEBUG
			if (scope.Kind != RtHeapTypeKind.MethodScope)
			{
				throw new InvalidOperationException();
			}
#endif

			var ns_set = scope.Type._link_codescope.NamespaceSet;
			NaNBoxing thisPtr = ((RtMethodScope)methodscope).ThisPtr;
			int code = MultiNameLSearch(ns_set, kind, as_type, name, constants[const_id].HeapPtr, dst, stackslots, stackStPos, instance, check_MultiNameLSearch_issameorinherit(instance, thisPtr.ValueType == BoxType.HeapPtr ? Context.GC.Heap[thisPtr.HeapPtr] : null), ref error);

			switch (code)
			{
				case 0:
					break;
				case 1:
					goto flag_handle_error;
				case 2:
					Context.GC.CheckGC(ref error);
					RaiseTypeError_Ambiguous(ref error, name);
					goto flag_handle_error;
				//case 3:
				//   RaiseReferenceError_MulitNameNotFound(ref error, name, as_type.QName);
				//   goto flag_handle_error;
#if DEBUG
				default:
					throw new InvalidOperationException();
#endif
			}


		flag_handle_error:
			;


		}


		private unsafe void Ld_MultiName_Val(int dst_index, byte** PC,ASMethod method ,RtHeapBase methodscope, Span<NaNBoxing> constants, Span<NaNBoxing> stackslots, int stackStPos, int scope_ptr, ref ReceiveError error)
		{
			//StackLocater dst;
			//dst.index = dst_index;

			StackLocater src;
			LoadStackLocater(&src, PC);

			int const_id;
			{
				LoadInt32(&const_id, PC);				
			}


			string name = ((RtString)Context.GC.Heap[constants[const_id].HeapPtr]).Str;

			StackLocater refholder;
			LoadStackLocater(&refholder, PC);


			NaNBoxing instance;
			RtHeapTypeKind kind;
			ASContainer as_type;

			instance = stackslots[src.index];
			kind = (RtHeapTypeKind)(instance.ValueType == BoxType.HeapPtr ? instance.HeapKind : 255);

			switch (instance.ValueType)
			{
				case NaNBoxing.BoxType.Null:
					Context.GC.CheckGC(ref error);
					RaiseTypeError_AccessNull(ref error);
					goto flag_handle_error;
				case NaNBoxing.BoxType.Undefined:
					Context.GC.CheckGC(ref error);
					RaiseTypeError_ATermUndefined(ref error);
					goto flag_handle_error;
				case NaNBoxing.BoxType.HeapPtr:
					as_type = GetASTypeFromValue(instance);
					break;
				case NaNBoxing.BoxType.Sbyte:
					as_type = Context.SBYTE.Instance;
					kind = (RtHeapTypeKind)255;
					break;
				case NaNBoxing.BoxType.Byte:
					as_type = Context.BYTE.Instance;
					kind = (RtHeapTypeKind)255;
					break;
				case NaNBoxing.BoxType.Number:
					as_type = Context.NUMBER.Instance;
					kind = (RtHeapTypeKind)255;
					break;
				case NaNBoxing.BoxType.Short:
					as_type = Context.SHORT.Instance;
					kind = (RtHeapTypeKind)255;
					break;
				case NaNBoxing.BoxType.UShort:
					as_type = Context.USHORT.Instance;
					kind = (RtHeapTypeKind)255;
					break;
				case NaNBoxing.BoxType.Int:
					as_type = Context.INT.Instance;
					kind = (RtHeapTypeKind)255;
					break;
				case NaNBoxing.BoxType.Uint:
					as_type = Context.INT.Instance;
					kind = (RtHeapTypeKind)255;
					break;
				case NaNBoxing.BoxType.Float:
					as_type = Context.FLOAT.Instance;
					kind = (RtHeapTypeKind)255;
					break;
				case NaNBoxing.BoxType.Boolean:
					as_type = Context.BOOLEAN.Instance;
					kind = (RtHeapTypeKind)255;
					break;
				case BoxType.LocalString:
					as_type = Context.STRING.Instance;
					kind = (RtHeapTypeKind)255;
					break;
#if DEBUG
				case NaNBoxing.BoxType.Fault:
				default:
					throw new InvalidOperationException();
#else
									default:
										as_type = null;break;
#endif
			}

			var scope = methodscope; //Context.GC.Heap[scope_ptr];
#if DEBUG
			if (scope.Kind != RtHeapTypeKind.MethodScope)
			{
				throw new InvalidOperationException();
			}
#endif

			var ns_set = scope.Type._link_codescope.NamespaceSet;
			NaNBoxing thisPtr = ((RtMethodScope)methodscope).ThisPtr;
			int code = MultiNameLSearch(ns_set, kind, as_type, name, constants[const_id].HeapPtr, refholder, stackslots, stackStPos, instance, check_MultiNameLSearch_issameorinherit(instance, thisPtr.ValueType == BoxType.HeapPtr ? Context.GC.Heap[thisPtr.HeapPtr] : null), ref error);

			switch (code)
			{
				case 0:

					if (stackslots[refholder.index].HeapKind == (byte)RtHeapTypeKind.STACK_CACHE_OBJ)
					{
						stackslots[dst_index] = LoadValue((RtStackCache)Context.GC.Heap[stackslots[refholder.index].HeapPtr],
							stackStPos - method.Body._link_codescope.Members.Count - 2, ref error, stackslots, stackStPos + dst_index
						);
						if (error.raised)
						{
							goto flag_handle_error;
						}
					}
					else
					{
						stackslots[dst_index] = stackslots[refholder.index];
					}


					break;
				case 1:
					goto flag_handle_error;
				case 2:
					Context.GC.CheckGC(ref error);
					RaiseTypeError_Ambiguous(ref error, name);
					goto flag_handle_error;
				//case 3:
				//   RaiseReferenceError_MulitNameNotFound(ref error, name, as_type.QName);
				//   goto flag_handle_error;
#if DEBUG
				default:
					throw new InvalidOperationException();
#endif
			}



		flag_handle_error:
			;
		}



		private unsafe void Ld_RTQNameL_Ref(int dst_index,byte** PC , RtHeapBase methodscope , Span<NaNBoxing> stackslots, int stackStPos,int scope_ptr, ref ReceiveError error )
		{

			StackLocater dst_loc;
			dst_loc.index = dst_index;

			StackLocater src;
			LoadStackLocater(&src, PC);

			StackLocater _ns;
			LoadStackLocater(&_ns, PC);

			StackLocater _name;
			LoadStackLocater(&_name, PC);




			NaNBoxing instance_box;
			RtHeapTypeKind kind;


			if (src.index >= 0)
			{
				instance_box = stackslots[src.index];
				kind = (RtHeapTypeKind)(instance_box.ValueType == BoxType.HeapPtr ? instance_box.HeapKind : 255);
			}
			else
			{
				ReadInstanceFromStacklocater(ref error, src, stackslots, stackStPos, scope_ptr, out kind, out instance_box);
				if (error.raised)
				{
					goto flag_handle_error;
				}
			}
			//var ns = LoadValue(stackslots[_ns.index], ref error, ref stackslots,stackStPos);
			//if (error.raised)
			//{
			//    goto flag_handle_error;
			//}
			var ns = stackslots[_ns.index];

			//var name = LoadValue(stackslots[_name.index], ref error, ref stackslots, stackStPos);
			//if (error.raised)
			//{
			//    goto flag_handle_error;
			//}
			var name = stackslots[_name.index];

			ASNamespace searchNs = null;

			Span<char> searchNameBuffer = stackalloc char[128];
			ReadOnlySpan<char> searchName = searchNameBuffer;
			if (ns.ValueType != NaNBoxing.BoxType.HeapPtr)
			{
				goto lbl_rtqname_ns_not_a_namespace;
			}
			else
			{
				RtHeapBase ns_instance = Context.GC.Heap[ns.HeapPtr];
				if (ns_instance.Kind == RtHeapTypeKind.NAMESPACE)
				{
					searchNs = ((RtNameSpace)ns_instance).ASNamespace;

				}
				else
				{
					goto lbl_rtqname_ns_not_a_namespace;
				}
			}

			if (name.ValueType != NaNBoxing.BoxType.HeapPtr)
			{
				//throw new NotImplementedException("cast to string");
				searchName = Extensions.GetPrimitiveValueToString(this, name, searchNameBuffer);
			}
			else
			{
				if (name.HeapKind == (byte)RtHeapTypeKind.STRING)
				{
					RtHeapBase name_instance = Context.GC.Heap[name.HeapPtr];
					searchName = ((RtString)name_instance).Str;
				}
				else if (name.HeapKind == (byte)RtHeapTypeKind.NAMESPACE)
				{
					RtHeapBase name_instance = Context.GC.Heap[name.HeapPtr];
					var n = ((RtNameSpace)name_instance).ASNamespace;
					if (!string.IsNullOrEmpty(n.def_uri))
					{
						searchName = n.def_uri;
					}
					else
					{
						searchName = n.Name;
					}
				}
				else
				{
					Context.GC.CheckGC(ref error);
					if (Context.StackPosition >= Context.STACK_LENGTH)
					{
						RaiseStackOverflow(ref error);
						goto flag_handle_error;
					}

					ref NaNBoxing conv = ref Context.StackSlots[Context.StackPosition];
					Context.StackPosition++;
					ConvertValueType(ref error, name, TypeKind.String, Context.STRING, ref conv, scope_ptr, ((RtMethodScope)methodscope).ThisPtr);
					if (error.raised)
					{
						Context.StackPosition--;
						goto flag_handle_error;
					}

					searchName = Extensions.GetPrimitiveValueToString(this, conv, searchNameBuffer);

					//throw new NotImplementedException("cast to string");
				}
			}

			RtHeapBase instance = null;
			var c_scope = methodscope; //Context.GC.Heap[scope_ptr];
#if DEBUG
			if (c_scope.Kind != RtHeapTypeKind.MethodScope)
			{
				throw new InvalidOperationException();
			}
#endif

			var ns_set = c_scope.Type._link_codescope.NamespaceSet;

			bool deepsearch = false;//如果是从instance的methodscope开始查找说明要继续查找静态成员-基类静态成员
			NaNBoxing instancePtr = default; instancePtr.SetNull();
			NaNBoxing o_instancePtr = default; o_instancePtr.SetNull();
			RtHeapBase o_instance = null;

			CodeScope primitive_codescope = null;

			switch (instance_box.ValueType)
			{
				case NaNBoxing.BoxType.Null:
					Context.GC.CheckGC(ref error);
					RaiseTypeError_AccessNull(ref error);
					goto flag_handle_error;
				case NaNBoxing.BoxType.Undefined:
					Context.GC.CheckGC(ref error);
					RaiseTypeError_ATermUndefined(ref error);
					goto flag_handle_error;
				case NaNBoxing.BoxType.HeapPtr:
					break;
				case NaNBoxing.BoxType.Sbyte:
					primitive_codescope = Context.SBYTE.Instance._link_codescope;
					goto lbl_primitive;
				case NaNBoxing.BoxType.Byte:
					primitive_codescope = Context.BYTE.Instance._link_codescope;
					goto lbl_primitive;
				case NaNBoxing.BoxType.Short:
					primitive_codescope = Context.SHORT.Instance._link_codescope;
					goto lbl_primitive;
				case NaNBoxing.BoxType.UShort:
					primitive_codescope = Context.USHORT.Instance._link_codescope;
					goto lbl_primitive;
				case NaNBoxing.BoxType.Int:
					primitive_codescope = Context.INT.Instance._link_codescope;
					goto lbl_primitive;
				case NaNBoxing.BoxType.Uint:
					primitive_codescope = Context.UINT.Instance._link_codescope;
					goto lbl_primitive;
				case NaNBoxing.BoxType.Float:
					primitive_codescope = Context.FLOAT.Instance._link_codescope;
					goto lbl_primitive;
				case NaNBoxing.BoxType.Number:
					primitive_codescope = Context.NUMBER.Instance._link_codescope;
					goto lbl_primitive;
				case NaNBoxing.BoxType.Boolean:
					primitive_codescope = Context.BOOLEAN.Instance._link_codescope;
					goto lbl_primitive;
#if DEBUG
				case NaNBoxing.BoxType.Fault:
				default:
					throw new InvalidOperationException();
#endif
			}

			instance = Context.GC.Heap[instance_box.HeapPtr];
			o_instance = instance;

			instancePtr = instance_box;
			//RTQName查找 -- 由于自定义命名空间只会在class级别定义，所以实际上只需要查找 静态成员 或者 类成员-继承的类成员-静态成员-基类静态成员找即可。
			while (instance.Kind == RtHeapTypeKind.MethodScope)
			{
				int parent = ((RtMethodScope)instance).ParentPtr;
				instance = Context.GC.Heap[parent];

				instancePtr.SetHeapPtr(parent, (byte)instance.Kind, (byte)(instance.Kind == RtHeapTypeKind.INSTANCE ? (((ASInstance)instance.Type).Flags.HasFlag(ClassFlags.Struct) ? HeapKindFlag.FLAG_STRUCT : HeapKindFlag.NONE) : HeapKindFlag.NONE)); //= ((RtMethodScope)instance).ParentPtr;

				deepsearch = true;
			}
			o_instancePtr = instancePtr;




		lbl_primitive:
			var thisPtr = ((RtMethodScope)methodscope).ThisPtr;
			bool issameorinherit = thisPtr.ValueType == NaNBoxing.BoxType.HeapPtr && instance != null &&
				thisPtr.HeapKind == (byte)instance.Kind
				//Context.GC.Heap[thisPtr.HeapPtr].Kind == instance.Kind
				&&
				thisPtr.HeapKind == (byte)RtHeapTypeKind.INSTANCE
				//Context.GC.Heap[thisPtr.HeapPtr].Kind == RtHeapTypeKind.INSTANCE
				&&
				((ASInstance)instance.Type).IsExtend((ASInstance)Context.GC.Heap[thisPtr.HeapPtr].Type)
			;

			//lambda search member
			var searchmember = (CodeScope scope, ASNamespace ns, ReadOnlySpan<char> name, out int index) =>
			{
				for (int i = 0; i < scope.Members.Count; i++)
				{
					var member = scope.Members[i];
					if (name.CompareTo(member.QName.Name, StringComparison.Ordinal) == 0 && !((ns.Kind == NamespaceKind.Protected || ns.Kind == NamespaceKind.StaticProtected) && !issameorinherit) &&
						(
							member.QName.Namespace == ns
							||
							(
								ns.Kind == NamespaceKind.PackageInternal && ns.def_uri == null
								&&
								member.QName.Namespace.Kind == NamespaceKind.PackageInternal &&
								member.QName.Namespace.def_uri == null &&
								member.DefineAt.QName.Namespace == ns_set.Namespaces[0]
							)
							||
							(
								(ns.Kind == NamespaceKind.PackageInternal || ns.Kind == NamespaceKind.Private || ns.Kind == NamespaceKind.Protected)
								&&
								(string.IsNullOrEmpty(ns.Name) || ns.Kind == NamespaceKind.Private)
								&&
								ns_set.Namespaces.Contains(member.QName.Namespace)
								&&
								(
									member.QName.Namespace.Kind == ns.Kind
									||
									(member.QName.Namespace.Kind == NamespaceKind.StaticProtected && ns.Kind == NamespaceKind.Protected)
								)
								&&
								member.QName.Namespace.def_uri == null
							)

						)

					)
					{
						index = i;
						return member;
					}
				}

				index = -1;
				return null;
			};


			var searchvtable = (VTable vtable, ASNamespace ns, ReadOnlySpan<char> name, out int m_idx, out int g_idx, out int s_idx) =>
			{
				m_idx = -1; g_idx = -1; s_idx = -1;
				for (int i = 0; i < vtable.Items.Count; i++)
				{
					var v = vtable.Items[i];

					if (name.CompareTo(v.Trait.QName.Name, StringComparison.Ordinal) == 0 && !((ns.Kind == NamespaceKind.Protected || ns.Kind == NamespaceKind.StaticProtected) && !issameorinherit) &&
					(
						v.Trait.QName.Namespace == ns
						||
						(
							ns.Kind == NamespaceKind.PackageInternal && ns.def_uri == null
							&&
							v.Trait.QName.Namespace.Kind == NamespaceKind.PackageInternal &&
							v.Trait.QName.Namespace.def_uri == null &&
							v.DefineAt.QName.Namespace == ns_set.Namespaces[0]
						)
						||
						(
							(ns.Kind == NamespaceKind.PackageInternal || ns.Kind == NamespaceKind.Private || ns.Kind == NamespaceKind.Protected)
							&&
							(string.IsNullOrEmpty(ns.Name) || ns.Kind == NamespaceKind.Private)
							&&
							ns_set.Namespaces.Contains(v.Trait.QName.Namespace)
							&&
							(
								v.Trait.QName.Namespace.Kind == ns.Kind
								||
								(v.Trait.QName.Namespace.Kind == NamespaceKind.StaticProtected && ns.Kind == NamespaceKind.Protected)
							)
							&&
							v.Trait.QName.Namespace.def_uri == null
						)

					)
					)
					{
						if (v.Trait.Kind == TraitKind.Method)
						{
							m_idx = i;
							break;
						}
						else if (v.Trait.Kind == TraitKind.Getter)
						{
							g_idx = i;
							if (s_idx != -1)
								break;
						}
						else if (v.Trait.Kind == TraitKind.Setter)
						{
							s_idx = i;
							if (g_idx != -1)
								break;
						}
					}
				}

			};
			//查函数表

			if (primitive_codescope != null)
			{
				int i;
				var member = searchmember(primitive_codescope, searchNs, searchName, out i);
				int m_idx, g_idx, s_idx;
				searchvtable(primitive_codescope.Container._vtable, searchNs, searchName, out m_idx, out g_idx, out s_idx);

				if (member != null)
				{
					int ptrIndex = stackStPos + dst_loc.index;
					int cacheobjpointer = Context.CacheObjPtr + ptrIndex;  //Context.CacheObjectPointers[ptrIndex];
					RtHeapBase cache = Context.GC.Heap[cacheobjpointer];
#if DEBUG
					if (cache.Kind != RtHeapTypeKind.STACK_CACHE_OBJ)
					{
						throw new InvalidOperationException();
					}
#endif

					RtStackCache cachePayload = (RtStackCache)cache;
					cachePayload.RefInstance = instance_box;
					cachePayload.trait[0] = member.trait; cachePayload.trait[1] = null;
					cachePayload.scopemember_index = (ushort)i;
					cachePayload.searchPropertyName.SetUndefined(); cachePayload.as_type = instance.Type;
					cachePayload.searchNameSpacePtr = 0; cachePayload.indexer_key.setFault();
					stackslots[dst_loc.index].SetHeapPtr(cacheobjpointer, (byte)RtHeapTypeKind.STACK_CACHE_OBJ, (byte)HeapKindFlag.NONE);
					goto lbl_rtqname_success;
				}
				else if (m_idx > -1)
				{
					var vitem = primitive_codescope.Container._vtable.Items[m_idx];

					int ptrIndex = stackStPos + dst_loc.index;
					int m_closurePtr = Context.M_ClosurePtr + ptrIndex;

					Context.GC.Heap[m_closurePtr].Type = vitem.Trait.Method.Body;
					RtClosure closure = (RtClosure)Context.GC.Heap[m_closurePtr];
					closure.This = instance_box;
					closure.ScopePtr = 0;
					closure.ScopeType = vitem.DefineAt;
					closure._ref_as_type = GetASTypeFromValue(instance_box); //as_type;
					closure.methodscopeslot_ref_state = 0; closure.HEAPINSTANCE_PTR = 0;
					stackslots[dst_loc.index].SetHeapPtr(m_closurePtr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);
				}
				else
				{
					Context.GC.CheckGC(ref error);

					//int searchPtr = Context.GC.AllocString(searchName);
					//if (searchPtr == 0)
					//{
					//	RaiseOutOfMemory(ref error);
					//	goto flag_handle_error;
					//}

					NaNBoxing searchPtr;
					if (!TryCreateStringValue(searchName, out searchPtr, ref error))
					{
						goto flag_handle_error;
					}


					int ptrIndex = stackStPos + dst_loc.index;
					int cacheobjpointer = Context.CacheObjPtr + ptrIndex;  //Context.CacheObjectPointers[ptrIndex];
					RtHeapBase cache = Context.GC.Heap[cacheobjpointer];
#if DEBUG
					if (cache.Kind != RtHeapTypeKind.STACK_CACHE_OBJ)
					{
						throw new InvalidOperationException();
					}
#endif

					RtStackCache cachePayload = (RtStackCache)cache;
					cachePayload.RefInstance = instance_box;
					cachePayload.trait[0] = null; cachePayload.trait[1] = null;
					cachePayload.scopemember_index = 0;
					cachePayload.searchPropertyName = searchPtr; cachePayload.searchNameSpacePtr = ns.HeapPtr; cachePayload.indexer_key.setFault(); cachePayload.as_type = primitive_codescope.TypeLayout.ASType.Instance;
					stackslots[dst_loc.index].SetHeapPtr(cacheobjpointer, (byte)RtHeapTypeKind.STACK_CACHE_OBJ, (byte)HeapKindFlag.NONE);

					goto lbl_rtqname_dynamicprop;

				}

			}
			else if (instance.Kind == RtHeapTypeKind.INSTANCE
				|| instance.Kind == RtHeapTypeKind.VECTOR
				|| instance.Kind == RtHeapTypeKind.STRING
				|| instance.Kind == RtHeapTypeKind.ARRAY
				)
			{
				CodeScope scope = instance.Type._link_codescope;
				int i;
				var member = searchmember(scope, searchNs, searchName, out i);
				int m_idx, g_idx, s_idx;
				searchvtable(instance.Type._vtable, searchNs, searchName, out m_idx, out g_idx, out s_idx);

				if ((member == null && m_idx < 0 && g_idx < 0 && s_idx < 0) && deepsearch)
				{
					scope = instance.Type._link_codescope.TypeLayout.ASType._link_codescope;
					instancePtr.SetHeapPtr(instance.Type._link_codescope.TypeLayout.ASType.__instance_index__, (byte)RtHeapTypeKind.CLASS, (byte)HeapKindFlag.NONE);

					issameorinherit = false; //静态成员查找跳过 protected..
					member = searchmember(scope, searchNs, searchName, out i); //查找静态成员
					searchvtable(scope.Container._vtable, searchNs, searchName, out m_idx, out g_idx, out s_idx);


					while (member == null && m_idx < 0 && g_idx < 0 && s_idx < 0)
					{
						var superType = ((ASClass)scope.Container).Instance._super_class_; //查找基类的静态成员
						if (superType == null)
							break;

						scope = superType._link_codescope;
						instancePtr.SetHeapPtr(((ASClass)scope.Container).__instance_index__, (byte)RtHeapTypeKind.CLASS, (byte)HeapKindFlag.NONE);
						member = searchmember(scope, searchNs, searchName, out i);
						searchvtable(scope.Container._vtable, searchNs, searchName, out m_idx, out g_idx, out s_idx);

					}
				}

				if (member != null)
				{
					int ptrIndex = stackStPos + dst_loc.index;
					int cacheobjpointer = Context.CacheObjPtr + ptrIndex;  //Context.CacheObjectPointers[ptrIndex];
					RtHeapBase cache = Context.GC.Heap[cacheobjpointer];
#if DEBUG
					if (cache.Kind != RtHeapTypeKind.STACK_CACHE_OBJ)
					{
						throw new InvalidOperationException();
					}
#endif

					RtStackCache cachePayload = (RtStackCache)cache;
					cachePayload.RefInstance = instancePtr;//.SetHeapPtr(instancePtr);
					cachePayload.trait[0] = member.trait; cachePayload.trait[1] = null;
					cachePayload.scopemember_index = (ushort)i;
					cachePayload.searchPropertyName.SetUndefined(); cachePayload.as_type = instance.Type;
					cachePayload.searchNameSpacePtr = 0; cachePayload.indexer_key.setFault();

					stackslots[dst_loc.index].SetHeapPtr(cacheobjpointer, (byte)RtHeapTypeKind.STACK_CACHE_OBJ, (byte)HeapKindFlag.NONE);


					goto lbl_rtqname_success;
				}
				else if (m_idx > -1 || g_idx > -1 || s_idx > -1)
				{
					if (m_idx > -1)
					{
						var vitem = scope.Container._vtable.Items[m_idx];

						int ptrIndex = stackStPos + dst_loc.index;
						int m_closurePtr = Context.M_ClosurePtr + ptrIndex;

						Context.GC.Heap[m_closurePtr].Type = vitem.Trait.Method.Body;
						RtClosure closure = (RtClosure)Context.GC.Heap[m_closurePtr];
						closure.This = instancePtr; //.SetHeapPtr(instancePtr);
						closure.ScopePtr = instancePtr.HeapPtr;
						closure.ScopeType = vitem.DefineAt;
						closure._ref_as_type = instance.Type;  //as_type;
						closure.methodscopeslot_ref_state = 0; closure.HEAPINSTANCE_PTR = 0;
						stackslots[dst_loc.index].SetHeapPtr(m_closurePtr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);

					}
					else
					{
						//throw new NotImplementedException();
						int ptrIndex = stackStPos + dst_loc.index;
						int cacheobjpointer = Context.CacheObjPtr + ptrIndex;
						RtHeapBase cache = Context.GC.Heap[cacheobjpointer];
#if DEBUG
						if (cache.Kind != RtHeapTypeKind.STACK_CACHE_OBJ)
						{
							throw new InvalidOperationException();
						}
#endif

						RtStackCache cachePayload = (RtStackCache)cache;
						cachePayload.RefInstance = instancePtr; //.SetHeapPtr(instancePtr);
						if (g_idx > -1)
						{
							cachePayload.trait[0] = scope.Container._vtable.Items[g_idx].Trait;
							cachePayload.g_index = g_idx;
						}
						else
						{
							cachePayload.trait[0] = null;
						}

						if (s_idx > -1)
						{
							cachePayload.trait[1] = scope.Container._vtable.Items[s_idx].Trait;
							cachePayload.s_index = s_idx;
						}
						else
						{
							cachePayload.trait[1] = null;
						}

						cachePayload.scopemember_index = 0;
						cachePayload.searchPropertyName.SetUndefined(); cachePayload.as_type = instance.Type;
						cachePayload.searchNameSpacePtr = 0; cachePayload.indexer_key.setFault();

						stackslots[dst_loc.index].SetHeapPtr(cacheobjpointer, (byte)RtHeapTypeKind.STACK_CACHE_OBJ, (byte)HeapKindFlag.NONE);

					}
					goto lbl_rtqname_success;
				}
				else
				{
					Context.GC.CheckGC(ref error);

					//int searchPtr = Context.GC.AllocString(searchName);
					//if (searchPtr == 0)
					//{
					//	RaiseOutOfMemory(ref error);
					//	goto flag_handle_error;
					//}
					NaNBoxing searchPtr;
					if (!TryCreateStringValue(searchName, out searchPtr, ref error))
					{
						goto flag_handle_error;
					}

					int ptrIndex = stackStPos + dst_loc.index;
					int cacheobjpointer = Context.CacheObjPtr + ptrIndex;  //Context.CacheObjectPointers[ptrIndex];
					RtHeapBase cache = Context.GC.Heap[cacheobjpointer];
#if DEBUG
					if (cache.Kind != RtHeapTypeKind.STACK_CACHE_OBJ)
					{
						throw new InvalidOperationException();
					}
#endif

					RtStackCache cachePayload = (RtStackCache)cache;
					cachePayload.RefInstance = o_instancePtr; //.SetHeapPtr(o_instancePtr);
					cachePayload.trait[0] = null; cachePayload.trait[1] = null;
					cachePayload.scopemember_index = 0;
					cachePayload.searchPropertyName = searchPtr; cachePayload.searchNameSpacePtr = ns.HeapPtr; cachePayload.indexer_key.setFault(); cachePayload.as_type = instance.Type;


					stackslots[dst_loc.index].SetHeapPtr(cacheobjpointer, (byte)RtHeapTypeKind.STACK_CACHE_OBJ, (byte)HeapKindFlag.NONE);


					goto lbl_rtqname_dynamicprop;

				}
			}
			else if (instance.Kind == RtHeapTypeKind.CLASS)
			{
				CodeScope cls = ((RtScriptClass)instance).Meta._link_codescope;
				int i;
				var member = searchmember(cls, searchNs, searchName, out i);

				int m_idx, g_idx, s_idx;
				searchvtable(cls.Container._vtable, searchNs, searchName, out m_idx, out g_idx, out s_idx);

				if (member != null)
				{
					int ptrIndex = stackStPos + dst_loc.index;
					int cacheobjpointer = Context.CacheObjPtr + ptrIndex;  //Context.CacheObjectPointers[ptrIndex];
					RtHeapBase cache = Context.GC.Heap[cacheobjpointer];
#if DEBUG
					if (cache.Kind != RtHeapTypeKind.STACK_CACHE_OBJ)
					{
						throw new InvalidOperationException();
					}
#endif

					RtStackCache cachePayload = (RtStackCache)cache;
					cachePayload.RefInstance = instancePtr; //.SetHeapPtr(instancePtr);
					cachePayload.trait[0] = member.trait; cachePayload.trait[1] = null;
					cachePayload.scopemember_index = (ushort)i;
					cachePayload.searchPropertyName.SetUndefined(); cachePayload.searchNameSpacePtr = 0; cachePayload.indexer_key.setFault(); cachePayload.as_type = instance.Type;

					stackslots[dst_loc.index].SetHeapPtr(cacheobjpointer, (byte)RtHeapTypeKind.STACK_CACHE_OBJ, (byte)HeapKindFlag.NONE);


					goto lbl_rtqname_success;
				}
				else if (m_idx > -1 || g_idx > -1 || s_idx > -1)
				{
					if (m_idx > -1)
					{
						var vitem = cls.Container._vtable.Items[m_idx];

						int ptrIndex = stackStPos + dst_loc.index;
						int m_closurePtr = Context.M_ClosurePtr + ptrIndex;

						Context.GC.Heap[m_closurePtr].Type = vitem.Trait.Method.Body;
						RtClosure closure = (RtClosure)Context.GC.Heap[m_closurePtr];
						closure.This.SetNull();
						closure.ScopePtr = instancePtr.HeapPtr;
						closure.ScopeType = vitem.DefineAt;
						closure._ref_as_type = cls.Container; //as_type;
						closure.methodscopeslot_ref_state = 0; closure.HEAPINSTANCE_PTR = 0;
						stackslots[dst_loc.index].SetHeapPtr(m_closurePtr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);

					}
					else
					{
						//throw new NotImplementedException();
						int ptrIndex = stackStPos + dst_loc.index;
						int cacheobjpointer = Context.CacheObjPtr + ptrIndex;
						RtHeapBase cache = Context.GC.Heap[cacheobjpointer];
#if DEBUG
						if (cache.Kind != RtHeapTypeKind.STACK_CACHE_OBJ)
						{
							throw new InvalidOperationException();
						}
#endif

						RtStackCache cachePayload = (RtStackCache)cache;
						cachePayload.RefInstance = instancePtr; //.SetHeapPtr(instancePtr);
						if (g_idx > -1)
						{
							cachePayload.trait[0] = cls.Container._vtable.Items[g_idx].Trait;
							cachePayload.g_index = g_idx;
						}
						else
						{
							cachePayload.trait[0] = null;
						}

						if (s_idx > -1)
						{
							cachePayload.trait[1] = cls.Container._vtable.Items[s_idx].Trait;
							cachePayload.s_index = s_idx;
						}
						else
						{
							cachePayload.trait[1] = null;
						}

						cachePayload.scopemember_index = 0;
						cachePayload.searchPropertyName.SetUndefined(); cachePayload.as_type = instance.Type;
						cachePayload.searchNameSpacePtr = 0; cachePayload.indexer_key.setFault();

						stackslots[dst_loc.index].SetHeapPtr(cacheobjpointer, (byte)RtHeapTypeKind.STACK_CACHE_OBJ, (byte)HeapKindFlag.NONE);

					}
					goto lbl_rtqname_success;
				}


				else if (searchNs.Kind != NamespaceKind.Package)
				{
					goto lbl_rtqname_notfound;
				}
				else
				{
					Context.GC.CheckGC(ref error);

					//int searchPtr = Context.GC.AllocString(searchName);
					//if (searchPtr == 0)
					//{
					//	RaiseOutOfMemory(ref error);
					//	goto flag_handle_error;
					//}

					NaNBoxing searchPtr;
					if (!TryCreateStringValue(searchName, out searchPtr, ref error))
					{
						goto flag_handle_error;
					}

					int ptrIndex = stackStPos + dst_loc.index;
					int cacheobjpointer = Context.CacheObjPtr + ptrIndex;  //Context.CacheObjectPointers[ptrIndex];
					RtHeapBase cache = Context.GC.Heap[cacheobjpointer];
#if DEBUG
					if (cache.Kind != RtHeapTypeKind.STACK_CACHE_OBJ)
					{
						throw new InvalidOperationException();
					}
#endif

					RtStackCache cachePayload = (RtStackCache)cache;
					cachePayload.RefInstance = o_instancePtr;//.SetHeapPtr(o_instancePtr);
					cachePayload.trait[0] = null; cachePayload.trait[1] = null;
					cachePayload.scopemember_index = 0;
					cachePayload.searchPropertyName = searchPtr; cachePayload.searchNameSpacePtr = 0; cachePayload.indexer_key.setFault(); cachePayload.as_type = instance.Type;

					stackslots[dst_loc.index].SetHeapPtr(cacheobjpointer, (byte)RtHeapTypeKind.STACK_CACHE_OBJ, (byte)HeapKindFlag.NONE);


					goto lbl_rtqname_dynamicprop;
				}

			}
			else if (instance.Kind == RtHeapTypeKind.GLOBAL)
			{
				goto lbl_rtqname_notfound;
			}
			else if (instance.Kind == RtHeapTypeKind.CLOSURE)
			{
				goto lbl_rtqname_notfound;
			}
#if DEBUG
			else
			{
				throw new InvalidOperationException();
			}
#endif

		lbl_rtqname_success:;
		lbl_rtqname_dynamicprop:;
			return;
		lbl_rtqname_ns_not_a_namespace:
			//throw new NotImplementedException("输出命名空间类型转换异常");
			Context.GC.CheckGC(ref error);
			RaiseTypeError(ref error, ns, TypeKind.Namespace);
			goto flag_handle_error;

		lbl_rtqname_notfound:;
			Context.GC.CheckGC(ref error);
			//throw new NotImplementedException("输出未找到异常");
			RaiseReferenceError_RTQNameNotFound(ref error, ns, searchName, instance_box);
			goto flag_handle_error;



		flag_handle_error:
			;

		}


		private unsafe void StoreScopeH( int dst_index,byte** PC,
			int scope_ptr,  RtHeapBase methodscope, int* method_scopes  ,Span<NaNBoxing> stackslots,
			ASContainer scopeType,
			ref ReceiveError error
			)
		{
			ScopeHeapLocater heapLocater;
			{
				heapLocater.ScopeIndex = *(ushort*)*PC; *PC += 2;
				heapLocater.MemberIndex = *(ushort*)*PC; *PC += 2;

				//byte* _p = (byte*)&heapLocater.ScopeIndex;
				//*_p++ = *PC++;
				//*_p = *PC++;

				//_p = (byte*)&heapLocater.MemberIndex;
				//*_p++ = *PC++;
				//*_p = *PC++;
			}
			StackLocater stackLocater;
			stackLocater.index = dst_index;


			NaNBoxing value = stackslots[stackLocater.index];
			var s = methodscope; //Context.GC.Heap[scope_ptr];

			int* m_scope = method_scopes;
			*m_scope++ = scope_ptr;

		label_method_parent:
			switch (s.Kind)
			{
				case RtHeapTypeKind.CLASS:
				case RtHeapTypeKind.GLOBAL:
					{
						RtScriptClass heap = (RtScriptClass)s;

						if (heap.Meta._link_codescope.index != heapLocater.ScopeIndex)
						{
#if DEBUG
							if (s.Kind != RtHeapTypeKind.CLASS)
							{
								throw new InvalidOperationException();
							}
							else
#endif
							{
								heap = (RtScriptClass)Context.GC.Heap[((ASScript)heap.Meta._link_codescope.Parent.Container).__global_index__]
										;
							}
						}

						ASTrait t = heap.Meta._link_codescope.Members[heapLocater.MemberIndex].trait;

						Context.GC.CheckGC(ref error);
						if (Context.StackPosition >= Context.STACK_LENGTH)
						{
							RaiseStackOverflow(ref error);
							goto flag_handle_error;
						}

						ref NaNBoxing conv = ref Context.StackSlots[Context.StackPosition];
						Context.StackPosition++;

						ConvertValueType(ref error, value, t.TypeKind, t.__rt_type_class__, ref conv, scope_ptr, ((RtMethodScope)methodscope).ThisPtr);
						if (error.raised)
						{
							Context.StackPosition--;
							goto flag_handle_error;
						}

						if (heap.IsUpdateStructOrEqual(Context, heapLocater.MemberIndex, conv))
						{
							Context.StackPosition--;
						}
						else
						{
							value = GetSaveValue(conv, ref error);
							Context.StackPosition--;

							if (error.raised)
							{
								goto flag_handle_error;
							}

							heap.SetSlot(value, heapLocater.MemberIndex);
						}
					}
					break;

				case RtHeapTypeKind.INSTANCE:
					{

						//考虑可能继承的情况，scopeType保存上下文堆内存用的布局类型
						//if (scopeType._link_codescope.index != heapLocater.ScopeIndex)
						if (
							scopeType._link_codescope.index != heapLocater.ScopeIndex  //子类调用基类的构造函数时，可能下面的条件不成立，这时判断一下scopeType的类型
							&&
							s.Type._link_codescope.index != heapLocater.ScopeIndex)
						{
							var sType = scopeType._link_codescope.Parent;    //这里还是必须用scopeType来查找global.
							while (sType.Kind != CodeScopeKind.Script)
							{
								sType = sType.Parent;
							}

							RtScriptClass heap = (RtScriptClass)Context.GC.Heap[((ASScript)sType.Container).__global_index__]
										;
							ASTrait t = heap.Meta._link_codescope.Members[heapLocater.MemberIndex].trait;


							Context.GC.CheckGC(ref error);
							if (Context.StackPosition >= Context.STACK_LENGTH)
							{
								RaiseStackOverflow(ref error);
								goto flag_handle_error;
							}

							ref NaNBoxing conv = ref Context.StackSlots[Context.StackPosition];
							Context.StackPosition++;

							ConvertValueType(ref error, value, t.TypeKind, t.__rt_type_class__, ref conv, scope_ptr, ((RtMethodScope)methodscope).ThisPtr);
							if (error.raised)
							{
								Context.StackPosition--;
								goto flag_handle_error;
							}

							if (heap.IsUpdateStructOrEqual(Context, heapLocater.MemberIndex, conv))
							{
								Context.StackPosition--;
							}
							else
							{
								value = GetSaveValue(conv, ref error);
								Context.StackPosition--;

								if (error.raised)
								{
									goto flag_handle_error;
								}

								heap.SetSlot(value, heapLocater.MemberIndex);
							}
						}
						else
						{
							RtInstance heap = (RtInstance)s;


							Context.GC.CheckGC(ref error);
							if (Context.StackPosition >= Context.STACK_LENGTH)
							{
								RaiseStackOverflow(ref error);
								goto flag_handle_error;
							}

							ref NaNBoxing conv = ref Context.StackSlots[Context.StackPosition];
							Context.StackPosition++;

							ConvertValueType(ref error, value,
								s.Type._link_codescope.Members[heapLocater.MemberIndex].TypeKind,
								s.Type._link_codescope.Members[heapLocater.MemberIndex].__rt_type_class__, ref conv, scope_ptr, ((RtMethodScope)methodscope).ThisPtr);

							if (error.raised)
							{
								Context.StackPosition--;
								goto flag_handle_error;
							}
							if (heap.IsUpdateStructOrEqual(Context, heapLocater.MemberIndex, conv, (ASInstance)s.Type))
							{
								Context.StackPosition--;
							}
							else
							{
								value = GetSaveValue(conv, ref error);
								Context.StackPosition--;
								if (error.raised)
								{
									goto flag_handle_error;
								}

								heap.SetSlot(value, heapLocater.MemberIndex, s.Type._link_codescope, this);
							}
						}
					}
					break;
				case RtHeapTypeKind.MethodScope:
					{

						if (s.Type._link_codescope.index != heapLocater.ScopeIndex)
						{
							int parentPtr = ((RtMethodScope)s).ParentPtr;
							s = Context.GC.Heap[parentPtr];
							*m_scope++ = parentPtr;

							goto label_method_parent;
						}
						else
						{
							var thisPtr = ((RtMethodScope)methodscope).ThisPtr;
							var scopemember = s.Type._link_codescope.Members[heapLocater.MemberIndex];


							Context.GC.CheckGC(ref error);
							if (Context.StackPosition >= Context.STACK_LENGTH)
							{
								RaiseStackOverflow(ref error);
								goto flag_handle_error;
							}

							ref NaNBoxing conv = ref Context.StackSlots[Context.StackPosition];
							Context.StackPosition++;

							bool isheaptype;
							if (scopemember.Kind == ScopeMemberKind.Parameter)
							{
								isheaptype = scopemember.TypeKind.IsHeapType();
								ConvertValueType(ref error, value, scopemember.TypeKind, scopemember.__rt_type_class__, ref conv, scope_ptr, thisPtr);
							}
							else
							{
								ASTrait t = s.Type._link_codescope.Members[heapLocater.MemberIndex].trait;
								isheaptype = t.TypeKind.IsHeapType();
								ConvertValueType(ref error, value, t.TypeKind, t.__rt_type_class__, ref conv, scope_ptr, thisPtr);
							}
							if (error.raised)
							{
								Context.StackPosition--;
								goto flag_handle_error;
							}


							value = conv;

							RtMethodScope heap = (RtMethodScope)s;
							if (isheaptype)
							{
								PrepareSaveMethodScope(heap, heapLocater, ref value, m_scope, method_scopes, ref error);

								if (error.raised)
								{
									Context.StackPosition--;
									goto flag_handle_error;
								}
							}
							heap.SetSlot(value, heapLocater.MemberIndex);
							Context.StackPosition--;
						}
					}
					break;
#if DEBUG
				default:
					throw new InvalidOperationException();
#endif
			}

		flag_handle_error:
			;

		}


		




		private unsafe void Ld_InstanceOrScopeMemberValueRef(int dst_index,byte** PC ,Span<NaNBoxing> stackslots,
			int stackStPos,int scope_ptr,
			ref ReceiveError error)
		{

			StackLocater target;
			target.index = dst_index;

			StackLocater src;
			LoadStackLocater(&src, PC);

			//ushort trait_index;
			uint scopemember_index;

			//LoadUShort(&trait_index, &PC);
			LoadUInt(&scopemember_index, PC);


			NaNBoxing instance_box;
			RtHeapTypeKind kind;
			//ASContainer as_type;

			if (src.index >= 0)
			{
				instance_box = stackslots[src.index];
				kind = (RtHeapTypeKind)(instance_box.ValueType == BoxType.HeapPtr ? instance_box.HeapKind : 255);
			}
			else
			{
				ReadInstanceFromStacklocater(ref error, src, stackslots, stackStPos, scope_ptr, out kind, out instance_box);
				if (error.raised)
				{
					goto flag_handle_error;
				}
			}

			switch (instance_box.ValueType)
			{
				case NaNBoxing.BoxType.Null:
					Context.GC.CheckGC(ref error);
					RaiseTypeError_AccessNull(ref error);
					goto flag_handle_error;
				case NaNBoxing.BoxType.HeapPtr:
					break;
#if DEBUG
				case NaNBoxing.BoxType.Number:
				case NaNBoxing.BoxType.Boolean:
				case NaNBoxing.BoxType.Int:
				case NaNBoxing.BoxType.Uint:
				case NaNBoxing.BoxType.Sbyte:
				case NaNBoxing.BoxType.Byte:
				case NaNBoxing.BoxType.Short:
				case NaNBoxing.BoxType.UShort:
				case NaNBoxing.BoxType.Float:
					throw new InvalidOperationException(); //这些东西没有成员

				case NaNBoxing.BoxType.Undefined:
				case NaNBoxing.BoxType.Fault:
				default:
					throw new InvalidOperationException();
#endif
			}




			do
			{
				if (instance_box.HeapKind == (byte)RtHeapTypeKind.CLASS || instance_box.HeapKind == (byte)RtHeapTypeKind.GLOBAL)
				{

					var instance = Context.GC.Heap[instance_box.HeapPtr];
					RtScriptClass heap = (RtScriptClass)instance;
					ASTrait trait = heap.Meta._link_codescope.Members[(ushort)scopemember_index].trait;
#if DEBUG

					if (!
						(trait.Kind == TraitKind.Slot ||
							trait.Kind == TraitKind.Constant
						)
						)
					{
						throw new InvalidOperationException();
					}
#endif

					int ptrIndex = stackStPos + target.index;
					int cacheobjpointer = Context.CacheObjPtr + ptrIndex;  //Context.CacheObjectPointers[ptrIndex];
					RtHeapBase cache = Context.GC.Heap[cacheobjpointer];
#if DEBUG
					if (cache.Kind != RtHeapTypeKind.STACK_CACHE_OBJ)
					{
						throw new InvalidOperationException();
					}

#endif
					RtStackCache cachePayload = (RtStackCache)cache;
					cachePayload.RefInstance = instance_box;
					cachePayload.trait[0] = trait; cachePayload.trait[1] = null;
					cachePayload.scopemember_index = (ushort)scopemember_index;
					cachePayload.searchPropertyName.SetUndefined(); cachePayload.searchNameSpacePtr = 0; cachePayload.indexer_key.setFault(); cachePayload.as_type = instance.Type;

					stackslots[target.index].SetHeapPtr(cacheobjpointer, (byte)RtHeapTypeKind.STACK_CACHE_OBJ, (byte)HeapKindFlag.NONE);

				}
				else if (instance_box.HeapKind == (byte)RtHeapTypeKind.INSTANCE)
				{

					var instance = Context.GC.Heap[instance_box.HeapPtr];
					ASTrait trait = instance.Type._link_codescope.Members[(ushort)scopemember_index].trait;
#if DEBUG

					if (!
						(trait.Kind == TraitKind.Slot ||
						trait.Kind == TraitKind.Constant
						)
						)
					{
						throw new InvalidOperationException();
					}
#endif
					int ptrIndex = stackStPos + target.index;
					int cacheobjpointer = Context.CacheObjPtr + ptrIndex;  //Context.CacheObjectPointers[ptrIndex];
					RtHeapBase cache = Context.GC.Heap[cacheobjpointer];
#if DEBUG
					if (cache.Kind != RtHeapTypeKind.STACK_CACHE_OBJ)
					{
						throw new InvalidOperationException();
					}

#endif

					RtStackCache cachePayload = (RtStackCache)cache;
					cachePayload.RefInstance = instance_box;
					cachePayload.trait[0] = trait; cachePayload.trait[1] = null;
					cachePayload.scopemember_index = (ushort)scopemember_index;
					cachePayload.searchPropertyName.SetUndefined(); cachePayload.searchNameSpacePtr = 0; cachePayload.indexer_key.setFault(); cachePayload.as_type = instance.Type;

					stackslots[target.index].SetHeapPtr(cacheobjpointer, (byte)RtHeapTypeKind.STACK_CACHE_OBJ, (byte)HeapKindFlag.NONE);

				}
#if DEBUG
				else if (instance_box.HeapKind == (byte)RtHeapTypeKind.STACK_CACHE_OBJ)
				{
					throw new InvalidOperationException();
					//                                        int instancePtr = instance_box.HeapPtr;
					//                                        NaNBoxing v = new NaNBoxing();
					//                                        v.SetHeapPtr(instancePtr);
					//                                        v = LoadValue(v, ref error  ,ref stackslots, stackStPos);

					//#if DEBUG
					//                                        if (error.raised)
					//                                        {
					//                                            throw new InvalidOperationException();
					//                                        }
					//#endif


					//                                        if (v.ValueType == NaNBoxing.BoxType.HeapPtr)
					//                                        {
					//                                            instancePtr = v.HeapPtr;
					//                                            instance = Context.GC.Heap[instancePtr];
					//                                            continue;
					//                                        }
					//                                        else
					//                                        {
					//                                            throw new NotImplementedException();
					//                                        }

				}
#endif
#if DEBUG
				else
				{
					throw new InvalidOperationException();
				}
#endif
				break;
			}
			while (true);

		flag_handle_error:
			;


		}




		private unsafe void Ld_MulitNameL_Ref( int dst_index,byte** PC, Span<NaNBoxing> constants ,
			Span<NaNBoxing> stackslots,
			int stackStPos,int scope_ptr, RtHeapBase methodscope,
			ref ReceiveError error)
		{
			uint* opcodePtr = (uint*)*PC - 1; Debug.Assert((*opcodePtr & 0xff) == (byte)INS_Code.ld_MultiNameL_Ref);


			StackLocater stack;
			stack.index = dst_index;

			StackLocater src;
			LoadStackLocater(&src, PC);

			StackLocater _name;
			LoadStackLocater(&_name, PC);

			int super_const_index;
			LoadInt32(&super_const_index, PC);



			NaNBoxing instance_box;
			RtHeapTypeKind kind;
			ASContainer as_type = null;

			if (src.index >= 0)
			{
				instance_box = stackslots[src.index];
				kind = (RtHeapTypeKind)(instance_box.ValueType == BoxType.HeapPtr ? instance_box.HeapKind : 255);
			}
			else
			{
				ReadInstanceFromStacklocater(ref error, src, stackslots, stackStPos, scope_ptr, out kind, out instance_box);
				if (error.raised)
				{
					goto flag_handle_error;
				}
			}

			if (super_const_index != 0)
			{
				//读基类
				super_const_index -= 1;

				var vbox = constants[super_const_index];

#if DEBUG
				if (vbox.ValueType != NaNBoxing.BoxType.Uint)
					throw new InvalidOperationException();
#endif

				var super_class = Context.link_const_class[(int)vbox.UIntValue];

#if DEBUG
				var check = GetASTypeFromValue(instance_box);
				if (check is ASInstance)
				{
					if (!((ASInstance)check).IsExtend(super_class.Instance))
					{
						throw new InvalidOperationException();
					}
				}

#endif

				as_type = super_class.Instance;
			}

			//RtHeapBase instance = null;
			bool setinstance = false;
			switch (instance_box.ValueType)
			{
				case NaNBoxing.BoxType.Null:
					Context.GC.CheckGC(ref error);
					RaiseTypeError_AccessNull(ref error);
					goto flag_handle_error;
				case NaNBoxing.BoxType.Undefined:
					Context.GC.CheckGC(ref error);
					RaiseTypeError_ATermUndefined(ref error);
					goto flag_handle_error;
				case NaNBoxing.BoxType.HeapPtr:
					break;
				case NaNBoxing.BoxType.Sbyte:
					as_type = Context.SBYTE.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case NaNBoxing.BoxType.Byte:
					as_type = Context.BYTE.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case NaNBoxing.BoxType.Short:
					as_type = Context.SHORT.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case NaNBoxing.BoxType.UShort:
					as_type = Context.USHORT.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case NaNBoxing.BoxType.Int:
					as_type = Context.INT.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case NaNBoxing.BoxType.Uint:
					as_type = Context.UINT.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case NaNBoxing.BoxType.Float:
					as_type = Context.FLOAT.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case NaNBoxing.BoxType.Number:
					as_type = Context.NUMBER.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case NaNBoxing.BoxType.Boolean:
					as_type = Context.BOOLEAN.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case BoxType.LocalString:
					as_type = Context.STRING.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;

#if DEBUG
				case NaNBoxing.BoxType.Fault:
				default:
					throw new InvalidOperationException();
#endif
			}


			setinstance = true;
		lbl_instance_primitive:
			Span<char> buffers = stackalloc char[128];
			ReadOnlySpan<char> name = buffers;

			NaNBoxing prop_name = stackslots[_name.index];

			if (setinstance && (
				instance_box.HeapKind == (byte)RtHeapTypeKind.INSTANCE
				&&
				((ASInstance)Context.GC.Heap[instance_box.HeapPtr].Type).Flags.HasFlag(ClassFlags.Indexer)
				)
				||

				(instance_box.HeapKind == (byte)RtHeapTypeKind.VECTOR && RtVector.IsValidIndexType(prop_name))

				)
			{
				//索引器处理
				int ptrIndex = stackStPos + stack.index;
				int cacheobjpointer = Context.CacheObjPtr + ptrIndex;  //Context.CacheObjectPointers[ptrIndex];
				RtHeapBase cache = Context.GC.Heap[cacheobjpointer];
#if DEBUG
				if (cache.Kind != RtHeapTypeKind.STACK_CACHE_OBJ)
				{
					throw new InvalidOperationException();
				}
#endif


				RtStackCache cachePayload = (RtStackCache)cache;
				cachePayload.RefInstance = instance_box;
				cachePayload.trait[0] = null; cachePayload.trait[1] = null;
				cachePayload.scopemember_index = 0;
				cachePayload.searchPropertyName.SetUndefined(); cachePayload.as_type = Context.GC.Heap[instance_box.HeapPtr].Type;
				cachePayload.searchNameSpacePtr = 0; cachePayload.indexer_key = prop_name;

				stackslots[stack.index].SetHeapPtr(cacheobjpointer, (byte)RtHeapTypeKind.STACK_CACHE_OBJ, (byte)HeapKindFlag.NONE);

				return;
			}
			else if (prop_name.ValueType != NaNBoxing.BoxType.HeapPtr)
			{
				if (setinstance && instance_box.HeapKind == (byte)RtHeapTypeKind.ARRAY)
				{
					long index;

					switch (prop_name.ValueType)
					{
						case BoxType.LocalString:
							// Use efficient char-based extraction to avoid string allocation

							int charCount = prop_name.GetLocalStringChars(buffers);
							name = charCount > 0 ? buffers.Slice(0, charCount) : ReadOnlySpan<char>.Empty;
							goto lbl_name_solved;
						case NaNBoxing.BoxType.Number:
							{
								double v = prop_name.Number;
								if (v >= 0 && (v == Math.Truncate(v)) && !Double.IsInfinity(v) && v < uint.MaxValue)
								{
									index = (long)v;
									if (index >= 0 && index < uint.MaxValue)
									{
										goto array_index;
									}
									else
									{
										name = index.ToString();
										goto array_prop;
									}
								}
								else
								{
									name = Extensions.GetPrimitiveValueToString(this, prop_name, buffers);
									goto array_prop;
								}
							}
						case NaNBoxing.BoxType.Float:
							{
								double v = prop_name.FloatValue;
								if (v >= 0 && (v == Math.Truncate(v)) && !Double.IsInfinity(v) && v < uint.MaxValue)
								{
									index = (long)v;
									if (index >= 0 && index < uint.MaxValue)
									{
										goto array_index;
									}
									else
									{
										name = index.ToString();
										goto array_prop;
									}
								}
								else
								{
									name = Extensions.GetPrimitiveValueToString(this, prop_name, buffers);
									goto array_prop;
								}
							}
						case NaNBoxing.BoxType.Undefined:
							name = "undefined";
							goto array_prop;
						case NaNBoxing.BoxType.Null:
							name = "null";
							goto array_prop;
						case NaNBoxing.BoxType.Boolean:
							name = prop_name.Boolean ? "true" : "false";
							goto array_prop;
						case NaNBoxing.BoxType.Int:
							{
								index = prop_name.IntValue;
								if (index >= 0)
								{
									goto array_index;
								}
								else
								{
									name = index.ToString();
									goto array_prop;
								}
							}
						case NaNBoxing.BoxType.Uint:
							{
								index = prop_name.UIntValue;
								if (index < uint.MaxValue)
								{
									goto array_index;
								}
								else
								{
									name = index.ToString();
									goto array_prop;
								}
							}
						case NaNBoxing.BoxType.Sbyte:
							{
								index = prop_name.SByteValue;
								if (index >= 0)
								{
									goto array_index;
								}
								else
								{
									name = index.ToString();
									goto array_prop;
								}
							}
						case NaNBoxing.BoxType.Byte:
							{
								index = prop_name.ByteValue;
								goto array_index;
							}
						case NaNBoxing.BoxType.Short:
							{
								index = prop_name.ShortValue;
								if (index >= 0)
								{
									goto array_index;
								}
								else
								{
									name = index.ToString();
									goto array_prop;
								}
							}
						case NaNBoxing.BoxType.UShort:
							{
								index = prop_name.UShortValue;
								goto array_index;
							}
#if DEBUG
						case NaNBoxing.BoxType.Fault:
						default:
							throw new InvalidOperationException();
#else
											default:
												Environment.FailFast("出错了，这里跑不到");

												error.error.setFault();
												goto flag_handle_error;
#endif
					}

				//索引处理
				array_index:
					uint array_i = (uint)index;
					int ptrIndex = stackStPos + stack.index;
					int cacheobjpointer = Context.CacheObjPtr + ptrIndex;  //Context.CacheObjectPointers[ptrIndex];
					RtHeapBase cache = Context.GC.Heap[cacheobjpointer];
#if DEBUG
					if (cache.Kind != RtHeapTypeKind.STACK_CACHE_OBJ)
					{
						throw new InvalidOperationException();
					}
#endif

					RtStackCache cachePayload = (RtStackCache)cache;
					cachePayload.RefInstance = instance_box;
					cachePayload.trait[0] = null; cachePayload.trait[1] = null;
					cachePayload.scopemember_index = 0;
					cachePayload.searchPropertyName.SetUndefined(); cachePayload.as_type = Context.GC.Heap[instance_box.HeapPtr].Type;
					cachePayload.searchNameSpacePtr = 0; cachePayload.indexer_key.SetUInt(array_i);

					stackslots[stack.index].SetHeapPtr(cacheobjpointer, (byte)RtHeapTypeKind.STACK_CACHE_OBJ, (byte)HeapKindFlag.NONE);


					//										//quickening
					//#if FORCOMPILER
					//										if (!IsComputeConstExpr)
					//										{
					//#endif
					//											if (super_const_index == 0 && src.index >=0 && 
					//											(prop_name.ValueType == BoxType.Int || prop_name.ValueType == BoxType.Byte 
					//												|| prop_name.ValueType == BoxType.Sbyte || prop_name.ValueType ==  BoxType.Short || prop_name.ValueType == BoxType.UShort) )
					//											{
					//												*opcodePtr = ((uint)INS_Code.ld_MultiNameL_Ref_ARR_INT | (0xffffff00 & (*opcodePtr)));
					//											}

					//#if FORCOMPILER
					//										}
					//#endif


					return;

				array_prop:;


				}

				else if (setinstance && instance_box.HeapKind == (byte)RtHeapTypeKind.VECTOR)
				{
					//不合理的索引范围
#if DEBUG
					if (RtVector.IsValidIndexType(prop_name))
					{
						throw new InvalidOperationException();
					}
#endif

					name = Extensions.GetPrimitiveValueToString(this, prop_name, buffers);
				}
				else
				{
					name = Extensions.GetPrimitiveValueToString(this, prop_name, buffers);
					//throw new NotImplementedException("转字符串？还是数组？");
				}
			}
			else
			{

				//RtHeapBase _n = Context.GC.Heap[prop_name.HeapPtr];
				if (prop_name.HeapKind != (byte)RtHeapTypeKind.STRING)
				{
					if (Context.StackPosition + 1 >= Context.STACK_LENGTH)
					{
						RaiseStackOverflow(ref error);
						goto flag_handle_error;
					}

					var span = Context.StackSlots.AsSpan(Context.StackPosition, 2); span.Clear();
					StackLocater tmp = default; tmp.index = 0;
					StackLocater tmp2 = default; tmp2.index = 1;

					int stpos = Context.StackPosition;
					Context.StackPosition +=2;
					NaNBoxing primitive_name = ToPrimitive(ref error, prop_name, HINT.h_string, scope_ptr, tmp, tmp2, span, stpos, ((RtMethodScope)methodscope).ThisPtr);
					if (error.raised)
					{
						Context.StackPosition=stpos;
						goto flag_handle_error;
					}

					name = Extensions.GetPrimitiveValueToString(this, primitive_name, buffers);
					Context.StackPosition=stpos;


					//throw new NotImplementedException("转字符串？");
				}
				else
				{
					RtHeapBase _n = Context.GC.Heap[prop_name.HeapPtr];
					name = ((RtString)_n).Str;
				}

			}

		lbl_name_solved:

			var scope = methodscope; //Context.GC.Heap[scope_ptr];
#if DEBUG
			if (scope.Kind != RtHeapTypeKind.MethodScope)
			{
				throw new InvalidOperationException();
			}
#endif

			if (as_type == null)
			{
				as_type = GetASTypeFromValue(instance_box);
			}

			var ns_set = scope.Type._link_codescope.NamespaceSet;
			NaNBoxing thisPtr = ((RtMethodScope)methodscope).ThisPtr;
			int code = MultiNameLSearch(ns_set, kind, as_type, name, 0, stack, stackslots, stackStPos, instance_box, check_MultiNameLSearch_issameorinherit(instance_box, thisPtr.ValueType == BoxType.HeapPtr ? Context.GC.Heap[thisPtr.HeapPtr] : null), ref error);

			switch (code)
			{
				case 0:
					break;
				case 1:
					goto flag_handle_error;
				case 2:
					Context.GC.CheckGC(ref error);
					RaiseTypeError_Ambiguous(ref error, name);
					goto flag_handle_error;
#if DEBUG
				//case 3:
				//    RaiseReferenceError_MulitNameNotFound(ref error, name, as_type.QName);
				//    goto flag_handle_error;
				default:
					throw new InvalidOperationException();
#endif
			}


		flag_handle_error:
			;

		}

		private unsafe void Store_MultiName(int dst_index, byte** PC, Span<NaNBoxing> constants,
			Span<NaNBoxing> stackslots,
			int stackStPos, int scope_ptr, RtHeapBase methodscope,
			ref ReceiveError error)
		{

			StackLocater source;
			source.index = dst_index;

			StackLocater ins;
			LoadStackLocater(&ins, PC);

			int const_id;
			{
				LoadInt32(&const_id, PC);				
			}

			string name = ((RtString)Context.GC.Heap[constants[const_id].HeapPtr]).Str;

			StackLocater tmp_holder;
			LoadStackLocater(&tmp_holder, PC);

			Debug.Assert(ins.index >= 0);

			//int instancePtr;
			NaNBoxing instance;
			RtHeapTypeKind kind;
			ASContainer as_type;
			

			instance = stackslots[ins.index];
			kind = (RtHeapTypeKind)(instance.ValueType == BoxType.HeapPtr ? instance.HeapKind : 255);
			
			switch (instance.ValueType)
			{
				case NaNBoxing.BoxType.Null:
					Context.GC.CheckGC(ref error);
					RaiseTypeError_AccessNull(ref error);
					goto flag_handle_error;
				case NaNBoxing.BoxType.Undefined:
					Context.GC.CheckGC(ref error);
					RaiseTypeError_ATermUndefined(ref error);
					goto flag_handle_error;
				case NaNBoxing.BoxType.HeapPtr:
					as_type = GetASTypeFromValue(instance);
					break;
				case NaNBoxing.BoxType.Sbyte:
					as_type = Context.SBYTE.Instance;
					kind = (RtHeapTypeKind)255;
					break;
				case NaNBoxing.BoxType.Byte:
					as_type = Context.BYTE.Instance;
					kind = (RtHeapTypeKind)255;
					break;
				case NaNBoxing.BoxType.Number:
					as_type = Context.NUMBER.Instance;
					kind = (RtHeapTypeKind)255;
					break;
				case NaNBoxing.BoxType.Short:
					as_type = Context.SHORT.Instance;
					kind = (RtHeapTypeKind)255;
					break;
				case NaNBoxing.BoxType.UShort:
					as_type = Context.USHORT.Instance;
					kind = (RtHeapTypeKind)255;
					break;
				case NaNBoxing.BoxType.Int:
					as_type = Context.INT.Instance;
					kind = (RtHeapTypeKind)255;
					break;
				case NaNBoxing.BoxType.Uint:
					as_type = Context.INT.Instance;
					kind = (RtHeapTypeKind)255;
					break;
				case NaNBoxing.BoxType.Float:
					as_type = Context.FLOAT.Instance;
					kind = (RtHeapTypeKind)255;
					break;
				case NaNBoxing.BoxType.Boolean:
					as_type = Context.BOOLEAN.Instance;
					kind = (RtHeapTypeKind)255;
					break;
				case BoxType.LocalString:
					as_type = Context.STRING.Instance;
					kind = (RtHeapTypeKind)255;
					break;
#if DEBUG
				case NaNBoxing.BoxType.Fault:
				default:
					throw new InvalidOperationException();
#else
									default:
										as_type = null;break;
#endif
			}

			var scope = methodscope; //Context.GC.Heap[scope_ptr];
#if DEBUG
			if (scope.Kind != RtHeapTypeKind.MethodScope)
			{
				throw new InvalidOperationException();
			}
#endif

			var ns_set = scope.Type._link_codescope.NamespaceSet;
			NaNBoxing thisPtr = ((RtMethodScope)methodscope).ThisPtr;
			int code = MultiNameLSearch(ns_set, kind, as_type, name, constants[const_id].HeapPtr, tmp_holder, stackslots, stackStPos, instance, check_MultiNameLSearch_issameorinherit(instance, thisPtr.ValueType == BoxType.HeapPtr ? Context.GC.Heap[thisPtr.HeapPtr] : null), ref error);

			switch (code)
			{
				case 0:

					if (stackslots[tmp_holder.index].HeapKind == (byte)RtHeapTypeKind.STACK_CACHE_OBJ)
					{
						StackLocater* tmpArgLoc = stackalloc StackLocater[2];

						SaveHeapRef(Context.GC.Heap[stackslots[tmp_holder.index].HeapPtr], source, stackslots,stackalloc char[128] , tmpArgLoc, scope_ptr, stackStPos, methodscope, ref error);

						if (error.raised)
						{
							goto flag_handle_error;
						}
					}
					else
					{

						Debug.Assert(stackslots[tmp_holder.index].HeapKind == (byte)RtHeapTypeKind.CLOSURE);
						RaiseReferenceError_WriteToMethod(ref error, (ASMethodBody)Context.GC.Heap[stackslots[tmp_holder.index].HeapPtr].Type, ((RtClosure)Context.GC.Heap[stackslots[tmp_holder.index].HeapPtr])._ref_as_type.QName);
						//throw new NotImplementedException($"Cannot assign to a method { cache.Type.QName.Name } on { ((RtPayloadClosure)cache)._ref_as_type.QName.Name }.");
						goto flag_handle_error;

					}


					break;
				case 1:
					goto flag_handle_error;
				case 2:
					Context.GC.CheckGC(ref error);
					RaiseTypeError_Ambiguous(ref error, name);
					goto flag_handle_error;
				//case 3:
				//   RaiseReferenceError_MulitNameNotFound(ref error, name, as_type.QName);
				//   goto flag_handle_error;
#if DEBUG
				default:
					throw new InvalidOperationException();
#endif
			}


		flag_handle_error:
			;




		}


		private unsafe void Store_MultiNameL_Slow(StackLocater source,  StackLocater instance_loc, int super_const_index, StackLocater tmp_holder, StackLocater _name, Span<NaNBoxing> constants,
			RtHeapBase methodscope,
			Span<NaNBoxing> stackslots, int stackStPos, int scope_ptr, ref ReceiveError error)
		{

			StackLocater* tmpArgLoc = stackalloc StackLocater[2];

			NaNBoxing instance_box;
			RtHeapTypeKind kind;
			ASContainer as_type = null;

			Debug.Assert(instance_loc.index >= 0);

			//if (instance_loc.index >= 0)
			{
				instance_box = stackslots[instance_loc.index];
				kind = (RtHeapTypeKind)(instance_box.ValueType == BoxType.HeapPtr ? instance_box.HeapKind : 255);
			}
			//else
			//{
			//	ReadInstanceFromStacklocater(ref error, instance_loc, stackslots, stackStPos, scope_ptr, out kind, out instance_box);
			//	if (error.raised)
			//	{
			//		goto flag_handle_error;
			//	}
			//}

			if (super_const_index != 0)
			{
				//读基类
				super_const_index -= 1;

				var vbox = constants[super_const_index];

#if DEBUG
				if (vbox.ValueType != NaNBoxing.BoxType.Uint)
					throw new InvalidOperationException();
#endif

				var super_class = Context.link_const_class[(int)vbox.UIntValue];

#if DEBUG
				var check = GetASTypeFromValue(instance_box);
				if (check is ASInstance)
				{
					if (!((ASInstance)check).IsExtend(super_class.Instance))
					{
						throw new InvalidOperationException();
					}
				}

#endif

				as_type = super_class.Instance;
			}

			//RtHeapBase instance = null;
			bool setinstance = false;
			switch (instance_box.ValueType)
			{
				case NaNBoxing.BoxType.Null:
					Context.GC.CheckGC(ref error);
					RaiseTypeError_AccessNull(ref error);
					goto flag_handle_error;
				case NaNBoxing.BoxType.Undefined:
					Context.GC.CheckGC(ref error);
					RaiseTypeError_ATermUndefined(ref error);
					goto flag_handle_error;
				case NaNBoxing.BoxType.HeapPtr:
					break;
				case NaNBoxing.BoxType.Sbyte:
					as_type = Context.SBYTE.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case NaNBoxing.BoxType.Byte:
					as_type = Context.BYTE.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case NaNBoxing.BoxType.Short:
					as_type = Context.SHORT.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case NaNBoxing.BoxType.UShort:
					as_type = Context.USHORT.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case NaNBoxing.BoxType.Int:
					as_type = Context.INT.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case NaNBoxing.BoxType.Uint:
					as_type = Context.UINT.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case NaNBoxing.BoxType.Float:
					as_type = Context.FLOAT.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case NaNBoxing.BoxType.Number:
					as_type = Context.NUMBER.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case NaNBoxing.BoxType.Boolean:
					as_type = Context.BOOLEAN.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case BoxType.LocalString:
					as_type = Context.STRING.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;

#if DEBUG
				case NaNBoxing.BoxType.Fault:
				default:
					throw new InvalidOperationException();
#endif
			}


			setinstance = true;
		lbl_instance_primitive:
			Span<char> buffers = stackalloc char[128];
			ReadOnlySpan<char> name = buffers;

			NaNBoxing prop_name = stackslots[_name.index];

			if (setinstance && (
				instance_box.HeapKind == (byte)RtHeapTypeKind.INSTANCE
				&&
				((ASInstance)Context.GC.Heap[instance_box.HeapPtr].Type).Flags.HasFlag(ClassFlags.Indexer)
				)
				||

				(instance_box.HeapKind == (byte)RtHeapTypeKind.VECTOR && RtVector.IsValidIndexType(prop_name))

				)
			{
				//索引器处理
				int ptrIndex = stackStPos + tmp_holder.index;
				int cacheobjpointer = Context.CacheObjPtr + ptrIndex;  //Context.CacheObjectPointers[ptrIndex];
				RtHeapBase cache = Context.GC.Heap[cacheobjpointer];
#if DEBUG
				if (cache.Kind != RtHeapTypeKind.STACK_CACHE_OBJ)
				{
					throw new InvalidOperationException();
				}
#endif

				if (instance_box.HeapKind == (byte)RtHeapTypeKind.VECTOR && RtVector.IsValidIndexType(prop_name))
				{

					Context.GC.CheckGC(ref error);
					if (Context.StackPosition >= Context.STACK_LENGTH)
					{
						RaiseStackOverflow(ref error);
						goto flag_handle_error;
					}

					//RtVector vector = ((RtVector)instance);
					RtVector vector;
					int vptr = RtVector.FindAndUpdateHeapInstancePtr(instance_box.HeapPtr, this, out vector);

					ref NaNBoxing conv = ref Context.StackSlots[Context.StackPosition];
					Context.StackPosition++;

					ConvertValueType(ref error, stackslots[source.index], vector.element_type, vector.element_asclass, ref conv);//, scope_ptr, ((RtMethodScope)methodscope).ThisPtr);
					if (error.raised)
					{
						Context.StackPosition--;
						goto flag_handle_error;
					}
					//为性能考虑，阻止ConvertValueType调函数


					int validid;
					var store = ((RtVector)vector).GetStore();
					if (!(store.IsValidIndexRange(prop_name, out validid)))
					{
						int maxlen = store.length;
						if (validid == maxlen && maxlen < int.MaxValue) //扩容
						{
							((RtVector)vector).Resize(validid + 1, ref error, this, (ASInstance)vector.Type, out VectorImpl.VectorStore resizedstore);

							if (error.raised)
							{
								Context.StackPosition--;
								goto flag_handle_error;
							}

							//throw new NotImplementedException();
						}
						else
						{
							Context.StackPosition--;
							RaiseRangeError(ref error, Extensions.GetPrimitiveValueToString(this, prop_name, buffers), maxlen);
							goto flag_handle_error;
						}
					}

					vector.SetSlot(validid, this, vptr, conv, ref error);

					Context.StackPosition--;

					if (error.raised)
					{
						goto flag_handle_error;
					}



				}
				else
				{
					RtStackCache cachePayload = (RtStackCache)cache;
					cachePayload.RefInstance = instance_box;
					cachePayload.trait[0] = null; cachePayload.trait[1] = null;
					cachePayload.scopemember_index = 0;
					cachePayload.searchPropertyName.SetUndefined(); cachePayload.as_type = Context.GC.Heap[instance_box.HeapPtr].Type;
					cachePayload.searchNameSpacePtr = 0; cachePayload.indexer_key = prop_name;


					SaveHeapRef(cachePayload, source, stackslots, buffers, tmpArgLoc, scope_ptr, stackStPos, methodscope, ref error);
					if (error.raised)
					{
						goto flag_handle_error;
					}
					//stackslots[stack.index].SetHeapPtr(cacheobjpointer, (byte)RtHeapTypeKind.STACK_CACHE_OBJ, (byte)HeapKindFlag.NONE);
				}
				return;
			}
			else if (prop_name.ValueType != NaNBoxing.BoxType.HeapPtr)
			{
				if (setinstance && instance_box.HeapKind == (byte)RtHeapTypeKind.ARRAY)
				{
					long index;

					switch (prop_name.ValueType)
					{
						case BoxType.LocalString:
							// Use efficient char-based extraction to avoid string allocation

							int charCount = prop_name.GetLocalStringChars(buffers);
							name = charCount > 0 ? buffers.Slice(0, charCount) : ReadOnlySpan<char>.Empty;
							goto lbl_name_solved;
						case NaNBoxing.BoxType.Number:
							{
								double v = prop_name.Number;
								if (v >= 0 && (v == Math.Truncate(v)) && !Double.IsInfinity(v) && v < uint.MaxValue)
								{
									index = (long)v;
									if (index >= 0 && index < uint.MaxValue)
									{
										goto array_index;
									}
									else
									{
										name = index.ToString();
										goto array_prop;
									}
								}
								else
								{
									name = Extensions.GetPrimitiveValueToString(this, prop_name, buffers);
									goto array_prop;
								}
							}
						case NaNBoxing.BoxType.Float:
							{
								double v = prop_name.FloatValue;
								if (v >= 0 && (v == Math.Truncate(v)) && !Double.IsInfinity(v) && v < uint.MaxValue)
								{
									index = (long)v;
									if (index >= 0 && index < uint.MaxValue)
									{
										goto array_index;
									}
									else
									{
										name = index.ToString();
										goto array_prop;
									}
								}
								else
								{
									name = Extensions.GetPrimitiveValueToString(this, prop_name, buffers);
									goto array_prop;
								}
							}
						case NaNBoxing.BoxType.Undefined:
							name = "undefined";
							goto array_prop;
						case NaNBoxing.BoxType.Null:
							name = "null";
							goto array_prop;
						case NaNBoxing.BoxType.Boolean:
							name = prop_name.Boolean ? "true" : "false";
							goto array_prop;
						case NaNBoxing.BoxType.Int:
							{
								index = prop_name.IntValue;
								if (index >= 0)
								{
									goto array_index;
								}
								else
								{
									name = index.ToString();
									goto array_prop;
								}
							}
						case NaNBoxing.BoxType.Uint:
							{
								index = prop_name.UIntValue;
								if (index < uint.MaxValue)
								{
									goto array_index;
								}
								else
								{
									name = index.ToString();
									goto array_prop;
								}
							}
						case NaNBoxing.BoxType.Sbyte:
							{
								index = prop_name.SByteValue;
								if (index >= 0)
								{
									goto array_index;
								}
								else
								{
									name = index.ToString();
									goto array_prop;
								}
							}
						case NaNBoxing.BoxType.Byte:
							{
								index = prop_name.ByteValue;
								goto array_index;
							}
						case NaNBoxing.BoxType.Short:
							{
								index = prop_name.ShortValue;
								if (index >= 0)
								{
									goto array_index;
								}
								else
								{
									name = index.ToString();
									goto array_prop;
								}
							}
						case NaNBoxing.BoxType.UShort:
							{
								index = prop_name.UShortValue;
								goto array_index;
							}
#if DEBUG
						case NaNBoxing.BoxType.Fault:
						default:
							throw new InvalidOperationException();
#else
						default:
							Environment.FailFast("出错了，这里跑不到");

							error.error.setFault();
							goto flag_handle_error;
#endif
					}

				//索引处理
				array_index:
					uint array_i = (uint)index;
					int ptrIndex = stackStPos + tmp_holder.index;
					int cacheobjpointer = Context.CacheObjPtr + ptrIndex;  //Context.CacheObjectPointers[ptrIndex];
					RtHeapBase cache = Context.GC.Heap[cacheobjpointer];
#if DEBUG
					if (cache.Kind != RtHeapTypeKind.STACK_CACHE_OBJ)
					{
						throw new InvalidOperationException();
					}
#endif

					RtHeapBase instance = Context.GC.Heap[instance_box.HeapPtr];

					SetArraySlot(stackslots[source.index], array_i, instance, ref error);
					if (error.raised)
					{
						goto flag_handle_error;
					}



					//RtStackCache cachePayload = (RtStackCache)cache;
					//cachePayload.RefInstance = instance_box;
					//cachePayload.trait[0] = null; cachePayload.trait[1] = null;
					//cachePayload.scopemember_index = 0;
					//cachePayload.searchPropertyName.SetUndefined(); cachePayload.as_type = Context.GC.Heap[instance_box.HeapPtr].Type;
					//cachePayload.searchNameSpacePtr = 0; cachePayload.indexer_key.SetUInt(array_i);

					//SaveHeapRef(cachePayload, source, stackslots, frame_holdchars, tmpArgLoc, scope_ptr, stackStPos, methodscope, ref error);
					//if (error.raised)
					//{
					//	goto flag_handle_error;
					//}



					return;

				array_prop:;


				}

				else if (setinstance && instance_box.HeapKind == (byte)RtHeapTypeKind.VECTOR)
				{
					//不合理的索引范围
#if DEBUG
					if (RtVector.IsValidIndexType(prop_name))
					{
						throw new InvalidOperationException();
					}
#endif

					name = Extensions.GetPrimitiveValueToString(this, prop_name, buffers);
				}
				else
				{
					name = Extensions.GetPrimitiveValueToString(this, prop_name, buffers);
					//throw new NotImplementedException("转字符串？还是数组？");
				}
			}
			else
			{

				//RtHeapBase _n = Context.GC.Heap[prop_name.HeapPtr];
				if (prop_name.HeapKind != (byte)RtHeapTypeKind.STRING)
				{
					if (Context.StackPosition + 1 >= Context.STACK_LENGTH)
					{
						RaiseStackOverflow(ref error);
						goto flag_handle_error;
					}

					var span = Context.StackSlots.AsSpan(Context.StackPosition, 2); span.Clear();
					StackLocater tmp = default; tmp.index = 0; StackLocater tmp2 = default; tmp2.index = 1;
					int stpos = Context.StackPosition;
					Context.StackPosition+=2;
					NaNBoxing primitive_name = ToPrimitive(ref error, prop_name, HINT.h_string, scope_ptr, tmp, tmp2, span, stpos, ((RtMethodScope)methodscope).ThisPtr);
					if (error.raised)
					{
						Context.StackPosition = stpos;
						goto flag_handle_error;
					}

					name = Extensions.GetPrimitiveValueToString(this, primitive_name, buffers);
					Context.StackPosition = stpos;


					//throw new NotImplementedException("转字符串？");
				}
				else
				{
					RtHeapBase _n = Context.GC.Heap[prop_name.HeapPtr];
					name = ((RtString)_n).Str;
				}

			}

		lbl_name_solved:

			var scope = methodscope; //Context.GC.Heap[scope_ptr];
#if DEBUG
			if (scope.Kind != RtHeapTypeKind.MethodScope)
			{
				throw new InvalidOperationException();
			}
#endif

			if (as_type == null)
			{
				as_type = GetASTypeFromValue(instance_box);
			}

			if (Context.StackPosition == Context.STACK_LENGTH)
			{
				RaiseStackOverflow(ref error);
				goto flag_handle_error;
			}



			var ns_set = scope.Type._link_codescope.NamespaceSet;
			NaNBoxing thisPtr = ((RtMethodScope)methodscope).ThisPtr;
			int code = MultiNameLSearch(ns_set, kind, as_type, name, 0, tmp_holder, stackslots, stackStPos, instance_box, check_MultiNameLSearch_issameorinherit(instance_box, thisPtr.ValueType == BoxType.HeapPtr ? Context.GC.Heap[thisPtr.HeapPtr] : null), ref error);


			switch (code)
			{
				case 0:

					if (stackslots[tmp_holder.index].HeapKind == (byte)RtHeapTypeKind.STACK_CACHE_OBJ)
					{

						SaveHeapRef(Context.GC.Heap[stackslots[tmp_holder.index].HeapPtr], source, stackslots, buffers, tmpArgLoc, scope_ptr, stackStPos, methodscope, ref error);

						if (error.raised)
						{
							goto flag_handle_error;
						}
					}
					else
					{

						Debug.Assert(stackslots[tmp_holder.index].HeapKind == (byte)RtHeapTypeKind.CLOSURE);
						RaiseReferenceError_WriteToMethod(ref error, (ASMethodBody)Context.GC.Heap[stackslots[tmp_holder.index].HeapPtr].Type, ((RtClosure)Context.GC.Heap[stackslots[tmp_holder.index].HeapPtr])._ref_as_type.QName);
						//throw new NotImplementedException($"Cannot assign to a method { cache.Type.QName.Name } on { ((RtPayloadClosure)cache)._ref_as_type.QName.Name }.");
						goto flag_handle_error;

					}


					break;
				case 1:

					goto flag_handle_error;
				case 2:

					Context.GC.CheckGC(ref error);
					RaiseTypeError_Ambiguous(ref error, name);
					goto flag_handle_error;
#if DEBUG
				//case 3:
				//    RaiseReferenceError_MulitNameNotFound(ref error, name, as_type.QName);
				//    goto flag_handle_error;
				default:

					throw new InvalidOperationException();
#endif
			}

		flag_handle_error:
			;

		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private unsafe void Store_MultiNameL( int dst_index,byte** PC , Span<NaNBoxing> constants,
			Span<NaNBoxing> stackslots,
			int stackStPos, int scope_ptr, RtHeapBase methodscope,
			ref ReceiveError error)
		{
			uint* opcodePtr = (uint*)*PC - 1; Debug.Assert((*opcodePtr & 0xff) == (byte)INS_Code.store_MultiNameL);

			StackLocater source;
			source.index = dst_index;

			StackLocater instance_loc;
			LoadStackLocater(&instance_loc, PC);

			StackLocater _name;
			LoadStackLocater(&_name, PC);

			StackLocater tmp_holder;
			LoadStackLocater(&tmp_holder, PC);

			int super_const_index;
			LoadInt32(&super_const_index, PC);

			var instance_box = stackslots[instance_loc.index];
			var name_box = stackslots[_name.index];

			Debug.Assert(instance_loc.index >= 0);

			if (instance_box.HeapKind == (byte)RtHeapTypeKind.ARRAY
				&& name_box.ValueType >= BoxType.Int && name_box.ValueType <= BoxType.UShort &&
				(name_box.IntValue >= 0 || (name_box.ValueType == BoxType.Uint && name_box.UIntValue < uint.MaxValue)))
			{
				uint array_i = name_box.ValueType == BoxType.Uint ? name_box.UIntValue : (uint)name_box.IntValue;
				RtHeapBase instance = Context.GC.Heap[instance_box.HeapPtr];

				SetArraySlot(stackslots[source.index], array_i, instance, ref error);
				return;
			}
			else if (instance_box.HeapKind == (byte)RtHeapTypeKind.VECTOR
				&& RtVector.IsValidIndexType(name_box)
				)
			{
				//打补丁
				//*opcodePtr = ((uint)INS_Code.store_Vector | (0xffffff00 & (*opcodePtr)));



				Context.GC.CheckGC(ref error);
				if (Context.StackPosition >= Context.STACK_LENGTH)
				{
					RaiseStackOverflow(ref error);
					return;
				}

				//RtVector vector = ((RtVector)instance);
				RtVector vector;
				int vptr = RtVector.FindAndUpdateHeapInstancePtr(instance_box.HeapPtr, this, out vector);

				ref NaNBoxing conv = ref Context.StackSlots[Context.StackPosition];
				Context.StackPosition++;

				ConvertValueType(ref error, stackslots[source.index], vector.element_type, vector.element_asclass, ref conv);//, scope_ptr, ((RtMethodScope)methodscope).ThisPtr);
				if (error.raised)
				{
					Context.StackPosition--;
					return;
				}
				//为性能考虑，阻止ConvertValueType调函数


				int validid;
				var store = ((RtVector)vector).GetStore();
				if (!(store.IsValidIndexRange(name_box, out validid)))
				{
					int maxlen = store.length;
					if (validid == maxlen && maxlen < int.MaxValue) //扩容
					{
						((RtVector)vector).Resize(validid + 1, ref error, this, (ASInstance)vector.Type, out VectorImpl.VectorStore resizedstore);

						if (error.raised)
						{
							Context.StackPosition--;
							return;
						}

						//throw new NotImplementedException();
					}
					else
					{
						Context.StackPosition--;
						RaiseRangeError(ref error, Extensions.GetPrimitiveValueToString(this, name_box, stackalloc char[128]), maxlen);
						return;
					}
				}

				vector.SetSlot(validid, this, vptr, conv, ref error);

				Context.StackPosition--;

				if (error.raised)
				{
					return;
				}

			}
			else
			{
				Store_MultiNameL_Slow(source, instance_loc, super_const_index, tmp_holder, _name, constants, methodscope, stackslots, stackStPos, scope_ptr, ref error);
			}
		

		}


		



		private unsafe void Ld_MultiNameL_Val_Slow(int dst_index,uint* opcodePtr, StackLocater src, StackLocater stack,StackLocater _name, Span<NaNBoxing> constants,

			ASMethod method, RtHeapBase methodscope,

			Span<NaNBoxing> stackslots, int stackStPos, int scope_ptr, ref ReceiveError error)
		{


			NaNBoxing instance_box;
			RtHeapTypeKind kind;
			ASContainer as_type = null;

			//if (src.index >= 0)
			{
				instance_box = stackslots[src.index];
				kind = (RtHeapTypeKind)(instance_box.ValueType == BoxType.HeapPtr ? instance_box.HeapKind : 255);
			}
			//else
			//{
			//	ReadInstanceFromStacklocater(ref error, src, stackslots, stackStPos, scope_ptr, out kind, out instance_box);
			//	if (error.raised)
			//	{
			//		goto flag_handle_error;
			//	}
			//}

//			if (super_const_index != 0)
//			{
//				//读基类
//				super_const_index -= 1;

//				var vbox = constants[super_const_index];

//#if DEBUG
//				if (vbox.ValueType != NaNBoxing.BoxType.Uint)
//					throw new InvalidOperationException();
//#endif

//				var super_class = Context.link_const_class[(int)vbox.UIntValue];

//#if DEBUG
//				var check = GetASTypeFromValue(instance_box);
//				if (check is ASInstance)
//				{
//					if (!((ASInstance)check).IsExtend(super_class.Instance))
//					{
//						throw new InvalidOperationException();
//					}
//				}

//#endif

//				as_type = super_class.Instance;
//			}

			//RtHeapBase instance = null;
			bool setinstance = false;
			switch (instance_box.ValueType)
			{
				case NaNBoxing.BoxType.Null:
					Context.GC.CheckGC(ref error);
					RaiseTypeError_AccessNull(ref error);
					goto flag_handle_error;
				case NaNBoxing.BoxType.Undefined:
					Context.GC.CheckGC(ref error);
					RaiseTypeError_ATermUndefined(ref error);
					goto flag_handle_error;
				case NaNBoxing.BoxType.HeapPtr:
					break;
				case NaNBoxing.BoxType.Sbyte:
					as_type = Context.SBYTE.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case NaNBoxing.BoxType.Byte:
					as_type = Context.BYTE.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case NaNBoxing.BoxType.Short:
					as_type = Context.SHORT.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case NaNBoxing.BoxType.UShort:
					as_type = Context.USHORT.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case NaNBoxing.BoxType.Int:
					as_type = Context.INT.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case NaNBoxing.BoxType.Uint:
					as_type = Context.UINT.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case NaNBoxing.BoxType.Float:
					as_type = Context.FLOAT.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case NaNBoxing.BoxType.Number:
					as_type = Context.NUMBER.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case NaNBoxing.BoxType.Boolean:
					as_type = Context.BOOLEAN.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;
				case BoxType.LocalString:
					as_type = Context.STRING.Instance;
					kind = (RtHeapTypeKind)255;
					goto lbl_instance_primitive;

#if DEBUG
				case NaNBoxing.BoxType.Fault:
				default:
					throw new InvalidOperationException();
#endif
			}


			setinstance = true;
		lbl_instance_primitive:
			Span<char> buffers = stackalloc char[128];
			ReadOnlySpan<char> name = buffers;

			NaNBoxing prop_name = stackslots[_name.index];

			if (setinstance && (
				instance_box.HeapKind == (byte)RtHeapTypeKind.INSTANCE
				&&
				((ASInstance)Context.GC.Heap[instance_box.HeapPtr].Type).Flags.HasFlag(ClassFlags.Indexer)
				)
				||

				(instance_box.HeapKind == (byte)RtHeapTypeKind.VECTOR && RtVector.IsValidIndexType(prop_name))

				)
			{
				//索引器处理
				int ptrIndex = stackStPos + stack.index;
				int cacheobjpointer = Context.CacheObjPtr + ptrIndex;  //Context.CacheObjectPointers[ptrIndex];
				RtHeapBase cache = Context.GC.Heap[cacheobjpointer];
#if DEBUG
				if (cache.Kind != RtHeapTypeKind.STACK_CACHE_OBJ)
				{
					throw new InvalidOperationException();
				}
#endif
				if (instance_box.HeapKind == (byte)RtHeapTypeKind.VECTOR)
				{
					RtVector vector;
					int v_ptr = RtVector.FindAndUpdateHeapInstancePtr(instance_box.HeapPtr, this, out vector);
					//int maxlen; int validid;
					var store = vector.GetStore();
					if (!(store.IsValidIndexRange(prop_name, out int validid)))
					{
						RaiseRangeError(ref error, Extensions.GetPrimitiveValueToString(this, prop_name, buffers), store.length);
						goto flag_handle_error;
					}
					else
					{
						stackslots[dst_index] = store.ReadSlot(vector.element_type, validid, this, v_ptr, stackStPos + dst_index, vector.element_asclass);
					}
				}
				else
				{
					RtStackCache cachePayload = (RtStackCache)cache;
					cachePayload.RefInstance = instance_box;
					cachePayload.trait[0] = null; cachePayload.trait[1] = null;
					cachePayload.scopemember_index = 0;
					cachePayload.searchPropertyName.SetUndefined(); cachePayload.as_type = Context.GC.Heap[instance_box.HeapPtr].Type;
					cachePayload.searchNameSpacePtr = 0; cachePayload.indexer_key = prop_name;

					stackslots[dst_index] = LoadValue(cachePayload,
						stackStPos - method.Body._link_codescope.Members.Count - 2, ref error, stackslots, stackStPos + dst_index
						);
					if (error.raised)
					{
						goto flag_handle_error;
					}

				}






				return;
			}
			else if (prop_name.ValueType != NaNBoxing.BoxType.HeapPtr)
			{
				if (setinstance && instance_box.HeapKind == (byte)RtHeapTypeKind.ARRAY)
				{
					long index;

					switch (prop_name.ValueType)
					{
						case BoxType.LocalString:
							// Use efficient char-based extraction to avoid string allocation

							int charCount = prop_name.GetLocalStringChars(buffers);
							name = charCount > 0 ? buffers.Slice(0, charCount) : ReadOnlySpan<char>.Empty;
							goto lbl_name_solved;
						case NaNBoxing.BoxType.Number:
							{
								double v = prop_name.Number;
								if (v >= 0 && (v == Math.Truncate(v)) && !Double.IsInfinity(v) && v < uint.MaxValue)
								{
									index = (long)v;
									if (index >= 0 && index < uint.MaxValue)
									{
										goto array_index;
									}
									else
									{
										name = index.ToString();
										goto array_prop;
									}
								}
								else
								{
									name = Extensions.GetPrimitiveValueToString(this, prop_name, buffers);
									goto array_prop;
								}
							}
						case NaNBoxing.BoxType.Float:
							{
								double v = prop_name.FloatValue;
								if (v >= 0 && (v == Math.Truncate(v)) && !Double.IsInfinity(v) && v < uint.MaxValue)
								{
									index = (long)v;
									if (index >= 0 && index < uint.MaxValue)
									{
										goto array_index;
									}
									else
									{
										name = index.ToString();
										goto array_prop;
									}
								}
								else
								{
									name = Extensions.GetPrimitiveValueToString(this, prop_name, buffers);
									goto array_prop;
								}
							}
						case NaNBoxing.BoxType.Undefined:
							name = "undefined";
							goto array_prop;
						case NaNBoxing.BoxType.Null:
							name = "null";
							goto array_prop;
						case NaNBoxing.BoxType.Boolean:
							name = prop_name.Boolean ? "true" : "false";
							goto array_prop;
						case NaNBoxing.BoxType.Int:
							{
								index = prop_name.IntValue;
								if (index >= 0)
								{
									goto array_index;
								}
								else
								{
									name = index.ToString();
									goto array_prop;
								}
							}
						case NaNBoxing.BoxType.Uint:
							{
								index = prop_name.UIntValue;
								if (index < uint.MaxValue)
								{
									goto array_index;
								}
								else
								{
									name = index.ToString();
									goto array_prop;
								}
							}
						case NaNBoxing.BoxType.Sbyte:
							{
								index = prop_name.SByteValue;
								if (index >= 0)
								{
									goto array_index;
								}
								else
								{
									name = index.ToString();
									goto array_prop;
								}
							}
						case NaNBoxing.BoxType.Byte:
							{
								index = prop_name.ByteValue;
								goto array_index;
							}
						case NaNBoxing.BoxType.Short:
							{
								index = prop_name.ShortValue;
								if (index >= 0)
								{
									goto array_index;
								}
								else
								{
									name = index.ToString();
									goto array_prop;
								}
							}
						case NaNBoxing.BoxType.UShort:
							{
								index = prop_name.UShortValue;
								goto array_index;
							}
#if DEBUG
						case NaNBoxing.BoxType.Fault:
						default:
							throw new InvalidOperationException();
#else
						default:
							Environment.FailFast("出错了，这里跑不到");

							error.error.setFault();
							goto flag_handle_error;
#endif
					}

				//索引处理
				array_index:
					uint array_i = (uint)index;
					//int ptrIndex = stackStPos + stack.index;
					//int cacheobjpointer = Context.CacheObjPtr + ptrIndex;  //Context.CacheObjectPointers[ptrIndex];

					bool isoutofindex_or_ishole;
					var a_element = LoadSlotFromArray(array_i, Context.GC.Heap[instance_box.HeapPtr], out isoutofindex_or_ishole);

					if (a_element.ValueType == BoxType.Fault)
					{
						a_element.SetUndefined();
					}
					else if (a_element.IsStruct())//v.ValueType == BoxType.HeapPtr && v.HeapKind == (byte)RtHeapTypeKind.INSTANCE && v.HeapFlag &)
					{
						a_element.SetHeapPtr(a_element.HeapPtr, (byte)RtHeapTypeKind.INSTANCE, (byte)(HeapKindFlag.FLAG_STRUCT | HeapKindFlag.FLAG_REFSTRUCT));

					}

					stackslots[dst_index] = a_element;

					//										//quickening
					//#if FORCOMPILER
					//										if (!IsComputeConstExpr)
					//										{
					//#endif
					//											if (super_const_index == 0 && src.index >=0 && 
					//											(prop_name.ValueType == BoxType.Int || prop_name.ValueType == BoxType.Byte 
					//												|| prop_name.ValueType == BoxType.Sbyte || prop_name.ValueType ==  BoxType.Short || prop_name.ValueType == BoxType.UShort) )
					//											{
					//												*opcodePtr = ((uint)INS_Code.ld_MultiNameL_Ref_ARR_INT | (0xffffff00 & (*opcodePtr)));
					//											}

					//#if FORCOMPILER
					//										}
					//#endif


					return;

				array_prop:;


				}

				else if (setinstance && instance_box.HeapKind == (byte)RtHeapTypeKind.VECTOR)
				{
					//不合理的索引范围
#if DEBUG
					if (RtVector.IsValidIndexType(prop_name))
					{
						throw new InvalidOperationException();
					}
#endif

					name = Extensions.GetPrimitiveValueToString(this, prop_name, buffers);
				}
				else
				{
					name = Extensions.GetPrimitiveValueToString(this, prop_name, buffers);
					//throw new NotImplementedException("转字符串？还是数组？");
				}
			}
			else
			{

				//RtHeapBase _n = Context.GC.Heap[prop_name.HeapPtr];
				if (prop_name.HeapKind != (byte)RtHeapTypeKind.STRING)
				{
					if (Context.StackPosition + 1 >= Context.STACK_LENGTH)
					{
						RaiseStackOverflow(ref error);
						goto flag_handle_error;
					}

					var span = Context.StackSlots.AsSpan(Context.StackPosition, 2); span.Clear();
					StackLocater tmp = default; tmp.index = 0; StackLocater tmp2 = default; tmp2.index = 1;
					int stpos = Context.StackPosition;
					Context.StackPosition +=2;
					NaNBoxing primitive_name = ToPrimitive(ref error, prop_name, HINT.h_string, scope_ptr, tmp, tmp2, span, stpos, ((RtMethodScope)methodscope).ThisPtr);
					if (error.raised)
					{
						Context.StackPosition = stpos;
						goto flag_handle_error;
					}

					name = Extensions.GetPrimitiveValueToString(this, primitive_name, buffers);
					Context.StackPosition = stpos;


					//throw new NotImplementedException("转字符串？");
				}
				else
				{
					RtHeapBase _n = Context.GC.Heap[prop_name.HeapPtr];
					name = ((RtString)_n).Str;
				}

			}

		lbl_name_solved:

			var scope = methodscope; //Context.GC.Heap[scope_ptr];
#if DEBUG
			if (scope.Kind != RtHeapTypeKind.MethodScope)
			{
				throw new InvalidOperationException();
			}
#endif

			if (as_type == null)
			{
				as_type = GetASTypeFromValue(instance_box);
			}

			var ns_set = scope.Type._link_codescope.NamespaceSet;
			NaNBoxing thisPtr = ((RtMethodScope)methodscope).ThisPtr;
			int code = MultiNameLSearch(ns_set, kind, as_type, name, 0, stack, stackslots, stackStPos, instance_box, check_MultiNameLSearch_issameorinherit(instance_box, thisPtr.ValueType == BoxType.HeapPtr ? Context.GC.Heap[thisPtr.HeapPtr] : null), ref error);

			switch (code)
			{
				case 0:

					if (stackslots[stack.index].HeapKind == (byte)RtHeapTypeKind.STACK_CACHE_OBJ)
					{
						stackslots[dst_index] = LoadValue((RtStackCache)Context.GC.Heap[stackslots[stack.index].HeapPtr],
							stackStPos - method.Body._link_codescope.Members.Count - 2, ref error, stackslots, stackStPos + dst_index
						);
						if (error.raised)
						{
							goto flag_handle_error;
						}
					}
					else
					{
						stackslots[dst_index] = stackslots[stack.index];
					}

					break;
				case 1:
					goto flag_handle_error;
				case 2:
					Context.GC.CheckGC(ref error);
					RaiseTypeError_Ambiguous(ref error, name);
					goto flag_handle_error;
#if DEBUG
				//case 3:
				//    RaiseReferenceError_MulitNameNotFound(ref error, name, as_type.QName);
				//    goto flag_handle_error;
				default:
					throw new InvalidOperationException();
#endif
			}


		flag_handle_error:
			;
		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private unsafe void Ld_MultiNameL_Val(int dst_index,byte** PC, Span<NaNBoxing> constants,
			
			ASMethod method,RtHeapBase methodscope,

			Span<NaNBoxing> stackslots,int stackStPos,int scope_ptr,ref ReceiveError error)
		{
			uint* opcodePtr = (uint*)*PC - 1; Debug.Assert((*opcodePtr & 0xff) == (byte)INS_Code.ld_MultiNameL_Val);


			
			StackLocater src;
			LoadStackLocater(&src, PC);

			StackLocater _name;
			LoadStackLocater(&_name, PC);

			StackLocater refholder_index;
			LoadStackLocater(&refholder_index, PC);

			


			var instance_box = stackslots[src.index];
			var name_box = stackslots[_name.index];

			Debug.Assert(src.index >= 0);

			if (//src.index >= 0 && 
				
				instance_box.HeapKind == (byte)RtHeapTypeKind.ARRAY
				&& name_box.ValueType >= BoxType.Int && name_box.ValueType <= BoxType.UShort &&
				(name_box.IntValue >= 0 || (name_box.ValueType == BoxType.Uint && name_box.UIntValue < uint.MaxValue)))
			{

				uint array_i = name_box.ValueType == BoxType.Uint ? name_box.UIntValue : (uint)name_box.IntValue;

				bool isoutofindex_or_ishole;
				var a_element = LoadSlotFromArray(array_i, Context.GC.Heap[instance_box.HeapPtr], out isoutofindex_or_ishole);

				if (a_element.ValueType == BoxType.Fault)
				{
					a_element.SetUndefined();
				}
				else if (a_element.IsStruct())//v.ValueType == BoxType.HeapPtr && v.HeapKind == (byte)RtHeapTypeKind.INSTANCE && v.HeapFlag &)
				{
					a_element.SetHeapPtr(a_element.HeapPtr, (byte)RtHeapTypeKind.INSTANCE, (byte)(HeapKindFlag.FLAG_STRUCT | HeapKindFlag.FLAG_REFSTRUCT));
				}

				stackslots[dst_index] = a_element;


				//打补丁
				//*opcodePtr = ((uint)INS_Code.ld_ARR_V | (0xffffff00 & (*opcodePtr)));

				return;

			}
			else if (instance_box.HeapKind == (byte)RtHeapTypeKind.VECTOR
				&& RtVector.IsValidIndexType(name_box)
				)
			{
				RtVector vector;
				int v_ptr = RtVector.FindAndUpdateHeapInstancePtr(instance_box.HeapPtr, this, out vector);
				//int maxlen; int validid;
				var store = vector.GetStore();
				if (!(store.IsValidIndexRange(name_box, out int validid)))
				{
					RaiseRangeError(ref error, Extensions.GetPrimitiveValueToString(this, name_box, stackalloc char[128] ), store.length);
					return;
				}
				else
				{
					stackslots[dst_index] = store.ReadSlot(vector.element_type, validid, this, v_ptr, stackStPos + dst_index, vector.element_asclass);
				}

			}
			else
			{
				Ld_MultiNameL_Val_Slow(dst_index, opcodePtr, src,  refholder_index, _name, constants, method, methodscope, stackslots, stackStPos, scope_ptr, ref error);
			}




		}



		private unsafe void Ld_ScopeH( int dst_index, byte** PC, Span<NaNBoxing> stackslots ,RtHeapBase scope, ASContainer scopeType, int stackStPos)
		{
			StackLocater stackLocater;
			stackLocater.index = dst_index;

			ScopeHeapLocater heapLocater;
			{
				heapLocater.ScopeIndex = *(ushort*)*PC; *PC += 2;
				heapLocater.MemberIndex = *(ushort*)*PC; *PC += 2;
				//byte* _p = (byte*)&heapLocater.ScopeIndex;
				//*_p++ = *PC++;
				//*_p = *PC++;

				//_p = (byte*)&heapLocater.MemberIndex;
				//*_p++ = *PC++;
				//*_p = *PC++;
			}




			var s = scope; int _parent_ptr = 0;
		label_method_parent:

			switch (s.Kind)
			{
				case RtHeapTypeKind.CLASS:
					{
						var codeScope = ((RtScriptClass)s).Meta._link_codescope;
						if (codeScope.index != heapLocater.ScopeIndex)
						{
							codeScope = codeScope.Parent;
#if DEBUG
							if (codeScope.Kind != CodeScopeKind.Script)
								throw new InvalidOperationException();
							if (codeScope.index != heapLocater.ScopeIndex)
								throw new InvalidOperationException();
#endif

							RtHeapBase sInstance = Context.GC.Heap[
							((ASScript)((RtScriptClass)s).Meta._link_codescope.Parent.Container).__global_index__];

							RtScriptClass heap = (RtScriptClass)sInstance;
							NaNBoxing value = heap.ReadSlot(heapLocater.MemberIndex);

							stackslots[stackLocater.index] = value;return;
							//return value;
						}
						else
						{
							RtScriptClass heap = (RtScriptClass)s;
							NaNBoxing value = heap.ReadSlot(heapLocater.MemberIndex);

							stackslots[stackLocater.index] = value;return;
							//return value;
						}

					}

				case RtHeapTypeKind.GLOBAL:
					{
#if DEBUG
						var codeScope = ((RtScriptClass)s).Meta._link_codescope;
						if (codeScope.index != heapLocater.ScopeIndex)
							throw new InvalidOperationException();
#endif

						RtScriptClass heap = (RtScriptClass)s;
						NaNBoxing value = heap.ReadSlot(heapLocater.MemberIndex);

						stackslots[stackLocater.index] = value;return;
						//return value;
					}

				case RtHeapTypeKind.INSTANCE:
					{
						//考虑可能继承的情况，scopeType保存上下文堆内存用的布局类型
						if (
							scopeType._link_codescope.index != heapLocater.ScopeIndex
							&&
							s.Type._link_codescope.index != heapLocater.ScopeIndex
							)
						{
							var sType = scopeType._link_codescope.Parent; //这里还是必须用scopeType来查找global.
							while (sType.Kind != CodeScopeKind.Script)
							{
								sType = sType.Parent;
							}

							//const KKK = 7; 在这种情况下发生。
							//class C extends Main
							//{
							//	/* INTERFACE II */
							//	public function B()
							//	{
							//		return function iii()
							//		{
							//				o = KKK;			
							//		}
							//	}
							//} 

							RtHeapBase sInstance = Context.GC.Heap[
									((ASScript)(sType.Container)).__global_index__];

							RtScriptClass heap = (RtScriptClass)sInstance;
							NaNBoxing value = heap.ReadSlot(heapLocater.MemberIndex);

							stackslots[stackLocater.index] = value;
							return;
							//return value;
						}
						else
						{
							NaNBoxing value = ((RtInstance)s).ReadSlot(heapLocater.MemberIndex, s.Type._link_codescope, this, stackStPos + stackLocater.index, _parent_ptr);
							stackslots[stackLocater.index] = value;
							return;
							//return value;
						}

					}

				case RtHeapTypeKind.MethodScope:
					{
						if (s.Type._link_codescope.index != heapLocater.ScopeIndex)
						{
							_parent_ptr = ((RtMethodScope)s).ParentPtr;
							s = Context.GC.Heap[_parent_ptr];
							goto label_method_parent;
						}
						else
						{
							RtMethodScope heap = (RtMethodScope)s;
							NaNBoxing value = heap.ReadSlot(heapLocater.MemberIndex
								//#if FORCOMPILER
								, this
								//#endif
								);

							stackslots[stackLocater.index] = value;return;
							//return value;
						}
					}

				case RtHeapTypeKind.STRING:
				//case RtHeapTypeKind.CACHE_LD_CLASS:
				case RtHeapTypeKind.STACK_CACHE_OBJ:
				default:
#if DEBUG
					throw new InvalidOperationException();
#else
					Environment.FailFast("出错了，这里跑不到");  return;
#endif
			}
		}



		private unsafe void Ld_function(int dst_index,byte** PC,RtHeapBase methodscope, Span<NaNBoxing> constants,Span<NaNBoxing> stackslots , int scope_ptr,int stackStPos,int* method_scopes,ref ReceiveError error)
		{
			StackLocater target;
			target.index = dst_index;

			ScopeHeapLocater heapLocater;
			{
				heapLocater.ScopeIndex = *(ushort*)*PC; *PC += 2;
				heapLocater.MemberIndex = *(ushort*)*PC; *PC += 2;
				//byte* _p = (byte*)&heapLocater.ScopeIndex;
				//*_p++ = *PC++;
				//*_p = *PC++;

				//_p = (byte*)&heapLocater.MemberIndex;
				//*_p++ = *PC++;
				//*_p = *PC++;
			}

			int function_id = 0;
			LoadInt32(&function_id, PC);

			NaNBoxing fbox = constants[function_id];
#if DEBUG
			if (fbox.ValueType != NaNBoxing.BoxType.Uint)
				throw new InvalidOperationException();
#endif

			ASMethod function = Context.link_const_methods[(int)fbox.UIntValue];
			
			RtHeapBase closure;
			Ld_function_and_store_member(function, heapLocater, methodscope, scope_ptr, ref error, stackStPos, target, stackslots, method_scopes, out closure);


		}


		private unsafe void Ld_supermethod(int dst_index,byte** PC,Span<NaNBoxing> stackslots,Span<NaNBoxing> constants,int stackStPos)
		{
			StackLocater target;
			target.index = dst_index;

			StackLocater instance;
			LoadStackLocater(&instance, PC);

			int method_id = 0;
			LoadInt32(&method_id, PC);


			NaNBoxing thisValue = stackslots[instance.index];

#if DEBUG
			if (thisValue.ValueType != NaNBoxing.BoxType.HeapPtr)
			{
				throw new InvalidOperationException();
			}
			else
			{
				RtHeapBase ins = Context.GC.Heap[thisValue.HeapPtr];
				if (ins.Kind != RtHeapTypeKind.INSTANCE)
				{
					throw new InvalidOperationException();
				}

			}

#endif

			NaNBoxing fbox = constants[method_id];

			Debug.Assert(fbox.ValueType == NaNBoxing.BoxType.Uint);


			var vtableitem = Context.link_const_vtableitems[(int)fbox.UIntValue];
			var function = vtableitem.Trait.Method;
			var define = (ASInstance)vtableitem.DefineAt;

			int ptrIndex = stackStPos + target.index;
			int closurePtr = Context.M_ClosurePtr + ptrIndex;

			var closure = Context.GC.Heap[closurePtr];
			closure.Type = function.Body;
			((RtClosure)closure).ScopePtr = thisValue.HeapPtr;
			((RtClosure)closure).ScopeType = define;
			((RtClosure)closure).This = thisValue;
			((RtClosure)closure)._ref_as_type = define;
			((RtClosure)closure).methodscopeslot_ref_state = 0;
			((RtClosure)closure).HEAPINSTANCE_PTR = 0;
			stackslots[target.index].SetHeapPtr(closurePtr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);

		}


		private unsafe void Ld_interface_method(int dst_index, byte** PC, Span<NaNBoxing> stackslots, Span<NaNBoxing> constants, int stackStPos,ref ReceiveError error)
		{
			StackLocater target;
			target.index = dst_index;

			StackLocater instance;
			LoadStackLocater(&instance, PC);

			int class_id;
			LoadInt32(&class_id, PC);

			uint vtable_ = 0;
			LoadUInt(&vtable_, PC);
			ushort vtable_index = (ushort)vtable_;

			NaNBoxing thisValue;

			Debug.Assert(instance.index >= 0);

			thisValue = stackslots[instance.index];


			if (thisValue.ValueType == NaNBoxing.BoxType.Null)
			{
				RaiseTypeError_AccessNull(ref error);
				goto flag_handle_error;
			}


			Debug.Assert(thisValue.ValueType == NaNBoxing.BoxType.HeapPtr);



			RtHeapBase ins = Context.GC.Heap[thisValue.HeapPtr];

			Debug.Assert(ins.Kind == RtHeapTypeKind.INSTANCE);

			var boxing = constants[class_id];

			Debug.Assert(boxing.ValueType == NaNBoxing.BoxType.Uint);


			var @class = Context.link_const_class[(int)boxing.UIntValue];

			Debug.Assert(@class.Instance.IsInterface);

			int m_idx = ((ASInstance)ins.Type)._interface_impl_.First((i) => i.interface_type == @class.Type_identifier)[vtable_index];

			var vtableitem = ins.Type._vtable.Items[m_idx];
			var function = vtableitem.Trait.Method;

			var define = (ASInstance)vtableitem.DefineAt;

			int ptrIndex = stackStPos + target.index;
			int closurePtr = Context.M_ClosurePtr + ptrIndex;

			var closure = Context.GC.Heap[closurePtr];
			closure.Type = function.Body;
			((RtClosure)closure).ScopePtr = thisValue.HeapPtr;
			((RtClosure)closure).ScopeType = define;
			((RtClosure)closure).This = thisValue;
			((RtClosure)closure)._ref_as_type = define;
			((RtClosure)closure).methodscopeslot_ref_state = 0;
			((RtClosure)closure).HEAPINSTANCE_PTR = 0;
			stackslots[target.index].SetHeapPtr(closurePtr, (byte)RtHeapTypeKind.CLOSURE, (byte)HeapKindFlag.NONE);


		flag_handle_error:
			;

		}



		private unsafe void Ld_length(int dst_index,byte** PC,Span<NaNBoxing> stackslots,ref ReceiveError error)
		{
			StackLocater target;
			target.index = dst_index;

			StackLocater instance;
			LoadStackLocater(&instance, PC);

			NaNBoxing thisValue = stackslots[instance.index];
			if (thisValue.ValueType == NaNBoxing.BoxType.Null)
			{
				RaiseTypeError_AccessNull(ref error);
				goto flag_handle_error;
			}
			else if (thisValue.ValueType == BoxType.LocalString)
			{
				Span<char> temp = stackalloc char[16];
				int len = thisValue.GetLocalStringChars(temp);
				stackslots[target.index].SetInt(len);

				return;
			}

			var obj = Context.GC.Heap[thisValue.HeapPtr];
			if (obj.Kind == RtHeapTypeKind.ARRAY)
			{
				uint len = ((RtArray)obj).GetLength(this);
				stackslots[target.index].SetUInt(len);
			}
			else if (obj.Kind == RtHeapTypeKind.STRING)
			{
				int len = ((RtString)obj).Str.Length;
				stackslots[target.index].SetInt(len);
			}
			else
			{
				Debug.Assert(obj.Kind == RtHeapTypeKind.VECTOR);
				int len = ((RtVector)obj).GetStore(this).length;
				stackslots[target.index].SetInt(len);
			}


		flag_handle_error:
			;
		}

		private unsafe void StoreMethodVariable_Slow(RtHeapBase methodscope, ScopeHeapLocater heapLocater, NaNBoxing value,ref NaNBoxing heapV,int scope_ptr,
			int* method_scopes,
			ref ReceiveError error)
		{
			if ((heapLocater.ScopeIndex & 0xff) == (byte)TypeKind.Any)
			{ 
			
			}
			else if ((heapLocater.ScopeIndex & 0xff) < (byte)TypeKind.Object)
			{
				ref NaNBoxing conv = ref Context.StackSlots[Context.StackPosition];
				Context.StackPosition++;
				ConvertValueType(ref error, value, (TypeKind)(heapLocater.ScopeIndex & 0xff), null, ref conv, scope_ptr, ((RtMethodScope)methodscope).ThisPtr);
				Context.StackPosition--;
				if (error.raised)
				{
					goto flag_handle_error;
				}

				value = conv;
			}
			else
			{
				var scopemember = methodscope.Type._link_codescope.Members[heapLocater.MemberIndex];

				ref NaNBoxing conv = ref Context.StackSlots[Context.StackPosition];
				Context.StackPosition++;

				var thisPtr = ((RtMethodScope)methodscope).ThisPtr;

				if (scopemember.Kind == ScopeMemberKind.Parameter)
				{
					//isheaptype = scopemember.TypeKind.IsHeapType();
					ConvertValueType(ref error, value, scopemember.TypeKind, scopemember.__rt_type_class__, ref conv, scope_ptr, thisPtr);
				}
				else
				{
					ASTrait t = scopemember.trait;
					//isheaptype = t.TypeKind.IsHeapType();
					ConvertValueType(ref error, value, t.TypeKind, t.__rt_type_class__, ref conv, scope_ptr, thisPtr);
				}
				Context.StackPosition--;
				if (error.raised)
				{
					goto flag_handle_error;
				}

				value = conv;

			}



			if (value.ValueType != BoxType.HeapPtr && heapV.ValueType != BoxType.HeapPtr)//!((TypeKind)(heapLocater.ScopeIndex & 0xff)).IsHeapType())
			{

			}
			else
			{
				int* m_scope = method_scopes;
				*m_scope++ = scope_ptr;
				PrepareSaveMethodScope((RtMethodScope)methodscope,	heapLocater, ref value, m_scope, method_scopes, ref error);

				if (error.raised)
				{

					goto flag_handle_error;
				}
			}

			heapV = value;

#if FORCOMPILER
			((RtMethodScope)methodscope).SetSlot(value, heapLocater.MemberIndex);
#endif

		flag_handle_error:
			;
		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private unsafe void StoreMethodVariable(int dst_index, byte** PC, RtHeapBase methodscope, Span<NaNBoxing> stackslots,int scope_ptr,int* method_scopes, ref ReceiveError error)
		{
			uint* opcodePtr = (uint*)*PC - 1; Debug.Assert((*opcodePtr & 0xff) == (byte)INS_Code.storeMethodVariable);

			ScopeHeapLocater heapLocater;
			{
				heapLocater.ScopeIndex = *(ushort*)*PC; *PC += 2;
				heapLocater.MemberIndex = *(ushort*)*PC; *PC += 2;
			}

			StackLocater convertedloc;LoadStackLocater(&convertedloc, PC);


			NaNBoxing value =  stackslots[dst_index];


			//#if DEBUG
			//								if (methodscope.Type._link_codescope.index != heapLocater.ScopeIndex)
			//									throw new InvalidOperationException();
			//#endif


			RtMethodScope heap = (RtMethodScope)methodscope;
			ref NaNBoxing heapV = ref heap.ReadSlotRef(heapLocater.MemberIndex);

			

			if (
				(heapLocater.ScopeIndex & 0xff) == (byte)TypeKind.Any 
				|| 
				( value.ValueType == heapV.ValueType && value.ValueType != BoxType.HeapPtr )
								
				)
			{
				if (value.ValueType != BoxType.HeapPtr && heapV.ValueType != BoxType.HeapPtr)//!((TypeKind)(heapLocater.ScopeIndex & 0xff)).IsHeapType())
				{
					heapV = value;
					stackslots[convertedloc.index] = value;
#if FORCOMPILER
					((RtMethodScope)heap).SetSlot(value, heapLocater.MemberIndex);
#endif

					return;
				}
			}

			StoreMethodVariable_Slow(methodscope, heapLocater,  value, ref heapV, scope_ptr, method_scopes, ref error);
			stackslots[convertedloc.index] = heapV;
		}


		private unsafe void StoreHeapValueRef(int dst_index,byte** PC, RtHeapBase methodscope, Span<NaNBoxing> stackslots, int stackStPos , int scope_ptr, ref ReceiveError error)
		{
			uint* opcodePtr = (uint*)*PC - 1; Debug.Assert((*opcodePtr & 0xff) == (byte)INS_Code.storeHeapValueRef);

			StackLocater target;
			StackLocater source;
			target.index = dst_index;
			LoadStackLocater(&source, PC);


			Debug.Assert(stackslots[target.index].ValueType == NaNBoxing.BoxType.HeapPtr);
			

			RtHeapBase cache = Context.GC.Heap[stackslots[target.index].HeapPtr];

			if (stackslots[target.index].HeapKind == (byte)RtHeapTypeKind.CLOSURE)
			{
				RaiseReferenceError_WriteToMethod(ref error, (ASMethodBody)cache.Type, ((RtClosure)cache)._ref_as_type.QName);

				//throw new NotImplementedException($"Cannot assign to a method { cache.Type.QName.Name } on { ((RtPayloadClosure)cache)._ref_as_type.QName.Name }.");
				goto flag_handle_error;
			}


			Debug.Assert(cache.Kind == RtHeapTypeKind.STACK_CACHE_OBJ);

			StackLocater* tmpArgLoc = stackalloc StackLocater[2];

			SaveHeapRef(cache, source, stackslots, stackalloc char[128], tmpArgLoc, scope_ptr, stackStPos, methodscope, ref error);


		flag_handle_error:
			;

		}



		private unsafe void Array_vector_initelement(int dst_index, byte** PC, Span<NaNBoxing> stackslots,ref ReceiveError error )
		{
			StackLocater instance;
			LoadStackLocater(&instance, PC);

			int index; LoadInt32(&index, PC);

			var arr = stackslots[instance.index];
			Debug.Assert(arr.ValueType == BoxType.HeapPtr);
			Debug.Assert(Context.GC.Heap[arr.HeapPtr].Kind == RtHeapTypeKind.VECTOR || Context.GC.Heap[arr.HeapPtr].Kind == RtHeapTypeKind.ARRAY);

			var obj = Context.GC.Heap[arr.HeapPtr];
			if (obj.Kind == RtHeapTypeKind.ARRAY)
			{
				var arr_payload = (RtArray)obj;

				Debug.Assert(arr_payload.StoreMode != RtArray.ArrayStoreMode.cache_on_stack);

				if (arr_payload.StoreMode != RtArray.ArrayStoreMode.normal && index >= arr_payload.cache_store.Length)
				{
					int heaparr = arr_payload.ChangeStoreToHeap(Context.player, ref error);
					if (error.raised)
					{
						goto flag_handle_error;
					}
					obj = Context.GC.Heap[heaparr];
					stackslots[instance.index].SetHeapPtr(heaparr, (byte)RtHeapTypeKind.ARRAY, (byte)HeapKindFlag.NONE);
				}

				SetArraySlot(stackslots[dst_index], (uint)index, obj, ref error);
				if (error.raised)
				{
					goto flag_handle_error;
				}
			}
			else
			{
				if (Context.StackPosition >= Context.STACK_LENGTH)
				{
					RaiseStackOverflow(ref error);
					goto flag_handle_error;
				}

				ref NaNBoxing conv = ref Context.StackSlots[Context.StackPosition];conv.SetUndefined();
				Context.StackPosition++;


				var vec_payload = (RtVector)obj;

				ConvertValueType(ref error, stackslots[dst_index], vec_payload.element_type, vec_payload.element_asclass, ref conv);
				if (error.raised)
				{
					Context.StackPosition--;
					goto flag_handle_error;
				}
				//刚才的ConvertValueType不会导致调函数，因为没有传scope_ptr;

				vec_payload.SetSlot(index, this, arr.HeapPtr, conv, ref error);
				Context.StackPosition--;

				if (error.raised)
				{					
					goto flag_handle_error;
				}



			}


		flag_handle_error:
			;
		}


		private unsafe void Yield_return(int dst_index,int pc_sub_start, ASMethod method,  Span<NaNBoxing> stackslots,int stackStPos,int scope_ptr ,
			//ExceptionContext* exception_ctx,
			//ExceptionContext* exception_ctx_stack,
			int exception_at,
			ExceptionContext* NO_TRY,
			
			GeneratorImpl.GeneratorWapper resume_state,
			int returnSlotIndex,
			int calleelastPos,
			ref ReceiveError error,ref int PC_PTR)
		{
			Context.GC.CheckGC(ref error);

			StackLocater value;
			value.index = dst_index;

			var lv = stackslots[value.index].HeapKind != (byte)RtHeapTypeKind.STACK_CACHE_OBJ ? stackslots[value.index] : LoadValue((RtStackCache)Context.GC.Heap[stackslots[value.index].HeapPtr],
				 stackStPos - method.Body._link_codescope.Members.Count - 2, ref error, stackslots, stackStPos + value.index);
			if (error.raised)
			{
				//如果有异常，那就不会保存上下文
				goto flag_handle_error;
			}

			if (lv.ValueType == BoxType.HeapPtr)
			{
				StoreReturnSlot(ref Context.StackSlots[returnSlotIndex], stackStPos, returnSlotIndex, calleelastPos, scope_ptr, lv, ref error, true);
				if (error.raised)
				{
					Context.StackSlots[returnSlotIndex].SetUndefined();
					goto flag_handle_error;
				}
			}
			else
			{
				Context.StackSlots[returnSlotIndex] = lv;
			}


			//保存上下文状态
			int exception_ctx_count = (method.Flags.HasFlag(MethodFlags.NoTry) ? 0 : Context.MAX_TRY_NESTED) + 2;
			//int exception_at = (int)(exception_ctx - exception_ctx_stack);

			GeneratorImpl.GeneratorWapper generatorWapper = (GeneratorImpl.GeneratorWapper)resume_state;
			generatorWapper.exception_ctx_at = exception_at;
			if (exception_ctx_count > 0)
			{
				for (int i = 1; i < exception_at + 1; i++)
				{
					generatorWapper.exceptionContext[i] = *(NO_TRY + i);
#if DEBUG
					if (stackslots[generatorWapper.exceptionContext[i].hold_error.index].ValueType != BoxType.Fault)
					{
						//yield禁止在catch块内使用，所以不可能有hold的异常。
						throw new InvalidOperationException();
					}
#endif
				}
			}

			generatorWapper.state = 1;
			generatorWapper.RESUME_PC = (int)pc_sub_start; //(PC - PC_START);

			PC_PTR = generatorWapper.RESUME_PC;


		flag_handle_error:
			;

		}




		private unsafe void Await_return(int dst_index, int pc_sub_start, ASMethod method, Span<NaNBoxing> stackslots, int stackStPos, int scope_ptr,
			//ExceptionContext* exception_ctx,
			//ExceptionContext* exception_ctx_stack,
			int exception_at,
			ExceptionContext* NO_TRY,

			PromiseImpl.AsyncGenWapper resume_state,
			int returnSlotIndex,
			int calleelastPos,
			ref ReceiveError error, ref int PC_PTR)
		{
			Context.GC.CheckGC(ref error);

			StackLocater value;
			value.index = dst_index;

			var lv = stackslots[value.index].HeapKind != (byte)RtHeapTypeKind.STACK_CACHE_OBJ ? stackslots[value.index] : LoadValue((RtStackCache)Context.GC.Heap[stackslots[value.index].HeapPtr],
				 stackStPos - method.Body._link_codescope.Members.Count - 2, ref error, stackslots, stackStPos + value.index);
			if (error.raised)
			{
				//如果有异常，那就不会保存上下文
				goto flag_handle_error;
			}

			if (lv.ValueType == BoxType.HeapPtr)
			{
				StoreReturnSlot(ref Context.StackSlots[returnSlotIndex], stackStPos, returnSlotIndex, calleelastPos, scope_ptr, lv, ref error, true);
				if (error.raised)
				{
					Context.StackSlots[returnSlotIndex].SetUndefined();
					goto flag_handle_error;
				}
			}
			else
			{
				Context.StackSlots[returnSlotIndex] = lv;
			}


			//保存上下文状态
			int exception_ctx_count = (method.Flags.HasFlag(MethodFlags.NoTry) ? 0 : Context.MAX_TRY_NESTED) + 2;
			//int exception_at = (int)(exception_ctx - exception_ctx_stack);

			PromiseImpl.AsyncGenWapper asyncGenWapper = (PromiseImpl.AsyncGenWapper)resume_state;
			asyncGenWapper.exception_ctx_at = exception_at;
			if (exception_ctx_count > 0)
			{
				for (int i = 1; i < exception_at + 1; i++)
				{
					asyncGenWapper.exceptionContext[i] = *(NO_TRY + i);
#if DEBUG
					if (stackslots[asyncGenWapper.exceptionContext[i].hold_error.index].ValueType != BoxType.Fault)
					{
						//await禁止在finally块内使用，所以不可能有hold的异常。
						throw new InvalidOperationException();
					}
#endif
				}
			}

			asyncGenWapper.state = 1;
			asyncGenWapper.RESUME_PC = pc_sub_start; //(int)(PC - PC_START);

			PC_PTR = asyncGenWapper.RESUME_PC;

		flag_handle_error:
			;

		}



	}
}
