using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.equal
{
	[TestClass]
	public class Test008 : CodeTestBase
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
if ((true == Number.NaN) !== false) {
  throw new Test262Error('#1: (true == NaN) === false');
}

//CHECK#2
if ((-1 == Number.NaN) !== false) {
  throw new Test262Error('#2: (-1 == NaN) === false');
}

//CHECK#3
if ((Number.NaN == Number.NaN) !== false) {
  throw new Test262Error('#3: (NaN == NaN) === false');
}

//CHECK#4
if ((Number.POSITIVE_INFINITY == Number.NaN) !== false) {
  throw new Test262Error('#4: (+Infinity == NaN) === false');
}

//CHECK#5
if ((Number.NEGATIVE_INFINITY == Number.NaN) !== false) {
  throw new Test262Error('#5: (-Infinity == NaN) === false');
}

//CHECK#6
if ((Number.MAX_VALUE == Number.NaN) !== false) {
  throw new Test262Error('#6: (Number.MAX_VALUE == NaN) === false');
}

//CHECK#7
if ((Number.MIN_VALUE == Number.NaN) !== false) {
  throw new Test262Error('#7: (Number.MIN_VALUE == NaN) === false');
}

//CHECK#8
if ((""string"" == Number.NaN) !== false) {
  throw new Test262Error('#8: (""string"" == NaN) === false');
}

//CHECK#9
if ((new Object() == Number.NaN) !== false) {
  throw new Test262Error('#9: (new Object() == NaN) === false');
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
