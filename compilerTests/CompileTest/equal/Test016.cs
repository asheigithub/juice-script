using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.equal
{
	[TestClass]
	public class Test016 : CodeTestBase
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
if ((false == undefined) !== false) {
  throw new Test262Error('#1: (false == undefined) === false');
}

//CHECK#2
if ((Number.NaN == undefined) !== false) {
  throw new Test262Error('#2: (Number.NaN == undefined) === false');
}

//CHECK#3
if ((""undefined"" == undefined) !== false) {
  throw new Test262Error('#3: (""undefined"" == undefined) === false');
}

//CHECK#4
if (({} == undefined) !== false) {
  throw new Test262Error('#4: ({} == undefined) === false');
}

//CHECK#5
if ((false == null) !== false) {
  throw new Test262Error('#5: (false == null) === false');
}

//CHECK#6
if ((0 == null) !== false) {
  throw new Test262Error('#6: (0 == null) === false');
}

//CHECK#7
if ((""null"" == null) !== false) {
  throw new Test262Error('#7: (""null"" == null) === false');
}

//CHECK#8
if (({} == null) !== false) {
  throw new Test262Error('#8: ({} == null) === false');
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
