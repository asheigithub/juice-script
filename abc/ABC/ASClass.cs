using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC
{
    public sealed class ASClass : ASContainer, IEquatable<ASClass>
    {
        public static event EventHandler<ASClass> NewClass;

        public ASClass(Token token,ulong type)
        {
            Token = token;
            Type_identifier = type;
            if (NewClass != null)
            {
                NewClass(null, this);
            }
        }

        public ulong Type_identifier { get; private set; }


        public Token Token { get;  set; }

        public ASInstance Instance { get; set; }

        public ASMethod Constructor { get;  set; }

        public override bool IsStatic { get { return true; } }

        public override ASMultiname QName { get { return Instance.QName; } }

        /// <summary>
        /// 记录是否被引擎初始化
        /// </summary>
        public int __instance_index__;
        

        public bool Equals(ASClass other)
        {
            return ReferenceEquals(this, other);
        }
    }
}
