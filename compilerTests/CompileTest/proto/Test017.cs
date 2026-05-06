using juicescript.runtime;
using juicescript;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.proto
{
    [TestClass]
    public sealed class Test017 : CodeTestBase
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
		public var v;
	}
	
}

var a = new Array();

trace(a[""33""]);


var b = """";
b.constructor.constructor.prototype.OK = ""ok"";
trace(String[""OK""]);

trace(int[""OK""]);

trace(uint[""OKK""]);


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

                RtScriptClass rtPayload = (RtScriptClass)globalInstance.facility;

				StringPrint print = (StringPrint)player.Print;

				Assert.AreEqual("undefined\r\nok\r\nok\r\nundefined\r\n", print.GetOutput());

			
			}

           
        }


        [TestMethod]
        public void Test()
        {
            Run();
        }
    }
}
