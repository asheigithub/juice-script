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
    public class TestGC058 : CodeTestBase
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

[struct]
final class o
{
	public var i:int;
}

[struct]
final class p
{
	public var i:int;
	
}


(
function () 
{
	var b = arguments;
	
	var d = b;
	
	trace(d.length,d);
	
	d[7] = 9;
	
	trace(d[7],d[8]);
	
	var c = b;
	
	c =(function () 
	{	
		return new Array(4);
	})();
	
	c.length = 8;
	trace(c);
	
	
	b[0] = 0;
	
	b[5] = new p();
	
	//b[5] = 9;
	
	trace( b === d, d.length, d, b.length );

	trace( delete b[8]);
	
	trace(b.length);
	
}
)(7,8,9,10, 11,new o());

"
				}


                );


            return project;

        }

        protected override void TestIsPass(Player player, PlayerException ex)
        {
           
            Assert.IsNull(ex);

            Assert.AreEqual(1, player.Context.GC.Heap.DumpHeap()
                .Where(o => o.Kind == RtHeapTypeKind.ARRAY).Count());

			
			player.ForceGC();

			Assert.AreEqual(0, player.Context.GC.Heap.DumpHeap()
			   .Where(o => o.Kind == RtHeapTypeKind.ARRAY).Count());


			var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Main");
            Assert.IsNotNull(global);
            var globalInstance = player.Context.GC.Heap[global.__global_index__];
            Assert.IsNotNull(globalInstance);
            Assert.IsNull(ex);

			Assert.AreEqual("6 7,8,9,10,11,[object o]\r\n9 undefined\r\n,,,,,,,\r\ntrue 8 0,8,9,10,11,[object p],,9 8\r\ntrue\r\n8\r\n", ((StringPrint)player.Print).output.ToString());

		}

		[TestMethod]
        public void Test()
        {
            Run();
        }

    }
}
