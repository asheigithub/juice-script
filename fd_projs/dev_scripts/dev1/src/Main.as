package 
{
	 [Doc]
    public class Main {
        public function Main() {
            test_basic_shift();
            test_empty_array_shift();
            test_mixed_types();
            test_length_property();
            test_chain_shift();
            test_sparse_array_shift();
            test_sparse_with_holes();
            test_struct_shift();

            trace("All tests passed!");
        }

        private function test_basic_shift(): void {
            var arr:Array = new Array(1, 2, 3);
            var shifted = arr.shift();
            assertEquals(1, shifted, "basic shift should return first element");
            assertEquals(2, arr.length, "length should decrease after shift");
            assertEquals(2, arr[0], "element at index 0 should be 2");
            assertEquals(3, arr[1], "element at index 1 should be 3");
            trace("test_basic_shift: PASS");
        }

        private function test_empty_array_shift(): void {
            var arr:Array = new Array();
            var shifted = arr.shift();
            assertEquals(undefined, shifted, "shift empty array should return undefined");
            assertEquals(0, arr.length, "empty array length should remain 0");
            trace("test_empty_array_shift: PASS");
        }

        private function test_mixed_types(): void {
            var arr:Array = new Array(1, "string", true, null, undefined);
            var shifted = arr.shift();
            assertEquals(1, shifted, "shift should return first element");
            assertEquals(4, arr.length, "length should decrease");
            assertEquals("string", arr[0], "element at index 0 should be string");
            trace("test_mixed_types: PASS");
        }

        private function test_length_property(): void {
            var arr:Array = new Array(1, 2, 3, 4, 5);
            arr.length = 3;
            var shifted = arr.shift();
            assertEquals(1, shifted, "shift after set length should return element at index 0");
            assertEquals(2, arr.length, "length should be 2 after shift");
            assertEquals(2, arr[0], "element at index 0 should be 2");
            trace("test_length_property: PASS");
        }

        private function test_chain_shift(): void {
            var arr:Array = new Array(1, 2, 3);
            var result = arr.shift();
            result = arr.shift();
            result = arr.shift();
            result = arr.shift();
            assertEquals(undefined, result, "multiple shifts should return undefined on empty");
            assertEquals(0, arr.length, "array should be empty");
            trace("test_chain_shift: PASS");
        }

        private function test_sparse_array_shift(): void {
            var arr:Array = new Array(1, 2, 3);
            arr[5] = 6;
            var shifted = arr.shift();
            assertEquals(1, shifted, "sparse array shift should return element at index 0");
            assertEquals(5, arr.length, "sparse array length should be 5");
            assertEquals(2, arr[0], "element at index 0 should be 2");
            assertEquals(3, arr[1], "element at index 1 should be 3");
            assertEquals(6, arr[4], "element at index 4 should be 6");
            trace("test_sparse_array_shift: PASS");
        }

        private function test_sparse_with_holes(): void {
            var arr:Array = new Array(1, 2, 3, 4, 5);
            delete arr[1];
            delete arr[3];
            var shifted = arr.shift();
            assertEquals(1, shifted, "shift should return first element");
            assertEquals(4, arr.length, "holes are treated as elements, length should decrease from 5 to 4");
            trace("test_sparse_with_holes: PASS");
        }

        private function test_struct_shift(): void {
            var arr:Array = new Array();
            var obj:Object = {x: 1};
            arr.push(obj);
            arr.push(2);
            var shifted = arr.shift();
            var result = shifted.x;
            assertEquals(1, result, "shift should preserve struct data");
            assertEquals(1, arr.length, "length should decrease");
            trace("test_struct_shift: PASS");
        }

        private function assertEquals(expected:*, actual:*, msg:String): void {
            if (expected != actual) {
                throw new Error("Assertion failed: " + msg + ", expected=" + expected + ", actual=" + actual);
            }
        }
    }
}

new Main();

//var __instance = new Object(42);
//
//__instance.charAt = String.prototype.charAt;
//
//if (__instance.charAt(false) + __instance.charAt(true) !== "42") {
  //throw new Error('#1: __instance = new Object(42); __instance.charAt = String.prototype.charAt;  __instance = new Object(42); __instance.charAt = String.prototype.charAt; __instance.charAt(false)+__instance.charAt(true) === "42". Actual: ' + __instance.charAt(false) + __instance.charAt(true));
//}
//
//
//trace(String["hasOwnProperty"]("fromCharCode"));



//async function asyncAdd(a:int, b:int):int {
    //var x:int = await Promise.resolve(a);
    //var y:int = await Promise.resolve(b);
    //return x + y;
//}
//async function asyncHello():String {
    //var result:String = await Promise.resolve("Hello");
    //return result + " World";
//}
//async function returnsPromise():* {
    //return Promise.resolve(42);
//}
//async function asyncVoid():void {
    //trace("asyncVoid executing");
//}
//async function asyncNest():int {
    //var inner:* = asyncInner(5);
    //var result:int = await inner;
    //return result * 2;
//}
//async function asyncInner(n:int):int {
    //return n + 1;
//}
//// Test
//var p1:* = asyncAdd(5, 3);
//p1.then(function(v:int) { trace("asyncAdd(5,3) = " + v); });
//var p2:* = asyncHello();
//p2.then(function(v:String) { trace("asyncHello: " + v); });
//var p3:* = returnsPromise();
//p3.then(function(v:int) { trace("returnsPromise: " + v); });
//var p4:* = asyncVoid();
//p4.then(function(v:*) { trace("asyncVoid done"); });
//var p5:* = asyncNest();
//p5.then(function(v:int) { trace("asyncNest: " + v); });
//trace("=== Init Complete ===");



//import geom.Vector2;
//var a:Vector2 = new Vector2(0,1);
//var b = new Vector2(1,0);
//
//a += b;
//
//trace(a);
//trace( a.dot(b) );
//trace( a.cross(b) );
//trace( a * 3 );
//trace( a / 3 );
//
//trace( 6 * b );
//
//trace( +-b);   

//final class BB
//{
	//[operator("%")]
	//static function bsb(i:BB,j:int):String
	//{
		//if(true)
		//{
			//return "BB % " + [j, j].toString() ;
		//}
		//else
		//{
			//throw 3;
		//}
	//}
//}
//
//
//var c = new BB();
//
////var d = c / 3;
////trace(d);
//
//c %= 3;
//trace("c:",c,typeof c);


//var v:Vector.<int> = new <int>[1,2,3,4,9,9,9,9,9,10,11,0,0,0,0,0,0,0,3+3,0,0,0];
//trace(v);


//
//
//async function Go()
//{
	//try
	//{
		//trace( await fetch("https://r.wxyfamily.duckdns.org") );	
		//trace(2);
	//
	//}
	//catch (e)
	//{
		//trace(e);
		//
		//trace( await fetch("http://oa.ofilm.com") );
		//
	//}
//}
//Go();
//


 
//class Test262Error extends Error
//{
	//public function Test262Error(t)
	//{
		//super(t);
	//}
//}



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
  if (typeof func !== "function") {
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

Array.prototype[1] = -1;
var x = [0, 1];
x.length = 2;

var shift = x.shift();
if (shift !== 0) {
  throw new Test262Error('#1: Array.prototype[1] = -1; x = [0,1]; x.length = 2; x.shift() === 0. Actual: ' + (shift));
}

if (x[0] !== 1) {
  throw new Test262Error('#2: Array.prototype[1] = -1; x = [0,1]; x.length = 2; x.shift(); x[0] === 1. Actual: ' + (x[0]));
}

if (x[1] !== -1) {
  throw new Test262Error('#3: Array.prototype[1] = -1; x = [0,1]; x.length = 2; x.shift(); x[1] === -1. Actual: ' + (x[1]));
}
trace('OK');





//while (x < 10)
//lbl1:
//lbl2:
    //x++;
//
	//
	//
	//trace(x);


//aaa:for (;; )
//lbl1:
//lbl2:
    //trace(x);
	
//function  fib(i:int):int 
	//{
		//if (i === 1 || i === 2)
		//{
			//return 1;
		//}
		//else 
		//{
			//
			//return fib(i - 2) + fib(i-1);
			//
		//}	
	//}
	//
	//
	//
//trace(fib(35));



//fid = null;

//trace("fib4 = ",fib(4));



//trace("OK");
