using juicescript;
using juicescript.ABC;
using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.typeconvert
{
    [TestClass]
    public class TestTypeConv9 : CodeTestBase
    {
        protected override TestCodeProject LoadProject()
        {
            TestCodeProject project = new TestCodeProject();

            project.libs = [Juice_GlobalSwc];

            project.testCodes = new List<TestCodeFile>();

            project.testCodes.Add(
                new TestCodeFile()
                { 
                    Path = "ns1/BaseM.as",
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
		
		public static function Test(k)
		{
			trace( k + "" 4"");
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
	
	//use namespace AS3;
	import flash.display.Sprite;
	import ns1.Class2;
	import ns1.TNS;
	[Doc]
	public class Main extends Sprite
	{
		
		//public static var BBB =  7;
		public function Main() 
		{
			
		}
		
		public var j:int ;
		
		
		
	}
}

class A
{
	public static var K:String;
	
	public static var O:String;
	
	public static function Test()
	{
		O = o;
		
		trace(O, typeof O);
	}
	
	
}

var a:String;
var o = {};
o.valueOf = function () 
{
	trace(arguments.callee == o.valueOf);
	return 6;
}
o.toString = function () 
{
	trace(""tostring"");
	return o;
}

var i:int ;
i = o;
trace(i);

var j:uint;
j = o;
trace(j);

var k:byte;
k = o;
trace(k);

var l:sbyte;
l = o;
trace(l);

var m:short;
m = o;
trace(m);

var n:ushort;
n = o;
trace(n);

var p:float;
p = o;
trace(p);

var q:Number;
q = o;
trace(q);


"
				}


                );


            return project;

        }

        protected override void TestIsPass(Player player, PlayerException ex)
        {
            

            Assert.IsNull(ex);
			//null 肯定是 object,这里flash的实现应该不正确
            Assert.AreEqual("true\r\n6\r\ntrue\r\n6\r\ntrue\r\n6\r\ntrue\r\n6\r\ntrue\r\n6\r\ntrue\r\n6\r\ntrue\r\n6\r\ntrue\r\n6\r\n", ((StringPrint) player.Print).output.ToString());

        }


        [TestMethod]
        public void Test()
        {
            Run();
        }

    }
}
