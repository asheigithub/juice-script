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
    public class TestMultiName1 : CodeTestBase
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
        
        private var PPPP = 7;

        public function Main()
        {
            super();
            c = b;
            d = PPPP;
            
            e=this['PPPP'];
            
        } 
    }
}

var b = new Main();
var c,d,e;

//var f = b['PPPP'];

class u
{
    internal var A;
}

var j = new u().A = 8;



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

            var payload = (RtScriptClass)globalInstance.facility;

            NaNBoxing c = payload.ReadSlot(1);
            Assert.AreEqual(c.ValueType, NaNBoxing.BoxType.Sbyte);
            Assert.AreEqual(c.SByteValue, 9);

            NaNBoxing d = payload.ReadSlot(2);
            Assert.AreEqual(d.ValueType, NaNBoxing.BoxType.Sbyte);
            Assert.AreEqual(d.SByteValue, 7);

            NaNBoxing e = payload.ReadSlot(3);
            Assert.AreEqual(e.ValueType, NaNBoxing.BoxType.Sbyte);
            Assert.AreEqual(e.SByteValue, 7);

            NaNBoxing j = payload.ReadSlot(4);
            Assert.AreEqual(j.ValueType, NaNBoxing.BoxType.Sbyte);
            Assert.AreEqual(j.SByteValue, 8);

        }


        [TestMethod]
        public void Test()
        {
            Run();
        }
    }
}
