// See https://aka.ms/new-console-template for more information
using System.Text;
using WCM;

double freq0 = 450;
double freq1 = 550;
int sampleRate = 44100;
int samplesPerSymbol = 100;

CancellationTokenSource cts = new CancellationTokenSource();

IModulator modulator = new FSKModulator(freq0, freq1, sampleRate, samplesPerSymbol);
var preambleSymbol = modulator.Modulate(Constants.BarkerPreambleBits);

IDemodulator demodulator = new FSKDemodulator(freq0, freq1, sampleRate, samplesPerSymbol);
ISource source = new NAudioSource(sampleRate, 40,0.02f,cts.Token);


#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
Task.Run(() =>
{
    while(!cts.Token.IsCancellationRequested)
    {
        float[] signal = source.Read();
        bool[] bits = demodulator.DemodulateAfterPreamble(signal,preambleSymbol, out int startIndex);
        if(bits == null)
        {
            continue;
        }

        byte[] bytes = bits.ToBytes();
        Console.WriteLine(Encoding.UTF8.GetString(bytes));
    }
}, cts.Token);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

Console.ReadLine();
cts.Cancel();

