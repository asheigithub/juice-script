using juicescript.runtime;
using System.Collections.Generic;

namespace compilerTests.CompileTest.array.sortOn
{
    [TestClass]
    public sealed class Test002 : CodeTestBase
    {
        protected override TestCodeProject LoadProject()
        {
            TestCodeProject project = new TestCodeProject();
            project.libs = [Juice_GlobalSwc];
            project.testCodes = new List<TestCodeFile>();

            project.testCodes.Add(
                new TestCodeFile()
                {
                    Path = "Main.as",
                    Code = @"
package {
    import flash.display.Sprite;

    [Doc]
    public class Main extends Sprite {
        public function Main() {
        }
    }
}

[struct]
final class Point {
    public var x:int = 0;
    public var y:int = 0;
}

var results = [];

// Test 1: Array 包含 struct 元素，按 x 字段数字排序
var p1 = new Point();
p1.x = 100; p1.y = 30;
var p2 = new Point();
p2.x = 3; p2.y = 10;
var p3 = new Point();
p3.x = 34; p3.y = 20;

var arr1 = [p1, p2, p3];
arr1.sortOn('x', Array.NUMERIC);
results.push(arr1[0].x == 3 && arr1[1].x == 34 && arr1[2].x == 100 ? 1 : 0);

// Test 2: 按 y 字段数字排序
var arr2 = [p1, p2, p3]; // 重新创建
p1 = new Point(); p1.x = 100; p1.y = 30;
p2 = new Point(); p2.x = 3; p2.y = 10;
p3 = new Point(); p3.x = 34; p3.y = 20;
arr2 = [p1, p2, p3];

arr2.sortOn('y', Array.NUMERIC);
results.push(arr2[0].y == 10 && arr2[1].y == 20 && arr2[2].y == 30 ? 1 : 0);

// Test 3: struct 元素可以正常访问字段
var arr3 = [p1, p2, p3];
results.push(arr3[0].x == 100 && arr3[1].x == 3 && arr3[2].x == 34 ? 1 : 0);

// Test 4: 多个 struct 按不同字段排序
var p4 = new Point(); p4.x = 50; p4.y = 5;
var p5 = new Point(); p5.x = 20; p5.y = 80;
var arr4 = [p4, p5, p1, p2, p3];
arr4.sortOn('y', Array.NUMERIC);
results.push(arr4[0].y == 5 && arr4[1].y == 10 && arr4[2].y == 20 && arr4[3].y == 30 && arr4[4].y == 80 ? 1 : 0);

trace(results.join(','));
"
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
            Assert.IsNull(ex, ex?.Message);

            StringPrint print = (StringPrint)player.Print;
            var output = print.GetOutput().Trim();

            var results = output.Split(',');
            Assert.AreEqual(4, results.Length, "Expected 4 test results");

            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual("1", results[i], $"Test {i + 1} failed");
            }
        }

        [TestMethod]
        public void Test() => Run();
    }
}
