using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.math
{
	[TestClass]
	public sealed class MathMaxMinNaN : CodeTestBase
	{
		protected override TestCodeProject LoadProject()
		{
			TestCodeProject project = new TestCodeProject();
			project.libs = [Juice_GlobalSwc];
			project.testCodes = new List<TestCodeFile>();
			project.testCodes.Add(new TestCodeFile()
			{
				Path = "Main.as",
				Code = @"
package {
    import flash.display.Sprite;
    [Doc]
    public class Main extends Sprite {
        public function Main() {
            var n:Number = NaN;
            var r1 = Math.max(n);
            var r2 = Math.max(1, n, 2);
            var r3 = Math.min(n);
            var r4 = Math.min(1, n, 2);
            if (!isNaN(r1)) throw new Error('Math.max(NaN) should be NaN, got ' + r1);
            if (!isNaN(r2)) throw new Error('Math.max(1, NaN, 2) should be NaN, got ' + r2);
            if (!isNaN(r3)) throw new Error('Math.min(NaN) should be NaN, got ' + r3);
            if (!isNaN(r4)) throw new Error('Math.min(1, NaN, 2) should be NaN, got ' + r4);
            trace('OK');
        }
    }
}
var main:Main = new Main();
"
			});
			return project;
		}

		protected override void TestIsPass(Player player, PlayerException ex)
		{
			player.ForceGC();
			var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Main");
			Assert.IsNotNull(global);
			Assert.IsNull(ex);
			var print = (StringPrint)player.Print;
			Assert.AreEqual("OK\r\n", print.GetOutput());
		}

		[TestMethod]
		public void Test() => Run();
	}
}
