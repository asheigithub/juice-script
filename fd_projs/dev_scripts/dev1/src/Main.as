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
var testMain:Main = new Main();

var testMain:Main = new Main();

function runTest():void {
    var v:Vector.<int> = new <int>[1,2];
	v.insertAt(1, 3);
	
	trace(v);
	
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
