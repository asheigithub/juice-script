using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.equal
{
	[TestClass]
	public class Test007 : CodeTestBase
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
if ((Number.NaN == true) !== false) {
  throw new Test262Error('#1: (NaN == true) === false');
}

//CHECK#2
if ((Number.NaN == 1) !== false) {
  throw new Test262Error('#2: (NaN == 1) === false');
}

//CHECK#3
if ((Number.NaN == Number.NaN) !== false) {
  throw new Test262Error('#3: (NaN == NaN) === false');
}

//CHECK#4
if ((Number.NaN == Number.POSITIVE_INFINITY) !== false) {
  throw new Test262Error('#4: (NaN == +Infinity) === false');
}

//CHECK#5
if ((Number.NaN == Number.NEGATIVE_INFINITY) !== false) {
  throw new Test262Error('#5: (NaN == -Infinity) === false');
}

//CHECK#6
if ((Number.NaN == Number.MAX_VALUE) !== false) {
  throw new Test262Error('#6: (NaN == Number.MAX_VALUE) === false');
}

//CHECK#7
if ((Number.NaN == Number.MIN_VALUE) !== false) {
  throw new Test262Error('#7: (NaN == Number.MIN_VALUE) === false');
}

//CHECK#8
if ((Number.NaN == ""string"") !== false) {
  throw new Test262Error('#8: (NaN == ""string"") === false');
}

//CHECK#9
if ((Number.NaN == new Object()) !== false) {
  throw new Test262Error('#9: (NaN == new Object()) === false');
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
