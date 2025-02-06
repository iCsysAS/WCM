using NAudio.Wave;

namespace WCM;

public class FloatArrayWaveProvider : WaveProvider16
{
    private readonly float[] _buffer;
    private int _position;

    public FloatArrayWaveProvider(float[] buffer, int sampleRate) : base(sampleRate, 1)
    {
        _buffer = buffer;
    }

    public override int Read(short[] buffer, int offset, int sampleCount)
    {
        int samplesToCopy = Math.Min(sampleCount, _buffer.Length - _position);
        for (int i = 0; i < samplesToCopy; i++)
        {
            buffer[offset + i] = (short)(_buffer[_position + i] * short.MaxValue);
        }
        _position += samplesToCopy;
        return samplesToCopy;
    }
}
