using juicescript;
using juicescript.compiler;
using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.multiname
{
    [TestClass]
    public class TestMultiName_19 : CodeTestBase
    {
        protected override TestCodeProject LoadProject()
        {
            TestCodeProject project = new TestCodeProject();

            project.libs = [Juice_GlobalSwc];

            project.testCodes = new List<TestCodeFile>();

            project.testCodes.Add(
                   new TestCodeFile()
                   {
                       Path = "ns1/TNS.as",
                       Code = @"
package ns1 
{
	public namespace TNS;
}
"
                   }
                );

            project.testCodes.Add(
                new TestCodeFile()
                {
                    Path = "BaseM.as",
                    Code = @"
package 
{
	import flash.display.Sprite;
	import ns1.TNS;
	
	/**
	 * ...
	 * @author 
	 */
	public class BaseM extends Sprite 
	{
		//internal static var JJ = 6 + Main.BBB;
		public function BaseM() 
		{
			super();
			//j=a;
			
		};
	
		//(function ():void 
		//{
		//trace(""BM"");
		//})();
		
		//var JJJ = 6 + 1;
		
		static protected  var KKF = uint.MIN_VALUE;
		
		var a:Number = 4;
		protected var b:uint;
		var c:Boolean;
		var d:Number;
		
		TNS var tttt;
		
		
		internal var ABC = 18;
		
	}

}

"
                }
                ); ;

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
	public class Main extends BaseM
	{
		
		
		public function Main() 
		{
		
		}
		
		
		public var j:int ;
		
		public var k:Namespace;
		
		
		(function ():void 
		{
		o = ABC;
		})();

	}

}
var o;

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
			bool raised=false;
            try
            {
                Run();
            }
            catch (ResolverException ex)
            {
                Assert.IsNotNull(ex);
                Assert.AreEqual(ex.Message, "Access of possibly undefined property ABC.");

				raised = true;
            }

			Assert.IsTrue(raised);

        }

    }
}
