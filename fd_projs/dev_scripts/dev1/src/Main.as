package {
    import flash.display.Sprite;
    [Doc]
    public class Main extends Sprite {
        public function Main() {
          
			var a = [1, 2, 3];
			
			a.insertAt(1, 'b');
			
			trace(a);
			
        }
    }
}

var main:Main = new Main();