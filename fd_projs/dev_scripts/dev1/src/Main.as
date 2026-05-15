package 
{
	import flash.display.Sprite;
	 [Doc]
    public class Main extends Sprite {
        public function Main() {
            
			
			
        }

       
    }
}

[struct]
final class Point {
    public var x:int = 0;
    public var y:int = 0;
}

function runTest():void {
    var results:Array = [];


    var v11:Vector.<int> = new <int>[1, 2];
    var visited11:String = '';
    var cb11 = function(i:int, idx:int, vec:*):Boolean {
        visited11 += i + ',';
        if (idx == 0) {
            v11 = new <int>[100];
        }
        return false;
    };
    var r11:Boolean = v11.some(cb11);
    trace(r11, visited11);
	//results.push((visited11 == '1,2,' && r11 == false) ? 1 : 0);
	
	trace(results);
	
    //var separator:String = ',';
    //trace(results[0] + separator + results[1] + separator + results[2] + separator + results[3] + separator + results[4] + separator + results[5] + separator + results[6] + separator + results[7] + separator + results[8] + separator + results[9] + separator + results[10]);
}
runTest();






//
//import flash.utils.getTimer;
//
//function fannkuch(n) {
   //var check = 0;
   //var perm = new Vector.<int>(n);
   //var perm1 = new Vector.<int>(n);
   //var count = new Vector.<int>(n);
   //var maxPerm = new Vector.<int>(n);
   //var maxFlipsCount = 0;
   //var m = n - 1;
//
   //for (var i = 0; i < n; i++) perm1[i] = i;
   //var r = n;
//
   //while (true) {
      //// write-out the first 30 permutations
      //if (check < 30){
         //var s = "";
         //for(var i=0; i<n; i++) s += (perm1[i]+1).toString();
         //check++;
      //}
//
      //while (r != 1) { count[r - 1] = r; r--; }
      //if (!(perm1[0] == 0 || perm1[m] == m)) {
         //for (var i = 0; i < n; i++) perm[i] = perm1[i];
//
         //var flipsCount = 0;
         //var k;
//
         //while (!((k = perm[0]) == 0)) {
            //var k2 = (k + 1) >> 1;
            //for (var i = 0; i < k2; i++) {
               //var temp = perm[i]; perm[i] = perm[k - i]; perm[k - i] = temp;
            //}
            //flipsCount++;
         //}
//
         //if (flipsCount > maxFlipsCount) {
            //maxFlipsCount = flipsCount;
            //for (var i = 0; i < n; i++) maxPerm[i] = perm1[i];
         //}
      //}
//
      //while (true) {
         //if (r == n) return maxFlipsCount;
         //var perm0 = perm1[0];
         //var i = 0;
         //while (i < r) {
            //var j = i + 1;
            //perm1[i] = perm1[j];
            //i = j;
         //}
         //perm1[r] = perm0;
//
         //count[r] = count[r] - 1;
         //if (count[r] > 0) break;
         //r++;
      //}
   //}
//}
//var st = getTimer();
//var n = 9;
//var ret = fannkuch(n);
//
//trace( getTimer() - st );
//
//var expected = 30;
//if (ret != expected)
    //throw "ERROR: bad result: expected " + expected + " but got " + ret;

