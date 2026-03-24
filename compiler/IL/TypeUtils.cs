using juicescript.ABC;
using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler.IL
{
    internal class TypeUtils
    {
        public static int FindOverrideTypeId(TypeKind type,CompileContext ctx)
        {
            switch (type)
            {
                case TypeKind.Any:
                    return -1;
                case TypeKind.Boolean:
                    return 1;
                case TypeKind.SByte:
                    return 2;
                case TypeKind.Byte:
                    return 3;
                case TypeKind.Short:
                    return 4;
                case TypeKind.UShort:
                    return 5;
                case TypeKind.Int:
                    return 6;
                case TypeKind.Uint:
                    return 7;
                case TypeKind.Float:
                    return 8;
                case TypeKind.Number:
                    return 9;
                case TypeKind.Fun_Void:
                case TypeKind.TraitDataReference:
                case TypeKind.RTQName_MultiName_DataReference:
                case TypeKind.CParseNS_Traits:
                case TypeKind.RTQNameRTQNameL_N:
                case TypeKind.SearchNameSpaceFromImports:
                case TypeKind.Unknown:
                    return -1;
                case TypeKind.Null:
                    return 0;
                case TypeKind.Object:
                case TypeKind.Class:
                case TypeKind.Super:
                case TypeKind.String:
                    return 10;
                case TypeKind.Function:
                case TypeKind.Array:
                case TypeKind.Vector:
                case TypeKind.Namespace:
                    return -1;
                default:

					var alltypes = ctx.scriptDefs.SelectMany(
					s => s.scriptClasses).Union(ctx.player_for_compiler.Context.dictTypes.Select(p => p.Value)).Where(t => t != null);

					var c1 = alltypes.First(t => t.Type_identifier == (ulong)type);

					return c1.Instance._operator_type_index;
            }
		}

        public static string ToTypeString(TypeKind kind, CompileContext ctx)
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
					var alltypes = ctx.scriptDefs.SelectMany(
					s => s.scriptClasses).Union(ctx.player_for_compiler.Context.dictTypes.Select(p => p.Value)).Where(t => t != null);

					var type = alltypes.First(t => t.Type_identifier == (ulong)kind);

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



        public static bool TestImplicitConvert(TypeKind from, TypeKind to, CompileContext ctx)
        { 
            if(from == to)
                return true;

            if (from == TypeKind.Super || to == TypeKind.Super)
                return false;

            if (from == TypeKind.String || from == TypeKind.Function || from == TypeKind.Array || from == TypeKind.Vector)
            {
                return to == TypeKind.Any || to == TypeKind.Boolean || to == TypeKind.Null || to == TypeKind.Object;
            }

            if (from > TypeKind.Unknown  && to == TypeKind.Boolean)
            {
                return true;
            }

            if (from > TypeKind.Unknown && to == TypeKind.Any)
            {
                return true;
            }

            if (from == TypeKind.Any && to > TypeKind.Unknown)
            {
                return true;
            }
            if (from == TypeKind.Null && to != TypeKind.Fun_Void && to != TypeKind.Unknown)
            {
                return true;
            }

            if (from == TypeKind.Unknown || to == TypeKind.Unknown)
            {
                return false;
            }

            if (from < TypeKind.Unknown && to == TypeKind.Object)
            {
                return true;
            }

            if (from < TypeKind.Unknown && to > TypeKind.Unknown)
            {
                return false;
            }

            if (from > TypeKind.Unknown && to < TypeKind.Unknown)
            {
                return false;
            }

            if (from > TypeKind.Unknown && to > TypeKind.Unknown)
            {
                if (ctx.vectorDefs.Any(v => v.Identifier == from)) //两个任意一个是Vector,则肯定是不能转换
                {
                    return false;
                }
                if (ctx.vectorDefs.Any(v => v.Identifier == to)) //两个任意一个是Vector,则肯定是不能转换
                {
                    return false;
                }


                var alltypes = ctx.scriptDefs.SelectMany(
                    s => s.scriptClasses).Union(ctx.player_for_compiler.Context.dictTypes.Select(p => p.Value)).Where(t=>t != null);

                var c1 = alltypes.First(t => t.Type_identifier == (ulong)from);
                var c2 = alltypes.First(t => t.Type_identifier == (ulong)to);

                if (c1.Instance.IsExtend(c2.Instance)) //检查是否继承
                {
                    return true;
                }

                if (c1.Instance.IsImplements(c2.Instance)) //检查是否接口实现
                {
                    return true;
                }

                return false;

            }


            switch (from)
            {
                case TypeKind.Any:
                    return to != TypeKind.Fun_Void && to != TypeKind.Unknown;
                case TypeKind.Boolean:
                case TypeKind.SByte:
                case TypeKind.Byte:
                case TypeKind.Short:
                case TypeKind.UShort:
                case TypeKind.Int:
                case TypeKind.Uint:
                case TypeKind.Float:
                case TypeKind.Number:
                    return to <= TypeKind.Number;

                case TypeKind.Fun_Void:
                    //return to == TypeKind.Fun_Void;
                    return true;
                case TypeKind.TraitDataReference:           
                case TypeKind.RTQName_MultiName_DataReference:                   
                case TypeKind.CParseNS_Traits:              
                case TypeKind.RTQNameRTQNameL_N:               
                case TypeKind.SearchNameSpaceFromImports:               
                case TypeKind.Unknown:
                default:
                    return false;
                
            }
        }

        /// <summary>
        /// 如果索引器索引是数值类型，则判断输入也是数值类型
        /// 否则走TestImplicitConvert
        /// </summary>
        /// <param name="value_type"></param>
        /// <param name="index_type"></param>
        /// <param name="ctx"></param>
        /// <returns></returns>
        public static bool IsIndexTypePass(TypeKind value_type, TypeKind index_type, CompileContext ctx)
        {
            if (index_type.IsNumericType())
            {
                return value_type.IsNumericType() ;
            }
            else
            { 
                return TestImplicitConvert(value_type, index_type, ctx);
            }
        }
    
    }
}
