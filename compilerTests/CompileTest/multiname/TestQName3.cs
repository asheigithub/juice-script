using juicescript.runtime;
using juicescript;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.multiname
{
    [TestClass]
    public class TestQName3 : CodeTestBase
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
    public class Main
    {
        var K = 1;
    }
}
var b = new Main();

b = b.K;

"
                }


                );


            return project;

        }

        protected override void TestIsPass(Player player, PlayerException ex)
        {
            var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Main");
            Assert.IsNotNull(global);
            var globalInstance = player.Context.GC.Heap[global.__global_index__];
            Assert.IsNotNull(globalInstance);

            RtPayloadScriptClass rtPayload = (RtPayloadScriptClass)globalInstance.facility;
            NaNBoxing b = rtPayload.ReadSlot(0);

            Assert.IsNotNull(ex);

            Assert.IsTrue(ex.ToDebugMessage().EndsWith("Property K not found on Main and there is no default value."));


        }

        [TestMethod]
        public void Test()
        {
            Run();
        }

    }
}
