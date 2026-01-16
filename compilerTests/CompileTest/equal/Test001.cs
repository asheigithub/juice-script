using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.equal
{
	[TestClass]
	public class Test001 : CodeTestBase
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
if ((1 == 1) !== true) {
  throw new Test262Error('#1: (1 == 1) === true');
}

//CHECK#2
var x = 1;
if ((x == 1) !== true) {
  throw new Test262Error('#2: var x = 1; (x == 1) === true');
}

//CHECK#3
var y = 1;
if ((1 == y) !== true) {
  throw new Test262Error('#3: var y = 1; (1 == y) === true');
}

//CHECK#4
var x = 1;
var y = 1;
if ((x == y) !== true) {
  throw new Test262Error('#4: var x = 1; var y = 1; (x == y) === true');
}

//CHECK#5
var objectx = new Object();
var objecty = new Object();
objectx.prop = 1;
objecty.prop = 1;
if ((objectx.prop == objecty.prop) !== true) {
  throw new Test262Error('#5: var objectx = new Object(); var objecty = new Object(); objectx.prop = 1; objecty.prop = 1; (objectx.prop == objecty.prop) === true');
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
