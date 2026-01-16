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
    public class TestGC006 : CodeTestBase
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
	import flash.display.Sprite;
	import ns1.BaseM;
	
	[Doc]
	/**
	 * ...
	 * @author 
	 */
	public class Main extends Sprite
	{
		public var v;
	}
	
}

dynamic class O
{
    
}
var i = new O();
var o;
var p;

(function () 
{
	var a;
	var b;
	
	
	(function ():void 
	{
		a = new O(); a.v = 1;
		b = a;		
		a = new O(); a.v = 2;
	})();
	
	o = a.v;
	p = b.v;		
})();




"
				}


                );


            return project;

        }

        protected override void TestIsPass(Player player, PlayerException ex)
        {
           
            Assert.IsNull(ex);

            Assert.AreEqual(1, player.Context.GC.Heap.DumpHeap()
                .Where(o => o.TypeKind == RtHeapTypeKind.INSTANCE && o.Type.QName.Name == "O").Count());

			Assert.AreEqual(2, player.Context.GC.Heap.DumpHeap()
				.Where(o => o.TypeKind == RtHeapTypeKind.DYNAMIC_PROPERTYS && ((RtPayloadShape)player.Context.GC.Heap[((RtPayloadDynamic)o.facility).SHAPE_PTR].facility).Attribute.HasFlag(RtPayloadShape.PropertyAttribute.Enumerable)).Count());

			var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Main");
            Assert.IsNotNull(global);
            var globalInstance = player.Context.GC.Heap[global.__global_index__];
            Assert.IsNotNull(globalInstance);
            Assert.IsNull(ex);

            RtPayloadScriptClass rtPayload = (RtPayloadScriptClass)globalInstance.facility;

            var o = rtPayload.ReadSlot(1);
            Assert.AreEqual(NaNBoxing.BoxType.Sbyte, o.ValueType);
            Assert.AreEqual(2, o.SByteValue);

            var p = rtPayload.ReadSlot(2);
			Assert.AreEqual(NaNBoxing.BoxType.Sbyte, p.ValueType);
			Assert.AreEqual(1, p.SByteValue);



		}

		[TestMethod]
        public void Test()
        {
            Run();
        }

    }
}
