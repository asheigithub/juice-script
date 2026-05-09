using juicescript.ABC;
using juicescript.ABC.Locaters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
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
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
			var arg0 = scope.ReadSlot(0, context.player);

#if DEBUG
			if (arg0.ValueType != NaNBoxing.BoxType.Number)
				throw new InvalidOperationException();
#endif


			context.StackSlots[returnSlotIndex].SetBoolean(double.IsNaN(arg0.Number));
		}

		[NativeFunction("$__AS3__.toplevel$public::isFinite")]
		public static void TopLevel_IsFinite(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
			var arg0 = scope.ReadSlot(0, context.player);

#if DEBUG
			if (arg0.ValueType != NaNBoxing.BoxType.Number)
				throw new InvalidOperationException();
#endif


			context.StackSlots[returnSlotIndex].SetBoolean(double.IsFinite(arg0.Number));
		}


		//$__AS3__.toplevel$public::getTimer

		[NativeFunction("$__AS3__.toplevel$public::getTimer")]
		public static void TopLevel_getTimer(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			int t = (int)(((DateTime.Now - context.starttime).TotalMilliseconds));
			context.StackSlots[returnSlotIndex].SetInt(t);
		}

		//$__AS3__.toplevel$public::getQualifiedClassName
		[NativeFunction("$__AS3__.utils$public::getQualifiedClassName")]
		public static void TopLevel_getQualifiedClassName(
			Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
			var arg0 = scope.ReadSlot(0, context.player);

			switch (arg0.ValueType)
			{
				case NaNBoxing.BoxType.Number:
					context.player.TryCreateStringValue("Number", out context.StackSlots[returnSlotIndex], ref error);
					break;
				case NaNBoxing.BoxType.Undefined:
					context.player.TryCreateStringValue("void", out context.StackSlots[returnSlotIndex], ref error);
					break;
				case NaNBoxing.BoxType.Null:
					context.StackSlots[returnSlotIndex].SetNull();
					break;
				case NaNBoxing.BoxType.Boolean:
					context.player.TryCreateStringValue("Boolean", out context.StackSlots[returnSlotIndex], ref error);
					break;
				case NaNBoxing.BoxType.Int:
					context.player.TryCreateStringValue("int", out context.StackSlots[returnSlotIndex], ref error);
					break;
				case NaNBoxing.BoxType.Uint:
					context.player.TryCreateStringValue("uint", out context.StackSlots[returnSlotIndex], ref error);
					break;
				case NaNBoxing.BoxType.Sbyte:
					context.player.TryCreateStringValue("sbyte", out context.StackSlots[returnSlotIndex], ref error);
					break;
				case NaNBoxing.BoxType.Byte:
					context.player.TryCreateStringValue("byte", out context.StackSlots[returnSlotIndex], ref error);
					break;
				case NaNBoxing.BoxType.Short:
					context.player.TryCreateStringValue("short", out context.StackSlots[returnSlotIndex], ref error);
					break;
				case NaNBoxing.BoxType.UShort:
					context.player.TryCreateStringValue("ushort", out context.StackSlots[returnSlotIndex], ref error);
					break;
				case NaNBoxing.BoxType.Float:
					context.player.TryCreateStringValue("float", out context.StackSlots[returnSlotIndex], ref error);
					break;
				case NaNBoxing.BoxType.HeapPtr:
					{
						RtHeapBase instance = context.GC.Heap[arg0.HeapPtr];

						switch (instance.Kind)
						{
							case RtHeapTypeKind.CLASS:
								context.player.TryCreateStringValue(Extensions.ToQualifiedName(((RtScriptClass)instance).Meta.QName), out context.StackSlots[returnSlotIndex], ref error);
								break;
							case RtHeapTypeKind.GLOBAL:
								context.player.TryCreateStringValue("global", out context.StackSlots[returnSlotIndex], ref error);
								break;
							case RtHeapTypeKind.STRING:
								context.player.TryCreateStringValue("String", out context.StackSlots[returnSlotIndex], ref error);
								break;
							case RtHeapTypeKind.INSTANCE:
								context.player.TryCreateStringValue(Extensions.ToQualifiedName(instance.Type.QName), out context.StackSlots[returnSlotIndex], ref error);
								break;
							case RtHeapTypeKind.NAMESPACE:
								context.player.TryCreateStringValue("Namespace", out context.StackSlots[returnSlotIndex], ref error);
								break;
							case RtHeapTypeKind.ARRAY:
								context.player.TryCreateStringValue("Array", out context.StackSlots[returnSlotIndex], ref error);
								break;
							case RtHeapTypeKind.VECTOR:
								context.player.TryCreateStringValue(

									Extensions.ToQualifiedName(instance.Type.QName)
									
									, out context.StackSlots[returnSlotIndex], ref error);
								break;
							case RtHeapTypeKind.STACK_CACHE_OBJ:
								break;
							case RtHeapTypeKind.DYNAMIC_PROPERTYS:
								break;
							case RtHeapTypeKind.SHAPE:
								break;
							case RtHeapTypeKind.MethodScope:
								break;
							case RtHeapTypeKind.CLOSURE:

								ASMethod m = ((ASMethodBody)instance.Type).Method;
								if (m.__ismethod)
								{
									context.player.TryCreateStringValue($"builtin.as${m.ast_function_index}::MethodClosure", out context.StackSlots[returnSlotIndex], ref error);
								}
								else
								{
									context.player.TryCreateStringValue("Function", out context.StackSlots[returnSlotIndex], ref error);
								}

								break;
							default:
								break;
						}


					}
					break;
				case NaNBoxing.BoxType.LocalString:
					context.player.TryCreateStringValue("String", out context.StackSlots[returnSlotIndex], ref error);
					break;
				case NaNBoxing.BoxType.Fault:
				default:
					break;
			}




			//if (arg0.ValueType == NaNBoxing.BoxType.Null || arg0.ValueType == NaNBoxing.BoxType.Undefined)
			//{
			//	context.player.TryCreateStringValue("*", out context.StackSlots[returnSlotIndex], ref error);
			//	return;
			//}

			//if (arg0.ValueType == NaNBoxing.BoxType.HeapPtr)
			//{
			//	var instance = context.GC.Heap[arg0.HeapPtr];
			//	if (instance.Type != null)
			//	{
			//		string name = instance.Type.QName.ToDebugTypeName();
			//		context.player.TryCreateStringValue(name, out context.StackSlots[returnSlotIndex], ref error);
			//		return;
			//	}
			//}

			//context.StackSlots[returnSlotIndex].SetUndefined();
		}



		//$__AS3__.toplevel$public::getQualifiedSuperclassName
		[NativeFunction("$__AS3__.utils$public::getQualifiedSuperclassName")]
		public static void TopLevel_getQualifiedSuperclassName(
			Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
			var arg0 = scope.ReadSlot(0, context.player);

			switch (arg0.ValueType)
			{
				case NaNBoxing.BoxType.Number:
					context.player.TryCreateStringValue("Object", out context.StackSlots[returnSlotIndex], ref error);
					break;
				case NaNBoxing.BoxType.Undefined:
					context.player.TryCreateStringValue("void", out context.StackSlots[returnSlotIndex], ref error);
					break;
				case NaNBoxing.BoxType.Null:
					context.StackSlots[returnSlotIndex].SetNull();
					break;
				case NaNBoxing.BoxType.Boolean:
					context.player.TryCreateStringValue("Object", out context.StackSlots[returnSlotIndex], ref error);
					break;
				case NaNBoxing.BoxType.Int:
					context.player.TryCreateStringValue("Object", out context.StackSlots[returnSlotIndex], ref error);
					break;
				case NaNBoxing.BoxType.Uint:
					context.player.TryCreateStringValue("Object", out context.StackSlots[returnSlotIndex], ref error);
					break;
				case NaNBoxing.BoxType.Sbyte:
					context.player.TryCreateStringValue("Object", out context.StackSlots[returnSlotIndex], ref error);
					break;
				case NaNBoxing.BoxType.Byte:
					context.player.TryCreateStringValue("Object", out context.StackSlots[returnSlotIndex], ref error);
					break;
				case NaNBoxing.BoxType.Short:
					context.player.TryCreateStringValue("Object", out context.StackSlots[returnSlotIndex], ref error);
					break;
				case NaNBoxing.BoxType.UShort:
					context.player.TryCreateStringValue("Object", out context.StackSlots[returnSlotIndex], ref error);
					break;
				case NaNBoxing.BoxType.Float:
					context.player.TryCreateStringValue("Object", out context.StackSlots[returnSlotIndex], ref error);
					break;
				case NaNBoxing.BoxType.HeapPtr:
					{
						RtHeapBase instance = context.GC.Heap[arg0.HeapPtr];

						switch (instance.Kind)
						{
							case RtHeapTypeKind.CLASS:
								context.player.TryCreateStringValue("Object", out context.StackSlots[returnSlotIndex], ref error);
								break;
							case RtHeapTypeKind.GLOBAL:
								context.player.TryCreateStringValue("Object", out context.StackSlots[returnSlotIndex], ref error);
								break;
							case RtHeapTypeKind.STRING:
								context.player.TryCreateStringValue("Object", out context.StackSlots[returnSlotIndex], ref error);
								break;
							case RtHeapTypeKind.INSTANCE:
								//context.player.TryCreateStringValue(Extensions.ToDebugTypeName(instance.Type.QName), out context.StackSlots[returnSlotIndex], ref error);

								{
									if (((ASInstance)instance.Type).Super != null)
									{
										context.player.TryCreateStringValue(Extensions.ToQualifiedName(((ASInstance)instance.Type).Super), out context.StackSlots[returnSlotIndex], ref error);

									}
									else
									{
										context.StackSlots[returnSlotIndex].SetNull();
									}
								}
								
								break;
							case RtHeapTypeKind.NAMESPACE:
								context.player.TryCreateStringValue("Object", out context.StackSlots[returnSlotIndex], ref error);
								break;
							case RtHeapTypeKind.ARRAY:
								context.player.TryCreateStringValue("Object", out context.StackSlots[returnSlotIndex], ref error);
								break;
							case RtHeapTypeKind.VECTOR:
								context.player.TryCreateStringValue("Object", out context.StackSlots[returnSlotIndex], ref error);
								break;
							case RtHeapTypeKind.STACK_CACHE_OBJ:
								break;
							case RtHeapTypeKind.DYNAMIC_PROPERTYS:
								break;
							case RtHeapTypeKind.SHAPE:
								break;
							case RtHeapTypeKind.MethodScope:
								break;
							case RtHeapTypeKind.CLOSURE:
								context.player.TryCreateStringValue("Object", out context.StackSlots[returnSlotIndex], ref error);
								break;
							default:
								break;
						}


					}
					break;
				case NaNBoxing.BoxType.LocalString:
					context.player.TryCreateStringValue("Object", out context.StackSlots[returnSlotIndex], ref error);
					break;
				case NaNBoxing.BoxType.Fault:
				default:
					break;
			}
		}



		//$__AS3__.utils$public::getDefinitionByName
		[NativeFunction("$__AS3__.utils$public::getDefinitionByName")]
		public static void TopLevel_getDefinitionByName(
			Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
			var arg0 = scope.ReadSlot(0, context.player);

			if (arg0.ValueType == NaNBoxing.BoxType.Null)
			{
				context.player.RaiseArgumentNotNull(ref error,"name");
				return;
			}


			Span<char> buffers = stackalloc char[16];
			ReadOnlySpan<char> chars = buffers;	
			if (arg0.ValueType == NaNBoxing.BoxType.LocalString)
			{
				int len = arg0.GetLocalStringChars(buffers);
				chars = chars.Slice(0,len);
			}
			else
			{
				chars = ((RtString)context.GC.Heap[ arg0.HeapPtr ]).Str;
			}

			foreach (var c in context.libs.SelectMany(o => o.Scripts.Select(s => s.Traits[0].Class)))
			{
				if (c != null)
				{ 
					if( chars.CompareTo(c.QName.ToQualifiedName().AsSpan(), StringComparison.Ordinal) == 0)
					{
						context.player.InitScript((ASScript)c._link_codescope.Parent.Container, ref error);
						if (error.raised)
						{
							return;
						}
						if (c.__instance_index__ == 0)
						{
							//在@class就在当前正在初始化的script中，却又没有初始化到的情况。
							context.player.InitASClass(c, ref error);
							if (error.raised)
							{
								return;
							}
						}

						context.StackSlots[returnSlotIndex].SetHeapPtr(c.__instance_index__, (byte)RtHeapTypeKind.CLASS);
						return;
					}
				}
			}


			context.player.RaiseReferenceError_TypeNotFound(ref error, chars);
			return;

		}











		[NativeFunction("$__AS3__.toplevel$public::parseFloat")]
		public static void TopLevel_parseFloat(Context context,
		ASMethod method,
		int scope_ptr,
		NaNBoxing thisPtr,
		int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
			var arg0 = scope.ReadSlot(0, context.player);

			if (arg0.ValueType == NaNBoxing.BoxType.Null || arg0.ValueType == NaNBoxing.BoxType.Undefined)
			{
				context.StackSlots[returnSlotIndex].SetNumber(double.NaN);
			}
			else
			{
				//unsafe
				{

					Span<char> thisbuffer = stackalloc char[16];
					ReadOnlySpan<char> thisStr = thisbuffer;

					if (arg0.ValueType == NaNBoxing.BoxType.LocalString)
					{
						var len = arg0.GetLocalStringChars(thisbuffer);
						thisStr = thisStr.Slice(0, len);
					}
					else
					{
						thisStr = ((RtString)context.GC.Heap[arg0.HeapPtr]).Str.AsSpan();
					}


					long nv = 0;//有效数字
								//bool nv_isoverflow = false;
					bool isINF = false;

					//double n = 0;
					int e = 0;
					bool isEmode = false;

					int sign = 1;
					int esign = 1;
					bool haschecksign = false;

					bool blank = false;


					bool isdecimal = false;
					int d = 0;

					//Infinity

					ReadOnlySpan<char> infchar = "Infinity";

					int inftest = 0;

					bool hasdigit = false;


					int charindex = 0;
					{

						while (charindex < thisStr.Length)
						{

							var p = thisStr[charindex];
							charindex++;

							if (p == '\0')
							{
								break;
							}

							char c = p;

							if (!char.IsWhiteSpace(c))//c != ' ')
							{

								if (blank)
								{
									break;
								}


								if (!haschecksign)
								{
									haschecksign = true;
									if (c == '-')
									{
										if (isEmode)
										{
											esign = -1;
										}
										else
										{
											sign = -1;
										}

										++p;
										continue;
									}
									else if (c == '+')
									{
										++p;
										continue;
									}
								}

								if (inftest < 8)
								{
									if (c == infchar[inftest])
									{
										++inftest;

										if (inftest == 8)
										{
											isINF = true;
											//n = double.PositiveInfinity;
											hasdigit = true;
											blank = true;
										}

										++p;
										continue;
									}
								}

								if (c >= '0' && c <= '9')
								{
									if (!isEmode)
									{

										hasdigit = true;
									}

									if (isEmode)
									{
										if (e * 10 + (c - '0') >= e)
										{
											e = e * 10 + (c - '0');
										}

									}
									else if (isdecimal)
									{

										if (nv * 10 + (c - '0') >= nv)
										{
											//d = d * 10;

											//n = n * 10 + (c - '0');

											d--;

											nv = nv * 10 + (c - '0');
										}
										else
										{
											//小数点后溢出，那么 d和n都不乘就好了
										}
									}
									else
									{
										//n = n * 10 + (c - '0');

										if (nv * 10 + (c - '0') >= nv)
										{
											nv = nv * 10 + (c - '0');
										}
										else
										{
											d++;
											//nv_isoverflow = true;
										}
									}
								}
								else if (c == 'e' || c == 'E')
								{
									//hasdigit = true;
									if (!isEmode)
									{
										isEmode = true;
										haschecksign = false;
									}
									else
									{
										break;
									}
								}

								else if (c == '.')
								{

									if (isdecimal || isEmode)
									{
										break;
									}
									else
									{
										isdecimal = true;
									}
								}
								else
								{
									break;
								}

							}
							else if (inftest > 0 || haschecksign)
							{
								blank = true;
							}
							else if (hasdigit)
							{
								blank = true;
							}


							++p;
						}


					}

					//if (double.IsNaN(n) || double.IsInfinity(n) || nv_isoverflow)
					//{
					//	n = n / d;
					//}
					//else
					//{ 
					//	n = nv / d;
					//}

					int E;

					if (esign > 0)
					{
						E = e + d;


						//while (e > 0)
						//{
						//	n = n * 10;
						//	--e;

						//	if (double.IsInfinity(n))
						//		break;

						//}

					}
					else
					{
						E = -e + d;

						//double div = 1;

						//while (e > 0)
						//{
						//	div *= 10;
						//	--e;

						//	if (double.IsInfinity(div))
						//	{
						//		break;
						//	}
						//}

						//n = n / div;


					}

					double n;
					if (isINF)
					{
						n = double.PositiveInfinity;
					}
					else
					{
						//n = nv * Math.Pow(10, E);

						n = nv;

						if (E > 0)
						{
							while (E > 0)
							{
								--E;

								n *= 10;
								if (double.IsInfinity(n))
									break;
							}
						}
						else if (E < 0)
						{
							E = -E;

							double div = 1;
							while (E > 0)
							{
								--E;

								div *= 10;
								if (double.IsInfinity(div))
									break;
							}

							n = n / div;
						}


					}
					if (!hasdigit)
						context.StackSlots[returnSlotIndex].SetNumber(double.NaN);
					else
						context.StackSlots[returnSlotIndex].SetNumber(n * sign);

				}

			}


		}


		//$__AS3__.toplevel$public::parseInt
		[NativeFunction("$__AS3__.toplevel$public::parseInt")]
		public static void TopLevel_parseInt(Context context,
			ASMethod method,
			int scope_ptr,
			NaNBoxing thisPtr,
			int stackStPos, ref ReceiveError error, int returnSlotIndex)
		{
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
			var arg0 = scope.ReadSlot(0, context.player);
			var arg1 = scope.ReadSlot(1, context.player);

			if (arg0.ValueType == NaNBoxing.BoxType.Null || arg0.ValueType == NaNBoxing.BoxType.Undefined)
			{
				context.StackSlots[returnSlotIndex].SetNumber(double.NaN);
			}
			else
			{
				Span<char> thisbuffer = stackalloc char[16];
				ReadOnlySpan<char> thisStr = thisbuffer;

				if (arg0.ValueType == NaNBoxing.BoxType.LocalString)
				{
					var len = arg0.GetLocalStringChars(thisbuffer);
					thisStr = thisStr.Slice(0, len);
				}
				else
				{
					thisStr = ((RtString)context.GC.Heap[arg0.HeapPtr]).Str.AsSpan();
				}

				uint radix = (uint)arg1.IntValue;

				/*if (str.length()==0)
				{
					returnSlot->value = (double)NAN;
					return;
				}*/
				//ASCII 48-57 : 0-9 ,65-90 : A-Z;

				bool isinput_radix_zero = false;
				if (radix == 0)
				{
					isinput_radix_zero = true;
					radix = 10;
				}


				if (radix < 2 || radix > 36)
				{
					context.StackSlots[returnSlotIndex].SetNumber(double.NaN);
					return;
				}//return new rtNumber(double.NaN); }



				double output = double.NaN;
				int sign = 1;

				int i = 0;

				while (i < thisStr.Length && char.IsWhiteSpace(thisStr[i]))
				{
					++i;
				}

				if (i < thisStr.Length)
				{
					if (thisStr[i] == '-')
					{
						sign = -1;
						++i;
					}
					else if (thisStr[i] == '+')
					{
						++i;
					}
				}

				if (i < thisStr.Length - 2 && (isinput_radix_zero || radix == 16))
				{
					if (thisStr[i] == '0' && (thisStr[i + 1] == 'x' || thisStr[i + 1] == 'X'))
					{
						radix = 16;
						i += 2;

						if (i < thisStr.Length)
						{
							if (thisStr[i] == '-')
							{
								i = thisStr.Length;
							}
						}
						else
						{
							i = thisStr.Length;
						}

					}
				}

				uint allowidx = 48 + radix;

				if (radix > 10)
				{
					allowidx = 65 + radix - 10;
				}

				for (; i < thisStr.Length; i++)
				{
					var cc = thisStr[i];
					if (cc >= 'a' && cc <= 'z')
					{
						cc = char.ToUpper(cc); //u+305 之类，ToUpper后会变成 A-Z里的字母，巨坑无比
					}


					var c = cc;
					if (c < allowidx && ((c < 58 && c >= 48) || c >= 65))
					{
						if (double.IsNaN(output))
						{
							output = c < 58 ? (c - 48) : (c - 65 + 10);
						}
						else
						{
							output = output * radix + (c < 58 ? (c - 48) : (c - 65 + 10));
						}
					}
					else
					{
						break;
					}
				}

				//returnSlot->value = output * sign;

				context.StackSlots[returnSlotIndex].SetNumber(output * sign);


			}
		}



		private static void WritePrimitive(NaNBoxing arg, IPrint printer, Context context)
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
					printer.Write(((RtString)context.GC.Heap[arg.HeapPtr]).Str); return;
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

		internal static void TraceElement(NaNBoxing arg, Context context, int stackStPos, ref ReceiveError error, int scope_ptr, NaNBoxing callee_bindthis, IPrint printer)
		{
		lbl_retry:
			// 快路径：原始值
			if (context.player.IsPrimitive(arg))
			{
				WritePrimitive(arg, printer, context);
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
				switch (instance.Kind)
				{
					case RtHeapTypeKind.CLASS:
						printer.Write($"[class {((RtScriptClass)instance).Meta.QName.Name}]");
						break;
					case RtHeapTypeKind.GLOBAL:
						printer.Write("[object global]");
						break;
					case RtHeapTypeKind.STRING:
						printer.Write(((RtString)instance).Str);
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
							int code = context.player.MultiNameLSearch(ns_set, instance.Kind,
								as_type, mode, 0, new StackLocater() { index = 0 }, stackslots, stPos, arg, context.player.check_MultiNameLSearch_issameorinherit(arg, callee_bindthis.ValueType == NaNBoxing.BoxType.HeapPtr ? (context.GC.Heap[callee_bindthis.HeapPtr]) : null), ref error, true);
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
							if (funinstance.Kind != RtHeapTypeKind.CLOSURE)
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
									var msg = ((RtInstance)instance).ReadSlot(0, instance.Type._link_codescope, context.player);
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
									var m = ((ASMethodBody)funinstance.Type).Method;

									NaNBoxing conv = context.player.RunMethod(m,
										arg,  ((RtClosure)funinstance).ScopePtr, ((RtClosure)funinstance).ScopeType, 0, null, null, ref error, stPos + 1, fun.HeapPtr);
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
						ASNamespace ns = ((RtNameSpace)instance).ASNamespace;
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
						((RtMethodScope)context.GC.Heap[context.M_MethodScopePtr + context.BackTraceIndex - 1]).EmptyStackSlot();
						((RtArray)instance).Trace(context, stackStPos, ref error, scope_ptr, printer, instance, ",");
						context.BackTraceIndex--;
						if (error.raised)
						{
							return;
						}
						break;
					case RtHeapTypeKind.VECTOR:
						//printer.Write($"[object .__AS3__.vec.vector<{((RtPayloadVector)instance).element_asclass.QName.ToDebugTypeName()}>]");
						if (context.BackTraceIndex >= Context.MAX_BACKTRACE)
						{
							printer.WriteLine(string.Empty);
							context.player.RaiseStackOverflow(ref error);
							return;
						}


						RtVector vector = (RtVector)instance;
						context.BackTraceIndex++;
						((RtMethodScope)context.GC.Heap[context.M_MethodScopePtr + context.BackTraceIndex - 1]).EmptyStackSlot();
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
			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
			context.StackSlots[returnSlotIndex].SetUndefined();

			var rest = scope.ReadSlot(0, context.player);
			var rest_array = (RtArray)context.GC.Heap[rest.HeapPtr];

#if DEBUG
			if (rest_array.StoreMode != RtArray.ArrayStoreMode.cache_on_stack)
				throw new InvalidOperationException();
#endif

			var arguments = rest_array.stack_store.Span;

			for (var i = 0; i < arguments.Length; i++)
			{
				var arg = arguments[i];

				TraceElement(arg, context, stackStPos, ref error, scope_ptr, thisPtr, context.player.Print);
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
			RtHeapBase promise;
			int ptr = context.MicroTaskQueue.CreateNativePromise(context, out promise);
			if (ptr == 0)
			{
				context.player.RaiseOutOfMemory(ref error);
				return;
			}

			var scope = (RtMethodScope)context.GC.Heap[scope_ptr];
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
				uri = ((RtString)context.GC.Heap[url.HeapPtr]).Str;
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
										(AysncGetResult r) =>
										{
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
										(AysncGetResult r) =>
										{
											context.player.TryCreateStringValue(str.Exception.Message, out r.value, ref r.error);
										}
										, false);
									}
									else
									{

										context.AsyncCallbackQueue.OnAsyncComplete(ptr,
										(AysncGetResult r) =>
										{
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
										(AysncGetResult r) =>
										{
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
					(AysncGetResult r) =>
					{
						context.player.TryCreateStringValue(ex.Message, out r.value, ref r.error);
					}
					, false);
				httpClient.Dispose();
			}

			context.StackSlots[returnSlotIndex].SetHeapPtr(ptr, (byte)RtHeapTypeKind.INSTANCE);

		}

	}
}
