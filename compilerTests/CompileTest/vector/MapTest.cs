using juicescript.runtime;
using System.Collections.Generic;
using System.Linq;

namespace compilerTests.CompileTest.vector
{
    [TestClass]
    public sealed class MapTest : CodeTestBase
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

    var v1:Vector.<int> = new <int>[1, 2, 3, 4, 5];
    var r1:Vector.<int> = v1.map(function(item:int):int {
        return item * 2;
    });
    results.push((r1.length == 5 && r1[0] == 2 && r1[1] == 4 && r1[2] == 6 && r1[3] == 8 && r1[4] == 10) ? 1 : 0);

    var v2:Vector.<int> = new <int>[1, 2, 3];
    var r2:Vector.<int> = v2.map(function(item:int, index:int):int {
        return item + index;
    });
    results.push((r2.length == 3 && r2[0] == 1 && r2[1] == 3 && r2[2] == 5) ? 1 : 0);

    var v3:Vector.<int> = new <int>[];
    var r3:Vector.<int> = v3.map(function(item:int):int {
        return item * 2;
    });
    results.push((r3.length == 0) ? 1 : 0);

    var v4:Vector.<int> = new <int>[10, 20, 30];
    var myObj:Object = { multiplier: 3 };
    var r4:Vector.<int> = v4.map(function(item:int):int {
        return item * this.multiplier;
    }, myObj);
    results.push((r4.length == 3 && r4[0] == 30 && r4[1] == 60 && r4[2] == 90) ? 1 : 0);

    var p1:Point = new Point();
    p1.x = 1; p1.y = 2;
    var p2:Point = new Point();
    p2.x = 3; p2.y = 4;
    var p3:Point = new Point();
    p3.x = 5; p3.y = 6;
    var v5:Vector.<Point> = new <Point>[p1, p2, p3];
    var r5:Vector.<Point> = v5.map(function(item:Point):Point {
        var result:Point = new Point();
        result.x = item.x * 2;
        result.y = item.y * 2;
        return result;
    });
    results.push((r5.length == 3 && r5[0].x == 2 && r5[0].y == 4 && r5[1].x == 6 && r5[1].y == 8 && r5[2].x == 10 && r5[2].y == 12) ? 1 : 0);

    var v6:Vector.<int> = new <int>[100, 200, 300];
    var r6:Vector.<int> = v6.map(function(item:int, index:int, vector:Vector.<int>):int {
        return item + index * 10;
    });
    results.push((r6.length == 3 && r6[0] == 100 && r6[1] == 210 && r6[2] == 320) ? 1 : 0);

    var v7:Vector.<int> = new <int>[1, 2, 3];
    var sum:int = 0;
    var r7:Vector.<int> = v7.map(function(item:int):int {
        sum += item;
        return item;
    });
    results.push((sum == 6) ? 1 : 0);

    var v8:Vector.<Point> = new <Point>[];
    var r8:Vector.<Point> = v8.map(function(item:Point):Point {
        var result:Point = new Point();
        result.x = item.x * 2;
        return result;
    });
    results.push((r8.length == 0) ? 1 : 0);

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
