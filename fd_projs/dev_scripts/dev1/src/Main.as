package {
    import flash.display.Sprite;
    import flash.Vector;

    [Doc]
    public class Main extends Sprite {
        private var width:int;
        private var height:int;
        private var maze:Vector.<Vector.<int>>;
        private var visited:Vector.<Vector.<Boolean>>;
        private var stack:Vector.<Object>;

        public function Main() {
            width = 31;
            height = 21;
            maze = new Vector.<Vector.<int>>(height);
            visited = new Vector.<Vector.<Boolean>>(height);

            for (var i:int = 0; i < height; i++) {
                maze[i] = new Vector.<int>(width);
                visited[i] = new Vector.<Boolean>(width);
                for (var j:int = 0; j < width; j++) {
                    maze[i][j] = 1;
                    visited[i][j] = false;
                }
            }

            // 生成迷宫
            generateMaze(1, 1);

            // 起点和终点坐标
            var startX:int = 1;
            var startY:int = 1;
            var endX:int = width - 2;
            var endY:int = height - 2;

            // A* 寻路
            var path:Array = findPath(startX, startY, endX, endY);

            // 将路径标记到迷宫上 (值为2表示路径)
            if (path.length > 0) {
                for (var k:int = 0; k < path.length; k++) {
                    var px:int = path[k].x;
                    var py:int = path[k].y;
                    if (maze[py][px] == 0) {
                        maze[py][px] = 2;
                    }
                }
            }

            // 输出迷宫
            var output:String = "";
            for (var y:int = 0; y < height; y++) {
                for (var x:int = 0; x < width; x++) {
                    if (maze[y][x] == 1) {
                        output += "█";  // 墙壁
                    } else if (maze[y][x] == 2) {
                        output += "●";  // 路径
                    } else {
                        output += " ";  // 通道
                    }
                }
                output += "\n";
            }
            trace(output);
            trace("Path length: " + path.length);
        }

        // 深度优先搜索(DFS)生成迷宫
        // 算法原理：从起点开始，随机选择一个未访问的相邻单元格，
        // 打通中间墙壁后移动到该单元格，重复此过程直到无法前进为止，然后回溯
        private function generateMaze(startX:int, startY:int):void {
            // 使用栈保存当前路径，实现回溯
            stack = new Vector.<Object>();
            stack.push({x: startX, y: startY});
            visited[startY][startX] = true;
            maze[startY][startX] = 0;  // 0 表示通道

            while (stack.length > 0) {
                // 获取栈顶单元格
                var current:Object = stack[stack.length - 1];
                var x:int = current.x;
                var y:int = current.y;

                // 四个方向的移动向量（每次走2格，以便跳过中间墙壁）
                var dirs:Vector.<Object> = Vector.<Object>([
                    {dx: 0, dy: -2},  // 上
                    {dx: 2, dy: 0},   // 右
                    {dx: 0, dy: 2},   // 下
                    {dx: -2, dy: 0}   // 左
                ]);
                shuffle(dirs);  // 随机打乱方向顺序

                var found:Boolean = false;
                // 尝试每个方向
                for (var i:int = 0; i < dirs.length; i++) {
                    var nx:int = x + dirs[i].dx;
                    var ny:int = y + dirs[i].dy;

                    // 检查是否在边界内且未被访问
                    if (ny > 0 && ny < height - 1 && nx > 0 && nx < width - 1 && !visited[ny][nx]) {
                        visited[ny][nx] = true;
                        // 打通中间墙壁
                        maze[y + dirs[i].dy / 2][x + dirs[i].dx / 2] = 0;
                        // 打通目标单元格
                        maze[ny][nx] = 0;
                        // 入栈，继续搜索
                        stack.push({x: nx, y: ny});
                        found = true;
                        break;
                    }
                }

                // 如果所有方向都已访问或超出边界，则回溯
                if (!found) {
                    stack.pop();
                }
            }
        }

        // A* 寻路算法
        // f(n) = g(n) + h(n)
        // g(n): 从起点到当前点的实际代价
        // h(n): 从当前点到终点的启发式估计（使用曼哈顿距离）
        // 核心思想：优先扩展 f 值最小的节点
        private function findPath(startX:int, startY:int, endX:int, endY:int):Array {
            // openSet: 待扩展的节点集合（优先队列）
            var openSet:Array = [];
            // closedSet: 已扩展的节点集合
            var closedSet:Vector.<Vector.<Boolean>> = new Vector.<Vector.<Boolean>>(height);
            // cameFrom: 记录每个节点的父节点，用于重建路径
            var cameFrom:Object = {};
            // gScore[n]: 从起点到节点n的实际代价
            var gScore:Vector.<Vector.<int>> = new Vector.<Vector.<int>>(height);
            // fScore[n]: 节点n的启发式估价 f(n) = g(n) + h(n)
            var fScore:Vector.<Vector.<int>> = new Vector.<Vector.<int>>(height);

            // 初始化
            for (var i:int = 0; i < height; i++) {
                closedSet[i] = new Vector.<Boolean>(width);
                gScore[i] = new Vector.<int>(width);
                fScore[i] = new Vector.<int>(width);
                for (var j:int = 0; j < width; j++) {
                    closedSet[i][j] = false;
                    gScore[i][j] = int.MAX_VALUE;
                    fScore[i][j] = int.MAX_VALUE;
                }
            }

            // 起点代价初始化
            gScore[startY][startX] = 0;
            fScore[startY][startX] = heuristic(startX, startY, endX, endY);
            openSet.push({x: startX, y: startY, f: fScore[startY][startX]});

            // 主循环：直到openSet为空或找到终点
            while (openSet.length > 0) {
                // 找到f值最小的节点
                var minIdx:int = 0;
                for (var kk:int = 1; kk < openSet.length; kk++) {
                    if (openSet[kk].f < openSet[minIdx].f) {
                        minIdx = kk;
                    }
                }
                var current:Object = openSet[minIdx];
                var cx:int = current.x;
                var cy:int = current.y;

                // 到达终点，重建路径
                if (cx == endX && cy == endY) {
                    return reconstructPath(cameFrom, cx, cy);
                }

                // 从openSet中移除当前节点，加入closedSet
                var newOpenSet:Array = [];
                for (var ii:int = 0; ii < openSet.length; ii++) {
                    if (ii != minIdx) {
                        newOpenSet.push(openSet[ii]);
                    }
                }
                openSet = newOpenSet;
                closedSet[cy][cx] = true;

                // 检查四个相邻节点
                var neighbors:Vector.<Object> = Vector.<Object>([
                    {dx: 0, dy: -1},  // 上
                    {dx: 1, dy: 0},   // 右
                    {dx: 0, dy: 1},   // 下
                    {dx: -1, dy: 0}   // 左
                ]);

                for (var n:int = 0; n < neighbors.length; n++) {
                    var nx:int = cx + neighbors[n].dx;
                    var ny:int = cy + neighbors[n].dy;

                    // 边界检查
                    if (ny < 0 || ny >= height || nx < 0 || nx >= width) continue;
                    // 墙壁检查
                    if (maze[ny][nx] == 1) continue;
                    // 已扩展检查
                    if (closedSet[ny][nx]) continue;

                    // 计算从当前节点到相邻节点的代价
                    var tentativeG:int = gScore[cy][cx] + 1;

                    // 检查相邻节点是否已在openSet中
                    var inOpen:Boolean = false;
                    for (var m:int = 0; m < openSet.length; m++) {
                        if (openSet[m].x == nx && openSet[m].y == ny) {
                            inOpen = true;
                            break;
                        }
                    }

                    // 如果不在openSet中，或找到更优路径，则更新
                    if (!inOpen || tentativeG < gScore[ny][nx]) {
                        // 记录父节点
                        var k:String = nx + "," + ny;
                        cameFrom[k] = {x: cx, y: cy};
                        // 更新代价
                        gScore[ny][nx] = tentativeG;
                        fScore[ny][nx] = tentativeG + heuristic(nx, ny, endX, endY);

                        // 如果不在openSet中，加入
                        if (!inOpen) {
                            openSet.push({x: nx, y: ny, f: fScore[ny][nx]});
                        }
                    }
                }
            }

            // 未找到路径
            return [];
        }

        // 启发式函数：曼哈顿距离
        // 计算两个坐标之间的水平距离和垂直距离之和
        private function heuristic(x1:int, y1:int, x2:int, y2:int):int {
            return Math.abs(x1 - x2) + Math.abs(y1 - y2);
        }

        // 从终点回溯到起点，重建完整路径
        private function reconstructPath(cameFrom:Object, cx:int, cy:int):Array {
            var path:Array = [];
            // 从终点开始
            path.push({x: cx, y: cy});

            var k:String = cx + "," + cy;
            // 沿着父节点回溯到起点
            while (cameFrom[k] != null) {
                var prev:Object = cameFrom[k];
                cx = prev.x;
                cy = prev.y;
                path.push({x: cx, y: cy});
                k = cx + "," + cy;
            }

            return path;
        }

        // Fisher-Yates 洗牌算法
        // 随机打乱数组元素顺序
        private function shuffle(arr:Vector.<Object>):void {
            for (var i:int = arr.length - 1; i > 0; i--) {
                var j:int = Math.floor(Math.random() * (i + 1));
                var temp:Object = arr[i];
                arr[i] = arr[j];
                arr[j] = temp;
            }
        }
    }
}

var main:Main = new Main();