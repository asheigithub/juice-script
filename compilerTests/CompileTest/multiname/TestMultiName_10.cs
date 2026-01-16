using juicescript.runtime;
using juicescript;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using juicescript.compiler;

namespace compilerTests.CompileTest.multiname
{
    [TestClass]
    public class TestMultiName_10 : CodeTestBase
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
		
        private var PPPP;

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
	/**
	 * ...
	 * @author 
	 */
	public class Main extends Class2 
	{
		
		//public static var BBB =  7;
		public function Main() 
		{
			super();
			
			c = internal::['a'];
			d = private::['a'];
			e = protected::['a'];

			
			f = Main.private::['a'];
			g = Main.internal::['a'];
			//h = Main.protected::['a']; //此处无法访问
			
			i = Main.M::['a'];
			j = this['M']::['a'];

			var as3 = M;			
			k = Main.as3::['a'];

		}
		
		
		static internal var a = 2;
		
		static private var a = 3;
		
		static protected var a = 4;
		
		  
		static AS3 var a = 5;
		
//		static TNS var b = 0xFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF;
		
		//static var Test = ""8"" / 6;
		
		protected var a = J;
		
		public var b = a;
		
		static public var kk = K;
		
		static var KJ = kk;
		
		J = KJ;
		
		TNS var lj = ""tns::LJ"";
		
		AS3 var lj = ""as3::LJ"";
		
		static TNS var a = ""kk"";
		
		public::KKF = 6;
		
	}
	
	
	
}


import flash.utils.IDataInput;
import flash.utils.IDataOutput;
import ns1.Class2;

import ns1.TNS;



var K = Error;
var J = K;


var c = 777;
var d = 888;
var e = 999;
var f = 1000;
var g = 1100;
var h = 1200;
var i = 1300;
var j = 1400;
var k = 1500;
var b = new Main();

"
                }

                );


            return project;
        }

        protected override void TestIsPass(Player player, PlayerException ex)
        {
			//player.ForceGC();
            var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Main");
            Assert.IsNotNull(global);
            var globalInstance = player.Context.GC.Heap[global.__global_index__];
            Assert.IsNotNull(globalInstance);

            Assert.IsNull(ex);

            var payload = (RtPayloadScriptClass)globalInstance.facility;

            NaNBoxing c = payload.ReadSlot(2);
            Assert.AreEqual( NaNBoxing.BoxType.Sbyte, c.ValueType);
            Assert.AreEqual(2,c.SByteValue);

            NaNBoxing d = payload.ReadSlot(3);
            Assert.AreEqual(NaNBoxing.BoxType.Sbyte, d.ValueType);
            Assert.AreEqual(3,d.SByteValue);

            NaNBoxing e = payload.ReadSlot(4);
            Assert.AreEqual(NaNBoxing.BoxType.HeapPtr, e.ValueType);
            var instance = player.Context.GC.Heap[e.HeapPtr];
            Assert.AreEqual(player.Context.ERROR,((RtPayloadScriptClass)instance.facility).Meta);

            NaNBoxing f = payload.ReadSlot(5);
            Assert.AreEqual(NaNBoxing.BoxType.Sbyte, f.ValueType);
            Assert.AreEqual(3,f.SByteValue);

            NaNBoxing g = payload.ReadSlot(6);
            Assert.AreEqual(NaNBoxing.BoxType.Sbyte, g.ValueType);
            Assert.AreEqual(2, g.SByteValue);

            NaNBoxing h = payload.ReadSlot(7);
            Assert.AreEqual(NaNBoxing.BoxType.Short, h.ValueType);
            Assert.AreEqual(1200, h.ShortValue);


            NaNBoxing i = payload.ReadSlot(8);
            Assert.AreEqual(NaNBoxing.BoxType.Sbyte, i.ValueType);
            Assert.AreEqual(5, i.SByteValue);

            NaNBoxing j = payload.ReadSlot(9);
            Assert.AreEqual(NaNBoxing.BoxType.Sbyte, j.ValueType);
            Assert.AreEqual(5, j.SByteValue);


            NaNBoxing k = payload.ReadSlot(10);
            Assert.AreEqual(NaNBoxing.BoxType.Sbyte, k.ValueType);
            Assert.AreEqual(5, k.SByteValue);



        }


        [TestMethod]
        public void Test()
        {

            Run();

        }
    }
}
