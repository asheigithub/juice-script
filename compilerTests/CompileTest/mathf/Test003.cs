using juicescript.runtime;
using System.Collections.Generic;

namespace compilerTests.CompileTest.@mathf
{
	[TestClass]
	public sealed class Test003 : CodeTestBase
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
			trace(Mathf.max(3, 5));
			trace(Mathf.min(3, 5));
			trace(Mathf.clamp(15, 0, 10));
			trace(Mathf.clamp(-5, 0, 10));
			trace(Mathf.clamp01(-0.5));
			trace(Mathf.clamp01(1.5));
			trace(Mathf.repeat(15, 10));
			trace(Mathf.pingPong(13, 10));
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
			Assert.AreEqual("5\r\n3\r\n10\r\n0\r\n0\r\n1\r\n5\r\n7\r\n", print.GetOutput());
		}

		[TestMethod]
		public void Test()
		{
			Run();
		}
	}
}
