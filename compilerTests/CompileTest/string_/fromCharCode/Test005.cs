using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.string_.fromCharCode
{
	[TestClass]
	public sealed class Test005 : CodeTestBase
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
            // S9.7_A1: NaN, 0, -0, Infinity 转 +0
            var nanChar:int = String.fromCharCode(Number.NaN).charCodeAt(0);
            var zeroChar:int = String.fromCharCode(0).charCodeAt(0);
            var negZeroChar:int = String.fromCharCode(-0).charCodeAt(0);
            var posInfChar:int = String.fromCharCode(Number.POSITIVE_INFINITY).charCodeAt(0);
            var negInfChar:int = String.fromCharCode(Number.NEGATIVE_INFINITY).charCodeAt(0);
            
            if (nanChar !== 0) throw new Error('FAIL #1: nanChar=' + nanChar);
            if (zeroChar !== 0) throw new Error('FAIL #2: zeroChar=' + zeroChar);
            if (negZeroChar !== 0) throw new Error('FAIL #3: negZeroChar=' + negZeroChar);
            if (posInfChar !== 0) throw new Error('FAIL #4: posInfChar=' + posInfChar);
            if (negInfChar !== 0) throw new Error('FAIL #5: negInfChar=' + negInfChar);
            
            // 检查 +0 (不是 -0)
            if ((1 / nanChar) !== Number.POSITIVE_INFINITY) throw new Error('FAIL #6: 1/nanChar=' + (1 / nanChar));
            if ((1 / zeroChar) !== Number.POSITIVE_INFINITY) throw new Error('FAIL #7: 1/zeroChar=' + (1 / zeroChar));
            if ((1 / negZeroChar) !== Number.POSITIVE_INFINITY) throw new Error('FAIL #8: 1/negZeroChar=' + (1 / negZeroChar));
            if ((1 / posInfChar) !== Number.POSITIVE_INFINITY) throw new Error('FAIL #9: 1/posInfChar=' + (1 / posInfChar));
            if ((1 / negInfChar) !== Number.POSITIVE_INFINITY) throw new Error('FAIL #10: 1/negInfChar=' + (1 / negInfChar));
            
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

				RtScriptClass rtPayload = (RtScriptClass)globalInstance;

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