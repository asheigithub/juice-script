using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.in_
{
	[TestClass]
	public sealed class Test001 : CodeTestBase
	{
		protected override TestCodeProject LoadProject()
		{
			TestCodeProject project = new TestCodeProject();

			project.libs = [Juice_GlobalSwc];

			project.testCodes = new List<TestCodeFile>();

			project.testCodes.Add(
				new TestCodeFile()
				{
					Path = "BaseM.as",
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
	import flash.display.Sprite;
	import ns1.BaseM;
	
	[Doc]
	/**
	 * ...
	 * @author 
	 */
	public class Main extends BaseM
	{
		
	}
	
}


var c = Class;
c.prototype[""F""] = 1;

trace( ""F"" in Number );

Number[""G""] = 2;

trace( ""G"" in Number );

trace( ""G"" in 33.0 );
trace( ""F"" in 33.0 );

Number.prototype.H = 3;
trace(""H"" in Number);
trace(""H"" in 44.0);

this[""I""] = 4;

trace( ""I"" in this );

trace( ""length"" in String );
trace( ""length"" in """" );

class A
{
	public var a;
	internal var b;
}

A.prototype.c = """";

trace( ""a"" in new A() );
trace( ""b"" in new A() );
trace( ""c"" in new A() );

trace( ""uri"" in AS3 );

function t() 
{
	
}

t[""666""] = 666;

trace( ""call"" in t);
trace( ""666"" in t);


var v:Vector.<int> = new <int>[5,6,7];

Vector.<int>.prototype[""8""] = 5;
Vector.prototype[""i""] = 6;
trace( 0 in v  );
trace( 1.0 in v  );
trace( -1 in v  );
trace( 8 in v  );
trace( ""8"" in v  );
trace( ""i"" in v  );




"
				}


				);


			return project;
		}

		protected override void TestIsPass(Player player, PlayerException ex)
		{
			player.ForceGC();
			{
				var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Main");
				Assert.IsNotNull(global);
				var globalInstance = player.Context.GC.Heap[global.__global_index__];
				Assert.IsNotNull(globalInstance);
				Assert.IsNull(ex);

				RtScriptClass rtPayload = (RtScriptClass)globalInstance;

				StringPrint print = (StringPrint)player.Print;

				Assert.AreEqual("true\r\ntrue\r\nfalse\r\nfalse\r\nfalse\r\ntrue\r\ntrue\r\ntrue\r\ntrue\r\ntrue\r\nfalse\r\ntrue\r\ntrue\r\ntrue\r\ntrue\r\ntrue\r\ntrue\r\nfalse\r\nfalse\r\ntrue\r\nfalse\r\n", print.GetOutput());

			}


		}


		[TestMethod]
		public void Test()
		{
			Run();
		}
	}
}
