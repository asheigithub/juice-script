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
	public class Test028 : CodeTestBase
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
if ( ({valueOf: function() {return 1}}) + 1 !== 2) {
  throw new Test262Error('#1: {valueOf: function() {return 1}} + 1 === 2. Actual: ' + (({valueOf: function() {return 1}}) + 1));
}

//CHECK#2
if (({valueOf: function() {return 1}, toString: function() {return 0}}) + 1 !== 2) {
  throw new Test262Error('#2: {valueOf: function() {return 1}, toString: function() {return 0}} + 1 === 2. Actual: ' + (({valueOf: function() {return 1}, toString: function() {return 0}}) + 1));
}

//CHECK#3
if (({valueOf: function() {return 1}, toString: function() {return {}}}) + 1 !== 2) {
  throw new Test262Error('#3: {valueOf: function() {return 1}, toString: function() {return {}}} + 1 === 2. Actual: ' + (({valueOf: function() {return 1}, toString: function() {return {}}}) + 1));
}

//CHECK#4
try {
  if (({valueOf: function() {return 1}, toString: function() {throw ""error""}}) + 1 !== 2) {
    throw new Test262Error('#4.1: {valueOf: function() {return 1}, toString: function() {throw ""error""}} + 1 === 2. Actual: ' + (({valueOf: function() {return 1}, toString: function() {throw ""error""}}) + 1));
  }
}
catch (e) {
  if (e === ""error"") {
    throw new Test262Error('#4.2: {valueOf: function() {return 1}, toString: function() {throw ""error""}} + 1 not throw ""error""');
  } else {
    throw new Test262Error('#4.3: {valueOf: function() {return 1}, toString: function() {throw ""error""}} + 1 not throw Error. Actual: ' + (e));
  }
}

//CHECK#5
if (1 + {toString: function() {return 1}} !== 2) {
  throw new Test262Error('#5: 1 + {toString: function() {return 1}} === 2. Actual: ' + (1 + {toString: function() {return 1}}));
}

//CHECK#6
if (1 + {valueOf: function() {return {}}, toString: function() {return 1}} !== 2) {
  throw new Test262Error('#6: 1 + {valueOf: function() {return {}}, toString: function() {return 1}} === 2. Actual: ' + (1 + {valueOf: function() {return {}}, toString: function() {return 1}}));
}

//CHECK#7
try {
  1 + {valueOf: function() {throw ""error""}, toString: function() {return 1}};
  throw new Test262Error('#7.1: 1 + {valueOf: function() {throw ""error""}, toString: function() {return 1}} throw ""error"". Actual: ' + (1 + {valueOf: function() {throw ""error""}, toString: function() {return 1}}));
}  
catch (e) {
  if (e !== ""error"") {
    throw new Test262Error('#7.2: 1 + {valueOf: function() {throw ""error""}, toString: function() {return 1}} throw ""error"". Actual: ' + (e));
  } 
}

//CHECK#8
try {
  1 + {valueOf: function() {return {}}, toString: function() {return {}}};
  throw new Test262Error('#8.1: 1 + {valueOf: function() {return {}}, toString: function() {return {}}} throw TypeError. Actual: ' + (1 + {valueOf: function() {return {}}, toString: function() {return {}}}));
}  
catch (e) {
  //if ((e instanceof TypeError) !== true) {
    //throw new Test262Error('#8.2: 1 + {valueOf: function() {return {}}, toString: function() {return {}}} throw TypeError. Actual: ' + (e));
  //} 
  trace(e.name);
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

			Assert.AreEqual("TypeError\r\nOK\r\n", output);

		}

		[TestMethod]
		public void Test()
		{
			Run();
		}
	}
}
