using juicescript.runtime;
using System.Collections.Generic;
using System.Linq;

namespace compilerTests.CompileTest.vector
{
    [TestClass]
    public sealed class IterateStructTest : CodeTestBase
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
    
    var points:Vector.<Point> = new <Point>[p1, p2, p3];
    
    // Test1: for loop
    var forResult:String = '';
    for (var i:int = 0; i < points.length; i++) {
        forResult += '(' + points[i].x + ',' + points[i].y + ')';
    }
    results.push((forResult == '(10,20)(30,40)(50,60)') ? 1 : 0);
    
    // Test2: for each (for each in)
    var eachResult:String = '';
    for each (var pt:Point in points) {
        eachResult += '(' + pt.x + ',' + pt.y + ')';
    }
    results.push((eachResult == '(10,20)(30,40)(50,60)') ? 1 : 0);
    
    // Test3: for-in (indices)
    var indices:Array = [];
    for (var idx in points) {
        indices.push(idx);
    }
    results.push((indices.length == 3 && indices[0] == 0 && indices[1] == 1 && indices[2] == 2) ? 1 : 0);
    
    // Test4: empty Vector iterate
    var emptyPoints:Vector.<Point> = new <Point>[];
    var emptyResult:String = '';
    for each (var emptyPt:Point in emptyPoints) {
        emptyResult += 'x';
    }
    results.push((emptyResult == '') ? 1 : 0);
    
    var separator:String = ',';
    trace(results[0] + separator + results[1] + separator + results[2] + separator + results[3]);
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
            Assert.AreEqual(4, numbers.Length, "Expected 4 test results");
            Assert.AreEqual("1", numbers[0], "Test1(for loop) fail");
            Assert.AreEqual("1", numbers[1], "Test2(for each) fail");
            Assert.AreEqual("1", numbers[2], "Test3(for in indices) fail");
            Assert.AreEqual("1", numbers[3], "Test4(empty Vector) fail");
        }

        [TestMethod]
        public void Test() => Run();
    }
}