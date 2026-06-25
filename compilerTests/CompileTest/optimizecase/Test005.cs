using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.optimizecase
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
package 
{
	import flash.display.Sprite;
	
	[Doc]
	/**
	 * ...
	 * @author 
	 */
	public class Main extends Sprite
	{
		
	}
	
}

//测试e
function K():void 
{
	
	try 
	{
		var a; 
		a = [1, 2];
		
		var b = a;
		
		if (a)
		{
			throw b;
		}
		
		trace(b);
		
		
	}
	catch (e)
	{
		trace(b);
		
		e = 0;
		trace(e,b);
		
		//e = 0;
		
		
	}
	finally 
	{
		trace(b);
	}
	
	
}	

K();
	

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

				Assert.AreEqual("1,2\r\n0 1,2\r\n1,2\r\n", print.GetOutput());

			}


		}


		[TestMethod]
		public void Test()
		{
			Run();
		}
	}

}
