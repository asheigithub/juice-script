using juicescript.runtime;
using System.Collections.Generic;

namespace compilerTests.CompileTest.parsefloat
{
	[TestClass]
	public sealed class Test001 : CodeTestBase
	{
		protected override TestCodeProject LoadProject()
		{
			TestCodeProject project = new TestCodeProject();
			project.libs = [Juice_GlobalSwc];
			project.testCodes = new List<TestCodeFile>();
			project.testCodes.Add(new TestCodeFile()
			{
				Path = "Main.as",
				Code = @"
package {
    import flash.display.Sprite;

    [Doc]
    public class Main extends Sprite {
        public function Main() {
            testBasic();
            testFloat();
            testInfinity();
            testNaN();
            testEdgeCases();
            testSign();
            testExponent();
            testNumericSeparators();
        }

        private function testBasic(): void {
            trace('=== Basic ===');
            trace(parseFloat('123'));
            trace(parseFloat('  123'));
            trace(parseFloat('123.456'));
            trace(parseFloat('.5'));
            trace(parseFloat('0.5'));
        }

        private function testFloat(): void {
            trace('=== Float ===');
            trace(parseFloat('3.14'));
            trace(parseFloat('0.123'));
            trace(parseFloat('123.'));
            trace(parseFloat('00.1'));
            trace(parseFloat('00'));
        }

        private function testInfinity(): void {
            trace('=== Infinity ===');
            trace(parseFloat('Infinity'));
            trace(parseFloat('-Infinity'));
            trace(parseFloat('+Infinity'));
        }

        private function testNaN(): void {
            trace('=== NaN ===');
            trace(parseFloat(''));
            trace(parseFloat('abc'));
            trace(parseFloat(null));
            trace(parseFloat(undefined));
            trace(parseFloat('+'));
            trace(parseFloat('str'));
            trace(parseFloat('s1'));
        }

        private function testEdgeCases(): void {
            trace('=== Edge ===');
            trace(parseFloat('123a'));
            trace(parseFloat('12a3'));
            trace(parseFloat('  123  '));
            trace(parseFloat('\t123'));
            trace(parseFloat('\n123'));
            trace(parseFloat('\r123'));
            trace(parseFloat('\u000B123'));
            trace(parseFloat('\f123'));
            trace(parseFloat('1a2b3'));
            trace(parseFloat('1e2.3'));
        }

        private function testSign(): void {
            trace('=== Sign ===');
            trace(parseFloat('-123'));
            trace(parseFloat('+123'));
            trace(parseFloat('-0'));
            trace(parseFloat('+0'));
        }

        private function testExponent(): void {
            trace('=== Exponent ===');
            trace(parseFloat('1e3'));
            trace(parseFloat('1e-3'));
            trace(parseFloat('1.5e2'));
            trace(parseFloat('2.5e-2'));
            trace(parseFloat('.1e1'));
            trace(parseFloat('1.e1'));
            trace(parseFloat('1e0'));
        }

        private function testNumericSeparators(): void {
            trace('=== NumSep ===');
            trace(parseFloat('1_000'));
            trace(parseFloat('1.1_1'));
            trace(parseFloat('1e1_0'));
        }
    }
}

var main:Main = new Main();
"
			});
			return project;
		}

		protected override void TestIsPass(Player player, PlayerException ex)
		{
			Assert.IsNull(ex);
			var print = (StringPrint)player.Print;
			string output = print.GetOutput();
			string[] lines = output.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

			int i = 0;
			Assert.AreEqual("=== Basic ===", lines[i++]);
			Assert.AreEqual(123.0, double.Parse(lines[i++]));
			Assert.AreEqual(123.0, double.Parse(lines[i++]));
			Assert.AreEqual(123.456, double.Parse(lines[i++]));
			Assert.AreEqual(0.5, double.Parse(lines[i++]));
			Assert.AreEqual(0.5, double.Parse(lines[i++]));

			Assert.AreEqual("=== Float ===", lines[i++]);
			Assert.AreEqual(3.14, double.Parse(lines[i++]));
			Assert.AreEqual(0.123, double.Parse(lines[i++]));
			Assert.AreEqual(123.0, double.Parse(lines[i++]));
			Assert.AreEqual(0.1, double.Parse(lines[i++]));
			Assert.AreEqual(0.0, double.Parse(lines[i++]));

			Assert.AreEqual("=== Infinity ===", lines[i++]);
			var inf = lines[i++];
			Assert.IsTrue(double.IsPositiveInfinity(ToDouble(inf)), "Infinity");
			inf = lines[i++];
			Assert.IsTrue(double.IsNegativeInfinity(ToDouble(inf)), "-Infinity");
			inf = lines[i++];
			Assert.IsTrue(double.IsPositiveInfinity(ToDouble(inf)), "+Infinity");

			Assert.AreEqual("=== NaN ===", lines[i++]);
			Assert.IsTrue(double.IsNaN(double.Parse(lines[i++])));
			Assert.IsTrue(double.IsNaN(double.Parse(lines[i++])));
			Assert.IsTrue(double.IsNaN(double.Parse(lines[i++])));
			Assert.IsTrue(double.IsNaN(double.Parse(lines[i++])));
			Assert.IsTrue(double.IsNaN(double.Parse(lines[i++])));
			Assert.IsTrue(double.IsNaN(double.Parse(lines[i++])));
			Assert.IsTrue(double.IsNaN(double.Parse(lines[i++])));

			Assert.AreEqual("=== Edge ===", lines[i++]);
			Assert.AreEqual(123.0, double.Parse(lines[i++]));
			Assert.AreEqual(12.0, double.Parse(lines[i++]));
			Assert.AreEqual(123.0, double.Parse(lines[i++]));
			Assert.AreEqual(123.0, double.Parse(lines[i++]));
			Assert.AreEqual(123.0, double.Parse(lines[i++]));
			Assert.AreEqual(123.0, double.Parse(lines[i++]));
			Assert.AreEqual(123.0, double.Parse(lines[i++]));
			Assert.AreEqual(123.0, double.Parse(lines[i++]));
			Assert.AreEqual(1.0, double.Parse(lines[i++]));
			Assert.AreEqual(100.0, double.Parse(lines[i++]));

			Assert.AreEqual("=== Sign ===", lines[i++]);
			Assert.AreEqual(-123.0, double.Parse(lines[i++]));
			Assert.AreEqual(123.0, double.Parse(lines[i++]));
			Assert.AreEqual(0.0, double.Parse(lines[i++]));
			Assert.AreEqual(0.0, double.Parse(lines[i++]));

			Assert.AreEqual("=== Exponent ===", lines[i++]);
			Assert.AreEqual(1000.0, double.Parse(lines[i++]));
			Assert.AreEqual(0.001, double.Parse(lines[i++]));
			Assert.AreEqual(150.0, double.Parse(lines[i++]));
			Assert.AreEqual(0.025, double.Parse(lines[i++]));
			Assert.AreEqual(1.0, double.Parse(lines[i++]));
			Assert.AreEqual(10.0, double.Parse(lines[i++]));
			Assert.AreEqual(1.0, double.Parse(lines[i++]));

			Assert.AreEqual("=== NumSep ===", lines[i++]);
			Assert.AreEqual(1.0, double.Parse(lines[i++]));
			Assert.AreEqual(1.1, double.Parse(lines[i++]));
			Assert.AreEqual(10.0, double.Parse(lines[i++]));
		}

		[TestMethod]
		public void Test()
		{
			Run();
		}

		private static double ToDouble(string s)
		{
			if (s == "Infinity") return double.PositiveInfinity;
			if (s == "-Infinity") return double.NegativeInfinity;
			return double.Parse(s);
		}
	}
}
