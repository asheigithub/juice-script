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
	public sealed class TestMemberInit020 : CodeTestBase
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
		public static const LLM = G2;		
		
		public static function MFFF()
		{
			
			function j() 
			{
				
				return function ():void 
				{
					var e = d;
					var d = c;
					
					
					t = d;
					w = c;
					v = e;
				}
				const c = LLM;	
			}
			
			
			return j();	
		}
		

		public  static const K = B;
		public  static const B = G1 ;
		
		
		public var DD = CC;
		public var EE = FF;
		public var CC = AA;
		public var AA = 6;
		const BB = Number.E;
		const FF = ""FFFF"";

		
		
		
		
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
var v;


Main.MFFF()();

//trace(o, p, q, r, s, t, w ,v);
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

			var K = ((RtPayloadScriptClass)clsInstance.facility).ReadSlot(2);
			var B = ((RtPayloadScriptClass)clsInstance.facility).ReadSlot(3);

			Assert.AreEqual(juicescript.NaNBoxing.BoxType.HeapPtr, K.ValueType);
			var Kinstance = player.Context.GC.Heap[K.HeapPtr];
			Assert.AreEqual(RtHeapTypeKind.STRING, Kinstance.TypeKind);
			Assert.AreEqual("hjk", ((RtPayloadString)Kinstance.facility).Str );

			Assert.AreEqual(K, B);

			var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Main");
			Assert.IsNotNull(global);
			var globalInstance = player.Context.GC.Heap[global.__global_index__];
			Assert.IsNotNull(globalInstance);
			
			NaNBoxing o = ((RtPayloadScriptClass)globalInstance.facility).ReadSlot(3);
			Assert.AreEqual(NaNBoxing.BoxType.Int, o.ValueType);
			Assert.AreEqual(int.MinValue, o.IntValue);


			NaNBoxing p = ((RtPayloadScriptClass)globalInstance.facility).ReadSlot(4);
			Assert.AreEqual(-5, p.IntValue);

			NaNBoxing q = ((RtPayloadScriptClass)globalInstance.facility).ReadSlot(5);
			Assert.AreEqual(juicescript.NaNBoxing.BoxType.HeapPtr, q.ValueType);
			var qinstance = player.Context.GC.Heap[q.HeapPtr];
			Assert.AreEqual(RtHeapTypeKind.STRING, qinstance.TypeKind);
			Assert.AreEqual("FFFF", ((RtPayloadString)qinstance.facility).Str);

			NaNBoxing r = ((RtPayloadScriptClass)globalInstance.facility).ReadSlot(6);
			Assert.AreEqual(NaNBoxing.BoxType.Undefined, r.ValueType);
			

			NaNBoxing s = ((RtPayloadScriptClass)globalInstance.facility).ReadSlot(7);
			Assert.AreEqual(NaNBoxing.BoxType.Sbyte, s.ValueType);
			Assert.AreEqual(6, s.SByteValue);

			NaNBoxing t = ((RtPayloadScriptClass)globalInstance.facility).ReadSlot(8); //原AIR运行时没有从class中取到值，但是这不合理。现在的结果是符合逻辑的
			Assert.AreEqual(NaNBoxing.BoxType.Int, t.ValueType);
			Assert.AreEqual(int.MinValue, t.IntValue);

			NaNBoxing w = ((RtPayloadScriptClass)globalInstance.facility).ReadSlot(9);//原AIR运行时没有从class中取到值，但是这不合理。现在的结果是符合逻辑的
			Assert.AreEqual(NaNBoxing.BoxType.Int, w.ValueType);
			Assert.AreEqual(int.MinValue, w.IntValue);

			NaNBoxing v = ((RtPayloadScriptClass)globalInstance.facility).ReadSlot(10);
			Assert.AreEqual(NaNBoxing.BoxType.Undefined, v.ValueType);
			

			//throw new NotImplementedException();
		}




		[TestMethod]
		public void Test()
		{
			Run();

		}
	}
}
