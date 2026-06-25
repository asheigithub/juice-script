using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.optimizecase
{
	[TestClass]
	public sealed class Test006 : CodeTestBase
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

//测试意外更改父级变量
function K()
{
	var a ;
	
	function D()
	{
		a = [666];
	}
	
	function M(i)
	{
		a = i;
	}
	
	function B()
	{
		
		
		
		var c = [999];
		M(c);
		
		var b = a;
		
		c = [1, 2];
		
		trace(a,b);
		trace(c);
		D();
		
		trace(a,b);
		trace(c);
		
	}
	
	
	trace(a);
	
	B();
	
	trace(a);
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

				Assert.AreEqual("undefined\r\n999 999\r\n1,2\r\n666 999\r\n1,2\r\n666\r\n", print.GetOutput());

			}


		}


		[TestMethod]
		public void Test()
		{
			Run();
		}
	}

}
