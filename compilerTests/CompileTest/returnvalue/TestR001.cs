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
	public class TestR001 : CodeTestBase
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
		public function Main() 
		{
			
		}
		
		public  var F:Function = function ():void 
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
	
}



class A  implements it2
{
	
	function A():void 
	{
		
	}
	
	public function foo():void
	{
		
	}
	
	public function foo2():void 
	{
		
	}
	var seed:int = 5;
	public function get p1():int 
	{
		seed = -seed;
		return seed;
	}
	
	public function set p1(i:int):void
	{
		j = 666;
	}
	
}

internal class C extends A
{
	public function C()
	{
		super();
	}
	
	public override function foo():void 
	{
		
		//o = null;
	}
	
	public function Tsss():int
	{
		return this.p1, k = p1, p1;		
	}
	
}


var o:it2 ;

var j;
var k;
function test(o:II):Class
{
	return C;
}

var t:Class = test(null);
j = (new t()).Tsss();



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
			Assert.AreEqual(NaNBoxing.BoxType.Null, o.ValueType);

			NaNBoxing j = rtPayload.ReadSlot(1);
			Assert.AreEqual(NaNBoxing.BoxType.Int, j.ValueType);
			Assert.AreEqual(-5, j.IntValue);

			NaNBoxing k = rtPayload.ReadSlot(2);
			Assert.AreEqual(NaNBoxing.BoxType.Int, k.ValueType);
			Assert.AreEqual(5, k.IntValue);

		}


		[TestMethod]
		public void Test()
		{
			Run();
		}
	}
}
