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
    private IDemodulator _demodulator;
    private int _samplesPrSymbol;
    float[] _preambleStartSignal;
    float[] _preambleStopSignal;

    public NAudioSource(int sampleRate, float SilenceThreshold,int DeviceNr,int SamplesPrSymbol, float[] PreambleStartSignal, float[] PreambleStopSignal, CancellationToken StoppingToken)
    {
        this.StoppingToken = StoppingToken;
        this.SampleRate = sampleRate;
        _samplesPrSymbol = SamplesPrSymbol;

        _preambleStartSignal = PreambleStartSignal;
        _preambleStopSignal = PreambleStopSignal;

        waveIn = new WaveInEvent
        {
            WaveFormat = new WaveFormat(sampleRate, 16, 1), // 44.1kHz, 16-bit, mono,
            DeviceNumber = DeviceNr,
            BufferMilliseconds = 10
        };
        waveIn.DataAvailable += OnDataAvailable;

        StoppingToken.Register(() => waveIn.StopRecording());

        //Task.Run(async () =>
        //{
        //    while (!StoppingToken.IsCancellationRequested) 
        //    {
        //        try 
        //        {
        //            if(Environment.TickCount64 - lastReceivedSignal > PauseTimeMs)
        //            {
        //                lock (_signalLock)
        //                {
        //                    float[] signal = new float[_receiveBufferIndex];
        //                    Array.Copy(_receiveBuffer, signal, _receiveBufferIndex);
        //                    _signalChannel.Add(signal);
        //                    _receiveBufferIndex = 0;
        //                }
        //            }

                    

        //            await Task.Delay(1);
        //        }
        //        catch (OperationCanceledException) { }


                
        //    }
        //},StoppingToken);
    }

    public int FindPatternIndex(float[] signal, float[] preambleSignal)
    {
        int searchLength = signal.Length - preambleSignal.Length + 1;
        double maxCorrelation = double.MinValue;
        int bestIndex = -1;

        for (int i = 0; i < searchLength; i++)
        {
            double correlation = 0.0;
            for (int j = 0; j < preambleSignal.Length; j++)
            {
                correlation += signal[i + j] * preambleSignal[j];
            }
            if (correlation > maxCorrelation)
            {
                maxCorrelation = correlation;
                bestIndex = i;
            }
        }
        return bestIndex;
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

            for (int i = 0; i < e.BytesRecorded / 2; i++)
            {
                if (_receiveBufferIndex >= _receiveBuffer.Length)
                {
                    Console.WriteLine("Buffer overflow");
                    _receiveBufferIndex = 0;
                }

                _receiveBuffer[_receiveBufferIndex++] = BitConverter.ToInt16(e.Buffer, i * 2) / 32768f;

                var startIndex = FindPatternIndex(_receiveBuffer, _preambleStartSignal);
                var stopIndex = FindPatternIndex(_receiveBuffer, _preambleStopSignal);
                if (startIndex != -1 && stopIndex != -1)
                {
                    int preambleStartSymbols = _preambleStartSignal.Length / _samplesPrSymbol;
                    int preambleStopSymbols = _preambleStopSignal.Length / _samplesPrSymbol;

                    int dataStartIndex = startIndex + preambleStartSymbols * _samplesPrSymbol;
                    int length = stopIndex - (dataStartIndex * _samplesPrSymbol);
                    _signalChannel.Add(_receiveBuffer.Skip(dataStartIndex).Take(length).ToArray());
                    _receiveBufferIndex = 0;
                    break;
                }

            }
        }
    }

    public float[] Read()
    {
       return _signalChannel.Take(StoppingToken);
    }
}
