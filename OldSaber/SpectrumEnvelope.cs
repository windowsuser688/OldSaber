using System;

namespace OldSaber
{
    // Pure audio-to-animation processing, independent of Unity and scene state.
    internal sealed class SpectrumEnvelope
    {
        internal const int Bands = 64;
        internal readonly float[] Values = new float[Bands];
        private readonly float[] levels = new float[Bands];
        private float peak = .005f;
        internal void Reset() { Array.Clear(Values,0,Bands); peak=.005f; }
        internal void Process(float[] fft, int sampleRate, float dt)
        {
            if (dt <= 0 || sampleRate <= 0 || fft.Length == 0) return;
            float framePeak = 0;
            double top = Math.Min(16000, sampleRate * .49);
            for (int band=0; band<Bands; band++)
            {
                double low=40*Math.Pow(top/40,band/(double)Bands);
                double high=40*Math.Pow(top/40,(band+1)/(double)Bands);
                int start=Math.Max(1,Math.Min(fft.Length-1,(int)(low*2*fft.Length/sampleRate)));
                int end=Math.Max(start+1,Math.Min(fft.Length,(int)Math.Ceiling(high*2*fft.Length/sampleRate)));
                float value=0;
                for(int bin=start;bin<end;bin++) if(!float.IsNaN(fft[bin]) && !float.IsInfinity(fft[bin])) value=Math.Max(value,fft[bin]);
                levels[band]=value;
                framePeak=Math.Max(framePeak,value);
            }
            peak=Math.Max(.005f,Math.Max(framePeak,peak*(float)Math.Exp(-dt*.7)));
            for(int band=0;band<Bands;band++)
            {
                float target=levels[band]<.00001f ? 0 : (float)Math.Sqrt(Math.Min(1,levels[band]/peak));
                float blend=1-(float)Math.Exp(-dt*(target>Values[band]?24:5));
                Values[band]+=(target-Values[band])*blend;
            }
        }
    }
}
