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
    public class TestDynamicProperty2 : CodeTestBase
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
	}
}
dynamic class OO
{
	
}

var o = new OO();

o.U = 5;
o.K = 6;

var i = new OO();
i.K = 9;
i.U = 4;

i.K;
o.K;

delete i.K;


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

            var OO = player.Context.libs.SelectMany(o => o.Classes).FirstOrDefault(o => o != null && o.QName.Name.EndsWith("OO"));
            Assert.IsNotNull(OO);


            RtPayloadScriptClass rtPayload = (RtPayloadScriptClass)globalInstance.facility;
            
            NaNBoxing o = rtPayload.ReadSlot(0);
            Assert.AreEqual( OO.Instance, player.Context.GC.Heap[o.HeapPtr].Type );
            RtPayloadInstance i_o = (RtPayloadInstance)player.Context.GC.Heap[o.HeapPtr].facility;
            int p = i_o.PROPERTY_PTR(player,OO.Instance);
            RtPayloadDynamic dynamic = (RtPayloadDynamic)player.Context.GC.Heap[p].facility;
            Assert.AreEqual(2, dynamic.Slots.Count);
            Assert.AreEqual(5, dynamic.Slots[0].SByteValue);
            Assert.AreEqual(6, dynamic.Slots[1].SByteValue);
            RtPayloadShape shape = (RtPayloadShape)player.Context.GC.Heap[ dynamic.SHAPE_PTR ].facility;
            Assert.AreEqual("K", ((RtPayloadString)player.Context.GC.Heap[ shape.PTR_NAME].facility).Str);
            shape = (RtPayloadShape)player.Context.GC.Heap[shape.PTR_PARENT].facility;
            Assert.AreEqual("U", ((RtPayloadString)player.Context.GC.Heap[shape.PTR_NAME].facility).Str);
            shape = (RtPayloadShape)player.Context.GC.Heap[shape.PTR_PARENT].facility;
            Assert.AreEqual(0, shape.PTR_NAME);
            Assert.AreEqual(0, shape.PTR_PARENT);

            NaNBoxing i = rtPayload.ReadSlot(1);
            Assert.AreEqual(OO.Instance, player.Context.GC.Heap[i.HeapPtr].Type);
            RtPayloadInstance i_i = (RtPayloadInstance)player.Context.GC.Heap[i.HeapPtr].facility;
            dynamic = (RtPayloadDynamic)player.Context.GC.Heap[i_i.PROPERTY_PTR(player, OO.Instance ) ].facility;
            Assert.AreEqual(1, dynamic.Slots.Count);

            Assert.AreEqual(((RtPayloadShape)player.Context.GC.Heap[((RtPayloadDynamic)player.Context.GC.Heap[p].facility).SHAPE_PTR].facility).PTR_PARENT,
                dynamic.SHAPE_PTR
                );
        }



        [TestMethod]
        public void Test()
        {
            Run();
        }

    }
}
