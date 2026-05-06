using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using juicescript;

namespace compilerTests.CompileTest.memberinitvalue
{
	[TestClass]
	public sealed class TestMemberInit008 : CodeTestBase
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
		
		public static var LLM = G2;		
		
		public  static const K = B;
		public  static const B = G1 ;
		
		public var DD = CC;
		public var EE = FF;
		public var CC = AA;
		public var AA = 6;
		const BB = Number.E;
		const FF = ""FFFF"";

		public function Test()
		{
			var a = b;
			var b = c;			
			const c = AA;
	
			u = a;
			v = b;
			w = c;

		}

	}
}

const G1 = ""hjk"";
const G2 = int.MIN_VALUE;

function a():void 
{
	var C = B;	
	const B = -G2;	
	//trace(""11"",C);
	o = Main.LLM;
	p = Main.K;	
	x = C;

}
a();

var o;




var p;

var q = new Main().EE;
var r = new Main().DD;
p = -5;
var s = new Main().CC;
var t = new Main().DD;
new Main().Test();
var u;
var v;
var w;
var x;
"
				}


				);


			return project;
		}

		protected override void TestIsPass(Player player, PlayerException ex)
		{
			player.ForceGC();

			var cls = player.Context.libs.SelectMany(o => o.Classes).FirstOrDefault(o =>o !=null && o.QName.Name == "Main");
			Assert.IsNotNull(cls);
			var clsInstance = player.Context.GC.Heap[cls.__instance_index__];
			Assert.IsNotNull(clsInstance);
			Assert.IsNull(ex);

			var K = ((RtScriptClass)clsInstance.facility).ReadSlot(1);
			var B = ((RtScriptClass)clsInstance.facility).ReadSlot(2);

			Assert.AreEqual(juicescript.NaNBoxing.BoxType.HeapPtr, K.ValueType);
			var Kinstance = player.Context.GC.Heap[K.HeapPtr];
			Assert.AreEqual(RtHeapTypeKind.STRING, Kinstance.TypeKind);
			Assert.AreEqual("hjk", ((RtString)Kinstance.facility).Str );

			Assert.AreEqual(K, B);

			var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Main");
			Assert.IsNotNull(global);
			var globalInstance = player.Context.GC.Heap[global.__global_index__];
			Assert.IsNotNull(globalInstance);
			
			NaNBoxing o = ((RtScriptClass)globalInstance.facility).ReadSlot(3);
			Assert.AreEqual(NaNBoxing.BoxType.Int, o.ValueType);
			Assert.AreEqual(int.MinValue, o.IntValue);


			NaNBoxing p = ((RtScriptClass)globalInstance.facility).ReadSlot(4);
			Assert.AreEqual(-5, p.IntValue);

			NaNBoxing q = ((RtScriptClass)globalInstance.facility).ReadSlot(5);
			Assert.AreEqual(juicescript.NaNBoxing.BoxType.HeapPtr, q.ValueType);
			var qinstance = player.Context.GC.Heap[q.HeapPtr];
			Assert.AreEqual(RtHeapTypeKind.STRING, qinstance.TypeKind);
			Assert.AreEqual("FFFF", ((RtString)qinstance.facility).Str);

			NaNBoxing r = ((RtScriptClass)globalInstance.facility).ReadSlot(6);
			Assert.AreEqual(NaNBoxing.BoxType.Undefined,r.ValueType);

			NaNBoxing s = ((RtScriptClass)globalInstance.facility).ReadSlot(7);
			Assert.AreEqual(NaNBoxing.BoxType.Sbyte, s.ValueType);
			Assert.AreEqual(6, s.SByteValue);

			NaNBoxing t = ((RtScriptClass)globalInstance.facility).ReadSlot(8);
			Assert.AreEqual(NaNBoxing.BoxType.Undefined, t.ValueType);

			NaNBoxing u = ((RtScriptClass)globalInstance.facility).ReadSlot(9);
			Assert.AreEqual(NaNBoxing.BoxType.Undefined, u.ValueType);


			NaNBoxing v = ((RtScriptClass)globalInstance.facility).ReadSlot(10);
			Assert.AreEqual(NaNBoxing.BoxType.Undefined, v.ValueType);

			NaNBoxing w = ((RtScriptClass)globalInstance.facility).ReadSlot(11);
			Assert.AreEqual(NaNBoxing.BoxType.Sbyte, w.ValueType);
			Assert.AreEqual(6, w.SByteValue);


			NaNBoxing x = ((RtScriptClass)globalInstance.facility).ReadSlot(12);
			Assert.AreEqual(NaNBoxing.BoxType.Int, x.ValueType);
			Assert.AreEqual(int.MinValue, x.IntValue);


			//throw new NotImplementedException();
		}




		[TestMethod]
		public void Test()
		{
			Run();

		}
	}
}
