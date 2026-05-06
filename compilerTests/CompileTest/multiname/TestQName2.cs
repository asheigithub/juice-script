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
    public class TestQName2 : CodeTestBase
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
        public var K = 1;
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

            RtScriptClass rtPayload = (RtScriptClass)globalInstance;
            NaNBoxing b = rtPayload.ReadSlot(0);

            Assert.AreEqual(b.ValueType, NaNBoxing.BoxType.Sbyte);
            Assert.AreEqual(b.SByteValue, 1);

            Assert.IsNull(ex);

        }

        [TestMethod]
        public void Test()
        {
            Run();
        }

    }
}
