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
	public sealed class TestPD003F : CodeTestBase
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
		public static var AA = 6;
	}
}

function abc(i = Main.AA,j='',k='' ):void 
{
	o = k;
	p = j;
	q = i;
}

abc();

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
			//player.ForceGC();

			//var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Main");
			//Assert.IsNotNull(global);
			//var globalInstance = player.Context.GC.Heap[global.__global_index__];
			//Assert.IsNotNull(globalInstance);
			//Assert.IsNull(ex);

			//NaNBoxing o = ((RtPayloadScriptClass)globalInstance.facility).ReadSlot(1);
			//Assert.AreEqual(NaNBoxing.BoxType.HeapPtr, o.ValueType);

			//NaNBoxing p = ((RtPayloadScriptClass)globalInstance.facility).ReadSlot(2);
			//Assert.AreEqual(NaNBoxing.BoxType.HeapPtr, p.ValueType);

			//Assert.AreEqual( RtHeapTypeKind.STRING, player.Context.GC.Heap[o.HeapPtr].TypeKind );
			//Assert.AreEqual( "", ((RtPayloadString)player.Context.GC.Heap[o.HeapPtr].facility).Str );

			//Assert.AreEqual(RtHeapTypeKind.STRING, player.Context.GC.Heap[p.HeapPtr].TypeKind);
			//Assert.AreEqual("2", ((RtPayloadString)player.Context.GC.Heap[p.HeapPtr].facility).Str);

			//NaNBoxing q = ((RtPayloadScriptClass)globalInstance.facility).ReadSlot(3);
			//Assert.AreEqual(NaNBoxing.BoxType.Number, q.ValueType);

			//Assert.AreEqual(0.5, q.Number);


		}

		[TestMethod]
		public void Test()
		{
			bool israise=false;

			try
			{
				Run();
			}
			catch (CompilerException ex)
			{
				israise=true;
				Assert.AreEqual("Parameter initializer unknown or is not a compile-time constant.", ex.Message);
			}

			

			Assert.IsTrue( israise );
		}
	}
}
