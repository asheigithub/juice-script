using juicescript;
using juicescript.compiler;
using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.callfun
{
    [TestClass]
    public class TestCallMethod19 : CodeTestBase
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
	
	[Doc]
	public class Main
	{
		
		//public static var BBB =  7;
		public function Main(i:int,...a) 
		{
			
		}
		
		public  var F:Function = function ():void 
		{
		
		}
		
		public static function CCC(obj)
		{
		
		}
		
		protected var j:int ;
		
		public var k:Namespace;
		
		private  function set B(i:*) 
		{
			
		}
		
	}
	
}

import flash.utils.IDataInput;


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
	
	function get p1():int;
	
}


class A  implements it2
{
	
	function aaa():void 
	{
		
	}
	
	public function foo():void
	{
		
	}
	
	public function foo2():void 
	{
		
	}
	
	public function get p1():int 
	{
		j = 777;
		return 1,2,j/ 3;
	}
	
	public function set p1(i:int):void
	{
		j = 666;
	}
	
}

internal class C extends A
{
	public override function foo():void 
	{
		//o = null;
	}
	
	public function Tsss():String
	{
		
	}
	
}

var cc:C;

var b:int;
b = cc.Tsss();


var o:it2 ;
o = new C();
var j;

function test(o:II)
{
	o.foo();
	
}


"
				}


                );


            return project;
        }

        protected override void TestIsPass(Player player, PlayerException ex)
        {
            
        }


        [TestMethod]
        public void Test()
        {
			bool israise = false;

			try
			{
				Run();
			}
			catch (ResolverException ex)
			{
				israise = true;
				Assert.AreEqual("Implicit coercion of a value with static type String to a possibly unrelated type int.", ex.Message);
			}

            Assert.IsTrue( israise );
        }

    }
}
