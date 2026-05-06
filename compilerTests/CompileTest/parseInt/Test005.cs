using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.parseInt
{
	[TestClass]
	public sealed class Test005 : CodeTestBase
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

assert.sameValue(parseInt(""\u000C1""), parseInt(""1""), 'parseInt(""\\u000C1"") must return the same value returned by parseInt(""1"")');
assert.sameValue(parseInt(""\u000C\u000C-1""), parseInt(""-1""), 'parseInt(""\\u000C\\u000C-1"") must return the same value returned by parseInt(""-1"")');

//CHECK#3
assert.sameValue(parseInt(""\u000C""), NaN, 'parseInt(""\\u000C"") must return NaN');



assert.sameValue(parseInt(""\u000B1""), parseInt(""1""), 'parseInt(""\\u000B1"") must return the same value returned by parseInt(""1"")');
assert.sameValue(parseInt(""\u000B\u000B-1""), parseInt(""-1""), 'parseInt(""\\u000B\\u000B-1"") must return the same value returned by parseInt(""-1"")');

//CHECK#3
assert.sameValue(parseInt(""\u000B""), NaN, 'parseInt(""\\u000B"") must return NaN');

assert.sameValue(parseInt(""\u000D1""), parseInt(""1""), 'parseInt(""\\u000D1"") must return the same value returned by parseInt(""1"")');
assert.sameValue(parseInt(""\u000D\u000D-1""), parseInt(""-1""), 'parseInt(""\\u000D\\u000D-1"") must return the same value returned by parseInt(""-1"")');

//CHECK#3
assert.sameValue(parseInt(""\u000D""), NaN, 'parseInt(""\\u000D"") must return NaN');


assert.sameValue(parseInt(""\u000A1""), parseInt(""1""), 'parseInt(""\\u000A1"") must return the same value returned by parseInt(""1"")');
assert.sameValue(parseInt(""\u000A\u000A-1""), parseInt(""-1""), 'parseInt(""\\u000A\\u000A-1"") must return the same value returned by parseInt(""-1"")');

//CHECK#3
assert.sameValue(parseInt(""\u000A""), NaN, 'parseInt(""\\u000A"") must return NaN');

assert.sameValue(parseInt(""\u20281""), parseInt(""1""), 'parseInt(""\\u20281"") must return the same value returned by parseInt(""1"")');
assert.sameValue(parseInt(""\u2028\u2028-1""), parseInt(""-1""), 'parseInt(""\\u2028\\u2028-1"") must return the same value returned by parseInt(""-1"")');

//CHECK#3
assert.sameValue(parseInt(""\u2028""), NaN, 'parseInt(""\\u2028"") must return NaN');

assert.sameValue(
  parseInt(""\u20291""),
  parseInt(""1""),
  'parseInt(""\\u20291"") must return the same value returned by parseInt(""1"")'
);

assert.sameValue(
  parseInt(""\u2029\u2029-1""),
  parseInt(""-1""),
  'parseInt(""\\u2029\\u2029-1"") must return the same value returned by parseInt(""-1"")'
);

//CHECK#3
assert.sameValue(parseInt(""\u2029""), NaN, 'parseInt(""\\u2029"") must return NaN');



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
