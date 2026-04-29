using juicescript.ABC;
using juicescript.compiler.IL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler
{
    internal class VectorDef
    {
        internal TypeKind Identifier;

        internal int depth;

        internal TypeKind ElementTypeId {  get; private set; }

        private string _TypeStr_;

        internal ASVector buildVector;


        public override bool Equals(object obj)
        {
            VectorDef vectorDef = obj as VectorDef;
            if (vectorDef != null) 
            { 
                return Identifier==vectorDef.Identifier && ElementTypeId==vectorDef.ElementTypeId && depth == vectorDef.depth;      
            } 
            else 
            { 
                return false; 
            }
        }

        public override string ToString()
        {
            return _TypeStr_ == null ? string.Empty : _TypeStr_;
        }


        public override int GetHashCode()
        {
            return Identifier.GetHashCode() ^ ElementTypeId.GetHashCode() ^ depth.GetHashCode();
        }

        public static VectorDef CreateOrGet(CompileContext context, TypeKind elemnet, int depth)
        { 
            List<TypeKind> vector_types = new List<TypeKind>();
            for (int i = 0; i < depth; i++)
            {
                vector_types.Add( TypeKind.Vector);
            }

            vector_types.Add(elemnet);

            return CreateOrGet(context, vector_types);

        }

        public static VectorDef CreateOrGet(CompileContext context, List<TypeKind> vector_type)
        {
            TypeKind type = vector_type[0];
            vector_type.RemoveAt(0);

            VectorDef vectorDef = new VectorDef();
            if (type == TypeKind.Vector)
            {
                var inner = CreateOrGet(context, vector_type);
                vectorDef.ElementTypeId =inner.Identifier;
                vectorDef._TypeStr_ = GetVectorTypeStr(context, vectorDef.ElementTypeId );

                vectorDef.depth = 1+inner.depth;
            }
            else
            { 
                vectorDef.ElementTypeId = type;
                vectorDef._TypeStr_ = GetVectorTypeStr(context, type);
               
                vectorDef.depth = 0;

            }

            vectorDef.Identifier = (TypeKind) ScriptDefBuilder.GetClassId(vectorDef._TypeStr_);
            //context.dict_VectorDefs[vectorDef.Identifier] = vectorDef;
            if (!context.vectorDefs.Contains(vectorDef))
            {
                context.vectorDefs.Add(vectorDef);

                ASVector v = new ASVector();
                v.Str = vectorDef._TypeStr_;
                v.depth = vectorDef.depth;
                v.Identifier = vectorDef.Identifier;
                v.ElementType = vectorDef.ElementTypeId;
                context.player_for_compiler.MakeVectorType(v , context.scriptDefs.SelectMany(o => o.scriptClasses).Where(c => c != null));
                
                vectorDef.buildVector = v;


            }
            else
            {
                vectorDef.buildVector = context.vectorDefs.First(v => v.Identifier == vectorDef.Identifier).buildVector;
			}

            return vectorDef;
        }

        private static string GetVectorTypeStr(CompileContext context, TypeKind type)
        {
            if (context.vectorDefs.Exists( v=>v.Identifier == type ) )//dict_VectorDefs.ContainsKey(type))
            {
                return "__AS3__.vec::Vector<" + 
                    //context.dict_VectorDefs[type]._TypeStr_ 
                    context.vectorDefs.First(v=>v.Identifier == type)._TypeStr_
                    + ">";
            }
            else
            {
                switch (type)
                {
                    case TypeKind.Any:
                        return "__AS3__.vec::Vector.<*>";
                    case TypeKind.Boolean:
                        return "__AS3__.vec::Vector.<Boolean>";
                    case TypeKind.SByte:
                        return "__AS3__.vec::Vector.<sbyte>";
                    case TypeKind.Byte:
                        return "__AS3__.vec::Vector.<byte>";
                    case TypeKind.Short:
                        return "__AS3__.vec::Vector.<short>";
                    case TypeKind.UShort:
                        return "__AS3__.vec::Vector.<ushort>";
                    case TypeKind.Int:
                        return "__AS3__.vec::Vector.<int>";
                    case TypeKind.Uint:
                        return "__AS3__.vec::Vector.<uint>";
                    case TypeKind.Float:
                        return "__AS3__.vec::Vector.<float>";
                    case TypeKind.Number:
                        return "__AS3__.vec::Vector.<Number>";
                    case TypeKind.Null:
                        throw new InvalidOperationException();
                        
                    case TypeKind.String:
                        return "__AS3__.vec::Vector.<String>";
                    case TypeKind.Function:
                        return "__AS3__.vec::Vector.<Function>";
                    case TypeKind.Fun_Void:
                        throw new InvalidOperationException();
                    case TypeKind.Array:
                        return "__AS3__.vec::Vector.<Array>";
                    case TypeKind.Vector:
                        throw new InvalidOperationException();
                        
                    case TypeKind.Namespace:
                        return "__AS3__.vec::Vector.<Namespace>";
                    
                    default:

                        foreach (var s in context.scriptDefs)
                        {
                            foreach (var c in s.scriptClasses)
                            {
                                if (c != null && c.Type_identifier == (ulong)type)
                                { 
                                    return "__AS3__.vec::Vector.<" + c.QName.Namespace.Name + "." + c.QName.Name + ">";
                                }

                            }
                        }

                        foreach (var s in context.player_for_compiler.Context.libs)
                        {
                            foreach (var c in s.Classes)
                            {
                                if (c != null && c.Type_identifier == (ulong)type)
                                {
                                    return "__AS3__.vec::Vector.<" + c.QName.Namespace.Name + "." + c.QName.Name + ">";
                                }
                            }
                        }

                        throw new InvalidOperationException();
                }


            }

        }
    }
}
