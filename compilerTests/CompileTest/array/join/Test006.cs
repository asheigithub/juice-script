using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.array.join
{
	[TestClass]
	public sealed class Test006 : CodeTestBase
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

var obj = {};
obj.join = Array.prototype.join;

obj.length = NaN;
if (obj.join() !== """") {
  throw new Test262Error('#1: var obj = {}; obj.length = NaN; obj.join = Array.prototype.join; obj.join() === """". Actual: ' + (obj.join()));
}

assert.sameValue(obj.length, NaN, ""obj.length is NaN"");

obj.length = Number.NEGATIVE_INFINITY;
if (obj.join() !== """") {
  throw new Test262Error('#5: var obj = {}; obj.length = Number.NEGATIVE_INFINITY; obj.join = Array.prototype.join; obj.join() === """". Actual: ' + (obj.join()));
}

if (obj.length !== Number.NEGATIVE_INFINITY) {
  throw new Test262Error('#6: var obj = {}; obj.length = Number.NEGATIVE_INFINITY; obj.join = Array.prototype.join; obj.join(); obj.length === Number.NEGATIVE_INFINITY. Actual: ' + (obj.length));
}

obj.length = -0;
if (obj.join() !== """") {
  throw new Test262Error('#7: var obj = {}; obj.length = -0; obj.join = Array.prototype.join; obj.join() === """". Actual: ' + (obj.join()));
}

if (obj.length !== -0) {
  throw new Test262Error('#8: var obj = {}; obj.length = -0; obj.join = Array.prototype.join; obj.join(); obj.length === 0. Actual: ' + (obj.length));
} else {
  if (1 / obj.length !== Number.NEGATIVE_INFINITY) {
    throw new Test262Error('#8: var obj = {}; obj.length = -0; obj.join = Array.prototype.join; obj.join(); obj.length === -0. Actual: ' + (obj.length));
  }
}

obj.length = 0.5;
if (obj.join() !== """") {
  throw new Test262Error('#9: var obj = {}; obj.length = 0.5; obj.join = Array.prototype.join; obj.join() === """". Actual: ' + (obj.join()));
}

if (obj.length !== 0.5) {
  throw new Test262Error('#10: var obj = {}; obj.length = 0.5; obj.join = Array.prototype.join; obj.join(); obj.length === 0.5. Actual: ' + (obj.length));
}

var x = new Number(0);
obj.length = x;
if (obj.join() !== """") {
  throw new Test262Error('#11: var x = new Number(0); var obj = {}; obj.length = x; obj.join = Array.prototype.join; obj.join() === """". Actual: ' + (obj.join()));
}

if (obj.length !== x) {
  throw new Test262Error('#12: var x = new Number(0); var obj = {}; obj.length = x; obj.join = Array.prototype.join; obj.join(); obj.length === x. Actual: ' + (obj.length));
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

				RtScriptClass rtPayload = (RtScriptClass)globalInstance.facility;

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