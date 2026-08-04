using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.optimizecase
{
	[TestClass]
	public sealed class Test013 : CodeTestBase
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
            
        }
    }
}
//O_NewInstance_Var  各路径测试
var a;

class T
{
	public function T()
	{
		a = this;
	}
}

class T2
{
	
}

(function ():void 
{
	
	var c = new T();
	var b = 1;
	a = function ():void 
	{
		b = new Object();
		c = null;
		
		var e = new T2();
		d = e;
	};
		
	trace(c,a,b,d);
	a();
	trace(c,a,b,d);
	var d = new Object();
	
	trace(c,a,b,d);
	
})();

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

				Assert.AreEqual("[object T] function Function() {} 1 undefined\r\nnull function Function() {} [object Object] [object T2]\r\nnull function Function() {} [object Object] [object Object]\r\n", print.GetOutput());

			}


		}


		[TestMethod]
		public void Test()
		{
			Run();
		}
	}

}
