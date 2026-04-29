package {
    import flash.display.Sprite;

    [Doc]
    public class Main extends Sprite {
        public function Main() {
           
        }
		
		var token:Token = new Token();		
		public function Test()
		{
					
			if (token.a) 
			{
				throw new Error("!!");
			}

			while (token.a) 
			{
				throw new Error("!!");
			}
			
			do 
			{
				trace("OK");
			} while (token.a);
			
			
		}
		
    }
}

new Main().Test();


class Token
{
	public var a;
}


