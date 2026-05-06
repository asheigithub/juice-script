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
    public class TestQName4 : CodeTestBase
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
        AS3 var K = 2;
    }
}
var b = new Main();

var c = b.AS3::K;

var d = b.public::K;

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

            Assert.IsNull(ex);

            RtScriptClass rtPayload = (RtScriptClass)globalInstance;
            NaNBoxing b = rtPayload.ReadSlot(0);
            NaNBoxing c = rtPayload.ReadSlot(1);
            NaNBoxing d = rtPayload.ReadSlot(2);

            Assert.AreEqual(c.ValueType, NaNBoxing.BoxType.Sbyte);
            Assert.AreEqual(d.ValueType, NaNBoxing.BoxType.Sbyte);

            Assert.AreEqual(c.SByteValue, 2);
            Assert.AreEqual(d.SByteValue, 1);


        }

        [TestMethod]
        public void Test()
        {
            Run();
        }

    }
}
