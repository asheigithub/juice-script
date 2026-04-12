using juicescript.runtime;
using System.Collections.Generic;
using System.Linq;

namespace compilerTests.CompileTest.vector
{
    [TestClass]
    public sealed class RemoveAtTest : CodeTestBase
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
    var removed1:int = v1.removeAt(1);
    results.push((v1.length == 2 && v1[0] == 1 && v1[1] == 3 && removed1 == 2) ? 1 : 0);

    var v2:Vector.<int> = new <int>[1, 2, 3, 4, 5];
    var removed2:int = v2.removeAt(0);
    results.push((v2.length == 4 && v2[0] == 2 && v2[3] == 5 && removed2 == 1) ? 1 : 0);

    var v3:Vector.<int> = new <int>[10, 20, 30];
    var removed3:int = v3.removeAt(2);
    results.push((v3.length == 2 && v3[0] == 10 && v3[1] == 20 && removed3 == 30) ? 1 : 0);

    var v4:Vector.<int> = new <int>[1];
    var removed4:int = v4.removeAt(0);
    results.push((v4.length == 0 && removed4 == 1) ? 1 : 0);

    var v5:Vector.<int> = new <int>[1, 2, 3];
    var removed5:int = v5.removeAt(-1);
    results.push((v5.length == 2 && v5[0] == 1 && v5[1] == 2 && removed5 == 3) ? 1 : 0);

    var v6:Vector.<int> = new <int>[1, 2, 3];
    var removed6:int = v6.removeAt(-2);
    results.push((v6.length == 2 && v6[0] == 1 && v6[1] == 3 && removed6 == 2) ? 1 : 0);

    var v7:Vector.<String> = new <String>['a', 'b', 'c'];
    var removed7:String = v7.removeAt(1);
    results.push((v7.length == 2 && v7[0] == 'a' && v7[1] == 'c' && removed7 == 'b') ? 1 : 0);

    var v8:Vector.<String> = new <String>['a'];
    var removed8:String = v8.removeAt(0);
    results.push((v8.length == 0 && removed8 == 'a') ? 1 : 0);

    var inner1:Vector.<int> = new <int>[1, 2];
    var v9:Vector.<Vector.<int>> = new <Vector.<int>>[inner1, new <int>[3, 4]];
    var removed9:Vector.<int> = v9.removeAt(0);
    results.push((v9.length == 1 && v9[0][0] == 3 && removed9[0] == 1) ? 1 : 0);

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

    [TestClass]
    public sealed class RemoveAtStructTest : CodeTestBase
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

    var v1:Vector.<Point> = new <Point>[new Point(), new Point(), new Point()];
    v1[0].x = 1; v1[0].y = 2;
    v1[1].x = 3; v1[1].y = 4;
    v1[2].x = 5; v1[2].y = 6;
    var removed1:Point = v1.removeAt(1);
    results.push((v1.length == 2 && v1[0].x == 1 && v1[1].x == 5 && removed1.x == 3) ? 1 : 0);

    var v2:Vector.<Point> = new <Point>[new Point()];
    v2[0].x = 10; v2[0].y = 20;
    var removed2:Point = v2.removeAt(0);
    results.push((v2.length == 0 && removed2.x == 10 && removed2.y == 20) ? 1 : 0);

    var v3:Vector.<Point> = new <Point>[new Point(), new Point()];
    v3[0].x = 1; v3[0].y = 1;
    v3[1].x = 2; v3[1].y = 2;
    var removed3:Point = v3.removeAt(-1);
    results.push((v3.length == 1 && v3[0].x == 1 && removed3.x == 2) ? 1 : 0);

    var v4:Vector.<Point> = new <Point>[new Point(), new Point(), new Point()];
    v4[0].x = 1; v4[0].y = 1;
    v4[1].x = 2; v4[1].y = 2;
    v4[2].x = 3; v4[2].y = 3;
    var removed4:Point = v4.removeAt(0);
    results.push((v4.length == 2 && v4[0].x == 2 && v4[1].x == 3 && removed4.x == 1) ? 1 : 0);

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
            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual("1", numbers[i], $"Test{i + 1} fail");
            }
        }

        [TestMethod]
        public void Test() => Run();
    }

    [TestClass]
    public sealed class RemoveAtErrorTest : CodeTestBase
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
    var v:Vector.<int> = new <int>[1, 2, 3];
    v.fixed = true;
    v.removeAt(0);
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
            Assert.IsNotNull(ex);
            Assert.IsTrue(ex.ToDebugMessage().EndsWith("Cannot change the length of a fixed Vector."));
        }

        [TestMethod]
        public void Test() => Run();
    }
}