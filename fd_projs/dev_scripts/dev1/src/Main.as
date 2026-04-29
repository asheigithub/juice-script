package {
    import flash.display.Sprite;

    [Doc]
    public class Main extends Sprite {
        public function Main() {
           
        }
    }
}


var o = {};

o[20] = NaN;
o[19] = [1];
o[18] = [0];
o[17] = [[]];
o[16] = {};
o[15] = [];
o[14] = -Infinity;
o[13] = Infinity;
o[12] = undefined;
o[11] = null;
o[10] = "";
o[9] = "-1";
o[8] = "0";
o[7] = "1";
o[6] = "false";
o[5] = "true";
o[4] = -1;
o[3] = 0;
o[2] = 1;
o[1] = false;
o[0] = true;

var r = {};

r[20] = NaN;
r[19] = [1];
r[18] = [0];
r[17] = [[]];
r[16] = {};
r[15] = [];
r[14] = -Infinity;
r[13] = Infinity;
r[12] = undefined;
r[11] = null;
r[10] = "";
r[9] = "-1";
r[8] = "0";
r[7] = "1";
r[6] = "false";
r[5] = "true";
r[4] = -1;
r[3] = 0;
r[2] = 1;
r[1] = false;
r[0] = true;


for (var i:int = 0;  i<20 ; i++) 
{
	var str = "";
	for (var j:int = 0;  j< 20; j++) 
	{
		str += (o[i] == r[j])?"O ":"X ";
	}
	trace(str);
}