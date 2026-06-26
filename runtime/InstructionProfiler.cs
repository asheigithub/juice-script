using juicescript.ABC.INS;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.runtime
{
	public class InstructionProfiler
	{
		public class InstructionStats
		{
			public long Count;
			public long TotalTicks;

			public INS_Code code;

			public void Record(long ticks)
			{
				Count++;
				TotalTicks += ticks;
			}

			public double AvgTicks => Count == 0 ? 0 : (double)TotalTicks / Count;

			public Stopwatch stopwatch;

		}

		public static  InstructionStats[] Stats = new InstructionStats[256];

		private static InstructionStats current_stats;
		public static void Profile_ActionStart(INS_Code code)
		{
			if (current_stats != null) //递归了
			{ 
				Profile_ActionEnd(code);
			}


			//if (!Stats.TryGetValue(code, out var stat))
			var stat = Stats[(byte)code];
			if ( stat== null)
			{
				stat = new InstructionStats();
				stat.code = code;
				Stats[(byte)code] = stat;
				stat.stopwatch = new Stopwatch();

			}
			current_stats = stat;
			stat.stopwatch.Restart();
			
		}

		public static void Profile_ActionEnd(INS_Code code)
		{
			var stat = current_stats;

			if (stat != null) //递归结束的情况？
			{
				stat.stopwatch.Stop();
				stat.Record(stat.stopwatch.ElapsedTicks);

				current_stats = null;
			}
		}

		public static void OutPutProfile()
		{
			foreach (var kv in Stats.Where(s=>s != null).OrderByDescending(kv => kv.TotalTicks))
			{
				Console.WriteLine($"{kv.code}: Count={kv.Count}, AvgTicks={kv.AvgTicks:F2}, TotalTicks={kv.TotalTicks}");
			}
		}

		
	}
}
