using juicescript;
using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.typeconvert
{
    [TestClass]
    public class TestTypeConv1 : CodeTestBase
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
	public class Main extends Sprite
	{
		public function Main() 
		{
			
		}
	}
}

class OO
{
	public var o:String;
    public var p:String;
    public var q:String;
    public var r:String;
    public var s:String;
    public var t:String;
}

var a = new OO();
a.o = 3;

a.p = NaN;
a.q = Infinity;
a.r = -Infinity;
a.s = true;
a.t = false;

"
                }


                );


            return project;

        }

        protected override void TestIsPass(Player player, PlayerException ex)
        {
            var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Main");
            Assert.IsNotNull(global);
            var globalInstance = player.Context.GC.Heap[global.__global_index__];
            Assert.IsNotNull(globalInstance);

            RtScriptClass rtPayload = (RtScriptClass)globalInstance;
            NaNBoxing a = rtPayload.ReadSlot(0);

            Assert.AreEqual(a.ValueType, NaNBoxing.BoxType.HeapPtr);
            RtHeapBase a_v = player.Context.GC.Heap[a.HeapPtr];
            Assert.IsNotNull(a_v);
            Assert.AreEqual(a_v.Kind, RtHeapTypeKind.INSTANCE);

            var OO = player.Context.libs.SelectMany(o => o.Classes).FirstOrDefault(o => o != null && o.QName.Name.EndsWith("OO"));
            Assert.IsNotNull(OO);


            Assert.AreEqual(a_v.Type, OO.Instance);

            Assert.AreEqual("3",
                
                ((RtInstance)a_v).ReadSlot(0, OO.Instance._link_codescope,player).LocalStringValue
               
                );

            Assert.AreEqual("NaN",
               ((RtString)
               player.Context.GC.Heap[((RtInstance)a_v).ReadSlot(1, OO.Instance._link_codescope, player).HeapPtr]
               ).Str
               );

            Assert.AreEqual("Infinity",
                ((RtString)
                player.Context.GC.Heap[((RtInstance)a_v).ReadSlot(2, OO.Instance._link_codescope, player).HeapPtr]
                ).Str
                );

            Assert.AreEqual("-Infinity",
               ((RtString)
               player.Context.GC.Heap[((RtInstance)a_v).ReadSlot(3, OO.Instance._link_codescope, player).HeapPtr]
               ).Str
               );


            Assert.AreEqual("true",
              ((RtString)
              player.Context.GC.Heap[((RtInstance)a_v).ReadSlot(4, OO.Instance._link_codescope, player).HeapPtr]
              ).Str
              );

            Assert.AreEqual("false",
              ((RtString)
              player.Context.GC.Heap[((RtInstance)a_v).ReadSlot(5, OO.Instance._link_codescope, player).HeapPtr]
              ).Str
              );




            Assert.IsNull(ex);
        }


        [TestMethod]
        public void Test()
        {
            Run();
        }

    }
}
