using juicescript.runtime;
using System.Collections.Generic;
using System.Linq;

namespace compilerTests.CompileTest.vector
{
    [TestClass]
    public sealed class ConcatTest : CodeTestBase
    {
        private const string testCode = @"
package {
    import flash.display.Sprite;

    [Doc]
    public class Main extends Sprite {
        public function Main() {
        }
    }
}

var testMain:Main = new Main();

// Base class
class Animal {
    public var name:String = 'animal';
}

class Dog extends Animal {
    public var breed:String = 'generic';
}

class Cat extends Animal {
    public var color:String = 'white';
}

function runTest():void {
    var results:Array = [];

    // Test1: basic <16 elements
    var v1:Vector.<int> = new <int>[1, 2, 3];
    var v2:Vector.<int> = new <int>[4, 5, 6];
    var result1:Vector.<int> = v1.concat(v2);
    results.push((result1.length == 6 && result1[0] == 1 && result1[5] == 6) ? 1 : 0);

    // Test2: >16 elements
    var v3:Vector.<int> = new <int>[0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19];
    var v4:Vector.<int> = new <int>[20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39];
    var result2:Vector.<int> = v3.concat(v4);
    results.push((result2.length == 40 && result2[0] == 0 && result2[39] == 39) ? 1 : 0);

    // Test3: Vector element
    var inner1:Vector.<int> = new <int>[1,2];
    var inner2:Vector.<int> = new <int>[3,4];
    var inner3:Vector.<int> = new <int>[5,6];
    var v5:Vector.<Vector.<int>> = new <Vector.<int>>[inner1, inner2];
    var v6:Vector.<Vector.<int>> = new <Vector.<int>>[inner3];
    var result3:Vector.<Vector.<int>> = v5.concat(v6);
    results.push((result3.length == 3 && result3[0][0] == 1 && result3[2][1] == 6) ? 1 : 0);

    // Test4: Array element
    var arr1:Array = [1,2];
    var arr2:Array = [3,4];
    var arr3:Array = [5,6];
    var v7:Vector.<Array> = new <Array>[arr1, arr2];
    var v8:Vector.<Array> = new <Array>[arr3];
    var result4:Vector.<Array> = v7.concat(v8);
    results.push((result4.length == 3 && result4[0][0] == 1 && result4[2][0] == 5) ? 1 : 0);

    // Test5: String type
    var v9:Vector.<String> = new <String>['a','b'];
    var v10:Vector.<String> = new <String>['c'];
    var result5:Vector.<String> = v9.concat(v10);
    results.push((result5.length == 3 && result5[0] == 'a' && result5[2] == 'c') ? 1 : 0);

    // Test6: empty Vector concat
    var v13:Vector.<int> = new <int>[];
    var v14:Vector.<int> = new <int>[1,2,3];
    var result6:Vector.<int> = v13.concat(v14);
    results.push((result6.length == 3 && result6[0] == 1 && result6[2] == 3) ? 1 : 0);

    // Test7: concat multiple Vectors (3 args)
    var v20:Vector.<int> = new <int>[1,2];
    var v21:Vector.<int> = new <int>[3,4];
    var v22:Vector.<int> = new <int>[5,6];
    var result7:Vector.<int> = v20.concat(v21, v22);
    results.push((result7.length == 6 && result7[0] == 1 && result7[5] == 6) ? 1 : 0);

    // Test8: concat 4 Vectors
    var v25:Vector.<int> = new <int>[1];
    var v26:Vector.<int> = new <int>[2];
    var v27:Vector.<int> = new <int>[3];
    var v28:Vector.<int> = new <int>[4];
    var result8:Vector.<int> = v25.concat(v26, v27, v28);
    results.push((result8.length == 4 && result8[0] == 1 && result8[3] == 4) ? 1 : 0);

    // Test9: concat with empty Vector
    var v30:Vector.<int> = new <int>[1,2,3];
    var v31:Vector.<int> = new <int>[];
    var v32:Vector.<int> = new <int>[4,5];
    var result9:Vector.<int> = v30.concat(v31, v32);
    results.push((result9.length == 5 && result9[0] == 1 && result9[4] == 5) ? 1 : 0);

    // Test10: Number type
    var v35:Vector.<Number> = new <Number>[1.5, 2.5];
    var v36:Vector.<Number> = new <Number>[3.5];
    var result10:Vector.<Number> = v35.concat(v36);
    results.push((result10.length == 3 && result10[0] == 1.5 && result10[2] == 3.5) ? 1 : 0);

    // Test11: subclass - Dog extends Animal
    var dog1:Dog = new Dog();
    dog1.breed = 'labrador';
    var dog2:Dog = new Dog();
    dog2.breed = 'poodle';
    var dogs:Vector.<Dog> = new <Dog>[dog1, dog2];
    var animals:Vector.<Animal> = new <Animal>[new Cat()];
    var result11:Vector.<Animal> = animals.concat(dogs);
    results.push((result11.length == 3 && result11[0].name == 'animal' && result11[1].name == 'animal' && result11[2].name == 'animal') ? 1 : 0);

    // Test12: multiple types with inheritance
    var cat1:Cat = new Cat();
    cat1.color = 'orange';
    var animals2:Vector.<Animal> = new <Animal>[dog1, cat1];
    var animals3:Vector.<Animal> = new <Animal>[new Animal()];
    var result12:Vector.<Animal> = animals2.concat(animals3);
    results.push((result12.length == 3) ? 1 : 0);

    var separator:String = ',';
    trace(results[0] + separator + results[1] + separator + results[2] + separator + results[3] + separator + results[4] + separator + results[5] + separator + results[6] + separator + results[7] + separator + results[8] + separator + results[9] + separator + results[10] + separator + results[11]);
}
runTest();
";

        protected override TestCodeProject LoadProject()
        {
            TestCodeProject project = new TestCodeProject();
            project.libs = [Juice_GlobalSwc];
            project.testCodes = new List<TestCodeFile>();

            project.testCodes.Add(
                new TestCodeFile()
                {
                    Path = "Main.as",
                    Code = testCode
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

            StringPrint print = (StringPrint)player.Print;
            var output = print.GetOutput();

            var results = output.Trim().Split('\n').Select(s => s.Trim()).ToArray();
            Assert.AreEqual(1, results.Length, "Expected 1 line of output");

            var numbers = results[0].Split(new char[]{','}, StringSplitOptions.RemoveEmptyEntries);
            Assert.AreEqual(12, numbers.Length, "Expected 12 test results");
            Assert.AreEqual("1", numbers[0], "Test1(<16 elements) fail");
            Assert.AreEqual("1", numbers[1], "Test2(>16 elements) fail");
            Assert.AreEqual("1", numbers[2], "Test3(Vector element) fail");
            Assert.AreEqual("1", numbers[3], "Test4(Array element) fail");
            Assert.AreEqual("1", numbers[4], "Test5(String) fail");
            Assert.AreEqual("1", numbers[5], "Test6(empty concat) fail");
            Assert.AreEqual("1", numbers[6], "Test7(multiple 3 args) fail");
            Assert.AreEqual("1", numbers[7], "Test8(concat 4 vectors) fail");
            Assert.AreEqual("1", numbers[8], "Test9(concat with empty) fail");
            Assert.AreEqual("1", numbers[9], "Test10(Number type) fail");
            Assert.AreEqual("1", numbers[10], "Test11(subclass Dog extends Animal) fail");
            Assert.AreEqual("1", numbers[11], "Test12(multiple inheritance types) fail");
        }

        [TestMethod]
        public void Test() => Run();
    }
}