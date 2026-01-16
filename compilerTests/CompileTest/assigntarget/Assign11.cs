using juicescript.runtime;
using juicescript;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using juicescript.compiler;

namespace compilerTests.CompileTest.assigntarget
{
    [TestClass]
    public sealed class Assign11 : CodeTestBase
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
	[Doc]
	/**
	 * ...
	 * @author 
	 */
	public class Main extends Sprite
	{
		protected var j = 999;
		
	}
}



var o = null;

function v():Namespace
{
	return AS3;
}


class  A extends Main
{
	var IIA = new A().j;
	
	var L = JJJ;
	const JJJ  = ""LLLL""; 
	
	function F() 
	{
		
		o = IIA;
		
	}
	
} 

class B extends A
{
	
}

new A().F();
//trace(o);

//trace(o);


"
				}


                );


            return project;
        }

        protected override void TestIsPass(Player player, PlayerException ex)
        {
			

			var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Main");
			Assert.IsNotNull(global);
			var globalInstance = player.Context.GC.Heap[global.__global_index__];
			Assert.IsNotNull(globalInstance);
			Assert.IsNotNull(ex);

			Assert.AreEqual("[Fault] exception,[Message]=Error: Stack overflow occurred.", ex.ToDebugMessage());

			player.ForceGC();
			//RtPayloadScriptClass rtPayload = (RtPayloadScriptClass)globalInstance.facility;

			//NaNBoxing o = rtPayload.ReadSlot(0);
			//Assert.AreEqual(NaNBoxing.BoxType.Short, o.ValueType);
			//Assert.AreEqual(999, o.ShortValue);
		}


        [TestMethod]
        public void Test()
        {
			Run();
        }
    }
}
