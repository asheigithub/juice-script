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
	public class Test017 : CodeTestBase
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
if (true + ""1"" !== ""true1"") {
  throw new Test262Error('#1: true + ""1"" === ""true1"". Actual: ' + (true + ""1""));
}

//CHECK#2
if (""1"" + true !== ""1true"") {
  throw new Test262Error('#2: ""1"" + true === ""1true"". Actual: ' + (""1"" + true));
}

//CHECK#3
if (new Boolean(true) + ""1"" !== ""true1"") {
  throw new Test262Error('#3: new Boolean(true) + ""1"" === ""true1"". Actual: ' + (new Boolean(true) + ""1""));
}

//CHECK#4
if (""1"" + new Boolean(true) !== ""1true"") {
  throw new Test262Error('#4: ""1"" + new Boolean(true) === ""1true"". Actual: ' + (""1"" + new Boolean(true)));
}

//CHECK#5
if (true + new String(""1"") !== ""true1"") {
  throw new Test262Error('#5: true + new String(""1"") === ""true1"". Actual: ' + (true + new String(""1"")));
}

//CHECK#6
if (new String(""1"") + true !== ""1true"") {
  throw new Test262Error('#6: new String(""1"") + true === ""1true"". Actual: ' + (new String(""1"") + true));
}

//CHECK#7
if (new Boolean(true) + new String(""1"") !== ""true1"") {
  throw new Test262Error('#7: new Boolean(true) + new String(""1"") === ""true1"". Actual: ' + (new Boolean(true) + new String(""1"")));
}

//CHECK#8
if (new String(""1"") + new Boolean(true) !== ""1true"") {
  throw new Test262Error('#8: new String(""1"") + new Boolean(true) === ""1true"". Actual: ' + (new String(""1"") + new Boolean(true)));
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
