using juicescript.runtime;
using System.Collections.Generic;

namespace compilerTests.CompileTest.arithmetic
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
			var nan:float = 0.0 / 0.0;
			var inf:float = 1.0 / 0.0;
			var negInf:float = -1.0 / 0.0;
			
			trace(nan + 5, nan - 5, nan * 5, nan / 5);
			trace(5 + nan, 5 - nan, 5 * nan, 5 / nan);
			trace(inf + 5, inf - 5, inf * 5, inf / 5);
			trace(inf + inf, inf - inf, inf * inf, inf / inf);
			trace(inf + negInf);
			trace(negInf * 2, negInf / 2);
			trace(5.0 / 0.0, -5.0 / 0.0);
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
			Assert.AreEqual("NaN NaN NaN NaN\r\nNaN NaN NaN NaN\r\nInfinity Infinity Infinity Infinity\r\nInfinity NaN Infinity NaN\r\nNaN\r\n-Infinity -Infinity\r\nInfinity -Infinity\r\n", print.GetOutput());
		}

		[TestMethod]
		public void Test()
		{
			Run();
		}
	}
}
