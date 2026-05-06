using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.parseInt
{
	[TestClass]
	public sealed class Test010 : CodeTestBase
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



assert.sameValue(parseInt(""0x0"", 0), parseInt(""0"", 16), 'parseInt(""0x0"", 0) must return the same value returned by parseInt(""0"", 16)');
assert.sameValue(parseInt(""0x1"", 0), parseInt(""1"", 16), 'parseInt(""0x1"", 0) must return the same value returned by parseInt(""1"", 16)');
assert.sameValue(parseInt(""0x2"", 0), parseInt(""2"", 16), 'parseInt(""0x2"", 0) must return the same value returned by parseInt(""2"", 16)');
assert.sameValue(parseInt(""0x3"", 0), parseInt(""3"", 16), 'parseInt(""0x3"", 0) must return the same value returned by parseInt(""3"", 16)');
assert.sameValue(parseInt(""0x4"", 0), parseInt(""4"", 16), 'parseInt(""0x4"", 0) must return the same value returned by parseInt(""4"", 16)');
assert.sameValue(parseInt(""0x5"", 0), parseInt(""5"", 16), 'parseInt(""0x5"", 0) must return the same value returned by parseInt(""5"", 16)');
assert.sameValue(parseInt(""0x6"", 0), parseInt(""6"", 16), 'parseInt(""0x6"", 0) must return the same value returned by parseInt(""6"", 16)');
assert.sameValue(parseInt(""0x7"", 0), parseInt(""7"", 16), 'parseInt(""0x7"", 0) must return the same value returned by parseInt(""7"", 16)');
assert.sameValue(parseInt(""0x8"", 0), parseInt(""8"", 16), 'parseInt(""0x8"", 0) must return the same value returned by parseInt(""8"", 16)');
assert.sameValue(parseInt(""0x9"", 0), parseInt(""9"", 16), 'parseInt(""0x9"", 0) must return the same value returned by parseInt(""9"", 16)');
assert.sameValue(parseInt(""0xA"", 0), parseInt(""A"", 16), 'parseInt(""0xA"", 0) must return the same value returned by parseInt(""A"", 16)');
assert.sameValue(parseInt(""0xB"", 0), parseInt(""B"", 16), 'parseInt(""0xB"", 0) must return the same value returned by parseInt(""B"", 16)');
assert.sameValue(parseInt(""0xC"", 0), parseInt(""C"", 16), 'parseInt(""0xC"", 0) must return the same value returned by parseInt(""C"", 16)');
assert.sameValue(parseInt(""0xD"", 0), parseInt(""D"", 16), 'parseInt(""0xD"", 0) must return the same value returned by parseInt(""D"", 16)');
assert.sameValue(parseInt(""0xE"", 0), parseInt(""E"", 16), 'parseInt(""0xE"", 0) must return the same value returned by parseInt(""E"", 16)');
assert.sameValue(parseInt(""0xF"", 0), parseInt(""F"", 16), 'parseInt(""0xF"", 0) must return the same value returned by parseInt(""F"", 16)');
assert.sameValue(parseInt(""0xE"", 0), parseInt(""E"", 16), 'parseInt(""0xE"", 0) must return the same value returned by parseInt(""E"", 16)');

assert.sameValue(
  parseInt(""0xABCDEF"", 0),
  parseInt(""ABCDEF"", 16),
  'parseInt(""0xABCDEF"", 0) must return the same value returned by parseInt(""ABCDEF"", 16)'
);


assert.sameValue(parseInt(""0X0"", 0), parseInt(""0"", 16), 'parseInt(""0X0"", 0) must return the same value returned by parseInt(""0"", 16)');
assert.sameValue(parseInt(""0X1""), parseInt(""1"", 16), 'parseInt(""0X1"") must return the same value returned by parseInt(""1"", 16)');
assert.sameValue(parseInt(""0X2""), parseInt(""2"", 16), 'parseInt(""0X2"") must return the same value returned by parseInt(""2"", 16)');
assert.sameValue(parseInt(""0X3""), parseInt(""3"", 16), 'parseInt(""0X3"") must return the same value returned by parseInt(""3"", 16)');
assert.sameValue(parseInt(""0X4""), parseInt(""4"", 16), 'parseInt(""0X4"") must return the same value returned by parseInt(""4"", 16)');
assert.sameValue(parseInt(""0X5""), parseInt(""5"", 16), 'parseInt(""0X5"") must return the same value returned by parseInt(""5"", 16)');
assert.sameValue(parseInt(""0X6""), parseInt(""6"", 16), 'parseInt(""0X6"") must return the same value returned by parseInt(""6"", 16)');
assert.sameValue(parseInt(""0X7""), parseInt(""7"", 16), 'parseInt(""0X7"") must return the same value returned by parseInt(""7"", 16)');
assert.sameValue(parseInt(""0X8""), parseInt(""8"", 16), 'parseInt(""0X8"") must return the same value returned by parseInt(""8"", 16)');
assert.sameValue(parseInt(""0X9""), parseInt(""9"", 16), 'parseInt(""0X9"") must return the same value returned by parseInt(""9"", 16)');
assert.sameValue(parseInt(""0XA""), parseInt(""A"", 16), 'parseInt(""0XA"") must return the same value returned by parseInt(""A"", 16)');
assert.sameValue(parseInt(""0XB""), parseInt(""B"", 16), 'parseInt(""0XB"") must return the same value returned by parseInt(""B"", 16)');
assert.sameValue(parseInt(""0XC""), parseInt(""C"", 16), 'parseInt(""0XC"") must return the same value returned by parseInt(""C"", 16)');
assert.sameValue(parseInt(""0XD""), parseInt(""D"", 16), 'parseInt(""0XD"") must return the same value returned by parseInt(""D"", 16)');
assert.sameValue(parseInt(""0XE""), parseInt(""E"", 16), 'parseInt(""0XE"") must return the same value returned by parseInt(""E"", 16)');
assert.sameValue(parseInt(""0XF""), parseInt(""F"", 16), 'parseInt(""0XF"") must return the same value returned by parseInt(""F"", 16)');
assert.sameValue(parseInt(""0XE""), parseInt(""E"", 16), 'parseInt(""0XE"") must return the same value returned by parseInt(""E"", 16)');
assert.sameValue(parseInt(""0XABCDEF""), parseInt(""ABCDEF"", 16), 'parseInt(""0XABCDEF"") must return the same value returned by parseInt(""ABCDEF"", 16)');



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

				RtScriptClass rtPayload = (RtScriptClass)globalInstance;

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
