using juicescript.runtime;
using juicescript;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.function_code
{
    [TestClass]
    public sealed class Test001 : CodeTestBase
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


var initialBV, currentBV, varBinding;

(function() {
  
  {
    function f() {

		initialBV = f; 
		//f = 123;

		currentBV = f; 

		return 'decl'; 
		
	}
  }

  varBinding = f;

  f();
}());

trace(initialBV(), currentBV, varBinding());


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

				Assert.AreEqual("decl function Function() {} decl\r\n", print.GetOutput());


                NaNBoxing initialBV = rtPayload.ReadSlot(0);
				Assert.AreEqual( NaNBoxing.BoxType.HeapPtr, initialBV.ValueType );
				
                NaNBoxing currentBV = rtPayload.ReadSlot(1);
				Assert.AreEqual(NaNBoxing.BoxType.HeapPtr, currentBV.ValueType);

                NaNBoxing varBinding = rtPayload.ReadSlot(2);
				Assert.AreEqual(NaNBoxing.BoxType.HeapPtr, varBinding.ValueType);

                Assert.AreEqual(initialBV.Raw, currentBV.Raw);
                Assert.AreEqual(initialBV.Raw, varBinding.Raw);

                
			}

           
        }


        [TestMethod]
        public void Test()
        {
            Run();
        }
    }
}
