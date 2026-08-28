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
	public class TestSturct019 : CodeTestBase
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
	
	public var z:float = 3;
	
	public function toString()
	{
		return ""{ x="" + x +"",z="" +z+""}"";
	}
	
}

var k:V;

var o:O = new O();
o.vec.x = 0;
o.vec.z = 666;

trace(o);

o.vec = k;

trace(o);


"
				}


				);


			return project;
		}

		protected override void TestIsPass(Player player, PlayerException ex)
		{
			Assert.IsNull(ex);

			
			player.ForceGC();

			Assert.AreEqual("{ i=0, vec={ x=0,z=666}, b=false}\r\n{ i=0, vec={ x=1,z=3}, b=false}\r\n", ((StringPrint)player.Print).GetOutput());

		}


		[TestMethod]
		public void Test()
		{
			Run();
		}
	}
}
