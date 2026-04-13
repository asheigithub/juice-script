using juicescript.runtime;
using System.Collections.Generic;
using System.Linq;

namespace compilerTests.CompileTest.vector
{
    [TestClass]
    public sealed class SortTest : CodeTestBase
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

[struct]
final class Point {
    public var x:int = 0;
    public var y:int = 0;
}

function runTest():void {
    var results:Array = [];

    var v1:Vector.<int> = new <int>[3, 1, 2];
    v1.sort(0);
    results.push((v1[0] == 1 && v1[1] == 2 && v1[2] == 3) ? 1 : 0);

    var v2:Vector.<int> = new <int>[3, 1, 2];
    v2.sort(2);
    results.push((v2[0] == 3 && v2[1] == 2 && v2[2] == 1) ? 1 : 0);

    var v3:Vector.<String> = new <String>['c', 'a', 'b'];
    v3.sort(0);
    results.push((v3[0] == 'a' && v3[1] == 'b' && v3[2] == 'c') ? 1 : 0);

    var v4:Vector.<String> = new <String>['C', 'a', 'b'];
    v4.sort(1);
    results.push((v4[0] == 'a' && v4[1] == 'b' && v4[2] == 'C') ? 1 : 0);

    var v5:Vector.<String> = new <String>['3', '1', '2'];
    v5.sort(16);
    results.push((v5[0] == '1' && v5[1] == '2' && v5[2] == '3') ? 1 : 0);

    var v6:Vector.<int> = new <int>[3, 1, 2];
    v6.sort(100);
    results.push((v6[0] == 1 && v6[1] == 2 && v6[2] == 3) ? 1 : 0);

    var v7:Vector.<Point> = new <Point>[new Point(), new Point(), new Point()];
    v7[0].x = 3; v7[0].y = 30;
    v7[1].x = 1; v7[1].y = 10;
    v7[2].x = 2; v7[2].y = 20;
    v7.sort(function(a:Point, b:Point):int {
        if (a.x > b.x) return 1;
        if (a.x < b.x) return -1;
        return 0;
    });
    results.push((v7[0].x == 1 && v7[1].x == 2 && v7[2].x == 3) ? 1 : 0);

    var v8:Vector.<int> = new <int>[3, 1, 2];
    v8.sort(function(a:int, b:int):String {
        if (a > b) return '1';
        if (a < b) return '-1';
        return '0';
    });
    results.push((v8[0] == 1 && v8[1] == 2 && v8[2] == 3) ? 1 : 0);

    var v9:Vector.<int> = new <int>[3, 1, 2];
    v9.sort(function(a:int, b:int):Object {
        if (a > b) return 1;
        if (a < b) return -1;
        return 0;
    });
    results.push((v9[0] == 1 && v9[1] == 2 && v9[2] == 3) ? 1 : 0);

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
            for (int i = 0; i < 9; i++)
            {
                Assert.AreEqual("1", numbers[i], $"Test{i + 1} fail");
            }
        }

        [TestMethod]
        public void Test() => Run();
    }
}