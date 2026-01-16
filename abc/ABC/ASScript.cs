using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC
{
    public sealed class ASScript : ASContainer
    {
        public override ASMultiname QName
        {
            get
            {
                return Traits[0].QName;
            }
        }

        public ASMethod Initializer { get; set; }

        /// <summary>
        /// 记录此脚本文件中所有的Container
        /// Container在运行时就是一个上下文，其中可能保存有变量
        /// </summary>
        public List<ASContainer> allContainers { get; set; }

        /// <summary>
        /// 编译或link时计算
        /// </summary>
        public List<CodeScope> codeScopes { get; set; }



        /// <summary>
        /// 记录是否被引擎初始化
        /// </summary>
        public int __global_index__;

        public ASScript()
        { 
            
        }

    }
}
