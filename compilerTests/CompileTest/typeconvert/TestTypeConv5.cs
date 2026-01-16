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
    public class TestTypeConv5 : CodeTestBase
    {
        protected override TestCodeProject LoadProject()
        {
            TestCodeProject project = new TestCodeProject();

            project.libs = [Juice_GlobalSwc];

            project.testCodes = new List<TestCodeFile>();

            project.testCodes.Add(
                new TestCodeFile()
                { 
                    Path = "ns1/BaseM.as",
                    Code = @"
package ns1 
{
	import flash.display.Sprite;
	/**
	 * ...
	 * @author 
	 */
	public class BaseM extends Sprite
	{
		
		public static const FFF = 6666;
		protected static const VVV = ""abcd"";
		public function BaseM() 
		{
			
		}
		
		public static function Test(k)
		{
			trace( k + "" 4"");
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
import ns1.BaseM;
var o = {};
o.toString = function () 
{
	trace(arguments.callee == o.toString);
	return ""ABCD"";
}

BaseM.Test(o);
trace(o);



"
				}


                );


            return project;

        }

        protected override void TestIsPass(Player player, PlayerException ex)
        {
            

            Assert.IsNull(ex);

            Assert.AreEqual("true\r\nABCD 4\r\ntrue\r\nABCD\r\n", ((StringPrint) player.Print).output.ToString());

        }


        [TestMethod]
        public void Test()
        {
            Run();
        }

    }
}
