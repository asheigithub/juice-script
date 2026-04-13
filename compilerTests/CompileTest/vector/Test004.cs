using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.vector
{
	[TestClass]
	public sealed class Test004 : CodeTestBase
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
package 
{
	import flash.display.Sprite;
	
	[Doc]
	public class Main extends Sprite
	{
		public function Main() 
		{
			var v = new <int>[1, 2, 3, 4, 5];
			v.insertAt(2, 99);
			trace('v.length=' + v.length);
			for (var i:int = 0; i < v.length; i++) 
			{
				trace(i + ':' + v[i]);
			}

			var v2 = new <int>[1, 2, 3];
			v2.insertAt(-1, 88);
			trace('v2.length=' + v2.length);
			for (var j:int = 0; j < v2.length; j++) 
			{
				trace(j + ':' + v2[j]);
			}

			var v3 = new <int>[1, 2];
			v3.insertAt(0, 0);
			trace('v3.length=' + v3.length);
			for (var k:int = 0; k < v3.length; k++) 
			{
				trace(k + ':' + v3[k]);
			}

			var v4 = new <int>[1, 2, 3];
			v4.insertAt(3, 4);
			trace('v4.length=' + v4.length);
			for (var m:int = 0; m < v4.length; m++) 
			{
				trace(m + ':' + v4[m]);
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

				RtPayloadScriptClass rtPayload = (RtPayloadScriptClass)globalInstance.facility;

				StringPrint print = (StringPrint)player.Print;

				Assert.AreEqual(
					"v.length=6\r\n0:1\r\n1:2\r\n2:99\r\n3:3\r\n4:4\r\n5:5\r\n" +
					"v2.length=4\r\n0:1\r\n1:2\r\n2:88\r\n3:3\r\n" +
					"v3.length=3\r\n0:0\r\n1:1\r\n2:2\r\n" +
					"v4.length=4\r\n0:1\r\n1:2\r\n2:3\r\n3:4\r\n",
					print.GetOutput());

			}


		}


		[TestMethod]
		public void Test()
		{
			Run();
		}
	}

}