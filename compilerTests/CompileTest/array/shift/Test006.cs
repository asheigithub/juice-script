using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.array.shift
{
	[TestClass]
	public sealed class Test006 : CodeTestBase
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
		public function Main()
		{
		}
	}
}

import geom.Vector2;

[struct]
final class A
{
	public var i:int;
	
	
}


[struct]
final class B
{
	public var j:uint;
}

function test(...rest)
{
	//var a:Array = [new A(), new B(),new Vector2()];
	
	rest.shift( );
	
	trace(rest);
	
}

test( new B(), new A(),new Vector2());
"
				}
			);

			return project;
		}

		protected override void TestIsPass(Player player, PlayerException ex)
		{
			player.ForceGC();
			{
				var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Main");
				Assert.IsNotNull(global);
				var globalInstance = player.Context.GC.Heap[global.__global_index__];
				Assert.IsNotNull(globalInstance);
				Assert.IsNull(ex);

				StringPrint print = (StringPrint)player.Print;

				Assert.AreEqual("[object A],(0.00,0.00)\r\n", print.GetOutput());
			}
		}

		[TestMethod]
		public void Test()
		{
			Run();
		}
	}
}