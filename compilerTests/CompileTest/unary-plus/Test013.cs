using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.unary_plus
{
	[TestClass]
	public sealed class Test013 : CodeTestBase
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





// CHECK#1
if (+(new Number()) !== 0) {
  throw new Test262Error('#1: +(new Number()) === 0. Actual: ' + (+(new Number())));
}

// CHECK#2
if (+(new Number(0)) !== 0) {
  throw new Test262Error('#2: +(new Number(0)) === 0. Actual: ' + (+(new Number(0))));
}

// CHECK#3
if (isNaN(+(new Number(Number.NaN))) !== true) {
  throw new Test262Error('#3: +(new Number(Number.NaN)) === Not-a-Number. Actual: ' + (+(new Number(Number.NaN))));
}

// CHECK#4
if (+(new Number(null)) !== 0) {
  throw new Test262Error('#4.1: +(new Number(null)) === 0. Actual: ' + (+(new Number(null)))); 
} else {
  if (1/+(new Number(null)) !== Number.POSITIVE_INFINITY) {
    throw new Test262Error('#4.2: +(new Number(null)) === +0. Actual: -0');
  }	
}

// CHECK#5
if (isNaN(+(new Number(void 0))) !== true) {
  throw new Test262Error('#5: +(new Number(void 0)) === Not-a-Number. Actual: ' + (+(new Number(void 0))));
}

// CHECK#6
if (+(new Number(true)) !== 1) {
  throw new Test262Error('#6: +(new Number(true)) === 1. Actual: ' + (+(new Number(true))));
}

// CHECK#7
if (+(new Number(false)) !== +0) {
  throw new Test262Error('#7.1: +(new Number(false)) === 0. Actual: ' + (+(new Number(false))));
} else {
  if (1/+(new Number(false)) !== Number.POSITIVE_INFINITY) {
    throw new Test262Error('#7.2: +(new Number(false)) === +0. Actual: -0');
  }
}

var o = new Boolean(true);

// CHECK#8
if (+(o) !== 1) {
  throw new Test262Error('#8: +(new Boolean(true)) === 1. Actual: ' + (+(o)));
}

o = new Boolean(false);

// CHECK#9
if (+(o) !== +0) {
  throw new Test262Error('#9.1: +(new Boolean(false)) === 0. Actual: ' + (+(o)));
} else {
  if (1/+(o) !== Number.POSITIVE_INFINITY) {
    throw new Test262Error('#9.2: +(new Boolean(false)) === +0. Actual: -0');
  }
}

o = new Array(2,4,8,16,32);

// CHECK#10
if (isNaN(+(o)) !== true) {
  throw new Test262Error('#10: +(new Array(2,4,8,16,32)) === Not-a-Number. Actual: ' + (+(o)));
}

// CHECK#11
var myobj1 = {
                ToNumber : function(){return 12345;}, 
                toString : function(){return ""67890"";},
                valueOf  : function(){return ""[object MyObj]"";} 
            };

if (isNaN(+(myobj1)) !== true){
  throw new Test262Error(""#11: +(myobj1) calls ToPrimitive with hint +. Exptected: Not-a-Number. Actual: "" + (+(myobj1)));
}

// CHECK#12
var myobj2 = {
                ToNumber : function(){return 12345;}, 
                toString : function(){return ""67890"";},
                valueOf  : function(){return ""9876543210"";} 
            };

if (+(myobj2) !== 9876543210){
  throw new Test262Error(""#12: +(myobj2) calls ToPrimitive with hint +. Exptected: 9876543210. Actual: "" + (+(myobj2)));
}


// CHECK#13
var myobj3 = {
                ToNumber : function(){return 12345;}, 
                toString : function(){return ""[object MyObj]"";} 
            };

if (isNaN(+(myobj3)) !== true){
  throw new Test262Error(""#13: +(myobj3) calls ToPrimitive with hint +. Exptected: Not-a-Number. Actual: "" + (+(myobj3)));
}

// CHECK#14
var myobj4 = {
                ToNumber : function(){return 12345;}, 
                toString : function(){return ""67890"";} 
            };

if (+(myobj4) !== 67890){
  throw new Test262Error(""#14: +(myobj4) calls ToPrimitive with hint +. Exptected: 67890. Actual: "" + (+(myobj4)));
}

// CHECK#15
var myobj5 = {
                ToNumber : function(){return 12345;} 
            };

if (isNaN(+(myobj5)) !== true){
  throw new Test262Error(""#15: +(myobj5) calls ToPrimitive with hint +. Exptected: 12345. Actual: "" + (+(myobj5)));
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
