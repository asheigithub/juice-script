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
	public class Test011 : CodeTestBase
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

class Test262Error extends Error
{
	public function Test262Error(t)
	{
		super(t);
	}
}


if (isNaN(true + undefined) !== true) {
  throw new Test262Error('#1: true + undefined === Not-a-Number. Actual: ' + (true + undefined));
}

//CHECK#2
if (isNaN(undefined + true) !== true) {
  throw new Test262Error('#2: undefined + true === Not-a-Number. Actual: ' + (undefined + true));
}

//CHECK#3
if (isNaN(new Boolean(true) + undefined) !== true) {
  throw new Test262Error('#3: new Boolean(true) + undefined === Not-a-Number. Actual: ' + (new Boolean(true) + undefined));
}

//CHECK#4
if (isNaN(undefined + new Boolean(true)) !== true) {
  throw new Test262Error('#4: undefined + new Boolean(true) === Not-a-Number. Actual: ' + (undefined + new Boolean(true)));
}
trace(""OK"");
"
				}


				);


			return project;
		}

		protected override void TestIsPass(Player player, PlayerException ex)
		{
			//test 262中 catch块中的function能够提升到外部，我们这里就和普通变量一样阻止拉倒



			Assert.IsNull(ex);

		
			player.ForceGC();

			
			string output = ((StringPrint)player.Print).GetOutput();

			Assert.AreEqual("OK\r\n", output);

		}

		[TestMethod]
		public void Test()
		{
			Run();
		}
	}
}
