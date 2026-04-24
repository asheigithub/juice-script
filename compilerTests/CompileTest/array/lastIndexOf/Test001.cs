using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.array.lastIndexOf
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
package {
	import flash.display.Sprite;
	
	[Doc]
	public class Main extends Sprite {
	}
}

function pass(msg) {
	trace(msg || 'OK');
}

var arr = [1, 2, 3, 2, 1];
var idx;

idx = arr.lastIndexOf(2);
if (idx !== 3) throw new Error('lastIndexOf(2) expected 3');

idx = arr.lastIndexOf(5);
if (idx !== -1) throw new Error('lastIndexOf(5) expected -1');

var empty = [];
idx = empty.lastIndexOf(1);
if (idx !== -1) throw new Error('empty lastIndexOf expected -1');

idx = arr.lastIndexOf(1, -1);
if (idx !== 4) throw new Error('lastIndexOf(1, -1) expected 4');

idx = arr.lastIndexOf(1, -2);
if (idx !== 0) throw new Error('lastIndexOf(1, -2) expected 0');

idx = arr.lastIndexOf(2, 2);
if (idx !== 1) throw new Error('lastIndexOf(2, 2) expected 1');

idx = arr.lastIndexOf(2, 1);
if (idx !== 1) throw new Error('lastIndexOf(2, 1) expected 1');

pass('tests passed');
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

				Assert.AreEqual("tests passed\r\n", print.GetOutput());
			}
		}

		[TestMethod]
		public void Test()
		{
			Run();
		}
	}
}