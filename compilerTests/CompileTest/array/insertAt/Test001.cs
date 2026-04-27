using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.array.insertAt
{
	[TestClass]
	public sealed class Test001 : CodeTestBase
	{
		protected override TestCodeProject LoadProject()
		{
			TestCodeProject project = new TestCodeProject();

			project.libs = [Juice_GlobalSwc];

			project.testCodes = new List<TestCodeFile>();

			project.testCodes.Add(
				new TestCodeFile()
				{
					Path = "Main.as",
					Code = @"
package
{
	import flash.display.Sprite;

	[Doc]
	public class Main extends Sprite
	{
		public var v;
	}

}

(function ()
{
	var arr = new Array('a', 'b', 'c');

	arr.insertAt(1, 'X');
	trace('After insertAt(1, X): ' + arr.join(','));

	var arr2 = new Array('a', 'b', 'c');
	arr2.insertAt(0, 'Start');
	trace('After insertAt(0, Start): ' + arr2.join(','));

	var arr3 = new Array('a', 'b', 'c');
	arr3.insertAt(5, 'End');
	trace('After insertAt(5, End): ' + arr3.join(','));

	var arr4 = new Array('a', 'b', 'c');
	arr4.insertAt(-1, 'Last');
	trace('After insertAt(-1, Last): ' + arr4.join(','));

	var arr5 = new Array();
	arr5.insertAt(0, 'First');
	trace('Empty insertAt(0): ' + arr5.join(','));

	var arr6 = new Array();
	arr6.insertAt(-1, 'Last');
	trace('Empty insertAt(-1): ' + arr6.join(','));

	var arr7 = new Array(1, 2, 3);
	arr7.insertAt(-2, 'X');
	trace('insertAt(-2): ' + arr7.join(','));

	var arr8 = new Array(1, 2, 3);
	arr8.insertAt(-10, 'Y');
	trace('insertAt(-10): ' + arr8.join(','));

})();

"
				}
				);

			return project;
		}

		protected override void TestIsPass(Player player, PlayerException ex)
		{
			player.ForceGC();
			{
				var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Main");
				Assert.IsNotNull(global);
				var globalInstance = player.Context.GC.Heap[global.__global_index__];
				Assert.IsNotNull(globalInstance);

				if (ex != null)
				{
					Console.WriteLine($"Exception: {ex.Message}");
					Console.WriteLine($"Stack trace: {ex.StackTrace}");
				}
				Assert.IsNull(ex);

				StringPrint print = (StringPrint)player.Print;
				string output = print.GetOutput();

				Console.WriteLine($"Actual output: '{output}'");

				string expectedOutput = "After insertAt(1, X): a,X,b,c\r\n" +
								"After insertAt(0, Start): Start,a,b,c\r\n" +
								"After insertAt(5, End): a,b,c,End\r\n" +
								"After insertAt(-1, Last): a,b,Last,c\r\n" +
								"Empty insertAt(0): First\r\n" +
								"Empty insertAt(-1): Last\r\n" +
								"insertAt(-2): 1,X,2,3\r\n" +
								"insertAt(-10): Y,1,2,3\r\n";

				Assert.AreEqual(expectedOutput, output);
			}
		}

		[TestMethod]
		public void Test()
		{
			Run();
		}
	}
}