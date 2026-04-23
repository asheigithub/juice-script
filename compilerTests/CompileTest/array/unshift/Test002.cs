using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.array.unshift
{
	[TestClass]
	public sealed class Test002 : CodeTestBase
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



class Test262Error extends Error
{
	var a;
	public function Test262Error(t=undefined)
	{
		super(t);
	}
}

function assert(mustBeTrue, message = undefined) {
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


var x = [];
if (x.length !== 0) {
  throw new Test262Error('#1: x = []; x.length === 0. Actual: ' + (x.length));
}

x[0] = 0;
var unshift = x.unshift(true, Number.POSITIVE_INFINITY, ""NaN"", ""1"", -1);
if (unshift !== 6) {
  throw new Test262Error('#2: x = []; x[0] = 0; x.unshift(true, Number.POSITIVE_INFINITY, ""NaN"", ""1"", -1) === 6. Actual: ' + (unshift));
}

if (x[5] !== 0) {
  throw new Test262Error('#3: x = []; x[0] = 0; x.unshift(true, Number.POSITIVE_INFINITY, ""NaN"", ""1"", -1); x[5] === 0. Actual: ' + (x[5]));
}

if (x[0] !== true) {
  throw new Test262Error('#4: x = []; x[0] = 0; x.unshift(true, Number.POSITIVE_INFINITY, ""NaN"", ""1"", -1); x[0] === true. Actual: ' + (x[0]));
}

if (x[1] !== Number.POSITIVE_INFINITY) {
  throw new Test262Error('#5: x = []; x[0] = 0; x.unshift(true, Number.POSITIVE_INFINITY, ""NaN"", ""1"", -1); x[1] === Number.POSITIVE_INFINITY. Actual: ' + (x[1]));
}

if (x[2] !== ""NaN"") {
  throw new Test262Error('#6: x = []; x[0] = 0; x.unshift(true, Number.POSITIVE_INFINITY, ""NaN"", ""1"", -1); x[2] === ""NaN"". Actual: ' + (x[2]));
}

if (x[3] !== ""1"") {
  throw new Test262Error('#7: x = []; x[0] = 0; x.unshift(true, Number.POSITIVE_INFINITY, ""NaN"", ""1"", -1); x[3] === ""1"". Actual: ' + (x[3]));
}

if (x[4] !== -1) {
  throw new Test262Error('#8: x = []; x[0] = 0; x.unshift(true, Number.POSITIVE_INFINITY, ""NaN"", ""1"", -1); x[4] === -1. Actual: ' + (x[4]));
}

if (x.length !== 6) {
  throw new Test262Error('#9: x = []; x[0] = 0; x.unshift(true, Number.POSITIVE_INFINITY, ""NaN"", ""1"", -1); x.length === 6. Actual: ' + (x.length));
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