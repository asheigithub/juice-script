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
    public class TestGC011 : CodeTestBase
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
		
	}
	
}
class O
{
	public function O(v)
	{
		this.v = v;
	}
	
	private var v;
	
	public function V()
	{
		return v;
	}
	
}

var o;
var p;

(function () 
{
	(function (obj1,obj2):void 
	{
		var a:O;
		var b:O;
		
		a = obj1;
		b = obj2;		
		
		var c = a;
		a = b;
		b = c;
		
		
		o = a.V();
		p = b.V();		
		
	})( new O(1),new O(2) );
})(  );

"
				}


                );


            return project;

        }

        protected override void TestIsPass(Player player, PlayerException ex)
        {
           
            Assert.IsNull(ex);

            Assert.AreEqual(0, player.Context.GC.Heap.DumpHeap()
                .Where(o => o.TypeKind == RtHeapTypeKind.INSTANCE && o.Type.QName.Name == "O").Count());

			Assert.AreEqual(0, player.Context.GC.Heap.DumpHeap()
				.Where(o => o.TypeKind == RtHeapTypeKind.DYNAMIC_PROPERTYS && ((RtShape)player.Context.GC.Heap[((RtDynamic)o).SHAPE_PTR]).Attribute.HasFlag(RtShape.PropertyAttribute.Enumerable)).Count());

			var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Main");
            Assert.IsNotNull(global);
            var globalInstance = player.Context.GC.Heap[global.__global_index__];
            Assert.IsNotNull(globalInstance);
            Assert.IsNull(ex);

            RtScriptClass rtPayload = (RtScriptClass)globalInstance;

            var o = rtPayload.ReadSlot(0);
            Assert.AreEqual(NaNBoxing.BoxType.Sbyte, o.ValueType);
            Assert.AreEqual(2, o.SByteValue);

            var p = rtPayload.ReadSlot(1);
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
