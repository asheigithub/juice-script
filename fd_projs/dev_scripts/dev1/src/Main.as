package 
{
	import flash.display.Sprite;
	 [Doc]
    public class Main extends Sprite {
		
		public static var LLL;
		
		
		var MM;
		
        public  function Main() {
			
			
			
        }

		
	
    }
}

import adobe.utils.CustomActions;
import flash.utils.Dictionary;
//
//import com.adobe.serialization.json.JSON;
//
//var obj = JSON.decode('{"a":1,"b":2}');
//obj.c = obj.a + obj.b;
//trace(JSON.encode(obj));
//


//import flash.utils.Dictionary;
//
//function ccc():void 
//{
	//this.a = 1;
	//
//}
//
//
//var d:Dictionary = new Dictionary();
//
//d["a"] = 'A';
//
//ccc.prototype = d ;
//
//Object.prototype.a = 123;
//
//(function()
//{
		//
	//var o = new ccc();
	//
	//for each(var v in o)
	//{
		//o = {};
		//trace(v);
		//for(var k in o)
		//{
			//trace(k);
		//}
	//}
//})();








//import flash.utils.getTimer;
//
//
//function  fib(i:int):int 
	//{
		//if (i === 1 || i === 2)
		//{
			//return 1;
		//}
		//else 
		//{
			//
			//return fib(i - 2) + fib(i-1);
			//
		//}	
	//}
	//
	//import flash.utils.getTimer;
	//
	//
	//var st = getTimer();
//trace(fib(35),getTimer() - st );






import flash.utils.getTimer;

function fannkuch(n) {
   var check = 0;
   var perm = Array(n);
   var perm1 = Array(n);
   var count = Array(n);
   var maxPerm = Array(n);
   var maxFlipsCount = 0;
   var m = n - 1;

   for (var i = 0; i < n; i++) perm1[i] = i;
   var r = n;

   while (true) {
      // write-out the first 30 permutations
      if (check < 30){
         var s = "";
         for(var i=0; i<n; i++) s += (perm1[i]+1).toString();
         check++;
		 
		 
      }

      while (r != 1) { count[r - 1] = r; r--; }
      if (!(perm1[0] == 0 || perm1[m] == m)) {
         for (var i = 0; i < n; i++) perm[i] = perm1[i];

         var flipsCount = 0;
         var k;

         while (!((k = perm[0]) == 0)) {
            var k2 = (k + 1) >> 1;
            for (var i = 0; i < k2; i++) {
               var temp = perm[i]; perm[i] = perm[k - i]; perm[k - i] = temp;
            }
            flipsCount++;
         }

         if (flipsCount > maxFlipsCount) {
            maxFlipsCount = flipsCount;
            for (var i = 0; i < n; i++) maxPerm[i] = perm1[i];
         }
      }

      while (true) {
         if (r == n) return maxFlipsCount;
         var perm0 = perm1[0];
         var i = 0;
         while (i < r) {
            var j = i + 1;
            perm1[i] = perm1[j];
            i = j;
         }
         perm1[r] = perm0;

         count[r] = count[r] - 1;
         if (count[r] > 0) break;
         r++;
      }
   }
}
var st = getTimer();
var n = 9;
var ret = fannkuch(n);

trace( getTimer() - st );

var expected = 30;
if (ret != expected)
    throw "ERROR: bad result: expected " + expected + " but got " + ret + " " + ( getTimer() - st);





//import flash.utils.getTimer;
//
//class TreeNode
//{
	//public var left:TreeNode;
	//public var right:TreeNode;
	//
	//public static function create(depth:int):TreeNode
        //{
            //return ChildTreeNodes(depth);
        //}
         //
		//public    static function ChildTreeNodes(depth:int):TreeNode
        //{
            //var node:TreeNode = new TreeNode();
            //if (depth > 0)
            //{
                //node.left = ChildTreeNodes(depth - 1);
                //node.right = ChildTreeNodes(depth - 1);
            //}
            //return node;
        //}
//
        //public function  check():int {
            //return left == null ? 1 : left.check() + right.check() + 1;
        //}
//}  
//
//var st = getTimer();
//
//
//
//
 //var n:int = 13; //args.length > 0 ? Integer.parseInt(args[0]) : 0;
        //var minDepth:int = 4;
        //var maxDepth:int = Math.max(minDepth + 2, n);
        //var stretchDepth:int = maxDepth + 1;
        //var check:int = (TreeNode.create(stretchDepth)).check();
        //
        //trace("stretch tree of depth " + (maxDepth + 1) + "\t check: " + check);
//
        //var longLivedTree:TreeNode = TreeNode.create(maxDepth);
        //for (var depth:int = minDepth; depth <= maxDepth; depth += 2)
        //{
           //var iterations:int = 1 << (maxDepth - depth + minDepth);
           //check = 0;
//
           //for (var i:int = 1; i <= iterations; i++)
           //{
                //check += (TreeNode.create(depth)).check();
           //}
           //trace(iterations + "\t trees of depth " + depth + "\t check: " + check);
        //}
//
       //trace("long lived tree of depth " + maxDepth + "\t check: " + longLivedTree.check());
	//
  //
	   //
	   //trace( getTimer() - st);
	   
	   
	   
	   
