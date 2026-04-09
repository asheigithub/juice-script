using juicescript.runtime;
using System.Collections.Generic;
using System.Linq;

namespace compilerTests.CompileTest.vector
{
    [TestClass]
    public sealed class PopTest : CodeTestBase
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
    var popped1:int = v1.pop();
    results.push((v1.length == 2 && popped1 == 3) ? 1 : 0);

    var v2:Vector.<int> = new <int>[1, 2, 3, 4, 5];
    var popped2:int = v2.pop();
    results.push((v2.length == 4 && v2[3] == 4 && popped2 == 5) ? 1 : 0);

    var v3:Vector.<int> = new <int>[100];
    var popped3:int = v3.pop();
    results.push((v3.length == 0 && popped3 == 100) ? 1 : 0);

    var v7:Vector.<int> = new <int>[10, 20, 30];
    var p7a:int = v7.pop();
    var p7b:int = v7.pop();
    var p7c:int = v7.pop();
    results.push((v7.length == 0 && p7a == 30 && p7b == 20 && p7c == 10) ? 1 : 0);

    var v8:Vector.<Array> = new <Array>[[1,2]];
    var popped8:Array = v8.pop();
    results.push((v8.length == 0 && popped8[0] == 1) ? 1 : 0);

    var inner:Vector.<int> = new <int>[1, 2];
    var v9:Vector.<Vector.<int>> = new <Vector.<int>>[inner];
    var popped9:Vector.<int> = v9.pop();
    results.push((v9.length == 0 && popped9[0] == 1) ? 1 : 0);

    var v10:Vector.<int> = new <int>[5, 10];
    var popped10:int = v10.pop();
    results.push((v10.length == 1 && v10[0] == 5 && popped10 == 10) ? 1 : 0);

    var v11:Vector.<String> = new <String>['a', 'b'];
    var popped11:String = v11.pop();
    results.push((v11.length == 1 && v11[0] == 'a' && popped11 == 'b') ? 1 : 0);

    var v12:Vector.<int> = new <int>[];
    var popped12:int = v12.pop();
    results.push((v12.length == 0) ? 1 : 0);

    var separator:String = ',';
    trace(results[0] + separator + results[1] + separator + results[2] + separator + results[3] + separator + results[4] + separator + results[5] + separator + results[6] + separator + results[7] + separator + results[8]);
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
            Assert.AreEqual(9, numbers.Length, "Expected 9 test results");
            Assert.AreEqual("1", numbers[0], "Test1(basic pop) fail");
            Assert.AreEqual("1", numbers[1], "Test2(pop remaining) fail");
            Assert.AreEqual("1", numbers[2], "Test3(length 1) fail");
            Assert.AreEqual("1", numbers[3], "Test4(multiple pop) fail");
            Assert.AreEqual("1", numbers[4], "Test5(pop Array) fail");
            Assert.AreEqual("1", numbers[5], "Test6(pop Vector) fail");
            Assert.AreEqual("1", numbers[6], "Test7(pop length 2) fail");
            Assert.AreEqual("1", numbers[7], "Test8(pop String) fail");
            Assert.AreEqual("1", numbers[8], "Test9(pop empty) fail");
        }

        [TestMethod]
        public void Test() => Run();
    }
}