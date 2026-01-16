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
	public class Test015 : CodeTestBase
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
if (({} + function(){return 1}) !== ({}.toString() + function(){return 1}.toString())) {
  throw new Test262Error('#1: ({} + function(){return 1}) === ({}.toString() + function(){return 1}.toString()). Actual: ' + (({} + function(){return 1})));
}

//CHECK#2
if ((function(){return 1} + {}) !== (function(){return 1}.toString() + {}.toString())) {
  throw new Test262Error('#2: (function(){return 1} + {}) === (function(){return 1}.toString() + {}.toString()). Actual: ' + ((function(){return 1} + {})));
}

//CHECK#3
if ((function(){return 1} + function(){return 1}) !== (function(){return 1}.toString() + function(){return 1}.toString())) {
  throw new Test262Error('#3: (function(){return 1} + function(){return 1}) === (function(){return 1}.toString() + function(){return 1}.toString()). Actual: ' + ((function(){return 1} + function(){return 1})));
}

//CHECK#4
if (({} + {}) !== ({}.toString() + {}.toString())) {
  throw new Test262Error('#4: ({} + {}) === ({}.toString() + {}.toString()). Actual: ' + (({} + {})));
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
