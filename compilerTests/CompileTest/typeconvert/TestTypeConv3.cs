using juicescript;
using juicescript.ABC;
using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.typeconvert
{
    [TestClass]
    public class TestTypeConv3 : CodeTestBase
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
	public var o:sbyte;
    public var a:byte;
    public var b:short;
    public var c:ushort;
    public var d:int;
    public var e:uint;
    public var f:float;
    public var g:float;
    public var h:Number;
}

var a = new OO();
a.o = '335';
a.a = '-1';
a.b = '6553';
a.c = '-1';
a.d = '3147000000';
a.e = '-1';
a.f = '6.778uuooo';
a.g = '7.4432';

a.h = '-Infinity';

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

            RtScriptClass rtPayload = (RtScriptClass)globalInstance.facility;
            NaNBoxing a = rtPayload.ReadSlot(0);

            Assert.AreEqual(a.ValueType, NaNBoxing.BoxType.HeapPtr);
            RtHeapBase a_v = player.Context.GC.Heap[a.HeapPtr];
            Assert.IsNotNull(a_v);
            Assert.AreEqual(a_v.TypeKind, RtHeapTypeKind.INSTANCE);

            var OO = player.Context.libs.SelectMany(o => o.Classes).FirstOrDefault(o =>o != null && o.QName.Name.EndsWith( "OO"));
            Assert.IsNotNull(OO);


            Assert.AreEqual(a_v.Type, OO.Instance);

            var slot = ((RtInstance)a_v.facility).ReadSlot(0, OO.Instance._link_codescope,player);
            Assert.AreEqual(NaNBoxing.BoxType.Sbyte, slot.ValueType);
            Assert.AreEqual(unchecked((sbyte)335), slot.SByteValue);

            var s_a = ((RtInstance)a_v.facility).ReadSlot(1, OO.Instance._link_codescope,player);
            Assert.AreEqual(NaNBoxing.BoxType.Byte, s_a.ValueType);
            Assert.AreEqual(255, s_a.ByteValue);


            var s_b = ((RtInstance)a_v.facility).ReadSlot(2, OO.Instance._link_codescope,player);
            Assert.AreEqual(NaNBoxing.BoxType.Short, s_b.ValueType);
            Assert.AreEqual(6553, s_b.ShortValue);

            var s_c = ((RtInstance)a_v.facility).ReadSlot(3, OO.Instance._link_codescope,player);
            Assert.AreEqual(NaNBoxing.BoxType.UShort, s_c.ValueType);
            Assert.AreEqual(65535, s_c.UShortValue);

            var s_d = ((RtInstance)a_v.facility).ReadSlot(4, OO.Instance._link_codescope,player);
            Assert.AreEqual(NaNBoxing.BoxType.Int, s_d.ValueType);
            Assert.AreEqual(-1147967296, s_d.IntValue);

            var s_e = ((RtInstance)a_v.facility).ReadSlot(5, OO.Instance._link_codescope,player);
            Assert.AreEqual(NaNBoxing.BoxType.Uint, s_e.ValueType);
            Assert.AreEqual( unchecked( (uint)-1) , s_e.UIntValue);

            var s_f = ((RtInstance)a_v.facility).ReadSlot(6, OO.Instance._link_codescope,player);
            Assert.AreEqual(NaNBoxing.BoxType.Float, s_f.ValueType);
            Assert.AreEqual(float.NaN , s_f.FloatValue);

            var s_g = ((RtInstance)a_v.facility).ReadSlot(7, OO.Instance._link_codescope,player);
            Assert.AreEqual(NaNBoxing.BoxType.Float, s_g.ValueType);
            Assert.AreEqual(7.4432f , s_g.FloatValue);

            var s_h = ((RtInstance)a_v.facility).ReadSlot(8, OO.Instance._link_codescope,player);
            Assert.AreEqual(NaNBoxing.BoxType.Number, s_h.ValueType);
            Assert.AreEqual(double.NegativeInfinity , s_h.Number);




            Assert.IsNull(ex);
        }


        [TestMethod]
        public void Test()
        {
            Run();
        }

    }
}
