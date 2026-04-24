using juicescript.runtime;
using System.Collections.Generic;

namespace compilerTests.CompileTest.array
{
    [TestClass]
    public sealed class SpliceHoleTest : CodeTestBase
    {
        private const string testCode = @"
package {
    import flash.display.Sprite;

    [Doc]
    public class SpliceHole extends Sprite {
        public function SpliceHole() {
        }
    }
}

function testHole1():void {
    var arr:Array = [0, 1, 2, 3, 4, 5];
    delete arr[2];  // Create hole at index 2
    
    var deleted:Array = arr.splice(1, 3);
    
    // deleted should have length 3, with deleted[1] being a hole
    if (deleted.length == 3 && deleted[0] == 1 && deleted[2] == 3) {
        trace('1');
    } else {
        trace('0');
    }
}

function testHole2():void {
    var arr:Array = [];
    arr[0] = 'zero';
    arr[2] = 'two';  // Index 1 is a hole
    
    // Insert at index 1 - hole should move to index 2
    arr.splice(1, 0, 'one');
    
    if (arr.length == 4 && arr[0] == 'zero' && arr[1] == 'one' && arr[3] == 'two') {
        trace('1');
    } else {
        trace('0');
    }
}

function testHole3():void {
    var arr:Array = [];
    arr[0] = 'a';
    arr[10] = 'b';  // Sparse array with holes at indices 1-9
    
    var result:Array = arr.splice(0, 5);
    
    // result should have length 5, arr should have 'b' moved to index 5
    if (result.length == 5 && arr.length == 6 && arr[5] == 'b') {
        trace('1');
    } else {
        trace('0');
    }
}

testHole1();
testHole2();
testHole3();
";

        protected override TestCodeProject LoadProject()
        {
            TestCodeProject project = new TestCodeProject();
            project.libs = [Juice_GlobalSwc];
            project.testCodes = new List<TestCodeFile>
            {
                new TestCodeFile
                {
                    Path = "SpliceHole.as",
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
            Assert.AreEqual("1", lines[0], "testHole1 should pass");
            Assert.AreEqual("1", lines[1], "testHole2 should pass");
            Assert.AreEqual("1", lines[2], "testHole3 should pass");
        }

        [TestMethod]
        public void Test() => Run();
    }
}
