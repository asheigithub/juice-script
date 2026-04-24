using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.array.indexOf
{
	[TestClass]
	public sealed class Test002 : CodeTestBase
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

var arr = [1, 2, 3];
var idx;

// fromIndex >= length returns -1
idx = arr.indexOf(1, 10);
if (idx !== -1) throw new Error('A');

idx = arr.indexOf(1, 3);
if (idx !== -1) throw new Error('B');

// fromIndex within bounds works
idx = arr.indexOf(1, 0);
if (idx !== 0) throw new Error('C');

idx = arr.indexOf(1, 1);
if (idx !== -1) throw new Error('D');

idx = arr.indexOf(2, 1);
if (idx !== 1) throw new Error('E: got ' + idx + ', expected 1');

idx = arr.indexOf(3, 2);
if (idx !== 2) throw new Error('F');

pass('fromIndex tests passed');
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

				Assert.AreEqual("fromIndex tests passed\r\n", print.GetOutput());
			}
		}

		[TestMethod]
		public void Test()
		{
			Run();
		}
	}
}