package 
{
	import flash.display.Sprite;
	import flash.utils.clearInterval;
	import flash.utils.setInterval;
	import ns1.BaseM;
	
    [Doc]
    public class Main extends Sprite {
        public function Main() {
            
        }
    }

}

[struct]
final class Point {
    public var x:int = 0;
    public var y:int = 0;
}

function runTest():void {
    var results:Array = [];

    var v1:Vector.<int> = new <int>[1, 3, 5, 7];
    results.push(v1.some(function(item:int,...r):Boolean {
        return item % 2 == 0;
    }) ? 0 : 1);

    var v2:Vector.<int> = new <int>[1, 2, 3, 4, 5];
    results.push(v2.some(function(item:int,...r):Boolean {
        return item > 3;
    }) ? 1 : 0);

    var v3:Vector.<int> = new <int>[1, 2, 3];
    results.push(v3.some(function(item:int,...r):Boolean {
        return item > 10;
    }) ? 0 : 1);

    var v4:Vector.<int> = new <int>[];
    results.push(v4.some(function(item:int,...r):Boolean {
        return true;
    }) ? 0 : 1);

    var v5:Vector.<int> = new <int>[10, 20, 30];
    var myObj:Object = { threshold: 25 };
    results.push(v5.some(function(item:int,...r):Boolean {
        return item > this.threshold;
    }, myObj) ? 1 : 0);

    var p1:Point = new Point();
    p1.x = 2; p1.y = 4;
    var p2:Point = new Point();
    p2.x = 4; p2.y = 6;
    var p3:Point = new Point();
    p3.x = 6; p3.y = 8;
    var v6:Vector.<Point> = new <Point>[p1, p2, p3];
    results.push(v6.some(function(item:Point,...r):Boolean {
        return item.x < 0;
    }) ? 0 : 1);

    var p4:Point = new Point();
    p4.x = -1; p4.y = 6;
    var p5:Point = new Point();
    p5.x = 4; p5.y = 8;
    var p6:Point = new Point();
    p6.x = 6; p6.y = 10;
    var v7:Vector.<Point> = new <Point>[p4, p5, p6];
    results.push(v7.some(function(item:Point,...r):Boolean {
        return item.x < 0;
    }) ? 1 : 0);

    var v8:Vector.<Point> = new <Point>[];
    results.push(v8.some(function(item:Point,...r):Boolean {
        return true;
    }) ? 0 : 1);

    var v9:Vector.<int> = new <int>[1, 2];
    var r9:Boolean = v9.some(function(item:int,...r):Boolean {
        if (item == 1) {
            v9.push(3);
        }
        return item == 2;
    });
    results.push((r9 == true && v9.length == 3) ? 1 : 0);

    var v10:Vector.<int> = new <int>[1, 2, 3, 4, 5];
    var visited10:String = '';
    var r10:Boolean = v10.some(function(item:int,...r):Boolean {
        visited10 += item + ',';
        if (item == 2) {
            v10.pop();
            v10.pop();
        }
        return item == 2;
    });
    results.push((visited10 == '1,2,' && v10.length == 3 && r10 == true) ? 1 : 0);

    var v11:Vector.<int> = new <int>[1, 2];
    var visited11:String = '';
    var cb11 = function(i:int, idx:int, vec:*):Boolean {
        visited11 += i + ',';
        if (idx == 0) {
            v11 = new <int>[100];
        }
        return false;
    };
    var r11:Boolean = v11.some(cb11);
    results.push((visited11 == '1,2,' && r11 == false) ? 1 : 0);

    var separator:String = ',';
    trace(results[0] + separator + results[1] + separator + results[2] + separator + results[3] + separator + results[4] + separator + results[5] + separator + results[6] + separator + results[7] + separator + results[8] + separator + results[9] + separator + results[10]);
}
runTest();


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



//class Test262Error extends Error
//{
	//var a;
	//public function Test262Error(t=undefined)
	//{
		//super(t);
	//}
//}
//
//function assert(mustBeTrue, message = undefined) {
  //if (mustBeTrue === true) {
    //return;
  //}
//
  //if (message === undefined) {
    //message = 'Expected true but got ' + assert._toString(mustBeTrue);
  //}
  //throw new Test262Error(message);
//}
//
//assert._toString = function (v:String) 
//{
	//return v;
//}
//
//assert._isSameValue = function (a, b) {
  //if (a === b) {
    //// Handle +/-0 vs. -/+0
    //return a !== 0 || 1 / a === 1 / b;
  //}
//
  //// Handle NaN vs. NaN
  //return a !== a && b !== b;
//};
//
//assert.sameValue = function (actual, expected, message) {
  //try {
    //if (assert._isSameValue(actual, expected)) {
      //return;
    //}
  //} catch (error) {
    //throw new Test262Error(message + ' (_isSameValue operation threw) ' + error);
    //return;
  //}
//
  //if (message === undefined) {
    //message = '';
  //} else {
    //message += ' ';
  //}
//
  //message += 'Expected SameValue(«' + assert._toString(actual) + '», «' + assert._toString(expected) + '») to be true';
//
  //throw new Test262Error(message);
//};
//
//assert.notSameValue = function (actual, unexpected, message) {
  //if (!assert._isSameValue(actual, unexpected)) {
    //return;
  //}
//
  //if (message === undefined) {
    //message = '';
  //} else {
    //message += ' ';
  //}
//
  //message += 'Expected SameValue(«' + assert._toString(actual) + '», «' + assert._toString(unexpected) + '») to be false';
//
  //throw new Test262Error(message);
//};
//
//assert.throws = function (expectedErrorConstructor, func, message) {
  //var expectedName, actualName;
  //if (typeof func !== "function") {
    //throw new Test262Error('assert.throws requires two arguments: the error constructor ' +
      //'and a function to run');
    //return;
  //}
  //if (message === undefined) {
    //message = '';
  //} else {
    //message += ' ';
  //}
//
  //try {
    //func();
  //} catch (thrown) {	  
	  //trace(thrown.name); 
    //if (typeof thrown !== 'object' || thrown === null) {
      //message += 'Thrown value was not an object!';
      //throw new Test262Error(message);
    //} else if (thrown.constructor !== expectedErrorConstructor) {
      //expectedName = expectedErrorConstructor.name;
      //actualName = thrown.constructor.name;
      //if (expectedName === actualName) {
        //message += 'Expected a ' + expectedName + ' but got a different error constructor with the same name';
      //} else {
        //message += 'Expected a ' + expectedName + ' but got a ' + actualName;
      //}
      //throw new Test262Error(message);
    //}
    //return;
  //}
//
  //message += 'Expected a ' + expectedErrorConstructor.name + ' to be thrown but no exception was thrown at all';
  //throw new Test262Error(message);
//};




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
