using juicescript.runtime;
using juicescript;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using juicescript.compiler;

namespace compilerTests.CompileTest.assigntarget
{
    [TestClass]
    public sealed class Assign29 : CodeTestBase
    {
        protected override TestCodeProject LoadProject()
        {
            TestCodeProject project = new TestCodeProject();

            project.libs = [Juice_GlobalSwc];

            project.testCodes = new List<TestCodeFile>();

			project.testCodes.Add(
				new TestCodeFile()
				{
					Path = "ns1/A.as",
					Code = @"
package ns1 
{
	import flash.display.Sprite;
	/**
	 * ...
	 * @author 
	 */
	public class A extends B
	{
		internal var APP = 8;
		public function A() 
		{
			
		}
		
		public function Go(o:A)
		{
			return o.BF;
		}
		
	}

}
"
				}
				);


			project.testCodes.Add(
				new TestCodeFile()
				{
					Path = "ns1/B.as",
					Code = @"
package ns1 
{
	import flash.display.Sprite;
	/**
	 * ...
	 * @author 
	 */
	public class B extends Sprite
	{
		
		public function B() 
		{
			
		}
		
		internal function BF()
		{
			o = 5;
		}
		public function BF()
		{
			o = 6;
		}
		
	}

}

var o;
"
				}
				);


			project.testCodes.Add(
                new TestCodeFile()
                {
                    Path = "Main.as",
                    Code = @"
package 
{
	import flash.display.Sprite;
	import ns1.A;
	import ns1.B;
	[Doc]
	/**
	 * ...
	 * @author 
	 */
	public class Main extends A
	{
		public function Test()
		{
			//o = this.internal::[""APP""];
			o = new A().Go(new Main());
		}
	}
}

new Main().Test();

var o;
o();

"
				}


                );


            return project;
        }

        protected override void TestIsPass(Player player, PlayerException ex)
        {
			//player.ForceGC();

		}


		[TestMethod]
        public void Test()
        {
			bool israise=false;
			try
			{
				Run();
			}
			catch (CompilerException ex)
			{
				Assert.AreEqual("Ambiguous reference to BF", ex.Message);
				israise=true;
			}
			
			Assert.IsTrue( israise );
			
        }
    }
}
