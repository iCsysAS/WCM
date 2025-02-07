// See https://aka.ms/new-console-template for more information
using System.Diagnostics;
using WCM;

double freq0 = 400;
double freq1 = 600;
int sampleRate = 48000;
int samplesPerSymbol = 100;

IModulator modulator = new FSKModulator(freq0, freq1, sampleRate, samplesPerSymbol);

var preambleStart = modulator.Modulate(Constants.BarkerStart);
var preambleStop = modulator.Modulate(Constants.BarkerStop);

ISink sink = new NAudioSink(sampleRate,preambleStart,preambleStop);





byte[] data = [255, 0, 255, 0, 255, 0, 255, 0, 255, 0, 255, 0, 255, 0, 255, 0];

var dataBits = data.ToBits();

var dataSignal = modulator.Modulate(dataBits);

Stopwatch sw = Stopwatch.StartNew();

while (true)
{
    sw.Restart();
    sink.Write(dataSignal);
    sw.Stop();

    Console.WriteLine($"{DateTime.UtcNow} DataLength: {data.Length} TxTime: {sw.ElapsedMilliseconds} ms");
    
    Thread.Sleep(1000);
}



