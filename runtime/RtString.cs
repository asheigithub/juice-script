using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.runtime
{
    /// <summary>
    /// 运行时字符串负载
    /// </summary>
    public class RtString :RtHeapBase
    {
        private string _str;
        public string Str
        {
            get 
            {
                return _str;
            }

            //internal set
            //{ 
            //    _str = value;
            //}

        }

        public RtString(string value):base( RtHeapTypeKind.STRING)
        {
#if DEBUG
            if(value == null)
                throw new ArgumentNullException("value");
#endif
            _str = value;
        }



        public override int Size
        {
            get 
            { 
                //MetaPointer + string pointer + 
                //C# 字符串内部utf16存储，因此占用字节数就是长度*2
                return 8 + 8 + Str.Length * 2;
            }
        }


        public override string ToString()
        {
            return Str;
        }

    }

}
