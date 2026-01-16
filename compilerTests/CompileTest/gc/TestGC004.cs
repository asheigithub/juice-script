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
    public class TestGC004 : CodeTestBase
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
	public function a()
	{
		new Object();
		new Object();
		new Object();
	}

    public var c:int;
    public var d;
}

new a().d = new a() ;

class b
{
    public var d:short;

    var ci;

    public function b()
    {
        d = 999;
        ci = new c();
        j = d;
    }

}

class c
{
    
}

var k = new b();

var j;

var i = k.ci;

"
                }


                );


            return project;

        }

        protected override void TestIsPass(Player player, PlayerException ex)
        {
            player.ForceGC();
            Assert.IsNull(ex);

            Assert.AreEqual( 1,  player.Context.GC.Heap.DumpHeap()
                .Where( o=> o.TypeKind == RtHeapTypeKind.CLASS && ((RtPayloadScriptClass)o.facility).Meta.QName.Name == "a" ).Count());

            ASClass type_a = (ASClass)((RtPayloadScriptClass)player.Context.GC.Heap.DumpHeap().First
                (o => o.TypeKind == RtHeapTypeKind.CLASS && ((RtPayloadScriptClass)o.facility).Meta.QName.Name == "a").facility).Meta;


            var objList = player.Context.GC.Heap.DumpHeap().Where(
                o => o.TypeKind == RtHeapTypeKind.INSTANCE && o.Type == type_a.Instance
                //&&
                //! ((RtPayloadInstance)o.facility).isCache
                );

            Assert.AreEqual(0, objList.Count() );

            Assert.AreEqual(0,
            player.Context.GC.Heap.DumpHeap().Where(
                o => o.TypeKind == RtHeapTypeKind.INSTANCE && o.Type == player.Context.OBJECT.Instance

                &&
                ! player.Context.GC.Heap.IsClassProtoType(o)
                //&&
                //!((RtPayloadInstance)o.facility).isCache
                ).Count());

            ASClass type_b = (ASClass)((RtPayloadScriptClass)player.Context.GC.Heap.DumpHeap().First
                (o => o.TypeKind == RtHeapTypeKind.CLASS && ((RtPayloadScriptClass)o.facility).Meta.QName.Name == "b").facility).Meta;

            Assert.AreEqual(1,
            player.Context.GC.Heap.DumpHeap().Where(
                o => o.TypeKind == RtHeapTypeKind.INSTANCE && o.Type == type_b.Instance
                //&&
                //!((RtPayloadInstance)o.facility).isCache
                ).Count());


            var globalInstance = FindGlobal(player);

            var payload = (RtPayloadScriptClass)globalInstance.facility;

            NaNBoxing k = payload.ReadSlot(0);
            Assert.AreEqual(NaNBoxing.BoxType.HeapPtr, k.ValueType);


            NaNBoxing j = payload.ReadSlot(1);
            Assert.AreEqual(NaNBoxing.BoxType.Short, j.ValueType);
            Assert.AreEqual(999, j.ShortValue);


            ASClass type_c = (ASClass)((RtPayloadScriptClass)player.Context.GC.Heap.DumpHeap().First
                (o => o.TypeKind == RtHeapTypeKind.CLASS && ((RtPayloadScriptClass)o.facility).Meta.QName.Name == "c").facility).Meta;


            NaNBoxing i = payload.ReadSlot(2);
            Assert.AreEqual(NaNBoxing.BoxType.HeapPtr, i.ValueType);
            var instance = player.Context.GC.Heap[i.HeapPtr];
            Assert.AreEqual(RtHeapTypeKind.INSTANCE, instance.TypeKind);
            Assert.AreEqual(instance.Type, type_c.Instance);


        }

        [TestMethod]
        public void Test()
        {
            Run();
        }

    }
}
