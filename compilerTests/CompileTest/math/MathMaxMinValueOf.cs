using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.math
{
	[TestClass]
	public sealed class MathMaxMinValueOf : CodeTestBase
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
            var obj = {
                valueOf: function():Number {
                    return 3;
                }
            };
            var r1 = Math.max(1, 2, obj);
            var r2 = Math.min(1, 2, obj);
            if (r1 != 3) throw new Error('Math.max(1, 2, obj) should be 3, got ' + r1);
            if (r2 != 1) throw new Error('Math.min(1, 2, obj) should be 1, got ' + r2);
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
