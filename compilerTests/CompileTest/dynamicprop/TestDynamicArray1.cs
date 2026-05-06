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
    public class TestDynamicArray1 : CodeTestBase
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
		
		public var k:Namespace;
		
	}
}


var m = new Array();

m[1.1] = 77;
m[null] = 6;
m[undefined] = 7;


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


            RtScriptClass rtPayload = (RtScriptClass)globalInstance.facility;
            
            NaNBoxing o = rtPayload.ReadSlot(0);
            Assert.AreEqual( RtHeapTypeKind.ARRAY , player.Context.GC.Heap[o.HeapPtr].TypeKind );
            RtArray i_o = (RtArray)player.Context.GC.Heap[o.HeapPtr].facility;
            int p = i_o.PROPERTY_PTR(player);
            RtDynamic dynamic = (RtDynamic)player.Context.GC.Heap[p].facility;
            Assert.AreEqual(3, dynamic.Slots.Count);
            Assert.AreEqual(77, dynamic.Slots[0].SByteValue);
            Assert.AreEqual(6, dynamic.Slots[1].SByteValue);
            RtShape shape = (RtShape)player.Context.GC.Heap[ dynamic.SHAPE_PTR ].facility;
            Assert.AreEqual("undefined", GetShapePropertyNameAsString(player, shape.PTR_NAME));
            shape = (RtShape)player.Context.GC.Heap[shape.PTR_PARENT].facility;
            Assert.AreEqual("null", GetShapePropertyNameAsString(player, shape.PTR_NAME));
            shape = (RtShape)player.Context.GC.Heap[shape.PTR_PARENT].facility;
            Assert.AreEqual("1.1", GetShapePropertyNameAsString(player, shape.PTR_NAME));
            shape = (RtShape)player.Context.GC.Heap[shape.PTR_PARENT].facility;
            Assert.AreEqual(true, IsShapePropertyNameEmpty(shape.PTR_NAME));
            Assert.AreEqual(0, shape.PTR_PARENT);


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
                return ((RtString)player.Context.GC.Heap[shapeName.HeapPtr].facility).Str;
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
