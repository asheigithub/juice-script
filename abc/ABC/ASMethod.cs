using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC
{
    public sealed class ASMethod
    {
        public string Name { get; set; }

        public ASMultiname ReturnType { get; set; }

        public TypeKind ReturnTypeKind { get; set; }


        public MethodFlags Flags { get; set; }

        public List<ASParameter> Parameters { get; }


        public ASTrait Trait { get; set; }

        public ASMethodBody Body { get; set; }

        public bool IsConstructor { get; set; }

        public ASContainer Container { get; private set; } // 在哪里定义的

        public bool IsAnonymous { get; set; }

        public Token Token { get; private set; }


        public int ast_function_index { get; set; }

        /// <summary>
        /// 指示是否是method方法。
        /// 在计算虚函数表时被赋值，在运行时使用
        /// </summary>
        public bool __ismethod;

        /// <summary>
        /// 是否是特殊方法 call,apply
        /// </summary>
        public bool __is_call_or_apply;

        /// <summary>
        /// hasOwnProperty是个特别函数，call和apply遇到它不要自动填充this
        /// {test262来说，所有内建函数都不要自动填充this,但是这和flash行为不一样，所以暂时就先改成只有遇到这个才跳过}
        /// </summary>
        public bool __is_hasOwnProperty;

        /// <summary>
        /// 是否引擎自动创建的原型链中对象
        /// </summary>
        public bool __is_buildin_proto;

        /// <summary>
        /// 标志是否是Vector构造的函数
        /// </summary>
        public bool __is_vector_method;

		/// <summary>
		/// 运行时链接后指定 函数的返回类型，避免hash查找
		/// </summary>
		public ASClass __return_type_class__;


        /// <summary>
        /// 如果是一个本地函数，指向它的委托
        /// </summary>
        public object nativefunction_delegate;

        
        public ASMethod(ASContainer parent, Token token)
        {
            Parameters = new List<ASParameter>();

            Container = parent;
            Token = token;

            __ismethod = false;

        }




        public void SetContainer(ASContainer parent)
        {
            Container = parent;
        }

        public override string ToString()
        {
            return $"ASMethod: {Container.QName.Namespace.Name}.{Container.QName.Name}::{Name}";
        }

    }
}
