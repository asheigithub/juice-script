using juicescript;
using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.Struct
{
	[TestClass]
	public class TestSturct006 : CodeTestBase
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
	import ns1.BaseM;
	
	[Doc]
	/**
	 * ...
	 * @author 
	 */
	public class Main extends Sprite
	{
		public var v;
		public function Main()
		{
			
		}
	}
	
}

[struct]
final class SSS
{
	public var a:int;
	public var b:Number;
}


var f =(function()
{
	var a = new SSS();	
	var b = a;
	
	return function ():void 
	{
		b.a = 66;	
		trace(a.a, a.b);	
		a = b;	
		
		b = new SSS();
		b = new SSS();
		b = null;
		
		trace(a.a, a.b);
	};
	
	
	
})();

f();

"
				}


				);


			return project;
		}

		protected override void TestIsPass(Player player, PlayerException ex)
		{
			Assert.IsNull(ex);

			//栈复用后，只剩一个了
			//Assert.AreEqual(2, player.Context.GC.Heap.DumpHeap()
			//	.Where(o => o.TypeKind == RtHeapTypeKind.INSTANCE && o.Type.QName.Name == "SSS").Count());

			player.ForceGC();

			Assert.AreEqual(1, player.Context.GC.Heap.DumpHeap()
				.Where(o => o.TypeKind == RtHeapTypeKind.INSTANCE && o.Type.QName.Name == "SSS").Count());

			var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Main");
			Assert.IsNotNull(global);
			var globalInstance = player.Context.GC.Heap[global.__global_index__];
			Assert.IsNotNull(globalInstance);
			Assert.IsNull(ex);

			Assert.AreEqual("0 NaN\r\n66 NaN\r\n", ((StringPrint)player.Print).GetOutput());

			
		}


		[TestMethod]
		public void Test()
		{
			Run();
		}
	}
}
