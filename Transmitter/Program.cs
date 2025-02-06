using NAudio.Wave;
using System.Text;
using WCM;
using static System.Runtime.InteropServices.JavaScript.JSType;

WaveOutEvent waveOut = new WaveOutEvent();
ModulationScheme modulationScheme = ModulationScheme.BPSK;

while (true)
{
    byte[] data = Encoding.UTF8.GetBytes("Hello, World!");
    byte[] withPreamble = Modulation.AddPreamble(data);
    byte[] encodedData = Modulation.ApplyReedSolomon(withPreamble);

    float[] audioBuffer = new float[encodedData.Length * Modulation.SAMPLES_PER_SYMBOL * 4];

    for (int i = 0; i < encodedData.Length; i++)
    {
        bool[] bits = new bool[8];
        for (int j = 0; j < 8; j++) bits[j] = (encodedData[i] & (1 << (7 - j))) != 0;

        float[] s1, s2, s3, s4;

        switch (modulationScheme)
        {
            case ModulationScheme.BPSK:
                s1 = Modulation.GenerateBPSK(bits[0]);
                s2 = Modulation.GenerateBPSK(bits[2]);
                s3 = Modulation.GenerateBPSK(bits[4]);
                s4 = Modulation.GenerateBPSK(bits[6]);
                break;
            case ModulationScheme.FSK:
                s1 = Modulation.GenerateFSK(bits[0]);
                s2 = Modulation.GenerateFSK(bits[2]);
                s3 = Modulation.GenerateFSK(bits[4]);
                s4 = Modulation.GenerateFSK(bits[6]);
                break;
            case ModulationScheme.QPSK:
                s1 = Modulation.GenerateQPSK(bits[0], bits[1]);
                s2 = Modulation.GenerateQPSK(bits[2], bits[3]);
                s3 = Modulation.GenerateQPSK(bits[4], bits[5]);
                s4 = Modulation.GenerateQPSK(bits[6], bits[7]);
                break;
            case ModulationScheme.EightPSK:
                s1 = Modulation.Generate8PSK(bits[0] ? 1 : 0, bits[1] ? 1 : 0, bits[2] ? 1 : 0);
                s2 = Modulation.Generate8PSK(bits[3] ? 1 : 0, bits[4] ? 1 : 0, bits[5] ? 1 : 0);
                s3 = Modulation.Generate8PSK(bits[6] ? 1 : 0, bits[7] ? 1 : 0, bits[0] ? 1 : 0);
                s4 = Modulation.Generate8PSK(bits[1] ? 1 : 0, bits[2] ? 1 : 0, bits[3] ? 1 : 0);
                break;
            default:
                throw new Exception("Invalid Modulation Scheme");
        }

        Array.Copy(s1, 0, audioBuffer, i * Modulation.SAMPLES_PER_SYMBOL * 4, Modulation.SAMPLES_PER_SYMBOL);
        Array.Copy(s2, 0, audioBuffer, i * Modulation.SAMPLES_PER_SYMBOL * 4 + Modulation.SAMPLES_PER_SYMBOL, Modulation.SAMPLES_PER_SYMBOL);
        Array.Copy(s3, 0, audioBuffer, i * Modulation.SAMPLES_PER_SYMBOL * 4 + Modulation.SAMPLES_PER_SYMBOL * 2, Modulation.SAMPLES_PER_SYMBOL);
        Array.Copy(s4, 0, audioBuffer, i * Modulation.SAMPLES_PER_SYMBOL * 4 + Modulation.SAMPLES_PER_SYMBOL * 3, Modulation.SAMPLES_PER_SYMBOL);
    }

    var waveProvider = new FloatArrayWaveProvider(audioBuffer, Modulation.SAMPLE_RATE);
    waveOut.Init(waveProvider);
    waveOut.Play();
}
