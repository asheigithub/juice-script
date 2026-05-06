using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.string_
{
	[TestClass]
	public sealed class substr_Test001 : CodeTestBase
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
package {
	import flash.display.Sprite;

	[Doc]
	public class Main extends Sprite {
		public function Main() {
			var str:String = 'Hello World';

			// Test 1: basic substr (0, 5) => 'Hello'
			var result1:String = str.substr(0, 5);
			if (result1 != 'Hello') {
				throw new Error('Test1 failed: ' + result1);
			}

			// Test 2: substr with only startIndex (0) => full string
			var result2:String = str.substr(0);
			if (result2 != 'Hello World') {
				throw new Error('Test2 failed: ' + result2);
			}

			// Test 3: substr from middle (6, 5) => 'World'
			var result3:String = str.substr(6, 5);
			if (result3 != 'World') {
				throw new Error('Test3 failed: ' + result3);
			}

			// Test 4: negative startIndex (counts from end) -5 => 'World'
			var result4:String = str.substr(-5);
			if (result4 != 'World') {
				throw new Error('Test4 failed: ' + result4);
			}

			// Test 5: negative startIndex with length
			var result5:String = str.substr(-5, 3);
			if (result5 != 'Wor') {
				throw new Error('Test5 failed: ' + result5);
			}

			// Test 6: len > remaining chars (should clamp)
			var result6:String = str.substr(6, 100);
			if (result6 != 'World') {
				throw new Error('Test6 failed: ' + result6);
			}

			// Test 7: startIndex > length => empty
			var result7:String = str.substr(20, 5);
			if (result7 != '') {
				throw new Error('Test7 failed: ' + result7);
			}

			// Test 8: negative len => empty
			var result8:String = str.substr(0, -1);
			if (result8 != '') {
				throw new Error('Test8 failed: ' + result8);
			}
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
				var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Main");
				Assert.IsNotNull(global);
				var globalInstance = player.Context.GC.Heap[global.__global_index__];
				Assert.IsNotNull(globalInstance);
				Assert.IsNull(ex);

				RtScriptClass rtPayload = (RtScriptClass)globalInstance;

				StringPrint print = (StringPrint)player.Print;

			}


		}


		[TestMethod]
		public void Test()
		{
			Run();
		}
	}

}
