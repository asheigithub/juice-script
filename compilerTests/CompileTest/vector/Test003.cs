using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.vector
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
			v.fixed = true;
			v.reverse();
			trace('v.length=' + v.length);
			for (var i:int = 0; i < v.length; i++) 
			{
				trace(i + ':' + v[i]);
			}

			var v2 = new <int>[];
			v2.push(1);
			v2.push(2);
			v2.reverse();
			trace('v2.length=' + v2.length);
			for (var j:int = 0; j < v2.length; j++) 
			{
				trace(j + ':' + v2[j]);
			}

			var v3:Vector.<int> = new Vector.<int>(100);
			for (var k:int = 0; k < 100; k++) 
			{
				v3[k] = k + 1;
			}
			v3.reverse();
			trace('v3[0]=' + v3[0] + ',v3[99]=' + v3[99]);

			var v4 = new <MyClass>[new MyClass(1), new MyClass(2), new MyClass(3)];
			v4.reverse();
			trace('v4.length=' + v4.length);
			for (var m:int = 0; m < v4.length; m++) 
			{
				trace(m + ':' + v4[m].value);
			}

			var v5 = new <MyStruct>[new MyStruct(10), new MyStruct(20), new MyStruct(30)];
			v5.reverse();
			trace('v5.length=' + v5.length);
			for (var n:int = 0; n < v5.length; n++) 
			{
				trace(n + ':' + v5[n].value);
			}
		}
	}
}

class MyClass {
	public var value:int;
	public function MyClass(v:int) {
		value = v;
	}
}

[Struct]
class MyStruct {
	public var value:int;
	public function MyStruct(v:int) {
		value = v;
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

				RtScriptClass rtPayload = (RtScriptClass)globalInstance.facility;

				StringPrint print = (StringPrint)player.Print;

				Assert.AreEqual(
					"v.length=5\r\n0:5\r\n1:4\r\n2:3\r\n3:2\r\n4:1\r\n" +
					"v2.length=2\r\n0:2\r\n1:1\r\n" +
					"v3[0]=100,v3[99]=1\r\n" +
					"v4.length=3\r\n0:3\r\n1:2\r\n2:1\r\n" +
					"v5.length=3\r\n0:30\r\n1:20\r\n2:10\r\n",
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