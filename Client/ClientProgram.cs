using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Client;

internal static class ClientProgram
{
    private static readonly Dictionary<int, long> _times = new Dictionary<int, long>();
    private static readonly AutoResetEvent _autoResetEvent = new AutoResetEvent(false);

    private static async Task Main()
    {
        var client = new TcpClient();
        // await client.ConnectAsync(IPAddress.Parse("134.209.202.27"), 8080);
        await client.ConnectAsync(IPAddress.Loopback, 8080);

        new Thread(() => _ = HandleSend(client)).Start();
        new Thread(() => _ = HandleReceive(client)).Start();

        Console.ReadKey();
    }

    private static async Task HandleSend(TcpClient client)
    {
        while (true)
        {
            var epoch = nanoTime();
            var indexEncoded = BitConverter.GetBytes(_times.Count);
            _times[_times.Count] = epoch;

            await client.Client.SendAsync(indexEncoded);
            _autoResetEvent.WaitOne();
        }
    }

    private static async Task HandleReceive(TcpClient client)
    {
        var receiveBuffer = new byte[1024];

        while (true)
        {
            await client.Client.ReceiveAsync(receiveBuffer);
            var end = nanoTime();

            var index = BitConverter.ToInt32(receiveBuffer);
            var delta = end - _times[index];

            Console.WriteLine($"Roundtrip {index} took: {(double)delta / 1_000_000}ms");
            _autoResetEvent.Set();
        }
    }

    private static long nanoTime()
    {
        var nano = 10000L * Stopwatch.GetTimestamp();
        nano /= TimeSpan.TicksPerMillisecond;
        nano *= 100L;
        return nano;
    }
}