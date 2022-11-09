using System;
using System.Diagnostics;
using System.Threading;

namespace HomeWork1
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var test = new Test();
            Test.StartTest();
        }
    }

   
    public class Test
    {
        public static Stopwatch stopwatch;

        public Test()
        {
            stopwatch = new Stopwatch();
        }

        public static void StartTest()
        {
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;
            Process.GetCurrentProcess().ProcessorAffinity = (IntPtr) (1 << (Environment.ProcessorCount - 1));
            var thread1 = new Thread(SlowMethod) {Priority = ThreadPriority.Highest};
            var thread2 = new Thread(StopTimer) {Priority = ThreadPriority.Highest};
            stopwatch.Start();
            thread1.Start();
            thread2.Start();
            thread2.Join();
            Console.WriteLine(stopwatch.ElapsedMilliseconds);
        }

        private static void SlowMethod() 
        {
            Thread.Sleep(1000);
        }

        private static void StopTimer() 
        {
            stopwatch.Stop();
        }
    }
}