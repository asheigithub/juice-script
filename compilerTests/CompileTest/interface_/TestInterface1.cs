using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using juicescript;
using juicescript.runtime;

namespace compilerTests.CompileTest.interface_
{
	[TestClass]
	public class TestInterface1 : CodeTestBase
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
	
}

internal class C extends A
{
	public override function foo():void 
	{
		o = null;
	}
}


var o:it2 ;
o = new C();

//o.foo();

function test(o:II)
{
	o.foo();
	
}

test(o);

//o.foo2();

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
			Assert.AreEqual(NaNBoxing.BoxType.Null, o.ValueType);

		}

		[TestMethod]
		public void Test()
		{
			Run();
		}
	}

}
