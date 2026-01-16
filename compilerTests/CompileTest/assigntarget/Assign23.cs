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
    public sealed class Assign23 : CodeTestBase
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
	public class A extends Sprite
	{
		internal var APP = 8;
		public function A() 
		{
			
		}
		
		public function Go(o)
		{
			
			return o.internal::[""BPP""];
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
	/**
	 * ...
	 * @author 
	 */
	public class B 
	{
		protected var BPP = 9;
		public function B() 
		{
		}
	}
}
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
			o = new A().Go(new B());
		}
	}
}

new Main().Test();

var o;


"
				}


                );


            return project;
        }

        protected override void TestIsPass(Player player, PlayerException ex)
        {
			//player.ForceGC();
			Assert.IsNotNull(ex);
			Assert.AreEqual("[Fault] exception,[Message]=ReferenceError: Property ns1::BPP not found on ns1.B and there is no default value.", ex.ToDebugMessage());
			
		}


		[TestMethod]
        public void Test()
        {
			
			Run();
			
        }
    }
}
