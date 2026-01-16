using juicescript.ABC;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static juicescript.NaNBoxing;

namespace juicescript.runtime
{
    public static class Extensions
    {

		public static bool CanConvertToFloatLossless (double value) 
        {
			// 处理特殊值：NaN、正无穷、负无穷（float 和 double 均可表示，视为无损）
			if (double.IsNaN(value) || double.IsInfinity(value))
				return true;

			// 1. 检查是否超出 float 的数值范围（绝对值过大）
			if (value > float.MaxValue || value < float.MinValue)
				return false; // 超出范围，转换后为无穷大，有损

			// 2. 处理接近 0 的小值（绝对值小于 float 能表示的最小正非零值）
			// float 最小正非零值约为 1.401298e-45，可通过 float.Epsilon 确认（注：float.Epsilon 是大于0的最小float值）
			double floatMinPositive = float.Epsilon; // 等价于 1.401298e-45
			if (Math.Abs(value) < floatMinPositive)
			{
				// 只有当原 double 是 0 时，转换为 float 才无损（否则会被转为 0，与原值不等）
				return value == 0.0;
			}

			// 3. 检查精度是否无损：转换为 float 后再转回 double，与原值比较
			float f = (float)value;
			return (double)f == value;
		}


        public static double GetDoubleValue(NaNBoxing v)
        {
            switch (v.ValueType)
            {
                case NaNBoxing.BoxType.Number:
                    return v.Number;
                case NaNBoxing.BoxType.Undefined:
                    return double.NaN;
                case NaNBoxing.BoxType.Null:
                    return 0.0;
                case NaNBoxing.BoxType.Boolean:
                    return v.Boolean ? 1.0 : 0.0;
                case NaNBoxing.BoxType.Int:
                    return v.IntValue;
                case NaNBoxing.BoxType.Uint:
                    return v.UIntValue;
                case NaNBoxing.BoxType.Sbyte:
                    return v.SByteValue;
                case NaNBoxing.BoxType.Byte:
                    return v.ByteValue;
                case NaNBoxing.BoxType.Short:
                    return v.ShortValue;
                case NaNBoxing.BoxType.UShort:
                    return v.UShortValue;
                case NaNBoxing.BoxType.Float:
                    return v.FloatValue;
                case NaNBoxing.BoxType.HeapPtr:
                case NaNBoxing.BoxType.Fault:
                default:
                    throw new InvalidOperationException();
            }
        }

		internal static float GetFloatValue(NaNBoxing v)
		{
			switch (v.ValueType)
			{
				case NaNBoxing.BoxType.Number:
                    throw new InvalidOperationException();
				case NaNBoxing.BoxType.Undefined:
					throw new InvalidOperationException();
				case NaNBoxing.BoxType.Null:
					throw new InvalidOperationException();
				case NaNBoxing.BoxType.Boolean:
					return v.Boolean ? 1.0f : 0.0f;
				case NaNBoxing.BoxType.Int:
					return v.IntValue;
				case NaNBoxing.BoxType.Uint:
					return v.UIntValue;
				case NaNBoxing.BoxType.Sbyte:
					return v.SByteValue;
				case NaNBoxing.BoxType.Byte:
					return v.ByteValue;
				case NaNBoxing.BoxType.Short:
					return v.ShortValue;
				case NaNBoxing.BoxType.UShort:
					return v.UShortValue;
				case NaNBoxing.BoxType.Float:
					return v.FloatValue;
				case NaNBoxing.BoxType.HeapPtr:
				case NaNBoxing.BoxType.Fault:
				default:
					throw new InvalidOperationException();
			}
		}

		internal static int GetIntValue(NaNBoxing v)
		{
			switch (v.ValueType)
			{
				case NaNBoxing.BoxType.Number:
					throw new InvalidOperationException();
				case NaNBoxing.BoxType.Undefined:
					throw new InvalidOperationException();
				case NaNBoxing.BoxType.Null:
					throw new InvalidOperationException();
				case NaNBoxing.BoxType.Boolean:
					return v.Boolean ? 1 : 0;
				case NaNBoxing.BoxType.Int:
					return v.IntValue;
				case NaNBoxing.BoxType.Uint:
					throw new InvalidOperationException();
				case NaNBoxing.BoxType.Sbyte:
					return v.SByteValue;
				case NaNBoxing.BoxType.Byte:
					return v.ByteValue;
				case NaNBoxing.BoxType.Short:
					return v.ShortValue;
				case NaNBoxing.BoxType.UShort:
					return v.UShortValue;
				case NaNBoxing.BoxType.Float:
					throw new InvalidOperationException();
				case NaNBoxing.BoxType.HeapPtr:
				case NaNBoxing.BoxType.Fault:
				default:
					throw new InvalidOperationException();
			}
		}

        public static bool IsHeapType(this TypeKind type)
        {
            return !(type > TypeKind.Any && type < TypeKind.Fun_Void);
        }



		public static bool IsExtend(this ASInstance type, ASInstance super)
        {
            var s = type;
            while (true)
            {
                if (s == super)
                    return true;

                if (s._super_class_ != null)
                {
                    s = s._super_class_.Instance;
                }
                else
                {
                    break;
                }
            }

            return false;
        }

        public static bool IsImplements(this ASInstance type, ASInstance _interface_)
        {
            if (!type.IsInterface)
            {
                return type._interface_impl_.Any(i => i._interface_.Instance == _interface_);
            }
            else
            {
                var s = type;

                while (true)
                {
                    if (s._implements_.Any(i => i.Instance == _interface_))
                    {
                        return true;
                    }

                    for (int i = 0; i < s._implements_.Count; i++)
                    {
                        if (s._implements_[i].Instance.IsImplements(_interface_))
                        {
                            return true;
                        }
                    }

                    if (s._super_class_ != null)
                    {
                        s = s._super_class_.Instance;
                    }
                    else
                    {
                        break;
                    }
                }

                return false;
            }
        }


        /// <summary>
        /// 是否是数值类型
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsNumericType(this TypeKind type)
        {
            switch (type)
            {
                
                case TypeKind.SByte:
                case TypeKind.Byte:
                case TypeKind.Short:
                case TypeKind.UShort:
                case TypeKind.Int:
                case TypeKind.Uint:
                case TypeKind.Float:
                case TypeKind.Number:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 是否是无符号整数类型
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsUnsigned(this TypeKind type)
        {
            switch (type)
            {
                case TypeKind.Byte:
                case TypeKind.UShort:
                case TypeKind.Uint:
                    return true;
                default:
                    return false;
            }
        }

        public static string ToDebugString(this TypeKind kind,Player player)
        {
            switch (kind)
            {
                case TypeKind.Any:
                    return "undefined";
                case TypeKind.Boolean:
                    return "Boolean";
                case TypeKind.SByte:
                    return "sbyte";
                case TypeKind.Byte:
                    return "byte";
                case TypeKind.Short:
                    return "short";
                case TypeKind.UShort:
                    return "ushort";
                case TypeKind.Int:
                    return "int";
                case TypeKind.Uint:
                    return "uint";
                case TypeKind.Float:
                    return "float"; 
                case TypeKind.Number:
                    return "number";
                case TypeKind.Fun_Void:
                    return "void";
                case TypeKind.TraitDataReference:
                case TypeKind.RTQName_MultiName_DataReference:
                case TypeKind.CParseNS_Traits:
                case TypeKind.RTQNameRTQNameL_N:
                case TypeKind.SearchNameSpaceFromImports:
                case TypeKind.Unknown:
                    return kind.ToString();
                case TypeKind.Null:
                    return "null";
                case TypeKind.Object:
                    return "Object";
                case TypeKind.Class:
                    return "Class";
                case TypeKind.String:
                    return "String";
                case TypeKind.Function:
                    return "Function";
                case TypeKind.Array:
                    return "Array";
                case TypeKind.Vector:
                    return "__AS3__.vec.Vector";
                case TypeKind.Namespace:
                    return "Namespace";
                default:
                    var type = player.Context.dictTypes[(ulong)kind];
                    if (string.IsNullOrEmpty(type.QName.Namespace.Name))
                    {
                        return type.QName.Name;
                    }
                    else
                    {
                        return type.QName.Namespace.Name + "." + type.QName.Name;
                    }


            }
        }

        public static string ToDebugString(this NaNBoxing value, Player player)
        {
            switch (value.ValueType)
            {
                case NaNBoxing.BoxType.Number:
                    return value.Number.ToString();
                case NaNBoxing.BoxType.Undefined:
                    return "undefined";
                case NaNBoxing.BoxType.Null:
                    return "null";
                case NaNBoxing.BoxType.Boolean:
                    return value.Boolean ? "true" : "false";
                case NaNBoxing.BoxType.Int:
                    return value.IntValue.ToString();
                case NaNBoxing.BoxType.Uint:
                    return value.UIntValue.ToString();
                case NaNBoxing.BoxType.Sbyte:
                    return value.SByteValue.ToString();
                case NaNBoxing.BoxType.Byte:
                    return value.ByteValue.ToString();
                case NaNBoxing.BoxType.Short:
                    return value.ShortValue.ToString();
                case NaNBoxing.BoxType.UShort:
                    return value.UShortValue.ToString();
                case NaNBoxing.BoxType.Float:
                    return value.FloatValue.ToString();
                case NaNBoxing.BoxType.HeapPtr:
                    {
                        RtHeapInstance instance = player.Context.GC.Heap[value.HeapPtr];

                        switch (instance.TypeKind)
                        {
                            case RtHeapTypeKind.CLASS:
                                return 

                                   (
                                    string.IsNullOrEmpty(((RtPayloadScriptClass)instance.facility).Meta.QName.Namespace.Name) ?
                                    "" :
                                    (((RtPayloadScriptClass)instance.facility).Meta.QName.Namespace.Name + ".")
                                    )
                                    +
                                    ((RtPayloadScriptClass)instance.facility).Meta.QName.Name +

                                    "$";
                               
                            case RtHeapTypeKind.GLOBAL:
                               return "[object global]";
                               
                            case RtHeapTypeKind.STRING:
                                return "'" + ((RtPayloadString)instance.facility).Str + "'";
                                
                            case RtHeapTypeKind.INSTANCE:
  
                                 return $"{(string.IsNullOrEmpty(instance.Type.QName.Namespace.Name)?string.Empty:(instance.Type.QName.Namespace.Name + "."))}{instance.Type.QName.Name}@{value.HeapPtr.ToString("x")}";
                                    

                            case RtHeapTypeKind.NAMESPACE:
                                {
                                    var ns = ((RtPayloadNameSpace)instance.facility).ASNamespace;
                                    return string.IsNullOrEmpty(ns.def_uri)? ns.Name :ns.def_uri ;
                                }
                            case RtHeapTypeKind.VECTOR:
                                {
                                    return $"{instance.Type.QName.Name}@{value.HeapPtr.ToString("x")}";
                                }
                            default:
                                throw new InvalidProgramException();
                        }

                    }
                case NaNBoxing.BoxType.Fault:
                    throw new InvalidOperationException();
                default:
                    throw new InvalidOperationException();
            }
        }


        public static string ToDebugTypeName(this ASMultiname multiname)
        {
            if (string.IsNullOrEmpty(multiname.Namespace.Name))
            {
                return multiname.Name;
            }
            else
            { 
                return multiname.Namespace.Name + "." + multiname.Name;
            }
        }

        public static string ToDebugPropertyName(this ASTrait trait)
        {
            if (!string.IsNullOrEmpty(trait.QName.Namespace.def_uri))
            {
                return $"{trait.QName.Namespace.def_uri}::{trait.QName.Name}";
            }
            else if (!string.IsNullOrEmpty(trait.QName.Namespace.Name))
            {
                return $"{trait.QName.Namespace.Name}::{trait.QName.Name}";
            }
            else
            {
                return trait.QName.Name;
            }
        }

        public static string ToDebugNameSpaceString(this ASNamespace ns)
        {
            switch (ns.Kind)
            {
                case NamespaceKind.TBD:
                    throw new InvalidOperationException();
                case NamespaceKind.Namespace:
                    return ns.Name;    
                case NamespaceKind.Package:
                    return "public";
                    
                case NamespaceKind.PackageInternal:
                    return string.IsNullOrEmpty(ns.def_uri) ? "internal" : ns.Name;
                    
                case NamespaceKind.Protected:
                case NamespaceKind.StaticProtected:
                    return "protected";
                    
                case NamespaceKind.Explicit:
                    throw new InvalidOperationException();
                case NamespaceKind.Private:
                    return "private";
                default:
                    throw new InvalidOperationException();
            }
        }



        public static int IsInaccessibleOrUndefinedOrInScript(ASContainer container,string id,out CodeScope out_scope)
        {
            var scope = container._link_codescope;
            while (scope.Kind == CodeScopeKind.Method)
            {
                scope = scope.Parent;
            }

            out_scope = scope;

            if (scope.Kind == CodeScopeKind.Script)
                return 0;

            if (scope.Kind == CodeScopeKind.Class)
            {
                if (scope.Container.Traits.Any(t => t.QName.Name == id))
                {
                    return 1;
                }

                var cls = ((ASClass)scope.Container).Instance._super_class_;
                while (cls != null)
                {
                    if (cls.Traits.Any(t => t.QName.Name == id))
                        return 1;

                    cls = cls.Instance._super_class_;
                }
                return 0;
            }
            else
            {
                if (scope.Container.Traits.Any(t => t.QName.Name == id))
                {
                    return 1;
                }

                if (((ASInstance)scope.Container)._super_class_ == null)
                {
                    return 0;
                }

                var instance = ((ASInstance)scope.Container)._super_class_.Instance;
                while (instance != null)
                {
                    if (instance.Traits.Any(t => t.QName.Name == id))
                        return 1;
                    if (instance._super_class_ != null)
                    {
                        instance = instance._super_class_.Instance;
                    }
                    else
                    {
                        break;
                    }
                }
                return 0;
            }

        }

		internal static string GetPrimitiveValueToString( Player player,NaNBoxing prop_name)
		{
			//转字符串
			switch (prop_name.ValueType)
			{
				case NaNBoxing.BoxType.Number:
					{
						double v = prop_name.Number;
						if (double.IsPositiveInfinity(v))
						{
							return "Infinity";
						}
						else if (double.IsNegativeInfinity(v))
						{
							return "-Infinity";
						}
						else if (double.IsNaN(v))
						{
							return "NaN";
						}
						else
						{                           
							return  v.ToString();
						}
						
					}
				case NaNBoxing.BoxType.Float:
					{
						float v = prop_name.FloatValue;
						if (float.IsPositiveInfinity(v))
						{
							return "Infinity";
						}
						else if (float.IsNegativeInfinity(v))
						{
							return "-Infinity";
						}
						else if (float.IsNaN(v))
						{
							return "NaN";
						}
						else
						{
							return v.ToString();
						}
						
					}
				case NaNBoxing.BoxType.Undefined:
					return "undefined";
					
				case NaNBoxing.BoxType.Null:
					return "null";
					
				case NaNBoxing.BoxType.Boolean:
					return prop_name.Boolean ? "true" : "false";
					
				case NaNBoxing.BoxType.Int:
					{
						var index = prop_name.IntValue;
						return index.ToString();
						
					}
				case NaNBoxing.BoxType.Uint:
					{
						var index = prop_name.UIntValue;
						return index.ToString();
						
					}
				case NaNBoxing.BoxType.Sbyte:
					{
						var index = prop_name.SByteValue;
						return index.ToString();
						
					}
				case NaNBoxing.BoxType.Byte:
					{
						var index = prop_name.ByteValue;
						return index.ToString();
						
					}
				case NaNBoxing.BoxType.Short:
					{
						var index = prop_name.ShortValue;
						return index.ToString();
						
					}
				case NaNBoxing.BoxType.UShort:
					{
						var index = prop_name.ShortValue;
						return index.ToString();
						
					}
                case BoxType.HeapPtr:
                    {
                        
                        var instance = (player.Context.GC.Heap[prop_name.HeapPtr]);

                        if (instance.TypeKind == RtHeapTypeKind.STRING)
                        {
                            return ((RtPayloadString)instance.facility).Str;
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }
                    }
				case NaNBoxing.BoxType.Fault:
				default:
					throw new InvalidOperationException();
			}
		}

		
	}
}
