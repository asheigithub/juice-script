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
	public class Test001 : CodeTestBase
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

var o;
var p;
var q;

class O
{
	private var v;
	public function O(v)
	{
		this.v = v;
		
	}

	public function TTT(a,b,c)
	{
		o = a;
		p = b;
		q = c;		
		return v;
	}
}

var r = new O(6).TTT.call(null,1,2,3);

"
				}


				);


			return project;
		}

		protected override void TestIsPass(Player player, PlayerException ex)
		{
			Assert.IsNull(ex);

			Assert.AreEqual(0, player.Context.GC.Heap.DumpHeap()
				.Where(o => o.TypeKind == RtHeapTypeKind.INSTANCE && o.Type.QName.Name == "O").Count());

			
			player.ForceGC();

			Assert.AreEqual(0, player.Context.GC.Heap.DumpHeap()
				.Where(o => o.TypeKind == RtHeapTypeKind.INSTANCE && o.Type.QName.Name == "O").Count());


			var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Main");
			Assert.IsNotNull(global);
			var globalInstance = player.Context.GC.Heap[global.__global_index__];
			Assert.IsNotNull(globalInstance);
			Assert.IsNull(ex);

			RtScriptClass rtPayload = (RtScriptClass)globalInstance.facility;

			var o = rtPayload.ReadSlot(0);
			Assert.AreEqual(NaNBoxing.BoxType.Sbyte, o.ValueType);
			Assert.AreEqual(1, o.SByteValue);

			var p = rtPayload.ReadSlot(1);
			Assert.AreEqual(NaNBoxing.BoxType.Sbyte, p.ValueType);
			Assert.AreEqual(2, p.SByteValue);

			var q = rtPayload.ReadSlot(2);
			Assert.AreEqual(NaNBoxing.BoxType.Sbyte, q.ValueType);
			Assert.AreEqual(3, q.SByteValue);

			var r = rtPayload.ReadSlot(3);
			Assert.AreEqual(NaNBoxing.BoxType.Sbyte, p.ValueType);
			Assert.AreEqual(6, r.SByteValue);
		}


		[TestMethod]
		public void Test()
		{
			Run();
		}
	}
}
