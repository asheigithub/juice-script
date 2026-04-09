using juicescript.runtime;
using System.Collections.Generic;
using System.Linq;

namespace compilerTests.CompileTest.vector
{
    [TestClass]
    public sealed class PushTest : CodeTestBase
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
    var len1:uint = v1.push(4);
    results.push((v1.length == 4 && v1[3] == 4 && len1 == 4) ? 1 : 0);

    var v2:Vector.<int> = new <int>[1, 2];
    var len2:uint = v2.push(3, 4, 5);
    results.push((v2.length == 5 && v2[2] == 3 && v2[4] == 5 && len2 == 5) ? 1 : 0);

    var v3:Vector.<int> = new <int>[];
    var len3:uint = v3.push(1);
    results.push((v3.length == 1 && v3[0] == 1 && len3 == 1) ? 1 : 0);

    var v4:Vector.<int> = new <int>[1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16];
    var len4:uint = v4.push(17, 18, 19, 20);
    results.push((v4.length == 20 && v4[15] == 16 && v4[19] == 20 && len4 == 20) ? 1 : 0);

    var v5:Vector.<Number> = new <Number>[1.5];
    var len5:uint = v5.push(2.5);
    results.push((v5.length == 2 && v5[0] == 1.5 && v5[1] == 2.5 && len5 == 2) ? 1 : 0);

    var v6:Vector.<String> = new <String>['a'];
    var len6:uint = v6.push('b', 'c');
    results.push((v6.length == 3 && v6[0] == 'a' && v6[2] == 'c' && len6 == 3) ? 1 : 0);

    var v7:Vector.<int> = new <int>[1, 2, 3];
    var len7:uint = v7.push();
    results.push((v7.length == 3 && len7 == 3) ? 1 : 0);

    var v8:Vector.<String> = new <String>['a'];
    var len8:uint = v8.push('b');
    results.push((v8.length == 2 && len8 == 2) ? 1 : 0);

    var arr1:Array = [1, 2];
    var v9:Vector.<Array> = new <Array>[arr1];
    var len9:uint = v9.push([3, 4]);
    results.push((v9.length == 2 && v9[0][0] == 1 && v9[1][0] == 3 && len9 == 2) ? 1 : 0);

    var inner1:Vector.<int> = new <int>[1, 2];
    var v10:Vector.<Vector.<int>> = new <Vector.<int>>[inner1];
    var len10:uint = v10.push(new <int>[3, 4]);
    results.push((v10.length == 2 && v10[0][0] == 1 && v10[1][0] == 3 && len10 == 2) ? 1 : 0);

    var separator:String = ',';
    trace(results[0] + separator + results[1] + separator + results[2] + separator + results[3] + separator + results[4] + separator + results[5] + separator + results[6] + separator + results[7] + separator + results[8] + separator + results[9]);
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
            Assert.AreEqual(10, numbers.Length, "Expected 10 test results");
            Assert.AreEqual("1", numbers[0], "Test1(push single) fail");
            Assert.AreEqual("1", numbers[1], "Test2(push multiple) fail");
            Assert.AreEqual("1", numbers[2], "Test3(push empty) fail");
            Assert.AreEqual("1", numbers[3], "Test4(push >16) fail");
            Assert.AreEqual("1", numbers[4], "Test5(push Number) fail");
            Assert.AreEqual("1", numbers[5], "Test6(push String) fail");
            Assert.AreEqual("1", numbers[6], "Test7(push no args) fail");
            Assert.AreEqual("1", numbers[7], "Test8(push *) fail");
            Assert.AreEqual("1", numbers[8], "Test9(push Array) fail");
            Assert.AreEqual("1", numbers[9], "Test10(push Vector) fail");
        }

        [TestMethod]
        public void Test() => Run();
    }
}