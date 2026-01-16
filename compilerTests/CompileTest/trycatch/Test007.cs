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
	public class Test007 : CodeTestBase
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


function fn(a) {
	trace(a, 1);
  try {
    throw 'stuff1';
  } catch (a) {
   trace(a, 'stuff1');
    // catch parameter shadowing function parameter name
    a = 2;
    trace(a, 2);
  }
  
  trace(a, 1);
  
}
fn(1);
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
				.Where(o => o.TypeKind == RtHeapTypeKind.INSTANCE && o.Type.QName.Name == "O").Count());


			player.ForceGC();

			Assert.AreEqual(0, player.Context.GC.Heap.DumpHeap()
			   .Where(o => o.TypeKind == RtHeapTypeKind.INSTANCE && o.Type.QName.Name == "O").Count());

			string output = ((StringPrint)player.Print).GetOutput();

			Assert.AreEqual("1 1\r\nstuff1 stuff1\r\n2 2\r\n1 1\r\n", output);

		}

		[TestMethod]
		public void Test()
		{
			Run();
		}
	}
}
