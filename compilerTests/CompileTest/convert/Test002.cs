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
	public sealed class Test002 : CodeTestBase
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
			testImplicitStringConversions();
			testImplicitNumericConversions();
			testImplicitBooleanConversions();
			testImplicitIntegerTypeConversions();
		}
		
		private function testImplicitStringConversions():void
		{
			var s:String;
			var a:*;
			
			a = 123;
			s = a;
			if (s == ""123"") trace(""PASS:implicit int->String"");
			else trace(""FAIL:implicit int->String"" + s);
			
			a = 456.78;
			s = a;
			if (s == ""456.78"") trace(""PASS:implicit Number->String"");
			else trace(""FAIL:implicit Number->String"" + s);
			
			a = true;
			s = a;
			if (s == ""true"") trace(""PASS:implicit Boolean(true)->String"");
			else trace(""FAIL:implicit Boolean(true)->String"" + s);
			
			a = false;
			s = a;
			if (s == ""false"") trace(""PASS:implicit Boolean(false)->String"");
			else trace(""FAIL:implicit Boolean(false)->String"" + s);
			
			a = null;
			s = a;
			if (s === null) trace(""PASS:implicit null->String"");
			else trace(""FAIL:implicit null->String"" + s);
		}
		
		private function testImplicitNumericConversions():void
		{
			var i:int;
			var u:uint;
			var n:Number;
			var a:*;
			
			a = ""123"";
			i = a;
			if (i == 123) trace(""PASS:implicit String->int"");
			else trace(""FAIL:implicit String->int"" + i);
			
			a = ""456"";
			u = a;
			if (u == 456) trace(""PASS:implicit String->uint"");
			else trace(""FAIL:implicit String->uint"" + u);
			
			a = ""789.5"";
			n = a;
			if (n == 789.5) trace(""PASS:implicit String->Number"");
			else trace(""FAIL:implicit String->Number"" + n);
			
			a = 100;
			n = a;
			if (n == 100) trace(""PASS:implicit int->Number"");
			else trace(""FAIL:implicit int->Number"" + n);
			
			a = 200;
			i = a;
			if (i == 200) trace(""PASS:implicit uint->int"");
			else trace(""FAIL:implicit uint->int"" + i);
			
			a = 300.5;
			i = a;
			if (i == 300) trace(""PASS:implicit Number->int"");
			else trace(""FAIL:implicit Number->int"" + i);
			
			a = 400.5;
			u = a;
			if (u == 400) trace(""PASS:implicit Number->uint"");
			else trace(""FAIL:implicit Number->uint"" + u);
		}
		
		private function testImplicitBooleanConversions():void
		{
			var b:Boolean;
			var a:*;
			
			a = 1;
			b = a;
			if (b == true) trace(""PASS:implicit int(1)->Boolean"");
			else trace(""FAIL:implicit int(1)->Boolean"" + b);
			
			a = 0;
			b = a;
			if (b == false) trace(""PASS:implicit int(0)->Boolean"");
			else trace(""FAIL:implicit int(0)->Boolean"" + b);
			
			a = 1.0;
			b = a;
			if (b == true) trace(""PASS:implicit Number(1)->Boolean"");
			else trace(""FAIL:implicit Number(1)->Boolean"" + b);
			
			a = 0.0;
			b = a;
			if (b == false) trace(""PASS:implicit Number(0)->Boolean"");
			else trace(""FAIL:implicit Number(0)->Boolean"" + b);
			
			a = NaN;
			b = a;
			if (b == false) trace(""PASS:implicit NaN->Boolean"");
			else trace(""FAIL:implicit NaN->Boolean"" + b);
			
			a = ""hello"";
			b = a;
			if (b == true) trace(""PASS:implicit non-empty String->Boolean"");
			else trace(""FAIL:implicit non-empty String->Boolean"" + b);
			
			a = """";
			b = a;
			if (b == false) trace(""PASS:implicit empty String->Boolean"");
			else trace(""FAIL:implicit empty String->Boolean"" + b);
			
			a = null;
			b = a;
			if (b == false) trace(""PASS:implicit null->Boolean"");
			else trace(""FAIL:implicit null->Boolean"" + b);
			
			a = undefined;
			b = a;
			if (b == false) trace(""PASS:implicit undefined->Boolean"");
			else trace(""FAIL:implicit undefined->Boolean"" + b);
		}
		
		private function testImplicitIntegerTypeConversions():void
		{
			var i:int;
			var sb:sbyte;
			var b:byte;
			var sh:short;
			var us:ushort;
			var a:*;
			
			a = 127;
			sb = a;
			if (sb == 127) trace(""PASS:implicit int->sbyte"");
			else trace(""FAIL:implicit int->sbyte"" + sb);
			
			a = 255;
			b = a;
			if (b == 255) trace(""PASS:implicit int->byte"");
			else trace(""FAIL:implicit int->byte"" + b);
			
			a = 32767;
			sh = a;
			if (sh == 32767) trace(""PASS:implicit int->short"");
			else trace(""FAIL:implicit int->short"" + sh);
			
			a = 65535;
			us = a;
			if (us == 65535) trace(""PASS:implicit int->ushort"");
			else trace(""FAIL:implicit int->ushort"" + us);
			
			a = sbyte(100);
			i = a;
			if (i == 100) trace(""PASS:implicit sbyte->int"");
			else trace(""FAIL:implicit sbyte->int"" + i);
			
			a = byte(200);
			i = a;
			if (i == 200) trace(""PASS:implicit byte->int"");
			else trace(""FAIL:implicit byte->int"" + i);
			
			a = short(30000);
			i = a;
			if (i == 30000) trace(""PASS:implicit short->int"");
			else trace(""FAIL:implicit short->int"" + i);
			
			a = ushort(60000);
			i = a;
			if (i == 60000) trace(""PASS:implicit ushort->int"");
			else trace(""FAIL:implicit ushort->int"" + i);
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
				
				Assert.AreEqual(29, passCount, "Expected 29 passes, got: " + passCount + " output: " + output);
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