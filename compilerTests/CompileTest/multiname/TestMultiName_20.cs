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
    public class TestMultiName_20 : CodeTestBase
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
		
		
		static internal var ABC = 18;
		
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
            var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Main");
            Assert.IsNotNull(global);
            var globalInstance = player.Context.GC.Heap[global.__global_index__];
            Assert.IsNotNull(globalInstance);

            Assert.IsNull(ex);

            var payload = (RtPayloadScriptClass)globalInstance.facility;

            NaNBoxing c = payload.ReadSlot(0);
            Assert.AreEqual(c.ValueType, NaNBoxing.BoxType.Sbyte);
            Assert.AreEqual(c.SByteValue, 18);
        }


        [TestMethod]
        public void Test()
        {
			Run();
        }

    }
}
