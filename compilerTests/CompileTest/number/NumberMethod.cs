using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.number
{
	[TestClass]
	public sealed class NumberMethod : CodeTestBase
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
    [Doc]
    public class Main {
        public function Main() {
            var n:Number = 123.456;
            if (n.valueOf() != 123.456) throw new Error('valueOf');
            if (n.toString() != '123.456') throw new Error('toString');
            if (n.toFixed(2) != '123.46') throw new Error('toFixed');
            if (Number(456).toString() != '456') throw new Error('Number constructor toString');
            if (Number(789).valueOf() != 789) throw new Error('Number constructor valueOf');
            trace('OK');
        }
    }
}
var main = new Main();
"
			});
			return project;
		}

		protected override void TestIsPass(Player player, PlayerException ex)
		{
			player.ForceGC();
			var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Main");
			Assert.IsNotNull(global);
			Assert.IsNull(ex, "Error: " + (ex?.Message ?? ""));
			var print = (StringPrint)player.Print;
			Assert.AreEqual("OK\r\n", print.GetOutput());
		}

		[TestMethod]
		public void Test() => Run();
	}
}
