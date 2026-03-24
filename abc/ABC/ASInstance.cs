using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC
{
    public sealed class ASInstance : ASContainer
    {
        public sealed class interface_impl : List<int>
        {
            public interface_impl(ASClass intf)
            {
                interface_type = intf.Type_identifier;
                _interface_ = intf;
            }
			public readonly ulong interface_type;
            public readonly ASClass _interface_;

		}

        public ClassFlags Flags { get; set; }
        public bool IsInterface => Flags.HasFlag(ClassFlags.Interface);

        public ASMultiname Super { get; set; }

        public ASMethod Constructor { get; set; }


        private readonly ASMultiname _qname_;
        public override ASMultiname QName { get { return _qname_; } }

        public ASNamespace ProtectedNamespace { get; set; }

        public List<ASMultiname> Interfaces { get; private set; }


        public ASClass _super_class_ { get; set; }


        public List<ASClass> _implements_ { get; set; }

        public List<interface_impl> _interface_impl_ { get; set; }



        public ASClass _element_class { get; set; }



        //如果标志有索引器，则保存具体索引器函数
        public ASMethod indexer_get;
        public ASMethod indexer_set;
        public ASMethod indexer_delete;

        //获取迭代器的方法
        public ASMethod iterator;


        //用于操作符重载时，操作数类型编号，动态生成。
        public int _operator_type_index;
        public ASClass _operator_type;

        public ASInstance(ASMultiname _qname_)
        {
            this._qname_ = _qname_;
            Interfaces = new List<ASMultiname>();

            _element_class = null;

            _implements_ = new List<ASClass>(); 

            _interface_impl_ = new List<interface_impl>();


            _operator_type_index = -1;
            _operator_type = null;
        }

    }


   

}
