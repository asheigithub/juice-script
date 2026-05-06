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
	public class TestSturct003 : CodeTestBase
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
		public var v;
	}
	
}

[struct]
final class O
{
	public function O(v:int)
	{
		this.v = v;
	}
	
	public var v:int;
	
	public function V():int
	{
		return v;
	}
}

var o;
var p;


(function (i,j,k) 
{
	var a = i;
	var b = a.V;
	a.v = 666;
	
	
	o = b();
	p = j.V();
	
	
})( new O(1),new O(2) );


//trace(o,p);
"
				}


				);


			return project;
		}

		protected override void TestIsPass(Player player, PlayerException ex)
		{
			Assert.IsNull(ex);

			Assert.AreEqual(0, player.Context.GC.Heap.DumpHeap()
				.Where(o => o.Kind == RtHeapTypeKind.INSTANCE && o.Type.QName.Name == "O").Count());

			Assert.AreEqual(0, player.Context.GC.Heap.DumpHeap()
				.Where(o => o.Kind == RtHeapTypeKind.DYNAMIC_PROPERTYS && ((RtShape)player.Context.GC.Heap[((RtDynamic)o).SHAPE_PTR]).Attribute.HasFlag(RtShape.PropertyAttribute.Enumerable)).Count());

			player.ForceGC();

			Assert.AreEqual(0, player.Context.GC.Heap.DumpHeap()
				.Where(o => o.Kind == RtHeapTypeKind.INSTANCE && o.Type.QName.Name == "O").Count());

			Assert.AreEqual(0, player.Context.GC.Heap.DumpHeap()
				.Where(o => o.Kind == RtHeapTypeKind.DYNAMIC_PROPERTYS && ((RtShape)player.Context.GC.Heap[((RtDynamic)o).SHAPE_PTR]).Attribute.HasFlag(RtShape.PropertyAttribute.Enumerable)).Count());


			var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Main");
			Assert.IsNotNull(global);
			var globalInstance = player.Context.GC.Heap[global.__global_index__];
			Assert.IsNotNull(globalInstance);
			Assert.IsNull(ex);

			RtScriptClass rtPayload = (RtScriptClass)globalInstance;

			var o = rtPayload.ReadSlot(0);
			Assert.AreEqual(NaNBoxing.BoxType.Int, o.ValueType);
			Assert.AreEqual(1, o.IntValue);

			var p = rtPayload.ReadSlot(1);
			Assert.AreEqual(NaNBoxing.BoxType.Int, p.ValueType);
			Assert.AreEqual(2, p.IntValue);
		}


		[TestMethod]
		public void Test()
		{
			Run();
		}
	}
}
