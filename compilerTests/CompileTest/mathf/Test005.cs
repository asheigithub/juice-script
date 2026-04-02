using juicescript.runtime;
using System.Collections.Generic;

namespace compilerTests.CompileTest.@mathf
{
	[TestClass]
	public sealed class Test005 : CodeTestBase
	{
		protected override TestCodeProject LoadProject()
		{
			TestCodeProject project = new TestCodeProject();
			project.libs = [Juice_GlobalSwc];
			project.testCodes = new List<TestCodeFile>();
			project.testCodes.Add(new TestCodeFile()
			{
				Path = "Main.as",
				Code = @"
package {
	import flash.display.Sprite;

	[Doc]
	public class Main extends Sprite {
		public function Main() {
			trace(Mathf.pow(2, 3));
			trace(Mathf.sqrt(16));
			trace(Mathf.exp(1));
			trace(Mathf.log(1));
			trace(Mathf.log10(100));
			trace(Mathf.logBase(8, 2));
			trace(Mathf.approximately(1.0, 1.0));
			trace(Mathf.approximately(1.0, 1.1));
			trace(Mathf.isPowerOfTwo(8));
			trace(Mathf.isPowerOfTwo(7));
			trace(Mathf.nextPowerOfTwo(5));
		}
	}
}

var main:Main = new Main();
"
			});
			return project;
		}

		protected override void TestIsPass(Player player, PlayerException ex)
		{
			Assert.IsNull(ex);
			var print = (StringPrint)player.Print;
			Assert.AreEqual("8\r\n4\r\n2.7182817\r\n0\r\n2\r\n3\r\ntrue\r\nfalse\r\ntrue\r\nfalse\r\n8\r\n", print.GetOutput());
		}

		[TestMethod]
		public void Test()
		{
			Run();
		}
	}
}
