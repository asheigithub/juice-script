using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC
{
    /// <summary>
    /// Trait—A trait is a fixed-name property shared by all objects that are instances of the same class;
    /// a set of traits expresses the type of an object.
    /// Trait——特征 一个固定名称的属性，由同一类实例的所有对象共享；一组特征表示对象的类型。
    /// </summary>
    public sealed class ASTrait
    {
        public ASMultiname QName { get; set; }


        private ASMultiname _type_;
        public ASMultiname Type
        {
            get
            {
                if (Kind == TraitKind.Slot || Kind == TraitKind.Constant)
                {
                    return _type_;
                }
                else
                    return null;
            }
             set { _type_ = value; }
        }

        public TypeKind TypeKind { get; set; }


        /// <summary>
        /// 运行时链接后，将trait的type和class关联，避免hash查找
        /// </summary>
        public ASClass __rt_type_class__;



        private ASMethod _method_;
        public ASMethod Method
        {
            get
            {
                if (Kind == TraitKind.Method ||
                    Kind == TraitKind.Getter ||
                    Kind == TraitKind.Setter)
                {
                    return _method_;
                }
                return null;
            }

             set
            {
                _method_ = value;
            }

        }

        private ASMethod _function_;
        public ASMethod Function
        {
            get
            {
                if (Kind != TraitKind.Function) return null;
                return _function_;
            }

            set
            {
                _function_ = value;
            }
        }

        private ASClass _class_;
        public ASClass Class
        {
            get
            {
                if (Kind != TraitKind.Class) return null;
                return _class_;
            }

             set
            {
                _class_ = value;
            }
        }


        public List<ASMeta> ASMetadata { get; private set; }

        //public object Value { get; set; }

        public TraitValue Value { get; set; }

        public ConstantKind ValueKind { get; set; }


        public bool IsStatic { get;  set; }

        public TraitKind Kind { get; set; }
        public TraitAttributes Attributes { get; set; }

        public ASTrait(Token token)
        {
            ASMetadata = new List<ASMeta>();
            Token = token;
            TypeKind = TypeKind.Unknown;
        }

        public Token Token { get; private set; }

        public override string ToString()
        {
            return Kind + ": " + QName.Name;
        }


        #region internal class

        public enum TraitValueType
        { 
            NameSpace,
            AS3Function,
            AS3Expression
        }

        public class TraitValue
        {
            public readonly TraitValueType ValueType;
            public ASNamespace Namespace;
            public int FunctionOrExpression_Index;

            public object _value;

            public NaNBoxing? initValue;


            public TraitValue(ASNamespace @namespace)
            {
                ValueType = TraitValueType.NameSpace;
                Namespace = @namespace;
            }

            public TraitValue(TraitValueType type,int index)
            { 
                ValueType=type;
                FunctionOrExpression_Index = index;
            }

        }

        #endregion


    }
}
