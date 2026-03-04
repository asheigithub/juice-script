package 
{
	[wapper]
    public final class Promise {
        /*
		private var _state:int = 0; // 0: pending, 1: fulfilled, 2: rejected
        private var _value:*;
        private var _reason:*;
        private var _onFulfilled:Array = [];
        private var _onRejected:Array = [];
        */
		
        public native function Promise(executor:Function);
        
        public native function then(onFulfilled:Function, onRejected:Function = null):Promise;
        
        public native function catch(onRejected:Function):Promise ;
        
        // 私有的resolve方法
        private native function _resolve(value:*):void;
        
        // 私有的reject方法  
        private native function _reject(reason:*):void;
        
        // 静态方法
        public native static function resolve(value:*):Promise ;
        
        public native static function reject(reason:*):Promise ;
    }
}