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
    public class TestGC066 : CodeTestBase
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


var J;

class A
{
 var K = 1;
 public function Test(f)
 {
  f(this);
  trace(K);
 }
}

class B
{

}

function runTest():void {

 var a = new A();

 a.Test( function (c):void
 {
	
	 
	J = a;
	
	trace(c === a);
	
	 a = new B();
	 
 } );

}
runTest();


"
				}


                );


            return project;

        }

        protected override void TestIsPass(Player player, PlayerException ex)
        {
           
            Assert.IsNull(ex);

            Assert.AreEqual(0, player.Context.GC.Heap.DumpHeap()
                .Where(o => o.TypeKind == RtHeapTypeKind.VECTOR).Count());

			
			player.ForceGC();

			var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Main");
            Assert.IsNotNull(global);
            var globalInstance = player.Context.GC.Heap[global.__global_index__];
            Assert.IsNotNull(globalInstance);
            Assert.IsNull(ex);

			Assert.AreEqual("true\r\n1\r\n", ((StringPrint)player.Print).output.ToString());

		}

		[TestMethod]
        public void Test()
        {
            Run();
        }

    }
}
