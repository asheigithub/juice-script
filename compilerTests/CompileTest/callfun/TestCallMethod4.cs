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
    public class TestCallMethod4 : CodeTestBase
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
			a = aa;
			
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
			//F(1);
			//o = ABC;
			
			//this.CCC();
			
			this.CCC(7);
			
		}
		
		//internal override function CCC()
		//{
			//o = 555;
		//}
		
		public var j:int ;
		
		public var k:Namespace;
		
		internal function set B(i:*) 
		{
			
		}
	}
		
}
import ns1.TNS;

//new Main().TNS::ABC();

var o;

var n = new Main();

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

			RtScriptClass rtPayload = (RtScriptClass)globalInstance.facility;

			NaNBoxing a = rtPayload.ReadSlot(0);
			Assert.AreEqual(NaNBoxing.BoxType.Undefined, a.ValueType);

			NaNBoxing n = rtPayload.ReadSlot(1);
            Assert.AreEqual(NaNBoxing.BoxType.HeapPtr, n.ValueType);
            Assert.AreEqual(RtHeapTypeKind.INSTANCE, player.Context.GC.Heap[n.HeapPtr].TypeKind);
            Assert.AreEqual("Main", player.Context.GC.Heap[n.HeapPtr].Type.QName.Name);

			RtInstance payloadInstance = (RtInstance)player.Context.GC.Heap[n.HeapPtr].facility;
			var member = player.Context.GC.Heap[n.HeapPtr].Type._link_codescope.Members[0];

			Assert.AreEqual("a", member.QName.Name);
			var v = payloadInstance.ReadSlot(0, player.Context.GC.Heap[n.HeapPtr].Type._link_codescope, player);
			Assert.AreEqual(NaNBoxing.BoxType.Number, v.ValueType);
			Assert.AreEqual(7, v.Number);

        }


        [TestMethod]
        public void Test()
        {
            Run();
        }

    }
}
