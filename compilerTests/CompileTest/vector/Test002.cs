using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.vector
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
			v.reverse();
			trace('v.length=' + v.length);
			for (var i:int = 0; i < v.length; i++) 
			{
				trace(i + ':' + v[i]);
			}

			var v2 = new <String>['a', 'b', 'c'];
			v2.reverse();
			trace('v2.length=' + v2.length);
			for (var j:int = 0; j < v2.length; j++) 
			{
				trace(j + ':' + v2[j]);
			}

			var v3 = new <int>[1];
			v3.reverse();
			trace('v3.length=' + v3.length + ',v3[0]=' + v3[0]);

			var v4 = new <int>[];
			v4.reverse();
			trace('v4.length=' + v4.length);
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

				Assert.AreEqual("v.length=5\r\n0:5\r\n1:4\r\n2:3\r\n3:2\r\n4:1\r\nv2.length=3\r\n0:c\r\n1:b\r\n2:a\r\nv3.length=1,v3[0]=1\r\nv4.length=0\r\n", print.GetOutput());

			}


		}


		[TestMethod]
		public void Test()
		{
			Run();
		}
	}

}