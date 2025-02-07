namespace WCM;

public class FSKDemodulator : IDemodulator
{
    public double Freq0 { get; }
    public double Freq1 { get; }
    public int SampleRate { get; }
    public int SamplesPerSymbol { get; }
    public int MaxPacketSize { get; }

    /// <summary>
    /// Modulates a stream of bits using FSK.
    /// Bit false uses frequency freq0, and bit true uses frequency freq1.
    /// </summary>
    /// <param name="freq0">Frequency for bit 0 (e.g. 400 Hz).</param>
    /// <param name="freq1">Frequency for bit 1 (e.g. 600 Hz).</param>
    /// <param name="sampleRate">Audio sample rate (e.g. 44100 Hz).</param>
    /// <param name="samplesPerSymbol">Number of samples per bit (symbol duration).</param>
    /// <returns>Array of float samples representing the FSK signal.</returns>
    public FSKDemodulator(double freq0, double freq1, int sampleRate, int samplesPerSymbol)
    {
        Freq0 = freq0;
        Freq1 = freq1;
        SampleRate = sampleRate;
        SamplesPerSymbol = samplesPerSymbol;
        this.MaxPacketSize = MaxPacketSize;

    }

    public bool[] Demodulate(float[] signal)
    {
        return Demodulate(signal.AsSpan());
    }

    public bool[] Demodulate(ReadOnlySpan<float> signal)
    {
        int numSymbols = signal.Length / SamplesPerSymbol;

        bool[] data = new bool[numSymbols];

        for (int i = 0; i < numSymbols; i++)
        {
            // Extract the samples corresponding to one symbol.
            ReadOnlySpan<float> symbol = signal.Slice(i * SamplesPerSymbol, SamplesPerSymbol);

            // Use the Goertzel algorithm to compute energy at both frequencies.
            double energy0 = ComputeGoertzelEnergy(symbol, Freq0, SampleRate);
            double energy1 = ComputeGoertzelEnergy(symbol, Freq1, SampleRate);

            // Decide bit value based on which frequency has higher energy.
            data[i] = energy1 > energy0;

        }

        return data;
    }

    public int FindPreamble(float[] signal, float[] preambleSignal)
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

    public bool[] DemodulateAfterPreamble(float[] signal, float[] preambleSignal, out int preambleStartIndex)
    {
        preambleStartIndex = FindPreamble(signal, preambleSignal);
        if (preambleStartIndex < 0)
            return null;

        // Determine how many symbols the preamble occupies.
        int preambleSymbols = preambleSignal.Length / SamplesPerSymbol;
        int dataStartIndex = preambleStartIndex + preambleSymbols * SamplesPerSymbol;
        ReadOnlySpan<float> dataSignal = signal.AsSpan().Slice(dataStartIndex);

        return Demodulate(dataSignal);
    }

    /// <summary>
    /// Uses the Goertzel algorithm to compute the energy of the signal at the target frequency.
    /// </summary>
    /// <param name="samples">The block of samples (one symbol) to analyze.</param>
    /// <param name="targetFrequency">The target frequency (Hz) for which to compute the energy.</param>
    /// <param name="sampleRate">The audio sample rate (Hz).</param>
    /// <returns>The computed energy at the target frequency.</returns>
    private double ComputeGoertzelEnergy(ReadOnlySpan<float> samples, double targetFrequency, int sampleRate)
    {
        int N = samples.Length;
        double omega = 2.0 * Math.PI * targetFrequency / sampleRate;
        double cosine = Math.Cos(omega);
        double coeff = 2.0 * cosine;
        double s_prev = 0.0;
        double s_prev2 = 0.0;

        // Process each sample using the Goertzel recurrence.
        for (int i = 0; i < N; i++)
        {
            double s = samples[i] + coeff * s_prev - s_prev2;
            s_prev2 = s_prev;
            s_prev = s;
        }

        // Compute the power at the target frequency.
        double power = s_prev2 * s_prev2 + s_prev * s_prev - coeff * s_prev * s_prev2;
        return power;
    }
}
