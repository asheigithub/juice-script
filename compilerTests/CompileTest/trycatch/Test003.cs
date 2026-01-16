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



(function() {
  //assert.sameValue(
    //f, undefined, 'Initialized binding created prior to evaluation'
  //);

 
  trace(f);
  
  try 
  {
    throw null;
  } 
  catch (f) 
  {
	  trace(f); 
	  {
		
		trace( f);
		//function f() { return 123; }
		var f = 123;
		
		trace( f);
		
		   
	  }
		//var f = 666;
		//trace(f);
  }

  //assert.sameValue(
    //typeof f,
    //'function',
    //'binding value is updated following evaluation'
  //);
  //assert.sameValue(f(), 123);       
  
  trace(f);
  
 //trace(f);
  
}());


"
				}


				);


			return project;
		}

		protected override void TestIsPass(Player player, PlayerException ex)
		{
			//test 262中 catch块会屏蔽 内部块定义的变量提升



			Assert.IsNull(ex);

			Assert.AreEqual(0, player.Context.GC.Heap.DumpHeap()
				.Where(o => o.TypeKind == RtHeapTypeKind.INSTANCE && o.Type.QName.Name == "O").Count());


			player.ForceGC();

			Assert.AreEqual(0, player.Context.GC.Heap.DumpHeap()
			   .Where(o => o.TypeKind == RtHeapTypeKind.INSTANCE && o.Type.QName.Name == "O").Count());

			string output = ((StringPrint)player.Print).GetOutput();

			Assert.AreEqual("undefined\r\nnull\r\nnull\r\n123\r\nundefined\r\n", output);

		}

		[TestMethod]
		public void Test()
		{
			Run();
		}
	}
}
