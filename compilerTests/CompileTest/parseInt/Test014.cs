using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.parseInt
{
	[TestClass]
	public sealed class Test014 : CodeTestBase
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

assert.sameValue(parseInt(""1"", 2), 1, 'parseInt(""1"", 2) must return 1');
assert.sameValue(parseInt(""11"", 2), 3, 'parseInt(""11"", 2) must return 3');
assert.sameValue(parseInt(""111"", 2), 7, 'parseInt(""111"", 2) must return 7');
assert.sameValue(parseInt(""1111"", 2), 15, 'parseInt(""1111"", 2) must return 15');
assert.sameValue(parseInt(""11111"", 2), 31, 'parseInt(""11111"", 2) must return 31');
assert.sameValue(parseInt(""111111"", 2), 63, 'parseInt(""111111"", 2) must return 63');
assert.sameValue(parseInt(""1111111"", 2), 127, 'parseInt(""1111111"", 2) must return 127');
assert.sameValue(parseInt(""11111111"", 2), 255, 'parseInt(""11111111"", 2) must return 255');
assert.sameValue(parseInt(""111111111"", 2), 511, 'parseInt(""111111111"", 2) must return 511');
assert.sameValue(parseInt(""1111111111"", 2), 1023, 'parseInt(""1111111111"", 2) must return 1023');
assert.sameValue(parseInt(""11111111111"", 2), 2047, 'parseInt(""11111111111"", 2) must return 2047');
assert.sameValue(parseInt(""111111111111"", 2), 4095, 'parseInt(""111111111111"", 2) must return 4095');
assert.sameValue(parseInt(""1111111111111"", 2), 8191, 'parseInt(""1111111111111"", 2) must return 8191');
assert.sameValue(parseInt(""11111111111111"", 2), 16383, 'parseInt(""11111111111111"", 2) must return 16383');
assert.sameValue(parseInt(""111111111111111"", 2), 32767, 'parseInt(""111111111111111"", 2) must return 32767');
assert.sameValue(parseInt(""1111111111111111"", 2), 65535, 'parseInt(""1111111111111111"", 2) must return 65535');
assert.sameValue(parseInt(""11111111111111111"", 2), 131071, 'parseInt(""11111111111111111"", 2) must return 131071');
assert.sameValue(parseInt(""111111111111111111"", 2), 262143, 'parseInt(""111111111111111111"", 2) must return 262143');
assert.sameValue(parseInt(""1111111111111111111"", 2), 524287, 'parseInt(""1111111111111111111"", 2) must return 524287');
assert.sameValue(parseInt(""11111111111111111111"", 2), 1048575, 'parseInt(""11111111111111111111"", 2) must return 1048575');

trace(""OK"");





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
