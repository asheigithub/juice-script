using juicescript;
using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.trycatch
{
	[TestClass]
	public sealed class Test040 : CodeTestBase
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
var c1=0;
function myFunction1(){
  try{
    return 1;
  }catch(err){
    throw new Test262Error('#1.1: ""return 1"" inside function does not lead to throwing exception');
    return 0;
  }finally{
    c1=1;
  }
  return 2;
}
var x1=myFunction1();
if(x1!==1){
  throw new Test262Error('#1.3: x1===1. Actual: x1==='+x1);
}
if (c1!==1){
  throw new Test262Error('#1.4: ""finally"" block must be evaluated');
}

// CHECK#2
var c2=0;
function myFunction2(){
  try{
    throw ""exc"";
    return 1;
  }catch(err){  	
    return 0;
  }finally{
    c2=1;
  }
  return 2;
}
var x2=myFunction2();
if (c2!==1){
  throw new Test262Error('#2.1: ""finally"" block must be evaluated');
}
if (x2!==0){
  throw new Test262Error('#2.2: x2===0. Actual: x2==='+x2);
}

// CHECK#3
var c3 = 0; 
function myFunction3(){
  try{
	  
    throw 1;
  }catch(err){  	
    return 1;
  }finally{
    c3=1;
  }
  return 2;
}
var x3=myFunction3();
if (c3!==1){
  throw new Test262Error('#3.1: ""finally"" block must be evaluated');
}
if (x3!==1){
  throw new Test262Error('#3.2: x3===1. Actual: x3==='+x3);
}

// CHECK#4
var c4=0;
function myFunction4(){
  try{
    throw ""ex1"";
    return 1;
  }catch(err){
    throw ""ex2""
    return 0;
  }finally{
    c4=1;
  }
  return 2;
}
try{
  var x4=myFunction4();
  throw new Test262Error('#4.1: Throwing exception inside function lead to throwing exception outside this function');
}
catch(e){
  if(e===""ex1""){
    throw new Test262Error('#4.2: Exception !== ""ex1"". Actual: catch previous exception');
  }
  if(e!==""ex2""){
    throw new Test262Error('#4.3: Exception === ""ex2"". Actual:  Exception ==='+ e  );
  }
  if (c4!==1){
    throw new Test262Error('#4.4: ""finally"" block must be evaluated');
  }	
}

// CHECK#5
var c5=0;
function myFunction5(){
  try{
    throw ""ex1"";
    return 1;
  }catch(err){
    return 0;
  }finally{
    c5=1;
    throw ""ex2"";
  }
  return 2;
}
try{
  var x5=myFunction5();
  throw new Test262Error('#5.1: Throwing exception inside function lead to throwing exception outside this function');
}
catch(e){
  if(e===""ex1""){
    throw new Test262Error('#5.2: Exception !== ""ex1"". Actual: catch previous exception');
  }
  if(e!==""ex2""){
    throw new Test262Error('#5.3: Exception === ""ex2"". Actual:  Exception ==='+ e  );
  }
  if (c5!==1){
    throw new Test262Error('#5.4: ""finally"" block must be evaluated');
  } 	
}

// CHECK#6
var c6=0;
function myFunction6(){
  try{
    throw ""ex1"";
    return 1;
  }catch(err){
    throw ""ex2"";
    return 0;
  }finally{
    c6=1;
    throw ""ex3"";
  }
  return 2;
}
try{
  var x6=myFunction6();
  throw new Test262Error('#6.1: Throwing exception inside function lead to throwing exception outside this function');
}
catch(e){
  if(e===""ex1""){
    throw new Test262Error('#6.2: Exception !== ""ex1"". Actual: catch previous exception');
  }
  if(e===""ex2""){
    throw new Test262Error('#6.3: Exception !== ""ex2"". Actual: catch previous exception');
  }
  if(e!==""ex3""){
    throw new Test262Error('#6.4: Exception === ""ex3"". Actual:  Exception ==='+ e  );
  }	
  if(c6!==1) throw new Test262Error('#6.5: ""finally"" block must be evaluated');
}

// CHECK#7
var c7=0;
function myFunction7(){
  try{
    throw ""ex1"";
    return 1;
  }catch(err){
    throw ""ex2"";
    return 0;
  }finally{
    c7=1;
    return 2;
  }
  return 3;
}
try{
  var x7=myFunction7();
  if(x7!==2) throw new Test262Error('#7.1: x7===2. Actual: x7==='+x7);
}
catch(e){}
if(c7!==1) throw new Test262Error('#7.2: ""finally"" block must be evaluated');





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
