using juicescript.runtime;
using System.Collections.Generic;
using System.Linq;

namespace compilerTests.CompileTest.vector
{
    [TestClass]
    public sealed class IndexOfTest : CodeTestBase
    {
        private const string testCode = @"
package {
    import flash.display.Sprite;

    [Doc]
    public class Main extends Sprite {
        public function Main() {
        }
    }
}

var testMain:Main = new Main();

function runTest():void {
    var results:Array = [];

    var v1:Vector.<int> = new <int>[1, 2, 3];
    results.push((v1.indexOf(1) == 0) ? 1 : 0);
    results.push((v1.indexOf(2) == 1) ? 1 : 0);
    results.push((v1.indexOf(3) == 2) ? 1 : 0);
    results.push((v1.indexOf(99) == -1) ? 1 : 0);

    var v2:Vector.<int> = new <int>[1, 2, 3, 4, 5];
    results.push((v2.indexOf(3, 2) == 2) ? 1 : 0);
    results.push((v2.indexOf(3, 3) == -1) ? 1 : 0);
    results.push((v2.indexOf(1, -2) == -1) ? 1 : 0);
    results.push((v2.indexOf(5, -1) == 4) ? 1 : 0);

    var v3:Vector.<int> = new <int>[];
    results.push((v3.indexOf(1) == -1) ? 1 : 0);

    var v4:Vector.<String> = new <String>['a', 'b', 'c'];
    results.push((v4.indexOf('b') == 1) ? 1 : 0);
    results.push((v4.indexOf('d') == -1) ? 1 : 0);

    var v5:Vector.<int> = new <int>[1, 2, 3, 2, 1];
    results.push((v5.indexOf(2) == 1) ? 1 : 0);
    results.push((v5.indexOf(2, 2) == 3) ? 1 : 0);

    var separator:String = ',';
    trace(results[0] + separator + results[1] + separator + results[2] + separator + results[3] + separator + results[4] + separator + results[5] + separator + results[6] + separator + results[7] + separator + results[8] + separator + results[9] + separator + results[10] + separator + results[11] + separator + results[12]);
}
runTest();
";

        protected override TestCodeProject LoadProject()
        {
            TestCodeProject project = new TestCodeProject();
            project.libs = [Juice_GlobalSwc];
            project.testCodes = new List<TestCodeFile>();

            project.testCodes.Add(
                new TestCodeFile()
                {
                    Path = "Main.as",
                    Code = testCode
                }
            );

            return project;
        }

        protected override void TestIsPass(Player player, PlayerException ex)
        {
            player.ForceGC();
            var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Main");
            Assert.IsNotNull(global);
            var globalInstance = player.Context.GC.Heap[global.__global_index__];
            Assert.IsNotNull(globalInstance);
            Assert.IsNull(ex);

            StringPrint print = (StringPrint)player.Print;
            var output = print.GetOutput();

            var results = output.Trim().Split('\n').Select(s => s.Trim()).ToArray();
            Assert.AreEqual(1, results.Length, "Expected 1 line of output");

            var numbers = results[0].Split(new char[]{','}, StringSplitOptions.RemoveEmptyEntries);
            Assert.AreEqual(13, numbers.Length, "Expected 13 test results");
            for (int i = 0; i < 13; i++)
            {
                Assert.AreEqual("1", numbers[i], $"Test{i + 1} fail");
            }
        }

        [TestMethod]
        public void Test() => Run();
    }
}
