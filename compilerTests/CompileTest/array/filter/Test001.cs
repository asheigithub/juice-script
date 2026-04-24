using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.array.filter
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

var arr = [1, 2, 3, 4, 5];
var res = arr.filter(function(e, i, a) {
	return e > 2;
});
if (res.length !== 3) throw new Error('length wrong');
if (res[0] !== 3 || res[1] !== 4 || res[2] !== 5) throw new Error('elements wrong');

var empty = [];
var emptyRes = empty.filter(function(e) { return true; });
if (emptyRes.length !== 0) throw new Error('empty wrong');

var one = [1];
var oneRes = one.filter(function(e) { return true; });
if (oneRes.length !== 1 || oneRes[0] !== 1) throw new Error('one element wrong');

var indexArr = [10, 20, 30];
var indexRes = indexArr.filter(function(e, i, a) {
	return i > 0;
});
if (indexRes.length !== 2) throw new Error('index test failed');

var arrRef = [1, 2, 3];
var arrRefResult = arrRef.filter(function(e, i, a) {
	return a === arrRef;
});
if (arrRefResult.length !== 3) throw new Error('array ref test failed');

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