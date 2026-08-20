using juicescript.ABC;
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
#if !FORCOMPILER

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

			Console.WriteLine("==methods==");

			foreach (var kv in MethodStats.OrderByDescending(kv=>kv.Value.TotalTicks))
			{
				Console.WriteLine($"{ Player.GetMethodKey( kv.Key)}: Count={kv.Value.Count}, AvgTicks={kv.Value.AvgTicks:F2}, TotalTicks={kv.Value.TotalTicks}");
			}


		}



		public static Dictionary<ASMethod, MethodExecStats> MethodStats = new Dictionary<ASMethod, MethodExecStats>();

		private static Stack<MethodExecStats> exec_stack=new Stack<MethodExecStats>();

		public class MethodExecStats
		{
			public long Count;
			public long TotalTicks;

			//public int counter;

			public void Record(long ticks)
			{
				Count++;
				TotalTicks += ticks;
			}

			public double AvgTicks => Count == 0 ? 0 : (double)TotalTicks / Count;

			//public Stopwatch stopwatch;

		}

		private static Stopwatch m_watch=new Stopwatch();
		private static Stack<long> m_tick=new Stack<long>();
		public static void Profile_MethodStart(ASMethod method)
		{
			

			if (!MethodStats.TryGetValue(method, out var stat))
			{
				stat = new MethodExecStats();
				MethodStats[method] = stat;
				//stat.stopwatch = new Stopwatch();
			}

			if (m_tick.Count == 0)
			{
				m_watch.Restart();
			}


			//if (stat.counter == 0)
			//{
			//	m_watch.Restart();
			//	//stat.stopwatch.Restart();
			//}

			//stat.counter++;

			exec_stack.Push(stat);
			m_tick.Push(m_watch.ElapsedTicks);

		}
		public static void Profile_MethodEnd()
		{
			var stat = exec_stack.Pop();
			//stat.counter--;

			//if (stat.counter == 0)
			//{
			//	m_watch.Stop();
			//	//stat.stopwatch.Stop();				
			//}
			//stat.Record(stat.stopwatch.ElapsedTicks);
			
			long ticks = m_watch.ElapsedTicks - m_tick.Pop() ;
			stat.Record(ticks);

			if (exec_stack.Count > 0)
			{
				exec_stack.Peek().TotalTicks -= ticks;  //stat.stopwatch.ElapsedTicks; // 减去调用函数的执行时间
			}
			else
			{
				m_watch.Stop();
			}

		}



	}

#endif

}
