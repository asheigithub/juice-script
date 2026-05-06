using juicescript;
using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.callfun
{
    [TestClass]
    public class TestCallMethod3 : CodeTestBase
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
		
		
		TNS function ABC( a = 1 ):void
		{
			
		}
		
		internal function get B():Object
		{
			return 0;
		}
		
		internal function CCC(aa)
		{
			
		}
	}

}

//trace(Main.BBB);

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
	import ns1.Class2;
	import ns1.TNS;
	[Doc]
	public class Main extends BaseM
	{
		//public static var BBB =  7;
		public function Main() 
		{
			this.CCC();
		}
		
		public  function CCC()
		{
			o = 555;
		}
		
		public var j:int ;
		
		public var k:Namespace;
		
		
		internal function set B(i:*) 
		{
			
		}
	}
	
	
	
}
import ns1.TNS;
new Main();
//new Main().TNS::ABC();

var o;


"
                }


                );


            return project;
        }

        protected override void TestIsPass(Player player, PlayerException ex)
        {
            player.ForceGC();

            var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Main");
            Assert.IsNotNull(global);
            var globalInstance = player.Context.GC.Heap[global.__global_index__];
            Assert.IsNotNull(globalInstance);
            Assert.IsNull(ex);

            RtScriptClass rtPayload = (RtScriptClass)globalInstance;
            
            NaNBoxing a = rtPayload.ReadSlot(0);
            Assert.AreEqual(NaNBoxing.BoxType.Short, a.ValueType);
            Assert.AreEqual(555, a.ShortValue );
        }


        [TestMethod]
        public void Test()
        {
            Run();
        }

    }
}
