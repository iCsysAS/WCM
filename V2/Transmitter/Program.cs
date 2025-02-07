// See https://aka.ms/new-console-template for more information
using System.Diagnostics;
using System.Text;
using WCM;

double freq0 = 450;
double freq1 = 550;
int sampleRate = 44100;
int samplesPerSymbol = 100;

IModulator modulator = new FSKModulator(freq0, freq1, sampleRate, samplesPerSymbol);
ISink sink = new NAudioSink(sampleRate);

var preambleSignal = modulator.Modulate(Constants.BarkerPreambleBits);


var data = new byte[50];
for (int i = 0; i < data.Length; i++)
{
    data[i] = (byte)(i % 2);
}

var dataBits = data.ToBits();

var dataSignal = modulator.Modulate(dataBits);


float[] completeSignal = new float[preambleSignal.Length + dataSignal.Length];
preambleSignal.CopyTo(completeSignal,0);
dataSignal.CopyTo(completeSignal, preambleSignal.Length);

Stopwatch sw = Stopwatch.StartNew();
while (true)
{
    sw.Restart();
    sink.Write(completeSignal);
    sw.Stop();

    Console.WriteLine($"{DateTime.UtcNow} DataLength: {data.Length} TxTime: {sw.ElapsedMilliseconds} ms");
    
    Thread.Sleep(50);
}



