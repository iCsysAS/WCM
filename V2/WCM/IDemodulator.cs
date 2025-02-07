namespace WCM;

public interface IDemodulator
{
    bool[] Demodulate(float[] signal);
    bool[] DemodulateAfterPreamble(float[] signal, float[] preambleSignal, out int preambleStartIndex);
}
