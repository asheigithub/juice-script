using juicescript;
using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.addition
{
	[TestClass]
	public sealed class Test008 : CodeTestBase
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

 

if (true + 1 !== 2) {
  throw new Error('#1: true + 1 === 2. Actual: ' + (true + 1));
}



//CHECK#2
if (1 + true !== 2) {
  throw new Error('#2: 1 + true === 2. Actual: ' + (1 + true));
}

//CHECK#3
if (new Boolean(true) + 1 !== 2) {
  throw new Error('#3: new Boolean(true) + 1 === 2. Actual: ' + (new Boolean(true) + 1));
}

//CHECK#4
if (1 + new Boolean(true) !== 2) {
  throw new Error('#4: 1 + new Boolean(true) === 2. Actual: ' + (1 + new Boolean(true)));
}

//CHECK#5
if (true + new Number(1) !== 2) {
  throw new Error('#5: true + new Number(1) === 2. Actual: ' + (true + new Number(1)));
}

//CHECK#6
if (new Number(1) + true !== 2) {
  throw new Error('#6: new Number(1) + true === 2. Actual: ' + (new Number(1) + true));
}

//CHECK#7
if (new Boolean(true) + new Number(1) !== 2) {
  throw new Error('#7: new Boolean(true) + new Number(1) === 2. Actual: ' + (new Boolean(true) + new Number(1)));
}

//CHECK#8
if (new Number(1) + new Boolean(true) !== 2) {
  throw new Error('#8: new Number(1) + new Boolean(true) === 2. Actual: ' + (new Number(1) + new Boolean(true)));
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

				RtScriptClass rtPayload = (RtScriptClass)globalInstance.facility;

				StringPrint print = (StringPrint)player.Print;

				Assert.AreEqual("", print.GetOutput());

			}


		}


		[TestMethod]
		public void Test()
		{
			Run();
		}
	}

}
