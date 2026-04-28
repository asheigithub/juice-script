package {
    import flash.display.Sprite;

    [Doc]
    public class Main extends Sprite {
        public function Main() {
            testStructSortOn();
        }

         private function testStructSortOn():void {
            trace("\n=== 测试 Array 包含 [struct] 元素的 sortOn ===");

            // 定义 struct
            var p1:Point = new Point();
            p1.x = 100; p1.y = 30;
            var p2:Point = new Point();
            p2.x = 3; p2.y = 10;
            var p3:Point = new Point();
            p3.x = 34; p3.y = 20;

            var arr:Array = [p1, p2, p3];
            trace("创建的数组: " + formatPointArray(arr));

            // 尝试访问第一个元素的 x 字段，看是否正常
            trace("arr[0].x = " + arr[0].x);
            trace("arr[1].x = " + arr[1].x);
            trace("arr[2].x = " + arr[2].x);

            // 按 x 字段排序（数字）
            try {
                arr.sortOn("x", Array.NUMERIC);
                trace("按 x 数字排序: " + formatPointArray(arr));

                if (arr[0].x == 3 && arr[1].x == 34 && arr[2].x == 100) {
                    trace("PASS: struct 按 x 数字排序");
                } else {
                    trace("FAIL: struct 按 x 数字排序，期望 3,34,100，得到 " + arr[0].x + "," + arr[1].x + "," + arr[2].x);
                }
            } catch (e:Error) {
                trace("排序出错: " + e.message);
            }
        }

        private function formatPointArray(arr:Array):String {
            var result:String = "";
            for (var i:int = 0; i < arr.length; i++) {
                if (i > 0) result += ", ";
                result += "Point(" + arr[i].x + "," + arr[i].y + ")";
            }
            return result;
        }
    }
}

var main:Main = new Main();
[struct]
final class Point {
    public var x:int = 0;
    public var y:int = 0;
}