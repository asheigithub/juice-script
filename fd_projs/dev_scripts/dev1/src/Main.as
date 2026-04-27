package {
    import flash.display.Sprite;
    [Doc]
    public class Main extends Sprite {
        public function Main() {
          
        }
    }
}

var numbers:Array = new Array(3,5,100,34,10);

trace(numbers); // 3,5,100,34,10
numbers.sort();
trace(numbers); // 10,100,3,34,5
numbers.sort(Array.NUMERIC);
trace(numbers); // 3,5,10,34,100