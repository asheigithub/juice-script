using MyMD5;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace juicescript.compiler.parse
{
    /// <summary>
    /// 语法树
    /// </summary>
    public class ParseTree
    {
        public ParseExpr Root;

        public readonly MyMD5.MD5Result Key;

        public ParseTree(ref MD5Result _key)
        { 
            Key = _key;
        }


        public string GetTreeString()
        {
            return Root.GetTreeString(0, '\t');
        }




    }
}
