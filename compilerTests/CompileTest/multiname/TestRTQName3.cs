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
    public class TestRTQName3 : CodeTestBase
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

            u_ins = new u();

            C = u_ins.test::k;


            

        }

        AS3 var u_ins:u;

        AS3 var L = new Class2();
        public var Y = TTT;

        AS3 var Z;
        
        public::KKF = 6666;
    }
}
namespace test = '';
class u
{
	test var k ;
	internal var k='aaa';
	
	function u()
	{
		k = 'bbb';
	}
}

var b = new Main();

var C;


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

            RtScriptClass rtPayload = (RtScriptClass)globalInstance;
            NaNBoxing test = rtPayload.ReadSlot(0);

            Assert.AreEqual(test.ValueType, NaNBoxing.BoxType.HeapPtr);

            var ns = player.Context.GC.Heap[test.HeapPtr];
            Assert.AreEqual(ns.TypeKind, RtHeapTypeKind.NAMESPACE);
            Assert.IsTrue(((RtNameSpace)ns).ASNamespace.Name.EndsWith(":test"));


            NaNBoxing b = rtPayload.ReadSlot(1);
            NaNBoxing C = rtPayload.ReadSlot(2);

            Assert.AreEqual(C.ValueType, NaNBoxing.BoxType.Undefined);

        }


        [TestMethod]
        public void Test()
        {
            Run();
        }
    }
}
