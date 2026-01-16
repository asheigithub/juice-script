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
		
	}
}