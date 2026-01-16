using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.delete
{
	[TestClass]
	public class Test010: CodeTestBase
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


var __color__map = {};

//////////////////////////////////////////////////////////////////////////////
//CHECK#1
if (delete __color__map.red !== true) {
  throw new Test262Error(
    '#1: var __color__map = {}; delete __color__map.red === true. Actual: ' +
    delete __color__map.red
  );
}
//
//////////////////////////////////////////////////////////////////////////////

//////////////////////////////////////////////////////////////////////////////
//CHECK#2
if (delete __color__map['green'] !== true) {
  throw new Test262Error(
    '#2: var __color__map = {}; delete __color__map[""green""] === true. Actual: ' +
    delete __color__map['green']
  );
}
//
//////////////////////////////////////////////////////////////////////////////

//////////////////////////////////////////////////////////////////////////////
//CHECK#3
var blue = 1;
if (delete __color__map[blue] !== true) {
  throw new Test262Error(
    '#3: var __color__map = {}; var blue = 1; delete __color__map[blue] === true. Actual: ' +
    delete __color__map[blue]
  );
}
//
//////////////////////////////////////////////////////////////////////////////


trace('OK');

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
