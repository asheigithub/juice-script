using juicescript.compiler;
using juicescript.compiler.parse;
using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.switch_
{

	[TestClass]
	public sealed class Test011 : CodeTestBase
	{
		protected override TestCodeProject LoadProject()
		{
			TestCodeProject project = new TestCodeProject();

			project.libs = [Juice_GlobalSwc];

			project.testCodes = new List<TestCodeFile>();

			project.testCodes.Add(
				new TestCodeFile()
				{
					Path = "BaseM.as",
					Code = @"
package ns1 
{
	import flash.display.Sprite;
	/**
	 * ...
	 * @author 
	 */
	public class BaseM extends Sprite
	{
		
		public static const FFF = 6666;
		protected static const VVV = ""abcd"";
		public function BaseM() 
		{
			
		}
		
	}

}


"
				}
				);

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
	public class Main extends BaseM
	{
		public var v;
	}
	
}



import adobe.utils.CustomActions;
import flash.sampler.NewObjectSample;
import flash.utils.ByteArray;
import flash.utils.Dictionary;


class Test262Error extends Error
{
	public function Test262Error(t)
	{
		super(t);
	}
}

function assert(mustBeTrue, message) {
  if (mustBeTrue === true) {
    return;
  }

  if (message === undefined) {
    message = 'Expected true but got ' + assert._toString(mustBeTrue);
  }
  throw new Test262Error(message);
}

assert._toString = function (v:String) 
{
	return v;
}

assert._isSameValue = function (a, b) {
  if (a === b) {
    // Handle +/-0 vs. -/+0
    return a !== 0 || 1 / a === 1 / b;
  }

  // Handle NaN vs. NaN
  return a !== a && b !== b;
};

assert.sameValue = function (actual, expected, message) {
  try {
    if (assert._isSameValue(actual, expected)) {
      return;
    }
  } catch (error) {
    throw new Test262Error(message + ' (_isSameValue operation threw) ' + error);
    return;
  }

  if (message === undefined) {
    message = '';
  } else {
    message += ' ';
  }

  message += 'Expected SameValue(«' + assert._toString(actual) + '», «' + assert._toString(expected) + '») to be true';

  throw new Test262Error(message);
};

assert.notSameValue = function (actual, unexpected, message) {
  if (!assert._isSameValue(actual, unexpected)) {
    return;
  }

  if (message === undefined) {
    message = '';
  } else {
    message += ' ';
  }

  message += 'Expected SameValue(«' + assert._toString(actual) + '», «' + assert._toString(unexpected) + '») to be false';

  throw new Test262Error(message);
};

assert.throws = function (expectedErrorConstructor, func, message) {
  var expectedName, actualName;
  if (typeof func !== ""function"") {
    throw new Test262Error('assert.throws requires two arguments: the error constructor ' +
      'and a function to run');
    return;
  }
  if (message === undefined) {
    message = '';
  } else {
    message += ' ';
  }

  try {
    func();
  } catch (thrown) {	  
	  trace(thrown.name); 
    if (typeof thrown !== 'object' || thrown === null) {
      message += 'Thrown value was not an object!';
      throw new Test262Error(message);
    } else if (thrown.constructor !== expectedErrorConstructor) {
      expectedName = expectedErrorConstructor.name;
      actualName = thrown.constructor.name;
      if (expectedName === actualName) {
        message += 'Expected a ' + expectedName + ' but got a different error constructor with the same name';
      } else {
        message += 'Expected a ' + expectedName + ' but got a ' + actualName;
      }
      throw new Test262Error(message);
    }
    return;
  }

  message += 'Expected a ' + expectedErrorConstructor.name + ' to be thrown but no exception was thrown at all';
  throw new Test262Error(message);
};


var x = ""2"";

function SwitchTest(value){
  var result = 0;
  
  switch(value) {
    case 0:
      result += 2;
    case '1':
      result += 4;
      break;
    case ""two"":
      result += 8;
    case 3:
      result += 16;
    default:
      result += 32;
      break;
    case 4:
      result += 64;
      break;
    case x:
      result += 128;
      break;
    case 0:
      result += 256;
    case 1:
      result += 512;
  }
  
  return result;
}
        
if(!(SwitchTest(0) === 6)){
  throw new Test262Error(""#1: SwitchTest(0) === 6. Actual:  SwitchTest(0) ===""+ SwitchTest(0)  );
}

if(!(SwitchTest(1) === 512)){
  throw new Test262Error(""#2: SwitchTest(1) === 512. Actual:  SwitchTest(1) ===""+ SwitchTest(1)  );
}

if(!(SwitchTest(2) === 32)){
  throw new Test262Error(""#3: SwitchTest(2) === 32. Actual:  SwitchTest(2) ===""+ SwitchTest(2)  );
}

if(!(SwitchTest(3) === 48)){
  throw new Test262Error(""#4: SwitchTest(3) === 48. Actual:  SwitchTest(3) ===""+ SwitchTest(3)  );
}

if(!(SwitchTest(4) === 64)){
  throw new Test262Error(""#5: SwitchTest(4) === 64. Actual:  SwitchTest(4) ===""+ SwitchTest(4)  );
}

if(!(SwitchTest(true) === 32)){
  throw new Test262Error(""#6: SwitchTest(true) === 32. Actual:  SwitchTest(true) ===""+ SwitchTest(true)  );
}

if(!(SwitchTest(false) === 32)){
  throw new Test262Error(""#7: SwitchTest(false) === 32. Actual:  SwitchTest(false) ===""+ SwitchTest(false)  );
}

if(!(SwitchTest(null) === 32)){
  throw new Test262Error(""#8: SwitchTest(null) === 32. Actual:  SwitchTest(null) ===""+ SwitchTest(null)  );
}

if(!(SwitchTest(void 0) === 32)){
  throw new Test262Error(""#9: SwitchTest(void 0) === 32. Actual:  SwitchTest(void 0) ===""+ SwitchTest(void 0)  );
}

if(!(SwitchTest('0') === 32)){
  throw new Test262Error(""#10: SwitchTest('0') === 32. Actual:  SwitchTest('0') ===""+ SwitchTest('0')  );
}

if(!(SwitchTest(x) === 128)){
  throw new Test262Error(""#10: SwitchTest(x) === 128. Actual:  SwitchTest(x) ===""+ SwitchTest(x)  );
}


trace('OK');

"
				}


				);


			return project;
		}

		protected override void TestIsPass(Player player, PlayerException ex)
		{
			player.ForceGC();
			{
				var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Main");
				Assert.IsNotNull(global);
				var globalInstance = player.Context.GC.Heap[global.__global_index__];
				Assert.IsNotNull(globalInstance);
				Assert.IsNull(ex);

				RtPayloadScriptClass rtPayload = (RtPayloadScriptClass)globalInstance.facility;

				StringPrint print = (StringPrint)player.Print;

				Assert.AreEqual("OK\r\n", print.GetOutput());

			}

		}


		[TestMethod]
		public void Test()
		{
			
			Run();
			
		}
	}


}
