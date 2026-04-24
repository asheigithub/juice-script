using juicescript.runtime;
using System.Collections.Generic;
using System.Linq;

namespace compilerTests.CompileTest.array
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

function runTest():void {
    var result:int = 1;

    // Test 1: Basic Delete
    var arr1:Array = [1, 2, 3, 4, 5];
    var del1:Array = arr1.splice(1, 2);
    if (!(arr1.length == 3 && arr1[0] == 1 && arr1[1] == 4 && arr1[2] == 5 &&
        del1.length == 2 && del1[0] == 2 && del1[1] == 3)) {
        result = 0;
    }

    // Test 2: Delete and Insert
    var arr2:Array = [1, 2, 3, 4, 5];
    var del2:Array = arr2.splice(1, 2, 10, 20);
    if (!(arr2.length == 5 && arr2[0] == 1 && arr2[1] == 10 && arr2[2] == 20 &&
        arr2[3] == 4 && arr2[4] == 5 &&
        del2.length == 2 && del2[0] == 2 && del2[1] == 3)) {
        result = 0;
    }

    // Test 3: Insert Only (deleteCount=0)
    var arr3:Array = [1, 2, 3];
    arr3.splice(1, 0, 10, 20);
    if (!(arr3.length == 5 && arr3[0] == 1 && arr3[1] == 10 && arr3[2] == 20 &&
        arr3[3] == 2 && arr3[4] == 3)) {
        result = 0;
    }

    // Test 4: Negative Index
    var arr4:Array = [1, 2, 3, 4, 5];
    arr4.splice(-2, 2);
    if (!(arr4.length == 3 && arr4[0] == 1 && arr4[1] == 2 && arr4[2] == 3)) {
        result = 0;
    }

    // Test 5: Delete All
    var arr5:Array = [1, 2, 3];
    var del5:Array = arr5.splice(0, 3);
    if (!(arr5.length == 0 && del5.length == 3)) {
        result = 0;
    }

    // Test 6: Delete Zero Elements
    var arr6:Array = [1, 2, 3];
    var del6:Array = arr6.splice(0, 0);
    if (!(arr6.length == 3 && del6.length == 0)) {
        result = 0;
    }

    // Test 7: Out of Range startIndex
    var arr7:Array = [1, 2, 3];
    arr7.splice(10, 2);
    if (!(arr7.length == 3)) {
        result = 0;
    }

    // Test 8: Mixed Types
    var arr8:Array = [1, 'two', true, null, undefined];
    var del8:Array = arr8.splice(1, 2, 'new1', 'new2');
    if (!(arr8.length == 5 && arr8[0] == 1 && arr8[1] == 'new1' && arr8[2] == 'new2' &&
        arr8[3] === null && arr8[4] === undefined &&
        del8.length == 2)) {
        result = 0;
    }

    // Test 9: Small Array Grow to Heap (trigger heap promotion)
    var arr9:Array = [1, 2, 3, 4, 5];
    arr9.splice(1, 0, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120, 130, 140, 150, 160, 170, 180);
    if (!(arr9.length == 23 && arr9[0] == 1 && arr9[1] == 10 && arr9[18] == 180 &&
        arr9[19] == 2 && arr9[20] == 3 && arr9[21] == 4 && arr9[22] == 5)) {
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
            var output = print.GetOutput().Trim();
            Assert.AreEqual("1", output);
        }

        [TestMethod]
        public void Test() => Run();
    }
}
