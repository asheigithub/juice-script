using juicescript;
using juicescript.ABC;
using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.returnvalue
{
	[TestClass]
	public class TestR005 : CodeTestBase
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
		return 6,o.vvv ;
	}	
}

var j = o.vvv()()()();

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

			RtScriptClass rtPayload = (RtScriptClass)globalInstance;

			NaNBoxing o = rtPayload.ReadSlot(0);
			Assert.AreEqual(NaNBoxing.BoxType.HeapPtr, o.ValueType);

			NaNBoxing j = rtPayload.ReadSlot(1);
			Assert.AreEqual(NaNBoxing.BoxType.HeapPtr, j.ValueType);

			var jobj = player.Context.GC.Heap[j.HeapPtr];
			Assert.AreEqual(RtHeapTypeKind.CLOSURE, jobj.Kind);

			var oobj = player.Context.GC.Heap[o.HeapPtr];
			Assert.AreEqual(RtHeapTypeKind.INSTANCE, oobj.Kind);

			int prop_ptr = ((RtInstance)oobj).PROPERTY_PTR(player,(ASInstance)oobj.Type);
			var prop = player.Context.GC.Heap[prop_ptr];
			Assert.AreEqual(RtHeapTypeKind.DYNAMIC_PROPERTYS, prop.Kind);
			Assert.AreEqual( j, ((RtDynamic)prop).Slots[0]);

		}


		[TestMethod]
		public void Test()
		{
			Run();
		}
	}
}
