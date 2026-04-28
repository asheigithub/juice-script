using juicescript.runtime;
using System.Collections.Generic;

namespace compilerTests.CompileTest.array.sortOn
{
    [TestClass]
    public sealed class Test001 : CodeTestBase
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

// 测试基本 sortOn (字符串字段)
var results = [];

// Test 1: 基本字符串排序
var arr1 = [
    {name: 'Charlie', age: 30},
    {name: 'Alice', age: 25},
    {name: 'Bob', age: 35}
];
arr1.sortOn('name');
results.push(arr1[0].name == 'Alice' && arr1[1].name == 'Bob' && arr1[2].name == 'Charlie' ? 1 : 0);

// Test 2: 数字排序 (NUMERIC)
var arr2 = [
    {name: 'A', age: 100},
    {name: 'B', age: 3},
    {name: 'C', age: 34},
    {name: 'D', age: 10}
];
// 字符串排序: 10, 100, 3, 34
arr2.sortOn('age');
results.push(arr2[0].age == 10 && arr2[1].age == 100 && arr2[2].age == 3 && arr2[3].age == 34 ? 1 : 0);

// Test 3: 数字排序 (NUMERIC)
var arr3 = [
    {name: 'A', age: 100},
    {name: 'B', age: 3},
    {name: 'C', age: 34},
    {name: 'D', age: 10}
];
arr3.sortOn('age', Array.NUMERIC);
results.push(arr3[0].age == 3 && arr3[1].age == 10 && arr3[2].age == 34 && arr3[3].age == 100 ? 1 : 0);

// Test 4: 降序排序
var arr4 = [
    {name: 'Charlie', age: 30},
    {name: 'Alice', age: 25},
    {name: 'Bob', age: 35}
];
arr4.sortOn('age', Array.DESCENDING | Array.NUMERIC);
results.push(arr4[0].age == 35 && arr4[1].age == 30 && arr4[2].age == 25 ? 1 : 0);

// Test 5: 不区分大小写
var arr5 = [
    {name: 'charlie', score: 30},
    {name: 'Alice', score: 25},
    {name: 'Bob', score: 35},
    {name: 'alice', score: 20}
];
arr5.sortOn('name', Array.CASEINSENSITIVE);
// alice 和 Alice 应该相邻
var alicePos = -1, AlicePos = -1;
for (var i = 0; i < 4; i++) {
    if (arr5[i].name == 'alice') alicePos = i;
    if (arr5[i].name == 'Alice') AlicePos = i;
}
results.push(Math.abs(alicePos - AlicePos) == 1 ? 1 : 0);

// Test 6: 多字段排序
var arr6 = [
    {dept: 'Sales', name: 'Charlie', age: 30},
    {dept: 'IT', name: 'Alice', age: 25},
    {dept: 'Sales', name: 'Bob', age: 35},
    {dept: 'IT', name: 'Bob', age: 28}
];
arr6.sortOn(['dept', 'name']);
results.push(arr6[0].dept == 'IT' && arr6[1].dept == 'IT' && arr6[2].dept == 'Sales' && arr6[3].dept == 'Sales' ? 1 : 0);

// Test 7: 边界情况 - 空数组
var arr7 = [];
arr7.sortOn('name');
results.push(arr7.length == 0 ? 1 : 0);

// Test 8: 边界情况 - 单个元素
var arr8 = [{name: 'Solo', age: 42}];
arr8.sortOn('name');
results.push(arr8.length == 1 && arr8[0].name == 'Solo' ? 1 : 0);

// Test 9: 相同字段值
var arr9 = [
    {name: 'Same', age: 30},
    {name: 'Same', age: 25},
    {name: 'Same', age: 35}
];
arr9.sortOn('age', Array.NUMERIC);
results.push(arr9[0].age == 25 && arr9[1].age == 30 && arr9[2].age == 35 ? 1 : 0);

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
            Assert.AreEqual(9, results.Length, "Expected 9 test results");

            for (int i = 0; i < 9; i++)
            {
                Assert.AreEqual("1", results[i], $"Test {i + 1} failed");
            }
        }

        [TestMethod]
        public void Test() => Run();
    }
}
