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
	public sealed class TestMemberInit011 : CodeTestBase
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
		
		public static var LL = LLM;
		public static var LLM = G2;		
		
		
		
		
		public  static const K = B;
		public  static const B = G1 ;
		
		
		public var DD = CC;
		public var EE = FF;
		public var CC = AA;
		public var AA = 6;
		const BB = Number.E;
		const FF = ""FFFF"";

		//public function Main()
		//{
			//var a = b;
			//var b = c;			
			//const c = AA;
		//}
		
		public function Test()
		{
			function j():void 
			{
				var d = cc;
				const cc = c;
				
				t = d;
				w = cc;
			}
			
			const c = BB;	
			
			return j;
			
		}
		
		
		
	}
}

const G1 = ""hjk"";

function a():void 
{
	var C = B;
	const B = G2;
	o = Main.LLM;
	p = Main.K;
	
	w = C;
};
a();

//trace(Main.LL);

var o;


const G2 = int.MIN_VALUE;

var p;

var q = new Main().EE;
var r = new Main().DD;

p = -5;

var s = new Main().CC;

var t;
var w;

new Main().Test()();
//Main.MFFF();
//trace(o, p, q, r, s, t, w);

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

			var K = ((RtScriptClass)clsInstance).ReadSlot(2);
			var B = ((RtScriptClass)clsInstance).ReadSlot(3);

			Assert.AreEqual(juicescript.NaNBoxing.BoxType.HeapPtr, K.ValueType);
			var Kinstance = player.Context.GC.Heap[K.HeapPtr];
			Assert.AreEqual(RtHeapTypeKind.STRING, Kinstance.TypeKind);
			Assert.AreEqual("hjk", ((RtString)Kinstance).Str );

			Assert.AreEqual(K, B);

			var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Main");
			Assert.IsNotNull(global);
			var globalInstance = player.Context.GC.Heap[global.__global_index__];
			Assert.IsNotNull(globalInstance);
			
			NaNBoxing o = ((RtScriptClass)globalInstance).ReadSlot(3);
			Assert.AreEqual(NaNBoxing.BoxType.Int, o.ValueType);
			Assert.AreEqual(int.MinValue, o.IntValue);


			NaNBoxing p = ((RtScriptClass)globalInstance).ReadSlot(4);
			Assert.AreEqual(-5, p.IntValue);

			NaNBoxing q = ((RtScriptClass)globalInstance).ReadSlot(5);
			Assert.AreEqual(juicescript.NaNBoxing.BoxType.HeapPtr, q.ValueType);
			var qinstance = player.Context.GC.Heap[q.HeapPtr];
			Assert.AreEqual(RtHeapTypeKind.STRING, qinstance.TypeKind);
			Assert.AreEqual("FFFF", ((RtString)qinstance).Str);

			NaNBoxing r = ((RtScriptClass)globalInstance).ReadSlot(6);
			Assert.AreEqual(NaNBoxing.BoxType.Undefined, r.ValueType);
			

			NaNBoxing s = ((RtScriptClass)globalInstance).ReadSlot(7);
			Assert.AreEqual(NaNBoxing.BoxType.Sbyte, s.ValueType);
			Assert.AreEqual(6, s.SByteValue);

			NaNBoxing t = ((RtScriptClass)globalInstance).ReadSlot(8);
			//Assert.AreEqual(NaNBoxing.BoxType.Undefined, t.ValueType);
			Assert.AreEqual(Math.E, t.Number); // 和AIR不同，这里理应已经计算常量


			NaNBoxing w = ((RtScriptClass)globalInstance).ReadSlot(9);
			Assert.AreEqual(NaNBoxing.BoxType.Number, w.ValueType);
			Assert.AreEqual(Math.E, w.Number);


			//throw new NotImplementedException();
		}




		[TestMethod]
		public void Test()
		{
			Run();

		}
	}
}
