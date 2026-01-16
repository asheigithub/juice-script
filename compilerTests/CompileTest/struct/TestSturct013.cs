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
	public class TestSturct013 : CodeTestBase
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
	public var x:Number = 1;
	public var y:W;
	public var z:Number = 3;
	
	public function toString()
	{
		return ""{ x="" + x + "",y="" +y +"",z="" +z+""}"";
	}
	
}

[struct]
final class W
{
	public var w:Boolean = true;	
	
	public var n:int = 3;
	public var p:int = 5;
	
	public function toString()
	{
		return ""{ w="" + w + "",n="" + n + "",p="" + p + ""}""  ;
	}
	
}

(
function ():void 
{
	var v:Vector.<O> = new Vector.<O>(3);
	
	
	function m1(o)
	{
		o.n = 1024;
		
		return o;
	}
	
	trace( m1(v[1].vec.y));
	
	trace(v);
	
	v[2].i = 666;
	v[2].vec.x = NaN;
	v[1].vec.y.n = 1024;
	
	trace(v);
	
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

			Assert.AreEqual("{ w=true,n=1024,p=5}\r\n{ i=0, vec={ x=1,y={ w=true,n=3,p=5},z=3}, b=false},{ i=0, vec={ x=1,y={ w=true,n=3,p=5},z=3}, b=false},{ i=0, vec={ x=1,y={ w=true,n=3,p=5},z=3}, b=false}\r\n{ i=0, vec={ x=1,y={ w=true,n=3,p=5},z=3}, b=false},{ i=0, vec={ x=1,y={ w=true,n=1024,p=5},z=3}, b=false},{ i=666, vec=NaN, b=false}\r\n", ((StringPrint)player.Print).GetOutput());

		}


		[TestMethod]
		public void Test()
		{
			Run();
		}
	}
}
