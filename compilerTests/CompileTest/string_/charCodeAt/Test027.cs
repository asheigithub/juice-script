using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.string_
{
	[TestClass]
	public sealed class Test027 : CodeTestBase
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
	
	[Doc]
	/**
	 * ...
	 * @author 
	 */
	public class Main extends Sprite
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


var __instance = { toString:function () 
{
	return false;
} };

__instance.charCodeAt = String.prototype.charCodeAt;

//////////////////////////////////////////////////////////////////////////////
//CHECK#1
if (__instance.charCodeAt(false) !== 0x66) {
  throw new Test262Error('#1: __instance = new Boolean; __instance.charCodeAt = String.prototype.charCodeAt; __instance.charCodeAt(false)===0x66. Actual: ' + __instance.charCodeAt(false));
}
//
//////////////////////////////////////////////////////////////////////////////

//////////////////////////////////////////////////////////////////////////////
//CHECK#2
if (__instance.charCodeAt(true) !== 0x61) {
  throw new Test262Error('#2: __instance = new Boolean; __instance.charCodeAt = String.prototype.charCodeAt; __instance.charCodeAt(true)===0x61. Actual: ' + __instance.charCodeAt(true));
}
//
//////////////////////////////////////////////////////////////////////////////

//////////////////////////////////////////////////////////////////////////////
//CHECK#3
if (__instance.charCodeAt(true + 1) !== 0x6C) {
  throw new Test262Error('#3: __instance = new Boolean; __instance.charCodeAt = String.prototype.charCodeAt; __instance.charCodeAt(true+1) === 0x6C. Actual: ' + __instance.charCodeAt(true + 1));
}
//
//////////////////////////////////////////////////////////////////////////////

//////////////////////////////////////////////////////////////////////////////
//CHECK#1
//since Number() evaluates to 0 charCodeAt() evaluates to charCodeAt(0)
if (""smart"".charCodeAt() !== 0x73) {
  throw new Test262Error('#1: ""smart"".charCodeAt() === 0x73. Actual: ""smart"".charCodeAt() ===' + (""smart"".charCodeAt()));
}
//
//////////////////////////////////////////////////////////////////////////////

//////////////////////////////////////////////////////////////////////////////
//CHECK#1
//since ToInteger(null) evaluates to 0 charCodeAt() evaluates to charCodeAt(0)
if (function() {
    return ""lego""
  }().charCodeAt(null) !== 0x6C) {
  throw new Test262Error('#1: function(){return ""lego""}().charCodeAt(null) === 0x6C. Actual: ' + function() {
    return ""lego""
  }().charCodeAt(null));
}
//
//////////////////////////////////////////////////////////////////////////////

//////////////////////////////////////////////////////////////////////////////
//CHECK#1
//since ToInteger(undefined) evaluates to 0 charCodeAt() evaluates to charCodeAt(0)
if (new String(""lego"").charCodeAt(x) !== 0x6C) {
  throw new Test262Error('#1: var x; new String(""lego"").charCodeAt(x) === 0x6C. Actual: new String(""lego"").charCodeAt(x) ===' + new String(""lego"").charCodeAt(x));
}
//
//////////////////////////////////////////////////////////////////////////////

var x;


//////////////////////////////////////////////////////////////////////////////
//CHECK#1
//since ToInteger(undefined) evaluates to 0 charCodeAt() evaluates to charCodeAt(0)
if (String(""lego"").charCodeAt(undefined) !== 0x6C) {
  throw new Test262Error('#1: String(""lego"").charCodeAt(undefined) === 0x6C. Actual: String(""lego"").charCodeAt(undefined) ===' + String(""lego"").charCodeAt(undefined));
}
//
//////////////////////////////////////////////////////////////////////////////

//////////////////////////////////////////////////////////////////////////////
//CHECK#1
//since ToInteger(void 0) evaluates to 0 charCodeAt() evaluates to charCodeAt(0)
if (String(42).charCodeAt(void 0) !== 0x34) {
  throw new Test262Error('#1: String(42).charCodeAt(void 0) === 0x34. Actual: String(42).charCodeAt(void 0) ===' + String(42).charCodeAt(void 0));
}
//
//////////////////////////////////////////////////////////////////////////////



//////////////////////////////////////////////////////////////////////////////
//CHECK#1
//since ToInteger(undefined) evaluates to 0 charCodeAt() evaluates to charCodeAt(0)
if (new String(42).charCodeAt(function() {}()) !== 0x34) {
  throw new Test262Error('#1: new String(42).charCodeAt(function(){}()) === 0x34. Actual: new String(42).charCodeAt(function(){}()) ===' + new String(42).charCodeAt(function() {}()));
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
