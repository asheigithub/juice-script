using juicescript.runtime;
using System.Collections.Generic;

namespace compilerTests.CompileTest.array.slice
{
    [TestClass]
    public sealed class Test001 : CodeTestBase
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
    }
}

function pass(msg) {
    trace(msg || 'OK');
}

var a1:Array = [1, 2, 3, 4, 5];

// Test 1: no args
var r1:Array = a1.slice();
if (r1.length !== 5) throw new Error('T1 length wrong: ' + r1.length);
if (r1[0] !== 1 || r1[1] !== 2 || r1[2] !== 3 || r1[3] !== 4 || r1[4] !== 5) throw new Error('T1 elements wrong');
if (a1.length !== 5) throw new Error('T1 original changed');

// Test 2: single arg start=2
var r2:Array = a1.slice(2);
if (r2.length !== 3) throw new Error('T2 length wrong: ' + r2.length);
if (r2[0] !== 3 || r2[1] !== 4 || r2[2] !== 5) throw new Error('T2 elements wrong');

// Test 3: two args start=1,end=3
var r3:Array = a1.slice(1, 3);
if (r3.length !== 2) throw new Error('T3 length wrong: ' + r3.length);
if (r3[0] !== 2 || r3[1] !== 3) throw new Error('T3 elements wrong');

// Test 4: negative start=-2
var r4:Array = a1.slice(-2);
if (r4.length !== 2) throw new Error('T4 length wrong: ' + r4.length);
if (r4[0] !== 4 || r4[1] !== 5) throw new Error('T4 elements wrong');

// Test 5: start=0, end=-1
var r5:Array = a1.slice(0, -1);
if (r5.length !== 4) throw new Error('T5 length wrong: ' + r5.length);
if (r5[0] !== 1 || r5[1] !== 2 || r5[2] !== 3 || r5[3] !== 4) throw new Error('T5 elements wrong');

// Test 6: both negative start=-3,end=-1
var r6:Array = a1.slice(-3, -1);
if (r6.length !== 2) throw new Error('T6 length wrong: ' + r6.length);
if (r6[0] !== 3 || r6[1] !== 4) throw new Error('T6 elements wrong');

// Test 7: start > end returns empty
var r7:Array = a1.slice(3, 1);
if (r7.length !== 0) throw new Error('T7 should be empty: ' + r7.length);

// Test 8: start == end returns empty
var r8:Array = a1.slice(2, 2);
if (r8.length !== 0) throw new Error('T8 should be empty: ' + r8.length);

// Test 9: start >= length returns empty
var r9:Array = a1.slice(10);
if (r9.length !== 0) throw new Error('T9 should be empty: ' + r9.length);

// Test 10: end >= length uses length
var r10:Array = a1.slice(0, 10);
if (r10.length !== 5) throw new Error('T10 length wrong: ' + r10.length);
if (r10[0] !== 1 || r10[4] !== 5) throw new Error('T10 elements wrong');

// Test 11: negative start out of bounds (start < -length) should start at 0
var r11:Array = a1.slice(-10);
if (r11.length !== 5) throw new Error('T11 length wrong: ' + r11.length);
if (r11[0] !== 1) throw new Error('T11 should start at 0');

// Test 12: negative end out of bounds (end < -length) should be 0
var r12:Array = a1.slice(0, -10);
if (r12.length !== 0) throw new Error('T12 should be empty: ' + r12.length);

// Test 13: non-integer args (ToInteger conversion)
var r13:Array = a1.slice(1.5, 3.7);
if (r13.length !== 2) throw new Error('T13 length wrong: ' + r13.length);
if (r13[0] !== 2 || r13[1] !== 3) throw new Error('T13 elements wrong');

// Test 14: empty array
var a2:Array = [];
var r14:Array = a2.slice();
if (r14.length !== 0) throw new Error('T14 empty array length wrong: ' + r14.length);
var r14b:Array = a2.slice(0, 1);
if (r14b.length !== 0) throw new Error('T14b empty with args length wrong: ' + r14b.length);

// Test 15: sparse array
var a3:Array = new Array(5);
a3[0] = 'a';
a3[3] = 'b';
var r15:Array = a3.slice(0, 5);
if (r15.length !== 5) throw new Error('T15 length wrong: ' + r15.length);
if (r15[0] !== 'a') throw new Error('T15[0] wrong');
if (r15[3] !== 'b') throw new Error('T15[3] wrong');
if (r15[1] !== undefined) throw new Error('T15[1] should be undefined');
if (r15[2] !== undefined) throw new Error('T15[2] should be undefined');
if (r15[4] !== undefined) throw new Error('T15[4] should be undefined');

// Test 16: sparse array slice from middle
var r16:Array = a3.slice(2);
if (r16.length !== 3) throw new Error('T16 length wrong: ' + r16.length);
if (r16[0] !== undefined) throw new Error('T16[0] should be undefined');
if (r16[1] !== 'b') throw new Error('T16[1] wrong');
if (r16[2] !== undefined) throw new Error('T16[2] should be undefined');

// Test 17: shallow copy (nested array shares reference)
var a4:Array = [1, [2, 3], 4];
var r17:Array = a4.slice();
if (r17.length !== 3) throw new Error('T17 length wrong');
if (r17[0] !== 1) throw new Error('T17[0] wrong');
if (r17[2] !== 4) throw new Error('T17[2] wrong');
a4[1][0] = 99;
if (r17[1][0] !== 99) throw new Error('T17 shallow copy failed');

// Test 18: single arg only start
var r18:Array = a1.slice(3);
if (r18.length !== 2) throw new Error('T18 length wrong: ' + r18.length);
if (r18[0] !== 4 || r18[1] !== 5) throw new Error('T18 elements wrong');

// Test 19: result can be modified independently
var r19:Array = a1.slice(1, 4);
r19[0] = 99;
if (r19[0] !== 99) throw new Error('T19 modify failed');
if (r19[1] !== 3 || r19[2] !== 4) throw new Error('T19 other elements wrong');
if (a1[1] !== 2) throw new Error('T19 original changed');

// Test 20: negative start and positive end
var r20:Array = a1.slice(-4, 3);
if (r20.length !== 2) throw new Error('T20 length wrong: ' + r20.length);
if (r20[0] !== 2 || r20[1] !== 3) throw new Error('T20 elements wrong');

pass('slice tests passed');
"
                }
            );

            return project;
        }

        protected override void TestIsPass(Player player, PlayerException ex)
        {
            Assert.IsNull(ex);
            var print = (StringPrint)player.Print;
            Assert.AreEqual("slice tests passed\r\n", print.GetOutput());
        }

        [TestMethod]
        public void Test() => Run();
    }
}
