using juicescript.runtime;
using System.Collections.Generic;
using System.Linq;

namespace compilerTests.CompileTest.array
{
    [TestClass]
    public sealed class SpliceArgumentsTest : CodeTestBase
    {
        private const string testCode = @"
package {
    import flash.display.Sprite;

    [Doc]
    public class SpliceArguments extends Sprite {
        public function SpliceArguments() {
            testArgumentsSplice();
        }
    }
}

function testArgumentsSplice():void {
    var result:int = 1;

    // Test 1: arguments.splice insert (cache_on_stack)
    (function (a, b, c) {
        arguments.splice(1, 0, 'x', 'y');
        if (!(arguments.length == 5 && arguments[0] == 1 && arguments[1] == 'x' && 
            arguments[2] == 'y' && arguments[3] == 2 && arguments[4] == 3)) {
            result = 0;
            trace('FAIL Test1');
        }
    })(1, 2, 3);

    // Test 2: arguments.splice delete and insert
    (function (a, b, c, d, e) {
        var deleted = arguments.splice(1, 2, 'new1', 'new2');
        if (!(arguments.length == 5 && arguments[0] == 'a' && arguments[1] == 'new1' && 
            arguments[2] == 'new2' && arguments[3] == 'd' && arguments[4] == 'e' &&
            deleted.length == 2)) {
            result = 0;
            trace('FAIL Test2');
        }
    })('a', 'b', 'c', 'd', 'e');

    trace(result);
}
var main:SpliceArguments = new SpliceArguments();
";

        protected override TestCodeProject LoadProject()
        {
            TestCodeProject project = new TestCodeProject();
            project.libs = [Juice_GlobalSwc];
            project.testCodes = new List<TestCodeFile>
            {
                new TestCodeFile
                {
                    Path = "SpliceArguments.as",
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
