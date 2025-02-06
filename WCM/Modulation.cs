using System.Text;
using Witteborn.ReedSolomon;

namespace WCM;

public static class Modulation
{
    public const int SAMPLE_RATE = 8000;
    public const int BIT_RATE = 100;
    public const int SAMPLES_PER_SYMBOL = SAMPLE_RATE / BIT_RATE;
    public const int FREQ_LOW = 400;
    public const int FREQ_HIGH = 600;

    private const int DATA_SHARDS = 8;
    private const int PARITY_SHARDS = 4;

    private static ReedSolomon rs = new ReedSolomon(8, 4);

    public static byte[] ApplyReedSolomon(byte[] data)
    {
        int paddedLength = (int)Math.Ceiling((double)data.Length / DATA_SHARDS) * DATA_SHARDS;
        byte[] paddedData = new byte[paddedLength];
        Array.Copy(data, paddedData, data.Length);

        byte[][] encodedBlocks = rs.ManagedEncode(paddedData, DATA_SHARDS, PARITY_SHARDS);
        return ConvertToByteArray(encodedBlocks); // Flatten byte[][] to byte[]
    }

    public static byte[] AddPreamble(byte[] data)
    {
        return Preamble.Sequence.Concat(data).ToArray();
    }

    public static bool DetectPreamble(byte[] data)
    {
        return data.Length >= Preamble.Sequence.Length && data.Take(Preamble.Sequence.Length).SequenceEqual(Preamble.Sequence);
    }

    public static byte[] RemovePreamble(byte[] data)
    {
        return data.Skip(Preamble.Sequence.Length).ToArray();
    }

    public static byte[] DecodeReedSolomon(byte[] bitstream)
    {
        byte[][] reshapedData = ConvertToByteMatrix(bitstream);
        return  rs.ManagedDecode(reshapedData,DATA_SHARDS,PARITY_SHARDS);
    }

    public static float[] GenerateBPSK(bool bit)
    {
        float phase = bit ? 0 : 180;
        float[] symbol = new float[SAMPLES_PER_SYMBOL];
        for (int i = 0; i < SAMPLES_PER_SYMBOL; i++)
        {
            double t = (double)i / SAMPLE_RATE;
            symbol[i] = (float)Math.Cos(2 * Math.PI * 1000 * t + Math.PI * phase / 180);
        }
        return symbol;
    }

    public static float[] GenerateFSK(bool bit)
    {
        float freq = bit ? FREQ_HIGH : FREQ_LOW;
        float[] symbol = new float[SAMPLES_PER_SYMBOL];
        for (int i = 0; i < SAMPLES_PER_SYMBOL; i++)
        {
            double t = (double)i / SAMPLE_RATE;
            symbol[i] = (float)Math.Sin(2 * Math.PI * freq * t);
        }
        return symbol;
    }

    public static float[] GenerateQPSK(bool bit1, bool bit2)
    {
        float phase = (bit1 ? 90 : 0) + (bit2 ? 180 : 0);
        float[] symbol = new float[SAMPLES_PER_SYMBOL];

        for (int i = 0; i < SAMPLES_PER_SYMBOL; i++)
        {
            double t = (double)i / SAMPLE_RATE;
            symbol[i] = (float)Math.Cos(2 * Math.PI * 1000 * t + Math.PI * phase / 180);
        }
        return symbol;
    }

    public static float[] Generate8PSK(int bit1, int bit2, int bit3)
    {
        float phase = (bit1 * 45) + (bit2 * 90) + (bit3 * 135);
        float[] symbol = new float[SAMPLES_PER_SYMBOL];
        for (int i = 0; i < SAMPLES_PER_SYMBOL; i++)
        {
            double t = (double)i / SAMPLE_RATE;
            symbol[i] = (float)Math.Cos(2 * Math.PI * 1500 * t + Math.PI * phase / 180);
        }
        return symbol;
    }

    public static byte[] DemodulateBPSK(float[] audioData)
    {
        byte[] bitstream = new byte[audioData.Length / SAMPLES_PER_SYMBOL / 4];

        for (int i = 0; i < bitstream.Length; i++)
        {
            float sum = 0;
            for (int j = 0; j < SAMPLES_PER_SYMBOL; j++)
                sum += audioData[i * SAMPLES_PER_SYMBOL + j];

            bitstream[i] = (sum > 0) ? (byte)1 : (byte)0;
        }
        return bitstream;
    }

    public static byte[] DemodulateFSK(float[] audioData)
    {
        byte[] bitstream = new byte[audioData.Length / SAMPLES_PER_SYMBOL / 4];

        for (int i = 0; i < bitstream.Length; i++)
        {
            float sumLow = 0, sumHigh = 0;
            for (int j = 0; j < SAMPLES_PER_SYMBOL; j++)
            {
                sumLow += MathF.Sin(2 * MathF.PI * FREQ_LOW * j / SAMPLE_RATE) * audioData[i * SAMPLES_PER_SYMBOL + j];
                sumHigh += MathF.Sin(2 * MathF.PI * FREQ_HIGH * j / SAMPLE_RATE) * audioData[i * SAMPLES_PER_SYMBOL + j];
            }
            bitstream[i] = (byte)((sumHigh > sumLow) ? 1 : 0);
        }
        return bitstream;
    }

    public static byte[] DemodulateQPSK(float[] audioData)
    {
        byte[] bitstream = new byte[audioData.Length / SAMPLES_PER_SYMBOL / 4];
        for (int i = 0; i < bitstream.Length; i++)
        {
            float sum = 0;
            for (int j = 0; j < SAMPLES_PER_SYMBOL; j++)
                sum += audioData[i * SAMPLES_PER_SYMBOL + j];

            bool bit1 = sum > 0;
            bool bit2 = sum < 0;
            bitstream[i] = (byte)((bit1 ? 1 : 0) << 7 | (bit2 ? 1 : 0) << 6);
        }
        return bitstream;
    }

    public static byte[] Demodulate8PSK(float[] audioData)
    {
        byte[] bitstream = new byte[audioData.Length / SAMPLES_PER_SYMBOL / 4];
        for (int i = 0; i < bitstream.Length; i++)
        {
            float sum = 0;
            for (int j = 0; j < SAMPLES_PER_SYMBOL; j++)
                sum += audioData[i * SAMPLES_PER_SYMBOL + j];

            int phaseIndex = (int)Math.Round(sum / 45.0) % 8;
            bitstream[i] = (byte)(phaseIndex);
        }
        return bitstream;
    }

    public static byte[][] ConvertToByteMatrix(byte[] flatData, int rowSize = DATA_SHARDS)
    {
        int numRows = flatData.Length / rowSize;
        byte[][] matrix = new byte[numRows][];

        for (int i = 0; i < numRows; i++)
        {
            matrix[i] = new byte[rowSize];
            Array.Copy(flatData, i * rowSize, matrix[i], 0, rowSize);
        }

        return matrix;
    }

    public static byte[] ConvertToByteArray(byte[][] matrix)
    {
        return matrix.SelectMany(row => row).ToArray();
    }

}
