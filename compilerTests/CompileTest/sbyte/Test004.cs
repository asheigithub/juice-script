using juicescript.runtime;
using System.Collections.Generic;

namespace compilerTests.CompileTest.@sbyte
{
	[TestClass]
	public sealed class Test004 : CodeTestBase
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
			var n:sbyte = 127;
			trace(n.toFixed(3));
			trace(n.toFixed(0));
			trace(n.toFixed(5));
			trace(sbyte.MIN_VALUE.toFixed(2));
			trace(sbyte.MAX_VALUE.toFixed(2));
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
			Assert.AreEqual("127.000\r\n127\r\n127.00000\r\n-128.00\r\n127.00\r\n", print.GetOutput());
		}

		[TestMethod]
		public void Test()
		{
			Run();
		}
	}
}
