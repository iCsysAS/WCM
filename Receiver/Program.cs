// See https://aka.ms/new-console-template for more information
using System.Text;
using WCM;

double freq0 = 400;
double freq1 = 600;
int sampleRate = 48000;
int samplesPerSymbol = 100;

CancellationTokenSource cts = new CancellationTokenSource();

IModulator modulator = new FSKModulator(freq0, freq1, sampleRate, samplesPerSymbol);
var preambleStart = modulator.Modulate(Constants.BarkerStart);
var preambleStop = modulator.Modulate(Constants.BarkerStop);

IDemodulator demodulator = new FSKDemodulator(freq0, freq1, sampleRate, samplesPerSymbol);
ISource source = new NAudioSource(sampleRate,0.1f,0,samplesPerSymbol,preambleStart,preambleStop,cts.Token);


#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
Task.Run(() =>
{
    while(!cts.Token.IsCancellationRequested)
    {
        float[] signal = source.Read();
        bool[] bits = demodulator.Demodulate(signal);
        if(bits == null)
        {
            continue;
        }

        for (int i = 0; i < bits.Length; i++)
        {
            Console.Write(bits[i] ? "1" : "0");
            Console.Write(" ");
        }

        Console.WriteLine();
        Console.WriteLine(bits.Length);
        Console.WriteLine();

        //byte[] bytes = bits.ToBytes();
        //Console.WriteLine(Encoding.UTF8.GetString(bytes));
    }
}, cts.Token);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

Console.ReadLine();
cts.Cancel();

