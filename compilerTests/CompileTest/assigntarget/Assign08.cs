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
    public sealed class Assign08 : CodeTestBase
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
		//protected var j = A.IIA;
		
		protected var jj = 666;
		
	}
}



var o = null;

function v():Namespace
{
	return AS3;
}


class  A extends Main
{
	static var IIA = 999;
	
	var L = JJJ;
	const JJJ  = ""LLLL""; 
	
	//var b = new A().j;
	
	
	//var B = new A().jj;
	
	function F() 
	{
		//o = k.protected::[""j""];
		//o = new Main().protected::[""j""];
		
		o = new A().jj;
		
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
			player.ForceGC();

			var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Main");
			Assert.IsNotNull(global);
			var globalInstance = player.Context.GC.Heap[global.__global_index__];
			Assert.IsNotNull(globalInstance);
			Assert.IsNull(ex);

			RtScriptClass rtPayload = (RtScriptClass)globalInstance;

			NaNBoxing o = rtPayload.ReadSlot(0);
			Assert.AreEqual(NaNBoxing.BoxType.Short, o.ValueType);
			Assert.AreEqual(666, o.ShortValue);
		}


		[TestMethod]
		public void Test()
		{
			Run();
		}
    }
}
