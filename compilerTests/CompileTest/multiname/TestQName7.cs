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
    public class TestQName7 : CodeTestBase
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
		
		static public var TTT = AS3;
		
		protected var b = 9;
		
		internal var G;
		
		public var M = AS3;
		
		TNS var PRI;
		
		public function Class2()
		{
			//JJ = Class2.TNS::TTT;
		}
		
	}

}

var JJ;
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
        }
        
        AS3 var L = new Class2();
        public var Y = UUU;
    }
}
var b = new Main();


import ns1.TNS;
var c = b.TNS::K;
var d = b.public::K;
var e = b.M;
var f = this['b'].K;

var g = b.AS3::L.M;
var h = b.AS3::L.public::M;
var i = b.Y;


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
            NaNBoxing b = rtPayload.ReadSlot(0);
            NaNBoxing c = rtPayload.ReadSlot(1);
            NaNBoxing d = rtPayload.ReadSlot(2);

            Assert.AreEqual(c.ValueType, NaNBoxing.BoxType.Sbyte);
            Assert.AreEqual(d.ValueType, NaNBoxing.BoxType.Sbyte);

            Assert.AreEqual(c.SByteValue, 2);
            Assert.AreEqual(d.SByteValue, 1);

            NaNBoxing e = rtPayload.ReadSlot(3);
            Assert.AreEqual(e.ValueType, NaNBoxing.BoxType.HeapPtr);

            var ns_instance = player.Context.GC.Heap[e.HeapPtr];
            Assert.AreEqual(ns_instance.Kind, RtHeapTypeKind.NAMESPACE);
            Assert.AreEqual(((RtNameSpace)ns_instance).ASNamespace.Name, ":AS3");

            NaNBoxing f = rtPayload.ReadSlot(4);
            Assert.AreEqual(f, d);

            NaNBoxing g = rtPayload.ReadSlot(5);
            Assert.AreEqual(e, g);

            NaNBoxing h = rtPayload.ReadSlot(6);
            Assert.AreEqual(h, g);

            NaNBoxing i = rtPayload.ReadSlot(7);
            Assert.AreEqual(i.ShortValue, 9999);
        }

        [TestMethod]
        public void Test()
        {

            Run();

        }

    }
}
