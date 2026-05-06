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
    public class TestRTQName4 : CodeTestBase
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
            
            F = test;

            GF = AS3::KKF;
        }

        public var F;

        AS3 var L = new Class2();
        public var Y = TTT;

        AS3 var Z;
        
        public::KKF = 6666;
    }
}

var b = new Main();

namespace test = '';
class u
{
	test var k ;
	internal var k='aaa';
	
	function u()
	{
		b['F']::k = 9;
	}
}

use namespace test;
var iu = new u();
var c = iu.test::k;


var GF ;

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

            RtScriptClass rtPayload = (RtScriptClass)globalInstance.facility;
            NaNBoxing test = rtPayload.ReadSlot(1);

            Assert.AreEqual(test.ValueType, NaNBoxing.BoxType.HeapPtr);

            var ns = player.Context.GC.Heap[test.HeapPtr];
            Assert.AreEqual(ns.TypeKind, RtHeapTypeKind.NAMESPACE);
            Assert.IsTrue(((RtNameSpace)ns.facility).ASNamespace.Name.EndsWith(":test"));

            NaNBoxing c = rtPayload.ReadSlot(3);

            Assert.AreEqual(c.ValueType, NaNBoxing.BoxType.Sbyte);
            Assert.AreEqual(c.SByteValue, 9);


            NaNBoxing GF = rtPayload.ReadSlot(4);
            Assert.AreEqual(GF.ValueType, NaNBoxing.BoxType.Short);
            Assert.AreEqual(GF.ShortValue, 9999);

        }


        [TestMethod]
        public void Test()
        {
            Run();
        }
    }
}
