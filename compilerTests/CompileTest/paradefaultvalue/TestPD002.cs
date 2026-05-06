using juicescript.runtime;
using juicescript;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using juicescript.compiler;

namespace compilerTests.CompileTest.paradefaultvalue
{
	[TestClass]
	public sealed class TestPD002 : CodeTestBase
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
		
	}
}

function abc(i,j=""2"",k="""" ):void 
{
	o = k;
	p = j;
	q = i / j;
}

abc(1);

var o;
var p;
var q;



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

			NaNBoxing o = ((RtScriptClass)globalInstance).ReadSlot(1);
			Assert.AreEqual(NaNBoxing.BoxType.HeapPtr, o.ValueType);

			NaNBoxing p = ((RtScriptClass)globalInstance).ReadSlot(2);
			Assert.AreEqual(NaNBoxing.BoxType.HeapPtr, p.ValueType);

			Assert.AreEqual( RtHeapTypeKind.STRING, player.Context.GC.Heap[o.HeapPtr].TypeKind );
			Assert.AreEqual( "", ((RtString)player.Context.GC.Heap[o.HeapPtr]).Str );

			Assert.AreEqual(RtHeapTypeKind.STRING, player.Context.GC.Heap[p.HeapPtr].TypeKind);
			Assert.AreEqual("2", ((RtString)player.Context.GC.Heap[p.HeapPtr]).Str);

			NaNBoxing q = ((RtScriptClass)globalInstance).ReadSlot(3);
			Assert.AreEqual(NaNBoxing.BoxType.Number, q.ValueType);

			Assert.AreEqual(0.5, q.Number);


		}

		[TestMethod]
		public void Test()
		{

			Run();


		}
	}
}
