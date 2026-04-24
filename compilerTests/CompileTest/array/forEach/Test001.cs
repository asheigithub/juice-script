using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.array.forEach
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

function pass(msg) {
	trace(msg || 'OK');
}

var results = [];
var arr = [1, 2, 3];
arr.forEach(function(element, index, array) {
	results.push({e: element, i: index});
});
if (results.length !== 3) {
	throw new Error('expected 3 results');
}
if (results[0].e !== 1 || results[0].i !== 0) {
	throw new Error('first element wrong');
}
if (results[1].e !== 2 || results[1].i !== 1) {
	throw new Error('second element wrong');
}
if (results[2].e !== 3 || results[2].i !== 2) {
	throw new Error('third element wrong');
}

var empty:Array = [];
var emptyCalled = false;
empty.forEach(function(e, i, a) {
	emptyCalled = true;
});
if (emptyCalled !== false) {
	throw new Error('empty array should not call callback');
}

var sum = 0;
[1, 2, 3, 4, 5].forEach(function(e) {
	sum += e;
});
if (sum !== 15) {
	throw new Error('sum should be 15');
}

pass('basic tests passed');
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
				Assert.IsNull(ex);

				StringPrint print = (StringPrint)player.Print;

				Assert.AreEqual("basic tests passed\r\n", print.GetOutput());
			}
		}

		[TestMethod]
		public void Test()
		{
			Run();
		}
	}
}