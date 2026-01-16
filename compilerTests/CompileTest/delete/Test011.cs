using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.delete
{
	[TestClass]
	public class Test011: CodeTestBase
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


function Palette() {};
Palette.prototype = {
  red: 0xff0000,
  green: 0x00ff00
};
var __palette = new Palette();

//////////////////////////////////////////////////////////////////////////////
//CHECK#1
if (__palette.red !== 0xff0000) {
  throw new Test262Error(
    '#1: function Palette(){}; Palette.prototype = {red:0xFF0000, green:0x00FF00}; __palette = new Palette; __palette.red === 0xFF0000. Actual: ' +
    __palette.red
  );
}
//
//////////////////////////////////////////////////////////////////////////////

//////////////////////////////////////////////////////////////////////////////
//CHECK#2
if (delete __palette.red !== true) {
  throw new Test262Error(
    '#2 function Palette(){}; Palette.prototype = {red:0xFF0000, green:0x00FF00}; __palette = new Palette; delete __palette.red === true. Actual: ' +
    delete __palette.red
  );
}
//
//////////////////////////////////////////////////////////////////////////////

//////////////////////////////////////////////////////////////////////////////
//CHECK#3
if (__palette.red !== 0xff0000) {
  throw new Test262Error(
    '#3: function Palette(){}; Palette.prototype = {red:0xFF0000, green:0x00FF00}; __palette = new Palette; __palette.red === 0xFF0000. Actual: ' +
    __palette.red
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
