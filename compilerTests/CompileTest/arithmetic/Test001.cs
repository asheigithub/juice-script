using juicescript.runtime;
using System.Collections.Generic;

namespace compilerTests.CompileTest.arithmetic
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
			var i:int = 10;
			var u:uint = 5;
			var s:short = 2;
			var sb:sbyte = 2;
			var b:byte = 3;
			var f:float = 3.0;
			
			trace(i + f, i - f, i * f, i / f, i % f);
			trace(u + f, u - f, u * f, u / f, u % f);
			trace(s + f, s - f, s * f, s / f, s % f);
			trace(sb + f, sb - f, sb * f, sb / f, sb % f);
			trace(b + f, b - f, b * f, b / f, b % f);
			trace(f + i, f - i, f * i, f / i, f % i);
			trace(f + u, f - u, f * u, f / u, f % u);
			trace(f + s, f - s, f * s, f / s, f % s);
			trace(f + sb, f - sb, f * sb, f / sb, f % sb);
			trace(f + b, f - b, f * b, f / b, f % b);
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
			Assert.AreEqual("13 7 30 3.3333333 1\r\n8 2 15 1.6666666 2\r\n5 -1 6 0.6666667 2\r\n5 -1 6 0.6666667 2\r\n6 0 9 1 0\r\n13 -7 30 0.3 3\r\n8 -2 15 0.6 3\r\n5 1 6 1.5 1\r\n5 1 6 1.5 1\r\n6 0 9 1 0\r\n", print.GetOutput());
		}

		[TestMethod]
		public void Test()
		{
			Run();
		}
	}
}
