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
    public class TestMultiName_13 : CodeTestBase
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
			
			c = private::a;
			
			var as3 = M;
			
			d = Main.as3::['a'];
			
			
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

class u
{
	TNS var tns;
	public function u()
	{
		TNS::tns = 444;
		
		c = TNS::tns;
		
	}
	internal var o:Object;
}



var b = new Main();

b.b = new u();

b.b.o = new u();

b.b.o.TNS::tns = 666;

var e = b.b.o.TNS::tns;

var f = b.b.o.o;

//trace(d);
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

            var payload = (RtPayloadScriptClass)globalInstance.facility;

            NaNBoxing c = payload.ReadSlot(2);
            Assert.AreEqual(c.ValueType, NaNBoxing.BoxType.Short);
            Assert.AreEqual(c.ShortValue, 444);

            NaNBoxing e = payload.ReadSlot(5);
            Assert.AreEqual(e.ValueType, NaNBoxing.BoxType.Short);
            Assert.AreEqual(e.ShortValue, 666);

            NaNBoxing f = payload.ReadSlot(6);
            Assert.AreEqual(f.ValueType, NaNBoxing.BoxType.Null);


        }


        [TestMethod]
        public void Test()
        {

            Run();

        }
    }
}
