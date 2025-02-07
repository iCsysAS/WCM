namespace WCM;

public static class Converters
{
    public static byte[] ToBytes(this bool[] bits)
    {
        if (bits.Length % 8 != 0)
        {
            throw new ArgumentException("The number of bits must be a multiple of 8.");
        }
        byte[] bytes = new byte[bits.Length / 8];
        for (int i = 0; i < bytes.Length; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                bytes[i] |= (byte)(bits[i * 8 + j] ? 1 << (7 - j) : 0);
            }
        }
        return bytes;
    }

    public static bool[] ToBits(this byte[] bytes)
    {
        bool[] bits = new bool[bytes.Length * 8];
        for (int i = 0; i < bytes.Length; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                bits[i * 8 + j] = (bytes[i] & (1 << (7 - j))) != 0;
            }
        }
        return bits;
    }
}
