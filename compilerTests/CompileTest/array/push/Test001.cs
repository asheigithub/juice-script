using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.array.push
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
		public var v;
	}
	
}

(
function () 
{
	// Test basic push functionality
	var arr = new Array();
	
	// Test single element push
	var result1 = arr.push(42);
	trace('Length after push(42): ' + result1);
	trace('Element at index 0: ' + arr[0]);
	trace('Array length: ' + arr.length);
	
	// Test multiple element push
	var result2 = arr.push('hello', true, null);
	trace('Length after push multiple: ' + result2);
	trace('Element at index 1: ' + arr[1]);
	trace('Element at index 2: ' + arr[2]);
	trace('Element at index 3: ' + arr[3]);
	trace('Array length: ' + arr.length);
	
	// Test empty push
	var result3 = arr.push();
	trace('Length after empty push: ' + result3);
	trace('Array length: ' + arr.length);
	
}
)();

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
				
				// Print exception details if there is one
				if (ex != null)
				{
					Console.WriteLine($"Exception: {ex.Message}");
					Console.WriteLine($"Stack trace: {ex.StackTrace}");
				}
				Assert.IsNull(ex);

				StringPrint print = (StringPrint)player.Print;
				string output = print.GetOutput();
				
				Console.WriteLine($"Actual output: '{output}'");

				// Verify expected output
				string expectedOutput = "Length after push(42): 1\r\n" +
									   "Element at index 0: 42\r\n" +
									   "Array length: 1\r\n" +
									   "Length after push multiple: 4\r\n" +
									   "Element at index 1: hello\r\n" +
									   "Element at index 2: true\r\n" +
									   "Element at index 3: null\r\n" +
									   "Array length: 4\r\n" +
									   "Length after empty push: 4\r\n" +
									   "Array length: 4\r\n";

				Assert.AreEqual(expectedOutput, output);
			}
		}

		[TestMethod]
		public void Test()
		{
			Run();
		}
	}
}