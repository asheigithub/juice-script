using juicescript;
using juicescript.ABC;
using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.typeconvert
{
    [TestClass]
    public class TestTypeConv4 : CodeTestBase
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
	
	//use namespace AS3;
	import flash.display.Sprite;
	import ns1.Class2;
	import ns1.TNS;
	[Doc]
	public class Main extends Sprite
	{
		
		//public static var BBB =  7;
		public function Main() 
		{
			
		}
		
		public var j:int ;
		
		
		
	}
}
import flash.utils.ByteArray;
import flash.utils.IDataInput;
import ns1.IEE;

class OO
{
	public var o:IDataInput;
}

var a = new OO();
a.o = new Main();



"
                }


                );


            return project;

        }

        protected override void TestIsPass(Player player, PlayerException ex)
        {
            

            Assert.IsNotNull(ex);

            Assert.IsTrue(ex.ToDebugMessage().EndsWith("to flash.utils.IDataInput."));

        }


        [TestMethod]
        public void Test()
        {
            Run();
        }

    }
}
