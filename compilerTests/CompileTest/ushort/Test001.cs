using juicescript.runtime;
using System.Collections.Generic;

namespace compilerTests.CompileTest.@ushort
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
			var n:ushort = 12345;
			trace(n.toString());
			trace(n.toString(16));
			trace(n.toString(2));
			trace(ushort.MAX_VALUE.toString());
			trace(ushort.MIN_VALUE.toString());
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
			Assert.AreEqual("12345\r\n3039\r\n11000000111001\r\n65535\r\n0\r\n", print.GetOutput());
		}

		[TestMethod]
		public void Test()
		{
			Run();
		}
	}
}
