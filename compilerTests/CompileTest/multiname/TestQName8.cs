using juicescript.runtime;
using juicescript;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using juicescript.compiler;

namespace compilerTests.CompileTest.multiname
{
    [TestClass]
    public class TestQName8 : CodeTestBase
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
		native function NTEst(); 
		
		var j = A.IIA;

	}
	
	

}

var o = null;

class A extends Main
{
	static var IIA;
	
	var b = new Main().j;
	
	
	
} 


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
			catch (CompilerException ex)
			{
				raised = true;

				Assert.AreEqual("Attempted access of inaccessible property j through a reference with static type Main.", ex.Message);
			}
            
			Assert.AreEqual(true, raised);
        }

    }
}
