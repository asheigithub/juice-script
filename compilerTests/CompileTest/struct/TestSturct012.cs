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
	public class TestSturct012 : CodeTestBase
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
final class O
{
	public var i:int;
	
	public var vec:V;
	
	public var b:Boolean;
	
	public function toString() 
	{
		return ""{ i="" + i + "", vec="" + vec + "", b="" + b+ ""}"";
	}
	
}

[struct]
final class V
{
	public var x:short = 1;
	public var y:W;
	public var z:float = 3;
	
	public function toString()
	{
		return ""{ x="" + x + "",y="" +y +"",z="" +z+""}"";
	}
	
}

[struct]
final class W
{
	public var w:Boolean = true;	
	
	public var n:short = 3;
	public var p:short = 5;
	
	public function toString()
	{
		return ""{ w="" + w + "",n="" + n + "",p="" + p + ""}""  ;
	}
	
}

(
function ():void 
{
	var o = new O();
	
	function m(v)
	{
		v.i = 5;
		v.vec.x = 9;
		v.vec.y.n = 10;
	}
	
	m(o);
	
	trace(o);
	
	function m2(v)
	{
		v.x = 8;
		v.y.w = false;
		
		return v;
	}
	
	var r= m2(o.vec);
	r.x = 2;
	trace(o, r);
}
)();




"
				}


				);


			return project;
		}

		protected override void TestIsPass(Player player, PlayerException ex)
		{
			Assert.IsNull(ex);

			Assert.AreEqual(0, player.Context.GC.Heap.DumpHeap()
				.Where(o => o.TypeKind == RtHeapTypeKind.INSTANCE && o.Type.QName.Name == "O").Count());
			Assert.AreEqual(0, player.Context.GC.Heap.DumpHeap()
				.Where(o => o.TypeKind == RtHeapTypeKind.INSTANCE && o.Type.QName.Name == "V").Count());
			Assert.AreEqual(0, player.Context.GC.Heap.DumpHeap()
				.Where(o => o.TypeKind == RtHeapTypeKind.INSTANCE && o.Type.QName.Name == "W").Count());

			player.ForceGC();

			Assert.AreEqual("{ i=5, vec={ x=9,y={ w=true,n=10,p=5},z=3}, b=false}\r\n{ i=5, vec={ x=9,y={ w=true,n=10,p=5},z=3}, b=false} { x=2,y={ w=false,n=10,p=5},z=3}\r\n", ((StringPrint)player.Print).GetOutput());

		}


		[TestMethod]
		public void Test()
		{
			Run();
		}
	}
}
