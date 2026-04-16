using juicescript.runtime;
using System.Collections.Generic;
using System.Linq;

namespace compilerTests.CompileTest.vector
{
    [TestClass]
    public sealed class SomeTest : CodeTestBase
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

    var v1:Vector.<int> = new <int>[1, 3, 5, 7];
    results.push(v1.some(function(item:int):Boolean {
        return item % 2 == 0;
    }) ? 0 : 1);

    var v2:Vector.<int> = new <int>[1, 2, 3, 4, 5];
    results.push(v2.some(function(item:int):Boolean {
        return item > 3;
    }) ? 1 : 0);

    var v3:Vector.<int> = new <int>[1, 2, 3];
    results.push(v3.some(function(item:int):Boolean {
        return item > 10;
    }) ? 0 : 1);

    var v4:Vector.<int> = new <int>[];
    results.push(v4.some(function(item:int):Boolean {
        return true;
    }) ? 0 : 1);

    var v5:Vector.<int> = new <int>[10, 20, 30];
    var myObj:Object = { threshold: 25 };
    results.push(v5.some(function(item:int):Boolean {
        return item > this.threshold;
    }, myObj) ? 1 : 0);

    var p1:Point = new Point();
    p1.x = 2; p1.y = 4;
    var p2:Point = new Point();
    p2.x = 4; p2.y = 6;
    var p3:Point = new Point();
    p3.x = 6; p3.y = 8;
    var v6:Vector.<Point> = new <Point>[p1, p2, p3];
    results.push(v6.some(function(item:Point):Boolean {
        return item.x < 0;
    }) ? 0 : 1);

    var p4:Point = new Point();
    p4.x = -1; p4.y = 6;
    var p5:Point = new Point();
    p5.x = 4; p5.y = 8;
    var p6:Point = new Point();
    p6.x = 6; p6.y = 10;
    var v7:Vector.<Point> = new <Point>[p4, p5, p6];
    results.push(v7.some(function(item:Point):Boolean {
        return item.x < 0;
    }) ? 1 : 0);

    var v8:Vector.<Point> = new <Point>[];
    results.push(v8.some(function(item:Point):Boolean {
        return true;
    }) ? 0 : 1);

    var v9:Vector.<int> = new <int>[1, 2];
    var r9:Boolean = v9.some(function(item:int):Boolean {
        if (item == 1) {
            v9.push(3);
        }
        return item == 2;
    });
    results.push((r9 == true && v9.length == 3) ? 1 : 0);

    var v10:Vector.<int> = new <int>[1, 2, 3, 4, 5];
    var visited10:String = '';
    var r10:Boolean = v10.some(function(item:int):Boolean {
        visited10 += item + ',';
        if (item == 2) {
            v10.pop();
            v10.pop();
        }
        return item == 2;
    });
    results.push((visited10 == '1,2,' && v10.length == 3 && r10 == true) ? 1 : 0);

    var v11:Vector.<int> = new <int>[1, 2];
    var visited11:String = '';
    var cb11 = function(i:int, idx:int, vec:*):Boolean {
        visited11 += i + ',';
        if (idx == 0) {
            v11 = new <int>[100];
        }
        return false;
    };
    var r11:Boolean = v11.some(cb11);
    results.push((visited11 == '1,2,' && r11 == false) ? 1 : 0);

    var separator:String = ',';
    trace(results[0] + separator + results[1] + separator + results[2] + separator + results[3] + separator + results[4] + separator + results[5] + separator + results[6] + separator + results[7] + separator + results[8] + separator + results[9] + separator + results[10]);
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
            Assert.AreEqual(11, numbers.Length, "Expected 11 test results");
            for (int i = 0; i < 11; i++)
            {
                Assert.AreEqual("1", numbers[i], $"Test{i + 1} fail");
            }
        }

        [TestMethod]
        public void Test() => Run();
    }
}
