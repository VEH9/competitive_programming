using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace HomeWork1
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var test = new Test();
            Console.WriteLine(Test.StartTest());
        }
    }

   
    public class Test
    {
        private static readonly List<DateTime> Switches = new();
        private static int previousId;
    
        public static double StartTest()
        {
            var process = Process.GetCurrentProcess();
            process.PriorityClass = ProcessPriorityClass.RealTime;
            process.ProcessorAffinity = (IntPtr)(1 << 7);
            var firstThread = new Thread(DoSomeWorks);
            var secondThread = new Thread(DoSomeWorks);
            firstThread.Start();
            secondThread.Start();
            firstThread.Join();
            secondThread.Join();
            
            var result = 0.0;
            for (var i = 0; i < Switches.Count - 1; i++)
            {
                var time = Switches[i + 1] - Switches[i];
                result += time.TotalMilliseconds;
            }

            return result / Switches.Count;
        }
        
        private static void DoSomeWorks()
        {
            var threadId = Environment.CurrentManagedThreadId;
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            while (stopwatch.Elapsed < TimeSpan.FromSeconds(2))
            {
                lock (Switches)
                {
                    if (threadId == previousId) continue;
                    Switches.Add(DateTime.Now);
                    previousId = threadId;
                }
            }
        }
    }
}