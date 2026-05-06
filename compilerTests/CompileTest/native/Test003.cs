using juicescript;
using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.native
{
	[TestClass]
	public class Test003 : CodeTestBase
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
	/**
	 * ...
	 * @author 
	 */
	public class Main extends Sprite
	{
		public var v;
	}
	
}
var a = 3;
var b = a.valueOf();
trace( b);


"
				}


				);


			return project;
		}

		protected override void TestIsPass(Player player, PlayerException ex)
		{
			Assert.IsNull(ex);

			player.ForceGC();

			

			var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Main");
			Assert.IsNotNull(global);
			var globalInstance = player.Context.GC.Heap[global.__global_index__];
			Assert.IsNotNull(globalInstance);
			Assert.IsNull(ex);

			RtScriptClass rtPayload = (RtScriptClass)globalInstance.facility;

			var a = rtPayload.ReadSlot(0);
			Assert.AreEqual(NaNBoxing.BoxType.Sbyte, a.ValueType);
			Assert.AreEqual(3, a.SByteValue);

			var b = rtPayload.ReadSlot(1);
			Assert.AreEqual(NaNBoxing.BoxType.Sbyte, b.ValueType);
			Assert.AreEqual(3, b.SByteValue);

			Assert.AreEqual("3\r\n", ((StringPrint)player.Print).GetOutput());

		}


		[TestMethod]
		public void Test()
		{
			Run();
		}
	}
}
