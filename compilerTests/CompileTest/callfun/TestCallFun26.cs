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
    public class TestCallFun26 : CodeTestBase
    {
        protected override TestCodeProject LoadProject()
        {
            TestCodeProject project = new TestCodeProject();

            project.libs = [Juice_GlobalSwc];

            project.testCodes = new List<TestCodeFile>();

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
		
		 const CCC = 0;
		public function Class2()
		{
			
		}

		public function SBAFF(obj)
		{
			JJ = 55;
			obj.i = 666;
		}
		
	}
}
var JJ=new Vector.<int>;
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
	public class Main extends Class2
	{
		//public static var BBB =  7;
		public function Main() 
		{
			CCC();
		}
		
		public var i:int;

		public function CCC()
		{
			SBAFF(this);
		}
		
		;
		
		
	}
}


var o =new Main();


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

                RtPayloadScriptClass rtPayload = (RtPayloadScriptClass)globalInstance.facility;

                NaNBoxing o = rtPayload.ReadSlot(0);
                Assert.AreEqual(NaNBoxing.BoxType.HeapPtr, o.ValueType);
				RtHeapBase instance = player.Context.GC.Heap[o.HeapPtr];

				Assert.AreEqual(RtHeapTypeKind.INSTANCE, instance.TypeKind);
				Assert.AreEqual( "Main",instance.Type.QName.Name );

				RtPayloadInstance payloadInstance = (RtPayloadInstance)instance.facility;

				NaNBoxing box = payloadInstance.ReadSlot(4, instance.Type._link_codescope, player);
				Assert.AreEqual(NaNBoxing.BoxType.Int, box.ValueType);
				Assert.AreEqual(666, box.IntValue);
            }


            {
                var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Class2");
				Assert.IsNotNull(global);
				var globalInstance = player.Context.GC.Heap[global.__global_index__];
				Assert.IsNotNull(globalInstance);
				Assert.IsNull(ex);

				RtPayloadScriptClass rtPayload = (RtPayloadScriptClass)globalInstance.facility;

				NaNBoxing o = rtPayload.ReadSlot(0);
				Assert.AreEqual(NaNBoxing.BoxType.Sbyte, o.ValueType);
				Assert.AreEqual(55, o.SByteValue);

			}

            
        }


        [TestMethod]
        public void Test()
        {
            Run();
        }

    }
}
