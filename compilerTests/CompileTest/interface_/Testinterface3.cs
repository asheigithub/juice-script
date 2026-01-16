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
	public class TestInterface3 : CodeTestBase
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
			
		}

		[TestMethod]
		public void Test()
		{
			bool raised = false;

			try
			{
				Run();
			}
			catch (ResolverException ex)
			{

				Assert.AreEqual("interface method foo2 in interface it2 not implemented by class A", ex.Message);

				raised = true;
			}

			Assert.IsTrue(raised);

		}
	}

}
