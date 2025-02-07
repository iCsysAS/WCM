using NAudio.SoundFont;
using NAudio.Wave;


namespace WCM;

public class NAudioSink : ISink
{
    public int SampleRate { get; }

    private WaveFormat waveFormat;
    public NAudioSink(int sampleRate)
    {
        SampleRate = sampleRate;
        waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1);
    }

    public void Write(float[] signal)
    {
        using var waveOut = new WaveOutEvent();
        var waveProvider = new FloatArrayWaveProvider(signal,SampleRate);
        waveOut.Init(waveProvider);
        waveOut.Play();

        while (waveOut.PlaybackState == PlaybackState.Playing)
        {
            System.Threading.Thread.Sleep(1);
        }

    }
}
