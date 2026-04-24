using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.array.indexOf
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

var arr = [1, 2, 3, 4, 5];
var idx = arr.indexOf(3);
if (idx !== 2) throw new Error('indexOf(3) expected 2, got ' + idx);

idx = arr.indexOf(6);
if (idx !== -1) throw new Error('indexOf(6) expected -1, got ' + idx);

var empty = [];
idx = empty.indexOf(1);
if (idx !== -1) throw new Error('empty indexOf expected -1, got ' + idx);

var one = [42];
idx = one.indexOf(42);
if (idx !== 0) throw new Error('one element found expected 0, got ' + idx);

idx = one.indexOf(1);
if (idx !== -1) throw new Error('one element not found expected -1, got ' + idx);

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