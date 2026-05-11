using juicescript.ABC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace juicescript
{
    /*
    +- C# NaN符号位是1
    |+- Exponent bits all set to 1
    ||
    ||          +- Quiet bit
    ||          |
    ||          |+- Intel QNaN Floating-Point Indefinite 
    vv          vv
    1[Exponent ]1000IIIIIIIIIIIIIIII  IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII
     ^         ^    ^
     1         11   |
                    64bit 指针最多会有这么多位数。


    所以如果把NaN全部存储为0xFFF8000000000000 。那么当一个值>0xFFF8000000000000 就说明这是一个打包的值。

   
    如果用object数组下标规避64bit指针,那么有足够空间保存类型。

    */
    [StructLayout(LayoutKind.Explicit)]
    public struct NaNBoxing
    {
        [FieldOffset(0)]
        internal ulong store;

        [FieldOffset(0)]
        internal double number;

        //[FieldOffset(4)]
        //internal float float_val;

        /// <summary>
        /// bit:
        /// 1111111111111000000000000000000000000000000000000000000000000000
        /// </summary>
        public const ulong QNAN = 0xFFF8000000000000;


        public const ulong UNDEFINED = 0xFFF8010000000000;
        public const ulong NULL = 0xFFF8020000000000;
        public const ulong TRUE = 0xFFF8030000000000;
        public const ulong FALSE = 0xFFF8040000000000;
        public const ulong TAG_INT = 0xFFF8050000000000;
        public const ulong TAG_UINT = 0xFFF8060000000000;
        public const ulong TAG_SBYTE = 0xFFF8070000000000;
        public const ulong TAG_BYTE = 0xFFF8080000000000;
        public const ulong TAG_SHORT = 0xFFF8090000000000;
        public const ulong TAG_USHORT = 0xFFF80A0000000000;
        public const ulong TAG_FLOAT = 0xFFF80B0000000000;
        public const ulong TAG_HEAP_POINTER = 0xFFF80C0000000000;
        public const ulong TAG_LOCAL_STRING = 0xFFF80D0000000000;

        //internal const ulong MASK_EXPONENT = 0x7ff0000000000000;
        //internal const ulong MASK_SIGNATURE = 0xFFFFFFFF00000000;

        /// <summary>
        ///  \0 !
        /// </summary>
        public const ulong L_ZEROSTRING =  0xFFF80D00FFFFFFFF;

        public NaNBoxing()
        {
            //float_val = 0;
            number = 0;
            store = UNDEFINED;
        }

        public NaNBoxing(ulong raw)
        {
            //float_val = 0;
            number = 0;
            store = raw;
        }

        public enum BoxType : uint
        {
            Number = 0,
            Undefined = (uint)(UNDEFINED >> 40) & 0xF,
            Null = (uint)(NULL >> 40) & 0xF,
            Boolean = (uint)(FALSE >> 40) & 0xF,
            Int = (uint)(TAG_INT >> 40) & 0xF,
            Uint = (uint)(TAG_UINT >> 40) & 0xF,
            Sbyte = (uint)(TAG_SBYTE >> 40) & 0xF,
            Byte = (uint)(TAG_BYTE >> 40) & 0xF,
            Short = (uint)(TAG_SHORT >> 40) & 0xF,
            UShort = (uint)(TAG_USHORT >> 40) & 0xF,
            Float = (uint)(TAG_FLOAT >> 40) & 0xF,
            HeapPtr = (uint)(TAG_HEAP_POINTER >> 40) & 0xF,
            LocalString = (uint)(TAG_LOCAL_STRING >> 40) & 0xF,

            Fault = 0xE0
        }


        /// <summary>
        /// 如果类型可快速比较，快速比较
        /// 如果返回true,说明两个数成功比较
        /// 否则要走后面的复杂比较。
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        [MethodImpl( MethodImplOptions.AggressiveInlining)]
        public bool FastTestComp(NaNBoxing other,out bool isequal)
        {
            if (store == QNAN || other.store == QNAN)
            {
                isequal = false;
                return true;
            }
            else if (other.store < UNDEFINED && store < UNDEFINED)
            {
				isequal = number == other.number;
				return true;
			}
            else if (store >= UNDEFINED && other.store >= UNDEFINED)
            {

                uint signature1 = (uint)(store >> 40) & 0xF;
                uint signature2 = (uint)(other.store >> 40) & 0xF;

                if (signature1 < 3 && signature2 < 3)
                {
                    isequal = false;
                    return false;
                }
                else if ((signature1 == 3 || signature1 == 4) && (signature2 == 3 || signature2 == 4))
                {
                    isequal = store == other.store;
                    return true;
                }
                else if (signature1 < 11 && signature2 < 11 && signature1 > 4 && signature2 > 4)
                {

                    long v1;
                    if (signature1 % 2 == 0)
                    {
                        v1 = (uint)(store & 0xffffffff);
                    }
                    else
                    {
                        v1 = (int)(store & 0xffffffff);
                    }

                    long v2;
                    if (signature2 % 2 == 0)
                    {
                        v2 = (uint)(other.store & 0xffffffff);
                    }
                    else
                    {
                        v2 = (int)(other.store & 0xffffffff);
                    }

                    isequal = v1 == v2;
                    return true;
                }
                else if (signature1 == 11 && signature2 == 11)
                {
                    float f1 = FloatValue;
                    float f2 = other.FloatValue;

                    if (float.IsNaN(f1) || float.IsNaN(f2))
                        isequal = false;
                    else
                        isequal = f1 == f2;

                    return true;
                }
                else if (signature1 == 13 && signature2 == 13)
                {
                    // LocalString与LocalString比较 - 使用高效的字节比较
                    Span<byte> bytes1 = stackalloc byte[5];
                    Span<byte> bytes2 = stackalloc byte[5];
                    
                    int len1 = GetLocalStringBytes(bytes1);
                    int len2 = other.GetLocalStringBytes(bytes2);
                    
                    if (len1 != len2)
                    {
                        isequal = false;
                    }
                    else if (len1 == 0)
                    {
                        isequal = true; // 两个都是空字符串
                    }
                    else
                    {
                        isequal = bytes1.Slice(0, len1).SequenceEqual(bytes2.Slice(0, len2));
                    }
                    return true;
                }
                else if (signature1 == 13 && signature2 == 12)
                {
                    // LocalString与HeapPtr字符串比较 - 需要在运行时上下文中处理
                    isequal = false;
                    return false;
                }
                else if (signature1 == 12 && signature2 == 13)
                {
                    // HeapPtr字符串与LocalString比较 - 需要在运行时上下文中处理
                    isequal = false;
                    return false;
                }
                else
                {
                    isequal = false;
                    return false;
                }
            }
            else if (other.store < UNDEFINED)
            {
                double v2 = other.number;
                uint signature1 = (uint)(store >> 40) & 0xF;


                if (signature1 < 11 && signature1 > 4)
                {
                    long v1;
                    if (signature1 % 2 == 0)
                    {
                        v1 = (uint)(store & 0xffffffff);
                    }
                    else
                    {
                        v1 = (int)(store & 0xffffffff);
                    }

                    isequal = v1 == v2;
                    return true;
                }
                else if (signature1 == 11)
                {
                    float f1 = FloatValue;
                    if (float.IsNaN(f1))
                        isequal = false;
                    else
                        isequal = f1 == v2;

                    return true;
                }
                else
                {
                    isequal = false;
                    return false;
                }
            }
            else// if (store < UNDEFINED)
            {
                double v1 = number;
                uint signature2 = (uint)(other.store >> 40) & 0xF;
                if (signature2 < 11 && signature2 > 4)
                {
                    long v2;
                    if (signature2 % 2 == 0)
                    {
                        v2 = (uint)(other.store & 0xffffffff);
                    }
                    else
                    {
                        v2 = (int)(other.store & 0xffffffff);
                    }

                    isequal = v1 == v2;
                    return true;
                }
                else if (signature2 == 11)
                {
                    float f2 = other.FloatValue;
                    if (float.IsNaN(f2))
                        isequal = false;
                    else
                        isequal = v1 == f2;
                    return true;
                }
                else
                {
                    isequal = false;
                    return false;
                }
            }
            
		}


        /// <summary>
        /// 尝试快速加法。如果不能加，则回退到慢路径
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        [MethodImpl( MethodImplOptions.AggressiveOptimization)]
        public static bool FastAdd(NaNBoxing a, NaNBoxing b, out NaNBoxing result)
        {
            result = default;
            if (a.store == QNAN || b.store == QNAN)
            {
                result.store = QNAN;
                return true;
            }
            else if (a.store < UNDEFINED && b.store < UNDEFINED)
            {
                result.SetNumber(a.number + b.number);
                return true;
            }
            else if (a.store < UNDEFINED)
            {
				uint signature2 = (uint)(b.store >> 40) & 0xF;
				if (signature2 > 11 || signature2 < 1 )
				{
					return false;
				}

                result.SetNumber( a.number + GetDouble(b) );
                return true;
			}
            else if (b.store < UNDEFINED)
            {
				uint signature1 = (uint)(a.store >> 40) & 0xF;
				if (signature1 > 11 || signature1 < 1)
				{
					return false;
				}

				result.SetNumber( GetDouble(a)  + b.number);
				return true;

			}
            else
            {
                uint signature1 = (uint)(a.store >> 40) & 0xF;
                uint signature2 = (uint)(b.store >> 40) & 0xF;

                // Handle LocalString + LocalString case for fast string concatenation
                if (signature1 == 13 && signature2 == 13)
                {
                    // Both are LocalString, try to concatenate directly using bytes
                    Span<byte> bytes1 = stackalloc byte[5];
                    Span<byte> bytes2 = stackalloc byte[5];
                    
                    int len1 = a.GetLocalStringBytes(bytes1);
                    int len2 = b.GetLocalStringBytes(bytes2);
                    
                    // Check if concatenated result can fit in LocalString (5 bytes max)
                    if (len1 >= 0 && len2 >= 0 && (len1 + len2) <= 5)
                    {
                        // Create concatenated LocalString directly from bytes
                        Span<byte> concatenated = stackalloc byte[len1 + len2];
                        bytes1.Slice(0, len1).CopyTo(concatenated);
                        bytes2.Slice(0, len2).CopyTo(concatenated.Slice(len1));
                        
                        result.SetLocalString(concatenated);
                        return true;
                    }
                    
                    // If result too long, fall back to slow path
                    return false;
                }
                
                // LocalString with other types should fall back to slow path for string concatenation
                if (signature1 > 11 || signature1 < 1 || signature2 > 11 || signature2 < 1)
                {
                    return false;
                }

                if (signature1 == 1 || signature2 == 1)
                {
                    result.store = QNAN;
                    return true;
                }


                //数值计算
                switch (signature1)
                {
                    case 2:
                        //v1 : null
                        {
                            switch (signature2)
                            {
                                case 2:
                                case 3:
                                case 4:
                                case 5:
                                case 6:
                                case 7:
                                case 8:
                                case 9:
                                case 10:
                                case 11:
                                    result.SetNumber(0.0 + GetDouble(b));
                                    break;
                                default:
#if DEBUG
					                throw new InvalidOperationException();
#else
									Environment.FailFast("出错了，这里跑不到"); return default;
#endif
							}
						}
                        break;
                    case 3:
                    case 4:
                        //v1 : boolean
                        {
                            switch (signature2)
                            {
                                case 2:
                                    result.SetNumber((a.Boolean ? 1 : 0) + 0.0);
                                    break;
                                case 3:
                                case 4:
                                    result.SetInt((a.Boolean ? 1 : 0) + (b.Boolean ? 1 : 0));
                                    break;
                                case 5:
                                    result.SetInt((a.Boolean ? 1 : 0) + b.IntValue);
                                    break;
                                case 6:
                                    result.SetNumber((a.Boolean ? 1U : 0U) + GetDouble(b));
                                    break;
                                case 7:
                                case 8:
                                case 9:
                                case 10:
                                    result.SetInt((a.Boolean ? 1 : 0) + GetInt(b));
                                    break;
                                case 11:
                                    result.SetFloat((a.Boolean ? 1 : 0) + b.FloatValue);
                                    break;
                                default:
#if DEBUG
					                throw new InvalidOperationException();
#else
									Environment.FailFast("出错了，这里跑不到"); return default;
#endif
							}
							break;
                        }
                    case 5:
                        //v1 BoxType.Int;
                        {
                            switch (signature2)
                            {
                                case 2:
                                    result.SetNumber(a.IntValue + GetDouble(b));
                                    break;
                                case 3:
                                case 4:
                                    result.SetInt(a.IntValue + (b.Boolean ? 1 : 0));
                                    break;
                                case 6:
                                    result.SetNumber((double)a.IntValue + b.UIntValue);
                                    break;
                                case 11:
                                    result.SetFloat(a.IntValue + b.FloatValue);
                                    break;
                                case 5:
                                case 7:
                                case 8:
                                case 9:
                                case 10:
                                    result.SetInt(a.IntValue + GetInt(b));
                                    break;
                                default:
#if DEBUG
					                throw new InvalidOperationException();
#else
									Environment.FailFast("出错了，这里跑不到"); return default;
#endif
							}
							break;
                        }

                    case 6:
                        //v1 BoxType.Uint;
                        {
                            switch (signature2)
                            {
                                case 2:
                                    result.SetNumber(a.UIntValue + GetDouble(b));
                                    break;
                                case 3:
                                case 4:
                                    result.SetNumber(a.UIntValue + GetDouble(b));
                                    break;
                                case 5:
                                    result.SetNumber((double)a.UIntValue + b.IntValue);
                                    break;
                                case 6:
                                    result.SetUInt(a.UIntValue + b.UIntValue);
                                    break;
                                case 7:
                                    result.SetNumber((double)a.UIntValue + b.SByteValue);
                                    break;
                                case 8:
                                    result.SetUInt(a.UIntValue + b.ByteValue);
                                    break;
                                case 9:
                                    result.SetNumber((double)a.UIntValue + b.ShortValue);
                                    break;
                                case 10:
                                    result.SetUInt(a.UIntValue + b.UShortValue);
                                    break;
                                case 11:
                                    result.SetFloat((float)a.UIntValue + b.FloatValue);
                                    break;
                                default:
#if DEBUG
					                throw new InvalidOperationException();
#else
									Environment.FailFast("出错了，这里跑不到"); return default;
#endif
							}
						}
                        break;
                    case 7:
                    //v1 BoxType.Sbyte; 
                    case 8:
                    //v1 BoxType.Byte;
                    case 9:
                    //v1 BoxType.Short;
                    case 10:
                        //v1 BoxType.UShort;
                        {
                            switch (signature2)
                            {
                                case 2:
                                    result.SetNumber(GetDouble(a) + GetDouble(b));
                                    break;
                                case 6:
                                    result.SetNumber(GetDouble(a) + b.UIntValue);
                                    break;
                                case 7:
                                case 8:
                                case 9:
                                case 10:
                                case 3:
                                case 4:
                                case 5:
                                    result.SetInt(GetInt(a) + GetInt(b));
                                    break;
                                case 11:
                                    result.SetFloat(GetFloat(a) + b.FloatValue);
                                    break;
                                default:
#if DEBUG
					                throw new InvalidOperationException();
#else
									Environment.FailFast("出错了，这里跑不到"); return default;
#endif
							}
						}
                        break;
                    case 11:
                        //v1 BoxType.Float;
                        switch (signature2)
                        {
                            case 2:
                                result.SetNumber(a.FloatValue + 0.0);
                                break;
                            case 3:
                            case 4:
                            case 5:
                            case 6:
                            case 7:
                            case 8:
                            case 9:
                            case 10:
                            case 11:
                                result.SetFloat(GetFloat(a) + GetFloat(b));
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

                return true;
            }

        }


        /// <summary>
        /// 尝试快速减法，如果失败，退回到慢路径
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static bool FastMinus(NaNBoxing a, NaNBoxing b, out NaNBoxing result)
        {
            result = default;
            if (a.store == QNAN || b.store == QNAN)
            {
                result.store = QNAN;
                return true;
            }
            else if (a.store < UNDEFINED && b.store < UNDEFINED)
            {
                result.SetNumber(a.number - b.number);
                return true;
            }
            else if (a.store < UNDEFINED)
            {
                uint signature2 = (uint)(b.store >> 40) & 0xF;
                if (signature2 > 11 || signature2 < 1)
                {
                    return false;
                }

                result.SetNumber(a.number - GetDouble(b));
                return true;
            }
            else if (b.store < UNDEFINED)
            {
                uint signature1 = (uint)(a.store >> 40) & 0xF;
                if (signature1 > 11 || signature1 < 1)
                {
                    return false;
                }

                result.SetNumber(GetDouble(a) - b.number);
                return true;

            }
            else
            {
                uint signature1 = (uint)(a.store >> 40) & 0xF;
                uint signature2 = (uint)(b.store >> 40) & 0xF;

                // LocalString (signature 13) should fall back to slow path for string operations
                if (signature1 > 11 || signature1 < 1 || signature2 > 11 || signature2 < 1)
                {
                    return false;
                }

                if (signature1 == 1 || signature2 == 1)
                {
                    result.store = QNAN;
                    return true;
                }


                switch (signature1)
                {
                    case 2: // null
                        result.SetNumber(0.0 - GetDouble(b));
                        break;
                    case 3:
                    case 4: //bool
                        {
                            switch (signature2)
                            {
                                case 2:
                                    result.SetNumber((a.Boolean ? 1 : 0) - 0.0);
                                    break;
                                case 3:
                                case 4:
                                    result.SetInt((a.Boolean ? 1 : 0) - (b.Boolean ? 1 : 0));
                                    break;
                                case 5:
                                    result.SetInt((a.Boolean ? 1 : 0) - b.IntValue);
                                    break;
                                case 6:
                                    result.SetNumber((a.Boolean ? 1U : 0U) - b.UIntValue);
                                    break;
                                case 7:
                                case 8:
                                case 9:
                                case 10:
                                    result.SetInt((a.Boolean ? 1 : 0) - GetInt(b));
                                    break;
                                case 11:
                                    result.SetFloat((a.Boolean ? 1 : 0) - b.FloatValue);
                                    break;
                                default:
#if DEBUG
					                throw new InvalidOperationException();
#else
									Environment.FailFast("出错了，这里跑不到"); return default;
#endif
							}
						}

                        break;
                    case 5: //int
                        {
                            switch (signature2)
                            {
                                case 2:
                                    result.SetNumber(a.IntValue - GetDouble(b));
                                    break;
                                case 3:
                                case 4:
                                    result.SetInt(a.IntValue - (b.Boolean ? 1 : 0));
                                    break;
                                case 6:
                                    result.SetNumber((double)a.IntValue - b.UIntValue);
                                    break;
                                case 11:
                                    result.SetFloat(a.IntValue - b.FloatValue);
                                    break;
                                case 5:
                                case 7:
                                case 8:
                                case 9:
                                case 10:
                                    result.SetInt(a.IntValue - GetInt(b));
                                    break;
                                default:
#if DEBUG
					                throw new InvalidOperationException();
#else
									Environment.FailFast("出错了，这里跑不到"); return default;
#endif
							}
						}
                        break;
                    case 6: //uint
                        {
                            switch (signature2)
                            {

                                case 2:
                                    result.SetNumber(a.UIntValue - GetDouble(b));
                                    break;
                                case 3:
                                case 4:
                                    result.SetUInt(a.UIntValue - (b.Boolean ? 1U : 0U));
                                    break;
                                case 5:
                                    result.SetNumber((double)a.UIntValue - b.IntValue);
                                    break;
                                case 6:
                                    result.SetUInt(a.UIntValue - b.UIntValue);
                                    break;
                                case 7:
                                    result.SetNumber((double)a.UIntValue - b.SByteValue);
                                    break;
                                case 8:
                                    result.SetUInt(a.UIntValue - b.ByteValue);
                                    break;
                                case 9:
                                    result.SetNumber((double)a.UIntValue - b.ShortValue);
                                    break;
                                case 10:
                                    result.SetUInt(a.UIntValue - b.UShortValue);
                                    break;
                                case 11:
                                    result.SetFloat((float)a.UIntValue - b.FloatValue);
                                    break;
                                default:
#if DEBUG
					                throw new InvalidOperationException();
#else
									Environment.FailFast("出错了，这里跑不到"); return default;
#endif
							}
						}
                        break;
                    case 7:
                    case 8:
                    case 9:
                    case 10:
                        {
                            switch (signature2)
                            {
                                case 2:
                                    result.SetNumber(GetDouble(a) - GetDouble(b));
                                    break;
                                case 6:
                                    result.SetNumber(GetDouble(a) - b.UIntValue);
                                    break;
                                case 7:
                                case 8:
                                case 9:
                                case 10:
                                case 3:
                                case 4:
                                case 5:
                                    result.SetInt(GetInt(a) - GetInt(b));
                                    break;
                                case 11:
                                    result.SetFloat(GetFloat(a) - b.FloatValue);
                                    break;
                                default:
#if DEBUG
					                throw new InvalidOperationException();
#else
									Environment.FailFast("出错了，这里跑不到"); return default;
#endif
							}
						}
                        break;
                    case 11:
                        {
                            switch (signature2)
                            {

                                case 2:
                                    result.SetNumber(GetDouble(a) - GetDouble(b));
                                    break;
                                case 3:
                                case 4:
                                case 5:
                                case 6:
                                case 7:
                                case 8:
                                case 9:
                                case 10:
                                case 11:
                                    result.SetFloat(GetFloat(a) - GetFloat(b));
                                    break;
                                default:
#if DEBUG
					                throw new InvalidOperationException();
#else
									Environment.FailFast("出错了，这里跑不到"); return default;
#endif
							}
						}
                        break;
                    default:
#if DEBUG
					    throw new InvalidOperationException();
#else
						Environment.FailFast("出错了，这里跑不到"); return default;
#endif
				}




				return true;
            }

        }



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static double GetDouble(NaNBoxing v)
        {
            if (v.store == QNAN)
            {
                return double.NaN;
            }
            else if (v.store < UNDEFINED)
            {
                return v.number;
            }
            else
            {
				uint signature = (uint)(v.store >> 40) & 0xF;

				switch (signature)
				{
					case 1:
                        return double.NaN; //BoxType.Undefined;
					case 2:
                        return 0.0;// BoxType.Null;
					case 3:
					case 4:
                        return v.Boolean ? 1.0 : 0.0; //BoxType.Boolean;
					case 5:
                        return v.IntValue;// BoxType.Int;
					case 6:
                        return v.UIntValue;// BoxType.Uint;
					case 7:
                        return v.SByteValue;// BoxType.Sbyte;
					case 8:
                        return v.ByteValue;// BoxType.Byte;
					case 9:
                        return v.ShortValue;// BoxType.Short;
					case 10:
                        return v.UShortValue;  //BoxType.UShort;
					case 11:
                        return v.FloatValue;// BoxType.Float;
					case 12:
                        return double.NaN;// BoxType.HeapPtr;
					case 13:
                        return double.NaN;// BoxType.LocalString - strings cannot be converted to numbers directly
					default:
                        return double.NaN; //BoxType.Fault;
				}

			}
        }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static float GetFloat(NaNBoxing v)
		{
			if (v.store == QNAN)
			{
				return float.NaN;
			}
			else if (v.store < UNDEFINED)
			{
				return (float)v.number;
			}
			else
			{
				uint signature = (uint)(v.store >> 40) & 0xF;

				switch (signature)
				{
					case 1:
						return float.NaN; //BoxType.Undefined;
					case 2:
						return 0.0f;// BoxType.Null;
					case 3:
					case 4:
						return v.Boolean ? 1.0f : 0.0f; //BoxType.Boolean;
					case 5:
						return v.IntValue;// BoxType.Int;
					case 6:
						return v.UIntValue;// BoxType.Uint;
					case 7:
						return v.SByteValue;// BoxType.Sbyte;
					case 8:
						return v.ByteValue;// BoxType.Byte;
					case 9:
						return v.ShortValue;// BoxType.Short;
					case 10:
						return v.UShortValue;  //BoxType.UShort;
					case 11:
						return v.FloatValue;// BoxType.Float;
					case 12:
						return float.NaN;// BoxType.HeapPtr;
					case 13:
						return float.NaN;// BoxType.LocalString - strings cannot be converted to numbers directly
					default:
						return float.NaN; //BoxType.Fault;
				}

			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int GetInt(NaNBoxing v)
        {
#if DEBUG
            if (v.store == QNAN)
            {
                throw new InvalidOperationException();
            }
            else if (v.store < UNDEFINED)
            {
                throw new InvalidOperationException();
            }
            else
#endif
            {
				uint signature = (uint)(v.store >> 40) & 0xF;
				switch (signature)
                {
                    
                    case 3:
                    case 4:
                        return v.Boolean ? 1 : 0;
                    case 5:
                        return v.IntValue;
                    case 7:
                        return v.SByteValue;
                    case 8:
                        return v.ByteValue;
                    case 9:
                        return v.ShortValue;
                    case 10:
                        return v.UShortValue;
#if DEBUG
                    default:
                        throw new InvalidOperationException();
#else
                    default:
						Environment.FailFast("出错了，这里跑不到");
						return 0;
#endif
                }


            }
		}


        public BoxType ValueType
        {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
            {

                if (store < UNDEFINED)
                {
                    return BoxType.Number;
                }
                else
                {
                    uint signature = (uint)(store >> 40) & 0xF;

                    switch (signature)
                    {
                        case 1:
                            return BoxType.Undefined;
                        case 2:
                            return BoxType.Null;
                        case 3:
                        case 4:
                            return BoxType.Boolean;
                        case 5:
                            return BoxType.Int;
                        case 6:
                            return BoxType.Uint;
                        case 7:
                            return BoxType.Sbyte;
                        case 8:
                            return BoxType.Byte;
                        case 9:
                            return BoxType.Short;
                        case 10:
                            return BoxType.UShort;
                        case 11:
                            return BoxType.Float;
                        case 12:
                            return BoxType.HeapPtr;
                        case 13:
                            return BoxType.LocalString;
                        default:
                            return BoxType.Fault;
                    }
                }
            }
        }

#if DEBUG
        public static Action<int,byte,byte> _setheapptr_validator;
#endif

		//未知的堆类型 实际上运行时只在GetSaveValue内部用到!
		public const int UNKNOWN_HEAPKIND = 0xF;  
        public void SetHeapPtr(int indexofheap,byte heapkind , byte heapflag = 0)
        {
#if DEBUG
            if (_setheapptr_validator != null)
            { 
                _setheapptr_validator(indexofheap, heapkind,heapflag);
            }
#endif
		    store = TAG_HEAP_POINTER | ((ulong)(heapkind | (heapflag << 4 ) ) << 32)  | (uint)indexofheap;
        }

        public int HeapPtr
        {
            get
            {
                return (int)(store & 0xffffffff);
            }
        }

        public byte HeapKind
        {
            get
            { 
                return (byte)((store >> 32) & 0xf);
            }
        }

        public byte HeapFlag
        {
            get
            {
				return (byte)((store >> 32 >> 4) & 0xf);
			}
        }


        public void SetNumber(double value)
        {
            if (double.IsNaN(value))
            {
                store = QNAN;
            }
            else
            {
                number = value;
            }
        }


        public double Number
        {
            get
            {
                if (store == QNAN)
                {
                    return double.NaN;
                }
                else
                {
                    return number;
                }
            }
        }

        public void SetNull()
        {
            store = NULL;
        }

        public void SetUndefined()
        {
            store = UNDEFINED;
        }

        public void SetBoolean(bool v)
        {
            if (v)
                store = TRUE;
            else
                store = FALSE;
        }

        public bool Boolean
        {
            get
            {
                return store == TRUE;
            }
        }

        public void SetSByte(sbyte v)
        { 
            store = TAG_SBYTE | (uint)v;
        }

        public sbyte SByteValue
        {
            get
            {
                return (sbyte)(store & 0xffffffff);
            }
        }

        public void SetByte(byte v)
        { 
            store = TAG_BYTE | (uint)v;
        }

        public byte ByteValue
        {
            get
            {
                return (byte)(store & 0xffffffff);
            }
        }

        public void SetShort(short v)
        {
            store = TAG_SHORT | (uint)v;
        }

        public short ShortValue
        {
            get
            {
                return (short)(store & 0xffffffff);
            }
        }

        public void SetUShort(ushort v)
        { 
            store = TAG_USHORT | v;
        }

        public ushort UShortValue
        {
            get
            {
                return (ushort)(store & 0xffffffff);
            }
        }

        public void SetFloat(float v)
        {
            unsafe
            {
                uint a = 0;
                byte* b = (byte*)&a;
                float* c = (float*)b;
                *c = v;

                store = TAG_FLOAT | a;
            }
        }

        public float FloatValue
        {
            get
            { 
                unsafe
                {
                    uint s= (uint)(store & 0xffffffff);
                    float* b = (float*)&s;
                    return *b;

                }
            }
        }



        public void SetInt(int v)
        {
            store = TAG_INT | (uint)v;
        }

        public int IntValue
        {
            get
            {
                return (int)(store & 0xffffffff);
            }
        }

        public void SetUInt(uint v)
        {
            store = TAG_UINT | v;
        }

        public uint UIntValue
        {
            get
            {
             return   (uint)(store & 0xffffffff);
            }
        }

        /// <summary>
        /// 获取LocalString的字符内容到指定的Span中，避免字符串分配
        /// </summary>
        /// <param name="destination">目标字符缓冲区</param>
        /// <returns>实际写入的字符数，如果缓冲区不够大则返回-1</returns>
        public int GetLocalStringChars(Span<char> destination)
        {
            if (store == L_ZEROSTRING)
            {
                destination[0] = '\0';
                return 1;
            }

            // 提取所有5字节，然后找到实际字符串结束位置
            Span<byte> utf8Bytes = stackalloc byte[5];
            for (int i = 0; i < 5; i++)
            {
                utf8Bytes[i] = (byte)((store >> (32 - i * 8)) & 0xFF);
            }
            
            // 找到第一个零字节的位置，或使用全部5字节
            int actualLength = 5;
            for (int i = 0; i < 5; i++)
            {
                if (utf8Bytes[i] == 0)
                {
                    actualLength = i;
                    break;
                }
            }
            
            if (actualLength == 0) return 0; // 空字符串
            
        
            // 尝试解码到目标缓冲区
            return Encoding.UTF8.GetChars(utf8Bytes.Slice(0, actualLength), destination);
            
        }

        /// <summary>
        /// 获取LocalString的UTF-8字节内容
        /// </summary>
        /// <param name="destination">目标字节缓冲区</param>
        /// <returns>实际写入的字节数</returns>
        public int GetLocalStringBytes(Span<byte> destination)
        {
            if (store == L_ZEROSTRING)
            {
                if (destination.Length < 1)
                    return -1;

                destination[0] = 0;
                return 1;
            }

            // 提取所有5字节，然后找到实际字符串结束位置
            Span<byte> utf8Bytes = stackalloc byte[5];
            for (int i = 0; i < 5; i++)
            {
                utf8Bytes[i] = (byte)((store >> (32 - i * 8)) & 0xFF);
            }
            
            // 找到第一个零字节的位置，或使用全部5字节
            int actualLength = 5;
            for (int i = 0; i < 5; i++)
            {
                if (utf8Bytes[i] == 0)
                {
                    actualLength = i;
                    break;
                }
            }
            
            if (actualLength > destination.Length)
                return -1; // 缓冲区不够大
                
            utf8Bytes.Slice(0, actualLength).CopyTo(destination);
            return actualLength;
        }

        /// <summary>
        /// 获取LocalString作为字符串（为了向后兼容保留）
        /// 注意：此方法会分配字符串对象，建议使用GetLocalStringChars方法
        /// </summary>
        public string LocalStringValue
        {
            get
            {
                Span<char> chars = stackalloc char[16]; // 5个UTF-8字节最多能解码出的字符数
                int charCount = GetLocalStringChars(chars);
                
                if (charCount == 0) return string.Empty;
                if (charCount == -1) 
                {
                    // 回退到原始实现
                    Span<byte> utf8Bytes = stackalloc byte[5];
                    for (int i = 0; i < 5; i++)
                    {
                        utf8Bytes[i] = (byte)((store >> (32 - i * 8)) & 0xFF);
                    }
                    
                    int actualLength = 5;
                    for (int i = 0; i < 5; i++)
                    {
                        if (utf8Bytes[i] == 0)
                        {
                            actualLength = i;
                            break;
                        }
                    }
                    
                    if (actualLength == 0) return string.Empty;
                    return Encoding.UTF8.GetString(utf8Bytes.Slice(0, actualLength));
                }
                
                return new string(chars.Slice(0, charCount));
            }
        }

        /// <summary>
        /// 原始内容
        /// </summary>
        public ulong Raw
        {
            get
            {
                return store;
            }
        }
        
        public override string ToString()
        {
            switch (ValueType)
            {
                case BoxType.Number:
                    return $"NaNBoxing: {ValueType},{ Number }";
                case BoxType.Undefined:
                    return $"NaNBoxing: {ValueType}";
                case BoxType.Null:
                    return $"NaNBoxing: {ValueType}";
                case BoxType.Boolean:
                    return $"NaNBoxing: {ValueType},{Boolean}";
                case BoxType.Int:
                    return $"NaNBoxing: {ValueType},{IntValue}";
                case BoxType.Uint:
                    return $"NaNBoxing: {ValueType},{UIntValue}";
                case BoxType.Sbyte:
                    return $"NaNBoxing: {ValueType},{SByteValue}";
                case BoxType.Byte:
                    return $"NaNBoxing: {ValueType},{ByteValue}";
                case BoxType.Short:
                    return $"NaNBoxing: {ValueType},{ShortValue}";
                case BoxType.UShort:
                    return $"NaNBoxing: {ValueType},{UShortValue}";
                case BoxType.Float:
                    return $"NaNBoxing: {ValueType},{FloatValue}";
                case BoxType.HeapPtr:
                    return $"NaNBoxing: {ValueType},P{HeapPtr}";
                case BoxType.LocalString:
                    return $"NaNBoxing: {ValueType},\"{LocalStringValue}\"";
                case BoxType.Fault:
                default:
                    return $"NaNBoxing: Fault occurred!!";
            }

            
        }

        
        /// <summary>
        /// 安全的UTF-8编码方法（Span版本），不会抛出异常
        /// 如果编码失败，返回0
        /// </summary>
        /// <param name="str">要编码的字符串</param>
        /// <param name="destination">目标字节缓冲区</param>
        /// <returns>实际写入的字节数，失败时返回0</returns>
        private static int SafeGetUtf8Bytes(ReadOnlySpan<char> str, Span<byte> destination)
        {
            //if (string.IsNullOrEmpty(str))
            if(str.IsEmpty)
               return 0;
                
            try
            {
                // 使用替换回退策略
                var encoder = Encoding.UTF8.GetEncoder();
                encoder.Fallback = EncoderFallback.ReplacementFallback;
                
                return Encoding.UTF8.GetBytes(str, destination);
            }
            catch
            {
                // 如果仍然失败，返回0
                return 0;
            }
        }

        /// <summary>
        /// 安全的UTF-8字节长度计算，不会抛出异常
        /// </summary>
        /// <param name="str">要计算的字符串</param>
        /// <returns>UTF-8字节长度，失败时返回0</returns>
        private static int SafeGetUtf8ByteCount(ReadOnlySpan<char> str)
        {
            //if (string.IsNullOrEmpty(str))
            if(str.IsEmpty)
                return 0;
                
            try
            {
                return Encoding.UTF8.GetByteCount(str);
            }
            catch
            {
                // 如果失败，返回0
                return 0;
            }
        }

        public void SetLocalString(ReadOnlySpan<byte> utf8Bytes)
        {
            // 调用者已经确保utf8Bytes.Length <= 5 (只有5字节可用空间)
            Debug.Assert(utf8Bytes.Length <= 5, "UTF-8 bytes length should not exceed 5");

            if (utf8Bytes.Length == 1 && utf8Bytes[0] == 0)
            {
				//// \0 !!
				//ulong data = TAG_LOCAL_STRING;

				//// 存储UTF-8字节，从高位开始，剩余位置自动为0
				
				//data |= ((ulong)0) << (32 - 0 * 8);
				//data |= ((ulong)255) << (32 - 1 * 8);
				//data |= ((ulong)255) << (32 - 2 * 8);
				//data |= ((ulong)255) << (32 - 3 * 8);
				//data |= ((ulong)255) << (32 - 4 * 8);
				
               


				store = L_ZEROSTRING;

			}
            else
            {
                ulong data = TAG_LOCAL_STRING;

                // 存储UTF-8字节，从高位开始，剩余位置自动为0
                for (int i = 0; i < utf8Bytes.Length; i++)
                {
                    data |= ((ulong)utf8Bytes[i]) << (32 - i * 8);
                }

                store = data;
            }
        }

        public void setFault()
        {
            store = 0xFFF80E0000000000;
        }

        /// <summary>
        /// 尝试从字符串创建LocalString的安全接口
        /// 提供安全的LocalString创建，返回bool指示创建是否成功
        /// </summary>
        /// <param name="str">要创建LocalString的字符串（不能为null）</param>
        /// <param name="result">创建的LocalString结果</param>
        /// <returns>如果成功创建LocalString返回true，否则返回false</returns>
        public static bool TryCreateLocalString(ReadOnlySpan<char> str, out NaNBoxing result)
        {
            Debug.Assert(str != null, "String cannot be null - use SetNull() for null values");

            result = default;
                       
            //if (string.IsNullOrEmpty(str))
            if(str.IsEmpty)
            {
                result = new NaNBoxing();
                result.SetLocalString(ReadOnlySpan<byte>.Empty);
                return true;
            }
            
            int utf8ByteCount = SafeGetUtf8ByteCount(str);
            if (utf8ByteCount > 0 && utf8ByteCount <= 5)
            {
                Span<byte> utf8Bytes = stackalloc byte[utf8ByteCount];
                int actualBytes = SafeGetUtf8Bytes(str, utf8Bytes);
                if (actualBytes > 0)
                {
                    result = new NaNBoxing();
                    result.SetLocalString(utf8Bytes.Slice(0, actualBytes));
                    return true;
                }
            }
            
            return false;
        }

		public void setDefault(TypeKind returnTypeKind)
		{
            switch (returnTypeKind)
            {
                case TypeKind.Any:
                    SetUndefined();
                    break;
                case TypeKind.Boolean:
                    SetBoolean(false);
                    break;
                case TypeKind.SByte:
                    SetSByte(0);
                    break;
                case TypeKind.Byte:
                    SetByte(0);
                    break;
                case TypeKind.Short:
                    SetShort(0);
                    break;
                case TypeKind.UShort:
                    SetUShort(0);
                    break;
                case TypeKind.Int:
                    SetInt(0);
                    break;
                case TypeKind.Uint:
                    SetUInt(0);
                    break;
                case TypeKind.Float:
                    SetFloat(float.NaN);    
                    break;
                case TypeKind.Number:
                    SetNumber(double.NaN);
                    break;
                case TypeKind.Fun_Void:
                    SetUndefined();
                    break;
               
                case TypeKind.Null:
                case TypeKind.Object:
                case TypeKind.Class:
                    SetNull();
                    break;
                
                case TypeKind.String:
                case TypeKind.Function:
                case TypeKind.Array:
                case TypeKind.Vector:
                case TypeKind.Namespace:
                    SetNull();
                    break;
                
				default:
                    Debug.Assert(returnTypeKind > TypeKind.Namespace);
                    
                    SetNull();
                    break;
            }

        }
	}
}
