using System.Runtime.InteropServices;

namespace WCM;

public class FSKModulator : IModulator
{
    public double Freq0 { get; }
    public double Freq1 { get; }
    public int SampleRate { get; }
    public int SamplesPerSymbol { get; }


    /// <summary>
    /// Modulates a stream of bits using FSK.
    /// Bit false uses frequency freq0, and bit true uses frequency freq1.
    /// </summary>
    /// <param name="freq0">Frequency for bit 0 (e.g. 400 Hz).</param>
    /// <param name="freq1">Frequency for bit 1 (e.g. 600 Hz).</param>
    /// <param name="sampleRate">Audio sample rate (e.g. 44100 Hz).</param>
    /// <param name="samplesPerSymbol">Number of samples per bit (symbol duration).</param>
    /// <returns>Array of float samples representing the FSK signal.</returns>
    public FSKModulator(double freq0, double freq1, int sampleRate, int samplesPerSymbol)
    {
        Freq0 = freq0;
        Freq1 = freq1;
        SampleRate = sampleRate;
        SamplesPerSymbol = samplesPerSymbol;
    }

    //private Span<bool> BytesToBoolArray(Span<byte> data)
    //{
    //    Span<bool> bools = new Span<bool>(_bitBuffer, 0, data.Length * 8);  

    //    for (int i = 0; i < data.Length; i++)
    //    {
    //        for (int j = 0; j < 8; j++)
    //        {
    //            bools[i * 8 + j] = ((data[i] >> j) & 1) == 1;
    //        }
    //    }
    //    return bools;
    //}

    /// <summary>
    /// Converts float samples (range [-1, 1]) to a 16-bit PCM little-endian byte array.
    /// </summary>
    /// <param name="samples">Float sample array.</param>
    /// <returns>Byte array of 16-bit PCM data.</returns>
    //private Span<byte> ConvertTo16BitPCM(Span<float> samples)
    //{
    //    Span<byte> bytes = new Span<byte>(_byteBuffer, 0, samples.Length * 2);
    //    Span<short> pcmSamples = MemoryMarshal.Cast<byte, short>(bytes);
    //    for (int i = 0; i < samples.Length; i++)
    //    {
    //        //float normalized = samples[i] * 2f - 1f;

    //        float sample = samples[i];
    //        // Clamp sample to [-1, 1]
    //        sample = Math.Max(-1f, Math.Min(1f, sample));
    //        pcmSamples[i] = (short)(sample * short.MaxValue);
    //    }

    //    return bytes;
    //}

    public float[] Modulate(bool[] data)
    {
        int totalSamples = data.Length * SamplesPerSymbol;
        float[] signal = new float[totalSamples];

        for (int i = 0; i < data.Length; i++)
        {
            // Choose frequency based on bit value.
            double freq = data[i] ? Freq1 : Freq0;
            for (int j = 0; j < SamplesPerSymbol; j++)
            {
                int sampleIndex = i * SamplesPerSymbol + j;
                double t = sampleIndex / (double)SampleRate;
                signal[sampleIndex] = (float)Math.Sin(2 * Math.PI * freq * t);
            }
        }

        return signal;
    }
}
