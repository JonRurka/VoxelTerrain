using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;


public static class DebugTimer
{
    private static Stopwatch Stopwatch;

    public static void Init()
    {
        Stopwatch = new Stopwatch();
    }

    public static void Start()
    {
        Stopwatch.Start();
    }

    public static void Stop()
    {
        Stopwatch.Stop();
    }

    public static void Reset()
    {
        Stopwatch.Reset();
    }

    public static void Restart()
    {
        Stopwatch.Restart();
    }

    public static TimeSpan Elapsed()
    {
        return Stopwatch.Elapsed;
    }
}

