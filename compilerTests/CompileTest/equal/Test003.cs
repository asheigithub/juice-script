using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.equal
{
	[TestClass]
	public class Test003 : CodeTestBase
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
var x = function () { throw ""x""; };
var y = function () { throw ""y""; };
try {
   x() == y();
   throw new Test262Error('#1.1: var x = function () { throw ""x""; }; var y = function () { throw ""y""; }; x() == y() throw ""x"". Actual: ' + (x() == y()));
} catch (e) {
	
   if (e === ""y"") {
     throw new Test262Error('#1.2: First expression is evaluated first, and then second expression');
   } else {
     if (e !== ""x"") {
       throw new Test262Error('#1.3: var x = function () { throw ""x""; }; var y = function () { throw ""y""; }; x() == y() throw ""x"". Actual: ' + (e));
     }
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
