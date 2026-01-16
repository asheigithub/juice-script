using juicescript;
using juicescript.compiler;
using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.property
{
    [TestClass]
    public class TestProperty08 : CodeTestBase
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
                    Path = "ns1/Class2.as",
                    Code = @"
package ns1 
{
	import flash.display.Sprite;
	/**
	 * ...
	 * @author 
	 */
	public class Class2 extends Sprite
	{
		static AS3 var KKF = 9999;
		static AS3 var UUU = 9999;
		public static var KKF = 1000;
		
		static public var TTT = AS3;
		
		protected var b = 9;
		
		internal var G;
		
		public var M:Namespace = AS3;
		
		//private var PPPPP;
		
		TNS var PRI;
		
		public function Class2()
		{
			
		}
		
		protected function CCC(aa)
		{
			e = aa;			
		}
	}

}

var e;

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
	import ns1.Class2;
	import ns1.TNS;
	
	/**
	 * ...
	 * @author 
	 */
	public class BaseM extends Class2 
	{
		//internal static var JJ = 6 + Main.BBB;
		public function BaseM() 
		{
			super();
			//j=a;
			
		};
	
		
		static protected  var KKF = uint.MIN_VALUE;
		
		var a:Number = 4;
		//protected var b:uint;
		var c:Boolean;
		var d:Number;
		
		TNS var tttt;
		
		
		TNS function ABC( a = 1 ):void
		{
			
		}
		
		TNS static function get B():Object
		{
			o = 5678;
		}
		
		protected override function CCC(aa)
		{
			super.CCC(999);
			add = aa;
		}
	}

}

var o;

var add;


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
	public class Main extends BaseM
	{
		
		
		//public static var BBB =  7;
		public function Main() 
		{
			BaseM.TNS::B;
		}

		protected override function CCC(aa)
		{
			
		}

		public var j:int ;
		
		public var k:Namespace;
		
		//internal function set B(i:*) 
		//{			
		//}
	}
		
}
import ns1.TNS;

new Main();
var o;


"
                }


                );


            return project;
        }

        protected override void TestIsPass(Player player, PlayerException ex)
        {
            player.ForceGC();
            {
                var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Main");
                Assert.IsNotNull(global);
                var globalInstance = player.Context.GC.Heap[global.__global_index__];
                Assert.IsNotNull(globalInstance);
                Assert.IsNull(ex);

                RtPayloadScriptClass rtPayload = (RtPayloadScriptClass)globalInstance.facility;

                //NaNBoxing a = rtPayload.ReadSlot(0);
                //Assert.AreEqual(NaNBoxing.BoxType.Undefined, a.ValueType);
                //Assert.AreEqual(666, a.ShortValue);
            }

            {
                var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "BaseM");
                Assert.IsNotNull(global);
                var globalInstance = player.Context.GC.Heap[global.__global_index__];
                Assert.IsNotNull(globalInstance);
                Assert.IsNull(ex);

                RtPayloadScriptClass rtPayload = (RtPayloadScriptClass)globalInstance.facility;

                NaNBoxing a = rtPayload.ReadSlot(0);
                Assert.AreEqual(NaNBoxing.BoxType.Short, a.ValueType);
                Assert.AreEqual(5678, a.ShortValue);

               

            }

        }


        [TestMethod]
        public void Test()
        {
            Run();
        }

    }
}
