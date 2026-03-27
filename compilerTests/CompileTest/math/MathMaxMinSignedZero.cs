using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.math
{
	[TestClass]
	public sealed class MathMaxMinSignedZero : CodeTestBase
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
            var pos0:Number = 0;
            var neg0:Number = -0;
            // max: +0 > -0
            var r1 = Math.max(pos0, neg0);
            var r2 = Math.max(neg0, pos0);
            // min: -0 < +0
            var r3 = Math.min(pos0, neg0);
            var r4 = Math.min(neg0, pos0);
            // Check signed zero: 1/0 is Infinity, 1/-0 is -Infinity
            if (1/r1 != Infinity) throw new Error('Math.max(0, -0) should return +0, got ' + r1 + ', 1/r=' + 1/r1);
            if (1/r2 != Infinity) throw new Error('Math.max(-0, 0) should return +0, got ' + r2 + ', 1/r=' + 1/r2);
            if (1/r3 != -Infinity) throw new Error('Math.min(0, -0) should return -0, got ' + r3 + ', 1/r=' + 1/r3);
            if (1/r4 != -Infinity) throw new Error('Math.min(-0, 0) should return -0, got ' + r4 + ', 1/r=' + 1/r4);
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
