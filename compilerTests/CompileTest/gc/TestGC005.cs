using juicescript;
using juicescript.ABC;
using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.gc
{
    [TestClass]
    public class TestGC005 : CodeTestBase
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
	public class Main
	{
		
		//public static var BBB =  7;
		public function Main() 
		{
			
		}
	}
}
class a
{
	public var I;
}

var b = new a();
b.I = 5;

var c = b;

b = new a();


"
                }


                );


            return project;

        }

        protected override void TestIsPass(Player player, PlayerException ex)
        {
            player.ForceGC();
            Assert.IsNull(ex);

            Assert.AreEqual(1, player.Context.GC.Heap.DumpHeap()
                .Where(o => o.Kind == RtHeapTypeKind.CLASS && ((RtScriptClass)o).Meta.QName.Name == "a").Count());

            ASClass @class = (ASClass)((RtScriptClass)player.Context.GC.Heap.DumpHeap().First
                (o => o.Kind == RtHeapTypeKind.CLASS && ((RtScriptClass)o).Meta.QName.Name == "a")).Meta;


            var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Main");
            Assert.IsNotNull(global);
            var globalInstance = player.Context.GC.Heap[global.__global_index__];
            Assert.IsNotNull(globalInstance);
            Assert.IsNull(ex);

            RtScriptClass rtPayload = (RtScriptClass)globalInstance;

            NaNBoxing b = rtPayload.ReadSlot(0);
            Assert.AreEqual(NaNBoxing.BoxType.HeapPtr, b.ValueType);

            RtInstance bins = (RtInstance)player.Context.GC.Heap[b.HeapPtr];
            var b_I = bins.ReadSlot(0, player);
            Assert.AreEqual(NaNBoxing.BoxType.Undefined, b_I.ValueType);

            NaNBoxing c = rtPayload.ReadSlot(1);
            Assert.AreEqual(NaNBoxing.BoxType.HeapPtr, b.ValueType);

            RtInstance cins = (RtInstance)player.Context.GC.Heap[c.HeapPtr];
            var c_I = cins.ReadSlot(0, player);
            Assert.AreEqual(NaNBoxing.BoxType.Sbyte, c_I.ValueType);





            //var objList = player.Context.GC.Heap.DumpHeap().Where(
            //    o => o.TypeKind == RtHeapTypeKind.INSTANCE && o.Type == @class.Instance
            //    &&
            //    !((RtPayloadInstance)o.facility).isCache
            //    );

            //Assert.AreEqual(0, objList.Count());

            //Assert.AreEqual(1,
            //player.Context.GC.Heap.DumpHeap().Where(
            //    o => o.TypeKind == RtHeapTypeKind.INSTANCE && o.Type == player.Context.OBJECT.Instance
            //    &&
            //    !((RtPayloadInstance)o.facility).isCache
            //    ).Count());



        }

        [TestMethod]
        public void Test()
        {
            Run();
        }

    }
}
