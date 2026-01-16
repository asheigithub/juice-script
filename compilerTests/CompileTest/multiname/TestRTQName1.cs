using juicescript;
using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.multiname
{
    [TestClass]
    public class TestRTQName1 : CodeTestBase
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
    import ns1.TNS;
    import ns1.Class2;
    [Doc]
    public class Main extends Class2
    {
        public var K = 1;
        TNS var K = 2;
        
        public function Main()
        {
            super();

            this['Z'] = G;

        }
        
        AS3 var L = new Class2();
        public var Y = TTT;

        AS3 var Z;
        
        public::KKF = 6666;
    }
}
import ns1.Class2;
var b = new Main();

var tns = Class2.TTT;
var L = b.Y;

var k1 = b.K;
var k2 = b.L::K;

var k3 = b['Z'];
var k4 = b['Z']::K;

var k5 = b['Y']::['K'];

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

            

            RtPayloadScriptClass rtPayload = (RtPayloadScriptClass)globalInstance.facility;
            NaNBoxing b = rtPayload.ReadSlot(0);
            NaNBoxing tns = rtPayload.ReadSlot(1);
            NaNBoxing L = rtPayload.ReadSlot(2);

            Assert.AreEqual(tns, L);

            NaNBoxing k1 = rtPayload.ReadSlot(3);
            NaNBoxing k2 = rtPayload.ReadSlot(4);

            Assert.AreEqual(k1.ValueType, NaNBoxing.BoxType.Sbyte);
            Assert.AreEqual(k2.ValueType, NaNBoxing.BoxType.Sbyte);

            Assert.AreEqual(k1.SByteValue, 1);
            Assert.AreEqual(k2.SByteValue, 2);

            NaNBoxing k3 = rtPayload.ReadSlot(5);
            Assert.AreEqual(k3, tns);

            NaNBoxing k4 = rtPayload.ReadSlot(6);
            Assert.AreEqual(k4.ValueType, NaNBoxing.BoxType.Undefined);

            //NaNBoxing k5 = rtPayload.ReadSlot(7);
            //Assert.AreEqual(k5, k2);

            Assert.IsNotNull(ex);
            Assert.IsTrue(ex.ToDebugMessage().EndsWith("K is not defined."));
        }


        [TestMethod]
        public void Test()
        {
            Run();
        }
    }
}
