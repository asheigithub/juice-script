using juicescript.runtime;
using juicescript;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using juicescript.compiler.parse;

namespace compilerTests.CompileTest.multiname
{
    [TestClass]
    public class TestQName5 : CodeTestBase
    {
        protected override TestCodeProject LoadProject()
        {

            TestCodeProject project = new TestCodeProject();

            project.libs = [Juice_GlobalSwc];

            project.testCodes = new List<TestCodeFile>();

            project.testCodes.Add(
                   new TestCodeFile()
                   {
                       Path = "ns1/TNS.as",
                       Code = @"
package ns1 
{
	public namespace TNS;
}
"
                   }
                );

            project.testCodes.Add(
                new TestCodeFile()
                {
                    Path = "Main.as",
                    Code = @"
package
{
    [Doc]
    public class Main
    {
        public var K = 1;
        TNS var K = 2;
    }
}
var b = new Main();

var c = b.TNS::K;

var d = b.public::K;

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
            bool haserror = false;
            try
            {
                Run();
            }
            catch (SyntaxException ex)
            {
                Assert.AreEqual(ex.Message, "Namespace was not found or is not a compile-time constant.");
                haserror = true;
            }

            Assert.IsTrue(haserror);

        }

    }
}
