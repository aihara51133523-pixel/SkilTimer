using System;
using System.Diagnostics;

namespace BuffTimer;
public sealed class Countdown
{
    private readonly Func<double> now;
    private double deadline;
    public bool Running { get; private set; }
    public bool Expired { get; private set; }
    public double Seconds { get; private set; }
    public Countdown(Func<double>? clock = null) => now = clock ?? (() => Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency);
    public void Reset(int seconds) { Running = Expired = false; Seconds = seconds; }
    public void Start(int seconds) { Reset(seconds); deadline = now() + seconds; Running = true; }
    public bool Tick()
    {
        if (!Running) return false;
        Seconds = Math.Ceiling(Math.Max(0, deadline - now()) * 100) / 100;
        if (Seconds != 0) return false;
        Running = false; Expired = true; return true;
    }
    public static void Verify()
    {
        double time = 0;
        var c = new Countdown(() => time);
        void Check(bool ok) { if (!ok) throw new InvalidOperationException(); }
        c.Reset(10); Check(c.Seconds == 10 && !c.Expired && !c.Running);
        c.Start(10); time = .125; Check(!c.Tick() && c.Seconds == 9.88);
        time = 2; Check(!c.Tick() && c.Seconds == 8);
        c.Start(10); time = 11; Check(!c.Tick() && c.Seconds == 1);
        time = 11.999; Check(!c.Tick() && c.Seconds == .01 && !c.Expired);
        time = 12; Check(c.Tick() && c.Seconds == 0 && c.Expired);
        Check(!c.Tick()); c.Start(3); Check(!c.Expired && c.Seconds == 3);
        c.Reset(5); time = 100; Check(!c.Tick() && c.Seconds == 5 && !c.Expired);
    }
}
