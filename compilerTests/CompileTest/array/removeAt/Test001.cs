using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.array.removeAt
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
	}

}

class Point
{
	public var x:float;
	public var y:float;
	
	public function Point(x:float = 0, y:float = 0)
	{
		this.x = x;
		this.y = y;
	}
}

(function ()
{
	var arr = new Array('a', 'b', 'c');
	var removed = arr.removeAt(1);
	trace('removeAt(1): removed=' + removed + ', remaining=' + arr.join(','));

	var arr2 = new Array('a', 'b', 'c');
	var removed2 = arr2.removeAt(0);
	trace('removeAt(0): removed=' + removed2 + ', remaining=' + arr2.join(','));

	var arr3 = new Array('a', 'b', 'c');
	var removed3 = arr3.removeAt(-1);
	trace('removeAt(-1): removed=' + removed3 + ', remaining=' + arr3.join(','));

	var arr4 = new Array('a', 'b', 'c');
	var removed4 = arr4.removeAt(10);
	trace('removeAt(10): removed=' + removed4 + ', remaining=' + arr4.join(','));

	var arr5 = new Array('a', 'b', 'c');
	var removed5 = arr5.removeAt(-10);
	trace('removeAt(-10): removed=' + removed5 + ', remaining=' + arr5.join(','));

	var arr6 = new Array();
	arr6.push(new Point(1, 2));
	arr6.push(new Point(3, 4));
	arr6.push(new Point(5, 6));
	var removed6 = arr6.removeAt(1);
	trace('removeAt(1) struct: removed=(' + removed6.x + ',' + removed6.y + '), len=' + arr6.length);

	var arr7 = new Array();
	arr7.push(new Point(10, 20));
	arr7.push(new Point(30, 40));
	var removed7 = arr7.removeAt(0);
	trace('removeAt(0) struct: removed=(' + removed7.x + ',' + removed7.y + '), len=' + arr7.length);

	var arr8 = new Array();
	arr8.push(new Point(100, 200));
	arr8.push(new Point(300, 400));
	var removed8 = arr8.removeAt(-1);
	trace('removeAt(-1) struct: removed=(' + removed8.x + ',' + removed8.y + '), len=' + arr8.length);
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

				string expectedOutput = "removeAt(1): removed=b, remaining=a,c\r\n" +
								"removeAt(0): removed=a, remaining=b,c\r\n" +
								"removeAt(-1): removed=c, remaining=a,b\r\n" +
								"removeAt(10): removed=undefined, remaining=a,b,c\r\n" +
								"removeAt(-10): removed=undefined, remaining=a,b,c\r\n" +
								"removeAt(1) struct: removed=(3,4), len=2\r\n" +
								"removeAt(0) struct: removed=(10,20), len=1\r\n" +
								"removeAt(-1) struct: removed=(300,400), len=1\r\n";

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