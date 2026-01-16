using MyMD5;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler.AST
{

    public sealed class AS3SrcFile 
    {
        public readonly MD5Result key;
        public string sourceFile;
        public string sourceFileFullpath;
        public AS3SrcFile(string sourceFile,string fullPath , MD5Result key)
        {
            this.sourceFile = sourceFile;
            this.sourceFileFullpath = fullPath;
            OutPackage = new AS3OutPackage(this);
            this.key = key;
        }

        public AS3Package Package;

        public AS3OutPackage OutPackage;


        internal List<AS3Function> _functions = new List<AS3Function>();
        internal List<AS3Expression> _expressions = new List<AS3Expression>();

        public void Write(System.Text.StringBuilder out_sb)
        {
            Package.Write(0, out_sb);
            OutPackage.Write(0, out_sb);
        }
    }
}
