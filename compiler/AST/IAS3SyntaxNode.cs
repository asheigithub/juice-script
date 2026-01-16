using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler.AST
{
    /// <summary>
    /// 语法节点
    /// </summary>
    public interface IAS3SyntaxNode
    {
        Token Token { get; }

        void Write(int v, StringBuilder out_sb);
    }
}
