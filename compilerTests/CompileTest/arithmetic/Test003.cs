using juicescript.runtime;
using System.Collections.Generic;

namespace compilerTests.CompileTest.arithmetic
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
			var i:int = 10;
			var u:uint = 5;
			var s:short = 2;
			var sb:sbyte = 2;
			var b:byte = 3;
			
			trace(i + i, i - i, i * i, i / i, i % i);
			trace(u + u, u - u, u * u, u / u, u % u);
			trace(s + s, s - s, s * s, s / s, s % s);
			trace(sb + sb, sb - sb, sb * sb, sb / sb, sb % sb);
			trace(b + b, b - b, b * b, b / b, b % b);
			
			trace(i + u, i - u, i * u, i / u, i % u);
			trace(i + s, i - s, i * s, i / s, i % s);
			trace(i + sb, i - sb, i * sb, i / sb, i % sb);
			trace(i + b, i - b, i * b, i / b, i % b);
			
			trace(u + i, u - i, u * i, u / i, u % i);
			trace(u + s, u - s, u * s, u / s, u % s);
			trace(u + sb, u - sb, u * sb, u / sb, u % sb);
			trace(u + b, u - b, u * b, u / b, u % b);
			
			trace(s + i, s - i, s * i, s / i, s % i);
			trace(s + u, s - u, s * u, s / u, s % u);
			trace(s + sb, s - sb, s * sb, s / sb, s % sb);
			trace(s + b, s - b, s * b, s / b, s % b);
			
			trace(sb + i, sb - i, sb * i, sb / i, sb % i);
			trace(sb + u, sb - u, sb * u, sb / u, sb % u);
			trace(sb + s, sb - s, sb * s, sb / s, sb % s);
			trace(sb + b, sb - b, sb * b, sb / b, sb % b);
			
			trace(b + i, b - i, b * i, b / i, b % i);
			trace(b + u, b - u, b * u, b / u, b % u);
			trace(b + s, b - s, b * s, b / s, b % s);
			trace(b + sb, b - sb, b * sb, b / sb, b % sb);
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
			var output = print.GetOutput();
			var expected = "20 0 100 1 0\r\n10 0 25 1 0\r\n4 0 4 1 0\r\n4 0 4 1 0\r\n6 0 9 1 0\r\n15 5 50 2 0\r\n12 8 20 5 0\r\n12 8 20 5 0\r\n13 7 30 3.3333333333333335 1\r\n15 -5 50 0.5 5\r\n7 3 10 2.5 1\r\n7 3 10 2.5 1\r\n8 2 15 1.6666666666666667 2\r\n12 -8 20 0.2 2\r\n7 -3 10 0.4 2\r\n4 0 4 1 0\r\n5 -1 6 0.6666666666666666 2\r\n12 -8 20 0.2 2\r\n7 -3 10 0.4 2\r\n4 0 4 1 0\r\n5 -1 6 0.6666666666666666 2\r\n13 -7 30 0.3 3\r\n8 -2 15 0.6 3\r\n5 1 6 1.5 1\r\n5 1 6 1.5 1\r\n";
			if (output != expected)
			{
				Console.WriteLine("Expected length: " + expected.Length);
				Console.WriteLine("Actual length: " + output.Length);
				Console.WriteLine("Expected bytes: " + BitConverter.ToString(System.Text.Encoding.UTF8.GetBytes(expected)));
				Console.WriteLine("Actual bytes: " + BitConverter.ToString(System.Text.Encoding.UTF8.GetBytes(output)));
			}
			Assert.AreEqual(expected, output);
		}

		[TestMethod]
		public void Test()
		{
			Run();
		}
	}
}
