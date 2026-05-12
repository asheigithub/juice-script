using juicescript.runtime;
using System.Collections.Generic;
using System.Linq;

namespace compilerTests.CompileTest.array
{
    [TestClass]
    public sealed class SpliceCacheStruct : CodeTestBase
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



[struct]
final class A
{
	public var i:int;
	
	
}


[struct]
final class B
{
	public var j:uint;
}

function test(...rest)
{
	var a = [new A(), new B()];
	
	a.splice(0, 1, new B(),new A());
	
	trace(a);
	
}

test( new B(), new B() );

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
            Assert.AreEqual("[object B],[object A],[object B]", output);
        }

        [TestMethod]
        public void Test() => Run();
    }
}
