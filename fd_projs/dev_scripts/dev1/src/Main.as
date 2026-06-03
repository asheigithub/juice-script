package 
{
	import flash.display.Sprite;
	 [Doc]
    public class Main extends Sprite {
        public function Main() {
            
			
			
        }

       
    }
}

                            function makeFact() {
                               return function f(n) {
								   trace( arguments.callee == f );
                                   return n == 0 ? 1 : n * f(n - 1);
                               };
                           }

                           var fact = makeFact();
                           trace(fact(5));


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





//
//
//import flash.utils.getTimer;
//
//function fannkuch(n) {
   //var check = 0;
   //var perm = Array(n);
   //var perm1 = Array(n);
   //var count = Array(n);
   //var maxPerm = Array(n);
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
//var n = 10;
//var ret = fannkuch(n);
//
//trace( getTimer() - st );
//
//var expected = 30;
//if (ret != expected)
    //throw "ERROR: bad result: expected " + expected + " but got " + ret + " " + ( getTimer() - st);
//
