using NAudio.SoundFont;
using NAudio.Wave;


namespace WCM;

public class NAudioSink : ISink
{
    public int SampleRate { get; }
    private readonly float[] preambleStart;
    private readonly float[] preambleStop;

    public NAudioSink(int sampleRate, float[] preambleStart, float[] preambleStop)
    {
        SampleRate = sampleRate;
        this.preambleStart = preambleStart;
        this.preambleStop = preambleStop;
    }

    public void Write(float[] signal)
    {
        float[] completeSignal = new float[preambleStart.Length + signal.Length + preambleStop.Length];
        Array.Copy(preambleStart, 0, completeSignal, 0, preambleStart.Length);
        Array.Copy(signal, 0, completeSignal, preambleStart.Length, signal.Length);
        Array.Copy(preambleStop, 0, completeSignal, preambleStart.Length + signal.Length, preambleStop.Length);


        using var waveOut = new WaveOutEvent();
        var waveProvider = new FloatArrayWaveProvider(completeSignal, SampleRate);
        waveOut.Init(waveProvider);
        waveOut.Play();

        while (waveOut.PlaybackState == PlaybackState.Playing)
        {
            System.Threading.Thread.Sleep(1);
        }

    }
}
