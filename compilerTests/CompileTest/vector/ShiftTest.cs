using juicescript.runtime;
using System.Collections.Generic;
using System.Linq;

namespace compilerTests.CompileTest.vector
{
    [TestClass]
    public sealed class ShiftTest : CodeTestBase
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
    var shifted1:int = v1.shift();
    results.push((v1.length == 2 && shifted1 == 1 && v1[0] == 2 && v1[1] == 3) ? 1 : 0);

    var v2:Vector.<int> = new <int>[1, 2, 3, 4, 5];
    var shifted2:int = v2.shift();
    results.push((v2.length == 4 && v2[0] == 2 && v2[3] == 5 && shifted2 == 1) ? 1 : 0);

    var v3:Vector.<int> = new <int>[100];
    var shifted3:int = v3.shift();
    results.push((v3.length == 0 && shifted3 == 100) ? 1 : 0);

    var v4:Vector.<int> = new <int>[];
    var shifted4:* = v4.shift();
    results.push((v4.length == 0) ? 1 : 0);

    var v5:Vector.<int> = new <int>[10, 20, 30];
    var s5a:int = v5.shift();
    var s5b:int = v5.shift();
    var s5c:int = v5.shift();
    results.push((v5.length == 0 && s5a == 10 && s5b == 20 && s5c == 30) ? 1 : 0);

    var v6:Vector.<Array> = new <Array>[[1,2]];
    var shifted6:Array = v6.shift();
    results.push((v6.length == 0 && shifted6[0] == 1) ? 1 : 0);

    var inner:Vector.<int> = new <int>[1, 2];
    var v7:Vector.<Vector.<int>> = new <Vector.<int>>[inner];
    var shifted7:Vector.<int> = v7.shift();
    results.push((v7.length == 0 && shifted7[0] == 1) ? 1 : 0);

    var v8:Vector.<String> = new <String>['a', 'b', 'c'];
    var shifted8:String = v8.shift();
    results.push((v8.length == 2 && v8[0] == 'b' && v8[1] == 'c' && shifted8 == 'a') ? 1 : 0);

    var v9:Vector.<int> = new <int>[1, 2];
    v9.fixed = true;
    var caught9:Boolean = false;
    try {
        v9.shift();
    } catch (e:Error) {
        caught9 = true;
    }
    results.push((caught9 && v9.length == 2) ? 1 : 0);

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
            Assert.AreEqual("1", numbers[0], "Test1(basic shift) fail");
            Assert.AreEqual("1", numbers[1], "Test2(shift remaining) fail");
            Assert.AreEqual("1", numbers[2], "Test3(length 1) fail");
            Assert.AreEqual("1", numbers[3], "Test4(empty vector) fail");
            Assert.AreEqual("1", numbers[4], "Test5(multiple shift) fail");
            Assert.AreEqual("1", numbers[5], "Test6(shift Array) fail");
            Assert.AreEqual("1", numbers[6], "Test7(shift Vector) fail");
            Assert.AreEqual("1", numbers[7], "Test8(shift String) fail");
            Assert.AreEqual("1", numbers[8], "Test9(fixed vector) fail");
        }

        [TestMethod]
        public void Test() => Run();
    }

    [TestClass]
    public sealed class ShiftStructTest : CodeTestBase
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
    p1.x = 1; p1.y = 2;
    var p2:Point = new Point();
    p2.x = 3; p2.y = 4;
    var p3:Point = new Point();
    p3.x = 5; p3.y = 6;

    var v1:Vector.<Point> = new <Point>[p1, p2, p3];
    var shifted1:Point = v1.shift();
    results.push((v1.length == 2 && v1[0].x == 3 && v1[0].y == 4 && v1[1].x == 5 && v1[1].y == 6) ? 1 : 0);

    var v2:Vector.<Point> = new <Point>[p1, p2];
    var s2a:Point = v2.shift();
    var s2b:Point = v2.shift();
    results.push((v2.length == 0 && s2a.x == 1 && s2a.y == 2 && s2b.x == 3 && s2b.y == 4) ? 1 : 0);

    var separator:String = ',';
    trace(results[0] + separator + results[1]);
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
            Assert.AreEqual(2, numbers.Length, "Expected 2 test results");
            Assert.AreEqual("1", numbers[0], "Test1(struct shift) fail");
            Assert.AreEqual("1", numbers[1], "Test2(multiple struct shift) fail");
        }

        [TestMethod]
        public void Test() => Run();
    }

    [TestClass]
    public sealed class UnshiftTest : CodeTestBase
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

    var v1:Vector.<int> = new <int>[2, 3];
    var len1:uint = v1.unshift(1);
    results.push((len1 == 3 && v1.length == 3 && v1[0] == 1 && v1[1] == 2 && v1[2] == 3) ? 1 : 0);

    var v2:Vector.<int> = new <int>[4, 5];
    var len2:uint = v2.unshift(1, 2, 3);
    results.push((len2 == 5 && v2[0] == 1 && v2[1] == 2 && v2[2] == 3 && v2[3] == 4 && v2[4] == 5) ? 1 : 0);

    var v3:Vector.<int> = new <int>[];
    var len3:uint = v3.unshift(1);
    results.push((len3 == 1 && v3[0] == 1) ? 1 : 0);

    var v4:Vector.<int> = new <int>[1, 2];
    v4.fixed = true;
    var caught4:Boolean = false;
    try {
        v4.unshift(0);
    } catch (e:Error) {
        caught4 = true;
    }
    results.push((caught4 && v4.length == 2) ? 1 : 0);

    var v5:Vector.<String> = new <String>['b', 'c'];
    var len5:uint = v5.unshift('a');
    results.push((len5 == 3 && v5[0] == 'a' && v5[1] == 'b' && v5[2] == 'c') ? 1 : 0);

    var v6:Vector.<int> = new <int>[3];
    var len6:uint = v6.unshift(1, 2);
    results.push((len6 == 3 && v6[0] == 1 && v6[1] == 2 && v6[2] == 3) ? 1 : 0);

    var v7:Vector.<int> = new <int>[1];
    var len7:uint = v7.unshift();
    results.push((len7 == 1 && v7[0] == 1) ? 1 : 0);

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
            Assert.AreEqual("1", numbers[0], "Test1(basic unshift) fail");
            Assert.AreEqual("1", numbers[1], "Test2(unshift multiple) fail");
            Assert.AreEqual("1", numbers[2], "Test3(empty vector) fail");
            Assert.AreEqual("1", numbers[3], "Test4(fixed vector) fail");
            Assert.AreEqual("1", numbers[4], "Test5(unshift String) fail");
            Assert.AreEqual("1", numbers[5], "Test6(unshift to length 1) fail");
            Assert.AreEqual("1", numbers[6], "Test7(unshift no args) fail");
        }

        [TestMethod]
        public void Test() => Run();
    }

    [TestClass]
    public sealed class UnshiftStructTest : CodeTestBase
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
    p1.x = 1; p1.y = 2;
    var p2:Point = new Point();
    p2.x = 3; p2.y = 4;

    var v1:Vector.<Point> = new <Point>[p2];
    var len1:uint = v1.unshift(p1);
    results.push((len1 == 2 && v1[0].x == 1 && v1[0].y == 2 && v1[1].x == 3 && v1[1].y == 4) ? 1 : 0);

    var v2:Vector.<Point> = new <Point>[];
    var p3:Point = new Point();
    p3.x = 5; p3.y = 6;
    var len2:uint = v2.unshift(p3);
    results.push((len2 == 1 && v2[0].x == 5 && v2[0].y == 6) ? 1 : 0);

    var separator:String = ',';
    trace(results[0] + separator + results[1]);
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
            Assert.AreEqual(2, numbers.Length, "Expected 2 test results");
            Assert.AreEqual("1", numbers[0], "Test1(struct unshift) fail");
            Assert.AreEqual("1", numbers[1], "Test2(struct unshift to empty) fail");
        }

        [TestMethod]
        public void Test() => Run();
    }
}