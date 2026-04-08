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
					Path = "Main.as",
					Code = @"package 
{
	import flash.display.Sprite;

	[Doc]
	public class Main extends Sprite
	{
		public function Main()
		{
			testStringConversions();
			testNumericConversions();
			testBooleanConversions();
			testIntegerTypeConversions();
			testFloatConversions();
		}
		
		private function testStringConversions():void
		{
			var a:int = 123;
			var s:String = String(a);
			if (s == ""123"") trace(""PASS:int->String"");
			else trace(""FAIL:int->String"" + s);
			
			var a2:uint = 456;
			var s2:String = String(a2);
			if (s2 == ""456"") trace(""PASS:uint->String"");
			else trace(""FAIL:uint->String"" + s2);
			
			var a3:Number = 789.5;
			var s3:String = String(a3);
			if (s3 == ""789.5"") trace(""PASS:Number->String"");
			else trace(""FAIL:Number->String"" + s3);
			
			var b:Boolean = true;
			var s4:String = String(b);
			if (s4 == ""true"") trace(""PASS:Boolean(true)->String"");
			else trace(""FAIL:Boolean(true)->String"" + s4);
			
			var b2:Boolean = false;
			var s5:String = String(b2);
			if (s5 == ""false"") trace(""PASS:Boolean(false)->String"");
			else trace(""FAIL:Boolean(false)->String"" + s5);
		}
		
		private function testNumericConversions():void
		{
			var s:String = ""123"";
			var a:int = int(s);
			if (a == 123) trace(""PASS:String->int"");
			else trace(""FAIL:String->int"" + a);
			
			var s2:String = ""-456"";
			var a2:int = int(s2);
			if (a2 == -456) trace(""PASS:String(-456)->int"");
			else trace(""FAIL:String(-456)->int"" + a2);
			
			var s3:String = ""456"";
			var a3:uint = uint(s3);
			if (a3 == 456) trace(""PASS:String->uint"");
			else trace(""FAIL:String->uint"" + a3);
			
			var s4:String = ""123.45"";
			var a4:Number = Number(s4);
			if (a4 == 123.45) trace(""PASS:String->Number"");
			else trace(""FAIL:String->Number"" + a4);
			
			var n:Number = 123.9;
			var i:int = int(n);
			if (i == 123) trace(""PASS:Number(123.9)->int"");
			else trace(""FAIL:Number(123.9)->int"" + i);
			
			var n2:Number = NaN;
			var i2:int = int(n2);
			if (i2 == 0) trace(""PASS:NaN->int"");
			else trace(""FAIL:NaN->int"" + i2);
			
			var n3:Number = 456.7;
			var u:uint = uint(n3);
			if (u == 456) trace(""PASS:Number(456.7)->uint"");
			else trace(""FAIL:Number(456.7)->uint"" + u);
			
			var i3:int = 789;
			var n4:Number = Number(i3);
			if (n4 == 789) trace(""PASS:int->Number"");
			else trace(""FAIL:int->Number"" + n4);
			
			var u2:uint = 1000;
			var n5:Number = Number(u2);
			if (n5 == 1000) trace(""PASS:uint->Number"");
			else trace(""FAIL:uint->Number"" + n5);
		}
		
		private function testBooleanConversions():void
		{
			var i:int = 1;
			var b:Boolean = Boolean(i);
			if (b == true) trace(""PASS:int(1)->Boolean"");
			else trace(""FAIL:int(1)->Boolean"" + b);
			
			var i2:int = 0;
			var b2:Boolean = Boolean(i2);
			if (b2 == false) trace(""PASS:int(0)->Boolean"");
			else trace(""FAIL:int(0)->Boolean"" + b2);
			
			var i3:int = -5;
			var b3:Boolean = Boolean(i3);
			if (b3 == true) trace(""PASS:int(-5)->Boolean"");
			else trace(""FAIL:int(-5)->Boolean"" + b3);
			
			var u:uint = 1;
			var b4:Boolean = Boolean(u);
			if (b4 == true) trace(""PASS:uint(1)->Boolean"");
			else trace(""FAIL:uint(1)->Boolean"" + b4);
			
			var u2:uint = 0;
			var b5:Boolean = Boolean(u2);
			if (b5 == false) trace(""PASS:uint(0)->Boolean"");
			else trace(""FAIL:uint(0)->Boolean"" + b5);
			
			var n:Number = 1;
			var b6:Boolean = Boolean(n);
			if (b6 == true) trace(""PASS:Number(1)->Boolean"");
			else trace(""FAIL:Number(1)->Boolean"" + b6);
			
			var n2:Number = 0;
			var b7:Boolean = Boolean(n2);
			if (b7 == false) trace(""PASS:Number(0)->Boolean"");
			else trace(""FAIL:Number(0)->Boolean"" + b7);
			
			var n3:Number = NaN;
			var b8:Boolean = Boolean(n3);
			if (b8 == false) trace(""PASS:NaN->Boolean"");
			else trace(""FAIL:NaN->Boolean"" + b8);
			
			var s:String = ""hello"";
			var b9:Boolean = Boolean(s);
			if (b9 == true) trace(""PASS:non-empty String->Boolean"");
			else trace(""FAIL:non-empty String->Boolean"" + b9);
			
			var s2:String = """";
			var b10:Boolean = Boolean(s2);
			if (b10 == false) trace(""PASS:empty String->Boolean"");
			else trace(""FAIL:empty String->Boolean"" + b10);
		}
		
		private function testIntegerTypeConversions():void
		{
			var a:int = 127;
			var sb:sbyte = sbyte(a);
			if (sb == 127) trace(""PASS:int(127)->sbyte"");
			else trace(""FAIL:int(127)->sbyte"" + sb);
			
			var a2:int = -128;
			var sb2:sbyte = sbyte(a2);
			if (sb2 == -128) trace(""PASS:int(-128)->sbyte"");
			else trace(""FAIL:int(-128)->sbyte"" + sb2);
			
			var a3:int = 255;
			var b:byte = byte(a3);
			if (b == 255) trace(""PASS:int(255)->byte"");
			else trace(""FAIL:int(255)->byte"" + b);
			
			var a4:int = 0;
			var b2:byte = byte(a4);
			if (b2 == 0) trace(""PASS:int(0)->byte"");
			else trace(""FAIL:int(0)->byte"" + b2);
			
			var a5:int = 32767;
			var sh:short = short(a5);
			if (sh == 32767) trace(""PASS:int(32767)->short"");
			else trace(""FAIL:int(32767)->short"" + sh);
			
			var a6:int = -32768;
			var sh2:short = short(a6);
			if (sh2 == -32768) trace(""PASS:int(-32768)->short"");
			else trace(""FAIL:int(-32768)->short"" + sh2);
			
			var a7:int = 65535;
			var us:ushort = ushort(a7);
			if (us == 65535) trace(""PASS:int(65535)->ushort"");
			else trace(""FAIL:int(65535)->ushort"" + us);
			
			var sb3:sbyte = 100;
			var i:int = int(sb3);
			if (i == 100) trace(""PASS:sbyte(100)->int"");
			else trace(""FAIL:sbyte(100)->int"" + i);
			
			var b3:byte = 200;
			var i2:int = int(b3);
			if (i2 == 200) trace(""PASS:byte(200)->int"");
			else trace(""FAIL:byte(200)->int"" + i2);
			
			var sh3:short = 30000;
			var i3:int = int(sh3);
			if (i3 == 30000) trace(""PASS:short(30000)->int"");
			else trace(""FAIL:short(30000)->int"" + i3);
			
			var us2:ushort = 60000;
			var i4:int = int(us2);
			if (i4 == 60000) trace(""PASS:ushort(60000)->int"");
			else trace(""FAIL:ushort(60000)->int"" + i4);
		}
		
		private function testFloatConversions():void
		{
			var n:Number = 1.5;
			var f:float = float(n);
			if (f == 1.5) trace(""PASS:Number(1.5)->float"");
			else trace(""FAIL:Number(1.5)->float"" + f);
			
			var f2:float = 2.5;
			var n2:Number = Number(f2);
			if (n2 == 2.5) trace(""PASS:float(2.5)->Number"");
			else trace(""FAIL:float(2.5)->Number"" + n2);
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
				
				Assert.AreEqual(37, passCount, "Expected 37 passes, got: " + passCount + " output: " + output);
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