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
    public sealed class Test011 : CodeTestBase
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

Number.prototype.LL = ""Number"";
var b:Number = 3e-2; 
var a = b;
trace(a);
trace(a.LL);
trace( a[""LL""] );

try
{
trace( a.AS3::LL );
}
catch(e)
{
	trace(e.message);
}




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

                RtPayloadScriptClass rtPayload = (RtPayloadScriptClass)globalInstance.facility;

				StringPrint print = (StringPrint)player.Print;

				Assert.AreEqual("0.03\r\nNumber\r\nNumber\r\nProperty http://adobe.com/AS3/2006/builtin::LL not found on Number and there is no default value.\r\n", print.GetOutput());


				NaNBoxing a = rtPayload.ReadSlot(0);
				Assert.AreEqual( NaNBoxing.BoxType.Number, a.ValueType );
				Assert.AreEqual( 0.03, a.Number);
                
               
			}

           
        }


        [TestMethod]
        public void Test()
        {
            Run();
        }
    }
}
