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
	public class TestSturct008 : CodeTestBase
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
  final class A{ public var i:int; };

[struct]
  final class B{ public var i:int; };


(function ():void 
{
	var b = new A();
	b.i = 100;
	
	function k(c):void 
	{
		
		var d = c;
		b = new B();
		c.i = 99;
		
		trace(b,b.i);
		trace(c, c.i);
		trace(d, d.i);
		//trace(arguments[0]);
		
	}
	
	
	k(b);
	trace(b, b.i);
	
	
})();



"
				}


				);


			return project;
		}

		protected override void TestIsPass(Player player, PlayerException ex)
		{
			Assert.IsNull(ex);

			Assert.AreEqual(0, player.Context.GC.Heap.DumpHeap()
				.Where(o => o.TypeKind == RtHeapTypeKind.INSTANCE && o.Type.QName.Name == "A").Count());
			Assert.AreEqual(0, player.Context.GC.Heap.DumpHeap()
				.Where(o => o.TypeKind == RtHeapTypeKind.INSTANCE && o.Type.QName.Name == "B").Count());

			player.ForceGC();

			Assert.AreEqual(0, player.Context.GC.Heap.DumpHeap()
				.Where(o => o.TypeKind == RtHeapTypeKind.INSTANCE && o.Type.QName.Name == "A").Count());
			Assert.AreEqual(0, player.Context.GC.Heap.DumpHeap()
				.Where(o => o.TypeKind == RtHeapTypeKind.INSTANCE && o.Type.QName.Name == "B").Count());

			var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Main");
			Assert.IsNotNull(global);
			var globalInstance = player.Context.GC.Heap[global.__global_index__];
			Assert.IsNotNull(globalInstance);
			Assert.IsNull(ex);

			Assert.AreEqual("[object B] 0\r\n[object A] 99\r\n[object A] 100\r\n[object B] 0\r\n", ((StringPrint)player.Print).GetOutput());

			
		}


		[TestMethod]
		public void Test()
		{
			Run();
		}
	}
}
