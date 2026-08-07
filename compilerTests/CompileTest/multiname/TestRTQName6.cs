using juicescript.runtime;
using juicescript;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using juicescript.compiler;

namespace compilerTests.CompileTest.multiname
{
    [TestClass]
    public class TestRTQName6 : CodeTestBase
    {
        protected override TestCodeProject LoadProject()
        {
            TestCodeProject project = new TestCodeProject();

            project.libs = [Juice_GlobalSwc];

            project.testCodes = new List<TestCodeFile>();

            
            project.testCodes.Add(
                new TestCodeFile()
                {
                    Path = "Main.as",
                    Code = @"

package 
{
	
	 [Doc]
    public class Main {
		
		public static var LLL;
		
		namespace KK = AS3;
		
		var MM;
		
        public function Main() {
			
            var b ;b= KK::MM;
        }

       
    }
}


var m = new Main();


"
				}

                );


            return project;
        }

        protected override void TestIsPass(Player player, PlayerException ex)
        {
           
          
        }


        [TestMethod]
        public void Test()
        {
            bool raise = false;
            try
            {
				Run();
			}
            catch (ResolverException)
            {
                raise = true;
            }
            Assert.IsTrue( raise );
        }
    }
}
