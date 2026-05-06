using juicescript;
using juicescript.compiler;
using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.ctor
{
	[TestClass]
	public class TestCtor011 : CodeTestBase
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

package
{
	import flash.display.Sprite;
	[Doc]
	public class Main extends Sprite
	{
		
	}
}

(function ():void 
{
	
	var x = function (i):void 
	{
		trace(""x="",x);
	}

	var o = new x(( function () 
	{
		return 	x = 1;
		
	})() )

	trace(x,o,o.constructor );

	o.constructor();
})();



trace(""OK"");




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

				Assert.AreEqual("x= 1\r\n1 [object Object] function Function() {}\r\nx= 1\r\nOK\r\n", print.GetOutput());

			}
		}


		[TestMethod]
		public void Test()
		{
			Run();
		}


	}
}
