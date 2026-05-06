using juicescript;
using juicescript.ABC;
using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.proto
{
    [TestClass]
    public sealed class Test019 : CodeTestBase
    {
        protected override TestCodeProject LoadProject()
        {
            TestCodeProject project = new TestCodeProject();

            project.libs = [Juice_GlobalSwc];

            project.testCodes = new List<TestCodeFile>();
            
            project.testCodes.Add(
                new TestCodeFile()
                {
                    Path = "BaseM.as",
                    Code = @"
package ns1 
{
	import flash.display.Sprite;
	/**
	 * ...
	 * @author 
	 */
	public class BaseM extends Sprite
	{
		
		public static const FFF = 6666;
		protected static const VVV = ""abcd"";
		public function BaseM() 
		{
			
		}
		
	}

}


"
				}
                );

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
	public class Main extends BaseM
	{
		
	}
	
}

	
function cc(i=0)
{
	this.a = 1;	
	
	this.b = this.LLL;
	
}

cc.prototype.LLL = 99;

var o1 = new cc();

cc.prototype = null;
var o2 = new cc();

trace(
o1.a,
o1.b);
trace(o2.a, o2.b);




"
				}


                );


            return project;
        }

        protected override void TestIsPass(Player player, PlayerException ex)
        {
           player.ForceGC();
            {
                var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Main");
                Assert.IsNotNull(global);
                var globalInstance = player.Context.GC.Heap[global.__global_index__];
                Assert.IsNotNull(globalInstance);
                Assert.IsNull(ex);

				
				Assert.AreEqual(1 + 1 , player.Context.GC.Heap.DumpHeap().Where(h => h.TypeKind == RtHeapTypeKind.CLOSURE && !((ASMethodBody)h.Type).Method.__is_buildin_proto).Count());


                RtScriptClass rtPayload = (RtScriptClass)globalInstance;

				StringPrint print = (StringPrint)player.Print;

				Assert.AreEqual("1 99\r\n1 undefined\r\n", print.GetOutput());

			
			}

           
        }


        [TestMethod]
        public void Test()
        {
            Run();
        }
    }
}
