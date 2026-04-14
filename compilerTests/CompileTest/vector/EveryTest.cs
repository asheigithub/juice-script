using juicescript.runtime;
using System.Collections.Generic;
using System.Linq;

namespace compilerTests.CompileTest.vector
{
    [TestClass]
    public sealed class EveryTest : CodeTestBase
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

    var v1:Vector.<int> = new <int>[2, 4, 6, 8];
    results.push(v1.every(function(item:int):Boolean {
        return item % 2 == 0;
    }) ? 1 : 0);

    var v2:Vector.<int> = new <int>[1, 2, 3, 4, 5];
    results.push(v2.every(function(item:int):Boolean {
        return item < 3;
    }) ? 0 : 1);

    var v3:Vector.<int> = new <int>[1, 2, 3];
    results.push(v3.every(function(item:int):Boolean {
        return item > 10;
    }) ? 0 : 1);

    var v4:Vector.<int> = new <int>[];
    results.push(v4.every(function(item:int):Boolean {
        return false;
    }) ? 1 : 0);

    var v5:Vector.<int> = new <int>[10, 20, 30];
    var myObj:Object = { threshold: 5 };
    results.push(v5.every(function(item:int):Boolean {
        return item > this.threshold;
    }, myObj) ? 1 : 0);

    var p1:Point = new Point();
    p1.x = 2; p1.y = 4;
    var p2:Point = new Point();
    p2.x = 4; p2.y = 6;
    var p3:Point = new Point();
    p3.x = 6; p3.y = 8;
    var v6:Vector.<Point> = new <Point>[p1, p2, p3];
    results.push(v6.every(function(item:Point):Boolean {
        return item.x > 0 && item.y > 0;
    }) ? 1 : 0);

    var p4:Point = new Point();
    p4.x = 2; p4.y = 4;
    var p5:Point = new Point();
    p5.x = -1; p5.y = 6;
    var p6:Point = new Point();
    p6.x = 6; p6.y = 8;
    var v7:Vector.<Point> = new <Point>[p4, p5, p6];
    results.push(v7.every(function(item:Point):Boolean {
        return item.x > 0;
    }) ? 0 : 1);

    var v8:Vector.<Point> = new <Point>[];
    results.push(v8.every(function(item:Point):Boolean {
        return false;
    }) ? 1 : 0);

    var separator:String = ',';
    trace(results[0] + separator + results[1] + separator + results[2] + separator + results[3] + separator + results[4] + separator + results[5] + separator + results[6] + separator + results[7]);
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
            Assert.AreEqual(8, numbers.Length, "Expected 8 test results");
            for (int i = 0; i < 8; i++)
            {
                Assert.AreEqual("1", numbers[i], $"Test{i + 1} fail");
            }
        }

        [TestMethod]
        public void Test() => Run();
    }
}
