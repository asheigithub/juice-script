using juicescript.runtime;
using System.Collections.Generic;

namespace compilerTests.CompileTest.array
{
    [TestClass]
    public sealed class SpliceSparseTest : CodeTestBase
    {
        private const string testCode = @"
package {
    import flash.display.Sprite;

    [Doc]
    public class SpliceSparse extends Sprite {
        public function SpliceSparse() {
        }
    }
}

function testSplice1():void {
    var arr:Array = [];
    arr[0] = 'zero';
    arr[5] = 'five';
    arr[10] = 'ten';
    arr[100] = 'one_hundred';
    
    var deleted:Array = arr.splice(1, 3);
    
    if (arr[0] == 'zero' && arr[2] == 'five' && arr[7] == 'ten' && arr[97] == 'one_hundred') {
        trace('1');
    } else {
        trace('0');
    }
}

function testSplice2():void {
    var arr:Array = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];
    arr[100] = 'sparse_100';
    arr[200] = 'sparse_200';
    
    var deleted:Array = arr.splice(3, 2);
    
    if (arr[3] == 5 && arr[4] == 6 && arr[98] == 'sparse_100' && arr[198] == 'sparse_200') {
        trace('1');
    } else {
        trace('0');
    }
}

function testSplice3():void {
    var arr:Array = [1, 2, 3];
    arr[100] = 'sparse';
    
    arr.splice(50, 10);
    
    if (arr[0] == 1 && arr[2] == 3 && arr[90] == 'sparse') {
        trace('1');
    } else {
        trace('0');
    }
}

testSplice1();
testSplice2();
testSplice3();
";

        protected override TestCodeProject LoadProject()
        {
            TestCodeProject project = new TestCodeProject();
            project.libs = [Juice_GlobalSwc];
            project.testCodes = new List<TestCodeFile>
            {
                new TestCodeFile
                {
                    Path = "SpliceSparse.as",
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
            var lines = output.Split('\n')
                .Select(l => l.Trim())
                .Where(l => l == "1" || l == "0")
                .ToArray();
            Assert.AreEqual(3, lines.Length, "Should have 3 test results");
            Assert.AreEqual("1", lines[0], "testSplice1 should pass");
            Assert.AreEqual("1", lines[1], "testSplice2 should pass");
            Assert.AreEqual("1", lines[2], "testSplice3 should pass");
        }

        [TestMethod]
        public void Test() => Run();
    }
}
