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
    public class TestCallFun7 : CodeTestBase
    {
        protected override TestCodeProject LoadProject()
        {
            TestCodeProject project = new TestCodeProject();

            project.libs = [Juice_GlobalSwc];

            project.testCodes = new List<TestCodeFile>();

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
	public class Main extends Sprite
	{
		
		AS3 var F:Function = function (i:int):void 
		{
			
		} ;
		
		//public static var BBB =  7;
		public function Main() 
		{
			//F(1);
		}
		
		public function ABC()
		{
			
		}
		
		
		public var j:int ;
		
		public var k:Namespace;
		
		
		
	}
}



var a;

(function ():void   
{
	a = arguments;
})(1,2,3);

//trace(a);


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

            RtPayloadScriptClass rtPayload = (RtPayloadScriptClass)globalInstance.facility;
            
            NaNBoxing a = rtPayload.ReadSlot(0);
            Assert.AreEqual(NaNBoxing.BoxType.HeapPtr, a.ValueType);

            RtHeapInstance instance = player.Context.GC.Heap[a.HeapPtr];
            Assert.AreEqual(RtHeapTypeKind.ARRAY, instance.TypeKind);
            Assert.AreEqual(RtPayloadArray.ArrayStoreMode.normal, ((RtPayloadArray)instance.facility).StoreMode);

            Assert.AreEqual(3u, ((RtPayloadArray)instance.facility).GetLength(player));
        }


        [TestMethod]
        public void Test()
        {
            Run();
        }

    }
}
