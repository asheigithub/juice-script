using juicescript.runtime;
using System.Collections.Generic;

namespace compilerTests.CompileTest.@mathf
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
			trace(Mathf.lerp(0, 10, 0.5));
			trace(Mathf.lerpUnclamped(0, 10, 1.5));
			trace(Mathf.inverseLerp(0, 10, 5));
			trace(Mathf.smoothStep(0, 10, 0.5));
			trace(Mathf.moveTowards(5, 10, 2));
			trace(Mathf.deltaAngle(0, 270));
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
			Assert.AreEqual("5\r\n15\r\n0.5\r\n5\r\n7\r\n-90\r\n", print.GetOutput());
		}

		[TestMethod]
		public void Test()
		{
			Run();
		}
	}
}
