using juicescript.runtime;
using juicescript;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.multiname
{
    [TestClass]
    public class TestRTQName5 : CodeTestBase
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
		
		//static public var AS3 = null;
		
		static public var TTT = TNS;
		
		protected var b = 9;
		
		protected var G;
		
		public var M = AS3;
		
		TNS var PRI;
		
		public function Class2()
		{
            G = TNS;
			//JJ = Class2.TNS::TTT;
            
		}
		
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
	public class Main
	{
		
		//public static var BBB =  7;
		public function Main() 
		{
			v.AS3::len = 7;
		}
	}
}

import ns1.Class2;
var v = new Vector.<int>();


var m = new Main();

//var cc:Class = Vector.<int>;

//new cc();
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

            Assert.IsNotNull(ex);
            Assert.AreEqual("[Fault] exception,[Message]=ReferenceError: Cannot create property http://adobe.com/AS3/2006/builtin::len on __AS3__.vec::Vector.<int>.", ex.ToDebugMessage());

            RtScriptClass rtPayload = (RtScriptClass)globalInstance;
            NaNBoxing test = rtPayload.ReadSlot(0);

            Assert.AreEqual(test.ValueType, NaNBoxing.BoxType.HeapPtr);

            var vs = player.Context.GC.Heap[test.HeapPtr];
            Assert.AreEqual(vs.TypeKind, RtHeapTypeKind.VECTOR);
            

        }


        [TestMethod]
        public void Test()
        {
            Run();
        }
    }
}
