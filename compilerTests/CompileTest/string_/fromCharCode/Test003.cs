using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.string_.fromCharCode
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
        public function Main() {
            // S9.7_A2.1: ToUint16
            if (String.fromCharCode(0).charCodeAt(0) !== 0) throw new Error('FAIL #1');
            if (String.fromCharCode(1).charCodeAt(0) !== 1) throw new Error('FAIL #2');
            if (String.fromCharCode(-1).charCodeAt(0) !== 65535) throw new Error('FAIL #3: ' + String.fromCharCode(-1).charCodeAt(0));
            if (String.fromCharCode(65535).charCodeAt(0) !== 65535) throw new Error('FAIL #4');
            if (String.fromCharCode(65534).charCodeAt(0) !== 65534) throw new Error('FAIL #5');
            if (String.fromCharCode(65536).charCodeAt(0) !== 0) throw new Error('FAIL #6');
            if (String.fromCharCode(4294967295).charCodeAt(0) !== 65535) throw new Error('FAIL #7');
            if (String.fromCharCode(4294967294).charCodeAt(0) !== 65534) throw new Error('FAIL #8');
            if (String.fromCharCode(4294967296).charCodeAt(0) !== 0) throw new Error('FAIL #9');
            trace('OK');
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

				RtPayloadScriptClass rtPayload = (RtPayloadScriptClass)globalInstance.facility;

				StringPrint print = (StringPrint)player.Print;

				Assert.AreEqual("OK\r\n", print.GetOutput());
			}
		}

		[TestMethod]
		public void Test()
		{
			Run();
		}
	}
}