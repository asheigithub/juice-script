using juicescript;
using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.ctor
{
	[TestClass]
	public class TestCtor001 : CodeTestBase
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
			o=5;
		}
		
		public function ABC()
		{
			
		}
		
	}
}

var o;

new Main();


"
				}


				);


			return project;
		}

		protected override void TestIsPass(Player player, PlayerException ex)
		{
			player.ForceGC();

			var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Main");
			Assert.IsNotNull(global);
			var globalInstance = player.Context.GC.Heap[global.__global_index__];
			Assert.IsNotNull(globalInstance);
			Assert.IsNull(ex);

			RtScriptClass rtPayload = (RtScriptClass)globalInstance;

			NaNBoxing o = rtPayload.ReadSlot(0);
			Assert.AreEqual(NaNBoxing.BoxType.Sbyte, o.ValueType);
			Assert.AreEqual(5, o.SByteValue);

		}


		[TestMethod]
		public void Test()
		{
			Run();
		}


	}
}
