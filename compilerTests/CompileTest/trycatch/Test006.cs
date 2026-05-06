using juicescript;
using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.trycatch
{
	[TestClass]
	public class Test006 : CodeTestBase
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
	import ns1.BaseM;
	
	[Doc]
	/**
	 * ...
	 * @author 
	 */
	public class Main extends Sprite
	{
		
	}
	
}



function fn() {
  var c = 1;
  try {
    throw 'stuff3';
  } catch (c) {
    try {
      throw 'stuff4';
    } catch(c) {
      trace(c,'stuff4');
      // catch parameter shadowing catch parameter
      c = 3;
      trace(c, 3);
    }
    trace(c, 'stuff3');
  }
  trace(c, 1);
}
fn();
"
				}


				);


			return project;
		}

		protected override void TestIsPass(Player player, PlayerException ex)
		{
			//test 262中 catch块中的function能够提升到外部，我们这里就和普通变量一样阻止拉倒



			Assert.IsNull(ex);

			Assert.AreEqual(0, player.Context.GC.Heap.DumpHeap()
				.Where(o => o.Kind == RtHeapTypeKind.INSTANCE && o.Type.QName.Name == "O").Count());


			player.ForceGC();

			Assert.AreEqual(0, player.Context.GC.Heap.DumpHeap()
			   .Where(o => o.Kind == RtHeapTypeKind.INSTANCE && o.Type.QName.Name == "O").Count());

			string output = ((StringPrint)player.Print).GetOutput();

			Assert.AreEqual("stuff4 stuff4\r\n3 3\r\nstuff3 stuff3\r\n1 1\r\n", output);

		}

		[TestMethod]
		public void Test()
		{
			Run();
		}
	}
}
