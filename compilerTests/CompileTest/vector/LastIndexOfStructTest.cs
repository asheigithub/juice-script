using juicescript.runtime;
using System.Collections.Generic;
using System.Linq;

namespace compilerTests.CompileTest.vector
{
    [TestClass]
    public sealed class LastIndexOfStructTest : CodeTestBase
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

    var p1:Point = new Point();
    p1.x = 10; p1.y = 20;
    var p2:Point = new Point();
    p2.x = 30; p2.y = 40;
    var p3:Point = new Point();
    p3.x = 50; p3.y = 60;
    
    var points:Vector.<Point> = new <Point>[p1, p2, p3, p2, p1];
    
    results.push((points.lastIndexOf(p1) == 4) ? 1 : 0);
    results.push((points.lastIndexOf(p2) == 3) ? 1 : 0);
    results.push((points.lastIndexOf(p3) == 2) ? 1 : 0);
    
    var p4:Point = new Point();
    p4.x = 99; p4.y = 99;
    results.push((points.lastIndexOf(p4) == -1) ? 1 : 0);
    
    results.push((points.lastIndexOf(p2, 2) == 1) ? 1 : 0);
    results.push((points.lastIndexOf(p2, 3) == 3) ? 1 : 0);
    
    var emptyPoints:Vector.<Point> = new <Point>[];
    results.push((emptyPoints.lastIndexOf(p1) == -1) ? 1 : 0);
    
    var separator:String = ',';
    trace(results[0] + separator + results[1] + separator + results[2] + separator + results[3] + separator + results[4] + separator + results[5] + separator + results[6]);
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
            Assert.AreEqual(7, numbers.Length, "Expected 7 test results");
            for (int i = 0; i < 7; i++)
            {
                Assert.AreEqual("1", numbers[i], $"Test{i + 1} fail");
            }
        }

        [TestMethod]
        public void Test() => Run();
    }
}
