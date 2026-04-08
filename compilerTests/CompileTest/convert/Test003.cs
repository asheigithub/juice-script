using juicescript;
using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.convert
{
	[TestClass]
	public sealed class Test003 : CodeTestBase
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
					Code = @"package 
{
	import flash.display.Sprite;

	[Doc]
	public class Main extends Sprite
	{
		public function Main()
		{
			testExplicitCast();
			testToPrimitive();
			testToPrimitiveHint();
		}
		
		private function testExplicitCast():void
		{
			var n:int = 100;
			var o:Object = Object(n);
			if (int(o) == 100) trace(""PASS:Object(int(100))"");
			else trace(""FAIL:Object(int(100))"" + int(o));
			
			var s:String = ""200"";
			var o2:Object = Object(s);
			if (String(o2) == ""200"") trace(""PASS:Object(String(200))"");
			else trace(""FAIL:Object(String(200))"" + String(o2));
			
			var b:Boolean = true;
			var o3:Object = Object(b);
			if (Boolean(o3) == true) trace(""PASS:Object(Boolean(true))"");
			else trace(""FAIL:Object(Boolean(true))"" + Boolean(o3));
			
			var n2:Number = 3.14;
			var o4:Object = Object(n2);
			if (Number(o4) == 3.14) trace(""PASS:Object(Number(3.14))"");
			else trace(""FAIL:Object(Number(3.14))"" + Number(o4));
		}
		
		private function testToPrimitive():void
		{
			var obj:Object = {valueOf: function():int { return 42; }};
			var n:Number = Number(obj);
			if (n == 42) trace(""PASS:Object with valueOf->Number"");
			else trace(""FAIL:Object with valueOf->Number"" + n);
			
			var obj2:Object = {toString: function():String { return ""hello""; }};
			var s:String = String(obj2);
			if (s == ""hello"") trace(""PASS:Object with toString->String"");
			else trace(""FAIL:Object with toString->String"" + s);
			
			var obj3:Object = {valueOf: function():int { return 100; }, toString: function():String { return ""custom""; }};
			var s2:String = String(obj3);
			if (s2 == ""custom"") trace(""PASS:Object with both valueOf and toString->String"");
			else trace(""FAIL:Object with both valueOf and toString->String"" + s2);
		}
		
		private function testToPrimitiveHint():void
		{
			var obj:Object = {valueOf: function():int { return 42; }, toString: function():String { return ""str""; }};
			
			var n:Number = Number(obj);
			if (n == 42) trace(""PASS:ToPrimitive(Number hint) uses valueOf"");
			else trace(""FAIL:ToPrimitive(Number hint)"" + n);
			
			var obj2:Object = {valueOf: function():int { return 42; }, toString: function():String { return ""str""; }};
			
			var s:String = String(obj2);
			if (s == ""str"") trace(""PASS:ToPrimitive(String hint) uses toString"");
			else trace(""FAIL:ToPrimitive(String hint)"" + s);
		}
	}
}

var main:Main = new Main();
"
				}
			);

			return project;
		}

		protected override void TestIsPass(Player player, PlayerException ex)
		{
			player.ForceGC();
			{
				Assert.IsNull(ex);

				StringPrint print = (StringPrint)player.Print;
				string output = print.GetOutput();
				
				string[] lines = output.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
				
				int passCount = 0;
				int failCount = 0;
				
				foreach (var line in lines)
				{
					if (line.StartsWith("PASS:"))
					{
						passCount++;
					}
					else if (line.StartsWith("FAIL:"))
					{
						failCount++;
					}
				}
				
				Assert.AreEqual(9, passCount, "Expected 9 passes, got: " + passCount + " output: " + output);
				Assert.AreEqual(0, failCount, "Expected 0 failures, got: " + failCount + " output: " + output);
			}
		}

		[TestMethod]
		public void Test()
		{
			Run();
		}
	}
}