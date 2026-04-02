using juicescript.runtime;
using System.Collections.Generic;

namespace compilerTests.CompileTest.@mathf
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
			trace(Mathf.sin(Mathf.PI / 2));
			trace(Mathf.cos(0));
			trace(Mathf.tan(Mathf.PI / 4));
			trace(Mathf.asin(1));
			trace(Mathf.acos(1));
			trace(Mathf.atan(1));
			trace(Mathf.atan2(1, 1));
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
			Assert.AreEqual("1\r\n1\r\n1\r\n1.5707964\r\n0\r\n0.7853982\r\n0.7853982\r\n", print.GetOutput());
		}

		[TestMethod]
		public void Test()
		{
			Run();
		}
	}
}
