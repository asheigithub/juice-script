package __AS3__.vec 
{
	[no_constructor];
	public final class Vector
	{
		public native function Vector (length:uint = 0, fixed:Boolean = false);
		

		public native function get fixed () : Boolean;
		public native function set fixed (value:Boolean) :void;
		
		
		public native function get length () : int;
		public native function set length (value:int) : void;
		
		public native function join (sep:String = ",") : String;
		
		private native function indexer_set(key:int,value:*):void;
		private native function indexer_get(key:int):*;
		private native function indexer_delete(key:int):Boolean;
		
		
		
		
		
		public native function concat(... args):Vector;
		
		public native function push(... args):uint;
		
		public native function pop():*;
		
		public native function unshift(... args):uint;
		
		public native function shift():*;
				
		public native function indexOf(searchElement:*, fromIndex:int = 0):int;
		
		public native function lastIndexOf(searchElement:*, fromIndex:int = 0x7fffffff):int;
		
		public native function removeAt(index:int):*;
		
		public native function reverse():Vector;
		
		public native function insertAt(index:int, element:*):void;
		
		public native function sort(sortBehavior:*):Vector;
		
		
	}
}