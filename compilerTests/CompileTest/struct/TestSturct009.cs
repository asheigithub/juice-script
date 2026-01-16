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
	public class TestSturct009 : CodeTestBase
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
		return ""{ w="" + w + "",n="" + n + "",p="" + p + ""}""  ;
	}
	
}


var v:Vector.<O> = new Vector.<O>(3);

v[1].i = 55;
v[1].vec = null;
v[1].b = true;

trace(v);


var o = new O(); //o.i = 100;

var w = new W()
w.n = -5;

o.vec.y = w;
o.i = 4;

trace(o.vec.y.n,o);// .n);

"
				}


				);


			return project;
		}

		protected override void TestIsPass(Player player, PlayerException ex)
		{
			Assert.IsNull(ex);

			Assert.AreEqual("{ i=0, vec={ x=1,y={ w=true,n=3,p=5},z=3}, b=false},{ i=55, vec={ x=1,y={ w=true,n=3,p=5},z=3}, b=true},{ i=0, vec={ x=1,y={ w=true,n=3,p=5},z=3}, b=false}\r\n-5 { i=4, vec={ x=1,y={ w=true,n=-5,p=5},z=3}, b=false}\r\n", ((StringPrint)player.Print).GetOutput());

		}


		[TestMethod]
		public void Test()
		{
			Run();
		}
	}
}
