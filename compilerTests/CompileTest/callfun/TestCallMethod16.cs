using juicescript;
using juicescript.compiler;
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
    public class TestCallMethod16 : CodeTestBase
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
	
		//(function ():void 
		//{
		//trace(""BM"");
		//})();
		
		//var JJJ = 6 + 1;
		
		static protected  var KKF = uint.MIN_VALUE;
		
		var a:Number = 4;
		//protected var b:uint;
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
		
		protected override function CCC(aa)
		{
			super.CCC(999);
			add = aa;
		}
	}

}

var o;
//trace(Main.BBB);
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
			var cc = super[""CCC""];
			cc(666);
		}

		protected override function CCC(aa)
		{
			super.CCC(555);
			o = aa;
		}

		public var j:int ;
		
		public var k:Namespace;
		
		internal function set B(i:*) 
		{
			
		}
	}
		
}
import ns1.TNS;

//new Main().TNS::ABC();
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

                RtScriptClass rtPayload = (RtScriptClass)globalInstance.facility;

                NaNBoxing a = rtPayload.ReadSlot(0);
                Assert.AreEqual(NaNBoxing.BoxType.Undefined, a.ValueType);
                //Assert.AreEqual(666, a.ShortValue);
            }

            {
                var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "BaseM");
                Assert.IsNotNull(global);
                var globalInstance = player.Context.GC.Heap[global.__global_index__];
                Assert.IsNotNull(globalInstance);
                Assert.IsNull(ex);

                RtScriptClass rtPayload = (RtScriptClass)globalInstance.facility;

                NaNBoxing a = rtPayload.ReadSlot(0);
                Assert.AreEqual(NaNBoxing.BoxType.Undefined, a.ValueType);

                NaNBoxing b = rtPayload.ReadSlot(1);
                Assert.AreEqual(NaNBoxing.BoxType.Short, b.ValueType);
                Assert.AreEqual(666, b.ShortValue);

            }

            {
                var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Class2");
                Assert.IsNotNull(global);
                var globalInstance = player.Context.GC.Heap[global.__global_index__];
                Assert.IsNotNull(globalInstance);
                Assert.IsNull(ex);

                RtScriptClass rtPayload = (RtScriptClass)globalInstance.facility;

                NaNBoxing a = rtPayload.ReadSlot(0);
                Assert.AreEqual(NaNBoxing.BoxType.Short, a.ValueType);
                Assert.AreEqual(999, a.ShortValue);


            }
        }


        [TestMethod]
        public void Test()
        {
            Run();
        }

    }
}
