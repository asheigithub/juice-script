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
		 //
		 //
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
    //throw "ERROR: bad result: expected " + expected + " but got " + ret + " " + ( getTimer() - st);
//





var last = 42, A = 3877, C = 29573, M = 139968;

function rand(max) {
  last = (last * A + C) % M;
  return max * last / M;
}

var ALU =
  "GGCCGGGCGCGGTGGCTCACGCCTGTAATCCCAGCACTTTGG" +
  "GAGGCCGAGGCGGGCGGATCACCTGAGGTCAGGAGTTCGAGA" +
  "CCAGCCTGGCCAACATGGTGAAACCCCGTCTCTACTAAAAAT" +
  "ACAAAAATTAGCCGGGCGTGGTGGCGCGCGCCTGTAATCCCA" +
  "GCTACTCGGGAGGCTGAGGCAGGAGAATCGCTTGAACCCGGG" +
  "AGGCGGAGGTTGCAGTGAGCCGAGATCGCGCCACTGCACTCC" +
  "AGCCTGGGCGACAGAGCGAGACTCCGTCTCAAAAA";

var IUB = {
  a:0.27, c:0.12, g:0.12, t:0.27,
  B:0.02, D:0.02, H:0.02, K:0.02,
  M:0.02, N:0.02, R:0.02, S:0.02,
  V:0.02, W:0.02, Y:0.02
}

var HomoSap = {
  a: 0.3029549426680,
  c: 0.1979883004921,
  g: 0.1975473066391,
  t: 0.3015094502008
}

function makeCumulative(table) {
  var last = null;
  for (var c in table) {
    if (last) table[c] += table[last];
    last = c;
  }
}

function fastaRepeat(n, seq) {
  var seqi = 0, lenOut = 60;
  while (n>0) {
    if (n<lenOut) lenOut = n;
    if (seqi + lenOut < seq.length) {
      ret += seq.substring(seqi, seqi+lenOut).length;
      seqi += lenOut;
    } else {
      var s = seq.substring(seqi);
      seqi = lenOut - s.length;
      ret += (s + seq.substring(0, seqi)).length;
    }
    n -= lenOut;
  }
}

function fastaRandom(n, table) {
  var line = new Array(60);
  makeCumulative(table);
  while (n>0) {
    if (n<line.length) line = new Array(n);
    for (var i=0; i<line.length; i++) {
      var r = rand(1);
      for (var c in table) {
        if (r < table[c]) {
          line[i] = c;
          break;
        }
      }
    }
    ret += line.join('').length;
    n -= line.length;
  }
}

var ret = 0;
import flash.utils.getTimer;
var st = getTimer();

var count = 7;
fastaRepeat(2*count*100000, ALU);
fastaRandom(3*count*1000, IUB);
fastaRandom(5*count*1000, HomoSap);

var expected = 1456000;

if (true)//ret != expected)
    throw "ERROR: bad result: expected " + expected + " but got " + ret + " " + (getTimer() - st);

