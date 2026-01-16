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
    public class TestTypeConv6 : CodeTestBase
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
o.toString = function () 
{
	trace(arguments.callee == o.toString);
	return ""ABCD"";
}

trace( String(o) );

function b(s:String,k:String ,...p)
{
	trace(s, p);
	
	var d:String = o;
	
	trace(d,typeof d);
	
	k = o;
	
	trace(k, typeof k);
	
}

b(o,null,1,2,3);

a = o;
trace(a , typeof a);

trace(A.K, typeof A.K);

A.K = o;

trace(A.K, typeof A.K);

A.Test();

class C
{
	public var ccc:String;
	
	public var bbb:String;
	
	
	
	public function Test()
	{
		trace(bbb, typeof bbb);
		
		bbb = o;
			
		trace(bbb, typeof bbb);
		
		trace(o);
	}
	
}

var JJJ:String;

class D
{
	public function D()
	{
		JJJ = o;
		trace(""jjj"", JJJ,typeof JJJ);
	}
}

class E extends D
{
	
}


var cc = new C();
trace(cc.ccc, typeof cc.ccc);
cc.ccc = o;
trace(cc.ccc, typeof cc.ccc);

cc.Test();

new E();


trace(""OK"");

"
				}


                );


            return project;

        }

        protected override void TestIsPass(Player player, PlayerException ex)
        {
            

            Assert.IsNull(ex);
			//null 肯定是 object,这里flash的实现应该不正确
            Assert.AreEqual("true\r\nABCD\r\ntrue\r\nABCD 1,2,3\r\ntrue\r\nABCD string\r\ntrue\r\nABCD string\r\ntrue\r\nABCD string\r\nnull object\r\ntrue\r\nABCD string\r\ntrue\r\nABCD string\r\nnull object\r\ntrue\r\nABCD string\r\nnull object\r\ntrue\r\nABCD string\r\ntrue\r\nABCD\r\ntrue\r\njjj ABCD string\r\nOK\r\n", ((StringPrint) player.Print).output.ToString());

        }


        [TestMethod]
        public void Test()
        {
            Run();
        }

    }
}
