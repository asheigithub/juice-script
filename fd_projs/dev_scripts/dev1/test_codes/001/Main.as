
package
{
	
	//use namespace AS3;
	[Doc]
	public class Main extends BaseM
	{
		
		//public static var BBB =  7;
		public function Main() 
		{
			super()
			
			//F(1);
			//o = ABC;
			
			//this.CCC();
			
			//this.CCC(666);
			
			//var ccc = Class2["SBAFF"];
			
			//o = delete Class2["SBAFF"]; //["ABC"];
			
			//ccc(88);
			
			//ccc();
			
			//public::["CCC"]();
			
			//internal::F();
			//public::F();
			
			//SBAFF(this);
			
			//this["CCC"]();
			
			//Main.public::["CCC"](this);
			
			//B.Y = 5;
			
			//o = j = B = 5;
			//trace(j,this.B);
			
			//o = Main.private::["B"] = 7;
			
			//this["B"];
			
			
		}
		
		public  var F:Function = function ():void 
		{
		//o = this;
		
		}
		
		public static function CCC(obj)
		{
			SBAFF(obj);
			obj.j = 777;
		}
		
		protected var j:int ;
		
		public var k:Namespace;
		
		//TNS override function ABC( b= 2 ) :void
		//{
			//
		//}
		
		//public var ABC:Function = function ():void 
		//{
			//
		//}
					
		private  function set B(i:*) 
		{
			
		}
		
	}
	
}

//Main.prototype.jjj = 5;

import flash.utils.IDataInput;
import ns1.TNS;
/*
var jm = new Main(Main);

new Main(0);
var o;
var p;
//trace(o);

var c = new (function(){
	this.bb = 555;
	this.cc = (arguments);
})(1,2,3);

trace(c, c.bb );

trace( jm.constructor );

*/

interface II
{
	
	function foo():void; 
}

interface it
{
	function foo():void;
}

interface it2 extends it,II,II
{
	function foo2():void;
	
	function get p1():Object;
	
	//function set p1(i:int):void;
}


class A  implements it2
{
	
	function A():void 
	{
		
	}
	
	public function foo():void
	{
		
	}
	
	public function foo2():void 
	{
		
	}
	var seed = new Object();
	public function get p1():Object 
	{
		return seed;
	}
	
	public function set p1(i:Object):void
	{
		
	}
	
}

internal class C extends A
{
	public function C()
	{
		super();
	}
	
	public override function foo():void 
	{
		
		//o = null;
	}
	
	TNS var j:int;
	
	public function Tsss():int
	{
		return 0;	
	}
	
	public function M():*
	{
		return function ():void 
		{
			o = null;
		}
	}
	
	public function M2()
	{
		return null;
	}
	
}

var o:C = new C();
o.M2()();


/*
var o = new Object();

o.valueOf = function ()
{
	return function ():void 
	{
		o = 0;
	}
}

o.valueOf()();

o.valueOf()();

*/

//var o:it2 ;
//o = new C();
//
//o.p1["3"] = 6;
//
//delete o.p1["3"];


//test(o);
//var c:int = o.p1;




//trace(j);

//var a = o.p1;


//o.foo2();

//trace(c.prototype);

//new Main().TNS::ABC();

//new Main().B = 6;


//var t;
//new Main().F(3);


//var a = function ():void 
//{
	//
//};


//var a:Vector.<int>;
//
//this["a"] = new Vector.<String>;



//var a;
//var c;
//(function ():void   
//{
	//a = arguments;
	//c = arguments.callee;
//})(1,2,3);
//
//c(6,7);

  
//trace(a);


//var b:int;
//var c:Array;
//var d;
//var a:Function = function(i:Class,j:*,...a)
//{
	//j();
	//
	//b = j;
	//c = a;
	//
//}
//
//a(String, function(){
	//d = this;
	//
//}, 2, 3, 4);


//var o;
//var o = new Object();
//o.f = a;

//o.f( int , 5);// , null , 666, "a", "gg" );


//(1,2,function b(i, j)
//{
	////trace(i, j);
//})();

//
//var m = new Array();
////m.y = 8;
////m[ -9] = 9;
////m[0.0] = 0;
//m[1.1] = 77;
//
//m[null] = 6;
//m[undefined] = 7;



//import ns1.TNS;
//import ns1.n2.N2Cls;
//import ns1.Class2;
//dynamic class OO
//{
	//
//}
//
//var o = new OO();
//
//o.U = 5;
//o.K = 6;
//o.C = 7;
//o.D = 8;
//
//var b = new Object();
//b.U = 0;
//b.C = 9;
////b.D = 7;
//
//
//namespace n2;
//
//var m = new Main();
//
//delete o.K;
//
//delete b.U;
//
//b.C = 10;
//
//
////Namespace
//
////var v:Array = new Array();
//
  //
////var i:int = v;




var Y = function (recuse) 
{
	function helper(self) 
	{
		return recuse( function (x) 
		{
			return self(self)(x);
		} );		
	}
	return helper(helper);
}

var fact = Y(
   function (recuse) 
   {
	   return function (n) 
	   {
		   return n == 0?1:n * recuse(n-1);
	   }
   } 
);

//trace( fact(10) );