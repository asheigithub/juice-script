using juicescript;
using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.dynamicprop
{
    [TestClass]
    public class TestDynamicArray1 : CodeTestBase
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
		
		//public static var BBB =  7;
		public function Main() 
		{
			
		}
		
		public var j:int ;
		
		public var k:Namespace;
		
	}
}


var m = new Array();

m[1.1] = 77;
m[null] = 6;
m[undefined] = 7;


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


            RtPayloadScriptClass rtPayload = (RtPayloadScriptClass)globalInstance.facility;
            
            NaNBoxing o = rtPayload.ReadSlot(0);
            Assert.AreEqual( RtHeapTypeKind.ARRAY , player.Context.GC.Heap[o.HeapPtr].TypeKind );
            RtPayloadArray i_o = (RtPayloadArray)player.Context.GC.Heap[o.HeapPtr].facility;
            int p = i_o.PROPERTY_PTR(player);
            RtPayloadDynamic dynamic = (RtPayloadDynamic)player.Context.GC.Heap[p].facility;
            Assert.AreEqual(3, dynamic.Slots.Count);
            Assert.AreEqual(77, dynamic.Slots[0].SByteValue);
            Assert.AreEqual(6, dynamic.Slots[1].SByteValue);
            RtPayloadShape shape = (RtPayloadShape)player.Context.GC.Heap[ dynamic.SHAPE_PTR ].facility;
            Assert.AreEqual("undefined", ((RtPayloadString)player.Context.GC.Heap[ shape.PTR_NAME].facility).Str);
            shape = (RtPayloadShape)player.Context.GC.Heap[shape.PTR_PARENT].facility;
            Assert.AreEqual("null", ((RtPayloadString)player.Context.GC.Heap[shape.PTR_NAME].facility).Str);
            shape = (RtPayloadShape)player.Context.GC.Heap[shape.PTR_PARENT].facility;
            Assert.AreEqual("1.1", ((RtPayloadString)player.Context.GC.Heap[shape.PTR_NAME].facility).Str);
            shape = (RtPayloadShape)player.Context.GC.Heap[shape.PTR_PARENT].facility;
            Assert.AreEqual(0, shape.PTR_NAME);
            Assert.AreEqual(0, shape.PTR_PARENT);


        }



        [TestMethod]
        public void Test()
        {
            Run();
        }

    }
}
