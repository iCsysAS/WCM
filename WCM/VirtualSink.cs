using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace WCM
{
    public class VirtualSink : ISink
    {
        private Channel<float> _channel;
        public VirtualSink(Channel<float> channel)
        {
            _channel = channel;
        }

        public void Write(float[] signal)
        {
            for (int i = 0; i < signal.Length; i++)
            {
                _channel.Writer.TryWrite(signal[i]);
            }
        }
    }
}
