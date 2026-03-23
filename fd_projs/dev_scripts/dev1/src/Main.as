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
			
			//	super();
			
			var a = 1+1;
		}
		
	}
}


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
