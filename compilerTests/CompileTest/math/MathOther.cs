using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.math
{
	[TestClass]
	public sealed class MathAbs : CodeTestBase
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
            if (Math.abs(-5) != 5) throw new Error('abs(-5) should be 5');
            if (Math.abs(5) != 5) throw new Error('abs(5) should be 5');
            if (Math.abs(0) != 0) throw new Error('abs(0) should be 0');
            if (Math.abs(-3.5) != 3.5) throw new Error('abs(-3.5) should be 3.5');
            trace('OK');
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
			player.ForceGC();
			var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Main");
			Assert.IsNotNull(global);
			var print = (StringPrint)player.Print;
			if (ex != null)
			{
				Console.WriteLine("Error: " + ex.Message);
				Console.WriteLine("Output: " + print.GetOutput());
			}
			Assert.IsNull(ex, "Expected no error but got: " + (ex?.Message ?? ""));
			Assert.AreEqual("OK\r\n", print.GetOutput());
		}

		[TestMethod]
		public void Test() => Run();
	}

	[TestClass]
	public sealed class MathFloorCeilRound : CodeTestBase
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
    [Doc]
    public class Main {
        public function Main() {
            if (Math.floor(1.2) != 1) throw new Error('floor(1.2)');
            if (Math.floor(1.8) != 1) throw new Error('floor(1.8)');
            if (Math.floor(-1.2) != -2) throw new Error('floor(-1.2)');
            if (Math.ceil(1.2) != 2) throw new Error('ceil(1.2)');
            if (Math.ceil(1.8) != 2) throw new Error('ceil(1.8)');
            if (Math.ceil(-1.2) != -1) throw new Error('ceil(-1.2)');
            if (Math.round(1.2) != 1) throw new Error('round(1.2)');
            if (Math.round(1.5) != 2) throw new Error('round(1.5)');
            if (Math.round(1.7) != 2) throw new Error('round(1.7)');
            if (Math.round(-1.2) != -1) throw new Error('round(-1.2)');
            if (Math.round(-1.5) != -2) throw new Error('round(-1.5)');
            trace('OK');
        }
    }
}
var main = new Main();
"
			});
			return project;
		}

		protected override void TestIsPass(Player player, PlayerException ex)
		{
			player.ForceGC();
			var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Main");
			Assert.IsNotNull(global);
			var print = (StringPrint)player.Print;
			Assert.IsNull(ex, "Error: " + (ex?.Message ?? ""));
			Assert.AreEqual("OK\r\n", print.GetOutput());
		}

		[TestMethod]
		public void Test() => Run();
	}

	[TestClass]
	public sealed class MathSqrtPow : CodeTestBase
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
            // sqrt
            if (Math.sqrt(4) != 2) throw new Error('sqrt(4)');
            if (Math.sqrt(2) != Math.sqrt(2)) throw new Error('sqrt(2)');
            if (Math.sqrt(0) != 0) throw new Error('sqrt(0)');
            // pow
            if (Math.pow(2, 3) != 8) throw new Error('pow(2,3)');
            if (Math.pow(4, 0.5) != 2) throw new Error('pow(4,0.5)');
            if (Math.pow(2, -2) != 0.25) throw new Error('pow(2,-2)');
            if (Math.pow(0, 0) != 1) throw new Error('pow(0,0)');
            trace('OK');
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
			player.ForceGC();
			var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Main");
			Assert.IsNotNull(global);
			var print = (StringPrint)player.Print;
			if (ex != null)
			{
				Console.WriteLine("Error: " + ex.Message);
				Console.WriteLine("Output: " + print.GetOutput());
			}
			Assert.IsNull(ex, "Expected no error but got: " + (ex?.Message ?? ""));
			Assert.AreEqual("OK\r\n", print.GetOutput());
		}

		[TestMethod]
		public void Test() => Run();
	}

	[TestClass]
	public sealed class MathTrig : CodeTestBase
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
    [Doc]
    public class Main {
        public function Main() {
            if (Math.sin(0) != 0) throw new Error('sin(0)');
            if (Math.cos(0) != 1) throw new Error('cos(0)');
            if (Math.tan(0) != 0) throw new Error('tan(0)');
            if (Math.acos(1) != 0) throw new Error('acos(1)');
            if (Math.asin(1) != Math.PI/2) throw new Error('asin(1)');
            if (Math.atan(1) != Math.PI/4) throw new Error('atan(1)');
            trace('OK');
        }
    }
}
var main = new Main();
"
			});
			return project;
		}

		protected override void TestIsPass(Player player, PlayerException ex)
		{
			player.ForceGC();
			var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Main");
			Assert.IsNotNull(global);
			Assert.IsNull(ex, "Error: " + (ex?.Message ?? ""));
			var print = (StringPrint)player.Print;
			Assert.AreEqual("OK\r\n", print.GetOutput());
		}

		[TestMethod]
		public void Test() => Run();
	}

	[TestClass]
	public sealed class MathAtan2 : CodeTestBase
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
    [Doc]
    public class Main {
        public function Main() {
            if (Math.atan2(0, 1) != 0) throw new Error('atan2(0,1)');
            if (Math.atan2(1, 0) != Math.PI/2) throw new Error('atan2(1,0)');
            if (Math.atan2(-1, 0) != -Math.PI/2) throw new Error('atan2(-1,0)');
            trace('OK');
        }
    }
}
var main = new Main();
"
			});
			return project;
		}

		protected override void TestIsPass(Player player, PlayerException ex)
		{
			player.ForceGC();
			var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Main");
			Assert.IsNotNull(global);
			Assert.IsNull(ex);
			var print = (StringPrint)player.Print;
			Assert.AreEqual("OK\r\n", print.GetOutput());
		}

		[TestMethod]
		public void Test() => Run();
	}

	[TestClass]
	public sealed class MathExpLog : CodeTestBase
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
    [Doc]
    public class Main {
        public function Main() {
            if (Math.exp(0) != 1) throw new Error('exp(0)');
            if (Math.log(1) != 0) throw new Error('log(1)');
            trace('OK');
        }
    }
}
var main = new Main();
"
			});
			return project;
		}

		protected override void TestIsPass(Player player, PlayerException ex)
		{
			player.ForceGC();
			var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Main");
			Assert.IsNotNull(global);
			Assert.IsNull(ex, "Error: " + (ex?.Message ?? ""));
			var print = (StringPrint)player.Print;
			Assert.AreEqual("OK\r\n", print.GetOutput());
		}

		[TestMethod]
		public void Test() => Run();
	}

	[TestClass]
	public sealed class MathRandom : CodeTestBase
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
    [Doc]
    public class Main {
        public function Main() {
            var r = Math.random();
            if (r < 0 || r >= 1) throw new Error('random should be in [0, 1), got ' + r);
            trace('OK');
        }
    }
}
var main = new Main();
"
			});
			return project;
		}

		protected override void TestIsPass(Player player, PlayerException ex)
		{
			player.ForceGC();
			var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Main");
			Assert.IsNotNull(global);
			Assert.IsNull(ex, "Error: " + (ex?.Message ?? ""));
			var print = (StringPrint)player.Print;
			Assert.AreEqual("OK\r\n", print.GetOutput());
		}

		[TestMethod]
		public void Test() => Run();
	}

	[TestClass]
	public sealed class MathConstants : CodeTestBase
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
    [Doc]
    public class Main {
        public function Main() {
            if (Math.log(Math.E) != 1) throw new Error('e');
            if (Math.PI != 3.141592653589793) throw new Error('pi');
            trace('OK');
        }
    }
}
var main = new Main();
"
			});
			return project;
		}

		protected override void TestIsPass(Player player, PlayerException ex)
		{
			player.ForceGC();
			var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Main");
			Assert.IsNotNull(global);
			Assert.IsNull(ex, "Error: " + (ex?.Message ?? ""));
			var print = (StringPrint)player.Print;
			Assert.AreEqual("OK\r\n", print.GetOutput());
		}

		[TestMethod]
		public void Test() => Run();
	}
}
