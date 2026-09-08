using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.other
{
	[TestClass]
	public sealed class Test031 : CodeTestBase
	{
		protected override TestCodeProject LoadProject()
		{
			TestCodeProject project = new TestCodeProject();

			project.libs = [Juice_GlobalSwc];

			project.testCodes = new List<TestCodeFile>();

			project.testCodes.Add(
				new TestCodeFile()
				{
					Path = "BaseM.as",
					Code = @"
package ns1 
{
	import flash.display.Sprite;
	/**
	 * ...
	 * @author 
	 */
	public class BaseM extends Sprite
	{
		
		public static const FFF = 6666;
		protected static const VVV = ""abcd"";
		public function BaseM() 
		{
			
		}
		
	}

}


"
				}
				);

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

//SSA 要考虑storeH的影响。      
(function ():void 
{
	var a;
	
	
	(function B():void 
	{
		var b;
		
		function C():* 
		{
			
			var j;
			if (1 + 1)
			{
				j = new <int> [1, 2];
			
			}
			else
			{
				j  = new <int> [4];
			}
			
			
			
			try 
			{
				
				return j;	
			}
			finally
			{
				if(1)
				{
					b = j;
					
					j.push(6);
					
					trace(b);
					
					trace(j[0]);
					
				}			
				else
				{
					b = j;
					j.push(3);
				}
			}
			
		};
		
		trace(C() , b);
		
		
		//(function d():void 
		//{
			//var c = a;
		//})();
		//
		//a = null;
		//trace(b);
		
	})();
	
		
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

				Assert.AreEqual("1,2,6\r\n1\r\n1,2,6 1,2,6\r\n", print.GetOutput());

			}


		}


		[TestMethod]
		public void Test()
		{
			Run();
		}
	}

}
