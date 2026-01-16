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
	public class Test013 : CodeTestBase
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
if (""1"" + ""1"" !== ""11"") {
  throw new Test262Error('#1: ""1"" + ""1"" === ""11"". Actual: ' + (""1"" + ""1""));
}

//CHECK#2
if (new String(""1"") + ""1"" !== ""11"") {
  throw new Test262Error('#2: new String(""1"") + ""1"" === ""11"". Actual: ' + (new String(""1"") + ""1""));
}

//CHECK#3
if (""1"" + new String(""1"") !== ""11"") {
  throw new Test262Error('#3: ""1"" + new String(""1"") === ""11"". Actual: ' + (""1"" + new String(""1"")));
}

//CHECK#4
if (new String(""1"") + new String(""1"") !== ""11"") {
  throw new Test262Error('#4: new String(""1"") + new String(""1"") === ""11"". Actual: ' + (new String(""1"") + new String(""1"")));
}

//CHECK#5
if (""x"" + ""1"" !==""x1"") {
  throw new Test262Error('#5: ""x"" + ""1"" === ""x1"". Actual: ' + (""x"" + ""1""));
}

//CHECK#6
if (""1"" + ""x"" !== ""1x"") {
  throw new Test262Error('#6: ""1"" + ""x"" === ""1x"". Actual: ' + (""1"" + ""x""));
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
