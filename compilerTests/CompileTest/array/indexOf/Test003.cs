using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.array.indexOf
{
	[TestClass]
	public sealed class Test003 : CodeTestBase
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

// NaN search always returns -1 (strict equality)
var nanArr = [Number.NaN];
var idx = nanArr.indexOf(Number.NaN);
if (idx !== -1) throw new Error('NaN test failed: got ' + idx);

// +0 and -0 are equal
var posZero = [0];
idx = posZero.indexOf(-0);
if (idx !== 0) throw new Error('+0/-0 test failed: got ' + idx);

var negZero = [-0];
idx = negZero.indexOf(0);
if (idx !== 0) throw new Error('-0/+0 test failed: got ' + idx);

pass('NaN and +/-0 tests passed');
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

				Assert.AreEqual("NaN and +/-0 tests passed\r\n", print.GetOutput());
			}
		}

		[TestMethod]
		public void Test()
		{
			Run();
		}
	}
}