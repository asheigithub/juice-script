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

            generateMaze(1, 1);

            var startX:int = 1;
            var startY:int = 1;
            var endX:int = width - 2;
            var endY:int = height - 2;

            var path:Vector.<Object> = findPath(startX, startY, endX, endY);

            if (path.length > 0) {
                for (var k:int = 0; k < path.length; k++) {
					
                    var px:int = path[k].x;
                    var py:int = path[k].y;
                    if (maze[py][px] == 0) {
                        maze[py][px] = 2;
                    }
                }
            }

            var output:String = "";
            for (var y:int = 0; y < height; y++) {
                for (var x:int = 0; x < width; x++) {
                    if (maze[y][x] == 1) {
                        output += "█";
                    } else if (maze[y][x] == 2) {
                        output += "●";
                    } else {
                        output += " ";
                    }
                }
                output += "\n";
            }
            trace(output);
            trace("Path length: " + path.length);
        }

        private function generateMaze(startX:int, startY:int):void {
            stack = new Vector.<Object>();
            stack.push({x: startX, y: startY});
            visited[startY][startX] = true;
            maze[startY][startX] = 0;

            while (stack.length > 0) {
                var current:Object = stack[stack.length - 1];
                var x:int = current.x;
                var y:int = current.y;

                var dirs:Vector.<Object> = Vector.<Object>([
                    {dx: 0, dy: -2},
                    {dx: 2, dy: 0},
                    {dx: 0, dy: 2},
                    {dx: -2, dy: 0}
                ]);
                shuffle(dirs);

                var found:Boolean = false;
                for (var i:int = 0; i < dirs.length; i++) {
                    var nx:int = x + dirs[i].dx;
                    var ny:int = y + dirs[i].dy;

                    if (ny > 0 && ny < height - 1 && nx > 0 && nx < width - 1 && !visited[ny][nx]) {
                        visited[ny][nx] = true;
                        maze[y + dirs[i].dy / 2][x + dirs[i].dx / 2] = 0;
                        maze[ny][nx] = 0;
                        stack.push({x: nx, y: ny});
                        found = true;
                        break;
                    }
                }

                if (!found) {
                    stack.pop();
                }
            }
        }

        private function findPath(startX:int, startY:int, endX:int, endY:int):Vector.<Object> {
            var openSet:Vector.<Object> = new Vector.<Object>();
            var closedSet:Vector.<Vector.<Boolean>> = new Vector.<Vector.<Boolean>>(height);
            var cameFrom:Object = {};
            var gScore:Vector.<Vector.<int>> = new Vector.<Vector.<int>>(height);
            var fScore:Vector.<Vector.<int>> = new Vector.<Vector.<int>>(height);

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

            gScore[startY][startX] = 0;
            fScore[startY][startX] = heuristic(startX, startY, endX, endY);
            openSet.push({x: startX, y: startY, f: fScore[startY][startX]});

            while (openSet.length > 0) {
                var minIdx:int = 0;
                for (var kk:int = 1; kk < openSet.length; kk++) {
                    if (openSet[kk].f < openSet[minIdx].f) {
                        minIdx = kk;
                    }
                }
                var current:Object = openSet[minIdx];
                var cx:int = current.x;
                var cy:int = current.y;

                if (cx == endX && cy == endY) {
                    return reconstructPath(cameFrom, cx, cy);
                }

                openSet.splice(minIdx, 1);
                closedSet[cy][cx] = true;

                var neighbors:Vector.<Object> = Vector.<Object>([
                    {dx: 0, dy: -1},
                    {dx: 1, dy: 0},
                    {dx: 0, dy: 1},
                    {dx: -1, dy: 0}
                ]);

                for (var n:int = 0; n < neighbors.length; n++) {
                    var nx:int = cx + neighbors[n].dx;
                    var ny:int = cy + neighbors[n].dy;

                    if (ny < 0 || ny >= height || nx < 0 || nx >= width) continue;
                    if (maze[ny][nx] == 1) continue;
                    if (closedSet[ny][nx]) continue;

                    var tentativeG:int = gScore[cy][cx] + 1;
                    var inOpen:Boolean = false;
                    for (var m:int = 0; m < openSet.length; m++) {
                        if (openSet[m].x == nx && openSet[m].y == ny) {
                            inOpen = true;
                            break;
                        }
                    }

                    if (!inOpen || tentativeG < gScore[ny][nx]) {
                        var k:String = nx + "," + ny;
                        cameFrom[k] = {x: cx, y: cy};
                        gScore[ny][nx] = tentativeG;
                        fScore[ny][nx] = tentativeG + heuristic(nx, ny, endX, endY);

                        if (!inOpen) {
                            openSet.push({x: nx, y: ny, f: fScore[ny][nx]});
                        }
                    }
                }
            }

            return new Vector.<Object>();
        }

        private function heuristic(x1:int, y1:int, x2:int, y2:int):int {
            return Math.abs(x1 - x2) + Math.abs(y1 - y2);
        }

        private function reconstructPath(cameFrom:Object, cx:int, cy:int):Vector.<Object> {
            var path:Vector.<Object> = new Vector.<Object>();
            path.push({x: cx, y: cy});

            var k:String = cx + "," + cy;
            while (cameFrom[k] != null) {
                var prev:Object = cameFrom[k];
                cx = prev.x;
                cy = prev.y;
                path.push({x: cx, y: cy});
                k = cx + "," + cy;
            }

            return path;
        }

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

new Main();


