using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests
{
    public class TestCodeFile
    {
        public string Code;

        public string Path;

        public string FileName
        {
            get
            { 
                return System.IO.Path.GetFileName(this.Path);
            }
        }

    }
}
