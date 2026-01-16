using juicescript;
using juicescript.compiler;
using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.Struct
{
	[TestClass]
	public class TestSturct001 : CodeTestBase
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
		
	}
	
}

[struct]
final class Vector2
{
	public var k:String
	public var x:float;
	public var y:float;
	
	public function Vector2(x:float=0,y:float=0):void 
	{
		this.x = x;
		this.y = y;
	}
	
}

var v = new Vector2(3, 4);

var w = v;

v.x = 66;
v.y = 77;

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
			bool raise = false;
			try
			{
				Run();
			}
			catch (ResolverException e)
			{
				Assert.AreEqual("struct only use primitive type.", e.Message);
				raise = true;
			}

			Assert.IsTrue(raise);
		}
	}
}
