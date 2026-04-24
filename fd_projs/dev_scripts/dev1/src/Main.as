package {
    import flash.display.Sprite;
    [Doc]
    public class Main extends Sprite {
        public function Main() {
            trace("=== OOM Threshold Test (Splice) ===");
            testSmallThreshold();
        }
    }
}
function testSmallThreshold():void {
    trace("\n--- Test: Find OOM Threshold with Splice ---");
    
    // 从较大数组开始
    var arr:Array = [];
    for (var i:int = 0; i < 50000; i++) {
        arr[i] = "initial_" + i;
    }
    trace("Created initial array with length=" + arr.length);
    
    var totalNew:int = 0;
    
    // 循环插入，每次插入1000个
    while (true) {
        try {
            // 在位置1插入1000个元素
            // 使用多次 splice(1,0,item) 来插入
            for (var batch:int = 0; batch < 1000; batch++) {
                arr.splice( arr.length , 0, "new_" + totalNew);
                totalNew++;
            }
            
            if (totalNew % 10000 == 0) {
                trace("  After " + (arr.length - 50000) + " new insertions: arr.length=" + arr.length);
            }
        } catch (e:Error) {
            trace("\nOOM triggered!");
            trace("  Original array length: 50000");
            trace("  New elements inserted: " + totalNew);
            trace("  Total arr.length at failure: " + arr.length);
            trace("  Error: " + e.message);
            trace("\nRESULT: OOM threshold is approximately " + arr.length + " elements");
            return;
        }
    }
}
var main:Main = new Main();