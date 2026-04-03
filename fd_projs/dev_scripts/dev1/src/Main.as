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
		public function Main()
		{
			 var i:int = 10;
            var u:uint = 5;
            var s:short = 2;
            var sb:sbyte = 2;
            var b:byte = 3;
            var f:float = 3.0;
            
            trace("=== int <op> int ===");
            trace(i + i, i - i, i * i, i / i, i % i);
            
            trace("=== int <op> uint ===");
            trace(i + u, i - u, i * u, i / u, i % u);
            
            trace("=== int <op> short ===");
            trace(i + s, i - s, i * s, i / s, i % s);
            
            trace("=== int <op> sbyte ===");
            trace(i + sb, i - sb, i * sb, i / sb, i % sb);
            
            trace("=== int <op> byte ===");
            trace(i + b, i - b, i * b, i / b, i % b);
            
            trace("=== int <op> float ===");
            trace(i + f, i - f, i * f, i / f, i % f);
            
            trace("=== uint <op> uint ===");
            trace(u + u, u - u, u * u, u / u, u % u);
            
            trace("=== uint <op> int ===");
            trace(u + i, u - i, u * i, u / i, u % i);
            
            trace("=== uint <op> short ===");
            trace(u + s, u - s, u * s, u / s, u % s);
            
            trace("=== uint <op> sbyte ===");
            trace(u + sb, u - sb, u * sb, u / sb, u % sb);
            
            trace("=== uint <op> byte ===");
            trace(u + b, u - b, u * b, u / b, u % b);
            
            trace("=== uint <op> float ===");
            trace(u + f, u - f, u * f, u / f, u % f);
            
            trace("=== short <op> short ===");
            trace(s + s, s - s, s * s, s / s, s % s);
            
            trace("=== short <op> int ===");
            trace(s + i, s - i, s * i, s / i, s % i);
            
            trace("=== short <op> uint ===");
            trace(s + u, s - u, s * u, s / u, s % u);
            
            trace("=== short <op> sbyte ===");
            trace(s + sb, s - sb, s * sb, s / sb, s % sb);
            
            trace("=== short <op> byte ===");
            trace(s + b, s - b, s * b, s / b, s % b);
            
            trace("=== short <op> float ===");
            trace(s + f, s - f, s * f, s / f, s % f);
            
            trace("=== sbyte <op> sbyte ===");
            trace(sb + sb, sb - sb, sb * sb, sb / sb, sb % sb);
            
            trace("=== sbyte <op> int ===");
            trace(sb + i, sb - i, sb * i, sb / i, sb % i);
            
            trace("=== sbyte <op> uint ===");
            trace(sb + u, sb - u, sb * u, sb / u, sb % u);
            
            trace("=== sbyte <op> short ===");
            trace(sb + s, sb - s, sb * s, sb / s, sb % s);
            
            trace("=== sbyte <op> byte ===");
            trace(sb + b, sb - b, sb * b, sb / b, sb % b);
            
            trace("=== sbyte <op> float ===");
            trace(sb + f, sb - f, sb * f, sb / f, sb % f);
            
            trace("=== byte <op> byte ===");
            trace(b + b, b - b, b * b, b / b, b % b);
            
            trace("=== byte <op> int ===");
            trace(b + i, b - i, b * i, b / i, b % i);
            
            trace("=== byte <op> uint ===");
            trace(b + u, b - u, b * u, b / u, b % u);
            
            trace("=== byte <op> short ===");
            trace(b + s, b - s, b * s, b / s, b % s);
            
            trace("=== byte <op> sbyte ===");
            trace(b + sb, b - sb, b * sb, b / sb, b % sb);
            
            trace("=== byte <op> float ===");
            trace(b + f, b - f, b * f, b / f, b % f);
            
            trace("=== float <op> float ===");
            trace(f + f, f - f, f * f, f / f, f % f);
            
            trace("=== float <op> int ===");
            trace(f + i, f - i, f * i, f / i, f % i);
            
            trace("=== float <op> uint ===");
            trace(f + u, f - u, f * u, f / u, f % u);
            
            trace("=== float <op> short ===");
            trace(f + s, f - s, f * s, f / s, f % s);
            
            trace("=== float <op> sbyte ===");
            trace(f + sb, f - sb, f * sb, f / sb, f % sb);
            
            trace("=== float <op> byte ===");
            trace(f + b, f - b, f * b, f / b, f % b);
            
            trace("=== int literal <op> float literal ===");
            trace(10 + 3.0, 10 - 3.0, 10 * 3.0, 10 / 3.0, 10 % 3.0);
            
            trace("=== float literal <op> int literal ===");
            trace(10.0 + 3, 10.0 - 3, 10.0 * 3, 10.0 / 3, 10.0 % 3);
            
            trace("=== NaN ops ===");
            var nan:float = 0.0 / 0.0;
            trace("NaN + int:", nan + 5);
            trace("NaN - int:", nan - 5);
            trace("NaN * int:", nan * 5);
            trace("NaN / int:", nan / 5);
            trace("int + NaN:", 5 + nan);
            trace("int * NaN:", 5 * nan);
            
            trace("=== Infinity ops ===");
            var inf:float = 1.0 / 0.0;
            var negInf:float = -1.0 / 0.0;
            trace("Inf + int:", inf + 5);
            trace("Inf - int:", inf - 5);
            trace("Inf * int:", inf * 5);
            trace("Inf / int:", inf / 5);
            trace("Inf + Inf:", inf + inf);
            trace("Inf - Inf:", inf - inf);
            trace("Inf * Inf:", inf * inf);
            trace("Inf / Inf:", inf / inf);
            trace("Inf + NegInf:", inf + negInf);
            trace("NegInf * 2:", negInf * 2);
            trace("NegInf / 2:", negInf / 2);
            trace("5 / 0:", 5.0 / 0.0);
            trace("-5 / 0:", -5.0 / 0.0);
            
            trace("=== NaN/Inf with other types ===");
            trace("NaN + float:", nan + f);
            trace("Inf * float(2):", inf * 2.0);
            trace("float(5) / 0:", float(5) / 0.0);
            trace("NaN * float:", nan * f);
            
            trace("All tests completed!");
  
		}
		
	}
}
 
var main:Main = new Main();


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
