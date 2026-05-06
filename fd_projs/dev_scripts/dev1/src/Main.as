package {
    import flash.display.Sprite;

    [Doc]
    public class Main extends Sprite {
        public function Main() {
            trace("=== 分形图案测试 ===\n");

            trace("1. 谢尔宾斯基三角形 (Sierpinski Triangle)");
            sierpinskiTriangle(16);
            trace("");

            trace("2. 分形树 (Fractal Tree)");
            fractalTree(6, 10);
            trace("");

            trace("3. 科赫曲线 (Koch Curve)");
            kochCurve(4, 40);
            trace("");

            trace("4. 谢尔宾斯基地毯 (Sierpinski Carpet)");
            recursiveSquares(3);
        }

        // 谢尔宾斯基三角形
        private function sierpinskiTriangle(size:int):void {
            for (var y:int = 0; y < size; y++) {
                var line:String = "";
                for (var i:int = 0; i < size - y - 1; i++) {
                    line += " ";
                }
                for (var x:int = 0; x <= y; x++) {
                    // 谢尔宾斯基条件：如果 (x & y) == 0 则打印，否则空格
                    if ((x & y) == 0) {
                        line += "* ";
                    } else {
                        line += "  ";
                    }
                }
                trace(line);
            }
        }

        // 分形树 - 文本版本
        private function fractalTree(depth:int, maxWidth:int):void {
            for (var i:int = 0; i < depth; i++) {
                var line:String = "";
                for (var s:int = 0; s < maxWidth / 2 - i; s++) {
                    line += " ";
                }
                for (var j:int = 0; j < (i + 1) * 2 - 1; j++) {
                    line += "*";
                }
                trace(line);
            }
            // 树干
            for (var t:int = 0; t < 2; t++) {
                var trunk:String = "";
                for (var sp:int = 0; sp < maxWidth / 2; sp++) {
                    trunk += " ";
                }
                trunk += "|";
                trace(trunk);
            }
        }

        // 科赫曲线 - 文本近似
        private function kochCurve(level:int, width:int):void {
            var size:int = width;
            for (var row:int = 0; row < level + 2; row++) {
                var line:String = "";
                for (var col:int = 0; col < size; col++) {
                    // 简单的科赫曲线近似
                    var normalized:Number = col / size;
                    var height:int = int(Math.pow(2, level - row) * Math.sin(normalized * Math.PI * Math.pow(2, row % 3)));
                    if (Math.abs(height) > row) {
                        line += "*";
                    } else {
                        line += " ";
                    }
                }
                trace(line);
            }
        }

        // 谢尔宾斯基地毯
        private function recursiveSquares(level:int):void {
            var size:int = int(Math.pow(3, level));
            for (var y:int = 0; y < size; y++) {
                var line:String = "";
                for (var x:int = 0; x < size; x++) {
                    line += shouldDraw(x, y, size, level) ? "*" : " ";
                }
                trace(line);
            }
        }

        private function shouldDraw(x:int, y:int, size:int, level:int):Boolean {
            for (var l:int = 0; l < level; l++) {
                var unit:int = int(size / Math.pow(3, l));
                if (unit <= 1) break;

                var cx:int = int(x / unit) % 3;
                var cy:int = int(y / unit) % 3;

                // 中心格子不画（谢尔宾斯基地毯规则）
                if (cx == 1 && cy == 1) {
                    return false;
                }
            }
            return true;
        }
    }
}

// 必须实例化文档类
var main:Main = new Main();
