using juicescript;
using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.Struct
{
	[TestClass]
	public class TestSturct018 : CodeTestBase
	{
		protected override TestCodeProject LoadProject()
		{
			TestCodeProject project = new TestCodeProject();

			project.libs = [Juice_GlobalSwc];

			project.testCodes = new List<TestCodeFile>();

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
	public class Main extends Sprite
	{
		public var v;
		public function Main()
		{
			
		}
	}
	
}


[struct]
final class O
{
	public var i:int;
	
	public var vec:V;
	
	public var b:Boolean;
	
	public function toString() 
	{
		return ""{ i="" + i + "", vec="" + vec + "", b="" + b+ ""}"";
	}
	
}

[struct]
final class V
{
	public var x:short = 1;
	public var y:W;
	public var z:float = 3;
	
	public function toString()
	{
		return ""{ x="" + x + "",y="" +y +"",z="" +z+""}"";
	}
	
}

[struct]
final class W
{
	public var w:Boolean = true;	
	
	public var n:short = 3;
	public var p:short = 5;
	
	public function toString()
	{
		return ""{ w="" + w + "",n="" + n + "",p="" + p + ""}"" ;
	}
	
}


(
function B():void 
{
	var o = {};
	o.toString = function () 
	{
		trace(""toString"");
		return ""bb"";
	}
	
	o.valueOf = function () 
	{
		trace(""valueOf"");
		return o;
	}
	
	//var v:Vector.<O> = new Vector.<O>(3); v[0] = new O();
	//B.prototype = O;
	
	//trace( v instanceof Vector.<int> );
	
	var v:Vector.<O> = new Vector.<O>(4);
	
	function m(a)
	{
		
		a.n = 6;	
		trace(a);
	}
	
	m(v[1].vec.y);
	
	trace(v);
	
	v[1].vec.x = 999;
	v[1].vec.y.p = 666;
	
	var b = v[1];
	m(b.vec.y);
	
	trace(b);
	
	//v[0].i = 9;
	//v[1][""""i""""] = 10;
	//v[""""2""""].i = 11;
	
	
	//trace(v);
	//trace(v[0].vec == v[1].vec);
	
}
)();



"
				}


				);


			return project;
		}

		protected override void TestIsPass(Player player, PlayerException ex)
		{
			Assert.IsNull(ex);

			
			player.ForceGC();

			Assert.AreEqual("{ w=true,n=6,p=5}\r\n{ i=0, vec={ x=1,y={ w=true,n=3,p=5},z=3}, b=false},{ i=0, vec={ x=1,y={ w=true,n=3,p=5},z=3}, b=false},{ i=0, vec={ x=1,y={ w=true,n=3,p=5},z=3}, b=false},{ i=0, vec={ x=1,y={ w=true,n=3,p=5},z=3}, b=false}\r\n{ w=true,n=6,p=666}\r\n{ i=0, vec={ x=999,y={ w=true,n=3,p=666},z=3}, b=false}\r\n", ((StringPrint)player.Print).GetOutput());

		}


		[TestMethod]
		public void Test()
		{
			Run();
		}
	}
}
