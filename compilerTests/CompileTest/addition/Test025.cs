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
	public class Test025 : CodeTestBase
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
if (1 + -0 !== 1 ) {  
  throw new Test262Error('#1: 1 + -0 === 1. Actual: ' + (1 + -0));
}

//CHECK#2
if (1 + 0 !== 1 ) {  
  throw new Test262Error('#2: 1 + 0 === 1. Actual: ' + (1 + 0));
} 

//CHECK#3
if (-0 + 1 !== 1 ) {  
  throw new Test262Error('#3: -0 + 1 === 1. Actual: ' + (-0 + 1));
}

//CHECK#4
if (0 + 1 !== 1 ) {  
  throw new Test262Error('#4: 0 + 1 === 1. Actual: ' + (0 + 1));
} 

//CHECK#5
if (Number.MAX_VALUE + -0 !== Number.MAX_VALUE ) {  
  throw new Test262Error('#5: Number.MAX_VALUE + -0 === Number.MAX_VALUE. Actual: ' + (Number.MAX_VALUE + -0));
}

//CHECK#6
if (Number.MAX_VALUE + 0 !== Number.MAX_VALUE ) {  
  throw new Test262Error('#6: Number.MAX_VALUE + 0 === Number.MAX_VALUE. Actual: ' + (Number.MAX_VALUE + 0));
} 

//CHECK#7
if (-0 + Number.MIN_VALUE !== Number.MIN_VALUE ) {  
  throw new Test262Error('#7: -0 + Number.MIN_VALUE === Number.MIN_VALUE. Actual: ' + (-0 + Number.MIN_VALUE));
}

//CHECK#8
if (0 + Number.MIN_VALUE !== Number.MIN_VALUE ) {  
  throw new Test262Error('#8: 0 + Number.MIN_VALUE === Number.MIN_VALUE. Actual: ' + (0 + Number.MIN_VALUE));
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
