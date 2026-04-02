using juicescript.runtime;
using System.Collections.Generic;

namespace compilerTests.CompileTest.@sbyte
{
	[TestClass]
	public sealed class Test001 : CodeTestBase
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
			trace(n.toString());
			trace(n.toString(16));
			trace(n.toString(2));
			trace(sbyte.MAX_VALUE.toString());
			trace(sbyte.MIN_VALUE.toString());
			trace((-1).toString());
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
			Assert.AreEqual("127\r\n7f\r\n1111111\r\n127\r\n-128\r\n-1\r\n", print.GetOutput());
		}

		[TestMethod]
		public void Test()
		{
			Run();
		}
	}
}
