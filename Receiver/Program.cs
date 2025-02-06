using NAudio.Wave;
using System.Text;
using WCM;

WaveInEvent waveIn = new WaveInEvent();
ModulationScheme modulationScheme = ModulationScheme.BPSK;

waveIn.DataAvailable += (sender, e) =>
{
    float[] audioData = new float[e.BytesRecorded / sizeof(float)];
    Buffer.BlockCopy(e.Buffer, 0, audioData, 0, e.BytesRecorded);

    byte[] bitstream;

    switch (modulationScheme)
    {
        case ModulationScheme.BPSK:
            bitstream = Modulation.DemodulateBPSK(audioData);
            break;
        case ModulationScheme.FSK:
            bitstream = Modulation.DemodulateFSK(audioData);
            break;
        case ModulationScheme.QPSK:
            bitstream = Modulation.DemodulateQPSK(audioData);
            break;
        case ModulationScheme.EightPSK:
            bitstream = Modulation.Demodulate8PSK(audioData);
            break;
        default:
            throw new Exception("Invalid Modulation Scheme");
    }

    try
    {
        byte[] decodedData = Modulation.DecodeReedSolomon(bitstream);

        if (!Modulation.DetectPreamble(decodedData))
        {
            Console.WriteLine("Preamble not detected, discarding data.");
            return;
        }

        byte[] cleanData = Modulation.RemovePreamble(decodedData);
        string receivedText = Encoding.ASCII.GetString(cleanData);
        Console.WriteLine($"Received: {receivedText}");

    }
    catch (Exception ex)
    {
        Console.WriteLine("Error decoding data: " + ex.Message);
    }
};

waveIn.StartRecording();
Console.ReadLine();
