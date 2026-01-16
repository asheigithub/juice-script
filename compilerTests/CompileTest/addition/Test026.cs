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
	public class Test026 : CodeTestBase
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

//CHECK#1
if (-Number.MIN_VALUE + Number.MIN_VALUE !== +0) {  
  throw new Test262Error('#1.1: -Number.MIN_VALUE + Number.MIN_VALUE === 0. Actual: ' + (-Number.MIN_VALUE + Number.MIN_VALUE));
} else {
  if (1 / (-Number.MIN_VALUE + Number.MIN_VALUE) !== Number.POSITIVE_INFINITY) {
    throw new Test262Error('#1.2: -Number.MIN_VALUE + Number.MIN_VALUE === + 0. Actual: -0');
  }
}

//CHECK#2
if (-Number.MAX_VALUE + Number.MAX_VALUE !== +0) {  
  throw new Test262Error('#2.1: -Number.MAX_VALUE + Number.MAX_VALUE === 0. Actual: ' + (-Number.MAX_VALUE + Number.MAX_VALUE));
} else {
  if (1 / (-Number.MAX_VALUE + Number.MAX_VALUE) !== Number.POSITIVE_INFINITY) {
    throw new Test262Error('#2.2: -Number.MAX_VALUE + Number.MAX_VALUE === + 0. Actual: -0');
  }
}

//CHECK#3
if (-1 / Number.MAX_VALUE + 1 / Number.MAX_VALUE !== +0) {  
  throw new Test262Error('#3.1: -1 / Number.MAX_VALUE + 1 / Number.MAX_VALUE === 0. Actual: ' + (-1 / Number.MAX_VALUE + 1 / Number.MAX_VALUE));
} else {
  if (1 / (-1 / Number.MAX_VALUE + 1 / Number.MAX_VALUE) !== Number.POSITIVE_INFINITY) {
    throw new Test262Error('#3.2: -1 / Number.MAX_VALUE + 1 / Number.MAX_VALUE === + 0. Actual: -0');
  }
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
