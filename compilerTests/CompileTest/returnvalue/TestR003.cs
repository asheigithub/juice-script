using juicescript;
using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.returnvalue
{
	[TestClass]
	public class TestR003 : CodeTestBase
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
		
		public function Main() 
		{
			
		}
		
	}

}

var o = new Object();
o.vvv = function () 
{
	var b = 6;
	
	return function () 
	{
		return this.vvv ;
	}
	
}

var j = o.vvv()();



"
				}


				);


			return project;
		}

		protected override void TestIsPass(Player player, PlayerException ex)
		{
			player.ForceGC();

			var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Main");
			Assert.IsNotNull(global);
			var globalInstance = player.Context.GC.Heap[global.__global_index__];
			Assert.IsNotNull(globalInstance);
			Assert.IsNull(ex);

			RtPayloadScriptClass rtPayload = (RtPayloadScriptClass)globalInstance.facility;

			NaNBoxing o = rtPayload.ReadSlot(0);
			Assert.AreEqual(NaNBoxing.BoxType.HeapPtr, o.ValueType);

			NaNBoxing j = rtPayload.ReadSlot(1);
			Assert.AreEqual(NaNBoxing.BoxType.Undefined, j.ValueType);
			
			

		}


		[TestMethod]
		public void Test()
		{
			Run();
		}
	}
}
