using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.pre_incr
{
	[TestClass]
	public sealed class Test003 : CodeTestBase
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

//CHECK#1
var object = {valueOf: function() {return 1}};
if (++object !== 1 + 1) {
  throw new Test262Error('#1: var object = {valueOf: function() {return 1}}; ++object === 1 + 1. Actual: ' + (++object));
} else {
  if (object !== 1 + 1) {
    throw new Test262Error('#1: var object = {valueOf: function() {return 1}}; ++object; object === 1 + 1. Actual: ' + (object));
  }
}

//CHECK#2
var object = {valueOf: function() {return 1}, toString: function() {return 0}};
if (++object !== 1 + 1) {
  throw new Test262Error('#2: var object = {valueOf: function() {return 1}, toString: function() {return 0}}; ++object === 1 + 1. Actual: ' + (++object));
} else {
  if (object !== 1 + 1) {
    throw new Test262Error('#2: var object = {valueOf: function() {return 1}, toString: function() {return 0}}; ++object; object === 1 + 1. Actual: ' + (object));
  }
}

//CHECK#3
var object = {valueOf: function() {return 1}, toString: function() {return {}}};
if (++object !== 1 + 1) {
  throw new Test262Error('#3: var object = {valueOf: function() {return 1}, toString: function() {return {}}}; ++object === 1 + 1. Actual: ' + (++object));
} else {
  if (object !== 1 + 1) {
    throw new Test262Error('#3: var object = {valueOf: function() {return 1}, toString: function() {return {}}}; ++object; object === 1 + 1. Actual: ' + (object));
  }
}

//CHECK#4
try {
  var object = {valueOf: function() {return 1}, toString: function() {throw ""error""}};
  if (++object !== 1 + 1) {
    throw new Test262Error('#4.1: var object = {valueOf: function() {return 1}, toString: function() {throw ""error""}}; ++object === 1 + 1. Actual: ' + (++object));
  } else {
    if (object !== 1 + 1) {
      throw new Test262Error('#4.1: var object = {valueOf: function() {return 1}, toString: function() {throw ""error""}}; ++object; object === 1 + 1. Actual: ' + (object));
    }
  }
}
catch (e) {
  if (e === ""error"") {
    throw new Test262Error('#4.2: var object = {valueOf: function() {return 1}, toString: function() {throw ""error""}}; ++object not throw ""error""');
  } else {
    throw new Test262Error('#4.3: var object = {valueOf: function() {return 1}, toString: function() {throw ""error""}}; ++object not throw Error. Actual: ' + (e));
  }
}

//CHECK#5
var object = {toString: function() {return 1}};
if (++object !== 1 + 1) {
  throw new Test262Error('#5.1: var object = {toString: function() {return 1}}; ++object === 1 + 1. Actual: ' + (++object));
} else {
  if (object !== 1 + 1) {
    throw new Test262Error('#5.2: var object = {toString: function() {return 1}}; ++object; object === 1 + 1. Actual: ' + (object));
  }
}


//CHECK#6
var object = {valueOf: function() {return {}}, toString: function() {return 1}}
if (++object !== 1 + 1) {
  throw new Test262Error('#6.1: var object = {valueOf: function() {return {}}, toString: function() {return 1}}; ++object === 1 + 1. Actual: ' + (++object));
} else {
  if (object !== 1 + 1) {
    throw new Test262Error('#6.2: var object = {valueOf: function() {return {}}, toString: function() {return 1}}; ++object; object === 1 + 1. Actual: ' + (object));
  }
}

//CHECK#7
try {
  var object = {valueOf: function() {throw ""error""}, toString: function() {return 1}};
  ++object;
  throw new Test262Error('#7.1: var object = {valueOf: function() {throw ""error""}, toString: function() {return 1}}; ++object throw ""error"". Actual: ' + (++object));
}  
catch (e) {
  if (e !== ""error"") {
    throw new Test262Error('#7.2: var object = {valueOf: function() {throw ""error""}, toString: function() {return 1}}; ++object throw ""error"". Actual: ' + (e));
  } 
}

//CHECK#8
try {
  var object = {valueOf: function() {return {}}, toString: function() {return {}}};
  ++object;
  throw new Test262Error('#8.1: var object = {valueOf: function() {return {}}, toString: function() {return {}}}; ++object throw TypeError. Actual: ' + (++object));
}  
catch (e) {
  if ((e instanceof TypeError) !== true) {
    throw new Test262Error('#8.2: var object = {valueOf: function() {return {}}, toString: function() {return {}}}; ++object throw TypeError. Actual: ' + (e));
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
