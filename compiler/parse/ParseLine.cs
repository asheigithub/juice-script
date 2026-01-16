using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler.parse
{
    /// <summary>
    /// 文法行
    /// </summary>
    public class ParseLine
    {
        /// <summary>
        /// 左边定义
        /// </summary>
        public ParseNode Main;

        /// <summary>
        /// 导出式
        /// </summary>
        public List<ParseNode> Derivation = new List<ParseNode>();

        public override string ToString()
        {
            var names = new List<string>();
            for (int i = 0; i < Derivation.Count; i++) { names.Add(Derivation[i].Name); }

            return Main.Name + "->" + string.Join(" ", names.ToArray());
        }


    }
}
