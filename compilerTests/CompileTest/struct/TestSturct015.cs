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
	public class TestSturct015 : CodeTestBase
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

var a = [ new P() ];

[struct]
final class P
{
	public var X:int;
	
	public function Test()
	{
		
		
	
		trace(X);
		//a.length = 0;
		X = 7;
		
		trace(X);
		
	}
	
}

//测试数组中的结构体刚取出时会标记，当作为参数传递时会clone一份。

a[0].X = 9;
a[0].Test();
trace(a[0].X);

"
				}


				);


			return project;
		}

		protected override void TestIsPass(Player player, PlayerException ex)
		{
			Assert.IsNull(ex);

			
			player.ForceGC();

			Assert.AreEqual("9\r\n7\r\n9\r\n", ((StringPrint)player.Print).GetOutput());

		}


		[TestMethod]
		public void Test()
		{
			Run();
		}
	}
}
