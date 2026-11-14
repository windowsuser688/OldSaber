using System;
using System.Linq;
using OldSaber;
internal static class SpectrumEnvelopeTests
{
    static void Check(bool ok,string message) { if(!ok) throw new Exception(message); }
    static void Main()
    {
        var e=new SpectrumEnvelope();var fft=new float[1024];
        for(int i=0;i<60;i++)e.Process(fft,48000,1f/60);
        Check(e.Values.All(v=>v==0),"silence must not animate");
        // A 1 kHz peak must animate the mid-frequency bands, not every column.
        fft[43]=.02f;
        for(int i=0;i<60;i++)e.Process(fft,48000,1f/60);
        Check(e.Values.Max()>.99f,"audible tone must reach visible height");
        int strongest=Array.IndexOf(e.Values,e.Values.Max());
        Check(strongest>25 && strongest<40,"frequency mapping must follow pitch");
        Check(e.Values.Count(v=>v>.1f)<8,"tone must remain localized");
        var paused=(float[])e.Values.Clone();e.Process(new float[1024],48000,0);
        Check(e.Values.SequenceEqual(paused),"pause must freeze response");
        Array.Clear(fft,0,fft.Length);
        for(int i=0;i<120;i++)e.Process(fft,48000,1f/60);
        Check(e.Values.Max()<.001f,"release must settle after silence");
        e.Reset();fft[43]=.001f;
        for(int i=0;i<60;i++)e.Process(fft,48000,1f/60);
        Check(e.Values.Max()>.4f,"quiet audio must produce visible movement");
        e.Reset();Check(e.Values.All(v=>v==0),"rewind reset must clear state");
        fft[43]=float.NaN;fft[44]=float.PositiveInfinity;
        e.Process(fft,44100,1f/60);
        Check(e.Values.All(v=>!float.IsNaN(v)&&!float.IsInfinity(v)&&v>=0&&v<=1),"invalid input must not poison transforms");
        Console.WriteLine("PASS: silence, tone frequency/height, quiet audio, pause, release, reset and invalid samples.");
    }
}
