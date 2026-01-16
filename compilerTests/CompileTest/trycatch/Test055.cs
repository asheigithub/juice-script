using juicescript;
using juicescript.compiler;
using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.trycatch
{
	[TestClass]
	public sealed class Test055 : CodeTestBase
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
try{
  try{
    throw ""ex2"";
  }
  catch(er2){
    if (er2!==""ex2"")
      throw new Test262Error('#1.1: Exception === ""ex2"". Actual:  Exception ==='+ er2  );
      throw ""ex1"";
    }
  }
  catch(er1){
    if (er1!==""ex1"") throw new Test262Error('#1.2: Exception === ""ex1"". Actual: '+er1);
    if (er1===""ex2"") throw new Test262Error('#1.3: Exception !== ""ex2"". Actual: catch previous embedded exception');
}

// CHECK#2
try{
  throw ""ex1"";
}
catch(er1){
  try{
    throw ""ex2"";
  }
  catch(er1){
    if (er1===""ex1"") throw new Test262Error('#2.1: Exception !== ""ex1"". Actual: catch previous catching exception');
    if (er1!==""ex2"") throw new Test262Error('#2.2: Exception === ""ex2"". Actual:  Exception ==='+ er1  );
  }
  if (er1!==""ex1"") throw new Test262Error('#2.3: Exception === ""ex1"". Actual:  Exception ==='+ er1  );
  if (er1===""ex2"") throw new Test262Error('#2.4: Exception !== ""ex2"". Actual: catch previous catching exception');
}

// CHECK#3
try{
  throw ""ex1"";
}
catch(er1){
  if (er1!==""ex1"") throw new Test262Error('#3.1: Exception ===""ex1"". Actual:  Exception ==='+ er1  );
}
finally{
  try{
    throw ""ex2"";
  }
  catch(er1){
    if (er1===""ex1"") throw new Test262Error('#3.2: Exception !==""ex1"". Actual: catch previous embedded exception');
    if (er1!==""ex2"") throw new Test262Error('#3.3: Exception ===""ex2"". Actual:  Exception ==='+ er1  );
  }
}

// CHECK#4
var c4=0;
try{
  throw ""ex1"";
}
catch(er1){
  try{
    throw ""ex2"";
  }
  catch(er1){
    if (er1===""ex1"") throw new Test262Error('#4.1: Exception !==""ex1"". Actual: catch previous catching exception');
    if (er1!==""ex2"") throw new Test262Error('#4.2: Exception ===""ex2"". Actual:  Exception ==='+ er1  );
  }
  if (er1!==""ex1"") throw new Test262Error('#4.3: Exception ===""ex1"". Actual:  Exception ==='+ er1  );
  if (er1===""ex2"") throw new Test262Error('#4.4: Exception !==""ex2"". Actual: Catch previous embedded exception');
}
finally{
  c4=1;
}
if (c4!==1) throw new Test262Error('#4.5: ""finally"" block must be evaluated');

// CHECK#5
var c5=0;
try{
  try{
    throw ""ex2"";
  }
  catch(er1){
    if (er1!==""ex2"") throw new Test262Error('#5.1: Exception ===""ex2"". Actual:  Exception ==='+ er1  );
  }
  throw ""ex1"";
}
catch(er1){
  if (er1!==""ex1"") throw new Test262Error('#5.2: Exception ===""ex1"". Actual:  Exception ==='+ er1  );
  if (er1===""ex2"") throw new Test262Error('#5.3: Exception !==""ex2"". Actual: catch previous embedded exception');
}
finally{
  c5=1;
}
if (c5!==1) throw new Test262Error('#5.4: ""finally"" block must be evaluated');

// CHECK#6
var c6=0;
try{
  try{
    throw ""ex1"";
  }
  catch(er1){
    if (er1!==""ex1"") throw new Test262Error('#6.1: Exception ===""ex1"". Actual:  Exception ==='+ er1  );
  }
}
finally{
  c6=1;		
}
if (c6!==1) throw new Test262Error('#6.2: ""finally"" block must be evaluated');

// CHECK#7
var c7=0;
try{
  try{
    throw ""ex1"";
  }
  finally{
    try{
      c7=1;
      throw ""ex2"";
    }
    catch(er1){
      if (er1!==""ex2"") throw new Test262Error('#7.1: Exception ===""ex2"". Actual:  Exception ==='+ er1  );
      if (er1===""ex1"") throw new Test262Error('#7.2: Exception !==""ex1"". Actual: catch previous embedded exception');
      c7++;
    }
  }
}
catch(er1){
  if (er1!==""ex1"") throw new Test262Error('#7.3: Exception ===""ex1"". Actual:  Exception ==='+ er1  );
}
if (c7!==2) throw new Test262Error('#7.4: ""finally"" block must be evaluated');



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
