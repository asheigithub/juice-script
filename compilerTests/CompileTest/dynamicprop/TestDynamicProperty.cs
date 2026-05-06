using juicescript;
using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.dynamicprop
{
    [TestClass]
    public class TestDynamicProperty : CodeTestBase
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
	//use namespace AS3;
	import flash.display.Sprite;
	import ns1.Class2;
	import ns1.TNS;
	[Doc]
	public class Main extends Sprite
	{
		
		//public static var BBB =  7;
		public function Main() 
		{	
		}
		public var j:int ;
	}
}
dynamic class OO
{
	
}

var o = new OO();

o.U = 5;
o.K = 6;

var i = new OO();
i.K = 9;
i.U = 4;

i.K;
o.K;

i.MM = ""t"";

var c = Object;
c[""hh""] = i;
c[""hh""];

this[""LL""] = c;
var a = this[""LL""];

delete this['LL'];
var b = this['LL'];

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
            Assert.IsNull(ex);

            var OO = player.Context.libs.SelectMany(o => o.Classes).FirstOrDefault(o => o != null && o.QName.Name.EndsWith("OO"));
            Assert.IsNotNull(OO);


            RtScriptClass rtPayload = (RtScriptClass)globalInstance;
            
            NaNBoxing o = rtPayload.ReadSlot(0);
            Assert.AreEqual( OO.Instance, player.Context.GC.Heap[o.HeapPtr].Type );
            RtInstance i_o = (RtInstance)player.Context.GC.Heap[o.HeapPtr];
            int p = i_o.PROPERTY_PTR(player, OO.Instance);
            RtDynamic dynamic = (RtDynamic)player.Context.GC.Heap[p];
            Assert.AreEqual(2, dynamic.Slots.Count);
            Assert.AreEqual(5, dynamic.Slots[0].SByteValue);
            Assert.AreEqual(6, dynamic.Slots[1].SByteValue);
            RtShape shape = (RtShape)player.Context.GC.Heap[ dynamic.SHAPE_PTR ];
            Assert.AreEqual("K", GetShapePropertyNameAsString(player, shape.PTR_NAME));
            shape = (RtShape)player.Context.GC.Heap[shape.PTR_PARENT];
            Assert.AreEqual("U", GetShapePropertyNameAsString(player, shape.PTR_NAME));
            shape = (RtShape)player.Context.GC.Heap[shape.PTR_PARENT];
            Assert.AreEqual(true, IsShapePropertyNameEmpty(shape.PTR_NAME));
            Assert.AreEqual(0, shape.PTR_PARENT);

            NaNBoxing i = rtPayload.ReadSlot(1);
            Assert.AreEqual(OO.Instance, player.Context.GC.Heap[i.HeapPtr].Type);
            RtInstance i_i = (RtInstance)player.Context.GC.Heap[i.HeapPtr];
            dynamic = (RtDynamic)player.Context.GC.Heap[i_i.PROPERTY_PTR(player, OO.Instance)];
            Assert.AreEqual(3, dynamic.Slots.Count);
            Assert.AreEqual(9, dynamic.Slots[0].SByteValue);
            Assert.AreEqual(4, dynamic.Slots[1].SByteValue);
            Assert.AreEqual("t", ((RtString) player.Context.GC.Heap[ dynamic.Slots[2].HeapPtr]).Str);
            shape = (RtShape)player.Context.GC.Heap[dynamic.SHAPE_PTR];
            Assert.AreEqual("MM", GetShapePropertyNameAsString(player, shape.PTR_NAME));
            shape = (RtShape)player.Context.GC.Heap[shape.PTR_PARENT];
            Assert.AreEqual("U", GetShapePropertyNameAsString(player, shape.PTR_NAME));
            shape = (RtShape)player.Context.GC.Heap[shape.PTR_PARENT];
            Assert.AreEqual("K", GetShapePropertyNameAsString(player, shape.PTR_NAME));
            shape = (RtShape)player.Context.GC.Heap[shape.PTR_PARENT];
            Assert.AreEqual(true, IsShapePropertyNameEmpty(shape.PTR_NAME));
            Assert.AreEqual(0, shape.PTR_PARENT);


            NaNBoxing c = rtPayload.ReadSlot(2);
            Assert.AreEqual(RtHeapTypeKind.CLASS, player.Context.GC.Heap[c.HeapPtr].TypeKind);
            RtScriptClass c_i = (RtScriptClass)player.Context.GC.Heap[c.HeapPtr];
            dynamic = (RtDynamic)player.Context.GC.Heap[c_i.PROPERTY_PTR];
            Assert.AreEqual(i, dynamic.Slots[0]);
            Assert.AreEqual("hh",
                GetShapePropertyNameAsString(player,
                ((RtShape)player.Context.GC.Heap[ dynamic.SHAPE_PTR]).PTR_NAME
                ));


            NaNBoxing a = rtPayload.ReadSlot(3);
            Assert.AreEqual(c, a);

            NaNBoxing b = rtPayload.ReadSlot(4);
            Assert.AreEqual(NaNBoxing.BoxType.Undefined, b.ValueType);

        }



        [TestMethod]
        public void Test()
        {
            Run();
        }

        /// <summary>
        /// 测试辅助方法：获取Shape属性名作为字符串
        /// </summary>
        private static string GetShapePropertyNameAsString(Player player, NaNBoxing shapeName)
        {
            if (shapeName.ValueType == NaNBoxing.BoxType.LocalString)
            {
                return shapeName.LocalStringValue;
            }
            else if (shapeName.ValueType == NaNBoxing.BoxType.HeapPtr && shapeName.HeapPtr != 0)
            {
                return ((RtString)player.Context.GC.Heap[shapeName.HeapPtr]).Str;
            }
            else
            {
                return "";
            }
        }

        /// <summary>
        /// 测试辅助方法：检查Shape属性名是否为空
        /// </summary>
        private static bool IsShapePropertyNameEmpty(NaNBoxing shapeName)
        {
            if (shapeName.ValueType == NaNBoxing.BoxType.LocalString)
            {
                return string.IsNullOrEmpty(shapeName.LocalStringValue);
            }
            else if (shapeName.ValueType == NaNBoxing.BoxType.HeapPtr)
            {
                return shapeName.HeapPtr == 0;
            }
            else
            {
                return true;
            }
        }

    }
}
