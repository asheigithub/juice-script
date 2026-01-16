using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler.AST
{
    /// <summary>
    /// 标记可以包含import列表
    /// </summary>
    public interface IAS3ImportList
    {
        List<string> Imports { get; }
    }
}
