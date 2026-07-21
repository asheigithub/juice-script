using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.optimizecase
{
	[TestClass]
	public sealed class Test010 : CodeTestBase
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
        public function Main() {
            var arr:Array;
            var idx:int;
            var passed:Boolean = true;

            // Default fromIndex (0x7fffffff) - search from last
            trace(""=== Default fromIndex test ==="");
            arr = [1, 2, 3];
            idx = arr.lastIndexOf(3);
            trace(""lastIndexOf(3) = "" + idx + "" (expected 2)"");
            if (idx != 2) passed = false;

            // Explicit int fromIndex
            trace("""");
            trace(""=== int fromIndex ==="");
            arr = [1, 2, 3, 2, 1];
            idx = arr.lastIndexOf(2, 2);
            trace(""lastIndexOf(2, 2) = "" + idx + "" (expected 1)"");
            if (idx != 1) passed = false;

            // Fractional fromIndex
            trace("""");
            trace(""=== Fractional fromIndex ==="");
            arr = [1, 2, 3, 2, 1];
            idx = arr.lastIndexOf(2, 1.49);
            trace(""lastIndexOf(2, 1.49) = "" + idx + "" (expected 1)"");
            if (idx != 1) passed = false;

            idx = arr.lastIndexOf(2, 0.51);
            trace(""lastIndexOf(2, 0.51) = "" + idx + "" (expected -1)"");
            if (idx != -1) passed = false;

            // boolean fromIndex
            trace("""");
            trace(""=== Boolean fromIndex ==="");
            arr = [1, 2, 3, 2, 1];
            idx = arr.lastIndexOf(2, true);
            trace(""lastIndexOf(2, true) = "" + idx + "" (expected 1)"");
            if (idx != 1) passed = false;

            idx = arr.lastIndexOf(2, false);
            trace(""lastIndexOf(2, false) = "" + idx + "" (expected -1)"");
            if (idx != -1) passed = false;

            // NaN search always returns -1
            trace("""");
            trace(""=== NaN search ==="");
            arr = [Number.NaN, 1, Number.NaN];
            idx = arr.lastIndexOf(Number.NaN);
            trace(""lastIndexOf(NaN) = "" + idx + "" (expected -1)"");
            if (idx != -1) passed = false;

            // +0/-0 equality
            trace("""");
            trace(""=== +0 vs -0 ==="");
            arr = [0];
            idx = arr.lastIndexOf(-0);
            trace(""lastIndexOf(-0) in [0] = "" + idx + "" (expected 0)"");
            if (idx != 0) passed = false;

            arr = [-0];
            idx = arr.lastIndexOf(0);
            trace(""lastIndexOf(0) in [-0] = "" + idx + "" (expected 0)"");
            if (idx != 0) passed = false;

            // Out of bounds fromIndex
            trace("""");
            trace(""=== Out of bounds fromIndex ==="");
            arr = [1, 2, 3];
            idx = arr.lastIndexOf(1, 100);
            trace(""lastIndexOf(1, 100) = "" + idx + "" (expected 0)"");
            if (idx != 0) passed = false;

            idx = arr.lastIndexOf(1, -100);
            trace(""lastIndexOf(1, -100) = "" + idx + "" (expected -1)"");
            if (idx != -1) passed = false;

            trace("""");
            if (passed) {
                trace(""ALL TESTS PASSED!"");
            } else {
                trace(""SOME TESTS FAILED!"");
            }
        }
    }
}

var main:Main = new Main();
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

				RtScriptClass rtPayload = (RtScriptClass)globalInstance;

				StringPrint print = (StringPrint)player.Print;

				Assert.AreEqual("=== Default fromIndex test ===\r\nlastIndexOf(3) = 2 (expected 2)\r\n\r\n=== int fromIndex ===\r\nlastIndexOf(2, 2) = 1 (expected 1)\r\n\r\n=== Fractional fromIndex ===\r\nlastIndexOf(2, 1.49) = 1 (expected 1)\r\nlastIndexOf(2, 0.51) = -1 (expected -1)\r\n\r\n=== Boolean fromIndex ===\r\nlastIndexOf(2, true) = 1 (expected 1)\r\nlastIndexOf(2, false) = -1 (expected -1)\r\n\r\n=== NaN search ===\r\nlastIndexOf(NaN) = -1 (expected -1)\r\n\r\n=== +0 vs -0 ===\r\nlastIndexOf(-0) in [0] = 0 (expected 0)\r\nlastIndexOf(0) in [-0] = 0 (expected 0)\r\n\r\n=== Out of bounds fromIndex ===\r\nlastIndexOf(1, 100) = 0 (expected 0)\r\nlastIndexOf(1, -100) = -1 (expected -1)\r\n\r\nALL TESTS PASSED!\r\n", print.GetOutput());

			}


		}


		[TestMethod]
		public void Test()
		{
			Run();
		}
	}

}
