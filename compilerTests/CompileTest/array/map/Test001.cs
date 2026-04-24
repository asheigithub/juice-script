using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.array.map
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
var res = arr.map(function(e) { return e * 2; });
if (res.length !== 5) throw new Error('length wrong');
if (res[0] !== 2 || res[1] !== 4 || res[2] !== 6 || res[3] !== 8 || res[4] !== 10) throw new Error('elements wrong');

var empty = [];
var emptyRes = empty.map(function(e) { return e; });
if (emptyRes.length !== 0) throw new Error('empty wrong');

var one = [1];
var oneRes = one.map(function(e) { return e * 2; });
if (oneRes.length !== 1 || oneRes[0] !== 2) throw new Error('one element wrong');

var indexArr = [10, 20, 30];
var indexRes = indexArr.map(function(e, i) { return e + i; });
if (indexRes[0] !== 10 || indexRes[1] !== 21 || indexRes[2] !== 32) throw new Error('index test failed');

var arrRef = [1, 2, 3];
var arrRefResult = arrRef.map(function(e, i, a) { return a === arrRef ? 1 : 0; });
if (arrRefResult[0] !== 1 || arrRefResult[1] !== 1 || arrRefResult[2] !== 1) throw new Error('array ref test failed');

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