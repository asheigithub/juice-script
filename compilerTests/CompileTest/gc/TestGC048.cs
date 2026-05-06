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
    public class TestGC048 : CodeTestBase
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

var o = { valueOf:function ():int 
{
	trace(""valueof"");
	return 4;
} };


(
function () 
{
	var b = new Array(1,2,3);
	
	var d = b;
	
	var c = b;
	
	c =(function () 
	{	
		return new Array(4);
	})();
	
	
	trace( b === d, d.length, c.length );
	
	
}
)();








"
				}


                );


            return project;

        }

        protected override void TestIsPass(Player player, PlayerException ex)
        {
           
            Assert.IsNull(ex);

            Assert.AreEqual(0, player.Context.GC.Heap.DumpHeap()
                .Where(o => o.Kind == RtHeapTypeKind.ARRAY).Count());

			
			player.ForceGC();

			Assert.AreEqual(0, player.Context.GC.Heap.DumpHeap()
			   .Where(o => o.Kind == RtHeapTypeKind.ARRAY).Count());


			var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Main");
            Assert.IsNotNull(global);
            var globalInstance = player.Context.GC.Heap[global.__global_index__];
            Assert.IsNotNull(globalInstance);
            Assert.IsNull(ex);

			Assert.AreEqual("true 3 4\r\n", ((StringPrint)player.Print).output.ToString());

		}

		[TestMethod]
        public void Test()
        {
            Run();
        }

    }
}
