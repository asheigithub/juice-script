using juicescript;
using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.multiname
{
    [TestClass]
    public class TestQName1 : CodeTestBase
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
        
    }
}
var b = new Main();
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

            Assert.AreEqual(b.ValueType, NaNBoxing.BoxType.HeapPtr);
            RtHeapBase b_v = player.Context.GC.Heap[b.HeapPtr];
            Assert.IsNotNull(b_v);
            Assert.AreEqual(b_v.TypeKind, RtHeapTypeKind.INSTANCE);

            Assert.AreEqual(b_v.Type.QName, global.QName);

            Assert.IsNull(ex);
        }

        [TestMethod]
        public void Test()
        {
            Run();
        }

    }
}
