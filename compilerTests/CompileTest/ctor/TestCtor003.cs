using juicescript;
using juicescript.compiler;
using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.ctor
{
	[TestClass]
	public class TestCtor003 : CodeTestBase
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
	public class Main extends Sprite
	{
		
		public function Main(i:int) 
		{
			o=i;
		}
		
		public function ABC()
		{
			
		}
		
	}
}

var o;

new Main(""2"");


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
				Assert.IsNotNull(ex);
				Assert.AreEqual(ex.Message, "Implicit coercion of a value with static type String to a possibly unrelated type int.");

				raised = true;
			}

			Assert.IsTrue(raised);
		}


	}
}
