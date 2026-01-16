using juicescript.ABC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript
{
    public class VTableItem
    {
        /// <summary>
        /// 如果是是覆写overide，则!=SuperTrait,如果是继承自基类，则 == SuperTrait.如果不是继承或者覆写的，则SuperTrait和InheritFrom都为null.
        /// </summary>
        public ASTrait Trait;

        /// <summary>
        /// 定义在哪个类
        /// </summary>
        public ASContainer DefineAt;

        ///// <summary>
        ///// 继承自基类的版本，不是继承的则为null
        ///// </summary>
        //public ASTrait SuperTrait;
        /// <summary>
        /// 从哪个类继承，如果不是继承的则为null
        /// </summary>
        public ASContainer InheritFrom;


        public override string ToString()
        {
            return $"{Trait}, Override:{InheritFrom}";
        }

    }

    public class VTable
    {
        public List<VTableItem> Items;

    }
}
