using juicescript.runtime;
using System.Collections.Generic;

namespace compilerTests.CompileTest.@mathf
{
	[TestClass]
	public sealed class Test002 : CodeTestBase
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
			trace(Mathf.ceil(3.7));
			trace(Mathf.floor(3.7));
			trace(Mathf.round(3.7));
			trace(Mathf.ceilToInt(3.7));
			trace(Mathf.floorToInt(3.7));
			trace(Mathf.roundToInt(3.7));
			trace(Mathf.abs(-5));
			trace(Mathf.sign(5));
			trace(Mathf.sign(-5));
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
			Assert.AreEqual("4\r\n3\r\n4\r\n4\r\n3\r\n4\r\n5\r\n1\r\n-1\r\n", print.GetOutput());
		}

		[TestMethod]
		public void Test()
		{
			Run();
		}
	}
}
