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
	public class Test002 : CodeTestBase
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

function throwsomething(e)
{
	throw e;
}

class O
{
	
}

try 
{
	throwsomething(new O());
}
catch(e:Object)
{
	try 
	{
		throw 2;
	}
	catch (e2)
	{
		trace(e2);
		throw e;
	}
	finally
	{
		trace(""f 2"");
	}
}
finally 
{
	trace(""finally"");
}

"
				}


				);


			return project;
		}

		protected override void TestIsPass(Player player, PlayerException ex)
		{
			Assert.IsNotNull(ex);

			Assert.AreEqual(0, player.Context.GC.Heap.DumpHeap()
				.Where(o => o.TypeKind == RtHeapTypeKind.INSTANCE && o.Type.QName.Name == "O").Count());


			player.ForceGC();

			Assert.AreEqual(0, player.Context.GC.Heap.DumpHeap()
			   .Where(o => o.TypeKind == RtHeapTypeKind.INSTANCE && o.Type.QName.Name == "O").Count());

			string output = ((StringPrint)player.Print).GetOutput();

			Assert.AreEqual("2\r\nf 2\r\nfinally\r\n", output);

		}

		[TestMethod]
		public void Test()
		{
			Run();
		}
	}
}
