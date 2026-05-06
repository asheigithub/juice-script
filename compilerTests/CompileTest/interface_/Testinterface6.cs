using juicescript.runtime;
using juicescript;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using juicescript.compiler;

namespace compilerTests.CompileTest.interface_
{
	[TestClass]
	public class TestInterface6 : CodeTestBase
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
	
	[Doc]
	public class Main 
	{
		
		//public static var BBB =  7;
		public function Main(i:int,...a) 
		{
			super()
		}
		
		public  var F:Function = function ():void 
		{
		
		}
		
		public static function CCC(obj)
		{
			
		}
		
	}
	
}

interface II
{
	function foo():void; 
}

interface it
{
	function foo():void;
}

interface it2 extends it,II,II
{
	function foo2():void;
	
	function get p1():int;
	
	//function set p1(i:int):void;
}


class A  implements it2
{
	function aaa():void 
	{
		
	}
	
	public function foo():void
	{
		
	}
	
	public function foo2():void 
	{
		
	}
	
	public function get p1():int 
	{
		j = 777;
		return 8;
	}
	
	public function set p1(i:int):void
	{
		j = 666;
	}
	
}

internal class C extends A
{
	public override function foo():void 
	{
		//o = null;
	}
}


var o:it2 ;
o = new C();
var j;
//o.foo();

function test(o:II)
{
	o.foo();
	
}

test(o);
o.p1;


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

			RtScriptClass rtPayload = (RtScriptClass)globalInstance.facility;

			NaNBoxing o = rtPayload.ReadSlot(0);
			Assert.AreEqual(NaNBoxing.BoxType.HeapPtr, o.ValueType);

			RtHeapBase instance = player.Context.GC.Heap[o.HeapPtr];
			Assert.AreEqual(RtHeapTypeKind.INSTANCE, instance.TypeKind);
			Assert.AreEqual("C", instance.Type.QName.Name);

			NaNBoxing p = rtPayload.ReadSlot(1);
			Assert.AreEqual(NaNBoxing.BoxType.Short, p.ValueType);

			Assert.AreEqual(777,p.ShortValue);

		}

		[TestMethod]
		public void Test()
		{
			
			Run();
			
		}
	}

}
