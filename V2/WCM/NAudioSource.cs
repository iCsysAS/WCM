using NAudio.Wave;
using System.Collections.Concurrent;

namespace WCM;

public class NAudioSource : ISource
{
    public int SampleRate { get; }
    private BlockingCollection<float[]> _signalChannel = new BlockingCollection<float[]>();

    private WaveInEvent waveIn;
    private readonly float silenceThreshold = 0.02f; // Adjust based on your needs
    private readonly int silenceDurationThreshold = 100; // Duration in ms that must be silent
    private System.Threading.Timer silenceTimer;
    private CancellationToken StoppingToken;
    private long lastReceivedSignal = Environment.TickCount64;

    private float[] _receiveBuffer = new float[10000];
    private int _receiveBufferIndex = 0;
    private object _signalLock = new object();

    public NAudioSource(int sampleRate, int PauseTimeMs, float SilenceThreshold, CancellationToken StoppingToken)
    {
        this.StoppingToken = StoppingToken;
        this.SampleRate = sampleRate;
        waveIn = new WaveInEvent
        {
            WaveFormat = new WaveFormat(44100, 16, 1) // 44.1kHz, 16-bit, mono
        };
        waveIn.DataAvailable += OnDataAvailable;

        StoppingToken.Register(() => waveIn.StopRecording());

        Task.Run(async () =>
        {
            while (!StoppingToken.IsCancellationRequested) 
            {
                try 
                {
                    if(Environment.TickCount64 - lastReceivedSignal > PauseTimeMs)
                    {
                        lock (_signalLock)
                        {
                            float[] signal = new float[_receiveBufferIndex];
                            Array.Copy(_receiveBuffer, signal, _receiveBufferIndex);
                            _signalChannel.Add(signal);
                            _receiveBufferIndex = 0;
                        }
                    }

                    

                    await Task.Delay(1);
                }
                catch (OperationCanceledException) { }


                
            }
        },StoppingToken);
    }

    private void OnDataAvailable(object sender, WaveInEventArgs e)
    {
        // Calculate RMS value from the 16-bit PCM data
        int bytesPerSample = 2; // for 16-bit audio
        int sampleCount = e.BytesRecorded / bytesPerSample;
        double sumSquares = 0;

        for (int index = 0; index < e.BytesRecorded; index += bytesPerSample)
        {
            // Convert little-endian 16-bit sample to a short
            short sample = BitConverter.ToInt16(e.Buffer, index);
            // Normalize sample to range [-1.0, 1.0]
            float sample32 = sample / 32768f;
            sumSquares += sample32 * sample32;
        }

        double rms = Math.Sqrt(sumSquares / sampleCount);

        // Check against threshold
        if (rms > silenceThreshold)
        {
            lastReceivedSignal = Environment.TickCount64;

            lock (_signalLock)
            {
                for (int i = 0; i < e.BytesRecorded / 2; i++)
                {
                    if(_receiveBufferIndex >= _receiveBuffer.Length)
                    {
                        Console.WriteLine("Buffer overflow");
                        _receiveBufferIndex = 0;
                    }
                    _receiveBuffer[_receiveBufferIndex++] = BitConverter.ToInt16(e.Buffer, i*2) / 32768f;
                }
            }
        }
    }

    public float[] Read()
    {
       return _signalChannel.Take(StoppingToken);
    }
}
