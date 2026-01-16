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
	public class Test023 : CodeTestBase
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
if (Number.POSITIVE_INFINITY + 1 !== Number.POSITIVE_INFINITY ) {
  throw new Test262Error('#1: Infinity + 1 === Infinity. Actual: ' + (Infinity + 1));
}

//CHECK#2
if (-1 + Number.POSITIVE_INFINITY !== Number.POSITIVE_INFINITY ) {
  throw new Test262Error('#2: -1 + Infinity === Infinity. Actual: ' + (-1 + Infinity));
}

//CHECK#3
if (Number.NEGATIVE_INFINITY + 1 !== Number.NEGATIVE_INFINITY ) {
  throw new Test262Error('#3: -Infinity + 1 === -Infinity. Actual: ' + (-Infinity + 1));
}

//CHECK#4
if (-1 + Number.NEGATIVE_INFINITY !== Number.NEGATIVE_INFINITY ) {
  throw new Test262Error('#4: -1 + -Infinity === -Infinity. Actual: ' + (-1 + -Infinity));
}

//CHECK#5
if (Number.POSITIVE_INFINITY + Number.MAX_VALUE !== Number.POSITIVE_INFINITY ) {
  throw new Test262Error('#5: Infinity + Number.MAX_VALUE === Infinity. Actual: ' + (Infinity + Number.MAX_VALUE));
}

//CHECK#6
if (-Number.MAX_VALUE + Number.POSITIVE_INFINITY !== Number.POSITIVE_INFINITY ) {
  throw new Test262Error('#6: -Number.MAX_VALUE + Infinity === Infinity. Actual: ' + (-Number.MAX_VALUE + Infinity));
}

//CHECK#7
if (Number.NEGATIVE_INFINITY + Number.MAX_VALUE !== Number.NEGATIVE_INFINITY ) {
  throw new Test262Error('#7: -Infinity + Number.MAX_VALUE === -Infinity. Actual: ' + (-Infinity + Number.MAX_VALUE));
}

//CHECK#8
if (-Number.MAX_VALUE + Number.NEGATIVE_INFINITY !== Number.NEGATIVE_INFINITY ) {
  throw new Test262Error('#8: -Number.MAX_VALUE + -Infinity === -Infinity. Actual: ' + (-Number.MAX_VALUE + -Infinity));
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
