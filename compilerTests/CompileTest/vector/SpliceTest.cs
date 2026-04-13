using juicescript.runtime;
using System.Collections.Generic;
using System.Linq;

namespace compilerTests.CompileTest.vector
{
    [TestClass]
    public sealed class SpliceTest : CodeTestBase
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
    
    public function Point(x:int = 0) {
        this.x = x;
        this.y = x * 10;
    }
}

function runTest():void {
    var result:int = 1;

    var v1:Vector.<int> = new <int>[1, 2, 3, 4, 5];
    var r1:Vector.<int> = v1.splice(1, 2);
    if (!(v1.length == 3 && v1[0] == 1 && v1[1] == 4 && v1[2] == 5 && r1.length == 2 && r1[0] == 2 && r1[1] == 3)) {
        result = 0;
    }

    var v2:Vector.<int> = new <int>[1, 2, 3];
    v2.splice(1, 0, 10, 20);
    if (!(v2.length == 5 && v2[0] == 1 && v2[1] == 10 && v2[2] == 20 && v2[3] == 2 && v2[4] == 3)) {
        result = 0;
    }

    var v3:Vector.<int> = new <int>[1, 2, 3, 4, 5];
    var r3:Vector.<int> = v3.splice(1, 2, 10, 20);
    if (!(v3.length == 5 && v3[0] == 1 && v3[1] == 10 && v3[2] == 20 && v3[3] == 4 && v3[4] == 5 && r3.length == 2 && r3[0] == 2 && r3[1] == 3)) {
        result = 0;
    }

    var v4:Vector.<int> = new <int>[1, 2, 3, 4, 5];
    v4.splice(-2, 2);
    if (!(v4.length == 3 && v4[0] == 1 && v4[1] == 2 && v4[2] == 3)) {
        result = 0;
    }

    var v5:Vector.<int> = new <int>[1, 2, 3];
    v5.splice(1, 0, 10);
    if (!(v5.length == 4 && v5[0] == 1 && v5[1] == 10 && v5[2] == 2 && v5[3] == 3)) {
        result = 0;
    }

    var v6:Vector.<int> = new <int>[1, 2, 3];
    var r6:Vector.<int> = v6.splice(0, 0);
    if (!(v6.length == 3 && r6.length == 0)) {
        result = 0;
    }

    var v7:Vector.<int> = new <int>[1, 2, 3];
    var r7:Vector.<int> = v7.splice(0, 3);
    if (!(v7.length == 0 && r7.length == 3 && r7[0] == 1 && r7[1] == 2 && r7[2] == 3)) {
        result = 0;
    }

    var v8:Vector.<Object> = new <Object>[{a:1}, {a:2}, {a:3}, {a:4}, {a:5}];
    var r8:Vector.<Object> = v8.splice(1, 2, {a:10}, {a:20});
    if (!(v8.length == 5 && v8[0].a == 1 && v8[1].a == 10 && v8[2].a == 20 && v8[3].a == 4 && v8[4].a == 5 && r8.length == 2 && r8[0].a == 2 && r8[1].a == 3)) {
        result = 0;
    }

    var v9:Vector.<Array> = new <Array>[[0], [1], [2], [3], [4]];
    var r9:Vector.<Array> = v9.splice(1, 2);
    if (!(v9.length == 3 && v9[0][0] == 0 && v9[1][0] == 3 && v9[2][0] == 4 && r9.length == 2 && r9[0][0] == 1 && r9[1][0] == 2)) {
        result = 0;
    }

    var v10:Vector.<Point> = new <Point>[new Point(0), new Point(1), new Point(2), new Point(3), new Point(4)];
    var r10:Vector.<Point> = v10.splice(1, 2);
    if (!(v10.length == 3 && v10[0].x == 0 && v10[1].x == 3 && v10[2].x == 4 && r10.length == 2 && r10[0].x == 1 && r10[1].x == 2)) {
        result = 0;
    }

    trace(result);
}
runTest();
";

        protected override TestCodeProject LoadProject()
        {
            TestCodeProject project = new TestCodeProject();
            project.libs = [Juice_GlobalSwc];
            project.testCodes = new List<TestCodeFile>
            {
                new TestCodeFile
                {
                    Path = "Main.as",
                    Code = testCode
                }
            };
            return project;
        }

        protected override void TestIsPass(Player player, PlayerException ex)
        {
            Assert.IsNull(ex);

            StringPrint print = (StringPrint)player.Print;
            var output = print.GetOutput();
            Assert.AreEqual("1", output.Trim());
        }

        [TestMethod]
        public void Test() => Run();
    }
}
