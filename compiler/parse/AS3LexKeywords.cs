using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler.parse
{
    public class AS3LexKeywords
    {
        public static readonly string[] LEXKEYWORDS = { "CONFIG::", "...", "..", "..*", "++", "--", "||", "||=", ":*", "::"
                                                            , "&&", "&&=", "<<", ">>", ">>>", "<=", ">="
                                                            , "==", "!=", "===", "!==", "Vector.<"
                                                            , "+=", "-=", "*=", "/=", "%=", ">>=", "<<=", ">>>=", "&=", "^=", "|="};

        public static readonly string[] LEXSKIPBLANKWORDS = { ".*", "default:", ":void" ,".internal",".public",".private",".protected","public::","protected::","internal::","private::"};

    }
}
