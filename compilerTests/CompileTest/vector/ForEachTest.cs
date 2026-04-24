using juicescript.runtime;
using System.Collections.Generic;
using System.Linq;

namespace compilerTests.CompileTest.vector
{
    [TestClass]
    public sealed class ForEachTest : CodeTestBase
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

    var sum1:int = 0;
    var v1:Vector.<int> = new <int>[1, 2, 3, 4, 5];
    v1.forEach(function(item:int):void {
        sum1 += item;
    });
    results.push((sum1 == 15) ? 1 : 0);

    var result2:String = '';
    var v2:Vector.<int> = new <int>[10, 20, 30];
    v2.forEach(function(item:int, index:int):void {
        result2 += index + ':' + item + ',';
    });
    results.push((result2 == '0:10,1:20,2:30,') ? 1 : 0);

    var sum3:int = 0;
    var v3:Vector.<int> = new <int>[10, 20, 30];
    var myObj:Object = { multiplier: 2 };
    v3.forEach(function(item:int):void {
        sum3 += item * this.multiplier;
    }, myObj);
    results.push((sum3 == 120) ? 1 : 0);

    var count4:int = 0;
    var v4:Vector.<int> = new <int>[];
    v4.forEach(function(item:int):void {
        count4++;
    });
    results.push((count4 == 0) ? 1 : 0);

    var result5:String = '';
    var p1:Point = new Point();
    p1.x = 2; p1.y = 4;
    var p2:Point = new Point();
    p2.x = -1; p2.y = 6;
    var p3:Point = new Point();
    p3.x = 6; p3.y = 8;
    var v5:Vector.<Point> = new <Point>[p1, p2, p3];
    v5.forEach(function(item:Point):void {
        result5 += item.x + ',';
    });
    results.push((result5 == '2,-1,6,') ? 1 : 0);

    var sum6:int = 0;
    var v6:Vector.<Point> = new <Point>[p1, p2, p3];
    v6.forEach(function(item:Point, index:int):void {
        sum6 += item.x + index;
    });
    results.push((sum6 == 10) ? 1 : 0);

    var count7:int = 0;
    var v7:Vector.<Point> = new <Point>[];
    v7.forEach(function(item:Point):void {
        count7++;
    });
    results.push((count7 == 0) ? 1 : 0);

    var sum8:int = 0;
    var v8:Vector.<int> = new <int>[1, 2];
    v8.forEach(function(item:int):void {
        sum8 += item;
        if (item == 1) {
            v8.push(3);
            v8.push(4);
        }
    });
    results.push((sum8 == 3) ? 1 : 0);

    var visited9:String = '';
    var v9:Vector.<int> = new <int>[1, 2, 3, 4, 5];
    v9.forEach(function(item:int):void {
        visited9 += item + ',';
        if (item == 2) {
            v9.pop();
            v9.pop();
        }
    });
    results.push((visited9 == '1,2,3,') ? 1 : 0);

    var visited10:String = '';
    var v10 = new <int>[1, 2];
    var cb10 = function(i:int, idx:int, vec:*):void {
        visited10 += i + ',';
        v10 = new <int>[100];
    };
    v10.forEach(cb10);
    results.push((visited10 == '1,2,') ? 1 : 0);

    var visited11:String = '';
    var v11 = new <int>[1, 2];
    var cb11 = function(i:int, idx:int, vec:*):void {
        visited11 += i + ',';
        if (idx == 0) {
            v11 = new <int>[];
        }
    };
    v11.forEach(cb11);
    results.push((visited11 == '1,2,') ? 1 : 0);

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
